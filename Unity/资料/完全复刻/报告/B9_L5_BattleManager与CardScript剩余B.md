# B9 · L5 · `BattleManager` 与 `CardScript` 剩余 B（253 条）归类

> **快照** 2026-10-26 · 账 `资料/完全复刻/账/战斗逻辑驱动.tsv`（现读 1,374 物理行）
> **范围** = 这两个类的 **253 条 B**（`BattleManager` 181 + `CardScript` 72）。
> ⛔ **不含** `BattleManagerSupport`（20 条）与 `RawCardScript`（20 条）—— 那两个类归别的代理，切片用 `--class` 子串会带出来，已剔除。
> **产出补丁** = `账/_parts/B9_L5.tsv`（**30 行**，`check_patch_conflicts.py` 已过：无跨补丁重复键）。

---

## 〇 一句话结论

| 项 | 数 | 说明 |
|---|---:|---|
| **能升 A** | **0** | 不是没找 —— 三类硬阻塞，见 §二 |
| **应改判 C**（我们没实现） | **30** | 本批**最高价值**，直接进引擎施工单，见 §三 |
| **应判 D / 域外** | **13** | 原版死代码 / 恒假 / 纯表现层，见 §四 |
| **有已知行为差（须先改码）** | **81** | 见 §五 |
| **结构上不能升 A** | **34** | 落点只能指字段/属性 ⇒ 按方案 §4.1 永远记 B，见 §六 |
| **只差判据要点/观测量** | **92** | 其中 **19 条判据要点仍是占位**（两个类合计 **30** 条），见 §二(c) |
| **落点仍无方法级** | **3** | 见 §二(d) |

> 与 Q5 路线图（`判据/B9_剩余B_路线图.md` 的 B1–B7）的对应：B1↔92 · B2+B3↔3 · B4↔81 · B6↔30 · B7↔13（其中「纯表现层移交」10 + 「判 D」3）· **本表新增的 H 档 = 34 条（Q5 把这类算进 B3「字段落点」，做法是「改到方法级」；实测这批改不出来 ⇒ 单列）**。

---

## 一 总表（253 条全部归类）

| # | 离 A 还差什么 | 条数 | 典型方法（各 3 个） |
|---:|---|---:|---|
| 1 | **只差判据要点/观测量**（零代码，记账即可） | **92** | `GetUnitsInPlayList` · `AddTriggerCodex` · `ChoiceOfCardPlayer` |
| 2 | **落点仍是类级/共享/无方法级**（补行号段） | **3** | `ResolveTriggerRegiment` · `ResolveTriggerTeleport` · `ResolveTriggerMob` |
| 3 | **有已知行为差**（须先改码） | **81** | `CheckAndApplyCurrentPack` · `ResolveUnitAttacked` · `FinishAfterAttack` |
| 4 | **应判 C**（我们没实现 / 缺机制） | **30** | `AddTriggerCourtesy` · `ResolveStealFromDeck` · `IsAgainstAI` |
| 5 | **应判 D / 域外**（原版死·恒假·纯表现） | **13** | `AllUnitsInPlay` · `SetTutorialStopState` · `IsPlayingCardState` |
| 6 | **结构上不能升 A**（落点只能指字段/属性/注释） | **34** | `GetHero` · `SetPray` · `GetDeck` |

> 全量清单（253 条逐条落到某一档）在 **§九**。

---

## 二 为什么「0 条能升 A」（这比「升了几条」重要）

### (a) 5 条已经全绿、仍只能 B —— 落点是字段/属性（`调度台 2026-10-26` 已 A→B）

| 方法 | 判据 | 落点为何不能当 A |
|---|---|---|
| `GetOwnHero()` | `decomp_full/CardScript__GetOwnHero.c:1-13` | 落点 `PlayerState.cs:72`（字段 `Warlord`）|
| `IsTheSameTurnPlayed()` | `CardScript__IsTheSameTurnPlayed.c:1-37` | 落点 `UnitState.cs:771`（字段 `DeployedTurn`）|
| `SetFutureRemnant(bool)` | `CardScript__SetFutureRemnant.c:1-8` | 落点 `UnitState.cs:410`（字段 `IsRemnant`）|
| `SetLeavingRemnant()` | `CardScript__SetLeavingRemnant.c:1-8` | 落点 `UnitState.cs:425`（字段）|
| `SetPray(bool)` | `CardScript__SetPray.c:1-8` | 落点 `UnitState.cs:351`（字段 `Prayed`）|

这 5 条 G0–G8 全 ✓（含两侧观测量），**唯一缺口就是落点形状**。同族的 `TransformIntoRemnant()` 已 7 格绿，但它还多一条真差异（`_keywords` 合并字典 vs 原版分层），所以连「落点」都不是唯一缺口。

### (b) 4 条「逐句对过、未发现差异」，但各差**一样**

| 方法 | 差的是什么 |
|---|---|
| `CheckIfAbilityCancelsAttack` | 原版这一行只是**一句转调**（我本轮读全 65 行被调方后订正的口径）；升 A 还差 1 个 hop：`Enchantments.FindCancelAttack` 与 `CardAbility.CanTriggerAbility(0x118)` 未逐句对 |
| `GetAvailableAttackTargets` | 枚举顺序：原版倒序、我们正序（可打集相同，**但返回表顺序是行为**）⇒ 有已知差 |
| `BroadcastMarkerlightResolved` | `.c` 把实参丢了 ⇒ 排除集合靠同族推，**判据侧不完整** |
| `ActionsLeftInQueue` | 落点 `ActionQueue.cs:209（Count）` = **属性 getter** ⇒ 按 §4.1 不得当落点 |

### (c) 92 条「只差判据要点」里，有 **19 条判据要点仍写着「（判据见 X 方法体）」**（两个类合计 **30** 条）—— 那是 G6 明令禁止的占位

本批把其中 **20 条**按 `.c` 全文补成真判据要点（清单见 §七），**还剩 30 条**（`Resolve*` 大方法为主：`ResolveAddEffect` / `ResolveCreate*` / `ResolveFollowAfterResolution` / `ResolveTriggerCodex` / `ResolveTriggerMob` / `TryUsingActiveAbility*` / `ChoiceOfCardEnemy` / `ChoiceOfCardPlayer` / `ChooseCardMethod` / `CheckIfCodexTriggered` / `GetDeathType` / `GetMulliganCard` / `LeaveBattle` / `CancelBattleActionFromQueue` / `ExecuteUseWaystone` / `GetUnitsInPlay*` 3 条 …）。

