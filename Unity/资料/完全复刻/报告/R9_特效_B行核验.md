# R9 · 特效动画 B 行核验（第八十七会话 · 2026-10-27）

**范围**：`账/特效动画.tsv` 里 `状态=B` 的 **48 行**（现读，逐行核完，无遗漏）。
**产物**：`账/_parts/B87_R9_特效B核验.tsv`（48 行，11 列，表头逐字照主表；`轮次=B87-R9`）。
**口径**：只改账里已有的行；判据优先 `d:/2/tools/decomp_full/*.c`（先看首行函数名是不是自己），ILSpy 交叉；我们这侧的行号**一律按符号名现读**（`BattleDriver.cs` 正在被 W1 改，原账多处已漂移）。

---

## ① 统计

| 状态 | 条数 | 含义 |
|---|---|---|
| **A** | **33** | 已实现且语义一致（落点已换成**现读**行号） |
| **B** | **11** | 仍待核（每条都写清了「还差什么」） |
| **C** | **3** | 真缺口（都带等级：P2 / P2 / P1） |
| **D** | **1** | 原版空转 + 全树 0 调用点（铁律 11 例外①） |

**B 的 11 条**（都不是「没做」，而是差一层/差一个裁定）：`CardEffectItem.LoadEffect` · `CardEffectsService.EnableEffects` · `HandEffect.DeepCopy` · `SegmentationController.AddLocalAttributes`(P3 边界) · `AbilityLogic.PlayAnim` · `AiScripted.ShowPlayerActionAnim` · `BattleManager.AddCardAnim` · `BattleManager.ResolveAnimInfoRangeAttack` · `BattleManager.ResolveTraitAnim` · `CardScript.CanContinueAnim` · `CardScript.ResolveAttackAnimationEffects`。

---

## ② 最重要的 3 条 C（各一句话）

1. **`CardScript.ActivateTraitParticlesFromCodeInTarget`（P1）** —— 原版这一支的**唯一**调用方是「带猎杀标记的小兵被打死」（`CardScript__TriggerOnMinionDeath.c:77`，trait `0x4ec` = **1260 = huntMark**，本机 `trait_collection.json` 现读），它把 `HuntMark_DamageEffect` 播在**死者**身上；我们的唯一粒子口是「按事件关键词、恒挂**触发者自己**」，而引擎那条 huntMark 死亡支（`RuleCore.cs:9722`）**根本不发 `EvtKind.Trigger`** ⇒ 这件粒子今天既不播、也无处可播。
2. **`BattleManager.ResolveBloodThirstAnim`（P2）** —— 四条闸我们已对齐（`HasCurrentTrait(0xdc)` ∧ 本回合第 1 次 ∧ 行动侧 ∧ `cardState != 5`，最后一条无等价物），但演出前半我们用的是**通用徽标脉冲（`DOScale` 单次）**，原版是 `BattleCardUI(+0x268 那枚 SpriteRenderer).DOFade(to 0.3, 0.15s) + SetLoops(6, Yoyo) + Ease(22)`（两个 `_DAT_` 现读 0.3 / 0.15）。
3. **`SwarmVFXController.Initialize`（P2）** —— 三处未复刻：① 材质两格贴图（卡插图 + 按 `CardTier` 的 MatCap）**判据其实在本地**（`CardMaterialHelper__ConfigureMaterial.c:23-58`，代码注释那句「查不到判据」不成立）② 藏「被并掉那张」的 3D 卡身 ③ 原版**动画演完才移除**被并掉那张（`onFinish` 闭包），我们结算当拍就移除。

---

## ③ 还差什么（按可判性排序，给调度台排活）

