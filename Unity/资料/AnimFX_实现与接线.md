# AnimFX 模块层 —— 实现与接线（2026-09-18 建立）

> **这份讲什么**：原版那 18 个 `AnimFXModule*` 是怎么搬进我们工程的 ——
> **数据从哪来、运行时怎么装、想加一个新模块该动哪几个文件**。
> 语义（每个模块干什么）在 `资料/AnimFX_18类方法体_块1.md` / `_块2.md`，**这里不重复**。

---

## 一、现在到哪一步（一句话）

**18 类读完方法体 → 数据链打通（895 个效果 / 2313 个模块）→ 运行时框架就位 → 15 个模块类全部实现 → 下游钩子接了 4 个、还剩 **7** 个（其中 🟡 1 个）。**
判据看 `-executeMethod AnimFXCheck.Run` 末尾的 `=== 合计：N 通过 / M 失败 ===`。

> 🔴 **2026-09-18 追加：全量反编译复核做完了 —— 有 1 处「必须改」的真 bug，见文末 §十一。**
> （`WFModuleScreenShake.TriggerCameraShake` 读错了数组，**434 个实例受影响**。）
> 同节还记着：3 处建议改 · 一批「复核后确认不用动」· 以及 **`sounds` 937 条现在能接了**。

---

## 二、数据链（从 bundle 到运行时）

```
bundle 里的 AnimFX 组件（缺失脚本，值还在）
  └─ 工具/dump_animfx.py          → 数据/游戏数据/animfx_components.json
       └─ 工具/gen_animfx_modules.py → 数据/游戏数据/animfx_modules.json   ← JsonUtility 吃得下的扁平形状
            └─ EffectLibraryBuilder.Run → WFEffectEntry.modules[]           ← SO 里
                 └─ WarpforgeEffectPlayer.Init → WFModuleFactory.Create     ← 运行时 AddComponent
```

**⚠️ 三个必须知道的形状约束**

| 约束 | 为什么 |
|---|---|
| `animfx_modules.json` 里每个模块是 **`{kind, keys[], values[]}`**（键是**点号路径**，如 `collisionAndParticles[0].collisionPlane`） | `UnityEngine.JsonUtility` **解析不了字典、也解析不了多态**。拍平之后每个模块类只要问自己要的字段名，**不用在 python 和 C# 两边各写一份 schema** |
| 引用值编码成字符串：`@node:组件类型:路径` / `@asset:类型:名字` | 同上。`@node` 是同 prefab 内的对象（**按路径解析**），`@asset` 是外部资产（按类型+名字找） |
| 路径里 `#N` = **它在父节点全部子物体里的下标**（`Transform.GetSiblingIndex`），为 0 时省略 | 这是 `dump_animfx.py:_path_of` 与 `WFEffectModule.ResolvePath` 之间的约定。**两边必须一致**，改动要同时改 |

`__node` 这个键 = **模块自己挂在哪个节点上**（原版模块挂在具体 GameObject 上，多数是根但不保证）。
播放器按它把组件挂回原位置 —— 不这么做的话，`TransformModifier` 这类「看自己所在 Transform」的模块会取错，**而且错得很安静**。

---

## 三、运行时框架（三个文件）

| 文件 | 是什么 |
|---|---|
| `WarpforgeVFX/Runtime/WFEffectModule.cs` | **基类** + `WFModuleDef`（数据）+ 路径解析 |
| `WarpforgeVFX/Runtime/WFModuleFactory.cs` | **kind → 组件类型**的映射；用**特性 + 反射自动登记** |
| `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs` | **唯一的控制器**：广播 `Initialize` / `ModuleTick` / `Exit` / `DoDestroy` |

**四条设计决定**（都有具体理由，别推翻）：

1. 🔴 **模块是 MonoBehaviour，不是纯 C# 对象** —— ① 自制特效可以**手挂**；② 要收 Unity 物理消息的
   模块（`OnParticleCollision`）**只有组件收得到**。
2. 🔴 **播放器就是控制器，不要再写第二个** —— 原版 `AnimFXController` 对**所有模块无条件广播**
   Initialize / Exit / DoDestroy，**它一处都不读 `actionStart`**；要不要响应由**各模块自己读那个字段**
   （有实据的读取者：`DestroyInTime` / `Event.whereToFire` / `ChangeMaterial`）。
   **别把 `actionStart` 做成调度开关**，也别再加一套销毁计时（两套会互相打架）。
3. 🔴 **工厂靠 `[WFModuleKind("原版类名")]` 特性 + 反射自动登记** —— 写成一个 `switch` 的话，
   **每加一个模块都要改公共文件**，多路并行必撞车。现在**加模块 = 只新建一个 .cs 文件**。
4. ⚠️ **基类的 `Exit()` / `DoDestroy()` 是空实现**（照原版）—— 18 类里只有 **5 个重写 `Exit`、
   1 个重写 `DoDestroy`**，所以「每个模块都会在 Exit 做事」**不成立**，要按模块逐个看。

**一处有意的偏离**：原版把屏震的自动档放在 `OnEnable`，我们放在 `Initialize`。
理由：运行时装配时 `AddComponent` 会**立刻**触发 `OnEnable`，而字段是 `AddComponent` **之后**才
`Configure` 填的 ⇒ 放 `OnEnable` 会用空参数播。`Initialize` 是**两条路（数据装配 / 手工挂）都会走**
的入口，放这儿两边都对。

