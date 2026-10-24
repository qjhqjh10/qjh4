# AN1 · 试点 —— **Sautekh（79 张）** 动画 / 特效逐卡审查

> 试点代理 **AN1** · 2026-10-24 · **只读**（未改任何代码 / 数据 / 场景，未跑 Unity，未动 git）。
> 方案 → `AN1_动画特效逐卡审查_方案.md`；修复清单 → `AN1_修复清单_骨架.md`。
> 脚本 → `资料/普查产出_1024c/scripts/`；逐行数据 → `资料/普查产出_1024c/data/`（下表都能从那里复算）。
> 选 Sautekh 的理由：79 张（覆盖适中）；7 个事件桶全都有；有残骸体、有阵营远程档、有 9 条 `play` 战术链、有 `Artifice/Talent/Regeneration` 三种 trait 触发族 —— 一张阵营能把 A–G 全部维度走一遍。

---

## 〇 一句话结论（数一数）

| 层 | 结果 |
|---|---|
| **数据层**（C5 已查） | 261 行逐条相同；`remnantBodyPrefab` 42 行按 `A2083` 不进桶（正常）。**本轮独立复核：仍然 0 处不符** |
| **素材真伪** | **0 件自造 / 0 件错落点**（261 行里 `CardAnim → prefab` 落点全对）；库内缺 6 行**全是 `Wait_*` 纯计时拍**（原版合法）；**52 件**去重 prefab 里 **50 件**有 `AnimFXController`/cue、**2 件无控制器**（`Necrons death explosion` 台账 Z / `ScytheAssaultShadow` 台账 W）；cue → wav **0 缺**；逐卡语音 **0 缺** |
| **接线（播不播得出来）** | 🔴 **37 行 / 22 张卡：`play` 桶「永不播」**（未指定目标的战术/防御 ⇒ `Play` 事件 `Slot=-1`，`BattleDriver.cs:19510` 直接 return）<br>🔴 **13 行 / 10 张卡：`play` 桶「单点播」** —— `sp/ep/pt/mv` 一个都不消费<br>🟡 **18 行：`ability` 桶里的行原版时机不是「发动能力」**（生成器未归口，见 A4）—— 其中 6 行 `Artifice` / 2 行 `FollowUpResolution` / 5 行 `Reanimated` / 3 行 `OtherUnitDeath` / 1 行 `OtherUnitSummoned` / 1 行 `TurnBeforeEnd`；**Sautekh 没有一张卡有主动能力 ⇒ 这 18 行在能力拍上不会播**，全靠 trigger 归口播放，而那条路**不消费锚点/位移**<br>🔴 **1 行：死亡触发的能力动画没有播放拍**（`SAU72` 的 `CreateCard Necron`）<br>✅ 26 行逐卡远程（锚点/位移全消费）· 67 行阵亡粒子 · **51 行 TraitFrames**（trigger 桶 42 + trait_onplay 9） |
| **参数（全字段）** | 🔴 `animInfo.screenShake` **6 行 / 4 张卡**（`SAU17`/`SAU20`/`SAU49`/`SAU51`）**整字段零消费**<br>🟡 `animInfo.minionMovementInfo`：Sautekh 无直接命中（全池 9 条，见修复清单）<br>✅ `easeCurve`/`orientToVelocity`/`applyMovementOffsets`/`waitAnimToApplyEffect` 已消费（抽查 3 件逐字段核过） |
| **时序** | ✅ **复核后订正**（铁律 5）：`AbilityAnimDurations` 对**真的有主动能力**（`animTimes.tg == -1`）的 **93 张卡全覆盖（93/93，缺 0）**。Sautekh 那 17 张有 `ability` 行的卡**没有一张有 `tg=-1` 行**（它们的行是「未归口触发」）⇒ 引擎不会为它们发 `EvtKind.Ability` ⇒ **1.0 s 兜底对它们不会被用到**。残留只有全池 **7 张卡**：`activeAbilityLogic` 非空但 `animTimes` 里没有 `tg=-1` 行（`SW9 / ASH74 / GOF77 / SOR66 / SOR70 / SOR34 / SOR23`）—— 它们会以兜底时长在能力拍上播**未归口**的那批件（与 R4 同源） |
| **表现层上下文（F 维）** | 3 个特效逐条走完：**1 件全对**（`NecronGauss`）· **1 件参数层丢**（`Technomancer_Buff`，只单点播）· **1 件层/空间错**（`Necrons death explosion`：根特效 + 竞技场坐标 ⇒ HUD 正交相机画）<br>＋ 一条参考系疑点（`MeleeEndPos dy=0` 的注释已过期） |

