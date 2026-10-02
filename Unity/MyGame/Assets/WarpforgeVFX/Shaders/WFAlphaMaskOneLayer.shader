// WFAlphaMaskOneLayer.shader — 自建，替代原版 `Everguild/FX/Alpha Mask One Layer`
//
// 🔴 **来由**（2026-10-02 第三批）：它原来被顶替成通用的 `WarpforgeVFX/Particles/Extra Color`
//    （只做「一张图 × 顶点色」）—— 而原版是**两张图各滚各的 UV、逐通道相乘、再经 alpha 幂曲线**。
//    兜底路上它是出现次数最多的那一个（审计表前 20 条里 8 条带它），`Recon Scan` 一家 0.44 上下。
//
// 判据（**全部实读**）—— 证据包 `_tmp_view/fam/alpha_mask_one.md`（反汇编 `_tmp_view/dxbc/alpha_mask_one.txt`）
// ---------------------------------------------------------------------------
// ① **渲染状态**：pass0 的 `rtBlend0` 全是**属性引用**，而材质里**两种活组合都有**
//    （`_SrcBlend=5`+`_DstBlend=10` 常规透明 / `5`+`1` 加色），`_Cull` 也有 **2 与 0** 两种
//    ⇒ **必须间接寻址**（写死会让加色那一批全错）。`ZWrite [_ZWrite]`(材质 0) · `ZTest [_ZTest]`(4) · AlphaToMask(0)。
//    Queue=Transparent(3000)。⚠️ 原版 5 个 pass（1=ShadowCaster · 2=MotionVectors · 3=DepthNormals · 4=DepthOnly），
//    本文件只做 pass0；**全篇没有 discard/clip**（材质 `_AlphaClip` 全 0）。
// ② **属性名逐字照抄**（25 个里真读的只有 7 个；⚠️ **`_Color_Mutliplier` 是原版就拼错的**
//    —— 写成 `Multiplier` 会让材质值**灌不进来**）：
//      `_MainTex` · `_PrimaryTex_Scale_XY_Speed_ZW` · `_SecondaryTex` · `_SecondaryTex_Scale_XY_Speed_ZW` ·
//      `_Color_Mutliplier` · `_Alpha_Modifier`(Range 0.01–2) · `_Alpha_Modifier_Multiply`
//      两张贴图都是 **NoScaleOffset** ⇒ 没有 `_ST`，UV 全在顶点算。
// ③ **顶点**（vs）：`clip = unity_MatrixVP·(unity_ObjectToWorld·v0)`；两条 UV（`t = cb0[19].x = _TimeParameters.x`）：
//      uvA = (uv0.x + uv0.z, uv0.y + uv0.z) · `_PrimaryTex…`.xy   + frac(t · `_PrimaryTex…`.zw)  → o2.zw → PS 采 **t1 = _MainTex**
//      uvB =  uv0.xy                        · `_SecondaryTex…`.xy + frac(t · `_SecondaryTex…`.zw) → o2.xy → PS 采 **t0 = _SecondaryTex**
//    🔴 **只有第一条多加 `uv0.z`**（加在 x 和 y 两个分量上）；第二条不加。两条都**只滚 offset**（`frac(t·speed)` 加在偏移上）。
// ④ **片元**（ps 段 1 = `_SURFACE_TYPE_TRANSPARENT` 那支）：
//      A = tex(`_SecondaryTex`, uvB) · B = tex(`_MainTex`, uvA)      ; 都带 `_GlobalMipBias.x` 的 mip bias
//      r = A * B                                                      ; **逐通道乘，rgb 与 alpha 一起**
//      r.rgb *= `_Color_Mutliplier` ;  r.a *= `_Alpha_Modifier_Multiply`
//      o0.rgb = r.rgb * vcol.rgb
//      o0.a   = saturate( pow(r.a, `_Alpha_Modifier`) * vcol.a )
//    · **段 0（不透明支）**：`o0.rgb` 同上，`o0.a = 1.0` ⇒ 由 `_SURFACE_TYPE_TRANSPARENT` 分叉（**必须配 pragma**）。
//    · 🔴 名字里的 **「One Layer」= 两张图的 alpha 相乘**，**没有**独立遮罩贴图、**没有** blend color
//      （对照 `Alpha Masks Two Layer` 那支才是「一层遮罩 + `_Blend_Color`」）。
//    · rgb **不预乘 alpha**；`mul_sat` **只饱和 o0.w**、rgb 不饱和（HDR 素材能超 1）。
// ⑤ **推的地方（已标注，A/B 会验）**：`cb1[4]` 的 `.x=指数(.Alpha_Modifier)` / `.y=颜色乘子` / `.z=alpha 乘子`
//    是按「角色 + 材质值落在 Range 内 + 与样板 `WFAlphaMasksTwoLayer` 的分工一致」推的（无 RDEF）；
//    两张图的 t0/t1 配对同理（按 Properties 声明序）。**若反了，A/B 会明显偏大。**
//
// ⚠️ **它在白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/AlphaMaskOneLayer"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _PrimaryTex_Scale_XY_Speed_ZW("PrimaryTex Scale(XY) Speed(ZW)", Vector) = (1, 1, 0, 0)
        _SecondaryTex("SecondaryTex", 2D) = "white" {}
        _SecondaryTex_Scale_XY_Speed_ZW("SecondaryTex Scale(XY) Speed(ZW)", Vector) = (0.5, 0.5, 1, 1)
        _Color_Mutliplier("Color Mutliplier", Float) = 1
        _Alpha_Modifier("Alpha Modifier", Range(0.01, 2)) = 1
        _Alpha_Modifier_Multiply("Alpha Modifier Multiply", Float) = 2

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
        _CastShadows("_CastShadows", Float) = 1
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

        // 原版这几项都是**材质属性间接引用**，而且两种活组合都出现过 —— 不许改成字面量
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Pass
        {
            Name "AlphaMaskOneLayer"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;                    // 原版 NoScaleOffset ⇒ 不参与
                float4 _SecondaryTex_ST;
                float4 _PrimaryTex_Scale_XY_Speed_ZW;
                float4 _SecondaryTex_Scale_XY_Speed_ZW;
                float  _Color_Mutliplier;
                float  _Alpha_Modifier;
                float  _Alpha_Modifier_Multiply;
                float  _Surface, _Blend, _AlphaClip;
                float  _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha;
                float  _ZWrite, _ZWriteControl, _ZTest, _Cull, _AlphaToMask;
                float  _QueueOffset, _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_SecondaryTex); SAMPLER(sampler_SecondaryTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 uv0        : TEXCOORD0;   // 用 xyz（z 是第一条 UV 的额外偏移）
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : TEXCOORD0;   // ps 的 v1
                float4 uvPair     : TEXCOORD1;   // ps 的 v2：xy=uvB(副图) · zw=uvA(主图)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;

                float  t = _TimeParameters.x;                                  // cb0[19].x
                float2 uvB = IN.uv0.xy * _SecondaryTex_Scale_XY_Speed_ZW.xy
                           + frac(t * _SecondaryTex_Scale_XY_Speed_ZW.zw);
                // ⚠️ 第一条把 `uv0.z` **加到 x 和 y 两个分量上**（`add r0.zw, v3.zzzz, v3.xyxy`）
                float2 uvA = (IN.uv0.xy + IN.uv0.zz) * _PrimaryTex_Scale_XY_Speed_ZW.xy
                           + frac(t * _PrimaryTex_Scale_XY_Speed_ZW.zw);
                OUT.uvPair = float4(uvB, uvA);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 A = SAMPLE_TEXTURE2D_BIAS(_SecondaryTex, sampler_SecondaryTex, IN.uvPair.xy, _GlobalMipBias.x);
                half4 B = SAMPLE_TEXTURE2D_BIAS(_MainTex,      sampler_MainTex,      IN.uvPair.zw, _GlobalMipBias.x);

                half4 r = A * B;                                   // 逐通道乘（rgb 与 alpha 一起）
                r.rgb *= _Color_Mutliplier;
                r.a   *= _Alpha_Modifier_Multiply;

                half3 rgb = r.rgb * IN.color.rgb;
            #if defined(_SURFACE_TYPE_TRANSPARENT)
                half  a   = saturate(pow(r.a, _Alpha_Modifier) * IN.color.a);
            #else
                half  a   = 1.0h;
            #endif
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
