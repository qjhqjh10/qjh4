// B97W19_Assert.cs — 执行代理 `B97_W19`（第九十七会话 · 波 1 补位）的断言
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与
// `B97W10_Assert.cs` / `B97_W3_Assert.cs` 同一种形状）—— `RuleEngine/Editor/RuleEngineTest.cs` 本体
// **本批不在白名单**，所以测试体全写在这里，宿主只需在 `Run()` 里加一行（见下）。
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动:253` —— `BattleManager.CheckUnitsCancellingAttack` 的**接线**：
//   攻击结算段开头「这一记被谁取消 / 改打谁」改回原版的**唯一入口**
//   （候选表 → 逐候选响 `AboutToAttack(280)` → `redirectedAttack(74)` 的同步等价 = 就地改目标）。
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R1_判据补全.md`「第二轮·1」
//   ＋ 本轮报告 `资料/完全复刻/报告/B97_W19.md`。
//
// ============================ 宿主接线（调度台做）============================
//   `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加一行（建议紧跟在
//   `Step(CheckB97W10PlayOrderAndStableSort);` 之后）：
//       Step(CheckB97W19CancelAttackByUnits);
//   （本方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 判红。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · W19A（真卡响 280）：把 `FireCancelAttackTrigger` 的「补发信号」那两行删掉（或把
//     `TryCancelAttackByUnits` 的 `using (BeginAction)` 整段去掉）⇒ 触发计数回到 0 ⇒ A2 红。
//   · W19A：把 `HasCancelAttackAbility` 的「数据层那一半」删掉 ⇒ `SAU33` 根本不是候选 ⇒
//     连替身改向都没了（A3/A4 一起红）—— 这一条同时钉住「候选来自数据层 ability」。
//   · W19B（只结算一次）：把旧的捷径（`TryRedirectAttackToBodyguard` 私有 `Bodyguard` 闸那一条）
//     和新的 280 路**同时**接上 ⇒ 这一记会打两遍 ⇒ B1/B2/B3/B4 红。
//   · W19C（`forced` 闸）：把 `DeclareAttackExecute` 那句 `if (!forced)` 去掉 ⇒ 强制攻击被截走 ⇒ C1/C3 红。
//   · W19D（成员来自统一表）：把 `CheckUnitsCancellingAttack` 换回「只看 `Card.Bodyguard`」⇒
//     「有 trigger 280 ability、但卡面没写替身那句」的夹具**不再是候选** ⇒ D2 红。
//   每条期望值都是**字面量 / 由构造规则直接算出**，⛔ 不从被测实现反算；
//   前提断言与结论断言**并列写**（⛔ 不写 `if (前提) Check(结论)` —— 前提不成立时要红、不许静默跳过）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里那一行调用）。</summary>
    public static void CheckB97W19CancelAttackByUnits()
    {
        Section("🆕 B97_W19：`CheckUnitsCancellingAttack` 接线（`AboutToAttack(280)` / 替身改向）");
        W19A_RealCardFiresAboutToAttack();
        W19B_OneAttackOnly();
        W19C_ForcedStillBypasses();
        W19D_CandidateComesFromUnifiedList();
    }

    // ==========================================================================================
    //  A. **真卡**：`Vargard Obyron`（池内唯一一张带 trigger 280 的卡）在场时，
    //     打敌方督军 ⇒ `AboutToAttack(280)` 响**一次**（改前 = **0 次**：旧的替身捷径根本不发 trigger），
    //     同时这一记照旧改到替身身上（⛔ 别为了让 280 响而把改向弄丢）。
    // ==========================================================================================
    static void W19A_RealCardFiresAboutToAttack()
    {
        var pool = CardDatabase.Load();
        var obyon = CreatePool.FindByName(pool, "Vargard Obyron");
        CheckTrue(obyon != null,
            "★（前提）卡池里有 `Vargard Obyron`｜🧨 卡名改动 / 整张被删 ⇒ 红");
        if (obyon == null) return;
        CheckTrue(obyon.Bodyguard,
            "★（前提）它是「替身」（`CardDef.Bodyguard`，从卡面那句原文解析出来）");
        Check(obyon.AbilityTriggerPresence(280), 1,
            "★（前提）它卡上**真有** `trigger 280` 的 ability 行（数据层）"
            + "｜🧨 `abilityTriggers` 那一列没读进来 ⇒ 红（这时下面 A2 也必然 0 次）");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        Place(ctx, 0, 3, Unit("FAtk280", 1, 3, 9), exhausted: false);
        var body = Place(ctx, 1, 3, obyon, exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int bodyHp = body.Health, lordHp = lord.Health;
        int before = CountTrigger(ctx, KeywordTable.AboutToAttack);

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "声明攻击**打敌方督军格**（场上有 `Vargard Obyron`）");

        int after = CountTrigger(ctx, KeywordTable.AboutToAttack);
        Check(after - before, 1,
            "★ `AboutToAttack(280)` 响**正好一次** —— 原版 `_ResolveAttack…:531-600`：候选表非空 ⇒"
            + " 对**每个**候选 `ResolveCancelAttack` → `OnTrigger(0x118)`"
            + "｜🧨 旧的替身捷径（只改目标、不发 trigger）⇒ 本行 0 次而红");
        CheckTrue(ctx.Events.Exists(s => s != null && s.Contains("ABOUTTOATTACK")),
            "★ 日志里也有一条 `触发 ABOUTTOATTACK`（`ctx.Events` 是给排查用的那一份）"
            + "｜🧨 只在事件流里发、日志漏了 ⇒ 红 ");
        CheckTrue(body.Health < bodyHp,
            $"★ 这一记**照旧改到替身身上**（{bodyHp} → {body.Health}）—— 响 280 与改向是同一件事"
            + "｜🧨 只接触发、丢了改向 ⇒ 红" + LogTail(ctx));
        Check(lord.Health, lordHp,
            "★ ……而督军**一滴没掉**（改前的行为不许退）");
    }

    // ==========================================================================================
    //  B. **这一记只结算一次** —— 替身那条 280 正文的净效果是「同一记攻击换个目标」，
    //     ⛔ **不是**「再打一记」（原版：先把原动作 `CancelAttack` 掉、再排一条 `redirectedAttack(74)`）。
    //     灭自证：旧捷径 + 新 280 路**同时接上** ⇒ 伤害 / 攻击事件 / 攻击配额都会翻倍。
    //     夹具把三个数**拉开**：攻方 4 · 替身 3 · 督军 2 ⇒ 量得出「用的是谁的那一份」。
    // ==========================================================================================
    static void W19B_OneAttackOnly()
    {
        var guard = new CardDef("B97W19Guard", "B97W19Guard", "unit",
                                "Any attack against your Warlord targets this troop instead.",
                                "common", "Test", 1, 3, 9, 0, null, subtype: "Infantry");
        CheckTrue(guard.Bodyguard, "★（前提）夹具卡识得出「替身」");
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("FAtk19Once", 1, 4, 9), exhausted: false);
        var body = Place(ctx, 1, 3, guard, exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int aHp = atk.Health, bHp = body.Health, lHp = lord.Health, lordAtk = lord.Attack;
        int before = CountTrigger(ctx, KeywordTable.AboutToAttack);

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "声明攻击**打督军格**");

        Check(bHp - body.Health, 4,
            "★① 替身挨的**正好是攻方那一份**（4 点）—— 不是两份（8 点）"
            + "｜🧨 旧捷径与 280 路同时生效 ⇒ 红");
        Check(aHp - atk.Health, 3,
            $"★② 攻方吃到的反击来自**替身**（3 点），**不是**督军的 {lordAtk} 点"
            + " —— 「只把伤害挪过去、反击还找督军算」在这里露馅");
        int nAtk = 0, iLast = -1;
        for (int i = 0; i < ctx.ActionLog.Count; i++)
            if (ctx.ActionLog[i].Kind == EvtKind.Attack) { nAtk++; iLast = i; }
        Check(nAtk, 1, "★③ 留档里**只有一条**攻击事件 —— 是【换目标】不是【多打一记】");
        CheckTrue(iLast >= 0 && ctx.ActionLog[iLast].TargetSlot == 3
                  && ctx.ActionLog[iLast].TargetCardId == body.Name,
            "★③……而那一条事件的**目标就是替身那一格 / 那一张**"
            + "（只换伤害、不换事件 = 只改了一半）");
        Check(atk.AttacksThisTurn, 1, "★④ 攻方的攻击配额只记了**一次**");
        Check(lord.Health, lHp, "★⑤ ……督军一滴没掉");
        Check(CountTrigger(ctx, KeywordTable.AboutToAttack) - before, 1,
            "★⑥ 这一条路上 280 也只响一次（⛔ 不是每结算一次各响一次）");
    }

    // ==========================================================================================
    //  C. `forced`（强制攻击）**不被截** —— 原版 `forceAttack(0x2a)` 走 `_ResolveForceAttack`，
    //     而**全库只有 `_ResolveAttack` 调 `CheckUnitsCancellingAttack`**（`A1256`）。
    //     灭自证：把 `DeclareAttackExecute` 那句 `if (!forced)` 去掉 ⇒ C1/C3 红。
    // ==========================================================================================
    static void W19C_ForcedStillBypasses()
    {
        var guard = new CardDef("B97W19GuardForce", "B97W19GuardForce", "unit",
                                "Any attack against your Warlord targets this troop instead.",
                                "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
        var mk = Tactic("T_B97W19ForceAtWarlord", 0, "Target friendly unit attacks an enemy warlord");
        var ctx = ProbeBattle(new[] { mk }, new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("FAtk19Force", 1, 4, 9), exhausted: false);
        var body = Place(ctx, 1, 3, guard, exhausted: true);
        var lord = ctx.Players[1].Warlord;
        int aHp = atk.Health, bHp = body.Health, lHp = lord.Health;
        int before = CountTrigger(ctx, KeywordTable.AboutToAttack);

        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_B97W19ForceAtWarlord"), 3),
                  RuleCodes.OK, "打出「强制攻击」那一张（**打的是敌方督军**）");
        Check(lHp - lord.Health, 4,
            "★【`A1256`】**强制攻击打的就是督军本人**，3 号格那个替身截不走它"
            + "｜🧨 去掉 `if (!forced)` ⇒ 红" + LogTail(ctx));
        Check(body.Health, bHp, "★ ……而替身**一滴没掉**");
        Check(aHp - atk.Health, 2,
            "★ 攻方吃的是**督军**那 2 点反击（替身只有 1 攻）—— 「只把伤害数字挪过去、目标没换」在这里露馅");
        Check(CountTrigger(ctx, KeywordTable.AboutToAttack) - before, 0,
            "★ 这一记**一次 280 都不响**（原版强制攻击那条路根本不调 `CheckUnitsCancellingAttack`）"
            + "｜🧨 把 `if (!forced)` 去掉 ⇒ 会响一次而红");
    }

    // ==========================================================================================
    //  D. **候选成员来自【统一表】`CheckUnitsCancellingAttack`，不是私有的 `Card.Bodyguard` 闸。**
    //     夹具：一张**卡面没写替身那句**（⇒ `Bodyguard == false`）、但**数据层真有 trigger 280** 的单位
    //     ⇒ 它必须是候选（原版 `CardScript__HasCancelAttackAbility.c:34-43`：扫自己 `cardAbilities`
    //     里 `trigger == 0x118` 的那条，⛔ 不看任何「替身」数据列）。
    //     ⚠️ 如实标着的一格：它的正文（`chosenAbility` 族）本版没有派发口 ⇒
    //        `CardDef.Bodyguard` 仍是「正文 = AddRedirectedAttack」的等价判定 ⇒ **这一记不会改向**
    //        （原版会），池内这种卡 **0 张**。
    // ==========================================================================================
    static void W19D_CandidateComesFromUnifiedList()
    {
        var f280 = new CardDef("B97W19NoBodyguard280", "B97W19NoBodyguard280", "unit", "",
                               "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry",
                               abilityTriggers: new[] { 280 }, abilityTriggersVersioned: true);
        CheckTrue(!f280.Bodyguard,
            "★（前提）这张夹具**没有** `Bodyguard` 标记（卡面空）｜🧨 若解析器把空文本认成替身 ⇒ 红");
        Check(f280.AbilityTriggerPresence(280), 1,
            "★（前提）它的 `abilityTriggers` 里**真有 280**");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 4);
        var atk = Place(ctx, 0, 3, Unit("FAtk19D", 1, 3, 9), exhausted: false);
        var u280 = Place(ctx, 1, 2, f280, exhausted: true);
        // 🔴🆕 **2026-10-30（`B101_R1`）：`4` → `5`。** `BoardSpec.WarlordSlot == 4`（`BoardSpec.cs:30`），
        //   开局 `RuleCore.cs:655` 正是 `p.Board[4] = p.Warlord` ⇒ 把普通部队摆 4 号格会**把督军挤出盘面**；
        //   而 `DeclareAttack`（`RuleCore.cs:6687`）的 target 就是 `Board[tgtSlot]`、
        //   `TryCancelAttackByUnits`（`:17409`）第一句要 `target.IsWarlord` ⇒ 改前那一记压根没进 280 那条路
        //   （实测 b100g `:341265` 声明攻击 OK、`:341281` 280 实得 0、`:341296` 出声句也没有）。
        var plain = Place(ctx, 1, 5, Unit("FPlain19D", 1, 1, 9), exhausted: true);
        var lord = ctx.Players[1].Warlord;

        var cands = RuleCore.CheckUnitsCancellingAttack(ctx, atk, lord);
        CheckTrue(cands.Contains(u280),
            "★ 有 trigger 280 ability、**没有** `Bodyguard` 标记的单位**是候选**"
            + "（成员按「卡上有没有那条 ability」判，⛔ 不是按替身数据列）"
            + "｜🧨 把候选判据换回 `Card.Bodyguard` ⇒ 本行红");
        CheckTrue(!cands.Contains(plain),
            "★ 反例：场上那个普通部队（无 280 ability、无替身标记）**不是候选**"
            + "｜🧨 判据写成「一律收」⇒ 红");
        CheckTrue(!cands.Contains(lord),
            "★ 反例：督军自己**不是候选**（它没有那条 ability）");
        CheckTrue(!cands.Contains(atk),
            "★ 反例：**攻方自己**不在候选表里（它在对面那一侧；能力 criteria 写的是 `friendlyUnits`）");

        int before = CountTrigger(ctx, KeywordTable.AboutToAttack);
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "打敌方督军格（场上有一个「只有 280 ability」的单位）");
        Check(CountTrigger(ctx, KeywordTable.AboutToAttack) - before, 1,
            "★ 它的 280 照样响一次（数据层 ability 也有 `OnTrigger(0x118)` 那一跳）");
        CheckTrue(ctx.Events.Exists(s => s != null && s.Contains("不是重定向")),
            "★ 而且**出声**：候选有、但本版判不出「它的正文是重定向」⇒ 明说这一记照旧结算"
            + "（原版会取消）｜🧨 静默吞掉 ⇒ 红");
        CheckTrue(u280.IsAlive, "★（反例的边界）这个夹具**没挨打**（它不替督军挡）—— 本版如实标着的缺口");
    }

    // ==========================================================================================
    //  夹具：数事件流里某个关键词的 `EvtKind.Trigger` 条数。
    //  ⚠️ 用 `ActionLog`（留档，`Emit` 同一份事件流的第二份），⛔ 不用 `Signals`
    //     （那是给表现层的、`DrainSignals` 一搬就空 ⇒ 数出来会随宿主时序变）。
    // ==========================================================================================
    static int CountTrigger(BattleContext ctx, string keyword)
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
