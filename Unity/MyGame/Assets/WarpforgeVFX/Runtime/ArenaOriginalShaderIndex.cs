// ArenaOriginalShaderIndex.cs — 13 场**原版 prefab** 的「材质名 → 原版 shader 名」索引（生成件 · 2026-10-23 · 战场线 T00·P0-2）
//
// 🔴 **正本不是这份 .cs**：正本是
//    `Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/arena_shader_guid_map.tsv`
//    （18 行：`old_guid → 原版 shader 名 → 材质数 → 产品包 → 材质清单`；判据见
//      `资料/普查产出_1023d/战场_T00引用修复_执行报告.md` §2）。
//    本文件是它的**运行期可读副本**；两张数组按 (shader 名, 材质名) 排序、平行等长。
//
// **为什么选「生成 C# 静态表」，不用 `Resources/` 或 `StreamingAssets/`**：
//   ① 与 `OriginalShaderBlendTable`（同样由 `工具/gen_shader_blend.py` 生成 C# 表）**同一种做法**，
//      不新增第二种「读表」通道；
//   ② 运行期**零 I/O、零平台差异** —— `Awake` 里同步就能用。`StreamingAssets` 在部分平台要
//      `UnityWebRequest`（异步）⇒ `Awake` 里读不到；`Resources.Load<TextAsset>` 则要**再拷一份**进
//      `Resources/`（多一份拷贝 = 多一处会漂移的地方）；
//   ③ 编译期就能 `grep` 到，探针里能直接遍历；
//   ④ **漂移由探针逐条对 tsv 断言**（`ArenaOriginalShaderRebindProbe` 的 A 组：18 行 / 17 个 shader 名 /
//      37 个材质 / 每个 `.mat` 的 `m_Shader` guid 逐条对上）—— 改了 tsv 忘了改这里，探针会红，不靠人记。
//
// ⚠️ **材质名不是全局唯一** —— 2026-10-23 现读：这 37 个里有 **6 个在工程别处有同名 `.mat`**
//    （`Battle Arena 1 Floor` / `Battle Arena 2 Ground` / `Astra Militarum Ground Atlas` 在
//      `WarpforgeArena1/arenas/*/Materials/`；`Necrons Flying Monolyth Rays` / `Vortex portal` /
//      `HiddenPass` 在 `WarpforgeVFX/Materials/`）。
//    ⇒ **这张表只是【兜底】**：`ArenaOriginalShaderRebind` 优先按 prefab 上挂的 `targets`（**对象引用**）匹配，
//      并且**只在材质当前 shader 是坏的**（null / `Hidden/InternalErrorShader`）时才查这张表。
//      工程里那些「同名但 shader 正常」的 `.mat` 永远不会被这张表碰到（它们走「已正确 ⇒ 跳过」那一支）。
//
// **重新生成**：从 tsv 的 `orig_shader_name`（第 2 列）与 `mats`（第 5 列，`;` 分隔）逐行展开，
//    材质名 = 路径最后一段去掉 `.mat`，再按 (shader 名, 材质名) 排序。
//    ⚠️ 同一个 shader 名会出现多行（`Everguild/FX/Alpha Mask One Layer  Color Ramp` 有两个 guid：
//      `fc85a458…` 与 `85e3ade8…`）⇒ **去重后 17 个名字、18 行**。别把 18 当成名字数。
using System.Collections.Generic;

namespace WarpforgeVFX
{
    /// <summary>13 颗原版 prefab 里「丢了 shader 的材质」→ 原版 shader 名的运行期索引。见文件头。</summary>
    public static class ArenaOriginalShaderIndex
    {
        /// <summary>正本（`Assets/` 相对路径）。探针拿它逐行对账；运行期**不读**它（见文件头「为什么」）。</summary>
        public const string TsvAssetPath =
            "Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/arena_shader_guid_map.tsv";

        /// <summary>这 37 个材质一共用到 **17 个**原版 shader 名（tsv 18 行 = 17 名 + 1 个重复名）。</summary>
        public static readonly string[] ShaderNames =
        {
            // ⚠️ 这个名字里有**两个空格**（原版就是这么命名的，⛔ 别手改成单空格）
            "Everguild/FX/Alpha Mask One Layer  Color Ramp",
            "Everguild/FX/Burning",
            "Everguild/FX/Floor Planar Reflections Grainny",
            "Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask",
            "Everguild/FX/Simple Fake Water",
            "Everguild/FX/Specific/Necrons Rays",
            "Everguild/FX/Specific/Tau Generator Energy",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Tyranid Tentacle",
            "Everguild/FX/Vortex",
            "Everguild/Misc/Unlit shadows receiver",                                // ← 9 块地面（品红最扎眼的那批）
            "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad",
            "Spine/Special/HiddenPass",
        };

