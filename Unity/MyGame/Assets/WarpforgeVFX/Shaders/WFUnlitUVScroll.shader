// WFUnlitUVScroll.shader — 自建，替代原版 `Everguild/FX/Unlit UV scroll`
//
// 🔴 **来由**（2026-10-02 第三批）：它原来被顶替成通用的 `WarpforgeVFX/Particles/Extra Color`
//    （只做「一张图 × 顶点色」）—— 而原版是**两张图各按自己的速度滚 UV、做一次 Overlay、再乘 `_Color` 和顶点色**。
//    兜底路上 `Recon Scan*` 那几家都在 0.44 上下，是靠它 + `Alpha Mask One Layer`。
//
// 判据（**全部实读**）—— `_tmp_view/dxbc/unlit_uvscroll{,_vs}.txt`（VS 37 条 / PS 38 条）
// ---------------------------------------------------------------------------
// ① **渲染状态**：pass0 的 `rtBlend0` 全是**属性引用**，但 15 个材质实测恒定
//    `_SrcBlend=5`/`_DstBlend=10`/`_SrcBlendAlpha=1`/`_DstBlendAlpha=10` ⇒ **写死 `Blend SrcAlpha OneMinusSrcAlpha`**；
//    `_ZWrite`=0 · `_ZTest`=4；`_Cull` **0 与 2 都有** ⇒ 留成属性 `Cull [_Cull]`。Queue=Transparent(3000)。
//    ⚠️ `_PREMULTIPLY` 在字节码里**不产生任何变体** ⇒ 它不改算式（别自己做预乘）。
//    ⚠️ 原版 5 个 pass（1=DepthOnly · 2=MotionVectors · 3=DepthNormals · 4=ShadowCaster，都是推的），本文件只做 pass0。
// ② **两张贴图都是 NoScaleOffset**（属性表里**没有** `_MainTex_ST`/`_SecondaryTex_ST`）⇒ **不做 `TRANSFORM_TEX`**。
// ③ **顶点**（vs 段 0，`t = cb0[19].x` = `_TimeParameters.x`）：
//      uvMain = uv0 · `Vector4_62056e…`.xy + frac(t · `Vector4_62056e…`.zw)   → 插值器 o2.zw → PS 里采 t0
//      uvSec  = uv0 · `Vector4_1`.xy        + frac(t · `Vector4_1`.zw)        → 插值器 o2.xy → PS 里采 t1
//    🔴 **只对偏移量取 `frac`，uv 不居中**（与 `WFMultiRay` 的 `frac(t·speed) + (scale·(uv−0.5)+0.5)` **不同**）。
//    ✅ **槽位归属已坐实（2026-10-02 晚，铁律 5 订正）** —— 不再是猜的。
//      **判据 = Unity 自己的序列化反射**（DXBC 段没有 RDEF 是真的，但这条数据不用 RDEF）：
//      `SerializedPass.progVertex.m_CommonParameters.m_ConstantBuffers[UnityPerMaterial].m_VectorParams`
//      里 **`m_Index` 是字节偏移**，按 pass 的 `m_NameIndices` 解名字 ⇒ pass 0：
//        `Vector4_1`                                off=48 → **cb[3]**
//        `Vector4_62056e41ff4042358d02be808b0352f9`  off=64 → **cb[4]**
//      对照 PS 同结构：`_Color` off=0 → `cb1[0]` · `_Layers_Blend_Opacity` off=80 → `cb1[5]`
//      —— 与 PS 字节码里的 `cb1[0]` / `cb1[5].x` **逐一对上** ⇒ 这套读法可信。
//      🔴 **原来这里写的是反的**（「`cb1[3]` = `Vector4_62056e…`」）—— 已订正为 **`cb[3]` = `Vector4_1`**。
//      ⇒ `.zw ← cb[3] = Vector4_1` · `.xy ← cb[4] = Vector4_62056e…`（`vert()` 里就是按这个写的）。
//      📌 **顺带记一条通用钥匙**：以后碰 `cbN[i]` 认不出名字的，**先试这条路**，别再去翻 DXBC 的 RDEF。
// ④ **片元**（ps 段 1 = `_SURFACE_TYPE_TRANSPARENT` 那支，逐条翻译）：
//      S0 = tex(t0, uvMain) · S1 = tex(t1, uvSec)         ; 都带 `_GlobalMipBias.x` 的 mip bias
//      C  = Overlay(base = S1, top = S0)                  ; (S1 ≤ 0.5) ? 2·S0·S1 : 1 − 2(1−S0)(1−S1)，**逐通道**
//      B  = lerp(S1, C, `_Layers_Blend_Opacity`)          ; 该属性**可 >1**（实测 1.43）⇒ 是外插，不是滑条
//      o0.rgb = B.rgb · **LinearToSRGB**(`_Color`.rgb) · vcol.rgb
//      o0.a   = saturate( B.a · `_Color`.a · vcol.a · `_FinalAlphaMultiplier` )
//    · **不透明支（ps 段 0）**：同上去掉 alpha 那条，`o0.a = 1.0` ⇒ 由 `_SURFACE_TYPE_TRANSPARENT` 分叉
//    · **`_SOFT` 支（ps 段 2，3 个材质用）**：t0 变深度图、两张彩图顺移 t1/t2，alpha 再乘
//      `pow(saturate((sceneEyeZ − fragEyeZ)/`_Depth_X_Falloff_Y`.x), `_Depth_X_Falloff_Y`.y)` ✓ 本文件实现
//      （深度取 `_CameraDepthTexture` + `LinearEyeDepth`；`fragEyeZ` 用世界位置经 `unity_MatrixVP` 求，与原版同法）
//    ⚠️ `_Color` **要过 `LinearToSRGB`**（编码，不是解码），常量逐字来自 DXBC。
// ⑤ **如实标注（判据不足）**：`_PREMULTIPLY` 到底改什么**没查到**（字节码里 0 变体、0 引用）；
//    本文件按「它就是不改算式」实现。
Shader "WarpforgeVFX/FX/UnlitUVScroll"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex("MainTex", 2D) = "white" {}
        _SecondaryTex("SecondaryTex", 2D) = "white" {}
        Vector4_62056e41ff4042358d02be808b0352f9("UV Scale (XY) Speed (ZW)", Vector) = (1, 1, 0.2, 0)
        Vector4_1("UV Scale (XY) Speed (ZW) 2", Vector) = (1, 1, 0.4, 0)
        _Layers_Blend_Opacity("Layers Blend Opacity", Float) = 0.5
        _FinalAlphaMultiplier("FinalAlphaMultiplier", Float) = 1
        _PREMULTIPLY("Premultiply", Float) = 0
        [Toggle(_SOFT)] _SOFT("Soft", Float) = 0
        _Depth_X_Falloff_Y("Depth (X) Falloff(Y)", Vector) = (0.5, 0.5, 0, 0)

        _Surface("__surface", Float) = 1
        _Blend("__blend", Float) = 0
        _AlphaClip("__clip", Float) = 0
        _SrcBlend("__src", Float) = 1
        _DstBlend("__dst", Float) = 0
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 0
        _ZWrite("__zw", Float) = 0
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 0
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = -1
        _CastShadows("_CastShadows", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        // 15 个材质实测恒定 5/10 —— 属性仍然声明（照原版属性表），但状态写死
        Blend SrcAlpha OneMinusSrcAlpha
        Cull [_Cull]
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "UnlitUVScroll"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // ⚠️ **必须有 pragma**：不声明的话下面那个 `#if` 恒为假（2026-10-02 在另一族踩过）
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;            // 原版没有这两个 _ST（NoScaleOffset）⇒ 只为占位
                float4 _SecondaryTex_ST;
                float4 _Color;
                float4 Vector4_62056e41ff4042358d02be808b0352f9;
                float4 Vector4_1;
                float  _Layers_Blend_Opacity;
                float  _FinalAlphaMultiplier;
                float  _PREMULTIPLY;
                // ⚠️ **这里【不能】声明 `float _SOFT;`** —— `_SOFT` 同时是**关键字**（`[Toggle(_SOFT)]` +
                //    下面的 pragma），Unity 开关键字时会 `#define _SOFT 1` ⇒ 与同名变量**重定义**、
                //    整份 shader 编不过（实测：exp 侧渲染成**品红**）。**只留 Properties 里那条**。
                float4 _Depth_X_Falloff_Y;
                float  _Surface, _Blend, _AlphaClip;
                float  _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha;
                float  _ZWrite, _ZWriteControl, _ZTest, _Cull, _AlphaToMask;
                float  _QueueOffset, _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_SecondaryTex); SAMPLER(sampler_SecondaryTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 uv0        : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : TEXCOORD0;   // ps 的 v1
                float4 uvScroll   : TEXCOORD1;   // ps 的 v2：xy=副图 · zw=主图
                float3 positionWS : TEXCOORD2;   // ps 的 v3（只有 _SOFT 支用）
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// linear → sRGB **编码**（原版 7 条；常量逐字来自 DXBC）
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            /// 一组滚动 UV：`uv·scale + frac(t·speed)`（**只对偏移取 frac、不居中**，照原版 vs）
            float2 ScrollUV(float2 uv, float4 scaleSpeed, float t)
            {
                return uv * scaleSpeed.xy + frac(t * scaleSpeed.zw);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.positionWS = posWS;
                OUT.color = IN.color;

                float t = _TimeParameters.x;                 // cb0[19].x
                OUT.uvScroll = float4(
                    ScrollUV(IN.uv0.xy, Vector4_62056e41ff4042358d02be808b0352f9, t),  // xy → t1 = _MainTex（cb[4]）
                    ScrollUV(IN.uv0.xy, Vector4_1, t));                                // zw → t0 = _SecondaryTex（cb[3]）
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 🔴 **2026-10-02 晚（第二轮订正，铁律 5）—— 这里推翻过两次，最后这版是硬判据。**
                //   **第一版错在**：只拿 `Recon Scan` 的「扇形 vs 条纹块」定案 ⇒ 把**两张贴图对调**。
                //   **第二版错在**：只拿 `StrikeEffect` 的「白块」定案 ⇒ 又把**两个 Vector4 的归属**当成对的没动。
                //   两次都犯同一个毛病（铁律 5·c）：**拿单个效果的单个现象去定两个独立的二值选择**。
                //
                //   ✅ **本轮把三件事分别坐实，各自独立**：
                //   ① **两个 Vector4 落在哪个槽** —— 判据 = **Unity 自己的序列化反射**（不用猜、也不是推的；
                //      DXBC 段没有 RDEF 是真的，但 `SerializedPass.progVertex.m_CommonParameters` 里有名字）：
                //        pass 0 · VS · `UnityPerMaterial`（size 160）：
                //          `Vector4_1`                                off=**48** ⇒ **cb[3]**
                //          `Vector4_62056e41ff4042358d02be808b0352f9`  off=**64** ⇒ **cb[4]**
                //      对照 PS：`_Color` off=0 ⇒ `cb1[0]` · `_Layers_Blend_Opacity` off=80 ⇒ `cb1[5]`
                //      —— 与 PS 段 1 字节码里的 `cb1[0]` / `cb1[5].x` **逐一对上**，说明这套读法可信。
                //      ⇒ **`.zw ← cb[3] = `Vector4_1`` · **`.xy ← cb[4] = `Vector4_62056e…``。
                //      ⚠️ **文件头 ③ 原来写的是反的**（「cb[3] = Vector4_62056e…」），本轮一并订正。
                //   ② **哪张图在 t0 / t1** + **谁是 Overlay 的 base** —— 判据 = DXBC（PS 段 1）：
                //        `sample_b r1, v2.zwzz, t0` · `sample_b r3, v2.xyxx, t1`；分支条件是 `ge 0.5, S1`
                //        ⇒ **base = S1 = t1**、**top = S0 = t0**。
                //   ③ **t0 / t1 各是哪张图名** —— 判据 = **两张图各自长什么样 + 原版渲出来什么样**：
                //        `_SecondaryTex` 在 `StrikeEffect` 里 = **`Glow.png`**（与同 prefab 的 URP 粒子 `_BaseMap`
                //          **同一 guid `31a107d9…`**）—— 一张**软径向光晕**，四角 `alpha=0`；
                //        `_MainTex` = `Strike_Sword/Skull/Background.png` —— **清晰的图徽**。
                //        · 原版里图徽是**清晰、不放大、不裁切**的 ⇒ 它必须走**静止那组** `Vector4_62056e…`
                //          （该材质 = `(1,1,0,0)`），也就是 **`.xy` ⇒ 图徽 = `t1` ⇒ `t1 = _MainTex`**。
                //        · 反过来验「谁是 base」：让 Glow 当 base 时，图徽四角 `RGB=白 / alpha=0`、
                //          而 Glow 那边 `alpha>0` ⇒ Overlay 的 lo 支 `2·top·base` 仍给出**白 RGB**、
                //          混合后就是并排图里那块**灰白方块**（见 `_tmp_view/cmp_crop2/`）。
                //          让**图徽当 base**（= 现在的写法）⇒ 图徽 alpha=0 处 `C.a = 2·S0.a·0 = 0` ⇒ **方块消失** ✓
                //      ⇒ **`t0 = _SecondaryTex`（Glow）· `t1 = _MainTex`（图徽）**。
                //   ⇒ 净结果：**贴图名不动（保持原写法），改的是「哪个 Vector4 喂给哪一组 UV」**。
                half4 S0 = SAMPLE_TEXTURE2D_BIAS(_SecondaryTex, sampler_SecondaryTex, IN.uvScroll.zw, _GlobalMipBias.x);
                half4 S1 = SAMPLE_TEXTURE2D_BIAS(_MainTex,      sampler_MainTex,      IN.uvScroll.xy, _GlobalMipBias.x);

                // Overlay(base = S1, top = S0)，逐通道
                half4 lo = 2.0h * S0 * S1;
                half4 hi = 1.0h - 2.0h * (1.0h - S0) * (1.0h - S1);
                half4 C  = half4(S1.r <= 0.5h ? lo.r : hi.r,
                                 S1.g <= 0.5h ? lo.g : hi.g,
                                 S1.b <= 0.5h ? lo.b : hi.b,
                                 S1.a <= 0.5h ? lo.a : hi.a);
                half4 B = lerp(S1, C, _Layers_Blend_Opacity);     // 可 >1（外插）

                half3 rgb = B.rgb * LinearToSrgb(_Color.rgb) * IN.color.rgb;

            #if defined(_SURFACE_TYPE_TRANSPARENT)
                half a = saturate(B.a * _Color.a * IN.color.a * _FinalAlphaMultiplier);
            #else
                half a = 1.0h;
            #endif

            #if defined(_SOFT)
                // 深度淡出：`pow(saturate((sceneEye − fragEye)/falloffX), falloffY)`
                float2 suv = IN.positionCS.xy / _ScaledScreenParams.xy;
                float  rawDepth = SAMPLE_TEXTURE2D_BIAS(_CameraDepthTexture, sampler_CameraDepthTexture,
                                                        suv, _GlobalMipBias.x).r;
                float  sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                // 🔴 **2026-10-02 晚订正（铁律 5）：下面 `fragEye` 原来多写了一个负号 —— 这是那两条
                //    `Leviathan` 环境效果（材质带 `_SOFT=1`）**恒亮 2.8 倍**的根因。**
                //    判据 = PS 段 2（`_SOFT` 支，`_tmp_view/dxbc/unlit_uvscroll.txt:144-252`）逐条：
                //      `mul r0.x, v3.y, cb0[79].w` · `mad r0.x, cb0[78].w, v3.x, r0.x`
                //      · `mad r0.x, cb0[80].w, v3.z, r0.x` · `add r0.x, r0.x, cb0[81].w`
                //    —— `cb0[78..81]` 是 **`unity_MatrixVP` 的四列**（§十 已坐实「矩阵按列放」），
                //    这四个 `.w` 取出来就是矩阵**第 3 行** ⇒ `r0.x = dot(row3, (x,y,z,1))` = **`clip.w`**。
                //    紧接着 `add r0.x, -r0.x, r0.z`（`r0.z = LinearEyeDepth` 的正距离）、**全程没有取负**。
                //    ✅ **物理上也只能是正的**：只有 `fragEye = clip.w > 0` 时，贴着场景表面的片元才有
                //      `sceneEye ≈ fragEye ⇒ fade → 0`（这才是软粒子淡出）；取负之后成了 `sceneEye + clip.w`，
                //      恒为正且很大 ⇒ `saturate` 恒 1、`pow(1, y) = 1` ⇒ **永不淡出、恒为全不透明**。
                //    ✅ **实测指纹吻合**：该材质 `_Depth_X_Falloff_Y = (0.29, 1.74)`；改前那两条 Leviathan
                //      在**全部 8 个时刻恒为 2.837 / 2.811，而 `lit` 几乎不变**（10578→10780）
                //      —— 是**逐像素乘性**偏差而非面积差，正是「该淡出的没淡出」
                //      （`_tmp_view/sweep_exp_自建1002e.tsv` vs `资料/比对基线/sweep_orig.tsv`）。
                float  fragEye  = mul(unity_MatrixVP, float4(IN.positionWS, 1.0)).w;   // = clip.w（**不取负**，见上）
                half   fade = saturate((sceneEye - fragEye) / max(1e-4, _Depth_X_Falloff_Y.x));
                a *= pow(fade, _Depth_X_Falloff_Y.y);
            #endif

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
