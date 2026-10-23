# A7 · 特效线第二批 —— `Start(0)` 十三档接通 + `新账-69` 两条 E 的判据追查（2026-10-23）

> 执行代理 **A7**（特效线第二批）。两件按简报顺序做。
> **没有跑 Unity**（红线）—— 改完两趟秒级类型检查：
> `TMPDIR=/tmp/wf_a7 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（两次均 0）。
> ⛔ 未动 git、未改黑名单里的任何文件；只改了白名单两个 `.cs` + 本报告。

---

## 一、`Start(0)` 那 13 条 trait effect —— ✅ 全部接上（11 条播件 / 2 条原版空引用留空+出声）

### 1·1 原版语义（判据逐句，第一权威 = 全量反编译）

判据文件：`d:/2/tools/decomp_full/BattleCardUI__DisplayStatusAnim.c`

| 事实 | 证据 |
|---|---|
| `DisplayStatusAnim(traitId)` 遍历该 trait 的**全部** `TraitEffect`（`CardTraitCollection.GetTrait(id).TraitEffects`，`:64` + `:70-72`），**按资产顺序** | `:64` `GetTrait(param_2, 0)`、`:70-72` `List.GetEnumerator`、`:79` `MoveNext` |
| **`TriggerType == 0 (Start)`**：拿这条 effect **直接** `BattleManager.GetTraitEffect(trait, triggerType, army)` → `ResolveTraitAnim(...)` → `StartCoroutine` | `:86` `if (*(int *)(local_58 + 0x28) == 0)`；`:104-106` GetTraitEffect（含 army = `cardRawData+0x2c`）；`:112-113` 取当前 trait 值；`:118-119` ResolveTraitAnim + StartCoroutine |
| **这一档没有任何字典 / 守卫 / 回调** —— 对比 `== 2` 那一支才 `TraitFrameEffectsPlay`（`param_1+0x2d0`）`ContainsKey` → 取表 → 归位 → `SetActive(true)`；没有才播、并在**回调**里写表 | `:121-198`（`FUN_18173ee80` = ContainsKey `:126`、`FUN_18174fef0` = get_Item `:132/142/164/175`、写表回调在 `:273-284`） |
| ⇒ **每一次 `DisplayStatusAnim` 调用都会重新演一遍 Start(0)**（不是「一辈子一次」） | 上面的「无守卫」 |
| **`Start(0)` 不过 `playIfWarlord` 门** —— 那道门只在 `DisplayTriggerAnim` | 本函数体内**没有**读 `TraitEffect+0x2c`（`playIfWarlord`）；该门见 `BattleCardUI__DisplayTriggerAnim.c:62-68` |
| **0 与 2 可以在同一次调用里两条都播**（`for` 逐条） | `huntMark` 资产顺序 = `6,0,2,5,1`；`markerlight` = `0,2,5,1`（`Resources/trait_effect_bindings.json` 的 `rows` 顺序） |
| `firstStrike` / `piercing` 的 Start(0) 那条 `effectAnim` 是**空引用** ⇒ 原版那一档什么都不播（`ResolveTraitAnim` 协程拿不到 CardAnim 直接结束） | 数据：两行 `prefab:""`（生成器 `unresolved` 列写明「`TraitEffect.effectAnim` 是空引用」）；`BattleManager._ResolveTraitAnim_d__465__MoveNext.c:118-119`（`effectAnim == null` ⇒ 提前返回） |
| Start(0) 播出来的对象**不进任何表** ⇒ 也**不会被 `RemoveDestroyableAnims` 收**（那个方法只遍历 `+0x2d0` / `+0x2d8` 两本字典） | `BattleManager__RemoveDestroyableAnims.c:44-83`（两处 `DoDestroy` 都只对字典项） |
| 原版调用点（谁触发这一档） | `CardScript__CardSetup.c:193`（230 = `dropPod`）· `CardScript__ActivateTraitsOnSummonOrEnchantment.c:67`（**召唤/附魔时逐条 trait 遍历**）· `CardScript__ActivateTraitAnim.c:25` · `CardScript__Stun.c:125`（100 = `stun`）· `CardScript__AddEffect.c:678`（600）/`:890`（750） |
| 函数尾还有 `flying(390)` / `vanguard(980)` 的 `ToggleFlying` 联动 | `:297-310`；id 值见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:36,95`（本批不动，`FlyingCardController` 已按这条做） |

