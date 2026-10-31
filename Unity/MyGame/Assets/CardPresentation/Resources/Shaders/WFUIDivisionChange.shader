// WFUIDivisionChange.shader —— 原版 `Everguild/UI/Division Change` 的**逐指令复刻**
//
// ============================ 判据（本机实读 2026-10-31 · B110_M1） ============================
// 原版 shaderlab : d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Division Change.shader
// vertex 真源码  : …/Everguild_UI_Division Change/Sub0_Pass0_Vertex_b1.hlsl:45-86
// fragment 真源码: 同文件 `.shader:115-186`（fragment 内联，两个 Pass 逐字相同）
// 材质参数       : assets_full/bundle_menus_assets_all/Material/Material_-7300284622968135969.json（`Rank Change`）
// 噪声贴图       : `_Noise` = pathID **-8214795764591836323** = Texture2D **`Noise Combined`**
//                  （在 `bundle_duplicateassetisolation_assets_all`；工程内 `Art/ui_menu/Noise_Combined.png`，
//                   guid `63bbe5a0fa2db8d4099819fffa2ec3ee`。⚠️ 不是 `UI_Dirt_And_Noise_skratches` ——
//                   那份报告 B9_W39MAT 的指认是错的，本行按 pathID 现查）· `_Noise_ST.scale = (3,3)`
// 用途/落点      : 段位变化窗 `Ranked Division Change Window` 的 `Division Image` 与 `Division Image/Rank Icon`；
//                  `_Blend` 由 legacy clip `Division Up` 经 `AnimateUIMaterialProperty.floatProperty1` 逐帧写
//                  （0 → 0.0867 → 0.2227 → 1.0486 @ 0.13/0.30/0.42s）。本 shader **不含任何 `_Time`**。
//
// ============================ 三处必须写清的移植口径 ============================
// ① **`INTERP.zw` 恒 0**：原版 `INTERP = TEXCOORD`（uv0，`Vector4`），uGUI 的 `Image` 只写 `xy`
//    ⇒ `INTERP.z = INTERP.w = 0`。而 fragment 把**四维**一起算进半径：
//       `_135.._138 = INTERP.xyzw + _DistortAmount*n - _DistortCenter.xyzw` → `_141 = sqrt(dot4)`
//    ⇒ 圆心处 r 不是 0 而是 `sqrt(0.5²+0.5²) = 0.7071`（`_DistortCenter.z/.w = 0.5`）。
//    🔴 **这一条决定观感**：`w = 0.6 + _Blend*0.4 ∈ [0.6, 1.0]`，而 r ∈ [0.7071, ≈0.96]
//    ⇒ `_Blend` 从 0 涨到 1 的过程 = 「整屏 Secondary → 一圈火线扫过 → 整屏 Main」。
//    照「二维半径」写会得到 `r(圆心) = 0`，那样 `_Blend=1` 时中心那块反而**永远 Secondary**，是错的。
// ② **`precise` 略去**：原版每条算式都带 `precise`（挡 D3D fast-math 重排）。这里保持同一算式形状，
//    但不再逐个加限定符 —— 常量与运算顺序逐字抄自 `.shader:115-186`。
// ③ **`_RendererColor * unity_SpriteColor` 换成 `_Color`**：原版 vertex 的顶点色 =
//    `_RendererColor * unity_SpriteColor * COLOR`。那两颗是 `SpriteRenderer` 的 per-draw 全局量，
//    我们的 `ImageQuad` 是 `MeshRenderer`（没有 `SpriteRenderer`）⇒ 读到的是未初始化值。
//    ⇒ 按本工程约定合成一处：**`vcol = COLOR(Image tint) * _Color(material tint)`**，`_Color` 出厂白。
// ④ **丢弃的原版属性（材质里有、算式里没有 ⇒ 死值，⛔ 别抄）**：
//    `_Color (3.2227,0,0,0)`（`Everguild/UI/Division Change` 的 Properties 里**没有** `_Color`，
//    那是上一颗 shader 的残留；DXBC 常量表里也没有它）· `_DISSOLVE_CHANNEL = 2` 与失效关键字
//    `_DISSOLVE_CHANNEL_B`（材质 `m_InvalidKeywords` 明写失效 ⇒ 走 catch-all 变体，噪声恒取 `.x`，
//    本文件的 fragment 就是那个 catch-all）· `Vector1_2780b564…` / `Vector4_d8c31f3b…`（ShaderGraph 内部槽）。
// ⑤ `_SecondaryTex = _MainTex`（见 `Shell/RankedDivisionChangeWindow.cs` 的 `ApplyDivisionChangeMaterial`）——
//    🔶 **自己设计（非原版）**：原版两张纹理都是 `m_PathID 0`，运行期由服务端 `RankedDivisions.GetRankData`
//    的 `RankArt` 各灌一张（= 段位图 + 次级图）；我们只有**一张**名次图 ⇒ 两张绑同一贴图。
//    为什么必须自定：第二张图的来源在关服后的远端包里，本地拿不到（判据 → B109_R15 §4·3）。
//    定的人/时间：调度台 2026-10-31（B110_M1）。
//
// 用法：材质挂在一张 quad 上；`_MainTex` 由 `ImageQuad.SetTexture`（`material.mainTexture`）喂，
//       `_Blend` 由 `AnimateUIMaterialProperty` 喂。两边都到位才有效果（缺一边都是静默 no-op）。
Shader "WarpforgeVFX/UI/UI Division Change"
{
    Properties
    {
        [PerRendererData] _MainTex ("MainTex", 2D) = "white" {}
        [NoScaleOffset] _SecondaryTex ("SecondaryTex", 2D) = "white" {}
        _Blend ("Blend", Range(0, 1)) = 0.77
        _Noise ("Noise", 2D) = "white" {}
        _DistortAmount ("DistortAmount", Range(0, 1)) = 0.04
        [HDR] _DissolveColor ("DissolveColor", Color) = (1, 0, 0, 1)
        [HDR] _DissolveColorSecondary ("DissolveColorSecondary", Color) = (1, 0.983102, 0, 1)
        _BorderWidth ("BorderWidth", Float) = 0.56
        _DissolveAmount ("DissolveAmount", Range(0, 1)) = 0
        _DistortCenter ("DistortCenter", Vector) = (0.5, 0.5, 0.5, 0.5)
        _ColorBorderWidth ("ColorBorderWidth", Float) = -0.02
        // 本工程约定（见文件头 ③）：原版没有这个属性，`_Color` 只承载 material tint
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        // 原版 Tags：QUEUE Transparent / RenderType Transparent（另两条 ShaderGraph 专用标签对渲染无影响）
        Tags
        {
            "QUEUE"="Transparent"
            "RenderType"="Transparent"
            "IGNOREPROJECTOR"="true"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="true"
        }
        Pass
        {
            Name "Sprite Unlit"
            // 原版：`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            // 原版：`ZClip On`（URP 专有指令，内置管线 ShaderLab 无此命令 ⇒ 省略；它只影响屏幕外裁剪）
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            // ⚠️ 故意**不写 LightMode** ⇒ URP 走 `SRPDefaultUnlit`（同 `WFUIPulsating.shader` 的先例）

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _SecondaryTex;
            sampler2D _Noise;
            float4 _Noise_ST;
            fixed4 _Color;
            float  _Blend;
            float  _DistortAmount;
            // ⚠️ 这两颗是 `[HDR]`，值 >1（1.5157 / 0.9245）⇒ 必须 `float4`（`fixed` 在部分平台是 [-2,2]）
            float4 _DissolveColor;
            float4 _DissolveColorSecondary;
            float  _BorderWidth;
            float  _DissolveAmount;
            float4 _DistortCenter;
            float  _ColorBorderWidth;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // 原版 `INTERP = TEXCOORD` —— **裸 uv**，不过 `_MainTex_ST`
                //（`[NoScaleOffset] _MainTex`，材质里 ST 恒 (1,1,0,0)）
                o.uv = v.uv;
                // 原版 `INTERP_1 = _RendererColor * unity_SpriteColor * COLOR`（见文件头 ③）
                o.color = v.color * _Color;
                return o;
            }

            // linear → gamma（原版 `.shader:153-177` 的 12.9232 / log2·(1/2.4) / 1.055−0.055 三件套）
            float3 LinToGamma3(float3 c)
            {
                float3 lo = c * 12.92321014404296875;
                float3 hi = exp2(log2(abs(c)) * 0.4166666567325592041) * 1.0549999475479125977
                          - 0.05499999970197677612;
                // 原版逐分量 `(c <= 0.0031308) ? lo : hi`
                return lerp(lo, hi, step(0.0031308000907301903, c));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // `_98 = noise(uv*_Noise_ST.xy + _Noise_ST.zw).x * 2 - 1`（:118）
                float n = tex2D(_Noise, i.uv * _Noise_ST.xy + _Noise_ST.zw).x * 2.0 - 1.0;

                // `_120 = _Blend * 0.4 + 0.6`（:120）—— 半径随 `_Blend` 扩张
                float w = _Blend * 0.39999997615814208984375 + 0.60000002384185791015625;

                // `_135.._138`（:121-124）：四维一起算，`INTERP.zw = 0`（见文件头 ①）
                float4 p = float4(i.uv, 0.0, 0.0) + _DistortAmount * n - _DistortCenter;
                float  r = sqrt(dot(p, p));                                    // `_141`

                // `_148 = 1 - saturate((r - w) * 11.1111145)`（:127-129）
                float  k = 1.0 - saturate((r - w) * 11.111114501953125);

                float4 mA = tex2D(_MainTex, i.uv);                             // `_161`
                float4 sB = tex2D(_SecondaryTex, i.uv);                        // `_171`

                // `_155 = (_DissolveAmount >= k)` · `_156 = float(_155)`（:130-132）
                float  u2 = (_DissolveAmount >= k) ? 1.0 : 0.0;
                float  a  = lerp(mA.a, sB.a, u2);                              // `_185`
                if (a == 0.0) discard;                                         // `:144-147`

                // `_197 = (_155 ? -1 : 0) + float((_BorderWidth + _DissolveAmount) >= k)`（:148-149）
                float  borderN = ((_BorderWidth + _DissolveAmount) >= k) ? 1.0 : 0.0;
                float  border  = borderN - u2;

                // `_280 = 1 - saturate((r - (w - _ColorBorderWidth)) * 11.1111145)`（:171-174）
                float band = 1.0 - saturate((r - (w - _ColorBorderWidth)) * 11.111114501953125);
                // 🔴 就地订正 B109_R15 §1·1 的写法：`mad(_280, Pri−Sec, Sec)` = `lerp(Sec, Pri, _280)`
                //    —— 内侧（band→1）取 **`_DissolveColor`**，外侧取 **`_DissolveColorSecondary`**。
                //    R15 写的是 `lerp(_DissolveColor, _DissolveColorSecondary, …)`，方向反了（本文件按 `.shader:175-177` 走）。
                float3 bc = lerp(LinToGamma3(_DissolveColorSecondary.rgb),
                                 LinToGamma3(_DissolveColor.rgb), band);

                // `_298.._300 = (lerp(main,sec,u2) + border*bc) * INTERP_1.xyz` · `_301 = a * INTERP_1.w`
                float3 rgb = lerp(mA.rgb, sB.rgb, u2) + border * bc;
                return fixed4(rgb * i.color.rgb, a * i.color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
