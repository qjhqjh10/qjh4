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

            // 🔴 **下面两样是 `UnitySprites.cginc` 提供的，HLSL 风格这份必须自己补**
            //    （2026-09-16 构建 player 才暴露：编辑器和 D3D12 那条路上一直没报）。
            //    · `_EnableExternalAlpha` 只在 Properties 里声明过，**HLSL 侧没有同名 uniform**
            //      ⇒ `undeclared identifier`（连累下一行的 `lerp` 也报「没有三参重载」）；
            //    · `UnityPixelSnap` 是个**函数**，CG 版由 `UnitySprites.cginc` 带进来，这里没有。
            //    不补的后果：`WFSpritesAdditive` 在 **d3d11 变体下整张编译失败** ⇒
            //    用到 `Everguild/Sprites/Sprite Additive` 的材质在真包里渲成**洋红**。
            float _EnableExternalAlpha;

            /// 逐像素对齐（照抄 `UnitySprites.cginc` 的 `UnityPixelSnap`）
            float4 UnityPixelSnap(float4 pos)
            {
                float2 hpc = _ScreenParams.xy * 0.5;
                float2 pixelPos = round((pos.xy / pos.w) * hpc);
                pos.xy = pixelPos / hpc * pos.w;
                return pos;
            }

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
                // 🔴 **这里是 `half4` 不是 `fixed4`**（2026-09-16 改）。
                //    `fixed4` 是 CG 时代的类型，**由 `HLSLSupport.cginc` 提供** ——
                //    `CGPROGRAM` 写的 shader（`ArtOpaque.shader` / `FrameCutout.shader`）会自动带上，
                //    而**本文件是 URP 的 HLSL 风格**（`HLSLPROGRAM` + `Core.hlsl`），那个头文件不会被包含。
                //    编辑器和 D3D12 那条路上一直没暴露，直到**构建 player** 才报：
                //      `Shader error in 'WarpforgeVFX/Sprites/Additive': unrecognized identifier 'fixed4'
                //       at WFSpritesAdditive.shader(86) (on d3d11)` —— **构建期 2 条错误**（顶点 + 片元）。
                //    ⇒ 用 `fixed`/`fixed2`/`fixed3` 的地方，在 HLSL 风格 shader 里一律换 `half`/`float`。
                half4 color       : COLOR;
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
