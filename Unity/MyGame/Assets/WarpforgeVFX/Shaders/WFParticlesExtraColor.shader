// WFParticlesExtraColor.shader — 自建，用来替代原版 Everguild/FX/Extra Color
//
// 原版 shader 的 HLSL 源码在打包时被剥离，只剩字节码，无法还原成工程资产。
// 这里按「属性表 + 实拍比对」重建一个功能等价的：
//   _Color × _MainTex × 粒子顶点色，可选软粒子，混合/剔除/裁剪状态全由材质上的
//   _SrcBlend/_DstBlend/_ZWrite/_Cull 驱动 —— 与原版属性名一一对应，
//   所以原版材质的数值可以原样灌进来，不需要任何转换。
//
// 覆盖：原版 1142 个特效材质里，Everguild/FX/Extra Color 占 2789 次引用，
//       是自定义 shader 里用量最大的一个（约占自定义用量 66%）。
Shader "WarpforgeVFX/Particles/Extra Color"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}

        [Toggle(_SOFTPARTICLES_ON)] _SOFTPARTICLES("Soft Particles", Float) = 0
        _SoftParticlesFadeDistance("    Soft Fade Distance", Range(0.01, 20)) = 1.0

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("    Alpha Cutoff", Range(0,1)) = 0.5

        // ---- 与原版同名的渲染状态（URP ShaderGraph 的标准一组）----
        _Surface("__surface", Float) = 1
        _Blend("__blend", Float) = 0
        _SrcBlend("__src", Float) = 5      // SrcAlpha
        _DstBlend("__dst", Float) = 10     // OneMinusSrcAlpha
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 1
        _ZWrite("__zw", Float) = 0
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 2
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = 0
        [HideInInspector] _CastShadows("_CastShadows", Float) = 0
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
        }

        // 只用两参数形式：粒子材质几乎不写 alpha 通道，四参数分离混合反而容易出意外
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull [_Cull]
        AlphaToMask [_AlphaToMask]

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _SOFTPARTICLES_ON
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float  _Cutoff;
                float  _SoftParticlesFadeDistance;
                float  _Surface;
                float  _Blend;
                float  _SrcBlend;
                float  _DstBlend;
                float  _SrcBlendAlpha;
                float  _DstBlendAlpha;
                float  _ZWrite;
                float  _ZWriteControl;
                float  _ZTest;
                float  _Cull;
                float  _AlphaToMask;
                float  _QueueOffset;
                float  _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;
                OUT.eyeDepth   = p.positionWS.z;   // 视图空间深度（负值）
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * _Color * IN.color;

                // 预乘：原版部分材质开了 _ALPHAPREMULTIPLY_ON
                #ifdef _ALPHAPREMULTIPLY_ON
                    col.rgb *= col.a;
                #endif

                // 软粒子：按与场景深度的差做淡出。URP 粒子 ShaderGraph 的标准做法
                #ifdef _SOFTPARTICLES_ON
                    float2 suv = IN.positionCS.xy / _ScreenParams.xy;
                    float  rawDepth = SampleSceneDepth(suv);
                    float  sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float  partEye  = -IN.eyeDepth;
                    float  fade = saturate((sceneEye - partEye) / max(1e-4, _SoftParticlesFadeDistance));
                    col.a *= fade;
                #endif

                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                return col;
            }
            ENDHLSL
        }

        // 影子投射（原版 _CastShadows 控制）。粒子一般不开，留着以免报缺 pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float  _Cutoff;
                float  _SoftParticlesFadeDistance;
                float  _Surface; float _Blend; float _SrcBlend; float _DstBlend;
                float  _SrcBlendAlpha; float _DstBlendAlpha; float _ZWrite;
                float  _ZWriteControl; float _ZTest; float _Cull; float _AlphaToMask;
                float  _QueueOffset; float _QueueControl; float _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct SA { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct SV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            SV ShadowVert(SA IN)
            {
                SV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                o.color = IN.color;
                return o;
            }

            half4 ShadowFrag(SV IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _Color.a * IN.color.a;
                #ifdef _ALPHATEST_ON
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
