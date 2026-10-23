// CountEval.cs — 「数几个、跟谁比」的求值器（原版 `BoardCriteria.countCriteria`）
//
// 背景（账 `新账-97` ④ / 审计 `E-A` §3·3）：
//   `role == "count"` 的条件行在数据里**早就落全了**（v16 起连「数谁」的 `countSet` 也有），
//   但全仓**没有消费者** ⇒ `EC58` / `SOR70` / `AM43` / `SOR42` 这 4 张卡一直靠正文正则兜底。
//   本文件是那个消费者；它只回答「这一格 count 条件成不成立」，⛔ 不碰目标筛选。
//
// 原版判据（🔴 **2026-10-23 逐行现读**，不是按字段名猜的）：
//   · `BoardCriteria`（`d:/2/tools/il2cpp_out/dump.cs:20128-20135`）：
//     `playerTurn`(0x10) / `countCriteria`(0x18) / `countOptions`(0x20) / `triggerIf`(0x24) / `referenceValue`(0x28)。
//   · 真正被判的是 `countCriteria` 那张 `TargetCriteria` —— 逐候选走
//     `FilterMethods__CheckIfMeetsCriteria.c`。**那一段读出来的口径**（本文件的判据正本）：
//       ① `friendlyUnits`(0x14) / `enemyUnits`(0x15) 是**相对 thisCard 的阵营闸**
//          （`CheckIfMeetsCriteria.c:210-232`）：同方且 `friendly==false` ⇒ 淘汰；异方且 `enemy==false` ⇒ 淘汰。
//          ⇒ **两个都是 false = 一个候选都不留（count 恒 0）**，⛔ 不是「不过滤」。
//       ② `includeHero`(0x16) / `includeMinions`(0x17) / `includeSpells`(0x18) / `includeWhispers`(0x19)
//          是**卡种白名单**（`:286-300`：`cardType` 10/0/20/40 各自「这一位为假 ⇒ 淘汰」）。
//          `includeRemnants`(0x1b) 同理是**残骸闸**（`:255-261`：为假且 `isRemnant` ⇒ 淘汰）。
//       ③ `includeSelf`(0x1d) / `excludeNotSelf`(0x1e)（`:234-244`）：
//          候选 == thisCard 且 `includeSelf==false` ⇒ 淘汰；候选 != thisCard 且 `excludeNotSelf==true` ⇒ 淘汰。
//       ④ `armyFilter`(0x28) / `keywordFilter`(0x2c) / `traitsFilter`(0x34) / `spellType`(0x68) /
//          `filterSpecificCards`(0x70) / `excludeEnchantments`(0x78) / 四个 `*Filter`+`*FilterOption`
//          （攻/血/费/稀有度）**都在这同一段里**（`:300-420`）。本版**一个都没实现** ⇒ 只要不是
//          「不适用」值就 **返回 null（判不了）**，⛔ 绝不拿「不过滤」冒充（那会静默多数人）。
//   · `countOptions`（`CountOptions`，`BoardCriteria` **+0x20**）：`0 AllTargets` = 数命中个数；
//     `10 EnemiesMinusAllies` = **逐个候选按「同方 -1 / 异方 +1」累加**（候选池仍是 `countCriteria`
//     选出来的那一批）。🔴 **2026-10-23（`A12`）订正**：本文件原来写「全量反编译里找不到 `countOptions`
//     的消费点」—— **那句是错的**，消费者就在 `BoardAnalysis__MeetsCriteria.c:104-118`
//     （`if (*(int *)(param_2 + 0x20) == 10)` 起的那段循环：`iVar7 = 1; if (候选.isPlayer(+0x40)
//     == 本条 criteria 的 card.isPlayer(+0x40)) iVar7 = -1; iVar6 += iVar7`）。
//     ⇒ 本版**实现 0 与 10**；其它值仍返回 null。
//   · `triggerIf`（`StatFilterOptions`）：`0 NotApplicable` / `1 EqualTo` / `2 EqualOrHigher` /
//     `3 EqualOrLower` 四档；`4-11`（`…ToThisUnitAttack` / `RandomChance` / `…Alt`）不是「跟一个常数比」
//     ⇒ 未实现、返回 null。
//   · `playerTurn`（`PlayersAffected` 0 Both / 10 Self / 20 Enemy，`BoardCriteria` **+0x10**）：
//     🔴 **2026-10-23（`A12`）订正** —— 它**有**消费点，在 `BoardAnalysis__MeetsCriteria.c:39-52`：
//       `playerTurn == 10(Self)`  ⇒ 要求 `BattleManager.IsPlayerTurn(bm) == thisCard.isPlayer`，否则整条**假**；
//       `playerTurn == 20(Enemy)` ⇒ 要求两者**不相等**，否则整条**假**。
//     我们这边 `IsPlayerTurn()` = `ctx.Active`、`thisCard.isPlayer` = 本条 ability 的 `owner`
//     （`RuleCore.cs:9361` 那条同口径换算）。
//     ⇒ 这是一道**整条 BoardCriteria 的闸**（不是「取哪一边数」），判假就 `return false`，⛔ 不是 null。
//
// 🔴 **本版不猜**：任何一格是「未知（-1）」或「本版没实现的筛」⇒ 返回 `null` 并给出 `gap` 文案，
//    由 `EffectResolver` 的正文兜底接手并出声（同 `ConditionEval` 的 `null` 契约）。

