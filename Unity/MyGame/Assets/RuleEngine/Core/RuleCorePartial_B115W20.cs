// RuleCorePartial_B115W20.cs — 第一百一十五会话 · 执行写手 `B115_W20` · **格 1：手牌 buff 桥**
//
// ── 这是什么 ─────────────────────────────────────────────────────────────────────────────────
//   原版 `AbilityLogic.PlayAbility` 在 `chosenAbility == 50 (buffHand)` 时，把这一行的
//   `abilityLogic.handBuff`（一个 **`HandEffect` 对象**）交给 `PlayerHand.AddHandEffect`；
//   后者（`PlayerHand__AddHandEffect.c:88-139`，164 行读全）做两件事：
//     ① `:90-110` **无条件**把这条 `HandEffect` 收进 `activeEffects`（`+0x48`），并给它的
//        `cardEffect.doNotStack(+0x75)` 置 1（`:99`）；
//     ② `:112-139` 遍历**当前手牌**，逐张 `FilterMethods.CheckIfMeetsCriteria(卡, …, HandEffect.targetCriteria(+0x20), …)`，
//        命中就 `CardScript__AddEffect(那张牌, HandEffect.cardEffect(+0x10), -1)`。
//   ⇒ **`buffHand` 不是新机制**：它就是「对每张命中的手牌做一次普通 `AddEffect`」
//      ＋「另记一条记录，供**后进手牌**的牌用（`PlayerHand__SetupCardInHand.c:58`）」。
//   🔴 `limitedUses / numberOfUses` 在 `AddHandEffect` 全文**一次都没读**（`:1-164`）——
//      它们的消费者在 `CardPlayedWithEffects.c:28-45`（打出时 `numberOfUses--`、到 0 摘记录）。
//      我们那一半**早就在**：`RuleCore.ApplyHandBuffs` 的「次数核销」段 + `RemoveHandEffectFromHand`。
//
// ── 我们怎么落 ───────────────────────────────────────────────────────────────────────────────
//   · 结构化载荷 = `CardDatabase.AbilityBuffSpec`（原版那个 `CardEffect` 的 26 格 + 本波新增的
//     `PlayersAffected / LimitedUses / NumberOfUses / Criteria` 四格）。
//     ⛔ **不拼 payload 字符串**（有损往返、丢格还是静默的 —— 调度台 2026-11-02 裁决 (b)）。
//   · 记录 = `CardInstance.HandEffect{ Spec = …, Op = null }`（`Op` / `Spec` 二选一，见那一格的 doc）。
//   · 「命中当前手牌」= `CardCriteria.MatchesRawCard(card, spec.Criteria, out gap)`
//     —— 同一个 `TargetCriteria` 36 格、同一个判据实现（⛔ 别另写一份筛）。
//     ⚠️ 判不出来（`null`）⇒ **不放行**（与 `RuleCore.HandEffectFits` 的收窄口径同一条）。
//   · 「兑现」= `ApplyAbilityBuffToHandCard`：**手牌侧只建了 `buffType 2 (changeCost)` 那一档**
//     （= 池内 10 条 `handBuff` 行的**全部**：现读 `enchantment_templates.json`，10/10 都是 `buffType 2`）。
//     其余 buffType 在手牌侧**没有落点**（例如加攻/加血要改的是「打出后的单位」，不是手牌），
//     一律 `ctx.Log` 出声 + 进 `unresolved`，⛔ 不假装做了。
//
// ── ⚠️ 仍未覆盖（如实标着，⛔ 别当成做了）──────────────────────────────────────────────────
//   ① **「后进手牌的牌」那一半今天接不上**：`RuleCore.HandEffectRegistry` 只收
//      `e.Op != null && e.Op.Verb == "give"` 的记录（`:11131`）⇒ 本文件登记的 `Spec` 记录
//      （`Op == null`）**不进**那份清单 ⇒ `SetupCardInHand` 不会把这条效果补给之后进手牌的牌。
//      要接上需要在 `RuleCore.cs` 的 `HandEffectRegistry` / `HandEffectFits` / `SetupCardInHand`
//      三处各加一支（`Spec != null` 那一档）；**那三个方法在 `RuleCore.cs`**，本代理按调度台
//      简报的 ⛔（别碰 `RuleCore.cs`）**没有动它们** ⇒ 补丁已写进报告 `B115_W20_手牌桥四格.md` §五。
//      影响面：**当前手牌**照常吃上（验收格走的就是这一档）；**之后抽到的**牌吃不上。
//   ② `ApplyHandBuffs` 打出那一刻对 `Spec` 条**不做任何事**（`Op == null` 直接被跳过、记录照旧清掉）——
//      对 `buffType 2` 是**对的**（费用在手牌里就结算完了，打出后不再有费用），
//      对「加攻/加血」那类会是缺口，但池内一条都没有。
//   ③ `untilEnemyTurnStart`（`+0x34`）我们**没有**那一档枚举 ⇒ 出声、按 `Never` 落（今天 0 条）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==========================================================================================
        //  入口：附魔模板某一行 `ca 50 buffHand` 的执行器
        // ==========================================================================================

        /// <summary>**`ca 50 buffHand` 的行执行器** —— 把这条 `(ab, lg)` 在 `abilityBuffs` 里的
        /// **`handBuff` 载荷行**按原版 `PlayerHand.AddHandEffect` 的形状落到手牌上。
        ///
        /// <para>调用点 = `EnchantmentExecutor_B115.RunAbilityBuffRowOfEnchantment`（原来那一支是
        /// `NoteEnchantBuffGap(… "本仓没有那个等价口")` —— 本文件就是那个等价口）。</para>
        /// <para>⚠️ **只吃 `IsHandBuff` 的行**：同一个 `(ab, lg)` 的 `buffEffect` 行走
        /// `ResolveAbilityBuffRows`（那是 `CardScript.AddEffect`），⛔ 两条别混。</para>
        /// </summary>
        /// <returns>真的落到手牌上的条数（0 = 没有 `handBuff` 载荷行 / 目标手牌一张都没命中）。</returns>
        internal static int RunHandBuffRowOfEnchantment(BattleContext ctx, int owner, UnitState carrier,
                                                        CardDef enchantmentCard,
                                                        CardDef.TriggerAbilityIndexSpec row, string by)
        {
            if (ctx == null || enchantmentCard == null || row == null) return 0;
            var rows = CardDatabase.AbilityBuffsOf(enchantmentCard);
            if (rows == null)
            {
                // `null` = 这一列**未知**（旧表 / 这张卡不在表里）—— ⛔ 不是「明确没有行」
                NoteEnchantBuffGap(enchantmentCard, row,
                    "`abilityBuffs` 列**缺失**（旧表）⇒ 判不了这一行的 `handBuff` 载荷 —— 不是「没有这条效果」");
                return 0;
            }

            int done = 0, found = 0;
            List<string> unresolved = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b == null || b.Ab != row.Ab || b.Lg != row.Lg) continue;
                if (!b.IsHandBuff) continue;
                found++;
                if (unresolved == null) unresolved = new List<string>(1);
                // `source` = **附魔卡名**（记录的身份 / 日志用）；`by` 只进日志。
                // ⚠️ 「不叠」的判据必须是 `(ab, lg, buffType, **附魔卡**)` —— 只按 `(ab,lg,buffType)`
                //    会让**另一件**附魔的同号行被误判成「已经有一条」而**整条吞掉**（池内 `ab0/lg0/bt2`
                //    的组合不止一件附魔在用）。
                done += AttachHandAbilityBuff(ctx, owner, enchantmentCard.Name ?? enchantmentCard.Id, by, b, unresolved);
            }
            if (found == 0)
            {
                // `handBuff` 对象存在但 `cardEffect.buffType == 0` ⇒ 生成器按 `AddEffect.c:964-966` 不落行
                NoteEnchantBuffGap(enchantmentCard, row,
                    "这一行在 `abilityBuffs` 里**没有 `handBuff` 载荷行**"
                    + "（`handBuff.cardEffect.buffType == 0` ⇒ 原版 `AddEffect.c:964-966` 什么都不做）");
            }
            if (unresolved != null && unresolved.Count > 0) ReportUnresolved(ctx, by, unresolved);
            return done;
        }

        // ==========================================================================================
        //  把一条 `HandEffect`（结构化）落到某一方的手牌上
        // ==========================================================================================

        /// <summary>**一条 `handBuff` 载荷 → 手牌**（原版 `PlayerHand.AddHandEffect` 那两段）。
        ///
        /// <para>**两段照抄原版的先后**（`:90-110` 先记记录、`:112-139` 再逐张贴牌）：
        /// ① 先按 `spec.PlayersAffected` 定「哪一方的手」并**无条件**登记记录；
        /// ② 再遍历那一方**当前手牌**，逐张过 criteria，命中的就 `ApplyAbilityBuffToHandCard` 并挂记录。</para>
        ///
        /// <para>⚠️ `PlayersAffected` 三档（`BoardCriteria`/`AbilityEffect` 同词）：
        /// `10 = Self`（施放者自己那手）· `20 = Enemy`（对方那手）· `0 = Both`（两边）。
        /// **`-1`（未知）/ 其它值 ⇒ 出声、不做** —— ⛔ 不拿 `10` 当默认（那会把「对手的手」做成「自己的手」）。</para>
        /// </summary>
        internal static int AttachHandAbilityBuff(BattleContext ctx, int casterOwner, string source,
                                                  string by,
                                                  CardDatabase.AbilityBuffSpec spec, List<string> unresolved)
        {
            if (ctx == null || spec == null) return 0;
            if (ctx.Players == null) return 0;

            int pa = spec.PlayersAffected;
            int first, second;
            if (pa == 10) { first = casterOwner; second = -1; }              // Self
            else if (pa == 20) { first = 1 - casterOwner; second = -1; }     // Enemy
            else if (pa == 0) { first = 0; second = 1; }                     // Both
            else
            {
                ctx.Log($"⚠️{by}：这条 `handBuff` 的 `playersAffected = {pa}` **认不出**"
                      + "（原版只认 0 Both / 10 Self / 20 Enemy）⇒ **不挂**（⛔ 不拿 10 当默认猜）");
                unresolved?.Add($"{by}（handBuff 的 playersAffected={pa} 认不出）");
                return 0;
            }

            int done = 0;
            for (int side = first; ; side = second)
            {
                if (side >= 0 && side <= 1 && ctx.Players[side] != null)
                    done += AttachHandAbilityBuffToSide(ctx, side, source, by, spec, unresolved);
                if (second < 0 || side >= second) break;
            }
            return done;
        }

        /// <summary>**只落到某一方的手牌**（原版一次 `AddHandEffect` 只作用于一只手）。
        /// 两段：先登记记录（无条件），再逐张当前手牌贴。</summary>
        static int AttachHandAbilityBuffToSide(BattleContext ctx, int side, string source, string by,
                                               CardDatabase.AbilityBuffSpec spec, List<string> unresolved)
        {
            var ps = ctx.Players[side];
            var recs = ps.HandEffectRecords;

            // ---- ① 记录（原版 `:90-110`）：**同一条效果不叠**（原版 `:99` 给 `doNotStack` 置 1）----
            for (int k = 0; k < recs.Count; k++)
            {
                var r0 = recs[k];
                if (r0 == null || r0.Spec == null) continue;
                if (r0.Spec.Ab != spec.Ab || r0.Spec.Lg != spec.Lg
                    || r0.Spec.BuffType != spec.BuffType) continue;
                if (!string.Equals(r0.Source, source, System.StringComparison.Ordinal)) continue;   // 另一件附魔的同号行 ⇒ 不是「同一条」
                ctx.Log($"（手牌效果：「{by}」在{ps.Name}的手上**已经有一条**同样的记录"
                      + $"（ab{spec.Ab}/lg{spec.Lg}/buffType{spec.BuffType}）⇒ 按原版 `doNotStack` 不叠）");
                return 0;
            }

            var entry = new CardInstance.HandEffect
            {
                Op = null,                       // ← 数据行那一档（`Op` / `Spec` 二选一，见 `HandEffect.Op` 的 doc）
                Spec = spec,
                Source = source,          // **附魔卡名**（身份位；日志另用 `by`）
                RecordId = ctx.NextHandEffectRecord(),
                Uses = HandBuffUsesOf(spec, ctx, by, unresolved),
                ActingUnit = ctx.ActingUnit,
                Target = null,                   // criteria 走 `Spec.Criteria`（36 格），⛔ 不造一个 `EffectTargetSpec` 近似
                Expire = HandBuffExpiryOf(spec, ctx, by),
                ExpireTurn = ComputeExpireTurn(spec, ctx),
            };
            recs.Add(entry);                     // ① 无条件登记（手上可能一张符合的都没有）

            // ---- ② 当前手牌逐张（原版 `:112-139`）----
            int n = 0;
            var hand = ps.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var inst = hand[i];
                if (inst == null || inst.Card == null) continue;
                string gap;
                if (!HandCardMatchesAbilityBuff(inst, spec, out gap))
                {
                    if (gap != null)
                        ctx.Log($"⚠️{by}：手牌「{inst.Card.Name}」**判不了**吃不吃得上"
                              + $"（{gap}）⇒ **不给它**（原版 `HandEffect.targetCriteria` 判不出来时我们"
                              + "按「宁可少给」走，同 `HandEffectFits` 的收窄口径）");
                    continue;
                }
                if (ApplyAbilityBuffToHandCard(ctx, side, inst, spec, by, unresolved))
                {
                    inst.HandEffects.Add(entry);     // 同一条记录对象（= 原版 `List.Add` 那一条 `CardEffect`）
                    n++;
                }
            }
            if (n == 0)
                ctx.Log($"（手牌效果：「{by}」在{ps.Name}的手上**一张牌都没命中** —— "
                      + "记录**照样登记**（原版 `PlayerHand__AddHandEffect.c:90-110` 先记记录、再贴牌），"
                      + "后进手牌的牌才吃得上）");
            return n;
        }

        /// <summary>`limitedUses` / `numberOfUses` → 共享计数盒（`null` = 不限次）。
        /// <para>判据：`HandEffect.limitedUses // +0x28` 是**开关**（`0` = 不限）、`numberOfUses // +0x2c` 是计数。
        /// ⇒ **`limitedUses &lt;= 0` ⇒ 不限次**（`Uses = null`）；否则用 `numberOfUses`。
        /// ⚠️ `limitedUses &gt; 0` 而 `numberOfUses &lt;= 0` = **坏数据**（现读池内 0 例：6 条 `1/1`、2 条 `0/0`）
        /// ⇒ 出声、按 `limitedUses` 当计数（⛔ 不落一个 `Left = 0` 的「立刻用完」）。</para></summary>
        static CardInstance.HandBuffUses HandBuffUsesOf(CardDatabase.AbilityBuffSpec spec, BattleContext ctx,
                                                        string by, List<string> unresolved)
        {
            if (spec.LimitedUses <= 0) return null;              // `0` = 不限次（原版开关）
            int left = spec.NumberOfUses;
            if (left <= 0)
            {
                ctx.Log($"⚠️{by}：这条 `handBuff` 的 `limitedUses = {spec.LimitedUses}`（限次）但"
                      + $" `numberOfUses = {spec.NumberOfUses}` —— 坏数据 ⇒ 按 `limitedUses` 当次数（原版 `+0x2c`）");
                unresolved?.Add($"{by}（handBuff numberOfUses 坏值）");
                left = spec.LimitedUses;
            }
            return new CardInstance.HandBuffUses { Limited = true, Left = left, Max = left };
        }

        /// <summary>到期档：`untilEndOfTurn` ⇒ `EndOfTurn` · 两条 `until*TurnStart` ⇒ `OwnerTurnStart`
        /// （与 `EffectResolver.AttachHandEffect` 的 `duration` 映射**同一套**）。
        /// <para>⚠️ `untilEnemyTurnStart(+0x34)` 我们**没有**那一档枚举 ⇒ 出声、按 `Never` 落
        /// （⛔ 不拿 `OwnerTurnStart` 顶 —— 那会在**错的时刻**摘）。现读池内 0 条。</para></summary>
        static CardInstance.HandBuffExpiry HandBuffExpiryOf(CardDatabase.AbilityBuffSpec spec,
                                                            BattleContext ctx, string by)
        {
            if (spec.UntilEndOfTurn != 0) return CardInstance.HandBuffExpiry.EndOfTurn;
            if (spec.UntilPlayerTurnStart != 0 || spec.UntilFollowingPlayerTurnStart != 0)
                return CardInstance.HandBuffExpiry.OwnerTurnStart;
            if (spec.UntilEnemyTurnStart != 0)
            {
                ctx.Log($"⚠️{by}：这条 `handBuff` 的 `untilEnemyTurnStart(+0x34)` **本仓没有那一档到期枚举**"
                      + " ⇒ 按「不过期」落（如实标着；⛔ 不拿 `OwnerTurnStart` 顶，那会在错的时刻摘）");
            }
            return CardInstance.HandBuffExpiry.Never;
        }

        /// <summary>到期回合号（配合 <see cref="CardInstance.HandBuffExpiry"/>；口径 = `ctx.Turn`）。
        /// 与 `EffectResolver.AttachHandEffect` 的 `duration` 分支**逐字同源**。</summary>
        static int ComputeExpireTurn(CardDatabase.AbilityBuffSpec spec, BattleContext ctx)
        {
            if (spec.UntilEndOfTurn != 0) return ctx.Turn;
            if (spec.UntilPlayerTurnStart != 0 || spec.UntilFollowingPlayerTurnStart != 0) return ctx.Turn;
            return 0;
        }

        /// <summary>**这张手牌吃不吃得上** —— 原版 `FilterMethods.CheckIfMeetsCriteria(卡, …, HandEffect.targetCriteria, …)`
        /// 的卡面级等价物（`TargetCriteriaEval.MatchesRawCard`，**同一份 36 格实现**）。
        ///
        /// <para>三态：`true` 命中 · `false` 明确不命中 · `gap != null` = **我们判不了**
        /// （某一格没建模 / 值认不出）⇒ 调用方**不放行**（同 `RuleCore.HandEffectFits` 的收窄口径：
        /// 「宁可少给，也不按『是单位卡就算』乱给」）。</para>
        /// <para>⚠️ `spec.Criteria == null` = 这一行没落 criteria（旧表）⇒ `gap` 非空、不放行 ——
        /// 原版 `FilterMethods__CheckIfMeetsCriteria.c` 头两句：criteria 缺失 ⇒ `LogError` + `return 0`（拒）。</para>
        /// </summary>
        static bool HandCardMatchesAbilityBuff(CardInstance inst, CardDatabase.AbilityBuffSpec spec, out string gap)
        {
            gap = null;
            if (inst == null || inst.Card == null) { gap = "手牌实例没有 Card"; return false; }
            if (spec == null) { gap = "载荷为空"; return false; }
            if (spec.Criteria == null)
            {
                gap = "`handBuff.targetCriteria` 缺列（旧表）⇒ 判不了（原版 criteria 缺失 = 拒）";
                return false;
            }
            bool? ok = CardCriteria.MatchesRawCard(inst.Card, spec.Criteria, out gap);
            return ok == true;
        }

        // ==========================================================================================
        //  手牌侧的 `AddEffect` —— 今天只有 `buffType 2 (changeCost)` 有落点
        // ==========================================================================================

        /// <summary>**原版 `CardScript.AddEffect` 在「手牌」这张牌上的等价物**（逐 `buffType` 一支）。
        ///
        /// <para>为什么单开一支、不复用 `ApplyAbilityBuff`：那个方法的靶是 **`UnitState`**（在场上），
        /// 而这里靶是**还在手里的 `CardInstance`**（没有 `UnitState`）—— 两者能落的属性不同：
        /// 手牌只有**费用**（`ctx.CostMods`）这一本账；攻/血/关键词都要等它打出、
        /// 有了 `UnitState` 才有落点（见本文件头「仍未覆盖 ②」）。</para>
        ///
        /// <para>🔴 **`changeCost(2)` 的落法**（判据 = `CardScript__AddEffect.c:331-459` 那个合并循环
        /// 累加 `costChange(+0x68)`，`EntityScript__CurrentCost` 现算）：
        /// 在 `ctx.CostMods` 里登记一条 **钉在这一份**（`HandInstanceId = inst.Id`）的修正 ——
        /// 原版的 `costChange` 挂在**那张牌自己的** `activeEffects` 上，不是「按卡名作用于所有同名」。
        /// ⚠️ 到期：`untilEndOfTurn` ⇒ `ExpireTurn = ctx.Turn`（`CostModApplies` 是 `ctx.Turn > ExpireTurn ⇒ 失效`，
        /// 与 `DoLowerCost` 的 `op.Duration == "turn"` 那一路**逐字同源**）；其余 ⇒ `-1`（永久，直到记录被摘）。</para>
        /// </summary>
        /// <returns>真的改了状态返回 `true`（调用方据此决定「这条记录挂不挂到这张牌上」）。</returns>
        static bool ApplyAbilityBuffToHandCard(BattleContext ctx, int owner, CardInstance inst,
                                               CardDatabase.AbilityBuffSpec spec, string by,
                                               List<string> unresolved)
        {
            if (ctx == null || inst == null || spec == null) return false;
            var card = inst.Card;

            switch (spec.BuffType)
            {
                case 2:      // `changeCost`（`CardScript__AddEffect.c:331-459` 的 `costChange` 那一半）
                {
                    if (spec.CostChange == 0)
                    {
                        ctx.Log($"{by}：「{card?.Name}」的 `changeCost(2)` 载荷 `costChange == 0`"
                              + " ⇒ 无状态变化（原版那一支同样只 `List.Add`，净 0）");
                        return true;
                    }
                    int expire = spec.UntilEndOfTurn != 0 ? ctx.Turn : -1;
                    ctx.CostMods.Add(new CostMod
                    {
                        Player = owner,
                        HandOf = owner,
                        Key = "*",                       // ⛔ 别写卡 id —— 那样同名的另一份也会被改
                        HandInstanceId = inst.Id,        // 钉在**这一份**上（原版 `costChange` 长在牌自己的 `activeEffects` 上）
                        Delta = spec.CostChange,
                        ExpireTurn = expire,
                    });
                    ctx.Log($"（手牌效果：「{inst.Card?.Name}」的费用 {(spec.CostChange > 0 ? "+" : "")}{spec.CostChange}"
                          + $"（钉在这一份上，id={inst.Id}；{(spec.UntilEndOfTurn != 0 ? "本回合结束失效" : "不过期")}）"
                          + " —— 原版 `PlayerHand__AddHandEffect.c:126-135` → `CardScript.AddEffect` 的 `changeCost`）");
                    return true;
                }

                default:
                    // ⚠️ 如实标着：手牌侧**没有**这些 buffType 的落点（要 `UnitState`）。现读池内 0 行。
                    ctx.Log($"⚠️{by}：「{card?.Name}」的 `handBuff` 是 `buffType {spec.BuffType}` —— "
                          + "**手牌侧没有落点**（本仓只有 `changeCost(2)` 那一档；其余要等它打出、"
                          + "有 `UnitState` 才能落）⇒ **这条不改状态**（⛔ 不假装做了）");
                    unresolved?.Add($"{by}（handBuff 的 buffType {spec.BuffType} 手牌侧未建）");
                    return false;
            }
        }

        // ==========================================================================================
        //  「后进手牌的牌」那一半（原版 `PlayerHand__SetupCardInHand.c:58-71`）
        // ==========================================================================================

        /// <summary>**补挂一条 `Spec`（数据行）记录** —— 原版 `CardScript.AddEffect(card, handEffect.cardEffect)`；
        /// 它传的是 `List.get_Item(hand.activeEffects, i)` 那**同一个 `CardEffect` 对象**
        /// ⇒ 我们把**同一条** `HandEffect` 记录挂到新进手牌的那一份上（`Spec` / `Uses` 共享，
        /// 与文本那一路的 `EffectResolver.AttachHandEffectCopy` 逐字同形）。
        ///
        /// <para>调用点 = `EffectResolver.AttachHandEffectCopy` 开头那一句（`src.Op == null` 那一档转进来）。</para>
        /// <para>去重 = 原版 `CardScript.AlreadyContainsEffect`：这一份身上**已经有**同一条
        /// （同 `Spec` 引用 + 同 `Source`）⇒ 不补（⛔ 别放宽：放宽会让同一张牌二次进手牌时翻倍）。</para>
        /// </summary>
        internal static bool AttachHandAbilityBuffCopy(BattleContext ctx, CardInstance inst,
                                                     CardInstance.HandEffect src)
        {
            if (ctx == null || inst == null || inst.Card == null || src == null || src.Spec == null) return false;
            for (int i = 0; i < inst.HandEffects.Count; i++)
            {
                var e0 = inst.HandEffects[i];
                if (e0 == null || e0.Spec == null) continue;
                if (ReferenceEquals(e0.Spec, src.Spec)
                    && string.Equals(e0.Source, src.Source, System.StringComparison.Ordinal)) return false;
            }
            int owner = HandOwnerOf(ctx, inst);
            if (owner < 0) return false;                     // 不在任何手牌里 ⇒ 没有「进手牌」这回事
            string gap;
            if (!HandCardMatchesAbilityBuff(inst, src.Spec, out gap))
            {
                if (gap != null)
                    ctx.Log($"⚠️（进手牌：「{inst.Card.Name}」判不了吃不吃得上「{src.Source}」（{gap}）"
                          + " ⇒ **没给它**（同 `HandEffectFits` 的「判不出来就不放行」））");
                return false;
            }
            var unresolved = new List<string>(1);
            if (!ApplyAbilityBuffToHandCard(ctx, owner, inst, src.Spec, src.Source, unresolved))
            {
                if (unresolved.Count > 0) ReportUnresolved(ctx, src.Source, unresolved);
                return false;
            }
            inst.HandEffects.Add(src);
            ctx.Log($"（进手牌：「{inst.Card.Name}」吃上了**还在生效**的手牌效果「{src.Source}」（数据行 "
                  + $"ab{src.Spec.Ab}/lg{src.Spec.Lg}/buffType{src.Spec.BuffType}）"
                  + " —— 原版 `PlayerHand__SetupCardInHand.c:71`）");
            return true;
        }
    }
}
