// EffectResolverPartial_B109E6.cs —— 第一百零九会话 · 波 1 · 执行代理 `B109_E6`（引擎线）
//
// 为什么单独一个文件：`Core/EffectResolver.cs` 有 21,047 行、且被 `账/*.tsv` 按 `文件:行号` 大量落点引用
//   ⇒ **往里插成员会让下游行号整体漂移**（`check_ledger_points` 立刻报，铁律 18·b / N9 先例）。
//   本批**唯一**改 `EffectResolver.cs` 的地方 = `:21006-21007` 那两行（**两行进、两行出，零行数变化**）；
//   其余新成员全部追加在这里 —— 编译期它与 `EffectResolver.cs` 是**同一个** `partial class RuleCore`
//   （照 `EffectResolverPartial_B88E2.cs` 的写法；全仓**没有**叫 `EffectResolver` 的类）。
//
// ── 判据（B109_R2 已核清，⛔ 别再重新查一遍；行号以 `B109_R2_附魔派发侧.md` 为准）──────────────
//   原版 `d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c` 的**内联附魔趟**：
//     · 附魔趟 = **`:49-197`**（`:49` 是 `LAB_18094b822`；派发在 **`:184-186`**）——
//       ⚠️ 施工单里旧的 `:47-108` 是**错的**（`:47-48` 只是「跑不跑载体自己那趟」的闸）。
//     · 前置闸 `:50-53`：`activeEnchantments(+0x110)` 空 / 件数 `< 1` / `param_11 != 0` ⇒ 整趟 return。
//     · `:66`  `EntityScript__IsSpentEnchantment(载体, 附魔卡, 0)` ⇒ **整件跳过**（不是跳一条正文）。
//     · `:71-72` 附魔卡的 `cardAbilities(+0x290)`。
//     · `:108-109` `CardAbility__CanTriggerAbility` —— 第一句 `CardAbility__CanTriggerAbility.c:71`
//       `if (ability.trigger(+0x10) != 传入 trigger) return 0;` ⇒ **判据是数据位，不是卡面正文**。
//     · `:115-116` `EntityScript__ReachedQuestCounter(载体, 附魔卡, 载体, 0)` ⇒ 任务附魔没蓄够次数不放行。
//     · `:184-186` `TriggerAbility(this, ability, mgr, **载体=thisCard**, acting, target, anon,
//                 **附魔卡**, **能力下标**, 0)` ⇒ `AbilityData +0x28` = 附魔卡、`+0x4c` = 能力下标
//                 （载体自己那趟传 `-1`，见 `:232`）。
//     · 三道 criteria（`CanTriggerAbility.c:131-138`）读**附魔卡那条 ability 的行**、被筛对象是**载体**；
//       `IsRightContext`（`:126`）看的也是**载体**。
//
// ── ⚠️ 与原版两处【如实差异】（⛔ 别当成 bug 去「修」）──────────────────────────────────────
//   ① 原版过闸后**一律** `TriggerAbility → PlayAbility`（`:184`；`TriggerAbility.c:73-106` 跑 `ability+0x50`
//      那批逻辑行）。我们能跑的只有**文本 op**（`CardDef.TriggerOpsByNumber`）—— 数据行
//      （`chosenAbility 340` 那类）的执行器**不在本口**，它们的**效果**已由手写等价物承担
//      （`RuleCore.ApplyDamageCore:9067` 的 `EnqueueRemovePray` / `GrantPrayEnchantmentFaith:1636`）。
//      ⇒ 本口派发时**只发信号 + 跑已解析出的文本 op**；⛔ **不许**在这里再执行那行数据（= 双发）。
//   ② 原版同一件附魔可以有多条同 trigger 的 ability（`:74-192` **全跑**）；我们按「一件附魔一发」派
//      （我们的文本 op 表本来就按 trigger 合并；现池唯一可达的 `Pray Enchantment` 每件附魔每 trigger
//      只有一条 ability）。
//
// ── 🧨 灭自证（改哪一句会红）────────────────────────────────────────────────────────────────
//   · 把 `EmitCardTrigger` 那一行删掉 ⇒ `B109_E6_EnchantDispatch_Assert` 的 B/E 组（「恰好多 1 条」）红；
//   · 把 `AllSpent` 那一行删掉 ⇒ C 组（已花掉的附魔不派）红；
//   · 把 `EnchantmentHasAbilityForTrigger` 换成恒 `true` ⇒ D/E 组（trigger 不匹配不派）红；
//   · 把 `EffectResolver.cs:21006` 的 `return _enchFired > 0;` 改回 `return false;` ⇒ F 组（返回值）红。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ================================================================================================
        //  账 `1288` ① 的**执行面**（原版 `RawCardScript__TriggerAbilities.c:49-197` 的附魔趟）
        // ================================================================================================

        /// <summary>🔴🆕 **2026-10-31（`B109_E6` · 判据 `B109_R2` §1）**：**按 trigger 数值派的附魔趟** ——
        /// 原版 `RawCardScript__TriggerAbilities.c:49-197`（`LAB_18094b822` 起的共享尾块）的等价物。
        ///
        /// <para>**逐条落法**：`:50-53` 空表闸 → `:59-65` 逐件 → `:66` `IsSpentEnchantment`
        /// → `:108-109` `CanTriggerAbility`（`CanTriggerAbility.c:71` 的「`ability.trigger == 传入值`」）
        /// → `:115-116` `ReachedQuestCounter` → `:184-186` 派发。</para>
        ///
        /// <para>**顺序**：调用方（`FireTriggerByNumberWithSink`）先在**同一句**里发载体自己那趟，
        /// 所以原版的「自己先跑、附魔按挂载顺序随后」（`:202` 的 `goto` 落进共享尾块）自然成立。</para>
        ///
        /// <para>⚠️ **派给谁 / 带什么**：载体 = `thisCard`（`AbilityData+0x10`），能力属于**附魔卡**
        /// （`+0x28` = 附魔卡）；三道 criteria 读**附魔卡的行**、被筛对象是**载体** ——
        /// 由 `EnqueueAbility(abilityHostCard: e.Card, actor: u, …)` 表达。
        /// ⛔ 别把 `abilityHostCard` 写成 `u.Card`（那会判载体的行 = 判错卡）。</para>
        ///
        /// <para>🧨 灭自证见本文件头部。</para>
        /// </summary>
        /// <returns>**真发出了几条 `EvtKind.Trigger`**（0 = 没有附魔 / 全被三道闸挡下）。</returns>
        static int FireEnchantmentAbilitiesByNumber(BattleContext ctx, UnitState u, int triggerNumber, string tn,
                                                    int owner, int slot, UnitState actingCard, UnitState targetCard)
        {
            // 原版 `:50-53`：载体没有附魔 ⇒ 整趟不进（`param_11` 那一档见 `判据/B109_R2_附魔派发侧.md` §5·2：
            //   全库 12 个调用点里唯一非 0 的是 Oath 支，我们两条路都还没建模 —— 如实留着缺口）
            if (ctx == null || u == null || u.Instance == null) return 0;
            if (!Enchantments.HasAny(u)) return 0;

            // 原版 `:59-72`：逐件附魔 → 已花掉整件跳（`:66`）→ 附魔卡有没有这条 trigger 的 ability（`:108`）
            var active = u.Instance.ActiveEnchantments;
            List<ActiveEnchantment> hits = null;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || e.Card == null) continue;
                if (Enchantments.AllSpent(u.Instance, e.Card)) continue;                       // 原版 `:66`
                if (!EnchantmentHasAbilityForTrigger(e.Card, triggerNumber, tn)) continue;     // 原版 `:108`（`CanTriggerAbility.c:71`）
                if (hits == null) hits = new List<ActiveEnchantment>(2);
                hits.Add(e);
            }
            if (hits == null || hits.Count == 0) return 0;

            // 原版 `:115-116`：`ReachedQuestCounter` —— 任务附魔没蓄够次数不放行（现池 0 张 `spellType 120`，
            //   与关键词路 `FireTriggerAt` 用**同一个**消费口，⛔ 别在这里另写一份计数器）
            hits = Enchantments.FilterByQuestCounter(ctx, u, hits, tn);
            // 🔴 2026-10-31（B109_C8）：410 去重闩 —— 同一次派发内，某件附魔派过 `trigger==410 ∧ triggerValue==1`
            //   后，该附魔本趟后续 ability 全跳过（原版 `.c:136-164`）。契约：可能原样返回入参 ⇒ 必须接返回值。
            hits = Enchantments.ApplyFollowUpResolutionDedup(hits, triggerNumber);

            int fired = 0;
            for (int i = 0; i < hits.Count; i++)
            {
                var e = hits[i];
                if (e == null || e.Card == null) continue;

                // ① 表现那一拍（原版 `TriggerAbility.c:58-63` 的载体脉冲 + `:73-106` 的能力动画）：
                //    `EmitCardTrigger` 的 `CardId` = **附魔卡名**（`BattleEvent.CardId` 的约定是卡名，⛔ 不是 `.Id`）、
                //    `Player/Slot` = 载体（= 原版 `AbilityData+0x10`）；
                //    它内部过 `HasTriggerSource`，等价于原版 `CanTriggerAbility.c:71` 那道「有没有这条 ability」。
                var evt = EmitCardTrigger(ctx, e.Card, owner, u, tn, tn.ToUpperInvariant());
                if (evt == null)
                {
                    // 已经过了我们自己的数据位闸，这里还发不出来 ⇒ 卡表与动画表打架 —— 出声，⛔ 不静默
                    UnityEngine.Debug.LogWarning(
                        $"[B109_E6] 「{e.Card.Name}」有 trigger {triggerNumber}({tn}) 的 ability，"
                      + "但 `EmitCardTrigger` 的 `HasTriggerSource` 闸判它发不出信号 ⇒ 这一拍**没有表现事件**。");
                    continue;
                }
                fired++;

                // ② 正文那一半：今天只有**文本 op** 能跑（见文件头「如实差异 ①」）。
                //    ⛔ 不要在这里执行数据行（`chosenAbility 340` 等）—— 效果已由手写等价物承担，重复 = 双发。
                var eops = e.Card.TriggerOpsByNumber(triggerNumber);
                if (eops == null || eops.Count == 0) continue;

                // 两道（target / board）ability 级 criteria：读**附魔卡的行**、被筛对象是**载体**。
                // `abilityActingGate: true` 打开第一道（acting）—— 原版 `CanTriggerAbility.c:127-130` 对
                //   **所有** trigger 都判，而 `actor` = 载体 ⇒ `abilityHostCard` 保证判的是附魔卡自己。
                EnqueueAbility(ctx, TriggerLaneOfCard(e.Card, tn),
                    (u.Name ?? "?") + "·" + (e.Card.Name ?? "?") + "·trigger#" + triggerNumber,
                    () => ResolveWithTriggerTargetSink(ctx, null, () =>
                    {
                        using (EventSlotsScope.TargetCardAndActing(ctx, targetCard, u.Card, actingCard))
                        {
                            ctx.EffectChain++;
                            try { ResolveOps(ctx, owner, u, RandomAbilityChoiceOps(ctx, u, eops, e, null, false, tn), tn.ToUpperInvariant(), chosen: targetCard); }   // 🆕 2026-11-01（写手 `W-19` · 活状态表 `B-251`）：**附魔那半条数值路也接 `randomAbilityChoice`** —— 原版 `RawCardScript__TriggerAbilities.c:184`（附魔趟）调的正是 `RawCardScript__TriggerAbility`，而 `ability+0x48` 那一跳在 `.c:75/:82-94`，**与能力属于哪张卡无关** ⇒ 附魔卡的 `rc` 同样成立。`only = e`（让 `RandomAbilityChoiceOps` 取**附魔卡自己**的 `TriggerRandomChoice`，⛔ 不是载体的）、`keyword = null`（数值路没有关键词）、第 7 形参直给 `tn`。与 `FireTriggerByNumberWithSink` 那处**同形、同一份判法**。
                            finally { ctx.EffectChain--; }
                        }
                    }),
                    actor: u,
                    requireActorInPlay: ActorMustBeInPlayForTrigger(e.Card, tn, null),
                    // 原版 `CanTriggerAbility.c:126` 的 `IsRightContext` 看的是**载体**，读的是**附魔卡的行**
                    abilityContext: AbilityContextOfAbilityV14(e.Card, tn, null),
                    abilityTrigger: tn,
                    abilityTarget: targetCard,
                    abilityHostCard: e.Card,
                    abilityActingCard: actingCard,
                    abilityActingGate: true);

                ctx.Log($"{u.Name} 身上的附魔「{e.Card.Name}」触发 {tn.ToUpperInvariant()}"
                      + "（原版 `RawCardScript__TriggerAbilities.c:184` 的附魔趟；本体 = 附魔卡、thisCard = 载体）");
            }
            return fired;
        }

        /// <summary>**这件附魔卡有没有一条 trigger == <paramref name="triggerNumber"/> 的 ability** ——
        /// 原版 `CardAbility__CanTriggerAbility.c:71`（`ability.trigger(+0x10) != param_2 ⇒ return 0`）
        /// 的等价物。
        ///
        /// <para>**三态（⛔ 别合并「未知」与「明确没有」）**：
        /// ① `triggerAbilityIndices` 列**已知**（`TriggerAbilityRowsOf` 非 null）⇒ 有行 = 命中、空数组 = 明确没有；
        /// ② 列未知（旧表 / 夹具直建）⇒ 退回 `AbilityTriggerPresence(triggerName)`（`1` 命中 / `0` 明确没有）；
        /// ③ 连名字都认不出（`-1`）⇒ 退回「按数值有没有文本 op」这一档并**出声一次**（⛔ 不静默、也不猜）。</para>
        ///
        /// <para>🧨 灭自证：把它改成恒 `true` ⇒ 断言的 D/E 组（trigger 不匹配不派）红。</para>
        /// </summary>
        static bool EnchantmentHasAbilityForTrigger(CardDef card, int triggerNumber, string tn)
        {
            if (card == null) return false;
            var rows = card.TriggerAbilityRowsOf(triggerNumber);      // null = 列未知；`[]` = 明确没有
            if (rows != null) return rows.Length > 0;
            int presence = card.AbilityTriggerPresence(tn);
            if (presence >= 0) return presence == 1;                  // 1 有 / 0 明确没有
            // `-1`：旧卡表 / 合成夹具 / 认不出的名字 —— 只能按「这张卡有没有这个数值的正文」兜底。
            //   ⚠️ 这是**回退口径**，不是原版判据（原版恒有 `cardAbilities`）⇒ 出声一次，⛔ 不静默。
            if (_b109e6NoIndexNoted.Add(card.Id ?? (card.Name ?? "?") + "|" + triggerNumber))
                UnityEngine.Debug.LogWarning(
                    $"[B109_E6] 「{(card.Name ?? "?")}」的 `triggerAbilityIndices` 列未知（`AbilityTriggerPresence == -1`）"
                  + $" ⇒ trigger {triggerNumber}({tn}) 的命中按「有没有这个数值的文本 op」兜底判（旧表口径，⛔ 不是原版判据）。");
            return card.TriggerOpsByNumber(triggerNumber) != null;
        }

        /// <summary>`EnchantmentHasAbilityForTrigger` 的「只喊一次」闩（键 = `卡|trigger`）。</summary>
        static readonly HashSet<string> _b109e6NoIndexNoted =
            new HashSet<string>(System.StringComparer.Ordinal);

        // --------------------------------------------------------------------------------------------
        //  🆕 2026-11-01（写手 `W-19` · 活状态表 `B-251`）—— 自检门
        //  ⚠️ 加在**类尾**（本文件在此之后没有成员）：不影响文件内任何已引用行号。
        //  ⛔ 生产侧别调（纪律同 `FireTriggerRowsByChosenAbilityForTest`）。
        // --------------------------------------------------------------------------------------------

        /// <summary>自检门：<see cref="FireEnchantmentAbilitiesByNumber"/>（附魔那半条数值路）。
        /// <para>它证明的是「附魔卡的 `ability+0x48`（`randomAbilityChoice`）也会被消费」——
        /// 判据 `RawCardScript__TriggerAbilities.c:184` 的附魔趟调的正是 `RawCardScript__TriggerAbility`
        /// （其 `.c:75/:82-94` 那两跳与「能力属于哪张卡」无关）。</para></summary>
        public static int FireEnchantmentAbilitiesByNumberForTest(BattleContext ctx, UnitState u, int triggerNumber,
                                                                  string tn, int owner, int slot,
                                                                  UnitState actingCard, UnitState targetCard)
        {
            return FireEnchantmentAbilitiesByNumber(ctx, u, triggerNumber, tn, owner, slot, actingCard, targetCard);
        }
    }
}
