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
    /// <summary>`ScenarioParticleSpawnerBlender`（4 个实例 · 每个只引 1 条 `ParticleSystemAreaSpawner`、`controllers` 全是空）。
    /// 判据 = `ScenarioParticleSpawnerBlender__{DoScenarioBlend,Update,IsAnyParticleSystemActiveInControllers,IsAnyParticleSystemActiveInAreas}.c`：
    ///   · `DoScenarioBlend(o)`：记下 `o` → 对**每个 controller** 调 `Toggle(o.ScenarioBlendDirection)`
    ///     → 对**每个 areaSpawner** 调 `Toggle(o.ScenarioBlendDirection)`。**不等 duration、当场生效、不回调**。
    ///   · `Update()`：`if (o != null &amp;&amp; !o.ScenarioBlendDirection &amp;&amp; !IsAnyParticleSystemActiveInControllers()
    ///     &amp;&amp; !IsAnyParticleSystemActiveInAreas()) o.OnComplete?.Invoke();`
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
}
