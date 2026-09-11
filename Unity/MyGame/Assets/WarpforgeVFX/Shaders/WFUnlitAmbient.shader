// WFUnlitAmbient.shader — 自建，替代原版 Everguild/UnlitAmbient 与
// Everguild/UnlitAmbient Emissive Flickker
//
// 这两个是**场景贴图烘焙用的非粒子 shader**（29 个属性），不是粒子 shader。
// 不映射的话会掉进默认的 URP **Particles**/Unlit —— 属性名对不上，
// `_BaseMap` 拿不到图 → 用默认白图渲染成**一整块纯白矩形**（实测过曝 4.6 倍）。
//
// 状态（从 bundle 读出）：Opaque 队列，ZWrite On，ZTest LEqual，Cull Off
//
// 属性名与原版一一对应，原版材质的数值和贴图可以原样灌进来。
Shader "WarpforgeVFX/UnlitAmbient"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _BaseMap("BaseMap", 2D) = "white" {}
        _ClipThreshold("ClipThreshold", Range(0, 1)) = 0.5

        _EmissiveColor("EmissiveColor", Color) = (0,0,0,1)
        _FlickerSpeed("FlickerSpeed", Float) = 1
        _FlickerMinMaxRange("FlickerMinMaxRange", Vector) = (0.8, 1, 0, 0)

        [Toggle(_RECEIVESHADOWS_ON)] _RECEIVESHADOWS("ReceiveShadows", Float) = 0
        _FogContribution("FogContribution", Range(0, 1)) = 1

        [Toggle(_APPLYAMBIENTCOLOR_ON)] _APPLYAMBIENTCOLOR("ApplyAmbientColor", Float) = 0
        _ExtraAmbientColor("ExtraAmbientColor", Color) = (1,1,1,1)
        _SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
        [HideInInspector] _CastShadows("_CastShadows", Float) = 0

        // 与原版同名的渲染状态（URP ShaderGraph 的标准一组）
        _Surface("__surface", Float) = 0
        _Blend("__blend", Float) = 0
        _SrcBlend("__src", Float) = 1
        _DstBlend("__dst", Float) = 0
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 1
        _ZWrite("__zw", Float) = 1
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 0
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
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
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
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _APPLYAMBIENTCOLOR_ON
            #pragma shader_feature_local_fragment _RECEIVESHADOWS_ON
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseMap_ST;
                float4 _EmissiveColor;
                float4 _FlickerMinMaxRange;
                float4 _ExtraAmbientColor;
                float  _ClipThreshold;
                float  _FlickerSpeed;
                float  _FogContribution;
                float  _Cutoff;
                float  _Surface; float _Blend;
                float  _SrcBlend; float _DstBlend;
                float  _SrcBlendAlpha; float _DstBlendAlpha;
                float  _ZWrite; float _ZWriteControl; float _ZTest; float _Cull;
                float  _AlphaClip; float _AlphaToMask;
                float  _QueueOffset; float  _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half4  color      : COLOR;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = n.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color      = IN.color;
                OUT.fogFactor  = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 col = tex.rgb * _Color.rgb * IN.color.rgb;

                // 环境色叠加（关键词控制，默认关）
                #ifdef _APPLYAMBIENTCOLOR_ON
                    col += SampleSH(IN.normalWS) * _ExtraAmbientColor.rgb;
                #endif

                #ifdef _RECEIVESHADOWS_ON
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    col *= lerp(1.0h, mainLight.shadowAttenuation, mainLight.shadowAttenuation < 1.0h);
                #endif

                col = MixFog(col, IN.fogFactor * _FogContribution);

                #ifdef _ALPHATEST_ON
                    clip(tex.a * _Color.a - _Cutoff);
                #else
                    clip(tex.a * _Color.a - _ClipThreshold);
                #endif

                return half4(col, tex.a * _Color.a * IN.color.a);
            }
            ENDHLSL
        }

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float4 _BaseMap_ST; float4 _EmissiveColor; float4 _FlickerMinMaxRange;
                float4 _ExtraAmbientColor; float _ClipThreshold; float _FlickerSpeed; float _FogContribution;
                float _Cutoff; float _Surface; float _Blend; float _SrcBlend; float _DstBlend;
                float _SrcBlendAlpha; float _DstBlendAlpha; float _ZWrite; float _ZWriteControl;
                float _ZTest; float _Cull; float _AlphaClip; float _AlphaToMask;
                float _QueueOffset; float _QueueControl; float _CastShadows;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct SA { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct SV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            SV ShadowVert(SA IN)
            {
                SV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(SV IN) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _Color.a - _ClipThreshold);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
