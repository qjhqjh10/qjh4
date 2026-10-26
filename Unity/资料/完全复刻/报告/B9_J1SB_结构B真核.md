# B9_J1SB · `战斗逻辑驱动` 域「结构 B」真核（34 条）+ 其余未归类 B 的归类（112 条）

> 代理：J1SB（只读查证）· 2026-10-26（第八十四会话）· 范围：`账/战斗逻辑驱动.tsv` 里 368 条 **B** 行。
> 判据第一权威 = `d:/2/tools/decomp_full/`（`.c` 方法体），次 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（ILSpy，用来**取字段名/枚举名**，⛔ 不用它取实参常量）。
> 产出补丁 = `账/_parts/B9_J1SB.tsv`（34 行，只改「差异」「备注」两列；`状态/等级/我们的落点/判据出处/判据要点/域/类/方法/轮次` 逐字未动 —— 已用脚本对主表逐列校验，0 处变更）。

---

## ① 结论

**一句话：34 条里没有一条是「没做」，也没有一条需要在代码层补实现 —— 26 条净等价、7 条有差异（全部落在「账面落点/判据写错」或「结构不同但净结果同」）、1 条判不出；真正的行为缺口在【另外 112 条】里，其中 19 条是可构造的行为差。**

### 34 条三档计数

| 档 | 条数 | 是哪些 |
|---|---:|---|
| **等价** | **26** | `AlreadyPlayedCardThisTurn` · `GetAllCardsInHand` · `GetAllCardsInPlay` · `GetCardsInCemetery` · `GetCardsInDeck` · `GetCardsInHandList` · `GetCardsInHandRef` · `GetDeck` · `GetEnemyHandRef` · `GetEnemyMinionsAndRemnantInPlay` · `GetFriendlyHandRef` · `GetHero` · `GetOriginalWarlord` · `GetPlayerMinionsAndRemnantInPlay` · `GetPremulliganScriptedData` · `GetStartingMana` · `GetUnitsInPlayRef` · `HasScriptedPremulliganActions` · `IsPremulliganScriptAvailable` · `IsThereDoubleOathEffect` · `CardWaitingToBePlayed` · `GetHighestAttackValue` · `GetOwnHero` · `IsTheSameTurnPlayed` · `SetAsPlayer` · `SetPray` |
| **有差异**（行为等价/已知偏离，**账面要改**） | **7** | `GetCurrentPlayerId`（返回 id 字符串、非 0/1）· `GetTotalMana`（落点指 `Energy`，应为 `MaxEnergy`）· `HasDiedSinceLastTurn` / `HasDiedThisTurn`（真等价物是 `DeadUnits.DeathTurn`，不是那两个计数）· `IsSameTrait`（条目粒度）· `SetFutureRemnant` / `SetLeavingRemnant`（结构：无可变位） |
| **判不出** | **1** | `WaitOpponentLastActions`（协程 `d__316` 真体未读） |

> 其中 3 条「等价」带**已知、今天不可观测**的残留：`GetCardsInHandList`（原版尾部 `OrderBy(uniqueInGameID)`、我们按手牌序，但我们的手牌类消费者只**计数**）· `GetUnitsInPlayRef`（引用语义换成 `Board`+`PlayOrder`）· `GetCardsInCemetery`（原版 `isRemnant` 那道闸我们没有 —— `EffectResolver` 已如实标着）。

### 其余未归类 B 的归类（**现读 112 条**，不是 106）

> ⚠️ 口径差：`B9_L5` 报的 106 = 当轮 368−253−9；现读主表 **B = 368**、`BattleManager` 180 + `CardScript` 76 = 256（L5 当时 253）⇒ 未归类者 = **112 条**。数字随账增长而变，⛔ 别写死。

| 归类 | 条数 | 判据 |
|---|---:|---|
| ① **结构性**（落点形状 / 结构差异 / 净结果同 ⇒ 永远 B，无需改码） | **58** | 差异列写的是「结构不同 / 机制差 / 分工不同 / 净结果同 / 已修」 |
| ② **只差判据要点或落点可升级**（可升 A） | **25** | 差异列写的是「没读全 / 未逐条核 / 不能声称等价 / 待核」 |
| ③ **有行为差（现状可达 ⇒ 该判 C，或 B+明确偏离）** | **19** | 见下 |
| **D 休眠真缺口**（机制缺、但**池内 0 张可达** ⇒ 建议判 D + 时间限定） | **5** | 见下 |
| **判不出**（判据读不到 / 未核） | **5** | 见下 |

