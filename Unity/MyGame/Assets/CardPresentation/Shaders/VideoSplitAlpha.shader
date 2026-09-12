// VideoSplitAlpha.shader — 自建：把「左右拼 alpha」的结算视频合回 RGBA
//
// ---- 为什么要自己合（实测，不是猜）----
// 源文件 `素材/Warpforge原版/视频/videos/{Victory,Defeat,Draw} Video.mp4` 实为
// **matroska/webm + VP8**、**3840×1080 / yuv420p**（ffprobe 实测，**没有 alpha 通道**）：
// 左半 1920 = 彩色正片，右半 1920 = 同一形状的白色剪影（明度即 alpha）。
//
// 原版 `VideoClip` 资产元数据写的是 `Width: 1920, Height: 1080, m_HasSplitAlpha: true`
// —— 那份是**原版导入时已经拆好的产物**（存在 bundle 里的就是拆好的 3840×1080 拼图）。
// 我们把同一份字节丢进自己的工程，Unity 报的是 **3840×1080**：`VideoProbe.Run` 实测，
// **Unity 不会替我们拆**。所以合成只能自己做。
//
// 判据（`VideoProbe.Play` 实测）：播出来的 RT 里 **alpha 恒为 255**，画面在 RGB 里
// —— 印证「右半是画，不是通道」。
//
// ---- 取法 ----
//   uv.x ∈ [0,1] → 左半 0..0.5 取颜色；右半 0.5..1 取明度当 alpha。
//   两半用**同一个 v**，保证取到同一行。
//   混合用常规 alpha 混合：遮罩黑的地方（a=0）就是全透明，正片的黑底因此消失。
//
// ⚠️ 这个 shader 只对**这一份**资产成立（左右拼、右半是明度遮罩）。
//    原版还有 `m_HasSplitAlpha:false` 的视频（开场/主菜单），那些不能套这个。
Shader "CardPresentation/Video Split Alpha"
{
    Properties
    {
        [PerRendererData] _MainTex("Video (left=color | right=alpha)", 2D) = "black" {}
        _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual
        Lighting Off

        Pass
        {
            Name "VideoSplitAlpha"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x * 0.5, uv.y)).rgb;
                half  a   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(0.5 + uv.x * 0.5, uv.y)).r;
                return half4(col, a) * IN.color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
