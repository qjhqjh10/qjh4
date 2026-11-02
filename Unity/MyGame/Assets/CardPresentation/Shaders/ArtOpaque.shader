// ArtOpaque.shader — 把立绘当**不透明**画（只取 RGB，忽略贴图自带的 alpha）
//
// 为什么需要它：原版卡面的立体感是**同一张立绘画两次**做出来的（2026-09-13 查实）：
//   · 立绘的 **RGB = 完整插图**（角色 + 背景），**alpha = 角色的抠图轮廓**（单位卡 91–97% 是透明的）
//   · 卡框的拱窗**自己也是透明的**（实测 alpha=0）→ 拱窗里的背景必须由底下一层补上
//   ⇒ 画法：① 底层「完整插图」**忽略 alpha**（垫在卡框下，拱窗里的背景就是它）
//           ② 前景层「同一个贴图 + 真 alpha」（盖在**卡框上面** → 角色越出卡框）
//
// 为什么不用现成的 `Unlit/Texture`：它不吃 `_Color`/顶点色，而我们的高亮/置灰（`CardView.SetTint`）
// 全靠那个颜色乘上去 —— 用了它，卡置灰时立绘还是亮的。
// 为什么不用「再导出一份不透明的图」：1137 张立绘现在 1.5 GB，翻倍要多占约 1 GB 磁盘，不值。
//
// ⚠️ 混合模式与 `Sprites/Default` 保持一致（`Blend SrcAlpha OneMinusSrcAlpha` + `ZWrite Off`），
//    这样两层之间、以及与其它层的**排序规则不变**（Unity 透明队列按相机距离排，我们靠 z 控制）。
Shader "CardPresentation/ArtOpaque"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        // 🆕 2026-10-19（`A2139`）：原版去饱和通道（判据 → `Resources/Shaders/CardSprite.shader` 头部）。
        // 0 = 原色（默认，画面不变）/ 1 = 全灰。立绘这层是卡面最大的一块颜色，
        // 少了它，「加不进的卡」只有框/文字会灰、插图还是彩的。
        _GreyScale ("Greyscale", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _GreyScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // 🆕 A2139：去饱和（同 `CardSprite.shader` 的口径：Rec.601 · 只动 RGB）
                fixed lum = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, lum.xxx, saturate(_GreyScale));
                // **关键**：贴图的 alpha 是「角色抠图」，这一层不要它 —— 要的是 **`_Color`/顶点色**那个 alpha。
                // 🔴 2026-11-02（`B118-W14` 就地订正）：原来是把 a 硬写成 1 —— 那会把**材质**的 alpha 也一起
                //   丢掉 ⇒ `CardView.ApplyTint` 给这一层写的 `_tint.a × _alpha × _artAlpha × _alpha2D`
                //   **全部静默失效**（整卡淡出 / 回手「2D 卡面淡回来」在屏幕上一点都看不出来，只有
                //   `Show()` 的硬开关起作用 —— 用户报的「回手露出一块无框矩形」有一半就是这条）。
                //   `c.a = i.color.a` 的语义正是想要的：**贴图 alpha 不要、材质与顶点 alpha 要**
                //   （mesh 没有 COLOR 流时 Unity 供 (1,1,1,1) ⇒ `_Color.a = 1` 时与改前**逐像素相同**）。
                c.a = i.color.a;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
