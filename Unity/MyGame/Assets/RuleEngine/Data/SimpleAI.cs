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

        /// <summary>该出哪张手牌（返回手牌索引，-1 = 不出）。挑**付得起的最贵**的那张</summary>
        public static int NextCardToPlay(BattleContext ctx)
        {
            if (ctx.IsOver) return -1;
            var p = ctx.ActivePlayer;
            if (!p.HasFreeSlot()) return -1;

            int best = -1, bestCost = -1;
            for (int i = 0; i < p.Hand.Count; i++)
            {
                var c = p.Hand[i];
                if (!c.IsUnit || c.Cost > p.Energy) continue;
                if (c.Cost > bestCost) { bestCost = c.Cost; best = i; }
            }
            return best;
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
                    int atk = RuleCore.FieldAttack(ctx, me, u, UseRanged(u));
                    if (spec.Amount < atk) continue;                       // 平A更划算

                    int t = PickAbilityTarget(ctx, me, spec.Amount);
                    if (t < 0) continue;                                   // 对面没部队可打，放了也白放
                    if (RuleCore.CanUseAbility(ctx, me, s, t) != RuleCodes.OK) continue;

                    // 几个都能放时挑**伤害高的**那个（同分按槽号，定死不掷骰）
                    if (spec.Amount > dmgAmount) { dmgAmount = spec.Amount; dmgSlot = s; dmgTarget = t; }
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
                    if (target.IsWarlord) score = 400 + dealt;                     // 打督军推进胜利
                    else if (dealt >= target.Health) score = 1000 + dealt;         // 能秒杀最优
                    else score = 100 + dealt - target.Attack * 2;                  // 否则看净收益（反击要还的）

                    if (score > bestScore ||
                        (score == bestScore && u.Card.Cost < bestAtkCost))
                    {
                        bestScore = score;
                        bestAtkCost = u.Card.Cost;
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
