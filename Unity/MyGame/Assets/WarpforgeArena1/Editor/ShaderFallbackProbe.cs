// ShaderFallbackProbe.cs — 「掉兜底的那 28 个 shader」到底解析到谁、能不能渲（2026-09-18）
//
// **为什么要它**：`资料/普查产出_0917/补充shader引用清单.md:76-77` 写着
//   「『掉原版 bundle 兜底』的 28 个 = **能渲染，但不安全**：bundle 里的 shader
//    **在编辑器下渲染会出故障**」
// 但那条「编辑器下会出故障」的实测出处（`WarpforgeShaderMap.cs:150-158`）讲的是
// **`URP Particles/Unlit`** 这个**工程自带的 URP 内建 shader** 从 bundle 里取到的那一份 ——
// 与「Everguild 自定义 shader 从 bundle 里取」**不是同一件事**（后者工程里根本没有同名对照物）。
// ⇒ 这是一个**从单例推广到全体的推断**，没有逐条验过。本探针就是去逐条验。
//
// **怎么判**（三条一起看，缺一条都不足以下结论）：
//   1. `TryResolve` 走的哪条路（自建 / 工程自带 / 原版bundle / 解析不到）
//   2. `shader.isSupported` —— 当前渲染管线里有没有可用变体（false = 必然渲成故障色）
//   3. **真渲一张**：用这个 shader 建材质，`Graphics.Blit` 到一个已知底色的 RT，分类像素
//      （洋红占比 / 与底色不同的像素占比 / 平均色）。洋红是 Unity 的「shader 坏了」色。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod ShaderFallbackProbe.Run -logFile "d:/4/_tmp_view/shaderfallback.log"
//   筛输出：grep "^SFP "
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class ShaderFallbackProbe
{
    const string P = "SFP ";

    /// <summary>那 28 个「未映射 → 掉原版 bundle 兜底」的 shader 名。
    /// **出处**：`资料/普查产出_0917/补充shader引用清单.md` 的清单表里，
    /// 「我们现在的处置」列写着「未映射 → 掉原版 bundle 兜底」的那 28 行（逐条抄下）。</summary>
    static readonly string[] Names =
    {
        "Everguild/FX/Rays For Trail",
        "Spine/Special/HiddenPass",
        "Everguild/FX/ShieldVfx",
        "Everguild/FX/Spiral Trail FX",
        "Everguild/FX/Glow Shader",
        "Everguild/Cards/Gem Crystal Glitter",
        "Everguild/FX/Burning",
        "Everguild/FX/FX Shine For Animation",
        "Everguild/FX/Burning Dissolve",
        "Everguild/FX/Specific/Necrons Rays",
        "Everguild/Matcap/Matcap Full Options VAT",
        "Everguild/Cards/3D Card Explosion",
        "Everguild/Cards/BlobShadow",
        "Everguild/FX/MarkerLight",
        "Everguild/Wind Matcap",
        "GlassRefraction",
        "Custom/EditorIcon",
        "Everguild/Cards/3D Card",
        "Everguild/Cards/Card Swarm Effect",
        "Everguild/Cards/Gem Crystal Glitter Explosion",
        "Everguild/Cards/Necrons Base Death",
        "Everguild/Cards/Shatter Inner Pieces",
        "Everguild/FX/Card Highlight And Shadow",
        "Everguild/FX/Card Remnant Death Icon",
        "Everguild/FX/Halo UV scroll",
        "Everguild/FX/Particle Dissolve Premultiply",
        "Everguild/FX/Specific/Pray Glow",
        "Everguild/FX/Vortex",
    };

    public static void Run()
    {
        var sb = new StringBuilder();
        int byBundle = 0, notResolved = 0, unsupported = 0, magenta = 0;
        var mag = new List<string>();

        foreach (var n in Names)
        {
            Shader sh; string src;
            bool ok = WarpforgeShaderMap.TryResolve(n, out sh, out src);
            if (!ok || sh == null)
            {
                notResolved++;
                sb.AppendLine("✗ " + n + "  → **解析不到**");
                continue;
            }
            if (src == "原版bundle") byBundle++;
            bool supported = sh.isSupported;
            if (!supported) unsupported++;

            var st = Render(sh, out float magentaRatio, out float diffRatio, out Color mean);
            if (magentaRatio > 0.5f) { magenta++; mag.Add(n); }

            sb.AppendLine(string.Format(
                "{0} {1}\n      → {2}（{3}） isSupported={4} pass={5} | 渲染 {6} 洋红{7:P0} 非底色{8:P0} 均色({9:F2},{10:F2},{11:F2},{12:F2})",
                ok ? "✓" : "✗", n, sh.name, src, supported, sh.passCount,
                st, magentaRatio, diffRatio, mean.r, mean.g, mean.b, mean.a));
        }

        Debug.Log(P + "=== 共 " + Names.Length + " 个 ===");
        Debug.Log(P + "走原版 bundle 的 " + byBundle + " 个 · 解析不到 " + notResolved +
                  " · isSupported=false 的 " + unsupported + " · **渲出来是洋红的 " + magenta + " 个**");
        if (mag.Count > 0) Debug.Log(P + "洋红名单：" + string.Join(" / ", mag.ToArray()));
        foreach (var line in sb.ToString().Split('\n')) if (line.Length > 0) Debug.Log(P + line);

        // 判据：全部解析得到，且没有一个渲成洋红。isSupported=false 单独报（不直接判失败，
        // 因为粒子类 shader 的变体是按需编译的，batch 下可能尚未编译）。
        bool pass = (notResolved == 0 && magenta == 0);
        Debug.Log(P + (pass ? "=== 通过 ===" : "=== 不通过 ==="));
        if (Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
    }

    /// <summary>真渲一张：把 shader 建材质，Blit 一张白图到已知底色的 RT，回读像素分类。</summary>
    static string Render(Shader sh, out float magentaRatio, out float diffRatio, out Color mean)
    {
        magentaRatio = 0f; diffRatio = 0f; mean = Color.clear;
        const int N = 64;
        RenderTexture rt = null;
        Material mat = null;
        Texture2D readback = null;
        try
        {
            mat = new Material(sh);
            // 尽量把常见的贴图口喂上一张不透明的图，否则「空贴图 → 全黑」会伪装成「坏了」
            var tex = Texture2D.whiteTexture;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", Color.white);

            rt = RenderTexture.GetTemporary(N, N, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0.25f, 0.25f, 0.25f, 1f));   // 已知底色，便于分辨「什么都没画」
            GL.PushMatrix(); GL.LoadOrtho();
            Graphics.Blit(tex, rt, mat);
            GL.PopMatrix();

            readback = new Texture2D(N, N, TextureFormat.RGBA32, false);
            readback.ReadPixels(new Rect(0, 0, N, N), 0, 0);
            readback.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt); rt = null;

            var px = readback.GetPixels32();
            int mg = 0, df = 0; double r = 0, g = 0, b = 0, a = 0;
            foreach (var c in px)
            {
                r += c.r / 255.0; g += c.g / 255.0; b += c.b / 255.0; a += c.a / 255.0;
                // Unity 的「shader 坏了」洋红 ≈ (1, 0, 1)
                if (c.r > 200 && c.g < 60 && c.b > 200) mg++;
                if (Mathf.Abs(c.r / 255f - 0.25f) > 0.08f || Mathf.Abs(c.g / 255f - 0.25f) > 0.08f
                    || Mathf.Abs(c.b / 255f - 0.25f) > 0.08f) df++;
            }
            int n = px.Length;
            magentaRatio = mg / (float)n; diffRatio = df / (float)n;
            mean = new Color((float)(r / n), (float)(g / n), (float)(b / n), (float)(a / n));
            return "成功";
        }
        catch (System.Exception e)
        {
            return "抛异常[" + e.GetType().Name + "]";
        }
        finally
        {
            if (mat != null) Object.DestroyImmediate(mat);
            if (readback != null) Object.DestroyImmediate(readback);
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
        }
    }
}