---

## 四、想加一个新模块，只动一个文件

```csharp
// Assets/WarpforgeVFX/Runtime/WFModuleXxx.cs
using UnityEngine;
namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleXxx")]            // ← 原版类名，**这一行就是登记**
    public class WFModuleXxx : WFEffectModule
    {
        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);                  // ⚠️ 必调（读 actionStart）
            var v = def.GetFloat("someField");    // 键就是原版字段名（点号路径）
        }
        public override void Initialize(WarpforgeEffectPlayer c) { base.Initialize(c); }
        public override void ModuleTick(float dt) { }
        public override void Exit() { }
        public override void DoDestroy() { }
    }
}
```
- 取引用：`WFEffectModule.ResolveNode<ParticleSystem>(transform, def.GetString("particleSystems[0]"), "WFModuleXxx")`
  （**失败会自己打警告**，不会静默）。
- **不用改工厂、不用改播放器**。`__node` 已经由播放器处理，你类里的 `transform` 就是对的那个节点。
- 参考实现：`WFModuleScreenShake.cs`（含数据读取 + 诊断计数 + `@asset:` 名字解析）。

---

## 五、和「自制特效」的关系

同一批模块类，**两种装法**：

| | 原版那 958 个 | 你自己做的特效 |
|---|---|---|
| 怎么装 | 运行时按 `entry.modules` **数据装配** | 在编辑器里**手挂**（拖引用、调参数） |
| 参数在哪 | SO 里的数据 | Inspector 里就是真的 |
| 要不要重导 | 不要 | 不要（见 `资料/自制特效_加入流程.md`） |

⚠️ 手挂的模块由播放器的 `BuildModules` 一并收进 `_modules` 并广播 `Initialize`，
**和原版那批走同一套生命周期**。

---

## 六、已经接到表现层的：屏震 + 卡上下文

`WFModuleScreenShake.OnShake` 是**下游钩子**（VFX 层不认识相机）——
`BattleDriver.HookAnimFxShake()` 挂上它 → `CardFeel.ShakeCamera`（相机 + `HudRoot` 同向平移）。
⚠️ **幅度换算是我们推的**：原版看 `amplitude × |direction|`，归一到 `Shake Hit Small`
（`CardFeel.ShakeWorldAmplitude` 就是这么推出来的）；原版的 `rawSignal`（Beautify 波形）**没复刻**。
另一条已接的是 **`WFEffectCards.Resolver`**（出手卡/目标卡上下文）—— `BattleDriver.HookAnimFxCards()`
在 `PlaySignal` 调 `FireEvent` 前后填/清「当前上下文」（整条是同步的，所以模块读得到）。
🔴 **2026-09-18 更正：这里原来写「其余 9 个钩子还没接」—— 与 §八 表对不上。**
按 §八 那张表**实际是「接了 4 个 / 未接 7 个」**（含 🟡 `ColliderLookup`）。**以 §八 表为准**（要引用就现数，别抄这一行）。

---

## 七、实现清单（2026-09-18）

**15 个模块类全部实现**，外加屏震那条链已接到表现层。数据覆盖：**895 个效果 / 2313 个模块**
（`animfx_modules.json` 有 2346 个，差的 33 个挂在 **UI / 棋盘 prefab** 上、不在效果库的 958 个里 —— 本来就不该收）。

