// WFMatcap.shader — 自建，替代原版 Everguild/Matcap/Matcap Full Options 和 Matcap With Texture
//
// 两个原版 shader 共用同一份属性表（25 个），只差 _MatCap 那张图：
//   Matcap Full Options  → 只有 _MainTex，光照信息完全写死在贴图里
//   Matcap With Texture  → 多一张 _MatCap
// 所以这里一个 shader 同时顶两个（属性名一致，原版数值原样灌入，零转换）。
//
// 原理：matcap 就是「从正面看一个球」的贴图，用**视图空间法线**的 xy 当 UV 去采样。
// 好处是完全不需要实时光照 —— 这也解释了为什么它在关卡里看着比周围亮。
//
// 状态（从 bundle 的 SerializedShaderState 读出，Opaque 队列）：
//   Blend SrcAlpha OneMinusSrcAlpha / ZWrite On / ZTest LEqual / Cull Off
//   Queue=Geometry, RenderType=Opaque, UniversalMaterialType=Unlit
Shader "WarpforgeVFX/Matcap/Matcap"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("MainTex", 2D) = "white" {}
        _MatCap("MatCap", 2D) = "white" {}
        _Intensity("Intensity", Range(0, 5)) = 1

        [Toggle(_APPLYAMBIENTCOLOR_ON)] _APPLYAMBIENTCOLOR("ApplyAmbientColor", Float) = 0
        _ExtraAmbientColor("ExtraAmbientColor", Color) = (1,1,1,1)
        _FogContribution("FogContribution", Range(0, 1)) = 1
        [HideInInspector] _CastShadows("_CastShadows", Float) = 0

        // 与原版同名的渲染状态（URP ShaderGraph 的标准一组）
        _Surface("__surface", Float) = 0
        _Blend("__blend", Float) = 0
        _SrcBlend("__src", Float) = 5      // SrcAlpha
        _DstBlend("__dst", Float) = 10     // OneMinusSrcAlpha
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 1
        _ZWrite("__zw", Float) = 1
        _ZWriteControl("__zwc", Float) = 0
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _APPLYAMBIENTCOLOR_ON
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _MatCap_ST;
                float4 _ExtraAmbientColor;
                float  _Intensity;
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MatCap);
            SAMPLER(sampler_MatCap);

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
                float2 matcapUV   : TEXCOORD1;
                half4  color      : COLOR;
                float  fogFactor  : TEXCOORD2;
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
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;

                // matcap 的核心：视图空间法线的 xy 拿来当 UV。
                // 法线要转到**观察空间**（view space），不是世界空间
                float3 normalVS = TransformWorldToViewDir(n.normalWS, true);
                OUT.matcapUV = normalVS.xy * 0.5 + 0.5;

                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color * IN.color;
                half3 cap  = SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, IN.matcapUV).rgb;

                half3 col = main.rgb * cap * _Intensity;

                #ifdef _APPLYAMBIENTCOLOR_ON
                    col += _ExtraAmbientColor.rgb * main.a;
                #endif

                col = MixFog(col, IN.fogFactor * _FogContribution);

                #ifdef _ALPHATEST_ON
                    clip(main.a - _Cutoff);
                #endif

                return half4(col, main.a);
            }
            ENDHLSL
        }

        // 影子投射。原版 _CastShadows 控制，粒子一般不开，留一个以免报缺 pass
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
                float4 _Color; float4 _MainTex_ST; float4 _MatCap_ST; float4 _ExtraAmbientColor;
                float _Intensity; float _FogContribution; float _Cutoff;
                float _Surface; float _Blend; float _SrcBlend; float _DstBlend;
                float _SrcBlendAlpha; float _DstBlendAlpha; float _ZWrite;
                float _ZWriteControl; float _ZTest; float _Cull;
                float _AlphaClip; float _AlphaToMask;
                float _QueueOffset; float _QueueControl; float _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct SA { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct SV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            SV ShadowVert(SA IN)
            {
                SV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return o;
            }

            half4 ShadowFrag(SV IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _Color.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
