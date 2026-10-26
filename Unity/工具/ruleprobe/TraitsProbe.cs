// TraitsProbe.cs — `traits`：**trait 家族 + `keyword(+0x98)`** 的只读读数口
//   （执行代理 `B8_AH` · 2026-10-26 · 账 `战斗逻辑驱动.tsv` 的 trait 家族 5 行 ＋ `IsKeyword` 行）
//
// 为什么单开一条（没有它，那 6 条账的 G3「可观察量」拿不到）：
//   · `layers` 只给 `kw`/`kwKeys`（`_keywords` 字典那一份），**看不到** `CardDef.CardTraits`
//     （原版 `RawCardScript.CardTraits` `+0x230`）与 `CardDef.KeywordId`（原版 `keyword` `+0x98`）。
//   · `play` 的 `SubtypeFilter` 走的是**卡面正文**（`a friendly Infantry or Beast`）⇒ 不是条件 277；
//     而夹具单位是 `new CardDef(...)` 现造的（不带 `keyword`）⇒ 结构上测不到真值透传。
//
// 🔴 **它走真实装载链**：`CardDatabase.Parse(File.ReadAllText(PoolPath))` —— 与 `dbparse` 同一条路。
//   ⛔ 不用 `Program.PoolList`（那是 `System.Text.Json` 手读、**不经过** `Data/CardDatabase.cs`
//   ⇒ 读不到 `CardTraits` / `KeywordId` 的接线，量出来恒「没挂」）。
//
// 四种用法（都只读；不碰基线/三个指纹，rc 恒 0 —— 读数摆出来，判决归调度台，与 `play`/`ai` 同口径）：
//   bash 工具/ruleprobe.sh traits                   全池一卡一行（紧凑）
//   bash 工具/ruleprobe.sh traits BL1               单卡详表（含 275/276/277 的产品返回值）
//   bash 工具/ruleprobe.sh traits q=armour,talent   逐卡问 `HasDefaultTrait` / `GetDefaultTraitValue`（只列命中）
//   bash 工具/ruleprobe.sh traits cond=275,276,277  逐卡跑**产品代码** `ConditionEval.MeetsCard`，列出为真的卡
//
// ⚠️ `cond=` 那一档的 `ctx` 传 `null`（与 `ConditionEval.MeetsCard` 的 doc 一致：这三格只读「卡本身」）。
// ⚠️ 判据侧 / 我们侧**分两栏打**：本工具只摆读数，⛔ 不代替判决、也⛔ 不拿我们自己的常量当期望值。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RuleEngine;

/// <summary>`traits` 入口（全局命名空间，与 `Program` / `DbParse` 同层）。</summary>
static class TraitsProbe
{
    /// <summary>`Keyword` 枚举的 9 档（`il2cpp_out/dump.cs:45899-45912`）—— 只用于**打印**枚举名，
    /// ⛔ 不参与任何判定（判定在 `CardDef.IsKeyword` 里，值来自卡表）。</summary>
    static readonly (int V, string N)[] KeywordNames =
    {
        (0, "none"), (5, "Infantry"), (10, "Vehicle"), (15, "Monster"), (20, "Building"),
        (25, "Battlesuit"), (30, "Drone"), (35, "Beast"), (40, "Daemon"),
    };

    static string KeywordName(int v)
    {
        foreach (var k in KeywordNames) if (k.V == v) return k.N;
        return "?";
    }