| # | 差什么 | 判据/入口 | 卡在哪 |
|---|---|---|---|
| 1 | `AbilityTargetList` 的**表现层消费者**（每拍 × 全部目标） | `EffectResolver`/`RuleCore.AttachAbilityTargets` 已发射；`grep AbilityTargetList` 只在 `RuleEngine/` | 纯表现层活，判据齐 |
| 2 | 命中音**三档**（`preset+0x28/+0x30/+0x38`，按 `GetUnitSize`）+ 命中粒子**两档**（`+0x1b8`/`+0x1c0`） | `CardScript__ResolveAttackAnimationEffects.c:26-58` | 需要三档 AudioCue 的资产对位（`_DAT_`/字段已有） |
| 3 | `UnitTweenSO` 的长度进**队尾**（`animInfo+0x80`，`+0x18` 为真才等） | `_d__468:96-118` | 数据侧 `tweenRows` 已解出长度 |
| 4 | `ResolveTraitAnim` 的 ②~⑥ 层（全局闸 `singleton+0xd8`、`ReplaySkipAnims` 跳延迟、`startDelay`、`DelayedVFXPlay` 锚点、`shk`、`ush`） | `_d__465:30-311` | 需先统计「trait 的 CardAnim 有几条带非 0 的 `sd/shk/ush`」——若全 0 则无可观测差 |
| 5 | `CanContinueAnim` 收一条**共用判定**（含 `activeSelf`）替换各拍的 `ViewAt == null` | `CardScript__CanContinueAnim.c:1-37`（11 个调用点） | 纯表现层，判据齐 |
| 6 | `LoadEffect` 的 `isPlayer` 侧可见性掩码（`+0x90`/`+0x91`） | `CardEffectItem__LoadEffect.c:36-120` | 我们的 `TempBuff` **没有这一位** ⇒ 要先定「对手施加的限时增益该不该显示」 |
| 7 | 血渴那一下的 6 次 yoyo 淡出 | `BattleCardUI__HighlightBloodThirst.c:19-39` | 判据齐（0.3 / 0.15 / 6 / Ease22） |
| 8 | 虫群合并：材质两格 · 藏体 · 销毁时相 | `SwarmVFXController__Initialize.c` + `CardMaterialHelper__ConfigureMaterial.c` | ②③ 要先让引擎把「被并掉那张」带到表现层（`BattleEvent` 今天只带存活那格） |
| 9 | `SegmentationController.AddLocalAttributes`（P3 服务端派生 + 上报） | `.c:40-60+` | **P3 ⇒ 建议调度台按 §四 判 D**（本轮保持 B，只把证据写全） |
| 10 | `HandEffect.DeepCopy` 的「模板不被就地改写」**断言** | `AbilityLogic__DeepCopy.c:81` | 判据齐，只缺一条回归断言 |

---

## ④ 顺手发现（只报不改；其中 5 条是文档/账**记错**，按铁律 5 应当就地改）

### ④-A 记录与事实不符（5 条，建议调度台就地订正）

1. **`CardPresentation/Shell/CardDetailPopup.cs:2837` 一带**写「原版 `UpgradePanel.TryUpgrade` 的方法体**没有产物**（`decomp_full` 0 命中）」—— **不成立**：`d:/2/tools/decomp_full/UpgradePanel__TryUpgrade.c` 存在（**105 行**），能逐句读出：`ResetDisplayOrder` → `DoAnim` → 记新 tier（`+0xc4`）→ **`+0xc0 = 2`（pending 计数）** → `SingletonBehaviour.Instance.<+0x20>.enabled = false`（**关输入**）→ PlayFab 云函数 `0x385`（`OnSuccess/OnFail` 各自再 `pending -= 1`）。⇒「动画期间关输入」**不是结构性等价，是有判据的**；而且解锁要**两条腿都完成**（动画 + 云函数），我们只有动画那一条（云函数 = P3）。
2. **账 484 的「发射口 = `RuleCore.EmitAbilityEvent`」** —— **这个方法不存在**（`grep EmitAbilityEvent` 全仓 0 命中）。真身 = `RuleCore.AttachAbilityTargets`（`RuleCore.cs:15127`，紧跟 `ctx.Emit(EvtKind.Ability, …)` 之后调；两个调用点 = `UseAbilityCore` / `UseAlternativeCore`）+ `AbilityTargetsFor`（`:15149`）。`RuleEngine/Core/BattleEvent.cs:755` 的 doc 也引用了那个不存在的名字 ⇒ 一并待改。
3. **`BattleDriver.cs` 的虫群合并注释**写「材质交接…查不到判据，不换」—— **判据在本地**：`CardMaterialHelper__ConfigureMaterial.c`（`mat.SetTexture(卡插图) ; mat.SetTexture(matCapByTier[clamp(CardTier,0,count-1)])`）。
4. **`BattleDriver.cs:13805`** 写「`0x4b0` 的具体 trait 语义**未核**」—— **已结案**：`0x4b0` = **1200 = `ambush`**（`d:/4/Unity/数据/游戏数据/trait_collection.json`，`definedTrait 1200` → `ambush`，`traitEffects` 只有一条 `End(1) → Ambush Effect`）。同一常量在 `HasToUseLegendarySummonEffect` 里就是「不是伏击」。
5. **`BattleDriver.cs:21504`** 写「档位（`SupportMethods.GetUnitSize`）只影响**震动与粒子**」—— **不成立**：`CardScript__ResolveAttackAnimationEffects.c:26-58` 按 size 分三支，**音效也是三档**（`preset+0x28` / `+0x30` / `+0x38`），只有粒子是两档（`+0x1b8` / `+0x1c0`）。

### ④-B 账的判据出处错配（2 条）

