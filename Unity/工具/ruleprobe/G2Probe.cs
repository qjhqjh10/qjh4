using System;
using System.Collections.Generic;
using RuleEngine;

/// <summary>
/// `g2` —— 引擎线剩余三件（`G2`，2026-10-24）的**离线真跑**（不跑 Unity）。
///
/// 🔴 **为什么有它**：本批三件（`D8` `ThisUnitSummoned` · `D9` `WhileInPlay` · `questTimes` 消费点）
///    的**正式验收**仍然是 `RuleEngineTest.Run` / `BattleScene.Run`，但那要占 Unity 批处理
///    （同一工程只能一个实例）。本探针用**同一份 `Core/` 源码**在进程内验「行为对不对」——
///    ⛔ 它不是第二份判据：判据在 **反编译方法体**（`CardScript__ResolveUnitSummoned.c` /
///    `CardScript__UpdateWhileInPlay.c` / `EntityScript__ReachedQuestCounter.c`）与**卡数据**上。
///
/// 跑法：`bash d:/4/Unity/工具/ruleprobe.sh g2`
/// </summary>
class G2Probe
{
    static int _fail;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "  ✓ " : "  ✗ ") + what);
        if (!ok) _fail++;
    }

    static CardDef Hero(string n, string faction = "SpaceWolves") =>
        new CardDef(n, n, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    static CardDef Unit(string n, int cost, int atk, int hp, string subtype = "Infantry") =>
        new CardDef(n, n, "unit", "", null, "Test", cost, atk, hp, 0, null, subtype: subtype);

    /// <summary>与 `RuleEngineTest.ProbeBattle` / `A24Probe` 同形：只有督军 + 指定的那几张牌。</summary>
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

    static int CountTrigger(BattleContext ctx, string keyword, string cardId)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
        {
            if (e == null || e.Kind != EvtKind.Trigger) continue;
            if (!string.Equals(e.Keyword, keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (cardId != null && e.CardId != cardId) continue;
            n++;
        }
        return n;
    }

    static string DumpTriggers(BattleContext ctx)
    {
        var l = new List<string>();
        foreach (var e in ctx.Signals)
        {
            if (e == null || e.Kind != EvtKind.Trigger) continue;
            l.Add(e.CardId + ":" + e.Keyword);
        }
        return l.Count == 0 ? "<无>" : string.Join(" · ", l);
    }

    static UnitState At(BattleContext ctx, int p, string name)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Card != null && u.Card.Name == name) return u;
        }
        return null;
    }

    /// <summary>第一个空格（`PlayCard` 传 `-1` 会吃 `ErrSlot` —— 它不做自动选位）。</summary>
    static int FirstFree(BattleContext ctx, int p)
    {
        for (int s = 0; s < BoardSpec.Size; s++) if (ctx.Players[p].Board[s] == null) return s;
        return -1;
    }

    /// <summary>把一张真卡**直接摆上场**（只用于「已经在场上的监听者」那一侧；被验的那张走真部署入口）。</summary>
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
        Console.WriteLine("=== G2 · 引擎线剩余三件（离线真跑） ===");

        // ================================================================
        //  ① `D8`：`SW10 Grey Hunter` 的部署那一拍 ⇒ `ThisUnitSummoned`(195)
        // ================================================================
        Console.WriteLine("--- ① `D8` `ThisUnitSummoned`（`SW10` 自部署）---");
        {
            var sw10 = CardDatabase.FindById(pool, "SW10");
            var tau34 = CardDatabase.FindById(pool, "TAU34");   // `When you deploy a troop, give it +2 Health`（190 那一族）
            Check(sw10 != null && sw10.Name == "Grey Hunter", "卡池里有 `SW10 Grey Hunter`");
            Check(tau34 != null && tau34.Name == "Coldstar Battlesuit", "卡池里有 `TAU34 Coldstar Battlesuit`（190 那一族）");
            if (sw10 == null || tau34 == null) return Math.Max(_fail, 1);

            // ---- ①-a 部署 SW10：它自己拿 195；场上的 TAU34（190 监听者）照旧拿 190 ----
            var ctx = NewCtx(pool, "SpaceWolves", sw10);
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            Place(ctx, 0, 6, tau34);                       // 190 那一族（`When you deploy a troop`）
            ctx.ClearSignals();
            int hi = HandIdx(ctx, 0, "Grey Hunter");
            Check(hi >= 0, "（前提）手里有 `Grey Hunter`");
            int rc = hi >= 0 ? RuleCore.PlayCard(ctx, 0, hi, FirstFree(ctx, 0)) : -999;
            Console.WriteLine("     递归读数：PlayCard rc = " + rc + " · 触发信号 = " + DumpTriggers(ctx));
            Check(rc == RuleCodes.OK, "（前提）`Grey Hunter` 真部署上去了（rc = " + rc + "）");
            Check(At(ctx, 0, "Grey Hunter") != null, "（前提）它在棋盘上");
            Check(CountTrigger(ctx, "ThisUnitSummoned", "Grey Hunter") == 1,
                  "★ `Grey Hunter` 部署**恰一条** `ThisUnitSummoned`(195)"
                + "（实得 " + CountTrigger(ctx, "ThisUnitSummoned", "Grey Hunter") + "）"
                + " —— 🧨 把 `TriggerNameOfWhenKind` 的 `Deploy` 支改回**不分 `SelfOnly`**"
                + "（恒 `OtherUnitSummoned`）⇒ 本条红（今天就是那一态）");
            Check(CountTrigger(ctx, "OtherUnitSummoned", "Grey Hunter") == 0,
                  "灭自证：`Grey Hunter` **没有** `OtherUnitSummoned`(190)（它不是「你部署了一个部队」那一族）");
            Check(CountTrigger(ctx, "OtherUnitSummoned", "Coldstar Battlesuit") >= 1,
                  "★ 190 那一族**没被误改**：`Coldstar Battlesuit` 照旧收到 `OtherUnitSummoned`"
                + "（实得 " + CountTrigger(ctx, "OtherUnitSummoned", "Coldstar Battlesuit") + "）"
                + " —— 🧨 把整个 `Deploy` 支改成 195 ⇒ 本条红");

            // ---- ①-b 灭自证：**别人**部署 ⇒ SW10 一条 `ThisUnitSummoned` 都不该再发 ----
            ctx.ClearSignals();
            var filler = Unit("G2Filler", 1, 1, 3);
            // 手牌里本来没有它 ⇒ 直接走**免费部署**那条真入口（`DeployFree`，同样会预调 SelfOnly）
            int slot2;
            bool dep = RuleCore.DeployFree(ctx, 0, filler, out slot2);
            Console.WriteLine("     递归读数：DeployFree = " + dep + " · slot = " + slot2
                            + " · 触发信号 = " + DumpTriggers(ctx));
            Check(dep, "（前提）`G2Filler` 免费部署成功");
            Check(CountTrigger(ctx, "ThisUnitSummoned", "Grey Hunter") == 0,
                  "★ 灭自证：**别人**部署时 `Grey Hunter` **没有** `ThisUnitSummoned`"
                + "（实得 " + CountTrigger(ctx, "ThisUnitSummoned", "Grey Hunter") + "）"
                + " —— 🧨 把 `Deploy` 支无条件映射成 195（不经 `SelfOnly`）⇒ 本条红");
        }

        // ================================================================
        //  ② `D9`：`SW18` 的 `WhileInPlay`(20) —— 动作队列排空时发一次
        // ================================================================
        Console.WriteLine("--- ② `D9` `WhileInPlay`（`SW18` 在场 ⇒ 每次动作队列排空发一次）---");
        {
            var sw18 = CardDatabase.FindById(pool, "SW18");
            Check(sw18 != null && sw18.Name == "Hrolf the Ironhowl", "卡池里有 `SW18 Hrolf the Ironhowl`");
            if (sw18 == null) return Math.Max(_fail, 1);

            var t1 = new CardDef("G2Dam1", "G2Dam1", "tactic", "Deal 1 damage to an enemy troop", "common", "Test", 0, 0, 0, 0, null);
            var ctx = NewCtx(pool, "SpaceWolves", t1);
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 20;
            Place(ctx, 0, 6, sw18);
            Place(ctx, 1, 3, Unit("G2Dummy", 1, 0, 9));
            {
                var dbg = At(ctx, 0, "Hrolf the Ironhowl");
                Console.WriteLine("     调试：在场 = " + (dbg != null)
                                + " · HasAnimTrigger(WhileInPlay) = " + (dbg != null && dbg.Card.HasAnimTrigger("WhileInPlay"))
                                + " · AnimTimes 行数 = " + (dbg != null && dbg.Card.AnimTimes != null ? dbg.Card.AnimTimes.Count : -1));
            }
            ctx.ClearSignals();
            // 一个真实动作（打一张战术）⇒ 顶层 `ActionScope` 退出时泵队排空 ⇒ 应发一条
            int hi = HandIdx(ctx, 0, "G2Dam1");
            Check(hi >= 0, "（前提）手里有那张伤害战术");
            int rc = hi >= 0 ? RuleCore.PlayTactic(ctx, 0, hi, 3) : -999;
            Console.WriteLine("     递归读数：PlayTactic rc = " + rc);
            int n1 = CountTrigger(ctx, "WhileInPlay", "Hrolf the Ironhowl");
            Console.WriteLine("     递归读数：一次动作后 `WhileInPlay` × " + n1 + " · 触发信号 = " + DumpTriggers(ctx));
            Check(n1 == 1, "★ 一次动作（队列排空一次）⇒ `Hrolf` **恰一条** `WhileInPlay`(20)"
                + "（实得 " + n1 + "）—— 🧨 把发射口删掉 / 改成 `Aura.Recompose` 里每算一次就发 ⇒ 本条红");

            // ---- ②-b 灭自证：SW18 **不在场** ⇒ 一条都不发 ----
            var ctx2 = NewCtx(pool, "SpaceWolves", t1);
            ToP1Turn(ctx2, 1);
            ctx2.Players[0].Energy = 20;
            Place(ctx2, 1, 3, Unit("G2Dummy2", 1, 0, 9));   // 靶子：让那张战术**真的打出去**
            ctx2.ClearSignals();
            int hi2 = HandIdx(ctx2, 0, "G2Dam1");
            if (hi2 >= 0) Console.WriteLine("     递归读数：②-b PlayTactic rc = "
                                          + RuleCore.PlayTactic(ctx2, 0, hi2, 3));
            Check(CountTrigger(ctx2, "WhileInPlay", null) == 0,
                  "★ 灭自证：`Hrolf` 不在场 ⇒ 全场**没有** `WhileInPlay`（实得 "
                + CountTrigger(ctx2, "WhileInPlay", null) + "）"
                + " —— 🧨 把发射口挂成「每个动作都无条件发一条」⇒ 本条红");

            // ---- ②-c 频率：两次动作 ⇒ 两条（不是每帧/每次重算都发） ----
            var t2 = new CardDef("G2Dam2", "G2Dam2", "tactic", "Deal 1 damage to an enemy troop", "common", "Test", 0, 0, 0, 0, null);
            var ctx3 = NewCtx(pool, "SpaceWolves", t1, t2);
            ToP1Turn(ctx3, 1);
            ctx3.Players[0].Energy = 20;
            Place(ctx3, 0, 6, sw18);
            Place(ctx3, 1, 3, Unit("G2Dummy3", 1, 0, 99));
            ctx3.ClearSignals();
            int a = HandIdx(ctx3, 0, "G2Dam1"), b = HandIdx(ctx3, 0, "G2Dam2");
            int n3 = 0;
            if (a >= 0) { RuleCore.PlayTactic(ctx3, 0, a, 3); n3 = CountTrigger(ctx3, "WhileInPlay", "Hrolf the Ironhowl"); }
            ctx3.ClearSignals();
            if (b >= 0) RuleCore.PlayTactic(ctx3, 0, b, 3);
            int n4 = CountTrigger(ctx3, "WhileInPlay", "Hrolf the Ironhowl");
            Console.WriteLine("     递归读数：第 1 次动作 × " + n3 + " · 第 2 次动作 × " + n4);
            Check(n3 == 1 && n4 == 1, "★ 频率 = **一次动作一条**（1 / 1，实得 " + n3 + " / " + n4 + "）"
                + " —— 🧨 改成每帧 / 每次 `Aura.Recompose` 都发 ⇒ 两个数都会 > 1");
        }

        // ================================================================
        //  ③ `questTimes`：`ReachedQuestCounter` 的消费点（原版语义 = 第 N 次触发才放行）
        // ================================================================
        Console.WriteLine("--- ③ `questTimes`（`EntityScript__ReachedQuestCounter.c` 的消费点）---");
        {
            // 人造夹具（现池 0 张卡有任务附魔 ⇒ 只能人造；⛔ 这不是「原版没有」）。
            // 附魔卡：`spellType == 120 (Quest)` + `questTimes = 3` + 一条 `Strike:` 正文。
            var quest = new CardDef("G2QUEST", "G2Quest", "tactic",
                                    "Strike: Deal 2 damage to an enemy troop", "common", "Test", 0, 0, 0, 0, null,
                                    spellType: 120, questTimes: 3);
            var bearer = Unit("G2Bearer", 1, 3, 40);
            var ctx = NewCtx(pool, "SpaceWolves");
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 3, bearer);
            var victim = Place(ctx, 1, 3, Unit("G2Victim", 1, 0, 40));
            var eff = Enchantments.Attach(ctx, u.Instance, quest, Enchantments.BuffTypeEnchantment);
            Check(eff != null, "（前提）任务附魔挂上了（`spellType = 120` · `questTimes = 3`）");
            Check(u.Instance.QuestCounter == 0, "（前提）载体 `QuestCounter` 从 0 起");

            // 用真触发口：`FireTriggerAt` 是 private ⇒ 走 `DeclareAttack` 的 Strike 那一跳
            // （`RuleCore.cs` 攻击尾部无条件 `FireTriggerAt(attacker, Strike, …)`）。
            // ⚠️ 夹具干预：攻击会耗掉本回合那一次行动 ⇒ 每发之前把 `Exhausted` / `AttacksThisTurn`
            //    归零（**只动夹具**，不碰引擎；真对局里这一步是回合推进做的）。
            // ⚠️ 读数要扣掉**攻击本身**的伤害（攻方近战 3）：
            //    `A[n] = 40 − 3·(n+1) − 2·max(0, n+1−3)`。
            int hp0 = victim.Health;
            int[] after = new int[5];
            for (int i = 0; i < 5; i++)
            {
                u.Exhausted = false;
                u.AttacksThisTurn = 0;
                int rc = RuleCore.DeclareAttack(ctx, 0, 3, 1, 3);
                after[i] = victim.Health;
                if (rc != RuleCodes.OK) Console.WriteLine("     !! 第 " + (i + 1) + " 次攻击 rc = " + rc);
            }
            Console.WriteLine("     递归读数：QuestCounter = " + u.Instance.QuestCounter
                            + " · 受击者生命 40 → " + string.Join(" → ", after));
            Check(u.Instance.QuestCounter == 5, "★ 每次触发都计数（5 次攻击 ⇒ 5，实得 "
                + u.Instance.QuestCounter + "）"
                + " —— 🧨 把计数写成「到 `questTimes` 就停」⇒ 本条红");
            Check(after[1] == hp0 - 6,
                  "★ 前 2 次（< `questTimes` = 3）**不放行**：附魔那条 ability 没结算"
                + "（40 → " + after[1] + "，只扣了攻击的 3+3）—— 🧨 把 `ReachedQuestCounter` 那道闸删掉 ⇒ 本条红");
            Check(after[2] == hp0 - 11,
                  "★ 第 3 次（= `questTimes`）**放行**：攻击 3 + 附魔 2（40 → " + after[2] + "）"
                + " —— 🧨 把 `>=` 写成 `>` ⇒ 第 3 次仍不放行、本格红");
            Check(after[3] == hp0 - 16 && after[4] == hp0 - 21,
                  "★ 达到之后**每次照常放行**（原版计数器不清零）（40 → " + after[3] + " → " + after[4] + "）");
        }

        Console.WriteLine(_fail == 0 ? "=== G2 探针：全部通过 ===" : "=== G2 探针：失败 " + _fail + " 条 ===");
        return _fail;
    }
}
