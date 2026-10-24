# AN1 · 修复清单**骨架**（按根因分组 · 2026-10-24）

> 只读审查代理 **AN1** 产出。**这是给修复代理直接照着做的单子**：每条 = 根因 · 影响面（现读数字）· 判据 · 修法 · 落点 `文件:行号` · 验收 · 建议写手 · 依赖/风险。
> 方案 → `AN1_动画特效逐卡审查_方案.md`；试点 → `AN1_试点_Sautekh.md`；复算脚本 → `资料/普查产出_1024c/scripts/`。
> 数字**全部现读**（2026-10-24 的卡池与代码）；⛔ 改完数据/卡池后这些数字会变，**重跑脚本、别手抄**。
> 严重度：**S0** 内容丢失（该播的没播 / 播成别的件）· **S1** 时机/落点/空间错 · **S2** 参数/观感 · **S3** 待判据。

---

## 〇 怎么用这份单子（给修复代理）

1. **先读** `AN1_动画特效逐卡审查_方案.md` §二（维度表）与 §五（切块），再动手。
2. **一条根因 = 一个写手**（见 §3 的切块表）；**同一文件同一时刻只有一个写手**（铁律 13·3）。
3. 每条做完**必须**在 `验收` 栏那种方式验：能静态断言的写断言（**灭自证**：见 §5），只能真机的写进 `资料/真Play待验清单.md`。
4. ⛔ **不许**「影响小就不做」（铁律 11）；只有 §4 的 A 类（原版本身没有 / 判据为空 / 用户拍板）可以不做，且要写清理由。
5. ⛔ **不许**跑 Unity 批处理（全局串行，只有主对话能跑）；改完 `.cs` 先跑 `bash 工具/typecheck.sh`。

---

## 一 总表（16 条；按「先做什么」排序）

| # | 严重度 | 根因一句话 | 影响面（现读） | 主落点 | 建议写手 |
|---|---|---|---|---|---|
| **R1** | **S0** | `EvtKind.Play` + `Slot<0`（未指定目标的战术/防御）⇒ 表现层把整条事件丢掉 | **405 行 / 269 卡 / 13 阵营**（Sautekh 37 行 / 22 卡） | `Battle/BattleDriver.cs:19510` + `:20404` | **W1** |
| **R2** | **S1** | `play` / `trigger` / `death` 桶只给「一个点」⇒ `sp/ep/pt/mv/ease` 全不消费 | `play` **270 行带参数**；`trigger` 9；`death` 3；`attack_melee` 6（全池） | `Battle/BattleDriver.cs:19497-19600` · `:19685` | **W1** |
| **R3** | **S1** | `ability` 桶里 `tg=-1` 但不在 `CardAnimOrder`（62 卡表）⇒ 单点播 | **59 行 / 31 卡** | `Battle/BattleDriver.cs:16387` · `Battle/CardAnimOrder.cs:316` | **W3**（数据/表）+ **W1**（调用） |
| **R4** | **S1** | 生成器把「未归口触发」一律塞进 `ability` 桶 | **456 行 / 274 卡**（其中 **30 行 / 19 卡** 有「在能力拍上错播」风险） | `工具/gen_card_vfx_by_card.py:229-238` | **W2** |
| **R5** | **S1** | `trigger` 桶名字未放行 ⇒ 整桶播（可能带出别的拍的件） | **36 行 / 32 卡** | `Core/VfxMap.cs:2233` · `:2583` | **W4** |
| **R6** | **S0** | 死亡触发的能力动画（`ThisUnitDeath(80)` 等）没有播放拍 | **7 行 / 6 卡**（`AM34`(180) · `BL62` · `BL66` · `GOF3`×2 · `GOF83` · `SAU72`，其余全是 80） | `Core/CardFeel.cs:2509` · `RuleEngine/Core/RuleCore.cs`（新发射口） | **W4** + 引擎线 |
| **R7** | **S2** | `animInfo.screenShake` 整字段零消费 | **57 个 CardAnim → 92 行 / 71 卡**（Sautekh 6 行 / 4 卡） | `Battle/CardAnimClips.cs:120-137` · `Battle/BattleDriver.cs:16343` | **W3** |
| **R8** | **S1** | trait 行可能被 `VfxMap`（整桶）与 `TraitFrames` **各播一次** | 与 R5 同集合（36 行 / 32 卡） | `Core/VfxMap.cs:2730` · `Core/TraitFrames.cs` | **W4** |
| **R9** | **S3** | `talent` / `regeneration` 等 trait 触发框**引擎无对应触发事件** | `talent` **96 行 / 96 卡** · `regeneration` **9 行 / 9 卡**（另 8 个 trait 同形状） | `Core/TraitFrames.cs:723` · `RuleEngine/Core/ActionPump.cs:392` | **查判据代理 → 再派** |
| **R10** | **S1** | 死亡爆散体是**根特效 + 竞技场坐标** ⇒ 由 HUD 正交相机画 | 全池**每张单位阵亡**（Sautekh 67 行） | `Core/CardFeel.cs:2619/2635` | **W5** |
| **R11** | **S2** | `MeleeEndPos dy=0` 的「没有等价物」注释已过期；参考系未统一 | 全池近战 | `Core/CardFeel.cs:916/922/726/3237` · `Core/CardView.cs:1811/1821` | **W5** |
| **R12** | **S2** | 「有 `activeAbilityLogic` 但 `animTimes` 无 `tg=-1` 行」的 7 张卡：能力拍用 1.0 s 兜底 + 播未归口件 | **7 卡**（`SW9 / ASH74 / GOF77 / SOR66 / SOR70 / SOR34 / SOR23`） | `Battle/BattleDriver.cs:16387` + 卡表生成器 | **W3** + 卡牌线 |
| **R13** | **S2** | `animInfo.minionMovementInfo` 零消费（3D 卡体自己上移 3.0 / 旋转） | **9 个 CardAnim → 10 行 / 8 卡** | `Battle/Card3DAnimationController.cs` · `Battle/CardAnimClips.cs` | **W3** |
| **R14** | **S3** | `animInfo.startDelay` 零消费 | **1 个 CardAnim**（`Cuts Brutal Warp Spider`，0.5 s）→ 1 行 | `Battle/CardAnimClips.cs` | **W3** |
| **R15** | **S3** | `trait_effect_bindings.json` 的 `conventions.overrides` **文档过期**（运行期已按阵营取件）；且 `army` 取的是**拥有者**阵营 | 10 条覆盖（`shield` 4 · `invulnerable` 6） | `Resources/trait_effect_bindings.json`（文档）· `Core/TraitFrames.cs:542` | **W4**（铁律 5 订正） |
| **R16** | **S2** | 同一 trait 在多条 `CardAbility` 里各出现一次时的**归属**（`Order` 三段）只覆盖了 3 行特例 | 3 行（`UM37`/`UM84`/`SW18`，已有 `A2` 的改法） | `Core/VfxMap.cs:2655` | **W4**（复核，不重做） |

