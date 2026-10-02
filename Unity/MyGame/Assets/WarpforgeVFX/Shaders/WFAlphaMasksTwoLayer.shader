// WFAlphaMasksTwoLayer.shader — 自建，替代原版 `Everguild/FX/Alpha Masks Two Layer`
//
// 🔴 **来由**（2026-10-02，兜底路审计第 3 族）：原来映射到通用的 `WarpforgeVFX/Particles/Extra Color`（只乘）
//    ⇒ 兜底路上三条 `Environmental Condition *` **×8.06 / ×5.34 / ×3.80**，全表第一、第三、第五惨。
//    逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读**）—— 反汇编 `d:/4/_tmp_view/dxbc/alpha_masks_two.txt` 与 `..._vs.txt`
// ---------------------------------------------------------------------------
// ① **渲染状态**（`dump_shader.py`）：pass0 的 `rtBlend0` **全是属性间接引用**
//    （`_SrcBlend`/`_DstBlend`/`_SrcBlendAlpha`/`_DstBlendAlpha`，**不是** `<noninit>`）
//    ⇒ 与 `Mobile/Particles/Multiply`（材质压根没有 `_SrcBlend` ⇒ 必须硬编码）**前提相反**，
//    这里**必须** `Blend [_SrcBlend][_DstBlend]`（9 个材质实测 `_DstBlend` = 1 与 10 **各半**）。
//    `Cull [_Cull]` 同理（8 处 Back / Sororitas 2 处 Off）。`ZWrite [_ZWrite]`（9/9=0）、
//    `ZTest [_ZTest]`（9/9=4）、`AlphaToMask`（9/9=0）。Queue=Transparent(3000)。
//    ⚠️ 原版 5 个 pass（1=ShadowCaster · 2=MotionVectors · 3=DepthNormals · 4=DepthOnly，都是推的），
//      本文件只做 pass0。
// ② **顶点**（`alpha_masks_two_vs.txt` 段 0，逐条）：
//      clip = `unity_MatrixVP · (unity_ObjectToWorld · v0)` · `o1 = v3(TEXCOORD0)` · `o2 = v4(COLOR)`
//      **两层噪声 UV 在顶点算**：
//        base  = (uv.x + uv.z, uv.y + uv.z)                       ; `add r0.zw, v3.zzzz, v3.xyxy`
//        o3.xy = base · `_Noise1ScaleAndOffset.xy` + frac(t · `_Noise1ScaleAndOffset.zw`)   ; cb2[3]
//        o3.zw = base · `_Noise2ScaleAndOffset.xy` + frac(t · `_Noise2ScaleAndOffset.zw`)   ; cb2[4]
//      （`t` = `cb0[19].x` = `_TimeParameters.x`，与 `WFTrailShader1.shader` 同一套槽位判据）
// ③ **片元**（段 1 = 47 条，逐条翻译）：
//      K   = tex(_MainTex, v1.xy).a · v2.a                        ; v2 = 顶点色
//      n   = tex(_NoiseTex1, v3.xy)[NOISE1CH] · tex(_NoiseTex2, v3.zw)[NOISE2CH]
//      C   = LinearToSRGB(_Blend_Color.rgb)
//      b   = tex(_MainTex, v1.xy).rgb · v2.rgb · `_Color_Multiplier`
//      ov  = Overlay(C, b)     ; 逐通道：b ≤ 0.5 ? 2Cb : 1 − 2(1−C)(1−b)
//      o0.rgb = C + `_Blend_Color_Opacity` · (ov − C)             ; = lerp(C, ov, 不透明度)
//      o0.a   = K · min( pow(n · K · _Alpha_Multiplier_2, _AlphaMultiplier), 1 )
//    🔴 两个标量的归属：**`_Blend_Color_Opacity` = 最终 lerp 权重 · `_Color_Multiplier` = Overlay 的乘数**
//      —— 与初版推的**相反**，2026-10-02 用 `EffectCompare` + `WFCMP_MATDUMP` 实测定的（见 frag 里那段）。
//    ⚠️ **两条颜色都走 `LinearToSRGB`**（`log/×0.416667/exp/×1.055−0.055` + `ge 0.003131`/`×12.92321`）
//      —— 是**编码**（linear→sRGB），不是解码。常量逐字来自 DXBC。
//    🔴 `t0/t1/t2` 与属性名的配对（**== 推 ==**，无 RDEF）：`t0 = _MainTex`（UV 原样，NoScaleOffset）；
//      `t2 = _NoiseTex1`（配 `v3.xy` / `_Noise1ScaleAndOffset`）、`t1 = _NoiseTex2`（配 `v3.zw`）。
//      依据：9 个材质的 (N1,N2) 取值集合与 9 个 ps 变体的 swizzle 组合**完全一致**，换一种指派就会落空。
//      ⇒ **配对才是判据**（乘积可交换）：用 `_Noise1ScaleAndOffset` 算出的 UV 那张图，取 `_NOISE1CHANNEL` 的通道。
// ④ **通道枚举**：`_NOISE1CHANNEL` / `_NOISE2CHANNEL` 的 **0/1/2/3 = R/G/B/A**（11 个材质实例的关键字名
//    与数值逐一对上）。我们按**数值**分支（不按关键字）—— 9 个变体只是 swizzle 不同，数值分支等价且更稳。
//
// ⚠️ **它在白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/AlphaMasksTwoLayer"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Color_Multiplier("Color Multiplier", Float) = 1
        _AlphaMultiplier("AlphaMultiplier", Range(0.01, 2)) = 0.5
        _Alpha_Multiplier_2("Alpha Multiplier 2", Float) = 1
        [HDR] _Blend_Color("Blend Color", Color) = (0, 0, 0, 0)
        _Blend_Color_Opacity("Blend Color Opacity", Range(0, 1)) = 0
        _NoiseTex1("NoiseTex1", 2D) = "white" {}
        _NOISE1CHANNEL("Noise1Channel", Float) = 0
        _Noise1ScaleAndOffset("Noise1ScaleAndOffset", Vector) = (1, 1, 1, 0)
        _NoiseTex2("NoiseTex2", 2D) = "white" {}
        _NOISE2CHANNEL("Noise2Channel", Float) = 0
        _Noise2ScaleAndOffset("Noise2ScaleAndOffset", Vector) = (1, 1, 0, 0)
        _CHANNELSELECTOR("ChannelSelector", Float) = 0

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

        // 原版这三项都是**材质属性间接引用**（9 个材质实测两派各半）—— 不许改成字面量
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Pass
        {
            Name "AlphaMasksTwoLayer"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;               // 原版 NoScaleOffset ⇒ 不参与，只为占位
                float  _Color_Multiplier;
                float  _AlphaMultiplier;
                float  _Alpha_Multiplier_2;
                float4 _Blend_Color;
                float  _Blend_Color_Opacity;
                float  _NOISE1CHANNEL;
                float4 _Noise1ScaleAndOffset;
                float  _NOISE2CHANNEL;
                float4 _Noise2ScaleAndOffset;
                float  _CHANNELSELECTOR;
                float  _Surface, _Blend, _AlphaClip;
                float  _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha;
                float  _ZWrite, _ZWriteControl, _ZTest, _Cull, _AlphaToMask;
                float  _QueueOffset, _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex1); SAMPLER(sampler_NoiseTex1);
            TEXTURE2D(_NoiseTex2); SAMPLER(sampler_NoiseTex2);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 uv0        : TEXCOORD0;   // 原版 vs 的 v3
                float4 color      : COLOR;       // 原版 vs 的 v4
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain     : TEXCOORD0;   // ps 的 v1.xy
                float4 color      : TEXCOORD1;   // ps 的 v2
                float4 uvNoise    : TEXCOORD2;   // ps 的 v3（xy=层1 · zw=层2）
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);

                float  t    = _TimeParameters.x;                       // cb0[19].x
                float2 base = IN.uv0.xy + IN.uv0.z;                    // `v3.zzzz + v3.xyxy`
                OUT.uvNoise = float4(
                    base * _Noise1ScaleAndOffset.xy + frac(t * _Noise1ScaleAndOffset.zw),
                    base * _Noise2ScaleAndOffset.xy + frac(t * _Noise2ScaleAndOffset.zw));
                OUT.uvMain = IN.uv0.xy;                                // NoScaleOffset ⇒ 不做 TRANSFORM_TEX
                OUT.color  = IN.color;
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

            /// 四通道取一（`_NOISE*CHANNEL`：0=R 1=G 2=B 3=A，实测枚举）
            half PickChannel(half4 v, float sel)
            {
                int i = (int)sel;
                return (i == 0) ? v.x : (i == 1) ? v.y : (i == 2) ? v.z : v.w;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 main = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uvMain, _GlobalMipBias.x);
                half4 t1 = SAMPLE_TEXTURE2D_BIAS(_NoiseTex1, sampler_NoiseTex1, IN.uvNoise.xy, _GlobalMipBias.x);
                half4 t2 = SAMPLE_TEXTURE2D_BIAS(_NoiseTex2, sampler_NoiseTex2, IN.uvNoise.zw, _GlobalMipBias.x);

                // ---- 颜色：C = 编码后的 _Blend_Color；b = 主图×顶点色×**`_Color_Multiplier`**；
                //      最后按 **`_Blend_Color_Opacity`** 在 C 与 Overlay(C,b) 之间插值
                //      🔴 **2026-10-02 实测更正（铁律 5）**：两个标量的**归属原来写反了** ——
                //        第一轮 A/B 里两条环境效果**整片变黑**（`…Void Combat` 11704 → 8 亮像素）。
                //        用 `EffectCompare` + `WFCMP_MATDUMP=1` 拿到两侧材质真值 + 渲出对照图后才定案：
                //          原版渲出来的是**均匀的 `_Blend_Color` 色云**（Void Combat 青 / Asteroid Zone 橙，
                //          与材质里 `_Blend_Color` 逐个吻合）⇒ `o0 = C + w·(ov−C)` 里的 w **必须是 0**
                //          （这两个材质 `_Blend_Color_Opacity` 都 = 0、`_Color_Multiplier` 是 1/1.66）
                //          ⇒ **w = `_Blend_Color_Opacity`（cb1[7].x）**、
                //             **Overlay 的乘数 = `_Color_Multiplier`（cb1[5].z）**。
                //        语义上也自洽：`_Color_Multiplier` 乘基色、`_Blend_Color_Opacity` 是「混色透出多少」。
                //        判据（可复核）→ `d:/4/_tmp_view/cmp_Environmental…__{orig,exp}.png` +
                //        `d:/4/_tmp_view/cmp_alphamasks.log` 的 `MD` 行。
                half3 C = LinearToSrgb(_Blend_Color.rgb);
                half3 b = main.rgb * IN.color.rgb * _Color_Multiplier;
                half3 lo = 2.0h * C * b;                               // b ≤ 0.5 那一支
                half3 hi = 1.0h - 2.0h * (1.0h - C) * (1.0h - b);      // b > 0.5 那一支
                half3 ov = half3(b.r <= 0.5h ? lo.r : hi.r,
                                 b.g <= 0.5h ? lo.g : hi.g,
                                 b.b <= 0.5h ? lo.b : hi.b);
                half3 rgb = C + _Blend_Color_Opacity * (ov - C);

                // ---- alpha：K 出现两次（幂底里一次、最后再乘一次），指数 = `_AlphaMultiplier`
                half K = main.a * IN.color.a;
                half n = PickChannel(t1, _NOISE1CHANNEL) * PickChannel(t2, _NOISE2CHANNEL);
                half a = K * min(pow(n * K * _Alpha_Multiplier_2, _AlphaMultiplier), 1.0h);

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
