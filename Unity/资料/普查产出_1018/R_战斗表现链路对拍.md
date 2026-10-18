# R · 战斗表现链路对拍（2026-10-18 · 只读）

> 只读代理产出（无写文件工具 ⇒ 主对话原样落盘）。
> 路径缩写：`A = d:/4/Unity/MyGame/Assets` · `D = D:/2/tools/decomp_full`（全量反编译，Ghidra 伪码）。

## 1 一句话结论

**我们是「先算完、再按事件表补演」，原版是「协程里一步一步边算边演、并挡住下一次操作」**：
一次动作在我们的引擎里同步跑完整段（多目标伤害、死亡、触发链都在内），表现层只拿到一串已发生的事件，
靠 `EventTiming` 排时间线补播；单条动画/音效的**时长**大多有原版出处，但**先后、阻塞、状态可见时刻**是系统性偏差。

## 2 链路图

**我们**
1. 拖牌松手 → `A/CardPresentation/Hand/CardInteraction.cs:552` 先播 `DeploySequence`（0.3+0.05+0.2=0.55 s），**onDone 才** `OnCardDeployed`→`DoPlay`。
2. `A/CardPresentation/Battle/BattleDriver.cs:8357` `LocalAct(playAct, () => RuleCore.PlayCard(...))` —— 无等待、无阻塞。
3. `A/RuleEngine/Core/RuleCore.cs:2404-2437` 扣费/`Emit(Play)` → `ResolveOps` 同步结算（`EffectResolver.cs:172`）→ `ctx.Emit` 只入队（`BattleContext.cs:1415-1420`）。
4. `BattleDriver.cs:11749 → :11826` `RefreshAll→PlaySignals`；`DrainSignals`（`:11831`）一次搬空。
5. 先**当场**播三条要「批内视野」的 cue（`:11839-11845`），再按 `EventTiming.DelayBetween/DurationOf`（`A/CardPresentation/Core/EventTiming.cs:34,86,149`）算 `at` 排进 `_timeline`（`:11904-11927`）。
6. `AdvanceTimeline`（`:12152`）每帧把 `at<=_clock` 的**全部**抽出 → `PlaySignal`（`:12579`）→ `VfxMap` 选件（`:12611`）/`PlayBattleSfx`（`:12256`）/`CardFeel` 补间/飘字/AnimFX prefab（fire-and-forget 自销毁）。
7. 同一帧 `SyncBoard`（`:13344`）把血量、格位、死亡改成**最终值**（`:13417` SetData）；只有死亡视图靠 `PendingExitFor` 延后销毁（`:13392`）。

**原版**
1. 动作入 `BattleManager` 动作队列；`BattleManager__WaitingForBlockingAction` 被 `CanPlayCard` / `ExecuteActiveAbility` / `EndTurnClick` / `Update` 调用 ⇒ **结算期间不能下一手**。
2. `_ResolvePlayCardFromHand` 状态机：付费/离手 → `MinionPlayedIntoField` 协程（`D/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:659-662`）→ `WaitForSeconds(0.3)`（`:673`）→ 之后才 `ResolveCardPlayed`（`:1081`）。
3. 攻击：`SetupAttack` → `WaitForSeconds(attackStepTime*2)`（`D/BattleManager._ResolveAttack_d__438__MoveNext.c:836-840`）→ `ResolveAttackDamage` → `ReceiveAttackAnim`。
4. 伤害逐目标：`D/BattleManager._ResolveMultiDamage_d__456__MoveNext.c:136-168` 每目标 `GetAdjustedDamage→BroadcastUnitDamaged→(最后一个才播 unitDamage)→CardScript.ReceiveDamage`。
5. 每一拍动画都等：`D/BattleManager._ResolveAnim_d__466__MoveNext.c:144` `WaitForCompletion(tween)` + `:200-207` `GetAnimDuration`→`WaitForSeconds`。
6. 收尾统一 `FinishResolvingAction`（`D/…ResolveTriggerTraitAnim_d__476:27`、`…AbilityIconAnim_d__475:27`）。
7. 音效**硬编码在方法体**：`D/CardScript__LandOnGround.c:41`（Play3D）· `D/CardScript__DrawCard.c:50` · `D/CardScript._UnitDeath_d__446__MoveNext.c:368`；prefab 自带 `sounds` 由 `AnimFXController.Update` 定时（`D/AnimFXController__Update.c:31-70`）。

## 3 逐项对拍表

