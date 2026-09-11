// TexCompare.cs — 把原版 bundle 贴图与导出 PNG 逐像素比一遍，定位「导出后变暗/变色」
//
// 只读工程，不改任何东西。用法：
//   Unity.exe -batchmode -quit -projectPath ... -executeMethod TexCompare.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class TexCompare
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string TexDir = "Assets/WarpforgeVFX/Textures";

    static readonly string[] Names =
    {
        "Glow", "LightningTrail", "Lightning_Burst_Random",
    };

    public static void Run()
    {
        Debug.Log("=== 贴图比对 开始 ===");
        // 同一个 bundle 重复 LoadFromFile 会返回 null（Unity 限制），所以加载一次后缓存复用
        var bundles = new Dictionary<string, AssetBundle>();
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null) bundles[Path.GetFileName(f)] = b;
        }
        Debug.Log($"加载 bundle {bundles.Count} 个");

        // 贴图是随材质/预制体被引用进来的，光加载 bundle 拿不到。
        // 只遍历 VFX 包（候选贴图都在这里），全 84 个包太慢
        var all = new List<Texture2D>();
        var vb0 = bundles.TryGetValue("battleprefabs_vfxandmisc_assets_all.bundle", out var vb1) ? vb1 : null;
        if (vb0 != null)
        {
            var names0 = vb0.GetAllAssetNames();
            Debug.Log($"VFX 包资产 {names0.Length} 个");
            foreach (var n in names0)
            {
                GameObject g = null;
                try { g = vb0.LoadAsset<GameObject>(n); } catch { }
                if (g == null) continue;
                foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || m.shader == null) continue;
                        foreach (var pn in m.GetTexturePropertyNames())
                        {
                            var t = m.GetTexture(pn) as Texture2D;
                            if (t != null && !all.Contains(t)) all.Add(t);
                        }
                    }
                }
            }
        }
        Debug.Log($"已收集贴图 {all.Count} 个");

        foreach (var name in Names)
        {
            var src = all.FirstOrDefault(t => t.name == name);
            if (src == null) { Debug.LogWarning($"bundle 里找不到贴图 {name}"); continue; }
            Compare(src, $"{TexDir}/{name}.png");
        }
        Debug.Log("=== 贴图比对 结束 ===");
    }

    static void Compare(Texture2D src, string pngPath)
    {
        var imp = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        var dst = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
        Debug.Log($"--- 「{src.name}」 bundle {src.width}x{src.height} fmt={src.graphicsFormat} " +
                  $"sRGB={(src.isDataSRGB ? "是" : "否")} mip={src.mipmapCount} readable={src.isReadable}");
        if (dst == null) { Debug.LogWarning($"   导出 PNG 不存在或读不到: {pngPath}"); return; }
        Debug.Log($"    PNG {dst.width}x{dst.height} fmt={dst.graphicsFormat} sRGB={(dst.isDataSRGB ? "是" : "否")} mip={dst.mipmapCount}");
        if (imp != null)
            Debug.Log($"    Importer: sRGB={imp.sRGBTexture} alphaIsTransparency={imp.alphaIsTransparency} " +
                      $"type={imp.textureType} mipmap={imp.mipmapEnabled} wrap={imp.wrapMode} " +
                      $"compression={imp.textureCompression} maxSize={imp.maxTextureSize} readable={imp.isReadable}");

        var a = Read(src);
        var b = Read(dst);
        if (a == null || b == null) { Debug.LogWarning("   有一边读不出像素"); return; }
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
        { Debug.LogWarning($"   尺寸不一致 {a.GetLength(0)}x{a.GetLength(1)} vs {b.GetLength(0)}x{b.GetLength(1)}"); return; }

        double dr = 0, dg = 0, db = 0, da = 0;
        double sr = 0, sg = 0, sb = 0, sa = 0;
        int n = a.GetLength(0) * a.GetLength(1);
        for (int y = 0; y < a.GetLength(1); y++)
            for (int x = 0; x < a.GetLength(0); x++)
            {
                dr += Math.Abs(a[x, y].r - b[x, y].r); dg += Math.Abs(a[x, y].g - b[x, y].g);
                db += Math.Abs(a[x, y].b - b[x, y].b); da += Math.Abs(a[x, y].a - b[x, y].a);
                sr += b[x, y].r / Math.Max(1e-6f, a[x, y].r);
                sg += b[x, y].g / Math.Max(1e-6f, a[x, y].g);
                sb += b[x, y].b / Math.Max(1e-6f, a[x, y].b);
                sa += b[x, y].a / Math.Max(1e-6f, a[x, y].a);
            }
        Debug.Log($"    平均绝对差: R={dr / n:F4} G={dg / n:F4} B={db / n:F4} A={da / n:F4}   (0=完全一致)");
        Debug.Log($"    平均比值(导/原): R={sr / n:F4} G={sg / n:F4} B={sb / n:F4} A={sa / n:F4}   (1=一致)");
    }

    static Color[,] Read(Texture2D t)
    {
        try
        {
            if (t.isReadable) return t.GetPixels().To2D(t.width, t.height);
            var rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32,
                                                RenderTextureReadWrite.sRGB);
            Graphics.Blit(t, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tmp = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
            tmp.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
            tmp.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var px = tmp.GetPixels().To2D(t.width, t.height);
            UnityEngine.Object.DestroyImmediate(tmp);
            return px;
        }
        catch (Exception e) { Debug.LogWarning($"   读像素失败: {e.Message}"); return null; }
    }
}

static class TexCompareExt
{
    public static Color[,] To2D(this Color[] flat, int w, int h)
    {
        var r = new Color[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                r[x, y] = flat[y * w + x];
        return r;
    }
}
