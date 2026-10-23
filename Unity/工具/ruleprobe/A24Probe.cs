using System;
using System.Collections.Generic;
using RuleEngine;

/// <summary>
/// `a24` —— 引擎线第四批（`A24`，2026-10-24）两条账的**离线真跑**（不跑 Unity）。
///
/// 🔴 **为什么要它**：本批的两条账（`T21④` 回合末发射序 · `新账-104` `EC58` 双子部署）的**正式验收**
///    仍然是 `RuleEngineTest.Run`，但那要占 Unity 批处理（同一工程只能一个实例、`RuleEngineTest` ≈ 4 分钟）。
///    本探针用**同一份 `Core/` 源码**在进程内把两局的读数先摆一遍 —— 它验的是
///    「**夹具与期望值对不对**」（例如「`give them Flank` 到底给到几张」），⛔ 不是第二份判据：
///    判据在 **反编译方法体 / 卡数据字段 / 成品卡图** 上（见 `RuleEngineTest_Order.TestT21TurnBeforeEndBeforeTurnEnd`
///    与 `RuleEngineTest_S23_Resolve.TestA24EC58DeployPairAndFlank` 的 doc）。
///
/// ⚠️ 与 `play` 的分工：`play <id>` 打的是**它自己那套固定夹具**（没有「本回合死过友方」这一档）；
///    本条要的是**可控局面**（先杀一个友方部队，再打 `EC58`）。
/// </summary>
class A24Probe
{
    static int _fail;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "  ✓ " : "  ✗ ") + what);
        if (!ok) _fail++;
    }

    static CardDef Hero(string n, string faction = "Ultramarines") =>
        new CardDef(n, n, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    static CardDef Unit(string n, int cost, int atk, int hp) =>
        new CardDef(n, n, "unit", "", null, "Test", cost, atk, hp, 0, null);
    static CardDef Tactic(string n, int cost, string desc) =>
        new CardDef(n, n, "tactic", desc, "common", "Test", cost, 0, 0, 0, null);

    /// <summary>与 `RuleEngineTest.ProbeBattle` 同形：只有督军 + 指定的那几张牌（不塞 filler）。</summary>
    static BattleContext NewCtx(IList<CardDef> pool, string warlordFaction, params CardDef[] hand0)
    {
        var d0 = new List<CardDef> { Hero("FIXWarlord", warlordFaction) };
        for (int i = hand0.Length - 1; i >= 0; i--) d0.Add(hand0[i]);
        var d1 = new List<CardDef> { Hero("FIXFoe") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool);
    }

    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Console.WriteLine("!! 回合推进超限"); return; }
        }
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i] != null && h[i].Card != null && h[i].Card.Name == name) return i;
        return -1;
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = true };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    public static int Run(List<CardDef> pool)
    {
        Console.WriteLine("=== A24 · 引擎线第四批（离线真跑） ===");

        // ================================================================
        //  ① `T21④`：`TurnBeforeEnd(41)` 整趟早于 `TurnEnd(40)` 整趟，棋盘趟按进场序
        // ================================================================
        Console.WriteLine("--- ① `T21④` 回合末发射序 ---");
        {
            var chrono = CardDatabase.FindById(pool, "SAU20");       // 41（棋盘）
            var ordnance = CardDatabase.FindById(pool, "AM18");      // 40（棋盘）
            var drone = CardDatabase.FindById(pool, "TAU10");        // 40（棋盘）
            var poison = CardDatabase.FindById(pool, "a7bb23355a30d344e9d2decf976d91e1"); // 40（手牌陷阱）
            Check(chrono != null && ordnance != null && drone != null && poison != null,
                  "夹具四张真卡都在池里");
            if (chrono == null || ordnance == null || drone == null || poison == null) return Math.Max(_fail, 1);

            var ctx = NewCtx(pool, "Ultramarines", poison);
            ToP1Turn(ctx, 1);
            Check(ctx.Active == 0, "P1 是当前行动方");
            Check(HandIdx(ctx, 0, poison.Name) >= 0, "手牌陷阱在手里");
            var a = Place(ctx, 0, 6, chrono);      // 先落外格
            var b = Place(ctx, 0, 5, ordnance);
            var c = Place(ctx, 0, 4, drone);       // 后落内格
            Check(a.PlayOrder < b.PlayOrder && b.PlayOrder < c.PlayOrder,
                  "进场序 chrono(" + a.PlayOrder + ") < ordnance(" + b.PlayOrder + ") < drone(" + c.PlayOrder
                + ")，格号 6 > 5 > 4（两键相反）");

            ctx.ClearSignals();
            RuleCore.EndTurn(ctx);

            int iChrono = -1, iOrd = -1, iDrone = -1, iPoison = -1, iFirst41 = -1, iFirst40 = -1, n41 = 0, n40 = 0;
            for (int i = 0; i < ctx.Signals.Count; i++)
            {
                var e = ctx.Signals[i];
                if (e == null || e.Kind != EvtKind.Trigger) continue;
                bool is41 = e.Keyword == "TurnBeforeEnd", is40 = e.Keyword == "TurnEnd";
                if (!is41 && !is40) continue;
                if (is41) { n41++; if (iFirst41 < 0) iFirst41 = i; }
                if (is40) { n40++; if (iFirst40 < 0) iFirst40 = i; }
                if (e.CardId == chrono.Name) iChrono = i;
                else if (e.CardId == ordnance.Name) iOrd = i;
                else if (e.CardId == drone.Name) iDrone = i;
                else if (e.CardId == poison.Name) iPoison = i;
            }
            Console.WriteLine($"     信号序：Chronomancer@{(iChrono < 0 ? "—" : iChrono.ToString())} · "
                            + $"Ordnance@{(iOrd < 0 ? "—" : iOrd.ToString())} · "
                            + $"Drone@{(iDrone < 0 ? "—" : iDrone.ToString())} · "
                            + $"Poison(手牌)@{(iPoison < 0 ? "—" : iPoison.ToString())} · "
                            + $"共 41×{n41} / 40×{n40}");
            Check(n41 == 1, "`Chronomancer` 恰好一条 `TurnBeforeEnd(41)`");
            Check(n40 == 3, "三张 `TurnEnd(40)` 各一条（棋盘两张 + 手牌一张）");
            Check(iChrono >= 0 && iOrd >= 0 && iDrone >= 0 && iPoison >= 0, "四条事件都发出来了");
            Check(iFirst41 >= 0 && iFirst40 >= 0 && iFirst41 < iFirst40,
                  "41 整趟早于 40（首个 41 @ " + iFirst41 + " < 首个 40 @ " + iFirst40 + "）");
            Check(iOrd >= 0 && iDrone >= 0 && iOrd < iDrone, "棋盘趟 = 进场序（Ordnance 先于 Drone）");
            Check(iDrone >= 0 && iPoison >= 0 && iDrone < iPoison, "棋盘整趟在前、当前玩家手牌在后");
        }

        // ================================================================
        //  ② `新账-104`：`EC58 Vengeful Surge` 双子部署 + `them` = 两张
        // ================================================================
        Console.WriteLine("--- ② `新账-104` `EC58` 双子部署 ---");
        {
            var ec58 = CardDatabase.FindById(pool, "EC58");
            var infra = CardDatabase.FindById(pool, "EC8");
            var torm = CardDatabase.FindById(pool, "EC5");
            Check(ec58 != null && infra != null && torm != null, "`EC58` / `EC8 Infractor` / `EC5 Tormentor` 都在池里");
            if (ec58 == null || infra == null || torm == null) return Math.Max(_fail, 1);

            // ---- ②-a 有「友方部队本回合阵亡」⇒ 两张都上场、都拿 Flank ----
            var kill = Tactic("A24Kill", 0, "Deal 99 damage to a friendly unit");
            var ctx = NewCtx(pool, "Test", ec58, kill);
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            Place(ctx, 0, 3, Unit("A24Victim", 1, 0, 1));
            int hk = HandIdx(ctx, 0, "A24Kill");
            Check(hk >= 0, "（前提）手里有那张「打死自己人」的战术");
            if (hk >= 0) RuleCore.PlayTactic(ctx, 0, hk, 3);
            bool dead = false;
            foreach (var d in ctx.DeadUnits)
                if (d != null && d.Owner == 0 && d.DeathTurn == ctx.Turn && d.Card != null
                    && CreatePool.MatchesKind(d.Card, "troop")) dead = true;
            Check(dead, "（前提）友方部队**本回合**进了 `DeadUnits`");
            int hi = HandIdx(ctx, 0, "Vengeful Surge");
            Check(hi >= 0, "（前提）手里有 `Vengeful Surge`");
            if (hi >= 0) Console.WriteLine("     PlayTactic rc = " + RuleCore.PlayTactic(ctx, 0, hi, -1));
            UnitState gotInfra = null, gotTorm = null;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[0].Board[s];
                if (u == null || u.Card == null) continue;
                if (u.Card.Name == infra.Name) gotInfra = u;
                else if (u.Card.Name == torm.Name) gotTorm = u;
            }
            Check(gotInfra != null, "`Infractor` 上场了");
            Check(gotTorm != null, "`Tormentor` 也上场了（summonAllTargets=1）");
            Check(gotInfra != null && gotInfra.Has("flank"), "`Infractor` 拿到 Flank");
            Check(gotTorm != null && gotTorm.Has("flank"), "`Tormentor` 也拿到 Flank（`them` = 两张）");

            // ---- ②-b 灭自证：没有阵亡 ⇒ 两张照上，但都不该有 Flank ----
            var ctx2 = NewCtx(pool, "Test", ec58);
            ToP1Turn(ctx2, 1);
            ctx2.Players[0].Energy = 20;
            int hi2 = HandIdx(ctx2, 0, "Vengeful Surge");
            Check(hi2 >= 0, "（前提·无阵亡局）手里有 `Vengeful Surge`");
            if (hi2 >= 0) RuleCore.PlayTactic(ctx2, 0, hi2, -1);
            int got = 0, flanked = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx2.Players[0].Board[s];
                if (u == null || u.Card == null) continue;
                if (u.Card.Name != infra.Name && u.Card.Name != torm.Name) continue;
                got++;
                if (u.Has("flank")) flanked++;
            }
            Check(got == 2, "无阵亡时也照部署两张（得 " + got + "）");
            Check(flanked == 0, "灭自证：条件不成立 ⇒ 一张都没有 Flank（得 " + flanked + "）");
        }

        Console.WriteLine(_fail == 0 ? "=== A24 探针：全部通过 ===" : "=== A24 探针：失败 " + _fail + " 条 ===");
        return _fail;
    }
}
