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
    /// <summary>`ScenarioGenericMaterialBlend`（原版 **1 个实例**：GSC 场 `…/Battle Arena Genestealers Baked/Floor`）。
    /// —— 也是 `ScenarioBlendOptions.FilterOptions` 的**唯一消费方**（A135）。
    /// 判据 = `ScenarioGenericMaterialBlend__{DoScenarioBlend,Start,Update,ToggleRenderers,SetTargetFade}.c`
    ///   ＋ `…RendererMaterialBlender__{Init,DoBlend,ToggleRenderer}.c` ＋ **反汇编 RVA 0x62FB70（DoScenarioBlend）
    ///   与 0x62FF50（Update）**（把 `.c` 印丢的分支逐条定了）：
    /// ```
    /// DoScenarioBlend(o):
    ///   if (!IsNullOrEmpty(this.filterCode)) {                 // +0x30
    ///       if (o == null || o.FilterOptions == null) 抛 NRE;
    ///       if (this.filterCode != o.FilterOptions.FilterCode) return;     // ★ 不匹配 ⇒ 整条跳过
    ///   }
    ///   current = new Options(o) { direction=o.direction, targetValue=o.direction?1:0, duration, onComplete, filterOptions }
    ///   if (current.FilterOptions == null) 抛 NRE;             // ★ 与 filterCode 空不空**无关**（反汇编 0x62E695）
    ///   if (!IsNullOrEmpty(current.FilterOptions.FilterCode)) {
    ///       current.direction   = current.FilterOptions.isEnabled;          // ★ 命中后【覆盖】
    ///       current.targetValue = current.FilterOptions.isEnabled ? 1 : 0;
    ///   }
    ///   foreach (r in renderersBlend) r.renderer.enabled = true;
    ///   if (fadeOnEnable &amp;&amp; current.direction) {
    ///       foreach (r in renderersBlend) r.Init(0f);
    ///       doFade = !doFade;  blendTime = currentBlendTime = current.duration;  targetFade = 1f;  return;
    ///   }
    ///   blendTime = currentBlendTime = o.duration;  doFade = !doFade;  targetFade = current.targetValue;
    /// Update():                                                 // 我们 = Advance(dt)
    ///   if (!doFade) return;
    ///   foreach (r in renderersBlend) { if (r.customMaterial == null) 跳过; if (!r.isInitialized) r.Init(0f);
    ///       foreach (p in r.propertiesToBlend) { cur = r.renderer.material.GetFloat(p);
    ///           r.renderer.material.SetFloat(p, Mathf.MoveTowards(cur, targetFade, Time.deltaTime / blendTime)); } }
    ///   if (currentBlendTime &lt;= 0) { OnComplete?.Invoke(); currentOptions = null; doFade = false; }
    ///   currentBlendTime -= Time.deltaTime;
    /// ```
    /// 🔴 **`doFade = !doFade` 是原版真的这么写**（反汇编 `0x62E794` / `0x62E7F3` 两处都是
    ///   `sete al; mov [this+0x40], al`）—— **照抄，不「修正」**。
    /// ⚠️ **两处落地偏离（都在这儿写清）**：
    ///   ① 原版那三个 `RendererMaterialBlender.Init(...)` 的实参在**三处调用点全是 `0f`**
    ///      （反汇编：`DoBlend` `0x62C877 xorps xmm1,xmm1` · `Update` `0x62EA37 movaps xmm1,xmm8`（=0）·
    ///       `DoScenarioBlend` `0x62E784`）—— `dump.cs` 记的默认值 `1` **一次都没被用到**，我们照 `0f` 落地。
    ///   ② 原版 `RendererMaterialBlender.Init` 会 `renderer.material = customMaterial`（换成那个 Material 资产）。
    ///      **我们工程里没有那个 Material**（原版叫 `Battle Arena Genestealers Water Floor`，是 bundle 里的
    ///      Material 资产；我们的 arena 材质是 `ArenaBuilder` 按清单 `props` **现建**的、
    ///      `_Blend` 本来就在上面）⇒ **不换材质**，直接改那件渲染器现有材质上的属性；
    ///      属性不存在就 `LogError` 出声（**不静默**）。这是一处**必要**偏离（没资产可换），如实记着。</summary>
    public class ScenarioGenericMaterialBlend : ScenarioBlendable
    {
        /// <summary>原版嵌套类 `ScenarioGenericMaterialBlend.RendererMaterialBlender`（TypeDefIndex 751）：
        /// `renderer`(+0x10) · `customMaterial`(+0x18) · `propertiesToBlend`(+0x20) · `isInitialized`(+0x28 运行时)。</summary>
        [Serializable]
        public class RendererMaterialBlender
        {
            public Renderer renderer;
            /// <summary>原版那条 Material 引用。⚠️ 我们工程里没有这个资产 ⇒ 恒 null（见类注释偏离 ②）。</summary>
            public Material customMaterial;
            public string[] propertiesToBlend;
            /// <summary>🆕 原版那个 Material 的**资产名**（旁挂 `Target.customMaterial` 给的值；只留档 / 出声用）。</summary>
            public string originalMaterialName;

            bool _isInitialized;
            bool _warned;
            /// <summary>🆕 **2026-10-19（`A1967` 残尾 · SC-4）**：这个渲染器**私有**的补间材质实例。
            /// <para>⚠️ 原版 `Update` 走 `renderer.material.GetFloat/SetFloat` —— 编辑模式下
            /// `.material` getter 会实例化一份并打 `Instantiating material due to calling
            /// renderer.material during edit mode …`。这里显式 `new Material(sharedMaterial)` 并赋回
            /// `sharedMaterial`（语义逐字等价 = 一份逐渲染器私有实例），同时缓存住**只复制一次**
            /// （`.material` getter 也是同样的缓存语义，逐帧再复制会让材质数量一直涨）。</para>
            /// <para>⛔ **不能直接写 `sharedMaterial`**：那会把 arena 的共享材质改脏（`customMaterial == null`
            /// 时我们改的就是场景里那件渲染器的现有材质）。</para></summary>
            Material _inst;

            /// <summary>判据 = `…RendererMaterialBlender__Init.c` + 反汇编 RVA 0x62C950：
            /// `if (customMaterial != null) { renderer.material = customMaterial; foreach (p) customMaterial.SetFloat(p, startValue); }`
            /// ⇒ **`customMaterial == null` 时它什么都不做、只把 `isInitialized` 置真**（原版如此）。
            /// 我们 `customMaterial` 恒 null ⇒ 走「什么都不做」那一支是**忠实**的，但整套就没效果了
            /// ⇒ **改在现有材质上写**（见类注释偏离 ②），属性不存在时出声。</summary>
            public void Init(float startValue)
            {
                if (customMaterial != null && renderer != null)
                {
                    // 原版是 `renderer.material = customMaterial`；这里赋给 `sharedMaterial` ——
                    // 语义就是「换成这件 Material 资产」。
                    // 🔴 **2026-10-19 更正（原文→实际）**：原来写「`.material` setter 在编辑模式下同样会
                    //   触发实例化警告（`A1967` 同族）」—— **不成立**。SC-5 用 IL 现核
                    //   `UnityEngine.CoreModule.dll`：`set_material` 与 `set_sharedMaterial` **都只调
                    //   `SetMaterial`**，实例化警告只在 **getter**（`get_material`）里。
                    //   判据 → `资料/普查产出_1019/W57_自检线_VAT材质残留.md` §二。
                    // ⚠️ 本工程 `customMaterial` 恒 null（见类注释偏离 ②），
                    // 这一支实际不跑；保留它是为了**语义完整**，不是死代码清理对象。
                    renderer.sharedMaterial = customMaterial;
                    _inst = customMaterial;
                    for (int i = 0; i < (propertiesToBlend != null ? propertiesToBlend.Length : 0); i++)
                        customMaterial.SetFloat(propertiesToBlend[i], startValue);
                }
                _isInitialized = true;
            }

            /// <summary>把 `propertiesToBlend` 里每个属性往 `target` 推（`instant` 时直接写）。</summary>
            public void DoBlend(float target, float blendSpeed, bool instant = false)
            {
                if (!_isInitialized) Init(0f);
                if (renderer == null || propertiesToBlend == null) return;
                // 🔴 **2026-10-19（`A1967` 残尾 · SC-4）**：原来这里是 `renderer.material` ——
                //    编辑模式下 getter 会实例化一份并打 `Instantiating material due to calling
                //    renderer.material during edit mode …`（`A1967` 残留的生产源之一）。
                //    改成显式复制一份私有实例并赋回 `sharedMaterial`（`.material` 的等价语义）；
                //    ⛔ 不直接写 `sharedMaterial`：那是 arena 的共享材质，改了会污染别的场景实例。
                var src = renderer.sharedMaterial;
                if (src == null) return;
                var m = _inst;
                if (m == null || m != src)
                {
                    m = new Material(src) { name = src.name + " (EnvBlend)" };
                    _inst = m;
                    renderer.sharedMaterial = m;
                }
                for (int i = 0; i < propertiesToBlend.Length; i++)
                {
                    string p = propertiesToBlend[i];
                    if (!m.HasProperty(p))
                    {
                        if (!_warned)
                        {
                            _warned = true;
                            Debug.LogError($"[EnvBlend] 渲染器 `{renderer.name}` 的材质 `{m.name}` 上没有属性 `{p}`"
                                         + $"（原版那件 Material 叫 `{originalMaterialName}`）⇒ 这一条补间写不进去"
                                         + "（出声，不静默）");
                        }
                        continue;
                    }
                    float nv = instant ? target : Mathf.MoveTowards(m.GetFloat(p), target, blendSpeed);
                    m.SetFloat(p, nv);
                }
            }

            /// <summary>判据 = `…RendererMaterialBlender__ToggleRenderer.c`：`renderer.enabled = option`。</summary>
            public void ToggleRenderer(bool option) { if (renderer != null) renderer.enabled = option; }
        }

        public RendererMaterialBlender[] renderersBlend;   // 原版 `[SerializeField] private`
        public bool fadeOnEnable;
        public bool isInDefaultScenario;
        public string filterCode;

        /// <summary>原版 `currentScenarioBlendOptions`(+0x38) —— 这里用「有没有」代替 null 引用。</summary>
        ScenarioBlendOptions _cur;
        bool _hasCur;
        /// <summary>= 原版 `doFade`(+0x40)。⚠️ 原版每次 `DoScenarioBlend` 都写 `doFade = !doFade`（**照抄**）。</summary>
        bool _doFade;
        float _targetFade;      // +0x44
        float _blendTime;       // +0x48（`_left` = 基类的 = 原版 +0x4C `currentBlendTime`）

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            // ---- ① filter（原版 +0x30 非空时：FilterOptions 必须非 null 且 FilterCode 相等，否则整条跳过）----
            if (!string.IsNullOrEmpty(filterCode))
            {
                if (o.filterOptions == null)
                {
                    // 原版这里**抛 NullReferenceException**（`.c` 的 `LAB_18062fe12` = NRE；反汇编 0x62E5CD）。
                    // 抛异常会把调用者整条链打断 ⇒ 我们**出声并跳过这一条**（等价于「不匹配」那一支）。
                    Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name}) 的 `filterCode` 是 "
                                 + $"`{filterCode}`，但这一程的 `options.filterOptions` 是 null —— 原版这里会抛 NRE；"
                                 + "我们出声并跳过这一条（数据缺 `filterCode`/`filterEnabled` ⇒ 查 `Resources/EnvironmentConditions.json`）");
                    return;
                }
                if (filterCode != o.filterOptions.filterCode) return;   // ★ 不匹配 ⇒ 整条跳过
            }
            // 🔴 **第二个 null 检查**：原版对 `currentScenarioBlendOptions.FilterOptions == null` 也会**抛 NRE**
            //    （反汇编 `0x62E692 test rcx,rcx` / `0x62E695 je 0x62e812` —— 与 `filterCode` 空不空**无关**）
            //    ⇒ 我们同样「出声 + 不生效」。⚠️ 别把它写成「当空串继续跑」——那会让这一条在
            //    `filterCode` 为空时**比原版多做一整套**（开渲染器 + 起补间）。
            if (o.filterOptions == null)
            {
                Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name})：这一程的 `options.filterOptions` 是 null —— "
                             + "原版这里会抛 NRE；我们出声并跳过这一条（不出声就会变成「多做一整套」，见代码注释）");
                return;
            }

            // ---- ② 记下这一程的 options（原版 new 一份、逐字段拷）----
            _cur = o;
            _hasCur = true;

            // ---- ③ FilterCode 非空 ⇒ 用 isEnabled 覆盖 direction / targetValue ★ ----
            if (!string.IsNullOrEmpty(o.filterOptions.filterCode))
            {
                _cur.direction = o.filterOptions.isEnabled;
                _cur.targetValue = o.filterOptions.isEnabled ? 1f : 0f;
            }

            // ---- ④ 全部渲染器先打开 ----
            if (renderersBlend != null)
                for (int i = 0; i < renderersBlend.Length; i++)
                {
                    var r = renderersBlend[i];
                    if (r == null || r.renderer == null)
                    {
                        Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name}) 的第 {i} 条 "
                                     + "`renderersBlend` 缺 `renderer` —— 原版这里会抛 NRE；我们出声并跳过这一条");
                        continue;
                    }
                    r.renderer.enabled = true;
                }

            // ---- ⑤ `fadeOnEnable` 且方向为真 ⇒ 先 Init(0) 再把 targetFade 顶到 1 ----
            if (fadeOnEnable && _cur.direction)
            {
                if (renderersBlend != null)
                    for (int i = 0; i < renderersBlend.Length; i++)
                        if (renderersBlend[i] != null) renderersBlend[i].Init(0f);
                _doFade = !_doFade;                                   // 原版真的这么写（反汇编 0x62E794）
                _blendTime = _cur.duration; _left = _cur.duration;
                _targetFade = 1f;
                return;
            }

            // ---- ⑥ 普通那一支（注意 duration 取的是**原始 options** 的那个）----
            _blendTime = o.duration; _left = o.duration;
            _doFade = !_doFade;                                       // 同上
            _targetFade = _cur.targetValue;
        }

        /// <summary>= 原版 `Update()`（判据见类注释；推进权收归执行器，见本文件开头那段）。</summary>
        public override void Advance(float dt)
        {
            if (!_doFade) return;
            // 原版是 `Time.deltaTime / blendTime`：`blendTime == 0` 时得 +∞ ⇒ `MoveTowards` 一步到位。
            // 这里显式把 `blendTime <= 0` 走「一步到位」那一支 —— 与 +∞ **行为等价**，同时躲开 `dt==0` 时的 NaN。
            float speed = (_blendTime <= 0f) ? float.PositiveInfinity : dt / _blendTime;
            if (renderersBlend != null)
                for (int i = 0; i < renderersBlend.Length; i++)
                {
                    var r = renderersBlend[i];
                    if (r == null || r.renderer == null)
                    {
                        Debug.LogError($"[EnvBlend] `ScenarioGenericMaterialBlend`({name}) 的第 {i} 条 "
                                     + "`renderersBlend` 缺 `renderer` —— 原版这里会抛 NRE；我们出声并跳过这一条");
                        continue;
                    }
                    r.DoBlend(_targetFade, speed);
                }

            // 完成判定在「跑完所有渲染器」之后（原版先判再减）
            if (_left <= 0f)
            {
                if (_hasCur)
                {
                    var cb = _cur.onComplete;
                    _cur.onComplete = null; _hasCur = false;
                    if (cb != null) cb();
                }
                _doFade = false;
            }
            _left -= dt;
        }

        /// <summary>判据 = `…__ToggleRenderers.c`：逐个 `renderer.enabled = option`。</summary>
        public void ToggleRenderers(bool option)
        {
            if (renderersBlend == null) return;
            for (int i = 0; i < renderersBlend.Length; i++)
                if (renderersBlend[i] != null) renderersBlend[i].ToggleRenderer(option);
        }

        /// <summary>判据 = `…__SetTargetFade.c`：`targetFade = &lt;第1个实参>; blendTime = currentBlendTime = &lt;第2个实参>`。</summary>
        public void SetTargetFade(float targetValue, float blendTimeArg)
        {
            _targetFade = targetValue; _blendTime = blendTimeArg; _left = blendTimeArg;
        }
    }
}
