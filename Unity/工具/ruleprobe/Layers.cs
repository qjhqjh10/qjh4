using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RuleEngine;

/// <summary>🔴 **2026-10-14（第二十八会话 · 写手 `W16` · 台账 `A1877①`）：`AuraSpecs` / `keywords` 层的 dump 口。**
///
/// **病灶**（`资料/普查产出_第二十八会话/A1759_逐条对账.md` §口径 与 §查不清）：
///   `A1759` 那条「原版 `cardAbilities[].abilityLogic[]` 逐条对账」的**我们面**走的是
///   `EffectText.Parse(desc)`（= `scan` 的 ops 列）。而**光环（`CardDef.AuraSpecs` → `Core/Aura.cs`）
///   与 `keywords` 驱动的机制是【另一层】，结构上不进那个口** ⇒ 报成「我们缺」的那 24 条
///   （`WhileInPlay` 7 + `WhileInPlayVariable` 17）**可能是口径缺口、不是实现缺口**；
///   `bt=3 addTrait` 那一群同理（`AuraSpec.Payload` 就是 `Flank` / `Vanguard` 这种）。
///   ⚠️ 引擎自己**早就知道这两层是分开的**：`EffectText.WillRunOps`（`EffectText.cs:1872`）
///   把七个来源（`WhenTriggers` / `TriggerTexts` / `SpiritOps` / `OathOps` / `FaithOps` /
///   `AttackedOps` / `AuraSpecs`）汇成「**执行层**」，而 `scan` 打的是**主解析器**那一层。
///   ⇒ 本入口就是把**那两层**摊开，让对账口看得见（⛔ 不新增任何解析文法，只读引擎已经收下的东西）。
///
/// **形状为什么选【新子命令 + JSONL】而不是「`scan` 多打几列」**（简报允许两者，这里写清取舍）：
///   ① **粒度不同**：`scan` 是**一行一卡、列里放 op**；而光环/关键词是**卡级**的（一张卡 N 条光环），
///      硬塞进 op 流会变成伪 op —— 哨兵 ⑧ 的 `phantom`（按 `;` 切出来的假 op）会**成倍上涨**，
///      把一条**有声**的读数变成噪声（那条哨兵正是靠「真 op 必带 `/{T:`」立住的）。
///   ② **分隔符不安全**：`scan` 的列以 `|` 分隔，而 `Payload` / `Source` 是**卡面原文**，
///      可以含 `|` / `;` / `/` —— 再加列就得赌「原文里没有这个字符」（本仓已经为这个赌过两次）。
///   ③ **对象更合适**：`{"id":…,"auras":[{…}],"kw":[…]}` 对消费方（对账脚本是 Python）是**零歧义**的，
///      `json.loads` 一行一个对象，不需要再写一个转义层。
///   ④ **零回归面**：不动 `scan`/`check`/基线/三个指纹 ⇒ **别的写手的仪器不被打断**。
///   ⚠️ 代价：`A1759` 的脚本要多读一个文件、按 `id` join 一次。对账脚本在 `_tmp_view`（⛔ 不是本件范围）。
///
/// **字段怎么来的（全部只读引擎公开面）**：
///   · `kw`     = `Program.PoolRawKeywords`（与 `Pool` **同序**的 `keywords` 原文条目）——
///                `CardDef.Keywords` 只有**规范化之后的键**，看不到 `"Sacrifice: Gain +1 Attack"` 这种整条；
///   · `kwKeys` = `CardDef.Keywords`（键 → 值，`KeywordTable.Parse` 的产物）；
///   · `auras`  = `CardDef.AuraSpecs` **逐字段**（列名与顺序写死 → <see cref="AuraCols"/>，
///                并配一条机器级守卫 <see cref="AuraColsDrift"/>：加了新字段没跟进 = 出声）；
///   · `willrun`= `EffectText.WillRunOps(c)`（**执行层**）＋ `AmountMax` / `Tail`（`A1877②` 同批补的两样）。
///
/// ⚠️ **列名与顺序写死**（照 `Program.TargetCols` 那条既有做法）：纯反射自动列会让**声明顺序**决定
///   输出 ⇒ 基线里那一大块会无意义地全变（把「红了不用管」训练成习惯）。
/// </summary>
class Layers
{
    /// <summary>`AuraSpec` 要打哪几个字段（顺序 = JSON 里的顺序）。
    /// 🔴 **加字段的地方在 `RuleEngine/Core/Aura.cs` 的 `AuraSpec`** —— 那边加了、这里没跟上
    ///   ⇒ <see cref="AuraColsDrift"/> 会出声（⛔ 别只改一边；形状照 `Program.TargetColsDrift`）。</summary>
    public static readonly string[] AuraCols = {
        "Head", "Payload", "Duration", "CostLess",
        "Adjacent", "Enemy", "ExcludeSelf", "Self", "RemnantStay",
        "Filter", "Source",
    };

