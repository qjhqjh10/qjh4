// B98K2_Assert.cs — 第九十八会话 · 波 8 · 执行代理 `B98_K2`（战斗线 · RuleCore 面）
//
// ============================ 它盯的是什么 ============================
//  账 **706 ①**（`CardScript.CardSetup` 的第 ③ 块「主动能力装填」）的**产品接线**：
//   `UnitState.StepActiveAbilityLoading` / `RequiresAbilityLoading` 早就建好（`B97_W22`），
//   但全仓**只有断言在调**（`B97W22_Assert.cs`）⇒ 原版那条「第一次打出只装填、不发动」的路**走不到**。
//  本批把它接进 `RuleCore.UseAbilityCore`（`RuleCore.cs` 的 `u.UsedActiveAbilityThisTurn = …` 那一行末尾，
//  行数零变化）—— 位置 = 原版 `CardScript__ResolveActiveAbilityPlayed.c:54-128`：
//    `+0x50 += 1`（我们的 `AttacksThisTurn`）与 `+0x4C = 1`（我们的 `UsedActiveAbilityThisTurn` / `ActiveAbilityUsed`）
//    在它**之前**（`:29-30`）⇒ **只装填也算花掉这次行动**，然后整条 return、**不调** `RawCardScript.ResolveActiveAbility`。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CardScript__ResolveActiveAbilityPlayed.c:54-128`（装载那一支）
//   · `d:/2/tools/decomp_full/CardScript__DoesAbilityRequireLoading.c:13-38`（三态合取，见 `UnitState.RequiresAbilityLoading` 的 doc）
//   · 施工判据 → `资料/完全复刻/判据/B98_J0_战斗剩余终局.md` §一「706」
//   · 本件报告 → `资料/完全复刻/报告/B98_K2.md`
//
// ============================ 🔴 覆盖边界（如实记）============================
//   · 今天**全池 `needsLoading` 全 0**（`cards_engine.json` / `cards_truth.json` 1,351 张，见 `B97W22_Assert.cs` A0 段）
//     ⇒ `RequiresAbilityLoading` 恒假 ⇒ 本件测的是**合成夹具**上的行为，真牌局**零行为变化、零指纹变化**。
//   · 只接了 `UseAbilityCore`（`Ability:` 那一族入口）。同族的另外三个入口
//     （`UseAlternativeCore` / `UseOathAbility` / `UseActiveAbilityCore`）原版也走同一个
//     `ResolveActiveAbilityPlayed`，但它们的「动作经济」置位时机各不相同（誓约是**付费成功之后**才置
//     `+0x50/+0x4C`）⇒ 装填早退要不要在此之前，得逐入口定，⛔ 本笔没动（见报告「还差什么」）。
//   · 账 706 ③（FTUE 低血无敌的**读点**，`ApplyDamageCore` 掉血之后）同样**产品恒不可达** ——
//     `ShouldBecomeInvulnerableWithLowLife` 的值来自远端 FTUE 配置，本仓所有生产调用点传 `false`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/RuleEngineTest.cs` 的 `Run()` 里加：`B98K2_Assert.Run(); _pass += B98K2_Assert.Pass; _fail += B98K2_Assert.Fail;`
//  （宿主由调度台代接线；本文件**只碰纯引擎**，不建 GameObject。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **A2/A3/A4**：把 `RuleCore.cs` 那句 `if (u.StepActiveAbilityLoading()) { … return RuleCodes.OK; }` 删掉
//     ⇒ 第一次 `UseAbility` 就**真结算**（目标 6→3、发 `EvtKind.Ability`、`ActiveAbilityLoaded` 仍 false）⇒ 三条全红。
//   · **B6**：把上面那句改成**无条件** `{ return RuleCodes.OK; }`（不看装填）⇒ `needsLoading == 0` 的对照卡
//     也变成「打不动」⇒ B6 红。
//   · **B2/B3**：把 `StepActiveAbilityLoading` 里「已装填 ⇒ 清 0 并返回 false」那一支删掉
//     ⇒ 第二次仍只装填、永不发动 ⇒ B2/B3 红。
//   · **D1**：把 `ApplyDamageCore` 掉血之后那句 `if (actual > 0 && u.ShouldBecomeInvulnerableWithLowLife && u.Health < 11) …`
//     删掉 ⇒ 血牛掉到 10 也不会拿到 `invulnerable` ⇒ D1 红；把 `< 11` 写成 `<= 11` ⇒ D2 红；
//     少掉 `ShouldBecomeInvulnerableWithLowLife` 那道闸 ⇒ D3 红。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B98_K2`：账 706 ① —— 主动能力「装填」机制接上产品路径（`RuleCore.UseAbilityCore`）。</summary>
public static class B98K2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[K2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_K2 账 706 ①：`StepActiveAbilityLoading` 接进 `UseAbilityCore`（第一次只装填）");

        SectionA_FirstUseOnlyLoads();
        SectionB_SecondUseReallyFires();
        SectionC_NoLoadingCardUnaffected();
        SectionD_FtueLowLifeInvulnerable();

        Debug.Log(_sink.P + "B98K2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    //  ⚠️ 走真实 `NewBattle` 路径（`shuffle: false`），与 `RuleEngineTest` 的夹具同口径；
    //    本文件是**独立**静态类，⛔ 别指望拿到那边 `private` 的 `Unit/Duel/Battle`（它们 `private`）。
    static CardDef Unit(string name, int costMelee, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", costMelee, atk, hp, 0, kws);
    }

    /// <summary>带主动能力正文 + 可控 `needsLoading` 的单位卡（`activeNeedsLoading` 是 `CardDef` 的构造形参）。</summary>
    static CardDef Loader(string name, int needsLoading)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 2, 2, 5, 0,
                           new[] { "Ability: Damage 3 EnemyUnit" },
                           hasActiveAbility: 1, activeNeedsLoading: needsLoading);
    }

    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("K2Warlord0", "SpaceWolves") };
        var d1 = new List<CardDef> { Hero("K2Warlord1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>推进到「P1 的第 n 个回合」（与 `RuleEngineTest.ToP1Turn` 同构）。</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Tr(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
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

    /// <summary>「主动能力 / 装填」那一局的夹具：P1 第 3 回合，本方 3 号格放施法者，敌方 3 号格放 6 血靶子。</summary>
    static BattleContext Duel(CardDef caster, out UnitState u, out UnitState prey)
    {
        var ctx = NewCtx();
        ToP1Turn(ctx, 3);
        u = Place(ctx, 0, 3, caster);
        prey = Place(ctx, 1, 3, Unit("K2Prey", 1, 1, 6));
        ctx.ClearSignals();
        return ctx;
    }

    // ============================================================ A：第一次打出 = 只装填
    static void SectionA_FirstUseOnlyLoads()
    {
        MenuCheck.Section(_sink, "A · 第一次打出 ⇒ **只装填、不发动**（`ResolveActiveAbilityPlayed.c:128`）");

        UnitState u, prey;
        var ctx = Duel(Loader("K2Loader", 1), out u, out prey);

        Tr(u.RequiresAbilityLoading,
           "A0 **前提断言**：`needsLoading == 1` ∧ `hasActiveAbility == 1` ⇒ 需要装填"
         + "｜🧨 前提不成立（`activeNeedsLoading` 形参没接上）⇒ 本段整段空转，必须先红");
        Tr(!u.ActiveAbilityLoaded, "A0′ **前提断言**：装填位初值 = 0（`SetBaseCardTraits.c:38` 那一写点）");
        C(prey.Health, 6, "A0″ **前提断言**：靶子满血 6（下面「没结算」才有判别力）");
        Tr(!HasAbilitySignal(ctx), "A0‴ **前提断言**：开打前 `Signals` 里没有 `EvtKind.Ability`");

        int rc = RuleCore.UseAbility(ctx, 0, 3, 3);
        C(rc, RuleCodes.OK, "A1 第一次打出 ⇒ `OK`（装填也是**成功的一次行动**，原版那一支不报错）");
        Tr(u.ActiveAbilityLoaded,
           "A2 ★★ 只装填：`ActiveAbilityLoaded` 变真（`+0x224 = 1`，`ResolveActiveAbilityPlayed.c:128`）"
         + "｜🧨 删掉 `RuleCore.cs` 那句 `if (u.StepActiveAbilityLoading()) { … return OK; }` ⇒ 这里是 false ⇒ 红");
        C(prey.Health, 6,
           "A3 ★★ 靶子**一滴血没掉**（`RawCardScript.ResolveActiveAbility` 整条没被调）"
         + "｜🧨 同上：装填早退没接 ⇒ 这里 6→3 ⇒ 红");
        Tr(!HasAbilitySignal(ctx),
           "A4 ★ 一条 `EvtKind.Ability` 都不发（原版那一支只播装填动画、不宣布能力发动；我们的宣布口在结算前一句）"
         + "｜🧨 同上 ⇒ 会发出 `Ability` ⇒ 红");
        Tr(u.Exhausted, "A5 行动照旧花掉（原版 `+0x50`/`+0x4C` 在装填判断**之前**，`:29-30`）");
        C(u.AttacksThisTurn, 1, "A5′ 本回合已行动计数 = 1（`+0x50 += 1` 那一格）");
        Tr(u.UsedActiveAbilityThisTurn && u.ActiveAbilityUsed,
           "A5″ 两格「用过了」都置真（`+0x50` / `+0x4C` —— 原版这两句在装填判断之前）");
    }

    // ============================================================ B：第二次打出 = 真发动
    static void SectionB_SecondUseReallyFires()
    {
        MenuCheck.Section(_sink, "B · 第二次打出 ⇒ **真发动**，打完之后装填位清 0（`.c:122`）");

        UnitState u, prey;
        var ctx = Duel(Loader("K2Loader2", 1), out u, out prey);

        C(RuleCore.UseAbility(ctx, 0, 3, 3), RuleCodes.OK, "B0 **前提**：第一次打出（只装填）成功");
        Tr(u.ActiveAbilityLoaded, "B0′ **前提**：装填位真了");
        C(prey.Health, 6, "B0″ **前提**：靶子还满血");
        ctx.ClearSignals();

        u.RefreshForNewTurn();   // 下个回合开始：解疲劳 / 清本回合攻击配额（`CardScript__OnTurnStart`）
        Tr(u.ActiveAbilityLoaded,
           "B1 装填位**跨回合保留**（原版只在 `ResetToValuesInHand` / `SetBaseCardTraits` 清它，回合开始不清）"
         + "｜🧨 若 `RefreshForNewTurn` 顺手清了它 ⇒ 「装填」永远无效 ⇒ 红");

        C(RuleCore.UseAbility(ctx, 0, 3, 3), RuleCodes.OK, "B2 ★ 第二次打出 ⇒ `OK`");
        C(prey.Health, 3,
           "B3 ★★ 这一次**真结算**（6 → 3，`Ability: Damage 3 EnemyUnit`）"
         + "｜🧨 `StepActiveAbilityLoading` 少「已装填 ⇒ 清 0 并返回 false」那一支 ⇒ 永远只装填 ⇒ 仍 6 ⇒ 红");
        Tr(!u.ActiveAbilityLoaded, "B4 ★ 发动后装填位清 0（`+0x224 = 0`，`.c:122`）");
        Tr(HasAbilitySignal(ctx), "B5 ★ 这一次发了 `EvtKind.Ability`（宣布口在结算那里）");
    }

    // ============================================================ C：不需要装填的卡一步到位（灭自证：别把闸做宽）
    static void SectionC_NoLoadingCardUnaffected()
    {
        MenuCheck.Section(_sink, "C · `needsLoading == 0` ⇒ 第一次打出就真发动（闸不许做宽）");

        UnitState u, prey;
        var ctx = Duel(Loader("K2Plain", 0), out u, out prey);

        Tr(!u.RequiresAbilityLoading, "C0 **前提断言**：`needsLoading == 0` ⇒ 不需要装填");
        C(RuleCore.UseAbility(ctx, 0, 3, 3), RuleCodes.OK, "C1 第一次打出 ⇒ `OK`");
        C(prey.Health, 3,
           "C2 ★★ 一步到位（6 → 3），**没有**「第一次只装填」这一跳"
         + "｜🧨 把 `if (u.StepActiveAbilityLoading())` 写成无条件的早退 ⇒ 这里仍是 6 ⇒ 红");
        Tr(!u.ActiveAbilityLoaded, "C3 装填位始终没被用过（对这一档不置位）");
    }

    // ============================================================ D：FTUE「低血无敌」的读点（账 706 ③）
    static void SectionD_FtueLowLifeInvulnerable()
    {
        MenuCheck.Section(_sink, "D · 账 706 ③：`+0x2e4` 那一格在**掉血之后**读一次（`ResolveDamageDealt.c:186`）");

        // D1：flag = 真 ∧ 掉血后 < 11 ⇒ 静默获得 invulnerable
        var ctx = NewCtx();
        ToP1Turn(ctx, 3);
        var tank = Place(ctx, 0, 3, Unit("K2Tank", 1, 0, 30));
        tank.ShouldBecomeInvulnerableWithLowLife = true;
        Tr(!tank.Has(ConditionEval.Invulnerable), "D0 **前提断言**：这条血牛一开始**没有** Invulnerable");
        RuleCore.HurtWithSourceForTest(ctx, tank, 20, "K2");
        C(tank.Health, 10, "D0′ **前提断言**：30 − 20 = 10（< 11，落进那一格）");
        Tr(tank.Has(ConditionEval.Invulnerable),
           "D1 ★★ 掉血后生命 < 11 ∧ `+0x2e4` 真 ⇒ 静默获得 `invulnerable`（`AddTraitSilently(0x136)`）"
         + "｜🧨 删掉 `RuleCore.cs:8968` 那句 ⇒ 这里 false ⇒ 红");

        // D2：阈值是 **< 11**，不是 <= 11
        var ctx2 = NewCtx();
        ToP1Turn(ctx2, 3);
        var tank2 = Place(ctx2, 0, 3, Unit("K2Tank2", 1, 0, 30));
        tank2.ShouldBecomeInvulnerableWithLowLife = true;
        RuleCore.HurtWithSourceForTest(ctx2, tank2, 19, "K2");
        C(tank2.Health, 11, "D2a **前提断言**：30 − 19 = **11**（正好在阈值上）");
        Tr(!tank2.Has(ConditionEval.Invulnerable),
           "D2 ★ 阈值是 `currentHealth < 0xb`（严格小于 11）⇒ 停在 11 **不给**"
         + "｜🧨 写成 `<= 11` ⇒ 红");

        // D3：flag = 假 ⇒ 掉到 2 也不给（本地恒 false 的那一格）
        var ctx3 = NewCtx();
        ToP1Turn(ctx3, 3);
        var tank3 = Place(ctx3, 0, 3, Unit("K2Tank3", 1, 0, 30));
        RuleCore.HurtWithSourceForTest(ctx3, tank3, 28, "K2");
        C(tank3.Health, 2, "D3a **前提断言**：30 − 28 = 2");
        Tr(!tank3.Has(ConditionEval.Invulnerable),
           "D3 ★ `ShouldBecomeInvulnerableWithLowLife == false` ⇒ 再低也不给（这一格的值来自远端 FTUE 配置）"
         + "｜🧨 少了那道 flag 闸 ⇒ 红");
    }
}
