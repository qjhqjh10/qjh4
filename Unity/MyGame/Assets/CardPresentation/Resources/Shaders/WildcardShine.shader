// WildcardShine.shader —— 原版 Coffee `UIEffect` 的**过渡/色调/边缘**一族（`TransitionFilter.Shiny = 4` 等）。
//
// 用途：账 `收藏:348`（`WildcardDisplay.OnEnable` → `shinyEffect.Play(resetTime:true)`）——
//   收藏窗 `CardsTab ▸ Header Filters ▸ WIldcard Display ▸ Background` 上那道扫光。
//   （那颗 `Background` 上总共挂了 4 个组件：`Image`（Sliced · 图 `40k_topmarquee_currency_display_BW` ·
//   色 `(0.462,0.462,0.462,1)`）+ **`UIEffect`** + **`UIEffectTweener`** + `CanvasRenderer` —— 2026-10-27 现读
//   `bundle_menus_assets_all` 的 `GameObject/…5282456591777456853`。）
//
// ============================ 判据从哪来（全是直读，⛔ 不是照截图猜）============================
//  ① 原版 shader = `Hidden/UI/Default (UIEffect)`（`globalgamemanagers` 的 `Shader_77`，`m_ParsedForm.m_Name`）。
//     它的 **`TRANSITION_SHINY` 关键字变体**的像素级实现 = `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/
//     Assets/Shader/Hidden_UI_Default (UIEffect)_0/Sub0_Pass0_Fragment_b16.hlsl`（ruri 反编译出的 SPIR-V → HLSL，
//     747 行）。下面每条式子都标了它原文里的变量名（`_433` / `_501` / `_510` …），可以逐条回去核。
//     🆕 **其余变体的判据同理**：同目录 `Sub0_Pass0_Fragment_b13..b32.hlsl` + `Sub0_Pass0_Vertex_b1..b4.hlsl`，
//     **每份文件第 2 行就写着它自己的 `// Keywords:`**（= 关键字组合的唯一判据）。
//  ② 这个实例的参数（`GameObject …5282456591777456853` 上的 `UIEffect` MB）：
//     `m_TransitionFilter 4 (Shiny)` · `m_TransitionRotation 120` · `m_TransitionWidth 0.25` ·
//     `m_TransitionSoftness 0.5` · `m_TransitionRate 1.0` · `m_TransitionReverse 0` ·
//     `m_TransitionColor (1,1,1,1)` · `m_TransitionColorFilter 6` · `m_TransitionColorGlow 0` ·
//     `m_TransitionKeepAspectRatio 1` · `m_TransitionTexScale/Offset/Speed` 全默认 ·
//     `m_TransitionAutoPlaySpeed 0` · `m_BlendType 1 / m_SrcBlendMode 1 / m_DstBlendMode 10`（= `Blend One OneMinusSrcAlpha`）；
//     驱动它的 `UIEffectTweener`：`m_Duration **0.75**` · `m_Delay 0` · `m_WrapMode 0`（播一次）· `m_ResetTimeOnEnable 1`。
//     🔴 **与账里那张 W64 表的 139 / 0.213 / 0.533 / 1.0 s / (2,2,2,1) 不是同一颗** —— 那一套是
//     `Deck Editing Menu ▸ WIldcard Counter ▸ Background` 那个实例（铁律 5·c：一个值 ≠ 全部情况）。
//     收藏窗这一颗是 **120 / 0.25 / 0.5 / 0.75 s / 白**（本轮现读，见 `CollectionWindow` 的 `WcShine*` 常量）。
//  ③ `m_TransitionTex`（外链 `externals[14]` = `CAB-34ccb919…`）= **`Transition-Horizontal`**
//     （256×256 · `TextureFormat 1 = Alpha8`）。逐像素实测（UnityPy 取 `image`，y 方向每 32 采样）：
//     `(0,0,0,254) (0,0,0,223) (0,0,0,190) (0,0,0,159) (0,0,0,127) (0,0,0,95) (0,0,0,63) (0,0,0,31)`，
//     横向 8 个采样全同 ⇒ **沿 y 的严格线性斜坡**（`alpha = (256 - y) / 256`，8 bit 量化）。
//     ⇒ 本 shader 在**没有这张贴图**时用解析式 `1 - saturate(uv.y)` 复现它 —— **这是逐像素等价的推算式、
//       不是「近似」**（贴在 `_TransitionTex` 上时照样走贴图，见 `_UseTransitionTex`）。
//  ④ **掩码用的是「本件框内的归一化坐标」，不是顶点 uv**（2026-10-27 `B9_W16SHINE` 现读 Coffee 的 IL）：
//     原版那串是 `unity_ObjectToWorld → _CanvasToWorldMatrix → _RootViewMatrix`（见 `b16.hlsl:103-130`），
//     而 `_RootViewMatrix` 的来源 = `UIEffectContext.UpdateViewMatrix(material, transitionRoot, canvas)`；
//     `transitionRoot` 的定义在 `Coffee.UIEffects/UIEffect.cs` 的 `override RectTransform transitionRoot`
//     = **`m_CustomRoot ?? 自己的 transform`**，而 5 个实例的 `m_CustomRoot` **全是 null**
//     （判据 = `报告/B9_W5COF_Coffee搬运.md` §2·6 那张逐字段表）⇒ **坐标空间 = 这一件自己的 Rect**。
//     ⇒ 本 shader 在 `vert` 里取顶点世界位置、减框心、绕框心转 θ、再按 `1/max(W,H)` 归一。
//     🔴 **这样九宫格的 9 块可以共用一份材质**（掩码按世界位置连续，天然与「原版一颗 Image 一张网格」同形）
//        —— 上一路打算给每块算一份 uv 仿射，是**没必要的**（而且一旦有裁切/软边重切就会失效）。
//     ⚠️ **θ 之后那一步的精确算式仍是重构的**（标 `🔶 自己设计`，见 `Effects/UIEffectTogglerCoffeeEffects.cs`
//        的 `WildcardShineLayer` 类注释）：Coffee 的 `UpdateViewMatrix` 方法体在 Cpp2IL 重建的 dll 里
//        **局部变量全被打散**（`ilspycmd -il` 里只剩 `IL_xxxx` 与 `NoteDecompilerIssue`）⇒ 拿不到逐句原文。
//        能坐实的只有三件：① 坐标空间 = 自己的 Rect（上面那条）；② 绕 `rect.pivot` 那一带转、且把
//        `1/W`、`1/H` 两个比例与一个 `m_TransitionKeepAspectRatio` 分支放在**旋转之前/之后**相乘；
//        ③ 结果进 `_TransitionTex` 之前被 `clamp(0,1)`（`b16.hlsl:130`）。
//        我们取「像素空间旋转 + 统一 1/max(W,H)」—— 只有这样，框才整块落在 [0,1] 内（另两种取法会把
//        半个框 clamp 成死白，见 `WildcardShineLayer` 类注释里的对照）。
//
// ============================ 原版到底有几个变体（判据 = 编译产物，⛔ 不是按关键字猜）============================
//   原版 `Hidden_UI_Default (UIEffect).shader` 的 YAML（`ruri_out_1020_rootshaders` 里那份）逐字是：
//     `#pragma multi_compile_local _ UNITY_UI_ALPHACLIP`
//     `#pragma multi_compile_local _ UNITY_UI_CLIP_RECT`
//     `#pragma multi_compile_local _ TONE_GRAYSCALE`
//     `#pragma multi_compile_local _ TRANSITION_DISSOLVE`
//     `#pragma multi_compile_local _ TRANSITION_SHINY`
//     `#pragma multi_compile_local _ EDGE_SHINY`
//   ⇒ 6 个二态关键字 = 名义 64 组合；但**构建产物里只有 20 个 fragment blob + 4 个 vertex blob**
//     （`Sub0_Pass0_Fragment_b13..b32` / `Sub0_Pass0_Vertex_b1..b4`），因为 shader 自己是一串
//     **精确匹配**的 `#if/#elif`：**只有「四个效果关键字里恰有一个」×「两个 UI 关键字任意」= 5×4 = 20** 命中，
//     其余一律落 `b13`（基础档，**什么效果都不做**）。逐份文件的 `// Keywords:` 已核，表如下：
//
//   | blob | 关键字组合 | 体的实质 |
//   |---|---|---|
//   | **b13** | `<none>` | **基础档**：采样 → `+_TextureSampleAdd` → 预乘 → **uv1 矩形掩码** → 顶点 α 量化 |
//   | **b14/b19/b24/b29** | `TONE_GRAYSCALE`（＋ALPHACLIP／CLIP_RECT／两者） | 基础 **＋ Tone（亮度加权去色）＋ `_ColorFilter` ＋ Shadow** |
//   | **b15/b20/b25/b30** | `TRANSITION_DISSOLVE`（＋同上） | 基础 **＋ 溶解过渡 ＋ `_TransitionColorFilter` ＋ Shadow** |
//   | **b16/b21/b26/b31** | `TRANSITION_SHINY`（＋同上） | 基础 **＋ 扫光过渡 ＋ `_TransitionColorFilter` ＋ Shadow** |
//   | **b17/b22/b27/b32** | `EDGE_SHINY`（＋同上） | 基础 **＋ 边缘检测 ＋ `_ColorFilter` ＋ `_EdgeColorFilter` ＋ 扫边光 ＋ Shadow** |
//   | **b18/b23/b28** | `ALPHACLIP` ／ `CLIP_RECT` ／ 两者 | **基础档**的裁剪修饰（无效果关键字） |
//   | **b1/b2/b3/b4**（vertex） | 4 种 | **体完全相同**（`diff` 只差第 1–2 行注释）⇒ 只有一份体 |
//
//   ✅ **「只差注释」的判定不是推测**：`diff <(grep -v '^//' b1) <(grep -v '^//' b2/b3/b4)` **三对全部 IDENTICAL**。
//   ⚠️ **同一效果的四个裁剪档之间不是「只差注释」**：ALPHACLIP 多一句 `discard`、CLIP_RECT 多一段
//     `_ClipRect` 软化裁切（判据 = `diff b18 b28`：多出 `_137.._161` 那 15 行与顶点 `TEXCOORD_2` 一路）。
//   ⚠️ 其余字节差异**大量是 SSA 变量重新编号**（同一段算式换个变体就从 `_433` 变成 `_411`），
//     ⇒ **不能拿整份 `diff` 行数判「体不一样」**，要看**算式与常量**（本文件每条都标了原版变量名可回核）。
//
// ============================ 本文件实现了哪些档（如实记）============================
//   ✅ **已实现**（全部四个效果 + 两个 UI 关键字）：
//     · `TRANSITION_SHINY` —— `b16` 在「uv1 = (0,0,1,1) 且无 Shadow」那一路的全部分支
//       （`_TransitionColorFilter` **0..8 全九档** · `_TransitionColorGlow` 0/1 · `_TransitionReverse` 0/1）
//     · `TRANSITION_DISSOLVE` —— `b15` 同口径：`_448 = saturate(2·band/softW)` 一路**额外乘进 rgb 与 a**
//       （原版 `_513.._516 = _448 * _509.._512`），其余与 SHINY 共用
//     · `TONE_GRAYSCALE` —— `b14` 的 Tone 那一段：`rgb = lerp(rgb, dot(rgb, lumaWeight), _ToneIntensity)`
//       （原版 `_155` = 加权和；`_172 = mad(_171, mad(-_146,_137,_155), _150)`）
//     · `EDGE_SHINY` —— `b17` 的 12 向 30° 采样边缘（`_417.._630`）＋ 边缘色 ＋ 扫边光（`_913.._974`）
//     · `UNITY_UI_ALPHACLIP` —— `b18` 的 `if (α·vAlpha < 0.001) discard;`
//     · `UNITY_UI_CLIP_RECT` —— `b23` 的顶点 `mask` ＋ 像素软裁切（`_137.._161`）
//   ✅ **三族 ColorFilter 已补齐 0..8 九档**（`B115_W22`，2026-11-01；0 = 不染）：
//     · `_ColorFilter`（`b14.hlsl:316-513`；b15 `:522` / b16 `:523` / b17 `:444` 同形，**4 个效果档都有**）
//       · `_TransitionColorFilter`（`b16.hlsl:179-306`，＝ b15 `:176-306` 逐字节同）· `_EdgeColorFilter`（`b17.hlsl:643-829`）
//     · 🔴 **三族算式互不相同 ⇒ ⛔ 别合并成一个函数**：(A) 基础 / (C) 边缘 = 「α 调制族」（乘 `C.a`，且再乘一次源 α）；
//       (B) 过渡 = 「换色族」（**不乘 `C.a`、也不乘 α**，例 4 直接 = `C.rgb`）。注意 (A) 与 (C) 的**公式逐条相同**，
//       差的是颜色参数与输入（(C) 的输入已是 (A)+intensity 之后的值）—— 仍各留一份，见那 3 个 `wfColorFilter*`。
//     · 顺带补上 `_ColorIntensity` / `_ColorGlow`（原版 `_282.._298` 那一跳）：现役实例取默认值 **1 / 0** ⇒ **恒等，观感不变**。
//   ⛔ **仍未实现（与本批「变体补齐」无关的、原本就缺的共用子特性）**：
//     · **Shadow 族**（`_ShadowColorFilter/_ShadowColor/_ShadowColorGlow/_ShadowIteration/_ShadowFade/…`）
//     · **Gradation / Target / Detail / Sampling** 那几族
//     · **uv1 矩形掩码**（原版每个变体都拿 `TEXCOORD1` 当「sprite 的 uv 矩形」做盒测；本工程没有那一路数据通道
//       ⇒ 等价于原版 `uv1 = (0,0,1,1)` 那一档，**逐项等价**：`_382 = uv.x>=0 = 1`、`_393 = 1>=uv.x = 1`、
//       且 `_287 = 0<0 = false` ⇒ 不进 Shadow 支；本文件所有式子都是照这一档写的）
//     ⚠️ 上面这几条都是**已记账的缺口**（见 `报告/B115_W12_WildcardShine变体.md` §五「顺手发现」），
//       ⛔ **不是「影响小所以不做」**（铁律 11）。
//
// ============================ 两个必须知道的工程口径 ============================
//  ⚠️ **① `multi_compile` 一定会把变体编进去，与该实例开没开那个关键字无关**（本文件 2026-11-01 就地订正）：
//     旧注释写「该实例 keywords 全 None ⇒ 这两个变体不会被编进去」—— **对 `#pragma multi_compile_local` 不成立**。
//     `multi_compile` = **无条件编全部档**（这就是它能保证运行时随便切的原因）；只有 `shader_feature`
//     才会按材质实际用到与否被剥离。所以「没实现」与「不会被编进去」是两件事 —— 原版这 20 档**一直都在包里**。
//  ✅ **② 关键字由材质显式点亮（2026-11-01 收口，兼容档已删）**：原版「四个效果关键字一个都不开」= **基础档 = 什么效果都不做**
//     ⇒ 本文件的 `#if` 链严格照这个语义写，**没有任何兜底**。谁用哪一档，谁在材质上 `EnableKeyword`：
//     `Effects/UIEffectTogglerCoffeeEffects.cs` 的 `WildcardShineLayer.Attach` 里有一行
//     `mat.EnableKeyword("TRANSITION_SHINY");`（判据：那 5 个实例 `m_TransitionFilter` 全是 4 = Shiny，见 `B9_W5COF` §2·6）。
//     ⛔ **删掉那一行 = 材质落回基础档 = 扫光静默消失**（那是原版语义，不是这些实例的语义）。
//
// ⚠️ **加载**：`Shader.Find("CardPresentation/WildcardShine")`。它不在 `Resources/` 下 ⇒ player 包里要靠
//   `ProjectSettings/GraphicsSettings.asset` 的 AlwaysIncluded（那份工程设置不在本件所有权内 ⇒ 未改）。
//   取不到时调用方必须**出声并跳过**（`CollectionWindow` 就是这么做的），⛔ 不许静默给一张白图。
Shader "CardPresentation/WildcardShine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ---- 原版 `UIEffect` 的 Transition 族（名字逐字照抄，⛔ 别改名）----
        _TransitionRate ("Transition Rate", Range(0,1)) = 1
        _TransitionWidth ("Transition Width", Range(0,1)) = 0.25
        _TransitionSoftness ("Transition Softness", Range(0,1)) = 0.5
        _TransitionColor ("Transition Color", Color) = (1,1,1,1)
        _TransitionColorFilter ("Transition Color Filter", Float) = 6
        _TransitionColorGlow ("Transition Color Glow", Float) = 0
        _TransitionReverse ("Transition Reverse", Float) = 0
        _TransitionAutoPlaySpeed ("Transition Auto Play Speed", Float) = 0

        // ---- 掩码贴图（原版 `_TransitionTex` / `_TransitionTex_ST` / `_TransitionTex_Speed`）----
        _TransitionTex ("Transition Texture", 2D) = "white" {}
        _TransitionTex_ST ("Transition Texture ST", Vector) = (1,1,0,0)
        _UseTransitionTex ("Use Transition Texture", Float) = 0

        // ---- 原版 `TONE_GRAYSCALE` 档（`b14`）：`_ToneIntensity` 在 c15 ----
        //   原版没有 `_ToneFilter` 这个 uniform（`b14` 的 cbuffer 里 c15 只映射到 `_ToneIntensity`）⇒ 去色方式恒为亮度加权
        _ToneIntensity ("Tone Intensity", Range(0,1)) = 0

        // ---- 原版基础 `_ColorFilter` 族（`b14:11/15/16` · `b15:11/15/16` · `b16:11/15/16` · `b17:12/16/17`）----
        //   **4 个效果档都有它**：`_ColorFilter`(c15.y) + `_ColorValue`(c16) + `_ColorIntensity`(c17) + `_ColorGlow`(c17.y)。
        //   ⚠️ 原版 `Properties` 里**没有**这 4 条（Coffee 的 C# 用 `Material.SetFloat/SetColor` 按名字绑 cbuffer）。
        //   我们照 Unity 常规写法声明出来，只为 `new Material(sh)` 时有**确定的默认值**：
        //   `_ColorFilter = 0`（= 不染）· `_ColorIntensity = 1` · `_ColorGlow = 0` ⇒ **现役 5 个实例全落在「恒等」档**
        //   （材质是 `new Material(sh)` 造的、没人 SetFloat 这 4 个名字 —— 判据 `Effects/UIEffectTogglerCoffeeEffects.cs` 的 `WildcardShineLayer.Attach`）。
        _ColorFilter ("Color Filter", Float) = 0
        _ColorValue ("Color", Color) = (1,1,1,1)
        _ColorIntensity ("Color Intensity", Float) = 1
        _ColorGlow ("Color Glow", Float) = 0

        // ---- 原版 `EDGE_SHINY` 档（`b17`）：名字逐字照抄 `b17.hlsl` 的 cbuffer ----
        _EdgeWidth ("Edge Width", Range(0,1)) = 0
        _EdgeShinyRate ("Edge Shiny Rate", Range(0,1)) = 0
        _EdgeShinyAutoPlaySpeed ("Edge Shiny Auto Play Speed", Float) = 0
        _EdgeShinyWidth ("Edge Shiny Width", Range(0,1)) = 0
        _EdgeColorFilter ("Edge Color Filter", Float) = 0
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _EdgeColorGlow ("Edge Color Glow", Float) = 0
        // `b17` c18 = `_SamplingScale`（`_MainTex_TexelSize` 由 Unity 自动填）
        _SamplingScale ("Sampling Scale", Float) = 1

        // ---- 原版 UI 那两条（`b18/b23`）----
        //   `_ClipRect` / `_UIMaskSoftnessX,Y` 由 UGUI 在运行时写，**不声明成 Property**（原版也一样）
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // ---- 原版 `_RootViewMatrix` 的替身（见文件头 ④ 里的取证）----
        //   `_ShineFrame` = 那一件的**世界**框（xy = 中心 · zw = 宽高）；`_ShineRot` = (cosθ, sinθ, 1/max(W,H), 0)。
        //   屏幕坐标在 **vert** 里由 `unity_ObjectToWorld` 取（原版也是拿顶点世界位置算的，见文件头 ④）
        //   ⇒ 九宫格的 9 块**共用一份材质**、掩码天然连续（⛔ 不需要给每块算仿射）。
        _ShineFrame ("Shine Frame (world center xy, world size zw)", Vector) = (0,0,1,1)
        _ShineRot ("Shine Rotate (cos, sin, 1/maxSize, -)", Vector) = (1,0,1,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        // 原版这颗实例的 `m_SrcBlendMode 1 / m_DstBlendMode 10` ⇒ `Blend One OneMinusSrcAlpha`（预乘 alpha）
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 🔶 自己设计（非原版）：编译档 = **3.0**（原版是 `#pragma target 5.0` + `use_dxc` + `HLSLPROGRAM`，我们沿用 `CGPROGRAM`）·
            //   为什么必须自定：Cg 在 2.5 档没有动态分支，本波的三族 ColorFilter（各 8 档 if/else）会被**全部拍平**⇒ 指令数成倍、
            //   大概率超限；3.0 是 CG 侧有动态分支的最低档，本工程 `WFUIFxShine.shader` 也是 3.0 · `B115_W22` 2026-11-01
            //   本波（`B115_W22`）把三族 ColorFilter 的 **8 档 if/else** 写进来之后，默认的 2.5 档必须**把全部
            //   分支拍平**才能编（Cg 在 2.5 上没有动态分支）⇒ 指令数会成倍膨胀、大概率超限。
            //   3.0 是 CG 侧**有动态分支**的最低档，且本工程 `WFUIFxShine.shader` 用的就是 3.0 ⇒ 提到 3.0。
            //   仍然**不写 `round()`**（那要 SM4），shader 里该用的地方继续用 `frac/floor`。
            #pragma target 3.0

            // ---- 与原版逐字一致的 6 条（判据 = 文件头那张变体表；原版是 `multi_compile_local`）----
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ TONE_GRAYSCALE
            #pragma multi_compile_local _ TRANSITION_DISSOLVE
            #pragma multi_compile_local _ TRANSITION_SHINY
            #pragma multi_compile_local _ EDGE_SHINY

            #include "UnityCG.cginc"

            // ---- 原版是「精确匹配 20 档、其余落 b13 基础档」（判据 = 文件头那张表）----
            //   ⇒ 这里也只在「四个效果关键字里**恰有一个**」时点亮对应那一段；两个以上同时开会落回「什么都不做」。
            #if defined(TONE_GRAYSCALE) && !defined(TRANSITION_DISSOLVE) && !defined(TRANSITION_SHINY) && !defined(EDGE_SHINY)
                #define WF_TONE 1
            #endif
            #if !defined(TONE_GRAYSCALE) && defined(TRANSITION_DISSOLVE) && !defined(TRANSITION_SHINY) && !defined(EDGE_SHINY)
                #define WF_DISSOLVE 1
            #endif
            #if !defined(TONE_GRAYSCALE) && !defined(TRANSITION_DISSOLVE) && defined(TRANSITION_SHINY) && !defined(EDGE_SHINY)
                #define WF_SHINY 1
            #endif
            #if !defined(TONE_GRAYSCALE) && !defined(TRANSITION_DISSOLVE) && !defined(TRANSITION_SHINY) && defined(EDGE_SHINY)
                #define WF_EDGE 1
            #endif

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
                float3 world : TEXCOORD1;
                // 原版顶点输出 `TEXCOORD_2`（`b23.hlsl` 的 `TEXCOORD_2 : TEXCOORD16`）
                #if defined(UNITY_UI_CLIP_RECT)
                float4 clipMask : TEXCOORD2;
                #endif
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _TransitionRate;
            float _TransitionWidth;
            float _TransitionSoftness;
            fixed4 _TransitionColor;
            float _TransitionColorFilter;
            float _TransitionColorGlow;
            float _TransitionReverse;
            float _TransitionAutoPlaySpeed;

            sampler2D _TransitionTex;
            float4 _TransitionTex_ST;
            float _UseTransitionTex;

            float _ToneIntensity;

            // 原版基础 `_ColorFilter` 族（4 个效果档都有；见 Properties 那段的判据与默认值说明）
            float _ColorFilter;
            float4 _ColorValue;
            float _ColorIntensity;
            float _ColorGlow;

            float4 _MainTex_TexelSize;      // Unity 自动填（判据 = `b17.hlsl` c14 的 `_MainTex_TexelSize`）
            float _SamplingScale;
            float _EdgeWidth;
            float _EdgeShinyRate;
            float _EdgeShinyAutoPlaySpeed;
            float _EdgeShinyWidth;
            float _EdgeColorFilter;
            fixed4 _EdgeColor;
            float _EdgeColorGlow;

            // 原版 `b23/b28` 的 c4 `_ClipRect` ＋ 顶点 c6 的 `_UIMaskSoftnessX/Y`（UGUI 运行时写）
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float4 _ShineFrame;
            float4 _ShineRot;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;      // 同 `UnitySprites.cginc` 的 `SpriteVert`（UI 顶点色 × 材质色）
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;

                #if defined(UNITY_UI_CLIP_RECT)
                // 逐字照 `Sub0_Pass0_Vertex_b1.hlsl:99-102`（= 顶点输出 `TEXCOORD_2`）：
                //   `_297 = mad(POSITION.x, 2, -clamp(_ClipRect.x)) - clamp(_ClipRect.z)`（y 同理）
                //   `_271/_272 = 0.25 / (_UIMaskSoftness * 0.25 + abs(pixelSize))`
                // 其中 `pixelSize` = `gl_Position.w / abs(proj[0][0]*ScreenParams.x + proj[1][0]*ScreenParams.y)`（x 向；
                // y 向换 `.y`）—— 判据 = `b1.hlsl` 的 `_239/_240/_257/_258`（矩阵按 HLSL 的 `[行][列]` 取）。
                float4 vPosition = o.pos;
                float2 pixelSize;
                pixelSize.x = vPosition.w / abs(UNITY_MATRIX_P[0][0] * _ScreenParams.x + UNITY_MATRIX_P[1][0] * _ScreenParams.y);
                pixelSize.y = vPosition.w / abs(UNITY_MATRIX_P[0][1] * _ScreenParams.x + UNITY_MATRIX_P[1][1] * _ScreenParams.y);

                o.clipMask.x = 2.0 * v.vertex.x - clamp(_ClipRect.x, -2.0e10, 2.0e10) - clamp(_ClipRect.z, -2.0e10, 2.0e10);
                o.clipMask.y = 2.0 * v.vertex.y - clamp(_ClipRect.y, -2.0e10, 2.0e10) - clamp(_ClipRect.w, -2.0e10, 2.0e10);
                o.clipMask.z = 0.25 / (max(_UIMaskSoftnessX, 0.0) * 0.25 + abs(pixelSize.x));
                o.clipMask.w = 0.25 / (max(_UIMaskSoftnessY, 0.0) * 0.25 + abs(pixelSize.y));
                #endif
                return o;
            }

            // 原版 `_TransitionTex` 的解析等价（判据见文件头 ③：`Transition-Horizontal` 是沿 y 的严格线性斜坡）
            float TransitionMask(float2 uv)
            {
                if (_UseTransitionTex > 0.5)
                {
                    float2 t = uv * _TransitionTex_ST.xy + _TransitionTex_ST.zw;
                    return tex2D(_TransitionTex, t).a;      // 原版取的是 `.w`；Alpha8 贴图四通道同值
                }
                return 1.0 - saturate(uv.y);
            }

            // ============================ ColorFilter 三族（原版 Coffee `UIEffect`）============================
            // 🔴 **三族算式互不相同 ⇒ ⛔ 别合并成一个函数**（`CLAUDE.md`/`B-313` 点名的坑）：
            //   · (A) `_ColorFilter`（`b14.hlsl:311-513`；b15 `:522-711` · b16 `:523-725` · b17 `:444-637` 同形）
            //         —— 「α 调制族」：结果再乘 `C.a`，且**再乘一次源 α**（例 2 = `p + a·C.rgb·C.a`）。
            //   · (B) `_TransitionColorFilter`（`b15.hlsl:176-306` ＝ b16 `:179-306`，两档产物逐字节相同）
            //         —— 「换色族」：**不乘 `C.a`、也不乘 α**；例 2 = `p + C.rgb`、例 4 直接 = `C.rgb`。
            //   · (C) `_EdgeColorFilter`（`b17.hlsl:638-829`）—— 公式与 (A) **逐条相同**
            //         （差异只有：颜色参数换成 `_EdgeColor`、输入已经是「(A) + intensity/glow 之后」的 `_747.._750`，见 `b17:631-637`）。
            //         ⚠️ **相同也各自一份** —— 以后动 (A) 要连带核 (C)；但⛔ 别因此合成一个函数（上一行那条）。
            //   例号 = 原版的整数档（0 = 不染；1/2/3/4/5/6/7/8 逐例见下）。

            // 亮度权重：`b14.hlsl:103` 的 `float3(0.22, 0.707, 0.071)`（三族同一套常量）
            float wfColorLuma(float3 c)
            {
                return dot(c, float3(0.2199999988079071044921875, 0.7070000171661376953125, 0.071000002324581146240234375));
            }

            // 原版 HSV 提取（`b14.hlsl:659-684` · `b16.hlsl:749-779` · `b17.hlsl:1456-1481`，三处逐句相同）
            //   返回 (h, s, v)。⚠️ **h 不是 0..1** —— 它就是原版那串 `frac(h+n)*6-3` 吃的那套量纲，⛔ 别自己归一。
            float3 wfRgb2Hsv(float3 p)
            {
                float gb     = (p.y >= p.z) ? 1.0 : 0.0;                 // `_659`
                float hi     = mad(gb, p.y - p.z, p.z);                  // `_662` = max(g,b)
                float lo     = mad(gb, p.z - p.y, p.y);                  // `_663` = min(g,b)
                float offA   = mad(gb, -1.0, 0.666666686534881591796875); // `_665`
                float offB   = mad(gb, 1.0, -1.0) - offA;                // `_670`
                float rGE    = (p.x >= hi) ? 1.0 : 0.0;                  // `_667`
                float v      = mad(rGE, p.x - hi, hi);                   // `_674`
                float mid    = mad(rGE, hi - p.x, p.x);                  // `_675`
                float chroma = v - min(lo, mid);                         // `_677`
                float h      = (mid - lo) / mad(chroma, 6.0, 9.9999997473787516355514526367188e-05)   // `_680`
                             + ((rGE > 0.5) ? offB : offA);              // `_681`
                float s      = chroma / (v + 9.9999997473787516355514526367188e-05);                  // `_684`
                return float3(h, s, v);
            }

            // 原版 HSV 回归的三个形参（`b14.hlsl:700-708`）：`k(n) = clamp(|frac(h+n)·6 − 3| − 1, 0, 1) − 1`
            //   n = 1 / 2/3 / 1/3（分别给 R / G / B）
            float3 wfHueK(float h)
            {
                float3 n = float3(1.0, 0.666666686534881591796875, 0.3333333432674407958984375);
                float3 f = abs(frac(h + n) * 6.0 - 3.0) - 1.0;           // `_700.._702`
                return saturate(f) - 1.0;                                // `_706.._708`
            }

            // ---- (A) 基础 `_ColorFilter`：逐例判据 `b14.hlsl:316-502`（b17 `:643-829` 同形）----
            //   `p` = 预乘 rgb · `a` = alpha · `C` = `_ColorValue` · `f` = `_ColorFilter`
            float4 wfColorFilterBase(float3 p, float a, float4 C, float f)
            {
                float cw = C.w;
                if (f < 0.5)  return float4(p, a);                              // 0 = 不染（原版走 catch-all `:478`）
                if (f < 1.5)  return float4(p * C.rgb * cw, a * cw);            // 例 1 `:316-330`
                if (f < 2.5)  return float4(p + a * C.rgb * cw, a);             // 例 2 `:337-347`
                if (f < 3.5)  return float4(p - a * C.rgb * cw, a);             // 例 3 `:354-364`
                if (f < 4.5)  return float4(a * C.rgb * cw, a * cw);            // 例 4 `:371-385`
                if (f < 5.5)  return float4(0.5 * a * C.rgb * cw * (wfColorLuma(p) + 1.0), a);   // 例 5 `:392-412`
                if (f < 6.5)  return float4(p * (C.rgb * cw + 1.0), a);         // 例 6 `:415-421`
                if (f < 7.5)                                                    // 例 7 `:422-462`
                {
                    float3 hsv = wfRgb2Hsv(p);
                    float3 k   = wfHueK(abs(hsv.x) + C.x);                      // `_685`
                    float3 rgb = (1.0 + (hsv.y + C.y) * k) * (hsv.z + C.z);     // `_712.._714`
                    return float4(a * rgb * cw, a * cw);                        // `_715.._721`
                }
                return float4(a * (C.y * 1.5 + ((p - 0.5) * (C.x + 1.0) + 0.5)) * cw, a * cw);      // 例 8 `:464-481`
            }

            // ---- (B) 过渡 `_TransitionColorFilter`：逐例判据 `b16.hlsl:179-306`（＝ b15 `:176-306`）----
            //   🔴 与 (A) **不是**同一个算式：这里**不乘 `C.a`、也不乘 α**（例 4 直接等于 `C.rgb`）
            float3 wfColorFilterTransition(float3 p, float4 C, float f)
            {
                if (f < 0.5)  return p;                                         // 0 = 不染（原版 `_479 > 0` 那一闸）
                if (f < 1.5)  return p * C.rgb;                                 // 例 1 `:179-187`
                if (f < 2.5)  return p + C.rgb;                                 // 例 2 `:193-198`（`mad(_403,_394,C.x)`）
                if (f < 3.5)  return p - C.rgb;                                 // 例 3 `:204-209`
                if (f < 4.5)  return C.rgb;                                     // 例 4 `:215-220` —— 纯色
                if (f < 5.5)  return 0.5 * C.rgb * (wfColorLuma(p) + 1.0);      // 例 5 `:226-238`
                if (f < 6.5)  return p * (C.rgb + 1.0);                         // 例 6 `:244-249`（⛔ 不乘 C.a —— 与 (A) 的例 6 不同）
                if (f < 7.5)                                                    // 例 7 `:250-282`
                {
                    float3 hsv = wfRgb2Hsv(p);
                    float3 k   = wfHueK(abs(hsv.x) + C.x);
                    return (1.0 + (hsv.y + C.y) * k) * (hsv.z + C.z);           // `_808.._810`
                }
                return C.y * 1.5 + ((p - 0.5) * (C.x + 1.0) + 0.5);             // 例 8 `:287-289`
            }

            // ---- (C) 边缘 `_EdgeColorFilter`：逐例判据 `b17.hlsl:643-829` ----
            //   ⚠️ 与 (A) 逐条相同（**事实**，不是抄漏）；分开是因为「三族别合并」那条纪律 + 输入/颜色参数不同
            float4 wfColorFilterEdge(float3 p, float a, float4 C, float f)
            {
                float cw = C.w;
                if (f < 0.5)  return float4(p, a);                              // 0（原版 `_1404` 的 `_750` 那一支）
                if (f < 1.5)  return float4(p * C.rgb * cw, a * cw);            // 例 1 `:643-657`
                if (f < 2.5)  return float4(p + a * C.rgb * cw, a);             // 例 2 `:664-674`
                if (f < 3.5)  return float4(p - a * C.rgb * cw, a);             // 例 3 `:681-691`
                if (f < 4.5)  return float4(a * C.rgb * cw, a * cw);            // 例 4 `:698-712`
                if (f < 5.5)  return float4(0.5 * a * C.rgb * cw * (wfColorLuma(p) + 1.0), a);   // 例 5 `:719-739`
                if (f < 6.5)  return float4(p * (C.rgb * cw + 1.0), a);         // 例 6 `:742-748`（`_1452`）
                if (f < 7.5)                                                    // 例 7 `:749-790`
                {
                    float3 hsv = wfRgb2Hsv(p);
                    float3 k   = wfHueK(abs(hsv.x) + C.x);                      // `_1482`
                    float3 rgb = (1.0 + (hsv.y + C.y) * k) * (hsv.z + C.z);     // `_1509.._1511`
                    return float4(a * rgb * cw, a * cw);                        // `_1512.._1518`
                }
                return float4(a * (C.y * 1.5 + ((p - 0.5) * (C.x + 1.0) + 0.5)) * cw, a * cw);      // 例 8 `:794-804`
            }

            // 基础 `_ColorFilter` 的**完整一跳**（`b14.hlsl:503-513` · `b16.hlsl:715-725` · `b17.hlsl:631-637` 同形）：
            //   ① `_ColorFilter == 0` ⇒ **整段跳过**（原版 `_282 = int(filter) > 0`）；
            //      ⚠️ `_ColorGlow` 也只在 filter>0 时才生效（原版 `_323 = _282 ? _298 : _274`）—— ⛔ 别把 glow 挪到闸外面
            //   ② 颜色 = `lerp(原值, 滤波值, _ColorIntensity)`
            //   ③ alpha = `lerp(α, α滤波, _ColorIntensity) · (1 − _ColorGlow·_ColorIntensity)`
            float4 wfBaseColorFilter(float3 p, float a)
            {
                if (_ColorFilter < 0.5) return float4(p, a);
                float4 flt = wfColorFilterBase(p, a, _ColorValue, _ColorFilter);
                float  I   = _ColorIntensity;
                return float4(p + I * (flt.rgb - p), (1.0 - _ColorGlow * I) * (a + I * (flt.a - a)));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                // 预乘（原版 `_378.._380` = `a * (tex.rgb * COLOR.rgb)`；`_410` = 同一份 alpha）
                float3 s = c.rgb * c.a;
                float  sa = c.a;

                // ------------------------------------------------------------------ TONE_GRAYSCALE（`b14`）
                //   原版：`_155 = dot(premultRgb, (0.22,0.707,0.071))`；
                //         `_172 = mad(_171, mad(-_146, _137, _155), _150)` = `_ToneIntensity*(luma - r) + r`
                //   （`_146*_137` / `_150` 都是「掩码后的预乘 r」；uv1 = (0,0,1,1) 时掩码 = 1，见文件头「仍未实现」那条）
                #if defined(WF_TONE)
                {
                    float luma = dot(s, float3(0.2199999988079071044921875, 0.7070000171661376953125, 0.071000002324581146240234375));
                    s = s + _ToneIntensity * (luma - s);
                    // 原版 `b14.hlsl:311-513`：Tone **之后**才走基础 `_ColorFilter`（8 档，判据 `b14:316-481`）
                    float4 bfc = wfBaseColorFilter(s, sa);
                    s = bfc.rgb; sa = bfc.a;
                }
                #endif

                // ------------------------------------------------------------------ 过渡坐标（两类过渡 + 边缘扫光共用）
                #if defined(WF_DISSOLVE) || defined(WF_SHINY) || defined(WF_EDGE)
                // 掩码坐标：顶点**世界位置** − 框心，绕框心转 θ，再按 1/max(W,H) 归一（= 原版 `_RootViewMatrix` 那一跳）
                //   判据见文件头 ④：原版是 `unity_ObjectToWorld → _CanvasToWorldMatrix → _RootViewMatrix`，
                //   而 `transitionRoot` 取的是**这一件自己的 RectTransform`（5 个实例的 `m_CustomRoot` 全 null）
                //   ⇒ 结果就是「本件框内的归一化坐标」，与这里逐项同义。
                float2 p = i.world.xy - _ShineFrame.xy;
                float2 uvT = float2(_ShineRot.x * p.x - _ShineRot.y * p.y,
                                    _ShineRot.y * p.x + _ShineRot.x * p.y) * _ShineRot.z + 0.5;
                #endif

                // ------------------------------------------------------------------ TRANSITION（`b15` 溶解 / `b16` 扫光）
                #if defined(WF_DISSOLVE) || defined(WF_SHINY)
                {
                    float tv = TransitionMask(uvT);
                    // 原版 `_330 = tex.w` / `_332 = 1 - tex.w`；`reverse` 选一条
                    float v = (_TransitionReverse > 0.5) ? (1.0 - tv) : tv;

                    // 原版 `_416 / _429 / _430 / _433 / _439 / _440 / _442.._447`
                    float rate  = _TransitionRate * 0.99989998340606689453125;
                    float tw    = _TransitionWidth;
                    float band  = v - frac(_TransitionAutoPlaySpeed * _Time.y + rate) * (tw + 1.0) + tw;
                    float softW = max(_TransitionSoftness * tw, 9.9999997473787516355514526367188e-05);
                    float mHi   = saturate(2.0 * band / softW);          // 原版 `_447 = (2*_433)/softW` 再 clamp
                    float mLo   = saturate(2.0 * (tw - band) / softW);   // 原版 `_444 = (2*(_429-_433))/softW` 再 clamp

                    // 颜色：原版 `_TransitionColorFilter` —— **8 档**（判据 `b16.hlsl:179-306`，b15 `:176-306` 逐字节同）
                    //   🔴 这族的 6 档 = `s · (C.rgb + 1)`，**不乘 C.a**；而 (A) 基础 / (C) 边缘的 6 档**乘 C.a**
                    float3 tc    = wfColorFilterTransition(s, _TransitionColor, _TransitionColorFilter);
                    // 原版 `_510.._512`：`s + mask * ( (_490 * (tc - s) + s) * sa - s )`，`_490 = _TransitionColor.a`
                    float3 inner = _TransitionColor.a * (tc - s) + s;
                    // 原版 `_513`：glow = 0 ⇒ alpha 原样；glow = 1 ⇒ 目标 alpha = `1 - TransitionColor.a`
                    float  aT    = (_TransitionColorGlow > 0.5) ? (1.0 - _TransitionColor.a) : 1.0;

                    #if defined(WF_DISSOLVE)
                    // 原版 `b15`：`_448 = clamp(_447)`，`_500 = _448 * clamp(_444)`，
                    //   然后 **整份颜色再乘一次 `_448`**（`_513.._516 = _448 * _509.._512`）—— 这就是「溶解」：
                    //   扫过的区域 α 归零、未扫过的是 0/1 硬边（`_448` 之外的带被乘没了）。
                    float maskD = mHi;                       // 原版 `_448`
                    float mask2 = maskD * mLo;               // 原版 `_500`
                    float3 rgbT = s  + mask2 * (inner * sa - s);
                    float  aTa  = sa + mask2 * (aT * sa - sa);
                    s  = rgbT * maskD;                       // 原版 `_513.._515`
                    sa = aTa  * maskD;                       // 原版 `_516`
                    #else
                    // 原版 `b16`：`_500 = clamp(_447)*clamp(_444)`；`_501 = _500*_500`（平方 ⇒ 软边更细）
                    float m0    = mHi * mLo;                 // 原版 `_500`
                    float mask2 = m0 * m0;                   // 原版 `_501`
                    s  = s  + mask2 * (inner * sa - s);      // 原版 `_510.._512`
                    sa = sa + mask2 * (aT * sa - sa);        // 原版 `_513`
                    #endif

                    // 原版 `b15:522-711` / `b16:523-725`：基础 `_ColorFilter` 在**过渡混合之后**才生效
                    float4 bfc = wfBaseColorFilter(s, sa);
                    s = bfc.rgb; sa = bfc.a;
                }
                #endif

                // ------------------------------------------------------------------ EDGE_SHINY（`b17`）
                #if defined(WF_EDGE)
                {
                    // ---- 边缘 = 半径方向 12 个 30° 采样取 α 最小（原版 `_386.._630`）----
                    //   `_394 = _EdgeWidth*19 + 1`；`_387/_388 = _MainTex_TexelSize.xy * _SamplingScale`
                    float2 texel = _MainTex_TexelSize.xy * _SamplingScale;
                    float  ew    = _EdgeWidth * 19.0 + 1.0;
                    float  dx    = texel.x * ew;              // 原版 `_396`
                    float  dy    = texel.y * ew;              // 原版 `_397`
                    float2 uv0   = i.uv;

                    //   12 个偏移逐条对应 `b17.hlsl:399-622`（0°/30°/60°/…/330°，`_396` 为径向 x 分量、`_397` 为 y）
                    float a01 = tex2D(_MainTex, uv0 + float2(        dx,        0.0)).a;   // _417
                    //   （`0.866025` 一律取反编译里的原值 `0.86602497100830078125` = cos30°）
                    float a02 = tex2D(_MainTex, uv0 + float2( 0.86602497100830078125 * dx,  0.5 * dy)).a;                 // _436
                    float a03 = tex2D(_MainTex, uv0 + float2( 0.5 * dx, 0.86602497100830078125 * dy)).a;                 // _461
                    float a04 = tex2D(_MainTex, uv0 + float2(       0.0,                            dy)).a;                // _480
                    float a05 = tex2D(_MainTex, uv0 + float2(-0.5 * dx, 0.86602497100830078125 * dy)).a;                 // _503
                    float a06 = tex2D(_MainTex, uv0 + float2(-0.86602497100830078125 * dx,  0.5 * dy)).a;                // _520
                    float a07 = tex2D(_MainTex, uv0 + float2(      -dx,                            0.0)).a;                // _538
                    float a08 = tex2D(_MainTex, uv0 + float2(-0.86602497100830078125 * dx, -0.5 * dy)).a;                // _555
                    float a09 = tex2D(_MainTex, uv0 + float2(-0.86602497100830078125 * dx, -0.86602497100830078125 * dy)).a; // _572
                    float a10 = tex2D(_MainTex, uv0 + float2(       0.0,                          -dy)).a;                 // _593
                    float a11 = tex2D(_MainTex, uv0 + float2( 0.5 * dx, -0.86602497100830078125 * dy)).a;                 // _606
                    float a12 = tex2D(_MainTex, uv0 + float2( 0.86602497100830078125 * dx, -0.5 * dy)).a;                // _619

                    // 原版 `_624 = min(min(…, 1.0)) - 0.15`；`_627 = max(_624,0)*6.6667`；`_630 = 1 - min(_627,1)`
                    float aMin = min(min(min(min(a01, a02), min(a03, a04)), min(min(a05, a06), min(a07, a08))),
                                     min(min(a09, a10), min(a11, a12)));
                    aMin = min(aMin, 1.0);
                    float edge = 1.0 - saturate(max(aMin - 0.1500000059604644775390625, 0.0) * 6.666666507720947265625);

                    // 原版 `b17.hlsl:444-637`：基础 `_ColorFilter` 在**边缘色之前**
                    //   （`_EdgeColorFilter` 的输入 `_747.._750` 就是它 + intensity/glow 算出来的）
                    float4 bfc = wfBaseColorFilter(s, sa);
                    s = bfc.rgb; sa = bfc.a;

                    // ---- 边缘色：原版 `_EdgeColorFilter` —— **8 档**（判据 `b17.hlsl:643-829`）----
                    //   🔴 6 档 = `rgb · (C.rgb·C.a + 1)`（**乘 C.a**）；而 `_TransitionColorFilter` 的 6 档**不乘 C.a**
                    //   （`b17.hlsl:746` `_1452 = _747 * mad(_1442, _1448, 1.0)`，`_1448 = _EdgeColor.w` vs `b16.hlsl:745`）。
                    float4 ecf = wfColorFilterEdge(s, sa, _EdgeColor, _EdgeColorFilter);
                    float3 ec  = ecf.rgb;
                    // 原版 `_910/_911` + `:846`：`(int(_EdgeColorFilter) > 0) ? (1−glow)·α滤波 : α滤波`
                    //   ⇒ `_EdgeColorGlow` 只影响 α，且**只在 filter > 0 时生效**
                    float  ea = (_EdgeColorFilter > 0.5) ? ((1.0 - _EdgeColorGlow) * ecf.a) : sa;

                    // ---- 扫边光：原版 `_913.._974`（= `atan2(uv.y-0.5, uv.x-0.5) / π` 的扫角）----
                    float ay = uvT.y - 0.5;                   // 原版 `_913`
                    float ax = uvT.x - 0.5;                   // 原版 `_914`
                    float adx = abs(ax), ady = abs(ay);
                    float amax = max(adx, ady);
                    float amin = min(adx, ady);
                    float t    = amin / max(amax, 1.0e-8);   // 原版 `_920`（原版此处 1/0 = inf ⇒ frac(NaN) 比较恒 false，结果同为「不亮」；
                                                            //   这里只是加了个下界把 NaN 挡掉，见报告 §五 顺手发现）
                    float t2   = t * t;                       // 原版 `_921`
                    // 原版 `_929`：atan 多项式
                    float poly = t2 * (t2 * (t2 * (t2 * 0.02083509974181652069091796875
                                                 - 0.08513300120830535888671875)
                                           + 0.1801410019397735595703125)
                                     - 0.33029949665069580078125)
                               + 0.999866008758544921875;
                    float a1    = t * poly;                   // 原版 `_931`
                    float angle = (ax < 0.0 ? -3.1415927410125732421875 : 0.0) + a1
                                + ((adx < ady) ? (1.57079637050628662109375 - 2.0 * a1) : 0.0);   // 原版 `_941`
                    float sweep = frac(angle * 0.3183098733425140380859375
                                       + _Time.y * _EdgeShinyAutoPlaySpeed
                                       + _EdgeShinyRate);     // 原版 `_974` 的 frac 那一半
                    float shiny = ((sweep < _EdgeShinyWidth) ? 1.0 : 0.0) * edge;   // 原版 `_974`

                    s  = s  + shiny * (ec - s);               // 原版 `_781/_783/_785/_787 = mad(_974, edge-base, base)`
                    sa = sa + shiny * (ea - sa);
                }
                #endif

                // 原版末尾 `SV_Target = _72 * (color)`，`_72` = 顶点 alpha 的 8 bit 量化值（UI 顶点色那一份）
                // ⚠️ 用 `floor(x+0.5)` 而不是 `round()`：`round` 要 SM4，本 shader 用默认 2.5 档（少一次编译风险）
                float vAlpha = floor(i.color.a * 255.0 + 0.5) * 0.0039215688593685626983642578125;
                float4 col = float4(s, sa) * vAlpha;

                #if defined(UNITY_UI_CLIP_RECT)
                // 原版 `b23.hlsl:137.._161`：软裁切（`_ClipRect` 宽高 − |顶点传下来的距离|）× 软化系数，两块都 clamp
                float cw = _ClipRect.z - _ClipRect.x;
                float ch = _ClipRect.w - _ClipRect.y;
                float cx = (cw - abs(i.clipMask.x)) * i.clipMask.z;
                float cy = (ch - abs(i.clipMask.y)) * i.clipMask.w;
                col *= clamp(cy, 0.0, 1.0) * clamp(cx, 0.0, 1.0);
                #endif

                #if defined(UNITY_UI_ALPHACLIP)
                // 原版 `b18.hlsl:71`：`if (mad(_111, _118, -0.001) < 0) discard;`（`_111`=最终 α · `_118`=顶点 α 量化值）
                if (col.a < 0.001) discard;
                #endif

                return fixed4(col.rgb, col.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
