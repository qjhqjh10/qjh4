// WFUIAdditive.shader —— 原版 `Everguild/UI/Additive` 的**逐指令复刻**（菜单 13 材质 · #7）
//
// ============================ 判据（本机实读，2026-10-31 首版 / 2026-11-01 `B-272` 补齐 b9·b10）============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Additive.shader
//   · `Blend One One, One One` · `ColorMask [_ColorMask]` · `ZClip On` · `ZTest [unity_GUIZTestMode]`
//   · `ZWrite Off` · `Cull Off` · 完整 `Stencil` 块 · Tags（CanUseSpriteAtlas / IGNOREPROJECTOR / QUEUE=Transparent）
//   · 属性表 9 个：`_MainTex _Color _StencilComp _Stencil _StencilOp _StencilWriteMask _StencilReadMask
//                   _ColorMask [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip`
// 原版资产       : d:/2/新解包资源/assets_full/bundle_menus_assets_all/Shader/Shader_4921318991789134610.json
//                  `m_ParsedForm.m_Name = "Everguild/UI/Additive"`；
//                  `m_KeywordNames` 末两条 = `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`，`m_KeywordFlags` 都是 **1 = local**
//                  ⇒ 原版这两条是 **`multi_compile_local`**（不是 feature：四档编译产物 b7/b8/b9/b10 **全在包里**
//                     ⇒ `shader_feature_local` 会把没人用的档裁掉，与原版不符）。
// vertex 真源码 : …/Everguild_UI_Additive/Sub0_Pass0_Vertex_b1.hlsl:61-108（**四份 VS 逐字节相同**，只有首行 Keywords 注释不同）
//                  `COLOR_1 = COLOR * _Color`（4 道 mul，:80-87）；`uv = TRANSFORM_TEX(TEXCOORD, _MainTex_ST)`（:88-89）
//                  🔴 还有一段 uGUI 裁切矩形插值器 `TEXCOORD_2`（:94-107）—— **四档 VS 全部无条件算它**
//                     ⇒ 本文件也在 `vert` 里无条件算（⛔ 别包进 `#ifdef UNITY_UI_CLIP_RECT`）。
// fragment 真源码: …/Sub0_Pass0_Fragment_b7.hlsl:29-49（catch-all 变体）
//                  `_34 = round(COLOR.w*255)/255`；`_82 = _34 * tex.w`；
//                  `out.rgb = _82 * COLOR.rgb * tex.rgb`；`out.a = _82`
// ALPHACLIP 变体 : …/Sub0_Pass0_Fragment_b8.hlsl:28-54 —— 多一句 `if (mad(_34, _71, -0.001) < 0) discard;`
// CLIP_RECT 变体 : …/Sub0_Pass0_Fragment_b9.hlsl:32-60 —— 软边 mask（见下「四档对照」）
// 两档齐全变体   : …/Sub0_Pass0_Fragment_b10.hlsl:32-64 —— b9 的 mask + b8 的 discard，**discard 判的是【乘完 mask】的 alpha**
// 材质（原版）   : d:/2/新解包资源/assets_full/bundle_menus_assets_all/Material/Material_-8464978790139598553.json
//                  `m_Name = "UI Additive"` · `_Color = (1,1,1,1)` · `_MainTex` 无图（运行期由 Image 喂）
//                  · `m_ValidKeywords = []`（⇒ 原版这颗材质走 **b7**；b9/b10 在菜单侧**零消费者**，见文件末）
//                  🔴 那个 JSON 里另有 `_ColorMain 337.79…` / `_Reveal 0.723` / `_Noise_Strength_*` 等
//                     —— 它们是**上一颗 shader 的残留**（本 shader 的 `m_PropInfo` 里没有这些名字）⇒ ⛔ 别抄。
//
// ============================ 四档变体（原版 `:44-45` 两条 pragma + `:48-58`/`:62-73` 四分支 + 四个编译产物，2026-11-01 `B-272` 逐档还原）============================
//   档位 | 关键字                        | 片元（各自的 .hlsl）              | 本文件对应代码
//   ---- | ----------------------------- | --------------------------------- | ---------------
//   b7   | （无）catch-all               | Fragment_b7                       | `frag` 主体
//   b8   | UNITY_UI_ALPHACLIP            | Fragment_b8（+ discard）          | `frag` 的 `#ifdef UNITY_UI_ALPHACLIP`
//   b9   | UNITY_UI_CLIP_RECT            | Fragment_b9（软边 mask）          | `frag` 的 `#ifdef UNITY_UI_CLIP_RECT`
//   b10  | 两条都要                      | Fragment_b10（mask 后再 discard） | 两段都在 ⇒ 自然合成（顺序见下）
//
//   🔴 **pragma 顺序按 Additive 自己**：ALPHACLIP **在前**、CLIP_RECT **在后**（原版 `:44` → `:45`）；
//      ⛔ 别照抄 `WFUIBorderHighlight.shader`（那边是 CLIP_RECT 先）—— 两族原版**顺序相反**。
//   🔴 **软边 mask 算式（b9 `:34-40` / b10 `:45-51`，逐指令照抄）**：
//        `w = _ClipRect.z - _ClipRect.x` · `h = _ClipRect.w - _ClipRect.y`
//        `mask = clamp((h - abs(TEXCOORD_2.y)) * TEXCOORD_2.w, 0, 1)      // 先乘 Y 那一半（原版 `_62`）
//              * clamp((w - abs(TEXCOORD_2.x)) * TEXCOORD_2.z, 0, 1)`     // 再乘 X（原版 `_61`）
//        `alpha *= mask`（b10：`_118 = _83 * _115`，discard 判的是 `_118 - 0.001`）
//      ⚠️ 与 BorderHighlight **算式同形、写法不同**（那边写成一整条 `saturate(...)`）；本轮**不抽公共 include**：
//         两族原版的**住处/空间/默认值/像素中心口径都不一样**（见下「与 BorderHighlight 的三处不同」），
//         抽公共件会把「各自照自己的原版」变成「凑一个两边都不完全对的近似」。
//   🔴 **裁切插值器（b1 `:94-107`，四档 VS 共用）**：
//        `pixelSize = gl_Position.w / abs( P00*SP.x + P10*SP.y , P01*SP.x + P11*SP.y )`
//        `TEXCOORD_2.zw = 0.25 / (0.25*_UIMaskSoftnessXY + abs(pixelSize))`
//        `TEXCOORD_2.xy = positionOS.xy * 2 − clamp(_ClipRect.xy, ±2e10) − clamp(_ClipRect.zw, ±2e10)`
//      ⇒ 空间 = **`positionOS`（对象空间）**，⛔ 不是世界空间（原版 `mad(POSITION.x, 2, -clamp(rect.x))`）。
//
// ============================ 与 `WFUIBorderHighlight.shader` 的三处不同（⛔ 别为了「统一」磨平）============================
//   ① **pragma 顺序**：本条 ALPHACLIP→CLIP_RECT（原版 `:44-45`）；BorderHighlight 是 CLIP_RECT→ALPHACLIP。
//   ② **`_ClipRect` 住处**：本条在原版 `type_Globals` 的 **c4**（与本 shader 自己的 `_Color` c2 / `_MainTex_ST` c5 同块）；
//      BorderHighlight 在 `$Globals cb0[131]`。⇒ 两边**各自的 packoffset 不同**，默认值/坐标空间要各自核。
//   ③ **`pixelSize` 首项**：本条原版是 **`gl_Position.w`**（b1 `:98`，`div r1.x, _158, |…|`）；
//      BorderHighlight 原版是**字面量 `1.0`**。⇒ ⛔ 别把 1.0 那版搬过来（正交 UI 下数值相同，但这是【形状】证据）。
//
// ============================ `_ClipRect` / `_UIMaskSoftnessX/Y` 的来历（⛔ 别当成「原版也有这三条材质属性」）============================
//   原版它们**不在 `m_PropInfo`**（属性表只有 9 条）—— 它们在 `type_Globals` cbuffer 里，由 **uGUI `CanvasRenderer` / native**
//   逐件喂（与我们无 `CanvasRenderer` 的 `ImageQuad` 是两条路）。本文件**只能**声明成材质属性 —— 这是**我们这一侧的必要差异**。
//   ✅ **名字有证据（不是推断）**：`compressedBlob` 解压后的名字表里逐字含
//      `_ClipRect` / `_UIMaskSoftnessX` / `_UIMaskSoftnessY`（`_tmp_view/b114_w42/verify_blob.py` 现跑；
//      同时含 `_ScreenParams` / `glstate_matrix_projection` / `unity_MatrixVP` / `_TextureSampleAdd` / `_MainTex_ST`）。
//   // 推断（非判据）：三条**默认值** —— `_ClipRect = (-32767,-32767,32767,32767)` 取自 uGUI 的「不裁」档；
//   //   `_UIMaskSoftnessX/Y = 1.0` 取自 ShaderGraph Canvas 目标 `CanvasProperties.cs` 那条属性的默认值。
//   //   原版字节码里读不到默认值（由 native 喂），只读得到**算式**。默认 ±32767 远大于任何 |2·posOS| ⇒ 恒不裁。
//   // 推断（非判据）：过渡带绝对宽度 = `_UIMaskSoftness + 4*|pixelSize|` 个对象单位（正交 + canvas scale 1 时 ≈ 3）——
//   //   这条是「套源码 + 默认值」推的，⛔ 没有从原版实拍量过。
//
// ============================ 名字口径（⛔ 别改成 "…/UI/UI Additive"）============================
// 原版 = `Everguild/UI/Additive`（**没有**第二个 `UI `）。同族的 `Everguild/UI/Pulsating` 我们落成
// `WarpforgeVFX/UI/UI Pulsating` 是因为**原版那个名字本身**带 `UI`（`Everguild/UI/UI Clock` 同族）。
// 本条按原版名照搬 ⇒ **`WarpforgeVFX/UI/Additive`**（`B110_D4` 已经在 `Shell/ParentEventWindow.cs` 用
// `Shader.Find("WarpforgeVFX/UI/Additive")` 兜底取它，改名字会让那条路静默失效）。
//
// ============================ 与 `WarpforgeVFX/Sprites/Additive` 的分工 ============================
// 那张是**精灵**加法（`Everguild/Sprites/Sprite Additive`，带 `_RendererColor` / `_Flip` / PixelSnap /
// ETC1 外部 alpha），给**战场特效**用；本条是**uGUI Image** 加法（stencil / ColorMask / 顶点色里已乘 `_Color`）。
// 两者算式的**公共部分**相同（都预乘 alpha），但属性表不同、cbuffer 来源不同 ⇒ **不要合并**。
//
// ============================ 消费者现状（如实记 · 2026-11-01）============================
// 今天**没有任何人**喂 `_ClipRect`、也没有人 `EnableKeyword("UNITY_UI_CLIP_RECT")`
// （`ImageQuad` 的渲染件是 `MeshRenderer`，没有 `CanvasRenderer` / `RectMask2D`；壳侧视口裁切走**几何等效**
//  `MenuDraw.Clip*` / `Shell/ViewportClip`）⇒ b9/b10 这两档**编得出来、但运行期恒不生效**。
// 按 `B-262` 的口径（原版声明了 + 编译产物有 ⇒ 我们就要有，⛔ 不看今天有没有消费者）补齐；
// 「惰性」只决定**先后**，不决定**做不做**（铁律 11 / 18·0）。
// `_ClipRect` 默认是「不裁」档 ⇒ 就算将来谁把关键字打开、忘了喂，也**恒不裁**（⛔ 别把默认值改成 0：那会整块消失）。
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
        // 🔴 `B-272`（2026-11-01 · 写手 `W-42`）：uGUI 的裁切矩形 —— 原版在 `type_Globals` **c4**（不是材质属性），
        //    由 uGUI / native 喂；我们没有 `CanvasRenderer` ⇒ 只能声明成材质属性（**我们这一侧的必要差异**）。
        //    默认 = uGUI 的「不裁」档 ⇒ 谁都没设时**恒不裁**（安全默认，⛔ 别改成 0）。
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
        // 🔴 `B-272`：软边裁切的两条柔化宽度（原版 `type_Globals` c6.x / c6.y，b1 `:100-101`）。
        //    默认 1.0 = ShaderGraph Canvas 目标的 `UIMaskSoftnessX/Y.value`（推断，见文件头）。
        [HideInInspector] _UIMaskSoftnessX ("Mask Softness X", Float) = 1
        [HideInInspector] _UIMaskSoftnessY ("Mask Softness Y", Float) = 1
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
            // 🔴 `B-272`：**顺序照 Additive 自己**——原版 `:44` ALPHACLIP 在前、`:45` CLIP_RECT 在后
            //    （⛔ 别照抄 `WFUIBorderHighlight.shader`，那边是反的）。两条都是 `local`（原版 `m_KeywordFlags`=1）。
            //    原版是 `multi_compile_local`（不是 feature）：四档编译产物 b7/b8/b9/b10 全在包里 ⇒ 照抄 multi_compile。
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _TextureSampleAdd;
            // 🔴 `B-272`：原版在 `type_Globals` c4 / c6（native 喂）；我们只能当材质属性声明（见 Properties 注释）。
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                // 🔴 `B-272`：原版 `TEXCOORD_2`（b1 `:57`）—— 裁切插值器，**对象空间**。
                //     .xy = 2*positionOS − clampedRect.xy − clampedRect.zw（= 相对裁切矩形中心的 2 倍距离）
                //     .zw = 0.25 / (0.25*_UIMaskSoftness + |pixelSize|)（= 线性斜坡标度 = 1/过渡带宽度）
                float4 clipRect : TEXCOORD1;
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

                // ── 🔴 `B-272`：裁切插值器（原版 `TEXCOORD_2`）。**四档 VS 逐字节相同 ⇒ 无条件算**，⛔ 别包 `#ifdef`。
                //    逐指令照 `Sub0_Pass0_Vertex_b1.hlsl:94-107`。
                //    空间 = **`positionOS`（v.vertex）**：原版 `mad(POSITION.x, 2.0, -clamp(rect.x)) - clamp(rect.z)`。
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                // 原版 `:98-99` = `mad(P00, SP.x, SP.y*P10)` / `mad(P01, SP.x, SP.y*P11)` 再 `div _158, |·|`，
                // 其中 `_158` = `gl_Position.w`（`:75/:79`）。`P` = `glstate_matrix_projection`（= `UNITY_MATRIX_P`，
                // `UnityShaderVariables.cginc:27` 逐字同义）⇒ 名字与算式都有证据，不是推断。
                float2 scr = _ScreenParams.xy;
                float2 proj2x2 = float2(UNITY_MATRIX_P[0][0] * scr.x + scr.y * UNITY_MATRIX_P[1][0],
                                        UNITY_MATRIX_P[0][1] * scr.x + scr.y * UNITY_MATRIX_P[1][1]);
                float2 pixelSize = o.pos.w / abs(proj2x2);
                o.clipRect = float4(v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw,
                                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Fragment_b7.hlsl:31-41 —— `_34 = round(COLOR.w*255)/255`；`_82 = _34 * tex.a`
                float  a255 = round(i.color.w * 255.0) * 0.003921568859368563;
                float4 tx   = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float  outA = a255 * tx.a;

                // ── 🔴 `B-272`：b9/b10 的**软边 mask**（原版 `Fragment_b9.hlsl:34-40` / `b10:45-51`，逐指令）。
                //    `mask = clamp((h - |t.y|) * t.w, 0, 1) * clamp((w - |t.x|) * t.z, 0, 1)`
                //    ⛔ 不是 `UnityGet2DClipping`（那是 `step`×2 的**硬边**、无过渡带）；
                //    ⛔ 也不是「比世界空间」（`t` 是对象空间的 `positionOS` 推出来的）。
                #ifdef UNITY_UI_CLIP_RECT
                    float mW = clamp(((_ClipRect.z - _ClipRect.x) - abs(i.clipRect.x)) * i.clipRect.z, 0.0, 1.0);
                    float mH = clamp(((_ClipRect.w - _ClipRect.y) - abs(i.clipRect.y)) * i.clipRect.w, 0.0, 1.0);
                    outA *= mH * mW;
                #endif

                // Fragment_b8.hlsl:40-43（ALPHACLIP 档）—— `if (mad(_34, _71, -0.001) < 0) discard;`
                // b10（两档齐全）的 discard 判的是**乘完 mask 的** alpha（原版 `mad(_83, _115, -0.001)`）
                // ⇒ 这一段必须排在 mask 之后（b8 里 mask 恒 1，两者同值）。
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
