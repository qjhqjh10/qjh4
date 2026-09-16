// SimpleAI.cs — 简单对手（贪心）
//
// 不是「AI 设计」，目标是**能对打**：验证规则跑得通、玩家有事可做。
// 策略很直白：能出牌就出最贵的；能放技能就放；能攻击就挑收益最高的一刀。
//
// 拆成「下一步做什么」而不是一口气打完，是为了让表现层能**一步一步播**
// （每步之间加个小延时，玩家才看得清发生了什么）。
using System.Collections.Generic;

namespace RuleEngine
{
    public static class SimpleAI
    {
        /// <summary>
        /// 这个单位这一刀用近战还是远程：**远程攻击力更高就用远程**。
        /// ⚠️ 和 `BattleDriver.UseRanged` 是**同一条规则**，两处必须一致 ——
        ///    各写一份的话，AI 算出来的刀和画面上播出来的刀会对不上。
        /// </summary>
        public static bool UseRanged(UnitState u)
        {
            return u != null && u.RangedAttack > u.Attack;
        }

        /// <summary>
        /// 这一手出什么牌、落哪一格。**单位卡和战术卡都算**，挑「付得起的、最贵的」那一张。
        ///
        /// ⚠️ **2026-09-12 之前这里只考虑单位卡** —— 战术卡一张都不出。
        ///    后果不是「AI 有点笨」，而是**整条战术卡线在实战里根本不会触发**：
        ///    引擎自检里那些战术卡用例全绿，真打起来对手（和自己自动结束回合的判断）
        ///    却从来没碰过它们。做两个阵营的检查时才发现。
        ///
        /// 落点由**引擎**说了算（`RuleCore.CanPlayCard`）：
        ///   · 单位卡 → 第一个空格
        ///   · 战术卡 → 挨个格位试，`CanPlayTactic` 认哪个就是哪个（不需要目标的战术卡随便落哪都算数）
        /// 所以这里**不重写一遍合法性判断**（两处判据迟早不一致）。
        /// </summary>
        /// <returns>有没有可出的牌</returns>
        public static bool NextPlay(BattleContext ctx, out int handIdx, out int slot)
        {
            handIdx = -1; slot = -1;
            if (ctx.IsOver) return false;
            int me = ctx.Active;
            var p = ctx.Players[me];

            int bestCost = -1;
            for (int i = 0; i < p.Hand.Count; i++)
            {
                var c = p.Hand[i];
                int co = RuleCore.CostOf(ctx, me, c);
                if (co > p.Energy) continue;
                if (co <= bestCost) continue;              // 已经找到更贵的了，这张不必再看

                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    if (RuleCore.CanPlayCard(ctx, me, i, s) != RuleCodes.OK) continue;
                    bestCost = co; handIdx = i; slot = s;
                    break;
                }
            }
            return handIdx >= 0;
        }

        /// <summary>该出哪张手牌（返回手牌索引，-1 = 不出）。
        /// 判据就是 <see cref="NextPlay"/>（**含战术卡**）—— 只留个索引给「还有没有事可做」那类判断用。</summary>
        public static int NextCardToPlay(BattleContext ctx)
        {
            int i, s;
            return NextPlay(ctx, out i, out s) ? i : -1;
        }

        /// <summary>挑一个空格部署</summary>
        public static int FirstFreeSlot(PlayerState p)
        {
            for (int s = 0; s < BoardSpec.Size; s++)
                if (BoardSpec.IsDeployable(s) && p.Board[s] == null) return s;
            return -1;
        }

        // ==================================================================
        //  主动技能（2026-09-12）
        // ==================================================================