| 项 | 我们怎么播 | 原版怎么播 | 档 |
|---|---|---|---|
| 出牌·单位 | 0.55 s 三段落位**之后**才调引擎；`Play`/`Deploy` 特效、落地音、震屏都在落地那一拍起（`BattleDriver.cs:12288-12302`、`:12960`） | 落位协程→0.3 s→`ResolveCardPlayed`；`playMinionFromHand` 在起飞（`:400`）、`LandOnGround` 在落地 | ② |
| 出牌·战术 | 同一段 `DeploySequence` 也跑（`CardInteraction.cs:552`），落地后才结算，手牌卡直接 `Kill`（`:8381`） | `NonTargetSpellPlayed`+`DissolveWholeCard`（`:633,:638`）或 `TargetedSpellPlayed`（`:1263`），**没有飞向格位** | ② |
| 主动技 | 点击即同步结算；`Ability`→`CardAnimOrder` 展拍（63 卡/100 拍），按 `AnimInfo` 时长排；每拍**只发 VFX**（`:12784-12815`），无位移/朝向 | `AbilityLogic.PlayAnim` 逐 CardAnim；每拍按自己的 `waitAnimToApplyEffect` 等完再结算（`D/AbilityLogic__PlayAnim.c:274`） | ② |
| 攻击命中 | `Attack`→`Hit`，近战 +0.1 s、远程 +1.2 s；`CardFeel.MeleeAttack` 冲击帧 0.1 s；但命中音提前（见偏差 4） | `SetupAttack`→0.2 s→`ResolveAttackDamage`→`ReceiveAttackAnim`，伤害随受击帧落 | ② |
| 受伤/死亡 | `Hit` punch 0.5+0.25、飘字；`Death` 0.2 s 溶解、前置 `DeathHold 0.1`（我们挑的）；**HP 在动作返回时已是终值** | `ReceiveDamage`/`UnitDeath` 协程里改 HP；`deathTimeMinionDuration=0.2`，音效 `:368` | ②（数值①、状态时刻②） |
| 触发式（Rally/Strike/Slay…） | `Trigger`→`TriggerHold 0.3`/`TriggerLen 0.2`；徽标脉冲+状态框+**通用** `Faith_trigger_unit` | `ResolveTriggerTraitAnim`：`ActivateTriggerTraitAnim`→0.2 s→Finish；只有图标/状态框/指定词条粒子，**无通用特效** | ② |
| 回合开始/结束 | `EndTurn`+`BeginTurn` 同步（`:9743`），`RefreshAll` 补演；HUD 只有一行 `TURN N YOUR TURN` 文字 | `ResolveEndTurn` 入队逐条演；`YourTurnUI.DisplaySign` 淡入→等→淡出（`D/YourTurnUI._DisplaySign_d__5__MoveNext.c:28-60`） | ②结算／③大牌子 |
| 单条演出时长来源 | 落位 0.3/0.05/0.2、攻击 0.1/0.2、死亡 0.2、技能 Σ`GetAnimDuration`（`EventTiming.cs:179-236`、`AbilityAnimDurations.cs`） | 同源字段（clip 关键帧 / UnitTweenSO / 卡预制体 / `VarsGlobal`） | ① |
| 特效 prefab 内音效/粒子 | AnimFX 941 条 `sounds/exitSounds` 按 `PlaySoundOnTime` 推进 | `AnimFXController.Update` 同一条判据 | ① |
| 逐卡逐拍覆盖 | `card_anim_order.json`：63 卡/100 拍、`cards_no_steps=834`；时长表 93 张，查不到退 1.0 | 每张 `activeAbility` 都有 `cardAnimations[]` | ③（大多数卡仍是一拍） |

## 4 偏差清单

1. **结算永远领先表现**。动作一返回，HP/格位/死亡/召唤物已是最终态，`SyncBoard` 立刻 `SetData`（`BattleDriver.cs:13417`）；
   原版 HP 在 `ReceiveDamage` 随受击帧改（`…MultiDamage:154-168`）。影响：血条/数字先跳、挨打动作后到；触发产物先出现在棋盘、特效后补。
2. **没有阻塞/串行**。`LocalAct` 无等待（`:259-275`），AI 固定 0.55 s 一步（`:11499-11524`），输入不受 `_timeline` 约束；
   原版 `WaitingForBlockingAction` 挡 `CanPlayCard`/`ExecuteActiveAbility`/`EndTurnClick`/`Update`。影响：可连点出牌、AI 可大步跨过动画。
3. **新事件会插队**。`PlaySignals` 用 `t=_clock`（`:11893`），不占住已排的未来 `at`；下一动作的事件更早 ⇒ `AdvanceTimeline` 先播它（`:12217-12229`）。
   影响：连点时后一动作的特效/音效压过前一动作的收尾。
4. **三条 cue 提前播**。`PlayUnitDamageCue`/`PlayMeleeHitCues` 在 drain 时立刻 `BattleSfx.Play`（`:11842,11845,12480-12483`），不随 `at`；
   原版分别在命中回调与 `ReceiveDamage` 那一拍。影响：近战命中音早 0.1 s，法术/远程 dot 早整段前摇。
5. **战术卡多一段 0.55 s 飞向格位**（`CardInteraction.cs:552` + `BattleDriver.cs:8381`）。原版无此段。
6. **主动技只演一半**：`PlayAbilityBeat` 只 `CardEffects.Fire`，`endPosOption`/`pointTowardsOption` 未消费（`:11940-11941`，`:12784-12815`）；
   拍时长是名义值，**不等 tween 完成**（对照 `…466:144`）。
7. **触发式多播一件原版没有的通用特效**：`VfxMap` 的 `Trigger → "Faith_trigger_unit"`；原版 `ActivateTriggerTraitAnim` 只 `HighlightTraitIcon`/`DisplayTriggerAnim`。
8. **AI 出牌没有落位演出**：`AfterAiAction` 只 `RefreshAll`（`:11542`），视图由 `SyncBoard` 直接建在位；原版 AI 走同一个 `_ResolvePlayCardFromHand`。
9. **音效结构不同**：我们「事件→cue 表」（`:12256`）；原版硬编码在动画方法体子时刻。Deploy 一次同时播四条（`:12288-12302`），原版前两条起飞、后两条落地，中间隔 0.55 s。
10. **快进/跳过**：产品内**没有**跳过入口；唯一丢事件的是批处理 `DropSignals`（`:12237`，仅 `Editor/BattleScene.cs:7835`）。

## 5 没查清

- `YourTurnUI` 的外部调用点（`grep DisplaySign` 只命中自身 3 个文件）。
- 原版 `_PlayAi` 内部 `WaitForSeconds` 的**实参**（伪码没落值）。
- `ResolveEndTurn`（870 行）逐条动画的完整顺序：只读了两处。
- 我们 tactic 拖放时 `DropReject` 的 slot 语义未跑 Unity 验证。
- `UnitTweenSO.waitAnimation` 在我们时长表里**没算**（`AbilityAnimDurations.cs` 文件头自述），实际偏差量未测。
