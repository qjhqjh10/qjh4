// B120_W30_Assert.cs — 执行代理 `B120-W30`（2026-11-03）· 账 `战斗逻辑驱动:248`
//   原版 `BattleManager.CheckForHeroDeckTransform(DeckAndWarlordData, bool)`（E03「变身督军换整副牌」）
//
// ============================ 它盯的是什么 ============================
//   ① **语义订正**：这个机制**不是**「换掉整副牌」，而是「**这副牌的督军变身成另一位督军**」——
//      判据满足后只换 `DeckAndWarlordData.deckWarlord(+0x18)` 这一格，牌库（`shuffledCardLibrary +0x28`）一张不动。
//      ⇒ B 组用「牌库张数/首张 id 前后不变」把旧读法（`Clear()` + 逐张重灌）**直接咬红**。
//   ② **两个取值口不再恒 null**：原版那两格就是**这个座位自己的**督军与牌库
//      （调用点 `SetupBoardPhase.c:56/66` 传的是 `matchData.playerRawBattleData/enemyRawBattleData`），
//      不是关卡数据 ⇒ `CampaignHeroTransformCard/Deck(ctx, seat)` 必须取得到真值（A 组）。
//   ③ **写口四条跳序**（`.c`）：空督军 ⇒ 不换 · criteria 不满足 ⇒ 不换 · 目标 id 不在英雄总表 ⇒ 不换 ·
//      全满足 ⇒ 记录原督军 + 换督军（B/C/D 组）。
//   ④ **今天休眠的原因**：督军卡上的两列（`transformedWarlordId +0x200` / `deckCriteria +0x1f8`）本机没有
//      （现扫 `cards_raw.json` 1,351 张 SO：前者非空 **0**、后者全默认档）⇒ E 组钉住「生产入口恒 false」，
//      F 组把这条事实钉成**金丝雀**（数据落地那天会红，届时同批改断言与两个取值口）。
//
// ============================ 判据（逐条实读）============================
//  · `d:/2/tools/decomp_full/BattleManager__CheckForHeroDeckTransform.c`（101 行，全文）
//  · `d:/2/tools/decomp_full/BoardAnalysis__DeckMeetsCriteria.c`（101 行，全文）
//  · `d:/2/tools/decomp_full/BattleManager.__c__DisplayClass282_0___CheckForHeroDeckTransform_b__0.c`
//    （谓词 = `hero.uniqueId(+0x38) == deckWarlord.transformedWarlordId(+0x200)`）
//  · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:1097-1145`（同一方法的可读版）
//  · 字段表 `d:/2/tools/il2cpp_out/dump.cs:23244`（RawCardScript）· `:77945`（DeckAndWarlordData）· `:78170`（MatchData）
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 B2 牌库张数/首张不变 ⇒ 退回「换牌库」的老实现必红（而 B1「督军被换」仍绿 ⇒ 两边不能一起绿）。
//  🧨 C1 criteria=false 必须**不换** ⇒ 把 criteria 参数当常量真处理必红（B1/C1 是同一观测量上的反相断言）。
//  🧨 A1/A2 取值口非空 ⇒ 退回「恒 null」必红（且生产入口 E 仍全绿 ⇒ 两条不能一起绿）。
//  🧨 D1/D2 两种「找不到目标」都必须保留原督军 ⇒ 把「找不到」写成「照换」必红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景、不读存档、不写 PlayerPrefs），纯引擎夹具。
//  挂法（由调度台决定）：宿主聚合行**同一物理行**追加
//      B120_W30_Assert.Run(); _sink.Pass += B120_W30_Assert.Pass; _sink.Fail += B120_W30_Assert.Fail;
//  判绿红：`grep "^B120W30"` 的**合计行**；逐条按行首 `B120W30 ✓` / `B120W30 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B120_W30`：账 248 `CheckForHeroDeckTransform` 的语义订正（换督军，⛔ 不是换牌库）。</summary>
public static class B120_W30_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B120W30 ✓ " + msg); }
        else { _fail++; Debug.LogError("B120W30 ✗ " + msg); }
    }

    // ---- 夹具（纯 CardDef，不碰 Unity 资产）------------------------------------
    static CardDef Hero(string id) { return new CardDef(id, "W30 " + id, "hero", "", "Common", "Test", 0, 1, 30, 0); }
    static CardDef Unit(string id) { return new CardDef(id, "W30 " + id, "unit", "", "Common", "Test", 1, 1, 1, 0); }

    /// <summary>造一局：座位 0 = 指定督军 + 4 张兵，座位 1 = 另一个督军 + 5 张兵；`cardPool` = 英雄总表等价物。</summary>
    static BattleContext MakeCtx(CardDef warlord0, CardDef warlord1, params CardDef[] pool)
    {
        var d0 = new List<CardDef> { warlord0, Unit("W30A1"), Unit("W30A2"), Unit("W30A3"), Unit("W30A4") };
        var d1 = new List<CardDef> { warlord1, Unit("W30B1"), Unit("W30B2"), Unit("W30B3"), Unit("W30B4"), Unit("W30B5") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false,
                                  cardPool: pool == null ? null : new List<CardDef>(pool));
    }

    public static void Run()
    {
        _pass = _fail = 0;
        try
        {
            GroupA_StructuralGetters();
            GroupB_TransformSwapsWarlordOnly();
            GroupC_CriteriaFalseNoTransform();
            GroupD_NoTargetNoTransform();
            GroupE_ProductionEntryIsDormant();
            GroupF_DataColumnsAbsentCanary();
        }
        finally
        {
            Debug.Log($"B120W30 === 合计：{_pass} 通过 / {_fail} 失败 ===");
        }
    }

    // ==================================================================
    //  A · 两个取值口：原版 `deckWarlord(+0x18)` / `shuffledCardLibrary(+0x28)` = 这个座位自己的
    // ==================================================================
    static void GroupA_StructuralGetters()
    {
        var w0 = Hero("W30Old"); var w1 = Hero("W30Old2");
        var ctx = MakeCtx(w0, w1, w0, w1, Hero("W30New"));
        var gotW = RuleCore.CampaignHeroTransformCard(ctx, 0);
        var gotD = RuleCore.CampaignHeroTransformDeck(ctx, 0);
        Check(gotW != null && gotW.Id == "W30Old",
              "A1 `CampaignHeroTransformCard(ctx,0)` = 座位 0 开局那位督军（实得 "
            + (gotW != null ? gotW.Id : "null") + "）｜🧨 退回「恒 null」⇒ 红");
        Check(gotD != null && gotD.Count > 0,
              $"A2 `CampaignHeroTransformDeck(ctx,0)` = 座位 0 的牌库（实得 {(gotD != null ? gotD.Count : -1)} 张，非空）"
            + "｜🧨 退回「恒 null」⇒ 红（原版这一格是 `shuffledCardLibrary`，⛔ 不是关卡数据）");
        bool allUnits = gotD != null;
        if (gotD != null) for (int i = 0; i < gotD.Count; i++) if (gotD[i] == null || gotD[i].Type != "unit") allUnits = false;
        Check(allUnits, "A3 牌表里只有兵（督军/防御卡已被 `BuildPlayer` 分流出去 —— 与原版 `shuffledCardLibrary` 同口径）");
        // 座位 1 也要能取到（原版两个调用点逐座位传）
        Check(RuleCore.CampaignHeroTransformCard(ctx, 1) != null
           && RuleCore.CampaignHeroTransformCard(ctx, 1).Id == "W30Old2",
              "A4 座位 1 的督军也取得到（原版 `:66` 传的是 `enemyRawBattleData`）");
        // 越界座位 ⇒ null（不抛）
        Check(RuleCore.CampaignHeroTransformCard(ctx, 5) == null
           && RuleCore.CampaignHeroTransformDeck(ctx, -1) == null,
              "A5 越界座位 ⇒ null（⛔ 不抛异常）");
    }

    // ==================================================================
    //  B · 判据满足 ⇒ **换督军、牌库一张不动**（旧读法「换牌库」在这里咬红）
    // ==================================================================
    static void GroupB_TransformSwapsWarlordOnly()
    {
        var w0 = Hero("W30Old"); var w0b = Hero("W30Old2"); var target = Hero("W30New");
        var ctx = MakeCtx(w0, w0b, w0, w0b, target);
        var ps = ctx.Players[0];
        int deckBefore = ps.Deck.Count;
        string firstBefore = ps.Deck.Count > 0 ? ps.Deck[0].Card.Id : null;
        int playOrderBefore = ps.Warlord.PlayOrder;

        var deckWarlord = RuleCore.CampaignHeroTransformCard(ctx, 0);
        var deck = RuleCore.CampaignHeroTransformDeck(ctx, 0);
        CardDef original;
        bool ok = RuleCore.CheckForHeroDeckTransform(ctx, 0, deckWarlord, deck,
                                                     "W30New", true, out original);

        Check(ok, "B1 判据满足 + 英雄总表里有该 id ⇒ 写口返回 true");
        Check(ps.Warlord != null && ps.Warlord.Card != null && ps.Warlord.Card.Id == "W30New",
              "B2 🧨 **督军已换成目标那位**（实得 " + (ps.Warlord != null && ps.Warlord.Card != null ? ps.Warlord.Card.Id : "null") + "）");
        Check(original != null && original.Id == "W30Old",
              "B3 `out originalWarlord` = 换之前的督军（原版写 `matchData.originalPlayerWarlord(+0xd8)`，"
            + "我们 BattleContext 没有那一格 ⇒ 由写口交回；实得 " + (original != null ? original.Id : "null") + "）");
        Check(ps.Deck.Count == deckBefore && (ps.Deck.Count == 0 || ps.Deck[0].Card.Id == firstBefore),
              $"B4 🧨 **牌库一张不动**（前 {deckBefore} 张 / 后 {ps.Deck.Count} 张；首张 {firstBefore}）"
            + "｜原版只换 `deckWarlord` 这一格 —— 退回「Clear + 重灌」的老实现 ⇒ 本条红");
        Check(ReferenceEquals(ps.Board[BoardSpec.WarlordSlot], ps.Warlord),
              "B5 督军格与 `ps.Warlord` 仍是同一个对象（换人后引用一起换）");
        Check(ps.Warlord.Exhausted == false && ps.Warlord.PlayOrder == playOrderBefore,
              $"B6 换人后 `Exhausted=false`（与 `BuildPlayer` 同句）且场上次序不变（{playOrderBefore}）");
        Check(ps.Warlord.Card.Health == 30, "B7 新督军生命 = 该卡卡面值（30，建人期修正按旧值−旧卡面原样搬）");
    }

    // ==================================================================
    //  C · 判据不满足 ⇒ 不换（灭自证：把 criteria 当常量真处理 ⇒ 红）
    // ==================================================================
    static void GroupC_CriteriaFalseNoTransform()
    {
        var w0 = Hero("W30Old"); var w0b = Hero("W30Old2"); var target = Hero("W30New");
        var ctx = MakeCtx(w0, w0b, w0, w0b, target);
        var ps = ctx.Players[0];
        int deckBefore = ps.Deck.Count;

        CardDef original;
        bool ok = RuleCore.CheckForHeroDeckTransform(ctx, 0,
                                                     RuleCore.CampaignHeroTransformCard(ctx, 0),
                                                     RuleCore.CampaignHeroTransformDeck(ctx, 0),
                                                     "W30New", false, out original);
        Check(!ok, "C1 🧨 criteria = false ⇒ 写口返回 false（原版 `DeckMeetsCriteria` 为假就 return）");
        Check(ps.Warlord.Card.Id == "W30Old" && original == null,
              "C2 督军没换、`originalWarlord` 仍是 null（与 B1/B2 是同一观测量上的反相断言）");
        Check(ps.Deck.Count == deckBefore, "C3 牌库也没动");
    }

    // ==================================================================
    //  D · 找不到目标 ⇒ 不换（原版 `fullHeroCollection.Find` 为 null 就 return）
    // ==================================================================
    static void GroupD_NoTargetNoTransform()
    {
        var w0 = Hero("W30Old"); var w0b = Hero("W30Old2"); var target = Hero("W30New");
        var ctx = MakeCtx(w0, w0b, w0, w0b, target);
        var ps = ctx.Players[0];

        CardDef original;
        bool ok1 = RuleCore.CheckForHeroDeckTransform(ctx, 0,
                                                      RuleCore.CampaignHeroTransformCard(ctx, 0),
                                                      RuleCore.CampaignHeroTransformDeck(ctx, 0),
                                                      null, true, out original);
        Check(!ok1 && ps.Warlord.Card.Id == "W30Old" && original == null,
              "D1 `transformedWarlordId` 为空 ⇒ 不换、原督军保留（这是**今天生产路径**那一支）");

        bool ok2 = RuleCore.CheckForHeroDeckTransform(ctx, 0,
                                                      RuleCore.CampaignHeroTransformCard(ctx, 0),
                                                      RuleCore.CampaignHeroTransformDeck(ctx, 0),
                                                      "W30NotInCollection", true, out original);
        Check(!ok2 && ps.Warlord.Card.Id == "W30Old" && original == null,
              "D2 目标 id 不在英雄总表 ⇒ 不换、原督军保留（🧨 把「找不到」写成「照换」⇒ 红）");
    }

    // ==================================================================
    //  E · 生产入口：今天两列数据没有 ⇒ 恒 false（休眠 ≠ 不可达）
    // ==================================================================
    static void GroupE_ProductionEntryIsDormant()
    {
        var w0 = Hero("W30Old"); var w0b = Hero("W30Old2"); var target = Hero("W30New");
        var ctx = MakeCtx(w0, w0b, w0, w0b, target);
        var ps = ctx.Players[0];
        int deckBefore = ps.Deck.Count;

        bool ok = RuleCore.CheckForHeroDeckTransform(ctx, 0,
                                                     RuleCore.CampaignHeroTransformCard(ctx, 0),
                                                     RuleCore.CampaignHeroTransformDeck(ctx, 0));
        Check(!ok, "E1 生产入口（旧 4 参形态）今天恒 false —— `transformedWarlordId/deckCriteria` 两列本机没有"
                 + "（⛔ 不是「不可达」：写口与判据都在位）");
        Check(ps.Warlord.Card.Id == "W30Old" && ps.Deck.Count == deckBefore,
              "E2 生产入口没换督军、也没动牌库");
        // `RuleCore.NewBattle` 内部那两个调用点不会让任何一方变形
        Check(ctx.Players[0].Warlord.Card.Id == "W30Old" && ctx.Players[1].Warlord.Card.Id == "W30Old2",
              "E3 `NewBattle` 开局链跑完之后双方督军原样（生产调用点就在 `ApplyWarlordLifeOverride` 之后）");
    }

    // ==================================================================
    //  F · 金丝雀：两列数据「查不到」的现状（数据落地那天本条会红 ⇒ 同批改断言 + 两个取值口）
    // ==================================================================
    static void GroupF_DataColumnsAbsentCanary()
    {
        var w0 = Hero("W30Old"); var w0b = Hero("W30Old2");
        var ctx = MakeCtx(w0, w0b, w0, w0b);
        Check(RuleCore.CampaignHeroTransformTargetId(ctx, 0) == null,
              "F1 `transformedWarlordId(+0x200)` 取值口 = **null（查不到）**"
            + "（现扫 `cards_raw.json` 1,351 张 SO：非空 0）｜数据落地后本条会红 —— 那是**预期的**金丝雀");
        Check(RuleCore.CampaignHeroTransformDeckMeets(ctx, 0) == null,
              "F2 `deckCriteria(+0x1f8)` 求值口 = **null（查不到）**，⛔ 不是「不满足」"
            + "（现扫 1,351 张：`countCriteria.targetsAffected` 全 0 = 原版「无条件即满足」早退档）");
    }
}