**与我们原有的两条链不冲突**：`TraitFrameEffectsPlay`（OnPlay 2）与 `TraitFrameEffectsTrigger`（3/6）各自那本字典**不变**；Start(0) 是**第三条、独立的一条**（不进表）。

### 1·2 改动（`Core/TraitFrames.cs`，全是新增/局部改）

| 位置 | 改动 |
|---|---|
| 文件头 `:6-24` | 新增「`Start(0)` 那一档也接了」整段判据（含「不查重 / 不过 `playIfWarlord` / 0 与 2 可同场 / 两条空引用」） |
| `:225-236` | 新增 `_startBook`（trait → 该 trait 的 Start(0) 真值行）＋ `_startEmptyTraits`（原版本档无动画的 trait）—— **明确写清「这不是字典/去重表」** |
| `:310-360` | `EnsureBindings` 分出 `TriggerType == 0` 那一档（能播的进 `_startBook`、空的进 `_startEmptyTraits`）；`noPrefab` 计数现在含这两条（合计 **7**，与文件头「7 条空引用」一致） |
| `:380-386` | 载入日志多报一段「Start(0) 那本 N 个 trait」 |
| `:531-570` | 新增公开查表口：`StartRowCount` / `StartTraitCount` / `StartBindingsFor` / `StartPrefabsFor(trait[, army])` / `StartIsEmptyByDesign` |
| `:618-640` | `Attach(Transform)` 新重载（`Attach(CardView)` 转发到它）—— 生产路径语义不变；自检可以在**不建整张卡**的情况下验播放 |
| `:643-664` | 新增断言探针：`StartPlayCount` / `LastStartPrefab` / `StartPlayCountOf(trait)`（只计**真建出 player** 的） |
| `:719-776` | `DisplayStatus`：**先** `PlayStartRows(key)`（不受 OnPlay 去重影响），**再**走原来的 OnPlay(2) 分支；方法注释补全两档语义与返回值含义 |
| `:778-812` | 新增 `PlayStartRows(key)`：逐条 `PrefabOf(row, army)` → `Play(name)`；**不保存 player**（交给 prefab 自己的 `destroyTime`，与原版「不进表」一致）；空引用那两条**只出声一次**、⛔ 不猜替身 |

⚠️ **返回值语义没变**：`DisplayStatus` 仍只在「**OnPlay 那张持久框这次新挂上**」时返回 `true`
（`SyncKeywords` 的 `added` 依赖它）—— 只播了 Start 的 trait 返回 `false`，这不影响 `BattleDriver`（忽略返回值）
与 `BattleScene.cs:8252` 那条「再同步一次不会重复挂」的断言（它只比 `PlayCount`）。

### 1·3 13 条逐条处置（全部来自 `Resources/trait_effect_bindings.json` 现读）

| # | trait | DefinedTrait | `playIfWarlord` | Start(0) prefab | 处置 |
|---|---|---|---|---|---|
| 1 | `armour` | 1020 | **false** | `ArmourEffect` | ✅ 接上（**这条正好证明「不过督军门」**：`false` 也照演） |
| 2 | `cantAttack` | 150 | true | `CantAttackEffect` | ✅ 接上 |
| 3 | `companion` | 1180 | false | `CompanionEffect` | ✅ 接上 |
| 4 | `fast` | 40 | true | `FastEffect` | ✅ 接上 |
| 5 | `ferocity` | 1265 | false | `FerocityEffect` | ✅ 接上（⚠️ **≠** 它的 FromCode(5) `Ferocity_RecallToDeck`） |
| 6 | `firstStrike` | 630 | true | **（空引用）** | ⛔ **留空 + 出声**（铁律 11 例外①；原版本档什么都不播） |
| 7 | `flank` | 460 | true | `FlankEffect` | ✅ 接上 |
| 8 | `huntMark` | 1260 | true | `HuntMark_StackEffect` | ✅ 接上（同一 trait 另有 OnPlay(2) `HuntMark_IdleEffect`，**一次调用两条都播**） |
| 9 | `markerlight` | 1170 | true | `MarkerlightProcEffect` | ✅ 接上（另有 OnPlay(2) `MarkerlightIdle`） |
| 10 | `pack` | 610 | true | `Pack_WhileInPlay` | ✅ 接上（⚠️ 原版 `preventDestroy = 1`，见 §1·6） |
| 11 | `piercing` | 900 | true | **（空引用）** | ⛔ **留空 + 出声**（同上） |
| 12 | `rally` | 10 | true | `RallyEffect` | ✅ 接上 |
| 13 | `tide` | 910 | true | `TideEffect` | ✅ 接上 |

