// WFUIBorderHighlight.shader —— 原版 `Everguild/UI/UI Border Highlight Appear Pulsating Scale` 的**工程内替身**
//
// ============================ 判据（本机实读） ============================
// 原版那颗 shader 的真名：**`Everguild/UI/UI Border Highlight Appear Pulsating Scale`**
//   （`assets_full/bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json` 的 `m_ParsedForm.m_Name`；
//    由材质 `Border Highlight Appear Yellow`（`bundle_menusharedresources_assets_all/Material/Material_-678375647445659077.json`）
//    的 `m_Shader = {m_FileID: 1, m_PathID: -4463273156108355430}` 两跳解出）。
// 原版属性表（逐条照抄，**顺序与命名不动** —— C# 侧（`UIBorderGlow` / 卡面那几处）已经在按这些名字读写）：
//   `_ColorMain` · `_ColorSecondary` · `_Color_offsets` · `_DEBUG` · `_Noise_R` · `_Noise_Strength_X_Speed_Y_Scale_Z` ·
//   `_FLIPDIRECTION` · `_Scale_X_Speed_Y` · `_StartOffset` · `_Reveal` · `_Smoothness` · `_Rotation` ·
//   `_AUTOROTATION` · `_AutoRotationSpeed` · `_QueueOffset` · `_QueueControl` · `unity_*`
// 材质上的默认值（同一份 JSON 的 `m_SavedProperties.m_Floats` 实读，本 shader 的默认值照抄）：
//   `_Reveal 0.723` · `_Smoothness 0.032` · `_NoiseStrength`（= `_Noise_Strength_X_Speed_Y_Scale_Z` 那一格的旧名，0.04）·
//   `_Range 89` · `_RangeStartOffset 196` · `_AutoRotationSpeed -180` · `_Add 0.09` · `_ColorMask 15` · `_Rotation 0` · `_StartOffset 0`
//
// ⚠️ **本文件是「属性表兼容的替身」，不是原版像素级复刻**：
//   原版 HLSL 源码被剥（`Shader.m_Script = null`），编译字节码在 `compressedBlob`（DXBC）里；
//   本轮**没有**逐指令重建那条「旋转噪声 + `_Reveal` 揭开」的曲线（上下文/时间不够，见报告 ③），
//   只做到：**同名属性、同类观感（沿边框旋转的噪声亮带，按 `_Reveal` 从 `_StartOffset` 方向揭开）、同一套分层规则**。
//   ⇒ 🔶 **自己设计（非原版）**：噪声取模、揭开方向、柔化曲线都是我们挑的 · 调度台 2026-10-27 裁定（先让画面出得来、再迭代像不像）。
//
// 用法：材质把它挂上后，把 `_Reveal` 从 −1 推到 1（`UIBorderGlow` 已经这么驱动），边框亮带就会沿角度揭开 / 收起。
Shader "WarpforgeVFX/UI/UI Border Highlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorMain ("Color Main", Color) = (1,1,1,1)
        _ColorSecondary ("Color Secondary", Color) = (1,1,1,1)
        _Color_offsets ("Color Offsets", Vector) = (0,0,0,0)
        _Noise_R ("Noise R", 2D) = "gray" {}
        _Noise_Strength_X_Speed_Y_Scale_Z ("Noise Strength / Speed / Scale", Vector) = (0.04,0,8.17,0)
        _Scale_X_Speed_Y ("Scale XY", Vector) = (1,1,0,0)
        _Reveal ("Reveal", Range(-1,1)) = 0.723
        _Smoothness ("Smoothness", Range(0,1)) = 0.032
        _StartOffset ("Start Offset", Float) = 0
        _Range ("Range", Float) = 89
        _RangeStartOffset ("Range Start Offset", Float) = 196
        _Rotation ("Rotation", Float) = 0
        _AUTOROTATION ("Auto Rotation", Float) = 0
        _AutoRotationSpeed ("Auto Rotation Speed", Float) = -180
        _FLIPDIRECTION ("Flip Direction", Float) = 0
        _Add ("Add", Float) = 0.09
        _ColorMask ("Color Mask", Float) = 15
        _DEBUG ("Debug", Float) = 0
        _QueueOffset ("Queue Offset", Float) = 0
        _QueueControl ("Queue Control", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane"
               "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One          // 边框高光是**加色**的（原版材质 `_SrcBlend/_DstBlend` 那一族是叠加）

        Pass
        {
            Name "UI_BORDER_HIGHLIGHT"
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`、内置管线走默认 pass，两边都渲染得出来。
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

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

            sampler2D _Noise_R;
            fixed4 _ColorMain, _ColorSecondary, _Color_offsets, _RendererColor;
            float4 _Noise_Strength_X_Speed_Y_Scale_Z;
            float4 _Scale_X_Speed_Y;
            float _Reveal, _Smoothness, _StartOffset, _Range, _RangeStartOffset;
            float _Rotation, _AUTOROTATION, _AutoRotationSpeed, _FLIPDIRECTION, _Add, _ColorMask, _DEBUG;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _RendererColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ① 「离中心的角的量」—— 用一张噪声图在极坐标方向上滚动（原版也是噪声驱动的亮带）
                float2 c  = i.uv - 0.5;
                float  ang = atan2(c.y, c.x) / 6.2831853 + 0.5;          // 0..1 的角
                if (_FLIPDIRECTION > 0.5) ang = 1.0 - ang;
                float  rot = _Rotation + _Time.y * (_AUTOROTATION > 0.5 ? _AutoRotationSpeed : 0.0);
                // 🔴 2026-10-27（第十一批 · DXBC）：`_StartOffset` **不进算式** —— 它是声明属性，但**不在**原版
                //   编译字节码那 26 个常量名里（`dump_shader_blob.py "UI Border Highlight Appear Pulsating Scale"` 实读）
                //   ⇒ 原版像素代码**不读它**。（`_Range` / `_RangeStartOffset` / `_Add` / `_ColorMask` 更进一步：
                //   连 shaderlab 属性都没声明，只是材质上的**死值** —— 详见报告第十一批。）
                float2 nuv = float2(ang * max(_Scale_X_Speed_Y.x, 0.001) + rot * 0.01,
                                    _Time.y * _Noise_Strength_X_Speed_Y_Scale_Z.y * 0.01
                                    + length(c) * max(_Noise_Strength_X_Speed_Y_Scale_Z.z, 0.001));
                float  n   = tex2D(_Noise_R, nuv).r;

                // ② `_Reveal`（−1..1）↔ 已揭开的角区间；`_Smoothness` 是边缘柔化
                float reveal = saturate((_Reveal + 1.0) * 0.5);
                float soft   = max(_Smoothness, 0.001);
                float opened = saturate((reveal * (1.0 + soft) - ang) / soft);

                // ③ 只留**边框**：离卡边越远越暗（`length(c)` 的径向衰减）
                float edge = saturate(1.0 - (0.5 - length(c)) * 2.0);
                float a    = opened * edge * saturate(n * (1.0 + _Noise_Strength_X_Speed_Y_Scale_Z.x * 2.0));

                fixed3 col = lerp(_ColorMain.rgb, _ColorSecondary.rgb, saturate(n)) + _Color_offsets.rgb;
                col *= (1.0 + _Add);
                return fixed4(col * i.color.rgb, a * i.color.a * _ColorMain.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
