# AnimFX 18 类 · 方法体逐类（块 2 = 后 9 类）

> **这块覆盖谁**：按 `资料/AnimFX_18类成表.md` 的类序取**后 9 个** —— `Cardback · Animation ·
> DoDestroyAnimation · Event · MoveParticlesToTarget · InstanceParticleAdjacent · ChangeVelocity ·
> ParticleCollisionNotifier · DestroyInTime`。
> ⚠️ 最后一个（`DestroyInTime`）在成表里**没有独立成行**（只在 §一 的 `actionStart` 分布里露过一次面），
> 它确实是第 18 个类（有桩文件、有 1 个实例、有 3 个方法体），本块补上。
> 前 9 个（Base · Controller · ScreenShake · ScaleByTarget · Tween · Collisions · TransformModifier ·
> ChangeMaterial · PostProcess）见同目录 `AnimFX_18类方法体_块1.md`（另一个会话写，本文件不碰它）。
>
> **方法体出处**：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`（18 类共 86 个 `.c`，
> 本块占 **29 个**）。下文 `[AI]/Xxx__Yyy.c` 都指这个目录；`[old]/` 指同级的 `decomp_out/`。
> **本块新补的反编译**：`AnimFXModuleChangeVelocity.ParticleSystemVelocity` 的 4 个内部方法
> （`CalculateMaxHeight` / `CalculateSecondProjectile` / `ModifyVelocity`×2）**0827 那批里没有**，
> 2026-09-17 由本会话补出 → `d:/2/tools/decomp_cv_0917/`（Ghidra 复用工程 `-process -noanalysis
> -readOnly`，`ok=4 fail=0`；完整命令见本文末）。没有这 4 个，`ChangeVelocity` 就只能写「查不到」。
> **字段偏移一律用 `d:/2/tools/il2cpp_out/dump.cs` 核过**（带 `// 0x??` 的那份），不是按字段顺序猜的。
> **数据面**：`d:/4/Unity/数据/游戏数据/animfx_components.json`（912 效果 / 2346 组件）。

## 0. 读法警告：Ghidra 输出里这几个符号名是**假的**（先把它们从眼里滤掉）

| 反编译里写成 | 实际是什么 |
|---|---|
| `NetworkingPeer__OnMessage(&x, m)` | **枚举器的 `Dispose()`**（`foreach` 的收尾） |
| `System_Threading_Tasks_TaskAwaiters__ForceAsync(x, 0)` | **结构体 getter 返回值**（`ps.main` / `ps.shape` 这类 `MinMaxCurve` 取值） |
| `UnityEngine_Rendering_HableCurve__set_x1` / `Keyframe__get_outWeight` / `set_outWeight` | 同一个 `MinMaxCurve` 结构体的 **`constantMax` 取值/写回**（编译器生成的小 thunk，名字是编的） |
| `UnityEngine_UIElements_..._PointerLocation__set_Position(&p)` | **`Particle.position` 赋值** |
| `Bounds__get_center(buf, &p)` | **读 `Particle.position`** |
| `PhotonHandler___ctor` / `<类名>___.ctor` | 空构造（只调 `UnityEngine.Behaviour..ctor`；有初始值的会显式写字段） |
| `FUN_1803f47a0()` / `FUN_1803f4790()` | **IL2CPP 的 null 检查抛异常**（= 那里会 `NullReferenceException`）与数组越界检查 |

数学助手 `FUN_1804886e0` / `FUN_180486da0` / `FUN_180488130` 在 Ghidra 里**没名字**，
本块按「参数个数 + 前后接的 `Deg2Rad`/`Rad2Deg` 转换 + 与抛体公式吻合」判为 **sin / atan2 / tan**；
`FUN_180488058`/`FUN_180488dd8` = **sqrt**。⚠️ **这四个判据不是逐字核实过的**，用它们的结论时带着这条。

---

## 1. `AnimFXModuleCardback` ‖ 把**玩家档案里的卡背美术**塞进粒子系统的 sprite 表 ‖ 3 个方法体

方法体：`.ctor`（空）· `Initialize` · `SetParticleMaterial`

