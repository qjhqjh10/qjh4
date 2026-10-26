# B9 · L2 · CardScript 与广播族账批（第八十三会话 · 完全复刻）

> 代理 `L2` · 2026-10-26 · 只改账（写 `账/_parts/B9_L2.tsv`），**未改任何 `.cs`**、未跑 Unity、未动 git。
> 块 = `X-账-CardScript`(41) · `X-广播族改账`(37) · `X-入队同步改账`(23)，共 **101 条**。

## 一 结论

| 结果 | 条数 | 说明 |
|---|---:|---|
| **升 A（写进补丁）** | **42** | 判据 = 反编译方法体起止（逐份核到声明行）；落点 = 符号级重钉 + **该行源码回读确认声明名**（47 行补丁、每条落点机器校验，0 mismatch） |
| **已在 A（未动）** | 1 | `BattleManager.AddFollowResolutionAttackContinuationAction`（前批已 A） |
| **留 B（补丁里修正 2 条）** | **53** | 见 §三；都有真差异或「还有一段没核」，按硬条件不许记 A |
| **判 C** | **3** | 无对应实现（不是「没查到」），见 §四 |
| **并发代理已判 D（未动）** | 2 | `TurnExtensions.GetFinishedTurn` / `SetFinishedTurn`（`B8_L`/`F3` 判的；见 §五·5） |

- 补丁 `账/_parts/B9_L2.tsv` = **表头 + 47 行**（A 42 · B 2 · C 3），11 列、LF、字段无 tab/换行。
- **零代码批**：全部结论靠只读（cpp2il dev_ilspy / decomp_full）+ `ruleprobe`（纯 dotnet，不是 Unity）。

### 判 A 的落实方式（对着简报硬条件逐条）

1. **判据侧方法级**：`cpp2il/out_cpp2il_dev_ilspy/<类>.cs:起-止` —— 起止是用**花括号配平**从声明行算出来的整段方法体（不是「附近大概」）。转调族把 `BattleManager`（一行转调）与 `BattleManagerSupport`（真体）**两段都给**。
2. **我们侧方法级**：落点统一写成 `路径:行（裸声明名）（等价实现 = …）`；**每一处都用脚本回读该行源码、确认那一行确实含那个声明名**（47 行、含多落点段，0 mismatch）。
3. **共享代码**：`AddStealFromDeck/Hand`（同一段 `FindZoneInstance+TakeFromZone`）· `BroadcastTurnEnd/TurnBeforeEnd`（同一段 `EmitAtTurnTriggers`）· `BroadcastUnitLanded` 两条（原版一行转调）· `BroadcastUnitHealed`（同上）—— 按方案 §4.1 的**例外**：给多处声明 + 在 `判据要点`/`差异` 里写清「这段代码如何分别满足各方法」。
4. **有已知行为差 ⇒ 一律不记 A**：本批因此把 2 条**主动从 A 改判 B**（见 §三·B-1），并把 51 条留在 B。

### 观测量（能跑的都跑了）

`ruleprobe check` 本轮实测 **rc=0 · 全池 1121 张 · 解析差异 0 行 · 哨兵失败 0 条**。手牌态监听夹具（`preset hand=self,event=…`）本轮新跑 4 组，产品形状 vs `legacy=1`（改前四跳）都是**有鉴别力的两次读数**：

| 夹具 | 产品形状 | `legacy=1`（改前） | 用在哪些行 |
|---|---|---|---|
| `play SOR38 preset hand=self,event=gainfaith` | **1 条**（信仰 12→13） | **3 条**（多出敌督军 −4） | 与 `CollectFaith` 同族，作口径旁证 |
| `play DA12 preset hand=self,event=gainquest` | **1 条**（任务点 0→1） | **5 条**（4 张友军各回 2 血） | `Support.BroadcastCollectQuestPoints` |
| `play DA15 preset hand=self,event=createssecret` | **0 条** | **2 条**（随机敌人 −3 / 伤害累计 +3） | `BattleManager.BroadcastSecretCreated` |
| `play GSC68 preset hand=self,event=createssabotage` | **0 条** | **2 条**（随机敌人 −2） | `BattleManager.BroadcastSabotageCreated` |

