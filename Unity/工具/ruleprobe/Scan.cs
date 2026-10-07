using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RuleEngine;

/// <summary>
/// 全池解析 dump —— 一行一卡，用来做「解析器改前/改后」的离线对拍。
/// 输出格式：`id|name|unparsed|partial|op;op;...`
///   · 第 1 列 `id|name` = **键**（比较时只有它变 ⇒ 算「改名」）
///   · 第 2 列往后 = **定义**（变了 ⇒ 算「解析差异」，那才是引擎真动了）
/// ⚠️ 2026-10-18 收编时修了两处（原来那份在 D:/tmp/wf_b14_probe）：
///   ① 参数名 `kind` 其实是 `unparsed`（EffectText.Parse 的第二个 out）—— 名字叫反了，正名。
///   ② 原来写 `.Append(kind)` ⇒ 命中 `StringBuilder.Append(object)` ⇒ **每张卡都打成字面量
///      `System.Collections.Generic.List\`1[System.String]`**，等于那一列恒为常数、**白扔**。
///      更要命的是「**没解析出来的碎片**」（`unparsed`，解析器最该被对拍的东西）被整个丢掉了。
///      修法 = `string.Join(",", unparsed)`。⚠️ 改完**基线必须重生成**（每一行都变了）。
/// </summary>
class Scan
{
    public static void ScanMain(string[] args, bool quiet = false)
    {
        string outp = args.Length > 0 ? args[0] : "scan.txt";
        string poolPath = Program.PoolPath;
        if (!File.Exists(poolPath)) { Console.Error.WriteLine("!! 找不到卡池：" + poolPath); Environment.Exit(2); }
        var doc = JsonDocument.Parse(File.ReadAllText(poolPath));
        var sb = new StringBuilder();
        foreach (var c in doc.RootElement.GetProperty("cards").EnumerateArray())
        {
            string S(string k) => c.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            string name = S("name"), desc = S("desc");
            if (string.IsNullOrEmpty(desc)) continue;
            var ops = EffectText.Parse(desc, out var unparsed, out var partial, false);
            sb.Append(S("id") ?? "").Append("|").Append(name ?? "").Append("|")
              .Append(unparsed == null ? "" : string.Join(",", unparsed)).Append("|")
              .Append(partial == null ? "" : string.Join(",", partial)).Append("|");
            if (ops != null)
                foreach (var op in ops)
                    sb.Append(op.Verb).Append("/").Append(op.ChooseSrc).Append("/").Append(op.ChooseWhat)
                      .Append("/").Append(op.ChooseAct).Append("/")
                      .Append(op.Amount).Append("/").Append(op.Payload).Append(";");
            sb.Append("\n");
        }
        var dir = Path.GetDirectoryName(Path.GetFullPath(outp));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outp, sb.ToString());
        if (!quiet) Console.WriteLine("wrote " + outp + " lines=" + sb.ToString().Split('\n').Length);
    }
}
