// WFSpritesAdditive.shader — 自建，用来替代原版 Everguild/Sprites/Sprite Additive
//
// 原版 shader 的 HLSL 源码在打包时被剥离，只剩字节码。这里按从 bundle 里读出来的
// 「属性表 + 渲染状态」重建一个功能等价的：
//
//   属性（7 个，与原版一一对应）：
//     _MainTex _Color PixelSnap _RendererColor _Flip _AlphaTex _EnableExternalAlpha
//   渲染状态（从 SerializedShaderState 读出）：
//     混合 Src=One Dst=One（纯加法，写死在 shader 里，材质上没有 _SrcBlend）
//     ZWrite=0  ZTest=LEqual  Cull=Off  Queue=Transparent  AlphaToMask=0
//     Tags: CanUseSpriteAtlas / IGNOREPROJECTOR / RenderType=Transparent / PreviewType=Plane
//
// 它本质就是 Unity 内置 `Sprites/Default` 的**加法混合版**。属性名与原版保持一致，
// 原版材质的数值可以原样灌进来，不需要转换。
//
// 用法：原版 shader 名 → "WarpforgeVFX/Sprites/Additive"
Shader "WarpforgeVFX/Sprites/Additive"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // 加法混合写死在 shader 里 —— 原版材质上没有 _SrcBlend/_DstBlend，
        // 所以不要在这里用 [_SrcBlend] 这种材质可驱动的写法
        Blend One One
        Cull Off
        ZWrite Off
        ZTest LEqual
        Lighting Off

        Pass
        {
            Name "SpriteAdditive"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

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
                fixed4 color      : COLOR;
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

                #ifdef PIXELSNAP_ON
                    OUT.positionCS = UnityPixelSnap(OUT.positionCS);
                #endif
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // ETC1 外部 alpha（原版属性里有，一般不开）
                #ifdef ETC1_EXTERNAL_ALPHA
                    half4 a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.uv);
                    c.a = lerp(c.a, a.a, _EnableExternalAlpha);
                #endif

                // 加法混合：SrcAlpha 由 blend factor 负责，这里直接输出颜色
                // 原版是 Blend One One，所以 alpha 不参与衰减 —— 保持原样
                return c;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
