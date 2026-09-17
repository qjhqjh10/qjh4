# AnimFX 18 类 · 方法体解读（块1：1–9 类）

> **2026-09-17 派子代理产出（只读 + 只写本文件）**。本块 = `资料/AnimFX_18类成表.md` **行文类序的前 9 类**：
> ① `AnimFXModuleBase` ② `AnimFXController` ③ `AnimFXModuleScreenShake` ④ `AnimFXModuleScaleByTarget`
> ⑤ `AnimFXModuleTween` ⑥ `AnimFXModuleCollisions` ⑦ `AnimFXModuleTransformModifier`
> ⑧ `AnimFXModuleChangeMaterial` ⑨ `AnimFXModulePostProcess`。
> ⚠️ **块边界提醒**：成表的表里只列了 17 类（**漏了 `AnimFXModuleDestroyInTime`**，它只在 Base 那格被提到）。
> 若是按「建议重写顺序」切块，块2 会含 `DoDestroyAnimation/Event/PostProcess/TransformModifier/MoveParticlesToTarget`。
> **汇总时若要换口径，按类重取即可 —— 下面每类都是自成一段的。**

**数据源（全部可复查）**

| 用途 | 出处 |
|---|---|
| 方法体 | `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/AnimFX*__*.c`（18 类共 86 个，**本块用到 57 个**） |
| **嵌套类方法体**（成表以为拿不到的那些） | 🆕 **本轮补的** `d:/2/tools/decomp_animfx_nested{,2}/`（14+4 个，清单见 §附A）。跑法 = `资料/反编译工具链_重建记录.md` §三·五（复用 Ghidra 工程 `-noanalysis`，几分钟） |
| 字段名与偏移（本文每个 `0x??`） | `d:/2/tools/il2cpp_out/dump.cs`（Il2CppDumper，含偏移） |
| 真实参数分布 | `d:/4/Unity/数据/游戏数据/animfx_components.json`（912 效果 / 2346 组件，本文件数字都是现算的） |
| 字符串（报错原文、shader/属性名） | `d:/2/tools/all_strings.txt`（26507 条）解析 |
| 浮点常量 | 从 `d:/2/unity_run_ref/GameAssembly.dll` 按 RVA 读 4 字节（PE 节表换算） |

**读之前必知的三条（都是坑）**
1. ⚠️ **反编译的"函数名"会错位**：`AnimFXModuleBase__Exit.c` / `__DoDestroy.c` 里函数签名叫 `NetworkingPeer__OnMessage`。
   **方法体是真的**（就是一个 `ret`），名字是 Ghidra 对空函数的重命名残留 ⇒ **认文件名，不认函数名**。
   📋 **名字错位不止这一处**：块2 文件 `资料/AnimFX_18类方法体_块2.md` §0 列了完整对照表（`NetworkingPeer__OnMessage` 在**带 2 个参数**时 = 枚举器 `Dispose()`、
   `System_Threading_Tasks_TaskAwaiters__ForceAsync` = 结构体 getter 返回值 `ps.shape`、`PhotonHandler___ctor` = 空构造……）—— **本块也用到了同一套读法**，不重复列表。
   🔴 本块特别用到的两条：`FUN_1803f47a0()` = **IL2CPP 的 null 检查抛异常**（读到它就说明那条分支会 `NullReferenceException`，不是"返回"）；
   `FUN_1803f4790()` = 数组越界检查。
2. 🔴 **`AnimFXModuleBase.Exit()/DoDestroy()` 是空实现**：模块**不重写 = 什么也不做**。
   「每个模块都会在 Exit 时做事」这个印象不成立 —— 18 类里**只有 5 个重写了 `Exit`**（`Animation`/`DestroyInTime`/`Event`/`ChangeMaterial`/`TransformModifier`），**只有 1 个重写了 `DoDestroy`**（`DoDestroyAnimation`）。
3. 🔴 **controller 不看 `actionStart`**：它**无条件**对所有模块调 Initialize/Exit/DoDestroy；
   **要不要响应由各模块自己读自己的 `actionStart`**（`ChangeMaterial`/`DestroyInTime`/`Event`/`DoDestroyAnimation` 是这么用的）。

---

## 1. `AnimFXModuleBase` ‖ 生命周期契约基类（abstract，0 实例） ‖ 4 个方法体
字段：`0x20 actionStart` · `0x28 animFXController` · `0x30 isRetaliation`

| 方法 | 方法体在做什么 |
|---|---|
| `.ctor` | 空（只调 `Behaviour..ctor`）⇒ **基类没有给任何字段默认值** |
| `Initialize(AnimFXController)` | ① `animFXController = 参数`；② `isRetaliation = BattleManager.Instance.IsPlayerTurn() ? !actingCard.isPlayer : actingCard.isPlayer`。 `actingCard.isPlayer` = `EntityScript.<isPlayer>k__BackingField`（`CardScript+0x40`，dump.cs 实证）⇒ **等价于 `IsPlayerTurn() XOR actingCard.isPlayer`** =「出招的那张卡不属于当前回合方」= 这是不是一次反击。③ **不读 `actionStart`**；`Initialize` 是**虚方法，被 controller.SetData 统一调用** |
| `Exit()` | **空**（`ret`） |
| `DoDestroy()` | **空**（`ret`） |

**真实数据**：无实例（abstract）。913 个模块实例的 `actionStart`：`0` 占 910；`DestroyInTime` 1 例 `=5`、`ChangeMaterial` 1 例 `=15` ⇒ **Exit/DoDestroy/Manual 三个枚举值在出货数据里几乎没用过**。
**Unity 侧重写**：新建 `EffectModuleBase : MonoBehaviour`（放 `Assets/WarpforgeVFX/Runtime/`），只为承载 `Initialize(controller)` 契约 + `actionStart` 调度。
`isRetaliation` 我们这边**没有对应物**（要新建：判断「这张卡是不是当前回合方的」）。
**依赖**：被 15 个模块继承；`AnimFXModuleEvent` 直接用 `ActionStart` 当「何时触发」的枚举（块2）。

## 2. `AnimFXController` ‖ 生命周期本体 / 模块宿主 ‖ 9 个方法体
字段：`0x20 sounds[]` · `0x28 exitSounds[]` · `0x30 preventDestroy` · `0x34 destroyTime` · `0x38 modules:List` · `0x40 exitDestroyTime` · `0x48 OnFinished:Action` · `0x50 actingCard` · `0x58 targetCard` · `0x60 currentTime` · `0x64 exiting` · `const SAFE_DESTROY_TIME=3`

