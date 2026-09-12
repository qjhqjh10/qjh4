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
                c.a = 1;                       // **关键**：贴图的 alpha 是「角色抠图」，这一层不要它
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
