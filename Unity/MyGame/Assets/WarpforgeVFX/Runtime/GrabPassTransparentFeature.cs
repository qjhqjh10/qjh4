// GrabPassTransparentFeature.cs — 给 URP 填上原版的全局纹理 `_GrabPassTransparent`
// （2026-09-13 第三十三轮，P1-a0 的收尾）
//
// ==================================================================
//  为什么需要它
// ==================================================================
// 原版 `Everguild/FX/Particle Distortion Affect Transparents` 读的是**全局纹理
// `_GrabPassTransparent`** —— 这是 Built-in RP 的 GrabPass 留下的东西，**URP 没有**。
// 证据：原版 HLSL 源码确实被剥了，但 `Shader.compressedBlob` 解出来是 DXBC、资源名是明文
// （`工具/dump_shader_blob.py`）。名字里的 *Affect Transparents* 也说明它拷的是
// **含透明物**的颜色缓冲。
//
// 我们现在的替代品是 URP 的 `_CameraOpaqueTexture`，但它**不含透明物**，而且
// `PC_RPAsset.asset` 里 `m_OpaqueDownsampling: 1` ⇒ 它还是**半分辨率**的。
// ⇒ 所有抓屏扭曲类效果（子代理按技术构成统计 **41 条**）的扭曲分量都不对：
//   隔壁的透明特效扭曲不到、而且扭曲本身是糊的。
//
// ==================================================================
//  这个 Feature 做什么
// ==================================================================
// 在**透明物画完之后**把相机的颜色缓冲整份拷进一张纹理，挂成全局 `_GrabPassTransparent`，
// 并把 `_GrabPassAvailable` 置 1（着色器靠它决定用哪一张 —— 没跑这个 Feature 的场景
// 会**自动退回** `_CameraOpaqueTexture`，不会变黑）。
//
// ⚠️ **时序**：拷的是「本帧透明物画完之后」的缓冲，而扭曲物自己也是透明物 ⇒
//    它这一帧采样到的是**上一帧**的拷贝（一帧延迟）。抓屏扭曲本来就有这个性质
//    （Built-in 的 GrabPass 也是在画到它那一刻抓当时已有的内容），可接受。
//
// ⚠️ **这是近似，不是等价**：原版 GrabPass 抓的是「画到那个物体那一刻」的内容，
//    我们只能抓一个**统一的时刻**。对一帧内多个扭曲物叠加的场景会有差异。
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