| 方法 | 方法体在做什么 |
|---|---|
| `.ctor` | `destroyTime = 4.0f` · `exitDestroyTime = 3.0f` · `modules = new List<>()`（IL 里字段初始化在 base ctor 之前，正常） |
| `OnEnable` | `currentTime = 0`；`if (destroyTime > 0 && !preventDestroy) Object.Destroy(gameObject, destroyTime)` ⇒ **自动销毁走的是 Unity 的延迟销毁，不是自己计时** |
| `SetData(actingCard, targetCard)` | 存两张卡 → **`foreach (modules) module.Initialize(this)`**（虚调，vtable `+0x178`）⇒ **模块的 Initialize 全部从这一处发** |
| `Update` | `currentTime += Time.deltaTime`；`foreach (sounds) PlaySoundOnTime.Update(currentTime, transform)`；**`if (exiting)` 再对 `exitSounds` 跑同一遍** |
| `PlaySound(AudioCue)` | `SoundManager.Instance.Play3D(cue, transform.position)` |
| `Exit()` | ① `OnFinished?.Invoke()`（**在模块 Exit 之前**）② `foreach (modules) module.Exit()` ③ `exiting = true` ④ `if (!preventDestroy) Object.Destroy(gameObject, exitDestroyTime)` |
| `DoDestroy()` | `foreach (modules) module.DoDestroy()`；**若一个模块都没有 → 兜底 `Destroy(gameObject)`**；**有模块时 controller 不销毁**（`bVar1` 在循环体里被置 true）。🔴 这不是我读错：`AnimFXModuleDoDestroyAnimation.DoDestroy()` 里就是 `Animation.Play(endAnimationName)` + `Destroy(gameObject, timeToDestroy)` ⇒ **销毁权交给模块**（"播完退出动画再销毁"），controller 只在没模块时兜底 |
| `get_ActingCard` / `get_TargetCard` | 直接返回字段（`0x50` / `0x58`） |

**触发源（实测调用点，印证 W 组「触发源是动画挂点」）**：`CardScript.PlayTriggerAnim` → 实例化 prefab + `SetData`；`BattleCardUI.CleanStatusAnims`/`RemoveStatusAnim` → `Exit()`；`BattleManager.RemoveDestroyableAnims` → `DoDestroy()`。
**真实数据**：990 实例 / 912 效果；`destroyTime` 众数 `4.0`=248 · `3.0`=133 · `1.8`=115（0 值 38）；`exitDestroyTime` **3.0=956**（其余散到 15.0）；`preventDestroy` 1=**97**；`sounds` 非空 854（其中 786 只有 1 条）；`exitSounds` 非空仅 **7**。
**Unity 侧重写**：**我们已经有三分之二** —— `Assets/WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs` 已实现 `destroyTime/exitDestroyTime/preventDestroy/OnFinished/Tick/Exit`，`WFEffectEntry` 也带着这些字段。**缺的三件**：① `SetData` 那层的**模块分发**（现在只 Play 特效，没有模块概念）② **`sounds`/`exitSounds` 全工程 0 命中**（`PlaySoundOnTime` 要新建）③ `Exit()` 里 **`OnFinished` 必须"立刻、且在模块 Exit 之前"发** —— 我们现在的实现是**在 `DestroyNow()` 里发**（= `exitDelay` 3 秒之后，见 `WarpforgeEffectPlayer.cs:245`），而且**全工程没有任何订阅者**（`OnFinished` 只在那一处 Invoke）⇒ 接模块时按原版把它挪到 `Exit()` 开头（先 `OnFinished` 再 `modules.Exit`）。
⚠️ 我们 `WFEffectPlayer.SafeDestroyTime=3f` 注释写「原版 SAFE_DESTROY_TIME」—— 但**这 9 个方法体里没有任何一处用到这个常量**（只在 ctor 里给 `exitDestroyTime` 赋了同一个 3.0f），**出处对不上，别当原版语义用**。
**依赖**：`modules` 就是 15 个模块；`PlaySoundOnTime.Update(currentTime, transform)` 的语义见下（§附B）。

## 3. `AnimFXModuleScreenShake` ‖ 屏震（最大覆盖面） ‖ 4 个方法体
字段：`0x38 cameraShakes[]` · `0x40 manualTriggerCameraShakes[]`（元素类 `OverwriteCameraShakePreset`）

| 方法 | 方法体在做什么 |
|---|---|
| `OnEnable` | `foreach (cameraShakes)`：**preset 引用非空就 `preset.Play()`**。⇒ **这是 MonoBehaviour 回调，跟 controller 无关**：特效 prefab 一被启用，自动档屏震就播 |
| `AnimEventDoShake(int i)` | 取 `manualTriggerCameraShakes[i]`；**越界静默 return**；`GetModifiedPreset()` → `CameraShakePreset.Play()`；preset 空 → `CustomDebug.LogError("Can't play null screen shake preset")` |
| `TriggerCameraShake(int i)` | 取 `cameraShakes[i]`（**自动档**）；**越界 → `Debug.LogError(string.Format("Can't play desired screen shake index ({0}) on {1}", i, gameObject.name))`**；其余同上 |
| `.ctor` | 空 |

**屏震的下游（本轮一并读了，块外但语义必需）**：`OverwriteCameraShakePreset.Play → GetModifiedPreset()`：从 `CameraShakerManager` 的预设**池**里拿一个空闲项（**池上限 30，满了 `LogWarning` + `IncreaseCameraShakePresetPoolSize()`**，再满就警告"不再扩容，返回第一个"），把 6 个 `overwrite*` 开关**逐项**覆盖到 `presetSO.preset` 的对应字段（`delay/sustainTime/amplitude/frequency/direction/attackTime/decayTime`），`Play()` = `CameraShakerManager.DoShake(sustainTime, attackTime, decayTime, amplitude, frequency, direction, delay, rawSignal)` + `StartCoroutine`（`CameraShakePresetSO.rawSignal` 是 **Beautify 的 SignalSourceAsset 波形**）。
**真实数据**：434 / 416 效果；`cameraShakes` 有值 278（1 个=246）；`manualTrigger` 有值 166；508 条 preset 指向 **14 个** presetSO。
**Unity 侧重写**：我们已有 `CardFeel.ShakeCamera(camera, hudRoot, worldAmp, delay)`（**相机与 HudRoot 同向平移**，2026-09-17 定案）⇒ 把 `OverwriteCameraShakePreset` 的 **6 个覆盖开关 + delay** 抄成数据类 `ShakePreset`，模块只负责「在 OnEnable / 动画事件时调我们的 ShakeCamera」。
🔴 **manualTrigger 那一条要有"动画事件"通道才跑得起来**：`AnimEventDoShake(i)` 是给**动画片段的事件**调的（原版 AnimationClip 里挂 AnimEvent——W 组的结论）。我们若不做动画事件，就只做自动档。
**依赖**：`CameraShakerManager`（池语义）、`CameraShakePresetSO`（波形）、`tween/Shake*.json`（21 个 preset 资产，14 个被引用）。

