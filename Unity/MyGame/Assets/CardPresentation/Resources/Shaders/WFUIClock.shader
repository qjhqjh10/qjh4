// WFUIClock.shader —— 原版 `Everguild/UI/UI Clock` 的**逐指令复刻**
//
// ============================ 判据（本机实读） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_UI Clock.shader
// vertex 真源码  : …/Everguild_UI_UI Clock/Sub0_Pass0_Vertex_b1.hlsl:115-167
// fragment 真源码: …/Everguild_UI_UI Clock/Sub0_Pass0_Fragment_b7.hlsl:48-86
// 材质参数       : assets_full/bundle_menus_assets_all/Material/Material_8837680948194134486.json（`UI Clock`，唯一一份，参数共享）
//
// 时间口径：原版读 cbuffer `type_Globals._TimeParameters.x`，**URP 里 `_TimeParameters.x` = 秒**
//   （`…universal@…/Runtime/ScriptableRenderer.cs:504` 的 `new Vector4(time, sin, cos, 0)`）⇒ 本 shader 用 `_Time.y`。
//   ⇒ `_Rotation_Speed_Seconds = -0.1591549 = -1/(2π)` ⇒ **39.48 s 一圈**；`_Rotation_Speed_Minutes = -0.0026526` ⇒ 39.48 min 一圈。
//
// ⚠️ **未查清（原样照抄，交真机验收）**：原版 fragment 最后一句是 `SV_Target.w = 1.0`，而材质
//   `_SrcBlend 5 / _DstBlend 10`（SrcAlpha / OneMinusSrcAlpha）⇒ 画面落下的是**不透明**结果，
//   而不是「只有环与针」。判据就是原版字节码，本轮**不改**（铁律 18·0：需要拍板一律复刻原版）。
// 🔴🔴 **但真机验收时优先看这一处（风险已定位到「纹理口径」，不是算式）**：
//   原版 fragment 的唯一贴图项是 `_195 = 针覆盖 + _MainTex.Sample(...).x`，即 **RGB 就是遮罩**；
//   而我们两张导出的 PNG（`assets_full/…/Texture2D/WF_icon_clock for shader.png` 与
//   `Warpforge_tools/data/ui_extract/menus_assets_all/Texture2D/` 同名那份）**RGB 全 255、遮罩只在 α 通道**
//   （64×64 里 3330 px 是 `(255,255,255,0)`）⇒ 照上式 `tex.rgb ≡ 1` ⇒ 这颗会渲成**一块不透明的纯色方块**。
//   最可能的真相 = 原版跑的是**预乘 α** 的图（RGB 已被 α 乘过）。⇒ 若真机上是方块，
//   **修法应落在这张贴图的导入/预乘口径**上（⛔ 不是改算式），并由调度台裁定。

