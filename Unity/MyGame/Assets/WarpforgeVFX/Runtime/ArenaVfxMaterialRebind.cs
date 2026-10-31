// ArenaVfxMaterialRebind.cs — 6 个 **VFX Graph 输出材质** 的「材质资产级」shader 重绑（B107_R4 · R4-04）
//
// 一、要解决的问题（判据 → `资料/完全复刻/判据/R4_VFX_Compute_Shader_定性.md` §七-1）
//   6 个 VFX 输出材质的 `m_Shader` 指向**原版 shader 的 guid**，而那个 guid 在工程 `*.meta` 里 0 命中
//   ⇒ 材质读到空 shader ⇒ 品红。它们是：
//     · `_shared/Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad.mat`
//     · `battlearenaaeldari/Hidden_VFX_Aeldari Portal_System_Output Particle Unlit Quad.mat`
//     · `battlearenaaeldari/Hidden_VFX_Aeldari Portal_System (3)_Output ParticleStrip Unlit Quad.mat`
//     · `battlearenaaeldari/Hidden_VFX_Aeldari Portal_System (4)_Output Particle Unlit Quad.mat`
//     · `battlearenaaeldari/Hidden_VFX_Aeldari Portal_System (5)_Output Particle Unlit Quad.mat`
//     · `_shared/Hidden_VFX_Gargoyle Swarm_System_Output Particle Unlit Quad.mat`（B107_R4 · G6 新拷）
//
//   这 6 个材质**不在任何 prefab 树里**（由 `VisualEffectAsset` 按 GUID 引用）
//   ⇒ 已有的 `ArenaOriginalShaderRebind`（遍历 `GetComponentsInChildren<Renderer>`）**一个都碰不到**，
//     只能走「材质资产级」这条新口。判据：`ArenaOriginalShaderIndex.cs` 文件头自己写着
//     「这 5 个由 VFX Graph 资产引用 ⇒ 13 颗 prefab 的树里一个都碰不到，只能按材质资产级验」。
//
// 二、绑到谁
//   `WarpforgeShaderMap.TryResolve(原版名)` —— 现在工程里已有 6 个同名 `.shader` 资产
//   （`WarpforgeVFX/Shaders/Hidden_VFX_*.shader`，B107_R4 · G8 建；名称与原版逐字相同）
//   ⇒ `TryResolve` 的「工程自带」那一跳会命中**工程 shader**（编辑器/真机都正常）；
//     若某个构建里那 6 个 shader 被剥离，`TryResolve` 会自动回落到**随包 bundle 里的原件**（仍可用）。
//   ⛔ 不新增第二套解析逻辑，全走 `WarpforgeShaderMap`（铁律：两处写同一条规则 = 迟早不一致）。
//
// 三、边界
//   · **只改内存里已加载的 `Material` 对象**（`m.shader = …`），**不写盘、不碰 `.mat` 资产**
//     —— 与 `ArenaOriginalShaderRebind` 的克隆路线同一条纪律（那边是克隆后赋、这边是资产实例上赋，
//       因为这些材质除了 VFX 资产之外没有别的持有者）。
//   · **幂等**：shader 已正确（非坏 & 名字已对）就跳过。
//   · **不静默**：认得出但取不到 ⇒ `LogError`。
//
// 四、谁触发
//   · `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` —— 进场后扫一遍**已加载**的材质（一次）。
//   · 也可由调用方显式 `Rebind(material)` / `RebindLoaded()`（编辑器探针、或将来 VFX 加载点）。
//   ⚠️ 进场那一刻还没加载的材质扫不到 —— 这是「运行期扫描」的固有限制；
//     若要「加载即绑」，需在 VFX 资产的加载点调 `Rebind(Material)`（见 `报告/B107_R4.md` §待办）。
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>6 个 VFX 输出材质的「材质资产级」重绑。见文件头。</summary>
    public static class ArenaVfxMaterialRebind
    {
        /// <summary>6 个材质资产名（不含 `.mat`）。与 <see cref="ShaderNames"/> 平行、等长。</summary>
        public static readonly string[] MaterialNames =
        {
            "Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad",
            "Hidden_VFX_Aeldari Portal_System_Output Particle Unlit Quad",
            "Hidden_VFX_Aeldari Portal_System (3)_Output ParticleStrip Unlit Quad",
            "Hidden_VFX_Aeldari Portal_System (4)_Output Particle Unlit Quad",
            "Hidden_VFX_Aeldari Portal_System (5)_Output Particle Unlit Quad",
            "Hidden_VFX_Gargoyle Swarm_System_Output Particle Unlit Quad",
        };

        /// <summary>每个材质该绑的原版 shader 名（这 6 个与材质名逐字相同）。与 <see cref="MaterialNames"/> 平行、等长。</summary>
        public static readonly string[] ShaderNames =
        {
            "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad",
            "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad",
            "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad",
        };

        /// <summary>材质名 → 原版 shader 名（只查这一张小表，⛔ 不碰 <see cref="ArenaOriginalShaderIndex"/> 的 37/18 口径）。</summary>
        /// <remarks>
        /// ⚠️ **两个名字口径都要认**（2026-10-31 B108_A14 现读 6/6 实测）：
        /// 这 6 个 `.mat` 的 YAML `m_Name` **不是文件名**，而是**原版 shader 名**本身
        /// （例：`m_Name: 'Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad'`，
        /// 而文件名是 `Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad.mat`）。
        /// 运行时 `Material.name` 取的就是 `m_Name` ⇒ **只按文件名比会 0 命中、整条兜底变哑巴**。
        /// </remarks>
        public static bool TryGetShaderName(string materialName, out string shaderName)
        {
            shaderName = null;
            if (string.IsNullOrEmpty(materialName)) return false;
            // ① 文件名口径（资产文件名的去扩展名形式）
            for (int i = 0; i < MaterialNames.Length; i++)
            {
                if (string.Equals(MaterialNames[i], materialName, System.StringComparison.Ordinal))
                {
                    shaderName = ShaderNames[i];
                    return true;
                }
            }
            // ② 运行时 `Material.name` 口径（= YAML `m_Name` = 原版 shader 名）
            for (int i = 0; i < ShaderNames.Length; i++)
            {
                if (string.Equals(ShaderNames[i], materialName, System.StringComparison.Ordinal))
                {
                    shaderName = ShaderNames[i];
                    return true;
                }
            }
            return false;
        }

        /// <summary>绑一个材质（幂等）。返回 true = 这次真的换过 shader。</summary>
        public static bool Rebind(Material m, bool log = true)
        {
            if (m == null) return false;
            string expected;
            if (!TryGetShaderName(m.name, out expected)) return false;

            if (!ArenaOriginalShaderRebind.IsBroken(m.shader)
                && string.Equals(m.shader.name, expected, System.StringComparison.Ordinal))
                return false;                                   // 已经对了

            Shader sh; string source;
            if (!WarpforgeShaderMap.TryResolve(expected, out sh, out source) || sh == null)
            {
                if (log)
                    Debug.LogError("[Arena/VfxRebind] ✗ 取不到 shader `" + expected + "`（材质 `" + m.name +
                                   "`）—— 保留原样（**仍会是品红**）");
                return false;
            }
            m.shader = sh;
            if (log)
                Debug.Log("[Arena/VfxRebind] ✓ 材质 `" + m.name + "` → `" + sh.name + "`（来源：" + source + "）");
            return true;
        }

        /// <summary>扫一遍**当前已加载**的材质，命中的就绑。返回真的换过的个数。</summary>
        public static int RebindLoaded(bool log = true)
        {
            int changed = 0, matched = 0;
            var all = Resources.FindObjectsOfTypeAll<Material>();
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var m = all[i];
                    if (m == null) continue;
                    string expected;
                    if (!TryGetShaderName(m.name, out expected)) continue;
                    matched++;
                    if (Rebind(m, log)) changed++;
                }
            }
            if (log)
                Debug.Log("[Arena/VfxRebind] 已加载材质里命中 " + matched + " / " + MaterialNames.Length +
                          " 个，本次换 shader " + changed + " 个");
            return changed;
        }

        /// <summary>进场后扫一次。⚠️ 只能看到**此时已加载**的材质，见文件头「四、谁触发」。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRebindOnLoad()
        {
            RebindLoaded(false);
        }
    }
}
