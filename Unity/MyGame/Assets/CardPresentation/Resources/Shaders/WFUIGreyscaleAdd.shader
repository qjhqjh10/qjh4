// WFUIGreyscaleAdd.shader —— 原版 `Everguild/UI/Greyscale Add` 的**逐指令复刻**
//
// ============================ 判据（本机实读 2026-10-31 · B110_M1） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Greyscale Add.shader
// vertex 真源码  : …/Everguild_UI_Greyscale Add/Sub0_Pass0_Vertex_b1.hlsl:61-108
// fragment 真源码: …/Everguild_UI_Greyscale Add/Sub0_Pass0_Fragment_b7.hlsl:39-61（catch-all 变体）
// 材质参数       : assets_full/bundle_menus_assets_all/Material/Material_-4471755909380014346.json（`UI Greyscale Add`）
// 用途/落点      : 27 个 prefab 的 `Image Top`（报价容器 `General Basic Offer Container/Popup*` ·
//                  `Shop Menu Variant` · 称号抽屉 `Title Drawer[ Horizontal Variant]`）—— 阵营徽记那一层。
//                  我们这边两处调用点：`Shell/TitleTab.cs:375`（本批）、`Shell/OfferContainer.cs`（归 `B110_M3`）。
//
// ============================ 算式（逐字抄自 Fragment_b7.hlsl） ============================
//   `_81.._84 = tex + _TextureSampleAdd`                       （:42-45）
//   `_90      = round(COLOR.w * 255) / 255`                    （:46-47）—— 顶点 α 量化到 8 位
//   `_99.._101 = tex.rgb * COLOR.rgb` · `_102 = tex.a * _90`   （:48-51）
//   `_104     = dot(_99.._101, (0.30, 0.59, 0.11))`            （:52）   —— 亮度
//   `out.rgb  = _102 * mad(_GreyScale, mad(-COLOR.rgb, tex.rgb, _104), _99..101)`
//             = _102 * lerp(COLOR.rgb*tex.rgb, grey, _GreyScale)          （:54-56）
//   `out.a    = _102`                                                      （:60）
//   Blend **`OneMinusDstColor One`**（= 滤色 / screen）                     （shaderlab `:24`）
//
// 🔴 `_GreyScale = 1` **常驻**（材质现读 1.0；全树 ILSpy 没有任何代码写它 ⇒ 恒 1）⇒ 实际输出
//    = `tex.a * colorA * grey`，即「**整块只剩灰度、再滤色叠上去**」。
//    ⚠️ 原版**为什么**给阵营徽记上灰度滤镜 —— **没找到设计意图**（B109_R15 §4·6）；本文件只按数值复刻，
//       ⛔ 不给它编一处理由。
//
// 👍 与原版的唯一替换：`COLOR` = `vcol * _Color`（本工程约定：Image tint 走顶点色、material tint 走 `_Color`）。
//    原版 vertex 就是 `COLOR * _Color`（`Sub0_Pass0_Vertex_b1.hlsl:80-87`）⇒ **完全一致**，不是近似。
//
// ⚠️ `_TextureSampleAdd`：原版在 `type_Globals`（c3），是 uGUI 给字体图集用的全局量，普通 sprite 恒 0。
//    我们的 `ImageQuad` 不设它 ⇒ 保持属性默认 (0,0,0,0) 即为原版语义。
//
// 用法：材质挂在一张 quad 上即可，无 `_Time`、无脚本驱动（静态）。
Shader "WarpforgeVFX/UI/UI Greyscale Add"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _GreyScale ("GreyScale", Range(0, 1)) = 1
        // 原版把混合模式写死在 Pass 里；这里参数化，材质里的值 = 原版那两个（`OneMinusDstColor` = 4 · `One` = 1），
        // 好处是自检能**数值化**核「混合模式没被改回 alpha」（见 `B110_M1_MenuMat_Assert`）。
        _SrcBlend ("Src Blend", Float) = 4
        _DstBlend ("Dst Blend", Float) = 1
        _ColorMask ("Color Mask", Float) = 15
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
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
            // 原版：`Blend OneMinusDstColor One, OneMinusDstColor One`
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`（同 `WFUIPulsating.shader` 的先例）
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
            float  _GreyScale;
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
                // 原版 `TEXCOORD_1 = uv * _MainTex_ST.xy + _MainTex_ST.zw`（Vertex_b1.hlsl:88-89）
                o.uv = float2(v.uv.x * _MainTex_ST.x + _MainTex_ST.z,
                              v.uv.y * _MainTex_ST.y + _MainTex_ST.w);
                // 原版 `COLOR_1 = COLOR * _Color`（:80-87）
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 tx = tex2D(_MainTex, i.uv) + _TextureSampleAdd;         // `_81.._84`

                // `_90 = round(COLOR.w * 255) * (1/255)`（:46-47）
                float  a8    = round(i.color.a * 255.0) * 0.0039215688593685626983642578125;

                float3 crgb  = tx.rgb * i.color.rgb;                            // `_99.._101`
                float  ca    = tx.a * a8;                                       // `_102`

                // `_104 = dot(_99.._101, (0.30, 0.59, 0.11))`（:52）
                float  grey  = dot(crgb, float3(0.300000011920928955078125,
                                                0.589999973773956298828125,
                                                0.10999999940395355224609375));

                // `_124.._126 = _102 * mad(_GreyScale, mad(-COLOR.rgb, tex.rgb, _104), _99..101)`（:54-56）
                float3 grey3 = float3(grey, grey, grey);
                float3 rgb   = ca * lerp(crgb, grey3, _GreyScale);

                return fixed4(rgb, ca);                                         // `SV_Target.w = _102`（:60）
            }
            ENDCG
        }
    }
    Fallback Off
}