| 原版类 | 实例 | 文件 | 说明 |
|---|---:|---|---|
| `AnimFXModuleScreenShake` | 430 | `WFModuleScreenShake.cs` | **已接下游**（`OnShake` → `CardFeel.ShakeCamera`） |
| `AnimFXModuleCollisions` | 355 | `WFModuleCollisions.cs` | 碰撞平面表逐支照反编译；下游 `ColliderLookup`/`ContextResolver` **未接** |
| `AnimFXModuleScaleByTarget` | 314 | `WFModuleScaleByTarget.cs` | ⚠️ `ChangeShapeAngle` **未还原**（`tanf/atan2f` 操作数配对读不出来，影响 97/314） |
| `AnimFXModuleTween` | 138 | `WFModuleTween.cs` | 顺序语义 + `alreadytrigger` 门闩还原；**补间本体路由到 `OnInvoke`（未接）** |
| `AnimFXModulePostProcess` | 50 | `WFModulePostProcess.cs` | 优先级仲裁 + 三段淡入淡出还原；**上屏那半路由到 `OnPostFx`（未接）** |
| `AnimFXModuleCardback` | 30 | `WFModuleCardback.cs` | ⚠️ 卡背 sprite **还在工程外**；导出侧 `textureSheetAnimation` 的 sprite 列表是死的 |
| `AnimFXModuleTransformModifier` | 18 | `WFModuleTransformModifier.cs` | 同时定义共用的 `WFEffectCards` / `WFEffectCardContext`；4 条分支未还原（各带警告+计数） |
| `AnimFXModuleAnimation` | 5 | `WFModuleAnimation.cs` | ⚠️ 我们**没有 SimpleAnimation**，退到 Animator/legacy Animation；controller 为空 ⇒ 现在播不出画面 |
| `AnimFXModuleChangeMaterial` | 3 | `WFModuleChangeMaterial.cs` | ⚠️ `fade`/`materialAnimations` 未还原（MaterialTween 资产 0 份导出）；3 张卡材质工程里没有 |
| `AnimFXInstanceParticleAdjacent` | 1 | `WFModuleInstanceParticleAdjacent.cs` | ⚠️ 延时用 `ModuleTick` 按 `delay` 推 = **推断**；相邻单位靠 `OnExecute`（未接） |
| `AnimFXModuleDoDestroyAnimation` | 1 | `WFModuleDoDestroyAnimation.cs` | ⚠️ clip 没导进来 ⇒ 只报不播；且播放器广播完 `DoDestroy` **立刻销毁**（原版「有模块就不销毁」） |
| `AnimFxModuleMoveParticlesToTarget` | 1 | `WFModuleMoveParticlesToTarget.cs` | ⚠️ 插值算式 = **推断**（原式丢了）；目标靠 `ResolveTarget`（未接） |
| `AnimFXModuleDestroyInTime` | 1 | `WFModuleDestroyInTime.cs` | ✅ 自读 `actionStart`（0/5 两条起算点都还原） |
| `AnimFXModuleEvent` | 1 | `WFModuleEvent.cs` | ⚠️ **UnityEvent 调什么查不到**（dump 把 `m_Calls` 截断了）⇒ 到点只报警告，不猜 |
| `AnimFXModuleChangeVelocity` | 0 | `WFModuleChangeVelocity.cs` | 抛体公式**逐字照抄**；输入靠静态钩子（未接）。原版 `applyToVelocityModule` 分支**自己也没实现** |
| `AnimFXParticleCollisionNotifier` | 0 | `WFModuleParticleCollisionNotifier.cs` | 刻意**不继承** `WFEffectModule`（原版它也不是 `AnimFXModuleBase` 子类） |
| （控制器）`sounds` / `exitSounds` | 937 | —— | 🔴 **未接线**：`sound` 指向一层 MonoBehaviour 包装（资产名是卡名），AudioClip 在它里面。累计记账 `WarpforgeEffectPlayer.UnwiredSoundCues` |

---

## 八、下游钩子：接了 4 个，还剩 7 个（2026-09-18 更新；⚠️ 原来写「8 个」，与下面那张表对不上 —— 表里是 4✅ + 1🟡 + 6❌）

VFX 层**不认识牌局/相机/后处理**，所以每个模块把「我做完了，该谁接手」暴露成静态钩子。
**没接的钩子会打一次性警告 + 计数**（不静默）。

| 钩子 | 谁要 | 现状 |
|---|---|---|
| `WFModuleScreenShake.OnShake` | 屏震 | ✅ **已接**：`BattleDriver.HookAnimFxShake()` → `CardFeel.ShakeCamera` |
| `WFEffectCards.Resolver` | `TransformModifier` / `ChangeMaterial` / `Cardback` / `ParticleAdjacent` | ✅ **已接**：`BattleDriver.HookAnimFxCards()`（用「当前上下文」，`PlaySignal` 前后填/清） |
| `WFModuleScaleByTarget.CardResolver` | 缩放 | ✅ **已接**（同一份 `_animfxCtx` 转发进去 —— 两处**同源**，不许各查一次） |
| `WFModuleCollisions.ContextResolver` | 碰撞平面的卡片上下文 | ✅ **已接**（同源；`targetIsWarlord` 用格位判） |
| `WFModuleCollisions.ColliderLookup` | `BattleCollider id → Transform` | 🟡 **数据已查全，只差换算**（见 §八之补）：7 个都是**场景里手摆的固定 Transform**，坐标已读出 —— **`*FromCamera` 那个和 `PlayerWarlord` 坐标一模一样**，它**不是按相机算的**。⚠️ 我 2026-09-18 一度写成「查不到」，**是错的**（没去查就下了结论）。现在的位置：坐标是**原版 arena 世界系**，要接到我们棋盘得走「格位节距 149.3 px」那座桥 |
| `WFModuleCardback.CardbackResolver` | 卡背 | ❌ 未接 —— 233 张卡背在工程外 `素材/Warpforge原版/卡背/`，还没导进来 |
| `WFModuleChangeMaterial.{SetCardMaterial,RestoreOriginalMaterial,CardTexture,MaterialResolver}` | 换材质 | ❌ 未接 —— 那 3 张卡材质在工程里没有 |
| `WFModuleInstanceParticleAdjacent.OnExecute` | 相邻特效 | ❌ 未接 —— 要「哪个单位跟它相邻」（棋盘在规则引擎里） |
| `WFModuleTween.OnInvoke` | 补间 | ❌ 未接 —— 要一层 DOTween 等价物（`UnitTweenSO.BuildSequence` + 6 个 tween 子类） |
| `WFModulePostProcess.OnPostFx` | LUT/Bloom | ❌ 未接 —— 要 `Hidden/LUTBlender` shader（不在工程）+ `PostFXController` 那一套 |
| `WFModuleMoveParticlesToTarget.ResolveTarget` | 吸粒子 | ❌ 未接 —— 要 HUD 灵石图标的屏幕位置 |

