# 审查 · 每卡 VFX / 音效 / AnimFX 模块（T2b · 只读）

> 审查代理（**只读**）。日期 **2026-10-23**。仓库根 `d:/4`。
> ⛔ 未跑 Unity / 未动 git / 未改任何源码与数据 / 未改两张正本。
> 判据顺序：① `d:/2/tools/decomp_full/`（第一权威）→ ② 本地解密资源 → ③ 我们自己的数据与代码。
> 基线：`Unity/数据/游戏数据/card_vfx_by_card.json` 与
> `Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json`（**工作区 == HEAD**，无未提交改动；
> +118 那次升权威已在 `74f6a99c` 落盘）。"改前" 取 `5e7cc775` 的两份同一文件逐条对比。

---

## 一、总表（范围 × 判定）

| # | 范围 | 符合 | 部分 | 替代（播了别的） | 缺 | 数值错 |
|---|---|---|---|---|---|---|
| ① | 每卡特效表 · 同档多播（新增 50 桶 / 109 picks） | 50 桶 | **59 picks**（未钉时间，`T=0`） | 0 | 0 | 0 |
| ② | 5 条 `unknown`（`penitence`） | 1（SOR8，被权威行裁掉） | 4（SOR10/21/27/32 仍会播） | 0 | 0 | 0 |
| ③ | 6 条 Shield 阵营变体 | 0 | 0 | **6**（播基础件而非变体） | 0 | 0 |
| ④ | 单候选 `Duration` 压 0 → 0.05 s | 1（Techmarine 双候选） | 0 | 0 | 0 | **16 桶 / 16 卡** |
| ⑤ | `vfxDelay` 双加（`BattleDriver:18655`） | 潜伏 | 0 | 0 | 0 | **今天 0**（无卡命中条件） |
| ⑥ | `MinionScaleByPositionSO` 过期注释 | 假设（注释错） | 1 档功能未还原 | 0 | **1** | 0 |
| ⑦ | AnimFX 11 钩子 / 音效 / 语音 | **10/11 钩子 + 全部音效数据** | 0 | 0 | **1 钩子**（`MaterialResolver`） | 0 |

一句话：**「+118」不是运行时会多播的桶数** —— 运行期那份表实测 **451 → 500（+49）**；多播这件事本身
（天赋各播一件）**判据成立**；真正的硬缺陷是 **④ 16 张卡的技能动画时长被压成 0.05 s** 和 **③ 6 张 Shield 播错变体**。

---

## 二、逐条（原版值 | 我们值 | 出处 | 判定 | 建议修法）

### ① 同档多播：口径核对 + 逐桶判定

| 项 | 原版值 | 我们值 | 出处 | 判定 |
|---|---|---|---|---|
| 多播桶计数口径 | —— | 生成器报 **1074 → 1192（+118）** | `工具/gen_card_vfx_by_card.py:1328,1442`；`stats.traitAuthority.multiPlayBucketsAfter` | **口径不同**：它数的是**全量件 `byCard` 的键空间**（含不落运行期的行）。运行期那份（`VfxMap` 真正读的）实测 **451 → 500 = +49** |
| 运行期净变化分解 | —— | `ability +26` · `trigger +23` · `play ±0` · `attack_melee ±0` · `death ±0`；**另有 1 桶缩水** = `SOR8/trigger` | 逐条 diff（本审查脚本）：`single→multi 50` · `multi→single 1` | ✅ 数字自洽 |
| 多件本身对不对 | 天赋动画**逐天赋各播一件、并发起播**：`CardScript.ActivateTraitsOnSummonOrEnchantment` 遍历天赋列表、每件调 `BattleCardUI.DisplayStatusAnim`（`:67`），后者对每条 `StartCoroutine(ResolveTraitAnim)` **fire-and-forget**（`BattleCardUI__DisplayStatusAnim.c:118-119`、`:284-289`） | 同档 `authoritative` 平票**全播**（`Core/VfxMap.cs:738-758`） | 判据：`d:/2/tools/decomp_full/CardScript__ActivateTraitsOnSummonOrEnchantment.c:67`、`BattleCardUI__DisplayStatusAnim.c:118-119`、`BattleManager._ResolveTraitAnim_d__465__MoveNext.c` | ✅ **符合**（原版确实会同时播，不是我们多播） |
| 时间/顺序 | 每件各等**自己的** `vfxDelayTime`（`ResolveTraitAnim` 起手 `WaitForSeconds(AnimInfo.vfxDelayTime)`） | 天赋那 59 条候选**钉不上** `CardAnimTime`（不在卡 `animTimes` 里）⇒ `T = 0`、`Duration = 0` | `Core/VfxMap.cs:1116-1124`（`FindAnimTimeIndex` 钉行）、`:1167-1168`、`:1197-1231` | ⚠️ **部分**：今天等价（天赋动画的 `vd` 全为 0，见下），但**没有留白出声**，将来数据一变就会静默错 |
| `vd > 0` 的规模实测 | —— | `cardanim_clips.json` 1067 行里 **只有 2 行** `vd > 0`：`Ferocity Recall to deck` 1.4 · `Rally EC Heldrake` 0.4；这 2 行**都不是**天赋动画 | `Resources/cardanim_clips.json`（本审查现算） | ✅ 所以「`T=0` 同时播」今天与原版等价 |