## 4. `AnimFXModuleScaleByTarget` ‖ 粒子按目标卡尺寸缩放 ‖ 3 个方法体
字段：`0x38 particleSystems[]` · `0x40 doParentRelation` · `0x48 particleSystemsShapeAngle[]`

| 方法 | 方法体在做什么 |
|---|---|
| `Initialize(controller)` | ① base.Initialize；② **`actingCard` 与 `targetCard` 必须都非空**，否则 `LogError("[ERROR] Can't find target or acting card in an AnimFXModule of prefab " + name)` 后 return；③ 逐分量算 `ratio = targetCard.localScale / actingCard.localScale`（除数为 0 会出 Inf，未做保护）；④ `foreach (particleSystems)`：**引用的粒子系统为空 → `LogError("Missing particle reference in " + name)`**；否则 `ps.transform.localScale *= ratio`，**若 `doParentRelation`** → `ps.gameObject.AddComponent<ParentConstraint>()` + `Init(targetCard.GetEffectAnchor())` + `ToggleParentingOptions(true,true,false)`（`ParentConstraint.ToggleParentingOptions(position, rotation, scale)` = 三个继承开关，dump.cs 实证）；⑤ `particleSystemsShapeAngle.Length >= 1` → `ChangeShapeAngle()` |
| `ChangeShapeAngle()` | 对 `particleSystemsShapeAngle` 每个：读 `ps.shape.angle`（度）→ `× Deg2Rad` → `tanf` → 与 `targetCard.transform.localScale`（以及 `BattleParticleColliderManager` 的 `playerMinionCollider(0x28)` / `enemyMinionCollider(0x40)` 两个位置）一起 `atan2f` → `× Rad2Deg` → 写回 `shape.angle`。常量已从 DLL 读出：`1.0f` · `Deg2Rad=0.017453292` · `Rad2Deg=57.29578`；两个内联是 `tanf` / `atan2f`（本轮补的反编译）。🔴 **精确操作数配对没能逐位还原**（Ghidra 把参数丢在寄存器里，两个位置的读数也没落到可见变量）⇒ **别照猜实现，按语义自己定** |
| `.ctor` | 空 |

**真实数据**：314 / 314 效果（第二大）；`particleSystems` 1 个=226 · 2 个=55 · **空=17**；`doParentRelation` 1=193 / 0=121；`particleSystemsShapeAngle` **空=217（69% 只走主数组）**；7 条 `particleSystems` 里有 null 引用（正是 LogError 那支）。
**Unity 侧重写**：**我们没有任何对应物**。要新建 `ScaleParticlesByTarget`（挂在特效实例上）：① 可按 `targetCard.localScale / actingCard.localScale` 缩放粒子系统 —— 但**我们的对局是"我们的尺寸"，不必跟原版比值同构**；② `ParentConstraint` 也要自写（用 `transform.SetParent` 就够，除非要"只继承位置/只继承旋转"）。**建议只做 localScale 那一半**（`doParentRelation` 193 例可以直接用父子挂接替代），ShapeAngle 那半边按语义自研。
**依赖**：`CardScript.GetEffectAnchor()` · `ParentConstraint` · `BattleParticleColliderManager`（与 §6 同一套 collider 位）；**触发点**：它重写的是 `Initialize`，所以被 `SetData` 驱动。

## 5. `AnimFXModuleTween` ‖ 单位补间（DOTween Sequence） ‖ 5 个方法体
字段：`0x38 tweenAnims:UnitTweenSO[]` · `0x40 playOnEnable` · `0x41 triggerOnlyOnce` · `0x42 alreadytrigger`

| 方法 | 方法体在做什么 |
|---|---|
| `Initialize` | base.Initialize → `if (playOnEnable) StartCoroutine(PlayAnimCoroutine())`（协程体在嵌套类 `d__7`，本轮补） |
| `PlayAnim()` | `StartCoroutine(PlayAnimCoroutine())`（**唯一的公开入口**：给动画事件/代码调） |
| `PlayAnimCoroutine()` | 只是 `new d__7 { <>4__this = this }`（状态机对象） |
| **`d__7.MoveNext`** 🆕 | `if (!alreadytrigger) { alreadytrigger = true; foreach (UnitTweenSO so in tweenAnims) { var seq = so.BuildSequence(controller.actingCard, controller.targetCard, null); if (so.waitAnimation) yield return seq.WaitForCompletion(); } }` —— 🔴 **`triggerOnlyOnce` 字段在代码里从未被读过**；真正的门闩是 `alreadytrigger`（首次置 1，**`OnDisable()` 复位为 0**） |
| `OnDisable` | `alreadytrigger = false` |
| `.ctor` | `playOnEnable = true` 且 `triggerOnlyOnce = true`（`*(u16*)(0x40) = 0x101`；**但 `triggerOnlyOnce` 没有任何读取者**） |

**`UnitTweenSO.BuildSequence(source, target, cb)`**（本轮补，块外但决定语义）：`DOTween.Sequence()` → `foreach (TweenInfoBase t in tweens)`：`t.GetTween(source, target)`（虚调 `+0x178`）→ 按 `TweenInfoBase.muted(0x10)` 与 **`appendType(0x14)`（`SequenceAppendType`）决定 `Append` 还是 `Join`** → 最后 `if (useCallBack) seq.InsertCallback(callBackTime, cb)`。
**真实数据**：138 / 138；`tweenAnims` 1 个=134 / 2 个=2；`playOnEnable` 1=54；`triggerOnlyOnce` 1=101；引用 **24 个** UnitTweenSO（`Impact Light Tween` 70）；59 条 tween（`PunchTween` 32 · `ResetBodyTween` 11 · `RotateTween` 6 · `ShakeTween` 4 · `ScaleTween` 3 · `MoveTween` 3），duration 0.10–2.00s（众数 0.5），ease 只有 10 种取值，`useCallBack` 全 0。
**Unity 侧重写**：`Assets/CardPresentation/Core/CardTween.cs` 现在只有 3 个静态助手（`Use`/`ToPose`/`Advance`）⇒ 要新建**两层**：① 数据 `UnitTween`（照 `UnitTweenSO`：`waitAnimation`/`tweens[]`/`appendType`/`duration`）+ 导入脚本读 `数据/游戏数据/tween/*.json`；② 工厂 `BuildSequence(CardView source, CardView target)`，把 6 种 tween（Punch/ResetBody/Rotate/Shake/Scale/Move）各写一个 builder。**`triggerOnlyOnce` 别实现（原版没读）**。
**依赖**：`UnitTweenSO` / `TweenInfoBase` / 6 个 tween 子类 —— **都在本块之外**（成表没把它们算进 18 类）。

