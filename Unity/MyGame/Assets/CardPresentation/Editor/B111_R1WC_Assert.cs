// B111_R1WC_Assert.cs — 执行代理 `B111_R1WC`（第 111 会话第 1 波 · 引擎侧三条小账 `B-87`/`B-108`/`B-109`）
//
// ============================ 它钉的是什么 ============================
//  **`B-87`**：`BattleAutoDrive` 的两条兼容调用点（`CardPresentation/Core/BattleAutoDrive.cs:272` 的
//  `_drv.SimulatePlay` 与 `:374` 的拖拽）**不走 `SimpleAI.ExecuteAction`**，而是直调 `RuleCore.PlayCard`。
//  `B111_E2` 当时记「这两条仍不掷」；`B111_E1B` 把掷骰挪进引擎（`RuleCore.DecideOathChoice` 里的
//  `RollOathPay`，**无条件掷**）之后，只要 `PlayCard` 的战术支经过 `PlayTacticCore`，这两条就**已经被覆盖**。
//
//  本件的判别式 = 「**从 `RuleCore.PlayCard` 进**，出牌一次 ⇒ 预烧随机表**正好消耗 1 格**，
//  且引擎**记下了这一次的誓约结论**」。
//  ⚠️ 这两条只有**真的从 `RuleCore.PlayCard` 进**才有判别力 —— 直接调 `DecideOathChoice` 是 `B111_E1B_Assert`
//  已经断过的（那一件断的是引擎那个点本身，**没断**「兼容调用点能不能走到它」）。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  · **B2**：把 `RuleCore.PlayCard` 的战术支改成不走 `PlayTactic`（或把 `PlayTacticCore` 里那句
//      `DecideOathChoice` 挪走/**改成只在有人答时才掷**）⇒ `RandomNumbersIndex` 增量变 0 ⇒ 红。
//  · **B3**：同上 ⇒ `TryPeekLastOathDecision` 拿不到 ⇒ 红。
//  · B2 与 B3 是**两个不同口**（随机表 vs 引擎记账）⇒ 只改回一侧不会一起变绿。
//
// ============================ B-108（2026-11-01 已落地）============================
//  `B-108`（引擎**拒绝**的那一手也会烧 1 格）**本批已修**：`EffectResolver.PlayTacticCore` 改成
//  「先 `CanPlayTactic(…, oathChosen:false)` 轻判过闸 → `DecideOathChoice`（掷骰）→ 再 `CanPlayTactic(…, oathChosen)` 复判」，
//  判据 = `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c`：`CanPlayCard` **:213** → `PayCardCost` **:377**
//  → `ChooseCardMethod` **:607**（`GetChoiceOptions` 只在那里面被调）。
//  ⇒ 本件 **C 段**钉它：**被拒的那一手 `RandomNumbersIndex` 前后不变**。
//  🔴 **灭自证**：把 `PlayTacticCore` 那两句调回「`DecideOathChoice` 在 `CanPlayTactic` 之前」⇒ C2 从 0 变 1 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  本件**不接线**（与 `B111_E1B_Assert` / `B106_W7_Assert` 同档）。建议宿主 = `Editor/BattleScene.cs` 的方法尾聚合行，
//  **同一物理行、第一个 `//` 之前**追加：
//      B111R1WC_Assert.Run(); <那两个计数变量> += B111R1WC_Assert.Pass + B111R1WC_Assert.Fail;
//
// ============================ 报告 ============================
//  `资料/完全复刻/报告/B111_R1WC.md`
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