**③ 19 条（行为差，主对话该分流去修）**

| 账行 | 方法 | 差异一句话 |
|---:|---|---|
| 93 | `AiScripted.ExecuteAction` | 未接 3 档（`ActiveAbility` 是桩；另两档池内 0 条） |
| 628 | `BroadcastCardPlayed` | 传 `preCalled=null` ⇒ **打出的那张会收到自己的 `card played`**（原版排除）；棋盘跳多一道待死闸 |
| 633 | `BroadcastCollectSpiritStone` | 预调实参 `card/subject` 传 null ⇒ **少了那条预调** |
| 634 | `BroadcastDeadUnit` | 缺原版第 4 支「手牌里不在场的那张」；`battleAction` 载荷（死因/格位）没进事件 |
| 635 | `BroadcastEarnedSkull` | **调用点未接**（缺 `earnedSkull` 计数 + 血量写完后的检查点） |
| 638 | `BroadcastSabotageCreated` | 缺「对造出来那张卡预调一次」（那张不在棋盘、无 `UnitState`） |
| 639 | `BroadcastSecretCreated` | 缺 `Signal.Raise(CreateSecretSignal)` + 预调主体 |
| 645 | `BroadcastUnitAttacked` | `attacker==target` 时原版**发两次**预调我们只一次；且不携带 `attackType` |
| 647 | `BroadcastUnitDamaged` | 缺原版「攻击者那一定收到一次」那一支（根因 = 伤害广播不带 `damageType`） |
| 650 | `BroadcastUnitHealed` | **调用点未接**（原版唯一生产点 = `UseSurvivor.c:87`） |
| 654 | `BroadcastUnitMob` | 自排除按 `subject`、原版排除 `caster` ⇒ 集合可能不同 |
| 655 | `BroadcastUnitObtainedHuntMark` | 棋盘跳带待死闸、原版这一跳无 `IsInPlay` 过滤 |
| 660 | `BroadcastUnitReanimated` | 同上待死闸 + 无 `Signal.Raise` 对应物 |
| 1004 | `DamageDeltToEnemyUnit.OnDamageDelt` | 我们记 `actual`、原版记 `damageValue`（absorbed 是否计入**未核**）；归属按「受害者对侧」⇒ 自己打自己人会记到对面 |
| 1025 | `EntityScript.EnemyReturnsAttack` | 3 个调用点未接（`RecordAttackPendingDamage` 无对应物 / `CardHighlight` 未接） |
| 1037 | `EntityScript.GetThisCardPlayedDamage` | 缺「本卡被打出时造成的伤害」取数口（高亮/预览用） |
| 1185 | `PlayerHand.RemoveEffectsFromCard` | 缺「按施放者卡反查 → 摘那条记录」入口 |
| 1188 | `PlayerHand.ReplaceMulliganCard` | 换牌后**牌库排列不同**（原版逐张从顶补；我们先洗再连抽） |
| 1308 | `SupportMethods.GetRandomOrder` | RNG 形状不同（原版预烧 100 整数+下标 vs 我们 `ctx.Rng.Next`）⇒ 同种子排列不同 |

**D 5 条（休眠：机制缺、0 张可达）**：`642 BroadcastTurnSetup(155)` · `644 BroadcastTurnStarted(160)` · `658 BroadcastUnitPoisoned(290)` · `103 AiScripted.GetTipPosition`（50/70/100 三档未做、0 条）· `1288 RawCardScript.UpdateTriggerOnce`（分发点没有 200 这一档）。
**判不出 5 条**：`1122 GetNextSlotToCard` · `1142 RemoveSwarm` · `1169 GetWhispersCardsCount`（产物文件名与内容不符）· `1247 GetTransformedKnightId`（50 行只读前段）· `1300 GetCardListFromIds`（原版保留 null 占位的 4 个调用点未核）。

**① 58 条**（结构性）：62, 77, 82, 643, 656, 661, 663, 668, 670, 677, 990, 992, 994, 1003, 1007, 1026, 1041, 1065, 1078, 1079, 1080, 1081, 1082, 1083, 1091, 1093, 1097, 1101, 1102, 1103, 1107, 1121, 1124, 1139, 1144, 1156, 1166, 1186, 1187, 1189, 1209, 1217, 1242, 1246, 1248, 1261, 1265, 1270, 1277, 1278, 1347, 1348, 1355, 1356, 1357, 1363, 1366, 1367。
⚠️ 这批里 `1102 BroadcastActivateMinion` / `670 GetTargetUnit` 是**边界行**（机制差三条、但可观察量只到断言），按口径归 ① 更保守。

