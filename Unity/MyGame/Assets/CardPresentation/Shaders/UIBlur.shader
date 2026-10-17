// UIBlur.shader — 「离屏烘焙 + UI Blur」用的**两趟可分离模糊**（界面线 T16）
//
// ============================ 判据（原版） ============================
// · 原版材质 `UI Blur` = `bundle_scenes_scenes_mainmenuwarpforge/Material/Material_3.json`；
//   shader 真属性表（`Shader/Shader_339.json` 的 `m_ParsedForm.m_PropInfo.m_Props` 逐条实读）
//   **只有三个**：`_MainTex` / **`_Sigma = 2.25`** / **`_steps = 10`**。
//   ⚠️ 材质里还烘着 `_BlurRadius` / `_GridSize` / `_Iterations` / `_Offset` / `_Spread` /
//      `_xBlur` / `_yBlur` / `_QueueControl` / `_QueueOffset` —— **全是别的 shader 代的残留死值**，
//      本 shader **一个都不声明**（工程已知坑：自建 shader 一旦声明同名属性，就会把死值当活值用）。
// · 两趟的用法（`UIBakeScreen__Toggle.c` 逐句）：
//      `Graphics.Blit(RT, temp, blurMat)`      ← **pass 0**
//      `Graphics.Blit(temp, RT, blurMat, 1)`   ← **pass 1**
//   ⇒ 本文件 **pass 0 = 横向 · pass 1 = 纵向**（可分离高斯的两趟）。
//
// ============================ 如实标注（铁律 3） ============================
// 🔴 **这是等价实现，没有逐条核原版 DXBC。** `_Sigma` / `_steps` 的语义（「高斯 σ」与「采样步数」）
//    是从属性名 + 两趟 Blit 推出来的；原版 shader 字节码在 `Shader_339.compressedBlob`（3556 B），
//    要 1:1 复算得解字节码（工具 `工具/dump_shader_blob.py`）—— **本件没做**。
//    这里的核 = `w(k) = exp(-k² / (2σ²))`，k = 1..`_steps`，采样 ±k 个 texel、按权重归一化
//    （σ = 2.25、steps = 10 ⇒ 左右各 10 抽、共 21 抽）。
// 🔴 **不写 `Blend [_SrcBlend][_DstBlend]` 这种间接寻址** —— 工程踩过（原版材质残留死值把透明变不透明）。
//    这里硬编码 `Blend Off`：两趟都是「整张覆盖」，显示用的 quad 走 `ImageQuad` 自己的材质，不靠这份。
// 🔴 **两个模糊 pass 都把 alpha 写成 1** —— 第二趟写回 RT 之后，显示端（`Sprites/Default`）拿到的
//    就是一张完全不透明的快照（相机清屏色的 alpha 是多少都不会漏底）。
Shader "CardPresentation/UIBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Sigma ("Sigma", Float) = 2.25
        _steps ("Steps", Float) = 10
    }

    SubShader
    {
        // 只被 `Graphics.Blit` 用（不挂给任何 MeshRenderer）⇒ 不用透明队列；两趟都整张覆盖。
        Tags { "Queue"="Overlay" "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend Off

        // ---------------------------------------------------------------- pass 0：横向
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Sigma;
            float _steps;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float sigma = max(_Sigma, 0.0001);
                float twoSigma2 = 2.0 * sigma * sigma;
                float steps = max(_steps, 1.0);

                float4 sum = tex2D(_MainTex, i.uv);
                float wsum = 1.0;
                [loop]
                for (float k = 1.0; k <= steps; k += 1.0)
                {
                    float w = exp(-(k * k) / twoSigma2);
                    float2 off = float2(texel.x * k, 0.0);
                    sum += w * (tex2D(_MainTex, i.uv + off) + tex2D(_MainTex, i.uv - off));
                    wsum += 2.0 * w;
                }
                return fixed4(sum.rgb / wsum, 1.0);
            }
            ENDCG
        }

        // ---------------------------------------------------------------- pass 1：纵向
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Sigma;
            float _steps;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float sigma = max(_Sigma, 0.0001);
                float twoSigma2 = 2.0 * sigma * sigma;
                float steps = max(_steps, 1.0);

                float4 sum = tex2D(_MainTex, i.uv);
                float wsum = 1.0;
                [loop]
                for (float k = 1.0; k <= steps; k += 1.0)
                {
                    float w = exp(-(k * k) / twoSigma2);
                    float2 off = float2(0.0, texel.y * k);
                    sum += w * (tex2D(_MainTex, i.uv + off) + tex2D(_MainTex, i.uv - off));
                    wsum += 2.0 * w;
                }
                return fixed4(sum.rgb / wsum, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
