# AnimFX 18 个类 · 逐类成表（2026-09-13 第三十三轮，派子代理产出）

> **为什么要这份**：`AnimFX` 是表现层**最大的一件没做的活**（`项目任务.md` §九·2「还剩」）。
> 以前只有「18 个模块 / 2346 个组件」这个规模数，**没有逐类的字段与参数分布** —— 没法排期。
> 这一轮按类把**字段 / 参数值分布 / 覆盖多少效果 / 依赖**全落了表。
>
> **⚠️ 别去反编译里找方法体**：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out{,2}/`
> 里**一个 AnimFX 类都没有**（Ghidra 反编译了 34 类 / 2024 方法，不含它们），
> `Warpforge_code` 的桩文件方法体全空 ⇒ **路 B 只能按语义重写**。这条路已经确认，别再试。
>
> 数据源：桩文件 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFX*.cs`（18 文件 / 907 行）·
> `d:/4/Unity/数据/游戏数据/animfx_components.json`（**2346 个组件**）·
> `d:/4/Unity/数据/游戏数据/tween/`（74 个 JSON）。

## 一、两个核心（**必须成对先做**）

| 类 | 字段（SF = `[SerializeField]`） | 参数值分布 | 覆盖 |
|---|---|---|---|
| `AnimFXModuleBase` | SF `actionStart:ActionStart`（`AllowNesting`）；非 SF `animFXController`、`isRetaliation`；枚举 `ActionStart{Initialize=0, Exit=5, DoDestroy=10, Manual=15}` | abstract，0 实例；派生实例里 `actionStart=0` 占 **913 中的 910**（只有 `DestroyInTime=5`、`ChangeMaterial` 1 例 `=15`） | 被全部 15 个模块类继承 → **间接覆盖 912 效果 / 990 prefab** |
| `AnimFXController` | SF `sounds`/`exitSounds:PlaySoundOnTime[]` · `preventDestroy` · `destroyTime` · `modules:List<AnimFXModuleBase>` · `exitDestroyTime`；非 SF `OnFinished:Action` · `SAFE_DESTROY_TIME=3f` · `actingCard/targetCard/currentTime/exiting` | **990 实例 / 912 效果**；`preventDestroy` 0=893/1=97；`destroyTime` 0–17s（众数 4s=248 · 3s=133 · 1.8s=116 · 5s=89 · 0 值 38）；`exitDestroyTime` **3.0s=956/990**；`modules` 数 0=422/1=178/2=108/3=189/4=91；`sounds` 非空 854（1 个=786），`exitSounds` 非空仅 **7** | 全部 |

**它俩就是生命周期本体**：base 持有 `animFXController` 字段、以 `Initialize(AnimFXController)` 为契约入口；
controller 以 `List<AnimFXModuleBase>` 持有全部模块并驱动 Initialize / Exit / DoDestroy；
`ActionStart` 枚举就是「**何时调用这三个回调**」的调度表。
**先把这对的语义钉死，7 个叶子模块才有落脚点。**

## 二、覆盖面最大的两个叶子（**建议第一批做，而且最简单**）

| 类 | 字段 | 参数值分布 | 覆盖 |
|---|---|---|---|
| `AnimFXModuleScreenShake` | SF `cameraShakes` / `manualTriggerCameraShakes:OverwriteCameraShakePreset[]`；API `AnimEventDoShake(int)` / `TriggerCameraShake(int)` | **434 实例 / 416 效果**；`cameraShakes` 数 0=156/1=246/2=18/…；`manualTrigger` 0=268/1=166；**508 条 preset 条目只指向 14 个不同 presetSO**；`delay` 325 条=0，其余散在 0.2–2.2s | **416 效果** |
| `AnimFXModuleScaleByTarget` | SF `particleSystems` · `doParentRelation` · `particleSystemsShapeAngle`；重写 `Initialize` / `ChangeShapeAngle` | **314 实例**；`particleSystems` 数 1=226/2=55/0=17/…；`doParentRelation` 1=193/0=121；`shapeAngle` 0=217（**69% 只用主数组**） | **314 效果** |

**`ScreenShake` 的外依**：`OverwriteCameraShakePreset.cs:6` + `CameraShakePresetSO` +
**21 个 `tween/Shake*.json` 与 `Warpforge 6D Shake*.json`**（`m_Script` 3633289390197731284，
字段 `preset{delay, sustainTime, attackTime, decayTime, amplitude, frequency, direction}`；
例 `Shake Hit Medium.json`：sustain 0.3 / decay 0.3 / **amp 2.0** / freq 0.05）。21 个里实际被引用 **14** 个。
⚠️ 每个 preset 的 6 组 `overwrite*` 开关在 dump 里**被截断**，只有 1 条可见 `overwriteSustainTime`。

