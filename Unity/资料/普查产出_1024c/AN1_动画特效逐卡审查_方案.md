# AN1 · 动画 / 特效逐卡审查 —— **方案**（第七十三会话 · 2026-10-24）

> 只读审查代理 **AN1**。本文件 = **审查方案**（怎么查、查什么、判据在哪、怎么判「是不是原版」、输出长什么样、之后怎么切块派活）。
> 试点结果 → `AN1_试点_Sautekh.md`；修复清单骨架 → `AN1_修复清单_骨架.md`；脚本 → `资料/普查产出_1024c/scripts/`；中间数据 → `资料/普查产出_1024c/data/`。
> ⛔ 本文件**不重复**前三轮已经做完的**数据层**审查（C3/C4/C5）；它审的是**上一层**：**这一拍真的播得出来吗 / 播的是不是原版那一件 / 参数对不对 / 表现层有没有把它演歪**。

---

## 〇 与前三轮的分工（⛔ 别重做）

| 层 | 谁做过 | 结论 | 本轮 |
|---|---|---|---|
| **数据层**：原版卡 SO 的 VFX 字段 → 运行期表（桶 · prefab 序列 · CardAnim 10 参数 · cue/wav · 语音） | `C3` / `C4` / `C5` 三批（索引见 `普查产出_1023e/C{3,4,5}_动画特效审查_索引.md`），**13 阵营 / 1127 张全覆盖** | 「几乎逐字节对齐」，差异集中在「哪一拍播得出来」 | **只引用，不重查**；抽 1 个阵营复核一遍（试点 §1） |
| **兜底/自造审计**（代码里所有替代件、近似常量） | `W64_兜底审计_特效.md`（B1–B19 / A1–A13 / U1–U7）· `A1_自造与兜底_特效动画.md`（R1–R29） | 已逐条成账 | **只引用**；本轮补的是「这几条在**卡**这一层体现为哪些行」 |
| **视觉对拍**（我们 vs 原版渲图像素） | `资料/特效还原台账.tsv`（960 件：对得上 702 · 两边全程空 254 · 异常 4） | 台账已有判定 | **按卡 join 台账**（件级质量 → 卡级影响面） |
| **本轮的层** | —— | —— | **接线（有没有真实发射口）· 逐件真伪 · **全字段**参数 · 时序 · 边界 · **表现层上下文** |

---

## 一 判据来源（权威顺序，逐条可复核）

