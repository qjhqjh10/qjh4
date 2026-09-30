// ArenaParticleKeywords.cs — 把原版粒子材质的**关键字**在【运行时】补上。
//
// 为什么要有它（2026-09-24，判据全文 → `资料/已知的坑.md`
// 「Unity 在进程里存不住粒子材质的 `_EMISSION` 关键字」）：
//   原版 `SmokeySteam01` 挂着 `_EMISSION` + `_EmissionColor = (0.4811,…,1)`，而 URP
//   `ShaderLibrary/Unlit.hlsl:22` 是 `finalColor = half4(albedo + surfaceData.emission, alpha)`
//   ⇒ **每通道加 0.4811**；少了它，arena1 那团粒子渲成**深灰**（固定种子实测 89.5 vs 原版 138.4）。
//   但**这个关键字写不进 `.mat`**：`EnableKeyword` / `SetKeyword(LocalKeyword)` / `shaderKeywords=`
//   三条 API 内存里都成、存盘就丢；写 `.mat` 文本再导入会被覆盖回 `[]`。
//   ⇒ 唯一的活路是**在运行时给材质【实例】设**（实例不落盘，也就没有「存不住」这回事）。
//
// 做法与网格那套（`ArenaOriginalMaterial`）完全同构：**新建一份材质实例、挂回渲染器**，
// 不碰盘上的资产。⚠️ `Awake` 在**批量预览**（`OpenScene` 编辑态）里不跑
// ⇒ 预览/量测那条路由 `ArenaBuilder.PrepareSceneMeasure` 显式调 `ApplyAllInScene()`。
using UnityEngine;

namespace WarpforgeVFX
{
    [DisallowMultipleComponent]
    public class ArenaParticleKeywords : MonoBehaviour
    {
        [Tooltip("要启用的 shader 关键字（照原版材质的 m_ValidKeywords 原样带过来）。")]
        public string[] keywords;

        /// <summary>🆕 2026-09-30：`true` = **并集**（`EnableKeyword`，**一个已有的都不删**）；
        /// `false` = **整表替换**（`shaderKeywords = keywords`，照原版）。
        ///
        /// 🔴 **网格必须用并集**（`ArenaBuilder` 挂组件时设 `true`）—— 有实测：
        ///   整表替换之后 `sororitas` **1.028 → 1.060**（`hi%` 0.07 → 0.42 = 爆亮）、`arena3` 1.012 → 1.023。
        ///   机理：**原版有些 shader 不靠关键字表达透明**（`m_ValidKeywords` 里没有 `_SURFACE_TYPE_TRANSPARENT`），
        ///   而**我们这一侧真正在跑的 shader 靠它**（`ArenaOriginalMaterial.ApplyRenderState` 按 `_Surface` 算出来设的）
        ///   ⇒ 整表替换把它**删掉**了 ⇒ 透明件按不透明画 ⇒ 过曝。
        ///   并集同时拿到我们要的那几个（`_SOFT` / `_USEDISTORT` …）且不丢自己的。
        /// ⚠️ 粒子那条路**保持整表替换**（`false`）：它已经过闸门①验收，且粒子的兜底/自建 shader
        ///   与原版同名关键字本来就一致（实测无差）—— **别顺手把粒子也改成并集**。
        public bool additive;

        /// <summary>实例材质的名字后缀 —— 用来判「已经实例化过了」，避免每次调用都堆一份。</summary>
        public const string InstanceSuffix = "_kw";

        void Awake() { Apply(); }

        void SetOn(Material m)
        {
            if (additive)
            {
                if (keywords == null) return;
                foreach (var k in keywords) if (!string.IsNullOrEmpty(k)) m.EnableKeyword(k);
            }
            else
            {
                // ⚠️ 数组口是**整体替换**：原版没开的关键字自然就是关（正是要的「照原版」）。
                m.shaderKeywords = keywords;
            }
        }