> **结论类（不是缺陷，别再开账）**：素材真伪 **0 件自造 / 0 件错落点**；音效 cue / wav / 逐卡语音 **0 缺**；`easeCurve` / `orientToVelocity` / `applyMovementOffsets` / `waitAnimToApplyEffect` / `tweenAnim` 已消费；`AbilityAnimDurations` 对真·主动能力 **93/93**。

---

## 二 逐条展开

### R1 · 未指定目标的战术/防御卡：`play` 桶整条事件被丢

- **根因（读代码得出，不是推断）**：
  - 引擎：`EffectResolver.PlayTactic` 发 `EvtKind.Play` 时 `Slot = targetSlot`，**没指定目标就保持 `-1`**（那一大段注释写的是「照原版记录字段」——正确）。
  - 表现层：`BattleDriver.PlaySignal` 在 `int slot = … e.Slot; if (slot < 0) return;`（`:19510`）把 `Slot<0` 当成「不在场上」⇒ 后面的 `FireEventAll(evt=play, …)` 一次都到不了。
  - 唯一豁免是 `IsHudAnchoredEvent`（`:20404`）= 资源三件 + `Draw`，**不含 `Play`**。
- **影响面**：全池 **405 行 / 269 卡**（`type ∈ {tactic,defence} && targeted==0` 且有 `play` 行）；试点 Sautekh **37 行 / 22 卡**（清单 → `AN1_试点_Sautekh.md` §2·2）。
- **判据**：原版 `CardScript._PlayPlayerCard/_PlayEnemyCard` 对非单位卡走 `DisplayEnemyCardInCenter`；卡的 `cardAnimations[]`（trigger `ThisCardPlayed(0)` 那批）由 `AbilityLogic.PlayAnim` 播，锚点用 `GetPos`（`thisCard.GetEffectAnchor()` = `2DCard` 那一级，因为卡此刻在 `state 10 inHandPlaying`）。
- **修法（建议）**：
  1. `PlaySignal` 对 `EvtKind.Play` 且 `slot < 0` **不再 return**：把这条事件交给「按拍播」那条出口（见 R2 的同一个改造），落点由 `AnimAnchors` 解析（`sp` 多为 0/2/3/5/14，见试点 §2·3）。
  2. `thisCard` 视图 = 中央展示的那张临时 `CardView`（`MaybeDisplayPlayedCardInCenter` 已经建了一张，`BattleDriver.cs:21600` 一带）——**复用那一张**，别另建。⚠️ 那张卡只在 `animateFeel` 为真时建（`:19315`）⇒ 自检/批处理档没有它：那一档落点退回 **`createCardPos(140)` / `stratagemGeneralShowCase(145)`** 那两个 HUD 锚（`AnimAnchors` 已实现），断言要**两档各断一条**。
  3. `MaybeDisplayPlayedCardInCenter` 与特效的先后：原版是同一协程（先摆正/翻面、再播动画）⇒ 顺序要对齐（见 F1）。
- **落点**：`CardPresentation/Battle/BattleDriver.cs:19510`（闸）· `:19315`（中央展示那一句）· `:21600`（临时卡视图的持有者）· `:19497-19590`（落点分支）。
- **验收**：
  - 断言（`Editor/BattleScene.cs`）：造一条 `EvtKind.Play`（`Slot = -1`，`CardId` = 一张无目标战术），`StepOne` → `FiredCount > 0` 且 `FiredPosOf == 该战术 CardAnim 的第一个 sp 锚点`；**灭自证**：把 `:19510` 的 `return` 恢复 ⇒ 断言必须红。
  - 真机：打一张无目标战术（如 `Spyder Nest` / `Tomb World`）→ 看召唤法阵/增益件真的出现。
- **风险**：`Slot=-1` 还可能是「从牌库直接进弃牌堆」那类（`:19510` 注释举的例子）⇒ 必须**只对 `EvtKind.Play` 且卡是非单位卡**放行，别把别的 `Slot=-1` 事件放进来。
- **依赖**：R2（同一处代码；建议同一个写手一起改）。

### R2 · `play` / `trigger` / `death` 桶不消费逐拍参数（锚点 / 位移 / 朝向 / 空间）

