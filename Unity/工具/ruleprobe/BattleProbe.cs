using System;
using System.Collections.Generic;
using System.Linq;
using RuleEngine;

static class BattleProbe
{
    static CardDef Hero(string name, string faction) =>
        new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef Fixture(string name, int cost, int atk, int hp) =>
        new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, null, subtype: "Infantry");

    /// 顺序照 RuleEngineTest：督军在最前，其余倒序（抽牌是 pop_back）
    static List<CardDef> DeckOf(string fac, IEnumerable<CardDef> hand, int fillers = 20, int fillerCost = 1)
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", fac) };
        for (int i = 0; i < fillers; i++) d.Add(Fixture("filler" + i, fillerCost, 0, 1));
        var h = hand.ToList();
        for (int i = h.Count - 1; i >= 0; i--) d.Add(h[i]);
        return d;
    }

    static CardDef ByName(List<CardDef> pool, string n) =>
        pool.FirstOrDefault(x => x.Name.Equals(n, StringComparison.OrdinalIgnoreCase));

    public static void Jackal(List<CardDef> pool)
    {
        var js = ByName(pool, "Jackal Scout");
        var bait = ByName(pool, "Aerial Dominion");
        Console.WriteLine("== Jackal Scout inccost ==");
        Console.WriteLine("  " + js.Name + " desc=" + js.Desc);
        Console.WriteLine("  bait=" + bait.Name + " cost=" + bait.Cost + " type=" + bait.Type + " rarity=" + bait.Rarity);

        var ctx = RuleCore.NewBattle(DeckOf("Genestealers", new[] { js }),
                                     DeckOf("Test", new[] { bait, bait, bait }),
                                     seed: 0, shuffle: false, cardPool: pool);
        ctx.Players[0].Energy = 9; ctx.Players[0].MaxEnergy = 9;

        var hand = ctx.Players[0].Hand;
        int idx = hand.FindIndex(h => h.Card == js);
        Console.WriteLine("  hand0=[" + string.Join(",", hand.Select(h => h.Card.Name)) + "] idx=" + idx);
        Console.WriteLine("  hand1=[" + string.Join(",", ctx.Players[1].Hand.Select(h => h.Card.Name)) + "]");
        int rc = RuleCore.PlayCard(ctx, 0, idx, 5);
        Console.WriteLine("  PlayCard rc=" + rc);
        Console.WriteLine("  Log tail: " + Tail(ctx, 4));

        int baseCost = bait.Cost;
        var costs = ctx.Players[1].Hand.Select(h => new { h, c = RuleCore.CostOf(ctx, 1, h) })
                                     .Where(x => x.h.Card == bait).ToList();
        foreach (var x in costs)
            Console.WriteLine("    P1 手里 " + x.h + " cost=" + x.c + (x.c == baseCost + 1 ? "  ← +1" : ""));
        Console.WriteLine("  LastChosenCard = " + ctx.LastChosenCard);
        Console.WriteLine("  加价份数 = " + costs.Count(x => x.c == baseCost + 1)
                          + "（期望 1）; 命中 LastChosenCard = "
                          + costs.Any(x => ReferenceEquals(x.h, ctx.LastChosenCard) && x.c == baseCost + 1));
        // 模板级查询（拿不到「哪一份」）必须**看不到**这条按份的修正
        Console.WriteLine("  CostOf(按模板, P1, bait) = " + RuleCore.CostOf(ctx, 1, bait) + "（期望 " + baseCost + "）");
    }

    public static void Emergency(List<CardDef> pool)
    {
        var ed = ByName(pool, "Emergency Dispensation");
        var troop = ByName(pool, "Assault Intercessor Squad") ?? pool.First(x => x.IsUnit && x.Cost >= 4 && x.Faction == "Ultramarines");
        Console.WriteLine("== Emergency Dispensation ==");
        Console.WriteLine("  " + ed.Name + " desc=" + ed.Desc);
        Console.WriteLine("  troop=" + troop.Name + " cost=" + troop.Cost);

        // 牌库 = 督军 + 高费部队（候选，放在**深处**）+ 6 张战术垫牌 + Emergency Dispensation（顶端 ⇒ 起手抽到）
        var d0 = new List<CardDef> { Hero("FixtureWarlord", "TauEmpire") };
        d0.Add(troop);                       // 牌库里**唯一**的部队 ⇒ 候选确定
        for (int i = 0; i < 6; i++)
            d0.Add(new CardDef("tf" + i, "tf" + i, "tactic", "", null, "Test", 1, 0, 0, 0, null, subtype: "Spell"));
        d0.Add(ed);                          // 列表末尾 = 牌库顶 ⇒ 起手抽到它
        var ctx = RuleCore.NewBattle(d0, DeckOf("Test", new CardDef[0]),
                                     seed: 0, shuffle: false, cardPool: pool);
        ctx.Players[0].Energy = 9; ctx.Players[0].MaxEnergy = 9;
        Console.WriteLine("  hand0=[" + string.Join(",", ctx.Players[0].Hand.Select(h => h.Card.Name)) + "]");
        Console.WriteLine("  deck0=[" + string.Join(",", ctx.Players[0].Deck.Select(h => h.Card.Name)) + "]");

        int idx = ctx.Players[0].Hand.FindIndex(h => h.Card == ed);
        if (idx < 0) { Console.WriteLine("  !! Emergency Dispensation 没在手上"); return; }
        int rc = RuleCore.PlayTactic(ctx, 0, idx, -1);
        Console.WriteLine("  PlayTactic rc=" + rc);
        Console.WriteLine("  Log tail: " + Tail(ctx, 8));
        foreach (var h in ctx.Players[0].Hand)
            Console.WriteLine("    手里 " + h + " cost=" + RuleCore.CostOf(ctx, 0, h)
                              + "（印 " + h.Card.Cost + "）");
        Console.WriteLine("  LastChosenCard=" + ctx.LastChosenCard + " DrawnThisResolve="
                          + string.Join(",", ctx.DrawnThisResolve.Select(x => x.ToString())));
    }

    public static void Suppressor(List<CardDef> pool)
    {
        var sup = ByName(pool, "Suppressor");
        Console.WriteLine("== Suppressor 候选域 ==");
        var op = RuleCore.PlayerChooseOps(sup).FirstOrDefault(x => x.Verb == "choosecard");
        Console.WriteLine("  op: src=" + op.ChooseSrc + " what=[" + op.ChooseWhat + "]");
        var ctx = RuleCore.NewBattle(DeckOf("Ultramarines", new CardDef[0]), DeckOf("Test", new CardDef[0]),
                                     seed: 0, shuffle: false, cardPool: pool);
        string srcName, detail, why;
        var cands = RuleCore.ChooseCardCandidates(ctx, 0, op, out srcName, out detail, out why);
        Console.WriteLine("  candidates=" + cands.Count + " srcName=" + srcName + " why=" + (why ?? "-"));
    }

    static string Tail(BattleContext ctx, int n)
    {
        var e = ctx.Events;
        return string.Join(" | ", e.Skip(Math.Max(0, e.Count - n)));
    }
}