- **`Initialize(AnimFXController)`** `[AI]/AnimFXModuleCardback__Initialize.c`
  1. `base.Initialize(controller)`（基类把 controller 存到 +0x28，并算 `isRetaliation`，见块1）；
  2. `sprite = BattleManager.Instance.GetCardback(controller.actingCard.isPlayer).Item1`
     —— 实参就是 `actingCard` 的 **`EntityScript.isPlayer`**（`+0x40`，dump.cs 核过）；
  3. `sprite == null` → `CustomDebug.LogError("[ERROR] Can't find cardback for FX " + this.name)`
     （名字取的是组件名 = 所在 GameObject 名）并 **return（粒子一个都不改）**；
  4. 否则调 `SetParticleMaterial(sprite)`（被内联进 `Initialize`，所以两处代码长得一样）。
- **`SetParticleMaterial(Sprite cardBack)`** `[AI]/AnimFXModuleCardback__SetParticleMaterial.c`
  - `particleSystemsToAddCardback(+0x38)` **为 null 或长度 0** → `LogError("[ERROR] Can't find particle system
    to assign card frame")`，**不抛异常、直接返回**；
  - 否则逐个粒子系统：`tsa = ps.textureSheetAnimation; tsa.enabled = true; tsa.mode = Sprites;
    tsa.AddSprite(cardBack)`（Ghidra 丢了 `AddSprite` 的实参，实参 = 第 3 步那个 sprite）。
- **`BattleManager.GetCardback(bool isPlayer)`**（不在本类里，`[old]/BattleManager__GetCardback.c`）：
  取该侧的 army/存档槽 → `PlayerDataManager.GetCosmeticItem(...)` → 转成 `CosmeticItemCardback` →
  `GetCardBackSprites(true)` 返回 `ValueTuple<Sprite,Sprite>`，模块**只取 Item1**。
  ⚠️ **两个 Sprite 的区别（正/背？普通/金？）查不到** —— `CosmeticItemCardback.GetCardBackSprites`
  没被反编译（RVA `7266656`，用文末那条命令一行就能补）。
- **数据里**：**30 实例 / 18 效果**。**27** 个数组长 1，`CreateCard GSC 5x` 是 **5**，
  `EnvironmentalCondition Ultramarines Bombardment` / `Orbital 4 repeat` 是 **0**（= 命中上面那条 LogError 分支）。
  效果全在 `CreateCard*` 家族（+ `DeckBuff_MoveToTop` / `Sau_ReconstitutionProtocol` / `Wild Rider Chieftain`）。
- **Unity 侧重写**：我们侧**没有对应物**。等价实现 = 在特效「Initialize 时机」补一句
  `ParticleSystem.TextureSheetAnimationModule.AddSprite(sprite)`。
  🔴 **导出侧现在缺这一块**：`WarpforgeArena1/Editor/EffectExporter.cs:341-345` 只给
  `SpriteRenderer` / `SpriteMask` 导了 sprite，`BuildArena1.cs:255-268` 只把 **翻页图集**写进 tsa
  ⇒ **textureSheetAnimation 的 sprite 列表是空的**。这 30 个实例的粒子在导出侧要么用材质贴图、
  要么空 —— **动手前先拿 `CreateCard*` prefab 对着原版比一次**（我们工程里已经有
  `WarpforgeVFX/Materials/Cardback_Particle.mat`、`Generic Particle Dissolve For Sprites {Orks,Tau} Cardback.mat`
  —— 说明材质本身是过来了的）。
- **依赖**：只依赖 base（+0x38 自己的数组）。**卡背美术的出处是玩家档案**（不是 prefab 硬编码）⇒
  我们复刻时得自己定「哪张卡背」（我们是单机，用原版卡背图即可）。

## 2. `AnimFXModuleAnimation` ‖ Exit 时让 `SimpleAnimation` 播**硬编码状态名 `"Exit"`**，播完销毁 ‖ 3 个方法体

方法体：`.ctor`（空）· `Exit` · `AnimationExitEnd`

- **`Exit()`** `[AI]/AnimFXModuleAnimation__Exit.c`：`simpleAnimation(+0x38).Play("Exit")`。
  状态名是 `const EXIT_STATE = "Exit"`（**已从字符串表核实**：`0x1842ae790 → "Exit"`）。
  `simpleAnimation` 为 null 会 `NullReferenceException`（没有保护）。
- **`AnimationExitEnd()`** `[AI]/AnimFXModuleAnimation__AnimationExitEnd.c`：`Object.Destroy(gameObject)` ——
  **立即、无延时、销毁的是整个 GameObject**（不是本组件）。⚠️ **全树里查不到它的调用点**
  ⇒ 它是给 **Exit 那个 clip 的动画事件**用的（**推断**，不是读到的）。
