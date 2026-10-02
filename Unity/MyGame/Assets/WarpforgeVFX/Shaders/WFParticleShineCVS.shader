// WFParticleShineCVS.shader — 自建，替代原版 `Everguild/FX/Particle Shine Custom Vertex Streams`
//
// 🔴 **来由**（2026-10-02 第三批）：它原来被顶替成通用的 `WarpforgeVFX/Particles/Extra Color`
//    （只做「一张图 × 顶点色」）—— 而原版在这之上还有**一层沿旋转 UV 采样的 Shine 叠加**。
//
// 判据（**全部实读**）—— `_tmp_view/dxbc/shine_cvs{,_vs}.txt`
// ---------------------------------------------------------------------------
// ① **渲染状态**：两个 pass 的 `rtBlend0` **全是字面量、一个属性名都没有**
//    ⇒ **写死**：`Blend SrcAlpha OneMinusSrcAlpha` · `ZWrite Off` · `ZTest LEqual` · **`Cull Off`**
//      （⚠️ 材质里那个 `_Cull=2` 是**死值** —— 原版根本不读它，⛔ 不许改成间接寻址）。
//      Queue=Transparent(3000)。`ShaderGraphTargetId = UniversalSpriteUnlitSubTarget`（Sprite 版 Unlit 图）。
// ② **属性名逐字照抄**（ShaderGraph 的 GUID 名，写错就灌不进值）：
//      `_MainTex`(NoScaleOffset) · `Texture2D_0bfdc50de139497fa85c0cd08848879a`(Shine Tex, NoScaleOffset)
//      · `Color_a142b0353b4945a49e1990789c04ea35`(Shine Color, **非 HDR**)
//      · `_ShineScale`(.xy 缩放 / .zw 偏移) · `Vector1_c76b369469204ddeb1ee5d707098307c`(UvRotation)
//      · `Vector1_90576362d340403fa7a5cc36416fb824`(AnimationTime)
//      · `Vector1_a48438ecc6b346ab85bcbad930c537e2`(ShineIntensity) · `Shine_Width`(.x=lo/.y=hi)
//      · `BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4`(UseCustomVertexData —— **只当关键字用，代码里不读**)
// ③ **顶点**：`o1 = TEXCOORD0.xyzw` **原样传**（xy = 主 UV，**zw = 自定义顶点流**）·
//    `o2 = COLOR`（原版还乘了 `_RendererColor * unity_SpriteColor` 两个 Sprite 全局；粒子场景下它们是白，
//     本文件直接传顶点色 —— **如实标注**，若 A/B 露出系统性偏差先回查这一条）· `o3 = 世界坐标`。
// ④ **片元**（35 条；材质走 **`BOOLEAN_7733627B…_ON`** 那支，即 K 取顶点流）：
//      t0 = tex(`_MainTex`, uv0.xy)
//      θ  = `Vector1_c76b36…`（UvRotation，弧度）
//      uv = (uv0.xy − 0.5) 按 θ **顺时针**旋转 → +0.5 → `_ShineScale`.xy 缩放 + `_ShineScale`.zw · K 偏移
//      t1 = tex(Shine Tex, uv)
//      g  = saturate( (t1.rgb − `Shine_Width`.x) / (`Shine_Width`.y − `Shine_Width`.x) )，再 `g = g²(3−2g)`
//      o0.rgb = ( t0.rgb + g · `Color_a142b0…`.rgb · K ) · vcol.rgb
//      o0.a   = t0.a · vcol.a
//      K = **uv0.w**（`…_ON` 支）｜ `Vector1_a48438…`(ShineIntensity)（OFF 支，配 `…Time` 当偏移）
//      discard 条件：**t0.a == 0**
//    🔴 **`…_ON` 那个关键字必须自己 `#pragma`** —— 不声明的话 `#ifdef` 恒假、永远走 OFF 支
//      （2026-10-02 在 `WFParticleDissolveMask` 上刚踩过）。
//
// ⚠️ **白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/ParticleShineCVS"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        Texture2D_0bfdc50de139497fa85c0cd08848879a("Shine Tex", 2D) = "white" {}
        Color_a142b0353b4945a49e1990789c04ea35("Shine Color", Color) = (1, 1, 1, 1)
        _ShineScale("ShineScale", Vector) = (1, 1, 1, 1)
        Vector1_c76b369469204ddeb1ee5d707098307c("Uv Rotation", Float) = 0.93
        Vector1_90576362d340403fa7a5cc36416fb824("AnimationTime", Float) = 0
        Vector1_a48438ecc6b346ab85bcbad930c537e2("ShineIntensity", Float) = 1
        Shine_Width("Shine Width", Vector) = (0, 1, 0, 0)
        BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4("UseCustomVertexData", Float) = 1
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

        // 原版两个 pass 全是字面量（材质上的 _Cull/_SrcBlend 是死值）—— 硬编码，不许间接寻址
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ParticleShineCVS"
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;                       // 原版两张图都 NoScaleOffset ⇒ 不参与
                float4 Texture2D_0bfdc50de139497fa85c0cd08848879a_ST;
                float4 Color_a142b0353b4945a49e1990789c04ea35;
                float4 _ShineScale;
                float  Vector1_c76b369469204ddeb1ee5d707098307c;
                float  Vector1_90576362d340403fa7a5cc36416fb824;
                float  Vector1_a48438ecc6b346ab85bcbad930c537e2;
                float4 Shine_Width;
                float  BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(Texture2D_0bfdc50de139497fa85c0cd08848879a);
            SAMPLER(samplerTexture2D_0bfdc50de139497fa85c0cd08848879a);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv0        : TEXCOORD0;   // zw 是自定义顶点流（`…_ON` 支当 K 与偏移用）
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv0        : TEXCOORD0;
                float4 color      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv0   = IN.uv0;      // 原版 vs：`mov o1.xyzw, v3.xyzw` —— 不做 TRANSFORM_TEX
                OUT.color = IN.color;    // 原版还乘了两个 Sprite 全局（粒子下为白），见文件头 ③
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 t0 = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uv0.xy, _GlobalMipBias.x);

            #if defined(BOOLEAN_7733627BA3A1447E9D9F28A82CD0E4D4_ON)
                half K = IN.uv0.w;                                            // 顶点流那一支
                half animK = IN.uv0.z;
            #else
                half K = Vector1_a48438ecc6b346ab85bcbad930c537e2;            // ShineIntensity
                half animK = Vector1_90576362d340403fa7a5cc36416fb824;        // AnimationTime
            #endif

                // 旋转（顺时针，θ = UvRotation）+ 缩放 + 偏移 —— 逐条照抄 vs/ps
                float th = Vector1_c76b369469204ddeb1ee5d707098307c;
                float s = sin(th), c = cos(th);
                float2 p = IN.uv0.xy - 0.5;
                float2 rot = float2(p.x * c + p.y * s, -p.x * s + p.y * c) + 0.5;
                float2 uvShine = rot * _ShineScale.xy + _ShineScale.zw * animK;

                half4 t1 = SAMPLE_TEXTURE2D_BIAS(Texture2D_0bfdc50de139497fa85c0cd08848879a,
                                                 samplerTexture2D_0bfdc50de139497fa85c0cd08848879a,
                                                 uvShine, _GlobalMipBias.x);

                half3 g = saturate((t1.rgb - Shine_Width.x) / max(1e-6h, Shine_Width.y - Shine_Width.x));
                g = g * g * (3.0h - 2.0h * g);                                // smoothstep 的 3-2x 那一步

                half3 rgb = (t0.rgb + g * Color_a142b0353b4945a49e1990789c04ea35.rgb * K) * IN.color.rgb;
                half  a   = t0.a * IN.color.a;

                clip(t0.a != 0.0h ? 1.0h : -1.0h);                            // 原版 `eq` + `discard_nz`
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
