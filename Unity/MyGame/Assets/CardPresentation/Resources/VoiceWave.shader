// VoiceWave.shader —— 语音台词气泡上那条「音波」的**位移**（第六十会话 · 2026-10-21）
//
// 判据（原版）：
//   · 底图 = `40k_voicelines_radio_wave equalizer`（708×96，透明区只在 41–44 行 = 一条 3 px 直线；
//     与本工程 `Resources/Art/ui/40k_voicelines_radio_wave_equalizer.png` 逐字节相同）。
//   · 动态来自 `AudioWaveEqualizer`（`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4115.json`：
//     `spectrumModifier=15` · `speedChange=10`；`VoiceLineBox__Initialize.c:6` 拿气泡 `AudioSource`；
//     `AudioWaveEqualizer__Update.c:29-67` 每帧 `GetSpectrumData` 前 16 bin 求和 ×15 → clamp01 → 按 10×dt 平滑
//     → `Material.SetFloat`）。
//   · ⚠️ **原版 shader 的位移数学拿不到**（`Shader_2656665607827278157.json` 无名字/无可读字节码）⇒
//     本 shader 是**等价近似**：在 UV 空间按 sin 位移采样 y，幅度由 C# 端的频谱驱动。
//     形状可能与原版不是逐像素一致（如实标注，别把它当原版判据）。
//   · 🔴 **2026-10-21 按判据订正属性名**：原版 `AudioWaveEqualizer.Update` 写的是
//     **`_MovementRange`**（属性名字符串 RVA 0x4235590 解出；材质
//     `battlesharedresources/Material_-3262226040299197450.json` 的默认值 **0.00591**）。
//     我们原来自己起名 `_Amplitude` 并乘 0.22 —— **量纲是我方自选**。
//     现在：C# 侧写**原版那个 0..1 的 `targetMovement` 原值**到 `_MovementRange`，
//     位移量纲收进本 shader 的 `_WaveScale`（= 我们挑的 0.22，原版数学拿不到，如实标着）。
//
// 用法：挂在气泡 `chat_wave_*` 这一件上（uGUI Image / 世界 quad 都行）；
//       `_MovementRange` 由 `UnitChatPanel.Advance` 按频谱写（0 = 直线）。
Shader "CardPresentation/VoiceWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _MovementRange ("Movement Range", Float) = 0.00591
        // 🔴 2026-10-21 按**原版实拍**定：`战斗截图参考.png` 里那条波是**细线小幅起伏**
        //   （整条带高 62.6 px，起伏约 ±3 px、横向上约 20 个周期）。原来 0.22 会让 3 px 的线
        //   扫成一根**实心绿条**（真机截图 `20_wave.png`/`22_late.png` 就是这么被发现的）。
        _WaveScale ("Wave Scale (ours)", Float) = 0.05
        _Freq ("Frequency", Float) = 20
        _Speed ("Speed", Float) = 14
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _MovementRange, _WaveScale, _Freq, _Speed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                // 只动 y：一条横线在 x 方向被推成波（原版也是这个观感）
                // `_MovementRange` = 原版 `targetMovement` 原值（0..1）；`_WaveScale` 是我们挑的 UV 量纲。
                uv.y += _MovementRange * _WaveScale * sin(uv.x * _Freq + _Time.y * _Speed);
                return tex2D(_MainTex, uv) * i.color * _Color;
            }
            ENDCG
        }
    }
}
