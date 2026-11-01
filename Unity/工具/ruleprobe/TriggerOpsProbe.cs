// TriggerOpsProbe.cs — `triggerops`：**「触发数值 → 正文 op」的逐条读数口**（只读，不跑 Unity）
//   执行代理 `W-22` · 2026-11-01 · 活状态表 `B-251`（写手 `W-19` 的未尽读数）
//
// 🔴 为什么需要它：
//   `W-19` 判「`RandomAbilityChoiceOps` 在 `LogicIndex < 0` 时原样返回全部 ⇒ 与原版『为真只跑一条』不等价」
//   —— 但判不出「不猜的修法」。要不要修，取决于一个**读数**：`GOF12 Spanner`（全池唯一 `rc=1`、
//   `tn = Mob`、trigger **300**）的 `TriggerOpsByNumber(300)` / `TriggerOps("mob")` 到底是**几条**、
//   每条的 `LogicIndex` 是**几**。
//     · 只有 1 条 ⇒ `RandomAbilityChoiceOps` 的 `ops.Count < 2` 那一步就返回 ⇒ 那一档**够不到** ⇒
//       `B-251` 应按「判据本身错 / 前提不成立」核销（⛔ 不是缺陷）；
//     · ≥2 条且 `LogicIndex == -1` ⇒ 才谈得上要修，且还得先证「op ↔ abilityLogic 的对应关系」。
//
// 🔴 **它走真实装载链**：`CardDatabase.Parse(File.ReadAllText(PoolPath))` —— 与 `traits` / `dbparse` 同一条路。
//   ⛔ **不用 `Program.PoolList`**：那是 `System.Text.Json` 手读、**不经过** `Data/CardDatabase.cs`
//   ⇒ `AttachTriggerOpsByNumber` 根本没被调（探针里 `_triggerOpsByNumber` 恒 null）⇒
//   `TriggerOpsByNumber` 会静默落进「卡面关键词」退回档，量出来的**不是产品口径**。
//   （同 `A1911` / `A1959` / `B8_AV` 那一族「探针与引擎口径不同 ⇒ 静默错」。）
//
// 跑法（bash d:/4/Unity/工具/ruleprobe.sh triggerops [<id|卡名>] [scan]）：
//   triggerops                 ⇒ 默认打 `GOF12`（本批的对象）
//   triggerops GOF12           ⇒ 同上
//   triggerops <任意卡>         ⇒ 单卡逐条表
//   triggerops GOF12 scan      ⇒ 外加**全池扫**：哪些卡真的会走进 `RandomAbilityChoiceOps` 的「抽一条」那一支
//                                 （判据 = `ops.Count >= 2 ∧ TriggerRandomChoice(tn)`），逐卡打条数与 LogicIndex 分布
// ⛔ 只读：不碰基线、不碰三个指纹、不改 `check`/`play`/`ai` 的任何输出；`rc` 恒 0（判决归调度台）。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;

/// <summary>`triggerops` 入口（全局命名空间，与 `Program` / `TraitsProbe` 同层）。</summary>
static class TriggerOpsProbe
{
    public static int Run(string arg, string arg2)
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
        Console.WriteLine($"triggerops: {path}  cards={pool.Count}");

        string a = (arg ?? "").Trim();
        bool scan = string.Equals((arg2 ?? "").Trim(), "scan", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a, "scan", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(a, "scan", StringComparison.OrdinalIgnoreCase)) a = "";

        if (string.IsNullOrEmpty(a))
        {
            a = "GOF12";
            Console.WriteLine("（未指定卡 ⇒ 默认打 `GOF12 Spanner`）");
        }

        var hit = new List<CardDef>();
        foreach (var c in pool)
            if (Eq(c.Id, a) || Eq(c.Name, a)) hit.Add(c);
        if (hit.Count == 0)
        {
            Console.Error.WriteLine("!! 池内找不到这张卡（按 id 精确 / 按卡名精确）：" + a);
            return 2;
        }
        foreach (var c in hit) Detail(c);