### 1·4 断言（`WarpforgeArena1/Editor/AnimFXCheck.cs` 新增局部函数 `CheckTraitStartEffects()`，⑧ 组 14 条 `Check`）

> 期望值**一律从 `trait_effect_bindings.json` 现算**（⛔ 不从 `TraitFrames` 读回 = 不自证）。
> 运行时那段用一个 `new GameObject("A7_Start0Probe")` + `TraitFrames.Attach(transform)` 的**探针宿主**，
> 收尾 `Kill()` 掉所有 player + `DestroyImmediate(host)`。

| # | 断言 | 怎么让它真红（灭自证） |
|---|---|---|
| ⑧-1 | 前提：`TriggerType == 0` 共 **13** 条 = 有动画 11 + 空引用 2（`firstStrike`/`piercing`） | 生成器漏读 / 混档 ⇒ 张数变 |
| ⑧-2 | `TraitFrames.StartRowCount` / `StartTraitCount` 真的载进来（13 / 11） | 不分这一档 ⇒ 0 |
| ⑧-3 | **逐条**：11 条的 `StartPrefabsFor(trait)` 都等于数据里那一条（对不上 0 条） | 按名字硬绑 / 拿 FromCode 那件顶替 ⇒ 对不上 |
| ⑧-4 | `firstStrike`/`piercing`：`StartIsEmptyByDesign` 真、`StartPrefabsFor` 空 | 猜替身 ⇒ 红 |
| ⑧-5 | 资产顺序：凡同时有 0 与 2 的 trait，**0 都在 2 前面**（2 个） | 数据顺序一变先红（否则「先 Start 再 OnPlay」不再等价） |
| ⑧-6 | **确实播了**：11 个 trait 逐个 `DisplayStatus`，每个 `StartPlayCountOf==1` 且最后一件 = 数据里那件（11/11） | 不接这一档 / 只登记不播 ⇒ 红 |
| ⑧-7 | 汇总 `StartPlayCount == 11`（只计真建出 player 的） | 库里缺件（`Play` 返回 null）就不计 ⇒ 不会空转绿 |
| ⑧-8 | 空引用那两条**调了 `DisplayStatus` 也 0 次** | 兜底替身 ⇒ 红 |
| ⑧-9 | **灭自证（不去重）**：`DisplayStatus("armour")` 再调一次 ⇒ Start 次数 1→2，**而 `PlayCount` 不变** | 🧨 把 Start 塞进 `_play` 那本（最省事的错写法）⇒ 第二次被去重挡住、红 |
| ⑧-10 | **灭自证（0/2 共存）**：`huntMark` 一次调用 ⇒ Start `HuntMark_StackEffect` **与** OnPlay `HuntMark_IdleEffect` **都播**、且两件**不相等** | 🧨 写成「有 OnPlay 就只走 OnPlay」⇒ 红 |
| ⑧-11 | **灭自证（两条链别串）**：`ferocity` Start = `FerocityEffect`、FromCode = `Ferocity_RecallToDeck`、两者不等 | 🧨 把名字最像的 `FerocityEffect` 塞进 FromCode ⇒ 红 |
| ⑧-12 | 反向：`stomp` **没有** Start(0) 行、也不是「空引用」。 | 🧨 把 FromCode 那 10 条也当 Start 播 ⇒ 红 |
| ⑧-13 | **灭自证（`playIfWarlord` 不挡 Start）**：`armour` 那条 `playIfWarlord=false` 却照样播了 `ArmourEffect` | 🧨 给 `DisplayStatus` 加 `BlocksForWarlord` ⇒ 红 |
| ⑧-14 | 探针前提：`TraitFrames.Attach(Transform)` 挂出 `effectsAnchor` | —— |

⚠️ **本批没跑 Unity**（红线）：期望值已用 `python -I` 对 JSON 逐条静态核过
（13 条 / 11 有件 / 2 空 / 逐条 prefab 名 / 0 与 2 的资产顺序），逻辑上应全绿；
**实际绿红以主对话跑 `-executeMethod AnimFXCheck.Run` 为准**。

### 1·5 没查清 / 已知边界

1. **`DisplayStatusAnim` 的「重复调用」我们复现不全**：原版那 5 个调用点里，`Stun`（每次被晕都调一次）、
   `CardSetup`、`AddEffect` 都是**事件驱动**；我们的入口只有 `TraitFrames.SyncKeywords` 的**集合对差**
   （只在新关键词出现时调 `DisplayStatus`）。⇒ 「同一个关键词再次施加（如再晕一次）」**不会重播 Start**。
   要补齐得在 `Battle/**`（`BattleDriver` 的事件流）上补调用点 —— **不属本批白名单**，只记。
