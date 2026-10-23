// ArenaOriginalShaderRebind.cs — 13 场原版 prefab 的「丢 shader 材质」运行时重绑器（2026-10-23 · 战场线 T00·P0-2）
//
// 一、要解决的问题（现读事实，判据 → `资料/普查产出_1023d/战场_T00引用修复_执行报告.md` §2）
//   13 颗原版 prefab 里有 **37 个材质**，它们的 `m_Shader` 指向**原版 shader 的 guid**
//   （例：9 块地面 = `Everguild/Misc/Unlit shadows receiver`，guid `2ef9073a…`），而那些 guid
//   **落不了工程资产** —— 本线坑 2：bundle 里的 shader 不能 `CreateAsset`，静态 import 之后
//   `m_Shader` 悬空 ⇒ 编辑器/运行时读到的是**空 shader ⇒ 品红**。
//   shader 本体**早就在随包 bundle 里**（18 行 guid / 17 个名字，逐包实读）⇒ 修法不是「再抽包」，
//   而是**加载时按原版名把 shader 绑回去**（`WarpforgeShaderMap.TryResolve` 的最后一步就是去
//   `WarpforgeShaderLoader` 的已加载包里按名取件）。
//
// 二、为什么是「克隆一份材质」而不是直接改 `renderer.sharedMaterial`
//   ① 直接改会把**工程内 `.mat` 资产**改成脏 —— 编辑器里一存盘就写进一个非法 shader 引用，
//      正是坑 2 的死法（`ArenaOriginalMaterial` 文件头记着同一条）；
//   ② 克隆只影响这一份 prefab 实例，多份实例各绑各的；
//   ③ 与 `ArenaOriginalMaterial.Rebuild()`（`new Material(sh)`）同一个思路。
//   材质上的**属性值**由 `new Material(source)` 原生整块拷贝 —— 原版材质的贴图/颜色/关键字都在
//   `m_SavedProperties` 里，**shader 缺失不影响它们被序列化下来**，换 shader 不会丢属性
//   （这正是「孤儿值」现象的同一件事：换 shader 后旧属性还在）。探针里有一条专门盯它
//   （9 块地面的 `_BaseMap` 必须非空），另外 `CopyTextureSlots` 再按名字补一遍贴图 + ST 兜底。
//
// 三、谁触发
//   13 颗 prefab 的**根**各挂一个本组件（`Awake` 跑）。这些 prefab 目前**没有消费方**，
//   将来谁实例化都会自动跑；也可以显式调静态入口 `RebindTree(go)`（**幂等**：已正确就跳过）。
//   ⚠️ **编辑器（非 Play）下 Unity 不调 `Awake`** ⇒ 探针里是**显式调 `Rebind()`**（与 `Awake` 同一个入口）。
//      「自动这一跳」只能在 Play / player 里验（日志里一行 `[Arena/Rebind]`）。
//
// 四、「按谁认领材质」的两条路（顺序固定，别颠倒）
//   ① **prefab 上挂的 `targets` + `shaderNames`（对象引用匹配）** —— 精确，不受同名影响；
//   ② **兜底**：`ArenaOriginalShaderIndex`（材质**名**）—— ⚠️ **只在材质当前 shader 是坏的时才查**，
//      因为 37 个材质里有 6 个在工程别处有同名 `.mat`（见那个文件头）。
//   两条都认不出、而 shader 又是坏的 ⇒ **`LogError` 出声**（⛔ 不许静默）。
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>
    /// 挂在一棵原版 prefab 的根上：`Awake` 时把「`m_Shader` 悬空」的材质换成
    /// 「同属性 + 原版 shader」的克隆材质。见文件头。
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaOriginalShaderRebind : MonoBehaviour
    {
        [Header("要重绑的材质（引用匹配，最可靠）")]
        [Tooltip("原版 prefab 里引用到的、m_Shader 悬空的材质资产。与 shaderNames 平行、等长。")]
        public Material[] targets;

        [Header("每个材质该绑的原版 shader 名")]
        [Tooltip("与 targets 平行、等长。名字必须是原版名（例：Everguild/Misc/Unlit shadows receiver），" +
                 "由 WarpforgeShaderMap.TryResolve 解析到 bundle 里的原件。")]
        public string[] shaderNames;

        [Tooltip("打一行汇总（成功/失败都会打；失败另有 LogError）。")]
        public bool logSummary = true;

        /// <summary>重绑结果计数。`Accounted` 应等于 `slots`。</summary>
        public struct Report
        {
            public int renderers;     // 树里的 Renderer 数
            public int slots;         // 走到的材质槽（含拖尾材质；非空材质才算）
            public int targetSlots;   // 其中「认得出该绑原版 shader」的槽（13 颗 prefab 合计 69）
            public int rebound;       // 这次换成克隆并绑上原版 shader 的槽
            public int alreadyOk;     // 本来就已经是对的、或无需处理的槽
            public int unresolved;    // 认得出该绑谁、但 shader 取不到
            public int unknown;       // shader 是坏的、但认不出该绑谁
            public int Accounted { get { return rebound + alreadyOk + unresolved + unknown; } }
            public override string ToString()
            {
                return "渲染器 " + renderers + " · 材质槽 " + slots + "（其中目标槽 " + targetSlots + "）"
                     + " · 重绑 " + rebound + " · 已正确 " + alreadyOk
                     + " · 取不到 " + unresolved + " · 认不出 " + unknown;
            }
        }

        /// <summary>只统计、不改任何东西 —— 探针的「重绑前/后」用它。</summary>
        public struct SurveyResult
        {
            public int renderers, slots, targetSlots, resolved, broken, unknown;
            public bool Clean { get { return broken == 0 && unknown == 0; } }
            public override string ToString()
            {
                return "渲染器 " + renderers + " · 材质槽 " + slots + "（其中目标槽 " + targetSlots + "）"
                     + " · 已解析 " + resolved + " · 仍是坏 shader " + broken + " · 认不出 " + unknown;
            }
        }

        void Awake() { Rebind(); }

        /// <summary>本组件克隆出来的材质（`OnDestroy` 时一起销毁 —— 否则每实例化一次就漏一批）。</summary>
        readonly List<Material> _created = new List<Material>();

        void OnDestroy()
        {
            foreach (var m in _created)
            {
                if (m == null) continue;
                // ⚠️ 编辑模式下 `Destroy` 会报「may not be called from edit mode」（探针里就是这么用的）
                if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
            }
            _created.Clear();
        }

        /// <summary>重绑自己这一棵树。可重复调用（幂等）。</summary>
        public Report Rebind()
        {
            return RebindTree(gameObject, targets, shaderNames, logSummary, name, _created);
        }

        /// <summary>重绑一棵树（不带 targets ⇒ 只走名字表兜底）。</summary>
        public static Report RebindTree(GameObject root)
        {
            return RebindTree(root, null, null, true, root != null ? root.name : "<null>");
        }

        /// <summary>重绑一棵树。见文件头「按谁认领材质」。</summary>
        /// <param name="owner">只用于日志（通常传 prefab / 实例名）。</param>
        /// <param name="created">可选：把克隆出来的材质记进这个表（`OnDestroy` 里好销毁）。</param>
        public static Report RebindTree(GameObject root, Material[] targets, string[] shaderNames,
                                        bool log, string owner, List<Material> created = null)
        {
            var rep = new Report();
            if (root == null)
            {
                Debug.LogError("[Arena/Rebind] ✗ root 是 null —— 什么都没做");
                return rep;
            }
            if (targets != null && shaderNames != null && targets.Length != shaderNames.Length)
            {
                Debug.LogWarning($"[Arena/Rebind] `{owner}`：targets({targets.Length}) 与 " +
                                 $"shaderNames({shaderNames.Length}) 长度不等 —— 只取前 " +
                                 $"{Mathf.Min(targets.Length, shaderNames.Length)} 对");
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            rep.renderers = renderers.Length;
            var cloneOf = new Dictionary<Material, Material>();   // 源材质 → 克隆（同一份材质只克隆一次）

            foreach (var r in renderers)
            {
                if (r == null) continue;

                var mats = r.sharedMaterials;
                if (mats != null && mats.Length > 0)
                {
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var dst = ResolveSlot(mats[i], targets, shaderNames, cloneOf, ref rep, owner, r.name);
                        if (!ReferenceEquals(dst, mats[i])) { mats[i] = dst; changed = true; }
                    }
                    if (changed) r.sharedMaterials = mats;
                }

                // `ParticleSystemRenderer.trailMaterial` 不在 sharedMaterials 里（原版拖尾就吃这条沟）
                var psr = r as ParticleSystemRenderer;
                if (psr != null && psr.trailMaterial != null)
                {
                    var dst = ResolveSlot(psr.trailMaterial, targets, shaderNames, cloneOf, ref rep,
                                          owner, r.name + ".trail");
                    if (!ReferenceEquals(dst, psr.trailMaterial)) psr.trailMaterial = dst;
                }
            }

            if (created != null)
                foreach (var kv in cloneOf) if (kv.Value != null) created.Add(kv.Value);

            if (log)
                Debug.Log($"[Arena/Rebind] `{owner}`：{rep}");

            return rep;
        }

        /// <summary>只数、不改：一棵树里「该绑原版 shader 的槽」现在是什么状态。</summary>
        public static SurveyResult Survey(GameObject root, Material[] targets, string[] shaderNames)
        {
            var s = new SurveyResult();
            if (root == null) return s;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            s.renderers = renderers.Length;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                if (mats != null)
                    for (int i = 0; i < mats.Length; i++) SurveySlot(mats[i], targets, shaderNames, ref s);
                var psr = r as ParticleSystemRenderer;
                if (psr != null && psr.trailMaterial != null)
                    SurveySlot(psr.trailMaterial, targets, shaderNames, ref s);
            }
            return s;
        }

        static void SurveySlot(Material m, Material[] targets, string[] shaderNames, ref SurveyResult s)
        {
            if (m == null) return;
            s.slots++;
            string expected;
            if (TryExpectedName(m, targets, shaderNames, out expected))
            {
                s.targetSlots++;
                if (!IsBroken(m.shader) && string.Equals(m.shader.name, expected, System.StringComparison.Ordinal))
                    s.resolved++;
                else
                    s.broken++;
            }
            else if (IsBroken(m.shader))
            {
                s.unknown++;
            }
        }

        /// <summary>一个材质槽该绑哪个原版名。顺序见文件头第四节。</summary>
        public static bool TryExpectedName(Material m, Material[] targets, string[] shaderNames,
                                           out string expected)
        {
            expected = null;
            if (m == null) return false;

            // ① prefab 上挂的 targets（**对象引用**匹配 —— 精确，不受「同名 .mat」影响）
            if (targets != null && shaderNames != null)
            {
                int n = Mathf.Min(targets.Length, shaderNames.Length);
                for (int i = 0; i < n; i++)
                {
                    if (!ReferenceEquals(targets[i], m)) continue;
                    if (string.IsNullOrEmpty(shaderNames[i])) return false;
                    expected = shaderNames[i];
                    return true;
                }
            }

            // ② 兜底：材质名。⚠️ **只在当前 shader 是坏的时候才查** ——
            //    37 个材质里有 6 个在工程别处有同名 `.mat`（判据 → `ArenaOriginalShaderIndex` 文件头），
            //    那些同名件的 shader 是好的，绝不能被这张表碰到。
            if (IsBroken(m.shader))
                return ArenaOriginalShaderIndex.TryGetOriginalShaderName(m.name, out expected);

            return false;
        }

        /// <summary>这个 shader 是不是「等于没有」。`Hidden/InternalErrorShader` = Unity 给悬空 shader 的替身。</summary>
        public static bool IsBroken(Shader shader)
        {
            if (shader == null) return true;
            var n = shader.name;
            if (string.IsNullOrEmpty(n)) return true;
            return n == "Hidden/InternalErrorShader"
                || n.IndexOf("InternalError", System.StringComparison.Ordinal) >= 0;
        }

        static Material ResolveSlot(Material m, Material[] targets, string[] shaderNames,
                                    Dictionary<Material, Material> cloneOf, ref Report rep,
                                    string owner, string rendererName)
        {
            if (m == null) return null;

            string expected;
            bool known = TryExpectedName(m, targets, shaderNames, out expected);
            rep.slots++;
            if (known) rep.targetSlots++;
            if (!known)
            {
                if (IsBroken(m.shader))
                {
                    rep.unknown++;
                    Debug.LogError($"[Arena/Rebind] ✗ `{owner}`：材质 `{m.name}`（渲染器 `{rendererName}`）" +
                                   "的 shader 是空的，而它既不在 prefab 的 targets 里、也不在 " +
                                   "ArenaOriginalShaderIndex 里 —— **认不出该绑谁**（保持原样，仍会是品红）");
                }
                else rep.alreadyOk++;
                return m;
            }

            if (!IsBroken(m.shader) && string.Equals(m.shader.name, expected, System.StringComparison.Ordinal))
            {
                rep.alreadyOk++;
                return m;                                    // 已经对了（幂等）
            }

            Material dst;
            if (cloneOf.TryGetValue(m, out dst) && dst != null)
            {
                rep.rebound++;
                return dst;                                  // 同一份源材质在本树里已克隆过 ⇒ 复用
            }

            Shader sh;
            string source;
            if (!WarpforgeShaderMap.TryResolve(expected, out sh, out source) || sh == null)
            {
                rep.unresolved++;
                Debug.LogError($"[Arena/Rebind] ✗ `{owner}`：取不到原版 shader `{expected}`" +
                               $"（材质 `{m.name}`，渲染器 `{rendererName}`）—— 保留原材质（**仍会是品红**）。" +
                               "先查 `StreamingAssets/WarpforgeVFX/wf_*.bundle` 是否齐全" +
                               "（`WarpforgeShaderLoader.EnsureLoaded` 会自己报包的情况）");
                return m;
            }

            // 克隆：`new Material(source)` 原生整块拷属性（含「源材质 shader 是 null」这种）。
            // 🔴 **不给它配「退路」**：曾想失败时退到 `new Material(shader)` + 手抄属性 —— 那条路在
            //    「源 shader 坏掉」时会**静默少抄**（`GetColor/GetTexture` 都按当前 shader 的属性表走），
            //    结果是一份「有 shader、没属性」的材质 = **看着不品红、其实全错**（比红更难查）。
            //    ⇒ 克隆失败就**出声**、保持原样（探针会红）。
            try { dst = new Material(m) { name = m.name }; }
            catch (System.Exception e)
            {
                rep.unresolved++;
                Debug.LogError($"[Arena/Rebind] ✗ `{owner}`：克隆材质 `{m.name}` 失败：{e.Message}" +
                               " —— 保留原材质（**仍会是品红**）");
                return m;
            }
            dst.shader = sh;
            CopyTextureSlots(m, dst);
            cloneOf[m] = dst;
            rep.rebound++;
            return dst;
        }

        /// <summary>兜底：按名字把源材质的贴图槽（含 Tiling/Offset）搬到克隆上。
        ///
        /// **为什么还要这一步**：`new Material(source)` 已经原生整块拷过一遍；这只是第二道保险 ——
        /// 若哪天 Unity 的构造函数对「shader 为 null 的源材质」少拷了贴图，这里能补上
        /// （读不到就跳过，不报错：源材质的 shader 是坏的时 `GetTexture` 可能一律返回 null，
        ///  那种情况下真正生效的是构造函数那一拷 —— 探针里 9 块地面的 `_BaseMap` 断言盯着结果）。</summary>
        static void CopyTextureSlots(Material src, Material dst)
        {
            if (src == null || dst == null) return;
            string[] names;
            try { names = src.GetTexturePropertyNames(); }
            catch { return; }
            if (names == null) return;
            foreach (var p in names)
            {
                if (string.IsNullOrEmpty(p) || !dst.HasProperty(p)) continue;
                try
                {
                    var t = src.GetTexture(p);
                    if (t == null) continue;
                    dst.SetTexture(p, t);
                    dst.SetTextureScale(p, src.GetTextureScale(p));
                    dst.SetTextureOffset(p, src.GetTextureOffset(p));
                }
                catch { /* 单个槽读不出来不致命 */ }
            }
        }
    }
}
