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
//   · `countOptions`（`CountOptions`）：`0 AllTargets` = 数命中个数；`10 EnemiesMinusAllies` = ？
//     🔴 **本版没有找到 `countOptions` 的消费点**（全量反编译里 `BoardCriteria` 只有 `.ctor` /
//     `DeepCopy` 两个方法体；`AbilityLogic__GetTargets.c` / `RawCardScript__GetTargeting*.c` 都不读 0x20）
//     ⇒ **`10` 不实现、返回 null**（⛔ 不拿 `AllTargets` 顶替）。
//   · `triggerIf`（`StatFilterOptions`）：`0 NotApplicable` / `1 EqualTo` / `2 EqualOrHigher` /
//     `3 EqualOrLower` 四档；`4-11`（`…ToThisUnitAttack` / `RandomChance` / `…Alt`）不是「跟一个常数比」
//     ⇒ 未实现、返回 null。
//   · `playerTurn`（`PlayersAffected` 0 Both / 10 Self / 20 Enemy）：**本版没有消费点可对齐**
//     （同上：找不到反编译读点）⇒ 见 `SideMatches` 的注释：它**不参与**本版的取数。
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
            // `countOptions`：只实现 `0 AllTargets`。`-1` = 没落（未知）；`10` = 未实现（见文件头 ③）。
            if (spec.CountOption != 0)
            {
                gap = $"countOptions={spec.CountOption} 本版未实现（只实现 0 AllTargets）";
                return null;
            }

            var set = spec.CountSet;
            string why = SetGap(set);
            if (why != null) { gap = why; return null; }

            int count;
            if (CountSet(ctx, owner, set, spec.Condition, actor, out count, out why) == null)
            {
                gap = why ?? "集合/条件判不了";
                return null;
            }

            bool? r = Compare(count, spec.StatOption, spec.ReferenceValue);
            if (r == null) gap = $"比较档 triggerIf={spec.StatOption} 本版未实现（只实现 1/2/3）";
            return r;
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

        /// <summary>数一数；返回 `null` = 这个集合/条件本版判不了。<paramref name="count"/> 供诊断。</summary>
        static int? CountSet(BattleContext ctx, int owner, CardDef.CountSetSpec set, int cond,
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
                        if (v.Value) count++;
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
                    if (v.Value) count++;
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
        /// <para>⚠️ **`playerTurn` 不在这里用**：它是 `BoardCriteria` 那一层的字段（0x10），
        /// 本版**没找到它的消费点**（全量反编译里 `BoardCriteria` 只有 `.ctor`/`DeepCopy`）⇒
        /// 不拿它当「取哪一边」猜。池内 6 条 `count` 行的 `playerTurn` 只有 0 与 10 两个值，
        /// 且两个阵营闸都恰好只开一个 ⇒ 今天不影响任何读数。</para></summary>
        static bool SideEmpty(CardDef.CountSetSpec set)
        {
            return set.FriendlyUnits == 0 && set.EnemyUnits == 0;
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
