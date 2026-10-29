// AiScoring.cs — 原版 `AI.ScoreFromPlayingNow` / `AI.ScoreFromCriteria` /
//                `AI.ValueOfEnchantments` 的移植（T06 / A1797 / A2144）
//
// **判据（第一权威，逐条现读）**
//   · `d:/2/tools/decomp_full/AI__ScoreFromPlayingNow.c`（4 条路径）
//   · **`GameAssembly.dll` RVA 0x93A290 的指令流**（`AI.ScoreFromCriteria`；Ghidra 的 `.c`
//     在函数中途被截断，**以反汇编为准**）—— 逐 case 读出来的：
//       - `:0x93A48C-0x93A4BD` base = `target!=null && target.side!=this.side ? baseValueEnemy : baseValueFriend`
//       - `:0x93A4BD-0x93A51D` multiplierCount = `multiplierCriteria.targetsAffected==0 ? 1 : GetTargets(...).Count`
//       - `:0x93A58D-0x93A651` option 1（Count）
//       - `:0x93A656-0x93A6E8` option 2（SumValueInPlay）
//       - `:0x93A6ED-0x93A818` option 3（SumValueOfDamage；DamageCriteria.GetAverageDamage → ScoreFromDamagingUnit）
//       - `:0x93A81D-0x93A8ED` option 4（SumValueOfHealing）
//       - `:0x93AA73-0x93AB0B` option 6（CurrentAttack）
//       - `:0x18093ABD7` / `:0x18093AC09` option 9 / 10
//       - `:0x18093AD1A` option 14（ScoreFromStun）· `:0x18093AE47` option 25（ScoreFromReturnToHand）
//       - `:0x18093AF8F-0x18093B0B0` option 35（ValueOfDrawingCard；**牌库空 = −2 − 疲劳**，
//         不是 `−2 − 手牌数` —— ⚠️ 这一条 **就地订正了 `A2144` 的原写法**）
//       - `:0x18093ACC7` option 12 / `:0x18093AD01` option 13（**整表一次调用，不逐目标**）
//   · `AI__ScoreFromStun`（RVA 0x93C6E0）· `AI__ScoreFromReturnToHand`（RVA 0x93C600）
//   · `AI__ValueOfEnchantments`（RVA 0x93D3B0）· `AI__GetCardValueInPlay`（RVA 0x939030）
//
// 🔴 **本文件只做「出牌 + 主动技」两条主链**：
//   · 出牌：`ScoreFromPlayingCard = 单位基础值 + ScoreFromPlayingNow`；
//   · 主动技 / 誓约 / 替代行动：`ScoreActiveAbility`（原版 `GetActionScore` case 5）。
//
// ⚠️ **已实现 / 未实现（如实标注）**：
//   ✅ option 0 / 1 / 2 / 3 / 4 / 5 / 6 / 9 / 10 / 12 / 13 / 14 / 25 / 35 —— 池内 577 条**全覆盖**
//   ✅ **2026-10-25（第八十会话 · `R11` · `R6-01`）：7 / 8 / 11 / 20 / 30 / 40 六支已补齐** ——
//      判据 = `判据/AI打分与动作选择_第2轮.md` §1·5 的 RVA；池内 0 条用这六支（现数：
//      `ai_scoring.json` 的 `scoringOption` 只有 0/1/2/3/4/5/6/12/13/14/25/35）⇒ **行为零变化**，
//      铁律 11 仍要做。⚠️ option 7 的 `ValueOfTraits` 因缺 `_aIScore` 数据只能算 0（见 `ScoreFromJam`）。
//      （2026-10-19 续工补上 option 5 = `ScoreFromBestAttack`、option 12 = `ScoreFromHandBuff`；
//       ⚠️ option 13 只接「手牌费用」那一半，原版后半段数「主动技 manaCost」的那一支未接，
//       我们的 `EffectSpec` 上没有主动技费用字段）。
//   ✅ `ValueOfEnchantments` 的 enchantment +1 / SecretOrder −2 已接 E3 的
//      `CardInstance.ActiveEffects`（`Core/Enchantment.cs`）；`addStats` 仍是「运行时属性 − 卡面」。
//   ⚠️ option 5 的 `extraScoreFromAttacking/TargetingOnAttack`（真值 0 非零张）与 poisonous 追加分未做；
//      option 12 的 `ScoreFromHandBuff`：**原版内部确实把 `targetCard = null` 传下去**
//      （`判据` §8 `R6-09`；我方 `HandBuffValue` 传的是 `slot=-1 / targetSide=-1` ⇒ 与之一致）。
//      ⚠️ 2026-10-25（`R11` · 铁律 5）订正：这一句原来写「原版会把动作目标传进去」，**已过期**。
//   ✅ **`DamageCriteria/HealCriteria.valueOptions` 已按反编译逐支对齐**（2026-10-20）：`80` ⇒
//      `min + max × multiplierCount`；`90/204` 与 `0` 同为 default（银行家舍入）—— 原版本来就是同一支
//      （见 `AverageValue` 的注释）。池内非 0 的只有 `GSC59`/`BL53`/`SOR14` 三条。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    public static class AiScoring
    {
        // ==================================================================
        //  🔬 只给离线探针用的**读数钩子**（🆕 2026-10-25 · `R11` 第二段）
        //
        //  为什么需要：`工具/ruleprobe play all dump` 是**结算层**读数，结构上看不见 AI 打分
        //  （它没有 AI 决策列）⇒ 每一笔 AI 改动都只能给「解析式读数」。
        //  这个钩子供 `工具/ruleprobe` 的 `ai <卡id>` 子命令把**每条 criteria 各贡献多少**打出来。
        //
        //  🔴 **产品路径恒 `null`（默认值）** ⇒ 只多一次 null 判断，行为零变化。
        //     ⛔ **别在游戏代码里给它赋值**（表现层 / 自检都不该依赖它）；
        //     `RuleEngineTest` 那一批断言走的是「直接调 `ScoreFromCriteria` 取返回值」，不碰这里。
        // ==================================================================
        public static Action<string> TraceCriteria;

        static void Trace(string line)
        {
            var t = TraceCriteria;
            if (t != null) t(line);
        }

        // ==================================================================
        //  出牌主链
        // ==================================================================

        /// <summary>
        /// 原版 `AI.ScoreFromPlayingNow(manager, ai, card, targetCard)`：
        /// **先**跑 `cardTargetScoringCriteria`（仅 `targeted=1` 且带目标），**再**跑
        /// `nonTargetScoringCriteria`（所有卡都跑，targeted 卡也跑）。
        /// ⚠️ **两列都空 ⇒ 0，没有兜底**（原版没有第二层）。
        /// </summary>
        /// <param name="targetSide">这条动作的目标在哪一方（`-1` = 没有目标）。</param>
        public static float ScoreFromPlayingNow(BattleContext ctx, CardDef card, CardInstance inst,
                                                int slot, int targetSide)
        {
            if (ctx == null || card == null) return 0f;
            var dto = AiScoringData.Get(card);
            if (dto == null) return 0f;

            int thisSide = ctx.Active;
            UnitState target = TargetUnit(ctx, slot, targetSide);

            float sum = 0f;

            // ---- cardTargetScoringCriteria：`targeted && targetCard != null` ----
            if (dto.targeted != 0 && target != null && dto.cardTargetScoringCriteria != null)
            {
                for (int i = 0; i < dto.cardTargetScoringCriteria.Length; i++)
                {
                    var e = dto.cardTargetScoringCriteria[i];
                    if (e == null) continue;
                    // 原版先 `FilterMethods.CheckIfMeetsCriteria(card, target, target, e.targetCriteria)`
                    if (!AiTargets.MatchesTarget(ctx, e.targetCriteria, card, inst, thisSide, target)) continue;
                    float v = ScoreFromCriteria(ctx, card, inst, thisSide, target, e);
                    sum += v;
                    Trace("cardTarget[" + i + "] option=" + e.scoringOption + " ⇒ " + v.ToString("0.###"));
                }
            }

            // ---- nonTargetScoringCriteria：**所有卡都跑** ----
            if (dto.nonTargetScoringCriteria != null)
            {
                for (int i = 0; i < dto.nonTargetScoringCriteria.Length; i++)
                {
                    var e = dto.nonTargetScoringCriteria[i];
                    if (e == null) continue;
                    float v = ScoreFromCriteria(ctx, card, inst, thisSide, target, e);
                    sum += v;
                    Trace("nonTarget[" + i + "] option=" + e.scoringOption + " ⇒ " + v.ToString("0.###"));
                }
            }
            return sum;
        }

        static UnitState TargetUnit(BattleContext ctx, int slot, int targetSide)
        {
            if (targetSide < 0 || targetSide > 1 || !BoardSpec.IsValid(slot)) return null;
            return ctx.Players[targetSide].Board[slot];
        }

        // ==================================================================
        //  ScoreFromCriteria（RVA 0x93A290）
        // ==================================================================

        /// <summary>
        /// 一条 `ScoringCriteria` 值多少分（原版 `AI.ScoreFromCriteria`）。
        /// **`baseValue` 恒先加上**（原版反汇编里它进 xmm7 之后没有任何「目标为空就跳过」的闸）。
        /// </summary>
        public static float ScoreFromCriteria(BattleContext ctx, CardDef card, CardInstance inst,
                                              int thisSide, UnitState target, AiScoringEntryDto e)
        {
            if (ctx == null || card == null || e == null) return 0f;

            // ---- base（`:0x93A48C-0x93A4BD`）----
            int targetSide = SideOf(ctx, target);
            float baseValue = (target != null && targetSide != thisSide)
                            ? e.baseValueEnemy : e.baseValueFriend;

            // ---- multiplierCount（`:0x93A4BD-0x93A51D`）----
            int mult = 1;
            if (e.multiplierCriteria != null && e.multiplierCriteria.targetsAffected != 0)
            {
                mult = AiTargets.GetTargets(ctx, e.multiplierCriteria, card, inst, thisSide, target).Count;
            }

            var targets = AiTargets.GetTargets(ctx, e.targetCriteria, card, inst, thisSide, target);

            switch (e.scoringOption)
            {
                case 0:     // FixedValue（含 15-19/21-24/26-34 落这一支）
                    return baseValue;

                case 1:     // Count（`:0x93A58D-0x93A651`）
                {
                    int friends = 0, enemies = 0;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (targets[i].Side == thisSide) friends++;
                        else enemies++;
                    }
                    return baseValue + (friends * e.multiplyByFriend + enemies * e.multiplyByEnemy) * mult;
                }

                case 2:     // SumValueInPlay（`:0x93A656-0x93A6E8`）
                {
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                        sum += ValueInPlay(targets[i]) * SideMul(targets[i].Side, thisSide, e) * mult;
                    return sum;
                }

                case 3:     // SumValueOfDamage（`:0x93A6ED-0x93A818`）
                {
                    float sum = baseValue;
                    if (e.damage == null) return sum;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        // 🔴 原版把 **multiplierCount** 也传给 `DamageCriteria.GetAverageDamage`
                        //    （`AI__ScoreFromCriteria.c:144` 第 4 个实参 = `uVar11` = multiplierCount），
                        //    `valueOptions=80`（MinPlusMaxTimesMultiplier）就是靠它算成 `min + max×mult`。
                        //    ⇒ 见 `AverageValue(valueOptions, …)` 的注释（2026-10-20 订正，不再是近似）。
                        int dmg = AverageValue(e.damage.valueOptions,
                                               e.damage.minDamage, e.damage.maxDamage, mult);
                        if (dmg <= 0) continue;
                        sum += SimpleAI.ScoreDamaging(u, dmg) * SideMul(targets[i].Side, thisSide, e);
                    }
                    return sum;
                }

                case 4:     // SumValueOfHealing（`:0x93A81D-0x93A8ED`）
                {
                    float sum = baseValue;
                    if (e.heal == null) return sum;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        // 同 case 3：原版 `HealCriteria.GetAverageValue` 也吃 multiplierCount
                        //（`AI__ScoreFromCriteria.c:167` 第 4 个实参）。池内 heal 的 `valueOptions` 全是 0。
                        int heal = AverageValue(e.heal.valueOptions,
                                                e.heal.minValue, e.heal.maxValue, mult);
                        if (heal <= 0) continue;
                        sum += SimpleAI.ScoreHealing(u, heal) * SideMul(targets[i].Side, thisSide, e);
                    }
                    return sum;
                }

                case 5:     // ValueOfBuffingAttack（`:0x93A8F2` → `ScoreFromBestAttack` RVA 0x939A00）
                {
                    // 原版 = `base + (最佳攻击分【带 buff】− 最佳攻击分【不带】) × sideMul × mult`。
                    // `GetPotentialBuffedAttackActions` 只枚举**近战** action（`actionType=1`），
                    // 所以这里也只看近战；`ScoreFromBuffedAttack` 用的是 `CurrentMeleeAttack + buff.meleeAttackChange`。
                    float sum = baseValue;
                    int meleeDelta = e.buff != null ? e.buff.meleeAttackChange : 0;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        // 🔴 2026-10-25（`R11` · `R6-02`）：原版循环体**第一句**就是 `t.mightAct` 闸
                        //   （`AI__ScoreFromCriteria.c` 的 `:0x18093A934-0x18093A936`：
                        //   `CardScript.get_mightAct` 为假 ⇒ `je 0x18093AA63` 跳过这个目标）。
                        //   `mightAct` 的引擎侧唯一判据 = `ConditionEval` 的 `case 20`（同源：
                        //   `CardScript__get_mightAct.c`）⇒ 走 `AiTargets.MightAct` 转发，别在这里另写一份。
                        if (!AiTargets.MightAct(ctx, targets[i])) continue;
                        if (targets[i].Unit == null) continue;
                        float withBuff = BestAttackScore(ctx, targets[i], meleeDelta);
                        float noBuff = BestAttackScore(ctx, targets[i], 0);
                        sum += (withBuff - noBuff) * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 6:     // CurrentAttack（`:0x93AA73-0x93AB0B`）
                {
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        int atk = u != null ? u.Attack : (targets[i].Card != null ? targets[i].Card.Attack : 0);
                        sum += atk * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                // ============================================================
                //  🆕 2026-10-25（`R11` · `R6-01`）：7 / 8 / 11 / 20 / 30 / 40 —— **池内 0 条用它们**
                //  （现数 `ai_scoring.json`：scoringOption 只出现 0/1/2/3/4/5/6/12/13/14/25/35）
                //  ⇒ 补齐是「铁律 11：只有先后、没有不做」。判据逐条 = 判据 §1·5 的 RVA。
                // ============================================================

                case 7:     // SumValueOfJam（`:0x18093AB10-0x18093AB41`）
                {
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        sum += ScoreFromJam(u) * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 8:     // RandomValue（`:0x18093ABA7-0x18093ABCC`）
                    // 原版 `acc + (float)manager.GetRandomInt(0, 100)` —— **不乘 M、不乘 mul**。
                    // ⚠️ 走 `ctx.AiRng`（`BattleContext.AiRng`，种子派生 ⇒ 同一局可复现），
                    //   与 `TweakAvailableActions` 用同一个骰子池；**别改成 `ctx.Rng`**。
                    return baseValue + ctx.AiRng.Next(0, 100);

                case 11:    // ValueOfBuffing（`:0x18093AC34-0x18093ACC2`）
                {
                    float sum = baseValue;
                    if (e.buff == null) return sum;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        sum += ScoreFromBuff(u, e.buff) * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 20:    // SumValueOfPoison（`:0x18093ADAC-0x18093AE42`）
                {
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        // 前置（`!poisoned && !resistant && !督军`）已经在 `SimpleAI.ScorePoisoning` 里
                        sum += SimpleAI.ScorePoisoning(u) * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 30:    // SumValueOfGivingCharge（`:0x18093AED9-0x18093AF8A`）
                {
                    // 原版 `ScoreFromGivingCharge` 被**内联**：
                    //   `(!t.CanActNow() && t.summonSickness(+0x58)) ? t.CurrentMeleeAttack : 0`
                    // 🔴 **不在这里重写一份** —— 转发现成的 `SimpleAI.ScoreGivingCharge`
                    //   （它内部的 `RuleCore.CanActNow` 是引擎那个共用口，注释里点明「⛔ 别重写
                    //   `!u.Exhausted && !u.IsStunned`」）。两条判据 = 迟早不一致。
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        int tSide = SideOf(ctx, u);
                        int tSlot = SlotOf(ctx, tSide, u);
                        if (tSlot < 0) continue;
                        sum += SimpleAI.ScoreGivingCharge(ctx, thisSide, tSlot,
                                                          tSide == thisSide ? "own" : "enemy")
                             * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 40:    // ValueOfChangeWeapon（`:0x18093B0BE-0x18093B13D`）
                {
                    // 原版：只对 `t.rawCard.maxHealth(+0x9C) == 0` 的目标**加系数本身**
                    // （不乘任何数、不乘 M）。`maxHealth == 0` = 非单位卡（战术/低语）。
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var c = targets[i].Card;
                        if (c == null || c.Health != 0) continue;
                        sum += SideMul(targets[i].Side, thisSide, e);
                    }
                    return sum;
                }

                case 9:     // FixedValueIfCountGreaterThanBaseEnemy（`:0x18093ABD7`）
                    return targets.Count > e.baseValueEnemy ? e.baseValueFriend : 0f;

                case 10:    // FixedValueIfCountLowerThanBaseEnemy（`:0x18093AC09`）
                    return e.baseValueEnemy > targets.Count ? e.baseValueFriend : 0f;

                case 12:    // ValueOfBuffingCost（`:0x18093ACC7` → `ScoreFromHandBuff` RVA 0x93B7A0）
                    return baseValue + HandBuffValue(ctx, card, thisSide, e, targets) * mult;

                case 13:    // ValueOfBuffingMana（`:0x18093AD01` → RVA 0x93BF60；**结果再 × multiplierCount**）
                    return baseValue + ManaBuffValue(ctx, thisSide, e) * mult;

                case 14:    // SumValueOfStun（`:0x18093AD1A` → RVA 0x93C6E0）
                {
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        sum += SimpleAI.ScoreStun(ctx, u) * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 25:    // SumValueOfReturnToHand（`:0x18093AE47` → RVA 0x93C600）
                {
                    float sum = baseValue;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var u = targets[i].Unit;
                        if (u == null) continue;
                        sum += ReturnToHandValue(ctx, targets[i].Side, u) * SideMul(targets[i].Side, thisSide, e) * mult;
                    }
                    return sum;
                }

                case 35:    // ValueOfDrawingCard（`:0x18093AF8F-0x18093B0B0`）
                {
                    // 🔴 原版这一支的 side 系数只比**这一方**是不是 0：
                    //    `cmp byte[thisCard+0x40], 0` → 相等取 multiplyByFriend(+0x18)、否则 multiplyByEnemy(+0x1C)。
                    //
                    // ✅ **2026-10-25（`R11` 第二段 · 前提已核）：原版 AI 的卡 `side` 恒为 0**
                    //    ⇒ 这一支在原版 AI 打分里**恒取 `multiplyByFriend`** —— 我方硬取 friend 是**对的**
                    //    （复现的是「原版 AI 的可观测行为」，⛔ 不是「照字面把三元抄过来」）。
                    //    证据三条（都可复核）：
                    //      ① `BattleManager` 的字段名与偏移：`playerFatigue @ +0x378` / `enemyFatigue @ +0x38C`
                    //         （`d:/2/tools/il2cpp_out/dump.cs:30862 / :30864`）；
                    //      ② 这一支自己就是这么取疲劳的：`thisCard.side == 0 ? enemyFatigue(+0x38C)
                    //         : playerFatigue(+0x378)`（判据 §1·6 实读）⇒ **0 号位的疲劳存在 `enemyFatigue` 里**
                    //         ⇒ **0 号位 = 敌方 = AI**、1 号位 = 本地玩家；
                    //      ③ `ResolveFatigue(bool playerDrawing)`（`dump.cs:32216`）的协程体
                    //         （`BattleManager._ResolveFatigue_d__587__MoveNext.c:60-82`）：
                    //         `playerDrawing == 0` ⇒ 加 `+0x38C`（enemyFatigue）—— 与 ② 互证；
                    //         而 `AI.PlayTurn` 拿动作时传的是 `GetAvailableActions(isPlayer: false)`
                    //         （cpp2il `AI.cs:118` / `.c` 的 `param_2 = 0`）⇒ **AI 取的是非玩家那一侧**。
                    //
                    // ⚠️ **为什么不能改成「字面三元」**（这才是坑）：我方 AI **两条席位都可能是它**。
                    //    若换成 `thisSide == 0 ? multF : multE`，AI 坐 1 号位时这一支会取 `multiplyByEnemy`
                    //    —— 而池内 **56 条** option 35 的 `multiplyByEnemy` **全部缺键 = SO 里就是 0.0**
                    //    （`cards_truth.json` 的 `defaults.exceptions` 明写这两列「照抄原值；SO 里模板是
                    //    1.0/-1.0，列表条目多为 0.0」）⇒ 那 56 张卡的抽牌分会**整段塌成 0**。
                    //    那是**偏离**（原版 AI 从没坐在 1 号位），不是复刻。
                    int p = thisSide;
                    int hand = ctx.Players[p].Hand.Count;
                    bool deckEmpty = ctx.Players[p].Deck.Count == 0;
                    // 牌库空 ⇒ `−2 − 该方疲劳`（⚙️ `A2144` 原来写「手牌数」= 误读，见文件头）
                    float delta = deckEmpty
                        ? -2f - ctx.Players[p].Fatigue
                        : RuleCore.HandMaxFor(ctx) - hand - 1;
                    // 🔴 2026-10-25（`R11` · `R6-10`）：**`baseValue` 进乘法一次、再在外面加一次**
                    //   （判据 §1·6：`:0x18093B083` 的 `addss xmm1, xmm7` + `:0x18093B0A9` 的
                    //   `addss xmm0, xmm7`）⇒ `base + M·mult·(delta + base)`。
                    //   ⚠️ **牌库空那一支【不带】里层那个 `base`** —— `:0x18093B092-0x18093B096`
                    //   把 `xmm1` 整个覆盖成 `−2−疲劳` ⇒ 空库 = `base + M·mult·(−2−疲劳)`。
                    float inner = deckEmpty ? delta : delta + baseValue;
                    return baseValue + e.multiplyByFriend * mult * inner;
                }

                default:
                    // option 7/8/11/20/30/40 等：池内评分数据 0 条（`AUD_T06` §三·三）。
                    // 原版这些分支各自有子程序；**没有判据就不猜** ⇒ 只留 base。
                    return baseValue;
            }
        }

        static float SideMul(int side, int thisSide, AiScoringEntryDto e)
        {
            return side == thisSide ? e.multiplyByFriend : e.multiplyByEnemy;
        }

        static int SideOf(BattleContext ctx, UnitState u)
        {
            if (u == null) return -1;
            if (ReferenceEquals(ctx.Players[0].Warlord, u)) return 0;
            if (ReferenceEquals(ctx.Players[1].Warlord, u)) return 1;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(ctx.Players[0].Board[s], u)) return 0;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(ctx.Players[1].Board[s], u)) return 1;
            return -1;
        }

        // ------------------------------------------------------------------
        //  子打分器（New）
        // ------------------------------------------------------------------

        /// <summary>卡在场上的价值（原版 `CardScript.GetValueInPlay` 对**场上的这一份**）。
        /// 基础式本身在 `SimpleAI.ValueInPlay`（那是全仓唯一读点），这里只负责候选目标
        /// 可能是**手牌/牌库/弃牌堆里的一份**时走 `2×费+1`（= 原位那份没有伤势/眩晕/附魔）。</summary>
        public static float ValueInPlay(AiTargetRef t)
        {
            if (t.Unit != null) return SimpleAI.ValueInPlay(t.Unit);
            var c = t.Card;
            return c == null ? 0f : 2f * c.Cost + 1f;
        }

        /// <summary>
        /// **附魔价值**（原版 `AI.ValueOfEnchantments`，RVA 0x93D3B0 逐指令读出来的）：
        /// 遍历 `activeEffects`，跳过 `extrinsic`；`buffType`（`CardEffect+0x38`）：
        ///   · **1 `addStats`** → `(meleeAttackChange + rangeAttackChange + healthChange) × 0.5`
        ///   · **12/15/16**（enchantment / singleUse / choice）→ **+1**；
        ///     若 `enchantment.spellType == 60 (SecretOrder)` → **−2**
        ///   · **其余全部不加**（尤其 **3 `addTrait` / 4 `removeTrait` = 0 分**）
        ///
        /// 🔴 **我们的实现边界（如实标注，⛔ 别当成「原版同款」）**：
        ///   ① **`addStats` 那一档已按原版算** —— 我们的引擎把属性 buff 直接折进
        ///      `UnitState.Attack/RangedAttack/MaxHealth`，所以「运行时 − 卡面」就是那些 delta 的和；
        ///   ② **12/15/16 那一档做不到** —— 引擎里没有 `activeEffects` 与 `CardEffect` 对象表，
        ///      而**关键词增量分不出 addTrait（原版 0 分）与 enchantment（+1）**
        ///      （实测：运行时 `AddKeyword(droppod)` 会把 `addTrait` 误算成 +1）。
        ///      ⇒ **本档按 0 处理**（少算「附魔 +1」，但不会把大量 `addTrait` 系统性高估）。
        ///      报告 §三 已记：`SecretOrder −2` 与 enchantment +1 **未覆盖**。
        /// </summary>
        public static float ValueOfEnchantments(UnitState u)
        {
            if (u == null || u.Card == null) return 0f;
            var card = u.Card;

            float sum = 0f;

            // ① buffType == addStats(1)：原版 `CardEffect` 0x48/0x50/0x60 = melee/range/health。
            // ⚠️ 原版**不读** 0x58（armourChange）—— 这里也不读。
            int dm = u.Attack - card.Attack;
            int dr = u.RangedAttack - card.RangedAttack;
            int dh = u.MaxHealth - card.Health;
            if (dm != 0 || dr != 0 || dh != 0) sum += (dm + dr + dh) * 0.5f;

            // ② enchantment / singleUse / choice —— **接 E3 建的真实附魔层**
            //    （`CardInstance.ActiveEffects`，原版 `EntityScript.activeEffects // +0x108`）。
            //    判据 = RVA 0x93D3B0：`buffType ∈ {12,15,16}` ⇒ +1；
            //    `enchantment.spellType(+0x90) == 0x3C(60 SecretOrder)` ⇒ −2。
            //    ⚠️ 原版跳过 `extrinsic` 的效果；E3 的 `ActiveCardEffect` **只收附魔那一族**
            //       （`Attach` 拒绝非 12/15/16），所以这一层里没有 extrinsic 条目。
            //    ⚠️ 用 `usedEnchantment` 与否**原版不看** ⇒ 花掉的附魔照样算。
            if (u.Instance != null)
            {
                var effects = u.Instance.ActiveEffects;
                for (int i = 0; i < effects.Count; i++)
                {
                    var e = effects[i];
                    if (e == null || !Enchantments.IsEnchantmentBuffType(e.BuffType)) continue;
                    bool secretOrder = e.Enchantment != null
                                    && e.Enchantment.SpellType == Enchantments.SpellTypeSecretOrder;
                    sum += secretOrder ? -2f : 1f;
                }
            }
            return sum;
        }

        /// <summary>手牌/牌库里的那一份**没有运行时附魔**（我们的手牌实例不记 stat buff）⇒ 0。
        /// 原版对原位卡同样只数 `activeEffects`（手牌加成在原版是另一种对象，见报告）。</summary>
        public static float ValueOfEnchantments(CardDef card)
        {
            return 0f;
        }

        /// <summary>抽牌分（原版 `ScoreFromDrawingCard` / `ScoreFromCriteria` case 35 的内联版）。
        /// ⚠️ **牌库空返回 `−2 − 疲劳`**（RVA 0x93B71B：`mov eax,-2; sub eax,ebp`，
        /// `ebp` = `ObscuredInt(op_Implicit)(fatigue)` 的结果）—— **不是 `−2 − 手牌数`**。
        /// `SimpleAI.ScoreDraw` 已按这一条改。</summary>
        public static float DrawValue(BattleContext ctx, int p)
        {
            if (ctx == null || p < 0 || p > 1) return 0f;
            int hand = ctx.Players[p].Hand.Count;
            if (ctx.Players[p].Deck.Count == 0) return -2f - ctx.Players[p].Fatigue;
            return RuleCore.HandMaxFor(ctx) - hand - 1;
        }

        /// <summary>
        /// 原版 `ScoreFromBestAttack`（RVA 0x939A00）对**一个单位**的取值：
        /// 枚举该单位所有合法的**近战**目标，取「`ScoreFromDamagingBuffedUnit(目标) − 反击 − 存活奖励`」的最大值。
        ///
        /// **逐条照 `ScoreFromBuffedAttack`（`AI__ScoreFromBuffedAttack.c`）**：
        ///   · 攻击力 = `CurrentMeleeAttack + buff.meleeAttackChange`（⛔ 不是 `FieldAttack`，原版这里不叠 pack/swarm 那一层）；
        ///   · 目标那一侧：`ScoreFromDamagingBuffedUnit(target, DamageInfo)`，`ignoreDroppod = true`
        ///     （调用点 flags `1,0`）；
        ///   · 会被反击：`ScoreFromDamagingBuffedUnit(attacker, 反击伤害)` 扣掉；反击后还活着 `+0.1`
        ///     （`0x1834B2DC4`）。
        /// ⚠️ **未做**：`extraScoreFromAttacking / extraScoreFromTargetingOnAttack`（真值 0 张非零）、
        ///     poisonous 的追加分（需要原版 `EnemyReturnsAttack`/bastion 组合，未逐条核）。
        /// </summary>
        static float BestAttackScore(BattleContext ctx, AiTargetRef t, int meleeDelta)
        {
            var u = t.Unit;
            if (u == null || !u.IsAlive) return 0f;
            // 原版 `GetPotentialBuffedAttackActions` 的**第一道闸** = `MightAttackBuffedCard(card, buff)`（`.c:48`，逐句判据 → `SimpleAI.MightAttackBuffedCard` 的 doc）。🔴 只对**带 buff 那一档**（`meleeDelta != 0`）套它 —— 原版 `ScoreFromBestAttack.c:21` 只调一次带 buff 的列表，`meleeDelta == 0` 是「不带 buff」的对照分、原版不过本闸。⚠️ 旧口径 `Exhausted || IsStunned`（= 我们侧 `canAct`）**保留**：原版 `get_mightAct` 并不读 `canAct`（见那个方法的 doc ③），两者谁该管这一格**待引擎线裁决**（2026-10-26 `B8_E` 顺手发现，已记账）
            if (u.Exhausted || u.IsStunned) return 0f;
            if (meleeDelta != 0 && !SimpleAI.MightAttackBuffedCard(ctx, t, meleeDelta, 0)) return 0f;
            var board = ctx.Players[t.Side].Board;
            int slot = -1;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(board[s], u)) { slot = s; break; }
            if (slot < 0) return 0f;

            int foe = 1 - t.Side;
            var foeBoard = ctx.Players[foe].Board;
            int atk = u.Attack + meleeDelta;
            if (atk <= 0) return 0f;

            float best = 0f;
            for (int ts = 0; ts < BoardSpec.Size; ts++)
            {
                var target = foeBoard[ts];
                if (target == null || !target.IsAlive) continue;
                if (RuleCore.IsValidTarget(ctx, t.Side, slot, foe, ts, false) != RuleCodes.OK) continue;

                float s = SimpleAI.ScoreDamaging(target, atk, ignoreDroppod: true);

                // ---- 反击（判据 = `AI__ScoreFromBuffedAttack.c:55-75` 逐句；2026-10-26 `E4` 复核）----
                //   ① 闸 = `EnemyReturnsAttack(…)` —— 唯一判据在 `CounterAttack.NoCounter`（与 `ScoreFromAttack`
                //      同一条，⛔ 别在这儿写第二份）；
                //   ② 🔴 **守方带 `ambush(1200)` ⇒ 反击伤害被【替换成常数 4】**（`.c:47-53` 重建
                //      `DamageInfo(target, 4, …)`，**不是 +4**）—— 旧实现用守方的实际近战攻击力；
                //      池内 `ambush` **12 张** ⇒ 这一条会改那 12 张的 option 5 估值；
                //   ③ 这一笔原版传 `shouldCountDamagedBastion = false`（`.c:66` 第 6 实参 0）；
                //   ④ `+0.1` 的闸 = `health > 反击伤害`（**扣甲前**，`.c:71-75` + `DAT_1834b2dc4`）；
                //      旧实现是 `DamageAfterReduction(u, counter) < u.Health`（**扣甲后**）= 与 `R6-07` 同一条缺口。
                int counter = target.Has(KeywordTable.Ambush) ? 4 : target.Attack;   // 4 = `.c:47-53` 的常数
                // 🔴 2026-10-26（批次 8 · `B8_P`）：这里改调**预测版** `EnemyReturnsAttack` —— 原版
                //   `AI__ScoreFromBuffedAttack.c:77` 调的正是它（四道 ＋ 第 5 道 `shuriken(1120)`）；
                //   而**实际结算**那条链（`_ResolveAttack_d__438__MoveNext.c:871-895`）只有四道
                //   ⇒ 原版这两条路本来就不同源，⛔ 别统一（`CounterAttack.EnemyReturnsAttack` 的 doc 已写明）。
                if (CounterAttack.EnemyReturnsAttack(ctx, t.Side, u, foe, target, false))
                {
                    s -= SimpleAI.ScoreDamaging(u, counter, ignoreDroppod: true, countDamagedBastion: false);
                    if (u.Health > counter) s += 0.1f;
                }

                // ---- 毒（`poisonous 380`）：这一刀**打不死**才加分（`.c:90-113`）----
                //   gate = `has(poisonous) && !dodge && !shield && !dropPod && atk < health + bastion`
                //   ⇒ `+= ScorePoisoning(target) × 0.7f`（`DAT_1834b3188`）。
                //   ⚠️ 它在 `ScoreFromBuffedAttack` 里 ⇒ **option 5 这条链也吃它**（旧实现漏了这一支）。
                //   池内 `poisonous` 0 张 ⇒ 今天零行为变化（铁律 11 照做）。
                if (u.Has(SimpleAI.PoisonousKeyword)
                    && !target.Has(KeywordTable.Dodge) && !target.Has(KeywordTable.Shield)
                    && !target.Has(KeywordTable.DropPod)
                    && atk < target.Health + target.Bastion)
                    s += SimpleAI.ScorePoisoning(target) * SimpleAI.PoisonousScale;

                if (s > best) best = s;
            }
            return best;
        }

        /// <summary>
        /// 原版 `ScoreFromHandBuff`（RVA 0x93B7A0）—— option 12 `ValueOfBuffingCost`。
        ///
        /// 逐段读出来的算法：
        ///   · **`handEffect == null` ⇒ 0**（原版第一句；`truth` 省略全默认对象，
        ///     生成器从 `cards_raw.json` 把「存在性」补回来 —— 见 `gen_ai_scoring.py`）。
        ///   · 设 `manaLeft = 当前能量`、`change = handEffect.cardEffect.costChange`：
        ///     对每个**手牌候选**：
        ///       - `CurrentCost ≤ manaLeft`（**已经打得起**）⇒ 计数 `playable++`；
        ///       - `manaLeft < CurrentCost ≤ manaLeft − change`（**这张卡让它变得打得起**）⇒
        ///         取它的 `ScoreFromPlayingCard` 最大值 `best`；
        ///   · `score = best − 3 × playable`（常数 3.0 @ `0x1834B2E8C`）；
        ///   · 若 `cardEffect.untilEndOfTurn` 为假 ⇒ 再加
        ///     `(−change) × 2 × (multiplyByFriend × (友方候选数 − 1) + multiplyByEnemy × 敌方候选数)`
        ///     （`0x18093BB13-0x18093BBE9`；`limitedUses` 时把两个计数都夹到 `numberOfUses`）。
        /// ⚠️ 我们数据里这两条 option 12 的 `handEffect.cardEffect` **全是默认值**（`costChange=0`）
        ///     ⇒ 结果 = `−3 × 已经打得起的手牌数`（原版也是这个结果，不是我们造的）。
        /// </summary>
        static float HandBuffValue(BattleContext ctx, CardDef thisCard, int thisSide,
                                   AiScoringEntryDto e, List<AiTargetRef> targets)
        {
            var he = e.handEffect;
            if (he == null) return 0f;                 // 原版 `[criteria+0x50] == null` ⇒ return 0
            var ce = he.cardEffect;
            int change = ce != null ? ce.costChange : 0;
            int manaLeft = ctx.Players[thisSide].Energy;

            float best = 0f;
            int playable = 0, friends = 0, enemies = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t.Side == thisSide) friends++; else enemies++;
                if (t.Inst == null) continue;          // 只数手牌/牌库里的那一份（原版 candidate 是 CardScript）
                int cost = RuleCore.CostOf(ctx, thisSide, t.Inst);
                if (cost <= manaLeft) { playable++; continue; }
                if (cost > manaLeft - change) continue;
                if (t.Card == null || ReferenceEquals(t.Card, thisCard)) continue;   // 防自递归
                float v = (t.Card.IsUnit ? 2f * t.Card.Cost + 1f
                                         + AiScoring.ValueOfEnchantments(t.Card) : 0f)
                        + ScoreFromPlayingNow(ctx, t.Card, t.Inst, -1, -1);
                if (v > best) best = v;
            }

            float score = best - 3f * playable;
            if (ce == null || !ce.untilEndOfTurn)
            {
                int f = friends > 0 ? friends - 1 : 0;
                int en = enemies;
                if (he.limitedUses)
                {
                    if (f > he.numberOfUses) f = he.numberOfUses;
                    if (en > he.numberOfUses) en = he.numberOfUses;
                }
                score += (-change) * 2f * (e.multiplyByFriend * f + e.multiplyByEnemy * en);
            }
            return score;
        }

        /// <summary>
        /// 原版 `AI.GetActionScore` 的 **case 5（activeAbility）**：技能自己的两列 criteria
        /// （`ActiveAbility.cardTargetScoringCriteria / nonTargetScoringCriteria`）。
        /// **顺序照反汇编**：先 `nonTargetScoringCriteria`（+0x58），**再** `cardTargetScoringCriteria`
        /// （+0x50，仅当 `targetCard != null`）。⚠️ 这里**没有** `targeted` 门（与出牌那条不同）。
        /// </summary>
        public static float ScoreActiveAbility(BattleContext ctx, CardDef card, CardInstance inst,
                                               int thisSide, UnitState target, AiActiveAbilityDto aa)
        {
            if (ctx == null || card == null || aa == null) return 0f;
            float sum = 0f;
            if (aa.nonTargetScoringCriteria != null)
            {
                for (int i = 0; i < aa.nonTargetScoringCriteria.Length; i++)
                {
                    var e = aa.nonTargetScoringCriteria[i];
                    if (e == null) continue;
                    float v = ScoreFromCriteria(ctx, card, inst, thisSide, target, e);
                    sum += v;
                    Trace("ability.nonTarget[" + i + "] option=" + e.scoringOption + " ⇒ " + v.ToString("0.###"));
                }
            }
            if (target != null && aa.cardTargetScoringCriteria != null)
            {
                for (int i = 0; i < aa.cardTargetScoringCriteria.Length; i++)
                {
                    var e = aa.cardTargetScoringCriteria[i];
                    if (e == null) continue;
                    float v = ScoreFromCriteria(ctx, card, inst, thisSide, target, e);
                    sum += v;
                    Trace("ability.cardTarget[" + i + "] option=" + e.scoringOption + " ⇒ " + v.ToString("0.###"));
                }
            }
            return sum;
        }

        /// <summary>原版 `AI.ScoreFromReturnToHand`（`d:/2/tools/decomp_full/AI__ScoreFromReturnToHand.c:1-40`，
        /// 2026-10-26 `E4` 逐句复核）：
        /// `CurrentCost − (2×基础费+1 − GetValueInPlay) × 1.5 − (rally ? 2 : 0) − (codex ? 2 : 0)`。
        ///
        /// 🔴 **2026-10-26（`E4`）订正两处（保留痕迹）**：
        ///   ① 头一项原版是 **`EntityScript.get_CurrentCost(card)`（运行时费，含降费/加费）**，
        ///      旧实现用 `u.Card.Cost`（**卡面费**）⇒ 有 `changeCost/setCost` 时算错；
        ///   ② `rally` / `codex` 那两笔原版是 **`RawCardScript.HasDefaultTrait(10 / 1000)`**（**卡面默认 trait**），
        ///      旧实现用 `u.Has(...)`（**当前**关键词集合，含临时给的）⇒ 被「给 rally/codex」的单位会被多扣 2 分。
        ///      现在两处都读 `u.Card.Has(...)`（= 卡模板那一份）。
        ///   ③ `2×基础费+1` 那一项原版是 `rawCard.overrideDefaultValueInPlay ? manualValueInPlay : manaCost`
        ///      —— 该分支数据全 0（`R6-08`）⇒ 这里仍用卡面 `Cost`，与 `SimpleAI.ValueInPlay` 同口径。
        /// </summary>
        static float ReturnToHandValue(BattleContext ctx, int side, UnitState u)
        {
            if (u == null || u.Card == null) return 0f;
            float refValue = 2f * u.Card.Cost + 1f;
            float v = (refValue - SimpleAI.ValueInPlay(u)) * 1.5f;      // 1.5 = `DAT_1834b3090`
            float bonus = 0f;
            if (u.Card.Has(KeywordTable.Rally)) bonus += 2f;            // 原版 `HasDefaultTrait(rally 10)`
            if (u.Card.Has(KeywordTable.Codex)) bonus += 2f;            // 原版 `HasDefaultTrait(codex 1000)`
            int cost = u.Instance != null ? RuleCore.CostOf(ctx, side, u.Instance) : u.Card.Cost;
            return cost - v - bonus;
        }

        /// <summary>原版 `ScoreFromManaBuff`（RVA 0x93BF60）的骨架：
        /// `cur = 当前能量`、`limit = cur + buff.costChange`；
        /// **`limit > 能量上限` ⇒ 0**；否则数**手牌里费用落在 `(cur, limit]`** 的张数 × 1.0。
        /// ⚠️ 原版后半段还数「主动技的 manaCost 落在区间」的那一支 —— 我们的 `EffectSpec`
        /// 上没有主动技费用 ⇒ **未接**（报告 §三）。</summary>
        static float ManaBuffValue(BattleContext ctx, int p, AiScoringEntryDto e)
        {
            // 🔴 判据只此一处 = `ScoreFromManaBuff`（原版 `AI.ScoreFromManaBuff` 的完整移植）。
            //    2026-10-26（`E4P08` · 账行 29）：本方法原来内联了它的**前半段**（⑤ 手牌），
            //    现在并到那一处 —— 同一条算式**不再有两个写点**。option 13 走这个窄口。
            // ⑤ 手牌：`CurrentCost > manaLeft && CurrentCost <= limit` ⇒ 每条 +1（`.c:35-47`）。
            // ⑥ 督军：`hasActiveAbility && activeAbility.manaCost ∈ (manaLeft, limit]` ⇒ +1（`.c:48-59`）。
            // ⑦ 同侧 `MinionsAndRemnantInPlay`：逐个同判 ⇒ 每条 +1（`.c:61-91`）。
            // ⑧ 返回累计分（`.c:97`）。
            // ⚠️ 结果与改前**逐字相同**：⑤ 的算式一字未改；⑥⑦ 因缺数据恒 0（列未落）。
            // 判据：`d:/2/tools/decomp_full/AI__ScoreFromManaBuff.c:15-97`。
            // ⚠️ 原版**没有** `change <= 0` 的早退；两者等价（区间为空 ⇒ 0），见 `ScoreFromManaBuff`。
            // ⚠️ 原版上限判的是 `totalMana`（`BoardAnalysis.GetTotalMana` → `PlayerManager.GetTotalMana`）
            //    = 我们 `PlayerState.MaxEnergy`；下限 `manaLeft` = `GetUnusedMana` = `PlayerState.Energy`
            //    （判据 `BoardAnalysis__GetTotalMana.c:4-9` / `__GetManaLeft.c:4-9` 的转发）。
            // ⚠️ `e.buff == null` ⇒ `change = 0` ⇒ 区间 `(cur, cur]` 为空 ⇒ 0（原版同样只读 `criteria.buffEffect`）。
            return ScoreFromManaBuff(ctx, p, e);
        }

        /// <summary>原版 `AbilityLogic.GetAverageValue(valueOptions, min, max, thisCard, targetCard, mult)`
        /// —— `DamageCriteria.GetAverageDamage` / `HealCriteria.GetAverageValue` 的共用实现
        /// （两份 `.c` 都只是把字段透传给同一个方法）。
        ///
        /// **逐条判据（`d:/2/tools/decomp_full/AbilityLogic__GetAverageValue.c`）**：
        ///   · `valueOptions == 80`（`MinPlusMaxTimesMultiplier`）⇒ **`min + max × multiplierFactor`**
        ///     （判据 = `AbilityLogic__GetAverageValue.c` 的 `if (param_1 == 0x50)` 支，正文
        ///      `return param_2 + param_3 * param_6;`）。
        ///     `multiplierFactor` = 调用方传进来的 **multiplierCount**（`AI__ScoreFromCriteria.c:144/167`
        ///     的第 4 个实参 = 前面按 `multiplierCriteria` 算出的 `uVar11`）。
        ///   · 其余取值（含 `0 Normal` / `90 RandomizedOnResolution` / `204 FaithValue`）全部落到
        ///     `LAB_18081692f` = **`Math.Round((min + max) × 0.5)` 银行家舍入**、**不乘 multiplierFactor**。
        ///     ⚠️ 这条是 2026-10-20 现读反编译订正的：E6 原来把 90/204 也当成「近似」，
        ///        实际原版对这两个取值就是走同一个 default 支 ⇒ 我们与它**逐字相同**、不是近似。
        ///        池内评分数据里 `valueOptions` 非 0 的只有 3 条（80/90/204）:
        ///        `GSC59 Day of Ascension`（80）· `BL53 Heldrake Strike`（90）· `SOR14 Amalia Novena`（204）。
        ///
        /// ⚠️ **本函数只实现「80 + default」两支**：反编译里还有 10/20/30/…/170 那些「读当前攻/血/壁垒」
        /// 的档，**池内评分数据 0 条**（`AUD_T06` §三·三）⇒ 按本仓口径不造无判据的分支
        /// （真出现时会落到 default = 平均值；与「静默猜一个」不同，这里是有出处的兜底）。
        /// </summary>
        static int AverageValue(int valueOptions, int min, int max, int multiplierFactor)
        {
            if (valueOptions == 80) return min + max * multiplierFactor;
            return (int)Math.Round((min + max) / 2.0, MidpointRounding.ToEven);
        }

        // ==================================================================
        //  option 7 / 11 的子打分器（🆕 2026-10-25 `R11` · `R6-01`；池内 0 条用）
        // ==================================================================

        /// <summary>单位在**哪一格**（`side` 那一行的槽号；不在场 / 侧非法 ⇒ `-1`）。
        /// 与 <see cref="SideOf"/> 配对用（option 30 要把目标交给只吃槽号的
        /// `SimpleAI.ScoreGivingCharge`）。</summary>
        static int SlotOf(BattleContext ctx, int side, UnitState u)
        {
            if (ctx == null || u == null || side < 0 || side > 1) return -1;
            var board = ctx.Players[side].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(board[s], u)) return s;
            return -1;
        }

        /// <summary>原版 `AI.ScoreFromJam`（判据 §6·6，`AI__ScoreFromJam.c:1-24` 逐句）：
        /// `ValueOfEnchantments(t) + ValueOfTraits(t) + (t.has(jam) ? 0 : rawCard.extraScoreFromJamming)`。
        ///
        /// ✅ **2026-10-26（`E4`）：`ValueOfTraits` 这一项【已查明恒为 0，所以算 0 是【精确】而不是近似】。**
        ///   判据 §9-2 / `R11` §五-4 原来写「本地一条 `_aIScore` 数据都没有」——**那句话查的是卡上的
        ///   trait 列表**（`cards_truth.json` 的 `CardTraits` 只有 name/_trait/_traitValue/_hide）。
        ///   **真正的 `CardTrait._aIScore` 在另一份数据里、本机有**：
        ///   `d:/4/Unity/数据/游戏数据/trait_collection.json`（= 原版 `CardTraitCollection` 的 77 个
        ///   `CardTrait` 资产，`d:/2/.../sharedassets0.assets` 现解）。
        ///   现读：**77 条里 `aIScore != 0` 的 0 条**（全 `0.0`；`berzerk`/`dodge` 两条 `traitConfig` 为 null）。
        ///   ⇒ 原版 `ValueOfTraits(card) = Σ GetTrait(t)._aIScore ≡ 0`（**与遍历哪个集合无关**，
        ///   `AI__ValueOfTraits.c` 那个 `card+0x128` 的遍历源疑问也随之**不再是缺口**）。
        ///   ⇒ 本函数返回 `ValueOfEnchantments(t)` 就是**逐字等价**。
        ///
        /// ⚠️ 剩下那一项 `rawCard.extraScoreFromJamming`（`+0x2dc`）现读 1,351/1,351 = 0，且本工程没有
        ///   这一列 ⇒ 今天恒 0（数据出现时再加）。可达性：option 7 在池内 **0 条**。
        /// </summary>
        static float ScoreFromJam(UnitState t)
        {
            if (t == null) return 0f;
            // `+ (t.has(jam) ? 0 : rawCard.extraScoreFromJamming)` —— 真值 1,351/1,351 = 0
            // ⇒ 那一项今天恒 0；`ValueOfTraits ≡ 0`（见 doc：77 个 CardTrait 的 aIScore 全 0）。
            return ValueOfEnchantments(t);
        }

        /// <summary>原版 `AI.ScoreFromBuff(t, e)`（判据 §6·6，逐句）：
        ///   · `buffType == 1 addStats` ⇒ `e.healthChange + e.rangeAttackChange + e.meleeAttackChange`；
        ///   · `buffType == 12 enchantment / 15 singleUse` ⇒ **1**；
        ///   · `buffType == 3 addTrait / 90 addTraitRemnant` ⇒ 按 `e.trait` 查 <see cref="TraitAiScore"/>；
        ///   · 其余 buffType ⇒ **0**（注意 `16 choice` **不加** —— `ValueOfEnchantments` 才把它算 +1）。
        /// </summary>
        static float ScoreFromBuff(UnitState t, AiBuffDto e)
        {
            if (e == null) return 0f;
            switch (e.buffType)
            {
                case 1:                       // addStats
                    return e.healthChange + e.rangeAttackChange + e.meleeAttackChange;
                case 12:                      // enchantment
                case 15:                      // singleUse
                    return 1f;
                case 3:                       // addTrait
                case 90:                      // addTraitRemnant
                    return TraitAiScore(t, e.trait, e.traitValue);
                default:
                    return 0f;
            }
        }

        /// <summary>原版 `ScoreFromBuff` 的 **trait → 分** 表（`AI.cs:1807-1965`，判据 §6·6 逐条）。
        ///
        /// 键 = 原版 `DefinedTrait` 的**数值**（`d:/2/tools/il2cpp_out/dump.cs:45721-45859` 现读），
        /// ⛔ 别按名字键 —— 我们 `KeywordTable` 只登记了其中一部分，按名字会静默漏项
        /// （同 `AiTargets.TraitName` 那条「未登记 ⇒ 不成立」的坑）。
        ///
        /// | 分 | trait |
        /// |---:|---|
        /// | **2.0** | vanguard(980) dodge(240) shield(80) survivor(430) firstStrike(630) precognition(640) |
        /// | **1.5** | stealth(70) sneakAttack(20) sacrifice(470) pack(610) |
        /// | **1.0** | ward(140) camouflage(985) longrange(1160) unstoppable(180) flying(390) syphon(110) tainted(160) coward(190) backlash(120) relentless(450) transient(455) resolution(840) terror(520) psyker(620) reactor(625) requiem(760) landing(770) |
        /// | **0.5** | dropPod(230) berzerk(360) |
        /// | **−1.5** | stun(100) pindown(970) blind(975) |
        /// | **−1.0** | vulnerable(480) markerlight(1170) |
        /// | **−2.0** | cantAttack(150) |
        /// | **−4.0** | poisoned(200) |
        /// | **3.0** | poisonous(380) |
        /// | 取值 | bloodThirst(220) → `t.CurrentMeleeAttack` · maintenance(370) → `t.CurrentMaintenance` · bastion(850) → `t.CurrentBastion` · mutation(780) → `GetCurrentTraitValue(mutation)` |
        /// | `e.traitValue` | blast(350) · shuriken(1120) |
        /// | 其它 | 0 |
        ///
        /// ⚠️ `mutation(780)` 我们引擎**没有建模**（`KeywordTable` 无此键）⇒ 按 `KwValue("mutation")` 取，
        ///    现有数据里恒 0；option 11 池内 0 条 ⇒ 今天无可观测影响。
        /// </summary>
        static float TraitAiScore(UnitState t, int trait, int traitValue)
        {
            switch (trait)
            {
                case 980: case 240: case 80: case 430: case 630: case 640: return 2f;
                case 70:  case 20:  case 470: case 610: return 1.5f;
                case 140: case 985: case 1160: case 180: case 390: case 110: case 160: case 190:
                case 120: case 450: case 455: case 840: case 520: case 620: case 625: case 760:
                case 770: return 1f;
                case 230: case 360: return 0.5f;
                case 100: case 970: case 975: return -1.5f;
                case 480: case 1170: return -1f;
                case 150: return -2f;
                case 200: return -4f;
                case 380: return 3f;
                case 220: return t != null ? t.Attack : 0f;                              // bloodThirst
                case 370: return t != null ? t.KwValue(RuleCore.Maintenance) : 0f;        // maintenance
                case 850: return t != null ? t.Bastion : 0f;                              // bastion
                case 780: return t != null ? t.KwValue("mutation") : 0f;                  // mutation（未建模）
                case 350: case 1120: return traitValue;                                   // blast / shuriken
                default: return 0f;
            }
        }

        // ==================================================================
        //  🆕 2026-10-26（批次 9 · 执行代理 `E4P08` · 账行 12 / 29 / 38 / 39）
        //     —— 原版 `AI` 的**四个「0 调用点」方法**
        //
        //  🔴 **四条都只加函数、不接线**（判据：`d:/2/tools/decomp_full/` 全树 grep
        //     `GetDefensiveAttackValue|SuicideHeroAttack|ShouldPlayComboCard` **只命中它们各自那份 `.c`**
        //     ⇒ 原版调用点 = **0**；我们这边也**一个调用点都没有**）。
        //  ⇒ **对局 / 结算指纹必须逐字不变**（2026-10-26 实核：`ruleprobe check` 与
        //     `bash 工具/ruleprobe.sh play check` 都是 0 差异、`af8e8178…` 基线没动）。
        //  ⚠️ 将来接线（尤其把前两条接进 `SimpleAI.ScoreFromAttack`）**会改 AI 出招偏好**
        //     ⇒ 那是一次**独立的调度台裁定** + 重钉基线，⛔ 别顺手接。
        //  ⚠️ 四条**全部追加在类尾**：本文件有 16 条账按「文件:行号」引用（`账/战斗逻辑驱动.tsv`），
        //     插在成员之间会让它们整体漂移 ⇒ 只许往类尾加（唯一在中间动过的是 `ManaBuffValue`
        //     的委托改写，**逐行等长**，行号没变）。
        // ==================================================================

        /// <summary>
        /// **原版 `AI.GetDefensiveAttackValue(actingCard, targetCard, currentScore)`**
        /// —— 🥇 `d:/2/tools/decomp_full/AI__GetDefensiveAttackValue.c:1-24`（24 行逐句）·
        /// 🥈 `cpp2il/out_cpp2il_dev_ilspy/AI.cs:971-991`（两份一致，2026-10-26 现读）。
        ///
        /// <para>**原版逐条**：</para>
        /// <list type="number">
        /// <item>攻方 `cardType == Hero(10)` 且守方 `cardType == Hero` ⇒ **返回 0**（`.c:9-16`，
        ///   走 `IL_00a4`）；</item>
        /// <item>守方是 Hero 且 **`currentScore &lt; 2f`** ⇒ **返回 0**（`.c:19-20`；
        ///   常数 `DAT_1834b2bbc` = `2.0f` —— 与 `ShouldPlayComboCard` 里那个 `2f` 是**同一个常量**，
        ///   两处互证）；</item>
        /// <item>其余 ⇒ **原样返回 `currentScore`**（`.c:22`）。</item>
        /// </list>
        ///
        /// <para>🔴 **两处如实标注（都是刻意偏离字面，理由写在这里）**：</para>
        /// <list type="number">
        /// <item>**签名取 `UnitState` 而不是施工单写的 `CardDef`**：原版判的是
        ///   `EntityScript.get_cardType()` —— 判的是**场上那个实体**。我们对等的就是
        ///   `UnitState.IsWarlord`（`CardDef.IsWarlordEquivalent` = 同一判据的**卡面视角**，
        ///   见它的 doc：`cardType 10 ↔ Type=="hero"`）。用 `CardDef` 也能判 10，
        ///   但会把「卡」和「场上的实体」混起来 —— 本方法第 2 条要看**当前**分，够不到实体就没意义。</item>
        /// <item>**空引用**：原版对 `actingCard == null` 或 `targetCard == null` 直接走
        ///   `FUN_1803f47a0`（全局 NRE 桩）= **抛 `NullReferenceException`**。我们**不抛**
        ///   （引擎里抛会打断整条 AI 回合，且本仓红线是「不许静默失败」而不是「照抄崩溃」），
        ///   改为 **原样返回 `currentScore`**（= 不扣分）。这是本方法**唯一**与原版不同的地方。</item>
        /// </list>
        ///
        /// <para>⚠️ **原版调用点 = 0** ⇒ 今天两侧都不可观测；接线 = 改 AI「督军互砍」的偏好，
        /// **必须单独裁定并重钉基线**。</para>
        /// </summary>
        public static float GetDefensiveAttackValue(UnitState acting, UnitState target, float currentScore)
        {
            if (acting == null || target == null) return currentScore;   // 原版在这里抛 NRE（见 doc ②）
            if (acting.IsWarlord && target.IsWarlord) return 0f;         // `.c:9-16`
            if (target.IsWarlord && currentScore < 2f) return 0f;        // `.c:19-20`（`DAT_1834b2bbc`）
            return currentScore;                                        // `.c:22`
        }

        /// <summary>
        /// **原版 `AI.SuicideHeroAttack(actingCard, targetCard)`**
        /// —— 🥇 `d:/2/tools/decomp_full/AI__SuicideHeroAttack.c:1-48` ·
        /// 🥈 `cpp2il/out_cpp2il_dev_ilspy/AI.cs:993-1015`（2026-10-26 现读）。
        ///
        /// <para>**原版逐条**：① 攻方 `cardType == Hero`（`.c:19-20`）；② 守方 `cardType == Hero`
        /// （`.c:22-23`）；③ 读双方 `+0x68..+0x78` 那一组 `ObscuredInt`（5 个 dword = 20 字节的
        /// 混淆整数结构）并用 `op_Implicit` 取 `int` —— 那是 **`currentHealth`**（`.c:24-39`）；
        /// ④ 攻方 `health &lt;= 守方 health` ⇒ **true**（`.c:40-42`）；⑤ 其余 ⇒ **false**。
        /// ⇒ 语义 = 「这一下是督军换督军、而且换了也不亏（对方血不比我多）」。</para>
        ///
        /// <para>🔴 **签名同样取 `UnitState`**（理由同 `GetDefensiveAttackValue` doc ①）：
        /// 原版读的是**当前血**，`CardDef.Health` 是**卡面血**、拿不到「打了几轮之后」的值。</para>
        ///
        /// <para>🔴 **空引用与 `GetDefensiveAttackValue` 同口径**：原版抛 NRE（`FUN_1803f47a0`），
        /// 我们返回 **false**。这是本方法**唯一**与原版不同的地方。</para>
        ///
        /// <para>⚠️ **原版调用点 = 0** ⇒ 今天不可观测；接线 = 改 AI 督军对砍偏好，
        /// **必须单独裁定并重钉基线**。</para>
        /// </summary>
        public static bool SuicideHeroAttack(UnitState acting, UnitState target)
        {
            if (acting == null || target == null) return false;          // 原版在这里抛 NRE（见 doc）
            if (!acting.IsWarlord || !target.IsWarlord) return false;    // `.c:19-23`（两侧都必须是 Hero）
            return acting.Health <= target.Health;                       // `.c:40-42`（当前血，不是卡面血）
        }

        /// <summary>
        /// **原版 `AI.ShouldPlayComboCard(manager, scoringCriteria, thisCard, score)`**
        /// —— 🥇 `d:/2/tools/decomp_full/AI__ShouldPlayComboCard.c:1-70` ·
        /// 🥈 `cpp2il/out_cpp2il_dev_ilspy/AI.cs:2673-2726`（2026-10-26 现读）。
        ///
        /// <para>**原版逐条**：① `scoringCriteria.comboCard(+0x60) == null` ⇒ false；
        /// ② 我方手牌 `PlayerHand.IsRawCardInHand(comboCard)`（`.c:22-25`）；
        /// ③ `unusedMana = manager.GetUnusedMana(isPlayer: false)`（`.c:26`）；
        /// ④ **`thisCard.rawCard.manaCost(+0x78) + comboCard.manaCost(+0x78) &lt;= unusedMana`**（`.c:29-30`）；
        /// ⑤ `AI.playedFirstComboCard(+0x10)` 为真 **或**（`scoringCriteria.comboCardFirst(+0x68)` 为真
        /// **且** `score &gt;= 2f`）⇒ true；⑥ 否则 false。</para>
        ///
        /// <para>🔴 **2026-10-26 顺手发现（施工单写错了一处偏移）**：施工单写「thisCard 的某
        /// `bool(+0x68)`」，而 `.c:32` 读的是 **`param_3 + 0x68`** = **`ScoringCriteria.comboCardFirst`**
        /// —— 🥈 的字段名直接给出了答案（`if (scoringCriteria.comboCardFirst == playedFirstComboCard)`）。
        /// 本方法按**实读**实现（参数 `comboCardFirst`），⛔ 别按施工单那一行找「卡上的标记」。</para>
        ///
        /// <para>🔴 **签名与原版的三处差异（都写清理由）**：</para>
        /// <list type="number">
        /// <item>`ScoringCriteria` → 拆成 `comboCard` / `comboCardFirst` 两个实参：
        ///   我们的 `AiScoringEntryDto` 上**没有**这两列，而 `Data/AiScoringData.cs` 不在本代理白名单
        ///   ⇒ 不硬塞 DTO（宁可显式传参）。**等数据侧把 `comboCard/comboCardFirst` 落进来之后**，
        ///   接线时那两个实参改从 `e` 取即可，方法体不用动。</item>
        /// <item>`manager.GetPlayerHand(isPlayer: false)` / `GetUnusedMana(false)` → 显式 `side`：
        ///   原版**写死 0 号位**（同 `ScoreFromCriteria` option 35 那一段的口径 —— 原版 AI 永远坐 0 号位）。
        ///   我们的 AI **两个座位都可能是它**（真机 / 联机）⇒ 由调用方传自己的座位
        ///   （接线时传 `ctx.Active`）。⚠️ 这是**刻意的字面偏离**，理由与 option 35 的 doc 同源。</item>
        /// <item>`playedFirstComboCard` 显式传入（原版是 `AI` 实例字段 `+0x10`）：
        ///   我们的 AI 是无状态静态类、没有那个字段 ⇒ 由调用方持有该状态后传入。</item>
        /// </list>
        ///
        /// <para>⚠️ **可达性 = 0**：池内 combo 卡 **0 张**、`trait 1130` 无实现路径 ⇒ 今天不可观测。
        /// 数据出现后接线点 = `SimpleAI.ScoreAction` 的出牌那一支（那一处**不在本批白名单**）。</para>
        /// </summary>
        public static bool ShouldPlayComboCard(BattleContext ctx, int side, CardDef comboCard,
                                               bool comboCardFirst, CardDef thisCard, float score,
                                               bool playedFirstComboCard)
        {
            if (ctx == null || side < 0 || side > 1) return false;   // 原版对 null manager 抛 NRE
            if (comboCard == null || thisCard == null) return false; // 原版对 null 抛 NRE（`.c:15-21`）
            if (!IsRawCardInHand(ctx, side, comboCard)) return false; // `.c:22-25`
            int unusedMana = ctx.Players[side].Energy;               // 原版 `GetUnusedMana(false)`
            // `.c:29-30`：两卡 **rawCard.manaCost**（`+0x78`）之和 ≤ 剩余能量 ⇒ 我们取卡面 `Cost`
            //（`CardDef.Cost` 就是原版 `RawCardScript.manaCost`；运行时降费不参与，原版这里也不参与）。
            if (thisCard.Cost + comboCard.Cost > unusedMana) return false;
            // `.c:31-37`：`playedFirstComboCard || (comboCardFirst && score >= 2f)`（`DAT_1834b2bbc` = 2.0f）
            return playedFirstComboCard || (comboCardFirst && score >= 2f);
        }

        /// <summary>「这张卡的模板在手牌里吗」= 原版 `PlayerHand.IsRawCardInHand(rawCard)`
        /// （引用相等；`PlayerHand__IsRawCardInHand.c` 逐个 `Object.op_Equality(rawCard)`）。
        /// ⚠️ 我们的手牌存的是 `CardInstance` ⇒ 比的是 `inst.Card`。</summary>
        static bool IsRawCardInHand(BattleContext ctx, int side, CardDef card)
        {
            var hand = ctx.Players[side].Hand;
            for (int i = 0; i < hand.Count; i++)
                if (hand[i] != null && ReferenceEquals(hand[i].Card, card)) return true;
            return false;
        }

        /// <summary>
        /// **原版 `AI.ScoreFromManaBuff(manager, thisCard, criteria)`**（RVA `0x93BF60`）
        /// —— 🥇 `d:/2/tools/decomp_full/AI__ScoreFromManaBuff.c:15-97` ·
        /// 🥈 `cpp2il/out_cpp2il_dev_ilspy/AI.cs:2052-2110`（2026-10-26 现读，两份一致）。
        ///
        /// <para>**原版逐条**：</para>
        /// <list type="number">
        /// <item>`totalMana = BoardAnalysis.GetTotalMana(manager, isPlayer)`（`.c:22`）→ 我们 `PlayerState.MaxEnergy`；
        ///   `manaLeft = GetManaLeft`（`.c:23`）→ `PlayerState.Energy`；</item>
        /// <item>`limit = criteria.buffEffect.costChange + manaLeft`（`.c:25`）；
        ///   **`limit &gt; totalMana` ⇒ 返回 0**（`.c:26-28`）；</item>
        /// <item>遍历 **`GetCardsInHandRef` 手牌**：`CurrentCost &gt; manaLeft` 且 `&lt;= limit` ⇒ **+1**
        ///   （`.c:35-47`）；</item>
        /// <item>**督军**：`hasActiveAbility` 且 `activeAbility.manaCost ∈ (manaLeft, limit]` ⇒ **+1**
        ///   （`.c:48-59`）；</item>
        /// <item>**同侧 `GetPlayerMinionsAndRemnantInPlay` / `GetEnemyMinionsAndRemnantInPlay`**
        ///   （按 `isPlayer` 选）逐个同判 ⇒ 每条 **+1**（`.c:61-91`）；</item>
        /// <item>返回累计分（`.c:97`）。</item>
        /// </list>
        ///
        /// <para>🔴 **⑥⑦ 今天恒 0 —— 缺的是【数据列】，不是算式**：原版读
        /// `ActiveAbility.manaCost(+0x14)`，而我们的卡表里**没有这个字段**
        /// （`Resources/cards_engine.json` 现读：`"activeAbility"` 键 **0 次**）。
        /// 真值在 `cards_truth.json` 的 `activeAbility.manaCost`（现池 **143 张**有 `activeAbility`），
        /// 但要落进 `cards_engine.json` 还得改 `工具/gen_cards_engine.py` + `Core/CardDef.cs`
        /// —— **两个文件都不在本代理白名单**（施工单行 29 的 ①② 两项）。
        /// ⇒ 本方法留了接线口 <see cref="ActiveAbilityManaCost"/>（默认 `null` = 未知 ⇒ 两段各恒 0）；
        /// 那两项做完之后**只要给这个钩子赋值**（或改读 `CardDef`），⑥⑦ 就活了。</para>
        ///
        /// <para>⚠️ **调用方 = `ManaBuffValue`（option 13 的窄口）**，所以本方法**不是**未接线函数；
        /// 只是它的 ⑥⑦ 两段今天恒 0。改前那个内联版的 ⑤ 算式**一字未改** ⇒ 指纹不变。</para>
        /// </summary>
        public static float ScoreFromManaBuff(BattleContext ctx, int p, AiScoringEntryDto e)
        {
            if (ctx == null || p < 0 || p > 1 || e == null) return 0f;
            int change = e.buff != null ? e.buff.costChange : 0;      // `.c:25`（`criteria.buffEffect`）
            int manaLeft = ctx.Players[p].Energy;                     // `.c:23`（`GetManaLeft`）
            int limit = manaLeft + change;                            // `.c:25`
            if (limit > ctx.Players[p].MaxEnergy)                      // `.c:26-28`（`GetTotalMana`）
            {
                // 🔬 早退这一支**也要有读数**（否则「读数项没出现」会被误读成「没接」）：
                if (TraceCriteria != null)
                    Trace("    manaBuff: hand=0 hero=0 minion=0（limit=" + limit + " > totalMana="
                          + ctx.Players[p].MaxEnergy + " ⇒ 原版整段返回 0）");
                return 0f;
            }

            float sum = 0f;
            int handHits = 0, heroHit = 0, minionHits = 0;

            // ---- ⑤ 手牌（`.c:35-47`）----
            var hand = ctx.Players[p].Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i] == null || hand[i].Card == null) continue;
                int cost = RuleCore.CostOf(ctx, p, hand[i]);           // 原版 `CurrentCost`（运行时费）
                if (cost > manaLeft && cost <= limit) { sum += 1f; handHits++; }
            }

            // ---- ⑥ 督军（`.c:48-59`）· ⑦ 同侧场上（含残骸，`.c:61-91`）----
            // 两段都只看「主动技 manaCost 是否落在 (manaLeft, limit]」，判据函数收在下面一处。
            if (AbilityManaCostInRange(ctx.Players[p].Warlord, manaLeft, limit)) { sum += 1f; heroHit++; }
            var board = ctx.Players[p].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                if (s == BoardSpec.WarlordSlot) continue;               // 督军不在原版那两条 minions 表里
                if (AbilityManaCostInRange(board[s], manaLeft, limit)) { sum += 1f; minionHits++; }
            }

            // 🔬 读数项（⛔ 只走 `TraceCriteria`，产品路径恒 null ⇒ 零影响）：
            //    ⑥⑦ 要等 `activeAbility.manaCost` 列落进来才会出非 0（见 doc）。
            if (TraceCriteria != null)
                Trace("    manaBuff: hand=" + handHits + " hero=" + heroHit + " minion=" + minionHits
                      + (ActiveAbilityManaCost == null ? "（🔴 主动技 manaCost 列未落 ⇒ ⑥⑦ 恒 0）" : ""));
            return sum;
        }

        /// <summary>
        /// 读数/接线口（🆕 2026-10-26 `E4P08`）：**某张卡的主动技 `manaCost`**
        /// （原版 `ActiveAbility.manaCost // +0x14`）。
        ///
        /// <para>🔴 2026-10-29（`B98_K6` · 账 233 ①）：**产品路径已接线** —— 卡表 v37+ 已有
        /// `activeAbilityManaCost` 那一列（`CardDef.ActiveAbilityManaCost`，见下面那格字段初值器）。
        /// 卡表缺这一列 / 夹具直接 `new CardDef(...)` ⇒ 该格 `-1`（未知）⇒ 钩子返回 `-1` ⇒ `AbilityManaCostInRange` 判 false（⛔ 不猜成 0）。</para>
        ///
        /// <para>⚠️ 只有 `ScoreFromManaBuff` 的 ⑥⑦ 两段读它；真实池内 `hasActiveAbility==1` 的 87 张
        /// **`activeAbilityManaCost` 现读全是 0** ⇒ `cost > manaLeft` 恒假 ⇒ **行为零变化**（指纹不动）。</para>
        /// <para>⚠️ 自检（<see cref="AiScoringSelfTest"/>）会把这一格临时改成假钩子 ⇒ **末尾必须复位回产品接线**（`= c =&gt; c != null ? c.ActiveAbilityManaCost : -1`），⛔ 别复位成 `null`（那会把产品路径永久留空）。</para>
        /// </summary>
        public static Func<CardDef, int> ActiveAbilityManaCost = c => c != null ? c.ActiveAbilityManaCost : -1;   // 🔴 2026-10-29（`B98_K6` · 账 233 ①）：**产品路径默认接线** = 卡表 v37+ 的 `CardDef.ActiveAbilityManaCost`（原版 `ActiveAbility.manaCost(+0x14)`）；未知（旧表 / 夹具）恒 `-1` ⇒ `AbilityManaCostInRange` 判 false（⛔ 不猜成 0）

        /// <summary>「这个单位的主动技费用落在 `(manaLeft, limit]` 吗」——**⑥⑦ 共用的唯一判据**
        /// （原版两段的算式逐字相同：`hasActiveAbility &amp;&amp; manaCost &gt; manaLeft &amp;&amp; manaCost &lt;= limit`）。
        /// `unknown`（钩子为 null / 卡没有主动技 / `hasActiveAbility` 判不出）⇒ **false**
        /// （⛔ 不猜 —— 本仓红线：没有判据就不猜）。
        /// </summary>
        static bool AbilityManaCostInRange(UnitState u, int manaLeft, int limit)
        {
            if (u == null || u.Card == null) return false;
            var look = ActiveAbilityManaCost;
            if (look == null) return false;                       // 列未落 ⇒ 未知（不猜）
            if (u.Card.HasActiveAbility == 0) return false;       // 原版 `hasActiveAbility == false` ⇒ 整段跳过
            int cost = look(u.Card);
            if (cost < 0) return false;                           // 约定：<0 = 未知
            return cost > manaLeft && cost <= limit;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 🆕 2026-10-26（批次 9 · 执行代理 `E4P08`）：**账行 12 / 29 / 38 / 39 四条的单测入口**。
    ///
    /// <para>放在 `#if UNITY_EDITOR` 里（`Assembly-CSharp.csproj` 的 `DefineConstants` **含
    /// `UNITY_EDITOR`** ⇒ `工具/typecheck.sh` 会真的编到它，不是「没被编过」的盲区）。
    /// ⛔ **产品路径不调它**；由自检宿主（`RuleEngine/Editor/RuleEngineTest.cs`）接一行即可：
    /// <c>AiScoringSelfTest.Run((tag, ok, detail) =&gt; CheckTrue(ok, tag + " —— " + detail));</c>
    /// （⚠️ 那一行要由调度台派给自检线的写手 —— `RuleEngineTest.cs` **不在本代理白名单**。）</para>
    ///
    /// <para>⚠️ 本入口**只依赖引擎的公开件**（`BattleContext(seed)` + `PlayerState` + `UnitState` +
    /// `CardInstance.Detached`）：不建牌库、不跑 `RuleCore.NewBattle` ⇒ 夹具不会碰到「洗牌/抽牌」
    /// 那些与本包无关的路径。</para>
    /// </summary>
    public static class AiScoringSelfTest
    {
        static CardDef Hero() { return new CardDef("E4P08HERO", "E4P08HERO", "hero", "",
                                                  "common", "Test", 0, 2, 30, 0, null); }
        static CardDef Troop(string id, int cost, int hp)
        {
            return new CardDef(id, id, "unit", "", "common", "Test", cost, 1, hp, 0, null);
        }
        static BattleContext Ctx()
        {
            var ctx = new BattleContext(0xC0FFEE);
            ctx.Players[0] = new PlayerState();
            ctx.Players[1] = new PlayerState();
            return ctx;
        }
        static void PutWarlord(BattleContext ctx, int p, CardDef card)
        {
            var w = new UnitState(card, true);
            ctx.Players[p].Warlord = w;
            ctx.Players[p].Board[BoardSpec.WarlordSlot] = w;
        }
        static void AddUnit(BattleContext ctx, int p, int slot, CardDef card)
        {
            ctx.Players[p].Board[slot] = new UnitState(card, false);
        }

        /// <param name="check">(说明, 通过?, 细节)</param>
        public static void Run(Action<string, bool, string> check)
        {
            if (check == null) return;

            // ================= 行 12：GetDefensiveAttackValue =================
            {
                var hero = Hero();
                var h1 = new UnitState(hero, true);
                var h2 = new UnitState(hero, true);
                var u1 = new UnitState(Troop("E4P08U", 1, 3), false);

                check("行12 ① 双督军 ⇒ 0（不看分）", AiScoring.GetDefensiveAttackValue(h1, h2, 9f) == 0f, "score=9 ⇒ 0");
                check("行12 ② 守方督军 + 1.9 分 ⇒ 0", AiScoring.GetDefensiveAttackValue(u1, h2, 1.9f) == 0f, "1.9 < 2f");
                check("行12 ③ 守方督军 + 2.1 分 ⇒ 2.1", AiScoring.GetDefensiveAttackValue(u1, h2, 2.1f) == 2.1f, "原样返回");
                check("行12 ④ 守方督军 + 正好 2.0 分 ⇒ 2.0（闸是 < 2f）",
                      AiScoring.GetDefensiveAttackValue(u1, h2, 2f) == 2f, "2f < 2f 为假 ⇒ 返回原分");
                check("行12 ⑤ 守方非督军 ⇒ 原样返回", AiScoring.GetDefensiveAttackValue(u1, u1, 7f) == 7f, "无任何闸");
                check("行12 ⑥ 空引用 ⇒ 原样返回（原版抛 NRE，见 doc）",
                      AiScoring.GetDefensiveAttackValue(null, u1, 3f) == 3f, "不抛");
            }

            // ================= 行 39：SuicideHeroAttack =================
            {
                var hero = Hero();
                var strong = new UnitState(hero, true); strong.Health = 5;
                var weak = new UnitState(hero, true); weak.Health = 3;
                var tie = new UnitState(hero, true); tie.Health = 3;
                var troop = new UnitState(Troop("E4P08T", 1, 3), false);

                check("行39 ① 攻血 3 ≤ 守血 5 ⇒ true", AiScoring.SuicideHeroAttack(weak, strong), "3 <= 5");
                var low = new UnitState(hero, true); low.Health = 2;
                check("行39 ② 攻血 3 > 守血 2 ⇒ false", !AiScoring.SuicideHeroAttack(weak, low), "3 > 2");
                check("行39 ③ 平血 ⇒ true（闸是 <=）", AiScoring.SuicideHeroAttack(weak, tie), "3 <= 3");
                check("行39 ④ 攻方非督军 ⇒ false", !AiScoring.SuicideHeroAttack(troop, strong), "两侧都必须是 Hero");
                check("行39 ⑤ 守方非督军 ⇒ false", !AiScoring.SuicideHeroAttack(strong, troop), "同上");
                check("行39 ⑥ 空引用 ⇒ false（原版抛 NRE，见 doc）", !AiScoring.SuicideHeroAttack(null, strong), "不抛");
            }

            // ================= 行 38：ShouldPlayComboCard =================
            {
                var ctx = Ctx();
                int side = 1;
                var combo = Troop("E4P08COMBO", 2, 2);
                var me = Troop("E4P08ME", 3, 2);
                ctx.Players[side].Hand.Add(CardInstance.Detached(combo));
                ctx.Players[side].Energy = 5;                       // 3 + 2 = 5 ⇒ 够

                check("行38 ① combo 在手 + 能量够 + comboCardFirst + 2 分 ⇒ true",
                      AiScoring.ShouldPlayComboCard(ctx, side, combo, true, me, 2f, false), "5 >= 3+2");
                ctx.Players[side].Energy = 4;                       // 4 < 5 ⇒ 差 1
                check("行38 ② 能量差 1 ⇒ false",
                      !AiScoring.ShouldPlayComboCard(ctx, side, combo, true, me, 9f, false), "4 < 3+2");
                ctx.Players[side].Energy = 5;
                check("行38 ③ comboCardFirst 假 且 没打过第一张 ⇒ false",
                      !AiScoring.ShouldPlayComboCard(ctx, side, combo, false, me, 9f, false), "两道闸都不成立");
                check("行38 ④ playedFirstComboCard ⇒ true（不看分数/标记）",
                      AiScoring.ShouldPlayComboCard(ctx, side, combo, false, me, 0f, true), "第一张已打");
                check("行38 ⑤ comboCardFirst + 1.9 分 ⇒ false",
                      !AiScoring.ShouldPlayComboCard(ctx, side, combo, true, me, 1.9f, false), "1.9 < 2f");
                ctx.Players[side].Hand.Clear();
                check("行38 ⑥ combo 不在手 ⇒ false",
                      !AiScoring.ShouldPlayComboCard(ctx, side, combo, true, me, 9f, true), "IsRawCardInHand 为假");
            }

            // ================= 行 29：ScoreFromManaBuff（⑤ 今天可验；⑥⑦ 待数据列）=================
            {
                var ctx = Ctx();
                int side = 1;
                var hero = Hero();
                PutWarlord(ctx, side, hero);
                ctx.Players[side].Energy = 3;
                ctx.Players[side].MaxEnergy = 5;
                // 手牌：cost 4（落在 (3,5] ⇒ +1）· cost 6（超出上限 ⇒ 不数）· cost 3（已打得起 ⇒ 不数）
                ctx.Players[side].Hand.Add(CardInstance.Detached(Troop("E4P08H4", 4, 2)));
                ctx.Players[side].Hand.Add(CardInstance.Detached(Troop("E4P08H6", 6, 2)));
                ctx.Players[side].Hand.Add(CardInstance.Detached(Troop("E4P08H3", 3, 2)));

                var e = new AiScoringEntryDto();
                e.buff = new AiBuffDto { costChange = 2 };          // limit = 3 + 2 = 5 = MaxEnergy ⇒ 不早退
                check("行29 ① ⑤ 手牌 (cur,limit] 的张数 = 1", AiScoring.ScoreFromManaBuff(ctx, side, e) == 1f, "只有 cost 4");

                e.buff.costChange = 3;                              // limit = 6 > 5 ⇒ 0
                check("行29 ② limit > totalMana ⇒ 0", AiScoring.ScoreFromManaBuff(ctx, side, e) == 0f, "6 > 5");
                e.buff.costChange = 2;

                // ⑥⑦：钩子默认 null ⇒ 恒 0（列未落）；赋一个假的钩子 ⇒ 两段各 +1（证明算式是活的）
                AiScoring.ActiveAbilityManaCost = null;
                check("行29 ③ 钩子手工置 null ⇒ ⑥⑦ 恒 0（`null` 那一档还活着；产品默认已接线，见字段初值器）",
                      AiScoring.ScoreFromManaBuff(ctx, side, e) == 1f, "只有 ⑤ 那 1 分");
                AiScoring.ActiveAbilityManaCost = c => (c != null && c.Id == hero.Id) ? 5 : 4;
                // 督军 cost 5 落在 (3,5] ⇒ +1；场上那个 cost 4 ⇒ +1
                AddUnit(ctx, side, BoardSpec.WarlordSlot + 1, Troop("E4P08M", 1, 2));
                check("行29 ④ 钩子给值 ⇒ 督军 +1 / 场上 +1（⑥⑦ 算式是活的）",
                      AiScoring.ScoreFromManaBuff(ctx, side, e) == 3f, "1(手牌) + 1(督军) + 1(场上)");
                AiScoring.ActiveAbilityManaCost = c => c != null ? c.ActiveAbilityManaCost : -1;             // 🔴 2026-10-29（`B98_K6` · 账 233 ①）：复位回**产品接线**（⛔ 别改回 `null` —— 那是「列未落」时代的占位，会把自检跑完后的产品钩子永久留空）
            }
        }
    }
#endif
}
