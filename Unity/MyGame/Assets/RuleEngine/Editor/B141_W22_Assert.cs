// B141_W22_Assert.cs — 批次 **B141** · 执行写手 `W22`（`RuleEngine/Core` 两件：SAN-27 残余③ / SAU55 `ta=235`）
//
// ============================ 它盯的是什么 ============================
//   A 组：SAN-27 残余③ —— 多行 activeAbility **每一行按自己的 `targetCriteria` 算目标**并进事件 `AbilityTargetList`
//                        （`RuleCore.AppendOneRowAbilityTargets*`）。改前事件表只挑一行。
//   B 组：SAU55 `ta=235 warlordsTalent` —— `TargetCriteriaEval` 的候选 = **己方（施放者）战将的天赋卡**
//                        （`FilterMethods__CheckIfMeetsCriteria.c:126-157`：`GetHero(施放者侧)` → `CardScript__GetTalentCard` → 按卡名比）。
//   C 组：SAU55 端到端 —— `handBuff ∧ ta=235` 的行落到手牌：**天赋卡 -1 费、非天赋卡不动**，且 `doNotStack` 幂等。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A3：只在计划里留 row0 ⇒ 敌方**不在**表里（证明 A1 的敌方来自 row1，不是「全体都加」）。
//   · A4：同一计划并两次 ⇒ 去重后条数不变（删去重 ⇒ 条数翻倍 ⇒ 红）。
//   · B3：把 `MatchesRawCard` 的 `ta=235` 分支写回 `return null` ⇒ B2/B3 红。
//   · B4：把比较放宽成按兵种/费用 ⇒ 非天赋卡也 true ⇒ B3 红。
//   · C2/C3：把 `ta` 过滤从 `235` 放开到「任意 handBuff」⇒ 非天赋卡也被 -1 ⇒ C3 红。
//   · C4：删 `AttachHandAbilityBuff` 的 `doNotStack` 闸 ⇒ 第二次调用又 -1 ⇒ C4 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 聚合行**同一物理行**追加（接线由调度台统一做）：
//     `Guard("B141W22_Assert", B141_W22_Assert.Run); _pass += B141_W22_Assert.Pass; _fail += B141_W22_Assert.Fail;`
//   （⚠️ 追加文本必须落在该行第一个 `//` **之前**。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W22 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W22_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W22：多行 ability 逐行目标 / `ta=235 warlordsTalent` 天赋卡降费");
        GroupA_MultiRowTargets();
        GroupB_WarlordTalent235();
        GroupC_HandBuffApply();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id, int cost)
        => new CardDef(id, id, "unit", "", null, "Test", cost, 1, 5, 0, (IEnumerable<string>)null, subtype: "Infantry");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B141W22Warlord0") };
        var d1 = new List<CardDef> { Hero("B141W22Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };   // `UnitState` 的 ctor 恒 `Exhausted = true`
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>合成一张「主动能力两行：logic0 `ta=10`（己方部队）/ logic1 `ta=140`（上一条 ability 的目标表）」的卡。</summary>
    static CardDef MultiRowCard(string id)
    {
        var rows = new List<CardDef.TargetCriteriaRowSpec>();
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 0,
            Criteria = new CardDef.CountSetSpec
            {
                TargetsAffected = 10, FriendlyUnits = 1, EnemyUnits = 0,
                IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0, IncludeSelf = 0,
                MaxTargets = 0, TargetSelectMethod = 0,
            },
        });
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 1,
            Criteria = new CardDef.CountSetSpec { TargetsAffected = 140, MaxTargets = 0, TargetSelectMethod = 0 },
        });
        return new CardDef(id, id, "unit", "", null, "Test", 3, 1, 5, 0,
                           (IEnumerable<string>)null, subtype: "Infantry",
                           activeAbilityTargetCriteria: rows,
                           activeAbilityTargetCriteriaVersioned: true);
    }

    // ==================================================================
    //  A 组：多行 ability 每一行各自的目标（SAN-27 残余③）
    // ==================================================================
    static void GroupA_MultiRowTargets()
    {
        Section("A 多行 activeAbility：每行按自己的 `targetCriteria` 算目标并进事件表");
        var ctx = NewCtx(); int p = ctx.Active;
        var carrier = Place(ctx, p, 5, MultiRowCard("B141W22_Carrier"));   // 载体（主动能力行所在单位）
        var f1 = Place(ctx, p, 3, Unit("B141W22_F1", 1));
        var f2 = Place(ctx, p, 7, Unit("B141W22_F2", 2));
        var enemy = Place(ctx, 1 - p, 5, Unit("B141W22_E1", 1));
        // row1 `ta=140` 读「上一条 ability 的目标表」⇒ 预先放敌方那一张（定向断言用）
        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(enemy));

        var plan = new List<int[]>
        {
            new[] { 30, 10, 0, CardDef.ActiveAbilityObject },    // logic0: ta=10 己方部队
            new[] { 30, 140, 1, CardDef.ActiveAbilityObject },   // logic1: ta=140 上一条目标（= 敌方）
        };
        var into = new List<BattleEventTarget>();
        RuleCore.AppendPlanRowAbilityTargetsForTest(ctx, p, carrier, null, plan, into);

        CheckTrue(HasTarget(into, enemy), "A1 ★ row1（`ta=140`）的目标（敌方）进了事件表 —— 改前只挑一行 ⇒ 这里缺（判据 `AbilityLogic__PlayAbility.c:262/273` 每行各一次 `GetTargets`）");
        CheckTrue(HasTarget(into, f1) && HasTarget(into, f2), "A2 ★ row0（`ta=10`）的己方部队也都在表里（每行各算各的）");

        // 灭自证①：只跑 row0 ⇒ 敌方不在（证明 A1 的敌方来自 row1，而不是「全体都加」）
        var only0 = new List<int[]> { plan[0] };
        var into2 = new List<BattleEventTarget>();
        RuleCore.AppendPlanRowAbilityTargetsForTest(ctx, p, carrier, null, only0, into2);
        CheckTrue(!HasTarget(into2, enemy), "A3 ★ 灭自证：只跑 row0 ⇒ 敌方**不在**表里");

        // 灭自证②：并两次 ⇒ 去重条数不变（删去重 ⇒ 条数翻倍 ⇒ 红）
        int n1 = into.Count;
        RuleCore.AppendPlanRowAbilityTargetsForTest(ctx, p, carrier, null, plan, into);
        Check(n1, into.Count, "A4 ★ 同一计划并两次 ⇒ 去重后条数不变（" + n1 + "）");
    }

    static bool HasTarget(List<BattleEventTarget> list, UnitState u)
    {
        if (list == null || u == null) return false;
        for (int i = 0; i < list.Count; i++)
            if (list[i].InstanceId != 0 && list[i].InstanceId == u.InstanceId) return true;
        return false;
    }

    // ==================================================================
    //  B 组：`ta=235 warlordsTalent` 的候选集（SAU55）
    // ==================================================================
    static void GroupB_WarlordTalent235()
    {
        Section("B `ta=235 warlordsTalent`：只命中天赋卡（按卡名）");
        var set = new CardDef.CountSetSpec { TargetsAffected = 235 };
        var talent = Unit("B141W22_TalentCard", 2);
        var other = Unit("B141W22_NotTalent", 2);
        string gap;

        TargetCriteriaEval.ClearWarlordTalentContext();
        CheckTrue(TargetCriteriaEval.MatchesRawCard(talent, set, out gap) == null,
                  "B1 没有天赋卡上下文 ⇒ `null`（判不了，⛔ 不是 true/false）");
        CheckTrue(!TargetCriteriaEval.HasWarlordTalentContext, "B1b 前提：上下文确实是空的");

        TargetCriteriaEval.SetWarlordTalentContext(talent);
        try
        {
            CheckTrue(TargetCriteriaEval.MatchesRawCard(talent, set, out gap) == true, "B2 ★ 候选 == 天赋卡 ⇒ true");
            CheckTrue(TargetCriteriaEval.MatchesRawCard(other, set, out gap) == false,
                      "B3 ★ 候选 != 天赋卡 ⇒ false（⛔ 不按兵种/费用放宽）");
            // 灭自证：把上下文换成别的卡 ⇒ 原天赋卡变 false
            TargetCriteriaEval.SetWarlordTalentContext(other);
            CheckTrue(TargetCriteriaEval.MatchesRawCard(talent, set, out gap) == false,
                      "B4 ★ 灭自证：上下文换一张 ⇒ 原天赋卡变 false");
        }
        finally { TargetCriteriaEval.ClearWarlordTalentContext(); }
        CheckTrue(!TargetCriteriaEval.HasWarlordTalentContext, "B5 清场：静态上下文已清（⛔ 别漏在全局）");

        // 棋盘路（`Matches` 的同一档）
        var ctx = NewCtx(); int p = ctx.Active;
        var bt = Place(ctx, p, 3, Unit("B141W22_BoardTalent", 1));
        var bo = Place(ctx, p, 5, Unit("B141W22_BoardOther", 1));
        TargetCriteriaEval.SetWarlordTalentContext(bt.Card);
        try
        {
            CheckTrue(TargetCriteriaEval.Matches(ctx, p, set, bt, null, null, null, out gap) == true,
                      "B6 ★ 棋盘路：候选 == 天赋卡 ⇒ true");
            CheckTrue(TargetCriteriaEval.Matches(ctx, p, set, bo, null, null, null, out gap) == false,
                      "B7 ★ 棋盘路：候选 != 天赋卡 ⇒ false");
        }
        finally { TargetCriteriaEval.ClearWarlordTalentContext(); }
    }

    // ==================================================================
    //  C 组：`ta=235` handBuff 端到端（SAU55「你的战将的才能花费减少 1」）
    // ==================================================================
    static void GroupC_HandBuffApply()
    {
        Section("C `ta=235` handBuff：天赋卡 -1 费、非天赋卡不动、幂等");
        var ctx = NewCtx(); int p = ctx.Active;
        var talentDef = Unit("B141W22_HandTalent", 5);
        var otherDef = Unit("B141W22_HandOther", 4);
        var talentInst = ctx.NewInstance(talentDef); ctx.Players[p].Hand.Add(talentInst);
        var otherInst = ctx.NewInstance(otherDef); ctx.Players[p].Hand.Add(otherInst);
        int baseT = RuleCore.CostOf(ctx, p, talentInst);
        int baseO = RuleCore.CostOf(ctx, p, otherInst);
        Check(baseT, 5, "C0 前提：天赋卡基础费 5");
        Check(baseO, 4, "C0b 前提：非天赋卡基础费 4");

        var rows = new List<CardDatabase.AbilityBuffSpec>
        {
            new CardDatabase.AbilityBuffSpec
            {
                Ab = 0, Lg = 0, IsHandBuff = true, BuffType = 2, CostChange = -1, PlayersAffected = 10,
                Criteria = new CardDef.CountSetSpec { TargetsAffected = 235 },
            },
        };
        int n = RuleCore.ApplyWarlordTalentHandBuffRowsForTest(ctx, p, "B141W22_SAU55", talentDef, rows);
        Check(n, 1, "C1 ★ 只命中 1 张（天赋卡；非天赋卡被 `ta=235` 挡掉）");
        Check(RuleCore.CostOf(ctx, p, talentInst), baseT - 1, "C2 ★ 天赋卡费用 -1（" + (baseT - 1) + "）");
        Check(RuleCore.CostOf(ctx, p, otherInst), baseO, "C3 ★ 非天赋卡费用不变（" + baseO + "）");

        // 幂等：同一条记录再落一次不叠（`doNotStack`）
        int n2 = RuleCore.ApplyWarlordTalentHandBuffRowsForTest(ctx, p, "B141W22_SAU55", talentDef, rows);
        Check(n2, 0, "C4 ★ 再调一次 ⇒ `doNotStack` 不叠（0 条）");
        Check(RuleCore.CostOf(ctx, p, talentInst), baseT - 1, "C5 ★ 费用仍是 -1（⛔ 不变 -2）");
        CheckTrue(!TargetCriteriaEval.HasWarlordTalentContext, "C6 清场：上下文已清");
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
        if (_fail == 0) Debug.Log($"B141_W22 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W22 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
