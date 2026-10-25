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
    /// <summary>`ScenarioMaterialFader`（旁挂 `by_class = 23`：**env prefab 侧 15 个实例 + 场景侧 8 个**，
    /// 全包扫描 26 = 15 + 8 + `sharedresources` 3。⚠️ 2026-10-25 订正：这里原来写「12/43 个 env prefab」，
    /// 两个数字都不对 —— 43 是 `stats.prefab_roots` 那个口径（另有文档写 42），实例数是 23/26）。
    /// 判据 = `ScenarioMaterialFader__{DoScenarioBlend,CacheOriginalMaterials,Update,RestoreOriginalMaterials,ToggleRenderers}.c`：
    ///   · 第一次调用先 `CacheOriginalMaterials`（记 `renderer.sharedMaterial`）
    ///   · **然后无条件 `foreach r: r.enabled = true`**（2026-10-25 补，见 `DoScenarioBlend` 里缺支 ①）
    ///   · **`fadeOnEnable && direction` ⇒ 先把 α 置 0、targetFade 写 1.0**（2026-10-25 补，缺支 ②）
    ///   · `Update` 里按 `currentBlendTime` 把每个 renderer 的**材质 `color.a`** 往 `targetValue` 推
    ///   · 完成时：`(restoreOriginalMaterialsOnFadeOut &amp;&amp; !direction) || (restoreOriginalMaterialsOnFadeIN &amp;&amp; direction)`
    ///     ⇒ 还原原材质（销毁我们实例化的那份）· `disableOnFadeOut &amp;&amp; !direction` ⇒ `renderer.enabled = false`
    /// ⚠️ **仍未抄的一条**：原版两个分支末尾都有 `doFade = !doFade`（**取反开关**；`.c:96` / `:124`，
    ///   ctor 的 `ScenarioMaterialFader__.ctor.c` 只在 `0x2a/0x2b/0x2c` 写 1 ⇒ `doFade(0x40)` 出厂是 false）
    ///   ⇒ 原版**每调一次就翻转**：首调开始淡、二调把进行中的淡**取消**（`Update` 首句 `if(!doFade) return`）。
    ///   我们没抄 —— 抄它之前必须先确认「每条环境到底调几次 `DoScenarioBlend`」，
    ///   否则会把该淡的那一次取消掉（比现在更糟）。**只记录**（P2，判据已写全）。</summary>
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

        /// <summary>`DoScenarioBlend` 被调的总次数（累计）。**只给「`doFade` 取反开关能不能抄」那次实测用**，
        /// 不参与任何行为（见方法里那段注释）。</summary>
        public static int BlendCalls { get; private set; }
        /// <summary>其中 `direction == true` 的次数。</summary>
        public static int BlendCallsForward { get; private set; }
        public static void ResetBlendCallCounters() { BlendCalls = 0; BlendCallsForward = 0; }

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            // 🆕 2026-10-25（完全复刻第 1 轮 · D 域）**计数探针**：原版两个分支末尾都有
            //    `doFade = !doFade`（取反开关，`ScenarioMaterialFader__DoScenarioBlend.c:96/124`；
            //    ctor 只写 `0x2a/0x2b/0x2c` ⇒ `doFade(0x40)` 出厂 false）⇒ 原版**每调一次就翻转**：
            //    首调开始淡、二调把进行中的淡**取消**（`Update` 首句 `if(!doFade) return`）。
            //    ⛔ 我们**没有**抄它 —— 抄之前必须先知道「每条环境到底调几次 `DoScenarioBlend`」
            //    （我们这边：`AttachBlendables` 每件 1 次 + `_sceneBlendables` 每次 `Apply` 1 次 + 卸载时 1 次，
            //      见 `EnvironmentApplier.cs:216/288`；若同一次变更落在同一件上两次，抄了就会把淡入取消掉）。
            //    ⇒ 这两个计数就是给那次实测用的（自检读增量即可，不改任何行为）。
            BlendCalls++;
            if (o.direction) BlendCallsForward++;   // `ScenarioBlendOptions` 是 struct ⇒ 不能判 null

            if (_orig == null) CacheOriginalMaterials();

            // 🔴 **2026-10-25 补齐（D 域 · 完全复刻第 1 轮）· 缺支 ①**：
            //    原版 `DoScenarioBlend` 在缓存之后、分支之前**无条件**把每个渲染器重新打开
            //    （`decomp_full/ScenarioMaterialFader__DoScenarioBlend.c:78-84`：`for (i<renderers.Length)
            //     { if (renderers[i]==0) goto fail; UnityEngine_Renderer__set_enabled(); }`，
            //     参数在反汇编里被省了，但这一句在下标循环里、且与 `disableOnFadeOut` 对称 ⇒ 就是置 1）。
            //    原来我们**没有这一句** ⇒ `disableOnFadeOut` 把渲染器关掉之后**再也回不来**：
            //    55 条环境里 38 条 `defaultScenarioObjectsState=0` ⇒ 打任何一张环境卡都会关掉
            //    aeldari `Dynamic Lights 4/7` / darkangels `Ship 1..4` / leviathan `Toxic Pool Glow`×2
            //    这批**场景常驻**道具，撤环境时我们不会重新打开 ⇒ **整局不再出现**。
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = true;

            // 🔴 **缺支 ②**：`fadeOnEnable && direction` 时原版先把 `material.color` 的 **α 置 0**、
            //    再把 `targetFade` 写 **1.0**（`.c:86-119`：`local_48..uStack_40 = color.rgb; uStack_3c = 0;`
            //    后 `*(param_1+0x44) = 0x3f800000`）。
            //    原来我们从「当前 α」（≈1）起 ⇒ 12/23 个 `fadeOnEnable=1` 的实例**丢了淡入**（直接出现）。
            if (fadeOnEnable && o.direction)
            {
                SetAlpha(0f);
                _fromA = 0f;
                _toA = 1f;                       // 原版 `targetFade = 1.0f`（**不看** `targetValue`）
            }
            else
            {
                _fromA = CurrentAlpha();
                _toA = o.targetValue;
            }
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
            SetAlpha(Mathf.Lerp(_fromA, _toA, t));
        }

        /// <summary>把每个渲染器的**私有实例材质**的 `color.a` 写成 `a`（r/g/b 原样）。
        /// 实例材质那份缓存的来由见 `_inst` 的注释（⛔ 别改回 `renderer.material` /
        /// ⛔ 更别直接写 `sharedMaterial`）。</summary>
        void SetAlpha(float a)
        {
            if (renderers == null) return;
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