> 另有 **18 条属 `Add*` / `Resolve*` 族** —— 按 `判据/战斗逻辑驱动_B2A_口径.md` §一，这一族升 A 还要 **G3/G4 两侧观测量**（要跑 `ruleprobe`，属同步点的活，本批不做）。

### (d) 3 条落点仍无方法级（`LOC`）

`ResolveTriggerMob` / `ResolveTriggerRegiment` / `ResolveTriggerTeleport` —— 落点写成 `RuleCore.cs:6540 FireTriggerAt(Regiment)` 这种「`路径:行 裸名(实参)`」形状，**没有 `（裸名）` 括号** ⇒ `check_ledger_points --fix` 认不出裸名（重定位失效）。改成 `RuleCore.cs:<行>（FireTriggerAt）` + 在等价说明里写清「哪个关键词那一支」即可。

---

## 三 应改判 C 的 30 条（本批最高价值 · 进引擎施工单）

### ① 整条机制缺、本 build 池内 0 可达（休眠档：按铁律 11 要做，排最后）— 11 条

- **AddExchangeMinions** —— 换位/交换两个单位归属（`exchangeMinion`）—— 全仓无此函数；池内 swap/exchange 卡 0 张
- **AddTransformCardWithBuff** —— 单位变身（`transformUnit`）—— 动词表无 transform；池内 0 张
- **AddTriggerCourtesy / ResolveTriggerCourtesy** —— Courtesy 整条机制（`KeywordTable` 无此常量、无触发口）
- **AddTriggerRelentless / ResolveTriggerRelentless** —— Relentless 整条机制（同上）
- **CopyUnitInCemetery / ResolveCopyUnitInCemetery** —— 墓地复制（`copyUnitInCemetery`）—— 只有枚举名，无实现
- **ResolveStealFromDeck / ResolveStealFromHand** —— 从对手牌库/手牌偷一张 —— 全仓无此函数
- **WaitingForBlockingAction** —— 查「队列里有 transformUnit(27)」的那个查询口（休眠：transformUnit 池内 0）

### ② 单点缺口、判据已清、成本极小 — 14 条

- **IsAgainstAI / IsAgainstOnlineBot / IsReplayMatch** —— matchType 白名单谓词（2~6 行）。⚠️ 我们现在用 `BattleDriver.AiShouldDriveOpponent`（`_net == null`）近似，**口径不同**；`IsAgainstHuman` = 它的 2 行取反，一并做（该行本账仍记 B1，见 §二）
- **ResolveMissAction** —— 缺 `MissActions` 计数器（攻击落空/用能力次数那一族；我们只做了 `AttacksThisTurn`）
- **CanPlayWithOathNowInPlay** —— 「在场版」的 oath 可付判据（全仓只有手牌版与付费口，没有在场版）
- **RemoveDuplicitous** —— `duplicitous(160)` 整条关键词缺（`KeywordTable` 无该位；池内 0 张 ⇒ 今天不可观测）
- **PayCardPlayedCostOath** —— 「打出带 oath 的卡 ⇒ 付 oath 费」（用能力那条已有）
- **RegisterPlayerActionCancelled** —— 缺 `thisTurnActionCounter`（`+0x3bc`）与其 −1；⚠️ 原版这计数器只影响日志与 `shouldCountReduced`，**不影响攻击配额**
- **CancelMulliganAfterBattleEnd** —— 对局提前结束 ⇒ 收换牌（我们的换牌面板由提交/回包驱动）
- **ResolveMissAction** —— 缺 `MissActions` 计数器（攻击落空/用能力次数那一族）
- **SetHealthToZero** —— 「裸把血写 0」这个口（⛔ 不该拿 `Hurt` 顶替：它含减伤/护甲/反击）
- **GetCampaignScriptedData / IsCampaignScriptAvailable / IsCampaignStageAvailable / IsScriptedStageAvailable** —— 战役脚本链（原版**不是死代码**：调用点在异步状态机体内，`调度台` 已就地订正过「不能判 D」）

### ③ 有可达性 / 影响主链 — 6 条

- **GetHighestHealthWarlord** —— `ta = 230` 选法未实现（现池 0 张 ⇒ 今天不可观测，出现可达卡立刻升 P0）
- **GetWarcry** —— 战吼文本取值/播报（本地化键 `Chat_Messages/warcry-*` 没接）
- **CheckForHeroDeckTransform** —— 督军卡组变身（战役/教程档；池内可达性未查清）
- **AddInitialCardToDeck** —— 「把防御卡插**牌库顶**」那条路（我们只做了「进手牌」那条；现读预组牌 `defensiveCard` 恒 null ⇒ 今天没用上）
- **ChangeMaxHealth** —— 「先摘固有生命增益、再叠 extrinsic 求和、再夹当前血」整套没有；我们口径是「增减连上限一起动」

---

## 四 应判 D / 域外 13 条

| 方法 | 判据 |
|---|---|
| `AllUnitsInPlay` | `decomp_full` + `out_cpp2il_dev_ilspy` **两棵树都只有定义、0 调用点** |
| `AddTriggerPlayerCodex` | 同上（0 调用点）；Codex 的真触发口另有等价物 |
| `ReplaceHeroPower` | 0 外部调用点（换督军能力那条路本 build 不可达） |
| `ResolveRevealOrders` | `AddRevealOrders` 入队但**出队口 0 调用点**（死链）；我们按 `EvtKind.RevealOrders` 做了表现 |
| `SetTutorialStopState` | **原版空桩**（`option==true` 只打一句 warning，不停教程） |
| `AddVoiceLinesControllerListeners` | 0 调用点；它唯一写的 `isAnyPopUpOpen` 全库 6 写 0 读（死状态） |
| `HandleOfflineBattleResult` | P3：把结果交给 PlayFab/离线队列（服务器边界） |
| `IsDuringBattle / IsPlayingCardState` | 读的是 **UI 状态机**（`UIstate`）⇒ 纯表现层，按 B2A §六 不由本域覆盖 ⇒ 移交战斗界面线 |
| `SetNextCardAsDeckDrawing` | 纯表现层（「下一张抽牌」的呈现态标记） |
| `ResolveResetDuty` | 实体只是 `UpdateFigures()` 刷新数值显示 ⇒ 表现层 |
| `RemoveDestroyableAnims` | 按 `buffType ∈ {15,16}` + trait 去销毁 trait 边框特效 ⇒ 特效线 |
| `ShowPotentialTargets` | 表现层（点亮范围；调度台已裁定「只在单位卡那一支点亮」） |