// ====================================================================
//  B19（2026-10-17）：本局打出过的牌 —— 离线真跑（不占 Unity 实例）
// ====================================================================
static class BattleProbe19
{
    static CardDef TacticCard(string name, string rarity) =>
        new CardDef(name, name, "tactic", "Refill 1 Energy", rarity, "Test", 0, 0, 0, 0, null);
    static CardDef UnitCard(string name) =>
        new CardDef(name, name, "unit", "", null, "Test", 0, 1, 1, 0, null);
    /// 「本局打出过的那张非传奇战略回到手里」—— **合成一张**（真卡 `Suppressor` 的 `act` 解不出来，见报告）
    static CardDef ChooserCard() =>
        new CardDef("B19Chooser", "B19Chooser", "unit",
                    "Rally: Choose a non-legendary Stratagem you played this game and put it in your hand",
                    "common", "Test", 0, 1, 1, 0, null, subtype: "Infantry", fromOriginalPool: true);

    static CardDef HeroOf(string name, string fac) =>
        new CardDef(name, name, "hero", "", null, fac, 0, 2, 30, 0, null, subtype: "Warlord");
    static List<CardDef> DeckOf2(string fac, params CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("FixtureWarlord", fac) };
        for (int i = 0; i < 20; i++) d.Add(new CardDef("f" + i, "f" + i, "unit", "", null, "Test", 1, 0, 1, 0, null));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static string Names(List<CardDef> l) => l.Count == 0 ? "(空)" : string.Join(",", l.Select(x => x.Name));
    static string W(string why) => why == null ? "-" : why;