        /// <summary>
        /// 该放技能吗。返回 false = 不放。
        ///
        /// 两条规则，各有理由：
        ///   ① **治疗类**：督军掉了血就放 —— 放技能花的是这次行动，不放在这个回合就没了
        ///   ② **伤害类**：**技能伤害 ≥ 这个单位的攻击力**就放。技能**不吃反击**，
        ///      打同样的伤害还不用挨还手，没道理去平A。
        ///      （`Ironclad` 2 攻 / 2 伤技能正好卡在这条线上；0 攻单位更是非它不可）
        ///
        /// 判据一律问引擎（`RuleCore.CanUseAbility`），这儿不复制一份目标规则。
        /// </summary>
        public static bool NextAbility(BattleContext ctx, out int slot, out int targetSlot)
        {
            slot = -1; targetSlot = -1;
            if (ctx.IsOver) return false;

            int me = ctx.Active;
            var ps = ctx.Players[me];

            int healSlot = -1;                       // 要治疗的
            int dmgSlot = -1, dmgTarget = -1, dmgAmount = -1;

            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                if (u == null || u.Exhausted || u.IsStunned || !u.HasAbility) continue;

                var spec = u.Ability;

                if (spec.IsHeal)
                {
                    var w = ps.Warlord;
                    if (spec.Target != EffectTargets.OwnWarlord) continue;
                    if (w == null || w.Health >= w.MaxHealth) continue;    // 满血就别浪费行动
                    if (RuleCore.CanUseAbility(ctx, me, s) != RuleCodes.OK) continue;
                    if (healSlot < 0) healSlot = s;
                    continue;
                }

                if (spec.IsDamage)
                {
                    bool needsPick = EffectTargets.NeedsPick(spec.Target);   // 要选目标的目标只能是部队
                    int t = -1;
                    if (needsPick)
                    {
                        t = PickAbilityTarget(ctx, me, spec.Amount);
                        if (t < 0) continue;                               // 对面没部队可打，放了也白放
                    }
                    // ⚠️ **打督军脸的技能不走 `PickAbilityTarget`**（2026-09-17 修）——
                    //    那个函数只在**敌方部队**里挑，对面场上空着就返回 -1，
                    //    于是这类技能原来会被上面那句 `continue` 掉、**永远放不出来**。
                    //    和 `NextAttack` 里「0 攻单位」那条是同一族坑：**判据写得比实际窄**。

                    int atk = RuleCore.FieldAttack(ctx, me, u, UseRanged(u));
                    // 平A更划算就不放技能 —— **除非这一下能斩杀**（那就不算收益，直接去死）
                    bool killsWarlord = spec.Target == EffectTargets.EnemyWarlord && CanLethal(ctx);
                    if (!killsWarlord && spec.Amount < atk) continue;

                    if (RuleCore.CanUseAbility(ctx, me, s, needsPick ? t : -1) != RuleCodes.OK) continue;

                    // 几个都能放时挑**值大的**那个（同分按槽号，定死不掷骰）
                    int worth = killsWarlord ? 1000000 + spec.Amount : spec.Amount;
                    if (worth > dmgAmount) { dmgAmount = worth; dmgSlot = s; dmgTarget = t; }
                }
            }

