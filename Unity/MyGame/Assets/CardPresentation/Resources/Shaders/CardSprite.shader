// CardSprite.shader —— 卡面各层的基材（`Sprites/Default` 的等价物 + 原版去饱和通道 `_GreyScale`）
//
// 为什么要自己这一支（`A2139`，2026-10-19）：
//   · 原版「加不进的卡 / 未解锁的卡」走的是**去饱和**，不是乘法压暗 ——
//     判据 = `CollectionCard__UpdateCardVisuals.c:19-20`（`ToggleGreyScale(controller, canAdd ^ 1, …)`）
//     → `UIImageGreyscaleController.ImageMaterialPair.Update()` 把灰度值写进材质
//     → 原版材质 `Sprite Greyscale` 的属性表里只有 `_GreyScale = 1.0`（0 = 原色 / 1 = 全灰）。
//   · 卡面各层原来拿的是内置 `Sprites/Default`，它没有这个属性（内置 shader 也改不了）
//     ⇒ 这一支逐行照 `UnitySprites.cginc`（`6000.3.23f1/Editor/Data/Resources/CGIncludes/` 实读）转写，
//       **只多一个 `_GreyScale`**：顶点色 × `_Color`、`c.rgb *= c.a`、`Blend One OneMinusSrcAlpha`
//       —— 与 `Sprites/Default` 逐项一致。省掉的 `_RendererColor` / `_Flip` / 外部 alpha 是
//       `SpriteRenderer` 专用（我们全是 `MeshRenderer`，见 `CardView.AddLayer`）。
//   · `_GreyScale` 默认 **0 = 画面与 `Sprites/Default` 逐像素一致**；1 = 全灰（Rec.601 亮度，只降饱和、不动 alpha）。
//
// ⚠️ **放在 `Resources/` 下是故意的**：`Shader.Find` 只能找到「构建里包含的」shader。
//    本仓其他自建卡面 shader 靠 `ProjectSettings/GraphicsSettings.asset` 的 AlwaysIncluded 兜底，
//    而那份工程设置不在本笔（`A2139`）的文件所有权里 ⇒ 放 `Resources/` = 自动进包，不必改工程设置。
//    ⚠️ 与 `Shaders/TraitDisabled.shader` 同一个对外契约（属性名 `_GreyScale`、语义 0/1 一致）。
//
// 🆕 **2026-11-04（`B141-W33b`）：卡框 / 卡背的 4-tap 超采样（`_AAScale` + `SUPERSAMPLING_ON`）**
//   判据（**原版编译字节码反汇编**，不是猜的）：原版 shader `Custom/SuperSampled Texture`
//   （`wf_shaders_extra.bundle`，`compressedBlob` LZ4 解开 = 4 个 DXBC 程序块，`D3DDisassemble` 逐块现读）：
//     · `SUPERSAMPLING_ON` **OFF** 变体（ps_4_0）= `sample(uv) × vertexColor`（与下面原路径逐句相同）；
//     · `SUPERSAMPLING_ON` **ON** 变体 = **4-tap 旋转网格超采样（RGSS）**：
//         dx = ddx(uv × _AAScale) · dy = ddy(uv × _AAScale)
//         a = tex(uv + 0.125·dx + 0.375·dy)          b = tex(uv + 0.375·dx − 0.125·dy)
//         c = tex(uv − 0.125·dx − 0.375·dy)          d = tex(uv − 0.375·dx + 0.125·dy)
//         out = (a + b + c + d) × 0.25 × vertexColor
//       （混合态 `SrcAlpha → OneMinusSrcAlpha`；我们既有的**预乘**写法与它逐像素等价：
//        `avg(rgb) × avg(a)` 两条路算出来相同。）
//   原版材质 `Card Frame and Cardback for combat`（`battleprefabs_vfxandmisc_assets_all`）：
//     `_AAScale = 1.25`，挂在 `CardPrefab` 的 `CardFrame` 与 `Cardback` 两个 `Image` 上。
//   开关 = **全局** `Shader.EnableKeyword("SUPERSAMPLING_ON")`（材质本地关键字是空的；原版
//     `QualitySettingsManager.ApplyGraphicsQuality` 按 per-tier `enableCardsAntiAliasing` 开/关，
//     `Intro.unity` 的 `qualityDefinitions` 现读 = quality 0/1/2 关、3/4 开）—— 落点见
//     `Shell/SettingsWindow.cs` 的 `SuperSampling.ApplyCardAntiAliasingKeyword()`。
//   ⚠️ **本 shader 是卡面各层共用的基材**（立绘 / 底板 / 数值层 / `ImageQuad` 都在用）⇒ 只有显式把
//     `_AAScale` 写成 1.25 的材质（= `CardView` 的卡框 / 卡背）才走超采样；其余保持 **0 = 原路径**。
Shader "CardPresentation/CardSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GreyScale ("Greyscale", Range(0,1)) = 0
        // 🔶 自己设计（非原版）：`_AAScale` 的**默认值**取 0（关），原版实参是 1.25 ——
        //    为什么必须自定：原版那支 shader 只服务卡框/卡背一颗材质；我们这一支是**全卡面层共用**的
        //    （`ImageQuad` 还会直接 `new Material(sh)`），默认 1.25 会把立绘/底板/数值层一起超采样
        //    ⇒ 用「默认关 + 卡框/卡背材质显式写 1.25」保留原版的**着色结果**。调度台 2026-11-04 裁定（`B141-W33b`）。
        //    原版属性表 = `_AAScale ("AA Pixel Width", Range(0.75, 10)) = 1.25`（`普查产出_0917/shader属性表_块1.md` §2）。
        _AAScale ("AA Pixel Width", Range(0, 10)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        // 🔴 与 `Sprites/Default` 一致：**预乘 alpha**（frag 里 `c.rgb *= c.a`，见下）
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 原版 shader 的关键字名（⛔ 别改名）：全局开/关由 `SuperSampling.ApplyCardAntiAliasingKeyword()` 落。
            #pragma multi_compile __ SUPERSAMPLING_ON
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GreyScale;
            float _AAScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;      // 同 `UnitySprites.cginc` 的 `SpriteVert`（去掉 _RendererColor）
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c;
            #ifdef SUPERSAMPLING_ON
                // `_AAScale = 0` ⇒ 原路径（逐像素与改动前一致）；>0 才超采样。
                if (_AAScale > 1e-4)
                {
                    // 逐句照原版 DXBC（见文件头）：dx/dy 里已经乘过 `_AAScale`。
                    float2 duv = i.uv * _AAScale;
                    float2 dx = ddx(duv);
                    float2 dy = ddy(duv);
                    c  = tex2D(_MainTex, i.uv + 0.125 * dx + 0.375 * dy);
                    c += tex2D(_MainTex, i.uv + 0.375 * dx - 0.125 * dy);
                    c += tex2D(_MainTex, i.uv - 0.125 * dx - 0.375 * dy);
                    c += tex2D(_MainTex, i.uv - 0.375 * dx + 0.125 * dy);
                    c *= 0.25;
                    c *= i.color;
                }
                else
            #endif
                {
                    c = tex2D(_MainTex, i.uv) * i.color;
                }
                // 原版 `Sprite Greyscale` 的语义：只降饱和、**不动 alpha**（灰但仍然实）。
                // 放在预乘之前 —— 灰化的是「颜色」，alpha 通道不参与。
                fixed lum = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, lum.xxx, saturate(_GreyScale));
                c.rgb *= c.a;                    // 同 `SpriteFrag`：预乘（与上面的 Blend 配套）
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
