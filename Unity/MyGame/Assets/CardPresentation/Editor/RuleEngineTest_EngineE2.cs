// RuleEngineTest_EngineE2.cs — 执行代理 `E2` 三条（2026-10-24）的回归断言
//
// 判据（⛔ 不要重新论证）：
//   · ① `PreviousAbilityTargets` 在结算中是上一张卡的残值 → `资料/普查产出_1024/E8_ta150空表回归.md` §六·2
//        原版逐句 → `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c`（每个 `case` 出口都
//        `uVar18 = *(param_1 + 0x80); … AbilityLogic__SetPreviousAbilityTargets(param_1,uVar18,0);`）
//        ＋ `AbilityLogic__SetPreviousAbilityTargets.c`（`Array.Clear` + `AddRange`，整表覆盖）
//        ＋ `AbilityLogic__DeepCopy.c`（**不拷贝** `+0x80` ⇒ 每次 play 从空开始）
//   · ② `EvtKind` 三处 `default` 静默兜住 → `资料/普查产出_1023e/J2_引擎结构自检.md` §四·3
//   · ③ `UM77`「died since last turn」被正文计成「本回合」→ `资料/普查产出_1023e/N2_八个role.md` §五·③
//        原版 → `d:/2/tools/decomp_full/CardScript__HasDiedSinceLastTurn.c:26-47`
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）。期望值一律写死字面量，⛔ 不从被测实现反算。
// 🔴 灭自证：②③ 的「新口径」各配一条「旧口径会是另一个数」的读数，结构上不可能同时满足。
//
// ✅ 接线：`RuleEngineTest.Run()` 里加一行 `Step(TestEngineE2);`
//    （⛔ 本文件不碰 `RuleEngineTest.cs` —— 它不在本代理白名单里；没接线时 `Run()` 的反射闸会红）
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`Run()` 里一行 `Step(TestEngineE2)`）。</summary>
    public static void TestEngineE2()
    {
        Section("🆕 执行代理 `E2`：`PreviousAbilityTargets` 逐 op 同步 · `EvtKind` 未登记出声 · `UM77` 自上一回合以来");
        E2T1PerOpFreshness();
        E2T2UnionAcrossOps();
        E2T3EvtKindNotSilent();
        E2T4Um77RealCard();
    }

    // ==================================================================================================
    //  ① 逐 op 同步：同一次结算里**后面**那条 op 读 `ta = 150`，拿到的必须是**本条 ability 已解出的目标**
    // ==================================================================================================

    /// <summary>
    /// **改前**（`A1` 第一版）：`PreviousAbilityTargets` 只在 `ResolveOps` 出口同步一次，取的是
    /// `ctx.LastTargets` ⇒ 结算过程中读到的一直是**上一张卡**留下的值。
    /// 夹具：场上 P0 三个友方部队（`A/B/C`）＋ P1 一个 30 血部队。
    ///   · 能力 1（上一张卡）= `Give +1 Attack to all friendly troops` ⇒ 槽 = **3**；
    ///   · 能力 2 = ① `Give +1 Attack to a friendly troop`（只打 **1** 个）
    ///             ② 手搓 `deal`（`CountRef = own|troop|all` 正文数 = 3，但探针卡的 `mult` 行
    ///                `targetsAffected = 150` ⇒ 结构化乘数优先，按**槽**数）
    /// ⇒ **改后**：第 ② 条读到的槽 = 1（能力 2 已解出的）⇒ **掉 1 血**；
    ///   **改前**：读到的槽 = 3（能力 1 的残值）⇒ **掉 3 血**。
    /// 🧨 把 `NotePreviousAbilityTargets` 退回「只在出口调一次」（或删掉两个主循环里那 4 处调用）
    ///    ⇒ 实得 3 ⇒ 红；🧨 把 `testCount`（正文）当结算依据 ⇒ 也是 3 ⇒ 红。
    /// </summary>
    static void E2T1PerOpFreshness()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 0, 1, Unit("E2T1A", 1, 1, 9));
        Place(ctx, 0, 3, Unit("E2T1B", 1, 1, 9));
        Place(ctx, 0, 5, Unit("E2T1C", 1, 1, 9));
        var foe = Place(ctx, 1, 3, Unit("E2T1Foe", 1, 1, 30));
        Check(foe.Health, 30, "E2T1 前提：对手那个部队 30 血");

        // ---- 能力 1（上一张卡）：打**全部** 3 个友方部队 ----
        var opsAll = EffectText.Parse("Give +1 Attack to all friendly troops", out _, out _);
        CheckTrue(opsAll != null && opsAll.Count == 1,
                  "E2T1 前提：`Give +1 Attack to all friendly troops` 解析出 1 条 op");
        if (opsAll == null || opsAll.Count != 1) return;
        List<string> un1;
        RuleCore.ResolveOps(ctx, 0, null, opsAll, null, out un1);
        Check(ctx.PreviousAbilityTargets.Count, 3,
              "E2T1 前提：上一张卡的目标表 = **3**（`all friendly troops`）"
            + "｜🧨 出口同步整条没了 ⇒ 0 ⇒ 红");

        // 先单独钉「`ta = 150` 的计数本身是对的」（把失败点拆开：这是 CountEval，下面那条才是同步）
        var own150 = A1CountSet(150, friendly: 1, enemy: 0, hero: 0, minions: 1);
        string gap150;
        Check(CountEval.CountTargets(ctx, 0, own150, 0, null, out gap150), 3,
              "E2T1 前提：此刻 `ta=150` 数「友方部队」= **3**（能力 1 留下的表，`E2T1A/B/C`）");

        // ---- 能力 2：先打 1 个，再按 `ta = 150` 数遍数 ----
        var opsOne = EffectText.Parse("Give +1 Attack to a friendly troop", out _, out _);
        CheckTrue(opsOne != null && opsOne.Count == 1,
                  "E2T1 前提：`Give +1 Attack to a friendly troop` 解析出 1 条 op（单数 ⇒ `Count = 1`）");
        if (opsOne == null || opsOne.Count != 1) return;

        var dealOp = new EffectOp
        {
            Verb = "deal", Amount = 1,
            // ⚠️ 正文计数故意写成「全部友方部队」= 3：它和「槽」在**改前**都是 3、**改后**才分叉（1 vs 3）。
            CountRef = "own|troop|all", CountScope = "board",
            Target = new EffectTargetSpec
            {
                Raw = "an enemy troop", Side = "enemy", Kind = "troop", Count = 1, Auto = true,
            },
            AbilityIndex = 0, LogicIndex = 0,
            Source = "E2 探针：按 `ta = 150` 数遍数",
        };
        var ops2 = new List<EffectOp> { opsOne[0], dealOp };
        List<string> un2;
        RuleCore.ResolveOps(ctx, 0, null, ops2, null, out un2, E2MultProbeCard());

        Check(foe.Health, 29,
              "★★ E2T1（灭自证）同一次结算里第 ② 条读到的槽 = **本条 ability 已解出的 1 个** ⇒ 掉 **1** 血"
            + $"（实得 {30 - foe.Health} 点）"
            + "｜🧨 退回「只在出口同步一次」⇒ 读到上一张卡的 3 ⇒ 掉 3 血 ⇒ 红；"
            + "🧨 结构化乘数没接上（`mult` 行 / `AbilityIndex+LogicIndex` 对不上）⇒ 退回正文的 3 ⇒ 红");
    }

    /// <summary>探针卡：正文带 `for each`，`mult` 行的 `targetsAffected = 150`（`previousAbilityFiltered`）。
    /// ⚠️ `CountSetSpec` 里本版未实现的维度**必须显式写 0**（缺省 `-1` 会被 `CountEval.SetGap` 判成「没落」）
    /// —— 直接复用 `A1` 那份 `A1CountSet`（同一份口径，⛔ 别抄第二份）。</summary>
    static CardDef E2MultProbeCard()
    {
        var conds = new List<CardDef.ConditionSpec>
        {
            new CardDef.ConditionSpec
            {
                Ability = 0, Logic = 0, Role = "mult", Condition = 0, Name = null, Exclude = false,
                MultiplierSet = A1CountSet(150, friendly: 1, enemy: 0, hero: 0, minions: 1),
            },
        };
        var ats = new[]
        {
            // 兜底那条（`FindSoleMultiplierRow`）也要求「整卡一条 ability、一条 logic」—— 一并满足，
            // 这样无论解析层给 `AbilityIndex` 归到 0 还是 -1，`mult` 行都取得到。
            new CardDef.AbilityTargetSpec { TargetsAffected = new[] { 150 } },
        };
        return new CardDef("E2_MultProbe", "E2 Mult Probe", "tactic",
                           "Deal 1 damage to an enemy troop for each friendly troop",
                           null, "Test", 0, 0, 0, 0, null,
                           abilityConditions: conds, conditionsVersioned: true,
                           abilityTargets: ats);
    }

    // ==================================================================================================
    //  ② 并集：同一条 ability 里「先打全体、再打一个」⇒ 槽 = 并集（⛔ 不是「最后那一条」）
    // ==================================================================================================

    /// <summary>
    /// 原版 `abilityTargetsToSave(+0x80)` 在一条 ability 内**只增不减**，每个效果出口把**当前整表**
    /// 覆盖进静态表 ⇒ 静态表 = **至今解出的并集**。
    /// 夹具：P0 三个友方部队；一条 ability 两条 op —— ① 全体（3 个）② 一个（1 个）。
    /// ⇒ **改后**：槽 = **3**；**改前**：出口那次取 `ctx.LastTargets` = 最后那条的 **1**。
    /// 🧨 把出口同步改回 `Clear()+AddRange(ctx.LastTargets)` 且删掉逐 op 调用 ⇒ 实得 1 ⇒ 红；
    ///    🧨 把并集做成「跨 ability 累积」⇒ 下面第三次调用的读数变成 6 ⇒ 红。
    /// </summary>
    static void E2T2UnionAcrossOps()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 0, 1, Unit("E2T2A", 1, 1, 9));
        Place(ctx, 0, 3, Unit("E2T2B", 1, 1, 9));
        Place(ctx, 0, 5, Unit("E2T2C", 1, 1, 9));

        var opsAll = EffectText.Parse("Give +1 Attack to all friendly troops", out _, out _);
        var opsOne = EffectText.Parse("Give +1 Attack to a friendly troop", out _, out _);
        CheckTrue(opsAll != null && opsAll.Count == 1 && opsOne != null && opsOne.Count == 1,
                  "E2T2 前提：两句都解析出 1 条 op");
        if (opsAll == null || opsAll.Count != 1 || opsOne == null || opsOne.Count != 1) return;

        var ops = new List<EffectOp> { opsAll[0], opsOne[0] };
        List<string> unresolved;
        RuleCore.ResolveOps(ctx, 0, null, ops, null, out unresolved);

        Check(ctx.PreviousAbilityTargets.Count, 3,
              "★★ E2T2 一条 ability 里「先打全体 3 个、再打 1 个」⇒ 槽 = **并集 3**"
            + $"（实得 {ctx.PreviousAbilityTargets.Count}）"
            + "｜🧨 退回「出口只取 `ctx.LastTargets`」⇒ 实得 1 ⇒ 红");

        // 再开一条 ability（只打 1 个）⇒ 槽必须**降回** 1（起手清空 + 只并这一条）
        List<string> unresolved2;
        RuleCore.ResolveOps(ctx, 0, null, opsOne, null, out unresolved2);
        Check(ctx.PreviousAbilityTargets.Count, 1,
              "★★ E2T2（灭自证）下一条 ability 只打了 1 个 ⇒ 槽 = **1**（起手清空，⛔ 不是跨 ability 累积的 3/6）"
            + $"（实得 {ctx.PreviousAbilityTargets.Count}）"
            + "｜🧨 起手不清 `AbilityTargetsSaved` ⇒ 仍是 3 ⇒ 红");
    }

    // ==================================================================================================
    //  ③ `EvtKind` 未登记 ⇒ 三处 `default` 不再静默兜住
    // ==================================================================================================

    /// <summary>
    /// **改前**：`EventTiming` 的 `IsSourced` / `SourceOf` / `DurationOf(EvtKind)` 三处 `default:`
    /// 一把兜住（`true` / `"（无）"` / `0f`）⇒ 往 `EvtKind` 里加一档**不登记也全绿**（静默失效）。
    /// **改后**：三处先查登记表（`ZeroDurationKinds` + `TimedKinds`），未登记 ⇒ 出声 + 计数，
    /// 且 `IsSourced` 回 **false**（`BattleScene` 那条 `unsourced == 2` 会因此变红）。
    /// 🧨 把三处 `default:` 改回 `return true;` / `return "（无）";` / `return 0f;` ⇒ 下面「不登记也全绿」的两条红。
    /// </summary>
    static void E2T3EvtKindNotSilent()
    {
        // ① **全枚举登记齐全**：这一条会在「下一个人往枚举里加一档却不登记」时红（这是那种缺陷的唯一闸）
        int missing = 0;
        string missingNames = "";
        foreach (EvtKind k in Enum.GetValues(typeof(EvtKind)))
            if (!EventTiming.IsRegisteredEventKind(k)) { missing++; missingNames += k + " "; }
        Check(missing, 0,
              "★ E2T3 ① `EvtKind` **每一档**都在登记表里（`ZeroDurationKinds` 或 `TimedKinds`）"
            + (missing > 0 ? $"—— 缺：{missingNames}" : "")
            + "｜🧨 往枚举里加一档却不登记 ⇒ 本条红（`EventTiming.ZeroDurationKinds` 上面那段是登记办法）");

        // ② 故意造一个未登记的档位 ⇒ 三处都必须「出声 / 报无出处」，计数要涨
        var bogus = (EvtKind)999;
        CheckTrue(!EventTiming.IsRegisteredEventKind(bogus), "E2T3 ② 前提：`(EvtKind)999` 确实没登记");

        EventTiming.ResetUnregisteredKindForTest();      // 🔴 只清**计数**（不清「已喊过」的闩）
        int hitsBefore = EventTiming.UnregisteredKindHits;
        string why999;
        bool sourced999 = EventTiming.IsSourced(bogus, out why999);
        float dur999 = EventTiming.DurationOf(bogus);
        string src999 = EventTiming.SourceOf(bogus);

        CheckTrue(!sourced999 && !string.IsNullOrEmpty(why999),
                  "★★ E2T3 ② 未登记的 `EvtKind` ⇒ `IsSourced` = **false** 且带原因"
                + $"（实得 {sourced999} / 原因「{why999}」）"
                + "｜🧨 三处 `default:` 改回 `return true;` ⇒ 实得 true（旧口径）⇒ 红");
        CheckTrue(dur999 == 0f,
                  "E2T3 ② 未登记档的时长仍是 **0**（行为不变，只是**不再静默**）");
        CheckTrue(!string.IsNullOrEmpty(src999) && src999.Contains("未登记"),
                  $"★ E2T3 ② `SourceOf` 明写「未登记」（实得「{src999}」）"
                + "｜🧨 改回 `return \"（无）\";` ⇒ 它看起来和「已登记的零时长档」一模一样 ⇒ 红");
        CheckTrue(EventTiming.UnregisteredKindHits > hitsBefore,
                  "★ E2T3 ② 未登记计数**涨了**（`ReportUnregisteredKind` 被调到）"
                + $"（{hitsBefore} → {EventTiming.UnregisteredKindHits}）"
                + "｜🧨 三处 `default:` 不调 `ReportUnregisteredKind` ⇒ 计数不动 ⇒ 红");

        // ③ 已登记档的读数**一个字不变**（防「登记表把老行为改坏」）
        CheckTrue(EventTiming.IsSourced(EvtKind.ShowCardsWindow)
                  && EventTiming.DurationOf(EvtKind.ShowCardsWindow) == 0f
                  && EventTiming.SourceOf(EvtKind.ShowCardsWindow) == "（无）",
                  "E2T3 ③ 已登记的零时长档（`ShowCardsWindow`）读数不变：`IsSourced` = true · 时长 0 · 出处「（无）」"
                + "｜🧨 把「已登记」也判成未登记（只查零时长表/只查有时长表）⇒ 红");
        CheckTrue(EventTiming.IsSourced(EvtKind.Hit) && EventTiming.DurationOf(EvtKind.Hit) > 0f,
                  "E2T3 ③ 已登记的**有时长**档（`Hit`）仍算「有出处」且时长 > 0"
                + "｜🧨 `IsSourced` 只查 `ZeroDurationKinds` ⇒ `Hit` 被判未登记 ⇒ 红");

        EventTiming.ResetUnregisteredKindForTest();      // 别把上面故意造的那一条留给后面的断言
    }

    // ==================================================================================================
    //  ④ `UM77 Chairon`：真卡正文 → `diedsincelastturn`（本回合 + 上一回合），⛔ 不是 `died`（本回合）
    // ==================================================================================================

    /// <summary>
    /// **改前**：正文 `Repeat for each friendly troop that died since your last turn` 被归成
    /// `CountScope = "died"`，而 `died` 的实现是 `ctx.DiedThisTurn`（回合开始清零）⇒
    /// **对手回合末被打死的兵不算**（少打一遍，静默）。
    /// **改后**：独立 scope `diedsincelastturn`，窗口 = `ctx.Turn` / `ctx.Turn - 1`
    /// （原版 `CardScript__HasDiedSinceLastTurn.c:26-47`）。
    /// 🧨 把 `diedsincelastturn` 并回 `died` ⇒ 「过一个回合」那一格实得 0 ⇒ 红。
    /// </summary>
    static void E2T4Um77RealCard()
    {
        var pool = CardDatabase.Load();
        var um77 = CardDatabase.FindById(pool, "UM77");
        CheckTrue(um77 != null, "E2T4 前提：`UM77 Chairon` 在池内");
        if (um77 == null) return;

        // ---- 真卡正文 → op（⛔ 不用手写字面量：要钉的是**这张卡实际印的那句**）
        var parsed = EffectText.Parse(um77.Desc, out _, out _);
        CheckTrue(parsed != null && parsed.Count > 0, "E2T4 前提：`UM77` 的正文解析出 op");
        if (parsed == null || parsed.Count == 0) return;
        EffectOp since = null;
        for (int i = 0; i < parsed.Count; i++)
            if (parsed[i] != null && parsed[i].CountScope == "diedsincelastturn") since = parsed[i];
        CheckTrue(since != null,
                  "★★ E2T4 真卡 `UM77` 的正文里那条 `repeat` 的 `CountScope` = `diedsincelastturn`"
                + "｜🧨 把它归回 `died` ⇒ 本处找不到 ⇒ 红（那正是改前的静默形状）");
        if (since == null) return;
        Check(since.CountRef, "own|troop",
              "E2T4 真卡是「**friendly** troop」⇒ `own|troop`"
            + "｜🧨 把 `since your last turn` 里的 `your` 当侧位判成 `enemy` ⇒ 红");

        // ---- 计数：本回合死 1 个友军 ⇒ 1；过一个回合 ⇒ **仍 1**；再过一回合 ⇒ 0 ----
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        Place(ctx, 0, 3, Unit("E2T4Killer", 1, 3, 10), exhausted: false);
        Place(ctx, 1, 3, Unit("E2T4Victim", 1, 0, 1), exhausted: true);
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "E2T4 前提：砍死 P1 那个 1 血部队");
        Check(ctx.DiedThisTurn, 1, "E2T4 前提：`DiedThisTurn = 1`");

        var thisTurn = new EffectOp { CountScope = "died", CountRef = "unit" };
        Check(RuleCore.CountForTest(ctx, 1, null, since), 1,
              "★ E2T4 本回合死的友军（owner=1）⇒ 「自上一回合以来」= **1**");
        Check(RuleCore.CountForTest(ctx, 0, null, since), 0,
              "★ E2T4 **侧位**：同一批死者对 owner=0 不算（`own|troop`）"
            + "｜🧨 忽略 `own` / 把 `your` 判成对方 ⇒ 实得 1 ⇒ 红");

        PassTurn(ctx);   // → 第 2 回合
        Check(ctx.Turn, 2, "E2T4 前提：现在第 2 回合");
        Check(RuleCore.CountForTest(ctx, 1, null, since), 1,
              "★★ E2T4（灭自证）过一个回合 ⇒ 「自上一回合以来」**仍数得到**（`Turn - 1` 那格）"
            + "｜🧨 把它并成 `ctx.DiedThisTurn`（= 改前的实现）⇒ 实得 0 ⇒ 红");
        Check(RuleCore.CountForTest(ctx, 1, null, thisTurn), 0,
              "E2T4 （对照）`died`（本回合）= **0** —— 两个 scope **必须分开**"
            + "｜🧨 把两条 scope 合并 ⇒ 上面那条与这条一起变 ⇒ 至少一条红");

        PassTurn(ctx);   // → 第 3 回合
        Check(ctx.Turn, 3, "E2T4 前提：现在第 3 回合");
        Check(RuleCore.CountForTest(ctx, 1, null, since), 0,
              "★ E2T4 又过一个回合 ⇒ 窗口关上（**0**）"
            + "｜🧨 窗口写成「本局累计」⇒ 实得 1 ⇒ 红");
    }
}