- **谁调 `Exit`**：`AnimFXController.Exit()` 对 `modules` **无条件**逐个广播（vtable 0x188），
  **不看 `actionStart`**；而 `AnimFXController.Exit()` 的调用点是
  `[old]/BattleCardUI__CleanStatusAnims.c:114,200` 与 `[old]/BattleCardUI__RemoveStatusAnim.c:68,107`
  —— 即**卡上的状态动画（status anim）被清理/移除时**。之后控制器自己
  `if (!preventDestroy) Destroy(gameObject, destroyTime(+0x34))` 并置 `exiting = true`。
- **数据里**：**5 实例 / 5 效果**，**全部**是 `ShieldTraitEffect_Idle`（+ Emperor's Children / Necron /
  Sororitas / TAU 四个变体），`simpleAnimation` 是个 `MonoBehaviour` 引用，`actionStart` 全 0。
- **Unity 侧重写**：我们侧**没有 SimpleAnimation**（原版用的是 AssetStore 那个插件）。
  等价物 = `Animator.Play("Exit")` 或 legacy `Animation.Play("Exit")`。
  要新建的东西：**特效播放器上的一条「Exit 回调」**（现在我们只有「播一个特效」，没有生命周期回调）。
  ⚠️ 先确认导出 prefab 上还留着哪套动画组件（SimpleAnimation 的 MonoBehaviour 是 bundle 资产，
  大概率**没导过来**）—— 这条不确认就实现不了。
- **依赖**：base（`Exit` 无条件广播）。与块1 的 `AnimFXController.Exit` **强耦合**（广播语义见 §0 与 §10）。

## 3. `AnimFXModuleDoDestroyAnimation` ‖ 播一段「退场动画」+ **定时**销毁（不等动画播完） ‖ 2 个方法体

方法体：`.ctor` · `DoDestroy`（**没有** `Initialize`/`Exit` 重写）

- **`.ctor`** `[AI]/AnimFXModuleDoDestroyAnimation__.ctor.c`：字段默认值 =
  `destroyOnAnimationEnd(+0x48) = true`、`timeToDestroy(+0x4C) = 1.0f`（`0x3f800000` 读出来的）。
- **`DoDestroy()`** `[AI]/AnimFXModuleDoDestroyAnimation__DoDestroy.c`：
  1. `if (myAnimation(+0x40) != null) myAnimation.Play(endAnimationName(+0x38))`；
     `myAnimation` 为 null 时**跳过播放**（有 null 检查，不抛）；
  2. `if (destroyOnAnimationEnd) Object.Destroy(gameObject, timeToDestroy)`。
  🔴 **名字骗人**：`destroyOnAnimationEnd` 只表示「要不要销毁」，
  **方法体里没有任何「等动画播完」的机制**（不查 clip 时长、不挂事件），销毁时刻 = `now + timeToDestroy`。
- **谁调 `DoDestroy`**：`AnimFXController.DoDestroy()` 广播给所有 module（vtable 0x198）；
  调用点 `[old]/BattleManager__RemoveDestroyableAnims.c`（**只对状态 anim 的 state∈{15,16} 那两类**，
  而且是「字典里查到就 DoDestroy 并从字典移除」）。控制器随后：若**没有任何** module 存在
  (`bVar1 == false`) → `Destroy(gameObject)`。
- **数据里**：**1 实例**（`Pray_Idle`）：`endAnimationName="Pray Exit"` / `destroyOnAnimationEnd=1` /
  `timeToDestroy=1.0`。
- **Unity 侧重写**：我们侧最接近的是「特效播完自己消失」这类生命周期，但没有「退出动画 + 定时销毁」。
  新建：特效播放器上的 **Exit 钩子**（与 §2 共用同一条）。
  ⚠️ 原版用的是 **legacy `Animation` 组件**（`UnityEngine_Animation__Play(Animation, string)`）——
  先确认我们导出 prefab 上还留着 `Animation` 组件与 clip（bundle 资产，同 §2 的疑问）。
- **依赖**：base；与块1 的 `AnimFXController.DoDestroy` 耦合。

## 4. `AnimFXModuleEvent` ‖ 在指定时机**触发任意 UnityEvent**（唯一实例指向 Exit） ‖ 3 个方法体