| 级别 | 来源 | 本方案用它判什么 |
|---|---|---|
| **① 第一权威** | `d:/2/tools/decomp_full/`（26,262 个方法体） | 时机、锚点、父子关系、朝向、位姿、`GetTraitFromAbilityTrigger` 的特例、`AnimInfo` 各字段的消费点 |
| **② 解包资源字段** | `d:/2/新解包资源/assets_full/**/MonoBehaviour` / `…/<Type>/*.json` | 序列化真值：`animInfo`（**全部字段**）、`TraitEffects[]`、`overrideByArmies`、`AnimFXController` 模块、prefab 的 `m_Layer` |
| ③ 成品卡图 | `d:/2/Warpforge部队卡片/` | 卡面文字/数值（本轮基本不用；出分歧时用） |
| 反射/索引 | `数据/索引/anim_address_map.json`（`cardanim_to_asset` · `guid_to_asset`）· `d:/2/tools/il2cpp_out/dump.cs`（枚举值：`AbilityTrigger` 97 项） | 「这件 CardAnim 的原版落点是哪个 prefab」「这个 trigger 数值叫什么」 |
| 我们自己的数据 | `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`animTimes[]`：`a/n/tg/tn/rc/o/d/sd/s/m/e/u/t`）· `CardPresentation/Resources/{card_vfx_by_card,cardanim_clips,card_anim_order,trait_effect_bindings,animfx_sounds,battle_sounds,voice_lines,remnant_body_by_card,ability_anim_durations}.json` | 我们的运行期真值 |
| 我们自己的代码 | `CardPresentation/Core/VfxMap.cs` · `Battle/BattleDriver.cs` · `Core/CardFeel.cs` · `Core/CardView.cs` · `Core/TraitFrames.cs` · `Core/TraitParticles.cs` · `Battle/CardAnimClips.cs` · `Battle/CardAnimOrder.cs` · `Battle/AnimAnchors.cs` · `WarpforgeVFX/Runtime/*` | 「这条行最终走到哪一行代码」 |
| ⛔ 非判据 | 《Warpforge Offline Rulebook》（粉丝实体版）· `d:/warpforge/scripts/*.gd`（我们旧的 Godot 复刻）· `vfx_wiring_j*.tsv`（旧台账） | 只能当旁证；**不能**用来定「原版是什么」 |

---

## 二 维度表（A–G）

> 每行格式：**编号 · 查什么 · 判据来源 · 机械判法（能不能脚本化）· 判绿阈值 · 状态**。
> 状态：`新` = 本轮首次提出；`已闭合` = 前人或本批已查清（**别重复报**）；`半` = 有部分覆盖、缺的那半要补。

### A 接线：这一拍到底播不播得出来

| # | 查什么 | 判据来源 | 机械判法 | 判绿阈值 | 状态 |
|---|---|---|---|---|---|
| **A1** | 每个 (卡 × 事件桶) 在原版有行吗、我们有吗、顺序一致吗 | 原版 `card_vfx_by_card.json#byCard[*][ev]` vs 运行期 `Resources/card_vfx_by_card.json#cards[*].events` | 逐条比 `n/a/c`（C3 的脚本） | 逐条相同（`remnantBodyPrefab` 按 `A2083` 不进桶 = 正常） | **已闭合**（C3/C4/C5） |
| **A2** | **这一拍的发射口**：桶 → `EvtKind` → 生产方法 | `BattleDriver.PlaySignal`（`:19245`）· `ExpandAbilityBeats`（`:16387`，**只对 `EvtKind.Ability`**）· `ScheduleAttackPresentation` · `CardFeel.PickDeathFx`（`:2509`）· `TraitFrames` | 脚本 `an1_bucket_matrix.py`：把每条候选归到「桶 × 出路」 | 每条候选都有**一条**生产出口；且出口时机 = 原版那个 `AbilityTrigger` | **新** |
| **A3** | `EvtKind.Play` 且 `BattleEvent.Slot < 0`（未指定目标的战术/防御） | `BattleDriver.cs:19510`（`if (slot < 0) return;`）· `IsHudAnchoredEvent`（`:20404`，只有资源三件 + `Draw`）· `EffectResolver.PlayTactic` 里 `Slot = targetSlot`（未指定目标保持 `-1`） | 逐卡判 `type ∈ {tactic,defence} && targeted==0`，再看它有没有 `play` 行 | **0 行**因槽位被丢 | **新**（试点：全池 405 行 / 269 张） |
| **A4** | `ability` 桶里 `CardAnimTime.Trigger != -1`（生成器「未归口」的触发动画） | `工具/gen_card_vfx_by_card.py` 的 `trigger_event()`（只归 7 个名字，其余 → `EV_ABILITY`） | 逐行 join `animTimes` 取 `tg/tn`；再判该卡有没有 `activeAbilityLogic`（有 ⇒ 能力拍真的会发） | 不该在能力拍播的行 = 0（或明确记录「靠 trigger 归口」） | **新**（全池 **456 行 / 274 卡**；其中 **30 行 / 19 卡**「会错拍播」） |
| **A5** | 路由白名单：`keyword` → `IsRoutedTriggerName`（`VfxMap.cs:2233`） | 白名单源码 + `NoEmitTriggerNames`（`:2881`）+ `MustRouteTriggerNames`（`:2899`） | 对每条 `trigger` 行取它的原版 trigger 名，判「在白名单 / 有发射口」 | 有发射口且在名单；**不许**「有发射但名单没有」 | **半**（`A21`/`A26`/`A30`/`G1b`/`J1` 已补 18 个名字；仍 36 行走整桶） |
| **A6** | 未放行名字 → `TriggerBeatRows` 整桶播（`VfxMap.cs:2583`）＋ `IsNonTriggerBeatTraitRow`（`:2575`） | 同上 | 逐卡列「trigger 桶里有哪些 `traitTriggerType`」，判整桶播会不会带出别的拍的件 | 整桶里只剩「这一拍该播的」 | **半**（`A2` 的 D4 已挡 `Start(0)`/`OnPlay(2)`；`Trigger(3)/(6)` 与卡级行仍整桶） |
| **A7** | 阵亡桶里的**死亡触发能力动画**（`ThisUnitDeath(80)` / `UnitDeathByAbility(180)` / `UnitDeathByPoison(170)`…） | `CardFeel.PickDeathFx` 只认 `customDeathParticles`（`ResolveDeathParticle`，`VfxMap.cs:2118`） | 逐行判 `soField != customDeathParticles && != remnantBodyPrefab` | 每条都有播放拍，或写明「原版也不播」 | **半**（`J1` 的 D13 已修「错播成爆散体」，但**播放拍本身仍缺**） |
| **A8** | trait 行 ↔ `TraitFrames` 两本字典（`_startBook` / `_playBook` / `_triggerBook`）的覆盖 | `Resources/trait_effect_bindings.json`（75 行）· `TraitFrames.PrefabFor(trait, trigger, army)`（`:542`） | 逐行：`traitName` + `traitTriggerType` → 绑定表有没有那一行 | 每条 trait 行都有消费口；**不许**出现「VfxMap 播一遍 + TraitFrames 再播一遍」 | **半**（绑定 0 缺；重复播未判） |
| **A9** | 单位技能动画两桶（`unit_attack_anim` / `unit_damaged_anim`）的搬迁与播放 | `VfxMap.LoadTableFromJson`（`:1183` 起「搬不是抄」）· `SelfCheckUnitSkillAnims`（`:3124`）· `PlayUnitSkillAnim`（`:20236`） | 已有自检 | `SelfCheckUnitSkillAnims` 空 | **已闭合**（E3 断言 + D3） |
| **A10** | `notPlayed`（运行期表主动不播的权威行，22 条） | 运行期表 `notPlayed[]`（带 `outcome/why`） | 逐条核「不播」的理由今天还成立吗 | 每条理由都有第一权威出处 | **半**（本轮只列名单，未逐条复核） |

### B 素材真伪：播的是不是原版那一件

| # | 查什么 | 判据来源 | 机械判法 | 判绿阈值 | 状态 |
|---|---|---|---|---|---|
| **B1** | `CardAnim → prefab` 落点一致性（运行期表里 `a` 与 `n` 的配对） | `anim_address_map.json#cardanim_to_asset[CardAnim].targetName` | 逐行比 `targetName == n`；`a` 为空的行按 `n` 反查 | 0 不符 | **新**（试点 Sautekh 0/261 不符） |
| **B2** | prefab 在**我们库里**（`WarpforgeVFX/Prefabs/<n>.prefab`，**大小写精确**） | 文件系统 | 逐行判 | 缺的必须是「纯计时 `Wait_*`」 | **新**（Sautekh 6 行缺 = 全是 `Wait_*`，A 类） |
| **B3** | prefab 是不是**原版件**（不是我们造的） | `assets_full/**/GameObject/<n>.json` 存在 + `guid_to_asset` | 逐行判 | 自造件 = 0（已知例外见 `A1_自造与兜底` R3/R5） | **半**（`A1` 已列自造清单；本轮按卡 join） |
| **B4** | 件级视觉质量 | `资料/特效还原台账.tsv` 的判定列（Z 对得上 / W 两边全程空 / C/D/E 异常）· `效果库报告.tsv` | 逐行 join，按卡统计 Z/W/异常 | 异常（C/D/E）**逐件**定义务；W 件要能说清「靠什么触发」 | **新（join 层）** |
| **B5** | 材质 / 贴图 / shader | `W64` B10–B15 · `A1` R1/R2 | 只引用 | —— | **已闭合（成账）** |
| **B6** | 音效 cue / wav / 语音 | `animfx_modules.json`（每件 `sounds[*].sound`）→ `animfx_sounds.json` → `Art/audio/sfx/*.wav`；`voice_lines.json` → `Art/audio/vo/*.ogg` | 脚本逐件 join（试点脚本已实现） | 缺 cue / 缺 wav / 缺语音 = 0，或写明「原版该件没有控制器」 | **新（逐卡 join）** |
| **B7** | 音效参数（`repeat` / `loops` / `time` / `timeInterval` / `is2d` / 3D 距离） | `WarpforgeEffectPlayer.BuildSounds`（`:486` 起） | 代码已读 | 4 个字段都有消费点 | **已闭合** |
| **B8** | AnimFX 模块齐全度（原件几个模块 / 我们几个） | `效果库报告.tsv` 的「模块」列 vs 原版 prefab 的 `AnimFXController.modules[]` | 逐件比模块名集合 | 模块名集合相同（缺 = 出声） | **半**（R23/R24/R25 三条已知通道未接） |

### C 参数：**全字段**（不是那 10 个）

> 🔴 本轮的方法论要点：C3/C4/C5 比的是 `cardanim_clips` 那 10 个字段（`sp/ep/pt/vd/ts/tm/te/mv`）。原版 `animInfo` 有 **40+ 个字段**，本轮把**每个字段有没有消费点**列清。

| # | 字段 / 参数 | 我们的消费点 | 机械判法 | 状态 |
|---|---|---|---|---|
| **C1** | `startPosOption / endPosOption / pointTowardsOption` | `AnimAnchors.Resolve`（36 档）+ `PlayAbilityBeat` | 逐 CardAnim 统计 sp/ep/pt 分布；对照 `AnimAnchors` 哪些档取不到 | **半**（`ManaTransformOf` 10 档没挂；试点里全池只有 **1 行**用 61，影响面小但仍是缺口） |
| **C2** | `vfxDelayTime` | `VfxMap.ResolveAllTimed` 的 `Delay` | 逐行比 | **已闭合** |
| **C3** | `startDelay` | **没进** `RtRow` / `CardAnimBeat`（`EventTiming` 只把它写成「命中时刻在数据里」） | 全池非 0 的行 = ？ | **新**（试点：全池 **1 个** CardAnim 非 0 —— `Cuts Brutal Warp Spider` 0.5） |
| **C4** | `timeAtStartPos / timeMoving / timeAtEndPos` | `PlayAbilityBeat` | 逐行比 | **已闭合（ability 桶）**；`play`/`trigger` 桶**不消费**（见 C9） |
| **C5** | `shouldMoveVFX` | 同上 | 同上 | **已闭合（ability/attack）** |
| **C6** | `easeCurve` | `CardAnimClips.RtRow.ease`（`:128`） | 全池曲线形状分布：88.6% = identity、55 条空曲线、6 条非 identity | **已闭合**（试点抽 3 件核过） |
| **C7** | `orientToVelocity` | `RtRow.otv` | 同上 | **已闭合** |
| **C8** | `applyMovementOffsets` + `offsetMovementAxis.{x,y,z}Axis` + `minMax*Offset` + `useNonModifiedAnimTime` | `RtRow.ao/ont/ofx/ofy/ofz/mn..mx` + `CardAnimBeat.OffsetAt` | 同上 | **已闭合**（`W` 偏差 6 第 1 条） |
| **C9** | **`play` / `trigger` 桶的 sp/ep/pt/mv** | ⛔ **没有消费点**：`PlaySignal` 对这两桶只给一个点（`layout.DropTargetWorld(slot)`） | 脚本 `an1_param_audit.py`：按桶统计「有参数的行数」 | **新**（全池 `play` 桶 **270 行**带参数；`trigger` 桶 9 行；`death` 3 行；`attack_melee` 6 行） |
| **C10** | **`screenShake`（`presetSO` + 6 个 `overwrite*` + `delay/sustain/amp/freq/dir/attack/decay`）** | ⛔ **没有消费点**（`RtRow` 无此列；`BattleDriver.cs:16343` 自承「仍未消费」） | 全池扫 `assets_full/*cardanims*/MonoBehaviour/*.json` | **新**（全池 **57 个 CardAnim**；试点 Sautekh **6 行 / 4 张**） |
| **C11** | `minionAnimationClipInfo.{enable,delay}` | 卡体 clip 表 **本身为空**（1099 个资产 `enable` 全 0） | 全池扫 | **A 类（原版本身没有）** |
| **C12** | `minionMovementInfo.*`（`shouldActivateMinionMovement` / `rotateX/Y/ZAxis` / `useXZCurve` / `useYCurve` / `x/y/zOffset` / `animTime` / `visibilityStateAfterFinish`） | ⛔ **没有消费点**（`Card3DAnimationController` 不读它） | 全池扫：9 条带 `yOffset=3.0/animTime=1.0`；**2 条** `shouldActivateMinionMovement=1`（`Scarab_Explosion` / `SporeMine_Explosion`）；6 条 `rotateYAxis.enable` | **新** |
| **C13** | `waitAnimToApplyEffect` | `CardAnimOrder.Waits`（`:164`） | 逐行比 | **已闭合** |
| **C14** | `tweenAnim`（UnitTweenSO） | 数据侧已解（`A11` §2·1）；**运行期没加进时长**（`waitAnimation=1` 的 4 条全在触发链 ⇒ 对 active 零影响） | 现读 | **半（有判据、影响 0）** |
| **C15** | AnimFX 模块字段（每个 `AnimFXModule*` 的序列化字段） | `数据/游戏数据/animfx_modules.json`（912 件）→ 我们 prefab 的组件 | 逐件逐字段比（需要字段名映射表，见 §3 ⑦） | **半**（`W64` B13 只覆盖被截断的曲线；**全量字段比对未做**） |
| **C16** | 锚点空间（HUD 正交 vs 竞技场透视）与 px/单位桥 | `AnimAnchors` 文件头（§「那些锚点不在同一个空间里」）/ `EnsureAnchorSpace` / `DropTargetWorld` | 逐条生产路：`parent == null` 还是挂在竞技场上 | **半**（见 F 节） |

### D 时序与节奏

| # | 查什么 | 判据来源 | 机械判法 | 状态 |
|---|---|---|---|---|
| **D1** | 一条 `ability` 拆 N 拍、每拍时长 = 自己 `AnimInfo.GetAnimDuration()` | `AbilityLogic.PlayAnim.c` · `BattleManager._ResolveAnim` | `CardAnimOrder` + `CardAnimClips` | **已闭合（62 张卡）** |
| **D2** | 「发动能力」总时长 = Σ（真·主动能力卡全在表里；表外卡退 1.0 中位数） | `ability_anim_durations.json`（93 卡 / 160 条） | 逐卡 join | **已闭合（93/93）**；残留 = 全池 **7 张**「`activeAbilityLogic` 非空但 `animTimes` 无 `tg=-1` 行」的卡（`SW9 / ASH74 / GOF77 / SOR66 / SOR70 / SOR34 / SOR23`）会以 1.0 s 兜底**并**播到未归口的件（与 A4 同源） |
| **D3** | 阵亡节拍（抖 → 关 3D 体 + 爆散体 +0.5 → 0.2 收尾） | `_UnitDeath_d__446__MoveNext.c` | 已有断言（`BattleScene.cs` 9026–9121） | **已闭合** |
| **D4** | 攻击命中帧（近战 0.1 / 远程 0.2）与命中粒子的挂点 | `ResolveAttackAnimationEffects.c` | `HitFrameLag` + `ScheduleAttackPresentation` | **已闭合** |
| **D5** | 多候选串行（同 (卡,事件) 多个 `cardAnimations` 按原版时长错开） | `AbilityLogic.PlayAnim.c` | `ResolveAllTimed` 的 `T` | **已闭合** |
| **D6** | 事件级副作用只跑一次（语音/音效/手感/trait 粒子） | 原版是事件级 | `beatTail` 门 | **已闭合** |
| **D7** | `waitAnimToApplyEffect` 的「等/不等」 | `PlayAnim.c:274` | `CardAnimOrder.Waits` | **已闭合** |
| **D8** | 打断/复位（卡被顶掉、回手、变形、残骸化时特效与时长） | 反编译 + `_fading` / `PendingExitFor` | 逐场景造事件（自检） | **新**（未系统查过） |

### E 边界

| # | 查什么 | 机械判法 | 状态 |
|---|---|---|---|
| **E1** | 多触发行 / 同名歧义 / `Order` 三段归属 | 已有 `SelfCheckRoutedTriggers` + `TrimToLabelledRowCount` | **半**（3 行「一个 CardAnim 挂两个 trigger」已验） |
| **E2** | 联机与回放一致性（表现层随机源、去重表、`_vfxOffsetRng`） | 读代码：`System.Random(seed)` 派生；`UnityEngine.Random` 在 `CardFeel` 里有哪些（爆散纹理偏移等） | **半**（回放比局面哈希；**表现层随机未比**） |
| **E3** | 批处理无帧循环（`Destroy` / 粒子 `Simulate` / 自毁靠 `Update`） | 已知纪律 | **已闭合** |
| **E4** | 2D 兜底布局 vs 3D 棋盘（同一件在两档下的空间/尺度） | `use3DBoard` 门 + `CardView.SetLayer` | **半**（死亡爆散体只在 3D 档出问题，见 F2） |
| **E5** | 阵容/模式/上下文差异（同一张卡在不同模式、不同座位、不同目标数下） | 铁律 5·c：**状态 → 参数**表 | **新**（如 `SW21` 替代能力 0.4/0.2 是已知一例；全池未系统列） |
| **E6** | 重名卡（3 组）与 id/名字双键解析 | `FindCardRow` / `ResolveCardIdOf` | **半** |
| **E7** | 卡池外/临时卡（`CreateCard` 造出来的卡）有没有动画数据 | `notPlayed` + `cards_engine` 覆盖 | **新** |

### F 表现层上下文（**用户 2026-10-24 新点名**：动画/特效本身是原版的，但被「演歪了」）

> 一句话：**「件对不对」和「演得对不对」是两层**。这一节查的是后者 —— 同一件原版 prefab，挂错形态 / 挂错锚 / 用错相机 / 排错层 / 生命周期被截断，看起来就不像原版。

| # | 查什么 | 判据来源 | 机械判法 | 状态 |
|---|---|---|---|---|
| **F1** | **卡体形态**：场上卡是 3D 网格（`Card 3D WH40k`）+ 卡面层 `2DCard`；特效是给哪一种做的 | `CardView.BuildBody3D` / `CardFace.{Hand,Board,Full}` / `_faceMode`；登场 clip 0.7 s 才 `SetFace(Board)`（`CardFeel.cs:1351`） | 读代码 + 在登场过渡期造一拍特效（自检） | **半**（结构兜底已加 `SyncBoard` 那次补换体；**过渡期**未验） |
| **F2** | **落点锚**：`effectsAnchor`（场上）/ `2DCard`（手牌）· 卡根 · 目标格 · 手牌锚 · HUD 锚；**HUD 正交（108 px/单位）vs 战场透视（≈182 px/单位）** | `CardView.EffectAnchorBoard`（`:1821`，局部位 `From3DBody(0,1.34,−0.128)`）· `AnimAnchors` | 逐条生产路判：`parent == null`（根特效 → **HUD 相机**）还是挂在竞技场层节点上；`EnsureAnchorSpace` 有没有走 | **新**（试点抓到 1 条：死亡爆散体） |
| **F3** | **位置 / 朝向 / 缩放**：`MinionScaleByPosition` 按深度改 `localScale` · 招式前冲/收招 · 朝向目标 · 桌面 z 深度 · 左右行镜像 | `CardFeel.DepthScale*`（`:925` 起）· `MeleeEndPos`（`:916`）· `FireAtAttackAnchor`（`:19083`） | 逐个位移公式比原版；**先统一参考系**（我们的卡根 = 卡心；原版 prefab 根 = **卡底**，见 `CardView.From3DBody`） | **半**（`MeleeEndPos` 的 `dy=0` 注释已过期，见 F3-a） |
| **F4** | **层级 / 排序**：渲染队列 · layer · zTest/cull · 特效在卡前还是卡后 · HUD 与 3D 的叠放 | `CardView.StampEffectRoot` · `ImageQuad.SetRenderQueue` · 两台相机的 `cullingMask`（`BattleScene.cs:28821/29200`） | 逐条：`parent` 的层 + `StampNewChildren` 覆盖范围 | **半**（`F4` 已接「特效跟卡排队」；**根特效不参与**） |
| **F5** | **状态与生命周期**：Hand→Board 过渡期 · 被顶掉/回手/变形/残骸化 · `PendingExitFor` 打断 `SyncBoard` | `_myFading` / `DetachStaleView` / `PendingExitFor`；特效挂在卡上 ⇒ 卡销毁即特效被截断 | 造事件逐条走 | **新** |
| **F6** | **时序**：特效相对动作拍的先后（命中帧 / 前冲 / 收招 / 死亡抖动）、并行与串行 | 同 D1–D6 | —— | **已闭合（主要拍）** |
| **F7** | **动画与声音的节拍对齐**（cue 的 `time` 字段相对 AnimFX 起播） | `WarpforgeEffectPlayer.BuildSounds`（`:486`） | 读代码 | **已闭合** |
| **F8** | **同一件的「根特效」与「挂卡特效」两条路**会不会在同一拍各播一份 | 逐拍判 `parent` | 造事件 | **新** |
| **F9** | **表现层随机**（爆散纹理偏移 `Random.value`、粒子 `autoRandomSeed`）是否破坏可复现/回放一致 | `CardFeel.SpawnDeathBody`（`:2670` 起） | 读代码 + 回放比 | **半** |
| **F10** | **`_animfxCtx`（`actingCard/targetCard`）在每条播放入口都对** | `HookAnimFxCards` + `FireCueVfx`（`:18579`，`cueCtx` 已带） | 逐入口列表比 | **已闭合**（`新账-82④`） |
| **F11** | **姿态/倾摆**（`AutoCardRotation` 在场上关掉）与特效朝向的叠加 | `CardView.SetFace` / `AutoCardRotation` | 读代码 | **半** |
| **F12** | **特效根缩放是「乘」还是「赋值」**（原版两种都有） | `WarpforgeEffectPlayer.Play(assignScale:)`（`W64` B17） | 逐调用点判 | **已闭合** |

**F3-a（本轮新增的具体疑点，试点已走到）**：`MeleeEndPos(origin, targetPos, isPlayer, dy = 0f)`（`CardFeel.cs:916`）—— 注释写「原版那个 `effectsAnchor` 我们**没有等价物**（我们的卡根节点就是卡中心）⇒ `dy` 取 0」。
🟡 **这条理由已经过期**：`CardView.EffectAnchorBoard`（2026-10-23）就是等价物。**但**不能直接把 `dy` 填上：原版 `MeleeEndPos` 的 `origin/targetPos` 是**卡根**（= prefab 根 = 卡底），我们是**卡心** ⇒ 两边要**先统一参考系**再算。判据/做法见 `AN1_修复清单_骨架.md` 的 `R11`。

### G 补充维度（本方案新增，说明为什么值得查）

| # | 维度 | 为什么值得查 | 机械判法 |
|---|---|---|---|
| **G1** | **卡牌动画的「归属权」**：同一个 `CardAnim` 被两张卡 / 两个 trigger 共用时，播的是「谁的」 | 数据里同一个 CardAnim 多处引用（`Anim` 名不唯一，59 行同名重复）；表现层靠 `Order` 三段区分，错一位就播到别的拍 | 逐 (卡, CardAnim, Order) 建归属表；`SelfCheckRoutedTriggers` 的件级那半已有先例 |
| **G2** | **`animInfo` 全字段消费矩阵**（C 节） | C3/C5 只比 10 个字段，「参数正确」远未被证明；本轮已抓出 `screenShake` / `minionMovementInfo` / `startDelay` 三条**整字段零消费** | 扫 `assets_full/*cardanims*/MonoBehaviour/*.json` 全字段 + 代码 grep 消费点 |
| **G3** | **「未归口触发」这条下水道**（生成器 7 个名字之外全进 `ability`） | 这是把「触发动画」塞进「能力拍」的唯一入口；一旦某张卡有主动能力就会**错拍播**，没有就**永不播** | 逐行 join `animTimes.tg`，分类（脚本已做） |
| **G4** | **`notPlayed` 名单**（22 条运行期主动不播） | 「不播」也是一种结果，必须有第一权威出处；否则就是丢内容 | 逐条核 `why` 的出处今天还成立吗 |
| **G5** | **件级质量 → 卡级影响面**（台账 join） | 台账只回答「这件像不像原版」，不回答「哪些卡因此看起来不像」；派活时要知道影响面 | 逐卡 join `资料/特效还原台账.tsv` |
| **G6** | **trait 触发框的 `triggerId → trait` 映射** | 原版 `ActivateTriggerTraitAnim` 先 `GetTraitFromAbilityTrigger(triggerId)`（三条特例：0→rally、80→backlash、81→unstable）再演；**不是**「任意关键词都演所有 trait」 | 读 `CardTraitCollection__GetTraitFromAbilityTrigger.c`（已读）+ 找 `TraitId` 枚举（**没找到，见 §六**） |
| **G7** | **`overrideByArmies`（按阵营换件）** | `trait_effect_bindings.json` 现读 **10 条**带覆盖（`shield` 4 · `invulnerable` 6）；`PrefabFor(trait, trigger, army)` 已实现，但阵营取的是 `CardView.Data.faction` = **拥有者的**阵营 ⇒ 跨阵营盟军/中立卡可能取错件 | 逐条列出「覆盖的 army → 件」，再逐卡判它是不是跨阵营 |
| **G8** | **「原版没有」也要有出处**（A 类清单） | 3D 战场里最容易把「我们没做」误写成「原版没有」；要求每条 A 类带反编译/资产出处 | 逐条核 `W64` A1–A13 + `A1` 的 A 类 |
| **G9** | **卡池外/临时卡的演出**（`CreateCard` 造出来的 token） | 造出来的卡常常有自己的动画（`CreateCard Necron` 等）；它不在 `cards_engine.animTimes` 里 ⇒ 参数全丢 | 逐 token 查 `card_vfx_by_card` 有没有行、参数从哪来 |
| **G10** | **同一件在「根特效」与「挂卡特效」两条路的**空间/层/缩放**是否等价** | 双相机下这是最容易静默错的一族（`A367` 就是它） | 建「调用点 → parent → 层 → 相机 → px/单位」表 |

---

## 三 输出 schema（逐卡表列）

**主表（逐卡 × 逐桶 × 逐条目，一行一条候选）**

| 列 | 含义 | 示例 |
|---|---|---|
| `card` / `name` / `faction` / `type` / `targeted` | 卡身份 | `SAU11` / `Technomancer` / `Sautekh` / `unit` / 0 |
| `bucket` / `idx` | 原版事件桶 + 桶内序号（与 `card_vfx_by_card` 的 `es` 顺序一致） | `ability` / 0 |
| `origHas` / `rtHas` | 原版有 / 我们有（`Y/N`） | `Y/Y` |
| `prefab` / `cardanim` / `conf` | 播的件 / CardAnim 名 / 置信档 | `Technomancer_Buff` / `Technomancer_Buff` / `authoritative` |
| `caTarget` / `caMatch` | `cardanim_to_asset` 的落点 / 是否等于 `prefab` | `Technomancer_Buff` / `Y` |
| `inLib` | 我们 `WarpforgeVFX/Prefabs/` 里有这件吗 | `Y` |
| `ledgerVerdict` / `reportVerdict` | 台账 / 效果库报告的判定 | `Z` / `Z` |
| `origTrigger` / `origTriggerVal` | 原版 `AbilityTrigger` 名 / 数值 | `FollowUpResolution` / 410 |
| `emit` | **生产播放口**（枚举，见下表） | `ABILITY_BEAT` |
| `params` | 该件 `AnimInfo` 里**有值**的位置/位移参数 | `move` / `orient` / `summonSpot` |
| `consumed` | 该桶**是否消费** `params` | `N` |
| `extra` | 未消费的额外字段 | `screenShake(preset)` |
| `diffType` | 差异类型（D 表编号） | `R2` |
| `severity` | `S0`（内容丢失/播错件）· `S1`（时机/落点错）· `S2`（参数/观感）· `S3`（待判） | `S1` |
| `rootCause` | 根因编号（→ 修复清单） | `R2` |
| `fix` | 修法 + 落点 `文件:行号` | `BattleDriver.cs:19510` |
| `verify` | 验收办法（断言名 / 真机步骤） | `BattleScene ★Sautekh-play-anchor` |

**`emit` 取值表**（与 `an1_bucket_matrix.py` 一致）
`PLAY_BUCKET(逐卡)` · `ABILITY_BEAT` · `ATTACK_RANGED(逐卡)` · `UNIT_ATTACK_BUCKET` · `UNIT_DAMAGED_BUCKET` · `DEATH_PARTICLE` · `REMNANT_TABLE` · `DEATH_ABILITY_NO_EMIT` · `TRAIT_FRAMES(OnPlay/Start)` · `TRAIT_FRAMES(Trigger)` · `TRAIT_NO_BINDING` · `TRAIT_FROMCODE` · `TRIGGER_BUCKET(归口待定)` · `TIMING_ONLY` · `UNKNOWN`

---

## 四 检测「兜底 / 自造 / 错件」的机械办法（脚本清单）

| 脚本（`资料/普查产出_1024c/scripts/`） | 做什么 | 产出 |
|---|---|---|
| `an1_lib.py` | 公共装载器（所有路径现读，不写死张数） | —— |
| `an1_pilot_sautekh.py` | 逐卡拆行：桶 · prefab · CardAnim · 落点一致性 · 在库 · 台账 · 语音 · **animInfo 额外字段** | `data/an1_sautekh_rows.tsv` · `data/an1_sautekh_cards.tsv` |
| `an1_emit_audit.py [Faction]` | 逐行归口：走哪条生产口 / trait 绑定覆盖 / 阵亡能力动画无口 | `data/an1_<Faction>_emit.tsv`（13 个阵营全跑过） |
| `an1_emit_inventory.py` | 引擎侧「哪些关键词真的会发 `EvtKind.Trigger`」清单 + 与白名单/卡数据的交叉表 | `data/an1_engine_emits.txt` · `data/an1_trigger_matrix.tsv` |
| `an1_param_audit.py` | 逐桶统计「带位置/位移参数的行数」与「该桶是否消费」+ 未指定目标的非单位卡 | `data/an1_param_buckets.tsv` |
| `an1_bucket_matrix.py [Faction]` | **总表**：每条候选的最终出路（含「永不播」「单点播」「整桶播」）。⚠️ **它的 `trigger` 判定是粗口径**：trait 行（`src=cardtrait-so`）在 `animTimes` 里没有行 ⇒ 它归到「按 trigger 名归口」；这些行的**真实播放口 = `TraitFrames`**（见 `an1_emit_audit.py` 的 `TRAIT_FRAMES*`）。两张表要**一起看** | `data/an1_bucket_matrix.tsv`（全池）· `data/an1_bucket_matrix_<Faction>.tsv`（单阵营） |

**「自造 / 兜底件」的三道机械闸**（都不靠印象）：
1. **落点闸**：`cardanim_to_asset[CardAnim].targetName == 运行期 prefab` —— 不符 = 播的不是原版那件。
2. **在库闸**：`WarpforgeVFX/Prefabs/<prefab>.prefab` 存在（**大小写精确**；`Wait_*` 例外 = 纯计时）。
3. **出处闸**：`assets_full/**/GameObject/<prefab>.json` 存在 ⇒ 原版件；不存在 ⇒ 必须能在 `A1_自造与兜底` 的自造清单里找到它，否则是**未登记的自造件**（= 新账）。
   ＋ **台账闸**：`资料/特效还原台账.tsv` 判定为 `C/D/E` 的件 → 逐件定义务；`W`（两边全程空）→ 必须写清「靠什么触发」。

**「参数没消费」的机械闸**：字段名 × 代码 grep。一条字段在 `CardAnimClips.RtRow` / `CardAnimBeat` / `AnimInfo` 消费链里**一次都不出现** ⇒ 「整字段零消费」（本轮抓出 `screenShake`、`minionMovementInfo`、`startDelay` 三条）。

---

## 五 切块建议（修复阶段 · 按**文件所有权**切，⛔ 同一文件只能一个写手）

| 块 | 归属文件 | 领什么账 | 为什么绑在一起 |
|---|---|---|---|
| **W1 · 播放路由** | `CardPresentation/Battle/BattleDriver.cs`（独占） | `play` 槽位丢失、`play/trigger` 参数不消费、`ability` 桶错拍、`death` 能力动画口、根特效层（F2） | 全部落在 `PlaySignal` / `ExpandAbilityBeats` / `ScheduleAttackPresentation` 三处，同一文件 |
| **W2 · 数据与生成器** | `工具/gen_card_vfx_by_card.py` · `工具/gen_cardanim_clips.py` · `卡表生成器`（纯 py，**不碰 `.cs`**） | 未归口的 456 行分桶、`screenShake` / `minionMovementInfo` 落列、`Wait_*`、`notPlayed` | 只有数据没代码，可独立跑 + `--check` |
| **W3 · 逐拍参数** | `Battle/CardAnimOrder.cs` · `Battle/CardAnimClips.cs` | 31 张卡「有 `tg=-1` 但不在拍表」、`screenShake` 列、`minionMovementInfo` 列、`startDelay` | 是「参数从数据到播放器」的桥 |
| **W4 · 取数与时长** | `Core/VfxMap.cs` · `Core/AbilityAnimDurations.cs` · `Core/EventTiming.cs` · `Core/TraitFrames.cs` | `ability` 桶过滤、trigger 整桶、trait 双播、时长兜底 | 都在 `Core/` 这几件里；`VfxMap` 只允许一个写手 |
| **W5 · 表现层上下文** | `Core/CardFeel.cs` · `Core/CardView.cs` · `WarpforgeVFX/Runtime/*` | 死亡爆散体层（F2）、`MeleeEndPos` 参考系（F3）、生命周期（F5）、特效 prefab 参数（C15） | 组件级，和 W1 的 `BattleDriver` 不重叠 |
| **W6 · 断言（后置）** | `Editor/BattleScene.cs` · `Editor/BattleVfxAssertE3.cs` | 给 W1–W5 每条补**灭自证**断言 | 必须**最后**做（先把实现落地，再钉）；⛔ 不许作者自己写自己的断言 |

⛔ **Unity 批处理全局串行**（子代理不许跑），由主对话统一在同步点跑：`BattleScene.Run`（主要）+ `RuleEngineTest.Run`（若动卡表）+ `AnimFXCheck.Run`（若动 AnimFX 列）+ `CardBaseDemo.Run`（若动卡面）。

---

## 六 本方案**没有覆盖 / 判据未闭合**的部分（⛔ 不许当结论）

| # | 问题 | 还差什么 |
|---|---|---|
| U1 | **`TraitId` 枚举**（`GetTraitFromAbilityTrigger` 里 `Int32Enum.TryParse` 的那个枚举）本地 `dump.cs` **没有** | 需要另一个程序集的元数据，或运行时探针（`mod` 打一条日志）⇒ **26 个 trait 的触发框里，10 个（`backlash` 除外）到底能不能被触发，现在判不了** |
| U2 | **原版「根特效」到底由哪台相机画**（原版只有一台相机，我们是两台） | 需要真机并排截图（同一件挂在竞技场层 vs 层 0） |
| U3 | **卡根参考系**（我们 = 卡心；原版 prefab 根 = 卡底）对所有「传 `transform.position`」的调用点的影响面 | 需要一次**逐调用点**的穷举 + 一个标定（原版截图里卡底/卡心各在哪一像素） |
| U4 | **`AnimFXModule` 全量字段比对**（C15） | 需要 `AnimFXModule*` 的字段名映射表（我们组件 ↔ 原版序列化名），本轮只做了「模块名集合」 |
| U5 | **`notPlayed` 22 条**的逐条复核 | 逐条读 `why` 的出处 + 今天的代码是否仍成立 |
| U6 | **表现层随机对回放的影响**（`Random.value` 在 `CardFeel` 里用于爆散纹理偏移） | 回放时比「特效实例的贴图偏移」—— 今天回放只比局面哈希 |
| U7 | **`overrideByArmies` 的 10 条**逐卡影响面 | 需要「卡所属阵营 vs 拥有者阵营」的逐卡表（跨阵营盟军） |

---

> 版本：2026-10-24 · AN1（只读）。配套：`AN1_试点_Sautekh.md` · `AN1_修复清单_骨架.md`。