namespace RuleEngine
{
    public static class CountEval
    {
        /// <summary>
        /// 判一条 `role == "count"` 的条件。返回 `null` = **本版判不了**（缺 `CountSet` /
        /// 未实现的 `targetsAffected` / 未实现的筛维度 / 未实现的比较档 / 某个候选的条件判不了）。
        /// <paramref name="actor"/> = 这条 ability 的**所有者单位**（拿得到就传，`includeSelf` /
        /// `excludeNotSelf` 两格要它；拿不到传 `null`，那两格**只在候选就是它时**才有区别）。
        /// </summary>
        public static bool? Meets(BattleContext ctx, int owner, CardDef.ConditionSpec spec, UnitState actor = null)
        {
            string gap;
            return Meets(ctx, owner, spec, actor, out gap);
        }

        /// <summary>同上，多一个 <paramref name="gap"/>（**判不了时**写清是哪一格没实现）。</summary>
        public static bool? Meets(BattleContext ctx, int owner, CardDef.ConditionSpec spec, UnitState actor,
                                  out string gap)
        {
            gap = null;
            if (ctx == null || spec == null || spec.CountSet == null) { gap = "没有 countSet"; return null; }
            if (spec.Condition <= 0) { gap = "cond 不是一条可数的条件"; return null; }

            // ---- `playerTurn` 闸（原版 `BoardAnalysis__MeetsCriteria.c:39-52`）----
            // 🔴 它是**整条 BoardCriteria 的闸**，判假 = 这条条件**不成立**（⛔ 不是「判不了」）。
            //    判据：`IsPlayerTurn(bm) != thisCard.isPlayer(+0x40)` ⇒ `return false`
            //    （`10 Self`）；`20 Enemy` 是同一个比较取反。
            int pt = spec.CountSet.PlayerTurn;
            if (pt != 0)
            {
                if (pt != 10 && pt != 20)
                {
                    gap = pt == CardDef.CountSetSpec.Unknown
                        ? "countSet.playerTurn **未知（没落）**"
                        : $"countSet.playerTurn={pt} 本版未实现（只实现 0/10/20）";
                    return null;
                }
                bool isMyTurn = ctx.Active == owner;
                if (pt == 10 && !isMyTurn) return false;      // 原版 Self：不是自己回合 ⇒ 整条假
                if (pt == 20 && isMyTurn) return false;       // 原版 Enemy：是自己回合 ⇒ 整条假
            }

            // `countOptions`：`0 AllTargets` 或 `10 EnemiesMinusAllies`（见文件头 ③ 的订正）。
            // ⚠️ `BoardAnalysis__MeetsCriteria.c` 的形状是「**只有 `== 10` 那一支特判**，其余一律
            //    按 `iVar6 = list.Count` 走」⇒ 原版里任何非 10 的值都等价于 AllTargets。
            //    本版仍**只认 0 / 10**、其余返回 null 并出声（宁可不判也不猜：将来枚举长出新值时，
            //    「按 AllTargets 处理」会静默错数，而 `gap` 文案能让人看见）。池内 6 条 `count` 行
            //    的 `opt` 现读**全是 0** ⇒ 今天零影响。
            if (spec.CountOption != 0 && spec.CountOption != 10)
            {
                gap = spec.CountOption == CardDef.ConditionUnknown
                    ? "countOptions **未知（没落）**"
                    : $"countOptions={spec.CountOption} 本版未实现（只实现 0 / 10）";
                return null;
            }

            var set = spec.CountSet;
            string why = SetGap(set);
            if (why != null) { gap = why; return null; }

            int count;
            if (CountSet(ctx, owner, set, spec.Condition, spec.CountOption, actor, out count, out why) == null)
            {
                gap = why ?? "集合/条件判不了";
                return null;
            }

            bool? r = Compare(count, spec.StatOption, spec.ReferenceValue);
            if (r == null) gap = $"比较档 triggerIf={spec.StatOption} 本版未实现（只实现 1/2/3）";
            return r;
        }

