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
        // 🆕 2026-09-13 第三十三轮：原版 `Everguild/FX/Extra Color` 的属性表里**有**这个，
        //    我们的第一版漏了 —— 后果是**亮度被静默丢掉**：那批材质的亮度写在 `_EmissionColor`
        //    （实测 `DA_Winged_Sword_Glow_extra` 是 4.62，而 `_Color` 只有 1；
        //     `Square_Glow_border` 是 4.62468 vs 2），binder 的逐属性拷贝循环
        //    `if (m.HasProperty(name))` 一看我们这边没这个属性就**直接跳过**（不报错）⇒ 偏暗。
        //    子代理按技术构成统计：这一类 **11 条**（6 亮 / 5 暗，其中 4 条可量化偏暗）。
        [HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)
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
                float4 _EmissionColor;
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
                // 🔴 2026-09-15 更正：这里原来是 `col = tex * _Color * _EmissionColor * IN.color`
                //    —— **乘法是错的，退了一大步**（sweep 实测：`Cut Wulfen SW` 一族亮度比
                //    0.033、`Bore Through` 0.029，即**导出比原版暗 30 倍**；回退这一行全部复原）。
                //
                //    错在哪：`_EmissionColor` 在黑着的时候**是一个死值**，和 CLAUDE.md 记的
                //    `_SrcBlend` 残留值是同一类坑 ——
                //      · 原版材质带着**内置 shader 的默认值**：URP/内置粒子 shader 的属性表里
                //        `[HDR] _EmissionColor("Color", Color) = (0,0,0)`（`ParticlesUnlit.shader:9`），
                //        没开发光的材质就留成 **{0,0,0,1}**；
                //      · 实测本工程里 12 个材质是黑的（`Cut_1_blend` / `Cut_4_blend` /
                //        `Shimmer_blend` / `Waterfall_Middle_blend` / `Embers 1` /
                //        `Strike_Effect_*` / `Vulnerable_Icon` …），它们正是那一族效果的**主体**；
                //      · 原版那边这些值是**死的**（`_EmissionEnabled=0` / 没有 `_EMISSION` 关键字
                //        ⇒ 内置 shader 根本不采它），乘上去等于把材质整个乘没。
                //
                //    改回**加法**：原版那套就是加法的 —— URP 粒子系（`ParticlesLitInput.hlsl:103`
                //    `emission = _EmissionMap * _EmissionColor.rgb`）算完之后是**加到**表面颜色上的；
                //    黑 = 不加 = 不发生任何事（正是原版的行为），亮 = 发光（`Smoke Sprite Sheet
                //    Extra Additive` 是 26.6、`Iron_Halo 1_add` 是 4.62，加法与乘法给的量级几乎一样）。
                half4 col = tex * _Color * IN.color;
                col.rgb += tex.rgb * _EmissionColor.rgb * IN.color.rgb;

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
                float4 _EmissionColor;
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
