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
// ⚠️ **还没复刻的（如实记着，别当成已做）**：原版 `IScenarioEnvironmentBlendeable` 一共 **9 个实现类**，
//   我们只做 5 个。另 4 个（`FlareScenarioToggler` 6 个实例 · `ScenarioAnimationBlend` 2 ·
//   `ScenarioGenericMaterialBlend` 1 · `TauCannonAnimationStopper` 2，**共 11 个**）**不在旁挂里**
//   —— 它们**从来没被 `gen_env_blendables.py` 的 `CLASSES` 收过**（不是「收了没接」）。
//   判据 → `资料/普查产出_1006/战A_第3_4条.md`（逐类逐个实例的出处）。
//
// 🔴 **批处理下没有帧循环**（CLAUDE.md §三）⇒ 每个组件的推进都做成**可手动 `Advance(dt)`**，
//    `Update()` 只是实时那条路；自检一律手动推（与 `EnvironmentApplier.Advance` 同一套）。
using System;
using UnityEngine;
using UnityEngine.Pool;      // 原版 `ParticleSystemAreaSpawner` 的池子就是 `UnityEngine.Pool.ObjectPool<ParticleSystem>`
                            // （判据 = 反汇编里那句 `new ObjectPool<ParticleSystem>(create, onGet, onRelease, onDestroy, …)`）

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
    ///   （`filterCode = "SumpOverspill"` · `filterEnabled = 1`）。
    ///   ⚠️ **我们还没接这一半**：`ScenarioGenericMaterialBlend` 这一类**连旁挂都没收**
    ///   （见文件头那段 ⚠️），而且 SO 的 FilterCode 要进运行时得动
    ///   `Resources/EnvironmentConditions.json` + `工具/gen_environment_conditions.py`（本批不在可碰名单）。
    ///   完整裁定与待办 → `资料/普查产出_1006/战A_第3_4条.md`「第 4 条」。</summary>
    public struct ScenarioBlendOptions
    {
        public float duration;      // `SO.blendTime`（`instant` 时为 0）
        public bool direction;      // true = 淡入/开（新环境）· false = 淡出/关（撤环境）
        public float targetValue;   // 原版与 direction 同源：true→1.0 / false→0.0
        public Action onComplete;   // 撤环境那条链靠它计数，**全部到齐才 Destroy**
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

    /// <summary>按旁挂那条 `cls` 建组件、把目标填进去（两个调用点：环境 prefab 实例内 · 战场场景侧）。
    /// `resolve` = 把旁挂里的一个 `Target` 翻成对象（prefab 侧按**路径**、场景侧按**名字+最近位置**，
    /// 由 `EnvironmentApplier` 提供 —— 两条路的判据不同，别在这里混成一套）。</summary>
    public static class ScenarioBlendableFactory
    {
        public static ScenarioBlendable Create(EnvBlendables.Item it, GameObject host,
                                               Func<EnvBlendables.Target, ParticleSystem> ps,
                                               Func<EnvBlendables.Target, Renderer> rd,
                                               Func<EnvBlendables.Target, GameObject> go,
                                               Func<EnvBlendables.Target, ParticleSystemAreaSpawner> sp,
                                               Func<EnvBlendables.Target, ParticleSystemAreaSpawnerController> ct,
                                               bool verbose)
        {
            if (it == null || host == null) return null;
            ScenarioBlendable c = null;
            switch (it.cls)
            {
                case "ScenarioParticleSystemToggler":
                {
                    var t = host.AddComponent<ScenarioParticleSystemToggler>();
                    t.particleSystems = CollectPS(it, ps);
                    c = t; break;
                }
                case "ScenarioParticleSystemBlender":
                {
                    var b = host.AddComponent<ScenarioParticleSystemBlender>();
                    b.particleSystems = CollectPS(it, ps);
                    b.forceEnableEmittersOnEnable = it.GetBool("forceEnableEmittersOnEnable");
                    b.notifyFinishWhenNoParticles = it.GetBool("notifyFinishWhenNoParticles");
                    c = b; break;
                }
                case "ScenarioMaterialFader":
                {
                    var f = host.AddComponent<ScenarioMaterialFader>();
                    f.renderers = CollectRenderers(it, rd);
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
                    g.gameObjects = CollectGO(it, go);
                    g.isInDefaultScenario = it.GetBool("isInDefaultScenario");
                    c = g; break;
                }
                case "ScenarioParticleSpawnerBlender":
                {
                    // 🆕 2026-10-06：4 个实例。两个数组各自是**目标对象上的组件**
                    //（`sp` / `ct` 两个解析器负责「按旁挂路径找到那个对象、把组件挂上、把 6 个字段填进去」）。
                    var s = host.AddComponent<ScenarioParticleSpawnerBlender>();
                    s.controllers = CollectControllers(it, ct);
                    s.areaSpawners = CollectSpawners(it, sp);
                    if (verbose && s.areaSpawners.Length == 0 && s.controllers.Length == 0)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}`（owner=`{it.owner}`）旁挂里一个目标都没解析出来"
                                       + " —— 这一条不会生成任何粒子（出声，不静默）");
                    c = s; break;
                }
                default:
                    // ⚠️ **不许静默**：没实现的类要出声
                    //（实测还有 4 个实现类不在这张表里：`FlareScenarioToggler` / `ScenarioAnimationBlend` /
                    //  `ScenarioGenericMaterialBlend` / `TauCannonAnimationStopper` —— 它们**连旁挂都没收**，
                    //  见本文件头部那段 ⚠️）
                    if (verbose)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}` 还没复刻 —— 这一条不生效。owner=`{it.owner}`"
                                       + " · 判据 → 项目任务.md §三 第 30 条");
                    return null;
            }
            return c;
        }

        static ParticleSystem[] CollectPS(EnvBlendables.Item it, Func<EnvBlendables.Target, ParticleSystem> ps)
        {
            if (it.targets == null || ps == null) return new ParticleSystem[0];
            int n = 0;
            var tmp = new ParticleSystem[it.targets.Length];
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != "ps") continue;
                var v = ps(it.targets[i]);
                if (v != null) tmp[n++] = v;
            }
            var outp = new ParticleSystem[n];
            Array.Copy(tmp, outp, n);
            return outp;
        }

        static Renderer[] CollectRenderers(EnvBlendables.Item it, Func<EnvBlendables.Target, Renderer> rd)
        {
            if (it.targets == null || rd == null) return new Renderer[0];
            int n = 0;
            var tmp = new Renderer[it.targets.Length];
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != "renderer") continue;
                var v = rd(it.targets[i]);
                if (v != null) tmp[n++] = v;
            }
            var outp = new Renderer[n];
            Array.Copy(tmp, outp, n);
            return outp;
        }

        static GameObject[] CollectGO(EnvBlendables.Item it, Func<EnvBlendables.Target, GameObject> go)
        {
            if (it.targets == null || go == null) return new GameObject[0];
            int n = 0;
            var tmp = new GameObject[it.targets.Length];
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != "go") continue;
                var v = go(it.targets[i]);
                if (v != null) tmp[n++] = v;
            }
            var outp = new GameObject[n];
            Array.Copy(tmp, outp, n);
            return outp;
        }

        /// <summary>目标 kind = `spawner`（`ParticleSystemAreaSpawner`）。解析器负责**建组件 + 填那 6 个字段**。</summary>
        static ParticleSystemAreaSpawner[] CollectSpawners(EnvBlendables.Item it,
                                                           Func<EnvBlendables.Target, ParticleSystemAreaSpawner> sp)
        {
            if (it.targets == null || sp == null) return new ParticleSystemAreaSpawner[0];
            int n = 0;
            var tmp = new ParticleSystemAreaSpawner[it.targets.Length];
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != "spawner") continue;
                var v = sp(it.targets[i]);
                if (v != null) tmp[n++] = v;
            }
            var outp = new ParticleSystemAreaSpawner[n];
            Array.Copy(tmp, outp, n);
            return outp;
        }

        /// <summary>目标 kind = `controller`（`ParticleSystemAreaSpawnerController`）。**实测 0 个实例**（见类注释）。</summary>
        static ParticleSystemAreaSpawnerController[] CollectControllers(
            EnvBlendables.Item it, Func<EnvBlendables.Target, ParticleSystemAreaSpawnerController> ct)
        {
            if (it.targets == null || ct == null) return new ParticleSystemAreaSpawnerController[0];
            int n = 0;
            var tmp = new ParticleSystemAreaSpawnerController[it.targets.Length];
            for (int i = 0; i < it.targets.Length; i++)
            {
                if (it.targets[i] == null || it.targets[i].kind != "controller") continue;
                var v = ct(it.targets[i]);
                if (v != null) tmp[n++] = v;
            }
            var outp = new ParticleSystemAreaSpawnerController[n];
            Array.Copy(tmp, outp, n);
            return outp;
        }
    }
}