---

## 一 数据层复核（不重复 C5，只验「它的结论今天还成立吗」）

| 项 | C5 结论 | 本轮独立复算 | 一致? |
|---|---|---|---|
| 逐卡 8 桶 vs 原版 | 79/79 一致（差 2 条已在 P0/P1） | 261 行逐条比 `n/a` 序列：**0 不同**（`remnantBodyPrefab` 42 行按既定跳过） | ✅ |
| CardAnim 10 参数 | 233 个全等 | 抽查 Sautekh 用到的 23 个 CardAnim：`sp/ep/pt/vd/ts/tm/te/mv` 与解包 `animInfo` **全等** | ✅ |
| prefab 在库 | 0 缺 | 6 行「缺」= `Wait_1s` / `Wait_1,5s`（**无 prefab，原版合法**） | ✅ |
| 音效 / 语音 | 0 缺 | 52 件去重 prefab 全在 `animfx_modules`（2 件无 `AnimFXController`：`Necrons death explosion`（台账 Z，脚本驱动）/ `ScytheAssaultShadow`（台账 W））；cue → wav **0 缺**；50 条逐卡语音 **0 缺** | ✅ |
| 台账判定 | —— | 224 行 `Z`（对得上）· 31 行 `W`（两边全程空 = 靠触发/脚本）· 6 行无件（`Wait_*`） | 新增（join 层） |

> 台账 `W` 最多的两件正好是**最常用的**：`NecronGauss`（远程兜底，26 行）与 `ScytheAssaultShadow` —— 它们「渲染探针对不上内容」不等于是错的，但**意味着像素级验收对它们无效**，只能靠真机看（见 §6）。

---

## 二 机械扫描结果（脚本 `an1_bucket_matrix.py Sautekh`）

### 2·1 桶 × 出路（**运行期表 219 行**）

> ⚠️ 口径说明（别把两个数当矛盾）：**§0 的 261 行** = **原版 `byCard`** 的行数（含 **42 行 `remnantBodyPrefab`** —— 那 42 行按 `A2083` 不进运行期桶）；**本表 219 行** = **运行期表**的行数（= 261 − 42）。

| 桶 | 行数 | 出路 | 判定 |
|---|---|---|---|
| `play` | **37** | 永不播（`Slot=-1`） | 🔴 **S0** |
| `play` | 13 | 单点播，锚点/位移不消费 | 🟠 **S1** |
| `play` | 5 | 纯计时拍（`Wait_*`） | ✅ A 类 |
| `ability` | 18 | 原版时机 ≠ 能力拍（生成器未归口） | 🟡 **S1**（时机） |
| `trigger` | 42 | 全部是 **trait 行**（`src=cardtrait-so`）⇒ 播放入口 = `TraitFrames`：**31 行 `_triggerBook`**（`Trigger(3)`）+ **11 行 `_startBook`/`_playBook`**（`Start(0)`/`OnPlay(2)`；`D4` 已把它们从 `VfxMap` 整桶里挡掉） | ✅（除 §5-R9） |
| `attack_ranged` | 26 | 逐卡远程（锚点/位移全消费） | ✅ |
| `death` | 67 | 阵亡粒子（`customDeathParticles`） | ✅ |
| `death` | **1** | **能力动画，无播放拍**（`SAU72`） | 🔴 **S0** |
| `attack_melee` | 1 | 搬进 `unit_attack_anim`（`SAU31`，D3 已修） | ✅ |
| `trait_onplay` | 9 | `TraitFrames._playBook`（登场那一拍） | ✅ |

### 2·2 「永不播」的 22 张卡 / 37 行（全清单）