    static string Table(BattleContext ctx)
    {
        return string.Join(" | ", ctx.PlayedCards.Select(p => p.ToString()));
    }

    public static void Run(List<CardDef> pool)
    {
        Console.WriteLine("== B19 本局打出过的牌 ==");
        var seg = EffectText.ParseSegment("Choose a non-Legendary Stratagem you played this game and return it to your hand");
        var op = seg.Ops[0];
        Console.WriteLine("  真卡那句: kind=" + seg.Kind + " src=" + op.ChooseSrc
                          + " what=[" + op.ChooseWhat + "] act=[" + op.ChooseAct + "]");

        // 带动作的合成句（真卡的 `act` 解不出来 ⇒ 用这句才测得到取走那一步）
        var seg2 = EffectText.ParseSegment("Choose a non-legendary Stratagem you played this game and put it in your hand");
        var op2 = seg2.Ops[0];
        Console.WriteLine("  合成那句: kind=" + seg2.Kind + " src=" + op2.ChooseSrc
                          + " what=[" + op2.ChooseWhat + "] act=[" + op2.ChooseAct + "]");

        var A = TacticCard("B19StratA", "common");
        var C = TacticCard("B19StratC", "common");
        var L = TacticCard("B19StratL", "legendary");
        var U = UnitCard("B19Unit");
        var B = TacticCard("B19StratB", "common");
        var chooser = ChooserCard();

        var ctx = RuleCore.NewBattle(DeckOf2("Test", A, C, L, U, chooser),
                                     DeckOf2("Test", B),
                                     seed: 0, shuffle: false, cardPool: pool);
        string sn, dt, wy;
        Console.WriteLine("  hand0=[" + string.Join(",", ctx.Players[0].Hand.Select(h => h.Card.Name)) + "]");
        Console.WriteLine("  hand1=[" + string.Join(",", ctx.Players[1].Hand.Select(h => h.Card.Name)) + "]");

        // ① 一局都没打过
        var c0 = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        Console.WriteLine("  ① 空表: 候选=" + c0.Count + " why=" + W(wy));

        // ② P0 打 A（非传奇战术）
        ctx.Players[0].Energy = 9;
        int iA = ctx.Players[0].Hand.FindIndex(h => h.Card == A);
        Console.WriteLine("  ② PlayTactic(A) rc=" + RuleCore.PlayTactic(ctx, 0, iA, -1) + "  表: " + Table(ctx));
        var cA = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        Console.WriteLine("     候选=" + Names(cA) + " why=" + W(wy));

        // ③ 换手：P1 打 B
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);
        ctx.Players[1].Energy = 9;
        int iB = ctx.Players[1].Hand.FindIndex(h => h.Card == B);
        Console.WriteLine("  ③ PlayTactic(B,P1) rc=" + RuleCore.PlayTactic(ctx, 1, iB, -1) + "  表: " + Table(ctx));
        var cB = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        Console.WriteLine("     P0 候选=" + Names(cB) + "（期望只有 A）");

        // ④ 回到 P0，打 C（更晚）⇒ 候选只该剩 C
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 9;
        int iC = ctx.Players[0].Hand.FindIndex(h => h.Card == C);
        Console.WriteLine("  ④ PlayTactic(C) rc=" + RuleCore.PlayTactic(ctx, 0, iC, -1) + "  表: " + Table(ctx));
        var cC = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        Console.WriteLine("     候选=" + Names(cC) + "（期望只有 C = 最近打出的）");

        // ⑤ 打一张**传奇**战略 ⇒ 候选仍该是 C
        int iL = ctx.Players[0].Hand.FindIndex(h => h.Card == L);
        Console.WriteLine("  ⑤ PlayTactic(L,传奇) rc=" + RuleCore.PlayTactic(ctx, 0, iL, -1));
        var cL = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        Console.WriteLine("     候选=" + Names(cL) + "（期望仍是 C）");

