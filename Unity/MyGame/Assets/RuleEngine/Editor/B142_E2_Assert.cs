// B142_E2_Assert.cs — 批次 **B142** · 执行写手 `B142-E2`（条目 `B141-L253-CANCEL` / `B141-DURINGBATTLE` / `CHK-05 L543`）
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与 `B97W19_Assert.cs` 同一种形状）
// —— `RuleEngine/Editor/RuleEngineTest.cs` 本体**本批不在白名单**，测试体全写在这里，宿主只需在 `Run()` 里加一行（见下）。
//
// ============================ 它盯的是什么 ============================
//  ① `B141-L253-CANCEL`：原版 `_ResolveAttack_d__438__MoveNext.c:531 CheckUnitsCancellingAttack → :537 CancelAttack
//     → :539/:541 ClearPendingDamage → :557-563 逐候选 ResolveCancelAttack → :601 才判迷彩`
//     ⇒ **候选存在、且它的 280 正文不是重定向 ⇒ 这一记被真的取消**（一点伤害都不出、不吃攻击配额、不失隐身/迷彩）。
//     我们改前：`RuleCore.cs` 只 `ctx.Log` 一句「本版照旧结算」⇒ 照打。
//  ② `B141-DURINGBATTLE`：`MatchTypes.IsDuringBattle` 改吃 `ctx`，`ctx == null` 即原版 `UIstate == 0`（⇒ false）。
//  ③ `CHK-05 L543`：`EventTiming.DeploySummonHold = 0.45` 就是**完整**寿命（原版那条「带 `cardAnim`」的分支是死码，
//     反证 → `报告/B141_W128_CHK05_543_986.md` §1；本轮 `B142_E2` 现读复核 `AddSummonUnit` 不写 `cardAnim(+0xF8)`）。
//
// ============================ 宿主接线（调度台做）============================
//   `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 聚合行**同一物理行追加**（落在该行第一个 `//` 之前）：
//       Step(CheckB142E2CancelAttackByUnitsAndPredicates);
//   （方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 判红。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · E2A/E2C：把 `DeclareAttackCore` 里那句 `TryCancelAttackByUnits(...)` 删掉（退回改前的「只出声、照旧结算」）
//     ⇒ 督军掉血、攻方吃反击、配额变 1、隐身被摘 ⇒ A1–A5 / C1 一起红。
//   · E2B（对照）：把 `HasCancelAttackAbility` 的数据层那一半改成「一律真」⇒ 没 280 的场上单位也成候选 ⇒ B1/B2 红。
//   · E2D（不双响）：把 `DeclareAttackExecute` 里那句 `TryCancelAttackByUnits` **也**加回来（两处都调）
//     ⇒ 280 响两次、替身挨两下 ⇒ D4/D1 红。
//   · E2E：把 `IsDuringBattle` 改回旧签名/旧式子 ⇒ E1–E5 红（`ctx == null` 那一档恒 true）。
//   · E2F：把 `DeploySummonHold` 改成「0.45 + 逐卡 clip」⇒ F1 红。
//   每条期望值都是**字面量 / 由构造规则直接算出**，⛔ 不从被测实现反算；前提断言与结论断言**并列写**。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里那一行调用）。</summary>
    public static void CheckB142E2CancelAttackByUnitsAndPredicates()
    {
        Section("🆕 B142_E2：`CheckUnitsCancellingAttack` 真取消 · `IsDuringBattle(ctx)` · 召唤 0.45 s");
        E2A_NonRedirectCandidateCancelsAttack();
        E2B_ControlWithoutCandidate_AttackLands();
        E2C_CancelledAttackKeepsStealth();
        E2D_BodyguardRedirectStillWorksAndFires280Once();
        E2E_IsDuringBattleCtx();
        E2F_SummonHoldIsCompleteWithoutPerCardClip();
    }

    // ==========================================================================================
    //  A. **候选存在、但它的 280 正文不是重定向 ⇒ 这一记被取消**
    //     （原版 `_ResolveAttack…:531-541`：`CancelAttack` + 两次 `ClearPendingDamage` + 早退，早于 `:601` 迷彩）
    // ==========================================================================================
    static void E2A_NonRedirectCandidateCancelsAttack()
    {
        var f280 = NonBodyguard280("B142E2_280A");
        CheckTrue(!f280.Bodyguard, "★（前提）夹具卡**没有** `Bodyguard` 标记（卡面空）");
        Check(f280.AbilityTriggerPresence(280), 1, "★（前提）夹具卡的 `abilityTriggers` 里**真有** 280");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("E2AtkA", 1, 3, 9), exhausted: false);
        var u280 = Place(ctx, 1, 2, f280, exhausted: true);
        var plain = Place(ctx, 1, 5, Unit("E2PlainA", 1, 1, 9), exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int lordHp = lord.Health, atkHp = atk.Health, plainHp = plain.Health;
        int before = E2CountTrigger(ctx, KeywordTable.AboutToAttack);

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "声明攻击**打敌方督军格**（场上有一个「只有 280 ability」的单位）");

        Check(lord.Health, lordHp,
            "★ 督军**一滴没掉** —— 这一记被取消（原版 `:531-541`）"
            + "｜🧨 退回「只出声、照旧结算」⇒ 本行红" + LogTail(ctx));
        Check(atk.Health, atkHp,
            "★ 攻方也**没吃反击** —— 攻击压根没发生（⛔ 不是「打出去但伤害为 0」）");
        Check(atk.AttacksThisTurn, 0,
            "★ **没消耗攻击配额**（原版取消排在攻击动画里的 `attacksTurn++` 之前）"
            + "｜🧨 把调用点留在 `DeclareAttackExecute`（配额之后）⇒ 本行红");
        CheckTrue(!atk.Exhausted, "★ ……所以也**不疲劳**（配额上限 1 的普通单位打完一次才疲劳）");
        Check(E2CountTrigger(ctx, KeywordTable.AboutToAttack) - before, 1,
            "★ 它的 280 **仍然响一次**（取消不等于不响 —— 原版先 `CancelAttack` 再逐候选 `ResolveCancelAttack`）");
        CheckTrue(ctx.Events.Exists(s => s != null && s.Contains("取消这一记")),
            "★ 日志里有一条「取消这一记」（⛔ 不许静默 —— 池内 0 张，但口径要留痕）"
            + "｜🧨 只改行为、删掉那句日志 ⇒ 红");
        CheckTrue(u280.IsAlive, "★ 那个 280 单位**没挨打**（它不是替身、这一记被取消）");
        Check(plain.Health, plainHp, "★ 场上的普通部队也没挨打（取消 = 一点伤害都不出）");
    }

    // ==========================================================================================
    //  B. **对照**：场上**没有** 280 候选时，同一条攻击**照常结算**（钉住 A 组不是空转）
    // ==========================================================================================
    static void E2B_ControlWithoutCandidate_AttackLands()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("E2AtkB", 1, 3, 9), exhausted: false);
        Place(ctx, 1, 5, Unit("E2PlainB", 1, 1, 9), exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int lordHp = lord.Health;

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "（对照）同一个局面、只把「280 单位」撤掉 ⇒ 打敌方督军格");

        Check(lordHp - lord.Health, 3,
            "★ 督军**掉了 3**（攻方 3 攻）—— 没有候选就照常结算" + LogTail(ctx));
        Check(atk.AttacksThisTurn, 1, "★ 配额**记了一次**");
        CheckTrue(!ctx.Events.Exists(s => s != null && s.Contains("取消这一记")),
            "★ ……而且**没有**「取消这一记」那条日志（⛔ 别把取消做成无条件）");
    }

    // ==========================================================================================
    //  C. 取消**不失隐身**（原版取消排在 `:601` 迷彩判定与攻击动画里的失 Stealth 之前）
    // ==========================================================================================
    static void E2C_CancelledAttackKeepsStealth()
    {
        var f280 = NonBodyguard280("B142E2_280C");
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("E2AtkC", 1, 3, 9, KeywordTable.Stealth), exhausted: false);
        CheckTrue(atk.Has(KeywordTable.Stealth), "★（前提）攻方带着 `stealth`");
        Place(ctx, 1, 2, f280, exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int lordHp = lord.Health;

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "带潜行的攻方打敌方督军格（场上有 280 单位）");

        Check(lord.Health, lordHp, "★ 这一记被取消（督军没掉血）");
        CheckTrue(atk.Has(KeywordTable.Stealth),
            "★ 攻方**仍然潜行** —— 取消发生在「失 Stealth」那一步之前"
            + "｜🧨 把取消留在 `DeclareAttackExecute`（失 Stealth 之后）⇒ 本行红");
    }

    // ==========================================================================================
    //  D. **替身改向那一支不许退**：280 响一次、替身挨打、督军不掉、配额一次
    //     （改前这条路的调用点在 `DeclareAttackExecute`；本轮上移，必须证明**没有双响 / 双打**）
    // ==========================================================================================
    static void E2D_BodyguardRedirectStillWorksAndFires280Once()
    {
        var guard = new CardDef("B142E2Guard", "B142E2Guard", "unit",
                                "Any attack against your Warlord targets this troop instead.",
                                "common", "Test", 1, 3, 9, 0, null, subtype: "Infantry");
        CheckTrue(guard.Bodyguard, "★（前提）夹具卡识得出「替身」");
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("E2AtkD", 1, 4, 9), exhausted: false);
        var body = Place(ctx, 1, 3, guard, exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int lordHp = lord.Health, bodyHp = body.Health, atkHp = atk.Health;
        int before = E2CountTrigger(ctx, KeywordTable.AboutToAttack);

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "打敌方督军格（场上有替身）");

        Check(lord.Health, lordHp, "★ 督军一滴没掉（改向）");
        Check(bodyHp - body.Health, 4, "★ 替身挨的**正好一份**（4 点；两份 = 8 ⇒ 双打）"
            + "｜🧨 两处都调 `TryCancelAttackByUnits` ⇒ 红");
        Check(atkHp - atk.Health, 3, "★ 攻方吃的是替身的 3 点反击（不是督军那 2 点）");
        Check(atk.AttacksThisTurn, 1, "★ 配额**只记一次**");
        Check(E2CountTrigger(ctx, KeywordTable.AboutToAttack) - before, 1,
            "★ 280 **只响一次**｜🧨 双调用点 ⇒ 2 次而红");
    }

    // ==========================================================================================
    //  E. `MatchTypes.IsDuringBattle(ctx, includeMulligan)` —— 15 档映射（`ctx == null` = `UIstate == 0`）
    // ==========================================================================================
    static void E2E_IsDuringBattleCtx()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        CheckTrue(MatchTypes.IsDuringBattle(ctx, false),
            "★① 有 `ctx`、未终局、不在换牌期 ⇒ **true**（原版「其余战斗态」）");
        CheckTrue(!MatchTypes.IsDuringBattle(null, true),
            "★② `ctx == null` ⇒ **false** —— 这就是原版 `UIstate == 0`（inactive 开打前）那一档"
            + "｜🧨 旧签名 `(isOver, mulliganOpen, includeMulligan)` 在这一格判 **true** ⇒ 红");
        ctx.MulliganOpen = true;
        CheckTrue(!MatchTypes.IsDuringBattle(ctx, false),
            "★③ 换牌期 + `includeMulligan == false` ⇒ **false**（原版 `UIstate ∈ {11,12}`）");
        CheckTrue(MatchTypes.IsDuringBattle(ctx, true),
            "★④ 换牌期 + `includeMulligan == true` ⇒ **true**");
        ctx.MulliganOpen = false;
        ctx.Winner = 1;
        CheckTrue(!MatchTypes.IsDuringBattle(ctx, true),
            "★⑤ 终局后（`Winner != 0`，原版 `UIstate ∈ {7,8}`）⇒ **false**，`includeMulligan` 也救不回来");
    }

    // ==========================================================================================
    //  F. L543：召唤那一拍 = **0.45 s 完整寿命**（原版「带 `cardAnim`」那一支是死码 ⇒ ⛔ 不加逐卡 clip）
    // ==========================================================================================
    static void E2F_SummonHoldIsCompleteWithoutPerCardClip()
    {
        Check(EventTiming.DeploySummonHold, 0.45f,
            "★ `DeploySummonHold` = **0.45**（= 原版 `_MinionPlayedIntoField` 的四个 `WaitForSeconds` 之和：0.1+0.05+0.2+0.1）"
            + "｜🧨 改成「0.45 + 逐卡 clip」⇒ 红");
        Check(EventTiming.DeployHoldOf(DeploySource.Summon), EventTiming.DeploySummonHold,
            "★ 效果召唤整档就是这个常量（⛔ 不是常量 + 一张卡的召唤动画）");
        Check(EventTiming.DeployHoldOf(DeploySource.Reanimate), EventTiming.DeploySummonHold,
            "★ 复活共用同一档（原版同一族协程）");
    }

    // ==================================================================
    //  夹具 / 小工具
    // ==================================================================
    /// <summary>「数据层有 trigger 280、卡面没写替身那句」的单位（⇒ 候选，但正文不是重定向）。</summary>
    static CardDef NonBodyguard280(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 9, 0, null,
                           subtype: "Infantry", abilityTriggers: new[] { 280 }, abilityTriggersVersioned: true);
    }

    /// <summary>数事件流里某个关键词的 `EvtKind.Trigger` 条数（⛔ 不用 `Signals`：那是给表现层的、会被搬空）。</summary>
    static int E2CountTrigger(BattleContext ctx, string keyword)
    {
        int n = 0;
        for (int i = 0; i < ctx.ActionLog.Count; i++)
        {
            var e = ctx.ActionLog[i];
            if (e.Kind != EvtKind.Trigger) continue;
            if (string.Equals(e.Keyword, keyword, StringComparison.OrdinalIgnoreCase)) n++;
        }
        return n;
    }
}