⚠️ **没接钩子 ≠ 模块没实现** —— 模块该算的都算了（数据读取、全部分支、参数覆盖），
只是「最后那一下」没人接。**接一个钩子通常是一行**（照上面两个已接的写法）。

---

## 九、验收

```bash
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod AnimFXCheck.Run -logFile -
```
验四件：**数据到位**（895 个效果 / 2313 个模块，按两文件交集断言）· **实现覆盖**（13 种 kind 全认得）·
**装配真的发生**（每种 kind 挑一个效果播，断言**这个 kind** 的模块建出来了 ——
判据不能写成「装了模块数 > 0」，一个效果常带好几个模块，写松了会让没实现的顶成绿的）·
**全量路径零失败**（895 个效果的 `__node` + 1231 条 `@node:` 引用逐条解析）。

数据侧复跑（改过 dump/拍平之后）：
```bash
PY="D:/2/Warpforge_tools/py312/python.exe"
"$PY" "d:/4/Unity/工具/dump_animfx.py"        # 读 bundle（几分钟）
"$PY" "d:/4/Unity/工具/gen_animfx_modules.py"  # 拍平
"$PY" "d:/4/Unity/工具/gen_shake_presets.py"   # 屏震 preset
```

---

## 十、这一轮修掉的两个「数据其实在、只是没读出来」

| 原来 | 实际 | 代价 |
|---|---|---|
| 嵌套序列化类读不出（`<UnknownObject<...>>`）—— 记成「拿不到值」 | **值就在 `UnknownObject.__dict__` 里**（UnityPy 的 `__repr__` 就是遍历它打的，只是每值截到 100 字符） | **1919 处**嵌套字段全丢，正好是最值钱的：`sounds` 937 · `cameraShakes` 508 · `collisionAndParticles` 450 |
| 对象引用只记了**类型**（`{"__ptr__": "ParticleSystem"}`）—— 组件没有 `m_Name`，退化成类型名 | 顺 `m_GameObject` → 父链能算出**根之下的完整路径**（带兄弟序号） | 约 **719 个**带引用的模块（ScaleByTarget 314 · Collisions 357 · Cardback 30 · TransformModifier 18）**没法还原** |

⚠️ 试过但**没成**的路（别再撞）：UnityPy 的 `TypeTreeGenerator` + 本机
`MelonLoader/Il2CppAssemblies/*.dll`（157 个）或 `load_local_game` —— 都报
`failed to dump nodes raw` / `Sequence contains no matching element`。
（`TypeTreeGeneratorAPI` 已按铁律 8 装进 `D:/2/Warpforge_tools/py312/`，将来别的活可能用得上。）


---

## 八之补、`BattleCollider` 那 7 个到底是什么（2026-09-18 查全）

> ⚠️ **更正**：本节是一次「说查不到、其实查得到」的记录。原话是「`*FromCamera` 两个原版怎么定的查不到」——
> 两处都错：**只有一个 `FromCamera`**，而且**查得到**。**教训同铁律 2：没去查 ≠ 查不到。**

**源头**（都在本地，一次就能查完）：

