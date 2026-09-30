// ScenarioBlendables.cs — 原版那族「环境混合组件」（`IScenarioEnvironmentBlendeable`）的复刻
//
// 判据 = **反编译方法体逐句**（`d:/2/tools/decomp_full/Scenario*__*.c`），不是按类名猜。
// 实测挂载数（`数据/游戏数据/env_blendables.json` 的 `by_class`，两边合计 128 条）：
//   `ScenarioParticleSystemBlender` 62 · `ScenarioParticleSystemToggler` 36 · `ScenarioMaterialFader` 23
//   `ScenarioParticleSpawnerBlender`  4 · `ScenarioGenericObjectToggler`   3
//
// ⚠️ **本文件只做前 4 个**：`ScenarioParticleSpawnerBlender` 要调原版的
//   `ParticleSystemAreaSpawner` / `ParticleSystemAreaSpawnerController`（`Toggle(...)`），
//   **那两个类我们工程里没有对应物**（我们的 prefab 是重导的，只带粒子/网格）⇒ **另立一条**，
//   不在这里糊一个「看着像」的实现（铁律：不许静默失败）。
//
// 🔴 **批处理下没有帧循环**（CLAUDE.md §三）⇒ 每个组件的推进都做成**可手动 `Advance(dt)`**，
//    `Update()` 只是实时那条路；自检一律手动推（与 `EnvironmentApplier.Advance` 同一套）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ScenarioBlendOptions`（字段名照原版，别改写）：
    ///   `ScenarioBlendTime`(+0x10) · `ScenarioBlendDirection`(+0x24) · `ScenarioBlendTargetValue`(+0x20) ·
    ///   `OnComplete`(+0x18) · `FilterOptions`(+0x28)。
    /// ⚠️ `FilterOptions` **我们不复刻**：实测 55 条 SO 里 **54 条是空的**
    ///   （唯一例外 = GSC 的 `Sump Overspill`）⇒ 「按 filter 分组」在这 13 场里本来就是恒等。</summary>
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

    /// <summary>按旁挂那条 `cls` 建组件、把目标填进去（两个调用点：环境 prefab 实例内 · 战场场景侧）。
    /// `resolve` = 把旁挂里的一个 `Target` 翻成对象（prefab 侧按**路径**、场景侧按**名字+最近位置**，
    /// 由 `EnvironmentApplier` 提供 —— 两条路的判据不同，别在这里混成一套）。</summary>
    public static class ScenarioBlendableFactory
    {
        public static ScenarioBlendable Create(EnvBlendables.Item it, GameObject host,
                                               Func<EnvBlendables.Target, ParticleSystem> ps,
                                               Func<EnvBlendables.Target, Renderer> rd,
                                               Func<EnvBlendables.Target, GameObject> go,
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
                default:
                    // ⚠️ **不许静默**：没实现的类要出声（`ScenarioParticleSpawnerBlender` 走这条）
                    if (verbose)
                        Debug.LogWarning($"[EnvBlend] `{it.cls}` 还没复刻（要原版的 ParticleSystemAreaSpawner*）"
                                       + $" —— 这一条不生效。owner=`{it.owner}` · 判据 → 项目任务.md §三 第 30 条");
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
    }
}
