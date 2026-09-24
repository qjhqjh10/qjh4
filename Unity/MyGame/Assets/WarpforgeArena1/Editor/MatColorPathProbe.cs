// MatColorPathProbe.cs — 判「清单里的 `matColor` 到底有没有走到材质上」。
//
// 为什么需要它（2026-09-24 查 arena1 (920,280) 蒸汽颜色时顺手查到）：
//   `ArenaBuilder.GetOrCreateParticleMaterial(..., matColor)` 里明明有
//   `if (matColor != null && matColor.Length >= 3) mat.SetColor("_BaseColor", c)`，
//   但实况是 **所有 `PS_*.mat` 的 `_BaseColor` 全是白 (1,1,1,1)**，而且
//   **清单里 13 个「贴图 × 颜色」组合只落了 11 个材质文件**（= 只按贴图去重）。
//   两个候选：① 清单反序列化后 `matColor` 是 null；② 值设了又被别处覆盖成白。
//   这支探针答的是 ①（读一遍 `ArenaBuilder.LoadManifest` 自己看）。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod MatColorPathProbe.Run -logFile "d:/4/_tmp_view/mcp.log"
//   筛输出：grep "^MCP " d:/4/_tmp_view/mcp.log
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MatColorPathProbe
{
    const string P = "MCP ";

    public static void Run()
    {
        Debug.Log(P + "=== 清单 matColor 通路判定 开始 ===");
        foreach (var scene in new[] { "battlearena1", "battlearenaleviathan" })
        {
            var mf = ArenaBuilder.LoadManifest(scene);
            if (mf == null) { Debug.LogWarning(P + $"  {scene}: LoadManifest 返回 null"); continue; }
            var ps = mf.particles;
            if (ps == null) { Debug.LogWarning(P + $"  {scene}: particles == null"); continue; }

            int nonNull = 0, nullCount = 0, wrongLen = 0;
            var colors = new Dictionary<string, int>();
            var samples = new List<string>();
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (p == null) continue;
                if (p.matColor == null) { nullCount++; if (samples.Count < 6) samples.Add($"    idx {i} {p.go}: matColor = **null**"); continue; }
                if (p.matColor.Length < 3) { wrongLen++; continue; }
                nonNull++;
                string k = $"{p.matColor[0]:F3},{p.matColor[1]:F3},{p.matColor[2]:F3}";
                colors.TryGetValue(k, out var c); colors[k] = c + 1;
                if (samples.Count < 6) samples.Add($"    idx {i} {p.go}: ({k}) len={p.matColor.Length}");
            }

            var sb = new StringBuilder();
            sb.Append(P).Append($"  {scene}: particles={ps.Length} · matColor 非空 {nonNull} · 空 {nullCount} · 长度<3 {wrongLen}\n");
            var texCount = new Dictionary<string, int>();
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i]; if (p == null || p.texFile == null) continue;
                texCount.TryGetValue(p.texFile, out var c); texCount[p.texFile] = c + 1;
            }
            sb.Append(P).Append($"    贴图种类 {texCount.Count} · 「贴图×颜色」组合 {CountCombos(ps)}\n");
            sb.Append(P).Append("    颜色分布：" + string.Join("  ", colors.Keys));
            foreach (var s in samples) sb.Append("\n").Append(P).Append(s);
            Debug.Log(sb.ToString());

            Debug.Log(P + "    ⇒ " + (nullCount > 0
                ? "**反序列化后是 null** ⇒ 通路断在清单→C# 这一步（探针 ①）"
                : "非空 ⇒ 通路没断在这里，去查「设了又被覆盖」（探针 ②）"));
        }
        Debug.Log(P + "=== 结束 ===");
    }

    static int CountCombos(ArenaBuilder.ParticleEntry[] ps)
    {
        var set = new HashSet<string>();
        foreach (var p in ps)
        {
            if (p == null) continue;
            string k = (p.texFile ?? "-") + "|" + (p.matColor == null ? "null" : $"{p.matColor[0]:F3}");
            set.Add(k);
        }
        return set.Count;
    }
}