方法体：`.ctor`（空）· `Initialize` · `Exit`（`FireEvent.TryFireEvent` 被内联进这两个里）

- **语义**（两个方法体同构）：遍历 `eventsToFire(+0x38)` 数组，对每个 `FireEvent`
  当 `whereToFire(+0x18) == 时机` 且 `eventToFire(+0x10) != null` 时 `UnityEvent.Invoke()`。
  `Initialize` 匹配 **0**（`ActionStart.Initialize`），`Exit` 匹配 **5**（`ActionStart.Exit`）。
  数组元素为 null 或事件为 null → 走 IL2CPP null 检查抛异常（**没有静默跳过**）。
- **数据里**：**1 实例**（`VanguardIdleEffect`）：`whereToFire = 5（Exit）`（**出货数据里 `whereToFire` 只有这一个取值**）。
  🔴 **UnityEvent 的 `m_Calls` 在 dump 里被截断了**（`m_PersistentCalls=<UnknownObject<...> m_Calls=[<Unknow...`）
  ⇒ **它实际调的是什么函数，查不到**。要查得回 prefab 原始 JSON 逐条读 `m_Calls`（本次没做）。
- **Unity 侧重写**：我们侧没有 UnityEvent 层。这就是「到某个时机执行一段策划连的调用」。
  建议实现成 `Action` 列表（`Init`/`Exit`/`DoDestroy`/`Manual` 四档），挂在特效播放器的生命周期上。
  🔴 **在知道它调什么之前不要瞎猜** —— 最靠谱的是**进原版把 `VanguardIdleEffect` 跑出来看**
  （这是「实况 > 字段」那条铁律的场景）。
- **依赖**：base 的 `ActionStart` 枚举（**它读的是基类那个字段**，不是自己算的）；
  与 §9 `DestroyInTime` **在同一个效果里**（`VanguardIdleEffect` 同时挂了 Event + DestroyInTime + ChangeMaterial）。

## 5. `AnimFxModuleMoveParticlesToTarget` ‖ 延时后把粒子**吸到该侧灵石/法力 UI 锚点** ‖ 6 个方法体

方法体：`.ctor` · `Initialize` · `LateUpdate` · `GetTarget` · `SetTarget` · `UpdateTargetPosition`
（这几个被两两内联：`Initialize` 里含 `SetTarget`+`UpdateTargetPosition` 的代码）

- **`.ctor`**：`attractSpeed(+0x48) = 2.0f`、`desiredZPosition(+0x5C) = -8.0f`、
  `particles(+0x78)` = 某静态空数组。
- **`GetTarget(CardScript card)`** `[AI]/...__GetTarget.c`：
  `card.isPlayer ? BattleManager.playerManager(+0xC0) : BattleManager.enemyManager(+0xC8)`
  → **`PlayerManager.GetSpiritStoneManaTransform()`**。
  ⇒ **目标是「那一侧的灵石/法力 UI 锚点」，不是卡、不是格位**（效果名 `Remnant Aeldari Collect particles`
  正好是灵族收集灵石）。
- **`SetTarget(Transform target, Vector3 originalPosition)`** `[AI]/...__SetTarget.c`：
  `enabled = true`；存 `target(+0x60)` / `originalPosition(+0x68)`；
  然后 `targetPosition(+0x84) = CamerasConversionHelper.ConvertPositionBetweenCameras(
  hudCamera, boardCamera, desiredZPosition - boardCamera.transform.position.z, target.position)`
  —— **把 HUD 相机的世界坐标换算到棋盘相机空间**，深度用 `desiredZ(-8)` 反推。
  （`CamerasConversionHelper.ConvertPositionBetweenCameras` 签名见 dump.cs:115604：
  `(Camera cameraOrigin, Camera cameraDestination, float desiredZDistance, Vector3 objectCurrentWorldPosition)`）
- **`UpdateTargetPosition()`** `[AI]/...__UpdateTargetPosition.c`：同一段换算（供目标移动时刷新）。
- **`Initialize(AnimFXController)`** `[AI]/...__Initialize.c`：
  1. `particles` 为 null 或 **长度 < `ps.main.maxParticles`** → `particles = new Particle[maxParticles]`；
  2. 若 `controller.actingCard != null` → `target = GetTarget(actingCard)`，
     再内联 `SetTarget(target, ps.transform.position)`（起点 = 粒子系统自己的位置）。
