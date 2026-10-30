// B97W1_Assert.cs — 执行代理 `B97_W1`（第九十七会话 · 波 1）的断言
//
// ============================ 它盯的是什么 ============================
//   战斗线 `RuleCore.cs` 本笔改的**三件半**（`账 战斗逻辑驱动`）：
//     ① `:172` `BattleManager.AddTransformCardWithBuff` —— `chosenAbility 85` 要能进主动能力的
//        「这一次跑哪几行」计划表（改前被当 `unimplemented` 剔掉 ⇒ 整支永远走不到）。
//     ② `:870` `CardScript.ResolveDamageDealt` 的 **concussive** —— **反击那一枪**也要能打晕
//        （改前反击的 `Hurt` 不传 `kind` / `actingCard` ⇒ 反击方的 `concussion` 从不触发）。
//     ③ `:866` 原版 `ResetToValuesInHand` 两个调用点：
//        ③-a 残骸翻面（`ResetToValuesInHand(card, 0)`）⇒ `AlreadyTriggeredBacklash` **要保留**、
//            `Instance` 那两本账要被清；
//        ③-b 抽空牌库时的墓地回填（`ResetToValuesInHand(card, 1)`）⇒ 回填那张的 `QuestCounter` 归零。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/报告/B97_W1.md` §逐条证据 ＋
//   `decomp_full/BattleManager._ResolveTransformIntoRemnant_d__554__MoveNext.c:43` ·
//   `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:89` ·
//   `decomp_full/CardScript__ResolveDamageDealt.c:209-221` ·
//   `decomp_full/AbilityLogic__PlayAbility.c:904-941`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A（ca 85）：把 `RuleCore.ActiveRowEntersPlan` 里的 `|| ca == 85` 删掉 ⇒ A1 红。
//                 把方法改成恒 `true` ⇒ A4（`9999` 必须 false）红。两条一起 ⇒ 不可能两边同时满足。
//   · B（反击震荡）：把 `DeclareAttack` 反击支那一行的 `ApplyConcussionOnAttackDamage(...)` 调用删掉 ⇒ B1 红。
//                 把 `ApplyConcussionOnAttackDamage` 里的 `if (!dealer.Has("concussion")) return;` 删掉 ⇒ B3 红
//                 （对照：守方**没有** `concussion` 时攻方**不许**被晕）。
//   · C（残骸保留 backlash）：把 `rem` 的 initializer 里 `AlreadyTriggeredBacklash = u.AlreadyTriggeredBacklash` 删掉
//                 ⇒ C2 红；把它改成恒 `false` ⇒ C3（对照：没置位的那一条必须 false）红。
//                 把 `u.ResetToValuesInHand(clearBacklash: false)` 删掉 ⇒ C4（`Instance.QuestCounter` 归零）红。
//   · D（回填清账）：把 `ReshuffleCemeteryIntoDeck` 里那句 `ResetHandCardToValuesInHand(...)` 删掉 ⇒ D2 红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里插一行：`Step(TestB97W1);`（本文件的类 = `RuleEngineTest` 的**分部**，
//   这样才复用得上夹具 `Battle` / `Unit` / `Place` / `ToP1Turn` / `Board` / `Check`——
//   ⛔ 另起一个类会把那套夹具抄第二份，违反「一处判据」。）
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>B97_W1 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB97W1);`）。</summary>
    public static void TestB97W1()
    {
        Section("B97_W1 —— 战斗线三件半（账 172 / 870 / 866）");

        // ---------------------------------------------------------------- A 账 172：`chosenAbility 85` 进计划表
        // 前提：夹具里**没有**任何 `activeAbilityLogic` 正文 op ⇒ `ActiveRowHasOps` 恒 false
        //      ⇒ 下面四条**只能**由 `ca` 那一段命中（这正是「灭自证」的第一半）。
        CheckTrue(!RuleCore.ActiveRowHasOpsForTest(null, 0),
                  "（前提）空载体上 `ActiveRowHasOps(null, 0)` = false —— 否则下面 A1 会被正文 op 顶上（假绿）");
        CheckTrue(RuleCore.ActiveRowEntersPlanForTest(null, 0, 85),
                  "★ A1 `chosenAbility 85 (transformUnitWithBuff)` **进**计划表 —— 判据 "
                  + "`AbilityLogic__PlayAbility.c:904-941`（`if (iVar8 != 0x55) return;` ⇒ `AddTransformCardWithBuff`，`0x55 = 85`）。"
                  + "改前它落到 `unimplemented` 分支 ⇒ `UseActiveAbilityCore` 返回 `ErrUnimplemented`、整支走不到");
        CheckTrue(RuleCore.ActiveRowEntersPlanForTest(null, 0, 360)
                  && RuleCore.ActiveRowEntersPlanForTest(null, 0, 370)
                  && RuleCore.ActiveRowEntersPlanForTest(null, 0, 400),
                  "★ A2 三条**既有**的免正文 op 档（360 / 370 / 400）没被这次抽方法改坏");
        CheckTrue(!RuleCore.ActiveRowEntersPlanForTest(null, 0, 9999),
                  "★ A3 **灭自证**：不在那四档里的 `ca`（`9999`）必须是 **false** —— 把方法写成恒 `true` 就红");

        // ---------------------------------------------------------------- B 账 870：反击那一枪的 concussive
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("Atk", 1, 2, 5));            // 2 攻 ⇒ 打不死 5 血的守方
            var def = Place(ctx, 1, 3, Unit("Def", 1, 2, 5, "Concussion")); // 守方带震荡
            Check(atk.IsStunned, false, "（前提）打之前攻方没晕");
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "B0 这一发攻击能打出去");
            Check(def.IsStunned, false, "（前提）**守方**没被晕 —— 攻方**不带** `concussion`，"
                                           + "主攻击那一枪的震荡只晕「被打者」⇒ 守方这一格必须是 false（否则说明夹具串了）");
            CheckTrue(atk.IsStunned,
                      "★ B1 **反击那一枪的 concussive 生效了** —— 判据 `CardScript__ResolveDamageDealt.c:209-221`"
                      + "（`case 4`：`damageType == 4` ∧ `DamageInfo.actingCard` 带 `0x3a2 concussion` ⇒ `AddStun(damagedCard)`）"
                      + "＋ `_ResolveAttack_d__438__MoveNext.c:1137`（反击也是 type-4）。"
                      + "改前反击的 `Hurt` 不传 `kind`/`actingCard` ⇒ 这一条是 false");
        }
        {
            // 对照（灭自证的另一半）：守方**没有** `concussion` ⇒ 攻方**不许**被晕。
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("Atk", 1, 2, 5));
            Place(ctx, 1, 3, Unit("Plain", 1, 2, 5));                   // 不带震荡
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "B2 对照局也能打出去");
            CheckTrue(!atk.IsStunned,
                      "★ B3 **灭自证**：守方没有 `concussion` ⇒ 反击**不许**晕攻方 —— "
                      + "把 `ApplyConcussionOnAttackDamage` 里那句 `if (!dealer.Has(\"concussion\")) return;` 删掉就红");
        }

        // ---------------------------------------------------------------- C 账 866-a：残骸翻面的 ResetToValuesInHand(0)
        {
            var remCard = new CardDef("FixtureRemnantBacklash", "FixtureRemnantBacklash", "unit", "",
                                      "common", "Test", 1, 2, 5, 0,
                                      new[] { KeywordTable.Remnant }, subtype: "Infantry");
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("Con", 1, 5, 5));                     // 5 攻 ⇒ 一击打掉 5 血
            var killed = Place(ctx, 1, 3, remCard);
            killed.AlreadyTriggeredBacklash = true;                     // 「这回合已经反噬过一次」
            killed.Instance.QuestCounter = 3;                           // 给 `ResetToValuesInHand ③` 留一个观测点
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "C0 这一发能打死它");
            var occ = Board(ctx, 1, 3);
            CheckTrue(occ != null && occ.IsRemnant,
                      "（前提）它**翻面成残骸**了（同一格还占着、`IsRemnant`）" + LogTail(ctx, 4));
            CheckTrue(occ != null && occ.AlreadyTriggeredBacklash,
                      "★ C1 残骸**保留了** `alreadyTriggeredBacklash` —— 原版 "
                      + "`_ResolveTransformIntoRemnant_d__554__MoveNext.c:43` 调的是 `ResetToValuesInHand(card, **0**)`，"
                      + "而 `.c:138-140` 只在 `param_2 != 0` 时清 `+0x222`。我们原来新造 `UnitState` 回落 false（= 悄悄清了）");
            CheckTrue(occ != null && occ.Instance != null && occ.Instance.QuestCounter == 0,
                      "★ C2 `ResetToValuesInHand` **真的跑了**：`Instance.QuestCounter(+0x270)` 归零 —— "
                      + "把残骸支那一句 `u.ResetToValuesInHand(clearBacklash: false)` 删掉就红");
        }
        {
            // 对照（灭自证的另一半）：**没置位**的那一条，翻面后必须仍是 false。
            var remCard = new CardDef("FixtureRemnantPlain", "FixtureRemnantPlain", "unit", "",
                                      "common", "Test", 1, 2, 5, 0,
                                      new[] { KeywordTable.Remnant }, subtype: "Infantry");
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("Con", 1, 5, 5));
            Place(ctx, 1, 3, remCard);                                  // `AlreadyTriggeredBacklash` 保持默认 false
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "C3 对照局");
            var occ = Board(ctx, 1, 3);
            CheckTrue(occ != null && occ.IsRemnant && !occ.AlreadyTriggeredBacklash,
                      "★ C4 **灭自证**：没置位的那一条翻面后必须仍是 **false** —— "
                      + "把 `AlreadyTriggeredBacklash = u.AlreadyTriggeredBacklash` 改成恒 `true` 就红");
        }

        // ---------------------------------------------------------------- D 账 866-b：抽空牌库时的墓地回填
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var ps = ctx.Players[0];
            // 拿一张**开局就在牌库里**的（`Battle` 建场时 `MarkInitialDeckCard` 已经登记过），挪进墓地。
            CheckTrue(ps.Deck.Count > 0, "（前提）P0 牌库非空");
            var inst = ps.Deck[ps.Deck.Count - 1];
            ps.Deck.RemoveAt(ps.Deck.Count - 1);
            ps.Discard.Add(inst);
            inst.QuestCounter = 5;                                      // 观测点
            CheckTrue(ctx.InitialDeckInstanceIds.Contains(inst.Id),
                      "（前提）这张是「开局就在牌库」的（原版 `+0x223`）—— 不然它不会回填");
            ps.Deck.Clear();                                            // 牌库抽空
            ctx.Vars.shouldReshuffle = true;
            RuleCore.Draw(ctx, 0);
            CheckTrue(ps.Hand.Contains(inst),
                      "（前提）它**回填后立刻被抽进了手牌** —— 否则下面 D1 对一条没走过的路径断言（假绿）"
                    + "｜🔴 2026-10-30（`B100_F37` · 铁律 5 订正）：原写「它**回填进牌库**了（`ps.Deck.Contains`）」—— "
                    + "**实际**产品是对的、断言的前提写错了：`RuleCore.cs:3070` 回填成功后会**继续往下抽**"
                    + "（原版 `_ResolveDrawCard_d__432__MoveNext.c:197` 也是回填后回到 `DrawCardFromDeck`），"
                    + "而牌库里只剩这一张 ⇒ `MoveDeckTopToHand`（`:3133-3141`）当场把它抽走 ⇒ `ps.Deck` 里当然没有它"
                    + LogTail(ctx, 4));
            Check(inst.QuestCounter, 0,
                  "★ D1 回填**之前**按原版 `CardScript__ResetToValuesInHand(card, 1)` 清过 "
                  + "`questCounter(+0x270)`（`_ResolveDrawCard_d__432__MoveNext.c:89 → :98` 的次序："
                  + "`if (+0x223 createdInInitialDeck) { ResetToValuesInHand(1) → UndissolveToDeck → … }`）。"
                  + "把 `ReshuffleCemeteryIntoDeck` 里那句 `ResetHandCardToValuesInHand(...)` 删掉就红");
        }
    }
}