**逐桶列表**见 §三·1（50 桶，按事件分组）。
**结论**：不要因为「+118」就回退这次升权威 —— 运行期只多 50 桶，且判据支持多播。
**但**有一条要补：`VfxMap` 对「钉不上的候选」应**出声一次**（现在静默 `T=0`），否则将来 `vd` 不为 0 时无法发现。

### ② 5 条 `unknown`（`penitence`，SOR8/10/21/27/32）

| 卡 | 桶 | 同桶还有什么 | 运行期实际行为 | 判定 |
|---|---|---|---|---|
| `SOR8` | `trigger` | `FlankEffect`（`authoritative`） | `unknown` 排最后（`confPriority` 末位）⇒ **被裁掉，不播**（**行为变了**：改前会播） | ✅ 反而更对（无依据的那条不再播） |
| `SOR10` | `trigger` | **只有**这条 | `unknown` 是唯一候选 ⇒ **仍会播**（与改前一致） | ⚠️ 同意 1023b §七·4「行为不变」；工作区版本已改成不播（§二·⑧） |
| `SOR21` | `trigger` | **只有**这条 | 同上 | ⚠️ 同上 |
| `SOR27` | `trigger` | **只有**这条 | 同上 | ⚠️ 同上 |
| `SOR32` | `trigger` | **只有**这条 | 同上 | ⚠️ 同上 |

补充（有实据）：这 5 张卡的 **`ability` 桶里另有一条 `authoritative` 的 `PenitenceEffect`**
（`PenitenceTraitTrigger`），所以「这个特效要不要播」在**卡 SO 那一层有依据** ⇒ 5 条 `unknown` 缺的只是
`CardTrait` 那一层，**不是「原版没有这个特效」**。
出处：`Resources/card_vfx_by_card.json`（本审查逐条 dump）；`VfxMap.cs:738-758`（`confPriority` 排序）。

**判定：不算缺陷，但要在账上写清「`unknown` 目前仍会播」**（⚠️ **本表是 HEAD 口径**；审查期间另一个代理已在工作区把 `unknown` 整桶清掉 —— 见 §二·⑧）。若产品口径是「无权威依据一律不播」，
改法只需在 `EnsureLoaded` 里加一句「`bestRank` 那一档是 `unknown` ⇒ 整桶丢弃 + 出声一次」——
**这是行为改动（4 张卡会变安静），按铁律 11 该做，但要和特效线确认口径。**

### ③ 6 条 Shield 阵营变体（EC20 / TAU17·36·32·19·3）

| 项 | 原版值 | 我们值 | 出处 | 判定 |
|---|---|---|---|---|
| `EC20.ability` | `ShieldTraitECPlay` → prefab `ShieldTraitEffect_Idle Emperor's Children Variant` | `ShieldTraitEffect_Idle`（基础件） | 权威：`trait_collection.json` 的 `Shield` trait `overrideByArmies`（报告 `633条inferred升权威_执行报告.md` §2·3）；我们：`Resources/card_vfx_by_card.json` 逐条 dump | ❌ **替代** |
| `TAU17/36/32/19/3.ability` | `ShieldTraitTAUPlay` → `… TAU Variant` | 同上（基础件） | 同上 | ❌ **替代** |
| 件在不在本地 | —— | **在**：`Assets/WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle Emperor's Children Variant.prefab` · `… TAU Variant.prefab`（另有 Necron / Sororitas 变体） | `ls Assets/WarpforgeVFX/Prefabs/ | grep ShieldTrait` | ✅ 不缺件 |
| 参数在不在 | —— | `ShieldTraitECPlay` / `ShieldTraitTAUPlay` **都在** `Resources/cardanim_clips.json` | 本审查现读（三条 `ShieldTrait*` 全 True） | ✅ 修起来是**纯数据改动** |

**判定：真缺陷（6 卡），判据已齐、缺件 0** ⇒ 属「能做、且能立刻做」，不该排到后面。

### ④ 单候选 `Duration` 被压成 0 ⇒ `CardAnimPlayback` 0.05 s【最严重】