- **根因**：只有两条路消费 `AnimInfo` 的位置参数 —— `ability` 的 `ExpandAbilityBeats`→`PlayAbilityBeat`，以及 `attack_ranged` 的 `ResolveAttackPieceAnchors`。其余桶走 `FireEventAll(evt, 一个点, …)`（`:19585`）：`ResolveAllTimed` 只给名字 + 时移 `T`，**锚点上下文根本没传**。
- **影响面**：全池 `play` **270 行带参数**（`move` 132 / `orient` 68 / `start!=end` 47 / `summonSpot` 68 / `hudAnchor` 33）· `trigger` 9 · `death` 3 · `attack_melee` 6。试点 Sautekh 13 行（§2·3）。
- **判据**：`AbilityLogic.PlayAnim` 对**任何** trigger 的动画都走 `GetPos(start/end/pointTowards)` → `BattleManager.AddCardAnim` → `CreateAnim`（挂在锚点下）→ `CardAnimController.Update`（位移/朝向）。没有「只有 activeAbility 才用锚点」的分支。
- **修法（建议）**：把 `ability` 那条「按拍播」的能力**抽成一个可复用的口**（`PlayAbilityBeat` 已经具备：锚点/终锚/朝向/载体/位移泵），`play` / `trigger` / `death(非粒子)` 的逐行改成同一口喂参数；单行（1 拍）时行为必须与今天**逐字相同**（`T=0`、`Duration=0` 的回归口径不许动）。
- **落点**：`Battle/BattleDriver.cs:19497-19600`（三个 `FireEventAll` 调用点）· `:19685`（`PlayAbilityBeat`）· `:20236`（`PlayUnitSkillAnim`，同一族的现成用法）。
- **验收**：
  - 断言：对每类桶挑 1 张卡（Sautekh 用 `SAU16` 的 `sp=14` / `SAU17` 的 `mv=1 tm=0.4` / `SAU65` 的远程件出现在 play 桶）→ 断 `FiredParentLayerOf == ArenaLayer` 且 `FiredPosOf == 锚点世界位置`。
  - **灭自证**：把新口改回 `FireEventAll(evt, pos)` ⇒ 三条必须红。
  - 真机：`Night Scythe` 的召唤法阵应出现在**下个空位**而不是卡中心。
- **风险**：`PlayAbilityBeat` 会**建载体**（空 GameObject）——根特效（抽卡/资源那类 HUD 锚）**不该**走它，否则特效会被挂到 HUD 锚下、层也跟着变。⇒ 只对「能解析出竞技场锚点」的档启用。

### R3 · 31 张「有主动能力动画但不在拍表」的卡

- **根因**：`CardAnimOrder` 的运行期表来自 `Resources/card_anim_order.json`（**62 卡 / 99 拍**，由 2022 快照过滤而来）；而 `cards_engine.animTimes` 现读有 **93 卡**带 `tg=-1` 行 ⇒ 其中 **31 卡**在 `ExpandAbilityBeats` 里查不到 ⇒ 走「原样一拍」。
- **影响面**：**59 行 / 31 卡**（脚本：`an1_bucket_matrix.py` 的「单点播」那两行）。
- **判据**：`cards_engine.animTimes` 的 `tg==-1` 行就是**当前 build** 的 `activeAbility.activeAbilityLogic[].cardAnimations[]`（`A11`/`B17` 已交叉核过 93/93）。
- **修法**：`CardAnimOrder.TryGetBeats` 命中不到时，**用 `CardDef.AnimTimes` 里 `tg==-1` 的行现组拍**（`a/n/sp/ep/pt/…` 都在；`Order` 三段也在）。⛔ 不要重生成 `card_anim_order.json` 完事——那会让两个数据源继续分叉。
- **落点**：`Battle/CardAnimOrder.cs:316`（`TryGetBeats`）· `Battle/BattleDriver.cs:16387`（`ExpandAbilityBeats`，`ViewAt`/`BeatSlotGate` 已有）。
- **验收**：`BattleScene` 断言「凡 `AnimTimes` 有 `tg=-1` 行的卡，`ExpandAbilityBeats` 命中数 ≥ 1」；**灭自证**：断 `TryGetBeats` 回 0 ⇒ 红。真机抽 2 张（`DA65` / `UM101`）。
- **依赖**：R12（同一段代码）。

### R4 · 生成器把「未归口触发」塞进 `ability` 桶

- **根因**：`工具/gen_card_vfx_by_card.py` 的 `trigger_event()`（`:229-238`）只把 7 个 `AbilityTrigger` 名归口（`ThisCardPlayed/Strike/Slay/ThisUnitDeath/UnitDeathByAbility/UnitDamaged/UnitAttack`），其余**一律 `EV_ABILITY`**（注释写「宁可少播、不可错播」——方向对，但 `ability` 桶是会被 `EvtKind.Ability` 播的，所以「不会错播」这个前提只在卡没有主动能力时成立）。
- **影响面**：**456 行 / 274 卡**；其中 **30 行 / 19 卡**的卡**有** `activeAbilityLogic` ⇒ 真的会在能力拍上播错（19 卡名单：`AM21/AM69` · `DA14/DA29/DA65` · `GOF77` · `ASH74` · `SOR4/23/26/34/37/66/67/70/71/72` · `SW9` · `UM96`；见 `data/an1_bucket_matrix.tsv` 过滤 `风险：在能力拍上播`）。
- **判据**：`CardAbility.trigger` 是**时机**；「未归口」= 我们引擎没发那个事件，不等于「这个时机不存在」。正确处理 = ①能映射到某个真实事件就映射；②映射不了 → **单独放一个 `unmapped` 桶**（**不参与任何事件的整桶播**，只留档），并在表现层出声。
- **修法（建议）**：
  1. 生成器新增桶 `trigger_unmapped`（或 `ability_trigger_only`），把「`trigger != -1` 且不在映射表」的行写进去；
  2. 运行期 `VfxMap` 不把它并进 `ability`（`ResolveAllTimed` / `TryResolveTriggerPicks` 只在**明确知道 trigger 名**时才按名取它 —— 那样 routed 的那些仍能播）；
  3. 对 7 张「有主动能力但无 `tg=-1`」的卡（R12）单列，别靠「未归口」凑。