6. **账 522 `RemnantBody.GetDeathParticlePosition`** 只引 `cpp2il/…/RemnantBody.cs:51（abstract）` —— 那是**声明、无体**。真身 = `RemnantAeldari__GetDeathParticlePosition.c`（`this+0x68` = `wayStoneIdle` 的 position）与 `RemnantNecrons__…c`（自身 `transform.position`）；且**死灵那一档在原版即不可达**（唯一调用方是 `RemnantAeldari__CollectWaystoneEffect` 的虚拟槽 `+0x1d8`，只被 `RemnantAeldari__DoDestroy` 调）。补丁里已把出处改掉。
7. **账 519 `CardScript.TutorialIconAnim`** 的「等价物 = 教学标注四块」是**误配**：`.c` 体只有 `yield WaitForSeconds(0.7)`（`DAT_1834b318c` 现读 4B float = 0.7），ILSpy 工厂 `CardScript.cs:11892` 只 `return new _003CTutorialIconAnim_003Ed__319(0)`，状态机类无任何卡引用字段，**全树按名 grep 只命中它自己 3 个文件**（无字符串型 `StartCoroutine("TutorialIconAnim")`）⇒ 判 **D**。

### ④-C 结构性观察（不是缺陷，但影响后续判据）

- **`CardEffectsService.*` 三个方法在 `decomp_full` 全是 0 调用点**：真正的消费者是 `CardDisplayWindow__DisplayCardEffects`（把 `EnableEffects` 的循环**内联**了一份，并多出两跳：kind 过滤 `+0x38 ∈ {6,0x23,0x1e}`/`0x10 且无任务`、以及「环境效果卡强制不显示整组」）。⇒ 判「有没有这条行为」时**别只看 `CardEffectsService`**。
- **`AnimAnchors` 那 10 档能量锚没挂，但今天无可观测差**：`cardanim_clips.json` 1,067 行的 `sp`/`ep`/`pt` 值域只有 `0/1/2/3/4/5/12/13/14/16/17/18/19/50/155`，**一次都没用到** `60/61/70/71/80/90/100/110/120/130`（`8`/`10` 原版自己就是死档）。
- **`CardDisplayLevelUpAnimationController__FadeOutImages.c` 首行是 `…__OnEnable`** —— 是 **ICF 同码折叠**，不是「文件名与内容不符」那一类；两文件逐字相同。
- **`CardEffectItem__Disable.c` 首行是 `BattleAlliancePanel__CloseButtonClick`** —— 同为 ICF 折叠（两边都是 `SetActive(false)`）。
- **`CardEffectItem.IsActive` 全树 0 调用点**（只命中它自己 + 三个 ICF 同址别名）⇒ 别为它造方法。
- **`UpgradePanel.AnimationFinished` 的解锁是计数**（`pending` 从 `TryUpgrade` 的 2 递减到 <1 才 `enabled = true`），不是布尔；`CheckFinish` / `OnSuccess` / `OnFail` 也各减 1（4 个扣减点共用同一格）。

---

## 核验方法与边界（本代理做了/没做）

- **做了**：48 行逐行判；`decomp_full` 亲读的 `.c` 清单 = `CardDisplayLevelUp*`(9) · `CardEffectItem*`(5) · `CardEffectsService*`(3) · `CardDisplayWindow__DisplayCardEffects` · `CardEffect/HandEffect*`(3) · `SegmentationController*`(3) · `SwarmVFXController*`(2) · `AnimFxModuleMoveParticlesToTarget__Initialize` · `CameraShakerManager__DoSimpleShake` · `CardVerticalFramer`(ILSpy) · `CardAnimController__Initialize/Update/RotateAnim` · `BattleManager__AddCardAnim/RemoveVoiceLinesControllerListeners/ReplaySkipAnims/ResolveBloodThirstAnim/ResolveAnimAnimationClip(_d__473)` · `_d__468` · `_d__465` · `CardScript__ActivateIconAnim/ActivateTriggerTraitAnim/DoBodyAnimation/HasToUseLegendarySummonEffect/CanContinueAnim/ActivateTraitParticlesFromCodeInTarget/ResolveAttackAnimationEffects/_ReturnToDeckAnim_d__248/HighlightBloodThirst/ActivateBloodThirst/TutorialIconAnim(_d__319)` · `BattleCardUI__HighlightTraitIcon/HighlightBloodThirst/DisplayTriggerAnim` · `RemnantAeldari/Necrons__GetDeathParticlePosition` · `CardMaterialHelper__ConfigureMaterial` · `UpgradePanel__TryUpgrade/AnimationFinished/ChangeFrameOnLevelUp/PlayAnim` · `EntityScript__GetCardEffectsToShow`。`read_literal` 现读 6 个常量（0.7 / 0.3 / 0.15 / 5.0 / 0.7 / 2.0 / 1.0 一类）。
- **没做**（如实记）：⛔ 没跑 Unity、没开真机（本波禁）；`ush` 那 257 行**没按「trait 件 / 攻击件」分组**；`PlayerHand__UpdateCardEffects` 的扣次点没读（`HandEffect.GetNewInstanceOfThisEffect` 的「每份 clone 各自扣次还是共扣」未核）；`BattleManager__AddAutoActionToQueue` 的道消费序没读；`singleton+0xd8` 那个全局开关的默认值没解。
