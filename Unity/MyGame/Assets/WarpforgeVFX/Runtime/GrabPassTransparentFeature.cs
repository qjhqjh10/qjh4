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
            renderer.EnqueuePass(_pass);   // 500 —— ⛔ 现状，一个字别改

            if (_redraw == null || !RedrawEnabled()) return;

            int mask = RedrawLayerMask();
            if (mask == 0)
            {
                WarnOnce("redrawAt501 / WF_GRAB_501 开着，但 redrawLayerMask = 0 —— " +
                         "501 那一步什么都不画（等于没开）。见 GrabPassTransparentFeature.cs 文件尾 §开启前置。");
                return;
            }
            _redraw.layerMask = mask;
            renderer.EnqueuePass(_redraw); // 501
        }

        /// <summary>环境变量 `WF_GRAB_501_LAYER`：`0..31` = 层号（内部按 `1&lt;&lt;n`），`&gt;31` = 直接当位掩码用。
        /// 没设就用序列化字段 `redrawLayerMask`。**只为 A/B 免改资产**（正常玩法请改资产字段）。</summary>
        int RedrawLayerMask()
        {
            var s = System.Environment.GetEnvironmentVariable("WF_GRAB_501_LAYER");
            if (!string.IsNullOrEmpty(s) && int.TryParse(s, out int v))
                return v >= 0 && v <= 31 ? (1 << v) : v;
            return redrawLayerMask.value;
        }

        // 开关：环境变量 `WF_GRAB_501` 优先（`0`/`false` = 关，其它值 = 开；便于 A/B 不需要改资产），
        // 没设就用序列化 bool `redrawAt501`。**默认两者都是关** ⇒ 只 Enqueue 500，与今天逐字相同。
        static int _env501 = -1;       // -1 未读 / 0 关 / 1 开 / 2 未设
        bool RedrawEnabled()
        {
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
}

// ==================================================================
//  §开关与前置 —— 打开 501 重画之前**必须**读完（2026-10-24 · B5）
// ==================================================================
//  ① 开关（两处，任一为「开」即开；默认都关）：
//       · 环境变量 `WF_GRAB_501=1`（`0` / `false` = 强制关；**优先于**序列化 bool）
//       · Feature 上的 `redrawAt501`（`PC_Renderer.asset` 里那个子资产）
//     重画哪一层：序列化 `redrawLayerMask`；A/B 时可用 `WF_GRAB_501_LAYER=<层号 0..31>` 覆盖（免改资产）。
//     关掉时**只 Enqueue 500 这一个 pass**，代码路径与 2026-10-24 之前**逐字相同** ⇒ 可安全 A/B。
//
//  ② 开启前置（**这两步不在本文件里，也不在本代理白名单里**）：
//       (a) 把**扭曲物**（用 `Everguild/FX/Particle Distortion Affect Transparents` 的那些 prefab
//           实例，现读 **233 个效果 / 233 个 prefab**）挪到一个专用图层 N；
//       (b) 把 `Assets/Settings/PC_Renderer.asset` 的 `m_TransparentLayerMask.m_Bits`
//           **清掉第 N 位**（现在是 `4294967295` = 全开），并把 `redrawLayerMask` 设为 `1<<N`。
//       ⚠️ **为什么必须是这两步**：501 只是「再画一遍」；它**不会**把物体从 URP 默认透明 pass 里拿走。
//          不先做 (a)(b) ⇒ 同一帧画两遍（`Blend SrcAlpha OneMinusSrcAlpha` ⇒ 扭曲叠两遍、明显更重）。
//       ⚠️ **顺序**：先 (b) 再开 `redrawAt501`。反过来会有一帧「两边都不画」的空窗（无害，但会让人以为坏了）。
//       📌 原版自己的 `LayerMask` 取的是哪一层：**查不到** —— 原版的 `UniversalRendererData` 资产
//          本地没解出来（`assets_full` 里没有带 `m_TransparentLayerMask` 的对象），所以层号由我们自己挑。
//
//  ③ 批处理 / 无帧循环：本 Feature 用的是标准 `ScriptableRenderPass`（**不挂
//     `RenderPipelineManager` 回调**），没有帧循环时它就是不被调用 —— 不会崩、也不会挂全局状态。
//     `layerMask == 0` 时连 `CreateRendererList` 都不进（`RecordRenderGraph` 第一行就 return）。
//
//  ④ 验收「501 那一半不漏 NativeArray」（2026-10-24 · `F2` · R1 P1-4）：
//       · 先做 ②(a)(b)（把图层分开），再 `WF_GRAB_501=1` + `WF_GRAB_501_LAYER=<N>` 起 player / 进 Play；
//       · 预期：**不再**出现 `A Native Collection has not been disposed` 这条错误流
//         （改前每帧两条）；且 501 的重画**画面照旧**（扭曲只叠一次、不消失、不闪）。
//       · 反面验收：**不开** `WF_GRAB_501` 时渲染结果与 2026-10-24 之前**逐像素相同**（默认关的路径没动）。