//
// 用法：材质挂在一张 quad 上即可（`_MainTex` = `WF_icon_clock for shader` —— 那张**没有画指针**，指针由本 shader 画）。
Shader "WarpforgeVFX/UI/UI Clock"
{
    Properties
    {
        [PerRendererData] _MainTex ("MainTex", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Pulse_toOn_On_toOff_Off ("Pulse (toOn,On,toOff,Off)", Vector) = (0.5, 0, 0.5, 0)
        _MinMaxScale ("MinMaxScale", Vector) = (1.2, 1, 0, 0)
        _Rotation_Speed_Seconds ("Rotation Speed Seconds", Float) = -0.1591549
        _ClockHandle_Offset_Seconds ("ClockHandle Offset Seconds", Vector) = (0, 0.17, 0, 0)
        _ClockHanldeSeconds_W_H_R ("ClockHanldeSeconds (W,H,R)", Vector) = (0.06, 0.35, 0.1, 0)
        _SecondsHandleStartOffset ("SecondsHandleStartOffset", Float) = 0
        _MinutesHandleStartOffset ("MinutesHandleStartOffset", Float) = 0
        _ClockHanldeMinutes_W_H_R ("ClockHanldeMinutes (W,H,R)", Vector) = (0.06, 0.23, 0.1, 0)
        _Clock_Handle_Offset_Minutes ("Clock Handle Offset Minutes", Vector) = (0, 0.1, 0, 0)
        _Rotation_Speed_Minutes ("Rotation Speed Minutes", Float) = -0.002652582
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            Blend SrcAlpha OneMinusSrcAlpha     // 原版材质 `_SrcBlend 5 / _DstBlend 10`
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
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _Pulse_toOn_On_toOff_Off;
            float4 _MinMaxScale;
            float  _Rotation_Speed_Seconds;
            float4 _ClockHandle_Offset_Seconds;
            float4 _ClockHanldeSeconds_W_H_R;
            float  _SecondsHandleStartOffset;
            float  _MinutesHandleStartOffset;
            float4 _ClockHanldeMinutes_W_H_R;
            float4 _Clock_Handle_Offset_Minutes;
            float  _Rotation_Speed_Minutes;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float4 uvS   : TEXCOORD0;   // xy = 秒针坐标系 uv · z = 未转的缩放 uv 的 y
                float4 uvM   : TEXCOORD1;   // xy = 分针坐标系 uv
                float2 uvRaw : TEXCOORD2;   // 原版 `INTERP_3.w / INTERP_4.w`（缩放后的 uv，取 MainTex）
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;                     // 原版：`INTERP = COLOR`（**不乘 `_Color`** —— 那份 cbuffer 里没有它）

                float t = _Time.y;                     // 秒（见文件头「时间口径」）

                // ---------- 4 段脉冲缩放（Vertex_b1.hlsl:115-131） ----------
                float4 P = _Pulse_toOn_On_toOff_Off;
                float period = P.x + P.y + P.z + P.w;
                float ph = t / period;
                float fracP = frac(abs(ph));
                fracP = (ph >= 0.0) ? fracP : -fracP;
                float phase = fracP * period;
                float fOff = clamp((phase - (P.x + P.y)) / P.z, 0.0, 1.0);   // _239
                float fOn  = clamp(phase / P.x, 0.0, 1.0);                   // _236
                float f    = clamp(1.0 - fOff, 0.0, 1.0) * fOn;              // _242
                float scale = f * (_MinMaxScale.y - _MinMaxScale.x) + _MinMaxScale.x;  // _250

                float2 d = (float2(v.uv.x * _MainTex_ST.x + _MainTex_ST.z,
                                   v.uv.y * _MainTex_ST.y + _MainTex_ST.w) - 0.5) * scale;

                // ---------- 秒针坐标系（:133-143） ----------
                float aS = (t + _SecondsHandleStartOffset) * _Rotation_Speed_Seconds;
                float cs = cos(aS), ss = sin(aS);
                float2 uvS = float2(d.x * cs + d.y * ss,
                                    -d.x * ss + d.y * cs) + 0.5 + _ClockHandle_Offset_Seconds.xy;

                // ---------- 分针坐标系（:144-153） ----------
                float aM = (t + _MinutesHandleStartOffset) * _Rotation_Speed_Minutes;
                float cm = cos(aM), sm = sin(aM);
                float2 uvM = float2(d.x * cm + d.y * sm,
                                    -d.x * sm + d.y * cm) + 0.5 + _Clock_Handle_Offset_Minutes.xy;

                o.uvS = float4(uvS, 0, 0);
                o.uvM = float4(uvM, 0, 0);
                // `INTERP_3.w = mad(scale, uv.x-0.5, 0.5)` · `INTERP_4.w = mad(scale, uv.y-0.5, 0.5)`
                o.uvRaw = float2(d.x + 0.5, d.y + 0.5);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---------- 秒针（Fragment_b7.hlsl:50-63） ----------
                float3 HS = _ClockHanldeSeconds_W_H_R;                 // (W, H, R)
                float hwS = max(min(min(abs(HS.z + HS.z), abs(HS.x)), abs(HS.y)), 9.9999997e-06);
                float2 qS = float2(max(hwS + (abs(i.uvS.x * 2.0 - 1.0) - HS.x), 0.0),
                                   max(hwS + (abs(i.uvS.y * 2.0 - 1.0) - HS.y), 0.0));
                float distS = sqrt(dot(qS, qS)) / hwS;
                float aS = 1.0 - distS;
                float gS = abs(ddy(distS)) + abs(ddx(distS));
                float covS = aS / max(gS, 9.9999997e-06);

                // ---------- 分针（:64-73） ----------
                float3 HM = _ClockHanldeMinutes_W_H_R;
                float hwM = max(min(min(abs(HM.z + HM.z), abs(HM.x)), abs(HM.y)), 9.9999997e-06);
                float2 qM = float2(max(hwM + (abs(i.uvM.x * 2.0 - 1.0) - HM.x), 0.0),
                                   max(hwM + (abs(i.uvM.y * 2.0 - 1.0) - HM.y), 0.0));
                float distM = sqrt(dot(qM, qM)) / hwM;
                float aM = 1.0 - distM;
                float gM = abs(ddy(distM)) + abs(ddx(distM));
                float covM = aM / max(gM, 9.9999997e-06);

                float h = clamp(covM, 0.0, 1.0) + clamp(covS, 0.0, 1.0);   // _174

                // ---------- 表盘（:75-85） ----------
                float4 tx = tex2D(_MainTex, i.uvRaw);
                return fixed4(saturate(h + tx.rgb) * i.color.rgb, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