1. 桩文件 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleParticleColliderManager.cs` ——
   枚举 `BattleCollider{Floor=0, Player=5, PlayerWarlord=7, PlayerWarlordFromCamera=8, Enemy=10, EnemyWarlord=11, GenericTarget=15}`
   + **7 个 `[SerializeField] Transform` 字段**。
2. 反编译方法体 `d:/2/tools/decomp_animfx_nested2/BattleParticleColliderManager__GetColliderTransform.c` ——
   逐值返回 `+0x20 / +0x28 / +0x30 / +0x38 / +0x40 / +0x48 / +0x50`，**和上面 7 个字段一一对应**。
   ⇒ **7 个都是「场景里手摆的 Transform」，没有一个是算出来的。**
3. 场景全树 `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:825` 的 `Particle colliders script` 底下
   正好挂着这 7 个物体（名字逐一对上）。
4. 坐标从 `d:/2/解包整理/07_场景/battlearena1/` 读（`GameObject/<名字>.json` → `m_Component[0].component.m_PathID`
   → `Transform/Transform_<pathID>.json`）。

**battlearena1 的值**（localPosition，父 = `Particle colliders`）：

| id | 物体名 | localPosition | localScale |
|---|---|---|---|
| 0 `Floor` | Floor Position Reference | (0, 0, **0**) | 1.0 |
| 5 `Player` | Player Minions Particle Collision | (0, 0, **−6.698**) | 2.5 |
| 7 `PlayerWarlord` | Player Warlord Particle Collision | (0, 0, **−7.119**) | 2.5 |
| **8 `PlayerWarlordFromCamera`** | Player Warlord **From Camera** Particle Collision | (0, 0, **−7.119**) | 2.5 |
| 10 `Enemy` | Enemy Minions Particle Collision | (0, 0, **0.966**) | 2.5 |
| 11 `EnemyWarlord` | Enemy warlord Particle Collision | (0, 0, **0.218**) | 2.5 |
| 15 `GenericTarget` | Generic Target | (0, 0, **0.218**) | 2.5 |

🔴 **`PlayerWarlordFromCamera` 和 `PlayerWarlord` 坐标完全相同** ⇒ 「FromCamera」是**用途名**（粒子从相机方向飞过来的那条路），
**不是一种算法**。别再去翻相机相关的东西。

⏭️ **接线还差的那一步**：这 7 个坐标是**原版 arena 的世界系**，要接到我们的棋盘得用那座桥
（**格位节距 149.3 px**，见 `记忆：原版 3D 尺寸怎么搬进来` / `资料/` 对应文档）。
⚠️ 我们战场目前**只摆了一张烘平的背景图**（见 `资料/战场还原度_差距清单_0917.md`），
这 7 个碰撞体的对应物**要先在场景里建出来** —— 这是接线前的一步实打实的活。

---

## 十一、🔴 全量反编译复核（2026-09-18）—— **1 必须改 + 3 建议改**

> **为什么单开这一节**：这一层当初是**明写「只能按语义重写」**做的 —— 因为 2026-09-18 之前
> `decomp_il2cpp_0827/` 里**一个 AnimFX 类的方法体都没有**（桩文件方法体全空）。
> **现在整个游戏的代码都反编译了**（`D:/2/tools/decomp_full/`，`DECOMP DONE ok=26275 fail=7`，
> 含 401 个协程体；AnimFX 相关 **118 个 `.c`，18 个类一个不缺**）。
> ⇒ 这一节就是「**当时靠推断写的东西，现在拿方法体逐条对**」的结果。
> 判读前提与查法见 `资料/全量反编译_入口与用法.md`。

### 11.1 ✅ 已改（1 处，**434 个实例受影响**）—— 2026-09-18 修完，**待复跑自检**

**`WFModuleScreenShake.TriggerCameraShake` 读错了数组。**

| | |
|---|---|
| 字段表 | `dump.cs:48610,48612`：`cameraShakes // 0x38` · `manualTriggerCameraShakes // 0x40` |
| **原版** | `TriggerCameraShake` 读 **`param_1 + 0x40`**（`decomp_full/AnimFXModuleScreenShake__TriggerCameraShake.c:15`，**越界报错用的也是这个数组的长度**）；`AnimEventDoShake` **也读 `0x40`**（`...__AnimEventDoShake.c:7`）⇒ **两个入口用的是同一个数组 `manualTriggerCameraShakes`**；**只有 `OnEnable` 才读 `cameraShakes`**（`...__OnEnable.c:8`） |
| **我们** | `TriggerCameraShake` 读成了 **`cameraShakes`**（`WarpforgeVFX/Runtime/WFModuleScreenShake.cs:87,93`；文件头注释 `:7` 也这么写着）❌ |
| **修法** | `TriggerCameraShake` 改读 `manualTriggerCameraShakes`（越界报错文案里的数组名与长度一并对齐），文件头 `:5-7` 那三行注释同步改 |

⚠️ 2026-09-18 复核者**亲验过**（字段表 + 两个 `.c` 的偏移都实读）。修完要复跑
`AnimFXCheck.Run` + `BattleScene.Run`。

### 11.2 🟡 建议改（3 处）

| # | 内容 | 影响 |
|---|---|---|
| ⓐ | **`ChangeVelocity` 的 `ModifyVelocity` 少写两个值**：原版 `..._ModifyVelocity.c:131-137` 用同一个比例 `v / 旧 constantMax` **同时**写了 `set_x1(...)` 与 `set_outWeight(...)`，`:146` 才 `set_startSpeed` ⇒ **原版是按比例缩放整条 `startSpeed` 曲线的两个端点**；我们只写了 `speed.constantMax`（`WFModuleChangeVelocity.cs:204-206`）⚠️ 那两个 setter 对应 `MinMaxCurve` 的哪两个成员**没判定出来**（Ghidra 猜的名字不可信），但「多写两个值、都乘同一比例」是确定的 | **该模块出货数据 0 个实例** ⇒ 今天零影响 |
| ⓑ | **寿命模型**：原版 `AnimFXController__OnEnable.c:20-27` 是 `if (!preventDestroy) Object.Destroy(gameObject, destroyTime)` —— **直接销毁，不调 `Exit`**。全量 grep `AnimFXController__Exit` 的外部调用点**只有** `BattleCardUI__CleanStatusAnims.c` / `RemoveStatusAnim.c` ⇒ **原版在 `destroyTime` 那条路上，`AnimFXModuleAnimation.Exit`（退场动画）/ `DestroyInTime.Exit` / `ChangeMaterial.Exit` 根本不会跑**。而我们 `Tick` 到寿命会**先调 `Exit()`** | 这条当初是**反推**的，建议按新证据复核 |
| ⓒ | **`MoveParticlesToTarget.LateUpdate`**：`guaranteeFinalPosition(0x58)` 的 `if` **不只包 `Evaluate`**，还包住「读粒子位置 + `Time.deltaTime`」（`...__LateUpdate.c:47-61`）⇒ 更偏向「**为真 = 不吸**」。我们那条推断要么重验、要么在注释里降级 | 注释级 |

### 11.3 ✅ 复核后确认**不用动**的（当时的判断被证实）

