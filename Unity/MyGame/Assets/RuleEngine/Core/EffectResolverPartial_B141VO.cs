// EffectResolverPartial_B141VO.cs — 批次 **B141** · 执行写手 `W9`（引擎 · `EffectResolver` 独占包）
//
// ── 本文件装什么 ─────────────────────────────────────────────────────────────────────────────
//   ① `TryResolveValueOption` —— 原版 `AbilityLogic.GetValueFromCriteria` 的**唯一分派表**
//      （26 档 `ChooseValueOptions` + `default => min`；判据 `.c` 逐支行号见方法体注释）。
//      它取代原来散在四处的「同一件事写四份」（R3 对账报告 §三 的 V1/V4/V6/V7 散点）：
//        · `EffectResolverPartial_B88E2.cs` 的 `ApplyRefCardValueOption`（100/110/120）
//        · `EnchantmentExecutor_B115.cs` 的 `TryResolveCriteriaValue`（0/10/20/30/40/41/50/80/90/203）
//        · `EffectResolver.cs` 的 `ApplyHealCriteria`（非 0 档一律「没有求值器」）
//        · `EffectResolver.cs` 的 `StampRollPerTarget`（90 档只决定「要不要逐目标掷」—— 它**不是**取值，保持原样）
//
// ── 为什么新开一个 partial 文件，而不是插进 `EffectResolver.cs` ────────────────────────────────
//   `EffectResolver.cs` 有几百处账落点按 `文件:行号` 引用（现读 ~2 万行）⇒ 往里插成员会让整片账漂移
//   （铁律 18·b）。本文件是**新文件、零账落点**，新成员一律放这里。
//
// ── 🔴 已知边界（如实标着）──────────────────────────────────────────────────────────────────
//   · `opt 8` / `healthValueOptions 17` **不在** 26 档里（`ChooseValueOptions.cs:1-29` 现读无 8/17）。
//     它们**不被本表接管**（`case 8/17` 出声 + `false`，调用方沿用正文/卡表数值）——
//     ❓ 语义来源未查清（R3 报告 §六·1）：`opt 8` 的 37 行全是 `DamageType.Random(5)`
//     （`Sawbonez`「Deal 1-2 damage」），套 `default => min` 会把 1-2 压成恒 1。
//   · `valueOptions < 0`（我们的「缺格」哨兵，原版没有这个值）⇒ 出声 + `false`（⛔ 不拿 `min` 顶）。
//   · 原版对「该档需要的对象是 null」一律走 `FUNC_1803f47a0`（空引用抛）⇒ 我们出声 + `false`，
//     由调用方决定「不结算 / 沿用正文」，⛔ 不崩。
//   · `201 OtherCardMarksOfChaos` 的**消费者在 buff 路**（`BL43/BL47` 的 `meleeAttackValueOptions` /
//     `armourValueOptions`），而那一路今天**没有任何读口**（`grep ValueOptions` 只在数据定义处命中）
//     ⇒ 本表把这一档**实现好了**，但接线点在白名单外（`RuleCore.cs`）⇒ 如实记在报告里。
using System;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==========================================================================================
        //  原版 `ChooseValueOptions` 的 26 个值（`cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29`）
        //  ⛔ 别改成「读我们自己某个枚举」—— 本仓没有那份枚举，这张表就是它的可执行拷贝。
        //  ✅ 已复刻（2026-11-04 · B141 · `AbilityLogic__GetValueFromCriteria.c` 全文 + `ChooseValueOptions.cs:1-29`）
        // ==========================================================================================

        /// <summary>
        /// **原版 `AbilityLogic.GetValueFromCriteria` 的唯一分派表**（26 档 + `default => min`）。
        ///
        /// <para>判据 = `d:/2/tools/decomp_full/AbilityLogic__GetValueFromCriteria.c`（全文 366 行，逐支行号见
        /// 每个 `case` 的注释）· 枚举成员 = `cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29`。
        /// 形参对应关系（`.c` 的形参名 → 本方法）：`param_3` = <paramref name="min"/> ·
        /// `param_4` = <paramref name="max"/> · `param_5` = <paramref name="thisCard"/> ·
        /// `param_6` = <paramref name="targetCard"/> · `param_7` = <paramref name="vo"/> ·
        /// `param_8` = <paramref name="multFactor"/> · `param_9` = <paramref name="refCard"/>。</para>
        ///
        /// <para>🔴 **多数档的收尾是 `min == 0 ? 原值 : min × 原值`**（`.c:121-126` 的
        /// `if (param_3 == 0) return uVar3; return param_3 * uVar3;`）—— 与 `200/201/202/203` 那族的
        /// **`min + 原值 × max`（加法）不是同一个算式**，⛔ 别混（`B123_E1` 的 203 就是靠这条分辨的）。</para>
        ///
        /// <para>🔴 **100/101/110/120/204/140 六级【不乘 min】**（原版直接 `return`）：照 40/41/50 那族写会多乘一次。</para>
        ///
        /// <para><paramref name="owner"/> = 施放方座位（`0/1`）。`202/204/140` 需要它（友方名单 / 信仰 / 督军）；
        /// 传 `-1` 时用 <see cref="FindUnit"/> 从 <paramref name="thisCard"/> 反查，查不到 ⇒ 出声 + `false`。</para>
        ///
        /// <para><paramref name="gap"/> 非空 = 这一档本刻读不出来（调用方按既有约定「不结算 / 沿用正文」）；
        /// `false` **不等于**「原版这里没有值」—— 原版只有 `default => min` 与空引用抛两条出口。</para>
        /// </summary>
        internal static bool TryResolveValueOption(BattleContext ctx, int vo, int min, int max, int multFactor,
                                                  UnitState thisCard, UnitState targetCard, UnitState refCard,
                                                  int owner, out int value, out string gap)
        {
            value = 0; gap = null;

            // `-1`（以及任何负数）= 我们的「这一格没落」哨兵 —— 原版没有这个值（`.c` 的 default 会给 min）。
            // ⛔ 不拿 min 顶：那会把「数据缺格」做成「结算了 1 点」，是静默错值。
            if (vo < 0)
            {
                gap = $"`valueOptions = {vo}`（缺格哨兵）：原版 `ChooseValueOptions` 没有负数档 ⇒ 不按 `min` 顶";
                return false;
            }

            // 🔴 **不在 26 档里的两个实测值**：`8`（37 行 / 24 卡，全 `DamageType.Random(5)`）与
            //    `17`（`healthValueOptions`，8 行 / 5 卡）—— 原版代码对它们走 `return min`（`.c:37/47`），
            //    但卡面（`Sawbonez`「Deal 1-2 damage」）与数据（`Random(5)`）都说明「区间随机」才是语义
            //    ⇒ **不由本表接管**，调用方保持原行为（⛔ 不许把 1-2 压成恒 1）。`gap` 出声（铁律「不许静默失败」）。
            if (vo == 8 || vo == 17)
            {
                gap = $"`valueOptions = {vo}` **不在原版 `ChooseValueOptions` 的 26 档里**"
                    + "（`cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29`）⇒ 本表**不接管**，"
                    + "沿用调用方原行为（正文 / 卡表 `min/max`）；❓语义来源未查清（B141 R3 报告 §六·1）"
                    + "—— ⛔ 不许按 `default => min` 把「Deal 1-2」压成恒 1";
                return false;
            }

            switch (vo)
            {
                // `.c:36-46`：`min == max ? min : GetRandomInt(bm, min, max + 1)`
                case 0:
                    value = (min == max) ? min : ctx.GetRandomInt(min, max + 1);
                    return true;

                // `.c:47-52`：`targetCard.CurrentMeleeAttack`
                case 10:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = MinTimes(min, targetCard.CurrentMeleeAttack);
                    return true;

                // `.c:57-61`：`targetCard.CurrentRangeAttack`
                case 20:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = MinTimes(min, targetCard.CurrentRangeAttack);
                    return true;

                // `.c:62-79`：`targetCard` 当前生命（`+0x68` 的 `ObscuredInt`）
                case 30:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = MinTimes(min, targetCard.Health);
                    return true;

                // `.c:82-86`：`thisCard.CurrentMeleeAttack`
                case 40:
                    if (!NeedThis(thisCard, vo, out gap)) return false;
                    value = MinTimes(min, thisCard.CurrentMeleeAttack);
                    return true;

                // `.c:87-92`：`thisCard.CurrentRangeAttack`
                case 41:
                    if (!NeedThis(thisCard, vo, out gap)) return false;
                    value = MinTimes(min, thisCard.CurrentRangeAttack);
                    return true;

                // `.c:93-119`：`thisCard` 当前生命
                case 50:
                    if (!NeedThis(thisCard, vo, out gap)) return false;
                    value = MinTimes(min, thisCard.Health);
                    return true;

                // `.c:120-124`：`targetCard.GetCurrentDamage()` = `currentMaxHealthProcessed − currentHealth`
                case 60:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = MinTimes(min, targetCard.CurrentMaxHealthProcessed - targetCard.Health);
                    return true;

                // `.c:126-130`：`thisCard.GetCurrentDamage()`（`.c:112` 把 `param_6 = param_5` 后同一条）
                case 70:
                    if (!NeedThis(thisCard, vo, out gap)) return false;
                    value = MinTimes(min, thisCard.CurrentMaxHealthProcessed - thisCard.Health);
                    return true;

                // `.c:113-116`：`min + max × multiplierFactor`（乘法分配是 `min + (max × f)`，⛔ 不是 `(min+max) × f`）
                case 80:
                    value = min + max * multFactor;
                    return true;

                // `.c:130`：`return min`（逐目标掷在 `CardScript__GetAdjustedDamage.c:23-30`，由 `StampRollPerTarget` 判）
                case 90:
                    value = min;
                    return true;

                // `.c:138-160`：`refCard.currentBaseMeleeAttack`（`+0x7c`）—— 原版直接 return，**不乘 min**
                case 100:
                    if (!NeedRef(refCard, vo, out gap)) return false;
                    value = refCard.AttackBase;
                    return true;

                // `.c:157-177`：`thisCard.referencedBaseCard != null ? 它.currentMaxHealth(+0xa4) : min`。
                // 现池该字段恒 null（唯一生产者 `chosenAbility 71` 全池 0 行）⇒ 现实值 = `min`。
                // ⛔ 单一实现 = `RefCardBaseAttack`（照抄原版「恒 min」，别在这儿另写一份）。
                case 101:
                    value = RefCardBaseAttack(ctx, min);
                    return true;

                // `.c:172-190`：`refCard` 当前生命（`+0x68`）—— 直接 return，**不乘 min**
                case 110:
                    if (!NeedRef(refCard, vo, out gap)) return false;
                    value = refCard.Health;
                    return true;

                // `.c:195-207`：`refCard.CurrentCost` —— 直接 return，**不乘 min**
                case 120:
                    if (!NeedRef(refCard, vo, out gap)) return false;
                    value = RefCardCostOf(ctx, refCard);
                    if (value < 0)
                    {
                        gap = $"`valueOptions = 120` 要读参照卡的费用，而「{refCard.Name}」不在棋盘上 ⇒ 取不到";
                        return false;
                    }
                    return true;

                // `.c:214-220`：`targetCard.CurrentCost`，然后 `min == 0 ? 原值 : min × 原值`
                case 130:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = RefCardCostOf(ctx, targetCard);
                    if (value < 0)
                    {
                        gap = $"`valueOptions = 130` 要读目标的费用，而「{targetCard.Name}」不在棋盘上 ⇒ 取不到";
                        return false;
                    }
                    value = MinTimes(min, value);
                    return true;

                // `.c:243-252`：`GetHero(bm, thisCard.isPlayer).CurrentMeleeAttack × multiplierFactor`
                //   —— 直接 return，**不乘 min**；`thisCard / bm / hero` 任一为 null 原版抛。
                case 140:
                {
                    int side = SideOf(ctx, owner, thisCard);
                    var hero = side >= 0 ? WarlordOfSide(ctx, side) : null;
                    if (hero == null)
                    {
                        gap = $"`valueOptions = 140` 要读施放方督军的当前近战，而本刻取不到督军"
                            + $"（side = {side}）";
                        return false;
                    }
                    value = hero.CurrentMeleeAttack * multFactor;
                    return true;
                }

                // `.c:253-256`：`targetCard.CurrentBastion`，然后 `min == 0 ? 原值 : min × 原值`
                case 150:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = MinTimes(min, targetCard.Bastion);
                    return true;

                // `.c:258-270`：`thisCard.CurrentBastion`，同上
                case 160:
                    if (!NeedThis(thisCard, vo, out gap)) return false;
                    value = MinTimes(min, thisCard.Bastion);
                    return true;

                // `.c:278-283`：`targetCard` 当前生命 − `targetCard.CurrentMeleeAttack`，然后 `min == 0 ? 原值 : min × 原值`
                case 170:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = MinTimes(min, targetCard.Health - targetCard.CurrentMeleeAttack);
                    return true;

                // `.c:278-290`（`case 200`）：`min + thisCard.CurrentMarksOfChaos × max`
                case 200:
                    if (!NeedThis(thisCard, vo, out gap)) return false;
                    value = min + thisCard.KwValue(KeywordTable.MarkOfChaos) * max;
                    return true;

                // `.c:283-288`（`case 0xc9`）：`min + targetCard.CurrentMarksOfChaos × max`
                // ⚠️ 消费者在 buff 路（`BL43/BL47`），而该路今天没有读口 ⇒ 本档实现好但未接线（报告里标着）。
                case 201:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    value = min + targetCard.KwValue(KeywordTable.MarkOfChaos) * max;
                    return true;

                // `.c:288-320`（`case 0xca`）：`min + Σ(友方在场单位的 trait 0x14a) × max`
                //   同址判据：`CurrentMarksOfChaos` = `GetCurrentTraitValueWithModifiers(0x14a)`（`EntityScript__get_CurrentMarksOfChaos.c`）
                //   ⇒ 我们读 `KeywordTable.MarkOfChaos`（`markofchaos`，`CardDef.cs:7357`）。
                case 202:
                {
                    int side = SideOf(ctx, owner, thisCard);
                    if (side < 0)
                    {
                        gap = "`valueOptions = 202` 要把友方在场单位的混沌印记求和，而本刻判不出施放方座位";
                        return false;
                    }
                    int sum = 0;
                    var board = ctx.Players[side].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var u = board[s];
                        if (u == null) continue;
                        sum += u.KwValue(KeywordTable.MarkOfChaos);
                    }
                    value = min + sum * max;
                    return true;
                }

                // `.c:320-325`（`case 0xcb`）：`min + thisCard.CurrentShuriken × max`
                // ⛔ 单一实现 = `TryResolveCriteriaValue203`（`B123_E1` 建的；那边有完整判据 doc）。
                case 203:
                    return TryResolveCriteriaValue203(ctx, thisCard, min, max, out value, out gap);

                // `.c:325-333`（`case 0xcc`）：`PlayerManager.GetCurrentFaith(thisCard.isPlayer)` —— 直接 return
                case 204:
                {
                    int side = SideOf(ctx, owner, thisCard);
                    if (side < 0)
                    {
                        gap = "`valueOptions = 204` 要读施放方的信仰，而本刻判不出施放方座位";
                        return false;
                    }
                    value = ctx.Players[side].Faith;
                    return true;
                }

                // `.c:333-366`（`case 0xd2`）：目标当前生命 `>= 0` ⇒ `0`；否则 `-(health × min)`（= 超额量的正数）
                case 210:
                    if (!NeedTarget(targetCard, vo, out gap)) return false;
                    if (targetCard.Health >= 0) { value = 0; return true; }
                    value = -(targetCard.Health * min);
                    return true;

                // 🔴 原版兜底：所有**未命中**的值都 `return uVar6`（= `min`，`.c:25/47/75/…` 的每一处 `return uVar6`）。
                //   这里只对「正整数但不认识」的值生效 —— 负数哨兵与 8/17 已在 switch 之前分流。
                default:
                    value = min;
                    return true;
            }
        }

        /// <summary>原版 `.c:121-126` 的收尾：`min == 0 ? 原值 : min × 原值`。</summary>
        static int MinTimes(int min, int raw) { return min == 0 ? raw : min * raw; }

        static bool NeedTarget(UnitState t, int vo, out string gap)
        {
            if (t != null) { gap = null; return true; }
            gap = $"`valueOptions = {vo}` 要读**卡目标**的属性，这一次没有点选目标"
                + "（原版 `GetValueFromCriteria.c` 对 `param_6 == 0` 走空引用出口）";
            return false;
        }

        static bool NeedThis(UnitState t, int vo, out string gap)
        {
            if (t != null) { gap = null; return true; }
            gap = $"`valueOptions = {vo}` 要读**载体**的属性，本刻取不到载体"
                + "（原版 `GetValueFromCriteria.c` 对 `param_5 == 0` 走空引用出口）";
            return false;
        }

        static bool NeedRef(UnitState t, int vo, out string gap)
        {
            if (t != null) { gap = null; return true; }
            gap = $"`valueOptions = {vo}` 要读**参照卡**的属性，本刻参照卡为空";
            return false;
        }

        /// <summary>施放方座位：优先用调用方递进来的 <paramref name="owner"/>，否则从载体反查。
        /// 判不出来 ⇒ `-1`（调用方据此出声，⛔ 不拿 0 顶）。</summary>
        static int SideOf(BattleContext ctx, int owner, UnitState thisCard)
        {
            if (owner >= 0 && owner < 2) return owner;
            if (ctx == null || thisCard == null) return -1;
            int o, s;
            return FindUnit(ctx, thisCard, out o, out s) ? o : -1;
        }

        /// <summary>自检门：<see cref="TryResolveValueOption"/>（⛔ 生产侧别调）。
        /// Editor 程序集看不到 `internal` ⇒ 必须走这个 `public` 口（同 `B123_E1` 的纪律）。</summary>
        public static bool TryResolveValueOptionForTest(BattleContext ctx, int vo, int min, int max, int multFactor,
                                                        UnitState thisCard, UnitState targetCard, UnitState refCard,
                                                        int owner, out int value, out string gap)
        {
            return TryResolveValueOption(ctx, vo, min, max, multFactor, thisCard, targetCard, refCard, owner,
                                         out value, out gap);
        }
    }
}
