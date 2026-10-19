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
    /// <summary>`ScenarioMaterialFader`（12/43 个 env prefab · 13 场场景侧若干）。**唯一真会「淡」的那个**。
    /// 判据 = `ScenarioMaterialFader__{DoScenarioBlend,CacheOriginalMaterials,Update,RestoreOriginalMaterials,ToggleRenderers}.c`：
    ///   · 第一次调用先 `CacheOriginalMaterials`（记 `renderer.sharedMaterial`）
    ///   · `Update` 里按 `currentBlendTime` 把每个 renderer 的**材质 `color.a`** 往 `targetValue` 推
    ///   · 完成时：`(restoreOriginalMaterialsOnFadeOut &amp;&amp; !direction) || (restoreOriginalMaterialsOnFadeIN &amp;&amp; direction)`
    ///     ⇒ 还原原材质（销毁我们实例化的那份）· `disableOnFadeOut &amp;&amp; !direction` ⇒ `renderer.enabled = false`
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
        /// <summary>🆕 **2026-10-19（`A1967` 残尾 · SC-4）**：每个渲染器**私有**的那份补间材质
        /// （`new Material(sharedMaterial)` 之后赋回 `sharedMaterial`）。
        /// ⛔ 原版/我们原来都走 `renderer.material` —— 编辑模式下每调一次就实例化一份并打
        /// `Instantiating material due to calling renderer.material during edit mode …`；
        /// ⛔ 直接写 `sharedMaterial` 又会把 **prefab 上的资产**改脏（`_orig` 正是为了还原它）。
        /// 缓存起来 = 只复制一次、逐帧复用（`.material` getter 也是同样的缓存语义）。</summary>
        Material[] _inst;
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
            if (_inst == null || _inst.Length != renderers.Length) _inst = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                // 🔴 **2026-10-19（`A1967` 残尾 · SC-4）**：原来这里是 `r.material` ——
                //    编辑模式下 getter 会实例化一份并打 `Instantiating material due to calling
                //    renderer.material during edit mode …`。改成显式 `new Material(sharedMaterial)`
                //    并赋回：语义与 `.material` 逐字等价（一份逐渲染器私有实例），但不触发警告；
                //    同时**绝不能直接写 `sharedMaterial`** —— 那是 prefab 上的资产，`_orig` 要还原它。
                //    `_inst[i]` 缓存住，逐帧复用（否则每帧都复制一份，材质数量会涨）。
                var src = r.sharedMaterial;
                if (src == null) continue;
                var m = _inst[i];
                if (m == null || m != src)
                {
                    m = new Material(src) { name = src.name + " (ScenarioMaterialFader fade)" };
                    _inst[i] = m;
                    r.sharedMaterial = m;
                }
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
                // 🆕 2026-10-19（SC-4）：实例已销毁 ⇒ 清掉缓存，下一次淡入重新复制一份。
                if (_inst != null && i < _inst.Length) _inst[i] = null;
                if (_orig[i] != null) r.sharedMaterial = _orig[i];
            }
        }
    }
}
