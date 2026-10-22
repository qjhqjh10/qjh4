// WFTrailShaderFading.shader — 自建，**逐条照抄**原版 `Everguild/FX/TrailShader_Fading`
//
// 🔴 来由（2026-10-22 · W64 兜底审计 **B11**）：这条原来在 `WarpforgeShaderMap.Replacements` 里
//    指到通用的 `WarpforgeVFX/Particles/Extra Color`（注释自承「近似」）。原版编译字节码已由
//    Ruri 反编译（`资料/普查产出_1020/W58_工具线_Ruri实测.md`）⇒ 按字节码逐条重写，替换那条近似。
//
// 判据（**全部实读**，不是猜）
// ----------------------------
// 源：`d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Everguild_FX_TrailShader_Fading.shader`
//     （主 pass 的片元体内联在同一个文件里，`// Stage: Fragment, Blob: 3`）
// ① 属性（逐字）：`_Color01`(HDR, def 0,1,0.04726887,0) · `_Color02`(HDR, def 0,1,0.8013716,0)
//      `_MainTexture`(NoScaleOffset) · `_MainTextureSpeed`(Vector, def −0.1) · `_DissolveScale`(Float, def 1)
//      `_DissolveSpeed`(Vector, def −0.5) · `_Noise_Combined`(NoScaleOffset) · `_TailLength`(Float, def **0.5**)
//      `_QueueOffset` · `_QueueControl`(−1)
//      ⚠️ 和同族的 `TrailShader_1` 差三处：**没有** `_TRAILALPHACUTOFF`；参数叫 `_TailLength`（不是 `_TrailLenght`，
//         def 0.5 不是 0）；**片元不用顶点色**（`SPIRV_Cross_Input` 里只有 `INTERP : TEXCOORD4`）。
// ② 渲染状态（反编译 pass 块逐字）：`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`
//      · `ZWrite Off` · `Cull Off` ⇒ 混合**硬编码**（材质上没有 `_SrcBlend`），⛔ 不许改成间接寻址。
// ③ 顶点：`o1 = TEXCOORD0` 原样传（不乘 `_ST`）· `clip = unity_MatrixVP · (unity_ObjectToWorld · pos)`。
// ④ 片元（逐条）：
//      t   = _TimeParameters.x
//      n   = SAMPLE(_Noise_Combined, uv * _DissolveScale + t * _DissolveSpeed.xy).z
//      A   = (1 − uv.x * _TailLength) + n.z − uv.x * _TailLength      ; ⚠️ `_TailLength` 项**出现两次**（原样照抄）
//      m   = SAMPLE(_MainTexture, uv + t * _MainTextureSpeed.xy)
//      alp = A * m.r                                                  ; ⚠️ 取 `.r`，不是 `.a`
//      rgb = A * m.rgb * lerp( S(_Color01.rgb), S(_Color02.rgb), uv.x )   ; ⚠️ **rgb 也乘 A**（`_106.._108 = _87 * m.xyz`）
//      o0  = (rgb, alphatoMask 分支) —— 与 `WFTrailShader1` 同一支：
//            fw = |ddx alp| + |ddy alp| ; a2m = saturate(((alp−0.5) − 0.5fw)/max(fw,1e-4) + 1)
//            `_AlphaToMaskAvailable != 0` ⇒ 输出 a2m、裁 a2m−1e-4；否则输出 alp、裁 alp−0.5
//
// ⚠️ 它在 `UseOriginal` 白名单里 ⇒ 正常态走原件；本文件只在兜底路生效。
Shader "WarpforgeVFX/FX/TrailShaderFading"
{
    Properties
    {
        _Color01("Color01", Color) = (0, 1, 0.04726887, 0)
        _Color02("Color02", Color) = (0, 1, 0.8013716, 0)
        _MainTexture("MainTexture", 2D) = "white" {}
        _MainTextureSpeed("MainTextureSpeed", Vector) = (-0.1, 0, 0, 0)
        _DissolveScale("DissolveScale", Float) = 1
        _DissolveSpeed("DissolveSpeed", Vector) = (-0.5, 0, 0, 0)
        _Noise_Combined("Noise Combined", 2D) = "white" {}
        _TailLength("TailLength", Float) = 0.5
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

        // 原版写死在 pass 里 —— ⛔ 不许改成 `Blend [_SrcBlend][_DstBlend]`
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "TrailShaderFading"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color01;
                float4 _Color02;
                float4 _MainTextureSpeed;
                float  _DissolveScale;
                float4 _DissolveSpeed;
                float  _TailLength;
                float  _QueueOffset;
                float  _QueueControl;
            CBUFFER_END

            TEXTURE2D(_MainTexture);    SAMPLER(sampler_MainTexture);
            TEXTURE2D(_Noise_Combined); SAMPLER(sampler_Noise_Combined);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv0        : TEXCOORD0;   // 拖尾 UV（原版 vs 的 v3）
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv         : TEXCOORD0;   // ps 的 v1
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv0;          // 原版 `mov o1.xyzw, v3.xyzw` —— **不做 TRANSFORM_TEX**
                return OUT;
            }

            /// linear → sRGB **编码**（与 `WFTrailShader1` 同一支、同一组常数）
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
                float  t  = _TimeParameters.x;
                float2 uv = IN.uv.xy;

                half n = SAMPLE_TEXTURE2D_BIAS(_Noise_Combined, sampler_Noise_Combined,
                            float2(uv.x * _DissolveScale + t * _DissolveSpeed.x,
                                   uv.y * _DissolveScale + t * _DissolveSpeed.y), _GlobalMipBias.x).z;
                // A = (1 − uv.x*TailLength) + n.z − uv.x*TailLength     （`_TailLength` 出现两次，见文件头 ④）
                half A = (1.0h - uv.x * _TailLength) + n - uv.x * _TailLength;

                half4 m = SAMPLE_TEXTURE2D_BIAS(_MainTexture, sampler_MainTexture,
                            float2(uv.x + t * _MainTextureSpeed.x,
                                   uv.y + t * _MainTextureSpeed.y), _GlobalMipBias.x);

                half alp = A * m.r;                       // ⚠️ m.r，不是 m.a
                half3 rgb = A * m.rgb
                          * lerp(LinearToSrgb(_Color01.rgb), LinearToSrgb(_Color02.rgb), uv.x);

                // ---- 输出 alpha + 裁剪（alpha-to-mask 可用性分叉，`cb0[4].z`）
                half fw  = abs(ddx(alp)) + abs(ddy(alp));
                half a2m = saturate(((alp - 0.5h) - 0.5h * fw) / max(fw, 0.0001h) + 1.0h);
                bool useM = _AlphaToMaskAvailable != 0.0;
                half outA = useM ? a2m : alp;
                clip((useM ? (a2m - 0.0001h) : (alp - 0.5h)));

                return half4(rgb, outA);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
