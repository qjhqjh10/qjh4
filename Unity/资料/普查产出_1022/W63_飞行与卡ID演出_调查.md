# W63 · 飞行 / 卡ID→动画特效 / 血痕锚点（只读代理，2026-10-22）

> ⚠️ 调查期间工作区被别的代理改动 ⇒ 行号按现读、以符号名为准。

## A. 飞行单位「飞一下就不动」

**结论：不是被截断，是原版曲线本身的形状。** 但仍有 2 处结构差异。

判据：`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_2087276408211151808.json`
- `initialHeightOffset=0.4` · `flyingHeight=0.1` · `animationSpeed=0.35` · 曲线 4 键(0→0.00023 / .25→+1 / .75→−1 / 1→0) · **`m_PreInfinity=2`(Loop) / `m_PostInfinity=1`(Once)**。
- 四态机 `FlyingCardController__{Update,ChangeState,GoToTargetPosition,FlyingState,GetFlyPosition,Toggle}.c`，我们的 `Battle/FlyingCardController.cs` 逐句照抄（过渡 0.4 s）。
- **时间线（我方行，1 原版单位 = 182.14 px）**：0.4 s 抬升 0.4 单位（**72.9 px**）→ `_time += dt*0.35`，曲线一个往复 **2.857 s**、振幅 ±0.1 单位（**±9.1 px**）→ t≥1 因 `postWrap=Once` **永久停在 +0.4**。
  ⇒ 玩家看到「抬起来 → 抖一下 → 定住」。**⛔ 修法不是改成 Loop**（与原版不符）。

**与原版真正不同的两处（会加重「卡住」观感）**：
1. `OnFlyingToggleFinish` **零消费者**（`FlyingCardController.cs:90-92` 自认）。原版挂在 `Card3DAnimationController`：起飞完成 → `AnimFinished`（停 3D 体动画）/ 落地完成 → `StartAnim`；且 `DoAnim(clip, loop:true)` 且当前在飞时会先 `ToggleFlying(false)` 把单位降下来播完再飞回。**我们既没有 `Card3DAnimationController`，场上单位也只是静态 3D 卡体** ⇒ 原版「飞行单位一直在动」的那部分，我们结构上就没有。
2. 抬升 72.9 px vs 场卡屏幕高 218.4 px = **抬离 1/3 卡高并永久停住**（值是原版的，观感确实像「悬停/卡住」）。

**部署链路没有截断点**（逐段核过）：`OnPlayerDeployFlightStart` 0.3 s → `OnDeployHandoff` 0.4 s（只换父+摆位）→ `DeploySequence` 0.65 s → `RefreshAll→SyncBoard→SyncUnitFlying`（此时才 `Toggle(true)`）→ clip 0.7 `SetFace(Board)`。飞行控制器只写卡根 y，clip 只写 `body3D`/α/材质，互不相交；`TickFlyingUnits` 挂在 `AdvanceTimeline`。

**真机判别式（先跑再改代码）**：部署后 0.5 / 1.5 / 3.5 / 5.0 s 各打一次 `driver.FlyingOfForTest(v)` 的 `State` / `Progress` / `v.transform.localPosition.y`。
- `Progress` 到 ≈1.0 才停 ⇒ **原版行为，别改**；
- 只抬 0.4 就停 / `Progress` 不动 / y 被拉回地面 ⇒ 真缺陷，查 `animateFeel` 与 `TickFlyingUnits`。

## B. 卡 ID → 攻击/反击/受伤 动画与特效

**整条链**（逐跳现读）：卡 id = 原版 `uniqueId`（池内 1127 全部命中）→ 卡 SO 的 `cardAbilities[].abilityLogic[].cardAnimations[]` GUID + 顶层 `customRangedAttackParticles`/`customDeathParticles`/`summonAnimDelayed`/`remnantBodyPrefab` → `gen_card_vfx_by_card.py` 分桶 → `card_vfx_by_card.json`（**1098 卡 / 2694 条**）→ `VfxMap.FindCardRow` 按卡名回 id（1098 个名字全对得上）→ `PlaySignal`/`FireEventAll`/`PlayHitFeel`。