    public static int Run(string arg)
    {
        string path = Program.PoolPath;
        if (!File.Exists(path)) { Console.Error.WriteLine("!! 找不到卡池：" + path); return 2; }
        if (!string.IsNullOrEmpty(Program.PoolOverride))
            Console.WriteLine("⚠️ 卡池被 `WFPROBE_POOL` 覆盖 ⇒ 下面读数只对这份输入成立：" + path);

        List<CardDef> pool;
        try { pool = CardDatabase.Parse(File.ReadAllText(path)); }
        catch (Exception e)
        {
            Console.Error.WriteLine("!! `CardDatabase.Parse` 抛异常：" + e.GetType().Name + ": " + e.Message);
            return 1;
        }

        string a = (arg ?? "").Trim();

        if (a.StartsWith("q=", StringComparison.OrdinalIgnoreCase)) return Query(pool, a.Substring(2));
        if (a.StartsWith("cond=", StringComparison.OrdinalIgnoreCase)) return Conditions(pool, a.Substring(5));

        Console.WriteLine($"traits: {path}  cards={pool.Count}");
        int traitsKnown = pool.Count(c => c.CardTraitsKnown);
        int kwKnown = pool.Count(c => c.KeywordId >= 0);
        Console.WriteLine($"  透传：`CardTraits` 已挂 **{traitsKnown}/{pool.Count}** 张 · "
                        + $"`keyword` 已挂 **{kwKnown}/{pool.Count}** 张（原版 `RawCardScript +0x230` / `+0x98`）");
        if (traitsKnown == 0 || kwKnown == 0)
            Console.WriteLine("  🔴 有列**没挂上** ⇒ 那几格查询会返回「判不了」+ why（⛔ 不是「原版说没有」）");

        var hit = string.IsNullOrEmpty(a)
            ? null
            : pool.Where(c => Eq(c.Id, a) || Eq(c.Name, a)).ToList();

        if (hit != null && hit.Count == 0)
        {
            Console.Error.WriteLine("!! 池内找不到这张卡（按 id 精确 / 按卡名精确）：" + a);
            return 2;
        }

        if (hit != null)
        {
            foreach (var c in hit) Detail(c);
            return 0;
        }

        // 全池紧凑一行一卡：`id · name · CountTraits · CardTraits 原文 · keyword 枚举 · Subtype · 护甲`
        //   ⚠️ 不排序（卡表原序）—— 与 `scan` / `layers` 同一口径，便于逐行 diff。
        foreach (var c in pool)
        {
            int n = c.CountTraits(out string why);
            string traits = c.CardTraitsKnown
                ? "[" + string.Join(", ", c.CardTraits.Select(t => t.Raw ?? t.Name)) + "]"
                : "(未挂)";
            string kw = c.KeywordId < 0 ? "(未挂)" : c.KeywordId + " " + KeywordName(c.KeywordId);
            Console.WriteLine($"{c.Id,-34} {Quote(c.Name),-32} n={(n < 0 ? "?" : n.ToString()),-2} "
                            + $"traits={traits,-40} kw={kw,-14} subtype={c.Subtype,-10} "
                            + $"armour(Pool/Keywords)={c.ArmourFromPool}/{c.ArmourFromKeywords}"
                            + (why != null ? "  ⚠️ " + why : ""));
        }
        return 0;
    }

