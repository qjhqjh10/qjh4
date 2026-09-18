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

        /// <summary>🔴 **这些原版名改走「原件」** —— 即跳过 <see cref="Replacements"/>，
        /// 直接落到解析链的最后一步（`WarpforgeShaderLoader` 从随包 bundle 取原版 shader 本体）。
        ///
        /// **为什么单开一张白名单，而不是把 <see cref="Replacements"/> 里的条目删掉**：
        /// `Replacements` 与 `EffectExporter.ShaderMap` 按文件头注释**必须同步**，而后者管的是
        /// **导出期占位材质用什么** —— 占位材质**不能**是 bundle 里的 shader（`Shader.Find` 拿不到
        /// 非工程资产）。所以「改走原件」这件事只该影响**运行时最终解析**，不该动导出期那张表。
        /// 单开白名单还让这件事**一句话可回滚**（把名字从这张表里删掉即可）。
        ///
        /// **怎么定的这些名字**：`资料/普查产出_0918/shader原件可用性_表.md`
        /// （由 `工具/survey_shader_originals.py` 实读两个 bundle 生成）。
        /// 入选条件 = ① 是 **Everguild / Shader Graphs 的自定义 shader**（不是 Unity 内建）②
        /// **原件确实在两个随包 bundle 之一里**。
        /// ⚠️ **Built-in 管线的老 shader（`Mobile/Particles/*` · `Legacy Shaders/Particles/*` ·
        /// `Particles/*` · `Sprites/*` · `UI/Default`）不在名单里，也永远不该进** ——
        /// 它们在 URP 工程里本来就渲染不了，自建替代才是对的。
        ///
        /// ✅ **2026-09-18 用户取消版权红线**（个人学习用途）⇒ 走原件 = 把原版编译字节码打包带走，**不再要求发布前处理**。
        ///    （原写「与 `Resources/Art/` 同一条红线」—— 那条已取消，见 `项目任务.md` §二「版权」。）</summary>
        public static readonly HashSet<string> UseOriginal = new HashSet<string>
        {
            // ---- 2026-09-18 第一批（原件在随包 bundle 里、且我们原本用的是「按名字挑的近似」）----
            "Everguild/Matcap/Matcap Full Options",                    // 120 条效果
            "Everguild/Matcap/Matcap With Texture",                    //  70
            "Everguild/FX/Unlit UV scroll",                            //  39
            "Everguild/FX/Multi Ray",                                  //  33
            "Everguild/FX/Alpha Mask One Layer",                       //  29
            "Everguild/FX/Particle Dissolve Mask",                     //  29
            "Everguild/FX/Particle Shine Custom Vertex Streams",       //  26
            "Everguild/FX/Particle Premultiply Greyscale Coloring",    //  25
            "Everguild/FX/Particle Premultiply",                       //  12
            "Shader Graphs/Fx_ParticleDissolve_apb",                   //  11
            "Everguild/FX/Alpha Masks Two Layer",                      //   9
            "Everguild/FX/TrailShader_1",                              //   8
            "Everguild/UnlitAmbient",                                  //   8
            "UI/Additive",                                             //   7
            "Shader Graphs/Fx_RockDissolve",                           //   4
            "Shader Graphs/Doomweaver effect",                         //   3
            "Shader Graphs/Eclipse Tau",                               //   3
            "Everguild/FX/TrailShader_Fading",                         //   2
            "Everguild/Sprites/Sprite Additive",                       //   1
            "Everguild/UnlitAmbient Emissive Flickker",                //   1
            "Everguild/Unlit Wind",                                    //   0
            // ⏸ **暂缓的两个大头**（改它们会一次性动 ~974 条效果，要单独一批 + 一次全量 sweep 量过再动）：
            //   `Everguild/FX/Extra Color`（741）· `Everguild/FX/Particle Distortion Affect Transparents`（233）
        };

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
            // 🆕 2026-09-15：C 组「导出整个丢了」的 `Explosion_Ground` 用的就是这一个。
            // 🔴 **2026-09-17 更正：这里原来写「它不在任何我们随包走的 shader 包里」—— 那条是错的。**
            //    实读 `wf_shaders_extra.bundle` 的 `m_Container`（共 42 条）：`Everguild/FX/Particle
            //    Premultiply` **就在里面**（与 `…/Particle Dissolve Premultiply` 是两个不同的
            //    Shader 对象，前者带编译字节码、不是桩）。
            //    **错因**：当时是拿**裸字节 grep** 这个包的 —— 而它的数据块是 **LZ4 压缩**的，
            //    42 条容器名里**只有 6 条**能在裸字节里搜到（这一条恰好搜不到，**已实测复现**）
            //    ⇒ 「搜出 0 命中」被当成了「不存在」。同铁律 2：**「没找到」≠「不存在」**。
            //    ⇒ 「`Shader.Find` 返回 null ⇒ 材质槽保留占位材质 ⇒ 整块渲不出来」这个**根因要重查**；
            //    目前更像踩的是 `资料/特效还原_进度与交接.md` 里那条
            //    「导出那趟加载源包会把补充包顶掉」。
            //    ⚠️ 顺带一条**还没做的改进**：既然原版 shader 就在我们随包的补充包里，
            //    这一条（以及同类「近似替代」）其实可以试着改走**原版 shader 本体**。
            //    ⚠️ **近似**：拿不到属性表，按名字挑最接近的自建 shader（预乘 alpha 那套
            //    在 `WarpforgeVFX/Particles/Extra Color` 里有 `_ALPHAPREMULTIPLY_ON` 支）。
            { "Everguild/FX/Particle Premultiply",                    "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/Unlit UV scroll",                         "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/TrailShader_1",                           "WarpforgeVFX/Particles/Extra Color" },
            { "Everguild/FX/TrailShader_Fading",                      "WarpforgeVFX/Particles/Extra Color" },
            { "Shader Graphs/Doomweaver effect",                      "WarpforgeVFX/Particles/Extra Color" },
            // 🆕 2026-09-15：**同一条根因的另外三个**（`Explosion_Ground` 那次只补了上面一行）。
            //    这三张 ShaderGraph 原版是**自定义 shader 图**，既不在我们随包的 shader 包里、
            //    也没有映射表条目 ⇒ `Shader.Find` 返回 null ⇒ 材质槽保留占位材质 ⇒ **整块渲不出来**。
            //    影响面（`grep Prefabs/*.prefab` 实测）：
            //      · `Fx_ParticleDissolve_apb` —— **13 个 prefab**，含 C 组剩下的 `BlastEffect`
            //      · `Fx_RockDissolve`         —— **4 个 prefab**，含 C 组剩下的 `BlastEffect`
            //      · `Eclipse Tau`             —— 3 个 Tau 环境效果
            //    ⚠️ **近似**（和上面那条一样）：拿不到 ShaderGraph 的属性表，按名字挑最接近的自建 shader。
            //    补完要**重跑全量扫描重出台账**看 C 组是不是清零（别只看这三个效果）。
            { "Shader Graphs/Fx_ParticleDissolve_apb",                "WarpforgeVFX/Particles/Extra Color" },
            { "Shader Graphs/Fx_RockDissolve",                        "WarpforgeVFX/Particles/Extra Color" },
            { "Shader Graphs/Eclipse Tau",                            "WarpforgeVFX/Particles/Extra Color" },
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

            // ---- 🆕 2026-09-16（构建后 player 验证抓出来的）--------------------------------
            // **原版的 TMP 变体名**：`TextMeshPro/Distance Field Offset` 在我们的 TMP Essentials 里
            // **不存在**（本地 13 张 TMP shader 里没有这个名字，只有原版 `CardPrefab.prefab` 引用它）。
            // 不映射的话运行时报「找不到 shader」⇒ 那个材质槽保留占位材质 ⇒ **整块渲成洋红**
            //（实测：白板 `CardPrefab` 那一格是一大块洋红）。
            // ⚠️ **近似**：拿不到原版那张 shader 的属性表，按名字挑最接近的（就是标准的 Distance Field）。
            { "TextMeshPro/Distance Field Offset",    "TextMeshPro/Distance Field" },
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
                   $"改走原件={UseOriginal.Count} 条 " +
                   $"原版bundle={(WarpforgeShaderLoader.Ready ? $"{bundleShaders} 个 shader" : "未加载")}";
        }

        public static bool TryResolve(string originalName, out Shader shader, out string source)
        {
            shader = null; source = null;
            if (string.IsNullOrEmpty(originalName)) return false;

            // 🔴 「改走原件」白名单**排在最前面**：这几个名字跳过自建替代，直接去 bundle 取原版本体。
            //    放在 `PreferBuiltIn` 之前是**故意**的 —— 它比「优先自建」这个总开关优先级更高，
            //    否则开关一开就把白名单也一起关掉了，那种「关了但没完全关」最难查。
            bool wantOriginal = UseOriginal.Contains(originalName);

            if (!wantOriginal && PreferBuiltIn && Replacements.TryGetValue(originalName, out var mine))
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
                source = wantOriginal ? "原版bundle（白名单）" : "原版bundle";
                return true;
            }
            return false;
        }
    }
}
