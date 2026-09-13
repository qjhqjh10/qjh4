// WarpforgeShaderMap.cs — 原版 shader 名 → 本工程 shader 的映射
//
// 原版自定义 shader 的源码被剥离、无法落成工程资产，所以自建功能等价版顶上。
// 属性名与原版保持一致，原版材质的数值可以直接灌进来，不需要转换。
//
// 解析优先级：
//   1. 自建替代 shader（工程资产，可改、跨平台）  ← 推荐
//   2. 工程自带的同名 shader（URP / Sprites / UI 等标准 shader）
//   3. 原版 shader bundle（工程里没有的 Everguild 自定义 shader 才走这条；
//      bundle 里那份在**编辑器下渲染会出故障**，只适合运行时用）
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WarpforgeShaderMap
    {
        /// <summary>优先用自建 shader 而不是原版 bundle</summary>
        public static bool PreferBuiltIn = true;

        /// <summary>原版 shader 名 → 本工程 shader 名</summary>
        public static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            // 自定义 ShaderGraph shader —— 源码被剥离，自建替代
            { "Everguild/FX/Extra Color", "WarpforgeVFX/Particles/Extra Color" },
            // 本质是 Unity 内置 Sprites/Default 的加法版（7 个属性、Blend One One 写死）。
            // 不映射的话会掉到默认的 URP 粒子 shader —— 属性名对不上，_MainTex/_Color 全丢，
            // 粒子直接渲染成空（ArdAsNailsEffect 的 Back Glow 就是这么没的）
            { "Everguild/Sprites/Sprite Additive", "WarpforgeVFX/Sprites/Additive" },
            // 抓屏扭曲，用 _DistortTex 偏移重采样场景颜色。影响 233 个效果，是第二大缺口
            { "Everguild/FX/Particle Distortion Affect Transparents", "WarpforgeVFX/FX/Distortion" },
            // matcap：用视图空间法线采样「从正面看一个球」的贴图，不需要实时光照。
            // 两个原版 shader 属性表一样，一个 shader 顶两个
            { "Everguild/Matcap/Matcap Full Options", "WarpforgeVFX/Matcap/Matcap" },
            { "Everguild/Matcap/Matcap With Texture", "WarpforgeVFX/Matcap/Matcap" },

            // ---- 🆕 2026-09-13 第三十三轮补的 10 个 ------------------------------------
            // ⚠️ 这张表与 `EffectExporter.ShaderMap` **是两份，改一份必须同步另一份**
            //    （那边管「占位材质用什么 + 报告标不标近似」，这边管「运行时重建材质解析到谁」）。
            // 这 10 个原来两边都没有 ⇒ 运行时 `Shader.Find` 也找不到 ⇒
            // `WarpforgeEffectBinder` 返回 null ⇒ 那个材质槽**保留占位材质**（16 条效果）。
            // ⚠️ **全部是近似**：我们拿不到这些 shader 的属性表，只按名字挑最接近的自建 shader
            //    （真实的溶解 / UV 滚动 / 顶点流**没做**）。
            { "Everguild/FX/Particle Dissolve Mask",                  "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Alpha Mask One Layer",                    "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Alpha Masks Two Layer",                   "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Multi Ray",                               "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Particle Shine Custom Vertex Streams",    "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Particle Premultiply Greyscale Coloring", "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Unlit UV scroll",                         "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/TrailShader_1",                           "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/TrailShader_Fading",                      "WarpforgeVFX/Particles/Extra Color" },
            { "Shader Graphs/Doomweaver effect",                      "WarpforgeVFX/Particles/Extra Color" },
            // 非粒子的 Everguild shader —— 必须指到 URP/Unlit。
            // 让它们掉进默认的 URP **Particles**/Unlit 会连粒子专用逻辑一起套上（实测过曝 4 倍）
            { "Everguild/UnlitAmbient",                   "WarpforgeVFX/UnlitAmbient" },
            { "Everguild/UnlitAmbient Emissive Flickker", "WarpforgeVFX/UnlitAmbient" },
            { "Everguild/Unlit Wind",                     "WarpforgeVFX/UnlitAmbient" },

            // 内置管线（Built-in RP）的老粒子 shader —— 在 URP 工程里会渲染成粉色。
            // 混合是写死在 shader 里的，材质上根本没有 _SrcBlend，所以还必须靠
            // InferBlend() 补上正确的混合模式。
            { "Mobile/Particles/Additive",            "WarpforgeVFX/Particles/Extra Color" },
            { "Mobile/Particles/Alpha Blended",       "WarpforgeVFX/Particles/Extra Color" },
            { "Mobile/Particles/Multiply",            "WarpforgeVFX/Particles/Extra Color" },
            { "Particles/Standard Unlit",             "WarpforgeVFX/Particles/Extra Color" },
            { "Particles/Additive",                   "WarpforgeVFX/Particles/Extra Color" },
            { "Legacy Shaders/Particles/Additive",    "WarpforgeVFX/Particles/Extra Color" },
            { "Legacy Shaders/Particles/Alpha Blended", "WarpforgeVFX/Particles/Extra Color" },
            { "Legacy Shaders/Particles/Alpha Blended Premultiply", "WarpforgeVFX/Particles/Extra Color" },
            { "Legacy Shaders/Particles/Anim Alpha Blended", "WarpforgeVFX/Particles/Extra Color" },
            { "UI/Additive",                          "WarpforgeVFX/Particles/Extra Color" },
        };

        /// <summary>有些原版 shader 把混合写死在 shader 里，材质上没有 _SrcBlend。
        /// 这时必须按 shader 名把混合补回去，否则加法发光会变成不透明。
        /// 返回 null 表示该 shader 能自己表达混合，不用补。</summary>
        public static float[] InferBlend(string originalName)
        {
            if (string.IsNullOrEmpty(originalName)) return null;
            string n = originalName.ToLowerInvariant();
            // Sprite Additive 是 Blend One One（纯加法），不是 SrcAlpha/One
            if (n.Contains("sprites/sprite additive") || n.Contains("sprites/additive"))
                return new[] { 1f, 1f, 0f };                       // One / One
            if (n.Contains("additive") || n.EndsWith("/add"))
                return new[] { 5f, 1f, 0f };                       // SrcAlpha / One，加法
            if (n.Contains("premultiply"))
                return new[] { 1f, 10f, 0f };                      // One / OneMinusSrcAlpha
            if (n.Contains("multiply"))
                return new[] { 2f, 0f, 0f };                       // DstColor / Zero
            if (n.Contains("alpha blended") || n.Contains("anim alpha"))
                return new[] { 5f, 10f, 0f };                      // SrcAlpha / OneMinusSrcAlpha
            return null;
        }

        /// <summary>已被自建 shader 覆盖的原版 shader 名单</summary>
        public static IEnumerable<string> Covered { get { return Replacements.Keys; } }

        /// <summary>诊断用：打印解析链各环节是否可用</summary>
        public static string Describe()
        {
            int bundleShaders = 0;
            foreach (var _ in WarpforgeShaderLoader.ShaderNames) bundleShaders++;
            return $"PreferBuiltIn={PreferBuiltIn} 自建替换={Replacements.Count} 条 " +
                   $"原版bundle={(WarpforgeShaderLoader.Ready ? $"{bundleShaders} 个 shader" : "未加载")}";
        }

        public static bool TryResolve(string originalName, out Shader shader, out string source)
        {
            shader = null; source = null;
            if (string.IsNullOrEmpty(originalName)) return false;

            if (PreferBuiltIn && Replacements.TryGetValue(originalName, out var mine))
            {
                shader = Shader.Find(mine);
                if (shader != null) { source = "自建"; return true; }
            }
            // 工程自带的同名 shader 排在 bundle 前面。
            //
            // 实测（Psychic_Lightning_down，同一个原版材质，只换 shader）：
            //   bundle 里的 URP Particles/Unlit   → 整片品红（编辑器下渲不出来）
            //   工程自带的 URP Particles/Unlit    → 干净正常
            // 两者是**不同的 Shader 对象**（GetInstanceID 不同）。
            //
            // bundle 里那份只在【运行时才能真正工作】，在编辑器里渲染会出故障 ——
            // 这会让「原版 vs 导出」的比对基准整个失真（原版那一侧全是品红）。
            // 所以只要工程里有同名 shader 就用工程的；bundle 只留给工程里没有的
            // Everguild 自定义 shader 兜底。
            shader = Shader.Find(originalName);
            if (shader != null) { source = "工程自带"; return true; }
            if (WarpforgeShaderLoader.TryGetShader(originalName, out shader))
            {
                source = "原版bundle";
                return true;
            }
            return false;
        }
    }
}
