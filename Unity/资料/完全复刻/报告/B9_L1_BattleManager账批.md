# B9_L1 —— 「X-账-BattleManager」+「X-账-其它」128 条账批（2026-10-26）

## 一、结论

| 项 | 条数 |
|---|---|
| 升 **A** | **12** |
| 留 **B** | **116** |
| 判 **C** | **0** |
| 小计 | 128 |

补丁：`账/_parts/B9_L1.tsv`（11 列 · **13 行** = 12 条 A + 1 条 B 补记差异）。

落点闸（只读）：`python -I -X utf8 工具/check_ledger_points.py 账/_parts/B9_L1.tsv` ⇒ **A=12 通过=12 漂移=0 失败=0**（1 条 GETTER 告警 = `Busy` 本来就是属性，已在备注写明）。

## 二、升 A 的 11 条

| 账行 | 原版方法 | 判据出处（截断） | 落点 |
|---|---|---|---|
| L126 | `ActionsInQueue` | decomp_full/BattleManager__ActionsInQueue.c:1-42（🥇 全文） | `RuleEngine/Core/ActionQueue.cs:230`（Busy） |
| L402 | `GetPendingBerzerkUnit` | BattleManager.cs:21188 | `RuleEngine/Core/RuleCore.cs:1432`（PendingBerzerkUnits） |
| L409 | `GetRandomDeckIndex` | decomp_full/BattleManager__GetRandomDeckIndex.c（39 行） | `RuleEngine/Core/EffectResolver.cs:12884`（InsertIntoDeckRandom） |
| L428 | `GetValueInPlay` | BattleManager.cs:41127 | `RuleEngine/Data/SimpleAI.cs:786`（ValueInPlay） |
| L475 | `IsValidActiveAbilityTarget` | BattleManager.cs:45987 | `RuleEngine/Core/RuleCore.cs:11096`（CanUseAbilityTarget） |
| L479 | `JamUnit` | BattleManager.cs:39686 | `RuleEngine/Core/EffectResolver.cs:15647`（DoJam） |
| L489 | `NextActionInQueue` | decomp_full/BattleManager__NextActionInQueue.c:1-40（🥇 全文） | `RuleEngine/Core/ActionPump.cs:1436`（PumpActionQueues） |
| L558 | `ResolveDisappearFromHand` | decomp_full/BattleManager._ResolveDisappearFromHand_d__545__MoveNext.c:134 | `RuleEngine/Core/RuleCore.cs:2865`（SweepEphemeral） |
| L651 | `ShouldTriggerAdjacentDamage` | decomp_full/BattleManager__ShouldTriggerAdjacentDamage.c:7-11 | `RuleEngine/Core/RuleCore.cs:6207`（DeclareAttackCore · 内联闸 :6877） |
| L652 | `ShouldTriggerStompDamage` | decomp_full/BattleManager__ShouldTriggerStompDamage.c:20-36 | `RuleEngine/Core/RuleCore.cs:6207`（DeclareAttackCore · 内联闸 :6914） |
| L1305 | `CheckIfMulliganCardsCorrect` | decomp_full/PlayerHand__CheckIfMulliganCardsCorrect.c:20-80（🥇） | `RuleEngine/Core/RuleCore.cs:13867`（MulliganCollectIndices） |
| L1419 | `OnCardDrawn` | decomp_full/RawCardScript__OnCardDrawn.c:1-9（🥇，全文 9 行） | `RuleEngine/Core/RuleCore.cs:12232`（FireTriggerAt） |

> `判据要点` 在账里每行 ≤60 字；被压缩掉的长版见 §附-A。落点括号一律写**裸声明名**（行号会有写手漂，`--fix` 靠名字重定位）。

## 三、留 B 的 116 条（逐条理由）

> 分类：① **判据不足**（原版体没读全 / 产物丢值）② **落点不足**（无方法级实现 / 多行共用 / 属性常量 / 歧义）③ **已知差**（顺序·时机·数值·缺支·口径不同）④ **未核对**（本轮没做逐条比）。

