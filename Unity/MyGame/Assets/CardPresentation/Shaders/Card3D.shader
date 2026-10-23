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
        // 🆕 2026-10-19（`A2139`）：原版去饱和通道（判据 → `Resources/Shaders/CardSprite.shader` 头部）。
        // 0 = 原色（默认，画面不变）/ 1 = 全灰。场上 3D 卡体也是「卡面」的一部分。
        _GreyScale ("Greyscale", Range(0,1)) = 0
        // 🔴 2026-10-23（D4 · F1 Step 3）：**只给 `UsePass` 进来的 `ShadowCaster` 用** ——
        //   URP 的 `Simple Lit` 那个 pass 写的是 `Cull [_Cull]`，而卡体材质里原本没有 `_Cull`
        //   ⇒ 不声明的话它读 0（= Cull Off，双面进阴影）。原版 `Card 3d Lvl1` 是 `cull 2`（Back）。
        //   ⚠️ 主 pass 仍然**硬编码** `Cull Back`，不受本属性影响。
        _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        // 🔴 2026-10-23（D4 · F1 Step 2）—— **卡体回到原版的「不透明」档**。
        //   判据（原始 bundle 逐字段实读，全文 → `资料/普查产出_1023/D3_F1判据与方案.md`）：
        //   原版材质 `Card 3d Lvl1` 的 shader `Everguild/Cards/3D Card`：
        //   子标签 `QUEUE=Geometry` / `RenderType=Opaque`；pass 状态 `blend (1,0)=One/Zero`、
        //   `zWrite 1`、`zTest 4`（LEqual）、`cull 2`（Back）。
        //   ⛔ 我们原来那套 `Transparent + SrcAlpha/OneMinusSrcAlpha` 是**我们自己排的**，不是原版。
        //
        //   改成不透明之后：
        //     · 卡体与场地装饰 / 其它卡体之间的先后由**深度**决定（不再吃「一张卡一段队列」）；
        //     · 卡面的透明层（框/立绘/数值/徽标）仍在透明队列，照旧按队列段排；
        //     · `_Color.a` **不再影响画面**（输出的 alpha 固定 1）⇒ 卡体的「淡出」必须走
        //       **溶解材质**（`Everguild/Cards/3D Card Dissolve`，AlphaTest 2450），
        //       2D 卡面那一档走 `CardView.SetAlpha2D`。⛔ 别为了淡出把它改回 Transparent。
        //
        //   ⚠️ **`renderQueue` 的排序优先级高于 shader 标签** ⇒ `CardView.SetQueue` / `StampQueue`
        //   必须**跳过 `_body3D`**（已同批改，`CardView.cs` 的 `StampQueue` 里有判据注释）——
        //   否则标签会被卡队列段（场上 2962–2979）盖掉、这次改动等于没做。
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Back
        ZWrite On
        Blend One Zero

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
            float _GreyScale;

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

                // ⑤ 🆕 2026-10-19（`A2139`）：去饱和（原版 `_GreyScale` 语义；同 `CardSprite.shader`）。
                //    放在 `_Color` 之前 —— 整卡淡出/着色照旧，只把颜色去饱和。
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, lum.xxx, saturate(_GreyScale));

                // 🔴 2026-10-23（D4）：不透明档下 **alpha 固定写 1**（`Blend One Zero` ⇒ 它本来就不参与混合，
                //   写 `_Color.a` 只会把 <1 的值带进 framebuffer alpha，对后处理是隐患）。
                //   `_Color.rgb` 仍然是整卡着色（高亮/状态色）的入口，**保持不变**。
                return fixed4(col * _Color.rgb, 1.0);
            }
            ENDCG
        }

        // 🔴 2026-10-23（D4 · F1 Step 3）：补齐原版那套 **pass**。
        //   判据 = 原版 `Everguild/Cards/3D Card` 的 `SerializedPass` 逐个实读，**5 个 pass**：
        //     `Universal Forward` / `DepthOnly` / `MotionVectors` / `DepthNormalsOnly` / **`ShadowCaster`**。
        //   我们原来只有 Forward + `Fallback Off` ⇒ ① 卡体**进不了阴影图**（`MeshRenderer` 的
        //   `castShadows=On` 等于空转 —— 上一批 F3 改的就是它）；② 卡体不在 URP 的
        //   `_CameraDepthTexture` / `_CameraNormalsTexture` 里。
        //
        //   ⇒ **直接复用 URP 自己的 pass**（`UsePass`）。名字在本工程 URP **17.3.0** 的
        //     `Shaders/Unlit.shader`（`:148` `DepthOnly` · `:189` `DepthNormalsOnly`）与
        //     `Shaders/SimpleLit.shader`（`:152` `ShadowCaster`）里逐个核对过。
        //   ⛔ **不手写**：`DepthNormals` 要做八面体编码 / `_GBUFFER_NORMALS_OCT` 分支，手写编不过
        //     就是满屏品红，而这一轮**跑不了 Unity**（验收主对话做）。
        //   · `DepthOnly` / `DepthNormalsOnly` ← `Universal Render Pipeline/Unlit`（Unlit **没有** ShadowCaster）
        //   · `ShadowCaster` ← `Universal Render Pipeline/Simple Lit`（它写 `Cull [_Cull]`
        //     ⇒ 靠上面 Properties 里那句 `_Cull = 2` 保持 `Cull Back`，与原版 `cull 2` 同值）
        //   · `MotionVectors` **没加**：我们没有开运动矢量/TAA 的需求（原版有，那是它管线的选择）。
        //   ⚠️ 这三个 pass 与我们主 pass 的顶点变换互不相干（各编各的）；它们不带 `_Color` 着色 ——
        //     深度/法线/阴影本来就不吃颜色。
        UsePass "Universal Render Pipeline/Unlit/DepthOnly"
        UsePass "Universal Render Pipeline/Unlit/DepthNormalsOnly"
        UsePass "Universal Render Pipeline/Simple Lit/ShadowCaster"
    }
    Fallback Off
}
