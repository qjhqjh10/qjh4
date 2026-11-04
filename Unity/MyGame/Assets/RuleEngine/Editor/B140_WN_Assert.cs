// B140_WN_Assert.cs — 批次 **B140** · 执行写手 `WN`（AI 评分参数 2 件）
//
// ============================ 它盯的是什么 ============================
//   A 组：**option 5（`ValueOfBuffingAttack`）不许有 `Exhausted` 闸** ——
//         「本回合已行动、但没眩晕」的单位**仍要计入** buff 攻击差值（池内 10 条 option 5）。
//   B 组：**`ValueOfEnchantments` 只数【非 extrinsic】的 `addStats`** ——
//         原版跳过 `extrinsic(+0x30) != 0` 的记录（光环 / whileInPlay 等），我们原来用
//         「运行时属性 − 卡面」把这些也算进去 ⇒ 每 1 点属性多 +0.5 分。
//
// ============================ 原版判据（现读 `d:/2/tools/decomp_full/`）====================
//   · `AI__ScoreFromCriteria.c` 的 `case 5`：循环体**只**有一道 `CardScript__get_mightAct` 闸，
//     然后两次 `AI__ScoreFromBestAttack`（带 buff / 不带）—— **没有** `Exhausted` / `canAct`。
//   · `BattleManager__MightAttackBuffedCard.c:10-27`：三闸 = `mightAct` ∧（`近战+Δ≥1` 或 `远程+Δ≥1`）
//     ∧ `cardState(+0x228)==2` —— 同样不读 `Exhausted`。
//   · `CardScript__get_mightAct.c:1-30`：只读 `attacksThisTurn(+0x48)` / `!displaySummonSickness` /
//     `!stun(100)` / `!cantAttack(150)` / `!nonCombatant(880)` ⇒ 「已行动」那一支走的是**攻击次数**，不是 `Exhausted`。
//   · `AI__ValueOfEnchantments.c:25-30`：`if (effect.extrinsic(+0x30) == 0) { … }` ⇒ **extrinsic 整条跳过**；
//     接着 `buffType(+0x38) == 1 (addStats)` ⇒ `(meleeAttackChange + rangeAttackChange + healthChange) × 0.5`
//     （🥈 交叉：`cpp2il/out_cpp2il_dev_ilspy/AI.cs:2416-2529` 逐字同形）。
//   · 我们这边的等价字段 = `UnitState.StatMod.Extrinsic`（原版 `CardEffect.extrinsic(+0x30)` 的投影，
//     见 `Core/UnitState.cs` 的 `StatMod.Extrinsic` doc）；`BuffType` 同表 `CardEffect.buffType(+0x38)`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2/A3：把 `Data/AiScoring.cs` 的 `BestAttackScore` 里**加回** `if (u.Exhausted || u.IsStunned) return 0f;`
//     ⇒ A2/A3 红（疲劳那一侧变 0、与对照不再相等）；**A4（眩晕仍 0）仍绿** ⇒ 两条必须同时成立，
//     说明删掉的是 `Exhausted` 那一闸、不是把整个候选表放开了。
//   · B1：把 `ValueOfEnchantments` 的 `m.Extrinsic` 跳过删掉（或改回「运行时 − 卡面」）
//     ⇒ B1 红（extrinsic +2 会算成 +1.0）；**B2/B3 仍绿**（非 extrinsic 那半照旧计入）⇒ 同上。
//   · B4：把 `m.IsSet` / `BuffType != 1` 两道过滤删掉 ⇒ B4 红（`setMeleeAttack` 档被当成 addStats 计分）。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B140WN_Assert", B140_WN_Assert.Run); _pass += B140_WN_Assert.Pass; _fail += B140_WN_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做，本写手不改那个文件。）
//   也可直接：`-executeMethod B140_WN_Assert.Run`（本类在**全局命名空间**、类名 == 文件名）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B140-WN 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B140_WN_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B140-WN：option 5 去 `Exhausted` 闸 / `ValueOfEnchantments` 跳 extrinsic");
        GroupA_Option5NoExhaustedGate();
        GroupB_ValueOfEnchantmentsSkipsExtrinsic();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static CardDef Hero(string id)
        => new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, (IEnumerable<string>)null);

    static CardDef Unit(string id, int cost, int atk, int hp)
        => new CardDef(id, id, "unit", "", "common", "Test", cost, atk, hp, 0, (IEnumerable<string>)null);

    static BattleContext NewCtx()
    {
        // 与 `RuleEngineTest.Deck(...)` 同形：督军 + 一批垫牌（**不让起手抽空牌库** —— 那会走疲劳分支、
        // 给督军发疲劳伤害，虽然不影响打分，但会让前提多一条噪声）。
        var d0 = new List<CardDef> { Hero("B140WNWarlord0") };
        var d1 = new List<CardDef> { Hero("B140WNWarlord1") };
        for (int i = 0; i < 10; i++)
        {
            d0.Add(Unit("B140WNFiller0_" + i, 1, 0, 1));
            d1.Add(Unit("B140WNFiller1_" + i, 1, 0, 1));
        }
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`（部署当回合不可行动）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>P0 槽 3 一个「被 buff 的单位」· P1 槽 3 一个「挨打的对象」，与 `RuleEngineTest.Duel` 同形。</summary>
    static BattleContext Duel(out UnitState ally, out UnitState foe)
    {
        var ctx = NewCtx();
        ally = Place(ctx, 0, 3, Unit("B140WN_Ally", 1, 2, 5));
        foe = Place(ctx, 1, 3, Unit("B140WN_Foe", 1, 0, 20));
        return ctx;
    }

    // ==================================================================
    //  A 组：option 5 只看 mightAct（原版 `AI__ScoreFromCriteria.c` case 5）
    // ==================================================================
    static void GroupA_Option5NoExhaustedGate()
    {
        Section("A option 5：已行动但未眩晕 ⇒ **仍计入**；眩晕 ⇒ 0");

        var e = new AiScoringEntryDto
        {
            scoringOption = 5,
            targetCriteria = new AiTargetCriteriaDto { targetsAffected = 30 },   // 30 = 这条动作选中的那个目标
            buff = new AiBuffDto { buffType = 1, meleeAttackChange = 2 },        // 近战 +2
            multiplyByFriend = 1f,
        };

        UnitState ctrl, foeC, tired, foeT, stun, foeS;
        var c1 = Duel(out ctrl, out foeC);
        var c2 = Duel(out tired, out foeT);
        var c3 = Duel(out stun, out foeS);

        // ---- 前提（⛔ 不写成 `if (前提) Check(结论)`：前提不成立必须红）----
        CheckTrue(!ctrl.Exhausted && !tired.Exhausted && !stun.Exhausted, "A0a 前提：三个单位构造时都没疲劳");
        tired.Exhausted = true;                                    // 本回合「已行动」（用过主动技等）
        stun.AddKeyword(KeywordTable.Stun, 1);                     // 眩晕
        CheckTrue(tired.Exhausted, "A0b 前提：探针单位 `Exhausted == true`");
        CheckTrue(!tired.IsStunned && !ctrl.IsStunned, "A0c 前提：对照组与疲劳组都**不眩晕**（疲劳 ≠ 眩晕）");
        CheckTrue(stun.IsStunned, "A0d 前提：眩晕组真的带 `stun(100)`");
        CheckTrue(AiTargets.MightAct(c1, new AiTargetRef { Unit = ctrl, Inst = ctrl.Instance }),
                  "A0e 前提：对照组 `mightAct` = 真（`CardScript__get_mightAct` 不读 `Exhausted`）");
        CheckTrue(AiTargets.MightAct(c2, new AiTargetRef { Unit = tired, Inst = tired.Instance }),
                  "A0f 前提（★关键）：**疲劳组 `mightAct` 也是真** —— 原版 `get_mightAct` 只看攻击次数 / 眩晕 / 召唤病 / 禁攻 / 非战斗员");
        CheckTrue(!AiTargets.MightAct(c3, new AiTargetRef { Unit = stun, Inst = stun.Instance }),
                  "A0g 前提：眩晕组 `mightAct` = 假（`HasCurrentTrait(stun 100)` 那一道）");
        Check(AiTargets.GetTargets(c2, e.targetCriteria, tired.Card, null, 0, tired).Count, 1,
              "A0h 前提：`targetsAffected=30` ⇒ 目标表就是这个单位（不跑筛选）");

        float sCtrl = AiScoring.ScoreFromCriteria(c1, ctrl.Card, null, 0, ctrl, e);
        float sTired = AiScoring.ScoreFromCriteria(c2, tired.Card, null, 0, tired, e);
        float sStun = AiScoring.ScoreFromCriteria(c3, stun.Card, null, 0, stun, e);

        // ---- 结论 ----
        CheckTrue(sCtrl > 0.01f,
                  "A1 对照（未行动）⇒ option 5 的 buff 差值真的进来（> 0；实际 = " + sCtrl + "）"
                + "｜🧨 改成「无条件 0」这里必红");
        CheckTrue(sTired > 0.01f,
                  "A2 ★★ 疲劳（已行动、未眩晕）⇒ **同样 > 0**（实际 = " + sTired + "）"
                + "｜🧨 把 `if (u.Exhausted || u.IsStunned) return 0f;` 加回 `BestAttackScore` ⇒ 这里变 0 ⇒ 红"
                + "（判据 `AI__ScoreFromCriteria.c` case 5 只有 `get_mightAct`；`BattleManager__MightAttackBuffedCard.c:10-27` 三闸也不读 `Exhausted`）");
        CheckFloat(sTired, sCtrl,
                   "A3 ★★【灭自证】疲劳组与对照组的**分必须相等**（同一局面、只差 `Exhausted` 一位）"
                 + "｜🧨 任何「把疲劳当成不能出手」的写法都会让两边不等 —— 与 A2 结构上不可能被同一个常量同时满足");
        CheckFloat(sStun, 0f,
                   "A4 ★ 眩晕 ⇒ 分 = 0（**由 `mightAct` 挡掉**，不是因为 `Exhausted`）"
                 + "｜🧨 把眩晕这一道也一起删掉（连 `mightAct` 闸）⇒ 这里变正 ⇒ 红");

        // 反方向：眩晕但**不**疲劳 —— 证明 A4 的分 0 来自眩晕而不是疲劳
        stun.Exhausted = false;
        CheckTrue(!stun.Exhausted && stun.IsStunned, "A5a 前提：眩晕组现在「不疲劳 ∧ 眩晕」");
        float sStun2 = AiScoring.ScoreFromCriteria(c3, stun.Card, null, 0, stun, e);
        CheckFloat(sStun2, 0f, "A5 ★ 不疲劳但眩晕 ⇒ 仍然 0（眩晕那一道独立于疲劳，判据 `get_mightAct` 的 stun(100)）");
    }

    // ==================================================================
    //  B 组：ValueOfEnchantments 跳过 extrinsic（原版 `AI__ValueOfEnchantments.c:25-30`）
    // ==================================================================
    static void GroupB_ValueOfEnchantmentsSkipsExtrinsic()
    {
        Section("B `ValueOfEnchantments`：extrinsic ⇒ 0；非 extrinsic ⇒ 计入；set 档 ⇒ 不数");

        // ---- ① 只有 extrinsic +2 近战（= 光环 / whileInPlay 那一族）----
        UnitState u1, foe1;
        Duel(out u1, out foe1);
        Check(u1.Attack, 2, "B0a 前提：卡面近战 = 2");
        u1.AddStatMod("attack", 2, "Aura:B140WN", extrinsic: true);
        Check(u1.Attack, 4, "B0b 前提：extrinsic +2 **真的进了运行时属性**（旧写法「运行时 − 卡面」= 2 ⇒ 会算 +1.0）");
        CheckFloat(AiScoring.ValueOfEnchantments(u1), 0f,
                   "B1 ★★ extrinsic 的 `addStats` **不计分**（实际；旧写法 = 1.0）"
                 + "｜🧨 把 `m.Extrinsic` 跳过删掉 / 改回「运行时 − 卡面」⇒ 这里变 1.0 ⇒ 红"
                 + "（判据 `AI__ValueOfEnchantments.c:25`：`extrinsic(+0x30) != 0` 整条跳过）");

        // ---- ② 同一张再挂一条**非** extrinsic +2（灭自证：只有非 extrinsic 那半计入）----
        u1.AddStatMod("attack", 2, null);                          // 默认 extrinsic:false
        Check(u1.Attack, 6, "B0c 前提：两条修饰都在运行时（2 + 2 + 2）");
        CheckFloat(AiScoring.ValueOfEnchantments(u1), 1f,
                   "B2 ★★ 只数**非 extrinsic** 那 2 点 ⇒ 2 × 0.5 = 1.0（旧写法 = (6−2) × 0.5 = 2.0）"
                 + "｜🧨 跳过开成「凡有 extrinsic 就整份 0」⇒ 这里变 0 ⇒ 红（与 B1 同时成立才算口径对）");

        // ---- ③ 只有非 extrinsic（三围各一档，含 health）----
        UnitState u2, foe2;
        Duel(out u2, out foe2);
        u2.AddStatMod("attack", 2);
        u2.AddStatMod("ranged", 2);
        u2.AddStatMod("health", 4);
        Check(u2.Attack, 4, "B0d 前提：非 extrinsic 近战 +2 生效");
        Check(u2.RangedAttack, 2, "B0e 前提：非 extrinsic 远程 +2 生效");
        CheckFloat(AiScoring.ValueOfEnchantments(u2), 4f,
                   "B3 ★ 非 extrinsic 的三围合计 = (2 + 2 + 4) × 0.5 = 4.0"
                 + "（原版 `meleeAttackChange + rangeAttackChange + healthChange` 三项同乘 0.5）");

        // ---- ④ set 档（`setMeleeAttack 70`）不是 `addStats` ⇒ 不数 ----
        UnitState u3, foe3;
        Duel(out u3, out foe3);
        u3.AddStatMod("attack", 9, null, isSet: true);              // = `buffType 70`
        Check(u3.Attack, 9, "B0f 前提：`setMeleeAttack(9)` 覆盖了运行时近战（2 → 9）");
        CheckFloat(AiScoring.ValueOfEnchantments(u3), 0f,
                   "B4 ★ `setMeleeAttack` 档不算附魔价值（原版只数 `buffType == 1 addStats`；旧写法会算 (9−2)×0.5 = 3.5）"
                 + "｜🧨 删掉 `m.IsSet || m.BuffType != 1` 这两道过滤 ⇒ 这里变 3.5 ⇒ 红");
    }

    // ==================================================================
    //  断言小工具（与 `B140_WA_Assert` 同形；不引 `RuleEngineTest` 的私有夹具）
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void CheckFloat(float actual, float expected, string msg)
    {
        bool ok = Math.Abs(actual - expected) <= 1e-4f;
        Report(ok, msg + "（实际 = " + actual.ToString("0.####") + "，期望 = " + expected.ToString("0.####") + "）");
    }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B140_WN 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B140_WN 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
