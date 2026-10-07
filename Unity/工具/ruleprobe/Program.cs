using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using RuleEngine;

/// <summary>
/// 引擎离线对拍探针 —— **快速内环 / 旁证**。
/// ⚠️ **正式验收仍然是 `RuleEngineTest.Run`（断言的真宿主）**，本工具只是让「改完立刻能对一眼」变便宜。
/// 原理：`RuleEngine/Core/` 按工程成文规矩**只许碰 `UnityEngine.Debug` / `UnityEngine.Random`**
/// （见 Core 各文件头注 + `TutorialScript.cs:344-352`）⇒ 三十行桩就能把整个 Core 编成控制台程序，
/// **不跑 Unity、不占 Unity 实例、不用等 1–9 分钟编译**（实测重编 ~2 秒）。
/// 收编自 `D:/tmp/wf_b14_probe/`（2026-10-18；那份在临时目录里、迟早被清）。
/// </summary>
class Program
{
    static List<CardDef> Pool;
    public static string OutDir;

    /// <summary>卡池路径 —— 由仓库根推出来，⛔ 不写死盘符绝对路径。</summary>
    public static string PoolPath { get { return Path.Combine(OutDir, "RuleEngine", "Resources", "cards_engine.json"); } }

    static void Main(string[] args)
    {
        // 🔴 本机默认是 GBK（实测中文输出 `B8 B4 C5 DC`）⇒ 不设这个，所有中文都乱码，
        //    「摘要行一眼判读」这个用途就废了。
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
        OutDir = ResolveOutDir();
        if (args.Length == 0)
        {
            Console.WriteLine("usage: seg <text...> | card <name> | scan <out> | check | filter <kw> | jackal | emergency | suppressor | played | b19test");
            Environment.Exit(1);
        }
        if (args[0] == "scan")
        {
            Scan.ScanMain(new[] { args.Length > 1 ? args[1] : "scan.txt" });
            return;
        }
        if (args[0] == "check" && !File.Exists(PoolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + PoolPath); Environment.Exit(2); }
        int i = 0;
        while (i < args.Length)
        {
            switch (args[i])
            {
                case "seg": Dump(args[i + 1]); i += 2; break;
                case "card": Card(args[i + 1]); i += 2; break;
                case "filter": LoadPool(); FilterAdd.Run(Pool, args[i + 1]); i += 2; break;
                case "jackal": LoadPool(); BattleProbe.Jackal(Pool); i += 1; break;
                case "emergency": LoadPool(); BattleProbe.Emergency(Pool); i += 1; break;
                case "suppressor": LoadPool(); BattleProbe.Suppressor(Pool); i += 1; break;
                case "played": LoadPool(); BattleProbe19.Run(Pool); i += 1; break;
                case "b19test": LoadPool(); if (B19Test.Run(Pool) > 0) Environment.Exit(1); i += 1; break;
                case "check": Environment.Exit(CheckMain.Run(args.Length > 1 ? args[1] : null)); break;
                default: Dump(args[i]); i += 1; break;
            }
        }
    }