/// <summary>执行代理 `B111_R1WC`：钉 `B-87` —— **兼容调用点（直调 `RuleCore.PlayCard`）也走引擎那一个掷骰点**。</summary>
public static class B111R1WC_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111R1WC] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具（与 `B111_E1B_Assert` 同形）
    static List<CardDef> _pool;

    static List<CardDef> Pool()
    {
        if (_pool != null && _pool.Count > 0) return _pool;
        try { _pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B111R1WC] `CardDatabase.Load()` 抛了：" + e.Message); _pool = new List<CardDef>(); }
        return _pool;
    }

    static CardDef ById(string id)
    {
        foreach (var c in Pool()) if (c != null && string.Equals(c.Id, id, StringComparison.Ordinal)) return c;
        return null;
    }

    static BattleContext NewCtx(long seed, int energy, int active = 0)
    {
        var ctx = new BattleContext(unchecked((int)seed));
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        ctx.Active = active;
        ctx.Players[0].Energy = energy;
        ctx.Players[1].Energy = energy;
        return ctx;
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B111_R1WC · B-87：兼容调用点（直调 `RuleCore.PlayCard`）也走引擎那一个掷骰点");
        A_Coverage();
        MenuCheck.Section(_sink, "🆕 B111_R1WC · B-108：**被引擎拒掉的那一手不烧随机格**（原版闸在前、掷骰在后）");
        C_RejectedDoesNotRoll();
    }

    // ==================================================================
    //  A：从 `RuleCore.PlayCard` 进（= `BattleAutoDrive.SimulatePlay` 那条路）
    // ==================================================================
    static void A_Coverage()
    {
        var um109 = ById("UM109");
        if (Pool().Count == 0 || um109 == null)
        {
            Tr(false, "A0（前提）`CardDatabase.Load()` 拿到卡池、且池内有 `UM109 Adaptive Strategy`"
                    + "（誓约卡 · 战术 · cost 1 · `Oath 1:`）—— 拿不到 ⇒ 本件下面的断言全部无从谈起");
            return;
        }

        var inst = default(CardInstance);
        BattleContext ctx = null;
        int code = -999;
        Exception boom = null;
        try
        {
            ctx = NewCtx(0xB111CA1, 10);                // 能量 10 ⇒ 卡费 1 + 誓约 1 都付得起
            inst = ctx.NewInstance(um109);
            ctx.Players[0].Hand.Add(inst);
            ctx.RandomNumbers[0] = 4;                   // 固定答案（偶数位 ⇒ 与原版 `GetRandomInt(0,2)` 同池）
            ctx.RandomNumbersIndex = 0;
            code = RuleCore.PlayCard(ctx, 0, 0, -1);     // ★ 就是 `BattleAutoDrive.SimulatePlay` 那句（`BattleDriver.SimulatePlay` → `LocalAct(… () => RuleCore.PlayCard(Ctx, _me, idx, slot))`）
        }
        catch (Exception e) { boom = e; }

        Tr(boom == null, "A1（前提）从 `RuleCore.PlayCard` 打出 `UM109` **没抛**"
                       + (boom != null ? "（实抛：" + boom.GetType().Name + " " + boom.Message + "）" : ""));
        if (boom != null) return;

        Tr(code == RuleCodes.OK, "A2（前提）这手**打出去了**（码 " + code + "）—— ⛔ 不是 OK 的话下面两条都测不到点子上");
        C(ctx.RandomNumbersIndex, 1,
          "A3 ★ B-87：**从 `RuleCore.PlayCard` 进**也消耗**正好 1 格**预烧随机"
        + "（掷骰在引擎 `DecideOathChoice`⇒`RollOathPay`；把那条路改回「兼容点不掷」⇒ 这里变 0 ⇒ 红）");

        bool pay;
        Tr(ctx.TryPeekLastOathDecision(0, out pay),
           "A4 ★ B-87（第二个口）：引擎**记下了**这一次的誓约结论 —— 与 A3 是两个独立读数"
         + "（随机表 vs 引擎记账），只改回一侧不会一起变绿");
    }

    // ==================================================================
    //  C：`B-108` —— **被引擎拒掉的那一手不烧随机格**
    //     选一张「需要目标」的誓约战术卡，故意**不给目标** ⇒ 必被 `CanPlayTactic` 拒（`ErrSlot`）；
    //     能量给足（卡费 + 誓约刚好付得起）⇒ **旧代码会在拒之前先掷**（`DecideOathChoice` 无条件 `RollOathPay`）。
    // ==================================================================
    static void C_RejectedDoesNotRoll()
    {
        var um111 = ById("UM111");   // 誓约战术 · cost 2 · `Oath 8:` · **要给一个友方 troop 目标**
        if (um111 == null || !RuleCore.OathChoiceNeeded(um111))
        {
            Tr(false, "C0（前提）池里有 `UM111`（誓约 8 · cost 2 · **需要目标**）—— 拿不到 ⇒ C1/C2/C3 无从谈起");
            return;
        }

        BattleContext ctx = null; int code = -999; Exception boom = null;
        try
        {
            ctx = NewCtx(0xB111CA2, 10);              // 能量 10 ⇒ 卡费 2 + 誓约 8 **刚好付得起**（付不起的话旧代码本来就不掷，这条就测不到）
            var inst = ctx.NewInstance(um111);
            ctx.Players[0].Hand.Add(inst);
            ctx.RandomNumbers[0] = 4;
            ctx.RandomNumbersIndex = 0;
            code = RuleCore.PlayCard(ctx, 0, 0, -1);  // `-1` = **不给目标** ⇒ 需要选目标的卡必被 `CanPlayTactic` 拒
        }
        catch (Exception e) { boom = e; }

        Tr(boom == null, "C1（前提）这手**没抛**" + (boom != null ? "（实抛：" + boom.GetType().Name + " " + boom.Message + "）" : ""));
        if (boom != null) return;

        Tr(code != RuleCodes.OK, "C2（前提）这手**确实被引擎拒了**（码 " + code + "；⛔ 不是 OK 的话下面一条测不到点子上）");
        C(ctx.RandomNumbersIndex, 0,
          "C3 ★ B-108：**被拒的那一手不烧预烧随机格**（原版闸在前 `_d__447:213`、掷骰在后 `:607`；"
        + "把 `PlayTacticCore` 调回「`DecideOathChoice` 在 `CanPlayTactic` 之前」⇒ 这里变 1 ⇒ 红）");
    }
}