**② 25 条**（可升 A）：25, 76, 79, 80, 83, 96, 636, 674, 675, 991, 1042, 1052, 1053, 1114, 1178, 1190, 1191, 1218, 1228, 1262, 1264, 1281, 1282, 1285, 1286。

> 🧭 归类方法（如实）：112 条的「差异」列**全部**读过（含落点列），其中约 40 条读了全文与方法体抽读；其余按账内措辞 + 上面两条正则归类后人工覆盖 20 条。① 与 ② 的边界是**语义判断**，不是机械判据 —— 若调度台要逐条签字，② 那 25 条先看。

---

## ② 34 条逐条表

| # | 方法 | 原版怎么取（`.c`/ILSpy 实读） | 我们是什么 | 结论 | 证据 |
|---:|---|---|---|---|---|
| 1 | `BattleManager.AlreadyPlayedCardThisTurn(bool)` | `+0x240`(isPlayer 真)/`+0x241` 两个 per-side bool 的**属性取值器**；写点 `_ResolvePlayCardFromHand:1433/:1444`、`_NextTurn:174`（一次清两字节）；**读点 0** | 全局 `int CardsPlayedInTurn`（是**另一个方法** `CardsPlayedInTurn(+0x3D0)` 的对应物） | **等价**（原版死状态） | `decomp_full/BattleManager__AlreadyPlayedCardThisTurn.c:1-10` · ILSpy `BattleManager.cs:42699` · `BattleManager__set_playerPlayerCardInTurn.c:5` |
| 2 | `GetAllCardsInHand()` | `.c:5` = `GetCardsInHandList(mgr,1,1,1,1,1,1)`；全树 **0 调用点** | 各方 `PlayerState.Hand` | **等价** | `BattleManager__GetAllCardsInHand.c:5` |
| 3 | `GetAllCardsInPlay(bool)` | 扫 `+0x470` 全场，**排除 cardType 10/20/40**，要 `IsInPlay()`；假则仅当 `includeDying && cardState==5`；用复用缓存 List；**0 调用点** | 双方 `Board`（督军另格）；待死 = `ctx.IsPendingDeath` | **等价**（无调用点） | `BattleManager__GetAllCardsInPlay.c:1-90` · ILSpy `:41391` |
| 4 | `GetCardsInCemetery(6×bool)` | 扫 `+0x110`/`+0x60` **正序**；过 inclPlayer/Enemy（`+0x40`）、inclMinions(0)/Spells(20)/Whispers(40)；**无条件排除 10(hero) 与 `isRemnant`**；**`inclWeapons` 未被读**；唯一调用点 `AbilityLogic.GetTargets:951` | `PlayerState.Discard` + `BattleContext.DeadUnits`；筛在 `EffectResolver.ReturnFromCemetery` | **等价** + 已知偏离（残骸维） | `BattleManager__GetCardsInCemetery.c:1-72` · ILSpy `:41757` · `EffectResolver.cs:6629-6633` |
| 5 | `GetCardsInDeck(5×bool)` | 扫 `+0x310` 再 `+0x318`；先判 `cardState==0(inDeck)`；**排除 10/40/残骸**；**`inclWeapons` 未读**；调用点 `GetTargets:917/:963` + `GetCardsDeckPlayer` | `PlayerState.Deck`（今天无建池消费者） | **等价** | `BattleManager__GetCardsInDeck.c:1-107` · ILSpy `:41997` |
| 6 | `GetCardsInHandList(6×bool)` | 两侧手牌（**先当前行动方那侧**）+ 同一组类型筛 + **排除 hero/残骸** + **`inclWeapons` 未读**；尾部 `Where(≠null).`**`OrderBy(c=>c.uniqueInGameID)`**`.ToList()`（`+0x22c`）；8 个调用点 | 各 `PlayerState.Hand`；有「先当前回合方再另一方」（`BroadcastHandWhen` 两跳），**无 `uniqueInGameID` 排序**（消费者只计数） | **等价** + 顺序残留（不可观测） | `BattleManager__GetCardsInHandList.c:1-93` · ILSpy `:41773-41794` · `BattleManager.__c___GetCardsInHandList_b__718_1.c`（key = `uniqueInGameID`） |
| 7 | `GetCardsInHandRef(bool)` | `playerHand(+0xd0)`/`enemyHand(+0xd8)` 的 `currentHand(+0x50)` **引用**；**40+ 个 `Broadcast*` 在读** | `PlayerState.Hand`（`readonly List`） | **等价** | `BattleManager__GetCardsInHandRef.c:1-19` · ILSpy `:41872` |
| 8 | `GetCurrentPlayerId(bool)` | 🔴 `public string`：`isPlayerTurn` 真 ⇒ `matchData.playerId`、假 ⇒ `matchData.opponentId`（**账户 id 字符串**）；唯一调用点 `_NextTurn:182` **只喂一句 Log** | `BattleContext.Active`（0/1 行动方）—— **不是同一个东西** | **有差异（账面）** / 行为等价 | ILSpy `BattleManager.cs:20807-20834` · `BattleManager._NextTurn_d__395__MoveNext.c:182-187` · 桩 `BattleManager.cs:5324` |
| 9 | `GetDeck(bool)` | `+0x310`(playerShuffledDeck)/`+0x318`(enemyShuffledDeck) = **活的抽牌堆**（`get_Item(0)` 是牌库顶）；4 个调用点 | `PlayerState.Deck` | **等价** | `BattleManager__GetDeck.c:1-10` · ILSpy `:42690` / `:35351` |
| 10 | `GetEnemyHandRef()` | `enemyHand(+0xd8).currentHand`；调用点 = `AiScripted.*` 7 处 | 敌方 `Hand` | **等价**（教程 AI 专用） | `BattleManager__GetEnemyHandRef.c:1-11` |
| 11 | `GetEnemyMinionsAndRemnantInPlay()` | 扫 `+0x470`：`isPlayer==false` ∧ cardType ∉{10,20,40} ∧ `IsInPlay()`；**明确排除 Hero**；缓存 `+0x548`；调用点 9 个 AI 文件 | 敌方 `Board` 非空格（督军由 `IsWarlord` 单列） | **等价** | `BattleManager__GetEnemyMinionsAndRemnantInPlay.c:1-82` · `AiTargets.cs:768` |
| 12 | `GetFriendlyHandRef()` | `playerHand(+0xd0).currentHand`；`AiScripted.*` 7 处 | 友方 `Hand` | **等价** | `BattleManager__GetFriendlyHandRef.c:1-11` |
| 13 | `GetHero(bool)` | `playerManager(+0xC0)`/`enemyManager(+0xC8)` ⇒ `heroCard(+0x28)`；调用点极多 | `PlayerState.Warlord`（= `Board[WarlordSlot]` 同一对象） | **等价**（null 处理不同：原版抛，我们出声） | `BattleManager__GetHero.c:1-19` · ILSpy `:42417` |
| 14 | `GetOriginalWarlord(bool)` | 返回 **RawCardScript** = `matchData.playerRawBattleData`/`enemyRawBattleData`.**`deckWarlord`**，null 才回退 `heroCard.rawCardData`；**唯一调用点 `CardScript__ResolveWeaponKilled.c:37-55`**（取原始督军 `+0x158` 武器[0] → 建 27 动作） | `Warlord.Card`；**`ResolveWeaponKilled` 我们没实现** | **等价**（今天不可达） | ILSpy `:42427-42477` · `CardScript__ResolveWeaponKilled.c:36-55` |
| 15 | `GetPlayerMinionsAndRemnantInPlay()` | 同上（`isPlayer != false`），缓存 `+0x550` | 本方 `Board` 非空格 | **等价** | `BattleManager__GetPlayerMinionsAndRemnantInPlay.c:1-82` |
| 16 | `GetPremulliganScriptedData()` | 🔴 三档门：`preMulliganScriptedActions ≠ null ∧ scriptedTurn ∧ Count ≥ 1`，否则 **null**；6 关 `scriptedTurn` **全 false** | 只有数据字段 `TutorialStageData.preMulligan`，无谓词 | **等价**（原版恒 null） | `BattleManager__GetPremulliganScriptedData.c:1-46` · ILSpy `:36209` · `Resources/tutorial_stages.json`（6 关现读） |
| 17 | `GetStartingMana(bool)` | `playerGoesSecond(+0x247)` XOR `isPlayer` ⇒ `scenario(+0xa8).startingManaSecond(+0x30)` / `startingMana(+0x2c)` | `p == Players[SecondSeat] ? startingManaSecond : startingMana`（逐字同义） | **等价** | `BattleManager__GetStartingMana.c:1-26` · `RuleCore.cs:1738`（现读，符号 = `BeginTurn` 的 `MaxEnergy` 式） |
| 18 | `GetTotalMana(bool)` | = `PlayerManager.totalMana(+0x38)`（本回合**总量**，纯字段读）；**剩余** 是 `unusedMana(+0x4C)` | 等价物 = **`MaxEnergy`**；行内落点写的 `Energy` 是「剩余」 | **有差异（落点错）** / 行为等价 | `BattleManager__GetTotalMana.c:1-20` · `PlayerManager__GetTotalMana.c:1-28` · `AiTargets.cs:250` |
| 19 | `GetUnitsInPlayRef()` | 直接返回 `unitsInPlay(+0x470)`（双方共用**追加序**表）；**0 调用点** | 每方 9 格 `Board`；顺序 = `UnitState.PlayOrder` | **等价**（顺序由 `PlayOrder` 承担） | `BattleManager__GetUnitsInPlayRef.c:1-7` · `AiTargets.cs:94-98` · `PlayerState.cs:206` |
| 20 | `HasScriptedPremulliganActions(TutorialStage)` | `.c:1-23` 同 16 的三条件（吃形参 stage） | 无谓词 | **等价**（恒假） | `BattleManager__HasScriptedPremulliganActions.c:1-23` · ILSpy `:36055` |
| 21 | `IsPremulliganScriptAvailable()` | `.c:1-34` 同 16 的三条件（内部取 `GetCurrentTutorialStage`） | 无谓词 | **等价**（恒假；连带 19 条动作原版也不跑） | `BattleManager__IsPremulliganScriptAvailable.c:1-34` |
| 22 | `IsThereDoubleOathEffect(bool)` | **返回 int 计数**：同方 `unitsInPlay` 里带 `oathDouble(0x4fd)` 的**张数**（不筛 cardType）；两调用点都当「再结算几次」，`while(count>i)` 之后还走一次基线结算 ⇒ **总次数 = count+1** | `RuleCore.OathExtraReplays`（同式）+ `EffectResolver.ResolveOathAbility` 的 `for (i=0; i<=replays; i++)` | **等价**（微差：原版每圈重算 count，我们算一次） | `BattleManager__IsThereDoubleOathEffect.c:1-42` · ILSpy `:46077` · `AbilityLogic__PlayAbility.c:2044-2046` · `CardScript__ResolveActiveAbilityPlayed.c:64-80` · `RuleCore.cs:11493`（`OathExtraReplays`）· `EffectResolver.cs:8977` |
| 23 | `WaitOpponentLastActions()` | 建 `<>f__AnonymousType7<Int32Enum>` 闭包并返回 = **协程 kickoff**；真体 `_WaitOpponentLastActions_d__316__MoveNext.c` | `NetBattle` 动作定序/重放，无同名等待口 | **判不出**（`d__316` 未读；净结果同） | `BattleManager__WaitOpponentLastActions.c:1-21` |
| 24 | `CardScript.CardWaitingToBePlayed()` | `cardState(+0x228)=4`（一句）；**2 个调用点都在 `BattleManager.Update` 的动作队列**（`:535/:792`） | 无 `cardState` 状态机（瞬态由事件时序表达） | **等价**（纯瞬态表现态） | `CardScript__CardWaitingToBePlayed.c:1-8` · `BattleManager__Update.c:535, 792` · 枚举 `CardStateOptions.cs` |
| 25 | `GetHighestAttackValue()` | `max(CurrentRangeAttack, CurrentMeleeAttack)` | `AiTargets.HighestAttack`（同一式；在场取运行时值、手牌取卡面值） | **等价**（逐句 2/2） | `CardScript__GetHighestAttackValue.c:1-17` · `AiTargets.cs:80-86` |
| 26 | `GetOwnHero()` | `battleManager.GetHero(isPlayer(+0x40))` 纯转发；**0 调用点** | `PlayerState.Warlord` | **等价** | `CardScript__GetOwnHero.c:1-12` |
| 27 | `HasDiedSinceLastTurn()` | **逐卡** `turnDied(+0x250) == turnCounter` **或** `+1 == turnCounter`；唯一调用点 `FilterMethods.MeetsConditionCriteria` | 真等价物 = `DeadUnits.DeathTurn`（`DeathTurn == Turn-1/Turn`）；行内落点写的 `DiedThisTurn/DiedLastTurn` 是**全局计数**（另一个量） | **有差异（落点错）** / 行为等价 | `CardScript__HasDiedSinceLastTurn.c:1-55` · `ConditionEval.cs:608` · `EffectResolver.cs:11157` |
| 28 | `HasDiedThisTurn()` | 逐卡 `turnDied == turnCounter` | 真等价物 = `DeadUnits.DeathTurn`（`ConditionEval.cs:319-325`）；**不是** `ctx.DiedThisTurn` | **有差异（落点错）** / 行为等价 | `CardScript__HasDiedThisTurn.c:1-38` |
| 29 | `IsSameTrait(CardEffect, CardEffect)` | 比 `+0x3c/+0x78/+0x7c` 三格 + `SameTimingAsEffect`；**唯一调用点 = `CardScript__AddEffect.c`** | `StatMod` **从不合并**（等价恒 `doNotStack`）⇒ 原版 1 条记录+值叠加、我们 2 条记录+投影相同 | **有差异（条目粒度）**，今天不可观测 ⇒ B + 已知偏离 | `CardScript__IsSameTrait.c:1-22` · `CardScript__AddEffect.c`（`SameTimingAsEffect` 的另一调用者） |
| 30 | `IsTheSameTurnPlayed()` | `turnPlayed(+0x30c) == turnCounter`；写点 `ResolveCardPlayed:55`（每次打出重写）；5 个调用点 | `UnitState.DeployedTurn`（两个部署入口写 `ctx.Turn`） | **等价**（残留：原版另有 `playedIntoFieldTurnCounter(+0x2E0)`，我们一维、无消费者） | `CardScript__IsTheSameTurnPlayed.c:1-37` · `UnitState.cs:771` · `RuleCore.CanUseOathAbility` |
| 31 | `SetAsPlayer(bool, CardArmy, bool)` | 写 `isPlayer(+0x40)`；**`setCardback` 只 `LogWarning("…disabled for now")`**；`!isPlayer⇒return`；`cardState∈{9,4}` 或（非 inHand ∧ Tactic ∧ spellType∈{Trap(70),SecretOrder(60)} ∧ `!armedTrap(+0x54)`）⇒ **重挂 `BattleCardUI` + 改名**（名+`rawCard.name`+名+`uniqueInGameID`）；**唯一调用点 = 两个偷牌协程** | 引擎侧 owner/座位 + 表现层 `mine = owner == _me`；偷牌未实现（判 C） | **等价**（今天不可达） | `CardScript__SetAsPlayer.c:1-74` · ILSpy `:6735-6790` · `BattleManager._ResolveStealFromDeck/_FromHand` |
| 32 | `SetFutureRemnant(bool)` | **是属性 setter** `EntityScript.set_isRemnant`（写 `+0x65`）；真实写点是**直写字段**：`TransformIntoRemnant.c:26`=1、`TransformFromRemnant.c:13` 清 0 | `UnitState.IsRemnant`（只在新建残骸时置真；翻回换对象） | **有差异（结构：无可变位）** / 净等价 | `CardScript__SetFutureRemnant.c:1-8` · `UnitState.cs:410` · `RuleCore.CleanupDeaths` |
| 33 | `SetLeavingRemnant()` | 写 `+0x66=1`；**唯一调用点 = `BattleManager.AddTransformFromRemnant`**；清零点 `TransformFromRemnant.c:13` 一次清两字节 | `UnitState.IsLeavingRemnant` **恒 false**（无写点） | **有差异（恒 false）** / 净等价 | `CardScript__SetLeavingRemnant.c:1-8` · `UnitState.cs:425` · 读点 `CounterAttack.cs:85/133` |
| 34 | `SetPray(bool)` | **是属性 setter** `EntityScript.set_isPraying`（写 `+0x67`）；🔴 **原版有清零点（直写字段）**：`OnTurnStart.c:100`（`BroadcastTurnStart` 对**全场每张 + 双方手牌**逐张调 ⇒ 每次回合开始清全场）+ `ResolveDamageDealt.c:168`（自伤，前有 `damageAbsorbed`/`damageValue==0` 两道早退） | `UnitState.Prayed`：置真 2 处、清零 4 处（回合开始扫双方全场 · 疲劳自伤 · `!absorbed && dmg != 0` · 状态复位） | **等价**（🔶「我们自己补的」标签**不成立**） | `CardScript__SetPray.c:1-8` · `CardScript__OnTurnStart.c:100` · `CardScript__ResolveDamageDealt.c:166-168` · `BattleManagerSupport__BroadcastTurnStart.c:20-51` · `RuleCore.cs` 的 `BeginTurn` 清 `Prayed` 那一圈 / `ApplyDamage` / `MoveDeckTopToHand` |

