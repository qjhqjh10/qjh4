// WFDistortion.shader — 自建，用来替代原版 Everguild/FX/Particle Distortion Affect Transparents
//
// 原版 shader 的 HLSL 源码在打包时被剥离，只剩字节码。这里按从 bundle 里读出来的
// 「属性表 + 渲染状态」重建一个功能等价的（原版 14 个属性、1 个 SubShader）：
//
//   属性：_DistortionStrength _DistortTex _Depth_And_Fallof _SOFTPARTICLES _USEMASK
//         _Mask _ANIMUVS _UVSpeed _UVScale _QueueOffset _QueueControl
//   状态：Blend SrcAlpha OneMinusSrcAlpha / ZWrite Off / ZTest LEqual / Cull Back
//         Queue=Transparent, RenderType=Transparent, UniversalMaterialType=Unlit
//
// 作用：把「屏幕上已经渲染好的东西」按 _DistortTex 的 UV 偏移重采样一遍，
// 做出热浪/冲击波那种扭曲。所以它必须读相机的不透明贴图 —— 这也是它
// 「单独看是空的、叠在别的特效上才有效果」的原因（实测时别被这个骗了）。
//
// 属性名与原版一一对应，原版材质的数值可以原样灌进来。
Shader "WarpforgeVFX/FX/Distortion"
{
    Properties
    {
        _DistortTex("DistortTex", 2D) = "gray" {}
        _DistortionStrength("DistortionStrength", Range(0, 1)) = 0.1

        [Toggle(_SOFTPARTICLES_ON)] _SOFTPARTICLES("SoftParticles", Float) = 0
        [Toggle(_USEMASK_ON)] _USEMASK("UseMask", Float) = 0
        _Mask("Mask", 2D) = "white" {}
        _Depth_And_Fallof("Depth And Fallof", Vector) = (0, 1, 1, 0)

        [Toggle(_ANIMUVS_ON)] _ANIMUVS("AnimUVs", Float) = 0
        _UVSpeed("UVSpeed", Vector) = (0, 0, 0, 0)
        _UVScale("UVScale", Vector) = (1, 1, 0, 0)

        // 与原版同名的渲染状态（URP ShaderGraph 的标准一组）
        _Surface("__surface", Float) = 1
        _Blend("__blend", Float) = 0
        _SrcBlend("__src", Float) = 5      // SrcAlpha
        _DstBlend("__dst", Float) = 10     // OneMinusSrcAlpha
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 1
        _ZWrite("__zw", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 2
        _AlphaClip("__clip", Float) = 0
        _Cutoff("__cut", Range(0, 1)) = 0.5
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = 0
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

        Blend [_SrcBlend] [_DstBlend]
        Blend [_SrcBlendAlpha] [_DstBlendAlpha]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull [_Cull]
        AlphaToMask [_AlphaToMask]

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _SOFTPARTICLES_ON
            #pragma shader_feature_local_fragment _USEMASK_ON
            #pragma shader_feature_local_fragment _ANIMUVS_ON

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
                float  _Cutoff;
                float  _Surface; float _Blend;
                float  _SrcBlend; float _DstBlend;
                float  _SrcBlendAlpha; float _DstBlendAlpha;
                float  _ZWrite; float _ZTest; float _Cull;
                float  _AlphaClip; float _AlphaToMask;
                float  _QueueOffset; float  _QueueControl;
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

                #ifdef _ANIMUVS_ON
                    uv = uv * _UVScale.xy + _UVSpeed.xy * _Time.y;
                #endif

                // 偏移量来自 _DistortTex，用顶点色 alpha 当强度掩码
                half4 d = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, uv);
                float2 offset = (d.rg * 2.0 - 1.0) * _DistortionStrength * IN.color.a;

                // 抓屏重采样。UV 是屏幕空间，所以用 positionCS → 屏幕 UV
                float2 screenUV = IN.positionCS.xy / _ScreenParams.xy;
                half3 scene = SampleSceneColor(screenUV + offset);

                half alpha = d.a * IN.color.a;

                #ifdef _USEMASK_ON
                    alpha *= SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
                #endif

                // 软粒子：和场景深度差做淡出（和 self Extra Color 同一套做法）
                #ifdef _SOFTPARTICLES_ON
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float partEye  = -IN.eyeDepth;
                    float fade = saturate((sceneEye - partEye) / max(1e-4, _Depth_And_Fallof.y));
                    alpha *= fade;
                #endif

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                return half4(scene, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
