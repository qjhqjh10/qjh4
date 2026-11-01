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
//   ⇒ 另外三档（b1 / b3 / b4）**没有任何消费者**。
// 🔴 **那三档到底该不该补 —— 结论：不需要补（`B-286`，2026-11-01 现核）**：
//   四份顶点反编译产物 `…/Sub0_Pass{0,1}_Vertex_b{1..4}.hlsl` **两两 `diff` 只差第 1-2 行注释**
//   （`// Blob:` 序号与 `// Keywords:` 那行：`<none>` / `_USESCREENSPACE _USETILEMASK` /
//   `SKINNED_SPRITE` / 后两者叠加）；**函数体一字不差**。两个 Pass 之间那 6 份文件也逐字节相同。
//   ⇒ ShaderGraph 只是按「关键字组合」复制了四份产物，**顶点算式一档都不差**；
//   区分 b3/b4 的 `SKINNED_SPRITE` 在这颗 shader 里**没有产生任何不同指令**，
//   而且我们走 `MeshRenderer` + `ImageQuad`，那条关键字本来也不会被定义。
//   我们这条 `vert` 就是那唯一一份体的复刻 ⇒ **四档同时覆盖**，⛔ 别再为「补齐 b1/b3/b4」加分支。
//   （口径：原文那句「本轮不复刻（与 `WFUIAdditive` 对 b9/b10 同一条口径）」是「惰性 ⇒ 不做」的
//    **已作废说法**；这里换掉它是**因为有上面那条逐字节反证**，⛔ 不是因为「影响小」。）
// ✅ **片元的两个档都已实现**（`B115_W22`，2026-11-01 —— 它补的是原来缺的 b7，见 `B-304`）：
//   `frag` 里那条 `#if defined(_USESCREENSPACE) && defined(_USETILEMASK)` **就是原版 shaderlab `:61-66` 的判据**
//   （原版 `:61` `#if defined(_USESCREENSPACE) && defined(_USETILEMASK)` → b8；`:63-66` `#else` / `// Catch-all.` → b7）
//   ⇒ **两个关键字都开才走 b8，其余一律 catch-all b7**（只开一个也走 b7，原版如此）。
//   两档的差别**只有两处**（逐行核过；⚠️ 原文件头曾把行号写成 `:87-95` / `:100-114`，那是 **b8 自己的**行号，已订正）：
//   ① 旋转中心 —— b7 `Sub0_Pass0_Fragment_b7.hlsl:66-67` 用 **`uv - 0.5`**（`_93 = INTERP.x - 0.5f` / `_95 = INTERP.y - 0.5f`，
//      **y 不取反**）；b8 `:72-76` 用屏幕坐标 `gl_FragCoord / _ScaledScreenParams` 且 y 是 `0.5 - y/H`（**取反过**）。
//   ② tile mask —— b7 **整段没有**（`:91` 取完扫光图，`:92` 直接进 `_ShineColor` 的 sRGB 编码；
//      而且 b7 的 `UnityPerMaterial` cbuffer `:11-22` 里**根本没有 `_Mask_Size`**）；b8 `:103-114` 才有 `_Mask_Size` + `ddx/ddy` 软边。
//   其余（底图早退、`off³×0.1` 亮度差分造法线、旋转 + `_Shine_Movement`、sRGB 编码、`(shine*enc + tx.rgb) * color` 的尾巴、
//   alpha = `底图 a × 顶点色 a`）两档**逐句相同**（唯一差别是 SSA 变量编号）。
//   **变体面（`B115_W22` 2026-11-01 改）**：pragma 已换成原版 `shaderlab:41-42` 的 `multi_compile_local` ⇒ **4 档无条件全编**
//      ⇒ **从今天起 b7 这条分支真的会被编译器看到**（原来 `shader_feature` 按材质用量剥离、唯一材质是 `[1,1]` ⇒ 它从没编过）。
//      ⚠️ 运行时选哪一档**不变**：材质的两个关键字都开（`m_ValidKeywords = [_USESCREENSPACE, _USETILEMASK]`）⇒ 仍走 b8。
//      （原版还有第三条 `multi_compile_local _ SKINNED_SPRITE`，我们**故意不加**：那两档顶点体与 b2 逐字节相同，见上 `B-286`。）
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
        // 🔶 自己设计（非原版）：`_Color` 这个**载体**——原版没有它（顶点色乘数来自 `_RendererColor * unity_SpriteColor * COLOR`，`Vertex_b2.hlsl:74-85`），我们的 `ImageQuad` 网格顶点色恒白、染色走材质色 ⇒ 用 `v.color * _Color` 表达同一个乘数（`WFUIAdditive` 同形）· 为什么必须自定：原版那三个乘数在 uGUI 侧，我们走 `MeshRenderer + ImageQuad` 没有它们，只能换传递通道（**乘数语义不变**）· `B110_S1` 2026-10-31 落地，`B115_W22` 2026-11-01 补标
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
            // 原版 `shaderlab:41-42` **逐字**：`#pragma multi_compile_local _ _USESCREENSPACE` / `… _ _USETILEMASK`
            // ⇒ **4 档无条件全编**（**为什么必须换**：`shader_feature` 按材质用量剥离 ⇒ 今天唯一材质是 `[1,1]`，b7 永远进不了编译器）
            #pragma multi_compile_local _ _USESCREENSPACE
            #pragma multi_compile_local _ _USETILEMASK
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
                // ---- 底图 + 早退（b7 `:59-65` / b8 `:65-71`，两档逐句相同）----
                float4 tx  = tex2D(_MainTex, i.uv);
                float  baseR = tx.x;                 // b8 `_91` / b7 `_85`
                float  baseA = tx.w;                 // b8 `_94` / b7 `_88`
                if (baseA == 0.0) discard;           // `:68-71`（`if (_94 == 0.0f) discard;`）

                // ---- 档位分派 —— 上面那条 `#if` 是原版 shaderlab `:61-66` 的判据逐字照搬 ----
                //   `#if defined(_USESCREENSPACE) && defined(_USETILEMASK)` → b8（`:62`）
                //   `#else` → b7 catch-all（`:63-65`）。⚠️ **只开一个关键字也走 b7**（原版就写着 `// Catch-all.`）
                float2 ctr;
