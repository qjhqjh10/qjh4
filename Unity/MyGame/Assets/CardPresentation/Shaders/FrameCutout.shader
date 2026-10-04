// FrameCutout.shader — 卡框的「角色破框」遮罩版
//
// ⚠️ 这是**另一条路**（备选），当前默认走的是「立绘画两层」（`CardView.UseFrontLayer = true`）。
//    两条路要达到的是**同一个观感**（PnP 成品卡那种「角色压在卡框上」）：本 shader 的做法是
//    **立绘照常画一次（垫在卡框下、不遮罩），卡框在角色处被挖开一个洞** ——
//    立绘只画一次 ⇒ 没有第二层的重影。
//
// 洞从哪来：立绘贴图的 **alpha 通道**（单位卡 71–97% 全透明、战术卡 0%）——
// 它就是「哪些像素属于角色」的现成遮罩，**按立绘的 UV** 采样（`uv2`，见下）。
//    ⚠️ 上面那两个数是**旧记录**；2026-10-07 把清单（`card_cutouts.json`，**668** 张）逐张量了一遍：
//    全透明像素占比 **8%–99%**（多数落在 80–95%），战术卡那种 1024² 的整幅插画 = **0%**（不进清单）。
//
// ⚠️ 两个 UV 不一样：卡框的 UV 裁到了金属 bbox（`FrameUv`），立绘的 UV 是它自己那块 ——
//    所以卡框网格上额外写了 **`uv2` = 立绘的 UV**（见 `CardView.FrameMesh` / `CardView.ArtUvAt`），
//    这个 shader 用 `uv2` 采遮罩。
//
// 框外那圈为什么还是不透明的：立绘摆出来比卡框**小一圈**（单位卡实测 1.87×2.86 vs 卡框 2.25×3.26 卡单位），
//    `uv2` 在框的上下缘会跑出 [0,1]。**矩形外一律不挖**（mask = 0，见 `frag` 里那段注释）——
//    那里根本没有立绘，挖掉只会让卡框破个洞。
//    ⚠️ 不能靠「贴图 Clamp 会把矩形外收边到最边上那行像素、而那行 alpha = 0」——
//    2026-10-07 把 668 张会破框的立绘**逐张量过**：上边缘像素 alpha > 32 的有 **22 张**（9 张 > 128）、
//    下缘 1 张、左右各 1～4 张 ⇒ 照 Clamp 采的话那 20 多张卡的**卡框上沿会被整条挖掉**。
//
// 🔴 这条路坏过的两次（都是「遮罩与立绘对不齐」，**不在这个 shader 里**，在 C# 那侧的 uv2）：
//    · 2026-09-13：`FrameMesh` 的缓存键没带立绘 ⇒ 所有卡共用第一张卡的 uv2 ⇒ 遮罩切花（**已修**）。
//    · 2026-10-07：uv2 少乘 `ArtCoverMargin`（立绘画出来是 1.04 倍大）⇒ 遮罩被放大约 4%
//      （671×1024 单位卡算到框边缘：u 差 **16.1** 纹素 / v 差 **23.5** 纹素）⇒ 改为两边共用 `CardView.ArtUvAt`（**已修**）。
//    ⇒ 现在有**静态断言**盯着它：`CardView.CheckCutoutUvAlignment`（在 `CardBaseDemo.Run` 里跑）。
//
// ⚠️ **还没查清**：遮罩用的是立绘的**软 alpha**（角色外那圈 25% 的过渡），所以挖洞边缘是**渐隐**；
//    原版对这圈过渡怎么处理（硬切？阈值？）**没查到** —— 本地没有对应的代码/资产。
//    真要改口径，别在 shader 里偷偷加阈值：那是**加了一层原版没有的语义**。
Shader "CardPresentation/FrameCutout"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _CutMask ("Art (alpha = 角色遮罩)", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        /// 遮罩强度 0..1（1 = 完全按立绘 alpha 挖洞；0 = 不挖，退回普通卡框）
        _CutAmount ("Cut Amount", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 uvm : TEXCOORD1; fixed4 color : COLOR; };

            sampler2D _MainTex;
            sampler2D _CutMask;
            fixed4 _Color;
            float _CutAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvm = v.uv2;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // 立绘的 alpha = 角色遮罩：角色处把卡框**挖掉**（alpha 归零）。
                // 🔴 **只在立绘那块矩形内才采**（`uv2` ∈ [0,1]）：立绘比卡框小一圈，框的上下缘
                //    会落到矩形外；矩形外**没有立绘**，因此 mask = 0（不挖）。
                //    ⚠️ 少了这道判断，`Clamp` 会把立绘**最边上一整行**重复到框外 ——
                //    668 张里上缘 alpha > 32 的有 22 张 ⇒ 那 22 张卡的卡框上沿会破一条洞。
                float mask = 0.0;
                if (i.uvm.x >= 0.0 && i.uvm.x <= 1.0 && i.uvm.y >= 0.0 && i.uvm.y <= 1.0)
                    mask = tex2D(_CutMask, i.uvm).a * _CutAmount;
                c.a *= (1.0 - mask);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