        /// <summary>
        /// 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：数一条 `TargetCriteria` 选出来的**目标个数**。
        ///
        /// <para>**谁用**：`role == "mult"` 的乘数 —— 原版 `AbilityLogic__PlayAbility.c:181-236`：
        /// `GetTargets(manager, abilityLogic.multiplier, …)` 的**个数**就是 `local_178`
        /// （`targetsAffected == any(0)` ⇒ **1**），随后 `if (0 < furtherMultiplier) local_178 *= furtherMultiplier`
        /// （那一格我们走 `EffectOp.AbilityFurtherMultiplier`，不在这里）。</para>
        ///
        /// <para>与 <see cref="Meets"/> 的区别（⛔ 别混用）：`multiplier` 是**纯 `TargetCriteria`**，
        /// **没有** `BoardCriteria.playerTurn` / `countOptions` / `triggerIf` 三格，也**不比较**
        /// —— 它只给个数。所以本方法**不读** `spec.CountSet.PlayerTurn`（那格恒 `-1`）。</para>
        ///
        /// <para>`null` = 本版判不了（`gap` 写清是哪一格）：`targetsAffected ∈ {210,220}`（原版是
        /// **累加 `traitsFilter` 的值**，不是数个数 —— 池内 `mult` 行 0 条用到，⛔ 不拿个数顶）；
        /// 以及 <see cref="CountSet"/> 没实现的那些池（手牌 / 牌库 / adjacent / 伪列表 …）。</para>
        /// </summary>
        public static int? CountTargets(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond,
                                        UnitState actor, out string gap)
        {
            gap = null;
            if (ctx == null || set == null) { gap = "没有 criteria"; return null; }
            int ta = set.TargetsAffected;
            if (ta == CardDef.CountSetSpec.Unknown)
            {
                gap = "`targetsAffected` **未知（没落）**";
                return null;
            }
            // 原版 `PlayAbility.c:190-192`：`targetsAffected == 0 (any)` ⇒ `local_178 = 1`
            //（**不看任何筛**：「Any disregards all other filters」）。
            if (ta == 0) return 1;
            // 原版 `:196-231`：210 / 220 是**累加 `traitsFilter` 的值**（`GetCurrentTraitValueWithModifiers`），
            // 不是数个数 ⇒ 本版未实现，⛔ 不拿 count 顶（那会把「每个 N 层标记」静默变成「几个目标」）。
            if (ta == 210 || ta == 220)
            {
                gap = $"`targetsAffected={ta}`（原版累加 `traitsFilter` 的值）本版未实现";
                return null;
            }
            string why = SetGap(set);
            if (why != null) { gap = why; return null; }
            int count;
            // `countOptions = 0`（AllTargets = 每个命中 +1）—— 纯 `TargetCriteria` 没有这一格，
            // 原版这一支也不按「同方 −1 / 异方 +1」加权（那是 `BoardCriteria.countOptions`）。
            if (CountSet(ctx, owner, set, cond, 0, actor, out count, out why) == null)
            {
                gap = why ?? "集合判不了";
                return null;
            }
            return count;
        }

