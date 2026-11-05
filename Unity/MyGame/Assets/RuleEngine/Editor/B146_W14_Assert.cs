// B146_W14_Assert.cs — 批次 **B146** · 执行写手 `W14`（S1 分支跳过点账「攻击链三缺口」）
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与 `B142_E2_Assert.cs` 同一种形状）
// —— `RuleEngine/Editor/RuleEngineTest.cs` 本体**本批不在白名单**，测试体全写在这里，宿主只需在 `Run()` 里加一行（见下）。
//
// ============================ 它盯的是什么 ============================
//  ① **G-2**：守方被打之后**失去 `LongRange`**（原版 `_ResolveAttack_d__438__MoveNext.c:608-615`：
//     `HasCurrentTrait(被打者, 0x488)` ⇒ `RemoveTrait(0x488)` + `RemoveBuffedTrait(0x488)`；
//     规则书中文 `:191` 「被攻击后失去此关键词」）。我们改前**全仓 0 处**。
//  ② **G-1**：`tainted` 摘的是**攻方**（原版 `:511-519` 读 `+0x28` = `battleAction.actingCard`；
//     `out_il2csharp/BattleManager.cs:33432` 明写 `battleAction.actingCard`）。我们改前传 `target`（被打者）。
//  ③ **G-7**：迷彩/隐身是**整摘**（`:601-607` = `RemoveTrait` + `RemoveBuffedTrait`；
//     `:429` = `RemoveTraitAndEffects(0x46)`），改前用 `RemoveKeyword`（**只减一层**）。
//
// ============================ 宿主接线（调度台做）============================
//   `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 聚合行**同一物理行追加**（落在该行第一个 `//` 之前）：
//       Step(CheckB146W14AttackChainTraitRemoval);
//   （方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 判红。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · G2-1：把 `DeclareAttackExecute` 里 `target.RemoveAll(KeywordTable.LongRange)` 那一句删掉 ⇒ 红。
//   · G2-2：把那一句改成 `attacker.RemoveAll(...)`（摘错人）⇒ 红。
//   · G1-1：把 `RemoveDuplicitousTrait(ctx, attacker)` 改回 `(ctx, target)` ⇒ 红（两条一起红）。
//   · G7-1/G7-2：把 `RemoveAll` 退回 `RemoveKeyword`（值 = 2 时只减 1 层）⇒ 红。
//   每组都**先写前提断言、再写结论断言**（前提不成立时不许静默跳过）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里那一行调用）。</summary>
    public static void CheckB146W14AttackChainTraitRemoval()
    {
        Section("🆕 B146_W14：攻击链三缺口（守方 LongRange 摘除 · tainted 摘攻方 · 迷彩/隐身整摘）");
        W14_G2();
        W14_G1();
        W14_G7();
    }

    // ==========================================================================================
    //  G-2 · 守方 `LongRange`：被打一次之后就没了（原版 `:608-615`）
    // ==========================================================================================
    static void W14_G2()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        // 攻方**自己也带** `longrange` —— 用来钉「原版只摘被打者(+0x30)，不摘攻方(+0x28)」。
        var atk = Place(ctx, 0, 3, Unit("W14G2Atk", 1, 3, 9, KeywordTable.LongRange), exhausted: false);
        var tgt = Place(ctx, 1, 3, Unit("W14G2Tgt", 1, 0, 20, KeywordTable.LongRange), exhausted: true);
        CheckTrue(atk.Has(KeywordTable.LongRange), "★（前提）攻方自己带 `longrange`");
        CheckTrue(tgt.Has(KeywordTable.LongRange), "★（前提）守方带 `longrange`（池内 13 张的那一档）");
        int hp = tgt.Health;

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "近战打一个带 `longrange` 的守方");

        Check(hp - tgt.Health, 3,
            "★（前提）这一记**真的结算了**（守方掉 3 = 攻方 3 攻）" + LogTail(ctx));
        CheckTrue(!tgt.Has(KeywordTable.LongRange),
            "★ 守方被打之后**失去 `longrange`**（原版 `:609-614`：`RemoveTrait(0x488)` + `RemoveBuffedTrait(0x488)`）"
            + "｜🧨 删掉 G-2 那一句 ⇒ 本行红");
        Check(tgt.KwValue(KeywordTable.LongRange), 0, "★ 值也归 0（⛔ 不是「剩 0 层的空壳」）");
        CheckTrue(atk.Has(KeywordTable.LongRange),
            "★ 反例：**攻方自己的 `longrange` 不动** —— 原版只摘被打者（`+0x30`）"
            + "｜🧨 摘错人（摘攻方）⇒ 本行红");
    }

    // ==========================================================================================
    //  G-1 · `tainted` 摘的是**攻方**（原版 `:511-519` 读 `+0x28` = `actingCard`）
    // ==========================================================================================
    static void W14_G1()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var atk = Place(ctx, 0, 3, Unit("W14G1Atk", 1, 2, 9, ConditionEval.Tainted), exhausted: false);
        var tgt = Place(ctx, 1, 3, Unit("W14G1Tgt", 1, 0, 20, ConditionEval.Tainted), exhausted: true);
        CheckTrue(atk.Has(ConditionEval.Tainted), "★（前提）攻方带 `tainted`");
        CheckTrue(tgt.Has(ConditionEval.Tainted), "★（前提）守方**也**带 `tainted`（这样摘错人才看得出来）");

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "近战打一个也带 `tainted` 的守方");

        CheckTrue(!atk.Has(ConditionEval.Tainted),
            "★ **攻方**的 `tainted` 被摘（原版 `:512` 读 `+0x28` = `battleAction.actingCard`）"
            + "｜🧨 实参改回 `target` ⇒ 本行红" + LogTail(ctx));
        CheckTrue(tgt.Has(ConditionEval.Tainted),
            "★ 反例：**被打者的 `tainted` 不动**（原版那一段从不碰 `+0x30`）"
            + "｜🧨 实参写成 `target`（= 改前的错法）⇒ 本行红");
    }

    // ==========================================================================================
    //  G-7 · 迷彩 / 隐身：**整摘**（值叠到 2 层时，`RemoveKeyword` 只减一层 ⇒ 本组会红）
    // ==========================================================================================
    static void W14_G7()
    {
        // ---- A · 伪装叠到 2 层（原版 `:601-607`）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("W14G7Cam", 1, 2, 9, ConditionEval.Camouflage), exhausted: false);
            atk.AddKeyword(ConditionEval.Camouflage, 1);          // 卡面 1 层 + 授予 1 层 = 2
            Check(atk.KwValue(ConditionEval.Camouflage), 2, "★（前提）伪装叠到 **2 层**");
            Place(ctx, 1, 3, Unit("W14G7CamTgt", 1, 0, 20), exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "带 **2 层**伪装的单位攻击");

            CheckTrue(!atk.Has(ConditionEval.Camouflage),
                "★ 伪装**整摘**（原版 `:601-607`：`RemoveTrait` + `RemoveBuffedTrait`）"
                + "｜🧨 退回 `RemoveKeyword`（只减一层）⇒ 本行红");
            Check(atk.KwValue(ConditionEval.Camouflage), 0, "★ 值归 0（⛔ 不是剩下 1 层）");
        }

        // ---- B · 隐身叠到 2 层（原版 `:429` `RemoveTraitAndEffects(0x46)`）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("W14G7Stl", 1, 2, 9, KeywordTable.Stealth), exhausted: false);
            atk.AddKeyword(KeywordTable.Stealth, 1);              // 卡面 1 层 + 授予 1 层 = 2
            Check(atk.KwValue(KeywordTable.Stealth), 2, "★（前提）隐身叠到 **2 层**");
            Place(ctx, 1, 3, Unit("W14G7StlTgt", 1, 0, 20), exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "带 **2 层**隐身的单位攻击");

            CheckTrue(!atk.Has(KeywordTable.Stealth),
                "★ 隐身**整摘**（原版 `:429` `RemoveTraitAndEffects(0x46)`）"
                + "｜🧨 退回 `RemoveKeyword`（只减一层）⇒ 本行红");
            Check(atk.KwValue(KeywordTable.Stealth), 0, "★ 值归 0（⛔ 不是剩下 1 层）");
        }
    }
}
