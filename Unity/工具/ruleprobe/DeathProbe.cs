using System;
using System.Collections.Generic;
using RuleEngine;

/// <summary>
/// `death` —— **死亡路径的仓库外夹具**（`D118-28` · 活状态表 `<见报告>`）。
///
/// 🔴 **它治的是哪一条**：`play` / `play all dump` **结构上看不见死亡** ——
///    · `play all dump` 是同卡前后两张 `Snap` 的**净差**，一个单位死掉在那一列里只会表现为
///      「某格从 `id 3/0/1/4` 变成 `.`」或一条 `☠ 离场`，**读者看不出来那是「死了」还是「挪走了」**；
///    · 而 `play check` 的指纹**不含死亡事件本身**（只有终局快照）⇒ 拿它绿当「死亡路径没问题」的证据是错的
///      （判据 → `报告/B101_K3_特效音效联机自检线核销.md:87` 的 A4 行 · `调度台日志.md` §12·105 末句）。
///   ⇒ 判据要求：「验收口写成**仓库外夹具**（`工具/ruleprobe/` 这一侧，不依赖 Unity）或 Unity 自检」。
///   本文件就是那个夹具。
///
/// 验什么（三组，每条都给「改前会怎样」的反证思路）
/// -------------------------------------------------
/// **① 死亡本身可观察（`play` 看不见的那一格）**
///   把一个单位打到 0 血 ⇒ 断言：① 它的格子**变成 null**（真离场，而不是留一个负血活单位）；
///   ② 它进了弃牌堆（`Discard` 里多一张同 id）；③ 引擎日志里有死亡记录。
///   ⚠️ `RuleCore.ApplyDamage` **不处理死亡**（只扣血 / 发事件，离场在 `Hurt` → `RemoveIfDead`
///   → `CleanupDeaths`）⇒ 本夹具走 `RuleCore.HurtWithSourceForTest`（= `Hurt` 的公开包装）。
///
/// **② 死亡时摘掉「来源 = 死者」的外来效果**（`调度台日志.md` §12·105 · `G22②`）
///   给幸存者挂三本账各一条**来源 = 即将死亡的那张牌**的 extrinsic 增益：
///   `_grants.Source`（`RecordGrant(source:)`）· `_grants.Caster`（`RecordGrant(caster:)`）·
///   `_buffs.SourceCard`（`AddTempBuff`）。杀掉来源卡之后断言**三条一起消失**（攻/血/远程回到基线），
///   **同时**保留一条「来源还活着」的负例：同样的三本账、不杀 ⇒ 三条**都还在**。
///   原版判据 = `CardScript__ResolveDeadCard.c:242-244` → `RemoveExtrinsicEffectsFrom(观察者, 死者)`。
///   ⚠️ 改前的实参形状是「改前 4 → 改后 2」（`调度台日志` 同节）；本夹具把它钉成**三条账各一条**。
///
/// **③ `Unstable`(81)：本单位死亡 ⇒ 对随机单位 1–3 伤害**（`S3未验到_分族与夹具.md` §3.2 第一行）
///   被杀的单位挂 `Unstable` ⇒ 泵一次动作队列，断言场上**有且只有**总量 1–3 的伤害落下，
///   且死者的死亡**先于**自爆结算（日志次序：死亡 ⇒ Unstable）。
///
/// 用法
/// ----
///     bash d:/4/Unity/工具/ruleprobe.sh death          # 跑全部三组，有红 ⇒ rc=1
///     bash d:/4/Unity/工具/ruleprobe.sh death dead     # 只跑 ①
///     bash d:/4/Unity/工具/ruleprobe.sh death ext      # 只跑 ②
///     bash d:/4/Unity/工具/ruleprobe.sh death unstable # 只跑 ③
///
/// ⚠️ 这是**量具**、不是产品代码：它只调公开口（`NewBattle` / `HurtWithSourceForTest` /
///    `ActionPump.PumpActionQueues` / `UnitState.RecordGrant` / `AddTempBuff`），
///    ⛔ 不改 `Core/**` 一行、⛔ 不碰三个指纹。
/// </summary>
class DeathProbe
{
    static int _fail;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "  ✓ " : "  ✗ ") + what);
        if (!ok) _fail++;
    }

    // ------------------------------------------------------------------ 夹具
    static CardDef Hero(string n, string faction) =>
        new CardDef(n, n, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef Unit(string n, int cost, int atk, int hp) =>
        new CardDef(n, n, "unit", "", null, "Test", cost, atk, hp, 0, null, subtype: "Infantry");

    /// <summary>与 `RuleEngineTest.ProbeBattle` / `N2Probe` 同形：只有两个督军、没有手牌。</summary>
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

    static int SlotOf(BattleContext ctx, int p, UnitState u)
    {
        var b = ctx.Players[p].Board;
        for (int s = 0; s < b.Length; s++) if (ReferenceEquals(b[s], u)) return s;
        return -1;
    }

    static int CountDiscard(BattleContext ctx, int p, string id)
    {
        int n = 0;
        foreach (var c in ctx.Players[p].Discard) if (c.Card != null && c.Card.Id == id) n++;
        return n;
    }

    static void DumpBoard(BattleContext ctx, string tag)
    {
        Console.WriteLine("    [" + tag + "] 棋盘：");
        for (int p = 0; p < 2; p++)
        {
            var b = ctx.Players[p].Board;
            for (int s = 0; s < b.Length; s++)
            {
                var u = b[s];
                Console.WriteLine("      P" + p + "[" + s + "] = " + (u == null ? "."
                    : u.Card.Id + " " + u.Attack + "/" + u.RangedAttack + "/" + u.Armor + "/" + u.Health
                      + (u.IsAlive ? "" : " (DEAD)")));
            }
        }
    }

    // ------------------------------------------------------------------ ① 死亡可观察
    static void CheckDeathVisible()
    {
        Console.WriteLine();
        Console.WriteLine("① 死亡本身可观察（`play` 看不见的那一格）");
        var ctx = ProbeBattle();
        var victim = Place(ctx, 0, 5, Unit("FixtureVictim", 1, 1, 3));
        int slot = SlotOf(ctx, 0, victim);
        int discBefore = CountDiscard(ctx, 0, "FixtureVictim");
        DumpBoard(ctx, "死前");

        int rc = RuleCore.HurtWithSourceForTest(ctx, victim, 99, "死亡夹具");
        RuleCore.PumpActionQueues(ctx);

        DumpBoard(ctx, "死后");
        Check(slot >= 0, "前提：死者死前在 P0 格 " + slot);
        Check(ctx.Players[0].Board[slot] == null, "死后那一格变成 null（真离场，不是「负血活单位」）");
        Check(!victim.IsAlive, "死者自己 `IsAlive == false`");
        Check(CountDiscard(ctx, 0, "FixtureVictim") == discBefore + 1,
              "死者进弃牌堆（弃牌数 " + discBefore + " → " + CountDiscard(ctx, 0, "FixtureVictim") + "）");
        Check(rc >= 0, "`HurtWithSourceForTest` 返回码正常（" + rc + "）");
        bool deathLogged = false;
        foreach (var l in ctx.Events) if (l != null && l.Contains("FixtureVictim") && (l.Contains("死") || l.Contains("离场") || l.Contains("阵亡"))) deathLogged = true;
        Check(deathLogged, "引擎日志里有死亡记录（⛔ 不静默）");
    }

    // ------------------------------------------------------------------ ② 死亡时摘外来效果
    /// <summary>给幸存者挂三本账各一条「来源 = source 名」的 extrinsic 增益。</summary>
    static void GrantThree(UnitState target, UnitState source)
    {
        target.RecordGrant("attack", 2, source: source.Name, extrinsic: true);            // 账本 1：_grants.Source
        target.RecordGrant("health", 2, source: "fixture-other-source",
                           caster: source.Name, extrinsic: true);                        // 账本 2：_grants.Caster
        target.AddTempBuff(new UnitState.TempBuff
        {
            Name = "ranged", Value = 1, Src = source.Name, SourceCard = source.Name,
            Extrinsic = true, UntilMyNextTurn = true,
        });                                                                              // 账本 3：_buffs.SourceCard
    }

    static int CountGrantsTouching(UnitState u, string name)
    {
        int n = 0;
        foreach (var g in u.PendingGrants)
            if (g.Source == name || g.Caster == name) n++;
        return n;
    }

    static int CountBuffsFrom(UnitState u, string name)
    {
        int n = 0;
        foreach (var b in u.TempBuffs) if (b != null && b.SourceCard == name) n++;
        return n;
    }

    static void CheckExtrinsicSweep()
    {
        Console.WriteLine();
        Console.WriteLine("② 死亡时摘掉「来源 = 死者」的外来效果（G22② · 三本账各一条）");
        // ---- 正例：来源死 ----
        var ctx = ProbeBattle();
        var src = Place(ctx, 0, 3, Unit("FixtureSource", 1, 1, 4));
        var tgt = Place(ctx, 0, 5, Unit("FixtureTarget", 1, 2, 4));
        int atk0 = tgt.Attack, hp0 = tgt.MaxHealth, rng0 = tgt.RangedAttack;
        GrantThree(tgt, src);
        Check(tgt.Attack == atk0 + 2, "前提：外来 attack +2 已生效（" + atk0 + " → " + tgt.Attack + "）");
        Check(tgt.MaxHealth == hp0 + 2, "前提：外来 health +2 已生效（" + hp0 + " → " + tgt.MaxHealth + "）");
        Check(tgt.RangedAttack == rng0 + 1, "前提：外来 ranged +1 已生效（" + rng0 + " → " + tgt.RangedAttack + "）");
        Check(CountGrantsTouching(tgt, "FixtureSource") == 2, "前提：两本 grant 账各 1 条（Source / Caster）");
        Check(CountBuffsFrom(tgt, "FixtureSource") == 1, "前提：TempBuff 账 1 条（SourceCard）");

        RuleCore.HurtWithSourceForTest(ctx, src, 99, "死亡夹具");
        RuleCore.PumpActionQueues(ctx);

        Console.WriteLine("    来源死后：target = " + tgt.Attack + "/" + tgt.RangedAttack + "/" + tgt.Armor + "/" + tgt.MaxHealth
                          + " · grants=" + CountGrantsTouching(tgt, "FixtureSource") + " · buffs=" + CountBuffsFrom(tgt, "FixtureSource"));
        Check(tgt.Attack == atk0, "账本 1（Source）已撤：attack 回到 " + atk0);
        Check(tgt.MaxHealth == hp0, "账本 2（Caster）已撤：MaxHealth 回到 " + hp0);
        Check(tgt.RangedAttack == rng0, "账本 3（SourceCard）已撤：ranged 回到 " + rng0);
        Check(CountGrantsTouching(tgt, "FixtureSource") == 0, "`PendingGrants` 里不再有来源 / 施放者 = 死者");
        Check(CountBuffsFrom(tgt, "FixtureSource") == 0, "`TempBuffs` 里不再有来源 = 死者");

        // ---- 负例：来源还活着 ⇒ 三条都保留（否则「全撤」也能让上面全绿 = 自证）----
        var ctx2 = ProbeBattle();
        var src2 = Place(ctx2, 0, 3, Unit("FixtureSource2", 1, 1, 4));
        var tgt2 = Place(ctx2, 1, 5, Unit("FixtureTarget2", 1, 2, 4));
        int a2 = tgt2.Attack, h2 = tgt2.MaxHealth, r2 = tgt2.RangedAttack;
        GrantThree(tgt2, src2);
        RuleCore.PumpActionQueues(ctx2);
        Check(tgt2.Attack == a2 + 2 && tgt2.MaxHealth == h2 + 2 && tgt2.RangedAttack == r2 + 1,
              "负例（来源活着）：三条外来增益**都保留**");
        Check(CountGrantsTouching(tgt2, "FixtureSource2") == 2 && CountBuffsFrom(tgt2, "FixtureSource2") == 1,
              "负例（来源活着）：三本账都还在");
    }

    // ------------------------------------------------------------------ ③ Unstable 81
    static void CheckUnstable()
    {
        Console.WriteLine();
        Console.WriteLine("③ `Unstable`(81)：本单位死亡 ⇒ 对随机单位 1–3 伤害");
        var ctx = ProbeBattle();
        var u = Place(ctx, 0, 3, Unit("FixtureUnstable", 1, 1, 3));
        u.AddKeyword(KeywordTable.Unstable, 1);
        var v = Place(ctx, 1, 5, Unit("FixtureBlastTarget", 1, 0, 10));
        var w0 = ctx.Players[0].Warlord;
        var w1 = ctx.Players[1].Warlord;
        int hpU = u.Health, hpV = v.Health, hpW0 = w0.Health, hpW1 = w1.Health;
        int logAt = ctx.Events.Count;

        Check(u.Has(KeywordTable.Unstable), "前提：被测单位身上挂着 `unstable`");
        RuleCore.HurtWithSourceForTest(ctx, u, 99, "死亡夹具");
        RuleCore.PumpActionQueues(ctx);

        int lost = (hpV - v.Health) + (hpW0 - w0.Health) + (hpW1 - w1.Health);
        Console.WriteLine("    血量：U " + hpU + "→" + u.Health + " · V " + hpV + "→" + v.Health
                          + " · W0 " + hpW0 + "→" + w0.Health + " · W1 " + hpW1 + "→" + w1.Health + " · 合计掉 " + lost);
        Check(!u.IsAlive, "前提：带 `unstable` 的单位已被打死");
        Check(lost >= 1 && lost <= 3, "自爆伤害落在 1–3 之间（实得 " + lost + "）");
        Check(lost > 0, "自爆真的落到了某个单位身上（不是 `ApplyDamage(999)` 那种「留负血活单位、一条触发都不响」）");
        // 次序：Unstable 在死亡之后结算（日志里 death-signal / 离场 早于 Unstable）
        int iDeath = -1, iUnstable = -1;
        for (int i = logAt; i < ctx.Events.Count; i++)
        {
            var l = ctx.Events[i];
            if (l == null) continue;
            if (iDeath < 0 && (l.Contains("离场") || l.Contains("死") || l.Contains("阵亡"))) iDeath = i;
            if (iUnstable < 0 && l.Contains("Unstable")) iUnstable = i;
        }
        Console.WriteLine("    日志窗口：death@" + iDeath + " · unstable@" + iUnstable);
        Check(iDeath >= 0, "日志里有死亡记录");
        // 只要求「死亡先于自爆」；两者都不在窗口里时不出红（如实印出来）
        if (iDeath >= 0 && iUnstable >= 0) Check(iDeath <= iUnstable, "次序：死亡记录在 Unstable 之前");
    }

    // ------------------------------------------------------------------ 入口
    public static int Run(string which)
    {
        _fail = 0;
        Console.WriteLine("= `death` 死亡路径仓库外夹具（D118-28 · 不跑 Unity / 不改 Core）= ");
        Console.WriteLine("  用法：death [dead|ext|unstable]，缺省跑全部三组");
        bool all = string.IsNullOrEmpty(which);
        if (all || which == "dead") CheckDeathVisible();
        if (all || which == "ext") CheckExtrinsicSweep();
        if (all || which == "unstable") CheckUnstable();
        Console.WriteLine();
        Console.WriteLine(_fail == 0 ? "全部通过（0 红）" : ("红 " + _fail + " 条"));
        return _fail;
    }
}
