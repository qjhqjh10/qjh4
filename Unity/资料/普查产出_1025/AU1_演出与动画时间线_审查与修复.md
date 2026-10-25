# AU1 · 战斗演出与动画时间线 —— 审查与修复（2026-10-25）

> 审查+修复代理 `AU1`。片 = **打出 / 登场 / 攻击 / 受伤 / 死亡 / 回手 / 换位 / 扇形排布**的**时间线**（时长、缓动、延迟、串行/并行、先后次序）。
> 判据优先级：① `d:/2/tools/decomp_full/`（第一权威）→ ② 解包资产字段 → ③ 运行时 dump → ④ 说明书 → ⑤ 本仓已有判据件。
> ⛔ 未跑 Unity / 未动 git / 未改 `CLAUDE.md`、`项目任务.md`、任何 `任务总表.md`。
> ✅ 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（改前改后各跑一次；中途两次红全在**别人的文件**上：`AttackSelector.cs` / `Editor/CardBaseDemo.cs`，隔 60 s 重跑即 0）。

---

## 〇 一句话结论

**审了 57 条**时间线参数：**✅ 符合 43** · **🔴 已修 3** · **⚠ 判据不足 10** · **📌 需主对话排批 1**（另有 4 条只剩真机，见 §四）。
改前这一片最大的三处偏离：**①命中帧同帧次序反了（且震屏被受击视图的 null 挡掉）** · **②`AnimInfo.startDelay` 整列零消费** · **③`EvtKind.Return`（回手）被登记成 0 s、原版队列其实等 0.75 s**。三条都按第一权威反编译改完、typecheck 0/0。
另查出 **2 条真缺陷但判据不足/落点在白名单外**（`play` 桶相对 `Deploy` 的先后、`EvtKind.Play` 的居中展示那一段），逐条写在 §三。

---

## 一 差异总表

判定口径：**✅ 符合** = 原版值与我们的值逐条对得上；**🔴 已修** = 本次改掉；**⚠ 判据不足** = 我们值可疑但判据没闭合（⛔ 没猜）；
**📌 需主对话** = 判据齐但落点/基线要主对话排批；**🟠 只能真机** = 静态判不了。

### 1·1 近战攻击（`CardScript._AttackMeleeAnim_d__357`）

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 1 | 抬刀停顿 | `WaitForSeconds(attackStepTime × 2.0)`；`attackStepTime` = 卡预制体 **0.1** ⇒ 0.2 s（`BattleManager._ResolveAttack_d__438:836-843`，`DAT_1834b2bbc`=2.0） | `EventTiming.AttackWindUp = 0.2f`（`Core/EventTiming.cs:599`） | 无 | ✅ |
| 2 | 段1 位移（=命中帧） | `DOMove(end, 0.1)`（`_d__357:194-207`） | `AttackStepMelee = 0.1f`（`:610`）· `CardFeel.MeleeAttack` 0.1 | 无 | ✅ |
| 3 | 段2 转向 / 段3 停 / 段4-5 归位 | 0.1 / 0.1 / 0.2（`_d__357:246-280`） | `CardFeel.AttackHoldTime=0.1`（`:903`）· `AttackReturnTime = 2×0.1`（`:905`） | 无 | ✅ |
| 4 | 出手 → 伤害结算 | 整条 0.4（`_ResolveAttack:988-1000` 之后才 `ReceiveDamage`） | `MeleeDamageLag = 0.4f`（`:640`） | 无 | ✅ |
| 5 | **命中帧同帧次序** | **震屏 → 命中音 → 命中粒子 → 后坐**（`CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:16-27` → `CardScript__ResolveAttackAnimationEffects.c:51-107` → `ReceiveAttackAnim`） | 改前：粒子 → 音 → 后坐 → 震屏（`BattleDriver.cs:20131-20138` 旧） | **次序反 + 震屏被 `target==null` 一起挡掉** | 🔴 **已修** |
| 6 | 段2 旋转基 | `AngleAxis(90, <未解出的 12 字节>) × 目标旋转 × AngleAxis(25, ·)`（`_d__357:214-257`） | `homeRot × Euler(0,90,0) × Euler(0,0,25)`（`CardFeel.cs:996-998`） | 读数串字节 | ⚠ 判据不足（沿用 2026-10-23 结论） |
| 7 | 序列 `timeScale` | `sequence.timeScale = attackSequenceTimeScale`（本 prefab = 1.0） | 未接（`CardFeel.cs:990-1009`） | 今天零可见差 | ⚠ 结构缺（P2，无可见差） |
| 8 | 抬手 0.2 s 的三件副作用 | `SetupAttack→PrepareAttack(1)`：`SetSortingLayer` / `ActivateMinion` / `StopShowingStealth` / `SetTraitsOnPlayAttackingStatus(1)` / 音 | 只接了音（`BattleDriver` `SetupAttack` 音那一段） | 三件缺（**卡不动是对的**） | ⚠ 判据半齐（我们卡上有没有等价物未查） |

### 1·2 远程攻击

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 9 | 回正段 + 再等 | `DOLocalMove(home,0.1)` 三条 + `WaitForSeconds(0.1)` ⇒ **0.2**（`_ResolveAttackRangedAnim_d__360:80-124`） | `AttackStepRanged = 0.2f`（`:617`）· `CardFeel.AttackRecover` 0.1 | 无 | ✅ |
| 10 | 伤害时刻 | `Attack + 0.2 + AnimInfo.GetAnimDuration()`（`_ResolveAnimInfoRangeAttack_d__468:111-124`） | `RangedDamageLag` + `CardAnimClips.RangedChainDuration` 三档（`CardAnimClips.cs:242-280`） | 无（2026-10-23 P1 已修 441→真值） | ✅ |
| 11 | 收招第二段 | `CardScript.AfterAttack`：`DOLocalMove(静止位, 0.1)` + `SetDelay(0.65)`（`DAT_1834b315c`=**0.65**）＋逐帧深度缩放 | `CardFeel.AttackRecover`：0.1 + `AppendInterval(0.65)` + 0.1（`CardFeel.cs:2199-2211`，`AfterAttackDelay=0.65` `:917`） | 无 | ✅ |
| 12 | 远程无命中音/震屏/`AttackHitSmall` | `_ResolveAttack:1114-1152` 远程支没有 | `!e.Ranged` 才排命中帧拍 | 无 | ✅ |
| 13 | `vfxDelayTime` 计一次 | `_DelayedVFXPlay_d__469:17-27` 只等一次 | 改前 `clip.T` + `clip.Place.VfxDelay` **双计**；现只信 `clip.T`（`BattleDriver.cs:21852-21858` 注释段） | 已修（今天 `vd` 全 0 ⇒ 零可见差） | ✅ |
| 14 | 弹道位移（`shouldMoveVFX`，32 卡） | `CardAnimController__Update.c:60-97` | `ArmAttackVfxMove` + `TickAbilityVfxMoves` 泵 | 无 | ✅ |

### 1·3 受击 / 技能动画（`UnitAttack(50)` / `UnitDamaged(60)`）

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 15 | `Hit` 这一拍占多久 | `_ReceiveDamage:342` `WaitForSeconds(1.0)`（`DAT_1834b2bb8` 实读 1.0），`doItFast=false` ⇒ 真执行；`_ResolveAttack:1152` 以 0.2 一拍轮询放行 ⇒ ≈1.0 s | `HitWaitSeconds = 1.0f`（`:684`） | 无（2026-10-24 已由 0.75 改 1.0） | ✅ |
| 16 | `Hit` 两个例外 | ①`DamageType` 7/8 置旗前先等 0.4 ⇒ 那条路**没有这道门**；②AoE 传 `extraValue` 可能快进 | 未分第二档（`:668-681` 记着） | 占比未统计 | ⚠ 判据不足（不分档，已记） |
| 17 | 受击后坐幅度/方向/空间 | `GetPushBackFactor(GetUnitSize(攻方))`；方向 = `normalize(受击方−攻击方)` | `CardFeel.PushBackMagnitude` · `PushBackDir` | 无 | ✅ |
| 18 | 后坐的**应用空间** | 原版 punch 打在卡 transform（父链 `MinionManager`，其 `lossyScale` **没查**） | 乘 `CardFeel.ToOurs`（1.686） | 若原版父链 scale=1 ⇒ 我们大 1.686× | ⚠ 判据不足（沿用 2026-10-23） |
| 19 | 单候选拍的 `Duration` | 队列等 `GetAnimDuration()` | `SkillBeatDuration` 消费端补真值（`BattleDriver.cs:21925`） | 无（16 卡已修） | ✅ |
| 20 | 受方 `UnitDamaged(60)` 时刻 | `_ResolveAttack:866` 早于 `:899` ⇒ `Attack+0.2` | `ScheduleAction(EventTiming.AttackWindUp, …)`（`BattleDriver.cs:20168`） | 无 | ✅ |
| 21 | 技能拍队尾 | 队列等 `GetAnimDuration` | `ExtendTimelineTail(_clock + at + max(0.05,dur))`（`:21861`） | 无 | ✅ |
| 22 | **`AnimInfo.startDelay`（0x28）** | `_ResolveAnim_d__466:67-75`：state 0 先 `WaitForSeconds(startDelay)` 才 `DelayedVFXPlay`；**不计入** `GetAnimDuration` | 改前**整列零消费**（数据里 1 行 0.5 s：`Cuts Brutal Warp Spider`） | 那一件早 0.5 s 起播 | 🔴 **已修** |
| 23 | `minionMovementInfo`（0x70） | `_ResolveAnim:222-236`：非空 ⇒ `CardScript.MoveAndDisappearAnim(卡, boardPos, target, moveInfo)` 并 `yield`（卡**本体**沿曲线飞+消失） | **零消费**（9 CardAnim / 10 行 / 8 卡） | 缺一整段；⚠ **不改变这一拍时长**（时长仍是 `GetAnimDuration`） | ⚠ 判据不足（数据侧 `gen_cardanim_clips.py` 不导 → 不在本片白名单） |
| 24 | `screenShake`（0x60） | `_ResolveAnim:97-105`：`CanPlay()` ⇒ `Play()`，在拍子起播那一帧（**与 `DelayedVFXPlay` 并行**） | **零消费**（57 CardAnim / 92 行 / 71 卡） | 缺震屏 | ⚠ 判据不足（要先把 preset GUID → amplitude/direction 导进运行期表） |
| 25 | `UnitShake`（animator clip infos） | `_ResolveAnim:106-124` | 未接 | 本 build `minionAnimationClipInfo.enable` 全 0 ⇒ 原版也不进这一支 | ✅（判据为空） |

### 1·4 阵亡 / 残骸

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 26 | 抖动 → 0.5 → 爆散 → 0.2 → 墓地 | 抖 0.2/0.5 不等；`WaitForSeconds(0.5)` → 爆散体 → `WaitForSeconds(0.2)` → `GoToCemetery`（`_UnitDeath_d__446:100-379`） | `CardFeel.DeathHoldBeforeFx=0.5` · `DeathFxHold=0.2` · `DeathTotal=0.7` | 无 | ✅ |
| 27 | 这一拍占多久（串行） | `_ResolveMinionDeath_d__454:113-121` 对 `UnitDeath` 是 `yield return` ⇒ 等整条 0.7；多个阵亡串行 | `DurationOf(Death) = CardFeel.DeathTotal(false)`（`EventTiming.cs:436` 附近） | 无（2026-10-23 由 0.2 改 0.7） | ✅ |
| 28 | `Hit`→`Death` 之间那一拍 | 原版没有额外等待 | `DeathHold = 0.1f`（`:776`）**我们挑的** | +0.1 s | ⚠ 已知（`新账 N7`，已在 `IsSourced` 里如实报 false） |
| 29 | 阵亡连带能力动画（7 行 / 6 卡） | `ThisUnitDeath(80)`/`UnitDeathByAbility(180)` → `PlayAnim` | `PlayDeathAbilityAnims`（`BattleDriver.cs:21453` 一带） | 无 | ✅ |
| 30 | 墓地生长 | `growInCemeteryTime = 0.3`（`SetEase(9)` OutCubic），被紧跟的 `SetActive(false)` 盖掉 | `CardFeel.CemeteryGrowTime=0.3` | 无 | ✅ |

### 1·5 登场 / 部署

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 31 | 飞行段 / 交接 / 悬停落下 | 0.3 / 0.1 / 0.05 / 0.2（`MinionManager__MoveMinionToConversionPoint:41-99` · `_MinionPlayedIntoField_d__314:60-121`） | `DeploySequence.MoveTime=0.3` / `TimeToScaleIntoMoving=0.1` / `TotalTime=0.65` | 无 | ✅ |
| 32 | 登场 clip 起播 / 全长 / 材质复位 | 飞行结束（0.3）起播；clip 0.9167；`DelayedResetMaterial(0.55)` | `ScheduleAction(MoveTime, StartHandToBoardClip)`；`HandToBoardEnd=0.9167`；`HandToBoardDone=0.55` | 无 | ✅ |
| 33 | `Deploy` 按来源分档 | 手牌 **0.4**（0.1+0.3）/ 自动打出 **0.1** / 效果召唤·复活 **1.0**（`Summon Troop Tween` DelayTween） | `DeployHoldOf`（`:759`）· `DeployLandHold=0.3` · `DeploySummonHold=1.0` | 无 | ✅ |
| 34 | `Summon Troop Tween` 的 ResetBody 0.3 | `waitAnimation=0` ⇒ 不阻塞 | 未计 | 无 | ✅ |
| 35 | 效果召唤「居中展示」两拍 | 我方 0.12+0.12=**0.24** / 敌方 0.12+0.1+0.12=**0.34**（`CardScript__ShowCardInCenter.c`） | `PlayAutoPlayCenterDisplay`（2026-10-24 A19 已订正两队互换） | 无 | ✅ |
| 36 | 飞行单位「落地→演→飞回」 | `ToggleFlying(false, 0.15)` → 演 → `ToggleFlying(true, 0.45)` | `Card3DAnimationController.ToggleOffFlyingTime=0.15` / `ToggleOnFlyingTime=0.45`（`Battle/Card3DAnimationController.cs:111,115`） | 无 | ✅ |
| 37 | 状态驱动的飞行过渡 | `BattleCardUI.ToggleFlying` 传 `DAT_1834b3158` = **0.4** | `FlyingCardController.DefaultTransitionTime=0.4`（`Battle/FlyingCardController.cs:78`） | 无 | ✅ |
| 38 | 飞行曲线 / 速度 / 往复 | `initialHeightOffset 0.4` · `flyingHeight 0.1` · `animationSpeed 0.35` · 4 键曲线 · `postWrap Once`（≈2.86 s 后停） | `FlyingCardController` 文件头逐字段（同件实读） | 无 | ✅ |

