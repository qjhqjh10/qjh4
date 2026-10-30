// B103R5_Assert.cs — 只读查判据代理 `B103_R5`（第一百零三会话 · 波 1）转来的**规格断言**
//
// ============================ 它盯的是什么 ============================
//   ① 账 `战斗逻辑驱动:1383`（`BattleManager.DisplayEnemyCardInCenter`）——
//      **破坏卡中央展示在原版是【真阻塞】**，我们原来把它登记成「零时长」⇒ 那 1.078 s 没建模。
//   ② 账 `战斗逻辑驱动:707`（`CardScript.CardSetup` 的 ② 格）——
//      FTUE「低血无敌」的**读点**：原版读的是【掉血之前】的血，我们读的是【掉血之后】。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · 施工单 / 逐句判据 / 常量实读 → `资料/完全复刻/判据/B103_R5_波次2施工单.md`
//   · 原版：`decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:100-107`（调用点）
//           `decomp_full/CardScript._DisplayEnemyCardInCenter_d__263__MoveNext.c:25-119`（四段 state）
//           `out_cpp2il_dev_ilspy/CardScript.cs:1160-1355`（ILSpy · 带真形参名）
//   · 原版：`out_cpp2il_dev_ilspy/CardScript.cs:14374-14382` ＋ `out_il2csharp/Assembly-CSharp/CardScript.cs:7868-7892`
//           （FTUE 读点；🔴 **`decomp_full/CardScript__ResolveDamageDealt.c` 的 `if` 嵌套不可信**，见施工单附录 A）
//
// ============================ 🔴🔴 本文件的 F2 / F3 现在【是红的】，这是设计如此 ============================
//   `RuleEngine/Core/RuleCore.cs` 的 `ApplyDamageCore` 里那句 FTUE 读点还没改
//   （`RuleCore.cs:9010` 现读仍是 `… && u.Health < 11` ＋ `AddKeyword(…, 1)`），
//   而 `RuleCore.cs` 此刻有别的写手（`B103_W5`）。
//   ⇒ **F2（掉血前 12 挨 5 ⇒ 不许给）与 F3（`KwValue` 必须 = 0）在那一行改掉之前必然红**。
//   ⛔ 别为了让自检变绿去放宽这两条 —— 它们是**规格**；改 `RuleCore.cs:9010` 是配套的另一笔
//      （补丁见 `资料/完全复刻/报告/B103_R5.md`），落地后这两条自动转绿。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   E1 ← 把 `SabotageCreated` 同时塞回 `ZeroDurationKinds`（E1 与 E2 同时红）。
//   E2 ← 把 `CardFeel.CardCenterWaitMul` 改成 0（或把 `DurationOf` 的 `case` 写死 0）⇒ 1.078 变 0.298 ⇒ 红。
//   E3 ← 只往 `TimedKinds` 加名字、忘了加 `SourceOf` 的 `case` ⇒ `SourceOf` 会回「登记在 `TimedKinds` 里但
//        `switch` 里没有 `case`」那句病话 ⇒ 红（这条专治「登记了没落地」）。
//   E4 ← 在 `DurationOf(BattleEvent)` 里加一个「`SabotageCreated ⇒ 0`」的早退分支 ⇒ 转发不再等价 ⇒ 红。
//   E5 ← 把 `CardFeel.CardCenterBlockingSeconds` 方法体写成 `return 1.078f;`（常数冒充公式）
//        ⇒ E5 红的（它就打 `stepTime = 0.1` 那一档），而 E2 仍绿 ⇒ 这两条互不替代。
//   F1/F2 ← **互斥对**：任何「统一读掉血前」或「统一读掉血后」的写法都只能让其中一条绿 ⇒
//        结构上不可能同时满足（F2 就是那条把两种口径分开的用例）。
//   F3  ← 把 `AddKeyword(…, 0)` 写回 `(…, 1)` ⇒ `KwValue` 变 1 ⇒ 红。
//   F4  ← 把 `actual > 0` 那道闸删掉 ⇒ 0 伤也发无敌 ⇒ 红。
//   F5  ← 把 `ShouldBecomeInvulnerableWithLowLife` 那道闸删掉 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   纯引擎 + 纯静态表，**不建 GameObject** ⇒ 接在 `Editor/RuleEngineTest.cs` 的 `Run()` 里：
//     `B103R5_Assert.Run(); _pass += B103R5_Assert.Pass; _fail += B103R5_Assert.Fail;`
//   （宿主聚合行由调度台接；本文件⛔ 不改任何宿主。）
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>查判据代理 `B103_R5` 的规格断言：① 破坏卡中央展示的阻塞时长 · ② FTUE 低血无敌读点的口径。</summary>
public static class B103R5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103R5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B103R5 ① 账 1383：破坏卡中央展示在原版是**真阻塞**（1.078 s）");
        SectionE_SabotageBlocking();

        MenuCheck.Section(_sink, "B103R5 ② 账 707：FTUE 低血无敌读点 —— 读的是**掉血之前**的血");
        SectionF_FtueLowLifeInvulnerable();

        Debug.Log(_sink.P + "B103R5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ E：① 阻塞时长

    static void SectionE_SabotageBlocking()
    {
        // E0 前提：这三格是「本账成立」的前提，前提不成立后面全是噪声（⛔ 别用 if 包起来）
        Tr(EventTiming.IsRegisteredEventKind(EvtKind.SabotageCreated),
           "E0 **前提断言**：`EvtKind.SabotageCreated` 在登记表里（`ZeroDurationKinds` 或 `TimedKinds`）");

        // E1 它在 **TimedKinds**，不在 ZeroDurationKinds
        Tr(EventTiming.IsTimedKind(EvtKind.SabotageCreated)
           && !EventTiming.IsZeroDurationKind(EvtKind.SabotageCreated),
           "E1 ★ `SabotageCreated` 登记在 **`TimedKinds`**（它**有**时长），⛔ 不在 `ZeroDurationKinds`"
         + "｜🧨 把它同时塞回 `ZeroDurationKinds` ⇒ 本条与 E2 同时红");

        // E2 时长 = 1.078 s（**字面量**；⛔ 别拿 CardFeel 的算式来比 —— 那是自证）
        float dur = EventTiming.DurationOf(EvtKind.SabotageCreated);
        Tr(Mathf.Abs(dur - 1.078f) < 1e-4f,
           $"E2 ★★ `DurationOf(SabotageCreated)` = **1.078 s**（实得 {dur:F4}）"
         + " —— 原版 `_DisplayEnemyCardInCenter_d__263__MoveNext.c`：`0.1`（state0）+ `stepTime×0.66`"
         + " + `stepTime×2.6`，调用点 `stepTime = 0.3f` ⇒ 0.1 + 0.198 + 0.78"
         + "｜🧨 把 `CardFeel.CardCenterWaitMul` 改成 0（或 `case` 写死 0）⇒ 红");

        // E3 出处真的有 case，不是「登记了没落地」那句病话
        string src = EventTiming.SourceOf(EvtKind.SabotageCreated);
        Tr(src != null && src.IndexOf("1.078", System.StringComparison.Ordinal) >= 0
           && src.IndexOf("_d__263", System.StringComparison.Ordinal) >= 0
           && src.IndexOf("登记在", System.StringComparison.Ordinal) < 0,
           "E3 ★ `SourceOf(SabotageCreated)` 有真出处（含 `1.078` 与 `_d__263`），**不是**"
         + "「登记在 `TimedKinds` 里但 `switch` 里没有 `case`」那句病话"
         + "｜🧨 只往 `TimedKinds` 加名字、忘了加 `SourceOf` 的 `case` ⇒ 红");

        // E4 带事件的重载必须转发（生产路径走的是它）
        var e = new BattleEvent { Kind = EvtKind.SabotageCreated, Player = 0, Slot = -1, Amount = 1 };
        float durEvt = EventTiming.DurationOf(e);
        Tr(Mathf.Abs(durEvt - dur) < 1e-4f && Mathf.Abs(durEvt - 1.078f) < 1e-4f,
           $"E4 `DurationOf(BattleEvent)` 转发一致（事件档 {durEvt:F4} = EvtKind 档 {dur:F4} = 1.078）"
         + "｜🧨 在 `DurationOf(BattleEvent)` 里加一个「`SabotageCreated` ⇒ 0」的早退 ⇒ 红");

        // E5 灭自证·结构：它是 `stepTime` 的**真函数**，不是常数
        // 🔴 2026-10-30（调度台订正）：原来第二句写的是 `at03 ≈ 3 × at01` —— **算错了**：
        //   公式 = `0.1 + stepTime×(0.66+2.6)` **带常数项 0.1**，所以不成比例
        //   （3 × 0.426 = 1.278 ≠ 1.078）。改成断**差值**：`at03 - at01` 正好抵消常数项 = `(0.3-0.1)×3.26 = 0.652`。
        float at01 = CardFeel.CardCenterBlockingSeconds(0.1f);
        float at03 = CardFeel.CardCenterBlockingSeconds(0.3f);
        Tr(Mathf.Abs(at01 - 0.426f) < 1e-4f && Mathf.Abs((at03 - at01) - 0.652f) < 1e-4f,
           $"E5 ★ 灭自证：`CardFeel.CardCenterBlockingSeconds` 是 `stepTime` 的**真函数** —— "
         + $"传 0.1 ⇒ {at01:F4}（期望 0.1 + 0.1×0.66 + 0.1×2.6 = 0.426），传 0.3 ⇒ {at03:F4}，"
         + $"差值 = {at03 - at01:F4}（期望 0.2×3.26 = 0.652 —— 常数项 0.1 抵消，⛔ 不是 3 倍关系）"
         + "｜🧨 把方法体写成 `return 1.078f;` ⇒ 本条红（而 E2 仍绿 ⇒ 两条互不替代）");
    }

    // ============================================================ F：② FTUE 低血无敌读点
    //  ⚠️ F2 / F3 在 `RuleCore.cs:9010` 改成「掉血前的血 + value 0」之前**是红的**（文件头已写清）。
    //  ⚠️ 「幂等」那一条**在自然夹具里不可隔离**：`invulnerable` 会**完全挡下**后续伤害
    //     （`RuleCore.ApplyDamageCore` 的无敌支 `EmitHit(ctx,u,0); return 0;`）⇒ 第二击 `actual == 0`，
    //     被 `actual > 0` 挡掉，走不到 `AddTraitSilently` 那一步。所以 F3 只钉**值 = 0**（可隔离的那一半）。

    static void SectionF_FtueLowLifeInvulnerable()
    {
        // F0 前提：句柄与默认值
        Tr(ConditionEval.Invulnerable == "invulnerable",
           "F0 **前提断言**：关键词句柄 `ConditionEval.Invulnerable` = `\"invulnerable\"`（原版 trait 0x136 = 310）");

        // F1 掉血**前** 10、挨 1 ⇒ 给（原版 `currentHealth <= 10` 才给，此刻 10 够）
        {
            var ctx = NewCtx();
            ToP1Turn(ctx, 3);
            var tank = Place(ctx, 0, 3, Unit("B103R5_T1", 1, 0, 10));
            Tr(!tank.Has(ConditionEval.Invulnerable), "F1-a **前提断言**：这条血牛一开始没有 Invulnerable");
            tank.ShouldBecomeInvulnerableWithLowLife = true;
            RuleCore.HurtWithSourceForTest(ctx, tank, 1, "B103R5");
            C(tank.Health, 9, "F1-b **前提断言**：10 − 1 = 9");
            Tr(tank.Has(ConditionEval.Invulnerable),
               "F1 ★ 掉血**前**生命 10（≤ 10）∧ `+0x2e4` 真 ⇒ 静默获得 `invulnerable`"
             + "（`out_il2csharp/CardScript.cs:7887` 的 `num6 <= 10` ⇒ `AddTraitSilently(invulnerable, false, 0)`）"
             + "｜🧨 把 `ShouldBecomeInvulnerableWithLowLife` 那道闸删掉 ⇒ 变不了红，但 F5 会红");
        }

        // F2 ★★ 区分用例：掉血**前** 12、挨 5 ⇒ **不给**（旧写法「掉血后 7 < 11」会给）
        {
            var ctx = NewCtx();
            ToP1Turn(ctx, 3);
            var tank = Place(ctx, 0, 3, Unit("B103R5_T2", 1, 0, 12));
            Tr(!tank.Has(ConditionEval.Invulnerable), "F2-a **前提断言**：一开始没有 Invulnerable");
            tank.ShouldBecomeInvulnerableWithLowLife = true;
            RuleCore.HurtWithSourceForTest(ctx, tank, 5, "B103R5");
            C(tank.Health, 7, "F2-b **前提断言**：12 − 5 = 7（**掉血后** < 11，正是旧写法的错处）");
            Tr(!tank.Has(ConditionEval.Invulnerable),
               "F2 ★★ **原版读的是掉血【之前】的血**：12 > 10 ⇒ **不给**"
             + "（`BroadcastUnitDamaged` 排在 `CardScript.ReceiveDamage` **之前**："
             + "`_ResolveAttack_d__438__MoveNext.c:1286 → :1290`；同函数还用 `currentHealth − max(1, damage − armour)`"
             + " 算 `finalHealth` ⇒ 那时血还没掉）"
             + "｜🧨 把条件写回 `u.Health < 11`（掉血后）⇒ 7 < 11 ⇒ 会给 ⇒ **本条红**（与 F1 互斥）");
        }

        // F3 值 = 0（原版 `AddTraitSilently(trait, hideTrait: false, defaultIntValue: 0)`）
        {
            var ctx = NewCtx();
            ToP1Turn(ctx, 3);
            var tank = Place(ctx, 0, 3, Unit("B103R5_T3", 1, 0, 10));
            tank.ShouldBecomeInvulnerableWithLowLife = true;
            RuleCore.HurtWithSourceForTest(ctx, tank, 1, "B103R5");
            Tr(tank.Has(ConditionEval.Invulnerable), "F3-a **前提断言**：先拿到 invulnerable");
            C(tank.KwValue(ConditionEval.Invulnerable), 0,
               "F3 ★ 那条 trait 的 **value = 0**（原版 `AddTraitSilently(invulnerable, hideTrait: false, defaultIntValue: 0)`）"
             + "｜🧨 把 `AddKeyword(ConditionEval.Invulnerable, 0)` 写回 `(…, 1)` ⇒ 变 1 ⇒ 红");
        }

        // F4 0 伤 ⇒ 不给（原版 `damageValue == 0 ⇒ return`）
        {
            var ctx = NewCtx();
            ToP1Turn(ctx, 3);
            var tank = Place(ctx, 0, 3, Unit("B103R5_T4", 1, 0, 10));
            tank.ShouldBecomeInvulnerableWithLowLife = true;
            RuleCore.HurtWithSourceForTest(ctx, tank, 0, "B103R5");
            C(tank.Health, 10, "F4-a **前提断言**：0 伤 ⇒ 血不动（仍 10，在阈值内）");
            Tr(!tank.Has(ConditionEval.Invulnerable),
               "F4 ★ 伤害 0 ⇒ **不给**（原版 `ResolveDamageDealt` 第一句 `damageValue == 0 ⇒ return`；"
             + "我们 = `actual > 0` 那道闸）"
             + "｜🧨 删掉 `actual > 0` ⇒ 红");
        }

        // F5 flag 假 ⇒ 再低也不给（本地恒 false 的那一格 —— 值来自远端 FTUE 配置）
        {
            var ctx = NewCtx();
            ToP1Turn(ctx, 3);
            var tank = Place(ctx, 0, 3, Unit("B103R5_T5", 1, 0, 5));
            RuleCore.HurtWithSourceForTest(ctx, tank, 1, "B103R5");
            C(tank.Health, 4, "F5-a **前提断言**：5 − 1 = 4（远在阈值下）");
            Tr(!tank.Has(ConditionEval.Invulnerable),
               "F5 ★ `ShouldBecomeInvulnerableWithLowLife == false` ⇒ 再低也不给"
             + "（这一格的值来自远端 FTUE 配置 ⇒ 本仓生产恒 false，见 `UnitState.SetupFTUELowLifeInvulnerability`）"
             + "｜🧨 删掉那道 flag 闸 ⇒ 红");
        }
    }

    // ============================================================ 夹具
    //  ⚠️ 与 `B98K2_Assert` / `B102C506_Assert` 同款（走真实 `NewBattle`，`shuffle: false`）；
    //     本文件是**独立静态类**，⛔ 别指望拿到别的断言类里 `private` 的夹具。
    static CardDef Unit(string name, int costMelee, int atk, int hp, params string[] kws)
        => new CardDef(name, name, "unit", "", null, "Test", costMelee, atk, hp, 0, kws);

    static CardDef Hero(string name, string faction)
        => new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B103R5_W0", "SpaceWolves") };
        var d1 = new List<CardDef> { Hero("B103R5_W1", "Goff") };
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

    /// <summary>摆一个单位。🔴 `{ Exhausted = false }` 必须写（ctor 恒 `Exhausted = !HasDeployExemption`
    /// = true，动作收尾的 `RecomputeActAndAttack` 会按 `SummonSickness`/`AttacksThisTurn` 重算 ⇒ 不钉住会飘）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }
}