        // ⑥ 打一张**单位** ⇒ 候选仍该是 C，但表里要有 Tactic=false 那一条
        int iU = ctx.Players[0].Hand.FindIndex(h => h.Card == U);
        Console.WriteLine("  ⑥ PlayCard(U,单位) rc=" + RuleCore.PlayCard(ctx, 0, iU, 3));
        var cU = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        Console.WriteLine("     候选=" + Names(cU) + "（期望仍是 C）  表: " + Table(ctx));

        // ⑦ 那一份 = 弃牌堆里那一份（判据 = 引用相等）
        var rowC = ctx.PlayedCards.LastOrDefault(p => p.Card == C);
        bool sameAsDiscard = rowC != null && rowC.Instance != null
                             && ctx.Players[0].Discard.Any(h => ReferenceEquals(h, rowC.Instance));
        Console.WriteLine("  ⑦ C 那一条的实例 == 弃牌堆里那一份 ? " + sameAsDiscard);

        // ⑧ 端到端：合成一张「把本局打出过的非传奇战略收回手」的单位，真打它
        for (int k = 0; k < 8 && ctx.Players[0].Hand.FindIndex(h => h.Card == chooser) < 0; k++)
        { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
        ctx.Players[0].Energy = 9;
        int iCh = ctx.Players[0].Hand.FindIndex(h => h.Card == chooser);
        int handBefore = ctx.Players[0].Hand.Count;
        int free = -1;
        for (int s2 = 0; s2 < BoardSpec.Size; s2++) if (ctx.Players[0].Board[s2] == null) { free = s2; break; }
        Console.WriteLine("     chooser idx=" + iCh + " 空格=" + free + " 回合=" + ctx.Turn + " active=" + ctx.Active);
        int rc = RuleCore.PlayCard(ctx, 0, iCh, free);
        var got = ctx.LastChosenCard;
        Console.WriteLine("  ⑧ PlayCard(chooser) rc=" + rc + " 挑中=" + (got == null ? "null" : got.ToString()));
        Console.WriteLine("     chooser 的 ask: " + string.Join(";",
            RuleCore.PlayerChooseOps(chooser).Select(x => x.ChooseSrc + "/" + x.ChooseWhat + "/" + x.ChooseAct)));
        Console.WriteLine("     挑中那一份 == 表里 C 的实例 ? " + (got != null && rowC != null && ReferenceEquals(got, rowC.Instance)));
        Console.WriteLine("     它现在在手里 ? " + (got != null && ctx.Players[0].Hand.Any(h => ReferenceEquals(h, got))));
        Console.WriteLine("     它还在弃牌堆里 ? " + (got != null && ctx.Players[0].Discard.Any(h => ReferenceEquals(h, got))));
        Console.WriteLine("     弃牌堆张数 " + ctx.Players[0].Discard.Count + " · 手牌张数 " + handBefore + "→" + ctx.Players[0].Hand.Count);
        Console.WriteLine("  表: " + Table(ctx));
        Console.WriteLine("  日志尾: " + string.Join(" | ", ctx.Events.Skip(Math.Max(0, ctx.Events.Count - 4))));
    }
}

// 与 `RuleEngineTest.TestPlayedTableB19` **逐个夹具、逐个动作对齐**的离线复跑
static class B19Test
{
    static int Pass, Fail;
    static void T(bool ok, string msg)
    {
        if (ok) { Pass++; }
        else { Fail++; Console.WriteLine("    FAIL: " + msg); }
    }
    static void Eq(int got, int want, string msg) { T(got == want, msg + "（got " + got + " want " + want + "）"); }
    static void Yes(bool ok, string msg) { T(ok, msg); }