| 账行 | 类.方法 | 类别 | 理由 |
|---|---|---|---|
| L25 | `ScoreFromHandBuff` | ③已知差 | 已实现；`.c` 把每一处的值都丢了 ⇒ 后半段（`untilEndOfTurn=false` 时那两个 counts 项与 limitedUses 夹取）**只有 R11 的 RVA 转述**，本轮没复读 ⇒ 保持 B |
| L76 | `GetTargets` | ④未核对 | 原版一个 1427 行方法建池 + 特例短路；我们拆成「TargetCriteriaEval 逐候选判」+「调用方建池」⇒ 结构不同；其余档仍未逐条核。2026-10-26（B8_AE）补了 `ta=113` 那一半，两… |
| L79 | `NewBuffStat` | ①判据不足 | E3 已标「可能不是纯倍率」—— 本轮只读到分支头，**仍不能声称等价**。 |
| L80 | `PlayAbility` | ①判据不足 | 🔴 **整条分流与逐 op 语义没读全**（2,878 行）⇒ 按口径**不能声称等价**；已知三条调用点（playersAffected / ShuffleCardList / 每效果出口 SetPreviousAbi… |
| L83 | `DeepCopy` | ②落点不足 | 原版是**可变数据 + 拷贝隔离**；我们是**共享不可变 + 只读消费**。等价的前提 = 没有任何消费者写那份数据 —— 现读成立的只有 `targetsAvailableCounter`（两处只读）；`target… |
| L84 | `ShallowCopy` | ②落点不足 | 我们全仓没有「同一份能力数据的浅副本」这个概念 —— 需要变体时直接读不同 `CardDef`。是否有调用者写过这种临时副本，未查（见 DeepCopy 那一行）。 |
| L104 | `GetActingCard` | ③已知差 | 拆三函数、由调用方自选 acting/target；原版手牌两档无论 acting 都用 actingUnit（疑原版瑕疵）。⚠️ 原账写的 `GetActingCard` / `ResolveUnit` **这两个名字… |
| L111 | `GetTipPosition` | ④未核对 | 锚点逐档对过；近似三处：卡面子锚按比例、50/70/100 未做（0 条）、偏移量同上一行 |
| L127 | `ActionsLeftInQueue` | ②落点不足 | 实现是**多行属性** `Count`（声明头 + `{ get { … } }`）⇒ 落点闸不接受多行属性头、且按口径纯属性 getter 不能当方法落点 |
| L135 | `AddCardToDeck` | ②落点不足 | 我们**没有**「插到牌库第 N 位」的通用动作（专用口：Deck.Add / InsertIntoDeckRandom / DoReturn）；原版两份产物 0 调用点 ⇒ 可达性未定 |
| L167 | `AddRemoveFromCemetery` | ②落点不足 | 原版这一跳只清一个重入闸 `+0x24b=0`（无数据效果）；我们同步结算没有「取出途中」中间态 ⇒ 无落点行，不是漏做 |
| L204 | `AllowResolveAttack` | ②落点不足 | 原版末段 `isPlayer ∧ !IsAttacking ∧ matchType!=0xa0` 三道闸我们没有对应物（566 行，只读了 92-134 / 226-460）⇒ 缺一支 |
| L223 | `BroadcastMarkerlightResolved` | ③已知差 | `.c` 丢实参 ⇒ 排除集合靠同族三份互推，判据不是逐字段实证 |
| L251 | `CanPlayCard` | ④未核对 | 原版返回 `CustomError` 枚举、我们返回 `int` 错误码；**逐码对表未做** |
| L254 | `CanUseActiveAbility` | ③已知差 | 原版 `BattleManager.cs:46680` 未逐条读；我们的 `CanUseAbility` 与它只做到入口同形 |
| L260 | `CancelBattleActionFromQueue` | ③已知差 | 原版按 kind 取消**指定几条**，我们 `ActionQueue.Clear()` 清四道全部 ⇒ 语义不同 |
| L270 | `CheckIfAbilityCancelsAttack` | ③已知差 | 状态闸位置不同（我们把 CancelAttack 的两档提前到本函数首）+ 无待结算伤害队列 ⇒ 结构差 |
| L272 | `CheckIfCodexTriggered` | ④未核对 | 原版先过 `manaLeft==0 ∧ CurrentCost>0` 两格再入队 `triggerPlayerCodex`；我们的 `CheckCodex` 只有 `Energy==0` 一格，且 `.cs` 产物带 NoteDecompilerIssue ⇒ 两格语义未定 |
| L274 | `CheckUnitsCancellingAttack` | ③已知差 | 原版对每个命中单位各排一条 redirectedAttack；我们取槽号最小 ⇒ 已知顺序差 |
| L275 | `ChoiceOfCardEnemy` | ②落点不足 | 原版 `ChoiceOfCardEnemy` 是 **UI 选卡面板**（`chooseCardManager.Setup`），引擎侧对应物是 `DoChooseCard` 的候选域 ⇒ 不是同一件事 |
| L276 | `ChoiceOfCardPlayer` | ③已知差 | 同 275：原版 `ChoiceOfCardPlayer` 只调 `chooseCardManager.Setup`（纯 UI） |
| L277 | `ChooseCardMethod` | ③已知差 | 同 275：原版 `ChooseCardMethod` 是回调式面板驱动，我们同步取返回值 ⇒ 跨层、非方法级对位 |
| L325 | `DisplayHurryUpChatMessage` | ④未核对 | 原版 `isPopUpShown` 闸**没查清是什么** ⇒ 闸不能断言一致（不许拿猜的顶判据） |
| L331 | `EndTurnClick` | ③已知差 | 原版这一层是**点击拦截** + 4 个 UI 状态位（UIstate/resolvingTurnEnd/allowPlayerActions/WaitingForBlockingAction）+ 教程闸 ⇒ 表现层，未逐条对 |
| L335 | `ExecuteNextAction` | ②落点不足 | 原版「按日志下标执行」那条重放支与我们的 `ReplayStore` 动作流重放是两条链 ⇒ 落点不同 |
| L337 | `ExecuteUseWaystone` | ④未核对 | 未逐条核（原版 `ExecuteUseWaystone` 那一族未读） |
| L357 | `GetAvailableActions` | ④未核对 | 逐段对过 370 行，但「两个本回合手牌闸字节（+0x244/+0x246）的写点」本轮没核 ⇒ 有未知支 |
| L358 | `GetAvailableAttackActions` | ③已知差 | 原版同一目标两种打法各出一条 AvailableAction 并逐条过三道闸；我们由视图/AI 现算 ⇒ 结构差 |
| L359 | `GetAvailableAttackTargets` | ③已知差 | 枚举顺序差（原版倒序、我们正序）会改变 UI 列表顺序 / AI 平手裁决 ⇒ 已有已知差 |
| L380 | `GetDeathType` | ②落点不足 | 落点 `DeathTypeOfSource` 有**两处声明**（RuleCore.cs:9227 / :13323）⇒ 歧义；原版 6 档未逐条对 |
| L394 | `GetLowestHealthUnit` | ②落点不足 | 原版两点（遍历序 / 卡种闸 {20,40}）已对，但实现是 `ResolveTargets` 里的一段内联分支（该方法已被多行共用）⇒ 落点会变成类级命中 |
| L399 | `GetMulliganCard` | ②落点不足 | 原版 `GetMulliganCard` 是「从洗过的牌库取第 0/1 张并激活」的**取牌器**，我们 `Mulligan` 是整段换牌动作 ⇒ 不是同一件事 |
| L408 | `GetRandomAttackTarget` | ②落点不足 | 我们**没有**「随机攻击目标」这个查询（grep 0 命中）⇒ 只在调用点现算，无方法级落点 |
| L415 | `GetStartingMana` | ②落点不足 | 原版是取值函数；我们是两个字段（`startingMana` / `startingManaSecond`）+ `BeginTurn` 现算，无同名方法 |
| L423 | `GetUnitsInPlayList` | ③已知差 | 原版 `GetUnitsInPlayList` 带 10 个 `incl*` 标志（inclHero/inclMinions/inclRemnants/inclPlayer/inclEnemy/includeDying…），我们的 `Units()` 是无标志枚举 ⇒ 过滤语义未建模 |
| L424 | `GetUnitsInPlayNewList` | ③已知差 | 同 423（`GetUnitsInPlayNewList` 只是把 pooled list 拷成新表） |
| L425 | `GetUnitsInPlayPooledList` | ②落点不足 | 原版 `GetUnitsInPlayPooledList` 才是真实现（含全部 incl* 标志 + 池化）；我们无对应物 |
| L430 | `GetViewportPointFromBoardPos` | ③已知差 | 原版用 `boardCamera` 求视口坐标；我们只有 `boardCam.WorldToScreenPoint`（CardHit）和 `ui.WorldToViewportPoint` ⇒ 不同相机 |
| L451 | `IsCardIncluded` | ③已知差 | 原版一个函数带九档 `incl*`；我们拆三个无过滤谓词（IsInDeck/IsInHand/IsInCemetery）⇒ 过滤语义未建模 |
| L452 | `IsClosedDeckGame` | ②落点不足 | 我们只有枚举值 `ClosedDeck = 110`，**没有**判定方法（grep `IsClosedDeck` 0 命中）⇒ 无方法级落点 |
| L469 | `IsThereValidAbilityTarget` | ②落点不足 | 原版是「场上任一张可点 ⇒ 真」的 **∃ 查询**；落点 `CanUseAbilityTarget` 是单目标校验器 ⇒ 循环在调用点 |
| L470 | `IsThereValidSpellTarget` | ③已知差 | 同 469（法术版 ∃ 查询） |
| L478 | `IsValidSpellTarget` | ②落点不足 | `IsValidSpellTarget` 是**法术**闸，落点 `CanUseAbilityTarget` 是**主动能力**闸 ⇒ 不是同一件事 |
| L481 | `LeaveBattle` | ②落点不足 | 原版还调 `ReleaseCardbacks()`（全仓无对应物）+ `FirstTwoTutorialCompleted()` 为假时重开教程局（我们没有这一支）⇒ 缺两支 |
| L501 | `PlayAi` | ③已知差 | 原版 `PlayAi()` 只是协程 kickoff，真逻辑在 `_PlayAi_d__399__MoveNext`；我们 `DriveAiTurn` 已接脚本接管那支，但状态机未逐条对 |
| L514 | `ReadyToUseActiveAbility` | ④未核对 | 未逐条核（原版 `ReadyToUseActiveAbility` 的准入链未读） |
| L521 | `RecordAttackPendingDamage` | ④未核对 | 原版把待结算伤害**逐条登记在卡实例**上；我们返回 `List<int>`、不落卡 ⇒ 只有 `EnoughPendingDamageToDie` 这一条消费者等价，其余读者未核 |
| L541 | `ResolveAddEffect` | ③已知差 | 原版实质是 `CardScript.AddEffect(card, effect, -1)`（往卡上挂一条效果对象）；我们 `ApplyOneGain` 是数值增减 ⇒ 机制不同 |
| L555 | `ResolveCreateDeckCard` | ②落点不足 | 原版是状态机 `_ResolveCreateDeckCard_d__513`；落点 `DoCreate` 是共用造牌口，造牌→进牌库那一跳在 `InsertIntoDeckRandom` ⇒ 需拆/未逐条对 |
| L556 | `ResolveCreateHandCard` | ③已知差 | 同 555（进手那一路，手牌上限在 RuleCore.cs:13947） |
| L569 | `ResolveFollowAfterResolution` | ①判据不足 | 原版 `.c` 434 行，本轮只读到 prologue（结构拷贝），**没读到正文** ⇒ 不能声称等价 |
| L570 | `ResolveForceAttack` | ③已知差 | 原版用 `AI.SelectBestAttackAction` 选打法/攻击型并 `ChangeAttackType`；我们的 `DoForceAttack` 注释写明攻击型取「近战/远程高的那个（用户 2026-09-14 口径）」⇒ **已知口径差**，需调度台裁 |
| L571 | `ResolveForceAttackFriendly` | ②落点不足 | 同 570（友方版共用 `DoForceAttack`） |
| L574 | `ResolveManaBuff` | ④未核对 | 原版按 `ManaType` 整数分档，我们按 `kind` 字符串分派 ⇒ 逐档对表未做 |
| L579 | `ResolvePlayActiveAbility` | ④未核对 | 原版分 FinishActiveAbility / UsedActiveAbility 两拍，我们合并进 `UseAbilityCore` ⇒ 时机差未核 |
| L580 | `ResolvePlayCardFromHand` | ③已知差 | 逐段对过 8 跳，但原版把 SetAmbush/ShouldTriggerTeleport/… 放在另一批钩子里 ⇒ 次序链未对齐 |
| L599 | `ResolveTransformFromRemnant` | ④未核对 | 原版走 `TransformFromRemnant` 的重放清单，我们按 `DoReanimate` 两条支重放 ⇒ 清单对齐度未核完 |
| L601 | `ResolveTransformUnit` | ④未核对 | 2,252 行只逐段对了 5 跳 ⇒ 未逐句核完 |
| L604 | `ResolveTriggerCodex` | ②落点不足 | 落点与 272 同为 `CheckCodex`：原版 `ResolveTriggerCodex` 是**动作**、`CheckIfCodexTriggered` 是**入口闸**，共用一个落点会变类级命中 |
| L606 | `ResolveTriggerMob` | ③已知差 | 监听侧名字映射缺（见 BattleManagerSupport 729） |
| L609 | `ResolveTriggerRegiment` | ④未核对 | 未逐条核（`ResolveTriggerRegiment` 那三段未读） |
| L616 | `ResolveTriggerTeleport` | ④未核对 | 未逐条核（`ResolveTriggerTeleport` 未读） |
| L632 | `SetUnitOrderInPlay` | ②落点不足 | 原版把序号发在**卡自己的字段**上并顺手向 `BattleCardManager` 登记；我们只发对局级单调号（`TakePlayOrder`）⇒ 登记那一步不在落点内，且 `PlayOrder` 无读数口 |
| L637 | `SetupInitialMana` | ②落点不足 | 原版开局算一次，我们 `BeginTurn` 按 `TurnCount` 现算 ⇒ 时机差 + 无同名方法 |
| L649 | `ShouldActivatInitialEnchantments` | ②落点不足 | 原版 `ShouldActivatInitialEnchantments` 是谓词；落点 `SetupInitialEnchantments` 是动作 ⇒ 不是同一件事 |
| L664 | `ShuffleDeck` | ③已知差 | 原版 `ShuffleDeck(list, checkStratagem)` 带 **stratum 分档**；我们 `Shuffle<T>` 是纯 Fisher–Yates ⇒ 分档语义没建 |
| L670 | `StartBattlePhase` | ③已知差 | 原版 `StartBattlePhase` 是协程 kickoff（真逻辑在状态机）；我们 `BeginTurnCore` 是回合开始整段 ⇒ 范围不对等、未逐条对 |
| L690 | `TryUsingActiveAbility` | ④未核对 | 未逐条核（`UseAbilityCore` 的攻击型/目标池那两段未读） |
| L691 | `TryUsingActiveAbilityOfCard` | ③已知差 | 同 690 |
| L721 | `BroadcastTurnStart` | ③已知差 | 原版同一回调连发两次 OnTrigger（30 再 31），我们按卡面二选一 ⇒ 池内今天无可观测差，但**形状不同**、真出现双触发要改 |
| L1126 | `GetTraitEffect` | ④未核对 | 机制差：原版「一个 trait 一份机制」（全局静态表、两键查询），我们「一张卡一份正文」⇒ 键不同，换算未证 |
| L1128 | `GetTraitEffects` | ③已知差 | 同 1126 |
| L1130 | `GetTraitFromAbilityTrigger` | ②落点不足 | 原版是「枚举名 → 另一个枚举」的反射式解析；我们落点 `AbilityTriggerNumberOfName` 是**只读字典常量**（const 不能当方法落点） |
| L1166 | `EnemyReturnsAttack` | ③已知差 | 方法本体 1:1（预测五道 / 实际四道），但三个调用点里 `CardHighlight` 预览那条**未接** ⇒ 缺消费支 |
| L1167 | `GetActiveAbilityCard` | ②落点不足 | 原版 `+0x118` 第二来源在我们结构上不存在（池内 `buffType==40` 0 条）；落点 `Card.Ability` 是能力本体不是「来源卡」⇒ 无对位实现 |
| L1183 | `HasCurrentTraitActiveEffect` | ②落点不足 | 与 1180 共用 `UnitState.Has` 同一落点；且原版扫「addTrait/addTraitRemnant 且非残骸」，我们 `_removedTraits` 只是近似 |
| L1184 | `HasEnchantment` | ②落点不足 | 原版 `HasAbilityType(trigger)` 扫 cardAbilities；我们按关键词扫 TriggerOps/Effect ⇒ enum→关键词映射未证，且原版四个调用点里三个 gate（WhileInPlay/Mob/Rage）我们**没有对应物** |
| L1195 | `RevealedSecret` | ④未核对 | 未逐条核（`RevealSecretOrders` 与 EntityScript `RevealedSecret` 的关系未读） |
| L1235 | `ActivateMinions` | ③已知差 | 原版回合开始逐个 Activate；我们用的时候现算 Exhausted/CannotAttack ⇒ 净效果同、**时机不同** |
| L1240 | `AreMinionsWithAmbush` | ②落点不足 | 原版是查询「谁带伏击」；我们没有这个查询（事件驱动翻面）⇒ 无对应实现；原版查询的消费者也未核 |
| L1244 | `Awake` | ②落点不足 | 槽数来源：原版读运行时 SO `GameplayVariables.slotsPerSide`（离线包无该 SO），我们固定 4；`cardShadow.SetActive(false)` 无对应物 ⇒ 判据侧不完整 |
| L1246 | `FillMinionPositions` | ②落点不足 | 与 1244 共用 `ArenaSlots.Position`；本方法在原版 0 调用者（两种解释都站得住）⇒ 调用关系未定 |
| L1250 | `GetClosestAvailableSlot` | ③已知差 | 原版在**屏幕坐标**里算最近格号；我们只保留「格号 → 插入位置」那一步 ⇒ 分工不同、屏幕那半在表现层 |
| L1265 | `GetNextSlotPos` | ③已知差 | 原版是「位置 + 需要几格 → 下一个可用格」；我们是「位置 → 最近格号」，占用/多格在引擎另一处 ⇒ 语义不同 |
| L1266 | `GetNextSlotToCard` | ④未核对 | 疑似缺一支：原版「贴着这张卡（IndexOf+2）」那两半，我们的 summonPos 是否实现**没核**（PlayAbility 2,878 行未读全） |
| L1268 | `GetOwnPositionInBoard` | ②落点不足 | 原版一个方法同时给格号 + 坐标；引擎侧只有格号（坐标在表现层）⇒ 落点只覆盖一半 |
| L1286 | `ReassembleMinionsWhilePlayingUnit` | ③已知差 | 原版结算时重排、我们拖拽预览时就重排 ⇒ 时机差（判为纯手感、无离线观测量） |
| L1292 | `SlotPosToBoardPos` | ③已知差 | 原版返回「棋盘位置 = 槽位位置 + up×ExtraYOnBoard」；我们把它**并入** `ArenaSlots.Position` 的 y 项（常数 0）⇒ 不是一段独立实现，且原版 0 调用者 |
| L1297 | `AddCardNotDrawnToHand` | ②落点不足 | 我们只有一个进手 helper（`AddCardToHandWithinLimit`），原版三条入口（本行 / AtIndex / CheckEffectsOnNewCard）共用它 ⇒ 三条都落同一行会变类级命中；且去向不同（原版 SendToCemetery / 我们 Discard） |
| L1298 | `AddCardNotDrawnToHandAtIndex` | ②落点不足 | 原版 `AddCardNotDrawnToHandAtIndex` 独立入口；我们新补的 `AddCardToHandAtWithinLimit` 与原版同形，但**三条入口共用**判据 ⇒ 未拆清 |
| L1304 | `CheckEffectsOnNewCard` | ②落点不足 | 原版 `CheckEffectsOnNewCard` 是独立入口；我们并进进手 helper ⇒ 形状更少一处、无同名实现 |
| L1306 | `CompleteMulliganPhase` | ③已知差 | 分工不同（原版 CompleteMulliganPhase 只管收尾清表；我们把弃回/洗/补抽放 Mulligan、关阶段发防御卡放 EndMulligan）；且原版对双方牌库各洗一次、我们只洗换过的一方 |
| L1328 | `MoveCardToMulliganPos` | ②落点不足 | 未逐条核：序列化判据（换牌面板 MB 4881 / 选牌面板 MB 4052 逐字段相同）已读，但落点 `MulliganPanel.RowWorldPosAt` 只覆盖行位，原版还有整卡摆位 + 缩放 |
| L1335 | `RemoveEffectsFromCard` | ④未核对 | 原版摘的是 CardScript 上的效果对象；我们的效果挂在 `CardInstance.HandEffects` 上 ⇒ 反查退化但等价；差异列自述那一点，未逐条核 |
| L1339 | `SetMulliganCardsToDiscard` | ②落点不足 | 原版分「记录 / 开始换」两步（有中间态），我们一次提交下标数组 ⇒ 无中间态；且落点 `Mulligan` 与 1305/1306 共用 |
| L1342 | `SetupCardInHand` | ②落点不足 | 原版 `SetupCardInHand` 由 PlayerHand 驱动（引擎半 + 界面半）；我们并进进手 helper、界面在 HandLayout ⇒ 无对位实现 |
| L1360 | `CanPlayAbilities` | ②落点不足 | 原版 `CanPlayAbilities` 只看 `GetTargets` 有没有目标；我们还要过能量/次数/ward ⇒ **我们更严**（已知差） |
| L1369 | `GetAbilitiesForTriggerRawcard` | ④未核对 | 原版返回同 trigger 的**能力列表**（可多条），我们按 trigger 归一成一组、例外（605 信仰族）在 CardDef.cs:680-760 单独配对 ⇒ 未逐条核 |
| L1370 | `GetActiveAbility` | ③已知差 | 逐句对过 17 行；已知差异 = 原版 `MeetsCriteria` 真假两态、我们多一态 ⇒ 三态语义未对上 |
| L1399 | `GetTargetsMeetingCriteria` | ②落点不足 | 职责同、形状不同（原版一个入口 + AbilityLogic.GetTargets；我们拆 ResolveTargets + CountEval）⇒ 落点会被多行共用 |
| L1400 | `GetTransformedKnightId` | ①判据不足 | 原版 50 行只读前段 ⇒ 不能声称等价（它是「pilot → 目标卡 id」表查询，我们由 op 字符串现查卡池） |
| L1401 | `GetValidTargets` | ③已知差 | 原版把「枚举 + 校验」合成一个方法；我们是单格校验 + 调用方循环 ⇒ 枚举那半在调用点 |
| L1417 | `IsValidActiveAbilityTarget` | ③已知差 | 原版对「没主动能力」是 LogError + 拼诊断串；我们返回错误码由调用方出声 ⇒ 形态不同（都出声） |
| L1420 | `OnCardPlayedWithTarget` | ①判据不足 | 原版 120 行只读前段 ⇒ 不能声称等价 |
| L1421 | `OnCardPlayedWithTargetJustThisCardPlayed` | ③已知差 | 原版把「自指」拆成独立入口 `JustThisCardPlayed`；我们用 trigger 号区分 ⇒ 形状不同 |
| L1426 | `OnQuestResolved` | ②落点不足 | 原版是 9 行薄包装（`OnTrigger(0x1ea=490)`）；我们的 490 在触发表里，落点指到的是一段内联注释 ⇒ 无方法级落点 |
| L1433 | `OnTriggerAlt` | ②落点不足 | 原版 `OnTriggerAlt` 是另一条触发路，我们收成「先选支再走同一分发」⇒ 结构更少一处、无同名实现 |
| L1434 | `OnTriggerEffectInCard` | ②落点不足 | `260` 这里也是 `chosenAbility`（= triggerEffectInCard）；触发分发落点未到该档 |
| L1437 | `ResolveAbilities` | ②落点不足 | 原版按 **AbilityLogic 对象**逐个 Play，我们按 **EffectOp** 逐个 Resolve ⇒ 粒度不同；`ResolveOps` 已被多行共用 |
| L1438 | `ResolveActiveAbility` | ③已知差 | 选支那句逐句等价；但原版把「播动画」与「执行」串在一个协程里，我们引擎先执行、表现层按事件播 ⇒ 「动画先于结算」是否一致**无法从引擎侧观测** |
| L1442 | `TriggerAbilities` | ①判据不足 | 原版 272 行只读签名 ⇒ 不能声称等价 |
| L1443 | `TriggerAbility` | ①判据不足 | 原版 116 行只读签名 ⇒ 不能声称等价 |
| L1465 | `GetCardListFromIds` | ④未核对 | 原版结果表**保留 null 占位**（下标 = 输入下标），我们返回单值 + 跳过 ⇒ 列表长度可能不同；4 个调用点是否消费 null 洞未核 |
| L1521 | `OnInitialize` | ③已知差 | 原版是任务实例「进度 +1」；我们是一局的原始计数（任务进度另算）⇒ 结构不同 |
| L1522 | `OnReset` | ②落点不足 | 原版靠信号注册生命周期，我们「一局一个 tally 对象」⇒ 没有注销这一步 |
| L1523 | `OnTriggerAbility` | ④未核对 | 原版在信号回调里判「算不算这条任务」；我们把计数与闸拆开 ⇒ 只有任务侧按 trigger 过滤后才可达面相同（未证） |

## 四、疑点 / 新发现（要记账的）

1. 🔴 **`LeaveBattle`（L481）缺两支**：原版 `ReleaseCardbacks()` 全仓无对应物（`grep -rn Cardback` 只有尺寸注释）；`FirstTwoTutorialCompleted()` 为假时的「重开教程局」也没有。原 差异 列为空 ⇒ 已在补丁里补记（状态仍 B）。
2. 🔴 **`DoForceAttack` 的攻击型选择是「用户口径」**（取近战/远程里高的那个，2026-09-14），原版是 `AI.SelectBestAttackAction` 择优 + `ChangeAttackType`。⇒ L570/L571 **不能**按等价记 A；要不要改由调度台裁（铁律 11：差异要记，做不做只是先后）。
3. 🔴 **`CheckIfCodexTriggered`（L272）两格闸对不上**：原版 `manaLeft==0 ∧ CurrentCost>0` 才入队；我们 `CheckCodex` 只有 `Energy==0`。`.cs` 产物带 `NoteDecompilerIssue` ⇒ 语义未定，**不许猜**。
4. ⚠️ **`ActionQueue.cs` 在我工作期间被写过**（`Busy` 从 :229 漂到 :230）⇒ 本批行号都是「2026-10-26 现读」；补丁落点括号都写了裸声明名，可 `--fix` 重定位。
5. ⚠️ **路线的 `可批量块` 标签偏乐观**：本块 128 条里只有 12 条真的只差「判据要点 + 落点重钉」；其余 116 条在账里**早已写清**卡点（多为「原版体没读全 / 落点无对应物 / 已知差」）。⇒ 下一批若要「零代码升 A」，应从 **`X-落点补行号`（51 条）**里取，而不是本块。

## 五、没查清的（如实记）

- L475 记 A，但**原版 tutorial 许可闸**我们放在哪一层没查（判为教程/表现侧，未逐句核）。
- L479 记 A 的依据是「原版只入队」；`ResolveJamUnit` 的效果体与原版 `_ResolveJamUnit_…MoveNext` 的逐句比**没有做**。
- L651/L652 记 A 的依据是「内联闸式子同」；两处在原版的**调用点**（`_ResolveAttack_d__438__MoveNext`）没有回读。
- 其余 116 条的理由**主要取自账里已有的自述**（没有全部重新复核原版）；凡我亲自读过的，理由已按现读改写（见 §三 类别列）。

## 附-A 升 A 的判据要点（长版，账里是 ≤60 字压缩）

### L126 `ActionsInQueue()`
- 账里（压缩前）：四道队列（+0x350/0x358/0x360/0x368）计数之和 > 0 ⇒ true；否则返回 `resolvingActionFlag(+0x280)`（ObscuredBool → bool）。

### L402 `GetPendingBerzerkUnit(bool isPlayer)`
- 账里（压缩前）：（原版 `BattleManager.GetPendingBerzerkUnit`，判据见 `BattleManager.cs` 方法体）

### L409 `GetRandomDeckIndex(bool isPlayerY)`
- 账里（压缩前）：随机挑一个牌库下标（`GetRandomInt(0, Count)`）；牌库空时**不掷**

### L428 `GetValueInPlay(CardScript card)`
- 账里（压缩前）：（原版 `BattleManager.GetValueInPlay`，判据见 `BattleManager.cs` 方法体）

### L475 `IsValidActiveAbilityTarget(CardScript actingCard, CardScript target)`
- 账里（压缩前）：（原版 `BattleManager.IsValidActiveAbilityTarget`，判据见 `BattleManager.cs` 方法体）

### L479 `JamUnit(CardScript jammedCard, int priority, bool blockPlayerActions)`
- 账里（压缩前）：（原版 `BattleManager.JamUnit`，判据见 `BattleManager.cs` 方法体）

### L489 `NextActionInQueue()`
- 账里（压缩前）：道序 **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**；取到就 `ResolveActionFromQueue(queue)`；全空返回。

### L558 `ResolveDisappearFromHand(BattleAction battleAction)`
- 账里（压缩前）：原版回合末只扫手牌六档、跳过墓地；我们 SweepEphemeral 逐条对上

### L651 `ShouldTriggerAdjacentDamage(CardScript actingCard)`
- 账里（压缩前）：原版判 CurrentBlast>0；我们判关键词存在，值 0 档不可达

### L652 `ShouldTriggerStompDamage(CardScript actingCard, CardScript targetCard, int adjacentUnits)`
- 账里（压缩前）：原版=stomp∧目标负血∧有相邻；负血即溢出，与我们 dealt>hpBefore 同值

### L1305 `CheckIfMulliganCardsCorrect(int[] mulliganCardsToDiscardId)`
- 账里（压缩前）：原版「全过才动手」：一条不合法 ⇒ LogError + 整批拒绝；我们原来逐条静默跳过（部分生效）

### L1419 `OnCardDrawn(BattleManager manager, CardScript thisCard, CardScript drawnCard)`
- 账里（压缩前）：直接转调 `OnTrigger(trigger=260 = OtherCardDrawn, manager, thisCard, drawnCard, …)`

