// T09Probe.cs —— **临时测量器**（2026-10-18，T09 首次开窗性能）。
// 目的：真机读数显示「每格 2,168 ms」不在 `CardView.Build`（实测均 11 ms），
//       而 `CollectionWindow` 的 phase ① 还包着 `BattleDriver.ToCardData`。
//       本探针在**编辑器批处理**里直接给 `ToCardData` 计时（它不依赖 Play 模式），
//       两趟：第 1 趟含一次性初始化，第 2 趟是暖路径。
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$U" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod T09Probe.Run -logFile d:/4/_tmp_view/t09_probe.log
// ⚠️ 测完即删（不是常驻工具）。
using System.Diagnostics;
using RuleEngine;
using UnityEngine;

public static class T09Probe
{
    public static void Run()
    {
        var pool = CardDatabase.Load();
        UnityEngine.Debug.Log($"[T09] pool = {pool.Count}");
        for (int pass = 0; pass < 2; pass++)
        {
            var sw = Stopwatch.StartNew();
            double total = 0, max = 0;
            string maxId = "";
            int n = 0;
            foreach (var c in pool)
            {
                double t0 = sw.Elapsed.TotalMilliseconds;
                var d = CardPresentation.BattleDriver.ToCardData(c, c.Faction);
                double dt = sw.Elapsed.TotalMilliseconds - t0;
                total += dt;
                if (dt > max) { max = dt; maxId = c.Id; }
                n++;
                if (n >= 100) break;
            }
            UnityEngine.Debug.Log($"[T09] pass{pass}: n={n} total={total:F1} ms · avg={(n > 0 ? total / n : 0):F2} ms · max={max:F1} ms ({maxId})");
        }
    }
}
