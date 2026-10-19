// T09Probe.cs —— **临时测量器**（2026-10-18，T09 首次开窗性能）。
// 目的：真机读数显示「每格 2,168 ms」不在 `CardView.Build`（实测均 11 ms），
//       而 `CollectionWindow` 的 phase ① 还包着 `BattleDriver.ToCardData`；
//       上一版探针只量了 `ToCardData` 整段（冷 2,428 ms / 暖 0.25 ms）。
//       本版把它**切开**：Name / FaceTextFull / ArtKey，并把 FaceTextFull 内部再拆
//       UnimplementedNotes（反射私有）/ KeywordSegment / CardIcons.Rewrite，
//       每个子件都用**一张全新的卡**量（谁吃掉一次性初始化，一眼能看出来）。
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$U" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod T09Probe.Run -logFile d:/4/_tmp_view/t09_probe.log
// ⚠️ 测完即删（不是常驻工具）。
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using RuleEngine;
using UnityEngine;

public static class T09Probe
{
    static double T(System.Action a)
    {
        var sw = Stopwatch.StartNew();
        a();
        return sw.Elapsed.TotalMilliseconds;
    }

    public static void Run()
    {
        var pool = CardDatabase.Load();
        UnityEngine.Debug.Log($"[T09] pool = {pool.Count}");
        if (pool.Count < 8) { UnityEngine.Debug.LogError("[T09] 卡池太小，量不了"); return; }

        // ---------- 阶段 A：每个子件用一张**新卡**，冷路径归因 ----------
        var miNotes = typeof(CardPresentation.BattleDriver).GetMethod(
            "UnimplementedNotes", BindingFlags.NonPublic | BindingFlags.Static,
            null, new[] { typeof(CardDef) }, null);
        if (miNotes == null) UnityEngine.Debug.LogWarning("[T09] 反射没找到 UnimplementedNotes（下面的 notes 一栏会是 -1）");

        var c0 = pool[0];
        double tName = T(() => CardPresentation.CardText.Name(c0));
        UnityEngine.Debug.Log($"[T09] A Name            = {tName:F1} ms  ({c0.Id})");

        var c1 = pool[1];
        double tNotes = -1;
        if (miNotes != null) tNotes = T(() => miNotes.Invoke(null, new object[] { c1 }));
        UnityEngine.Debug.Log($"[T09] A UnimplementedNotes = {tNotes:F1} ms  ({c1.Id})");

        var c2 = pool[2];
        bool zh2 = CardPresentation.CardText.Zh && !string.IsNullOrEmpty(c2.DescZh);
        string body2 = zh2 ? c2.DescZh : c2.Desc;
        double tSeg = T(() => CardPresentation.CardText.KeywordSegment(c2.Keywords, zh2, body2, c2.NumericKeywords));
        UnityEngine.Debug.Log($"[T09] A KeywordSegment  = {tSeg:F1} ms  ({c2.Id})");

        var c3 = pool[3];
        bool zh3 = CardPresentation.CardText.Zh && !string.IsNullOrEmpty(c3.DescZh);
        string body3 = zh3 ? c3.DescZh : c3.Desc;
        double tIcons = T(() => CardPresentation.CardIcons.Rewrite(c3.Id, zh3 ? "descZh" : "desc", body3));
        UnityEngine.Debug.Log($"[T09] A CardIcons.Rewrite = {tIcons:F1} ms  ({c3.Id})");

        var c4 = pool[4];
        double tArt = T(() => CardPresentation.BattleDriver.ArtKey(c4));
        UnityEngine.Debug.Log($"[T09] A ArtKey          = {tArt:F1} ms  ({c4.Id})");

        var c5 = pool[5];
        double tFace = T(() => CardPresentation.BattleDriver.FaceTextFull(c5));
        UnityEngine.Debug.Log($"[T09] A FaceTextFull(整段) = {tFace:F1} ms  ({c5.Id})");

        var c6 = pool[6];
        double tOne = T(() => CardPresentation.BattleDriver.ToCardData(c6, c6.Faction));
        UnityEngine.Debug.Log($"[T09] A ToCardData(整段)   = {tOne:F1} ms  ({c6.Id})");

        // ---------- 阶段 B：暖路径 100 张 ----------
        for (int pass = 0; pass < 2; pass++)
        {
            var sw = Stopwatch.StartNew();
            double total = 0, max = 0;
            string maxId = "";
            int n = 0;
            foreach (var c in pool)
            {
                double t0 = sw.Elapsed.TotalMilliseconds;
                CardPresentation.BattleDriver.ToCardData(c, c.Faction);
                double dt = sw.Elapsed.TotalMilliseconds - t0;
                total += dt;
                if (dt > max) { max = dt; maxId = c.Id; }
                n++;
                if (n >= 100) break;
            }
            UnityEngine.Debug.Log($"[T09] B pass{pass}: n={n} total={total:F1} ms · avg={(n > 0 ? total / n : 0):F2} ms · max={max:F1} ms ({maxId})");
        }

        // ---------- 阶段 C（2026-10-18 补）：把 `ToCardData` 按【三段】拆开量 ----------
        // 为什么要拆：`N79` 已把主因 `CardDatabase.Load` 缓存掉，但 `ToCardData` 里还有三条**重腿**
        //   —— ① `CardText.Name`（本地化键查表）② `FaceTextFull`（卡面正文 + 图标改写）
        //   ③ `ArtKey`（立绘键）—— 真机上到底哪条还贵，只有分开量才知道。
        // ⚠️ **纯插桩**：三段的调用次序/实参与 `ToCardData` 内部逐字相同（同一份判据：`Battle/BattleDriver.cs` 的 `ToCardData`），
        //   ⛔ 不改任何行为；这里**不**重写 `ToCardData` 本身（那会造出第二份逻辑）。
        //   「三段之和」与上面 B 的「整段」之差 = 没被这三条覆盖的标量读取（应为 0 附近）。
        {
            double sName = 0, sFace = 0, sArt = 0, sAll = 0, mxName = 0, mxFace = 0, mxArt = 0;
            string mxNameId = "", mxFaceId = "", mxArtId = "";
            int n = 0;
            foreach (var c in pool)
            {
                var swS = Stopwatch.StartNew();
                CardPresentation.CardText.Name(c);
                double d1 = swS.Elapsed.TotalMilliseconds;
                swS.Restart();
                CardPresentation.BattleDriver.FaceTextFull(c);
                double d2 = swS.Elapsed.TotalMilliseconds;
                swS.Restart();
                CardPresentation.BattleDriver.ArtKey(c);
                double d3 = swS.Elapsed.TotalMilliseconds;
                swS.Restart();
                CardPresentation.BattleDriver.ToCardData(c, c.Faction);
                double dAll = swS.Elapsed.TotalMilliseconds;
                sName += d1; sFace += d2; sArt += d3; sAll += dAll;
                if (d1 > mxName) { mxName = d1; mxNameId = c.Id; }
                if (d2 > mxFace) { mxFace = d2; mxFaceId = c.Id; }
                if (d3 > mxArt) { mxArt = d3; mxArtId = c.Id; }
                n++;
                if (n >= 100) break;
            }
            UnityEngine.Debug.Log($"[T09] C n={n} · ToCardData(整段) total={sAll:F1} ms");
            UnityEngine.Debug.Log($"[T09] C ① Name            total={sName:F1} ms · avg={(n > 0 ? sName / n : 0):F3} · max={mxName:F2} ({mxNameId})");
            UnityEngine.Debug.Log($"[T09] C ② FaceTextFull    total={sFace:F1} ms · avg={(n > 0 ? sFace / n : 0):F3} · max={mxFace:F2} ({mxFaceId})");
            UnityEngine.Debug.Log($"[T09] C ③ ArtKey          total={sArt:F1} ms · avg={(n > 0 ? sArt / n : 0):F3} · max={mxArt:F2} ({mxArtId})");
            UnityEngine.Debug.Log($"[T09] C 三段之和={sName + sFace + sArt:F1} ms · 未归因(整段−三段)={sAll - (sName + sFace + sArt):F1} ms");
        }
    }
}