        // ======================================================================================
        //  🆕 2026-10-24（引擎线 `A20` · 第三批 ②）：原版 `BoardAnalysis.MeetsCriteria`
        // ======================================================================================
        //  判据（第一权威 = `d:/2/tools/decomp_full/BoardAnalysis__MeetsCriteria.c`，155 行逐句读过）：
        //    `:44-46` criteria == null                    ⇒ **真**
        //    `:47-49` criteria.countCriteria == null      ⇒ **真**
        //    `:50-64` playerTurn == 10 Self  ⇒ 要求 `IsPlayerTurn() == card.isPlayer`
        //             playerTurn == 20 Enemy ⇒ 要求两者**不等**；0 Both ⇒ 不查
        //             🔴 这是**整条闸**：判假就 `return false`（⛔ 不是「判不了」）
        //    `:65-99` (第 6 实参 == 0) ∧ triggerIf == 8 RandomChance ∧ referenceValue > 0
        //             ⇒ 掷 `GetRandomInt(0,100)`，`rand < referenceValue`（**这一支不数目标**）
        //    `:100-103` countCriteria.targetsAffected == 0 (any) ⇒ **真**
        //    `:104-118` 否则 `GetTargets(countCriteria)` 数个数（countOptions 10 = 同方 −1 / 异方 +1）
        //    `:119-131` triggerIf 1 EqualTo / 2 EqualOrHigher / 3 EqualOrLower；其余值 ⇒ `LogError` 后**真**
        //
        //  🔴 与 <see cref="Meets"/> 的分工：那个回答「一条 `count` 条件行成不成立」（条件 = 行里的 `cond`），
        //     本方法回答「**这张卡的普通/替代主动能力该走哪一支**」（条件 = `countCriteria` 自己，
        //     **没有**额外的 `cond` ⇒ 数的时候按 `UnitConditions.none(0)` = 无条件，恒真）。
        //  ⚠️ `RandomChance(8)` 本版**未实现**：反编译里第 6 个实参（`param_6`）的语义读不出，
        //     而它正是那一支的闸之一 ⇒ **返回 null 并出声**（⛔ 不猜「param_6 == 0」）。
        //     现读全库**没有**卡的 criteria 用到 8（`SW9` / `SW21` 都是 1）⇒ 今天零影响。

        /// <summary>原版 `BoardAnalysis.MeetsCriteria` 的等价求值器。`null` = 本版判不了。
        /// <paramref name="gate"/> 为 `null` ⇒ **真**（原版恒真的默认 criteria，不是「判不了」）。
        /// <paramref name="owner"/> = 这张卡的拥有者（`playerTurn` 要拿它比 `ctx.Active`）。</summary>
        public static bool? MeetsBoardCriteria(BattleContext ctx, int owner, CardDef.BoardGateSpec gate,
                                               UnitState actor = null)
        {
            string gap;
            return MeetsBoardCriteria(ctx, owner, gate, actor, out gap);
        }

