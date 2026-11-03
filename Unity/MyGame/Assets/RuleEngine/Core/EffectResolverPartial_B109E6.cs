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
                //    🆕 2026-11-01（`B115_S5` 波 1）：正文**为空**时改交给 `ca` 执行器（`Core/EnchantmentExecutor_B115.cs`）；正文非空时⛔ 别再跑数据行（= 双发）。🆕 2026-11-03（`B124_W6`）：正文为空时还要先过**双发闸第二档** `CarrierBodyCoversSelfEnchantment`（自挂附魔 + 载体卡正文非空 ⇒ 让路；判据链见类尾那个方法的头注）。
                var eops = e.Card.TriggerOpsByNumber(triggerNumber);
                if (eops == null || eops.Count == 0) { if (CarrierBodyCoversSelfEnchantment(e, u)) continue; RunEnchantmentLogicRows(ctx, e, u, owner, slot, triggerNumber, tn, actingCard, targetCard); continue; }

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

        // ================================================================================================
        //  🆕 2026-11-03 · `B124_W6` —— 双发闸**第二档**：载体自己那张卡的正文已经覆盖了这件「自挂附魔」
        //  ⚠️ 加在**类尾**（本文件在此之后没有成员）：文件内已引用的行号（`账/战斗逻辑驱动.tsv` 的
        //     `:69` A 档落点 / `:162` 次处）一个都不动。
        //
        //  ── 判据链（三层，⛔ 别只读一层）────────────────────────────────────────────────────────
        //   ① 原闸（本文件 `FireEnchantmentAbilitiesByNumber` 内那句 `if (eops == null …)`）：只比
        //      **附魔卡自己**的正文（`e.Card.TriggerOpsByNumber(triggerNumber)`）。而**模板的正文恒空**
        //      ⇒ 凡模板带着 `ca` 数据行，这条闸必然放行（不是 bug，是它只回答了「附魔卡有没有正文」）。
        //   ② 自挂点（`Core/EffectResolverPartial_B97W2.cs:755`）：`buffType 12/15/16` 的附魔由
        //      `Enchantments.AttachGrantedByPlayedCard`（`Core/Enchantment.cs:1654-1657`）挂到 `chosen` 身上，
        //      `source = playedInst.Card.Name`。**单位部署时 `chosen` 就是它自己** ⇒ 这类附魔的
        //      `e.Effect.Source == u.Card.Name`（= 「载体自己那张卡挂自己的」唯一可判的形状）。
        //   ③ 回归点：`RunDoDamageRowOfEnchantment` **2026-11-03 02:59 才落地**（`EnchantmentExecutor_B115.cs:860`）；
        //      15:24 那次基线它只出声不结算 ⇒ 在此之前「正文跑一遍 + 模板行空转」看起来是对的。
        //
        //  ── 为什么第二档必须存在（实测样本 `GOF56 Deffkopta`）────────────────────────────────────
        //   `cards_engine.json`：`GOF56` 的 `enchantment` 列 = `{trigger 0, buffType 15, token c40a0661…}`，
        //   指向模板 `c40a06616a2968240b1f765d0f919b69`（`mName = 28 Goff-Deffkopta_enchantment`），
        //   模板唯一那条 ability = `trigger 0 / ca 10 / damage {2,2}` —— **与卡面正文
        //   「Rally: Other friendly Deffkopta deal 2 damage to a random enemy」是同一件事的两种表示**。
        //   ⇒ 正文那一趟（`eachunitdeal`）打 2、模板那一趟（`ca 10`）又打 2 = 4（断言期望 2）。
        //
        //  ── 判法：谁有正文谁就跑 ─────────────────────────────────────────────────────────────────
        //   · 附魔卡自己有正文 ⇒ 走原路（`eops` 非空，⛔ 不进本方法）；
        //   · 否则：**自挂**（`IsSelfGrantedEnchantmentOf` = 载体卡自己的数据行挂的）**且载体卡有正文 op** ⇒ 模板行**让路**；
        //   · 其它一切（他挂的附魔 / 载体正文为空 / 数据里不是载体自己那张卡挂的）⇒ 模板照跑（= 模板独有的行不丢）。
        //
        //  ── 🔴 2026-11-03 `B124_W18` 订正（**上一版修复没生效**，铁律 5 就地改）──────────────────────
        //   ① **事实（实跑日志，非推断）**：`_tmp_view/ruleengine.log:825825` 的同一行里写着
        //      `附魔层：把「Deffkopta」挂到「Deffkopta」身上（#id 0，buffType 15，spellType 0，**来源 Deffkopta**）`
        //      ⇒ 这条路上 `Effect.Source` **就是卡名**（`Enchantments.AttachGrantedByPlayedCard`
        //      在 `Enchantment.cs:1654-1657` 传的 `source: playedInst.Card.Name`），
        //      W6 那句「`Source` 在别的路是日志标签」（`B124_W6` 报告 §六·1）当**隐患**成立、当**本次根因不成立**。
        //   ② **真根因 = 闸没被调用**：本文件那个 `if` 在 `FireEnchantmentAbilitiesByNumber` 里，服务的是
        //      **数值路**（`FireTriggerByNumberWithSink`，现读只跑 60/185/187/210/410 —— `EffectResolver.cs:20605-20607`
        //      / `:20679` / `B97W2.cs:549` / `RuleCore.cs:13035`）；而 `Rally` 走的是**关键词路**
        //      `RuleCore.cs:13019` → `RunEnchantmentLogicRowsByKeyword` → `RunEnchantmentLogicRows`
        //      （`EnchantmentExecutor_B115.cs:77`）—— **那条路一次都不经过本文件**。
        //      实证：整份日志里 `[B124_W6]` 让路告警只出现 2 次（`FPrayer` / `FX13Prayer`，走数值路），
        //      `Deffkopta` **0 次**；且 `身上的附魔「Deffkopta」触发 RALLY`（数值路的 `ctx.Log`，本文件 `:148`）
        //      全份日志 **0 行** ⇒ 数值路根本没为这次 Rally 跑过。
        //      ⇒ 闸现在同时挂到 `RunEnchantmentLogicRows`（**两条路共用的执行口**）：`EnchantmentExecutor_B115.cs:84`
        //        同物理行追加 `|| CarrierBodyCoversSelfEnchantment(e, carrier)`（零行数变化，见 W18 报告）。
        //   ③ **判据换成了「数据行身份」**（`IsSelfGrantedEnchantmentOf`）：`Effect.Source` 是**字符串标签**，
        //      同一个类型在两条挂载路上语义不同（`AttachGrantedByPlayedCard` 传卡名、`ApplyAbilityBuff` 传
        //      `who`/`by` 日志标签，`EffectResolverPartial_B97W2.cs:755` 的 `source: who`）⇒ 拿它比卡名
        //      「在这条路上碰巧成立、在另一条路上必然不成立」，是**静默陷阱**。
        //      换成「这件附魔的模板 id 出现在**载体卡自己**的 `enchantment` 数据行里（且那行 `trigger == 0 打出即挂`）」
        //      —— 那是**对象身份**（唯一 id，Ordinal 全等），与「谁挂的」是同一件事，不受日志标签影响。
        //   ⚠️ 现读爆半径 = **1 张**：54 张带 `enchantment` 行的卡里，只有 `GOF56` 是**单位**且在
        //     `trigger 0` 自挂（其余 `type 20` 是战术卡，挂的是**被选中的单位**，模板来自战术卡的行
        //     ⇒ 条件①不成立；SOR 那批单位在 `trigger 655/180` 挂，不走 `AttachGrantedByPlayedCard`）。
        //   ⚠️ 已知残余（未修，记账用）：若某张卡的正文只能表达它自挂附魔的**一部分**，本闸是**整件让路**
        //     （二选一，不是逐行）—— 要逐行只可能在 `RunEnchantmentLogicRows` 里逐行判。
        //   🧨 灭自证：把这里的 `return true` 改成恒 `false`（= 删掉让路闸）⇒ `RuleEngineTest.TestA5Batch3` 那条
        //     「只有「别的」那一只开火（2 点）」立刻回到 4（红）；把条件①（`IsSelfGrantedEnchantmentOf`）
        //     删成恒 `true`（= 永远让路）⇒ **他挂的**附魔也被让路，`B109_E6_EnchantDispatch_Assert` 的派发计数、
        //     `EnchantmentTemplateCheck` 那批「模板行要真执行」的断言、`B97W35` 的 A2/A3 全红。
        // ================================================================================================

        /// <summary>「这件附魔是**载体自己那张卡**挂的，而那张卡的正文已经解析成 op」⇒ 模板行让路。
        /// <para>判据链 / 为什么 / 爆半径 / 灭自证 → 上面那一节的头注（⛔ 别在这里抄第二份）。</para>
        /// <para>⚠️ **不判触发数值**：样本里正文那条 `eachunitdeal` 挂在 `Rally`（`trigger 81`）上，
        /// 而模板那条 `ca 10` 挂在 `trigger 0` 上 —— 按 trigger 逐格比会**两边都对不上**（实测：
        /// `GOF56.TriggerOpsByNumber(0)` 为空）。这里问的是「这张卡的正文整体能不能表达效果」，
        /// 不是「这一格有没有正文」。</para></summary>
        static bool CarrierBodyCoversSelfEnchantment(ActiveEnchantment e, UnitState u)
        {
            if (e == null || e.Effect == null || u == null || u.Card == null) return false;
            // ① 自挂闸（🔴 2026-11-03 `B124_W18` 换成**数据行身份**；⛔ 不再比 `Effect.Source` / 卡名 —— 理由见上方头注 ③）
            if (!IsSelfGrantedEnchantmentOf(e, u.Card)) return false;
            var rts = CardDef.RoutableTriggers;
            if (rts == null) return false;
            // ② 载体卡正文闸：只要**有一个**可路由触发词解析出了 op，就说明这张卡的正文能表达效果
            for (int i = 0; i < rts.Length; i++)
            {
                string kw = rts[i];
                if (string.IsNullOrEmpty(kw)) continue;
                var ops = u.Card.TriggerOps(kw);
                if (ops == null || ops.Count == 0) continue;
                string key = (u.Card.Id ?? u.Card.Name ?? "?") + "|" + (e.Card != null ? (e.Card.Name ?? "?") : "?");
                if (_b124w6SelfEnchantCovered.Add(key))
                    UnityEngine.Debug.LogWarning(
                        $"[B124_W6] 「{u.Card.Name}」自挂的附魔「{(e.Card != null ? e.Card.Name : "?")}」的模板行**让路**："
                      + $"载体卡自己的正文已经解析成 op（触发词 `{kw}`）⇒ 正文那一趟已经做过同一件事，"
                      + "模板再跑一遍 = 双发（判据 → `EffectResolverPartial_B109E6.cs` 的 `CarrierBodyCoversSelfEnchantment` 头注）。");
                return true;
            }
            return false;
        }

        /// <summary>`CarrierBodyCoversSelfEnchantment` 的「只喊一次」闩（键 = `载体卡|附魔卡`）。</summary>
        static readonly HashSet<string> _b124w6SelfEnchantCovered =
            new HashSet<string>(System.StringComparer.Ordinal);

        // ================================================================================================
        //  🆕 2026-11-03 · `B124_W18` —— 自挂判据换成**数据行身份**（替掉 `Effect.Source == 卡名`）
        //  ⚠️ 加在**类尾**（本文件在此之后没有成员）：`账/战斗逻辑驱动.tsv:1285` 引的 `:69` / `:162` 一个都不动。
        //
        //  ── 为什么这个判据稳定（问的是「谁挂的」，答的是**对象身份**）──────────────────────────────────
        //   ① 唯一事实源 = 卡表 `enchantment` 列（`cards_engine.json` v38，`CardDatabase.EnchantmentGrantsOf`
        //      在 `Data/CardDatabase.cs:3910-3938` 建索引）：每一行 = `{SourceCardId, TemplateCardId, Trigger, …}`。
        //      载体卡自己那张卡的 id ⇒ 它自己「打出即挂」那几行 —— 行里的 `TemplateCardId` 就是
        //      `CardDatabase.FindEnchantmentTemplate(id)` 用的**同一个 `CardDef.Id`**（`BattleContext.cs:2395-2407`
        //      按 `CardDef.Id == id` 命中；模板 `CardDef` 的 `Id` 由 `CardDatabase.cs:3784` 的
        //      `new CardDef(t.id, …)` 直接落）⇒ 两边比的是**唯一 id**，不是名字。
        //   ② 为什么「载体卡自己的行」= 「自挂」：`Enchantments.AttachGrantedByPlayedCard`
        //      （`Core/Enchantment.cs:1624`，挂载行取自 `playedInst.Card`、载体是 `chosen`）在**单位部署**那条路上
        //      的 `chosen` 就是它自己（`RuleCore.cs:4212`）⇒ 这条附魔的模板只可能来自**载体自己的数据行**。
        //      他挂的附魔（战术卡挂给被选中的单位）⇒ 模板来自**战术卡**的行，载体卡的行里没有它 ⇒ 判 false（模板照跑）。
        //   ③ 为什么⛔ 不能比 `Effect.Source`：`ActiveCardEffect.Source` 的注释只说「日志用」（`Enchantment.cs:89`），
        //      而现读两条挂载路的实参语义**不同** —— `AttachGrantedByPlayedCard` 传卡名（`Enchantment.cs:1655`），
        //      `ApplyAbilityBuff` 传 `who`（日志标签，`EffectResolverPartial_B97W2.cs:755`）⇒
        //      「`Source` == 某张卡的卡名」在一半的路上**恒不成立**，写出来的闸会**静默失效**（W6 那次就是）。
        //   ④ 三态照本项目口径：数据源没注册 / 卡表 < v38 ⇒ `GrantedEnchantmentRowsOf` 返回 `null`
        //      ⇒ 本方法判 `false`（**判不了就不让路**，模板照跑 = 改前行为）—— ⛔ 不拿名字顶替。
        //      ⚠️ `ruleprobe` 里就是这个状态（`Stub.Resources.Load` 恒 null ⇒ 没有附魔实例 ⇒ 闸根本到不了）。
        // ================================================================================================

        /// <summary>「这件附魔是**由 <paramref name="carrierCard"/> 自己这张卡的「打出即挂」数据行**挂上去的」。
        /// <para>判据 = 附魔模板的 `CardDef.Id` 逐字等于载体卡某条 `enchantment` 行的 `TemplateCardId`
        /// （且那行 `trigger == ThisCardPlayed(0)`）⇒ 唯一 id 全等，⛔ 不比卡名、⛔ 不比 `Effect.Source`。</para>
        /// <para>⚠️ `false` 有两种含义（都是「不让路」）：① 明确不是载体自己挂的；② 数据源拿不到、判不了。
        /// 两者都走「模板照跑」= 改前行为，**不会**把模板独有的行吞掉。</para></summary>
        static bool IsSelfGrantedEnchantmentOf(ActiveEnchantment e, CardDef carrierCard)
        {
            if (e == null || e.Card == null || carrierCard == null) return false;
            string tmplId = e.Card.Id;
            if (string.IsNullOrEmpty(tmplId)) return false;          // 模板没有唯一 id ⇒ 判不了（⛔ 不猜）
            var rows = Enchantments.GrantedEnchantmentRowsOf(carrierCard);
            if (rows == null) return false;                          // 数据源没注册 / 这张卡没有数据行
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null) continue;
                if (r.Trigger != Enchantments.TriggerThisCardPlayed) continue;
                if (string.Equals(r.TemplateCardId, tmplId, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
