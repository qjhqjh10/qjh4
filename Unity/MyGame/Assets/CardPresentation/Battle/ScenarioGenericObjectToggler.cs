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
}
