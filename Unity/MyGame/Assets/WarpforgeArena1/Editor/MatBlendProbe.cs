// MatBlendProbe.cs — 一次性探针：**bundle 里的原版材质，属性表到底读不读得到**（2026-10-01）
//
// 起因：`EffectExporter.ApplyRenderState` 对一个材质做了「照搬混合」还是「按 shader 名兜底」的判断，
// 而导出的 `Cruelty Particle Custom.mat` 落在**不透明**（`_SrcBlend=1,_DstBlend=0,_ZWrite=1`），
// 原版那份却是 **5/10/0**（`Material_-935563986013433035.json` 实读）。
// 那条分支唯一的开关是 `src.HasProperty("_SrcBlend")` —— 本探针把这件事**量出来**，不再猜。
//
// 跑：`unset ELECTRON_RUN_AS_NODE && Unity -batchmode -quit -projectPath D:\4\Unity\MyGame
//      -executeMethod MatBlendProbe.Run -logFile -`
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MatBlendProbe
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string BundleName = "battleprefabs_vfxandmisc_assets_all.bundle";

    // 要探的材质名（原版那份的 m_Name）
    static readonly string[] Names = { "Spirit Stone Explosion", "Cruelty Particle Custom", "Spirit Stone", "Glimmer VFX" };

    public static void Run()
    {
        var path = Path.Combine(BundleDir, BundleName);
        if (!File.Exists(path)) { Debug.LogError($"[Probe] 找不到 bundle：{path}"); return; }
        var b = AssetBundle.LoadFromFile(path);
        if (b == null) { Debug.LogError("[Probe] bundle 载入失败"); return; }

        // ⚠️ 材质在包里**不是 addressable**（`LoadAllAssets<Material>()` = 0、按名字也不通）
        //    ⇒ 唯一稳的取法是**通过引用它的 prefab**：加载 GameObject → 读渲染器的 `sharedMaterial`
        //    （这正是 `EffectExporter` 的路子）。
        var mats = new Dictionary<string, Material>();
        int loadedGo = 0;
        foreach (var key in b.GetAllAssetNames())
        {
            GameObject go = null;
            try { go = b.LoadAsset<GameObject>(key); } catch { }
            if (go == null) continue;
            loadedGo++;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;
                mats[r.sharedMaterial.name] = r.sharedMaterial;
            }
        }
        Debug.Log($"[Probe] 载入 GameObject {loadedGo} 个 · 从中取到材质 {mats.Count} 个");

        foreach (var name in Names)
        {
            Material src;
            if (!mats.TryGetValue(name, out src) || src == null) { Debug.Log($"[Probe] 包里没有材质 {name}"); continue; }
            var sh = src.shader;
            string shName = sh != null ? sh.name : "(null)";
            Debug.Log($"[Probe] === {name}");
            Debug.Log($"[Probe]   shader = {shName}"
                    + $" · isSupported={(sh == null ? false : sh.isSupported)}");
            if (sh != null)
            {
                int n = sh.GetPropertyCount();
                Debug.Log($"[Probe]   shader.GetPropertyCount() = {n}");
                var names = new List<string>();
                for (int i = 0; i < Math.Min(n, 40); i++) names.Add(sh.GetPropertyName(i));
                Debug.Log($"[Probe]   前 40 个属性名：{string.Join(" ", names.ToArray())}");
            }
            foreach (var p in new[] { "_SrcBlend", "_DstBlend", "_ZWrite", "_Surface", "_Blend", "_Cull",
                                      "_Color", "_MainTex", "_SOFTPARTICLES" })
            {
                bool has = src.HasProperty(p);
                string v = "-";
                try
                {
                    if (has && sh != null)
                    {
                        int idx = sh.FindPropertyIndex(p);
                        var t = idx >= 0 ? sh.GetPropertyType(idx) : UnityEngine.Rendering.ShaderPropertyType.Float;
                        v = t == UnityEngine.Rendering.ShaderPropertyType.Texture
                          ? (src.GetTexture(p) != null ? src.GetTexture(p).name : "null")
                          : (t == UnityEngine.Rendering.ShaderPropertyType.Color ? src.GetColor(p).ToString() : src.GetFloat(p).ToString());
                    }
                }
                catch (Exception e) { v = "读值异常:" + e.GetType().Name; }
                Debug.Log($"[Probe]   HasProperty({p}) = {has} · 值 = {v}");
            }
        }
        b.Unload(true);
    }
}
