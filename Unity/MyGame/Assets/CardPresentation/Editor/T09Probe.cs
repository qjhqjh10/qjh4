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
    }
}