- **基类 `Exit` / `DoDestroy` 是空实现** —— 原版**编译结果就是空函数**（`AnimFXModuleBase__Exit.c` / `__DoDestroy.c`）⇒ 当初的设计决定**被证实**
- `WFModuleDestroyInTime` **零差异**（`__Initialize.c:20-23` / `__Exit.c:16-22` 两条分支都只有 `Destroy(gameObject, destroyTime)`）
- `WFModuleTween`：`playOnEnable(0x40)` 起播、`alreadytrigger(0x42)` 门闩**逐条吻合**；`triggerOnlyOnce(0x41)` 五个方法体**一次没读过** ⇒ 「只留痕不判断」的决定被证实
- `WFModuleChangeMaterial`：`==0` 开 · **`==5` 也开** · `toggleMaterialOffOnExit(0x43)` 关 ✔
- `WFModuleEvent`：`whereToFire == 0/5` 分派 ✔
- `WFModuleCardback`：`GetCardback(actingCard.isPlayer)` → 空则 LogError+return ✔（唯一差异是**报错先后与原版相反**，无害）
- `isRetaliation = IsPlayerTurn() XOR actingCard.isPlayer` 逐支证实（`AnimFXModuleBase__Initialize.c:30-70`）
- `WFModuleInstanceParticleAdjacent`：**「延迟实现是推断」可以升级为已证实** —— `..._d__4__MoveNext.c:22-31` = `new WaitForSeconds(module.delay(0x40))` → `ExecuteEffect()`；`Initialize` 闸门 `playOnRetaliation(0x44) || !isRetaliation(0x30)` 与 `ExecuteEffect.c:80-108` 的 8 个实参逐字对得上
- 有意偏离且复核后**不需要动**：Cardback 报错顺序 · Collisions 未知枚举不抛异常 · ScreenShake/Tween 的 `OnEnable→Initialize` 位移 · ChangeVelocity 的 ÷0 兜底
- `ScaleByTarget.ChangeShapeAngle` 锥角：**仍不能逐位还原**，但线索清楚了 —— 「兵线距离」= `BattleParticleColliderManager` 的 `playerMinionCollider(0x28)` / `enemyMinionCollider(0x40)`（`dump.cs:48922-48928`），且 `tan(angle·Deg2Rad)` 确实在 `atan2` 里（`...__ChangeShapeAngle.c:99-112`）。97/314 实例仍与原版不一致 ⇒ **保持「不改 + 警告 + 计数」可接受**

### 11.4 🎁 `sounds` 937 条 —— **从「标了+报警」变成「可以做完」**

原文 §八 把这批记成「没还原」。**触发点已找到，而且数据齐**：

- **播放点** = `AnimFXController.Update`（`decomp_full/AnimFXController__Update.c:31-70`）：
  `currentTime(0x60) += Time.deltaTime` → 逐条 `sounds[i].Update(currentTime, transform)`；
  **之后 `if (exiting(0x64))`** 再逐条 `exitSounds[i].Update(...)`
- **每条怎么判** = `PlaySoundOnTime.Update`（`...__Update.c:29-41`）：
  `n = repeat(0x21) ? loops(0x24) : 1`；`if (n <= numberOfTimesPlayed(0x2c)) return`；
  `if (elapsed < numberOfTimesPlayed * timeInterval(0x28) + time(0x10)) return`；
  否则 `is2d(0x20) ? Play2D(cue 0x18) : Play3D(cue, transform.position)`；`numberOfTimesPlayed++`
- **另一条独立入口**：`AnimFXController.PlaySound`（`...__PlaySound.c:20-27`）= 立刻 `Play3D` 一次，不吃定时
- **数据在哪**：`数据/游戏数据/animfx_modules.json` —— **854 个控制器**带
  `sounds[0].{time,sound,is2d,repeat,loops,timeInterval}`（+ 68 个 `[1]` / 12 个 `[2]` / 3 个 `[3]` / 7 个 `exitSounds[0]` … ≈ 937 条），
  **键名与 `PlaySoundOnTime` 的 6 个序列化字段一一对应**；`sounds[0].sound` 形如 `@asset:MonoBehaviour:Mark of Nurgle`
- **我们的触发点现成**：`WarpforgeEffectPlayer._time`（`:93`）就是 `currentTime` 的同款单调钟
  （`Tick:275` 累加、`Exit:294` **不复位** —— **与原版一致**，所以 `exitSounds` 的 `time` 也是从 `Play` 起算）。
  接线只需在 `Tick:279-280` 的模块广播旁**加一条 `sounds` 广播**；
  `NoteUnwiredSounds:354-366`（由 `:245` 调）已经在数条数，**可直接替换**

⚠️ 还有一处**原版有、我们完全没有**：`AnimFXModuleAnimation__Exit.c:16-19` 的
`SimpleAnimation.Play(0x38, "…")` —— 需要 `SimpleAnimation` 组件才跑得起来。

### 11.5 复核交接里可以升级的一句话

§八 里那些「**⚠️ 没还原的**」条目里，`sounds` 那条**现在可以移出「没还原」**（见 11.4）。
其余（`ChangeShapeAngle` / `Tween` 本体 / `PostProcess` 上屏 / `Cardback`）结论不变。

### 11.6 🎁 `ChangeShapeAngle` 锥角 —— **能复刻，半天可出可验收版本**（2026-09-18 查全）

