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
// ⇒ **本 Feature 的现状与差口**（⛔ 这不是「近似可以接受」，是**没做完**的第二半，写清楚缺什么）：
//    ✅ 已经对上的：**拷屏时机 = 500**（`PC_Renderer.asset` 里 `passEvent: 500`，与原版逐字相同）、
//       整帧一次拷贝、全局纹理名、`_GrabPassAvailable` 兜底。
//    ❌ **缺的那一步 = `RenderPass`（在 501 把扭曲物重画一遍）**。我们现在的扭曲物是在
//       **默认透明 pass 里**画的 ⇒ 它采样到的是**上一帧**的拷贝（一帧延迟），而原版这一帧就是对的。
//       要等价需要两件（都还没做）：
//         ① `WFDistortion.shader` 的前向 pass 换一个**自定义 LightMode**（例如 `WarpforgeGrab`），
//            让 URP 默认 pass 不再画它；
//         ② 本 Feature 加**第二个** `ScriptableRenderPass`（event 501），用
//            `ShaderTagId("WarpforgeGrab") + RenderQueueRange.all() + layerMask=everything` 把它画一遍。
//       ⚠️ 为什么这次没直接做：那会**同时**改 233 个材质的绘制时机与顺序（41 条效果），
//          而矩阵/RT 绑定的写法**只能在 Unity 里 A/B 才算数**（本轮红线不跑 Unity）。
//          ⛔ 无验证地改 = 有可能让这些效果整个不画（自定义 LightMode 没被任何 pass 认领就是不可见）；
//          按铁律 11 例外清单之外的「做不到」——**如实留在这里，不拿现在的行为当成品**。
//       📌 落地时的判据就是上面 ①②两条行号，外加 `PC_Renderer.asset` 已有的 `passEvent: 500`。
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

        GrabPassPass _pass;

        public override void Create()
        {
            _pass = new GrabPassPass { renderPassEvent = passEvent };
            // URP 16 起要求显式声明；抓屏要改全局纹理，必须允许改全局状态
            _pass.requiresIntermediateTexture = true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
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
    }
}