        /// <summary>同上，多一个 <paramref name="gap"/>（**判不了时**写清是哪一格没实现）。</summary>
        public static bool? MeetsBoardCriteria(BattleContext ctx, int owner, CardDef.BoardGateSpec gate,
                                               UnitState actor, out string gap)
        {
            gap = null;
            if (gate == null) return true;              // `:44-46` criteria == null
            var set = gate.CountSet;
            if (set == null) return true;               // `:47-49` countCriteria == null

            // ---- `playerTurn`（`:50-64`）：整条闸 ----
            int pt = set.PlayerTurn;
            if (pt == CardDef.CountSetSpec.Unknown)
            {
                gap = "`normalAbilityBoardCriteria.countSet.playerTurn` **未知（没落）**";
                return null;
            }
            if (pt != 0 && pt != 10 && pt != 20)
            {
                gap = $"playerTurn={pt} **不在 `PlayersAffected`（0/10/20）里**";
                return null;
            }
            if (pt != 0)
            {
                if (ctx == null || owner < 0) { gap = "`playerTurn` 需要 `ctx` / `owner`"; return null; }
                bool myTurn = ctx.Active == owner;
                if (pt == 10 && !myTurn) return false;
                if (pt == 20 && myTurn) return false;
            }

            // ---- `triggerIf` 本身必须是已知的（未知 ⇒ ⛔ 不猜成「不比较」）----
            if (gate.StatOption == CardDef.CountSetSpec.Unknown)
            {
                gap = "`normalAbilityBoardCriteria.triggerIf` **未知（没落）**";
                return null;
            }

            // ---- `RandomChance(8)`（`:65-99`）：本版未实现 ⇒ null（见上面文件尾那段）----
            if (gate.StatOption == 8)
            {
                // ⚠️ 原版那一支还有个 `referenceValue > 0` 的前置：不满足时**不进随机**，
                //    落到 `:118` 的 else ⇒ 数完**不比较** ⇒ **真**。这一半可以照做。
                if (gate.ReferenceValue == 0) return true;
                gap = "`triggerIf = RandomChance(8)` 本版未实现（第 6 实参 `param_6` 的语义反编译读不出）";
                return null;
            }

            if (gate.CountOption != 0 && gate.CountOption != 10)
            {
                gap = gate.CountOption == CardDef.CountSetSpec.Unknown
                    ? "`normalAbilityBoardCriteria.countOptions` **未知（没落）**"
                    : $"countOptions={gate.CountOption} 本版未实现（只实现 0 / 10）";
                return null;
            }

            // ---- `targetsAffected == 0 (any)` ⇒ 真（`:100-103`）----
            int ta = set.TargetsAffected;
            if (ta == CardDef.CountSetSpec.Unknown)
            {
                gap = "`normalAbilityBoardCriteria.countSet.targetsAffected` **未知（没落）**";
                return null;
            }
            if (ta == 0) return true;

            string why = SetGap(set);
            if (why != null) { gap = why; return null; }

            int count;
            // `cond = 0`（`UnitConditions.none`）= **无条件** —— 原版这一支数的是 `countCriteria`
            // 选出来的**全部**候选（条件就是 criteria 自己），不是「再叠一条 cond」。
            if (CountSet(ctx, owner, set, 0, gate.CountOption, actor, out count, out why) == null)
            {
                gap = why ?? "集合判不了";
                return null;
            }

            if (gate.ReferenceValue == CardDef.CountSetSpec.Unknown)
            {
                gap = "`normalAbilityBoardCriteria.referenceValue` **未知（没落）**";
                return null;
            }
            switch (gate.StatOption)
            {
                case 1: return count == gate.ReferenceValue;   // EqualTo
                case 2: return count >= gate.ReferenceValue;   // EqualOrHigher
                case 3: return count <= gate.ReferenceValue;   // EqualOrLower
                default:
                    // 原版 `:118-130`：不是 1/2/3/8 ⇒ `CustomDebug.LogError` 之后**照样 return true**
                    // （那一档不比）。⚠️ 与 `Compare`（返回 null）**不同** —— 那是 `count` 行的口径。
                    NoteUnhandledTriggerIf(gate.StatOption);
                    return true;
            }
        }