另外两条单卡读数写进了 `备注`：`play GSC49`（抢人：`P1[3]` → `P0[2] {fast1}`，配 `AddStealMinion`）· `play ASH15`（眩晕：`P1[3] {stun1}`，配 `AddStun`）。

⚠️ 其余行的「观测量」**没有硬凑**：`Add*` 族与多数广播族没有单卡夹具，`备注` 里写的是「该看哪个量」（跳数／触发次数／棋盘两列），**没有伪造读数**。按 G3 的口径，这类行如果只有「看着一样」就不该升；本批升它们靠的是**判据方法体 + 落点方法级 + 机制差异写清**（简报硬条件），不是靠读数。

## 二 升 A 的 42 条（分组）

**入队 vs 同步 13 条**：`AddResignAction` · `AddRevealOrders` · `AddShowCardsWindowAction` · `AddStealFromDeck` · `AddStealFromHand` · `AddStealMinion` · `AddStun` · `AddTransformCard` · `AddTransformFromRemnant` · `AddTriggerRequiem` · `BroadcastTurnBeforeEnd` · `BroadcastTurnEnd` · `BroadcastUnitMob`。

> 口径：原版 `Add*` 一律「构造 `BattleAction(type)` → `AddAutoActionToQueue(a, priority)` 只入队」，我们 `Do*` 同步施加同一 payload。差别只在调度形状（调度台 2026-10-25 已裁）。`AddStun` 的 payload 在 cpp2il 里**可读**（`CardEffect{buffType=addTrait, trait=stun}`），其余几条 payload 字段写被反编译成 `unmanaged load` —— 这一层在 `差异`/`备注` 里**如实写「读不到」**，判据只到 action 类型 + 实参语义。

**广播族 20 条**：`HuntMarkResolved` · `MarkResolved` · `SabotageCreated` · `SecretCreated` · `TacticPlayed` · `UnitCodex` · `UnitDuty` · `UnitFerocity` · `UnitHealed` · `UnitLanded`(BattleManager) · `UnitLoseStealth` · `UnitPray` · `UnitRequiem` · `UnitSwarm` · `CollectQuestPoints` · `UnitLanded`(Support) · `UnitSummoned` · `TriggerOtherCardCodex` · `TriggerOtherCardMob` · `TriggerSacrifice`（+ 入队块里的 `BroadcastUnitMob`/`BroadcastTurnEnd`/`BroadcastTurnBeforeEnd` 三条）。

> 每条都写死了三件事：**跳数**（棋盘 1 趟 + 当前方手牌 + 对方手牌 = 3；`EarnedSkull`/`CollectQuestPoints`/`Secret`/`Sabotage` 只有棋盘 1 趟）· **预调**（`Summoned`/`CollectQuestPoints`/`Mark`/`HuntMark`/`Secret`/`Sabotage` 有，其余无）· **自排除**（原版 Support 循环里的 `!=` 与接收方体内的 `!= this` 逐份读了，硬表见 §六）。

**CardScript 族 9 条**：`ChangeOwner` · `FollowUpResolutionPlayed` · `GetTargetsAvailable` · `HasCancelAttackAbility` · `IsActiveAbilityTargeted` · `QuestResolved` · `ReceiveDamage` · `SetRemoveMarkerlight` · `TryDraggingFromBoard`。

## 三 留 B（53 条）

### B-1 补丁里**主动从 A 改判 B** 的 2 条（本轮新查出的真差异）

