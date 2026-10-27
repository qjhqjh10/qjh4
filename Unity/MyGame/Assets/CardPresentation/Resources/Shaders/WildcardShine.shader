// WildcardShine.shader —— 原版 Coffee `UIEffect` 的 **Shiny 过渡**（`TransitionFilter.Shiny = 4`）那一支。
//
// 用途：账 `收藏:348`（`WildcardDisplay.OnEnable` → `shinyEffect.Play(resetTime:true)`）——
//   收藏窗 `CardsTab ▸ Header Filters ▸ WIldcard Display ▸ Background` 上那道扫光。
//   （那颗 `Background` 上总共挂了 4 个组件：`Image`（Sliced · 图 `40k_topmarquee_currency_display_BW` ·
//   色 `(0.462,0.462,0.462,1)`）+ **`UIEffect`** + **`UIEffectTweener`** + `CanvasRenderer` —— 2026-10-27 现读
//    `bundle_menus_assets_all` 的 `GameObject/…5282456591777456853`。）
//
// ============================ 判据从哪来（全是直读，⛔ 不是照截图猜）============================
//  ① 原版 shader = `Hidden/UI/Default (UIEffect)`（`globalgamemanagers` 的 `Shader_77`，`m_ParsedForm.m_Name`）。
//     它的 **`TRANSITION_SHINY` 关键字变体**的像素级实现 = `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/
//     Assets/Shader/Hidden_UI_Default (UIEffect)_0/Sub0_Pass0_Fragment_b16.hlsl`（ruri 反编译出的 SPIR-V → HLSL，
//     747 行）。下面每条式子都标了它原文里的变量名（`_433` / `_501` / `_510` …），可以逐条回去核。
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
// ============================ 只实现了「Shiny」这一档（如实记）============================
//   ⛔ 未实现（该实例上全是 None ⇒ 这两个关键字变体里也不会被编进去；将来真要用要按同样的办法另开变体）：
//     `TONE_GRAYSCALE` · `TRANSITION_DISSOLVE` · `EDGE_SHINY` · Gradation / Target / Detail / Shadow / Color /
//     Sampling 那几族，以及 **UI 的 `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`**（那两条在原版是
//     `#pragma multi_compile` 的独立变体；我们没有 UI 的 `_ClipRect`/`_UseUIAlphaClip` 通道 ⇒ 少这两路）。
//   ✅ 已实现 = 原版 `TRANSITION_SHINY` 变体在「无裁剪」那一路的全部分支：`_TransitionColorFilter` 的
//     **6 / 0 两档**（6 = 该实例的值；0 = 不染色）、`_TransitionColorGlow` 的 0/1 两档、`_TransitionReverse` 两档。
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
        _TransitionAutoPlaySpeed ("Transition AutoPlay Speed", Float) = 0

        // ---- 掩码贴图（原版 `_TransitionTex` / `_TransitionTex_ST` / `_TransitionTex_Speed`）----
        _TransitionTex ("Transition Texture", 2D) = "white" {}
        _TransitionTex_ST ("Transition Texture ST", Vector) = (1,1,0,0)
        _UseTransitionTex ("Use Transition Texture", Float) = 0

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
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0;
                             float3 world : TEXCOORD1; };

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

            float4 _ShineFrame;
            float4 _ShineRot;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;      // 同 `UnitySprites.cginc` 的 `SpriteVert`（UI 顶点色 × 材质色）
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

                // 预乘（原版 `_378.._380` = `a * (tex.rgb * COLOR.rgb)`；`_410` = 同一份 alpha）
                float3 s = c.rgb * c.a;
                float  sa = c.a;

                // ---- 掩码坐标：顶点**世界位置** − 框心，绕框心转 θ，再按 1/max(W,H) 归一（= 原版 `_RootViewMatrix` 那一跳）----
                //   判据见文件头 ④：原版是 `unity_ObjectToWorld → _CanvasToWorldMatrix → _RootViewMatrix`，
                //   而 `transitionRoot` 取的是**这一件自己的 RectTransform**（5 个实例的 `m_CustomRoot` 全 null）
                //   ⇒ 结果就是「本件框内的归一化坐标」，与这里逐项同义。
                float2 p = i.world.xy - _ShineFrame.xy;
                float2 uvT = float2(_ShineRot.x * p.x - _ShineRot.y * p.y,
                                    _ShineRot.y * p.x + _ShineRot.x * p.y) * _ShineRot.z + 0.5;

                float tv = TransitionMask(uvT);
                // 原版 `_330 = tex.w` / `_332 = 1 - tex.w`；`reverse` 选一条
                float v = (_TransitionReverse > 0.5) ? (1.0 - tv) : tv;

                // 原版 `_416 / _429 / _430 / _433 / _439 / _440 / _442.._447`（逐字照 Sub0_Pass0_Fragment_b16.hlsl:164-171）
                float rate   = _TransitionRate * 0.99989998340606689453125;
                float width  = _TransitionWidth;
                float band   = v - frac(_TransitionAutoPlaySpeed * _Time.y + rate) * (width + 1.0) + width;
                float softW  = max(_TransitionSoftness * width, 9.9999997473787516355514526367188e-05);
                float m0     = saturate(2.0 * band / softW) * saturate(2.0 * (width - band) / softW);
                float mask2  = m0 * m0;                        // 原版 `_501 = _500 * _500`

                // ---- 颜色：原版 `_TransitionColorFilter` 的 6 档（= `色 * (TransitionColor + 1)`）与 0 档（不染）----
                float3 tc = (_TransitionColorFilter > 5.5) ? (s * (_TransitionColor.rgb + 1.0)) : s;
                // 原版 `_510.._512`：`s + mask2 * ( (_490 * (tc - s) + s) * sa - s )`，`_490 = _TransitionColor.a`
                float3 inner = _TransitionColor.a * (tc - s) + s;
                float3 outRgb = s + mask2 * (inner * sa - s);

                // 原版 `_513`：glow = 0 ⇒ alpha 原样；glow = 1 ⇒ 目标 alpha = `1 - TransitionColor.a`
                float aT = (_TransitionColorGlow > 0.5) ? (1.0 - _TransitionColor.a) : 1.0;
                float outA = sa + mask2 * (aT * sa - sa);

                // 原版末尾 `SV_Target = _72 * (color)`，`_72` = 顶点 alpha 的 8 bit 量化值（UI 顶点色那一份）
                // ⚠️ 用 `floor(x+0.5)` 而不是 `round()`：`round` 要 SM4，本 shader 用的是默认 2.5 档（少一次编译风险）
                float vAlpha = floor(i.color.a * 255.0 + 0.5) * 0.0039215688593685626983642578125;
                return fixed4(outRgb, outA) * vAlpha;
            }
            ENDCG
        }
    }
    Fallback Off
}