## 6. `AnimFXModuleCollisions` ‖ 粒子碰撞平面 ‖ 2 个方法体（+3 个嵌套类方法体 🆕）
字段：`0x38 collisionAndParticles:CollisionAndParticles[]`

| 方法 | 方法体在做什么 |
|---|---|
| `Initialize` | base → `foreach (cap in collisionAndParticles) cap.Initialize(controller.actingCard, controller.targetCard)` |
| **`CollisionAndParticles.Initialize`** 🆕 | ① 先把 `CollisionPlane` 枚举**解析成 `BattleCollider` id**（下表）；② `BattleParticleColliderManager.Instance.GetColliderTransform(id)`（**id → 7 个 Transform 字段的 switch**，本轮补）；③ **若解析结果是 `GenericTarget(15)`**：把该 collider 的 `position` 设成 `targetCard.position`、`up` 设成 `normalize(actingCard.position − targetCard.position)`（"朝来袭方向立起来"）；④ `foreach (particleSystemsDefinition) def.Initialize(colliderTransform)` |
| **`ParticleCollisionDefinition.Initialize(Transform plane)`** 🆕 | `ps.collision.AddPlane(plane)`；**若 `receiveCollisionMessage`**：取/加 `AnimFXParticleCollisionNotifier` 组件 → 把自己 **append 进 `notifier.collisionsModules`** → `ps.collision.sendCollisionMessages = true`。粒子系统为空 → `LogError("[ERROR] particle system not assigned to particle VFX")` |
| **`ParticleCollisionDefinition.OnParticleCollision()`** 🆕 | `if (receiveCollisionMessage && collisionEvent != null) collisionEvent.Invoke()`（**UnityEvent，Inspector 里连**） |
| **`CollisionAndParticles.UpdateTargetCollisionPlanePosition(targetPlane, actingT, targetT)`** 🆕 | 与 ③ 同一条公式的**独立刷新版**（`up = normalize(actingT.pos − targetT.pos)`、`position = targetT.pos`）。⚠️ 反编译集**只覆盖约 71 个类**（`decomp_out/`+`decomp_out2/` 2024 个 `.c`）里**只有它自己的定义、没有调用点** ⇒ 是不是废弃的**没验证** |

**枚举 → 平面解析表**（左=`collisionPlane`，右=实际 `BattleCollider`；`W` = `targetCard.IsWarlordEquivalent()`）：

| 枚举值 | acting 是玩家方 | acting 是敌方 |
|---|---|---|
| `Floor 0` | `Floor 0` | `Floor 0` |
| `Opponent 5` | `W ? EnemyWarlord 11 : Enemy 10` | `W ? PlayerWarlord 7 : Player 5` |
| `OpponentForceInFrontOfWarlord 10` | `EnemyWarlord 11` | `PlayerWarlord 7` |
| `OpponentForceInFrontOfWarlordFromCamera 12` | `EnemyWarlord 11` | `PlayerWarlordFromCamera 8` |
| `MySelf 15` | `W ? PlayerWarlord 7 : Player 5` | `W ? EnemyWarlord 11 : Enemy 10` |
| `MySelfForceInFrontOfWarlord 20` | `PlayerWarlord 7` | `EnemyWarlord 11` |
| `DynamicTarget 25` | 同阵营 → `GenericTarget 15`；对方 → 对方那侧 5/7 或 10/11 | 同 |

**真实数据**：357 / 354 效果；`collisionPlane` 5=251 · 0=180 · 25=13 · 10=3 · 12=3；每个实例的 def 数 1=271 · 2=81 · 3=2。
**Unity 侧重写**：我们没有「粒子碰撞平面」这一层。建议**不要照搬枚举**：我们的战场是 3D 特效 + UI 卡框，**建一个 `EffectCollisionPlane` 静态类，把 7 个 collider 收成 3 个语义位（地面 / 目标单位 / 我方单位）**，粒子撞到时发一个 `UnityEvent`（我们的 `CardEffects.FireEvent` 已经在做"事件名 → 特效"的转发，可以复用）。
**依赖**：🔴 **块2 的 `AnimFXParticleCollisionNotifier` 必须与它一起做**（它持有 `List<ParticleCollisionDefinition>`，注册/回调接口要一起定）；`BattleParticleColliderManager`；`CardScript.IsWarlordEquivalent()`。

## 7. `AnimFXModuleTransformModifier` ‖ 挂父级 / 位置旋转缩放 / 朝目标 ‖ 5 个方法体（+1 🆕）
字段：`0x38 transformModifiers[]` · `0x40 alwaysMaintainWorldScale` · `0x44 objective:VFXObjective{Caster=0,Target=1}` · `0x48 affectEnemyOnly` · `0x49 updateContinuously` · `0x4A aimToTargetInUpdate` · `0x4B updateScaleByBoardPosition` · `0x4C changeParent` · `0x4D unParentAtStart` · `0x4E parentAtExit` · `0x4F setDefaultUnitTransformScaleWhenUnParenting` · `0x50 originalParent` · `0x58 cardTarget` · `0x60/0x6C originalScale/originalLossyScale`

