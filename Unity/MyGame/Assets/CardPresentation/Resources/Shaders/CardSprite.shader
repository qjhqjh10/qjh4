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
Shader "CardPresentation/CardSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GreyScale ("Greyscale", Range(0,1)) = 0
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
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GreyScale;

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
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
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