## 三、带数据管线的那个

| 类 | 字段 | 参数值分布 | 覆盖 |
|---|---|---|---|
| `AnimFXModuleTween` | SF `tweenAnims:UnitTweenSO[]` · `playOnEnable` · `triggerOnlyOnce`；非 SF `alreadytrigger`；API `PlayAnim` / `PlayAnimCoroutine` | **138 实例**；`tweenAnims` 长度 1=134/2=2/0=2；`playOnEnable` 0=84/1=54；`triggerOnlyOnce` 1=101/0=37；引用 **24 个不同 UnitTweenSO**（`Impact Light Tween` 70 · `Recoil Normal Tween` 9 · `Impact Target Medium` 7 …） | **138 效果** |

**tween 数据**（`tween/*.json`，24/74 个被引用）：**59 条** = `PunchTween` 32 · `ResetBodyTween` 11 ·
`RotateTween` 6 · `ShakeTween` 4 · `ScaleTween` 3 · `MoveTween` 3；
duration **0.10–2.00s**（中位 0.40 · 众数 0.5s=17 · 0.3s=12）；ease **只有 10 种取值**（6:31 · 5:10 · 23:4 · 1:3 …）；
`waitAnimation=1` 的 6/24，`useCallBack` 全 0。
⇒ **需要先有统一的「单位补间」抽象**，所以排在 base 之后、其余之前。

## 四、其余叶子

