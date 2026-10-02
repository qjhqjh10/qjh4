// WFTrailShader1.shader — 自建，替代原版 `Everguild/FX/TrailShader_1`
//
// 🔴 **来由**（2026-10-02，兜底路审计第 4 族）：它原来和另外几个一起被映射到通用的
//    `WarpforgeVFX/Particles/Extra Color`（只乘）⇒ 兜底路上 `BulletImpact_DeathSpinner Test`
//    **×9.61**（全表第二惨）、`BulletImpact_deathspinner_arc_alt` ×2.57。
//    逐条数据 → `资料/比对基线/兜底路审计_1002.tsv`。
//
// 判据（**全部实读**）—— 反汇编 `d:/4/_tmp_view/dxbc/trailshader1.txt`（段 0 = 基础变体，53 条指令）
// ---------------------------------------------------------------------------
// ① **渲染状态**（`工具/dump_shader.py "TrailShader_1"`，pass0）：
//      rtBlend0: srcBlend=5(SrcAlpha) destBlend=10(OneMinusSrcAlpha) separateBlend=False
//      zWrite=0 · zTest=4(LEqual) · cull=0(Off) · alphaToMask=0 · colMask=15
//      SubShader tags: QUEUE=Transparent / RenderType=Transparent / RenderPipeline=UniversalPipeline
//      ⚠️ 原版共 4 个 pass（1=运动矢量 · 2=法线 · 3=无颜色输出），本文件只做 pass0。
//      ⚠️ 原版材质上**没有** `_SrcBlend`（rtBlend 全是 `<noninit>`）⇒ **混合硬编码**，⛔ 不许间接寻址。
// ② **属性（11 个，名字/类型/默认值逐字照抄 `dump_shader.py`）**：
//      `_Color01`(HDR Color, def 0,1,0.04726887,0) · `_Color02`(HDR, def 0,1,0.80137157,0)
//      `_MainTexture`(NoScaleOffset) · `_MainTextureSpeed`(Vector, def −0.1) · `_DissolveScale`(Float, def 1)
//      `_DissolveSpeed`(Vector, def −0.5) · `_Noise_Combined`(NoScaleOffset) · `_TRAILALPHACUTOFF`(Float, def 0)
//      `_TrailLenght`(Float, def 0) · `_QueueOffset` · `_QueueControl`(def −1)
//      ⚠️ 两张贴图都是 **NoScaleOffset** ⇒ 原版不乘 `_ST`，我们也不乘。
// ③ **顶点**：`o1 = v3(TEXCOORD0)`（拖尾 UV 原样传）· `o2 = v4(COLOR)`（顶点色原样传）·
//    clip = `unity_MatrixVP · (unity_ObjectToWorld · v0)`。**VS 不改 UV、不改色**。
// ④ **片元**（逐条，`v1.xy`=拖尾 UV、`v2.xyzw`=顶点色）：
//      t       = cb0[19].x                                  ; = `_TimeParameters.x`（见下「槽位判据」）
//      n       = SAMPLE(t1, v1.xy · cb1[3].z + t · cb1[4].xy)   ; t1 = `_Noise_Combined`，**只用 .z**
//      m       = SAMPLE(t0, v1.xy + t · cb1[3].xy)              ; t0 = `_MainTexture`
//      A       = (1 − v1.x) + n.z − cb1[6].x                ; 沿长度线性淡出 + 噪声 − 常数
//      alp     = A · m.r · v2.w                             ; ⚠️ 主贴图取的是 **.r**，不是 .a
//      o0.rgb  = m.rgb · lerp( S(_Color01), S(_Color02), v1.x ) · v2.rgb   ; S = LinearToSRGB 编码
//      o0.a    = `_AlphaToMaskAvailable == 0` ? alp : saturate( (alp−0.5−0.5·fw)/fw + 1 )，fw=|ddx alp|+|ddy alp|
//      discard : 普通路径 **alp − 0.5 < 0**（0.5 写死在指令里）· a2m 路径用上面那个值减 1e-4
//
// 🔴 **cbuffer 槽位怎么定的**（与 `WFParticleDissolveAPB.shader` 同一套，判据写在那边文件头）：
//      `cb0` = Unity `$Globals`，按 URP 全局声明顺序累加 ⇒
//      **cb0[4].x = `_GlobalMipBias`**（两个 sample 的 bias 项）· **cb0[4].z = `_AlphaToMaskAvailable`**
//      （URP `Input.hlsl:112/115` 相邻两条，与实读逐位吻合）· **cb0[19].x = `_TimeParameters.x`**
//      （我的累加表：15=_Time · 16=_SinTime · 17=_CosTime · 18=unity_DeltaTime · **19=_TimeParameters**；
//       与 `资料/战场13场_逐场对账_0920.md:1524` 独立记的「时间项统一是 cb0[19].x」一致）·
//      `cb0[78..81]` = `unity_MatrixVP`（VS 拿它算 clip，独立佐证）。
//      `cb1` = **UnityPerMaterial**；本 shader 用到 `[0].[1]`（两个 Color）· `[3].xy/[3].z` · `[4].xy` · `[6].x`。
//      ⚠️ **`cb1[3]/[4]/[6]` 的「槽 → 属性名」是推的**（无 RDEF、材质 CB 偏移读不出来）：判据是
//        **用法 + 名字**两路都指向同一个答案，两条都记在下面每个属性后面。
//        「推」不等于「猜」：A/B 会再量一遍（`WFBIND_FORCE_BUILTIN=TrailShader_1`）。
// ⑤ **两处如实标注的「判据不足」**（铁律 3）：
//      · `cb1[6].x` 取 `_TrailLenght`（== 推 ==）：另一候选 `_TRAILALPHACUTOFF` **恒 0**（8 个材质全是），
//        而 `_TRAILALPHACUTOFF` 同时是**关键字**（关键字全集里有它）⇒ 更像「关键字那支才用它当阈值」，
//        而本 build **没编出那个变体**。选 `_TrailLenght` 的理由是语义：`A` 里减一个负数 = **把拖尾拉长**
//        （那几个 Necron 材质正是 `_TrailLenght=-0.1`）。
//      · `_TRAILALPHACUTOFF` 关键字那一支**没编进 blob**（每 pass 只有一个变体）⇒ **照不到判据**，
//        本文件实现的是**已编译的那一支**（阈值 0.5 写死）。
//
// ⚠️ **它在白名单里** ⇒ 运行时走原件；本文件只在**兜底路**生效。
Shader "WarpforgeVFX/FX/TrailShader1"
{
    Properties
    {
        _Color01("Color01", Color) = (0, 1, 0.04726887, 0)
        _Color02("Color02", Color) = (0, 1, 0.80137157, 0)
        _MainTexture("MainTexture", 2D) = "white" {}
        _MainTextureSpeed("MainTextureSpeed", Vector) = (-0.1, 0, 0, 0)
        _DissolveScale("DissolveScale", Float) = 1
        _DissolveSpeed("DissolveSpeed", Vector) = (-0.5, 0, 0, 0)
        _Noise_Combined("Noise Combined", 2D) = "white" {}
        _TRAILALPHACUTOFF("TrailAlphaCutoff", Float) = 0
        _TrailLenght("TrailLenght", Float) = 0
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
        ZTest LEqual

        Pass
        {
            Name "TrailShader1"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color01;
                float4 _Color02;
                float4 _MainTextureSpeed;
                float  _DissolveScale;
                float4 _DissolveSpeed;
                float  _TRAILALPHACUTOFF;
                float  _TrailLenght;
                float  _QueueOffset;
                float  _QueueControl;
            CBUFFER_END

            TEXTURE2D(_MainTexture);     SAMPLER(sampler_MainTexture);
            TEXTURE2D(_Noise_Combined);  SAMPLER(sampler_Noise_Combined);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 uv0        : TEXCOORD0;   // 拖尾 UV（原版 vs 的 v3）
                float4 color      : COLOR;       // 拖尾顶点色（原版 vs 的 v4）
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv         : TEXCOORD0;   // ps 的 v1
                float4 color      : TEXCOORD1;   // ps 的 v2
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv0;          // 原版 `mov o1.xyzw, v3.xyzw` —— **不做 TRANSFORM_TEX**
                OUT.color = IN.color;     // 原版 `mov o2.xyzw, v4.xyzw`
                return OUT;
            }

            /// linear → sRGB **编码**（原版那 7 条：`log/mul 0.416667/exp/mad 1.055,-0.055/ge 0.003131/mul 12.92321/movc`）
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
                float  t  = _TimeParameters.x;                 // cb0[19].x
                float2 uv = IN.uv.xy;

                // 噪声（t1）：UV 先乘 `_DissolveScale` 再按 `_DissolveSpeed` 滚时间；**只用 .z**
                half4 n = SAMPLE_TEXTURE2D_BIAS(_Noise_Combined, sampler_Noise_Combined,
                            uv * _DissolveScale + t * _DissolveSpeed.xy, _GlobalMipBias.x);
                // 主贴图（t0）：UV 按 `_MainTextureSpeed` 滚时间
                half4 m = SAMPLE_TEXTURE2D_BIAS(_MainTexture, sampler_MainTexture,
                            uv + t * _MainTextureSpeed.xy, _GlobalMipBias.x);

                half  A   = (1.0h - uv.x) + n.z - _TrailLenght;
                half  alp = A * m.r * IN.color.w;              // ⚠️ m.r（不是 m.a）

                half3 rgb = m.rgb
                          * lerp(LinearToSrgb(_Color01.rgb), LinearToSrgb(_Color02.rgb), uv.x)
                          * IN.color.rgb;

                // ---- 输出 alpha + 裁剪（原版是 alpha-to-mask 可用性分叉，`cb0[4].z`）
                half fw   = abs(ddx(alp)) + abs(ddy(alp));
                half a2m  = saturate(((alp - 0.5h) - 0.5h * fw) / max(fw, 0.0001h) + 1.0h);
                bool useM = _AlphaToMaskAvailable != 0.0;
                half outA = useM ? a2m : alp;
                clip((useM ? (a2m - 0.0001h) : (alp - 0.5h)));

                return half4(rgb, outA);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
