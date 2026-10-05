// ScenarioBlendables.cs — 原版那族「环境混合组件」（`IScenarioEnvironmentBlendeable`）的复刻
//
// 判据 = **反编译方法体逐句**（`d:/2/tools/decomp_full/Scenario*__*.c`），不是按类名猜。
// 实测挂载数（`数据/游戏数据/env_blendables.json` 的 `by_class`，两边合计 128 条）：
//   `ScenarioParticleSystemBlender` 62 · `ScenarioParticleSystemToggler` 36 · `ScenarioMaterialFader` 23
//   `ScenarioParticleSpawnerBlender`  4 · `ScenarioGenericObjectToggler`   3
//
// 🆕 **2026-10-06 战-A：`ScenarioParticleSpawnerBlender`（那 4 条）已经补上了** —— 连同它要的
//   原版 `ParticleSystemAreaSpawner` / `ParticleSystemAreaSpawnerController` / `ParticleSystemPoolable`
//   三个组件（都在这份文件里，见文件末尾那一段）。它们的 6 个序列化字段**走旁挂**
//   （`EnvBlendables.Target.fields`，`工具/gen_env_blendables.py` 直读 bundle 生成）。
//
// ⚠️ **9 个实现类现在【全在了】**（🔴 2026-10-07 波9离线订正：原来这里写着「我们只做 5 个、另 4 个连旁挂都没收」）：
//   `FlareScenarioToggler` 6 个实例 · `ScenarioAnimationBlend` 2 · `ScenarioGenericMaterialBlend` 1 ·
//   `TauCannonAnimationStopper` 2（**共 11 个**）已进旁挂（`gen_env_blendables.py` 的 `CLASSES`）与本文件的工厂；
//   合计 **139 = 128 + 11**。
// 🆕 **2026-10-12（A393）：另加了「场景侧、不被任何 blendable 管」的那一节** —— 15 个 `scenes_scenes_*`
//   包里一共 7 个 `AnimFXController`，上面那条链只覆盖 tauviorla 的 2 个；另 **5 个**（`battlearena2`
//   的 `RocketTrail` · `battlearena3` 的两个 `Lightning_Green` · tauviorla 的 `Big Gun Effect` 与
//   `Railgun BIG (1)`）原来**连账都没有**。它们走 `ScenarioBlendableFactory.BuildSceneAnimFx`
//   （旁挂 `sceneStandalone` / `sceneStandaloneBuild`；**建法是复用同一个 `MakeAnimFx`**，不另写一份）。
//   ✅ **2026-10-12（A417）调用点已接**（🔴 **订正**：原来这两行写着「⚠️ **调用点还没接**」）——
//   接在 `Battle/ArenaRuntimeLoader.cs` 的 `Load()` 里（`CurrentKey = arenaKey;` 之后那一行）。
//   ⚠️ **只此一处**（原文写「在 `ArenaRuntimeLoader` / `EnvironmentApplier` 那两个文件里」，H2 §5.1 已裁掉后者）：
//   ① 时机 —— `EnvironmentApplier.EnsureSceneBlendables` 那条链**第一次跑已经是「打出一张进攻卡」**
//      （`BattleDriver` 里 `_envApplier.Apply` 的唯一调用点）⇒ 放那儿这 5 条**大半局都不存在**，与原版不符；
//   ② 去重 —— 那边传的根是 `Arena3D`、这边是 `Warpforge_<场>`，`BuildSceneAnimFx` 按**传进来的 root** 去重
//      ⇒ **两个都接会建两遍**（节点是无条件 `new GameObject`），别再补第二处。
//   判据 → `资料/普查产出_1012/H2_场景侧AnimFX.md` §五。
//   ⚠️ **但其中两类的【资产】还有缺口**（如实记着，别当成已做）：
//     · `ScenarioAnimationBlend`：🆕 **2026-10-07（A192）那两个 clip 已经收进 `wf_prefabs_extra.bundle`**
//       （`LightAnimationOrbit` · `Dark Angels Void Combat animations`，按 **assetGUID** 登记容器别名
//       ⇒ 见 `ScenarioBlendableFactory.AnimationClipByGuid`）。🔴 **但仍有一条不会播**，而且**不是「我们漏了」**：
//       ① ✅ **2026-10-11（A191）已补**：宿主 `Battle Arena Dark Angels baked` 已照原版建成**真分组节点**，
//          并且那颗节点与 `Directional Light` 上的 `Animation` 组件**也按原版补上了**
//          （`ArenaBuilder.ApplyGroupNodes` + 旁挂 `<场>_groups.json`；上面那半句「我们一件 arena prefab 里
//          一个 `Animation` 组件都没有」和「宿主我们工程里没有」**都已不成立**）；
//       ② ⛔ **照抄原版数据的结果**：另一颗的组件 `filterCode` 实读是 `LightAnimationOrbital`、而 SO 那条写
//          `LightAnimationOrbit`（**原版自己差一个 `al`、永远配不上**，照抄不改数据）。
//       ③ `AnimationClipByGuid` 那一跳**已有一条自检真的跑过**（`Editor/BattleScene.cs` 的 A201 一节）。
//     · `TauCannonAnimationStopper`：✅ **2026-10-11（A196）`LookAtConstrainWIP` / `AnimFXController`
//       两个类已经移植进工程了**（`Battle/LookAtConstrainWIP.cs` · `Battle/AnimFXController.cs`）。
//       ✅ **2026-10-11（A191）四个宿主对象也照原版建出来了**（`Railgun Turret 1/2` 两个带 `Animation` 的
//       父节点 · `Railgun turret` 那颗 `AnimFXController` 的宿主 · `LookAtConstrainWIP.target` 指的
//       `Railgun Turret N Target` 两件）—— 建场侧 `ArenaBuilder.ApplyGroupNodes`，旁挂
//       `arenas/battlearenatauviorla/battlearenatauviorla_groups.json`（由 `工具/gen_arena_groups.py` 直读原版场景包）。
//       ⚠️ `Railgun Turret N Target` 那两件**从来不在** `_missingTargets` 里（那张表只查 blendable 自己的 target、
//       不查「target 组件自己的字段」）⇒ 旁挂的应收处是 `LookAtConstrainWIP` 目标的 `fields`（`k = "target"`）。
//  逐条缺口 + 判据 → `资料/普查产出_1007/波9离线_A136_A135.md`；旁挂里还有一张 `_missingTargets` 逐条记着；
//   A192 那两条 clip 的收尾（含「为什么收进来还不播」）→ `资料/普查产出_1007/波9_A192_两个clip进包.md`。
//
// 🔴 **批处理下没有帧循环**（CLAUDE.md §三）⇒ 每个组件的推进都做成**可手动 `Advance(dt)`**，
//    `Update()` 只是实时那条路；自检一律手动推（与 `EnvironmentApplier.Advance` 同一套）。
using System;
using UnityEngine;
using UnityEngine.Pool;      // 原版 `ParticleSystemAreaSpawner` 的池子就是 `UnityEngine.Pool.ObjectPool<ParticleSystem>`
                            // （判据 = 反汇编里那句 `new ObjectPool<ParticleSystem>(create, onGet, onRelease, onDestroy, …)`）
// 🆕 2026-10-07：`FlareScenarioToggler` 管的是 `LensFlareComponentSRP`（`Unity.RenderPipelines.Core.Runtime`）。
// ⚠️ **用别名、别 `using UnityEngine.Rendering;`** —— 那个命名空间里**也有一个 `ObjectPool<T>`**，
//    与 `UnityEngine.Pool.ObjectPool<T>` 撞名 ⇒ 上面那行池子会 CS0104「不明确的引用」。
using LensFlareComponentSRP = UnityEngine.Rendering.LensFlareComponentSRP;

namespace CardPresentation
{
    /// <summary>原版 `ScenarioBlendOptions`（字段名照原版，别改写）：
    ///   `ScenarioBlendTime`(+0x10) · `ScenarioBlendDirection`(+0x24) · `ScenarioBlendTargetValue`(+0x20) ·
    ///   `OnComplete`(+0x18) · `FilterOptions`(+0x28)。
    /// 🔴 **2026-10-06 战-A 订正（原来这里写着「FilterOptions 我们不复刻 …… 本来就是恒等」，那是错的）**：
    ///   `FilterOptions`(+0x28) **有消费方**——`ScenarioGenericMaterialBlend.DoScenarioBlend`
    ///   （判据 = `decomp_full/ScenarioGenericMaterialBlend__DoScenarioBlend.c`：
    ///    `if (!string.IsNullOrEmpty(this.filterCode)) { if (options.FilterOptions == null) 抛 NRE;
    ///     if (this.filterCode != options.FilterOptions.FilterCode) return; }` ⇒ **不匹配就整条跳过**；
    ///    命中后还会用 `FilterOptions.isEnabled` **覆盖** 那一程的 `direction/targetValue`）。
    ///   它不是恒等：**它让「非空 filterCode 的组件」只在对应那条 SO 下才响应** —— 全库恰好**一对**：
    ///   GSC 场 `Battle Arena Genestealers Baked/Floor` 上那个 `ScenarioGenericMaterialBlend`
    ///   （`filterCode = "SumpOverspill"`）↔ SO `EnvironmentalCondition GSC Sump Overspill`
    ///   （`filterCode = "SumpOverspill"` · `filterEnabled = 1` · `defaultScenarioObjectsState = 0`）。
    /// ✅ **2026-10-07 波9离线：这一半已经落地** —— 三个构造点（实例侧 / 场景侧 / 撤环境那一路）都灌了值，
    ///   SO 那两个字段也进了 `Resources/EnvironmentConditions.json`（生成器补写）与 `EnvironmentConditions.Item`。
    ///   完整裁定与逐条证据 → `资料/普查产出_1006/战A_第3_4条.md`「第 4 条」+ `资料/普查产出_1007/波9离线_A136_A135.md`。</summary>
    public struct ScenarioBlendOptions
    {
        public float duration;      // `SO.blendTime`（`instant` 时为 0）
        public bool direction;      // true = 淡入/开（新环境）· false = 淡出/关（撤环境）
        public float targetValue;   // 原版与 direction 同源：true→1.0 / false→0.0
        public Action onComplete;   // 撤环境那条链靠它计数，**全部到齐才 Destroy**
        /// <summary>原版 `options.FilterOptions`（`+0x28`）—— **只有 `ScenarioGenericMaterialBlend` 读它**。
        /// 我们这边**恒非 null**（`EnvironmentConditions.Item` 即使 `filterCode` 是空串也会建一个出来；
        /// `FilterCode` 空 ⇒ 消费方不做过滤）。null 只可能出现在手搓的 options 上（那是调用方的错，消费方会出声）。</summary>
        public ScenarioBlendOptions.BlendFilterOptions filterOptions;

        /// <summary>原版**嵌套类** `ScenarioBlendOptions.BlendFilterOptions`（TypeDefIndex 744）：
        /// `FilterCode`(+0x10) · `isEnabled`(+0x18)。</summary>
        [Serializable]
        public class BlendFilterOptions
        {
            public string filterCode;   // 原版 `FilterCode`
            public bool isEnabled;      // 原版 `isEnabled`
        }
    }

    public interface IScenarioBlendable { void DoScenarioBlend(ScenarioBlendOptions o); }

    /// <summary>四个类的公共部分：`{记下 options → 等 duration → 回调}`。
    /// 原版每个类各写一份（`Update` 里 `currentBlendTime -= dt`），这里合一处 —— **判据相同**。</summary>
    public abstract class ScenarioBlendable : MonoBehaviour, IScenarioBlendable
    {
        protected ScenarioBlendOptions _opts;
        protected float _left;            // 原版 `currentBlendTime`
        protected bool _busy;

        public bool Busy { get { return _busy; } }
        public float Left { get { return _left; } }

        /// <summary>这个组件**会不会回调 `OnComplete`** —— 撤环境那条链靠它计数
        /// （原版 `SO.DisableEnvironment` 里 `SO+0x78 = blendables.Length`，每个回调减一、归零才 `Destroy`）。
        /// 🔴 **实测原版 `ScenarioParticleSystemToggler.DoScenarioBlend` 从不调 `OnComplete`**
        ///   （方法体里只有一句 `emission.enabled = direction`）⇒ 原版那个计数**永远归不了零**、
        ///   旧实例**根本不会被销毁**（留在场上，只是粒子关着）。
        ///   我们照「**哪些会回调**」计数 ⇒ 到点就销毁：**视觉等价、且不留泄漏**。
        ///   这条差异如实记着（判据 → `资料/加时与冲突模式_原版规格.md` 的 2026-09-30 那一节）。</summary>
        public virtual bool NotifiesComplete { get { return true; } }

        /// <summary>原版那族组件是**去问 `ScenarioEnvironmentConditionsManager`** 要「当前环境是哪个 SO」的
        /// （`ScenarioAnimationBlend.DoScenarioBlend` 读 `Manager.CurrentEnvironment` +0x28）。
        /// 我们这边的「管理器」就是 `EnvironmentApplier` ⇒ 建组件之后由它接上（`EnvironmentApplier` 里那句
        /// `c.manager = this`）。**只有需要「当前 SO」的那个类会读它**，别的类不受影响。</summary>
        public EnvironmentApplier manager;

        public abstract void DoScenarioBlend(ScenarioBlendOptions o);

        /// <summary>推进一步（**批处理/自检唯一的路**）。子类可覆写（例如 Blender 的「等粒子跑完」）。</summary>
        public virtual void Advance(float dt)
        {
            if (!_busy) return;
            _left -= dt;
            if (_left <= 0f) Complete();
        }

        protected virtual void Complete()
        {
            _busy = false;
            var cb = _opts.onComplete;
            _opts.onComplete = null;
            if (cb != null) cb();
        }

        // ⚠️ **这里故意没有 `Update()`** —— 推进权收归 `EnvironmentApplier.AdvanceBlendables` **一处**
        //   （批处理下没有帧循环 ⇒ 只能手推；而实时那条路若两边都推就会**推两遍**）。
        //   场景侧的组件挂在战场对象上、实例侧的挂在 prefab 实例上，但**都由执行器推**（它是唯一的驱动者）。

        protected static void SetEmission(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            var e = ps.emission;      // 结构体，改完要写回（Unity 的 API 就是这样）
            e.enabled = on;
        }
    }

    /// <summary>`ScenarioParticleSystemToggler`（7/43 个 env prefab · 13 场场景侧 36 个实例）。
    /// 判据 = `ScenarioParticleSystemToggler__DoScenarioBlend.c`：
    ///   `foreach (ps in particleSystems) ps.emission.enabled = options.ScenarioBlendDirection;`
    /// ⇒ **全有或全无、立即生效、不等 duration、没有 OnComplete**。
    /// 🔴 **不是**「按 `priority` 关掉一部分 emitter」（那是没读方法体时的猜测；`priority` 属
    ///   `InitializableBehaviour`，只用于初始化排序）。</summary>
    public class ScenarioParticleSystemToggler : ScenarioBlendable
    {
        public ParticleSystem[] particleSystems;