> 上面 11.3「可以不改」里那条 `ChangeShapeAngle`，**现在有确切路径了**。
> 本文**只记结论与坐标，不动手**；要动手照下面两件实活走。

#### a) 那 7 个「碰撞体」到底是什么（已逐个查实）

= `BattleParticleColliderManager.BattleCollider` 的 **7 个枚举值 ↔ 7 个 `[SerializeField] private Transform`（单数，不是数组）**。
原版场景里是空父节点 `Particle colliders` 下的 7 个空物体：

| 枚举(值) | 字段 | 物体名 | localZ（相对 `Particle colliders`） | scale |
|---|---|---|---|---|
| `Floor`(0) | `floorCollider` | Floor Position Reference | **0.000** | 1.0 |
| `Player`(5) | `playerMinionCollider` | Player Minions Particle Collision | **−6.698** | 2.5 |
| `PlayerWarlord`(7) | `playerWarlordCollider` | Player Warlord Particle Collision | **−7.119** | 2.5 |
| `PlayerWarlordFromCamera`(8) | `playerWarlordColliderFromCamera` | Player Warlord **From Camera** Particle Collision | **−7.119** | 2.5 |
| `Enemy`(10) | `enemyMinionCollider` | Enemy Minions Particle Collision | **+0.966** | 2.5 |
| `EnemyWarlord`(11) | `enemyWarlordCollider` | Enemy warlord Particle Collision | **+0.218** | 2.5 |
| `GenericTarget`(15) | `genericTargetCollider` | Generic Target | **+0.218** | 2.5 |

- 父链 `Particle colliders` ← `BattleBoardElements`（**x = +100**）← 根 ⇒ **世界 X 全是 +100，只有 Z 不同**（**兵线 = Z 轴**）。
  归属：**Z<0 玩家侧**（0/5/7/8）· **Z>0 敌方侧**（10/11/15）。
- 🆕 **`FromCamera` 与 `PlayerWarlord` 只差旋转、不差坐标** —— 且那个旋转**与敌方三个的朝向完全一致**
  （T_1265 rot=(0,−0.7071,−0.7071,0) vs 敌方 T_1446/1380/1276）。**再次坐实「不是按相机算的」。**
- 🆕 这 7 个 Z 在 **四个 arena 逐位相同**（battlearena1/2/aeldari/astramilitarum）⇒ 常量在**共享 BattlePrefab** 里，
  不是每战场各摆一套（13 个战场全树都有这 7 个）。
- **出处**：`dump.cs:48900-48928` · `解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_4504.json:8-38` ·
  同目录 `Transform/Transform_{1355,1327,1265,1365,1446,1380,1276,1438,1301,1300}.json` ·
  `说明书/01_战斗_对战/2D层_battlearena1全树.md:826-833`

#### b) 谁用它们

- `GetColliderTransform` 是**纯查表**（0/5/7/8/10/11/15 → `+0x20/+0x28/+0x30/+0x38/+0x40/+0x48/+0x50`），其余 return null。
- **只有两个用户**：
  - `AnimFXModuleCollisions.CollisionAndParticles.Initialize`：按「哪一方 × 是否督军」选 id
    （玩家侧 `2×warlord+5` → 5/7；敌方侧 `warlord+10` → 10/11；另有 8 与 0xF 两支）。
    **选到 15（`GenericTarget`）时当场改写它**：`set_up(normalize(target.pos − acting.pos))` + `set_position(target.pos)`
    ⇒ **它是唯一运行时可动的**。
  - **`ScaleByTarget.ChangeShapeAngle` 不走 `GetColliderTransform`**，直接读单例的 `+0x28`/`+0x40` 取 `position`
    ⇒ **锥角只用 7 个里的 2 个**（`Player` / `Enemy` 两条兵线）。
- **「兵线距离」= `|playerMinionCollider.pos − enemyMinionCollider.pos|` = 7.664 世界单位**（世界 X 都是 100 ⇒ 纯深度差）；
  另一半「卡距」= `|targetCard.pos − actingCard.pos|`（controller `+0x58` / `+0x50`）。
- **出处**：`decomp_full/BattleParticleColliderManager__GetColliderTransform.c:5-25` ·
  `AnimFXModuleCollisions.CollisionAndParticles__Initialize.c:100-130` ·
  `AnimFXModuleScaleByTarget__ChangeShapeAngle.c:60-70`

#### c) 那段数学 —— 三个单位已钉死

| 符号 | 真身 | 旁证 |
|---|---|---|
| `FUN_18048a0d0` | **`Mathf.Tan`** | `CamerasConversionHelper` 里 `Tan(半FOV×Deg2Rad)` |
| `FUN_180486da0` | **`Mathf.Atan2`** | `UILineTextureRenderer` 里 `Atan2(dy,dx)` · `CalculateSecondProjectile` 里 `Atan2(v²x,d)` |
| `DAT_1834b2dc0` | **`Deg2Rad`** | 角度×它再进 Tan/Sin |
| `DAT_1834b2e98` | **`Rad2Deg`** | `GetFovFromCamera` 里 `(2×半角)×它` = 视角度数 |