        static readonly System.Collections.Generic.HashSet<int> _unhandledTriggerIf =
            new System.Collections.Generic.HashSet<int>();

        /// <summary>原版对没实现的 `triggerIf` 会 `LogError` —— 本版照抄（同一个值只喊一次）。</summary>
        static void NoteUnhandledTriggerIf(int op)
        {
            if (!_unhandledTriggerIf.Add(op)) return;
            UnityEngine.Debug.LogError(
                $"[RuleEngine] `BoardCriteria.triggerIf={op}` **本版没有比较档** —— 原版"
              + "（`BoardAnalysis__MeetsCriteria.c:118-130`）在这一档 `CustomDebug.LogError` 之后"
              + " **照样 return true**（不比较）⇒ 本版照抄 **true**。");
        }

        /// <summary>
        /// `countSet` 里**本版一个都没实现**的那些筛维度 —— 只要它们不是「不适用」/「空」就返回一句缺口，
        /// 调用方据此返回 `null`（⛔ 不许当「不过滤」）。
        /// 取值域 → `CountSetSpec` 的 doc；判据 → `FilterMethods__CheckIfMeetsCriteria.c`（见文件头 ④）。
        /// </summary>
        static string SetGap(CardDef.CountSetSpec s)
        {
            if (s.ArmyFilter != 0) return Gap("armyFilter", s.ArmyFilter);
            if (s.KeywordFilter != 0) return Gap("keywordFilter", s.KeywordFilter);
            if (s.TraitsFilter != 0) return Gap("traitsFilter", s.TraitsFilter);
            if (s.SpellType != 0) return Gap("spellType", s.SpellType);
            if (s.AttackFilterOption != 0) return Gap("attackFilterOption", s.AttackFilterOption);
            if (s.CurrentHealthFilterOption != 0) return Gap("currentHealthFilterOption", s.CurrentHealthFilterOption);
            if (s.ManaCostFilterOption != 0) return Gap("manaCostFilterOption", s.ManaCostFilterOption);
            if (s.RarityFilterOption != 0) return Gap("rarityFilterOption", s.RarityFilterOption);
            if (s.FilterSpecificCardIds != null && s.FilterSpecificCardIds.Length > 0)
                return "countSet.filterSpecificCards 未实现";
            if (s.ExcludeEnchantmentIds != null && s.ExcludeEnchantmentIds.Length > 0)
                return "countSet.excludeEnchantments 未实现";
            // 下面这些是**布尔筛**（1 = 开）：本版没实现的都为 0 才算「不适用」。
            if (s.ExcludeSimilarCards != 0) return "countSet.excludeSimilarCards 未实现";
            if (s.ExcludeTargetted != 0) return "countSet.excludeTargetted 未实现";
            if (s.ExcludeFriendlyHero != 0) return "countSet.excludeFriendlyHero 未实现";
            if (s.ExcludeEnemyHero != 0) return "countSet.excludeEnemyHero 未实现";
            if (s.ExcludeRace != 0) return "countSet.excludeRace 未实现";
            if (s.ExcludeTrait != 0) return "countSet.excludeTrait 未实现";
            if (s.SelectFromChooseResolution != 0) return "countSet.selectFromChooseResolution 未实现";
            return null;
        }

        /// <summary>`-1` = **没落/未知**（旧表）· 其它非 0 值 = **本版没实现** —— 两种都要 `null`，
        /// 但日志里必须分得开（「没落」不是「没实现」）。</summary>
        static string Gap(string field, int value)
        {
            return value == CardDef.CountSetSpec.Unknown
                 ? $"countSet.{field} **未知（没落）**"
                 : $"countSet.{field}={value} 未实现";
        }