- **`LateUpdate()`** `[AI]/...__LateUpdate.c`：
  `curretTime(+0x80) += Time.deltaTime;` → **`curretTime < delay(+0x38)` 就直接 return**（延时未到不碰粒子）→
  `n = ps.GetParticles(particles)` → 逐粒子：读 `remainingLifetime` / `startLifetime`；
  若 `guaranteeFinalPosition(+0x58) == false` → `attractSpeedByLifetime(+0x50).Evaluate(1 - remaining/start)`
  （**`1 -` 里那个 1.0f 是从 DLL 里读出来的常量 `0x1834b2bb8`**，所以曲线横轴 = 粒子寿命进度 0→1）；
  最后写回粒子位置并 `ps.SetParticles(particles, n)`。
  🔴 **浮点插值算式在反编译里丢了**（只剩 `Evaluate` / `Time.deltaTime` / 读位置 / 写位置这四个调用，
  中间怎么合成新坐标看不出来）。按字段名推是「朝 `targetPosition` 以 `attractSpeed × 曲线值 × dt` 靠近」，
  **这是推断、不是读到的** —— 要用就得先实况验一次。
- **数据里**：**1 实例**（`Remnant Aeldari Collect particles`）：`delay 0.65` / `attractSpeed 2.0` /
  `attractSpeedByLifetime` 首键 `(t=0, v=1.0)` / `guaranteeFinalPosition 0` / `desiredZ -8.0`；
  同效果还有一个 `AnimFXController(destroyTime 2.5)`。（⚠️ dump 里的 curve 是截断的，**只看到第一个键**。）
- **Unity 侧重写**：我们侧**没有粒子级操纵**。要做就是个 `LateUpdate` 里 `GetParticles`→改→`SetParticles`
  的小组件（**数组要复用、不能每帧 new**；粒子多时这是 O(n) 的 CPU 活）。
  `CardTween`/`CardFeel` 里**没有对应概念**（那是 Transform 层面的补间）；要新建成特效模块。
  其中「HUD↔棋盘换算」我们**现在没有**（全工程搜不到 `ConvertPosition`/`hudCamera`）——
  我们的棋盘是另一套 2D 坐标（`LayoutSpace` / `BoardLayout`），灵石图标是 HUD 上的
  （`BattleDriver.ResourceIcon01`）。⇒ 复刻时**等价物 = 把世界坐标吸到 HUD 灵石图标的屏幕位置**，
  不需要原版那套相机换算。
- **依赖**：外依 `BattleManager` / `PlayerManager` / `CamerasConversionHelper`；
  内依 controller 的 `actingCard(+0x50)`（块1）。

## 6. `AnimFXInstanceParticleAdjacent` ‖ 延时后**在每个「目标卡的相邻单位」身上各实例化一份 CardAnim** ‖ 4 个方法体

方法体：`.ctor`（空）· `Initialize` · `DelayActivation` · `ExecuteEffect`

- **`Initialize(AnimFXController)`** `[AI]/AnimFXInstanceParticleAdjacent__Initialize.c`：
  1. `base.Initialize(controller)`（**`isRetaliation` 是基类在这里算的**：`IsPlayerTurn()` 与
     `actingCard.isPlayer` 异或 = 「这张卡属于**非**当前回合方」）；
  2. 对 `cardAnim(+0x38)` 做一次**虚调用**（vtable 0x248，判资产引用是否有效；**具体方法名未核实**）→
     无效则 `Debug.LogError("[ERROR] Roope, delete the Module for adjacent VFX if you are not using it")`
     **并整条 return**（开发留言，字面就长这样，字符串表核过）；
  3. `if (playOnRetaliation(+0x44) || !isRetaliation(+0x30))`：
     `delay(+0x40) > 0` → `StartCoroutine(DelayActivation())`；否则**直接** `ExecuteEffect()`。
     ⇒ **跳过条件只有一条**：`playOnRetaliation == false` **且** 本次是反击。
- **`DelayActivation()`** `[AI]/...__DelayActivation.c`：返回编译器生成的迭代器
  （`_003CDelayActivation_003Ed__4`，新建对象、`state=0`、持有 this）。
  🔴 **状态机的 `MoveNext` 不在反编译集里**（它在 `d:/2/tools/all_methods.txt` 里有名字，
  RVA `6796352`）⇒ **「等的是 `WaitForSeconds(delay)` 还是别的」没读到**，只能按 `delay` 字段推。
