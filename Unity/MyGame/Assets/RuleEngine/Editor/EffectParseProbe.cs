// EffectParseProbe.cs — **逐句解析探针**（改 `EffectText` 之前/之后的量尺）
//
// 为什么要有它：改解析层的判据时，「这句话现在还认不认、解出来长什么样」要**一条一条量**。
// 靠读代码推是推不出来的（2026-09-13 A4 那个 `|` 优先级 bug 就是拿探针量出来的，不是看出来的）。
//
// 用法（CLI）：
//   1. 把要量的句子写进 `d:/4/_tmp_view/probe_in.txt`（一行一句，`#` 开头是注释）
//   2. unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//        -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//        -executeMethod EffectParseProbe.Run -logFile -
//   3. 读 `d:/4/_tmp_view/probe_out.txt`
//
// ⚠️ **它只读文件，不碰工程** —— 可以放心反复跑。
// ⚠️ 一次 Unity 只能跑一个实例，所以**别在别的 Unity 批处理跑着的时候跑它**。
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static class EffectParseProbe
{
    const string InPath = "d:/4/_tmp_view/probe_in.txt";
    const string OutPath = "d:/4/_tmp_view/probe_out.txt";

    [MenuItem("Tools/RuleEngine/逐句解析探针")]
    public static void Run()
    {
        if (!System.IO.File.Exists(InPath))
        {
            Debug.LogError($"EP 没有输入文件：{InPath}");
            EditorApplication.Exit(1);
            return;
        }

        var sb = new StringBuilder();
        int unknown = 0, partial = 0, ok = 0;

        // 🔴 **必须先加载卡池**（2026-09-16）：有一族目标的判据是
        //    「**池里真有一张卡叫这个名字**」（`CreatePool.MatchCardName`，见
        //    `CardCriteria.Name` / `EffectText.TailCardName`）—— 卡池没加载时那条路一律不生效，
        //    探针量出来的就不是引擎实际的行为（会少认一批目标）。
        //    ⚠️ 这条**只影响「按卡名指目标」那一族**，别的句子加载与否都一样。
        int poolN = RuleEngine.CardDatabase.Load().Count;
        sb.AppendLine($"（卡池已加载 {poolN} 张 —— 「按卡名指目标」那条路要靠它）");

        foreach (string raw in System.IO.File.ReadAllLines(InPath))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            sb.AppendLine($"【{line}】");
            // ① 卡级：整条 desc 的判定（`Parse` 会自己切句 + 回填目标）
            var ops = EffectText.Parse(line, out var unparsed, out var partialSegs);
            sb.AppendLine($"  卡级 → op {ops.Count} 条 · 不认识 {unparsed.Count} · 半懂 {partialSegs.Count}");
            if (unparsed.Count > 0) sb.AppendLine("        ✗ 不认识: " + string.Join(" | ", unparsed));
            if (partialSegs.Count > 0) sb.AppendLine("        ⚠ 半懂: " + string.Join(" | ", partialSegs));

            // ② 逐句：每一段单独送 ParseSegment，看它落在哪一类、切成了什么
            foreach (string seg in EffectText.Split(line))
            {
                var r = EffectText.ParseSegment(seg);
                string kind;
                switch (r.Kind)
                {
                    case EffectText.SegKind.Ok: kind = "认了"; ok++; break;
                    case EffectText.SegKind.Partial: kind = "半懂"; partial++; break;
                    case EffectText.SegKind.KeywordOnly: kind = "纯关键词"; ok++; break;
                    default: kind = "不认"; unknown++; break;
                }
                sb.AppendLine($"   · [{kind}] 「{seg}」");
                if (r.Ops == null) continue;
                foreach (var o in r.Ops) sb.AppendLine("        " + Dump(o, 8));
            }
            sb.AppendLine();
        }

        System.IO.File.WriteAllText(OutPath, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log($"EP === 探针完：{ok} 段认了 / {partial} 半懂 / {unknown} 不认 → {OutPath}");
        EditorApplication.Exit(0);
    }

    /// <summary>把一条 op 摊成一行。`indent` 用于 `Tail` 递归。
    /// ⚠️ **`internal` 是给 `CardProbe` 复用的**（2026-09-16）—— op 的摊法**只此一处**，
    ///    另写一份迟早和这份不一致（本工程的规矩：判据与格式都别写第二份）。</summary>
    internal static string Dump(EffectOp o, int indent)
    {
        if (o == null) return "(null op)";
        var sb = new StringBuilder();
        sb.Append($"{o.Verb}");
        if (o.Amount != 0 || o.AmountMax != 0) sb.Append($" n={o.Amount}" + (o.AmountMax != 0 ? $"-{o.AmountMax}" : ""));
        // 🆕 2026-09-16：`…, N times` 的重复次数（见 `EffectOp.RepeatTimes`）——
        //   探针必须看得见它，不然「重复 8 次」和「打 8 个目标」在输出里长得一样。
        if (o.RepeatTimes > 1) sb.Append($" ×{o.RepeatTimes}次");
        if (!string.IsNullOrEmpty(o.Payload)) sb.Append($" 载荷「{o.Payload}」");
        if (!string.IsNullOrEmpty(o.Duration)) sb.Append($" 时长={o.Duration}");
        if (!string.IsNullOrEmpty(o.CostKind) || o.Cost != 0) sb.Append($" 付费={o.Cost}{o.CostKind}");
        if (o.CostSetTo != 0) sb.Append($" 设为{o.CostSetTo}费");
        if (!string.IsNullOrEmpty(o.AmountRef)) sb.Append($" 数值取自={o.AmountRef}");
        if (!string.IsNullOrEmpty(o.DeployFrom) && o.DeployFrom != "pool") sb.Append($" 来源={o.DeployFrom}");
        if (o.CostMin != 0 || o.CostMax != 0) sb.Append($" 费用区间={o.CostMin}..{o.CostMax}");
        if (o.Random) sb.Append(" 随机");
        if (!string.IsNullOrEmpty(o.Dest)) sb.Append($" 去处={o.Dest}");
        if (o.Instead) sb.Append(" 【替换】");
        if (!string.IsNullOrEmpty(o.ConditionKind)) sb.Append($" 条件={o.ConditionKind}");
        else if (!string.IsNullOrEmpty(o.Condition)) sb.Append($" 条件=⚠「{o.Condition}」判不了");
        if (!string.IsNullOrEmpty(o.CountRef)) sb.Append($" 计数={o.CountRef}" + (o.PerCount > 0 ? $"(每条+{o.PerCount})" : ""));
        sb.Append($" 目标[{Target(o.Target)}]");
        // 🆕 2026-09-15：「条件换数值」`…, or <N> if <条件>`（`EffectOp.AltAmount`）——
        //    探针必须看得见它，不然「接没接上」只能靠读代码猜。
        if (o.AltAmount != 0 || !string.IsNullOrEmpty(o.AltCondition))
        {
            sb.Append($" ⇒条件成立时换成 {o.AltAmount}");
            if (!string.IsNullOrEmpty(o.AltPayload)) sb.Append($"（或载荷「{o.AltPayload}」）");
            sb.Append($"｜条件=«{o.AltCondition}»"
                      + (string.IsNullOrEmpty(o.AltConditionKind) ? " ⚠**归不出名、结算层判不了**"
                                                                  : "→" + o.AltConditionKind));
        }
        if (o.Target2 != null) sb.Append($" 第二目标[{Target(o.Target2)}]");
        if (!string.IsNullOrEmpty(o.Tail)) sb.Append($"\n{new string(' ', indent)}↳尾句 「{o.Tail}」");
        return sb.ToString();
    }

    static string Target(EffectTargetSpec t)
    {
        if (t == null) return "（没写）";
        var sb = new StringBuilder();
        sb.Append($"{t.Side}/{t.Kind}");
        if (t.Count != 0) sb.Append($" ×{t.Count}");
        if (t.Adjacent) sb.Append($" 相邻(锚={t.Anchor}{(t.AnchorInSet ? "+自身" : "")}{(t.AdjacentAll ? " 全要" : "")}{(t.AdjacentFailed ? " 认不出" : "")})");
        if (!string.IsNullOrEmpty(t.KeywordFilter)) sb.Append($" 关键词筛={t.KeywordFilter}");
        // 🆕 2026-09-16：**取反的关键词筛**（`all **other** enemies`，见 `EffectTargetSpec.NotKeyword`）
        if (!string.IsNullOrEmpty(t.NotKeyword)) sb.Append($" 排除带={t.NotKeyword}");
        // 🆕 2026-09-16：**`… in play and in hand` 的手牌那半**（见 `EffectTargetSpec.AlsoHand`）
        if (t.AlsoHand) sb.Append(" 也含手牌");
        if (t.HandOnly) sb.Append(" 只在手牌");
        if (!string.IsNullOrEmpty(t.SubtypeFilter)) sb.Append($" 兵种筛={t.SubtypeFilter}");
        if (!string.IsNullOrEmpty(t.NameFilter)) sb.Append($" 名牌={t.NameFilter}");
        if (!string.IsNullOrEmpty(t.PickMost)) sb.Append($" 挑={t.PickMost}");
        if (t.DamagedOnly) sb.Append(" 只要已受伤");
        if (t.PrayedOnly) sb.Append(" 只要正在祈祷");
        if (t.Deployed) sb.Append(" 已部署");
        if (t.Each) sb.Append(" 每个");
        if (t.Auto) sb.Append(" 自动");
        if (t.Subjectless) sb.Append(" 没写主语");
        if (t.Random) sb.Append(" 随机");
        if (t.KindUnfilterable) sb.Append(" ⚠兵种筛不了");
        if (!string.IsNullOrEmpty(t.Raw)) sb.Append($" 「{t.Raw}」");
        return sb.ToString();
    }
}
#endif