| 项 | 原版值 | 我们值 | 出处 | 判定 |
|---|---|---|---|---|
| 单候选 ⇒ `Duration = 0` | 原版队列**等完** `AnimInfo.GetAnimDuration()` 才放下一条动作（`BattleManager__ResolveActionFromQueue.c:44-76`、`_ResolveAnim_d__466__MoveNext.c:200-210`） | `regression = onlyOne && !filtered` ⇒ `Duration = 0f` | `Core/VfxMap.cs:1167-1168`、`:1216` | ❌ **数值错** |
| 下游怎么用这个 0 | —— | `Mathf.Max(0.05f, clip.Duration)` ⇒ **0.05 s**：① 喂 `CardAnimPlayback.Duration`（`_currentAnimationTime`，`Card3DAnimationController.cs:252`，`AnimFinished` 提前）；② `ExtendTimelineTail(_clock + at + 0.05f)`（动作队列提前放行） | `Battle/BattleDriver.cs:18655-18657`、`:18698` | ❌ 同上 |
| 受影响卡数 | **任务书写的 9 张 → 实测 16 桶 / 16 卡**（见 §三·4 全表）；其中 `Duration ≥ 0.5 s` 的 **15 张**、`2.2 s` 的是 `Arjac Rockfist` | 全部按 0.05 s 播 | 本审查逐卡 join `cards_engine.json` 的 `animTimes` 与 `cardanim_clips.json`（**两个来源逐卡一致**） | ❌ |

**这一条与「近战动画」的用户反馈直接相关**（12 张是 `attack_melee` 桶）：
近战技能动画的**排队时长**与**卡体动画长度**都被压到 0.05 s，而原版是 0.5–2.2 s。
**建议修法**（**不在本件白名单，交战斗线**）：`regression` 只压 `T`（组偏移），**不压 `Duration`**；
或让 `PlayUnitSkillAnim` 用 `CardAnimClips.DurationOf(clip.Anim)`（同一份 `AnimInfo`）兜底 —— ⛔ 别写死常数。
⚠️ 改之前要先确认「`Duration` 也归零」是 B6 的**有意回归口径**（`VfxMap.cs:1168` 注释说「队尾由事件自己负责」）
⇒ 那就该在 **`PlayUnitSkillAnim` 那一侧**补真值，而不是改 `VfxMap`（**VfxMap 归 Trait 代理，别抢**）。

### ⑤ `vfxDelay` 加两次（潜伏）

| 项 | 原版值 | 我们值 | 出处 | 判定 |
|---|---|---|---|---|
| 延迟怎么算 | `WaitForSeconds(animInfo.vfxDelayTime)` **一次** | `at = t0 + clip.T + clip.Place.VfxDelay`，而 **`clip.T` 里已经含同一次 `vfxDelayTime`** | 双加处 `Battle/BattleDriver.cs:18655`；`T` 的构成 `Core/VfxMap.cs:1197-1228`（`T = t + delay`，`delay = times[idx].Delay`）+ doc `:971-976`；`clip.Place.VfxDelay = r.vd`（`Battle/CardAnimClips.cs:138`） | ⚠️ **结构上确实双加** |
| 今天会不会真的多加 | —— | **不会**：条件 = ①事件是 `UnitAttack`/`UnitDamaged` ②候选钉到一条 `AnimTime` 且 `d > 0` ③同一件 `CardAnim` 在 `cardanim_clips` 里 `vd > 0`。全池 `d > 0` 的**只有 `EC29 Heldrake` 的 `play` 行（0.4 s）**，它**不在**这两桶；`cardanim_clips` 里 `vd > 0` 的只有 2 行，同样都不在这两桶 | `cards_engine.json` 逐条扫 `animTimes[].d`；`cardanim_clips.json` 逐行扫 `vd` | ✅ **今天 0 影响（潜伏）** |
| 触发条件样例 | —— | 若将来某张卡进 `attack_melee` 且 `d = 0.4`、`vd = 0.4` ⇒ **0.8 s** 才起播（原版 0.4 s） | 同上 | —— |

**建议修法**：二选一（**只能留一处**）：删掉 `BattleDriver:18655/18698` 里的 `clip.Place.VfxDelay`，
或让 `VfxMap` 的 `T` 不折算 `delay`（后者会动到 `FireEventAll` 那条**已经正确**的路，不推荐）。
推荐**前者**，并在 `CardAnimClips.VfxDelay` 的 doc 上写明「只给 `DurationOf`/自检复核用，⛔ 别再加进 `T`」。

### ⑥ `MinionScaleByPositionSO`「没导出」= 过期注释（件在本地）

