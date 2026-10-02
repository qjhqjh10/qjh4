// WFParticleDissolveMask.shader — 自建，替代原版 `Everguild/FX/Particle Dissolve Mask`
//
// 🔴 **来由**（2026-10-02，兜底路审计第 2 族）：原来映射到通用的 `WarpforgeVFX/Particles/Extra Color`（只乘）
//    ⇒ 兜底路上 `CreateCard Gold` ×2.23 · `CreateCard GSC 5x` ×2.25 · `CreateCard Sabotage` ×2.21 ·
//    `CreateCard DA` ×2.01 · `CreateCard GSC` ×1.97 · `CreateCard Necron` ×1.79 · `AmbushEffect` ×1.56 …
//    逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读**）—— 反汇编 `d:/4/_tmp_view/dxbc/dissolve_mask.txt`，证据包 `_tmp_view/fam/dissolve_mask.md`
// ---------------------------------------------------------------------------
// ① **渲染状态**：pass0 的 `rtBlend0` 四个值**全是材质属性间接引用**（`_SrcBlend`/`_DstBlend`/
//    `_SrcBlendAlpha`/`_DstBlendAlpha`，不是 `<noninit>`）· `zWrite=_ZWrite`(0) · `zTest=_ZTest`(4) ·
//    `cull=_Cull` · `alphaToMask=_AlphaToMask`(0)。30 个材质块**全都有**这四个值、且有 3 种组合
//    （多数 5/10；`BacklashEffect` 5/1 加色；`Will Of Gork` 1/0 + `_ZWrite=1`）
//    ⇒ **必须间接寻址** —— 与 `Mobile/Particles/Multiply` 那条「不许间接寻址」的教训**前提相反**
//    （那条的原版材质压根没有 `_SrcBlend`）。Queue=Transparent(3000)。
// ② **属性名必须与原版一字不差**（ShaderGraph 的 obfuscated 名，binder 只往 `HasProperty` 为真的灌）：
//    `_MainTex` · `_Disolve` · `Vector4_d8c31f3b…`(UV scale xy / offset zw) · `Vector1_4e2dbe7b…`(DissolveAmount)
//    · `Vector1_2780b564…`(Dissolve Border Width) · `_Color`(Border Color, HDR) · `_Add_Color`(HDR)
//    · `_DISSOLVE_CHANNEL` · `BOOLEAN_F96BE1E6…`(UseCustomVertexData) · `BOOLEAN_D80F51B6…`(MultiplyEdgeColor)
// ③ **两条路**（原版靠关键字分叉，我们照关键字 `#ifdef`）：
//    · **`BOOLEAN_F96BE1E6…_ON`（自定义顶点数据路）= 材质实际走的那支**（30 个材质块全是 1）：
//        A = v2.z（溶解量来自顶点流）· edge = (v1.z, v1.w, v2.x)（边缘色来自顶点流）
//        base.rgb = c.a·(c.rgb + sRGB(_Add_Color).rgb)·vcol.rgb
//        base.a   = c.a·(c.a  + _Add_Color.a)·vcol.a
//        cut = (A ≥ d) · mask = saturate(cut − ((A−W) ≥ d))          ; 只有 (A−W, A] 那条带
//        `BOOLEAN_D80F51B6…_ON`(MultiplyEdgeColor)：rgb = lerp(base.rgb, base.rgb·edge, mask)
//        否则：                                    rgb = base.rgb + mask·edge
//        o0.a = base.a · cut                        ; 只有段 0/1 是 o0.w=1
//    · **否则**（材质值路，段 0）：A = `Vector1_4e2dbe7b…` · W = `Vector1_2780b564…`
//        mask = saturate((A ≥ d) − ((A−W) ≥ d))
//        o0.rgb = c.a·(c.rgb + sRGB(_Add_Color).rgb)·vcol.rgb + mask·sRGB(`_Color`.rgb)   ; ← **这支用材质边框色**
//        o0.a = 1.0
//    ⚠️ 两条路都**先把 `_Add_Color` / `_Color` 做 `LinearToSRGB` 编码**再参与算式（常量逐字来自 DXBC：
//       0.416667 / 1.055 / −0.055 / 阈值 0.003131 / 线性段 12.92321）—— **不是** SRGBToLinear。
//       材质值（HDR，有 >1 的分量）就是在这一步**之前**的那份，少这一步会明显偏。
// ④ **两张贴图**（都 NoScaleOffset ⇒ **不做 `TRANSFORM_TEX`**）：
//    `_MainTex` @ v1.xy（基色 + 基色 alpha）· `_Disolve` @ `v1.xy·UVScale + UVOffset`（取一个通道当阈值）
//    两次采样都带 `_GlobalMipBias.x` 作 mip bias。
//    ⚠️ 通道：**自定义顶点数据路**由 `_DISSOLVE_CHANNEL_*` 关键字选（0/1/2/3 = R/G/B/A，与材质数值逐一对上），
//       我们按**数值** `_DISSOLVE_CHANNEL` 分支；**材质值路（段 0）恒取 .r**（实读）。
//
// ⚠️ **如实标注（判据不足，不是不做）**：pass0 共 8 个 ps 变体，本文件只实现了**材质在用到的 4 支**
//    （段 2/3/6/7）。段 1/4/5（`o0.w=1` 那支、G 通道那支）**没有材质用到**，而且「哪个关键字门控它们」
//    在 DXBC 侧查不到（关键字全集里没有对应名字）⇒ **照不到判据**（铁律 3）。见证据包 §7。
//
// ⚠️ **它在白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/ParticleDissolveMask"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Disolve("DissolveTexture", 2D) = "white" {}
        Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d("DissolveUVScale(XY)Offset(ZW)", Vector) = (1, 1, 0, 0)
        Vector1_4e2dbe7bff184ada93bb896e70f4c06d("DissolveAmount", Range(0, 1)) = 0.43
        Vector1_2780b5644c1e463480f9eca2dba65f9a("Dissolve Borded Width", Float) = 0.04
        [HDR] _Color("Border Color", Color) = (3.222744, 0, 0, 0)
        [HDR] _Add_Color("Add Color", Color) = (0, 0, 0, 0)
        _DISSOLVE_CHANNEL("DissolveChannel", Float) = 2
        BOOLEAN_F96BE1E681E64E0E98322A14BDBD35A3("UseCustomVertexData", Float) = 0
        BOOLEAN_D80F51B627624648A0B3B2D563AA7541("MultiplyEdgeColor", Float) = 1

        _Surface("__surface", Float) = 1
        _Blend("__blend", Float) = 0
        _AlphaClip("__clip", Float) = 0
        _SrcBlend("__src", Float) = 1
        _DstBlend("__dst", Float) = 0
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 0
        _ZWrite("__zw", Float) = 0
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 2
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = -1
        _CastShadows("_CastShadows", Float) = 0
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

        // 原版这四项都是**材质属性间接引用**（30 个材质块全有值、3 种组合）—— 不许改成字面量
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Pass
        {
            Name "ParticleDissolveMask"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // 🔴 **这两个 `#pragma` 不能省**（2026-10-02 第一次 A/B 就栽在这）：
            //    下面用 `#if defined(BOOLEAN_F96…_ON)` 分叉，而 Unity **只会在 shader 自己声明过变体的关键字上定义它**
            //    ⇒ 不写 pragma 的话两分支恒为假、永远走「材质值路」，
            //    实测四条效果（Pray_Exit / AmbushEffect / Buff_SW_Rune_Tornado / Buff_SW_Rune_Blizzard）当场退步。
            //    关键字名逐字来自原版（材质上带的就是 `BOOLEAN_F96BE1E6…_ON`），binder 的 `EnableKeyword` 按名打开 ✓。
            #pragma shader_feature_local BOOLEAN_F96BE1E681E64E0E98322A14BDBD35A3_ON
            #pragma shader_feature_local BOOLEAN_D80F51B627624648A0B3B2D563AA7541_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;                    // 原版 NoScaleOffset ⇒ 不参与
                float4 _Disolve_ST;
                float4 Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d;
                float  Vector1_4e2dbe7bff184ada93bb896e70f4c06d;
                float  Vector1_2780b5644c1e463480f9eca2dba65f9a;
                float4 _Color;
                float4 _Add_Color;
                float  _DISSOLVE_CHANNEL;
                float  BOOLEAN_F96BE1E681E64E0E98322A14BDBD35A3;
                float  BOOLEAN_D80F51B627624648A0B3B2D563AA7541;
                float  _Surface, _Blend, _AlphaClip;
                float  _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha;
                float  _ZWrite, _ZWriteControl, _ZTest, _Cull, _AlphaToMask;
                float  _QueueOffset, _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_Disolve); SAMPLER(sampler_Disolve);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv0        : TEXCOORD0;   // v1（xy=主 UV；zw / uv2 供自定义顶点数据路）
                float4 uv2        : TEXCOORD1;   // v2（.x=边缘色 B；.z=溶解量）
                float4 color      : COLOR;       // v3
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain     : TEXCOORD0;   // ps 的 v1.xy
                float4 uvEdge     : TEXCOORD1;   // ps 的 v1.zw + v2.x 用到的原始流
                float4 stream2    : TEXCOORD2;   // ps 的 v2
                float4 color      : TEXCOORD3;   // ps 的 v3
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvMain  = IN.uv0.xy;
                OUT.uvEdge  = IN.uv0;
                OUT.stream2 = IN.uv2;
                OUT.color   = IN.color;
                return OUT;
            }

            /// linear → sRGB **编码**（原版那 7 条；常量逐字来自 DXBC）
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            /// `_DISSOLVE_CHANNEL` 的实读枚举：0=R 1=G 2=B 3=A
            half PickDissolve(half4 d)
            {
                int i = (int)_DISSOLVE_CHANNEL;
                return (i == 0) ? d.r : (i == 1) ? d.g : (i == 2) ? d.b : d.a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ── 两张贴图（都 NoScaleOffset ⇒ 不做 TRANSFORM_TEX）
                half4 c = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uvMain, _GlobalMipBias.x);
                float2 uvD = IN.uvMain * Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d.xy
                                      + Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d.zw;
                half4 dTex = SAMPLE_TEXTURE2D_BIAS(_Disolve, sampler_Disolve, uvD, _GlobalMipBias.x);

                half3 addC = LinearToSrgb(_Add_Color.rgb);

            #if defined(BOOLEAN_F96BE1E681E64E0E98322A14BDBD35A3_ON)
                // ══ 自定义顶点数据路（材质实际走这支）══
                half  d = PickDissolve(dTex);
                half  A = IN.stream2.z;                                              // 溶解量来自顶点流
                half  W = Vector1_2780b5644c1e463480f9eca2dba65f9a;                  // 带宽
                half3 edge = half3(IN.uvEdge.z, IN.uvEdge.w, IN.stream2.x);          // 边缘色来自顶点流

                half3 baseRGB = c.a * (c.rgb + addC) * IN.color.rgb;
                half  baseA   = c.a * (c.a + _Add_Color.a) * IN.color.a;

                half  cut  = (A >= d) ? 1.0h : 0.0h;
                half  mask = saturate(cut - (((A - W) >= d) ? 1.0h : 0.0h));

                half3 rgb;
            #if defined(BOOLEAN_D80F51B627624648A0B3B2D563AA7541_ON)
                rgb = lerp(baseRGB, baseRGB * edge, mask);                           // MultiplyEdgeColor
            #else
                rgb = baseRGB + mask * edge;
            #endif
                return half4(rgb, baseA * cut);
            #else
                // ══ 材质值路（段 0；30 个材质块没有一支走这里，照实现以保忠实）══
                half  A = Vector1_4e2dbe7bff184ada93bb896e70f4c06d;
                half  W = Vector1_2780b5644c1e463480f9eca2dba65f9a;
                half  mask = saturate(((A >= dTex.r) ? 1.0h : 0.0h) - (((A - W) >= dTex.r) ? 1.0h : 0.0h));
                half3 rgb  = c.a * (c.rgb + addC) * IN.color.rgb + mask * LinearToSrgb(_Color.rgb);
                return half4(rgb, 1.0h);
            #endif
            }
            ENDHLSL
        }
    }

    Fallback Off
}