> ⚠️ **判 D 的「没人引用」口径**（方案 §四）要求查 Unity 侧挂接。上面前 5 条是前任代理已查过的；`HandleOfflineBattleResult` 按 P3 边界判；其余 7 条是**域外/纯表现**，不是「原版没行为」。

---

## 五 有已知行为差 81 条（须先改码，进改码批）

这些行**账里已经写清了差在哪**（不是「未核」）：按 `RuleCore.cs` / `EffectResolver.cs` / `UnitState.cs` 为主的改码批，逐条判据在账里。

逐条清单：

- `BattleManager.ActionsLeftInQueue()`
- `BattleManager.AddRemoveFromCemetery(CardScript targetCard, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.AllowResolveAttack(BattleAction battleAction)`
- `BattleManager.BroadcastActivateMinions(bool playerTurn)`
- `BattleManager.BroadcastChangedLanguage()`
- `BattleManager.BroadcastCollectedFaith(CardScript unitCollected)`
- `BattleManager.BroadcastCollectedQuestPoints(CardScript unitCollected)`
- `BattleManager.BroadcastCollectedSpiritStone(CardScript unitCollected)`
- `BattleManager.BroadcastEarnSkull(bool isPlayer)`
- `BattleManager.BroadcastQuestResolved(CardScript questCard)`
- `BattleManager.BroadcastSacrificeResolved(CardScript unitSacrifice)`
- `BattleManager.BroadcastUnitDamaged(CardScript damagedUnit, DamageInfo damageInfo, bool dealtByAttacker)`
- `BattleManager.BroadcastUnitRegiment(CardScript unitRegiment, CardScript unitRegimentCaster)`
- `BattleManager.BroadcastUnitSpiritStone(CardScript unitSpiritStone)`
- `BattleManager.BroadcastUnitSynapse(CardScript synapseUnit)`
- `BattleManager.CheckIfAbilityCancelsAttack(CardScript actingCard, CardScript targetCard)`
- `BattleManager.CheckUnitsCancellingAttack(CardScript actingCard, CardScript targetCard)`
- `BattleManager.DealMultiDamageAndDestroySelf(DamageInfo damageInfo, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.DealMultiDamageVariable(List<DamageInfo> damageInfoList, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.DisplayHurryUpChatMessage()`
- `BattleManager.EndTurnClick(bool timeOutFlag)`
- `BattleManager.ExecuteNextAction(List<PlayerAction> actionsLog, ref int actionsIndex, bool isPlayer)`
- `BattleManager.GetAvailableActions(bool isPlayer, bool allowAutoActions)`
- `BattleManager.GetAvailableAttackActions(CardScript actingCard, bool allowAutoActions)`
- `BattleManager.GetAvailableAttackTargets(CardScript attacker)`
- `BattleManager.GetChoiceIndexOfCard(CardScript actingCard, CardScript targetCard, RawCardScript chosenCard)`
- `BattleManager.GetViewportPointFromBoardPos(Vector3 worldPos)`
- `BattleManager.LeaveBattle()`
- `BattleManager.NeedToChooseMinionTarget(CardScript actingCard)`
- `BattleManager.RecordAttackPendingDamage(CardScript actingCard, CardScript attackedCard, AttackTypes attackType, int actionPriority, bool recordReturnDamage)`
- `BattleManager.ResolveAction(BattleAction battleAction)`
- `BattleManager.ResolveAddHandEffect(BattleAction battleAction)`
- `BattleManager.ResolveChangeBaseAttack(CardScript targetCard, int newAttack)`
- `BattleManager.ResolveChangeMaxHealth(CardScript targetCard, int newHealth)`
- `BattleManager.ResolveClearEndOfTurnEffectsFrom(CardScript actingCard)`
- `BattleManager.ResolveDiscardFromHand(BattleAction battleAction)`
- `BattleManager.ResolveExchangeMinion(BattleAction battleAction)`
- `BattleManager.ResolveFatigue(bool playerDrawing)`
- `BattleManager.ResolveMinionDeath(BattleAction battleAction)`
- `BattleManager.ResolvePlayActiveAbility(BattleAction battleAction)`
- `BattleManager.ResolveRemoveEffectsFrom(CardScript actingCard)`
- `BattleManager.ResolveResetActions(CardScript target)`
- `BattleManager.ResolveTransformFromRemnant(BattleAction battleAction)`
- `BattleManager.ResolveTransformUnit(BattleAction battleAction)`
- `BattleManager.SetUnitOrderInPlay(CardScript card)`
- `BattleManager.ShuffleDeck(bool isPlayer)`
- `CardScript.ChangeBaseAttack(int newAttack)`
- `CardScript.CheckAndApplyCurrentPack()`
- `CardScript.CheckReplacedAbility()`
- `CardScript.ExecuteSynapeEffect(CardScript actingCard)`
- `CardScript.ForceChangeMaxHealth(int newHealth)`
- `CardScript.HasPackBuffTrait()`
- `CardScript.IsValidActiveAbilityTarget(CardScript target)`
- `CardScript.NeedsToChooseFromPool(CardScript targetCard)`
- `CardScript.NonTargetSpellPlayed()`
- `CardScript.ReactToUnitHealed(CardScript healedUnit, CardScript actingUnit)`
- `CardScript.ReactToUnitJammed(CardScript disenchantedCard)`
- `CardScript.RemoveFromGameCard(CardScript removedCard)`
- `CardScript.RemoveTraitSilently(DefinedTrait traitToRemove)`
- `CardScript.ResetToValuesInHand(bool resetTriggeredBacklash)`
- `CardScript.ResolveDamageDealt(CardScript damagedCard, DamageInfo damageInfo)`
- `CardScript.ResolveTransient()`
- `CardScript.ResolveUnitAttacked(CardScript actingCard, CardScript targetCard, AttackTypes attackType)`
- `CardScript.ResolveUnitSummoned(CardScript cardPlayed, CardScript targetCard)`
- `CardScript.SendClearEndOfTurnEffects(int priority)`
- `CardScript.SendRemoveEffect(CardEffect cardEffect, int priority)`
- `CardScript.SetStateAsMulligan(bool toBeDiscarded)`
- `CardScript.SetupActiveAbility()`
- `CardScript.SetupAttack(bool playSound, bool playerInitiated)`
- `CardScript.ShouldTriggerCompanion()`
- `CardScript.ShowDropPodFigures()`
- `CardScript.TacticPlayed(CardScript actingCard, CardScript targetCard)`
- `CardScript.TargetedSpellPlayed(CardScript actingCard, CardScript targetCard)`
- `CardScript.TransformIntoRemnant()`
- `CardScript.TriggerCodex(CardScript targetCard)`
- `CardScript.TriggerOtherCardAbility(RawCardScript abilityCard)`
- `CardScript.TriggerOtherCardRequiem(CardScript requiemUnit)`
- `CardScript.TriggerSpiritStone(CardScript actingCard)`
- `CardScript.TriggeredEarnSkull(CardScript earnedWarlord)`
- `CardScript.UsedActiveAbility()`
- `CardScript.hasMaintenanceEffects()`