    /// <summary>`AuraSpec` 里【类里有、清单没有】或【清单有、类里没有】的字段名（空 = 一致）。</summary>
    public static List<string> AuraColsDrift()
    {
        var drift = new List<string>();
        var ty = typeof(AuraSpec);
        var inType = new List<string>();
        foreach (var f in ty.GetFields(BindingFlags.Public | BindingFlags.Instance)) inType.Add(f.Name);
        foreach (var p in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanRead && p.GetIndexParameters().Length == 0) inType.Add(p.Name);
        foreach (var n in inType) if (Array.IndexOf(AuraCols, n) < 0) drift.Add("类里有、清单没有: " + n);
        foreach (var n in AuraCols) if (!inType.Contains(n)) drift.Add("清单里有、类里没有: " + n);
        return drift;
    }

    static string AuraValue(AuraSpec a, string name)
    {
        var ty = typeof(AuraSpec);
        var mi = (MemberInfo)ty.GetField(name) ?? ty.GetProperty(name);
        if (mi == null) return null;                       // 清单过期（`AuraColsDrift` 会报出来）
        object v = mi is FieldInfo f ? f.GetValue(a) : ((PropertyInfo)mi).GetValue(a);
        if (v == null) return null;
        if (v is bool b) return b ? "1" : "0";
        return v.ToString();
    }