- **`ExecuteEffect()`** `[AI]/...__ExecuteEffect.c`：
  `units = BattleManager.GetAdjacentUnits(controller.targetCard(+0x58))`（**取的是「目标卡」的相邻单位**）→
  `asset = cardAnim.Load(cacheGroup: 1)`（`AssetReferenceTyped<CardAnim>.Load`）→
  `foreach (unit in units)` → `BattleManager.CreateAnimFromAnimFxController(
  asset.animInfo /*CardAnim.animInfo 在 +0x18，dump.cs 核过*/,
  controller.actingCard.transform, unit.transform, unit.transform,
  controller.actingCard, unit, null, false)`。
  ⇒ **起点 = 出手卡，终点/朝向 = 那个相邻单位**，逐单位各来一份。
- **数据里**：**1 实例**（`BulletImpact_deathspinner_arc_alt`）：`delay 1.6` / `playOnRetaliation 0` /
  `cardAnim` 是资产 GUID `df138b834086ffe43a025ca6ec25d46f`。该效果同时还挂了
  ScreenShake / Tween / TransformModifier / **ScaleByTarget** / **Collisions** / Controller（块1 的四个同框）。
- **Unity 侧重写**：我们侧 `VfxMap` 只支持「一个事件 → 一个特效」。这条要
  **「一个事件 → 对每个相邻单位各播一份（起点=出手卡）」** —— 新建一个 `SpawnOnAdjacent` 之类的小组件。
  相邻判定我们已经有了：棋盘就是 `Board[slot]` 裸数组，**相邻 = 同排 `slot±1`**（`BoardLayout` 的
  相邻中心距 149.3px 是布局常数，不是判定逻辑）。
  ⚠️ 我们**没有 Addressables**（`cardAnim` 是资产引用）⇒ 等价物 = 直接引用导出 prefab / 按名字查表。
- **依赖**：**是 9 个类里唯一用 `CardAnim` 资产引用的**；`isRetaliation` 来自 base（块1）；
  相邻单位来自 `BattleManager`（外）。

## 7. `AnimFXModuleChangeVelocity` ‖ 把粒子初速/角度改成**能打到目标距离的抛物线** ‖ 6 个方法体

方法体（**本块补了 4 个**）：`.ctor`（空）· `Initialize` `[AI]/` ·
`ModifyVelocity(Transform,float,float)` · `CalculateMaxHeight` · `CalculateSecondProjectile` ·
`ModifyVelocity(float,Transform)` → 后四个在 **`d:/2/tools/decomp_cv_0917/`**

- **`Initialize(AnimFXController)`** `[AI]/AnimFXModuleChangeVelocity__Initialize.c`：
  `heroA = BattleManager.GetHero(true)`、`heroB = BattleManager.GetHero(false)`（双方督军）；
  任一为 null → `LogError("[ERROR] Can't find player or enemy hero")` 并 return；
  `d1 = |heroA.pos - heroB.pos|`；`d2 = |actingCard(+0x50).pos - targetCard(+0x58).pos|`；
  然后 foreach `particleSystems(+0x38)`（元素 16 字节结构：`ParticleSystem` + `bool applyToVelocityModule`）
  → `ModifyVelocity(this.transform, d1, d2)`。
- **`ModifyVelocity(Transform, float d1, float d2)`**（新补）：
  `applyToVelocityModule == true` → `Debug.LogError("[ERROR] Not implemented yet. Ask Cesar for implementation")`
  （**原版自己没实现这条分支**，字符串表核过）；否则：
  `H = CalculateMaxHeight(main.startSpeed.constantMax, shape.rotation.x)` →
  `CalculateSecondProjectile(H, main.startLifetime.constantMax, d2, out v, out ang)` →
  `shape.rotation.x = sign(旧 rotation.x) * ang` → `main.startSpeed.constantMax = v`。
  ⚠️ **`d1`(督军间距) 在这个方法体里读了没用** —— 传进来就算了，不参与计算。
- **`CalculateMaxHeight(v0, angle)`**（新补）：抛体**最高点**公式 ——
  `H = (lossyScale.x · v0)² · sin²(angle·Deg2Rad) / (2·|Physics.gravity.y × main.gravityModifier.constantMax|)`。
