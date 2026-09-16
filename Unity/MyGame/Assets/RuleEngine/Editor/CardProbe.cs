// CardProbe.cs — 逐卡「**卡级解剖**」探针（2026-09-16）
//
// 为什么要有它（和 `EffectParseProbe` 的分工）：
//   `EffectParseProbe` 量的是「**主解析器对一句文本的意见**」——把一行文字丢给
//   `EffectText.Parse`，看它解出什么。它**不知道卡级的路由**，而卡级（`CardDef`）会把同一段
//   文字**分给好几层**：触发层（`Rally:` …）· 事件层（`When <事件>, …`）· 光环 · 灵魂石 · 誓约 …
//
//   于是「探针说这句话被吞了」**不等于**「这张卡实际会静默失效」—— 可能事件层早就把同一段
//   收对了，主解析器那份只是**没人用的残渣**。2026-09-16 就卡在这一点上：
//   `When …, gain …` 那一族（12 张）主解析器解成
//   `gain 载荷「…」 目标[enemy/any 「when an enemy …」]`（**条件从句当主语**），
//   但「这是不是真 bug」只能**问卡对象自己** —— 就是本探针。
//
// 用法（CLI）：
//   1. 把卡名写进 `d:/4/_tmp_view/cardprobe_in.txt`（一行一个，`#` 开头是注释）
//   2. unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//        -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//        -executeMethod CardProbe.Run -logFile -
//   3. 读 `d:/4/_tmp_view/cardprobe_out.txt`
//
// ⚠️ **它只读文件、不碰工程**，可以放心反复跑。⚠️ 一次 Unity 只能跑一个实例。
// ⚠️ 卡名要**逐字相等**（大小写不敏感）；查不到的会在输出里报「卡池里没有这张」——
//    那是**如实报**，别当它没跑。
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static class CardProbe
{
    const string InPath = "d:/4/_tmp_view/cardprobe_in.txt";
    const string OutPath = "d:/4/_tmp_view/cardprobe_out.txt";
    const string P = "CP ";

    [MenuItem("Tools/RuleEngine/卡级解剖探针")]
    public static void Run()
    {
        if (!File.Exists(InPath))
        {
            Debug.LogError($"{P}没有输入文件：{InPath}");
            EditorApplication.Exit(1);
            return;
        }

        var pool = CardDatabase.Load();
        var byName = new Dictionary<string, CardDef>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in pool) if (!byName.ContainsKey(c.Name)) byName[c.Name] = c;

        var sb = new StringBuilder();
        sb.AppendLine($"（卡池 {pool.Count} 张）");
        int found = 0, missing = 0;

        foreach (string raw in File.ReadAllLines(InPath))
        {
            string name = raw.Trim();
            if (name.Length == 0 || name.StartsWith("#")) continue;

            CardDef c;
            if (!byName.TryGetValue(name, out c))
            {
                sb.AppendLine($"【{name}】—— ⚠️ 卡池里没有这张");
                missing++;
                continue;
            }
            found++;
            DumpCard(sb, c);
        }

        File.WriteAllText(OutPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"{P}=== 卡级解剖完：查到 {found} 张 / 没查到 {missing} 张 → {OutPath}");
        EditorApplication.Exit(0);
    }

    static void DumpCard(StringBuilder sb, CardDef c)
    {
        sb.AppendLine($"【{c.Name}】 type={c.Type}");
        sb.AppendLine($"  desc  : {c.Desc}");
        if (!string.IsNullOrEmpty(c.DescZh)) sb.AppendLine($"  descZh: {c.DescZh}");

        // ---- ① 事件层：`When <事件>, <正文>` ----
        var wts = c.WhenTriggers;
        sb.AppendLine($"  ① 事件监听（WhenTriggers）= {wts.Count} 条");
        foreach (var t in wts)
        {
            int n = t.Ops == null ? 0 : t.Ops.Count;
            sb.AppendLine($"     · 事件 «{(t.Ev == null ? "(null)" : t.Ev.ToString())}»");
            sb.AppendLine($"       正文「{t.Body}」→ op {n} 条");
            if (t.Ops != null) foreach (var o in t.Ops) sb.AppendLine("         " + EffectParseProbe.Dump(o, 9));
        }

        // ---- ② 触发层：`Rally:` / `Strike:` / `Codex:` 那一族 ----
        var texts = c.TriggerTexts;
        sb.AppendLine($"  ② 触发层（TriggerTexts）= {texts.Count} 键");
        foreach (var kv in texts)
        {
            var ops = c.TriggerOps(kv.Key);
            int n = ops == null ? 0 : ops.Count;
            sb.AppendLine($"     · {kv.Key}：正文「{kv.Value}」→ op {n} 条");
            if (ops != null) foreach (var o in ops) sb.AppendLine("         " + EffectParseProbe.Dump(o, 9));
        }

        // ---- ③ 光环 / 灵魂石 / 誓约（解析得出、各自有人触发）----
        sb.AppendLine($"  ③ 光环 {c.AuraSpecs.Count} 条 · 灵魂石 {(c.SpiritOps == null ? 0 : c.SpiritOps.Count)} 条"
                      + $" · 誓约 {(c.OathOps == null ? 0 : c.OathOps.Count)} 条");
        foreach (var a in c.AuraSpecs) sb.AppendLine($"     · 光环 {a}");
        if (c.SpiritOps != null) foreach (var o in c.SpiritOps) sb.AppendLine("     · 灵魂石 " + EffectParseProbe.Dump(o, 9));
        if (c.OathOps != null) foreach (var o in c.OathOps) sb.AppendLine("     · 誓约 " + EffectParseProbe.Dump(o, 9));

        // ---- ④ 主解析器对**整条 desc** 的意见（对照用，**不代表卡级会用它**）----
        List<string> unparsed, partial;
        var ops2 = EffectText.Parse(c.Desc, out unparsed, out partial);
        sb.AppendLine($"  ④ 主解析器：op {(ops2 == null ? 0 : ops2.Count)} 条 · 不认 {unparsed.Count} · 半懂 {partial.Count}"
                      + (EffectText.IsFullyParsed(c.Desc) ? "（IsFullyParsed=true）" : "（IsFullyParsed=false）"));
        if (unparsed.Count > 0) sb.AppendLine("      ✗ 不认: " + string.Join(" | ", unparsed));
        if (partial.Count > 0) sb.AppendLine("      ⚠ 半懂: " + string.Join(" | ", partial));
        if (ops2 != null) foreach (var o in ops2) sb.AppendLine("      " + EffectParseProbe.Dump(o, 6));

        sb.AppendLine();
    }
}
#endif
