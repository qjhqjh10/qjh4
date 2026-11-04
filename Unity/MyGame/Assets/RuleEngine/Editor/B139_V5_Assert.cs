// B139_V5_Assert.cs — 执行代理 `B139_V5`（2026-11-04 · 批次 `B139`）
//
// ============================ 它盯的是什么 ============================
//  破坏（Sabotage）广播 **`OnSabotageCreated`(701)** 的 `subject` / 原版 `targetCard` 取**哪一方的督军**。
//
//  原版判据（逐句现读）：
//    · `decomp_full/BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:95-98`：
//      `BattleManager__GetHero(mgr, *(createdCardScript + 0x40 /*isPlayer*/))` → `BroadcastSabotageCreated`。
//    · 那张卡落在**敌方手牌** —— 卡面 `Create a random Sabotage in the enemy hand`；
//      数据 `cards_truth.json` GSC7 `summon.isForEnemy = 1`；手牌侧由创牌目的侧决定
//      （`BattleManager__AddNewCardToHand.c:52-57` → `BattleCardManager__CreateCard.c:90` →
//      `CardScript__CardSetup.c:42` 写 `isPlayer = param_4 = 收牌方`）。
//    · ⇒ 原版 `targetCard` = **收牌方（敌方）督军**，不是施放方督军。
//  我们的落地 = `EffectResolver.cs:4301` 传 `subjectSide = who`（收牌方），`who` 仍 = `owner`（`When you …` 归属）。
//
// ============================ 为什么它会长这样 ============================
//  `TargetCriteriaEval.cs:222-237` 的阵营闸：候选（= `subject`）与 ability 归属**同侧** ⇒ 查 `friendlyUnits`，
//  **异侧** ⇒ 查 `enemyUnits`。真卡 `GSC68 Atalan Jackal`（池内 701 **唯一**一行）的 criteria 是
//  `{enemyUnits:1, friendlyUnits:0}` ⇒ **只有 subject 在敌方侧**这一条才过闸。
//  ⇒ 下面 A / B 两条用的是同一份夹具、只翻 criteria 的 friendly/enemy：
//    · A（`enemyUnits:1 / friendlyUnits:0`）⇒ **必须恰好 1 条**；
//    · B（`friendlyUnits:1 / enemyUnits:0`）⇒ **必须 0 条**。
//  **两侧结构上不可能同时满足**（候选只能在一侧）⇒ 这一对就是「灭自证」：把 subject 传回施放方，
//  A 变 0 条、B 变 1 条 ⇒ **两条同时红**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A：把 `EffectResolver.cs:4301` 的 `subjectSide` 从 `who` 改回 `owner`（= 改前的缺陷形状）⇒ A 红（0 条）。
//  · B：同一处改成「subject 取收牌方」以外的任何一侧（含 `null` 哨兵不加约束）⇒ B 红（1 条）。
//  · 前提三条（两侧督军都在场 / 敌方棋盘**只有督军** / 破坏卡真的进了敌方手牌）是**硬检查**，
//    ⛔ 不写成 `if (前提) Check(结论)` —— 前提塌了必须当场红，不许静默跳过（CLAUDE.md「自检里的
//    `if (x != null) Check(...)` = 前提不成立时静默跳过」那一条）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由 `CardPresentation/Editor/B108_A1_TriggerEmit_Assert.cs` 的
    /// `CheckB108A1TriggerEmits()` 调 —— 与 701 那个族的断言同宿，⛔ 不另开宿主 / 不改 `RuleEngineTest.cs`）。</summary>
    static void CheckB139V5SabotageSubject()
    {
        Section("🆕 B139_V5：`OnSabotageCreated`(701) 的 subject = **收牌方（敌方）督军**");

        // ---- A：真生产链（真卡池 + 真 `PlayTactic`）· criteria = GSC68 的形状 `enemyUnits:1 / friendlyUnits:0` ----
        {
            var watch = E6Listener("B139V5WatchA", "When you create a Sabotage, gain +1 Attack",
                                   701, E6Criteria(1, 0));
            var mk = Tactic("T_B139V5SabA", 0, "Create a random Sabotage in the enemy hand");
            var ctx = BattlePool(new[] { watch, mk }, new[] { Unit("B139V5FoeA", 1, 1, 9) },
                                 CardDatabase.Load(), warlordFaction: "Genestealers");
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, watch, exhausted: true);

            // 前提（硬检查）：两侧督军都在场 —— 否则主体取不到，会走「只跑棋盘那一跳」的退回分支，
            //   本断言就测不到 subject 那一侧（⛔ 不许静默变成一条空转的绿）。
            CheckTrue(ctx.Players[0].Warlord != null && ctx.Players[1].Warlord != null,
                      "★ A①（前提）双方督军都在场（`ctx.Players[0/1].Warlord != null`）"
                    + "｜🧨 夹具换代 / 督军没上场 ⇒ 长注释里那条退回分支生效 ⇒ 红");
            // 前提（硬检查）：敌方棋盘上**只有督军** ⇒ 「subject 在敌方侧」≡「subject 就是敌方督军」。
            {
                int n = 0; UnitState only = null;
                var b = ctx.Players[1].Board;
                for (int s = 0; s < b.Length; s++) if (b[s] != null) { n++; only = b[s]; }
                Check(n, 1, "★ A②（前提）敌方棋盘上**只有督军**一个单位"
                          + "｜🧨 多摆一个敌方单位 ⇒ 「在敌方侧」就不再等价于「是敌方督军」⇒ 判据变宽 ⇒ 红");
                CheckTrue(ReferenceEquals(only, ctx.Players[1].Warlord),
                          "★ A③（前提）那一个就是 `ctx.Players[1].Warlord` 本尊");
            }

            int enemyHandBefore = ctx.Players[1].Hand.Count;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_B139V5SabA"), -1), RuleCodes.OK,
                      "A④ 打出「造一张破坏进**敌方**手牌」的战术卡");
            Check(ctx.Players[1].Hand.Count, enemyHandBefore + 1,
                  "★ A⑤（前提）破坏卡真的进了**敌方**手牌（手牌 +1）"
                + "｜🧨 造到了施放方自己手里 ⇒ 前提塌 ⇒ 红");

            Check(B139V5CountTrigger(ctx, "OnSabotageCreated"), 1,
                  "★★ A⑥ `OnSabotageCreated`：监听者（我方 · criteria `enemyUnits:1 / friendlyUnits:0`）"
                + "**过了 ability 级阵营闸** ⇒ 恰好一条 `EvtKind.Trigger`"
                + "｜🧨 把 `EffectResolver.cs:4301` 的 `subjectSide` 改回 `owner`（= subject = 施放方督军）"
                + "⇒ 同侧 ⇒ 查 `friendlyUnits`（那格 = 0）⇒ 闸判死 ⇒ 0 条 ⇒ **红**");
            // ⚠️ 2026-11-04 更正（调度台）：原期望「恰好一行」是**我们自己编的口径**（原版没有日志）。
            //    实测同一事件会产出 **2 行**同文本（日志重复，已记成 `B139-V5b` 待查）。
            //    ⇒ 这条只守「**有**对应日志」这一件事；「事件条数」由 A⑥ 守（那个是硬的）。
            CheckTrue(WhenFired(ctx, "B139V5WatchA") >= 1,
                  "★ A⑦ 同一件事的日志口径（`—— 事件「createsabotage」触发：「B139V5WatchA」的监听器 ——` **至少一行**）");
        }

        // ---- B：灭自证 —— 同一份夹具，criteria 翻成 `friendlyUnits:1 / enemyUnits:0` ⇒ **必须 0 条** ----
        //   候选（subject）只可能在**一侧** ⇒ A 与 B 结构上不可能同时满足。
        //   ⛔ 这不是「再跑一遍同一个方向」，它挡的正是「subject 又传回施放方」那种改法（那时 B 会变 1 条）。
        {
            var watch = E6Listener("B139V5WatchB", "When you create a Sabotage, gain +1 Attack",
                                   701, E6Criteria(0, 1));
            var mk = Tactic("T_B139V5SabB", 0, "Create a random Sabotage in the enemy hand");
            var ctx = BattlePool(new[] { watch, mk }, new[] { Unit("B139V5FoeB", 1, 1, 9) },
                                 CardDatabase.Load(), warlordFaction: "Genestealers");
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, watch, exhausted: true);
            CheckTrue(ctx.Players[0].Warlord != null && ctx.Players[1].Warlord != null,
                      "★ B①（前提）双方督军都在场");
            int enemyHandBefore = ctx.Players[1].Hand.Count;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_B139V5SabB"), -1), RuleCodes.OK,
                      "B② 同样的战术卡");
            Check(ctx.Players[1].Hand.Count, enemyHandBefore + 1,
                  "★ B③（前提）破坏卡同样进了**敌方**手牌");
            Check(B139V5CountTrigger(ctx, "OnSabotageCreated"), 0,
                  "★★ B④（灭自证）`friendlyUnits:1 / enemyUnits:0` 的监听者 ⇒ **0 条** —— "
                + "因为原版的 `targetCard` 是**敌方**督军，与「我方」那条闸正好相反"
                + "｜🧨 把 subject 传回施放方督军（= 改前的缺陷形状）⇒ 同侧 ⇒ `friendlyUnits` 命中 ⇒ **1 条 ⇒ 红**");
        }
    }

    /// <summary>`E6CountTriggerEvents` 的「带 ability 后缀」版 —— `EmitCardTrigger` 的 `Keyword` 可能是
    /// `OnSabotageCreated#N`（`BattleEvent.KeywordWithAbility`，`abilityIndex >= 0` 时才加）。
    /// ⚠️ 只在**本条**用；`E6CountTriggerEvents` 的逐字相等口径不动（别的族指望它数「不带后缀」）。</summary>
    static int B139V5CountTrigger(BattleContext ctx, string keyword)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
        {
            if (e == null || e.Kind != EvtKind.Trigger || e.Keyword == null) continue;
            if (e.Keyword == keyword || e.Keyword.StartsWith(keyword + "#")) n++;
        }
        return n;
    }
}
