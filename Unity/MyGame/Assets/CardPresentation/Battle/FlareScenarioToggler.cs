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
    /// <summary>`FlareScenarioToggler`（原版 **6 个实例、全在场景侧**，每个都挂在 `Sun flare` 上）。
    /// 判据 = `FlareScenarioToggler__{DoScenarioBlend,Initialize,OnValidate}.c` ＋ 两个闭包
    /// `…__&lt;DoScenarioBlend>b__1_{0,1}.c` ＋ 反汇编 `RVA 0x625730`（`DOTween.To` 的两个实参：
    /// `targetValue` 来自 `options+0x20`、`duration` 来自 `options+0x10`）：
    ///   · `DoScenarioBlend(o)`：`DOTween.To(() => lensFlare.intensity, x => lensFlare.intensity = x,
    ///     o.ScenarioBlendTargetValue, o.ScenarioBlendTime)` —— **只对 `intensity` 补间**，
    ///     返回的 tween 被丢掉 ⇒ **不回调 `OnComplete`、不理 `onComplete`**。
    ///   · `Initialize()` ⇒ `ScenarioEnvironmentConditionsManager.Register(this)`（= 场景侧那批；我们由执行器建一次）。
    ///   · `OnValidate()` ⇒ 没填就 `GetComponent&lt;LensFlareComponentSRP>()`。
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
}
