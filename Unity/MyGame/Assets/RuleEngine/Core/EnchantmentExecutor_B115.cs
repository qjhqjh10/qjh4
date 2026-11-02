// EnchantmentExecutor_B115.cs — 第一百一十五会话 · `B115_S5` · 波 1
//   **附魔模板 `cardAbilities[].abilityLogic[]` 的 `ca`（`AbilityLogic.chosenAbility`）执行器**
//
// ── 为什么另起 partial ───────────────────────────────────────────────────────────────────────
//   `EffectResolver.cs` 有 21,000+ 行、且被 `账/*.tsv` 按 `文件:行号` 大量落点引用
//   ⇒ 往里插成员会让下游行号整体漂移（铁律 18·b）。本波**唯一**改它的地方是
//   `EffectResolverPartial_B109E6.cs` 的 `:122`（**一行进、一行出，零行数变化**）。
//   本文件与它们是**同一个** `public static partial class RuleCore`（照 `EffectResolverPartial_B109E6.cs`
//   的先例；全仓**没有**叫 `EffectResolver` 的类，文档里 `EffectResolver.Xxx` 是口头叫法）。
//
// ── 判据（第一权威 = `d:/2/tools/decomp_full/`，本波逐句现读；⛔ 不拿账里的转述当判据）──────
//   · 附魔趟 = `RawCardScript__TriggerAbilities.c:49-197`（`:49` = `LAB_18094b822`）：
//     `:50-53` 空表闸 → `:59-65` 逐件 → `:66 IsSpentEnchantment` → `:108-109 CanTriggerAbility`
//     → `:115-116 ReachedQuestCounter` → `:184-186 TriggerAbility(…, **附魔卡**, 能力下标, 0)`。
//     ⇒ 这条链**已经**由 `EffectResolverPartial_B109E6.FireEnchantmentAbilitiesByNumber` 复刻。
//   · 本文件补的是**再下一跳**：`AbilityLogic__PlayAbility.c:286` `iVar8 = *(int*)(param_1 + 0x10)`
//     （= `chosenAbility`）之后那个**巨型 if 链**的分派。原版对**每一行 `abilityLogic`** 都跑一次。
//   · `param_4`（= 触发数值）在 `:72` 存进 `local_res20`、`:285` 转给 `iVar21` —— 它是**当前 trigger**，
//     **不是常数**。这在 `ca == 340` 那一支里被直接读到（`:2281` 的 `MatchingTrigger(…, iVar21, …)`）。
//
// ── 本波（波 1）只接一格：`ca = 340 removeThisEnchantment` ─────────────────────────────────
//   判据 `AbilityLogic__PlayAbility.c:2250-2295`（摘除调用在 `:2280 AddRemoveEnchantment`）：
//     ① 遍历 **`abilityData.thisCard(+0x10)` 自己的 `+0x108`**（= 效果表；⛔ 不是 actingCard、不读目标表）；
//     ② 闸① `effect.buffType(+0x38) ∈ {0xf(15 singleUse), 0x10(16 choice)}`；
//     ③ 闸② `effect.enchantingCard(+0x88) != null` ∧
//        `AbilityLogic__MatchingTrigger(enchantingCard + 0x290 /* cardAbilities */, **当前 trigger**, …)`
//        —— `MatchingTrigger.c` 全 31 行：逐项取 `+0x10` 与传入数值比，命中返回 1
//        ⇒ 实义 = 「**那张附魔卡自己有一条 trigger == 当前 trigger 的 ability**」；
//     ④ 命中 ⇒ `AddRemoveEnchantment(bm, thisCard, effect, priority, 0, 0)` ⇒ 摘掉**载体身上**这一条。
//   📊 数据面：现读 `enchantment_templates.json` 135 行里 `ca 340` = **8 行 / 6 张**（池内可达 7 行 / 6 张）。
//
// ── 🔴 与 `RuleCorePartial_B103W5.RunRemoveThisEnchantmentRow` 的关系（**必须读**）────────────
//   那个方法是**主动能力路**的 `ca 340` 执行器，判据与本文件**逐句同源**，但它把闸② 的
//   `trigger` **写死成 400**（`RuleCorePartial_B103W5.cs:405` 的 `AbilityTriggerPresence(400)`）——
//   这在主动能力路是**对的**（`RawCardScript__ResolveActiveAbility.c:58` 恒传 `ThisUnitAbilityUsed = 400`），
//   但附魔路的 trigger 是 `60 / 80 / 190 / 640 …`，写死 400 会**一条都摘不掉**。
//
//   ✅ **2026-11-01 补丁 A 已落**：`RuleCorePartial_B103W5.RunRemoveThisEnchantmentRow` 的方法体已换成
//   转调 `RunRemoveThisEnchantmentForTrigger(…, 400, …)` ⇒ **全仓只有这一份判据体**（铁律 6）。
//   主路传 400（`RawCardScript__ResolveActiveAbility.c:58` 的 `ThisUnitAbilityUsed`）⇒ 行为逐字不变，
//   由 `ruleprobe play check` 指纹不变证明（见 `报告/B115_S5_附魔执行器波01.md`）。
//
// ── ⛔ 双发闸：Pray / DA42 ────────────────────────────────────────────────────────────────────
//   `Pray Enchantment`（`9218576f…`）与 DA42 模板（`e8cf1eb5…`）的 `ca 60 / ca 340` 已经由
//   **手写等价物**承担（`Enchantments.AttachPray` / `RuleCore.GrantPrayEnchantmentFaith` /
//   `Enchantments.RemovePray`；`Enchantments.OnCarrierPlayed` / `Enchantments.EnqueueRemove`）——
//   本执行器对这两张**整张让开**，否则同一件附魔会被摘两次、同一份信仰会被给两次。
//   📌 这是**过渡闸**：两处手写链删掉之后（波 5）必须一起删。
//
// ── 🧨 灭自证（改哪一句会红）────────────────────────────────────────────────────────────────
//   · 删掉 `:340` 那一行分派 ⇒ `B115_S5` 验收格的「件数 1 → 0」变红（件数仍 1）；
//   · 把闸③ 的 `AbilityTriggerPresence(triggerNumber)` 改成恒 `true` ⇒ 「trigger 不匹配的那一件也被摘」负例变红；
//   · 把 `triggerNumber` 写死回 `400` ⇒ 验收格全红（模板的 trigger 从来不是 400）；
//   · 删掉 `IsHandWrittenEnchantmentLane` 的闸 ⇒ 祈祷附魔被摘两次（`Enchantments.RemovePray` 那条链 + 本链）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ================================================================================================
        //  入口：把一件附魔在某个 trigger 上命中的所有 `abilityLogic` 行按 `ca` 分派
        // ================================================================================================

        /// <summary>**附魔卡片上一行 `abilityLogic` 的 `ca` 执行器**（波 1 只接 `340`）。
        ///
        /// <para>调用点 = `EffectResolverPartial_B109E6.FireEnchantmentAbilitiesByNumber`：**只在
        /// `e.Card.TriggerOpsByNumber(trigger)` 为空时**才进来（模板的正文恒空 ⇒ 它是唯一执行路；
        /// 正文非空的行**逐字保持改前行为**，⛔ 别两条都跑 = 双发）。</para>
        ///
        /// <para>⚠️ 与 `FireTriggerRowsByChosenAbility`（`EffectResolver.cs` 文件尾）**不是**同一个东西：
        /// 那个读的是**载体自己那张卡**的行（`u.Card.TriggerAbilityRowsOf`），本方法读的是**附魔卡**的行
        /// （原版 `AbilityData+0x28` = 附魔卡、`+0x10` = 载体）。</para>
        /// </summary>
        /// <returns>真的结算了几行（0 = 这条 trigger 上这行附魔没有可执行的行）。</returns>
        internal static int RunEnchantmentLogicRows(BattleContext ctx, ActiveEnchantment e, UnitState carrier,
                                                    int owner, int slot, int triggerNumber, string tn,
                                                    UnitState actingCard, UnitState targetCard)
        {
            if (ctx == null || e == null || e.Card == null || carrier == null) return 0;
            var card = e.Card;
            // ⛔ 双发闸（见文件头）：这两张的效果已由手写等价物承担。
            if (IsHandWrittenEnchantmentLane(card)) return 0;

            var rows = card.TriggerAbilityRowsOf(triggerNumber);      // `null` = 列未知；`[]` = 明确没有
            if (rows == null || rows.Length == 0) return 0;

            int done = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null) continue;
                if (r.Lg < 0) continue;      // 这条 ability **本来就没有 logic 行** ⇒ 原版那个循环是空的（不是缺口）
                if (r.Ca == ChosenRemoveThisEnchantment)
                {
                    done += RunRemoveThisEnchantmentForTrigger(ctx, owner, carrier, triggerNumber,
                                                               card, $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                // 🆕 2026-11-01（波 2）：`ca 30 buffOthers` / `40 buffSelf` / `50 buffHand`
                if (r.Ca == ChosenBuffOthers || r.Ca == ChosenBuffSelf || r.Ca == ChosenBuffHand)
                {
                    done += RunAbilityBuffRowOfEnchantment(ctx, owner, carrier, card, r, actingCard,
                                                           $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}",
                                                           targetCard);
                    continue;
                }
                // 🆕 B121-D1（2026-11-03）：另外三档**能真执行**的（判据 → 类尾那一节的头注）
                if (r.Ca == ChosenRecallToOwnerHand)
                {
                    done += RunRecallToOwnerHandRowOfEnchantment(ctx, owner, carrier, card, r, targetCard,
                                                                 $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenFollowAfterResolution)
                {
                    done += RunFollowAfterResolutionOfEnchantment(ctx, owner, carrier, card, r, targetCard,
                                                                  $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenTriggerCodex)
                {
                    done += RunTriggerCodexRowOfEnchantment(ctx, owner, carrier, card, r, targetCard,
                                                            $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                // 🆕 B121-D1B（2026-11-03）：正文载荷落地后的真执行器（判据 → 类尾那一节的头注）
                if (r.Ca == ChosenDrawCard)
                {
                    done += RunDrawCardRowOfEnchantment(ctx, owner, carrier, card, r,
                                                        $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenDoDamage || r.Ca == ChosenDoDamageFast || r.Ca == ChosenDamageAndDestroySelf)
                {
                    done += RunDoDamageRowOfEnchantment(ctx, owner, carrier, card, r, actingCard, targetCard,
                                                        destroySelf: r.Ca == ChosenDamageAndDestroySelf,
                                                        by: $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenDoHeal)
                {
                    done += RunHealRowOfEnchantment(ctx, owner, carrier, card, r, targetCard,
                                                    $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenBuffMana)
                {
                    done += RunManaBuffRowOfEnchantment(ctx, owner, carrier, card, r,
                                                        $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                // 🆕 B121-D5（2026-11-03）：四档**执行体补齐**（判据 → 类尾「B121-D5」那一节的头注）
                if (r.Ca == ChosenStun)
                {
                    done += RunStunRowOfEnchantment(ctx, owner, carrier, card, r, actingCard, targetCard,
                                                    $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenStealMinionSelf)
                {
                    done += RunStealMinionSelfRowOfEnchantment(ctx, owner, carrier, card, r, actingCard,
                                                               $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenScriptedActingCardAttack || r.Ca == ChosenScriptedSelfCardAttackDontCount)
                {
                    done += RunScriptedAttackRowOfEnchantment(ctx, owner, carrier, card, r, actingCard, targetCard,
                                                              selfAttacks: r.Ca == ChosenScriptedSelfCardAttackDontCount,
                                                              by: $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                // 🆕 B121-D5R（2026-11-03）：`ca 70 summonUnit` / `ca 100 createCardInHand`（五支取牌，判据 → 类尾那一节）
                if (r.Ca == ChosenSummonUnit)
                {
                    done += RunSummonUnitRowOfEnchantment(ctx, owner, carrier, card, r, actingCard, targetCard,
                                                          $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                if (r.Ca == ChosenCreateCardInHand)
                {
                    done += RunCreateCardInHandRowOfEnchantment(ctx, owner, carrier, card, r, actingCard, targetCard,
                                                                $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
                    continue;
                }
                // 🆕 B121-D1：已知 `ca`、但载荷/执行体缺 ⇒ **专门的出声**（与「本版没这个执行器」区分开）
                if (IsKnownChosenAbilityWithoutPayload(r.Ca))
                {
                    NoteEnchantRowDataGap(card, triggerNumber, r);
                    continue;
                }
                NoteEnchantRowUnsupported(card, triggerNumber, r);
            }
            _b115s5EnchantRowsExecuted += done;
            return done;
        }

        /// <summary>`AbilityEffect.removeThisEnchantment = 340`（`D:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`）。</summary>
        internal const int ChosenRemoveThisEnchantment = 340;

        /// <summary>**关键词路**（`FireTriggerAt(..., only: e)`）的等价入口 —— 把触发**名字**换成数值后转
        /// <see cref="RunEnchantmentLogicRows"/>。
        ///
        /// <para>🔴 **为什么必须有这一格（本波最重要的发现）**：我们这一侧**有两条附魔派发路**，
        /// 而 `FireEnchantmentAbilitiesByNumber` 只服务**数值路**（`FireTriggerByNumber`，
        /// 现读调用点 = 60/185/187 · 210 · 410）：</para>
        /// <list type="number">
        /// <item>**数值路** → `FireTriggerByNumberWithSink` → `FireEnchantmentAbilitiesByNumber`；</item>
        /// <item>**关键词路** → `FireTriggerAt(..., only: e)`（`RuleCore.cs:13020` 的附魔递归；
        ///   `RunActiveEnchantmentTriggerOf` 同形）—— 它对每一件附魔跑的是
        ///   `only.Card.TriggerOps(keyword)`（`RuleCore.cs:12716`），模板**正文恒空** ⇒ 同样什么都不跑。</item>
        /// </list>
        /// <para>⇒ 绝大多数真实触发（`TurnEnd(40)` / `ThisUnitDeath(80)` / `TurnStartPlayer(31)` /
        /// `OtherUnitSummoned(190)` …）走的是**第 2 条**，光接第 1 条等于没接。本方法的调用点补丁见
        /// `资料/完全复刻/报告/B115_S5_附魔执行器波01.md` §待落补丁 B（`RuleCore.cs:13020`，
        /// 该文件**本波被 `B115_W14` 占用** ⇒ 本代理不落）。</para>
        ///
        /// <para>⚠️ **正文非空就让开**：`FireTriggerAt(..., only: e)` 已经跑过 `only.Card.TriggerOps(keyword)`，
        /// 这里再跑 `ca` 就是双发（与数值路同一条闸）。</para></summary>
        internal static int RunEnchantmentLogicRowsByKeyword(BattleContext ctx, ActiveEnchantment e, UnitState carrier,
                                                             int owner, int slot, string triggerName,
                                                             UnitState actingCard, UnitState targetCard)
        {
            if (ctx == null || e == null || e.Card == null || carrier == null || string.IsNullOrEmpty(triggerName)) return 0;
            // 关键词 → trigger 数值：与 `Enchantments.Matching` 同一层换算（`rally → ThisCardPlayed(0)` 那一族）
            int tn = CardDef.AbilityTriggerNumber(TriggerNameOfKeyword(triggerName) ?? triggerName);
            if (tn < 0) return 0;                                     // 认不出名字 ⇒ 不猜（数值路同口径）
            var textOps = e.Card.TriggerOps(triggerName);
            if (textOps != null && textOps.Count > 0) return 0;       // 正文已跑过 ⇒ ⛔ 别再跑数据行（双发）
            return RunEnchantmentLogicRows(ctx, e, carrier, owner, slot, tn, triggerName, actingCard, targetCard);
        }

        /// <summary>⛔ 过渡闸：这两张走手写链（见文件头「双发闸」）。**波 5 与那两条手写链一起删。**</summary>
        static bool IsHandWrittenEnchantmentLane(CardDef enchantmentCard)
        {
            if (enchantmentCard == null) return true;
            if (enchantmentCard.Id == Enchantments.PrayEnchantmentCardId) return true;
            return Enchantments.IsDA42Enchantment(enchantmentCard);
        }

        // ================================================================================================
        //  `ca = 340 removeThisEnchantment` —— 带 **trigger 形参** 的那一版
        // ================================================================================================

        /// <summary>**`chosenAbility 340` 的行执行器（trigger 可指定）** —— 判据逐句见文件头。
        ///
        /// <para>✅ 这里是**唯一实现**：`RuleCorePartial_B103W5.RunRemoveThisEnchantmentRow`（主动能力路）
        /// 已改成转调本方法并传 `400`（补丁 A，2026-11-01）。⛔ 别在那边再写第二份体。</para>
        /// </summary>
        /// <param name="triggerNumber">**当前触发数值**（原版 `PlayAbility` 的 `param_4` → `.c:285` 的 `iVar21`）。
        /// ⛔ 不是常数 400 —— 那个值是主动能力路的，附魔路的 trigger 是 60 / 80 / 190 / 640 …。</param>
        /// <param name="selfTemplate">触发这条 ability 的那张附魔卡（原版 `AbilityData+0x28`）。</param>
        /// <returns>真入队摘除的条数。</returns>
        internal static int RunRemoveThisEnchantmentForTrigger(BattleContext ctx, int owner, UnitState carrier,
                                                              int triggerNumber, CardDef selfTemplate, string by)
        {
            if (ctx == null || carrier == null || (owner != 0 && owner != 1)) return 0;
            var inst = carrier.Instance;
            if (inst == null || inst.ActiveEffects == null || inst.ActiveEffects.Count == 0)
            {
                // 原版 `:2252-2255` 的两道 null 闸 —— 这时**什么都不发生**；出声（红线：不许静默失败）
                ctx.Log($"{by}：`chosenAbility 340`（removeThisEnchantment）——载体「{carrier.Name}」身上"
                      + "**没有效果记录** ⇒ 无事可做（原版 `AbilityLogic__PlayAbility.c:2250-2295` 遍历的就是 `thisCard + 0x108`）");
                return 0;
            }

            // 先收集再入队：`EnqueueRemove` 只入队、不立即摘，但照「先快照后动手」的纪律写。
            List<ActiveCardEffect> hits = null;
            for (int i = 0; i < inst.ActiveEffects.Count; i++)
            {
                var eff = inst.ActiveEffects[i];
                if (eff == null) continue;
                // 闸①：`buffType ∈ {15 singleUse, 16 choice}`（`.c:2261` / `:2290` 的 `0xf` / `0x10`）
                if (eff.BuffType != Enchantments.BuffTypeSingleUse && eff.BuffType != Enchantments.BuffTypeChoice) continue;
                // 闸②/③：那张附魔卡存在，且**它自己有一条 trigger == 当前 trigger 的 ability**
                //   （原版 `AbilityLogic__MatchingTrigger.c` 逐项比 `CardAbility.trigger(+0x10)`）
                if (eff.EnchantmentCard == null) continue;
                if (eff.EnchantmentCard.AbilityTriggerPresence(triggerNumber) != 1) continue;
                if (hits == null) hits = new List<ActiveCardEffect>(2);
                hits.Add(eff);
            }

            int queued = 0;
            if (hits != null)
            {
                for (int i = 0; i < hits.Count; i++)
                    if (Enchantments.EnqueueRemove(ctx, inst, hits[i], ActionPriority.Instant,
                                                   by + "·removeThisEnchantment"))
                        queued++;
            }
            ctx.Log($"{by}：`chosenAbility 340`（removeThisEnchantment，trigger {triggerNumber}）"
                  + $"⇒ {queued}/{(hits != null ? hits.Count : 0)} 条附魔被排进摘除"
                  + "（原版 `AbilityLogic__PlayAbility.c:2250-2295`：`buffType ∈ {15,16}` ∧ "
                  + "`MatchingTrigger(附魔卡.cardAbilities, 当前 trigger)`；`AbilityLogic__MatchingTrigger.c`）");
            _b115s5EnchantRemovals += queued;
            return queued;
        }

        // ================================================================================================
        //  🆕 波 2（2026-11-01）：`ca 30 / 40 / 50` —— 把模板的 `buffEffect` 交给 `AddEffect` 的等价位
        // ================================================================================================
        //  判据（第一权威 `d:/2/tools/decomp_full/`，逐句现读）：
        //   · `40 buffSelf`  → `AbilityLogic__PlayAbility.c:1310-1380`，`:1369 BattleManager__AddEffect`
        //     **硬写 `abilityData.thisCard`**（= 附魔的宿主 = `carrier`），**`targetCriteria` 不参与**。
        //   · `30 buffOthers` → `:1176-1250`：对 `AbilityLogic__GetTargets(thisCard, targetCriteria, …)`
        //     的**每个目标**克隆一份 `CardEffect` 再 `AddEffect`（`:1218`）。目标集由这一行的 `ta`
        //     决定（`AbilityLogic__GetTargets.c`）—— 我们这一侧只落了 `ta`、**没有**那 30 格 criteria
        //     （施工面 §4·0 已记），所以**只接「不需要 criteria 就能定下来」的那几个 `ta`**，其余出声。
        //   · `50 buffHand`   → `:1412 / :1431 BattleManager__AddHandEffect`：作用对象是**手牌**，
        //     不是场上的 `UnitState` ⇒ 本仓没有那个口 ⇒ 出声（早退）。
        //
        //  载荷来源 = `CardDatabase.AbilityBuffsOf(附魔模板卡)`（波 2 新增的那一列；形状与卡表 v40 同）。
        //  应用体 = **`RuleCore.ApplyAbilityBuff`**（`Core/EffectResolverPartial_B97W2.cs:205` 的
        //  `CardScript.AddEffect` 11 组 case 表）—— ⛔ 别在这里另写一份加成逻辑（铁律 6）。
        //  ⚠️ **已知缺口（如实）**：那 11 组**不含 `buffType 1 addStats` / `2 changeCost` / `3 addTrait`**，
        //     而附魔 `ca 30/40` 的 58 行里正好是 `1:17 · 3:18 · 2:5 · 8:2 · 12/15:9 · 90:1`
        //     ⇒ **今天走进去会落到它的 `default:`「不在 11 组里」那一支（出声、不改状态）**。
        //     补丁见 `资料/完全复刻/报告/B115_S5_附魔执行器波01.md` §十二（`EffectResolverPartial_B97W2.cs`，
        //     不属本代理白名单）。

        /// <summary>`AbilityEffect.buffOthers = 30` —— 逐目标 `AddEffect`（目标集由 `ta` 定）。</summary>
        internal const int ChosenBuffOthers = 30;
        /// <summary>`AbilityEffect.buffSelf = 40` —— **恒打 `thisCard`**（= 附魔的宿主），不读 `ta`。</summary>
        internal const int ChosenBuffSelf = 40;
        /// <summary>`AbilityEffect.buffHand = 50` —— 作用对象是**手牌**（本仓无等价口 ⇒ 只出声）。</summary>
        internal const int ChosenBuffHand = 50;

        /// <summary>`ca 30 buffOthers` 的**目标来源** —— 🆕 2026-11-03（`B121-D5`）**换口**：
        /// 走本文件的统一解算口 <see cref="ResolveEnchantmentTargets"/>（按 `ta` 建池 → `TargetCriteriaEval.Matches`
        /// 逐候选过 36 格 → `maxTargets` 裁剪），⛔ 不再是「只认 `ta` 的四档 switch」。
        ///
        /// <para>判据：原版 `AbilityLogic__PlayAbility.c:1176-1250`（`case 0x1e`）对
        /// `AbilityLogic__GetTargets(thisCard, **logic.targetCriteria**, …)` 的每个目标克隆一份 `CardEffect` 再 `AddEffect`；
        /// 目标集由 36 格 criteria 决定，`ta` 只是**建池那一跳**。
        /// 改前的四档 switch 是「第二张表只落了 `ta`」时的过渡实现；`B121-D1B` 已把 36 格导出并挂上
        /// （`CardDef.AttachAbilityTargetCriteria`）⇒ 这一格换成真解算口。</para>
        ///
        /// <para>⚠️ **`ta = 50 inHandFiltered` 仍然出声**：那一档的候选是**手牌里的卡**（原版 `GetTargets.c:884-891`
        /// 之前的 0x32 支），不是场上单位，而本仓 `ApplyAbilityBuff` 只吃 `UnitState`
        /// ⇒ 硬拿棋盘单位顶 = 打错人（静默）。</para>
        /// </summary>
        static List<UnitState> ResolveBuffOthersTargets(BattleContext ctx, int owner, UnitState carrier,
                                                        UnitState actingCard, UnitState chosen, int ta, CardDef card,
                                                        CardDef.TriggerAbilityIndexSpec row)
        {
            if (ta == 50)
            {
                NoteEnchantBuffGap(card, row,
                    "`ca 30 buffOthers` 的 `ta = 50 inHandFiltered` 候选是**手牌里的卡**（不是场上单位），"
                  + "而本仓 `ApplyAbilityBuff` 只吃 `UnitState` ⇒ **本行不结算**（⛔ 不拿棋盘单位顶）");
                return null;
            }
            return ResolveEnchantmentTargets(ctx, owner, carrier, chosen, ta, card, row, "30 buffOthers", actingCard);
        }

        /// <summary>某一方的督军（`PlayerState.Warlord`）—— 取不到返回 `null`，⛔ 不造一个假的。
        /// <para>⚠️ `Enchantments.WarlordOf` 是那个类的 **private** 成员，跨类拿不到 ⇒ 这里用**同一句**
        /// `ctx.Players[side].Warlord`（与它逐字同源，不是第二份判据）。</para></summary>
        static UnitState WarlordOfSide(BattleContext ctx, int side)
        {
            if (ctx == null || ctx.Players == null || side < 0 || side >= ctx.Players.Length) return null;
            var ps = ctx.Players[side];
            return ps != null ? ps.Warlord : null;
        }

        /// <summary>`ca 30 / 40 / 50` 的行执行器（判据 → 本段头注）。
        /// <para>🆕 `B121-D5`：`chosen` 加在**形参表末尾**（不是中间）—— `ChoiceOptionCards_B116W32` 等
        /// 既有调用点按位置传 `by`，插在 `by` 之前会把 string 绑到 UnitState 上。</para></summary>
        internal static int RunAbilityBuffRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                           CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                           UnitState actingCard, string by, UnitState chosen = null)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;

            if (row.Ca == ChosenBuffHand)
            {
                // 🆕 2026-11-02（执行写手 `B115_W20` · 格 1）：**那个等价口建出来了** ——
                //   `RuleCorePartial_B115W20.cs` 的 `RunHandBuffRowOfEnchantment` / `AttachHandAbilityBuff`
                //   （判据 = `PlayerHand__AddHandEffect.c:88-139` 两段：先记记录、再逐张当前手牌 `AddEffect`）。
                //   ⚠️ 那一支原来在这里 `NoteEnchantBuffGap(… "本仓没有那个等价口")` + `return 0`。
                return RunHandBuffRowOfEnchantment(ctx, owner, carrier, enchantmentCard, row, by);
            }

            List<UnitState> targets;
            if (row.Ca == ChosenBuffSelf)
                targets = new List<UnitState>(1) { carrier };                       // 原版硬写 thisCard
            else
                targets = ResolveBuffOthersTargets(ctx, owner, carrier, actingCard, chosen, row.Ta, enchantmentCard, row);
            if (targets == null || targets.Count == 0) return 0;

            int applied = 0;
            var unresolved = new List<string>(1);
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null) continue;
                applied += ResolveAbilityBuffRows(ctx, owner, t, enchantmentCard, row.Ab, row.Lg,
                                                  isHandBuff: false, by: by, unresolved: unresolved);
            }
            if (unresolved.Count > 0) ReportUnresolved(ctx, by, unresolved);
            return applied;
        }

        /// <summary>同一 `(卡, ab, lg, 原因键)` 只喊一次。</summary>
        static readonly HashSet<string> _b115s5BuffGapNoted = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>`ca 30/40/50` 的出声口（⛔ 走 `UnityEngine.Debug.LogWarning`、不进 `ctx.Log` —— 同 `NoteEnchantRowUnsupported`）。</summary>
        static void NoteEnchantBuffGap(CardDef card, CardDef.TriggerAbilityIndexSpec row, string why)
        {
            string key = (card != null ? (string.IsNullOrEmpty(card.Id) ? card.Name : card.Id) : "?")
                       + "|" + (row != null ? row.Ca : -1) + "|" + (row != null ? row.Ab : -1) + "|" + (row != null ? row.Lg : -1);
            if (!_b115s5BuffGapNoted.Add(key)) return;
            _b115s5EnchantRowsUnsupported++;
            UnityEngine.Debug.LogWarning($"[B115_S5] 附魔「{(card != null ? card.Name : "?")}」的 `ca {(row != null ? row.Ca : -1)}`"
                           + $"（ab{row?.Ab ?? -1}/lg{row?.Lg ?? -1}）：{why}");
        }

        // ================================================================================================
        //  缺口出声 + 诊断计数（**⛔ 不走 `ctx.Log`**）
        // ================================================================================================

        /// <summary>已结算的附魔行（诊断读数，⛔ 不参与任何判据）。</summary>
        internal static int _b115s5EnchantRowsExecuted;
        /// <summary>已入队摘除的附魔条数（同上）。</summary>
        internal static int _b115s5EnchantRemovals;
        /// <summary>「有这一行 `ca`、但本版没有它的执行器」的**不同键**数（同上）。</summary>
        internal static int _b115s5EnchantRowsUnsupported;

        /// <summary>同一 `(卡, trigger, ca)` 只喊一次。</summary>
        static readonly HashSet<string> _b115s5EnchantGapNoted = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>🔴 **出声口**：这一行 `chosenAbility` 在本版**没有执行器** ⇒ 原版这一刻会执行、我们不结算。
        /// <para>⛔ 走 `UnityEngine.Debug.LogWarning`、**不进 `ctx.Log`**：后者会改 `ctx.Events.Count`、进而改
        /// `NetProtocol.Fingerprint`（同 `NoteTriggerRowKindGap` 的纪律）。同一 `(卡, trigger, ca)` 只喊一次。</para>
        /// <para>📊 波 1 之后仍未接的 `ca`（现读 135 行的分布）→ `判据/B115_S5_附魔执行器施工面.md` §三·§四。</para></summary>
        static void NoteEnchantRowUnsupported(CardDef card, int triggerNumber, CardDef.TriggerAbilityIndexSpec row)
        {
            int ca = row != null ? row.Ca : -1;
            string key = (card != null ? (string.IsNullOrEmpty(card.Id) ? card.Name : card.Id) : "?")
                       + "|" + triggerNumber + "|" + ca;
            if (!_b115s5EnchantGapNoted.Add(key)) return;
            _b115s5EnchantRowsUnsupported++;
            UnityEngine.Debug.LogWarning(
                $"[B115_S5] 附魔「{(card != null ? card.Name : "?")}」的 trigger {triggerNumber} 有一行 "
              + $"`chosenAbility {ca}`（ab{row?.Ab ?? -1}/lg{row?.Lg ?? -1}），本版**没有它的执行器** ⇒ "
              + "**本行不结算**（⛔ 不猜；也⛔ 不会与正文重复）。判据 = `AbilityLogic__PlayAbility.c:266/286` 起那个"
              + "按 `chosenAbility` 分派的巨型 if 链；施工面 → `判据/B115_S5_附魔执行器施工面.md` §四。");
        }

        // ================================================================================================
        //  自检门（⚠️ 加在类尾；⛔ 生产侧别调 —— 纪律同 `FireEnchantmentAbilitiesByNumberForTest`）
        // ================================================================================================

        /// <summary>自检门：直接调 `ca 340` 那一格（不经过附魔趟的前置闸，方便单点测）。</summary>
        public static int RunRemoveThisEnchantmentForTriggerForTest(BattleContext ctx, int owner, UnitState carrier,
                                                                   int triggerNumber, CardDef selfTemplate, string by)
        {
            return RunRemoveThisEnchantmentForTrigger(ctx, owner, carrier, triggerNumber, selfTemplate, by);
        }

        /// <summary>自检门：`⛔ 双发闸` 的读数（`true` = 这张模板被让开）—— 灭自证用它钉「闸真的在」。 </summary>
        public static bool IsHandWrittenEnchantmentLaneForTest(CardDef enchantmentCard)
        {
            return IsHandWrittenEnchantmentLane(enchantmentCard);
        }

        // ================================================================================================
        //  🆕 B121-D1（2026-11-03）：附魔模板 `ca` 执行器的**其余 15 档**分派
        // ================================================================================================
        //  判据（第一权威 `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c`，本波逐支现读）：
        //    · 目标表 = `:262` `AbilityLogic__GetTargets(manager, abilityData.targetCriteria(+0x28), abilityData, 1, 0)`
        //      → 存进 `abilityLogic + 0x78`；**每一支都用同一张表 `*puVar1`**；
        //    · `:285` `iVar21 = param_4`（= **当前 trigger**，不是常数）；`:286` `iVar8 = chosenAbility`；
        //    · `:288` 起是巨型 if/switch；每支末尾 `LAB_18081cc87` → `:2743-2745 SetPreviousAbilityTargets`。
        //
        //  ── 本版**能真执行**的三档（数据够、执行体在仓里已有唯一实现 ⇒ 只做分派，⛔ 不抄第二份体）──────
        //    · `ca 140 recallToOwnerHand`     判据 `:541-566`（`case 0x8c`）—— 逐目标 `AddRecallToHand`；
        //                                     ⇒ 复用 `EffectResolver.DoReturn`（回手/回牌库的唯一实现）。
        //    · `ca 320 followAfterResolution` 判据 `:2244`（`if (iVar8 != 0x140) return;`）落到 `:2472-2478`；
        //                                     ⇒ 复用 `RunFollowAfterResolutionRow`（实参逐字对得上：
        //                                        `thisCard = 载体`、`targetCard = 触发时点的目标`）。
        //    · `ca 410 triggerCodex`          判据 `:1685-1699`（`if (iVar8 == 0x19a)`）—— 逐目标 `AddTriggerCodex`；
        //                                     ⇒ 复用 `ActionPump.EnqueueGrantedTrigger`（与 courtesy/relentless 同族）。
        //
        //  ── 其余 12 档（本波 D1 时的状态）→ 🆕 B121-D1B 已经把 6 档接成真执行（见类尾那一节）──────────
        //    ✅ `ca 0 / 10 / 13 / 15 / 20 / 60`：数据面补齐后**真执行**（载荷列 = `enchantment_templates.json`
        //      的 `criteria` / `damage` / `heal` / `mana`；装载口 = `CardDatabase.AttachEnchantmentLogicPayload`；
        //      执行器 → 类尾「B121-D1B 正文载荷落地后的真执行器」那一节）。
        //    ⛔ 仍**不执行**的档：🆕 2026-11-03（`B121-D5R`）已**清零** —— `ca 70 / 100` 也接了
        //      （五支取牌 → 类尾「B121-D5R」那一节）。今天附魔模板里出现过的每个 `ca` 都有执行器；
        //      `IsKnownChosenAbilityWithoutPayload` 保留给**将来新接的档**出声用。
        //    🔴 **2026-11-03 就地订正（铁律 5 · `B121-D1` 原文已过期）**：这里原来写「模板一行 logic 只导出
        //      4 格标量、`targetCriteria` 其余 35 格与 `damageCriteria`/`healCriteria`/`manaBuff`/`summonCriteria`
        //      一个都没导出，`Pa`/`Fm` 装载时就丢」—— **现在全部导出并挂上了**（`CardDef.TriggerAbilityIndexSpec`
        //      新增 `Pa` / `Fm` / `Mult` 三格；36 格走 `CardDef.AttachAbilityTargetCriteria`）。⛔ 别照旧口径当缺口。

        //  🧨 灭自证（本段怎么才能红）：
        //    · 删掉 `:140` / `:320` / `:410` 任一档分派 ⇒ 它对应的 `B121_D1` 断言变红（件数 1 → 0）；
        //    · 删掉 `:0` / `:10` / `:20` / `:60` 任一档分派 ⇒ `B121_D1B` 对应断言变红；
        //    · 把 `IsKnownChosenAbilityWithoutPayload` 改成恒 `false` ⇒ 「数据缺口」计数不涨 ⇒ 负例红；
        //    · 把 `ResolveEnchantmentRowTargets` 的 `ta=30` 改成「取第一个己方单位」⇒ ca 140 的那条
        //      「点定的那个回手、别的没动」红（灭自证：两者结构上不可能同时满足）。

        /// <summary>`AbilityEffect.drawCard = 0`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`）。</summary>
        internal const int ChosenDrawCard = 0;
        /// <summary>`AbilityEffect.doDamage = 10`。</summary>
        internal const int ChosenDoDamage = 10;
        /// <summary>`AbilityEffect.doDamageFast = 13`。</summary>
        internal const int ChosenDoDamageFast = 13;
        /// <summary>`AbilityEffect.doDamageAndDestroySelf = 15`。</summary>
        internal const int ChosenDamageAndDestroySelf = 15;
        /// <summary>`AbilityEffect.doHeal = 20`。</summary>
        internal const int ChosenDoHeal = 20;
        /// <summary>`AbilityEffect.buffMana = 60`。</summary>
        internal const int ChosenBuffMana = 60;
        /// <summary>`AbilityEffect.summonUnit = 70`。</summary>
        internal const int ChosenSummonUnit = 70;
        /// <summary>`AbilityEffect.createCardInHand = 100`。</summary>
        internal const int ChosenCreateCardInHand = 100;
        /// <summary>`AbilityEffect.stun = 130`。</summary>
        internal const int ChosenStun = 130;
        /// <summary>`AbilityEffect.recallToOwnerHand = 140`。</summary>
        internal const int ChosenRecallToOwnerHand = 140;
        /// <summary>`AbilityEffect.stealMinionSelf = 185`。</summary>
        internal const int ChosenStealMinionSelf = 185;
        /// <summary>`AbilityEffect.scriptedActingCardAttack = 236`。</summary>
        internal const int ChosenScriptedActingCardAttack = 236;
        /// <summary>`AbilityEffect.scriptedSelfCardAttackDontCount = 238`。</summary>
        internal const int ChosenScriptedSelfCardAttackDontCount = 238;
        /// <summary>`AbilityEffect.followAfterResolution = 320`。</summary>
        internal const int ChosenFollowAfterResolution = 320;
        /// <summary>`AbilityEffect.triggerCodex = 410`。</summary>
        internal const int ChosenTriggerCodex = 410;

        /// <summary>`chosenAbility` 已知、但本版**仍然没有可结算的载荷/执行体**的那几档
        /// （逐支原因 → <see cref="EnchantRowGapReason"/>）。⛔ 别把新接上的档留在这里。
        /// <para>🆕 2026-11-03（`B121-D1B`）：`0 / 10 / 13 / 15 / 20 / 60` 六档**已移出**（载荷落地 + 真执行器；
        /// 见类尾「B121-D1B」那一节）；🆕 2026-11-03（`B121-D5`）：`130 / 185 / 236 / 238` 四档**已移出**
        /// （执行体补齐，见类尾「B121-D5」那一节）；🆕 2026-11-03（`B121-D5R`）：最后的 `70 / 100` 两档
        /// **也已移出**（五支取牌 + `DeployFree` / `AddCardToHandWithinLimit` 落点，见类尾「B121-D5R」那一节）
        /// ⇒ **本表今天为空**（附魔模板里出现过的每个 `ca` 都有执行器）。
        /// ⚠️ 保留这个函数 = **未来新 `ca` 的入口**：原版还有几十档我们没有，⛔ 别把没接的档静默掉。</para></summary>
        static bool IsKnownChosenAbilityWithoutPayload(int ca)
        {
            switch (ca)
            {
                default:
                    return false;
            }
        }

        /// <summary>那一档**具体**缺什么（写进出声口；判据出处逐条见本段头注）。</summary>
        static string EnchantRowGapReason(int ca)
        {
            switch (ca)
            {
                default:
                    return "本版没有这一档的载荷 / 执行体";
            }
        }

        /// <summary>同一 `(卡, trigger, ca)` 只喊一次（数据缺口语；⛔ 不走 `ctx.Log`，同 `NoteEnchantRowUnsupported`）。</summary>
        static readonly HashSet<string> _b121d1EnchantDataGapNoted = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>「已知 `ca`、但载荷/执行体缺」的**不同键**数（诊断读数，⛔ 不参与判据）。</summary>
        internal static int _b121d1EnchantRowsDataGap;

        /// <summary>数据缺口语：与「本版没有这个执行器」区分开 —— 这一档**原版有、我们也知道它要什么**，
        /// 只是数据列/执行体还没落。⛔ 静默 = 红线。</summary>
        static void NoteEnchantRowDataGap(CardDef card, int triggerNumber, CardDef.TriggerAbilityIndexSpec row)
        {
            int ca = row != null ? row.Ca : -1;
            string key = (card != null ? (string.IsNullOrEmpty(card.Id) ? card.Name : card.Id) : "?")
                       + "|" + triggerNumber + "|" + ca;
            if (!_b121d1EnchantDataGapNoted.Add(key)) return;
            _b121d1EnchantRowsDataGap++;
            UnityEngine.Debug.LogWarning(
                $"[B121_D1] 附魔「{(card != null ? card.Name : "?")}」的 trigger {triggerNumber} 有一行 "
              + $"`chosenAbility {ca}`（ab{row?.Ab ?? -1}/lg{row?.Lg ?? -1}）：{EnchantRowGapReason(ca)} ⇒ **本行不结算**。"
              + "（判据 `AbilityLogic__PlayAbility.c` 的 `chosenAbility` 分派链；补法 → "
              + "`资料/完全复刻/报告/B121_D1_附魔执行器.md` §还差什么）");
        }

        /// <summary>解这一行 `abilityLogic` 的**目标表**（原版 `:262` `AbilityLogic.GetTargets(targetCriteria,…)`）。
        ///
        /// <para>**只接能用现有数据判出来的那几档**（逐档判据见下）；其余档出声并返回 `null`（调用方不结算）。</para>
        /// <list type="bullet">
        /// <item>`ta 0 any` → `GetTargets.c:884-891`：`GetUnitsInPlayList(… inclHero 1, inclMinions 1, inclWeapons 1,
        ///   inclRemnants 1, inclPlayer 1, inclEnemy 1, includeDying 0)` = **双方全场的单位**（含督军/残骸）；
        ///   ⚠️ 2026-11-03 现读；与 `B121-03` 同一条判据。</item>
        /// <item>`ta 20 self` → `:184-194` `List.Add(thisCard)`；附魔路上 `thisCard` = **载体**。</item>
        /// <item>`ta 80 actingCard` → `:761-769`：取**触发源**（= 本方法的 `actingCard` 形参，
        ///   原版 `abilityData.actingCard`）。🆕 `B121-D5` 补 —— 改前这一档只有 `ca 30` 的旧四档 switch 认得。</item>
        /// <item>`ta 30 target` / `ta 220` → `:192-207`：整张 `GetTargets` 只有这两档读调用方点定的那个目标。</item>
        /// <item>`ta 120 friendlyHero` / `130 enemyHero` → `:721-736` / `:738-750`（`GetHero(actingCard.player)` /
        ///   `GetHero(!actingCard.player)`）；附魔路上 actingCard = 触发源，其方 = `owner`。</item>
        /// <item>`ta 140 previousAbilityTargets` / `150 previousAbilityFiltered` → `:849-883`（`AddRange` 上一条 ability 的表）。</item>
        /// </list>
        /// <returns>`null` = 判不了（已出声）；空表 = 判得出、但一个目标都没有。</returns>
        static List<UnitState> ResolveEnchantmentRowTargets(BattleContext ctx, int owner, UnitState carrier,
                                                            UnitState chosen, int ta, CardDef card,
                                                            CardDef.TriggerAbilityIndexSpec row, string caName,
                                                            UnitState actingCard = null)
        {
            if (ctx == null || ctx.Players == null || ctx.Players.Length < 2) return null;
            switch (ta)
            {
                case 20:   // self → thisCard（= 载体）
                    return carrier != null ? new List<UnitState>(1) { carrier } : new List<UnitState>();
                case 80:   // actingCard（`GetTargets.c:761-769`：`0x50`/`0x28` 一起落到 actingCard 形参那一格）
                    return actingCard != null ? new List<UnitState>(1) { actingCard } : new List<UnitState>();
                case 30:   // target（读调用方点定的那个）
                case 220:
                    return chosen != null ? new List<UnitState>(1) { chosen } : new List<UnitState>();
                case 120:
                    {
                        var w = WarlordOfSide(ctx, owner);
                        return w != null ? new List<UnitState>(1) { w } : new List<UnitState>();
                    }
                case 130:
                    {
                        var w = WarlordOfSide(ctx, 1 - owner);
                        return w != null ? new List<UnitState>(1) { w } : new List<UnitState>();
                    }
                case 0:    // any → 双方全场（含督军 + 残骸）
                    {
                        var pool = new List<UnitState>(8);
                        AddSide(pool, ctx.Players[owner], false, false, -1, includeRemnants: true);
                        AddSide(pool, ctx.Players[1 - owner], true, false, owner, includeRemnants: true);
                        var w0 = WarlordOfSide(ctx, owner);
                        if (w0 != null && w0.IsAlive && !pool.Contains(w0)) pool.Add(w0);
                        var w1 = WarlordOfSide(ctx, 1 - owner);
                        if (w1 != null && w1.IsAlive && !pool.Contains(w1)) pool.Add(w1);
                        return pool;
                    }
                case 140:  // previousAbilityTargets
                case 150:
                    {
                        var list = new List<UnitState>(4);
                        var prev = ctx.PreviousAbilityTargets;
                        if (prev != null)
                            for (int i = 0; i < prev.Count; i++)
                            {
                                var r0 = prev[i];
                                if (r0.Inst == null && r0.Unit == null) continue;
                                var u = r0.Unit != null ? r0.Unit : r0.ResolveUnit(ctx);
                                if (u != null && !list.Contains(u)) list.Add(u);
                            }
                        return list;
                    }
                default:
                    NoteEnchantRowTargetGap(card, row, caName, ta);
                    return null;
            }
        }

        /// <summary>目标集判不了时的出声（同 `NoteEnchantRowDataGap` 的去重纪律）。</summary>
        static void NoteEnchantRowTargetGap(CardDef card, CardDef.TriggerAbilityIndexSpec row, string caName, int ta)
        {
            string key = "tg|" + (card != null ? (string.IsNullOrEmpty(card.Id) ? card.Name : card.Id) : "?")
                       + "|" + (row != null ? row.Ca : -1) + "|" + ta;
            if (!_b121d1EnchantDataGapNoted.Add(key)) return;
            _b121d1EnchantRowsDataGap++;
            UnityEngine.Debug.LogWarning(
                $"[B121_D1] 附魔「{(card != null ? card.Name : "?")}」的 `ca {caName}`：目标集是 `ta = {ta}`，"
              + "而这一档要读 `targetCriteria` 的其余格（`friendlyUnits` / `enemyUnits` / `includeHero` / …）"
              + "才判得出来 —— `enchantment_templates.json` 只导出了 `ta` 一格 ⇒ **本行不结算**（⛔ 不猜「全体」或「随机一个」）。"
              + "判据 `AbilityLogic__GetTargets.c` 各分支；补法 → `资料/完全复刻/报告/B121_D1_附魔执行器.md` §还差什么");
        }

        // ================================================================================================
        //  ca 140 recallToOwnerHand —— 逐目标回**其所有者**手牌（判据 `.c:541-566`）
        // ================================================================================================
        //  原版（`case 0x8c`）：`while (targets)` → `AddRecallToHand(manager, target, *(byte*)(target + 0x40)
        //  /* target 自己的阵营 */, priority(+0x44), checkIfCasterInPlay(+0x74), thisCard(+0x10), block(+0x48), 0)`。
        //  ⇒ **回的是目标自己的所有者手牌**（不是施放者），且**逐目标各排一条**。
        //  我们的等价体 = `EffectResolver.DoReturn`（回手 / 回牌库 / 弃牌堆三支的唯一实现，含
        //  `BoardSlots.RemoveAt` 外侧补位、`ResetToValuesInHand(true)`、`Auras.Recompose`、210 广播两跳）。
        //  ⛔ 别在这里再写一份「挪棋盘 → 进手牌」。
        internal static int RunRecallToOwnerHandRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                                 CardDef enchantmentCard,
                                                                 CardDef.TriggerAbilityIndexSpec row,
                                                                 UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var targets = ResolveEnchantmentRowTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                       "140 recallToOwnerHand");
            if (targets == null || targets.Count == 0) return 0;

            int moved = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.IsAlive) continue;
                // 复用 `DoReturn`：`SubjectTargetsAffected = 30` + `Count = 1` ⇒ `ResolveTargets` 的
                // 「调用方已点定目标」那一支会把表换成 `chosen`（判据 `AbilityLogic__GetTargets.c:192-207`）。
                var op = new EffectOp
                {
                    Verb = "return",
                    Dest = "hand",
                    Source = enchantmentCard.Name ?? "附魔",
                    SubjectTargetsAffected = 30,
                    Target = new EffectTargetSpec
                    {
                        Raw = "(附魔 ca 140 recallToOwnerHand)",
                        Side = "any", Kind = "unit", Count = 1,
                    },
                };
                var unresolved = new List<string>(1);
                if (DoReturn(ctx, owner, by, op, t, unresolved))
                {
                    moved++;
                }
                else if (unresolved.Count > 0)
                {
                    ReportUnresolved(ctx, by, unresolved);
                }
            }
            ctx.Log($"{by}：`chosenAbility 140`（recallToOwnerHand）⇒ {moved}/{targets.Count} 张回手"
                  + "（原版 `AbilityLogic__PlayAbility.c:541-566` 逐目标 `AddRecallToHand`；"
                  + "结算体 = `RuleCore.DoReturn`）");
            return moved;
        }

        // ================================================================================================
        //  ca 320 followAfterResolution —— 登记「这次结算之后再来一次」（判据 `.c:2244` → `:2472-2478`）
        // ================================================================================================
        //  原版：`if (iVar8 != 0x140) return;` ⇒ `AddFollowAfterResolution(manager, thisCard(+0x10),
        //  targetCard(+0x20), priority(+0x44), checkIfCasterInPlay(+0x74), block(+0x48), 0)`。
        //  ⚠️ 这一支**不读目标表**（`*puVar1` 一次都没用）—— 直接传 `thisCard` / `targetCard` 两格。
        //  我们的等价体 = `RunFollowAfterResolutionRow(ctx, p, u, chosen)`：它发 `trigger 410`（`FollowUpResolutionPlayed`）。
        internal static int RunFollowAfterResolutionOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                                  CardDef enchantmentCard,
                                                                  CardDef.TriggerAbilityIndexSpec row,
                                                                  UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null) return 0;
            int fired = RunFollowAfterResolutionRow(ctx, owner, carrier, chosen);
            return fired > 0 ? 1 : 0;
        }

        // ================================================================================================
        //  ca 410 triggerCodex —— 逐目标登记一条 codex 触发（判据 `.c:1685-1699`）
        // ================================================================================================
        //  原版（`case 0x19a`）：`while (targets)` → `AddTriggerCodex(manager, target, thisCard(+0x10),
        //  priority(+0x44), 1)`；出队体 = `ResolveTriggerCodex` → `CardScript__TriggerCodex.c:18-30`
        //  （`OnTrigger(0x91 = 145 Codex, this=target, actingCard=thisCard, targetCard=thisCard)`）。
        //  我们的等价体 = `ActionPump.EnqueueGrantedTrigger(kw = KeywordTable.Codex, target, actingCard: carrier, lane)`：
        //  队列体正是 `FireTriggerAt(target, "codex", …, chosen:null, subject: actingCard)`。
        //  ⚠️ **如实标注**：原版 `BattleAction.type = 0x45`，而 `EnqueueGrantedTrigger` 只给
        //  courtesy(0x41) / relentless(0x3e) 回填 `actionType`（账 L490「我们的入队点还没回填 ActionType」）
        //  ⇒ 本档沿用 `0`。⛔ 别为此去改 `ActionPump`（本代理白名单外；要改请落补丁）。
        internal static int RunTriggerCodexRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                            CardDef enchantmentCard,
                                                            CardDef.TriggerAbilityIndexSpec row,
                                                            UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            // 🆕 2026-11-03（`B121-D5` · `B121-D1B-02`）：换到**统一解算口** —— 改前走 `ResolveEnchantmentRowTargets`
            //   （只认 `ta`）⇒ `ta = 10` 那一行（`Duty's End`，36 格 `friendlyUnits=1 ∧ includeHero=1 ∧ includeMinions=1`）
            //   明明「数据够了」却仍然 `NoteEnchantRowTargetGap` 出声、不结算。
            var targets = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                    "410 triggerCodex");
            if (targets == null || targets.Count == 0) return 0;

            var lane = ListenerLane(enchantmentCard, "Codex");   // = 原版 `abilityData.ability.priority(+0x44)`
            int queued = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null) continue;
                if (EnqueueGrantedTrigger(ctx, KeywordTable.Codex, t, actingCard: carrier, lane: lane,
                                          label: $"{carrier.Name ?? "?"}→{t.Name ?? "?"}·codex"))
                    queued++;
            }
            ctx.Log($"{by}：`chosenAbility 410`（triggerCodex）⇒ 给 {queued}/{targets.Count} 个目标各登记一条 `codex` 触发"
                  + "（原版 `AbilityLogic__PlayAbility.c:1685-1699` 逐目标 `AddTriggerCodex`；"
                  + "队列体 = `CardScript__TriggerCodex.c:18-30`）");
            return queued;
        }

        // ================================================================================================
        //  自检门（⚠️ 加在类尾；⛔ 生产侧别调 —— 纪律同 `RunRemoveThisEnchantmentForTriggerForTest`）
        // ================================================================================================

        /// <summary>自检门：直接走 `RunEnchantmentLogicRows`（不经过附魔趟的前置闸，方便单点测 `ca` 分派）。</summary>
        public static int RunEnchantmentLogicRowsForTest(BattleContext ctx, ActiveEnchantment e, UnitState carrier,
                                                         int owner, int slot, int triggerNumber, string tn,
                                                         UnitState actingCard, UnitState targetCard)
        {
            return RunEnchantmentLogicRows(ctx, e, carrier, owner, slot, triggerNumber, tn, actingCard, targetCard);
        }

        /// <summary>自检门：「已知 `ca` 但载荷缺」的去重键数（读数；用作「不静默」的判别式）。</summary>
        public static int EnchantRowsDataGapForTest { get { return _b121d1EnchantRowsDataGap; } }
        // ================================================================================================
        //  🆕 B121-D1B（2026-11-03）：正文载荷落地后的**真执行器**（`ca 0 / 10 / 13 / 15 / 20 / 60`）
        // ================================================================================================
        //  判据（第一权威 `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c`，逐支现读）：
        //   · `ca 0 drawCard`     `:1439-1454`：`pa ∈ {0 Both, 10 Self}` ⇒ 己方抽；`pa ∈ {0, 20 Enemy}` ⇒ 敌方抽；
        //                          张数 = `local_178`（`:180-236`；`multiplier.ta == 0` ⇒ 1）再乘 `furtherMultiplier`。
        //   · `ca 10/13 doDamage` `:1461-1508`：`damageCriteria` 四格 → `GetValueFromCriteria` 算**一次**数值 →
        //                          一个 `DamageInfo` → `DealMultiDamage` 打**整张目标表**（`ca 13` 多一个 fast 位）。
        //   · `ca 15 damageAndDestroySelf` `:1629-1674`：同上，但走 `DealMultiDamageAndDestroySelf` —— 伤害之后
        //                          **施放者（`abilityData.actingCard`）自己离场**（等价体 = `DestroyUnitSelfCore`）。
        //   · `ca 20 doHeal`      `:1262-1306`：`healCriteria` 三格 → `GetValueFromCriteria`；
        //                          `targets.Count < 2` ⇒ `HealOneCharacter(targets[0])`，否则 `HealMultipleCharacters(targets)`。
        //   · `ca 60 buffMana`    `:685-721`：`manaBuff` 十一格 → `AddManaBuff` → `ResolveManaBuff` 按
        //                          `friendlyPlayer` / `enemyPlayer` 两侧各跑一次 `ProcessManaBuff`；后者的优先级 =
        //                          **spiritStone → faith → questPoints → 普通法力（createMana / refillMana）**。
        //  ⛔ **不做的档**：🆕 2026-11-03（`B121-D5R`）已**清零** —— `ca 70/100` 接在类尾「B121-D5R」那一节
        //     （`summonCriteria` 五支取牌 + `DeployFree` / `AddCardToHandWithinLimit` 落点）。
        //  🧨 灭自证：删掉任一档分派 ⇒ `B121_D1B_Assert` 对应那条（件数 1 → 0）变红。

        /// <summary>`ca 0 drawCard` 的行执行器（判据 `.c:1439-1454`）。
        /// <para>⚠️ `multiplier.ta != 0` 的档（`local_178` = 目标表长度）本版**没有导出**那 36 格 ⇒ 只在
        /// `Mult == 0`（= 1 张）时执行；其余出声不结算（⛔ 不拿 1 顶）。</para></summary>
        internal static int RunDrawCardRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                        CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                        string by)
        {
            if (ctx == null || enchantmentCard == null || row == null) return 0;
            int pa = row.Pa;
            if (pa != 0 && pa != 10 && pa != 20)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row, $"`ca 0 drawCard` 的 `playersAffected = {pa}` 不认识"
                    + "（原版只有 0 Both / 10 Self / 20 Enemy 三档）⇒ **本行不结算**");
                return 0;
            }
            if (row.Mult != 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`ca 0 drawCard` 的 `multiplier.targetsAffected = " + row.Mult + "`（≠ 0 any）⇒ 抽牌张数"
                  + "取自**乘数目标表**（`PlayAbility.c:180-236` 的 `local_178`），而乘数那 36 格没导出 ⇒ **本行不结算**");
                return 0;
            }
            int count = row.Fm > 0 ? row.Fm : 1;
            int drawn = 0;
            if (pa == 0 || pa == 10) drawn += DrawForSide(ctx, owner, count, by);
            if (pa == 0 || pa == 20) drawn += DrawForSide(ctx, 1 - owner, count, by);
            ctx.Log($"{by}：`chosenAbility 0`（drawCard，pa {(pa == 0 ? "Both" : (pa == 10 ? "Self" : "Enemy"))}）"
                  + $"⇒ 抽 {count} 张 × {(pa == 0 ? 2 : 1)} 侧 = {drawn} 张"
                  + "（原版 `AbilityLogic__PlayAbility.c:1439-1454`；`AddDrawCard.c` 的 `multiplier.ta == 0` 支）");
            return drawn;
        }

        /// <summary>让某一方抽 <paramref name="count"/> 张（复用 `RuleCore.Draw` —— 疲劳 / 手牌上限 / 广播的唯一实现）。</summary>
        static int DrawForSide(BattleContext ctx, int side, int count, string by)
        {
            int n = 0;
            for (int i = 0; i < count && !ctx.IsOver; i++) { Draw(ctx, side); n++; }
            return n;
        }

        /// <summary>`ca 10 / 13 doDamage` + `ca 15 damageAndDestroySelf` 的行执行器
        /// （判据 `.c:1461-1508` / `:1629-1674`）。</summary>
        /// <param name="destroySelf">`ca 15` 那一档：伤害结算后**施放者自己离场**
        /// （原版 `DealMultiDamageAndDestroySelf` 传的是 `abilityData.actingCard(+0x18)` ⇒ 等价体 `DestroyUnitSelfCore`）。</param>
        internal static int RunDoDamageRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                        CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                        UnitState actingCard, UnitState chosen, bool destroySelf,
                                                        string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var dmg = enchantmentCard.AbilityDamageAt(row.Ab, row.Lg);
            if (dmg == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`damageCriteria`（`minDamage` / `maxDamage` / `valueOptions` / `damageType`）在运行时**取不到**"
                  + "（旧表 / 手写夹具）⇒ **本行不结算**");
                return 0;
            }
            var targets = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                    "doDamage");
            if (targets == null || targets.Count == 0) return 0;

            int factor = row.Fm > 0 ? row.Fm : 1;
            if (dmg.ValueOptions == 80 && row.Mult != 0)
            {
                // 原版 `GetValueFromCriteria.c:113-116` 的 `param_8` = `local_178` = **乘数目标表长度 × furtherMultiplier**；
                // 乘数那 36 格没导出 ⇒ 只给得出 `Fm` 那一半（⛔ 不拿 1 顶目标数）。
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`valueOptions = 80`（`min + max × mult`）的乘数取自 `multiplier` 的目标表"
                  + $"（`mult = {row.Mult}`），而乘数那 36 格没导出 ⇒ **本行不结算**");
                return 0;
            }
            int amount; string gap;
            if (!TryResolveCriteriaValue(ctx, dmg.ValueOptions, dmg.Min, dmg.Max, carrier, chosen, factor,
                                         out amount, out gap))
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row, "`ca doDamage`：" + gap + " ⇒ **本行不结算**");
                return 0;
            }
            // `CardScript__GetAdjustedDamage.c:23-30`：`damageType` 的 `0x1420` 掩码（5 / 10 / 12）⇒ **逐目标**掷
            // `[min, max]`。⚠️ 原版 `PlayAbility.c:1477-1490` 还有一条：`valueOptions != 90 ∧ damageType == 5` ⇒
            // `LogError` + 把类型清成 0（= 不掷）⇒ 5 档**只有 90 档才掷**。
            bool rollPerTarget = (dmg.Type == 10 || dmg.Type == 12 || (dmg.Type == 5 && dmg.ValueOptions == 90));

            int hits = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.IsAlive) continue;
                int v = amount;
                if (rollPerTarget)
                {
                    int lo = dmg.Min, hi = dmg.Max;
                    if (hi < lo) hi = lo;
                    v = (hi > lo) ? ctx.GetRandomInt(lo, hi + 1) : lo;
                }
                Hurt(ctx, t, v, by + "·附魔伤害", actingCard: carrier,
                     deathType: DeathTypeOfSource(ctx, dmg.Type), kind: DamageKind.Spell);
                hits++;
            }

            if (destroySelf)
            {
                int o, s;
                if (actingCard != null && FindUnit(ctx, actingCard, out o, out s)) DestroyUnitSelfCore(ctx, o, actingCard);
                else ctx.Log($"{by}：`ca 15 damageAndDestroySelf` 的施放者（actingCard）本刻不在场 ⇒ 只结算伤害"
                           + "（原版 `DealMultiDamageAndDestroySelf` 的 `SetAsUnitDestroyed(actingCard)` 那一跳没有对象）");
            }
            ctx.Log($"{by}：`chosenAbility {(destroySelf ? 15 : (row.Ca == ChosenDoDamageFast ? 13 : 10))}`"
                  + $"（{RowNameOf(row.Ca)}）⇒ 对 {hits}/{targets.Count} 张各 {amount} 点伤害"
                  + (rollPerTarget ? "（90 档逐目标掷）" : "")
                  + "（原版 `AbilityLogic__PlayAbility.c:1461-1508`；伤害口 = `RuleCore.Hurt`）");
            return hits;
        }

        /// <summary>`ca 20 doHeal` 的行执行器（判据 `.c:1262-1306`）。</summary>
        internal static int RunHealRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                    CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                    UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var heal = enchantmentCard.AbilityHealAt(row.Ab, row.Lg);
            if (heal == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`healCriteria`（`minValue` / `maxValue` / `valueOptions`）在运行时**取不到** ⇒ **本行不结算**");
                return 0;
            }
            var targets = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                    "doHeal");
            if (targets == null || targets.Count == 0) return 0;
            int factor = row.Fm > 0 ? row.Fm : 1;
            int amount; string gap;
            if (!TryResolveCriteriaValue(ctx, heal.ValueOptions, heal.Min, heal.Max, carrier, chosen, factor,
                                         out amount, out gap))
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row, "`ca 20 doHeal`：" + gap + " ⇒ **本行不结算**");
                return 0;
            }
            // `.c:1272-1305`：`targets.Count < 2` ⇒ 只治 `targets[0]`（`HealOneCharacter`）；否则 `HealMultipleCharacters`。
            int healed = 0;
            if (targets.Count < 2)
            {
                var one = targets[0];
                if (one != null && one.IsAlive) healed += HealUnit(ctx, one, amount, false) > 0 ? 1 : 0;
            }
            else
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (t == null || !t.IsAlive) continue;
                    HealUnit(ctx, t, amount, false);
                    healed++;
                }
            }
            ctx.Log($"{by}：`chosenAbility 20`（doHeal）⇒ {healed}/{targets.Count} 张各治疗 {amount}"
                  + "（原版 `AbilityLogic__PlayAbility.c:1262-1306`；治疗口 = `RuleCore.HealUnit`）");
            return healed;
        }

        /// <summary>`ca 60 buffMana` 的行执行器（判据 `.c:685-721` + `ResolveManaBuff.c` + `ProcessManaBuff.c`）。</summary>
        internal static int RunManaBuffRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                        CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                        string by)
        {
            if (ctx == null || enchantmentCard == null || row == null) return 0;
            var mb = enchantmentCard.AbilityManaBuffAt(row.Ab, row.Lg);
            if (mb == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row, "`manaBuff`（十一格）在运行时**取不到** ⇒ **本行不结算**");
                return 0;
            }
            int amount = mb.ManaAmount * (row.Fm > 0 ? row.Fm : 1);   // `.c:706`：`manaAmount *= local_178`
            if (amount <= 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row, $"`ca 60 buffMana` 的 `manaAmount × fm = {amount}`"
                    + "（≤ 0）⇒ **本行不结算**");
                return 0;
            }
            int applied = 0;
            if (mb.FriendlyPlayer != 0) applied += ProcessManaBuffForSide(ctx, owner, carrier, row, mb, amount, by);
            if (mb.EnemyPlayer != 0) applied += ProcessManaBuffForSide(ctx, 1 - owner, carrier, row, mb, amount, by);
            if (mb.FriendlyPlayer == 0 && mb.EnemyPlayer == 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`ca 60 buffMana` 的 `friendlyPlayer` / `enemyPlayer` 都是 0 ⇒ 原版 `ResolveManaBuff.c:94-104`"
                  + " **一条都不跑**（⛔ 不猜给谁）");
            }
            ctx.Log($"{by}：`chosenAbility 60`（buffMana）⇒ {applied} 侧结算"
                  + "（原版 `AbilityLogic__PlayAbility.c:685-721` → `ProcessManaBuff.c`）");
            return applied;
        }

        /// <summary>`ProcessManaBuff`（一侧）—— 优先级照原版 `:38-104` 的 else-if 链：
        /// `spiritStoneMana` → `faithMana` → `questPoints` → 普通法力（`createMana` / `refillMana`）。</summary>
        static int ProcessManaBuffForSide(BattleContext ctx, int side, UnitState carrier,
                                          CardDef.TriggerAbilityIndexSpec row, CardDef.AbilityManaBuffSpec mb,
                                          int amount, string by)
        {
            var unresolved = new List<string>(1);
            var op = new EffectOp { Source = "附魔 buffMana", Amount = amount };
            bool ok;
            if (mb.SpiritStoneMana > 0)
                ok = DoFactionResource(ctx, side, by, op, "spirit", unresolved);       // `ProcessManaBuff.c:88-104`
            else if (mb.FaithMana > 0)
                ok = DoFactionResource(ctx, side, by, op, "faith", unresolved);        // `:66-87`
            else if (mb.QuestPoints > 0)
                ok = DoFactionResource(ctx, side, by, op, "quest", unresolved);        // `:40-65`
            else if (mb.CreateMana > 0)
                ok = DoEnergy(ctx, side, by, op, unresolved);                          // `AddNewMana`（永久抬上限）
            else if (mb.RefillMana > 0)
                ok = DoRefill(ctx, side, by, op);                                      // `RefillOneMana`
            else
            {
                NoteEnchantRowDataGapEx(null, row, "`ca 60 buffMana` 的十一格里**没有任何一个生效位**"
                    + "（spiritStone / faith / questPoints / createMana / refillMana 全 0）⇒ **本侧不结算**");
                return 0;
            }
            if (unresolved.Count > 0) ReportUnresolved(ctx, by, unresolved);
            return ok ? 1 : 0;
        }

        /// <summary>`GetValueFromCriteria`（`d:/2/tools/decomp_full/AbilityLogic__GetValueFromCriteria.c`）的
        /// 可读子集：`0` 固定/区间 · `10/20/30` 取**目标**的近战/远程/生命 · `40/41/50` 取**载体**的近战/远程/生命 ·
        /// `80` `min + max × 乘数` · `90` 取 `min`（逐目标掷由调用方做）。
        /// <para>返回值 = `GetValueFromCriteria` 的同一句：`min == 0` ⇒ 直接给取到的值；否则 `min × 值`。</para>
        /// <para>`false` = 这一档本版读不出来（`out gap` 说清缺什么），调用方**不结算**。</para></summary>
        static bool TryResolveCriteriaValue(BattleContext ctx, int opt, int min, int max,
                                            UnitState thisCard, UnitState targetCard, int factor,
                                            out int value, out string gap)
        {
            value = 0; gap = null;
            switch (opt)
            {
                case 0:   // `:57-60`：`min == max` ⇒ min；否则 `GetRandomInt(min, max + 1)`
                    value = (min == max) ? min : ctx.GetRandomInt(min, max + 1);
                    return true;
                case 10:  // `:63`：`targetCard.CurrentMeleeAttack`
                case 20:  // `:72`：`targetCard.CurrentRangeAttack`
                case 30:  // `:78`：`targetCard` 的当前生命
                    if (targetCard == null) { gap = $"`valueOptions = {opt}` 要读**卡目标**的属性，这一次没有点选目标"; return false; }
                    value = (opt == 10) ? targetCard.CurrentMeleeAttack
                          : (opt == 20) ? targetCard.CurrentRangeAttack : targetCard.Health;
                    if (min != 0) value *= min;
                    return true;
                case 40:  // `:87`：`thisCard.CurrentMeleeAttack`
                case 41:  // `:97`：`thisCard.CurrentRangeAttack`
                case 50:  // `:104`：`thisCard` 的当前生命
                    if (thisCard == null) { gap = $"`valueOptions = {opt}` 要读**载体**的属性，本刻取不到载体"; return false; }
                    value = (opt == 40) ? thisCard.CurrentMeleeAttack
                          : (opt == 41) ? thisCard.CurrentRangeAttack : thisCard.Health;
                    if (min != 0) value *= min;
                    return true;
                case 80:  // `:113-116`：`min + max × multiplierFactor`（乘数**已折进数值**，伤害只结算一次）
                    value = min + max * factor;
                    return true;
                case 90:  // `:120`：先给 `min`，逐目标掷在 `CardScript__GetAdjustedDamage.c:23-30`
                    value = min;
                    return true;
                default:
                    gap = $"`valueOptions = {opt}` 不在本版读得出来的档里"
                        + "（已实现 0 / 10 / 20 / 30 / 40 / 41 / 50 / 80 / 90；其余见 `AbilityLogic__GetValueFromCriteria.c`）";
                    return false;
            }
        }

        /// <summary>这一档 `ta` 在原版 `AbilityLogic__GetTargets.c` 里**会不会**再走末尾那段
        /// `CheckIfMeetsCriteria`（`:1086-1095`）+ `maxTargets` 裁剪。
        /// <para>判据 = **分支出口**：`LAB_180818485: return *puVar13;` 是「建完池直接返回」，
        /// 下列各档都 `goto` 它 ⇒ **不筛不裁**：`20 self` · `30 target` · `220` · `40/80 actingCard` ·
        /// `120 friendlyHero` · `130 enemyHero` · `140/150 previousAbilityTargets`。</para>
        /// <para>其余档（尤其 `0 any` / `10 allFiltered` / `210` / `90 targetIfMeetsCriteria`）落到那段循环 ⇒ 要筛。</para></summary>
        static bool TaGoesThroughCriteriaFilter(int ta)
        {
            switch (ta)
            {
                case 20: case 30: case 40: case 80: case 120: case 130: case 140: case 150: case 220:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>附魔目标的**统一解算口**：先按 `ta` 建池（原版 `AbilityLogic__GetTargets.c` 的建池那一跳），
        /// 再用 `TargetCriteriaEval.Matches`（= `FilterMethods__CheckIfMeetsCriteria` 的唯一实现）逐候选筛，
        /// 最后按 `maxTargets` / `targetSelectMethod` 裁剪（`GetTargets.c:1174-1300`）。
        ///
        /// <para>没有 36 格（旧表 / 手写夹具）⇒ **退回** <see cref="ResolveEnchantmentRowTargets"/> 那八档
        /// （它们的判据只有 `ta` 一格）；`ta 10 / 210` 在那一支里仍然判不了 ⇒ 出声返回 `null`。</para>
        /// </summary>
        static List<UnitState> ResolveEnchantmentTargets(BattleContext ctx, int owner, UnitState carrier,
                                                         UnitState chosen, int ta, CardDef card,
                                                         CardDef.TriggerAbilityIndexSpec row, string caName,
                                                         UnitState actingCard = null)
        {
            if (ctx == null || ctx.Players == null || ctx.Players.Length < 2) return null;
            List<UnitState> pool;
            if (ta == 10 || ta == 210)
                pool = AllUnitsBothSides(ctx, owner);
            else
            {
                pool = ResolveEnchantmentRowTargets(ctx, owner, carrier, chosen, ta, card, row, caName, actingCard);
                if (pool == null) return null;
            }

            var set = card != null ? card.AbilityTargetCriteriaAt(row.Ab, row.Lg) : null;
            // 🔴 2026-11-03（`B121-D5` · 铁律 5 就地订正）：原版 `GetTargets.c` 里**有一批 `ta` 建完池就 `return`**
            //   —— `LAB_180818485: return *puVar13;`（**不经过** `:1086-1095` 的 `CheckIfMeetsCriteria` 循环、
            //   也不裁 `maxTargets`）。逐档现读：`0x14 self(20)` · `0x1e target(30)` / `0xdc(220)` ·
            //   `0x28 actingCard(40/80)` · `0x78 friendlyHero(120)` · `0x82 enemyHero(130)` ·
            //   `0x8c/0x96 previousAbilityTargets(140/150)`。
            //   ⇒ 这些档**只按 `ta` 建池**，36 格与裁剪都不参与。
            //   ⚠️ 改前（`B121-D1B`）对它们也筛 ⇒ 会被 `includeHero=0 / includeMinions=0` 整表筛空：
            //     实测 `Raging Storm`（`ca 30`，`ta=120/130`，36 格全是 0）的两行会被筛成空集 = **静默丢效果**。
            if (set == null || !TaGoesThroughCriteriaFilter(ta))
            {
                if (set == null && (ta == 10 || ta == 210))
                {
                    // 与改前逐字相同：没有 36 格 + `ta 10` ⇒ 判不出是哪些单位 ⇒ 出声、不结算。
                    NoteEnchantRowTargetGap(card, row, caName, ta);
                    return null;
                }
                return pool;
            }

            var cond = card.AbilityTargetConditionAt(row.Ab, row.Lg);
            var kept = new List<UnitState>(pool.Count);
            bool noted = false;
            for (int i = 0; i < pool.Count; i++)
            {
                var u = pool[i];
                if (u == null) continue;
                string gap;
                var ok = TargetCriteriaEval.Matches(ctx, owner, set, u, cond, thisCard: carrier,
                                                    targetCard: chosen, out gap);
                if (ok == null)
                {
                    // 契约：判不了 ⇒ **保留**候选 + 出声（与 `ApplyTargetCriteriaFilter` 同一条纪律）
                    kept.Add(u);
                    if (!noted) { noted = true; NoteEnchantTargetCriteriaGap(card, row, caName, gap); }
                }
                else if (ok.Value) kept.Add(u);
            }

            int maxT = set.MaxTargets;
            if (maxT > 0 && kept.Count > maxT)
            {
                if (set.TargetSelectMethod != 0)
                {
                    NoteEnchantTargetCriteriaGap(card, row, caName,
                        $"`maxTargets = {maxT}` 且 `targetSelectMethod = {set.TargetSelectMethod}`"
                      + "（1/2/3/5 各是一个具名挑选法）本版没有实现 ⇒ **不裁剪也不猜**");
                    return null;
                }
                var copy = new List<UnitState>(kept);
                kept.Clear();
                for (int i = 0; i < maxT && copy.Count > 0; i++)
                {
                    int idx = ctx.GetRandomInt(0, copy.Count);   // 原版 `GetTargets.c:1256` 的 `default` 支 = 随机取
                    kept.Add(copy[idx]);
                    copy.RemoveAt(idx);
                }
            }
            return kept;
        }

        /// <summary>双方全场的单位（含督军 + 残骸）—— 原版 `GetTargets.c` 的 `ta 0 / 10` 建池那一跳
        /// （`GetUnitsInPlayList(…, inclHero, inclMinions, …, inclPlayer, inclEnemy, …)`；具体哪些格由
        /// 36 格里的 `include*` 决定，**筛选交给 `TargetCriteriaEval`** ⇒ 这里给**超集**，⛔ 别在这里预筛）。</summary>
        static List<UnitState> AllUnitsBothSides(BattleContext ctx, int owner)
        {
            var pool = new List<UnitState>(8);
            AddSide(pool, ctx.Players[owner], false, false, -1, includeRemnants: true);
            AddSide(pool, ctx.Players[1 - owner], true, false, owner, includeRemnants: true);
            var w0 = WarlordOfSide(ctx, owner);
            if (w0 != null && w0.IsAlive && !pool.Contains(w0)) pool.Add(w0);
            var w1 = WarlordOfSide(ctx, 1 - owner);
            if (w1 != null && w1.IsAlive && !pool.Contains(w1)) pool.Add(w1);
            return pool;
        }

        /// <summary>目标筛「判不了 / 裁不了」的出声（同 `NoteEnchantRowDataGap` 的去重纪律）。</summary>
        static void NoteEnchantTargetCriteriaGap(CardDef card, CardDef.TriggerAbilityIndexSpec row,
                                                 string caName, string why)
        {
            string key = "tc|" + (card != null ? (string.IsNullOrEmpty(card.Id) ? card.Name : card.Id) : "?")
                       + "|" + (row != null ? row.Ca : -1) + "|" + (row != null ? row.Ab : -1) + "|" + why;
            if (!_b121d1EnchantDataGapNoted.Add(key)) return;
            _b121d1EnchantRowsDataGap++;
            UnityEngine.Debug.LogWarning(
                $"[B121_D1] 附魔「{(card != null ? card.Name : "?")}」的 `ca {caName}`：目标筛 {why}"
              + " ⇒ 这一行的目标集**不完整可信**（判不了的候选按契约保留并在这里出声；裁不了的整行不结算）。"
              + "判据 `AbilityLogic__GetTargets.c` / `FilterMethods__CheckIfMeetsCriteria.c`；"
              + "补法 → `资料/完全复刻/报告/B121_D1B_附魔载荷.md`");
        }

        /// <summary>「这一行的载荷缺 / 值不认识」的出声（复用 `_b121d1EnchantDataGapNoted` 去重）。</summary>
        static void NoteEnchantRowDataGapEx(CardDef card, CardDef.TriggerAbilityIndexSpec row, string why)
        {
            int ca = row != null ? row.Ca : -1;
            string key = "ex|" + (card != null ? (string.IsNullOrEmpty(card.Id) ? card.Name : card.Id) : "?")
                       + "|" + ca + "|" + (row != null ? row.Ab : -1) + "|" + (row != null ? row.Lg : -1) + "|" + why;
            if (!_b121d1EnchantDataGapNoted.Add(key)) return;
            _b121d1EnchantRowsDataGap++;
            UnityEngine.Debug.LogWarning(
                $"[B121_D1B] 附魔「{(card != null ? card.Name : "?")}」的 `ca {ca}`"
              + $"（ab{row?.Ab ?? -1}/lg{row?.Lg ?? -1}）：{why}");
        }

        /// <summary>`chosenAbility` 的可读名（只用于日志）。</summary>
        static string RowNameOf(int ca)
        {
            switch (ca)
            {
                case ChosenDrawCard: return "drawCard";
                case ChosenDoDamage: return "doDamage";
                case ChosenDoDamageFast: return "doDamageFast";
                case ChosenDamageAndDestroySelf: return "damageAndDestroySelf";
                case ChosenDoHeal: return "doHeal";
                case ChosenBuffMana: return "buffMana";
                default: return "ca " + ca;
            }
        }

        // ================================================================================================
        //  🆕 B121-D5（2026-11-03）：`ca 130 / 185 / 236 / 238` 的执行体
        // ================================================================================================
        //  判据（第一权威 `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c`，本波逐支现读）：
        //   · `ca 130 stun`                `:642-657`（`case 0x82`）：`while(targets)` →
        //       `BattleManager__AddStun(manager, target, priority(+0x44), checkIfCasterInPlay(+0x74),
        //        thisCard(+0x10), block(+0x48), 0)`；出队体 = `CardScript__Stun.c`（= 我们 `DoStun` 的唯一判据源）。
        //   · `ca 185 stealMinionSelf`     `:406-414`（`case 0xb9`）：**不读目标表** ——
        //       `AddStealMinion(manager, **abilityData.actingCard(+0x18)**, priority, block, 0)`；
        //       出队体 `_ResolveStealMinion_d__551__MoveNext.c` = `RemoveMinion` → `RemoveAllExtrinsicEffects`
        //       → `ChangeOwner` → 入新方 → `ResetSummonSickness`（**永久**，`EndTurn` 里没有翻回去的路）。
        //   · `ca 236 scriptedActingCardAttack` `:2716-2747`（`case 0xec`）：**只取 `targets[0]`** →
        //       `AddRedirectedAttack(manager, **actingCard**, targets[0], actingCard.currentAttackType(+0x120),
        //        priority, checkIfCasterInPlay, thisCard, block, 0)`；
        //       出队体 `BattleManager__AddRedirectedAttack.c:36-90`：攻方必须在场、不能「将死」，
        //       然后 `ClearPendingDamage` 两方 → `SetupAttack(攻方)` → `RecordAttackPendingDamage(攻方, 目标, 类型, 1, 1, 0)`。
        //       ⚠️ **不碰行动额度**（那一支里 `RemoveAttackThisTurn` 0 命中）。
        //   · `ca 238 scriptedSelfCardAttackDontCount` `:2764-2779`（`case 0xee`）：`while(targets)` →
        //       `AddScriptedForceAttack(manager, **thisCard**, targets[i], priority, checkIfCasterInPlay,
        //        thisCard, block, **dontCountAttack = 1**, 0)`；
        //       出队体 `BattleManager__AddScriptedForceAttack.c:42-46`：`RemoveAttackThisTurn(攻方)` 预扣 +
        //       记 `wasFree`，结算后由 `_ResolveForceAttack…:73-77` 排 `AddResetActions` 退还。
        //  ⛔ **没有第二份结算体**：
        //   · 眩晕 → 转调 `RuleCore.DoStun`（= `CardScript__Stun.c` 的唯一实现，`EffectResolver.cs:4373`）；
        //   · 易主 → 转调 `RuleCore.DoTakeControl`（= `_ResolveStealMinion` 的唯一实现，`EffectResolver.cs:9594`；
        //     🔴 2026-11-03 现读订正：它**早就是永久易主**了 —— 2026-10-28 `B91·A1` 删掉了「回合末归还」那一半，
        //     判据 `ChangeOwner` 全库 2 个调用点 + `EndTurn` 无翻回 ⇒ 旧口径「DoTakeControl 是『本回合+Fast』的
        //     另一支、⛔ 不能拿它顶」**已过期**）；
        //   · 两记脚本攻击 → 转调 `RuleCore.DeclareAttackCore`（= 声明层 + 结算的唯一实现）。
        //  ⚠️ **为什么 236 用 `DeclareAttackCore` 而不是 `DeclareAttack(forced:true)`**：后者的「退不退还」
        //     由 `ForcedAttackRefunds(ctx)` 按**卡 id 查表**决定，而那张表只装 `cards_engine.json`
        //     （`Data/ForcedAttackRefundMap.cs`）⇒ 附魔模板卡恒 `Unknown` ⇒ **一律按退还**。
        //     `ca 236` 原版不退（`AddRedirectedAttack` 不碰额度）⇒ 走 `DeclareAttackCore`（不预扣、不退还）；
        //     `ca 238` 原版退 ⇒ 走类尾 `DeclareScriptedForceAttack238`（预扣 + 退还，逐句同
        //     `DeclareAttackRefundingForced`，只是 `refunds` 恒真 —— 那一支传的就是 `1`）。
        //     理想修法 = 给 `DeclareAttackRefundingForced` 加一个 `forceRefund` 形参（`RuleCore.cs` 本波白名单外，
        //     已写进报告的可粘贴补丁）。
        //  🧨 灭自证：删掉 `:130` / `:185` / `:236` / `:238` 任一档分派 ⇒ `B121_D5_Assert` 对应那条变红
        //     （件数 1 → 0 / 生命或归属不变）。

        /// <summary>`ca 130 stun` 的行执行器（判据 `.c:642-657`）。
        /// <para>目标集 = 36 格解算口（本档 4 行全是 `ta = 10` + `enemyUnits=1 ∨ friendlyUnits=1` +
        /// `traitsFilter=100(stun) ∧ excludeTrait=1`，即「**还没被晕**的那一侧单位」）。</para>
        /// <para>施加段转调 <see cref="RuleCore.DoStun"/>（`CardScript__Stun.c` 的唯一实现：`unstunnable` 闸 ·
        /// 已晕跳过 · `AddKeyword(Stun)` · 清 `StunnedAtStartOfTurn` · `GetsStun` 广播）。</para>
        /// <para>⚠️ **如实标着**：`DoStun` 里那道 `AddEffectCasterGates(...)"（`CardScript__AddEffect.c` 的 jam 闸）
        /// 不在原版 `AddStun` 的路径上；今天 `ctx.ResolvingExtrinsic` **恒 false** ⇒ 无可观测差异，
        /// 但它是**共用件里多出来的一道闸**（提取共用件的补丁 → 报告 §顺手发现）。</para></summary>
        internal static int RunStunRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                    CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                    UnitState actingCard, UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var targets = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                    "130 stun", actingCard);
            if (targets == null || targets.Count == 0) return 0;

            // 目标集已经定死 ⇒ 让 `DoStun` 走「调用方已点定目标」那一支（`SubjectTargetsAffected = 30` + `Count = 1`，
            // 与 `RunRecallToOwnerHandRowOfEnchantment` 同一手法）；⛔ 别让它自己再扫一遍棋盘。
            var unresolved = new List<string>(1);
            int stunned = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.IsAlive) continue;
                bool before = t.IsStunned;
                var op = new EffectOp
                {
                    Verb = "stun",
                    Source = enchantmentCard.Name ?? "附魔",
                    SubjectTargetsAffected = 30,
                    Target = new EffectTargetSpec
                    {
                        Raw = "(附魔 ca 130 stun)", Side = "any", Kind = "unit", Count = 1,
                    },
                };
                DoStun(ctx, owner, by, op, t, unresolved);
                if (!before && t.IsStunned) stunned++;      // 真晕上的才计数（`DoStun` 的返回真只表示「这一 op 跑完了」）
            }
            if (unresolved.Count > 0) ReportUnresolved(ctx, by, unresolved);
            ctx.Log($"{by}：`chosenAbility 130`（stun）⇒ {stunned}/{targets.Count} 张被眩晕"
                  + "（原版 `AbilityLogic__PlayAbility.c:642-657` 逐目标 `AddStun`；施加体 = `RuleCore.DoStun`）");
            return stunned;
        }

        /// <summary>`ca 185 stealMinionSelf` 的行执行器（判据 `.c:406-414`）。
        /// <para>🔴 **目标 = `actingCard`（触发源），⛔ 不读目标表**（原版 `AddStealMinion(actingCard)`，单张、不遍历）。
        /// 我们是附魔路 ⇒ 用形参 `actingCard` 而不是 `ctx.ActingUnit`（后者可能被外层 op 覆盖），
        /// 再用「调用方已点定目标」那一支把它交给 `DoTakeControl`。</para>
        /// <para>🔴 **永久易主**：`DoTakeControl` 自 2026-10-28（`B91·A1`）起已删掉「回合末归还」，
        /// 与 `_ResolveStealMinion_d__551` 逐跳同形（`RemoveMinion` → `RemoveAllExtrinsicEffects` →
        /// `ChangeOwner` → `ResetSummonSickness`）⇒ 直接复用，⛔ 不另写一份搬运。</para>
        /// <para>⚠️ 原版对**己方**单位也照翻（没有「本来就是你的人就跳过」那道闸）—— 那是 `185` 的正常形状。</para></summary>
        internal static int RunStealMinionSelfRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                              CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                              UnitState actingCard, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            if (actingCard == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`ca 185 stealMinionSelf` 的目标 = **触发源（actingCard）**，而本刻它是空的"
                  + "（原版 `PlayAbility.c:406-414` 的 `abilityData.actingCard` 同样是 null ⇒ 整支空转）");
                return 0;
            }
            int fromP, fromSlot;
            if (!FindSlot(ctx, actingCard, out fromP, out fromSlot))
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca 185 stealMinionSelf` 的触发源「{actingCard.Name}」**不在场上** ⇒ 抢不了"
                  + "（原版 `_ResolveStealMinion…:35` 的 `IsAvailableSlot` 支只 LogWarning）");
                return 0;
            }

            var unresolved = new List<string>(1);
            var op = new EffectOp
            {
                Verb = "takecontrol",                 // ⚠️ 不是 `stealminionself`：那一支的目标来源是 `ctx.ActingUnit`，
                Source = enchantmentCard.Name ?? "附魔",  //   而附魔路的真值 = 形参 `actingCard`（见 doc）
                SubjectTargetsAffected = 30,
                Target = new EffectTargetSpec
                {
                    Raw = "(附魔 ca 185 stealMinionSelf：目标 = 触发源)", Side = "any", Kind = "unit", Count = 1,
                },
            };
            DoTakeControl(ctx, owner, by, op, actingCard, unresolved);
            if (unresolved.Count > 0) ReportUnresolved(ctx, by, unresolved);

            // 计「真换了主」的那一档：抢之前/之后各解一次座位
            int toP, toSlot;
            bool moved = FindSlot(ctx, actingCard, out toP, out toSlot) && toP != fromP;
            ctx.Log($"{by}：`chosenAbility 185`（stealMinionSelf）⇒ 触发源「{actingCard.Name}」"
                  + $"{(moved ? $"从 {fromP} 侧**永久**易主到 {toP} 侧" : "**没有换主**（落点满 / 不在场）")}"
                  + "（原版 `AbilityLogic__PlayAbility.c:406-414` 的 `AddStealMinion(actingCard)`；"
                  + "搬运体 = `RuleCore.DoTakeControl`）");
            return moved ? 1 : 0;
        }

        /// <summary>`ca 236 scriptedActingCardAttack` / `ca 238 scriptedSelfCardAttackDontCount` 的行执行器
        /// （判据 `.c:2716-2747` / `:2764-2779`）。
        /// <list type="bullet">
        /// <item><b>236</b>：攻方 = <paramref name="actingCard"/>、被打 = **`targets[0]`**（原版 `GetItem(targets, 0)`），
        ///   打法 = **攻方当前那一档**（`actingCard + 0x120`），**不碰行动额度**。</item>
        /// <item><b>238</b>：攻方 = **载体**（`thisCard`）、被打 = 逐目标，`dontCountAttack = 1` ⇒ **预扣 + 退还**。</item>
        /// </list>
        /// <para>两档都走 `RuleCore.DeclareAttackCore`（声明层 + 结算的唯一实现）。</para></summary>
        internal static int RunScriptedAttackRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                              CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                              UnitState actingCard, UnitState chosen,
                                                              bool selfAttacks, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var targets = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                    selfAttacks ? "238 scriptedSelfCardAttackDontCount"
                                                                : "236 scriptedActingCardAttack", actingCard);
            if (targets == null || targets.Count == 0) return 0;

            var attacker = selfAttacks ? carrier : actingCard;
            if (attacker == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    (selfAttacks ? "`ca 238` 的攻方 = 载体" : "`ca 236` 的攻方 = 触发源（actingCard）") + "，本刻是空的"
                  + "（原版 `AbilityLogic__PlayAbility.c:2716-2747` 的 `AddRedirectedAttack` 拿到 null 也一样空转）");
                return 0;
            }
            int atkP, atkSlot;
            if (!FindSlot(ctx, attacker, out atkP, out atkSlot))
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {(selfAttacks ? 238 : 236)}` 的攻方「{attacker.Name}」**不在场上** ⇒ 这一记不打"
                  + "（原版 `BattleManager__AddRedirectedAttack.c:36-40` 的 `Ignoring AddRedirectedAttack because unit no longer in play`）");
                return 0;
            }
            // 打法 = 攻方**当前**那一档（原版 `actingCard.currentAttackType(+0x120)`；238 的
            // `AddScriptedForceAttack` 也是拿 `param_2 + 0x120` 去 `RecordAttackPendingDamage`）。
            // 判据同一句：`CardScript.ChooseAttackTypeAutomatically` = `(近战 < 远程) + 1`。
            bool ranged = FieldAttack(ctx, atkP, attacker, true) > FieldAttack(ctx, atkP, attacker, false);

            // 236 只打 targets[0]（原版 `GetItem(targets, 0)`）；238 逐目标各一记。
            int limit = selfAttacks ? targets.Count : 1;
            int hit = 0;
            for (int i = 0; i < limit && i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.IsAlive) continue;
                int dp, dslot;
                if (!FindSlot(ctx, t, out dp, out dslot)) continue;
                int code = selfAttacks
                    ? DeclareScriptedForceAttack238(ctx, atkP, atkSlot, dp, dslot, ranged)   // 预扣 + 退还
                    : DeclareScriptedAttack236(ctx, atkP, atkSlot, dp, dslot, ranged);      // 不碰额度
                if (code == RuleCodes.OK) hit++;
                else ctx.Log($"{by}：`ca {(selfAttacks ? 238 : 236)}` 打「{t.Name}」没成 —— {RuleCodes.Describe(code)}"
                           + "（原版那条路判不过就 `goto` 收尾，同样什么都不发生）");
            }
            ctx.Log($"{by}：`chosenAbility {(selfAttacks ? 238 : 236)}`"
                  + $"（{(selfAttacks ? "scriptedSelfCardAttackDontCount" : "scriptedActingCardAttack")}）"
                  + $"⇒ 「{attacker.Name}」对 {hit}/{limit} 个目标出手"
                  + "（原版 `AbilityLogic__PlayAbility.c:2716-2747` / `:2764-2779`；攻击口 = `RuleCore.DeclareAttackCore`）");
            return hit;
        }

        /// <summary>`ca 236` 那一记：原版 `AddRedirectedAttack` **不碰行动额度**
        /// ⇒ 绕过 `DeclareAttackRefundingForced` 的预扣/退还，直接走声明层 + 结算。
        /// <para>⚠️ `BeginAction` 必须自带 —— `DeclareAttack` 就是在它里面包的 `DeclareAttackCore`。</para></summary>
        static int DeclareScriptedAttack236(BattleContext ctx, int p, int atkSlot, int tgtP, int tgtSlot, bool ranged)
        {
            using (BeginAction(ctx)) return DeclareAttackCore(ctx, p, atkSlot, tgtP, tgtSlot, ranged, forced: true);
        }

        /// <summary>`ca 238` 那一记：原版 `AddScriptedForceAttack(…, dontCountAttack = **1**, 0)`
        /// ⇒ **入队预扣 + `wasFree` 才退还**，逐句同 `RuleCore.DeclareAttackRefundingForced`
        /// （净效果：攻击前 `0` 次 ⇒ 净 0；`> 0` 次 ⇒ 净不变）。
        /// <para>⚠️ 为什么复制这三句而不是直接转调那个方法：它的 `refunds` 由
        /// `ForcedAttackRefunds(ctx)` 按**卡 id 查表**算，而那张表只装 `cards_engine.json`
        /// ⇒ 附魔模板卡恒 `Unknown`、会被算成「退还」，**行为上碰巧对**，但那不是这一档的判据
        /// （判据是 `.c:2764-2779` 把 `1` 写死在实参里）。补丁（给 `DeclareAttackRefundingForced`
        /// 加 `forceRefund` 形参）已写进报告。</para></summary>
        static int DeclareScriptedForceAttack238(BattleContext ctx, int p, int atkSlot, int tgtP, int tgtSlot, bool ranged)
        {
            UnitState atkUnit = (ctx != null && p >= 0 && p <= 1 && BoardSpec.IsValid(atkSlot))
                                ? ctx.Players[p].Board[atkSlot] : null;
            bool wasFree = atkUnit != null && atkUnit.AttacksThisTurn == 0;
            RemoveAttackThisTurn(atkUnit);                                  // 原版 `AddScriptedForceAttack.c:44`
            int code;
            using (BeginAction(ctx)) code = DeclareAttackCore(ctx, p, atkSlot, tgtP, tgtSlot, ranged, forced: true);
            if (code == RuleCodes.OK && wasFree && atkUnit != null)
                RefundForcedAttackAction(ctx, p, atkSlot, atkUnit);         // 原版 `_ResolveForceAttack…:73-77`
            return code;
        }

        // ================================================================================================
        //  🆕 B121-D5R（2026-11-03）：`ca 70 summonUnit` / `ca 100 createCardInHand` 的执行体（五支取牌）
        // ================================================================================================
        //  判据（第一权威 `d:/2/tools/decomp_full/AbilityLogic__GetCardToSummon.c` 全文，本波逐句现读；
        //  另用 ILSpy 干净版 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AbilityLogic.cs:1400-1610` 交叉复核 ——
        //  ⚠️ 那一版把 `GetTargets` 的 `targetCard` / `checkMaxTargets` 两个实参反编译成了 `default`
        //  （已知的「ILSpy 丢常量」坑），实参一律以 `.c:110` 的 `(manager, targetCriteria, thisCard,
        //  actingCard, targetCard, 1, 0)` 为准）：
        //
        //  ── 五支（原版分支顺序就是这个顺序，⛔ 别调换）──────────────────────────────────────────────
        //   ⑤ `targetCriteria.selectFromChooseResolution != 0`（**最先判**，`.c:41` / `:239-276`）
        //      ⇒ `manager.cardChosenInSelection`（**取一次就清空**）否则 `poolChoosenCard[0]` 的卡；
        //      ⚠️ 这一支**不走**末尾那段「整表 / 随机一张」的收尾（原版直接 return）。
        //   ① `unitsToSummon` **非空** ∧ `summonAllTargets == 0` ⇒ `GetRandomInt(0, Count)` **随机取一张**（`.c:191-208`）
        //   ② `unitsToSummon` **非空** ∧ `summonAllTargets != 0` ⇒ **整表全给**（`.c:209-236`）
        //   ③ `unitsToSummon` **明确为空** ∧ `chooseCardInPlay != 0` ⇒ `GetTargets(…)` 的目标集**逐张取其卡**
        //      （`.c:103-149`；`shouldRemoveFromCemetery` ∧ 候选 `cardState == inCemetery` ⇒ 从墓地摘掉）
        //   ④ `unitsToSummon` **明确为空** ∧ `chooseCardInPlay == 0` ⇒ 扫**整个收藏**（`.c:45-101`），三道闸：
        //      `CheckIfRawCardMeetsCriteria(卡, targetCriteria, manager, thisCard.isPlayer)` ∧ `!cantBeRandomlyCreated`
        //      ∧ `!IsHidden()`
        //   ③④ 之后有一段**共同收尾**（`.c:150-187` = `LAB_1808171c0`）：空表原样返回；`summonAllTargets != 0`
        //      ⇒ 整表；否则随机一张。
        //
        //  ── 落点（⛔ 不另写落盘逻辑）────────────────────────────────────────────────────────────────
        //   · `ca 70`（`PlayAbility.c:723-878` 的 `case 0x46`）= `RuleCore.DeployFree`（`RuleCore.cs:5097` / `:4876`）；
        //     收兵侧 = `carrier.isPlayer XOR isForEnemy`（`.c:744-753`）；落点 = `createAtPos` 四档
        //     （`.c:759-793`）经 `NextSlotToCard`（`EffectResolverPartial_B88E2.cs:255`）算出的**请求格**。
        //   · `ca 100`（`PlayAbility.c:986-1037` 的 `case 0x64`）= `RuleCore.AddCardToHandWithinLimit`
        //     （`RuleCore.cs:3270`）；收牌方 = 同一个 XOR。
        //   · 重复次数 = 原版 `local_178`（`PlayAbility.c:180-236`）= `multiplier` 目标数（`0 any` ⇒ 1）×
        //     `furtherMultiplier`（`0 < x` 才乘）。`multiplier` 的 36 格**没导出** ⇒ `Mult != 0` 判不了 ⇒
        //     出声不结算（与 `RunDrawCardRowOfEnchantment` / `RunDoDamageRowOfEnchantment` 同一口径，⛔ 不拿 1 顶）。
        //
        //  ⚠️ **如实标（今天不可观测，不是「没做」）**：原版 `AddSummonUnit` 的 `checkIfCasterInPlay`
        //     （`AbilityLogic + 0x74`）与 `AddNewCardToHand` 的 `priority` 都是**动作队列**参数；我们是同步结算，
        //     caster 不可能在「入队 → 出队」之间离场 ⇒ 无差异。`ca 100` 原版还会把新卡追加进
        //     `abilityLogic + 0x80`（供同一 logic 的后续分支读），附魔路每行独立、没有那个消费者 ⇒ 不落地。
        //  🧨 灭自证：删掉 `:70` / `:100` 任一档分派 ⇒ `B121_D5R_Assert` 对应那条变红；
        //     把「随机取一张」改成「全给」⇒ 支①的反证红；把 `unitsToSummon == null` 折成空表 ⇒ 支④那条
        //     「出声、不扫池」的反证红（会变成真去扫 >0 张）。

        /// <summary>`ca 70 summonUnit` 的行执行器（判据 `.c:723-878`）。
        /// <para>每轮取一次牌（支①~⑤）→ 逐张 `DeployFree`（= 原版 `AddSummonUnit` 的唯一等价口）；
        /// 落点按 `createAtPos` 四档算**请求格**。场上没空格 ⇒ 这一轮余下的放不下，停（原版那条路同样落不下）。</para></summary>
        internal static int RunSummonUnitRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                          CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                          UnitState actingCard, UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var spec = enchantmentCard.AbilitySummonAt(row.Ab, row.Lg);
            if (spec == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`ca 70 summonUnit` 要读 `summonCriteria`（原版 `SummonCriteria` 七格 + `unitsToSummon`），"
                  + "而这一行在运行时**取不到**（旧表 / 手写夹具 / 本行本来没有 `summonCriteria`）⇒ **本行不结算**");
                return 0;
            }
            if (spec.IsForEnemy < 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca 70 summonUnit` 的 `summonCriteria.isForEnemy` 未知（{spec.IsForEnemy}）"
                  + "⇒ 判不出召到哪一侧 ⇒ **本行不结算**（⛔ 别默认「召给自己」）");
                return 0;
            }
            int repeats;
            if (!TryRepeatsOfEnchantment(enchantmentCard, row, "70 summonUnit", out repeats)) return 0;

            int side = spec.IsForEnemy != 0 ? 1 - owner : owner;      // 原版 `.c:744-753`：thisCard.isPlayer XOR isForEnemy
            int summoned = 0;
            bool boardFull = false;
            for (int r = 0; r < repeats && !boardFull; r++)
            {
                var cards = GetCardsToSummon(ctx, owner, carrier, enchantmentCard, row, spec,
                                             actingCard, chosen, "70 summonUnit", by);
                if (cards == null) return summoned;                    // 判不了（已出声）
                if (cards.Count == 0) continue;                        // 原版空表 = 这一轮什么都不做，`do{}while` 照跑下一轮
                for (int i = 0; i < cards.Count; i++)
                {
                    var c = cards[i];
                    if (c == null) continue;
                    int req = SummonRequestSlotOf(ctx, side, spec, carrier, chosen, by);
                    int slot;
                    if (!DeployFree(ctx, side, ctx.NewInstance(c), out slot, false, DeploySource.Summon, null, req))
                    {
                        ctx.Log($"{by}：`chosenAbility 70`（summonUnit）召「{c.Name}」时**场上没空格了** ⇒ "
                              + "这一轮余下的也放不下，停（原版 `AddSummonUnit` 那条路同样落不下）");
                        boardFull = true;
                        break;
                    }
                    summoned++;
                }
            }
            ctx.Log($"{by}：`chosenAbility 70`（summonUnit）⇒ 召出 {summoned} 个"
                  + $"（{repeats} 轮 × 每轮候选见上 · {(spec.IsForEnemy != 0 ? "敌方" : "己方")}侧 · "
                  + $"`createAtPos = {spec.CreateAtPos}`）；原版 `AbilityLogic__PlayAbility.c:723-878` 逐张 `AddSummonUnit`；"
                  + "落点 = `RuleCore.DeployFree`");
            return summoned;
        }

        /// <summary>`ca 100 createCardInHand` 的行执行器（判据 `.c:986-1037`）。
        /// <para>每轮取一次牌（支①~⑤）→ 逐张进**收牌方**手牌（`AddCardToHandWithinLimit`，满手 ⇒ 该张进弃牌堆，
        /// 原版 `AddNewCardToHand` 同形）。</para></summary>
        internal static int RunCreateCardInHandRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                                CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                                UnitState actingCard, UnitState chosen, string by)
        {
            if (ctx == null || carrier == null || enchantmentCard == null || row == null) return 0;
            var spec = enchantmentCard.AbilitySummonAt(row.Ab, row.Lg);
            if (spec == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    "`ca 100 createCardInHand` 要读 `summonCriteria`（造哪几张 = `unitsToSummon`），"
                  + "而这一行在运行时**取不到**（旧表 / 手写夹具 / 本行本来没有 `summonCriteria`）⇒ **本行不结算**");
                return 0;
            }
            if (spec.IsForEnemy < 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca 100 createCardInHand` 的 `summonCriteria.isForEnemy` 未知（{spec.IsForEnemy}）"
                  + "⇒ 判不出造给哪一方 ⇒ **本行不结算**（⛔ 别默认「造给自己」）");
                return 0;
            }
            int repeats;
            if (!TryRepeatsOfEnchantment(enchantmentCard, row, "100 createCardInHand", out repeats)) return 0;

            int side = spec.IsForEnemy != 0 ? 1 - owner : owner;      // 同 `ca 70`：`.c:1024-1026` 的 `isPlayer`
            int created = 0, full = 0;
            for (int r = 0; r < repeats; r++)
            {
                var cards = GetCardsToSummon(ctx, owner, carrier, enchantmentCard, row, spec,
                                             actingCard, chosen, "100 createCardInHand", by);
                if (cards == null) return created;
                if (cards.Count == 0) continue;                        // 同 `ca 70`：原版空表只是这一轮空转
                for (int i = 0; i < cards.Count; i++)
                {
                    var c = cards[i];
                    if (c == null) continue;
                    if (AddCardToHandWithinLimit(ctx, side, ctx.NewInstance(c))) created++;
                    else full++;                                       // 满手 ⇒ 该张进弃牌堆（`AddCardToHandWithinLimit` 自带出声）
                }
            }
            ctx.Log($"{by}：`chosenAbility 100`（createCardInHand）⇒ 造出 {created} 张进"
                  + $"{(spec.IsForEnemy != 0 ? "敌方" : "己方")}手牌"
                  + (full > 0 ? $"（另 {full} 张因手牌已满直接进弃牌堆）" : "")
                  + $"；{repeats} 轮 × 每轮候选见上；原版 `AbilityLogic__PlayAbility.c:986-1037` 逐张 `AddNewCardToHand`；"
                  + "落点 = `RuleCore.AddCardToHandWithinLimit`");
            return created;
        }

        /// <summary>原版 `local_178`（`AbilityLogic__PlayAbility.c:180-236`）的等价：这一行要重复几轮。
        /// <para>`multiplier.targetsAffected == 0 (any)` ⇒ 1；**其余档 = 乘数目标表的长度**，而乘数那 36 格
        /// 没有导出 ⇒ 判不了（出声、不结算）。`furtherMultiplier` 只在 `> 0` 时才乘（原版 `:235` 的
        /// `if (0 < …)`）⇒ `Fm <= 0` / 未知(-1) 都按 1，**逐字同原版**。</para></summary>
        static bool TryRepeatsOfEnchantment(CardDef card, CardDef.TriggerAbilityIndexSpec row, string caName, out int repeats)
        {
            repeats = 1;
            if (row == null) return false;
            if (row.Mult != 0)
            {
                NoteEnchantRowDataGapEx(card, row,
                    $"`ca {caName}` 的重复次数 = 原版 `local_178`（`PlayAbility.c:180-236`）= **乘数目标表的长度** × "
                  + $"`furtherMultiplier`，而本行 `multiplier.targetsAffected = {row.Mult}`（≠ 0 any）"
                  + "且乘数那 36 格没导出 ⇒ **本行不结算**（⛔ 不拿 1 顶）");
                return false;
            }
            if (row.Fm > 0) repeats = row.Fm;
            return true;
        }

        /// <summary>原版 `AbilityLogic.GetCardToSummon`（`AbilityLogic__GetCardToSummon.c` 全文）的等价体 ——
        /// **五支取牌**，返回「这一轮要召 / 要造的卡」（**保留顺序与重复**）。
        /// <para>分支顺序与逐支判据 → 本节头注；⛔ 别调换 ⑤ 的位置（它最先判、且不走末尾收尾）。</para>
        /// <returns>`null` = **判不了**（已出声，调用方不结算）；空表 = 判得出、但一个候选都没有。</returns></summary>
        static List<CardDef> GetCardsToSummon(BattleContext ctx, int owner, UnitState carrier,
                                              CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                              CardDef.AbilitySummonSpec spec, UnitState actingCard,
                                              UnitState chosen, string caName, string by)
        {
            if (ctx == null || enchantmentCard == null || row == null) return null;
            if (spec == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row, $"`ca {caName}`：这一行没有 `summonCriteria` ⇒ 判不了");
                return null;
            }
            // `targetCriteria` = 原版 `abilityLogic.targetCriteria(+0x28)`（**不是** summonCriteria 里的格）
            var crit = enchantmentCard.AbilityTargetCriteriaAt(row.Ab, row.Lg);

            // ---- 支⑤：`selectFromChooseResolution`（`.c:41` / `:239-276`）—— 最先判，且**不走**末尾收尾 ----
            if (crit != null && crit.SelectFromChooseResolution != 0)
            {
                var one = new List<CardDef>(1);
                if (ctx.CardChosenInSelection != null)
                {
                    var picked = ctx.CardChosenInSelection;
                    ctx.CardChosenInSelection = null;              // 原版 `*(manager + 0x2e8) = 0` —— **一次性**
                    one.Add(picked);
                    ctx.Log($"{by}：`ca {caName}` 取「刚在选牌里点定的那张」{picked.Name}"
                          + "（原版 `GetCardToSummon.c:268-274`：`cardChosenInSelection` 取一次就清空）");
                    return one;
                }
                var pool = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                     caName, actingCard);
                if (pool == null) return null;
                if (pool.Count > 0 && pool[0] != null && pool[0].Card != null)
                {
                    one.Add(pool[0].Card);
                    ctx.Log($"{by}：`ca {caName}` 没有「刚选定的那张」⇒ 取目标表第一张「{pool[0].Name}」"
                          + "（原版 `GetCardToSummon.c:250-264`：`poolChoosenCard[0]`）");
                }
                else
                    ctx.Log($"{by}：`ca {caName}` 既没有「刚选定的那张」、目标表也空 ⇒ **没有候选**"
                          + "（原版同样得到空表，什么都不发生）");
                return one;
            }

            // 支①~④的收尾 / 随机都要读它 ⇒ 未知就整行不结算
            if (spec.SummonAllTargets < 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {caName}` 的 `summonCriteria.summonAllTargets` 未知（{spec.SummonAllTargets}）"
                  + "⇒ 判不出「整表全给 / 随机取一张」⇒ **本行不结算**");
                return null;
            }

            // ---- 支①②：`unitsToSummon` 非空（`.c:191-236`）----
            if (spec.UnitsToSummon == null)
            {
                // 原版这里恒非 null（数组），我们三态里的 `null` = **没导出** ⇒ 判不了，⛔ 绝不当空表
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {caName}` 的 `summonCriteria.unitsToSummon` **没有导出**（`null` = 未知，⛔ 不是「空表」）"
                  + "⇒ 判不出「用候选表 / 扫收藏」⇒ **本行不结算**");
                return null;
            }
            if (spec.UnitsToSummon.Length > 0)
            {
                int outside;
                var mapped = MapSummonIds(ctx, spec.UnitsToSummon, enchantmentCard, row, caName, by, out outside);
                if (mapped == null) return null;
                if (mapped.Count == 0)
                {
                    ctx.Log($"{by}：`ca {caName}` 的 `unitsToSummon`（{spec.UnitsToSummon.Length} 个 id）"
                          + "**一个都查不到卡** ⇒ 没有候选（原版拿不到 `RawCardScript` 同样什么都不发生）");
                    return mapped;
                }
                if (spec.SummonAllTargets != 0) return mapped;                                     // 支②：整表全给
                return new List<CardDef>(1) { mapped[ctx.GetRandomInt(0, mapped.Count)] };           // 支①：随机取一张
            }

            // ---- `unitsToSummon` **明确为空** ⇒ 支③ / 支④ ----
            if (spec.ChooseCardInPlay < 0)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {caName}` 的 `summonCriteria.chooseCardInPlay` 未知（{spec.ChooseCardInPlay}）"
                  + "⇒ 判不出「取场上目标 / 扫整个收藏」⇒ **本行不结算**");
                return null;
            }
            List<CardDef> cand;
            if (spec.ChooseCardInPlay != 0)
            {
                // ---- 支③：`GetTargets(…)` 的目标集，逐张取其卡（`.c:103-149`）----
                var targets = ResolveEnchantmentTargets(ctx, owner, carrier, chosen, row.Ta, enchantmentCard, row,
                                                        caName, actingCard);
                if (targets == null) return null;
                cand = new List<CardDef>(targets.Count);
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (t == null) continue;
                    if (t.Card == null) continue;
                    cand.Add(t.Card);
                    if (spec.ShouldRemoveFromCemetery != 0) RemoveChosenFromCemetery(ctx, t, caName, by);
                }
                ctx.Log($"{by}：`ca {caName}` 取**场上目标**的卡（原版 `GetCardToSummon.c:103-149`："
                      + "`validTargetsInPlay.Count " + targets.Count + "`）⇒ " + cand.Count + " 张候选");
            }
            else
            {
                // ---- 支④：扫整个收藏（`.c:45-101`）----
                cand = ScanCollectionForSummon(ctx, crit, enchantmentCard, row, caName, by);
                if (cand == null) return null;
            }

            // ---- 共同收尾（`.c:150-187` = `LAB_1808171c0`）----
            if (cand.Count < 1) return cand;
            if (spec.SummonAllTargets != 0) return cand;
            return new List<CardDef>(1) { cand[ctx.GetRandomInt(0, cand.Count)] };
        }

        /// <summary>支①/② 的 `unitsToSummon` 卡 id → <see cref="CardDef"/>（原版直接拿 PPtr 指向的
        /// `RawCardScript`：**不筛任何准则、保留顺序与重复**）。
        /// <para>查表源 = <see cref="CardDef.RawCardPool"/>（`CardDatabase` 静态构造登记的唯一钩子）。
        /// `null` = **没登记**（未初始化 / 手写夹具）⇒ **出声 + 返回 `null`**（判不了，⛔ 不拿 `ctx.CardPool` 顶替 ——
        /// 那会换掉「哪些卡查得到」这个域，而不是补一个实现细节）。</para>
        /// <para>⚠️ 生产侧实际上到不了这一档：附魔模板本身由 `CardDatabase.LoadEnchantmentTemplates` 装载
        /// ⇒ 能拿到模板卡就说明钩子已登记；这一闸是给**探针 / 手写夹具**用的（同 `ScanCollectionForSummon` 那条）。</para></summary>
        static List<CardDef> MapSummonIds(BattleContext ctx, string[] ids, CardDef enchantmentCard,
                                          CardDef.TriggerAbilityIndexSpec row, string caName, string by, out int outside)
        {
            outside = 0;
            var list = new List<CardDef>(ids != null ? ids.Length : 0);
            if (ids == null || ids.Length == 0) return list;
            var pool = CardDef.RawCardPool;
            if (pool == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {caName}` 的 `unitsToSummon` 要按卡 id 查表，而 `CardDef.RawCardPool` **没有登记**"
                  + "（未初始化 / 手写夹具）⇒ 这些 id 指向哪张卡**判不了** ⇒ **本行不结算**（⛔ 不拿本局可玩池顶替）");
                return null;
            }
            var byId = new Dictionary<string, CardDef>(System.StringComparer.Ordinal);
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                {
                    var c = pool[i];
                    if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                    if (!byId.ContainsKey(c.Id)) byId[c.Id] = c;
                }
            var missing = new List<string>(4);
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                CardDef hit;
                if (!string.IsNullOrEmpty(id) && byId.TryGetValue(id, out hit)) list.Add(hit);
                else { outside++; if (missing.Count < 8) missing.Add(string.IsNullOrEmpty(id) ? "null" : id); }
            }
            if (outside > 0)
                ctx.Log($"{by}：`ca {caName}` 的 `unitsToSummon` 有 **{outside} 个 id 查不到卡**"
                      + "（查表源 = 全卡池 `CardDef.RawCardPool`）"
                      + " ⇒ 照原版「拿不到就是拿不到」如实跳过：" + string.Join("、", missing.ToArray())
                      + (outside > missing.Count ? " …" : ""));
            return list;
        }

        /// <summary>支④：扫**整个收藏**（原版 `PlayerDataManager.singletonManager.fullCardCollection`，
        /// `GetCardToSummon.c:45-101`）。三道闸：`CheckIfRawCardMeetsCriteria` ∧ `!cantBeRandomlyCreated` ∧ `!IsHidden()`。
        /// <para>数据源 = <see cref="CardDef.RawCardPool"/>（`B121-D4` 的钩子）。`null` = **未知** ⇒ 出声 + 返回 `null`：
        /// ⛔ **不拿可玩池顶替**（那会静默换掉候选域 —— 「扫收藏」这一支的全部语义就在这个域上）。</para>
        /// <para>⚠️ 闸①只做到**卡模板级子集**（`TargetCriteriaEval.MatchesRawCard`：兵种 / 关键词 / 卡名 /
        /// spellType / 督军 …）；原版 `CheckIfRawCardMeetsCriteria` 还有 rarity / cost / expansion 等格
        /// ⇒ 判不了的候选**保留 + 出声**（同 `CardPoolCriteriaFilter` 的契约）。闸③ `IsHidden` 是**代理判据**
        /// （卡名带 `(tutorial)`，原版方法体没读到）—— 与 `EffectResolver.IsHiddenCard` 同一份实现，⛔ 别写第二份。</para></summary>
        static List<CardDef> ScanCollectionForSummon(BattleContext ctx, CardDef.CountSetSpec crit,
                                                     CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                     string caName, string by)
        {
            if (crit == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {caName}` 的「扫整个收藏」要按 `abilityLogic.targetCriteria` 逐张筛，"
                  + "而这一行的 36 格**没导出** ⇒ 判不了要哪些卡 ⇒ **本行不结算**（⛔ 不拿「全池随便取」顶）");
                return null;
            }
            var pool = CardDef.RawCardPool;
            if (pool == null)
            {
                NoteEnchantRowDataGapEx(enchantmentCard, row,
                    $"`ca {caName}` 的「扫整个收藏」这一支要 `CardDef.RawCardPool`（`CardDatabase` 登记的钩子），"
                  + "而它**没有登记**（未初始化 / 探针 / 手写夹具）⇒ 原版那个候选域**判不了** "
                  + "⇒ **本行不结算**（⛔ 不当空池、也不拿可玩池顶替）");
                return null;
            }
            var list = new List<CardDef>(16);
            int skippedRandom = 0, skippedHidden = 0, skippedCrit = 0, unclear = 0;
            string firstGap = null;
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                if (c.CantBeRandomlyCreated) { skippedRandom++; continue; }        // 闸②（`.c:80`）
                if (IsHiddenCard(c)) { skippedHidden++; continue; }                // 闸③（代理判据，`.c:88`）
                string gap;
                bool? ok = TargetCriteriaEval.MatchesRawCard(c, crit, out gap);    // 闸①（`.c:71-73`）
                if (ok == false) { skippedCrit++; continue; }
                if (ok == null) { unclear++; if (firstGap == null) firstGap = gap; }   // 判不了 ⇒ **保留** + 出声
                list.Add(c);
            }
            if (unclear > 0) NoteEnchantTargetCriteriaGap(enchantmentCard, row, caName, firstGap);
            ctx.Log($"{by}：`ca {caName}` 扫收藏取候选（原版 `GetCardToSummon.c:45-101`）：全池 " + pool.Count
                  + " 张 ⇒ " + list.Count + " 张（剔：`cantBeRandomlyCreated` " + skippedRandom
                  + " · 隐藏 " + skippedHidden + " · 准则 " + skippedCrit
                  + (unclear > 0 ? " · **判不了但保留 " + unclear + "**（已出声）" : "") + "）");
            return list;
        }

        /// <summary>支③ 的 `shouldRemoveFromCemetery`（`.c:138-148`）：原版对**候补那张卡**判
        /// `cardState == inCemetery`，是就从墓地摘掉（免得「墓地里还留着一张、场上又冒出一个」）。
        /// <para>我们只有「在场 = `UnitState` / 在墓地 = `PlayerState.Discard` + `DeadUnits`」两种表示
        /// ⇒ **候选还在场上**时原版条件不成立（`cardState` 不是 `inCemetery`）⇒ 什么都不做（出声说明）。</para></summary>
        static void RemoveChosenFromCemetery(BattleContext ctx, UnitState target, string caName, string by)
        {
            if (ctx == null || target == null) return;
            int p, slot;
            if (FindSlot(ctx, target, out p, out slot))
            {
                ctx.Log($"{by}：`ca {caName}` 的 `shouldRemoveFromCemetery = 1`，但候选「{target.Name}」**还在场上**"
                      + "（原版 `cardState == inCemetery` 不成立）⇒ 不从墓地摘");
                return;
            }
            int side = SeatOf(ctx, target);
            if (side < 0 || target.Card == null) return;
            var taken = ctx.TakeFromGraveyard(side, target.Card);
            ctx.Log($"{by}：`ca {caName}` 的 `shouldRemoveFromCemetery = 1` ⇒ 候选「{target.Name}」"
                  + (taken != null ? "已从墓地摘掉（原版 `RemoveCardFromCemetery`）"
                                   : "**墓地里没找到**（原版同样什么都不发生）"));
        }

        /// <summary>`ca 70` 的**请求落点**（原版 `PlayAbility.c:759-793` 的四档 `createAtPos`）。
        /// <para>⛔ 这里不另写落点算法 —— 只做「拿谁当锚点」这一跳，格号仍由
        /// <see cref="RuleCore.NextSlotToCard"/>（= 原版 `GetNextSlotToCard`）算。</para></summary>
        static int SummonRequestSlotOf(BattleContext ctx, int side, CardDef.AbilitySummonSpec spec,
                                       UnitState carrier, UnitState targetCard, string by)
        {
            if (ctx == null || spec == null) return -1;
            int cap = spec.CreateAtPos;
            if (cap < 0) return -1;                                  // 缺键/坏值 = 不知道（同 `SummonRequestSlotFor`，⛔ 别当 0）
            switch (cap)
            {
                case 0: return NextSlotToCard(ctx, side, carrier);    // `.c:760-767`：贴**施放者**（thisCard = 载体）
                case 1: return NextSlotToCard(ctx, side, targetCard); // `.c:769-774`：贴**卡目标**（abilityData.targetCard）
                case 3: return -1;                                    // `.c:775-790`：督军 ⇒ `GetNextSlotToCard` 直接返默认
                default:
                    // `.c:792-793`：`AddMinionOccupation(area, 0)` + `LogWarning`，**落点仍取默认**（`14` 也在这档）
                    ctx.Log($"{by}：`chosenAbility 70` 的 `createAtPos = {cap}` 不在 0 self / 1 cardTarget / 3 friendlyHero 三档内"
                          + " —— 原版 `AddMinionOccupation(area,0)` + LogWarning、落点仍取默认（我们的棋盘无占用表 ⇒ 只出声）");
                    return -1;
            }
        }

        // ---- 自检门（⚠️ 加在类尾；⛔ 生产侧别调 —— 纪律同 `RunRemoveThisEnchantmentForTriggerForTest`）----

        /// <summary>自检门：`ca 30 buffOthers` 的目标来源（换口后的那一支）。</summary>
        public static List<UnitState> ResolveBuffOthersTargetsForTest(BattleContext ctx, int owner, UnitState carrier,
                                                                     UnitState actingCard, UnitState chosen, int ta,
                                                                     CardDef card, CardDef.TriggerAbilityIndexSpec row)
        {
            return ResolveBuffOthersTargets(ctx, owner, carrier, actingCard, chosen, ta, card, row);
        }

        /// <summary>自检门：`ca 238` 那一记（预扣 + 退还）。返回 `RuleCodes`。</summary>
        public static int DeclareScriptedForceAttack238ForTest(BattleContext ctx, int p, int atkSlot, int tgtP, int tgtSlot, bool ranged)
        {
            return DeclareScriptedForceAttack238(ctx, p, atkSlot, tgtP, tgtSlot, ranged);
        }

    }
}
