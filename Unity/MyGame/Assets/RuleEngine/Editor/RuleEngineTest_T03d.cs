// RuleEngineTest_T03d.cs — 引擎线 `T03·d` · `P2-2` 副产物（2026-10-21）
//
// **账**：`W58-REVIEWFIX-B2`（审查 `P2-2`：把 `Attack` 宣言/广播挪到哨戒/星镖之后）的**副产物**——
//   被哨戒打死的攻击者，**它自己的 `When … attacks` 触发也不再响**。
//
// **判据（第一权威 = 反编译 `d:/2/tools/decomp_full/`）**：
//   · `BattleManagerSupport__BroadcastUnitAttacked.c:22`（被打者）/ `:29`（攻击者）——
//     两次 `CardScript__ResolveUnitAttacked(...)` 是**无条件预调**（只判非 null）；
//     **棋盘循环 `:48` 才** `CardScript__IsInPlay(...)` ⇒ **预调那一跳没有「在场」判据**。
//   · `CardScript__ResolveUnitAttacked.c:26` —— 接收者那一道闸只有「身上有 `RawCardScript`」。
//   · 能力层自己的闸 = `CardAbility__CanTriggerAbility` → `CardAbility__IsRightContext.c:16-33/51-55`：
//       `abilityContext = Play(0)`         ⇒ `IsInPlay` + `!EnoughPendingDamageToDie`（状态 5 **不算**在场）；
//       `abilityContext = InPlayOrDying(40)` ⇒ `CardScript__IsInPlayOrDying.c:10-13`（**状态 5 = 待死算在场**）。
//   · 现读当前 build 的卡数据（`资料/普查产出_1018/allcards_1350/cards_raw.json`）：
//       `SW16 Long Fang`（全池唯一 `When this unit attacks …`）`abilityContext` = **40** ⇒ **该响**；
//       6 张「… attacked [by this unit]」也**全是 40** ⇒ **该响**。
//       ⚠️ 反过来，`UM83 Valtus` / `GSC12 Neophyte Iconbearer` / `DA22 Techmarine`（`Play(0)`）
//          在**自己正待死**时原版**不响**。
//     ⚠️ 如实标着：我们这一层**没有** `abilityContext` 这个字段（解析层只有卡面文法）⇒
//        本轮把放开的范围**收在「攻击者那一下预调」**（账点名的症状），并**接受**那 3 张 `Play(0)`
//        卡在待死时跟着响的近似；被打者侧的 `:22` 预调**不动**（那一侧 ctx 混合，判据未闭合）。
//        要精确收口得把 `abilityContext` 带进卡数据（见本笔报告的「没查清」）。
//     ⚠️ 反过来，**棋盘循环那一跳必须继续挡待死单位**（原版 `:48` 的 `IsInPlay`），⛔ 别一起放开。
//
// **我们改前的形状（两条闸）**：
//   · `EffectResolver.FireBoardWhenListener` 的第一道闸 `ctx.IsPendingDeath(u)` 被**预调**与**棋盘跳**共用
//     ⇒ 把预调也挡了（原版只挡棋盘循环）⇒ 本条就是它挡出来的；
//   · `RuleCore.DeclareAttack` 里 `… attacked [by this unit]` 那一族的 `attacker.IsAlive`（同族、同一次攻击）。
//
// 🔴 每条都带 `🧨`（什么改动能让它红）——没写 `🧨` 的断言一律当没写。
// ⚠️ 本文件里直接调 `RuleCore.*` 的是**夹具直调**（同 `RuleEngineTest_S14.cs` 口径），不是对局。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>
    /// **被哨戒打死的攻击者，自己的「攻击时」触发仍然要响**（`T03·d` · `P2-2` 副产物）。
    ///
    /// 夹具共同形状：攻击者 2 血、目标带 `Sentry 5` ⇒ **哨戒先打死攻击者**；攻击照常结算。
    ///   ① `When this unit attacks an enemy with Hunt Mark, gain +1 Attack`（= `Long Fang` 文法）；
    ///   ② `Destroy any troop attacked by this unit`（= 6 张 `… attacked [by this unit]` 那一族）。
    /// 判据与两条闸的来历 → 本文件头。
    /// </summary>
    static void TestT03dAttackerKilledBySentryStillFiresOwnAttack()
    {
        // ==============================================================
        //  ① `When this unit attacks …`（原版 trigger 50 · `abilityContext = 40`）
        // ==============================================================
        var atk = new CardDef("T03DAtk", "T03DAtk", "unit",
                              "When this unit attacks an enemy with Hunt Mark, gain +1 Attack",
                              "common", "Test", 1, 2, 2, 0, null, subtype: "Infantry");
        CheckTrue(atk.WhenTriggers.Count > 0,
                  "🧪 夹具自检：攻击者的 `When this unit attacks …` 监听器注册上了");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var a = Place(ctx, 0, 3, atk, exhausted: false);
        var b = Place(ctx, 1, 3, Unit("T03DSentry", 1, 0, 12, "Sentry 5"), exhausted: true);
        b.AddKeyword("huntmark", 1);                       // 宾语筛选（`an enemy with Hunt Mark`）要它
        Check(a.Attack, 2, "🧪 （前提）攻击者初始攻 2");
        ctx.ClearSignals();

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                  "近战打那个带 `Sentry 5` 的目标");
        CheckTrue(!a.IsAlive,
                  "（前提）**哨戒把攻击者打死了**（2 血 − 5 点）" + LogTail(ctx));
        Check(b.Health, 12 - 2,
              "（前提）攻击**照常打下去**（目标挨了 2；哨戒不打断攻击）" + LogTail(ctx));

        Check(WhenFired(ctx, "T03DAtk"), 1,
              "★★ T03·d：被哨戒打死的攻击者，**它自己的 `When this unit attacks` 仍然响 1 次**"
            + "｜判据 = `BattleManagerSupport__BroadcastUnitAttacked.c:29`（攻击者那一下是**无条件预调**、"
            + "不在 `:48` 的 `IsInPlay` 循环里）+ `CardScript__IsInPlayOrDying.c:10-13`"
            + "（`abilityContext`=40 ⇒ **状态 5 待死算在场**；现读 `cards_raw.json` 的 `SW16 Long Fang` = 40）"
            + "｜🧨 把 `PreCallBoardListener(..., allowPendingDeath: true)` 去掉（或把 "
            + "`FireBoardWhenListener` 的闸改回无条件的 `if (ctx.IsPendingDeath(u)) return;`）⇒ 本行实得 0"
            + LogTail(ctx));
        Check(a.Attack, 3,
              "★★ 而且效果**真的落了地**：攻击者 2 → 3（`DoGive` 的逐目标闸 =「活着 ∨ 还在棋盘上」，"
            + "待死单位仍在棋盘上）｜🧨 同上 ⇒ 本行实得 2" + LogTail(ctx));

        // ==============================================================
        //  ② `… attacked [by this unit]` 那一族（原版**同一个 trigger 50**，`abilityContext` 也全是 40）
        // ==============================================================
        var venom = new CardDef("T03DVenom", "T03DVenom", "unit",
                                "Destroy any troop attacked by this unit",
                                "common", "Test", 1, 2, 2, 0, null, subtype: "Infantry");
        CheckTrue(venom.AttackedOps != null,
                  "🧪 夹具自检：`… attacked by this unit` 的正文收得下来");

        var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx2, 1);
        var v = Place(ctx2, 0, 3, venom, exhausted: false);
        Place(ctx2, 1, 3, Unit("T03DBig", 1, 0, 20, "Sentry 5"), exhausted: true);
        ctx2.ClearSignals();

        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 3, 1, 3), RuleCodes.OK,
                  "近战打那个带 `Sentry 5` 的目标（20 血，只可能被正文摧毁）");
        CheckTrue(!v.IsAlive, "（前提）**哨戒把攻击者打死了**" + LogTail(ctx2));
        CheckTrue(Board(ctx2, 1, 3) == null,
                  "★★ T03·d：被哨戒打死的攻击者，**「Destroy any troop attacked by this unit」仍然结算**"
                + "（20 血的目标被正文摧毁，不是被那 2 点伤害打死的）"
                + "｜判据同 ①（6 张真卡的 `abilityContext` 全是 40；原版那一跳也是 `:29` 的无条件预调）"
                + "｜🧨 把 `if (ctx.Players[p].Board[atkSlot] == attacker)` 改回 "
                + "`if (attacker.IsAlive && …)` ⇒ 目标还站在场上，本行红" + LogTail(ctx2));
    }
}
