// BattlePostFx.cs — `WFModulePostProcess` 的**下游**（原版 `PostFXController` + `LUTBlender` 那一层）
//
// 为什么要有这一层：`WarpforgeVFX` 那层**不认识战场 / 相机 / Volume**（它只管特效），所以
// `WFModulePostProcess` 把「要后期做什么」收成一个 `PostFxRequest` + 一个静态钩子 `OnPostFx`。
// 本文件就是那个钩子的实现 —— **它是唯一把 LUT 链接到画面的地方**。
//
// 判据（**全部实读，逐条给在下面；不是按名字猜**）
// ----------------------------------------------------------------
// · `LUTBlender` 的方法体（`d:/2/tools/decomp_full/LUTBlender__*.c`）：
//     `Awake`          → 建**两张 256×16 RT**；把 `ColorLookup.texture` 拷进 RT；`SetTexture("_LUT1", workingRT)`
//     `SetTargetLUT`   → `Blit(combinedLUT → workingRT)` + `SetTexture("_LUT2", 目标)`
//     `DoBlend(v)`     → `SetFloat("_Blend", v)` + `Blit(null → combinedLUT, mat)`
//     `Update`         → `v += dt`；`DoBlend(v / timeToBlend)`，到点自关
//     `ResetToDefaultLUT` / `SetOriginalLUTOnBlendPos0` / `TransitionTo` / `OnDestroy`
// · `PostFXController`：
//     `Start`     → 单例独占；`GetComponent<LUTBlender>()`；`VolumeProfile.TryGet<Bloom>` 并缓存原值
//     `ResetLUT`  → `LUTBlender.SetTargetLUT(blender, blender.originalLUTTexture, 0)`；再写
//                   `timeToBlend = TIME_TO_RESET`（instant=0）或 0、`currentTime = 0`、`doAnimatedBlend = 1`
//     `.cctor`    → `TIME_TO_RESET = 0x3f800000` = **1.0f**
// · `Hidden/LUTBlender` 的**算式**（反汇编）→ 见 `WarpforgeVFX/Shader 里的 WFLUTBlender.shader` 文件头：
//     `o = lerp(_LUT1, _LUT2, _Blend)`
// 判据全文 → `资料/普查产出_1001/资产导入路三件_侦察.md` §③。
//
// 🔴 **四处如实标注的不同**（都不是疏漏，写明白免得下个会话当 bug 改）
//  ① **`originalLUTTexture` 我们取「进场那一刻 `ColorLookup.texture` 里那张」**
//     （= 该战场的静态 LUT）。原版这个字段**由场景/预制体塞进去，赋值点没查到**
//     （`LUTBlender.SetDefaultLUT` 的反编译产物还错配成了 `PlayerHand__set_currentHand`）。
//  ② **6 个战场（arena1/2/3 · astramilitarum · blacklegion · darkangels）的 profile 没有
//     `ColorLookup` 槽**（原版那几场静态 LUT 也是空的，见 `ArenaBuilder.ApplyPostFx` 的注释）
//     ⇒ 我们在**运行时那份 profile 副本**上补一个 `ColorLookup`（见下面「为什么能安全改 profile」），
//       否则那 6 场的 LUT 特效**一点作用都没有**。⚠️ **这一步原版没有对应物**，会计数 + 出声。
//  ③ **`_Blend` 的过渡由模块自己算**（`WFModulePostProcess.TickBlendRamp`，因为批处理没有帧循环、
//     DOTween 推不动）—— 本文件只负责「拿到 `lutBlend` 就往下写」。
//  ④ **我们不建 `workingRT`**：原版两张 RT 是为了在两份 LUT 之间来回插值；我们的用法是
//     「`_LUT1` = 原 LUT（**只读**）、`_LUT2` = 目标 LUT」，**不需要那份中间拷贝**。
//     ⇒ 只建 `combinedLut`（= 原版 `combinedLUT`）+ `targetLutRt`（= 原版 `lutToApplyRT`）。
//
// 🔴 **为什么能安全改 profile**（踩过的坑，别改回去）：`Volume.profile` 的 getter 在
//    **没有实例时返回的是 `sharedProfile`**，直接往它上面写会**把资产改脏**
//    （工程里为这件事修过一次：`arenas/battlearena1/Profiles/battlearena1_PostFx.asset` 被自检弄脏）。
//    ⇒ `Attach` 里**先 `vol.profile = vol.sharedProfile`** 强制出一份实例副本，
//      之后所有写都落在副本上，**资产一条 diff 都不会有**。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WarpforgeVFX;

