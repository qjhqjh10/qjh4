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
//
// 🔴 **2026-10-02：算式按反编译核过一轮**（`工具/disasm_dxbc.py "Everguild/UnlitAmbient"
//    --stage ps|vs` → `_tmp_view/dxbc/Everguild_UnlitAmbient_{ps,vs}.txt`，812 段）。
//    **基础彩色变体**（14 条指令，ps）逐行：
//        sample_b r0.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x   ; 主图（UV 直通，无 _ST）
//        add  r1.xyz, -r0.xyzx, v2.xyzx
//        mad  r0.xyz, v2.wwww, r1.xyzx, r0.xyzx              ; = lerp(主图.rgb, v2.rgb, v2.w)
//        mul  o0.xyz, r0.xyzx, cb1[5].xyzx
//        mov  o0.w, l(1.000000)                              ; **基础变体输出 alpha 恒 1、不裁剪**
//    ⇒ 三处与我们不同：① 顶点色是 **lerp 的目标色**（不是乘）② 输出 alpha 恒 1
//      ③ 基础变体 **完全不 clip**（只有部分变体有 discard）。
//
// 🔴 **本文件今天只改了「与 cbuffer 槽位无关」的两条**（下面 `frag` 里标了号），
//    其余**没改是因为缺证据、不是因为不做**：
//      · `cb1[i]` ↔ 属性名的对应**读不出来** —— DXBC 段只有 ISGN/OSGN/SHDR、**没有 RDEF**，
//        而 `dump_shader.py` 的 `nameIndices` 那组下标（`_Color`=8 / `_Intensity`=9 /
//        `_MainTex`=4 / `_MatCap`=3）**对不上**反汇编里用到的槽（cb1[2] / cb1[5].x）⇒
//        谁是谁只能靠用法猜。**照猜测重写算式 = 静默错一片**（铁律 2）。
//      · **要做的**（判据已齐，缺的只是上面那一步坐实）：顶点色 lerp（含
//        `_APPLYAMBIENTCOLOR` 那支的乘性系数 `lerp(1, 环境色×cb0[56], cb0[130].x)`）·
//        `Emissive Flickker` 那整支（ramp 采样 + `[min,max]` 重映射；我们声明了
//        `_EmissiveColor/_FlickerSpeed/_FlickerMinMaxRange` 却在 HLSL 里**一个都没用**）·
//        第二张图（`_SampleTexture2D_…_Texture2D` 声明了但**从没采样**）·
//        手写 derivative 的 alpha-to-coverage。
//      ⇒ 详细判据：`项目任务.md` §三 第 4 条。
Shader "WarpforgeVFX/UnlitAmbient"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _BaseMap("BaseMap", 2D) = "white" {}
        _ClipThreshold("ClipThreshold", Range(0, 1)) = 0.5

        _EmissiveColor("EmissiveColor", Color) = (0,0,0,1)
        _FlickerSpeed("FlickerSpeed", Float) = 1
        // 🔴 2026-09-30 更正：原版属性表里是 **`(0,1,0,0)`**（min=0），我们原来写成 `(0.8,1,0,0)`
        //   ⇒ 闪烁的下限被抬到 0.8、摆幅只有原版的三分之一。（判据 → `项目任务.md` §三 第 30 条 散件 C。）
        //   ⚠️ 这条**只影响兜底/自建这条路**：战场网格在运行时是用**原版 shader** 重建的
        //   （`ArenaOriginalMaterial`），`_FlickerMinMaxRange` 走材质里带过来的原值。
        _FlickerMinMaxRange("FlickerMinMaxRange", Vector) = (0, 1, 0, 0)

        // 🔴 2026-10-02 修：原来写 `_RECEIVESHADOWS_ON` / `_APPLYAMBIENTCOLOR_ON` ——
        //    **原版两个名字都没有 `_ON` 后缀**（原版关键字表逐字：`资料/普查产出_0917/shader属性表_块3.md:264`：
        //    `_APPLYAMBIENTCOLOR`, `_RECEIVESHADOWS`）⇒ binder 按原版名 EnableKeyword 全部落空。
        //    同族错见 `WFParticlesExtraColor`（`_SOFTPARTICLES`）。
        [Toggle(_RECEIVESHADOWS)] _RECEIVESHADOWS("ReceiveShadows", Float) = 0
        _FogContribution("FogContribution", Range(0, 1)) = 1

        [Toggle(_APPLYAMBIENTCOLOR)] _APPLYAMBIENTCOLOR("ApplyAmbientColor", Float) = 0
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
            #pragma shader_feature_local_fragment _APPLYAMBIENTCOLOR
            #pragma shader_feature_local_fragment _RECEIVESHADOWS
            // 🆕 2026-10-02：`frag` 里那个 `#ifdef _ALPHATEST_ON` 原来**没有对应的 pragma**
            //   ⇒ 恒假、恒走 `#else` 分支去 clip。补上它，裁不裁才真的由材质的关键字说了算。
            #pragma shader_feature_local_fragment _ALPHATEST_ON
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
                #ifdef _APPLYAMBIENTCOLOR
                    col += SampleSH(IN.normalWS) * _ExtraAmbientColor.rgb;
                #endif

                #ifdef _RECEIVESHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    col *= lerp(1.0h, mainLight.shadowAttenuation, mainLight.shadowAttenuation < 1.0h);
                #endif

                col = MixFog(col, IN.fogFactor * _FogContribution);

                // 🔴 2026-10-02 修 **①**：这里原来是个 `#ifdef _ALPHATEST_ON / #else clip(...)` 的**双分支**，
                //    而本 shader **从来没声明过 `_ALPHATEST_ON` 这个 pragma** ⇒ 那个 `#ifdef` **恒假**、
                //    永远走 `#else` ⇒ **每一帧都按 alpha 裁一次**（原版基础变体里**一条 discard 都没有**，
                //    见文件头）。现在改成：**只在 `_ALPHATEST_ON` 真被打开时才裁**，并补上那条 pragma。
                #ifdef _ALPHATEST_ON
                    clip(tex.a * _Color.a - _ClipThreshold);
                #endif

                // 🔴 2026-10-02 修 **②**：去掉末尾的 `* IN.color.a`。
                //    判据：反汇编**全文件**里 `o0.w` 只有四种写法 —— `l(0)` / `l(1)` /
                //    「主图.w × cb1[5].w」/ `movc`；**顶点 alpha 一次都没进过 alpha 通道**。
                //    ⚠️ 原版**基础**变体是 `mov o0.w, l(1.000000)`（恒 1）。我们保留「主图.a × _Color.a」
                //    是因为实测**这些材质 10/12 带 `_SURFACE_TYPE_TRANSPARENT`**（带 alpha 的那支变体
                //    算的就是「主图.a × 一个材质的 alpha」）—— **这是取近似，不是原版基础变体**，
                //    如实标在这里（要坐实得先把 cb1[5] 是谁读出来，见文件头）。
                return half4(col, tex.a * _Color.a);
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
