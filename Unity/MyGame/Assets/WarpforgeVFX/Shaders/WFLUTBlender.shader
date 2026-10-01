// WFLUTBlender.shader — 自建，替代原版 `Hidden/LUTBlender`
//
// 判据（**全部实读 / 实解，不是猜**）
// ----------------------------------------------------------------
//  · **属性表 3 条，一字不差** → `资料/普查产出_0917/shader属性表_块3.md:294-300`：
//      `_LUT1` Texture def "white" · `_LUT2` Texture def "white" · `_Blend` Range def 0 [0..1]
//  · **pass 状态**（同一处）：`RenderType=Opaque` · 混合 **One→Zero**（不透明、不混合）
//      · zW=On · zT=LEqual · cull=Back · LOD 100
//  · 🔴 **片元算式 = 反汇编读出来的**（原版 HLSL 源码被剥，但**编译字节码在**）。
//    工具：`工具/disasm_dxbc.py` 的 `_d3d_disassemble` + `split_dxbc`（Windows 自带 `d3dcompiler_47.dll`）。
//    ⚠️ **它不在 `dump_shader_blob.BUNDLES` 那 3 个包里** —— 它在**每个战场场景包**
//       （`scenes_scenes_battlearena*`，13 份）里 ⇒ 要自己指源包。
//    `Shader.compressedBlob` 解出 3504 字节，两段 DXBC（偏移 446 / 1278）：
//      --- vs_4_0（684 B）---  只把 `v1.xy` 直通到 `o0.xy` + 常规 MVP 变换
//                              （**没有 3D LUT 坐标运算**）
//      --- ps_4_0（452 B）---
//        dcl_resource_texture2d (float,float,float,float) t0
//        dcl_resource_texture2d (float,float,float,float) t1
//        sample r0.xyzw, v0.xyxx, t1.xyzw, s1     // t1 = _LUT2
//        sample r1.xyzw, v0.xyxx, t0.xyzw, s0     // t0 = _LUT1
//        add    r0.xyzw, r0.xyzw, -r1.xyzw        // _LUT2 − _LUT1
//        mad    o0.xyzw, cb0[3].xxxx, r0.xyzw, r1.xyzw
//        ret
//    ⇒ **`o = lerp(_LUT1, _LUT2, _Blend)`**（`cb0[3].x` = `_Blend`）
//    ⇒ 是 **`texture2d` + 同一个 UV**、在 **256×16 展开空间里逐纹素插值**（**不是** `texture3D`）。
//      这正是「混两张 color-grading LUT」应有的语义 —— 在 3D 空间插值反而是错的。
//    判据全文 → `资料/普查产出_1001/资产导入路三件_侦察.md` §3·5·a。
//
// 🔴 **两处刻意的选择**（写明白，别当成疏漏）
//  · **名字不叫 `Hidden/LUTBlender`**：自建 shader 一律 `WarpforgeVFX/` 前缀；
//    「原版名 → 我们这份」的映射写在 `WarpforgeShaderMap.Replacements` 里（全工程同一条约定）。
//  · **不直接用原版那份编译字节码**：bundle 里的 shader 在**编辑器 / 批处理下渲染会出故障**
//    （实测整片品红，见 `WarpforgeShaderMap.TryResolve` 的注释）—— 而**自检就跑在批处理里**，
//    用原版那份等于整条 LUT 链在自检里失效。算式已经逐条解出来了，自建一份行为等价。
Shader "WarpforgeVFX/LUTBlender"
{
    Properties
    {
        _LUT1 ("Texture", 2D) = "white" {}
        _LUT2 ("Texture", 2D) = "white" {}
        _Blend ("Blend", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        // 原版 pass 状态：One→Zero · zW=On · zT=LEqual · cull=Back
        Blend One Zero
        ZWrite On
        ZTest LEqual
        Cull Back

        Pass
        {
            Name "LUTBlend"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ⚠️ 本文件是 **URP 的 HLSL 风格**（`HLSLPROGRAM` + `Core.hlsl`），
            //    所以**不能用 `fixed4`**（那是 CG 时代的类型，由 `HLSLSupport.cginc` 带进来，
            //    这里不会被包含）—— 一律 `half` / `float`。同族教训见 `WFSpritesAdditive.shader:104-112`。
            CBUFFER_START(UnityPerMaterial)
                float _Blend;
            CBUFFER_END

            TEXTURE2D(_LUT1);   SAMPLER(sampler_LUT1);
            TEXTURE2D(_LUT2);   SAMPLER(sampler_LUT2);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                // 原版 vs 就是**直通**（`mad o0.xy, v1.xyxx, cb0[2].xyxx, cb0[2].zwzz` —— 无滚动/无缩放）
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 a = SAMPLE_TEXTURE2D(_LUT1, sampler_LUT1, i.uv);
                half4 b = SAMPLE_TEXTURE2D(_LUT2, sampler_LUT2, i.uv);
                return lerp(a, b, (half)_Blend);      // ← 反汇编那 4 条指令，一字不改
            }
            ENDHLSL
        }
    }
    Fallback Off
}
