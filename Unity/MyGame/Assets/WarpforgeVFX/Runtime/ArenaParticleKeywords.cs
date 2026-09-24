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
    [RequireComponent(typeof(ParticleSystemRenderer))]
    public class ArenaParticleKeywords : MonoBehaviour
    {
        [Tooltip("要启用的 shader 关键字（照原版材质的 m_ValidKeywords 原样带过来）。")]
        public string[] keywords;

        /// <summary>实例材质的名字后缀 —— 用来判「已经实例化过了」，避免每次调用都堆一份。</summary>
        public const string InstanceSuffix = "_kw";

        void Awake() { Apply(); }

        public void Apply()
        {
            var pr = GetComponent<ParticleSystemRenderer>();
            if (pr == null || keywords == null || keywords.Length == 0) return;
            var src = pr.sharedMaterial;
            if (src == null || src.shader == null) return;

            // 已经是我们的实例 ⇒ 只刷新关键字（不重复 new）
            if (src.name.EndsWith(InstanceSuffix))
            {
                src.shaderKeywords = keywords;
                return;
            }
            var m = new Material(src) { name = src.name + InstanceSuffix };
            // ⚠️ 数组口是**整体替换**：原版没开的关键字自然就是关（正是要的「照原版」）。
            m.shaderKeywords = keywords;
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