| 卡 | 行数 | 播不出来的件 |
|---|---|---|
| `SAU57 Tomb World` | 5 | `RelentlessMarchSummon` ×5 |
| `SAU50 Dimensional Breach` | 4 | `Dimensional_Breach_Battleground` · `Dimensional_Breach_Spawn` ×3 |
| `SAU55 Immortal Pride` | 3 | `Buff_Sautekh_Intense_warlord` · `GainArmourEffect` · `Sau_ImmortalPride` |
| `SAU44 Disruption Blades` | 2 | `Buff_Red_target` · `Damage_Protocol_target` |
| `SAU46 Spyder Nest` | 2 | `RelentlessMarchSummon` ×2 |
| `SAU51 Scythe Assault` | 2 | `Sau_ScytheAssault` · `Sau_ScytheAssault_Shadow` |
| `SAU74 Dimensional Corridor` | 2 | `CreateCard_Necrons` · `DeckBuff_Green` |
| `SAU75 Awakened Dynasty` | 2 | `CreateCard_Necrons` · `DeckBuff_Green` |
| `SAU76 Inescapable Retribution` | 2 | `RelentlessMarchSummon` ×2 |
| 其余 13 张各 1 行 | 13 | `Indomitable_Will_Effect`（SAU45/SAU58/SAU60）· `Sau_Reconstitution_Protocols`(39) · `Heal_Protocol_target`(42) · `Solar_Pulse_Effect`(43) · `CurseOfThePharon_Effect`(47) · `ResurrectionOrb`(49) · `Buff_Sautekh_Intense_warlord`(53) · `Necron_Regeneration_Effect`(54) · `Master_Chronomancer_Effect`(master) · `RelentlessMarchSummon`(relentless-march) |

> 共同点：**全是 `type ∈ {tactic, defence}` 且 `targeted == 0`**（22/22）。⇒ 不是「哪张卡特殊」，是**一整类卡**的路由断了。
> ⚠️ 判据不是「事件 Slot 应该是多少」：`EffectResolver.PlayTactic` 里 `Slot = targetSlot`，而**没指定目标时一个都不填**（保持 `-1`）——那是**照原版记录字段**的正确结果。断的是**表现层**：`PlaySignal` 把 `Slot<0` 当成「不在场上」直接 return，而没问「这张非单位卡此刻在不在中央展示位」。

### 2·3 「单点播」的 10 张卡 / 13 行（锚点/位移被丢）

| 卡 | 行 | 被丢的参数（该卡该行 `sp/ep/pt/mv/tm`） |
|---|---|---|
| `SAU16 Night Scythe` | 1 | `RelentlessMarchSummon`（sp=14 召唤位 → 应落在下个空位） |
| `SAU17 Psychomancer` | 1 | `Psychomancer_Stun`：sp=0→ep=2、**mv=1 tm=0.4**、`screenShake` |
| `SAU38 Extermination Protocol` | 1 | `Explosion_Gauss_ability`：sp=2 目标位 |
| `SAU40 Protection Protocol` | 1 | `Heal_Protocol_target`：sp=2 |
| `SAU56 Methodical Destruction` | 1 | `Sau_MethodicalDestruction` |
| `SAU59 Awakened Obelisk` | 2 | `CreateCard_Necrons` · `Explosion_Gauss_ability` |
| `SAU65 Royal Warden` | 1 | `Atk_Gauss_Twinlinked`（**远程件**出现在 `play` 桶：方向/朝向全靠参数） |
| `Hardwired Destruction` | 3 | `Damage_Protocol_quick_target` · `Damage_Protocol_target` · `GainArmourEffect` |
| `Lord of the Storm` | 1 | `Lord_of_the_storm_hit` |
| `Particle Whip` | 1 | `ParticleWhip`（sp=4/5 板中心族） |

### 2·4 `ability` 桶的 18 行：原版时机不是「发动能力」