### 1·6 回手 / 换位 / 扇形

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 39 | **回手这一拍占多久** | `_ResolveRecallToHand:273-278` `yield TransformUnitInPlayToCard`（`_d__256:80-85` 等 `GetHandToBoardAnimEventTime()` = clip 唯一事件 0.55）+ `:55-60` `yield AddCardNotDrawnToHand`（`_d__40:42-47` 等 `timeToPositionCard` 0.2）⇒ **0.75**；带 `teleport`(0x4ce=1230) **+0.55 ⇒ 1.30** | 改前登记成**零时长**（`EventTiming.ZeroDurationKinds`） | 后一条事件/下一批早 0.75 s | 🔴 **已修** |
| 40 | 回手表现（倒放 clip） | `PlayBackToHandAnimation`：`Rewind`/`speed=−1`/`time=length`/`Play`；`DOMove(+up×scale.x×3.0, 0.208)` **线性** | `CardFeel.ReturnToHand` 0.9167 反向时间轴；`ReturnLiftTime=0.208` · `ReturnLiftScale=3.0`（`CardFeel.cs:1865-1870`） | 无 | ✅ |
| 41 | 回牌库（dest=deck/decktop） | 同 0.55 + `MoveToDeck(…,1,0)` = **0.3** ⇒ **0.85**（teleport 1.40） | 事件**没有「回手/回牌库」那一位** ⇒ 按 0.75 排 | 早 0.1 s | ⚠ 判据不足（要 `RuleEngine` 给事件补一位，不在白名单） |
| 42 | 换位/补位时长 | `minionReassembleTime(0.15) ÷ 1.5` = **0.1**（`MinionManager__ReassembleMinions.c:47`） | `CardFeel.ReassembleTime=0.1`（`:3215`） | 无（缓动是我们推的：原版没 `SetEase`） | ✅ |
| 43 | 状态切换补间（大图/扇形那档） | `DoChange`：`+0x24 = 0.3`，`SetEase(0x1b)` = **OutBack** | `CardView.BigStateTweenDuration=0.3` / `BigStateTweenEase=OutBack`（`Core/CardView.cs:10961-10965`，⚠ 不在本片白名单） | 无 | ✅ |
| 44 | 扇形排布的补间 | 见 `Core/CardFan.cs`（`CardTween.Use(..., OutQuad, v)`） | 同 | 无（**本片白名单不含 `CardFan.cs`**，只记不审） | ✅（旁证） |

### 1·7 发牌 / 换牌 / 抽卡

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 45 | 抽卡时长（分敌我） | `VarsGlobal.TimeToDrawCard`：我方 0.3 / 敌方 0.15（`_AddDrawnCardToHand_d__37:78-80`） | `CardFeel.DealDuration/DealDurationFoe`；`DrawDealOf(e.Player)` 按座位分档（`EventTiming.cs:481` 一带） | 无 | ✅ |
| 46 | 换牌逐张错开 | 每放好一张 `WaitForSeconds(timeToDraw × 0.5)`（`_AddCardsToMulligan_d__41:119-127`，`DAT_1834b2bb4`=0.5）⇒ 我方 0.15/张 · 敌方 0.075/张 | `BattleDriver.DealStaggerFrac=0.5` + `DealStaggerDelay`（`:24776-24792`） | 无 | ✅ |
| 47 | 发牌翻面 | 每段 `drawSeconds × 0.25`（`TurnCardAround.c:32`）· 90°（`DAT_1834b2e04`） | `CardFeel.TurnCardAroundFrac=0.25` · `TurnCardAroundDeg=90` | 无 | ✅ |
| 48 | 打字机节拍 | `GameStaticData.timeBetweenCharsTypewriter = 0.01`（`_AnimateText_d__22:23-65`，先等一拍再显第 1 字） | `UnitChatPanel.TypewriterCharSeconds=0.01f`（`Battle/UnitChatPanel.cs:1334`，2026-10-24 `E17` 已改） | **无**（本片简报里那条「0.03 ⇒ 要做」**已过期**，见 §五·1） | ✅ |

### 1·8 触发 / 治疗 / 反击 / 回合

| # | 项 | 原版值 + 出处 | 我们值 + 位置 | 差异 | 判定 |
|---|---|---|---|---|---|
| 49 | `Trigger` 间隔 / 时长 | `sec3FractionDelay=0.3` / `twoFractDelay=0.2`（`_ResolveTriggerTraitAnim:11-14`） | `TriggerHold=0.3` · `TriggerLen=0.2`（`:784,793`） | 无 | ✅ |
| 50 | 治疗 | `_Heal_d__468:204` `WaitForSeconds(1.5)`，`ResolveHeal` 每目标 `yield` ⇒ 串行 | `DurationOf(Heal) = CardView.HealTextHoldSeconds`（1.5，`:525`） | 无 | ✅ |
| 51 | 反击 | `_ResolveAttack:1119-1131` 远程守方播 `ResolveAttackRangedAnim` 并等它跑完才落伤害；近战守方不播 | `RetaliationHoldOf`（`:97-112`）`= 0.2 + 守方那件链时长`，近战 0 | 无 | ✅ |
| 52 | 伏击翻面 | `SetAmbush(card,false)` 普通方法、不阻塞 | `AmbushExit = 0` | 无 | ✅ |
| 53 | 收集灵魂石 | `CollectWaystoneEffect` 不 `yield` | `CollectWaystone = 0` | 无 | ✅ |
| 54 | 关键词施加 | `DisplayStatusAnim.c:78-119` `StartCoroutine(Start(0))` 不 `yield` | `StatusApplied = 0`（2026-10-24 登记） | 无 | ✅ |
| 55 | 消耗型（ephemeral）移出 | 见 §三·4（未查清） | `Return(Slot<0)` 仍 0 | 未查 | ⚠ 判据不足 |
| 56 | 回合抬头后的停顿 | `_NextTurn_d__395:483-491`：`ShowHeadsUpMessage(YourTurn,1,0)` → `WaitForSeconds(DAT_1834b3090 = 1.5)` | 只弹抬头、**不搬那 1.5 s 停顿**（`BattleDriver.ShowYourTurnHeadsUp` `:13152` 的 doc 写明了原因） | 原版每回合多 1.5 s 排队 | 📌 需主对话（见 §三·6） |
| 57 | `EvtKind.Play` 本身 | 手牌出单位：`_ResolvePlayCardFromHand` 整条链（含 0.3+0.1+0.3，见 §三·1）；居中展示那支**有条件**（`PlayerAction+0x4c`） | `Play = 0`（`ZeroDurationKinds`） | 见 §三·1 / §三·2 | ⚠ 判据不足 |

---

## 二 已修清单（3 项，全部 typecheck 0/0）

### 修 1 · 命中帧同帧次序（`新账 N5`）

| 项 | 内容 |
|---|---|
| **原版** | `CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:16-27`：先 `ResolveAttackAnimationEffects(攻方, 目标, pos)`、**之后**才 `ReceiveAttackAnim(受方)`；`CardScript__ResolveAttackAnimationEffects.c:51-107` 内部顺序 = `SetDirection` → `SetAmplitude` → **`Play()` 震屏** → `SoundManager.Play2D(命中音)` → `Instantiate(命中粒子)` + `set_localScale`。⇒ **震屏 → 音 → 粒子 → 后坐** |
| **我们（改前）** | 粒子 → 音 → 后坐 → 震屏；且 `PlayHitFrameFeel` 开头 `if (target == null) return;` ⇒ **受击视图不在时连震屏也不播**（而原版那条震屏由**攻方**的 `CardScript` 触发，与受击视图无关） |
| **改动** | `Battle/BattleDriver.cs:20146-20160`（命中帧那一拍重排序 + 注释）· `:20538-20566`（`PlayHitFrameFeel` 拆成 `PlayHitFrameShake()` / `PlayHitFrameRecoil(e, target, hitAmount)`，震屏不再挂 `target==null`） |
| **影响** | 同帧内操作次序（肉眼基本无差）＋ **受击视图缺失时不再丢震屏**（行为变化，正向） |
| **需要补的断言** | `BattleScene.cs` 里目前只有一条「后坐方向」断言（`:8065-8070`，注释里还写着 `PlayHitFrameFeel` 这个名字 ⇒ 顺手订正）。建议加两条：①受击视图置空时 `CardFeel` 的震屏计数 **+1**（改前 = 0）；②同帧调用序列用记录钩子断「shake 早于 particle」（⛔ 那条宿主不在本片白名单） |

### 修 2 · `AnimInfo.startDelay` 零消费（`新账 N4`）

| 项 | 内容 |
|---|---|
| **原版** | `BattleManager._ResolveAnim_d__466__MoveNext.c:67-75`：state 0 末尾 `WaitForSeconds(*(float*)(AnimInfo + 0x28))` → state 1 才 `DelayedVFXPlay`（后者再等 `vfxDelayTime`）。`0x28 = startDelay`（`AnimInfo` 字段序 `0x24 vfxDelayTime / 0x28 startDelay / 0x2c timeAtStartPos / 0x30 timeMoving / 0x34 timeAtEndPos`，与 `AnimInfo__GetAnimDuration.c` 读的四格逐项对齐）。**不计入** `GetAnimDuration()`。 |
| **数据** | `Resources/cardanim_clips.json` 的 `sd` 列（生成器只在非 0 时写）：全池 **1 行** = `Cuts Brutal Warp Spider` **0.5 s**（`Empyric Ambush SaimHann` 的 `play` 桶） |
| **改动** | `Battle/CardAnimClips.cs:46-68`（`CardAnimPlacement.StartDelay` + 出处）· `:139-143`（`RtRow.sd`）· `:201`（装载）· `:320-333`（`Resolve`：`T = it.T + max(0, StartDelay)`）· `:119-121`（`CardAnimClip.T` 的 doc）· `Battle/BattleDriver.cs:21398-21412`（`PlayPlacedPieces`：`at = it.T + StartDelay`，`<= eps` 才当场播） |
| **影响** | 那**一件**起播晚 0.5 s（= 原版）；其余 1,066 行 `sd=0` ⇒ 零变化。**不影响**这一拍占多久（`GetAnimDuration` 不含 `startDelay`） |
| **需要补的断言** | `CardAnimClips.PlacementOf("Cuts Brutal Warp Spider").Value.StartDelay == 0.5`；以及 `CardAnimClips.Resolve(...)` 返回的 `T` 比 `VfxMap.ResolvedVfx.T` 大 0.5（同一件）；再一条「99% 的行 `StartDelay == 0`」（防把别的字段误读成 `sd`） |

### 修 3 · 回手这一拍占多久（改前零时长）

| 项 | 内容 |
|---|---|
| **原版** | `BattleManager._ResolveRecallToHand_d__542__MoveNext.c:273-278`：`yield return StartCoroutine(CardScript.TransformUnitInPlayToCard)`；那条 `CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:80-85` 在 state 0 末尾 `WaitForSeconds(GetHandToBoardAnimEventTime())`；`GetHandToBoardAnimEventTime` = clip 的**第一条 AnimationEvent** 的时刻，而 clip `AnimationClip_-3614292623624764332.json`（`Card Hand To Board`）`m_Events` **只有一条** `CardHandToBoardAnimationFinished @ 0.55` ⇒ **0.55 s**。之后 `_ResolveRecallToHand:55-60` 又 `yield return AddCardNotDrawnToHand`，那条 `PlayerHand._AddCardNotDrawnToHandAtIndex_d__40__MoveNext.c:42-47` 等 `VarsGlobal.timeToPositionCard` = **0.2 s**。⇒ **0.75 s**（`DOMove 0.208` 与 0.55 **并行**，不另加） |
| **分支（铁律 5·c）** | ①带 `teleport`（`HasDefaultTrait(0x4ce)` = 1230，`trait_collection.json`）⇒ state 1 再等一次 0.55 ⇒ **1.30 s**（`_d__256:108-115`）；②回**牌库**（dest=deck/decktop）收尾是 `MoveToDeck(…,1,0)` = 0.3 ⇒ **0.85 s**（`_ResolveRecallToDeck:350-356`）——⚠ 事件里没有「回手/回牌库」那一位 ⇒ 这一档按 0.75 排（早 0.1 s） |
| **改动** | `Core/EventTiming.cs:110-215`（`HasTraitOf` 钩子 + `ReturnHoldOf` + `ReturnCardAnimHold = CardFeel.HandToBoardDone`（= 0.55，⛔ 没另写一个字面量）+ `ReturnHandRelayoutHold = CardTween.RelayoutDuration`（0.2））；`:343-352`（`TimedKinds` 加 `Return`、从 `ZeroDurationKinds` 搬走并留痕）；`:520-528`（`DurationOf(EvtKind.Return)` = 0.75 兜底）；`:563`（`DurationOf(BattleEvent)` 分档）；`:1005-1024`（`SourceOf`）；`:94`（钩子声明）；`Battle/BattleDriver.cs:3541-3552`（`HookEventTimingCardSource` 挂 `HasTraitOf`，读 `CardDef.Has` = **基础词条**，与原版 `HasDefaultTrait` 同义） |
| **影响** | 回手之后的事件延迟 0.75 s（teleport 卡 1.30）；`Slot<0`（墓地→手牌/牌库、ephemeral）**照旧 0** |
| **需要补的断言** | ①`ReturnHoldOf` 三档真值表：`Slot=-1 ⇒ 0` · `Slot>=0 & 无 teleport ⇒ 0.75` · `Slot>=0 & teleport ⇒ 1.30`；②`DurationOf(EvtKind.Return) == 0.75` 且 **≠ 0**（🧨 写回 `ZeroDurationKinds` ⇒ 红）；③结构反例：`EventTiming.IsSourced(Return) == true` 且 `IsTimedKind(Return) == true`；④驱动级：一条 `Return` 之后紧跟一条 `Draw`，`Draw` 的起播时刻 ≥ Return 的 `at + 0.75`（🧨 退回 0 ⇒ 红） |