- **落点**：`工具/gen_card_vfx_by_card.py:229-238` + `:1275`（`TRAIT_TRIGGER_EVENT`）· 运行期 `Core/VfxMap.cs:2068`（`ResolvePerCard`）· `:2730`（`TryResolveTriggerPicks`）。
- **验收**：生成器 `--check` 通过；`data/an1_bucket_matrix.py` 里 `风险：在能力拍上播` = 0；routed 触发（`Artifice/Reanimated/…`）的件数**不变**（回归断言：同 `(卡, trigger)` 的候选集合逐条相同）。
- **风险**：改桶名会动 `card_vfx_by_card.json` 的 schema ⇒ `VfxMap` 的 `Events` 表、`SelfCheck*`、`BattleScene` 的一批断言都要跟着；**必须同一个写手**。

### R5 · `trigger` 桶名字未放行 ⇒ 整桶播

- **根因**：`IsRoutedTriggerName`（`VfxMap.cs:2233`）白名单现读 = **36 个 `case` 标签 + 2 条 `OrdinalIgnoreCase`（`ambush` / `triggerEnchantment`）= 38 个名字**；名单之外，`TryResolveTriggerPicks` 走第 ④ 档「判不出 ⇒ 回 `trigger` 整桶」，再用 `TriggerBeatRows` 按 `traitTriggerType` 过滤（`:2583`）。
- **影响面**：**36 行 / 32 卡**（名单：`data/an1_*_emit.tsv` 里 `emit == TRIGGER_BUCKET(归口待定)`）。这些行绝大多数是**卡级** `cardAbilities[]` 行（`src = cardSO.cardAbilities…`），不是 trait 行。
- **判据**：原版 `TriggerAbilities(triggerId)` 只取**那一条 trigger** 的 ability；整桶播 = 把别的 trigger 的件也播了。
- **修法**：把这 36 行对应的 `AbilityTrigger` 名逐个补进白名单（**前提 = 引擎真的会发**，判据同 `A21` 那套：`grep FireTrigger*/EmitCardTrigger`）；**发不出来的**留白（像 `NoEmitTriggerNames` 那样登记）。
- **落点**：`Core/VfxMap.cs:2233`（`IsRoutedTriggerName`）· `:2881`（`NoEmitTriggerNames`）· `:2899`（`MustRouteTriggerNames`）。
- **验收**：`VfxMap.SelfCheckRoutedTriggers()` 空；**每条**补进去的名字都要在 `MustRouteTriggerNames` 里锁住（防误删）；`BattleVfxAssertE3` 的逐卡那半要覆盖新增名。
- **注意**：`Strike(580)` 是这批里最多的（21 行）—— 引擎**已经在发**（`RuleCore.cs:6096` `FireTriggerAt(..., KeywordTable.Strike, ...)`）⇒ 只差白名单。

### R6 · 死亡触发的能力动画（`ThisUnitDeath(80)` / `UnitDeathByAbility(180)` …）没有播放拍

- **根因**：生成器把 `ThisUnitDeath`/`UnitDeathByAbility` 归到 `death` 桶（`TRIGGER_EVENT`），而 `CardFeel.PickDeathFx` 只认 `customDeathParticles`（`ResolveDeathParticle`，`VfxMap.cs:2118`）。`J1` 的 D13 只修掉了「被当成爆散体播」，**播放拍本身没建**。
- **影响面**：**7 行 / 6 卡**：`AM34 Valkyrie`（`UnitDeathByAbility(180)` → `GuardSummon`）· `BL62 Dark Disciple` · `BL66 Ghallaron's Disciple` · `GOF3 Bomb Squig`（2 行：`Wait_1s` + `Squig_Explosion`，前者是纯计时）· `GOF83 Grot on Bombsquig` · `SAU72 Lokhust Lord`（其余全是 `ThisUnitDeath(80)`）。判据 = `cards_engine.animTimes` 现读。
- **判据**：原版单位死亡时 `OnTrigger(80 / 180)`（`CardScript._UnitDeath` 尾部 / `BroadcastUnitAttacked` 同族），`TriggerAbilities` → `AbilityLogic.PlayAnim` 演这条动画；**不是**死亡粒子。
- **修法**：引擎侧为 `ThisUnitDeath(80)` / `UnitDeathByAbility(180)` 补 `EvtKind.Trigger` 发射口（照 `UnitAttack(50)` 的 `BroadcastUnitAttacked` 那一套），表现层把这两个名字加进白名单（R5 同一处）。**先查引擎线**：`NoEmitTriggerNames` 里没有它们 ⇒ 属「未登记」，不是「原版没有」。
- **落点**：`RuleEngine/Core/RuleCore.cs`（死亡广播处，`BroadcastUnitAttacked` 同族；需引擎线写手）· `Core/VfxMap.cs:2233` · `Core/CardFeel.cs:2509`（确认不再被 `PickDeathFx` 抢）。
- **验收**：`RuleEngineTest` 新增断言「单位死亡后发出 `keyword=ThisUnitDeath` 的 `EvtKind.Trigger`」；`BattleScene` 断言 `SAU72` 死亡后 `FiredCount(CreateCard Necron) == 1`。
- **依赖**：跨线（引擎 + 表现）⇒ **先派查判据代理**确认 80/180 的广播点在原版哪个方法（别只凭名字）。

### R7 · `animInfo.screenShake` 零消费

