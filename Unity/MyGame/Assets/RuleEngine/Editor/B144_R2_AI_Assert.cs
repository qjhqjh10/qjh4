// B144_R2_AI_Assert.cs — 批次 **B144** · 执行代理 `R2`（AI 审计剩余三条）
//
// ============================ 它盯的是什么 ============================
//   A 组：**`ScorePoisoning` 读 `stunnedAtStartOfTurn(+0x55)`，不是 `IsStunned`**
//         （原版 `AI__ScoreFromPoisoning.c`：`+0x55 != 0 ⇒ 0.8`；否则 cantAttack ⇒ 0.5、能攻击 ⇒ 血<3 ? 0.2 : 0.5）。
//   B 组：**`TraitScore` 的 `camouflage` / `longrange` = 1.0**（原版 `ScoreFromBuff` trait 表，R6 判据 §6·6；原写 1.5）。
//   C 组：**`MightAttackBuffedCard` 的第三闸 = `cardState(+0x228) == 2`**
//         （原版 `BattleManager__MightAttackBuffedCard.c:22-24`；⛔ 不能假设「棋盘上 ⇒ 恒 2」——
//           `CardStateOf` 在 `waitingToDie(5)` / `inPlayAttacking(3)` / `inPlayTransforming(15)` / `inPlayAttacking(17)` 时都不是 2）。
//   D 组：只做登记 —— option 5 的 `Exhausted` 闸与 `ValueOfEnchantments` 跳 extrinsic 已由
//         `B140_WN_Assert` 的 A/B 两组覆盖（本文件不重复；见 `Data/AiScoring.cs:546` / `:482-485`）。
//
// ============================ 原版判据 ============================
//   · `decomp_full/AI__ScoreFromPoisoning.c`（全文）：`*(char*)(target+0x55) == 0` 才走
//     cantAttack / health<3 两档；`0x55 != 0` ⇒ 常数 `DAT_1834b2fa0`（0.8）。
//   · `decomp_full/BattleManager__MightAttackBuffedCard.c:10-27`：
//     `mightAct` →（`CurrentMeleeAttack + buff+0x48 >= 1` 或 `CurrentRangeAttack + buff+0x50 >= 1`）
//     → `*(int*)(card+0x228) == 2` ⇒ true；否则 false。
//   · trait 表：`判据/AI打分与动作选择_第2轮.md` §6·6（`camouflage` / `longrange` = **1.0**）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2：把 `ScorePoisoning` 改回 `target.IsStunned` ⇒ A1/A2 都变 0.8 倍 ⇒ 与期望的 0.5/0.2 倍不等 ⇒ 红。
//   · B1/B2：把 `TraitScore` 的 1.0 改回 1.5 ⇒ 红。
//   · C2：把 `MightAttackBuffedCard` 的 `CardStateOf(ctx) == inPlay` 删掉 ⇒ state 3 也返回 true ⇒ 红。
//   · C3：`ClearCardStateMark` 后仍 false ⇒ 红（证明不是「一律 false」）。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//     `B144_R2_AI_Assert.Run(); _pass += B144_R2_AI_Assert.Pass; _fail += B144_R2_AI_Assert.Fail;`
//   （本类在**全局命名空间**、类名 == 文件名 ⇒ 可直接 `-executeMethod B144_R2_AI_Assert.Run`。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B144-R2：`ScorePoisoning` 眩晕字段 / `TraitScore` 两值 / `MightAttackBuffedCard` 第三闸。</summary>
public static class B144_R2_AI_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("--- 🆕 B144-R2：AI 审计剩余三条（poison 字段 / trait 两值 / option5 第三闸）---");
        GroupA_ScorePoisoningStunnedAtStartOfTurn();
        GroupB_TraitScoreTwoValues();
        GroupC_MightAttackBuffedCardCardStateGate();
        Debug.Log("--- B144-R2 合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ============================================================ 夹具
    static CardDef Hero(string id)
        => new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, (IEnumerable<string>)null);

    static CardDef Unit(string id, int cost, int atk, int hp)
        => new CardDef(id, id, "unit", "", "common", "Test", cost, atk, hp, 0, (IEnumerable<string>)null);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B144R2Hero0") };
        var d1 = new List<CardDef> { Hero("B144R2Hero1") };
        for (int i = 0; i < 10; i++)
        {
            d0.Add(Unit("B144R2Filler0_" + i, 1, 0, 1));
            d1.Add(Unit("B144R2Filler1_" + i, 1, 0, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // `UnitState` ctor 恒 `Exhausted = true` ⇒ 显式写 false（与 `B140_WN_Assert.Place` 同形）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ============================================================ A：ScorePoisoning 的眩晕字段
    static void GroupA_ScorePoisoningStunnedAtStartOfTurn()
    {
        Section("A `ScorePoisoning`：读 `StunnedAtStartOfTurn(+0x55)`，不是 `IsStunned`");

        var ctx = NewCtx();
        var u = Place(ctx, 0, 3, Unit("B144R2_PoisonTarget", 2, 1, 5));
        u.AddKeyword(KeywordTable.Stun, 1);                 // 当前「被眩晕」（IsStunned = true）
        u.StunnedAtStartOfTurn = false;                     // 但**不是**回合开始时就在眩晕

        // 前提（⛔ 不写成 if (前提) Check(结论)：前提不成立必须红）
        CheckTrue(u.IsStunned, "A0a 前提：目标的 stun(100) 关键词在（IsStunned = 真）");
        CheckTrue(!u.StunnedAtStartOfTurn, "A0b 前提：`StunnedAtStartOfTurn` = 假（本回合中途被晕的那一档）");

        float v = SimpleAI.ValueInPlay(u);
        CheckTrue(v > 0.01f, "A0c 前提：ValueInPlay > 0（实际 = " + v + "）");

        // 结论 1：未在回合开始眩晕 + 血 >= 3 ⇒ 0.5（**不是** 0.8）
        CheckFloat(SimpleAI.ScorePoisoning(u), v * 0.5f,
                   "A1 ★★ 本回合中途被眩晕（`StunnedAtStartOfTurn=false`, 血 5）⇒ 系数 **0.5**"
                 + "｜🧨 改回 `target.IsStunned` ⇒ 变 0.8 ⇒ 红（判据 `AI__ScoreFromPoisoning.c` 的 `+0x55`）");

        // 结论 2：回合开始就眩晕 ⇒ 0.8
        u.StunnedAtStartOfTurn = true;
        CheckFloat(SimpleAI.ScorePoisoning(u), v * 0.8f,
                   "A2 ★ `StunnedAtStartOfTurn=true` ⇒ 系数 **0.8**（原版 0x1834b2fa0）"
                 + "｜🧨 把字段读成别的 / 整条删掉 ⇒ 这里与 A1 无法同时成立");

        // 结论 3：未开始眩晕 + 血 < 3 ⇒ 0.2
        u.StunnedAtStartOfTurn = false;
        u.Health = 2;
        float v2 = SimpleAI.ValueInPlay(u);
        CheckFloat(SimpleAI.ScorePoisoning(u), v2 * 0.2f,
                   "A3 ★ 血 2（<3）且不是回合开始眩晕 ⇒ 系数 **0.2**（原版 `DAT_1834b2bb0`）");

        // 结论 4：未眩晕 + cantAttack ⇒ 0.5
        u.Health = 5; u.StunnedAtStartOfTurn = false;
        u.RemoveKeyword(KeywordTable.Stun);
        u.AddKeyword(KeywordTable.CantAttack, 1);
        CheckFloat(SimpleAI.ScorePoisoning(u), SimpleAI.ValueInPlay(u) * 0.5f,
                   "A4 ★ 未眩晕 + cantAttack ⇒ 系数 **0.5**（原版 cantAttack 那支）");
    }

    // ============================================================ B：TraitScore 两值
    static void GroupB_TraitScoreTwoValues()
    {
        Section("B `TraitScore`：camouflage / longrange = 1.0（原版 trait 表）");
        CheckFloat(SimpleAI.TraitScore("camouflage"), 1.0f,
                   "B1 ★ `camouflage` = **1.0**（R6 §6·6；原写 1.5）｜🧨 改回 1.5 ⇒ 红");
        CheckFloat(SimpleAI.TraitScore(KeywordTable.LongRange), 1.0f,
                   "B2 ★ `longrange` = **1.0**（同上）｜🧨 改回 1.5 ⇒ 红");
        // 对照（证明这张表不是「一律 1.0」）
        CheckFloat(SimpleAI.TraitScore(KeywordTable.Shield), 2.0f, "B3 对照：`shield` = 2.0（原版表）");
        CheckFloat(SimpleAI.TraitScore(KeywordTable.CantAttack), -2.0f, "B4 对照：`cantAttack` = −2.0（原版表）");
    }

    // ============================================================ C：MightAttackBuffedCard 第三闸
    static void GroupC_MightAttackBuffedCardCardStateGate()
    {
        Section("C `MightAttackBuffedCard`：`cardState(+0x228) == 2` 是第三闸");
        var ctx = NewCtx();
        var u = Place(ctx, 0, 3, Unit("B144R2_Buffed", 2, 2, 10));
        var t = new AiTargetRef { Unit = u, Inst = u.Instance, Side = 0 };

        CheckTrue(u.Attack >= 1, "C0a 前提：近战 = " + u.Attack + "（+Δ 1 ⇒ ≥1）");
        CheckTrue(AiTargets.MightAct(ctx, t), "C0b 前提：`mightAct` = 真（新鲜单位：没攻击过 / 没眩晕 / 没禁攻）");
        CheckTrue(u.CardStateOf(ctx) == CardStateOptions.inPlay, "C0c 前提：默认 state = `inPlay(2)`");

        CheckTrue(SimpleAI.MightAttackBuffedCard(ctx, t, 1, 0),
                  "C1 ★ state 2 ⇒ 过闸（原版第三闸 == 2）");

        CheckTrue(u.MarkCardState(CardStateOptions.inPlayAttacking), "C1b 前提：`MarkCardState(inPlayAttacking)` 成功");
        CheckTrue(u.CardStateOf(ctx) == CardStateOptions.inPlayAttacking, "C1c 前提：此刻 state = 3");
        CheckTrue(!SimpleAI.MightAttackBuffedCard(ctx, t, 1, 0),
                  "C2 ★★ state 3（攻击瞬态）⇒ **不过闸**（原版 `+0x228 == 2` 严格等于；"
                + "⛔ 不能拿 `IsInPlayState` 那五档顶）｜🧨 删掉 `CardStateOf(ctx) == inPlay` ⇒ 这里变 true ⇒ 红");

        u.ClearCardStateMark();
        CheckTrue(SimpleAI.MightAttackBuffedCard(ctx, t, 1, 0),
                  "C3 ★ 复位回 state 2 ⇒ 又过闸（证明 C2 不是「一律 false」）");

        // 第四档：攻击力不足（近战和远程都 <1）⇒ 不过闸（对照，原版第二闸）
        var u2 = Place(ctx, 0, 5, Unit("B144R2_NoAtk", 2, 0, 10));
        var t2 = new AiTargetRef { Unit = u2, Inst = u2.Instance, Side = 0 };
        CheckTrue(AiTargets.MightAct(ctx, t2), "C4a 前提：攻 0 的单位 `mightAct` 仍为真（不靠 cantAttack 挡）");
        CheckTrue(!SimpleAI.MightAttackBuffedCard(ctx, t2, 0, 0), "C4 对照：攻 0 / 远程 0 ⇒ 不过闸（原版第二闸：近战+Δ 与 远程+Δ 都 <1）");
    }

    // ============================================================ 断言小工具（同 `B140_WN_Assert`）
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void CheckFloat(float actual, float expected, string msg)
    {
        bool ok = Math.Abs(actual - expected) <= 1e-4f * Math.Max(1f, Math.Abs(expected));
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B144R2AI] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B144R2AI] ✗ " + msg); }
    }
}
