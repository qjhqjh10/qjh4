// WFDistortion.shader — 自建，替代原版 Everguild/FX/Particle Distortion Affect Transparents
//
// 原版是 ShaderGraph shader，**HLSL 源码和编译字节码在打包时都被剥掉了**
// （实测：Shader.m_Script = null、m_SubProgramBlob = null）。能拿到的只有序列化出来的
// 属性表 + pass 状态，下面每一项都从 bundle 里读出来，出处写在括号里。
//
// 读法（py312 + UnityPy，脚本 `d:/4/_tmp_view/shaderprobe2.py`）：
//   pf = shader.m_ParsedForm
//   pf.m_PropInfo.m_Props            # 属性表（名字/类型/默认值）
//   pf.m_SubShaders[0].m_Passes[0].m_State.rtBlend0   # 混合
//   pf.m_KeywordNames                # 关键字全集
//
// ── 属性（原版 11 个，名字/类型/默认值一字不差）─────────────────────────────
//   _DistortionStrength  Float    def 1.0            ← 注意是 **Float** 不是 Range
//   _DistortTex          Texture  def "bump"
//   _Depth_And_Fallof    Vector   def (1, 0.1, 0, 0) ← 不是 (0,1,1,0)
//   _SOFTPARTICLES       Float    def 0
//   _USEMASK             Float    def 0
//   _Mask                Texture  def "white"
//   _ANIMUVS             Float    def 0
//   _UVSpeed             Vector   def (1, 1, 0, 0)
//   _UVScale             Vector   def (1, 1, 0, 0)
//   _QueueOffset         Float    def 0
//   _QueueControl        Float    def -1
//   （另有 unity_Lightmaps / unity_LightmapsInd / unity_ShadowMasks 三个，是 Unity 按
//     光照贴图关键字自动加的，不用自己声明）
//
// ── 渲染状态（原版 pass0，**写死在 shader 里**）──────────────────────────────
//   rtBlend0: srcBlend=5(SrcAlpha) destBlend=10(OneMinusSrcAlpha)
//             srcBlendAlpha=1 destBlendAlpha=10   separateBlend=False
//   zWrite=0  zTest=4(LEqual)  cull=2(Back)  alphaToMask=0
//   SubShader tags: RenderPipeline=UniversalPipeline / QUEUE=Transparent / RenderType=Transparent
//                   ShaderGraphShader=true / ShaderGraphTargetId=UniversalUnlitSubTarget
//   共 3 个 pass（pass1=运动矢量 colMask=12、pass2=深度，本文件只做 pass0）
//
// ⚠️ **原版属性表里没有 _SrcBlend/_DstBlend/_ZWrite/_Surface/_Blend/_Cull，一个都没有。**
//    所以混合**必须硬编码**，不能写成 `Blend [_SrcBlend] [_DstBlend]`。
//    踩过的坑：原版材质上**带着一批内置 Standard shader 的残留值**
//      （`_SrcBlend=1` One / `_DstBlend=0` Zero / `_ZWrite=1` / `_Surface=0` / `_Glossiness=0` …），
//    ours 曾经用 `[_SrcBlend]` 间接寻址 → 这些残留值被灌进来 → 变成**不透明覆盖 + 写深度**，
//    在场景里会把后面的东西整块抠掉。实测见 DistortProbe（棋盘背景上导出侧糊出一块纯黑）。
//    同类坑 `WFSpritesAdditive.shader` 头部也记过（那个是 Blend One One 写死）。
//
// 作用：把「屏幕上已经渲染好的东西」按 _DistortTex 的 UV 偏移重采样一遍，
// 做出热浪/冲击波那种扭曲。所以它必须读相机的屏幕贴图 —— 这也是它
// 「单独在纯色背景上看是空的、叠在别的画面上才有效果」的原因（实测时别被这个骗了）。
//
// ⚠️ **还没查清的一条（2026-09-12）**：原版读的**具体是哪张屏幕贴图**没有确证。
//    实测（DistortProbe）：把相机的不透明贴图喂上内容、并把材质排进透明队列之后，
//    **我们这版能渲出错位的棋盘（说明读的就是 _CameraOpaqueTexture）**，
//    而原版在同样条件下**输出完全不变**，仍然是一块半透明灰 —— 说明原版读的不是
//    `_CameraOpaqueTexture`，而是另一张在本工程里没被绑定的全局贴图
//    （shader 名字里的 "Affect Transparents" 也指向这个：URP 的不透明贴图**不含透明物**，
//     要扭曲透明物必须自己拷一份颜色缓冲）。那张贴图的名字在 bundle 里查不到
//    （字节码被剥了），要用别的办法找（见交接文档 P1-a0）。
//
// 属性名与原版一一对应，原版材质的数值可以原样灌进来。
Shader "WarpforgeVFX/FX/Distortion"
{
    Properties
    {
        _DistortionStrength("DistortionStrength", Float) = 1.0
        _DistortTex("DistortTex", 2D) = "bump" {}

        [Toggle(_SOFTPARTICLES)] _SOFTPARTICLES("SoftParticles", Float) = 0
        [Toggle(_USEMASK)] _USEMASK("UseMask", Float) = 0
        _Mask("Mask", 2D) = "white" {}
        _Depth_And_Fallof("Depth And Fallof", Vector) = (1, 0.1, 0, 0)

        [Toggle(_ANIMUVS)] _ANIMUVS("AnimUVs", Float) = 0
        _UVSpeed("UVSpeed", Vector) = (1, 1, 0, 0)
        _UVScale("UVScale", Vector) = (1, 1, 0, 0)

        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "UniversalMaterialType" = "Unlit"
        }

        // ↓ 原版 pass0 的状态，硬编码（原版也没有可驱动的属性）
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Back

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // 关键字名照原版（**没有 _ON 后缀**）—— 名字写错的话原版材质/导出侧
            // EnableKeyword 全部落空，等于这三个功能永远关着
            #pragma shader_feature_local_fragment _SOFTPARTICLES
            #pragma shader_feature_local_fragment _USEMASK
            #pragma shader_feature_local_fragment _ANIMUVS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DistortTex_ST;
                float4 _Mask_ST;
                float4 _UVSpeed;
                float4 _UVScale;
                float4 _Depth_And_Fallof;
                float  _DistortionStrength;
            CBUFFER_END

            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                float  eyeDepth   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _DistortTex);
                OUT.color      = IN.color;
                OUT.eyeDepth   = p.positionWS.z;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                #ifdef _ANIMUVS
                    uv = uv * _UVScale.xy + _UVSpeed.xy * _Time.y;
                #endif

                // 偏移量来自 _DistortTex，用顶点色 alpha 当强度掩码
                half4 d = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, uv);
                float2 offset = (d.rg * 2.0 - 1.0) * _DistortionStrength * IN.color.a;

                // 抓屏重采样。UV 是屏幕空间，所以用 positionCS → 屏幕 UV
                float2 screenUV = IN.positionCS.xy / _ScreenParams.xy;
                half3 scene = SampleSceneColor(screenUV + offset);

                half alpha = d.a * IN.color.a;

                #ifdef _USEMASK
                    alpha *= SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
                #endif

                // 软粒子：和场景深度差做淡出（和 self Extra Color 同一套做法）
                #ifdef _SOFTPARTICLES
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float partEye  = -IN.eyeDepth;
                    float fade = saturate((sceneEye - partEye) / max(1e-4, _Depth_And_Fallof.y));
                    alpha *= fade;
                #endif

                return half4(scene, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
