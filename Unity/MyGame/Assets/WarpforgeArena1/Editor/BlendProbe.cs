// BlendProbe.cs — **混合状态**的常驻守卫（2026-09-13 第三十三轮）
//
// 为什么单开一个探针：这条链路**踩过两次**，两次都是「判据选错」，而且症状一样 ——
// **加法发光变成不透明覆盖 + 写深度**（场景里把后面的东西整块抠掉）：
//   · 2026-09-12（P1-a0）：判「原版材质里有没有 `_SrcBlend`」—— 原版材质带着内置 Standard 的
//     **残留值**（1/0/1），判据恒为真 ⇒ 兜底 `InferBlend` 根本没机会跑。
//     修成「判**我们这边的 shader** 认不认 `_SrcBlend`」。
//   · 2026-09-13（第三十三轮，派子代理逐效果定根因时查出来）：`WFParticlesExtraColor.shader`
//     自己就写着 `Blend [_SrcBlend][_DstBlend]` —— **它认这个属性** ⇒ 残留值又被灌进来。
//     按技术构成统计出 **61 条**效果中这一条（精灵图 32 + Mesh/Matcap 29）。
//
// ⇒ 现在的判据是「**按原版 shader 名推应有的状态**」（`WarpforgeShaderMap.InferBlend`），
//    **不问材质、也不问我们这边认不认**。这个探针就是钉住它：
//    把每个材质定义**真的建出来**，断言它的 `_SrcBlend/_DstBlend/_ZWrite` 等于推出的值。
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod BlendProbe.Run -logFile "d:/4/_tmp_view/blend.log"
//   筛输出：grep "^BP "；退出码 0 = 断言全过
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class BlendProbe
{
    const string P = "BP ";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string ReportPath = "d:/4/_tmp_view/blend_report.tsv";

    public static void Run()
    {
        int pass = 0, fail = 0, mats = 0, skipped = 0;
        var bad = new List<string>();
        var rows = new StringBuilder();
        rows.AppendLine("原版shader\t材质名\t期望_SrcBlend\t实得\t期望_DstBlend\t实得\t期望_ZWrite\t实得\t结论");

        var files = Directory.GetFiles(PrefabDir, "*.prefab", SearchOption.TopDirectoryOnly);
        Debug.Log(P + $"=== 混合状态探针：{files.Length} 个 prefab ===");

        foreach (var f in files)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'));
            if (go == null) continue;
            foreach (var binder in go.GetComponentsInChildren<WarpforgeEffectBinder>(true))
            {
                if (binder.materials == null) continue;
                foreach (var d in binder.materials)
                {
                    if (d == null) continue;
                    var want = WarpforgeShaderMap.InferBlend(d.shader);
                    if (want == null) { skipped++; continue; }   // 推不出来 ⇒ 不判（原版属性表是活的）

                    mats++;
                    var m = WarpforgeEffectBinder.BuildForProbe(d);
                    if (m == null) { fail++; bad.Add($"{Path.GetFileName(f)}/{d.name}: 材质建不出来"); continue; }

                    float s = m.HasProperty("_SrcBlend") ? m.GetFloat("_SrcBlend") : float.NaN;
                    float dstop = m.HasProperty("_DstBlend") ? m.GetFloat("_DstBlend") : float.NaN;
                    float zw = m.HasProperty("_ZWrite") ? m.GetFloat("_ZWrite") : float.NaN;

                    bool ok = (!m.HasProperty("_SrcBlend") || Mathf.Approximately(s, want[0]))
                           && (!m.HasProperty("_DstBlend") || Mathf.Approximately(dstop, want[1]))
                           && (!m.HasProperty("_ZWrite") || Mathf.Approximately(zw, want[2]));
                    rows.AppendLine($"{d.shader}\t{d.name}\t{want[0]}\t{s}\t{want[1]}\t{dstop}\t{want[2]}\t{zw}\t{(ok ? "OK" : "**错**")}");
                    if (ok) pass++;
                    else
                    {
                        fail++;
                        // ⚠️ **1/0/1 是那个残留值的指纹**（One / Zero / 写深度）
                        bool residue = Mathf.Approximately(s, 1f) && Mathf.Approximately(dstop, 0f)
                                       && Mathf.Approximately(zw, 1f);
                        bad.Add($"{Path.GetFileName(f)}/{d.name}（原版 {d.shader}）："
                              + $"期望 {want[0]}/{want[1]}/{want[2]}，实得 {s}/{dstop}/{zw}"
                              + (residue ? " —— **正是 Standard 残留值的指纹（不透明+写深度）**" : ""));
                    }
                }
            }
        }

        foreach (var b in bad)
            if (bad.IndexOf(b) < 20) Debug.LogError(P + "   ✗ " + b);

        System.IO.File.WriteAllText(ReportPath, rows.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + $"   可判材质 {mats} 个（跳过的 {skipped} 个：原版 shader 名推不出混合，不判）");
        Debug.Log(P + $"   报告 {ReportPath}");
        if (fail == 0) Debug.Log(P + $"=== 断言：{pass} 通过 / 0 失败 ✅ ===");
        else Debug.LogError(P + $"=== 断言：{pass} 通过 / **{fail} 失败** ❌ ===");

        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }
}
