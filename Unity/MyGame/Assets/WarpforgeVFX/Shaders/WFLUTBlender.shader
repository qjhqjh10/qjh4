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
//       🆕 **2026-10-23（铁律 5 补）：它也在 `StreamingAssets/WarpforgeVFX/wf_arena_2.bundle` 里**
//       （`Shader.path_id = 42`，带完整 DXBC：压缩 2027 B → 解压 3504 B / 两段）——
//       复核这条链**不必去翻 13 个场景包**，跑 `工具/dump_lutblender.py` 一条命令即可
//       （判据 → `资料/普查产出_1023/E4_R29分因与R1判据.md` §2·3 / §⑤-4）。
//    `Shader.compressedBlob` 解出 3504 字节，两段 DXBC（偏移 446 / 1278）：
//      --- vs_4_0（684 B）---  见下一条（**2026-10-23 订正：原版 VS 不是直通**）
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
// 🔴🔴 **2026-10-23 就地订正（W65 · A1 的 `R1` · 铁律 5）—— 本文件原来写着「原版 vs 就是直通」，错的**：
//   原版 `vs_4_0` 的**第一条指令就是 UV 的 scale/bias**（本件重跑 `工具/dump_lutblender.py` 现读）：
//       `dcl_constantbuffer CB0[3], immediateIndexed`
//       `mad o0.xy, v1.xyxx, cb0[2].xyxx, cb0[2].zwzz`      // ← uv * ST.xy + ST.zw
//   而我们当时写的是 `o.uv = v.uv;`（**少了这一跳**）。
//   为什么一直没被发现：`BattlePostFx` 是 `Graphics.Blit(null, rt, mat)`，`_MainTex` 未绑定
//   ⇒ 各张 `*_ST` 都是**默认 (1,1,0,0) = 单位变换** ⇒ 两侧等价。
//   但只要出现**非单位 ST**（源是真实纹理 / 走翻转投影的渲染通道 / 立体渲染），原版会缩放+平移、
//   我们不会 ⇒ 256×16 的 LUT 会被**上下颠倒**（16 行 = 3D LUT 的纵轴）。
//   ⇒ 已按原版补上这一跳（见 `vert()`）。
//   ⚠️ **`cb0[2]` 到底是哪一张 `_ST`，查不到**：内置管线 shader 的常量缓冲反射
//   （`SerializedPass.m_CommonParameters`）**没有序列化**（本件实读 `m_CommonParameters = None`），
//   而 cb0 的槽位排布又拿不到 `m_ConstantBuffers` ⇒ **无法判定它是 `_LUT1_ST` / `_LUT2_ST` 还是别的**。
//   取 `_LUT1_ST`（主纹理、原版只读那一张）—— **今天**这三张 ST 全是单位值，取哪一张**渲染逐像素相同**；
//   将来真要接非单位 ST，先用 `LutBlendProbe` 的 A7 条（见下）对一次原件再定。
//   判据 → `资料/普查产出_1023/E4_R29分因与R1判据.md` §1·D（新发现）+ 本文件 2026-10-23 补测。
//   专验法（要 Unity）：`LutBlendProbe.Run` 的 A1–A7 七条断言 —— 方案在
//   `资料/普查产出_1023/E4_R29分因与R1判据.md` §④-1；⛔ 本轮红线不跑 Unity，故**判据已齐、验证待跑**。
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
                // 🆕 2026-10-23（R1）：原版 VS 的 `mad o0.xy, v1.xyxx, cb0[2].xyxx, cb0[2].zwzz` 要这一格。
                // ⚠️ 哪一张 `_ST` 查不到（见文件头那段订正）⇒ 取主纹理 `_LUT1` 的；默认 (1,1,0,0) 时与直通等价。
                float4 _LUT1_ST;
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
                // 原版 vs（反汇编第一条指令）：`mad o0.xy, v1.xyxx, cb0[2].xyxx, cb0[2].zwzz`
                //   = uv * ST.xy + ST.zw   ← **2026-10-23 补**（原来写成直通 = 少了这一跳，见文件头订正）
                o.uv = v.uv * _LUT1_ST.xy + _LUT1_ST.zw;
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
