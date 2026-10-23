using System;
using System.Collections.Generic;
using RuleEngine;

/// <summary>
/// `n2` —— 引擎线 `新账-95` #2（`unitConditions` 的 `mult` 结构化乘数）**离线读数**（不跑 Unity）。
///
/// 🔴 **为什么有它**：`RuleEngineTest_UnitRoles.N2RoleMult()` 的两条断言（2 个受伤的敌人 ⇒ 乘数 2 ·
///    0 个受伤 ⇒ 乘数 0）在 `RuleEngineTest.Run` 里是红的（实得 3 / 1）。正式验收仍然是那个宿主，
///    但它要占 Unity 批处理 ⇒ 本探针用**同一份 `Core/` 源码**把中间每一跳的读数摆出来：
///    归位下标 → `TryGetCondition` → `CountEval.CountTargets` → `repeats` → 督军 MaxHealth 差。
///    ⛔ 它不判对错；判据在 `AbilityLogic__PlayAbility.c:181-236` 与卡面
///    （`Give Armour 1 to your Warlord, plus +1 Health for each damaged enemy`）。
///
/// 跑法：`bash d:/4/Unity/工具/ruleprobe.sh n2`
/// </summary>
class N2Probe
{
    static int _fail;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "  ✓ " : "  ✗ ") + what);
        if (!ok) _fail++;
    }

    static CardDef Hero(string n, string faction) =>
        new CardDef(n, n, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    static CardDef Unit(string n, int cost, int atk, int hp) =>
        new CardDef(n, n, "unit", "", null, "Test", cost, atk, hp, 0, null, subtype: "Infantry");

    /// <summary>与 `RuleEngineTest.ProbeBattle` 同形：只有两个督军、没有手牌。</summary>
    static BattleContext ProbeBattle()
    {
        var d0 = new List<CardDef> { Hero("FixtureWarlord", "Ultramarines") };
        var d1 = new List<CardDef> { Hero("FixtureWarlord", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>把某一方的督军补满血 —— 🔴 夹具归零用：`ProbeBattle` 的牌库只有督军 ⇒ 开局疲劳
    /// 立刻扣 10 点，敌方督军天然就是 `damaged(40)`，而 `mult` 行 `includeHero = 1` ⇒ 它会被数。</summary>
    static void HealToFull(BattleContext ctx, int p)
    {
        var w = ctx.Players[p].Warlord;
        w.Health = w.MaxHealth;
    }

    static void DumpBoard(BattleContext ctx, int p)
    {
        var ps = ctx.Players[p];
        for (int s = 0; s < ps.Board.Length; s++)
        {
            var u = ps.Board[s];
            if (u == null) continue;
            Console.WriteLine($"      board[{p}][{s}] = {u.Name} {u.Health}/{u.MaxHealth}"
                            + (u.IsWarlord ? " (Warlord)" : "") + (ReferenceEquals(u, ps.Warlord) ? " [== Players[p].Warlord]" : ""));
        }
        Console.WriteLine($"      Players[{p}].Warlord = {ps.Warlord.Name} {ps.Warlord.Health}/{ps.Warlord.MaxHealth}"
                        + $"（在 board 里？{System.Array.IndexOf(ps.Board, ps.Warlord) >= 0}）");
    }

    public static int Run(List<CardDef> pool)
    {
        Console.WriteLine("=== N2 · `mult` 结构化乘数（离线读数）===");

        var gof74 = CardDatabase.FindById(pool, "GOF74");
        Check(gof74 != null, "卡池里有 `GOF74 Grizzled Skarboy`");
        if (gof74 == null) return Math.Max(_fail, 1);
        Console.WriteLine("  desc = " + gof74.Desc);

        var ops = EffectText.Parse(gof74.Desc, out var unparsed, out var partial, card: gof74);
        Console.WriteLine($"  解析出 {ops.Count} 条 op（unparsed={unparsed.Count} partial={partial.Count}）");
        for (int i = 0; i < ops.Count; i++)
        {
            var o = ops[i];
            Console.WriteLine($"   op[{i}] verb={o.Verb} amount={o.Amount} payload=\"{o.Payload}\""
                            + $" ab={o.AbilityIndex} lg={o.LogicIndex} countRef={o.CountRef ?? "<null>"}"
                            + $" countScope={o.CountScope ?? "<null>"} perCount={o.PerCount}"
                            + $" repeatTimes={o.RepeatTimes} fm={o.AbilityFurtherMultiplier}");
        }
        if (ops.Count != 2) { Check(false, "(前提) 两条 op"); return Math.Max(_fail, 1); }

        var bare = ops[1].Clone();
        bare.CountRef = null;
        bare.CountScope = null;
        bare.PerCount = 0;

        // ---- 行归位 ----
        CardDef.ConditionSpec spec;
        bool has = gof74.TryGetCondition(bare.AbilityIndex, bare.LogicIndex, "mult", out spec);
        Console.WriteLine($"  TryGetCondition(ab={bare.AbilityIndex}, lg={bare.LogicIndex}, \"mult\") = {has}"
                        + (has && spec != null ? $"（cond={spec.Condition} ex={(spec.Exclude ? 1 : 0)}"
                                               + $" multSet={(spec.MultiplierSet != null ? "有" : "null")}）" : ""));

        // ================================================================
        //  A：2 个受伤的敌人（+1 个满血）⇒ 期望 2 遍
        // ================================================================
        Console.WriteLine("--- A：2 个受伤敌人 + 1 个满血 ⇒ 期望 MaxHealth +2 ---");
        {
            var ctx = ProbeBattle();
            var w = ctx.Players[0].Warlord;
            int before = w.MaxHealth;
            // 🔴 夹具归零：`ProbeBattle` 的牌库只有督军 ⇒ **开局疲劳立刻扣了 10 点**
            //    ⇒ 敌方督军是 20/30 = **damaged**，而 `mult` 行的 `includeHero = 1` ⇒ 它**会被数**。
            //    把敌方督军补满，才是「受伤的敌人 = 那两张部队」。
            HealToFull(ctx, 1);
            var e0 = Place(ctx, 1, 0, Unit("N2_mult_hurt0", 1, 1, 5));
            var e1 = Place(ctx, 1, 1, Unit("N2_mult_hurt1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("N2_mult_full", 1, 1, 5));
            e0.Health = 3; e1.Health = 3;
            DumpBoard(ctx, 1);

            if (has && spec != null && spec.MultiplierSet != null)
            {
                var n = CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out var gap);
                Console.WriteLine($"      CountTargets(cond={spec.Condition}, actor=null) = "
                                + (n == null ? "null" : n.Value.ToString()) + $"（gap={gap ?? "<null>"}）");
            }
            List<string> un;
            RuleCore.ResolveOps(ctx, 0, null, new List<EffectOp> { bare }, null, out un, sourceCard: gof74);
            Console.WriteLine($"      督军 MaxHealth: {before} → {w.MaxHealth}（差 {w.MaxHealth - before}）  unresolved={un.Count}");
            foreach (var e in ctx.Events) Console.WriteLine("      ‖ " + e);
            Check(w.MaxHealth - before == 2, "A：2 个受伤敌人 ⇒ MaxHealth +2");
        }

        // ================================================================
        //  B：3 个敌人都满血 ⇒ 期望 0 遍
        // ================================================================
        Console.WriteLine("--- B：3 个敌人全满血 ⇒ 期望 MaxHealth +0 ---");
        {
            var ctx = ProbeBattle();
            var w = ctx.Players[0].Warlord;
            int before = w.MaxHealth;
            HealToFull(ctx, 1);
            Place(ctx, 1, 0, Unit("N2_mult_a", 1, 1, 5));
            Place(ctx, 1, 1, Unit("N2_mult_b", 1, 1, 5));
            Place(ctx, 1, 2, Unit("N2_mult_c", 1, 1, 5));
            DumpBoard(ctx, 1);
            if (has && spec != null && spec.MultiplierSet != null)
            {
                var n = CountEval.CountTargets(ctx, 0, spec.MultiplierSet, spec.Condition, null, out var gap);
                Console.WriteLine($"      CountTargets(cond={spec.Condition}, actor=null) = "
                                + (n == null ? "null" : n.Value.ToString()) + $"（gap={gap ?? "<null>"}）");
            }
            List<string> un;
            RuleCore.ResolveOps(ctx, 0, null, new List<EffectOp> { bare }, null, out un, sourceCard: gof74);
            Console.WriteLine($"      督军 MaxHealth: {before} → {w.MaxHealth}（差 {w.MaxHealth - before}）  unresolved={un.Count}");
            foreach (var e in ctx.Events) Console.WriteLine("      ‖ " + e);
            Check(w.MaxHealth - before == 0, "B：0 个受伤敌人 ⇒ MaxHealth +0");
        }

        // ================================================================
        //  C：正文口径（不摘 `for each`）—— 对照：结构化与正文是否一致
        // ================================================================
        Console.WriteLine("--- C（对照）：保留正文 `for each damaged enemy` ⇒ 期望同样是 2 ---");
        {
            var ctx = ProbeBattle();
            var w = ctx.Players[0].Warlord;
            int before = w.MaxHealth;
            HealToFull(ctx, 1);
            var e0 = Place(ctx, 1, 0, Unit("N2_mult_hurt0", 1, 1, 5));
            var e1 = Place(ctx, 1, 1, Unit("N2_mult_hurt1", 1, 1, 5));
            Place(ctx, 1, 2, Unit("N2_mult_full", 1, 1, 5));
            e0.Health = 3; e1.Health = 3;
            List<string> un;
            RuleCore.ResolveOps(ctx, 0, null, new List<EffectOp> { ops[1] }, null, out un, sourceCard: gof74);
            Console.WriteLine($"      督军 MaxHealth: {before} → {w.MaxHealth}（差 {w.MaxHealth - before}）");
            foreach (var e in ctx.Events) Console.WriteLine("      ‖ " + e);
            Check(w.MaxHealth - before == 2, "C：正文口径也是 +2");
        }

        Console.WriteLine(_fail == 0 ? "=== N2 读数：全部符合期望 ===" : $"=== N2 读数：{_fail} 条不符 ===");
        return _fail;
    }
}
