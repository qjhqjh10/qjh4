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
//     `ColorLookup` 槽**（⚠️ **2026-10-11 订正（A293）**：这里原来写「原版那几场静态 LUT **也是空的**」——
//     **那句是错的**，错因 = 只量了 `battlearena1` 一场就写成通用结论（铁律 5·c）。
//     **真相**：原版 **13/13 场都有 `ColorLookup`**（`active=1` · `contribution=1.0` ·
//     `texture.overrideState=1`，逐场现读），只是那 6 场指的那张是**共享的** `LUT Normal`
//     （`battlesharedresources`，**严格恒等**）；另外 **7 场**（aeldari · emperorschildren · genestealers ·
//     leviathan · sororitas · spacewolves · tauviorla）的 `m_FileID = 0` = **包内自带**一张 `LUT <场>`。
//     判据（判据只此一处）= 逐场读 `ColorLookup.texture.m_Value`：**`m_FileID=0` 的 7 场** /
//     **`m_FileID = 9`（`arena2` 是 10 —— 那是 externals 表下标，别当常量）+ `PathID 382974660631151556`
//     的 6 场**；旁证 = `bundle_scenes_scenes_<场>/Texture2D/` 里那 7 个包**各只有一张 LUT**、
//     而 `battlearena1` 那个包里**一张都没有**。见 `ArenaBuilder.ApplyPostFx` 的注释）
//     ⇒ 我们**构建时**没给这 6 场建那个槽（本地没有专属 LUT ⇒ 那边那支 `不接`，且**如实出声**）
//       ⇒ 在**运行时那份 profile 副本**上补一个 `ColorLookup`（见下面「为什么能安全改 profile」），
//       否则那 6 场的 LUT 特效**一点作用都没有**。⚠️ **这一刀原版没有对应动作**（原版不需要「补」：
//       它那 6 场的槽本来就在、贴的是恒等那张）⇒ 会计数 + 出声；**净状态与原版一致**。
//  ③ **`_Blend` 的过渡由模块自己算**（`WFModulePostProcess.TickBlendRamp`，因为批处理没有帧循环、
//     DOTween 推不动）—— 本文件只负责「拿到 `lutBlend` 就往下写」。
//  ④ **我们不建 `workingRT`**：原版两张 RT 是为了在两份 LUT 之间来回插值；我们的用法是
//     「`_LUT1` = 原 LUT（**只读**）、`_LUT2` = 目标 LUT」，**不需要那份中间拷贝**。
//     ⇒ 只建 `combinedLut`（= 原版 `combinedLUT`）+ `targetLutRt`（= 原版 `lutToApplyRT`）。
//
// 🔴 **为什么能安全改 profile**（⚠️ **2026-10-11 更正：本段原来写反了，错因值得记**）
//    **原文（错）**：「`Volume.profile` 的 getter 在**没有实例时返回的是 `sharedProfile`**；
//      ⇒ `Attach` 里先 `vol.profile = vol.sharedProfile` **强制出一份实例副本**」。
//    **实际（本机真源码逐行读的）**：`com.unity.render-pipelines.core@0bb36005e9ba/Runtime/Volume/Volume.cs`
//      · **建副本的是 getter（`:79-98`）** —— `m_InternalProfile == null` 时 `CreateInstance<VolumeProfile>()`，
//        再把 `sharedProfile.components` 逐个 `Instantiate` 一份（`overrideState` 是 `[SerializeField]`，
//        `Instantiate` 会带上 ⇒ 副本**保真**，见 `VolumeParameter.cs:46`）；
//      · **setter 是裸赋值（`:99`）** `set => m_InternalProfile = value;`
//        ⇒ 原来那句 `vol.profile = vol.sharedProfile` **一份副本都没建**，等于把 Volume **直接指回工程资产**，
//          之后所有写（`_cl.active` / `contribution.overrideState` / `Add<ColorLookup>` / `_cl.texture.*`）
//          **全落在资产上**。
//    **它是怎么被抓出来的**：`Editor/BattleScene.cs:8749-8774` 的逐场对账（A243）报出
//      `battlearenaaeldari` / `battlearenaemperorschildren` 的 `ColorLookup` 「没打 override」——
//      写这条的是 `Detach` 里那句 `_cl.texture.overrideState = false`（全仓唯一一处写 false）。
//      ⚠️ **它只脏内存、不落盘** ⇒ 盘上仍是 `m_OverrideState: 1`、重启进程后又「自己好了」，
//         **光读盘永远读不出矛盾** —— 这正是它从 2026-10-01 起一直没被发现的原因。
//    **正确写法**：`vol.profile = vol.profile;`（RHS 先过 getter ⇒ 副本先建出来，再赋回去）。
//      写法故意选「赋回自己」：它**看着就像在要一份副本**，下个会话不会当成冗余删掉。
//    ⚠️ 光那一句还不够（`sharedProfile` 被谁换进来都一样坏）⇒ `Attach` 里配了一条**身份判据**
//      （`prof` 不许 === `vol.sharedProfile`）：真指回资产时**出声 + 计数 + 拒绝往下写**，
//      而不是把资产写脏之后再指望自检去发现。
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
        /// <summary>🔴 拿到的 profile 是**共享资产**而不是副本 ⇒ **拒绝往下写**（见文件头那条更正）。
        /// 正常恒为 0；非 0 = 有人把 `Attach` 里那句 `vol.profile = vol.profile` 改回去了。</summary>
        public int SharedProfileRefused;
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
        VolumeProfile _prof;              // 我们那份**副本**（`Detach` 要靠它摘掉自己补的槽）
        bool _addedCl;                    // 那个 `ColorLookup` 是**我们补的**（进场时本来没有，第 ② 条）
        bool _clActive0;                  // 进场那一刻 `ColorLookup` 的状态 —— `Detach` 按它原样还回去
        bool _texOverride0;
        bool _contributionOverride0;
        float _contribution0;
        Bloom _bloom;
        Texture _original;
        float _bloomIntensity0 = -1f, _bloomThreshold0 = -1f;

        /// <summary>接到战场的全局 `Volume` 上（原版 `PostFXController.Start` 那一段）。
        /// 返回 false = 这一局没有后期链（**会计数**）。</summary>
        public bool Attach(Volume vol)
        {
            if (vol == null) { MissingVolume++; return false; }

            // 🔴 拿一份**只属于这个 Volume 的运行时副本** —— 走的是 **getter**（`Volume.cs:79-98`）那一侧，
            //    ⛔ 不是 setter（`:99` 是裸赋值，见文件头「为什么能安全改 profile」）。
            //    写法故意选「赋回自己」：它看着就像在要一份副本，下个会话不会当成冗余删掉。
            //    （RHS 先求值 ⇒ 副本先建出来，再赋回去；之后再读到的都是这一份。）
            vol.profile = vol.profile;
            var prof = vol.profile;
            if (prof == null) { MissingVolume++; return false; }

            // 🔴 **身份判据**（盯的是「**写出去的那份东西还是不是工程资产**」，⛔ 不是我们自己的常量）：
            //    `Volume.profile` 的 getter 在 `m_InternalProfile == null` 时 `CreateInstance` 一份
            //    ⇒ 它与 `sharedProfile` **恒不同引用**（`sharedProfile` 为空时 `prof` 是一份空副本，也不相等）。
            //    **引用相等 ⇔ 有人把 `m_InternalProfile` 指回了资产**（上面那句被改回 `vol.sharedProfile` 就是）。
            //    ⚠️ 落到这一支就**出声 + 计数 + 拒绝往下写**：宁可不接 LUT，也不许把
            //      `arenas/*/Profiles/*_PostFx.asset` 写脏 —— 那种脏**只存在内存里**、查不出来（见文件头）。
            //    改坏法：把上面那句退回 `vol.profile = vol.sharedProfile;` ⇒ 这里立刻 LogError + 计数，
            //      `Attach` 返回 false ⇒ 自检「★ 后期下游就绪」（`BattleScene.cs:3449`）当场红。
            if (ReferenceEquals(prof, vol.sharedProfile))
            {
                SharedProfileRefused++;
                Debug.LogError("[BattlePostFx] 🔴 `Volume.profile` 拿到的还是**共享 profile 资产**本身"
                             + $"（`{prof.name}`）—— 拒绝往下写（写下去会把工程资产弄脏，且只脏内存、"
                             + "读盘读不出来）。去本文件头「为什么能安全改 profile」那条更正看改法。");
                return false;
            }

            _prof = prof;
            if (!prof.TryGet<ColorLookup>(out _cl))
            {
                // 第 ② 条那种战场：**我们构建时没给那个槽**（原版那 6 场**有** `ColorLookup`，只是贴的是
                // 共享的**恒等** `LUT Normal` ⇒ 见文件头第 ② 条那条订正）⇒ 在**副本**上补一个
                // （副本不是资产 ⇒ 资产不受影响；这个槽由 `Detach` 收掉，见那里）
                _cl = prof.Add<ColorLookup>(true);
                _addedCl = true;
                MissingColorLookup++;      // ⚠️ 计数：这一刀原版没有对应动作（净状态与原版一致）
            }

            // 🔴 **记下进场那一刻的状态** —— `Detach` 要**原样还回去**（⛔ 不许无条件置 `false`：
            //    `texture.overrideState` 原版 **13/13 场都是 1**（逐场现读，含那 7 场专属 LUT），
            //    置 false 等于「撤走时把 LUT 关了」）。
            //    ⚠️ 必须记在下面任何一句写之前。
            _clActive0 = _cl.active;
            _texOverride0 = _cl.texture.overrideState;
            _contributionOverride0 = _cl.contribution.overrideState;
            _contribution0 = _cl.contribution.value;

            _cl.active = true;             // 生效是被 `_Blend` 推的，见 `BlendTo`
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
            if (_cl != null)
            {
                _cl.texture.value = _original;                     // 第 ① 条那张（进场时那张静态 LUT）
                // 🔴 **按进场那一刻的状态还回去**（改坏法：无条件置 `false`）。
                //    原来那句无条件 `overrideState = false` 在「写的是副本」时无害、在「写的是资产」时
                //    把 7 场静态 LUT 从 Volume 栈上掀掉了（`VolumeComponent.Override` 只搬打勾的参数）
                //    ⇒ 那一局之后该战场**不再把 LUT 推上栈**，而这「≠ 交还静态 LUT」。
                _cl.texture.overrideState = _texOverride0;
                _cl.contribution.value = _contribution0;
                _cl.contribution.overrideState = _contributionOverride0;
                _cl.active = _clActive0;

                // 第 ② 条补出来的那个 ⇒ **连组件一起收掉**（进场时本来没有）。
                // 两个理由：① 留着会让下一局 `Attach` 的 `MissingColorLookup` 少算一次
                // （那个计数是「按局计的补槽次数」，见 `BattleDriver` 那条日志）；
                // ② 它是**运行时造出来、没人管**的 `ScriptableObject`（不是资产的子资产、也没 `DontSave`）
                // ⇒ 生命周期不由我们说了算。⚠️ 顺带记一条**未坐实的观测**（不写正本、只写在这儿）：
                // 本文件头那次外溢在实测里留下过一个「`TryGet` 能命中、但 `cl != null` 为假」的条目
                // （`BattleScene.cs:8949` 的 `has` 因此判成 false）—— 最像的解释就是这种没人管的组件
                // 在切场景时被收走了。**修完后这条路不再存在**：谁造的就由谁在这里收掉。
                if (_addedCl && _prof != null)
                {
                    // 走**公开 API**（`VolumeProfile.Remove` 顺带置 `dirtyState`）——
                    // ⚠️ 别只 `components.Remove`：`VolumeManager.OverrideData`（`VolumeManager.cs:640-655`）
                    //    每帧**现读**这份 `components` 并解引用 `component.active` ⇒ 留着一个**已销毁**的
                    //    条目在那儿，下一帧就是 MissingReference。
                    _prof.Remove(typeof(ColorLookup));
                    Object.DestroyImmediate(_cl);      // `Remove` 只摘引用、**不销毁对象** ⇒ 这一步得自己做
                                                       //（批处理无帧循环 ⇒ 只能 `DestroyImmediate`，CLAUDE.md §三）
                }
                _cl = null;                    // （下面还有一次，是「没走到这一支」时的兜底）
            }
            if (_combined != null) { _combined.Release(); Object.DestroyImmediate(_combined); _combined = null; }
            if (_targetLutRt != null) { _targetLutRt.Release(); Object.DestroyImmediate(_targetLutRt); _targetLutRt = null; }
            if (_mat != null) { Object.DestroyImmediate(_mat); _mat = null; }
            _cl = null; _bloom = null; _original = null;
            _prof = null; _addedCl = false;
        }
    }
}