| 方法 | 方法体在做什么 |
|---|---|
| `Initialize` | ① base；② `cardTarget = null`；③ `if (changeParent && controller.targetCard != null) transform.SetParent(targetCard.transform)`；④ `originalParent = transform.parent`；⑤ `if (unParentAtStart)` → `transform.SetParent(null)`，且 **若 `setDefaultUnitTransformScaleWhenUnParenting`** → `transform.localScale = BattleManager.Instance.GetUnitSizeInPlay(actingCard.cardType, actingCard.isPlayer)`（**用"场上单位尺寸"复位**）；⑥ `cardTarget = objective == Caster ? actingCard : targetCard`；⑦ `cardTarget == null` → `LogError("[ERROR] Can't find target card")` + `enabled = false`；⑧ 记 `originalScale` / `originalLossyScale`（⚠️ 2026-09-18 核过：**取的是 `cardTarget` 的** transform、**不是本体的** —— `__Initialize.c:102-114` ⇒ 所以 `alwaysMaintainWorldScale` 要拿得到卡才算得出）；⑨ `if (affectEnemyOnly && cardTarget.isPlayer) enabled = false; else ApplyTransforms()` |
| `ApplyTransforms()` | `foreach (m in transformModifiers)`：`modifyPosition` → **`useEffectAnchorParent` 为真就 `LogError("[ERROR] Use effect anchor parent for position is not implemented")`**，否则 `m.target.localPosition = m.position`；`modifyRotation` → 同上分支，否则 `m.target.localEulerAngles = m.rotation`，**带 anchor 时** `m.target.rotation = transform.parent.rotation * Quaternion.LookRotation(m.rotation)`；`modifyScale` → 带 anchor 时**先** `LogError("...scale is not implemented")`，**然后照样 `m.target.localScale = m.scale`**（⚠️ 2026-09-18 更正：原来这里写的是「否则」，实际两份方法体都是「报错在 if 里、赋值在 if 外」= **带 anchor 也设值** —— `__ApplyTransforms.c:108-119` · `ApplyModification.c:72-84`） |
| `Update()` | `if (alwaysMaintainWorldScale)` → `transform.localScale = originalLossyScale / (parent ? parent.lossyScale : 全局静态 Vector3（在 `DAT_1842da2a8` 的静态块 `+0xc`，**类名未定**）)`；`if (updateScaleByBoardPosition)` → `localScale = cardTarget.CardUI.<0x1c0>.GetScale(cardTarget.isPlayer, cardTarget.transform.localPosition, originalScale)`；`if (aimToTargetInUpdate)` → `transform.forward = normalize(controller.targetCard.GetEffectAnchor().position − transform.position)`；`if (updateContinuously) ApplyTransforms()` |
| `Exit()` | `if (unParentAtStart && parentAtExit) transform.SetParent(originalParent)` |
| **`TransformModifier.ApplyModification(Transform)`** 🆕 | 与 `ApplyTransforms` **同一份逻辑**（单条版；`ApplyTransforms` 是把它内联展开了）⇒ 实现时只写一份 |
| `.ctor` | `affectEnemyOnly = true`（**默认开**） |

**真实数据**：**只有 18 / 18**；`changeParent` 1 例 · `unParentAtStart` 9 · `parentAtExit` 2 · `alwaysMaintainWorldScale` 4 · `updateScaleByBoardPosition` 2 · `aimToTargetInUpdate` 1 · `updateContinuously` 1 · `objective` Caster 16 / Target 2 · `affectEnemyOnly` 1=10（样本默认值生效）；`transformModifiers` 长度 1=9 / **0=9**；**`modifyPosition` 全 0**。
**Unity 侧重写**：我们**没有**「特效挂卡/脱父级」这层（现在是 parent 到卡或格位，一次性）。建议新建 `EffectAttachToCard`：**只做 `changeParent` / `unParentAtStart`(+尺寸复位) / `parentAtExit` / `alwaysMaintainWorldScale`** —— 这已覆盖 18 例里的绝大多数；`updateScaleByBoardPosition` / `aimToTargetInUpdate` 各只有 1 例，**先不做也说得过去**（要照原版就得连 `MinionScaleByPositionSO` 的曲线语义一起搬）。
**依赖**：`BattleManager.GetUnitSizeInPlay` · `MinionScaleByPositionSO`（本轮补了 `GetScale`：`scaleCurve.Evaluate(z)` × 全局默认尺度 × `initialScale/曲线末点`）· `CardScript.CardUI.<0x1c0>` · `GetEffectAnchor()`。

## 8. `AnimFXModuleChangeMaterial` ‖ 换材质 / 换贴图 / 淡入淡出 ‖ 6 个方法体（+1 🆕）
字段：`0x38 material` · `0x40 isCardMaterial` · `0x41 fade` · `0x42 initializeWithCardImage` · `0x43 toggleMaterialOffOnExit` · `0x44 forceRecoverMaterialOnDestroy` · `0x45 changeTexture` · `0x48 textureMaterialProperty` · `0x50 materialAnimations:DynamicList<MaterialTweenBase>` · `0x58 useCustomRenderers` · `0x60 customRenderers[]` · `0x68 materialInstance`

| 方法 | 方法体在做什么 |
|---|---|
| `Initialize` | base → **`if (actionStart == Initialize(0)) ToggleMaterial(true)`** ⇒ 这类**是自己读 `actionStart` 的** |
| `Exit` | **`if (actionStart == Exit(5)) ToggleMaterial(true)`**（注意：Exit 时是**开**）；否则 `if (toggleMaterialOffOnExit) ToggleMaterial(false)` |
| `OnDestroy` | `if (forceRecoverMaterialOnDestroy && actingCard != null) actingCard.CardUI.RestoreOriginalMaterial()` |
| `ToggleMaterial(bool on)` | **开**：`isCardMaterial` → `materialInstance = actingCard.CardUI.SetCardMaterial(material, initializeWithCardImage)`；否则 `useCustomRenderers` → `materialInstance = new Material(material)`（material 为 null 时取 `customRenderers[0].GetSharedMaterial()`）再 `foreach (r in customRenderers) r.SetMaterial(materialInstance)`。然后 `if (changeTexture) materialInstance.SetTexture(textureMaterialProperty, actingCard.rawCard.cardSprite.texture)`。最后 `if (fade)`：`StopAllCoroutines + DOTween.Kill(materialInstance)` → `foreach (anim in materialAnimations) anim.<slot 0x178>(materialInstance, on)` → 取**最长 duration** → `StartCoroutine(WaitForFinishMaterialChange(maxDuration))`。**关**：先从 `materialAnimations` 反向播（同上，bool 相反）并等最长时长，再 `RestoreOriginalMaterial()`（`isCardMaterial==false && useCustomRenderers==true` 时不做恢复） |
| **`WaitForFinishMaterialChange` MoveNext** 🆕 | `yield return new WaitForSeconds(timeToWait)` → `actingCard.CardUI.RestoreOriginalMaterial()` |
| `.ctor` | `isCardMaterial = true`（写 1 字节）· `initializeWithCardImage = true` 且 `toggleMaterialOffOnExit = true`（`*(u16*)(0x42) = 0x101` ⇒ **`0x42` 与 `0x43` 两个字节都是 1**）；**`fade(0x41)` 没被赋值 ⇒ 默认 false** |

