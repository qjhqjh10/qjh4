// WFEclipseTau.shader — 自建，**逐条照抄**原版 `Shader Graphs/Eclipse Tau`
//
// 🔴 来由（2026-10-22 · W64 兜底审计 **B11**）：这条原来在 `WarpforgeShaderMap.Replacements` 里
//    指到通用的 `WarpforgeVFX/Particles/Extra Color`（注释自承「近似」）。原版编译字节码已由
//    Ruri 反编译（`资料/普查产出_1020/W58_工具线_Ruri实测.md`）⇒ 按字节码逐条重写。
//    判据 → `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/908a5b774fff04cbbb713496c16325fc.shader`
//    ＋ 同 hash 目录的 `Sub0_Pass0_{Vertex_b1,Fragment_b7,Fragment_b8}.hlsl`（b7 = catch-all · b8 = `_APPLYAMBIENTCOLOR`）。
//
// 判据（全部实读）
// ----------------
// ① 属性（逐字）：`_MainTex`("white") · `_MainColor`(def **(1,0.9258057,0.3066038,0)**)
//      `_EclipseColor`(def (1,0,0,0)) · `_EclipseColorBorder`(HDR, def **(1,0,0.8089566,0)**)
//      `_EclipseRange`(Range(−1,0), def **−0.55**) · `_Scale`(Vector, def (1,1,0,0))
//      `_APPLYAMBIENTCOLOR`(Toggle, def **1**) · `_ExtraAmbientColor`(HDR, def (1,1,1,0))
//      `_FogContribution`(Range(0,1), def 1)
// ② 渲染状态（反编译 pass 逐字）：`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`
//      · `ZWrite Off` · `Cull Off`（混合硬编码，材质上没有 `_SrcBlend`）
// ③ 顶点（`Vertex_b1` / `_b2`）：
//      INTERP   = TEXCOORD0（uv 原样）
//      INTERP_1 = COLOR0 * `_RendererColor` * `unity_SpriteColor`     ; 片元把它当「整体 tint」
//      INTERP_2 = `unity_FogColor`，**w 恒 0** ⇒ 片元那一步 `lerp(col, fog, w)` 在**原版里就是恒等**
//      ⚠️ `unity_SpriteColor` 只在 **SpriteRenderer** 上被 Unity 写入；本 shader 的三件使用方
//        （`EnvironmentalCondition Tau Electro-Static Interference` / `Radiation Storm` / `Solar Eclipse`）
//        的材质都挂在 **ParticleSystemRenderer** 上（实测这三个 prefab 里 `m_SpriteRenderer: 0`）
//        ⇒ 这一项取 1。这是**按渲染器类型取的实况**，不是忽略。
// ④ 片元（`Fragment_b7` 逐条）：
//      t  = SAMPLE(_MainTex, uv) ; t.a == 0 ⇒ discard
//      b  = t.rgb * _MainColor.rgb
//      eb = LinearToSRGB(_EclipseColorBorder.rgb)
//      uv2= (uv − 0.5) * _Scale.xy + 0.5 ; **uv2.x 再加 `_EclipseRange`**（`_139 = mad(_Scale.x, uv.x−0.5, _EclipseRange) + 0.5`）
//      t2 = SAMPLE(_MainTex, uv2)
//      a  = t.a * t2.a
//      col= b + a*(eb − b) + a²*(_EclipseColor.rgb − eb)      ; 即 `_173 = a*mad(a, _EclipseColor−eb, eb) + b` 展开
//      col = lerp(col, fog, 0) * INTERP_1.rgb  = col * tint
//      o.a= t.a * tint.a
// ⑤ 环境光那一支（`Fragment_b8`）：
//      amb = lerp(1, LinearToSRGB(_ExtraAmbientColor.rgb) * `unity_AmbientSky`.rgb, `_AmbientColorBlend`)
//      col *= amb
//      （`_AmbientColorBlend` 是**我们已经在灌的全局**：`ArenaSceneState` / `EnvironmentApplier`
//        每场 `Shader.SetGlobalFloat` —— 13 场现读全是 0 ⇒ 恒等；这里接上它才和原版同源。）
//
// ⚠️ 它在 `UseOriginal` 白名单里 ⇒ 正常态走原件；本文件只在兜底路生效。
Shader "WarpforgeVFX/FX/EclipseTau"
{
    Properties
    {
        [NoScaleOffset] _MainTex("MainTex", 2D) = "white" {}
        _MainColor("MainColor", Color) = (1, 0.9258057, 0.3066038, 0)
        _EclipseColor("EclipseColor", Color) = (1, 0, 0, 0)
        [HDR] _EclipseColorBorder("EclipseColorBorder", Color) = (1, 0, 0.8089566, 0)
        _EclipseRange("EclipseRange", Range(-1, 0)) = -0.55
        _Scale("Scale", Vector) = (1, 1, 0, 0)
        [Toggle(_APPLYAMBIENTCOLOR)] _APPLYAMBIENTCOLOR("ApplyAmbientColor", Float) = 1
        [HDR] _ExtraAmbientColor("ExtraAmbientColor", Color) = (1, 1, 1, 0)
        _FogContribution("FogContribution", Range(0, 1)) = 1
        [HideInInspector] _RendererColor("RendererColor", Color) = (1, 1, 1, 1)
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

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        // 原版有「Sprite Unlit（Universal2D）」与「Sprite Unlit（UniversalForward）」两个同名 pass。
        // 本工程的对战是 URP 前向渲染 ⇒ 这里只保留一个不带 LightMode 的 pass（= `SRPDefaultUnlit`，
        // 前向那条路会画），⛔ 不为了「两个 pass 看起来更像原版」而复制一份 —— 复制会画两遍。
        Pass
        {
            Name "Sprite Unlit"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _APPLYAMBIENTCOLOR

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // `unity_AmbientSky`（环境光那一支要用）来自 UnityInput.hlsl，Core.hlsl 已经包含它。

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _EclipseColor;
                float4 _EclipseColorBorder;
                float  _EclipseRange;
                float4 _Scale;
                // ⚠️ **这里【不能】声明 `float _APPLYAMBIENTCOLOR;`** —— 它是 `[Toggle(_APPLYAMBIENTCOLOR)]`
                //    的关键字名，又出现在 `#pragma multi_compile_local _ _APPLYAMBIENTCOLOR` 里；材质一开
                //    Unity 就 `#define _APPLYAMBIENTCOLOR 1` ⇒ 与同名变量**重定义**、整份编不过。
                //    实测：`Shader error in 'WarpforgeVFX/FX/EclipseTau': redefinition of '_APPLYAMBIENTCOLOR'`
                //    （`_tmp_view/20261024_playerbuild.log:5661`）。
                //    本文件只走 `#ifdef`、不读它的值 ⇒ 按 `WFUnlitUVScroll.shader:120` 的同族做法
                //    **只留 Properties 里那一条**（材质默认值 1 ⇒ 原版材质的关键字本来就开着，行为不变）。
                float4 _ExtraAmbientColor;
                float  _FogContribution;
                float4 _RendererColor;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // 全局（`Shader.SetGlobalFloat`，见文件头 ⑤）—— 不放进 UnityPerMaterial。
            float _AmbientColorBlend;

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
                float4 uv         : TEXCOORD0;
                float4 tint       : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv0;                                  // 不乘 `_ST`（原版 `mov o1, v3`）
                OUT.tint = IN.color * _RendererColor;             // 见文件头 ③：`unity_SpriteColor` 对本组渲染器 = 1
                return OUT;
            }

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
                half4 t = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, IN.uv.xy, _GlobalMipBias.x);
                if (t.a == 0.0h) discard;

                half3 b  = t.rgb * _MainColor.rgb;
                half3 eb = LinearToSrgb(_EclipseColorBorder.rgb);

                // uv2 = (uv − 0.5) * _Scale.xy + 0.5，**x 再加 `_EclipseRange`**
                float2 uv2 = float2((IN.uv.x - 0.5h) * _Scale.x + _EclipseRange + 0.5h,
                                    (IN.uv.y - 0.5h) * _Scale.y + 0.5h);
                half4 t2 = SAMPLE_TEXTURE2D_BIAS(_MainTex, sampler_MainTex, uv2, _GlobalMipBias.x);

                half a = t.a * t2.a;
                half3 col = b + a * (eb - b) + a * a * (_EclipseColor.rgb - eb);

            #ifdef _APPLYAMBIENTCOLOR
                // amb = lerp(1, srgb(_ExtraAmbientColor) * unity_AmbientSky, _AmbientColorBlend)
                half3 amb = lerp(half3(1, 1, 1),
                                 LinearToSrgb(_ExtraAmbientColor.rgb) * unity_AmbientSky.rgb,
                                 _AmbientColorBlend);
                col *= amb;
            #endif

                col *= IN.tint.rgb;
                half outA = t.a * IN.tint.a;
                return half4(col, outA);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
