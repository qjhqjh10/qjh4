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
    /// ✅ **2026-10-27（`B9_E2UDW` · 施工单 362/367）：`doFade` 取反开关已抄上** —— 原版两个分支末尾各有一次
    ///   `doFade = !doFade`（`ScenarioMaterialFader__DoScenarioBlend.c:78` / `:110`；ctor 不写 `0x40` ⇒ 出厂 false），
    ///   `Update` 首句 `if(!doFade) return;`（`ScenarioMaterialFader__Update.c:15`）⇒ 原版**每调一次就翻转**：
    ///   首调开始淡、二调把进行中的淡**冻住**。我们同形实现（字段 `_doFade` / `Advance` 首句闸）。
    ///   🔴 **仍需实测**：若同一次环境变更把 `DoScenarioBlend` 落在同一件上**两次**，这次翻转会把该淡的那次冻掉
    ///   —— 那时要改的是**调用方次数**（`EnvironmentApplier.cs:216/288`），⛔ 不是把开关撤掉。读数口 =
    ///   `BlendCalls` / `BlendCallsForward`（本类静态计数）。</summary>
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

        /// <summary>原版 `doFade`（`+0x40`）—— **取反开关**，ctor 不写它 ⇒ 出厂 `false`。
        /// <para>判据（🥇 `decomp_full/ScenarioMaterialFader__DoScenarioBlend.c:78` 与 `:110` 两处：
        /// `*(bool *)(this+0x40) = *(char *)(this+0x40) == '\0';`；`ScenarioMaterialFader__Update.c:15`
        /// 首句 `if (*(char *)(this+0x40) == '\0') return;`）⇒ **每调一次 `DoScenarioBlend` 就翻转**：
        /// 首调开始淡、二调把进行中的淡**冻住**（`Advance` 首句直接 return，不推进也不结算）。</para></summary>
        bool _doFade;

        /// <summary>`DoScenarioBlend` 被调的总次数（累计）。**只给「`doFade` 取反开关能不能抄」那次实测用**，
        /// 不参与任何行为（见方法里那段注释）。</summary>
        public static int BlendCalls { get; private set; }
        /// <summary>其中 `direction == true` 的次数。</summary>
        public static int BlendCallsForward { get; private set; }
        public static void ResetBlendCallCounters() { BlendCalls = 0; BlendCallsForward = 0; }

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            // 计数探针（2026-10-25 起）：见下面 `_doFade` 那段 —— 抄完 `doFade` 开关之后，这两个计数
            // 用来**实测**「每条环境一次变更到底调几次」，不是行为的一部分。
            // 🆕 2026-10-27（`B9_E2UDW` · 施工单 362/367）：**原版那两句已经抄上了** ——
            //    ① `doFade = !doFade`：两个分支末尾各一次（`ScenarioMaterialFader__DoScenarioBlend.c:78`（`fadeOnEnable && direction` 支）
            //       与 `:110`（另一支），两处都是 `*(bool*)(this+0x40) = *(char*)(this+0x40) == '\0'`）
            //       ⇒ 原版**每调一次就翻转**：首调开始淡、二调把进行中的淡**冻住**；
            //    ② `Update` 首句 `if(!doFade) return;`（`ScenarioMaterialFader__Update.c:15`）⇒ 我们 `Advance` 首句同形。
            //    🔴 **抄完必须读数**：原版「每条环境一次变更 = **1 次** `DoScenarioBlend`」。我们这边
            //    `AttachBlendables` 每件 1 次 + `_sceneBlendables` 每次 `Apply` 1 次 + 卸载时 1 次
            //    （`EnvironmentApplier.cs:216/288`）⇒ 若**同一次变更落在同一件上两次**，翻转会把该淡的那次冻掉。
            //    那时要修的是**调用方次数**（⛔ 不是把开关撤掉）。下面的 `BlendCalls/BlendCallsForward` 就是给这次读数用的。
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
            // ① `doFade = !doFade`（原版两支末尾各一次：`.c:78` 与 `:110`）—— **必须在 `_left = o.duration` 之前**，
            //    否则本帧就会先进 `Advance` 再被冻住（原版是先写 `0x40` 再写 `0x44/0x48/0x4c`）。
            _doFade = !_doFade;
            _opts = o;
            _busy = true;
            if (o.duration <= 0f) { ApplyFade(1f); Complete(); return; }
            _left = o.duration;
        }

        public override void Advance(float dt)
        {
            // ② 原版 `Update` 首句 `if(!doFade) return;`（`ScenarioMaterialFader__Update.c:15`）—— 被冻住的那一次**既不推进 α、也不结算**（`disableOnFadeOut` / 还原材质都不会发生）。🔴 **下面三行 = 原版 `Update` 的次序**：① 每帧**先**把材质 `color` 写回（`:19-29` 的 `GetMaterial → get_color → set_color` 循环；α 用**递减前**的剩余时间）② 再判完成（`:31` `if (blendTime <= 0)`，到点才走 `OnComplete` / 还原材质 / `disableOnFadeOut`）③ **末尾**才 `blendTime -= dt`（`:75` / `:79` `LAB_180630d1a`）。⚠️ 改前是「先退计时、再 `if (_busy)` 写」⇒ **收尾那一帧 α 一次都没写**（`Complete()` 已把 `_busy` 清掉）⇒ 最后一个 α 停在前一帧的中间值上、永远到不了 `_toA`（2026-10-29 `B99_W3` · 施工单 特效 `#14`）。
            if (!_doFade) return;
            if (!_busy) return;
            ApplyFade(_opts.duration <= 0f ? 1f : Mathf.Clamp01(1f - _left / _opts.duration));   // ① 先写（用**递减前**的 `_left` ⇒ 收尾帧 t=1）
            if (_left <= 0f) Complete();                                                        // ② 再判完成（原版 `:31`）
            _left -= dt;                                                                         // ③ 末尾才递减（原版 `:75` / `:79`）
        }

        void ApplyFade(float t)
        {
            SetAlpha(Mathf.MoveTowards(_fromA, _toA, t));   // 原版是 `MoveTowards(a, targetFade, dt/blendTime)`（**恒定速率**），不是 Lerp
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
            base.Complete(); _doFade = false;   // 🔴 2026-10-27（`B9_W17FX2`）：**原版完成时把 `doFade` 归零**（`ScenarioMaterialFader__Update.c:75`，在 `OnComplete` 回调与清 options 之后）—— 漏这一句 ⇒ 淡完一次之后 `_doFade` 恒为 true，**下一次 `DoScenarioBlend` 的取反会把它翻成 false ⇒ 那次淡被整条冻住**（`Advance` 首句 return，`OnComplete` 永不触发）。
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
