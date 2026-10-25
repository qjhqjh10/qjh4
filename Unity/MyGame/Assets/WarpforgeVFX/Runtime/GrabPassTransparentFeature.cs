// GrabPassTransparentFeature.cs — 给 URP 填上原版的全局纹理 `_GrabPassTransparent`
// （2026-09-13 第三十三轮，P1-a0 的收尾）
//
// ==================================================================
//  为什么需要它
// ==================================================================
// 原版 `Everguild/FX/Particle Distortion Affect Transparents` 读的是**全局纹理
// `_GrabPassTransparent`**，**URP 没有这一张**。
// 证据：原版 HLSL 源码确实被剥了，但 `Shader.compressedBlob` 解出来是 DXBC、资源名是明文
// （`工具/dump_shader_blob.py`）。名字里的 *Affect Transparents* 也说明它拷的是
// **含透明物**的颜色缓冲。
//
// 🔴🔴 **2026-10-22（W64 · 兜底审计 B14）就地订正（铁律 5）—— 原来这里写着
//    「这是 Built-in RP 的 GrabPass 留下的东西」「原版 GrabPass 抓的是画到那个物体那一刻的内容」
//    【两条都不成立】**：
//    · 原版那个 shader **根本没有 GrabPass** —— 它的 3 个 pass 里 `m_GrabPass = None`（逐 pass 实读
//      `Shader.m_ParsedForm.m_SubShaders[0].m_Passes[*].m_GrabPass`）；
//    · 真正灌这张全局纹理的是**原版自己的 URP Feature** `GrabScreenFeature`（三个版本：
//      `GrabScreenFeature` / `GrabScreenFeatureU6` / `GrabScreenFeatureRenderGraphAPI`），
//      方法体在 `d:/2/tools/decomp_full/GrabScreenFeature*.c`。
//    逐条实读（`GrabScreenFeatureRenderGraphAPI__Create.c` / `…RenderPass__.ctor.c`）：
//      ① `GrabPass`（拷屏）：`renderPassEvent = **500**`（= `RenderPassEvent.AfterRenderingTransparents`）；
//         在 `OnCameraSetup` 里按 `cameraTargetDescriptor × textureSizeMultiplier` 分配一张 RT，
//         `Shader.SetGlobalTexture(settings.TextureName, rt)`；`Execute` 就一次
//         `ScriptableRenderPass.Blit(cmd, 源, 目标)` —— **整帧一次拷贝**。
//      ② `RenderPass`（重画）：`renderPassEvent = **0x1f5 = 501**`（刚好在拷屏之后一格），
//         用 `FilteringSettings(RenderQueueRange.all(), settings.LayerMask)` +
//         `RenderStateBlock{ depthState = (ZWrite off, ZTest LEqual) }` 把**抓屏那一层的物体再画一遍**。
//    ⇒ **原版 = 「透明物画完 → 整帧拷一次 → 再把扭曲物画一遍」**，不是逐物体抓屏。
//      这也解释了为什么它要 `LayerMask`：那些物体**不在**默认透明 pass 里画（否则会画两遍）。
//
// ✅✅ **2026-10-24（W64 · B14 的第二半 · 代理 B5）：「两步改法」【已落地】** ✅✅
//    （原来这一段写的是「❌ 缺的那一步 = RenderPass（501 重画）」，并给出两步改法
//      「① WFDistortion.shader 换自定义 LightMode ② 加第二个 pass 画它」 ——
//      🔴 **那两步是当时的猜测，现已作废**：原版**没有换 LightMode**，也没有新标签。判据见下。）
//
//    判据（逐条实读 `d:/2/tools/decomp_full/`，文件名 + 方法名可复查）：
//      · `GrabScreenFeatureRenderGraphAPI__Create.c` —— 两个 pass 都在 `Create()` 里建；
//        `…__AddRenderPasses.c` —— 两个都 `EnqueuePass`（**同一次相机渲染里先 500 后 501**）。
//      · `GrabScreenFeatureRenderGraphAPI.RenderPass__.ctor.c`（= 501 那一步的设置，逐字）：
//          `*(param_1 + 0x10) = 0x1f5;`                      ⇒ renderPassEvent = **501**
//          `ShaderTagId___ctor(&local_d8, DAT_1842d2738, 0)` ⇒ **三条** ShaderTagId，逐个 Add
//          `RenderQueueRange__get_all()`                      ⇒ RenderQueueRange.**all**
//          `FilteringSettings___ctor(…, uVar4 /*settings+0x18*/, 0xffffffff, 0, 0)`
//                                                             ⇒ layerMask = Settings.**LayerMask**、
//                                                               renderingLayerMask = 0xffffffff
//          `RenderStateBlock___ctor(&local_a8, 4)`            ⇒ mask = **RenderStateMask.Depth**
//          `DepthState___ctor(local_res20, 0, 4)`             ⇒ writeEnabled = **false**、compare = **4 = LessEqual**
//          `local_1e8 = 0x17;`                                ⇒ SortingCriteria = **23 = CommonTransparent**
//      · 那**三条 ShaderTagId 的名字**是 il2cpp 字符串字面量槽（`DAT_1842xxxxxx`）。
//        解法（可复查）：槽地址 − ImageBase(0x180000000) 就是 `d:/2/tools/all_strings.txt` 的行号键 ——
//          0x42d2738 → **`SRPDefaultUnlit`** · 0x4260608 → **`UniversalForward`** · 0x42aceb8 → **`LightweightForward`**
//        （同一张表里 0x42d38e8 → `_GrabPassTransparent`，正好是 `Settings.TextureName` ⇒ 解法自证）。
//      · `GrabScreenFeatureRenderGraphAPI.RenderPass__RecordRenderGraph.c` —— 501 那一步的执行：
//        `GetRendererListDesc(cullResults, CreateDrawingSettings(三条 tag, …, 0x17), FilteringSettings)`
//        → `CreateRendererList` → `AddRasterRenderPass` → 颜色挂 `activeColorTexture`、深度挂 `activeDepthTexture`
//        → `UseRendererList` → `DrawRendererList`。**与下面 `GrabRedrawPass` 一一对应。**
//
//    ⇒ **原版 = 单 pass 画物件，靠【图层】分工**：扭曲物**在** `Settings.LayerMask` 那一层，
//      而那一层**不在** URP 默认透明 pass 的 `m_TransparentLayerMask` 里（`UniversalRenderer.cs:430`
//      把 `data.transparentLayerMask` 传进 `DrawObjectsPass`）⇒ 默认 pass 不画它、501 才画它。
//      ⚠️ 这条是**推断**（原版的 `UniversalRendererData` 资产本地没解出来，见文件尾 §开启前置），
//         但它是唯一的自洽解释 —— 否则 501 会把**所有** forward 标签的透明物再画一遍。
//
//    ⛔ **500 那一次一个字节没动**（`GrabPassPass` 原样保留）；**500 常开、501 默认关**（见文件尾 §开关与前置）。
//
// ✅ **2026-10-25（`B14` · `X10`）：501 启用所需的【两步前置】也做成代码 + 开关了**（默认仍关）：
//    ① URP 默认透明 pass 的 `m_TransparentLayerMask` **清掉那一层**（运行期做，见 `GrabScreen501`）；
//    ② 用 `Everguild/FX/Particle Distortion Affect Transparents` 的那批效果 **搬到那一层**
//       （`WarpforgeEffectPlayer.Play` 里一处收口；名单 = prefab 自己带的 `WarpforgeEffectBinder.materials[].shader`
//        ＋运行时材质名，**不写死条数、不手抄名单**）；
//    ③ 外加原版的 **拷屏 RT ×0.25**（`GrabPassPass` 里一行，判据见那一段注释）。
//    ⛔ 全部藏在 `WF_GRAB501` 后面：**不设 / =0 ⇒ 这三件一件都不执行**，代码路径与 2026-10-24 之前逐位相同。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace WarpforgeVFX
{
    public class GrabPassTransparentFeature : ScriptableRendererFeature
    {
        /// <summary>原版读的那个全局纹理名（**照抄，别改** —— 着色器里按它采样）</summary>
        public const string GrabTextureName = "_GrabPassTransparent";
        /// <summary>`1` = 这一帧填过了。着色器靠它决定用新抓的还是退回 `_CameraOpaqueTexture`</summary>
        public const string AvailableName = "_GrabPassAvailable";

        static readonly int GrabId = Shader.PropertyToID(GrabTextureName);
        static readonly int AvailId = Shader.PropertyToID(AvailableName);

        [Tooltip("留空 = 每帧都抓。抓一次的代价是一次全屏 Blit")]
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

        // ------------------------------------------------------------------
        // 🆕 2026-10-24（B14 第二半）：原版 `GrabScreenFeature.RenderPass` = event 501 把抓屏那一层重画一遍
        // ------------------------------------------------------------------
        [Tooltip("开着 = 追加原版那一步 501 重画（画 redrawLayerMask 那一层）。默认关 = 与今天逐字相同。\n" +
                 "⛔ 直接打开会**画两遍**：必须先把扭曲物挪到 redrawLayerMask 这一层，并把 " +
                 "PC_Renderer.asset 的 m_TransparentLayerMask 里那一位**关掉**（见文件尾 §开启前置）。")]
        public bool redrawAt501 = false;

        [Tooltip("501 重画哪一层（= 原版 `Settings.LayerMask`；`0` = 什么都不画）。")]
        public LayerMask redrawLayerMask = 0;

        GrabPassPass _pass;
        GrabRedrawPass _redraw;

        public override void Create()
        {
            _pass = new GrabPassPass { renderPassEvent = passEvent };
            // URP 16 起要求显式声明；抓屏要改全局纹理，必须允许改全局状态
            _pass.requiresIntermediateTexture = true;

            // 🔴 2026-10-24（`F2` · R1 P1-4）：`Create()` 可能被调多次（脚本重载 / 渲染器重建），
            //   先把上一份 `GrabRedrawPass` 那对常驻 NativeArray 释放掉，别漏（见 GrabRedrawPass 里那段说明）。
            _redraw?.DisposeLists();
            _redraw = new GrabRedrawPass();
        }

        /// <summary>🔴 2026-10-24（`F2` · R1 P1-4）：释放 501 那一半常驻的那对 NativeArray。
        /// ⚠️ 只在 `Dispose()` 里做 —— **不能**在 `graph.CreateRendererList(param)` 之后立刻释放
        /// （原因见 <see cref="GrabRedrawPass"/> 里 `BuildLists` 上面那段）。</summary>
        protected override void Dispose(bool disposing)
        {
            _redraw?.DisposeLists();
            _redraw = null;
            _pass = null;
            base.Dispose(disposing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 🆕 2026-10-25（`B14` · `X10`）：501 的两个前置（清透明层掩码位 / 给相机补 cullingMask）。
            // ⛔ 关着时第一行就 return（`GrabScreen501.Enabled == false`）⇒ 与改前逐位相同。
            if (GrabScreen501.Enabled)
            {
                GrabScreen501.EnsureTransparentMaskCleared();
                GrabScreen501.EnsureCameraSeesRelocated(renderingData.cameraData.camera);
            }

            renderer.EnqueuePass(_pass);   // 500 —— ⛔ 现状，一个字别改

            if (_redraw == null || !RedrawEnabled()) return;

            int mask = RedrawLayerMask();
            if (mask == 0)
            {
                WarnOnce("redrawAt501 / WF_GRAB501 开着，但层掩码 = 0 —— " +
                         "501 那一步什么都不画（等于没开）。见 GrabPassTransparentFeature.cs 文件尾 §开启前置。");
                return;
            }
            _redraw.layerMask = mask;
            renderer.EnqueuePass(_redraw); // 501
        }

        /// <summary>环境变量 `WF_GRAB501_LAYER`（旧名 `WF_GRAB_501_LAYER`）：`0..31` = 层号（内部按 `1&lt;&lt;n`），`&gt;31` = 直接当位掩码用。
        /// 没设就用序列化字段 `redrawLayerMask`。**只为 A/B 免改资产**（正常玩法请改资产字段）。</summary>
        int RedrawLayerMask()
        {
            // 🆕 2026-10-25（B14）：总开关开着 ⇒ 层由 `GrabScreen501` 统一解析（与「挪层」那一步**同一个层**，
            //   ⛔ 不能两处各解析一次 —— 两份迟早不一致）。
            if (GrabScreen501.Enabled) return GrabScreen501.LayerMask;

            var s = System.Environment.GetEnvironmentVariable("WF_GRAB_501_LAYER");
            if (!string.IsNullOrEmpty(s) && int.TryParse(s, out int v))
                return v >= 0 && v <= 31 ? (1 << v) : v;
            return redrawLayerMask.value;
        }

        // 开关：环境变量 `WF_GRAB_501` 优先（`0`/`false` = 关，其它值 = 开；便于 A/B 不需要改资产），
        // 没设就用序列化 bool `redrawAt501`。**默认两者都是关** ⇒ 只 Enqueue 500，与今天逐字相同。
        // 🆕 2026-10-25（B14）：**总开关 `WF_GRAB501=1` 也开**（它连带把两个前置一起做掉，见 `GrabScreen501`）。
        static int _env501 = -1;       // -1 未读 / 0 关 / 1 开 / 2 未设
        bool RedrawEnabled()
        {
            if (GrabScreen501.Enabled) return true;
            if (_env501 < 0)
            {
                var s = System.Environment.GetEnvironmentVariable("WF_GRAB_501");
                if (string.IsNullOrEmpty(s)) _env501 = 2;
                else _env501 = (s == "0" || string.Equals(s, "false", System.StringComparison.OrdinalIgnoreCase)) ? 0 : 1;
            }
            return _env501 == 2 ? redrawAt501 : _env501 == 1;
        }

        static bool _warnedNoMask;
        static void WarnOnce(string msg)
        {
            if (_warnedNoMask) return;
            _warnedNoMask = true;
            Debug.LogWarning("[WarpforgeVFX/GrabPass] " + msg);   // 🔴 不许静默失败
        }

        class GrabPassPass : ScriptableRenderPass
        {
            class PassData { public TextureHandle Src; }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>();
                if (res == null || !res.activeColorTexture.IsValid()) return;
                // 直接渲到 backbuffer 时无法当纹理读（URP 的常规路径是临时 RT，这里只是保险）
                if (res.isActiveTargetBackBuffer) return;

                var desc = graph.GetTextureDesc(res.activeColorTexture);
                desc.name = GrabTextureName;
                desc.clearBuffer = false;
                desc.useMipMap = false;
                // 🆕 2026-10-25（`B14` · `X10`）：**原版这张拷屏 RT 是 ×0.25**（低分辨率拷贝）。
                //   判据（逐条实读反编译）：
                //     · `GrabScreenFeatureRenderGraphAPI.Settings__.ctor.c:11` —— `*(undefined4 *)(param_1 + 0x1c) = 0x3e800000;`
                //       = **0.25f**（`textureSizeMultiplier` 的默认值；同一条也在 `GrabScreenFeature.Settings__.ctor.c:11`）。
                //       ⚠️ **十六进制浮点字面量** ⇒ 在 `.c` 里 grep `0.25` / `0.25f` **永远 0 命中**
                //       （`AUD3` 报的「41 个 `.c` 0 命中」就是这么来的 —— 不是「没有」，是「写法搜不到」）。
                //     · `GrabScreenFeatureRenderGraphAPI.GrabPass__RecordRenderGraph.c` ——
                //       `iVar2 = (int)((float)width * *(float *)(lVar3 + 0x1c)); if (0 &lt; iVar2) 宽度 = iVar2;`
                //       （高度同款）⇒ **宽高各乘这个倍数、结果 ≤0 时取 1**。
                //   ⛔ **只在 `WF_GRAB501=1` 那一档生效**：关着时 `RtScale == 1` ⇒ 这一步整段不执行，
                //      `desc` 与改前**逐字段相同**（默认档零变化是硬要求，见 `GrabScreen501.RtScale`）。
                float rtScale = GrabScreen501.RtScale;
                if (!Mathf.Approximately(rtScale, 1f))
                {
                    desc.width = Mathf.Max(1, (int)(desc.width * rtScale));
                    desc.height = Mathf.Max(1, (int)(desc.height * rtScale));
                }
                var dst = graph.CreateTexture(desc);

                using (var builder = graph.AddRasterRenderPass<PassData>("GrabPassTransparent", out var data))
                {
                    data.Src = res.activeColorTexture;
                    builder.UseTexture(data.Src, AccessFlags.Read);
                    builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
                    // 拷完之后把它挂成全局 —— 下一帧画扭曲物时着色器就能采到
                    builder.SetGlobalTextureAfterPass(dst, GrabId);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.Src, new Vector4(1f, 1f, 0f, 0f), 0, false);
                        ctx.cmd.SetGlobalFloat(AvailId, 1f);   // 「填过了」
                    });
                }
            }
        }

        /// <summary>
        /// 原版 `GrabScreenFeature.RenderPass` 的 RenderGraph 版 —— 在 **501**（= 抓屏 500 的下一格）
        /// 把**抓屏那一层**的物体再画一遍。逐项设置照反编译抄（判据见文件头 2026-10-24 那一段）：
        ///   ShaderTagId 三条 = `SRPDefaultUnlit` / `UniversalForward` / `LightweightForward` ·
        ///   `RenderQueueRange.all` · `FilteringSettings.layermask` = 本层 · renderingLayerMask 全开 ·
        ///   `RenderStateBlock{ mask = Depth, depthState = (write:false, LEqual) }` ·
        ///   `SortingCriteria = CommonTransparent(0x17)`.
        ///
        /// ⚠️ 本类**只负责「画」**：「别让 URP 默认透明 pass 先把它画一遍」是**渲染器资产 + 图层**的事，
        ///    不是这个 pass 能决定的 —— 所以打开它之前必须先做文件尾 §开启前置 那两步，
        ///    否则同一帧会画两遍（扭曲叠两遍）。
        /// </summary>
        class GrabRedrawPass : ScriptableRenderPass
        {
            /// <summary>原版那三条 ShaderTagId（反编译三个字面量槽解出来的名字，见文件头）。</summary>
            static readonly List<ShaderTagId> TagIds = new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("LightweightForward"),
            };

            /// <summary>原版 `0x1f5` = 501 —— 拷屏那一格（500）的**下一格**（不是枚举里现成的名字）</summary>
            public const RenderPassEvent Event501 = (RenderPassEvent)501;

            /// <summary>= 原版 `Settings.LayerMask`。0 = 不画（默认，等于没开）</summary>
            public int layerMask;

            /// <summary>原版 `RenderStateBlock{ mask = Depth, depthState = DepthState(write:false, LEqual) }`</summary>
            readonly RenderStateBlock _stateBlock = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
            };

            class PassData { public RendererListHandle List; }

            // ================================================================
            //  🔴 2026-10-24（`F2` · R1 P1-4）：**这两个 NativeArray 原来每帧新建、从不释放**
            //    （原文：`tagValues = new NativeArray<…>(…, Allocator.Temp)` / `stateBlocks = …`）
            //    ⇒ 一开 `WF_GRAB_501`（且 `layerMask != 0`）就**每帧漏 2 个 NativeArray**：
            //      editor 里是每帧刷 "A Native Collection has not been disposed"、player 里内存持续增长。
            //    （默认关 ⇒ `RecordRenderGraph` 第一行 `if (layerMask == 0) return;` 早退，
            //      这一段**根本不会执行** —— 所以此前没暴露。）
            //
            //  ✅ 改法：**一对常驻数组**（`Allocator.Persistent`，长度恒 1），懒建一次、每帧只改值，
            //    由 `ScriptableRendererFeature.Dispose(bool)` → `DisposeLists()` 释放。
            //
            //  ⛔ **不能**「`graph.CreateRendererList(param)` 之后立刻 Dispose」（R1 建议的那一版）：
            //     `CreateRendererList` **不是同步消费** —— `RenderGraphResourceRegistry.CreateRendererList`
            //     只是把这份 `RendererListParams` **按值存进** `RendererListResource.desc`
            //     （`RenderGraphResourceRegistry.cs:863-867` + `RenderGraphResourceRendererList.cs:50-59`），
            //     真正的 `context.CreateRendererList(ref desc)` 发生在之后的
            //     `CompileRenderGraph → CreateRendererLists`（`RenderGraph.cs:2489` /
            //     `RenderGraphResourceRegistry.cs:1208-1223`）—— **那一步**才通过 `GetUnsafeReadOnlyPtr()`
            //     去读这两个数组。提前 Dispose ⇒ **读已释放内存**（editor 里被 safety check 直接抛、
            //     player 里读垃圾内存，501 的重画就会画错/崩）。
            //  ⚠️ 也不能「用 `Allocator.Temp` 留到下一帧再 Dispose」：`Allocator.Temp` 的内存在**帧末自动回收**
            //     ⇒ 跨帧 Dispose 是**重复释放**。所以只能 `Persistent` + 显式释放。
            // ================================================================
            Unity.Collections.NativeArray<ShaderTagId> _tagValues;
            Unity.Collections.NativeArray<RenderStateBlock> _stateBlocks;
            bool _listsReady;

            /// <summary>懒建那对常驻数组（只建一次；⛔ 别每帧 new）。</summary>
            void BuildLists()
            {
                if (_listsReady) return;
                _tagValues = new Unity.Collections.NativeArray<ShaderTagId>(
                    new[] { ShaderTagId.none }, Unity.Collections.Allocator.Persistent);
                _stateBlocks = new Unity.Collections.NativeArray<RenderStateBlock>(
                    new[] { _stateBlock }, Unity.Collections.Allocator.Persistent);
                _listsReady = true;
            }

            /// <summary>释放那对常驻数组（幂等）。由 <see cref="GrabPassTransparentFeature.Dispose(bool)"/>
            /// 与 `Create()`（重入时）调。</summary>
            public void DisposeLists()
            {
                _listsReady = false;
                if (_tagValues.IsCreated) _tagValues.Dispose();
                if (_stateBlocks.IsCreated) _stateBlocks.Dispose();
                _tagValues = default(Unity.Collections.NativeArray<ShaderTagId>);
                _stateBlocks = default(Unity.Collections.NativeArray<RenderStateBlock>);
            }

            public GrabRedrawPass()
            {
                renderPassEvent = Event501;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                if (layerMask == 0) return;                 // 没配层 = 不画（默认态，与现状逐字相同）

                var res = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();
                if (res == null || renderingData == null || cameraData == null || lightData == null) return;
                // 与 500 同一条保险：直接渲到 backbuffer 时这张颜色缓冲当不了纹理，501 也就没有意义
                if (!res.activeColorTexture.IsValid() || res.isActiveTargetBackBuffer) return;

                var drawSettings = RenderingUtils.CreateDrawingSettings(
                    TagIds, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                var filter = new FilteringSettings(RenderQueueRange.all, layerMask);

                // URP 的 `RenderingUtils.CreateRendererListWithRenderStateBlock` 是 **internal**（本程序集调不到）
                // ⇒ 照那份 internal 实现逐行自己拼 `RendererListParams`（`…/RenderingUtils.cs:404-417`）：
                //    tagValues = { ShaderTagId.none } · isPassTagName = false · stateBlocks = { 本 pass 的 state }
                // 🔴 2026-10-24（`F2` · R1 P1-4）：数组改成**常驻的一对**（见 `BuildLists` 上面那段），
                //   每帧只改值 —— ⛔ 别再改回每帧 `new NativeArray(…, Allocator.Temp)` 而不释放。
                BuildLists();
                _tagValues[0] = ShaderTagId.none;
                _stateBlocks[0] = _stateBlock;
                var param = new RendererListParams(renderingData.cullResults, drawSettings, filter)
                {
                    tagValues = _tagValues,
                    stateBlocks = _stateBlocks,
                    isPassTagName = false,
                };
                RendererListHandle list = graph.CreateRendererList(param);

                using (var builder = graph.AddRasterRenderPass<PassData>("GrabPassTransparent 501 redraw", out var data))
                {
                    // 重画时着色器要采**上一步刚挂上来的** `_GrabPassTransparent` ⇒ 必须声明会读全局纹理
                    // （顺序照 URP 自己的 DrawObjectsPass：它是进 builder 的第一句）
                    builder.UseAllGlobalTextures(true);
                    data.List = list;
                    builder.UseRendererList(list);
                    builder.SetRenderAttachment(res.activeColorTexture, 0, AccessFlags.Write);
                    if (res.activeDepthTexture.IsValid())
                        // 原版这一 pass 是 ZWrite **off** ⇒ 深度只读（同 URP 自己 disableZWrite 那一支的取法）
                        builder.SetRenderAttachmentDepth(res.activeDepthTexture, AccessFlags.Read);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.DrawRendererList(d.List);
                    });
                }
            }
        }
    }

    // ==================================================================
    //  GrabScreen501 —— 501 的「两个前置」+ 拷屏 RT 缩放，做成**运行期可 A/B**
    //  （2026-10-25 · `B14` / `X10`；判据与来历见文件头 2026-10-25 那段）
    // ==================================================================
    /// <summary>
    /// **一个环境变量把 501 从「机制已落、默认关」推到「能跑」**。三件事（全部默认关）：
    ///   ① 清 `UniversalRendererData.transparentLayerMask` 的**【501 那一层】那一位**
    ///      —— 否则 URP 的默认透明 pass 会把失真物**先画一遍**，501 再画一遍 = 扭曲叠两遍；
    ///   ② 把**失真效果**搬到那一层（`WarpforgeEffectPlayer.Play` / `WarpforgeEffectHook` 两处收口）；
    ///   ③ 拷屏 RT 按原版 **×0.25**。
    ///
    /// <para>🔑 **开关**（`System.Environment`，本项目惯例）：</para>
    /// <list type="bullet">
    /// <item>`WF_GRAB501=1` → ①②③ + 501 重画**一起开**；不设 / `0` / `false` → **全部关（= 现状）**。</item>
    /// <item>`WF_GRAB501_LAYER=&lt;0..31 | 原始位掩码&gt;` → 用哪一层（默认 **9**）。旧名 `WF_GRAB_501_LAYER` 仍认。</item>
    /// <item>`WF_GRAB501_RT_SCALE=&lt;float&gt;` → 拷屏 RT 缩放（不设时：开 = **0.25**（原版）、关 = **1.0**（现状））。</item>
    /// </list>
    ///
    /// <para>⛔ **旧的 `WF_GRAB_501=1` / 资产上的 `redrawAt501` 语义不变**：只开 501 重画，
    /// **不**替你清位/挪层（那是「手工前置」那条老路，见文件尾 §开启前置 ②）。</para>
    ///
    /// <para>🔴 **默认档为什么逐位等价**（每一件都单独论证，缺一条就不算等价）：</para>
    /// <list type="number">
    /// <item>所有入口（`EnsureTransparentMaskCleared` / `EnsureCameraSeesRelocated` / `FinalLayer`）
    ///   第一行都是 `if (!Enabled) return …`，而 `Enabled` 是 `static readonly bool`、只在类型初始化时读一次环境变量
    ///   ⇒ **不设环境变量 = 这三个函数体一行都不执行**。</item>
    /// <item>`GrabScreen501.RtScale` 关着时**恒等于 1f**，`GrabPassPass` 里那一段是
    ///   `if (!Mathf.Approximately(rtScale, 1f))` ⇒ 关着时 `desc` 的 `width/height` **一个字段都不写**
    ///   （与改前的 `GetTextureDesc` + `name/clearBuffer/useMipMap` 三行完全同序同值）。</item>
    /// <item>`RedrawEnabled()` 关着时仍是「`WF_GRAB_501` / `redrawAt501`」那套老逻辑（只在最前面多一个
    ///   `if (GrabScreen501.Enabled) return true;`，而它为 false）⇒ 老的 A/B 行为一字不变。</item>
    /// <item>`WarpforgeEffectPlayer.Play` 里只多了一句 `finalLayer = GrabScreen501.FinalLayer(go, wantLayer)`，
    ///   关着时它把入参**原样返回** ⇒ 后面的 `if (finalLayer &gt;= 0) SetLayerRec(go, finalLayer)` 与改前同值。
    ///   同理 `WarpforgeEffectHook` 那处（关着时返回 `parent.gameObject.layer`，与改前同值）。</item>
    /// </list>
    /// </summary>
    public static class GrabScreen501
    {
        public const string EnvSwitch = "WF_GRAB501";
        public const string EnvLayer = "WF_GRAB501_LAYER";
        public const string EnvRtScale = "WF_GRAB501_RT_SCALE";
        /// <summary>旧开关名（只开 501 重画那一档，历史名，仍认）</summary>
        const string EnvSwitchLegacy = "WF_GRAB_501";
        const string EnvLayerLegacy = "WF_GRAB_501_LAYER";

        /// <summary>默认层。**为什么是 9**：① 全仓资产里只出现过 0/1/5/6/8/10/20 层
        /// （`grep -rhoE '^  m_Layer: [0-9]+' --include=*.prefab --include=*.unity | sort | uniq -c`，2026-10-25 现读），
        /// `TagManager.asset` 里 9 空着（和已经在用的 `ArenaLayer=8` 一样是未命名层，Unity 允许直接用）；
        /// ② **不取 31**：`1 &lt;&lt; 31` 会把 `LayerMask` 变成负数（= `int.MinValue`），
        ///    `Camera.cullingMask` 能用，但 `FilteringSettings.layerMask` / Inspector 对负值的处理
        ///    在引擎侧看不到源码（`FilteringSettings` 不在 core 包里）⇒ 不冒这个险。</summary>
        public const int DefaultLayer = 9;

        /// <summary>原版 `Settings.textureSizeMultiplier` 默认值 = `0x3e800000` = 0.25f
        /// （`GrabScreenFeatureRenderGraphAPI.Settings__.ctor.c:11` / `GrabScreenFeature.Settings__.ctor.c:11`）。</summary>
        public const float OriginalRtScale = 0.25f;

        /// <summary>总开关。`WF_GRAB_501` 是旧名 —— 它**不**触发本类的三件（老语义：只开重画）。</summary>
        public static readonly bool Enabled = ParseEnabled();

        /// <summary>501 用哪一层（0..31）。挪层与清位**共用这一个数**（⛔ 别两处各解析一次）。</summary>
        public static readonly int LayerIndex = ParseLayer();
        /// <summary>= `1 &lt;&lt; LayerIndex`，给 `GrabRedrawPass.layerMask` 与「清位」用。</summary>
        public static readonly int LayerMask = 1 << LayerIndex;

        /// <summary>拷屏 RT 的缩放。关着时**恒 1.0**（现状，全分辨率）；开着时原版值 0.25，可用环境变量覆盖。</summary>
        public static readonly float RtScale = ParseRtScale();

        /// <summary>探针/自检用：到此刻为止**被搬到 501 层**的失真效果实例数（关着时恒 0）。</summary>
        public static int RelocatedCount { get; private set; }

        static bool ParseEnabled()
        {
            var s = System.Environment.GetEnvironmentVariable(EnvSwitch);
            if (string.IsNullOrEmpty(s)) return false;          // ⛔ 不设 = 关（老开关不在这里）
            if (s == "0" || string.Equals(s, "false", System.StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        static int ParseLayer()
        {
            var s = System.Environment.GetEnvironmentVariable(EnvLayer);
            if (string.IsNullOrEmpty(s)) s = System.Environment.GetEnvironmentVariable(EnvLayerLegacy);
            if (string.IsNullOrEmpty(s) || !int.TryParse(s, out int v)) return DefaultLayer;
            if (v >= 0 && v <= 31) return v;
            // >31 = 直接当位掩码用（旧名 `WF_GRAB_501_LAYER` 的老语义）⇒ 取**最低置位**那一层来搬物体。
            int idx = 0;
            for (int m = v; idx < 31 && (m & 1) == 0; m >>= 1) idx++;
            if (Enabled)   // 关着时连日志都不打（默认档「逐位等价」连日志也一并算上）
                Debug.LogWarning($"[WarpforgeVFX/Grab501] `{EnvLayer}={v}` 是多位掩码 ⇒ 搬物体只用最低位那一层 {idx}；" +
                                 "501 重画仍按整张掩码过滤。要单层请直接给 0..31。");
            return idx;
        }

        static float ParseRtScale()
        {
            var s = System.Environment.GetEnvironmentVariable(EnvRtScale);
            if (!string.IsNullOrEmpty(s) &&
                float.TryParse(s, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out float f) && f > 0f)
                return f;
            return Enabled ? OriginalRtScale : 1f;
        }

        // ------------------------------------------------------------------
        //  ① 清 `m_TransparentLayerMask` 的【501 那一层】那一位（运行期，不改资产）
        // ------------------------------------------------------------------
        //  为什么这样能生效（不是猜的，读的是本工程 PackageCache 里的 URP 源码）：
        //    · `UniversalRendererData.transparentLayerMask` 有 **public setter**，setter 里 `SetDirty()`
        //      （`UniversalRendererData.cs:197-203`）；
        //    · `ScriptableRendererData.SetDirty()` ⇒ `isInvalidated = true`（`ScriptableRendererData.cs:60-63`）；
        //    · `UniversalRenderPipelineAsset.scriptableRenderer` / `GetRenderer(index)` 每帧检查
        //      `isInvalidated`，为真就 `DestroyRenderer` + `InternalCreateRenderer()`（`:902` / `:945`）
        //      ⇒ **下一次取 renderer 时整条 render pass 用新掩码重建**
        //      （`UniversalRenderer.cs:308` 读 `data.transparentLayerMask`、`:430` 建 `DrawObjectsPass`）。
        //  ⇒ 不需要反射、不需要改 `PC_Renderer.asset`、也不需要重启。
        //  ⚠️ **只做一次**：每帧都设会每帧重建 renderer（画面会抖、性能白扔）。
        //  🔴 **改的只是内存里的那份资产对象，不落盘**：URP 的 `SetDirty()` 只置 `isInvalidated`，
        //     **不走** `EditorUtility.SetDirty` ⇒ Unity 不会把它写回 `PC_Renderer.asset`（`git diff` 里看不到）。
        //     ⚠️ 同一 Editor 会话里**换 `WF_GRAB501` 的值要重载域**（默认「进 Play 就重载」已满足）——
        //     `Enabled` 是 `static readonly`、一个域只读一次环境变量。player 每次启动都是新进程，不受影响。
        static bool _maskCleared;
        /// <summary>`UniversalRenderPipelineAsset` 里**所有** renderer data 都清（PC + Mobile 各一份，
        /// 见 `Settings/PC_Renderer.asset` / `Settings/Mobile_Renderer.asset` 各挂了一个本 Feature）。</summary>
        public static void EnsureTransparentMaskCleared()
        {
            if (!Enabled || _maskCleared) return;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return;                     // 管线还没就绪 ⇒ 下一帧再试

            int cleared = 0;
            var list = urp.rendererDataList;
            if (list.Length == 0) return;                // 还没建起来 ⇒ 下一帧再试（别把 _maskCleared 锁死）
            for (int i = 0; i < list.Length; i++)
            {
                var d = list[i] as UniversalRendererData;
                if (d == null) continue;
                int v = d.transparentLayerMask.value;
                if ((v & LayerMask) == 0) continue;       // 已经是清的
                d.transparentLayerMask = new LayerMask { value = v & ~LayerMask };
                cleared++;
            }
            _maskCleared = true;
            Debug.Log($"[WarpforgeVFX/Grab501] 已把第 {LayerIndex} 层从 {cleared} 份 RendererData 的 " +
                      "`m_TransparentLayerMask` 里清掉（501 重画那一层，⛔ 不许被默认透明 pass 再画一遍）。");
            if (cleared == 0)
                Debug.LogWarning($"[WarpforgeVFX/Grab501] 没有任何 renderer data 需要清（都已经是清的？）" +
                                 $"—— 若 `{LayerIndex}` 层其实还被透明 pass 画，501 会**叠两遍**扭曲。");
        }

        // ------------------------------------------------------------------
        //  ② 失真效果搬到 501 层 + 给「原本画得到它」的相机补 cullingMask
        // ------------------------------------------------------------------
        //  为什么要补相机掩码：层是**两套过滤** —— `Camera.cullingMask`（看不看得见）和
        //  `m_TransparentLayerMask`（透明 pass 画不画）。只清后者不补前者 ⇒ 物体从**所有**相机消失。
        //  ⚠️ 补的是「**原本画得到源层 L** 的相机」：战场透视相机只画层 8、HUD 只画 0/5/…，
        //     一律补会把 A 相机才该看的东西塞进 B 相机（同一个特效被两台相机各画一次）。
        static int _relocatedSourceMask;    // 已经搬走过的「源层」位或
        static readonly HashSet<int> _loggedCameras = new HashSet<int>();

        /// <summary>调用方（`WarpforgeEffectPlayer.Play` / `WarpforgeEffectHook`）在**设层之前**调它：
        /// 是失真效果 ⇒ 返回 501 那一层；不是 / 开关关着 ⇒ **原样返回 `wantedLayer`**。</summary>
        /// <param name="wantedLayer">原本要设的层（`-1` = 保持 prefab 自带层）。</param>
        public static int FinalLayer(GameObject go, int wantedLayer)
        {
            if (!Enabled || go == null) return wantedLayer;
            if (!UsesDistortion(go)) return wantedLayer;

            int source = wantedLayer >= 0 ? wantedLayer : go.layer;
            bool already = go.layer == LayerIndex;     // 第二次调用（`WarpforgeEffectHook` 会再设一次层）⇒ 不重复计数
            if (!already)
            {
                if (source >= 0 && source <= 31) _relocatedSourceMask |= 1 << source;
                RelocatedCount++;
                if (_loggedNames.Add(go.name))
                    Debug.Log($"[WarpforgeVFX/Grab501] 失真效果《{go.name}》搬到第 {LayerIndex} 层" +
                              $"（源层 {source}）—— 它由 501 重画，**不再**走默认透明 pass。");
            }
            return LayerIndex;
        }

        /// <summary>每帧（`AddRenderPasses`）调：让「原本画得到某个源层」的相机也画得到 501 层。</summary>
        public static void EnsureCameraSeesRelocated(Camera cam)
        {
            if (!Enabled || cam == null || _relocatedSourceMask == 0) return;
            if ((cam.cullingMask & LayerMask) != 0) return;
            if ((cam.cullingMask & _relocatedSourceMask) == 0) return;   // 这个相机本来就看不到那些源层
            cam.cullingMask |= LayerMask;
            if (_loggedCameras.Add(cam.GetInstanceID()))
                Debug.Log($"[WarpforgeVFX/Grab501] 相机「{cam.name}」的 cullingMask 补上第 {LayerIndex} 层" +
                          $"（原掩码 {cam.cullingMask & ~LayerMask}）—— 否则搬过去的失真效果会**整块消失**。");
        }

        static readonly HashSet<string> _loggedNames = new HashSet<string>();
        static readonly Dictionary<string, bool> _distortionCache = new Dictionary<string, bool>(System.StringComparer.Ordinal);

        /// <summary>这个实例是不是「用抓屏扭曲 shader」的那一族。
        /// <para>🔑 **数据驱动，不写死条数、不手抄名单**：判据取**两处真实引用** ——</para>
        /// <list type="number">
        /// <item>`WarpforgeEffectBinder.materials[].shader` —— 导出器把**原版 prefab 的材质槽**烘进来的，
        ///   它正是 `grep "Particle Distortion Affect Transparents" Prefabs/ | wc -l = 233` 数的那一列
        ///   （2026-10-22 现读 233；本条不引用这个数，只按真实数据判）；</item>
        /// <item>运行时材质（binder 重建之后 / 走了 `UseOriginal` 的原件）—— `shader.name` 命中
        ///   原名或 `WarpforgeShaderMap.Replacements` 里给它的替代名（两份都认，是因为
        ///   `WarpforgeShaderMap` 的白名单让正常态走**原件**、兜底路走**我们那份**）。</item>
        /// </list>
        /// <para>结果按效果名缓存（同名 = 同一个 prefab）⇒ 只在第一次播时扫一遍。</para></summary>
        static bool UsesDistortion(GameObject go)
        {
            string key = go.name;
            bool cached;
            if (_distortionCache.TryGetValue(key, out cached)) return cached;

            bool hit = false;
            var binders = go.GetComponentsInChildren<WarpforgeEffectBinder>(true);
            for (int i = 0; i < binders.Length && !hit; i++)
            {
                var mats = binders[i].materials;
                if (mats == null) continue;
                for (int j = 0; j < mats.Length; j++)
                    if (mats[j] != null && IsDistortionShaderName(mats[j].shader)) { hit = true; break; }
            }

            if (!hit)
            {
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length && !hit; i++)
                {
                    var r = renderers[i];
                    var mats = r.sharedMaterials;
                    for (int j = 0; j < mats.Length; j++)
                        if (mats[j] != null && IsDistortionShaderName(mats[j].shader.name)) { hit = true; break; }
                    if (hit) break;
                    var pr = r as ParticleSystemRenderer;
                    if (pr != null && pr.trailMaterial != null && IsDistortionShaderName(pr.trailMaterial.shader.name))
                        hit = true;
                }
            }

            _distortionCache[key] = hit;
            return hit;
        }

        /// <summary>原版名 **或** `WarpforgeShaderMap` 给它的替代名（⛔ 别在别处再抄一份替代名 —— 判据只留一处）。</summary>
        public const string DistortionOriginalName = "Everguild/FX/Particle Distortion Affect Transparents";
        static bool IsDistortionShaderName(string n)
        {
            if (string.IsNullOrEmpty(n)) return false;
            if (string.Equals(n, DistortionOriginalName, System.StringComparison.Ordinal)) return true;
            string repl;
            return WarpforgeShaderMap.Replacements.TryGetValue(DistortionOriginalName, out repl)
                && string.Equals(n, repl, System.StringComparison.Ordinal);
        }
    }
}

// ==================================================================
//  §开关与前置 —— 打开 501 重画之前**必须**读完（2026-10-24 · B5；2026-10-25 · X10 补「自动档」）
// ==================================================================
//  ✅ **2026-10-25 起有【自动档】（推荐 A/B 用）：`WF_GRAB501=1`**
//     · 它把下面 ① 的开关、② 的 (a)(b) 两步、外加「拷屏 RT ×0.25」（原版 `textureSettingsMultiplier`
//       = `0x3e800000` = 0.25f，见 `GrabPassPass` 里那段判据）**一起做掉**，层默认 **31**
//       （`WF_GRAB501_LAYER` 可换；旧名 `WF_GRAB_501_LAYER` 仍认）。
//     · 不设 / `WF_GRAB501=0` ⇒ **这三件一件都不执行**，只 Enqueue 500 这一个 pass，
//       与 2026-10-24 之前**逐位相同**（逐条论证见 `GrabScreen501` 的 doc）。
//     · 验收时看日志三行：`已把第 N 层从 K 份 RendererData 的 m_TransparentLayerMask 里清掉` ·
//       `失真效果《…》搬到第 N 层` · `相机「…」的 cullingMask 补上第 N 层`。
//
//  ① 手工档开关（两处，任一为「开」即开；默认都关）：
//       · 环境变量 `WF_GRAB_501=1`（`0` / `false` = 强制关；**优先于**序列化 bool）
//       · Feature 上的 `redrawAt501`（`PC_Renderer.asset` 里那个子资产）
//     重画哪一层：序列化 `redrawLayerMask`；A/B 时可用 `WF_GRAB_501_LAYER=<层号 0..31>` 覆盖（免改资产）。
//     关掉时**只 Enqueue 500 这一个 pass**，代码路径与 2026-10-24 之前**逐字相同** ⇒ 可安全 A/B。
//     ⚠️ 手工档**不会**替你清位/挪层 —— 那两步要照 ② 自己做（自动档才一并做）。
//
//  ② 开启前置（自动档已含；手工档要自己做）：
//       (a) 把**扭曲物**（用 `Everguild/FX/Particle Distortion Affect Transparents` 的那些 prefab
//           实例，现读 **233 个效果 / 233 个 prefab**）挪到一个专用图层 N；
//       (b) 把 `Assets/Settings/PC_Renderer.asset` 的 `m_TransparentLayerMask.m_Bits`
//           **清掉第 N 位**（现在是 `4294967295` = 全开），并把 `redrawLayerMask` 设为 `1<<N`。
//       ⚠️ **为什么必须是这两步**：501 只是「再画一遍」；它**不会**把物体从 URP 默认透明 pass 里拿走。
//          不先做 (a)(b) ⇒ 同一帧画两遍（`Blend SrcAlpha OneMinusSrcAlpha` ⇒ 扭曲叠两遍、明显更重）。
//       ⚠️ **顺序**：先 (b) 再开 `redrawAt501`。反过来会有一帧「两边都不画」的空窗（无害，但会让人以为坏了）。
//       📌 原版自己的 `LayerMask` 取的是哪一层：**查不到** —— 原版的 `UniversalRendererData` 资产
//          本地没解出来（`assets_full` 里没有带 `m_TransparentLayerMask` 的对象），所以层号由我们自己挑
//          （自动档取 **9**：全仓资产只用过 0/1/5/6/8/10/20 层，9 空着；⛔ 不用 31 —— `1<<31` 是负数）。
//
//  ③ 批处理 / 无帧循环：本 Feature 用的是标准 `ScriptableRenderPass`（**不挂
//     `RenderPipelineManager` 回调**），没有帧循环时它就是不被调用 —— 不会崩、也不会挂全局状态。
//     `layerMask == 0` 时连 `CreateRendererList` 都不进（`RecordRenderGraph` 第一行就 return）。
//
//  ④ 验收「501 那一半不漏 NativeArray」（2026-10-24 · `F2` · R1 P1-4）：
//       · 自动档：`WF_GRAB501=1` 起 player / 进 Play 即可；手工档：先做 ②(a)(b)，再
//         `WF_GRAB_501=1` + `WF_GRAB_501_LAYER=<N>`；
//       · 预期：**不再**出现 `A Native Collection has not been disposed` 这条错误流
//         （改前每帧两条）；且 501 的重画**画面照旧**（扭曲只叠一次、不消失、不闪）。
//       · 反面验收：**不开** `WF_GRAB501` / `WF_GRAB_501` 时渲染结果与 2026-10-24 之前**逐像素相同**。
//
//  ⑤ 已知边界（2026-10-25 · X10）：
//       · 挪层挂在**实例化那一刻**（`WarpforgeEffectPlayer.Play` 与 `WarpforgeEffectHook` 两处）。
//         `CardView.SetLayer`（出牌搬卡）会把**已挂在卡上的子件**一起刷回卡层 —— 若那个效果还没播完，
//         它会退回卡层（此时它会被默认透明 pass 画，不再走 501）。判据：`CardView.cs:2196-2206`。
//       · `EffectIso`（编辑器隔离渲染）自己 `Object.Instantiate`、**不走 `WarpforgeEffectPlayer.Play`**
//         ⇒ 本自动档对它**零影响**（层不变、开关关也一样）；但反过来，**要在 EffectIso 里量 501 得另配**
//         —— 它渲染前会把实例整棵刷到隔离层 30、并把相机掩码设成 `1<<30`（`EffectIso.cs:354/1054/1606`
//         刷层 · `:320/1301` 设 `cullingMask`），和「挪到 501 层」互斥。本轮**没做**（A/B 走真机 / Play）。
//       · ⛔ **别在批处理（sweep / 自检 / `EffectSweepBatch` / `CardBaseDemo` 之类）里设 `WF_GRAB501=1`**：
//         批处理没有帧循环 ⇒ `EnsureCameraSeesRelocated` 不会被调用，而 `FinalLayer` 已经把它们搬到 9 层
//         ⇒ 那些效果的图会**整块空掉**（并可能让 `RootVfxLayerAssert` 那类「层要落在战场相机掩码里」的断言红）。
//         要量画面就走 **player / Play**。