2. **`_prev` 是「每个 CardView 实例」一份**：视图被复用到另一张卡时，若关键词集合相同则不重播
   （原版是另一套生命周期）。今天 `BattleDriver` 按卡重建视图 ⇒ 无实际影响，但**没有断言盯着**。
3. `MarkerlightProcEffect`（`preventDestroy=1`, `destroyTime=3.0`）与 `Pack_WhileInPlay`（`preventDestroy=1`,
   `destroyTime=0`）**永远不会自毁**（`WarpforgeEffectPlayer` 对 `preventDestroy` 取 `Infinity`）。
   它们跟着卡一起销毁 —— 这与原版一致（原版 Start(0) 那条**不进任何表**，`RemoveDestroyableAnims`
   只遍历两本字典），但**如果同一张卡反复走 `DisplayStatus`，会在卡上叠多份**。原版同样会叠（同一条判据）
   ⇒ 照抄、不改；只在这里如实记。

---

## 二、`新账-69` 的 E 两条判据 —— 查证结果（一件**闭合一半**、一件**仍未闭合**）

> 只查不改（数据在 `d:/4/Unity/资料/...` 与 `d:/2/`）。
> 两条 E 的本质是「原版渲染 vs 我们渲染」的**亮度比**，最终判定必须跑 sweep（Unity）——
> 本代理不能跑。**但下面两条是把「用哪一份数据、哪一趟 sweep」这件事查实之后能直接改判的**。

### 2·1 先说一件**全局过期**（两条都受影响）

- `特效还原台账.tsv`（`资料/`，mtime **2026-10-21 22:00**）里这两行来自 **1021 那趟 sweep**。
  而 `资料/比对基线/sweep_orig.tsv` / `sweep_exp.tsv`（mtime **2026-10-22 18:35/18:42**）是**更新的一趟**
  —— 用 md5 核过：它们与 `_tmp_view/w65/sweep_{orig,exp}_1022_post.tsv` **逐字节相同**
  （`eafc6629…` / `a0993973…`）⇒ **台账落后基线一趟**。
- 更要紧：本批要看的两个 prefab
  `WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle Necron Variant.prefab` / `… TAU Variant.prefab` / `…Idle.prefab`
  的 mtime 是 **2026-10-23 21:23**（= **今天重导过**），
  `EnvironmentalCondition Sororitas Raging Storm.prefab` 同批。
  ⇒ **1021 / 1022 任何一趟的读数都不对应「现在盘上这份 prefab」** —— 这是两条 E 共同的前提问题。

### 2·2 `ShieldTraitEffect_Idle Necron Variant`（台账 `E` 1.42 / `|ln|` 0.353）—— **判据闭合：E 是「整族一起漂」的运行级效应，不是这件资产的缺陷；最新基线对里它已经是 Z**

用 `资料/比对基线/sweep_{orig,exp}.tsv`（1022_post，最新一趟）现算 `sum` 比值：

| t | 0.15 | 0.30 | 0.50 | 0.75 | 1.00 | 1.50 | 2.00 | 3.00 |
|---|---|---|---|---|---|---|---|---|
| **Necron Variant**（1022） | 1.029 | 1.023 | 1.004 | 1.008 | 1.007 | 1.007 | 0.990 | 0.989 |
| Necron Variant（1021_pre，同一份脚本现算） | 1.183 | 1.165 | 1.288 | 1.543 | 1.446 | 1.402 | 1.773 | 1.896 |
| **TAU Variant**（1022） | 0.978 | 0.977 | 0.922 | 0.878 | 0.884 | 0.905 | 0.863 | 0.840 |
| TAU Variant（1021_pre） | 1.206 | 1.191 | 1.348 | 1.598 | 1.400 | 1.340 | 1.633 | 1.780 |

- **1022 那趟 Necron 全时段 0.99–1.03**（`|ln| ≤ 0.03`，远在 0.7–1.4 亮度带内）⇒ 按台账自己的判据应记 **Z**。
- **1021 那趟两件变体同向同幅**（Necron 1.18–1.90 / TAU 1.21–1.78，差 ≤ 3.9%）——
  这正是 W61 「TAU 1.37 vs Necron 1.42 只差 3.9%」的由来。
  ⇒ **同一趟里两件「不同变体」一起偏亮**，下一趟又一起回到 1.0 ⇒ 这是**运行级 / 尺子级**的偏移，
  **不是某一件资产的导出缺陷**（否则不会两件同步）。
