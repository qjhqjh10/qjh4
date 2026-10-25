// WFParticlePremultiply.shader — 自建，**逐条照抄**原版 `Everguild/FX/Particle Premultiply`
//
// 🔴 来由（2026-10-22 · W64 兜底审计 **B11**）：这条原来在 `WarpforgeShaderMap.Replacements` 里
//    指到通用的 `WarpforgeVFX/Particles/Extra Color`，注释自承「**近似**：拿不到属性表，按名字挑最接近的」。
//    现在原版**编译字节码**已经被 Ruri 反编译出来了（`资料/普查产出_1020/W58_工具线_Ruri实测.md`），
//    算法可以逐条对上 ⇒ 本节替换那条近似。
//
// 判据（**全部实读**，不是猜）
// ----------------------------
// 源：`d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Everguild_FX_Particle Premultiply.shader`
//     ＋ 同目录 `Everguild_FX_Particle Premultiply/Sub0_Pass0_{Vertex_b1,Fragment_b5,Fragment_b6}.hlsl`
// ① 属性（名字/类型/默认值逐字照抄反编译出来的 Properties 块）：
//      `_MainTex`(2D, "white") · `_Color`(HDR Color, def **(1,1,1,0)**) · `_ENABLEVERTEXSTREAMS`(Toggle, 0)
//      `_SOFTPARTICLES`(Toggle, 0) · `_Depth_And_Offset`(Vector, **(0.3,1,0,0)**) · `_QueueOffset`(0) · `_QueueControl`(-1)
// ② 渲染状态（反编译 pass 块逐字）：`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`
//      · `ZWrite Off` · 没有 `Cull` 行 ⇒ **默认 Back**（`OriginalShaderBlendTable.CullMap` 也记 2）
//      ⚠️ 名字叫 Premultiply，**pass 里的混合并不是预乘** —— 是普通 alpha 混合；照抄 pass，别按名字改。
// ③ 顶点（`Sub0_Pass0_Vertex_b1.hlsl`）：只做 `clip = unity_MatrixVP · (unity_ObjectToWorld · pos)`，
//      `INTERP = TEXCOORD0`（原样传，**不**乘 `_ST`）· `INTERP_2 = COLOR0`（顶点色原样传）。
//      ⚠️ `_ENABLEVERTEXSTREAMS` 那一支**原版没有编译产物**（每个 pass 只编了一个变体，blob 里没有它）
//         ⇒ 按**铁律 11 例外①（判据为空）**留空；属性保留只为让原版材质的值灌得进来。
// ④ 片元（`Sub0_Pass0_Fragment_b5.hlsl`，27 条指令）：
//      t     = SAMPLE(_MainTex, uv)
//      c     = LinearToSRGB(_Color.rgb)          ; 那 7 条 log/exp/mad 就是 sRGB 编码（0.003131 分界）
//      rgb   = c * t.rgb * vcol.rgb
//      a     = _Color.a * t.a * vcol.a * t.a     ; ⚠️ **t.a 乘了两次**（`_124=_101*_Color.w; _129=_128*_101`）
// ⑤ 软粒子（`Fragment_b6.hlsl` = `_SOFTPARTICLES` 那一支）：
//      d     = LinearEyeDepth(sceneDepth) − LinearEyeDepth(particleDepth)
//      fade  = pow( saturate(d / _Depth_And_Offset.x), _Depth_And_Offset.y )
//      rgb  *= fade ; a *= fade                  ; ⚠️ **rgb 也乘**（URP 内置那套只乘 alpha，别照抄那边）
//      （原版对正交相机另有一支「反投影求距离」；本工程相机是透视（`_ProjectionParams.x/y` 那条路没用上），
//        按**铁律 11 例外①**只实现透视这一支，注释在此留证。）
//
// ⚠️ 它在 `UseOriginal` 白名单里 ⇒ **正常态走原件**（bundle 里的原版编译 shader）；本文件只在
//    「原件包不在场」的**兜底路**生效。
Shader "WarpforgeVFX/FX/ParticlePremultiply"
{
    Properties
    {
        [NoScaleOffset] _MainTex("MainTex", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1, 1, 1, 0)
        [Toggle(_ENABLEVERTEXSTREAMS)] _ENABLEVERTEXSTREAMS("EnableVertexStreams", Float) = 0
        [Toggle(_SOFTPARTICLES)] _SOFTPARTICLES("SoftParticles", Float) = 0
        _Depth_And_Offset("Depth And Offset", Vector) = (0.3, 1, 0, 0)
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("QueueControl", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        // 原版写死在 pass 里（`OriginalShaderBlendTable` 里这一条也是烘死的 5/10）—— ⛔ 不改间接寻址
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "Universal Forward"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // 原版 pass 就是 `#pragma multi_compile_local _ _SOFTPARTICLES`（逐字）
            #pragma multi_compile_local _ _SOFTPARTICLES

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _ENABLEVERTEXSTREAMS;
                // ⚠️ **这里【不能】声明 `float _SOFTPARTICLES;`** —— `_SOFTPARTICLES` 同时是**关键字**
                //    （`[Toggle(_SOFTPARTICLES)]` ＋ 上面那条 `#pragma multi_compile_local`）。
                //    只要有材质把它打开，Unity 就 `#define _SOFTPARTICLES 1` ⇒ 与同名变量**重定义**、整份编不过。
                //    实测：2026-10-24 的 player 构建日志
                //    `Shader error in 'WarpforgeVFX/FX/ParticlePremultiply': redefinition of '_SOFTPARTICLES'`
                //    （`_tmp_view/20261024_playerbuild.log:4967`）。
                //    本文件**也没用到它的值**（片元只走 `#ifdef`）⇒ 按 `WFParticlePremultiplyGreyscale.shader:109`
                //    与 `WFUnlitUVScroll.shader:120` 的同族做法：**只留 Properties 里那一条**。
                //    ⚠️ `_ENABLEVERTEXSTREAMS` 不算这一类：原版没给它编任何变体、本文件也没有它的 pragma
                //    ⇒ 永远不会被 `#define`，留着无害（只是让原版材质的值灌得进来）。
                float4 _Depth_And_Offset;
                float  _QueueOffset;
                float  _QueueControl;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv0        : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv         : TEXCOORD0;
                float4 color      : TEXCOORD1;
                float  eyeDepth   : TEXCOORD2;   // 片元里算软粒子要用
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv0;                 // 原版 `mov o1.xyzw, v3.xyzw` —— **不做 TRANSFORM_TEX**
                OUT.color = IN.color;
                OUT.eyeDepth = -TransformWorldToView(TransformObjectToWorld(IN.positionOS.xyz)).z;
                return OUT;
            }

            /// linear → sRGB **编码**（原版那 7 条：log2*0.416667 → exp2 → mad 1.055,-0.055；<=0.003131 走 *12.92321）
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uv.xy, _GlobalMipBias.x);

                // ④ 逐条：`c = LinearToSRGB(_Color.rgb)`；rgb = c * t.rgb * vcol.rgb
                half3 c = LinearToSrgb(_Color.rgb);
                half3 rgb = c * t.rgb * IN.color.rgb;
                // a = _Color.a * t.a * vcol.a * t.a   （t.a 两次，见文件头 ④）
                half  a   = _Color.a * t.a * IN.color.a * t.a;

            #ifdef _SOFTPARTICLES
                // ⑤ fade = pow(saturate((sceneEye - partEye) / _Depth_And_Offset.x), _Depth_And_Offset.y)
                float2 suv = IN.positionCS.xy / _ScreenParams.xy;
                float  sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float  d = sceneEye - IN.eyeDepth;
                half   fade = (half)pow(saturate(d / max(1e-6, _Depth_And_Offset.x)), _Depth_And_Offset.y);
                rgb *= fade;
                a   *= fade;
            #endif

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