> ⚠️ 其中 **21 条今天 0 可达**（`池内 0 张` / `0 入口`）—— 按铁律 11 仍要做，排在可达的 60 条之后。

---

## 六 结构上不能升 A 的 34 条（按方案 §4.1 永远记 B）

共同点：**落点只能指到字段 / 只读属性 / 注释**（`PlayerState.Hand/Deck/Discard/Board/Warlord/Energy`、`UnitState.Prayed/IsRemnant/DeployedTurn`、`AiTargets.HighestAttack` 属性、`Aura.cs` 注释…），而 §4.1 明令「纯属性 getter 不得当方法落点」、`调度台 2026-10-26` 也已就此把 5 条 A→B。

**这一档不该再派「升 A」的活** —— 要么承认它是 B 的终点（记 D 也不对：原版有行为），要么等将来给这些字段包一层方法再回填。

- `BattleManager.AlreadyPlayedCardThisTurn(bool isPlayer)`
- `BattleManager.GetAllCardsInHand()`
- `BattleManager.GetAllCardsInPlay(bool includeDying)`
- `BattleManager.GetCardsInCemetery(bool inclMinions, bool inclSpells, bool inclWhispers, bool inclWeapons, bool inclPlayer, bool inclEnemy)`
- `BattleManager.GetCardsInDeck(bool inclMinions, bool inclSpells, bool inclWeapons, bool inclPlayer, bool inclEnemy)`
- `BattleManager.GetCardsInHandList(bool inclMinions, bool inclSpells, bool inclWhispers, bool inclWeapons, bool inclPlayer, bool inclEnemy)`
- `BattleManager.GetCardsInHandRef(bool isPlayer)`
- `BattleManager.GetCurrentPlayerId(bool isPlayerTurn)`
- `BattleManager.GetDeck(bool playerHeroY)`
- `BattleManager.GetEnemyHandRef()`
- `BattleManager.GetEnemyMinionsAndRemnantInPlay()`
- `BattleManager.GetFriendlyHandRef()`
- `BattleManager.GetHero(bool playerHeroY)`
- `BattleManager.GetOriginalWarlord(bool playerHeroY)`
- `BattleManager.GetPlayerMinionsAndRemnantInPlay()`
- `BattleManager.GetPremulliganScriptedData()`
- `BattleManager.GetStartingMana(bool isPlayer)`
- `BattleManager.GetTotalMana(bool isPlayer)`
- `BattleManager.GetUnitsInPlayRef()`
- `BattleManager.HasScriptedPremulliganActions(TutorialStage tutorialStage)`
- `BattleManager.IsPremulliganScriptAvailable()`
- `BattleManager.IsThereDoubleOathEffect(bool isPlayer)`
- `BattleManager.WaitOpponentLastActions()`
- `CardScript.CardWaitingToBePlayed()`
- `CardScript.GetHighestAttackValue()`
- `CardScript.GetOwnHero()`
- `CardScript.HasDiedSinceLastTurn()`
- `CardScript.HasDiedThisTurn()`
- `CardScript.IsSameTrait(CardEffect effect1, CardEffect effect2)`
- `CardScript.IsTheSameTurnPlayed()`
- `CardScript.SetAsPlayer(bool isPlayer, CardArmy army, bool setCardback)`
- `CardScript.SetFutureRemnant(bool willBeRemnant)`
- `CardScript.SetLeavingRemnant()`
- `CardScript.SetPray(bool activate)`

---

## 七 这一轮实改的 30 行（`账/_parts/B9_L5.tsv`）

| 类 | 改了什么 | 行数 |
|---|---|---:|---|
| 判据订正（铁律 5） | `CheckIfAbilityCancelsAttack`（原版这句只是转调，真体在 `CardScript__HasCancelAttackEffect`）· `GetTraitEffect`（原版一句转调 `GetTraitEffectObject`）· `IsAgainstHuman`（`return !IsAgainstAI()`）· `TriggerOtherCardAbility`（落点挂错：不是 trigger 15） | 4 |
| 断链落点 | `IsThereValidAbilityTarget` / `IsThereValidSpellTarget` / `IsValidSpellTarget` —— 原落点 `RuleCore.CanUseAbilityTarget` **这个方法已不存在**（全仓只剩 `<see cref>`），改到 `ResolveAbilityTargetOwner` / `IsLegalPlayTarget` | 3 |
| 判据要点占位 → 实写 | 20 条（`GetAllCardsInHand` · `GetCardsInHandRef` · `GetEnemyHandRef` · `GetFriendlyHandRef` · `GetHero` · `GetDeck` · `GetUnitsInPlayRef` · `IsClosedDeckGame` · `ShouldActivatInitialEnchantments` · `StartBattlePhase` · `PlayAi` · `GetRandomAttackTarget` · `ShuffleDeck` · `IsThereValid*` 2 · `IsValidSpellTarget` · `GetTraitEffect` · `IsAgainstHuman`） | 20 |
| 行号重钉 | `BroadcastActivateMinions` 3493 · `BroadcastEarnedSkull` 15613 · `BroadcastMarkerlightResolved` 10779 · `BroadcastQuestResolved` 12930 · `BroadcastUnitDamaged` 9100 · `GetLowestHealthUnit` **1382**（原记 1798，漂 416 行）· `TriggerCodex` 12923 · `ReadyToUseActiveAbility` 10854 · `CanPlayCard` 3235 · `CanUseActiveAbility` 10908 · `ShouldActivatInitialEnchantments` 7990 · `PlayAi`（→`BattleDriver.cs:18302`） | 12 |