---

## 三 判据不足 / 需主对话（⛔ 没猜、没改）

### 3·1 🔴 `play` 桶相对 `Deploy` 的先后 —— **疑真缺陷，但「晚多久」有三种说法**

- **事实**：原版 `ThisCardPlayed`（`trigger = 0`）是由 `BattleManagerSupport.BroadcastCardPlayed` 触发的，调用点在
  `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1426`，而那条协程里 **0.1 / 0.3 / 0.6 之类的前置等待都在更早的 state**
  （state 4 = `MinionPlayedIntoField` `:659-665`，state 5 = `WaitForSeconds(0.3)` `:673-676`，之后 8/9/10/0xb/0xc/0xd **再无等待**）
  ⇒ **units 的 `play` 桶动画应该在「登场链跑完」之后**。
- **我们**：`EvtKind.Play` 与 `EvtKind.Deploy` 在时间线上**同刻**（`DelayBetween(Play→Deploy)=0`，`DurationOf(Play)=0`），
  `play` 桶由 `PlaySignal` 当场按 `it.T` 排 ⇒ **最多早一整条登场链**。
- **为什么没改**：「登场链到底多久」现有两份记录互相打架 ——
  `EventTiming.DeployHoldOf(HandPlayed)` = **0.4**（0.1 + 0.3，`W61b` 那次审计的结论），
  而同一批审计的 §2·4 又把 `_MinionPlayedIntoField` 的 0.3 飞行 + 0.1 交接 + 0.05 悬停 + 0.2 落下 = **0.65** 全标 ✅，
  两者相加是 **1.05**。⇒ 偏移可能是 0.4 / 0.7 / 1.05 三个数，**不猜**。
- **还差**：把 `_MinionPlayedIntoField` 那条 state 机逐 state 对齐一遍（哪个 state 被 `yield`、飞行是不是在 state 之内），
  得出「`Deploy` 事件在时间线上应该占多久」＋「`BroadcastCardPlayed` 相对它晚多少」，两个数一起定。
  落点：`EventTiming.DeployHoldOf` / `PlaySignal` 的 Play 支（都在本片白名单内，判据一闭合就能改）。

### 3·2 ⚠ `EvtKind.Play` 的「居中展示」那一段是否进时间线

- `_ResolvePlayCardFromHand:234-265`：只有 `PlayerAction + 0x4c != 0`（或 `matchType == 0xa0`）才
  `StartCoroutine(PlayEnemyCard/PlayPlayerCard)` 并 `yield` —— 那是 `DisplayEnemyCardInCenter` 那一套
  （我方 0.12+0.12 = **0.24** / 敌方 0.12+0.1+0.12 = **0.34**，`A19` 已把数值订正过）。
- **还差**：`+0x4c` 的语义（=「这张牌要不要居中展示」？）没查 ⇒ 不知道哪些卡/哪些局要加这 0.24–0.34 s。
  ⇒ `EvtKind.Play` 的时长今天可能对一部分卡**偏短**。⛔ 没按「所有卡都加」硬套。

### 3·3 ⚠ 回手 vs 回牌库（差 0.1 s）

见 §二·修 3 的「分支②」。**还差**：`RuleEngine` 给 `EvtKind.Return` 补一位目的地区分（`op.Dest`），
本片白名单不含 `RuleEngine/**` ⇒ 只把 0.75 落到两条路上，记 0.1 s 已知偏差。

### 3·4 ⚠ `Slot < 0` 的 `Return`（墓地→手牌/牌库、`ephemeral`）仍 0

- `EffectResolver:6153`（墓地→手牌/牌库）与 `RuleCore:2701`（`effect: "ephemeral"` 移出游戏）都发 `Slot = -1`。
- **还差**：这两条在原版各走哪个协程、等多久（`AddCardNotDrawnToHand` 那条 0.2 大概也在，但没读）。
  ⇒ ⛔ 没拿 0.75 硬套（那会让「从墓地取回一张牌」凭空多 0.75 s）。

### 3·5 ⚠ 其它已知判据不足（沿用，非本轮新账）

| 项 | 还差什么 |
|---|---|
| `Hit` 的两个例外（DamageType 7/8 的 0.4 前置；AoE 的 `doItFast = extraValue`） | 实战占比没统计；`extraValue` 谁置 true 没查 |
| `SW21 Wulfen Pack Leader` 替代能力 0.2 vs 普通 0.4 | 要 `normalAbilityBoardCriteria` 进卡导出（数据侧）+ 事件带「哪一份」（`RuleEngine`） |
| 近战段2 的旋转基 | `AngleAxis(90, <12 字节>)` 里的向量没解出（要汇编级读或逐帧看） |
| 后坐的 `ToOurs`（1.686×） | 原版卡父链 `MinionManager` 的 `lossyScale` 没查 |
| `DeathHold = 0.1` | **我们挑的**（`新账 N7`）；`RangedVfxFallback 0.75` 的「无件」档没单独记账（`N8`） |
| `minionMovementInfo`（N3）/ `screenShake`（N2） | 见 §一 1·3 #23/#24：数据侧要先导，消费点也要新建（`MoveAndDisappearAnim` / 震屏 preset） |
| `HandToBoard` 反向时间轴在 0.55 s 那一刻 | 原版 0.55 就 `CardPosAndScaleToUI` + `SetStateInHand`，我们的补间跑到 0.9167 —— **观感**是否需要截断，见 §四 |

### 3·6 📌 回合抬头后的 1.5 s 停顿（判据齐，但会动整局基线）

- **原版**：`_NextTurn_d__395:483-491`：`ShowHeadsUpMessage(YourTurn, 1, 0)` 之后 `WaitForSeconds(DAT_1834b3090)`，
  `DAT_1834b3090` = **1.5**（同址在 `MinionManager.Reassemble` 当 1.5 用、`ShowCardInCenter` 当 ×1.5 用，三处互证）。
- **我们**：只弹抬头，**不搬**这 1.5 s（`BattleDriver.ShowYourTurnHeadsUp` `:13137-13159` 的 doc 写明「抽牌在 `RuleCore.BeginTurn` 已同步做完」）。
- **为什么不自己改**：它会给**每个回合**的整条时间线加 1.5 s（`BattleScene.Run` 里大量按 `Clock` 推的基线要整体后移），
  而断言宿主 `Editor/BattleScene.cs` **不在本片白名单**，本代理又**不能跑 Unity** ⇒ 改了必然红且无法重定基线。
- **建议排批**：与「`BattleScene.Run` 基线整体重定」同一批做；落点 = `ResetClock` / `ShowYourTurnHeadsUp`
  （把 1.5 s 排进「下一批事件的起点」，不是横幅时长 —— 横幅是 `TipDuration 1.7`，另一回事）。

---

## 四 只能真机看的清单

1. **回手 0.75 s 与表现补间 0.9167 s 的重叠**：改后队列在 0.75 s 就放行（原版如此），我们的卡还在倒放最后 0.17 s
   ⇒ 真机看「下一条事件起来时那张卡是否已经离开画面」；若观感打架，要动的**是表现补间的收尾**（不是时间线）。
2. **`startDelay` 那 0.5 s**：`Empyric Ambush SaimHann` 打出时，`Cuts Brutal Warp Spider` 那件应比其同伴晚半秒起 ——
   逐帧截图（≥30 fps）对一眼，静态只能证明数值接线对。
3. **命中帧同帧次序**：四个操作同帧，肉眼本来无差；真机能验的只有「受击方视图不存在时震屏还在不在」
   （`Warboss Gordrang` 那种一击致死 + 视图同步清掉的场面）—— 那一条改后**应该**有震屏。
4. **近战逐帧深度缩放 / 后坐幅度**（沿用 2026-10-23 的 P0 复核清单，本片未动）。

---

## 五 顺手发现 / 台账订正（⛔ 本代理没改正本，交主对话）

1. **🔴 本片简报里那条「打字机节拍原版 0.01 / 我们 0.03（3 倍慢）—— 已判定要做」已过期**：
   `Battle/UnitChatPanel.cs:1334` 现读 **`TypewriterCharSeconds = 0.01f`**，且 doc 里有 `E17`（2026-10-24）的就地订正
   （「0.03 是慢 3 倍的错误值，⛔ 别改回去」），「先等一拍再显第 1 字」也已按 `_AnimateText` 结构实现（`TypewriterVisibleAt = floor(t/节拍)`）。
   ⇒ **`资料/任务线/战斗线/任务总表.md` 第 130 行那条「要做」应核销**（我没权限改 `任务总表.md`）。
2. **`Editor/BattleScene.cs:8065` / `:8070` 的注释还写着 `PlayHitFrameFeel`**（那个方法已拆成 `PlayHitFrameShake` + `PlayHitFrameRecoil`）——
   行为不变，只是名字过期；那张宿主不在本片白名单。
3. **`Core/EventTiming.cs` 的 `ZeroDurationKinds` 里 `Return` 那条注释**（原文「离场位移是表现层补间，不进动作队列」）**已被本轮推翻**，
   就地改成「已搬去 `TimedKinds`」并留了原因（铁律 5）。
4. **`play` / `trigger` 桶的 `startDelay` 已接，但 `card_anim_order.json`（ability 那 99 拍）不带 `sd`**：
   现读全池只有 1 行 `sd` 且不在 ability 桶 ⇒ 今天零影响；将来若出现 `sd>0` 的 ability 拍，
   要连 `gen_card_anim_order.py` 一起补（否则静默早播）。
5. **`EvtKind.Play` 的 `DurationOf` 在「居中展示」那一档可能偏短**（§三·2）—— 与 §三·1 同族，建议一起排。

---

## 六 验证与改动面

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改后连跑 2 次；中途 2 次红全在别人的文件：`AttackSelector.cs` / `Editor/CardBaseDemo.cs`） |
| 行尾 | `BattleDriver.cs` **30346 CRLF / 30346 LF**（未翻）· `EventTiming.cs` / `CardAnimClips.cs` **纯 LF**（与改前一致） |
| `git diff --numstat` | 最终：`BattleDriver.cs 433/47` · `CardAnimClips.cs 26/2` · `EventTiming.cs 121/4`（这三个文件在我开工前**都已是 modified**，数值含前序未提交改动；**没有整篇重写**；§七 追加轮又给 `BattleDriver.cs` 加了 +17/−2 左右） |
| 未跑 | Unity 批处理 / 自检（红线）⇒ 同步点需要跑 **`BattleScene.Run`**（本片只动 `BattleDriver` + 两个 Core 文件，`RuleEngine` 一个字节没动） |
| 改动文件（全部在白名单内） | `Battle/BattleDriver.cs` · `Battle/CardAnimClips.cs` · `Core/EventTiming.cs` |

---

## 七 追加 · AU2（战斗 HUD 审查代理）移交的 4 条 —— BattleDriver 侧

> 2026-10-25 追加。AU2 的原件 → `普查产出_1025/AU2_战斗HUD与界面_审查与修复.md` §6。
> 只动 `Battle/BattleDriver.cs`（⛔ 没碰 `EndPanel` / `CardChoicePanel` / `CardDisplayWindow` / `MultiCardDisplay` / `WfSlider`）。

| # | 条目 | 现核结论 | 本次改了什么 | 还差什么 / 交给谁 |
|---|---|---|---|---|
| 1 | **`Energy Accumulation` 整组 `SetActive` 缺** | 🔴 **AU2 引的 T13 原文已过期 —— 它 2026-10-15 就做完了**：现读 `BattleDriver.cs:27334-27337`，两颗 ON quad（`EnergyAccumulation_Me/_Foe`）建完立刻 `SetActive(manaAccumulation > 0)`。判据复核过：`ManaTypeHolder__SetAccumulationMana.c:11-20` —— **组 GO（`+0x40`）与 ON 图（`+0x58`）都 `SetActive(param_2)`**，`SetupBoardPhase:179,194` 传 `0 < *(int*)(vars+0x34)`（`GameplayVariablesData.manaAccumulation`） | **零改动**（已正确） | 仍开着（**不是本轮新增**）：① `OFF` 图（`40k_battle_energy_empty`）什么时候亮 —— 判据未查，我们**只建 ON 图**；② `manaAccumulation` 是 LiveOps 下发、本地恒 0 ⇒ 产品里整组恒关（真机也看不出来） |
| 2 | **牌库 / 手牌张数「按住牌才显示」缺** | 🔴 **主体 2026-10-15（T13）已做完**（`CardHoldPeek` + `ApplyTutorialVisibility` 的 `peek` 三组 `SetVis`，`BattleDriver.cs:15160-15180`）；但**原版那个闸的头一道 `duringBattleFlag` 确实没接** —— 复核：`BattleManager__ShowDeckSize.c:22-25` 读 `BattleManager+0x25c`（`ObscuredBool`），**置真 = `_StartBattlePhase_d__334:163`**、**置假 = `DeadHero:172`**；同一个 `+0x25c` 还被 `CanShowCemetery.c:16` 读（= 我们 `CemeteryRowClickAllowed` 那一族的另一半） | 🔴 **已接等价物**：`CardHoldPeek`（`BattleDriver.cs:15133-15156`）加 `Ctx == null \|\| Ctx.IsOver \|\| InMulligan \|\| Ctx.Turn <= 0 ⇒ false`（`SetCardHoldForTest` 的早退在它**之前** ⇒ 自检那 7 条不受影响）。第 ② 条闸（`UIstate != choosingResolutionCard(14)`）**结构上已覆盖**（选牌/摊开/放大窗开着时输入先被它们吃掉） | 收起那一支的半个条件 `FUN_180617800` **仍无反编译产物** ⇒ 只做了「松手」这一半（原样留在源码注释里）。「按在哪」是等价物（原版按卡的 collider） |
| 3 | **HUD 级共享 `ShadePanel` 缺** | 🔴 **`BattleDriver.cs` 里没有自己的一份压暗** —— 现读 `grep Shade`：只有 `WaitBanner` 的 `Dark Shade`（**原版另一个节点**：3963.5×3366 / α 0.6118 / `rt=1`，AU2 已核 ✅一致）与 `LoadingMenu` 黑场。真正的整屏压暗在**四个都不在本片白名单**的文件里：`MulliganPanel.cs:775`（世界 quad，3006，α`ShadeAlpha`0.8）· `CardChoicePanel.cs:446`（同，AU2 刚改 0.8）· `BattleLogPanel.cs:826`（`HudPiece`）· `EndPanel.cs:302`（`HudPiece`，色 0.0283 / α0.7529） | **零改动（无落点）** | **不是缺陷、是「实例数」**：两边**共用同一批常量**（`CardChoicePanel.ShadeAlpha/QChoiceShade` **直接引用** `MulliganPanel.ShadeAlpha/QMulliganShade`），任一时刻只有一扇面板在画 ⇒ 行为等价。若主对话要按原版「一个 `ShadePanel` 多处 `SwitchShade`」收口，建议在 `HudCanvas`/`HudPiece` 侧加惰性单例 `SharedShade`（世界 quad，队列 3006），四个调用点改成 `SetAlpha + SetActive`；⚠️ 会牵动 `D6①–⑦` / `A1` 那批队列与 alpha 断言（四个文件一起改） |
| 4 | **`TickMultiCards` 的 Hide/ShowMyDeck 顺序** | 🔴 **已于 2026-10-21（`T09` A4 · 步骤 S1）整段删除**：现读 `TickMultiCards`（`BattleDriver.cs:13070-13096`）只有 `TickFeedback` / `TickScroll` / `UpdateHud` + `return true`，**没有任何 `Hide()`**；`Hide` 只在 `ShowMyDeck(false)`（`:12325`）与「关全部模态」（`:12608`）。AU2 引的是 `:13065` 那段**历史注释**（「这里原来还有 … 已整段删除」） | **零改动** | 原版这扇窗**唯一入口是效果 `470`**，「点我方牌堆开窗」是我们自造入口（`ShowMyDeck` 的 doc 已标）⇒ 这一格的「顺序」在原版**本来就没有对应物** |