🔑 **由此可定一条结构结论**：`atan2` 的结果要 `×Rad2Deg` 才能变度 ⇒ **它两个操作数必须是「长度」不是角度**
⇒ 公式形状必然是 **`newAngle = atan2(t × L_a, L_b) × Rad2Deg`**
（「把标定好的锥斜率按长度比换回来」）。**与原版 Tooltip 原话一致**：
「必须在**兵线距离**上、一前一后摆两个卡 prefab 标定这条粒子」。

🔴 **当年为什么没读出配对**：两个 helper 是**经 VA 直接调**的、Ghidra 不知签名 ⇒ 浮点实参留在 **XMM** 未实体化成 C 参数
⇒ `.c` 里出现 `FUN_180486da0()` **一个参数都没有**（`:114`）、`tan` 的结果**整个被丢**（`:109` 无赋值）、
sqrt 结果与两个碰撞体 position 也**没有落点**；中间还夹着会清寄存器的调用（`get_localScale`/`ForceAsync`）。
**要的那几条数据流只活在寄存器里。**

✅ **现在能不能**：**能**。函数有 RVA/VA（`0x668E00` / `0x180668E00`），两个 helper 的 VA 也有
（`0x18048a0d0` / `0x180486da0`）—— **把这 ~40 条指令反汇编出来即见 xmm 传参**。
Ghidra 12.1.3 + 已导入工程都在盘上，但 headless listing **会写文件**，得由主对话跑。

#### d) 🔴 要在我们场景里建什么（**含一处换算更正**）

- 桥：我们格位节距 **149.3 px = 1.382 我们世界单位**；两行中心线 = 玩家 **708 px** / 敌方 **466 px**
  ⇒ **相距 242 px = 2.24 我们世界单位 = 1.621 格距**。
- 🔴 **别把 arena 的 7.664 世界单位乘一个 px 系数搬过来** —— 项目里两个 px/单位读数**互相矛盾**：
  - `CardPresentation/.../TargetReticle.cs:47` 写 **49.77** ← **错**：它把「槽距 **3.0**」当成世界单位了，
    而 3.0 是 `MinionArea` 的**归一化槽位步长**（`leftSlotPosNormal` 步长 3.0）
  - `CardPresentation/Board/BoardLayout.cs:17-19` 的 **k = 182.14** 才是**世界单位**那把
  - 两者差 **3.66 倍**（= 3.0 / 0.82，那个 0.82 是 `MinionSeparation`）
- ✅ **歧义可以绕开**：公式**只需要比值**（原版两个距离都在同一世界系里），而我们横向格距本来就 = 原版屏上 149.3 px、
  两行 242 px 也 = 原版屏上那 242 px（708−466，**已逐位复刻**）⇒ **一律照「投影距离」建**，这个歧义自然消失。
- **最小可用版（只为 `ChangeShapeAngle`）= 两个空物体**：
  `playerMinionCollider` 摆**玩家行中心线**上、`enemyMinionCollider` 摆**敌方行中心线**上，X 都对齐**督军槽中心**，**相距 242 px**。
  这两条就是「兵线」—— 与原版作者标定时用的是同一个东西。
- **其余 5 个**（要接 `Collisions` 才需要）：按「到玩家兵线的**格距**」换算
  （`slotZ = (z + 6.698) / 7.664 × 1.621`）：
  `Player` **0.000** · `PlayerWarlord` **−0.089** · `PWF` **−0.089** · `Floor` **+1.417** ·
  `Enemy` **+1.621** · `EnemyWarlord` **+1.463** · `GenericTarget` **+1.463**
  （X = 督军槽中心；`PWF` 照原版用**与敌方相同**的朝向；`GenericTarget` 必须能被 `set_position`/`set_up`）。
- ⚠️ 原版那 **2.5 的 scale** 是粒子/碰撞的量纲，接 `Collisions` 时要一起过桥；本节只解决 `ChangeShapeAngle`。

#### e) 总判

**能 —— 但要先做两件实活，都不是「查不到」：**

1. **反汇编那 ~40 条指令**（VA `0x180668E00` 起），把 `atan2` 的操作数配对钉死。**这是唯一的未知数** ——
   7 个物体、坐标、两个距离的来源、`Tan`/`Atan2`/`Deg2Rad`/`Rad2Deg` 的单位**都已查实**。
2. **场景里建 2 个（或 7 个）空物体** + 一个 manager 组件，按 d) 的位置摆。

**工作量级：小 —— 半天内可出可验收版本**（listing 一次 + 两个空物体 + 实现十几行）。
之后 `WFModuleScaleByTarget.ChangeShapeAngle` 从「不改角度 + 警告 + 计数」换成真算法，
**97/314 的警告计数应当归零**。

⚠️ **三条残余风险（先说清）**：
① 即便配对拿到，**跨行目标的 `d_actual` 在原版是 3D 深度、我们 2D 只能给投影距离** ⇒ 97/314 里跨行那部分可能仍有小偏差，
   只能用「一律取投影量」保持一致；
② 公式里的 `localScale` 取自 **`+0x58` 那侧（目标卡）**，**别接成出招卡**；
③ 若反汇编发现它还依赖 arena 绝对世界系（比如那个 **X = 100**），这条桥也得照搬 —— 但按 c) 的结构
   （`atan2` 里只有长度比）可能性很低。