| 条目 | 真差异（判据逐字） |
|---|---|
| `BattleManager.BroadcastSacrificeResolved` | 原版 Support 那三趟**都没有 `IsInPlay` 闸**（`BattleManagerSupport.cs:1764-1805`），我们的棋盘快照**跳过待死/无卡单位** ⇒ 不在场的监听者原版会调、我们跳过。其余同形（220/225 两支、三趟都排除用献祭那张）。 |
| `BattleManager.BroadcastUnitSpiritStone` | 原版**接收方体** `TriggerOtherCardSpiritStone`（`CardScript.cs:7619-7627`）有 `spUnit != this` 自排除；我们发的是非 `triggers:*` 族（`SpiritAbility`），`SelfExcludesOnKeywordTrigger` **不适用** ⇒ 「自己用灵魂石能力时自己的 455 监听器」我们也会叫醒。补法要改 `.cs`。 |

### B-2 未改的 51 条（分组 + 一句话理由）

**入队同步族**：

- `BattleManager.AddSummonUnit(RawCardScript summonedCard, int summonPos, bool isPlayer, int priority, bool checkIfCasterInPlay, CardScript casterCard, CardEffect cardEffect, bool blockPlayerActions, RawCardScript enchantment)` —— 入队 vs 同步（§六 口径）；另原版召唤带 `enchantment` 参数（同一次落下就挂附魔），我们的附魔是**另一步** ⇒ 「召唤即附魔」的中间态可观察（未实测）
- `BattleManager.AddTriggerCodex(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)` —— 原版入队后由淡出/演出逐拍消费；我们同步一次算完（净结果相同，演出由 BattleDriver 从事件流补）
- `BattleManager.DealMultiDamageVariable(List<DamageInfo> damageInfoList, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)` —— 机制差异同 DealMultiDamage；额外一条：原版这条不写 pendingDamage，所以同时结算的中间态在这条路上根本不存在，我们同步模型反而更贴近
- `BattleManager.ResolveAction(BattleAction battleAction)` —— 机制差异（已核的部分）：分派点从「出队后的 switch」搬到「入队时的闭包」；队列四条道与取序逐条对得上。**未核的那一段** = `BattleManager.cs:21939-22450` 那 500 余行的 switch 分支表本身…
- `BattleManager.ResolveClearEndOfTurnEffectsFrom(CardScript actingCard)` —— 原版按卡、按 untilEndOfTurn 旗标逐条摘；我们在回合末对所有单位统一 RevertBuffs 扫一遍（净结果同，作用域与触发时机不同：原版可对单张卡触发）。🔴 2026-10-26 B9_E4 补读：ResolveClearE…
- `CardScript.OnTurnEnd(bool playerTurn)` —— Courtesy(43)/Resolution(45) 我们不发（池内各 0）；AddDisappearFromHand 半由 SweepEphemeral 承担且排在整段 turn_end 之前（自述）；CancelAttack 半无对应
- `CardScript.SendClearEndOfTurnEffects(int priority)` —— 原版是入队一条动作；我们是回合末同步清（无动作条目）⇒ 净结果同、调度形状不同（与 CLAUDE.md 记的「入队 vs 同步净结果等价」同口径）
- `CardScript.SendRemoveEffect(CardEffect cardEffect, int priority)` —— 原版入队一条动作；我们同步摘（无动作条目）⇒ 净结果同、调度形状不同
- `CardScript.TriggerUnitBacklashActions(UnitDeathType deathType, CardScript actingCard)` —— **缺**：入队前那道 criteria 预检（我们由 ActionPump 的 context 闸承担，未逐句比）；`skipBacklash` 形参无等价物（我们按 deathType 分流）

**广播族**：