**追加轮的验证**：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；`BattleDriver.cs` **30361 CRLF / 30361 LF**（未翻）。
**追加轮需要主对话补的断言**（宿主 `Editor/BattleScene.cs` 不在本片白名单）：
① `manaAccumulation = 0 ⇒ 两颗 `EnergyAccumulation_*` 都不在画`；`= 1 ⇒ 都在画且贴的是 `_full`（🧨 只做一半 —— 只 `SetVis` 不判判据 —— ⇒ 第二条红）；
② `duringBattleFlag` 等价物：设 `Ctx.Turn = 0`（或 `IsOver`）时按住一张牌 ⇒ `DeckSizeVisibleForTest == false`（🧨 删掉新加的那行闸 ⇒ 红）。

---

## 八 追加 · 协调台派下的 4 件（AU1-A/B/C/D）—— 全部落在 `Battle/BattleDriver.cs`

> 2026-10-25 第二轮追加。本轮**只动 `BattleDriver.cs`**；`AttackSelector.cs` / `Editor/BattleScene.cs` 归别人（⛔ 没碰）。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；`BattleDriver.cs` **30435 CRLF / 30435 LF**（未翻）；`git diff --numstat` = `516/56`。

### AU1-A · 回合抬头之后 `WaitForSeconds(1.5)` —— ✅ 已做（原 📌 那条，「照原版做」）

| 项 | 内容 |
|---|---|
| **判据** | `BattleManager._NextTurn_d__395__MoveNext.c:483-491`：`ShowHeadsUpMessage(Battle/HUD/YourTurn, 1, 0)` → `WaitForSeconds(DAT_1834b3090)`；`read_literal.py` 实读 = **1.5**（同址在 `MinionManager.ReassembleMinions` 当 ÷1.5 的除数、`ShowCardInCenter` 当 ×1.5 的倍率 ⇒ 三处互证）。它在 **`_NextTurn` 的「本地玩家」分支**（`:444` 的 `+0x28 != 0`）里；教程第 1 回合 `:451-475` 整段 `goto` 跳过 ⇒ 也不停。 |
| **🔑 顺手查实的关键次序（决定 hold 放在哪）** | 那一支的**后续**是 `state=2` → `case 2: goto LAB_1809aaee2` → `goto LAB_1809aaf0c` → **`BroadcastTurnSetup` → `TurnStartCardDraw` → … → `BroadcastTurnStarted`**（`:396-420`）⇒ **本回合的抽牌是在 1.5 s 之后才发生**。所以 hold 必须排在「回合事件之前」，不是之后。 |
| **落法（⛔ 不是 `Thread.Sleep`/`WaitForSeconds`）** | `ShowYourTurnHeadsUp()` 末尾调 `HoldForTurnHeadsUp()`：`at = _clock + 1.5` → `ExtendTimelineTail(at)`（下一批从 `max(_clock, _timelineTailAt)` 起排 = `PlaySignals` 的既定口径）＋ `BeginBlockingWindow(at)`（这 1.5 s 里 `WaitingForBlockingAction` 为真 —— 原版那一拍本就挂在那条协程上；回放局那道闸恒 false，见 `WaitingForBlockingAction` 的 `_replaySession` 短路）。两件都由**同一条时间线泵**（`AdvanceTimeline`）推 ⇒ 批处理 `Step`/`DrainQueue` 推得过去。 |
| **放在 `ResetClock` 那一支的理由** | `ShowYourTurnHeadsUp` **只由 `ResetClock`（+ 联机 `ApplyLoggedAction` 的 `EndsTurn` 支）** 调，而 `ResetClock` 的**六个生产调用点全部在 `RefreshAll()`（→ `PlaySignals`）之前**（`:5352` · `:7868` · `:16781` · `:16828` · `:16865` · 联机 `:349/:422`）⇒ 新回合的第一批事件必然落在 hold 之后。**对手回合**：`ShowYourTurnHeadsUp` 的 `Ctx.Active != _me` 早退 ⇒ 不停（与原版「只有本地玩家分支才等」一致）。 |
| **改动** | `Battle/BattleDriver.cs:13162-13171`（调用 + 判据注释）· `:13173-13211`（`HoldForTurnHeadsUp` / `TurnHeadsUpHold = 1.5f` / 自检口 `TurnHeadsUpHolds`） |
| **需要 R 改的断言** | ① **文案**：`Editor/BattleScene.cs:8135-8138` 那段（与 AU1-C 同一条，见 §AU1-C）；② **新增**：`TurnHeadsUpHolds` 逐条 —— 我方每回合 +1、对手回合不涨、教程第 1 回合 = 0；`TimelineTailAtForTest - Clock >= 1.5 - dt`（同回合首批事件起排前）；🧨 把 `HoldForTurnHeadsUp()` 那次调用删掉 ⇒ 前两条红 |

**🔴 AU1-A 会让「回合切换之后按固定 `Step(秒)` 推表」的断言整体后移 1.5 s —— 清单与扫法：**

- **自动吸收、不用改的两类**：`DrainQueue(...)`（推到队尾，`BattleScene.cs:44673`）· `while (WaitingForBlockingAction …) Step(...)`（我加了 blocking ⇒ 这些循环自己会多推 90 帧）。
- **不受影响的方向**：`SimulateEndTurn()` 之后（= **交给对手**，对手回合不停）。
- **受影响 = 凡是「进入我方回合」之后再固定 `Step`**：
  · `Begin(...)` / `BeginFromDeckLibrary()` / `SimulateMulliganDone()` 之后的固定 `Step`（第 1 回合也是我方）—— 现扫 **55 处**（`Step(0.3f)` 为主，另有 `:23678/:23723` 的 `Step(0.625f)`、`:26568` 的 `Step(0.05f)`、`:40493/:44294`…）；
  · `SimulateAiTurn()` 收尾回到我方之后的固定 `Step`（`:34723` / `:34737`）；
  · 上界的绝对值断言 **`BattleScene.cs:13818`**（`TimelineTailAtForTest <= b30TailBound`）—— 若那一窗跨了回合起点要 +1.5。
  · **扫法**（可复跑，纯读）：
    `python -I -X utf8 -c "import io,re;s=io.open('d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs',encoding='utf-8').read().split(chr(10));[print(i+1,l.strip()[:60]) for i,l in enumerate(s) if re.search(r'\.Begin\(|BeginFromDeckLibrary|BeginBattleAfterSetup|SimulateMulliganDone|SimulateAiTurn',l)]"`
- **最省事的统一改法**：受影响的格子把 `Step(0.3f)` 换成 `Step(BattleDriver.TurnHeadsUpHold + 0.3f)`（或直接 `DrainQueue`），⛔ 别去改 `HoldForTurnHeadsUp`（那是判据）。

### AU1-B · `ShowJustOne(...)` 补传 `army` —— ✅ 已做

| 项 | 内容 |
|---|---|
| **判据** | 原版 `UnitOnBoardAttackTypeSelector__Toggle.c:74-80`：只显一颗（`ToggleJustOne`）时技能钮的图标走 **`ArmyUtilities.GetActiveAbilityIconForArmy(card.rawCard.army(+0x2c))`** → `IconsByArmySO.activeAbilityIcons`（**只有 5 条**）；查不到（阵营不在那 5 条）⇒ `iconSprite == null` ⇒ 整颗钮关掉。 |
| **改动** | `BattleDriver.cs:13710-13715`：第 4 个实参 = `u.Card != null ? ArmyIcons.Id(u.Card.Faction) : 0`（与正常那一路 `Option{ Army = … }`（`:13692`）**同一个表达式**）。 |
| **影响面** | **只有一支**：`displaySummonSickness && skill && (ferocity \|\| oath)` 那个「只显技能一颗」的入口（`W10` 那条「`ShowJustOne` 没传 `Army`」⇒ 技能钮是占位图）。正常三选一那一路**本来就传了**，一个字不变。 |
| **仍留的残差（如实）** | `ArmyIcons.Id` 对**不在 5 条里的阵营**返回 **-1**（`Core/ArmyIcons.cs:124`）⇒ `ApplyOptionIcons` 的 `o.Army > 0` 不成立 ⇒ 走**占位图**；而原版那一档是**整颗钮关掉**（`Hidden/Enabled=false`，`AttackSelector.cs:1259-1265` 那条只在 `Army > 0` 时生效）。要补齐 = 让 `ApplyOptionIcons` 对 `Army < 0` 也进「查不到 ⇒ 关钮」那一支（`AttackSelector.cs` 不在本片白名单）。 |
| **需要 R 的断言** | 造一个召唤病 + ferocity 的单位（如 SpaceWolves 的带 ferocity 兵）⇒ `ShowJustOne` 后技能钮的贴图 = `ArmyIcons.ActiveAbilityIconName(ArmyIcons.Id(阵营))`（🧨 不传 army ⇒ 占位图 ⇒ 红） |

### AU1-C · 弹出（drag→selector）阈值改 `PopDragThresholdWorld` —— ✅ 已做

| 项 | 内容 |
|---|---|
| **判据** | 原版真正的「拖拽开始」门是 **uGUI `EventSystem.pixelDragThreshold`**（Unity 默认 **10 px**，`IDragHandler` 要过它才被派）；`CardScript__TryDraggingFromBoard.c` 里那个 `DAT_1834b2bb0 = 0.2` 是**屏幕 px 的平方**去抖死区（≈0.45 px）。而 `DragThreshold01`(0.085) 是**三钮入场动画的进度分母**（`accumulatedDragForMinDistance`）—— 两件事，混用 ⇒ 要拖 **92 px**（≈9 倍）才弹。 |
| **改动** | `BattleDriver.cs:13498-13516`（`BoardPress` 的 `dragged` 判据改 `AttackSelector.PopDragThresholdWorld` = 10 px / 108 ≈ **0.0926 世界单位**，原注释保留为判据说明）；`BattleDriver.cs:30141`（自检口 `SimulateDragUnit` 的「拖够」距离改用同一常量 ×2 ⇒ 仍是「够远」，与真门同源） |
| **影响面** | 「轻点开大卡窗 / 拖够弹三选一」这条分流（`BoardPress`）—— 现在**拖出 10 px** 就算拖拽（原版口径）。⇒ 轻点要求更稳（手抖 >10 px 会变成拖拽，这正是原版 uGUI 的行为）。⛔ **入场动画那一侧没动**（`DragThreshold01` 照旧是 0.085 的分母）。 |
| **需要 R 改的文案/断言** | `Editor/BattleScene.cs:8135-8138` 那条现在断的是 `DragThreshold01 == 0.085` 并**打印**「真正的弹出阈值常量没定死」。⇒ 改成：保留 `DragThreshold01 == 0.085`（分母）+ **新增** `AttackSelector.PopDragThresholdPx == 10f` / `PopDragThresholdWorld ≈ 0.0926`，文案改成「弹出阈值 = `EventSystem.pixelDragThreshold` 默认 10 px（≈0.0926 世界单位）；0.085 只是入场动画的进度分母」。🧨 把 `BoardPress` 改回 `DragThresholdWorld` ⇒ 新的那条红。 |

### AU1-D · `LastAttackType` 在**选定时**写 —— ✅ 已做

| 项 | 内容 |
|---|---|
| **判据** | 原版 `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` → `CardScript.ChangeAttackType`：**点定打法那一下**就写 `card.attackType(+0x20)` 并高亮那一圈（`HighlightSelectedAttackTypeButtons`）。我们这边「选定」= `CommitCommand`（`AttackSelector.SetSelected` 原来 **0 调用点**）。 |
| **改动** | `BattleDriver.cs:13912-13932`：`_command = kind` 之后 —— ①把档位算式提成 `pickedType`（三分支，只此一处）；②`Ctx.Players[_me].Board[_selectedSlot].LastAttackType = pickedType`（带四处边界守卫）；③`selector.SetSelected(kind)` 再 `Hide()`。下面 `NoteAttackTypeSound` 那一块**改用 `pickedType`**（原来是第二份同形三元，已删）。 |
| **保住不变的部分** | 结算路径（`RuleCore.cs:5834/10157/10345/10556`）照旧写**同一个字段的同一个值** ⇒ 结算后的读数一个字不变（既有断言不受影响）；**声音那道闸**用的是 `NoteAttackTypeSound` 自己的记忆表（`_attackTypeSoundAt`），与这一格无关 ⇒ 不会多响/少响。 |
| **影响面** | 同一回合内「改选打法 → 再开选择器」时，黄圈/1.3 倍高亮的是**这一次选的**（原来恒为上次真打出去的那一档）；`RuleCore` 里 `LastAttackType` 作为「上一次打法」的读值在**选中后、结算前**会提前变 —— 这正是原版语义。 |
| **需要 R 的断言** | 同一回合：`SimulateOpenCommand` → `SimulateCommand(Melee)` → `SimulateDeselect` → 再 `SimulateOpenCommand` ⇒ 期望 `u.LastAttackType == AttackTypeMelee`（选中即写）且 `Selector` 的高亮格 = 近战；🧨 删掉新加那三行 ⇒ 第一次仍是旧值 ⇒ 红。另：`AU3` 台账那条「`SetSelected` 0 调用点」可以核销。 |

### 顺手发现（本轮）

1. **`_NextTurn` 里还有一个能量累积的消费点没接**：`BattleManager._NextTurn_d__395:287-289` —— `turnCounter > 1` 时 `BattleManager__UpdateManaLeftAccumulation(isLocal)`（同一个方法族就是 `ManaTypeHolder.SetAccumulationMana` 的上游）。我们只在建 HUD 时按 `manaAccumulation` 判一次（`BattleDriver.cs:27334-27337`）。⚠️ **本地恒 0**（LiveOps 下发）⇒ 今天零可见差；要接的话判据已齐（回合 > 1 时按「剩余 mana」更新那组显隐）。归 HUD 那一批，不在本轮。
2. `AttackSelector.PopDragThresholdPx = 10` 的**序列化真值**仍是「Unity 默认」（原版 `EventSystem` 的 `m_DragThreshold` 在 `level0`/`battlearena1`/`menus` 三包 0 命中）⇒ 真机手感若觉得太敏感，先回来看这一格（如实记，不是缺陷）。

---

## 九 追加 · 第三批（判据代理 `J1` 现查的 9 条；`J1` 全文 → `资料/普查产出_1025/J1_判据_战斗时间线与HUD缺口.md`）

> 2026-10-25 第三轮。只动 `BattleDriver.cs` / `Core/EventTiming.cs` / `Core/CardAnimClips.cs` / `Core/AbilityAnimDurations.cs` / `Core/CardFeel.cs`
> （⚠️ `Core/AbilityAnimDurations.cs` 与 `Core/CardFeel.cs` 在本片白名单里；`Editor/BattleScene.cs` 一个字没碰）。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；`BattleDriver.cs` CRLF 见 §十。

| # | 条目 | 原版（`J1` 判据） | 本次改动 | 状态 |
|---|---|---|---|---|
| 1 | **回牌库时长** | `MoveToDeck` 返回 **0.4**（`timeToScaleIntoMoving 0.1` + 0.3）⇒ 回牌库 = 0.55+0.4 = **0.95**（卡背朝上 **0.65**） | `EventTiming.ReturnHoldOf` **改成按目的地分档**：新增 <br>· 常量 `MoveToDeckTotal = CardTween.PickUpDuration + 0.3f`（= 0.4，带 doc）<br>· 钩子 `ReturnDestOf`（`BattleDriver` 按 `InstanceId` 在 `Hand` / `Deck` 里反查 ⇒ 0 手 / 1 库 / -1 判不出）<br>· 判不出 ⇒ 按回手 0.75 并**出声一次** | ✅ 已改（**卡背朝上 0.65 未接**：要 `ShouldShowCardback` / `card+0x54` 的语义，`+0x54` 连字段名都没定 ⇒ 今天按 0.95，晚 0.3） |
| 2 | **`minionMovementInfo` 改这一拍时长** | `if/else`：走它 ⇒ 时长 = `delay + animTime`（2 行、`animTime 0.6`） | 消费端**全部接好**：`CardAnimPlacement.MoveBody/MoveBodyDelay/MoveBodyTime/MoveBodyEndVis/MoveUseXZ/MoveUseY/MoveCurveX-Y-Z/MoveOffX-Y-Z` + `RtRow` 的 `mvt/mvd/mva/mvvis/mvxz/mvy/mvcx/mvcy/mvcz/mox/moy/moz`；<br>· 时长：`BattleDriver.SkillBeatDuration`（技能两桶）+ `PlayPlacedPieces` 的 `beatDur`（play/trigger/death 三桶）<br>· 收尾：`PlayMinionMovement`（`visibilityStateAfterFinish == 0` ⇒ 到点 `SetBody3DVisible(false)`，走 `CardTween.Mode` 的 `DOVirtual.DelayedCall`）<br>· **出声一次**（曲线位移本身**未建**，见下） | ⏳ **代码就绪 / 数据未到**：运行期 `Resources/cardanim_clips.json` **没有这 12 列**（`工具/gen_cardanim_clips.py` 要加，不在本片白名单）⇒ 今天零触发、零行为变化。**曲线位移的插值算式**只读到 `_d__358:77-120`（起点/终点/margin），插值那半没读全 ⇒ ⛔ 没猜（`PlayMinionMovement` 里 `LogWarning` 明写） |
| 3 | **`Hit` 的易伤例外** | 类型 7/8 ⇒ 先 0.4（`:291-303`）**再**走 1.0 ⇒ **1.4**；`doItFast` 本 build 6 个调用点全 false ⇒ 不可达 | `EventTiming.HitHoldOf`（1.0 / 1.4）+ 常量 `HitVulnerablePreWait = 0.4f` + 钩子 `HitIsVulnerable`（`BattleDriver` 答：受击方 `RuleCore.VulnerableRaw(u) > 0`）；`DurationOf(BattleEvent)` 加 `Hit` 分派。<br>`SourceOf(Hit)` 两个例外**按 `J1` 就地订正**（原文「那条路没有这道门」= 错） | ✅ 已改。⚠️ 残差如实记：原版对易伤目标有**两条** `_ReceiveDamage`（1.0 + 1.4 = 2.4），我们只有一条 ⇒ 取 1.4 **仍短 1.0**（主那一发没建模） |
| 4 | **`SW21` 替代能力 0.2** | `hasAlternativeAbility && !MeetsCriteria(normalAbilityBoardCriteria)` ⇒ 替代支；SW21 普通 0.4 / 替代 0.2 | `AbilityAnimEntry.alt`（可选列）+ `AbilityAnimDurations.TryGetAlternative` + `EventTiming.AbilityHoldOf` + 钩子 `AbilityUsesAlternative`（转发引擎现成的 `CardDef.UsesAlternativeAbility(ctx, owner, actor)`，`null` ⇒ 普通支） | ⏳ **分支已接 / 数据未到**：运行期 `Resources/ability_anim_durations.json` 的 items 只有 `{id,n,name,s}`、**没有 `alt`**（全量件的 `alternative.seconds` 已就位）⇒ 今天两支都按 0.4。要真生效需 `工具/gen_ability_anim_durations.py` 加一行（不在白名单） |
| 5 | **近战段2 旋转基 = 世界基** | `AngleAxis(25°, AngleAxis(90°, Vector3.up) × normalize(目标位置 − 自己位置))`（`_d__357:216-257`；那「12 字节」= `Vector3.up`） | `CardFeel.MeleeAttack` 的 `toTarget` **整段重写**（不乘 `homeRot`、不乘目标旋转；`endPos − tr.position` 与原文「目标位置 − 自己位置」同向，因为落点在同一射线上；两点重合 ⇒ 保持原朝向，避免 `normalized(0)` 的 NaN） | ✅ 已改 |
| 6 | **后坐 `ToOurs` 保留** | 13 场 `MinionArea` `lossyScale` 全 (1,1,1) ⇒ 1.6865 只是像素密度换算 | **零代码改动**；把 `CardFeel.UnitsToOurs` 的 doc 写成「判据已闭合、⛔ 别删」并附 `J1` §8 的实证 | ✅ 已核（无改动） |
| 7 | **`screenShake` 接消费** | `_ResolveAnim:79-105`：拍子起播**同帧**、门 = `preset != null`（36 条） | `CardAnimPlacement.ShakePreset/ShakeDelay/ShakeAmplitude/ShakeFrequency/ShakeOverAmplitude/ShakeOverFrequency` + `RtRow` 的 `shk/shkd/shka/shkf/shkoa/shkof`；消费点 `BattleDriver.PlayBeatShake`（走现成 `WFModuleScreenShake.Resolve` + `OnShake`，⛔ 没另写震屏），在两个拍子入口（`PlayPlacedOne` / `PlayOneUnitAnim`）各调一次 | ⏳ **代码就绪 / 数据未到**：数据要的是 **preset 名**，而 SO 里存 PathID（10 个 preset / 36 行）⇒ 生成器要做 PathID→名字那一跳（`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/<名字>_<pathID>.json`）⇒ 今天零触发 |
| 8 | **`play` 桶相对 `Deploy` = 1.30** | `BroadcastCardPlayed` 唯一调用点 `_d__447:1426`（state 0xb，登场链后）：0.3 + 0.85 + 0.45 = **1.30**；战术 0；自动/召唤不广播；`teleport` ≈1.25（待核） | `EventTiming.PlayBucketHandPlayedHold = 1.30f`（带逐段判据）+ `BattleDriver.PlayBucketOffsetOf(e)`（`Play` + `Slot>=0` + 卡是单位 ⇒ 1.30；战术/法术/`Slot<0` ⇒ 0）+ `PlayPlacedPieces` 新形参 `baseAt`（折进 `at`，`ScheduleAction` 会推队尾 ⇒ 下一批也跟着等） | ✅ 已改（`teleport` 按 1.30，差 0.05 如实记；`targeted && fromEnemyHand` 的 0.5 居中展示前置**未接**） |
| 9 | **`Slot<0`（墓地→手/库）= 0.3** | `CreateCardInGame(…, timeToDrawPlayerCard 0.3)`，整条协程只这一个等待；`ephemeral` 原版无此链 ⇒ 0 | `EventTiming.ReturnHoldOf` 的 `Slot<0` 支：`Effect == "ephemeral"` ⇒ 0，其余 ⇒ `ReturnGraveHold = CardFeel.DealDuration`（0.3，引用同一字段） | ✅ 已改 |
| 10 | **回合高亮互斥** | 每副牌堆一对、各自互斥；实况两枚同 True = 两个不同 `DeckManager` | **零改动**（我们的互斥实现正确） | ✅ 已核（无改动） |

### 9·1 需要主对话复跑 / 重定基线的断言（⛔ 本代理不跑 Unity）

- **必跑 `BattleScene.Run`**（时序变了三格：`Return` 分档、`Hit` 易伤 1.4、`play` 桶 +1.30）。
- **`play` 桶 +1.30（影响面最大）**：凡是**「打出一个单位之后按固定 `Step(秒)` 推表」并断言那件 `play` 触发动画已起播/已截图**的格子都要 +1.30（或改用 `DrainQueue`）。
  现扫**没有**直接断言 `PlayPlacedPieces` 的格子（`grep PlayPlacedPieces` 在 `Editor/` 0 命中）⇒ 风险点是**间接的**（`VfxLog`/`CardEffects` 记录窗口），扫法：
  `python -I -X utf8 -c "import io,re;s=io.open('d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs',encoding='utf-8').read().split(chr(10));[print(i+1,l.strip()[:70]) for i,l in enumerate(s) if re.search(r'Step\(|DrainQueue',l) and i>0 and re.search(r'Play|Deploy',s[i-1])]"`
- **`Hit` 1.4**：只有**带 `vulnerable` 的受击方**那一拍变长 0.4 ⇒ 那类夹具（若有）的后续 `Step` 要 +0.4。`Editor/BattleScene.cs` 里 `grep -n Vulnerable` = **0 命中** ⇒ 今天**没有**受影响断言；`RuleEngineTest` 只测伤害数值（不测表现时间）⇒ 不受影响。
- **`Return`**：回手那一档 **0.75 不变**（`BattleScene.cs:8927` 那一组步进 0.35+0.70 ⇒ 不受影响）；**回牌库 +0.2**（只影响「洗回牌库」那类效果的后续事件）· **墓地取回 +0.3**（`Slot<0` 那一族）。
- **2 / 4 / 7 三条数据未到 ⇒ 今天零行为变化**，不影响任何基线；数据落地后要**再复跑一次**（那时 `play`/技能拍的时长会变）。
- **5（旋转基）是纯视觉**：`BattleScene` 里若有断言比 `toTarget` 的四元数/欧拉角，要按新式重算（`grep -n "AttackRotDeg\|toTarget" Editor/` 现读 0 命中）。

### 9·2 数据/白名单外的三处（写清「还差什么」）

| 缺口 | 要谁做 | 具体动作 |
|---|---|---|
| **`minionMovementInfo` 12 列** | 工具代理（`工具/gen_cardanim_clips.py` 不在本片白名单） | 每行从 `cardAnims[i].animInfo.minionMovementInfo` 取：`shouldActivateMinionMovement→mvt` · `delay→mvd` · `animTime→mva` · `visibilityStateAfterFinish→mvvis` · `useXZCurve/useYCurve→mvxz/mvy` · 三条 `minionMovementAnimCurve*.m_Curve`（扁平 `[t,v,…]`）`→mvcx/mvcy/mvcz` · `xOffset/yOffset/zOffset→mox/moy/moz`。现读非 0 行 **2 行**（`Scarab_Explosion` 等，`animTime 0.6`）。⚠️ **曲线位移的播放器仍未建**（插值算式只读到 `_d__358:77-120`）—— 要另读 388 行那条 state 机。 |
| **`screenShake` 6 列** | 同上（+ PathID→名字那一跳） | `screenShake.presetSO.m_PathID` → preset 名（`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/<名字>_<pathID>.json`）`→shk` · `delay→shkd` · 两个 `overwrite*` 开关 `→shkoa/shkof` · `amplitude`/`frequency→shka/shkf`。现读 **36 行 / 10 个 preset**。 |
| **`alt`（替代支时长）** | 工具代理（`工具/gen_ability_anim_durations.py`） | 全量件的 `alternative.seconds` → 运行期 items 的 **`alt`**（SW21 = 0.2；其余不写 = 0）。 |
| （附）**`DeployHoldOf(HandPlayed)=0.4`** | 主对话裁定 | `J1` §1 指出那 0.4「取错了三段」（0.1 + teleport 才有的 0.3，且整条省掉 0.85），但 `J1` 同时明确**别把它并进 `play` 桶那一笔**。⇒ 本轮按指令**没动** `DeployHoldOf`；若确认 `Deploy` 这拍的真实时长也是 1.30，要单独立一笔（会再动一次整局基线）。 |

### 9·3 本轮对前两批结论的订正（铁律 5）

1. §一 1·3 #23「`minionMovementInfo` 不改变这一拍时长」→ **错**（if/else，走它就不等 `GetAnimDuration`）。已在 §九 #2 改口径。
2. §一 1·3 #16「`Hit` 的 7/8 那条路**没有这道门**」→ **错**（0.4 之后照走 1.0 = 1.4）。`SourceOf(Hit)` 已就地订正。
3. §二 修 3 的「回牌库 0.85」→ **0.95**（`MoveToDeck` 返回值 = 0.1 + 0.3）。已在 `ReturnHoldOf` 的 doc 与 `SourceOf(Return)` 订正。
4. §三·1「0.4 / 0.7 / 1.05 三个候选」→ **1.30**（`J1` §1 已闭合）；§三·2 的「`PlayerAction+0x4c`」→ **`BattleAction.fromEnemyHand`**（`+0x3c`）。两条都已在 §九 #8 落成代码/注释。

---

## 十 第三批的验证（可复核）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改后连跑 2 次） |
| 行尾 | `BattleDriver.cs` **30662 CRLF / 30662 LF**（未翻）· `CardAnimClips.cs` / `EventTiming.cs` / `AbilityAnimDurations.cs` / `CardFeel.cs` **纯 LF**（与改前一致） |
| `git diff --numstat`（累计，含前两批与别人开工前就有的改动） | `BattleDriver.cs 752/65` · `CardAnimClips.cs 86/2` · `EventTiming.cs 265/16` · `AbilityAnimDurations.cs 24/0` · `CardFeel.cs 23/5`（**都没有整篇重写**） |
| 未跑 | Unity 批处理 / 自检（红线）⇒ **需主对话复跑 `BattleScene.Run`**（清单见 §9·1） |
| 本轮改动文件 | `Battle/BattleDriver.cs` · `Battle/CardAnimClips.cs` · `Core/EventTiming.cs` · `Core/AbilityAnimDurations.cs` · `Core/CardFeel.cs`（全在本片白名单内；`Editor/BattleScene.cs` **一个字节没动**） |

---

## 十一 追加 · 卡面高亮族（`CF` 落地后的 BattleDriver 侧接线）

> 2026-10-25 第四轮。只动 `Battle/BattleDriver.cs`（`Core/CardView.cs` 归 `CF`、`Editor/BattleScene.cs` 归 `R`，⛔ 没碰）。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；行尾见 §十一·二。

### 十一·一 改了什么

| 项 | 内容 |
|---|---|
| **原版口径** | 每张卡自己带两个 `HighlightAttackType` 实例（近战 / 远程），`CardScript.ChangeAttackType` 时**只点亮这张卡自己的那一格**（底光 + 该数值 ×**1.35**，0.15 s `OutQuad`）；`Spell(3)` / `Active(4)` **没有实例** ⇒ 两颗都不亮（判据 → `J2_判据_卡面高亮族与渲染残项.md` §A1/A2，`CardView` 那一大段注释已逐句抄过）。**「谁是被选中的目标」不碰底光** —— 那只由 `CardHighlight.ChangeState` 的描边（我们的 `SetHighlight(ValidTarget)`）表达。 |
| **病灶** | 驱动层三处 `SetTargetGem(...)` 传的是**「目标合法性」**（`GemForCommand(_command)`）。`CF` 把 `SetTargetGem` 改成同义转发到 `SetOwnAttackType` 之后，语义变成「**这张卡自己的** attackType」⇒ **目标卡**会按**攻击方**的打法把数值放大 1.35（中间态）。 |
| **改法** | 新增两个口（`:14279` / `:14294`）：<br>· `OwnAttackTypeOf(UnitState)` —— 引擎那一格 → `AttackTypes`：`Melee 1 / Ranged 2 / Active 4` 显式 switch，**`0` / `Spell(3)` / 认不出 ⇒ `None`**（⛔ 不猜默认打法）；<br>· `ApplyOwnAttackType(owner, slot, v)` —— 按 `Ctx.Players[owner].Board[slot]` 取**这张卡自己的** `LastAttackType` 刷卡面。 |
| **三处接线** | ① `HighlightTargetsCore` 的「另一侧」循环（`:14220`）：从「熄掉」改成**刷成每张卡自己的**（原版底光与「谁在选目标」无关）；<br>② 目标循环（`:14236`）：`SetTargetGem(legal ? … : None)` → `ApplyOwnAttackType(side, t, v)`（**与 `legal` 无关**），合法性只留下面那圈 `SetHighlight(ValidTarget)`；<br>③ `ClearSelection`（`:14852`）：**清选择不再熄底光**，同样刷成每张卡自己的。 |
| **`CommitCommand` 补一拍** | `:14057`：写完 `LastAttackType`（上一批已改成「选定时写」）之后**立刻**刷这张卡自己的高亮 —— 原版 `ChangeAttackType` 就是同一拍点亮自己。**非要有这一句**：`u != null && CommandTargetSide() < 0` 那条（不用选目标的主动技）下面直接 `Resolve` 就 return，走不到 `HighlightTargets` ⇒ 不加这张卡的底光永远不亮。 |
| **删掉的死代码** | `GemForCommand(...)`（全仓只被上面第 ② 处用）—— 语义已由 `OwnAttackTypeOf` 取代，⛔ 别照旧名加回来。`BattleDriver` 里现在**零** `TargetGem` 引用。 |

### 十一·二 需要 `R` 改/加的断言（`Editor/BattleScene.cs`）

**🔴 会直接变红的三条（它们测的是**旧**语义 —— 目标卡按攻击方打法亮）：**

| 位置 | 现断言 | 新语义下 |
|---|---|---|
| `BattleScene.cs:8949` | `driver.FoeUnits[foeSlot].CurrentGem == TargetGem.Melee`（提交近战后） | **红**（目标卡的底光 = **它自己的** `LastAttackType`，多半是 `None`）⇒ 改成下面 ①②③ |
| `BattleScene.cs:9072` | `…CurrentGem == TargetGem.Ranged`（切远程后） | **红**（同上） |
| `BattleScene.cs:9076` | `SimulateDeselect()` 后 `…CurrentGem == TargetGem.None` | 今天**碰巧仍绿**（那张敌人从没攻击过 ⇒ 自己就是 None），但语义已变（不再是「清选择 ⇒ 熄底光」）⇒ 建议一起改掉，⛔ 别留着当保护 |

**要新增的三条（协调台点名的 ①②③）：**

1. **有 `LastAttackType=Melee` ⇒ 底光开 + 数值 ×1.35**：造一个我方单位，`SimulateOpenCommand` → `SimulateCommand(Melee)`（`CommitCommand` 会写 `LastAttackType` 并刷自己）⇒ `v.CurrentGem == TargetGem.Melee` **且** 近战数值格的 `localScale.x` ≈ `StatBaseScaleOf(StatMelee) × 1.35`（读 `CardView` 的只读口或用 `OwnAttackType` + 现有统计层读数）。🧨 把 `:14057` 那句 `ApplyOwnAttackType` 删掉 ⇒ 红。
2. **`LastAttackType=None`（没选过打法 / 手牌卡）⇒ 底光关、数值 ×1.0**：一张从没攻击过的敌人 ⇒ `CurrentGem == TargetGem.None` 且数值 scale = 基准。🧨 在 `OwnAttackTypeOf` 里给个默认值（比如 `Melee`）⇒ 红。
3. **目标合法性变化**不再改底光（负例，防回退）：给敌人手工置 `LastAttackType = Ranged`，我方提交**近战**并 `HighlightTargets()` ⇒ 那张敌人的 `CurrentGem` **仍是 `Ranged`**（不是 Melee、也不是 None），只有 `SetHighlight` 的描边随合法性变（可读 `CardView` 的高亮状态口）。🧨 把 `:14236` 改回 `SetTargetGem(legal ? GemForCommand(cmd) : None)` ⇒ 红。

⚠️ 另：`:44330/:44338/:44481/:44483` 那四处仍直接调 `qv.SetTargetGem(...)` / `pv.SetTargetGem(...)` —— `CardView` 保留了这个旧口（同义转发），所以它们**照旧能编能绿**，但它们**测的已经是值域映射、不是生产语义**；建议一并挪到 `SetOwnAttackType`（不阻塞本批）。

### 十一·三 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 行尾 | `BattleDriver.cs` **30693 CRLF / 30693 LF**（未翻） |
| 改动文件 | 只有 `Battle/BattleDriver.cs`（`git diff --numstat` = **799/81**，含前三批与前序未提交改动；非整篇重写） |

---

## 十二 追加 · 卡本体位移播放器（`TL` 把 `_d__358` 读全了）

> 2026-10-25 第五轮。判据 = `TL_动画数据三列.md` §1·4（`_d__358` 388 行全读）＋ §5（三处没读全）；数据 = `TL` 已补进运行期表。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；行尾见 §十二·三。

### 十二·一 实现的算式（逐条对 `TL` §1·4）

| 步 | 原版 | 我们的实现（`BattleDriver.PlayMinionMovement`） |
|---|---|---|
| 1 | `delay > 0 ⇒ WaitForSeconds(delay)`（`:71-79`） | `CardTween.Use(...).SetDelay(delay)` |
| 2 | `start = TransformPoint(originalLocalPosInPlay)`（`:81-87`） | **等价物 = 起播这一刻的世界位置**（我们没有 `originalLocalPosInPlay` 那份缓存；让位/瞄准补间与它同帧结束，正常就是格位）**如实记** |
| 3 | `n = normalize(boardPos − start)`（`:101-112`） | `n = target.transform.position − start`（`GetBoardPosOfCard(targetCard)` 的等价物 = 目标卡的当前位置）；`sqrMagnitude ≤ 1e-8` ⇒ 出声、不位移（原版那一下是 `normalize(0)`） |
| 4 | `margin = isPlayer ? playerAttackMargin(1.7) : enemyAttackMargin(1.0)` | `CardFeel.PlayerAttackMargin` / `EnemyAttackMargin`（同一个常量，⛔ 没另写） |
| 5 | **`end = boardPos − n × margin`**（`:113-117`） | 同 |
| 6 | `end += v × Δy × attackPositionYOffset(0.5)`（`:126-137`） | 🔴 **跳过 + 出声一次**（`v` = `Vector3` 静态块那一格，静态读不到 ⇒ `TL` §5·1；Δy 我们有 `CardFeel.EffectsAnchorHeight`，但不知道乘哪个基向量 ⇒ ⛔ 不按 y=up 硬拍） |
| 7 | `end.x += xOffset; end.z += zOffset`（yOffset 不在这） | 同（`MoveOffX/MoveOffZ`） |
| 8 | 时长全 = `animTime`：`useXZCurve==false` ⇒ `DOMove(end)`（**不 SetEase**）；`true` ⇒ `DOMoveX(end.x).SetEase(curveX)` + `DOMoveZ(end.z).SetEase(curveZ)`；`useYCurve` ⇒ 另加 `DOMoveY(yOffset + end.y).SetEase(curveY)` | 同（Sequence：`Append(X)` + `Join(Z)` + `Join(Y)`；扁平 `[t,v,…]` → `AnimationCurve`；`useXZCurve==false` 那条**不 SetEase** ⇒ 吃 `DOTween.defaultEaseType`）｜全走 `CardTween.Use` + `SetLink`（批处理 = `Manual`，由 `CardTween.Advance` 推，⛔ 不靠 `Update`） |
| 9 | 收尾：`position = end`；`vis==0 ⇒ ToggleBody3D(false, true)`；`vis!=0 ⇒` 什么都不做 | `OnComplete`：钉回 `end`；`MoveBodyEndVis == 0` ⇒ `SetBody3DVisible(false)`；`!= 0` ⇒ **出声 + 什么都不做**（无样本，⛔ 不猜「显示」）。⚠️ `ToggleBody3D(_, true)` 的「`effectsAnchor` 显示」那半**不用做**（我们的 `EffectAnchorBoard` 从不被关） |
| 10 | 三轴旋转（`LerpUnclamped` + 与 `startRotation` 连乘） | 🔴 **整段不做 + 出声一次**：三条旋转曲线**没有进任何运行期表**（`TL` §1·2 第 4 条）+ 连乘顺序未定序（§5·2）⇒ 保持原朝向（位置公式不受影响） |

**改动**：`BattleDriver.PlayMinionMovement`（方法体 `:21701` 起、判据 doc 在它上面；签名从 `(CardView, CardAnimPlacement)` 改成
`(self, target, CardAnimPlacement, isPlayer)`）+ `MinionCurve(flat)`（`:21781`）+ `WarnMinionMove`（`:21793`，5 类各喊一次）；两处调用点
（`PlayPlacedOne` / `PlayOneUnitAnim`）按新签名传 `target` 与 `e.Player == _me`。

### 十二·二 数据已到位 ⇒ 从「零变化」变成**真的会生效**（这改基线）

现读运行期表：`mvt==1` **2 行**（`Scarab_Explosion` / `SporeMine_Explosion`，`mva=0.6` `mvxz=1` `mvy=1` `mox/moy/moz=−0.1/0.2/−0.1`、三条曲线非空）· `shk` **57 行 / 10 个 preset** · `alt` **1 条（SW21 = 0.2）**。

| 生效点 | 影响 |
|---|---|
| **时长**（`SkillBeatDuration` / `PlayPlacedPieces` 的 `beatDur`） | `Scarab_Explosion`：0.5 → **0.6**（+0.1）· `SporeMine_Explosion`：**0.75 → 0.6**（−0.15）。⚠️ 其中 `Scarab_Explosion` **不在 `card_vfx_by_card.json` 里**（当前卡池没有它）⇒ 今天只有 `SporeMine_Explosion`（`TL37 Spore Mine` 的 `trigger` 桶）会变。 |
| **屏震**（`PlayBeatShake`） | **57 行真会响**（原版那 57 拍的 `screenShake`）。只动相机/HUD 平移，**不改时长**。 |
| **替代支**（`AbilityHoldOf`） | `SW21` 在「棋盘条件不满足」那一档 ⇒ **0.2**（普通支仍 0.4）。 |
| **一段真缺陷（如实记）** | `SporeMine_Explosion` 所在的事件（`TL37`「回合开始摧毁自己并对随机敌人 3 点」）**我们的事件不带目标**（`TargetPlayer/Slot = -1`）⇒ `boardPos` 算不出来 ⇒ 走「**没有目标卡**」那条：**出声一次 + 只占 0.6 s 时长、不位移**。要真看到那一下位移，得让 `RuleEngine` 的 `trigger` 事件带上目标（原版 `PlayAnim` 是**按目标逐个**播的，见 `TL` §1·4 的调用点 `:214-231`）——**不在本片白名单，已记账**。 |

### 十二·三 需要 `R` 的断言（`Editor/BattleScene.cs`）

1. **时长 = `delay + animTime`**：`BattleDriver.SkillBeatDuration` 对一条 `MoveBody=true` 的 `CardAnimClip`（`mvd=0` `mva=0.6`）⇒ **0.6**；🧨 删掉 `SkillBeatDuration` 里那句 `if (clip.Place.MoveBody)` ⇒ 回 0.75/0.5 ⇒ 红。
2. **XZ 曲线只在 `useXZCurve` 时走**：`MinionCurve(flat)` 对非空曲线回非 null、对空数组回 null；再加行为面 —— `useXZCurve=true` 的那一档（现读 2 行都是 1）位移由**两条独立轴补间**驱动 ⇒ 断言「X/Z 两条子补间都存在」；`useXZCurve=false` 的构造（自造一个 `CardAnimPlacement`）⇒ 只有 1 条 `DOMove`。🧨 把 `if (p.MoveUseXZ)` 分支写死成 true ⇒ 第 2 条红。
3. **`useYCurve` 与否的 y 分量差异**：`useYCurve=true` ⇒ 有 `DOMoveY`（末值 = `yOffset + end.y`）；`false` ⇒ 序列里没有 Y 轴补间（y 保持不变）。🧨 去掉 `if (p.MoveUseY)` ⇒ 红。
4. **三轴旋转**：⛔ **今天不能断「转到位」** —— 数据没有旋转曲线、我们**明说不做**；只能断一条负例：「`PlayMinionMovement` 跑完卡的 `rotation` **没变**」（= 现状固化，等数据/顺序闭合再改成正向断言）。
5. （附）**收尾**：`MoveBodyEndVis == 0` 那一档，`animTime` 到点后 `SetBody3DVisible(false)` 生效（现读两行都是 0）；`!= 0` 那一档只能断「出声 + 不隐藏」（无样本）。

### 十二·四 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（连跑 2 次） |
| 行尾 | `BattleDriver.cs` **30800 CRLF / 30800 LF**（未翻）· `CardAnimClips.cs` / `EventTiming.cs` / `AbilityAnimDurations.cs` 纯 LF |
| `git diff --numstat`（累计） | `BattleDriver.cs 906/81` · `CardAnimClips.cs 88/2` · `EventTiming.cs 263/16` · `AbilityAnimDurations.cs 23/0` · `CardFeel.cs 23/5` |
| 未跑 | Unity ⇒ **需主对话复跑 `BattleScene.Run`**（§十二·二 那三样现在真的会改时长/画面） |
| 本轮改动文件 | `Battle/BattleDriver.cs` · `Battle/CardAnimClips.cs` · `Core/AbilityAnimDurations.cs` · `Core/EventTiming.cs`（只是把 3 处「数据未到」的注释改成「已到」，铁律 5） |

---

## 十三 追加 · `J3` 闭合的三条（三轴旋转 / 准星 0.5 / 回牌库 0.65）

> 2026-10-25 第六轮。判据 = `J3_判据_战斗剩余四条.md` §1/§2/§4（`TL` 的「没查清」与 `AU1` §十二 的两条**已作废**）。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；行尾见 §十三·四。
> 只动 `Battle/BattleDriver.cs` / `Battle/CardAnimClips.cs` / `Core/EventTiming.cs`。

### AU1-E · 卡本体位移的三轴旋转 —— ✅ 已实现（含那条 `v` 项）

| 项 | 内容 |
|---|---|
| **订正（铁律 5）** | `TL` §1·2 第 4 条 / 我上一批 §四 #10 / `BattleDriver` 里那句「旋转曲线没进任何运行期表」**已过期** —— 运行期 `Resources/cardanim_clips.json` **现读 10 行**带 `mmRxE/mmRxM/mmRxC`（+Y/Z 同形），生成器早就在写，只是 `RtRow` **没有那 9 个字段**（`JsonUtility` 不吃的键根本不进 C#）。本批三处注释都已订正。 |
| **数据装载** | `CardAnimClips.RtRow` +9 列（`mmRxE/mmRxM/mmRxC` · `mmRy*` · `mmRz*`）· `CardAnimPlacement` +9 属性（`RotXOn/RotXDeg/RotXCurve` 同形三组）· `_table[...]` 逐项赋值；字段 doc 抄了 `J3` §2 的判据（轴 = 世界 right/up/forward，`maxValue` 单位 = 度）。 |
| **`v` 项（上一批跳过的那个）** | `v` = **`Vector3.up`**（`J3` §2·②）⇒ `end.y += CardFeel.EffectsAnchorHeight(target) × CardFeel.AttackPositionYOffset`（0.5）。**上一批那条「跳过 + 出声」已删**。 |
| **旋转算式** | 起播那一刻取 `startRot`；`move.OnUpdate` 里逐帧 `t01 = Elapsed(false)/animTime`（不 Clamp），`q = enable ? Quaternion.SlerpUnclamped(startRot, AngleAxis(maxValue, axis), curve.Evaluate(t01)) : identity`，**`transform.rotation = qX * qY * qZ * startRot`**（三次的 `cur` 都是 `startRot`）。曲线**先建一次**（不每帧 `new AnimationCurve`）。🔴 **必须 Unclamped**（中键 −0.952 ⇒「先反拧约 −43° 再甩到 +45°」）。 |
| **如实记一处原版形状** | 单轴 `SlerpUnclamped(startRot, …, k)` 在 `k == 0` 时返回 **`startRot`（不是 identity）** ⇒ `startRot` 非单位四元数时 t=0 那一帧会算成 `startRot⁴`。**原版是同一式**（`J3` §2·③/④），而双方场上卡都是 billboard（`rotation ≈ identity`）⇒ 今天无可见差；⛔ 别为此把 k=0 改成 identity（那是改原版口径）。 |
| **偏移的两条支** | `end.x += xOffset; end.z += zOffset` **与目标无关** ⇒ 从「只在有目标那一支」改成**两条支都加**（「没有目标卡」那条退路里也加；⛔ 不是猜方向）。 |
| **现读唯一会真跑的卡** | `SporeMine_Explosion`（`TL37 Spore Mine`，`rotateXAxis enable=1 maxValue=45.0 animTime=0.6`，曲线 `[0,0]·[0.2373,−0.952]·[1,1]`）。⚠️ 它所在的事件**我们不带目标** ⇒ 走「没有目标卡」那条：**不位移，但旋转 + x/z 偏移照走 + 0.6 s 后隐藏卡体**（比上一批「原地不动」更接近原版；`boardPos` 那一段仍缺 —— 要 `RuleEngine` 给 trigger 事件带目标）。<br>`Scarab_Explosion` 虽也在池（`SAU52`），但现行 `card_vfx_by_card` 给它的是 `Necrons death explosion` ⇒ **不触发**。 |

### AU1-F · 战术 / 低语 · `targeted` · `fromEnemyHand` ⇒ `play` 桶 +0.5

| 项 | 内容 |
|---|---|
| **判据** | 那 0.5 s = **准星定位 `EnemyTargetingAnim`**（不是「居中展示」——`J1` 的**值**对、**名**错）；`BattleManager__GetTargettingTime.c` = `VarsGlobal.targettingAnimTime(+0x88)` = **0.5**；`_d__447:477-491`：取牌 + `PayCardCost` 之后 → `EnemyTargetingAnim` + `WaitForSeconds(0.5)` → `state 0xC` → **法术在 `TargetedSpellPlayed` 才生效**。 |
| **门** | `cardType ∈ {Tactic(20), Whispers(40)}` ∧ `rawCardData.targeted(+0x268)` ∧ `BattleAction.fromEnemyHand(+0x3C)`。我们的对应：`def.Type == "tactic"`（池内低语并入这一档，没有单独的 `whisper`）∧ `def.Targeted == 1` ∧ **`e.Player != _me`**（`fromEnemyHand` 的等价物 = 打出方不是本机）。 |
| **改动** | `BattleDriver.PlayBucketOffsetOf`：非单位卡从「一律 0」改成按上面那道门 ⇒ 0.5；**回放局**（`ReplaySkipAnims()` 的等价物 = `_replaySession`）**÷3 = 0.1667**。常量在 `EventTiming`：`EnemyTargetingHold = CardFeel.EnemyTargetingAnimTime`（0.5，同一个 `VarsGlobal` 字段）+ `ReplaySkipAnimDivisor = 3f`（`DAT_1834b2e8c` 实读）。 |
| **真时演那半** | 早就有了（`StartAiTargetingAnim/TickAiTargetingAnim/FinishAiTargetingAnim`，0.5 / OutQuad）⇒ 本批只补**时间线**上的偏移，⛔ 没碰那三个方法。 |
| **影响面** | 只有「**敌方**（AI/联机对面）从手牌打出**带目标**的战术/低语」这一档的 `play` 桶 VFX 晚 0.5 s 起播；我方自己的 → 0（`fromEnemyHand == 0` 原版直接跳过）；单位仍 1.30。 |

### AU1-G · 回牌库 0.65 —— **不改，只记账（判据闭合）**

- `J3` §1：`card+0x54` = **`EntityScript.armedTrap`**（`dump.cs:21983`；唯一置 1 点 `CardScript__SetAsActiveTrap.c` = 「陷阱已布置、待触发」），`MoveToDeck` 的门是**并列** `ShouldShowCardback(cardState) || armedTrap`；那 0.3 s = 「向上弹出 15 单位 + 翻牌」**整段**（跳过时 `MoveToDeck` 只返 0.1 ⇒ 0.65）。
- **`ShouldShowCardback` 真值表**（已抄进 `EventTiming.ReturnHoldOf` 的 doc）：`inDeck(0)/inDeckDrawing(16) ⇒ 1`；`inHand(1) ⇒ 只有敌方手牌`；`inHandJustCreated(12) ⇒ 我方看 armedTrap / 敌方恒 1`；**其余 11 档 ⇒ 0**。
- **结论**：从**场上**回牌库（`cardState = inPlay(2)`）⇒ 那 0.3 段照走 ⇒ 我们的 **0.95 是对的**；0.65 只在「卡没离过牌库就回」与「`armedTrap == 1`」两条路上成立，而**这两条路我们的引擎都没有** ⇒ ⛔ 不改（`EventTiming` 的 doc 与 `SourceOf(Return)` 已按此订正）。
- **顺手订正**（`BattleDriver.cs` 的 `EnemyTargetingAnim` 那段头注，铁律 5 · 未验行为）：原文「三个调用点的守卫都是施法方 `isPlayer == false`（含 `_d__438:823,1476`）」⇒ 改为：`_d__438` **全文没读 `+0x40`**、它那一支的门只有「守方 `CardHighlight != null`」（而每张卡都有 ⇒ **双方都会演，我们只给 AI 侧演、可能少演一次，未验**）；且 `EnemyTargetingAnim` 在 `_d__438` **只有 `:1476` 一处**、没有 `:823`。**行为一个字没改**。

### 十三·四 验证与需要 `R` 的断言

| 项 | 结果 |
|---|---|
| `typecheck` | **运行时 0 / 编辑器 0**（连跑 2 次） |
| 行尾 | `BattleDriver.cs` **30855 CRLF / 30855 LF**（未翻）· `CardAnimClips.cs` / `EventTiming.cs` 纯 LF |
| `git diff --numstat`（累计） | `BattleDriver.cs 964/84` · `CardAnimClips.cs 126/2` · `EventTiming.cs 290/16` |
| 未跑 | Unity ⇒ **需主对话复跑 `BattleScene.Run`** |

**要 `R` 加的断言（`Editor/BattleScene.cs`）：**
1. **数据装载**：`CardAnimClips.PlacementOf("SporeMine_Explosion")` ⇒ `RotXOn == true`、`RotXDeg == 45f`、`RotXCurve` 三键 `(0,0)(0.2373,−0.952)(1,1)`（🧨 `RtRow` 少一列 ⇒ 红）。
2. **旋转到位**：`PlayMinionMovement` 跑满 0.6 s 后，卡的 `rotation` ≈ `Quaternion.AngleAxis(45°, Vector3.right) * startRot`（`startRot` 取 identity 时 ≈ `Euler(45,0,0)`，容差 2°）；🧨 把 `SlerpUnclamped` 换 `Slerp` ⇒ **中段**那条红（见 3）。
3. **Unclamped 判别式**（灭自证）：同一运行到 `t01 ≈ 0.2373`（`delay + 0.1424 s`）时，`rotation` 的 X 分量 ≈ **−43°±5°**；🧨 用钳到 0 的实现 ⇒ 那时是 0° ⇒ 红。
4. **`v` 项**：`end.y` 比「不算 Δy」那一版高 `EffectsAnchorHeight(target) × 0.5`（自造一个 `EffectsAnchorHeight` 已知的夹具 ⇒ 差 ≈ 那个乘积；🧨 删掉那句 ⇒ 差 0 ⇒ 红）。
5. **`play` 桶 0.5**：`PlayBucketOffsetOf` 对「敌方 + `tactic` + `Targeted==1`」= **0.5**、回放局 = **0.1667**、我方同条件 = **0**、单位 = **1.30**（🧨 把门写成 `def.Type == "tactic"` 不看 `Targeted`/`e.Player` ⇒ 后两条红）。
6. **回牌库 0.95 不变**（回归）：`EventTiming.ReturnHoldOf` 对「场上回牌库」仍 = 0.95（🧨 凭「卡背就该 0.65」改成 0.65 ⇒ 红）。

---

## 十四 追加 · 易伤两拍的判别式（`E4` 拆完两拍后的接续）

> 2026-10-25 第七轮。只动 `Battle/BattleDriver.cs` / `Core/EventTiming.cs`。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；行尾见 §十四·三。

### 十四·一 改了什么

| 项 | 内容 |
|---|---|
| **判据换位（必须）** | `EventTiming.HitIsVulnerable`：`BattleDriver.cs:3646` 从「**受击方现在带 `vulnerable`**」改成 **`e => e != null && e.IsVulnerableSecondHit`**（`E4` 在 `RuleEngine.Core.BattleEvent:553` 新加的那一位）。<br>🔴 **不改的后果**：`ApplyDamage` 尾巴现在把一次易伤伤害拆成**两条串行** `EvtKind.Hit`（主 = `actual − vulnPart`、第二 = `vulnPart` 且带 `IsVulnerableSecondHit`，`RuleCore.cs:8160-8162`）⇒ 旧判据对**两条都**回 true ⇒ **每题两拍都排 1.4 s**（整条多 0.8 s）。 |
| **语义确认（口径与原版逐段同形）** | 主拍 `IsVulnerableSecondHit = false` ⇒ `HitHoldOf` = **1.0**（`_ReceiveDamage:342` 的 `WaitForSeconds(1.0)`）；第二拍 = **1.4**（`:291-303` 先等 **0.4** 再等那 1.0）。`DelayBetween(Hit→Hit) = 0`（default）⇒ 两拍**串行**，整条 = **1.0 + 1.4 = 2.4 s**，与 `J1` §5 / `J3` §3 的「主 1.0 + 易伤 1.4」**逐段一致**。 |
| **`HitVulnerablePreWait`（0.4）会不会重复计？** | **不会，也不用改**：那 0.4 是**第二拍自己头上**的等待（原版那一次 `_ReceiveDamage` 里 `:291-303` 先等 0.4、再走到 `:342` 的 1.0），**不是**拍与拍之间的额外前置 —— 两拍之间**没有**别的等待（`E4` 的 `DealMultiDamage` 是 `void`、只入队 `priority = 2`）。⇒ 0.4 已经含在「第二拍 = 1.4」里，⛔ 别在 `DelayBetween` 再补一次。 |
| **`doItFast` 例外** | 仍**不实现**（本 build 6 个调用点第 3 个实参全是 0 ⇒ 不可达）。 |
| **🔴 顺手补的一处（同一件事的下游）** | `PlayHitFeel`（`BattleDriver.cs:23072`）：新增 `bool recoilBeat = !meleeAttackFollow && !e.IsVulnerableSecondHit`，**易伤第二拍不做后坐、不做震屏**（飘字照旧）。判据 = 原版第二拍只是一条 `_ReceiveDamage`（写血 → `UpdateHealthText` → … → `DamageReceived` 伤害计数器 → `WaitForSeconds`），**没有** `ReceiveAttackAnim`（后坐只挂在近战攻击动画的命中回调）、震屏同理。⛔ 不补这一句 ⇒ 一次挨打会**抖两下、震两次**。 |

### 十四·二 `EventTiming` 侧同步订正（铁律 5）

`HitHoldOf` 的 doc 原来写着「我们的引擎把易伤折进同一条 `Hit` ⇒ 取 1.4 仍比原版短 1.0」—— **已过期**，改为：
两拍串行 ⇒ `1.0 + 1.4 = 2.4`，与原版同形；并写清「判别式只读 `IsVulnerableSecondHit`，⛔ 别拿『受击方带 vulnerable』当替身」。

### 十四·三 验证 + 需要 `R` 的断言

| 项 | 结果 |
|---|---|
| `typecheck` | **运行时 0 / 编辑器 0**（连跑 2 次） |
| 行尾 | `BattleDriver.cs` **30857 CRLF / 30857 LF**（未翻）· `EventTiming.cs` 纯 LF |
| `git diff --numstat`（累计） | `BattleDriver.cs 968/86` · `EventTiming.cs 293/16` |
| 未跑 | Unity ⇒ **需主对话复跑 `BattleScene.Run`**（`RuleEngineTest` 的 E4 那几条是引擎侧，已经由 `E4` 自己加过） |

**要 `R` 加的断言（`Editor/BattleScene.cs`）：**

1. **两拍齐全**：造一个带 `vulnerable` 的目标吃一次伤害 ⇒ 该批信号里 `EvtKind.Hit` **恰有 2 条**，且**恰好 1 条** `IsVulnerableSecondHit == true`；🧨 引擎退回一拍（或两条都带该位）⇒ 红。
2. **主拍 1.0**：`EventTiming.DurationOf(第一条 Hit)` == **1.0**（`IsVulnerableSecondHit == false`）；🧨 用旧判据（受击方带 vulnerable）⇒ 变 1.4 ⇒ 红。
3. **次拍 1.4**：`EventTiming.DurationOf(第二条 Hit)` == **1.4**（`== HitWaitSeconds + HitVulnerablePreWait`）；🧨 把 `HitVulnerablePreWait` 置 0 ⇒ 红。
4. **无易伤只有一条**：不掉 `vulnerable` 的目标挨一次 ⇒ 该批 `Hit` **恰 1 条**且 `IsVulnerableSecondHit == false`。
5. **致死只有一条**：一击致死（主拍就把血打光）⇒ `Hit` **恰 1 条**（`vulnSecondPart > 0` 那个门在引擎里；🧨 引擎那条闸写错 ⇒ 2 条 ⇒ 红）。
6. **第二拍不后坐/不震屏**（本轮新加）：第二拍那一帧 —— 目标卡的位姿**没有** punch（读前后 `localPosition` 差 ≈ 0）且相机震屏计数**没涨**；同一次伤害的**主拍**那条涨 1。🧨 删掉 `&& !e.IsVulnerableSecondHit` ⇒ 计数 +2 ⇒ 红。
7. （附）**整条时长**：`主 + 次 = 2.4`（`DurationOf` 两条相加，`DelayBetween` 两条都是 0）。

---

## 十五 追加 · 主动能力的候选侧（友方 / 自己目标层）

> 2026-10-25 第八轮。只动 `Battle/BattleDriver.cs`。判据 = `E5_引擎剩余小缺口.md` §E5-2/§E5-3 + 反编译。
> 验证：`TMPDIR=/tmp/wf_au1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；行尾见 §十五·四。

