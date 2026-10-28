// RuleEngineTest_EngineB94Z3.cs — 执行代理 `Z3`（2026-10-29 · 第九十四会话 · 波 2 · 文件所有者 = `Core/RuleCore.cs`）
//
// 为什么单开一个文件：简报指定的断言宿主 `Editor/RuleEngineTest.cs` **归调度台**（接线归它）
// ⇒ 本代理**不碰那个文件**（一个文件同一时刻只能有一个写手）。
// 接线只差 `RuleEngineTest.cs` 的 `Run()` 里**一行**：
//     Step(CheckB94Z3RuleCoreGaps);
//
// 🔴 每条断言都带 `🧨`（写清什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（接线 = `RuleEngineTest.cs` 的 `Run()` 里一行 `Step(CheckB94Z3RuleCoreGaps)`）。
    /// ⚠️ 方法名**不以 `Test` 打头**（简报要求：入口用 `CheckB94Z3…` 这一族，免得与宿主里既有的 `Test*` 命名混）。</summary>
    public static void CheckB94Z3RuleCoreGaps()
    {
        Section("🆕 引擎线 Z3 · B94：`UseActiveAbilityCore` 的计划表要收 `chosenAbility 360`（账 455 的上游那一行）");
        B94Z3Case1PlanIncludes360();
        B94Z3Case2NoSelectionStillConsumesAction();
    }

    /// <summary>
    /// **账 455 的上游那一行** —— `RuleCore.UseActiveAbilityCore` 的 `else if` 里加了 `|| ca == 360`
    /// （`Core/RuleCore.cs` 里 `plan.Add` 那一行；判据 `decomp_full/AbilityLogic__PlayAbility.c:1994` +
    /// `:2014-2070`，执行器 = `EffectResolver.RunPlayChosenCardAbility`）。
    ///
    /// <para>**这一条改的是什么**：加之前，`chosenAbility 360` 只有当该行**另外**配到正文 op
    /// （`ActiveRowHasOps`）时才进计划表；正文没配到 op 的 360 行会被当成 `unimplemented` **剔掉** ⇒
    /// `plan.Count == 0` ⇒ 在**费用/动作经济之前**就 `return ErrUnimplemented` ⇒ 整支**永远走不到**。</para>
    ///
    /// <para>⚠️ **现池可达性（如实标）**：`cards_engine.json` 现读 **0 张卡**的 `activeAbilityLogic` 带 `ca == 360`
    /// （那 18 张 360 卡全在**普通** `abilityLogic` 上、走正文解析那条路）⇒ 本格今天**只能由夹具驱动**。
    /// 保留它的理由 = 结构照原版（`PlayAbility` 的 `switch` 里 360 是独立一支）；数据一变立刻可达。</para>
    /// </summary>
    static void B94Z3Case1PlanIncludes360()
    {
        var uCard = B94Z3ActiveAbilityCard360("Z3U360");
        // 选中的那张：`Rally:` = 卡面写法的 `ThisCardPlayed(0)`（`ActionPump` 的 `AbilityTriggerNameOfKeyword`）
        var chosen = new CardDef("Z3C1", "Z3C1", "unit", "Rally: Gain +1 Attack", null, "Test", 1, 1, 5, 0);

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        int slot = BoardSlots.SlotOf(BoardSlots.Right, 1);
        var unit = Place(ctx, 0, slot, uCard);

        // 🧨 前提硬检查（**先断前提、再断结论**；⛔ 不写 `if (前提) Check(结论)`）
        CheckTrue(unit != null && unit.Card == uCard, "🧨 前提：360 那张主动能力单位真的上了场");
        CheckTrue(unit.ActiveAbilityAvailable, "🧨 前提：`hasActiveAbility == 1` ⇒ 主动能力这条入口开着");
        var rows = uCard.ActiveAbilityLogicGroup(CardDef.ActiveAbilityObject);
        CheckTrue(rows != null && rows.Count == 1, "🧨 前提：`activeAbilityLogic` 恰好一行（`ab = -1`）");
        Check(rows[0].Gate, 1, "🧨 前提：那一行 `gate == 1`（`gate = 0` 是死数据，计划表会 `continue` 掉）");
        CheckTrue(rows[0].ChosenAbility != null && rows[0].ChosenAbility.Length == 1
                  && rows[0].ChosenAbility[0] == 360, "🧨 前提：那一行的 `chosenAbility` 就是 360");
        CheckTrue(chosen.TriggerOps(KeywordTable.Rally) != null,
                  "🧨 前提：选中的那张正文解析得出 `rally` 族（= `ThisCardPlayed(0)` 的卡面写法）");
        CheckTrue(ctx.CardChosenInSelection == null, "🧨 前提：`CardChosenInSelection` 初始 = null");

        ctx.CardChosenInSelection = chosen;
        int code = RuleCore.UseActiveAbility(ctx, 0, slot);

        // 🧨 关键一条：把 `|| ca == 360` 拿掉 ⇒ 这一行被剔进 `unimplemented` ⇒ `plan.Count == 0` ⇒
        //   `ErrUnimplemented`（而且**在动作经济之前**就返回）⇒ 这条立刻红。
        Check(code, RuleCodes.OK,
              "🧨 360 行进了计划表 ⇒ 执行器真的跑了一次（返回 OK 而不是 `ErrUnimplemented`）");
        // 🧨 原版 `AbilityLogic__PlayAbility.c:2060-2068`：跑完把 `bm+0x2e8` 清空
        //   —— 只有真进了 `RunPlayChosenCardAbility` 才会清；计划表漏掉 360 ⇒ 这一格永远留着。
        CheckTrue(ctx.CardChosenInSelection == null,
                  "🧨 跑完 `CardChosenInSelection` 被清空（原版 `:2060-2068` 的 `bm+0x2e8 = 0`）");
        // 🧨 动作经济（原版 `ResolveActiveAbilityPlayed.c:31-32`）
        CheckTrue(unit.Exhausted, "🧨 主动能力用掉 ⇒ 该单位本回合 `Exhausted`");
    }

    /// <summary>
    /// 同一条 360 行、**但这次没有选中任何卡**（`CardChosenInSelection == null`）。
    ///
    /// <para>**原版**：`AbilityLogic__PlayAbility.c:2015-2019` —— `bm+0x2e8 == null` ⇒ **这一支什么都不做**，
    /// 连日志都没有；而动作经济（`ResolveActiveAbilityPlayed.c:31-32`）**已经在进 `PlayAbility` 之前落过**
    /// ⇒ 「能力用掉了、但什么都没发生」是**原版行为**，⛔ 不是缺陷。</para>
    ///
    /// <para>⚠️ **我们这一档的差异（如实记、不写断言）**：执行器返回 0 ⇒ `executed == 0` ⇒
    /// `UseActiveAbilityCore` 末尾仍会回 `ErrUnimplemented`，`BattleDriver` 那边会弹一条
    /// 「这一手打不出去」的提示 —— 原版没有这条提示。今天**不可达**（现池 0 张卡的
    /// `activeAbilityLogic` 带 360），已记进 `报告/B94_Z3.md` §没查清/残余。</para>
    /// </summary>
    static void B94Z3Case2NoSelectionStillConsumesAction()
    {
        var uCard = B94Z3ActiveAbilityCard360("Z3U360b");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        int slot = BoardSlots.SlotOf(BoardSlots.Right, 1);
        var unit = Place(ctx, 0, slot, uCard);

        CheckTrue(unit != null && unit.Card == uCard, "🧨 前提：360 那张主动能力单位真的上了场");
        CheckTrue(unit.ActiveAbilityAvailable, "🧨 前提：主动能力入口开着");
        CheckTrue(ctx.CardChosenInSelection == null, "🧨 前提：`CardChosenInSelection` 是 null（本用例的前提）");
        CheckTrue(!unit.Exhausted, "🧨 前提：发动之前该单位**没有** `Exhausted`");

        RuleCore.UseActiveAbility(ctx, 0, slot);

        // 🧨 这一条**同样**能抓住「把 `|| ca == 360` 拿掉」：老代码在 `plan.Count == 0` 就 return，
        //   **走不到**动作经济那一段 ⇒ `Exhausted` 仍是 false ⇒ 红。
        CheckTrue(unit.Exhausted,
                  "🧨 360 行被计划表收下之后，动作经济照原版先落（`ResolveActiveAbilityPlayed.c:31-32`）—— "
                  + "即使选中的那张是空的（原版 `:2015-2019` 什么都不做，但行动已经花掉）");
        CheckTrue(ctx.CardChosenInSelection == null, "🧨 没有选中 ⇒ 那一格保持 null（执行器不会凭空造一张）");
    }

    /// <summary>360 夹具卡：`hasActiveAbility = 1` + 一行 `gate = 1` / `ta = 0 (any)` / `ca = 360`。
    /// <para>`ta = 0`（**不是** 30/220）⇒ `ActiveAbilityNeedsPick` 回 false ⇒ 不用先点目标，
    /// `CanUseActiveAbility` 那一层不会挡（`RuleCore.ActiveAbilityNeedsPick` 的 doc）。</para></summary>
    static CardDef B94Z3ActiveAbilityCard360(string id)
    {
        var row = new CardDef.ActiveAbilityLogicSpec
        {
            Ability = CardDef.ActiveAbilityObject,
            Gate = 1,
            Trigger = 400,
            NeedsLoading = 0,
            TargetsAffected = new[] { 0 },
            FurtherMultiplier = new[] { 0 },
            ChosenAbility = new[] { 360 },
            PlayersAffected = new[] { 0 },
        };
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                           hasActiveAbility: 1,
                           activeAbilityLogic: new[] { row },
                           activeAbilityLogicVersioned: true);
    }
}
