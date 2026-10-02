// WFParticleDissolveAPB.shader — 自建，替代原版 `Shader Graphs/Fx_ParticleDissolve_apb`
//
// 🔴 **来由**（2026-10-02，兜底路全量审计的第 2 族）：它原来和另外 5 个原版 shader 一起被
//    `Replacements` 映射到通用的 `WarpforgeVFX/Particles/Extra Color`（那是个**只乘**的简化版）
//    ⇒ 兜底路上 `BlastEffect` **×7.65**、`Invoke Minion *` ×4.4~4.5。
//    逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读，不是猜**）—— 三条来源，逐条标出处
// ---------------------------------------------------------------------------
// ① **渲染状态**（`工具/dump_shader.py "Fx_ParticleDissolve_apb"`，pass0）：
//      rtBlend0: srcBlend=5(SrcAlpha) destBlend=10(OneMinusSrcAlpha) separateBlend=False
//      zWrite=0 · zTest=4(LEqual) · cull=0(Off) · alphaToMask=0
//      SubShader tags: QUEUE=Transparent / RenderType=Transparent / RenderPipeline=UniversalPipeline
//                      ShaderGraphShader=true / ShaderGraphTargetId=UniversalUnlitSubTarget
//    ⇒ **混合硬编码 `Blend SrcAlpha OneMinusSrcAlpha`**，⛔ 不许 `[_SrcBlend]` 间接寻址
//      （会把内置 Standard 的残留值当活值用，`CLAUDE.md` 三那条）。
//    ⚠️ 原版共 4 个 pass（pass1 运动矢量 colMask=12 · pass2 深度 · pass3 影子），
//      本文件**只做 pass0**（同 `WFParticlesMultiply.shader` 的做法：那几个 pass 与画面无关）。
// ② **顶点流**（我们导出的 prefab：`Assets/WarpforgeVFX/Prefabs/BlastEffect.prefab:4898`
//      `m_VertexStreams: 0001030405` = Position(0)/Normal(1)/Color(3)/UV(4)/UV2(5)）：
//      原版 vs 的 ISGN 正好是 POSITION/NORMAL/TANGENT/TEXCOORD0/TEXCOORD1/COLOR 六条，
//      `dcl_input v0.xyz/v1.xyz/v3.xyzw/v4.xyzw/v5.xyzw` ⇒ **v3=UV→TEXCOORD0 · v4=UV2→TEXCOORD1 · v5=COLOR**。
// ③ **算式** = `工具/disasm_dxbc.py "Fx_ParticleDissolve_apb" --stage ps/vs` 的**全部**指令逐条翻译
//    （文件头下半部分逐行对照）。原版是 ShaderGraph，**HLSL 源码被剥**，所以只能从编译字节码读。
//
// 🔴 **cbuffer 槽位怎么定的（这一条对六族都通用，别再造轮子）**
//    DXBC 段**没有 RDEF**（22/22 实测）⇒ 槽位没有名字。但 `cb0` 是 **Unity 的 `$Globals`**：
//    它的布局 = **URP 的全局声明顺序**（`com.unity.render-pipelines.universal/ShaderLibrary/
//    Input.hlsl` 在前、`UnityInput.hlsl` 在后，逐个 float4 累加）。实测**逐个对上**：
//      cb0[3]        = `_ScaledScreenParams`   （Input.hlsl:108；原版拿它当屏幕尺寸除数）
//      cb0[4].x      = `_GlobalMipBias`        （Input.hlsl:112；原版拿它当采样 bias）
//      cb0[22].x     = `_ProjectionParams.x`   （UnityInput.hlsl:57；<0 表示投影翻转）
//      cb0[23]       = `_ScreenParams`         （UnityInput.hlsl:63）
//      cb0[24]       = `_ZBufferParams`        （UnityInput.hlsl:75；`1/(z*d+w)` = LinearEyeDepth）
//      cb0[25].w     = `unity_OrthoParams.w`   （UnityInput.hlsl:81；==1 表示正交相机）
//      cb0[28]       = `_RTHandleScale`        （UnityInput.hlsl:91；屏幕 UV 的 RTHandle 缩放）
//      cb0[66..69]   = `unity_MatrixV`         （UnityInput.hlsl:~230）
//      cb0[78..81]   = `unity_MatrixVP`        （**顶点着色器拿它算 clip pos，独立佐证**）
//      cb0[82..85]   = `unity_MatrixInvVP`
//      cb0[130]      = `_CameraDepthTexture_TexelSize`（引擎给每张纹理自动追加在 $Globals 末尾）
//    而 `cb1` 两侧含义不同：**vs 里是 `UnityPerDraw`**（cb1[0..3]=unity_ObjectToWorld、
//    cb1[4..6]=unity_WorldToObject 的 3x3）· **ps 里是 `UnityPerMaterial`**（只用了 2 槽，
//    其中 `cb1[1].x` = 材质上的 `Boolean_52F3CBA5`）。
//
// ── 顶点算式（原版 vs，逐条）──────────────────────────────────────────────
//   r0 = cb1[0..3] · v0  ⇒ 世界位置（`TransformObjectToWorld`）
//   o0 = cb0[78..81] · r0 ⇒ clip（`TransformWorldToHClip`）
//   o1 = v3(TEXCOORD0) · o2 = v4(TEXCOORD1) · o3 = v5(COLOR) · o4 = 世界位置 · o5 = 法线
//
// ── 片元算式（原版 ps，逐条；`v0..v4` 是插值后的）──────────────────────────
//   sp   = (v0.x, _ProjectionParams.x<0 ? _ScaledScreenParams.y−v0.y : v0.y) / _ScaledScreenParams.xy
//   pw   = (v4 · unity_MatrixVP).w      ; 粒子的 clip.w = 视空间深度（软粒子要用）
//   A    = Texture2D_F593E37E(v1.xy)
//   B    = Texture2D_F593E37E(v1.xy + (v1.x·v2.z·A.b, 0))
//   o0.rgb = B.r · v3.rgb               ; ⚠️ 只取 B 的 **r 通道**当灰度遮罩，不是 B.rgb
//   s    = saturate(v2.x·v2.y) ; z = s−1 ; z = v2.x·(1−z)+z
//   m    = saturate( saturate(B.a − saturate(z + 1 − B.g)) · (1+v2.y) )
//   d    = _CameraDepthTexture(min(sp{x,1−y}, 1−0.5·texel) · _RTHandleScale.xy).r
//   正交(_OrthoParams.w==1)：把 (2·sp{x,1−y}−1, d) 经 **unity_MatrixInvVP** 反投影回世界，
//                          再取 `|(world · unity_MatrixV).z|` 当场景深度
//   透视：                  sceneD = 1/(_ZBufferParams.z·d + _ZBufferParams.w)
//   fade = saturate((sceneD − pw) · v2.w) · m
//   o0.a = (_Boolean_52F3CBA5 ≠ 0 ? fade : m) · v3.a
//   ⚠️ **反投影那一步原版是 `−ndc.y`**（`mul r3, -r0.wwww, cb0[83]`）—— 逐字照抄，不是笔误。
//
// ⚠️ **属性名必须与原版一致**：原版材质的属性名是 ShaderGraph 的 obfuscated 名
//    （`Texture2D_F593E37E` / `Boolean_52F3CBA5`），`WarpforgeEffectBinder.Build` 只往
//    `m.HasProperty(名字)` 为真的属性里灌值 ⇒ **名字写错 = 静默收不到贴图和开关**。
//    （原版属性表 7 个里另外三个 unity_Lightmaps* 是 Unity 按光照贴图关键字自动加的，不用声明。）
// ⚠️ `[Toggle]` 一律不用：`Boolean_52F3CBA5` 在原版是**普通 Float**（不是关键字），
//    而 `[Toggle(KEY)]` 的属性默认值非 0 时 Unity **建材质会自动开关键字**（2026-10-02 踩过）。
//
// ⚠️ **它在白名单里**（`WarpforgeShaderMap.UseOriginal`）⇒ **运行时走原件**，
//    本文件只在**兜底路**（`wf_shaders_extra.bundle` 不在场，或 `WFBIND_FORCE_BUILTIN` 打开时）生效。
Shader "WarpforgeVFX/FX/ParticleDissolveAPB"
{
    Properties
    {
        // 原版 desc='MainTex'，flags=4（NoScaleOffset ⇒ 不做 TRANSFORM_TEX，原版也没做）
        Texture2D_F593E37E("MainTex", 2D) = "white" {}
        // 原版 desc='Use SoftParticleFactor?'，默认 0（材质上给的是 1）
        Boolean_52F3CBA5("Use SoftParticleFactor?", Float) = 0
        _QueueOffset("Queue offset", Float) = 0
        _QueueControl("__qc", Float) = -1
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

        // 原版写死在 pass 里（材质上没有 _SrcBlend）—— 不许改成间接寻址
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ParticleDissolveAPB"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float Boolean_52F3CBA5;
                float _QueueOffset;
                float _QueueControl;
            CBUFFER_END

            TEXTURE2D(Texture2D_F593E37E);
            SAMPLER(samplerTexture2D_F593E37E);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;    // 原版 vs 的 v1（只用来算 o5，ps 不用）
                float4 uv0        : TEXCOORD0; // 粒子流 UV  → 原版 v3 → ps 的 v1
                float4 uv1        : TEXCOORD1; // 粒子流 UV2 → 原版 v4 → ps 的 v2
                float4 color      : COLOR;     // 粒子流 Color → 原版 v5 → ps 的 v3
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv0        : TEXCOORD0;  // ps 的 v1
                float4 uv1        : TEXCOORD1;  // ps 的 v2
                float4 color      : TEXCOORD2;  // ps 的 v3
                float3 positionWS : TEXCOORD3;  // ps 的 v4（世界位置，原版在 ps 里重算 clip.w 用它）
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.positionWS = posWS;
                OUT.uv0 = IN.uv0;
                OUT.uv1 = IN.uv1;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ── 屏幕 UV（原版：`movc` 按 `_ProjectionParams.x < 0` 决定翻不翻 y）
                float2 sp = IN.positionCS.xy;
                float  sy = (_ProjectionParams.x < 0.0) ? (_ScaledScreenParams.y - sp.y) : sp.y;
                float2 r1 = float2(sp.x, sy) / _ScaledScreenParams.xy;

                // ── 粒子深度 = 世界位置的 clip.w（原版：`mul/mad` 三条 + `add cb0[81].w`）
                //    用 named global 取同一件事：mul(unity_MatrixVP, float4(posWS,1)).w
                float particleW = mul(unity_MatrixVP, float4(IN.positionWS, 1.0)).w;

                // ── 主贴图两次采样（第二张按 A.b 沿 u 方向偏移，是这张 graph 的「流动」那一步）
                float4 A = SAMPLE_TEXTURE2D_BIAS(Texture2D_F593E37E, samplerTexture2D_F593E37E,
                                                 IN.uv0.xy, _GlobalMipBias.x);
                float2 uvB = IN.uv0.xy + float2(IN.uv0.x * IN.uv1.z * A.b, 0.0);
                float4 B = SAMPLE_TEXTURE2D_BIAS(Texture2D_F593E37E, samplerTexture2D_F593E37E,
                                                 uvB, _GlobalMipBias.x);

                half3 rgb = B.r * IN.color.rgb;   // ⚠️ 标量 B.r × 顶点色，不是 B.rgb

                // ── 遮罩/溶解项 m（原版 6 条：mul_sat / add / add / mad / add_sat / add_sat / mad_sat）
                float s = saturate(IN.uv1.x * IN.uv1.y);
                float z = s - 1.0;
                z = IN.uv1.x * (1.0 - z) + z;
                float m = saturate(z + (1.0 - B.g));
                m = saturate(B.a - m);
                m = saturate(m * IN.uv1.y + m);

                // ── 场景深度（两个分支：正交 vs 透视；判据 = `unity_OrthoParams.w == 1`）
                float2 cap = 1.0 - 0.5 * _CameraDepthTexture_TexelSize.xy;
                float2 dUV = min(float2(r1.x, 1.0 - r1.y), cap) * _RTHandleScale.xy;
                float  dRaw = SAMPLE_TEXTURE2D_BIAS(_CameraDepthTexture, sampler_CameraDepthTexture,
                                                    dUV, _GlobalMipBias.x).r;

                float sceneD;
                if (unity_OrthoParams.w == 1.0)
                {
                    // 正交：从 (ndc, depth) 反投影回世界，再取视空间 z 的绝对值
                    // ⚠️ 原版 y 是**取负**的（`mul r3.xyzw, -r0.wwww, cb0[83].xyzw`），逐字照抄
                    float2 ndc = float2(r1.x, 1.0 - r1.y) * 2.0 - 1.0;
                    float4 wp = mul(unity_MatrixInvVP, float4(ndc.x, -ndc.y, dRaw, 1.0));
                    wp.xyz /= wp.w;
                    sceneD = abs(mul(unity_MatrixV, float4(wp.xyz, 1.0)).z);
                }
                else
                {
                    sceneD = 1.0 / (_ZBufferParams.z * dRaw + _ZBufferParams.w);
                }

                // ── 软粒子淡出 + 开关（`ne`+`movc`：Boolean ≠ 0 用 fade，否则用 m）
                float fade = saturate((sceneD - particleW) * IN.uv1.w) * m;
                float a    = (Boolean_52F3CBA5 != 0.0) ? fade : m;

                return half4(rgb, a * IN.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
