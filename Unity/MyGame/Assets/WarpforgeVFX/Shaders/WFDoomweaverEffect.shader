// WFDoomweaverEffect.shader — 自建，**逐条照抄**原版 `Shader Graphs/Doomweaver effect`
//
// 🔴 来由（2026-10-22 · W64 兜底审计 **B11**）：这条原来在 `WarpforgeShaderMap.Replacements` 里
//    指到通用的 `WarpforgeVFX/Particles/Extra Color`（注释自承「近似」）。原版编译字节码已由
//    Ruri 反编译（`资料/普查产出_1020/W58_工具线_Ruri实测.md`）⇒ 按字节码逐条重写。
//    判据 → `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Shader Graphs_Doomweaver effect.shader`
//    以及同目录 `Shader Graphs_Doomweaver effect/Sub0_Pass0_Fragment_b{3,4}.hlsl`。
//
// 这不是「粒子贴图 × 色」—— 是**边框 + 主题贴图**：
//   ① 贴图 × 顶点色 × `_ExtraColorMultiplier`
//   ② 边框项 = `smoothstep` 差值 × `LinearToSRGB(_FadeInBorderColor)` × `_ExtraBorderColorMultiplier` × （`s0` × 贴图 alpha）
//
// 判据（全部实读）
// ----------------
// ① 属性（名字/默认值逐字）：`_MainTex`("white") · `_BorderSharpness`(Vector, **(0.75,1,0,0)**)
//      `_ExtraColorMultiplier`(1) · `_FadeInBorderColor`(HDR, **(0.05418388,1.682742,2.297397,0)**)
//      `_FadeInBorderWidth`(0.1) · `_Float`(Range(0,1), 0) · `_ExtraBorderColorMultiplier`(2)
//      ＋ URP 标准那一组（`_Surface`/`_Blend`/`_SrcBlend`/`_DstBlend`/`_SrcBlendAlpha`/`_DstBlendAlpha`/
//        `_ZWrite`/`_ZWriteControl`/`_ZTest`/`_Cull`/`_AlphaToMask`/`_QueueOffset`/`_QueueControl`/`_CastShadows`）
// ② 渲染状态（反编译 pass 逐字）：
//      `Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]`
//      `AlphaToMask [_AlphaToMask]` · `ZTest [_ZTest]` · `ZWrite [_ZWrite]` · `Cull [_Cull]`
//      ⇒ 这一支**是间接寻址**的（与 TrailShader_1 那族相反），材质上的值是真值。
// ③ 顶点：`INTERP = TEXCOORD0`（原样传，前三个分量片元要用）· `INTERP_1 = COLOR0`（顶点色原样传）。
// ④ 片元（`Fragment_b3` = 不透明支，`Fragment_b4` = `_SURFACE_TYPE_TRANSPARENT` 支；**只有 alpha 那一行不同**）：
//      e     = 2*uv.z + uv.y − 1                       ; `_88 = mad(INTERP.z, 2, INTERP.y) − 1`
//      s0    = smooth(clamp((e − sharp.x) / (sharp.y − sharp.x)))      ; smooth(s) = s²(3−2s)
//      s1    = smooth(clamp((e − fadeWidth − sharp.x) / (sharp.y − sharp.x)))
//      border= (s0 − s1) * LinearToSRGB(_FadeInBorderColor.rgb)
//      tex   = SAMPLE(_MainTex, uv.xy) * vcol
//      rgb   = _ExtraColorMultiplier * tex.rgb + border * _ExtraBorderColorMultiplier * (s0 * tex.a)
//      a     = 1（不透明支）· saturate(s0 * tex.a)（透明支）
//      ⚠️ `_Float` 在**两个已编译变体里都没被读**（原版每 pass 只编了这两支）⇒ 按**铁律 11 例外①**只声明、
//         不参与计算；⛔ 别自己给它编一个语义。
//
// ⚠️ 它在 `UseOriginal` 白名单里 ⇒ 正常态走原件；本文件只在兜底路生效。
Shader "WarpforgeVFX/FX/DoomweaverEffect"
{
    Properties
    {
        [NoScaleOffset] _MainTex("MainTex", 2D) = "white" {}
        _BorderSharpness("BorderSharpness", Vector) = (0.75, 1, 0, 0)
        _ExtraColorMultiplier("ExtraColorMultiplier", Float) = 1
        [HDR] _FadeInBorderColor("FadeInBorderColor", Color) = (0.05418388, 1.682742, 2.297397, 0)
        _FadeInBorderWidth("FadeInBorderWidth", Float) = 0.1
        _Float("Float", Range(0, 1)) = 0
        _ExtraBorderColorMultiplier("ExtraBorderColorMultiplier", Float) = 2

        [HideInInspector] _CastShadows("_CastShadows", Float) = 1
        [HideInInspector] _Surface("_Surface", Float) = 1
        [HideInInspector] _Blend("_Blend", Float) = 0
        [HideInInspector] _AlphaClip("_AlphaClip", Float) = 0
        [HideInInspector] _SrcBlend("_SrcBlend", Float) = 1
        [HideInInspector] _DstBlend("_DstBlend", Float) = 0
        [HideInInspector] _SrcBlendAlpha("_SrcBlendAlpha", Float) = 1
        [HideInInspector] _DstBlendAlpha("_DstBlendAlpha", Float) = 0
        [HideInInspector] _ZWrite("_ZWrite", Float) = 0
        [HideInInspector] _ZWriteControl("_ZWriteControl", Float) = 0
        [HideInInspector] _ZTest("_ZTest", Float) = 4
        [HideInInspector] _Cull("_Cull", Float) = 2
        [HideInInspector] _AlphaToMask("_AlphaToMask", Float) = 0
        [HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1
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

        Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
        AlphaToMask [_AlphaToMask]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Universal Forward"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // 原版：`#pragma multi_compile_local _ _SURFACE_TYPE_TRANSPARENT`（逐字）
            // ⚠️ 用 `shader_feature_local`：`_SURFACE_TYPE_TRANSPARENT` 也是 URP 内置关键字，加 `_local` 避免打架。
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BorderSharpness;
                float  _ExtraColorMultiplier;
                float4 _FadeInBorderColor;
                float  _FadeInBorderWidth;
                float  _Float;
                float  _ExtraBorderColorMultiplier;
                float  _CastShadows;
                float  _Surface;
                float  _Blend;
                float  _AlphaClip;
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
                float3 uv         : TEXCOORD0;   // ps 的 INTERP（只用前三个分量）
                float4 color      : TEXCOORD1;   // ps 的 INTERP_1
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv0.xyz;      // 原版 `mov o1.xyzw, v3.xyzw`（不做 TRANSFORM_TEX）
                OUT.color = IN.color;     // 原版 `mov o1_1.xyzw, v4.xyzw`
                return OUT;
            }

            /// linear → sRGB **编码**（原版那 7 条；与 `WFTrailShader1` 同一支、同一组常数）
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            /// `s*s*(3-2s)`（原版 `_118 = _111*_111; _119 = _118 * mad(_111,-2,3)`）
            half Smooth01(half s) { return s * s * (3.0h - 2.0h * s); }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 uv = IN.uv;

                // e = 2*uv.z + uv.y − 1
                half e   = 2.0h * uv.z + uv.y - 1.0h;
                half lo  = _BorderSharpness.x;
                half hi  = _BorderSharpness.y;
                half den = hi - lo;

                half s0 = Smooth01(saturate((e - lo) / den));
                half s1 = Smooth01(saturate((e - _FadeInBorderWidth - lo) / den));

                half3 border = (s0 - s1) * LinearToSrgb(_FadeInBorderColor.rgb);

                half4 tex = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, uv.xy, _GlobalMipBias.x) * IN.color;

                half3 rgb = _ExtraColorMultiplier * tex.rgb
                          + border * _ExtraBorderColorMultiplier * (s0 * tex.a);

            #ifdef _SURFACE_TYPE_TRANSPARENT
                half a = saturate(s0 * tex.a);
            #else
                half a = 1.0h;
            #endif

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