**真实数据**：**只有 3 例**（`AmbushEffect`·`StealthEffect`·`VanguardIdleEffect`）；`fade` 3/3 开；`toggleMaterialOffOnExit` 2；`forceRecoverMaterialOnDestroy` 1；`changeTexture` 1（`textureMaterialProperty = "_TargetImage"`）；`material` 分别是 `Card 3d Dissolve Blend Image Ambush` / `Card 3d Stealth` / `Vanguard_Frame VAT Dissolve`。
**Unity 侧重写**：对应我们的 `CardFeel.Dissolve(card, delay, onDone)`（已有）+ `CardView` 的贴图/材质切换。建议新建 `CardMaterialFX`（挂 `CardView`）：负责「换 material / 换贴图 / 淡入淡出 / **恢复原材质**」，把 `RestoreOriginalMaterial` 收口成一处。**成本低、只有 3 例**，可以最后做。
**依赖**：`BattleCardUI.SetCardMaterial` / `RestoreOriginalMaterial` · `MaterialTweenBase`（块外的动画资产类）· `RawCardScript.cardSprite`。

## 9. `AnimFXModulePostProcess` ‖ 全屏 LUT + Bloom（带优先级仲裁） ‖ 14 个方法体 + 5 个 lambda
字段：`0x38 controlledByAnimation` · `0x39 animateBloomIntensity` · `0x3C bloomIntensity` · `0x40 animateBloomThreshold` · `0x44 bloomThreshold` · `0x48 doLUTAnim` · `0x4C lutBlend` · `0x50 maxLUTBlend` · `0x54 timeToOn` · `0x58 timeOn` · `0x5C timeToOff` · `0x60 customLUT1` · `0x68 customLUT2` · `0x70 betweenLUTsBlend` · `0x74 effectPriority` · `0x78 blendMaterialID` · `0x7C alreadySetLUTAnimTexture` · `0x80 lutToApplyRT` · `0x88 instanceMaterial` · `0x90/0x98 tweenStart/tweenEnd` · `0xA0 allowAnimation`；静态 `currentAnimPriority` / `currentEffectInPlay`

| 方法 | 方法体在做什么 |
|---|---|
| `.cctor` / `.ctor` | `currentAnimPriority = -1`；`controlledByAnimation = true` · `maxLUTBlend = 1.0f` · `effectPriority = 1` · `blendMaterialID = Shader.PropertyToID("_Blend")` |
| `OnEnable` | **优先级仲裁**：`allowAnimation = true`；`if (effectPriority < currentAnimPriority) { allowAnimation = false; return; }`；**踢掉正在播的那个**：`currentEffectInPlay.ResetPostFX(true)` + `Kill` 它的两条 tween；`currentAnimPriority = effectPriority; currentEffectInPlay = this`；`instanceMaterial ??= new Material(Shader.Find("Hidden/LUTBlender"))`；`if (doLUTAnim && lutToApplyRT == null && customLUT1 != null) lutToApplyRT = new RenderTexture(256, 16) { useMipMap = false, name = "Particle FX controller LUT" }`（🔴 **LUT 合并 RT 是 256×16**）；**`if (!controlledByAnimation)`**：`Kill` 旧 tween；`timeToOn == 0` 时 `lutBlend = maxLUTBlend`；`tweenStart = DOTween.To(()=>lutBlend, v=>lutBlend=v, maxLUTBlend, timeToOn)`；`tweenEnd = DOTween.To(..., 0f, timeToOff).SetDelay(timeOn + timeToOn).OnComplete(ResetCurrentPlayingEffect)` |
| `Update` | `if (!allowAnimation) return;` → `DoBloomAim()` → `DoLUTAnim()` → `Universal.CameraExtensions.UpdateVolumeStack(BattleManager.<0xa8>)` |
| `DoBloomAim` | `if (PostFXController.Instance.Bloom)`：`animateBloomIntensity` → 写 `Bloom.<0x48>`（intensity），`animateBloomThreshold` → 写 `Bloom.<0x40>`（threshold）；两个参数各走虚调 `+0x228`（该虚调带一个 float 实参） |
| `DoLUTAnim` | `if (!doLUTAnim) return;` 首次：`instanceMaterial.SetTexture("_LUT1", customLUT1)` + `("_LUT2", customLUT2)` + `LUTBlender.SetTargetLUT(lutToApplyRT ?? customLUT1)` 并置 `alreadySetLUTAnimTexture`；然后 `if (lutToApplyRT) { instanceMaterial.SetFloat(blendMaterialID, betweenLUTsBlend); Graphics.Blit(lutToApplyRT, instanceMaterial) }`；最后 `LUTBlender.DoBlend(lutBlend)` |
| `BlendLutTextures` | 与上面 "[lutToApplyRT 存在] 那两行" 相同（可单独调） |
| `CancelAnimation` | `ResetPostFX(true)` + `Kill(tweenStart)`/`(tweenEnd)` |
| `ResetPostFX(bool instant)` | `PostFXController.Instance.ResetBloom(instant)` + `ResetLUT(instant)` |
| `ResetCurrentPlayingEffect` | **只有当 `currentEffectInPlay == this`** 才 `ResetPostFX(false)`，并把静态 `currentAnimPriority = -1`、`currentEffectInPlay = null` |
| `OnDisable` / `OnDestroy` | `alreadySetLUTAnimTexture = false` + `ResetCurrentPlayingEffect()`；销毁时 `Destroy(lutToApplyRT)` + `Destroy(instanceMaterial)` |
| `OnValidate` | 运行时才把两张 LUT 重新 SetTexture 到 instanceMaterial |
| `AnimEventSetOriginalLUTNoTransition` | `LUTBlender.instanceMaterial.SetTexture("_LUT1", LUTBlender.<0x50>)`（"立刻回原 LUT，无过渡"，供动画事件调） |
| `b__24_0..3` / `b__24_4` | 前四个 = `lutBlend` 的 getter/setter（DOTween 的取/写值 lambda）；`b__24_4` = 上面那条 `OnComplete` → `ResetCurrentPlayingEffect()` |