        public void Apply()
        {
            // 🆕 2026-09-30：**网格也走这一条**（`MeshRenderer` / `SkinnedMeshRenderer`）。
            //   原来这里是 `GetComponent<ParticleSystemRenderer>()` + `[RequireComponent(…)]`
            //   ⇒ 给网格挂这个组件会**自动多出一个 ParticleSystemRenderer**（RequireComponent 的副作用）。
            //   改成 `Renderer` 之后一个组件同时覆盖"粒子的关键字"与"网格的关键字"
            //   （两条路的判据同一处 —— 反例见 `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
            //   网格那一侧的判据 → `gen_arena_meshkeywords.py` 与 `ArenaBuilder.LoadMeshKeywords`。
            var pr = GetComponent<Renderer>();
            if (pr == null || keywords == null || keywords.Length == 0) return;
            var src = pr.sharedMaterial;
            if (src == null || src.shader == null) return;

            // 已经是我们的实例 ⇒ 只刷新关键字（不重复 new）
            if (src.name.EndsWith(InstanceSuffix)) { SetOn(src); return; }
            // 🔴🔴 **2026-09-30 实测的结论：能不换实例就别换。**
            //   `ArenaOriginalMaterial.Rebuild()` 重建出来的材质名字带 `_orig`，**每个渲染器一份**
            //   （它每次 `new Material(sh)`，不缓存）⇒ 对它们**就地设关键字**即可，没有污染共享资产的风险。
            //   而**换一份新实例**会把画面改掉（实测 `sororitas` 1.026 → **1.060**，`hi%` 0.07→0.42，
            //   爆亮的是 32 个烛焰）：**拷贝前后属性逐项完全相同、关键字语义无关**
            //   （换成 `_ZZZ_CONTROL_NONEXISTENT` 一样亮）、`renderQueue`/贴图/实例化开关也都相同
            //   ⇒ 差异只能出在**渲染器那一侧**（换实例会改掉「同队列同深度时按材质实例 ID 兜底排序」
            //   的那一档 → 透明件的绘制顺序变了；烛焰与烛身本来就是叠着的）。
            //   ⇒ **判据：材质名以 `_orig` 结尾（= 我们自己的运行时实例）就地改；否则才拷贝一份。**
            bool ours = src.name.EndsWith("_orig");
            if (ours) { SetOn(src); return; }
            var m = new Material(src) { name = src.name + InstanceSuffix };
            // ⚠️ 这条是**防御性**的，不是「已坐实的 bug」：拷完再把源材质已有的关键字补回来，
            //   保证「并集」语义在任何情况下都成立。**没单独测过「不补会怎样」**
            //   （2026-09-30 那次探针是在已经补上的版本上跑的，分辨不出来）。
            foreach (var k in src.shaderKeywords) if (!string.IsNullOrEmpty(k)) m.EnableKeyword(k);
            SetOn(m);
            // 🔎 诊断（`WF_KWPROBE=1`）：**只读**，用来判「拷贝这一步到底丢了什么」——
            //   2026-09-30 实测：只要挂上这个组件，`sororitas` 就从 1.026 变 **1.060**（画面下部整片变亮），
            //   而把关键字换成**不存在的**也一样 ⇒ 问题不在关键字，在**拷贝这个动作**。
            if (System.Environment.GetEnvironmentVariable("WF_KWPROBE") == "1")
            {
                var t0 = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
                var t1 = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Debug.Log($"[KWPROBE] {name} shader={src.shader.name}→{m.shader.name}"
                        + $" rq={src.renderQueue}→{m.renderQueue}"
                        + $" kw={src.shaderKeywords.Length}→{m.shaderKeywords.Length}"
                        + $" tex={(t0 != null ? t0.name : "-")}→{(t1 != null ? t1.name : "-")}"
                        + $" inst={src.enableInstancing}→{m.enableInstancing}");
                // 🔎 逐属性比（**只读诊断**）：2026-09-30 靠它证明了「拷贝前后属性逐项相同」
                //   ⇒ 变亮不是材质内容的问题。留着给下次同类问题用。
                {
                    string C(Material mm, string k)
                    {
                        if (mm == null || !mm.HasProperty(k)) return "无";
                        var v = mm.GetVector(k);
                        return $"({v.x:F3},{v.y:F3},{v.z:F3},{v.w:F3})";
                    }
                    string F(Material mm, string k)
                        => (mm != null && mm.HasProperty(k)) ? mm.GetFloat(k).ToString("F3") : "无";
                    string[] cs = { "_Color", "_BaseColor", "_EmissionColor" };
                    string[] fs = { "_Surface", "_ZWrite", "_Cull", "_SrcBlend", "_DstBlend", "_AlphaClip", "_Cutoff",
                                    "_FlickerMinMaxRange", "_AlphaMultiplier", "_QueueOffset" };
                    foreach (var k in cs)
                        if (C(src, k) != C(m, k)) Debug.Log($"[KWPROBE-DIFF] {name} {k} {C(src, k)} → {C(m, k)}");
                    foreach (var k in fs)
                        if (F(src, k) != F(m, k)) Debug.Log($"[KWPROBE-DIFF] {name} {k} {F(src, k)} → {F(m, k)}");
                }
            }
            pr.sharedMaterial = m;
        }

        /// <summary>把当前场景里所有这类组件刷一遍（**批量预览/量测那条路必须显式调** —— `Awake` 不跑）。
        /// 返回实际处理的组件数。</summary>
        public static int ApplyAllInScene()
        {
            var all = Object.FindObjectsByType<ArenaParticleKeywords>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int n = 0;
            for (int i = 0; i < all.Length; i++)
            {
                all[i].Apply();
                n++;
            }
            return n;
        }
    }
}
