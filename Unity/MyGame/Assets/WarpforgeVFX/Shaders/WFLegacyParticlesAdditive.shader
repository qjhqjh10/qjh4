// WFLegacyParticlesAdditive.shader — 自建，替代原版内置管线的 `Legacy Shaders/Particles/Additive`
//
// 🔴 **为什么要单独一份**：它原来和另外几个内置老粒子 shader 一起被映射到通用的
//    `WarpforgeVFX/Particles/Extra Color`（只做 `tex × 顶点色`）—— 而这一族**多两样东西**：
//    **`×2`** 和 **`_TintColor`**。（同族的 `Mobile/Particles/*` **没有**这两样，实测就是 `tex × 顶点色`，
//    所以那两条**不用**单开 —— 判据见下。）
//
// 判据（**实读编译字节码**，2026-10-02）：`工具/disasm_dxbc.py "Legacy Shaders/Particles/Additive" --stage ps`
//   （原件在 `Warpforge_unitybuiltinassets.bundle`）—— **全部 9 条指令**：
//        add  r0.xyzw, v1.xyzw, v1.xyzw      ; v1 = 顶点色(COLOR) ⇒ 先乘 2
//        mul  r0.xyzw, r0.xyzw, cb0[2].xyzw  ; cb0[2] = `_TintColor`（== 推 ==：这件 shader 除贴图外的
//                                            ;   唯一 float4 材质属性就是它；`_InvFade` 是 float，在软粒子那支）
//        sample r1, v2.xy, t0                 ; v2.xy = TEXCOORD0
//        mul  r0.xyzw, r0.xyzw, r1.xyzw      ; × 主贴图
//        mov_sat o0.w, r0.w                   ; ⚠️ **alpha 取 saturate**，rgb **不** saturate
//        mov  o0.xyz, r0.xyzx
//   ⇒ `o0 = float4(2·vcol.rgb·_TintColor.rgb·tex.rgb, saturate(2·vcol.a·_TintColor.a·tex.a))`
//
// 对照（**别搞混**，同一天一起读的）：
//   · `Mobile/Particles/Additive` / `Mobile/Particles/Alpha Blended` = **`o0 = tex × vcol`**（**只有 2 条指令、
//     连 cbuffer 都没有**）⇒ 通用那份 `WFParticlesExtraColor` 对它们是**逐位正确**的，**不要**改成这条。
//   · `Legacy Shaders/Particles/Alpha Blended` = `2·vcol·tex`（**没有 `_TintColor`**）⇒ 用旁边那份
//     `WFLegacyParticlesAlphaBlended.shader`。
//
// ⚠️ **为什么审计表上看不出这条错**：`_TintColor` 的默认值是 **(0.5,0.5,0.5,0.5)** ⇒ `×2×0.5 = ×1`，
//    和通用那份**恰好抵消**。只有**改过 `_TintColor`** 的材质才会现形 ⇒ 本文件是「照原版补齐」，
//    不是「修一个正在冒烟的 bug」。
// ⚠️ **混合**：原版材质上**没有** `_SrcBlend`（legacy 那批的混合写死在 shader 里）⇒ 由
//    `WarpforgeShaderMap.InferBlend` 按名字补（`"additive"` → SrcAlpha/One ✓）。
Shader "WarpforgeVFX/Particles/LegacyAdditive"
{
    Properties
    {
        _MainTex("Particle Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _InvFade("Soft Particles Factor", Range(0.01, 3.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            Name "LegacyAdditive"
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
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
                // 逐条照抄那 9 条指令：×2 → ×TintColor → ×贴图 → alpha 取 saturate、rgb 不取
                half4 c = (2.0h * IN.color) * _TintColor * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return half4(c.rgb, saturate(c.a));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
