// RuleEngineTest_EngineF2.cs — 引擎线 `F2`（2026-10-28 · 第九十三会话 · 文件所有者 = `EffectResolver.cs`）
//
// 为什么单开一个文件：简报里指定的断言宿主 `RuleEngineTest_EngineE5.cs` **此刻仍在 F4 名下**
// （见 `资料/完全复刻/文件所有权_当前.md`）⇒ 本代理**不碰它**（一个文件同一时刻只能有一个写手）。
// 本件的测试体放这里；接线只差 `RuleEngineTest.cs` 的 `Run()` 里**一行**：
//     Step(TestF2EffectResolverGaps);
// ⚠️ `RuleEngineTest.cs` 归 F1 ⇒ 本代理**没有加那一行**，由调度台转发接线（报告 §接线 里写全了）。
//
// 🔴 每条断言都带 `🧨`（写清什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（接线 = `Run()` 里一行 `Step(TestF2EffectResolverGaps)`）。</summary>
    public static void TestF2EffectResolverGaps()
    {
        Section("🆕 引擎线 F2：`turn_start` 的两遍发射（30 双方都发 / 31 只发给 owner==行动方）—— 账 643");
        F2Case1TurnStartTwoPasses();
    }

    /// <summary>
    /// **账 643 `BattleManagerSupport.BroadcastTurnStart` + `CardScript.OnTurnStart`**
    ///
    /// <para>判据（🥇 `decomp_full/CardScript__OnTurnStart.c:137-146` 逐句，本代理 2026-10-28 亲读）：</para>
    /// <code>
    /// RawCardScript__OnTrigger(raw, 0x1e /*30 TurnStart*/, mgr, this, this, 0, 0);   // :137-139 **无条件**
    /// if (param_2 /*isPlayerTurn*/ != *(char*)(this + 0x40) /*this.isPlayer*/) return; // :140
    /// RawCardScript__OnTrigger(raw, 0x1f /*31 TurnStartPlayer*/, mgr, this, this, 0, 0); // :144-146
    /// </code>
    /// <para>⇒ **31 只发给「卡主 == 行动方」的卡**；30 双方都发。改前我们是「按卡二选一」
    /// ⇒ 非行动方那张只带 31 的卡**也会**收到 31（比原版宽）。</para>
    /// </summary>
    static void F2Case1TurnStartTwoPasses()
    {
        // 🔴 2026-10-28（`B93_FX2` · `B93_DG1` ⑥⑦ 定性 = δ 夹具前提不成立）：`ResolveAtTurn` 的**作业表只由正文解析产生**
        //   —— `EffectResolver.cs:8781` `AtTurnClauses(u.Card.Desc)` + `:8826` `if (total == 0) return;`
        //   ⇒ 夹具卡 `desc = ""` 时**连发射段都进不去**（30/31 一条都不会发），与实现无关。
        //   ⚠️ **必须 `each`**（`:8784`：`view != "each" && pi != active ⇒ continue`）—— 写成 `your turn` 会让
        //   非行动方那张（`u31p1`）压根不进作业表 ⇒ 第 75 行那条「不收 31」变成**假绿**。
        const string AtTurnEach = "At the start of each turn, deal 1 damage to a random enemy";
        var c30 = new CardDef("F2T30", "F2T30", "unit", AtTurnEach, null, "Test", 1, 1, 5, 0,
                              abilityTriggers: new[] { 30 }, abilityTriggersVersioned: true);
        var c31 = new CardDef("F2T31", "F2T31", "unit", AtTurnEach, null, "Test", 1, 1, 5, 0,
                              abilityTriggers: new[] { 31 }, abilityTriggersVersioned: true);

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var u30 = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), c30);
        var u31p0 = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 2), c31);
        var u31p1 = Place(ctx, 1, BoardSlots.SlotOf(BoardSlots.Right, 1), c31);

        // 🧨 前提硬检查（**先断前提、再断结论**；⛔ 不写 `if (前提) Check(结论)`）
        CheckTrue(u30 != null && u30.Card == c30, "🧨 前提：P0 的 30 卡真的上了场");
        CheckTrue(u31p0 != null && u31p0.Card == c31, "🧨 前提：P0 的 31 卡真的上了场");
        CheckTrue(u31p1 != null && u31p1.Card == c31, "🧨 前提：P1 的 31 卡真的上了场");
        Check(c30.AbilityTriggerPresence(30), 1, "🧨 前提：30 卡的 `abilityTriggers` 认得出 30");
        Check(c31.AbilityTriggerPresence(31), 1, "🧨 前提：31 卡的 `abilityTriggers` 认得出 31");
        Check(ctx.Active, 0, "🧨 前提：行动方 = P0");
        // 🧨 前提：正文**真的解析出** `turn_start` + `each` 子句（作业表非空的唯一来源；写成 `your turn` ⇒ 第 3 条假绿）
        var cl30 = EffectText.AtTurnClauses(c30.Desc);
        CheckTrue(cl30.Count == 1 && cl30[0].Phase == "turn_start" && cl30[0].View == "each",
                  "🧨 前提：30 卡的正文解析出**一条** `turn_start` + `each` 子句（`desc` 空 ⇒ `ResolveAtTurn` 在 `total == 0` 就 return）");

        ctx.Signals.Clear();
        RuleCore.ResolveAtTurn(ctx, "turn_start");

        int t30 = 0, p0_31 = 0, p1_31 = 0;
        foreach (var e in ctx.Signals)
        {
            if (e.Kind != EvtKind.Trigger) continue;
            if (e.Keyword == "TurnStart") t30++;
            else if (e.Keyword == "TurnStartPlayer")
            {
                if (e.Player == 0) p0_31++;
                else if (e.Player == 1) p1_31++;
            }
        }

        // 🧨 30 无条件发（本夹具只放了一张 30 卡；若把第 1 遍也加上 owner 闸，这条会红）
        Check(t30, 1, "🧨 `TurnStart(30)` 双方都发 —— 本夹具只有一张 30 卡 ⇒ 恰好 1 次");
        // 🧨 行动方那张 31 要收到
        Check(p0_31, 1, "🧨 行动方(P0) 的 31 卡收到一次 `TurnStartPlayer`");
        // 🧨 非行动方那张 31 **不许**收到（改回「按卡二选一」⇒ 这条立刻红）
        Check(p1_31, 0, "🧨 非行动方(P1) 的 31 卡**不**收到 31（原版 `CardScript__OnTurnStart.c:140` 的 owner 闸）");
    }
}
