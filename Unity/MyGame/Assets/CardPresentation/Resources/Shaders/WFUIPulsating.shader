// WFUIPulsating.shader —— 原版 `Everguild/UI/Pulsating` 的**逐指令复刻**
//
// ============================ 判据（本机实读） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Pulsating.shader
// vertex 真源码  : …/Everguild_UI_Pulsating/Sub0_Pass0_Vertex_b1.hlsl:91-122
// fragment 真源码: …/Everguild_UI_Pulsating/Sub0_Pass0_Fragment_b7.hlsl:29-49
// 材质参数       : assets_full/bundle_menus_assets_all/Material/Material_-2164664953476156828.json（`Forge UI Light`）
//                  assets_full/bundle_menus_assets_all/Material/Material_750193365382565044.json（`Everguild Campaing Premium node`）
//
// 🔴 **时间口径（就地订正 R15 §1·4 的「8 Hz」）**：原版 vertex 读的是 cbuffer `type_UnityPerCamera._Time`，
//    而 URP 的 `_Time` = `(t/20, t, 2t, 3t)`（`Com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl:42`
//    + `Runtime/ScriptableRenderer.cs:499` 的 `time * (1/20, 1, 2, 3)`）
//    ⇒ 实际是 `sin(A.y · t/20)`；A.y = 50 ⇒ **2.5 rad/s ≈ 0.398 Hz（2.51 s 一个来回）**，不是 8 Hz。
//    （R15 的 8 Hz = 50/2π，那一步把 `_Time.x` 当成了秒；我们的 2.5 s 呼吸与「蜡烛火苗 / 高级节点呼吸」自洽。）
//    ⚠️ 与 #5 / #9 不同 —— 那两条用的是 `_TimeParameters.x`（= 秒），见各自文件头。
//
// 🔴 **死值（材质上有、DXBC 常量表里没有 ⇒ ⛔ 别抄）**：`_Size`(1.2,58.19,0,0) ·
//    `_SizeRotationAlpha`(1.2,50,180,0.16) · `_RemapAtlasUVs`(0.5,0.5,0,1)。
//    真正进算式的是 `_SizeRotation` 与 `_AlphaPulsating`（材质里这两个是 m_Colors 存的，见上面两份 JSON）。
//
// 用法：材质挂在一张 quad 上即可，无需任何 C# 逐帧驱动（`_Time` 自走）。
Shader "WarpforgeVFX/UI/UI Pulsating"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _SizeRotation ("SizeMod (X) Speed (Y) Rotation (Z)", Vector) = (0.8, 50, 180, 0)
        _AlphaPulsating ("AlphaVariation (X) Speed (Y)", Vector) = (0.8, 50, 0, 0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        // 原版 fragment 里 `_MainTex.Sample + _TextureSampleAdd` 那一项（UI 字体图集用的，普通 sprite 恒 0）
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
            // 原版：`Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha`
            Blend One OneMinusSrcAlpha
            ColorMask [_ColorMask]
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`，与 `WFUIBorderHighlight.shader` 同一条先例。
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
            float4 _SizeRotation;
            float4 _AlphaPulsating;
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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float t = _Time.x;   // ⚠️ `_Time.x` = t/20（见文件头「时间口径」），原版读的就是它

                // --- α：`_224 = mad((1-A.x)*(sin(A.y*t)+1), 0.5, A.x) * COLOR.w*_Color.w`（Vertex_b1.hlsl:91-101）
                float aHalf = (sin(_AlphaPulsating.y * t) + 1.0) * 0.5;
                float aFactor = _AlphaPulsating.x + (1.0 - _AlphaPulsating.x) * aHalf;

                o.color = v.color * _Color;
                o.color.w = aFactor * o.color.w;

                // --- 尺度：`_251 = mad((1-X)*(sin(Y*t)+1), 0.5, X)`（:105-113）
                float sHalf = (sin(_SizeRotation.y * t) + 1.0) * 0.5;
                float sFactor = _SizeRotation.x + (1.0 - _SizeRotation.x) * sHalf;

                // --- 旋转：`_240 = Z*t*0.017453292384743690`（度→弧度）（:106-107）
                float ang = _SizeRotation.z * t * 0.017453292384743690;

                // --- uv' = 绕 (0.5,0.5) 先缩放、再转 -ang（:114-122）
                //     原版逐句：uvx = 0.5 + d.x*cos + d.y*sin · uvy = 0.5 + d.y*cos - d.x*sin
                float2 d = float2(v.uv.x * _MainTex_ST.x + _MainTex_ST.z,
                                  v.uv.y * _MainTex_ST.y + _MainTex_ST.w) - 0.5;
                d *= sFactor;
                float c = cos(ang), s = sin(ang);
                o.uv = float2(d.x * c + d.y * s,
                              d.y * c - d.x * s) + 0.5;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Fragment_b7.hlsl:31-48 —— `_34 = round(COLOR.w*255)/255`；`_82 = _34 * tex.a`
                float  a255   = round(i.color.w * 255.0) * 0.003921568859368563;
                float4 tx     = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float  outA   = a255 * tx.a;
                // out.rgb = outA * COLOR.rgb * tex.rgb
                return fixed4(outA * i.color.rgb * tx.rgb, outA);
            }
            ENDCG
        }
    }
    Fallback Off
}