namespace CardPresentation
{
    /// <summary>`WFModulePostProcess` 的下游：把 `PostFxRequest` 落到战场的那个全局 `Volume` 上。</summary>
    public class BattlePostFx
    {
        // ---- 诊断（**不静默**：每一条都有计数，自检盯着）----
        public int Requests;              // 收到的请求数
        public int Blends;                // 真做了 LUT Blit 的次数
        public int MissingVolume;         // 没找到 Volume ⇒ 整条不生效
        public int MissingColorLookup;    // 补出来的 ColorLookup 次数（第 ② 条那个偏离）
        public readonly List<string> MissingLutTextures = new List<string>();

        public bool Ready { get { return _mat != null && _cl != null; } }
        /// <summary>现在写进 `ColorLookup.texture` 的那张（自检用；= null 表示还是静态那张）。</summary>
        public RenderTexture Combined { get { return _combined; } }
        /// <summary>「原 LUT」（进场那一刻 ColorLookup 上那张；自检用）。</summary>
        public Texture OriginalLut { get { return _original; } }
        public ColorLookup ColorLookupRef { get { return _cl; } }

        Material _mat;                    // `WarpforgeVFX/LUTBlender`（= 原版 `LUTBlender.instanceMaterial`）
        RenderTexture _combined;          // 原版 `combinedLUT`：最终喂给 ColorLookup 的那张
        RenderTexture _targetLutRt;       // 原版 `lutToApplyRT`：两张 LUT 先合到这里（规格 256×16）
        ColorLookup _cl;
        Bloom _bloom;
        Texture _original;
        float _bloomIntensity0 = -1f, _bloomThreshold0 = -1f;

        /// <summary>接到战场的全局 `Volume` 上（原版 `PostFXController.Start` 那一段）。
        /// 返回 false = 这一局没有后期链（**会计数**）。</summary>
        public bool Attach(Volume vol)
        {
            if (vol == null) { MissingVolume++; return false; }

            // 🔴 先强制出一份**运行时副本**，别往资产上写（见文件头）
            if (vol.sharedProfile != null) vol.profile = vol.sharedProfile;
            var prof = vol.profile;
            if (prof == null) { MissingVolume++; return false; }

            if (!prof.TryGet<ColorLookup>(out _cl))
            {
                // 第 ② 条那种战场：静态 LUT 本来就没有 ⇒ 在**副本**上补一个（资产不受影响）
                _cl = prof.Add<ColorLookup>(true);
                _cl.active = true;
                MissingColorLookup++;      // ⚠️ 计数：这一步原版没有对应物
            }
            else
            {
                _cl.active = true;         // 生效是被 `_Blend` 推的，见 `BlendTo`
            }
            // `colorLookup.texture` 原版是 `contribution` 1 —— 照抄
            _cl.contribution.overrideState = true;
            _cl.contribution.value = 1f;

            prof.TryGet<Bloom>(out _bloom);
            if (_bloom != null)            // 原版 `Start` 里缓存 bloom 原值，`ResetBloom` 要用
            {
                _bloomIntensity0 = _bloom.intensity.value;
                _bloomThreshold0 = _bloom.threshold.value;
            }

            _original = _cl.texture.value; // 第 ① 条：原版是 `LUTBlender.originalLUTTexture`

            Shader sh; string src;
            if (WarpforgeShaderMap.TryResolve("Hidden/LUTBlender", out sh, out src))
                _mat = new Material(sh) { name = "WFLUTBlender (BattlePostFx)" };
            else
                Debug.LogWarning("[BattlePostFx] 🔴 `Hidden/LUTBlender` 解析不到（既不在 `WarpforgeShaderMap` "
                               + "也不在随包 shader 里）⇒ LUT 链整条不生效");

            int w = WFModulePostProcess.LutMergeWidth, h = WFModulePostProcess.LutMergeHeight;
            _combined = NewRt(w, h, "Particle FX controller LUT");     // 原版那个名字，照抄
            _targetLutRt = NewRt(w, h, "LUT Blend helper");
            return _mat != null;
        }

