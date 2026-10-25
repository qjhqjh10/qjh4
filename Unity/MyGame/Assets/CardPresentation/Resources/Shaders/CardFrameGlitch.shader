// CardFrameGlitch.shader — 自建，复刻原版 `Everguild/FX/Sprite Scan Lines`（ShaderGraph · UniversalSpriteUnlit）
//
// 谁用它：`CardView.SetGlitch(true)` —— 原版 `Card2DController.ToggleGlitch(bool)` 换的就是这一支
// （`frame.material = option ? glitchMaterial : originalFrameMaterial`，
//  `Card2DController.cs:132`；调用面只有一处：`CardScript.cs:8046` 的
//  `if (HasCurrentTrait(ephemeral) && isPlayer) card2DController.ToggleGlitch(true);`）。
//
// 为什么是【自建】而不是直接拿原版 shader：
//   · 原版 shader **在本地**（`bundle_shaders_assets_all/Shader/Shader_-5025784757600742591.json`，
//     `m_ParsedForm.m_Name = "Everguild/FX/Sprite Scan Lines"`），运行时也能从 `wf_shaders.bundle` 取到
//     （`资料/普查产出_0918/shader原件可用性_表.md:97`「原件在主包 = 有」）。
//   · 🔴 **但那支 shader 在 VS 里把顶点色乘了 `unity_SpriteColor`**（见下 ② 的实读），
//     而 `unity_SpriteColor` / `unity_SpriteProps` 是 **`SpriteRenderer` 每绘制一次的常量** ——
//     我们的卡面全是 **MeshRenderer**（`CardView.AddLayer`），那两条通道**没人填**
//     ⇒ 直接用原件会渲染成黑/透明。**本轮不能跑 Unity 验证**（铁律 12 自检暂停），
//     所以选「算式照抄、通道改我们自己的」这条 —— 算式是**逐条实读**的，不是猜的。
//
// 判据（`工具/disasm_dxbc.py "Sprite Scan Lines"`，落在 `_tmp_view/e7/scanlines_{ps,vs}.txt`）
// ---------------------------------------------------------------------------------------------
// ① **渲染状态**（`Shader.json` 的 `m_SubShaders[0].m_Passes[*].m_State` 实读）：
//      blend = SrcAlpha(5) / OneMinusSrcAlpha(10，ShaderLab 序列化枚举) · srcA = One(1) · dstA = 10
//      zWrite = 0 · zTest = 4(LEqual) · culling = 0(Off) · colMask = 15 · Queue = Transparent(3000)
//    ⇒ **直通 alpha**（不是预乘）。⚠️ 注意：我们卡面基材 `CardPresentation/CardSprite` 是**预乘**
//      （`Blend One OneMinusSrcAlpha` + `rgb *= a`）—— 两支在数学上等价（两边都是 `src*a + dst*(1-a)`），
//      所以卡框换材质时**边缘不会有可见跳变**。
// ② **顶点**（vs 段实读）：
//      `o1 = v3`（TEXCOORD0 → PS 的 v1 = **uv**）
//      `o2 = cb0[130] * cb1[43] * v4`（COLOR → PS 的 v2 = **顶点色 × `unity_SpriteProps` × `unity_SpriteColor`**）
//    🔴 **这就是不能用原件的理由**：`unity_SpriteColor` 对 MeshRenderer 是空的。本文件里 v2 只留**顶点色**
//      （我们所有 quad 都没有 color 通道 ⇒ 恒 (1,1,1,1)；`CardSprite` 同一条约定，已在线上跑）。
// ③ **片元**（ps 段实读，`t = cb0[19].x` = `_TimeParameters.x` = `_Time.x`）：
//      uvn   = uv · `_NoiseScaleAndSpeed`.xy + frac(t · `_NoiseScaleAndSpeed`.zw)      // 采 `_Noise`
//      rnd   = frac(sin(dot(t, (12.9898, 78.233))) · 43758.546875)
//      gate  = rnd >= (1 − `_GlitchPercentaje`) ? 1 : 0                                // 抖动闸
//      uv2   = uv + gate · `_NoiseIntensity` · noise.xy                                // 采样被挪的 uv
//      c     = tex2D(`_MainTex`, uv2) · vcol
//      if (c.a == 0) discard
//      k     = 1 − `_ScanLinesIntensity`
//      rgb   = c.rgb · saturate(noise.rgb + k) + `_NoiseColor`.rgb · (noise.rgb + k)     // ⚠️ 第二项**不 saturate**
//      o     = (rgb · vcol, c.a · vcol.a)                                              // 原版**再乘一次**顶点色
//    ⚠️ 原版 `sin` 那条：`dp2` → `sincos r1.x, null, r1.x` 只取了 **sin**、再乘 43758.546875。
//    ⚠️ `_AntialiasScale` 只在 **`SUPERSAMPLING_ON`** 变体里被读（4-tap + derivatives）；
//       该材质 `m_ValidKeywords = []` ⇒ **实际跑的是单采样那支** ⇒ 本文件默认单采样，把 4-tap 留在
//       `SUPERSAMPLING_ON` 关键字后面（照抄，不是我们自己加的优化）。
//    ⚠️ `_MainColorIntensityFix` 在**可达的那支**里一次都没读（死属性）—— 声明它是为了「原版材质能整套灌进来」。
//
// 我们这边多出来的两个属性（**如实标注：原版没有**）：
//   · `_Color`   —— 我们的「整卡着色」通道（`CardView.ApplyTint` 给 `_layers` 每一层写 `_Color`）。
//                  原版 `Card2DController` 没有这一层，所以原版也没有这个属性。**默认 (1,1,1,1) ⇒ 与原版一致。**
//   · `_GreyScale` —— 同上，我们的去饱和通道（`CardView.SetGrey` 走 MPB 写每层）。**默认 0 ⇒ 与原版一致。**
//   ⚠️ 原版最后那次「再乘一遍顶点色」在本文件里**并进 `i.color`**（只乘一次）：顶点色恒白时两者逐像素相同，
//      而乘两次会让 `_Color` 的 alpha 变成二次方（我们的淡出会跟着变慢）。
//      另外片元里 **rgb 与 alpha 分开乘**（`rgb *= _Color.rgb` · `a *= _Color.a`）—— 直通 alpha 混合下
//      把 alpha 也乘进 rgb 会二次变暗（那是预乘支路才该做的事）。
//   · `_UvRect`  —— 我们的卡框 mesh 的 uv 是**图集子矩形**（`CardView.FrameUv`），不是原版 UI Image 的 0..1。
//                  噪声 UV 必须按 0..1 归一化才和原版同一张噪声图同一块区域。默认 (0,0,1,1) ⇒ 等价于 0..1。
Shader "CardPresentation/CardFrameGlitch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Noise ("Noise", 2D) = "white" {}
        _NoiseScaleAndSpeed ("Noise Scale (XY) Speed (ZW)", Vector) = (1, 1, 1, 1)
        _GlitchPercentaje ("Glitch Percentage", Range(0, 1)) = 0.5
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.1
        _ScanLinesIntensity ("Scan Lines Intensity", Range(0, 1)) = 1
        _NoiseColor ("Noise Color", Color) = (1, 1, 1, 1)
        _AntialiasScale ("Antialias Scale", Range(0.75, 3)) = 1.25
        _MainColorIntensityFix ("Main Color Intensity Fix", Float) = 1

        // ---- 我们这边多出来的（见文件头「我们这边多出来的两个属性」）----
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _GreyScale ("Greyscale", Range(0, 1)) = 0
        _UvRect ("UV Rect (xMin, yMin, w, h)", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            // `ddx`/`ddy`（4-tap 那支）要 SM3.0；其余与 `CardPresentation/CardSprite` 同档
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment SUPERSAMPLING_ON
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _Noise;
            float4 _NoiseScaleAndSpeed;
            float  _GlitchPercentaje;
            float  _NoiseIntensity;
            float  _ScanLinesIntensity;
            float4 _NoiseColor;
            float  _AntialiasScale;
            float  _MainColorIntensityFix;
            float4 _Color;
            float  _GreyScale;
            float4 _UvRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // 原版：o2 = unity_SpriteProps * unity_SpriteColor * v.color。
                // 我们的 MeshRenderer 拿不到前两项 ⇒ 只留顶点色 × `_Color`（见文件头 ② / 「多出来的属性」）。
                o.color = v.color * _Color;
                return o;
            }

            // 原版 `SUPERSAMPLING_ON` 那支：4-tap（±0.375 / ±0.125 个屏幕导数）+ 平均。
            // ⚠️ 本材质 `m_ValidKeywords = []` ⇒ **正常态走单采样**；这一段只为「有人打开关键字时照抄不跑偏」。
            float4 SampleMainAntiAliased(float2 uv, float aaScale)
            {
                // ⚠️ 局部量**不能叫 `ddx`/`ddy`** —— 那会把 HLSL 内建函数遮蔽掉（独立编译实测报 X3005）
                float2 du = ddx(uv * aaScale);
                float2 dv = ddy(uv * aaScale);
                float4 c  = tex2D(_MainTex, uv + du * 0.375 + dv * 0.375);
                c += tex2D(_MainTex, uv - du * 0.375 + dv * 0.125);
                c += tex2D(_MainTex, uv + du * 0.125 - dv * 0.375);
                c += tex2D(_MainTex, uv - du * 0.125 - dv * 0.125);
                return c * 0.25;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.x;                       // ≡ 原版 cb0[19].x = `_TimeParameters.x`

                // 归一化到 0..1（我们的 mesh uv 是图集子矩形；原版 UI Image 本来就是 0..1）
                float2 uvn = (i.uv - _UvRect.xy) / max(_UvRect.zw, float2(1e-6, 1e-6));

                // ① 噪声 UV：uv·scale + frac(t·speed)
                uvn = uvn * _NoiseScaleAndSpeed.xy + frac(t * _NoiseScaleAndSpeed.zw);
                float4 noise = tex2D(_Noise, uvn);

                // ② 抖动闸：与 `_GlitchPercentaje` 占比的伪随机
                float rnd  = frac(sin(dot(t, float2(12.9898, 78.233))) * 43758.546875);
                float gate = step(1.0 - _GlitchPercentaje, rnd);

                // ③ 被挪的 uv 采样卡框
                float2 uv2 = i.uv + gate * _NoiseIntensity * noise.xy;
            #if defined(SUPERSAMPLING_ON)
                float4 c = SampleMainAntiAliased(uv2, _AntialiasScale);
            #else
                float4 c = tex2D(_MainTex, uv2);
            #endif
                // 🔴 原版这里是 `c = tex * vcol`（vcol 恒白 ⇒ 等价）。我们把它拆成 rgb / alpha 两条：
                //    `i.color` 带着 `_Color`，而**直通 alpha 混合下把 alpha 也乘进 rgb 会二次变暗**
                //    （预乘那支才该那么做）。见文件头「多出来的属性」。
                float3 crgb = c.rgb * i.color.rgb;
                float  ca   = c.a   * i.color.a;
                if (ca == 0.0) discard;                  // 原版 `eq` + `discard_nz`（卡框开口处 α=0 ⇒ 不画，立绘从下面透出来）

                // ④ 扫描线混色
                float  k   = 1.0 - _ScanLinesIntensity;
                float3 n   = noise.rgb + k;
                float3 rgb = crgb * saturate(n) + _NoiseColor.rgb * n;

                // ⑤ 我们这边的去饱和通道（原版没有；默认 0 ⇒ 逐像素一致）
                float lum = dot(rgb, float3(0.299, 0.587, 0.114));
                rgb = lerp(rgb, lum.xxx, saturate(_GreyScale));

                return float4(rgb, ca);
            }
            ENDCG
        }
    }
    Fallback Off
}