- 数据侧再加一条：`Lightning Burst`（原版 `ParticleSystem_2564568966103877692.json`）的
  `lengthInSec 0.5 / looping false / startLifetime 0.1·0.05 / startSpeed 0 / startSize 3·2 /
  startColor min(1,0.9219,0.2972,0.3059) max(0.6824,0.8796,1,1) / rateOverTime 10 / maxNumParticles 1000`
  与**我们 prefab 里同名 PS**（`ShieldTraitEffect_Idle Necron Variant.prefab` 的 `PS 6600441922531728978`）
  **逐项相同**；原版 `ShapeModule.type = 18`（矩形）⇒ 那条「`m_Mesh` guid 全 0」确实是**死字段**（与 `W61b` 一致）。

**还差什么（要闭合它只差一步）**：
① 用**当前（10-23 21:23）那份 prefab** 重扫一趟 Necron + TAU + 基件三件，看是否仍是 1.00；
② 若仍偏，先查 `ShieldTraitEffect_Idle` **整族**（5 个变体）是不是同向 —— 同向就别当单件缺陷。
（判据阈值与工具：`analyze_sweep.py` 的 0.7–1.4 亮度带。）

### 2·3 `EnvironmentalCondition Sororitas Raging Storm`（台账 `E` 0.65 / `|ln|` 0.425）—— **仍未闭合，但已把可能面从「导出数据」缩到「渲染侧/原件 shader 表现」**

**(a) 两趟 sweep 方向一致、幅度接近** ⇒ **不是抖动**（对照组：Necron 那件两趟翻了个方向）：

| t | 0.15 | 0.30 | 0.50 | 0.75 | 1.00 | 1.50 | 2.00 | 3.00 |
|---|---|---|---|---|---|---|---|---|
| 1021_pre（orig/exp `sum` 比） | 0.973 | 0.972 | 0.949 | 0.547 | 0.571 | 0.602 | 0.635 | 0.673 |
| 1022_post | 0.993 | 0.990 | 0.983 | 0.758 | 0.731 | 0.711 | 0.699 | 0.691 |

**(b) 导出数据逐项与原件一致**（本批新查，判据都在 `assets_full` 里）：

| 检查项 | 原版（assets_full） | 我们 | 结论 |
|---|---|---|---|
| `Smoke Traily Background` 实例数 | 4 | **4**（`grep -c "m_Name: Smoke Traily Background"`） | ✅ |
| PS 参数（`lengthInSec 5 / looping 1 / prewarm 1 / simulationSpeed 0.5 / maxNumParticles 7 / rateOverTime 0.1 / startLifetime 8·10 / startSize 10 / shape type 18 angle 25 scale(1.48,1.85,1)`） | `ParticleSystem_7241470991743629147.json` | 逐项相同 | ✅ |
| 4 个实例的 `startColor` **各不相同** | 原件里确有 `maxColor` = (0.3647,0.5059,0.6588) 与 (0.6038,0.5888,0.5212) 等多种 | 我们 (1)/(3)/基件 = 蓝灰、**(2) = (0.6038,0.5888,0.5212)** | ✅（**不是导出错**，原件本来就有这个暖色变体） |
| `ParticleSystemRenderer` 自定义顶点流 | `m_UseCustomVertexStreams 1` / `m_VertexStreams [0,1,3,4,23]` | prefab 里 `m_VertexStreams: 0001030417` = 0,1,3,4,23 | ✅ |
| 材质 `Sand Storm`（原版 `Material_164117948008697147.json`，在 `duplicateassetisolation` 包里） | `_Alpha_Modifier 0.459`、`_Alpha_Modifier_Multiply 5`、`_Color_Mutliplier 1`、`_SrcBlend 5 / _DstBlend 10 / _ZWrite 0 / _Cull 2 / _ZTest 4`、`queue 3000`、`_PrimaryTex_Scale_XY_Speed_ZW (1,1,0,0)`、`_SecondaryTex_Scale_XY_Speed_ZW (1,1,-0.1,0)`、`_SecondaryTex` 有绑定 | `WarpforgeVFX/Materials/Sand Storm.mat` **逐项相同** | ✅ |
| **`_SrcBlendAlpha`** | **1**（One） | **5**（SrcAlpha） | ⚠️ 唯一数值差异 —— 但**是死值**：原 shader 的 pass0 `rtSeparateBlend = False`（`Shader_6307360966591251699.json` 的 `m_ParsedForm.m_SubShaders[0].m_Passes[0].m_State`）⇒ `_SrcBlendAlpha` **不参与混合** ⇒ **对画面零影响** |
| 其余「差异」 | `_BaseColor/_Color/_EmissionColor/_Metallic/_Smoothness/…` | 我们没有 | ✅ **Standard shader 残留值**，`Alpha Mask One Layer` 的属性表里**没有**这些名字（`m_PropInfo.m_Props` 24 项逐条核过）⇒ 死值，不该搬（与本工程既有规矩一致） |