**真实数据**：52 / 52；`doLUTAnim` 1=**50**（**本质是 LUT 模块，Bloom 是附赠**）；`controlledByAnimation` 0=29 / 1=23；`effectPriority` 只有 `{1:45, 0:7}`；`customLUT1` 14 个具名 LUT（`Red Tint` 9 · `Dimensional Breach` 9 · `Red Hell` 6…）；`customLUT2` **46/52 为 null**；`betweenLUTsBlend` 50/52 为 0。
**Unity 侧重写**：我们有 `GrabPassTransparentFeature`（URP renderer feature）但**没有 LUT/后期链**。要新建 `PostFxDirector`（全局单例）：① **优先级仲裁（照抄，语义清楚、`effectPriority` 只有 0/1 两档）** ② LUT 混合：URP 下用 `Volume`+`ColorLookup` 或自建全屏 blit（原版的 `Hidden/LUTBlender` + `_LUT1/_LUT2/_Blend` + 256×16 合并 RT 是现成规格）③ 三条时间曲线 `timeToOn/timeOn/timeToOff`。**这是本块里最"要自建"的一块**。
**依赖**：`PostFXController`（`Bloom` / `LUTBlender` / `Volume`）· `LUTBlender`（`SetTargetLUT`/`DoBlend`/`TransitionTo`）· DOTween · `BattleManager` 的相机；与块2 无耦合。

---

## 附A：本轮补的嵌套类方法体（`d:/2/tools/decomp_animfx_nested{,2}/`，18 个）

成表说「反编译里一个 AnimFX 类都没有 ⇒ 只能按语义猜」——**方法体有了，但有一半逻辑在生成类/嵌套类里**，那些**没被上一轮的重命名清单覆盖**（清单按 `<类名>$$` 前缀匹配）。本轮用同样的 Ghidra 工程补了 18 个（含块2 用得上的 3 个）：
`AnimFXModuleTween.<PlayAnimCoroutine>d__7.MoveNext` · `...ChangeMaterial.<WaitForFinishMaterialChange>d__16.MoveNext` · `CollisionAndParticles.Initialize` / `UpdateTargetCollisionPlanePosition` · `ParticleCollisionDefinition.Initialize` / `OnParticleCollision` · `TransformModifier.ApplyModification` · `PlaySoundOnTime.Update` · `CameraShakePreset.Play` · `OverwriteCameraShakePreset.Play` / `GetModifiedPreset` · `UnitTweenSO.BuildSequence` · `ParentConstraint.ToggleParentingOptions` / `Sync` · `BattleParticleColliderManager.GetColliderTransform` · `MinionScaleByPositionSO.GetScale` · 两个数学内联 `tanf`(RVA 0x48A0D0) / `atan2f`(RVA 0x486DA0)。
**复现命令**（`资料/反编译工具链_重建记录.md` §三·五 那套，只换清单文件）：
```bash
export JAVA_HOME="D:\\2\\tools\\jdk-21.0.12.1+1"
cd /d/2/tools && cmd //c "D:\\2\\tools\\ghidra_12.1.3_PUBLIC\\support\\analyzeHeadless.bat D:\\2\\tools\\ghidra_proj Warpforge \
  -process GameAssembly.dll -noanalysis -scriptPath D:\\2\\Warpforge_tools\\data\\decomp_il2cpp_0827\\ghidra_scripts \
  -postScript Decomp.java D:\\2\\tools\\wanted_nested.txt D:\\2\\tools\\decomp_animfx_nested"
```
清单文件两行格式 = `十进制 RVA ⇥ 名字`（名字只当输出文件名用，**不用跟 Ghidra 符号一致**）；RVA 从 `dump.cs` 的 `// RVA: 0x…` 列取（**不是** `Offset:` 列，也不是 VA）。
⚠️ 上一轮没覆盖这些嵌套类的**原因**：`gen_ghidra_lists.py` 是按 `<类名>$$` 前缀从 script.json 抓方法名的，`<PlayAnimCoroutine>d__7` / `CollisionAndParticles` 这种带 `<` 或嵌套路径的名字**没被抓到**。
**同形状的坑（块2 会撞上）**：`AnimFXInstanceParticleAdjacent.<DelayActivation>d__4.MoveNext`、`AnimFXModuleEvent.FireEvent.TryFireEvent` 这类嵌套/生成类的方法体**同样不在 `decomp_out_ai` 里**，按上面同一条命令补即可（本次没补，**别以为它们不存在**）。

## 附B：`PlaySoundOnTime.Update(currentTime, transform)`（controller 的定时音效语义，本轮补）
```
int max = repeat ? loops : 1;                    // 0x21 repeat / 0x24 loops
if (numberOfTimesPlayed >= max) return;
if (currentTime < time + numberOfTimesPlayed * timeInterval) return;   // 0x10 time / 0x28 timeInterval
if (!is2d) SoundManager.Instance.Play3D(sound, transform.position);    // 0x20 is2d / 0x18 sound
else       SoundManager.Instance.Play2D(sound);
numberOfTimesPlayed++;                           // 0x2C
```
⇒ **「第 N 次到点播一次，可重复 N 次、间隔 `timeInterval`」**；`controller.Update` 传的 `currentTime` 是**特效自己的存活时长**（不是全局时间）⇒ 我们这边 `WarpforgeEffectPlayer.Tick` 里的 `_time` 正好可以喂给它。

## 附C：读不懂 / 未确证（别当结论用）
1. 🔴 `ScaleByTarget.ChangeShapeAngle` —— `tanf`→`atan2f` 的**操作数配对**没能逐位还原（见 §4），实现请按语义自定。
2. `TransformModifier.Update` 里 `parent == null` 时的**全局静态 Vector3**（`DAT_1842da2a8` 静态块 `+0xc`，即该类的第 2 个静态字段）**类名未定**；`MinionScaleByPositionSO.GetScale` 用的是同一个 ⇒ 是"场上默认单位尺寸"，但**没定位到是哪个类**。
3. `Graphics.Blit(lutToApplyRT, instanceMaterial)` 的**重载**没确证（静态调用的寄存器约定在这份反编译里不统一）——不影响行为。
4. `AnimFXController.DoDestroy` 里「有模块就不销毁」的写法**语义可疑但证据一致**：块2 §3 独立读到 `AnimFXModuleDoDestroyAnimation.DoDestroy()` = `Animation.Play(endAnimationName)` + **`Destroy(gameObject, timeToDestroy)`**（模块自己销毁本体）⇒ 两条互为佐证，**可以当结论用**（只是"设计怪"，不是"读错"）。
5. `AnimFXModuleTween.triggerOnlyOnce` **在 5 个方法体里都没被读过** —— 不排除它被别处（编辑器/被裁掉的代码）引用，但**别把它当门闩实现**。
6. 本块 9 类的**字段/参数分布**不重复成表已有的（§4 值分布、建议顺序看 `资料/AnimFX_18类成表.md`），本文只补**方法体那层**与**新算的分布**。

