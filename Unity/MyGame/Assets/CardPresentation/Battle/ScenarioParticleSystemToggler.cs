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
}