- **根因**：`CardAnimClips.RtRow`（`Battle/CardAnimClips.cs:120-137`）没有这一列；`CardAnimBeat`（`BattleDriver.cs:16540+`）也没有；`BattleDriver.cs:16343` 自承「`screenShake` 仍未消费」。
- **影响面**：全池 **57 个 CardAnim → 92 行 / 71 卡**（`ability` 15 · `play` 47 · `attack_ranged` 28 · `trigger` 2）；Sautekh **6 行 / 4 卡**。
- **判据**：`assets_full/*cardanims*/MonoBehaviour/<CardAnim>.json` 的 `animInfo.screenShake`：`presetSO`（PPtr）+ 6 个 `overwrite*` + `delay/sustainTime/amplitude/frequency/direction/attackTime/decayTime`。原版消费点 = `OverwriteCameraShakePreset.GetModifiedPreset()` → `CameraShakerManager.DoShake`（`W64` B? / `A1` R10 已有部分）。
- **修法**：
  1. `工具/gen_cardanim_clips.py` 增列（`ssPreset` + 6 个 overwrite + 6 个值）；
  2. `RtRow` / `CardAnimBeat` 加字段；
  3. 在 `PlayAbilityBeat` / 攻击命中帧 / `play` 那一拍（取决于该 CardAnim 属于哪个拍）调 `CardFeel.ShakeCamera`（已有），传 preset + 覆盖参数。
  4. **前置缺口**：`presetPathID → preset 名` 的映射表本机没有（`Resources/WarpforgeVFX/shake_presets.json` 只有 preset 本体）⇒ 先从 `bundle_*` 里导这张映射（`extract_mirror_shaders.py`/`gen_shader_*` 那套同形状）。
- **落点**：`工具/gen_cardanim_clips.py` · `Battle/CardAnimClips.cs:120-137` · `Battle/BattleDriver.cs:16540-16600`（`CardAnimBeat`）· `:19685`（`PlayAbilityBeat`）· `Core/CardFeel.cs`（`ShakeCamera`）。
- **验收**：`BattleScene` 断言「含 screenShake 的 3 个 CardAnim，播那一拍时 `CardFeel.LastShakePresetName` 等于数据里的 preset」；**灭自证**：把新列删掉 ⇒ 红。真机看 `Psychomancer_Stun` 那一发有没有震。

### R8 · trait 行可能被播两遍

- **根因**：一条 trait 行同时存在于 ①`card_vfx` 的 `trigger` 桶（会被 `VfxMap` 在 trigger 拍整桶播）与 ②`trait_effect_bindings` 的 `_triggerBook`（`TraitFrames.DisplayTrigger` 会播）。**routed 名字**时 `VfxMap` 把未标名的 trait 候选丢了（单播 ✅）；**未 routed 名字**时整桶照播 + `TraitFrames` 也播 ⇒ **同一件实例化两次**（不同锚点：`DropTargetWorld` vs 卡的 `effectsAnchor`）。
- **影响面**：与 R5 同集合（**36 行 / 32 卡**）；Sautekh 0（42 行全 routed）。
- **判据**：原版 `ActivateTriggerTraitAnim` **只**调 `DisplayTriggerAnim`（一次）；`AbilityLogic.PlayAnim` 播的是**卡自己的** `cardAnimations`，不含 trait frame。⇒ 同一件播两次 = 原版没有的第二下。
- **修法**：`TryResolveTriggerPicks` 的第 ④ 档（整桶）与 `TriggerBeatRows` 再挡一类：**`src == cardtrait-so` 的行一律不进整桶**（它们由 `TraitFrames` 负责）。判据写成一条注释 + 一条断言。
- **落点**：`Core/VfxMap.cs:2583`（`TriggerBeatRows`）· `:2575`（`IsNonTriggerBeatTraitRow` 同族）。
- **验收**：断言「对未 routed 的 trait 名字，`ResolveAllTimed(Trigger, …, keyword)` 返回的候选里没有 `cardtrait-so` 行」；真机抽一张带 `Rally`/`Armour` 的卡触发一次，看是不是只出现一个框。

### R9 · trait 触发框的「触发事件」到底有没有（**判据未闭合**）

- **现象**：`talent`（**96 行 / 96 卡**）与 `regeneration`（**9 行 / 9 卡**）（另 8 个 trait 同形状）在 `trait_effect_bindings` 里是 `TriggerType 3/6`，但我们的引擎里**没有 `KeywordTable.Talent` / `KeywordTable.Regeneration` 的任何 `FireTrigger*` 调用**（grep 结果）。
- **原版判据（已读、但没读完）**：`CardScript__ActivateTriggerTraitAnim.c` → `CardTraitCollection__GetTraitFromAbilityTrigger(triggerId)`：
  - `triggerId == 0`（`ThisCardPlayed`）→ **trait 10 = `rally`**
  - `triggerId == 80`（`ThisUnitDeath`）→ **trait 120 = `backlash`**
  - `triggerId == 81`（`Unstable`）→ **trait 1010 = `unstable`**
  - 其余 → 把 `AbilityTrigger` 枚举名转 lowerCamel，在**另一个 `TraitId` 枚举**里 `TryParse`。
- **缺的判据**：那个 `TraitId` 枚举本地 `dump.cs` **没有**（可能在别的程序集 / 被裁）。⇒ **`talent` / `regeneration` 到底由哪个 `AbilityTrigger` 触发，现在判不了**。
- **下一步（先做这个，别直接改代码）**：派**查判据代理**（只读）做两件事：
  1. 在 `d:/2` 全盘找那个枚举（`il2cpp` 元数据 / `global-metadata.dat` / 另一个 dump）；
  2. 找不到就设计一次**运行时探针**（`SceneJumpShot` mod 打一行 `GetTraitFromAbilityTrigger(x)` 的日志，x 遍历 97 个 `AbilityTrigger`）—— 这是最快、最准的方案（mod 源码在本地）。
- **落点（判据到手后）**：`Core/TraitFrames.cs:723`（`Norm`）· `RuleEngine/Core/ActionPump.cs:392`（`TriggerNameOfKeyword`，需要补 `Talent` / `Regeneration` 等）· 白名单（R5 同处）。
- **验收**：`TraitFrames.DisplayTrigger("talent")` 真有调用路径；断言「`Talent` 关键词触发时播 `TalentEffect`」。
- **⛔ 现在不许做的事**：别按「名字猜」把 `talent` 塞进 `TriggerNameOfKeyword` —— 那会造出一个原版没有的触发。

### R10 · 死亡爆散体：根特效 + 竞技场坐标 ⇒ 被 HUD 正交相机画（表现层）