        /// <summary>37 个材质（材质资产名，不含 `.mat`）。与 <see cref="MaterialShaderNames"/> 平行、等长。
        /// ⚠️ 13 颗 Scenario prefab **只实例化到其中 32 个**；剩下 5 个（`Hidden_VFX_Aeldari Dust…` +
        /// 4 个 `Hidden_VFX_Aeldari Portal…`）由 **VFX Graph 资产**（`_shared/Aeldari Dust.asset` /
        /// `battlearenaaeldari/Aeldari Portal.asset`）引用 ⇒ 13 颗 prefab 的树里**一个都碰不到**，
        /// 只能按「材质资产级」验（探针 B 组）。</summary>
        public static readonly string[] MaterialNames =
        {
            "Glow Charge Lines",                                       // ← Everguild/FX/Alpha Mask One Layer  Color Ramp
            "Water Flow",
            "Claudron Fire",                                           // ← Everguild/FX/Burning
            "Battle Arena Aeldari Ground",                             // ← …Floor Planar Reflections Grainny
            "Ground",
            "Sororitas Ground",
            "Battle Arena Tau Viorla Ground",                          // ← …Vertex color shadow mask
            "Battle Arena 2 Oil lake",                                 // ← Everguild/FX/Simple Fake Water
            "Necrons Flying Monolyth Rays",                            // ← Everguild/FX/Specific/Necrons Rays
            "Plasma Generator Energy Exterior",                        // ← Everguild/FX/Specific/Tau Generator Energy
            "Plasma Generator Energy Interior",
            "Battle Arena Leviathan Background 1 -pulsating",           // ← Everguild/FX/Tyranids/Pulsating Mesh
            "Battle Arena Leviathan Dynamic Objects - pulsating",
            "Battle Arena Leviathan Props 1 - pulsating",
            "Battle Arena Leviathan Props 1 - pulsating small",
            "Battle Arena Leviathan Props 2",
            "Battle Arena Leviathan Props 2 - pulsating",
            "Battle Arena Leviathan Biomass 1 - moving",                // ← Everguild/FX/Tyranids/Tyranid Tentacle
            "Battle Arena Leviathan Biomass 2 - moving",
            "Battle Arena Leviathan Props 1 - moving",
            "Battle Arena Leviathan Props 1 - moving Back",
            "Vortex portal",                                           // ← Everguild/FX/Vortex
            "Astra Militarum Ground Atlas",                             // ← Everguild/Misc/Unlit shadows receiver（9 块地面）
            "Battle Arena 1 Floor",
            "Battle Arena 2 Ground",
            "Battle Arena Leviathan Ground",
            "Black Legion Ground",
            "Dark Angels Ground",
            "Emperors Children Ground",
            "Genestealers Ground Flood Atlas",
            "Space Wolves Floor",
            "Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad",   // ← Hidden/VFX/Aeldari Dust/…
            "Hidden_VFX_Aeldari Portal_System (3)_Output ParticleStrip Unlit Quad",  // ← Hidden/VFX/Aeldari Portal/…
            "Hidden_VFX_Aeldari Portal_System (4)_Output Particle Unlit Quad",
            "Hidden_VFX_Aeldari Portal_System (5)_Output Particle Unlit Quad",
            "Hidden_VFX_Aeldari Portal_System_Output Particle Unlit Quad",
            "HiddenPass",                                              // ← Spine/Special/HiddenPass
        };

        /// <summary>与 <see cref="MaterialNames"/> 平行、等长：每个材质该绑的原版 shader 名。</summary>
        public static readonly string[] MaterialShaderNames =
        {
            "Everguild/FX/Alpha Mask One Layer  Color Ramp",
            "Everguild/FX/Alpha Mask One Layer  Color Ramp",
            "Everguild/FX/Burning",
            "Everguild/FX/Floor Planar Reflections Grainny",
            "Everguild/FX/Floor Planar Reflections Grainny",
            "Everguild/FX/Floor Planar Reflections Grainny",
            "Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask",
            "Everguild/FX/Simple Fake Water",
            "Everguild/FX/Specific/Necrons Rays",
            "Everguild/FX/Specific/Tau Generator Energy",
            "Everguild/FX/Specific/Tau Generator Energy",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Pulsating Mesh",
            "Everguild/FX/Tyranids/Tyranid Tentacle",
            "Everguild/FX/Tyranids/Tyranid Tentacle",
            "Everguild/FX/Tyranids/Tyranid Tentacle",
            "Everguild/FX/Tyranids/Tyranid Tentacle",
            "Everguild/FX/Vortex",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Everguild/Misc/Unlit shadows receiver",
            "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad",
            "Spine/Special/HiddenPass",
        };

        /// <summary>材质名 → 原版 shader 名。⚠️ **只做兜底**（材质名不全局唯一，见文件头）。</summary>
        public static bool TryGetOriginalShaderName(string materialName, out string shaderName)
        {
            shaderName = null;
            if (string.IsNullOrEmpty(materialName)) return false;
            for (int i = 0; i < MaterialNames.Length; i++)
            {
                if (string.Equals(MaterialNames[i], materialName, System.StringComparison.Ordinal))
                {
                    shaderName = MaterialShaderNames[i];
                    return !string.IsNullOrEmpty(shaderName);
                }
            }
            return false;
        }

        /// <summary>材质名是不是这张表里的一员（不解析 shader）。</summary>
        public static bool IsKnownMaterial(string materialName)
        {
            string ignored;
            return TryGetOriginalShaderName(materialName, out ignored);
        }

        /// <summary>表里的材质资产名（探针遍历用）。</summary>
        public static IList<string> KnownMaterialNames { get { return MaterialNames; } }
    }
}