---

## ③ 没查清的部分（还差什么）

1. **`WaitOpponentLastActions` 的 `d__316` 真体没读** —— 它到底等哪几条动作未知 ⇒ 「我们的定序是否覆盖它等的那几条」判不了。下一步：读 `BattleManager._WaitOpponentLastActions_d__316__MoveNext.c`（100 行，已存在）。
2. **`IsSameTrait` 的 `+0x78/+0x7c` 字段语义**（反编译未解名）。不影响结论（等式两侧取同一字段），但若将来要做「合并」就必须先解名。
3. **`GetAllCardsInPlay` 的 `includeDying`**：我们没有 `cardState==5` 的**可枚举**态（只有 `ctx.IsPendingDeath(u)`）。今天 0 调用点 ⇒ 不影响。
4. **手牌/牌库/墓地三类查询的「侧极性」只抽核了墓地一支**（`EffectResolver.ReturnFromCemetery`）；手牌/牌库那两支靠 `EffectTargetSpec` 的 side 语义，**没逐条对 `criteria.includeMinions/…` 与 `friendlyUnits/enemyUnits` 的映射**。
5. **112 条里 58 条（①）是「按账内措辞」归类的**，只有约 40 条读了全文 ⇒ ①/② 的边界可能有个位数误判。
6. `SetAsPlayer` 的 `armedTrap(+0x54)` 是按 ILSpy 命名认的（`.c` 只见到 `*(char*)(param_1+0x54)==0`）；`rename` 的实际用途未在运行时验。