- **根因**：`CardFeel.SpawnDeathBody`（`Core/CardFeel.cs:2619`）调 `WarpforgeEffectPlayer.Play(fx, **parent = null**, card.transform.position, 1f)`（`:2635`）。`WarpforgeEffectHook.cs:43` 只在 `parent != null` 时把层刷成父节点的层 ⇒ 这件留在 prefab 自带的 **层 0**；而战场相机 `cullingMask = 1<<8`、HUD 相机摘掉第 8 层 ⇒ **由 HUD 正交相机画一个竞技场世界坐标的点**。
- **影响面**：**所有单位阵亡**（Sautekh 67 行）。`出场淡入`（`FireDeployVfxInArenaSpace`）与 `技能拍`（`EnsureAnchorSpace`）**都已经**为此专门处理，并有断言（`Editor/BattleScene.cs:10890-10920`）——**只有阵亡这一族漏了**。
- **判据**：原版 `_UnitDeath…:245-263` `Instantiate(prefab, 卡位置, 3D 体旋转)`（单相机 ⇒ 天然在战场空间）；我们的双相机下必须给特效竞技场层（同族先例 = `FireDeployVfxInArenaSpace` 的注释 `:18467` 一带）。
- **修法（二选一，推荐 ①）**：
  1. 照登场那一档：用**临时载体**（层 = `ArenaLayer`、位置 = 卡根、旋转/缩放按原版）当父节点播一次，播完把特效摘到世界；
  2. 播完 `SetLayerRec(fx.gameObject, ArenaSlots.ArenaLayer)`（注意：层要**递归**设，且不能改位置）。
- **落点**：`Core/CardFeel.cs:2619`（`SpawnDeathBody`）· `:2635`（两处 `Play`）· 参照 `Battle/BattleDriver.cs:18467`（`FireDeployVfxInArenaSpace` 的载体法）。
- **验收**：
  - 断言（`Editor/BattleScene.cs`，与登场那两条同形状）：3D 档下阵亡 → `FiredParentLayerOf(DeathBodyFx) == ArenaSlots.ArenaLayer`；**灭自证**：改回 `parent: null` ⇒ 红。
  - 真机：阵亡时爆散体应**盖在卡位**、尺寸与卡一致（不是偏到别处/变小）。
- **风险**：`Play(..., 1f)` + 之后 `localScale = scale` 是**赋值**语义（`:2643`）；挂到载体后要确认赋值仍等价（载体缩放为 1 时等价）。

### R11 · `MeleeEndPos` 的 `dy` 与「卡根参考系」

- **现象**：`CardFeel.MeleeEndPos(origin, targetPos, isPlayer, dy = 0f)`（`:916`），注释（`:726` / `:913` / `:3237`）写「原版那个 `effectsAnchor` 我们**没有等价物**（我们的卡根节点就是卡中心）⇒ 取 0」。
- **为什么不成立**：`CardView.EffectAnchorBoard`（`:1821`，局部位 `From3DBody(0, 1.34, −0.128)`）**就是**等价物（2026-10-23 落的）。注释过期。
- **但也不能直接填**：原版 `MeleeEndPos` 的 `origin/targetPos` 是**卡根**（= prefab 根 = **卡底**，见 `CardView.From3DBody` 的说明），我们是**卡心** ⇒ `dy` 与 `up` 两项都要**先统一参考系**。`up × dy × attackPositionYOffset` 这一项的物理含义 = 「冲向目标时抬高到目标的特效锚高度」。
- **影响面**：所有近战单位（每次近战冲刺的终点）。
- **修法（建议）**：
  1. 先做一次**标定**：用真机截图/场景 dump 量出「我们的卡根（卡心）」与「原版卡根（卡底）」在屏幕上的差（= `Height/2 × lossyScale` 的投影），把它写成一个**有名字的常量**（`CardRootToPrefabRoot`）。
  2. 再把 `origin/targetPos` 换算到 prefab-root 参考系算 `dy`，或把 `dy` 显式定义成「目标 `EffectAnchorBoard.y − 目标卡根 y`」并**不乘**那半拍（写法要能一眼看出参考系）。
  3. 同时复核其它「传 `卡.transform.position`」的调用点（`SpawnDeathBody`、`MeleeEndPos`、`AttackRecover`）—— 用同一张 `参考系 → 量` 的表（方案 §六 U3）。
- **落点**：`Core/CardFeel.cs:916/922`（函数）· `:726/913/3237`（过期注释，铁律 5 订正）· `Core/CardView.cs:1811/1821`（等价物）。
- **验收**：断言「近战终点 y − 目标卡根 y ≈ 原版公式（含 1.34×scale×0.25）」；真机并排：近战冲刺终点应落在**目标卡的中上部**而不是卡心。
- **⛔ 别做**：直接 `dy = anchor.y - card.y` 传进去就完事（参考系没统一 ⇒ 数值可能反号/翻倍）。

### R12 · 7 张「有主动能力但没 `tg=-1` 行」的卡

- **根因**：`activeAbilityLogic` 非空，但 `animTimes` 里没有 `tg=-1`（因为 `ca` 是空串 = 原版没绑 CardAnim，或数据导出缺）。能力拍会：① 用 1.0 s 兜底（`AbilityAnimDurations` 查不到）；② 播 `ability` 桶里**未归口**的那批件（R4）。
- **影响面**：**7 卡**（`SW9 / ASH74 / GOF77 / SOR66 / SOR70 / SOR34 / SOR23`）。
- **判据**：这 7 张卡的 `activeAbility` 是否真的「一条 CardAnim 都没绑」——若是（像 `SOR30/SOR65/SW9` 那三条已知），则原版这一档**本来就不演** ⇒ A 类；若不是，是导出缺。
- **修法**：先逐卡核 `activeAbility.activeAbilityLogic[*].cardAnimations[]` 的 GUID（`资料/普查产出_1018/allcards_1350/cards_raw.json`）。空的 → 记 A 类（并在 `AbilityAnimDurations` 的 `uncovered` 里已有）；非空 → 补 `ca`（数据侧）。
- **落点**：`工具/gen_cards_engine.py`（`animTimes`）· `工具/gen_ability_anim_durations.py`。
- **验收**：`AbilityAnimDurations` 的 `uncovered` 名单与这 7 张逐卡对上；`BattleScene` 断「凡 `activeAbilityLogic` 非空且 `animTimes` 无 `tg=-1` 的卡 = 上面那 7 张白名单」。

