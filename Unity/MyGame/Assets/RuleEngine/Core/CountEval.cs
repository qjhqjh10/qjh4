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
        /// <para>🆕 2026-10-24（`A1` 第 1 件）：`targetsAffected = 150 (previousAbilityFiltered)`
        /// **已实现**（候选 = <see cref="BattleContext.PreviousAbilityTargets"/>，见 `CountSet` ④）。
        /// 🆕 同日（执行代理 `E8`）：**上一跳是抽牌时**候选 = <see cref="BattleContext.DrawnThisResolve"/>
        /// 里那几张牌（判据见 `CountSet` ④ 与 <see cref="CountDrawnAsPreviousTargets"/>）。</para>
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
        //     本方法回答「这条 `BoardCriteria`（`countCriteria` 自己）成不成立」。
        //     🔴🆕 **2026-10-26（`G17` 批次 6）就地订正**：原来这里写「**没有**额外的 `cond` ⇒ 按 `none(0)` 数」——
        //     **只对了一半**：`countCriteria` 这个 `TargetCriteria` 对象**自己带 `conditionsFilter`**
        //     （`+0x38`）；卡级 `normalAbilityBoardCriteria` 那一格至今没落（⇒ 传 0，与本条旧行为相同），
        //     而 **ability 级第三道闸**的 `cond` 就落在 `unitConditions` 的 `role="count"` 行 `cond` 格里
        //     （现读 99 行里 6 行非 0）⇒ 那个消费点**必须**把 `cond` 传进来（见 `<param name="cond">`）。
        //  🔴🆕 **2026-10-26（执行代理 `G17` · 批次 6 · 第三道闸）：第 6 实参 = `ignoreRandom`。**
        //     判据 = `CardAbility__CanTriggerAbility.c` 的签名
        //     （`dump.cs:20360`：`CanTriggerAbility(AbilityTrigger trigger, BattleManager manager,
        //       CardScript thisCard, CardScript actingCard, CardScript targetCard, bool ignoreRandomTrigger = false)`）
        //     与调用点 `:127-140`：`BoardAnalysis.MeetsCriteria(manager, boardCriteria, thisCard,
        //     actingCard, targetCard, ignoreRandom, 0)` ⇒ 原版那一支的闸是
        //     **`(ignoreRandom == 0) && triggerIf == 8 && referenceValue > 0`**。
        //     ⇒ `ignoreRandom == true` 时**整段跳过随机支**，落到 `:104` 的「数个数再比」
        //     （⚠️ 那一支对 `triggerIf == 8` **不比较、也不 LogError** —— 原版 `else if (local_28 != 8)`）。
        //     ⚠️ 随机**掷法本身**本版仍未实现（原版走 `BattleManager.GetRandomInt`
        //     = `MatchData` 的「开局预烧 100 + 按下标取」那张表，见 `CLAUDE.md` 那条红线）⇒
        //     `ignoreRandom == false` 且真命中这一档时 **返回 null 并出声**（⛔ 不猜、不拿 `ctx.Rng.Next` 顶）。
        //     现读全库**没有**卡的 criteria 用到 8（`normalAbilityBoardCriteria` 2 张都是 1；
        //     v30 的 `role="count"` 行 99 条 `op ∈ {0,1,2,3}` · 见 `cards_engine.json`）⇒ 今天零影响。

        /// <summary>原版 `BoardAnalysis.MeetsCriteria` 的等价求值器。`null` = 本版判不了。
        /// <paramref name="gate"/> 为 `null` ⇒ **真**（原版恒真的默认 criteria，不是「判不了」）。
        /// <paramref name="owner"/> = 这张卡的拥有者（`playerTurn` 要拿它比 `ctx.Active`）。
        /// <para>⚠️ 本重载 = `ignoreRandom: false`（原版触发路径
        /// `RawCardScript.TriggerAbilities` 的 `param_10` 在全部已知调用点上都是 0）。</para></summary>
        public static bool? MeetsBoardCriteria(BattleContext ctx, int owner, CardDef.BoardGateSpec gate,
                                               UnitState actor = null)
        {
            string gap;
            return MeetsBoardCriteria(ctx, owner, gate, actor, out gap);
        }

        /// <summary>同上，多一个 <paramref name="gap"/>（**判不了时**写清是哪一格没实现）。</summary>
        /// <param name="ignoreRandom">原版第 6 实参（`CanTriggerAbility` 的 `ignoreRandomTrigger`）。
        /// `true` ⇒ **跳过 `RandomChance(8)` 支**，落到「数个数再比」（原版 `:104-131` 那一半）。
        /// `false`（缺省）⇒ 命中 `triggerIf == 8 && referenceValue > 0` 时进随机支 ——
        /// 本版那一支的**掷法**未实现 ⇒ 返回 `null` + `gap`（⛔ 不是「直接 true」）。</param>
        /// <param name="cond">🆕 2026-10-26（执行代理 `G17` · 批次 6）：**`countCriteria.conditionsFilter`**
        /// （原版 `TargetCriteria +0x38`）—— 数候选时**逐个过的那条条件**。
        /// 缺省 `0`（`UnitConditions.none`）= 无条件（卡级 `normalAbilityBoardCriteria` 那条老调用点就是这一档：
        /// 它的 `conditionsFilter` 至今没落进 `BoardGateSpec`，见 `报告/G17_第6轮.md` §四）。
        /// 🔴 **ability 级第三道闸必须传**：数据在 `unitConditions` 的 `role="count"` 行 `cond` 格里
        /// （现读 99 行里 **6 行** `cond != 0`：`221`×2 / `331`×2 / `222` / `243`）。
        /// ⛔ 不传 = 把「只数带某条件的候选」静默变成「全数」。</param>
        public static bool? MeetsBoardCriteria(BattleContext ctx, int owner, CardDef.BoardGateSpec gate,
                                               UnitState actor, out string gap, bool ignoreRandom = false,
                                               int cond = 0)
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

            // ---- `RandomChance(8)`（`:65-99`）----
            //  原版的闸是 `(ignoreRandom == 0) && triggerIf == 8 && referenceValue > 0`
            //  ⇒ `ignoreRandom == true` 时**根本不进这一支**（见方法 doc 与文件尾那段）。
            if (gate.StatOption == 8)
            {
                // ⚠️ 原版那一支还有个 `referenceValue > 0` 的前置：不满足时**不进随机**，
                //    落到 `:104` 的 else ⇒ 数完**不比较** ⇒ **真**。这一半可以照做。
                if (gate.ReferenceValue == 0) return true;
                if (!ignoreRandom)
                {
                    // 本版**掷法未实现**（原版走 `BattleManager.GetRandomInt` = `MatchData` 的
                    // 「开局预烧 100 + 按下标取」那张表）⇒ 判不了 + 出声，⛔ 不拿 `ctx.Rng.Next` 顶。
                    gap = "`triggerIf = RandomChance(8)` 的**掷法**本版未实现"
                        + "（原版 `BattleManager.GetRandomInt` = `MatchData` 预烧 100 + 下标）"
                        + " ⇒ 判不了；传 `ignoreRandom: true` 可跳过这一支（原版第 6 实参）";
                    return null;
                }
                // `ignoreRandom == true` ⇒ 原版整段跳过随机支，落下面「数个数」那一半。
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
            // `cond` = `countCriteria.conditionsFilter`（原版同一个 `TargetCriteria` 对象里的那一格）：
            //   卡级老调用点不传 ⇒ `0`（无条件，行为与改前逐字相同）；
            //   ability 级第三道闸传 `role="count"` 行的 `cond`（见方法 doc 的 `<param name="cond">`）。
            // ⚠️ 改前这里**恒 0**（注释还写着「条件就是 criteria 自己」）—— 那只对「行里 `cond` 恰好是 0」
            //   的 93 条成立；另 6 条（`221`/`331`/`222`/`243`）本来是**带条件地数**。
            if (CountSet(ctx, owner, set, cond, gate.CountOption, actor, out count, out why) == null)
            {
                gap = why ?? "集合判不了";
                return null;
            }

            // 原版 `:100-131` 的 else 支：`triggerIf == 8` 那一档**只数不比**（`else if (local_28 != 8)`
            // ⇒ 既不比较、也不 `LogError`）⇒ 走到这里时 `referenceValue` 对 8 档没有意义。
            if (gate.StatOption != 8 && gate.ReferenceValue == CardDef.CountSetSpec.Unknown)
            {
                gap = "`normalAbilityBoardCriteria.referenceValue` **未知（没落）**";
                return null;
            }
            switch (gate.StatOption)
            {
                case 1: return count == gate.ReferenceValue;   // EqualTo
                case 2: return count >= gate.ReferenceValue;   // EqualOrHigher
                case 3: return count <= gate.ReferenceValue;   // EqualOrLower
                case 8:
                    // `ignoreRandom == true` 落到这里（原版 `:104-131` 的 else 支；`local_28 != 8`
                    // 那一跳 ⇒ **不比较、不 LogError**）⇒ 数完**真**。
                    // ⚠️ `ignoreRandom == false` 根本走不到这里（上面那一支已 return）。
                    return true;
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

            // ---- ④ 上一条能力的目标集合（`150 previousAbilityFiltered`）----
            //  判据（2026-10-24 · 引擎线 `A1` 第 1 件，逐行现读）：
            //    · `AbilityLogic__GetTargets.c:867-882`：`ta == 0x96(150)` ⇒ 把静态表
            //      （`AbilityLogic.pooledPossibleTargets`）**AddRange 成候选池**（与 `140` 同一支，
            //      只多一句 `CustomDebug.Log`）；
            //    · `FilterMethods__CheckIfMeetsCriteria.c:172-183`：候选还要**在表里**（`Contains`），
            //      然后照常走 `:184-420` 的阵营 / 卡种 / 自他 / 条件 / 兵种筛；
            //    · 表的写点 = `AbilityLogic__SetPreviousAbilityTargets.c:19-28`（整表覆盖），
            //      调用点 = `AbilityLogic__PlayAbility.c:2743-2745`（该能力末尾）⇒ 我们落在
            //      `BattleContext.PreviousAbilityTargets`。
            //      🔴 **2026-10-24 订正（铁律 5 · 执行代理 E2）**：这里原来写「写点 = `ResolveOps` 出口」——
            //      **已经不成立**：现在**每条 op 之后各同步一次**（`EffectResolver.cs:489/546/595/6514`，
            //      出口再幂等一次 `:603/:6528`），发布视图 = **本条 ability 至今的并集**
            //      （原版每个效果出口都整表覆盖、且 `AbilityLogic__DeepCopy.c` 不拷贝 `+0x80` ⇒
            //      每条 ability 从空开始）。详见 `资料/普查产出_1024b/E2_结算残值与EvtKind.md`。
            //  ⚠️ **已知差别**（如实标，⛔ 不拿猜的顶）：原版候选可以是**手牌/牌库的卡**
            //     （`SW51` 的上一跳是抽牌）—— **抽牌那一跳**已建模（见下面 `DrawnThisResolve` 分支，
            //     2026-10-24 执行代理 `E8`，修 `G6` 5 条 `mult` 行把正文的 2 顶成 0 的回归）；
            //     其余手牌/牌库族（选牌 / 翻找）仍未建模 ⇒ 那种候选落到这里会 `null` + 出声。
            if (ta == 150)
            {
                if (SideEmpty(set)) return 0;
                // 🔴 **上一跳是抽牌 ⇒ 候选 = 抽到的牌**（手牌里的卡，不是场上单位）。
                //  判据（`d:/2/tools/decomp_full/`，逐句现读）：
                //    · `AbilityLogic__AddDrawCard.c`：抽牌那一支把
                //      `BattleManager__GetNextCardsInDeck(...)` 的结果 **`AddRange` 进
                //      `abilityTargetsToSave`（`param_1 + 0x80`）**（`System_Collections_Generic_
                //      List<object>__AddRange(lVar2, uVar3, …)`，`lVar2 = *param_7`、`param_7 = param_1+0x80`）；
                //    · 该 effect 的出口 `AbilityLogic__SetPreviousAbilityTargets.c:19-28`
                //      （`Array.Clear` + `AddRange`，**整表覆盖**）把它写进静态表；
                //    · `AbilityLogic__GetTargets.c` 的 `iVar6 == 0x96 (150)` 支再把静态表
                //      `AddRange` 成候选池 ⇒ **候选就是抽到的那几张牌**。
                //  ⇒ 本版 `PreviousAbilityTargets` 是 `List<UnitState>`（只装场上单位）⇒ 抽牌那一跳
                //     **结构上装不下**；`ctx.DrawnThisResolve`（「一张卡一次结算」窗口，入口
                //     `ResolveOps` 清、抽牌写）就是这一跳的候选来源。
                //  🔴 **必须先判抽牌、再判场上表** —— ⚠️ **2026-10-24 订正（铁律 5 · E2）**：这里原来的理由是
                //     「`PreviousAbilityTargets` 只在 `ResolveOps` 出口同步 ⇒ 结算中读到的是上一张卡的残值」，
                //     **那个理由已经不成立**（E2 已改成逐 op 同步 + 并集）。**结论仍然成立，但依据换了**：
                //     我们的表是 `List<UnitState>`，**结构上装不下抽到的牌**（原版 `abilityTargetsToSave` 能装
                //     手牌/牌库的卡）⇒ 抽牌那一跳只能由 `DrawnThisResolve` 单独建模，**必须先看它**。
                //     池内 `ta = 150` 的消费行今天只有 5 张「先抽后数」的战术卡。
                //  ⚠️ **仍是近似（如实标）**：DrawnThisResolve 的窗口是「一张卡」而不是「一跳」——
                //     若同一张卡里抽牌之后又打了别的目标、再用 `150`，原版取的是**最后那一跳**
                //     （整表覆盖），这里会取抽到的牌。池内 0 张这种卡。
                if (ctx.DrawnThisResolve.Count > 0)
                {
                    // ⚠️ 必须**写回 `count`** 再 `return count`（`count` 是 `out` 形参）——
                    //    直接 `return CountDrawnAsPreviousTargets(...)` 会让调用方拿到 0，
                    //    而返回值非 null ⇒ 又不报 gap（那正是本件那条回归的形状）。
                    var drawn = CountDrawnAsPreviousTargets(ctx, owner, set, cond, countOption, ref gap);
                    if (drawn == null) return null;
                    count = drawn.Value;
                    return count;
                }
                var prev = ctx.PreviousAbilityTargets;
                for (int i = 0; i < prev.Count; i++)
                {
                    var u = prev[i];
                    if (u == null) continue;
                    int side = SideOfUnit(ctx, u);
                    if (side < 0)
                    {
                        gap = "`targetsAffected=150 previousAbilityFiltered` 的候选里有**不在场上**的卡"
                            + "（抽到的牌 / 已离场）—— 原版按 `CardScript` 筛，本版只建模场上单位";
                        return null;
                    }
                    if (!SideMatches(owner, side, set)) continue;
                    if (!IncludeUnit(u, set, actor)) continue;
                    var v = ConditionEval.MeetsUnit(ctx, u, cond, false);
                    if (v == null) { gap = $"cond={cond} 在上一条能力的目标身上判不了"; return null; }
                    if (v.Value) count += Weight(owner, side, countOption);
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

        /// <summary>🆕 2026-10-24（引擎线 `A1`）：某个单位在哪一方棋盘上（`-1` = 不在任何一方的棋盘上）。
        /// <para>为什么需要它：`PreviousAbilityTargets` 是**一串 `UnitState`**，没有「属于哪方」这一列
        /// （`UnitState` 没有 `Owner` 字段）⇒ `targetsAffected=150` 的阵营筛只能回棋盘上查。
        /// 查不到 = 那张候选已经离场 / 本来就不在场上（抽到的牌）—— 由调用方**如实报判不了**。</para></summary>
        static int SideOfUnit(BattleContext ctx, UnitState u)
        {
            for (int p = 0; p < 2; p++)
            {
                var b = ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++)
                    if (object.ReferenceEquals(b[s], u)) return p;
            }
            return -1;
        }

        /// <summary>🆕 2026-10-24（执行代理 `E8`）：`targetsAffected = 150 (previousAbilityFiltered)`
        /// 在「**上一跳是抽牌**」时的候选 = <see cref="BattleContext.DrawnThisResolve"/> 里那几张牌。
        ///
        /// <para>**判据**（为什么是抽到的牌，而不是场上单位）→ `CountSet` ④ 那一大段注释：
        /// `AbilityLogic__AddDrawCard.c` 把 `GetNextCardsInDeck` 的结果 `AddRange` 进
        /// `abilityTargetsToSave(param_1+0x80)`，随后 `SetPreviousAbilityTargets` 整表覆盖，
        /// `GetTargets` 的 `150` 支把它当候选池。</para>
        ///
        /// <para>**筛**：阵营（按「这份牌在谁手里」判，<see cref="SideOfCardInstance"/>）＋ 卡种白名单
        /// （<see cref="DrawnKindPasses"/>：兵种 / 督军 / 法术三档）＋ 条件行（`cond`，走
        /// `ConditionEval.MeetsCard` —— 主语是一张**不在场上的卡**，与该文件里墓地那一支同口径）。
        /// `includeSelf` / `excludeNotSelf`：候选是牌、`actor` 是单位 ⇒ 两者**永不是同一件东西**；
        /// `excludeNotSelf != 0`（「只要这张卡自己」）⇒ 抽到的牌一个都不合格（0），
        /// `includeSelf` 不限制别的候选（与原版 `:233-244` 的两句同构）。</para>
        ///
        /// <para>`null` = 判不了（写 `gap`）—— ⛔ 不拿「都算」或 0 顶。</para>
        /// </summary>
        static int? CountDrawnAsPreviousTargets(BattleContext ctx, int owner, CardDef.CountSetSpec set,
                                                int cond, int countOption, ref string gap)
        {
            if (ctx == null) { gap = "没有对局上下文"; return null; }
            int count = 0;
            for (int i = 0; i < ctx.DrawnThisResolve.Count; i++)
            {
                var inst = ctx.DrawnThisResolve[i];
                if (inst == null || inst.Card == null) continue;
                int side = SideOfCardInstance(ctx, inst);
                if (side < 0)
                {
                    gap = "`targetsAffected=150 previousAbilityFiltered` 的候选里有**不在任一方手牌里**的牌"
                        + "（抽到之后又被移走 / 弃掉）—— 阵营判不了";
                    return null;
                }
                if (!SideMatches(owner, side, set)) continue;
                bool? kind = DrawnKindPasses(inst.Card, set);
                if (kind == null)
                {
                    gap = "`targetsAffected=150` 的卡种筛（`includeWhispers`/`includeWeapons`/"
                        + "`includeHeroParts` 或 `type=" + (inst.Card.Type ?? "?") + "`）在**不在场上的牌**上本版判不了";
                    return null;
                }
                if (!kind.Value) continue;
                if (set.ExcludeNotSelf != 0) continue;   // 「只要这张卡自己」⇒ 抽到的牌永远不合格
                var v = ConditionEval.MeetsCard(ctx, inst.Card, side, cond, false, inst.Id);
                if (v == null) { gap = $"cond={cond} 在抽到的牌身上判不了"; return null; }
                if (v.Value) count += Weight(owner, side, countOption);
            }
            return count;
        }

        /// <summary>这份牌在**哪一方的手牌里**（`-1` = 都不在）。语义与 <see cref="SideOfUnit"/> 同理，
        /// 只是查的是 `PlayerState.Hand`（`CardInstance` 没有 `Owner` 字段）。</summary>
        static int SideOfCardInstance(BattleContext ctx, CardInstance inst)
        {
            for (int p = 0; p < 2; p++)
            {
                var hand = ctx.Players[p].Hand;
                for (int i = 0; i < hand.Count; i++)
                    if (object.ReferenceEquals(hand[i], inst)) return p;
            }
            return -1;
        }

        /// <summary>🆕 2026-10-24（执行代理 `E8`）：卡种白名单（`includeHero` / `includeMinions` /
        /// `includeSpells`）在**一张不在场上的牌**上的口径 —— 只给 <see cref="CountDrawnAsPreviousTargets"/> 用。
        ///
        /// <para>取值口径与 <see cref="IncludeUnit"/> 一致：**`!= 0` 才筛**（`Unknown(-1)` = 未知 ⇒ 不筛，
        /// 与原版「没落 ≠ 明确不要」同一条纪律）。</para>
        /// <para>`null` = 本版分不了这一档：`includeWhispers` / `includeWeapons` / `includeHeroParts`
        /// 非 0（原版另有 `cardType` 取值，本版没建模），或卡种不在这三档里（`defence` 等）。
        /// ⛔ 不静默当「都算」。</para>
        /// </summary>
        static bool? DrawnKindPasses(CardDef c, CardDef.CountSetSpec set)
        {
            if (c == null) return false;
            if (set.IncludeWhispers > 0) return null;
            if (set.IncludeWeapons > 0) return null;
            if (set.IncludeHeroParts > 0) return null;
            if (c.Type == "hero") return set.IncludeHero != 0;        // 督军
            if (c.Type == "unit") return set.IncludeMinions != 0;     // 部队
            if (c.Type == "tactic") return set.IncludeSpells != 0;    // 法术
            return null;   // `defence` / 空 type ⇒ 本版没建模（原版 `cardType` 另有取值）
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