**逐事件对账**：
- `attack_ranged`：590 个远程攻击者 → **490 逐卡** · 63 走 `ByFaction` 4 家 · **37 走我们的 `BulletImpact_2shot`**。
  🔴 原版兜底是 `RangedAttackParticlesByClanSO__GetRangedClanParticles.c` 查不到就返回 SO 第一条（= Ultra 的 **`BulletImpact_2shot_trail`**）⇒ 我们播的是**另一件**（少了 `_trail`）。
- `attack_melee`：631 个近战攻击者 → 逐卡 **13**，其余 **618 播 `BulletImpactBurst_crowd`**（**原版根本没有通用近战件**，已记 `新账-65`）。
  且那 13 条**不是近战 VFX**：是 `UnitAttack(50)` 触发的**技能动画**（`TRIGGER_EVENT`），应走技能队列。
- `hit`：池内只有 **4 张**（1 条 authoritative = `BL20 Buff_DiabolicStrength`，其实也是 `UnitDamaged(60)` 技能动画；3 条 `inferred-name` 是按名字猜的）。其余 `Hit` 全走 `AttackHitSmall`。
  🔴 原版 `CardScript._ReceiveDamage…MoveNext.c` **不生成任何粒子**（只调 `BattleCardUI.DamageReceived`）⇒ 我们在**非攻击来源**的 `Hit`（技能伤害/疲劳/反噬）上多播一件 `AttackHitSmall`。
- **反击：我们完全没有演出。** `RuleCore.DeclareAttack` 里反击就是一句 `Hurt(...)`，只发一条 `Hit`。原版 `_ResolveAttack_d__438__MoveNext.c:1109-1145` 还有：① `CardScript.ReceiveAttackAnim(被打者)`；② **远程攻击时 `CardScript.ResolveAttackRangedAnim(防守方)`** —— 防守方用自己的远程攻击动画 + 自己的 `customRangedAttackParticles` 反击。
- 🔴 **引擎侧顺带查出**：我们反击伤害用 `counterAtk = target.Attack`（当前**近战**攻击力），原版 `GetCombatDamage(defender, attackType)` ⇒ **远程攻击引发的反击取 `CurrentRangeAttack`**（`EntityScript__GetCombatDamage.c:19-24`）。
- **卡 ID 对应结论**：链路真的按卡 id 对上；「对不上」的是**语义分桶**（`UnitAttack`/`UnitDamaged` 被当成攻击/受击特效）、以及**近战/反击两档没有逐卡数据可用**（原版判据就是空的）。另：池内 **29 张**原始 SO 的 `cardAnimations` 全空 GUID（运行时辰 1098 = 1127 − 29）。

## C. 血痕在卡牌上面

- 血痕 = `CardPrefab/3DBody/DamageCounter/DamageIcon`（sprite `Card Damage`），**不是 VFX**，走 `Battle/PopNumber.cs`。
- 原版真值：`DamageCounter` localPos **(0,1.708,0)** · `DamageIcon` (0.094,−0.104,−0.05) scale 1.01665（clip 0.5763→0.7）· `DamageText` (0,0,−0.05) scale 1.54。`3DBody` 原点 = 3D 体底边。
- 换算：卡根空间体底 = **−1.6550**、体中线 = `ArenaSlots.BodyMidOffset` = **−0.34335** ⇒ 原版 `DamageCounter` 卡根 **y = +0.053**；飘字锚 = 体中线 ⇒ **原版偏移 = +0.396 卡单位 = 我方 26.0 px / 敌方 23.6 px**（卡高 218.4 px）。
- 我们 HEAD：`PopOffset.y = ToOurs(0.35) = 0.590`，注释写「我们挑的 / 原版查不到」——**不成立**。实际我方 **63.7 px**，比原版高 **37.8 px**；3D 卡体顶边在体中线 +0.968 卡单位 = 63.5 px ⇒ **锚点正落在卡体上沿**。
- ✅ **工作区已修**（代理 S）：`PopOffset.y = (1.708 − Height/2 − BodyMidOffset) × CardLocalToOurs` = **0.3857 卡单位 ≈ 0.2342 世界 ≈ 25.3 px**。代理独立算 0.396 卡单位 ⇒ 差 0.7 px，**改法是对的**。`z=-0.4` 保留（HUD 排序用）。

