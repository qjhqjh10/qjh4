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
//    **它是怎么被抓出来的**：`Editor/BattleScene.cs` 的 `Run` 里那节逐场 `ColorLookup` 对账 的逐场对账（A243）报出
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
using WarpforgeVFX; using DG.Tweening;   // ⚠️ 必须 `using DG.Tweening`：`SetEase` / `IsActive` / `Complete` / `Elapsed` 都是**扩展方法**（同 `BattleCameraSreenSize.cs:89`）

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

        // ---- 🔴 2026-10-25（D 域 · 完全复刻第 1 轮 · 原版 `AnimFXModulePostProcess.Update` 末句）----
        //   原版每帧最后一句：`camera.UpdateVolumeStack()`（游戏相机 = `BattleManager.boardCamera`）。
        //   **为什么必须有这一句**：URP 只在 `requiresVolumeFrameworkUpdate == true` 时每帧刷 Volume 栈，
        //   而它在 `m_VolumeFrameworkUpdateModeOption == ViaScripting(2)` 时为 **false**
        //   （`UniversalAdditionalCameraData.cs:723-734`）⇒ 管线只建一次本地栈并缓存
        //   （`UniversalRenderPipeline.cs:1258-1292`）。我们 `Scenes/Battle.unity` 的**两台相机都是 2**
        //   （`:1043` / `:2761`）⇒ 写进 `VolumeProfile` 的 Bloom / LUT 值**上不了屏**；
        //   而编辑器/批处理（`isPlaying == false`）URI 会强制每帧刷 ⇒ **自检截图看着正常、真 player 才是坏的**。
        //   ⛔ **不要改相机上的 `VolumeFrameworkUpdateModeOption`** —— 那是原版序列化值。
        Camera _boardCam;
        System.Action _flushVolumeStack;      // 本实例挂上去的那一个（`Detach` 只摘自己的）
        /// <summary>没找到可用相机而**没接上**刷新的次数（按局计）。>0 = 后期动画这一局上不了屏。</summary>
        public static int MissingBoardCamera { get; private set; }

        /// <summary>接到战场的全局 `Volume` 上（原版 `PostFXController.Start` 那一段）。
        /// 返回 false = 这一局没有后期链（**会计数**）。
        /// <param name="boardCamera">原版那句 `UpdateVolumeStack` 的目标相机（= `BattleManager.boardCamera`）。
        /// 传 null ⇒ 自己找：优先 `CameraRenderType.Base` 那台（URP 里唯一一台 base，就是战场相机），
        /// 退 `Camera.main`。**找不到会出声 + 计数**，不会静默少这一跳。</param></summary>
        public bool Attach(Volume vol, Camera boardCamera = null)
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
            //      `Attach` 返回 false ⇒ 自检「★ 后期下游就绪」（`Editor/BattleScene.cs` 的 `Run` 里「★ 后期下游就绪」那条）当场红。
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

            WireVolumeStackFlush(boardCamera);                          // 原版 `Update` 末句那一跳
            return _mat != null;
        }

        /// <summary>把「每帧刷一次 Volume 栈」接到模块那侧（见上面 `_boardCam` 那段判据）。</summary>
        void WireVolumeStackFlush(Camera boardCamera)
        {
            UnwireVolumeStackFlush();                                   // 重入（同一局重开）先摘旧的
            _boardCam = boardCamera != null ? boardCamera : ResolveBoardCamera();
            if (_boardCam == null)
            {
                MissingBoardCamera++;
                Debug.LogWarning("[BattlePostFx] 🔴 没找到可刷 Volume 栈的相机 ⇒ 原版 "
                               + "`AnimFXModulePostProcess.Update` 末句 `camera.UpdateVolumeStack()` **这一局没接上**。"
                               + "我们两台相机都是 `ViaScripting` ⇒ URP 不会自己刷 ⇒ "
                               + "**特效里的 Bloom / LUT 动画可能上不了屏**。");
                return;
            }
            var cam = _boardCam;
            // `VolumeManager.instance` 没初始化时 URP 会 `LogError` 并 return（`UniversalAdditionalCameraData.cs:262`）
            // ⇒ 先挡一下，免得每帧刷一条红字。
            _flushVolumeStack = () =>
            {
                if (VolumeManager.instance != null && VolumeManager.instance.isInitialized) cam.UpdateVolumeStack();
            };
            WFModulePostProcess.OnFlushVolumeStack = _flushVolumeStack;
        }

        void UnwireVolumeStackFlush()
        {
            if (_flushVolumeStack != null && WFModulePostProcess.OnFlushVolumeStack == _flushVolumeStack)
                WFModulePostProcess.OnFlushVolumeStack = null;
            _flushVolumeStack = null;
            _boardCam = null;
        }

        /// <summary>找战场那台相机：URP 里 `CameraRenderType.Base` **只有一台**，就是它；
        /// 取不到再退 `Camera.main`。两样都没有 ⇒ null（调用方出声 + 计数）。</summary>
        static Camera ResolveBoardCamera()
        {
            var cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c == null) continue;
                var uad = c.GetComponent<UniversalAdditionalCameraData>();
                if (uad != null && uad.renderType == CameraRenderType.Base) return c;
            }
            return Camera.main;
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

        /// <summary>原版 `DoLUTAnim()` + `DoBlend()`：先（首次）把两张 LUT 推上去、必要时合进 RT，再按 `lutBlend` 把结果写进 `ColorLookup.texture`。
        /// ⚠️ 复位斜坡在走时丢掉「空闲那一发」（`lutBlend == 0`），否则它会把 `ResetLUT` 的 1.0 s 过渡当场盖掉（见类尾 `ResetLutCore`）；在播的效果照旧放行。</summary>
        void LutBlend(PostFxRequest r)
        {
            if (_mat == null || _cl == null || (_lutRampActive && r.lutBlend == 0f)) return;   // ← 后者见 doc（别删）

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
            Blends++; _lastBlend = v;        // `_lastBlend` = 读口 + `ResetLutCore` 的起点（见类尾那段）
        }

        /// <summary>原版 `PostFXController.ResetAll(bool instant)`（🥇 `decomp_full/PostFXController__ResetAll.c`
        /// = `ResetBloom(instant)` + `ResetLUT(instant)` 两句转调）。两件的**机制**都收在**类尾**
        /// （<see cref="ResetBloomCore"/> / <see cref="ResetLutCore"/>，判据出处与两侧数值写在那一段），
        /// 这里保持原版那一层的「纯转发」。
        /// <para>判据落点：`ResetBloom` = `__ResetBloom.c:14-47`（instant=0 ⇒ `DOTween.Kill` + 两条 `DOTween.To(→ 缓存原值, 1.0 s)`；
        /// 否则逐件判空后直接写回）；`ResetLUT` = `__ResetLUT.c:13-37`（`timeToBlend = instant ? 0 : TIME_TO_RESET`，
        /// `LUTBlender` 为空则原版 NRE）+ `LUTBlender__Update.c:8-21`（`currentTime += dt; DoBlend(currentTime / timeToBlend)`，到点自关）。</para>
        /// <para>⚠️ 我们这条链上还有两个**既有偏离**（文件头 ③ ④ 已写）：`_Blend` 由模块每帧算、且不建 workingRT。
        /// 两者都不影响「Reset 之后画面 = 原 LUT」这个净结果（`_Blend → 1` ⇒ `lerp(_LUT1,_LUT2,1) = _LUT2 = 原 LUT`）。</para>
        /// <para>观测量（自检读口）= <see cref="BloomIntensityNow"/> / <see cref="BloomThresholdNow"/> /
        /// <see cref="ResetBloomRamp01"/> / <see cref="ResetLutRamp01"/>（原版侧期望值见类尾那段）。</para></summary>
        void Reset(bool instant)
        {
            ResetBloomCore(instant);
            ResetLutCore(instant);
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
            UnwireVolumeStackFlush();      // 原版 `Update` 末句那一跳的接线（见 `_boardCam` 那段）
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
                // （`Editor/BattleScene.cs` 的 `Run` 里 `bool has = prof != null && prof.TryGet(out cl) …` 那一句（`has` 因此判成 false））—— 最像的解释就是这种没人管的组件
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
            if (_bloomTweenI != null) _bloomTweenI.Kill(); if (_bloomTweenTh != null) _bloomTweenTh.Kill(); if (_lutTween != null) _lutTween.Kill(); _lutRampActive = false; _lutRampT = 0f;
        }

        // ==================================================================
        //  🆕 2026-10-26（执行代理 `B8_AF`）：原版 `ResetBloom` / `ResetLUT` 的 **1.0 s 过渡**
        // ==================================================================
        //  判据（🥇 `decomp_full/` 2026-10-26 现读；🥈 `cpp2il/out_cpp2il_dev_ilspy/PostFXController.cs:128-186` 同体）：
        //   · `PostFXController__ResetAll.c` = `ResetBloom(instant)` + `ResetLUT(instant)` 两句（本类 `Reset` 就是它）。
        //   · `PostFXController__ResetBloom.c`：
        //       instant=0 ⇒ `DOTween.Kill(bloom,0,0)` → 两条 `DOTween.To(getter, setter, 目标, TIME_TO_RESET)`
        //         （目标 = `*(this+0x38)` / `*(this+0x3c)` = `bloomOriginalIntensity` / `bloomOriginalThreshold`，
        //          即 `Start` 里缓存的那两个原值；**没有 SetEase** ⇒ 走 DOTween 全局默认缓动）；
        //       instant=1 ⇒ 先写 `intensity`（`bloom+0x48` 判空）、再写 `threshold`（`bloom+0x40` 判空），
        //         **任一为空原版抛 NRE**（`FUN_1803f47a0`）。
        //   · `PostFXController__ResetLUT.c`：`timeToBlend = instant ? 0 : TIME_TO_RESET`；`LUTBlender == null` ⇒ 原版 NRE；
        //       否则 `SetTargetLUT(originalLUTTexture /*+0x50*/)` → `timeToBlend(0x6c)` → `currentTime(0x64)=0`
        //       → `doAnimatedBlend(0x68)=1` → `enabled=1`。
        //   · `LUTBlender__SetTargetLUT.c`：`Blit(combined(+0x40) → working(+0x48))` + `SetTexture(mat,"_LUT2",目标)`。
        //   · `LUTBlender__Update.c`：`currentTime += Time.deltaTime; DoBlend(currentTime / timeToBlend)`；
        //       到点 ⇒ `doAnimatedBlend=0` + `enabled=0`。**这是一段手写线性斜坡，不是 tween**。
        //   · `LUTBlender__DoBlend.c`：v 夹到 [0,1]（上界 `0x1834b2bb8` = 1.0f）→ `SetFloat("_Blend", v)` → `Blit(null→combined)`。
        //   · `PostFXController..cctor.c`：`TIME_TO_RESET = 0x3f800000` = **1.0f**。
        //  ⇒ **Bloom 用真 `DOTween.To`**（本工程有 DOTween：`Assets/Plugins/Demigiant/DOTween`；
        //    `Resources/DOTweenSettings.asset` 的 `defaultEaseType = 6` = `OutQuad`，与原版「不写 SetEase」一致；
        //    同一份 DOTween 已由 `Battle/BattleCameraSreenSize.cs` 在用）。
        //    **LUT 用一段手写线性斜坡**（原版就是 `currentTime / timeToBlend`，⛔ 别改成 tween —— 那会带上缓动）。
        //  ⚠️ 批处理里没有帧循环 ⇒ 两条 tween 都不会自己推进；自检要读数就调 `CompleteResetRamps()`
        //    （同 `BattleCameraSreenSize._sensorTween/_lensTween` 的处置）。
        //  🔴 **不是「原版自己也有 bug」那一类**：两条都照原版的时长 / 曲线 / 净结果，没有改语义。

        /// <summary>原版 `PostFXController.TIME_TO_RESET`（`..cctor`：`0x3f800000` = **1.0f**）。</summary>
        public const float TimeToReset = 1f;

        DG.Tweening.Tween _bloomTweenI, _bloomTweenTh;   // 原版 `ResetBloom` 那两条 `DOTween.To`（自检可 `Complete` 跑完）
        bool _lutRampActive;
        float _lutRampT;                                 // 原版 `LUTBlender.currentTime`（0 → TimeToReset）
        DG.Tweening.Tween _lutTween;                     // 只当 `currentTime` 的泵（线性），把 `BlendTo` 推起来
        float _lastBlend = -1f;                          // 最近一次写进 `_Blend` 的值（读口 + 判断是否已写过）
        int _lutRampFallback;                            // 「要过渡但没有材质/原 LUT」⇒ 退回直接落位的次数（不静默）

        /// <summary>原版 `ResetBloom(bool instant)`。`instant=false` ⇒ 两条 **1.0 s** 的 `DOTween.To`
        /// （intensity / threshold 各一条，终点 = `Start` 缓存的 `bloomOriginal*`，**不写 SetEase** = 全局默认 `OutQuad`）。</summary>
        void ResetBloomCore(bool instant)
        {
            if (_bloom == null) return;      // 原版这一支会走到 `FUN_1803f47a0`（NRE）；我们按「这一局没有 Bloom」处理
            if (!instant)
            {
                // 原版 `DOTween.Kill(bloom, 0, 0)`：那两条 tween **没有 target**（`TweenerCore.target == null`），
                // 所以这句其实杀不到东西 —— 照抄，⛔ 别当冗余删（同 `BattleCameraSreenSize` 文件头 F 那条）。
                DG.Tweening.DOTween.Kill(_bloom);
                _bloomTweenI = DG.Tweening.DOTween.To(
                    () => _bloom.intensity.value, x => _bloom.intensity.value = x, _bloomIntensity0, TimeToReset);
                _bloomTweenTh = DG.Tweening.DOTween.To(
                    () => _bloom.threshold.value, x => _bloom.threshold.value = x, _bloomThreshold0, TimeToReset);
                return;
            }
            // instant=1：逐件判空后直接写回（原版 `bloom+0x48` / `bloom+0x40`；`overrideState` 是我们既有的写法，见 `BloomAim`）
            if (_bloomIntensity0 >= 0f) { _bloom.intensity.overrideState = true; _bloom.intensity.value = _bloomIntensity0; }
            if (_bloomThreshold0 >= 0f) { _bloom.threshold.overrideState = true; _bloom.threshold.value = _bloomThreshold0; }
        }

        /// <summary>原版 `ResetLUT(bool instant)`：`instant=false` ⇒ `_Blend` 从 **0** 线性推到 **1**（1.0 s，
        /// `LUTBlender.Update` 那条手写斜坡），画面 = `lerp(当前基准 LUT, 原 LUT, v)`；`instant=true` ⇒ 当场落到原 LUT。
        /// <para>🔴 **我们那份「快照」是隐式的**：原版 `SetTargetLUT` 先把 combined 拷进 working（`_LUT1` 的宿主），
        /// 但模块在每次效果起手时都会把 `_LUT1` 改写成 `customLUT1`（`AnimFXModulePostProcess.cs:224-231`）
        /// ⇒ **真正参与插值的是「当前基准 LUT」，不是那份快照**。我们的 `_LUT1` 一直是「当前基准」
        /// （`LutBlend` 的 `setTextures` 支负责写、平时不动）⇒ **净等价，不需要 workingRT**（与文件头第 ④ 条一致）。</para></summary>
        void ResetLutCore(bool instant)
        {
            if (_cl == null) return;         // 原版 `LUTBlender != null` 那道守卫（为空原版 NRE）
            if (instant || _mat == null || _original == null)
            {
                if (!instant && _lutRampFallback++ == 0)
                    Debug.LogWarning("[BattlePostFx] `ResetLUT(instant:false)` 该走 1.0 s 过渡，但 "
                                   + (_mat == null ? "`Hidden/LUTBlender` 材质没解析到" : "`originalLUTTexture` 是 null")
                                   + " ⇒ 这一支退回**直接落位**（不静默，只报一次）。");
                _lutRampActive = false;
                _lutRampT = 0f;
                _cl.texture.overrideState = true;
                _cl.texture.value = _original;      // 原版走到那一步的**净结果**也是这张
                if (_mat != null && _original != null)
                {
                    // `_LUT1`/`_LUT2` 都归位成原 LUT + `_Blend = 1` ⇒ 之后任何一次 `BlendTo(0)`
                    // （模块空闲时每帧都会来一发）得到的还是原 LUT —— 不会把刚复位的结果又掀回去。
                    _mat.SetTexture("_LUT1", _original);
                    _mat.SetTexture("_LUT2", _original);
                    BlendTo(1f);
                }
                return;
            }
            _mat.SetTexture("_LUT2", _original);    // 原版 `SetTargetLUT(originalLUTTexture)` 的第二句
            _lutRampActive = true;
            _lutRampT = 0f;
            BlendTo(0f);                            // 原版 `Update` 的第一帧 ≈ `DoBlend(dt / 1.0) ≈ 0`
            // 用一条**线性** tween 当 `currentTime` 的泵（原版是 `LUTBlender` 自己 `Update` 累加，我们没建那件 MonoBehaviour）
            if (_lutTween != null) _lutTween.Kill();
            _lutTween = DG.Tweening.DOTween
                .To(() => _lutRampT,
                    x => { _lutRampT = x; if (_lutRampActive) BlendTo(Mathf.Clamp01(x / TimeToReset)); },
                    TimeToReset, TimeToReset)
                .SetEase(DG.Tweening.Ease.Linear)
                .OnComplete(LutRampComplete);
        }

        /// <summary>原版 `LUTBlender.Update` 的收尾（`currentTime >= timeToBlend` ⇒ `doAnimatedBlend=0` + `enabled=0`）。</summary>
        void LutRampComplete()
        {
            _lutRampActive = false;
            _lutRampT = TimeToReset;
            BlendTo(1f);
            if (_mat != null && _original != null) _mat.SetTexture("_LUT1", _original);   // 过渡完 = 画面就是原 LUT ⇒ 基准也归位
        }

        /// <summary>把两段过渡**手动跑完**（批处理/自检里没有帧循环，`DOTween` 不推进 ⇒ 用它读数）。
        /// 只给自检与无帧循环那一路用，⛔ 生产路径别调（真机上它们自己会走完）。</summary>
        public void CompleteResetRamps()
        {
            if (_bloomTweenI != null && _bloomTweenI.IsActive()) _bloomTweenI.Complete();
            if (_bloomTweenTh != null && _bloomTweenTh.IsActive()) _bloomTweenTh.Complete();
            if (_lutTween != null && _lutTween.IsActive()) _lutTween.Complete();
            if (_lutRampActive) LutRampComplete();     // tween 已被 Kill/丢掉时的兜底
        }

        // ---- 自检读口：⛔ 别读我们自己的 bool 当观测量；这几个读的是**真落到 Volume / 材质上的值** ----
        /// <summary>Bloom intensity 的**现值**（= 原版 `bloom.intensity.value`；没有 Bloom ⇒ NaN）。</summary>
        public float BloomIntensityNow { get { return _bloom != null ? _bloom.intensity.value : float.NaN; } }
        /// <summary>Bloom threshold 的**现值**（同上）。</summary>
        public float BloomThresholdNow { get { return _bloom != null ? _bloom.threshold.value : float.NaN; } }
        /// <summary>Bloom 那段过渡的进度 0..1（tween 不在 ⇒ 1）。原版侧期望：`ResetBloom(false)` 后 0→1 走 1.0 s。</summary>
        public float ResetBloomRamp01
        {
            get
            {
                if (_bloomTweenI != null && _bloomTweenI.IsActive())
                    return Mathf.Clamp01(_bloomTweenI.Elapsed() / TimeToReset);
                return 1f;
            }
        }
        /// <summary>LUT 那段过渡的进度 0..1（没在走 ⇒ 1）。原版侧期望：`ResetLUT(false)` 后 0→1 走 1.0 s（线性）。</summary>
        public float ResetLutRamp01 { get { return _lutRampActive ? Mathf.Clamp01(_lutRampT / TimeToReset) : 1f; } }
        /// <summary>最近一次写进 `_Blend` 的值（-1 = 还没写过）。</summary>
        public float BlendNow { get { return _lastBlend; } }
    }
}
