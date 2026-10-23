// VoiceWave.shader —— 语音台词气泡上那条「音波」的**原版 `Shader Graphs/Equalizer` 逐句复刻**
// （2026-10-23 · 第二组 · 特效线 `UI·音波线（AudioWaveEqualizer）`）
//
// ════════════════════════════════════════════════════════════════════════════════════════════
// 一、这条 shader 的判据（全部是现读的硬证据，⛔ 没有一句是「看起来像」）
// ════════════════════════════════════════════════════════════════════════════════════════════
// 原版资产：
//   · 材质 `Equalizer`（场景实际用的那份 = `Material_-3262226040299197450.json`；
//     AssetRipper 导出里叫 `Equalizer_0.mat`）。Image 挂在
//     `bundle_scenes_scenes_battlearena1` 的 `MonoBehaviour_4509.json`（`m_Material` → 这个材质、
//     `m_Sprite` → `40k_voicelines_radio_wave equalizer`）。
//   · shader = `Shader Graphs/Equalizer`（`Shader_2656665607827278157.json`，
//     `bundle_shaders_assets_all`；属性表见 `资料/普查产出_0917/shader属性表_块4.md` §112）。
//   · **字节码没被剥**：`Shader.compressedBlob` 解出 3 个 DXBC 容器（vs,vs,ps），
//     用 `D3DDisassemble`（`d3dcompiler_47.dll`）反汇编出 **ps_4_0 全部 37 条指令** —— 下面每一条 math
//     都标了它的**指令号**，逐句可以对回反汇编。重跑办法见 `资料/普查产出_1023d/特效_UI音波线_执行报告.md`。
//   · **cbuffer 布局**（不是猜的）：同一 shader 的另一平台 blob 是 Unity 的 GLSL 反射表，明文写着
//     `UnityPerMaterial`(10 vec4) 里每个参数的**字节偏移**：
//        `_XOffsets`           @0x10 (reg1) 3 分量
//        `_MovementRange`      @0x30 (reg3) 1 分量   ← ★ 就是 C# 每帧 SetFloat 的那个（0..1）
//        `_WaveScaleRemap`     @0x38 (reg3) 2 分量   ← ★ 和它**共用一个 vec4**（reg3.y/.z）
//        `_Noise2ScaleAndSpeed`@0x40 (reg4) 4 分量
//        `_Noise1ScaleAndSpeed`@0x50 (reg5) 4 分量
//     ⇒ 反汇编里的 `cb1[3].x` = `_MovementRange`、`cb1[3].y/.z` = `_WaveScaleRemap.x/.y`。
//     （⚠️ 曾经把 `cb1[3]` 整个当成 `_WaveScaleRemap`，于是 `_MovementRange` 看起来「没被用」——
//        错因就是没查这张偏移表。见报告「坑」。）
//   · 场景实例：`MonoBehaviour_4115.json` → `spectrumModifier=15` · `speedChange=10`。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
// 二、原版 ps_4_0 的 37 条指令（逐条搬到这里，指令号 = D3DDisassemble 的输出）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   0: base = tex2D(_MainTex, uv)                                         ; sample_b（bias=0）
//   1-4: n2 = tex2D(_Noise, uv * _Noise2ScaleAndSpeed.xy
//                          + frac(t * _Noise2ScaleAndSpeed.zw))           ; 只用 .r
//   5: d  = _WaveScaleRemap.y - _WaveScaleRemap.x                          ; cb1[3].z - cb1[3].y
//   6-9: s1 = _MovementRange * (0.5*d*n2.r + _WaveScaleRemap.x - 20) + 20   ; **v 方向的缩放**
//  10-14: tap1 = tex2D(_MainTex, (uv.x + _XOffsets.x, s1*(uv.y-0.5)+0.5))
//  14-18: n1 = tex2D(_Noise, uv * _Noise1ScaleAndSpeed.xy
//                          + frac(t * _Noise1ScaleAndSpeed.zw))           ; 只用 **.b**（与上一条不对称！）
//  19-22: s2 = _MovementRange * (0.5*d*n1.b + _WaveScaleRemap.x - 20) + 20
//  23-25: tap2 = tex2D(_MainTex, (uv.x + _XOffsets.y, s2*(uv.y-0.5)+0.5))
//  26-30: v = saturate(_MovementRange^4 * ((tap1.r + tap2.r) - base.b) + base.b)
//         （27-28 是 `x*x` 再平方 = 四次方；29 先减 base.b；30 是 `mad_sat`）
//  31   : c = v * vColor
//  32-34: alpha = saturate(c.a)；`== 0` ⇒ discard
//  35   : o = c * vColor                      ← **顶点色乘了两次**（原版就是这么写的，别「修正」）
//
// ════════════════════════════════════════════════════════════════════════════════════════════
// 三、贴图/通道语义（这张图 `40k_voicelines_radio_wave equalizer`，708×96）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   · **B 通道** = 静止态那条**细线**（只有第 45/46 两行 B=255；其余 B=0）
//        ⇒ `_MovementRange = 0` 时公式退化成 `v = base.b` = 一条线（这就是「静音 = 直线」）。
//   · **R 通道** = **等化器竖条**（高度不一的竖条，基线在第 42-44 行）
//        ⇒ 有声时 `tap.r` 采到的是这些竖条，`v` 才起伏。
//   · **A 通道完全没用**（原版 shader 一个 alpha 都没采）—— 我们旧近似用 alpha 画线，是错的。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
// 四、与原版的差异（**只有这三条，且都不改数学**）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   1. `_Color` 是**我们加的**（原版没有这个属性）：原版的染色走**顶点色**（uGUI `Image.color`），
//      而且原版把它**乘了两次**（指令 31 / 35）。我们这条线画在 `ImageQuad`（MeshRenderer）上，
//      `ImageQuad.SetTint` 写的是 `material.color` 也就是 `_Color` ⇒ 保留它，并且**和顶点色一样乘两次**
//      （`tint = i.color * _Color`，输出 `v * tint * tint`）。`_Color = 白` 时与原版逐位等价。
//      想要 100% 原版口径也可改调 `ImageQuad.SetCornerColors(...)`（那样 `_Color` 留白即可）。
//   2. **采样器的 wrap 用 shader 自己算**：原版贴图导入是 `wrapU = Repeat / wrapV = Clamp`
//      （`40k_voicelines_radio_wave equalizer.png.meta:42-43` 原文 `wrapU: 0 · wrapV: 1`），
//      我们这张 PNG 的 meta 是 Clamp/Clamp，而那份 meta **不在本件的白名单里** ⇒ 在
//      `TapU()` / `TapV()` 里显式 `frac`（U 重复）/ `saturate`（V 夹住）。
//      差别只剩**接缝那一列**的双线性混色（原版硬件会在 U=0/1 之间跨接缝混，我们不会）。
//      根治 = 把 `Resources/Art/ui/40k_voicelines_radio_wave_equalizer.png.meta` 的 `wrapU` 改回 0。
//   3. `_Noise` 要按**线性**采样（原版那张 `Noise Combined` 的导入是 `sRGBTexture: 0`）。
//      工程里 `Art/ui_menu/Noise_Combined.png` 与原件**逐像素相同**，但导入是 **sRGB**，而且它被
//      `Up Rays` / 卡背 SDF 等**别的件共用**，不能改它的 meta。✅ 已经找到现成的等价件：
//      **`Art/card_sdf/cardback_noise.png`** —— 与原件**逐像素相同**（128×128，0 差异）、
//      导入就是 `sRGBTexture: 0` + `enableMipMap: 0` + bilinear（判据：那份 `.meta` 第 9/10/37 行）。
//      `VoiceWaveMaterial.Create()` 就是挂这一张。⛔ 别改挂 sRGB 那张 —— 噪声会偏亮/偏暗，缩放整条不对。
//
//   采样 LOD：原版 `sample_b` 的 bias 来自 `_GlobalMipBias`（URP 里通常 0），且原版贴图**没有 mip**
//   （`mipCount = 1`）；我们这张 PNG 的导入也是 `enableMipMap: 0` ⇒ `tex2D` 与它等价。
//
// 用法：挂在气泡 `chat_wave_*` 这一件上（`ImageQuad`）；`_MovementRange` 由
//       `Core/VoiceWaveDriver.cs` 每帧按原版 `AudioWaveEqualizer.Update` 写（0..1 原值）。
Shader "CardPresentation/VoiceWave"
{
    Properties
    {
        // —— 原版属性表（名字/显示名/默认值逐字照 `Shader Graphs/Equalizer`，⛔ 别改名）——
        [NoScaleOffset] _MainTex ("MainTex", 2D) = "white" {}
        _XOffsets ("XOffsets", Vector) = (0, 0.33, 0, 0)
        [NoScaleOffset] _Noise ("Noise", 2D) = "white" {}
        _MovementRange ("MovementRange", Range(0, 1)) = 1
        _WaveScaleRemap ("WaveScaleRemap", Vector) = (0, 10, 0, 0)
        _Noise1ScaleAndSpeed ("Noise1ScaleAndSpeed", Vector) = (0.05, 0.05, 0, 0.3)
        _Noise2ScaleAndSpeed ("Noise2ScaleAndSpeed", Vector) = (0.05, 0.05, 0, 0.2)
        // —— 我们加的（见文件头 §四·1）：`ImageQuad.SetTint` 走 `material.color` = `_Color` ——
        _Color ("Tint (ours)", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        // 原版 pass 状态（`shader属性表_块4.md` §112）：QUEUE=Transparent · RenderType=Transparent
        //   × SrcAlpha→OneMinusSrcAlpha | α:One→OneMinusSrcAlpha | mask=15 · zW=Off · zT=LEqual · cull=Off
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        LOD 100
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Noise;
            float4 _Color;
            float4 _XOffsets;
            float _MovementRange;
            float4 _WaveScaleRemap;          // 只用到 .x/.y（原版 cb1[3].y/.z）
            float4 _Noise1ScaleAndSpeed;
            float4 _Noise2ScaleAndSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;                 // 原版 INTERP0 = TEXCOORD0，未做任何变换（`_MainTex` 是 NoScaleOffset）
                o.color = v.color;
                return o;
            }

            // 原版采样器：U = Repeat、V = Clamp（见文件头 §四·2）。
            inline float TapU(float u) { return u - floor(u); }
            inline float TapV(float v) { return saturate(v); }
            // 原版是 `sample_b`（带 LOD bias）：bias = `_GlobalMipBias`（默认 0），且原版贴图**没有 mip**
            //   ⇒ 普通 `tex2D` 与它等价，这里不额外做 lod 计算。

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float  t  = _Time.y;            // 原版 cb0[19].x = `_TimeParameters.x` = t（秒）

                // ── 0：底图 ──────────────────────────────────────────────────────────────
                float4 baseTex = tex2D(_MainTex, uv);

                // ── 1-4：噪声 2（`_Noise2ScaleAndSpeed`，只取 .r）────────────────────────
                //   `frac` 是替原版硬件采样器的 Repeat/Repeat（见 §四·2，噪声贴图那份也是 Clamp）。
                float2 n2uv = frac(uv * _Noise2ScaleAndSpeed.xy + frac(t * _Noise2ScaleAndSpeed.zw));
                float  n2   = tex2D(_Noise, n2uv).r;

                // ── 5-9：tap1 的 v 缩放 ─────────────────────────────────────────────────
                //   d = `_WaveScaleRemap.y` - `_WaveScaleRemap.x`（原版 cb1[3].z - cb1[3].y）
                float d  = _WaveScaleRemap.y - _WaveScaleRemap.x;
                float s1 = _MovementRange * (0.5 * d * n2 + _WaveScaleRemap.x - 20.0) + 20.0;

                // ── 10-14：tap1 ────────────────────────────────────────────────────────
                float2 uv1  = float2(TapU(uv.x + _XOffsets.x), TapV(s1 * (uv.y - 0.5) + 0.5));
                float4 tap1 = tex2D(_MainTex, uv1);

                // ── 15-18：噪声 1（`_Noise1ScaleAndSpeed`，只取 **.b**）──────────────────
                float2 n1uv = frac(uv * _Noise1ScaleAndSpeed.xy + frac(t * _Noise1ScaleAndSpeed.zw));
                float  n1   = tex2D(_Noise, n1uv).b;

                // ── 19-22：tap2 的 v 缩放 ───────────────────────────────────────────────
                float s2 = _MovementRange * (0.5 * d * n1 + _WaveScaleRemap.x - 20.0) + 20.0;

                // ── 23-25：tap2 ────────────────────────────────────────────────────────
                float2 uv2  = float2(TapU(uv.x + _XOffsets.y), TapV(s2 * (uv.y - 0.5) + 0.5));
                float4 tap2 = tex2D(_MainTex, uv2);

                // ── 26-30：混 ──────────────────────────────────────────────────────────
                float m2 = _MovementRange * _MovementRange;
                float m4 = m2 * m2;                                       // 指令 27-28
                float v  = saturate(m4 * ((tap1.r + tap2.r) - baseTex.b) + baseTex.b);  // 29-30（mad_sat）

                // ── 31-35：顶点色乘**两次**（原版如此）+ 我们那层 `_Color` ─────────────────
                //   `_Color` 是**替原版顶点色**的那一格（见 §四·1）⇒ 和顶点色一样**一起乘两次**，
                //   这样 `_Color = Image.color` 时与原版逐位等价。
                float4 tint = i.color * _Color;
                float4 c = v.xxxx * tint;                 // 31
                if (saturate(c.a) <= 0.0) discard;        // 32-34（`mov_sat` + `eq 0` + `discard_nz`）
                return c * tint;                          // 35（+§四·1）
            }
            ENDCG
        }
    }

    // 原版 Fallback = `Hidden/Shader Graph/FallbackError`（URP 专有，内置管线里没有这个 shader）。
    // 这里退回 `Sprites/Default`：只在**编译不出可用变体**时才会用到（真机见到它说明这条 shader 没编过）。
    Fallback "Sprites/Default"
}
