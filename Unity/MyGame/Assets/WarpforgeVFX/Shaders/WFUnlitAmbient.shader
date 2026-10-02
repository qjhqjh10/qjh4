// WFUnlitAmbient.shader — 自建，替代原版 Everguild/UnlitAmbient 与
// Everguild/UnlitAmbient Emissive Flickker
//
// 这两个是**场景贴图烘焙用的非粒子 shader**（29 个属性），不是粒子 shader。
// 不映射的话会掉进默认的 URP **Particles**/Unlit —— 属性名对不上，
// `_BaseMap` 拿不到图 → 用默认白图渲染成**一整块纯白矩形**（实测过曝 4.6 倍）。
//
// 状态（从 bundle 读出）：Opaque 队列，ZWrite On，ZTest LEqual，Cull Off
//
// 属性名与原版一一对应，原版材质的数值和贴图可以原样灌进来。
//
// 🔴 **2026-10-02：算式按反编译核过一轮**（`工具/disasm_dxbc.py "Everguild/UnlitAmbient"
//    --stage ps|vs` → `_tmp_view/dxbc/Everguild_UnlitAmbient_{ps,vs}.txt`，812 段）。
//    **基础彩色变体**（14 条指令，ps）逐行：
//        sample_b r0.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x   ; 主图（UV 直通，无 _ST）
//        add  r1.xyz, -r0.xyzx, v2.xyzx
//        mad  r0.xyz, v2.wwww, r1.xyzx, r0.xyzx              ; = lerp(主图.rgb, v2.rgb, v2.w)
//        mul  o0.xyz, r0.xyzx, cb1[5].xyzx
//        mov  o0.w, l(1.000000)                              ; **基础变体输出 alpha 恒 1、不裁剪**
//    ⇒ 三处与我们不同：① 顶点色是 **lerp 的目标色**（不是乘）② 输出 alpha 恒 1
//      ③ 基础变体 **完全不 clip**（只有部分变体有 discard）。
//
// ✅ **2026-10-02 第二轮：上面那两条「要做的」做完了**（判据原文没变，还是文件头这几条指令）：
//    · **顶点色改成 lerp 的目标色** —— `col = lerp(tex.rgb, vcol.rgb, vcol.w) * _Color.rgb`
//      （原来是 `tex * _Color * vcol` 的乘法 ✗）。
//    · **`_APPLYAMBIENTCOLOR` 改成乘性 tint** —— `col *= lerp(1, sRGB(_ExtraAmbientColor)×unity_AmbientSky,
//      _AmbientColorBlend)`（原来是 `col += SampleSH(normalWS) * _ExtraAmbientColor` 的加法 ✗，
//      而且用的是 SH 不是 `unity_AmbientSky`）。**与 `WFMatcap.shader` 同一套算式**，
//      那边已靠 `EffectCompare` 的对照图坐实（原版渲出来是均匀的 `_Blend_Color` 色云）。
//
// ✅ **2026-10-02 晚：「`cb1[i]` 读不出来」这个卡点已解除**（原来是本项目最硬的拦路石）。
//    **办法 = 读 Unity 自己的序列化反射**，不用 RDEF、不用猜：
//      `SerializedPass.progVertex / progFragment .m_CommonParameters`，按该 pass 的 `m_NameIndices` 解名。
//      · `ConstantBuffer.m_VectorParams[].m_Index` **是字节偏移** ⇒ `slot = off / 16`
//      · `m_TextureParams[].m_Index` **直接就是纹理槽号**
//      · `m_ConstantBufferBindings[].m_Index` 就是 `cbN` 的 N
//    **本 shader 实读结果（pass 0）**：`_BaseMap` → **t0** · PS `UnityPerMaterial` 里
//      `_Color` off=80 ⇒ **slot 5** —— 与字节码 `sample_b r0, v1.xyxx, t0` / `mul o0.xyz, r0.xyzx, cb1[5].xyzx`
//      **逐一对上**（自检办法就是这个：拿它对已知量）。同样办法在 `WFUnlitUVScroll` 上解出
//      `Vector4_1`→slot 3 / `Vector4_62056e…`→slot 4（那里原来记反了，一并订正）。
//    ⇒ 🔴 **下一轮要动本文件时，先用这套把每个 `cb1[i]` / `tN` 读成名字，别再按用法猜。**
//
// 🔴 **2026-10-02 晚还修掉一条真错（下面 `frag` 里标了「顶点色」那条）**：
//    上一版把这一行写成 `lerp(tex.rgb, IN.color.rgb, IN.color.w)` ⇒ **12 个材质里凡用它渲天空的，
//    整片渲成纯白**，`EffectCompare` 实测 `|exp|/|orig| = 1.766`。判据两条（详见 `frag` 里的注释）：
//    ① 有一支 VS 变体的 Input signature **没有 `COLOR`**，`v2 = float4(cb0[61].xyz, **0**)` ⇒ lerp 是空操作；
//    ② **原版网格没有顶点色**（`m_Channels` 里颜色通道 `dimension = 0`），
//       而我们读 `IN.color` 渲出纯白 ⇒ 默认值是 `(1,1,1,1)` ⇒ 原版若走顶点色那支也会白 —— 它不是。
//    改后 `1.766 → 0.971 / 1.013`。
//
// ⚠️ **还没做的（判据已齐，按铁律 11 都要做，只有先后）**：
//    · `Emissive Flickker` 那整支（ramp 采样 + `[min,max]` 重映射；我们声明了
//      `_EmissiveColor/_FlickerSpeed/_FlickerMinMaxRange` 却在 HLSL 里**一个都没用**——
//      ⚠️ **现在有反射这条路，这一支的槽位能直接读出来了，别再拿「读不出槽位」当理由**
//      （它原来那句「顶点色 lerp（含 `_APPLYAMBIENTCOLOR` 那支的乘性系数 …）」**已做完，销账**）。
//    · 第二张图（`_SampleTexture2D_…_Texture2D` 声明了但**从没采样**）· 手写 derivative 的 alpha-to-coverage。
//    · ⚠️ **一支带 `COLOR` 的 VS 变体**：本 shader **确实有**（段 0/1/2，`mov o2.xyzw, v4.xyzw`），
//      我们这 12 个材质不是那一支。**将来碰到真有顶点色的网格要回来重判那一行。**
//    ⇒ 详细判据：`资料/特效还原_进度与交接.md` §十四 + `项目任务.md` §三 第 4 条。
Shader "WarpforgeVFX/UnlitAmbient"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _BaseMap("BaseMap", 2D) = "white" {}
        _ClipThreshold("ClipThreshold", Range(0, 1)) = 0.5

        _EmissiveColor("EmissiveColor", Color) = (0,0,0,1)
        _FlickerSpeed("FlickerSpeed", Float) = 1
        // 🔴 2026-09-30 更正：原版属性表里是 **`(0,1,0,0)`**（min=0），我们原来写成 `(0.8,1,0,0)`
        //   ⇒ 闪烁的下限被抬到 0.8、摆幅只有原版的三分之一。（判据 → `项目任务.md` §三 第 30 条 散件 C。）
        //   ⚠️ 这条**只影响兜底/自建这条路**：战场网格在运行时是用**原版 shader** 重建的
        //   （`ArenaOriginalMaterial`），`_FlickerMinMaxRange` 走材质里带过来的原值。
        _FlickerMinMaxRange("FlickerMinMaxRange", Vector) = (0, 1, 0, 0)

        // 🔴 2026-10-02 修：原来写 `_RECEIVESHADOWS_ON` / `_APPLYAMBIENTCOLOR_ON` ——
        //    **原版两个名字都没有 `_ON` 后缀**（原版关键字表逐字：`资料/普查产出_0917/shader属性表_块3.md:264`：
        //    `_APPLYAMBIENTCOLOR`, `_RECEIVESHADOWS`）⇒ binder 按原版名 EnableKeyword 全部落空。
        //    同族错见 `WFParticlesExtraColor`（`_SOFTPARTICLES`）。
        [Toggle(_RECEIVESHADOWS)] _RECEIVESHADOWS("ReceiveShadows", Float) = 0
        _FogContribution("FogContribution", Range(0, 1)) = 1

        [Toggle(_APPLYAMBIENTCOLOR)] _APPLYAMBIENTCOLOR("ApplyAmbientColor", Float) = 0
        _ExtraAmbientColor("ExtraAmbientColor", Color) = (1,1,1,1)
        _SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D("Texture2D", 2D) = "white" {}
        [HideInInspector] _CastShadows("_CastShadows", Float) = 0

        // 与原版同名的渲染状态（URP ShaderGraph 的标准一组）
        _Surface("__surface", Float) = 0
        _Blend("__blend", Float) = 0
        _SrcBlend("__src", Float) = 1
        _DstBlend("__dst", Float) = 0
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 1
        _ZWrite("__zw", Float) = 1
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 0
        _AlphaClip("__clip", Float) = 0
        _Cutoff("__cut", Range(0, 1)) = 0.5
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit"
        }

        Blend [_SrcBlend] [_DstBlend]
        Blend [_SrcBlendAlpha] [_DstBlendAlpha]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull [_Cull]
        AlphaToMask [_AlphaToMask]

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _APPLYAMBIENTCOLOR
            #pragma shader_feature_local_fragment _RECEIVESHADOWS
            // 🆕 2026-10-02：`frag` 里那个 `#ifdef _ALPHATEST_ON` 原来**没有对应的 pragma**
            //   ⇒ 恒假、恒走 `#else` 分支去 clip。补上它，裁不裁才真的由材质的关键字说了算。
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseMap_ST;
                float4 _EmissiveColor;
                float4 _FlickerMinMaxRange;
                float4 _ExtraAmbientColor;
                float  _ClipThreshold;
                float  _FlickerSpeed;
                float  _FogContribution;
                float  _Cutoff;
                float  _Surface; float _Blend;
                float  _SrcBlend; float _DstBlend;
                float  _SrcBlendAlpha; float _DstBlendAlpha;
                float  _ZWrite; float _ZWriteControl; float _ZTest; float _Cull;
                float  _AlphaClip; float _AlphaToMask;
                float  _QueueOffset; float  _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            // ⚠️ **在 CBUFFER 外**：**进程级全局**（`Shader.SetGlobalFloat`），不是材质属性。
            //    灌它的是 `CardPresentation/Battle/ArenaEnvGlobal.cs`（原版 = `ApplyAmbientColor`）。
            float _AmbientColorBlend;

            /// linear → sRGB **编码**（与 `WFMatcap.shader` / `WFTrailShader1.shader` 同一份常量；
            /// 原版这两支环境光算式里都是这 7 条：`log/×0.416667/exp/×1.055−0.055` + `ge 0.003131`/`×12.92321`）
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half4  color      : COLOR;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = n.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color      = IN.color;
                OUT.fogFactor  = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                // 🔴 **2026-10-02 晚订正（铁律 5）：上一版那句 `lerp(tex.rgb, IN.color.rgb, IN.color.w)`
                //    **把整片天空渲成纯白**（并排图 `_tmp_view/cmp_crop3/Leviathan_UA_三连.png` 的中图；
                //    `EffectCompare` 强制 `UnlitAmbient` 时全图 `|exp|/|orig| = 1.766`、43452 个像素差 > 12）。**
                //    判据（两条独立、同向）：
                //    ① **VS 的 `COLOR` 那支不是这个材质在用的那支。** VS 段 3 的 Input signature 里
                //       **根本没有 `COLOR`**（只有 POSITION / NORMAL / TANGENT / TEXCOORD0），它把 `v2` 换成了
                //       一个 `$Globals` 常量：`mov o2.xyz, cb0[61].xyzx` + `mov o2.w, l(0)`
                //       ⇒ **`v2 = float4(cb0[61].xyz, 0)`**。带着 `o2.w = 0` 回到 PS 基础变体（段 17，14 条）：
                //         `sample_b r0, v1.xyxx, t0` → `add r1, -r0, v2` → `mad r0, v2.w, r1, r0`
                //       ⇒ **`lerp(tex, v2, 0) ≡ tex`，那个 lerp 是空操作**；
                //       紧接着 `mul o0.xyz, r0.xyzx, cb1[5].xyzx`（`cb1[5]` = `_Color`
                //       —— Unity 序列化反射 `off=80 → slot 5` 已坐实，见文件头「读 cb1[i] 的办法」）。
                //    ② **原版网格压根没有顶点色** —— `battlesharedresources/Background_sky1` 与 13 个
                //       `Background Sky` 的 `m_VertexData.m_Channels` 里，颜色通道 **`dimension = 0`**（实读 bundle）。
                //       而我们读 `IN.color` 渲出来是**纯白** ⇒ Unity 对「网格缺颜色流」的默认值是 `(1,1,1,1)`
                //       ⇒ `lerp(tex, 1, 1) = 1`。**若原版真走顶点色那支，它也会是白的 —— 它不是** ⇒ 走的是 ① 那支。
                //    ⇒ 本行按「空操作」写。
                //    ⚠️ **仍如实记着（别当已查清）**：本 shader 确实**存在**一支带 `COLOR` 的 VS 变体
                //       （段 0/1/2，`mov o2.xyzw, v4.xyzw`）；我们这 12 个材质不是那一支。
                //       **将来碰到真有顶点色的网格（例如战场那些网格）要回来重判这一行。**
                //    ⚠️ `cb0[61]` **是什么名字没坐实** —— 但这里**不影响**：`v2.w = 0` 时 lerp 恒等于 `tex`，
                //       取值多少都一样（按 §十 的办法能算出来，要真用到时再算）。
                half3 col = tex.rgb * _Color.rgb;

                // ✅ **2026-10-02 修（原来是加法 `col += SampleSH(normalWS) * _ExtraAmbientColor.rgb`）**：
                //    判据与本工程 `ArenaEnvGlobal.cs` 的注释一致（那句注释就是照着这件 shader 写的）：
                //      `tint = lerp(1, LinearToSRGB(_ExtraAmbientColor) × unity_AmbientSky, _AmbientColorBlend)`
                //    与 `WFMatcap.shader` 的 `_APPLYAMBIENTCOLOR` 支**同一套算式**（那边 2026-10-02 已按这式子改过，
                //    并靠 `EffectCompare` 的对照图坐实）。⚠️ **是乘性、乘在整条积上**，不是加法、也不是 `SampleSH`。
                //    ⚠️ `_AmbientColorBlend` 是**进程级全局**（`Shader.SetGlobalFloat`，由 `ArenaEnvGlobal` 灌）
                //    ⇒ 声明在 CBUFFER 外；13 个战场里它是 0 ⇒ tint ≡ 1（那一层是恒等），但非零时必须对。
                #ifdef _APPLYAMBIENTCOLOR
                    half3 amb = LinearToSrgb(_ExtraAmbientColor.rgb);
                    col *= (1.0h + _AmbientColorBlend * (amb * unity_AmbientSky.rgb - 1.0h));
                #endif

                #ifdef _RECEIVESHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                    col *= lerp(1.0h, mainLight.shadowAttenuation, mainLight.shadowAttenuation < 1.0h);
                #endif

                col = MixFog(col, IN.fogFactor * _FogContribution);

                // 🔴 2026-10-02 修 **①**：这里原来是个 `#ifdef _ALPHATEST_ON / #else clip(...)` 的**双分支**，
                //    而本 shader **从来没声明过 `_ALPHATEST_ON` 这个 pragma** ⇒ 那个 `#ifdef` **恒假**、
                //    永远走 `#else` ⇒ **每一帧都按 alpha 裁一次**（原版基础变体里**一条 discard 都没有**，
                //    见文件头）。现在改成：**只在 `_ALPHATEST_ON` 真被打开时才裁**，并补上那条 pragma。
                #ifdef _ALPHATEST_ON
                    clip(tex.a * _Color.a - _ClipThreshold);
                #endif

                // 🔴 2026-10-02 修 **②**：去掉末尾的 `* IN.color.a`。
                //    判据：反汇编**全文件**里 `o0.w` 只有四种写法 —— `l(0)` / `l(1)` /
                //    「主图.w × cb1[5].w」/ `movc`；**顶点 alpha 一次都没进过 alpha 通道**。
                //    ⚠️ 原版**基础**变体是 `mov o0.w, l(1.000000)`（恒 1）。我们保留「主图.a × _Color.a」
                //    是因为实测**这些材质 10/12 带 `_SURFACE_TYPE_TRANSPARENT`**（带 alpha 的那支变体
                //    算的就是「主图.a × 一个材质的 alpha」）—— **这是取近似，不是原版基础变体**，
                //    如实标在这里（要坐实得先把 cb1[5] 是谁读出来，见文件头）。
                return half4(col, tex.a * _Color.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float4 _BaseMap_ST; float4 _EmissiveColor; float4 _FlickerMinMaxRange;
                float4 _ExtraAmbientColor; float _ClipThreshold; float _FlickerSpeed; float _FogContribution;
                float _Cutoff; float _Surface; float _Blend; float _SrcBlend; float _DstBlend;
                float _SrcBlendAlpha; float _DstBlendAlpha; float _ZWrite; float _ZWriteControl;
                float _ZTest; float _Cull; float _AlphaClip; float _AlphaToMask;
                float _QueueOffset; float _QueueControl; float _CastShadows;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct SA { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct SV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            SV ShadowVert(SA IN)
            {
                SV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(SV IN) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _Color.a - _ClipThreshold);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
