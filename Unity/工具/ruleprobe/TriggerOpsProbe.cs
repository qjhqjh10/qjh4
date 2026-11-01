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
//   🆕 triggerops trigger0      ⇒ **`trigger 0 (ThisCardPlayed)` 的桶空/非空逐卡读数**（`B115_S2`，2026-11-01）：
//                                 数据半挂载汇总 + 逐 trigger 桶统计 + 逐卡「ca 兜底进不进得去」+ 行级
//                                 「这一行的 ab 在桶里有没有 op」+ `AM54`/`GOF93`/`GOF104` 逐行明细。
//                                 🔴 它回答的是 `B-211`「层②」那一格：桶非空 ⇒ `FireTriggerRowsByChosenAbility` 不会被叫到。
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

        // 🆕 `trigger0`（`B115_S2`，2026-11-01 · 活状态表 `B-211` 的「层②」读数）：
        //   「打出时逐 trigger 0 能力派发」这条路上唯一还靠推理的一格 —— 逐卡的 `TriggerOpsByNumber(0)` 桶空不空。
        if (string.Equals(a, "trigger0", StringComparison.OrdinalIgnoreCase))
        {
            Trigger0Report(pool);
            return 0;
        }

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

    static string CountOf(IReadOnlyList<EffectOp> ops) => ops == null ? "null" : ops.Count.ToString();

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

    // ==========================================================================================
    //  `trigger0` 模式 —— `B115_S2`（2026-11-01 · 活状态表 `B-211` 的「层②」读数）
    //
    //  要回答的唯一一件事：**「卡被打出」那一刻，如果真把 `FireTriggerByNumber(…, 0, …)` 接上，
    //  `FireTriggerRowsByChosenAbility` 会不会被叫到？** 它的闸是
    //  `TriggerOpsByNumber(0) == null || Count == 0`（`EffectResolver.cs:21078`）⇒
    //  只要某张卡的 **trigger 0 桶非空**，它的 `ca` 行（380/360/320…）就**永远轮不到**兜底。
    //
    //  三个读数：
    //   ① `TriggerOpsByNumberSummary()`（数据半挂载情况，产品口径）；
    //   ② 逐 trigger：多少张卡「列已知且桶非空 / 桶空 / 列未知（退回档，判不了）」；
    //   ③ trigger 0：逐卡桶空不空 + **行级**「这一行自己的 `Ab` 在桶里有没有 op」（这才是
    //      「这条 ability 有没有文本对应物」的真读数；`Lg == -1` 的行在原版就是空循环、单独计）。
    //  外加 `AM54` / `GOF93` / `GOF104` 三张的逐行明细（`B-211` 的三张卡）。
    //
    //  ⛔ 只读：不碰基线 / 三个指纹 / `check`·`play`·`ai` 的任何输出；`rc` 恒 0。
    // ==========================================================================================
    const int T0Trigger = 0;
    static readonly string[] T0Focus = { "AM54", "GOF93", "GOF104" };

    static void Trigger0Report(List<CardDef> pool)
    {
        Console.WriteLine();
        Console.WriteLine("=== ① 数据半挂载读数（`TriggerOpsByNumberSummary`，产品口径）===");
        Console.WriteLine("  " + CardDatabase.TriggerOpsByNumberSummary());

        // ---- ② 逐 trigger：桶非空 / 桶空 / 列未知 ----
        //   [0]=有该 trigger 行的卡数 · [1]=列已知且桶非空 · [2]=列已知且桶空 · [3]=列未知（退回档）
        var byTrigger = new SortedDictionary<int, int[]>();
        foreach (var c in pool)
        {
            var rows = c.TriggerAbilityIndices;
            if (rows == null || rows.Length == 0) continue;          // 整卡列未知 / 明确没有行
            bool known = c.HasTriggerOpsByNumber;
            var trigs = new HashSet<int>();
            foreach (var r in rows) if (r != null && r.Trigger >= 0) trigs.Add(r.Trigger);
            foreach (int t in trigs)
            {
                int[] cell;
                if (!byTrigger.TryGetValue(t, out cell)) { cell = new int[4]; byTrigger[t] = cell; }
                cell[0]++;
                if (!known) { cell[3]++; continue; }
                var ops = c.TriggerOpsByNumber(t);
                if (ops != null && ops.Count > 0) cell[1]++; else cell[2]++;
            }
        }
        Console.WriteLine();
        Console.WriteLine("=== ② 逐 trigger：桶非空 / 桶空 / 列未知（卡数）===");
        Console.WriteLine("  trigger | 有该行卡数 | 列已知·桶非空 | 列已知·桶空 | 列未知(退回档)");
        foreach (var kv in byTrigger)
        {
            var v = kv.Value;
            Console.WriteLine($"  {kv.Key,7} | {v[0],10} | {v[1],13} | {v[2],11} | {v[3],13}");
        }

        // ---- ③ trigger 0 专项 ----
        int cardsWithRows = 0;
        int knownNonNull = 0, knownNull = 0, colUnknown = 0;
        int rowsTotal = 0, rowsLgNeg = 0, rowsWithOps = 0, rowsWithoutOps = 0;
        var rowsByCard = new SortedDictionary<int, int>();          // trigger-0 行数 → 卡数
        var noOpsByCa = new SortedDictionary<int, int>();           // 「这一行自己 Ab 没 op」→ 按 ca 计数
        var nullBucketCards = new List<string>();                   // 桶为空的卡（= 今天会被 ca 兜底接住的候选）
        var nonEmptyBuckets = new List<string>();                    // 桶非空的卡（= 接上数值路会**双发**的候选）
        foreach (var c in pool)
        {
            var rows = c.TriggerAbilityRowsOf(T0Trigger);           // null = 列未知 · [] = 明确没有
            if (rows == null || rows.Length == 0) continue;
            cardsWithRows++;
            int n = rows.Length;
            int prev; rowsByCard.TryGetValue(n, out prev); rowsByCard[n] = prev + 1;

            bool known = c.HasTriggerOpsByNumber;
            var bucket = known ? c.TriggerOpsByNumber(T0Trigger) : null;
            int bucketCount = bucket == null ? 0 : bucket.Count;
            if (!known) colUnknown++;
            else if (bucketCount > 0)
            {
                knownNonNull++;
                nonEmptyBuckets.Add($"{c.Id} {c.Name}（op {bucketCount} 条 · trigger 0 行 {n} 条）");
            }
            else { knownNull++; nullBucketCards.Add(c.Id ?? c.Name); }

            foreach (var r in rows)
            {
                if (r == null) continue;
                rowsTotal++;
                if (r.Lg < 0) { rowsLgNeg++; continue; }            // 原版这条 ability 没有 logic 行 ⇒ 空循环
                int opsForAb = 0;
                if (bucket != null)
                    foreach (var op in bucket)
                        if (op != null && op.AbilityIndex == r.Ab) opsForAb++;
                if (opsForAb > 0) rowsWithOps++;
                else
                {
                    rowsWithoutOps++;
                    int pc; noOpsByCa.TryGetValue(r.Ca, out pc); noOpsByCa[r.Ca] = pc + 1;
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine($"=== ③ trigger {T0Trigger}（ThisCardPlayed）逐卡 ===");
        Console.WriteLine($"  有 trigger 0 行的卡：{cardsWithRows} 张（共 {rowsTotal} 行；其中 lg=-1 的 {rowsLgNeg} 行）");
        Console.WriteLine($"  · 列已知 ∧ 桶**非空** ⇒ ca 兜底**进不去**：{knownNonNull} 张");
        Console.WriteLine($"  · 列已知 ∧ 桶**空**   ⇒ ca 兜底**会**被叫到：{knownNull} 张");
        Console.WriteLine($"  · 列未知（退回档，判不了，⛔ 不算任何一边）：{colUnknown} 张");
        Console.WriteLine($"  行级：它自己的 `Ab` 在桶里有 op 的 {rowsWithOps} 行 · **没有** op 的 {rowsWithoutOps} 行");
        Console.Write("    「没有 op」那批按 `ca` 分布：");
        foreach (var kv in noOpsByCa) Console.Write($"ca={kv.Key}×{kv.Value}  ");
        Console.WriteLine();
        Console.Write("  trigger-0 行数 → 卡数：");
        foreach (var kv in rowsByCard) Console.Write($"{kv.Key}行×{kv.Value}张  ");
        Console.WriteLine();
        if (nullBucketCards.Count > 0)
        {
            Console.WriteLine($"  「列已知 ∧ 桶空」的卡（{nullBucketCards.Count} 张，前 40）：");
            for (int i = 0; i < nullBucketCards.Count && i < 40; i++) Console.Write(nullBucketCards[i] + " ");
            Console.WriteLine();
        }
        Console.WriteLine();
        Console.WriteLine($"=== ⑤ trigger 0 **桶非空**的卡（{nonEmptyBuckets.Count} 张）===");
        Console.WriteLine("  ⚠️ 这批是「若把 `FireTriggerByNumber(0)` 接进出牌点，会把**已经由文本路跑过的正文再跑一遍**」的候选");
        Console.WriteLine("     —— 补派发器时**必须逐张确认**，别让它们双发。");
        foreach (var s in nonEmptyBuckets) Console.WriteLine("  · " + s);
        Console.WriteLine();
        Console.WriteLine("  ⚠️ 反向同样重要：「桶空」在这里**不等于**「这张卡没有正文」——");
        Console.WriteLine("     战术/防御的正文走的是 `desc` 解析（`PlayTacticBodyDeferred`），**不经过** `TriggerOpsByNumber` 的那张表；");
        Console.WriteLine("     单位的 `Rally:` 正文在 `TriggerOps(\"rally\")` 那一格，而数值路的退回档查的是 `TriggerOps(\"thiscardplayed\")`（恒 null）。");

        // ---- ④ 三张焦点卡逐行 ----
        Console.WriteLine();
        Console.WriteLine("=== ④ `B-211` 三张焦点卡（AM54 / GOF93 / GOF104）逐行 ===");
        foreach (var id in T0Focus)
        {
            CardDef c = null;
            foreach (var x in pool) if (Eq(x.Id, id)) { c = x; break; }
            if (c == null) { Console.WriteLine($"  !! 池内找不到 {id}"); continue; }
            var rows = c.TriggerAbilityRowsOf(T0Trigger);
            bool known = c.HasTriggerOpsByNumber;
            var bucket = known ? c.TriggerOpsByNumber(T0Trigger) : null;
            // 对照：文本路真正取正文的那两格（单位 = `TriggerOps("rally")`；战术 = `desc` 解析，不在这张表里）
            var rallyOps = c.TriggerOps("rally");
            string tn;
            bool hasTn = CardDef.TryAbilityTriggerNameOfNumber(T0Trigger, out tn);
            Console.WriteLine();
            Console.WriteLine($"  == {c.Id} · {c.Name} · type={c.Type} ==");
            Console.WriteLine($"     HasTriggerOpsByNumber = {known}"
                            + (known ? "（产品口径）" : " 🔴（未挂表 ⇒ 退回档）"));
            Console.WriteLine($"     TriggerOpsByNumber(0) = {(bucket == null ? "null（桶空）" : bucket.Count + " 条")}");
            Console.WriteLine($"     退回档会查的名字：TryAbilityTriggerNameOfNumber(0) = {(hasTn ? tn : "认不出")}"
                            + $" ⇒ TriggerOps(\"{(hasTn ? tn.ToLowerInvariant() : "?")}\") = "
                            + (hasTn ? CountOf(c.TriggerOps(tn.ToLowerInvariant())) : "-"));
            Console.WriteLine($"     对照 · TriggerOps(\"rally\") = {CountOf(rallyOps)} 条"
                            + (rallyOps == null && c.Type == "unit" ? "（⚠️ 单位卡本应从这里取正文）" : ""));
            if (bucket != null && bucket.Count > 0)
            {
                var abCnt = new SortedDictionary<int, int>();
                foreach (var op in bucket)
                {
                    if (op == null) continue;
                    int k = op.AbilityIndex, pc; abCnt.TryGetValue(k, out pc); abCnt[k] = pc + 1;
                }
                Console.Write("       桶内按 AbilityIndex 分布：");
                foreach (var kv in abCnt) Console.Write($"ab={kv.Key}×{kv.Value}  ");
                Console.WriteLine();
            }
            if (rows == null) { Console.WriteLine("     trigger 0 行：列未知（判不了）"); continue; }
            if (rows.Length == 0) { Console.WriteLine("     trigger 0 行：明确没有（[]）"); continue; }
            Console.WriteLine($"     trigger 0 行 {rows.Length} 条：");
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null) { Console.WriteLine($"       [{i}] null"); continue; }
                int opsForAb = 0;
                if (bucket != null)
                    foreach (var op in bucket)
                        if (op != null && op.AbilityIndex == r.Ab) opsForAb++;
                string verdict = r.Lg < 0 ? "lg=-1 ⇒ 原版空循环，不贡献 op"
                                         : (opsForAb > 0 ? $"有 {opsForAb} 条 op（文本能表达）"
                                                         : "**0 条 op（文本表达不了 ⇒ 需要行级执行器）**");
                Console.WriteLine($"       [{i}] ab={r.Ab} lg={r.Lg} ca={r.Ca} ⇒ 该 ab 在桶里 {opsForAb} 条 · {verdict}");
            }
            // 派发口那一层：桶非空 ⇒ `FireTriggerRowsByChosenAbility` 今天压根不会被叫
            Console.WriteLine(known && bucket != null && bucket.Count > 0
                ? "     ⇒ 层②结论：**桶非空 ⇒ `FireTriggerRowsByChosenAbility` 不会被叫到**（ca 行轮不到兜底）"
                : (known ? "     ⇒ 层②结论：**桶空 ⇒ 若接上 `FireTriggerByNumber(0)`，ca 兜底会被叫到**"
                         : "     ⇒ 层②结论：列未知 ⇒ 只能走退回档，⛔ 不下判断"));
        }
    }
}
