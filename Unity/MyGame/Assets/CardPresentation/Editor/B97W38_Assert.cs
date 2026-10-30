// B97W38_Assert.cs — 第九十七会话 · 波 4-D · 执行代理 `B97_W38`
//
// ============================ 它盯的是什么 ============================
//  本件是「引擎小件」那一批里**唯一落代码**的一处，加上四条**复核 / 钉现状**的闸：
//   · **A**（账 L240）`BattleManager.CancelBattleActionFromQueue` 的 **`0x17 createHandCard` 副作用**
//     —— 原版撤这条动作时要对 `targetCard` 的 `gameObject` 调 `SetActive(false)`（`.c:44-52`）。
//     引擎里没有 `GameObject` ⇒ 本代理在 `ActionQueue` 加了一个表现层订阅口
//     `ActionQueue.OnCancelCreateHandCard`（缺省 `null` = 行为与加钩子之前逐字相同）。
//   · **B**（账 L241/L242）离场清扫那条**尚未落盘的补丁**（`RuleCore.CleanupDeaths` 残骸支：
//     `CancelEffectsFromDisappearingUnit(ctx, u, u, true)`）**实参形状**的唯一自检 —— 它必须是
//     `(u, u, true)` 而**不是** `(u, null, true)`（原版 `RemoveSelfBuffsFromQueues` 传 `(card, card, 1)`）。
//   · **C**（账 L1121）`BoardSlots.NextSlotPos` 的**核销复核**（引擎半＋接线已由 `B97_W11` 做完，本代理不重做，
//     只在**本宿主**里留三条能抓「被改回去」的闸；完整 30 条在 `B97W11_Assert.cs`）。
//   · **D**（账 L676）`selectFromChooseResolution` 的候选池收口（`B97_W5` 已落）＋ `BoardGateSpec.ConditionsFilter` 缺省值。
//   · **E**（账 L576）`SetUnitOrderInPlay` 的**值等价**：`BattleContext.TakePlayOrder` = 后置 `++`（首号 0）。
//
// 判据（⛔ 别在这里抄第二份）：
//   · `d:/2/tools/decomp_full/BattleManager__CancelBattleActionFromQueue.c:1-57`（`case 0x17` 那一支）
//   · `d:/2/tools/decomp_full/BattleManager__RemoveSelfBuffsFromQueues.c`（全文两行 ⇒ `(card, card, 1)`）
//   · `d:/2/tools/decomp_full/MinionManager__GetNextSlotPos.c:44-95` ＋ `:165-276`
//   · `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:1101-1140`（`selectFromChooseResolution` 在筛选之后、最外层）
//   · `d:/2/tools/decomp_full/BattleManager__SetUnitOrderInPlay.c:1-57`（取当前值写 `+0x44` → 再 `op_Increment`）
//   施工单 → `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·3 / §1·5；本件报告 → `资料/完全复刻/报告/B97_W38.md`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `Editor/RuleEngineTest.cs` 的 `Run()` 里加**一行块语句**（建议位置 = `{ CheckB97W11Gaps(); }` **之后**）：
//       // 🆕 2026-10-29（调度台代接线）：`B97_W38` —— `0x17 createHandCard` 订阅口 ＋ 残骸支补丁形状 ＋ 三条核销复核。
//       { CheckB97W38Gaps(); }
//   🔴 **故意不用 `Step(...)`、方法名不带 `Test`/`Report` 前缀** —— 与 `B97_W1` / `B97_W2` / `B97_W11` 同一条理由：
//     `Step(...)` 会让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿反射清单对账，而 `工具/gen_diag_runner.py`
//     只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **A2/A3**：把 `ApplyCancelSideEffects` 的 `case 0x17` 那行
//     `if (OnCancelCreateHandCard != null) { OnCancelCreateHandCard(act.Target); break; }` **删掉**
//     ⇒ 钩子一次都不被调 ⇒ **A2 红**（变 0）。把实参从 `act.Target` 改成 `act.Actor` ⇒ **A3 红**。
//   · **A4**：把那行 `if` 挪到 `_cancelSideEffectsPending++;` **之后** ⇒ **A4 红**（有订阅者还计数）。
//   · **A6**：把 `OnCancelCreateHandCard` 的缺省行为改成「有订阅才撤条目」⇒ **A6/A7 红**（无订阅者时条目也必须被撤）。
//   · **B1**：把补丁写成 `CancelEffectsFromDisappearingUnit(ctx, u, null, true)`（照 `ResolveJamUnit` 那两格）⇒
//     `targetCard == null` 时 ③ 不再筛 ⇒ 那条「Actor 相同但 Target 为空」的也会被撤 ⇒ **B1 变 2，红**。
//   · **C1**：`BoardSlots.NextSlotPos` 的循环上界换回 `Math.Max(CountOnSide(Right), CountOnSide(Left))`（人数）⇒ **C1 红**（+4）。
//   · **C2**：`required == 1` 那 block 退回「出声 + `int.MinValue`」⇒ **C2 红**。
//   · **D2/D4**：`CountEval.ApplySelectFromChooseResolution` 改成恒返回 `pool`（不收窄 / 不清空）⇒ **D2、D4 红**。
//   · **D5**：`CardDef.BoardGateSpec.ConditionsFilter` 缺省从 `0` 改成 `-1` ⇒ **D5 红**（`0` = 合法值「不筛」）。
//   · **E1**：`BattleContext.TakePlayOrder` 改成前置 `++`（`++_playOrderSeq`）⇒ **E1 红**（首号变 1）。
//
// ============================ 已知边界（⛔ 不是「已验收」）============================
//   · **A 段今天零可观测**：全仓 `actionType: 0x17` 的入队点 = 0（`grep` 只命中判据注释）⇒ 钩子接上也走不到；
//     本段靠**夹具手工入队**验证「接上了」。
//   · **B 段钉的是一条还没落盘的补丁的形状** —— 补丁本身在 `RuleCore.cs`（`B97_W34` 的文件）⇒
//     本段的断言**不依赖**补丁是否落地（它调的是 `ActionPump` 那个口，形状对了才 1 条）。
//   · **C 段只钉格号**，⛔ 不钉动画锚点的屏幕位置（那要真机，`B97_W11` 报告 §五·4 已如实记）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B97_W38：引擎小件（`0x17` 订阅口 / 残骸支补丁形状 / 三条核销复核）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W38Assert
{
    static readonly CheckSink _sink = new CheckSink("[W38] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W38 引擎小件（`0x17` 订阅口 · 残骸支补丁形状 · 核销复核）");

        SectionA_CreateHandCardHook();
        SectionB_RemnantPatchShape();
        SectionC_NextSlotPosReverify();
        SectionD_ChooseResolutionPool();
        SectionE_PlayOrderValue();

        Debug.Log(_sink.P + "B97W38 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static CardDef Troop(string name)
    {
        return new CardDef(name, name, "troop", "", null, "SpaceWolves", 1, 1, 3, 0, null, subtype: "troop");
    }

    static List<CardDef> DeckOf(string faction)
    {
        return new List<CardDef>
        {
            new CardDef("w38hero_" + faction, "W38Hero", "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord"),
        };
    }

    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    static PlayerState Board(int left, int right)
    {
        var ps = new PlayerState { Name = "B97W38" };
        for (int i = 0; i < left; i++)
            ps.Board[BoardSlots.SlotOf(BoardSlots.Left, i)] = new UnitState(Troop("W38L" + i), false);
        for (int i = 0; i < right; i++)
            ps.Board[BoardSlots.SlotOf(BoardSlots.Right, i)] = new UnitState(Troop("W38R" + i), false);
        return ps;
    }

    // ============================================================ A 账 L240：`0x17 createHandCard` 的表现层订阅口
    static void SectionA_CreateHandCardHook()
    {
        MenuCheck.Section(_sink, "A 账 L240：`0x17 createHandCard` ⇒ 原版 `targetCard.gameObject.SetActive(false)`（订阅口 = `ActionQueue.OnCancelCreateHandCard`）");

        // 归零（上一条断言跑挂了也不会把订阅者漏给本条 —— ⛔ 不是「断言缺省值是 null」，那条由代码读）
        ActionQueue.OnCancelCreateHandCard = null;

        var actor = new UnitState(Troop("W38Actor"), false);
        var target = new UnitState(Troop("W38Target"), false);

        // ---- A1..A5：**有订阅者** ⇒ 调它、不计数、条目照样被撤 --------------------
        var ctx = NewCtx();
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "w38-create-hand-card", () => { },
                                actor: actor, abilityTarget: target, actionType: 0x17);
        int before = ctx.ActionQueue.CountOf(ActionPriority.Instant);
        Tr(before >= 1, "A0 **前提断言**：Instant 道里至少有一条（我入队的那条；`NewBattle` 可能另有自动动作 ⇒ 只判下界）");
        int pending0 = ctx.ActionQueue.CancelSideEffectsPendingForTest;

        int hits = 0;
        UnitState seen = null;
        ActionQueue.OnCancelCreateHandCard = u => { hits++; seen = u; };
        try
        {
            bool ok = ctx.ActionQueue.RemoveQueuedActionAt(ActionPriority.Instant, before - 1);
            Tr(ok, "A1 **前提断言**：下标合法 ⇒ 真的撤掉了一条");
            C(hits, 1, "A2 ★ 有订阅者 ⇒ 钩子被调 **1** 次（🧨 删掉 `case 0x17` 那行 `if` ⇒ 变 0，红）");
            Tr(ReferenceEquals(seen, target),
               "A3 ★★ 钩子收到的是 **`act.Target`**（原版 `targetCard` `+0x10`），⛔ 不是 `Actor`（🧨 两格弄反 ⇒ 红）");
            C(ctx.ActionQueue.CancelSideEffectsPendingForTest, pending0,
              "A4 有订阅者 ⇒ **不计数**（🧨 把 `if` 挪到 `_cancelSideEffectsPending++` 之后 ⇒ 红）");
            C(ctx.ActionQueue.CountOf(ActionPriority.Instant), before - 1, "A5 条目照样被撤（副作用补完才 `RemoveAt`，但结果必须少一条）");
        }
        finally { ActionQueue.OnCancelCreateHandCard = null; }

        // ---- A6/A7：**没有订阅者** ⇒ 退回「待接点」（出声 + 计数），⛔ 但条目也必须被撤 ----
        var ctx2 = NewCtx();
        RuleCore.EnqueueAbility(ctx2, ActionPriority.Instant, "w38-create-hand-card-2", () => { },
                                actor: actor, abilityTarget: target, actionType: 0x17);
        int before2 = ctx2.ActionQueue.CountOf(ActionPriority.Instant);
        int pending1 = ctx2.ActionQueue.CancelSideEffectsPendingForTest;
        bool ok2 = ctx2.ActionQueue.RemoveQueuedActionAt(ActionPriority.Instant, before2 - 1);
        Tr(ok2, "A6 **前提断言**：无订阅者时条目仍被撤（⛔ 钩子是「补副作用」，不是「撤不撤」的闸）");
        C(ctx2.ActionQueue.CancelSideEffectsPendingForTest, pending1 + 1,
          "A7 无订阅者 ⇒ 走「待接点」⇒ `CancelSideEffectsPendingForTest` **+1**（⛔ 不静默）");

        // ---- A8：`attack(1)` 那一支**不**调这个钩子（两个副作用是两个 case）----
        var ctx3 = NewCtx();
        int hits3 = 0;
        ActionQueue.OnCancelCreateHandCard = u => { hits3++; };
        try
        {
            RuleCore.EnqueueAbility(ctx3, ActionPriority.Instant, "w38-attack", () => { },
                                    actor: actor, abilityTarget: target, actionType: 1);
            int before3 = ctx3.ActionQueue.CountOf(ActionPriority.Instant);
            ctx3.ActionQueue.RemoveQueuedActionAt(ActionPriority.Instant, before3 - 1);
            C(hits3, 0, "A8 `attack(1)` 被撤时 **不**调 `0x17` 的钩子（🧨 把 `case 1` 也接上它 ⇒ 红）");
        }
        finally { ActionQueue.OnCancelCreateHandCard = null; }
    }

    // ============================================================ B 账 L241/L242：残骸支补丁的实参形状
    static void SectionB_RemnantPatchShape()
    {
        MenuCheck.Section(_sink, "B 账 L241/L242：`CleanupDeaths` 残骸支补丁 `CancelEffectsFromDisappearingUnit(ctx, u, u, true)` 的实参形状");

        var ctx = NewCtx();
        var leaver = new UnitState(Troop("W38Leaver"), false);
        var other = new UnitState(Troop("W38Other"), false);

        // 三条：① 同一 actor、Target 为空（`ResolveJamUnit` 那一档的形状）
        //      ② 同一 actor、Target 就是自己（= `RemoveSelfBuffsFromQueues` 那一档）
        //      ③ 别人的 actor（⛔ 不该被撤）
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "w38-no-target", () => { },
                                actor: leaver, actionType: 0);
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "w38-self-target", () => { },
                                actor: leaver, abilityTarget: leaver, actionType: 0);
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "w38-other-actor", () => { },
                                actor: other, abilityTarget: other, actionType: 0);

        int before = ctx.ActionQueue.CountOf(ActionPriority.Instant);
        Tr(before >= 3, "B0 **前提断言**：Instant 道里至少三条（只判下界 —— `NewBattle` 可能另有自动动作）");

        int n = RuleCore.CancelEffectsFromDisappearingUnit(ctx, leaver, leaver, true);
        C(n, 1,
          "B1 ★ 补丁的实参形状 `(ctx, u, u, true)` ⇒ 只撤「`Actor == u` ∧ `Target == u`」那**一条**"
          + "（🧨 写成 `(ctx, u, null, true)` ⇒ 变 2，红 —— 原版 `RemoveSelfBuffsFromQueues.c` 传的是 `(card, card, 1)`）");
        C(ctx.ActionQueue.CountOf(ActionPriority.Instant), before - 1, "B2 只少了那一条（别人的 actor 一条都没动）");
    }

    // ============================================================ C 账 L1121：核销复核（⛔ 不是重做）
    static void SectionC_NextSlotPosReverify()
    {
        MenuCheck.Section(_sink, "C 账 L1121 核销复核：`BoardSlots.NextSlotPos` 上界 = 每侧格数 ＋ `required==1` 中性支（`B97_W11` 已修已接线）");

        C(BoardSpec.SlotsPerSide, 4, "C0 **前提断言**：每侧 4 格（原版 `left/rightSlotPos.Length`，13 场逐场 4）");

        var two = Board(2, 2);
        C(BoardSlots.NextSlotPos(two, 2), -3,
          "C1 ★ 右 2 / 左 2、`required = 2` ⇒ **左第 3 格（-3）**（🧨 循环上界换回「人数」⇒ 变 +4，红）");
        C(BoardSlots.NextSlotPos(two, 1), 3,
          "C2 右 2 / 左 2、`required = 1` ⇒ 中性支平手走右 ⇒ 右第 3 格（+3）（🧨 `required==1` 块退回「出声 + int.MinValue」⇒ 红）");

        var empty = Board(0, 0);
        C(BoardSlots.NextSlotPos(empty, 3), 2,
          "C3 空场、`required = 3` ⇒ 右第 2 格（+2）（🧨 上界改回人数 ⇒ 只枚举 i=0 ⇒ 变 -1，红）");
    }

    // ============================================================ D 账 L676：候选池收口 ＋ `ConditionsFilter` 缺省
    static void SectionD_ChooseResolutionPool()
    {
        MenuCheck.Section(_sink, "D 账 L676：`selectFromChooseResolution` 候选池收口（`B97_W5` 已落）＋ `BoardGateSpec.ConditionsFilter` 缺省 = 0");

        var ctx = NewCtx();
        var a = new UnitState(Troop("W38PoolA"), false);
        var b = new UnitState(Troop("W38PoolB"), false);
        Tr(!ReferenceEquals(a.Card.Id, b.Card.Id), "D0 **前提断言**：两张卡的 `Id` 不同（下面按 `Id` 比对）");

        ctx.CardChosenInSelection = b.Card;
        var keep = CountEval.ApplySelectFromChooseResolution(ctx, 0, new List<UnitState> { a, b });
        C(keep.Count, 2, "D1 `flag == 0` ⇒ 原样返回（原版那一支压根不执行）");
        Tr(ctx.CardChosenInSelection != null, "D1′ `flag == 0` ⇒ **不清** `CardChosenInSelection`");

        var narrowed = CountEval.ApplySelectFromChooseResolution(ctx, 1, new List<UnitState> { a, b });
        C(narrowed.Count, 1, "D2 ★ `flag != 0` ⇒ 收窄成选定的那一张（筛 → 截成 1）（🧨 收口改成恒返回 pool ⇒ 变 2，红）");
        Tr(narrowed.Count == 1 && ReferenceEquals(narrowed[0], b), "D3 留下的确实是**选中的那张**（`b`）");
        C(ctx.CardChosenInSelection, (CardDef)null, "D4 收窄后**清空** `CardChosenInSelection`（原版 `*plVar1 = 0`；🧨 不清 ⇒ 红）");

        C(new CardDef.BoardGateSpec().ConditionsFilter, 0,
          "D5 `BoardGateSpec.ConditionsFilter` 缺省 `0` = `UnitConditions.none`（**不筛，合法值**；`-1` = 未知）（🧨 缺省改成 `-1` ⇒ 红）");
    }

    // ============================================================ E 账 L576：进场序号的值等价
    static void SectionE_PlayOrderValue()
    {
        MenuCheck.Section(_sink, "E 账 L576 值等价：`BattleContext.TakePlayOrder` = 后置 `++`（原版 `SetUnitOrderInPlay.c:30-48`）");

        // 🔴 2026-10-30 `B100_F25`（#55–#57）：原来断「新局首号 = 0」是**过期期望** ——
        //    `RuleCore.NewBattle` 开局就给**两个督军各发过一次号**（`RuleCore.cs:663`
        //    `p.Warlord.PlayOrder = ctx.TakePlayOrder();`）⇒ 计数器已到 2。产品（后置 `++`）本身没错，
        //    错的是断言没算这段基线。改成「取基线再比增量」，判别力不变：前置 `++` 会让下面每一条都偏 1。
        var ctx = NewCtx();
        int base0 = ctx.TakePlayOrder();               // 新局基线（= 开局已发掉的那几号之后的值）
        int o0 = ctx.TakePlayOrder();
        int o1 = ctx.TakePlayOrder();
        C(o0, base0 + 1, "E1 ★ 后置 `++`：取到的号 = **基线 + 1**（原版 `+0x3a8`「先取当前值再自增」）"
                       + "｜🧨 改成前置 `++` ⇒ 实得 基线 + 2 ⇒ 红");
        C(o1, o0 + 1, "E2 第二次再 +1（后置 `++` 的单调走势）");

        var ctx2 = NewCtx();
        int base2 = ctx2.TakePlayOrder();
        C(base2, base0, "E3 新局重新从**同一基线**起（⛔ 不是静态计数器 —— 若是静态的，这里会是 `base0 + 2`）");
    }
}