| 原版触发 | 行 | 卡（行数） |
|---|---|---|
| `Artifice(560)` | 6 | `SAU25` · `SAU26` · `SAU6` · `SAU69` · `SAU70` · `SAU73` |
| `Reanimated(570)` | 5 | `SAU22` · `SAU3` · `SAU4`×2 · `SAU71` |
| `OtherUnitDeath(90)` | 3 | `Lord of the Storm` · `SAU32` · `SAU68` |
| `FollowUpResolution(410)` | 2 | `SAU11 Technomancer` · `SAU61 Resurrection Vault` |
| `OtherUnitSummoned(190)` | 1 | `SAU2` |
| `TurnBeforeEnd(41)` | 1 | `SAU20 Chronomancer` |

**为什么不至于错播**：Sautekh 这 17 张卡**都没有非空 `activeAbilityLogic`** ⇒ 引擎不会为它们发 `EvtKind.Ability` ⇒ 「在能力拍上播」这条今天不可达。
**但代价是**：它们只能靠 `trigger` 归口播；而 `trigger` 那条路走 `FireEventAll`（**单点**），所以**锚点/位移同样被丢**（与 §2·3 同一个根因）。`artifice` 6 行是这条路里最典型的（`Technomancer_Buff`/`Hexmark_Artifice` 都带 `mv=1`，见 §4-E3）。

### 2·5 `screenShake`：6 行 / 4 张卡（参数层整字段零消费）

| 卡 | 桶 | CardAnim |
|---|---|---|
| `SAU17 Psychomancer` | `play` + `attack_ranged` | `Psychomancer_Stun` |
| `SAU20 Chronomancer` | `attack_ranged` | `Psychomancer_Stun` |
| `SAU49 Resurrection Orb` | `play` | `ResurrectionOrb` |
| `SAU51 Scythe Assault` | `play` | `Sau_ScytheAssault` · `Sau_ScytheAssault_Shadow` |

判据：`assets_full/bundle_necronssautekhcardanims_assets_all/MonoBehaviour/Psychomancer_Stun.json` 的 `animInfo.screenShake.presetSO` 非空（`m_PathID ≠ 0`）；而我们 `CardAnimClips.RtRow`（`Battle/CardAnimClips.cs:120-137`）**没有这一列**、`CardAnimBeat` 也没有 ⇒ 全池 **57 个 CardAnim** 的这一档都没接。

---

## 三 逐卡表（79 张 · 只列**有差异/需要关注**的 29 张；全表见 `data/an1_sautekh_rows.tsv` / `an1_sautekh_cards.tsv`）