            if (dmgSlot >= 0) { slot = dmgSlot; targetSlot = dmgTarget; return true; }
            if (healSlot >= 0) { slot = healSlot; return true; }
            return false;
        }

        /// <summary>
        /// 技能要选目标时挑谁：**能一发打死的优先**，否则取槽号最小的活部队。
        /// 定死的规则，不掷骰 —— 种子只该影响洗牌（一局必须可复现）。
        /// </summary>
        static int PickAbilityTarget(BattleContext ctx, int me, int amount)
        {
            var b = ctx.Players[1 - me].Board;
            int fallback = -1;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var t = b[s];
                if (t == null || t.IsWarlord || !t.IsAlive) continue;
                if (t.Has(KeywordTable.Stealth)) continue;      // 潜行单位不能被任何方式选中
                int dealt = t.Armor > 0 ? System.Math.Max(1, amount - t.Armor) : amount;
                if (dealt >= t.Health) return s;
                if (fallback < 0) fallback = s;
            }
            return fallback;
        }

        // ==================================================================
        //  斩杀线（2026-09-17）
        // ==================================================================
        //
        // 🔴 **为什么要有它**：原来的评分里「打死一个部队」记 **1000** 分、
        //    「打督军脸」只记 **400** 分 ⇒ **能一击斩杀的时候，AI 会先去清一个部队**
        //    而不是赢下这局。这是「只贪心」最刺眼的一条表现，也不是「菜」，是**直接送掉胜局**。

        /// <summary>
        /// 这一回合**最多能对对面督军造成多少伤害**。
        ///
        /// 把「还能动的单位」逐个数一遍，每个单位取它**能打出的最大那一份**：
        ///   · **攻击**：近战 / 远程两路各问一次引擎（`IsValidTarget`），取伤害高的那一路
        ///     —— 一个单位一回合只出一刀，**不能两路相加**；
        ///   · **技能**：只算「打 `EnemyWarlord`」那类伤害技能（要选目标的目标**只能是部队**，
        ///     见 `RuleCore.CanUseAbility` 的注释）。⚠️ 技能和攻击**二选一**（放了技能这个单位就疲劳），
        ///     所以取两者里大的那个，**也不能相加**。
        ///
        /// ⚠️ 伤害一律过 `RuleCore.DamageAfterReduction`（督军也可能有护甲）。
        /// ⚠️ **带盾的督军会被严重低估** —— `DamageAfterReduction` 对带盾目标一律返回 0
        ///    （盾会碎，但我们没有「第几次命中」的概念）。`Shield` 在督军身上极罕见，先如实记着。
        /// ⚠️ **定死的规则，不掷骰** —— 种子只该影响洗牌（一局必须可复现）。
        /// </summary>
        public static int DamageToFoeWarlord(BattleContext ctx)
        {
            if (ctx == null || ctx.IsOver) return 0;
            int me = ctx.Active, foe = 1 - me;
            var myBoard = ctx.Players[me].Board;
            var foeW = ctx.Players[foe].Warlord;
            if (foeW == null) return 0;

            int total = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = myBoard[s];
                if (u == null || u.Exhausted || u.IsStunned) continue;

                int byAttack = 0;
                for (int ranged = 0; ranged <= 1; ranged++)
                {
                    int atk = RuleCore.FieldAttack(ctx, me, u, ranged == 1);
                    if (atk <= 0) continue;
                    if (RuleCore.IsValidTarget(ctx, me, s, foe, BoardSpec.WarlordSlot,
                                               ranged == 1) != RuleCodes.OK) continue;
                    int dealt = RuleCore.DamageAfterReduction(foeW, atk);
                    if (dealt > byAttack) byAttack = dealt;
                }

                int byAbility = 0;
                var spec = u.Ability;
                if (spec != null && spec.IsDamage && spec.Target == EffectTargets.EnemyWarlord
                    && RuleCore.CanUseAbility(ctx, me, s) == RuleCodes.OK)
                    byAbility = RuleCore.DamageAfterReduction(foeW, spec.Amount);

                total += System.Math.Max(byAttack, byAbility);
            }
            return total;
        }

        /// <summary>这一回合够不够把对面督军打死（= 斩杀线到了）。</summary>
        public static bool CanLethal(BattleContext ctx)
        {
            if (ctx == null || ctx.IsOver) return false;
            var foeW = ctx.Players[1 - ctx.Active].Warlord;
            return foeW != null && foeW.Health > 0
                   && DamageToFoeWarlord(ctx) >= foeW.Health;
        }

        // ==================================================================
        //  攻击
        // ==================================================================

        /// <summary>
        /// 该打哪一刀。返回 false = 没有可打的。
        /// 评分：能秒杀 &gt; 打督军 &gt; 造成伤害多；同分优先**更弱的攻击者**
        /// （先把便宜单位用掉，好单位留着）。
        /// </summary>
        public static bool NextAttack(BattleContext ctx, out int atkSlot, out int targetP, out int targetSlot,
                                      out bool ranged)
        {
            atkSlot = -1; targetP = -1; targetSlot = -1; ranged = false;
            if (ctx.IsOver) return false;

            int me = ctx.Active;
            int foe = 1 - me;
            var myBoard = ctx.Players[me].Board;
            var foeBoard = ctx.Players[foe].Board;

            // 一次算好：这一回合有没有斩杀线（每个候选都重算的话是 O(n²)，而且结论一样）
            bool lethal = CanLethal(ctx);

            int bestScore = int.MinValue;
            int bestAtkCost = int.MaxValue;

            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = myBoard[s];
                if (u == null || u.Exhausted || u.IsStunned) continue;

                // ⚠️ 0 攻单位**打不了任何一刀**（DeclareAttack 会返回 ErrNoAttack）。
                //    不在这儿挡掉的话，一个 0 攻单位能凭「打督军加分」被选成最优刀，
                //    然后 DeclareAttack 报错、攻击循环直接断掉（`Reef Guard` / `Ballista` 踩过）。
                //    `Ballista` 近战 0、远程 4 —— 所以还得按**实际用的那套数值**取攻击力。
                bool useRanged = UseRanged(u);
                int atk = RuleCore.FieldAttack(ctx, me, u, useRanged);
                if (atk <= 0) continue;

                for (int t = 0; t < BoardSpec.Size; t++)
                {
                    var target = foeBoard[t];
                    if (target == null) continue;

                    // 只问引擎「这一刀合不合法」—— 别在这儿复制一套目标规则
                    if (RuleCore.IsValidTarget(ctx, me, s, foe, t, useRanged) != RuleCodes.OK) continue;

                    int dealt = atk;
                    if (target.Armor > 0) dealt = System.Math.Max(1, dealt - target.Armor);

                    int score;
                    if (target.IsWarlord)
                        // 🔴 **能斩杀时打脸是唯一该做的事** —— 给它一个压倒性的分，
                        //    否则会被下面那条「打死部队 = 1000」压过去（原来就是这么送掉胜局的）
                        score = lethal ? 1000000 + dealt : 400 + dealt;
                    else if (lethal)
                        // 这一回合赢定了 —— 部队一个都别管（清了也是白清）
                        score = -1000 + dealt;
                    else if (dealt >= target.Health)
                        // 打死：值钱的不是「它掉了几点血」，是**它以后打不了我了** ⇒ 按它的攻击力加权
                        score = 1000 + dealt + target.Attack * 3;
                    else
                        score = 100 + dealt - target.Attack * 2;                   // 否则看净收益（反击要还的）

                    if (score > bestScore ||
                        (score == bestScore && RuleCore.CostOf(ctx, me, u.Card) < bestAtkCost))
                    {
                        bestScore = score;
                        bestAtkCost = RuleCore.CostOf(ctx, me, u.Card);
                        atkSlot = s; targetP = foe; targetSlot = t; ranged = useRanged;
                    }
                }
            }
            return atkSlot >= 0;
        }

        /// <summary>
        /// 调试用：一口气把这回合打完（自检里跑完整对局用它；表现层要分部播的话别用）。
        /// 顺序：出牌 → **技能** → 攻击。
        /// ⚠️ 技能必须排在攻击前面：放了技能这个单位就疲劳了，再想攻击也打不了。
        /// </summary>
        public static void PlayTurn(BattleContext ctx)
        {
            for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
            {
                int card = NextCardToPlay(ctx);
                if (card < 0) break;
                int slot = FirstFreeSlot(ctx.ActivePlayer);
                if (slot < 0) break;
                if (RuleCore.PlayCard(ctx, ctx.Active, card, slot) != RuleCodes.OK) break;
            }

            for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
            {
                int s, ts;
                if (!NextAbility(ctx, out s, out ts)) break;
                if (RuleCore.UseAbility(ctx, ctx.Active, s, ts) != RuleCodes.OK) break;
            }

            for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
            {
                int a, tp, ts;
                bool ranged;
                if (!NextAttack(ctx, out a, out tp, out ts, out ranged)) break;
                if (RuleCore.DeclareAttack(ctx, ctx.Active, a, tp, ts, ranged) != RuleCodes.OK) break;
            }
        }
    }
}
