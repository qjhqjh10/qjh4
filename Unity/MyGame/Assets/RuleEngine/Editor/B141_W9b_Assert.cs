// B141_W9b_Assert.cs — 批次 **B141** · 执行写手 `W9b`（引擎 · SAN-04 残余②「605/600 先算后贴」）
//
// ============================ 它盯的是什么 ============================
//   原版 `RawCardScript__TriggerAbilities.c:59` 逐条遍历 `cardAbilities`、`:207` 每条过闸、`:231` 触发；
//   `AbilityLogic__PlayAbility.c:262` 那一条 ability **自己的** `targetCriteria` → `GetTargets`，
//   `:273` `PlayAnim` 按这批目标逐张播 ⇒ **每条 ability 各算各的目标**。
//
//   A 组（灭自证核心）：**多组 605 逐组各算自己的目标** —— 用两条手搭 op（目标分别是敌方 / 己方）喂
//        `ResolveFaithGroupsUnderSinkForTest`：必须拿回 **2 条 scratch**，且各自 `TargetPlayer` 按自己的 criteria。
//        🧨 退回「整批一次 `ResolveOps`」⇒ 只拿回 1 条 / 第二条目标错 ⇒ 红。
//   B 组：`SplitPaidGroups` 的组边界与 `ChargePaidGroups` 同源（`Cost>0 && !CostShared` 起组）。
//   C 组（真实卡）：`Canoness`（SOR23 · 单位 · 两条 605 · 门槛 2/4）在 Faith=4 时发 **2 条** `UseFaith`，
//        `AbilityIndex` 依次 0/1；两条都是 `Gain …`（自己）⇒ 目标被收口按判据②跳过，`TargetPlayer` 保持 `-1`。
//   D 组：单组卡（`Crusader`）仍然只发 1 条（改前行为）。
//
// ============================ 灭自证（怎么才能红）============================
//   · A1：把 `ResolveFaithGroupsUnderSink` 改回「一次 `ResolveOps(全部 ops)`」⇒ `scratches.Count == 1` ⇒ 红；
//   · A2/A3：把两组用同一个 scratch / 忽略每组自己的 criteria ⇒ 两条 `Target*` 相同 ⇒ 红；
//   · C1：只发一条（或只结算第一组）⇒ `UseFaith` 计数 != 2 ⇒ 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B141W9b_Assert", B141_W9b_Assert.Run); _pass += B141_W9b_Assert.Pass; _fail += B141_W9b_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做。）
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W9b 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W9b_Assert
{
    static int _pass, _fail;

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W9b：SAN-04 残余②「605/600 先算后贴」");
        GroupA_PerGroupTargets();
        GroupB_SplitPaidGroups();
        GroupC_RealMultiGroupUnit();
        GroupD_SingleGroupUnchanged();
        Summary();
    }

    // ==================================================================
    //  A 组：多组 605 逐组各算自己的目标（核心）
    // ==================================================================
    static void GroupA_PerGroupTargets()
    {
        Section("A 多组 605 逐组各算自己的目标（判据 `TriggerAbilities.c:59/207/231`）");
        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "A0 前提不成立：测试对局没建起来"); return; }

        // 友军（座位 0）· 敌军（座位 1）各一个；**载体不摆上棋盘** ⇒ 「own troop」只可能选中友军
        var ally = Place(ctx, 0, 3, PlainCard("B141W9bAlly", "unit"));
        var enemy = Place(ctx, 1, 3, PlainCard("B141W9bEnemy", "unit"));
        var carrier = new UnitState(ctx.NewInstance(PlainCard("B141W9bCarrier", "unit")), false);
        ctx.Players[0].Faith = 10;   // 两组门槛 2 / 4 都过
        CheckTrue(ally != null && enemy != null, "A0b 前提：友军与敌军都摆上棋盘");
        if (ally == null || enemy == null) return;

        // 两条「付费组首」op：group1 打敌人 · group2 打自己人（**目标不同**才验得出「各算各的」）
        var g1 = new EffectOp
        {
            Verb = "deal", Amount = 2, AmountMax = 2, Cost = 2, Source = "A-g1",
            Target = new EffectTargetSpec { Side = "enemy", Kind = "troop", Count = 1 },
        };
        var g2 = new EffectOp
        {
            Verb = "deal", Amount = 2, AmountMax = 2, Cost = 4, Source = "A-g2",
            Target = new EffectTargetSpec { Side = "own", Kind = "troop", Count = 1 },
        };
        var ops = new List<EffectOp> { g1, g2 };

        List<EffectOp> activated;
        var scratches = RuleCore.ResolveFaithGroupsUnderSinkForTest(ctx, 0, carrier, ops, out activated);

        Check(activated.Count, 2, "A1 前提：两组都过门槛（`ChargePaidGroups` 各收各的）= 2");
        CheckTrue(scratches.Count == 2,
                  "A2 ★★★ **结论**：**逐组各一条 scratch**（2 条）"
                + "｜🧨 退回「整批一次 `ResolveOps`」⇒ 只有 1 条 ⇒ 红");
        if (scratches.Count != 2) return;

        Check(scratches[0].TargetPlayer, 1, "A3 ★★★ **结论**：第 1 组的目标 = **敌方**（自己的 criteria）");
        Check(scratches[0].TargetCardId, "B141W9bEnemy", "A3b 第 1 组落到的就是那个敌人");
        Check(scratches[1].TargetPlayer, 0, "A4 ★★★ **结论**：第 2 组的目标 = **己方**（自己的 criteria）");
        Check(scratches[1].TargetCardId, "B141W9bAlly", "A4b 第 2 组落到的就是那个友军");
        CheckTrue(scratches[0].TargetCardId != scratches[1].TargetCardId,
                  "A5 ★★★ **灭自证**：两组目标**不是同一个**（不是「拿第一组的目标套给所有组」）");
        Check(enemy.Health, 18, "A6 第 1 组伤害真的落在敌人身上（20 − 2）");
        Check(ally.Health, 18, "A7 第 2 组伤害真的落在友军身上（20 − 2）");
    }

    // ==================================================================
    //  B 组：组边界与 `ChargePaidGroups` 同源
    // ==================================================================
    static void GroupB_SplitPaidGroups()
    {
        Section("B `SplitPaidGroups`：`Cost>0 && !CostShared` 起组、后随 `Cost>0 && CostShared` 归同组");
        var g1 = new EffectOp { Verb = "give", Cost = 2, CostShared = false, Source = "B1" };
        var g1b = new EffectOp { Verb = "give", Cost = 2, CostShared = true, Source = "B1b" };
        var g2 = new EffectOp { Verb = "give", Cost = 4, CostShared = false, Source = "B2" };
        var groups = RuleCore.SplitPaidGroupsForTest(new List<EffectOp> { g1, g1b, g2 });
        Check(groups.Count, 2, "B1 两个付费组首 ⇒ 2 组");
        CheckTrue(groups.Count == 2 && groups[0].Count == 2 && groups[0][1] == g1b,
                  "B2 第 1 组 = 组首 + 它的 `CostShared` 兄弟");
        CheckTrue(groups.Count == 2 && groups[1].Count == 1 && groups[1][0] == g2,
                  "B3 第 2 组 = 只有组首");
    }

    // ==================================================================
    //  C 组：真实多组单位卡（Canoness · SOR23）
    // ==================================================================
    static void GroupC_RealMultiGroupUnit()
    {
        Section("C 真实多组 605：Canoness（门槛 2/4）在 Faith=4 时发 **2 条** `UseFaith`");
        var pool = CardDatabase.Load();
        var canoness = PoolCard(pool, "Canoness");
        CheckTrue(canoness != null, "C0 前提：池里有 `Canoness`");
        if (canoness == null) return;
        Check(canoness.FaithAbilityGroupCount, 2, "C0b 前提：Canoness 信仰能力组数 = 2");

        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "C0c 前提不成立：测试对局没建起来"); return; }
        var u = Place(ctx, 0, 3, canoness);
        if (u == null) { Report(false, "C0d 前提不成立：Canoness 没摆上棋盘"); return; }
        ctx.Players[0].Faith = 4;

        int code = RuleCore.UseFaithAbility(ctx, 0, 3);
        Check(code, RuleCodes.OK, "C1 前提：`UseFaithAbility` 返回 OK");

        var evs = FaithSignals(ctx);
        Check(evs.Count, 2, "C2 ★★★ **结论**：Faith=4 ⇒ **两条** `UseFaith`（逐组各一条）"
                          + "｜🧨 只发一条 / 只结算第一组 ⇒ 红");
        CheckTrue(evs.Count == 2 && evs[0].AbilityIndex == 0 && evs[1].AbilityIndex == 1,
                  "C3 序号依次 = 0 / 1（原版两条 605 能力各自的 `cardAbilities[i]`）");
        CheckTrue(evs.Count == 2 && evs[0].TargetPlayer == -1 && evs[1].TargetPlayer == -1,
                  "C4 两条都是 `Gain …`（目标是**自己**）⇒ 收口按判据②跳过自指目标，`TargetPlayer` 保持 -1"
                + "（⛔ 不是「被错填成某个队友」）");
    }

    // ==================================================================
    //  D 组：单组卡行为不变
    // ==================================================================
    static void GroupD_SingleGroupUnchanged()
    {
        Section("D 单组卡（Crusader · 门槛 3）仍然只发 1 条、无后缀");
        var pool = CardDatabase.Load();
        var crusader = PoolCard(pool, "Crusader");
        CheckTrue(crusader != null, "D0 前提：池里有 `Crusader`");
        if (crusader == null) return;
        Check(crusader.FaithAbilityGroupCount, 1, "D0b 前提：Crusader 信仰能力组数 = 1");

        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "D0c 前提不成立：测试对局没建起来"); return; }
        var u = Place(ctx, 0, 3, crusader);
        if (u == null) { Report(false, "D0d 前提不成立：Crusader 没摆上棋盘"); return; }
        ctx.Players[0].Faith = 3;
        RuleCore.UseFaithAbility(ctx, 0, 3);

        var evs = FaithSignals(ctx);
        Check(evs.Count, 1, "D1 ★★ 单组卡只发 **1 条**（改前行为，一个字节都不许变）");
        CheckTrue(evs.Count == 1 && evs[0].Keyword == "UseFaith" && evs[0].AbilityIndex == -1,
                  "D2 单组卡：`Keyword` 不带后缀、`AbilityIndex = -1`");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    static List<BattleEvent> FaithSignals(BattleContext ctx)
    {
        var list = new List<BattleEvent>();
        if (ctx == null || ctx.Signals == null) return list;
        foreach (var e in ctx.Signals)
            if (e != null && e.Kind == EvtKind.Trigger && e.Keyword != null
                && (e.Keyword == "UseFaith" || e.Keyword.StartsWith("UseFaith#")))
                list.Add(e);
        return list;
    }

    static CardDef PoolCard(IList<CardDef> pool, string name)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].Name == name) return pool[i];
        return null;
    }

    static CardDef PlainCard(string id, string type)
    {
        var c = new CardDef(id, id, type, "", "common", "Test", 1, 1, 20, 0, null,
                            subtype: type == "unit" ? "Infantry" : null);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    static BattleContext MakeBattle()
    {
        var heroA = new CardDef("B141W9bHeroA", "B141W9bHeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B141W9bHeroB", "B141W9bHeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var deckA = new List<CardDef> { heroA };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainCard("B141W9bFillA" + i, "unit")); deckB.Add(PlainCard("B141W9bFillB" + i, "unit")); }
        return RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ==================================================================
    //  最小断言框架
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
        if (_fail == 0) Debug.Log($"B141_W9b 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W9b 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
