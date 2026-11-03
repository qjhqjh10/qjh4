// RuleCorePartial_B123E2.cs — 批次 B123 · 执行代理 **E2** · `ca 30 buffOthers` 的 `ta = 50 inHandFiltered` **手牌目标路**
//
// ── 这是什么 ─────────────────────────────────────────────────────────────────────────────────
//   原版 `AbilityLogic.PlayAbility` 的 `case 0x1e (30 buffOthers)`（`AbilityLogic__PlayAbility.c:1176-1250`）是
//   「对 `AbilityLogic.GetTargets(thisCard, targetCriteria, …)` 的**每个目标**克隆一份 `CardEffect` 再
//   `BattleManager.AddEffect(那张牌, 克隆)`」。**目标可以是手牌里的卡** —— `GetTargets.c:893-901`：
//     `if (criteria.targetsAffected == 0x32 /* 50 inHandFiltered */)`
//       `list = BattleManager.GetCardsInHandList(bm, criteria+0x17, +0x18, +0x19, +0x1a, friendlyUnits, enemyUnits, 0)`
//   建完池后**照样**走 `:1086-1095` 的 `FilterMethods.CheckIfMeetsCriteria` 逐候选筛 + `maxTargets` 裁剪
//   （与棋盘族同一段尾巴，见 `CountEval.cs:1110-1132` 那段头注的同一份判据）。
//   ⇒ 这一档的落点是**手牌实例**上的 `AddEffect`，**不是** `PlayerHand.AddHandEffect`。
//
// ── ⛔ 为什么【不能】与 `ca 50 buffHand` 合流（调度台 2026-11-03 裁定）─────────────────────────
//   · `ca 50`（`RunHandBuffRowOfEnchantment` / `AttachHandAbilityBuff`）= `PlayerHand.AddHandEffect`：
//     先把记录**无条件登记**进 `ps.HandEffectRecords`，再逐张当前手牌贴 ⇒ **之后进手牌的牌也吃**。
//   · `ca 30 + ta 50`（本文件）= 逐目标 `AddEffect`：只作用于**结算那一刻在手的那几张**，
//     ⛔ **不登记** `ps.HandEffectRecords`（登记了就是加宽到后进手牌的牌 = 原版没有的行为）。
//   ⇒ 两条路的差别就在「记录登记」这一句，⛔ 别为了少写代码合流。
//
// ── 我们怎么落 ───────────────────────────────────────────────────────────────────────────────
//   · 目标解析 = `ResolveBuffOthersHandTargets`：按 criteria 的 `friendlyUnits(+0x14)` / `enemyUnits(+0x15)`
//     （**施法者视角**，同 `CountEval.SideMatches` / `FilterMethods__CheckIfMeetsCriteria.c:210-232`）
//     定「哪几只手」，再逐张过 **`CountEval.CardPoolCandidatePasses`**（= 卡面级 `CheckIfRawCardMeetsCriteria`
//     的唯一实现 + 那几格「要『还有谁』才判得了」的补充），最后按 `maxTargets` / `targetSelectMethod` 裁剪
//     （`GetTargets.c:1164-1365`，与 `ResolveEnchantmentTargets` 同一套写法）。
//   · 载荷 = `abilityBuffs` 里该 `(ab, lg)` 的 **`buffEffect` 行**（`IsHandBuff == false`）——
//     `handBuff` 行是 `ca 50` 的载荷，⛔ 不在这里消费。
//   · 兑现 = `RuleCore.AttachHandAbilityBuffToInstances`（`RuleCorePartial_B115W20.cs` 类尾）：
//     逐张 `HandOwnerOf` → `ApplyAbilityBuffToHandCard` → `inst.HandEffects.Add(entry)`。
//
// ── 现读覆盖（2026-11-03 `enchantment_templates.json` v?)──────────────────────────────────────
//   `ca 30 ∧ ta 50` 共 **3 行 / 2 张模板**：`Living Icon`（我方手 · 兵 · `manaCostFilter` · max1）·
//   `Solar Eclipse`（我方手 `ab0/lg0` + 敌方手 `ab0/lg1`，各 max1）。载荷都是 `buffType 2 / costChange -1`。
//   ⇒ 改前这 3 行**完全不结算**（旧 `ResolveBuffOthersTargets` 对 `ta == 50` 直接出声返回 `null`）。
//
// ── 🧨 灭自证（改哪一句会红）────────────────────────────────────────────────────────────────
//   · 把「按 `maxTargets` 裁剪」删掉 ⇒ 目标数从 1 变 2 ⇒ 断言 `E2.1/E2.2` 红；
//   · 把「手牌侧不登记 `HandEffectRecords`」改成 `recs.Add(entry)` ⇒ 后抽到的同名牌也吃上 ⇒ `E2.5` 红；
//   · 把「钉在这一份（`HandInstanceId = inst.Id`）」改成按卡 id ⇒ 手里同名的另一份也变价 ⇒ `E2.4` 红；
//   · 把 `friendlyUnits` / `enemyUnits` 两格忽略（= 两边手牌全给）⇒ `E2.3` 红。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==========================================================================================
        //  `ca 30 buffOthers` + `ta 50`：手牌目标解算
        // ==========================================================================================

        /// <summary>`ca 30 buffOthers` 的 `ta = 50 inHandFiltered` **手牌目标集**
        /// （原版 `AbilityLogic__GetTargets.c:893-901` 建池 + `:1086-1095` 筛 + `:1164-1365` 裁）。
        ///
        /// <para>⚠️ **三态**（同 `ResolveEnchantmentTargets` 的契约）：`null` = 判不了（**已出声**）；
        /// 空表 = 判得出、但一张都没有。</para>
        /// <para>⚠️ **不做的事**：不碰 `ps.HandEffectRecords`（那是 `ca 50 buffHand` 的语义）；
        /// 不改任何卡的状态（只挑目标）。</para>
        /// </summary>
        internal static List<CardInstance> ResolveBuffOthersHandTargets(BattleContext ctx, int owner,
                                                                        UnitState carrier, UnitState chosen,
                                                                        CardDef card,
                                                                        CardDef.TriggerAbilityIndexSpec row,
                                                                        string caName)
        {
            if (ctx == null || ctx.Players == null || ctx.Players.Length < 2) return null;
            var set = card != null ? card.AbilityTargetCriteriaAt(row.Ab, row.Lg) : null;
            if (set == null)
            {
                // 原版两条 `?? :` 里的「不指定玩家」那一档我们能读；但**哪一只手**必须由那两格定
                // ⇒ 没有 36 格就不能猜（⛔ 不拿「两边手牌全给」顶：那会在对手的手上也压一张牌）。
                NoteEnchantBuffGap(card, row,
                    "`ca 30 buffOthers` 的 `ta = 50 inHandFiltered` 要按 `targetCriteria` 的 "
                  + "`friendlyUnits` / `enemyUnits` 定「哪一方的手牌」，而这一行**没有 36 格**（旧表）"
                  + " ⇒ **本行不结算**（⛔ 不拿「两方手牌全给」顶）");
                return null;
            }
            if (set.FriendlyUnits == CardDef.CountSetSpec.Unknown
                || set.EnemyUnits == CardDef.CountSetSpec.Unknown)
            {
                NoteEnchantBuffGap(card, row,
                    "`ca 30 buffOthers` 的 `ta = 50`：`friendlyUnits` / `enemyUnits` 有一格**未知**（没落）"
                  + " ⇒ 判不出候选是哪一只手 ⇒ **本行不结算**");
                return null;
            }

            var kept = new List<CardInstance>(4);
            bool noted = false;
            for (int side = 0; side < 2; side++)
            {
                if (!HandSideIncluded(owner, side, set)) continue;
                var ps = ctx.Players[side];
                if (ps == null || ps.Hand == null) continue;
                for (int i = 0; i < ps.Hand.Count; i++)
                {
                    var inst = ps.Hand[i];
                    if (inst == null || inst.Card == null) continue;
                    bool? pass = CountEval.CardPoolCandidatePasses(owner, side, set, inst.Card, carrier, inst,
                                                                   out string why);
                    if (pass == null)
                    {
                        // 契约同 `ResolveEnchantmentTargets`：判不了的候选**保留** + 出声（⛔ 不当「不命中」静默剔掉）
                        kept.Add(inst);
                        if (!noted) { noted = true; NoteEnchantTargetCriteriaGap(card, row, caName, why); }
                    }
                    else if (pass.Value) kept.Add(inst);
                }
            }

            // `maxTargets` 裁剪（`GetTargets.c:1164-1365`）—— 与 `ResolveEnchantmentTargets` 同一套写法。
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
                var copy = new List<CardInstance>(kept);
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

        /// <summary>`friendlyUnits(+0x14)` / `enemyUnits(+0x15)` —— 相对**这条 ability 的所有者**的阵营闸。
        /// <para>判据 = `FilterMethods__CheckIfMeetsCriteria.c:210-232`（与 `CountEval.SideMatches` 同一句：
        /// 两格都开 = 两边都留；只开一格 = 只留那一侧；**两格都不开 ⇒ 一个候选都不留**）。</para>
        /// <para>⚠️ 这里重写一行的理由：`CountEval.SideMatches` 是 **private**（同程序集但跨类不可见），
        /// 而本文件在 `RuleCore` 这一侧。⛔ 别把这个判据扩散成第三份 —— 消费方只有本文件。</para></summary>
        static bool HandSideIncluded(int owner, int side, CardDef.CountSetSpec set)
        {
            int friendly = set.FriendlyUnits, enemy = set.EnemyUnits;
            if (friendly != 0 && enemy != 0) return true;
            if (friendly != 0) return side == owner;
            if (enemy != 0) return side != owner;
            return false;
        }

        // ==========================================================================================
        //  `ca 30 buffOthers` + `ta 50`：行执行器
        // ==========================================================================================

        /// <summary>`ca 30 buffOthers` 的 `ta = 50` 行执行器 —— 逐手牌目标 `AddEffect`
        /// （原版 `AbilityLogic__PlayAbility.c:1176-1250` 的 `case 0x1e`）。
        ///
        /// <para>载荷 = 该 `(ab, lg)` 的 **`buffEffect` 行**（`IsHandBuff == false`）；`handBuff` 行属 `ca 50`，
        /// ⛔ 不在这里消费。</para>
        /// <para>⚠️ 与 `RunHandBuffRowOfEnchantment` 的分工（⛔ 别合流）→ 本文件头注。</para>
        /// </summary>
        /// <returns>真的改到手牌状态的（行 × 卡）条数。</returns>
        internal static int RunBuffOthersHandRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                              CardDef enchantmentCard,
                                                              CardDef.TriggerAbilityIndexSpec row,
                                                              UnitState actingCard, string by, UnitState chosen)
        {
            if (ctx == null || enchantmentCard == null || row == null) return 0;
            var targets = ResolveBuffOthersHandTargets(ctx, owner, carrier, chosen, enchantmentCard, row,
                                                       "30 buffOthers");
            if (targets == null) return 0;
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：`ca 30 buffOthers` 的 `ta = 50` **目标集为空**（那一侧手牌里没有命中 criteria 的卡）"
                      + " ⇒ 本条不改状态（原版 `PlayAbility.c:1176-1250` 逐个目标 `AddEffect`，零目标 = 零次）");
                return 0;
            }

            var rows = CardDatabase.AbilityBuffsOf(enchantmentCard);
            if (rows == null)
            {
                // `null` = 这一列**未知**（旧表）—— ⛔ 不是「明确没有行」
                NoteEnchantBuffGap(enchantmentCard, row,
                    "`abilityBuffs` 列**缺失**（旧表）⇒ 判不了这一行的 `buffEffect` 载荷 —— 不是「没有这条效果」");
                return 0;
            }

            int applied = 0, found = 0;
            var unresolved = new List<string>(1);
            // `source` = **附魔卡名**（记录的身份）；`by` 只进日志。同 `RunHandBuffRowOfEnchantment`。
            string source = enchantmentCard.Name ?? enchantmentCard.Id;
            for (int k = 0; k < rows.Count; k++)
            {
                var b = rows[k];
                if (b == null || b.Ab != row.Ab || b.Lg != row.Lg) continue;
                if (b.IsHandBuff) continue;                  // `handBuff` 走 `ca 50`，⛔ 不在这里消费
                found++;
                applied += AttachHandAbilityBuffToInstances(ctx, owner, source, by, b, targets, unresolved);
            }
            if (found == 0)
            {
                // `buffEffect.cardEffect.buffType == 0` ⇒ 生成器按 `AddEffect.c:964-966` 不落行
                NoteEnchantBuffGap(enchantmentCard, row,
                    "这一行在 `abilityBuffs` 里**没有 `buffEffect` 载荷行**"
                  + "（`cardEffect.buffType == 0` ⇒ 原版 `AddEffect.c:964-966` 什么都不做）");
            }
            if (unresolved.Count > 0) ReportUnresolved(ctx, by, unresolved);
            return applied;
        }

        // ==========================================================================================
        //  自检门（⚠️ 生产侧别调 —— 纪律同 `ResolveBuffOthersTargetsForTest`）
        // ==========================================================================================

        /// <summary>自检门：只解目标集（不结算），用来把「裁剪 / 阵营闸 / criteria 筛」钉住。</summary>
        public static List<CardInstance> ResolveBuffOthersHandTargetsForTest(BattleContext ctx, int owner,
                                                                             UnitState carrier, UnitState chosen,
                                                                             CardDef card,
                                                                             CardDef.TriggerAbilityIndexSpec row)
        {
            return ResolveBuffOthersHandTargets(ctx, owner, carrier, chosen, card, row, "30 buffOthers");
        }
    }
}
