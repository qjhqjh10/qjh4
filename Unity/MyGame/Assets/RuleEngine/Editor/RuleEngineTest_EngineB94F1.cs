// RuleEngineTest_EngineB94F1.cs — 第九十四会话 · 波 1 · 执行代理 `F1`（`RuleCore.cs` 文件所有者）
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 本件只放本批**新增行为**的断言；宿主 `RuleEngineTest.cs` 里只加一个
// `{ CheckB94F1CemeteryExileAndOathPay(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，
// 也**不进 8 空格缩进的顶层语句** ⇒ 不会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例，
// 与 `B93_G1` / `B93_F7` 那两段的既有做法同形）。
//
// 本批覆盖的两条**真实行为改动**（判据 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   ① `RuleCore.ExileFromCemetery` / `IsExiledFromCemetery` —— 原版
//      `CardScript.shouldCheckIfIsInCemetery(+0x24b)`（`CardScript__Awake.c:40` 置 1 ·
//      `CardScript__RemoveCardFromCemetery.c:5` 置 0 · 全库无写回 1）＋
//      `BattleManager__GetCardsInCemetery.c:54` 只在非 0 时才把牌放进结果
//      ⇒ **放逐后的牌从墓地查询里永久消失**（账 534；旧账「可观测效果为零 ⇒ D」已被 `B94_R-B1` 推翻）。
//   ② `RuleCore.PayCardPlayedCostOath` —— 原版 `BattleManager__PayCardPlayedCostOath.c:1-31` 全文：
//      `HasCurrentTrait(oath)` ⇒ `UseMana(GetCurrentTraitValue(oath))` →
//      `CheckIfCodexTriggered(…, shouldCheckCost: 0)`（账 455）。
//      🔴 **2026-10-30（`B100_F29` · 铁律 5）：原文漏写了【谁在调用】** —— 实际：
//      ① **手牌出牌不调用它**（2026-10-29 `B96_OATH` 按原版删掉了 `PlayCardCore` 里那两处调用点；
//         `RuleCore.cs:3955` 的注释写着「⛔ 别在这里加誓约费」）；
//      ② 原版全树**唯一调用者** = `AbilityLogic__PlayAbility.c:2033`（`chosenAbility 360` 支，
//         守卫是 `cardChosenInSelection(+0x2e8) != null && HasDefaultTrait(oath)`）；
//         我们侧唯一调用点 = `EffectResolverPartial_B88E2.cs:993`（同一支）。
//      ⇒ 下面 `B94F1T2` 的期望值已按这条翻成「手牌出牌**只扣牌费**」。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94F1CemeteryExileAndOathPay()
    {
        Section("🆕 B94_F1：墓地放逐闩（`+0x24b`，账 534）· 手牌出牌**不收**誓约费（`PayCardPlayedCostOath` 只在 360 支，账 455）");
        B94F1T1CemeteryExile();
        B94F1T2OathPlayCost();
    }

    /// <summary>`B94F1` ①：墓地放逐 —— 查得到 → 放逐 → **查不到**，且**只影响那一份**。
    /// 判据 = `BattleManager__GetCardsInCemetery.c:54`（`+0x24b != 0` 才 Add）。</summary>
    static void B94F1T1CemeteryExile()
    {
        var ctx = Battle(new[] { Unit("CemA", 1, 1, 1), Unit("CemB", 1, 1, 1) },
                         new[] { Unit("FoeDummy", 1, 0, 9) });
        ToP1Turn(ctx, 1);

        // ---- 前提（先硬检查，⛔ 别写成 `if (前提) Check(结论)`）----
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "CemA"), 1), RuleCodes.OK, "前提：打出 `CemA`");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "CemB"), 2), RuleCodes.OK, "前提：打出 `CemB`");
        var a = B94F1UnitByName(ctx, 0, "CemA");
        var b = B94F1UnitByName(ctx, 0, "CemB");
        CheckTrue(a != null && b != null, "前提：两张都在场上");
        if (a == null || b == null) return;                 // 前提不成立就别往下测（会 NRE）

        // 🔴 2026-10-29（`B94_FX8` · DG1 δ1）：改用 `HurtWithSourceForTest`（`RuleCore.cs:9172` → `Hurt:9025`，自带
        //   `ActionScope` + `RemoveIfDead`）。裸 `ApplyDamage` 的 doc 明文写着「少了受伤触发 + **离场结算**」
        //   （`RuleCore.cs:8446-8447`）⇒ 尸体留在棋盘上、要等下一次 `CleanupDeaths` 才进墓地 ⇒ 下面是拉空的。
        RuleCore.HurtWithSourceForTest(ctx, a, 9, "自检");
        RuleCore.HurtWithSourceForTest(ctx, b, 9, "自检");

        var cem0 = RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true);
        var ia = B94F1CemFind(cem0, "CemA");
        var ib = B94F1CemFind(cem0, "CemB");
        CheckTrue(ia != null && ib != null, "前提：两张都进了墓地、且墓地查询**都查得到**（= 原版 `+0x24b == 1` 那一档）");
        if (ia == null || ib == null) return;
        Check(cem0.Count, 2, "前提：墓地查询一共 2 张");

        // ---- 被测：放逐 `CemA` ----
        CheckTrue(!RuleCore.IsExiledFromCemetery(ctx, ia), "放逐前 `IsExiledFromCemetery(CemA)` = false");
        RuleCore.ExileFromCemetery(ctx, ia);
        CheckTrue(RuleCore.IsExiledFromCemetery(ctx, ia), "放逐后 `IsExiledFromCemetery(CemA)` = true");

        var cem1 = RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true);
        // 🧨 把 `GetCardsInCemetery` 里那句 `|| IsExiledFromCemetery(ctx, inst)` 删掉 ⇒ 本条红（「CemA 又出现在墓地查询里」）。
        CheckTrue(B94F1CemFind(cem1, "CemA") == null,
                  "🧨 放逐后 `CemA` **不再出现**在任何墓地查询里（原版 `GetCardsInCemetery.c:54` 只在 `+0x24b != 0` 时 Add）");
        // 灭自证（同族）：**只能影响那一份**，别把整条墓地一起挡掉。
        CheckTrue(B94F1CemFind(cem1, "CemB") != null, "灭自证：没被放逐的 `CemB` **仍然查得到**");
        Check(cem1.Count, 1, "灭自证：墓地查询 2 → 1（不是 0）");

        // 幂等：再放逐一次不改变任何东西。
        RuleCore.ExileFromCemetery(ctx, ia);
        Check(RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true).Count, 1,
              "放逐是幂等的（同一份调两次，结果仍是 1 张）");
    }

    /// <summary>`B94F1` ②：手牌出牌费 —— 打出带 `oath` 的牌**只收牌费**（誓约费只由**主动能力**那一支收）。
    /// <para>判据 = `BattleManager__PayCardPlayedCostOath.c:1-31`（函数体全文）＋
    /// `AbilityLogic__PlayAbility.c:2033`（**全树唯一调用者**：`chosenAbility 360` 支，
    /// 守卫 `cardChosenInSelection != null && HasDefaultTrait(oath)`）。</para>
    /// <para>🔴 **2026-10-30（`B100_F29` · 铁律 5）：本方法的期望值翻面** —— 原写
    /// 「打出带 `oath` 的牌要**再付一次**誓约点数，`3 − 1 − 1 = 1`」（2026-10-29 `cef436cd` 那版），
    /// **实际 = `3 − 1 = 2`**：同一天 `B96_OATH` 按原版删掉了 `PlayCardCore` 里那两处调用点
    /// （见 `RuleCore.cs:3955` 那行注释）。⇒ 本条现在守的是**反向**规矩：手牌出牌**不许**多扣誓约费。</para></summary>
    static void B94F1T2OathPlayCost()
    {
        // 夹具：1 费 + `oath 1`（关键词表里 `oath` 无数字 ⇒ `KwValue` 按「存在即真」取 1）。
        var oathCard = new CardDef("OathFixture", "OathFixture", "unit", "", null, "Test",
                                   1, 1, 1, 0, new[] { "oath" });
        // ---- 前提：夹具真的带 oath，且点数 = 1（⛔ 别假定解析成功）----
        CheckTrue(oathCard.Has(KeywordTable.Oath), "前提：夹具身上有 `oath`（`CardDef.Has`）");
        Check(oathCard.KwValue(KeywordTable.Oath), 1, "前提：`oath` 的点数 = 1（无数字 ⇒ 存在即真取 1）");
        if (!oathCard.Has(KeywordTable.Oath) || oathCard.KwValue(KeywordTable.Oath) != 1) return;

        // 第 3 回合（P1 的第二个回合）能量 = 1 + 2 × 1 = 3 ⇒ 只付 1 费后**还剩 2**（不触发钳位）。
        var ctx = Battle(new[] { oathCard }, new[] { Unit("FoeDummy", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        Check(ctx.Players[0].Energy, 3, "前提：P1 第 2 个回合能量 3（`startingMana 1 + TurnCount 2 × manaPerTurn 1`）");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "OathFixture"), 1), RuleCodes.OK,
                  "打出 1 费 + `oath 1` 的单位");
        // 🧨 把 `PayCardPlayedCostOath` 加回 `PlayCardCore`（原版不走那一路，见方法头）⇒ 本条红（能量会是 1）。
        Check(ctx.Players[0].Energy, 2, "🧨 手牌出牌**只扣牌费**：3 − 1（牌费）= **2**（⛔ 不再扣 `oath 1` —— "
              + "誓约费的原版唯一调用者是 `AbilityLogic__PlayAbility.c:2033` 的 `chosenAbility 360` 支）");

        // 对照：同费用、**不带** oath ⇒ 也剩 2 —— 与上面那条**必须相等**（手牌出牌压根不看 `oath`）。
        var plain = Unit("PlainFixture", 1, 1, 1);
        var ctxP = Battle(new[] { plain }, new[] { Unit("FoeDummy", 1, 0, 9) });
        ToP1Turn(ctxP, 2);
        Check(ctxP.Players[0].Energy, 3, "对照：同一时刻能量也是 3");
        CheckCode(RuleCore.PlayCard(ctxP, 0, HandIdx(ctxP, 0, "PlainFixture"), 1), RuleCodes.OK,
                  "对照：打出同费用但**不带** `oath` 的单位");
        Check(ctxP.Players[0].Energy, 2,
              "对照：只付 1 费 ⇒ 剩 **2**；⚠️ 与上面那条**相等**才是原版口径（原版手牌出牌不碰 `oath`）—— "
            + "谁把誓约费加回 `PlayCardCore`，上面那条会变 1 而本条仍是 2 ⇒ 红");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>按卡名在某一侧棋盘上找那个单位（我们的小格号不保证，扫全场最稳）。</summary>
    static UnitState B94F1UnitByName(BattleContext ctx, int p, string name)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u != null && u.Card != null && u.Card.Name == name) return u;
        }
        return null;
    }

    /// <summary>在墓地查询结果里按卡名找那一份实例。</summary>
    static CardInstance B94F1CemFind(List<CardInstance> list, string name)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].Card != null && list[i].Card.Name == name) return list[i];
        return null;
    }
}