## D. 与现有账的关系

- A：**新账**（单立「飞行单位的 body 动画与推进回调未接」，不要混进 65）。
- B：属 `新账-65` 的：近战共享兜底件 · 远程时长半数无逐卡真值 · `DurationOf(Hit)=0.75`。**新账 3 条**：① 反击无演出（且引擎反击用近战攻击力）② 非攻击来源的 `Hit` 多播 `AttackHitSmall` ③ `hit`/`attack_melee` 桶其实是 `UnitDamaged`/`UnitAttack` 技能动画（语义混用）。
- C：**新账**（`新账-65` 只记了 scale，没记锚点）—— 已在修，核销 + 补断言。

## E. 建议改动清单

| 文件 | 位置 | 改什么 |
|---|---|---|
| `Battle/VfxMap.cs` | `ByEvent[AttackMelee]` | 删掉 `BulletImpactBurst_crowd`（原版无通用近战件）；同步改 `BattleScene` 里引用该名字的断言 |
| `Battle/VfxMap.cs` | `ByEvent[AttackRanged]` | `BulletImpact_2shot` → **`BulletImpact_2shot_trail`** |
| `Battle/BattleDriver.cs` | `PlaySignal` 的 `else if (!hitOfAttack) FireEventAll(...)` | 非攻击来源的 `Hit` 不再播 `VfxMap.Hit` |
| `Battle/VfxMap.cs` + `工具/gen_card_vfx_by_card.py` | `TRIGGER_EVENT` | 把 `UnitAttack`/`UnitDamaged` 从 `attack_melee`/`hit` 拆出去（改归 `ability` 或新桶）——**需要 body 动画路径，否则会静默不播** |
| `Battle/BattleDriver.cs` | `ScheduleAttackPresentation` 之后 | 反击：引擎多带一条「反击」事件，表现层播守方 `ResolveAttackRangedAnim` 那发 |
| `RuleEngine/Core/RuleCore.cs` | 反击段 `int counterAtk = target.Attack;` | 远程攻击时取 `target.Range`（原版 `GetCombatDamage`）——先核 `UnitState` 有无 `Range` |
| `Battle/BattleDriver.cs` | `PopOffset` | 已修；注释已订正 |
| `Battle/FlyingCardController.cs` | — | 不求改曲线；真机 Progress 不推进再查。**不要**改 Loop |

## F. 断言建议

1. `PopOffset.y` 原版靶：`0.39 卡单位 × CardLocalToOurs = 0.237±0.02 世界`；**灭自证**：断言「血痕锚点 ≤ `CardCenter + 0.6×体半高`」（旧值 0.97 卡单位 = 0.59 世界会红）。
2. 反击：造一次**远程**攻击（目标有反击值），断「守方 `CustomRangedAttackParticles` 被播」+「守方锚点为起点」。
3. 逐卡覆盖：断 `VfxMap.ResolveAll("attack_ranged", faction, 卡名)` 对 490 张有 `customRangedAttackParticles` 的卡逐张命中、且不再回 `ByEvent`。
4. 飞行：断 `Progress` 在 3.2 s 后 ≥0.99 且 `y - home.y == 0.4±0.01`（把「一次往复后停」钉死，⛔ 别断「一直在动」）。

## G. 没查清 / 需要真机

- **A 的「卡住」到底是哪一帧**：静态能证明曲线只走一轮，证明不了用户看到的是「2.9 s 后停」还是「0.4 s 就停」。需真机探针。
- `Card3DAnimationController.DoAnim` 的 `param_3(loop)` 由谁传、哪些卡走「落地播动画再飞回」——`DoBodyAnimation.c` 反编译腔读不出实参，**没查清**。
- 原版**近战**是否还有别的逐卡件：`AlternateArtCard.attackAnim` 1195 张里 2 张且都是空引用 ⇒ 判据为空（2022 快照口径）。
- 反击的守方远程反击是否**也**播 `AttackHitSmall` / 命中音：`_ResolveAttack` 协程 1652 行，只逐段读了 1030–1160 与反击入口，**没通读**。
- `PopOffset.z=-0.4` 是否影响投影对齐（HUD 正交相机下应无缩放影响）——未实测。
