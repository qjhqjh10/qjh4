// FrameCutout.shader — 卡框的「角色破框」遮罩版
//
// 机制（2026-09-13 推断 + 实测中）：原版卡面的立体感**不是**「同张图画两层」——
// 那样做在半透明区会留下鬼影（我们的第一版就是这样，实测卡框上有一片斑驳）。
// 更像的做法是：**立绘照常画一次（垫在卡框下、不遮罩），卡框在角色处被挖开一个洞**。
// 于是：拱窗里的背景来自插图 ✓、角色从洞里透出来 ✓、没有第二层 → 没有重影 ✓。
//
// 洞从哪来：立绘贴图的 **alpha 通道**（单位卡 91–97% 透明、战术卡 0%）——
// 它就是「哪些像素属于角色」的现成遮罩，**按立绘的 UV** 采样。
//
// ⚠️ 两个 UV 不一样：卡框的 UV 裁到了金属 bbox（`FrameUv`），立绘的 UV 是它自己那块 ——
//    所以卡框网格上额外写了 **`uv2` = 立绘的 UV**（见 `CardView.FrameMesh`），
//    这个 shader 用 `uv2` 采遮罩。遮罩图超出 [0,1] 的部分由 Clamp 收边：
//    立绘比卡框小一圈，框外面 clamp 到边缘像素（那里 alpha=0）→ **框照常不透明** ✓。
Shader "CardPresentation/FrameCutout"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _CutMask ("Art (alpha = 角色遮罩)", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        /// 遮罩强度 0..1（1 = 完全按立绘 alpha 挖洞；0 = 不挖，退回普通卡框）
        _CutAmount ("Cut Amount", Range(0,1)) = 1
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 uvm : TEXCOORD1; fixed4 color : COLOR; };

            sampler2D _MainTex;
            sampler2D _CutMask;
            fixed4 _Color;
            float _CutAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvm = v.uv2;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // 立绘的 alpha = 角色遮罩：角色处把卡框**挖掉**（alpha 归零）
                float mask = tex2D(_CutMask, i.uvm).a * _CutAmount;
                c.a *= (1.0 - mask);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
