// WFUIGlowSdfNoise.shader —— 原版 `Everguild/UI/UI Glow SDF Noise` 的**逐指令复刻**
//
// ============================ 判据（本机实读） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_UI Glow SDF Noise.shader
// vertex 真源码  : …/Everguild_UI_UI Glow SDF Noise/Sub0_Pass0_Vertex_b1.hlsl:109-147
// fragment 真源码: …/Everguild_UI_UI Glow SDF Noise/Sub0_Pass0_Fragment_b15.hlsl:52-95
// 5 份材质      : assets_full/bundle_menus_assets_all/Material/
//                 Material_-1001325612927388121.json（`Border Chest Collect`）
//                 Material_4751468191648642739.json（`Border Chest`）
//                 Material_1755421458546770695.json（`UI Highlight Red SDF`）
//                 Material_6690636151482980369.json（`UI Highlight Smooth Red SDF`）
//                 Material_-6653266153491287024.json（`UI Highlight Alliance trophy`）
//
// 时间口径：原版 vertex 读 `type_Globals._TimeParameters.x` = **秒**（URP `ScriptableRenderer.cs:504`）
//   ⇒ 本 shader 用 `_Time.y`（`_Time.x` 是 t/20，⛔ 别用）。
//
// 🔴 **`_NOISE_CHANNEL` 的映射（原版是 4 个 shader 变体，我们合成一条分支）**：
//   原版 `[KeywordEnum(R,G,B,A)] _NOISE_CHANNEL`；材质里 `m_ValidKeywords` 现读 =
//   `Border Chest*` → `_NOISE_CHANNEL_B`（值 2）· `UI Highlight Red SDF` → `_NOISE_CHANNEL_A`（值 3）·
//   `Smooth Red SDF` / `Alliance trophy` → `_NOISE_CHANNEL_R`（值 0）。
//   ⇒ 0=R(.x) / 1=G(.y) / 2=B(.z) / 3=A(.w)。行为等价（同一个采样、同一段算式），只省掉 4 份变体裁剪。
//   ⚠️ **主纹理（精灵）那一路恒 `.x`** —— 原版 fragment 两句 `_MainTex.SampleBias(...).x` 写死 `.x`
//   （`_ALPHACHANNEL = 0` ⇒ `_ALPHACHANNEL_R`；5 份材质全是 0）。
//
// 用法：材质挂在一张 quad 上即可（噪声图 `_Noise` 由材质给；原版用的是同一张 `-8214795764591836323`）。
Shader "WarpforgeVFX/UI/UI Glow SDF Noise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _BackgroundColor ("BackgroundColor", Color) = (0.9811321, 0, 0, 1)
        _SpriteAlphaEdge_Inner ("SpriteAlphaEdge Inner", Range(0, 1)) = 0.5
        _Inner_Edge ("Inner Edge", Range(0, 1)) = 1
        _Inner_Fallof ("Inner Fallof", Range(0, 1)) = 1
        _Noise ("Noise", 2D) = "white" {}
        _Noise_Speed_Scale_StepLow_StepHigh ("Noise (Speed/Scale/StepLow/StepHigh)", Vector) = (1, 0.8, 0, 0.37)
        _NoiseMaskScale ("NoiseMaskScale", Float) = 1
        _NoiseIntensity ("NoiseIntensity", Float) = 1
        _NOISE_CHANNEL ("Noise Channel (0=R,1=G,2=B,3=A)", Float) = 0
        _SrcBlend ("Src Blend", Float) = 5
        _DstBlend ("Dst Blend", Float) = 1
        _ColorMask ("Color Mask", Float) = 15
        _Stencil ("Stencil ID", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
            "QUEUE"="Transparent"
            "RenderType"="Transparent"
            "IGNOREPROJECTOR"="true"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="true"
        }
        Pass
        {
            Name "Default"
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            Stencil
            {
                Ref [_Stencil]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilOp]
                Fail Keep
                ZFail Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Noise;
            float4 _MainTex_ST;
            fixed4 _BackgroundColor;
            float  _SpriteAlphaEdge_Inner;
            float  _Inner_Edge;
            float  _Inner_Fallof;
            float4 _Noise_Speed_Scale_StepLow_StepHigh;
            float  _NoiseMaskScale;
            float  _NoiseIntensity;
            float  _NOISE_CHANNEL;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos    : SV_POSITION;
                float4 color  : COLOR;
                float2 uvRaw  : TEXCOORD0;   // 原版 `INTERP`（裸 uv，SDF 那一路用）
                float4 nAB    : TEXCOORD1;   // xy = 噪声B（逆时针那一路）· zw = 噪声A（顺时针那一路）
                float2 uvMask : TEXCOORD2;   // 原版 `INTERP_4.w / INTERP_5.w`（乘过 `_NoiseMaskScale` 的 uv）
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;

                float t = _Time.y;                                    // 秒（见文件头「时间口径」）

                // `_222 = t * Speed`; `_223 = frac(_222)`; `_224 = _223*2π`; `_226 = -_223*2π`（Vertex_b1.hlsl:109-114）
                float f   = frac(t * _Noise_Speed_Scale_StepLow_StepHigh.x);
                float ang = f * 6.2831850051879882813;
                float sn  = sin(ang);
                float cs  = cos(ang);

                float2 d = float2(v.uv.x * _MainTex_ST.x + _MainTex_ST.z,
                                  v.uv.y * _MainTex_ST.y + _MainTex_ST.w) - 0.5;
                float scale = _Noise_Speed_Scale_StepLow_StepHigh.y;

                // 噪声 A：`_241 = d·(cos(-a), sin(-a)) + .5` · `_242 = d·(-sin(-a), cos(-a)) + .5` ⇒ 转 +a（:120-124）
                // 噪声 B：`_264 = d·(cos(a), sin(a)) + .5`  · `_265 = d·(-sin(a), cos(a)) + .5`  ⇒ 转 -a（:128-131）
                float2 nA = float2(d.x * cs - d.y * sn, d.x * sn + d.y * cs) + 0.5;
                float2 nB = float2(d.x * cs + d.y * sn, -d.x * sn + d.y * cs) + 0.5;
                o.nAB = float4(nB * scale, nA * scale);

                // `INTERP_4.w = mad(_NoiseMaskScale, uv.x-0.5, 0.5)` · `INTERP_5.w` 同（:146-147）
                o.uvRaw  = v.uv;
                o.uvMask = (v.uv - 0.5) * _NoiseMaskScale + 0.5;
                return o;
            }

            // 原版 `smoothstep(x) = x*x*(3-2x)`（`mad(mad(x,-2,3), x², …)` 三处同形）
            float Smooth01(float x) { return x * x * (3.0 - 2.0 * x); }

            // linear → gamma（Fragment_b15.hlsl:79-87）
            float3 LinToGamma(float3 c)
            {
                float3 lo = c * 12.92321014404296875;
                float3 hi = exp2(log2(abs(c)) * 0.4166666567325592041) * 1.0549999475479125977 - 0.05499999970197677612;
                // 原版逐分量取 `(c <= 0.0031308) ? lo : hi`；用 `step` 表达同一个逐分量选择
                return lerp(lo, hi, step(0.0031308000907301903, c));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 mts = tex2D(_MainTex, i.uvMask);
                float  m   = mts.x;                                    // 原版 `_60`（恒 `.x`）

                float4 nA = tex2D(_Noise, i.nAB.zw);
                float4 nB = tex2D(_Noise, i.nAB.xy);
                float  cA = (_NOISE_CHANNEL < 0.5) ? nA.x : ((_NOISE_CHANNEL < 1.5) ? nA.y : ((_NOISE_CHANNEL < 2.5) ? nA.z : nA.w));
                float  cB = (_NOISE_CHANNEL < 0.5) ? nB.x : ((_NOISE_CHANNEL < 1.5) ? nB.y : ((_NOISE_CHANNEL < 2.5) ? nB.z : nB.w));

                float lo = _Noise_Speed_Scale_StepLow_StepHigh.z;
                float hi = _Noise_Speed_Scale_StepLow_StepHigh.w;
                float inv = 1.0 / (hi - lo);

                float e0 = saturate(inv * (m * cA - lo));               // `_94`
                float e1 = saturate(inv * (m * cB - lo));               // `_116`
                float nInf = (Smooth01(e1) + Smooth01(e0)) * _NoiseIntensity;   // `_123`

                // ---- SDF 环（Fragment_b15.hlsl:68-78）----
                float eIn   = _SpriteAlphaEdge_Inner;                   // `_129` 的输入项
                float a1    = eIn * (1.0 - _Inner_Edge) + _Inner_Edge;  // `_129`
                float d1    = eIn - a1;                                 // `_130`
                float b1    = _Inner_Fallof * d1 + a1;                  // `_133`
                float inv2  = 1.0 / (a1 - b1);                          // `_135`
                float ss    = saturate(inv2 * (tex2D(_MainTex, i.uvRaw).x - b1));   // `_150`

                float v = (Smooth01(ss) + nInf) * i.color.a;            // `_156`

                float3 bg = LinToGamma(_BackgroundColor.rgb);           // `_191..193`
                return fixed4(lerp(bg, i.color.rgb, v), 1.0);           // `_207..` + `SV_Target.w = 1`
            }
            ENDCG
        }
    }
    Fallback Off
}