- `BattleManager.BroadcastActivateMinions(bool playerTurn)` —— 机制差异：原版**逐卡**调 `ActivateMinion`（每张两道闸 + 重算 `canAct`/`canAttack`）；我们把「重算」抽成单一函数 `RecomputeActAndAttack`，由 `BeginTurnCore`…
- `BattleManager.BroadcastChangedLanguage()` —— 机制差异：原版靠 Unity 的 `BroadcastMessage` 打整棵 HUD 子树（`CemeteryManager` / `BasicCardUI` / `BattleCardUI` 各实现一个 `ChangedLanguage…
- `BattleManager.BroadcastEarnSkull(bool isPlayer)` —— 逐句对过 Support 版 21 行，未发现差异（跳数已按原版收窄）。⚠️ **调用点还没接**：原版 `CardScript.CheckForSkullEarned`（督军血量跨阈值、只在变大时广播一次）的等价物在 `RuleCore`…
- `BattleManager.BroadcastUnitDamaged(CardScript damagedUnit, DamageInfo damageInfo, bool dealtByAttacker)` —— ① 战绩统计那两笔我们**没有对应物**（挑战榜 = P2/P3 统计；另有 `AchievementTally`，口径未核）⇒ 如实记。② `ResolveDamageDealt` 里 7 个 trigger 我方的落点：`UnitDam…
- `BattleManager.BroadcastUnitRegiment(CardScript unitRegiment, CardScript unitRegimentCaster)` —— 机制差异同 `BroadcastUnitCodex`。⚠️ **原版另有前置闸**：`ResolveUnitAttacked.c:159` 判 `HasDefaultTrait(0x4c9)` 不成立就**连广播都不发**；我们由 `Fir…
- `BattleManager.BroadcastUnitSynapse(CardScript synapseUnit)` —— 机制差异同 `BroadcastUnitCodex`。⚠️ 我们**多一个递归守卫** `SynapseBusy`（原版不会自递归）—— 如实记，属刻意差异
- `BattleManager.DealMultiDamageAndDestroySelf(DamageInfo damageInfo, List<CardScript> targetList, int priority, bool checkIfCasterInPlay, CardScript casterCard, bool blockPlayerActions)` —— 机制差异：原版先标将死、后结算伤害，我们先伤害、后自毁，顺序相反；当自毁触发 Backlash 或死亡广播时事件序可能不同
- `BattleManager.ResolveRemoveEffectsFrom(CardScript actingCard)` —— 原版把「摘全部效果」做在 BroadcastRemoveEffectsFrom 的监听器里（棋盘 IsInPlay 全员 + 当前方手牌 + 另一方手牌各一跳，都是 RemoveExtrinsicEffectsFrom）；我们按来源 Rev…
- `BattleManager.ResolveTriggerRequiem(BattleAction battleAction)` —— **本批新建入口**：`IsInPlayOrDying` 闸（不满足什么都不做）→ `FireTriggerAlways(… KeywordTable.Requiem …)`（430 自己）→ `BroadcastUnitRequiem`（…
- `CardScript.TriggerSpiritStone(CardScript actingCard)` —— 逐句对过 ~4 拍；差异：原版「有 600 能力才广播」那道 `HasAbilityType(600)` 闸我们没有专门判（我们按 presence 闸判事件是否发），口径不同但结果相近
- `MinionManager.BroadcastActivateMinion(bool playerTurn)` —— 原版 = 对本侧两排逐张 `CardScript.ActivateMinion(playerTurn)`，一次写两位：canAct(+0x230) + canAttack(+0x23C)。我们**没有这个广播方法**：等价链 = 回合开始那…
- `TurnExtensions.GetTurnStart(RoomInfo room)` —— 原版把回合号与「本回合是否已结束」写进 **Photon Room customProperties**（键 Turn / TStart / FToA）当**状态广播**；我们的 P2P 是**确定性重放**（起始条件 + 动作流，见 Ba…

**CardScript 族**：

- `CardScript.ActivateMinion(bool isPlayerTurn)` —— 原版 displaySummonSickness 是字段，我们是函数 DisplaySummonSickness(u) 现算；原版末尾两句 UI（canAttackAnim / CanUseAbilityNow）属表现层、我们没接；逐句 4…
- `CardScript.AddEffect(CardEffect newEffect, int enchantmentCardId)` —— 只逐句对了 addStats 一支 + 残骸闸（判据 :159-170），K2/K5/K9 那几支没核；逐句约 120/975
- `CardScript.CancelAttack(bool playerInitiated)` —— ① 原版清空 pendingDamage 台账，我们的 pending 是现算的、没有「清空」口；② PrepareAttack(false) 那一半在表现层，引擎侧没有；逐句 1/3
- `CardScript.CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool keepStats)` —— 原版一个方法兼做状态与表现初始化（挂 BattleCardUI / 刷卡面），我们拆成 UnitState 构造 + 部署状态两处、表现不在引擎侧；逐句约 60/350
- `CardScript.CheckReplacedAbility()` —— 原版是「运行时挂上去再重算」；我们读卡表列（87 张 hasActiveAbility=1）+ activeAbility 正文，没有 CheckReplacedAbility 这个重算入口；逐句 2/4
- `CardScript.ExecuteSynapeEffect(CardScript actingCard)` —— 原版走 RawCardScript 的 OnCardPlayedWithTargetJustThisCardPlayed 入口逐邻重放；我们重放 tactic 的 op 列表，等价点=逐邻、差异点=入口不同；逐句 2/3
- `CardScript.FinishAfterAttack()` —— 原版一次复位三件；我们位置与缩放归 AttackRecover、状态复位归 AttacksThisTurn/Exhausted；逐句 3/4
- `CardScript.ForceChangeMaxHealth(int newHealth)` —— 🔴 原版改上限；我们的 DoSetHealth 只把当前血写成当前上限（t.Health = t.MaxHealth），不改上限 ⇒ 语义不同；逐句 1/3
- `CardScript.GetHighestAttackValue()` —— 逐句 2/2：同一式、同一个 max 口径（判据已被 G3 那条自检引过）
- `CardScript.GetOwnHero()` —— 结构差异：原版督军是 `BattleManager` 两侧的两个字段（按 `isPlayer` 位取），我们督军是棋盘固定格上的一个 `UnitState`（按座位号取）⇒ 同一件事只有一处读点；反过来「督军临时不在场」时两侧答案形式有别（…
- `CardScript.HasDiedSinceLastTurn()` —— 原版按「死亡回合号 vs 当前回合号」判两回合窗口；我们用两个计数（本回合 / 上回合，BeginTurnCore 滚动）；逐句 2/2
- `CardScript.HasDiedThisTurn()` —— 同一条判据的单回合切片，两侧都按回合号；逐句 1/1
- `CardScript.HasEnoughPendingDamageAndIsRemnant()` —— 原版三合一谓词；我们的判死闸与残骸翻转分在 CleanupDeaths 两处，没有这个谓词本身；逐句 2/3
- `CardScript.IsSameTrait(CardEffect effect1, CardEffect effect2)` —— 原版会按三格相等把增量累加进已有记录；我们的 StatMod 从不合并（2026-10-25 E1 明确口径）⇒ 同一效果挂两次时条目数不同、投影数值相同
- `CardScript.IsTheSameTurnPlayed()` —— 逐句对过 37 行。原版比的是「当前全局回合号」（一个 ObscuredInt 计数器，双方回合都推进）；我们在**两个部署入口写快照** `DeployedTurn = ctx.Turn`、读点同样是「与当前回合号比」⇒ 同义。差异两处：…
- `CardScript.IsValidActiveAbilityTarget(CardScript target)` —— 同义（F1 落点）。差异：E3 已列「camouflage 与 trait 0x46 两格在哪没核」，本轮**未再动**。
- `CardScript.OnTurnSetup(bool playerTurn, bool isInHand)` —— 原版按「效果上记的归属旗标」逐条摘；我们按「到期回合数」两段清（限时增益/失明），且 turn_setup 无独立钩子（注释已如实说）⇒ 逐条形状不对应
- `CardScript.OnTurnStart(bool playerTurn)` —— 30/31 我们二选一发（EffectResolver.cs:8248-8257 自述）；缺 Psyker/Reactor/Transient 加资源（池内 0）
- `CardScript.PrepareAttack(bool toPrepareAttack, bool playSound)` —— 原版是「先摆位再攻击」的演出协程；我们把摆位/音效交给 BattleDriver 的攻击件、规则在 DeclareAttack ⇒ 语句不在同一函数里
- `CardScript.RemoveFromGameCard(CardScript removedCard)` —— 原版有独立 state 11 与 UI 收尾；我们用 Removed 表 + 视图移除 ⇒ 记法不同、净效果相同
- `CardScript.ResetToValuesInHand(bool resetTriggeredBacklash)` —— 我们用「换新 UnitState」达成前半；CardInstance 那层实例态按用户 2026-09-18 口径保留（与原版逐字段重置不同口径）
- `CardScript.SetAsPlayer(bool isPlayer, CardArmy army, bool setCardback)` —— 原版的 +0x40 是每卡一份；我们是「引擎侧 owner 索引 + 表现层 _me 比较」⇒ 无每卡标志，语义等价
- `CardScript.SetFutureRemnant(bool willBeRemnant)` —— 逐句对过 8 行。**差异**：原版是**可变位**（可以「先预标将来会变残骸」，再由翻面那一刻清 `isLeavingRemnant` 并置真）；我们侧**无「预标」这一档** —— `IsRemnant` 只在**新建残骸 `UnitS…
- `CardScript.SetLeavingRemnant()` —— 逐句对过 8 行 + 配对的两个清零点。**差异（结构）**：原版在**同一个对象上翻位**（`+0x66=1` 片刻 ⇒ `currentCombatAttack` 那一支落回正常攻击值；翻回完成后 `+0x65/+0x66` 一起清 0…
- `CardScript.SetStateAsMulligan(bool toBeDiscarded)` —— 逐句对过 ~8 句；机制差异：原版用 cardState 13/14 编码「换牌区·保留/弃掉」，我们不设状态位，由面板挂载与选中集合表示
- `CardScript.SetupActiveAbility()` —— 逐句对过 1 句；机制差异：原版一个状态位 3，我们没有状态位（`OathUsesThisTurn` 等配额字段承担「用过没」）
- `CardScript.SetupAttack(bool playSound, bool playerInitiated)` —— 逐句对过 ~12 句；机制差异：原版的 `(*(byte*)(+0x40) & playerInitiated)` 位与判都无对应物（我们的「谁发起」由调用方给）；StartCoroutine(播音) 走 CardFeel/AttackSe…
- `CardScript.ShowDropPodFigures()` —— 逐句对过 ~6 句；机制差异：原版是**查询**（谁调用它决定是否展示 figures，调用点未查），我们是**动作**（开舱即换值）
- `CardScript.TriggerCodex(CardScript targetCard)` —— 只做「能量恰好归零」这一条闸（原版 Codex 的闸就在能力数据里），其余在 PlayCard/PlayTactic 路径上天然成立；目标集 = 同方场上带 codex 者。**差异**：原版「目标卡或自己」那个第 6 参我们是 `null…
- `CardScript.StartShowingCardInHand()` —— 逐句对过 8 句：本轮补了 tooltip 与第二颗 collider 两句；`ToggleOpenCardDisplayOnTouch(false)` 早已在 `CardDisplaySuppressedFor`；位姿 / 缩放 / 排序…

## 四 判 C（3 条）

| 条目 | 原版做什么 | 为什么是 C（无实现） |
|---|---|---|
| `CardScript.ClearPendingDamage()` | 把 `pendingDamage` 台账 size 置 0 + 一行日志（`CardScript.cs:13329-13345`） | 我们没有这个台账：判死用 `EnoughPendingDamageToDie` **现算** ⇒ 没有可清的中间态，**没有落点可指**。 |
| `CardScript.RemoveCardFromCemetery()` | 只清 `+0x24b`「正在从墓地取出」的 UI 闸标志（`:16422-16425`） | 我们的墓地就是 `PlayerState.Discard` 表，**没有这个中间态**。 |
| `CardScript.SetCardState(CardStateOptions)` | 把 `cardState(+0x228)` 写成入参（18 档状态机唯一入口，`:9521-9530`） | 我们**不存卡状态**：视图由「在哪张表里/哪个布局」直接驱动 ⇒ 缺这个状态机（架构选择，属表现层口径六）。 |

## 五 疑点与没查清（下一批必须知道的）

1. 🔴 **主账在我工作期间被并发重写了两次**：开工时 1,539 行、收工时 1,373 行；路线图里给的「账行号」**全部失效**。本批一律按 **(域, 原版类, 方法)** 键工作；**合并补丁时别按行号**。
2. 🔴 **`RuleEngine/Core/*.cs` 的行号在 10 分钟内漂过**（实测 `EffectResolver.cs` 第 6600 行前后内容在两次读之间变了）⇒ 补丁落点行号 = **写入那一刻**的读数；合并/复跑后必须走一次 `check_ledger_points --all-A --fix`（只在「无 `.cs` 写手 + 补丁已合并」时跑）。每条 `备注` 都写了「行号以 2026-10-26 现读为准」。
3. **`cpp2il` 的 payload 字段写被摊成 `unmanaged load`**：`AddRevealOrders` / `AddShowCardsWindow` / `AddTransformCard` / `AddTransformFromRemnant` / `SendClearEndOfTurnEffects` / `SendRemoveEffect` 这 6 类都有这一段 ⇒ 判据只到「action 类型 + 实参语义」，**字段级载荷读不到**。要更深的等价性只能 VA 反汇编（本批没做）。
4. **`BroadcastEarnSkull` 的发射点缺失**（`BroadcastEarnedSkull` 定义在 `EffectResolver.cs`，全仓**没有生产调用者**；原版上游是 `CardScript.CheckForSkullEarned`）⇒ 那条留 B，**建议单独立一条施工项**（不是账能解决的）。今天**不可观测**：池内 1,128 张只有 1 张提到 skull（`Ferocious Rage` 的 `[skull] Slay:`，不是该触发）⇒ 640 的监听者 = 0。
5. **`TurnExtensions` 三条的现状（工作期间被并发代理改判了 2 条，如实记）**：`GetFinishedTurn` / `SetFinishedTurn` 已被 **`B8_L`/`F3` 判 D**（更强的证据：`PunTurnManager` 除自身与 `IPunTurnManagerCallbacks`（0 实现者）外**零引用** ⇒ 整条 Photon 回合管理器链在原版里就没接上）；**只剩 `GetTurnStart` 仍 B**（读 `TStart` = **服务器时间戳**，我们这套没有对应量 —— 确定性重放里没有"服务器时间"）。我原来的建议（裁 D）与那次裁定一致，**不需要再动**。
6. **`MinionManager.BroadcastActivateMinion` 仍 B**：我们**没有这个广播方法**（等价链 = `BeginTurnCore` 逐格 + `RefreshForNewTurn` + `RecomputeActAndAttack`），且有三条已记差异（督军不参与 / 清召唤病无条件 / 触发面不同）⇒ 不是「落点指错」，是**结构不同**。
7. **`AddTriggerCodex` 仍 B**：原版 action 的载荷带 `targetCard`，我们 `FireTriggerAt` 没有该形参的等价物（与 `TriggerCodex` 同一格差异）⇒ 目标为「它卡」时我们按同方全员触发。
8. **`AddSummonUnit` 仍 B**：原版把 `enchantment` 作为召唤动作的载荷（「召唤即附魔」一拍照完），我们附魔是**另一步** ⇒ 中间态可观察（未实测）。
9. **可观察量的边界**：`ruleprobe play` 只输出局面（棋盘两列 / 资源 / 伤害 / 变化条数），**不输出** `Exhausted`/`AttacksThisTurn`/`cardState` 这类字段 ⇒ 依赖这些字段的 A 判定在本批**没有硬读数**，只有断言出处（本批因此没升那几条，见 §三）。

## 六 广播族自排除对照（本批逐份读出来的硬表，下一批直接用）

| 广播 | 原版棋盘趟排除 | 原版手牌两趟排除 | 接收方体自排除 | 我们 | 结论 |
|---|---|---|---|---|---|
| UnitSummoned | 排除被召唤者 | 不排除 | 按 param_2==param_1 分 190/195 | PreCallBoardListener(selfOnlyOnly) | 一致 |
| UnitLanded | 不排除 | 不排除 | `landedUnit != this` | triggers:landing 默认自排除 | 一致 |
| UnitCodex | 排除 codex 那张 | 不排除 | `codexUnit != this` | triggers:codex 默认自排除 | 一致 |
| UnitMob | 排除 caster | 不排除 | `mobUnit != this` | triggers:mob 默认自排除 | 一致 |
| UnitRegiment | 排除 caster | 不排除 | `unitRegiment != this` | triggers:regiment 默认自排除 | 一致（另有前置闸未复刻 ⇒ 仍 B） |
| UnitRequiem | 排除 caster | 不排除 | `requiemUnit != this` | OtherCardRequiem 显式自排除 | 一致 |
| UnitSwarm | 排除被并者 | 不排除 | 无 | triggers:swarm 默认自排除 | 一致（手牌那侧不可观测） |
| UnitUsedSacrifice | 三趟都排除 | 三趟都排除 | 无 | triggers:sacrifice 默认自排除 | 自排除一致、IsInPlay 闸不同 ⇒ B |
| UnitObtainedMark / HuntMark | 排除被标记者 | 排除被标记者 | 无 | preCallSelf（只排棋盘） | 棋盘侧一致；手牌侧不可观测 ⇒ A |
| UnitDuty / UnitFerocity | 不排除 | 不排除 | 无 | 名单内 ⇒ 不自排除 | 一致 |
| UnitHealed / UnitLoseStealth / UnitPray / EarnedSkull | 不排除 | 不排除 | 无 | 非 triggers 族 ⇒ 不自排除 | 一致 |
| UnitSpiritStone | 不排除 | 不排除 | `spUnit != this` | 非 triggers 族 ⇒ **不自排除** | 不一致 ⇒ B |
| CollectQuestPoints / Secret / Sabotage | 预调 + 棋盘排除 | **没有手牌趟** | 无 | boardAndPersistentOnly | 一致（结构差：预调那一拍我们传 null） |
| TacticPlayed | 无（接收方按同侧 + Artifice 判） | **没有手牌趟** | 按特质判 | FireTriggerOnSide(Artifice) | 一致（筛选器 HasFx 的通用差异另账） |

## 七 交接

- **补丁**：`账/_parts/B9_L2.tsv`（表头 + 47 行）。合并建议：`--only B9_L2`，合并后**跑一次 `check_ledger_points --all-A --fix`**（行号会漂）。
- **下一批最值钱的 3 件**（都不是账能解决的）：① `BroadcastUnitSpiritStone` 的自排除缺口（加进自排除口或按接收方逐条判）；② `BroadcastSacrificeResolved` 的 `IsInPlay` 闸（要不要按原版去掉快照过滤）；③ `BroadcastEarnSkull` 的发射点接线。
- **`TurnExtensions` 三条**等调度台裁 D（P3/Photon 房间属性；替代物 = `SendFingerprint` 指纹对拍）。
- **剩余 CardScript 族 B** 里约 20 条是「逐句 N/M 没读完」（`AddEffect` 120/975、`CardSetup` 60/350、`ActivateMinion` 4/6…）⇒ 想升 A 只能**回原版读完整的体**，不要靠账上补字。
- **复跑读数**（本批已跑、可直接引用）：`ruleprobe check` = rc 0 / 全池 1121 / 解析差异 0 / 哨兵失败 0；4 组手牌态夹具两档读数见 §一。

> ⛔ 本批未改任何 `.cs`、未跑 Unity、未动 git、未碰他人的 `_parts/B9_*.tsv` 与 `判据/B9_*.tsv`。
