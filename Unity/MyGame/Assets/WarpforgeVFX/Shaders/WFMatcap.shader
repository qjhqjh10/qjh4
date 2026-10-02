// WFMatcap.shader — 自建，替代原版 Everguild/Matcap/Matcap Full Options 和 Matcap With Texture
//
// 🔴 **2026-10-02 订正（铁律 5）**：这里原来写着「两个原版 shader 共用同一份属性表（25 个）」——
//    **那句话是错的**，而且错得有代价（下面第三段说明）。判据 = `工具/dump_shader.py "<名>"` 实读：
//      · `Matcap With Texture`  → **25 个属性**（`_APPLYAMBIENTCOLOR` / `_ExtraAmbientColor` /
//        `_FogContribution` 是它独有的；`_APPLYAMBIENTCOLOR` **默认 1**、`_CastShadows` 默认 1）
//      · `Matcap Full Options` → **35 个属性**（多出 `_AlphaClipThreshold` / `_USEEMISSION` /
//        `_EmissionTex` / `_EmissionColor` / `_USENORMAL` / `_Normal` / `_USENOISE` / `_Noise` /
//        `_NOISECHANNEL` / `_BorderColor1` / `_BorderColor2` / `_BorderWidth` / `_DissolveAmount` 13 个；
//        `_CastShadows` 默认 0）
//    ⇒ 本文件现在声明的是**两者的并集**（多声明属性无害：材质只是把值带进来）。
//
// 原理：matcap 就是「从正面看一个球」的贴图，用**视图空间法线**的 xy 当 UV 去采样。
// 好处是完全不需要实时光照 —— 这也解释了为什么它在关卡里看着比周围亮。
//
// 🔴 **2026-10-02：算式按反编译逐条核过**（`工具/disasm_dxbc.py "Everguild/Matcap/Matcap Full Options"
//    --stage ps|vs`，产出 `_tmp_view/dxbc/Everguild_Matcap_*_{ps,vs}.txt`）。
//    **基础彩色变体**（`Matcap Full Options` ps 行 22–49，27 条指令）逐行：
//        r0 = LinearToSrgb(cb1[2].xyz)        ; ← _Color（**原版把它做了 linear→sRGB 编码**）
//        r0 = r0 * sample(t1, v1.xy).xyz      ; ← × _MainTex
//        r1 = sample(t0, (v3.w, v4.w))        ; ← _MatCap（UV 来自顶点阶段算好的插值器）
//        r1.xyz *= cb1[5].x                   ; ← × _Intensity
//        o0.xyz = r0 * r1.xyz ;  o0.w = 1.0   ; ← **输出 alpha 恒 1**
//    我们原来有三处不一样（都在下面 `frag` 里标了号）：
//      ① 没做 `_Color` 的 sRGB 编码（非 0/1 的颜色会偏暗）
//      ② 多乘了一个 `IN.color`（**原版基础变体一个颜色分量都没读**：它只用 v1.xy / v3.w / v4.w）
//      ③ 输出 alpha 用了 `_MainTex.a`（原版恒 1）
//    ✅ **2026-10-02 复刻掉一条**：`_APPLYAMBIENTCOLOR`（WT 那支）原来写的是个**发明的加法项**，
//      现在按反汇编改成原版的**乘性系数** `lerp(1, sRGB(_ExtraAmbientColor)×unity_AmbientSky,
//      _AmbientColorBlend)`，乘在**整条积**上（判据与逐条指令 → `frag` 里那一支的注释）。
//    ⚠️ **仍未复刻的**（12+ 个关键字分支，判据见 `资料/普查产出_0918/E组根因_B2.md` 与
//      `项目任务.md` §三 第 4 条）：`_USEEMISSION`（t2×EmissionColor **加法**项）·
//      `_USENOISE`+`_NOISECHANNEL_*`（溶解/边框：smoothstep + `_BorderColor1/2`）·
//      `_ALPHATEST_ON`（alpha = `_MainTex.a × _Color.a × _MatCap.a × 顶点色.a`，且连 rgb 一起乘）·
//      `_SURFACE_TYPE_TRANSPARENT` 的 alpha-to-coverage。
//      ⇒ **列在这里 = 要做**，不是「不做」。
//
// 状态（从 bundle 的 SerializedShaderState 读出，Opaque 队列）：
//   🔴 **2026-10-02 更正（铁律 5）**：这里原来写「Blend SrcAlpha OneMinusSrcAlpha」—— **是错的**。
//      原版状态**全是材质属性引用**，没有字面量：
//        `Blend [_SrcBlend][_DstBlend]` · `ZWrite [_ZWrite]` · `ZTest [_ZTest]` · `Cull [_Cull]`
//        · `alphaToMask [_AlphaToMask]` · `separateBlend=False`
//      而两个原版 shader 的属性默认值**都是** `_SrcBlend=1(One) / _DstBlend=0(Zero)` ⇒ **默认不透明**。
//      （判据：`工具/dump_shader.py "Matcap Full Options"` / `"Matcap With Texture"` 的属性表 + 同一份
//       输出里的 `rtBlend0`。）我们下面那两行 `Blend` 用的是 `[_SrcBlend][_DstBlend]`，这一层本来就对；
//      错的只是**属性的默认值**（5/10 → 已改 1/0）。
//      ⚠️ **还开着一条**：原版 `separateBlend=False`，而我们把 alpha 单列了一行
//      （`Blend [_SrcBlendAlpha][_DstBlendAlpha]`）。现有材质上 `_SrcBlendAlpha=1/_DstBlendAlpha=0`
//      ⇒ 两种写法**同值**、量不出差别；但「separateBlend=False 时 Unity 到底认不认那两个 alpha 字段」
//      **判据不足，没查到** ⇒ 先不动，记在这里。
//   Queue=Geometry, RenderType=Opaque, UniversalMaterialType=Unlit
Shader "WarpforgeVFX/Matcap/Matcap"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,0)      // ⚠️ a=0，照原版属性表（FO/WT 两族**都是** [1,1,1,0]）
        _MainTex("MainTex", 2D) = "white" {}
        _MatCap("MatCap", 2D) = "white" {}
        _Intensity("Intensity", Range(0, 5)) = 1

        // 🔴 2026-10-02 修：关键字名原来写 `_APPLYAMBIENTCOLOR_ON`，**原版是 `_APPLYAMBIENTCOLOR`（没有 _ON 后缀）**
        //    （原版关键字表逐字：`资料/普查产出_0917/shader属性表_块2.md:278`）⇒ binder 按原版名
        //    `EnableKeyword` 全部落空、这个开关永远是默认态。同族错见 `WFParticlesExtraColor`（`_SOFTPARTICLES`）。
        // 🔴🔴 **2026-10-02 同一天里踩了两次，第二次的教训更重要：默认值必须留 0。**
        //    我先照原版属性表把它从 0 改成 1（`Matcap With Texture` 里确实是 1）—— 结果 **A/B 对拍立刻现形**：
        //    `Goff_ProperKilly` 的两把 Choppa **渲成纯白**（原版是暗棕）。
        //    根因：`[Toggle(_APPLYAMBIENTCOLOR)]` 会让 Unity 在**建材质时按属性默认值同步关键字**
        //    ⇒ 默认值一改成 1，**每一份重建出来的材质都自动打开了这个关键字**，
        //    于是下面那个**发明的加法项** `col += _ExtraAmbientColor.rgb * main.a` 对**所有** matcap 材质生效
        //    （`_ExtraAmbientColor` 默认是纯白 ⇒ 直接加 1.0 ⇒ 饱和）。
        //    实测：`_Color`=1 · `_Intensity`=2 · 主图采样 ≤106/255 · matcap 采样 ≤247/255
        //    ⇒ 三者相乘**不可能**到 255；补上那 1.0 就正好。
        //    ⇒ **属性默认值保持 0**。原版那个 1 说的是**属性值**，不是**关键字**；
        //      真正带 `_APPLYAMBIENTCOLOR` 的材质（WT 那 70 个）由 binder 的 `EnableKeyword` 打开，
        //      不靠默认值。**要动这里，先把下面那一支换成原版的乘性系数**（见文件头「仍未复刻的」）。
        [Toggle(_APPLYAMBIENTCOLOR)] _APPLYAMBIENTCOLOR("ApplyAmbientColor", Float) = 0
        _ExtraAmbientColor("ExtraAmbientColor", Color) = (1,1,1,0)   // ⚠️ a=0，照原版属性表
        _FogContribution("FogContribution", Range(0, 1)) = 1

        // ---- `Matcap Full Options` 独有的 13 个（2026-10-02 照 `dump_shader.py` 补进并集）----
        _AlphaClipThreshold("AlphaClipThreshold", Range(0, 1)) = 0.5
        _USEEMISSION("UseEmission", Float) = 0
        _EmissionTex("EmissionTex", 2D) = "white" {}
        _EmissionColor("EmissionColor", Color) = (1,1,1,0)
        _USENORMAL("UseNormal", Float) = 0
        [Normal] _Normal("Normal", 2D) = "bump" {}
        _USENOISE("UseNoise", Float) = 0
        _Noise("Noise", 2D) = "white" {}
        _NOISECHANNEL("NoiseChannel", Float) = 0
        _BorderColor1("BorderColor1", Color) = (1,0,0,1)
        _BorderColor2("BorderColor2", Color) = (1,0.9082565,0,1)
        _BorderWidth("BorderWidth", Float) = 0.1
        _DissolveAmount("DissolveAmount", Range(0, 1)) = 0

        // ⚠️ 两个原版的默认值**不一样**（FO=0 / WT=1），并集里只能取一个 —— 取 FO 的 0
        //    （带它的效果多得多）。**这是一处如实标注的取舍**，不是抄漏。
        [HideInInspector] _CastShadows("_CastShadows", Float) = 0

        // 与原版同名的渲染状态（URP ShaderGraph 的标准一组）
        _Surface("__surface", Float) = 0
        _Blend("__blend", Float) = 0
        // 🔴 **2026-10-02 改：这一组默认值原来抄的是「URP 透明」那一套（5/10），原版两个 matcap
        //    shader（`Matcap Full Options` 35 属性 · `Matcap With Texture` 25 属性）**逐字都是**
        //    `_SrcBlend=1(One)` / `_DstBlend=0(Zero)` / `_SrcBlendAlpha=1` / `_DstBlendAlpha=0`
        //    ⇒ 默认就是**不透明**（Queue=Geometry=2000 · ZWrite=1）。出处：`工具/dump_shader.py` 的属性表。
        //    ⚠️ 实证：材质上**确实带着**这一组值（`Prefabs/Buff_Tyranid Armor.prefab:14940` 的
        //    `Tyranid_Claws` 有 `_SrcBlend/_DstBlend/_DstBlendAlpha/_ZTest/_Cull/_ZWrite`），
        //    binder 按名字灌得进来 ⇒ 改成原版默认**不改变现有画面**，只修正「材质没写这一条」时的行为。
        _SrcBlend("__src", Float) = 1      // One（原版默认；材质上那份也是 1）
        _DstBlend("__dst", Float) = 0      // Zero
        _SrcBlendAlpha("__srcA", Float) = 1
        _DstBlendAlpha("__dstA", Float) = 0
        _ZWrite("__zw", Float) = 1
        _ZWriteControl("__zwc", Float) = 0
        _ZTest("__zt", Float) = 4
        _Cull("__cull", Float) = 2
        _AlphaClip("__clip", Float) = 0
        _Cutoff("__cut", Range(0, 1)) = 0.5
        _AlphaToMask("__atm", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = -1      // 原版两个 matcap shader 都是 −1（我们原来是 0）
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _APPLYAMBIENTCOLOR
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _MatCap_ST;
                float4 _ExtraAmbientColor;
                float  _Intensity;
                float  _FogContribution;
                float  _Cutoff;
                // 🆕 2026-10-02：`Matcap Full Options` 独有的那 13 个（并集，见文件头）
                float4 _EmissionTex_ST;
                float4 _EmissionColor;
                float4 _Normal_ST;
                float4 _Noise_ST;
                float4 _BorderColor1;
                float4 _BorderColor2;
                float  _AlphaClipThreshold;
                float  _USEEMISSION;
                float  _USENORMAL;
                float  _USENOISE;
                float  _NOISECHANNEL;
                float  _BorderWidth;
                float  _DissolveAmount;
                float  _Surface; float _Blend;
                float  _SrcBlend; float _DstBlend;
                float  _SrcBlendAlpha; float _DstBlendAlpha;
                float  _ZWrite; float _ZWriteControl; float _ZTest; float _Cull;
                float  _AlphaClip; float _AlphaToMask;
                float  _QueueOffset; float  _QueueControl;
                float  _CastShadows;
            CBUFFER_END

            // ⚠️ **在 CBUFFER 外**：这是**进程级全局**（`Shader.SetGlobalFloat`），不是材质属性 ——
            //    放进 `UnityPerMaterial` 会被 SRP Batcher 当材质量、永远读不到游戏灌的值。
            //    灌它的是 `CardPresentation/Battle/ArenaEnvGlobal.cs`（原版 = `ApplyAmbientColor`）。
            float _AmbientColorBlend;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MatCap);
            SAMPLER(sampler_MatCap);

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
                float2 matcapUV   : TEXCOORD1;
                half4  color      : COLOR;
                float  fogFactor  : TEXCOORD2;
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
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color;

                // matcap 的核心：视图空间法线的 xy 拿来当 UV。
                // 法线要转到**观察空间**（view space），不是世界空间
                float3 normalVS = TransformWorldToViewDir(n.normalWS, true);
                OUT.matcapUV = normalVS.xy * 0.5 + 0.5;

                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            /// linear → sRGB **编码**（照抄原版基础变体开头那 7 条指令；常量逐字来自 DXBC）。
            /// ⚠️ 方向别弄反：`1.055·c^(1/2.4) − 0.055` 是 **OETF（编码）**，不是解码。
            ///    分支条件也是原版的：`ge 0.003131, c` ⇒ **c ≤ 0.003131（含负数）走 `c × 12.923210`**。
            half3 LinearToSrgb(half3 c)
            {
                half3 hi = 1.055000h * pow(abs(c), 0.416667h) - 0.055000h;
                half3 lo = c * 12.923210h;
                return half3(c.r <= 0.003131h ? lo.r : hi.r,
                             c.g <= 0.003131h ? lo.g : hi.g,
                             c.b <= 0.003131h ? lo.b : hi.b);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 🔴 原版基础变体（`Matcap Full Options` ps 行 22–49，判据见文件头）：
                //      r0 = LinearToSrgb(_Color) * _MainTex.rgb
                //      r1 = _MatCap.rgb * _Intensity
                //      o0 = (r0 * r1, **1.0**)
                // 相对我们原来那版的三处改动：① 补 `_Color` 的 sRGB 编码 ② 去掉多乘的 `IN.color`
                // （原版基础变体**一个颜色分量都没读**）③ 输出 alpha 从 `main.a` 改成常量 1。
                half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 cap4 = SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, IN.matcapUV);
                half3 col  = LinearToSrgb(_Color.rgb) * main.rgb * cap4.rgb * _Intensity;

                #ifdef _APPLYAMBIENTCOLOR
                    // ✅ **2026-10-02 复刻**（原来这里是个**发明的加法项** `col += _ExtraAmbientColor.rgb * main.a`）。
                    // 判据 = `Matcap With Texture` 的 ambient 支逐条指令（比 base 支多出 10 条，
                    // 反汇编 `d:/4/_tmp_view/dxbc/matcap_with_tex.txt:76-135`）：
                    //     r1 = LinearToSrgb(_ExtraAmbientColor.rgb)          ; 103-109（与 :35-41 对 _Color 的同一段）
                    //     r1 = r1 * unity_AmbientSky.rgb + (−1)              ; 110   ← cb0[56] = unity_AmbientSky
                    //     r1 = _AmbientColorBlend * r1 + 1                   ; 111   ← cb0[130].x
                    //     o0.rgb = r0 · r1  再 lerp 到雾色(v2.xyz, 系数 v2.w)  ; 112-114（本变体 v2.w=0 ⇒ 就是 r0·r1）
                    //   ⇒ **tint 是乘在「四个因子全乘完的整条积」上（含 _MainTex），且乘在最后那次 lerp 之前**。
                    //   ⇒ `_AmbientColorBlend` 是**进程级全局**（游戏 `ApplyAmbientColor` 灌，我们这边的
                    //      对应件是 `ArenaEnvGlobal`/`EnvironmentApplier`），**不是材质属性** ⇒ 声明在 CBUFFER 外。
                    // ⚠️ 13 个战场里它是 0 ⇒ tint ≡ 1（那一层是恒等）—— 但**非零的下标下必须对**。
                    half3 amb  = LinearToSrgb(_ExtraAmbientColor.rgb);
                    col *= (1.0h + _AmbientColorBlend * (amb * unity_AmbientSky.rgb - 1.0h));
                #endif

                // ⚠️ 原版彩色段里**没有雾算式**（`_FogContribution` 在 ps 里 0 次引用）—— 同属待复刻。
                col = MixFog(col, IN.fogFactor * _FogContribution);

                // 🔬 临时探针已撤（2026-10-02）—— 结论见下面 `_APPLYAMBIENTCOLOR` 那条注释。
                half alpha = 1.0h;                     // ← 原版基础变体：`mov o0.w, l(1.000000)`
                #ifdef _ALPHATEST_ON
                    // 原版 alpha 分支：alpha = `_MainTex.a × _Color.a × _MatCap.a × 顶点色.a`，
                    // 再 `clip(alpha − 阈值)`。阈值用**原版的属性名** `_AlphaClipThreshold`（不是 `_Cutoff`）。
                    alpha = main.a * _Color.a * cap4.a * IN.color.a;
                    clip(alpha - _AlphaClipThreshold);
                #endif

                return half4(col, alpha);
            }
            ENDHLSL
        }

        // 影子投射。原版 _CastShadows 控制，粒子一般不开，留一个以免报缺 pass
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                // ⚠️ 这个 CBUFFER 必须和主 pass 的**逐字段同序**（Unity 的规矩：同一 shader 的
                //    `UnityPerMaterial` 在各 pass 里布局要一致）。改主 pass 那个时这里要一起改。
                float4 _Color; float4 _MainTex_ST; float4 _MatCap_ST; float4 _ExtraAmbientColor;
                float _Intensity; float _FogContribution; float _Cutoff;
                float4 _EmissionTex_ST; float4 _EmissionColor; float4 _Normal_ST; float4 _Noise_ST;
                float4 _BorderColor1; float4 _BorderColor2;
                float _AlphaClipThreshold; float _USEEMISSION; float _USENORMAL; float _USENOISE;
                float _NOISECHANNEL; float _BorderWidth; float _DissolveAmount;
                float _Surface; float _Blend; float _SrcBlend; float _DstBlend;
                float _SrcBlendAlpha; float _DstBlendAlpha; float _ZWrite;
                float _ZWriteControl; float _ZTest; float _Cull;
                float _AlphaClip; float _AlphaToMask;
                float _QueueOffset; float _QueueControl; float _CastShadows;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct SA { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct SV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            SV ShadowVert(SA IN)
            {
                SV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return o;
            }

            half4 ShadowFrag(SV IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a * _Color.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
