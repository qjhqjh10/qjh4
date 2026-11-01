// WFUIBorderHighlight.shader —— 原版 `Everguild/UI/UI Border Highlight Appear Pulsating Scale` 的工程内复刻
//
// ============================ 判据（本机实读） ============================
// 唯一判据 = `资料/完全复刻/判据/B114_R5_Reveal_DXBC.md`（原版 shader 的 **DXBC 逐指令还原**）。
//   原件 = `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json`
//   的 `compressedBlob`（128 个 DXBC 段）。该 shader 的 RDEF 段被剥 ⇒ 按资源名查 `cbN[reg]` 恒 0 命中；
//   属性名 ↔ 字节偏移取自 Unity 自己序列化的 `m_CommonParameters`（`m_Index` = 字节偏移）。
//
// 原版属性表（`m_PropInfo` 19 条 = 下面 16 条 + 3 个 `unity_*`；默认值逐条照抄 `m_DefValue`）：
//   `_ColorMain (1, 0.887489, 0, 1)` · `_ColorSecondary (1, 0, 0, 1)` · `_Color_offsets (0.66, 1, 0, 0)` ·
//   `_DEBUG 1` · `_Noise_R "white"` · `_Noise_Strength_X_Speed_Y_Scale_Z (0.01, 1, 1, 0)` ·
//   `_FLIPDIRECTION 0` · `_Scale_X_Speed_Y (0.9, 3, 0, 0)` · `_StartOffset [0,360] = 0` ·
//   `_Reveal [−0.5,1] = 1` · `_Smoothness [0,1] = 0.05` · `_Rotation 0` · `_AUTOROTATION 0` ·
//   `_AutoRotationSpeed 1` · `_QueueOffset 0` · `_QueueControl −1`（后两条 HideInInspector）
//   ⚠️ `_MainTex` **不在** `m_PropInfo` 里，但 `progFragment.m_TextureParams` **按名把 t0/s0 绑给它**
//      ⇒ 原版确实采 `_MainTex`（边框形状来自它的 .a），本文件照此声明。
//
// ============================ 这一版做了什么（2026-11-01 · B-45） ============================
// 把「噪声 + `_Reveal` 揭开」整条链 **照 DXBC 逐指令重写**，不再是自己挑的近似：
//   · 顶点：角度坐标 `ang = atan2(q.y, q.x) * 0.159236`（字面量 = 1/6.28，值域 (−0.5, +0.5]，
//           **不加 0.5**）；`q` = `p` 按 `_StartOffset`（+ `_Rotation`）（+ 自转）**顺时针**转过的坐标。
//           VS-A / VS-C：`p = 屏幕UV − uv0.zw`（`uv0.zw` = 本件视口坐标，原版由 `UIBorderGlow.ModifyMesh` 写、
//           我们由 `WarpforgeVFX.UIViewportUvWriter` 写）；VS-B（`_DEBUG`）：`p = uv0.xy − 0.5`。
//   · 片元：`m = 1 − smoothstep(_Reveal − _Smoothness, _Reveal + _Smoothness, ang)`
//           ⇒ `_Smoothness` 是**绝对半宽**（整圈 = 1.0，软边全宽 = 2S），⛔ 不是「相对柔化比值」。
//   · 颜色链：`_MainTex.r` 经 `_Color_offsets` 两档阈值 `smoothstep` → 在 **LinearToSRGB 过**的
//           `_ColorSecondary` / `_ColorMain` 之间插值；**边框形状来自 `_MainTex.a`**。
//   · 输出：**预乘**（`rgb *= c.a*m`），混合 `SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`（⛔ 不是加色）。
//   · `_StartOffset` 在 **VS** 里参与（与旋转角相加、决定「缝」在哪）；它**不进片元**那 10 条指令
//     —— 旧注释「`_StartOffset` 不进算式」是错的，已就地订正（B114_R5 §五·1）。
//
// ============================ 两处【推断】（非判据，⛔ 别当已证） ============================
// 原版 `UnityPerMaterial` 的 byte 88 / byte 92 两格**没有名字作证**（RDEF 被剥、serde 里 unmapped）：
//   · byte 88（`cb1[5].z`）：VS 里与 `_StartOffset` **相加**后一起度→弧度 ⇒ 唯一像「角度」的候选 = `_Rotation`。
//   · byte 92（`cb1[5].w`）：VS-C 里 `360·frac(t·X)` 的 `X` ⇒ 唯一「速度」候选 = `_AutoRotationSpeed`
//     （唯一走 VS-C 的材质值 `−0.5` 按「圈/秒」正好 2 s 一圈）。
//   ⇒ 本文件按这两个名字实现；它们**不是我们编的数值**（有证据、缺名字），⛔ 不打 `🔶 自己设计`。
//   // 推断（非判据）：cb1[5].z = `_Rotation` · cb1[5].w = `_AutoRotationSpeed`（B114_R5 §三·❓1/❓2）
//
// ============================ `B-273`：裁切两路按原版字节码重写（2026-11-01 · 写手 `W-36`）============================
// 判据 = `判据/B114_R8_BorderHighlight_clip.md`（原版 shader 自己的 **DXBC 逐指令**还原，PS idx 24 / VS idx 0）。
// 原版算式（逐指令，`_ClipRect` = `cb0[131]`）：
//   片元：`add r0.xy, -cb0[131].xyxx, cb0[131].zwzz` … `mul_sat r0.xy, r0.xyxx, v3.zwzz` … `mul o0.w, r0.x, r0.w`
//        ⇒ `m = saturate((_ClipRect.zw - _ClipRect.xy - |o3.xy|) * o3.zw)`，`alpha *= m.x * m.y`。
//   顶点：`clampedRect = clamp(_ClipRect, -2e10, 2e10)`
//        `o3.xy = positionOS.xy*2 - clampedRect.xy - clampedRect.zw`
//        `o3.zw = 0.25 / ( 0.25*_UIMaskSoftness + |pixelSize| )`
//        `pixelSize = 1 / |P00*_ScreenParams.x , P11*_ScreenParams.y|`
// 本文件现在**逐条照上面写**（`vert` 里的 o3 那一段 / `frag` 的 ⑨）。旧版有两处不等价：
//   ① 用 `UnityGet2DClipping`（`step`×2 = 硬边）而不是 `saturate` 线性斜坡；
//   ② 比的是 **世界空间**（`mul(unity_ObjectToWorld, v.vertex).xy`）而不是 `positionOS`。
// 过渡带总宽（对象空间单位）= `1/o3.z = 4*(0.25*S + |pixelSize|) = S + 4|pixelSize|`；
//   Overlay + canvas scale 1 时 `|pixelSize| = 0.5`、`S` 默认 1.0 ⇒ ≈ 3 个对象单位（⚠️ 这条是「套源码+默认值」推的，
//   不是从原版实拍量的 ⇒ **推断，非判据**）。
// ⚠️ `_UIMaskSoftnessX/Y` 到底「是材质常量还是 native 逐帧喂」本机没定案（R8 §五·3）；
//   本文件按**材质属性**声明（我们这一侧只有这条路），默认 1.0 = ShaderGraph 那两条属性的默认值。
//
// ============================ 与原版的已知差异（逐条判过 · 2026-11-01 · W-13 现核） ============================
// ① **原版 4 个 pass，本文件只有 pass0**：pass1 `LIGHTMODE=MOTIONVECTORS` · pass2 `LIGHTMODE=DepthNormalsOnly` ·
//    pass3 `LIGHTMODE=SHADOWCASTER`（`colMask=0` = 不写颜色）。逐条查过消费者，**本工程内 0 消费者**，故不补：
//    `DepthNormalOnlyPass`（URP 17.3.0 `UniversalRenderer.cs:364`）与 `MotionVectorRenderPass`
//    （`MotionVectorRenderPass.cs:35`）都用 `RenderQueueRange.opaque` ⇒ 本 shader 的 `Queue=Transparent` 进不去；
//    ShadowCaster 需要渲染件开阴影投射，而唯一渲染件 `ImageQuad.Create` 把 `shadowCastingMode` 置 Off。
//    反证全表 → `报告/B114_W13_边框shader其余5处.md` §五。
// ② 原版 `Cull Back`（pass0 `culling = 2`）—— 本文件**已照改**。判过不会掉面：本工程 quad 三角形
//    `0,2,1 / 0,3,2`（BL→TR→BR）是**顺时针 = Unity 正面**，与 uGUI `Image` 同向；唯一消费者是
//    `CardDisplayEffects` 的升级边框，父链 `root.localScale = Vector3.one`、无镜像缩放。
// ③ 三个关键字 **`shader_feature_local` 与原版一致**：原版 `m_KeywordFlags`=1（= local，与 `UNITY_UI_CLIP_RECT` /
//    `UNITY_UI_ALPHACLIP` 同档），且编译产物只有 4/8 种组合、恰等于 5 份材质 `m_ValidKeywords` 的并集
//    ⇒ 原版也是**出包会裁变体**的 feature（若原版是 `multi_compile`，8 种会全在）。⛔ 别改成 `multi_compile_local`。
// ④ 原版 `tex2D_bias` 带 `_GlobalMipBias`（URP 全局），内置 CG 用 `tex2D`（同一次采样的隐式 mip）。
// ⑤ 原版屏幕 UV 在片元里由 `SV_POSITION.xy / _ScaledScreenParams` 重算；我们用 VS 的 `ComputeScreenPos`
//    齐次坐标逐像素除 w（平面 quad 下**逐像素同值**），并用内置管线可用的 `_ScreenParams` 代替 URP-only 的
//    `_ScaledScreenParams`。`_TimeParameters.x`（URP）同样用等价的 `_Time.y`（同为「秒」）。
// ⑥ **本文件没有 `_Color` 属性**（原版也没有）。原版的「染色 / α」走**顶点色 `COLOR`**（判据 §2·1 `o2 = v4`、
//    §2·2 ⑧ 末句 = `× v2`）⇒ 调用方必须**先开顶点色通道**（`ImageQuad.SetVertexTint(c)` 或
//    `SetVertexTintChannel(true)`）：`CardDisplayEffects.cs` 的 `im.hasAmp` 那支、
//    `UIBorderGlow.ChangeGraphicAlpha`（2026-11-01 `B-261`）都已改。
//    ⚠️ **通道没开时** `ImageQuad.SetTint` 写的是 `_Color` ⇒ 对这颗 shader **静默落空**（只有一条 Unity 日志）。
// ⑦ `_RendererColor` **原版没有**，本文件旧版带过（`Sprites/Default` 一族残留）、全工程无写点 ⇒ 已删（见报告 §一·④）。
// ⑧ 🔴 **`UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`（原版有、本文件 2026-11-01 `B-262` 补上，`B-273` 订正算式）**：
//    原版 `m_KeywordNames[18]/[19]`、`m_KeywordFlags` 同为 1（local），且编译产物里有 clip / alphaclip 两路。
//    `B-262` 先按 uGUI `UI/Default` 的约定补了两条 `#pragma multi_compile_local` ＋ `clip (o.a - 0.001)`；
//    🔴 `B-273`（写手 `W-36`）按原版 shader **自己的字节码**订正了裁切那一条算式（判据 →
//    `判据/B114_R8_BorderHighlight_clip.md`），**与 `UI/Default` 是双重不等价**：
//      ① **软边 vs 硬边**：原版 = `saturate((边距) * 标度)` 的**线性斜坡**（`mul_sat`，有限宽过渡带）；
//         ⛔ 不是 `UnityUI.cginc` 的 `UnityGet2DClipping`（两条 `step`，0/1 突变、无抗锯齿）。
//      ② **对象空间 vs 世界空间**：原版比的是 **`positionOS`**（`v0.xy`）；旧版比的是
//         `mul(unity_ObjectToWorld, v.vertex).xy`（**世界**）—— 只要画布 `object→world` 不是恒等就根本不等价。
//         ⇒ 谁要喂 `_ClipRect`，必须喂**与 `positionOS` 同空间**的矩形。
//    另补 `_UIMaskSoftnessX/Y` 两个属性（原版有、旧版没有）+ 整条 `pixelSize` 链（在 `vert` 里算、插值下来）。
//    ⚠️ **本工程今天没有数据源 ⇒ 这两路恒不生效**（如实记，⛔ 别当「已复刻成可用的裁切」）：
//      `ImageQuad` 的渲染件是 `MeshRenderer`（没有 `CanvasRenderer` / `RectMask2D`），壳侧裁切走**几何等效**
//      （`MenuDraw.ApplySoftEdges` / `Shell/ViewportClip`）。要真生效得有人 `SetVector("_ClipRect", 对象空间矩形)`
//      + `EnableKeyword("UNITY_UI_CLIP_RECT")`；`_ClipRect` 的默认值是 uGUI 的「不裁」档 ⇒ 没人设时恒不裁。
//    ⚠️ `_ClipRect` / `_UIMaskSoftnessX/Y` 是**我们这一侧新增的材质属性**（原版它们由 uGUI / native 喂进 `$Globals`
//      `cb0[131]` / `cb0[132]`），不是「原版也有这几条材质属性」。
// ⑨ 🔴 **`_AUTOROTATION` / `_FLIPDIRECTION` / `_DEBUG` 三个名字【不声明为 uniform】**（`B114_W50`，2026-11-01）：
//    它们是 `[Toggle(_XXX)]` 的关键字名、又各有一条 `#pragma shader_feature_local _XXX`
//    ⇒ 材质一开那个 Toggle（`MenuFxMaterials.cs` 真会 `EnableKeyword`），Unity 就 `#define _XXX`
//    ⇒ 与同名变量**重定义**、整份编不过（实测 `redefinition of '_AUTOROTATION' at ...shader(202)`）。
//    本文件对它们**只做 `#if defined(...)`、从不读值** ⇒ 按同族做法（`WFEclipseTau.shader:96-101` ·
//    `WFParticlePremultiplyGreyscale.shader:109-111`）**只留 `Properties` 里那三条 `[Toggle]`**。
//    ⚠️ 编译器每 pass 只报**第一个**同名标识符 ⇒ 三颗同病、只报了 `_AUTOROTATION`。
//    ⚠️ `_Rotation` / `_AutoRotationSpeed` **不是**关键字名，必须照旧声明（VS 里要读值）。
//
// 用法：`UIBorderGlow` 把 `_Reveal` 从 −0.5 推到 1（原版 `DoAnimation` 的区间），边框亮带沿角度揭开。
Shader "WarpforgeVFX/UI/UI Border Highlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorMain ("ColorMain", Color) = (1,0.8874891,0,1)
        _ColorSecondary ("ColorSecondary", Color) = (1,0,0,1)
        _Color_offsets ("Color offsets", Vector) = (0.66,1,0,0)
        [Toggle(_DEBUG)] _DEBUG ("Debug", Float) = 1
        _Noise_R ("Noise (R)", 2D) = "white" {}
        _Noise_Strength_X_Speed_Y_Scale_Z ("Noise Strength(X) Speed(Y) Scale (Z)", Vector) = (0.01,1,1,0)
        [Toggle(_FLIPDIRECTION)] _FLIPDIRECTION ("FlipDirection", Float) = 0
        _Scale_X_Speed_Y ("Scale (X) Speed (Y)", Vector) = (0.9,3,0,0)
        _StartOffset ("StartOffset", Range(0,360)) = 0
        _Reveal ("Reveal", Range(-0.5,1)) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 0.05
        _Rotation ("Rotation", Float) = 0
        [Toggle(_AUTOROTATION)] _AUTOROTATION ("AutoRotation", Float) = 0
        _AutoRotationSpeed ("AutoRotationSpeed", Float) = 1
        [HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl ("_QueueControl", Float) = -1
        // 🔴 `B-262`（2026-11-01 · 写手 `W-31`）：uGUI 的裁切矩形。
        //    原版**不是**材质属性 —— 它在 `$Globals`（`cb0[131]`，判据 → `判据/B114_R5_Reveal_DXBC.md` §三·❓6），
        //    由 uGUI 的 `CanvasRenderer` / `RectMask2D` 逐件喂。而我们的渲染件是 `MeshRenderer`（没有 `CanvasRenderer`）
        //    ⇒ 想喂只能走材质属性，所以在这里声明它（这是**我们这一侧的必要差异**，⛔ 不是「原版有这条属性」）。
        //    默认值 = uGUI 的「不裁」档 (-32767,-32767,32767,32767) ⇒ 谁都没设时**恒不裁**（安全默认，⛔ 别改成 0）。
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        // 🔴 `B-273`（2026-11-01 · 写手 `W-36`）：软边裁切的两条柔化宽度（原版属性表里有，我们原来没有）。
        //    判据 = 原版 VS 的 `0.25 / (0.25*_UIMaskSoftness + |pixelSize|)`
        //          （`判据/B114_R8_BorderHighlight_clip.md` §2·2 逐指令 + §2·3 `CanvasPass.hlsl:82` 逐字同形）。
        //    默认 1.0 = ShaderGraph Canvas 目标的 `UIMaskSoftnessX/Y.value`（`CanvasProperties.cs:76-96`，`hidden=true`）
        //    ⇒ 原版 5 份材质都没覆写（`assets_full` 全库 `grep UIMaskSoftness` = 0 命中）⇒ 运行期取 1.0。
        //    推断（非判据）：`cb0[132].xy` 就是这两个槽位（RDEF 被剥、serde 里无名；依据是它在上述算式的**唯一**位置）。
        [HideInInspector] _UIMaskSoftnessX ("Mask Softness X", Float) = 1
        [HideInInspector] _UIMaskSoftnessY ("Mask Softness Y", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane"
               "CanUseSpriteAtlas"="True" }
        Cull Back                    // 原版 pass0 `culling = 2`（判据 §三·14）；网格是顺时针=正面，见文件头差异②
        Lighting Off
        ZWrite Off
        ZTest LEqual                 // 原版 pass0 = `ZTest 4(LEqual)`（本行是显式写出来的默认值）
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha   // 原版 pass0；⛔ 不是加色（旧版写了 `SrcAlpha One`）

        Pass
        {
            Name "UI_BORDER_HIGHLIGHT"
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`、内置管线走默认 pass，两边都渲染得出来。
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            // 原版这三个是 **local + feature** 关键字（`m_KeywordNames` 15=`_DEBUG` / 16=`_AUTOROTATION` /
            // 17=`_FLIPDIRECTION`；`m_KeywordFlags`=1=local，与 `UNITY_UI_CLIP_RECT` 同档；编译产物只有 4/8 种
            // 组合 ⇒ 原版出包也**会裁变体**）⇒ 照原版用 `_local`，⛔ 别改成 `multi_compile_local`。
            #pragma shader_feature_local _DEBUG
            #pragma shader_feature_local _AUTOROTATION
            #pragma shader_feature_local _FLIPDIRECTION
            // 🔴 `B-262`（2026-11-01 · `W-31`）：原版**还有**这两个 local 关键字
            //    （`m_KeywordNames[18]=UNITY_UI_CLIP_RECT` / `[19]=UNITY_UI_ALPHACLIP`，`m_KeywordFlags` 同为 1
            //    —— 与上面三条同档），本文件原来漏了 ⇒ 补上（`B-273` 又按原版字节码订正了算式，见文件头那一节）。
            //    ⚠️ **本工程今天没有任何数据源**，⇒ 这两路**编得出来、但恒不生效**（如实记，⛔ 别当「已复刻」）：
            //      `ImageQuad` 的渲染件是 `MeshRenderer`，没有 `CanvasRenderer` / `RectMask2D`；壳侧的视口裁切走的是
            //      **几何等效**（`MenuDraw.ApplySoftEdges` 切块 + `Shell/ViewportClip`），不是 uGUI 的 `_ClipRect`。
            //    ⇒ 要让它真生效，得有人对这份材质 `SetVector("_ClipRect", **对象空间**(minX,minY,maxX,maxY))`
            //      + `EnableKeyword("UNITY_UI_CLIP_RECT")`；`_ClipRect` 的默认值是「不裁」档 ⇒ 没人设时恒不裁。
            //      🔴 `B-273` 订正：喂的矩形必须与 **`positionOS`** 同空间（原版比的是 `v0.xy`）—— 旧注释写「世界坐标」是错的。
            //    ⛔ 别把 `UNITY_UI_ALPHACLIP` 设成默认打开：原版 5 份材质 `m_ValidKeywords` 里一条都没有它。
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            // ⛔ 不再 `#include "UnityUI.cginc"`（`B-273`）：本机 `UnityUI.cginc:4-8` 的 `UnityGet2DClipping`
            //    是 **两条 `step`**（0/1 突变、无过渡带），而原版用的是 ShaderGraph Canvas 目标那条
            //    **`saturate` 线性软边**（判据 → `判据/B114_R8_BorderHighlight_clip.md` §三）⇒ 两条不同形状的东西，
            //    抄它等于抄错。软边算式在本文件里逐指令写（见 `vert` / `frag` 的 ⑤ 与 ⑨）。

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv0    : TEXCOORD0;   // .xy = 网格 uv；.zw = 极坐标原点（视口坐标，见文件头 VS-A）
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos       : SV_POSITION;
                float4 screenPos : TEXCOORD0;  // `ComputeScreenPos` 的齐次形式；.xy/.w = 屏幕 UV
                float4 uv        : TEXCOORD1;  // .xy = 网格 uv；.zw = 极坐标原点
                float4 color     : COLOR;
                float  ang       : TEXCOORD2;  // 原版 `o5.w`：角度坐标 atan2/6.28 ∈ (−0.5, +0.5]
                // 🔴 `B-273`：原版 `o3`（裁切插值器，**对象空间**，⛔ 不是世界空间）。
                //     .xy = 2*positionOS − clampedRect.xy − clampedRect.zw（= 相对裁切矩形中心的 2 倍距离）
                //     .zw = 0.25 / (0.25*_UIMaskSoftness + |pixelSize|)（= 线性斜坡的标度 = 1/过渡带宽度）
                float4 clipRect  : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _Noise_R;
            float4 _ColorMain, _ColorSecondary, _Color_offsets, _Noise_Strength_X_Speed_Y_Scale_Z, _Scale_X_Speed_Y;
            float _Reveal, _Smoothness, _StartOffset;
            float _Rotation, _AutoRotationSpeed, _QueueOffset, _QueueControl;
            // 🔴 **这里【不能】声明 `float _AUTOROTATION, _FLIPDIRECTION, _DEBUG;`**（`B114_W50`，2026-11-01）：
            //    它们是 `[Toggle(_XXX)]` 的关键字名、又各有一条 `#pragma shader_feature_local _XXX`；
            //    材质一开那个 Toggle（`MenuFxMaterials.cs:262-270 / 438-448 / 499-506` 真会 `EnableKeyword`）
            //    ⇒ Unity 就 `#define _AUTOROTATION` ⇒ 与同名变量**重定义**、整份编不过。
            //    实测报错原文：`redefinition of '_AUTOROTATION' at WFUIBorderHighlight.shader(202)`
            //    （`_tmp_view/b114_unity_shellscene.log` 第 1003 / 181400 行附近）。
            //    ⚠️ 编译器每 pass 只报**第一个**同名标识符 ⇒ 三颗同病、只报了 `_AUTOROTATION`。
            //    本文件对它们**只做 `#if defined(...)`、从不读值**（`vert` 里的两处 + `frag` 里的 `_FLIPDIRECTION`）
            //    ⇒ 按同族做法只留 `Properties` 里那三条 `[Toggle]`（材质面板要它，`SetFloat` 也照旧能写）。
            //    同族先例：`WFEclipseTau.shader:96-101`（`_APPLYAMBIENTCOLOR`）·
            //              `WFParticlePremultiplyGreyscale.shader:109-111`（`_SOFTPARTICLES`）。
            float4 _ClipRect;        // 🔴 `B-262`：uGUI 的裁切矩形 (minX,minY,maxX,maxY)；
                                     //    ⚠️ `B-273` 起判据把它定为**与 `positionOS` 同空间**（原版比的是 `v0.xy`），
                                     //    ⛔ 别再按「世界坐标」喂它（空间不对 ⇒ 软边公式抄对了照样错）。
            float _UIMaskSoftnessX, _UIMaskSoftnessY;   // 🔴 `B-273`：软边宽度（默认 1.0；见 Properties 注释）

            // 原版片元指令 `log2*0.4166667 → exp2 → *1.055 − 0.055`；`≤0.0031308` 时改走 `*12.92321`（逐分量 movc）。
            // 0.4166667 = 1/2.4 · 12.92321 = 12.92（原版字面量打印到 6 位）。
            float3 WFUILinearToSrgb (float3 c)
            {
                c = max(c, 1e-6);                                  // 防 log2(0) = −inf；这两个分支下取值与 0 同
                float3 lo = c * 12.92321;
                float3 hi = exp2(log2(c) * 0.4166667) * 1.055 - 0.055;
                return (c <= 0.0031308) ? lo : hi;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.uv        = v.uv0;
                o.color     = v.color;                               // 原版 `o2 = v4`（⛔ 不乘 `_RendererColor`：原版没有这颗属性）

                // ── 🔴 `B-273`：裁切插值器（原版 `o3`）。逐指令照 `判据/B114_R8_BorderHighlight_clip.md` §2·2。
                //    VS **无条件**算它（原版 16 段 VS 里 `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP` 完全不影响 VS ⇒ §1·2）。
                //    空间 = **`positionOS`（v.vertex）**，⛔ 不是世界空间 —— 原版 `mad r1.xy, v0.xyxx, l(2,2), -r1.xyxx`。
                // 推断（非判据）：`pixelSize = 1/|P00*_ScreenParams.x, P11*_ScreenParams.y|`。
                //    DXBC 侧是 `mul r1.z, cb0[23].x, cb0[62].x` / `mul r1.w, cb0[23].y, cb0[63].y` 再 `div l(1,1), |·|`，
                //    形状与 `CanvasPass.hlsl:78-82` 的 `mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy)` 逐字同形；
                //    正交投影下 P00/P11 就在对角线上 ⇒ 取对角线等价（R8 §五·2：RDEF 被剥，本机拿不到名字作证）。
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 pixelSize   = 1.0 / abs(float2(UNITY_MATRIX_P[0][0] * _ScreenParams.x,
                                                      UNITY_MATRIX_P[1][1] * _ScreenParams.y));
                o.clipRect = float4(v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw,
                                    0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));

                // ── 极坐标原点 + 角度（原版 VS 三种分支，由关键字选）
                float2 sp = o.screenPos.xy / o.screenPos.w;          // 屏幕 UV（与 `sp − uv0.zw` 同一个空间）
                float2 p;
                float  angDeg;
                #if defined(_DEBUG)
                    // VS-B：极坐标原点换成网格 uv 中心；`_Rotation` / 自转在这一支里被编译掉
                    p      = v.uv0.xy - 0.5;
                    angDeg = _StartOffset;
                #else
                    // VS-A / VS-C：极坐标原点 = 视口坐标（`uv0.zw`）
                    p = sp - v.uv0.zw;
                    #if defined(_AUTOROTATION)
                        // VS-C：`360·frac(t · 自转)` 与 `_StartOffset` 相加（t = 秒 ⇒ 量纲 = 圈/秒）
                        // 推断（非判据）：byte 92 = `_AutoRotationSpeed`（B114_R5 §三·❓2）—— 没有名字作证
                        angDeg = _StartOffset + 360.0 * frac(_Time.y * _AutoRotationSpeed);
                    #else
                        // 推断（非判据）：byte 88 = `_Rotation`（B114_R5 §三·❓1）—— 没有名字作证
                        angDeg = _StartOffset + _Rotation;           // VS-A
                    #endif
                #endif

                // ── 度 → 弧度，再**顺时针**旋转 p（矩阵 [[cos, sin], [−sin, cos]]），取 atan2
                float rad = radians(angDeg);
                float sn, cs;
                sincos(rad, sn, cs);
                float2 q = float2(p.x * cs + p.y * sn, -p.x * sn + p.y * cs);
                o.ang = atan2(q.y, q.x) * 0.159236;                  // 原版字面量 = 1/6.28（⛔ 不是 1/(2π)）
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // ① 屏幕 UV（等价实现：原版在片元里由 SV_POSITION 重算，见文件头差异⑤）
                float2 sp = i.screenPos.xy / i.screenPos.w;

                // ② 噪声 UV：屏幕 UV 先做 (x, 1−y) 翻转、按 .z 缩放、居中，再绕中心转 frac(t·.y) 圈
                float2 np = float2(sp.x, 1.0 - sp.y) * _Noise_Strength_X_Speed_Y_Scale_Z.z - 0.5;
                float  na = frac(_Time.y * _Noise_Strength_X_Speed_Y_Scale_Z.y) * 6.283185;
                float  nsn, ncs;
                sincos(na, nsn, ncs);
                float2 nuv = float2(np.x * ncs + np.y * nsn, -np.x * nsn + np.y * ncs) + 0.5;

                // ③ 噪声（只取 .r）→ 映射到 −1..1
                float nz = tex2D(_Noise_R, nuv).r * 2.0 - 1.0;

                // ④ Pulsating Scale：按中心缩放主纹理 uv（.x = **缩放下限**，.y = sin 的弧度/秒）
                float  puls  = 0.5 * (1.0 - _Scale_X_Speed_Y.x) * (sin(_Time.y * _Scale_X_Speed_Y.y) + 1.0)
                             + _Scale_X_Speed_Y.x;
                float2 uv2   = puls * (i.uv.xy - 0.5) + 0.5;
                uv2 += nz * _Noise_Strength_X_Speed_Y_Scale_Z.x;     // 同一个标量加到 **x 和 y 两个分量**（各向同性）

                // ⑤ 主纹理（边框形状 = 它的 .a）
                float4 c = tex2D(_MainTex, uv2);

                // ⑥ 双色渐变：由 sprite 的**红通道**按 `_Color_offsets` 两档阈值切换（两色先 Linear→sRGB）
                float k = saturate((c.r - _Color_offsets.x) / (_Color_offsets.y - _Color_offsets.x));
                k = k * k * (3.0 - 2.0 * k);                         // smoothstep01
                float3 col = lerp(WFUILinearToSrgb(_ColorSecondary.rgb), WFUILinearToSrgb(_ColorMain.rgb), k);

                // ⑦ ★ `_Reveal` 的链：`m = 1 − smoothstep(R−S, R+S, ang)`（S = 绝对半宽）
                float ang = i.ang;
                #if defined(_FLIPDIRECTION)
                    ang = -ang;                                      // 原版变体：把每个 `v5.w` 换成 `−v5.w`
                #endif
                float S = _Smoothness;
                float m = 1.0 - smoothstep(_Reveal - S, _Reveal + S, ang);

                // ⑧ 输出（★ 预乘：alpha 同时乘进 rgb 和 a）
                float4 o;
                o.rgb = col * c.a * m * i.color.rgb;
                o.a   =       c.a * m * i.color.a;

                // ⑨ 🔴 `B-262` 补的两路裁切 + `B-273` 的算式订正。
                //    `B-273` 改了两处（判据 → `判据/B114_R8_BorderHighlight_clip.md` §二/§三/§四）：
                //      ① **软边**：原版是 `mul_sat`（= `saturate` 线性斜坡、有限宽过渡带），
                //         ⛔ 不是 `UnityGet2DClipping` 的 `step`×2（0/1 突变、无抗锯齿）。
                //      ② **空间**：原版比的是 **`positionOS`**（`v0.xy`，插值器 `i.clipRect.xy` 由它算出），
                //         ⛔ 不是世界空间 —— 这条更隐蔽：软边公式抄对了、空间不对照样错。
                //    顺序仍与 uGUI `UI/Default` 一致：先乘裁切、再 alpha clip（原版 alphaclip 档也是这个序）。
                //    ⚠️ 本工程**没有任何人设 `_ClipRect` / 开这两个关键字** ⇒ 今天这两路是**惰性**的（如实记）。
                //       `_ClipRect` 默认值是「不裁」档 ⇒ 就算被打开、没人喂也恒不裁（宽度远大于任何 |2·posOS|）。
                #ifdef UNITY_UI_CLIP_RECT
                    half2 mk = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.clipRect.xy)) * i.clipRect.zw);
                    o.a *= mk.x * mk.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip (o.a - 0.001);
                #endif
                return o;
            }
            ENDCG
        }
    }
    Fallback Off
}
