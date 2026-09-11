// ProbeMat.cs — 把一个原版材质的全部属性打出来（做自建 shader 前先看属性表）
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod ProbeMat.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class ProbeMat
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";

    // 要探的材质名（空数组 = 不按名字过滤）
    static readonly string[] MatNames = { };

    // 按 shader 名过滤（空 = 不过滤）
    static readonly string[] ShaderNames = { };   // 例：{ "Everguild/FX/Rays For Trail" }

    public static void Run()
    {
        var vfx = AssetBundle.LoadFromFile(Path.Combine(BundleDir, VfxBundleName));
        if (vfx == null) { Debug.LogError("包加载失败"); return; }

        var seen = new HashSet<Shader>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g == null) continue;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null) continue;
                    if (MatNames.Length > 0 && System.Array.IndexOf(MatNames, m.name) < 0) continue;
                    if (ShaderNames.Length > 0 && System.Array.IndexOf(ShaderNames, sh0name(m)) < 0) continue;
                    if (!seen.Add(m.shader)) continue;
                    Dump(m);
                }
        }
        Debug.Log("=== 材质探针 结束 ===");
    }

    static string sh0name(Material m) { return m.shader == null ? "" : m.shader.name; }

    static void Dump(Material m)
    {
        var sh = m.shader;
        Debug.Log($"### 材质「{m.name}」 shader=「{sh.name}」 queue={m.renderQueue} 属性数={sh.GetPropertyCount()}");
        for (int i = 0; i < sh.GetPropertyCount(); i++)
        {
            var pn = sh.GetPropertyName(i);
            var t = sh.GetPropertyType(i);
            string v;
            try
            {
                switch (t)
                {
                    case ShaderPropertyType.Color:  v = m.GetColor(pn).ToString("F4"); break;
                    case ShaderPropertyType.Vector: v = m.GetVector(pn).ToString("F4"); break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:  v = m.GetFloat(pn).ToString("F4"); break;
                    case ShaderPropertyType.Int:    v = m.GetInt(pn).ToString(); break;
                    case ShaderPropertyType.Texture:
                        var tx = m.GetTexture(pn);
                        v = tx == null ? "<null>" : $"「{tx.name}」 {tx.width}x{tx.height}";
                        break;
                    default: v = "?"; break;
                }
            }
            catch (Exception e) { v = "<" + e.GetType().Name + ">"; }
            Debug.Log($"    {pn}\t({t})\t= {v}");
        }
    }
}
