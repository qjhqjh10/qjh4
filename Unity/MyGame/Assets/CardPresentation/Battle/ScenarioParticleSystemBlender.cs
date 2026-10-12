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
}