| # | 卡（id） | 类型 | 桶与件 | 差异 | 严重度 | 根因 |
|---|---|---|---|---|---|---|
| 1 | **Tomb World**（`SAU57`） | tactic·无目标 | play ×5 `RelentlessMarchSummon` | 永不播（Slot=-1） | **S0** | R1 |
| 2 | **Dimensional Breach**（`SAU50`） | tactic·无目标 | play ×4（Battleground + Spawn×3） | 永不播 | **S0** | R1 |
| 3 | **Immortal Pride**（`SAU55`） | tactic·无目标 | play ×3 | 永不播 | **S0** | R1 |
| 4 | **Disruption Blades**（`SAU44`） | tactic·无目标 | play ×2 | 永不播 | **S0** | R1 |
| 5 | **Spyder Nest**（`SAU46`） | tactic·无目标 | play ×2 `RelentlessMarchSummon` | 永不播 | **S0** | R1 |
| 6 | **Scythe Assault**（`SAU51`） | tactic·无目标 | play ×2 | 永不播 + `screenShake` 未消费 | **S0** | R1 / R7 |
| 7 | **Dimensional Corridor**（`SAU74`） | tactic·无目标 | play ×2 | 永不播 | **S0** | R1 |
| 8 | **Awakened Dynasty**（`SAU75`） | tactic·无目标 | play ×2 | 永不播 | **S0** | R1 |
| 9 | **Inescapable Retribution**（`SAU76`） | tactic·无目标 | play ×2 | 永不播 | **S0** | R1 |
| 10 | **Lokhust Lord**（`SAU72`） | unit | death: `CreateCard Necron` | 死亡触发能力动画**无播放拍** | **S0** | R6 |
| 11 | **Indomitable Will** / **Hyperlogical Strategist** / **Implacable Conqueror** / **Divination Menhir** | tactic/defence | play `Indomitable_Will_Effect` | 永不播 | S0 | R1 |
| 12 | **Reconstitution Protocol** / **Relentless Advance** / **Solar Pulse** / **Curse of the Phaeron** / **Resurrection Orb** / **Undying Legions** / **Conquering Tyrant** / **Master Chronomancer** / **Relentless March** | tactic/defence | play ×1 | 永不播 | S0 | R1 |
| 13 | **Night Scythe**（`SAU16`） | unit | play `RelentlessMarchSummon` | 单点播（应落召唤位 `sp=14`） | S1 | R2 |
| 14 | **Psychomancer**（`SAU17`） | unit·targeted | play `Psychomancer_Stun` | 单点播 + `screenShake` | S1 | R2/R7 |
| 15 | **Chronomancer**（`SAU20`） | unit | ranged `Psychomancer_Stun` | `screenShake` 未消费 | S2 | R7 |
| 16 | **Extermination Protocol** / **Protection Protocol** / **Methodical Destruction** / **Awakened Obelisk** ×2 / **Royal Warden** / **Hardwired Destruction** ×3 / **Lord of the Storm** / **Particle Whip** | mixed | play | 单点播（锚点/位移丢） | S1 | R2 |
| 17 | **Technomancer**（`SAU11`） | unit | ability `Technomancer_Buff`（其实原版时机 = `FollowUpResolution`） | 走 trigger 单点播、`mv=1 tm=0.2` 的「从自己 → 目标」位移丢失 | S1 | R2/R4 |
| 18 | **Resurrection Vault**（`SAU61`） | defence | ability `Heal_Protocol_target`（原版 = `FollowUpResolution`） | 同上 | S1 | R2/R4 |
| 19 | **Hexmark Destroyer / Plasmancer / Tesla Immortal / Flayed Ones Pack / Damaged Hexmark / Annihilation Barge** | unit | ability（原版 = `Artifice`） | 靠 trigger 单点播；`Artifice` 同时被 `TraitFrames` 播一次框 | S1 | R2/R4/R8 |
| 20 | **Gauss Warrior**（`SAU4`）· **Flayed One**（`SAU3`）· **Lokhust Heavy Destroyer**（`SAU22`）· **Immortals Phalanx**（`SAU71`） | unit | ability（原版 = `Reanimated`） | 同上（`reanimated` 已放行） | S1 | R2/R4 |
| 21 | **Skorpekh Lord**（`SAU32`）· **Lokhust Destroyer**（`SAU68`）· **Lord of the Storm** | unit/tactic | ability（原版 = `OtherUnitDeath`） | 同上 | S1 | R2/R4 |
| 22 | **Canoptek Plasmacyte**（`SAU2`） | unit | ability（原版 = `OtherUnitSummoned`） | 同上 | S1 | R2/R4 |
| 23 | **Chronomancer**（`SAU20`） | unit | ability（原版 = `TurnBeforeEnd`） | 同上 | S1 | R2/R4 |
| 24 | **全池 7 张卡**（`SW9 / ASH74 / GOF77 / SOR66 / SOR70 / SOR34 / SOR23`） | — | `ability`（未归口件） | 有 `activeAbilityLogic` 但 `animTimes` 里没有 `tg=-1` 行 ⇒ 能力拍会以 1.0 s 兜底播**未归口**的件 | S2 | R12/R4 |
| 25 | **Canoptek Spyder**（`SAU13`）/ **Scarab Swarm**（`SAU27`）/ **Triarch Stalker**（`SAU30`）/ **Necron Lord**（`SAU29`）/ **Ophydian Destroyer**（`SAU9`）/ **Gauss Immortal**（`SAU8`）/ **Lokhust Lord**（`SAU72`） | unit | trigger（`talent` / `regeneration`） | 这两个 trait 的「触发框」在**引擎里没有对应的触发事件**（`KeywordTable.Talent` / `Regeneration` 从未出现在任何 `FireTrigger*` 调用） | **待判**（U1） | R9 |

---

## 四 表现层上下文（F 维）· 三个特效逐条走

### E1 · `NecronGauss`（远程攻击兜底件，26 行；代表卡 `SAU4 Gauss Warrior`）

