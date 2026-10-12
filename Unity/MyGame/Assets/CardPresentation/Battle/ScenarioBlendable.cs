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
}