    /// <summary>从**程序集所在目录**往上找 `MyGame/Assets`（三条腿通用：bin/Debug/net8.0 → ruleprobe → 工具 → Unity）。</summary>
    static string ResolveOutDir()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        while (d != null)
        {
            var p = Path.Combine(d.FullName, "MyGame", "Assets");
            if (Directory.Exists(p)) return p;
            d = d.Parent;
        }
        return @"D:/4/Unity/MyGame/Assets";   // 兜底：找不到就按标准盘符（找不到时 caller 会出声，⛔ 不静默）
    }

    static void LoadPool()
    {
        if (Pool != null) return;
        if (!File.Exists(PoolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + PoolPath); Environment.Exit(2); }
        var doc = JsonDocument.Parse(File.ReadAllText(PoolPath));
        var list = new List<CardDef>();
        foreach (var c in doc.RootElement.GetProperty("cards").EnumerateArray())
        {
            string S(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            int I(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            var kws = new List<string>();
            if (c.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Array)
                foreach (var k in kw.EnumerateArray()) if (k.ValueKind == JsonValueKind.String) kws.Add(k.GetString());
            list.Add(new CardDef(S("id"), S("name"), S("type"), S("desc"), S("rarity"), S("faction"),
                                 I("cost"), I("attack"), I("health"), I("ranged"), kws,
                                 S("nameZh"), S("descZh"), true, S("subtype")));
        }
        Pool = list;
    }

    static void Dump(string line, string tag = "SEG")
    {
        var ops = EffectText.Parse(line, out var unparsed, out var partial, false);
        Console.WriteLine(tag + " = [" + line + "]");
        Console.WriteLine("  unparsed=" + (unparsed == null ? "-" : string.Join(" | ", unparsed))
                          + " partial=" + (partial == null ? "-" : string.Join(" | ", partial)));
        if (ops == null) { Console.WriteLine("  (null)"); return; }
        foreach (var op in ops)
        {
            Console.WriteLine("  op: verb=" + op.Verb
                + " amount=" + op.Amount
                + " payload=[" + op.Payload + "]"
                + " target=[" + (op.Target == null ? "" : op.Target.Raw) + "]"
                + " tail=[" + (op.Tail ?? "") + "]"
                + " cost=" + op.Cost + " ck=" + op.CostKind
                + " chooseSrc=" + op.ChooseSrc + " what=[" + op.ChooseWhat + "] act=[" + op.ChooseAct + "]"
                + " deadScope=" + op.ChooseDeadScope);
        }
    }

    static void Card(string name)
    {
        LoadPool();
        var hits = Pool.Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (hits.Count == 0) { Console.WriteLine("!! no card named " + name); return; }
        foreach (var c in hits)
        {
            Console.WriteLine("CARD " + c.Name + " id=" + c.Id + " type=" + c.Type + " subtype=" + c.Subtype
                              + " rarity=" + c.Rarity + " faction=" + c.Faction + " cost=" + c.Cost
                              + " kw=[" + string.Join(",", c.Keywords.Keys) + "]");
            Console.WriteLine("  desc = " + c.Desc);
            var ops = PlayerChooseOpsOf(c);
            foreach (var op in ops)
                Console.WriteLine("  ASK verb=" + op.Verb + " src=" + op.ChooseSrc + " what=[" + op.ChooseWhat
                                  + "] act=[" + op.ChooseAct + "] payload=[" + op.Payload + "]");
            Dump(c.Desc, "  parse");
        }
    }

    static List<EffectOp> PlayerChooseOpsOf(CardDef c)
    {
        return RuleCore.PlayerChooseOps(c);
    }
}

/// <summary>
/// `check` 模式 —— 把「全池解析 dump」与基线逐行比，**打一行摘要就完事**（给 `ruleprobe.sh` 当判据）。
/// 🔴 两个数必须分开（照 `D:/tmp/wf_b14_probe` 那次的教训）：
///   · **解析差异** = 第 2 列往下变了 ⇒ **红**（引擎解析器真动了，要逐行看）
///   · **改名** = 只有第 1 列（`id|name`）变了 ⇒ **黄**（本工程每批都在改卡名/重跑产物，通常无害）
/// ⛔ 别拿「逐字节相同」当判据 —— 卡名是**会变的产物**（B16/B20 两批共改 6 张），
///    拿它当基线判据 ⇒ 第一天就红、而且红的是**噪声**，会把「红了不用管」训练成习惯。
/// 退出码：0 = 解析差异 0 行 · 1 = 有解析差异 · 2 = 基线缺失/读不到。
/// </summary>
class CheckMain
{
    public static int Run(string baselinePath)
    {
        if (string.IsNullOrEmpty(baselinePath))
            baselinePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "基线", "out_baseline.txt");
        baselinePath = Path.GetFullPath(baselinePath);
        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine("!! 没有基线：" + baselinePath);
            Console.Error.WriteLine("   生成办法：先修好解析器，再跑  scan 工具/ruleprobe/基线/out_baseline.txt  把它定为新基线。");
            return 2;
        }
        var tmp = Path.Combine(Path.GetTempPath(), "wfprobe_check.txt");
        Scan.ScanMain(new[] { tmp }, true);
        var oldLines = File.ReadAllLines(baselinePath);
        var newLines = File.ReadAllLines(tmp);

        string KeyOf(string l) { int p = l.IndexOf('|', l.IndexOf('|') + 1); return p < 0 ? l : l.Substring(0, p); }
        string DefOf(string l) { int p = l.IndexOf('|', l.IndexOf('|') + 1); return p < 0 ? "" : l.Substring(p + 1); }

        var oldMap = new Dictionary<string, string>();
        foreach (var l in oldLines) { if (l.Length == 0) continue; oldMap[KeyOf(l)] = DefOf(l); }
        var newMap = new Dictionary<string, string>();
        foreach (var l in newLines) { if (l.Length == 0) continue; newMap[KeyOf(l)] = DefOf(l); }

        int parseDiff = 0, renamed = 0, added = 0, removed = 0;
        var parseNames = new List<string>();
        foreach (var kv in newMap)
        {
            if (!oldMap.TryGetValue(kv.Key, out var od)) { added++; continue; }
            if (!string.Equals(od, kv.Value, StringComparison.Ordinal)) { parseDiff++; if (parseNames.Count < 20) parseNames.Add(kv.Key); }
        }
        foreach (var kv in oldMap) if (!newMap.ContainsKey(kv.Key)) removed++;
        // 改名：同一张卡的 id 在两侧都有、但 name 段不同（键是 `id|name` ⇒ 会被当 added+removed 各一条）
        var oldById = new Dictionary<string, string>();
        foreach (var l in oldLines) { var k = KeyOf(l); int p = k.IndexOf('|'); if (p > 0) oldById[k.Substring(0, p)] = k.Substring(p + 1); }
        for (int n = 0; n < newLines.Length; n++)
        {
            var k = KeyOf(newLines[n]); int p = k.IndexOf('|'); if (p <= 0) continue;
            var id = k.Substring(0, p); var nm = k.Substring(p + 1);
            if (oldById.TryGetValue(id, out var on) && on != nm) renamed++;
        }
        added -= renamed; removed -= renamed;
        if (added < 0) added = 0; if (removed < 0) removed = 0;

        Console.WriteLine("=== 全池 " + newMap.Count + " 张 · 解析差异 " + parseDiff + " 行 · 改名 " + renamed
                          + " 行 · 新增 " + added + " · 消失 " + removed + " · 基线 " + Path.GetFileName(baselinePath) + " ===");
        if (parseDiff > 0)
        {
            Console.WriteLine("⚠️ 解析差异（前 " + parseNames.Count + " 张）：" + string.Join(" · ", parseNames));
            Console.WriteLine("⚠️ 这**不一定**是缺陷 —— 逐行看过再决定是「修基线」还是「修解析器」。");
        }
        return parseDiff > 0 ? 1 : 0;
    }
}