        static RenderTexture NewRt(int w, int h, string name)
        {
            // 原版：`new RenderTexture(256, 16) { useMipMap = false }`
            return new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        /// <summary>收一个 `PostFxRequest`（= `WFModulePostProcess.OnPostFx` 的实现体）。</summary>
        public void Handle(PostFxRequest r)
        {
            Requests++;
            switch (r.op)
            {
                case PostFxOp.BloomAim: BloomAim(r); break;
                case PostFxOp.LutBlend: LutBlend(r); break;
                case PostFxOp.Reset: Reset(r.instant); break;
                case PostFxOp.SetOriginalLut: SetOriginalLut(); break;
            }
        }

        Texture Lut(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // 🔴 只能按名字取（模块给下游的就是名字）⇒ 贴图必须落在 `Resources/` 下。
            //    导入器：`工具/import_original_luts.py`（14 张，含尺寸自检）
            var t = Resources.Load<Texture2D>("WarpforgeVFX/LUT/" + name);
            if (t == null && !MissingLutTextures.Contains(name)) MissingLutTextures.Add(name);
            return t;
        }

        void BloomAim(PostFxRequest r)
        {
            if (_bloom == null) return;     // 原版外面也套了两层判空
            if (r.setIntensity) { _bloom.intensity.overrideState = true; _bloom.intensity.value = r.intensity; }
            if (r.setThreshold) { _bloom.threshold.overrideState = true; _bloom.threshold.value = r.threshold; }
        }

        /// <summary>原版 `DoLUTAnim()` + `DoBlend()`：先（首次）把两张 LUT 推上去、必要时合进 RT，
        /// 再按 `lutBlend` 把结果写进 `ColorLookup.texture`。</summary>
        void LutBlend(PostFxRequest r)
        {
            if (_mat == null || _cl == null) return;

            if (r.setTextures)              // 原版 `alreadySetLUTAnimTexture == false` 那一支
            {
                var t1 = Lut(r.lut1);
                if (t1 != null) _mat.SetTexture("_LUT1", t1);
                var t2 = Lut(r.lut2);
                if (t2 != null) _mat.SetTexture("_LUT2", t2);

                if (r.mergeLuts)
                {
                    // 原版 `SetTargetLUT(rt)`：先 `Blit(combinedLUT → lutToApplyRT)`，再把 lutToApplyRT 当 `_LUT2`
                    // ⚠️ 这一支要求 `_LUT1`/`_LUT2` **已经设好**（上面刚设）
                    _mat.SetFloat(WFModulePostProcess.BlendPropertyName, r.betweenLutBlend);
                    Graphics.Blit(null, _targetLutRt, _mat);
                    _mat.SetTexture("_LUT2", _targetLutRt);
                }
                // 两张 LUT 尺寸不符时**出声**（原版是直接拉伸填满 —— 我们照做，但不静默；
                // 实测 `LUT Overexpose High` 原版就是 64×4，见侦察正本 §3·7）
                WarnIfSizeOdd(t1, r.lut1);
                WarnIfSizeOdd(t2, r.lut2);
            }
            BlendTo(r.lutBlend);
        }

        readonly HashSet<string> _sizeWarned = new HashSet<string>();
        void WarnIfSizeOdd(Texture t, string name)
        {
            if (t == null || _sizeWarned.Contains(name)) return;
            if (t.width == WFModulePostProcess.LutMergeWidth && t.height == WFModulePostProcess.LutMergeHeight)
                return;
            _sizeWarned.Add(name);
            Debug.LogWarning($"[BattlePostFx] LUT `{name}` 是 {t.width}×{t.height}，而合并 RT 是 "
                           + $"{WFModulePostProcess.LutMergeWidth}×{WFModulePostProcess.LutMergeHeight}"
                           + " —— 原版的 shader 是**同 UV 采样** ⇒ 它会把这张**拉伸填满**；我们照做"
                           + "（实测 `LUT Overexpose High` 原版就是 64×4，不是我们导小的）。");
        }

        /// <summary>原版 `LUTBlender.DoBlend(v)`：`SetFloat("_Blend", v)` + `Blit(null → combinedLUT, mat)`，
        /// 结果就是 `lerp(_LUT1, _LUT2, v)`，再把它交给 `ColorLookup.texture`。</summary>
        public void BlendTo(float v)
        {
            if (_mat == null || _cl == null) return;
            _mat.SetFloat(WFModulePostProcess.BlendPropertyName, v);
            Graphics.Blit(null, _combined, _mat);
            _cl.texture.overrideState = true;
            _cl.texture.value = _combined;
            Blends++;
        }

        /// <summary>原版 `PostFXController.ResetLUT(instant)`：把目标 LUT 设回「原 LUT」。
        /// ⚠️ 原版这里会起一段 `timeToBlend`（instant=0 → `TIME_TO_RESET` = **1.0f**）的**动画**过渡；
        ///    我们**直接落位**（我们的 `_Blend` 过渡由模块每帧算，见文件头第 ③ 条），
        ///    并把 `ColorLookup.texture` 交还战场那张静态 LUT。</summary>
        void Reset(bool instant)
        {
            if (_cl != null)
            {
                _cl.texture.overrideState = true;
                _cl.texture.value = _original;      // 第 ① 条那张
            }
            if (_bloom != null)                     // 原版 `ResetBloom`
            {
                if (_bloomIntensity0 >= 0f) { _bloom.intensity.overrideState = true; _bloom.intensity.value = _bloomIntensity0; }
                if (_bloomThreshold0 >= 0f) { _bloom.threshold.overrideState = true; _bloom.threshold.value = _bloomThreshold0; }
            }
            _ = instant;                            // 见上面：过渡由模块负责，这里两条路一样
        }

        /// <summary>原版 `AnimEventSetOriginalLUTNoTransition()`：把 `_LUT1` 换回原 LUT，不过渡。</summary>
        public void SetOriginalLut()
        {
            if (_mat == null || _original == null) return;
            _mat.SetTexture("_LUT1", _original);
        }

        /// <summary>拆掉（切场景 / 重开一局）。原版 `OnDestroy` **只**销毁那两张 RT 与材质
        /// （**不复位** —— 复位在 `OnDisable`，那是 `Reset` 那条路的事）。</summary>
        public void Detach()
        {
            // 交还静态 LUT 再销毁 RT —— 否则 `ColorLookup.texture` 会指着一张已销毁的 RT（粉/黑屏）
            if (_cl != null) { _cl.texture.value = _original; _cl.texture.overrideState = false; }
            if (_combined != null) { _combined.Release(); Object.DestroyImmediate(_combined); _combined = null; }
            if (_targetLutRt != null) { _targetLutRt.Release(); Object.DestroyImmediate(_targetLutRt); _targetLutRt = null; }
            if (_mat != null) { Object.DestroyImmediate(_mat); _mat = null; }
            _cl = null; _bloom = null; _original = null;
        }
    }
}