### R13 · `animInfo.minionMovementInfo` 零消费

- **根因**：`Card3DAnimationController` / `CardAnimClips` 都不读这个结构。
- **影响面**：**9 个 CardAnim → 10 行 / 8 卡**：`DA Teleport Recall`（`DA51`×2 / `Master of Manoeuvre`）· `DA Teleport Summon Next`（`DA57`）· `DA Teleport Trigger Board Only`（`DA48`）· `DA Teleport Trigger Effect`（`DA20` / `DA48`）· `Generic Recall`（`UM110`）· `Summon Generic`（`UM118`）· `SporeMine_Explosion`（`TL37`）· `Scarab_Explosion`（0 行）。
- **判据**：原版 `CardAnimController` 把 `minionMovementInfo` 交给卡体（3D 卡自己上升 `yOffset=3.0`、`animTime=1.0`）；`shouldActivateMinionMovement=1` 的两件是「爆炸时卡体位移」。
- **修法**：数据列 → `RtRow` / `CardAnimBeat` → `Card3DAnimationController` 增加一条「卡体位移动画」（它已经由 `AdvanceTimeline` 泵推进，不需要帧循环）。
- **落点**：`工具/gen_cardanim_clips.py` · `Battle/CardAnimClips.cs:120-137` · `Battle/Card3DAnimationController.cs`（新增播放形态）。
- **验收**：断言「`UM110 Fall Back` 打出时卡视图 y 上升 3.0（卡单位）并在 1.0 s 回位」；真机看 `Fall Back` / `Indomitus Crusade`。

### R14 · `animInfo.startDelay` 零消费（1 个 CardAnim）

- **影响面**：`Cuts Brutal Warp Spider`（0.5 s）→ 1 行（`Empyric Ambush`）。`GetAnimDuration` **不含** `startDelay`（原版那条公式确实不含）⇒ 它影响的是**命中/生效那一帧**，不是时长。
- **修法**：先确认这条 CardAnim 走哪个拍（`play`/`ability`），再在那一拍的「命中帧」处把 `startDelay` 加上（`EventTiming.HitFrameLag` 同族）。
- **落点**：`Battle/CardAnimClips.cs`（加列）· `Battle/BattleDriver.cs`（命中帧）。
- **风险**：1 行、影响极小但不许不做（铁律 11）；**验收**要能指名这一张卡。

### R15 · `trait_effect_bindings.json` 文档过期 + `army` 口径

- **现象 1（文档）**：`conventions.overrides` 写「运行期**今天还没按阵营取**（`TraitFrames.PrefabFor` 没有阵营参数）」—— **已过期**：`PrefabFor(trait, trigger, army)`（`Core/TraitFrames.cs:542`）已经实现（2026-10-23）。
- **现象 2（口径）**：`army` 取的是 `CardView.Data.faction` = **拥有者的**阵营（`ToCardData(UnitState, faction)`）⇒ 跨阵营盟军/中立卡可能取错 variant（`TraitFrames.cs` 文件头 91-94 行自承）。
- **影响面**：10 条覆盖（`shield` 4 · `invulnerable` 6）。
- **修法**：①按铁律 5 订正 `conventions.overrides` 那段（**保留更正痕迹**）；②`army` 改成从 `CardDef.Faction` 取（需要 `BattleDriver` 把 `CardDef` 传进 `DisplayStatus/DisplayTrigger`，属 W1 的文件 ⇒ **排在 W1 之后**）。
- **落点**：`CardPresentation/Resources/trait_effect_bindings.json`（文档字符串）· `Core/TraitFrames.cs:542` · `Battle/BattleDriver.cs:19274/20581`（两个调用口）。
- **验收**：断言「跨阵营盟军卡的 `shield` 框取的是它**自己**阵营的 variant」。

### R16 · 同一 `CardAnim` 多触发名的归属（复核，不重做）

- 已有 `A2`（`AddTriggerNamesOf` / `TrimToLabelledRowCount`）与 `SelfCheckRoutedTriggers` 的件级覆盖（3 行：`UM37/UM84/SW18`）。本轮**只复核**：跑 `VfxMap.SelfCheckRoutedTriggers()` 应为空；若新数据又出现「同一 CardAnim 跨两张桶」的形状，按 `TrimToLabelledRowCount` 的既有逻辑处理。

---

## 三 切块与派活（5 执行 + 1 断言后置；⛔ 同文件只能一个写手）

