// WFUIAdditive.shader —— 原版 `Everguild/UI/Additive` 的**逐指令复刻**（菜单 13 材质 · #7）
//
// ============================ 判据（本机实读，2026-10-31）============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Additive.shader
//   · `Blend One One, One One` · `ColorMask [_ColorMask]` · `ZClip On` · `ZTest [unity_GUIZTestMode]`
//   · `ZWrite Off` · `Cull Off` · 完整 `Stencil` 块 · Tags（CanUseSpriteAtlas / IGNOREPROJECTOR / QUEUE=Transparent）
//   · 属性表 9 个：`_MainTex _Color _StencilComp _Stencil _StencilOp _StencilWriteMask _StencilReadMask
//                   _ColorMask [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip`
// vertex 真源码 : …/Everguild_UI_Additive/Sub0_Pass0_Vertex_b1.hlsl:61-108
//                  `COLOR_1 = COLOR * _Color`（4 道 mul，:80-87）；`uv = TRANSFORM_TEX(TEXCOORD, _MainTex_ST)`（:88-89）
//                  ⚠️ 另有一段 uGUI 裁切矩形（`_ClipRect` / `_UIMaskSoftness*` → TEXCOORD_2，:94-107）
//                     —— 只被 `UNITY_UI_CLIP_RECT` 变体用；本材质 keywords 为空，**走不到**，故未复刻（见文件末）。
// fragment 真源码: …/Sub0_Pass0_Fragment_b7.hlsl:29-49（catch-all 变体）
//                  `_34 = round(COLOR.w*255)/255`；`_82 = _34 * tex.w`；
//                  `out.rgb = _82 * COLOR.rgb * tex.rgb`；`out.a = _82`
// ALPHACLIP 变体 : …/Sub0_Pass0_Fragment_b8.hlsl:28-54 —— 多一句 `if (mad(_34, _71, -0.001) < 0) discard;`
// 材质（原版）  : d:/2/新解包资源/assets_full/bundle_menus_assets_all/Material/Material_-8464978790139598553.json
//                  `m_Name = "UI Additive"` · `_Color = (1,1,1,1)` · `_MainTex` 无图（运行期由 Image 喂）
//                  🔴 那个 JSON 里另有 `_ColorMain 337.79…` / `_Reveal 0.723` / `_Noise_Strength_*` 等
//                     —— 它们是**上一颗 shader 的残留**（本 shader 的 `m_PropInfo` 里没有这些名字）⇒ ⛔ 别抄。
//
// ============================ 名字口径（⛔ 别改成 "…/UI/UI Additive"）============================
// 原版 = `Everguild/UI/Additive`（**没有**第二个 `UI `）。同族的 `Everguild/UI/Pulsating` 我们落成
// `WarpforgeVFX/UI/UI Pulsating` 是因为**原版那个名字本身**带 `UI`（`Everguild/UI/UI Clock` 同族）。
// 本条按原版名照搬 ⇒ **`WarpforgeVFX/UI/Additive`**（`B110_D4` 已经在 `Shell/ParentEventWindow.cs` 用
// `Shader.Find("WarpforgeVFX/UI/Additive")` 兜底取它，改名字会让那条路静默失效）。
//
// ============================ 与 `WarpforgeVFX/Sprites/Additive` 的分工 ============================
// 那张是**精灵**加法（`Everguild/Sprites/Sprite Additive`，带 `_RendererColor` / `_Flip` / `PixelSnap` /
// ETC1 外部 alpha），给**战场特效**用；本条是**uGUI Image** 加法（stencil / ColorMask / 顶点色里已乘 `_Color`）。
// 两者算式的**公共部分**相同（都预乘 alpha），但属性表不同、cbuffer 来源不同 ⇒ **不要合并**。
//
// ============================ 未复刻的变体（如实记，⛔ 不是「查不到」）============================
// 原版这条 shader 有 4 档变体（`UNITY_UI_ALPHACLIP` × `UNITY_UI_CLIP_RECT`）：
//   b7 = 都没有（**本材质走的这一档，已逐指令复刻**）· b8 = 只有 ALPHACLIP（已复刻 clip 那一句）
//   b9 / b10 = 带 `UNITY_UI_CLIP_RECT`（uGUI `RectMask2D` 的裁切矩形，走 TEXCOORD_2 / `_ClipRect`）
// ⇒ **b9 / b10 本轮没做**：① 材质 keywords 为空、走不到；② 我们 UI 的视口裁切走的是 `MenuDraw.Clip*`
//   （按 quad 改几何/alpha），不是 uGUI 的 `RectMask2D` ⇒ 这条路上没有消费者。
//   将来若要接 `RectMask2D` 语义，判据 = 那两份 `.hlsl` 还在原处，照 b7 的写法补一段即可。
Shader "WarpforgeVFX/UI/Additive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            // 原版：`Blend One One, One One` —— RGB 与 A **两路都是纯加法**（这是本条的**全部**差异所在）
            Blend One One, One One
            ColorMask [_ColorMask]
            ZClip On
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`，与 `WFUIBorderHighlight.shader` /
            //    `WFUIPulsating.shader` 同一条先例（URP 17.3.0）。
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
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
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
                // Vertex_b1.hlsl:88-89 —— `uv = mad(uv, _MainTex_ST.xy, _MainTex_ST.zw)`
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // Vertex_b1.hlsl:80-87 —— `COLOR_1 = COLOR * _Color`（原版 4 道独立 mul，
                // 即「uGUI Image 的 `m_Color`（= 顶点色）」×「材质 `_Color`」合成一处）
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Fragment_b7.hlsl:31-41 —— `_34 = round(COLOR.w*255)/255`；`_82 = _34 * tex.a`
                float  a255 = round(i.color.w * 255.0) * 0.003921568859368563;
                float4 tx   = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float  outA = a255 * tx.a;
                // Fragment_b8.hlsl:40-43（ALPHACLIP 档）—— `if (mad(_34, _71, -0.001) < 0) discard;`
                #ifdef UNITY_UI_ALPHACLIP
                    if (outA - 0.001 < 0.0) discard;
                #endif
                // Fragment_b7.hlsl:42-48 —— RGB **预乘 alpha**：`out.rgb = outA * COLOR.rgb * tex.rgb`
                return fixed4(outA * i.color.rgb * tx.rgb, outA);
            }
            ENDCG
        }
    }
    Fallback Off
}
