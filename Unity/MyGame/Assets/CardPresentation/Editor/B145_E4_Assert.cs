// B145_E4_Assert.cs — 批次 **B145** · 执行代理 `E4`（`A-07` TraitScore 补表 / `A-04` option 5 对照档过闸）
//
// ============================ 它盯的是什么 ============================
//   A 组 · `A-07`：`SimpleAI.TraitScore` 现在是**原版 `ScoreFromBuff` 的完整 trait 表**
//         （判据 `判据/AI打分与动作选择_第2轮.md` §6·6）。本体只留 15 条，其余 24 条（含动态取值那 6 个）
//         由 `default:` 转发到 `SimpleAI.TraitScoreExtra`（`Data/SimpleAI.TraitScore.cs`）。
//         本组死盯**三个非平凡值**（`pindown = −1.5` / `poisoned = −4.0` / `poisonous = 3.0`）
//         ＋ 两档「原版有、我们原来缺」的正值，＋ 动态取值档，＋ **反例对照**（表不是「一律非 0」）。
//   B 组 · `A-04`：`AiScoring.BestAttackScore` 的「不带 buff 对照档」（`meleeDelta == 0`）**也**要过
//         `MightAttackBuffedCard`。原版 case 5 对每个目标调**两次** `ScoreFromBestAttack`，
//         第二次的 buff 实参是**现构的空 buff 对象**（不是 null）⇒ 照样进 `GetPotentialBuffedAttackActions`
//         → `MightAttackBuffedCard(card, 空buff)` ⇒ 三闸按 Δ=0 判。
//
// ============================ 原版判据 ============================
//   · trait 表：`判据/AI打分与动作选择_第2轮.md` §6·6（`AI.cs:1807-1965` 实读）。
//     数值键的同源表 = `AiScoring.TraitAiScore`（`Data/AiScoring.cs:861-890`）。
//   · `A-04`：`decomp_full/AI__ScoreFromCriteria.c:181-204`（case 5 的两次调用；
//     第二次 `uVar8 = thunk_FUN_18042f4a0(DAT_1842d44f8)` + `ItemDrawerOptions___ctor(uVar8,0)`）
//     ＋ `AI__GetPotentialBuffedAttackActions.c:40`（`MightAttackBuffedCard(param_2,param_3,param_4,0)`）
//     ＋ `BattleManager__MightAttackBuffedCard.c:10-27`（三闸：mightAct ∧ 攻或远≥1 ∧ `+0x228 == 2`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2/A3：把对应分值改回 0（或改成别的数）⇒ 红。
//   · A6：把 `TraitScore` 的 `default:` 改回裸 `return 0.0f;`（= 切断转发）⇒ A1–A5 全红。
//   · A7：把 `TraitScoreExtra(string, UnitState)` 的 `bloodthirst` 支删掉 ⇒ 红。
//   · A8：把 `TraitScore(string)` 无单位档的 `bloodthirst` 猜成一个常数 ⇒ 红
//          （原版这一格读的是**单位的当前近战攻击力**，字符串入口拿不到 ⇒ 只能 0）。
//   · A9：把「没列出的就是 0 分」改成「一律给个正分」⇒ 红。
//   · B2：把 `AiScoring.BestAttackScore` 的闸改回 `meleeDelta != 0 && …`（= 对照档不过闸）⇒ 变负 ⇒ 红。
//   · B1：`state == 2` 的对照组必须 > 0 —— 挡住「干脆整条返回 0」这种「改法」。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//     `B145_E4_Assert.Run(); _pass += B145_E4_Assert.Pass; _fail += B145_E4_Assert.Fail;`
//   （本类在**全局命名空间**、类名 == 文件名 ⇒ 也可直接 `-executeMethod B145_E4_Assert.Run`。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B145-E4：`TraitScore` 完整 trait 表（A-07）· option 5 对照档过闸（A-04）。</summary>
public static class B145_E4_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("--- 🆕 B145-E4：TraitScore 补表（A-07）/ option5 对照档过闸（A-04）---");
        GroupA_TraitScoreFullTable();
        GroupB_BestAttackNoBuffPassesGate();
        Debug.Log("--- B145-E4 合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ============================================================ 夹具
    static CardDef Hero(string id)
        => new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, (IEnumerable<string>)null);

    static CardDef Unit(string id, int cost, int atk, int hp)
        => new CardDef(id, id, "unit", "", "common", "Test", cost, atk, hp, 0, (IEnumerable<string>)null);

    static BattleContext NewCtx()
    {
        // 与 `B140_WN_Assert.NewCtx` 同形：督军 + 一批垫牌（不让起手抽空牌库 ⇒ 少一条噪声前提）。
        var d0 = new List<CardDef> { Hero("B145E4Warlord0") };
        var d1 = new List<CardDef> { Hero("B145E4Warlord1") };
        for (int i = 0; i < 10; i++)
        {
            d0.Add(Unit("B145E4Filler0_" + i, 1, 0, 1));
            d1.Add(Unit("B145E4Filler1_" + i, 1, 0, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // `UnitState` ctor 恒 `Exhausted = true` ⇒ 显式写 false（同 `B140_WN_Assert.Place`）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>P0 槽 3 = 被 buff 的单位（近战 2）· P1 槽 3 = 挨打的对象（近战 0、血 20）—— 同 `B140_WN_Assert.Duel`。</summary>
    static BattleContext Duel(out UnitState ally, out UnitState foe)
    {
        var ctx = NewCtx();
        ally = Place(ctx, 0, 3, Unit("B145E4_Ally", 1, 2, 5));
        foe = Place(ctx, 1, 3, Unit("B145E4_Foe", 1, 0, 20));
        return ctx;
    }

    // ============================================================ A：TraitScore 完整表（A-07）
    static void GroupA_TraitScoreFullTable()
    {
        Section("A `TraitScore`：原版 §6·6 的完整 trait 表（A-07）");

        // ★ 三个非平凡值（简报点名的三个）
        CheckFloat(SimpleAI.TraitScore("pindown"), -1.5f,
                   "A1 ★★ `pindown` = **−1.5**（原版 `pindown(970)`；§6·6）"
                 + "｜🧨 改回 0 / 改成正数 ⇒ 红");
        CheckFloat(SimpleAI.TraitScore(KeywordTable.Poisoned), -4.0f,
                   "A2 ★★ `poisoned` = **−4.0**（原版 `poisoned(200)`；表格里最负的一格）"
                 + "｜🧨 改成 −1.5（和 stun/blind 混档）⇒ 红");
        CheckFloat(SimpleAI.TraitScore(SimpleAI.PoisonousKeyword), 3.0f,
                   "A3 ★★ `poisonous` = **3.0**（原版 `poisonous(380)`；与 `poisoned` 一正一负、⛔ 别合并）"
                 + "｜🧨 改成 0 或负 ⇒ 红");

        // ★ 两档「原版有、我们原来缺」的
        CheckFloat(SimpleAI.TraitScore(KeywordTable.Dodge), 2.0f,
                   "A4 ★ `dodge` = **2.0**（原版 `dodge(240)`，与 shield/vanguard 同档）");
        CheckFloat(SimpleAI.TraitScore(KeywordTable.DropPod), 0.5f,
                   "A5 ★ `droppod` = **0.5**（原版 `dropPod(230)`，与 berzerk 同档）");
        CheckFloat(SimpleAI.TraitScore(RuleCore.Unstoppable), 1.0f,
                   "A5b ★ `unstoppable` = **1.0**（原版 `unstoppable(180)`）");

        // ★ 动态取值档：带单位才有分
        var ctx = NewCtx();
        var u = Place(ctx, 0, 5, Unit("B145E4_BloodThirsty", 2, 3, 10));
        CheckTrue(u.Attack == 3, "A6a 前提：探针单位近战 = " + u.Attack + "（= 3）");
        CheckFloat(SimpleAI.TraitScoreExtra(KeywordTable.BloodThirst, u), 3f,
                   "A6 ★★ `bloodthirst` 动态档 = **单位当前近战攻击力**（原版 `t.CurrentMeleeAttack`）"
                 + "｜🧨 写成常数 ⇒ 红");
        CheckFloat(SimpleAI.TraitScore(KeywordTable.BloodThirst), 0f,
                   "A6b ★ `bloodthirst` **无单位上下文** ⇒ 0（字符串入口拿不到攻击力，⛔ 不许猜一个数）"
                 + "｜🧨 把一个常数填进 `TraitScore(string)` 那一档 ⇒ 红");

        // ★ 反例对照：表不是「一律非 0」
        CheckFloat(SimpleAI.TraitScore("fast"), 0f,
                   "A7 对照：`fast`（原版表里**没有**这一格）⇒ 0 —— 证明转发没有把「其余都变成正分」");
        CheckFloat(SimpleAI.TraitScore("__B145E4_NotAKeyword__"), 0f,
                   "A8 对照：不存在的关键词 ⇒ 0（⛔ 不许拿 `default` 兜一个值）");

        // ★ 本体那 15 条**没被改坏**（改 `default:` 时最容易连带弄坏的对照组）
        CheckFloat(SimpleAI.TraitScore(KeywordTable.Shield), 2.0f, "A9 对照：`shield` = 2.0（本体那 15 条之一）");
        CheckFloat(SimpleAI.TraitScore(KeywordTable.CantAttack), -2.0f, "A10 对照：`cantattack` = −2.0（本体那 15 条之一）");
        CheckFloat(SimpleAI.TraitScore("vulnerable"), -1.0f, "A11 对照：`vulnerable` = −1.0（本体那 15 条之一）");
    }

    // ============================================================ B：option 5 的对照档也过闸（A-04）
    static void GroupB_BestAttackNoBuffPassesGate()
    {
        Section("B option 5：`meleeDelta == 0` 的对照档**也**过 `MightAttackBuffedCard`（A-04）");

        var e = new AiScoringEntryDto
        {
            scoringOption = 5,
            targetCriteria = new AiTargetCriteriaDto { targetsAffected = 30 },   // 30 = 这条动作选中的那个目标
            buff = new AiBuffDto { buffType = 1, meleeAttackChange = 2 },        // 近战 +2 ⇒ meleeDelta != 0
            multiplyByFriend = 1f,
        };

        UnitState ally, foe, ally3, foe3;
        var c1 = Duel(out ally, out foe);
        var c2 = Duel(out ally3, out foe3);

        // ---- 前提（⛔ 不写 `if (前提) Check(结论)`）----
        CheckTrue(ally.Attack >= 1, "B0a 前提：对照组单位近战 = " + ally.Attack + "（≥1）");
        CheckTrue(ally.CardStateOf(c1) == CardStateOptions.inPlay, "B0b 前提：对照组 state = `inPlay(2)`");
        CheckTrue(AiTargets.MightAct(c1, new AiTargetRef { Unit = ally, Inst = ally.Instance }),
                  "B0c 前提：对照组 `mightAct` = 真（三闸里只有 state 这一闸会被 B2 打掉）");
        CheckTrue(AiTargets.GetTargets(c1, e.targetCriteria, ally.Card, null, 0, ally).Count == 1,
                  "B0d 前提：`targetsAffected=30` ⇒ 目标表就是这个单位（不跑筛选）");

        CheckTrue(ally3.MarkCardState(CardStateOptions.inPlayAttacking),
                  "B0e 前提：`MarkCardState(inPlayAttacking)` 成功");
        CheckTrue(ally3.CardStateOf(c2) == CardStateOptions.inPlayAttacking, "B0f 前提：此刻 state = 3");
        CheckTrue(AiTargets.MightAct(c2, new AiTargetRef { Unit = ally3, Inst = ally3.Instance }),
                  "B0g 前提（★关键）：state 3 的单位 `mightAct` **仍为真** —— 所以它被挡掉只可能是因为第三闸");

        float sCtrl = AiScoring.ScoreFromCriteria(c1, ally.Card, null, 0, ally, e);
        float sState3 = AiScoring.ScoreFromCriteria(c2, ally3.Card, null, 0, ally3, e);

        // ---- 结论 ----
        CheckTrue(sCtrl > 0.01f,
                  "B1 对照（state 2）⇒ option 5 的 buff 差值真的进来（> 0；实际 = " + sCtrl + "）"
                + "｜🧨 把 `BestAttackScore` 整条改成返回 0 ⇒ 这里必红");
        CheckFloat(sState3, 0f,
                   "B2 ★★ state 3（`inPlayAttacking`）⇒ **0**，不是负数"
                 + "｜🧨 把 `AiScoring.BestAttackScore` 的闸改回 `meleeDelta != 0 && …`（对照档不过闸）⇒ "
                 + "`withBuff` 被闸成 0、`noBuff` 仍算出一个正数 ⇒ 差值变**负** ⇒ 红"
                 + "（判据 `AI__ScoreFromCriteria.c:181-204` ＋ `AI__GetPotentialBuffedAttackActions.c:40`）");

        // 复位再测一次：证明 B2 的 0 来自 state 那一闸，不是「这个单位本来就没分」
        ally3.ClearCardStateMark();
        CheckTrue(ally3.CardStateOf(c2) == CardStateOptions.inPlay, "B3a 前提：`ClearCardStateMark()` 后回到 state 2");
        float sBack = AiScoring.ScoreFromCriteria(c2, ally3.Card, null, 0, ally3, e);
        CheckTrue(sBack > 0.01f,
                  "B3 ★ 复位回 state 2 ⇒ 分又变正（实际 = " + sBack + "）—— 证明 B2 的 0 不是「一律 0」");
        CheckFloat(sBack, sCtrl,
                   "B4 ★ 【灭自证】state 2 的两组（B1 / B3）**分数必须相等**（同一局面、只差一个瞬态标记）"
                 + "｜🧨 任何「按 state 给不同分」的写法都会让两边不等");
    }

    // ============================================================ 断言小工具（同 `B140_WN_Assert` / `B144_R2_AI_Assert`）
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
        if (ok) { _pass++; Debug.Log("[B145E4] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B145E4] ✗ " + msg); }
    }
}
