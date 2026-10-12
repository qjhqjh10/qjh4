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
//    ② **原版那些天空网格没有顶点色**（`Background Sky` 一族的 `m_Channels` 里颜色通道 `dimension = 0`），
//       而我们读 `IN.color` 渲出纯白 ⇒ 默认值是 `(1,1,1,1)` ⇒ 原版若走顶点色那支也会白 —— 它不是。
//       ⚠️ **2026-10-12 更正：那是【那一族】的情况，不是全称** —— Flickker 的烛光网格**有** ch3
//       （`Candles 213`：dim=4）；判据与后果 → `frag` 里那条「2026-10-12 就地订正」。
//    改后 `1.766 → 0.971 / 1.013`。
//
// ✅ **2026-10-12：下面那三条「还没做的」全做完了**（判据 = 原版 DXBC + Unity 序列化反射，逐条在下面）。
//    🔑 **谁该有、谁不该有 —— 先看这张表**（用 `dump_shader_blob.blob_of()` 把**两份 shader 的 blob 分开**数：
//      按名字子串 `disasm_dxbc.py` 会把两份都命中，**分不开**，所以这一轮是自己写脚本按 blob 数的）：
//      · `Everguild/UnlitAmbient Emissive Flickker`：**32 段 · 4 段带第二张图（`sampler s1`）· 3 段有闪烁块 · 0 段有 derivative**
//      · `Everguild/UnlitAmbient`                ：**336 段 · 0 段有闪烁块 · 12 段有 derivative**
//    ⇒ **闪烁只在 Flickker 那份；alpha-to-coverage 只在基础那份。** 两份共用我们这一个自建 shader
//      ⇒ **闪烁必须闸起来**（见 `_EmissiveFlickerOn`）；alpha-to-coverage 不用闸。
//
// ① `Emissive Flickker` 整支（判据 = `_tmp_view/dxbc/Everguild_UnlitAmbient_ps.txt` 段 0/1，逐字）：
//      log/mul/exp/mad/ge/mul/movc  →  r0 = LinearToSrgb(cb1[6])          ; cb1[6] = `_EmissiveColor`
//      mul  r0.xyz, r0.xyzx, v2.xyzx                                      ; × 顶点色（v2）
//      div  r0.w, cb0[19].x, cb1[7].x                                     ; t / `_FlickerSpeed`
//      sample_b r1.xyzw, r0.wwww, t0.xyzw, s0                             ; 噪声图，UV = float2(t,t)/speed
//      add  r0.w, -cb1[7].y, cb1[7].z                                     ; (max - min)
//      mad  r0.w, r1.x, r0.w, cb1[7].y                                    ; k = lerp(min, max, n)
//      … 主图那一路 …  `mad o0.xyz, r0.wwww, r0.xyzx, r1.xyzx`             ; 🔴 **加法项、最后一条**（不是乘、不是 lerp）
//    **槽位铁证**（`progFragment.m_CommonParameters.m_ConstantBuffers`，2026-10-12 实读；字节偏移 `slot = off/16`）：
//      `_Color` off=80 → cb1[5] · `_EmissiveColor` off=96 → cb1[6] ·
//      `_FlickerSpeed` off=112 (dim=1) → **cb1[7].x** · `_FlickerMinMaxRange` off=116 (dim=2) → **cb1[7].yz**
//      ⇒ 与指令里的 `cb1[5]` / `cb1[6]` / `cb1[7].x` / `cb1[7].yz` **逐一吻合**。
//      （`cb1` 的 N 号即 `m_ConstantBufferBindings` 的 index；此处 = `UnityPerMaterial`，size 192。）
//    ⚠️ `_FlickerMinMaxRange` 是**只用了两个分量**的 Vector ⇒ **它的 `.x` = min、`.y` = max**
//      （在寄存器里落在 `.y`/`.z`，所以汇编是 `-cb1[7].y + cb1[7].z`）。材质实值 `(0,1,0,0)`／`(0,0.8,0,0)` ⇒ k ∈ [0,1]。
//    ⚠️ `cb0[19].x` = `_TimeParameters.x`（`$Globals` 槽位表 → `资料/特效还原_进度与交接.md` §十）。
//    ⚠️ **原版这一支不是 keyword 门控的**（那 4 个前向变体逐字都有它）—— 隔离靠**我们自己的闸**
//      `_EmissiveFlickerOn`（原版没有这个 uniform；由 `WarpforgeEffectBinder` 按**原版 shader 名**灌）。
// ② 第二张图 = **与 ① 同一支**：`_SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D`
//    就是那张闪烁噪声图。原版材质里**确实赋了值**（判据：`d:/2/解包整理/07_场景/battlearenasororitas/Material/Material_4.json`
//    的 `m_TexEnvs` 里有这个名字）—— 原来「声明了但从没采样」说的是我们这份。
//    ⚠️ 原版的寄存器顺序与基础那份**相反**：Flickker 里 **t0 = 噪声 · t1 = `_BaseMap`**
//      （我们按**名字**取，不受寄存器号影响）。
//    ✅ **噪声图 = `Noise Combined`**（128²·Repeat·无 mip·`m_ColorSpace: 0` 线性数据贴图）；
//    ⚠️ 工程里那张同名 png 现导入成 **sRGB** ⇒ 采样值偏亮（`资料/战场13场_逐场对账_0920.md` §8 已记）。
//    🔴 **战场清单只记了 `_BaseMap`** ⇒ 战场那条路**绑不上噪声图**（`n` = 1 ⇒ `k` 恒为 max = 「只亮不闪」）。
//       ⛔ 修它要动 `gen_arena_*.py`（**另一条线、本轮白名单外**）⇒ 只记不改。
// ③ 手写 derivative 的 alpha-to-coverage（判据 = **URP 自己的实现**，不是猜的）：
//    `…/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl:181-216` 的
//    `IsAlphaToMaskAvailable()` / `SharpenAlphaStrict()` / `AlphaClip()` —— 与原版段 23/24 的**每条指令逐一吻合**：
//      `SharpenAlphaStrict = saturate(((alpha - cutoff - 0.5*fwidth(alpha)) / max(fwidth(alpha), 0.0001)) + 1)`
//      `clipVal = (a2c && cutoff > 0) ? (a2cAlpha - 0.0001) : (alpha - cutoff)`
//      `outputAlpha = a2c ? a2cAlpha : alpha`   ← 段 24 就是这一版（段 23 的 else 支被折成了 `l(1.0)`，另一支变体）
//    ⇒ 直接调 `AlphaDiscard(alpha, _ClipThreshold)`，**别再手写**（原来那句 `clip(tex.a*_Color.a - _ClipThreshold)`
//      在 `_AlphaToMaskAvailable == 0` 时与它**逐位相同** ⇒ 这次改动对现状是**恒等的**）。
//
// ⚠️ **仍然如实记着（别当已查清）**：
//    · **一支带 `COLOR` 的 VS 变体**：本 shader **确实有**（VS 段 0/1/2：`mov o2.xyzw, v4.xyzw` + `mov o3.xyz, cb0[61].xyzx`）。
//      ⇒ ① 里那个 `v2.xyz` 就是**顶点色**（无 COLOR 时换成 `(cb0[61].xyz, 0)`；`cb0[61]` 按 `$Globals` 槽位表推 = `unity_FogColor`，
//      **未坐实**）。原版那些烛光网格 `m_Channels` 里**有** ch3（4 维颜色）⇒ 走的是 COLOR 那支。
//      ⛔ **我们的 OBJ 导出链把颜色通道丢了** ⇒ `IN.color` 落到 (1,1,1,1)（详见报告「顺手发现」）。
//    · `_EmissiveColor` 的**原版声明默认值 = (2.113019, 2.297397, 0, 0)**（`dump_shader.py` 实读，**HDR**）。
Shader "WarpforgeVFX/UnlitAmbient"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _BaseMap("BaseMap", 2D) = "white" {}
        _ClipThreshold("ClipThreshold", Range(0, 1)) = 0.5

        // 🔴 2026-10-12 更正（铁律 5）：原来写 `(0,0,0,1)`；**原版声明的默认值是 `(2.113019, 2.297397, 0, 0)`**
        //   （`dump_shader.py "UnlitAmbient"` 实读，**HDR**）。只在「材质没显式设过它」时可见 ——
        //   而**只有 `Emissive Flickker` 那份原版的材质**会走到这条支（见下面 `_EmissiveFlickerOn`）。
        _EmissiveColor("EmissiveColor", Color) = (2.113019, 2.297397, 0, 0)
        // 原版声明默认值 = **5.0**（实读），原来写 1。
        _FlickerSpeed("FlickerSpeed", Float) = 5
        // 🔴 **2026-10-12 新加的闸（原版没有这个 uniform）**：原版 `…Emissive Flickker` 的闪烁那一支
        //   **不是 keyword 门控的**（4 个前向变体逐字都有），而基础那份 `Everguild/UnlitAmbient` **一条都没有**
        //   ⇒ 两份原版共用我们这一个 shader，只能按**原版 shader 名**把它闸掉。
        //   灌它的地方 = `WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs`（那一处有全部理由）。
        //   ⚠️ **用浮点、不用 keyword**：keyword 在 player 构建里可能被剥掉变体，而这个 keyword
        //      **没有材质数据能替它保活**（`_APPLYAMBIENTCOLOR` 那批是原版材质自己带的）。
        [HideInInspector] _EmissiveFlickerOn("EmissiveFlickerOn (ours, not original)", Float) = 0
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
                float  _EmissiveFlickerOn;   // 🔴 2026-10-12：我们加的闸（原版没有它）
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

            // 🔴 2026-10-12：原版 Flickker 那份的**第二张图 = 闪烁噪声图**（原版 PS 的 `t0`；我们原来只声明、从没采样）。
            //    属性名逐字是原版的（ShaderGraph 自动生成）—— 我们的 Properties 里也一直是它。
            //    ⚠️ 采样器名 = `sampler` + 属性名（两种拼读法在这里得到同一个串：`sampler_SampleTexture2D_…`）。
            TEXTURE2D(_SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D);
            SAMPLER(sampler_SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D);

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
                //    🔴 **2026-10-12 就地订正（铁律 5）**：上面那句「**我们这 12 个材质不是那一支**」**只对了一半**。
                //       现核（`battlesharedresources_assets_all.bundle` 实读 `m_VertexData.m_Channels`）：
                //       · `Background Sky` / `Background_sky1` 那一族 —— 颜色通道 `dimension = 0` ⇒ **确实没有顶点色** ✓ 原结论成立；
                //       · 🔴 **但 Flickker 的烛光网格有**（`Candles 213`：ch1 = 法线 · **ch3 dim=4 = 颜色** · ch4 = UV0）
                //         ⇒ 那些网格走的是**带 `COLOR` 的那支 VS**。
                //       **错因**：当时只读了天空那一族就下了全称结论。**后果与处置**（别只看这半句）：
                //       · Flickker 的 PS 里那个 lerp 用的是 **`v3`**，而 COLOR 那支 VS 把 `o3` 写成
                //         `(cb0[61].xyz, 0)`（`o3.w = 0`）⇒ **对 Flickker 仍然是空操作** ✓ 本行不改；
                //       · 但**基础那份** `Everguild/UnlitAmbient` 的 PS 用的是 **`v2`**，COLOR 那支 VS 把 `o2` 写成
                //         **真正的顶点色**（`mov o2.xyzw, v4.xyzw`）⇒ 那种网格上 `lerp(tex, 顶点色, 顶点色.a)` **不是空操作**
                //         ⇒ **本行对「基础 shader + 带顶点色的网格」是取近似**。⚠️ **还没查清**（要查：那 8 个基础材质
                //         落在哪些网格上、那些网格的颜色通道是什么）⇒ 记成新账，**本轮不擅自改**。
                //       · 🔴 另一条并列发现：**我们的 OBJ 导出链把颜色通道整个丢了**（烛光那个 OBJ 只有 3 个 float/顶点）
                //         ⇒ 本 shader 拿到的 `IN.color` 是 (1,1,1,1) ⇒ ① 那条自发光会乘上白色而不是真顶点色。
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

                // ================= ① `Emissive Flickker` 整支 ＋ ② 第二张图（2026-10-12 新做）=================
                // 判据逐条 → 文件头「2026-10-12」那一节（原版 PS 段 0/1 的指令 + Unity 反射实读的槽位）。
                // 🔴 **加法项、最后一条** —— 原版是 `mad o0.xyz, r0.w, r0.xyzx, r1.xyzx`（k·em + col），
                //    位置在 `× _Color` 与 `_RECEIVESHADOWS` **之后**，而且**不吃雾**
                //    （所以这里摆在 `MixFog` 之后，与原版「最后一条」一致）。
                // ⚠️ 闸 `_EmissiveFlickerOn` 是我们加的（原版没有）—— 原版这一支**不是 keyword 门控的**，
                //    而基础那份 `Everguild/UnlitAmbient` 里一条都没有 ⇒ 只能按原版 shader 名闸。
                if (_EmissiveFlickerOn > 0.5)
                {
                    float  t   = _TimeParameters.x;                       // cb0[19].x（$Globals 槽位表）
                    float2 nuv = float2(t, t) / max(_FlickerSpeed, 1e-4); // div r0.w, cb0[19].x, cb1[7].x
                    half   n   = SAMPLE_TEXTURE2D(
                                     _SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D,
                                     sampler_SampleTexture2D_56151bd863ba4ebfae5e17e92f13cf49_Texture_1_Texture2D,
                                     nuv).x;                              // 只取 **.x**（原版 `r1.x`）
                    half   k   = lerp(_FlickerMinMaxRange.x, _FlickerMinMaxRange.y, n); // = n*(max-min)+min
                    col += k * LinearToSrgb(_EmissiveColor.rgb) * IN.color.rgb;
                }

                // 🔴 2026-10-12 修 **③**：原来那句是 `#ifdef _ALPHATEST_ON clip(tex.a*_Color.a - _ClipThreshold);`
                //    —— **手写 derivative 的 alpha-to-coverage 那一支没做**。现在改成调 URP 自己的
                //    `AlphaDiscard`（判据 = `ShaderVariablesFunctions.hlsl:181-216`，与原版段 23/24 逐条吻合）。
                //    · `_ALPHATEST_ON` 由 `AlphaDiscard` **内部判**（关着时返回入参、不裁）⇒ 这里不用再包 `#ifdef`。
                //    · `_AlphaToMaskAvailable == 0`（没开 MSAA/alpha-to-mask）时 `clipVal = alpha - cutoff`
                //      ⇒ **与改动前逐位相同**（改动对现状是恒等的；见文件头 ③）。
                return half4(col, AlphaDiscard(tex.a * _Color.a, _ClipThreshold));
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
                float4 _ExtraAmbientColor; float _ClipThreshold; float _FlickerSpeed;
                // ⚠️ 这一份 CBUFFER 必须与 pass 0 **逐字段同序**（SRP Batcher 兼容要求；顺序不一致 = 同一个属性在
                //    两个 pass 里落在不同偏移 ⇒ 静默退回旧批处理，而且影子 pass 会读到隔壁字段的值）
                float _EmissiveFlickerOn; float _FogContribution;
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