> 有重叠（判据要点与行号同一条里一起改），去重后 **30 行**。全部保持 **B**（没有一条够格 A）。

### 顺带查实的三个「非平凡」发现

1. 🤖 **协程 kickoff 两条**：`StartBattlePhase` / `PlayAi` 的 `BattleManager.cs` 体只是 `<>f__AnonymousType7` + 状态机 kickoff，真体在 `_StartBattlePhase_d__334__MoveNext.c` / `_PlayAi_d__399__MoveNext.c`。⛔ 只读 `BattleManager.cs` 会误判「没逻辑」—— 已在账里写死。
2. 🔴 **`RuleCore.CanUseAbilityTarget` 不存在**：账里 3 条落点指着它（`RuleCore.cs:11055`），而全仓只有 `<see cref="CanUseAbilityTarget"/>` 的注释引用，声明已随改名消失 ⇒ `--fix` 永远修不好这类落点（名字对不上）。**建议**：全表 grep 一遍「落点裸名在当前源码里找得到声明」再合并。
3. 🔴 **行号漂得比预期狠**：今天 `RuleCore.cs` / `EffectResolver.cs` 被多路写手改过，抽查 12 处方法级落点，**11 处已漂**（最大 416 行 = `GetLowestHealthUnit`）。⇒ **落点括号里的裸名是唯一保险**；这批 30 行现在都带裸名，将来 `--fix` 能自愈。

---

## 八 没查清 / 下一轮该做什么（按收益排序）

1. **补完 30 条占位判据要点**（本批做了 20 条）—— 全是 `Resolve*` 大方法，读 `.c` 就能补，零代码。
2. **`GetRandomAttackTarget` 判死/判活**：本批查实**三方 0 调用点**（`decomp_full` / `out_cpp2il_dev_ilspy` / `assets_full` 的 json 串搜）⇒ 疑死代码；⚠️ **724 条无方法体没扫**，扫完仍 0 ⇒ 判 D（时间限定）；若可达 ⇒ 判 C（我们没有「随机挑攻击目标」这条路，且抽签形状也不同：预烧数组+下标 vs `ctx.Rng`）。
3. **`AddTransformCardWithBuff` 与 `ResolveTransformUnit` 的口径冲突**：前者说「transformUnit 机制未实现」，后者却已落到 `DoBecome`（逐段对过 5 跳）。**没查清**的是「动作级 `transformUnit(27)`」与「效果级 become」是不是同一件事 —— 决定 `AddTransformCardWithBuff` 判 C 还是判 B。
4. **`Enchantments.FindCancelAttack` ⇄ `CardAbility.CanTriggerAbility(0x118)`**：`CheckIfAbilityCancelsAttack` 升 A 的最后一个 hop。
5. **120 条落点格式不合规**（首括号不是裸声明名 / 没有括号）。⚠️ **不能机械改** —— 我实测「往上找最近的声明」这种启发式在这些行上**多数给错答案**（`CheckIfAbilityCancelsAttack:7776` 会指到 `OpenDropPod`、`SetAsPlayer:537` 会指到 `ExtraTriggerDepth`），正是 `CLAUDE.md` 点名的坑。要逐条按符号+语义确认。

---

## 九 全量归类清单（253 条）

### 只差判据要点/观测量（零代码，记账即可） — 92 条