        if (scan) ScanPool(pool);
        return 0;
    }

    static bool Eq(string x, string y) => !string.IsNullOrEmpty(x) && string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

    // ==========================================================================================
    //  单卡逐条
    // ==========================================================================================
    static void Detail(CardDef c)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {c.Id} · {c.Name} · type={c.Type} faction={c.Faction} subtype={c.Subtype} ===");
        Console.WriteLine($"  desc   = {c.Desc}");
        Console.WriteLine($"  descZh = {c.DescZh}");
        // ⚠️ `CardDef` 没有公开的 `Keywords` 属性（卡面关键词只进私有 `_keywords` / `_numericKeywords`）
        //    ⇒ 这里不打；要看关键词走 `layers` / `traits` 那两个子命令。

        // ---- ① `triggerAbilityIndices` 行（原版 `cardAbilities[i].abilityLogic[j].trigger`）----
        var rows = c.TriggerAbilityIndices;
        Console.WriteLine($"  HasTriggerAbilityIndices = {c.HasTriggerAbilityIndices}");
        if (rows == null) Console.WriteLine("     （列未知 ⇒ 判不了；⛔ 不是「明确没有」）");
        else if (rows.Length == 0) Console.WriteLine("     （明确没有 ability 行 = []）");
        else
        {
            Console.WriteLine($"     {rows.Length} 行：");
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null) { Console.WriteLine($"       [{i}] null"); continue; }
                Console.WriteLine($"       [{i}] ab={r.Ab} lg={r.Lg} trigger={r.Trigger} triggerValue={r.TriggerValue} ca={r.Ca}");
            }
        }

        // ---- ② `animTimes[].rc`（= 原版 `CardAbility.randomAbilityChoice`）----
        Console.WriteLine($"  AnimTimes = {c.AnimTimes.Count} 行；其中 RandomChoice=1 的：");
        int rcRows = 0;
        for (int i = 0; i < c.AnimTimes.Count; i++)
        {
            var t = c.AnimTimes[i];
            if (t == null || !t.RandomChoice) continue;
            rcRows++;
            Console.WriteLine($"       anim[{i}] trigger={t.Trigger} tn={t.TriggerName} rc=1 order={t.Order} anim={t.Anim}");
        }
        if (rcRows == 0) Console.WriteLine("       （0 行）");

        // ---- ③ 触发数值 ↔ 触发名 ----
        foreach (var nm in new[] { "Mob", "mob" })
        {
            int num = CardDef.AbilityTriggerNumber(nm);
            Console.WriteLine($"  AbilityTriggerNumber(\"{nm}\") = {num} · TriggerRandomChoice(\"{nm}\") = {c.TriggerRandomChoice(nm)}");
        }

        // ---- ④ `TriggerOps(<关键词>)` —— 卡面正文收下来的那一格 ----
        foreach (var kw in new[] { "mob", "Mob" })
        {
            var ops = c.TriggerOps(kw);
            DumpOps($"TriggerOps(\"{kw}\")", ops);
        }

        // ---- ⑤ `TriggerOpsByNumber(300)` —— **产品口径**（数据半已挂表）----
        Console.WriteLine($"  HasTriggerOpsByNumber = {c.HasTriggerOpsByNumber}"
                        + (c.HasTriggerOpsByNumber ? "（已挂表 ⇒ 产品口径）" : " 🔴（未挂表 ⇒ 会落进「卡面关键词」退回档）"));
        foreach (int trig in new[] { 300 })
        {
            var ops = c.TriggerOpsByNumber(trig);
            DumpOps($"TriggerOpsByNumber({trig})", ops);
        }

        // ---- ⑥ `RandomAbilityChoiceOps` 的**入口闸**逐条（只摆读数，不跑结算）----
        //   产品那道闸 = `ops == null || ops.Count < 2 || ignoreRandomTrigger || ctx == null` ⇒ 原样返回；
        //   过了之后才看 `TriggerRandomChoice(tn)`；再看 `LogicIndex < 0` ⇒ 出声 + 原样返回。
        var o300 = c.TriggerOpsByNumber(300);
        int n = o300 == null ? 0 : o300.Count;
        bool tnRC = c.TriggerRandomChoice("Mob");
        Console.WriteLine("  —— `RandomAbilityChoiceOps` 入口闸（静态读数）——");
        Console.WriteLine($"     ops.Count = {n}  ⇒ `ops.Count < 2` 那一步：{(n < 2 ? "**返回原样（够不到后面每一档）**" : "放行")}");
        Console.WriteLine($"     TriggerRandomChoice(\"Mob\") = {tnRC}  ⇒ 为假时也会原样返回");
        if (o300 != null)
        {
            int lgNeg = 0, lgPos = 0;
            var seen = new HashSet<int>();
            foreach (var op in o300)
            {
                if (op == null) continue;
                if (op.LogicIndex < 0) lgNeg++;
                else { lgPos++; seen.Add(op.LogicIndex); }
            }
            Console.WriteLine($"     LogicIndex: -1 的 {lgNeg} 条 · >=0 的 {lgPos} 条（不同 lg 值 {seen.Count} 个"
                            + (seen.Count > 0 ? "：" + string.Join(",", seen) : "") + "）");
            Console.WriteLine($"     ⇒ 若走到「按 LogicIndex 分组」那一步：{(lgNeg > 0 ? "**第一张 -1 就出声并原样返回**" : (seen.Count < 2 ? "分组数 < 2 ⇒ 原样返回" : "会真的抽一条"))}");
        }
    }

    static void DumpOps(string label, IReadOnlyList<EffectOp> ops)
    {
        if (ops == null) { Console.WriteLine($"  {label} = null（0 条）"); return; }
        Console.WriteLine($"  {label} = {ops.Count} 条：");
        for (int i = 0; i < ops.Count; i++)
        {
            var op = ops[i];
            if (op == null) { Console.WriteLine($"     [{i}] null"); continue; }
            Console.WriteLine($"     [{i}] Verb={Q(op.Verb)} Tail={Q(op.Tail)} AbilityIndex={op.AbilityIndex}"
                            + $" LogicIndex={op.LogicIndex} AbilityBlockSize={op.AbilityBlockSize}"
                            + $" AbilityMap={Q(op.AbilityMap)} ActiveAbilityRow={op.ActiveAbilityRow}"
                            + $" Amount={op.Amount} RandomPick={op.RandomPick} Payload={Q(op.Payload)}");
            Console.WriteLine($"           Source={Q(op.Source)}");
        }
    }

    static string Q(string s) => s == null ? "null" : "\"" + s + "\"";

    // ==========================================================================================
    //  全池扫 —— 真正会走进「抽一条」那一支的卡有几张
    // ==========================================================================================
    static void ScanPool(List<CardDef> pool)
    {
        Console.WriteLine();
        Console.WriteLine("=== 全池扫：`ops.Count >= 2 ∧ TriggerRandomChoice(tn)` 的卡 ===");
        Console.WriteLine("    （= **过得了** `RandomAbilityChoiceOps` 前两道闸的卡；⛔ 不是「所有 rc=1 的卡」）");

        int rcCards = 0, gateCards = 0;
        foreach (var c in pool)
        {
            // rc=1 的行（原版 `CardAbility.randomAbilityChoice`）
            var rcNames = new List<string>();
            foreach (var t in c.AnimTimes)
            {
                if (t == null || !t.RandomChoice) continue;
                string nm = t.TriggerName ?? "";
                if (nm.Length > 0 && !rcNames.Contains(nm)) rcNames.Add(nm);
            }
            if (rcNames.Count == 0) continue;
            rcCards++;
            Console.WriteLine($"  · {c.Id} {c.Name}  rc 行 = {rcNames.Count} 个触发名 [{string.Join(",", rcNames)}]  AnimTimes={c.AnimTimes.Count}");

            foreach (var nm in rcNames)
            {
                int trig = CardDef.AbilityTriggerNumber(nm);
                var ops = c.TriggerOpsByNumber(trig);
                int n = ops == null ? 0 : ops.Count;
                int lgNeg = 0, lgPos = 0;
                var seen = new HashSet<int>();
                if (ops != null)
                    foreach (var op in ops)
                    {
                        if (op == null) continue;
                        if (op.LogicIndex < 0) lgNeg++; else { lgPos++; seen.Add(op.LogicIndex); }
                    }
                string verdict = n < 2 ? "够不到（`ops.Count < 2` 返回）"
                                        : (lgNeg > 0 ? "**会出声并原样返回（LogicIndex=-1）**"
                                                     : (seen.Count < 2 ? "分组 < 2 ⇒ 原样返回" : "**真的会抽一条**"));
                Console.WriteLine($"      tn={nm} trigger={trig} ⇒ ops={n} 条 · LogicIndex -1×{lgNeg} / >=0×{lgPos}"
                                + $" · {verdict}");
                if (n >= 2) gateCards++;
            }
        }
        Console.WriteLine($"  小计：rc=1 的卡 **{rcCards}** 张 · `ops.Count>=2` 的 (卡,触发名) 对 **{gateCards}** 个");

        // ---- 附加：若「将来某张卡也拿到 rc=1」，`LogicIndex < 0` 那一支会不会被撞上 ----
        //   （只量形状，⛔ 不判对错：这里只回答「`ops.Count>=2` 的卡里，有多少张含 `LogicIndex == -1` 的 op」）
        Console.WriteLine();
        Console.WriteLine("=== 附加扫：全池「某个 trigger 上 `TriggerOpsByNumber` 条数 >= 2」的形状 ===");
        int cards2 = 0, cardsWithLgNeg = 0, pairs2 = 0;
        foreach (var c in pool)
        {
            var rows = c.TriggerAbilityIndices;
            if (rows == null || rows.Length == 0) continue;
            var trigs = new HashSet<int>();
            foreach (var r in rows) if (r != null && r.Trigger >= 0) trigs.Add(r.Trigger);
            bool any2 = false, anyNeg = false;
            var detail = new List<string>();
            foreach (var trig in trigs)
            {
                var ops = c.TriggerOpsByNumber(trig);
                if (ops == null || ops.Count < 2) continue;
                any2 = true; pairs2++;
                int lgNeg = 0;
                foreach (var op in ops) if (op != null && op.LogicIndex < 0) lgNeg++;
                if (lgNeg > 0) anyNeg = true;
                detail.Add($"trigger={trig}:{ops.Count}条(-1×{lgNeg})");
            }
            if (!any2) continue;
            cards2++;
            if (anyNeg) cardsWithLgNeg++;
            Console.WriteLine($"  · {c.Id} {c.Name}  {string.Join(" · ", detail)}");
        }
        Console.WriteLine($"  小计：条数 >= 2 的卡 **{cards2}** 张（(卡,trigger) 对 {pairs2}）· 其中含 `LogicIndex == -1` 的 **{cardsWithLgNeg}** 张");
    }
}
