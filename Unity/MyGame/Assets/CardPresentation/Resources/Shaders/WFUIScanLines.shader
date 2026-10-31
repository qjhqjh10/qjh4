// WFUIScanLines.shader —— 原版 `Everguild/UI/Scan Lines Alpha Mask` 的**逐指令复刻**（菜单 13 材质 #11）
//
// ============================ 判据（本机实读） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Scan Lines Alpha Mask.shader
// vertex 真源码  : …/Everguild_UI_Scan Lines Alpha Mask/Sub0_Pass0_Vertex_b1.hlsl:63-131
// fragment 真源码: …/Everguild_UI_Scan Lines Alpha Mask/Sub0_Pass0_Fragment_b7.hlsl:46-71（catch-all = 无 ALPHACLIP / 无 CLIP_RECT）
// 材质           : assets_full/bundle_menus_assets_all/Material/Material_-8529927314253658951.json
//                  （`UI Scan Lines Alpha Mask`；节点 = `Two Sides Event Window > Scan Lines`，sprite = `Glow UI W40K`）
//
// ============================ 算式（逐句对 HLSL）============================
//   `_49 = _ScanlineSpeed * _Time.y`                     ← **秒**
//   `_56 = sin(-_49) + TEXTCOORD_3.y`                     ← 见下「屏幕坐标」一节
//   `_62 = _56 * _ScanlineCount`；`_69 = sin(_62) * _ScanlineIntensity`
//   `_76 = round(COLOR.a*255)/255`
//   `_119.._121 = COLOR.rgb * (tex.rgb + add.rgb)`        ← base
//   `_122 = _76 * (tex.a + add.a)`
//   `_147 = _69 * _ScanColor.a`
//   `_148 = _147 * _122`                                  ← out.a
//   `_149.._151 = _148 * mad(_69, mad(-COLOR.rgb, tex.rgb, _ScanColor.rgb), base)`
//                = out.a * lerp(base, _ScanColor.rgb, _69) ← out.rgb
//
// ============================ 🔴 屏幕坐标：`TEXTCOORD_3` = **clip-space 位置** ============================
//   原版 vertex 把 `gl_Position`（**整个 float4**）原样塞进一个普通 varying（`TEXTCOORD_3 : TEXCOORD20`，
//   Vertex_b1.hlsl:105-108），fragment 只取它的 **`.y`**（Fragment_b7.hlsl:49）。
//   ⇒ 扫描线是**屏幕空间（NDC y）**的横带，**不是** uv 空间的 —— 同一个 quad 换尺寸/换 uv 都不会改变带子的疏密。
//   ⚠️ ⛔ **别改成 `i.pos.y`（`SV_POSITION`）**：那是**像素坐标**，量纲完全不同（差 ScreenH/2 倍，而且会随分辨率变）。
//   ⚠️ 也不能用 uv.y：原版这里与贴图无关。
//   本文件照抄同一个形状：`o.clip = o.pos`（raw clip position）当 varying 传下去。
//
// ============================ 死值（材质上有、DXBC / Properties 里都没有 ⇒ ⛔ 别抄）============================
//   `_InterferenceColor` · `_InterferenceTilling` · `_InterrferenceEffectTransparency` · `_FlickerIntensity` ·
//   `_FlickerSpeed_X_Y` · `_Speed` · `_Speed_` —— 全是原版旧 shader 的残留。
//
// ============================ 时间口径 ============================
//   原版读 `type_UnityPerCamera._Time.y` = **秒**（`_Time = (t/20, t, 2t, 3t)`）。
//
// ============================ 混合 / 状态 ============================
//   `Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha`（**预乘 α**）· `ZWrite Off` · `Cull Off`
//   · `ColorMask [_ColorMask]` · `ZTest [unity_GUIZTestMode]` · Stencil 照抄。
//
// ⚠️ `Shader` 名必须**逐字**是 `WarpforgeVFX/UI/Scan Lines` —— `Shell/ParentEventWindow.cs` 的
//    `ScanLinesMaterial()` 退路走的是 `Shader.Find("WarpforgeVFX/UI/Scan Lines")`（执行代理 `B110_D4` 已接线）。
Shader "WarpforgeVFX/UI/Scan Lines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [Header(Scanlines)] _ScanlineCount ("Scanline Count", Float) = 800
        _ScanlineSpeed ("Scanline Move Speed", Float) = 200
        _ScanlineIntensity ("Scanline Intensity", Float) = 0.04
        _ScanColor ("Scanline Alpha", Color) = (1, 1, 1, 0.2)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [HideInInspector] _TextureSampleAdd ("TextureSampleAdd", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "CanUseSpriteAtlas"="true"
            "IGNOREPROJECTOR"="true"
            "PreviewType"="Plane"
            "QUEUE"="Transparent"
            "RenderType"="Transparent"
        }
        Pass
        {
            Name "Default"
            Blend One OneMinusSrcAlpha
            ColorMask [_ColorMask]
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`。
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
            float  _ScanlineCount;
            float  _ScanlineSpeed;
            float  _ScanlineIntensity;
            fixed4 _ScanColor;
            float4 _TextureSampleAdd;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
                float4 clip  : TEXCOORD1;   // 原版 `TEXTCOORD_3`（= raw clip position）
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.clip = o.pos;                                // ← 原版 `TEXTCOORD_3 = gl_Position`（Vertex_b1.hlsl:105-108）
                o.color = v.color * _Color;                    // 原版 `COLOR_1 = COLOR * _Color`（:99-104）
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);          // 原版 `mad(uv, _MainTex_ST.xy, _MainTex_ST.zw)`（:105-108）
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Fragment_b7.hlsl:48-51
                float s = sin(-_ScanlineSpeed * _Time.y) + i.clip.y;
                float k = sin(s * _ScanlineCount) * _ScanlineIntensity;

                // :52-53 —— `_76 = round(COLOR.a*255)/255`
                float a255 = round(i.color.a * 255.0) * 0.003921568859368563;

                // :54-64
                float4 tx = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float3 baseRgb = i.color.rgb * tx.rgb;                 // `_119.._121`
                float  outA    = (k * _ScanColor.a) * (a255 * tx.a);   // `_147 * _122`
                float3 outRgb  = outA * lerp(baseRgb, _ScanColor.rgb, k);   // `_149.._151`

                return fixed4(outRgb, outA);                          // `SV_Target.w = _148`（:71）
            }
            ENDCG
        }
    }
    Fallback Off
}