| 类 | 字段 | 参数值分布 | 覆盖 |
|---|---|---|---|
| `AnimFXModuleCollisions` | SF `collisionAndParticles:CollisionAndParticles[]`；枚举 `CollisionPlane{Floor=0, Opponent=5, OpponentForceInFrontOfWarlord=10, …FromCamera=12, MySelf=15, …=20, DynamicTarget=25}`；嵌套 `ParticleCollisionDefinition{particleSystem, receiveCollisionMessage, collisionEvent:UnityEvent}` | **357 实例 / 354 效果**；`collisionPlane` 5=251/0=180/25=13/10=3/12=3；每个模块 def 数 1=270/2=81/…（共 449 个 def） | 354 效果 |
| `AnimFXModuleTransformModifier` | SF `transformModifiers:TransformModifier[]` · `alwaysMaintainWorldScale` · `objective:VFXObjective` · `affectEnemyOnly` · `updateContinuously` · `aimToTargetInUpdate` · `updateScaleByBoardPosition` · `changeParent` · `unParentAtStart` · `parentAtExit` · `setDefaultUnitTransformScaleWhenUnParenting` | **18 实例**；`transformModifiers` 长度 1=9/**0=9**（一半只用来挂/脱父级）；`modifyPosition` **全 0**；`affectEnemyOnly` 1=10/0=8；`unParentAtStart` 9/9 … | 18 效果 |
| `AnimFXModuleChangeMaterial` | SF `material` · `isCardMaterial` · `fade` · `initializeWithCardImage` · `toggleMaterialOffOnExit` · `forceRecoverMaterialOnDestroy` · `changeTexture` · `textureMaterialProperty` · `materialAnimations:DynamicList<MaterialTweenBase>` · `useCustomRenderers` · `customRenderers:Renderer[]` | **仅 3 实例**：`Card 3d Dissolve Blend Image Ambush` / `Card 3d Stealth` / `Vanguard_Frame VAT Dissolve`；`isCardMaterial` 1=2/0=1；`toggleMaterialOffOnExit` 1=2；`actionStart` 0=2、15=1 | 3 效果（`AmbushEffect` / `StealthEffect` / `VanguardIdleEffect`）|
| `AnimFXModulePostProcess` | SF 18 个：`controlledByAnimation` · `animateBloomIntensity` · `bloomIntensity` · `animateBloomThreshold` · `bloomThreshold` · `doLUTAnim` · `lutBlend` · `maxLUTBlend` · `timeToOn` · `timeOn` · `timeToOff` · `customLUT1/2` · `betweenLUTsBlend` · `effectPriority`；静态 `currentAnimPriority/currentEffectInPlay` | **52 实例**；`doLUTAnim` 50/52=1（**本质是 LUT 模块，Bloom 是附赠**）；`effectPriority` 只有 {1:45, 0:7}；`customLUT1` 14 个具名 LUT（`Red Tint` 9 · `Dimensional Breach` 9 · `Red Hell` 6 · `Pink Emperors Children` 5 …），`customLUT2` 46/52 为 null；`timeToOn` 0–4s / `timeOn` 0–5s / `timeToOff` 0–1.5s | 52 效果 |
| `AnimFXModuleCardback` | SF `particleSystemsToAddCardback:ParticleSystem[]` | **30 实例**（数组长度 1=27/0=2/5=1）| 18 效果 |
| `AnimFXModuleAnimation` | SF `simpleAnimation:SimpleAnimation`；const `EXIT_STATE="Exit"`（**状态名硬编码**）| **5 实例**，全部 `ShieldTraitEffect_Idle` 系列 | 5 效果 |
| `AnimFXModuleDoDestroyAnimation` | SF `endAnimationName:string` · `myAnimation:Animation` · `destroyOnAnimationEnd` · `timeToDestroy`；只重写 `DoDestroy()` | **1 实例**（`Pray_Idle`）：`"Pray Exit"` / 1 / 1.0s | 1 |
| `AnimFXModuleEvent` | 内嵌 `FireEvent{UnityEvent eventToFire, ActionStart whereToFire}`；SF `eventsToFire:FireEvent[]` | **1 实例**（`VanguardIdleEffect`）：`whereToFire=Exit(5)`，**无第二个取值** | 1 |
| `AnimFxModuleMoveParticlesToTarget` | SF `delay` · `particleSystems:ParticleSystem` · `attractSpeed` · `attractSpeedByLifetime:AnimationCurve` · `guaranteeFinalPosition` · `desiredZPosition` | **1 实例**（`Remnant Aeldari Collect particles`）：delay 0.65 / attractSpeed 2.0 / desiredZ −8.0 | 1 |
| `AnimFXInstanceParticleAdjacent` | SF `cardAnim:AssetReferenceTyped<CardAnim>` · `delay` · `playOnRetaliation` | **1 实例**（`BulletImpact_deathspinner_arc_alt`）：delay 1.6s | 1 |
| `AnimFXModuleChangeVelocity` | SF `particleSystems:ParticleSystemVelocity[]`；嵌套 struct 含弹道数学 `CalculateMaxHeight` / `CalculateSecondProjectile` | **0 实例、0 效果** —— 18 个类里唯一在数据中**完全没挂载**的 | 0 |
| `AnimFXParticleCollisionNotifier` | 无 SF；`List<AnimFXModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition> collisionsModules`；`Register(...)` / `OnParticleCollision(GameObject)` | 数据里 **0 次**、全目录无引用 ⇒ **由 `AnimFXModuleCollisions` 运行时挂载** | 运行时 |

⚠️ 后 8 个模块的 `actionStart` **100% 为 0（Initialize）** —— Exit / DoDestroy / Manual 三个枚举值
**在出货数据里从未被用过**。

## 五、依赖与建议顺序

- **9 个叶子之间零互相引用** —— 全是被 prefab 挂载的叶子 MonoBehaviour，依赖几乎都在组外。
- **唯一的跨类边**：`AnimFXParticleCollisionNotifier.cs:8` 的参数类型就是
  `AnimFXModuleCollisions.ParticleCollisionDefinition` ⇒ **必须排在 `Collisions` 之后**。
  其余叶子都继承 `AnimFXModuleBase`，且 `AnimFXModuleEvent.whereToFire` 直接用基类的 `ActionStart`。

**推荐顺序**：① `AnimFXModuleBase` + `AnimFXController`（**成对**） →
② `AnimFXModuleScreenShake`（416 效果，最简单，无外依）· `AnimFXModuleScaleByTarget`（314 效果，同上）→
③ 无依赖叶子（`DoDestroyAnimation` / `Event` / `PostProcess` / `TransformModifier` / `MoveParticlesToTarget`）→
④ `AnimFXModuleTween`（需先有 UnitTweenSO + 补间抽象）→
⑤ `AnimFXModuleCollisions` + `AnimFXParticleCollisionNotifier`（要一起定注册/回调接口）→
⑥ `AnimFXModuleChangeVelocity`（**0 引用，可最后做**，但它的弹道数学是唯一含实体物理算法的，语义不可省）。

**可只做一半的**：`TransformModifier` 的 `modifyPosition` 全 0、9/18 实例数组为空 ⇒ 只实现**父级挂载分支**即可。
`DoDestroyAnimation` / `Event` / `MoveParticlesToTarget` 各只有 1 个实例，可最后做甚至先硬编码。
