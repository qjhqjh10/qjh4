// WFUIDistort.shader —— 原版 `Everguild/UI/UI Distort` 的**逐指令复刻**（菜单 13 材质 #6）
//
// ============================ 判据（本机实读） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_UI Distort.shader
// vertex 真源码  : …/Everguild_UI_UI Distort/Sub0_Pass0_Vertex_b1.hlsl:63-131（标准 UGUI 顶点：变换 + `COLOR*_Color` + uv ST，
//                  **没有**任何变形；变形全在 fragment）
// fragment 真源码: …/Everguild_UI_UI Distort/Sub0_Pass0_Fragment_b7.hlsl:39-63（catch-all 变体 = 无 ALPHACLIP / 无 CLIP_RECT）
// 两份材质       : assets_full/bundle_menus_assets_all/Material/
//                  Material_-6878048798577840027.json（`UI Distort in Atlas Small objects`）
//                  Material_-909700596349559062.json（`Forge Candle`）
//
// ============================ 算式（逐句对 HLSL）============================
//   原版 `_54 = _Time.y` · `_55 = S.x*_54` · `_56 = S.y*_54`  （S = `_DistortionSpeedAndScale`）
//   `_82 = mad(noise.Sample(uv*S.zw + _55/_56).x, 2, -1)`      ⇒ n = noise(uv*scale + speed*t)*2-1
//   `_96 = _MainTex.Sample(uv + _DistortionAmount*n)`           ⇒ 只按噪声**同一标量**平移两轴
//   `_124 = round(COLOR.a*255)/255`（原版对 α 做 8bit 量化）
//   `_136 = (tex.a + _TextureSampleAdd.a) * _124`               ⇒ out.a
//   `_137 = _136 * (tex.r + add.r) * COLOR.r`                   ⇒ out.rgb = out.a * tex.rgb * COLOR.rgb
//   β：`_DistortionSpeed`（float **和** 同名 Color 各一条）· `_Parallax`(0.005) —— **DXBC 常量表里没有**
//      （shaderlab 的 Properties 里也没有）⇒ 原版旧 shader 的残留**死值**，⛔ 别抄。
//
// ============================ 时间口径 ============================
//   原版 fragment 读 cbuffer `type_UnityPerCamera._Time`，取 **`.y`**。
//   Unity / URP 的 `_Time = (t/20, t, 2t, 3t)`（`UnityInput.hlsl` + `ScriptableRenderer` 的 `time*(1/20,1,2,3)`）
//   ⇒ `_Time.y` = **秒**。（同族 #4 Pulsating 读的是 `.x` = t/20，两者不同，⛔ 别互相抄。）
//
// ============================ 混合 / 状态（照原版 shaderlab 逐条）============================
//   `Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha`（**预乘 α**）· `ZWrite Off` · `Cull Off`
//   · `ColorMask [_ColorMask]` · `ZTest [unity_GUIZTestMode]` · Stencil 照抄。
//   ⚠️ 因为输出 rgb 已经乘过 α，预乘混合才是对的；换成普通 `SrcAlpha OneMinusSrcAlpha` 会**再乘一次 α**（变暗）。
//
// ============================ 我们这一侧的三个不变式 ============================
//   ① **必须保留 `_MainTex`**（`ImageQuad.SetMaterial` 写的是 `material.mainTexture`）。
//   ② 原版的顶点色 `COLOR`（= Image 的 tint）在我们的管线里走的是 `material._Color`
//      （`ImageQuad.SetTint` 写 `sharedMaterial.color`）⇒ 顶点里 `v.color * _Color` 把两路合成一处，
//      与原版 `COLOR_1 = COLOR * _Color`（Vertex_b1.hlsl:99-104）逐句一致。
//   ③ 原版 multi_compile 的两个变体（`UNITY_UI_ALPHACLIP` / `UNITY_UI_CLIP_RECT`）**本工程不用**
//      —— 我们的裁切走几何（`ViewportClip`，见 `MenuDraw.Rect` 的 doc），所以只实现 catch-all 变体 `b7`。
//
// 用法：`.mat` 挂在 quad 上即可，无需任何 C# 逐帧驱动（`_Time` 自走）。
Shader "WarpforgeVFX/UI/UI Distort"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DistortionSpeedAndScale ("Distortion Speed (XY) Scale (ZW)", Vector) = (1, 1, 1, 1)
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.05
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        // 原版 fragment 里 `_MainTex.Sample + _TextureSampleAdd` 那一项（UI 字体图集用；Sprite 恒 0）
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
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`（同 `WFUIPulsating.shader` 的先例）。
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
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _DistortionSpeedAndScale;
            float  _DistortionAmount;
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
                o.color = v.color * _Color;                    // 原版 `COLOR_1 = COLOR * _Color`（Vertex_b1.hlsl:99-104）
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);          // 原版 `mad(uv, _MainTex_ST.xy, _MainTex_ST.zw)`（:105-108）
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;                             // 秒（见文件头「时间口径」）
                float2 S = _DistortionSpeedAndScale.xy;        // (speedX, speedY)
                float2 K = _DistortionSpeedAndScale.zw;        // (scaleX, scaleY)

                // Fragment_b7.hlsl:41-44 —— `_55 = S.x*t` · `_56 = S.y*t` ·
                //   `_82 = mad(noise(uv*K + (_55,_56)).x, 2, -1)`
                float2 nuv = float2(i.uv.x * K.x + S.x * t,
                                    i.uv.y * K.y + S.y * t);
                float  n   = tex2D(_NoiseTex, nuv).x * 2.0 - 1.0;

                // :45-46 —— `_96 = _MainTex.Sample(uv + _DistortionAmount * n)`
                float4 tx = tex2D(_MainTex, i.uv + _DistortionAmount * n) + _TextureSampleAdd;

                // :50-52 —— `_124 = round(COLOR.a*255) * 1/255`
                float a255 = round(i.color.a * 255.0) * 0.003921568859368563;

                // :55-56 —— `_136 = tex.a * _124`（`_TextureSampleAdd` 已经加在 `tx` 里）
                float outA = a255 * tx.a;

                // :57-63 —— `out.rgb = outA * tex.rgb * COLOR.rgb`，`out.a = outA`
                return fixed4(outA * i.color.rgb * tx.rgb, outA);
            }
            ENDCG
        }
    }
    Fallback Off
}
