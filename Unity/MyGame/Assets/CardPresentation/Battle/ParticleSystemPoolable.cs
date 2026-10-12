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
    /// <summary>`ParticleSystemPoolable`（TypeDefIndex 1108）—— 池子的**回收那一跳**。
    /// 判据 = `ParticleSystemPoolable__{Start,OnParticleSystemStopped,OnValidate,AssignParticleSystemReference}.c`
    /// （`SetCallbackOnStop` 的方法体与 `Start` 逐字相同 —— 反编译件里两份内容一样）：
    ///   `main = myParticleSystem.main; main.stopAction = Callback;`（反汇编里那句 `set_stopAction(&amp;main, 3)`；
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
}