    /// <summary>JSON 字符串转义（本入口唯一的一处 —— ⛔ 别在别处再写一份）。
    /// ⚠️ **必须转义**：这里和 `scan` 的取舍**相反**（那边「原样打、不转义」是为了让 dump 的值
    ///   与引擎字段**逐字相同**，值本身是文本列）；这里是 **JSON** —— 不转义就是**坏 JSON**，
    ///   消费方 `json.loads` 会直接抛（比静默拿错值好，但没必要让它抛）。
    ///   转义**不改变语义**：`json.loads` 之后拿到的字符串与引擎字段**逐字相同**。</summary>
    static string J(string s)
    {
        if (s == null) return "null";
        var sb = new StringBuilder(s.Length + 8);
        sb.Append('"');
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    else sb.Append(ch);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    /// <summary>`layers <out.jsonl>`：一行一卡的 JSON（见类注）。返回失败条数（`0` = 全过）。
    /// ⚠️ 出问题**一律出声**（本仓红线「不许静默失败」）：读不到就写 `null` 并在 stderr 上说出来。</summary>
    public static int Run(string outPath)
    {
        Program.LoadPool();
        var pool = Program.PoolList;
        var rawKw = Program.PoolRawKeywords;
        int fail = 0;

        var drift = AuraColsDrift();
        if (drift.Count > 0)
        {
            fail++;
            Console.Error.WriteLine("!! [光环列] `AuraSpec` 与 `Layers.AuraCols` **对不上**（" + drift.Count + " 处）："
                                    + string.Join(" · ", drift)
                                    + " ⇒ 对不上的字段**在本 dump 里看不见**。修法：把 `Layers.AuraCols` 补齐/删旧。");
        }
        if (rawKw == null || rawKw.Count != pool.Count)
        {
            fail++;
            Console.Error.WriteLine("!! [keywords 原文] `Program.PoolRawKeywords` 与 `Pool` **不同长**（"
                                    + (rawKw == null ? -1 : rawKw.Count) + " vs " + pool.Count
                                    + "）⇒ `kw` 那一列会**错位**（本件如实报出来，⛔ 不静默打半份）。");
        }
        if (pool.Count > 0 && pool.All(c => c.AuraSpecs == null || c.AuraSpecs.Count == 0))
        {
            // ⚠️ 全池一条光环都没有 ⇒ 极可能是**探针造卡时没走引擎那条链**（`CardDef` 构造函数里
            //    `CollectTriggerOps(keywords)` → `AddAura`），而不是「工程里真的没有光环」。
            //    判据见 `RuleEngineTest` 那侧的同一族断言（`AuraSpecs` 2026-09-16 起就收进了 `WillRunOps`）。
            fail++;
            Console.Error.WriteLine("!! [光环层] 全池 `AuraSpecs` **一条都没有** —— 要么工程里真没有，"
                                    + "要么探针造卡时没走 `CardDef` 的收集链。⛔ 别静默当成「没有光环」。");
        }

        var sb = new StringBuilder();
        int nAura = 0, nWr = 0, nKw = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            sb.Append("{\"id\":").Append(J(c.Id))
              .Append(",\"name\":").Append(J(c.Name))
              .Append(",\"type\":").Append(J(c.Type))
              .Append(",\"subtype\":").Append(J(c.Subtype));

            // ---- keywords（原文 + 规范化键）----
            var raws = (rawKw != null && i < rawKw.Count) ? rawKw[i] : null;
            sb.Append(",\"kw\":[");
            if (raws != null)
                for (int k = 0; k < raws.Count; k++)
                {
                    if (k > 0) sb.Append(',');
                    sb.Append(J(raws[k]));
                }
            sb.Append("],\"kwKeys\":{");
            var keys = c.Keywords;
            if (keys != null)
            {
                bool first = true;
                foreach (var kv in keys.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(J(kv.Key)).Append(':').Append(kv.Value);
                }
            }
            sb.Append('}');

            // ---- 光环（`AuraSpecs`）----
            sb.Append(",\"auras\":[");
            var auras = c.AuraSpecs;
            if (auras != null)
                for (int k = 0; k < auras.Count; k++)
                {
                    if (k > 0) sb.Append(',');
                    sb.Append('{');
                    for (int j = 0; j < AuraCols.Length; j++)
                    {
                        if (j > 0) sb.Append(',');
                        sb.Append(J(AuraCols[j])).Append(':').Append(J(AuraValue(auras[k], AuraCols[j])));
                    }
                    sb.Append('}');
                    nAura++;
                }
            sb.Append(']');

            // ---- 通道计数（让对账口看得见「哪一层有东西」）----
            sb.Append(",\"ch\":{")
              .Append("\"trig\":").Append(c.TriggerTexts == null ? 0 : c.TriggerTexts.Count)
              .Append(",\"when\":").Append(c.WhenTriggers == null ? 0 : c.WhenTriggers.Count)
              .Append(",\"spirit\":").Append(c.SpiritOps == null ? 0 : c.SpiritOps.Count)
              .Append(",\"oath\":").Append(c.OathOps == null ? 0 : c.OathOps.Count)
              .Append(",\"faith\":").Append(c.FaithOps == null ? 0 : c.FaithOps.Count)
              .Append(",\"attacked\":").Append(c.AttackedOps == null ? 0 : c.AttackedOps.Count)
              .Append('}');

            // ---- 执行层 ops（`WillRunOps`）＋ 本批补的 `AmountMax` / `Tail` ----
            sb.Append(",\"willrun\":[");
            var ops = EffectText.WillRunOps(c);
            for (int k = 0; k < ops.Count; k++)
            {
                var o = ops[k];
                if (k > 0) sb.Append(',');
                sb.Append("{\"v\":").Append(J(o.Verb))
                  .Append(",\"n\":").Append(o.Amount)
                  .Append(",\"n2\":").Append(o.AmountMax)
                  .Append(",\"p\":").Append(J(o.Payload))
                  .Append(",\"t\":").Append(J(o.Target == null ? null : o.Target.Raw))
                  .Append(",\"tail\":").Append(J(o.Tail))
                  .Append(",\"src\":").Append(J(o.Source))
                  .Append('}');
                nWr++;
            }
            sb.Append("]}").Append('\n');
            nKw += raws == null ? 0 : raws.Count;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine("wrote " + outPath + " cards=" + pool.Count + " auras=" + nAura
                          + " willrunOps=" + nWr + " rawKeywords=" + nKw);
        return fail;
    }
}