        /// <summary>数一数；返回 `null` = 这个集合/条件本版判不了。<paramref name="count"/> 供诊断。
        /// <para><paramref name="countOption"/> = `BoardCriteria.countOptions`：
        /// `0` = 数命中个数；`10` = **同方 −1 / 异方 +1 累加**（`BoardAnalysis__MeetsCriteria.c:104-118`）。</para></summary>
        static int? CountSet(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond, int countOption,
                             UnitState actor, out int count, out string gap)
        {
            count = 0;
            gap = null;
            int ta = set.TargetsAffected;

            // ---- ① 在场上（含「在场上或将死」那一档：候选本来就在棋盘上）----
            if (ta == 10 || ta == 55)
            {
                if (SideEmpty(set)) return 0;              // 两个阵营闸都关 ⇒ 原版一个候选都不留
                for (int p = 0; p < 2; p++)
                {
                    if (!SideMatches(owner, p, set)) continue;
                    var ps = ctx.Players[p];
                    if (ps == null) continue;
                    for (int s = 0; s < ps.Board.Length; s++)
                    {
                        var u = ps.Board[s];
                        if (!IncludeUnit(u, set, actor)) continue;
                        var v = ConditionEval.MeetsUnit(ctx, u, cond, false);
                        if (v == null) { gap = $"cond={cond} 在候选身上判不了"; return null; }
                        if (v.Value) count += Weight(owner, p, countOption);
                    }
                }
                return count;
            }

            // ---- ② 墓地（阵亡流水）----
            if (ta == 60)
            {
                if (SideEmpty(set)) return 0;
                var dead = ctx.DeadUnits;
                for (int i = 0; i < dead.Count; i++)
                {
                    var d = dead[i];
                    if (d == null || d.Card == null) continue;
                    if (!SideMatches(owner, d.Owner, set)) continue;
                    // `DeadUnits` 只收**非督军**（`CleanupDeaths` 的督军分支提前 return）
                    // ⇒ `includeMinions == 0` 时整个墓地都不数；`includeHero` 在这里没有对象
                    //   （督军死亡 = 输局，正常对局里不会进墓地）。
                    if (set.IncludeMinions == 0) continue;
                    var v = ConditionEval.MeetsCard(ctx, d.Card, d.Owner, cond, false, d.InstanceId);
                    if (v == null) { gap = $"cond={cond} 在墓地候选身上判不了"; return null; }
                    if (v.Value) count += Weight(owner, d.Owner, countOption);
                }
                return count;
            }

            // ---- ③ 手牌 / 牌库 / 手牌+场上：**本版未实现**（池内 `count` 行 0 条用到）----
            //  🔴 2026-10-23：原来这里按 50/51/52 写了一版「最小档」，其中 **52（inHandAndPlayFiltered）
            //     少数了场上那一半**（注释说「场上那半已在 ① 数过」，可 ① 与本支互斥、永远走不到）。
            //     池内 `count` 行现读 **6 条 / 4 张，全部是 10 或 60** ⇒ 与其留一个静默少数的分支，
            //     不如**如实判不了**（铁律 2/11：判据不足 ⇒ null + 出声），等真有卡用到再按
            //     `filterSpecificCards` / `includeSpells` 那一整段一起补。
            if (ta == 50 || ta == 51 || ta == 52 || ta == 65)
            {
                gap = $"targetsAffected={ta}（手牌/牌库族）本版未实现";
                return null;
            }

            gap = $"targetsAffected={ta} 本版未实现";
            return null;
        }