#if defined(_USESCREENSPACE) && defined(_USETILEMASK)
                // ---- b8 · `Fragment_b8.hlsl:72-76` —— 屏幕坐标（含 `_ProjectionParams.x` 的镜像补偿）----
                // `_126 = gl_FragCoord.x / _ScaledScreenParams.x` · `_127 = (flip<0 ? H-y : y)/H`
                float2 screenPx = _ScreenParams.xy;
                float  py = (_ProjectionParams.x < 0.0) ? (screenPx.y - i.pos.y) : i.pos.y;
                ctr = float2(i.pos.x / screenPx.x - 0.5, 0.5 - py / screenPx.y);   // `_128` / `_130`
#else
                // ---- b7（catch-all）· `Fragment_b7.hlsl:66-67` —— 旋转中心 = **`uv - 0.5`** ----
                //   `_93 = INTERP.x - 0.5f` · `_95 = INTERP.y - 0.5f` · `_104 = float2(_93, _95)`
                // ⚠️ 与 b8 的差别**不只是「换个坐标系」**：b8 的 y 是 `0.5 - y/H`（**取反过**），
                //    b7 直接 `uv.y - 0.5`（**不取反**）—— 这是 uv 与屏幕 y 取向相反导致的，⛔ 别「统一」掉。
                ctr = float2(i.uv.x - 0.5, i.uv.y - 0.5);
#endif

                // ---- 旋转（Uv Rotation）+ `_Shine_Movement` —— b7 `:68-75` / b8 `:77-84`，两档逐句相同 ----
                float rot = Vector1_c76b369469204ddeb1ee5d707098307c;
                float sn = sin(rot), cs = cos(rot);
                float2 rot1 = float2(cs, sn), rot2 = float2(-sn, cs);                 // `:80-82`
                float  u = wfDot2(ctr, rot1) + _Shine_Movement.x;                     // `_152`
                float  v = wfDot2(ctr, rot2) + _Shine_Movement.y;                     // `_153`
                float2 sm = float2(u + 0.5, v - 0.5);                                 // `_154` / `_155`

                // ---- 用 `_MainTex` 的亮度差分造法线，扰一下扫光 uv —— b7 `:76-91` / b8 `:85-99`，两档逐句相同 ----
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

                // ---- 取扫光图（b7 `:91` / b8 `:102` —— 同一条 sample、同一位置，都在 sRGB 编码之前）----
                // b8 `_215` = `Texture2D_… .SampleBias(sampler, float2(mad(_154,_Shine_Scale.x,_200), mad(_155,_Shine_Scale.y,_201)))`
                float4 sh = tex2D(Texture2D_0bfdc50de139497fa85c0cd08848879a, shineUV);

#if defined(_USESCREENSPACE) && defined(_USETILEMASK)
                // ---- b8 · `Fragment_b8.hlsl:103-114` —— tile mask（屏幕空间矩形带 + `ddx/ddy` 软边）----
                float mx = abs(shineUV.x * 2.0 - 1.0) - _Mask_Size.x;                 // `_232`
                float my = abs(shineUV.y * 2.0 - 1.0) - _Mask_Size.y;                 // `_233`
                float gx = abs(ddx(mx)) + abs(ddy(mx));                               // `_242`
                float gy = abs(ddx(my)) + abs(ddy(my));                               // `_243`
                float mask = min(saturate(1.0 - mx / gx), saturate(1.0 - my / gy));   // `_246.._250`
                float3 shine = mask * sh.rgb;                                         // `_251.._253`
#else
                // ---- b7（catch-all）—— **没有 tile mask** ——
                // 原版 b7 在 `:91` 取完扫光图之后，`:92` 第一句就是 `_196 = Color_…x`（sRGB 编码），
                // 中间**没有** `_Mask_Size` / `ddx` / `ddy` 任何一段；b7 的 cbuffer（`:11-22`）里也没有 `_Mask_Size`
                // ⇒ 它**不是「mask 恒等于 1」那种写法**，而是**整段不存在** ⇒ 这里同样整段不写。
                float3 shine = sh.rgb;
#endif

                // ---- `_ShineColor` 是 HDR 值，先按 sRGB 编码再叠加 —— b7 `:92-100` / b8 `:115-123`，两档逐句相同 ----
                float3 sc = Color_a142b0353b4945a49e1990789c04ea35.rgb;               // `_261.._263`
                float3 enc = float3(wfSrgbEncode(sc.x), wfSrgbEncode(sc.y), wfSrgbEncode(sc.z));

                // ---- 输出 = (底图 + 扫光) × 顶点色；alpha = 底图 a × 顶点 a —— b7 `:101-108` / b8 `:124-131`，两档逐句相同 ----
                float3 rgb = (shine * enc + tx.rgb) * i.color.rgb;                    // `_304.._306`
                float  a   = baseA * i.color.w;                                       // `_307`
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