---

## ④ 改动清单

| 文件 | 什么 |
|---|---|
| `d:/4/Unity/资料/完全复刻/账/_parts/B9_J1SB.tsv` | 34 行补丁（11 列；只改「差异」「备注」；`状态` 全 `B`、`等级`/`轮次`/`落点`/`判据` 逐字未动 —— 脚本对主表逐列校验 0 处变更；LF、无 BOM、表头与主表逐字节相同） |
| `d:/4/Unity/资料/完全复刻/报告/B9_J1SB_结构B真核.md` | 本报告 |

⛔ 未改：任何 `.cs` · 主账表 · `_merged_*` · 正本（`CLAUDE.md`/`项目任务.md`/`任务总表.md`）· git。

---

## ⑤ 顺手发现（只报不改）

1. 🔴 **`SetPray` 这一行的旧结论是反的，而且已经写进了代码注释链**：账里原写「原版 `set_isPraying(…,0)` 全树 0 命中 ⇒ 我们按卡面语义补了 4 处清零 ⇒ 🔶 自己设计」。**实际原版有清零点**（`CardScript__OnTurnStart.c:100` + `CardScript__ResolveDamageDealt.c:168`），我们那 4 处与之一致 ⇒ 那是**复刻**、不是自设计。⇒ ① 账已在本批订正；② 若有人在那两处 `.cs` 上打了 `🔶 自己设计`，**要撤**（`RuleCore` 的 `BeginTurn` 清 `Prayed` 那一圈 / `ApplyDamage` 的 `!absorbed && dmg != 0`）。
2. 🔴 **「属性 setter 的 0 调用点」是假否定**：`SetFutureRemnant` / `SetPray` / `SetLeavingRemnant` 三个都是 setter（`set_isRemnant` / `set_isPraying` …），调用点统计对它们**无效** —— 真实写点是**直写字段**。判法：按 `+0x65 / +0x66 / +0x67` 搜赋值，⛔ 别搜方法名。这与 `CLAUDE.md` 那条「私有 setter 反编译成 `obj = value`、字段名丢了」是同一族。
3. 🟡 **两处文档互相矛盾（铁律 5）**：`RuleEngine/Core/UnitState.cs` 的 `IsLeavingRemnant` doc 写「原版 `+0x66` 在哪一刻置 1 **没查清**」，而账行 900 早已查实（置点 = `CardScript.SetLeavingRemnant` ← `BattleManager.AddTransformFromRemnant`；清零点 = `TransformFromRemnant.c:13`）。⇒ 建议派写手就地改那条注释（**只改注释、零行数变化**）。
4. 🔴 **四条「我们的落点」指错了对象**（我只能改「差异/备注」两列，落点列请调度台定）：`GetTotalMana`（`Energy`→`MaxEnergy`）· `HasDiedThisTurn` / `HasDiedSinceLastTurn`（`DiedThisTurn/DiedLastTurn`→`DeadUnits.DeathTurn`）· `AlreadyPlayedCardThisTurn`（`CardsPlayedInTurn` 是另一个方法 +0x3D0）· `GetCurrentPlayerId`（`BattleContext.Active` 与「playerId 字符串」不是一件事，建议撤落点并改判「等价（日志专用）」）。
5. 🔴 **`IsThereDoubleOathEffect` 的「判据要点」要订正**：它不是「≥ 阈值 ⇒ 真」，而是**返回同方带 `oathDouble` 的张数**，两个调用点拿它当「再结算几次」。⚠️ 微差：原版**每圈重算** count，我们只算一次 —— 若某张誓约能力会打死带 `oathDouble` 的单位，圈数会不同（可构造，但今天无此卡组合已核？**未核**）。
6. 🔴 **新的 C 级缺口候选（不在 34 条内）**：`CardScript.ResolveWeaponKilled` **我们没实现**（全仓 grep `WeaponKilled` = 0）—— 原版它是 `GetOriginalWarlord` 的唯一调用者，语义 = 「武器被摧毁 → 从**原始督军**的武器列表取第 0 张 → 建 `transformUnit(27)` 动作」。⇒ 建议立账（注意它同时是 §八 #3「动作级 `transformUnit(27)` 与效果级 become 是不是同一件事」的那条线）。
7. 🟢 **两个死形参**：`inclWeapons`（`GetCardsInCemetery` / `GetCardsInDeck` / `GetCardsInHandList` / `AddCardsInHandToList` / `IsCardIncluded`）与 `inclHeroParts`（`IsCardIncluded`）**在原版体内一次都没读**（`undefined8` 形参）⇒ 这三个查询的判据以后别再为「weapons 怎么筛」花力气。
8. 🟢 **`GetCardsInHandList` 的尾部是 `Where(≠null).OrderBy(c => c.uniqueInGameID).ToList()`**（`+0x22c` = `uniqueInGameID`），**不是「先我方后敌方」的顺序**；「先当前行动方那侧」只是**收集**顺序，排序会把它抹掉。将来若做「跨两手牌取第一张」必须照 `uniqueInGameID`。
9. 🟡 **`GetAllCardsInPlay` 排除 Hero(10)**：它的名字像「全场所有卡」，实际**不含督军、不含战术/低语**。谁拿它当「全场」用就会漏督军。
10. 🔴 **系统性问题 X1（跨多行）**：多条广播行都记着「我们的棋盘跳带**待死闸**，原版这一跳没有 `IsInPlay` 过滤」（645/647/655/660/663/668 等）⇒ 这不是 6 个独立缺陷，**是同一处形状**，应在 `BroadcastWhen` 的棋盘跳一处统一核 / 一处改。
11. 🟡 **系统性问题 X2**：原版多处 `Signal.Raise(...)`（Scaffold 内部事件总线）我们无对应物（639/660 记着）⇒ 要么统一判「无消费者、等价」，要么统一补 —— 逐行判会重复劳动。
12. 🔴 **`RuleEngine/Core/RuleCore.cs` 正被别的写手改**：我 21:30 读到的行号与 21:48 的行号已相差约 **+50 行**（`Prayed` 清零从 `:8726` 挪到 `:8779`）⇒ 本报告的 `.cs:行号` 全是「现读」，**引用时以符号名为准**；账里按行号钉的落点会继续漂。
13. 🟡 **账行 1367 `WarlordMasteryContainer.OnInitialize` 自相矛盾**：`状态 = B`，但「我们的落点」列写着「（归 C）」、差异列空。
14. 🟡 **`PlayerHand.GetWhispersCardsCount`（账行 1169）** 又是一例「反编译产物文件名与内容不符」，且它是死代码 ⇒ 判据可能永久取不到；建议直接按「原版出货数据 0 入口 + 判据不可读」判 D（时间限定）。
15. 🟢 **`CardScript.ResolveActiveAbilityPlayed` 的残差**：`for` 里原版**每圈重算** `IsThereDoubleOathEffect`，我们只算一次（见 5）；另外原版这句还会写 `+0x50`（誓约次数）与 `+0x4c`（用过的标记），我们那两位在 `OathUsesThisTurn` —— 已对齐，但**注意别把它和 `ActivateMinion` 的 `+0x230/+0x23C` 混**（那是行动/攻击位）。