### 十五·一 原版判据（读到的）

| # | 结论 | 出处 |
|---|---|---|
| 1 | 候选合法性里**没有任何「候选恒敌方」的限制** —— 只按「**异侧** `stealth(70)` / `ward(140/145)`」挡，其余由 `activeAbility.targetCriteria`（`+0x38`）的 36 格决定 | `BattleManager__IsValidActiveAbilityTarget.c:47-56` · `RawCardScript__IsValidActiveAbilityTarget.c:94-107`（`E5` §E5-2 已逐句） |
| 2 | **候选侧**只由 `friendlyUnits` / `enemyUnits` 两格决定；`includeSelf` 是**过滤器**（在已定的那一侧里再筛自己），**不是**第三侧 | `TargetCriteria` 字段表 · `AM69` 的读数（`friendlyUnits=1 / enemyUnits=0 / includeSelf=1`） |
| 3 | 现池 `activeAbilityPickCriteria` **107 行**分布：**只敌方 12** · **只友方 24** · **敌我双方都算 36** · **两侧都没标 (0,0) 35** | 本轮现读 `cards_engine.json` |
| 4 | 今天**可达**的「友方目标」卡 = **0**：`AM69` 的 `Card.Ability == null`（正文没有 `Ability:` 段 ⇒ `CanStartAbility` 先回 `ErrNoAbility`） | `E5` §E5-3 |