| F 维 | 原版 | 我们 | 判定 |
|---|---|---|---|
| F1 卡体形态 | 攻击方是场上 3D 卡（`Card 3D WH40k`），从 `effectsAnchor` 发 | `CardView.SetFace(Board)` + `SetLayer(ArenaLayer)`（`BattleDriver.cs:22122`）；锚点 = `EffectAnchorBoard` | ✅ |
| F2 落点锚 / 相机 | `Instantiate(prefab, 攻方 effectsAnchor.position, LookRotation(target−start), anchor)` | `FireAtAttackAnchor`（`BattleDriver.cs:19083`）：carrier 挂 anchor、`EnsureAnchorSpace`、世界朝向、播完摘到世界 | ✅ 两条都属于**竞技场空间**（同一台透视相机） |
| F3 位置/朝向/缩放 | 朝目标；缩放继承卡 | 同（carrier 挂在 anchor 下 ⇒ 继承 `lossyScale`）；`ArmAttackVfxMove` 消费 `mv/ts/tm` | ✅ |
| F4 层级/排序 | 单相机 | 特效在 `ArenaLayer`（`EnsureAnchorSpace`）；`StampEffectRoot` 让效果跟卡排队 | ✅ |
| F5 生命周期 | 挂在 anchor 下（卡走它走） | 同；攻击件播完 `ReleaseAbilityCarrier`（比原版少一段「跟着卡」） | ✅（等价） |
| F6 时序 | 回正 0.2 s 后 `DelayedVFXPlay` | `ScheduleAttackPresentation` 排在 `Attack + AttackWindLag` | ✅ |
| **结论** | —— | —— | **这一件是唯一「参数层 + 表现层全对」的样板**；台账判定 `W`（渲染探针对不上）⇒ 只能真机看 |

### E2 · `Necrons death explosion`（阵亡爆散体，67 行；代表卡任何 Sautekh 单位）

| F 维 | 原版 | 我们 | 判定 |
|---|---|---|---|
| F1 卡体形态 | 关掉 3D 体、在卡位生成爆散体，贴图从卡上借、matcap 按 tier 四档 | `SpawnDeathBody`（`CardFeel.cs:2619`）：位置 = 卡 `transform.position`、旋转/缩放取 `body3D`、matcap 四档 | ✅（位姿那三项逐条对） |
| **F2 落点锚 / 相机** | 原版**只有一台相机** ⇒ `Instantiate(…, 卡位置)` 天然在战场空间 | `WarpforgeEffectPlayer.Play(fx, **parent = null**, card.transform.position, 1f)`（`:2635`）⇒ **根特效**；`WarpforgeEffectHook.cs:43` 只在 `parent != null` 时把层刷成父节点的层 ⇒ 这件**留在层 0**；而战场相机 `cullingMask = 1<<8`、HUD 相机摘掉第 8 层（`BattleScene.cs` 28821/29200 注释）⇒ **由 HUD 正交相机画**，位置却是**竞技场世界坐标** | 🔴 **S1（疑）** |
| F2-b 对照 | 登场淡入（`normalSummon`）与技能拍**都已经**为此专门处理：`FireDeployVfxInArenaSpace`（`BattleDriver.cs:18487` 一带）+ `EnsureAnchorSpace`；`BattleScene.cs:10890-10920` 有两条断言盯着 | 阵亡爆散体**没有**同等待遇，也没有断言 | 🔴 |
| F3 缩放 | `localScale = 3D 体 lossyScale`（**赋值**） | `Play(..., 1f)` + 之后 `p.transform.localScale = scale`（`:2643`） | ✅（等价赋值） |
| F5 生命周期 | 自己的 `Animator` + `destroyTime` | `WarpforgeEffectPlayer` 按 `destroyTime` 收 | ✅ |
| **结论** | —— | —— | **机制上可证**（层 + 相机 + parent=null 都是读得到的）；**观感差多少必须真机并排**。修法 = 与登场同一形状（载体/竞技场层）或 `SetLayerRec` 到 `ArenaLayer` |