        /// <summary>`friendlyUnits` / `enemyUnits` **两个都是 false** ⇒ 原版一个候选都不留
        /// （`CheckIfMeetsCriteria.c:210-232`：同方看 `friendly`、异方看 `enemy`，都关就都淘汰）。
        /// <para>⚠️ **`playerTurn` 不在这里用**：它是 `BoardCriteria` 那一层的**整条闸**
        /// （见 `CountEval.Meets` 开头那一支／`BoardAnalysis__MeetsCriteria.c:39-52`），
        /// 不是「取哪一边」的筛选 —— 判假时整条条件直接不成立，连数都不数。</para></summary>
        static bool SideEmpty(CardDef.CountSetSpec set)
        {
            return set.FriendlyUnits == 0 && set.EnemyUnits == 0;
        }

        /// <summary>一个候选在「命中条件」之后**记多少分**。
        /// `countOptions == 0`（AllTargets）⇒ 每个 +1；`== 10`（EnemiesMinusAllies）⇒
        /// **同方 −1 / 异方 +1**（`BoardAnalysis__MeetsCriteria.c:104-118`：
        /// `iVar7 = 1; if (候选.isPlayer(+0x40) == 本条 criteria 的 card.isPlayer(+0x40)) iVar7 = -1;`）。</summary>
        static int Weight(int owner, int side, int countOption)
        {
            if (countOption != 10) return 1;
            return side == owner ? -1 : 1;
        }

        /// <summary>`friendlyUnits`(0x14) / `enemyUnits`(0x15) —— 相对**这条 ability 的所有者**的阵营闸
        /// （`FilterMethods__CheckIfMeetsCriteria.c:210-232`）。两个都开 = 两边都留。</summary>
        static bool SideMatches(int owner, int side, CardDef.CountSetSpec set)
        {
            int friendly = set.FriendlyUnits, enemy = set.EnemyUnits;
            if (friendly == 1 && enemy == 1) return true;
            if (friendly == 1) return side == owner;
            if (enemy == 1) return side != owner;
            return false;       // 两个都不开 ⇒ 一个候选都不留（见 `SideEmpty`）
        }

        /// <summary>`includeHero` / `includeMinions` / `includeRemnants` 三格（**卡种白名单**，不是「要不要额外加」）
        /// ＋ `includeSelf` / `excludeNotSelf` 两格（`CheckIfMeetsCriteria.c:234-261`）。
        /// <para>`-1` = 未知 ⇒ 按「不拿它筛」处理（不是 0）；0 = 明确**不要**这一档。</para>
        /// <para>⚠️ `includeSelf==0`（默认）要求**候选不能是这条 ability 的所有者自己**；
        /// 拿不到 <paramref name="actor"/> 时这一格**没法判** ⇒ 只能不筛（池内 4 张卡里
        /// 三张是战术卡、`SOR70` 自己是辆不祈祷的车 ⇒ 今天不影响读数）。</para></summary>
        static bool IncludeUnit(UnitState u, CardDef.CountSetSpec set, UnitState actor)
        {
            if (u == null) return false;
            if (u.IsRemnant && set.IncludeRemnants == 0) return false;
            bool hero = u.IsWarlord;
            if (hero && set.IncludeHero == 0) return false;
            if (!hero && set.IncludeMinions == 0) return false;
            if (actor != null)
            {
                bool self = ReferenceEquals(u, actor);
                if (self && set.IncludeSelf == 0) return false;
                if (!self && set.ExcludeNotSelf == 1) return false;
            }
            return true;
        }

        /// <summary>`StatFilterOptions` 里跟「数量」有关的三档；其余（含 `0` / `-1` / `4-11`）返回 `null`（判不了）。
        /// 口径与 `Data/AiTargets.MeetsStat` 同一张表：<paramref name="count"/> = 数出来的个数，
        /// <paramref name="filter"/> = `referenceValue`。</summary>
        static bool? Compare(int count, int op, int filter)
        {
            if (filter < 0) return null;                 // 没落（-1）⇒ 未知
            switch (op)
            {
                case 1: return count == filter;          // EqualTo
                case 2: return filter <= count;          // EqualOrHigher
                case 3: return count <= filter;          // EqualOrLower
                default: return null;
            }
        }
    }
}