### 十五·二 改了什么（`CommandTargetSide` 的 `Ability` 那一支）

| 项 | 内容 |
|---|---|
| **入口** | `CommandTargetSide`（`:13921` 起）的能力支：原来 `return u.Ability != null && EffectTargets.NeedsPick(u.Ability.Target) ? 1 - _me : -1;` ⇒ 现在 `return AbilityPickOwner(u);` |
| **判据本体（纯读）** | 新增 `enum AbilityPickSide { Unknown=0, Enemy=1, Friendly=2, Any=3 }` + `AbilityPickSideOf(UnitState, out why)`（`:13979`）：先 `CardDef.UsesAlternativeAbility` 定普通/替代支（`null` ⇒ Unknown，⛔ 不猜），再 `CardDef.ActiveAbilityPickCriteriaFor(ab, out why)` —— 四条判定：`(1,0)` ⇒ **Enemy** · `(0,1)` ⇒ **Friendly** · `(1,1)` ⇒ **Any（双方）** · `(0,0)` / 列没落 / 支判不出 ⇒ **Unknown**；`set == null && why == null`（`targetsAffected = any`，生成器不落行）⇒ **Any**。 |
| **行为** | `AbilityPickOwner`（`:13965`）：**只有 Enemy 静默走 `1 - _me`（= 现状）**；Friendly / Any / Unknown **一律出声一次**（`NoteAbilitySideGap`，每卡每类原因一次 + 计数口 `AbilitySideGapWarningsForTest`）**并退回 `1 - _me`**。 |
| **自检口（新增两个）** | `CommandTargetSideForTest`（`:14026`，生产同一条判据）· `AbilityPickSideForTest`（`:14013`，`0/1/2/3` = Unknown/Enemy/Friendly/Any，**只读数**）· `AbilitySideGapWarningsForTest` + `ResetAbilitySideGapWarningsForTest()`。 |