        /// <summary>**不回调**（原版就没有回调那一段，见基类的说明）。</summary>
        public override bool NotifiesComplete { get { return false; } }

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (particleSystems != null)
                for (int i = 0; i < particleSystems.Length; i++)
                    SetEmission(particleSystems[i], o.direction);
        }
    }

    /// <summary>`ScenarioParticleSystemBlender`（39/43 个 env prefab · **13 场场景侧全都有**）。
    /// 判据 = `ScenarioParticleSystemBlender__DoScenarioBlend.c` + `__Update.c` + 协程 `WaitForBlendTime`：
    ///   `if (!o.direction || forceEnableEmittersOnEnable)  foreach (ps) ps.emission.enabled = (o.direction || force);`
    ///   `if (!notifyFinishWhenNoParticles) { 起协程(等 duration) → OnComplete } else { Update 里等所有 ps.particleCount==0 → OnComplete }`
    /// ⇒ **direction=true 且没开 `force` 时它【什么都不做】**（粒子 `playOnAwake` 本来就是开的）；
    ///    direction=false 才关 —— **这条就是「放进攻卡会把战场自己那批粒子关掉」的那一半**。</summary>
    public class ScenarioParticleSystemBlender : ScenarioBlendable
    {
        public ParticleSystem[] particleSystems;
        public bool forceEnableEmittersOnEnable;
        public bool notifyFinishWhenNoParticles;

        bool _waitNoParticles;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            bool on = o.direction || forceEnableEmittersOnEnable;
            if (!o.direction || forceEnableEmittersOnEnable)
            {
                if (particleSystems != null)
                    for (int i = 0; i < particleSystems.Length; i++)
                        SetEmission(particleSystems[i], on);
            }
            _opts = o;
            _busy = true;
            if (notifyFinishWhenNoParticles) { _waitNoParticles = true; _left = 0f; }
            else { _waitNoParticles = false; _left = Mathf.Max(0f, o.duration); }
            if (!_waitNoParticles && _left <= 0f) Complete();     // instant ⇒ 立刻回调
        }

        public override void Advance(float dt)
        {
            if (!_busy) return;
            if (_waitNoParticles)
            {
                if (AllParticlesIdle()) Complete();
                return;
            }
            base.Advance(dt);
        }

        bool AllParticlesIdle()
        {
            if (particleSystems == null) return true;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps != null && ps.particleCount > 0) return false;
            }
            return true;
        }

        protected override void Complete()
        {
            _waitNoParticles = false;
            base.Complete();
        }
    }

    /// <summary>`ScenarioMaterialFader`（12/43 个 env prefab · 13 场场景侧若干）。**唯一真会「淡」的那个**。
    /// 判据 = `ScenarioMaterialFader__{DoScenarioBlend,CacheOriginalMaterials,Update,RestoreOriginalMaterials,ToggleRenderers}.c`：
    ///   · 第一次调用先 `CacheOriginalMaterials`（记 `renderer.sharedMaterial`）
    ///   · `Update` 里按 `currentBlendTime` 把每个 renderer 的**材质 `color.a`** 往 `targetValue` 推
    ///   · 完成时：`(restoreOriginalMaterialsOnFadeOut && !direction) || (restoreOriginalMaterialsOnFadeIN && direction)`
    ///     ⇒ 还原原材质（销毁我们实例化的那份）· `disableOnFadeOut && !direction` ⇒ `renderer.enabled = false`
    /// ⚠️ **算式里有几处是被混淆过的**（那个 `doFade = !doFade` 的取反、alpha 的起值），
    ///   这里按「起点 = 当前 `color.a`、终点 = `targetValue`、线性推 `duration` 秒」落地 ——
    ///   **这一条是「尽力还原」，不是逐句对齐**，如实记着（实测有差就改这里，别改调用方）。</summary>
    public class ScenarioMaterialFader : ScenarioBlendable
    {
        public Renderer[] renderers;
        public bool fadeOnEnable;
        public bool isInDefaultScenario;
        public bool restoreOriginalMaterialsOnFadeOut;
        public bool restoreOriginalMaterialsOnFadeIN;
        public bool disableOnFadeOut;

        Material[] _orig;
        float _fromA = 1f;
        float _toA = 1f;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (_orig == null) CacheOriginalMaterials();
            _fromA = CurrentAlpha();
            _toA = o.targetValue;
            _opts = o;
            _busy = true;
            if (o.duration <= 0f) { ApplyFade(1f); Complete(); return; }
            _left = o.duration;
        }

        public override void Advance(float dt)
        {
            if (!_busy) return;
            base.Advance(dt);                       // 到点会调 Complete()
            if (_busy) ApplyFade(_opts.duration <= 0f ? 1f : Mathf.Clamp01(1f - _left / _opts.duration));
        }

        void ApplyFade(float t)
        {
            if (renderers == null) return;
            float a = Mathf.Lerp(_fromA, _toA, t);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                var m = r.material;                 // 取实例（改的是我们自己的那份）
                if (m == null) continue;
                var c = m.color;
                c.a = a;
                m.color = c;
            }
        }

        float CurrentAlpha()
        {
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && renderers[i].sharedMaterial != null)
                        return renderers[i].sharedMaterial.color.a;
            return 1f;
        }

        void CacheOriginalMaterials()
        {
            if (renderers == null) { _orig = new Material[0]; return; }
            _orig = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                _orig[i] = (renderers[i] != null) ? renderers[i].sharedMaterial : null;
        }

        protected override void Complete()
        {
            bool dir = _opts.direction;
            if ((restoreOriginalMaterialsOnFadeOut && !dir) || (restoreOriginalMaterialsOnFadeIN && dir))
                RestoreOriginalMaterials();
            if (disableOnFadeOut && !dir && renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = false;
            base.Complete();
        }

        void RestoreOriginalMaterials()
        {
            if (renderers == null || _orig == null) return;
            for (int i = 0; i < renderers.Length && i < _orig.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                var inst = r.sharedMaterial;
                if (inst != null && (i >= _orig.Length || inst != _orig[i]))
                {
                    if (Application.isPlaying) Destroy(inst); else DestroyImmediate(inst);
                }
                if (_orig[i] != null) r.sharedMaterial = _orig[i];
            }
        }
    }

    /// <summary>`ScenarioGenericObjectToggler`（1/43 个 env prefab · aeldari 场景侧 2 个）。
    /// 判据 = `ScenarioGenericObjectToggler__{DoScenarioBlend,Update}.c`：
    ///   记下 options 与 duration → **等 duration 到点** → `foreach (go) go.SetActive(direction)` → `OnComplete`。</summary>
    public class ScenarioGenericObjectToggler : ScenarioBlendable
    {
        public GameObject[] gameObjects;
        public bool isInDefaultScenario;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            _opts = o;
            _busy = true;
            _left = Mathf.Max(0f, o.duration);
            if (_left <= 0f) Complete();
        }

        protected override void Complete()
        {
            if (gameObjects != null)
                for (int i = 0; i < gameObjects.Length; i++)
                    if (gameObjects[i] != null) gameObjects[i].SetActive(_opts.direction);
            base.Complete();
        }
    }

    // ==========================================================================================
    // ③ `ScenarioParticleSpawnerBlender`（原版 4 个实例，全在**环境 prefab 侧**）＋ 它要的三件组件
    //    2026-10-06 战-A 补（原来这里只有一句「还没复刻 ⇒ 出声」）。
    //
    // 第一权威 = `d:/2/tools/decomp_full/` 的**方法体**（逐句读过，件名都写在各自的方法注释里）；
    //   类型形状（字段名 / 偏移 / 默认值）= `d:/2/tools/il2cpp_out/dump.cs`：
    //     `ScenarioParticleSpawnerBlender` TypeDefIndex 755（`controllers` 0x20 · `areaSpawners` 0x28 · `scenarioBlendOptions` 0x30）
    //     `ParticleSystemAreaSpawner` 1104 · `ParticleSystemAreaSpawnerController` 1107（嵌套 `ParticleSpawnDefinition` 1105）· `ParticleSystemPoolable` 1108
    // 🔴 两处**只有反汇编能给**（`.c` 里参数/常量被丢掉），已按 x64 反汇编定（`.text` 节：文件偏移 = RVA − 0x1600）：
    //   · `ScenarioParticleSpawnerBlender.DoScenarioBlend` RVA 0x630DB0：`movzx edx,[rsi+0x24]` ⇒ 它调
    //     `Controller.Toggle(...)` 传的**也是 `options.ScenarioBlendDirection`**（`.c` 那行只印了 `this`）。
    //   · 常量 `DAT_1834b2bb4 = 0.5f`（⇒ 随机区间 = `±boxSize/2`）· `DAT_1834b2bb8 = 1.0f`（⇒ `WaitForSeconds(1/spawnRate)`）
    //     · `DAT_1834b2c80 = 0x80000000`（符号位 ⇒ Ghidra 把它印成了 `^ DAT_…` 的取负写法）。
    // ⚠️ 批处理下没有帧循环 ⇒ 三条**协程**（两个 `SpawnParticles` + 那两处 `StartCoroutine`）都不在这里起，
    //   改成**手动推进** `Advance(dt)`，由 `EnvironmentApplier.AdvanceBlendables` 一处驱动（与本文件其它组件同一套口径）。
    // ==========================================================================================

    /// <summary>`ScenarioParticleSpawnerBlender`（4 个实例 · 每个只引 1 条 `ParticleSystemAreaSpawner`、`controllers` 全是空）。
    /// 判据 = `ScenarioParticleSpawnerBlender__{DoScenarioBlend,Update,IsAnyParticleSystemActiveInControllers,IsAnyParticleSystemActiveInAreas}.c`：
    ///   · `DoScenarioBlend(o)`：记下 `o` → 对**每个 controller** 调 `Toggle(o.ScenarioBlendDirection)`
    ///     → 对**每个 areaSpawner** 调 `Toggle(o.ScenarioBlendDirection)`。**不等 duration、当场生效、不回调**。
    ///   · `Update()`：`if (o != null && !o.ScenarioBlendDirection && !IsAnyParticleSystemActiveInControllers()
    ///     && !IsAnyParticleSystemActiveInAreas()) o.OnComplete?.Invoke();`
    ///     ⇒ **只有「撤环境」那一程**才等生成器静下来再回调（apply 那一程永不回调）。
    /// ⚠️ 我们**没有**自己的 `Update`（推进权收归执行器一处）⇒ 那句轮询挪进 `Advance`。
    /// 🔴 `OnComplete` 在 `Update` 里是**每帧**调的（原版如此）；我们走基类 `Complete()`，它把
    ///   `_opts.onComplete` 置空 ⇒ **只回调一次**（这是本文件既有的口径：撤环境那条链按「每个组件回调一次」计数）。</summary>
    public class ScenarioParticleSpawnerBlender : ScenarioBlendable
    {
        public ParticleSystemAreaSpawnerController[] controllers;
        public ParticleSystemAreaSpawner[] areaSpawners;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            _opts = o;
            _busy = true;                       // 原版 `Update` 的判据是「options 引用非 null」，这里用同一件事
            if (controllers != null)
                for (int i = 0; i < controllers.Length; i++)
                    if (controllers[i] != null) controllers[i].Toggle(o.direction);
            if (areaSpawners != null)
                for (int i = 0; i < areaSpawners.Length; i++)
                    if (areaSpawners[i] != null) areaSpawners[i].Toggle(o.direction);
        }

        public override void Advance(float dt)
        {
            // 原版这两个数组里的组件各自跑协程；我们这里**顺势把它们推一步**（它们没有别的驱动者）。
            if (controllers != null)
                for (int i = 0; i < controllers.Length; i++)
                    if (controllers[i] != null) controllers[i].Advance(dt);
            if (areaSpawners != null)
                for (int i = 0; i < areaSpawners.Length; i++)
                    if (areaSpawners[i] != null) areaSpawners[i].Advance(dt);

            if (!_busy || _opts.direction) return;                       // 原版：只在 direction==false 这一程轮询
            if (IsAnyParticleSystemActiveInControllers()) return;
            if (IsAnyParticleSystemActiveInAreas()) return;
            Complete();                                                  // = 原版那句 `o.OnComplete?.Invoke()`
        }

        /// <summary>判据 = `…__IsAnyParticleSystemActiveInControllers.c`（逐个转调 `IsAnyParticlePoolActive`）。</summary>
        public bool IsAnyParticleSystemActiveInControllers()
        {
            if (controllers == null) return false;
            for (int i = 0; i < controllers.Length; i++)
                if (controllers[i] != null && controllers[i].IsAnyParticlePoolActive()) return true;
            return false;
        }

        /// <summary>判据 = `…__IsAnyParticleSystemActiveInAreas.c`（逐个转调 `IsAnyPoolItemActive`）。</summary>
        public bool IsAnyParticleSystemActiveInAreas()
        {
            if (areaSpawners == null) return false;
            for (int i = 0; i < areaSpawners.Length; i++)
                if (areaSpawners[i] != null && areaSpawners[i].IsAnyPoolItemActive()) return true;
            return false;
        }
    }

    /// <summary>`ParticleSystemPoolable`（TypeDefIndex 1108）—— 池子的**回收那一跳**。
    /// 判据 = `ParticleSystemPoolable__{Start,OnParticleSystemStopped,OnValidate,AssignParticleSystemReference}.c`
    /// （`SetCallbackOnStop` 的方法体与 `Start` 逐字相同 —— 反编译件里两份内容一样）：
    ///   `main = myParticleSystem.main; main.stopAction = Callback;`（反汇编里那句 `set_stopAction(&main, 3)`；
    ///   `ParticleSystemStopAction.Callback` = 3）⇒ 粒子停 ⇒ `OnParticleSystemStopped()` ⇒ `pool.Release(myParticleSystem)`。
    /// 🔴 **没有它，池子只进不出**（`Get` 每次都新建一颗 ⇒ `CountInactive` 恒 0、
    ///   `IsAnyPoolItemActive` 恒真 ⇒ 「撤环境」那条链永远等不到 OnComplete）。</summary>
    public class ParticleSystemPoolable : MonoBehaviour
    {
        [SerializeField] ParticleSystem myParticleSystem;
        public IObjectPool<ParticleSystem> pool;

        void Start() { SetCallbackOnStop(); }

        void SetCallbackOnStop()
        {
            if (myParticleSystem == null) return;
            var main = myParticleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        void OnParticleSystemStopped()
        {
            if (pool != null) pool.Release(myParticleSystem);
        }

        public void AssignParticleSystemReference(ParticleSystem particleSystemToCache)
        {
            myParticleSystem = particleSystemToCache;
            // ⚠️ 我们把原版 `Start()` 里那句 `SetCallbackOnStop()` **提前到这里**：
            //   原版靠 `Start()`（要等一帧），而**批处理下没有帧循环** ⇒ 不这么写它在我们自检里永远收不回池。
            //   play 模式下与原来等价（同一个值设两遍）。**这是有意的偏离，如实记着。**
            SetCallbackOnStop();
        }

        void OnValidate()
        {
            if (myParticleSystem == null) myParticleSystem = GetComponent<ParticleSystem>();
            SetCallbackOnStop();
        }
    }

    /// <summary>原版 `ParticleSystemAreaSpawner`（TypeDefIndex 1104）—— 「在 boxSize 盒子里按速率随机生粒子」。
    /// 6 个序列化字段照原版（名字/类型/默认值全是 `dump.cs` 的字段表 + `.c` 里 ctor 的默认值：
    /// `boxSize (1,1,1)` · `maxPoolSize 5` · `useAutomaticSpawn true` · `spawnRate 1` · `chances 1`）。
    /// 判据 = `ParticleSystemAreaSpawner__{OnEnable,Toggle,SpawnParticle,SpawnParticles,IsAnyPoolItemActive,get_Pool,
    ///   CreatePooledItem,GetPoolObject,OnReturnedToPool,OnDestroyPoolObject,GetRandomPositionInsideBox,OnDrawGizmosSelected}.c`
    ///   ＋ `<SpawnParticles>d__14.MoveNext.c`（那条循环）。
    /// ⚠️ 三个偏离，都是**为了在批处理里能推**（原版靠 Unity 的帧循环/协程）：
    ///   ① 循环不做成协程，改 `Advance(dt)`（由 `ScenarioParticleSpawnerBlender.Advance` 推）；
    ///   ② `OnEnable` 那两句挪进 `Configure(...)` 并由 `_ready` 挡住「`AddComponent` 当场触发的那一次」
    ///      （原版是反序列化完才 OnEnable —— 那时字段已经有值，而我们是运行时挂组件、字段还没填）；
    ///   ③ `Start()` 里 `StopAllCoroutines` 保留（我们没协程，留着只为逐句对齐）。
    /// 🔴 `Toggle(false)` **不恢复**模板粒子的 active（原版只在 `OnEnable` 里关它一次 —— 别自作聪明加回来）。</summary>
    public class ParticleSystemAreaSpawner : MonoBehaviour
    {
        // 原版这 6 个都是 `[SerializeField] private`（`spawnRate`/`chances` 还带 `[ShowIf("useAutomaticSpawn")]`、
        // `chances` 带 `[Range(0,1)]`）—— 我们这里**放成 public**：与本文件其它几个类同一套口径
        //（运行时由旁挂灌值、自检读回来核对），且不引 NaughtyAttributes。
        public Vector3 boxSize = new Vector3(1f, 1f, 1f);   // 原版 ctor 默认 (1,1,1)
        public ParticleSystem particleSystemPrefab;
        public int maxPoolSize = 5;
        public bool useAutomaticSpawn = true;
        public float spawnRate = 1f;
        public float chances = 1f;

        int totalPoolItems;                                 // 原版 +0x48（CreatePooledItem 里 +1 · OnDestroyPoolObject 里 −1）
        IObjectPool<ParticleSystem> m_Pool;                 // 原版 +0x50
        readonly bool collectionChecks = true;              // 原版 +0x58（ctor 里 = 1）
        bool _ready;                                        // 旁挂把 6 个字段填好了没有（见类注释 ②）
        bool _running;                                      // 原版 = 「那条协程在不在跑」
        float _t;                                           // 原版 = 距下一颗的累计秒（`WaitForSeconds(1/spawnRate)`）

        /// <summary>原版 `get_Pool`（判据 = `…__get_Pool.c`）：第一次访问时建池 ——
        /// `new ObjectPool<ParticleSystem>(CreatePooledItem, GetPoolObject, OnReturnedToPool, OnDestroyPoolObject,
        ///  collectionChecks, defaultCapacity: 10, maxSize: maxPoolSize)`，并**先把 `totalPoolItems` 清零**。
        /// 那 4 个回调各是哪一个，是拿反汇编里的 `Method$…` 名逐个核过的（不是按参数位置猜的）。</summary>
        public IObjectPool<ParticleSystem> Pool
        {
            get
            {
                if (m_Pool == null)
                {
                    totalPoolItems = 0;
                    m_Pool = new ObjectPool<ParticleSystem>(
                        CreatePooledItem, GetPoolObject, OnReturnedToPool, OnDestroyPoolObject,
                        collectionChecks, 10, maxPoolSize);
                }
                return m_Pool;
            }
        }

        /// <summary>旁挂填字段 + 起循环（**等价于原版的 `OnEnable`** —— 见类注释 ②：
        /// `particleSystemPrefab.gameObject.SetActive(false)` 然后 `if (useAutomaticSpawn)` 起循环）。</summary>
        public void Configure(Vector3 box, ParticleSystem template, int maxPool, bool autoSpawn,
                              float rate, float chance)
        {
            boxSize = box;
            particleSystemPrefab = template;
            maxPoolSize = maxPool;
            useAutomaticSpawn = autoSpawn;
            spawnRate = rate;
            chances = chance;
            _ready = true;
            OnEnableBody();
        }

        void OnEnable()
        {
            // `AddComponent` 当场就会调到这里 —— 那时旁挂还没填字段（`_ready == false`）⇒ 什么都不做。
            // 手动挂在 prefab 上的（`_ready` 已置）再被启用时，走的就是原版那条 OnEnable。
            if (!_ready) return;
            OnEnableBody();
        }

        void OnEnableBody()
        {
            if (particleSystemPrefab == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}` 的 `particleSystemPrefab` 是空的 —— "
                             + "原版这里会抛 NullReferenceException；我们出声并停手（不许静默）");
                _running = false;
                return;
            }
            // 判据 = 反汇编（RVA 0x675E10）：`prefab.gameObject.SetActive(false)` 在**起循环之前**，且**无条件**。
            particleSystemPrefab.gameObject.SetActive(false);
            _running = useAutomaticSpawn;
            _t = 0f;
        }

        /// <summary>判据 = `…__Toggle.c` + 反汇编（RVA 0x676310）：`StopAllCoroutines(); if (option) StartCoroutine(SpawnParticles());`
        /// —— **开与关都先停**。我们的循环不是协程（`Advance` 手推），`StopAllCoroutines` 留着只为逐句对齐。</summary>
        public void Toggle(bool option)
        {
            StopAllCoroutines();
            _running = option;
            _t = 0f;
        }

        /// <summary>判据 = `…__IsAnyPoolItemActive.c`：`Pool.CountInactive != totalPoolItems`
        /// ⇒ 真 = **有池里的东西被借出去了**（= 场上还有活着的粒子）。</summary>
        public bool IsAnyPoolItemActive() { return Pool.CountInactive != totalPoolItems; }

        /// <summary>原版那条循环的**一步**（判据 = `<SpawnParticles>d__14.MoveNext.c`：
        /// `while (true) { if (Random.value <= chances) SpawnParticle(); yield return new WaitForSeconds(1f / spawnRate); }`）。
        /// 🔴 用的是 `UnityEngine.Random`（**原版就是它** —— 这是表现层，不进引擎的确定性那条链）。</summary>
        public void Advance(float dt)
        {
            if (!_ready || !_running) return;
            if (spawnRate <= 0f) return;                    // 原版会 `WaitForSeconds(∞)` ⇒ 永不生成（等价）
            float step = 1f / spawnRate;
            _t += dt;
            if (_t < step) return;
            _t -= step;
            // 🔴 **一次 `Advance` 最多生成一颗** —— 原版那条协程带 `yield`，**一帧只被恢复一次**
            //（哪怕这一帧耗时 600 s）。自检里 `AdvanceBlendables(600f)` 那种大 dt 就靠这一条挡住：
            // 不这么写，600 s 会一次补出「每 7.7 s 一颗 × 78 拍」一串 `Instantiate`。
            if (_t > step) _t = 0f;                         // 落后太多 ⇒ 别把下一拍也提前（重新起算）
            if (UnityEngine.Random.value <= chances) SpawnParticle();
        }

        /// <summary>判据 = `…__SpawnParticle.c`：池里取一颗 → 摆到 `transform.position ± boxSize/2` 的随机点 →
        /// `Play(true)`（withChildren）。取到 null（`GetPoolObject` 报过错了）时原版**把池丢掉重建**再取一次。
        /// 🔴 位置是**世界轴**加减（`.c` 里直接拿 `transform.position` 加三个 `Random.Range`）。</summary>
        public void SpawnParticle()
        {
            Vector3 pos = GetRandomPositionInsideBox();
            ParticleSystem ps = Pool.Get();
            if (ps == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}`：池里取不到粒子（`particleSystemPrefab` 那条链断了）"
                             + " —— 照原版把池丢掉重建后再取一次");
                m_Pool = null;
                ps = Pool.Get();
            }
            if (ps == null) return;
            ps.transform.position = pos;
            ps.Play(true);
        }

        /// <summary>判据 = `…__GetRandomPositionInsideBox.c` + 常量：`transform.position + (每轴
        /// `Random.Range(-box/2, +box/2)` )`。</summary>
        Vector3 GetRandomPositionInsideBox()
        {
            Vector3 p = transform.position;
            return new Vector3(
                p.x + UnityEngine.Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f),
                p.y + UnityEngine.Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f),
                p.z + UnityEngine.Random.Range(-boxSize.z * 0.5f, boxSize.z * 0.5f));
        }

        /// <summary>判据 = `…__CreatePooledItem.c`：`Instantiate(particleSystemPrefab, transform)` →
        /// `GetComponent<ParticleSystemPoolable>()` → 没有就 `LogError` + `AddComponent` + 接上粒子引用 →
        /// `poolable.pool = Pool` → `totalPoolItems++`。
        /// （那两个泛型实参是从反汇编的 `Method$UnityEngine.Component.GetComponent<ParticleSystemPoolable>()`
        ///  与 `AddComponent<ParticleSystemPoolable>()` 读出来的，不是猜的。）</summary>
        ParticleSystem CreatePooledItem()
        {
            ParticleSystem ps = Instantiate(particleSystemPrefab, transform);
            if (ps == null) return null;
            var poolable = ps.GetComponent<ParticleSystemPoolable>();
            if (poolable == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}`：粒子 prefab 上没有 `ParticleSystemPoolable`"
                             + "（回收那一跳靠它）—— 照原版就地补一个");
                poolable = ps.gameObject.AddComponent<ParticleSystemPoolable>();
                if (poolable != null) poolable.AssignParticleSystemReference(ps);
            }
            if (poolable == null) return null;
            poolable.pool = Pool;
            totalPoolItems++;
            return ps;
        }

        /// <summary>判据 = `…__GetPoolObject.c`（= `ObjectPool` 的 `actionOnGet`）：
        /// `system == null || system.gameObject == null` ⇒ `LogError` 后**原样返回**（池就把 null 交出去，
        /// 由 `SpawnParticle` 兜）；否则 `SetActive(true)`。</summary>
        void GetPoolObject(ParticleSystem system)
        {
            if (system == null || system.gameObject == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}`：池里的粒子已经被销毁（`ObjectPool` 交出了 null）");
                return;
            }
            system.gameObject.SetActive(true);
        }

        /// <summary>= `actionOnRelease`。判据 = `…__OnReturnedToPool.c`：`system.gameObject.SetActive(false)`。</summary>
        void OnReturnedToPool(ParticleSystem system)
        {
            if (system != null && system.gameObject != null) system.gameObject.SetActive(false);
        }

        /// <summary>= `actionOnDestroy`。判据 = `…__OnDestroyPoolObject.c`：`totalPoolItems--`
        /// （反汇编 RVA 0x674790 就一句 `dec [rcx+0x48]` —— 池满时 `Release` 会走销毁这一跳）。</summary>
        void OnDestroyPoolObject(ParticleSystem system) { totalPoolItems--; }

        /// <summary>判据 = `…__OnDrawGizmosSelected.c`：`Gizmos.DrawWireCube(transform.position, boxSize)`。</summary>
        void OnDrawGizmosSelected() { Gizmos.DrawWireCube(transform.position, boxSize); }
    }

    /// <summary>原版 `ParticleSystemAreaSpawnerController`（TypeDefIndex 1107）—— 一组生成器的**加权随机**调度器。
    /// 判据 = `ParticleSystemAreaSpawnerController__{OnEnable,Toggle,SpawnParticle,SelectRandomWeightedItem,
    ///   IsAnyParticlePoolActive,FetchAllParticleSpawners}.c` · `<SpawnParticles>d__6.MoveNext.c` ·
    ///   `…ParticleSpawnDefinition__{TrySpawnParticles,IsAnyPoolItemActive}.c`。
    /// ⚠️ **这一类在我们数据里 0 个实例**（4 条 blender 的 `controllers` 全是空数组；bundle 里那 3 个
    ///   controller 自己 `startOnEnable` 自启、不被任何 blendable 引用）⇒ 移植是为了**完备**（铁律 11），
    ///   运行时目前不会有人建它。它同样改成 `Advance(dt)` 手推。</summary>
    public class ParticleSystemAreaSpawnerController : MonoBehaviour
    {
        /// <summary>原版嵌套类（TypeDefIndex 1105，字段：spawner 0x10 · weight 0x18 · chances 0x1C）。
        /// ⚠️ 它是**普通 `[Serializable]` 类**（dump.cs:49909 里没有基类、字段从 0x10 起）——
        ///   这决定了下面 `FetchAllParticleSpawners` 那条不能照抄，见那儿的注释。</summary>
        [Serializable]
        public class ParticleSpawnDefinition
        {
            public ParticleSystemAreaSpawner particleSystemAreaSpawner;   // 原版 `[SerializeField] private`
            public float weight;                                          // 同上（+0x18）
            public float chances;                                         // 同上（+0x1C，原版带 `[Range(0,1)]`）

            public float Weight { get { return weight; } }

            /// <summary>判据 = `…ParticleSpawnDefinition__TrySpawnParticles.c`：
            /// `if (Random.value > chances) return; spawner.SpawnParticle();`</summary>
            public void TrySpawnParticles()
            {
                if (UnityEngine.Random.value > chances) return;
                if (particleSystemAreaSpawner == null) return;
                particleSystemAreaSpawner.SpawnParticle();
            }

            /// <summary>判据 = `…ParticleSpawnDefinition__IsAnyPoolItemActive.c` = 转调它那颗 spawner 的同名方法。</summary>
            public bool IsAnyPoolItemActive()
            {
                return particleSystemAreaSpawner != null && particleSystemAreaSpawner.IsAnyPoolItemActive();
            }
        }

        public ParticleSpawnDefinition[] particleSystemAreaSpawners;  // 原版 `[SerializeField] private`
        public float spawnRate = 1f;      // ctor 默认 1.0f（`…Controller__.ctor.c` 的 0x28；原版 private）
        public bool startOnEnable = true; // ctor 默认 1（0x2C；原版 private）

        bool _running;
        float _t;

        /// <summary>判据 = `…__OnEnable.c`：`if (startOnEnable) { StopAllCoroutines(); StartCoroutine(SpawnParticles()); }`</summary>
        void OnEnable()
        {
            if (!startOnEnable) return;
            StopAllCoroutines();
            _running = true;
            _t = 0f;
        }

        /// <summary>判据 = `…__Toggle.c` + 反汇编（RVA 0x6742A0）：`StopAllCoroutines(); if (option) StartCoroutine(...)`
        /// —— `false` 那一支**只停**（不启动）。</summary>
        public void Toggle(bool option)
        {
            StopAllCoroutines();
            _running = option;
            _t = 0f;
        }

        /// <summary>🔴 **原版这个方法很怪，照抄不美化**（`.c` 与反汇编 RVA 0x6754F0 逐条对上）：
        ///   `definitions.Length < 1` ⇒ `false`；否则它**调一次 `Pool.CountInactive` 却把结果丢掉**、
        ///   **无条件 `return true`** ⇒ 可观察行为 = **「有定义就 true」**（与池里有没有粒子无关）。
        ///   那次读取的副作用是 `get_Pool()`（池会被建出来）。</summary>
        public bool IsAnyParticlePoolActive()
        {
            if (particleSystemAreaSpawners == null || particleSystemAreaSpawners.Length < 1) return false;
            var def = particleSystemAreaSpawners[0];
            if (def == null || def.particleSystemAreaSpawner == null) return false;   // 原版这里抛 NRE
            int _ = def.particleSystemAreaSpawner.Pool.CountInactive;
            return true;
        }

        /// <summary>原版那条循环的**一步**（判据 = `<SpawnParticles>d__6.MoveNext.c`：
        /// `while (true) { SelectRandomWeightedItem().TrySpawnParticles(); yield return new WaitForSeconds(1f/spawnRate); }`）。
        /// 与 spawner 那条同理：**一次 `Advance` 最多一颗**（原版一帧只恢复一次协程）。</summary>
        public void Advance(float dt)
        {
            if (!_running || spawnRate <= 0f) return;
            float step = 1f / spawnRate;
            _t += dt;
            if (_t < step) return;
            _t -= step;
            if (_t > step) _t = 0f;
            SpawnParticle();
        }

        /// <summary>判据 = `…__SpawnParticle.c`：`SelectRandomWeightedItem().TrySpawnParticles()`（选完还要再过一次 chances）。</summary>
        void SpawnParticle()
        {
            var def = SelectRandomWeightedItem();
            if (def != null) def.TrySpawnParticles();
        }

        /// <summary>判据 = `…__SelectRandomWeightedItem.c`：按 `Weight` 求总和 → `Random.value * 总和` 逐个减 →
        /// 第一个减到 `<= 0` 的就是它；**一个都没命中时返回第 0 个**（原版如此）。</summary>
        ParticleSpawnDefinition SelectRandomWeightedItem()
        {
            if (particleSystemAreaSpawners == null || particleSystemAreaSpawners.Length == 0) return null;
            float total = 0f;
            for (int i = 0; i < particleSystemAreaSpawners.Length; i++)
                if (particleSystemAreaSpawners[i] != null) total += particleSystemAreaSpawners[i].Weight;
            float r = UnityEngine.Random.value * total;
            for (int i = 0; i < particleSystemAreaSpawners.Length; i++)
            {
                if (particleSystemAreaSpawners[i] == null) continue;
                r -= particleSystemAreaSpawners[i].Weight;
                if (r <= 0f) return particleSystemAreaSpawners[i];
            }
            return particleSystemAreaSpawners[0];
        }

        /// <summary>原版是 NaughtyAttributes 的 `[Button]`（**只在编辑器里手点**，运行时不调）。
        /// 🔴 原版那一句 `GetComponentsInChildren<ParticleSystemAreaSpawnerController.ParticleSpawnDefinition>()`
        ///   **在 C# 里编不过** —— `GetComponentsInChildren<T>` 要求 `T : Component`，而 `ParticleSpawnDefinition`
        ///   是个普通 `[Serializable]` 类（`dump.cs:49909`）⇒ 这里按**意图**落地：把子树上那批
        ///   `ParticleSystemAreaSpawner` 各包成一条定义、引用接上（权重/chances 保持字段默认值）。
        ///   **这一条不是逐句移植**，如实记着。</summary>
        void FetchAllParticleSpawners()
        {
            var found = GetComponentsInChildren<ParticleSystemAreaSpawner>(true);
            var list = new ParticleSpawnDefinition[found.Length];
            for (int i = 0; i < found.Length; i++)
                list[i] = new ParticleSpawnDefinition { particleSystemAreaSpawner = found[i] };
            particleSystemAreaSpawners = list;
            Debug.LogWarning($"[EnvSpawner] `{name}`：`FetchAllParticleSpawners` 是**近似移植**"
                           + $"（原版那句泛型调用在 C# 里编不过，见方法注释）—— 收进 {found.Length} 条");
        }
    }

    // ==========================================================================================
    // ④ 🆕 2026-10-07 波9离线（A136）：原版另 **4 个** `IScenarioEnvironmentBlendeable` 实现类
    //    （`FlareScenarioToggler` 6 · `ScenarioAnimationBlend` 2 · `ScenarioGenericMaterialBlend` 1 ·
    //     `TauCannonAnimationStopper` 2 = **11 个实例、全在场景侧**）。
    //
    // 第一权威 = `d:/2/tools/decomp_full/` 的**方法体**（逐句读过，件名写在各自的方法注释里）＋**反汇编**
    //   （`.c` 里丢参数/丢常量的地方按 x64 反汇编补；`.text` 节 文件偏移 = RVA − 0x1600）；
    //   类型形状（字段名 / 偏移 / 默认值 / 枚举值）= `d:/2/tools/il2cpp_out/dump.cs`：
    //     `FlareScenarioToggler` 741 · `ScenarioAnimationBlend` 743 · `BlendFilterOptions` 744 ·
    //     `ScenarioBlendOptions` 745 · `ScenarioGenericMaterialBlend(.RendererMaterialBlender)` 751/752 ·
    //     `TauCannonAnimationStopper` 759 · `ScenarioEnvironmentConditionSO.AnimationsToChange` 747。
    // ==========================================================================================

    /// <summary>`FlareScenarioToggler`（原版 **6 个实例、全在场景侧**，每个都挂在 `Sun flare` 上）。
    /// 判据 = `FlareScenarioToggler__{DoScenarioBlend,Initialize,OnValidate}.c` ＋ 两个闭包
    /// `…__<DoScenarioBlend>b__1_{0,1}.c` ＋ 反汇编 `RVA 0x625730`（`DOTween.To` 的两个实参：
    /// `targetValue` 来自 `options+0x20`、`duration` 来自 `options+0x10`）：
    ///   · `DoScenarioBlend(o)`：`DOTween.To(() => lensFlare.intensity, x => lensFlare.intensity = x,
    ///     o.ScenarioBlendTargetValue, o.ScenarioBlendTime)` —— **只对 `intensity` 补间**，
    ///     返回的 tween 被丢掉 ⇒ **不回调 `OnComplete`、不理 `onComplete`**。
    ///   · `Initialize()` ⇒ `ScenarioEnvironmentConditionsManager.Register(this)`（= 场景侧那批；我们由执行器建一次）。
    ///   · `OnValidate()` ⇒ 没填就 `GetComponent<LensFlareComponentSRP>()`。
    /// ⚠️ **两处有意偏离**（都写在这儿，别当成原版）：
    ///   ① 原版用 **DOTween**（`DOTween.To`，**没调 `SetEase`** ⇒ 用的是 DOTween 全局默认缓动
    ///      `Ease.OutQuad`）；本仓口径是**手推 `Advance(dt)` + 线性**（批处理下没有帧循环 ⇒ 必须能手动推，
    ///      见 `EnvironmentApplier` 头注释 ①）。**缓动曲线因此与原版不同**（线性 vs OutQuad）。
    ///   ② `lensFlare` 放成 `public`（与本文件其它类同一套：运行时由旁挂灌值、自检读回来核）。</summary>
    public class FlareScenarioToggler : ScenarioBlendable
    {
        public LensFlareComponentSRP lensFlare;

        /// <summary>**不回调**（判据：原版那句 `DOTween.To` 的返回值被丢掉，方法体里没有任何 OnComplete）。</summary>
        public override bool NotifiesComplete { get { return false; } }

        float _from, _to;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (lensFlare == null)
            {
                Debug.LogError($"[EnvBlend] `FlareScenarioToggler`({name}) 的 `lensFlare` 是空的 —— "
                             + "原版这一句会抛 NRE；我们出声并停手（不许静默）");
                return;
            }
            _from = lensFlare.intensity;
            _to = o.targetValue;
            _opts = o;
            float dur = Mathf.Max(0f, o.duration);
            if (dur <= 0f) { lensFlare.intensity = _to; _busy = false; return; }   // 0 时长 ⇒ 立刻到位
            _left = dur;
            _busy = true;
        }

        public override void Advance(float dt)
        {
            if (!_busy) return;
            _left -= dt;
            float t = Mathf.Clamp01(1f - _left / Mathf.Max(1e-6f, _opts.duration));
            if (lensFlare != null) lensFlare.intensity = Mathf.Lerp(_from, _to, t);
            if (_left <= 0f) { if (lensFlare != null) lensFlare.intensity = _to; _busy = false; }
        }
    }

    /// <summary>`ScenarioAnimationBlend`（原版 **2 个实例**，都在 darkangels 场：`Directional Light` 与
    /// `Battle Arena Dark Angels baked`）。判据 = `ScenarioAnimationBlend__{DoScenarioBlend,AddAnimation,Initialize}.c`：
    ///   · **只在 `direction == false`** 那一程干活（`if (options.ScenarioBlendDirection) return;`）
    ///   · 取**当前环境 SO** 的 `animationsToChange[]`（原版 `Manager.CurrentEnvironment`(+0x28) → SO(+0x60)），
    ///     逐条 `if (atc.filterCode == this.filterCode)` ⇒ `clip = atc.clip.Load()` ⇒
    ///     `myAnimation.AddClip(clip, clip.name)`；`autoPlayWhenChange` 时再 `myAnimation.CrossFade(name, blendTime)`
    ///   · 命中 **> 1 条** ⇒ `CustomDebug.LogWarning`（原版那句是 `Concat(...)` 拼出来的告警）
    ///   · `AddAnimation(clip, alsoPlay = true)` 是公开方法，走的是同一条路
    /// 🔴 **这是【另一套 filter】，别与 `ScenarioBlendOptions.FilterOptions` 混**：这套比的是
    ///   **组件自己的 `filterCode`(+0x28)** ↔ **SO 里 `animationsToChange[]` 每条的 `filterCode`**。
    /// 🔴 **两处数据缺口（如实记着，不是「已做」；两处都会出声）**：
    ///   ① ✅ **2026-10-07（A192）已补上**：那两个 clip（`AssetReferenceTyped<AnimationClip>`，**按 GUID 取**）
    ///      现在由 `工具/extract_missing_shaders.py --prefabs` 收进 `wf_prefabs_extra.bundle`、
    ///      并按 **assetGUID** 登记容器别名（原版源包的容器键就是 GUID）⇒
    ///      `ScenarioBlendableFactory.AnimationClipByGuid` 按 GUID 取原件；取不到仍然出声。
    ///      ✅ **2026-10-11（A201）那一跳已经有一条真跑过的自检**（`Editor/BattleScene.cs` 的 A201 一节：
    ///      GUID → `LoadAsset<AnimationClip>` → `Animation.AddClip` 走一遍）。
    ///      ⛔ **仍然有一条不会播，而那是照抄原版数据的结果，别去「修」**：
    ///        · `Directional Light` 那颗的 `filterCode` = `LightAnimationOrbital`，SO 那条写的是
    ///          `LightAnimationOrbit`（差一个 `al`）⇒ **原版自己这一对永远配不上**。
    ///   ② ✅ **2026-10-11（A191）已补**（这一格原来写「我们 13 件 arena prefab 里一个 `Animation` 组件都没有」+
    ///      「`Battle Arena Dark Angels baked` 那个分组节点我们工程里没有」—— **两句都已不成立**）：
    ///      那个分组节点已照原版建成**真分组节点**（连同它的整棵原子树，`ArenaBuilder.ApplyGroupNodes`），
    ///      它和 `Directional Light` 两颗的 `Animation` 组件**都按原版补上了**（旁挂 `animation` 标记）
    ///      ⇒ `myAnimation` **不再是 null**；建场那一步零条没对上（自检盯着）。
    /// ⚠️ **顺带一条实测**：`Directional Light` 那颗的 `filterCode = "LightAnimationOrbital"`，而 SO
    ///   `Dark Angels Orbiting` 里那条是 `"LightAnimationOrbit"`（**差一个 `al`**）⇒ **原版自己这一对永远匹配不上**。
    ///   我们**照抄**（不做模糊匹配）—— 不替原版「修正」数据。</summary>
    public class ScenarioAnimationBlend : ScenarioBlendable
    {
        public string filterCode;
        public Animation myAnimation;
        public float blendTime = 0.3f;          // 原版 ctor 默认 0.3（`…__.ctor.c` 里的 0x3e99999a）
        public bool autoPlayWhenChange = true;  // 原版 ctor 默认 true

        /// <summary>GUID → `AnimationClip`。**原版是 `AssetReferenceTyped<AnimationClip>.Load()`**；
        /// 由工厂注入 `ScenarioBlendableFactory.AnimationClipByGuid` —— 从重打的
        /// `wf_prefabs_extra.bundle` 里**按 assetGUID** 取原件；**拿不到仍返回 null 并出声**。</summary>
        public Func<string, AnimationClip> clipLoader;

        bool _warnedNoAnim, _warnedNoClip;

        /// <summary>**不回调**（判据：原版整个方法体里没有 `OnComplete`）。</summary>
        public override bool NotifiesComplete { get { return false; } }

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (o.direction) return;                                  // 原版第一句：只在「撤环境」那一程干活
            if (string.IsNullOrEmpty(filterCode)) return;             // 原版：filterCode 空 ⇒ 整段不进
            var so = (manager != null) ? manager.CurrentItem : null;
            var list = (so != null) ? so.animationsToChange : null;
            if (list == null || list.Length == 0) return;             // SO 里没有这条 ⇒ 原版也就没事可做
            if (myAnimation == null)
            {
                if (!_warnedNoAnim)
                {
                    _warnedNoAnim = true;
                    Debug.LogError($"[EnvBlend] `ScenarioAnimationBlend`({name}) 的 `myAnimation` 是空的 —— "
                                 + "我们 13 件 arena prefab 里没有 `Animation` 组件（旁挂 `_missingTargets` 里记着）"
                                 + " ⇒ 这一条不生效（出声，不静默）");
                }
                return;
            }
            int n = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var atc = list[i];
                if (atc == null || atc.filterCode != filterCode) continue;
                var clip = (clipLoader != null) ? clipLoader(atc.clip) : null;
                if (clip == null)
                {
                    if (!_warnedNoClip)
                    {
                        _warnedNoClip = true;
                        Debug.LogError($"[EnvBlend] `ScenarioAnimationBlend`({name}) 命中了 SO `{so.so}` 的一条 "
                                     + $"`animationsToChange`（filterCode=`{atc.filterCode}`），但它要的 clip "
                                     + $"（assetGUID `{atc.clip}`）**取不到** ⇒ 这一条不生效（出声，不静默；"
                                     + "取不到的原因见上面 `AnimationClipByGuid` 那条报错）");
                    }
                    continue;
                }
                AddAnimation(clip, autoPlayWhenChange);
                n++;
            }
            if (n > 1)
                Debug.LogWarning($"[EnvBlend] `ScenarioAnimationBlend`({name})：SO `{so.so}` 里有 {n} 条 "
                               + $"`animationsToChange` 都命中 `filterCode={filterCode}` —— 原版也告警（同名 clip 会被重复 AddClip）");
        }

        /// <summary>判据 = `…__AddAnimation.c`：`AddClip(clip, clip.name)`；`alsoPlay` 时再 `CrossFade(clip.name, blendTime)`。</summary>
        public void AddAnimation(AnimationClip clipToAdd, bool alsoPlay = true)
        {
            if (clipToAdd == null || myAnimation == null) return;
            myAnimation.AddClip(clipToAdd, clipToAdd.name);
            if (alsoPlay) myAnimation.CrossFade(clipToAdd.name, blendTime);
        }
    }

    /// <summary>`TauCannonAnimationStopper`（原版 **2 个实例**，都在 tauviorla 场，挂在
    /// `…/Railgun Turret 1(2)/Railgun Turret Base.00N/Cylinder.00N` 这一族上）。
    /// 判据 = `TauCannonAnimationStopper__{DoScenarioBlend,Toggle,Initialize}.c` ＋ 反汇编
    ///   （`DoScenarioBlend` RVA 0x631B20 · `Toggle` RVA 0x631C00；每个调用目标都拿 `script.json` 的
    ///    `ScriptMethod` 核过：`Behaviour.set_enabled` / `ParticleSystem.Play` / `ParticleSystem.Stop` /
    ///    `Quaternion.Internal_FromEulerRad` / `DOLocalRotateQuaternion` / `SetEase`）：
    ///   `DoScenarioBlend(o)`：`if (o == null) 抛 NRE; if (o.ScenarioBlendDirection) return; Toggle(false);`
    ///   `Toggle(option)`：
    ///     ① `foreach (c in lookAtConstrains) c.enabled = option`
    ///     ② `foreach (ps in particleSystems) { option ? ps.Play(false) : ps.Stop(false); }`
    ///        （⚠️ 反汇编里两次调用的 `withChildren` 实参**都是 0** = `Play(false)`/`Stop(false)`；
    ///         `.c` 把这两个实参印丢了 —— 不是我们挑的）
    ///     ③ `if (animFXController != null) { animFXController.enabled = option;
    ///          if (!option) { animationComponent.enabled = false;
    ///            cannon.DOLocalRotateQuaternion(Quaternion.Euler(finalRotation * Deg2Rad), 0.8f)
    ///                 .SetEase(Ease.OutBounce); } }` —— **这一整支被 `animFXController != null` 挡着**
    ///        （常量：`Deg2Rad` = `DAT_1834b2dc0` = 0.0174532924；时长 = `DAT_1834b2fa0` = **0.8**；
    ///         缓动 = `SetEase(0x1e)`，`DG.Tweening.Ease`（TypeDefIndex 19111）第 30 项 = **OutBounce**）
    /// 🔴 **缺的是哪几件（2026-10-11 更新）**：原版那三类资产里，
    ///   ① ✅ **`LookAtConstrainWIP` / `AnimFXController` 两个类** —— **2026-10-11（A196）已经移植**
    ///      （`Battle/LookAtConstrainWIP.cs` · `Battle/AnimFXController.cs`，
    ///       类名/字段/方法体逐句照 `d:/2/tools/decomp_full/`）；
    ///   ② ✅ **宿主对象（A191）—— 2026-10-11 照原版建出来了**（原来这里写「仍然没有」）：
    ///      `Railgun Turret 1/2`（带 `Animation` 的父节点）· `Railgun turret`（`AnimFXController` 的宿主）·
    ///      `Railgun Turret N Target`（`LookAtConstrainWIP.target` 指的对象）四条宿主 + 两条 Target。
    ///      建场 = `ArenaBuilder.ApplyGroupNodes`（旁挂 `arenas/battlearenatauviorla/battlearenatauviorla_groups.json`，
    ///      由 `工具/gen_arena_groups.py` 直读原版场景包产出；那两件 Target **不在 `_missingTargets` 里** —— 那张表
    ///      只查 blendable 自己的 target，判据 → `资料/普查产出_1011/W9_A196_A210_A211.md` §五·4）。
    ///   ⇒ `lookAtConstrains` / `animFXController` / `animationComponent` 三处**现在解析得到**了；
    ///      真解析不到时工厂仍然「出声、**不建**」（见 `MakeLookAtConstrains` / `MakeAnimFx`）。
    ///   ⚠️ **但原版第 ③ 支仍然不执行** —— 那**不是**因为宿主缺，而是**原版自己**就把它挡在
    ///      `animFXController != null` 后面、而它自己那些实例的 `animFXController` 字段**有值**
    ///      （实测那两个 `TauCannonAnimationStopper` 各 1 个、**2/2 都指到了 `Railgun turret` 上那颗**
    ///        = MB 4767 / 5247）⇒ 原版**是会回位的**。
    ///      我们这条是否真的回位**要等一次 Unity 实跑**（`Toggle(false)` 里那条出声会告诉你走没走到）。
    /// ⚠️ 有意偏离：原版 ③ 用 DOTween；本仓口径是手推 `Advance(dt)`（缓动仍照 `SetEase(0x1e)` = OutBounce 实现）。</summary>
    public class TauCannonAnimationStopper : ScenarioBlendable
    {
        /// <summary>原版 `LookAtConstrainWIP[]`。🆕 2026-10-11（A196）：**类型从 `Behaviour[]` 换成真类**
        /// —— 由 `MakeLookAtConstrains` 就地建（宿主对象在不在见类注释 ②）。空数组 = 一条都没建出来。</summary>
        public LookAtConstrainWIP[] lookAtConstrains;
        public Animation animationComponent;
        public Transform cannon;
        public Vector3 finalRotation;
        public ParticleSystem[] particleSystems;
        /// <summary>原版 `AnimFXController`。🆕 2026-10-11（A196）：**类型从 `Behaviour` 换成真类**
        /// —— 由 `MakeAnimFx` 就地建。null = 宿主对象不在（今天就是，见类注释 ②）。</summary>
        public AnimFXController animFXController;

        /// <summary>原版那条 `DOLocalRotateQuaternion(..., 0.8f)` 的时长（`DAT_1834b2fa0` = 0.800000012）。</summary>
        public const float CannonReturnTime = 0.8f;

        /// <summary>**不回调**（判据：原版 `DoScenarioBlend` 只有 `Toggle(false)` 一句）。</summary>
        public override bool NotifiesComplete { get { return false; } }

        Quaternion _rotFrom, _rotTo;
        float _rotLeft;
        bool _warnedLookAt, _warnedAnimFx;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (!o.direction) Toggle(false);
        }

        /// <summary>判据见类注释（① ② ③ 三步；③ 整支被 `animFXController != null` 挡着）。</summary>
        public void Toggle(bool option)
        {
            if (lookAtConstrains != null)
                for (int i = 0; i < lookAtConstrains.Length; i++)
                    if (lookAtConstrains[i] != null) lookAtConstrains[i].enabled = option;

            if (particleSystems != null)
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    var ps = particleSystems[i];
                    if (ps == null) continue;
                    if (option) ps.Play(false); else ps.Stop(false);     // withChildren = false（反汇编 edx=0）
                }

            if (animFXController != null)
            {
                animFXController.enabled = option;
                if (!option)
                {
                    if (animationComponent == null)
                    {
                        Debug.LogError($"[EnvBlend] `TauCannonAnimationStopper`({name})：`animationComponent` 是空的 —— "
                                     + "原版这一句会抛 NRE；我们出声并停手（不许静默）");
                        return;
                    }
                    animationComponent.enabled = false;
                    BeginCannonReturn();
                }
            }
            else if (!option && !_warnedAnimFx)
            {
                // 忠实照抄 ⇒ 没有 `animFXController` 就不回位。**出声**说清是「**宿主对象**没有」而不是「我们没做」
                //（2026-10-11 A196 之前这句说的是「那个类我们工程里没有」—— 类早就不缺了；
                //  🆕 2026-10-11 A191 之后**宿主也补齐了** ⇒ **这一条正常情况下不该再出现**；
                //  真出现说明那件 arena prefab 还是旧的：跑 `-executeMethod ArenaBuilder.BuildArenaPrefabs`。）
                _warnedAnimFx = true;
                Debug.LogWarning($"[EnvBlend] `TauCannonAnimationStopper`({name})：`animFXController` 解析不到"
                               + "（它的宿主 `Railgun turret` 应当已由 A191 建场建出 —— 见 `ArenaBuilder.ApplyGroupNodes`；"
                               + " 若仍出现 ⇒ 那件 arena prefab 没重建，跑 `ArenaBuilder.BuildArenaPrefabs`）"
                               + " ⇒ 原版第 ③ 支（关 `animationComponent` + 炮塔转回 `finalRotation`）不执行"
                               + "（出声，不静默）");
            }
            if (!option && (lookAtConstrains == null || lookAtConstrains.Length == 0) && !_warnedLookAt)
            {
                _warnedLookAt = true;
                Debug.LogWarning($"[EnvBlend] `TauCannonAnimationStopper`({name})：`lookAtConstrains` 一条都没建出来"
                               + "（`LookAtConstrainWIP` 的宿主或它自己的 `target` 应当已由 A191 建场建出 —— 见 "
                               + "`ArenaBuilder.ApplyGroupNodes`；若仍出现 ⇒ 那件 arena prefab 没重建，"
                               + "跑 `ArenaBuilder.BuildArenaPrefabs`）"
                               + " ⇒ 原版第 ① 步不生效（出声，不静默）");
            }
        }

        /// <summary>炮塔转回 `finalRotation`（原版 = `DOLocalRotateQuaternion(Euler(finalRotation * Deg2Rad), 0.8s)`
        /// `.SetEase(Ease.OutBounce)`）。我们手推 —— 缓动照 `SetEase(0x1e)` = **OutBounce**（Penner 那条）。</summary>
        void BeginCannonReturn()
        {
            if (cannon == null)
            {
                Debug.LogError($"[EnvBlend] `TauCannonAnimationStopper`({name})：`cannon` 是空的 —— 炮塔转不回去（出声）");
                return;
            }
            _rotFrom = cannon.localRotation;
            _rotTo = Quaternion.Euler(finalRotation * Mathf.Deg2Rad);   // ⚠️ `Internal_FromEulerRad` 收的是**弧度**
            _rotLeft = CannonReturnTime;
        }

        public override void Advance(float dt)
        {
            if (_rotLeft <= 0f || cannon == null) return;
            _rotLeft -= dt;
            float t = Mathf.Clamp01(1f - Mathf.Max(0f, _rotLeft) / CannonReturnTime);
            cannon.localRotation = Quaternion.Slerp(_rotFrom, _rotTo, OutBounce(t));
            if (_rotLeft <= 0f) cannon.localRotation = _rotTo;
        }

        /// <summary>DOTween `Ease.OutBounce`（= `DG.Tweening.Ease` 第 30 项，反汇编里 `SetEase(0x1e)`）。
        /// 算式 = Penner 的经典 OutBounce（DOTween 用的就是它），`t ∈ [0,1]`、`b=0` / `c=1`。
        /// ⚠️ **2026-10-12（A420）订正**：原来这里写「这一支今天走不到（`animFXController` 我们工程里没有）
        /// ⇒ 真验它得等那件资产补齐」—— **那句已经过期**（本文件 `TauCannonAnimationStopper` 的类注释
        /// ③ 就写着「`animFXController` 现在解析得到了、2/2 实例都指到了 `Railgun turret` 上那颗」，
        /// 两处打架）。⇒ 这一支**跑得到**；`OutBounce` 自己**没有直接断言**（自检断到的是那一层的
        /// `sounds`，见 `Editor/BattleScene.cs:1715`）⇒ 留给「画面/手感」那一档（真 Play 清单）。</summary>
        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) { return n1 * t * t; }
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1; return n1 * t * t + 0.984375f;
        }
    }

    /// <summary>`ScenarioGenericMaterialBlend`（原版 **1 个实例**：GSC 场 `…/Battle Arena Genestealers Baked/Floor`）。
    /// —— 也是 `ScenarioBlendOptions.FilterOptions` 的**唯一消费方**（A135）。
    /// 判据 = `ScenarioGenericMaterialBlend__{DoScenarioBlend,Start,Update,ToggleRenderers,SetTargetFade}.c`
    ///   ＋ `…RendererMaterialBlender__{Init,DoBlend,ToggleRenderer}.c` ＋ **反汇编 RVA 0x62FB70（DoScenarioBlend）
    ///   与 0x62FF50（Update）**（把 `.c` 印丢的分支逐条定了）：
    /// ```
    /// DoScenarioBlend(o):
    ///   if (!IsNullOrEmpty(this.filterCode)) {                 // +0x30
    ///       if (o == null || o.FilterOptions == null) 抛 NRE;
    ///       if (this.filterCode != o.FilterOptions.FilterCode) return;     // ★ 不匹配 ⇒ 整条跳过
    ///   }
    ///   current = new Options(o) { direction=o.direction, targetValue=o.direction?1:0, duration, onComplete, filterOptions }
    ///   if (current.FilterOptions == null) 抛 NRE;             // ★ 与 filterCode 空不空**无关**（反汇编 0x62E695）
    ///   if (!IsNullOrEmpty(current.FilterOptions.FilterCode)) {
    ///       current.direction   = current.FilterOptions.isEnabled;          // ★ 命中后【覆盖】
    ///       current.targetValue = current.FilterOptions.isEnabled ? 1 : 0;
    ///   }
    ///   foreach (r in renderersBlend) r.renderer.enabled = true;
    ///   if (fadeOnEnable && current.direction) {
    ///       foreach (r in renderersBlend) r.Init(0f);
    ///       doFade = !doFade;  blendTime = currentBlendTime = current.duration;  targetFade = 1f;  return;
    ///   }
    ///   blendTime = currentBlendTime = o.duration;  doFade = !doFade;  targetFade = current.targetValue;
    /// Update():                                                 // 我们 = Advance(dt)
    ///   if (!doFade) return;
    ///   foreach (r in renderersBlend) { if (r.customMaterial == null) 跳过; if (!r.isInitialized) r.Init(0f);
    ///       foreach (p in r.propertiesToBlend) { cur = r.renderer.material.GetFloat(p);
    ///           r.renderer.material.SetFloat(p, Mathf.MoveTowards(cur, targetFade, Time.deltaTime / blendTime)); } }
    ///   if (currentBlendTime <= 0) { OnComplete?.Invoke(); currentOptions = null; doFade = false; }
    ///   currentBlendTime -= Time.deltaTime;
    /// ```
    /// 🔴 **`doFade = !doFade` 是原版真的这么写**（反汇编 `0x62E794` / `0x62E7F3` 两处都是
    ///   `sete al; mov [this+0x40], al`）—— **照抄，不「修正」**。
    /// ⚠️ **两处落地偏离（都在这儿写清）**：
    ///   ① 原版那三个 `RendererMaterialBlender.Init(...)` 的实参在**三处调用点全是 `0f`**
    ///      （反汇编：`DoBlend` `0x62C877 xorps xmm1,xmm1` · `Update` `0x62EA37 movaps xmm1,xmm8`（=0）·
    ///       `DoScenarioBlend` `0x62E784`）—— `dump.cs` 记的默认值 `1` **一次都没被用到**，我们照 `0f` 落地。
    ///   ② 原版 `RendererMaterialBlender.Init` 会 `renderer.material = customMaterial`（换成那个 Material 资产）。
    ///      **我们工程里没有那个 Material**（原版叫 `Battle Arena Genestealers Water Floor`，是 bundle 里的
    ///      Material 资产；我们的 arena 材质是 `ArenaBuilder` 按清单 `props` **现建**的、
    ///      `_Blend` 本来就在上面）⇒ **不换材质**，直接改那件渲染器现有材质上的属性；
    ///      属性不存在就 `LogError` 出声（**不静默**）。这是一处**必要**偏离（没资产可换），如实记着。</summary>
    public class ScenarioGenericMaterialBlend : ScenarioBlendable
    {
        /// <summary>原版嵌套类 `ScenarioGenericMaterialBlend.RendererMaterialBlender`（TypeDefIndex 751）：
        /// `renderer`(+0x10) · `customMaterial`(+0x18) · `propertiesToBlend`(+0x20) · `isInitialized`(+0x28 运行时)。</summary>
        [Serializable]
        public class RendererMaterialBlender
        {
            public Renderer renderer;
            /// <summary>原版那条 Material 引用。⚠️ 我们工程里没有这个资产 ⇒ 恒 null（见类注释偏离 ②）。</summary>
            public Material customMaterial;
            public string[] propertiesToBlend;
            /// <summary>🆕 原版那个 Material 的**资产名**（旁挂 `Target.customMaterial` 给的值；只留档 / 出声用）。</summary>
            public string originalMaterialName;

            bool _isInitialized;
            bool _warned;

            /// <summary>判据 = `…RendererMaterialBlender__Init.c` + 反汇编 RVA 0x62C950：
            /// `if (customMaterial != null) { renderer.material = customMaterial; foreach (p) customMaterial.SetFloat(p, startValue); }`
            /// ⇒ **`customMaterial == null` 时它什么都不做、只把 `isInitialized` 置真**（原版如此）。
            /// 我们 `customMaterial` 恒 null ⇒ 走「什么都不做」那一支是**忠实**的，但整套就没效果了
            /// ⇒ **改在现有材质上写**（见类注释偏离 ②），属性不存在时出声。</summary>
            public void Init(float startValue)
            {
                if (customMaterial != null && renderer != null)
                {
                    renderer.material = customMaterial;
                    for (int i = 0; i < (propertiesToBlend != null ? propertiesToBlend.Length : 0); i++)
                        customMaterial.SetFloat(propertiesToBlend[i], startValue);
                }
                _isInitialized = true;
            }

            /// <summary>把 `propertiesToBlend` 里每个属性往 `target` 推（`instant` 时直接写）。</summary>
            public void DoBlend(float target, float blendSpeed, bool instant = false)
            {
                if (!_isInitialized) Init(0f);
                if (renderer == null || propertiesToBlend == null) return;
                var m = renderer.material;
                if (m == null) return;
                for (int i = 0; i < propertiesToBlend.Length; i++)
                {
                    string p = propertiesToBlend[i];
                    if (!m.HasProperty(p))
                    {
                        if (!_warned)
                        {
                            _warned = true;
                            Debug.LogError($"[EnvBlend] 渲染器 `{renderer.name}` 的材质 `{m.name}` 上没有属性 `{p}`"
                                         + $"（原版那件 Material 叫 `{originalMaterialName}`）⇒ 这一条补间写不进去"
                                         + "（出声，不静默）");
                        }
                        continue;
                    }
                    float nv = instant ? target : Mathf.MoveTowards(m.GetFloat(p), target, blendSpeed);
                    m.SetFloat(p, nv);
                }
            }

            /// <summary>判据 = `…RendererMaterialBlender__ToggleRenderer.c`：`renderer.enabled = option`。</summary>
            public void ToggleRenderer(bool option) { if (renderer != null) renderer.enabled = option; }
        }

        public RendererMaterialBlender[] renderersBlend;   // 原版 `[SerializeField] private`
        public bool fadeOnEnable;
        public bool isInDefaultScenario;
        public string filterCode;

        /// <summary>原版 `currentScenarioBlendOptions`(+0x38) —— 这里用「有没有」代替 null 引用。</summary>
        ScenarioBlendOptions _cur;
        bool _hasCur;
        /// <summary>= 原版 `doFade`(+0x40)。⚠️ 原版每次 `DoScenarioBlend` 都写 `doFade = !doFade`（**照抄**）。</summary>
        bool _doFade;
        float _targetFade;      // +0x44
        float _blendTime;       // +0x48（`_left` = 基类的 = 原版 +0x4C `currentBlendTime`）

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            // ---- ① filter（原版 +0x30 非空时：FilterOptions 必须非 null 且 FilterCode 相等，否则整条跳过）----
            if (!string.IsNullOrEmpty(filterCode))
            {
                if (o.filterOptions == null)
                {
                    // 原版这里**抛 NullReferenceException**（`.c` 的 `LAB_18062fe12` = NRE；反汇编 0x62E5CD）。
                    // 抛异常会把调用者整条链打断 ⇒ 我们**出声并跳过这一条**（等价于「不匹配」那一支）。
                    Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name}) 的 `filterCode` 是 "
                                 + $"`{filterCode}`，但这一程的 `options.filterOptions` 是 null —— 原版这里会抛 NRE；"
                                 + "我们出声并跳过这一条（数据缺 `filterCode`/`filterEnabled` ⇒ 查 `Resources/EnvironmentConditions.json`）");
                    return;
                }
                if (filterCode != o.filterOptions.filterCode) return;   // ★ 不匹配 ⇒ 整条跳过
            }
            // 🔴 **第二个 null 检查**：原版对 `currentScenarioBlendOptions.FilterOptions == null` 也会**抛 NRE**
            //    （反汇编 `0x62E692 test rcx,rcx` / `0x62E695 je 0x62e812` —— 与 `filterCode` 空不空**无关**）
            //    ⇒ 我们同样「出声 + 不生效」。⚠️ 别把它写成「当空串继续跑」——那会让这一条在
            //    `filterCode` 为空时**比原版多做一整套**（开渲染器 + 起补间）。
            if (o.filterOptions == null)
            {
                Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name})：这一程的 `options.filterOptions` 是 null —— "
                             + "原版这里会抛 NRE；我们出声并跳过这一条（不出声就会变成「多做一整套」，见代码注释）");
                return;
            }

            // ---- ② 记下这一程的 options（原版 new 一份、逐字段拷）----
            _cur = o;
            _hasCur = true;

            // ---- ③ FilterCode 非空 ⇒ 用 isEnabled 覆盖 direction / targetValue ★ ----
            if (!string.IsNullOrEmpty(o.filterOptions.filterCode))
            {
                _cur.direction = o.filterOptions.isEnabled;
                _cur.targetValue = o.filterOptions.isEnabled ? 1f : 0f;
            }

            // ---- ④ 全部渲染器先打开 ----
            if (renderersBlend != null)
                for (int i = 0; i < renderersBlend.Length; i++)
                {
                    var r = renderersBlend[i];
                    if (r == null || r.renderer == null)
                    {
                        Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name}) 的第 {i} 条 "
                                     + "`renderersBlend` 缺 `renderer` —— 原版这里会抛 NRE；我们出声并跳过这一条");
                        continue;
                    }
                    r.renderer.enabled = true;
                }

            // ---- ⑤ `fadeOnEnable` 且方向为真 ⇒ 先 Init(0) 再把 targetFade 顶到 1 ----
            if (fadeOnEnable && _cur.direction)
            {
                if (renderersBlend != null)
                    for (int i = 0; i < renderersBlend.Length; i++)
                        if (renderersBlend[i] != null) renderersBlend[i].Init(0f);
                _doFade = !_doFade;                                   // 原版真的这么写（反汇编 0x62E794）
                _blendTime = _cur.duration; _left = _cur.duration;
                _targetFade = 1f;
                return;
            }

            // ---- ⑥ 普通那一支（注意 duration 取的是**原始 options** 的那个）----
            _blendTime = o.duration; _left = o.duration;
            _doFade = !_doFade;                                       // 同上
            _targetFade = _cur.targetValue;
        }

        /// <summary>= 原版 `Update()`（判据见类注释；推进权收归执行器，见本文件开头那段）。</summary>
        public override void Advance(float dt)
        {
            if (!_doFade) return;
            // 原版是 `Time.deltaTime / blendTime`：`blendTime == 0` 时得 +∞ ⇒ `MoveTowards` 一步到位。
            // 这里显式把 `blendTime <= 0` 走「一步到位」那一支 —— 与 +∞ **行为等价**，同时躲开 `dt==0` 时的 NaN。
            float speed = (_blendTime <= 0f) ? float.PositiveInfinity : dt / _blendTime;
            if (renderersBlend != null)
                for (int i = 0; i < renderersBlend.Length; i++)
                {
                    var r = renderersBlend[i];
                    if (r == null || r.renderer == null)
                    {
                        Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name}) 的第 {i} 条 "
                                     + "`renderersBlend` 缺 `renderer` —— 原版这里会抛 NRE；我们出声并跳过这一条");
                        continue;
                    }
                    r.DoBlend(_targetFade, speed);
                }

            // 完成判定在「跑完所有渲染器」之后（原版先判再减）
            if (_left <= 0f)
            {
                if (_hasCur)
                {
                    var cb = _cur.onComplete;
                    _cur.onComplete = null; _hasCur = false;
                    if (cb != null) cb();
                }
                _doFade = false;
            }
            _left -= dt;
        }

        /// <summary>判据 = `…__ToggleRenderers.c`：逐个 `renderer.enabled = option`。</summary>
        public void ToggleRenderers(bool option)
        {
            if (renderersBlend == null) return;
            for (int i = 0; i < renderersBlend.Length; i++)
                if (renderersBlend[i] != null) renderersBlend[i].ToggleRenderer(option);
        }

        /// <summary>判据 = `…__SetTargetFade.c`：`targetFade = <第1个实参>; blendTime = currentBlendTime = <第2个实参>`。</summary>
        public void SetTargetFade(float targetValue, float blendTimeArg)
        {
            _targetFade = targetValue; _blendTime = blendTimeArg; _left = blendTimeArg;
        }
    }

    /// <summary>把旁挂里的一个 `Target` 翻成工程里的对象。**两条路各一份实现**
    /// （prefab 侧按层级路径 · 场景侧按名字+最近位置）—— 两条路的判据不同，别在这里混成一套。
    /// 返回 null = 这个目标我们工程里没有（**解析器自己负责出声**，别静默）。</summary>
    public interface IEnvTargetResolver
    {
        ParticleSystem PsOf(EnvBlendables.Target t);
        Renderer RendererOf(EnvBlendables.Target t);
        GameObject GoOf(EnvBlendables.Target t);
        ParticleSystemAreaSpawner SpawnerOf(EnvBlendables.Target t);
        ParticleSystemAreaSpawnerController ControllerOf(EnvBlendables.Target t);
        /// <summary>🆕 `FlareScenarioToggler.lensFlare`（`LensFlareComponentSRP`）。</summary>
        LensFlareComponentSRP FlareOf(EnvBlendables.Target t);
        /// <summary>🆕 `ScenarioAnimationBlend.myAnimation` / `TauCannonAnimationStopper.animationComponent`。</summary>
        Animation AnimationOf(EnvBlendables.Target t);
        /// <summary>🆕 `TauCannonAnimationStopper.cannon`。</summary>
        Transform TransformOf(EnvBlendables.Target t);
    }

    /// <summary>按旁挂那条 `cls` 建组件、把目标填进去（两个调用点：环境 prefab 实例内 · 战场场景侧）。
    /// 目标怎么翻成对象由 `IEnvTargetResolver` 给（prefab 侧按**路径**、场景侧按**名字+最近位置**，
    /// 由 `EnvironmentApplier` 提供 —— 两条路的判据不同，别在这里混成一套）。</summary>
    public static class ScenarioBlendableFactory
    {
        /// <summary>按旁挂那条 `cls` 建组件、把目标填进去（两个调用点：环境 prefab 实例内 · 战场场景侧）。
        /// 🆕 2026-10-07：参数从「5 个 `Func`」换成**一个 `IEnvTargetResolver`** —— 目标种类涨到 8 种
        /// （`ps`/`renderer`/`go`/`spawner`/`controller`/`flare`/`animation`/`transform`），再摊成 lambda 就没人读得动了。
        /// `manager` 由调用方在返回值上补（`c.manager = this`）—— 只有需要「当前环境 SO」的那个类会读它。
        /// 🔴 **不许静默**：`default` 那条会出声；某个目标解析不到时**解析器自己出声**。</summary>
        public static ScenarioBlendable Create(EnvBlendables.Item it, GameObject host,
                                               IEnvTargetResolver res, bool verbose)
        {
            if (it == null || host == null) return null;
            if (res == null) res = NullResolver.Instance;
            ScenarioBlendable c = null;
            switch (it.cls)
            {
                case "ScenarioParticleSystemToggler":
                {
                    var t = host.AddComponent<ScenarioParticleSystemToggler>();
                    t.particleSystems = CollectTargets(it, "ps", res.PsOf);
                    c = t; break;
                }
                case "ScenarioParticleSystemBlender":
                {
                    var b = host.AddComponent<ScenarioParticleSystemBlender>();
                    b.particleSystems = CollectTargets(it, "ps", res.PsOf);
                    b.forceEnableEmittersOnEnable = it.GetBool("forceEnableEmittersOnEnable");
                    b.notifyFinishWhenNoParticles = it.GetBool("notifyFinishWhenNoParticles");
                    c = b; break;
                }
                case "ScenarioMaterialFader":
                {
                    var f = host.AddComponent<ScenarioMaterialFader>();
                    f.renderers = CollectRenderers(it, res.RendererOf);
                    f.fadeOnEnable = it.GetBool("fadeOnEnable");
                    f.isInDefaultScenario = it.GetBool("isInDefaultScenario");
                    f.restoreOriginalMaterialsOnFadeOut = it.GetBool("restoreOriginalMaterialsOnFadeOut");
                    f.restoreOriginalMaterialsOnFadeIN = it.GetBool("restoreOriginalMaterialsOnFadeIN");
                    f.disableOnFadeOut = it.GetBool("disableOnFadeOut");
                    c = f; break;
                }
                case "ScenarioGenericObjectToggler":
                {
                    var g = host.AddComponent<ScenarioGenericObjectToggler>();
                    g.gameObjects = CollectTargets(it, "go", res.GoOf);
                    g.isInDefaultScenario = it.GetBool("isInDefaultScenario");
                    c = g; break;
                }
                case "ScenarioParticleSpawnerBlender":
                {
                    // 🆕 2026-10-06：4 个实例。两个数组各自是**目标对象上的组件**
                    //（`SpawnerOf` / `ControllerOf` 负责「按旁挂找到那个对象、把组件挂上、把 6 个字段填进去」）。
                    var s = host.AddComponent<ScenarioParticleSpawnerBlender>();
                    s.controllers = CollectTargets(it, "controller", res.ControllerOf);
                    s.areaSpawners = CollectTargets(it, "spawner", res.SpawnerOf);
                    if (verbose && s.areaSpawners.Length == 0 && s.controllers.Length == 0)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）旁挂里一个目标都没解析出来"
                                       + " —— 这一条不会生成任何粒子（出声，不静默）");
                    c = s; break;
                }
                case "FlareScenarioToggler":
                {
                    // 🆕 2026-10-07（A136）：原版 6 个实例。它的 `lensFlare` 就挂在宿主自己身上
                    // （`OnValidate` 就是 `GetComponent<LensFlareComponentSRP>()`）⇒ 先问解析器、问不到再就地取。
                    var f = host.AddComponent<FlareScenarioToggler>();
                    f.lensFlare = ResolveFirst(it, "flare", res.FlareOf);
                    if (f.lensFlare == null) f.lensFlare = host.GetComponent<LensFlareComponentSRP>();
                    if (verbose && f.lensFlare == null)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）没解析到 `LensFlareComponentSRP`"
                                       + " —— 这一条的补间不会发生（出声，不静默）");
                    c = f; break;
                }
                case "ScenarioAnimationBlend":
                {
                    // 🆕 2026-10-07（A136）：原版 2 个实例。⚠️ 数据缺口两处（见类注释）：没有 `Animation` 组件、
                    // 也没有那两个 clip ⇒ 这里照常建组件、把解析到的填进去，缺的部分由类自己出声。
                    var a = host.AddComponent<ScenarioAnimationBlend>();
                    a.filterCode = it.GetS("filterCode");
                    a.myAnimation = ResolveFirst(it, "animation", res.AnimationOf);
                    if (a.myAnimation == null) a.myAnimation = host.GetComponent<Animation>();
                    a.blendTime = it.GetF("blendTime", 0.3f);
                    a.autoPlayWhenChange = it.GetBool("autoPlayWhenChange", true);
                    a.clipLoader = AnimationClipByGuid;
                    c = a; break;
                }
                case "ScenarioGenericMaterialBlend":
                {
                    // 🆕 2026-10-07（A135）：原版 1 个实例（GSC 场 `…/Floor`）—— `FilterOptions` 的唯一消费方。
                    var g = host.AddComponent<ScenarioGenericMaterialBlend>();
                    g.renderersBlend = CollectMaterialBlenders(it, res);
                    g.fadeOnEnable = it.GetBool("fadeOnEnable");
                    g.isInDefaultScenario = it.GetBool("isInDefaultScenario");
                    g.filterCode = it.GetS("filterCode");
                    if (verbose && string.IsNullOrEmpty(g.filterCode))
                        Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）的 `filterCode` 是空的 —— "
                                       + "原版这一类**靠它**决定「只在哪条 SO 下响应」（判据见类注释）；"
                                       + "旁挂里没有它 ⇒ 这一条会对所有 SO 都响应（**与原版不同**，出声）");
                    if (verbose && g.renderersBlend.Length == 0)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）一条 `renderersBlend` 都没解析出来"
                                       + " —— 这一条不会补间任何东西（出声，不静默）");
                    c = g; break;
                }
                case "TauCannonAnimationStopper":
                {
                    // 🆕 2026-10-07（A136）：原版 2 个实例。🆕 2026-10-11（A196）：两个类**已经移植进工程**，
                    // 这两条不再「恒 null」——改成**就地建组件**（宿主对象在不在见类注释 ②；
                    // 建不出来时由 `MakeLookAtConstrains` / `MakeAnimFx` **逐条出声**，不静默）。
                    var t = host.AddComponent<TauCannonAnimationStopper>();
                    t.lookAtConstrains = MakeLookAtConstrains(it, res);              // 原版 `LookAtConstrainWIP[]`
                    t.animationComponent = ResolveFirst(it, "animation", res.AnimationOf);
                    t.cannon = ResolveFirst(it, "transform", res.TransformOf);
                    t.finalRotation = new Vector3(it.GetF("finalRotation.x"), it.GetF("finalRotation.y"),
                                                  it.GetF("finalRotation.z"));
                    t.particleSystems = CollectTargets(it, "ps", res.PsOf);
                    t.animFXController = MakeAnimFx(it, res);                        // 原版 `AnimFXController`
                    if (verbose && (t.cannon == null || t.particleSystems.Length == 0))
                        Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）连 `cannon` / 粒子都没解析出来"
                                       + " —— 这一条几乎不会有效果（出声，不静默）");
                    c = t; break;
                }
                default:
                    // ⚠️ **不许静默**：没实现的类要出声
                    //（原版 9 个实现类 2026-10-07 起**全在这张表里**了 —— 真走到这儿 = **旁挂里出现了新类名**，
                    //  那是这一族又扩了 / 名字抄错了，两种情况都该当场看见）
                    if (verbose)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}` 不在工厂的表里 —— 这一条不生效。owner=`{it.owner}`"
                                       + " · 判据 → 项目任务.md §三 第 30 条"
                                       + "（原版 9 个实现类应已全收，见 `gen_env_blendables.py` 的 CLASSES）");
                    return null;
            }
            return c;
        }

        /// <summary>把旁挂里 **kind == `kind`** 的目标逐个翻成对象，丢掉解析不到的那些
        /// （**解析器自己负责出声** —— 这里不吞）。
        /// 🆕 2026-10-07：原来 `ps`/`renderer`/`go`/`spawner`/`controller` 各写一份、五段代码只差**类型与 kind 两个字面量**
        /// ⇒ 合成这一份（本仓铁律 6：同一份判据别写五遍）。</summary>
        static T[] CollectTargets<T>(EnvBlendables.Item it, string kind, Func<EnvBlendables.Target, T> resolve)
            where T : class
        {
            if (it.targets == null || resolve == null) return new T[0];
            int n = 0;
            var tmp = new T[it.targets.Length];
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != kind) continue;
                var v = resolve(it.targets[i]);
                if (v != null) tmp[n++] = v;
            }
            var outp = new T[n];
            Array.Copy(tmp, outp, n);
            return outp;
        }

        /// <summary>只用一次的那些目标（`flare` / `animation` / `transform` —— 原版那几个字段都是**单个引用**）。</summary>
        static T ResolveFirst<T>(EnvBlendables.Item it, string kind, Func<EnvBlendables.Target, T> resolve)
            where T : class
        {
            if (it.targets == null || resolve == null) return null;
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != kind) continue;
                var v = resolve(it.targets[i]);
                if (v != null) return v;
            }
            return null;
        }

        /// <summary>🆕 2026-10-11（A196）：旁挂里 `kind == "lookat"` 的目标 = **原版挂在那两个炮塔节点上的
        /// `LookAtConstrainWIP` 组件**（`Railgun Turret Base.00N` / `Cylinder.00N`，各 1 个 × 2 个炮塔）。
        /// 逐条建；**建不出来就不进数组**（`MakeLookAt` 自己出声）。
        /// 🔴 宿主对象怎么找：**借 `res.GoOf(t)`** —— `FindGoInPrefab`/`FindGoInScene` 只按
        /// `path` / `leaf+pos` 找对象、**根本不看 `t.kind`**（判据：`EnvironmentApplier.cs:531-532` 与 `:761-762`）
        /// ⇒ 对 `lookat`/`animfx` 同样成立。**不必**给 `IEnvTargetResolver` 再加两个方法
        ///（加了就是把「按路径/按名字找对象」这件事写第二遍 = 本仓铁律 6）。</summary>
        static LookAtConstrainWIP[] MakeLookAtConstrains(EnvBlendables.Item it, IEnvTargetResolver res)
        {
            if (it.targets == null || res == null) return new LookAtConstrainWIP[0];
            var list = new System.Collections.Generic.List<LookAtConstrainWIP>();
            for (int i = 0; i < it.targets.Length; i++)
            {
                var t = it.targets[i];
                if (t == null || t.kind != "lookat") continue;
                var c = MakeLookAt(t, res);
                if (c != null) list.Add(c);
            }
            return list.ToArray();
        }

        /// <summary>建一个 `LookAtConstrainWIP` 并**逐字段照旁挂填**（字段来源 = `gen_env_blendables.py`
        /// 的 `TARGET_FIELDS['LookAtConstrainWIP']`，直读 bundle 的 `MonoBehaviour`）。
        /// 🔴 **`target` 解析不到就不建**（而且出声）—— 理由两条，都不是偷懒：
        ///   ① 原版 `Update()` 在 `target == null` 时**每帧 `LogWarning`** ⇒ 建出来就是一个纯刷屏的空转组件；
        ///   ② 组件的**唯一作用**就是「每帧照着 `target` 转向」，没 target = 它什么都做不了。
        ///   ⇒ 宁可**不建 + 出声点名缺哪个对象**（那些对象 2026-10-11 A191 起**应当都已建出**；
        ///      真缺就是那件 arena prefab 没重建 —— 跑 `ArenaBuilder.BuildArenaPrefabs`）。</summary>
        static LookAtConstrainWIP MakeLookAt(EnvBlendables.Target t, IEnvTargetResolver res)
        {
            var host = res.GoOf(t);
            if (host == null)
            {
                Debug.LogWarning($"[EnvBlend] `LookAtConstrainWIP`(`{t.leaf}`) 的**宿主对象**解析不到 —— "
                               + "这一条不建（出声，不静默）。`Railgun Turret Base.00N` / `Cylinder.00N` 这两类宿主"
                               + "应当已由 A191 建场建出（`ArenaBuilder.ApplyGroupNodes`）；若仍出现 ⇒ 那件 arena prefab 没重建");
                return null;
            }

            string tpath = t.GetS("target");
            GameObject targetGo = null;
            if (!string.IsNullOrEmpty(tpath))
            {
                // 旁挂里存的是**原版的整条层级路径**（`Scenario/…/Railgun Turret N Target`）——
                // 🆕 2026-10-11（A191）起那条父链**我们也有了**，但 `SceneResolver` 那一侧的判据本来就是
                // 「名字 + 最近位置」（`FindNearest` 递归整棵子树）⇒ 拿**最后一段**（叶子名）找**照旧成立**，
                // 而且**不必**因为建了父链就改成按路径找（那会让 prefab 侧与场景侧又分叉一次，铁律 6）。
                // ⚠️ 原版那名字**带一个尾随空格**（`Railgun Turret 1 Target `）：`FindNearest` 的比较走 `Norm()`（会 Trim）
                //    ⇒ 对得上；这里**别**顺手把它 Trim 掉再存回去（旁挂里那份是原样留档的判据）。
                var segs = tpath.Split('/');
                targetGo = res.GoOf(new EnvBlendables.Target { leaf = segs[segs.Length - 1], path = tpath });
            }
            if (targetGo == null)
            {
                Debug.LogWarning($"[EnvBlend] `LookAtConstrainWIP`(`{t.leaf}`) 的 `target`（原版指向 `{tpath}`）"
                               + "解析不到 —— 这一条**不建**（理由：原版 `Update()` 在 target 为空时每帧告警、"
                               + "而且组件本身什么都做不了）。出声，不静默；那两件 `Railgun Turret N Target` 应当已由 "
                               + "A191 建场建出（`ArenaBuilder.ApplyGroupNodes`）—— 若仍出现 ⇒ 那件 arena prefab 没重建");
                return null;
            }

            var c = host.AddComponent<LookAtConstrainWIP>();
            if (c == null) return null;
            c.target = targetGo.transform;
            c.lockXAxis = t.GetB("lockXAxis");
            c.lockYAxis = t.GetB("lockYAxis");
            c.lockZAxis = t.GetB("lockZAxis");
            c.upVector = new Vector3(t.GetF("upVector.x", 0f), t.GetF("upVector.y", 1f), t.GetF("upVector.z", 0f));
            c.rotationOffset = new Vector3(t.GetF("rotationOffset.x"), t.GetF("rotationOffset.y"),
                                           t.GetF("rotationOffset.z"));
            return c;
        }

        /// <summary>🆕 2026-10-11（A196）：旁挂里 `kind == "animfx"` 的目标 = 原版挂在那颗 GameObject
        /// 上的 `AnimFXController`。建不出来就不建（宿主不在、或字段缺，都出声）。
        /// 🆕 2026-10-12（A340 + A341）**订正**：原来这里写着「三层旁挂没收、`preventDestroy = false`
        /// 那条路没实现」—— 两条**都已做**（`BuildSoundTrack` / `WarnAnimFxModules` / `SelfDestroyScheduled`）。
        /// 🆕 2026-10-12（A393）：本方法现在**两个调用点**共用 —— ① blendable 那条（`Create` 里
        /// `TauCannonAnimationStopper.animFXController`）② 场景侧 standalone（`BuildSceneAnimFx`）。
        /// ⇒ 多了个 `selfDestroyOk` 参数，见它的注释。</summary>
        /// <param name="selfDestroyOk">🆕 A393：**原版那句 `OnEnable` 到底会不会跑**。
        /// 原版 `OnEnable` 只在「组件 `m_Enabled = 1` ∧ 宿主 `activeInHierarchy`」时才跑 ⇒ 那两句
        /// （`currentTime = 0` 与 `if (destroyTime &gt; 0 &amp;&amp; !preventDestroy) Destroy(go, destroyTime)`）
        /// 才执行。**两档在数据里真的都存在**：场景侧 7 个实例里 `battlearena3` 两个 `Lightning_Green`
        /// 的组件是关的、tauviorla 的 `Big Gun Effect` 宿主 GO 是关的 —— 它们原版**永远不自毁**。
        /// 传 `false` ⇒ 这一跳**不做**（否则我们会排定一次**原版根本不会有**的销毁，而且 `Destroy(go, t)`
        /// **取消不掉**）。默认 `true` = blendable 那条路今天的行为，一个字节不变。</param>
        /// <param name="simulateSelfDestroyInEditor">🆕 **2026-10-12（A431 收红那轮）**：**编辑模式那一档要不要
        /// 「当场模拟自毁」**。编辑模式没有帧循环 ⇒ `Destroy(go, t)` 不生效，本方法原来一律改走
        /// `DestroyImmediate(宿主)`（= 当场销毁）。但**两条调用链要的东西不一样**：
        ///   · `Create`（blendable 那条）与 A341 那条**合成探针**：要的就是「自毁这条支路真的会执行」⇒ 传 `true`（默认）；
        ///   · `BuildSceneAnimFx`（场景侧 standalone，A393/A417）：要的是「**组件建出来 + 5 个子件改挂**」——
        ///    原版那一刻组件**确实还在**（`destroyTime = 6s` 是**排定**、不是当场）⇒ 传 **`false`**。
        /// ⛔ 不传 `false` 的后果**就在 2026-10-12 那四条红里**（实读 `d:/4/_tmp_view/battle.log` 的 arena2 段 +
        ///   `D9_Battle六红诊断.md` §二·3 的链条）：`battlearena2` 的 `RocketTrail` 是那 5 条里**唯一**
        ///   `preventDestroy = 0 ∧ enabled ∧ goActive` 的一条 ⇒ 宿主被**当场删掉** ⇒ `return c` 回来的是一个
        ///   **已销毁**的组件（Unity 的 `==` 判它 null）⇒ `BuildSceneAnimFx` 把它记成「`MakeAnimFx` 建不出来
        ///   （宿主/字段缺）」、`continue` 跳过 ③ 改挂 ⇒ **A431 四条红**。⚠️ **只影响编辑模式**：
        ///   `Application.isPlaying` 那一档（真 Play / 生产）照旧 `Destroy(go, c.destroyTime)`，与本参数**无关**。</param>
        static AnimFXController MakeAnimFx(EnvBlendables.Item it, IEnvTargetResolver res, bool selfDestroyOk = true,
                                           bool simulateSelfDestroyInEditor = true)
        {
            if (it.targets == null || res == null) return null;
            for (int i = 0; i < it.targets.Length; i++)
            {
                var t = it.targets[i];
                if (t == null || t.kind != "animfx") continue;

                var host = res.GoOf(t);
                if (host == null)
                {
                    Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的**宿主对象**解析不到 —— "
                                   + "这一条不建（出声，不静默）。两种可能：① blendable 那条路上，`Railgun turret` "
                                   + "那两颗应当已由 A191 建场建出（`ArenaBuilder.ApplyGroupNodes`）—— 若仍出现 ⇒ 那件 "
                                   + "arena prefab 没重建；② 场景侧 standalone 那条路上（A393），宿主本该由 "
                                   + "`BuildSceneAnimFx` 照旁挂 `sceneStandaloneBuild.nodes[]` 现场建 —— 若仍出现 ⇒ "
                                   + "那条 `nodes[]` 里没有它（旁挂旧了 / 重建过）");
                    return null;
                }

                // 🔴 `preventDestroy` **必须**来自旁挂（`TARGET_FIELDS['AnimFXController']` 收的就是它）：
                //   拿不到就**不建** —— 本仓铁律 5·c 明写「⛔ 不要用序列化的默认值顶替」，
                //   而且这里顶替的后果是实打实的（blendable 那条路上**能走到工厂的**那 2 条都是 `true`
                //   —— 是 `2/2`，不是「4/4」：全库场景侧 7 个实例里另 3 个是 `0`，它们走的是
                //   `BuildSceneAnimFx` 那条路（A393 起），不经过 `Create`；
                //   🆕 **订正**：原来这里写「它们不归任何 blendable 管、走不到这里」—— 那句话在 A393
                //   之前是对的，现在那 5 条有专门的入口了，见本方法上面那段类注释；
                //   我们那份默认值虽然也是 `true` —— 见 `AnimFXController.preventDestroy` 那条 🔴
                //   —— 但那是**为了躲 `AddComponent` 先跑一次 `OnEnable` 的次序坑**，不是「数据」）。
                float pd = t.GetF("preventDestroy", -1f);
                if (pd < 0f)
                {
                    Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的旁挂里**没有 `preventDestroy`** "
                                   + "—— 这一条不建（拿默认值顶数据 = 静默错，本仓红线）。出声，不静默。"
                                   + "跑一次 `python 工具/gen_env_blendables.py` 重生成旁挂");
                    return null;
                }

                var c = host.AddComponent<AnimFXController>();
                if (c == null) return null;
                c.preventDestroy = pd != 0f;
                c.destroyTime = t.GetF("destroyTime", 4f);              // 原版 ctor 的默认值就是 4f
                c.exitDestroyTime = t.GetF("exitDestroyTime", AnimFXController.SAFE_DESTROY_TIME);

                // 🆕 2026-10-12（A340）：三层照旁挂填。
                // 🔴 **订正**：原来这里是一条 LogWarning，写着「`sounds` / `exitSounds` / `modules` 三层
                //    **旁挂没收**」⇒ 建出来的实例这三项恒空。**那个缺口 2026-10-12 已收**：
                //    `gen_env_blendables.py` 新开了 `pack_animfx_defs`（摊平成 `sounds.<i>.*` 等键，
                //    跨包的 `AudioCue` 引用走 `CueNames` 解成 **cue 名**）⇒ 现在两层音效能照原版建出来。
                c.sounds = BuildSoundTrack(t, "sounds");
                c.exitSounds = BuildSoundTrack(t, "exitSounds");
                WarnAnimFxModules(t);                                   // 模块那一层：建不出来 ⇒ 有就出声（见方法注释）

                // 🆕 2026-10-12（A341）：照抄原版 `OnEnable()` 的**第二句** —— 我们这份 `preventDestroy`
                //   的**默认值是 `true`**（躲 `AddComponent` 会先跑一次 `OnEnable` 的次序坑，见
                //   `AnimFXController.cs` 的「有意偏离 ⑤」）⇒ 那一次 `OnEnable` **不会排定自毁**，
                //   所以由工厂在这里补排一次，条件**逐字照原版**（`destroyTime > 0` 也在内）。
                // 🆕 2026-10-12（A393）：外面**再套一层** `selfDestroyOk` —— 原版那句所在的 `OnEnable`
                //   只有在「组件开着 ∧ 宿主 GO 开着」时才会跑（见参数注释）；不然我们会排定一次
                //   **原版根本不会有**的销毁。
                if (selfDestroyOk && !c.preventDestroy && c.destroyTime > 0f)
                {
                    SelfDestroyScheduled++;                            // 可观测点（自检用它断这条支路真的执行了）
                    // ⚠️ 批处理下没有帧循环 ⇒ `Destroy`（延时销毁）**不生效**（CLAUDE.md §三）⇒
                    //    编辑模式那一档走 `DestroyImmediate` —— 与本文件族既有做法一致
                    //    （`EnvironmentApplier.FinishFadeOut` 是同一句）。两档**判据同一句**，差别只在
                    //    「排定」vs「当场」；运行时那一档与原版逐字同路。
                    // 🔴 **2026-10-12（A431 收红那轮）：编辑模式这一档现在可以「只排不定」**（见下面 `else if`
                    //    与 `simulateSelfDestroyInEditor` 参数）—— `BuildSceneAnimFx` 传 `false`，因为它要的是
                    //    「组件建出来 + 子件改挂」，而原版那一刻组件**确实还在**。
                    //    ⛔ 运行时那一档（`Application.isPlaying`）**一个字都不许动**。
                    if (Application.isPlaying) UnityEngine.Object.Destroy(c.gameObject, c.destroyTime);
                    else if (simulateSelfDestroyInEditor) UnityEngine.Object.DestroyImmediate(c.gameObject);
                    else
                    {
                        // 出声（⛔ 不静默）：这一档「排了但**不当场销**」是有意为之 —— 让调用方
                        // 事后仍能拿到这颗组件与它底下的子件；宿主会在下一次编辑模式自检/场景重载时随场景一起没。
                        Debug.Log($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 排定了自毁（`destroyTime = "
                                + $"{c.destroyTime:F1}s`）但**编辑模式这一档不模拟当场销毁**（调用方要求）"
                                + " —— 组件保留、子件照改挂；真 Play 下这一档走 `Destroy(go, destroyTime)`");
                    }
                }
                return c;
            }
            return null;
        }

        // ============================ 🆕 2026-10-12（A340 / A341）============================

        /// <summary>A341：工厂**排定过几次自毁** —— 那条支路的**可观测点**。
        /// 为什么需要它：**blendable 那条路**上那 2 条（tauviorla 两个 `Railgun turret`）`preventDestroy`
        /// 都是 `true` ⇒ 那条路上这条支路**数据走不到**；而 `Destroy`（延时销毁）在批处理下不生效
        /// （CLAUDE.md §三）⇒ 自检要验「这条支路真的执行了」，只能靠这个计数器（配一条**合成的**
        /// `preventDestroy = 0` 目标 —— ✅ **2026-10-12（A417）起 `sceneStandalone` 那 5 条已经真接线**，
        /// 但那 5 条里只有 `battlearena2` 的 `RocketTrail` 一颗会真自毁 ⇒ **合成目标这一手仍然要做**）。
        /// 🆕 **2026-10-12（A393）订正**：原来这里写着「另 3 个 `preventDestroy = 0` 的实例不归任何
        /// blendable 管 ⇒ 工厂今天仍走不到它们（要建得先给这条链开一节）」—— **那一节已经开了**：
        /// `BuildSceneAnimFx` + 旁挂 `sceneStandalone` / `sceneStandaloneBuild`（那 5 条走同一条链）。
        /// ⚠️ 但**这个计数器仍然主要靠合成目标**：那 5 条里只有 `battlearena2` 的 `RocketTrail`
        /// **真的会自毁**（另两个 `Lightning_Green` 组件是关的、`Big Gun Effect` 宿主 GO 是关的
        /// ⇒ `MakeAnimFx` 的 `selfDestroyOk` 传 `false`、`Railgun BIG (1)` 是 `preventDestroy = 1`）。
        /// ⚠️ 它只是**观测点**，不参与任何逻辑。</summary>
        public static int SelfDestroyScheduled { get; private set; }

        /// <summary>🆕 2026-10-12（A340）：把旁挂里**一层** `PlaySoundOnTime[]` 建出来
        /// （`sounds` 与 `exitSounds` 除键前缀外完全同形 ⇒ 共用这一份，本仓铁律 6）。
        ///
        /// 旁挂键（生成器 `gen_env_blendables.py` 的 `pack_animfx_defs`，每个键的出处写在那里）：
        ///   · `<arr>.count` —— 原版这层的**条数**。🔴 **缺这个键 = 旁挂没收这一层**（旧版旁挂）⇒ **出声**，
        ///     与「原版这层本来就是空的」（写 `0`）**分开** —— 前者是缺口、后者是数据。
        ///   · `<arr>.<i>.sound` —— **cue 名**（本工程里 `PlaySoundOnTime.sound` 存的是 cue 名，
        ///     判据见 `AnimFXController.cs` 的「有意偏离 ①」）。空 = 这条没有 cue（原版就有空引用这一类，
        ///     留档写空串 ⇒ 不播、也不出声）。
        ///   · `<arr>.<i>.soundUnresolved` = 1 —— 🔴 **生成器解不出那条跨包引用**时的**标记键**
        ///     （`GetS` 分不开「值是空串」与「键不在」，那两档含义相反 ⇒ 解不出时**显式留一条**）。
        ///     这里见到它就出声点名到第几条。
        ///   · `<arr>.<i>.{time,is2d,repeat,loops,timeInterval}` —— 照 `PlaySoundOnTime` 的字段名。
        /// ⚠️ 我们**不**在这里过滤「cue 表里有没有这个 cue」—— 到点由 `PlaySoundOnTime.Update` →
        ///   `AnimFXController.PlayCue` 去解，那里对解不出的 cue **出声**（`WFSoundBank.BadCues`）。
        ///   这里过滤 = 把「播的时候才发现表里没有」变成「悄悄少播一条」（静默，本仓红线）。</summary>
        static PlaySoundOnTime[] BuildSoundTrack(EnvBlendables.Target t, string arr)
        {
            float nf = t.GetF(arr + ".count", -1f);
            if (nf < 0f)
            {
                Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的旁挂里**没有 `{arr}.count`** —— "
                               + $"这一层（原版 `{arr}`）没接上，这个实例这一层是空的。跑一次 "
                               + "`python 工具/gen_env_blendables.py` 重生成旁挂（出声，不静默）");
                return new PlaySoundOnTime[0];
            }
            int n = (int)nf;
            if (n <= 0) return new PlaySoundOnTime[0];      // 原版这层本来就是空的（数据，不是缺口）

            var outp = new PlaySoundOnTime[n];
            int noCue = 0, withCue = 0, unresolved = 0;
            for (int i = 0; i < n; i++)
            {
                string pre = arr + "." + i + ".";
                string cue = t.GetS(pre + "sound");
                if (string.IsNullOrEmpty(cue))
                {
                    noCue++;
                    // 「**解不出**」与「**原版就是空引用**」两档的含义相反，而 `GetS` 分不开它们
                    //   （`Core/EnvBlendables.cs` 的 `GetS` 对「键不在」与「值是空串」都回 `""`；
                    //    `EnvironmentApplier.HasField` 是同一用途、但它是 `private`，本件动不了它）
                    //   ⇒ 生成器解不出时**显式写一条 `<arr>.<i>.soundUnresolved` = 1**，这里据此出声
                    //     （点名到第几条，不靠生成期那道闸**独占**这个信号）。
                    if (t.GetF(pre + "soundUnresolved", 0f) != 0f)
                    {
                        unresolved++;
                        Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的 `{arr}.{i}` 那条 cue "
                                       + "**解不出来**（旁挂写下了 `soundUnresolved`）—— 这一条不会响。"
                                       + "跑一次 `python 工具/gen_env_blendables.py --check` 看 `_unresolved`"
                                       + "（出声，不静默）");
                    }
                }
                else withCue++;
                outp[i] = new PlaySoundOnTime
                {
                    time = t.GetF(pre + "time"),
                    sound = cue,
                    is2d = t.GetF(pre + "is2d") != 0f,
                    repeat = t.GetF(pre + "repeat") != 0f,
                    loops = (int)t.GetF(pre + "loops"),
                    timeInterval = t.GetF(pre + "timeInterval"),
                };
            }
            if (withCue == 0)
                Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的 `{arr}` 旁挂里记着 {n} 条"
                               + $"（没有 cue 名 {noCue} 条，其中**解不出** {unresolved} 条）、**一条 cue 都没有**"
                               + " ⇒ 这一层不会出声（出声，不静默）");
            return outp;
        }

        /// <summary>🆕 2026-10-12（A340）：`modules` 那一层的**如实**处理（⛔ 不是「已覆盖」）。
        ///
        /// 原版 `AnimFXController.modules` 是 `List<AnimFXModuleBase>`；旁挂**已经把类名收进来了**
        /// （`modules.<i>` = 类名 · `modules.count` = 条数），但我们**建不出来**：
        ///   · 本工程这条线（战场场景侧）的模块基类不存在 —— 特效那条线有 `WarpforgeVFX.WFEffectModule`，
        ///     可它的 `Initialize` 收的是 `WarpforgeEffectPlayer`（**另一条线**的控制器）⇒ 建出来也接不上
        ///     （见 `AnimFXController.cs` 的「有意偏离 ③」与 `SetData` 里那条出声）；
        ///   · 场景侧 7 个实例里只有 **1** 个有模块（`AnimFXModuleScreenShake`，挂在 `Railgun BIG (1)` 上）。
        ///     ⚠️ **2026-10-12（A420）订正**：原来这句接着写「而它**不归任何 blendable 管** ⇒ 这条路上
        ///     模块数今天**恒为 0**（不会造出假警报）」—— 🔴 **A393（数据）+ A417（接线）之后不成立了**：
        ///     `Railgun BIG (1)` 是 `sceneStandalone` 那 5 条之一，`BuildSceneAnimFx` 会建它
        ///     ⇒ 这条警告**会在运行时真出现**（那时它是**正确的出声**，⛔ 别把它当假警报去消）。
        ///     判据 → `AnimFXController.cs` 文件头「旁挂里的三层」与「有意偏离 ③」。
        /// ⇒ 有模块就**点名报出类名**（出声，不静默）；要真建得先给场景侧一条模块线（记在报告里）。</summary>
        static void WarnAnimFxModules(EnvBlendables.Target t)
        {
            float nf = t.GetF("modules.count", -1f);
            if (nf < 0f)
            {
                Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的旁挂里**没有 `modules.count`** —— "
                               + "这一层没接上（旧版旁挂）。跑 `python 工具/gen_env_blendables.py` 重生成"
                               + "（出声，不静默）");
                return;
            }
            int n = (int)nf;
            if (n <= 0) return;

            var names = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++)
            {
                string cn = t.GetS("modules." + i);
                if (names.Length > 0) names.Append("、");
                names.Append(string.IsNullOrEmpty(cn) ? "（解不出类名）" : cn);
            }
            Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 原版挂着 {n} 个模块（{names}）—— "
                           + "我们**不建**（场景侧没有模块基类：特效那条线的 `WFEffectModule.Initialize` 收的是 "
                           + "`WarpforgeEffectPlayer`，见 `AnimFXController.cs` 的「有意偏离 ③」）⇒ 这一跳不做"
                           + "（出声，不静默；要做得先给场景侧一条模块线）");
        }

        // ==================== 🆕 2026-10-12（A393）：**场景侧、不被任何 blendable 管**的那几条 ====================
        //
        //  是什么：15 个 `scenes_scenes_*` 包里一共 **7** 个 `AnimFXController`，其中 2 个
        //  （tauviorla 两个 `Railgun turret`）是 `TauCannonAnimationStopper.animFXController` 的目标
        //  ⇒ 走 `CLASSES` 那条路（A340 接的）。**另 5 个谁都不管** —— 它们只是原版场景里一个普通
        //  MonoBehaviour（自己 `OnEnable`／靠帧循环），我们原来**连账都没有**（判据 → 旁挂
        //  `sceneStandalone` 一节，由 `工具/gen_env_blendables.py` 的 `collect_scene_standalone` 直读原版包产出；
        //  逐条表与「这 5 个各自到底有没有效果」→ `资料/普查产出_1012/H2_场景侧AnimFX.md` §二）。
        //
        //  🔴 **建法完全复用 `MakeAnimFx`**（同一个 kind `animfx`、同一份字段装配）—— ⛔ 不另写一份。
        //     本条只多做三件**数据侧**的事（都在旁挂里，见 `sceneStandaloneBuild`）：
        //       ① `nodes[]`   —— 宿主/祖先我们工程里**没有**的，照**原版 local TRS** 现场建（浅→深）；
        //       ② `reparent[]`—— 原版挂在宿主下面、我们已建出来的对象，改挂回去
        //                        （`RocketTrail` 那颗 `destroyTime = 6` 一销毁要**连带子件一起消失**）；
        //       ③ `enabled` / `goActive` —— 原版这 5 个里有 3 个**根本不跑**（两个 `Lightning_Green`
        //                        组件 `m_Enabled = 0`、`Big Gun Effect` 的 GO `activeInHierarchy = 0`）
        //                        ⇒ 不照抄这两个开关就会建出「原版不跑、我们跑」的假象。
        //
        //  ✅ **2026-10-12（A417）：已接线**（🔴 **订正**：原来这三行写着「🔴 **待接线**」）——
        //     调用点 = `Battle/ArenaRuntimeLoader.cs` 的 `Load()`（`CurrentKey = arenaKey;` 之后那一行，
        //     即 `Instantiate` 之后、`ArenaSceneState.Apply` 之前）。
        //     ⚠️ **只接了这一处，别再往 `EnvironmentApplier.EnsureSceneBlendables` 里加第二处**：
        //     ① 那条链第一次跑是在**打出进攻卡**时（这 5 条大半局都不存在）；
        //     ② 它传的根是 `Arena3D`、这里传的是 `Warpforge_<场>` —— 按-root 去重拦不住 ⇒ **会建两遍**。
        //     裁断 → `资料/普查产出_1012/H2_场景侧AnimFX.md` §五·1。
        // ==========================================================================================

        /// <summary>`sceneStandaloneBuild` 里的一个**要新建的节点**（判据 = 原版场景包直读，
        /// 见 `工具/gen_env_blendables.py` 的 `scene_standalone_build`）。字段名与 `gen_arena_groups.py`
        /// 的 `nodes[]` 同一套（同一件事别两套名字），只是这里**只覆盖「祖先链 + 宿主自己」**。</summary>
        [Serializable] public class AnimFxNode
        {
            public string path;       // 原版整条层级路径（本表内唯一；`parent` 引用的是同一个键）
            public string name;       // 节点名（原版原样，可能带尾随空格）
            public string parent;     // 原版父路径；空 = 挂在战场根下
            public float[] parentPos; // 父的**无缩放世界位置**（与清单同一套，用来按「名字 + 最近位置」对）
            public float[] localPos, localRot, localScale;   // **原版 local** TRS（直接写下去，不引入第二套口径）
        }

        /// <summary>`sceneStandaloneBuild` 里的一条**改挂**：把已经建出来的 `name` 挂到 `parent` 那条路径下。
        /// `pos` = 它自己的无缩放世界位置、`parentPos` = 宿主的（用来对同名对象）。</summary>
        [Serializable] public class AnimFxReparent
        {
            public string name, parent; public float[] pos, parentPos;
        }

        [Serializable] public class AnimFxBuildGroup
        {
            public string root; public AnimFxNode[] nodes; public AnimFxReparent[] reparent;
        }

        /// <summary>`Resources/EnvBlendables.json` 里这两节的**局部** DTO。
        /// 🔴 为什么不把它们并进 `Core/EnvBlendables.cs` 的 `File`：那个文件不在本件白名单里；
        ///   而 `JsonUtility` **忽略**目标类型里没有的键 ⇒ 这里照 `EnvironmentApplier.StandaloneFile`
        ///   那个先例**只解析自己要的两节**，**复用同一套 `EnvBlendables.Group` 类型**（不另定义一份 DTO）。</summary>
        [Serializable] class SceneStandaloneFile
        {
            public EnvBlendables.Group[] sceneStandalone;
            public AnimFxBuildGroup[] sceneStandaloneBuild;
        }

        static EnvBlendables.Group[] _sceneStan;
        static AnimFxBuildGroup[] _sceneStanBuild;
        static bool _sceneStanTried;

        static void LoadSceneStandalone()
        {
            if (_sceneStanTried) return;
            _sceneStanTried = true;
            var ta = Resources.Load<TextAsset>("EnvBlendables");
            if (ta == null)
            {
                Debug.LogError("[EnvBlend] 取不到 `Resources/EnvBlendables.json` ⇒ 场景侧那 5 条"
                             + "`AnimFXController`（不被任何 blendable 管的那些）**一条都建不出来**。"
                             + "跑一次 `python 工具/gen_env_blendables.py`。");
                return;
            }
            var f = JsonUtility.FromJson<SceneStandaloneFile>(ta.text);
            _sceneStan = f != null ? f.sceneStandalone : null;
            _sceneStanBuild = f != null ? f.sceneStandaloneBuild : null;
            if (_sceneStan == null || _sceneStan.Length == 0)
                Debug.LogError("[EnvBlend] `EnvBlendables.json` 里**没有 `sceneStandalone` 一节** ⇒ 场景侧那 5 条"
                             + "常驻 `AnimFXController` 会**静默消失**（其中 `battlearena2` 的 `RocketTrail`"
                             + "那颗一销毁要连带 6 个子件粒子）。重跑 `python 工具/gen_env_blendables.py`。");
        }

        /// <summary>自检用：`sceneStandalone` 一节的**总条数**（`-1` = 那一节没读到/解析失败）。原版直读 = **5**。</summary>
        public static int SceneStandaloneDataCount()
        {
            LoadSceneStandalone();
            if (_sceneStan == null) return -1;
            int n = 0;
            for (int i = 0; i < _sceneStan.Length; i++)
                if (_sceneStan[i] != null && _sceneStan[i].items != null) n += _sceneStan[i].items.Length;
            return n;
        }

        /// <summary>上一次 `BuildSceneAnimFx` 的结果 —— **可观测点**（自检直接读它）。
        /// · `SceneAnimFxBuilt` = 真的建出来几个组件（原版直读：battlearena2 1 · battlearena3 2 · tau 2）·
        /// · `SceneAnimFxNodesCreated` = 为它们新建了几个节点（原版有的对象我们工程里没有 ⇒ 照原版 local 建）·
        /// · `SceneAnimFxReparented` = 改挂回宿主下几个（只有 `RocketTrail` 那颗会销毁宿主 ⇒ 只有它有）·
        ///   ⚠️ **2026-10-12**：编辑模式下那颗宿主的「当场自毁」**不模拟**了（见 `MakeAnimFx` 的
        ///   `simulateSelfDestroyInEditor`）⇒ 改挂照做；真 Play 下也是**先改挂、6 秒后**宿主连同这 5 个子件
        ///   一起没 ⇒ **两档这个数都是 5**。
        /// · `SceneAnimFxMissed` / `SceneAnimFxMissedWhat` = 没建出来/没对上的条数与逐条名字（**出声，不静默**）。</summary>
        public static int SceneAnimFxBuilt { get; private set; }
        public static int SceneAnimFxNodesCreated { get; private set; }
        public static int SceneAnimFxReparented { get; private set; }
        public static int SceneAnimFxMissed { get; private set; }
        public static string SceneAnimFxMissedWhat { get; private set; }
        /// <summary>🆕 调用点**有没有人来调** —— 没接线时这个数是 0，正好能把
        /// 「数据在、但没人调」与「建了 0 条」区分开。
        /// ✅ **2026-10-12（A417）已接线**（`Battle/ArenaRuntimeLoader.cs` 的 `Load()`）⇒ 现在每 `Load()`
        /// 一次 +1。🔴 **订正**：原来这句写着「**调用点待接线**（见类注释那段 🔴）」。</summary>
        public static int SceneAnimFxCalls { get; private set; }

        static Transform _sceneAnimFxRoot;         // 上一轮是给哪个战场实例建的（同一个实例不重复建）

        /// <summary>🆕 2026-10-12（A393）：按旁挂把**场景侧那 5 条** `AnimFXController` 建出来。
        /// 调用时机 = **战场实例化之后**（原版这些组件就序列化在场景里 ⇒ 从「战场出现」那一刻就该在）；
        /// 传 `arenaKey` = 场名（`battlearena2` 这种）· `arenaRoot` = 战场实例的根（`Warpforge_&lt;场&gt;`）。
        ///
        /// 🔴 **不许静默**：宿主找不到 / cue 解不出 / 节点父路径不在树里 —— 全部逐条进
        ///   `SceneAnimFxMissedWhat` 并打一条日志。
        /// 🔴 **对象解析一律走生产那份 `SceneResolver`**（按「名字 + 最近位置」）—— ⛔ 别在这里另写一套找对象的。
        /// 🆕 **2026-10-12（A431 收红那轮）**：② 那一跳给 `MakeAnimFx` 的 `simulateSelfDestroyInEditor` 传
        ///   **`false`** —— 原版那条「6 秒后自毁」在**编辑模式不模拟**（原版那一刻组件确实还在；否则
        ///   `RocketTrail` 的宿主被当场删掉 ⇒ 组件被误记成「建不出来」、③ 改挂被跳过）。真 Play 照旧
        ///   `Destroy(go, 6s)`，与本参数无关（`D9_Battle六红诊断.md` §二·3）。</summary>
        public static int BuildSceneAnimFx(Transform arenaRoot, string arenaKey)
        {
            SceneAnimFxCalls++;
            SceneAnimFxBuilt = 0; SceneAnimFxNodesCreated = 0; SceneAnimFxReparented = 0;
            SceneAnimFxMissed = 0; SceneAnimFxMissedWhat = "";
            if (arenaRoot == null || string.IsNullOrEmpty(arenaKey))
            {
                Debug.LogWarning("[EnvBlend] `BuildSceneAnimFx` 收到空根/空场名 ⇒ 不建（出声，不静默）");
                return 0;
            }
            if (_sceneAnimFxRoot != null && _sceneAnimFxRoot == arenaRoot)
            {
                // ⚠️ 判据是**同一个战场实例**（不是同一个场名）：`ArenaRuntimeLoader` 换场时会先 `Unload`
                //    再实例化一份新的 ⇒ 新实例必须**重新建**（只按场名去重会让第二局没有这些组件）。
                //    Unity 的 `==` 对已销毁的对象回「null」⇒ 旧实例没了就自然走到重建那一支。
                Debug.Log($"[EnvBlend] 场景侧 `AnimFXController`（`{arenaKey}`）**这个战场实例已经建过** ⇒ 不重复建"
                        + "（原版那些组件随场景只出现一次）");
                return 0;
            }
            LoadSceneStandalone();
            if (_sceneStan == null || _sceneStanBuild == null) { _sceneAnimFxRoot = arenaRoot; return 0; }

            EnvBlendables.Item[] items = null;
            for (int i = 0; i < _sceneStan.Length; i++)
                if (_sceneStan[i] != null && _sceneStan[i].root == arenaKey) { items = _sceneStan[i].items; break; }
            if (items == null || items.Length == 0) { _sceneAnimFxRoot = arenaRoot; return 0; }

            AnimFxBuildGroup bg = null;
            for (int i = 0; i < _sceneStanBuild.Length; i++)
                if (_sceneStanBuild[i] != null && _sceneStanBuild[i].root == arenaKey) { bg = _sceneStanBuild[i]; break; }

            var missed = new System.Collections.Generic.List<string>();
            var res = new EnvironmentApplier.SceneResolver(arenaRoot);

            // ---- ① 先建缺的节点（旁挂已按「浅 → 深」排 ⇒ 父一定先于子出现）----
            var created = new System.Collections.Generic.Dictionary<string, Transform>();
            if (bg != null && bg.nodes != null)
            {
                for (int i = 0; i < bg.nodes.Length; i++)
                {
                    var n = bg.nodes[i];
                    if (n == null || string.IsNullOrEmpty(n.path)) continue;
                    var parentTr = ResolveAnimFxParent(arenaRoot, created, res, n.parent, n.parentPos, missed);
                    var go = new GameObject(string.IsNullOrEmpty(n.name) ? "AnimFX" : n.name);
                    go.transform.SetParent(parentTr, false);            // false = 按原版 **local** 写下去
                    go.transform.localPosition = Vec3(n.localPos, Vector3.zero);
                    go.transform.localRotation = Quat(n.localRot);
                    go.transform.localScale = Vec3(n.localScale, Vector3.one);
                    created[n.path] = go.transform;
                    SceneAnimFxNodesCreated++;
                }
            }

            // ---- ② 组件：**复用 `MakeAnimFx`**（同一个 kind、同一份字段装配）----
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null) continue;
                var t = FirstAnimFxTarget(it);
                if (t == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`):旁挂里没有 `kind == \"animfx\"` 的目标"); continue; }

                // 两个开关**必须**来自旁挂（拿默认值顶数据 = 静默错，本仓红线）——
                // 与 `MakeAnimFx` 里 `preventDestroy` 那条**同一条判据**（哨兵 `-1f` = 键不在，
                // ⚠️ 值域本来就只有 0/1 ⇒ 不会与真值撞上）。
                float en = t.GetF("enabled", -1f);
                float goAct = t.GetF("goActive", -1f);
                if (en < 0f || goAct < 0f)
                {
                    missed.Add($"{it.cls}(`{t.leaf}`):旁挂里没有 `enabled` / `goActive`"
                             + "（原版这 5 个里有 3 个根本不跑 ⇒ 不记下来就不该建）");
                    Debug.LogWarning($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的旁挂里**没有 `enabled` / "
                                   + "`goActive`** —— 这一条不建（拿默认值顶数据 = 静默错）。"
                                   + "跑一次 `python 工具/gen_env_blendables.py` 重生成旁挂（出声，不静默）");
                    continue;
                }
                bool goActive = goAct != 0f;
                // 🔴 `selfDestroyOk` = 「原版那句 `OnEnable` 到底会不会跑」（组件开着 ∧ 宿主 GO 开着）——
                //    传错这一档的后果**不可逆**（`Destroy(go, t)` 排定了取消不掉），见参数注释。
                // 🔴 **第 4 个参数 `false`（2026-10-12 A431 收红那轮）** = **编辑模式不模拟「当场销毁宿主」**：
                //    本条链要的是「组件建出来 + 5 个子件改挂」，而原版那一刻组件**确实还在**（`destroyTime = 6`
                //    是**排定**、不是当场）。原来不传 ⇒ `battlearena2` 的 `RocketTrail`（5 条里唯一
                //    `preventDestroy = 0 ∧ enabled ∧ goActive`）宿主被当场 `DestroyImmediate` ⇒ 回来的 `c`
                //    已是**已销毁**组件（Unity `==` 判 null）⇒ 被记成「`MakeAnimFx` 建不出来（宿主/字段缺）」
                //    ⇒ ③ 改挂被 `continue` 跳过 ⇒ **A431 四条红**。
                //    ⛔ 别把这里改回 `true`；运行时那一档（`Application.isPlaying`）不看这个参数，一个字节没动。
                var c = MakeAnimFx(it, res, en != 0f && goActive, false);
                if (c == null) { missed.Add($"{it.cls}(`{t.leaf}`):`MakeAnimFx` 建不出来（宿主/字段缺）"); continue; }
                c.enabled = en != 0f;
                if (c.gameObject.activeSelf != goActive)
                {
                    c.gameObject.SetActive(goActive);
                    Debug.Log($"[EnvBlend] `AnimFXController`(`{t.leaf}`) 的宿主 GameObject 按原版设成 "
                            + $"`activeSelf = {goActive}`（原版那个 GO 就是关着的）");
                }
                SceneAnimFxBuilt++;

                // ---- ③ 改挂（只有「这颗组件真会销毁宿主」的条目才有，见类注释）----
                if (bg != null && bg.reparent != null)
                {
                    for (int k = 0; k < bg.reparent.Length; k++)
                    {
                        var rp = bg.reparent[k];
                        if (rp == null || string.IsNullOrEmpty(rp.name)) continue;
                        if (Normalize(rp.parent) != Normalize(it.owner)) continue;   // 这条属于别的那颗
                        var child = res.GoOf(new EnvBlendables.Target { leaf = rp.name, pos = rp.pos });
                        if (child == null)
                        { missed.Add($"改挂 `{rp.name}`:在战场树里找不到（原版它挂在 `{rp.parent}` 下）"); continue; }
                        // 🔴 `worldPositionStays: true` —— 世界位姿**逐字不变** ⇒ 这一步不改变任何画面
                        //    （判据同 `ArenaBuilder.ApplyGroupNodes`，那儿的头注写了为什么不能用 `false`）。
                        child.transform.SetParent(c.transform, true);
                        SceneAnimFxReparented++;
                    }
                }
            }

            SceneAnimFxMissed = missed.Count;
            SceneAnimFxMissedWhat = string.Join("、", missed);
            Debug.Log($"[EnvBlend] 场景侧 `AnimFXController`（`{arenaKey}`）：旁挂 {items.Length} 条 → 建出 "
                    + $"{SceneAnimFxBuilt} 个 · 新建节点 {SceneAnimFxNodesCreated} 个 · 改挂 {SceneAnimFxReparented} 个"
                    + (missed.Count > 0 ? $"；**{missed.Count} 条没建出来/没对上**（出声，不静默）：{SceneAnimFxMissedWhat}" : ""));
            _sceneAnimFxRoot = arenaRoot;
            return SceneAnimFxBuilt;
        }

        /// <summary>`nodes[]` 里那条 `parent` 路径 → Transform：
        ///  ① 本表**刚建出来**的节点（键 = 原版整条路径）；
        ///  ② 否则它必须是我们**已建**的对象 —— 走生产那份 `SceneResolver`（**名字 + 最近位置**），
        ///     ⛔ 不另写一套找对象的（`ArenaBuilder.ResolveGroupParent` 是**建场侧**的同一条判据，
        ///     运行时有 `SceneResolver` 就用它）；
        ///  ③ 都不通 ⇒ 挂到场根 + **逐条出声**（判据同 `ArenaBuilder.ResolveGroupParent`）。</summary>
        static Transform ResolveAnimFxParent(Transform arenaRoot, System.Collections.Generic.Dictionary<string, Transform> created,
                                             IEnvTargetResolver res, string path, float[] pos,
                                             System.Collections.Generic.List<string> missed)
        {
            if (string.IsNullOrEmpty(path)) return arenaRoot;
            Transform ct;
            if (created != null && created.TryGetValue(path, out ct) && ct != null) return ct;
            var leaf = path.Substring(path.LastIndexOf('/') + 1);
            var t = res.GoOf(new EnvBlendables.Target { leaf = leaf, pos = pos });
            if (t != null) return t.transform;
            missed.Add($"节点父路径 `{path}` 既不在本表新建的节点里、也不在战场树里（叶子 `{leaf}`）"
                     + " ⇒ 这一条挂到场根下（出声，不静默）");
            return arenaRoot;
        }

        static EnvBlendables.Target FirstAnimFxTarget(EnvBlendables.Item it)
        {
            if (it.targets == null) return null;
            for (int i = 0; i < it.targets.Length; i++)
                if (it.targets[i] != null && it.targets[i].kind == "animfx") return it.targets[i];
            return null;
        }

        static Vector3 Vec3(float[] a, Vector3 dflt)
        { return (a != null && a.Length >= 3) ? new Vector3(a[0], a[1], a[2]) : dflt; }

        static Quaternion Quat(float[] a)
        { return (a != null && a.Length >= 4) ? new Quaternion(a[0], a[1], a[2], a[3]) : Quaternion.identity; }

        static string Normalize(string s) { return EnvironmentApplier.Norm(s); }

        static Renderer[] CollectRenderers(EnvBlendables.Item it, Func<EnvBlendables.Target, Renderer> rd)
        { return CollectTargets(it, "renderer", rd); }

        /// <summary>目标 kind == `renderer` 且旁挂里带 `blendProps` 的（只有 `ScenarioGenericMaterialBlend` 那种）。
        /// `customMaterial` 只当**留档 / 出声用**（原版那个 Material 资产我们工程里没有 —— 见类注释偏离 ②）。</summary>
        static ScenarioGenericMaterialBlend.RendererMaterialBlender[] CollectMaterialBlenders(
            EnvBlendables.Item it, IEnvTargetResolver res)
        {
            if (it.targets == null || res == null) return new ScenarioGenericMaterialBlend.RendererMaterialBlender[0];
            var list = new System.Collections.Generic.List<ScenarioGenericMaterialBlend.RendererMaterialBlender>();
            for (int i = 0; i < it.targets.Length; i++)
            {
                var t = it.targets[i];
                if (t == null || t.kind != "renderer") continue;
                var r = res.RendererOf(t);
                if (r == null)
                {
                    Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）的目标渲染器 `{t.leaf}` 解析不到"
                                   + " —— 这一条补间不会发生（出声，不静默）");
                    continue;
                }
                list.Add(new ScenarioGenericMaterialBlend.RendererMaterialBlender
                {
                    renderer = r,
                    customMaterial = null,                 // ⚠️ 原版那条资产引用我们工程里没有（见类注释偏离 ②）
                    propertiesToBlend = t.blendProps,
                    originalMaterialName = t.customMaterial,
                });
            }
            return list.ToArray();
        }

        /// <summary>GUID → `AnimationClip`：**按原版那条路取**（原版是 `AssetReferenceTyped<AnimationClip>.Load()`，
        /// 手里只有一个 **assetGUID**，`EnvironmentConditions.Item.animationsToChange[].clip` 存的就是它）。
        ///
        /// 🔴 **为什么按 GUID 取、而不是「先查名字再按名字取」**：原版源包的 `AssetBundle.m_Container`
        ///   **键就是 GUID**（实测 `bundle_battleprefabs_vfxandmisc_assets_all` 的 988 条键全是 32 位十六进制：
        ///   `58db0a1f…` → PathID 1230949865609814630 = `LightAnimationOrbit`）⇒ 重打
        ///   `wf_prefabs_extra.bundle` 时**照原样登记一条 GUID 别名**（`extract_missing_shaders.py`
        ///   的 `extra_clip_guids`），这里 `LoadAsset<AnimationClip>(guid)` 与原版同一条路。
        ///   **GUID→名字的映射全仓只有一处** = `数据/游戏数据/animator_controllers.json` 的 `clipsByGuid`
        ///   （那份表同时决定「哪两条 clip 收进包」）—— **这里不抄第二份**（铁律 6：同一份判据别写两遍）。
        ///
        /// 🔴 **取不到一律出声**（不许静默失败）：包不在 / 包加载不出来 / 包里没有这个 GUID 三种情形都会
        ///   点名到 GUID，并给出补救办法。⚠️ **拿到 clip ≠ 会播** —— 那两条原因（差一个 `al` · 宿主缺失）
        ///   见 `ScenarioAnimationBlend` 的类注释，**不是这里能补的**。
        /// 机制与 `WarpforgeVFX.WarpforgeAnimatorBridge` 同源（同一只包·同样的 `LoadFromFile` + 已加载包兜底）。</summary>
        static AnimationClip AnimationClipByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (_clipByGuid.TryGetValue(guid, out var cached)) return cached;

            var clip = ClipFrom(_clipBundle, guid);
            if (clip == null)
            {
                EnsureClipBundle();
                clip = ClipFrom(_clipBundle, guid);
            }
            if (clip == null)
            {
                // 🔴 **同一份内容已在进程里加载过时，`LoadFromFile` 返回 null**（Unity 限制：同一个文件不能加载两次）
                //    —— 自检/工具先把源 bundle 全量加载时必然撞上。与 `WarpforgeAnimatorBridge` 同一条兜底。
                foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (b == null || ReferenceEquals(b, _clipBundle)) continue;
                    clip = ClipFrom(b, guid);
                    if (clip != null)
                    {
                        Debug.Log($"[EnvBlend] 自己的包没加载成，改从**已加载的**包 `{b.name}` 里捡到 clip `{guid}`");
                        break;
                    }
                }
            }
            if (clip == null)
            {
                if (_warnedClip.Add(guid))          // 一个 GUID 只喊一次（环境会被反复切）
                    Debug.LogError($"[EnvBlend] 原版 `animationsToChange` 要的 clip（assetGUID `{guid}`）**取不到** "
                                 + "⇒ 这一条 `CrossFade` 不生效。排查：① 跑一次 "
                                 + "`工具/extract_missing_shaders.py --prefabs`（重打 "
                                 + $"`{WarpforgeVFX.WarpforgeAnimatorBridge.BundleRelPath}`）；"
                                 + "② 包在不在 `StreamingAssets/` 下；③ 表 "
                                 + "`数据/游戏数据/animator_controllers.json` 的 `clipsByGuid` 里有没有这一条"
                                 + "（GUID→名字的映射**只有那一处**）");
                return null;
            }
            _clipByGuid[guid] = clip;
            return clip;
        }

        static AnimationClip ClipFrom(AssetBundle b, string guid)
        {
            if (b == null) return null;
            try
            {
                var c = b.LoadAsset<AnimationClip>(guid);
                if (c != null) return c;
                // 兜底：万一调用方给的其实是**片段名**（不是 GUID），按类型全捞也能命中。
                // ⚠️ 给的是纯 GUID 时这里**恒不命中**（片段的 `name` 是它的名字，不是 GUID）⇒ 返回 null，由上面出声。
                foreach (var x in b.LoadAllAssets<AnimationClip>())
                    if (x != null && x.name == guid) return x;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EnvBlend] 在 `{b.name}` 里取 clip `{guid}` 抛了：{e.GetType().Name}: {e.Message}");
            }
            return null;
        }

        static void EnsureClipBundle()
        {
            if (_clipBundleTried) return;
            _clipBundleTried = true;
            // 包路径**只有一处**（`WarpforgeAnimatorBridge.BundleRelPath`）—— 那只包是两条线共用的，别各写一份。
            var path = System.IO.Path.Combine(Application.streamingAssetsPath,
                                              WarpforgeVFX.WarpforgeAnimatorBridge.BundleRelPath);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[EnvBlend] 找不到 `{path}` —— 先跑一次 "
                               + "`工具/extract_missing_shaders.py --prefabs`（产物的 `.meta`/包体都不进 git）");
                return;
            }
            try { _clipBundle = AssetBundle.LoadFromFile(path); }
            catch (Exception e)
            { Debug.LogWarning($"[EnvBlend] 加载 `{path}` 抛了：{e.GetType().Name}: {e.Message}"); }
            if (_clipBundle == null)
                Debug.LogWarning($"[EnvBlend] `{path}` 加载不出（同一份内容可能已在进程里加载过？）"
                               + " —— 下面会去已加载的包里再找一遍");
        }

        /// <summary>给自检用：清掉缓存，下一问重新加载（与 `WarpforgeAnimatorBridge.Reset` 同一个用途）。</summary>
        public static void ResetClipCache()
        {
            _clipBundle = null; _clipBundleTried = false; _clipByGuid.Clear(); _warnedClip.Clear();
        }

        static AssetBundle _clipBundle;
        static bool _clipBundleTried;
        static readonly System.Collections.Generic.Dictionary<string, AnimationClip> _clipByGuid =
            new System.Collections.Generic.Dictionary<string, AnimationClip>();
        static readonly System.Collections.Generic.HashSet<string> _warnedClip =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>`Create` 传 null 时用的空实现（**只出声、不解**）—— 防手滑传 null 变成 NRE。</summary>
        class NullResolver : IEnvTargetResolver
        {
            public static readonly NullResolver Instance = new NullResolver();
            public ParticleSystem PsOf(EnvBlendables.Target t) { Warn(); return null; }
            public Renderer RendererOf(EnvBlendables.Target t) { Warn(); return null; }
            public GameObject GoOf(EnvBlendables.Target t) { Warn(); return null; }
            public ParticleSystemAreaSpawner SpawnerOf(EnvBlendables.Target t) { Warn(); return null; }
            public ParticleSystemAreaSpawnerController ControllerOf(EnvBlendables.Target t) { Warn(); return null; }
            public LensFlareComponentSRP FlareOf(EnvBlendables.Target t) { Warn(); return null; }
            public Animation AnimationOf(EnvBlendables.Target t) { Warn(); return null; }
            public Transform TransformOf(EnvBlendables.Target t) { Warn(); return null; }
            static void Warn() { Debug.LogWarning("[EnvBlend] 工厂拿到的是空解析器 —— 一个目标都解析不出来（出声，不静默）"); }
        }
    }
}
