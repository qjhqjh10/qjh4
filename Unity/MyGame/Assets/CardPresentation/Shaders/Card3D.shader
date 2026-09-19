// Card3D.shader — 原版**场上那张 3D 卡体**的材质（`Card 3d Lvl1` 的等价版）
//
// 为什么自己写：原版那张是 Shader Graph（`Universal Unlit`），**图源不在解包资源里**，
// 而工程里那份材质被降级成了 `URP/Unlit`（`_CardImage`/`_MatCap` 全丢）⇒ 贴不上立绘。
// 但 **PS/VS 的编译字节码在**，2026-09-19 逐行反汇编出来，本 shader 就是照它 1:1 转写的。
// 正本 = `资料/3DBody_原版场上卡体规格.md`（§三「缺口 1」那一节有属性对照与 fragment 骨架）。
//
// 🔴 **两条与原版一致、别改坏的**：
//   ① **立绘吃 mesh 的第二套 UV（UV1）** —— 根本没有「UV 重映射」这回事；
//      外面只套一个**编译期写死**的区间 mask `0.18 < UV1.x ≤ 0.82 && UV1.y > 0`（DXBC 立即数 `l(0.82,1,0.18,0)`）。
//      mesh 正面四角 UV1 = (0.1818,0.0017)/(0.8203,0.0017)/(0.1833,0.9978)/(0.8188,0.9978)。
//   ② **不乘顶点色** —— 原版的 ISGN 里**没有 COLOR**；而 mesh 的顶点色在**滚边上全是 0**
//      ⇒ 一旦乘上去，卡片底部的滚边会**整个变黑**（不是「颜色不对」，是直接没了一块）。
//
// ⚠️ **一处查不到**：原版 PS 里 matcap 之后还乘了一个全局向量（`mul r0.xyz, r0.xyzx, cb0[56].xyzx`）——
//    DXBC 被剥了 RDEF、`$Globals` 里只列 `_GlobalMipBias` / `unity_AmbientSky` ⇒ **名字查不到**。
//    这里**当 1 处理**（只影响 matcap 的亮度/色调，不影响形状与立绘）。
//
// ⚠️ `_Color` 是**我们这边多出来的**（原版没有）：`CardView.SetTint/SetAlpha` 靠它做高亮与淡出。
//    它只乘最终颜色，不参与上面那条「不乘顶点色」的规矩。
Shader "CardPresentation/Card3D"
{
    Properties
    {
        _BaseMap ("底板图集（UV0）", 2D) = "white" {}
        _CardImage ("立绘（UV1）", 2D) = "white" {}
        _MatCap ("MatCap", 2D) = "white" {}
        _MatCap_Intensity ("MatCap 强度", Float) = 1.69
        _MatCapPower ("MatCap 幂", Float) = 1.24
        _CountersIntensity ("计数器面板强度", Float) = 1.12
        _Color ("整卡着色 / 透明度", Color) = (1,1,1,1)
    }

    SubShader
    {
        // 透明队列（我们整套卡面都在透明队列里按 z 排序）· 写深度：网格是立体的，
        // 不写深度的话背面会透出来；写深度之后正面的数字/徽标（z 更靠前）照样盖得住。
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Back
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0    : TEXCOORD0;
                float2 uv1    : TEXCOORD1;      // ← 立绘那一套（原版的「重映射」就在这儿）
                float4 uv2    : TEXCOORD2;      // ← .x = 「这块走平涂 + 计数器增强」的开关
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv0      : TEXCOORD0;
                float2 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                float2 matcapUv : TEXCOORD3;
            };

            sampler2D _BaseMap;
            sampler2D _CardImage;
            sampler2D _MatCap;
            float _MatCap_Intensity;
            float _MatCapPower;
            float _CountersIntensity;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                o.uv2 = v.uv2;
                // matcap 的取法：**视图空间法线**的 xy 当 UV（「从正面看一个球」那张图）
                float3 n = UnityObjectToWorldNormal(v.normal);
                float3 vn = mul((float3x3)UNITY_MATRIX_V, n);
                o.matcapUv = vn.xy * 0.5 + 0.5;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ① 底板（UV0）+ 立绘（UV1）
                float3 baseCol = tex2D(_BaseMap, i.uv0).rgb;
                // 🔴 **我们这边必须补的一步**（原版没有）：原版 `_CardImage` 灌的是 **1024² 图集整图**
                //    （`rawCard.cardSprite.texture`），而 UV1 那一块**正好框住 sprite 的子矩形**
                //    （正面四角 0.1818–0.8203 × 0.0017–0.9978）⇒ 图集采样恰好对上。
                //    **我们工程里的立绘是已经按 textureRect 裁好的图**（`Resources/Art/cards/art_*.png`），
                //    再把 UV1 当 0..1 用就会**二次裁切** —— 实测症状是「角色被放大、脸占满整张卡」。
                //    ⇒ 这里用「UV1 那块的角点」反算回 0..1。角点出处 = `资料/3DBody_原版场上卡体规格.md` §一。
                const float2 ArtUvMin = float2(0.1818, 0.0017);
                const float2 ArtUvMax = float2(0.8203, 0.9978);
                float2 artUv = (i.uv1 - ArtUvMin) / (ArtUvMax - ArtUvMin);
                float4 art   = tex2D(_CardImage, artUv);
                art.rgb *= tex2D(_BaseMap, i.uv1).a;

                // ② matcap 假光照（原版：先乘强度、再取幂）
                float3 cap = tex2D(_MatCap, i.matcapUv).rgb * _MatCap_Intensity;
                cap = pow(max(cap, 0), _MatCapPower);

                // ③ uv2.x = 1 的那块（正面下部计数器面板）：走平涂 + `_CountersIntensity`，不吃 matcap
                float3 col = lerp(cap * baseCol,
                                  baseCol * i.uv2.x * _CountersIntensity,
                                  i.uv2.x);

                // ④ 立绘可见区：**编译期写死的区间**（原版 DXBC 立即数 `l(0.82,1,0.18,0)`）。
                //    背面（0.015–0.128）与滚边（0.836–0.989）故意落在区间外 ⇒ 自动回落到底板
                float m = (i.uv1.x > 0.18 && i.uv1.x <= 0.82 && i.uv1.y > 0) ? 1 : 0;
                col = saturate(col * (1 - m) + art.rgb * m);

                return fixed4(col * _Color.rgb, _Color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