| 块 | 独占文件 | 领的条 | 依赖 | 备注 |
|---|---|---|---|---|
| **W1 · 播放路由** | `CardPresentation/Battle/BattleDriver.cs` | **R1 · R2** · R10 的调用侧（若需要）· R15 的 `CardDef` 传递 | 无（先做） | 最大的一块；`PlaySignal` / `MaybeDisplayPlayedCardInCenter` / `PlayAbilityBeat` 都在这里 |
| **W2 · 生成器/数据** | `工具/gen_card_vfx_by_card.py` · `工具/gen_cardanim_clips.py` · `工具/gen_card_anim_order.py`（纯 py） | **R4 · R7 的数据列 · R13 的数据列 · R14 的数据列** | 无（可与 W1 并行） | 只改数据；改完要重生成 `Resources/*.json` 并 `--check` |
| **W3 · 逐拍参数桥** | `Battle/CardAnimOrder.cs` · `Battle/CardAnimClips.cs` · `Battle/Card3DAnimationController.cs` | **R3 · R13 · R7 的运行期列 · R14** | 依赖 W2 的列名（**先约定列名**，两批同时开） | —— |
| **W4 · 取数与触发归口** | `Core/VfxMap.cs` · `Core/TraitFrames.cs` · `Core/AbilityAnimDurations.cs` · `Resources/trait_effect_bindings.json` | **R5 · R8 · R15 · R16 复核** | R9 的判据（未到手前不做 R9 的代码） | `VfxMap.cs` 只允许一个写手 |
| **W5 · 表现层上下文** | `Core/CardFeel.cs` · `Core/CardView.cs` · `WarpforgeVFX/Runtime/*` | **R10 · R11 · R7 的播放侧** | R11 需要一次标定（真机） | 与 W1 的 `BattleDriver` 不重叠 |
| **W6 · 断言（后置）** | `Editor/BattleScene.cs` · `Editor/BattleVfxAssertE3.cs` | 给 W1–W5 每条补灭自证断言 | **必须最后** | ⛔ 不许作者自己写自己的断言（铁律 13·5） |
| **查判据（只读，与上面并行）** | 不改代码 | **R9 的 `TraitId` 枚举** · R6 的 80/180 广播点 · R7 的 `presetPathID → preset 名` 映射 · R12 的 7 张卡 `activeAbility` GUID | —— | 一次查完、落文件（`资料/普查产出_1024c/AN1_判据_*.md`） |

**Unity 批处理**（主对话统一跑，串行）：`BattleScene.Run`（主）· `RuleEngineTest.Run`（R6/R12 动了引擎/卡表）· `AnimFXCheck.Run`（R7/R13 动了 AnimFX/卡体）· `CardBaseDemo.Run`（若动卡面/卡体层）。

---

## 四 A 类（**不做**，但要写清理由与出处）

| # | 内容 | 是哪一种 | 出处 |
|---|---|---|---|
| A-1 | `Wait_*` 纯计时拍没有 prefab（全池 12 行；Sautekh 6 行） | ① 原版本身没有（`animAdressable` 空 GUID） | `gen_card_vfx_by_card.py` + `cardanim_clips` |
| A-2 | `minionAnimationClipInfo.enable` 全 0（1099 个 CardAnim） | ① 原版本身没有 | `CardAnimClips.cs:320` 现读 |
| A-3 | 3 个「主动能力一条 CardAnim 都没绑」的卡（`SOR30/SOR65/SW9`）这一档不演 | ① 判据为空 | `AbilityAnimDurations.cs` 文件头 ② |
| A-4 | `attack_melee` **通用**件留白（原版没有通用近战件） | ① 判据为空 | `W64` A2 |
| A-5 | `Trigger` **通用**件留白（原版只做徽标/状态框） | ① 判据为空 | `W64` A3 / 偏差 7 |
| A-6 | `remnantBodyPrefab` 不进 `death` 桶（`RT_SKIP_SOFIELDS`） | ② 既定设计（两个不同触发时刻共用一键会掷硬币） | `A2083` |

---

## 五 断言怎么写（本清单专用，避免自证）

1. **断「父层 / 世界点 / 件名」三样**，不要只断「播了」：`FiredParentLayerOf(name) == ArenaLayer` · `FiredPosOf(name) ≈ 锚点世界位置` · `FiredCount(name) == 1`。
2. **灭自证**：每条断言都要写一句「🧨 改坏法：把 X 改回 Y ⇒ 本断言红」，并在注释里点名 X/Y（同族先例：`BattleScene.cs:10890-10920` 的登场那两条）。
3. **反向闸**：给「不该出现的」也配断言 —— 例：`SAU72` 阵亡时 `FiredCount(DeathBodyFx)` 只该有 1 次（不许 `CreateCard Necron` 冒充爆散体）。
4. **数据驱动**：期望值**按卡池现算**（`CardDef.AnimTimes` / `CardAnimClips`），⛔ 不写死「269 张 / 405 行」这类数字（数据一变，断言要么自动跟着走、要么直接红）。
5. **别断浮点精确值**：`Vector3.Distance < 0.02` 这一级；时长断 `Mathf.Abs(a-b) < 0.011`。

---

## 六 还没查清（⛔ 这些**不是**结论，别照着改）

| # | 问题 | 差什么 | 谁去做 |
|---|---|---|---|
| U1 | `TraitId` 枚举（`GetTraitFromAbilityTrigger` 的 `TryParse` 目标） | 本地 `dump.cs` 没有；要另一个程序集元数据，或运行时探针 | **查判据代理**（R9） |
| U2 | 原版「根特效」在双相机下该由哪台画 | 真机并排（同一件挂竞技场层 vs 层 0） | 主对话（真机） |
| U3 | 卡根参考系（卡心 vs 卡底）的**影响面** | 一次标定 + 逐调用点穷举 | W5 + 主对话 |
| U4 | `AnimFXModule` **全量字段**比对（不只模块名） | 需要「我们组件字段 ↔ 原版序列化名」映射表 | 下一轮只读代理 |
| U5 | `notPlayed` 22 条逐条复核 | 逐条读 `why` 的出处 | 下一轮只读代理 |
| U6 | 表现层随机（爆散纹理偏移 `Random.value`）对回放的影响 | 回放时比特效实例的贴图偏移 | 联机/回放线 |
| U7 | `overrideByArmies` 10 条的逐卡影响面（跨阵营盟军） | 逐卡「卡阵营 vs 拥有者阵营」表 | W4 |

---

> 生成：2026-10-24 · AN1 · 只读。所有数字现读；复算脚本见 `资料/普查产出_1024c/scripts/`。
> 本文件是**骨架**：每条修法的「具体代码」由修复代理按判据落，落完在 `验收` 栏勾掉并回填提交号。
