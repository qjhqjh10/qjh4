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

    /// <summary>第九十四会话（`B94_F2`）新增的**唯一入口** —— 接线 = `RuleEngineTest.cs` 的 `Run()` 里一行
    /// `Step(TestF2B94);`（⚠️ 那个文件归 F1，本代理**没有加**）。</summary>
    public static void TestF2B94()
    {
        Section("🆕 引擎线 F2 · B94：`chosenAbility 360` · `TriggerOtherCardAbility` · `BroadcastRemoveFromGame` · `UnitHealed` 的 actor/target");
        F2B94Case1PlayChosenCard();
        F2B94Case2TriggerOtherCardAbility();
        F2B94Case4UnitHealedActorTarget();
    }

    /// <summary>
    /// **账 455 `chosenAbility 360 = playChosenCardAbility`**（`AbilityLogic__PlayAbility.c:2014-2070`）。
    /// 三条：① 没有选中的卡 ⇒ `0`；② 有 ⇒ 按 `ThisCardPlayed(0)`（卡面 `rally`）放**一次**并把
    /// `CardChosenInSelection` **清空**；③ 清空后再调又是 `0`。
    /// </summary>
    static void F2B94Case1PlayChosenCard()
    {
        var uCard = new CardDef("F2U1", "F2U1", "unit", "", null, "Test", 1, 1, 5, 0);
        var chosen = new CardDef("F2C1", "F2C1", "unit", "Rally: Gain +1 Attack", null, "Test", 1, 1, 5, 0);
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var unit = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), uCard);

        CheckTrue(unit != null && unit.Card == uCard, "🧨 前提：主动能力那张真的上了场");
        CheckTrue(ctx.CardChosenInSelection == null, "🧨 前提：`CardChosenInSelection` 初始 = null");
        Check(RuleCore.RunPlayChosenCardAbility(ctx, 0, unit, null), 0,
              "🧨 没有选中的卡 ⇒ 这一支**什么都不做**（原版 `PlayAbility.c:2015-2019`）");
        CheckTrue(chosen.TriggerOps(KeywordTable.Rally) != null,
                  "🧨 前提：选中的那张正文解析得出 `rally` 族（= `ThisCardPlayed(0)` 的卡面写法）");

        ctx.CardChosenInSelection = chosen;
        int ran = RuleCore.RunPlayChosenCardAbility(ctx, 0, unit, null);
        Check(ran, 1,
              "🧨 选中那张**不带 oath** ⇒ 只放一次。把正文键写回字符串 \"ThisCardPlayed\"（`TriggerOps` 恒 null）或把 `times` 算错，这条立刻红");
        CheckTrue(ctx.CardChosenInSelection == null,
                  "🧨 原版末尾清空 `bm+0x2E8`（`PlayAbility.c:2060-2068`）—— 不清这条红");
        Check(RuleCore.RunPlayChosenCardAbility(ctx, 0, unit, null), 0,
              "🧨 清空之后再调 ⇒ 又是 0（证明上面那条清空**真的生效**，不是偶合）");
    }

    /// <summary>**账 947 `CardScript.TriggerOtherCardAbility`**：`ThisCardPlayed` 硬编码、`thisCard = actingCard = 调用者`、`target = null`。</summary>
    static void F2B94Case2TriggerOtherCardAbility()
    {
        var withBody = new CardDef("F2C2", "F2C2", "unit", "Rally: Gain +1 Attack", null, "Test", 1, 1, 5, 0);
        var noBody = new CardDef("F2C3", "F2C3", "unit", "", null, "Test", 1, 1, 5, 0);
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var caller = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1),
                           new CardDef("F2U2", "F2U2", "unit", "", null, "Test", 1, 1, 5, 0));

        CheckTrue(caller != null, "🧨 前提：调用者真的上了场");
        CheckTrue(RuleCore.TriggerOtherCardAbility(ctx, withBody, 0, caller),
                  "🧨 原版把 trigger 硬编码成 `ThisCardPlayed`（`out_il2csharp/CardScript.cs:2187-2189`）⇒ 有 `rally` 正文就该跑起来");
        CheckTrue(!RuleCore.TriggerOtherCardAbility(ctx, noBody, 0, caller),
                  "🧨 正文解析不出来的卡 ⇒ **不假装跑过**（工程红线：不许静默失败）");
    }

    // ⚠️ 账 637 的 `BroadcastRemoveFromGame` **没有断言**：它是 `internal`（`RuleCorePartial_B88E1.cs`），
    //    而自检宿主在**编辑器程序集**（本仓无 `InternalsVisibleTo`，见 `EffectResolver.cs:7794`）⇒ 看不见。
    //    要么由 F1 把它改 `public`，要么由 F1 在 `RuleEngineTest.cs` 里加一条直调断言。本轮如实记在报告 §三。

    /// <summary>**账 830 `BroadcastUnitHealed`**：三跳（棋盘 `IsInPlay` / 当前方手牌 / 另一方手牌），
    /// 被治疗者**自己**也要收到（`.c:25-53` 预调 = 棋盘第一跳就把自己算在内）。</summary>
    static void F2B94Case4UnitHealedActorTarget()
    {
        var watcher = new CardDef("F2H", "F2H", "unit", "When this unit is healed, gain +1 Attack", null, "Test", 1, 1, 5, 0);
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var h = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 1), watcher);
        CheckTrue(h != null && h.Card.WhenTriggers.Count > 0,
                  "🧨 前提：`When this unit is healed, …` 收成了 event 层监听（`WhenTriggers` 非空）");

        int before = h.CurrentMeleeAttack;
        RuleCore.BroadcastUnitHealed(ctx, 0, h);
        Check(h.CurrentMeleeAttack, before + 1,
              "🧨 被治疗者自己的 `When … healed` 必须响（原版 `BroadcastUnitHealed.c:25-53` 的棋盘那一跳）");
    }
}
