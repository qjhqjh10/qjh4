// B109_E3_SdfFormatProbe.cs — 「卡框 SDF 改了 `.meta` 压缩档之后，**导入后的真实 `Texture2D.format`** 到底是什么」的读数探针。
//
// ============================ 为什么要有这个探针 ============================
// `B108_A17`（2026-10-31）把 `Resources/Art/card_sdf/` 的 105 份 `.meta` 的
// `textureCompression` 改成了 `0`（Uncompressed），判据 = 原版 13 阵营 104 张 SDF 全是
// `m_TextureFormat = 4`(RGBA32)；`ArtBaker.ApplyImportSettings` 也补了 `isSdf` 分支并跑过 rc=0。
//
// 🔴 **但「`.meta` 写了什么」≠「导入后引擎里是什么」**：`.meta` 只决定**下次导入**的参数，
//    `Library/` 里已经编码好的纹理对象不会自己变（A17 §三·1 原话）。所以 R7 §五·1 那条空缺
//    （「重导后实读 `Texture2D.format`」）一直没人补 —— 本探针就是补它。
//
// 本探针**只读**：不动任何 `.meta` / 不重导 / 不 SaveScene。
// 唯一输出 = `<Unity 工程根>/_tmp_view/b109_e3/sdf_format.tsv`（`_tmp_view` 不进 git）。
//
// ============================ 判定规则（写进 TSV 的 verdict 列） ============================
//   期望值来判据 `资料/完全复刻/判据/B108_AUD09_纹理格式.md` §二 D 行 + §3·1：
//     · `sdf` 组（`<阵营>_tier{1..4}` / `<阵营>_strat_tier{1..4}` / `cardback_noise`）
//       原版 **`m_TextureFormat = 4`(RGBA32) · 未压缩** ⇒ 期望 `TextureFormat.RGBA32`
//     · `keeper` 组（`generic` / `Card_board_frame_SDF`）原版 `Card board frame SDF` 是 **BC7**
//       ⇒ 期望 `TextureFormat.BC7`（这两张同时兼作**对照组**：BC7 读得出来，说明探针本身没瞎）
//   `OK`          —— 实际 fmt == 期望
//   `MISMATCH`    —— 不等于期望（⇒ 这次改 `.meta` 没生效，或还没重导）
//   `NO_TEXTURE`  —— `AssetDatabase` 取不到（导入失败 / 路径写错）
//
// ============================ 用法（批处理，⛔ 子代理不许跑，归调度台） ============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod B109_E3_SdfFormatProbe.Run -logFile -
//   筛输出：grep "^B109 "
//   ⚠️ 若 `.meta` 改过但还没重导，先跑一次 `-executeMethod ArtBaker.ApplyImportSettings` 再跑本探针；
//      两条一起跑也可以（本探针排在后面即可）。
//   ⚠️ 类名 == 文件名、且在全局命名空间（本工程的硬要求，见 CLAUDE.md「!u!115 存根」那条）。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class B109_E3_SdfFormatProbe
{
    const string Dir = "Assets/CardPresentation/Resources/Art/card_sdf";
    const string OutRel = "_tmp_view/b109_e3/sdf_format.tsv";

    // 原版就是 BC7 的两张（`Card board frame SDF` 本体 + 它的 79×107 裁片）——
    // 与 `ArtBaker.ApplyImportSettings` 的 `isSdf` 例外表、`工具/import_original_card_sdf.py`
    // 的 `KEEP_BC7` 是**同一条判据**（三处，改一处要顺手核另两处）。
    static readonly string[] Keepers = { "generic.png", "Card_board_frame_SDF.png" };

    /// <summary>批处理入口：`-executeMethod B109_E3_SdfFormatProbe.Run`。</summary>
    public static void Run()
    {
        string outPath = Path.Combine(ProjectRoot(), OutRel);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        var lines = new List<string>();
        lines.Add(string.Join("\t", new[]
        {
            "file", "class", "expected", "actualFormat", "actualCode", "width", "height", "mipCount",
            "importerCompression", "importerSRGB", "importerMip", "importerNPOT", "importerType",
            "importerAlphaIsTransparency", "importerReadable", "platformDefaultComp",
            "platformStandaloneComp", "platformStandaloneOverridden", "verdict",
        }));

        if (!AssetDatabase.IsValidFolder(Dir))
        {
            Debug.LogError("B109 目录不存在: " + Dir);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Dir });
        Array.Sort(guids, StringComparer.Ordinal);

        int seen = 0, ok = 0, mismatch = 0, noTex = 0;
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !seenPaths.Add(path)) continue;
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            seen++;

            string file = Path.GetFileName(path);
            bool keeper = Array.IndexOf(Keepers, file) >= 0;
            string klass = keeper ? "keeper" : "sdf";
            TextureFormat expected = keeper ? TextureFormat.BC7 : TextureFormat.RGBA32;
            string expText = expected + "(" + (int)expected + ")";

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                noTex++;
                // 19 列：file/class/expected/actualFormat + actualCode/width/height/mipCount
                //        + importer×7 + platform×3 + verdict
                lines.Add(Row(file, klass, expText, "NO_TEXTURE",
                              "-", "-", "-", "-",
                              "-", "-", "-", "-", "-", "-", "-",
                              "-", "-", "-",
                              "NO_TEXTURE"));
                Debug.LogWarning("B109 NO_TEXTURE " + path);
                continue;
            }

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            string impComp = ti != null ? ti.textureCompression.ToString() : "?";
            string impSrgb = ti != null ? (ti.sRGBTexture ? "1" : "0") : "?";
            string impMip = ti != null ? (ti.mipmapEnabled ? "1" : "0") : "?";
            string impNpot = ti != null ? ti.npotScale.ToString() : "?";
            string impType = ti != null ? ti.textureType.ToString() : "?";
            string impAlpha = ti != null ? (ti.alphaIsTransparency ? "1" : "0") : "?";
            string impRead = ti != null ? (ti.isReadable ? "1" : "0") : "?";

            string defComp = "?", stdComp = "?", stdOver = "?";
            if (ti != null)
            {
                // `DefaultTexturePlatform` 块 = **生效值**（没开平台覆盖时 Unity 就按它导）
                var d = ti.GetDefaultPlatformTextureSettings();
                defComp = d.textureCompression.ToString();
                var s = ti.GetPlatformTextureSettings("Standalone");
                stdComp = s.textureCompression.ToString();
                stdOver = s.overridden ? "1" : "0";
            }

            string verdict = tex.format == expected ? "OK" : "MISMATCH";
            if (verdict == "OK") ok++;
            else mismatch++;

            lines.Add(Row(file, klass, expText, tex.format.ToString(), ((int)tex.format).ToString(CultureInfo.InvariantCulture),
                          tex.width.ToString(CultureInfo.InvariantCulture), tex.height.ToString(CultureInfo.InvariantCulture),
                          tex.mipmapCount.ToString(CultureInfo.InvariantCulture),
                          impComp, impSrgb, impMip, impNpot, impType, impAlpha, impRead,
                          defComp, stdComp, stdOver, verdict));

            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "B109 {0,-9} {1,-40} expected={2} actual={3}({4}) {5}x{6} mip={7} imp={8}/sRGB{9} def={10} std={11}(over{12})",
                verdict, file, expText, tex.format, (int)tex.format, tex.width, tex.height,
                tex.mipmapCount, impComp, impSrgb, defComp, stdComp, stdOver));
        }

        File.WriteAllText(outPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));

        Debug.Log(string.Format(CultureInfo.InvariantCulture,
            "B109 合计 读={0} OK={1} MISMATCH={2} NO_TEXTURE={3}", seen, ok, mismatch, noTex));
        Debug.Log("B109 产出: " + outPath);

        if (mismatch > 0)
            Debug.LogWarning("B109 有 " + mismatch + " 张的实际 format 与期望不符 —— `.meta` 改了但可能还没重导（先跑 ArtBaker.ApplyImportSettings）。");
    }

    static string Row(params string[] cells)
    {
        return string.Join("\t", cells);
    }

    static string ProjectRoot()
    {
        // Application.dataPath = <根>/MyGame/Assets  ⇒ 上两级 = <根>（= D:/4/Unity）
        string assets = Application.dataPath.Replace('\\', '/');
        var di = new DirectoryInfo(assets);
        return di.Parent != null && di.Parent.Parent != null ? di.Parent.Parent.FullName : assets;
    }
}
