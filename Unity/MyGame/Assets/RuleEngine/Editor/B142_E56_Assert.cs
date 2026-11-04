// B142_E56_Assert.cs — 批次 **B142** · 执行写手 `E56`（`B142-DEAD-WINDOW`）＋ `E75`（同族第二处：`ReturnFromCemetery`，见 B 组）
//
// ============================ 它盯的是什么 ============================
//   `EffectResolver.DeadCandidates`（`choose … died …` 的候选口）改前对 `since_last_turn` 那一档
//   还按 `DeathTurn >= LastTurnStartMark` 划窗口。**已裁定照原版**：原版
//   `BattleManager__GetCardsInCemetery.c` 全文**没有**死亡回合这一维（窗口只出现在卡面文字里），
//   ⇒ 这一档现在**取整座墓地**，与 `all` 档同集合。
//   ⚠️ 同族的 `ReturnFromCemetery`（`EffectResolver.cs:6957`）**本件没动**，那是另一条待裁项
//      ⇒ 🔴 2026-11-04 `B142-E75` 已按**同一判据**收掉（见本文件 B 组）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A3/A6：把 `DeadCandidates` 的窗口那一行加回去 ⇒ 「更早死」夹具变 0 张 ⇒ A3/A6 红。
//   · A2/A5（前提）：夹具里那张 `DeathTurn = 2` **确实 < mark = 5** ⇒ 旧窗口必然会把它筛掉；
//     这条保证 A3/A6 的 1 张不是「夹具压根没落进窗口外」的空转。
//   · A4/A7（判别式）：本文件复刻了一份**旧窗口**口径（`LegacyWindowCount`），同一份夹具上必须给出 0
//     ⇒ 证明夹具真有鉴别力，而不是两档都恒 1。
//   · A8/A9：两张死牌（本回合 + 更早）⇒ 2 张；删掉窗口后仍然是 2（`all` 档口径不变）。
//   · B4（判别式，`B142-E75`）：本文件复刻了一份**改前 `ReturnFromCemetery` 的窗口**口径（`LegacyReturnWindowCount`），
//     同一份 3 张夹具上必须只给 1 ⇒ 证明 B5 的 3 张是真变化；把 `EffectResolver.cs:6957` 那行
//     `since_last_turn && d.DeathTurn < mark` 加回去 ⇒ B4 变 3、B5 变 1 ⇒ 两条一起红。
//   · B6：两路口径只要有一边重新加窗口 ⇒ 集合/顺序必然不同 ⇒ 红（这条是「同判据」的直接判别式）。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行追加**（落在该行第一个 `//` 之前）：
//     `Guard("B142E56_Assert", B142_E56_Assert.Run); _pass += B142_E56_Assert.Pass; _fail += B142_E56_Assert.Fail;`
//   （接线由调度台统一做；本写手不改宿主文件。）
//
// ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `RuleCore.NewBattle` 的内存局面。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B142-E56 的断言（独立类：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B142_E56_Assert
{
    static int _pass, _fail;

    /// <summary>离线取证用的卡池注入窄口（同 `B141_W77_Assert.PoolOverride`；Unity 侧恒 `null` ⇒ 走真装载链）。
    /// ⛔ 生产侧别写它。</summary>
    public static List<CardDef> PoolOverride;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B142-E56：`DeadCandidates` 的 `since_last_turn` 窗口已收掉（原版 `GetCardsInCemetery.c` 无该维）");
        GroupA_NoDeathTurnWindow();
        Section("🆕 B142-E75：`ReturnFromCemetery` 同一口径 —— `since_last_turn` 窗口已收掉，与 `DeadCandidates` 同集合");
        GroupB_ReturnFromCemeterySameSet();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                                                 (IEnumerable<string>)null, subtype: "Infantry");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B142E56Warlord0") };
        var d1 = new List<CardDef> { Hero("B142E56Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>一张「已进墓地」的单位卡：实例进 `Discard`（= 墓地列表）＋一条阵亡登记（带 `DeathTurn`）。</summary>
    static CardInstance CemeteryCard(BattleContext ctx, CardDef card, int side, int deathTurn)
    {
        var inst = ctx.NewInstance(card);
        ctx.Players[side].Discard.Add(inst);
        ctx.DeadUnits.Add(new DeadUnit { Card = card, Instance = inst, Owner = side, DeathTurn = deathTurn });
        return inst;
    }

    /// <summary>**改前的窗口口径**（只为本文件当判别式用；⛔ 生产侧不调，也⛔ 别拿它当实现）。
    /// 它存在的唯一目的：证明「同一份夹具上旧口径会给出 0 张」⇒ 新口径的 1 张是真变化。</summary>
    static int LegacyWindowCount(BattleContext ctx, int owner, string scope)
    {
        if (ctx == null) return 0;
        int mark = ctx.Players[owner].LastTurnStartMark;
        int n = 0;
        foreach (var d in ctx.DeadUnits)
        {
            if (d == null || d.Owner != owner || d.Card == null || !d.Card.IsUnit) continue;
            if (scope == "since_last_turn" && (d.DeathTurn < 0 || d.DeathTurn < mark)) continue;
            n++;
        }
        return n;
    }

    static string Names(List<CardDef> list)
    {
        if (list == null || list.Count == 0) return "（空）";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append("、");
            sb.Append(list[i] != null ? list[i].Name : "null");
        }
        return sb.ToString();
    }

    // ==================================================================
    //  A 组：`since_last_turn` 不再按死亡回合筛（两张真卡各跑一遍）
    // ==================================================================
    static void GroupA_NoDeathTurnWindow()
    {
        Section("A `since_last_turn` 候选 = 整座墓地（「本回合刚死」与「更早死」同集合）");

        var pool = PoolOverride ?? CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0,
                  "A0 前提：卡池可读（`CardDatabase.Load()` / `PoolOverride`；离线探针必须注入 PoolOverride）");
        if (pool == null) return;

        var ids = new[] { "EC39", "SOR42" };   // 池内两张 `since_last_turn` 的 `choose … died …`（卡面亲读）
        foreach (var id in ids)
        {
            var card = CardDatabase.FindById(pool, id);
            CheckTrue(card != null, "A0b 前提：" + id + " 在池内");
            if (card == null) continue;

            EffectOp chooseOp = null;
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            foreach (var o in ops)
                if (o != null && o.Verb == "choosecard" && o.ChooseSrc == "dead") { chooseOp = o; break; }
            CheckTrue(chooseOp != null, "A1 ★ " + id + " 的正文解析出 `choosecard` / `ChooseSrc = dead`（走的就是本件改的那条路）");
            if (chooseOp == null) continue;
            Check(chooseOp.ChooseDeadScope, "since_last_turn",
                  "A1b ★ " + id + " 的窗口档 = `since_last_turn`（卡面 `" + card.Desc + "`）⇒ 正是被收掉窗口的那一档");

            // 夹具统一：`mark = 5`（= 上一次自己回合开始），`ctx.Turn = 5`。
            int mark = 5;

            // ---- 夹具 ①「本回合刚死」：DeathTurn = 5 ⇒ 新旧口径都给 1 ----
            var ctxA = NewCtx();
            ctxA.Turn = mark;
            ctxA.Players[0].LastTurnStartMark = mark;
            var thisTurnCard = Unit("B142E56_" + id + "_ThisTurn");
            CemeteryCard(ctxA, thisTurnCard, 0, deathTurn: mark);
            var prevSrcA = ctxA.PushCreationSource(card);
            List<CardDef> sinceA;
            try { sinceA = RuleCore.DeadCandidatesForTest(ctxA, 0, "since_last_turn", chooseOp); }
            finally { ctxA.CreationSource = prevSrcA; }
            Check(sinceA.Count, 1, "A2 ★ " + id + " 夹具①「本回合刚死」⇒ 1 张（实际 = " + Names(sinceA) + "）");

            // ---- 夹具 ②「更早死」：DeathTurn = 2 < mark = 5 ⇒ 旧口径会筛掉，新口径必须仍给 1 ----
            var ctxB = NewCtx();
            ctxB.Turn = mark;
            ctxB.Players[0].LastTurnStartMark = mark;
            var earlierCard = Unit("B142E56_" + id + "_Earlier");
            CemeteryCard(ctxB, earlierCard, 0, deathTurn: 2);
            CheckTrue(2 < ctxB.Players[0].LastTurnStartMark,
                      "A3 前提（灭自证）：夹具②那张 `DeathTurn = 2` **确实 < mark = 5** ⇒ 旧窗口一定会把它筛掉");
            Check(LegacyWindowCount(ctxB, 0, "since_last_turn"), 0,
                  "A4 ★ 判别式：**旧口径**在同一份夹具②上给出 0 张（⇒ 下面的 1 张是真变化，不是空转）");
            var prevSrcB = ctxB.PushCreationSource(card);
            List<CardDef> sinceB;
            try { sinceB = RuleCore.DeadCandidatesForTest(ctxB, 0, "since_last_turn", chooseOp); }
            finally { ctxB.CreationSource = prevSrcB; }
            Check(sinceB.Count, 1,
                  "A5 ★★ " + id + " 夹具②「更早死」⇒ **仍然 1 张**（窗口已收掉；实际 = " + Names(sinceB) + "）");
            CheckTrue(sinceB.Count == 1 && ReferenceEquals(sinceB[0], earlierCard),
                  "A5b ★★ 夹具②留下的正是「更早死」那张（不是按名字/顺序蒙的）");
            CheckTrue(sinceA.Count == sinceB.Count && sinceA.Count == 1,
                  "A6 ★★ 两种夹具**同集合规模**（各 1 张）—— 死亡回合不再影响候选（铁律：原版无该维）");

            // ---- 夹具 ③ 两张都在：本回合 + 更早 ⇒ 2 ----
            var ctxC = NewCtx();
            ctxC.Turn = mark;
            ctxC.Players[0].LastTurnStartMark = mark;
            CemeteryCard(ctxC, Unit("B142E56_" + id + "_C1"), 0, deathTurn: mark);
            CemeteryCard(ctxC, Unit("B142E56_" + id + "_C2"), 0, deathTurn: 2);
            var prevSrcC = ctxC.PushCreationSource(card);
            List<CardDef> sinceC, allC;
            try
            {
                sinceC = RuleCore.DeadCandidatesForTest(ctxC, 0, "since_last_turn", chooseOp);
                allC = RuleCore.DeadCandidatesForTest(ctxC, 0, "all", chooseOp);
            }
            finally { ctxC.CreationSource = prevSrcC; }
            Check(sinceC.Count, 2, "A7 ★★ 两张死牌（本回合 + 更早）⇒ 2 张（实际 = " + Names(sinceC) + "）");
            Check(allC.Count, 2, "A8 ★ `all` 分支保持原口径：同样 2 张（实际 = " + Names(allC) + "）");
            CheckTrue(sinceC.Count == allC.Count,
                  "A9 ★ `since_last_turn` 与 `all` 现在**同集合**（这正是「原版无死亡回合维」的直接推论）");
        }
    }

    // ==================================================================
    //  B 组（2026-11-04 `B142-E75`）：`ReturnFromCemetery` 与 `DeadCandidates` 同集合
    //  —— 同一个判据（原版 `GetCardsInCemetery.c` 无死亡回合维），同一份夹具，两条路必须给出同一批卡。
    // ==================================================================
    static EffectTargetSpec TroopSpec()
        => new EffectTargetSpec { Raw = "your troop", Side = "own", Kind = "troop", Count = 1 };

    static EffectOp ReturnOp(EffectTargetSpec spec)
        => new EffectOp
        {
            Verb = "return", Source = "B142E75Fixture", Dest = "decktop", Amount = 0,
            Target = spec, SubjectTargetsAffected = 60,
        };

    static EffectOp DeadOp(string scope)
        => new EffectOp
        {
            Verb = "choosecard", Source = "B142E75Choose", ChooseSrc = "dead", ChooseDeadScope = scope,
        };

    /// <summary>**改前 `ReturnFromCemetery` 的窗口口径**（只为本组当判别式；⛔ 生产侧不调）。
    /// 存在的唯一目的：证明「同一份夹具上旧口径只给 1 张」⇒ 新口径的 3 张是真变化。</summary>
    static int LegacyReturnWindowCount(BattleContext ctx, int owner, int mark)
    {
        if (ctx == null) return 0;
        int n = 0;
        foreach (var d in ctx.DeadUnits)
        {
            if (d == null || d.Owner != owner || d.Card == null || !d.Card.IsUnit) continue;
            if (d.DeathTurn < mark) continue;      // ← 改前 `since_last_turn` 那一行
            n++;
        }
        return n;
    }

    static string NamesOfInstances(List<CardInstance> list)
    {
        if (list == null || list.Count == 0) return "（空）";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append("、");
            sb.Append(list[i] != null && list[i].Card != null ? list[i].Card.Name : "null");
        }
        return sb.ToString();
    }

    static void GroupB_ReturnFromCemeterySameSet()
    {
        Section("B `ReturnFromCemetery` 的 `since_last_turn` 窗口已收掉（原版 `BattleManager__GetCardsInCemetery.c` 无该维）");

        int mark = 5;
        var spec = TroopSpec();

        // ---- 夹具（候选口 / 消费端各一份，逐字相同）：本回合刚死 + 更早死 + 没有死亡回合（`-1`）----
        var ctxChoose = NewCtx();
        ctxChoose.Turn = mark;
        ctxChoose.Players[0].LastTurnStartMark = mark;
        CemeteryCard(ctxChoose, Unit("B142E75_ThisTurn"), 0, deathTurn: mark);
        CemeteryCard(ctxChoose, Unit("B142E75_Earlier"), 0, deathTurn: 2);
        CemeteryCard(ctxChoose, Unit("B142E75_NoTurn"), 0, deathTurn: -1);
        Check(ctxChoose.DeadUnits.Count, 3, "B0 ★ 前提：夹具 3 条阵亡登记（`DeathTurn = 5 / 2 / -1`）");
        Check(ctxChoose.Players[0].Discard.Count, 3, "B0b ★ 前提：3 张都在墓地列表（`Discard`）里 —— 下面读不到不会是「压根不在墓地」");
        CheckTrue(2 < ctxChoose.Players[0].LastTurnStartMark,
                  "B1 ★ 前提（灭自证）：「更早死」那张 `DeathTurn = 2` **确实 < mark = 5** ⇒ 旧窗口必然筛掉它");

        List<CardDef> chooseList;
        var prevSrc = ctxChoose.PushCreationSource(null);
        try { chooseList = RuleCore.DeadCandidatesForTest(ctxChoose, 0, "since_last_turn", DeadOp("since_last_turn")); }
        finally { ctxChoose.CreationSource = prevSrc; }
        Check(chooseList.Count, 3, "B2 ★★ 候选口 `DeadCandidates(since_last_turn)` = 3 张（实际 = " + Names(chooseList) + "）");

        var ctxRet = NewCtx();
        ctxRet.Turn = mark;
        ctxRet.Players[0].LastTurnStartMark = mark;
        var iThis = CemeteryCard(ctxRet, Unit("B142E75_ThisTurn"), 0, deathTurn: mark);
        var iEarlier = CemeteryCard(ctxRet, Unit("B142E75_Earlier"), 0, deathTurn: 2);
        var iNoTurn = CemeteryCard(ctxRet, Unit("B142E75_NoTurn"), 0, deathTurn: -1);
        Check(ctxRet.DeadUnits.Count, 3, "B3 ★ 前提：消费端夹具与候选口**逐字相同**（3 条阵亡登记）");

        Check(LegacyReturnWindowCount(ctxRet, 0, mark), 1,
              "B4 ★★ 判别式：**改前的窗口口径**在同一份夹具上只给 1 张（⇒ 下面 3 张是真变化，不是空转）"
            + "｜🧨 把 `:6957` 那行 `d.DeathTurn < mark` 加回去 ⇒ 实得 1 ⇒ B5 红");

        var moved = new List<CardInstance>();
        int n = RuleCore.ReturnFromCemeteryForTest(ctxRet, 0, "E75", ReturnOp(spec), spec, "since_last_turn", moved);
        Check(n, 3, "B5 ★★ `ReturnFromCemetery(since_last_turn)` 搬走 **3 张**（窗口已收掉；实际 = " + NamesOfInstances(moved) + "）");
        Check(moved.Count, 3, "B5b 搬走表也是 3 张");
        CheckTrue(ctxRet.Players[0].Deck.Contains(iThis) && ctxRet.Players[0].Deck.Contains(iEarlier)
                  && ctxRet.Players[0].Deck.Contains(iNoTurn),
                  "B5c ★★ 「更早死」与「没有死亡回合（`-1`）」那两张**真的进了牌库**"
                + "（改前它们会被窗口挡在墓地；原版 `GetCardsInCemetery` 照样收）");

        // ---- 同集合：两条路的输出逐字相同（顺序也相同 = 进墓先后）----
        Check(NamesOfInstances(moved), Names(chooseList),
              "B6 ★★ `ReturnFromCemetery` 与 `DeadCandidates` 在**同一份夹具**上给出**同一批卡、同一顺序**"
            + "（同判据的直接推论：原版两条路都只是 `GetCardsInCemetery`）");

        // ---- `all` 档保持原口径：分档不再改变结果 ----
        var ctxAll = NewCtx();
        ctxAll.Turn = mark;
        ctxAll.Players[0].LastTurnStartMark = mark;
        CemeteryCard(ctxAll, Unit("B142E75_ThisTurn"), 0, deathTurn: mark);
        CemeteryCard(ctxAll, Unit("B142E75_Earlier"), 0, deathTurn: 2);
        CemeteryCard(ctxAll, Unit("B142E75_NoTurn"), 0, deathTurn: -1);
        var movedAll = new List<CardInstance>();
        int nAll = RuleCore.ReturnFromCemeteryForTest(ctxAll, 0, "E75", ReturnOp(spec), spec, "all", movedAll);
        Check(nAll, 3, "B7 ★ `all` 档保持原口径：同样 3 张（实际 = " + NamesOfInstances(movedAll) + "）");
        Check(NamesOfInstances(movedAll), NamesOfInstances(moved),
              "B8 ★ `since_last_turn` 与 `all` 现在**同集合**（这正是「原版无死亡回合维」的直接推论）");
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B142_E56 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B142_E56 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
