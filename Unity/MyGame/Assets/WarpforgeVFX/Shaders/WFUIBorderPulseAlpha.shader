// WFUIBorderPulseAlpha.shader —— 原版 `Everguild/UI/UI Border Highlight Pulse Alpha` 的**照源重建**
//
// ============================ 判据（本机实读，逐指令） ============================
// 原版 shader 的**编译产物**不在我们的随包 `wf_shaders.bundle` 里（`判据/B112W_B08_接线图.md` §0·3 实读），
// 但 **Ruri 已把它的 SPIR-V 反编译成可读 HLSL**：
//   `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/
//    Everguild_UI_UI Border Highlight Pulse Alpha/Sub0_Pass0_Vertex_b1.hlsl`（脉冲相位，:283-323）
//   同目录 `Sub0_Pass0_Fragment_b7.hlsl`（屏幕 UV 噪声旋转 + `_MainTex` 采样 + HDR 线性→sRGB，:76-273）
// 本文件是**照着那份 HLSL 逐句搬的**（不是观感近似）：
//   · 顶点：`pulse = clamp(1 - (tt-(toOn+on))/toOff) * clamp(tt/toOn)`，`tt = total*frac(t/total)`，
//     `t = _Time.y * _AnimationSpeed`，`total = toOn+on+toOff+off`（原版 `_TimesParameters=(0.5,1,0.5,0)` ⇒ 2 s 一圈）
//   · 片元：屏幕坐标 × `_Noise_Strength.z` 绕 (−0.5) 转 `_Time.y*_Noise_Strength.y` → 采样 `_Noise.r` ×2−1
//     → `_MainTex` 的 uv = 原 uv 绕 0.5 按 `_ScaleFromCenter` 缩放后 + `n*_Noise_Strength.x`
//     → `k = smoothstep((c.r - offsets.x)/(offsets.y - offsets.x))` → `lerp(LinearToSRGB(ColorSecondary),
//       LinearToSRGB(ColorMain), k)`；`out.rgb = c.a·col·COLOR.rgb`；`out.a = c.a·COLOR.a·pulse`
//   · 混合 = 原版 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`
// ⚠️ **两处与原版的差异，都是「载体不同」而不是「算式不同」**：
//   ① 原版顶点色里只有 uGUI 的 `COLOR`；我们的 `ImageQuad` 把材质 tint 写在 `_Color` 上
//      ⇒ 这里 `v.color * _Color` 合成一处（与 `判据/B109_R15` §2「把原版 COLOR 与我们的 _Color 合成一处」同一口径）。
//      `_Color` 出厂白 ⇒ 默认输出与原版逐像素相同。
//   ② 原版用 `gl_FragCoord / _ScaledScreenParams` 取屏幕 UV；这里用 `ComputeScreenPos`（平台无关），
//      两者在 D3D/GL 上取到的是同一个「右上为 (1,1)」的归一化坐标（原版那段还带 `_ProjectionParams.x` 翻转）。
// ⚠️ 需要 Unity 跑一次确认编译通过（`typecheck` 管不到 shader）；且**它必须不被 build 剥掉** ——
//    保证办法：把 `{ "Everguild/UI/UI Border Highlight Pulse Alpha", "WarpforgeVFX/UI/UI Border Highlight Pulse Alpha" }`
//    加进 `WarpforgeShaderMap.Replacements` 并重跑 `WarpforgeSetup.RegisterShaders`（那条表会进 Always Included）。
Shader "WarpforgeVFX/UI/UI Border Highlight Pulse Alpha"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _ColorMain ("ColorMain", Color) = (1, 0.8874891, 0, 1)
        [HDR] _ColorSecondary ("ColorSecondary", Color) = (1, 0, 0, 1)
        _Color_offsets ("Color offsets", Vector) = (0.66, 1, 0, 0)
        _Noise ("Noise", 2D) = "white" {}
        _Noise_Strength_X_Speed_Y_Scale_Z ("Noise Strength(X) Speed(Y) Scale (Z)", Vector) = (0.01, 1, 1, 0)
        _ScaleFromCenter ("ScaleFromCenter", Vector) = (1, 1, 0, 0)
        _NOISE_CHANNEL ("Noise Channel", Float) = 0
        _TimesParameters ("Times (ToOn,On,ToOff,Off)", Vector) = (0.1, 1, 0.1, 2)
        _AnimationSpeed ("AnimationSpeed", Float) = 1
        [HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl ("_QueueControl", Float) = -1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"
               "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            Name "UI_BORDER_PULSE_ALPHA"
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`、内置管线走默认 pass（同 `WFUIBorderHighlight.shader`）。
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                float4 screen : TEXCOORD1;
                float  pulse  : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _Noise;
            fixed4 _Color;
            float4 _ColorMain, _ColorSecondary, _Color_offsets;
            float4 _Noise_Strength_X_Speed_Y_Scale_Z, _ScaleFromCenter, _TimesParameters;
            float _NOISE_CHANNEL, _AnimationSpeed;

            // 原版 `Vertex_b1.hlsl:283-323`：4 段脉冲（toOn → on → toOff → off），`t` 为秒。
            float Pulse01(float t)
            {
                float toOn  = max(_TimesParameters.x, 1e-4);
                float on    = _TimesParameters.y;
                float toOff = max(_TimesParameters.z, 1e-4);
                float total = max(toOn + on + toOff + _TimesParameters.w, 1e-4);
                float f  = frac(t / total);
                float tt = total * f;
                float up    = saturate(tt / toOn);
                float down  = saturate(1.0 - (tt - (toOn + on)) / toOff);
                return up * down;
            }

            // 原版 `Fragment_b7.hlsl:195-246`：HDR 色先做一次 线性→sRGB（不是 clamp，>1 分量照旧 >1）。
            float3 LinearToSRGB(float3 c)
            {
                float3 lo = c * 12.923210144;
                float3 hi = exp2(log2(abs(c)) * 0.416666657) * 1.05499995 - 0.05499999;
                return lerp(lo, hi, step(0.0031308, c));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color * _Color;
                o.screen = ComputeScreenPos(o.pos);
                o.pulse  = Pulse01(_Time.y * _AnimationSpeed);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ① 屏幕坐标（右上为 (1,1)）→ 绕 (−0.5) 按 `_Noise_Strength.z` 缩放 → 随 `_Time` 旋转
                float2 suv = i.screen.xy / i.screen.w;
                float2 p = float2(suv.x * _Noise_Strength_X_Speed_Y_Scale_Z.z - 0.5,
                                  (1.0 - suv.y) * _Noise_Strength_X_Speed_Y_Scale_Z.z - 0.5);
                float  a = _Time.y * _Noise_Strength_X_Speed_Y_Scale_Z.y;
                float  ca = cos(a), sa = sin(a);
                float2 nuv = float2(p.x * ca + p.y * sa, -p.x * sa + p.y * ca) + 0.5;
                float  n = tex2D(_Noise, nuv).r * 2.0 - 1.0;

                // ② `_MainTex`：原 uv 绕 0.5 按 `_ScaleFromCenter` 缩放，再按噪声偏移
                float2 muv = float2(n * _Noise_Strength_X_Speed_Y_Scale_Z.x + (_ScaleFromCenter.x * (i.uv.x - 0.5) + 0.5),
                                    n * _Noise_Strength_X_Speed_Y_Scale_Z.x + (_ScaleFromCenter.y * (i.uv.y - 0.5) + 0.5));
                float4 c = tex2D(_MainTex, muv);

                // ③ 红通道做 smoothstep 混色（原版 `_247-249` = sRGB(ColorMain) - sRGB(ColorSecondary)）
                float k = saturate((c.r - _Color_offsets.x) / max(_Color_offsets.y - _Color_offsets.x, 1e-4));
                k = k * k * (3.0 - 2.0 * k);
                float3 col = lerp(LinearToSRGB(_ColorSecondary.rgb), LinearToSRGB(_ColorMain.rgb), k);

                float3 rgb = c.a * col * i.color.rgb;
                float  al  = c.a * i.color.a * i.pulse;
                return fixed4(rgb, al);
            }
            ENDCG
        }
    }
    Fallback Off
}
