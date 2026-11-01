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
                                                           $"{(carrier.Name ?? "?")}·{card.Name ?? "?"}·trigger#{triggerNumber}");
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

        /// <summary>`TargetCriteria.targetsAffected` 里**不需要那 30 格 criteria 就能定下来**的四个：
        /// `20 self` · `80 actingCard` · `120 friendlyHero` · `130 enemyHero`。
        /// 判据 = `AbilityLogic__GetTargets.c:184-194`（self）/ `:761-769`（actingCard）/ `:721-736`（友方督军）/
        /// `:738-750`（敌方督军）；其余档（`0 any` · `10 allFiltered` · `30 target` · `50 inHandFiltered` …）
        /// **都要读 criteria 或调用方点选** ⇒ 本版不猜。</summary>
        static List<UnitState> ResolveBuffOthersTargets(BattleContext ctx, int owner, UnitState carrier,
                                                        UnitState actingCard, int ta, CardDef card, CardDef.TriggerAbilityIndexSpec row)
        {
            UnitState one = null;
            switch (ta)
            {
                case 20: one = carrier; break;
                case 80: one = actingCard; break;
                case 120: one = WarlordOfSide(ctx, owner); break;
                case 130: one = WarlordOfSide(ctx, 1 - owner); break;
                default:
                    NoteEnchantBuffGap(card, row,
                        $"`ca 30 buffOthers` 的 `ta = {ta}` 需要那 30 格 `targetCriteria`（或调用方点选的那张）"
                      + "才定得出目标集，而第二张表只落了 `ta` ⇒ **本行不结算**（⛔ 不猜「全场上」）");
                    return null;
            }
            if (one == null)
            {
                NoteEnchantBuffGap(card, row, $"`ca 30 buffOthers` 的载体（`ta = {ta}`）本刻取不到 ⇒ 本行不结算");
                return null;
            }
            return new List<UnitState>(1) { one };
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

        /// <summary>`ca 30 / 40 / 50` 的行执行器（判据 → 本段头注）。</summary>
        internal static int RunAbilityBuffRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                           CardDef enchantmentCard, CardDef.TriggerAbilityIndexSpec row,
                                                           UnitState actingCard, string by)
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
                targets = ResolveBuffOthersTargets(ctx, owner, carrier, actingCard, row.Ta, enchantmentCard, row);
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
    }
}