    /// <summary>单卡详表 —— 四个查询逐条打，外加条件 275/276/277 的**产品代码返回值**。</summary>
    static void Detail(CardDef c)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {c.Id} · {c.Name} · type={c.Type} faction={c.Faction} ===");
        Console.WriteLine($"  CountTraits           = {Show(c.CountTraits(out _))}");
        Console.WriteLine($"  CardTraits            = {Show(c.CardTraits)}");
        foreach (var t in c.CardTraits ?? (IReadOnlyList<CardDef.CardTraitEntry>)Array.Empty<CardDef.CardTraitEntry>())
            Console.WriteLine($"     · `{t.Raw}` ⇒ name=`{t.Name}` value={t.Value}");
        foreach (var q in new[] { "armour", "talent", "ephemeral", "codex", "strike", "stomp", "pack", "longrange", "frenzied", "destroyer", "rage", "penitence" })
        {
            bool has = c.HasDefaultTrait(q, out string why);
            if (!has && why == null) continue;             // 「明确没有」不刷屏；「判不了」要看得见
            Console.WriteLine($"  HasDefaultTrait({q,-10}) = {has}"
                            + $" · GetDefaultTraitValue = {c.GetDefaultTraitValue(q, out _)}"
                            + (why != null ? "  ⚠️ " + why : ""));
        }
        Console.WriteLine($"  KeywordId(+0x98)      = {(c.KeywordId < 0 ? "(未挂)" : c.KeywordId + " " + KeywordName(c.KeywordId))}");
        Console.WriteLine($"  Subtype(卡面橙字·显示用) = `{c.Subtype}`  ← ⛔ 不是 IsKeyword 的判据");
        foreach (var k in KeywordNames)
            Console.WriteLine($"     IsKeyword({k.V,2} {k.N,-10}) = {Show(c.IsKeyword(k.V, out _))}");
        Console.WriteLine($"  ArmourFromPool / FromKeywords = {c.ArmourFromPool} / {c.ArmourFromKeywords}");
        Console.WriteLine($"  Keywords(_keywords 字典) = {c.Keywords.Count} 条："
                        + string.Join(" · ", c.Keywords.Select(kv => kv.Key + ":" + kv.Value)));
        // 🔴 275/276/277 走**产品代码**（`ConditionEval.MeetsCard`，与结算同一条路）。
        foreach (int cond in new[] { 270, 275, 276, 277 })
            Console.WriteLine($"  ConditionEval.MeetsCard({cond}) = {Show(ConditionEval.MeetsCard(null, c, 0, cond))}");
        // 🔴 卡级 `role="cardtarget"` 条件**有没有落到 op 上**（原版 `RawCardScript.cardTargetCriteria`，
        //   唯一消费者 `RawCardScript__IsValidTarget.c:8-44`；闸 = `CardDef.Targeted == 1`）。
        //   ⚠️ 必须**带上 `card:`** 解析（不带 card 的那条重载跑不到 `AssignCardTargetFallback`）——
        //   这也正是 `play` 那条路今天看不见它的原因之一（见报告「顺手发现」）。
        var ops = EffectText.Parse(c.Desc, out _, out _, card: c);
        Console.WriteLine($"  CardTargetFallback：Targeted={c.Targeted} · op={ops.Count} 条");
        for (int i = 0; i < ops.Count && i < 4; i++)
            Console.WriteLine($"     op[{i}] `{ops[i].Verb}` TargetCondition = "
                            + (ops[i].TargetCondition == null
                                ? "null"
                                : "cond=" + ops[i].TargetCondition.Condition + " ex=" + ops[i].TargetCondition.Exclude
                                  + " role=`" + ops[i].TargetCondition.Role + "`")
                            + " · gap=`" + (ops[i].TargetConditionGap ?? "") + "`");
    }

    /// <summary>`q=trait[,trait]` —— 逐卡问 `HasDefaultTrait` / `GetDefaultTraitValue`，只列**命中**的卡。</summary>
    static int Query(List<CardDef> pool, string spec)
    {
        var wants = (spec ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        if (wants.Length == 0) { Console.Error.WriteLine("!! `q=` 后面要写 trait 名（逗号分隔）"); return 2; }
        foreach (var w in wants)
        {
            int hits = 0, unknown = 0;
            var ids = new List<string>();
            foreach (var c in pool)
            {
                bool has = c.HasDefaultTrait(w, out string why);
                if (why != null) unknown++;
                if (!has) continue;
                hits++;
                if (ids.Count < 40) ids.Add(c.Id + "(" + c.GetDefaultTraitValue(w, out _) + ")");
            }
            Console.WriteLine($"[{w}] HasDefaultTrait = 真 **{hits}** 张 / {pool.Count}（判不了 {unknown} 张）"
                            + (ids.Count > 0 ? "：" + string.Join(" ", ids) : ""));
        }
        return 0;
    }

    /// <summary>`cond=275,276,277` —— 逐卡跑**产品代码** `ConditionEval.MeetsCard`，列出为真的卡。
    /// ⚠️ `ctx` 传 `null`：这三格只读「卡本身」（`FilterMethods__MeetsConditionCriteria.c:183-216`）。</summary>
    static int Conditions(List<CardDef> pool, string spec)
    {
        var wants = new List<int>();
        foreach (var p in (spec ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(p.Trim(), out int n)) wants.Add(n);
        if (wants.Count == 0) { Console.Error.WriteLine("!! `cond=` 后面要写条件号（逗号分隔）"); return 2; }

        foreach (int cond in wants)
        {
            int t = 0, f = 0, unknown = 0;
            var trues = new List<string>();
            var bySubtype = new Dictionary<string, int>();
            foreach (var c in pool)
            {
                bool? r = ConditionEval.MeetsCard(null, c, 0, cond);
                if (!r.HasValue) { unknown++; continue; }
                if (r.Value)
                {
                    t++;
                    if (trues.Count < 60) trues.Add(c.Id);
                    string st = string.IsNullOrEmpty(c.Subtype) ? "(空)" : c.Subtype;
                    bySubtype[st] = bySubtype.GetValueOrDefault(st) + 1;
                }
                else f++;
            }
            Console.WriteLine($"\n=== cond {cond} · ConditionEval.MeetsCard（产品代码）· 池 {pool.Count} 张 ===");
            Console.WriteLine($"  真 **{t}** · 假 {f} · 判不了 {unknown}");
            if (bySubtype.Count > 0)
                Console.WriteLine("  为真的卡按 **Subtype（卡面橙字）** 分组："
                                + string.Join(" · ", bySubtype.OrderByDescending(kv => kv.Value)
                                                           .Select(kv => kv.Key + "×" + kv.Value)));
            if (trues.Count > 0) Console.WriteLine("  为真（前 60）：" + string.Join(" ", trues));
            // 🔴 **差异列**：把「旧实现等价式」摊出来（旧实现 = `SubtypeIs`，判据 → 改前
            //   `ConditionEval.cs:427/433/439`；⛔ 这只是**读数对照**，判决仍看 `keyword(+0x98)` 的真值）。
            var legacy = new Dictionary<int, string[]>
            {
                [275] = new[] { "Vehicle" },
                [276] = new[] { "Battlesuit", "Vehicle" },
                [277] = new[] { "Infantry", "Beast" },
            };
            if (legacy.TryGetValue(cond, out var want))
            {
                var diff = new List<string>();
                foreach (var c in pool)
                {
                    bool old = want.Any(w => Eq(c.Subtype, w));
                    bool? now = ConditionEval.MeetsCard(null, c, 0, cond);
                    if (cond == 275)
                    {
                        // 275 前半是 `Type == "hero"`（`MeetsCard` 已含），旧实现同样含它 ⇒ 直接比。
                        old = old || Eq(c.Type, "hero");
                    }
                    if (now.HasValue && now.Value != old)
                        diff.Add($"{c.Id}(subtype={c.Subtype},kw={c.KeywordId}{(now.Value ? "→真" : "→假")})");
                }
                Console.WriteLine($"  ⚠️ 与旧 `Subtype` 等价式**答案不同**的卡 **{diff.Count}** 张"
                                + (diff.Count > 0 ? "：" + string.Join(" ", diff) : "（两侧同）"));
            }
        }
        return 0;
    }

    static bool Eq(string a, string b)
        => a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    static string Show(bool b) => b ? "真" : "假";
    static string Show(bool? b) => b.HasValue ? (b.Value ? "真" : "假") : "null（判不了）";
    static string Show(int n) => n < 0 ? "null（判不了）" : n.ToString();
    static string Show(IReadOnlyList<CardDef.CardTraitEntry> list) => list == null ? "null（未挂）" : "[" + list.Count + " 条]";
    static string Quote(string s) => string.IsNullOrEmpty(s) ? "\"\"" : "\"" + s + "\"";
}
