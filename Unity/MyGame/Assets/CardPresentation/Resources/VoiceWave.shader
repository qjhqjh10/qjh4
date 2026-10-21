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
//
// 用法：挂在气泡 `chat_wave_*` 这一件上（uGUI Image / 世界 quad 都行）；
//       `_Amplitude` 由 `UnitChatPanel.Advance` 按频谱写（0 = 直线）。
Shader "CardPresentation/VoiceWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Amplitude ("Amplitude", Float) = 0
        _Freq ("Frequency", Float) = 38
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
            float _Amplitude, _Freq, _Speed;

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
                uv.y += _Amplitude * sin(uv.x * _Freq + _Time.y * _Speed);
                return tex2D(_MainTex, uv) * i.color * _Color;
            }
            ENDCG
        }
    }
}
