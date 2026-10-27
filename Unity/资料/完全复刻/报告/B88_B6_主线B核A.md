# B88_B6 —— 主线「可直核 A」183 条的逐条核实（核成 A / 发现差异）

> 执行代理 **B6**（第八十八会话第三波）· 2026-10-28
> 输入 = `资料/完全复刻/判据/B88_R7_主线B分类.md` 里分类为 **可直核A** 的 **183 条**（R7 主表 317 行中的那一档）。
> 产物 = `账/_parts/B88_B6.tsv`（42 行，`状态 B→A`）+ 本报告。⛔ 未改任何 `.cs` / 账主表 / 正本 / git。

**结论一行**：183 条里 **42 条核成 A**（其中 11 条走 §4.1 的字段/属性/嵌套类型例外）、**141 条核过但发现真实差异或证据不足、保持 B**、**0 条没核**。

核实方式：7 个执行代理各领 27 条（判据读**全文**、落点 `grep` 现找不认旧行号）→ 产出 `b6_out_1..7.tsv`；随后**另派 2 个独立审查代理**（≠作者）对 56 条候选 A 逐条复核，剔除 14 条（其中 3 条只是落点行写错、修正后仍为 A）。

## 一、核成了 A 的：42 条

| 账行 | 类.方法 | 新落点（现读行号） | 备注 |
| --- | --- | --- | --- |
| 118 | ActionsLeftInQueue() | ActionQueue.cs:224（Count） | §4.1 字段/属性例外 |
| 204 | BroadcastChangedLanguage() | BattleHudLanguage.cs:47（Notify） | 方法落点 |
| 249 | CheckIfAbilityCancelsAttack(CardScript actingCard, C | RuleCore.cs:8013（TryCancelAttackByOwnAboutToAttackCore） | 方法落点 |
| 304 | ExecuteActiveAbility(CardScript actingCard, CardScri | RuleCore.cs:15352（UseActiveAbility） | 方法落点 |
| 308 | ExecuteUseWaystone(CardScript actingCard) | RuleCore.cs:12368（CollectWaystone） | 方法落点 |
| 345 | GetCurrentTurnScriptedData() | RuleCore.cs:1222（CurrentTurnScriptedData） | 方法落点 |
| 355 | GetHero(bool playerHeroY) | PlayerState.cs:72（Warlord） | §4.1 字段/属性例外 |
| 369 | GetPlayerHand(bool playerY) | PlayerState.cs:20（Hand） | §4.1 字段/属性例外 |
| 370 | GetPlayerManager(bool playerY) | BattleContext.cs:319（Players） | §4.1 字段/属性例外 |
| 382 | GetTotalMana(bool isPlayer) | PlayerState.cs:425（GetTotalMana） | 方法落点 |
| 535 | ResolveResetActions(CardScript target) | RuleCore.cs:14530（RefundForcedAttackAction） | 方法落点 |
| 536 | ResolveResetDuty(CardScript target) | EffectResolver.cs:15171（DoReloadDuty） | 方法落点 |
| 551 | ResolveTriggerCodex(BattleAction battleAction) | RuleCore.cs:13242（CheckCodex） | 方法落点 |
| 556 | ResolveTriggerRegiment(BattleAction battleAction) | RuleCore.cs:12514（FireTriggerAt） | 方法落点 |
| 563 | ResolveTriggerTeleport(BattleAction battleAction) | RuleCore.cs:12514（FireTriggerAt） | 方法落点 |
| 580 | SetupInitialMana(bool isPlayer) | RuleCore.cs:1696（BeginTurnCore） | 方法落点 |
| 590 | SetupWarlordsLifeAndEnchantment() | RuleCore.cs:605（BuildPlayer） | 方法落点 |
| 663 | BroadcastUnitSpiritStone(BattleManager manager, Card | EffectResolver.cs:16431（BroadcastUnitSpiritStone） | 方法落点 |
| 668 | BroadcastUnitUsedSacrifice(BattleManager manager, Ca | WhenEvent.cs:1886（SelfExcludesOnKeywordTrigger） | 方法落点 |
| 703 | CardDrawn(CardScript actingCard) | EffectResolver.cs:9842（BroadcastWhen） | 方法落点 |
| 730 | DissolveMinionCard(bool dissolveY) | CardFeel.cs:3705（MoveBackToHand） | 方法落点 |
| 762 | GetOwnHero() | PlayerState.cs:72（Warlord） | §4.1 字段/属性例外 |
| 841 | RecordPendingHealing(HealInfo newHeal) | RuleCore.cs:14667（RecordPendingHealing） | 方法落点 |
| 845 | RemoveAttackThisTurn() | RuleCore.cs:14514（RemoveAttackThisTurn） | 方法落点 |
| 897 | SetFutureRemnant(bool willBeRemnant) | UnitState.cs:410（IsRemnant） | §4.1 字段/属性例外 |
| 900 | SetLeavingRemnant() | UnitState.cs:425（IsLeavingRemnant） | §4.1 字段/属性例外 |
| 902 | SetPray(bool activate) | UnitState.cs:351（Prayed） | §4.1 字段/属性例外 |
| 1031 | GetCurrentTraitData(DefinedTrait trait) | UnitState.cs:1104（KwValue） | 方法落点 |
| 986 | GetAbilityToTraitDict(CardTypeOptions cardType) | CardDefExtraTraits.cs:110（ExtraAbilityTraits） | §4.1 字段/属性例外 |
| 990 | GetTraitEffect(DefinedTrait trait, CardTrait.EffectT | TraitFrames.cs:467（BindingsFor） | 方法落点 |
| 991 | GetTraitEffectObject(DefinedTrait trait, CardTrait.E | TraitFrames.cs:553（PrefabFor） | 方法落点 |
| 101 | GetDelay(BattleManager manager, TurnScriptedData tur | BattleDriver.cs:18341（DriveTutorialScript） | 方法落点 |
| 674 | GetManaLeft(BattleManager manager, bool isPlayer) | PlayerState.cs:74（Energy） | §4.1 字段/属性例外 |
| 675 | GetTotalMana(BattleManager manager, bool isPlayer) | PlayerState.cs:75（MaxEnergy） | §4.1 字段/属性例外 |
| 1003 | GetAverageDamage(CardScript thisCard, CardScript tar | AiScoring.cs:770（AverageValue） | 方法落点 |
| 1139 | ReassembleMinionsWhilePlayingUnit(Vector2 newUnitPos | BattleDriver.cs:26891（OnDropPreview） | 方法落点 |
| 1140 | RefreshOccupationSlots() | BoardSlots.cs:78（CountOnSide） | 方法落点 |
| 1162 | GetCardInPos(int indexInHand) | PlayerState.cs:20（Hand） | 方法落点 |
| 1180 | NumCardsInHand() | PlayerState.cs:20（Hand） | 方法落点 |
| 1218 | GetActiveAbility(BattleManager manager, CardScript t | CardDef.cs:295（UsesAlternativeAbility） | 方法落点 |
| 1261 | IsValidActiveAbilityTarget(CardScript thisCard, Card | RuleCore.cs:11367（CanUseAbilityTarget） | 方法落点 |
| 1375 | BroadcastUnitUsedSacrifice(CardScript unitSacrifice) | RuleCore.cs:12514（FireTriggerAt） | 方法落点 |

**这 42 条的共同形状**：判据（原版方法体）逐条读完后，我们侧**确有等价实现**，差别只在落点写法/行号/拆成几处；残差都写进了该行「差异」列。
其中 11 条（118 · 355 · 369 · 370 · 674 · 675 · 762 · 897 · 900 · 902 · 986）属**纯取值器 / 结构等价**，按方案 §4.1（2026-10-27 例外）用**字段 / 属性 / 嵌套类型**声明当落点 —— 差异列已明写「等价物 = 字段（属性）直读」或「结构等价」，机检结果是 **MEMBER 告警（不计失败）**。

## 二、核过但发现差异、**没升 A** 的：141 条

口径：每条都打开判据全文 + 现读我们的落点后判的。**「差在哪」是核实结论的摘要**（完整版在 `b6_out_1..7.tsv` 的「差异补充」列）；「归类」= 建议下一波按哪一档处理。

| 账行 | 类.方法 | 差在哪（摘要） | 归类 |
| --- | --- | --- | --- |
| 76 | GetTargets(BattleManager manager, TargetCriteria criteria, CardScript thisCard, CardScript actingCard, CardScript targetCard, bool checkMaxTargets) | 不能升 A：41 档里 21 档仍无落点/缺一格，且有两条已确认的真缺陷（`your Warlord and adjacent units` 被判成 Kind=warlord ⇒ 池里只放督军、邻居漏放）⇒ 实现与判据不一致；建议把这两条缺陷单独立账（按铁律 11 属 C），本行保 B | 保持 B（无声明可落） |  |
| 79 | NewBuffStat(BattleManager manager, ChooseValueOptions valueOption, int minValue, AbilityData abilityData, int multiplier, int altvalue) | 差一跳：原版 NewBuffStat 是「把基准值 + ChooseValueOptions 折成一个数」，我们的 ApplyOneGain（EffectResolver.cs:11923）是「把这个数施到单位上」，职责不同；valueOptions 的非常规档在引擎里没有对应实现（池内 buf… | 差一跳 |  |
| 80 | PlayAbility(BattleManager manager, AbilityData abilityData, AbilityTrigger trigger) | 不能升 A：86 支里 19 支是 C（变身 80/85/90 · 手牌搬移 160-175 · exchange 190/200 · copyCemetery 210 · 造卡进牌库 270 · 从手牌展示 310 · 墓地移除 350 · oath 360 · 置顶 420 · disapp… | 差一跳 |  |
| 82 | DeepCopy() | 不能升 A：这一簇在 dev_ilspy / decomp_full / assets_full 字符串三处都 0 调用点（疑死代码 ⇒ 按 §4.1 应判 D 而不是 A），且 DeepCopy 深拷的 targetCriteria(+0x38) 运行期写点没扫（VA 级）；写点存在则「共享不… | 建议改判据有问题 / 候选 D |  |
| 83 | ShallowCopy() | 等价物 = 结构等价：ActiveAbilityLogicSpec（嵌套类型）承担「一份能力数据被多张卡共用」的同一职责；方案 §4.1 的 2026-10-27 例外把 ShallowCopy→ActiveAbilityLogicSpec 明列为 A 的范例。⚠️ 原版这簇全树 0 调用点（本… | 保持 B（有实质缺口） | 审查 NO：原版 ShallowCopy 是 0 调用点的死方法（out_cpp2il_dev_ilspy/ActiveAbility.cs:39 与  |
| 115 | SkippableActionWait(ScriptedAction action, float time) | 差一跳：原版只有 CanSkipAction（聊天五档）那一档能被 skipNextAction 打断，其余走 WaitForSeconds 真等满；我们的 RequestTutorialSkip（BattleDriver.cs:18508）只判 `_tutWaitArmed`，而 animat… | 差一跳 |  |
| 156 | AddRemoveFromCemetery(CardScript targetCard, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions) | 判据已核：原版这一跳唯一效果 = 清 `shouldCheckIfIsInCemetery(+0x24b)`（`CardScript__RemoveCardFromCemetery.c:5` 全文一句）；我们**没有「取出途中」这个状态**（墓地候选每拍现算、同步完成；全仓 `shouldChe… | 差一跳 |  |
| 169 | AddSummonUnit(RawCardScript summonedCard, int summonPos, bool isPlayer, int priority, bool checkIfCasterInPlay, CardScript casterCard, CardEffect cardEffect, bool blockPlayerActions, RawCardScript enchantment) | ；**「召唤即附魔」那条已可撤**：原版两个可选载荷（`cardEffect` / `enchantment`）在**唯一调用点**全传 0（`out_il2csharp/Assembly-CSharp/AbilityLogic.cs:1375` 末两个实参 = `0, 0`；decomp_fu… | 保持 B（有实质缺口） | 审查 NO：解算半缺两跳且未写进差异列：原版 _ResolveSummonUnit 对带伏击(1200)的召唤物 CardScript.SetAmbus |
| 193 | AllowResolveAttack(BattleAction battleAction) | 本节现读补正：末段不是「isPlayer 闸」，而是**出队时**的陈旧动作守卫 —— `IsAttacking()` 为真或 `matchType == 0xa0(160)` 时不走正常路（`BattleManager__AllowResolveAttack.c:440-460` 写 LogW… | 差一跳 |  |
| 227 | BroadcastUnitRegiment(CardScript unitRegiment, CardScript unitRegimentCaster) | 本节现读补正：账里两处落点**都不成立** —— `RuleCore.cs:6695` 现读是「同时伤害」批的注释行，`EffectResolver.cs:7372` 现读是 `TN_OtherUnitSummoned` 常量；Regiment 真正那一跳 = `FireTriggerAt(..… | 差一跳 |  |
| 231 | BroadcastUnitSynapse(CardScript synapseUnit) | 本节现读补正：`EffectResolver.cs:2746` 是 `ResolveOps` 里的注释行、`:7353` 是「引擎事件」那一节的注释头；真正的广播 = `BroadcastKeywordEvent(ctx, Triggers(Synapse), anchor)`（`EffectR… | 差一跳 |  |
| 233 | CanPlayCard(CardScript card, bool showTips, bool initiateAction, bool actionAlreadyStarted) | 本节现读两处账错：① 首闸不是「棋盘满」—— `+0x350` 是 **`actionQueue`**（`dump.cs:30856`），原文是「showTips 且动作队列非空 ⇒ 弹一句拒」；② 我们的闸**跨两层**：`RuleCore.CanPlayCard:3273` 只有 费用 / … | 差一跳 |  |
| 253 | CheckUnitsCancellingAttack(CardScript actingCard, CardScript targetCard) | 本节现读对上原版：`CheckUnitsCancellingAttack` 返回 `List<CardScript>` —— **每个** `HasCancelAttackAbility` 且非 `jam` 的单位各收一条（cpp2il `BattleManager.cs:23386-23430… | 差一跳 |  |
| 254 | ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, int selectedCard) | 本节现读补正：账里 `EffectResolver.cs:4706` 是 `ChooseDeadScope` 那一支的语句行（过不了闸），`DoChooseCard` 声明在 :4760；且**选法不一致** —— 原版 flag3 档是「取 `GetChoiceOptions(...)[0]`… | 差一跳 |  |
| 255 | ChoiceOfCardPlayer(CardScript actingCard, CardScript targetCard, int actionValue) | 本节现读补正：账里 `EffectResolver.cs:4706` 是语句行；原版这是**面板回包入口**（`CancelActionStates` → `UIstate=choosingResolutionCard` → `chooseCardManager.Setup(...)` 等玩家选… | 差一跳 |  |
| 256 | ChooseCardMethod(BattleAction battleAction, Action<int> callback) | 本节现读补正：原版是**协程**（`IEnumerator ChooseCardMethod` → 状态机 `_d__449.MoveNext` 推进「等待 / 超时 / 取消」），我们的 `ChooseCardCandidates`（`EffectResolver.cs:4607`）只做候选域… | 差一跳 |  |
| 279 | DealMultiDamageAndDestroySelf(DamageInfo damageInfo, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions) | 本节现读坐实差异（判据改用 `.c`；cpp2il 那份体是 mangled，不可用）：原版**先** `CardScript__SetAsUnitDestroyed(casterCard)`（`BattleManager__DealMultiDamageAndDestroySelf.c:50`… | 差一跳 |  |
| 292 | DeviceVariables() | 原版是一行**平台**取值器 `return desktopVariables;`（cpp2il `BattleManager.cs:47933-47936`）；`GameplayVariables.For`（:547）按 **GameMode** 分档，平台差异是 `VarsGlobal` 的… | 差一跳 |  |
| 306 | ExecuteNextAction(List<PlayerAction> actionsLog, ref int actionsIndex, bool isPlayer) | 本节现读补正：账里落点 `ActionPump.cs:1379` 现读是 `TriggerLane`（**无关**）；原文（`BattleManager__ExecuteNextAction.c:44-75`）= 取 `actionsLog[*actionsIndex]` → `CreateRe… | 差一跳 |  |
| 322 | GetAllCardsInPlay(bool includeDying) | 本节现读：原版返回**过滤后的缓存 List**（排除 Tactic / Whispers（及 Hero）、要求 `IsInPlay()`，`includeDying` 时另收 `waitingToDie`，cpp2il `BattleManager.cs:41426-41478`）；我们**没… | 差一跳 |  |
| 342 | GetChoiceIndexOfCard(CardScript actingCard, CardScript targetCard, RawCardScript chosenCard) | 原文 = `GetChoiceFullPool(...).IndexOf(chosenCard)`（一行取值器，cpp2il `BattleManager.cs:25424-25428`）；我们**没有这一份声明**（取第几张内联在 `DoChooseCard`(:4760) / `TakePi… | 差一跳 |  |
| 346 | GetDeathType(BattleAction battleAction, DamageInfo damageInfo) | 本节现读（判据用 `.c`；cpp2il 那份体是 unmanaged-load 乱码、不可用）：原文按 **(damageType, actingCard.isPlayer vs IsPlayerTurn)** 选档 —— `VulnerableCombat` 且 `actingCard.is… | 差一跳 |  |
| 348 | GetDeckManager(bool playerHeroY) | 原文是一行**平台对象取值器**（`return playerHeroY ? playerDeck : enemyDeck;`，cpp2il `BattleManager.cs:42681-42688`），返回的是 `DeckManager` 这个**组件**（卡背 / 位置 / 翻牌那套表现职… | 差一跳 |  |
| 350 | GetEnemyMinionsAndRemnantInPlay() | 原文 = 扫 `unitsInPlay` 筛「`!isPlayer` ∧ cardType∉{Hero,Tactic,Whispers} ∧ `IsInPlay()`」写进缓存 List（cpp2il `BattleManager.cs:41514-41611`；**名称里的 AndRemnan… | 差一跳 |  |
| 361 | GetMinionManager(bool playerY) | 原文 = 一行平台对象取值器（`playerY ? playerMinionManager : enemyMinionManager`，cpp2il `BattleManager.cs:45512-45519`）；我们**没有 MinionManager 这一层**（棋盘 = `PlayerSt… | 差一跳 |  |
| 364 | GetMulliganCard(bool isPlayer) | 本节现读补正：原文**不是**「取 UI 选中那张」，而是「**从牌库顶弹一张出来**」—— `list[1].SetActive(true)` 后 `card = list[0]; SetActive(true); return list[0]; list.RemoveAt(0)`，牌库空则 … | 差一跳 |  |
| 371 | GetPlayerMinionsAndRemnantInPlay() | 2026-10-28 B6-C2 升级依据：原版 = unitsInPlay 里 isPlayer 为真 ∧ cardType 不是 10/20/40 ∧ IsInPlay()（结果 AddRange 进 +0x550 缓存）；我们 = 该 side 的 Board 非空格且 !IsWarlor… | 保持 B（证据不足） | 审查 WEAK：原版三方过滤（isPlayer ∧ cardType∉{10,20,40} ∧ IsInPlay）在「本方 Board 非空 ∧ !IsWa |
| 376 | GetRawcardScript(TargetsAffected targetsAffected, List<CardScript> cardScriptList, List<RawCardScript> rawCardScriptList) | 保持 B：原版 GetRawcardScript 把 CardScript 列表映射成它们的 raw SO 列表（choiceResolution 档直接返入参那份 / cardsTargetedInChosenResolution）；我们**没有 raw SO 这一层**（UnitState.… | 差一跳 |  |
| 383 | GetTraitEffect(DefinedTrait trait, CardTrait.EffectTriggerType trigger, CardArmy cardArmy) | 保持 B（差一跳）：原版 GetTraitEffect = 转调 CardTraitCollection.GetTraitEffectObject，后者按 +0x28 == trigger 逐条挑出「那一个 trigger」的行再做 army 覆盖 + AssetReference.Load；我… | 差一跳 |  |
| 385 | GetUnitsInPlayList(ref List<CardScript> listToFill, bool clearList, bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying) | 保持 B：原版 GetUnitsInPlayList(ref List, clearList, 8 个筛位) = 按 clearList 清表 + AddRange(GetUnitsInPlayPooledList(...))；我们 Units() 只有「非空、非督军」一档，8 个筛位由调用点各… | 差一跳 |  |
| 386 | GetUnitsInPlayNewList(bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying) | 保持 B：同 385 —— GetUnitsInPlayNewList 只是新分配一张 List 再走同一组筛位；我们 Units() + 调用方 ToList() 不含筛位（该行 Cpp2IL 还把部分实参写成 default，形状待核） | 差一跳 |  |
| 387 | GetUnitsInPlayPooledList(bool inclHero, bool inclMinions, bool inclHeroParts, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy, bool includeDying) | 保持 B：GetUnitsInPlayPooledList 逐位筛（inclPlayer/inclEnemy 比 +0x40、inclHero=cardType10、inclMinions=cardType0、恒排除 20/40、inclRemnants(+0x65)、includeDying=… | 差一跳 |  |
| 388 | GetUnitsInPlayRef() | 保持 B：原版直接返回双方共用的 unitsInPlay 列表引用(+0x470)（追加序）；我们是每方 9 格 Board、顺序等价物 = UnitState.PlayOrder ⇒ 没有「一张共享表」这个声明，Board 不是它的等价物 | 差一跳 |  |
| 395 | HandleOfflineBattleResult() | 保持 B / P3 边界：原版 HandleOfflineBattleResult = 组装离线结果 + BattleFinished() + 把 rating/xp/elo 写死 30 并（服务端那半）上报；本地半（结算）我们有，但没有同名/同义声明，且上报属 P3（§四） | 差一跳 |  |
| 411 | IsCardIncluded(CardScript card, bool inclHero, bool inclMinions, bool inclHeroParts, bool inclSpells, bool inclWhispers, bool inclWeapons, bool inclRemnants, bool inclPlayer, bool inclEnemy) | 保持 B（落点语义不对）：原版 IsCardIncluded 是**侧/卡种/残骸**过滤谓词（inclPlayer/inclEnemy 比 +0x40、inclHero=cardType10、inclMinions=cardType0、inclSpells=20、inclWhispers=40… | 差一跳 |  |
| 412 | IsClosedDeckGame(MatchType chosenMatchType) | 保持 B：原版 IsClosedDeckGame(t) = t 属于 {110 ClosedDeck, 120 ClosedDeckBot}；我们**没有同名谓词**，调用点直接比枚举（MatchType 值域已逐值对上）⇒ 无声明可落（枚举成员不是可落声明）；最近的 MatchTypes.Us… | 差一跳 |  |
| 427 | IsThereDoubleOathEffect(bool isPlayer) | 保持 B（差一跳）：原版调用点每圈重算 IsThereDoubleOathEffect（CardScript__ResolveActiveAbilityPlayed.c:64-79 的 while(iVar3 > iVar8)），我们 OathExtraReplays 只算一次再 for i<=… | 差一跳 |  |
| 436 | IsValidSpellTarget(CardScript spell, CardScript target) | 保持 B：原版 IsValidSpellTarget 71 行含 ①tutorial 许可闸(CheckIfPlayerActionPermittedInTutorial 5) ②spell.targeted 前置 ③异侧 stealth(0x46)/ward/camouflage 三道 ④该卡… | 差一跳 |  |
| 445 | MulliganFallbackCountdown() | 保持 B（差一跳）：原版 RequestMulliganResend = 请**对端重发它的换牌**，我们 TickMulliganFallback 落成「自己重发自己的」（NetBattle.cs:1967-1968 自述「本地等价」）⇒ 丢的若是对端的包，这一条修不回来；ConfirmMul… | 差一跳 |  |
| 457 | PlayAi() | 保持 B：MoveNext（246 行）的等待/超时/中断三档未逐段对读；账里落点 :18841 已不是 DriveAiTurn（真声明 = BattleDriver.cs:18990）⇒ 证据不足、落点还要重钉 | 差一跳 |  |
| 469 | ReadyToUseActiveAbility(CardScript parentCard, bool isPlayerAsking, bool showTips) | 保持 B（差一跳）：原版 ReadyToUseActiveAbility 的闸 = ①归属侧 == isPlayerAsking ②allowPlayer/EnemyActions 标志 ③card.IsAttacking ④按侧 PlayerManager.HasEnoughMana（含 ps… | 差一跳 |  |
| 476 | RecordAttackPendingDamage(CardScript actingCard, CardScript attackedCard, AttackTypes attackType, int actionPriority, bool recordReturnDamage) | 保持 B：存储形态差异未消（原版登记在 CardScript 待结算缓冲、按 DamageInfo 逐条；我们返回 List<int> 不落到卡实例）；另账里落点 :8234 是注释行，真声明 = RuleCore.cs:8252(AttackDamageEntries) ⇒ 落点要重钉 | 差一跳 |  |
| 483 | RemoveSelfBuffsFromQueues(CardScript card) | 保持 B：引擎侧 CancelEffectsFromDisappearingUnit（含 shouldIgnoreDestroy=1 语义）全仓无实现，表现层 RemoveSelfBuffsFromQueues 只撤四条道（BattleDriver.cs:34731 自述「另一半等引擎批」）；账… | 差一跳 |  |
| 490 | ResolveAction(BattleAction battleAction) | 保持 B：队列四条道/取序/Resolving 闸已对（见 ResolveActionFromQueue 那行），但 ResolveAction 那张按 actionType 的大 switch 分支表仍未逐值对「我们哪一处入队口承担它」⇒ 证据不足；另 ActionQueue.cs 记的 27… | 差一跳 |  |
| 493 | ResolveAddEffect(BattleAction battleAction) | 保持 B（差一跳）：原版 ResolveAddEffect = targetCard.AddEffect(cardEffect) → ChallengeLog.LogAddEffect → buffType==poisoned 时 BroadcastUnitPoisoned(290) → Fin… | 差一跳 |  |
| 503 | ResolveClearEndOfTurnEffectsFrom(CardScript actingCard) | 保持 B：原版按卡逐条 ClearEndOfTurnEffects（SendRemoveEffect 入 DelayedInstant 道）+ Finish；我们在回合末对双方所有单位 RevertBuffs 一锅扫；「谁入队 clearEndOfTurnEffects(61)、传哪张卡」仍未证 | 差一跳 |  |
| 505 | ResolveCreateDeckCard(BattleAction battleAction) | 保持 B：原版 ResolveCreateDeckCard 是「建卡→演出→MoveToDeck→AddCardInPlayToDeck→(spellType==150 则 BroadcastSecretCreated)→Finish」多跳；我们 DoCreate 的 decktop 支只 Ne… | 差一跳 |  |
| 506 | ResolveCreateHandCard(BattleAction battleAction) | 保持 B：原版 ResolveCreateHandCard 含 spellType==230 的 Scaffold 信号 + BroadcastSabotageCreated + DisplayEnemyCardInCenter，以及 BroadcastCardCreated（+0x90 附带一… | 差一跳 |  |
| 509 | ResolveDiscardFromHand(BattleAction battleAction) | 保持 B：原版是独立 action（可被队列取消/排序），我们在进手牌那一刻同步判上限并弃（RuleCore.cs:3166）；净结果同、中间态与取消语义不同 | 差一跳 |  |
| 516 | ResolveFatigue(bool playerDrawing) | 保持 B（差一跳）：原版疲劳走 DealMultiDamage(DamageInfo 量=疲劳值) → ReceiveDamage 完整管线；我们 MoveDeckTopToHand 的疲劳支绕开 ApplyDamage 直接扣督军血（RuleCore.cs:3069 起注释自述「不吃护盾/护甲… | 差一跳 |  |
| 517 | ResolveFollowAfterResolution(BattleAction battleAction) | 落点不是等价物：`EmitFollowUpResolution` 只发表现事件（`EmitCardTrigger` → `EvtKind.Trigger`，⛔ 不结算效果），原版这一跳是 `CardScript.FollowUpResolutionPlayed` 的 `OnTrigger(0x1… | 差一跳 |  |
| 522 | ResolveManaBuff(BattleAction battleAction) | 逐档对表已做（+0x1b 灵魂石无卡级广播 / +0x1c 信仰一次广播取英雄卡 / +0x1d 任务点逐点广播+来源卡三态 / 能量 create·refill 两档）——旧「逐档对表未做」已过期；残差：createMana∧refillMana 同时为真的 3 张（Path of the S… | 保持 B（证据不足） | 审查 WEAK：还差：原版 ResolveManaBuff 按 ManaBuff.friendlyPlayer(+0x14)/enemyPlayer(+0x |
| 523 | ResolveMinionDeath(BattleAction battleAction) | 仍差「重组动作」那一条：原版 `_ResolveMinionDeath_d__454__MoveNext.c:56-62` 三连（`MinionManager.RemoveMinion` → 从 `+0x470` 摘 → `AddReassembleMinionsOrder`，7 号动作），我们… | 差一跳 |  |
| 527 | ResolvePlayActiveAbility(BattleAction battleAction) | 仍差一跳：原版末尾 `:583 ResolveActiveAbilityPlayed` → `:586 CardScript.UsedActiveAbility`，后者还有 `BroadcastUnitDuty`(0x4c4) / `PayActiveAbilityCostOath`(0x4fb… | 差一跳 |  |
| 533 | ResolveRemoveEffectsFrom(CardScript actingCard) | 作用面仍不等价：原版广播器三趟 = 棋盘 `IsInPlay` 全员 / 当前方手牌（都调 `CardScript.RemoveExtrinsicEffectsFrom(card)`）+ 对方手牌（带 actingCard）；我们 `RevertGrantsFrom(source)` 是**按来… | 差一跳 |  |
| 534 | ResolveRemoveFromCemetery(BattleAction battleAction) | 无落点（过不了闸）：原版实体只有 `CardScript__RemoveCardFromCemetery.c:5` 的 `+0x24b = 0`（表现层「正在从墓地取出」标志），本仓已判「我们没有这个中间态、无等价落点」；账里指的 `RemoveFromSource` 是「从某区域取一份」，不是它 | 保持 B（无声明可落） |  |
| 546 | ResolveTransformFromRemnant(BattleAction battleAction) | 重放清单已对齐且带断言：`RuleEngineTest_W12.W12ReanimateSkipsDeployBroadcast` 四档（570/20 照发、195/190/605 不发），残骸与墓地两支同输出面；手牌 `UpdateCardEffects` 两跳属表现层（我们费用/效果现算，无… | 保持 B（有实质缺口） | 审查 NO：落点行 EffectResolver.cs:15450 是 DoReanimate 的文档注释，声明在 :15481；另：原版回调顺序 Re |
| 548 | ResolveTransformUnit(BattleAction battleAction) | 落点只覆盖手牌那一支：`DoBecome` 是 `chosenAbility 90`（GetCardToSummon+AddTransformCard）的**手牌战略卡**变身；原版 `ResolveTransformUnit`（2252 行，action 0x1b 的解析器）还含**在场卡**… | 差一跳 |  |
| 558 | ResolveTriggerRequiem(BattleAction battleAction) | 实现逐句对得上（`IsInPlayOrDying` 闸→430→435 三跳+接收方自排除），但**全仓无调用点**：原版由死亡收尾那条 `AddTriggerRequiem` 排动作触发，我们只有声明没人调 ⇒ 机制没接线（建议归「差一跳」：接调用点即可升 A；池内 requiem 0 张 ⇒… | 差一跳 |  |
| 576 | SetUnitOrderInPlay(CardScript card) | 57 行三件事已核清（写号/自增/登记）；`orderInPlay` 那位由调用点写 `UnitState.PlayOrder`；原版 `ObscuredInt` 只是反作弊包装，值语义相同 | 保持 B（证据不足） | 审查 WEAK：还差：第 3 步 RegisterCardInPlay 不只是「追加进场表+按进场序排」——它还发 inBattleId（BattleCar |
| 592 | ShouldActivatInitialEnchantments() | 落点不是等价物、判据也记错：`ShouldActivatInitialEnchantments` 读的是 **MatchData.matchType(+0x1c) ∈ {0x32=50 PracticeOffline, 0x50=80 EventAI}**（不是「战役关卡 id」，`RuleCo… | 差一跳 |  |
| 603 | ShuffleDeck(List<CardScript> deckToShuffle, bool checkStratagem) | 「钉位支只在轮抽使用」**不成立**：`CreatePlayerDeck.c:90` 在**每局非教程开局**都用 `checkStratagem=1` 调它（唯一门=ShouldShuffleDeck(matchType)）；钉位条件=`rawCardData+0x228 != 0`（字段名未… | 差一跳 |  |
| 604 | ShuffleDeck(bool isPlayer) | 引擎侧（算法+`GetRandomInt` 预烧表）已对齐，但**末尾那一跳没有落点**：原版把洗后首张 `SetActive(true)+SetupInDeck`、其余隐藏；本仓 `BattleDriver.cs:35018` 自述「SetupInDeck 无落点」；账里引的 `CardVie… | 保持 B（无声明可落） |  |
| 607 | StartBattlePhase() | 落点错位：`StartBattlePhase`（299 行）是**开局序列**（隐藏 UI → AddGoesSecondCardToDeck → ShouldActivatInitialEnchantments ⇒ TriggerOtherCardAbility → 督军 `+0x148` 加… | 差一跳 |  |
| 617 | TryUsingActiveAbility() | 原版这一跳是 UI 分发口（`waitingToResolveActiveAbilityFlag`+`ExecuteActiveAbility`），我们拆成「引擎闸+结算」与「BattleDriver 选目标/提示」两半，语义一一对应 | 保持 B（有实质缺口） | 审查 NO：落点名不符实：UseAbility(:11562)/CanUseAbility(:11156) 是我们自定 `Ability:` 关键词那条 |
| 618 | TryUsingActiveAbilityOfCard(CardScript card, bool forceSelectCard) | 原版两方法体除 targetCard 来源与 forceSelectCard 外逐字节同形；我们按 p/slot 定位同一张牌，forceSelectCard 对应的「选中态」在表现层 | 保持 B（有实质缺口） | 审查 NO：同上：落点仍指向 `Ability:` 那条路（UseAbility/CanUseAbility），原版 TryUsingActiveAbi |
| 624 | WaitOpponentLastActions() | 已读全 `d__316` 与 `ShouldWaitForOpponentLastActions`：等待条件 =「`+0x2cc` 的 ObscuredBool ∨ ActionsLeftInQueue>0」，每 0.1s 轮询、上限 12.5s；我们联机走 `NetBattle` 的 MsgA… | 差一跳 |  |
| 643 | BroadcastTurnStart(BattleManager manager, bool isPlayerTurn) | 逐句核后：池内 30×31 交集 0，且 31 的「仅卡主回合」已由卡面 view!=each 闸等价实现；唯一残留＝原版同一回调连发 30→31、我们按名二选一 ⇒ 差一跳（出现双名单卡才现形），保持 B | 差一跳 |  |
| 654 | BroadcastUnitMob(BattleManager manager, CardScript unitMob, CardScript unitMobCaster) | 订正旧差异：唯一调用点 CardScript__ResolveUnitAttacked.c:142 传 unitMobCaster=0（恒 null），自排除实际由接收方 TriggerOtherCardMob 的 other!=self 完成，与我们按 subject 自排除等价；真差异＝有 … | 建议改判据有问题 / 候选 D |  |
| 655 | BroadcastUnitObtainedHuntMark(BattleManager manager, CardScript unitMarked) | 逐句同形（预调＋自排除＋双手牌）；真差异＝原版棋盘跳无 IsInPlay、待死单位照收，我们快照跳待死且 GetsHuntMark 不在放行名单 ⇒ 保持 B（差一跳） | 差一跳 |  |
| 661 | BroadcastUnitRegiment(BattleManager manager, CardScript unitRegiment, CardScript unitRegimentCaster) | 订正旧差异：caster 恒 null（CardScript__ResolveUnitAttacked.c:164 传 0），自排除由接收方 other!=self 完成、与我们等价；真差异同 Mob 族——有 regiment 词条而无正文的卡（AM31/AM8/AM32 等）原版仍广播 64… | 建议改判据有问题 / 候选 D |  |
| 677 | CanTriggerAbility(AbilityTrigger trigger, BattleManager manager, CardScript thisCard, CardScript actingCard, CardScript targetCard, bool ignoreRandomTrigger) | 差一跳：六道闸里第 ③ 条「只在 actingCard==targetCard 时才查阵营资源」这个前置我们没建模（600/605 的资源门槛全落在 CardDef.FaithOps 付费组，不分目标）；其余五道与 ⑥ 的短路次序已逐句对上 ⇒ 保持 B | 差一跳 |  |
| 707 | CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool keepStats) | 仍证据不足：350 行只逐句核了「状态初始化」那一段（`UnitState` 构造 + `ApplyDeployTurnState`，约 60/350），`BattleCardUI.BattleCardSetup`/卡面刷新/`keepStats` 那一支（表现半边）落在 `CardPresen… | 差一跳 |  |
| 709 | CardWaitingToBePlayed() | ⛔ 无落点：我们的引擎与视图都不存 cardState（`BattleDriver.cs` 多处自述「我们的视图不存卡状态」、按归属现算）⇒ 同形/同义的声明不存在，过不了闸；行为面（纯瞬态表现态、无逻辑读者）已核等价 ⇒ 保持 B。 | 保持 B（无声明可落） |  |
| 720 | CheckReplacedAbility() | 池内 `buffType==40 (replaceAbility)` 现扫 0 条（cards_truth.json 1,351 张）⇒ 今天行为等价；但原版该方法是**遍历 activeEffects 的运行时重算入口**（不是取值器），§4.1 例外不适用，我们也没有 recompute 入… | 差一跳 |  |
| 724 | ClearPendingDamage() | ⛔ 无落点：我们没有 pendingDamage 台账（判死现算，见 EnoughPendingDamageToDie :7721），也没有「清空缓冲」这个动作 ⇒ 同形声明不存在；原版该方法做的事在我们模型里没有对象 ⇒ 保持 B。 | 保持 B（无声明可落） |  |
| 725 | ClickMulliganSelected(bool fromClick) | 落点需重写：`MulliganPanel.cs:1437（ToggleMark）` 是真声明（保留），但 `RuleCore.cs:455-462` 是注释块。四道闸现核：mulligan 期/本地玩家=面板生命期结构性满足；Replay 模式我们不存在；`0x1e(30)` 档在批量入口 `R… | 差一跳 |  |
| 740 | ExecuteSynapeEffect(CardScript actingCard) | 落点行漂移：`RepeatTacticOnAdjacent` 现在在 `EffectResolver.cs:2865`（账里 2627-2685 是 `PlayTacticCore` 的正文段）。等价面成立（逐邻 + 同一套目标闸 + 重跑战术正文），但**入口不同**未闭合：原版重放的是 `s… | 差一跳 |  |
| 742 | FinishAfterAttack() | 两处原落点都过不了闸：`RuleCore.cs:6241-6252` 是 `DeclareAttack` 的注释块、`BattleDriver.cs:22467-22520` 是命中回调里的语句。真落点 = `CardFeel.cs:2264（AttackRecover，位/朝向/缩放回正）` … | 差一跳 |  |
| 771 | HasDiedSinceLastTurn() | 落点可改成 `ConditionEval.cs:605（DiedInTurn）`（那才是这个谓词，:333 = `DiedInTurn(0)||DiedInTurn(1)`），但有一条**真差异**：我们「活着的单位直接 false」(:332) 且残骸翻面**不进 DeadUnits**，而原… | 差一跳 |  |
| 772 | HasDiedThisTurn() | 同上：落点可改成 `ConditionEval.cs:605（DiedInTurn）`（:326 = `DiedInTurn(0)`），但同一条真差异（活着恒 false / 残骸不进 DeadUnits）未消 ⇒ 保持 B。 | 建议改判据有问题 / 候选 D |  |
| 774 | HasEnoughPendingDamageAndIsRemnant() | 落点改指 `CleanupDeaths`（:9535 声明 + :9858 那一行判定）：原版那个具名谓词我们没单独立函数，是内联（不是缺失）。 | 保持 B（有实质缺口） | 审查 NO：:9858 判的是 Has(Remnant)‖Has(Waystone)，那是兄弟方法 HasEnoughPendingDamageAndH |
| 799 | IsTheSameTurnPlayed() | 落点必须写成「方法 + 内联行」：谓词在我们这边没有单独函数，是 `CanUseOathAbility`(:11726) 与 `FireTriggerOnSide`(:13410) 各内联一处（§4.1「同一段代码服务多个原版方法」那一条）；两条残留（`playedIntoFieldTurnCo… | 保持 B（有实质缺口） | 审查 NO：两条额外入口也写 DeployedTurn：免费部署 RuleCore.cs:4822、残骸复活 EffectResolver.cs:155 |
| 801 | IsValidActiveAbilityTarget(CardScript target) | 落点行漂移 11349→11367（11349 是注释行）。E3 那条「camouflage 与 0x46 两格没核」**已结案**：`EntityScript__get_isCurrentlyWarded.c` 全文只有 0x8c/0x91 两次 HasCurrentTrait（无 camou… | 保持 B（证据不足） | 审查 WEAK：五道闸（IsInPlay/潜行只异侧/warded 只异侧/summonRift 不分侧/tainted+vanguard+Hero）逐条对 |
| 820 | PrepareAttack(bool toPrepareAttack, bool playSound) | 原落点两处都过不了闸（`RuleCore.cs:6144-6245` 注释块、`BattleDriver.cs:22467-22520` 语句）。真落点分散：起手音 = `BattleDriver.cs:19575`（`PlaySignal` 的 EvtKind.Attack 支）、摆位/回正 … | 差一跳 |  |
| 830 | ReactToUnitHealed(CardScript healedUnit, CardScript actingUnit) | 落点订正：`EffectResolver.cs:15778` 是 **`BroadcastCardReturnedHand` 那一段的 doc 注释**，不是 `BroadcastUnitHealed`；真声明 = `EffectResolver.cs:15849（BroadcastUnitHe… | 差一跳 |  |
| 840 | RecordPendingDamage(DamageInfo newDamage) | ⛔ 无落点：原版是「先记 pendingDamage 缓冲、攻击前后清」的两段式；我们伤害**立即落地**、判死现算(:7721) ⇒ 缓冲这个对象不存在，同形声明也没有。消费者全是内部判死/AI（无卡面可观测）⇒ 保持 B（不是缺功能，是结构不同）。 | 保持 B（无声明可落） |  |
| 855 | RemoveFromGameCard(CardScript removedCard) | 落点重写（账里的 `BattleContext.cs:1513` 只是 Removed 容器、`RuleCore.cs:9688-9760` 是别的方法）：真落点 = B88-E1c 专建的 `MarkRemovedFromGame`(:172) 与 `BroadcastRemoveFromGa… | 保持 B（有实质缺口） | 审查 NO：第一分支没落地：MarkRemovedFromGame 全仓 0 调用点、UnitState.RemovedFromGame 0 读点；视图 |
| 879 | SendClearEndOfTurnEffects(int priority) | 原落点两处都过不了闸（`EffectResolver.cs:6600-6620` 是回手 op 语句、`RuleCore.cs:2448-2465` 是临时控制归还）。原版该方法**只组 action 0x3d 入队**；我们的 `ActionQueue`(4 道 + priority) **没… | 差一跳 |  |
| 880 | SendRemoveEffect(CardEffect cardEffect, int priority) | 同上（只组 action 0x10 入队的那一层我们没有）：原落点 `RuleCore.cs:1905-1926` 其实是**回合末到期清除**的代码（与 879 同族）、`:11720-11730` 是一次性标记消费；真正的「摘一条效果」= `UnitState.cs:2816（RemoveE… | 差一跳 |  |
| 887 | SetAsPlayer(bool isPlayer, CardArmy army, bool setCardback) | 原落点两处都过不了闸（`BattleContext.cs:537-545` 是 `TempControl` 注释、`BattleDriver.cs:24843-24847` 是 `PlayTraitPulse`）。我们只有「归属 = 待在谁的 `PlayerState.Board[]` 里」+ … | 差一跳 |  |
| 893 | SetCardState(CardStateOptions newState) | ⛔ 无落点：原版是 18 档卡状态机的唯一写入口，我们视图按「在哪张表/哪个布局」现算、**不存** cardState（`BattleDriver.cs:3983` 等多处自述）⇒ 同形声明不存在；建议维持 B（若认定「视图状态机不打算建」则应改判 D）。 | 保持 B（无声明可落） |  |
| 898 | SetHealthToZero() | 原版这一句是「裸置血 0」，我们没有这个口（置零只发生在 `Hurt`→`CleanupDeaths` 链里，⛔ 不能拿 Hurt 顶替）；且**三源 0 命中**：decomp_full 里只出现在自己的定义文件、dev_ilspy 只有定义、Unity 侧（prefab m_MethodNa… | 差一跳 |  |
| 907 | SetStateAsMulligan(bool toBeDiscarded) | 落点现读是 SetReplayPaused（BattleDriver.cs:8407-8412，无关方法），第二个落点 MulliganPanel.cs 连行号都没给；我们的换牌等价物拆在 MulliganPanel.Open:965 / BuildSelectedMarkers:1170 / … | 差一跳 |  |
| 911 | SetupActiveAbility() | 落点 11250-11360 现读是 AbilityPickSideOf（候选侧判定，与原版无关）；原版整个方法只有一句 cardState=3（表现层状态位），我们对应入口是 CanStartAbility:11124 / CanUseAbility:11156，没有任何声明承担「把卡置成激发… | 差一跳 |  |
| 912 | SetupAttack(bool playSound, bool playerInitiated) | 落点 6180-6240 现读是 TryCreditKill（击杀记账，无关）；原版三件（cardState=0x11 / 记 playerInitiated / StartAttackFrom+播音）在我们这边拆成 RuleCore.DeclareAttack:6269 与表现层 Attack… | 差一跳 |  |
| 926 | ShowDropPodFigures() | 落点 7670 现读是 DamageAfterReductionList（伤害归约，无关）；原版是**查询**「该不该展示空投数值」（返回 bool），我们是**动作** OpenDropPod:7896（回合开始即换值）⇒ 职责不同，且原版该 bool 的调用点未扫 ⇒ 保持 B。 | 差一跳 |  |
| 947 | TriggerOtherCardAbility(RawCardScript abilityCard) | 落点 ApplyAbilityFamilyGate:237 只是「按能力族筛行」的闸，不是「让另一张卡跑它自己的 ThisCardPlayed」；真正的等价物是 ResolveOps(… abilityFamilyCard: card, abilityFamilyTrigger: "ThisCa… | 差一跳 |  |
| 958 | TriggerSpiritStone(CardScript actingCard) | 三个落点现读分别落在注释行（14470-14500）、abilityRepeats 局部区（7216）、触发名→编号表（CardDef.cs:1930-1945，不是「取触发能力」）；真实等价物拆成 EmitAbilityUseTrigger:8358（600 自触发）+ BroadcastUn… | 差一跳 |  |
| 960 | TriggerUnitBacklashActions(UnitDeathType deathType, CardScript actingCard) | 落点现读是 CheckIfDead 的 Survivor 段（9648-9690，注释与语句）与 DeathTypeOfSource（9473-9493，无关）；真实等价物 = FlushDeaths:10319 + CleanupDeaths 里的 FireTriggerAt(KeywordT… | 差一跳 |  |
| 964 | TriggeredEarnSkull(CardScript earnedWarlord) | 落点 15806 现读是上一个方法体内（BroadcastEarnedSkull 现读在 15877，行号漂 +71）；接收侧（TriggeredEarnSkull → OnTrigger(640)）我们只有数据驱动监听器、无方法级载体（同 944/1081 口径）⇒ 保持 B。 | 差一跳 |  |
| 989 | GetTrait(DefinedTrait traitName) | 无落点：原版返回 CardTrait 资产（icon 三列 + aIScore + customObject + traitEffects 全在同一个对象里）；我们把它的字段拆进四处（TraitFrames 效果表 · Badges 图标键 · AiScoring.TraitAiScore · … | 保持 B（无声明可落） |  |
| 992 | GetTraitEffects(DefinedTrait trait) | 无落点：真值表按档拆成四个口（BindingsFor / StartBindingsFor / TraitParticles），没有「取某 trait 全部行」的单一访问器；且原版唯一消费者 PreloadTraitEffect（逐条 AssetReference.Load + PreloadA… | 保持 B（无声明可落） |  |
| 994 | GetTraitFromAbilityTrigger(AbilityTrigger abilityTrigger) | 落点错 + 判据代理不等价：账里记的是名字表，真实现是 AbilityIconAnim.TraitOfTrigger（CardPresentation/Battle/AbilityIconAnim.cs:96）；它判「名字在不在 DefinedTrait」用的是 trait_effect_bin… | 差一跳 |  |
| 996 | Init() | 无对应物：Init 建 trait→CardTrait 全表（73 条，GetTrait 的全部前置）；我们的 TraitFrames.EnsureBindings（TraitFrames.cs:325）只载「有 TraitEffect 的 62 个 trait」的分档绑定表，没有 trait→… | 差一跳 |  |
| 1007 | IsPlayer() | 无落点：原版 DamageInfo 是个 DTO（actingCard + deathType + IsPlayer()），我们引擎没有这个结构 —— 伤害来源走 RuleCore.ApplyDamage 的 owner 实参、死因走 BattleContext 字段，没有「按伤害信息问属于哪一… | 保持 B（无声明可落） |  |
| 1026 | GetActiveAbilityCard() | 等价物 = 属性直读（CardDef.Ability）；原版第二来源 +0x118（replaceAbility）在池内 buffType==40 现扫 0 条 ⇒ 恒取自己那份，净等价（§4.1 纯取值器例外） | 保持 B（证据不足） | 审查 WEAK：+0x118（buffType 40）现读 0 条属实（cards_truth.json 1,351 张全扫）；但落点 CardDef.Ab |
| 1041 | HasCurrentTraitActiveEffect(DefinedTrait trait) | 落点 Has 比原版宽：Has 读合并后的 _keywords（含卡面印刷关键词/光环/手牌加成），原版只扫 activeEffects 的 addTrait/addTraitRemnant ⇒ 印刷带该词的卡会假真；正确形状是记录账 TraitEffects(UnitState.cs:2960… | 差一跳 |  |
| 1053 | RevealedSecret(RawCardScript enchantment) | 落点错位：RevealSecretOrders 对应的是 BattleManager.ResolveRevealOrders 那一族（展示/执行/摘 singleUse），不是 EntityScript.RevealedSecret；原版该方法只把该附魔的 secretBuff(+0x90) 置… | 差一跳 |  |
| 1065 | GetBotDeck(MatchType matchType) | 数据源不同（原版按 matchType 走 AIBotsConfig/活动 config 的 botDecks；我们读本地 prebuilt_decks 按模式筛）＋183 行 switch 未逐条对拍 ⇒ 保持 B | 差一跳 |  |
| 1078 | SetBotMatch() | 判据有问题：原版 SetBotMatch 全库无调用点（decomp_full 与 out_cpp2il_dev_ilspy 都只有定义），四档翻转实际内联在 SetBotOpponent；我们 MatchTypes.Effective 对齐的是 SetBotOpponent 那张表 ⇒ 本行不… | 差一跳 |  |
| 1079 | SetBotOpponent(CardDeck enemyDeck) | 在线半（playfabId/匹配/FeatureConfig）属 P3；离线半（对手牌组＋四档翻转）由 BeginFromDeckLibrary＋MatchTypes.Effective 承担，但未逐句对拍（含 DeckAndWarlordData 的账户/化妆字段）⇒ 保持 B | 差一跳 |  |
| 1080 | SetBotOpponentDeck(PrebuiltDeck botDeck, PlayerDataManager playerData) | 洗牌闸已按原版分开判（NewBattle 的 shuffleP1Deck/shuffleP2Deck，100 与 140 都不洗）；残留：入口窗仍未把 matchType 传给 NewBattle（140 不可达、今天行为等价），且 DeckAndWarlordData(botDeck,play… | 差一跳 |  |
| 1082 | SetPlayerData(MatchType matchType, PlayerBattleData playerBattleData, CardDeck playerDeck) | 原版这一步＝写 playerPlayfabId 后转调 SetPlayerDeck（账户字段属 P3）；我们无对应转调，牌组装在 BeginFromPendingCore/BuildPlayer ⇒ 落点不同层，保持 B | 差一跳 |  |
| 1083 | SetPlayerDeck(MatchType matchType, PlayerBattleData playerBattleData, CardDeck playerDeck) | 牌组半（DeckAndWarlordData→PlayerState）＋洗牌闸已等价（BuildPlayer＋shuffleP1Deck）；但原版同方法还写 6 项化妆/账户字段（avatar/title/cardback/elo/alliance/badge）在另一域 ⇒ 保持 B | 差一跳 |  |
| 1093 | ActivateMinions(bool isPlayerTurn) | 落点 3448-3458 现读是 DisplaySummonSickness 与其上方注释（RecomputeActAndAttack 现读 3531）；等价物是 BeginTurnCore 内的逐格循环（2070-2075，语句级）而不是独立声明 ⇒ 无方法级落点，保持 B。 | 差一跳 |  |
| 1094 | AddMinionOccupation(CardScript card) | CountOnSide:78 只是**读**半边（数人数）；原版是把 true 追加进该侧平行占位表（写），我们的对应写发生在 BoardSlots.Insert:307 的 Board[slot]=u（同时插推/换格）⇒ 记账与插入合并、没有独立的「占位记账」声明，保持 B。 | 差一跳 |  |
| 1101 | Awake() | 落点 ArenaSlots.Position:98 只承担公式那半（18 格 18/18、Δ≤2.5e-7）；Awake 另有三件无对应物：读 MatchData.GameplayData.slotsPerSide（我们编译期常量 4）、建 4 个 List、cardShadow.SetActi… | 差一跳 |  |
| 1102 | BroadcastActivateMinion(bool playerTurn) | 三个落点现读全是注释或无关：2052-2056 是 BeginTurnCore 里的注释、UnitState.cs:1732-1774 是 IsAlive/RemoveBuffsFromCard 区（RefreshForNewTurn 现读 1782）、3493-3502 是 Recompute… | 差一跳 |  |
| 1103 | FillMinionPositions() | 落点 ArenaSlots.Position:98 公式 6 句逐字同、18 格 18/18 一致；但该方法在原版 decomp_full 与 ILSpy **全树 0 调用者**（可能被内联进 Awake）⇒ 判据侧链路未定，属有疑点；若调度台裁定「公式等价即可算 A」可改判 A。 | 差一跳 |  |
| 1106 | GetCardInPos(int indexInPlay) | 无落点（账里本就写「—」）。原版按**有符号 indexInPlay** 取卡（<0 走左表、>0 走右表、==0 警告返回 null）；我们只有 BoardSlots.SlotOf:72 与 OriginalIndexOfCard:528 这对映射、调用方直接 Board[slot]，没有「有… | 保持 B（无声明可落） |  |
| 1107 | GetClosestAvailableSlot(Vector2 pos) | 落点 BoardSlots.Resolve:144 只做「请求格号 → 插入侧/下标」；原版是「屏幕坐标 + 合法插入区间 → 最近格号」（.c:95-143 含 WorldToScreenPoint 与平方距离），屏幕那半在我们这边散在拖拽层、距离度量也不同（我们 x 向 + y 阈）⇒ 保持… | 差一跳 |  |
| 1114 | GetMinionList() | 落点 PlayerState.UnitsByPlayOrder:206 **全仓只有声明、0 调用者**（现扫 Assets 仅 1 处命中）⇒ 未接线，不能算复刻；另原版扫 leftMinions/rightMinions 是否含督军仍未核 ⇒ 保持 B。 | 差一跳 |  |
| 1121 | GetNextSlotPos(Vector2 pos, int nextSlotRequired) | 落点 BoardLayout.TryResolveSlot:487 是「位置 → 最近格号」；原版是「位置 + 需要几格 → 下一个可用格的位置」（含两侧占用比较、满盘回督军锚点 .c:266-273、找不到 LogError+回右最外格 .c:150-152），多格/占用半在引擎 BoardS… | 差一跳 |  |
| 1124 | GetOwnPositionInBoard(CardScript card) | 落点 BoardSlots.OriginalIndexOfCard:528 只给格号；原版同一方法还返回**世界坐标**（GetLeftSlotPos/GetRightSlotPos），坐标那半在表现层 ArenaSlots.Position ⇒ 拆两处、非同一方法，保持 B。 | 差一跳 |  |
| 1138 | PositionWillBeAtLeft(int targetPosition, CardScript refMinion) | 无落点（账里写「—」）。原版判「目标位是否在参照单位左侧」：督军特档 targetPosition==-1，其余按参照在左/右表的下标 ±1（.c:23-64）；我们的教程匹配 TutorialScript.Matches:1275 用的是**绝对半场**判据（TargetSlot < Warl… | 保持 B（无声明可落） |  |
| 1144 | SlotPosToBoardPos(Vector3 targetPos) | 落点 ArenaSlots.Position:98 把 up×ExtraYOnBoard 并入返回值 y（不是独立一步），且该方法在原版 decomp_full/ILSpy 全树 0 调用者 ⇒ 所属链路未定、「并入」是否等价仍有疑点，保持 B。 | 差一跳 |  |
| 1156 | CompleteMulliganPhase() | 分工不同且有覆盖缺口：方法体＝逐卡送入 currentHand(+0x50)＋逐卡 UI 复位(SetStateInHand/ToggleOverrideSorting/SetSortingLayer/FinishMulliganUi/ToggleTooltips)＋重建空表＋起收尾协程；我们拆… | 差一跳 |  |
| 1166 | GetMulliganCardsToDiscardIds() | 线格式差异仍在（原版发卡 id、我们发手牌下标），联机协议只能两端自洽 ⇒ 保持 B；要与原版协议互通须改线格式 | 差一跳 |  |
| 1175 | IsRawCardInHand(RawCardScript card) | 无落点：原版按 RawCard 模板身份扫手牌；我们手牌是 CardInstance、IsInHand 比的是实例身份，全仓没有任何「某张卡模板在不在手里」的声明 ⇒ 过不了闸，保持 B | 保持 B（无声明可落） |  |
| 1178 | MoveCardToMulliganPos(CardScript newCardDrawn, int cardPos, int totalCards) | 落点只在表现层（MulliganPanel），卡宽/间距来自实拍而非序列化值；父链 lossyScale≈0.794 的差额仍未逐节读 ⇒ 差一跳（需实机复测），保持 B | 差一跳 |  |
| 1188 | ReplaceMulliganCard(CardScript cardsToDiscard) | 原版逐张：挂回牌库尾＋ToggleOverrideSorting＋FinishMulliganUi，随后收尾协程「先洗双方牌库」；我们整批弃回→洗→补抽 ⇒ 净集合同、牌库排列与后续抽牌序不同 ⇒ 保持 B | 差一跳 |  |
| 1189 | SetMulliganCardsToDiscard(int[] mulliganCardsToDiscardId) | 原版这一步只是「记录选择＋逐张置 13/14 态」，我们一次性提交下标、勾选态在 MulliganPanel ⇒ 中间态与键都不同，保持 B | 差一跳 |  |
| 1190 | SetMulliganCardsToDiscardStart(int[] mulliganCardsToDiscardId) | 整批校验已复刻（MulliganCollectIndices），剩①键不同（原版卡 id／我们下标）②原版另一半「按 id 命中就 ClickMulliganSelected 打勾」在 MulliganPanel ⇒ 保持 B | 差一跳 |  |
| 1191 | SetupCardInHand(CardScript card) | 落点只覆盖判据②（引擎半：遍历手牌效果记录并 AddEffect）；判据①的界面四步（设父/排序层/ShowCardBack/+0x148）在 CardPresentation 另一域 ⇒ 保持 B | 差一跳 |  |
| 1209 | CanPlayAbilities(List<AbilityLogic> standardAbilities, BattleManager manager, AbilityData abilityData) | 三个落点现读都不是声明行（11138 是 CanUseAbility 的 doc、11349 是注释、11106 是 DeathWatchDeferred）；真实声明 = CanStartAbility:11124 / CanUseAbility:11156 / CanUseAbilityTar… | 保持 B（无声明可落） |  |
| 1217 | GetAbilitiesForTriggerRawcard(AbilityTrigger trigger) | 落点 CardDef.cs:1914 是**触发名→编号字典**（AbilityTriggerNumberOfName），不是「按触发取该卡能力列表」；真正的等价物候选 = CardDef.TriggerOps:1811（同 trigger 归一成一组），而原版返回**列表**（同 trigge… | 差一跳 |  |
| 1248 | GetValidTargets(BattleManager manager, CardScript thisCard) | 落点 5374 是 IsValidTarget 的 doc 注释行（声明在 5392）；且原版把「枚举全部合法目标」合成一个方法，我们只有单格校验（枚举散在调用方循环，HasAnyValidAttackTarget:1484 只给布尔）⇒ 没有返回列表的等价声明，保持 B。 | 差一跳 |  |
| 1300 | GetCardListFromIds(string[] enemyDeckIDs, List<RawCardScript> cardCollection) | 契约不同：原版返回与输入下标对齐、找不到也 Add(null) 的列表（调用方按下标取）；我们的 DeckLookup（CardDatabase.cs:3307）是单值 Func<id,CardDef>、调用方自己跳 null。唯一调用点 DeckAndWarlordData__SetupSer… | 差一跳 |  |
| 1356 | OnReset() | 无落点：原版 OnReset = Signal.Unregister(OnTriggerAbility)（注册/注销成对）；我们的 AchievementTally 一局一份、随 BattleContext 生死，没有注册层 ⇒ 没有可注销的对象。净结果等价（不可能遗留回调），但按 §4.1 落… | 保持 B（无声明可落） |  |
| 1374 | ShowPotentialTargets(BattleManager self, CardScript card, int mode) | 现扫：`CardPrefab.prefab:3086` 挂了 `CardHighlight.cs`（guid 2c4533f2…）⇒ 在场卡都带该组件（`StartSpellFrom.c:41`/`StartChoosingAbilityTargetByClick.c:29` 也按「非空才继续」… | 保持 B（证据不足） | 审查 WEAK：亮点只单位卡那一支 +「break 不可达」的论据只核了 CardPrefab.prefab:3086 一份（该 prefab 确实挂 Ca |

**结构分布（141 条的粗分）**：

| 类型 | 条数 | 含义 |
| --- | --- | --- |
| 无落点 / 架构层不同 | 22 | 原版有 `DeckManager` / `PlayerManager` / `MinionManager` / `RawCardScript` 这一层，我们的数据模型扁了 ⇒ **没有语义相当的声明可落**（不是没实现，是落不成方法） |
| 行为缺口（差一跳） | 68 | 具体差一步/一道闸/一个广播，**要改代码**才能升 A |
| 判据侧问题 | 8 | 原版这一支是死代码 / 恒 null / 恒 false ⇒ 建议改判「判据有问题」（其中若干候选判 D，须调度台裁） |
| 其余（数据/契约/证据不足） | 43 | 数据源不同、线格式不同、或审查判 WEAK（证据不足） |

**审查代理判 NO 而退回 B 的 11 条**（56 条候选 A 里剔掉的）：83（0 调用点死方法 + `MemberwiseClone` 行为未实现）· 169（缺「伏击召唤物 SetAmbush」与「召唤物 ⇒ 施法卡 TriggerOtherCardAbility」两跳）· 371（原版有 `CardScript.IsInPlay()` 状态闸，`PlayerState.Units()` 不过 — 与 350 同一理由，一致性订正）· 546（落点行 + 广播次序：`BroadcastUnitReanimated` 与 `ResetSummonSickness` 顺序相反）· 617 / 618（落点指向 `Ability:` 关键词那条路，不是 `activeAbility`；同批 304 已把该口落在 `RuleCore.cs` 的 `UseActiveAbility`）· 774（落点是「兄弟方法」的口径：含 waystone 那支）· 799（我们额外在免费部署/残骸复活也写 `DeployedTurn` ⇒ 效果召唤的卡能当回合用誓约能力，原版不能）· 855（`cardState=11` 那一支没落：`MarkRemovedFromGame` 全仓 0 调用点、`RemovedFromGame` 0 读点）

**审查判 WEAK（证据不足）5 条**：522（`ManaBuff.enemyPlayer` 第二趟「取反再跑一次」我们没建模，17 张环境卡 desc 为空、今天不可达）· 576（`RegisterCardInPlay` 还发 `inBattleId`，我们没有这一格）· 801（替代能力支取 `activeAbilityPickCriteria[-2]` 那格没落，SW21 是否被查询未定）· 1026（原版返回整张 `RawCardScript`，我们落的是 `CardDef.Ability`，10 个消费点未逐个对）· 1374（「`break` 不可达」只核了一份 prefab，残骸体/其它变体未证）

## 三、这批里「唯一阻塞原本是落点形状」的：42 条（= 本波升 A 的全部）

- **22 条的旧落点根本过不了闸**（注释行 13 · 语句中间 5 · 落点为空/无 `文件:行` 4）——这 22 条**不是没做**，是账里记的行号漂到注释/语句上了（`EffectResolver.cs` 在本波运行期间被别的写手改动 ≥2 次、单次 +15 行，漂移是实时发生的）。
- **11 条原本卡在「字段/属性不能当落点」这条口径上** —— 正是 §4.1（2026-10-27）那档例外解掉的（`Count` 属性 · `PlayerState.Warlord/Hand/Energy/MaxEnergy` · `UnitState.IsRemnant/IsLeavingRemnant/Prayed` · `CardDefExtraTraits.ExtraAbilityTraits`）。
- 因此 §4.1 例外在本波的实际收益 = **11 条**（另外 9 条本来就有方法落点，只是行号/名字要订正）。
- ⚠️ **反过来说**：141 条保持 B 的里，**只有 4 条**（156 · 725 · 830 · 964）属于「行为已在、但没有方法级载体 / 落点只是写错」——它们可以下一波快速处理；其余 137 条都有**真实的代码缺口或数据模型差**，⛔ 不是落点问题。

## 四、没核完的：0 条

183 条全部核过（`b6_out_1..7.tsv` 合计 183 行，无重复、无遗漏，已机检 5 列 × 183 行）。其中 5 条属「核了但证据不足」（见 §二 末），已如实标 WEAK，**没有冒充 A**。

## 五、附：核出来的账错 / 必须改代码的（**只报不做**）

### 5·1 落点指错东西（不是漂移，是指到了别的方法）—— 下一波改账时优先修

- 730 `DissolveMinionCard` 的等价物是 `CardPresentation/Core/CardFeel.cs:3705（MoveBackToHand）`，账里指的 `ReturnToHand` / `UnDissolve` 是反方向的另一条动画路（本波已按新落点升 A）。
- 855 `RemoveFromGameCard` 第一分支的真落点是 `RuleEngine/Core/RuleCorePartial_B88E1.cs:172（MarkRemovedFromGame）`，账里 `BattleContext.cs:1513` 只是容器。
- 411 `IsCardIncluded` 原版是「侧/卡种/残骸」过滤谓词（`.c:10-59`），**不查区域成员资格**；账里落的 `IsInDeck/IsInHand/IsInCemetery` 与它无关 ⇒ 那一行的差异列要整段重写。
- 412 `IsClosedDeckGame` 落的 `BattleContext.cs:2059（ClosedDeck）` 那一行其实是别的方法的注释，名字根本对不上。
- 1053 `RevealedSecret` 落的 `RuleCore.cs:7933` 是 cancel-attack 注释；`RevealSecretOrders` 在 :8130。
- 607 `StartBattlePhase` 落的落点是**开局序列**不是 `BeginTurnCore`。
- 860/879/880 一族：账里把「回合末到期清除」当成了原版那两条 action 的等价物（两者净结果可能等价，但落点名不对）。
- 456/459 一族的行号普遍偏向 doc 注释块（`EffectResolver` 最密集）。

### 5·2 差异列的**前提写错**（会误导下一波）

- 654 / 661：旧差异说「原版排除 caster、我们是 subject」⇒ 不成立 —— `CardScript__ResolveUnitAttacked.c:142/164` 传 `unitMobCaster = 0`（恒 null），自排除实际在接收方 `TriggerOtherCardMob/Regiment.c:14`，与我们一致。真差异是「有 Mob/Regiment 词条但无正文」的卡原版仍广播 645/646，我们 `FireTriggerAt` 无正文即早退。
- 1375：旧差异两条都不成立（双手牌两跳由 `BroadcastHandWhen/BroadcastHandTrapWhen` 覆盖；`triggers:sacrifice` 确实自排除）。
- 1139：旧差异「原版在结算时重排、我们在拖拽预览时重排」⇒ 不成立 —— 原版由 `BattleManager__Update.c:572-575` **每帧**调，时机与我们一致（本波已按订正后升 A）。
- 169：旧差异「召唤即附魔未实测」⇒ 可撤，唯一调用点两个可选载荷全传 0。
- 990 / 992：把 `TraitEffect` 误读成「机制表」，实测它只装动画绑定（`effectAnim` / `TriggerType` / `playIfWarlord` / `overrideByArmies`）。
- 233：判据要点「棋盘满(+0x350)」是误读 —— `+0x350` 是动作队列。
- 101：`GetDelay` 两个偏移写反（`+0x20 = waitBefore`、`+0x24 = waitAfter`）。
- 603：「钉位支只在轮抽用」不成立 —— `CreatePlayerDeck.c:90` 每局非教程开局都传 `checkStratagem=1`。
- 1078：`MatchData.SetBotMatch` 全库 0 调用点（四档翻转内联在 `SetBotOpponent`）⇒ 判据为空，建议并入 1079。

### 5·3 需要改代码才能升 A 的（按文件归拢，**只报不做**）

- `RuleEngine/Core/RuleCore.cs`：`FireTriggerAt` 的「无正文即早退」要对 Mob/Regiment 族放行（654/661）· `UseAbilityCore` 补 `UsedActiveAbility` 的 duty 广播/誓约付费/ferocity+`AddRecallToDeck`（527）· `ShuffleByRandomOrder` 加 pinned 档（603）· `CanStartAbility` 补 `allow*Actions` 三闸或写明「同步模型无此状态」（233/469）· `RecordAttackPendingDamage` 的 10/15 档从未产生（346）· `DeployFreeCore` 补 `SetAmbush` 与「召唤物 ⇒ 施法卡 TriggerOtherCardAbility」（169）
- `RuleEngine/Core/EffectResolver.cs`：`DoCreate` 的 decktop 支补 `spellType==150 ⇒ BroadcastSecretCreated`、hand 支补 `spellType==230` 与 `BroadcastCardCreated`（505/506）· `ApplyOneGain` 的 keyword 支补 `UnitPoisoned(290)` 广播（今天全仓 0 发射点，493）· `EmitFollowUpResolution` 只有动画事件，缺「410 真结算」口（517）· `ResolveTransformUnit` 缺在场卡支（548）
- `RuleEngine/Core/Aura.cs` + `UnitState.cs` + `Enchantment.cs`：建「按来源全场撤一遍」的口（533）· `IsThereDoubleOathEffect` 改成每圈重算 + 读当前 trait（427）
- `RuleEngine/Core/PlayerState.cs`：`Units()` 补 `IsInPlay` 等价闸（322/350/371 一族）· 加「按 8 个筛位枚举」入口（385/386/387）
- `RuleEngine/Core/UnitState.cs` + `ConditionEval.cs`：本回合死后变残骸仍要算「死过」（771/772）
- `CardPresentation/Battle/BattleDriver.cs`：`RequestTutorialSkip` 要过 `TutorialScript.CanSkipCurrentAction` 闸（115）· 补 `ShowStealth/StopShowingStealth`（820）
- `CardPresentation/Battle/AbilityIconAnim.cs:126`：`TraitExists` 的存在性判据要换成 `DefinedTrait` 全表（994，现在会让 landing/rage/requiem/sacrifice/support 五个返回 null）
- `CardPresentation/Core/TraitFrames.cs`：`PrefabFor` 目前只取 `rows[0]`，3/6 合一 ⇒ 要按 `TriggerType` 建表（383）
- `CardPresentation/Net/NetBattle.cs`：`RequestMulliganResend` 现在是自己重发自己，要真的「请对端重发」（445）

### 5·4 顺手发现的**新缺口**（不属本次 183 行，建议立账）

- **徽标漏画**：`CardDef.ExtraAbilityTraits`（25 对 trigger→trait 派生集合）**全仓零消费者**；`Badges.For`（`BattleDriver.cs:28024`）只吃 `u.Keywords` ⇒ 原版「ability 派生 trait 也进 7 格徽标」这一半没有落点，初算约 **30 对（卡, trait）** 会被漏画（`ASH69`/`UM69` strike · `SOR1/3/65` pray · `EC` 一族 ecstasy · `SAU` 三张 artifice · `SW27/34`+`AM72` slay · `SOR8/10/21/27/32` rage …）。
- **关键词 290（poisoned）全仓 0 发射点**（见 493）：`CardDef.cs:1929` 只有名字映射。
- 三处**过期注释**（铁律 5 该订正，会误导下个会话）：`UnitState.cs:421-424`（说 `+0x66` 无写入点，实际有）· `RuleCore.cs:14643-14646`（说 case 40 待接，实际 `ConditionEval.cs:231-233` 已接）· `RuleCore.cs` 约 :9643 那段说「我们统一不自排除」，与现行 `WhenEvents.SelfExcludesOnKeywordTrigger` 相反。

## 六、验收读数

- `python -I -X utf8 工具/merge_ledger_parts.py --dry --only B88_B6` ⇒ **更新 42 · 新增 0 · 无变化 0**
- `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ **0 条涉及 B88_B6**（83 条跨补丁重复键都是别的批次之间的，与本波无关）
- 本波另用 `check_ledger_points.py` 的**真函数**对 42 条新落点做过机检模拟：**fail 0 · MEMBER 告警 10 · 其余 32 条为方法声明**
- ⚠️ **行号时效**：`RuleEngine/Core/EffectResolver.cs` 在本波运行期间被其它写手改动（整体 +15 行/次）⇒ 42 条落点行号 = **写补丁那一刻的现读值**；合并前若该文件又变了，请先跑 `check_ledger_points`（漂移可 `--fix`，`--fix` 只能在没有 `.cs` 写手时跑）。

