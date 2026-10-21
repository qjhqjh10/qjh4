// WFParticlesExtraColorEC.shader — 自建，**只**用来替代原版 `Everguild/FX/Extra Color`
//
// 🔴 **2026-10-19（T02 的持久修法）：从 `WFParticlesExtraColor.shader` 拆出来的专用变体。**
//
// **为什么要拆**：原来 `WarpforgeShaderMap.Replacements` 里一条
//   `Everguild/FX/Extra Color → WarpforgeVFX/Particles/Extra Color`
// **同时顶了 8 个原版 shader**（EC 741 处 + `Mobile/Particles/{Additive,Alpha Blended}` 582 处
// + `Particles/Standard Unlit` 等）。而 **`_Color` 只对 EC 这一族有意义**，对内置族是死值（那些认 `_TintColor`）
// ⇒ 统一乘一次 = 修好一族、弄坏一族（2026-10-02 那次「乘回去 ⇒ 全量 A/B 退步」正是这个原因）。
// 拆开之后：
//   · **EC** → 本文件（算式含 `LinearToSRGB(_Color)`）；
//   · 其余 7 个内置管线名 → 继续用 `WFParticlesExtraColor.shader`（`tex × vcol`）。
//
// **判据 = 原版 `Everguild/FX/Extra Color` 的编译字节码逐条读出来的**（不是猜）：
//   全文 → `资料/普查产出_第二十三会话/V23_特效ExtraColor的PS读数.md`。
//   ① VS 的 `UnityPerMaterial` 绑在 **`cb2`**，`slot[1]`（off=16）经序列化反射解名 = **`_Color`**；
//   ② **32/32 个 VS 变体**都写 `o2.xyz = LinearToSRGB(_Color.rgb) × vcol.rgb`、
//      **`o2.w = vcol.w × _Color.w`**；
//   ③ Universal Forward 的 **12 个 PS 变体**（去重 3 种形状）全都把 `v2`（= o2）乘进最终色：
//      `mul o0.xyzw, r0.xyzw, v2.xyzw` / `mul o0.xyz, r0.xyzx, v2.xyzx` / 软粒子那支 `r1 = tex*v2`。
//   ⇒ **最终色 = `tex × LinearToSRGB(_Color) × vcol`**；alpha = `tex.a × _Color.a × vcol.a`。
//
// **量级自检（兜底路 = 我们 / 原版）**：`1 / LinearToSRGB(_Color.r)` ——
//   `_Color=2` → 0.739 · `4` → 0.548 · `8` → 0.407 · **`766.996` → 0.060**。
//   与 `资料/普查产出_1019/W_特效线_残余_1019b.md` §一·T02·C 的倍率表逐条一致
//   （`LightningTrail_intense` 就是 766.996 那条）。
//   按本文件低/高两支的常量实算：`LinearToSRGB(2) = 1.353` · `LinearToSRGB(4) = 1.825` ·
//   `LinearToSRGB(8) = 2.454` · `LinearToSRGB(766.996) ≈ 16.74` ⇒ 倒数 0.739 / 0.548 / 0.407 / 0.060。
//   ⚠️ 旧文件头里写的「4 → 1.851」是手算误差，**以本文件这两个数（1.825 / 16.74）为准**。
//
// **原版属性表**（21 项，`资料/普查产出_0917/shader属性表_块1.md:278` 逐字）里有
// `_Color`(HDR, def=(1,1,1,**0**))，**没有 `_EmissionColor`** ⇒ 本文件不声明那个属性
// （旧的那份里它是 09-13 凭空加的，2026-09-18 已把用处删掉，留着一个死属性只会误导）。
//
// ⚠️ **未编译、未渲染**（本机只有 Unity 能编 HLSL；本轮按用户口径不跑 Unity）。
//   静态检查：属性名/关键字/两个 pass 的 `UnityPerMaterial` 布局与旧文件逐字段对齐。
//   ⚠️ 需要重跑一次 `WarpforgeSetup.RegisterShaders` 才会进 Always Included Shaders
//      （它遍历 `WarpforgeShaderMap.Replacements.Values`，本文件已在表里）。
Shader "WarpforgeVFX/Particles/Extra Color EC"
{
    Properties
    {
        // 🔴 默认值照原版属性表 = `(1,1,1,**0**)`（不是旧那份的 a=1）。
        //    `_Color.a` 参与最终 alpha（`o2.w = vcol.w × _Color.w`）⇒ 默认 a=0 时原版也是不可见，
        //    这里照抄才是对的。材质里显式写了 `_Color` 的（绝大多数）不受影响。
        [HDR] _Color("Color", Color) = (1,1,1,0)
        _MainTex("Main Texture", 2D) = "white" {}

        // 关键字名照原版：`_SOFTPARTICLES`（**没有 `_ON` 后缀**）。
        [Toggle(_SOFTPARTICLES)] _SOFTPARTICLES("Soft Particles", Float) = 0
        _SoftParticlesFadeDistance("    Soft Fade Distance", Range(0.01, 20)) = 1.0

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        // ⚠️ 原版属性表里没有 `_Cutoff`（alpha clip 的阈值是烘死的 0.5）⇒ 默认值必须 0.5。
        _Cutoff("    Alpha Cutoff", Range(0,1)) = 0.5

        // ---- 与原版同名的渲染状态（URP ShaderGraph 的标准一组）----
        _Surface("__surface", Float) = 1
        _Blend("__blend", Float) = 0
        _SrcBlend("__src", Float) = 5      // SrcAlpha
        _DstBlend("__dst", Float) = 10     // OneMinusSrcAlpha
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 1
        _ZWrite("__zw", Float) = 0
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 2
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = 0
        [HideInInspector] _CastShadows("_CastShadows", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        // 🔴 2026-10-21（T17 · Ruri 反编译对拍）：原来这里写「粒子材质几乎不写 alpha 通道 ⇒ 只用两参数形式」，
        //    **那句不成立** —— 原版 pass0 就是 `Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]`
        //    （色与 alpha **分开寻址**），且材质的 `_SrcBlendAlpha`/`_DstBlendAlpha` 真的带值
        //    （工程 969 个 .mat 现读：`_SrcBlendAlpha` 1→555 / 5→359 · `_DstBlendAlpha` 10→444 / 1→358 / 0→120）。
        //    判据 = `Everguild/FX/Extra Color` 反编译体（Ruri 导出，pass0）。
        Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull [_Cull]
        AlphaToMask [_AlphaToMask]

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _SOFTPARTICLES
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // 原版 EC 的关键字表里有 `_ALPHAPREMULTIPLY_ON`（9 个 `_SrcBlend=One + _DstBlend=OneMinusSrcAlpha` 的材质）
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float  _Cutoff;
                float  _SoftParticlesFadeDistance;
                float  _Surface;
                float  _Blend;
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
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                float  eyeDepth   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// linear → sRGB **编码**（常量与分支逐字来自 DXBC；与 `WFAlphaMasksTwoLayer` / `WFMatcap` 同一份）
            /// ⚠️ 用 `float3` 而不是 `half3`：`_Color` 是 HDR，盘上有 `766.996` 这种值，
            ///    `pow(766.996, 1/2.4)` 用 half 会丢精度。
            float3 LinearToSrgb(float3 c)
            {
                float3 hi = 1.055000 * pow(abs(c), 0.416667) - 0.055000;
                float3 lo = c * 12.923210;
                return float3(c.r <= 0.003131 ? lo.r : hi.r,
                              c.g <= 0.003131 ? lo.g : hi.g,
                              c.b <= 0.003131 ? lo.b : hi.b);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;
                OUT.eyeDepth   = p.positionWS.z;   // 视图空间深度（负值）
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 🔴 本文件与旧那份的唯一实质差别（判据见文件头）：
                //    原版最终色 = tex × LinearToSRGB(_Color) × vcol；alpha 也要乘 `_Color.a`。
                //    （原版是在 VS 里算 `o2 = LinearToSRGB(_Color) × vcol` 再插值过来；
                //      `LinearToSRGB(_Color)` 是**逐材质常量** ⇒ 搬到 PS 逐像素算，结果等价。）
                float4 tint = float4(LinearToSrgb(_Color.rgb), _Color.a);
                half4 col = tex * (half4)tint * IN.color;

                // 预乘：原版部分材质开了 `_ALPHAPREMULTIPLY_ON`（`_SrcBlend=One` + `_DstBlend=OneMinusSrcAlpha`）
                #ifdef _ALPHAPREMULTIPLY_ON
                    col.rgb *= col.a;
                #endif

                // 软粒子：按与场景深度的差做淡出。URP 粒子 ShaderGraph 的标准做法
                #ifdef _SOFTPARTICLES
                    float2 suv = IN.positionCS.xy / _ScreenParams.xy;
                    float  rawDepth = SampleSceneDepth(suv);
                    float  sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float  partEye  = -IN.eyeDepth;
                    float  fade = saturate((sceneEye - partEye) / max(1e-4, _SoftParticlesFadeDistance));
                    col.a *= fade;
                #endif

                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                return col;
            }
            ENDHLSL
        }

        // 影子投射（原版 _CastShadows 控制）。粒子一般不开，留着以免报缺 pass
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ⚠️ 必须与主 pass 的 UnityPerMaterial **逐字段同序**（Unity 的规矩）
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float  _Cutoff;
                float  _SoftParticlesFadeDistance;
                float  _Surface; float _Blend; float _SrcBlend; float _DstBlend;
                float  _SrcBlendAlpha; float _DstBlendAlpha; float _ZWrite;
                float  _ZWriteControl; float _ZTest; float _Cull; float _AlphaToMask;
                float  _QueueOffset; float _QueueControl; float _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct SA { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct SV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            SV ShadowVert(SA IN)
            {
                SV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                o.color = IN.color;
                return o;
            }

            half4 ShadowFrag(SV IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _Color.a * IN.color.a;
                #ifdef _ALPHATEST_ON
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
