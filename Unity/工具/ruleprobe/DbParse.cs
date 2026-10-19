// DbParse.cs — `dbparse`：**`CardDatabase.Parse` 的形状 + 耗时**探针（E12 · 2026-10-19 · 第五十七会话）
//
// 为什么单开一条：
//   · `cards_engine.json` v12 加了 `animTimes`（每卡逐候选动画时间）与顶层 `talentNameIds`，
//     文件从 842,451 B → 1,102,287 B（+30.8%）。**「JsonUtility 会不会抛 / 装载慢了没有」**
//     在这之前**一条读数都没有**（E12 报告 §三·9 / §三·6）。
//   · 本仓 `Program.LoadPool` 走的是 `System.Text.Json` 手读（为了同时留 `keywords` 原文），
//     **不经过 `Data/CardDatabase.cs`** ⇒ 它量不出 `CardDatabase.Parse` 的真实成本。
//     本子命令**直接调用真 `CardDatabase.Parse`**（csproj 把 `Data/CardDatabase.cs` 也编进来）；
//     `JsonUtility` 用的是 `Stub.cs` 那份（System.Text.Json + `IncludeFields`）——
//     ⚠️ **它不是 Unity 的原生 JsonUtility**，所以它证明的是「DTO 形状能被一个 Unity 形状的
//     解析器吃下、字段能逐字落到 `CardDef`」，**不是**「Unity 原生解析不抛」。
//     后者仍需首跑 Unity（见 `资料/普查产出_1019/W57_引擎线_E12四条.md` §一·1）。
//
// 读数口径（照 `资料/普查产出_1019/W57_引擎线_E12四条.md` §四）：
//   · first = 本进程**第一次** `Parse`（进程由 `ruleprobe.sh` 新起 ⇒ ≈ 冷启动，含 JIT）；
//   · warm  = 7 次，**每次前强制 GC**（堆压力固定住），取中位数 / 最小 / 最大；
//   · 口径对照：同一份数据去掉 `animTimes` + `talentNameIds`（842,478 B）再跑，
//     热中位数 **369.8 ms vs 368.6 ms**（2026-10-19 实测）⇒ 新增那 259,809 B **量不出来**，
//     JSON 解序列化本身只从 2.56 → 4.10 ms（`FromJson` 单测）。成本大头是 1127 个 `CardDef` 构造。
//
// ⛔ 本子命令**只读卡池**（与 `A1382` 那条「探针对卡池只读」一致）；不写基线、不动任何指纹。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RuleEngine;

/// <summary>`dbparse` 入口（全局命名空间，与 `Program` / `Layers` 同层）。</summary>
static class DbParse
{
    public static int Run()
    {
        string path = Program.PoolPath;
        if (!File.Exists(path)) { Console.Error.WriteLine("!! 找不到卡池：" + path); return 2; }
        if (!string.IsNullOrEmpty(Program.PoolOverride))
            Console.WriteLine("⚠️ 卡池被 `WFPROBE_POOL` 覆盖 ⇒ 下面读数只对这份输入成立：" + path);

        long bytes = new FileInfo(path).Length;
        string json = File.ReadAllText(path);
        Console.WriteLine($"dbparse: {path}");
        Console.WriteLine($"  bytes={bytes:N0}  chars={json.Length:N0}");

        // ① 首调（本进程内 ≈ 冷）
        var sw = Stopwatch.StartNew();
        List<CardDef> pool;
        try { pool = CardDatabase.Parse(json); }
        catch (Exception e) { Console.WriteLine("!! `CardDatabase.Parse` 抛异常：" + e.GetType().Name + ": " + e.Message); return 1; }
        sw.Stop();
        double first = sw.Elapsed.TotalMilliseconds;

        // ② 热调 ×7（每次前强制 GC）
        var ts = new List<double>();
        for (int k = 0; k < 7; k++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var s = Stopwatch.StartNew();
            List<CardDef> p;
            try { p = CardDatabase.Parse(json); }
            catch (Exception e) { Console.WriteLine("!! 第 " + (k + 2) + " 次 Parse 抛异常：" + e.GetType().Name + ": " + e.Message); return 1; }
            s.Stop();
            if (p.Count != pool.Count) { Console.WriteLine("!! 两次装载张数不同：" + pool.Count + " vs " + p.Count); return 1; }
            ts.Add(s.Elapsed.TotalMilliseconds);
        }
        ts.Sort();

        int nAnim = pool.Count(c => c.AnimTimes.Count > 0);
        int nEntries = pool.Sum(c => c.AnimTimes.Count);
        Console.WriteLine($"  cards={pool.Count}  ids-uniq={pool.Select(c => c.Id).Distinct().Count()}"
                        + $"  animTimes-cards={nAnim}  entries={nEntries}  talentNameKeys={TalentCardData.NameMapKeys}");
        Console.WriteLine($"  Parse: first={first:F0} ms   warm x7 med={ts[3]:F0} min={ts[0]:F0} max={ts[6]:F0} ms"
                        + $"   bytes/card={(pool.Count > 0 ? bytes / (double)pool.Count : 0):F0}");
        return 0;
    }
}