## 附D：块1 的驱动链（汇总时对齐用）

```
CardScript.PlayTriggerAnim(prefab, targetCard)        ← 动画挂点/牌局在"该播特效"时调
   └─ Instantiate(prefab, CardUI.<0x1a0>)
      ├─ GetComponent<AnimFXController>().SetData(this, targetCard)
      │     └─ foreach (modules) module.Initialize(this)     ← 模块唯一的初始化入口
      ├─ GetComponent<其它组件>().<slot 0x198>(param_4)      ← 同 prefab 上还有第二个组件被调（未解出是谁）
      └─ Controller.OnEnable(destroyTime>0 && !preventDestroy) → Object.Destroy(go, destroyTime)

动画事件（AnimationClip/AnimEvent）→  AnimEventDoShake(i) / TriggerCameraShake(i) / Tween.PlayAnim() / AnimEventSetOriginalLUTNoTransition()
BattleCardUI.CleanStatusAnims / RemoveStatusAnim  →  Controller.Exit()   → OnFinished → modules.Exit → Destroy(go, exitDestroyTime)
BattleManager.RemoveDestroyableAnims              →  Controller.DoDestroy() → 有模块则**不销毁**（由模块负责，如 DoDestroyAnimation）
```

🔴 **这些方法在已反编译的类里一个调用点都没有**（`decomp_out{,2}/` ~71 个类 + `decomp_out_ai/` 全部 grep 为空）⇒ 它们是**动画片段事件 / Inspector UnityEvent 的调用目标**（"AnimEvent*" 的命名就是线索；`AnimFXModuleTween.PlayAnim()` 是 public 且无调用点，也属这一类）：
`AnimFXModuleScreenShake.AnimEventDoShake(i)` · `TriggerCameraShake(i)` · `AnimFXModuleTween.PlayAnim()` · `AnimFXModulePostProcess.AnimEventSetOriginalLUTNoTransition()` · `DoBloomAim()` · `BlendLutTextures()` · `CancelAnimation()` · `AnimFXModuleEvent` 的 `UnityEvent`。
**对我们的意义**：这几条**没有"代码驱动"的替代路径** —— 要么我们做一层"动画事件"（时间轴上按时刻回调），要么就把它们挂到我们自己的时序/信号上（`BattleDriver.PlaySignal`）。W 组「触发源是动画挂点」这条在方法体层面被再次证实。

## 附E：跨类 / 跨块依赖表（本块九类）

| 类 | 依赖谁 | 谁依赖它 | 跨块？ |
|---|---|---|---|
| `AnimFXModuleBase` | `BattleManager.IsPlayerTurn` · `CardScript.isPlayer` | 全部 15 个模块 | 块2 的 `Event` 用 `ActionStart` |
| `AnimFXController` | `PlaySoundOnTime` · `SoundManager` · 模块列表 | 被 `CardScript`/`BattleCardUI`/`BattleManager` 调 | — |
| `ScreenShake` | `OverwriteCameraShakePreset` → `CameraShakerManager` → `CameraShakePresetSO` | — | — |
| `ScaleByTarget` | `ParentConstraint` · `CardScript.GetEffectAnchor` · `BattleParticleColliderManager` | — | 与 `Collisions` 共用 collider 管理器 |
| `Tween` | `UnitTweenSO` · `TweenInfoBase` · 6 个 tween 子类（**都在块外**） | — | 块外的数据类 |
| `Collisions` | `BattleParticleColliderManager` · `CardScript.IsWarlordEquivalent` | — | 🔴 **块2 的 `AnimFXParticleCollisionNotifier`**（注册接口要一起定） |
| `TransformModifier` | `BattleManager.GetUnitSizeInPlay` · `MinionScaleByPositionSO` · `CardScript.CardUI` | — | — |
| `ChangeMaterial` | `BattleCardUI.SetCardMaterial/RestoreOriginalMaterial` · `MaterialTweenBase`（块外） | — | 块外的 `MaterialTweenBase` 资产类 |
| `PostProcess` | `PostFXController` · `LUTBlender` · DOTween · `BattleManager` 相机 | — | — |

**块1 内部建议实施顺序**（按"已有基建 / 覆盖面 / 自建量"排）：
`Controller 的模块分发`（我们已有 `WarpforgeEffectPlayer`，补一层即可）→ `ScreenShake`（416 效果，最简）→ `Tween`（138 效果，要先有 `UnitTween` 数据层）→ `ScaleByTarget`（314 效果，只做缩放那一半）→ `Collisions`+`AnimFXParticleCollisionNotifier`（一起做）→ `TransformModifier`（只做挂父级）→ `PostProcess`（要自建后期链，最重）→ `ChangeMaterial`（3 例，最后做）。

## 附F：与 `AnimFX_18类成表.md` / 块2 的对齐（**只有一条是新发现**）
1. 成表开头「别去反编译里找方法体」那条**今天已被更正**（改成「18 类 86 个方法体在 `decomp_out_ai/`」）⇒ 本文不重复。
   `AnimFXModuleDestroyInTime` 也已补进成表 §四。**这两件都不是本文的待办。**
2. 🔴 **`actionStart` 的读取者名单还差一个**：成表 2026-09-17 那条更正写「真正读它的只有两处：
   `AnimFXModuleDestroyInTime` 与 `AnimFXModuleEvent.whereToFire`」——**少了 `AnimFXModuleChangeMaterial`**。
   实据（本块 §8 逐方法）：`AnimFXModuleChangeMaterial__Initialize.c` 里 `if (*(int *)(param_1 + 0x20) == 0) ToggleMaterial(true)`、
   `__Exit.c` 里 `if (*(int *)(param_1 + 0x20) == 5) ToggleMaterial(true)`（`param_1+0x20` = 基类 `actionStart`，dump.cs 实证）。
   ⇒ 准确说法是「**三处**读 `actionStart`：`DestroyInTime` / `Event.whereToFire` / `ChangeMaterial`(Init 与 Exit 各一次)」，
   **`10(DoDestroy)` 这一个值仍然没人用**（成表后半句是对的）。