- **`CalculateSecondProjectile(H, t, d2, out v, out ang)`**（新补）：`g' = |gravity.y × gravityModifier.constantMax|`；
  `w = sqrt(2·g'·H)`；`ang = atan2(w·t, d2) · Rad2Deg`；`v = (d2 / (tan(ang·Deg2Rad) · t)) / lossyScale.x`。
- **`ModifyVelocity(float multiplier, Transform transform)`**（新补）：同样是「`applyToVelocityModule` 为真就 LogError」，
  否则 `main.startSpeed.constantMax *= multiplier / transform.lossyScale.x`。
- **常量（全部从 `GameAssembly.dll` 读出来的）**：`0.0174533`(Deg2Rad) · `57.29578`(Rad2Deg) ·
  `0x7FFFFFFF`(取绝对值掩码) · `±1.0`(sign)。**没有一条是猜的。**
- **数据里**：**0 实例 / 0 效果** —— 18 个类里**唯一在出货数据中完全没挂载**的。
- **Unity 侧重写**：**0 引用 ⇒ 可以最后做甚至不做**。要做的话它**不碰 Transform、只改
  `ParticleSystem.main.startSpeed`（`MinMaxCurve.constantMax`）与 `shape.rotation`** ——
  和 `CardTween`/`CardFeel` 的补间概念**无关**，是一个「发射参数标定器」。
  它的真正用途：我们将来做「抛射物从 A 格飞到 B 格」时，这就是原版的现成公式
  （`d2` 用格位节距算，`CardFeel.OriginalBoardPxPerUnit = 182.14` 那套尺寸桥量距离）。
- **依赖**：外依 `BattleManager.GetHero`；内依 controller 的 `actingCard`/`targetCard`。
  ⚠️ `d1`（督军间距）传导链是断的 ⇒ **汇总时不用为它设计输入**。

## 8. `AnimFXParticleCollisionNotifier` ‖ 粒子撞到东西 → **广播全部已注册的碰撞事件** ‖ 3 个方法体

方法体：`.ctor` · `Register` · `OnParticleCollision`

- **`.ctor`** `[AI]/AnimFXParticleCollisionNotifier__.ctor.c`：`collisionsModules(+0x20) = new List<ParticleCollisionDefinition>()`。
- **`Register(ParticleCollisionDefinition)`** `[AI]/...__Register.c`：**就是 `list.Add(def)`**
  （走的是 `List.Add` 的扩容路径，没有任何去重/过滤）。
- **`OnParticleCollision(GameObject other)`** `[AI]/...__OnParticleCollision.c`：
  `foreach (def in collisionsModules) if (def.receiveCollisionMessage(+0x18)) def.collisionEvent(+0x20).Invoke()`。
  🔴 **`other` 参数完全没被用**：不筛粒子系统、不筛撞到的是谁 ——
  **任何粒子撞任何东西，都会把列表里所有开了开关的事件全点一遍**。
- **数据里**：**0 实例**（该类无 SF 字段）⇒ 由 `AnimFXModuleCollisions` 在运行时 `Register` 挂上去。
- **Unity 侧重写**：这就是 Unity 的 `OnParticleCollision` 消息接收器（碰撞事件接不接属于 Collisions 的范围）。
  要做的形状：MonoBehaviour + `List<事件>` + `OnParticleCollision` 广播；**不需要 `other`**。
  注意批处理/无帧循环下这个回调根本不会来（要手动 `Simulate`）。
- **依赖**：🔴 **9 个类里唯一的跨类硬依赖** —— 参数类型就是
  `AnimFXModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition`
  （字段 `particleSystem +0x10` / `receiveCollisionMessage +0x18` / `collisionEvent +0x20`）
  ⇒ **必须和块1 的 `Collisions` 一起定注册接口**。

## 9. `AnimFXModuleDestroyInTime` ‖ 到点**自毁整个 GameObject**；按 `actionStart` 二选一从哪一刻起算 ‖ 3 个方法体

方法体：`.ctor`（空）· `Initialize` · `Exit`

- **`Initialize(controller)`** `[AI]/AnimFXModuleDestroyInTime__Initialize.c`：
  `base.Initialize(...)` 之后 `if (actionStart(+0x20) == 0 /*Initialize*/) Object.Destroy(gameObject, destroyTime(+0x38))`。
