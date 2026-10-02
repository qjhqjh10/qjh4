// WFRockDissolve.shader — 自建，替代原版 `Shader Graphs/Fx_RockDissolve`
//
// 🔴 **来由**（2026-10-02，兜底路审计第 1 族的一半）：原来它**连映射表都没进**（导出期占位材质掉到
//    `URP/Unlit`），兜底路上 `BlastEffect` ×7.65 · `Invoke Minion Legendary ALT` ×4.48 ·
//    `Invoke Minion Hits Ground Legendary` ×4.39 · `Invoke Minion Hits Ground` ×3.08。
//    逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读**）—— 反汇编 `d:/4/_tmp_view/dxbc/rock_dissolve.txt`（段 0 = ForwardLit 基础变体，92 条指令）
// ---------------------------------------------------------------------------
// ① **它是个 URP Lit ShaderGraph**（SubShader tags: `ShaderGraphTargetId=UniversalLitSubTarget` /
//    `UniversalMaterialType=Lit` / `QUEUE=AlphaTest`），6 个 pass；本文件只做 **pass0（ForwardLit）**。
//    pass0 状态：`Blend One Zero` · `ZWrite On` · `ZTest LEqual` · `Cull Back` · **`AlphaToMask On`**（实读
//    `rtBlend0{src=1,dst=0,colMask=15}` + `alphaToMask=1`；与工程内 `OriginalShaderBlendTable.cs:72`
//    的 `{1,0,1}` 独立吻合）。材质 renderQueue=2450 ✓（SubShader QUEUE=AlphaTest）。
// ② **属性表只有 1 个真材质属性**：`Texture2D_EDA87E5`（Texture, flags=4 = NoScaleOffset, desc='MainTex',
//    默认 white）。**没有 `_Cutoff`/`_BaseColor`/`_SrcBlend`** —— 材质里那 23 个 float / 5 个 color
//    是**上一个 shader 的死值**（连 VS 的 cbuffer 都没有它们）。
// ③ **顶点流**（原版 vs 的 ISGN）：`v0=POSITION · v1=NORMAL · v2=TANGENT(unused) · v3=TEXCOORD0 ·
//    v4=TEXCOORD1 · v5=TEXCOORD2 · v6=COLOR0`；PS 侧 `v4.xy`=主 UV · `v5.x`=TEXCOORD1.x ·
//    `v6.xyzw`=TEXCOORD2 · `v7.xyzw`=COLOR0 · `v9.xyz`=世界位置 · `v10.xyz`=世界法线 · `v2.xyz`=主光阴影坐标。
//    ⚠️ 粒子上只有 Position/Normal/Color/UV/UV2 五条流（prefab `m_VertexStreams: 0001030405`）⇒
//      **TEXCOORD2（v6）大概率读成 0** ⇒ `lerp(v7.rgb, v6.rgb, tex.b*v6.a)` 退化成 **`v7.rgb`**（顶点色）。
//      本文件照原式实现（流在就用、不在就退化，与原件同构）。
// ④ **片元前段**（逐条，段 0 行 51-76）：
//      tex    = sample(`_MainTex`, v4.xy, bias=`_GlobalMipBias.x`)      ; NoScaleOffset ⇒ 不做 TRANSFORM_TEX
//      r1     = tex.r · lerp(v7.rgb, v6.rgb, tex.b · v6.a)              ; = **Emission**（见下）
//      alpha  = saturate( tex.a − saturate( saturate(v5.x) − tex.g ) )
//      thr    = v7.w                                                    ; = **顶点色 alpha 当裁剪阈值**
//      fw     = |ddx alpha| + |ddy alpha|
//      cov    = saturate( ((alpha−thr) − 0.5·fw) / max(fw, 1e-4) + 1 )
//      clipA  = (thr ≤ 0) ? 1 : cov
//      discard 当 (thr > 0 && alphaToMask可用) ? (clipA − 1e-4) < 0 : (alpha − thr) < 0
//      o0.w   = alphaToMask可用 ? clipA : 1.0
// ⑤ **光照**（段 0 行 77-159；这正是 URP 自己的光照，**不乘任何 albedo**）：
//      N = normalize(v10)   V = unity_OrthoParams.w==0 ? normalize(camPos−v9) : (view 矩阵的一列)
//      mainLight = `_MainLightColor`.rgb · saturate(N·L) · shadow · **unity_LightData.z**
//      spec      = 0.04 / max( 6.000120 · saturate(dot(L,H))², 0.1 )    ; H = normalize(V+L)
//      addLights = Σ_i [ 主光那套同构：atten_i · saturate(N·L_i) · color_i · spec_i ]
//      o0.rgb = **r1（Emission 那支）+ spec·mainLight + addLights**
//    ⚠️ **光照项不乘 albedo**（159 行是纯 `add`）⇒ 可见颜色几乎全来自 Emission 那支。
//      这条是 **== 推 ==**（DXBC 里看不到「乘 1」，编译器会把它消掉）——A/B 会验它。
//    🔴 阴影/附加光/级联/软阴影**一律走 URP 自己的 `Lighting.hlsl` API**（`GetMainLight`/`GetAdditionalLight`），
//      因为原件本来就是 URP 的 Lit 目标 ⇒ 用同一套函数才可能逐像素对上（`GetMainLight()` 里
//      `distanceAttenuation = unity_LightData.z` 正好就是 ④ 里那一条 `* cb2[11].z`）。
//
// ⚠️ **未复刻的**（判据不足）：pass1~pass5（DepthOnly / MotionVectors / 只写 R 那个 / DepthNormals /
//    ShadowCaster）—— 它们与「画面颜色」无关（`CLAUDE.md` 里同族的 `WFParticlesMultiply` 也这么处理）。
// ⚠️ **它在白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/RockDissolve"
{
    Properties
    {
        // 原版唯一的真材质属性（ShaderGraph 占位名，**必须逐字一致**，否则 binder 灌不进贴图）
        Texture2D_EDA87E5("MainTex", 2D) = "white" {}
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = -1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
        }

        // 原版 pass0 写死（材质上没有 _SrcBlend/_ZWrite）—— 不许间接寻址
        Blend One Zero
        ZWrite On
        ZTest LEqual
        Cull Back
        AlphaToMask On

        Pass
        {
            Name "RockDissolveForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 Texture2D_EDA87E5_ST;   // 原版 NoScaleOffset ⇒ 不参与
                float  _QueueOffset;
                float  _QueueControl;
            CBUFFER_END

            TEXTURE2D(Texture2D_EDA87E5);
            SAMPLER(samplerTexture2D_EDA87E5);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 uv0        : TEXCOORD0;   // vs 的 v3 → ps 的 v4（主 UV）
                float4 uv1        : TEXCOORD1;   // vs 的 v4 → ps 的 v5
                float4 uv2        : TEXCOORD2;   // vs 的 v5 → ps 的 v6（粒子上多半没有这条流 ⇒ 0）
                float4 color      : COLOR;       // vs 的 v6 → ps 的 v7
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float4 uv0         : TEXCOORD0;   // ps 的 v4
                float4 uv1         : TEXCOORD1;   // ps 的 v5
                float4 uv2         : TEXCOORD2;   // ps 的 v6
                float4 color       : TEXCOORD3;   // ps 的 v7
                float3 positionWS  : TEXCOORD4;   // ps 的 v9
                float3 normalWS    : TEXCOORD5;   // ps 的 v10
                float4 shadowCoord : TEXCOORD6;   // ps 的 v2
                float  fogFactor   : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = TransformWorldToShadowCoord(posWS);
                OUT.uv0 = IN.uv0;
                OUT.uv1 = IN.uv1;
                OUT.uv2 = IN.uv2;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ── 前段：Emission + alpha（逐条照抄段 0 行 51-76）
                half4 tex = SAMPLE_TEXTURE2D_BIAS(Texture2D_EDA87E5, samplerTexture2D_EDA87E5,
                                                  IN.uv0.xy, _GlobalMipBias.x);
                half3 emission = tex.r * lerp(IN.color.rgb, IN.uv2.rgb, tex.b * IN.uv2.a);

                half  alpha = saturate(tex.a - saturate(saturate(IN.uv1.x) - tex.g));
                half  thr   = IN.color.a;
                half  fw    = abs(ddx(alpha)) + abs(ddy(alpha));
                half  cov   = saturate(((alpha - thr) - 0.5h * fw) / max(fw, 0.0001h) + 1.0h);
                half  clipA = (thr <= 0.0h) ? 1.0h : cov;
                bool  a2m   = _AlphaToMaskAvailable != 0.0;
                clip((a2m && thr > 0.0h) ? (clipA - 0.0001h) : (alpha - thr));

                // ── 光照（URP 自己的 API；原件就是 URP Lit 目标）
                half4 shadowMask = half4(1, 1, 1, 1);
                float3 N = normalize(IN.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                Light mainLight = GetMainLight(IN.shadowCoord, IN.positionWS, shadowMask);
                half3 L  = mainLight.direction;
                half3 H  = normalize(V + L);
                half  sp = saturate(dot(L, H));
                half  spec = 0.04h / max(6.000120h * sp * sp, 0.1h);
                half3 lighting = spec * mainLight.color
                               * saturate(dot(N, L))
                               * (mainLight.shadowAttenuation * mainLight.distanceAttenuation);

            #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light l = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                    half3 Li = l.direction;
                    half3 Hi = normalize(V + Li);
                    half  si = saturate(dot(Li, Hi));
                    half  specI = 0.04h / max(6.000120h * si * si, 0.1h);
                    lighting += specI * l.color * saturate(dot(N, Li))
                              * (l.distanceAttenuation * l.shadowAttenuation);
                LIGHT_LOOP_END
            #endif

                half3 col = emission + lighting;
                col = MixFog(col, IN.fogFactor);
                return half4(col, a2m ? clipA : 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