### E3 · `Technomancer_Buff`（`SAU11` 的 ability 桶行；原版时机 = `FollowUpResolution(410)`）

| F 维 | 原版 | 我们 | 判定 |
|---|---|---|---|
| 参数（`animInfo` 实读） | `sp=0 self` · `ep=2 abilityTarget` · `pt=0` · `mv=1` · `tm=0.2` · `waitAnimToApplyEffect=1` · `screenShake=0` | `CardAnimClips` 有这一行（10 字段全等） | ✅ 数据在 |
| A2 发射口 | `FollowUpResolution` 触发 → `AbilityLogic.PlayAnim` → `CreateAnim`（锚点 + 位移 + 朝向） | `PlaySignal` 的 `EvtKind.Trigger` 支 → `FireEventAll(Trigger, DropTargetWorld(slot), …)` ⇒ **单点播**：`sp/ep` 不解析、`mv/tm` 不生效 | 🔴 **S1** |
| F2 落点 | 起 = 施法者 `GetEffectAnchor()`（场上档） | 点 = `DropTargetWorld(slot)`（**卡心投到 HUD 平面**）—— 位置「差不多」，但**不在锚点上、也不在竞技场层**（根特效 → HUD 相机，尺度按 108 px/单位） | 🔴 同上根因 |
| F3 位移 | 0.2 s 从自己走到目标 | 不走（单点） | 🔴 |
| F4 排序 | 单相机 | HUD 层，压不到 3D 卡的正确前后 | 🔴 |
| **结论** | —— | —— | **件对、参数在、播放路丢一半** —— 这正是用户说的「看起来不像原版」的典型：同一件 prefab，少了一段 0.2 s 位移 + 换了一台相机 |

> **F 维的共性结论（试点）**：**「逐拍参数消费」目前只存在于两条路**（`ability` 的 `ExpandAbilityBeats` 与 `attack_ranged` 的 `ResolveAttackPieceAnchors`）。其余桶（`play` / `trigger` / `death` / `attack_melee` 的边缘）都是「一个点 + 根特效」⇒ **锚点、位移、朝向、竞技场层四样一起丢**。这不是某一件事的 bug，是**一条结构性的次级路径缺层**。

---

## 五 差异按根因归组（Sautekh 落地数；全池数见修复清单）

| 根因 | 是什么 | Sautekh 命中 | 全池命中 |
|---|---|---|---|
| **R1** | `EvtKind.Play` + `Slot<0`（未指定目标的非单位卡）⇒ `PlaySignal` 直接 return | **37 行 / 22 卡** | **405 行 / 269 卡** |
| **R2** | `play` / `trigger` / `death` 桶**不消费** `sp/ep/pt/mv/ease`（只给一个点） | 13 行（play）+ 18 行（trigger 侧的 ability 桶） | play 270 行带参数 + trigger 9 + death 3 + attack_melee 6 |
| **R3** | `ability` 桶里 `tg=-1` 但不在 `CardAnimOrder`（62 卡表）⇒ 单点播 | Sautekh 0 行（该阵营无 `tg=-1` 行） | **59 行 / 31 卡** |
| **R4** | 生成器把未归口触发（7 个名字之外）一律塞进 `ability` 桶 | **18 行 / 17 卡**（Sautekh 无卡有主动能力 ⇒ 错拍播风险 0） | **456 行 / 274 卡**（其中 **30 行 / 19 卡**有「错拍播」风险） |
| **R5** | `trigger` 桶名字未放行 ⇒ 整桶播（可能带出别的拍的件，且 trait 件会被 `TraitFrames` 再播一遍） | 0 行（Sautekh 42 行都归了口） | 36 行 / 32 卡 |
| **R6** | 阵亡触发的能力动画（80/180）没有播放拍 | **1 行**（`SAU72`） | 7 行 / 6 卡（`AM34`(180) · `BL62` · `BL66` · `GOF3`×2 · `GOF83` · `SAU72`） |
| **R7** | `animInfo.screenShake` 整字段零消费 | **6 行 / 4 卡** | **57 个 CardAnim**（影响行数按卡统计见修复清单） |
| **R8** | trait 行 ↔ `TraitFrames` 双播风险（routed 时 trait 候选被丢 ⇒ 单播；未 routed 时整桶播 + `TraitFrames` 也播 ⇒ 双播） | 0 行（42 行全 routed） | 与 R5 同集合 |
| **R9** | trait 触发框可达性：`talent`(10 行) / `regeneration`(8 行) 在引擎里**没有对应触发事件** | **18 行** | 见清单（按 trait 统计） |
| **R10** | 死亡爆散体是**根特效 + 竞技场坐标**（F2） | 67 行（所有单位阵亡） | 全池阵亡 |
| **R11** | `MeleeEndPos dy=0` 的「没有等价物」注释已过期（`EffectAnchorBoard` 已存在），且参考系未统一 | 所有近战单位 | 全池近战 |
| **R12** | 「发动能力」时长表 **对真有主动能力的卡是完整的（93/93）**；残留 = **7 张**「有 `activeAbilityLogic` 但 `animTimes` 无 `tg=-1` 行」的卡会以 1.0 s 兜底 + 播未归口件 | 0（Sautekh 无此形状） | 7 卡 |
| **R13** | 素材真伪 | **0**（这一条是**结论不是缺陷**） | 0（按本轮机械闸） |
| **R14** | 音效/语音覆盖 | **0 缺** | 0 缺 |
| **R15** | `trait_effect_bindings.json` 的 `conventions.overrides` 文档过期（运行期**已经**按阵营取件，`TraitFrames.PrefabFor(trait,trigger,army)`），且 `army` 取的是**拥有者**阵营 | Sautekh 无覆盖命中的 trait | 10 条覆盖（`shield` 4 · `invulnerable` 6） |

