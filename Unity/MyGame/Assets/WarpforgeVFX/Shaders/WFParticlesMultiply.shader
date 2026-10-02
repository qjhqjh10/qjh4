// WFParticlesMultiply.shader — 自建，替代原版内置的 `Mobile/Particles/Multiply`
//
// 🔴 **为什么要单独给它一份**（2026-10-02，由「兜底路全量审计」查出来）：
//    原来它和另外 5 个内置/老粒子 shader 一样，被 `Replacements` **统统映射到
//    `WarpforgeVFX/Particles/Extra Color`** —— 而那是个**只乘**的通用版
//    （`col = tex * 顶点色`）。**`Multiply` 这一支的算式根本不是这个**：
//    它是「**按 alpha 从白插值到 tex×color**」，差得不是一点半点。
//    审计里 `Goff_Rok_Invasion` **×7.96** 就是它（该 prefab 的材质里有 `Mobile/Particles/Multiply`）。
//    逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读，不是猜**）
// ----------------------------------------------------------------
//  · **属性表只有 1 个**：`_MainTex`（flags=4 = NoScaleOffset）—— 由 `UnityPy` 读
//    `MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_builtin.bundle` 的 `m_PropInfo.m_Props` 得到。
//    **没有 `_Color` / `_TintColor` / `_EmissionColor`** ⇒ 一个都不许乘（同族教训见 `CLAUDE.md` 三）。
//  · **关键字**：只有 STEREO_* / FOG_*（**没有材质关键字**）⇒ 没有变体分支。
//  · **片元算式** = `工具/disasm_dxbc.py "Mobile/Particles/Multiply" --stage ps` 的**全部** 11 条指令：
//        sample r0.xyzw, v1.xyxx, t0.xyzw, s0                     ; _MainTex @ UV
//        mul    r1.x,    r0.w,    v0.w                            ; a = tex.a * 顶点色.a
//        mad    r0.xyzw, r0.xyzw, v0.xyzw, l(-1,-1,-1,-1)         ; tex*color − 1
//        mad    o0.xyzw, r1.xxxx, r0.xyzw, l( 1, 1, 1, 1)         ; a·(tex*color−1) + 1
//      ⇒ **`o0 = lerp(1, tex × 顶点色, tex.a × 顶点色.a)`**（四个通道都算，连 alpha 一起）。
//      ⚠️ 这一步**不是**「乘 blend」—— 混合状态另外写死在 pass 上（见下）。
//  · **混合写死在 pass 里**（原版材质上**没有** `_SrcBlend`，实读 `rtBlend0` 全是 `<noninit>`）
//      ⇒ 本文件**硬编码 `Blend DstColor Zero`**，⛔ 不许做 `[_SrcBlend]` 间接寻址
//        （会把内置 Standard 的残留值当活值用，见 `CLAUDE.md` 三那条）。
//  · Tags：`QUEUE=Transparent` · `IGNOREPROJECTOR=true` · `RenderType=Transparent` · `PreviewType=Plane`。
//
// ⚠️ **它在白名单里**（`WarpforgeShaderMap.UseOriginal` 的 2026-09-19 那批）⇒ **运行时走的是原件**，
//    本文件只在**兜底路**（`wf_builtin.bundle` 不在场，或 `WFBIND_FORCE_BUILTIN` 打开时）生效。
Shader "WarpforgeVFX/Particles/Multiply"
{
    Properties
    {
        [PerRendererData] _MainTex("Particle Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        // 原版写死在 shader 里（材质上没有 _SrcBlend）—— 不许改成间接寻址
        Blend DstColor Zero
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            Name "ParticleMultiply"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ⚠️ 这里**故意留空**：原版 `Mobile/Particles/Multiply` 的属性表里**只有 `_MainTex`**，
            //    没有任何标量/向量——不要为了「看着完整」往里加。
            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;      // 原版 ps 的 `v0.xyzw` 就是它
                float2 uv         : TEXCOORD0;  // 原版 ps 的 `v1.xy`
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;                 // 原版 `_MainTex` 是 NoScaleOffset ⇒ **不做 TRANSFORM_TEX**
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half  a   = tex.a * IN.color.a;                       // `mul r1.x, r0.w, v0.w`
                return lerp(half4(1, 1, 1, 1), tex * IN.color, a);    // 后两条 mad 的等价写法
            }
            ENDHLSL
        }
    }

    Fallback Off
}
