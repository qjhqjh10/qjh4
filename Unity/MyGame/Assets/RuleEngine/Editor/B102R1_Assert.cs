// B102R1_Assert.cs — 执行代理 `B102_R1`（2026-10-30 · 第一百零三会话 · 战斗线 · 文件所有权 = `RuleEngine/Core/RuleCore.cs`）
//
// ============================ 它盯的是什么 ============================
//  账 `战斗逻辑驱动.tsv:867`（`CardScript.ResolveActiveAbilityPlayed`）的**最后一条真残差 R1**：
//  主动能力的「**装填**」那一步原来落在 **`RuleCore.UseAbilityCore`**（旧入口），
//  而那个入口只服务 `Card.Ability`（`Ability:` 文本）—— `CardDef.cs:1423` 的 `if (!FromOriginalPool)`
//  ⇒ **原版池 1,129 张的 `Card.Ability` 恒 null** ⇒ `CanStartAbility` 直接回 `ErrNoAbility`
//  ⇒ **装填机制对原版池 0 可达**（等于没接）。
//  本批把它挪到真对应物 **`RuleCore.UseActiveAbilityCore`**（`RuleCore.cs:15505` 那条物理行的行末，**零行数变化**）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CardScript__ResolveActiveAbilityPlayed.c`
//       `:23-28` 入口闸（`+0x220 == 0 ⇒ LogWarning + return`）· `:29-30` `+0x50 += 1` / `+0x4c = 1`
//       · `:31-43` 带 duty/ferocity/oath 重挑攻击型 · `:53-54` `DoesAbilityRequireLoading()` 分叉
//       · `:124-128` **只装填**支（置 `+0x224 = 1`、整条 return、**不调** `RawCardScript.ResolveActiveAbility`）
//   · `d:/2/tools/decomp_full/CardScript__DoesAbilityRequireLoading.c:13-38`（三态合取 → `UnitState.RequiresAbilityLoading`）
//   · 施工单 → `资料/完全复刻/判据/B102_R1_战斗B类三条.md` §③（含 C1–C5 与「为什么是这个位置」三条）
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/RuleEngineTest.cs` 的 `Run()`，**加这一行**：
//      B102R1_Assert.Run(); _pass += B102R1_Assert.Pass; _fail += B102R1_Assert.Fail;
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **A2 / A3 / A4**：把 `RuleCore.cs:15505` 行末那句 `if (u.StepActiveAbilityLoading()) { … return RuleCodes.OK; }`
//     删掉 ⇒ 第一次 `UseActiveAbility` 就**真结算**（执行器跑完把 `CardChosenInSelection` 清空、发
//     `EvtKind.Ability`、`ActiveAbilityLoaded` 仍是 false）⇒ 三条全红。
//   · **A1/A2 的「只接在旧入口」反证 = D1 + D5**：同一个夹具走 `UseAbility` 被 `ErrNoAbility` 挡下（D1），
//     换成 `UseActiveAbility` 才真的装填（D5）⇒ 若把新补的那句挪回 `UseAbilityCore`，A2/A3/A4 **全红**。
//   · **B4 / D-系**：把 `StepActiveAbilityLoading` 里「已装填 ⇒ 清 0 并返回 false」那一支删掉
//     ⇒ 第二次仍只装填、永不发动 ⇒ B4 红。
//   · **C2**：把装填判断写成**无条件**早退（不看 `RequiresAbilityLoading`）⇒ `needsLoading == 0` 的对照卡
//     也变成「打不动」⇒ C2/C4 红。
//
// ============================ 覆盖边界（如实记）============================
//   · 现池 `activeAbilityLogic` 的 `needsLoading` **全 0**（`cards_engine.json` 现读 133 行全 0）
//     ⇒ `RequiresAbilityLoading` 恒假 ⇒ 本件测的是**合成夹具**，真牌局**零行为变化 / 零指纹变化**。
//     （两者**同一条判据**：`UnitState.RequiresAbilityLoading` 读 `Card.ActiveAbilityNeedsLoading`。）
//   · 只验引擎态；原版那一支的 `CardUI.CleanStatusAnims / UpdateTraitIcons` 与装填动画属表现层，不在此。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B102_R1`：账 867 R1 —— 主动能力的「装填」从 `UseAbilityCore` 挪到 `UseActiveAbilityCore`。</summary>
public static class B102R1_Assert
{
    const string P = "[B102R1] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            SectionA_FirstUseOnlyLoads();
            SectionB_SecondUseReallyFires();
            SectionC_NoLoadingUnaffected();
            SectionD_OnlyThisEntryCanLoad();
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B102R1 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================ 夹具
    //  ⚠️ 走真实 `RuleCore.NewBattle`（`shuffle: false`），与 `RuleEngineTest` / `B98K2_Assert` 同口径。
    //     本文件是**独立**静态类 ⇒ 拿不到宿主那边 `private` 的夹具，自己建一份。

    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("R1Warlord0", "SpaceWolves") };
        var d1 = new List<CardDef> { Hero("R1Warlord1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>推进到「P1 的第 n 个回合」（与 `RuleEngineTest.ToP1Turn` / `B98K2_Assert.ToP1Turn` 同构）。</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Ok(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `{ Exhausted = false }` 必须写 —— ctor（`UnitState.cs:916`）恒 `Exhausted = true`，
        //    不钉住的话下面第一次 `UseActiveAbility` 会拿到 `ErrExhausted(6)`（同 `B100_F25` 那 56 处夹具）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>带一条 `activeAbilityLogic` 的夹具卡：`gate = 1` / `ta = 0 (any)` / `ca = 360`。
    /// <para>`ta = 0`（**不是** 30/220）⇒ `ActiveAbilityNeedsPick` 回 false ⇒ 不用先点目标。
    /// `ca = 360` 的好处 = **执行器会留下可观测的痕**（跑完清空 `CardChosenInSelection`）⇒
    /// 「正文到底跑没跑」能直接断，⛔ 不用猜。</para>
    /// <para>`needsLoading` 走 `CardDef` 的 `activeNeedsLoading` 形参 —— 那正是
    /// `UnitState.RequiresAbilityLoading` 读的那一格（`Card.ActiveAbilityNeedsLoading`）。</para></summary>
    static CardDef ActiveCard(string id, int needsLoading)
    {
        var row = new CardDef.ActiveAbilityLogicSpec
        {
            Ability = CardDef.ActiveAbilityObject,
            Gate = 1,
            Trigger = 400,
            NeedsLoading = needsLoading,
            TargetsAffected = new[] { 0 },
            FurtherMultiplier = new[] { 0 },
            ChosenAbility = new[] { 360 },
            PlayersAffected = new[] { 0 },
        };
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0,
                           hasActiveAbility: 1,
                           activeNeedsLoading: needsLoading,
                           activeAbilityLogic: new[] { row },
                           activeAbilityLogicVersioned: true);
    }

    /// <summary>`ca = 360` 选中的那张：卡面写法 `Rally:` = `ThisCardPlayed(0)`（`ActionPump` 的取法）。</summary>
    static CardDef Chosen()
    {
        return new CardDef("R1Chosen", "R1Chosen", "unit", "Rally: Gain +1 Attack", null, "Test", 1, 1, 5, 0);
    }

    static bool HasAbilitySignal(BattleContext ctx)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Ability) return true;
        }
        return false;
    }

    /// <summary>夹具：P1 第 3 回合，本方 3 号格放施法者；`CardChosenInSelection` 由调用方设。</summary>
    static BattleContext Duel(CardDef caster, out UnitState u)
    {
        var ctx = NewCtx();
        ToP1Turn(ctx, 3);
        u = Place(ctx, 0, 3, caster);
        ctx.ClearSignals();
        return ctx;
    }

    // ============================================================ A：第一次打出 = 只装填（C0 / C1 / C5）
    static void SectionA_FirstUseOnlyLoads()
    {
        Debug.Log(P + "--- A · 第一次打出 ⇒ **只装填、不发动**（`ResolveActiveAbilityPlayed.c:124-128`）---");

        UnitState u;
        var ctx = Duel(ActiveCard("R1Loader", 1), out u);
        ctx.CardChosenInSelection = Chosen();

        // 前提（⛔ 一律硬断言；不许 `if (前提) Check(结论)`）
        Eq(u.Card.HasActiveAbility, 1, "A0 **前提**：夹具卡 `hasActiveAbility == 1`（原版 `+0x220`）");
        Ok(u.RequiresAbilityLoading,
           "A0′ **前提**：`needsLoading == 1` ∧ 没被 `reload` 摘 ⇒ 需要装填（`DoesAbilityRequireLoading.c:13-38`）");
        Ok(!u.ActiveAbilityLoaded, "A0″ **前提**：装填位初值 = 0（`SetBaseCardTraits.c:38` 那一写点）");
        Ok(!u.HasAbility,
           "A0‴ **前提**：夹具卡 `Card.Ability == null` —— **这就是原版池的形状**"
         + "（`CardDef.cs:1423` 的 `!FromOriginalPool` ⇒ `Ability:` 那一档对原版池恒 null）");
        Ok(!HasAbilitySignal(ctx), "A0⁗ **前提**：开打前 `Signals` 里没有 `EvtKind.Ability`");
        Ok(!u.Exhausted, "A0⁵ **前提**：发动之前该单位**没有** `Exhausted`");
        Ok(ctx.CardChosenInSelection != null, "A0⁶ **前提**：选中位非空（`ca = 360` 那一支要它；正文跑完会被清）");

        int rc = RuleCore.UseActiveAbility(ctx, 0, 3);
        Eq(rc, RuleCodes.OK, "A1 第一次 `UseActiveAbility` ⇒ `OK`（装填也是**成功的一次行动**，原版那一支不报错）");
        Ok(u.ActiveAbilityLoaded,
           "A2 ★★ **只装填**：`ActiveAbilityLoaded` 变真（原版 `+0x224 = 1`，`.c:128`）"
         + "｜🧨 删掉 `RuleCore.cs:15505` 行末那句 `if (u.StepActiveAbilityLoading()) { … return OK; }` ⇒ 这里 false ⇒ 红");
        Ok(ctx.CardChosenInSelection != null,
           "A3 ★★ 主动能力正文**没有跑**（`ca = 360` 的执行器没被执行 ⇒ 选中位没被清空，原版 `.c:124-128` 整条 return）"
         + "｜🧨 同 A2：装填早退没接 ⇒ 执行器会把这一格清成 null ⇒ 红");
        Ok(!HasAbilitySignal(ctx),
           "A4 ★ 一条 `EvtKind.Ability` 都不发（原版「只装填」那一支**不宣布发动**；我们的宣布口在结算前一句）"
         + "｜🧨 同 A2 ⇒ 会发出 `Ability` ⇒ 红");
        Ok(u.Exhausted, "A5 行动照旧花掉（原版 `+0x50` / `+0x4C` 在装填判断**之前**，`.c:29-30`）");
        Eq(u.AttacksThisTurn, 1, "A5′ 本回合已行动计数 = 1（`+0x50 += 1` 那一格）");
        Ok(u.UsedActiveAbilityThisTurn && u.ActiveAbilityUsed,
           "A5″ 两格「用过了」都置真（`+0x50` 本回合 / `+0x4C` 本局 —— 原版这两句也在装填判断之前）");
    }

    // ============================================================ B：第二次打出 = 真发动（C2）
    static void SectionB_SecondUseReallyFires()
    {
        Debug.Log(P + "--- B · 第二次打出 ⇒ **真发动**，发动后装填位清 0（`.c:118-122`）---");

        UnitState u;
        var ctx = Duel(ActiveCard("R1Loader2", 1), out u);
        ctx.CardChosenInSelection = Chosen();

        Eq(RuleCore.UseActiveAbility(ctx, 0, 3), RuleCodes.OK, "B0 **前提**：第一次打出（只装填）成功");
        Ok(u.ActiveAbilityLoaded, "B0′ **前提**：装填位真了");
        Ok(ctx.CardChosenInSelection != null, "B0″ **前提**：正文还没跑（选中位还在）");
        ctx.ClearSignals();

        u.RefreshForNewTurn();   // 下个回合开始：解疲劳 / 清本回合攻击配额（`CardScript__OnTurnStart`）
        Ok(u.ActiveAbilityLoaded,
           "B1 装填位**跨回合保留**（原版只在 `ResetToValuesInHand` / `SetBaseCardTraits` 清它，回合开始不清）"
         + "｜🧨 若 `RefreshForNewTurn` 顺手清了它 ⇒「装填」永远无效 ⇒ 红");

        Eq(RuleCore.UseActiveAbility(ctx, 0, 3), RuleCodes.OK, "B2 ★ 第二次打出 ⇒ `OK`");
        Ok(!u.ActiveAbilityLoaded, "B3 ★ 发动后装填位清 0（`+0x224 = 0`，`.c:122`）");
        Ok(ctx.CardChosenInSelection == null,
           "B4 ★★ 这一次**真结算**（`ca = 360` 执行器跑完、把选中位清空 —— 原版 `ResolveActiveAbility` 那一段真被调了）"
         + "｜🧨 `StepActiveAbilityLoading` 少「已装填 ⇒ 清 0 并返回 false」那一支 ⇒ 永远只装填 ⇒ 仍非 null ⇒ 红");
        Ok(HasAbilitySignal(ctx), "B5 ★ 这一次发了 `EvtKind.Ability`（宣布口在结算那里）");
    }

    // ============================================================ C：不需要装填的卡一步到位（C4 · 灭自证「别把闸做宽」）
    static void SectionC_NoLoadingUnaffected()
    {
        Debug.Log(P + "--- C · `needsLoading == 0` ⇒ 第一次打出就真发动（闸不作宽）---");

        UnitState u;
        var ctx = Duel(ActiveCard("R1Plain", 0), out u);
        ctx.CardChosenInSelection = Chosen();

        Ok(!u.RequiresAbilityLoading, "C0 **前提**：`needsLoading == 0` ⇒ 不需要装填（同一份判据 `UnitState.RequiresAbilityLoading`）");
        Ok(ctx.CardChosenInSelection != null, "C0′ **前提**：选中位非空");

        Eq(RuleCore.UseActiveAbility(ctx, 0, 3), RuleCodes.OK, "C1 第一次打出 ⇒ `OK`");
        Ok(ctx.CardChosenInSelection == null,
           "C2 ★★ 一步到位（正文真跑了），**没有**「第一次只装填」这一跳"
         + "｜🧨 把装填判断写成无条件的早退（不看 `RequiresAbilityLoading`）⇒ 这里仍非 null ⇒ 红");
        Ok(!u.ActiveAbilityLoaded, "C3 装填位始终没被置过（对这一档不置位）");
        Ok(HasAbilitySignal(ctx), "C4 ★ 直接发 `EvtKind.Ability`（这一档没有「只装填」那一支）");
    }

    // ============================================================ D：C3 —— 「只接旧入口」的反证
    static void SectionD_OnlyThisEntryCanLoad()
    {
        Debug.Log(P + "--- D · C3 灭自证：装填**只可能**从 `UseActiveAbilityCore` 走到（旧入口对这形卡不可达）---");

        UnitState u;
        var ctx = Duel(ActiveCard("R1Entry", 1), out u);

        Ok(!u.HasAbility,
           "D0 **前提**：`Card.Ability == null`（`Ability:` 文本那条自设计路对这张卡**根本没有内容**）");
        Eq(RuleCore.UseAbility(ctx, 0, 3), RuleCodes.ErrNoAbility,
           "D1 ★★ **灭自证**：走**旧入口** `UseAbility` ⇒ `ErrNoAbility`（`RuleCore.CanStartAbility` 的 `!u.HasAbility` 那一档）"
         + "｜🧨 这一条把「RuleCore.cs:11665 那句对原版池形状永不可达」变成**可执行的事实**："
         + "只接在旧入口时，本件 A2/A3/A4 一条都跑不过");
        Ok(!u.ActiveAbilityLoaded, "D2 旧入口被拒 ⇒ 装填位没被碰过");
        Ok(!u.Exhausted, "D3 旧入口被拒 ⇒ 行动也没花（`CanStartAbility` 在动作经济之前返回）");

        ctx.CardChosenInSelection = Chosen();
        Eq(RuleCore.UseActiveAbility(ctx, 0, 3), RuleCodes.OK, "D4 同一张卡、同一回合，改走 `UseActiveAbility` ⇒ `OK`");
        Ok(u.ActiveAbilityLoaded,
           "D5 ★★ 装填在**新入口**真的发生了 —— 同一个夹具**只有换入口才生效**（这正是 R1 的判别力）"
         + "｜🧨 把那句挪回 `UseAbilityCore` ⇒ 这里 false ⇒ 红");
    }
}
