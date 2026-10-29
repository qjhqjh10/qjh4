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
        public static bool? Meets(BattleContext ctx, int owner, CardDef.ConditionSpec spec, UnitState actor = null, UnitState target = null, bool targetKnown = false)
        {
            string gap;
            return Meets(ctx, owner, spec, actor, out gap, target, targetKnown);
        }

        /// <summary>同上，多一个 <paramref name="gap"/>（**判不了时**写清是哪一格没实现）。</summary>
        public static bool? Meets(BattleContext ctx, int owner, CardDef.ConditionSpec spec, UnitState actor,
                                  out string gap, UnitState target = null, bool targetKnown = false)
        {
            gap = null;
            // 🔴🆕 **2026-10-26（批次 9 · 执行代理 `E7P06` · 账 676 ⑤）就地拆开这两种「没有」**：
            //   改前 `ctx == null` 与「没有 countSet」共用一句 `gap` ⇒ 前者会被误读成「这条条件没有
            //   被数集合」（两件事的修法完全不同）。⛔ 不是「不静默」就够了 —— **文案也要指对**。
            if (ctx == null) { gap = "没有对局上下文（`ctx == null`）—— 判不了"; return null; }
            if (spec == null || spec.CountSet == null) { gap = "没有 countSet"; return null; }
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
                // 🔴🆕 **2026-10-26（批次 9 · `E7P06` · 账 676 ⑤）：缺 `ctx` / `owner` 时【出声放行】。**
                //   判据（原版 `BoardAnalysis__MeetsCriteria.c:40-41`）：`playerTurn == 10(Self)` 那一支
                //   写的是 `if ((manager == null) || (IsPlayerTurn(manager) …) || (thisCard == null))
                //   goto LAB_…77b2`，而 `LAB_1806077b2` = **NRE 桩**（`FUN_1803f47a0`，不返回）
                //   ⇒ 原版在「拿不到 manager / thisCard」时是**崩溃**。
                //   ⛔ **我们不复制崩溃语义**（口径同 `EnqueueAbility` 那几道闸：判不了 ⇒ 出声 + 放行），
                //   但**也不许静默**：改前 `owner < 0` 会走到 `isMyTurn = ctx.Active == owner` ⇒ 恒假
                //   ⇒ `pt == 10` 时**静默返回 false**（= 把「不知道」当成「不是自己回合」，那是错的）。
                if (ctx == null || owner < 0)
                {
                    gap = "`playerTurn` 这一格要 `ctx` / `owner` 才能判（`IsPlayerTurn() == owner`），"
                        + "本路径拿不到（`ctx == null` 或 `owner < 0`）—— 原版 `BoardAnalysis__MeetsCriteria.c:40-41` "
                        + "在这一格直接 NRE，**我们不复制崩溃语义** ⇒ 判不了（调用点出声 + 放行）";
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
            string why = SetGap(set, false);   // `mult`/`avail` 这两支**没有**计数池那两跳（它们不收集候选）⇒ 保留旧口径（见 `SetGap` 的 `boardPoolWired`）
            if (why != null) { gap = why; return null; }

            int count;
            if (CountSet(ctx, owner, set, spec.Condition, spec.CountOption, actor, out count, out why, null, null, target, targetKnown) == null)
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
                                        UnitState actor, out string gap, UnitState target = null, bool targetKnown = false)
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
            string why = SetGap(set, false);   // 同 `Meets`：本支不收集候选 ⇒ 没有计数池那两跳，保留旧口径
            if (why != null) { gap = why; return null; }
            int count;
            // `countOptions = 0`（AllTargets = 每个命中 +1）—— 纯 `TargetCriteria` 没有这一格，
            // 原版这一支也不按「同方 −1 / 异方 +1」加权（那是 `BoardCriteria.countOptions`）。
            if (CountSet(ctx, owner, set, cond, 0, actor, out count, out why, null, null, target, targetKnown) == null)
            {
                gap = why ?? "集合判不了";
                return null;
            }
            // 🔴🆕 **2026-10-26（批次 9 · `E7P06` · 账 676 ④）**：同一个 `maxTargets` 截断也适用
            //   这一支 —— 它是 `AbilityLogic__PlayAbility.c:185/:262` 的 `GetTargets(…, 1, 0)`
            //   （第 4 实参 = `checkMaxTargets`，与原版 `MeetsCriteria.c:101` 那一跳同一个值）
            //   ⇒ 截短之后 `local_178` 才是 `furtherMultiplier` 的乘数。`countOption` 恒 0 ⇒ 只要个数。
            int? trimmed = ApplyMaxTargets(set, count, null, null, owner, 0, out why);
            if (trimmed == null) { gap = why ?? "`maxTargets` 截断判不了"; return null; }
            return trimmed.Value;
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
        //     ✅ **2026-10-27（`E1RNG`）已实现掷法**：`ctx.GetRandomInt(0,100) < referenceValue`（见下方那一支）；
        //     本条原来写「掷法未实现 ⇒ 返回 null 并出声」——随机表落地后不再成立；
        //     ⛔ 也别再写「不拿 `ctx.Rng.Next` 顶」那种话：现在**有**正主可用了（`ctx.GetRandomInt`）。
        //     现读全库**没有**卡的 criteria 用到 8（`normalAbilityBoardCriteria` 2 张都是 1；
        //     v30 的 `role="count"` 行 99 条 `op ∈ {0,1,2,3}` · 见 `cards_engine.json`）⇒ 今天零影响。

        /// <summary>原版 `BoardAnalysis.MeetsCriteria` 的等价求值器。`null` = 本版判不了。
        /// <paramref name="gate"/> 为 `null` ⇒ **真**（原版恒真的默认 criteria，不是「判不了」）。
        /// <paramref name="owner"/> = 这张卡的拥有者（`playerTurn` 要拿它比 `ctx.Active`）。
        /// <para>⚠️ 本重载 = `ignoreRandom: false`（原版触发路径
        /// `RawCardScript.TriggerAbilities` 的 `param_10` 在全部已知调用点上都是 0）。</para></summary>
        public static bool? MeetsBoardCriteria(BattleContext ctx, int owner, CardDef.BoardGateSpec gate,
                                               UnitState actor = null, UnitState target = null, bool targetKnown = false)
        {
            string gap;
            return MeetsBoardCriteria(ctx, owner, gate, actor, out gap, false, 0, target, targetKnown);
        }

        /// <summary>同上，多一个 <paramref name="gap"/>（**判不了时**写清是哪一格没实现）。</summary>
        /// <param name="ignoreRandom">原版第 6 实参（`CanTriggerAbility` 的 `ignoreRandomTrigger`）。
        /// `true` ⇒ **跳过 `RandomChance(8)` 支**，落到「数个数再比」（原版 `:104-131` 那一半）。
        /// `false`（缺省）⇒ 命中 `triggerIf == 8 && referenceValue > 0` 时进随机支 ——
        /// 本版那一支的**掷法**未实现 ⇒ 返回 `null` + `gap`（⛔ 不是「直接 true」）。</param>
        /// <param name="cond">🆕 2026-10-26（执行代理 `G17` · 批次 6）：**`countCriteria.conditionsFilter`**
        /// （原版 `TargetCriteria +0x38`）—— 数候选时**逐个过的那条条件**。
        /// 缺省 `0`（`UnitConditions.none`）= 无条件（卡级 `normalAbilityBoardCriteria` 那条老调用点就是这一档：
        /// 它的 `conditionsFilter` 至今没落进 `BoardGateSpec`）。
        /// 🔴🆕 **2026-10-26（批次 9 · 执行代理 `E7P06`）把那一格的交接写全** —— 卡级要**四处一起动**
        /// （全在本件白名单外）：① `Core/CardDef.cs:267` 的 `BoardGateSpec` 加 `ConditionsFilter` 一格；
        /// ② `Core/CardDef.cs:300`（`UsesAlternativeAbility`）把它当 `cond:` 传进本方法；
        /// ③ `Data/CardDatabase.cs:2303`（`MapBoardGate`）＋ `:604`（`AbilityGateDto`）映射
        /// `normalAbilityBoardCriteria.countCriteria.conditionsFilter`；
        /// ④ `工具/gen_cards_engine.py:2467`（`board_gate_of`）从 `countCriteria.conditionsFilter` 取值。
        /// ⚠️ 现读池内 2 张卡（`SW9` / `SW21`）与池外 1 张（`Fenrisian Wolfpack`）该值**全是 0**
        /// （`cards_raw.json` 的 `full_raw` 逐张读）⇒ 落上也**零行为变化**，但 ⛔ 不能因此不做。
        /// 🔴 **ability 级第三道闸必须传**：数据在 `unitConditions` 的 `role="count"` 行 `cond` 格里
        /// （现读 99 行里 **6 行** `cond != 0`：`221`×2 / `331`×2 / `222` / `243`）。
        /// ⛔ 不传 = 把「只数带某条件的候选」静默变成「全数」。</param>
        public static bool? MeetsBoardCriteria(BattleContext ctx, int owner, CardDef.BoardGateSpec gate,
                                               UnitState actor, out string gap, bool ignoreRandom = false,
                                               int cond = 0, UnitState target = null, bool targetKnown = false)
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
                    // 🔴 2026-10-27（`E1RNG`）：原版这一步 = 预烧表取一格 —— `BoardAnalysis__MeetsCriteria.c:55`
                    //   `rand = GetRandomInt(0,100)`（上界开区间 ⇒ `[0,100)`），`:89` `return rand < referenceValue`。
                    //   `referenceValue` = `criteria + 0x28`（`gate.ReferenceValue`），方向是**严格小于**。
                    //   ⚠️ 原版还打一条 `CustomDebug.Log`（带卡名/阈值/掷值）—— 我们不接调试日志。
                    if (ctx == null) { gap = "`RandomChance(8)` 要 `ctx` 才能掷（预烧表挂在 ctx 上）"; return null; }
                    return ctx.GetRandomInt(0, 100) < gate.ReferenceValue;
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

            string why = SetGap(set, true);   // 只有这一支（账 676 的计数路径）接上了「去重 → 收窄」两跳
            if (why != null) { gap = why; return null; }

            int count;
            // `cond` = `countCriteria.conditionsFilter`（原版同一个 `TargetCriteria` 对象里的那一格）：
            //   卡级老调用点不传 ⇒ `0`（无条件，行为与改前逐字相同）；
            //   ability 级第三道闸传 `role="count"` 行的 `cond`（见方法 doc 的 `<param name="cond">`）。
            // ⚠️ 改前这里**恒 0**（注释还写着「条件就是 criteria 自己」）—— 那只对「行里 `cond` 恰好是 0」
            //   的 93 条成立；另 6 条（`221`/`331`/`222`/`243`）本来是**带条件地数**。
            // 🔴🆕 **2026-10-26（批次 9 · 执行代理 `E7P06` · 账 676 ④）：`maxTargets` 截断 = 原版
            //   `AbilityLogic.GetTargets` 的【尾段】**（判据见下面 `ApplyMaxTargets` 的 doc）。
            //   ⚠️ `countOptions == 10`（同方 −1 / 异方 +1）时**截断发生在加权之前**（原版先
            //   `GetTargets` 把列表截短、`BoardAnalysis__MeetsCriteria.c:104-118` 才按截短的列表累加）
            //   ⇒ 那一档必须拿到**候选本身**才排得出顺序，所以要按需收集。
            bool needCands = (set.MaxTargets >= 1 && gate.CountOption == 10) || set.SelectFromChooseResolution > 0 || set.TraitsFilter == 780;   // 🆕 2026-10-29（B98_G6 · 账 676 的「计数路径」格）：`selectFromChooseResolution`（候选池**来源** = 收窄）与 `traitsFilter == 780`（单次变异去重）都作用在**候选本身**上 ⇒ 必须真有候选才能做（判据 `AbilityLogic__GetTargets.c:1101-1140` / `:155-172` + `:1370-1385`）。⚠️ 只收 `> 0`：`Unknown(-1)` 是「没落」，`SetGap` 会先把它判成 gap，⛔ 不拿它当「有此位」
            var cands = needCands ? new System.Collections.Generic.List<UnitState>() : null;
            var candSides = needCands ? new System.Collections.Generic.List<int>() : null;
            if (CountSet(ctx, owner, set, cond, gate.CountOption, actor, out count, out why,
                         cands, candSides, target, targetKnown) == null)
            {
                gap = why ?? "集合判不了";
                return null;
            }
            if (cands != null) cands = CountEval.ApplySelectFromChooseResolution(ctx, set.SelectFromChooseResolution, RuleCore.FilterSingleUseMutationTargets(ctx, cands, actor != null ? actor.Instance : null, set.TraitsFilter, "计数池"), "计数池");   // 🆕 2026-10-29（B98_G6 · 账 676 的「计数路径」格）：原版 `MeetsCriteria` 数候选走的就是 `AbilityLogic.GetTargets(…)` ⇒ 候选池**同样**要过末尾两段，**顺序照原版**：`traitsFilter == 780` 去重（内层，`.c:155-172`）→ `selectFromChooseResolution` 收窄（最外层，`.c:1101-1140`）→ 下面那一步 `maxTargets` 截断（`.c:1164`）。⛔ 别把顺序倒过来（去重要在收窄之前：收窄后只剩 1 张，先去重会把它剔掉 = 静默多数一张）
            int? trimmed = ApplyMaxTargets(set, count, cands, candSides, owner, gate.CountOption, out why);
            if (trimmed == null) { gap = why ?? "`maxTargets` 截断判不了"; return null; }
            count = trimmed.Value;

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
        ///
        /// <para>🆕 **2026-10-26（第八十三会话 · 执行代理 `B1`）：从这里【放开】六格** ——
        /// `keywordFilter`（池内 12 行）/ `traitsFilter`（2 行）/ `currentHealthFilterOption`（2 行）/
        /// `filterSpecificCards`（1 行）/ `excludeRace` / `excludeTrait`。
        /// 它们由 <see cref="TargetCriteriaEval.Matches"/> 求值（**全仓唯一那份 36 格判据**），
        /// 本文件不再自带一份 —— 棋盘跳与 `ta=150` 两支直接转调它（见 `BoardCandidatePasses`）。
        /// ⚠️ 放开之后**墓地那支（`ta=60`）要自己再挡一次**（候选不是 `UnitState`，
        /// 运行期那几维读不到）⇒ 见 `CemeteryCandidatePasses`。</para>
        ///
        /// <para>⛔ 仍挡在这里的：`armyFilter` / `spellType` / `attackFilterOption` /
        /// `manaCostFilterOption` / `rarityFilterOption` / `excludeEnchantments` / 四个布尔排除 /
        /// `selectFromChooseResolution`。前六格 `TargetCriteriaEval` 其实**已经有实现**，
        /// 本笔只按「一次放一类、池内现读有行才放」的保守口径处理（这六格池内现读均 **0 行**）。</para>
        ///
        /// <para>🔴🆕 **2026-10-26（批次 9 · 执行代理 `E7P06`）就地订正 —— `selectFromChooseResolution`
        /// 那句「全量反编译里没有任何消费点」是【错的】**（铁律 5；写在这里免得下个会话照旧话放弃）：
        /// <list type="bullet">
        /// <item>**消费点至少三处**（判据 = `cpp2il/out_cpp2il_dev_ilspy/AbilityLogic.cs:1075` /
        /// `:1414`（`GetCardToSummon`）/ `:1628`（`GetCardScriptToSummon`））；
        /// 字段声明 = 同目录 `TargetCriteria.cs:82` / `il2cpp_out/dump.cs` 的 `TargetCriteria`（`+0x80`）。</item>
        /// <item>**语义（现读能确定的）**：该位为真 ⇒ **候选池不是「按 criteria 从棋盘/牌库筛出来的」**，
        /// 而是**本次「选牌结算」里选中的那一张** —— `AbilityLogic.cs:1077` 那段拿
        /// `manager + 744`（= `BattleManager.cardChosenInSelection`，`dump.cs:30844` `// 0x2E8`）
        /// 去**淘汰 uniqueId 不等于它的候选**；`:1414` / `:1628` 则是「为真 ⇒ **跳过**正常的
        /// `GetTargets` / 牌库扫描那一支」。</item>
        /// <item>**还差什么（所以本版仍判不了，⛔ 不猜）**：① 我们**没有** `cardChosenInSelection`
        /// 的等价物（`BattleContext` 上没有「本次选牌结算选中了哪张」的槽）——要先查它的**写点**
        /// （谁在选牌结算里写它）才能建模；② ILSpy 那三处是**同一个巨函数**（`GetTargets` 1427 行
        /// 的反编译），`else` 两支的具体行为被反编译糊掉了（`NoteDecompilerIssue` 满屏），
        /// 要按 `.c` 逐句核才敢落地；③ 池内**唯一一行**用到它 = `ASH82`（Farseer Skyrunner，
        /// `role="count"`）——而那一行 `targetsAffected = 50`（**手牌族**），本文件的
        /// `CountSet` ③ 本来就判不了（`ta ∈ {50,51,52,65}` ⇒ `null` + 出声）⇒ **就算把这一格做掉，
        /// 那张卡也仍然判不了**。⇒ 顺序应当是「先做手牌族（`ta=50`）再回来看这一格」。</item>
        /// </list></para>
        /// </summary>
        static string SetGap(CardDef.CountSetSpec s, bool boardPoolWired = false)
        {
            // 🆕 2026-10-26（`W6K11` · 账 `收藏:188`）：原来这 9 格在这里整条挡成 `null` ——
            //   它们**早在 `TargetCriteriaEval.Matches` 里实现了**（全仓唯一那份 36 格判据）⇒ 撤掉。
            //   现池受影响：`currentHealthFilterOption ∈ {2,3}` 2 行（`AM54` At All Costs / `GOF105`
            //   Monster Hunters，ta 都是 10 ⇒ 棋盘路，`Matches` 判得了）；其余 8 格现读 **0 行**。
            // ⚠️ 判不了的格仍然 `null`（`Matches` / `CemeteryCandidatePasses` 自己出 `gap`）——
            //   降级路没变，只是从「整行不判」变成「逐格判到判不了为止」。
            // ⚠️ `excludeTargetted`（`TargetCriteria +0x20`）：ILSpy 全树**只有声明、0 个读者**
            //   （`CheckIfMeetsCriteria` / `CheckIfRawCardMeetsCriteria` 都不读它）⇒ 它不参与判定，
            //   挡着反而会把整行误判成「判不了」。
            // 🔴 `selectFromChooseResolution`（`+0x80`）= **候选池来源**的开关（不是筛）：只有**接了两跳的那一条路**
            //   （`MeetsBoardCriteria`，`boardPoolWired = true`）能放行棋盘族；`mult`/`avail` 两支不收集候选 ⇒ 照旧 gap。
            //   ⚠️ 池内该旗非 0 的 `role="count"` 行只有 `ASH82`，而它是 `ta=50`（手牌族）⇒ 今天仍走这句。
            if (s.SelectFromChooseResolution != 0 && (s.SelectFromChooseResolution == CardDef.CountSetSpec.Unknown || !boardPoolWired || (s.TargetsAffected != 10 && s.TargetsAffected != 55))) return "countSet.selectFromChooseResolution 只接到【棋盘族（ta=10/55）的计数路径】（MeetsBoardCriteria 里那两跳：去重 → 收窄）；以下三种仍判不了：① 非棋盘族（手牌/墓地/牌库的候选是 `CardInstance` 而不是 `UnitState`，与账 1282(b)(c) 同一根）② 不收集候选的那两支（`mult` / `avail`）③ 该格**未知（没落）**";   // 🆕 2026-10-29（B98_G6 · 账 676 的 (a) 格）：**收窄成「旗有、但候选不是棋盘单位」才 gap** —— 棋盘族已由 `MeetsBoardCriteria` 的两跳接上（判据 `AbilityLogic__GetTargets.c:1101-1140` / `:155-172`）。⚠️ `-1`（没落）与「非棋盘族」一样判不了 ⇒ ⛔ 别把它当 0 放行（放行 = 把「判不了」静默当成「没这个位」）
            return null;
        }

        /// <summary>`-1` = **没落/未知**（旧表）· 其它非 0 值 = **本版没实现** —— 两种都要 `null`，
        /// 但日志里必须分得开（「没落」不是「没实现」）。🆕 2026-10-26（`W6K11`）：`SetGap` 撤掉那 9 格后本方法**当前无调用点**（留着给将来新挡的格用同一套文案）。</summary>
        static string Gap(string field, int value)
        {
            return value == CardDef.CountSetSpec.Unknown
                 ? $"countSet.{field} **未知（没落）**"
                 : $"countSet.{field}={value} 未实现";
        }

        /// <summary>数一数；返回 `null` = 这个集合/条件本版判不了。<paramref name="count"/> 供诊断。
        /// <para><paramref name="countOption"/> = `BoardCriteria.countOptions`：
        /// `0` = 数命中个数；`10` = **同方 −1 / 异方 +1 累加**（`BoardAnalysis__MeetsCriteria.c:104-118`）。</para>
        /// <para>🆕 **2026-10-26（批次 9 · 执行代理 `E7P06` · 账 676 ④）**：<paramref name="cands"/> /
        /// <paramref name="candSides"/> 非 `null` 时进入「**收集档**」—— 每个命中候选**各收集一份**
        /// （单位 + 它所在的方），`count` 记的是**命中个数**（⛔ 不在这里加权）。
        /// 为什么要这样分：原版 `AbilityLogic.GetTargets` 先把列表**截到 `maxTargets`**、
        /// `MeetsCriteria` 才按截后的列表累加（`countOptions == 10`）⇒ 加权必须在截断**之后**做。
        /// ⚠️ 只有「加权 + 需要截断」这一种组合才进收集档；其余(含全部现池数据 `maxTargets = 0`)走原路。</para></summary>
        static int? CountSet(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond, int countOption,
                             UnitState actor, out int count, out string gap,
                             System.Collections.Generic.List<UnitState> cands = null,
                             System.Collections.Generic.List<int> candSides = null, UnitState target = null, bool targetKnown = false)   // 🆕 2026-10-30（B99_T2）：`ta=30/90` 的 `[targetCard]` 池要它（见类尾 `CountSetT2` 的 doc）
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
                        // 🆕 2026-10-26（`B1`）：逐候选的 36 格判据**改走 `TargetCriteriaEval.Matches`**
                        //   （全仓唯一一份）。改前这里只有 `IncludeUnit`（卡种 / 残骸 / 自他），
                        //   `keywordFilter` / `traitsFilter` / 四组 stat / `filterSpecificCards`
                        //   四维**一格都没跑**（而且整行被 `SetGap` 挡成 null）。
                        bool? pass = BoardCandidatePasses(ctx, owner, set, u, actor, out string bWhy);
                        if (pass == null) { gap = bWhy; return null; }
                        if (!pass.Value) continue;
                        var v = ConditionEval.MeetsUnit(ctx, u, cond, false);
                        if (v == null) { gap = $"cond={cond} 在候选身上判不了"; return null; }
                        if (!v.Value) continue;
                        // 🆕 2026-10-26（`E7P06`）：收集档只记个数 + 候选（加权与截断都在调用方，
                        //   见方法 doc）；否则照旧就地加权。
                        if (candSides != null) { candSides.Add(p); cands.Add(u); count += 1; }
                        else count += Weight(owner, p, countOption);
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
                    // 🆕 2026-10-26（`B1`）：`keywordFilter` / `filterSpecificCards` 在**卡模板**上就能判
                    //   （原版 `RawCardScript.IsKeyword` = `rawCardData.keyword(+0x98) == k`，纯静态）；
                    //   运行期那几维（trait / 四组 stat）墓地候选**没有单位实体** ⇒ 判不了、如实出声。
                    // 🔴 2026-10-29（`B99_E3` · 账 1281/1379 的 (c) 格）：判据改走**全仓唯一那份卡面级判据**
                    //   `TargetCriteriaEval.MatchesRawCard`（= 原版 `FilterMethods.CheckIfRawCardMeetsCriteria`），
                    //   并把「要靠阵营 / 实例才判得了」的几格一并接上 ⇒ 原来整条挡成 gap 的
                    //   `traitsFilter` / 四组 stat / `armyFilter` / `spellType` / 稀有度**现在都判得了**
                    //   （卡面值就是原版这一路读的值：`CheckIfRawCardMeetsCriteria.c:18-103` 全程只读 `RawCardScript`）。
                    bool? cpass = CemeteryCandidatePasses(set, d.Card, owner, d.Owner, actor, d.Instance, out string cWhy);
                    if (cpass == null) { gap = cWhy; return null; }
                    if (!cpass.Value) continue;
                    var v = ConditionEval.MeetsCard(ctx, d.Card, d.Owner, cond, false, d.InstanceId);
                    if (v == null) { gap = $"cond={cond} 在墓地候选身上判不了"; return null; }
                    if (!v.Value) continue;
                    // 🆕 2026-10-26（`E7P06`）：收集档只记个数 —— 墓地候选是 `DeadUnit`（没有 `UnitState`）
                    //   ⇒ 收集不到「候选实体」；需要按 `targetSelectMethod` 排序时 `ApplyMaxTargets` 会
                    //   如实报「判不了」（⛔ 不拿个数顶排序）。
                    if (candSides != null) count += 1;
                    else count += Weight(owner, d.Owner, countOption);
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
                    // 🆕 2026-10-26（`B1`）：同棋盘那一支 —— 36 格判据走 `TargetCriteriaEval.Matches`
                    //   （⛔ 不在这里再写一份）。池内 `ta=150` 的 count 行现读 **0 条**，这里只为口径一致。
                    bool? pass = BoardCandidatePasses(ctx, owner, set, u, actor, out string bWhy);
                    if (pass == null) { gap = bWhy; return null; }
                    if (!pass.Value) continue;
                    var v = ConditionEval.MeetsUnit(ctx, u, cond, false);
                    if (v == null) { gap = $"cond={cond} 在上一条能力的目标身上判不了"; return null; }
                    if (!v.Value) continue;
                    // 🆕 2026-10-26（`E7P06`）：同 ①（收集档只记个数 + 候选）。
                    if (candSides != null) { candSides.Add(side); cands.Add(u); count += 1; }
                    else count += Weight(owner, side, countOption);
                }
                return count;
            }

            // ---- ③a **手牌 `50` · 手牌+牌库 `51` · 牌库 `65`**（三个卡池，2026-10-29 `B99_E3` 起统一）----
            //  判据 = `AbilityLogic__GetTargets.c:918-931(50 手牌)` / `:933-950(51 手牌 AddRange 牌库)` /
            //  `:966-978(65 牌库)` ⇒ 候选池 = **某一侧的 `PlayerHand` / `PlayerState.Deck`**（按
            //  `friendlyUnits/enemyUnits` 选边），再按 `TargetCriteria` 逐候选过一遍
            //  （`FilterMethods__CheckIfMeetsCriteria`；卡面级那一半转调 `TargetCriteriaEval.MatchesRawCard`）。
            //  🔴 **2026-10-29（`B99_E3`）就地订正**：本条原来写「只实现卡种两档、其余维度一律如实判不了」
            //     —— **已过期**（铁律 5）。现在支持面 = `CardPoolSetSupported` 那一道闸（只剩
            //     `selectFromChooseResolution` 一格挡着，理由见那个方法的 doc）；判定/裁剪全在
            //     `CountCardPool` / `CountCardList` / `CardPoolCandidatePasses`（类尾）与 `ApplyMaxTargets`。
            //  🔴 判据文件 → `资料/完全复刻/判据/B88_R2_80档_VA判定.md` §二 / §四·3。
            if (ta == 50 || ta == 51 || ta == 65)
            {
                if (SideEmpty(set)) return 0;
                string handGap;
                if (!CardPoolSetSupported(set, ta, out handGap)) { gap = handGap; return null; }
                int? cards = CountCardPool(ctx, owner, set, cond, countOption, ta, actor, cands, candSides, ref gap);
                if (cards == null) return null;
                count = cards.Value;
                return count;
            }

            // ---- ③b `52 inHandAndPlayFiltered` 与 `20 / 30 / 90` 三档：**2026-10-30（`B99_T2`）起交给类尾 `CountSetT2`** ----
            //  判据（`AbilityLogic__GetTargets.c:920-948`，`iVar6 == 0x34`）：候选池 =
            //    `GetCardsInHandList(…, 本方/异方)` 再 `GetUnitsInPlayList(…, clearList = 0)` **追加**
            //    —— **两个池子拼在一起**，不是「场上的那半已在 ① 数过」。
            //  🔴 2026-10-23 订正过的旧口径（留痕，⛔ 别按它改回去）：原来按「手牌 + 场上已在 ① 数过」
            //    写过一版最小档 ⇒ **少数了场上那一半**（① 与本支互斥、永远走不到）。
            //  🔴 2026-10-30（`B99_T2` · 铁律 5）：上面那句「只有 `52` 仍如实判不了」**已过期** —— `52` 现已按
            //    「手牌池 ＋ 场上池」两半各判一次再相加（`CountSetT2`；卡片那半进不了 `cands`，只记个数）。
            //  `20 self` / `30 target` / `90 targetIfMeetsCriteria` 同批补齐（原版按 `ta` 建池：`[thisCard]` /
            //    `[targetCard]` / `[targetCard]` 再落常规筛，判据 `GetTargets.c:184-206` / `:747-759`）。
            //  ⚠️ `30` / `90` 要 `targetCard`：本文件四个入口都多了尾部可选形参 `target` / `targetKnown`；
            //    今天没有调用点喂它 ⇒ 如实出声（⛔ 不拿 `ctx.LastTarget`（代词槽）/ `LastTargets[0]` 顶）。
            //  📊 可达性（`cards_engine.json` v44 现读）：`unitConditions[].countSet` 的 `ta` 里这四档 **0 行** ⇒ 休眠件，照样建。
            int? t2 = CountSetT2(ctx, owner, set, cond, countOption, actor, target, targetKnown, cands, candSides, ref gap);
            if (t2 != null) { count = t2.Value; return count; }
            if (gap == null) gap = $"targetsAffected={ta} 本版未实现";
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
        /// <para>取值口径与棋盘那一支一致：**`!= 0` 才筛**（`Unknown(-1)` = 未知 ⇒ 不筛，
        /// 与原版「没落 ≠ 明确不要」同一条纪律）。
        /// ⚠️ 2026-10-26（`B1`）：棋盘那一支已改走 `TargetCriteriaEval.Matches`，本函数
        /// **仍是自己一份**（抽到的牌不在棋盘上，`Matches` 收不了）—— 但它只判「卡种」那一格，
        /// 其余维度仍由调用方按 `null` 出声。</para>
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

        /// <summary>
        /// 🆕 **2026-10-26（第八十三会话 · 执行代理 `B1`）：棋盘跳 / `ta=150` 两支的逐候选判据** ——
        /// **直接转调 <see cref="TargetCriteriaEval.Matches"/>**（全仓唯一那份 36 格判据，⛔ 别在这里再写一份）。
        ///
        /// <para>**为什么换成它**：改前这两支只调 `IncludeUnit`（卡种 / 残骸 / 自他）+ `ConditionEval`，
        /// `keywordFilter` / `traitsFilter` / 四组 `MeetsStatCriteria` / `filterSpecificCards`
        /// 四维**一格都没跑**，而 `SetGap` 又把带这四维的行整条挡成 `null` ⇒ 池内
        /// **18 行 / 13 张卡**一直在「正文正则兜底 + 出声」（普查见
        /// `资料/完全复刻/报告/B1b_追加三项判据.md` §③）。
        /// `TargetCriteriaEval` 里这四维**早就有实现**（`KeywordPasses` / `TraitPasses` /
        /// `StatGroupsPass` / ⑫白名单）⇒ 本笔只是把它接上，**没有第二份判据**。</para>
        ///
        /// <para>⚠️ **`cond` 不在这里判**：调用方随后仍调 `ConditionEval.MeetsUnit/MeetsCard`（既有口径，
        /// 也是 `CountEval` 文件头的分工）⇒ 这里给 `Matches` 传 `cond: null`
        /// （= `none(0)` = 放行），**不判两次**。</para>
        /// <para>⚠️ `thisCard` 传 <paramref name="actor"/>（= 这条 ability 的所有者单位）。
        /// 只有 `4/5/7` 三档 stat 需要载体，`Matches.TryStatThisCardStat` 已按档判过；
        /// 战术卡那条路 `actor` 本来就是 `null`，池内也 0 行用到那三档。</para>
        /// <para>⚠️ 每多导入一个候选侧信息，返回 `false` 的方向**只会更准、不会更宽**：
        /// `Matches` 对判不了的维度返回 `null`（本方法原样上抛 ⇒ 调用方 `return null` + 出声）。</para>
        /// </summary>
        static bool? BoardCandidatePasses(BattleContext ctx, int owner, CardDef.CountSetSpec set,
                                          UnitState u, UnitState actor, out string gap)
            => TargetCriteriaEval.Matches(ctx, owner, set, u, null, actor, null, out gap);

        /// <summary>
        /// 🆕 **2026-10-26（`B1`）：墓地候选（`ta=60`）的逐候选判据。**
        ///
        /// <para>**为什么不能走 <see cref="TargetCriteriaEval.Matches"/>**：它收 `UnitState`，
        /// 而墓地候选是 `BattleContext.DeadUnit`（只有 `Card` / `Owner` / `Instance`），
        /// **没有运行期状态**（当前攻/血/费、当前 traits 全没了）。</para>
        ///
        /// <para>🔴 **2026-10-29（`B99_E3`）就地订正 —— 下面两段是改前的旧口径，已过期**：
        /// 原来这里「只判 `filterSpecificCards` + `keywordFilter` 两维、其余整条 `null`」——
        /// 现在**整份判定转调** <see cref="CardPoolCandidatePasses"/>（卡面那一半 = 全仓唯一那份
        /// `TargetCriteriaEval.MatchesRawCard` ⇒ `CheckIfRawCardMeetsCriteria.c:18-103` 全部九格：
        /// 卡种白名单 / `spellType` / traits / 攻血费稀有度四组 / 白名单 / `armyFilter` / `keywordFilter`+`excludeRace`；
        /// 另补 `includeSelf`/`excludeNotSelf`/`excludeSimilarCards`/`excludeFriendly|EnemyHero`/`excludeEnchantments`）。
        /// ⚠️ 池内 `ta=60` 的 count 行只用到 `keywordFilter`（`SAU57` ×5 = Infantry · `GOF94` = Beast）
        /// ⇒ 本笔对**现池零行为变化**（多判的那几格都是 0）。</para>
        /// </summary>
        static bool? CemeteryCandidatePasses(CardDef.CountSetSpec set, CardDef card, int owner, int side,
                                             UnitState actor, CardInstance inst, out string gap)
        {
            // 🔴 2026-10-29（`B99_E3` · 账 1281/1379 的 (c) 格）：**改走全仓唯一那份卡面级判据**。
            //   改前这里自带一份「只认 keywordFilter + filterSpecificCards、其余整条 gap」的手写表
            //   （判据只覆盖 `CheckIfMeetsCriteria.c:304-313` / `:465-483` 两格）——
            //   而原版这一路对墓地候选跑的也是**同一个** `FilterMethods.CheckIfMeetsCriteria`，
            //   其中「只读卡模板」的那一半**原版就有函数**：`CheckIfRawCardMeetsCriteria.c:18-103`
            //   （卡种白名单 / spellType / traits / 攻血费稀有度四组 / 白名单 / army / keyword+excludeRace），
            //   我们的等价物 = `TargetCriteriaEval.MatchesRawCard`（⛔ 别在这里再写第三份）。
            //   ⇒ 本方法只剩「它不管、但墓地这一路要管」的四格：`includeSelf` / `excludeNotSelf` /
            //     `excludeSimilarCards` / `excludeFriendly|EnemyHero` / `excludeEnchantments`。
            return CardPoolCandidatePasses(owner, side, set, card, actor, inst, out gap);
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

        // ======================================================================================
        //  🆕 2026-10-26（批次 9 · 执行代理 `E7P06` · 账 676 ④）：`maxTargets` 截断
        //  ======================================================================================
        //  判据（第一权威 = `d:/2/tools/decomp_full/`，逐句现读）：
        //   · **为什么要在 `BoardCriteria` 这条路上截**：`BoardAnalysis__MeetsCriteria.c:101`
        //     调 `AbilityLogic__GetTargets(param_1, lVar2 /*countCriteria*/, param_3, param_4, param_5, **1**, 0)`
        //     —— 第 6 实参 = `checkMaxTargets`（ILSpy 签名
        //     `GetTargets(BattleManager, TargetCriteria, AbilityData, bool checkMaxTargets = true)`）
        //     ⇒ **数的是截断之后的列表**（`:103 iVar6 = list._size`）。
        //   · **截断本体** = `AbilityLogic__GetTargets.c:1164-1365`：
        //       `if (!checkMaxTargets || maxTargets < 1) skip`（`:1164`）
        //       `if (pool.Count <= maxTargets) skip`（`:1169`）
        //       `switch (targetSelectMethod)`（`:1171`）：
        //         `1 highestAttack`  ⇒ `OrderByDescending(CardScript.GetHighestAttackValue)`
        //                              ＋次键 `CurrentIntrinsicHealth`（`:1172-1214`）；末尾 `Take(maxTargets)`
        //         `2 highestValueInPlay` ⇒ `OrderByDescending(GetValueInPlay())`（`:1215-1246`）
        //         `3 lowestValueInPlay`  ⇒ `OrderBy(GetValueInPlay())`（`:1247-1261`）
        //         `5 lastAdded`     ⇒ 从**表尾**往前取 `maxTargets` 个（`:1274-1313`）
        //         `6 lowestHealth`  ⇒ `OrderBy(CurrentIntrinsicHealth)`（`:1314-1338`）
        //         `7 firstAdded`    ⇒ 从**表头**取 `maxTargets` 个（`:1339-1361`）
        //         `default`（= `0 random` / `4 cardTarget`）⇒ 逐个
        //                              `BattleManager.GetRandomInt(0, pool.Count)` 抽一个并从池里删（`:1252-1273`）
        //       非 `default` 的六档统一在 `:1365` `Enumerable.Take(pool, maxTargets) → ToList()`。
        //   · **顺序键的语义**（ILSpy 的闭包体 `AbilityLogic._003CGetTargets_003Eb__26_1..4` 现读）：
        //       `highestAttack` 键 = `CardScript.GetHighestAttackValue()` = **max(CurrentMeleeAttack, CurrentRangeAttack)**
        //       （`CardScript__GetHighestAttackValue.c:5-13`）；`lowestHealth` 键 =
        //       `CurrentIntrinsicHealth`（`EntityScript__get_CurrentIntrinsicHealth.c`：血量 **减去**
        //       `+0x108` 那一串 intrinsic 修饰里 `(+0x30 != 0 && +0x38 == 1)` 的和）。
        //
        //  🔑 **纯计数档（`countOptions == 0`）的结论可以省掉顺序**：上面**每一个**分支截完都恰好
        //     剩 `maxTargets` 个（排序+Take / 头尾取 N / 随机抽 N 都是 N）⇒ 个数 = `min(count, max)`。
        //     ⚠️ 只有 `countOptions == 10`（同方 −1 / 异方 +1）**必须真的选出是哪几个**，因为净和依赖它们。
        //
        //  ⚠️ **`Unknown(-1)` 按「不限」处理**：`maxTargets` 是 `int` 字段（原版 `-1` 不存在，`<= 0` 即不限），
        //     而我们的 `CountSetSpec` 用 `-1` 当「没落」哨兵；它与「不限」**同向**（都不截），
        //     与本文件其它布尔格「`Unknown ⇒ 不筛`」是同一条约定。数据侧现读 **99/99 + 2/2 + 31/31
        //     全部落的是确定的 `0`** ⇒ 生产路径上走不到这一格。
        // ======================================================================================

        /// <summary>原版 `AbilityLogic.GetTargets` 尾段的 `maxTargets` 截断（判据全文见上面那段）。
        /// <para>`candSides == null` ⇒ 纯计数（`countOptions == 0` 或 `CountTargets` 那条路）：
        /// 个数 = `min(count, maxTargets)`。</para>
        /// <para>`candSides != null` ⇒ **加权档**（`countOptions == 10`）：先按 `targetSelectMethod`
        /// 选出 `maxTargets` 个候选，再按「同方 −1 / 异方 +1」累加。
        /// ⚠️ 候选**不是 `UnitState`**（墓地 `DeadUnit` / 抽到的牌）时排不出顺序 ⇒ `null` + `gap`
        /// （⛔ 不拿个数顶）。</para></summary>
        static int? ApplyMaxTargets(CardDef.CountSetSpec set, int count,
                                   System.Collections.Generic.List<UnitState> cands,
                                   System.Collections.Generic.List<int> candSides,
                                   int owner, int countOption, out string gap)
        {
            gap = null;
            if (set == null) return count;
            int max = set.MaxTargets;
            if (max < 1 || count <= max) return count;      // `:1164` / `:1169` 那两道早退

            if (candSides == null) return max;               // 纯计数档（见上面 🔑 那条结论）

            if (cands == null || cands.Count != count || candSides.Count != count)
            {
                gap = "需要按 `maxTargets` 截断（`AbilityLogic__GetTargets.c:1164-1365`），但这一档的候选"
                    + "**不是 `UnitState`**（墓地 `DeadUnit` / 抽到的牌）⇒ 排不出 `targetSelectMethod` 的顺序，判不了";
                return null;
            }

            int method = set.TargetSelectMethod;
            var keep = new System.Collections.Generic.List<int>(max);
            switch (method)
            {
                case 7:   // firstAdded：池序（= 我们扫棋盘 / 表序）的前 max 个（`:1339-1361`）
                    for (int i = 0; i < max; i++) keep.Add(i);
                    break;
                case 5:   // lastAdded：池序的后 max 个（`:1274-1313`）
                    for (int i = count - max; i < count; i++) keep.Add(i);
                    break;
                case 1:   // highestAttack：`OrderBy(isRemnant).ThenByDescending(GetHighestAttackValue)`（`:1172-1214`）
                    {
                        var idx = new System.Collections.Generic.List<int>(count);
                        for (int i = 0; i < count; i++) idx.Add(i);
                        // 键链判据（🥇 `out_il2csharp/Assembly-CSharp/AbilityLogic.cs:4059`）：`ThenByDescending(OrderBy(pool, b__26_0), b__26_1)`
                        idx.Sort((a, b) =>
                        {
                            int ra = (cands[a] != null && cands[a].IsRemnant) ? 1 : 0, rb = (cands[b] != null && cands[b].IsRemnant) ? 1 : 0;
                            int c = ra.CompareTo(rb);                            // ① OrderBy(isRemnant)：非残骸(0) 在前
                            if (c != 0) return c;
                            int ka = HighestAttackKey(cands[a]), kb = HighestAttackKey(cands[b]);
                            return kb != ka ? kb.CompareTo(ka) : a.CompareTo(b); // ② ThenByDescending + ③ 稳定排序（平手保持池序）
                        });
                        // ⚠️ 2026-10-30（`B99_T2` · 铁律 5）修的第二处：改前这里**漏了 `isRemnant` 主键**，
                        //   注释还把 `CurrentIntrinsicHealth` 说成「次键」—— 两处都错：那是 `6 lowestHealth`
                        //   的键（`b__26_2`），本档的次键就是 `GetHighestAttackValue`。池内该组合 0 行。
                        for (int i = 0; i < max; i++) keep.Add(idx[i]);
                        break;
                    }
                default:
                    // 原版这里不是「判据未取到」：🥇 `out_il2csharp/Assembly-CSharp/AbilityLogic.cs:4021`（`targetSelectMethod - 1` 的 switch）
                    //   把 `0 random` / `3 ⇒ method 4 (cardTarget)` / `>6 未知` **全落到同一支随机循环**（`:4024` 的
                    //   `L_180819895`；`case 3: goto` 在 `:4091-4093`）⇒ `cardTarget` 原版就是「随机抽一张」。
                    gap = method switch
                    {
                        0 or 4 => $"`targetSelectMethod={method}`（random(0) / cardTarget(4)）：原版**两支都进随机抽**"
                           + "（il2csharp `AbilityLogic.cs:4024` / `:4091-4093`）⇒ 本版随机源已落地（`ctx.GetRandomInt` + 预烧 100 表）、选择链未建模，判不了",
                        2 or 3 => "`targetSelectMethod = highest/lowestValueInPlay(2/3)` 要 `CardScript.GetValueInPlay()`"
                           + "（原版 2 = `OrderByDescending` / 3 = `OrderBy`），本版**没有**这个等价字段 ⇒ 判不了",
                        6 => "`targetSelectMethod = lowestHealth(6)` 要 `CurrentIntrinsicHealth`"
                           + "（`EntityScript__get_CurrentIntrinsicHealth.c`：当前血量 **减去** intrinsic 修饰），"
                           + "本版**没有**这个等价字段（`Health` / `MaxHealth` 都不是它）⇒ 判不了",
                        _ => method == CardDef.CountSetSpec.Unknown
                                ? "`targetSelectMethod` **未知（没落）** ⇒ 判不了"
                                : $"`targetSelectMethod={method}` 不在 `TargetSelectMethod` 的 0..7 里 ⇒ 判不了",
                    };
                    return null;
            }

            int sum = 0;
            for (int i = 0; i < keep.Count; i++) sum += Weight(owner, candSides[keep[i]], countOption);
            return sum;
        }

        /// <summary>`CardScript.GetHighestAttackValue()` 的等价物（`CardScript__GetHighestAttackValue.c:5-13`：
        /// `max(CurrentMeleeAttack, CurrentRangeAttack)`）—— 只供 `maxTargets` 的 `highestAttack` 排序用。</summary>
        static int HighestAttackKey(UnitState u)
        {
            if (u == null) return int.MinValue;
            int melee = u.CurrentMeleeAttack, ranged = u.CurrentRangeAttack;
            return melee >= ranged ? melee : ranged;
        }

        /// <summary>🆕 2026-10-28（B88 · `A1b`）：<see cref="CountSet"/> 的 `ta == 50`（手牌）那一支的
        /// **支持面闸** —— 只认「卡种两档（`includeMinions` / `includeSpells`）＋ 阵营」这一种形状，
        /// 其余维度一律 `false` + `why`（调用方据此 `null` + `gap`，⛔ 不按「大概是」补，铁律 2）。
        ///
        /// <para>现池唯一消费者 = `GSC35 Benefictus` / `GSC59 Day of Ascension` 的 `multiplier`
        /// （`abilities[0].logic[0].multiplierSet`：`enemyUnits=1 · includeMinions=1 · includeSpells=1`，
        /// 其余全 0，见 `cards_engine.json` 现读）。判据 → `资料/完全复刻/判据/B88_R2_80档_VA判定.md` §二/§四·3。</para>
        /// <para>⚠️ **`playerTurn` 不在这道闸里**：原版它是 `BoardCriteria` 那一层的整条闸，不是候选筛
        /// （同 <see cref="SideEmpty"/> 的 doc）。这两张卡的 `playerTurn = -1`（缺列）⇒ 若这里要求它已知就整支废掉。</para>
        /// </summary>
        static bool CardPoolSetSupported(CardDef.CountSetSpec set, int ta, out string why)
        {
            // 🔴 2026-10-29（`B99_E3` · 账 1281/1379 的 (b) 格）：**支持面从「只认卡种两档」放开到整条 criteria**。
            //   改前 `HandSetSupported` 把 `keywordFilter` / `traitsFilter` / 四组 stat /
            //   `filterSpecificCards` / `armyFilter` / `maxTargets` / `includeWhispers` 等
            //   **一律挡成切判不了**（= 整行 null + 出声）—— 其中 `keywordFilter` / 四组 stat /
            //   `includeWhispers` 三档**池内现在就真有消费者**：
            //     · `GSC75 Roaming Outriders`（`ta=50·friendlyUnits·keywordFilter=10 Vehicle`）⇒ 改前 gap，
            //       那条 `multiplier` 数不出来（「每有一辆载具就重复一次」直接失效）；
            //     · `GSC37 Patriarch`（`ta=50·includeWhispers=1`）⇒ 改前 gap（「每张手牌减 1 费」失效）。
            //   ⇒ 这些格**全都能在卡模板上判**（原版 `CheckIfRawCardMeetsCriteria.c:18-103` 就是这么做的，
            //     我们的等价物 = `TargetCriteriaEval.MatchesRawCard`）⇒ 不再挡。
            //   ⚠️ 仍然挡的只有两格（都不是「筛」，本版接不上卡片候选）：
            //     · `selectFromChooseResolution`：它把候选池收窄成「选牌窗里点定的那一张」，
            //       而我们那一格装的是 `UnitState`（`BattleContext.CardChosenInSelection`）；
            //     · `excludeTargetted`：原版全树**只有声明、0 个读者**（见 `SetGap` 的注释）⇒ 不参与判定，
            //       **不挡**（挡着反而把整行误判成判不了）。
            why = null;
            if (set.SelectFromChooseResolution != 0)
            {
                why = $"ta={ta}：`selectFromChooseResolution` 非 0 —— 它把候选池收窄成「选牌窗里点定的那一张」，"
                    + "而 `ctx.CardChosenInSelection` 装的是 `UnitState`，卡片候选接不上（账 676 的 (a) 格同一根）";
                return false;
            }
            return true;
        }

        // ==========================================================================================
        //  🆕 2026-10-29（第九十七会话 · 波 1 · 执行代理 `B97_W5`）—— 账 `:676` 的 (a) 格
        //  `TargetCriteria.selectFromChooseResolution`（`+0x80`）的**消费端**
        // ==========================================================================================
        //  判据（🥇 `decomp_full/AbilityLogic__GetTargets.c:1101-1140`，逐句现读）：
        //    候选池建好之后，
        //      `if (criteria.selectFromChooseResolution(+0x80) != 0) {`
        //        `chosen = manager.cardChosenInSelection(+0x2e8);`
        //        `if (chosen != null) {`
        //          `// ① 筛：倒序扫，把「卡 id(+0x28 → +0x38) != chosen.uniqueId」的逐条 RemoveAt`
        //          `// ② 截成 1：再把下标 > 0 的全 RemoveAt（留第 0 条）`
        //          `// ③ 清空：manager.cardChosenInSelection = null`
        //        `}`
        //      `}`
        //  🔴 **语义是「候选池的来源」不是「筛条件」**（所以它不属于 `SetGap` 那张「未实现的筛」清单：
        //    它会把池子**收窄成恰好一张**，或者——在没选过牌时——**原样不动**）。
        //  ⚠️ **写点已经在了**（⛔ 本批不新增）：`EffectResolver.DoChooseCard` 把选中的那张同时写进
        //    `ctx.LastChosenCard`（我们自己的槽）与 `ctx.CardChosenInSelection`（原版 `+0x2E8`）——
        //    判据 `ChoiceOfCardEnemy.c:47` / `ClickChosenCardDone.c:22`。
        //  ⚠️ **`null` 池 / `null` ctx ⇒ 原样返回**（原版这两处都在 `if (chosen != null)` 里面）。
        //  📊 **可达性**：池内该 flag 非 0 的 criteria 行现读 88 行 / 78 张卡（最大 `ta=180` 43 行），
        //    其中能走到本方法的（`ta` 我们支持的那几档）今天仍受 `SetGap` 的其它格限制 ⇒ **休眠件**
        //    （铁律 11：照样建、⛔ 不判 D）。
        // ==========================================================================================

        /// <summary>原版 `AbilityLogic.GetTargets` 末尾那段 `selectFromChooseResolution` 收口：
        /// **把候选池收窄成「刚在选牌窗里点定那一张」**，然后**清空** `ctx.CardChosenInSelection`。
        ///
        /// <para>⚠️ 调用方必须**只在** criteria 的 `selectFromChooseResolution != 0` 时调它
        /// （传 <paramref name="flag"/>，本方法自己再判一次 —— 免得把「别的池子」也收窄成一张）。</para>
        /// <para>`flag == 0` ⇒ 原样返回（一个都不动、也不清空 —— 原版那一支压根不执行）。</para>
        /// <para>`flag != 0` 且没选过牌（`ctx.CardChosenInSelection == null`）⇒ **原样返回**。
        /// 原版是 `if (chosen != null)` 才动手 ⇒ ⛔ 别在这儿清空成空表（那会把「全部候选」变成「没有候选」）。</para>
        /// </summary>
        /// <returns>筛过的池子（<paramref name="pool"/> 原对象可能被就地改，也可能换一份新的 —— 用返回值）。</returns>
        public static System.Collections.Generic.List<UnitState> ApplySelectFromChooseResolution(BattleContext ctx, int flag,
                                                                      System.Collections.Generic.List<UnitState> pool, string by = null)
        {
            if (flag == 0 || ctx == null || pool == null) return pool;   // 🆕 2026-10-29（B98_G6 · 账 676 计数池接线顺手订正）：原来这里还有 `|| pool.Count == 0` ⇒ **空池时不清 `cardChosenInSelection`**。原版 `.c:1101-1140` 的清空在 `if (chosen != null)` 里、**与池空不空无关**（筛完可能是 0 张，照样清）⇒ 去掉那半，否则「选了牌、但候选池本来就空」那次收窄会把选中项**留给下一条带同一旗的效果**
            var chosen = ctx.CardChosenInSelection;
            if (chosen == null) return pool;                       // 原版 `if (chosen != null)`

            int before = pool.Count;
            // ① 筛（原版倒序扫 + RemoveAt ⇒ 结果与原序一致；我们用 RemoveAll 保持顺序，语义相同）
            pool.RemoveAll(u => u == null || u.Card == null
                                || !string.Equals(u.Card.Id, chosen.Id, System.StringComparison.Ordinal));
            // ② 截成 1（原版第二轮循环 `if (iVar15 != 0) RemoveAt(iVar15)`）
            if (pool.Count > 1) pool.RemoveRange(1, pool.Count - 1);
            // ③ 清空（原版 `*plVar1 = 0`）
            ctx.CardChosenInSelection = null;

            ctx.Log($"{(string.IsNullOrEmpty(by) ? "目标池" : by)}：`selectFromChooseResolution` ⇒ 候选池按刚选定的"
                  + $"「{chosen.Name}」收窄，{before} → {pool.Count} 张（原版 `AbilityLogic__GetTargets.c:1101-1140`："
                  + "筛 → 截成 1 → 清 `cardChosenInSelection`）"
                  + (pool.Count == 0 ? "；⚠️ **选中的那张不在候选池里 ⇒ 本行没有目标**（原版同样得到空表）" : ""));
            return pool;
        }

        // ==========================================================================================
        //  🆕 2026-10-29（第九十九会话 · 波 1 · 执行代理 `B99_E3`）—— 账 `1281` / `1379` 的 (b)(c) 两格：
        //  **卡片候选池**（手牌 `50` / 手牌+牌库 `51` / 牌库 `65` / 墓地 `60`）的建池 + 逐候选判据 + 计数
        // ==========================================================================================
        //  判据（第一权威 = `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`，逐句现读）：
        //   · **建池**（`ta` 分派）：
        //       `:918-931` `iVar6 == 0x32 (50)` ⇒ `BattleManager.GetCardsInHandList(…, 本/异方, …)`；
        //       `:933-950` `iVar6 == 0x33 (51)` ⇒ 同上，再 `AddRange(GetCardsInDeck(…))`；
        //       `:952-964` `iVar6 == 0x3c (60)` ⇒ `BattleManager.GetCardsInCemetery(…)`；
        //       `:966-978` `iVar6 == 0x41 (65)` ⇒ `BattleManager.GetCardsInDeck(…)`。
        //     ⚠️ 「本/异方」那两个 bool 由 `:204-212` 现算：`bVar16 = 本方那一侧` · `bVar17 = 异方那一侧`
        //        —— 与我们 `SideMatches(owner, p, set)` 同义。
        //   · **逐候选筛** = `:1086-1095` 的 `FilterMethods__CheckIfMeetsCriteria`。
        //     其中「只读卡模板」的那一半原版另有函数 `FilterMethods__CheckIfRawCardMeetsCriteria.c:18-103`
        //     ⇒ 我们的等价物 = `TargetCriteriaEval.MatchesRawCard`（⛔ 别在 CountEval 里再写一份，
        //     那会变成第三份判据）。剩下几格（阵营 / 自他 / 相似卡 / 英雄排除 / 附魔排除）在
        //     `CardPoolCandidatePasses` 里补，判据 = `CheckIfMeetsCriteria.c:203-272` / `:430-463`。
        //   · **裁剪**（`maxTargets` / `targetSelectMethod`）**不在这里** —— 由调用方走
        //     `ApplyMaxTargets`（`AbilityLogic__GetTargets.c:1164-1365`），与本文件棋盘那一支同一份。
        //  📊 可达性（现读 `cards_engine.json`）：
        //     · `ta=50`：`countSet` 2 行（`ASH82`/`GOF93`）+ `multiplierSet` 4 行（`GSC35/37/59/75`）；
        //       `ASH82` 带 `selectFromChooseResolution=1` ⇒ 仍如实 gap（账 676 的 (a) 格）；
        //     · `ta=51`：`countSet` **0 行**（休眠件，铁律 11 照样建）；`ta=65`：`countSet` **0 行**。
        //     · `ta=60`：`countSet` 10 行，全部只用 `friendlyUnits/includeMinions/keywordFilter/cond`
        //       ⇒ 本笔对这些行**零行为变化**（多判的那几格都是 0）。
        // ==========================================================================================

        /// <summary>手牌 / 手牌+牌库 / 牌库（`ta = 50 / 51 / 65`）三个**卡池**的计数。
        /// <para>`null` = 判不了（写 <paramref name="gap"/>）。`countOptions == 10`（同方 −1 / 异方 +1）
        /// 时按「这份牌在谁手里」加权 —— 与棋盘那一支同一个 <see cref="Weight"/>。</para>
        /// <para>⚠️ **收集档**（<paramref name="cands"/> 非 `null`）只记个数：候选是 `CardInstance`、
        /// 不是 `UnitState` ⇒ 需要按 `targetSelectMethod` 排序时 `ApplyMaxTargets` 会如实报判不了
        /// （同墓地那一支的口径，⛔ 不拿个数顶排序）。</para></summary>
        static int? CountCardPool(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond, int countOption,
                                  int ta, UnitState actor,
                                  System.Collections.Generic.List<UnitState> cands,
                                  System.Collections.Generic.List<int> candSides,
                                  ref string gap)
        {
            if (ctx == null) { gap = "没有对局上下文（`ctx == null`）"; return null; }
            int count = 0;
            for (int p = 0; p < 2; p++)
            {
                if (!SideMatches(owner, p, set)) continue;
                var ps = ctx.Players[p];
                if (ps == null) continue;
                if (ta == 50 || ta == 51)                       // 50 = 手牌 · 51 = 手牌 + 牌库
                {
                    int? r = CountCardList(ctx, owner, p, set, cond, countOption, ps.Hand, actor, cands, candSides, ref gap);
                    if (r == null) return null;
                    count += r.Value;
                }
                if (ta == 51 || ta == 65)                       // 51 再并上牌库 · 65 = 只有牌库
                {
                    int? r = CountCardList(ctx, owner, p, set, cond, countOption, ps.Deck, actor, cands, candSides, ref gap);
                    if (r == null) return null;
                    count += r.Value;
                }
            }
            return count;
        }

        /// <summary><see cref="CountCardPool"/> 的单张列表演算（一份手牌 / 一份牌库）。
        /// 逐候选 = <see cref="CardPoolCandidatePasses"/> ＋ 条件行（`cond`，走 `ConditionEval.MeetsCard`
        /// —— 主语是一张**不在场上的卡**，与墓地那一支同一份口径）。</summary>
        static int? CountCardList(BattleContext ctx, int owner, int side, CardDef.CountSetSpec set, int cond,
                                  int countOption, System.Collections.Generic.List<CardInstance> list,
                                  UnitState actor,
                                  System.Collections.Generic.List<UnitState> cands,
                                  System.Collections.Generic.List<int> candSides,
                                  ref string gap)
        {
            if (list == null) return 0;
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var inst = list[i];
                var card = inst != null ? inst.Card : null;
                if (card == null) continue;
                bool? pass = CardPoolCandidatePasses(owner, side, set, card, actor, inst, out string why);
                if (pass == null) { gap = why; return null; }
                if (!pass.Value) continue;
                var v = ConditionEval.MeetsCard(ctx, card, side, cond, false, inst.Id);
                if (v == null) { gap = $"cond={cond} 在候选卡（{(card.Name ?? "?")}）身上判不了"; return null; }
                if (!v.Value) continue;
                if (candSides != null) count += 1;              // 卡片候选没有 `UnitState` ⇒ 收集档只记个数（同墓地那一支）
                else count += Weight(owner, side, countOption);
            }
            return count;
        }

        /// <summary>🆕 2026-10-29（`B99_E3`）：**一张不在场上的牌**（手牌 / 牌库 / 墓地）过不过这份 criteria。
        ///
        /// <para>**卡面那一半**直接转调 <see cref="TargetCriteriaEval.MatchesRawCard"/>（全仓唯一那份
        /// `CheckIfRawCardMeetsCriteria` 等价物 —— 卡种白名单 / `spellType` / traits / 四组 stat /
        /// `filterSpecificCards` / `armyFilter` / `keywordFilter`+`excludeRace`）。</para>
        ///
        /// <para>**这里补的那几格**（`MatchesRawCard` 故意不收：它们要「还有谁」这一信息）：
        /// <list type="bullet">
        /// <item>`includeSelf` / `excludeNotSelf`（`CheckIfMeetsCriteria.c:232-243`）：原版比的是
        /// **对象身份**（候选 `CardScript` == `thisCard`）。候选是一张**不在场上的牌**、`thisCard` 是
        /// 场上单位 ⇒ 两者永不是同一件东西 ⇒ `excludeNotSelf != 0`（「只要这张卡自己」）时**一个候选都不留**
        /// （同 `CountDrawnAsPreviousTargets` 的写法）；`includeSelf` 只「允许」自己，不构成限制。</item>
        /// <item>`excludeSimilarCards`（`:203-215`）：原版比 `rawCardData(+0x28)` 的**对象引用**
        /// ⇒ 我们比 `CardDef` 引用（与 `TargetCriteriaEval.Matches` 同一句）。</item>
        /// <item>`excludeFriendlyHero` / `excludeEnemyHero`（`:244-272`）：候选是 `hero` 型卡、且
        /// 阵营与这条 ability 的归属方相同/不同 ⇒ 淘汰。</item>
        /// <item>`excludeEnchantments`（`:430-463`）：候选实例挂了表里任一张附魔 ⇒ 淘汰
        /// （`CardInstance.ActiveEnchantments`；拿不到实例 ⇒ `null` + 出声，⛔ 不当「没挂」）。</item>
        /// </list></para>
        ///
        /// <para>`null` = 本版判不了（写 `gap`）—— ⛔ 不静默当「都过」。</para></summary>
        static bool? CardPoolCandidatePasses(int owner, int side, CardDef.CountSetSpec set, CardDef card,
                                             UnitState actor, CardInstance inst, out string gap)
        {
            gap = null;
            if (card == null) return false;
            bool? raw = TargetCriteriaEval.MatchesRawCard(card, set, out gap);
            if (raw != true) return raw;                        // `null` = 判不了（原样上抛）
            if (set.IncludeSelf == CardDef.CountSetSpec.Unknown
                || set.ExcludeNotSelf == CardDef.CountSetSpec.Unknown)
            { gap = "`includeSelf`/`excludeNotSelf` 未知（没落）"; return null; }
            if (set.ExcludeNotSelf != 0) return false;          // 见上面 doc：牌候选永远「不是 thisCard」
            if (set.ExcludeSimilarCards != 0)
            {
                if (actor == null || actor.Card == null)
                { gap = "`excludeSimilarCards` 要拿 thisCard 比原卡，本路径没有载体"; return null; }
                if (object.ReferenceEquals(actor.Card, card)) return false;
            }
            if (set.ExcludeFriendlyHero == CardDef.CountSetSpec.Unknown
                || set.ExcludeEnemyHero == CardDef.CountSetSpec.Unknown)
            { gap = "`excludeFriendlyHero`/`excludeEnemyHero` 未知（没落）"; return null; }
            if ((set.ExcludeFriendlyHero != 0 || set.ExcludeEnemyHero != 0)
                && string.Equals(card.Type, "hero", System.StringComparison.OrdinalIgnoreCase))
            {
                bool sameSide = side == owner;
                if (sameSide && set.ExcludeFriendlyHero != 0) return false;
                if (!sameSide && set.ExcludeEnemyHero != 0) return false;
            }
            if (set.ExcludeEnchantmentIds != null && set.ExcludeEnchantmentIds.Length > 0)
            {
                var ench = inst != null ? inst.ActiveEnchantments : null;
                if (ench == null) { gap = "候选牌没有实例 ⇒ `excludeEnchantments` 的附魔表读不到"; return null; }
                for (int i = 0; i < ench.Count; i++)
                {
                    var ec = ench[i] != null ? ench[i].Card : null;
                    if (ec == null || ec.Id == null) continue;
                    for (int k = 0; k < set.ExcludeEnchantmentIds.Length; k++)
                        if (string.Equals(ec.Id, set.ExcludeEnchantmentIds[k], System.StringComparison.Ordinal))
                            return false;
                }
            }
            return true;
        }

        // ==========================================================================================
        //  🆕 2026-10-30（第九十九会话 · 执行代理 `B99_T2`）—— 施工判据 `判据/B99_R4b_引擎与工具.md` §三·3·1 ①②④
        //  `CountSet` 的 `20 self` / `30 target` / `90 targetIfMeetsCriteria` / `52 inHandAndPlayFiltered` 四档
        // ==========================================================================================
        //  判据（🥇 `decomp_full/AbilityLogic__GetTargets.c`；🥈 `out_il2csharp/Assembly-CSharp/AbilityLogic.cs`，逐句现读）：
        //   · `20 self`（`GetTargets.c:184-191`，`iVar6 == 0x14`）：池 = `[thisCard]`（`List.Add(param_3)`），
        //     **不筛、不裁**，直接 `goto LAB_180818485`（`:1378-1380` = `return 池`）⇒ 个数恒 = 1。
        //   · `30 target`（`:192-206`，`iVar6 == 0x1e`；与 `220` 共用）：只有 `if (targetCard(param_5) != null)` 才
        //     `List.Add(param_5)`，随后 `goto LAB_180818474 → LAB_180818485`（= return）⇒ **不筛不裁**，
        //     个数 = `targetCard != null ? 1 : 0`。🔴 **null ⇒ 空池是原版语义**，⛔ 不以「自己」兜底。
        //   · `90 targetIfMeetsCriteria`（`:747-759`，`iVar6 == 0x5a`）：`if (targetCard != null) List.Add(param_5)`，
        //     然后**不 return**，一路落到 `:1086-1095` 的逐候选筛（`FilterMethods.CheckIfMeetsCriteria`）＋尾段裁
        //     ⇒ 个数 = `targetCard != null && 过筛 ? 1 : 0`。身份那半的判据 → `TargetCriteriaEval.Matches` 的 `case 90`。
        //   · `52 inHandAndPlayFiltered`（`:920-948`，`iVar6 == 0x34`）：先 `GetCardsInHandList(本/异方, 卡种位…)`
        //     **覆盖**成池，再 `GetUnitsInPlayList(…, clearList = 0 (第 3 实参), …)` **追加**同一侧别/卡种位的场上单位
        //     ⇒ 池 = 手牌 ∪ 场上（**同一份 `TargetCriteria` 两半都要过**），再走 `:1086-1095` 筛 ＋ 尾段裁。
        //  ⚠️ **`targetCard` 的来源**：本文件四个入口（`Meets` / `CountTargets` / `MeetsBoardCriteria` 两个重载）都加了
        //     尾部可选形参 `target` / `targetKnown`。**今天没有调用点喂它**（`EffectResolver` / `CardDef` / `ActionPump`
        //     都在本代理白名单外）⇒ `30` / `90` 会**如实出声**，⛔ 不拿 `ctx.LastTarget`（代词槽）/ `LastTargets[0]` 顶。
        //     接线办法 → `报告/B99_T2.md`「宿主接线指令」。
        //  📊 **可达性（`cards_engine.json` v44 现读）**：`unitConditions[].countSet` 的 `ta` 分布 =
        //     `0:25 · 10:37 · 50:2 · 60:10 · 160:21 · 162:4`，其余 343 行该格没落 ⇒ **这四档今天 0 行**（休眠件，
        //     铁律 11：照样建、⛔ 不按「没数据」删掉）。`abilityLevelCriteria` 里确有 `20:8 · 30:8 · 90:7`，但那一列是
        //     **targetCriteria**（走 `TargetCriteriaEval.Matches`，**不走**本方法）⇒ 别把那 23 行算成计数缺口。
        // ==========================================================================================

        /// <summary>`CountSet` 的尾部四条档位（`20` / `30` / `90` / `52`）。返回 `null` 且 <paramref name="gap"/> 为
        /// `null` = **不归本方法管**（调用方写通用「本版未实现」文案）。判据全文见上面那段注释。</summary>
        static int? CountSetT2(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond, int countOption,
                               UnitState actor, UnitState target, bool targetKnown,
                               System.Collections.Generic.List<UnitState> cands,
                               System.Collections.Generic.List<int> candSides, ref string gap)
        {
            int ta = set.TargetsAffected;

            // ---- `20 self`：池 = [thisCard]，不筛不裁（`GetTargets.c:184-191`）----
            if (ta == 20)
            {
                // 候选就是 thisCard（= 本文件的 `actor`），阵营恒与 `owner` 同侧 ⇒ `countOption == 10` 时权重 −1。
                if (candSides != null && actor != null) { cands.Add(actor); candSides.Add(owner); }
                return Weight(owner, owner, countOption);
            }

            // ---- `30 target` / `90 targetIfMeetsCriteria`：池 = [targetCard]，30 不筛、90 落常规筛 ----
            if (ta == 30 || ta == 90)
            {
                if (!targetKnown)
                {
                    gap = $"`targetsAffected={ta}`（{(ta == 30 ? "target" : "targetIfMeetsCriteria")}）的池 = `[targetCard]`，"
                        + "但本路径**没有把 `targetCard` 传进来**（`target` / `targetKnown` 两个形参）"
                        + " —— 原版这一跳解引用 `targetCard`，⛔ 我们不拿代词槽兜底 ⇒ 判不了";
                    return null;
                }
                if (target == null) return 0;                  // 原版：null ⇒ 池空（⛔ 不以「自己」兜底）
                int side = ctx != null ? SideOfUnit(ctx, target) : -1;
                if (ta == 30)
                {
                    // `30` 建完池就 return ⇒ 不筛。唯一要判的是「怎么称重」：`countOption == 10` 要阵营。
                    if (countOption == 10 && side < 0)
                    {
                        gap = "`targetsAffected=30` 的 `targetCard` 不在任一方棋盘上 ⇒ 同方 −1 / 异方 +1 的阵营判不了";
                        return null;
                    }
                    if (candSides != null && side >= 0) { cands.Add(target); candSides.Add(side); }
                    return Weight(owner, side < 0 ? owner : side, countOption);
                }
                // `90`：身份相符之后再落常规筛（`Matches` 的 `case 90` 已按 `:193-201` 实现这一跳）。
                bool? pass = TargetCriteriaEval.Matches(ctx, owner, set, target, null, actor, target, out string why90);
                if (pass == null) { gap = why90; return null; }
                if (!pass.Value) return 0;
                var v90 = ConditionEval.MeetsUnit(ctx, target, cond, false);
                if (v90 == null) { gap = $"cond={cond} 在 `ta=90` 的 targetCard 身上判不了"; return null; }
                if (!v90.Value) return 0;
                if (countOption == 10 && side < 0)
                {
                    gap = "`targetsAffected=90` 的 `targetCard` 不在任一方棋盘上 ⇒ 同方 −1 / 异方 +1 的阵营判不了";
                    return null;
                }
                if (candSides != null && side >= 0) { cands.Add(target); candSides.Add(side); }
                return Weight(owner, side < 0 ? owner : side, countOption);
            }

            // ---- `52 inHandAndPlayFiltered`：手牌池 ∪ 场上池（同一份 criteria 两半都要过）----
            if (ta == 52)
            {
                if (ctx == null) { gap = "没有对局上下文（`ctx == null`）"; return null; }
                if (SideEmpty(set)) return 0;                  // 两个阵营闸都关 ⇒ 原版两半都是空池
                string hg;
                if (!CardPoolSetSupported(set, ta, out hg)) { gap = hg; return null; }
                // 手牌那一半：与 `ta == 50` 同一条口径（`CountCardPool` 只吃手牌，不并牌库）。
                int? cards = CountCardPool(ctx, owner, set, cond, countOption, 50, actor, cands, candSides, ref gap);
                if (cards == null) return null;
                // 场上那一半：与 ①（`ta == 10`）同一套判据 —— 见 `CountUnitsOnBoardForCount` 的 doc。
                int? units = CountUnitsOnBoardForCount(ctx, owner, set, cond, countOption, actor, cands, candSides, ref gap);
                if (units == null) return null;
                return cards.Value + units.Value;
            }

            return null;   // 其余档不归这里管（调用方写通用文案）
        }

        /// <summary>`targetsAffected = 52` 的**场上那一半** —— 判据 `AbilityLogic__GetTargets.c:930-948`：
        /// 手牌池建完再 `GetUnitsInPlayList(…, clearList = 0, …)` **追加**场上单位。
        ///
        /// <para>逐候选口径与 `CountSet` ① 支（`ta == 10 || ta == 55`）**同一套**（`BoardCandidatePasses`
        /// ＋ `ConditionEval` ＋ 同一份 `Weight`）。⚠️ 那边**没有**抽成共用方法，是因为账里 `CountEval.cs:495`
        /// 这类 A 档落点按行号引用本文件 ⇒ 在 ① 里增删行会让下游落点整体漂移（铁律 18·b）。
        /// ⇒ 这里是一份**同义复制**：**改一处必须同时改另一处**；它⛔ 不是「第二条判据」
        /// （判据本体仍是 `TargetCriteriaEval.Matches` + `ConditionEval`）。</para>
        /// </summary>
        static int? CountUnitsOnBoardForCount(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond,
                                              int countOption, UnitState actor,
                                              System.Collections.Generic.List<UnitState> cands,
                                              System.Collections.Generic.List<int> candSides, ref string gap)
        {
            if (ctx == null) { gap = "没有对局上下文（`ctx == null`）"; return null; }
            int count = 0;
            for (int p = 0; p < 2; p++)
            {
                if (!SideMatches(owner, p, set)) continue;
                var ps = ctx.Players[p];
                if (ps == null) continue;
                for (int s = 0; s < ps.Board.Length; s++)
                {
                    var u = ps.Board[s];
                    bool? pass = BoardCandidatePasses(ctx, owner, set, u, actor, out string bWhy);
                    if (pass == null) { gap = bWhy; return null; }
                    if (!pass.Value) continue;
                    var v = ConditionEval.MeetsUnit(ctx, u, cond, false);
                    if (v == null) { gap = $"cond={cond} 在候选身上判不了"; return null; }
                    if (!v.Value) continue;
                    if (candSides != null) { candSides.Add(p); cands.Add(u); count += 1; }
                    else count += Weight(owner, p, countOption);
                }
            }
            return count;
        }
    }
}