- **`Exit()`** `[AI]/AnimFXModuleDestroyInTime__Exit.c`：
  `if (actionStart == 5 /*Exit*/) Object.Destroy(gameObject, destroyTime)`。
- ⇒ **同一个类按 `actionStart` 当模式开关**：`0` = 初始化后 `destroyTime` 秒自毁；`5` = Exit 起算 `destroyTime` 秒自毁。
  **两个分支都只有 `Object.Destroy(go, t)` 一句话。**
- 🔴 **这条推翻了成表 §一 的一个假设**：成表写「`ActionStart` 枚举就是**何时调用这三个回调**的调度表」。
  实测：`AnimFXController` 对 `modules` **无条件**逐个调 `Initialize` / `Exit` / `DoDestroy`，
  **一处都没读 `actionStart`**；18 个类里**只有本类**把它当分支用（`ChangeMaterial` 也用，但那是块1）。
  ⇒ **汇总时统一口径：`actionStart` 不是全局调度表，是各模块自己的模式字段。**
- **数据里**：**1 实例**（`VanguardIdleEffect`）：`actionStart = 5`、`destroyTime = 0.75`。
  ⚠️ 成表 §四 的 12 行里**没有这个类**（只在 §一 的 `actionStart` 分布里出现过一次「只有 `DestroyInTime=5`」）
  —— 这就是「18 个类却只列了 17 个」的那一个，本块补上。
- **Unity 侧重写**：我们侧等价物 = 特效的销毁时机；**没有对应概念**，要新建成特效生命周期的一环。
  🔴 **批处理下 `Object.Destroy` 不生效**（要等帧末，批处理没有帧循环）⇒ 自己的实现要用
  `DestroyImmediate` 或 `CardFeel`/`BattleDriver` 那套「按 dt 推进的计时器」
  （工程里已有的写法见 `CardFeel.cs:625-632`、`BattleDriver.Kill`）。
- **依赖**：base 的 `actionStart`（**只有它一个人读**）；与 §4 `Event` 同效果（一起在 Exit 时动作）。

---

## 10. 本块结论（给汇总用）

- **可读性**：9 类 / **29 个方法体**，**28 个读得懂**。唯一读不全的是
  **§5 `MoveParticlesToTarget.LateUpdate` 的浮点插值算式**（调用序列在、算式丢）——
  已在正文标出，**没有拿推断冒充读到**。
- **本块新补反编译 4 个方法体**（`ChangeVelocity` 抛体数学），落点 `d:/2/tools/decomp_cv_0917/`；
  命令（复用已分析好的工程，**几分钟**，不影响 Unity）：
  `analyzeHeadless.bat D:\2\tools\ghidra_proj Warpforge -process GameAssembly.dll -noanalysis -readOnly
   -max-cpu 4 -scriptPath <ghidra_scripts> -postScript Decomp.java <清单「十进制RVA<TAB>名字」> <输出目录>`
- **跨块（块1）依赖 3 条**：
  1. `AnimFXParticleCollisionNotifier` ⇄ `AnimFXModuleCollisions.ParticleCollisionDefinition`（同定接口）；
  2. **`AnimFXController.Exit/DoDestroy` 是无条件广播**（不看 `actionStart`）——与成表「ActionStart 是调度表」
     的说法冲突，建议成表就地更正；
  3. 本块 4 个类要吃 controller 的 **`actingCard(+0x50)` / `targetCard(+0x58)`** 与 base 的
     **`isRetaliation(+0x30)`**（后者的算法已解：`IsPlayerTurn()` 与 `card.isPlayer` 异或）。
- **`d:/2/Warpforge_code/.../AnimFX*.cs` 桩文件的方法体全空** —— 行为只能看 `decomp_out_ai/`。
- **还没查到的三处**（都写了「查不到」，别当「没有」）：
  ① `AnimFXModuleEvent` 的 UnityEvent **实际调什么**（dump 截断，要看 prefab 原始 `m_Calls`）；
  ② `CosmeticItemCardback.GetCardBackSprites` 返回的**两个 Sprite 差在哪**（RVA `7266656`，未反编译）；
  ③ `AnimFXInstanceParticleAdjacent.<DelayActivation>d__4.MoveNext`（RVA `6796352`，未反编译）
     ⇒ 延时的**具体等待方式**没读到。