| 项 | 注释写的 | 实际 | 出处 |
|---|---|---|---|
| SO 在不在 | 「那份 SO **没有导出来**（`数据/` 里 0 命中）」 | **在**：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MinionScaleByPosition.json`（`m_Name = MinionScaleByPosition`，`scaleCurve` 2 个关键帧：`t 0.0028→7.70`、`value 0.360→0.700`） | `Battle/WFModuleTransformModifier.cs:74-75`、`:418-421`（实为 `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs`） |
| `GetScale` 在不在 | 「`GetScale` 里那个『全局默认尺度』静态 Vector3 类名未定」 | 方法体**在**：`d:/2/tools/decomp_full/MinionScaleByPositionSO__GetScale.c`；**仍成立的只有那一半** —— 它乘的静态 `Vector3` 取自 `DAT_1842da2a8 + 0xb8`（`+0xc/0x10/0x14`），类名仍未定 | 同上 |
| 受影响效果 | —— | `Ork_Squiggoth_Charge` · `Ork_Trampla_Charge`（`updateScaleByBoardPosition` 2 例，现在 `SkippedBoardScale++` + 警告） | 注释同处 + `WFModuleTransformModifier` 的那段 |

**判定：注释错一半（「没导出」= 假），功能仍缺一半（静态默认尺度）；铁律 5 要求就地改掉注释。**
**建议修法**：先改注释 + 把 SO 路径写进去；功能另立一条账（**判据已到位 80%**，只差静态量、可先用
`Vector3.one` 复现并在注释里如实标「这一档是我们挑的」—— ⛔ 别写成「原版就是」）。

### ⑦ 音效 / 语音 / AnimFX 模块

| 项 | 实测 | 出处 | 判定 |
|---|---|---|---|
| AnimFX 下游钩子 | **11 个钩子里 10 个有赋值**：`WFEffectCards.Resolver:2033` · `ScaleByTarget.CardResolver:2036` · `Tween.CardResolver:2047` · `Collisions.ContextResolver:2063` · `Collisions.ColliderLookup:2074` · `MoveParticlesToTarget.ResolveTarget:2096` · `Cardback.CardbackResolver:2118` · `InstanceParticleAdjacent.OnExecute:2140` · `ChangeMaterial.SetCardMaterial:2157` / `RestoreOriginalMaterial:2162` / `CardTexture:2167` · `PostProcess.OnPostFx:2207` · `ScreenShake.OnShake:2556` + `UnitTweenRuntime.cs:48` 的 `Tween.OnInvoke`；销毁侧逐个置 null（`:3098-3140`） | `Battle/BattleDriver.cs`、`Core/UnitTweenRuntime.cs` | ✅ |
| **缺的第 11 个** | `WFModuleChangeMaterial.MaterialResolver` **声明在有（`:233`）、读在有（`:579`）、赋值 0 处**（全仓 grep） | `WarpforgeVFX/Runtime/WFModuleChangeMaterial.cs:233,579`；`资料/AnimFX_实现与接线.md:167` 却写着「四个全做完」 | ❌ **缺（潜伏）**：今天 0 影响 —— 3 个 `AnimFXModuleChangeMaterial` 模块都带**具体 `material` 资产**、`materialName` 空 ⇒ 走不到 `MaterialResolver`。但一旦出现 `materialName` 的效果就**静默拿不到材质**（有 warning 分支） |
| 特效音效数据 | `animfx_modules.json`：**912 effects / 2346 modules / 941 条 `sounds` 引用**（`sounds` 934 + `exitSounds` 7）；引用 **408 个不同 cue**，在 `animfx_sounds.json`（459 cues）里**缺 0 个**；533 条 clip 引用在 `Resources/Art/audio/sfx/`（516 个文件）里**缺 0 个** | `数据/游戏数据/animfx_modules.json`、`Resources/animfx_sounds.json`（本审查逐条核对） | ✅ 齐全（比 `AnimFXCheck` 的 4 个白名单还干净） |
| 战斗音效 | `battle_sounds.json` **27 条 cue / 36 条 clip**，clip **缺 0**；第 28 格 `changeToActiveAbilityCUE` 原版是空 PPtr（正确地没进表） | `Resources/battle_sounds.json`；`Battle/BattleSfx.cs` 文件头（28 = 16+12 口径） | ✅ 与口径自洽 |
| 语音 | `voice_lines.json` **642 卡 / 1875 条**，clip 在 `Resources/Art/audio/vo/`（1893 个文件）里**缺 0**；`.wav` 后缀 115 条由 `Path.GetFileNameWithoutExtension` 正常吃掉 | `Resources/voice_lines.json`；`Core/VoiceLines.cs:243,578-580` | ✅ |
| `card_vfx_by_card.json` 的 `audio` 字段 | **698 条 / 641 卡**，🔴 **2026-10-23 订正（铁律 5）：这条是错的 —— `audio` 有消费者。** `Core/VoiceLines.cs:153-155` 自己声明了 `RtAudio/RtCard/RtFile`、`:187` 读**同一份** `card_vfx_by_card.json`、`:214-230` 建表，链 = `VoiceLines.TryPickExact`(`:256`) ← `BattleDriver.SpeakFor`(`:5726`)。原判错的因：只看了 `VfxMap.RtCard` 那一份 DTO（`JsonUtility` 忽略未声明键、两份 DTO 各取所需，是 `VoiceLines.cs:150-155` 写明的**有意设计**）。⛔ 别当死字段剪掉。 | `Resources/card_vfx_by_card.json`（`audioCount 698 / audioCardCount 641 / audioNoNameCount 2`）；`Core/VfxMap.cs:637` 的 `RtCard` | ⚠️ **死字段**：不丢播放（这 641 卡**全部**在 `voice_lines.json` 里有，实测 `vfx-only = 0` 张），但它是 388 KB 里的纯冗余，且**会误导下一个会话**以为卡语音走这张表 |
| `AnimFXCheck` 覆盖不到的 | ① 只验 cue **名**在表里（不验 clip 文件存在 —— 今天实测 0 缺，但**回归抓不到**）；② 只对**一个**效果断 `SoundSlotCount > 0`，**不验** `PlaySoundOnTime` 的 `time`/`is2d`/3D/重复次数；③ 注释仍写「除了已知的 4 个 cue」，而 `KnownMissingCues = new string[0]`（**注释过期**，断言本身是严的） | `WarpforgeArena1/Editor/AnimFXCheck.cs:103-144`（①b）、`:31`、`:484` | ⚠️ 三处都是**补强**项，不是现存缺陷 |

### ⑧ ⚠️ **审查期间工作区被另一个代理改了（必须知道，否则上面的逐卡表会对不上）**

本审查的**全部数字取自 HEAD `74f6a99c`**（= 633 升权威已落盘的那一版）。审查进行中
`git status` 出现 **Trait 权威化代理的在飞改动**（未提交）：
`Core/VfxMap.cs`(+34) · `Editor/BattleScene.cs`(+147) · `工具/gen_card_vfx_by_card.py` ·
两份 `card_vfx_by_card.json` · 新增 `Resources/trait_effect_bindings.json`。
它新加了一步 `strip_unbacked_inference`（生成器注释：「**把兜底之类的东西都去掉** —— `inferred-*` 一条都不许留在表里」）。

**它对我这份报告的影响（实测 diff HEAD vs 工作区，运行期那份表）：**
- 运行期 `entryCount` **2694 → 2672**（−22）；多播桶 **仍是 500**（分布不变）⇒ §二·① 的结论不受影响。
- **14 个桶的最优档候选整个变空**（= 这一拍**什么都不播**了）：
  `EC29/attack_melee`(`HeldrakeStrike`) · `SOR8/attack_melee`(`Arco-Flagellant Strike`) ·
  `30007162…/hit`(`Sau_LordOfTheStorm`) · `68909cf0…/hit`(`Jammed Communications damage`) ·
  `a84a5b49…/hit`(`Psychic_Lightning_down_Intense_white`) ·
  **`SOR10`/`SOR21`/`SOR27`/`SOR32`/`trigger`(`PenitenceEffect`)** ·
  `AM30/play` · `SAU3/deploy` · `SAU50/deploy` · `TL72/deploy` · `d8a14171…/deploy`。
- ⇒ §二·② 那 4 条「`unknown` 仍会播」**在这份在飞改动里已经变成「不播」**（口径变了，不是我的判据错）；
  §三·4 那 16 行里 **5 行（Heldrake / SOR8 / Lord of the Storm / Jammed Communications /
  Master Interromancer）已经整桶消失** ⇒ **它们不再播技能动画**，`Duration=0.05` 那条缺陷对它们不再适用
  （对剩下 11 行仍然适用）。

**判定：这不是我判错，是口径在审查期间被改了。** 按铁律 5·b 记在这里，并要求主对话**拍板**：
「`inferred-name` 那 42 张卡（17 条在事件桶里）**整批变安静**」是不是有意的？如果不是，这条比本报告
任何一条都严重（P0）；如果是，请把 §三·4 的 16 行改成 11 行、§二·② 改成「已不播」。

---

## 三、逐卡表（只列被标记的）

### 三·1 新增多播的 50 个桶（`卡 · 桶 —— 播哪些`；前一条是**已钉时间**的，后面是**未钉**的天赋件）

> `[P]` = 钉到了 `CardAnimTime`（有 `T`）；`[—]` = 没钉（`T = 0`、`Duration = 0`）。
> 判据：见 §二·① 那张表（原版会并发播多件）。

**ability（26 桶）**
| 卡 | 阵营 | 播的件 |
|---|---|---|
| Astropath | AM | `Basic_army_Guard Digital`[P] + `ShieldTraitEffect_Idle`[—] + `CamouflageEffect`[—] |
| Tempestus Scion | AM | `Lasrifle_Hotshot_2shots`[P] + `CamouflageEffect`[—] |
| Warlock Skyrunner | SH | `BulletImpact_Shuriken_Short_twinlink`[P] + `FlyingEffect`[—] |
| Shining Spear Exarch | SH | `GainShuriken`[P] + `FlyingEffect`[—] |
| Orian Laratharjos | SH | `GainShuriken`[P] + `StealthEffect`[—] |
| Bright Lance Vyper | SH | `Buff_Purple`[P] + `FlyingEffect`[—] |
| Iskandar Khayon | BL | `Buff_MarkOfTzeentch`[P] + `ShieldTraitEffect_Idle`[—] |
| Librarian | DA | `Trait_GainQuest`[P] + `ShieldTraitEffect_Idle`[—] |
| Ravenwing Talonmaster | DA | `BulletImpact_gatling_1s`[P] + `FlyingEffect`[—] |
| Turyan Ghauze | EC | `MarkOfSlaanesh_enchantment`[P] + `ShieldTraitEffect_Idle`[—] |
| Locus | GSC | `Atk_BulletImpact_Vox-pulse purple`[P] + `StealthEffect`[—] |
| Atalan Jackal | GSC | `BulletImpact_autogun_2shot`[P] + `CamouflageEffect`[—] |
| Canoptek Plasmacyte | SAU | `Sau_HealProtocol`[P] + `FlyingEffect`[—] + `StealthEffect`[—] |
| Annihilation Barge | SAU | `NecronGauss_Tesla_Annihilation Barge`[P] + `FlyingEffect`[—] |
| Ephrael Stern | SOR | `BulletImpact_3shot_pointblank`[P] + `CamouflageEffect`[—] |
| Zephyrim Superior | SOR | `Buff_Sororitas_quick`[P] + `FlyingEffect`[—] |
| Deathwolf Scout | SW | `BulletImpact_1shot_trail`[P] + `CamouflageEffect`[—] |
| Gun Drone | TAU | `BulletImpact_Tau_Pulse Drone Dual`[P] + `FlyingEffect`[—] |
| Marker Drone | TAU | `MarkerlightSource`[P] + `FlyingEffect`[—] |
| Neurotyrant | TY | `Scream_purple_Quick`[P] + `FlyingEffect`[—] |
| Genestealer | TY | `Swarm_Trigger_OnTarget`[P] + `StealthEffect`[—] |
| Sergeant Telion | UM | `Card_Buff_Hand`[P] + `CamouflageEffect`[—] |
| Stormtalon | UM | `BulletImpact_gatling_1s`[P] + `FlyingEffect`[—] |
| Scout Bike | UM | `BulletImpact_2shot_trail`[P] + `CamouflageEffect`[—] |
| Storm Speeder Hailstrike | UM | `GatlingSweep`[P] + `FlyingEffect`[—] |
| Eliminator Sergeant | UM | `BlindEffect`[P] + `StealthEffect`[—] |

**trigger（23 桶）**
| 卡 | 阵营 | 播的件 |
|---|---|---|
| Vitus Gryf | AM | `GuardSummon`[P] + `StrikeEffect`[—] |
| Warp Spider | SH | `Recall_Eldar`[P] + `StrikeEffect`[—] |
| Nuadhu 'Fireheart' | SH | `KhaineBuff`[P] + `FlankEffect`[—] + `StrikeEffect`[—] |
| Howling Banshee Exarch | SH | `Buff_Red`[P] + `StrikeEffect`[—] |
| Maulerfiend | BL | `TraitorsHate_Board`[P] + `ArmourEffect`[—] + `StrikeEffect`[—] |
| Havoc Champion | BL | `Buff_Chaos_Star`[P] + `StrikeEffect`[—] |
| Black Knight | DA | `Trait_GainQuest`[P] + `FlankEffect`[—] |
| Sammael | DA | `CreateCard Gold`[P] + `FlankEffect`[—] + `TalentEffect`[—] |
| Aggressor | DA | `Trait_GainQuest`[P] + `StrikeEffect`[—] |
| Flawless Blade Champion | EC | `Buff EC Pink Twirl`[P] + `EcstasyEffect`[—] |
| Fiend | EC | `CreateCard EC`[P] + `FlankEffect`[—] |
| Kommando | GOFF | `Stealth_Proc`[P] + `StrikeEffect`[—] |
| Acolyte Heavy | GSC | `CreateCard Sabotage`[P] + `StrikeEffect`[—] |
| Achilles Ridgerunner | GSC | `CreateCard Sabotage`[P] + `FlankEffect`[—] + `StrikeEffect`[—] |
| Atalan Leader | GSC | `Basic_army_GSC_Jackal`[P] + `FlankEffect`[—] + `StrikeEffect`[—] |
| Missionary | SOR | `Buff_Sororitas_quick`[P] + `StrikeEffect`[—] + `TalentEffect`[—] |
| Blackmane Inceptor | SW | `HuntMark_User`[P] + `FlankEffect`[—] + `StrikeEffect`[—] |
| Wolf Guard Terminator Pack Leader | SW | `Buff_Blue_SW`[P] + `ArmourEffect`[—] |
| Murderfang | SW | `Buff_Blue_SW`[P] + `ArmourEffect`[—] |
| Fire Warrior Marksman | TAU | `MarkerlightSource`[P] + `StrikeEffect`[—] |
| Mawloc | TY | `Tyranid_Burrow`[P] + `StrikeEffect`[—] |
| Deathleaper | TY | `Rapacious_Hunger`[P] + `StrikeEffect`[—] |
| Mucolid Spore | TY | `Spore Explosion`[P] + `StrikeEffect`[—] |
| Tyrannofex | TY | `AcidSpraySweepAttack`[P] + `StrikeEffect`[—] |

（表里 26+24=50 桶；`trigger` 那栏最后一行 `Tyrannofex` 计入，故 ability 26 + trigger 24 = 50，
与「净 +49」的差 = `SOR8/trigger` 缩水。）

⚠️ **要点**：**59 条天赋候选全都 `[—]`** ⇒ 与主件**同一瞬间**起播。原版天赋动画的 `vfxDelayTime` 也全为 0
（全库 1067 条只有 2 条 `vd > 0`），所以**听感/观感上与原版一致**；但一旦某条天赋动画有了 `vd`，
我们**会静默忽略**（见 §二·① 的建议）。

### 三·2 5 条 `unknown`（见 §二·② 的判定表）

`SOR8`（被裁）· `SOR10`/`SOR21`/`SOR27`/`SOR32`（仍播 `PenitenceEffect`）。

### 三·3 6 条 Shield 变体（见 §二·③）

`EC20` → EC 变体；`TAU17`/`TAU36`/`TAU32`/`TAU19`/`TAU3` → TAU 变体。**件与参数都在本地。**

### 三·4 `Duration` 被压成 0 的 16 个桶（原版真值逐条）

| 卡 | 桶 | `CardAnim` | 原版 `GetAnimDuration()` | 我们现在 |
|---|---|---|---|---|
| Arjac Rockfist (SW26) | attack_melee | `Atk_HammerSlam` | **2.2 s**（0 + 0 + 1.0 + 1.2） | 0.05 s |
| Long Fang (SW16) | attack_melee | `Atk_BulletImpact_Bolter_Wide Area` | **1.8 s** | 0.05 s |
| Heldrake (EC29) | attack_melee | `HeldrakeStrike` | **1.5 s**（`animTimes` 无此行，取自 `cardanim_clips`） | 0.05 s |
| Neophyte Iconbearer (GSC12) | attack_melee | `CreateCard_GSC` | **1.2 s** | 0.05 s |
| Blastmaster Noise Marine (EC2) | attack_melee | `EC Sonic Destroy` | **1.0 s** | 0.05 s |
| Harpy (TL24) | attack_melee | `Tyranid_Spawn_Self` | **1.0 s** | 0.05 s |
| Valtus (UM83) | attack_melee | `Atk_BulletImpact_5shot_gatling` | **1.0 s** | 0.05 s |
| Doomstalker (SAU31) | attack_melee | `Atk_Gauss_Large` | **0.75 s** | 0.05 s |
| Invictor Warsuit (UM70) | attack_melee | `Atk_BulletImpact_2shot_autocannon` | **0.75 s** | 0.05 s |
| Sonic Blaster Noise Marine (EC1) | attack_melee | `Debuff EC Lower Stats` | **0.5 s** | 0.05 s |
| Arco-flagellant (SOR8) | attack_melee | `Arco-Flagellant Strike` | **0.5 s** | 0.05 s |
| Venomthrope (TL11) | attack_melee | `ToxicMiasma_Venomthrope` | **0.5 s** | 0.05 s |
| Master Interromancer | hit | `Master Interromancer Hit` | **0.65 s** | 0.05 s |
| Child of Torment (BL20) | hit | `Buff_DiabolicStrength` | **1.0 s** | 0.05 s |
| Jammed Communications | hit | `Jammed Communications Damage` | **0.5 s** | 0.05 s |
| Lord of the Storm | hit | `Lord_of_the_storm_hit` | **0.3 s** | 0.05 s |
| （不含）Techmarine (DA22) | attack_melee | 2 个候选 | 0.7 + 1.0（`Offset 0 / 0.7`） | ✅ 正确走多候选 |

> **任务书说 9 张，实测 16 张**（`≥0.5 s` 的 15 张 + `0.3 s` 的 1 张）。两个数据源（卡池 `animTimes` 与
> `cardanim_clips`）逐卡一致（唯一差异 `Master Interromancer` 0.65 vs 0.3 —— 他那行是 `inferred-name`、
> 按 prefab 名钉到了另一条同名行，属**另一条**小账：`Duration` 究竟该取哪一条）。

### 三·5 音频缺失

**没有**：`animfx_sounds` 缺 0 cue / 缺 0 clip；`battle_sounds` 缺 0；`voice_lines` 缺 0。
唯一相关的是 `card_vfx_by_card.json` 的 `audio` 字段 —— ⚠️ 原写「无消费者」**已证伪**（见 §逐条表那一行的 2026-10-23 订正）。

---

## 四、按严重度排序的修复清单

### P0（与原版差一个量级 / 直接影响玩家看到的东西；判据已齐）

0. 🔴 **先拍板：`inferred-name` / `unknown` 整档被清出事件桶**（§二·⑧）。若这是在飞代理的有意口径，
   则 14 个桶（含 5 张卡的近战/受击技能动画、4 个 `deploy`、4 条 `Penitence`）**整拍变安静**；
   若不是有意的，回滚那一步比修本报告其余任何一条都优先。**判据齐（diff 已列全）**。
1. **16 张卡的技能动画时长被压成 0.05 s**（§二·④、§三·4）。
   判据齐（原版 `GetAnimDuration()`，两源一致）。修法：**在 `BattleDriver.PlayUnitSkillAnim` 一侧补真值**
   （`CardAnimClips.DurationOf(clip.Anim)`），⛔ 别先改 `VfxMap`（归 Trait 代理）。
   建议先跑 `BattleScene.Run` 看 `UnitSkillAnimsScheduled / UnitSkillAnimEmpty` 有没有现成断言被这个 0 掩盖。
2. **6 张 Shield 播错变体**（§二·③）。判据齐、件齐、参数齐 ⇒ **纯数据**（运行时表的 `n` 换成变体名；
   权威名已在 `authoritativeAnim` 里）。改完要真机看 EC20/TAU17 的 `ability` 那一拍。

### P1（结构上不对 / 会静默；今天或不触发，或需要一次真机确认）

3. **`vfxDelay` 双加**（§二·⑤）：删 `BattleDriver` 里多加的那一项（**只留一处**），并把
   `CardAnimClips.VfxDelay` 的注释改成「只给 `DurationOf` 用」。今天 0 影响，但**改起来三行**。
4. **`WFModuleChangeMaterial.MaterialResolver` 从未赋值**（§二·⑦）：补一个转发到
   `WFModuleChangeMaterial` 的材质查找（原版判据在 `BattleCardUI__SetCardMaterial` 那一族，已在本仓注释里），
   或**明确记为「不做」并写理由**（当前 3 个模块都用具体 `material`）。顺手把
   `资料/AnimFX_实现与接线.md:167` 的「四个全做完」就地订正（铁律 5）。
5. **多播里 59 条天赋候选钉不上时间且静默**（§二·①）：加一条「钉不上就出声一次（限 N 条）」。
   今天行为等价，属**防回归**；不改变任何卡的播放。

### P2（注释 / 冗余 / 补强）

6. `WFModuleTransformModifier.cs:74-75,418-421` 的「SO 没导出」**就地订正**（件在
   `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MinionScaleByPosition.json`；
   `GetScale` 也反编译得出）；`updateScaleByBoardPosition` 的功能另立账（**判据到位 80%**，只差静态默认 `Vector3`）。
7. ~~`card_vfx_by_card.json` 的 `audio`（698 条）无消费者~~ 🔴 **2026-10-23 订正：`Core/VoiceLines.cs` 在读它**（`TryPickExact:256` ← `BattleDriver.SpeakFor:5726`）⇒ 不是死字段，⛔ 别剪；原建议作废。
   要么停掉生成（**别让下个会话以为卡语音走这张表**）。
8. `AnimFXCheck` 补强三处：clip 文件存在性；`PlaySoundOnTime` 的 `time/is2d/3D`；`KnownMissingCues`
   的注释（现值 `new string[0]`，注释却写「已知 4 个」）。
9. （口径待拍板）`unknown` 档要不要「整桶不播」：改则 4 张卡变安静（`SOR10/21/27/32`），
   且与 §二·② 里「卡 SO 那一层其实有依据」相冲突 ⇒ **建议先不动，等特效线/战斗线定口径**。

---

## 五、没查清的部分（如实记，⛔ 不是「查不到就算了」）

1. **天赋多播的「同一瞬间」是否与原版逐位一致**：已证明原版是**逐天赋各自 `StartCoroutine`（并发）**且
   各等自己的 `vfxDelayTime`；但**没有真机**对比「同一帧起播」的观感。缺的条件 = 一次 `BattleScene` 真机录屏
   （`BL17 Warp Talon` 的 `ability` 是最好的样本：`FlyingTraitPlay` + `StealthTraitPlay`）。
2. **`trigger` 桶那 23 桶的「该不该多播」只做到「逻辑上原版会各播一件」**：`Strike` 天赋动画
   （`StrikeTraitTrigger`）在原版是** Strike 那一拍**播的（`CardScript__ActivateTraitAnim` 的调用点没逐条跟完），
   而我们的 `trigger` 桶把「卡自己的触发动画」与「`Strike` 天赋动画」合成一桶 ⇒ **可能多播**。
   判据缺的一半：`Strike` 天赋动画与卡自身 `Strike` 动画在原版**是不是同一次事件**（`RawCardScript.TriggerAbilities`
   的方法体在本地缺）⇒ 建议真机看 `Howling Banshee Exarch` / `Makarfiend`。
3. **`MinionScaleByPositionSO.GetScale` 的那个静态 `Vector3`**（`DAT_1842da2a8 + 0xb8`）类名未定 ——
   要按 RVA→文件偏移读常量（`资料/战斗规则与数值_出处.md` §三），**本件没做**（只读、且超出本范围）。
4. **`cardanim_clips` 与 `animTimes` 对 `Master Interromancer` 给出两个时长（0.65 / 0.3）**：
   需要裁定「`Duration` 取哪一条」—— 属于「钉行规则」，比本件更深一层，本件只如实记。
5. **`SOR8` 的 `trigger` 桶从「播」变成「不播」**：这是本次升权威的**唯一净缩水**，
   逻辑上更对（`unknown` 被权威行裁掉），但**没有任何断言钉住**它 ⇒ 建议特效线补一条。
