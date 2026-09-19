// RQProbe.cs — 一次性诊断：原版材质的 `renderQueue` 在**导出环境下**读到什么、
// 以及**哪个判据**能可靠区分「工程自带的 shader」与「只在 bundle 里的 shader」。
//
// 为什么要它（2026-09-19 晚）：`EffectExporter` 里那条
//   `renderQueue = (om.shader != null && AssetDatabase.Contains(om.shader)) ? om.renderQueue : -1`
// **重导全量之后 prefab 里记的还是 2000** ⇒ 说明 `AssetDatabase.Contains` 判不出来。
// 而一次重导要 ~30 分钟 ⇒ **必须先在这儿把判据定死**，不能盲试。
//
// 走的是**和导出器同一条取数路径**（bundle → GameObject → renderer → sharedMaterial），
// 因为材质靠跨包依赖解析，`LoadAllAssets<Material>()` 拿不到（试过，空）。
//
// 用法：-executeMethod RQProbe.Run -logFile -   → 筛 `grep "^RQ "`
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RQProbe
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";

    // 两条原版 shader（只在 bundle 里）+ 一条工程自带（`URP/Particles/Unlit`）当对照
    static readonly string[] WantShaders = {
        "Shader Graphs/Fx_ParticleDissolve_apb", "Shader Graphs/Fx_RockDissolve",
        "Universal Render Pipeline/Particles/Unlit" };

    public static void Run()
    {
        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError("bundle 没加载"); return; }

        var seen = new System.Collections.Generic.HashSet<string>();
        // 每个 shader 名**只打 1 条**（否则 `URP/Particles/Unlit` 那一大族会把名额占满，
        // 那两个只在 bundle 里的 shader 一条都打不出来）。
        var seenShader = new System.Collections.Generic.HashSet<string>();
        int hit = 0;
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g == null) continue;

            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null) continue;
                    var shName = m.shader.name;
                    bool want = false;
                    foreach (var w in WantShaders) if (shName == w) want = true;
                    if (!want || !seen.Add(m.name + "|" + shName)) continue;
                    if (!seenShader.Add(shName)) continue;   // 每个 shader 名只要一条
                    hit++;

                    var sh = m.shader;
                    Debug.Log($"RQ mat={m.name}"
                            + $" | shader={shName}"
                            + $" | mat.renderQueue={m.renderQueue}"
                            + $" | shader.renderQueue={sh.renderQueue}"
                            + $" | Contains={AssetDatabase.Contains(sh)}"
                            + $" | IsForeignAsset={AssetDatabase.IsForeignAsset(sh)}"
                            + $" | IsMainAsset={AssetDatabase.IsMainAsset(sh)}"
                            + $" | IsNativeAsset={AssetDatabase.IsNativeAsset(sh)}"
                            + $" | AssetPath='{AssetDatabase.GetAssetPath(sh)}'"
                            + $" | FindSame={(Shader.Find(shName) == sh)}");
                }
            }
            if (hit >= 6) break;
        }
        Debug.Log($"RQ 结束（{hit} 条）");
    }
}
