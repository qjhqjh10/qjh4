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
// 🔴 **2026-10-02 算式照反编译核过**（`工具/disasm_dxbc.py "Everguild/Sprites/Sprite Additive" --stage ps|vs`），
//    判据文件：`_tmp_view/dxbc/Everguild_Sprites_Sprite Additive_{ps,vs}.txt`。
//    原版 **vs**（24 指令，最短那段）末尾三条：
//        mul r0.xyzw, v1.xyzw, cb0[2].xyzw     ; 顶点色 × 材质色
//        mul o1.xyzw, r0.xyzw, cb3[0].xyzw     ; × _RendererColor      ← 我们原来漏了这一项
//        mov o2.xy,   v2.xyxx                  ; UV 直通（**不做** TRANSFORM_TEX）
//    原版 **ps**（11 指令，基础变体 = 材质 `keywords: []` 那一档）：
//        sample r0.xyzw, v2.xyxx, t0.xyzw, s0  ; _MainTex
//        mul    r0.xyzw, r0.xyzw, v1.xyzw      ; × 顶点色（_Color/_RendererColor 已在 vs 里乘过）
//        mul    o0.xyz,  r0.wwww, r0.xyzx      ; 🔴 **预乘 alpha** —— 我们原来少了这一条
//        mov    o0.w,    r0.w
//    ⇒ 两处已按原版改：① vs 补 `_RendererColor` ② ps 补「rgb 预乘 alpha」。
//      `Blend One One` 下第二项是**看得见**的（贴图 alpha 沿边缘衰减 ⇒ 少乘就是硬边高亮）。
//    另有 ETC1 那一档（17 指令）用 `_AlphaTex` 的 **`.x`（红通道）**做外部 alpha，不是 `.a`，也一并照改。
//    ⚠️ `_AlphaTex` 只在 `ETC1_EXTERNAL_ALPHA` 开时采；本工程所有用它的材质 `keywords` 都是空的。
//
// 用法：原版 shader 名 → "WarpforgeVFX/Sprites/Additive"
//
// ✅ **2026-10-21（T17）按 Ruri 真反编译体逐行复核了一遍**（第一权威）：
//    `D:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/9e119372519374c758e1bb89f134fde8.shader`
//    ＝ `Shader "Everguild/Sprites/Sprite Additive"`，逐变体在 `9e119372…/` 下（6 个：PIXELSNAP × ETC1 四种组合）。
//    对出来的**真差异两处**（其余逐行吻合：VS 的 `COLOR*_Color*_RendererColor` 两道、UV 直通、
//    FS 的 `tex*COLOR` + 预乘 alpha、`Blend One One, One One` / `ZWrite Off` / `Cull Off` / `ZClip On`）：
//      ① `_Flip` —— 原版 VS **真的用它**：`POSITION.xy *= _Flip.xy` **在 ObjectToWorld 之前**
//         （`Sub0_Pass0_Vertex_b2.hlsl`）。我们以前只在 Properties 里声明、HLSL 侧零引用 ⇒ 这条已补。
//      ② ETC1 外部 alpha 的次序 —— 原版是 `lerp(tex.a, alphaTex.x, E) * COLOR.a`，我们原来把
//         `COLOR.a` 乘进了 lerp 的左端点（`Sub0_Pass0_Fragment_b10.hlsl`）。现状 keywords 全空 ⇒ 零变化。
//    ⚠️ ⛔ **任务给的 `D:/2/tools/assetripper_out/ExportedProject/Assets/` 里 218 个 `.shader` **全部**是
//      `//DummyShaderTextExporter` 占位**（全文件搜标记 218/218，含 `Shader/` 子目录那 149 个）；
//      真正的反编译体在上面的 Ruri 目录。那份 assetripper 目录只用来核 **Properties**
//      （它是真的，序列化自 `m_ParsedForm`）。
//    ⚠️ 原版 Properties 里 `_Color` 带 `[HDR]`（已照加）；`ZClip On` 是显式写的（我们没写 = 默认 On，等价）。
Shader "WarpforgeVFX/Sprites/Additive"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        // 原版 = `[HDR] _Color ("Tint", Color)`（判据 → 文件头 2026-10-21 那段）
        [HDR] _Color("Tint", Color) = (1,1,1,1)

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
                float4 _RendererColor;   // 🆕 2026-10-02：原版 vs 里乘了两道材质色（判据见文件头）
                // 🆕 2026-10-21（T17）：`_Flip` 以前只有 Properties 声明、**HLSL 侧完全没用**。
                //   原版是真的用它（判据见下面 `vert` 里那段）。放在本 CBUFFER 而不是原版的
                //   `UnityPerDrawSprite` —— 理由也写在 `vert` 那段里（我们的渲染器不是 SpriteRenderer）。
                float4 _Flip;
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

                // 🔴 2026-10-21（T17）`_Flip` 落地。判据（第一权威）= Ruri 反编译出的原版源码：
                //   `D:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/9e119372519374c758e1bb89f134fde8/Sub0_Pass0_Vertex_b2.hlsl`
                //   （= `Shader "Everguild/Sprites/Sprite Additive"` 的默认变体），逐行：
                //     `_53 = POSITION.x * _Flip.x` · `_54 = POSITION.y * _Flip.y`
                //     → **然后**才 `_54 * unity_ObjectToWorld[1u]` / `_53 * unity_ObjectToWorld[0u]`
                //   ⇒ **先翻【对象空间】的 xy、再乘 ObjectToWorld**（= Unity 自己的 `UnityFlipSprite(pos.xy, _Flip)`）。
                //   z 分量不进翻转（原版 `POSITION.z` 原样进矩阵）。
                // ⚠️ 原版把 `_Flip` 与 `_RendererColor` 一起放在 **`UnityPerDrawSprite`**（b3）：
                //     `cbuffer type_UnityPerDrawSprite : register(b3, space0) { float4 _RendererColor : packoffset(c0);
                //      float2 _Flip : packoffset(c1); }`
                //   我们**没有**照抄那个 cbuffer 名：这套 effect 的渲染器是 ParticleSystemRenderer / MeshRenderer，
                //   `UnityPerDrawSprite` 是 **SpriteRenderer 专属**的引擎侧 per-draw 常量缓冲，别的渲染器不灌
                //   ⇒ 照抄会让 `_RendererColor` 读成 0、整批 sprite 变黑（不可接受的回归）。
                //   放在 `UnityPerMaterial` 里，材质序列化值和 MaterialPropertyBlock 两条路都写得到。
                //   ⇒ **算式与原版逐行一致，cbuffer 位置取「值一定到得了」的那一处**；`_Flip` 真机待验（台账已记）。
                float3 posOS = IN.positionOS.xyz;
                posOS.xy *= _Flip.xy;
                OUT.positionCS = TransformObjectToHClip(posOS);
                // 原版 UV **直通**（`mov o2.xy, v2.xyxx`）—— 不做 TRANSFORM_TEX，这里跟着原版
                OUT.uv = IN.uv;
                // 原版 = `v1 * cb0[2] * cb3[0]`，即「顶点色 × _Color × _RendererColor」两道（判据见文件头）
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                    OUT.positionCS = UnityPixelSnap(OUT.positionCS);
                #endif
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 c   = tex * IN.color;

                // ETC1 外部 alpha（原版属性里有，一般不开）。
                // 🔴 原版取的是 `_AlphaTex` 的 **x（红）通道**，**不是 .a**
                // 🔴 2026-10-21（T17）就地订正：lerp 的**两个端点都是未乘顶点色的 alpha**，
                //    `_EnableExternalAlpha` 那一步之后**才**乘顶点色 alpha。判据 =
                //    `…/9e119372519374c758e1bb89f134fde8/Sub0_Pass0_Fragment_b10.hlsl` 逐行：
                //      `_56 = tex.w` · `_58 = AlphaTex.x - tex.w`
                //      `_80 = mad(_EnableExternalAlpha, _58, _56) * COLOR.w`   ← 先 lerp、后乘 COLOR.a
                //      `_81..83 = _80 * (tex.rgb * COLOR.rgb)`
                //    我们原来写成 `lerp(tex.a * COLOR.a, a.r, E)` ⇒ E ∈ (0,1) 且 COLOR.a ≠ 1 时**多算一项**
                //    （`E·alphaTex` 没被 COLOR.a 缩放）。所有材质 keywords 都是空的 ⇒ **现状零变化**。
                #ifdef ETC1_EXTERNAL_ALPHA
                    half4 a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.uv);
                    c.a = lerp(tex.a, a.r, _EnableExternalAlpha) * IN.color.a;
                #endif

                // 🔴 原版最后一步 = **预乘 alpha**：`mul o0.xyz, r0.wwww, r0.xyzx`（判据见文件头）。
                //    混合是写死的 `Blend One One` ⇒ 不预乘 = 「alpha 完全不参与衰减」，
                //    贴图半透明处会渲成硬边高亮 —— 这就是我们原来和原版差的那一处。
                return half4(c.rgb * c.a, c.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