- `BattleManager.AddCardToDeck(CardScript card, List<CardScript> deck, int posInDeck)`
- `BattleManager.AddResetDuty(CardScript resetCard, bool targetPlayer, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.AddSummonUnit(RawCardScript summonedCard, int summonPos, bool isPlayer, int priority, bool checkIfCasterInPlay, CardScript casterCard, CardEffect cardEffect, bool blockPlayerActions, RawCardScript enchantment)`
- `BattleManager.AddTriggerCodex(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)`
- `BattleManager.ApplyEnvEffect(RawCardScript card, bool isPlayer)`
- `BattleManager.BroadcastMarkerlightResolved(CardScript unitMarked)`
- `BattleManager.CanPlayCard(CardScript card, bool showTips, bool initiateAction, bool actionAlreadyStarted)`
- `BattleManager.CanUseActiveAbility(CardScript parentCard, bool isPlayerAsking, bool showTips)`
- `BattleManager.CancelActionStates()`
- `BattleManager.CancelBattleActionFromQueue(List<BattleAction> queue, int actionIndex)`
- `BattleManager.CancelEffectsFromDisappearingUnit(CardScript disappearingUnit, CardScript targetCard, bool shouldIgnoreDestroy)`
- `BattleManager.CancelEffectsFromDisappearingUnitFromQueue(CardScript jammedUnit, CardScript targetCard, List<BattleAction> queue, bool shouldIgnoreDestroy)`
- `BattleManager.CheckIfCodexTriggered(CardScript card, int manaLeft, CardScript targetCard, bool shouldCheckCost)`
- `BattleManager.ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, int selectedCard)`
- `BattleManager.ChoiceOfCardPlayer(CardScript actingCard, CardScript targetCard, int actionValue)`
- `BattleManager.ChooseCardMethod(BattleAction battleAction, Action<int> callback)`
- `BattleManager.DeviceVariables()`
- `BattleManager.ExecuteCampaignScriptedTurn(List<ScriptedActionCampaignData> turnScriptedData, Action onComplete)`
- `BattleManager.ExecuteUseWaystone(CardScript actingCard)`
- `BattleManager.GetAdjacentUnitRight(CardScript referenceCard)`
- `BattleManager.GetCardsDeckPlayer(bool isPlayer)`
- `BattleManager.GetCardsHandPlayer(bool isPlayer)`
- `BattleManager.GetCardsPlayPlayer(bool isPlayer)`
- `BattleManager.GetCurrentTurnScriptedData()`
- `BattleManager.GetDeathType(BattleAction battleAction, DamageInfo damageInfo)`
- `BattleManager.GetDeckManager(bool playerHeroY)`
- `BattleManager.GetLowestHealthUnit(bool isPlayer)`
- `BattleManager.GetMinionManager(bool playerY)`
- `BattleManager.GetMulliganCard(bool isPlayer)`
- `BattleManager.GetNextCardsInDeck(bool isPlayer, int numCards)`
- `BattleManager.GetPlayerHand(bool playerY)`
- `BattleManager.GetPlayerManager(bool playerY)`
- `BattleManager.GetRandomAttackTarget(CardScript attacker)`
- `BattleManager.GetRawcardScript(TargetsAffected targetsAffected, List<CardScript> cardScriptList, List<RawCardScript> rawCardScriptList)`
- `BattleManager.GetTargetInPos(Vector3 worldSpaceMousePosition)`
- `BattleManager.GetTraitEffect(DefinedTrait trait, CardTrait.EffectTriggerType trigger, CardArmy cardArmy)`
- `BattleManager.GetUnitsInPlayList(ref List<CardScript> listToFill, bool clearList, bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying)`
- `BattleManager.GetUnitsInPlayNewList(bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying)`
- `BattleManager.GetUnitsInPlayPooledList(bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying)`
- `BattleManager.IsAgainstHuman()`
- `BattleManager.IsCardIncluded(CardScript card, bool inclHero, bool inclMinions, bool inclHeroParts, bool inclSpells, bool inclWhispers, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy)`
- `BattleManager.IsClosedDeckGame(MatchType chosenMatchType)`
- `BattleManager.IsThereValidAbilityTarget(CardScript actingCard)`
- `BattleManager.IsThereValidSpellTarget(CardScript targetedSpellCard)`
- `BattleManager.IsValidSpellTarget(CardScript spell, CardScript target)`
- `BattleManager.MulliganFallbackCountdown()`
- `BattleManager.PlayAi()`
- `BattleManager.ReadyToUseActiveAbility(CardScript parentCard, bool isPlayerAsking, bool showTips)`
- `BattleManager.RegisterPlayerAction(BattleAction battleAction)`
- `BattleManager.RemoveExtrinsicEffectsFrom(CardScript actingCard, int priority)`
- `BattleManager.ResolveAddEffect(BattleAction battleAction)`
- `BattleManager.ResolveCreateDeckCard(BattleAction battleAction)`
- `BattleManager.ResolveCreateHandCard(BattleAction battleAction)`
- `BattleManager.ResolveFollowAfterResolution(BattleAction battleAction)`
- `BattleManager.ResolveJamUnit(BattleAction battleAction)`
- `BattleManager.ResolveManaBuff(BattleAction battleAction)`
- `BattleManager.ResolveRemoveFromCemetery(BattleAction battleAction)`
- `BattleManager.ResolveTriggerCodex(BattleAction battleAction)`
- `BattleManager.ResolveTriggerRequiem(BattleAction battleAction)`
- `BattleManager.ResolveTriggerSacrifice(BattleAction battleAction)`
- `BattleManager.SetPlayerDataAndWarlord(DeckAndWarlordData deckData, bool isPlayer)`
- `BattleManager.SetupInitialMana(bool isPlayer)`
- `BattleManager.SetupReplayMulligan()`
- `BattleManager.SetupReplayMulliganStart()`
- `BattleManager.ShouldActivatInitialEnchantments()`
- `BattleManager.ShuffleDeck(List<CardScript> deckToShuffle, bool checkStratagem)`
- `BattleManager.StartBattlePhase()`
- `BattleManager.TryUsingActiveAbility()`
- `BattleManager.TryUsingActiveAbilityOfCard(CardScript card, bool forceSelectCard)`
- `BattleManager.UpdateManaLeftAccumulation(bool isPlayer)`
- `CardScript.ActivateBloodThirst()`
- `CardScript.ActivateMinion(bool isPlayerTurn)`
- `CardScript.AddEffect(CardEffect newEffect, int enchantmentCardId)`
- `CardScript.CanPlayWithCodexNow()`
- `CardScript.CanPlayWithOathNowInHand()`
- `CardScript.CancelAttack(bool playerInitiated)`
- `CardScript.CardDrawn(CardScript actingCard)`
- `CardScript.CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool keepStats)`
- `CardScript.ClickMulliganSelected(bool fromClick)`
- `CardScript.FinishAfterAttack()`
- `CardScript.GetAdjustedDamage(DamageInfo grossDamage, bool shouldCountBastion)`
- `CardScript.HasEnoughPendingDamageAndIsRemnant()`
- `CardScript.HideWarlordActiveAbilityForTutorial()`
- `CardScript.OnTurnEnd(bool playerTurn)`
- `CardScript.OnTurnSetup(bool playerTurn, bool isInHand)`
- `CardScript.PrepareAttack(bool toPrepareAttack, bool playSound)`
- `CardScript.RemoveBuffHandCostEffects()`
- `CardScript.ResolveActiveAbilityPlayed(CardScript actingCard, CardScript targetCard)`
- `CardScript.ResolveCardPlayed(CardScript cardPlayed, CardScript targetCard)`
- `CardScript.ResolveDeadCard(BattleAction battleAction)`
- `CardScript.StartShowingCardInHand()`
- `CardScript.TriggerUnitBacklashActions(UnitDeathType deathType, CardScript actingCard)`

### 落点仍是类级/共享/无方法级（要补行号段） — 3 条

- `BattleManager.ResolveTriggerMob(BattleAction battleAction, bool isAdditionalTriggerMob)`
- `BattleManager.ResolveTriggerRegiment(BattleAction battleAction)`
- `BattleManager.ResolveTriggerTeleport(BattleAction battleAction)`

### 有已知行为差（须先改码） — 81 条

