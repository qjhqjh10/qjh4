// WFMultiRay.shader — 自建，替代原版 `Everguild/FX/Multi Ray`
//
// 🔴 **来由**（2026-10-02，兜底路审计第 5 族）：原来映射到通用的 `WarpforgeVFX/Particles/Extra Color`（只乘）
//    ⇒ 兜底路上 `ScytheAssault` 比值 **0.247（我们偏暗，|ln| 1.27）**、`Sau_MethodicalDestruction` ×0.76、
//    `Teleport Trait Summon` ×0.84。逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读**）—— `_tmp_view/dxbc/multi_ray.txt`（ps）与 `..._vs.txt`（vs）
// ---------------------------------------------------------------------------
// ① **渲染状态**：pass0 的 `rtBlend0` 全是**材质属性间接引用**（`_SrcBlend`/`_DstBlend`）⇒
//    38 个材质实测 `_SrcBlend=5/_DstBlend=10` ⇒ `Blend SrcAlpha OneMinusSrcAlpha`，
//    **但仍按属性寻址**（同 `WFAlphaMasksTwoLayer`：这一件的原版材质**有**真值）。
//    `zWrite=_ZWrite`(0) · `zTest=_ZTest`(4) · `cull=_Cull`(0=Off) · `alphaToMask`(0)。Queue=Transparent(3000)。
//    ⚠️ 原版 5 个 pass（1=DepthOnly · 2=MotionVectors · 3=DepthNormals · 4=ShadowCaster，都是推的），本文件只做 pass0。
// ② **属性名逐字照抄**（`dump_shader.py` 的 32 个）：`_MainTex`(NoScaleOffset) ·
//    `_Scale_1..4_XY_Speed_ZW` · `_Color1..4`(HDR) · `_Remap_Color_Min_XY_Max_ZW` · `_General_Speed_Modifier` ·
//    `_USEBORDERFADE` · `_Border_Fade_XminXmax_YminYmax` · `_BorderFadeMultiplier` + 一整套 `_SrcBlend` 状态属性。
// ③ **顶点**（vs 段 0）：`clip = unity_MatrixVP·(unity_ObjectToWorld·v0)` · `o2 = COLOR`（→ ps 的 v2）·
//    `o1 = TEXCOORD0`（→ ps 的 v1，只有 border-fade 那支用）；四层 UV 全在 VS 算（`t = cb0[19].x = _TimeParameters.x`）：
//        centered = uv0.xy − 0.5
//        layer_i  = frac( _Scale_i.zw · _General_Speed_Modifier · t ) + ( _Scale_i.xy · centered + 0.5 )
//     🔴 **2026-10-19 订正（原来这行写「o3 = (layer4, layer1) · o4 = (layer2, layer3)」—— 那是错的）**：
//        VS 的 `UnityPerMaterial` 走**序列化反射**实读（`SerializedPass.progVertex.m_CommonParameters`）：
//          cb2[0]=`_Scale_2` · cb2[1]=`_Scale_3` · cb2[2]=`_Scale_4` · cb2[3]=`_Scale_1` · cb2[10].x=`_General_Speed_Modifier`
//        对上 VS 指令：`o3.zw ← cb2[0]`（layer2）· `o3.xy ← cb2[3]`（layer1）· `o4.zw ← cb2[2]`（layer4）· `o4.xy ← cb2[1]`（layer3）
//        ⇒ **o3 = (layer1, layer2) · o4 = (layer3, layer4)**。
//        PS 侧同一把钥匙实读：cb1[8]=`_Color1` · cb1[5]=`_Color2` · cb1[6]=`_Color3` · cb1[7]=`_Color4`；
//        对 PS 指令：`cb1[8]·tex(v3.xy).r` · `cb1[5]·tex(v3.zw).g` · `cb1[6]·tex(v4.xy).z` · `cb1[7]·tex(v4.zw).w`
//        ⇒ **layer1 取 .r / layer2 取 .g / layer3 取 .b / layer4 取 .a**，且**颜色与层一一对应**。
//        ⚠️ 本文件旧版把通道按 (layer4→.r, layer1→.g, layer2→.b, layer3→.a) 轮转了一遍 —— 层与色**配对是对的**，
//        但**通道取错了**（同一张图上会取到另一层的通道）⇒ 已按上面实读改正。
// ④ **片元**（ps 段 1，60 条，逐条翻译）—— **同一张 `_MainTex` 采 4 次、各取一个通道、配 4 个 HDR 色相加**：
//        acc.rgb = sRGB(_Color1.rgb)·tex(v3.xy).r + sRGB(_Color2.rgb)·tex(v3.zw).g
//                + sRGB(_Color3.rgb)·tex(v4.xy).b + sRGB(_Color4.rgb)·tex(v4.zw).a
//        acc.a   = tex(v3.xy).r·_Color1.a + tex(v3.zw).g·_Color2.a
//                + tex(v4.xy).b·_Color3.a + tex(v4.zw).a·_Color4.a
//        （⚠️ 2026-10-19 订正：上面「层/通道/色」的配对按序列化反射实读改过，判据见 ③；
//          旧版把通道轮转成 layer4→.r / layer1→.g / layer2→.b / layer3→.a —— **错**。）
//        M       = `_Remap_Color_Min_XY_Max_ZW`  ; .x=inMin · .y=inMax · .z=outMin · .w=outMax
//        o0.rgb  = ( acc.rgb·v2.rgb − M.x ) · (M.w − M.z) / (M.y − M.x) + M.z
//        o0.a    = saturate(acc.a) · v2.a                     ; ← `_SURFACE_TYPE_TRANSPARENT` 那支
//      ⚠️ 4 个色的 **`.rgb` 都过一遍 `LinearToSRGB` 编码**，**`.a` 不过**（常量逐字来自 DXBC）。
//      ⚠️ 段 0（53 条）与段 1 只差 `o0.w`（段 0 = 1.0）；**37/38 个材质都带 `_SURFACE_TYPE_TRANSPARENT`**
//        ⇒ 实机走的是段 1（就是本文件这条），段 0 那支照 `#ifndef` 实现。
// ⑤ ✅ **`_USEBORDERFADE` 那一支 —— 2026-10-19 已实现**（判据 = PS 段 2，74 条，逐条读；见 `frag` 里那段注释）。
//    ⚠️ 它**是关键字**（关键字全集里有 `_USEBORDERFADE`，**没有 `_ON` 后缀** —— 与 Everguild 的 `_SOFTPARTICLES` 同族）。
//    ⚠️ **槽位是推的（但推法可复核）**：段 2 用 `cb1[11]` 当 (xmin,xmax,ymin,ymax)、`cb1[12].x` 当 multiplier，
//       而 PS 反射只列出被这一变体用到的 5 个槽（cb1[5..9]）。推法 = **属性声明序 + Unity 的标量打包规则**
//       （标量塞进当前 vec4 的剩余分量，塞不下才换槽）—— 这条规则用 `TrailShader_1` 的反射独立验过：
//       `_MainTextureSpeed`(Vector)=slot3 .x,.y、紧随其后的 `_DissolveScale`(Float)=slot3 **.z**。
//       ⇒ `_General_Speed_Modifier` 占 cb1[10].x（实读）⇒ `_USEBORDERFADE`(Float) 进 cb1[10].y
//       ⇒ `_Border_Fade_…`(Vector4) 必须换槽 = **cb1[11]** ⇒ `_BorderFadeMultiplier`(Float) = **cb1[12].x**，
//       与段 2 的用法**形状完全吻合**（11 是四分量、12 只用 .x）。
//    ⚠️ 补这条之前实测只有 `Teleport Trait Summon` 一个材质带该关键字；⛔ 别把它当成「所有材质的默认路径」。
//
// ⚠️ **它在白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/MultiRay"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Scale_1_XY_Speed_ZW("Scale 1 (XY) Speed (ZW)", Vector) = (1, 1, 0, 0)
        _Scale_2_XY_Speed_ZW("Scale 2 (XY) Speed (ZW)", Vector) = (1, 1, 0, 0)
        _Scale_3_XY_Speed_ZW("Scale 3 (XY) Speed (ZW)", Vector) = (1, 1, 0, 0)
        _Scale_4_XY_Speed_ZW("Scale 4 (XY) Speed (ZW)", Vector) = (1, 1, 0, 0)
        [HDR] _Color1("Color1", Color) = (1, 1, 1, 1)
        [HDR] _Color2("Color2", Color) = (1, 1, 1, 1)
        [HDR] _Color3("Color3", Color) = (1, 1, 1, 1)
        [HDR] _Color4("Color4", Color) = (1, 1, 1, 1)
        _Remap_Color_Min_XY_Max_ZW("Remap Color Min(XY) Max (ZW)", Vector) = (0, 10, 0, 100)
        _General_Speed_Modifier("General Speed Modifier", Float) = 1
        _USEBORDERFADE("UseBorderFade", Float) = 0
        _Border_Fade_XminXmax_YminYmax("Border Fade XminXmax YminYmax", Vector) = (0, 1, 0.45, 1.1)
        _BorderFadeMultiplier("BorderFadeMultiplier", Float) = 1

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

        // 原版这几项都是**材质属性间接引用**（38 个材质实测 5/10）—— 不许改成字面量
        // 🔴 2026-10-21（T17 · Ruri 反编译对拍）：原版 pass0 = `Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]`
        //    （色与 alpha 分开寻址）；我们原来只写了前半截。判据 = `Everguild/FX/Multi Ray` 反编译体。
        Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]

        Pass
        {
            Name "MultiRay"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // 段 0（o0.w=1）vs 段 1（o0.w=累积 alpha）由它分叉 —— 不声明 pragma 的 `#if` 恒为假（2026-10-02 踩过）
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            // 🆕 2026-10-19：段 2（border fade，74 条）—— 名字逐字来自原版关键字全集，**没有 `_ON` 后缀**。
            #pragma shader_feature_local_fragment _USEBORDERFADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;                     // 原版 NoScaleOffset ⇒ 不参与
                float4 _Scale_1_XY_Speed_ZW;
                float4 _Scale_2_XY_Speed_ZW;
                float4 _Scale_3_XY_Speed_ZW;
                float4 _Scale_4_XY_Speed_ZW;
                float4 _Color1, _Color2, _Color3, _Color4;
                float4 _Remap_Color_Min_XY_Max_ZW;
                float  _General_Speed_Modifier;
                float  _USEBORDERFADE;
                float4 _Border_Fade_XminXmax_YminYmax;
                float  _BorderFadeMultiplier;
                float  _Surface, _Blend, _AlphaClip;
                float  _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha;
                float  _ZWrite, _ZWriteControl, _ZTest, _Cull, _AlphaToMask;
                float  _QueueOffset, _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 uv0        : TEXCOORD0;   // vs 的 v3
                float4 color      : COLOR;       // vs 的 v4
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uvA        : TEXCOORD0;   // ps 的 v3 = (layer1, layer2)
                float4 uvB        : TEXCOORD1;   // ps 的 v4 = (layer3, layer4)
                float4 color      : TEXCOORD2;   // ps 的 v2
                float2 uvBase     : TEXCOORD3;   // ps 的 v1.xy（只有 border-fade 那支读）
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// 一层 UV：`frac(speed·mod·t) + (scale·(uv−0.5) + 0.5)`
            float2 LayerUV(float4 scaleSpeed, float2 centered, float t)
            {
                return frac(scaleSpeed.zw * _General_Speed_Modifier * t) + (scaleSpeed.xy * centered + 0.5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                float  t        = _TimeParameters.x;              // cb0[19].x
                float2 centered = IN.uv0.xy - 0.5;
                OUT.uvA = float4(LayerUV(_Scale_1_XY_Speed_ZW, centered, t),
                                 LayerUV(_Scale_2_XY_Speed_ZW, centered, t));
                OUT.uvB = float4(LayerUV(_Scale_3_XY_Speed_ZW, centered, t),
                                 LayerUV(_Scale_4_XY_Speed_ZW, centered, t));
                OUT.uvBase = IN.uv0.xy;
                OUT.color = IN.color;
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

            half4 frag(Varyings IN) : SV_Target
            {
                // 层 ↔ 通道 ↔ 颜色（2026-10-19 按序列化反射实读，判据见文件头 ③）：
                //   layer1 → uvA.xy · .r · _Color1 · layer2 → uvA.zw · .g · _Color2
                //   layer3 → uvB.xy · .b · _Color3 · layer4 → uvB.zw · .a · _Color4
                half4 tex1 = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uvA.xy, _GlobalMipBias.x);
                half4 tex2 = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uvA.zw, _GlobalMipBias.x);
                half4 tex3 = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uvB.xy, _GlobalMipBias.x);
                half4 tex4 = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uvB.zw, _GlobalMipBias.x);

                half3 acc = LinearToSrgb(_Color1.rgb) * tex1.r
                          + LinearToSrgb(_Color2.rgb) * tex2.g
                          + LinearToSrgb(_Color3.rgb) * tex3.b
                          + LinearToSrgb(_Color4.rgb) * tex4.a;
                half  accA = tex1.r * _Color1.a
                           + tex2.g * _Color2.a
                           + tex3.b * _Color3.a
                           + tex4.a * _Color4.a;

                float4 M = _Remap_Color_Min_XY_Max_ZW;   // x=inMin y=inMax z=outMin w=outMax
                half3 rgb = (acc * IN.color.rgb - M.x) * (M.w - M.z) / (M.y - M.x) + M.z;

                half a;
            #if defined(_USEBORDERFADE)
                // ══ PS 段 2（74 条，逐条翻译）—— 只有 `Teleport Trait Summon` 一个材质用 ══
                //   r1.xy = 1 / (max − min)（cb1[11].y−x , .w−z）
                //   t     = saturate((v1.xy − min) / (max − min))
                //   s     = t²·(3−2t)                     （smoothstep）
                //   fade  = saturate( (4·sx·(1−sx)) · (4·sy·(1−sy)) · _BorderFadeMultiplier )
                float2 mn   = _Border_Fade_XminXmax_YminYmax.xz;
                float2 mx   = _Border_Fade_XminXmax_YminYmax.yw;
                float2 t    = saturate((IN.uvBase - mn) / (mx - mn));
                float2 s    = t * t * (3.0 - 2.0 * t);
                half   fade = saturate((4.0 * s.x * (1.0 - s.x)) * (4.0 * s.y * (1.0 - s.y))
                                       * _BorderFadeMultiplier);
                a = saturate(accA) * IN.color.a * fade;
            #elif defined(_SURFACE_TYPE_TRANSPARENT)
                a = saturate(accA) * IN.color.a;      // 段 1
            #else
                a = 1.0h;                             // 段 0
            #endif
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
