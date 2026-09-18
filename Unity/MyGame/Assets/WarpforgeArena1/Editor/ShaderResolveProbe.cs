// ShaderResolveProbe.cs — 点名验「这几个 shader 到底解析得到吗」（2026-09-15）
//
// **为什么要它**：`BlendProbe` 的「shader 解析」那一段是从 `导出报告.tsv` 里抓
// `原 shader:` 那些行的名字来试的（`BlendProbe.cs:128-150`）。实测**它没覆盖到全部**：
// `Shader Graphs/Fx_ParticleDissolve_apb` / `Fx_RockDissolve` / `Eclipse Tau` 三个名字
// **就在报告里**，可**把它们从映射表里删掉、`BlendProbe` 照样报 70/70 / 0 解析不到**
// （2026-09-15 亲手 stash 验过）—— 也就是说**那条断言一直漏着它们**，
// 这正是它们能在没映射的情况下一直烂到 C 组的原因。
//
// 这个探针**不依赖任何报告**：点名试这几个名字，逐条打印
// `TryResolve` 走的是哪条路（自建 / 工程自带 / 原版bundle / 解析不到）、拿到的是什么 shader。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod ShaderResolveProbe.Run -logFile "d:/4/_tmp_view/shaders.log"
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class ShaderResolveProbe
{
    const string P = "SRP ";

    /// <summary>点名的名字。前三个 = 2026-09-15 补映射的那三张 ShaderGraph；
    /// 后两个是**对照组**（一个已知有映射、一个已知是工程自带的）。</summary>
    static readonly string[] Names =
    {
        "Shader Graphs/Fx_ParticleDissolve_apb",
        "Shader Graphs/Fx_RockDissolve",
        "Shader Graphs/Eclipse Tau",
        "Shader Graphs/Doomweaver effect",          // 对照：已知在映射表里
        "Universal Render Pipeline/Particles/Unlit",// 对照：工程自带
        "Everguild/FX/Particle Premultiply",        // 对照：2026-09-15 补的那条（Explosion_Ground）
    };

    public static void Run()
    {
        int ok = 0, bad = 0;
        foreach (var n in Names)
        {
            Shader sh; string src;
            bool r = WarpforgeShaderMap.TryResolve(n, out sh, out src);
            if (r && sh != null) ok++; else bad++;
            Debug.Log(P + (r && sh != null ? "✓ " : "✗ ") + n +
                      "  → " + (r && sh != null ? (sh.name + "（" + src + "）") : "**解析不到**"));
        }
        Debug.Log(P + "小结：" + ok + " 个解析得到 / " + bad + " 个解析不到");

        // 🆕 2026-09-18：`UseOriginal` 白名单是**真的断言**，不是打印 ——
        //    这批名字「改走原件」的**唯一意思**就是解析到 `原版bundle`；
        //    一旦有谁回落到「自建」或解析不到，就说明白名单没生效（那会是**静默**的：
        //    画面还是自建近似，没人看得出来）。判据出处：`WarpforgeShaderMap.UseOriginal`。
        int wOk = 0, wBad = 0;
        var badList = new System.Collections.Generic.List<string>();
        foreach (var n in WarpforgeShaderMap.UseOriginal)
        {
            Shader sh; string src;
            bool r = WarpforgeShaderMap.TryResolve(n, out sh, out src);
            bool good = r && sh != null && src != null && src.StartsWith("原版bundle");
            if (good) wOk++;
            else { wBad++; badList.Add(n + "(" + (r && sh != null ? src : "解析不到") + ")"); }
        }
        Debug.Log(P + "改走原件白名单：" + wOk + " 个走原件 / " + wBad + " 个**没走成**");
        if (badList.Count > 0) Debug.Log(P + "没走成的：" + string.Join(" / ", badList.ToArray()));

        bool pass = (bad == 0 && wBad == 0);
        Debug.Log(P + (pass ? "=== 通过 ===" : "=== 不通过 ==="));
        if (Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
    }
}
