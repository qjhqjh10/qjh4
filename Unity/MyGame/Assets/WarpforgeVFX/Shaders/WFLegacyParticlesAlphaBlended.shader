// WFLegacyParticlesAlphaBlended.shader — 自建，替代原版内置管线的 `Legacy Shaders/Particles/Alpha Blended`
//
// 🔴 **为什么要单独一份**：它原先被映射到通用的 `WarpforgeVFX/Particles/Extra Color`（`tex × 顶点色`），
//    而这一族是 **`2 × 顶点色 × 贴图`** —— 差的正是那个 **`×2`**。
//
// 判据（**实读编译字节码**，2026-10-02）：`工具/disasm_dxbc.py "Legacy Shaders/Particles/Alpha Blended" --stage ps`
//   —— **全部 7 条指令**（seg0 = 无软粒子那支）：
//        add  r0.xyzw, v1.xyzw, v1.xyzw      ; v1 = 顶点色 ⇒ 先乘 2
//        sample r1, v2.xy, t0                 ; TEXCOORD0
//        mul  r0.xyzw, r0.xyzw, r1.xyzw       ; × 主贴图
//        mov_sat o0.w, r0.w                   ; ⚠️ alpha 取 saturate，rgb 不取
//        mov  o0.xyz, r0.xyzx
//   ⇒ `o0 = float4(2·vcol.rgb·tex.rgb, saturate(2·vcol.a·tex.a))`
//   🔴 **注意：这一支【不读 `_TintColor`】**（seg0 一个 cbuffer 都没声明）—— 与旁边那份
//      `WFLegacyParticlesAdditive.shader`（那个**乘** `_TintColor`）是**两个不同算式**，别合并成一份。
//
// 🟡 **为什么审计表上看不出这条错**：`_TintColor` 在本支里不参与，所以没有「默认 0.5 抵消」那回事；
//    但被它影响的材质很少、且往往与别的 shader 同屏 ⇒ 在**逐效果**的审计里被摊薄了（现列出的几条
//    偏差都 <0.14）。**它仍然是「与原版不符」，按铁律 11 照原版补齐。**
//
// ⚠️ **混合**：原始材质上没有 `_SrcBlend` ⇒ 由 `WarpforgeShaderMap.InferBlend` 按名字补
//    （`"alpha blended"` → SrcAlpha/OneMinusSrcAlpha ✓）。
Shader "WarpforgeVFX/Particles/LegacyAlphaBlended"
{
    Properties
    {
        _MainTex("Particle Texture", 2D) = "white" {}
        _InvFade("Soft Particles Factor", Range(0.01, 3.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            Name "LegacyAlphaBlended"
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _InvFade;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float4 uv0 : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = TRANSFORM_TEX(IN.uv0.xy, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 逐条照抄：×2 → ×贴图 → alpha 取 saturate、rgb 不取（**不读 `_TintColor`**）
                half4 c = (2.0h * IN.color) * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return half4(c.rgb, saturate(c.a));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
