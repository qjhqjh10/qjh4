// WFParticlePremultiplyGreyscale.shader — 自建，替代原版 `Everguild/FX/Particle Premultiply Greyscale Coloring`
//
// 🔴 **来由**（2026-10-02 第三批）：它原来被顶替成通用的 `WarpforgeVFX/Particles/Extra Color`
//    （只做「一张图 × 顶点色」）—— 而原版是**拿主图红通道当灰度、在两个颜色之间插值、再预乘 alpha**。
//
// 判据（**全部实读**）—— `_tmp_view/dxbc/premul_grey{,_vs}.txt`
// ---------------------------------------------------------------------------
// ① **渲染状态**：5 个 pass，pass0 的 blend/z **全是属性引用**，而 40+ 材质实测**完全一致**：
//      `_SrcBlend=1`/`_DstBlend=10`/`_SrcBlendAlpha=1`/`_DstBlendAlpha=10` · `_ZWrite=0` · `_ZTest=4` ·
//      `_Cull=2`(Back) · `_AlphaToMask=0` ⇒ **`Blend One OneMinusSrcAlpha`（预乘）· ZWrite Off · Cull Back**。
//      Queue=Transparent(3000)。⚠️ 状态仍按属性寻址（原版就是那样）。
// ② **属性名逐字照抄**（24 个）：`_MainTex`(NoScaleOffset) · `_Color`(HDR) · `_Color2`(HDR) ·
//    `_ENABLEVERTEXSTREAMS` · `_SOFTPARTICLES` · `_Depth_And_Offset` + 一整套 `_SrcBlend` 状态属性。
// ③ **顶点**：正则的 `unity_MatrixVP·(unity_ObjectToWorld·v0)`；插值器 `o1=uv0`、`o4=COLOR`
//    （输入签名：POSITION=0·NORMAL=1·TANGENT=2·TEXCOORD0=3·TEXCOORD1=4·TEXCOORD2=5·COLOR=**6**）。
// ④ **片元**（31 条，1 次采样；逐条在 `premul_grey.txt` 第 82-113 行）：
//      t    = tex(`_MainTex`, uv0.xy)                    ; 无 `_ST`，bias = `_GlobalMipBias.x`
//      A    = **LinearToSRGB**(cb1[3].rgb) · B = **LinearToSRGB**(cb1[1].rgb)
//      grey = t.r
//      rgb  = t.r · lerp(B, A, t.r) = t²·A + t(1−t)·B    ; ⚠️ 比普通 lerp **多乘了一次 t**
//      rgb *= vcol.rgb
//      alpha = t.a · vcol.a
//      o0.xyz = alpha · rgb                              ; ← **预乘在这里**（与 `Blend One OneMinusSrcAlpha` 自洽）
//      o0.w   = alpha（`_SURFACE_TYPE_TRANSPARENT` 那支）｜ 1.0（另一支）
//    🔴 **`o0.w` 两支持平**：判据是 **URP 自己的源码**（不是猜）—— `UnlitPass.hlsl` 末行
//      `finalColor.a = OutputAlpha(finalColor.a, isTransparent)`，而 `OutputAlpha` = `isTransparent ? alpha : 1.0`
//      ⇒ **没开 `_SURFACE_TYPE_TRANSPARENT` 的那支才是 `o0.w = 1`**。40+ 材质**全部只带这一个关键字**
//      ⇒ 实机走 `o0.w = alpha` 那支。**必须声明该 pragma**（不声明 `#ifdef` 恒假，2026-10-02 踩过）。
// ⑤ **如实标注（推 + 存疑）**：
//    · `cb1[3] = `_Color`（灰度=1 端）· `cb1[1] = `_Color2`（灰度→0 端）**—— 名字是按「哪端该亮」推的
//      （`FireBlack` 材质火心 `_Color=(0,0,0)`、边缘 `_Color2` 是绿 ⇒ 「黑火」要求火心=黑 ✓）。
//      ⚠️ **槽序与同族规则对不上**（`Multi Ray`/`Shine` 那两件的「贴图 _ST 在前、其余按声明序」会把两色放在
//      `[1]/[2]`，这里却在 `[1]/[3]`）⇒ 标 **== 存疑 ==**；若 A/B 偏大，**先试把这两个换过来**。
//    · `_ENABLEVERTEXSTREAMS` / `_SOFTPARTICLES` / `_Depth_And_Offset` 在**已编译变体里一次都没出现**
//      （这个 build 没编出那几支）⇒ 照不到判据，本文件**不实现**（软粒子那支是空的）。
//    · `_Color`/`_Color2` 的 **alpha 完全没进算式**（只用 .rgb）—— 预乘用的 alpha 是 `tex.a × vcol.a` ✓。
//
// ⚠️ **白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/ParticlePremultiplyGreyscale"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (1, 1, 1, 0)
        [HDR] _Color2("Color2", Color) = (1, 0, 0, 1)
        _ENABLEVERTEXSTREAMS("EnableVertexStreams", Float) = 0
        [Toggle(_SOFTPARTICLES)] _SOFTPARTICLES("SoftParticles", Float) = 0
        _Depth_And_Offset("Depth And Offset", Vector) = (0.3, 1, 0, 0)

        _Surface("__surface", Float) = 1
        _Blend("__blend", Float) = 1
        _AlphaClip("__clip", Float) = 0
        _SrcBlend("__src", Float) = 1
        _DstBlend("__dst", Float) = 10
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 10
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

        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Pass
        {
            Name "ParticlePremultiplyGreyscale"
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;            // 原版 NoScaleOffset ⇒ 不参与
                float4 _Color;
                float4 _Color2;
                float  _ENABLEVERTEXSTREAMS;
                // ⚠️ **同样不能声明 `float _SOFTPARTICLES;`** —— 它是 `[Toggle(_SOFTPARTICLES)]` 的关键字名，
                //    一旦有材质把它打开，Unity 就 `#define _SOFTPARTICLES` ⇒ 与同名变量重定义、编不过。
                //    （本文件也没用到它的值 —— 软粒子那支这个 build 没编出来，见文件头 ⑤。）
                float4 _Depth_And_Offset;
                float  _Surface, _Blend, _AlphaClip;
                float  _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha;
                float  _ZWrite, _ZWriteControl, _ZTest, _Cull, _AlphaToMask;
                float  _QueueOffset, _QueueControl;
                float  _CastShadows;
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
                float4 uv0        : TEXCOORD0;   // ps 的 v1
                float4 color      : TEXCOORD1;   // ps 的 v4
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// linear → sRGB **编码**（原版那 7 条；常量逐字来自 DXBC）
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv0   = IN.uv0;     // 无 `_ST` ⇒ 不做 TRANSFORM_TEX
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uv0.xy, _GlobalMipBias.x);

                half3 A = LinearToSrgb(_Color.rgb);      // 灰度 = 1 端（== 存疑 ==，见文件头 ⑤）
                half3 B = LinearToSrgb(_Color2.rgb);     // 灰度 → 0 端
                half  g = t.r;

                // `t·lerp(B,A,t)` = t²·A + t(1−t)·B（逐条：mul / mad / mad）—— **不是普通 lerp**
                half3 rgb = g * (B + g * (A - B)) * IN.color.rgb;
                half  a   = t.a * IN.color.a;

            #if defined(_SURFACE_TYPE_TRANSPARENT)
                return half4(a * rgb, a);                // 预乘
            #else
                return half4(a * rgb, 1.0h);
            #endif
            }
            ENDHLSL
        }
    }

    Fallback Off
}
