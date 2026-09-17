# AnimFX 模块层 —— 实现与接线（2026-09-18 建立）

> **这份讲什么**：原版那 18 个 `AnimFXModule*` 是怎么搬进我们工程的 ——
> **数据从哪来、运行时怎么装、想加一个新模块该动哪几个文件**。
> 语义（每个模块干什么）在 `资料/AnimFX_18类方法体_块1.md` / `_块2.md`，**这里不重复**。

---

## 一、现在到哪一步（一句话）

**18 类读完方法体 → 数据链打通（895 个效果 / 2313 个模块）→ 运行时框架就位 → 15 个模块类全部实现 → 下游钩子接了 4 个、还剩 8 个。**
判据看 `-executeMethod AnimFXCheck.Run` 末尾的 `=== 合计：N 通过 / M 失败 ===`。

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
**其余 9 个钩子还没接**，清单与各自缺什么见 §八。

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

## 八、下游钩子：接了 4 个，还剩 8 个（2026-09-18 更新）

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