    static CardDef HeroOf(string name, string fac) =>
        new CardDef(name, name, "hero", "", null, fac, 0, 2, 30, 0, null, subtype: "Warlord");
    static BattleContext ProbeBattle(CardDef[] h0, CardDef[] h1)
    {
        var d0 = new List<CardDef> { HeroOf("FixtureWarlord", "Ultramarines") };
        for (int i = h0.Length - 1; i >= 0; i--) d0.Add(h0[i]);
        var d1 = new List<CardDef> { HeroOf("FixtureWarlord", "Goff") };
        for (int i = h1.Length - 1; i >= 0; i--) d1.Add(h1[i]);
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }
    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i].Card.Name == name) return i;
        return -1;
    }
    static int FirstFree(PlayerState p)
    {
        for (int i = 0; i < BoardSpec.Size; i++) if (p.Board[i] == null) return i;
        return -1;
    }
    static void PassTurn(BattleContext ctx) { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }

    public static int Run(List<CardDef> pool)
    {
        Pass = 0; Fail = 0;
        Console.WriteLine("== B19 :: 照 `TestPlayedTableB19` 复跑 ==");
        var A = new CardDef("B19StratA", "B19StratA", "tactic", "Refill 1 Energy", "common", "Test", 0, 0, 0, 0, null);
        var C = new CardDef("B19StratC", "B19StratC", "tactic", "Refill 1 Energy", "common", "Test", 0, 0, 0, 0, null);
        var B = new CardDef("B19StratB", "B19StratB", "tactic", "Refill 1 Energy", "common", "Test", 0, 0, 0, 0, null);
        var L = new CardDef("B19StratL", "B19StratL", "tactic", "Refill 1 Energy", "legendary", "Test", 0, 0, 0, 0, null);
        var U = new CardDef("B19Unit", "B19Unit", "unit", "", null, "Test", 0, 1, 1, 0, null);
        var chooser = new CardDef("B19Chooser", "B19Chooser", "unit",
                                  "Rally: Choose a non-legendary Stratagem you played this game and put it in your hand",
                                  "common", "Test", 0, 1, 1, 0, null, subtype: "Infantry", fromOriginalPool: true);

        var op = EffectText.ParseSegment("Choose a non-Legendary Stratagem you played this game and return it to your hand").Ops[0];

        var ctx = ProbeBattle(new[] { A, C, L, U, chooser }, new[] { B });
        ctx.CardPool = pool;
        ctx.Players[0].Energy = 9; ctx.Players[0].MaxEnergy = 9;
        ctx.Players[1].Energy = 9; ctx.Players[1].MaxEnergy = 9;

        string sn, dt, why;
        Eq(ctx.PlayedCards.Count, 0, "① 新开一局表是空的");
        var c0 = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out why);
        Eq(c0.Count, 0, "① 候选 0");
        Yes(why != null, "① why 非空");
        Yes(why != null && why.IndexOf("还没有记账表") < 0, "① 理由不再是「还没有记账表」");
        string d2, w2;
        var byFilter = CreatePool.FilterChoose(pool, pool, op.ChooseWhat, out d2, out w2);
        Yes(byFilter.Count > 0, "① 全卡池能筛出 " + byFilter.Count + " 张");

        int iA = HandIdx(ctx, 0, "B19StratA");
        Yes(iA >= 0, "② A 在起手");
        Eq(RuleCore.PlayTactic(ctx, 0, iA, -1), RuleCodes.OK, "② 打出 A");
        Eq(ctx.PlayedCards.Count, 1, "② 表里恰好一笔");
        Eq(ctx.PlayedCards[0].Owner, 0, "② Owner");
        Yes(ReferenceEquals(ctx.PlayedCards[0].Card, A), "② Card");
        Yes(ctx.PlayedCards[0].Tactic, "② Tactic=true");
        Eq(ctx.PlayedCards[0].Turn, ctx.Turn, "② Turn");
        Yes(ctx.PlayedCards[0].Instance != null && ctx.Players[0].Discard.Count > 0
            && ReferenceEquals(ctx.Players[0].Discard[ctx.Players[0].Discard.Count - 1], ctx.PlayedCards[0].Instance),
            "② 实例 == 弃牌堆末尾那一份");
        var cA = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out why);
        Eq(cA.Count, 1, "② 候选 1");
        Yes(cA.Count == 1 && ReferenceEquals(cA[0], A), "② 就是 A");
        Yes(why == null, "② why == null");

        PassTurn(ctx);
        ctx.Players[1].Energy = 9;
        int iB = HandIdx(ctx, 1, "B19StratB");
        Yes(iB >= 0, "③ B 在 P1 起手");
        Eq(RuleCore.PlayTactic(ctx, 1, iB, -1), RuleCodes.OK, "③ P1 打出 B");
        Eq(ctx.PlayedCards.Count, 2, "③ 表里两条");
        Eq(ctx.PlayedCards[1].Owner, 1, "③ 第二条 Owner=1");
        var cB = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out why);
        Eq(cB.Count, 1, "③ P0 候选还是 1");
        Yes(cB.Count == 1 && ReferenceEquals(cB[0], A), "③ 只有自己那张");
        var cB1 = RuleCore.ChooseCardCandidates(ctx, 1, op, out sn, out dt, out why);
        Yes(cB1.Count == 1 && ReferenceEquals(cB1[0], B), "③ P1 的候选是他自己的 B");
        Yes(ctx.PlayedCards[1].Seq > ctx.PlayedCards[0].Seq, "③ 流水号递增");

        PassTurn(ctx);
        ctx.Players[0].Energy = 9;
        int iC = HandIdx(ctx, 0, "B19StratC");
        Yes(iC >= 0, "④ C 还在手上");
        Eq(RuleCore.PlayTactic(ctx, 0, iC, -1), RuleCodes.OK, "④ 打出 C");
        Eq(ctx.PlayedCards.Count, 3, "④ 表里三条");
        var cC = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out why);
        Eq(cC.Count, 1, "④ 至多一张");
        Yes(cC.Count == 1 && ReferenceEquals(cC[0], C), "④ 是最近那张 C");

        int iL = HandIdx(ctx, 0, "B19StratL");
        Yes(iL >= 0, "⑤ L 在手上");
        Eq(RuleCore.PlayTactic(ctx, 0, iL, -1), RuleCodes.OK, "⑤ 打出传奇 L");
        var cL = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out why);
        Yes(cL.Count == 1 && ReferenceEquals(cL[0], C), "⑤ 传奇不进候选（仍是 C）");

        int iU = HandIdx(ctx, 0, "B19Unit");
        Yes(iU >= 0, "⑤ U 在手上");
        Eq(RuleCore.PlayCard(ctx, 0, iU, FirstFree(ctx.Players[0])), RuleCodes.OK, "⑤ 打出单位 U");
        var cU = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out why);
        Yes(cU.Count == 1 && ReferenceEquals(cU[0], C), "⑤ 单位不进候选（仍是 C）");
        var rowU = ctx.PlayedCards[ctx.PlayedCards.Count - 1];
        Yes(ReferenceEquals(rowU.Card, U) && !rowU.Tactic, "⑤ 单位那笔进表且 Tactic=false");

        for (int k = 0; k < 8 && HandIdx(ctx, 0, "B19Chooser") < 0; k++) PassTurn(ctx);
        int iCh = HandIdx(ctx, 0, "B19Chooser");
        Yes(iCh >= 0, "⑥ chooser 抽上手了（回合 " + ctx.Turn + "）");
        if (iCh >= 0)
        {
            ctx.Players[0].Energy = 9;
            int discBefore = ctx.Players[0].Discard.Count;
            int handBefore = ctx.Players[0].Hand.Count;
            var rowC = ctx.PlayedCards.Find(p => ReferenceEquals(p.Card, C));
            Yes(rowC != null, "⑥ 表不清：C 那条还在");
            Eq(RuleCore.PlayCard(ctx, 0, iCh, FirstFree(ctx.Players[0])), RuleCodes.OK, "⑥ 打出 chooser");
            var got = ctx.LastChosenCard;
            Yes(got != null && rowC != null && ReferenceEquals(got, rowC.Instance), "⑥ got == 表里那一份");
            Yes(got != null && ctx.Players[0].Hand.Contains(got), "⑥ 回到手牌");
            Yes(got != null && !ctx.Players[0].Discard.Contains(got), "⑥ 从弃牌堆摘走了");
            Eq(ctx.Players[0].Discard.Count, discBefore - 1, "⑥ 弃牌堆 −1");
            Eq(ctx.Players[0].Hand.Count, handBefore, "⑥ 手牌净 0");
            Yes(ctx.PlayedCards.Exists(p => ReferenceEquals(p.Card, C)), "⑥ 收回后 C 那条仍在表里");
        }
        Console.WriteLine("  ==> PASS " + Pass + " · FAIL " + Fail);
        return Fail;
    }
}