### 十五·三 🔴 为什么**没有**把友方候选真的打开（本条最重要）

**不是「读不到」，是「引擎结算层只认敌方」** —— 三条现读：

1. `RuleCore.UseAbilityCore`：`var chosen = EffectTargets.NeedsPick(spec.Target) ? ctx.Players[1 - p].Board[targetSlot] : null;`
   —— 写死 `1-p`；
2. `RuleCore.CanUseAbility`（`E5` 新加的那道闸的**包装**）：`return CanUseAbilityTarget(ctx, p, caster, /*targetOwner*/ 1 - p, targetSlot);`
   —— **也写死 `1-p`**（`CanUseAbilityTarget` 本身已经支持显式 `targetOwner`，但只有 `E5` 的断言会传别的侧）；
3. `EffectTargets` 的目标规格只有 `self / ownwarlord / enemywarlord / enemyunit`（`NeedsPick` 只认 `enemyunit`），
   `ResolveTarget` 也只有这四支 —— **没有「友方单位」这一档**。

⇒ 表现层若把友方格位放行，引擎会拿**敌方棋盘的同号格**去解：那格空 ⇒ `ErrTarget`（看起来「点不动」）；
那格有人 ⇒ **打错人**。这正是 `E5-2` 亲手实测过的「**静默错打**」那一类，所以本批**故意**只做到「判据 + 出声 + 退回」。