---

## 六 没查清 / 只能真机判的

| # | 问题 | 差什么 |
|---|---|---|
| 1 | `NecronGauss`（26 行）与 `ScytheAssaultShadow` 台账判定 = **W（两边全程空）** | 渲染对拍看不出内容 ⇒ **必须真机看**「远程那一下有没有、像不像」 |
| 2 | 死亡爆散体（R10）观感偏差量 | 机制可证（层/相机/坐标），**差多少像素/多大尺寸**要真机并排 |
| 3 | `talent` / `regeneration` 触发框到底该在哪一拍 | `TraitId` 枚举本地 `dump.cs` 没有（`CardTraitCollection__GetTraitFromAbilityTrigger.c` 里那个 `Int32Enum.TryParse`）⇒ 判不了「原版会不会演」 |
| 4 | `psychomancer_stun` 的 `screenShake` 参数（preset 指向哪个 preset、6 个 `overwrite*` 值） | 数据已读到 `presetPathID`，但**本机没有 `presetPathID → preset 名` 的映射表**（`Resources/WarpforgeVFX/shake_presets.json` 有 preset，但缺 pathID 键）⇒ 修 R7 前要先补这张映射 |
| 5 | 卡根参考系（卡心 vs 卡底）影响面 | 需要一次标定：原版截图里「卡底 / 卡心 / `effectsAnchor`」各在哪一像素 |
| 6 | 触发期**双播**（R8）在真实对局里是不是被人眼看见 | 静态可证「两条路都会实例化同一个 prefab」，**观感**要真机 |
| 7 | `SAU72` 的 `CreateCard Necron` 在原版哪一拍播（`ThisUnitDeath(80)` 的 `AbilityLogic.PlayAnim`） | 已读到 `TriggerAbilities(80)` 那条链，但**我们引擎没有 `ThisUnitDeath` 事件**（`NoEmitTriggerNames` 里也没有它，属「未登记」）⇒ 修法要新发射口，属引擎线 |

---

> 生成：2026-10-24 · AN1 · 只读。数据复算命令：
> `python -I -X utf8 资料/普查产出_1024c/scripts/an1_pilot_sautekh.py`
> `python -I -X utf8 资料/普查产出_1024c/scripts/an1_emit_audit.py Sautekh`
> `python -I -X utf8 资料/普查产出_1024c/scripts/an1_param_audit.py`
> `python -I -X utf8 资料/普查产出_1024c/scripts/an1_bucket_matrix.py Sautekh`