**(c) 结论与还差什么**：
- 粒子 / 渲染器 / 材质**三个层面逐字段与原件一致**，且 `Alpha Mask One Layer` 在 `UseOriginal` 白名单（走原件 shader）
  ⇒ **「导出丢了东西」这条可以排除**。
- 那么 0.55–0.76 的尾部偏差只剩两类可能：**① 原件 shader 在我们的渲染环境里的表现**
  （原版是 URP + 后处理/曝光，我们是自建管线；`_Alpha_Modifier 0.459 × 5 = 2.295` 这种**过 1 的 alpha** 在不同
  管线/色彩空间下的钳制与叠加结果可能不同）；**② 环境/曝光**（同一次 sweep 里其它效果对得上，说明不是全局曝光 ——
  但尾段是**低亮度大面积**，对曝光/后处理最敏感）。
- **要闭合必须跑**（本代理不能跑 Unity）：
  ⓐ 单效果两跑（同一份当前 prefab，`WFSWEEP_SIDE=both`）确认 0.69–0.76 可复现；
  ⓑ `WFBIND_FORCE_BUILTIN="Alpha Mask One Layer"` 与 `"Extra Color"` **分开** A/B（W61 已做了一半：normal 0.349 → builtin 0.031，
     说明**走原件反而更亮**，即我们能拿到的最好状态仍是 0.65–0.7）；
  ⓒ 若 ⓑ 仍偏暗 ⇒ 按 PS 逐个关（先把 4× `Smoke Traily Background` 逐个隐掉看尾段由谁贡献），
     再对 `_Alpha_Modifier` 做一次**受控改值**（0.459 → 1.0）看方向 —— 这是唯一还没被数据排除的活属性。

---

## 三、顺手发现（⛔ 没越界改，交主对话分流）

1. 🔴 **`特效还原台账.tsv` 落后基线一趟**：台账 mtime 10-21 22:00，而 `资料/比对基线/sweep_{orig,exp}.tsv`
   是 10-22 18:35/18:42（md5 与 `w65/sweep_*_1022_post.tsv` 相同）。按 1022 那趟，**Necron 的 `E 1.42` 已经不成立（1.00）**。
   ⇒ 建议主对话**用 1022 那对重跑一次 `analyze_sweep.py`** 刷新台账（我没有工具线的写权限，也没跑）。
2. 🔴 **prefab 今天（10-23 21:23）又重导过**（`WarpforgeVFX/Prefabs/ShieldTraitEffect_Idle*.prefab` 与
   `EnvironmentalCondition Sororitas Raging Storm.prefab`）⇒ 台账/`新账-69` 里「未重导」「z 1.183」这些描述
   **全部过期**。任何 sweep 结论在此之前都要重跑。
3. ⚠️ `Sand Storm.mat` 的 `_SrcBlendAlpha = 5`（原版 1）—— **已查明是死值**（原 shader pass0 不分 alpha 混合）。
   但它是「材质搬运器按自己口味补默认值」的一个痕迹（原版没设过这个值，我们补了 5）。**要不要改成 1 由主对话定**
   （对画面零影响，属洁癖项；构件在 `WarpforgeVFX/Materials/` 与导出器，不属本批白名单）。
4. 💡 `TraitFrames.Attach(Transform)` 这个新重载**可以给别的线复用**：任何「不是 CardView 但要有状态框/特效挂点」的
   宿主都能直接挂（`ArmyId` 会自动退 `-1`）。
5. 💡 原版 `BattleCardUI__DisplayStatusAnim.c:297-310` 的 `flying(390)` / `vanguard(980)` 联动是**函数尾**无条件跑的
   （与 triggerType 分支无关）；`DefinedTrait.cs:36,95` 已核过 id。我们 `FlyingCardController` 走的是另一条链，
   **本批没有动**，这里只留坐标。