- `BattleManager.ActionsLeftInQueue()`
- `BattleManager.AddRemoveFromCemetery(CardScript targetCard, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.AllowResolveAttack(BattleAction battleAction)`
- `BattleManager.BroadcastActivateMinions(bool playerTurn)`
- `BattleManager.BroadcastChangedLanguage()`
- `BattleManager.BroadcastCollectedFaith(CardScript unitCollected)`
- `BattleManager.BroadcastCollectedQuestPoints(CardScript unitCollected)`
- `BattleManager.BroadcastCollectedSpiritStone(CardScript unitCollected)`
- `BattleManager.BroadcastEarnSkull(bool isPlayer)`
- `BattleManager.BroadcastQuestResolved(CardScript questCard)`
- `BattleManager.BroadcastSacrificeResolved(CardScript unitSacrifice)`
- `BattleManager.BroadcastUnitDamaged(CardScript damagedUnit, DamageInfo damageInfo, bool dealtByAttacker)`
- `BattleManager.BroadcastUnitRegiment(CardScript unitRegiment, CardScript unitRegimentCaster)`
- `BattleManager.BroadcastUnitSpiritStone(CardScript unitSpiritStone)`
- `BattleManager.BroadcastUnitSynapse(CardScript synapseUnit)`
- `BattleManager.CheckIfAbilityCancelsAttack(CardScript actingCard, CardScript targetCard)`
- `BattleManager.CheckUnitsCancellingAttack(CardScript actingCard, CardScript targetCard)`
- `BattleManager.DealMultiDamageAndDestroySelf(DamageInfo damageInfo, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.DealMultiDamageVariable(List<DamageInfo> damageInfoList, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)`
- `BattleManager.DisplayHurryUpChatMessage()`
- `BattleManager.EndTurnClick(bool timeOutFlag)`
- `BattleManager.ExecuteNextAction(List<PlayerAction> actionsLog, ref int actionsIndex, bool isPlayer)`
- `BattleManager.GetAvailableActions(bool isPlayer, bool allowAutoActions)`
- `BattleManager.GetAvailableAttackActions(CardScript actingCard, bool allowAutoActions)`
- `BattleManager.GetAvailableAttackTargets(CardScript attacker)`
- `BattleManager.GetChoiceIndexOfCard(CardScript actingCard, CardScript targetCard, RawCardScript chosenCard)`
- `BattleManager.GetViewportPointFromBoardPos(Vector3 worldPos)`
- `BattleManager.LeaveBattle()`
- `BattleManager.NeedToChooseMinionTarget(CardScript actingCard)`
- `BattleManager.RecordAttackPendingDamage(CardScript actingCard, CardScript attackedCard, AttackTypes attackType, int actionPriority, bool recordReturnDamage)`
- `BattleManager.ResolveAction(BattleAction battleAction)`
- `BattleManager.ResolveAddHandEffect(BattleAction battleAction)`
- `BattleManager.ResolveChangeBaseAttack(CardScript targetCard, int newAttack)`
- `BattleManager.ResolveChangeMaxHealth(CardScript targetCard, int newHealth)`
- `BattleManager.ResolveClearEndOfTurnEffectsFrom(CardScript actingCard)`
- `BattleManager.ResolveDiscardFromHand(BattleAction battleAction)`
- `BattleManager.ResolveExchangeMinion(BattleAction battleAction)`
- `BattleManager.ResolveFatigue(bool playerDrawing)`
- `BattleManager.ResolveMinionDeath(BattleAction battleAction)`
- `BattleManager.ResolvePlayActiveAbility(BattleAction battleAction)`
- `BattleManager.ResolveRemoveEffectsFrom(CardScript actingCard)`
- `BattleManager.ResolveResetActions(CardScript target)`
- `BattleManager.ResolveTransformFromRemnant(BattleAction battleAction)`
- `BattleManager.ResolveTransformUnit(BattleAction battleAction)`
- `BattleManager.SetUnitOrderInPlay(CardScript card)`
- `BattleManager.ShuffleDeck(bool isPlayer)`
- `CardScript.ChangeBaseAttack(int newAttack)`
- `CardScript.CheckAndApplyCurrentPack()`
- `CardScript.CheckReplacedAbility()`
- `CardScript.ExecuteSynapeEffect(CardScript actingCard)`
- `CardScript.ForceChangeMaxHealth(int newHealth)`
- `CardScript.HasPackBuffTrait()`
- `CardScript.IsValidActiveAbilityTarget(CardScript target)`
- `CardScript.NeedsToChooseFromPool(CardScript targetCard)`
- `CardScript.NonTargetSpellPlayed()`
- `CardScript.ReactToUnitHealed(CardScript healedUnit, CardScript actingUnit)`
- `CardScript.ReactToUnitJammed(CardScript disenchantedCard)`
- `CardScript.RemoveFromGameCard(CardScript removedCard)`
- `CardScript.RemoveTraitSilently(DefinedTrait traitToRemove)`
- `CardScript.ResetToValuesInHand(bool resetTriggeredBacklash)`
- `CardScript.ResolveDamageDealt(CardScript damagedCard, DamageInfo damageInfo)`
- `CardScript.ResolveTransient()`
- `CardScript.ResolveUnitAttacked(CardScript actingCard, CardScript targetCard, AttackTypes attackType)`
- `CardScript.ResolveUnitSummoned(CardScript cardPlayed, CardScript targetCard)`
- `CardScript.SendClearEndOfTurnEffects(int priority)`
- `CardScript.SendRemoveEffect(CardEffect cardEffect, int priority)`
- `CardScript.SetStateAsMulligan(bool toBeDiscarded)`
- `CardScript.SetupActiveAbility()`
- `CardScript.SetupAttack(bool playSound, bool playerInitiated)`
- `CardScript.ShouldTriggerCompanion()`
- `CardScript.ShowDropPodFigures()`
- `CardScript.TacticPlayed(CardScript actingCard, CardScript targetCard)`
- `CardScript.TargetedSpellPlayed(CardScript actingCard, CardScript targetCard)`
- `CardScript.TransformIntoRemnant()`
- `CardScript.TriggerCodex(CardScript targetCard)`
- `CardScript.TriggerOtherCardAbility(RawCardScript abilityCard)`
- `CardScript.TriggerOtherCardRequiem(CardScript requiemUnit)`
- `CardScript.TriggerSpiritStone(CardScript actingCard)`
- `CardScript.TriggeredEarnSkull(CardScript earnedWarlord)`
- `CardScript.UsedActiveAbility()`
- `CardScript.hasMaintenanceEffects()`

### 应判 C（我们没实现 / 缺机制） — 30 条

