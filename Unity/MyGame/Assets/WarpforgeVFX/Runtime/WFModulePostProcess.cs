// WFModulePostProcess.cs — 原版 `AnimFXModulePostProcess` 的对应物（**52 实例 / 52 效果**）
//
// 语义出处（**照方法体写**）：
//   · `资料/AnimFX_18类方法体_块1.md` §9「AnimFXModulePostProcess ‖ 全屏 LUT + Bloom（带优先级仲裁）」
//     —— 14 个方法体 + 5 个 lambda 的读解；字段与实例数见 `资料/AnimFX_18类成表.md` §四。
//   · 本轮又对着**反编译原文**逐行核了一遍（结论一致；补了三条细节，下面标 🆕）：
//     `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/AnimFXModulePostProcess__{OnEnable,Update,
//      DoBloomAim,DoLUTAnim,ResetPostFX,ResetCurrentPlayingEffect,CancelAnimation,OnDisable,OnDestroy,
//      AnimEventSetOriginalLUTNoTransition,BlendLutTextures,OnValidate,.ctor}.c`
//
// 原版在做什么（下面每一条都是对着方法体写的）：
//   · `.ctor`      → `controlledByAnimation = true` · `maxLUTBlend = 1.0f` · `effectPriority = 1` ·
//                    `blendMaterialID = Shader.PropertyToID("_Blend")`
//   · `OnEnable`   → **优先级仲裁**：`allowAnimation = true`；
//                    `if (effectPriority < currentAnimPriority) { allowAnimation = false; return; }`（低优先级整段退场）
//                    否则**踢掉正在播的那个**：`currentEffectInPlay.ResetPostFX(true)` + Kill 它那两条 tween；
//                    然后 `currentAnimPriority = effectPriority` · `currentEffectInPlay = this`；
//                    `instanceMaterial ??= new Material(Shader.Find("Hidden/LUTBlender"))`；
//                    `if (doLUTAnim && lutToApplyRT == null && customLUT1 != null)` →
//                    `new RenderTexture(256, 16)` + `useMipMap = false` + 名字 `Particle FX controller LUT`；
//                    **`if (!controlledByAnimation)`** → 先 Kill 旧 tween；`timeToOn == 0` 时直接
//                    `lutBlend = maxLUTBlend`；再建两条 tween：`起点→maxLUTBlend`（时长 `timeToOn`）、
//                    `→0`（时长 `timeToOff`、`SetDelay(timeOn + timeToOn)`、`OnComplete(ResetCurrentPlayingEffect)`）
//   · `Update`     → `if (!allowAnimation) return;` → `DoBloomAim()` → `DoLUTAnim()` → `UpdateVolumeStack`
//   · `DoBloomAim` → 只在 `animateBloomIntensity` / `animateBloomThreshold` 打开时写 Bloom 的
//                    intensity / threshold（🆕 原版外面还套了两层判空：`PostFXController.Instance`
//                    与它那个 Bloom 对象都得非空）
//   · `DoLUTAnim`  → 首次（`alreadySetLUTAnimTexture == false`）：`SetTexture("_LUT1", customLUT1)` +
//                    `SetTexture("_LUT2", customLUT2)` + `SetTargetLUT(lutToApplyRT ?? customLUT1)`；
//                    此后每帧：`lutToApplyRT` 非空时 `SetFloat("_Blend", betweenLUTsBlend)` +
//                    `Blit(null, lutToApplyRT, instanceMaterial)`；最后 `DoBlend(lutBlend)`
//   · `ResetPostFX(instant)` = `PostFXController.Instance.ResetBloom(instant)` + `ResetLUT(instant)`
//   · `ResetCurrentPlayingEffect()` → **只有自己还是「正在播的那个」**（Unity 的 `==` 比）才
//                    `ResetPostFX(false)`，并把 `currentAnimPriority = -1`、`currentEffectInPlay = null`
//   · `CancelAnimation()` → `ResetPostFX(true)` + Kill 自己那两条 tween
//   · `OnDisable`  → `alreadySetLUTAnimTexture = false` + `ResetCurrentPlayingEffect()`
//   · `OnDestroy`  → 🆕 **只**销毁 `lutToApplyRT` 与 `instanceMaterial`（**不复位** —— 复位在 `OnDisable`。
//                    块1 §9 那行把两者合写成一句，读原文能看清是**两件分开的事**）
//   · `OnValidate` → 仅当 `Application.isPlaying` 且 `instanceMaterial` 非空：把两张 LUT 重新 SetTexture
//   · `AnimEventSetOriginalLUTNoTransition()` → 🆕 原文签名**没有 this**（`void …(void)`）⇒ 原版是**静态方法**；
//                    做的是 `LUTBlender.instanceMaterial.SetTexture("_LUT1", LUTBlender.originalLUTTexture)`
//                    ⚠️ 块1 §9/附D 把它归在「给动画事件调」那一类，**以原文为准**（见文件头「我们定的」末条）
//   · `BlendLutTextures()` → 就是上面 `_Blend` + Blit 那两行（🆕 它的判空是 `instanceMaterial != null`，
//                    **不是** `lutToApplyRT != null` —— 可单独调）
//
// ✅ 还原了什么
//   ① **优先级仲裁整段**：`effectPriority` / `currentAnimPriority` / `currentEffectInPlay` 三个语义
//      与原版一致（低优先级整段不生效、高优先级踢掉在播的那个、复位时只复位「自己还是那个」）。
//   ② **`lutBlend` 的三段时间曲线**：`timeToOn` 淡入 → `timeOn` 保持 → `timeToOff` 淡出 →
//      `ResetCurrentPlayingEffect()`，起点/终点/时刻都照原版（`timeToOn == 0` 直接置 `maxLUTBlend`）。
//   ③ **开关语义全在**：`doLUTAnim` / `animateBloomIntensity` / `animateBloomThreshold` /
//      `controlledByAnimation` / `betweenLUTsBlend` / `maxLUTBlend` / `effectPriority` 都按原版的分支走；
//      `alreadySetLUTAnimTexture`「首次才推贴图」也照抄。
//   ④ 六个公开入口都在：`ResetPostFX` / `ResetCurrentPlayingEffect` / `CancelAnimation` /
//      `AnimEventSetOriginalLUTNoTransition` / `BlendLutTextures` / `OnValidate`。
//      （块1 §附D 那条：它们的**代码调用点一个都没找到**，属于动画事件 / Inspector 那条路 —— 等接线。）
//
// ⚠️ 我们自己定的（显式标出）
//   · **仲裁时机放 `Initialize`，不放 `OnEnable`**：`AddComponent` 会**先**触发 `OnEnable`、
//     那时字段还没被 `Configure` 填（与 `WFModuleScreenShake` 同一条理由）。
//   · **`Exit()` / `DoDestroy()` 映射 `OnDisable` / `OnDestroy`**（我们这边没有「被 disable」这个时刻，
//     有的是「进收尾 / 被销毁」）。🔴 关键差异：**我们 `Exit()` 之后对象还会活 `exitDestroyTime`（默认 3 秒）**
//     而且 `ModuleTick` 继续广播 ⇒ 必须在 `Exit()` 里把 `allowAnimation` 关掉，否则收尾那 3 秒会继续
//     往后处理链写值（原版 `OnDisable` 之后 `Update` 就不跑了）。
//   · **`lutBlend` 的过渡是我们自己算的，不用 DOTween**：批处理下没有帧循环（工程约定），补间推不动。
//     起点/终点/时长/延迟都照原版，**缓动用 `Ease.OutQuad`** —— 那是 DOTween 的全局默认
//     （原版这两条 tween **没有**显式 `SetEase`）。🔴 **我们没在原版工程的 DOTweenSettings 里核过
//     那个默认值** ⇒ 这一条算「我们定的」，不是原版结论。
//   · **`alreadySetLUTAnimTexture` / `allowAnimation` 做成 public 字段**（原版是非序列化字段）
//     —— 为了 Inspector 里看得见、自检能断言。
//   · **`AnimEventSetOriginalLUTNoTransition` 按原文做成 `static`**。将来要用**动画事件**绑它
//     （动画事件只能绑实例方法）就把 `static` 去掉 —— 改的时候记得同步这句注释。
//
// 🔴 未还原（整条「怎么把它画到屏幕上」的那一半）
//   · **`instanceMaterial = new Material(Shader.Find("Hidden/LUTBlender"))`**：那个 shader **不在我们工程里**
//     （`Assets/WarpforgeVFX/Shaders/` 只有 5 个自建 `WF*`），也没有**运行时**取它的路 ⇒ 不建。
//     📌 规格是查得到的，下游照这个做：`资料/普查产出_0917/shader属性表_块3.md:294-300` ——
//     `Hidden/LUTBlender` 属性 3 个：`_LUT1`(Texture, def white) · `_LUT2`(Texture, def white) ·
//     `_Blend`(Range 0–1, def 0)；pass `One→Zero`、zWrite On、lod 100；原版有 13 个材质用它。
//   · **`lutToApplyRT`（两张 LUT 的合并 RT，原版规格 **256×16**、`useMipMap = false`）**：
//     它的用途是 `Graphics.Blit(null, lutToApplyRT, instanceMaterial)` 的目标 —— **没有那个 shader 就建了没用**
//     ⇒ 由下游建。规格（`LutMergeWidth` / `LutMergeHeight` 两个常量）留在这里，别改。
//   · **真正把后期应用到画面上**：原版是 `PostFXController.ResetBloom/ResetLUT` + `LUTBlender.DoBlend`
//     + `Universal.CameraExtensions.UpdateVolumeStack`。我们**没有后期链**（没有 URP `Volume` / `ColorLookup`）
//     ⇒ 这一层整段路由到静态钩子 `OnPostFx`，**没人挂就整段不生效**（计数 + 警告，不静默）。
//     📌 下游该长什么样，原版已经给了骨架：`LUTBlender` 是个 MonoBehaviour，字段
//     `blendMaterial:Material` · `volume:Volume` · `colorLUT:ColorLookup` · `originalLUTTexture:Texture` ·
//     `instanceMaterial:Material` · `combinedLUT` / `workingRT:RenderTexture` · `targetBlendValue` ·
//     `currentTime` · `doAnimatedBlend` · `timeToBlend`；方法 `ResetToDefaultLUT(time)` ·
//     `TransitionTo(tex, time)` · `SetTargetLUT(tex)` · `DoBlend(v)` · `SetOriginalLUTOnBlendPos0()` ·
//     `SetDefaultLUT(tex)`。⚠️ **只有字段名与签名**：那份 full_decomp 的**方法体是空的**
//     （`d:/2/Warpforge_tools/tmp/full_decomp/LUTBlender.cs`）—— 别把它的空方法体当行为依据。
//   · **「原 LUT」是哪张贴图**：`AnimEventSetOriginalLUTNoTransition` 读的是 `LUTBlender.originalLUTTexture`，
//     **具体赋值查不到**（它在场景 / 预制体里被塞进去）⇒ 由下游决定，别猜。
//   · **`controlledByAnimation = 1` 的 23/52 个实例**：原版由**动画驱动 `lutBlend` 这个序列化字段**，
//     模块只负责每帧把它推下去。我们**没有「动画驱动 MonoBehaviour 字段」那层** ⇒ 那些实例的
//     `lutBlend` 会是常量（好在数据里 46/52 本来就是 0.0）。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>后期请求的种类。原版没有这个枚举（它直接调 `PostFXController` / `LUTBlender`）——
    /// 我们这边那两层都不存在，所以把「模块要后期做什么」收成一个请求结构 + 一个静态钩子。</summary>
    public enum PostFxOp
    {
        /// <summary>`DoBloomAim()`：只在开关打开时写 Bloom 的 intensity / threshold。</summary>
        BloomAim = 0,
        /// <summary>`DoLUTAnim()`：两张 LUT（+ 它们之间的混合）+ `DoBlend(lutBlend)`。</summary>
        LutBlend = 1,
        /// <summary>`ResetPostFX(instant)` = `ResetBloom(instant)` + `ResetLUT(instant)`。</summary>
        Reset = 2,
        /// <summary>`AnimEventSetOriginalLUTNoTransition()`：立刻换回「原 LUT」，不过渡。</summary>
        SetOriginalLut = 3,
    }

    /// <summary>一次后期请求。字段按 `op` 分三组用，没用到的那组是默认值。</summary>
    [Serializable]
    public struct PostFxRequest
    {
        public PostFxOp op;
        /// <summary>下这个请求的模块的 `effectPriority`（诊断 / 下游若要再仲裁用）。</summary>
        public int priority;
        /// <summary>哪个效果（诊断用）。</summary>
        public string effect;

        // ---- BloomAim ----
        public bool setIntensity;   public float intensity;
        public bool setThreshold;   public float threshold;

        // ---- LutBlend ----
        /// <summary>`true` = 首次（`alreadySetLUTAnimTexture == false`）⇒ 下游要做一次
        /// SetTexture(`_LUT1`/`_LUT2`) + 「`SetTargetLUT(lutToApplyRT ?? lut1)`」等价动作。</summary>
        public bool setTextures;
        /// <summary>`customLUT1` 的**资产名**（原版是 Texture2D 引用；我们按名字传，下游自己找贴图）。</summary>
        public string lut1;
        /// <summary>`customLUT2` 的资产名（数据里 46/52 为空）。</summary>
        public string lut2;
        /// <summary>对应原版 `if (lutToApplyRT)` 那一支：先按 `betweenLutBlend` 把两张 LUT 合并进
        /// 256×16 的 RT，再用合并结果当 LUT。</summary>
        public bool mergeLuts;
        /// <summary>两张 LUT 之间的混合系数（原版写进 `_Blend`，属性 def 0）。</summary>
        public float betweenLutBlend;
        /// <summary>`DoBlend(lutBlend)` 的那个值（0 – `maxLUTBlend`）。</summary>
        public float lutBlend;
        /// <summary>`BlendLutTextures()` 单独调的（原版那两行：`SetFloat("_Blend")` + `Blit`）。</summary>
        public bool blitNow;

        // ---- Reset / SetOriginalLut ----
        /// <summary>`ResetPostFX(instant)`：`true` = 不过渡（`CancelAnimation` 用的就是它）。</summary>
        public bool instant;
    }

    [WFModuleKind("AnimFXModulePostProcess")]
    public class WFModulePostProcess : WFEffectModule
    {
        // ---- 原版的两个静态（**跨实例**，优先级仲裁的关键；`.cctor` 里 `currentAnimPriority = -1`）----
        public static int currentAnimPriority = -1;
        public static WFModulePostProcess currentEffectInPlay;

        /// <summary>原版 `.ctor` 里 `Shader.PropertyToID("_Blend")`。属性名出处：
        /// `资料/普查产出_0917/shader属性表_块3.md:294-300`（`_Blend` Range 0–1，def 0）。</summary>
        public static readonly int blendMaterialID = Shader.PropertyToID("_Blend");
        public const string BlendPropertyName = "_Blend";

        /// <summary>原版 LUT 合并 RT 的规格（`new RenderTexture(0x100, 0x10, …)`）—— 下游照它建。**别改。**</summary>
        public const int LutMergeWidth = 256;
        public const int LutMergeHeight = 16;

        /// <summary>🔑 **下游钩子**：表现层挂这里（原版是 `PostFXController` + `LUTBlender` 那一层）。
        /// **没挂 = 整个后期不生效**（计数 + 警告，不静默）。</summary>
        public static Action<PostFxRequest> OnPostFx;

        /// <summary>「后期整段没落地」的**实例数**（每个实例只记一次，不按帧涨）。自检看它是不是 0。</summary>
        public static int DroppedEffects;

        /// <summary>累计派发次数（每帧可能一次）—— 自检只须看它是不是 > 0（证明下游真收到了）。</summary>
        public static int AppliedRequests;

        static int _warnedDrops;

        /// <summary>自检用：把两个计数与静态仲裁状态清零（**静态是跨实例的**，跑测试之间要清）。</summary>
        public static void ResetDiagnostics()
        {
            DroppedEffects = 0;
            AppliedRequests = 0;
            _warnedDrops = 0;
            currentAnimPriority = -1;
            currentEffectInPlay = null;
        }

        // ---- 字段：名字与默认值照原版（默认值出处 = `.ctor`）----
        [Tooltip("原版 `controlledByAnimation`：为真时 `lutBlend` 由外部（动画）驱动，模块不自己跑过渡")]
        public bool controlledByAnimation = true;

        [Tooltip("原版 `animateBloomIntensity`：每帧把 `bloomIntensity` 写进 Bloom")]
        public bool animateBloomIntensity;
        public float bloomIntensity;

        [Tooltip("原版 `animateBloomThreshold`：每帧把 `bloomThreshold` 写进 Bloom")]
        public bool animateBloomThreshold;
        public float bloomThreshold;

        [Tooltip("原版 `doLUTAnim`（数据里 50/52 打开 —— 这一类本质是 LUT 模块，Bloom 是附赠）")]
        public bool doLUTAnim;

        [Tooltip("原版 `lutBlend`：当前 LUT 混合强度（`controlledByAnimation` 为真时由动画推）")]
        public float lutBlend;

        [Tooltip("原版 `maxLUTBlend`（.ctor 默认 1.0）—— 淡入的终点")]
        public float maxLUTBlend = 1f;

        [Tooltip("原版 `timeToOn`：淡入时长（0 = 立刻到 maxLUTBlend）")]
        public float timeToOn;
        [Tooltip("原版 `timeOn`：保持时长")]
        public float timeOn;
        [Tooltip("原版 `timeToOff`：淡出时长（到 0 之后 `ResetCurrentPlayingEffect`）")]
        public float timeToOff;

        [Tooltip("原版 `customLUT1:Texture2D` —— 填**资产名**（如 `LUT Red Tint`）")]
        public string customLUT1 = "";
        [Tooltip("原版 `customLUT2:Texture2D` —— 填资产名（数据里 46/52 为空）")]
        public string customLUT2 = "";

        [Tooltip("原版 `betweenLUTsBlend`：两张 LUT 之间的混合系数（写进 `_Blend`）")]
        public float betweenLUTsBlend;

        [Tooltip("原版 `effectPriority`（.ctor 默认 1；数据里只有 1 与 0 两档）")]
        public int effectPriority = 1;

        /// <summary>原版非序列化 `alreadySetLUTAnimTexture`：两张 LUT 只往下游推一次。</summary>
        [Tooltip("原版非序列化字段 `alreadySetLUTAnimTexture`（首次之后不再推贴图；`OnDisable` 复位）")]
        public bool alreadySetLUTAnimTexture;

        /// <summary>原版非序列化 `allowAnimation`：优先级仲裁的产物。`false` = 本实例整段不生效。</summary>
        [Tooltip("原版非序列化字段 `allowAnimation`（= 仲裁通过）；false 时 `ModuleTick` 直接返回")]
        public bool allowAnimation;

        // ---- 我们自己的过渡状态（替代原版那两条 DOTween）----
        float _rampT;        // 从 `Initialize` 起算的秒数（= 原版两条 tween 从创建起的秒数）
        float _rampFrom;     // 原版 tweenStart 的**起点** = 建 tween 那一刻的 `lutBlend`
        bool _rampActive;
        bool _dropCounted;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;
            controlledByAnimation = def.GetBool("controlledByAnimation", controlledByAnimation);
            animateBloomIntensity = def.GetBool("animateBloomIntensity", animateBloomIntensity);
            bloomIntensity        = def.GetFloat("bloomIntensity", bloomIntensity);
            animateBloomThreshold = def.GetBool("animateBloomThreshold", animateBloomThreshold);
            bloomThreshold        = def.GetFloat("bloomThreshold", bloomThreshold);
            doLUTAnim             = def.GetBool("doLUTAnim", doLUTAnim);
            lutBlend              = def.GetFloat("lutBlend", lutBlend);
            maxLUTBlend           = def.GetFloat("maxLUTBlend", maxLUTBlend);
            timeToOn              = def.GetFloat("timeToOn", timeToOn);
            timeOn                = def.GetFloat("timeOn", timeOn);
            timeToOff             = def.GetFloat("timeToOff", timeToOff);
            customLUT1            = AssetName(def.GetString("customLUT1"));
            customLUT2            = AssetName(def.GetString("customLUT2"));
            betweenLUTsBlend      = def.GetFloat("betweenLUTsBlend", betweenLUTsBlend);
            effectPriority        = def.GetInt("effectPriority", effectPriority);
        }

        /// <summary>原版 `OnEnable`（时机见文件头「我们定的」第一条）。</summary>
        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);

            // ---- 优先级仲裁 ----
            allowAnimation = true;
            if (effectPriority < currentAnimPriority)
            {
                // 原版：低优先级**直接退场** —— 连正在播的那个也不碰（整段不生效）
                allowAnimation = false;
                return;
            }
            if (currentEffectInPlay != null && currentEffectInPlay != this)
                currentEffectInPlay.CancelAnimation();      // 原版 = ResetPostFX(true) + Kill 它两条 tween
            currentAnimPriority = effectPriority;
            currentEffectInPlay = this;

            // 🔴 未还原：`instanceMaterial = new Material(Shader.Find("Hidden/LUTBlender"))`（那个 shader 不在工程里）
            // 🔴 未还原：`lutToApplyRT = new RenderTexture(256, 16){ useMipMap = false, name = "Particle FX controller LUT" }`
            //    —— 两条都见文件头「未还原」段；下游要建的话规格是 LutMergeWidth / LutMergeHeight。

            if (!controlledByAnimation)
            {
                // 原版：先 Kill 旧 tween（我们这里是把自己的过渡状态归零），再建两条 tween
                _rampActive = true;
                _rampT = 0f;
                _rampFrom = lutBlend;
                if (timeToOn == 0f) lutBlend = maxLUTBlend;   // 原版：`timeToOn == 0` 时直接赋值
            }
        }

        /// <summary>我们这边的每帧（= 原版 `Update`。原版各模块自己写 `Update`，我们收口成 `ModuleTick`）。</summary>
        public override void ModuleTick(float dt)
        {
            if (!allowAnimation) return;      // 原版 `Update` 第一句
            TickBlendRamp(dt);                // 原版这一步在 DOTween 自己的 Update 里 —— 见文件头
            DoBloomAim();
            DoLUTAnim();
            // 🔴 未还原：原版最后一句是 `Universal.CameraExtensions.UpdateVolumeStack(BattleManager.<0xa8>)`
            //    —— 那是相机后处理栈的刷新，我们这边没有 VolumeStack（也没有后期链），对应物在下游。
        }

        /// <summary>原版 `DoBloomAim()`：只在开关打开时写 Bloom 的 intensity / threshold。
        /// （原版外面还套了 `PostFXController.Instance` 与 Bloom 对象的判空 —— 那两条由下游负责。）</summary>
        void DoBloomAim()
        {
            if (!animateBloomIntensity && !animateBloomThreshold) return;
            Dispatch(new PostFxRequest
            {
                op = PostFxOp.BloomAim,
                priority = effectPriority,
                effect = EffectName,
                setIntensity = animateBloomIntensity,
                intensity = bloomIntensity,
                setThreshold = animateBloomThreshold,
                threshold = bloomThreshold,
            });
        }

        /// <summary>原版 `DoLUTAnim()`：首次推两张贴图 + `SetTargetLUT`，之后每帧推混合值。</summary>
        void DoLUTAnim()
        {
            if (!doLUTAnim) return;                       // 原版第一句
            bool first = !alreadySetLUTAnimTexture;
            if (first) alreadySetLUTAnimTexture = true;   // 原版：置 1 之后就不再 SetTexture 了
            Dispatch(new PostFxRequest
            {
                op = PostFxOp.LutBlend,
                priority = effectPriority,
                effect = EffectName,
                setTextures = first,
                lut1 = customLUT1,
                lut2 = customLUT2,
                // 原版：`if (lutToApplyRT)` 才走「先合并两张 LUT」那一支；而 RT 是在 OnEnable 建的
                // （条件 `doLUTAnim && lutToApplyRT == null && customLUT1 != null`）⇒ 等价于「有第一张 LUT」。
                mergeLuts = !string.IsNullOrEmpty(customLUT1),
                betweenLutBlend = betweenLUTsBlend,
                lutBlend = lutBlend,
            });
        }

        /// <summary>原版那两条 `DOTween.To` 的等价物：`timeToOn` 淡入 → `timeOn` 保持 →
        /// `timeToOff` 淡出 → `ResetCurrentPlayingEffect()`。</summary>
        void TickBlendRamp(float dt)
        {
            if (!_rampActive) return;
            _rampT += dt;
            float hold = timeToOn + timeOn;               // 原版 tweenEnd 的 `SetDelay(timeOn + timeToOn)`

            if (timeToOn > 0f && _rampT < timeToOn)       // 淡入段：`_rampFrom → maxLUTBlend`
            {
                lutBlend = Mathf.LerpUnclamped(_rampFrom, maxLUTBlend, Ease(_rampT / timeToOn));
                return;
            }
            if (_rampT < hold)                            // 保持段：原版靠 tweenStart 停在终点
            {
                lutBlend = maxLUTBlend;
                return;
            }
            if (timeToOff <= 0f)                          // 原版：0 时长的 tween **当场完成**并触发 OnComplete
            {
                lutBlend = 0f;
                _rampActive = false;
                ResetCurrentPlayingEffect();
                return;
            }
            float k = (_rampT - hold) / timeToOff;        // 淡出段：`maxLUTBlend → 0`
            if (k >= 1f)
            {
                lutBlend = 0f;
                _rampActive = false;
                ResetCurrentPlayingEffect();
                return;
            }
            lutBlend = Mathf.LerpUnclamped(maxLUTBlend, 0f, Ease(k));
        }

        /// <summary>原版 `ResetPostFX(instant)` = `ResetBloom(instant)` + `ResetLUT(instant)`。</summary>
        public void ResetPostFX(bool instant)
        {
            Dispatch(new PostFxRequest
            {
                op = PostFxOp.Reset,
                priority = effectPriority,
                effect = EffectName,
                instant = instant,
            });
        }

        /// <summary>原版：**只有自己还是「正在播的那个」**才复位，并清掉两个静态（`-1` / `null`）。
        /// 判等用的是 Unity 的 `==`（原版 `op_Equality`）—— C# 里写 `!=` 就是同一个语义。</summary>
        public void ResetCurrentPlayingEffect()
        {
            if (currentEffectInPlay != this) return;
            ResetPostFX(false);
            currentAnimPriority = -1;
            currentEffectInPlay = null;
        }

        /// <summary>原版 `CancelAnimation()` = `ResetPostFX(true)` + Kill 自己那两条 tween
        /// （我们 Kill 的是自算的过渡状态）。</summary>
        public void CancelAnimation()
        {
            _rampActive = false;
            ResetPostFX(true);
        }

        /// <summary>🆕 原版这个方法是**静态**的（原文签名没有 this）：把「原 LUT」立刻换上去、不过渡。
        /// 原版做的是 `LUTBlender.instanceMaterial.SetTexture("_LUT1", LUTBlender.originalLUTTexture)`。
        /// 🔴 「原 LUT」是哪张贴图**查不到**（由场景/预制体赋值）⇒ 由下游决定（见文件头）。
        /// ⚠️ 要用**动画事件**绑它就得去掉 `static`（动画事件只能绑实例方法）。</summary>
        public static void AnimEventSetOriginalLUTNoTransition()
        {
            DispatchStatic(new PostFxRequest { op = PostFxOp.SetOriginalLut });
        }

        /// <summary>原版 `BlendLutTextures()`：`instanceMaterial.SetFloat("_Blend", betweenLUTsBlend)`
        /// + `Blit(null, lutToApplyRT, instanceMaterial)`（原版的判空是 `instanceMaterial != null`）。
        /// 我们这边没有那个材质 ⇒ 转成一个「请下游重新合并一次 LUT」的请求。</summary>
        public void BlendLutTextures()
        {
            Dispatch(new PostFxRequest
            {
                op = PostFxOp.LutBlend,
                priority = effectPriority,
                effect = EffectName,
                setTextures = false,
                lut1 = customLUT1,
                lut2 = customLUT2,
                mergeLuts = !string.IsNullOrEmpty(customLUT1),
                betweenLutBlend = betweenLUTsBlend,
                lutBlend = lutBlend,
                blitNow = true,
            });
        }

        /// <summary>原版 `OnValidate()`：仅 `Application.isPlaying` 时重推两张 LUT 贴图（编辑器路径）。</summary>
        void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (OnPostFx == null) return;      // 没下游就别在编辑器里刷计数/警告
            Dispatch(new PostFxRequest
            {
                op = PostFxOp.LutBlend,
                priority = effectPriority,
                effect = EffectName,
                setTextures = true,            // 原版这里是无条件重新 SetTexture（不看 alreadySetLUTAnimTexture）
                lut1 = customLUT1,
                lut2 = customLUT2,
                mergeLuts = !string.IsNullOrEmpty(customLUT1),
                betweenLutBlend = betweenLUTsBlend,
                lutBlend = lutBlend,
            });
        }

        /// <summary>对应原版 `OnDisable`。⚠️ 我们这边 `Exit()` 之后对象**还会活 `exitDestroyTime`**
        /// 且 `ModuleTick` 继续广播 ⇒ 这里必须把 `allowAnimation` 一起关掉（见文件头「我们定的」）。</summary>
        public override void Exit()
        {
            allowAnimation = false;
            _rampActive = false;
            alreadySetLUTAnimTexture = false;   // 原版 OnDisable 第一句
            ResetCurrentPlayingEffect();        // 原版 OnDisable 第二句
        }

        /// <summary>对应原版 `OnDestroy`（原版这里**只**销毁 RT 与材质，复位是 `OnDisable` 干的）。
        /// 我们没有那两样东西 ⇒ 这里补一次复位：`Kill()` 那条路**不走 `Exit`**，不补就会把静态留在原地。
        /// 幂等（`ResetCurrentPlayingEffect` 只在「自己还是正在播的那个」时才动）。</summary>
        public override void DoDestroy()
        {
            allowAnimation = false;
            _rampActive = false;
            alreadySetLUTAnimTexture = false;
            ResetCurrentPlayingEffect();
        }

        // ---- 派发 ----

        /// <summary>实例路径。**每帧都会调** ⇒ 没下游时「每个实例只记一次」，不按帧刷计数。</summary>
        void Dispatch(PostFxRequest req)
        {
            var h = OnPostFx;
            if (h == null)
            {
                if (!_dropCounted)
                {
                    _dropCounted = true;
                    DroppedEffects++;
                    WarnNoDownstream(req);
                }
                return;
            }
            h(req);
            AppliedRequests++;
        }

        /// <summary>静态入口（`AnimEventSetOriginalLUTNoTransition`）走这条：没有实例可以记账。</summary>
        static void DispatchStatic(PostFxRequest req)
        {
            var h = OnPostFx;
            if (h == null)
            {
                DroppedEffects++;
                WarnNoDownstream(req);
                return;
            }
            h(req);
            AppliedRequests++;
        }

        static void WarnNoDownstream(PostFxRequest req)
        {
            if (_warnedDrops >= 5) return;
            _warnedDrops++;
            Debug.LogWarning("[WarpforgeVFX] 后期（LUT / Bloom）**没有下游接**：`AnimFXModulePostProcess` 整段没生效" +
                             $"（效果 `{req.effect}`，op = {req.op}）。要接：表现层挂 `WFModulePostProcess.OnPostFx`" +
                             "（原版是 `PostFXController` + `LUTBlender`：`Hidden/LUTBlender`、`_LUT1`/`_LUT2`/`_Blend`、" +
                             $"{LutMergeWidth}×{LutMergeHeight} 合并 RT）。这条警告只报前 5 次。");
        }

        /// <summary>`@asset:Texture2D:LUT Red Tint` → `LUT Red Tint`（不是资产引用就原样返回；空串还是空串）。</summary>
        static string AssetName(string v)
        {
            string kind, type, rest;
            WFModuleDef.SplitRef(v, out kind, out type, out rest);
            return kind == "asset" ? rest : (v ?? "");
        }

        /// <summary>原版那两条 tween 是 `DOTween.To(...)`、**没有显式 `SetEase`** ⇒ 走 DOTween 的全局默认缓动。
        /// 我们按 `Ease.OutQuad` 算（DOTween 的默认值）。🔴 **没在原版工程的 DOTweenSettings 里核过** ⇒ 「我们定的」。</summary>
        static float Ease(float x)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            return 1f - (1f - x) * (1f - x);      // OutQuad
        }
    }
}
