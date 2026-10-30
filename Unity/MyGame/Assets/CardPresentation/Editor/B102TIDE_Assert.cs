// B102TIDE_Assert.cs — 执行代理 `B102_TIDE`（2026-10-30）· 账 `战斗逻辑驱动:707` 的【下半截】
//
// ============================ 它盯的是什么 ============================
//  上一步（`B102_707`）落了 `UnitState.TideCounter / CompanionCounter`（**打出那一刻**的快照）。
//  但原版那个 `−1` 是写在**新造出来的那张卡**上的：
//    · `CardScript__ProcessTide.c:21-28` —— 闸 = `!HasCurrentTrait(tide) || +0x5C == 0` ⇒ return；
//    · `CardScript__ProcessTide.c:68`     —— **一次只造一张**（唯一一处 `AddNewCardToHand`，无循环）；
//    · `CardScript__ProcessTide.c:98`     —— `newCard + 0x5C = 源 + 0x5C − 1`（写在**新卡**上）；
//  伴生同理：`CardScript__ProcessCompanion.c:26/:29/:88-89`。
//  ⇒ 计数必须**长在牌上**（`CardInstance.TideCounter / CompanionCounter`，造牌那一刻按
//    `CardScript__CardSetup.c:251-260` 盖快照），并且**逐份 −1 跟着牌走进手牌**。
//    `Tide X` 的「X 张」= **打一次给一张**的链，⛔ 不是「一次塞 X 张」。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B102_R1_战斗B类三条.md` §② 的 R3/R4。
//
// ============================ 怎么用（宿主）============================
// `B102TIDE_Assert.Run();`
//   + 宿主两行（照 `B102CARD707_Assert` 的先例）：
//     `Guard("B102TIDE_Assert", B102TIDE_Assert.Run); _pass += B102TIDE_Assert.Pass; _fail += B102TIDE_Assert.Fail;`
//   · 推荐宿主 = `RuleEngine/Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本件**只碰纯引擎**
//     （`CardInstance` / `RuleCore`），不建 GameObject、不需要场景。
//
// ============================ 期望值从哪来 ============================
//   · 快照值 = **原版 `CardSetup` 那两句的语义**（`HasCurrentTrait` + 当前值）—— 夹具卡的词条值就是判据
//     （`Tide 2` ⇒ 2），⛔ 不是拿我们自己的常量反推。
//   · 产品路径看的是**手牌里真多出来的那一份实例**（`ReferenceEquals`），⛔ 不是读代码里的字段。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A0：删掉 `CardInstance` 构造函数里那句 `StampTideAndCompanionCounters(card)` ⇒ A0 红（恒 0）。
//   · A1 张数：把 `SpawnTideCopies` 改回 `for (i < x)` 造 X 张 ⇒ 打出 `Tide 2` 实得 2 张 ⇒ 红。
//   · A1 计数：删掉 `copy.TideCounter = left - 1` ⇒ 新卡停在构造时的盖值 2 ⇒ 红。
//   · A2 ★★：把 `int left = origin.TideCounter;` 改回 `int left = card.KwValue(KeywordTable.Tide);`
//     （= 读**印刷值**、不是牌实例的快照）⇒ 链**永远停不下来**（每一环都再造一张 1）⇒ A2 级联红。
//     这条同时钉住「`UnitState.TideCounter`（打出那一刻的快照）不够用」——它每环都是 2。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B102_TIDE`：账 `战斗逻辑驱动:707` 下半截 —— `CardInstance.TideCounter /
/// CompanionCounter` 的「造牌快照 + 逐份 −1 跟着牌走」语义，以及 `SpawnTideCopies` 的**原版一次一张**形状。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B102TIDE_Assert
{
    static readonly CheckSink _sink = new CheckSink("[TIDE] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        SnapshotAtCreation();   // A0 前提（硬断言）
        TideChain();            // A1 ★ + A2 灭自证
    }

    // ------------------------------------------------------------ ① 造牌那一刻的盖值
    static void SnapshotAtCreation()
    {
        // A0：带 `Tide 2` ⇒ 造出来就有 2（原版 `CardSetup.c:251-255` 的 `+0x5C = CurrentTide`）
        var tide2 = Unit("B102Tide2", 1, 1, 3, "Tide 2");
        var inst = CardInstance.Detached(tide2);
        // 🔴 **硬前提先断**（⛔ 不许写成 `if (inst != null) Check(...)` —— 前提不成立时会静默跳过）
        Tr(inst != null && inst.Card != null && inst.Card.Name == "B102Tide2",
           "A0 前提：拿到的实例确实来自那张 `Tide 2` 定义");
        C(inst.TideCounter, 2,
          "A0 ★ 造牌那一刻 `Tide 2` ⇒ `CardInstance.TideCounter == 2`"
          + "（原版 `CardSetup.c:251-255` 的 `+0x5C = CurrentTide`）"
          + "｜🧨 删掉构造函数里那句 `StampTideAndCompanionCounters(card)` ⇒ 实得 0 ⇒ 红");

        // A0'：不带该词条 ⇒ 0（原版那两个 `if` 整支不执行）
        var plain = CardInstance.Detached(Unit("B102Plain", 1, 1, 3));
        C(plain.TideCounter, 0,
          "A0' 不带 `Tide` ⇒ `TideCounter == 0`（原版 `HasCurrentTrait(0x38e)` 假 ⇒ 那一句不写）");

        // A0''：同伴那格同刻盖值（`CardSetup.c:256-260` 的 `+0x60 = CurrentCompanion`）
        var host = new CardDef("B102HostSnap", "B102HostSnap", "unit", "Companion 2: B102Drone",
                               "common", "Test", 3, 2, 5, 0,
                               new[] { "Companion 2: B102Drone" }, subtype: "Battlesuit");
        var hi = CardInstance.Detached(host);
        C(hi.CompanionCounter, 2,
          "A0'' ★ `Companion 2: X` ⇒ `CardInstance.CompanionCounter == 2`"
          + "（原版 `CardSetup.c:256-260`；这条与 `TideCounter` 走同一个盖值函数）");
    }

    // ------------------------------------------------------------ ② 产品路径：链是一环一环来的
    static void TideChain()
    {
        var tide = Unit("B102TidePlay", 1, 1, 3, "Tide 2");
        var ctx = RuleCore.NewBattle(Deck("B102W0", "Test", tide),
                                     Deck("B102W1", "Test", Unit("B102Foe", 1, 1, 5)),
                                     seed: 0, shuffle: false);
        RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 20;

        int hi = HandIdx(ctx, 0, "B102TidePlay");
        Tr(hi >= 0, "A1 夹具（硬前提）：手牌里有那张 `Tide 2`");
        if (hi < 0) return;                       // 前提已断过；这里只是防止后面读到错的索引
        var played = ctx.Players[0].Hand[hi];     // 打出去之前，先记下**那一份实例**

        C(RuleCore.PlayCard(ctx, 0, hi, 1), RuleCodes.OK, "A1 打出 `Tide 2` 单位");

        // ---- 第一环：只多 1 张，且那张自己带着 1 ----
        var copies1 = HandNamed(ctx, 0, "B102TidePlay");
        C(copies1.Count, 1,
          "A1 ★★ 打出 `Tide 2` ⇒ 手牌里只多 **1 张**复制（原版 `ProcessTide.c:68` 一次只造一张、无循环）"
          + "｜🧨 改回 `for (i < x)` 造 X 张 ⇒ 实得 2 ⇒ 红");
        if (copies1.Count == 0) return;           // 前置已红；后续无意义

        var copy1 = copies1[0];
        C(copy1.TideCounter, 1,
          "A1 ★ 新卡**自己**带着 `源 2 − 1 = 1`（原版 `ProcessTide.c:98` `newCard + 0x5C = 源 + 0x5C − 1`）"
          + "｜🧨 删掉 `copy.TideCounter = left - 1` ⇒ 停在构造时的盖值 2 ⇒ 红");

        // 「计数跟着牌走」：打出去的是**同一份实例**（原版 `+0x5C` 长在 `CardScript` 上、随牌移动）
        var onBoard = BoardInstance(ctx, 0, played);
        Tr(onBoard != null,
           "A1'' ★ 上场的那一份就是手里那一份（`ReferenceEquals`）—— 计数跟着【牌】走，"
           + "不是「到场上重新算」｜🧨 把 `PlayCard` 改成重建实例 ⇒ 这条红（先于 A1' 暴露）");

        // ---- 第二环：打出那张（实例计数 1）⇒ 再带 1 张、计数 0 ----
        int ci = RefIdx(ctx, 0, copy1);
        Tr(ci >= 0, "A1' 夹具：那一份复制按**实例身份**在手牌里");
        if (ci < 0) return;
        C(RuleCore.PlayCard(ctx, 0, ci, 1), RuleCodes.OK, "A1' 打出那一**份**复制");

        var copies2 = HandNamed(ctx, 0, "B102TidePlay");
        C(copies2.Count, 1, "A1' ★ 它（实例计数 1）⇒ **再带 1 张**");
        if (copies2.Count == 0) return;
        var copy2 = copies2[0];
        C(copy2.TideCounter, 0, "A1' ★ 第二环的新卡计数 = **1 − 1 = 0**");

        // ---- 第三环：计数 0 ⇒ 不再造（终止；本条也是上面那条 🧨 的落地）----
        int ci2 = RefIdx(ctx, 0, copy2);
        Tr(ci2 >= 0, "A2 夹具：计数 0 的那一份在手牌里");
        if (ci2 < 0) return;
        C(RuleCore.PlayCard(ctx, 0, ci2, 1), RuleCodes.OK, "A2 打出**计数 0** 的那一份");
        C(HandNamed(ctx, 0, "B102TidePlay").Count, 0,
          "A2 ★★【灭自证】计数 0 ⇒ **不再造**（原版 `ProcessTide.c:26` `+0x5C == 0` ⇒ return）"
          + "｜🧨 把 `left` 改回 `card.KwValue(Tide)`（印刷值 2）或 `unit.TideCounter`（每环都是 2）"
          + " ⇒ 这一环会再造一张、链**永远停不下来** ⇒ 本条红");
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

    static List<CardInstance> HandNamed(BattleContext ctx, int p, string name)
    {
        var r = new List<CardInstance>();
        foreach (var h in ctx.Players[p].Hand)
            if (h != null && h.Card != null && h.Card.Name == name) r.Add(h);
        return r;
    }

    static int RefIdx(BattleContext ctx, int p, CardInstance inst)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (ReferenceEquals(hand[i], inst)) return i;
        return -1;
    }

    static UnitState BoardInstance(BattleContext ctx, int p, CardInstance inst)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && ReferenceEquals(u.Instance, inst)) return u;
        }
        return null;
    }
}
