// B97_ER2_Assert.cs — 第九十七会话 · 执行代理 `B97_ER2`
//
// ============================ 它盯的是什么 ============================
// 本批只动 `RuleEngine/Core/EffectResolver*.cs` + `ActionPump.cs`，其中**真正落了代码**的是三处：
//   · 账 `921` ① —— 弃牌揭示：`EffectResolverPartial_B88E2.cs` 的 `DiscardHandCardCore`
//     （`AddToCemetery` **之前**发 `EmitCardBeingDrawn(..., isSteal:false)`）；
//   · 账 `921` ② —— 偷牌揭示：`EffectResolver.cs` 的 `StealCardAcrossSides`
//     （`from.Hand/Deck.Remove` **之前**发 `EmitCardBeingDrawn(..., isSteal:true)`，`seat` = **被偷方**）；
//   · 账 `427` —— `ResolveOathAbility` / `RunPlayChosenCardAbility` 的誓约重放次数**改成每圈重算**
//     （原版 `while (IsThereDoubleOathEffect(...) > i)`）；
//   · 账 `1278` —— 判据侧结论（`isEnchantment` 不参与任何分支，只被透传进 `AbilityData+0x38`）⇒ 只记不作改动。
//
// 判据：
//   921 → `decomp_full/BattleManager._ResolveDiscardFromHand_d__544__MoveNext.c:95`（0.2f）
//         `decomp_full/BattleManager._ResolveStealFromDeck_d__547__MoveNext.c:243`（0.4f）
//         + `CardScript__ShowCardBeingDrawn.c:9-14`（两档时长**各用各的**）
//   427 → `decomp_full/BattleManager__IsThereDoubleOathEffect.c:33` +
//         `CardScript__ResolveActiveAbilityPlayed.c:66-72` / `AbilityLogic__PlayAbility.c:2043-2057`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 里加**一个块**（建议位置 = 紧接 `{ CheckB97USJam(); }` 那一行**之后**；
// 现读该行 = `:1294`，插完在它下面）：
//       // 🆕 2026-10-29（调度台代接线）：`B97_ER2` —— 账 921 弃/偷揭示两跳 + 账 427 誓约重放每圈重算。
//       { CheckB97ER2Gaps(); }
//   🔴 **故意不用 `Step(...)`、方法名也不带 `Test`/`Report` 前缀** —— 理由与 `B96_CR` / `B97_US` / `B97_W1`
//     三份同一条：`Step(...)` 会让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿**反射清单**对账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · ① 把 `DiscardHandCardCore` 里那句 `EmitCardBeingDrawn` 删掉 ⇒ A 段首条红（0 条 DiscardReveal）。
//   · ① `seat` 写成 `1 - seat`（或写死对面）⇒ A 段第二条红（Player 实得 1）。
//   · ② 把 `isSteal: true` 写成 `false`（或删掉）⇒ B 段首条红（收到的是一条 DiscardReveal）。
//   · ② `seat` 写成 `owner`（被偷的那一方没传对）⇒ B 段第二条红（Player 实得 0）。
//   · ② `StealCardAcrossSides` 里那句被挪到 `Remove` **之后** ⇒ 本件**测不出**（本件只验「发没发/发对没」，
//     时序靠代码注释与 `.c` 对位；如实标着，⛔ 不假装断到时序）。
//   · ③ 把 `OathExtraReplays` 改成「数双方」⇒ C 段第 3 条红（实得 2 而不是 1）。
//   · ③ 把 `OathExtraReplays` 改成恒 0 ⇒ C 段首条红。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97ER2Gaps()
    {
        Section("🆕 B97_ER2：账 921 弃/偷揭示两跳（`ShowCardBeingDrawn` · 0.2 / 0.4 各用各的）· 账 427 誓约重放每圈重算");
        B97ER2DiscardRevealUsesDiscardingSeat();
        B97ER2StealRevealUsesVictimSeat();
        B97ER2OathReplaysCountsOnlyOwnSide();
    }

    // ==================================================================
    //  1 · 弃牌揭示（账 921 ①）
    //  落点 = `EffectResolverPartial_B88E2.DiscardHandCardCore` 的 `AddToCemetery(ps, inst);` **之前**
    //  原版 = `_ResolveDiscardFromHand_d__544__MoveNext.c:95` 的 `ShowCardBeingDrawn(0.2f)`
    // ==================================================================
    static void B97ER2DiscardRevealUsesDiscardingSeat()
    {
        var mine = Unit("B97ER2DA", 1, 1, 1);
        // 🔴 2026-10-30（`B100_F18` · G1）：`Battle` → `ProbeBattle`。`Battle` 的 `Deck()` 塞 20 张 filler
        //   再起手发 3/4 张 ⇒ 「手牌 1 张」前提不成立（实测 4）。⛔ 期望值仍是 1（改成 4 会废掉判别式）。
        var ctx = ProbeBattle(new[] { mine }, new CardDef[] { });
        Check(ctx.Players[0].Hand.Count, 1, "前提：我方手牌 1 张");
        if (ctx.Players[0].Hand.Count != 1) return;
        var inst = ctx.Players[0].Hand[0];
        int mark = ctx.Signals.Count;

        // 🔴 2026-10-30（`B100_F34` · 330135/330150 那一组 · **α 夹具**，⛔ 产品无缺陷）：
        //   `DoDiscardWholeHand` 按 `playersAffected` 决定动哪一侧
        //   （`EffectResolverPartial_B88E2.PlayersAffectedOf`：`op.LogicIndex < 0` 或读不到载体卡
        //   ⇒ 回 `-1` ⇒ 代码里那句写死「**两侧都不动**」⇒ 弃牌根本没发生 ⇒ `DiscardReveal` 自然不发）。
        //   改前夹具只给了 `{Verb, Source}` ⇒ `LogicIndex` 停在 `-1`、`ctx.ActingUnit/PlayingCard` 都是 null ⇒ `pa = -1`。
        //   **`-1` = 「读不到 ⇒ 不猜」是正确行为**（`PlayersAffectedOf` 的 doc 写死），所以修夹具、不改期望。
        //   载体位 = `ctx.PlayingCard`（本入口 `source` 传 null ⇒ `ActingUnit` 恒 null，回落位只剩它；
        //   且本重载**不覆写** `PlayingCard` —— 只有重载 #1 的 `EffectResolver.cs:371` 会覆写）。
        ctx.PlayingCard = B97ER2WholeHandCarrier();
        var ops = new List<EffectOp>
        {
            new EffectOp { Verb = "discardwholehand", Source = "B97ER2 弃牌", AbilityIndex = 0, LogicIndex = 0 },
        };
        RuleCore.ResolveOps(ctx, 0, null, ops, by: "B97ER2");

        Check(ctx.Players[0].Hand.Count, 0, "前提：手牌已被弃空（否则说明这一条根本没走到弃牌那一跳）");

        BattleEvent drain = null, steal = null;
        for (int i = mark; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind == EvtKind.DiscardReveal) drain = e;
            if (e.Kind == EvtKind.StealReveal) steal = e;
        }
        Check(drain != null, true,
              "🧨 弃牌发了 `DiscardReveal`（原版 `_ResolveDiscardFromHand_d__544__MoveNext.c:95`；删掉 `DiscardHandCardCore` 里那句 `EmitCardBeingDrawn` ⇒ 这里 0 条）");
        Check(steal == null, true, "🧨 弃牌**没有**发成 `StealReveal`（两档时长不同、⛔ 别统一）");
        if (drain == null) return;
        Check(drain.Player, 0,
              "🧨 `Player` = **弃牌那一手的主人**（原版 `EntityScript.isPlayer`）；`seat` 写成 `1 - seat` ⇒ 实得 1");
        Check(drain.Slot, -1, "`Slot` 恒 -1（不在场上）");
        Check(drain.InstanceId, inst.Id, "`InstanceId` = 被揭示那一份（表现层要靠它反查视图）");
        Check(drain.CardId, mine.Name, "`CardId` = 卡名");
    }

    // ==================================================================
    //  2 · 偷牌揭示（账 921 ②）—— `seat` 必须是**被偷那一方**（不是拿到牌的那一方）
    //  落点 = `EffectResolver.StealCardAcrossSides` 的 `from.Hand/Deck.Remove` **之前**
    //  原版 = `_ResolveStealFromDeck_d__547__MoveNext.c:243`（0.4f）
    // ==================================================================
    static void B97ER2StealRevealUsesVictimSeat()
    {
        var theirs = Unit("B97ER2SB", 1, 1, 1);
        // 🔴 2026-10-30（`B100_F18` · G1）：同上 —— `ProbeBattle`（只有督军 + 指定牌），否则后手起手 4 张。
        var ctx = ProbeBattle(new CardDef[] { }, new[] { theirs });
        Check(ctx.Players[1].Hand.Count, 1, "前提：对手手牌 1 张");
        if (ctx.Players[1].Hand.Count != 1) return;
        var inst = ctx.Players[1].Hand[0];
        ctx.LastChosenCard = inst;                 // 钉住目标（`PickStealTarget` 的 ⓪ 支）
        int mark = ctx.Signals.Count;

        var ops = new List<EffectOp> { new EffectOp { Verb = "stealhand", Source = "B97ER2 偷牌" } };
        RuleCore.ResolveOps(ctx, 0, null, ops, by: "B97ER2");

        Check(ctx.Players[1].Hand.Count, 0, "前提：对手手牌已被拿空（否则这一条没走到偷牌那一跳）");

        BattleEvent steal = null, drain = null;
        for (int i = mark; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e.Kind == EvtKind.StealReveal) steal = e;
            if (e.Kind == EvtKind.DiscardReveal) drain = e;
        }
        Check(steal != null, true,
              "🧨 偷牌发了 `StealReveal`（原版 `_ResolveStealFromDeck_d__547__MoveNext.c:243`；把 `isSteal:true` 写成 `false` ⇒ 这里收到的是 DiscardReveal）");
        Check(drain == null, true, "🧨 偷牌**没有**发成 `DiscardReveal`");
        if (steal == null) return;
        Check(steal.Player, 1,
              "🧨 `Player` = **被偷那一方**（原版 `EntityScript.isPlayer` = 这张牌属于谁的手）；传 `owner` ⇒ 实得 0");
        Check(steal.Slot, -1, "`Slot` 恒 -1（不在场上）");
        Check(steal.InstanceId, inst.Id, "`InstanceId` = 被揭示那一份");
    }

    // ==================================================================
    //  3 · 誓约重放次数（账 427）—— 只数**同方场上**带卡面 `oathDouble` 的牌
    //  判据 = `BattleManager__IsThereDoubleOathEffect.c:33`
    //  ⚠️ **如实标着的残余**：原版读 `HasCurrentTrait`（含运行时授予），
    //     我们读 `Card.OathDouble`（**卡面** trait）—— 那一半在 `RuleCore.cs`，不属本代理。
    //     本段因此**断的是「算子今天的口径」**（同方 + 卡面 trait），不是原版口径。
    // ==================================================================
    static void B97ER2OathReplaysCountsOnlyOwnSide()
    {
        var a = Unit("B97ER2OA", 1, 2, 5);
        var b = Unit("B97ER2OB", 1, 2, 5);
        var e = Unit("B97ER2OE", 1, 2, 5);
        var ctx = Battle(new CardDef[] { }, new CardDef[] { });
        var ua = Place(ctx, 0, 1, a);
        var ub = Place(ctx, 0, 2, b);
        var ue = Place(ctx, 1, 1, e);
        Check(ua != null && ub != null && ue != null, true, "前提：三个单位都上了棋盘");
        if (ua == null || ub == null || ue == null) return;

        a.OathDouble = true;
        e.OathDouble = true;                        // 对面那张：⛔ 不该被数进来
        Check(RuleCore.OathExtraReplays(ctx, 0), 1,
              "🧨 只数**本方**场上带 `oathDouble` 的牌（判据 `BattleManager__IsThereDoubleOathEffect.c:33`）；改成数双方 ⇒ 实得 2");

        b.OathDouble = true;
        Check(RuleCore.OathExtraReplays(ctx, 0), 2,
              "🧨 本方第二张也带上 ⇒ 跟着变 2（证明它真的**逐张现数**，不是缓存/写死）");
        Check(RuleCore.OathExtraReplays(ctx, 1), 1, "对面同理（各数各的侧）");
    }

    // ==================================================================
    //  夹具 · `discardwholehand` 的 `playersAffected` 载体卡（2026-10-30 `B100_F34` 新增）
    // ==================================================================
    /// <summary>
    /// `discardwholehand` 取值用的**载体卡**：`PlayersAffectedOf` 只认「`ActiveAbilityLogicGroup(ActiveAbilityObject)`
    /// **恰好 1 行** ∧ `PlayersAffected[LogicIndex] >= 0`」（`EffectResolverPartial_B88E2.cs:819-833`）。
    /// <para>口径：`playersAffected = 0`（**Both**）= 原版 `AbilityLogic.playersAffected` 的 SO 缺省值，
    /// 也是 `AbilityLogic__PlayAbility.c:293-345` 取 `GetCardsInHandList(…, affected 己, affected 敌)` 时
    /// 两侧都为真的那一档（本夹具 P1 手牌为空 ⇒ 实际只弃 P0 那 1 张）。</para>
    /// <para>⚠️ 形状照 <see cref="RuleEngineTest.B94Z3ActiveAbilityCard360"/> 同一套（`hasActiveAbility = 1`
    /// + 一行 `gate = 1` + `activeAbilityLogicVersioned = true`），只是把 `chosenAbility` 换成 `165 (discardWholeHand)`。</para>
    /// </summary>
    static CardDef B97ER2WholeHandCarrier()
    {
        var row = new CardDef.ActiveAbilityLogicSpec
        {
            Ability = CardDef.ActiveAbilityObject,   // -1 = 普通支（`PlayersAffectedOf` 读的就是这一组）
            Gate = 1,
            Trigger = 400,                            // 原版逐行传的虚拟时机（`ThisUnitAbilityUsed`）
            NeedsLoading = 0,
            TargetsAffected = new[] { 0 },            // 0 = any（`LogicCount` 靠这几列任一行取长度）
            FurtherMultiplier = new[] { 0 },
            ChosenAbility = new[] { 165 },            // 165 = discardWholeHand
            PlayersAffected = new[] { 0 },            // 0 = Both（原版 SO 缺省）
        };
        return new CardDef("B97ER2Carrier", "B97ER2Carrier", "unit", "", null, "Test", 1, 1, 1, 0,
                           hasActiveAbility: 1,
                           activeAbilityLogic: new[] { row },
                           activeAbilityLogicVersioned: true);
    }
}
