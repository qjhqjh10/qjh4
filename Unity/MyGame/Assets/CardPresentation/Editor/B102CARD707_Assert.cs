// B102CARD707_Assert.cs — 执行代理 `B102_707`（2026-10-30）· 账 `战斗逻辑驱动:707` `CardScript.CardSetup` 的 R3/R4
//
// ============================ 它盯的是什么 ============================
//   原版 `CardScript__CardSetup.c:251-260`（2026-10-30 现读）在建卡那一刻做两个**快照**：
//     · `HasCurrentTrait(0x38e = tide)`      ⇒ `+0x5C = CurrentTide`   （= `GetCurrentTraitValueWithModifiers(0x38e)`）
//     · `HasCurrentTrait(0x49c = companion)` ⇒ `+0x60 = CurrentCompanion`
//   之后由 `CardScript__ProcessTide.c:21-24`（`+0x5C == 0` ⇒ return）与
//   `CardScript__ProcessCompanion.c:29`（`+0x60 < 1` ⇒ return）当闸、并**逐份 −1**。
//
//   我们这边原来是：`RuleCore.SpawnTideCopies` 用 `unit.KwValue(Tide)`（打出那一刻现读）、
//   `RuleCore.PlayCompanions` 用 `card.KwValue(Companion)`（**卡面印刷值**）—— 两处都不是快照。
//   本件钉的 = `UnitState.TideCounter / CompanionCounter`（构造时快照）＋ 两个消费口改读它。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B102_R1_战斗B类三条.md` §② 的 R3/R4。
//
// ============================ 怎么用（宿主）============================
// `B102CARD707_Assert.Run();`
//   + 宿主两行（照 `B97W22Assert` 的先例）：
//     `_pass += B102CARD707_Assert.Pass; _fail += B102CARD707_Assert.Fail;`
//   · 推荐宿主 = `RuleEngine/Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本件**只碰纯引擎**
//     （`UnitState` / `RuleCore`），不建 GameObject、不需要场景。
//
// ============================ 期望值从哪来 ============================
//   · 快照值 = **原版 `CardSetup` 那两句的语义**（`HasCurrentTrait` + 当前值）—— 夹具卡的词条值就是判据
//     （`Tide 2` ⇒ 2 · `Companion 2: X` ⇒ 2），⛔ 不是拿我们自己的常量反推。
//   · 产品路径的两条（C1/C2）看的是**手牌里真多出来的那几张实例**，⛔ 不是读代码里的字段。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：删掉构造函数末尾那句 `SetupTideAndCompanionCounters();` ⇒ A1/A3 红（两格恒 0）。
//   · A2：把 `SetupTideAndCompanionCounters` 里的 `if (Has(...))` 去掉、无条件写 ⇒ A2/A4 可能仍绿，
//        但 B1/B2 会红：**不带该词条**的卡被写了非零值。⇒ A2/A4 + B1/B2 合起来钉「只在带词条时写」。
//   · B1/B2：把 `TideCounter` 改成 `public int TideCounter { get { return KwValue(KeywordTable.Tide); } }`
//        （活值 getter 而不是快照字段）⇒ B1/B2 红 —— 这正是「快照 vs 现读」的分歧点。
//   · C1：把 `RuleCore.cs` 里 `SpawnTideCopies(..., unit.TideCounter, ...)` 改回
//        `unit.KwValue(KeywordTable.Tide)` ⇒ 本批**不会红**（两值一般相同，见报告「残差仍开着的那一半」）
//        ⇒ C1 只钉「产品路径没被打坏」，**不声称**能区分快照与现读。
//   · C2：把 `PlayCompanions` 里那个 `x` 改回 `card.KwValue(KeywordTable.Companion)`
//        ⇒ 本批**同样不会红**（印刷值 == 快照值），理由同 C1。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B102_707`：账 `战斗逻辑驱动:707` R3/R4 —— `UnitState.TideCounter / CompanionCounter`
/// 的「建卡快照」语义 ＋ 两个消费口改读快照。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B102CARD707_Assert
{
    static readonly CheckSink _sink = new CheckSink("[CARD707] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        SnapshotAtConstruction();
        SnapshotIsNotLive();
        ProductPathTide();
        ProductPathCompanion();
    }

    // ------------------------------------------------------------ ① 三个构造点
    static void SnapshotAtConstruction()
    {
        // A1：带 `Tide 2` ⇒ `TideCounter == 2`（原版 `+0x5C = CurrentTide`）
        var tide2 = Unit("C707Tide2", 1, 1, 3, "Tide 2");
        var u1 = new UnitState(tide2, false);
        C(u1.TideCounter, 2, "A1 ★ `Tide 2` 卡 ⇒ `UnitState.TideCounter == 2`（原版 `CardSetup.c:251-255` 的快照）");
        C(u1.KwValue(KeywordTable.Tide), 2, "A1' 同一刻 `KwValue(Tide)` 也是 2（快照点 = 构造点）");

        // A2：不带该词条 ⇒ 0（原版 `HasCurrentTrait` 为假时那一句**整支不执行**，字段留在默认 0）
        var plain = Unit("C707Plain", 1, 1, 3);
        var u2 = new UnitState(plain, false);
        C(u2.TideCounter, 0, "A2 ★ 不带 `Tide` ⇒ `TideCounter == 0`（没词条就不写）");
        C(u2.CompanionCounter, 0, "A4 ★ 不带 `Companion` ⇒ `CompanionCounter == 0`");

        // A3：带 `Companion 2: X` ⇒ `CompanionCounter == 2`（原版 `+0x60 = CurrentCompanion`）
        var host = new CardDef("C707HostSnap", "C707HostSnap", "unit", "Companion 2: C707Drone",
                               "common", "Test", 3, 2, 5, 0,
                               new[] { "Companion 2: C707Drone" }, subtype: "Battlesuit");
        var u3 = new UnitState(host, false);
        C(u3.CompanionCounter, 2, "A3 ★ `Companion 2: X` 卡 ⇒ `UnitState.CompanionCounter == 2`（`CardSetup.c:256-260`）");
    }

    // ------------------------------------------------------------ ② 快照 ≠ 活值（本件的核心灭自证）
    static void SnapshotIsNotLive()
    {
        // B1：构造之后再把 `tide` 值抬高 —— `KwValue` 跟着变，`TideCounter` **不许跟着变**。
        //     原版 `+0x5C` 是 `CardSetup` 那一刻的**副本**，之后加/减 trait 不改它
        //     （`CardScript__AddEffect.c` 的 case 3 只动 `_keywords`/面板，⛔ 不碰 `+0x5C`）。
        var tide2 = Unit("C707Tide2b", 1, 1, 3, "Tide 2");
        var u = new UnitState(tide2, false);
        C(u.TideCounter, 2, "B1 前置：快照 = 2");
        u.AddKeyword(KeywordTable.Tide, 3);
        C(u.KwValue(KeywordTable.Tide), 5, "B1 前置：加 `+3` 之后**当前值** = 5（关键词那本账确实动了）");
        C(u.TideCounter, 2, "B1 ★ `TideCounter` **仍是 2** —— 它是快照字段、不是活值 getter"
                          + "（改成 `get { return KwValue(...); }` ⇒ 本条必红）");

        // B2：companion 同理
        var host = new CardDef("C707HostSnap2", "C707HostSnap2", "unit", "Companion 2: C707Drone",
                               "common", "Test", 3, 2, 5, 0,
                               new[] { "Companion 2: C707Drone" }, subtype: "Battlesuit");
        var v = new UnitState(host, false);
        C(v.CompanionCounter, 2, "B2 前置：快照 = 2");
        v.AddKeyword(KeywordTable.Companion, 1);
        C(v.KwValue(KeywordTable.Companion), 3, "B2 前置：当前值 = 3");
        C(v.CompanionCounter, 2, "B2 ★ `CompanionCounter` **仍是 2**（快照，不是活值）");
    }

    // ------------------------------------------------------------ ③ 产品路径：潮涌
    static void ProductPathTide()
    {
        var tide = Unit("C707TidePlay", 1, 1, 3, "Tide 2");
        var ctx = RuleCore.NewBattle(Deck("C707W0", "Test", tide),
                                     Deck("C707W1", "Test", Unit("C707Foe", 1, 1, 5)),
                                     seed: 0, shuffle: false);
        RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 20;
        int hi = HandIdx(ctx, 0, "C707TidePlay");
        Tr(hi >= 0, "C1 夹具：手牌里有 `Tide 2` 那张");
        if (hi < 0) return;
        var rc = RuleCore.PlayCard(ctx, 0, hi, 1);
        C(rc, RuleCodes.OK, "C1 打出 `Tide 2` 单位");
        int copies = 0;
        foreach (var h in ctx.Players[0].Hand)
            if (h != null && h.Card != null && h.Card.Name == "C707TidePlay") copies++;
        C(copies, 1, "C1 ★ 打出 `Tide 2` ⇒ 手牌里只多 **1 张**复制"
                   + "（原版 `CardScript__ProcessTide.c:68` 唯一一处 `AddNewCardToHand`、无循环 ⇒ 打一次给一张；新卡自带 `2 − 1 = 1`，`:98`；⛔ 不是一次塞 2 张）");
    }

    // ------------------------------------------------------------ ④ 产品路径：伴生
    static void ProductPathCompanion()
    {
        var drone = Unit("C707Drone", 1, 1, 3);
        var host = new CardDef("C707HostPlay", "C707HostPlay", "unit", "Companion 2: C707Drone",
                               "common", "Test", 3, 2, 5, 0,
                               new[] { "Companion 2: C707Drone" }, subtype: "Battlesuit");
        // 合成夹具不是卡池真卡 ⇒ 自己补那条「原版 `relatedCard1`」的边（同 `TestCompanion` 的纪律）
        RelatedCardData.SetForTest("C707HostPlay", new[] { "C707Drone" });
        var pool = new List<CardDef> { host, drone };
        var foe = new CardDef[10];
        for (int i = 0; i < foe.Length; i++) foe[i] = Unit("C707FE" + i, 1, 0, 9);
        var ctx = RuleCore.NewBattle(Deck("C707W2", "TauEmpire", host), Deck("C707W3", "Test", foe),
                                     seed: 0, shuffle: false, cardPool: pool);
        RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 20;
        int hi = HandIdx(ctx, 0, "C707HostPlay");
        Tr(hi >= 0, "C2 夹具：手牌里有带 `Companion 2: …` 的宿主");
        if (hi < 0) return;
        var rc = RuleCore.PlayCard(ctx, 0, hi, 0);
        C(rc, RuleCodes.OK, "C2 打出带伴生的单位");
        CardInstance made = null;
        foreach (var h in ctx.Players[0].Hand)
            if (h != null && h.Card != null && h.Card.Name == "C707Drone" && h.IsCompanion) { made = h; break; }
        Tr(made != null, "C2 ★ 造出来的那张**在手里**、带 `isCompanion`（`ProcessCompanion.c:88`）");
        if (made != null)
            C(made.CompanionCounter, 1,
              "C2 ★ 新卡计数 = **快照 2 − 1 = 1**（`PlayCompanions` 的 `x` 现在读 `unit.CompanionCounter`；"
              + "原版 `ProcessCompanion.c:89` `newCard + 0x60 = 源 + 0x60 − 1`）");
    }

    // ============================================================ 夹具小工具（只在本件内用）
    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, kws);
    }

    static List<CardDef> Deck(string warlordName, string faction, params CardDef[] hand)
    {
        var d = new List<CardDef>
        {
            new CardDef(warlordName, warlordName, "hero", "", null, faction, 0, 2, 30, 0, null,
                        subtype: "Warlord"),
        };
        for (int i = 0; i < 20; i++) d.Add(Unit(warlordName + "_f" + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i] != null && hand[i].Card != null && hand[i].Card.Name == name) return i;
        return -1;
    }
}
