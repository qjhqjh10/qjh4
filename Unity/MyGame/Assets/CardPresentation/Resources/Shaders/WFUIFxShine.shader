// WFUIFxShine.shader —— 原版 `Everguild/FX/FX Shine For Animation Normal` 的**逐指令复刻**（菜单 13 材质 · #3）
//
// 用途：「选中头像后，一道光 4 秒扫过头像」（`B110_M3` 报告 §「#3」那一条）。
// 驱动 = 原版 `AvatarDisplay.DoShine(float time)`：把 `_Shine_Movement` 从 `(0, 0.2, 0, 0)`
//   用 DOTween `DOVector` 补到 `(0, 1, 0, 0)`，**4.0 秒**（常量 `.rdata` 直读，见下）。
//   ⚠️ **不是 `_Time` 自走、也不是 `_Color` 脉冲** —— 这一条必须由 C# 驱动（见 `Shell/AvatarTab.cs` 的 `DoShine`）。
//
// ============================ 判据（本机实读，2026-10-31）============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_FX_FX Shine For Animation Normal.shader
//   · 两个 Pass（`LIGHTMODE=Universal2D` / `UniversalForward`），**内容逐字相同**；
//     渲染状态 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha` · `ZClip On` · `ZWrite Off` · `Cull Off`
//     （**没有** `ZTest` ⇒ 默认 `LEqual`，与 `Sprites/Default` 同 ⇒ 换材质不会改遮挡关系）。
//   · 属性表 13 个（含三个 ShaderGraph 生成名 + 三个 `unity_*` 空图）。
// vertex 真源码 : …/Everguild_FX_FX Shine For Animation Normal/Sub0_Pass0_Vertex_b2.hlsl（**b2 档**）
//   `INTERP.w = uv` 直通（`:70-73`，**没有** `TRANSFORM_TEX`，因为 `_MainTex` 是 `[NoScaleOffset]`）·
//   `INTERP_1 = _RendererColor * unity_SpriteColor * COLOR`（`:74-85`）。
// fragment 真源码: …/Sub0_Pass0_Fragment_b8.hlsl（**b8 档** = `_USESCREENSPACE` + `_USETILEMASK`）
//   逐句抄在下面 `frag` 里，每段都标了原行号。
//
// 🔴 **档位判据（⛔ 别再指向 b7）**：材质 `Material_1349812993706917952.json` 的
//   `m_ValidKeywords = [_USESCREENSPACE, _USETILEMASK]` ⇒ 走 **b2 + b8**。
//   b7 是 catch-all（**不含屏幕空间、不含 tile mask**），本材质**走不到**。
//   全库用这颗 shader 的材质**只有 1 份**（`grep '"m_PathID": -2850556267643237983'` 全 assets_full = 1 命中）
//   ⇒ 另外三档（b1 / b3 / b4）**没有任何消费者**，本轮不复刻（与 `WFUIAdditive.shader` 对 b9/b10 的处置同一条口径）。
//
// ============================ 与「两个 Pass」的差（如实记）============================
// 原版两个 Pass 是 URP **Sprite Unlit ShaderGraph 目标**的产物：`Universal2D` 给 2D Renderer、
// `UniversalForward` 给前向 Renderer —— **同一个渲染器只会挑中其中一个**（按 `LightMode`），
// 不是「画两遍」。我们这条走的是**引擎侧 `MeshRenderer` + `SRPDefaultUnlit`**（与 `WFUIPulsating` /
// `WFUIAdditive` 同一条先例）⇒ **只写一个 Pass** 才是等价物；写两个会让 `DrawRenderer` 真画两遍。
//
// ============================ 顶点色的口径（我们与 uGUI 的差异）============================
// 原版 `INTERP_1 = _RendererColor * unity_SpriteColor * COLOR`，其中 `COLOR` = uGUI `Image.m_Color`。
// 我们的 `ImageQuad` **网格顶点色恒白**，染色走**材质 `_Color`**（`ImageQuad.SetTint` 写 `material.color`）
// ⇒ 本 shader 用 `v.color * _Color` 表达同一个乘数（`WFUIAdditive` 同形）。`ImageQuad.SetMaterial`
// **不搬材质色**，所以调用点必须自己 `var keep = quad.Tint; … SetTint(keep);`。
//
// ============================ 屏幕空间的两条注意（都不是「查不到」）============================
// ① 原版用 `_ScaledScreenParams`（URP 的渲染缩放版）；我们走 `UnityCG.cginc` ⇒ 只有 `_ScreenParams`
//    （= 无缩放）。本工程渲染缩放 = 1，两者相等；将来若开渲染缩放，这里要对齐。
// ② `_ProjectionParams.x < 0` 那一支是**逐字照抄**（镜像平台的 y 取向补偿），⛔ 别按 D3D 直觉删掉。
Shader "WarpforgeVFX/UI/FX Shine For Animation Normal"
{
    Properties
    {
        // 原版属性表（`Everguild_FX_FX Shine For Animation Normal.shader:2-15`），名字**逐字照搬**
        // （三个 ShaderGraph 生成名是 GUID 派生名 —— 保留它们，材质 YAML 才能与原版 JSON 逐字段对照）
        [NoScaleOffset] _MainTex ("MainTex", 2D) = "white" {}
        [NoScaleOffset] Texture2D_0bfdc50de139497fa85c0cd08848879a ("Shine Tex", 2D) = "black" {}
        [HDR] Color_a142b0353b4945a49e1990789c04ea35 ("Shine Color", Color) = (1, 1, 1, 0)
        Vector1_c76b369469204ddeb1ee5d707098307c ("Uv Rotation", Float) = 0.93
        _Shine_Scale ("Shine Scale", Vector) = (1, 2.45, 0, 0)
        _Shine_Movement ("Shine Movement", Vector) = (0, 0, 0, 0)
        [Toggle(_USESCREENSPACE)] _USESCREENSPACE ("UseScreenSpace", Float) = 0
        [Toggle(_USETILEMASK)] _USETILEMASK ("UseTileMask", Float) = 0
        _Mask_Size ("Mask Size", Vector) = (1.26, 0.56, 0, 0)
        _NormalGenerator_Offset_Strenght ("NormalGenerator(Offset/Strenght)", Vector) = (0.5, 1, 0, 0)
        // ⚠️ 原版还有三张 `unity_Lightmaps / unity_LightmapsInd / unity_ShadowMasks`（空 2DArray）——
        //    那是 ShaderGraph 给 URP 光照加的占位，本 shader 的算式里**一个都没采** ⇒ 不复刻。
        // 🆕 我们这一项：原版没有 `_Color`，其乘数来自顶点色（见文件头「顶点色的口径」）
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "QUEUE"="Transparent"
            "RenderType"="Transparent"
            "IGNOREPROJECTOR"="true"
            "PreviewType"="Plane"
        }
        Pass
        {
            Name "Sprite Unlit"
            // 原版逐字：RGB 走 `SrcAlpha OneMinusSrcAlpha`，alpha 走 `One OneMinusSrcAlpha`
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZClip On
            ZWrite Off
            Cull Off
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`（同 `WFUIPulsating` / `WFUIAdditive` 先例）。

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            // 原版有这两条关键字（`shaderlab:41-42` 是 multi_compile_local）——这里用 shader_feature，
            // ⇒ 只有真被材质用到的组合才会编；本工程的材质是 `[1, 1]` 那一档。
            #pragma shader_feature_local _ _USESCREENSPACE
            #pragma shader_feature_local _ _USETILEMASK
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D Texture2D_0bfdc50de139497fa85c0cd08848879a;
            float4 Color_a142b0353b4945a49e1990789c04ea35;
            float  Vector1_c76b369469204ddeb1ee5d707098307c;
            float4 _Shine_Scale;
            float4 _Shine_Movement;
            float4 _Mask_Size;
            float4 _NormalGenerator_Offset_Strenght;
            fixed4 _Color;

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
                // Vertex_b2.hlsl:70-73 —— uv 原样直通（`_MainTex` 是 `[NoScaleOffset]`，没有 `_MainTex_ST`）
                o.uv = v.uv;
                // Vertex_b2.hlsl:74-85 —— `INTERP_1 = _RendererColor * unity_SpriteColor * COLOR`
                // （那两个 `unity_*` 是 uGUI/SpriteRenderer 的乘数，我们的等价物 = 材质 `_Color`）
                o.color = v.color * _Color;
                return o;
            }

            // Fragment_b8.hlsl 的两个点积辅助（`:50-60`）。名字加 `wf` 前缀，避免与 URP/UnityCG 的宏撞名。
            float wfDot2 (float2 a, float2 b) { return a.y * b.y + a.x * b.x; }

            // Fragment_b8.hlsl:115-123 —— `_ShineColor` 是 HDR 值，逐分量做 sRGB 编码
            // （原版对每个分量各算一遍：`c <= 0.0031308 ? c*12.923210144 : exp2(log2(abs(c))*(1/2.4))*1.055-0.055`）
            float wfSrgbEncode (float c)
            {
                return (c <= 0.003130800090730190277099609375)
                     ? c * 12.92321014404296875
                     : exp2(log2(abs(c)) * 0.4166666567325592041015625) * 1.05499994754791259765625
                       - 0.054999999701976776123046875;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---- Fragment_b8.hlsl:65-71 —— 底图 + 早退 ----
                float4 tx  = tex2D(_MainTex, i.uv);
                float  baseR = tx.x;                 // `_91`
                float  baseA = tx.w;                 // `_94`
                if (baseA == 0.0) discard;           // `:68-71`（`if (_94 == 0.0f) discard;`）

                // ---- Fragment_b8.hlsl:72-84 —— 屏幕坐标（含 `_ProjectionParams.x` 的镜像补偿）----
                // `_126 = gl_FragCoord.x / _ScaledScreenParams.x` · `_127 = (flip<0 ? H-y : y)/H`
                float2 screenPx = _ScreenParams.xy;
                float  py = (_ProjectionParams.x < 0.0) ? (screenPx.y - i.pos.y) : i.pos.y;
                float2 ctr = float2(i.pos.x / screenPx.x - 0.5, 0.5 - py / screenPx.y);   // `_128` / `_130`

                // ---- Fragment_b8.hlsl:77-84 —— 旋转（Uv Rotation）+ `_Shine_Movement` ----
                float rot = Vector1_c76b369469204ddeb1ee5d707098307c;
                float sn = sin(rot), cs = cos(rot);
                float2 rot1 = float2(cs, sn), rot2 = float2(-sn, cs);                 // `:80-82`
                float  u = wfDot2(ctr, rot1) + _Shine_Movement.x;                     // `_152`
                float  v = wfDot2(ctr, rot2) + _Shine_Movement.y;                     // `_153`
                float2 sm = float2(u + 0.5, v - 0.5);                                 // `_154` / `_155`

                // ---- Fragment_b8.hlsl:85-99 —— 用 `_MainTex` 的亮度差分造法线，扰一下扫光 uv ----
                float off = _NormalGenerator_Offset_Strenght.x;                       // `_160`
                float off3 = off * off * off;                                         // `_161`·`_162`
                float e = off3 * 0.100000001490116119384765625;
                float dx = tex2D(_MainTex, float2(i.uv.x + e, i.uv.y)).x - baseR;     // `_183`
                float dy = tex2D(_MainTex, float2(i.uv.x, i.uv.y + e)).x - baseR;     // `_190`
                float str = _NormalGenerator_Offset_Strenght.y;                       // `_188`
                float3 n = float3(-dx * str, -dy * str, 1.0);                         // `_197`（`_194 = 1.0`）
                float inv = rsqrt(dot(n, n));                                         // `_199`
                float2 shineUV = float2(sm.x * _Shine_Scale.x + n.x * inv,            // `_209`
                                        sm.y * _Shine_Scale.y + n.y * inv);           // `_210`

                // ---- Fragment_b8.hlsl:100-114 —— 取扫光图 + tile mask（屏幕空间矩形带）----
                float4 sh = tex2D(Texture2D_0bfdc50de139497fa85c0cd08848879a, shineUV);   // `_215`
                float mx = abs(shineUV.x * 2.0 - 1.0) - _Mask_Size.x;                 // `_232`
                float my = abs(shineUV.y * 2.0 - 1.0) - _Mask_Size.y;                 // `_233`
                float gx = abs(ddx(mx)) + abs(ddy(mx));                               // `_242`
                float gy = abs(ddx(my)) + abs(ddy(my));                               // `_243`
                float mask = min(saturate(1.0 - mx / gx), saturate(1.0 - my / gy));   // `_246.._250`
                float3 shine = mask * sh.rgb;                                         // `_251.._253`

                // ---- Fragment_b8.hlsl:115-126 —— `_ShineColor` 是 HDR 值，先按 sRGB 编码再叠加 ----
                float3 sc = Color_a142b0353b4945a49e1990789c04ea35.rgb;               // `_261.._263`
                float3 enc = float3(wfSrgbEncode(sc.x), wfSrgbEncode(sc.y), wfSrgbEncode(sc.z));

                // ---- Fragment_b8.hlsl:124-131 —— 输出 = (底图 + 扫光) × 顶点色；alpha = 底图 a × 顶点 a ----
                float3 rgb = (shine * enc + tx.rgb) * i.color.rgb;                    // `_304.._306`
                float  a   = baseA * i.color.w;                                       // `_307`
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