- `BattleManager.AddExchangeMinions(CardScript actingCard, CardScript minion1, CardScript minion2, int priority, bool blockPlayerActions)`
- `BattleManager.AddInitialCardToDeck(RawCardScript startingCard, bool isPlayerY, RawCardScript warlord, ref List<CardScript> deck)`
- `BattleManager.AddTransformCardWithBuff(CardScript targetCard, RawCardScript transformedIntoCard, int priority, bool checkIfCasterInPlay, CardScript casterCard, CardEffect buffEffect, bool keepEffects, bool blockPlayerActions)`
- `BattleManager.AddTriggerCourtesy(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)`
- `BattleManager.AddTriggerRelentless(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)`
- `BattleManager.CancelMulliganAfterBattleEnd()`
- `BattleManager.CheckForHeroDeckTransform(DeckAndWarlordData rawBattleData, bool isPlayer)`
- `BattleManager.CopyUnitInCemetery(CardScript targetCard, int priority, bool blockPlayerActions)`
- `BattleManager.GetCampaignScriptedData(CampatinScriptedActionType actionType)`
- `BattleManager.GetHighestHealthWarlord()`
- `BattleManager.GetWarcry(bool isPlayer)`
- `BattleManager.IsAgainstAI()`
- `BattleManager.IsAgainstOnlineBot()`
- `BattleManager.IsCampaignScriptAvailable(CampatinScriptedActionType actionType)`
- `BattleManager.IsCampaignStageAvailable()`
- `BattleManager.IsReplayMatch()`
- `BattleManager.IsScriptedStageAvailable()`
- `BattleManager.PayCardPlayedCostOath(CardScript card)`
- `BattleManager.RegisterPlayerActionCancelled(BattleAction battleAction)`
- `BattleManager.ResolveCopyUnitInCemetery(BattleAction battleAction)`
- `BattleManager.ResolveMissAction(CardScript target)`
- `BattleManager.ResolveStealFromDeck(BattleAction battleAction)`
- `BattleManager.ResolveStealFromHand(BattleAction battleAction)`
- `BattleManager.ResolveTriggerCourtesy(BattleAction battleAction)`
- `BattleManager.ResolveTriggerRelentless(BattleAction battleAction)`
- `BattleManager.WaitingForBlockingAction(bool showTips)`
- `CardScript.CanPlayWithOathNowInPlay()`
- `CardScript.ChangeMaxHealth(int newHealth)`
- `CardScript.RemoveDuplicitous()`
- `CardScript.SetHealthToZero()`

### 应判 D / 域外（原版死·恒假·纯表现） — 13 条

- `BattleManager.AddTriggerPlayerCodex(CardScript actingCard, CardScript targetCard, int priority, bool blockPlayerActions)`
- `BattleManager.AddVoiceLinesControllerListeners()`
- `BattleManager.AllUnitsInPlay(List<CardScript> cards)`
- `BattleManager.HandleOfflineBattleResult()`
- `BattleManager.IsDuringBattle(bool includeMulligan)`
- `BattleManager.IsPlayingCardState()`
- `BattleManager.RemoveDestroyableAnims(CardScript targetCard, CardEffect cardEffect)`
- `BattleManager.ReplaceHeroPower(RawCardScript newPower, bool isPlayer, int priority)`
- `BattleManager.ResolveResetDuty(CardScript target)`
- `BattleManager.ResolveRevealOrders(BattleAction battleAction)`
- `BattleManager.SetNextCardAsDeckDrawing(bool isPlayer)`
- `BattleManager.SetTutorialStopState(bool option)`
- `BattleManager.ShowPotentialTargets(BattleManager self, CardScript card, int mode)`

### 结构上不能升 A（落点只能指字段/属性/注释） — 34 条

- `BattleManager.AlreadyPlayedCardThisTurn(bool isPlayer)`
- `BattleManager.GetAllCardsInHand()`
- `BattleManager.GetAllCardsInPlay(bool includeDying)`
- `BattleManager.GetCardsInCemetery(bool inclMinions, bool inclSpells, bool inclWhispers, bool inclWeapons, bool inclPlayer, bool inclEnemy)`
- `BattleManager.GetCardsInDeck(bool inclMinions, bool inclSpells, bool inclWeapons, bool inclPlayer, bool inclEnemy)`
- `BattleManager.GetCardsInHandList(bool inclMinions, bool inclSpells, bool inclWhispers, bool inclWeapons, bool inclPlayer, bool inclEnemy)`
- `BattleManager.GetCardsInHandRef(bool isPlayer)`
- `BattleManager.GetCurrentPlayerId(bool isPlayerTurn)`
- `BattleManager.GetDeck(bool playerHeroY)`
- `BattleManager.GetEnemyHandRef()`
- `BattleManager.GetEnemyMinionsAndRemnantInPlay()`
- `BattleManager.GetFriendlyHandRef()`
- `BattleManager.GetHero(bool playerHeroY)`
- `BattleManager.GetOriginalWarlord(bool playerHeroY)`
- `BattleManager.GetPlayerMinionsAndRemnantInPlay()`
- `BattleManager.GetPremulliganScriptedData()`
- `BattleManager.GetStartingMana(bool isPlayer)`
- `BattleManager.GetTotalMana(bool isPlayer)`
- `BattleManager.GetUnitsInPlayRef()`
- `BattleManager.HasScriptedPremulliganActions(TutorialStage tutorialStage)`
- `BattleManager.IsPremulliganScriptAvailable()`
- `BattleManager.IsThereDoubleOathEffect(bool isPlayer)`
- `BattleManager.WaitOpponentLastActions()`
- `CardScript.CardWaitingToBePlayed()`
- `CardScript.GetHighestAttackValue()`
- `CardScript.GetOwnHero()`
- `CardScript.HasDiedSinceLastTurn()`
- `CardScript.HasDiedThisTurn()`
- `CardScript.IsSameTrait(CardEffect effect1, CardEffect effect2)`
- `CardScript.IsTheSameTurnPlayed()`
- `CardScript.SetAsPlayer(bool isPlayer, CardArmy army, bool setCardback)`
- `CardScript.SetFutureRemnant(bool willBeRemnant)`
- `CardScript.SetLeavingRemnant()`
- `CardScript.SetPray(bool activate)`