**要真做（item 2/3 的完整形态）需要引擎侧两处**（`RuleEngine/**` 不在本片白名单）：
① `CanUseAbility` / `UseAbilityCore` 的候选所有者从 `1-p` 改成**与表现层同一个决策**（把 `AbilityPickSideOf` 的结论传进去）；
② `EffectTargets` 加 `FriendlyUnit` 规格 + `ResolveTarget` 对应支（`ctx.Players[owner].Board[chosen]`）。
那两件落地后，`AbilityPickOwner` 只需把 Enemy ⇒ `1-_me`、Friendly ⇒ `_me`、Any ⇒ 「两侧都进候选」（`CommandTargetSide`
目前只回一个侧，Any 需要 `HighlightTargetsCore` 那一层改成两侧合并 —— 一并列在这里，⛔ 别只改一半）。

**基线影响：0**（所有档的返回值与改前一致；新增的只有 `LogWarning`，且只在「选中一个 `Ability != null` 的单位并提交技能」时触发）。

### 十五·四 需要 `R` 的断言（`Editor/BattleScene.cs`）

| # | 断言 | 判别式（🧨 改坏法 ⇒ 红） |
|---|---|---|
| 1 | **只敌方那一档**（现池 12 行之一，如 `SW4`/`SOR6`/`AM68`/`SOR37` 这类「Deal N damage to an enemy」）⇒ `driver.AbilityPickSideForTest == 1`（Enemy）**且** `driver.CommandTargetSideForTest == 1 - MySeat`，且 `AbilitySideGapWarningsForTest` **不涨** | 把 `(1,0)` 那支写成 Friendly ⇒ 前一条红 |
| 2 | **友方那一档**（`AM69`）⇒ `AbilityPickSideForTest == 2`（Friendly）；**并且**（今天的现实）`CommandTargetSideForTest == 1 - MySeat`（退回现状）+ `AbilitySideGapWarningsForTest` **+1**（且再问一次**不再涨** = 「每张卡每类一次」） | 把 Friendly 直接映射成 `_me` ⇒ 第二条红（且会打开静默错打的口子）；删掉 `NoteAbilitySideGap` 那次调用 ⇒ 计数那条红 |
| 3 | **敌我双方 / 两侧都没标**⇒ 分别 `== 3` / `== 0`，且都 `CommandTargetSideForTest == 1 - MySeat` + 出声 +1 | `(1,1)` 映射成 Enemy（静默）⇒ 第一条红 |
| 4 | **判不出**（负例）：把 `UsesAlternativeAbility` 的输入弄成判不了（或直接 `AbilityPickSideForTest` 对一个 `Card == null` 的单位问）⇒ `== 0` + 出声 +1 + 仍回敌方 | 让它猜一个默认侧 ⇒ 红 |

⚠️ 断言里**别**写「候选已经含友方部队」—— 今天**做不到**（§十五·三），写了就是把引擎缺口伪装成已实现。
