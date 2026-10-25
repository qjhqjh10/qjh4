# E · 判据总表（引擎线 + 战斗线缺口）

> 代理 E 为只读模式（Explore，无写文件工具）⇒ 报告由主对话代存，内容未改。
> 日期：2026-10-25 · 第七十五会话
> 所有引用的编号/数值均由代理现场 `grep`/`python` 复核过一次（含 `1351 张 modifier 全 0`、SW18 候选 2 张、23 个 ta 值、20/10/0 常量）。

---

## 引擎线

### 1 SW18 变身（transform）时的分配口径
- **判据结论**：分四层，原版方法体都在 `BattleManager__ResolveTransformUnit.c`：
  - **随机分配**：`chosenAbility==90` 走 `AbilityLogic__PlayAbility.c:950-993`，`:953-978` 是**按目标表逐张** `while`，循环体内 `:958` 每张各调一次 `AbilityLogic__GetCardToSummon`，`:974` 才 `AddTransformCard`；`AbilityLogic__GetCardToSummon.c:191-206` 在「候选非空 ∧ `summonAllTargets(+0x28)==0`」时于 `:193` `GetRandomInt(0,count)` 取一张 ⇒ **逐张目标各自独立掷一次**（SW18 数据：`cards_raw.json` 的 `cardAbilities[1].abilityLogic[0]` = `chosenAbility 90` · `targetCriteria.targetsAffected 50(inHandFiltered)` · `summonCriteria.unitsToSummon` 2 张 · `summonAllTargets` 缺键=0）。出处 `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:950-993`、`d:/2/tools/decomp_full/AbilityLogic__GetCardToSummon.c:191-206`、`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（SW18 字段）。
  - **属性**：换成新卡的 `RawCardScript` 后重算 —— `SetRawCardData`（指针换新，同一 CardScript 对象）→ `CardSetup`（`param_5==0` 分支把攻/血等复位到新卡基值 + `SetBaseCardTraits`）→ 两次 `UpdateFigures`。出处 `BattleManager__ResolveTransformUnit.c:791`/`:977`/`:1480`/`:1678`；`CardScript__SetRawCardData.c:23-24`；`CardScript__CardSetup.c:116-160`、`:194`。
  - **关键词/特性**：`CardScript__CardSetup.c:156` `SetBaseCardTraits` 从新卡重建；随后 `HasCurrentTrait(0x4b0=1200)` 补设伏击，`SetAmbush`。出处 `BattleManager__ResolveTransformUnit.c:1775`/`:1825`。
  - **格号/手牌位**：**不重排**。整个方法体**没有** `InsertMinion/RemoveMinion/NextFree/GetNextSlot` 一类调用；同一个 CardScript 对象状态 15（`SetAsUnitTransforming :367`）→ 2（`SetAsUnitInPlay :1076`；手牌目标走 `SetAsUnitInHand :1028`），原地放回。`:1207` 的 `MinionPlayedIntoField` 只做落地/层/位置补间（`CardScript._MinionPlayedIntoField_d__314__MoveNext.c:37-143`），不动棋盘列表 ⇒ 格号/手牌 index 不变。
  - **旧状态**：`keepEffects`（`SummonCriteria+0x2a`）为 0 时先 `RemoveOwnExtrinsicEffects`（`:702`）并 `ResetToValuesInHand`（`:605`）；SW18 该位=0 ⇒ 旧 buff/附魔不保留；两侧手牌 `UpdateCardEffects` 在 `:1829-1831`。
  - **我们的现状**：B3 已按逐张随机改好（`pickIdx` 移进 `for`，`Core/EffectResolver.cs:2991` 每张 `ctx.Rng.Next`；`:2999` 用 `ctx.NewInstance(pick)` 换**新实例**），判据记录见 `d:/4/Unity/资料/普查产出_1024/C1_引擎线判据总表.md:123-135` 与 `d:/4/Unity/资料/普查产出_1024/B3_引擎第二批.md:15`。
- **还差什么**：判据无缺口。要跑 B3 的 Unity 断言（`TestB3EngineFive` / SW18 块）与一次真机。**唯一未定的差异**：原版换的是同一 CardScript 的 rawCardData（in-battle id 不变），我们用 `NewInstance`（id 换新）；`keepEffects=0` 时现读无观测差。要做一次「实例 id 是否有消费者」的身份审计再去动（`d:/4/Unity/资料/卡实例身份_爆炸半径.md`）。

### 2 T03·b 「正修正额外牌」摆位档
- **判据结论**：原版**确有**四档分类，但**不是坐标**，是每张牌的 `mulliganOption`（`CardScript+0x260`），阈值与判据全在 `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:38-66` + `:104-117`（同一套逻辑另有 `PlayerHand__GetMulliganOption.c:5-16`）：
  - 记 `S=startingHand(+0x1C)`、`m=督军 initialHandModifier(+0x118)`、`e=secondExtraCards(+0x20)`（只给**先手之外**那一侧）；`base=S+min(m,0)`、`pos=max(m,0)`、`count=base+pos+e`（教程 `param_3!=0` 整体覆盖）。
  - 逐张按 index `i`：`i<pos` → **20**；`pos<=i<pos+e` → **10**；`i>=pos+e` → **0**；`i<0` → **30**（本路径 `+0x3c` 被写成 0，**30 档不可达**）。
  - **摆位不是由档值算的**：`MoveCardToMulliganPos(hand,card,i,count)`（`PlayerHand__MoveCardToMulliganPos.c:22-31`）→ `CardsHorizontalLayout.GetPosition(i,count,…)` ⇒ 位置只由 **index/count** 决定；档值只写进 `SetupCardInMulligan`（`CardScript__SetupCardInMulligan.c:5-18`）。全反编译对 `+0x260` 的读取只有两处：`CardScript__ClickMulliganSelected.c:27`（`==30` 不许换）与 `AI__GetAiMulliganCards.c:39`（`==30` 跳过）。
  - 今天数据：`cards_raw.json` 1351 张 `initialHandModifier` 全 0（现场扫 0/1351）⇒ 20 档永不出现；后手那侧 index0 是 10 档，其余 0 档。
- **还差什么**：判据齐。台账「只影响换牌摆位（20/10/0）」这句**与反编译不符**：档值不影响摆位，只作可换/不可换标记（且 30 档本路径不可达）。若仍要照原版留字段：在引擎按上面的 index 区间给每张起手牌打 `MulliganOption`，表现层照旧按 index/count 摆；今天无任何可观测差，不需要真机。

### 3 T03·c 多词条 / 多触发结算顺序
- **判据结论**：原版是一条固定的四层链：
  1. **监听者遍历序 = `allCardsInPlay` 进场序**（`BattleManagerSupport__BroadcastTurnStart.c:18-31`；TurnEnd 同形）。D10：遍历**活列表**而非快照，但 `Remove` 由队列驱动、不在广播栈上 ⇒ 现读不可区分，**不改**。
  2. **手牌层 = 当前方 → 另一方**（`BroadcastTurnStart.c:32-43`、`:44-61`）。
  3. **卡内 = `cardAbilities` 列表序，全触发、不去重**（`RawCardScript__TriggerAbilities.c:108,207`）；D15 = 同文去重 + 异文追加（`d:/4/Unity/资料/交接_第六十七会话.md:156`）。
  4. **动作层 = priority 四道独立 FIFO，固定消费序 `Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)`**，同队 FIFO、不抢占当前动作、每帧最多一条（`BattleManager__AddAutoActionToQueue.c:47/284/174-175/225-231`；`BattleManager__Update.c:1271-1299`；`BattleManager__NextActionInQueue.c:11-28`；`BattleManager__ResolveActionFromQueue.c:42-44`）。
  - 跨道 priority 压过 PlayOrder；多个 Artifice「先落场先结算」，`SAU13/SAU70` 是 priority=3 ⇒ 最后（判据总表 `d:/4/Unity/资料/普查产出_1021/W61_引擎线_T03c批B判据.md:10,22-34,85-87`）。
  - 死亡链：判死 → `backlash(13)@Instant` → `ResolveDeadCard` → `minionDeath(4)@Instant` → `BroadcastDeadUnit(90/Slay)` → finish（`CardScript__CheckIfDead.c:129-141` 等）。
  - 我们已落：`Core/ActionQueue.cs`（四道 FIFO）+ `UnitState.PlayOrder`；D11 结算序已改为 棋盘→当前方手牌→常驻（`B3_引擎第二批.md:15`）。
- **还差什么**：判据齐，无行为缺口。D10 判定「不改」；剩 `T03` 行的「电池 3 张真机探针」未跑（`d:/4/Unity/资料/任务线/引擎线/任务总表.md:107`）。

### 4 T03·d 独立审查缺陷里「判据不足」的 3 条
- **判据结论**（`d:/4/Unity/资料/普查产出_1023e/C2_引擎线判据_伤害与选靶.md`）：
  - **GetsShield ↔ UnitReceivesMarkOfChaos(510)**：映射**不成立**。510 是 Mark of Chaos（`markOfChaos=8` → `BroadcastMarkResolved`），`AbilityTrigger` 枚举里没有任何「获得护盾」触发（只有 510/515/520 三个标记）。`GetsShield` 是我们这侧的推断词，**无原版形状 ⇒ 保持现状**（`C2…伤害与选靶.md:19`，D-05）。已闭合。
  - **P2-8 berzerk 选靶**：原版 = 回合末 `GetPendingBerzerkUnit` 取候选，对每个单位调 `AI.SelectBestAttackAction`，null 则 `LogWarning` 跳过；候选口径 = `cardType != 20/40 ∧ isRemnant(+0x65)==0 ∧ IsInPlay()`（黑名单两档，不是「必须是 unit」）。出处 `BattleManager__ResolveEndTurn.c:44/51/85`、`BattleManager__GetPendingBerzerkUnit.c:49-57`；`C2…伤害与选靶.md:17-18`（D-03/D-04）。我们已按黑名单补齐，**池内 `berzerk` 0 张 ⇒ 不可达（例外①）**。
  - **P3-C 取消攻击是否发 Attack**：**表现层不发、动作流水照发**。三处取消支（`:288-305` 伏击、`:531-597` 取消能力、`:1310-1368`）都在 `:988 BroadcastUnitAttacked` 之前 `goto :1370`；但 `:286 CommunicateBattleAction` 在三处取消之前，联机对端会收到「攻击尝试」，`RegisterPlayerActionCancelled` 只改计数不发包。出处 `BattleManager._ResolveAttack_d__438__MoveNext.c:286/291-305/531-597/988/1310-1368/1370`；`C2…伤害与选靶.md:20`（D-06）。我们表现层现状正确；联机/回放侧必须让这次尝试进动作流水（现状 `OK` 后即发，已满足），⛔ 别复用 `EvtKind.Attack`。
  - 附：若「三条」指 B5 那三条「判据空」（`initialHandModifier/initialManaModifier`、`skipCardDraw`、`berzerk`），前两条已由 `T03·b` 闭合（无生产者/全 0），第三条即上面 P2-8。
- **还差什么**：三条判据均已闭合，无未定项。要跑 `RuleEngineTest`（P2-8 的 `N1BerzerkCandidateFilter`）与同步点绿；真机不需要。

### 5 新账-89 ② 负伤害
- **判据结论**：原版扣血**没有 `damage > 0` 闸**：`currentHealth = currentHealth - damageValue` 无条件执行；护甲只在「伤害≥1 ∧ 护甲≥1」时改写为 `max(1, 伤害−护甲)`。**负伤害=回血**，但**不可达**：近战/远程取攻击值都钳 ≥0（近战 `if (-1 < base+sum) return base+sum; return 0`；远程 `test ecx,ecx / cmovns`），攻击力≤0 时攻击请求本身被拒（`CantAttackWithZeroMelee/RangedAttack`）；`cards_raw.json` 2196 条 `damageCriteria` 无一条负值。出处 `CardScript._ReceiveDamage_d__381__MoveNext.c:93/115/173`、`EntityScript__get_CurrentMeleeAttack.c:60-64`、`IsValidAttackTarget.c:57-86`、`d:/4/Unity/资料/普查产出_1023e/C2_引擎线判据_伤害与选靶.md:15`（D-01）。
- **还差什么**：无。**不改结算**（给 `Hurt` 加 `>0` 闸反而与原版不同）；按例外①记账即可。

### 6 新账-97 规则语义里「判据不足」的 3 条
- **判据结论**：
  - **`UnitDeathByAbility(180)` 发射口（已找到）**：`RawCardScript__OnMinionDeath.c:21-43` 按死因分派 —— `param_6==10→175`、`==20(ability)→180`、`==40→170`，其余 return；上游是 `BroadcastDeadUnit.c:82-150` 对**每张在场卡**调 `ResolveDeadCard` → `TriggerOnMinionDeath.c:97`。`UnitDeathType.cs:3-9`：只有 10/20/40 发，15/30/50 一条不发。出处 `d:/4/Unity/资料/普查产出_1024/C1_引擎线判据总表.md:40-56`；B3 已补结构化发射口（`B3_引擎第二批.md:15`）。
  - **`TargetsAffected` 23 档（已逐档给出）**：池内实际值 = `{0,10,20,30,50,51,52,60,65,80,90,100,106,110,113,120,130,140,150,180,200,201,240}`；唯一 resolver = `AbilityLogic__GetTargets.c`（顺序 if 链），23 档逐档语义见 `d:/4/Unity/资料/普查产出_1024/C1_引擎线判据总表.md:57-93`。已实现 vs 缺档：`140/150` 只有窄特例；`200/201` 缺 `referencedCards` 列做不到；`240` 只有裸 deal 形状；`AiTargets` 的 `ta==0` 语义错（应=场上全部）。`250/280/320/330` 属 `conditionsFilter`，不是本表数字，别混读（C1 §4 开头）。
  - **`terror` 的 `get_currentCombatAttack`（已读方法体）**：三分支 —— `dropPod(230)` → 返回存档攻击值 `+0xe0`；`isRemnant(+0x65) ∧ !isLeavingRemnant(+0x66)` → 0；否则 `currentAttackType(+0x120)==2` 取远程、其余近战。调用点 = `ActivatesTerror` 反击豁免、`EnemyReturnsAttack`、`HasBastionHigherThanAttack`、AI 几处。出处 `EntityScript__get_currentCombatAttack.c:19-47`、`EntityScript__ActivatesTerror.c:10-18`、`C1…总表.md:95-107`；B3 已补 `IsLeavingRemnant`，`dropPod` 池内 0 张不可达。
- **还差什么**：判据齐。剩的是**实现缺口**（140/150/200/201/240 与 `AiTargets` ta=0）与 `dropPod` 分支（0 张）；200/201 要先拿 `referencedCards` 列（数据管线不在本白名单）。真机不需要。

### 7 新账-88 回放终局指纹分叉（轨迹相同、终局不同）
- **判据结论**：
  - 已知**一个真根因且已修**（与 F7 同一族）：`TickReplayPump` 先 `_replayCursor++` 再调 `ApplyReplayActionAt`，被 `AdvanceTimeline(0f)` 重入时 `_replayPumping` 把这一条 no-op 掉 ⇒ 游标偷走一格、静默少演一条。修法 = 把重入闸挪到游标推进之前；判别式 = `ReplayCursorForTest == actions.Count && 终局指纹 == 录制时`。出处 `d:/4/Unity/资料/普查产出_1024d/D2_BattleScene11红诊断.md:258-320`、`W24_战斗11红修复_执行报告.md:100-130`（`Battle/BattleDriver.cs:1288-1298`）。
  - F7 的「`trace` 摘要相同、终局 `StateHash` 不同」只剩两种可能：**差异落在 `StateHash` 有而 `DeepHash` 没覆盖的维度**，或**落在最后一条 trace 取样之后**（R2 §6 五候选：循环外多调一次 `CompleteReplayBeginTurn`；棋盘 `Attack/RangedAttack/Armor/Exhausted`；弃牌堆/牌库全量内容；Faith/SpiritStones/QuestPoints/Fatigue/ManaCarry/CostDelta/HandEffectRecords/CostMods/TempControls；督军四围）。判据 = `d:/4/Unity/资料/普查产出_1024d/R2_回放表现判据.md:179-230`。
  - 工具已就位：`Core/ReplayFingerprintTrace.cs`（`FirstDivergentIndex`：`-1` 无分叉 / `-2` 开局不同源 / `>=0` 第 N 条后分叉；`NoExpectedTrace` 表示老档**没判≠通过**）；`Battle/ReplayStore.cs:151` `startFullHash` + `:168` `traceFull`；比对口径 = `Net/NetProtocol.cs:466 StateHash`（⛔ 不用 `string.GetHashCode`）。出处 `普查产出_1024d/W2_引擎批_执行报告.md:17-33`、`W9_回放指纹接线_执行报告.md:30-34,101-122`。
  - **怎么造可复现用例**：固定 seed / 固定双方卡组，`mulliganEnabled=1`，录一局（`ReplayStore` version≥4），动作流至少含：一次换牌换张、一条 AI 动作、**最后一条不产生任何 VFX/时间线事件的动作**（专抓游标被偷）；另造两个变体：换牌期投降、带进攻卡选择。分别从「战斗内 ▶」与「菜单档案窗 ▶」两个入口各放一次。
  - **要断的不变量**：① `startFullHash` 两侧相等（`FirstDivergentIndex != -2`）；② `traceFull.Length == actions.Count`（`ActionCountMismatch == 0`）；③ `FirstDivergentIndex == -1`；④ `finalHash == 录制时`；⑤ `ReplayCursorForTest == actions.Count`（一条不落）；⑥ `NoExpectedTrace == false`（真对了账）；⑦ 既有 `DeepHash` 轨迹逐条相等；⑧ `StateBrief`/盘面速写逐字相同。诊断加一格：`RecFinish` 写 `finalBreakdown`（StateHash 每维各一数），回放末尾逐维比、打印**第一个不等维名**。
- **还差什么**：判据/工具齐；**还差一次真机**：F7 那局的 `Player.log` 原文已丢（被同晚两趟重开覆盖），要用带 `traceFull` 的新录像重放一次，按上面 ①–⑧ 定位第一个分叉维/条。要真机。

---

## 战斗线

### 8 换牌阶段「正修正额外牌」20/10/0 三档的摆位数值
- **判据结论**：与第 2 条同一份方法体（`PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:38-66,104-117`、`PlayerHand__GetMulliganOption.c:5-16`）。数值/区间：
  - `i < pos` → `20`；`pos <= i < pos+e` → `10`；`i >= pos+e` → `0`；`i < 0` → `30`（`+0x3c=0` ⇒ 本路径不可达）。
  - 其中 `pos = max(warlord.initialHandModifier,0)`、`e = ScenarioVariables.secondExtraCards`（默认 1，只给**非先手方**）、`count = startingHand + min(mod,0) + pos + e`。
  - **摆位** = `MoveCardToMulliganPos(hand,card,i,count)` → `CardsHorizontalLayout.GetPosition(i,count,…)`，只吃 index/count。**档值不是坐标**，写入 `CardScript+0x260`；消费点仅 `CardScript__ClickMulliganSelected.c:27`（`==30` 不许换）与 `AI__GetAiMulliganCards.c:39`（`==30` 跳过）。
  - 谁拿 `secondExtraCards`：`BattleManager._SetupMulliganPhase_d__341__MoveNext.c:79-86` 把 `BattleManager+0x247`（= `GetPlayerGoesFirstWithInitiative`，写入点 `BattleManager__SetupBoardPhase.c:108`）传给先手手牌、取反给另一侧；`AddCardsToMulligan` 里 `+0x28==0` 那一侧才加 `+0x20` ⇒ **后手方拿 10 档**（与我们按 `SecondSeat` 发一致；W62 §五残余①「给非玩家那侧」的说法应订正为「给非先手那侧」）。
- **还差什么**：判据齐；数据侧 1351 张修正值全 0（现场扫 0/1351）⇒ **20 档今天不可达**；30 档本路径不可达。表现层不必改摆位；若要留档位语义，只需在引擎给每张起手牌打 `MulliganOption` 字段。不需要真机。

### 9 DoReanimate 的退路口径
- **判据结论**：
  - **原版根本没有「墓地翻回来」这条路**：唯一入口 `BattleManager__AddTransformFromRemnant`（Action `0x48`，`:38`）→ `ResolveTransformFromRemnant.c:78-96`（`SetAsUnitTransforming` → `TransformFromRemnant`）→ 回调 `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31` 的清单 = `ResetToValuesInHand` → `BroadcastUnitReanimated` → `SetAsUnitInPlay` → `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` → `CheckStun` → `UpdateFigures` → `BroadcastWhileInPlay` → 两侧 `UpdateCardEffects`。**整条链没有 `ResolveUnitSummoned` / `BroadcastUnitSummoned`** ⇒ 不發 `When deployed`(195) / `When you deploy`(190) / `UseFaith`(605)。
  - **所以 `DeployFree` 的改法 = 在 `DeploySource.Reanimate` 下整组跳过部署广播**：`RuleCore.cs:4358` `bool reanimated = deploySource == DeploySource.Reanimate;` → `:4359` `if (!reanimated)` 包住 `PreCallBoardListener(selfOnlyOnly:true)` + `BroadcastWhen(Deploy)` + `ResolveDeploy`，`:4414` 用同一 flag 挡 `UseFaith(605)`；`EvtKind.Deploy`（`deployFrom: Reanimate`）仍发，`When Reanimated` 与 `WhileInPlay` 照发。**这条已于 2026-10-24 W12 落地**（`d:/4/Unity/资料/任务线/引擎线/任务总表.md:40`；断言 `Editor/RuleEngineTest_W12.cs` 的 `W12ReanimateSkipsDeployBroadcast`）。残骸那条退路本来就只发 `EvtKind.Deploy` + `Reanimated`（`Core/EffectResolver.cs:13531-13535`）；墓地退路走 `DeployFree(..., Reanimate)`（`:13568`）。
- **还差什么**：判据 + 实现都已落，无新增改动。还差：跑 `RuleEngineTest` 的 W12 断言；`战斗线/任务总表.md:221` 那行状态（仍写「要改」）要订正为已落。真机不需要。

### 10 AN1·R8-b 第 ④ 档仍播别 trait 的框
- **判据结论**：原版在 trigger 拍播**两件互不替代**的东西：
  1. **触发卡自己的卡级行**：`RawCardScript__TriggerAbility.c:85-110` 对这条 ability 的 `abilityLogic` 逐条 `AbilityLogic__PlayAbility`（`AbilityLogic__PlayAnim.c` 只播这条 ability 自己的动画引用，**不碰 trait**）。
  2. **一张 trait 框，只由 trait 链播一次**：`RawCardScript__TriggerAbility.c:58-62` → `BattleManager__PlayTriggerTraitAnim.c:33`（Action `0x44`）→ `_ResolveTriggerTraitAnim_d__476__MoveNext.c:10-12` → `CardScript__ActivateTriggerTraitAnim.c:6-20`：`GetTraitFromAbilityTrigger(triggerId)` 后 `HighlightTraitIcon` + **`DisplayTriggerAnim(traitId)` 一次**。`CardTraitCollection__GetTraitFromAbilityTrigger.c`：`0→10(rally)`、`0x50(80)→0x78(backlash)`、`0x51(81)→0x3f2(1010 unstable)`、其余按 `AbilityTrigger` 名 lowerCamel 在 `TraitId` 枚举 `TryParse`。且 `BattleCardUI__DisplayTriggerAnim.c:75-82` **只播该 trait 表里 triggerType==3/6 的 TraitEffect**。
  - 因此**第 ④ 档（整桶）不该播任何 `src==cardtrait-so` 的行**；trait 框只归 `TraitFrames.DisplayTrigger`（`trait_effect_bindings.json` 的 3/6 行）。现状 `VfxMap.TriggerBeatRows`（`Core/VfxMap.cs:2720`）只让开「本拍 TraitFrames 真会播的同一件」（`IsTraitFrameDuplicate`），其余 trait 行照播 —— 这就是 R8-b。
  - **例 GOF26+unstable**：`GOF26` 自己的 trigger 81 行是 `UnstableEffect` + `UnstableEffect_ExplosionExtra`（`cards_engine.json` 的 `animTimes`）；`unstable(1010)` 的绑定是 `triggerType 1 (End)`（`Resources/trait_effect_bindings.json`），**不是 3/6 ⇒ 这一拍原版不播任何 trait 框**；`ConcussiveEffect` 属 `concussive(930)` 的 `triggerType 3 (Trigger)`，与 trigger 81 无关 ⇒ **不该播**。来源 `d:/4/Unity/资料/普查产出_1024d/W5_特效VfxMap_执行报告.md:148`。
- **还差什么**：判据齐。要做：把 `TriggerBeatRows` 收成「所有档一律排除 `cardtrait-so` 行」，并同步改 `Editor/BattleVfxAssertE3.cs` 的 `AM27/GOF26/AM15` 与 `BattleScene` 同款夹具期望（那三处现在把缺陷钉成了期望）；再跑 `AnimFXCheck.Run` / `BattleScene.Run` / `BattleVfxAssertE3`。`BattleVfxAssertE3.cs` 若被 W10 占用，等它收工。要 Unity。

### 11 对手聊天「联机收发缺」（只查判据，本线暂停）
- **判据结论**：
  - **发送**：`BattleCommsManager__SendChatMessage.c:20-37` —— 把 **1 个 int（台词序号）装箱**成 object[]，`PhotonView.RPC(<方法>, …)` 发出。协议形状 = **单 int RPC**。
  - **接收**：`BattleCommsManager__PUNReceiveChatMessage.c:20-26` —— 先取 `PhotonMessageInfo` 的发送者标识（`param_3+8 → +0x18`）与本地玩家（`+0x40`）比，**不相等才** `VoiceLinesController__DisplayWarlordChatMessage(ctrl, chatId, 0, 0)`。
  - **显示/静音/录像**：`VoiceLinesController__DisplayWarlordChatMessage.c` —— `param_4==0` 先过 `CanChat`；`DisplayWarlordRegularChatMessage` 播台词；`param_3!=0`（远端/敌方）且联机且 Photon 已连且 `chatId!=0 && chatId!=1` ⇒ `SendChatMessage`（`:67`，防回环）；`AIResponse(force=1)`（`:81`）；**第 5 条**（`+0x6c` 自增后 `4 <`）且一辈子一次 `+0x68==0` 且未静音 `+0x40==0` ⇒ 弹 `MuteEnemyChat` SmallTip 并把 `+0x68=1`、`+0x6c=0`（`:86-109`）；最后把 `PlayerAction` 类型 **`0x37`**（含 warlord in-battle id `+0x22c`、chatId、isEnemy、clip 长度 `+0x4e0`）追加进当前方/敌方动作流水；`matchType==0xa0`（回放）或 `chatId==0/4` 时不记（`:137-155`）。
  - 我们现状：`Net/**` 搜 `Chat` **0 命中** ⇒ 联机局我方发言不发给对面、对面发来的不显示；单机侧 E7 已把触发面/静音闸/打字机/召唤闸做齐（`d:/4/Unity/资料/任务线/战斗线/任务总表.md:220`）。
- **还差什么**（查不到的部分，不许猜）：
  1. `PhotonView.RPC` 的**方法名字符串**：反编译里只是 metadata 引用 `DAT_18427c0d8`，未导出字面量；要拿 `global-metadata.dat` / `dump.cs` / 一次 `strings` 或 IL 读取。
  2. `PhotonMessageInfo` 的字段布局（发送者取自 `param_3+8→+0x18`）需要在 `dump.cs` 里核实类型名。
  3. `BotEmotesConfig` 的概率四值在 RemoteConfig，本地 0 命中 ⇒ 联机/断线重连下「断结构不断原版数」。
  4. 所有 Net 层实现（发送/接收/回环闸/`0x37` 动作进录像）——本线暂停，未动。

---

## 摘要（≤25 行）

- 第 1 条 SW18：**可照做**（判据齐，B3 已落逐张随机；属性/关键词取新卡、格号/手牌位原位不变；余 in-battle id 身份审计）。
- 第 2 条 T03·b 摆位档：**可照做**（阈值 20/10/0 已查实；但档值不是坐标、不影响摆位；20/30 档今天不可达）。
- 第 3 条 T03·c：**可照做**（四层链 + priority 四道消费序 2→1→3→0 全查实；D10 判「不改」）。
- 第 4 条 T03·d 三条：**可照做**（GetsShield 映射不成立、berzerk 黑名单口径、P3-C 表现不发/流水发；全闭合，P2-8 池内 0 张）。
- 第 5 条 新账-89②：**可照做**（原版无 `>0` 闸、负伤害不可达；不改结算）。
- 第 6 条 新账-97 三条：**可照做**（180 发射口 = OnMinionDeath 死因 20；23 档逐档语义已列；terror 方法体三分支已读）。
- 第 7 条 新账-88：**判据/工具可照做**；**仍查不到**首个分叉点 —— 缺一次带 `traceFull` 的真机重放（F7 日志已丢），按 8 条不变量定位。
- 第 8 条换牌 20/10/0：同第 2 条；数值区间已给，今天 20 档不可达。
- 第 9 条 DoReanimate：**可照做**（原版无墓地路；Reanimate 不发 When deployed；W12 已落）。
- 第 10 条 R8-b：**可照做**（原版只播卡级行 + trait 链一次；第 ④ 档必须排除全部 cardtrait-so 行；GOF26+unstable 该拍不播 trait 框）。
- 第 11 条聊天：**判据可照做**（单 int RPC、发送者过滤、5 条 SmallTip、静音闸、`0x37` 动作进流水）；**仍查不到** RPC 方法名字符串 / PhotonMessageInfo 布局 / RemoteConfig 概率值。
- 全局：未改任何代码/数据/台账；未跑 Unity；未动 git；报告因只读工具未能落盘。
