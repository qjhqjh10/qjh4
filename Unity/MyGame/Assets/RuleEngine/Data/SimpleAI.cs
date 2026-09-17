// SimpleAI.cs — 对手 AI
//
// 🔴 **2026-09-17 按原版 `AI` 类重写**（正本 `资料/AI_原版反编译_0917.md`，那里有公式/常量/数据类/死代码清单）。
//
// 为什么重写：老版本是「能出就出**最贵**的牌 / 能放技能就放 / 能打就挑收益最高的一刀」三条贪心 ——
// 能对打，但它不是原版的行为。原版 `AI` 类那 41 个方法体 2026-09-17 全部反编译出来了，形状是这样的：
//
//     PlayTurn(manager, mode)                ← 一次调用**只做一步**（循环在 BattleManager 的协程里）
//       ① GetAvailableActions(...)          ← 动作表：出牌 / 近战 / 远程 / 技能 / waystone / **endTurn 基准**
//       ② 逐条 GetActionScore(...)          ← 打分族（见下）
//       ③ TweakAvailableActions(...)        ← **唯一随机源、唯一难度旋钮**：把最低分的几条随机丢掉
//       ④ SelectBestAction(...)             ← 严格 `>`，平手保留先出现的；最佳是 endTurn 就收手
//       ⑤ ExecuteAction(...)
//
// 打分族的量级（照 `GameAssembly.dll` 浮点池读出来的真值）：
//   · 打死督军 **1e6**（`ScoreFromDamagingBuffedUnit`）· 合击够斩杀 **+10000**（`GetActionScore`）
//   · 打督军非致命 = `伤害 × (剩余血 > 15 ? 1.2 : 1.8)`
//   · 小兵：打死 = `卡价值 × 1.0`（带 survivor 0.8）；没打死 = `卡价值 × 0.6 × 实际伤害 / 血上限`
//   · **卡价值 = 费用 × 2 + 1**（`GetCardReferenceValueInPlay`）+ 关键词分值 × 伤势/眩晕衰减
//   · 攻击 = `伤害分 − 反击分`，被反击后还活着 `+0.1`
//   · `endTurn` 恒为 **0** ⇒ **「分数 ≤ 0 的动作就不做」** —— 原版的「留牌」其实就写在这条上
//     （`ScoreFromKeepingTheCard` 那版构建里 0 调用点，见正本 §六）
//
// 🔴 **权重那层的口径**：原版的分数挂在卡的 `ScoringCriteria` **资产**上，**那些数据在服务端、本地没有**。
//    所以我们的做法是：**结构照原版、criteria 从我们自己解析出来的 `EffectOp` 推**（`deal N` → 伤害分…
//    见 `ScoreOp`），**这一层的系数是我们配的**（代码里逐处标明）。这不是「查不到就不做」，
//    是「原版的值真拿不到，我们把结构照搬、把系数写在明处」。
//
// ⚠️ **两条不能改的**：
//   ① 随机**只用 `ctx.AiRng`**（种子派生，一局必须可复现）——绝不用 `UnityEngine.Random`；
//   ② 「近战还是远程」的**兼容判据**仍然只此一处（`UseRanged`），别在别处另写一份。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 动作种类。**取值照原版 `PlayerActions`**（`dump.cs:45579`）：
    /// `playCard=0 / attackMelee=1 / attackRanged=2 / useHeroPower=3 / endTurn=4 / activeAbility=5 / clickWaystone=6`。
    ///
    /// ⚠️ 原版把**主动技能 / 誓约能力 / 替代行动**（`duty` / `pray` / `ferocity` / `agenda`）**都归在
    ///    `activeAbility`(5) 一类**里（判据是 `CardScript.hasActiveAbility`）。我们引擎里这三条是
    ///    三条不同的入口（`RuleCore.CanUseAbility` / `CanUseOathAbility` / `CanUseAlternative`），
    ///    所以动作上带一个 <see cref="AiAction.AltKeyword"/> 区分，**类型值照旧用 5**。
    ///
    /// ⚠️ `useHeroPower`(3) 原版**从不产出**（`GetAvailableActions` 里没有这一支）；我们同样不产出。
    /// ⚠️ `clickWaystone`(6) **先留位不做** —— 引擎那半（主动「收集」灵魂石）还没实现（待办第 8 行）。
    /// </summary>
    public enum AiActionKind
    {
        PlayCard = 0,
        AttackMelee = 1,
        AttackRanged = 2,
        EndTurn = 4,
        ActiveAbility = 5,
    }

    /// <summary>
    /// 难度。**取值照原版 `DeckDifficultyLevel`**（`SuperEasy=0 / Easy=5 / Normal=10 / Hard=15`）。
    /// 它只调**一个**东西：`TweakAvailableActions` 的 `minSkips / maxSkips / skipChance`
    /// —— 也就是「把 AI 本来想做的事，按最低分往下砍掉多少」。**原版没有搜索深度这回事。**
    /// </summary>
    public enum AiDifficulty { SuperEasy = 0, Easy = 5, Normal = 10, Hard = 15 }

    /// <summary>
    /// 一条可用动作（照原版 `AvailableAction.cs`：`actionType` / `actingCard` / `targetCard` /
    /// `actionScore` / `manaCost`）。我们这边用**下标**代替对象引用（`HandIdx` / `Slot` / `TargetSlot`）——
    /// 引擎的状态是裸数组（`PlayerState.Board`），存引用会在棋盘变动后指向旧对象。
    /// </summary>
    public class AiAction
    {
        public AiActionKind Kind;
        /// <summary>出牌时的**手牌下标**</summary>
        public int HandIdx = -1;
        /// <summary>行动单位所在的格位（出牌时 = **落点**格位）</summary>
        public int Slot = -1;
        /// <summary>目标方（0/1）。`-1` = 这个动作没有目标</summary>
        public int TargetP = -1;
        /// <summary>目标格位</summary>
        public int TargetSlot = -1;
        /// <summary>这一刀用近战还是远程（只有攻击动作有意义）</summary>
        public bool Ranged;
        /// <summary>`ActiveAbility` 的子类：`null` = 普通主动技能 · `"oath"` = 誓约能力 ·
        /// 其余 = 替代行动的关键词（`duty` / `pray` / `ferocity` / `agenda`）</summary>
        public string AltKeyword;
        /// <summary>原版把它写在 `AvailableAction.actionScore`（`+0x28`）</summary>
        public float Score;
        /// <summary>花多少能量（原版 `+0x2C`）</summary>
        public int ManaCost;

        public override string ToString()
        {
            return Kind + (HandIdx >= 0 ? " hand#" + HandIdx : "")
                 + (Slot >= 0 ? " @" + Slot : "")
                 + (TargetSlot >= 0 ? " →P" + (TargetP + 1) + "@" + TargetSlot : "")
                 + (AltKeyword != null ? " [" + AltKeyword + "]" : "")
                 + " = " + Score.ToString("F2");
        }
    }

    public static class SimpleAI
    {
        // ==================================================================
        //  常量（照原版：`资料/AI_原版反编译_0917.md` §五·一）
        // ==================================================================

        /// <summary>打死督军/英雄 —— 原版 `ScoreFromDamagingBuffedUnit` 的出口值</summary>
        const float WarlordKillScore = 1000000f;
        /// <summary>合击够斩杀 —— 原版 `GetActionScore` 加的分</summary>
        const float CombinedKillBonus = 10000f;
        /// <summary>被反击之后还活着 —— 原版 `ScoreFromBuffedAttack` 的小奖励</summary>
        const float SurvivalBonus = 0.1f;
        /// <summary>打督军非致命时的系数：剩余血 &gt; 15 用 1.2，否则 1.8（原版同）</summary>
        const float HeroDamageLow = 1.2f, HeroDamageHigh = 1.8f;
        /// <summary>小兵没被打死时的系数（原版 `0.6 × 伤害 / 血上限`）</summary>
        const float MinionPartial = 0.6f;
        /// <summary>带 `survivor` 的小兵被打死时打折（原版 0.8）</summary>
        const float SurvivorDiscount = 0.8f;
        /// <summary>卡价值 = 费用 × 2 + 1（原版 `GetCardReferenceValueInPlay`）</summary>
        public static float RefValue(CardDef c) { return c == null ? 0f : c.Cost * 2f + 1f; }

        // ==================================================================
        //  ① 动作表（原版 `BattleManager.GetAvailableActions`）
        // ==================================================================

        /// <summary>
        /// 枚举当前行动方**所有能做的动作**。
        ///
        /// 照原版那个顺序（出处见正本 §三）：手牌 → 场上单位的近战/远程 → 技能 → **最后追加 endTurn**。
        /// 每一条都是「引擎说合法」的（一律问 `RuleCore.CanPlayCard` / `IsValidTarget` /
        /// `CanUseAbility` / `CanUseOathAbility` / `AvailableAlternative`）——
        /// **不在这儿复制第二份合法性判据**（两处写迟早不一致，工程铁律）。
        ///
        /// ⚠️ 近战和远程**各列一条**（原版就是这样：`attackMelee` / `attackRanged` 是两条动作，
        ///    各自打分再挑高的）。老版本那句「远程攻击力更高就用远程」是另一种近似。
        /// </summary>
        public static List<AiAction> EnumerateActions(BattleContext ctx)
        {
            var list = new List<AiAction>();
            if (ctx == null || ctx.IsOver) return list;

            int me = ctx.Active, foe = 1 - me;
            var mine = ctx.Players[me];
            var foeBoard = ctx.Players[foe].Board;

            // ---- 手牌 ----
            for (int i = 0; i < mine.Hand.Count; i++)
            {
                var card = mine.Hand[i];
                if (card == null) continue;
                int cost = RuleCore.CostOf(ctx, me, card);
                if (cost > mine.Energy) continue;                       // 付不起：原版在 CanPlayCard 里挡
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    if (RuleCore.CanPlayCard(ctx, me, i, s) != RuleCodes.OK) continue;
                    list.Add(new AiAction { Kind = AiActionKind.PlayCard, HandIdx = i, Slot = s, ManaCost = cost });
                }
            }

            // ---- 场上单位：近战 / 远程各一条 ----
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = mine.Board[s];
                if (u == null || !u.IsAlive || u.Exhausted || u.IsStunned) continue;

                for (int t = 0; t < BoardSpec.Size; t++)
                {
                    if (foeBoard[t] == null) continue;
                    for (int r = 0; r <= 1; r++)
                    {
                        bool ranged = r == 1;
                        if (RuleCore.IsValidTarget(ctx, me, s, foe, t, ranged) != RuleCodes.OK) continue;
                        list.Add(new AiAction
                        {
                            Kind = ranged ? AiActionKind.AttackRanged : AiActionKind.AttackMelee,
                            Slot = s, TargetP = foe, TargetSlot = t, Ranged = ranged,
                        });
                    }
                }
            }

            // ---- 技能 / 誓约 / 替代行动（原版都算 `activeAbility`）----
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = mine.Board[s];
                if (u == null || !u.IsAlive || u.Exhausted || u.IsStunned) continue;

                // 誓约能力（原版 `CardScript.CanUseOathAbility`；不点目标）
                if (u.Card != null && u.Card.OathOps != null && RuleCore.CanUseOathAbility(ctx, me, s) == RuleCodes.OK)
                    list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s, AltKeyword = "oath" });

                // 替代行动（`duty` / `pray` / `ferocity` / `agenda`；至多一条）
                string alt = RuleCore.AvailableAlternative(ctx, me, s);
                if (alt != null)
                {
                    // 要选目标的那种（`AlternativeTargetSide` 给出目标在哪一方，`-1` = 不用点）：
                    // 每个合法目标一条 —— 判据**问引擎**（`CanPickAlternativeTarget`），
                    // 这边不重写一遍「谁能被选中」。
                    int side = RuleCore.AlternativeTargetSide(ctx, me, s, alt);
                    if (side >= 0)
                    {
                        var tb = ctx.Players[side].Board;
                        for (int t = 0; t < BoardSpec.Size; t++)
                        {
                            if (tb[t] == null) continue;
                            if (!RuleCore.CanPickAlternativeTarget(ctx, me, s, alt, t)) continue;
                            list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s, AltKeyword = alt,
                                                    TargetP = side, TargetSlot = t });
                        }
                    }
                    else
                    {
                        list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s, AltKeyword = alt });
                    }
                }

                // 普通主动技能
                var spec = u.Ability;
                if (spec == null) continue;
                if (EffectTargets.NeedsPick(spec.Target))
                {
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        if (foeBoard[t] == null) continue;
                        if (RuleCore.CanUseAbility(ctx, me, s, t) != RuleCodes.OK) continue;
                        list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s,
                                                TargetP = foe, TargetSlot = t });
                    }
                }
                else if (RuleCore.CanUseAbility(ctx, me, s) == RuleCodes.OK)
                {
                    list.Add(new AiAction { Kind = AiActionKind.ActiveAbility, Slot = s });
                }
            }

            // ---- 兜底：endTurn（原版恒为最后一条、分数恒为 0）----
            list.Add(new AiAction { Kind = AiActionKind.EndTurn });
            return list;
        }

        // ==================================================================
        //  ② 打分（原版 `GetActionScore` 的分派）
        // ==================================================================

        /// <summary>
        /// 一条动作值多少分。分派照原版 `GetActionScore`：
        /// 出牌 → `ScoreFromPlayingCard` · 攻击 → `ScoreFromAttack`（**够合击斩杀再 +10000**）·
        /// 技能 → 技能自己的 criteria · `endTurn` → **0**（⇒ 分 ≤0 的动作就不做）。
        /// </summary>
        public static float ScoreAction(BattleContext ctx, AiAction a)
        {
            if (ctx == null || a == null) return 0f;
            int me = ctx.Active, foe = 1 - me;

            switch (a.Kind)
            {
                case AiActionKind.PlayCard:
                    return ScoreFromPlayingCard(ctx, a);

                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                {
                    float s = ScoreFromAttack(ctx, a);
                    // 合击够斩杀：照原版给一个压倒性的加分（`AI__GetActionScore.c:62`）
                    if (a.TargetSlot >= 0 && a.TargetP == foe)
                    {
                        var tgt = ctx.Players[foe].Board[a.TargetSlot];
                        if (tgt != null && tgt.IsWarlord && CombinedAttackCanKillWarlord(ctx))
                            s += CombinedKillBonus;
                    }
                    return s;
                }

                case AiActionKind.ActiveAbility:
                    return ScoreAbility(ctx, a);

                default:
                    return 0f;      // endTurn 基准
            }
        }

        /// <summary>
        /// 出一张牌值多少分 = **卡本身的价值** + **它写在卡面上的事值多少**。
        /// 原版：`(非法术 ? GetValueInPlay : 0) + ScoreFromPlayingNow`（`AI__ScoreFromPlayingCard.c:21-30`）。
        /// 我们这边：单位/督军卡给「参照价值 + 关键词分值」，效果那半走 <see cref="ScoreOps"/>
        /// （原版那半是从卡的 `ScoringCriteria` 资产读的，**那些值在服务端** —— 见文件头）。
        /// </summary>
        public static float ScoreFromPlayingCard(BattleContext ctx, AiAction a)
        {
            var card = ctx.Players[ctx.Active].Hand[a.HandIdx];
            if (card == null) return 0f;

            float s = 0f;
            if (card.IsUnit) s += RefValue(card) + KeywordValue(card);
            s += ScoreOps(ctx, EffectText.WillRunOps(card), a.Slot);
            return s;
        }

        /// <summary>
        /// 一刀值多少分 = `伤害分 − 反击分`（原版 `ScoreFromBuffedAttack` 的无 buff 版）。
        ///
        /// 反击口径**照引擎**（`RuleCore.DeclareAttack`）：目标用**近战攻击力**反击；
        /// 只有 `LongRange`（远程）与 `Sniper`（远程击杀）免 —— 别退回「远程不吃反击」那个错口径。
        /// </summary>
        public static float ScoreFromAttack(BattleContext ctx, AiAction a)
        {
            int me = ctx.Active, foe = 1 - me;
            var u = ctx.Players[me].Board[a.Slot];
            var target = ctx.Players[foe].Board[a.TargetSlot];
            if (u == null || target == null) return 0f;

            int atk = RuleCore.FieldAttack(ctx, me, u, a.Ranged);
            if (atk <= 0) return 0f;

            float s = ScoreDamaging(target, atk);       // 里面已经按「是不是督军」分了两支

            // 反击：目标用**近战攻**还手（引擎里就是这么结算的）
            bool noCounter = a.Ranged && u.Has(KeywordTable.LongRange);
            bool sniperKill = a.Ranged && u.Has("sniper") && RuleCore.WouldKill(target, atk);
            int counter = target.Attack;
            if (!noCounter && !sniperKill && counter > 0)
            {
                s -= ScoreDamaging(u, counter);
                if (RuleCore.DamageAfterReduction(u, counter) < u.Health) s += SurvivalBonus;
            }
            return s;
        }

        /// <summary>
        /// 技能/誓约/替代行动值多少分（原版 `activeAbility` 那一支：累加它自己的 criteria）。
        /// ⚠️ 我们引擎里技能是**封闭文法**（`EffectSpec`：damage / heal / draw），誓约与替代行动是 op 表
        ///    —— 两条路分别打分，最后都汇到同一个分数域里和攻击比。
        /// </summary>
        public static float ScoreAbility(BattleContext ctx, AiAction a)
        {
            int me = ctx.Active;
            var u = ctx.Players[me].Board[a.Slot];
            if (u == null || u.Card == null) return 0f;

            if (a.AltKeyword == "oath") return ScoreOps(ctx, u.Card.OathOps, a.Slot);
            if (a.AltKeyword != null) return ScoreOps(ctx, u.Card.TriggerOps(a.AltKeyword), a.Slot);

            var spec = u.Ability;
            if (spec == null) return 0f;

            int amount = spec.Amount;
            if (spec.IsDamage)
            {
                if (spec.Target == EffectTargets.EnemyWarlord)
                {
                    var w = ctx.Players[1 - me].Warlord;
                    return w == null ? 0f : ScoreWarlordDamage(w, amount);
                }
                if (spec.Target == EffectTargets.EnemyUnit && a.TargetSlot >= 0)
                {
                    var t = ctx.Players[1 - me].Board[a.TargetSlot];
                    return t == null ? 0f : ScoreDamaging(t, amount);
                }
                if (spec.Target == EffectTargets.Self) return 0f;        // 打自己：不加分
                return 0f;
            }
            if (spec.IsHeal)
            {
                var w = ctx.Players[me].Warlord;
                if (spec.Target == EffectTargets.OwnWarlord && w != null) return ScoreHealing(w, amount);
                if (spec.Target == EffectTargets.Self) return ScoreHealing(u, amount);
                return 0f;
            }
            return ScoreDraw(ctx);      // draw
        }

        // ------------------------------------------------------------------
        //  子打分器
        // ------------------------------------------------------------------

        /// <summary>
        /// **卡在场上的价值**（原版 `GetCardValueInPlay`）：
        /// `费用 × 2 + 1 + 附魔价值`，**受伤**再 `× (0.5 × 当前血/上限 + 0.5)`，**晕/压制**再 `× 0.8`。
        /// ⚠️ 「附魔价值」我们换成**关键词分值**（<see cref="KeywordValue"/>）——
        ///    原版那层是 `ValueOfEnchantments`（按 `CardEffect` 的 `buffType` 算），
        ///    我们引擎里没有「附魔」这个对象，只有关键词 ⇒ **这一层是我们配的**。
        /// </summary>
        public static float ValueInPlay(UnitState u)
        {
            if (u == null) return 0f;
            float v = RefValue(u.Card) + KeywordValue(u.Card);
            if (u.MaxHealth > 0 && u.Health < u.MaxHealth)
                v *= 0.5f * u.Health / u.MaxHealth + 0.5f;
            if (u.IsStunned || u.Has("pindown")) v *= 0.8f;
            return v;
        }

        /// <summary>
        /// 小兵挨这一下的分数（原版 `ScoreFromDamagingBuffedUnit` 的无 buff 版）。
        /// 门禁照原版：`ambush`→0 · `longrange`→0 · `shield`/`dodge`→只给一点点。
        /// ⚠️ **护甲那层照我们引擎改写**：原版是「伤害 ≥ 血 + `bastion`」，我们引擎里护甲已经在
        ///    `RuleCore.DamageAfterReduction` 里减过（且最低 1 点）⇒ 直接用**实际伤害 ≥ 当前血**判死。
        /// </summary>
        public static float ScoreDamaging(UnitState target, int dmg)
        {
            if (target == null || !target.IsAlive) return 0f;
            if (target.Has(KeywordTable.Ambush)) return 0f;         // 原版：伏击单位不吃这一套
            if (target.Has(KeywordTable.LongRange)) return 0f;      // 原版：远程单位那条出口是 0
            if (target.Has(KeywordTable.Shield) || target.Has("dodge"))
                return dmg / 3f + 1f;                               // 原版那个「只给一点」的出口

            int dealt = RuleCore.DamageAfterReduction(target, dmg);
            if (target.IsWarlord) return ScoreWarlordDamage(target, dmg);

            float v = ValueInPlay(target);
            if (dealt >= target.Health)                             // 打死
                return v * (target.Has("survivor") ? SurvivorDiscount : 1f);
            return v * MinionPartial * dealt / System.Math.Max(1, target.MaxHealth);
        }

        /// <summary>
        /// 打**督军**值多少分（原版 `ScoreFromDamagingBuffedUnit` 的英雄那一支）：
        /// 打死 → **1e6**（压倒性）；没打死 → `伤害 × (剩余血 &gt; 15 ? 1.2 : 1.8)`。
        /// </summary>
        public static float ScoreWarlordDamage(UnitState warlord, int dmg)
        {
            if (warlord == null || !warlord.IsAlive) return 0f;
            int dealt = RuleCore.DamageAfterReduction(warlord, dmg);
            if (dealt <= 0) return 0f;
            if (dealt >= warlord.Health) return WarlordKillScore;
            return dealt * (warlord.Health - dealt > 15 ? HeroDamageLow : HeroDamageHigh);
        }

        /// <summary>
        /// 治疗一个单位值多少分。⚠️ **原版的公式没解出来**（`ScoreFromHealingUnit` 里一个浮点字面量都没有，
        /// 说明它用的是运行时量/血量的比例，反编译把浮点算术吞了）⇒ **这里的系数是我们配的**：
        /// 只算**真治得进去的那部分**（`min(治疗量, 已掉的血)`），督军按 1.5 加权（督军倒了就输了）。
        /// </summary>
        public static float ScoreHealing(UnitState u, int amount)
        {
            if (u == null || !u.IsAlive) return 0f;
            int missing = System.Math.Max(0, u.MaxHealth - u.Health);
            int healed = System.Math.Min(amount, missing);
            if (healed <= 0) return 0f;                             // 满血：一点分都不加（原来那条「别浪费行动」照旧成立）
            return healed * (u.IsWarlord ? 1.5f : 1.0f);
        }

        /// <summary>晕一个目标值多少分（原版 `ScoreFromStun`：英雄按近战攻、小兵减半）。</summary>
        public static float ScoreStun(UnitState target)
        {
            if (target == null || !target.IsAlive) return 0f;
            if (target.Has(KeywordTable.CantAttack) || target.Has("unstunnable"))
                return ValueInPlay(target) * 0.2f;
            return target.Attack * (target.IsWarlord ? 1f : 0.5f);
        }

        /// <summary>下毒一个目标值多少分（原版 `ScoreFromPoisoning`）。</summary>
        public static float ScorePoisoning(UnitState target)
        {
            if (target == null || !target.IsAlive) return 0f;
            if (target.IsWarlord || target.Has("poisoned") || target.Has("resistant")) return 0f;
            float f = target.IsStunned ? 0.8f
                    : (target.Has(KeywordTable.CantAttack) ? 0.5f
                       : (target.Health < 3 ? 0.2f : 0.5f));
            return ValueInPlay(target) * f;
        }

        /// <summary>把一个单位弹回手牌值多少分（原版 `ScoreFromReturnToHand`：
        /// `当前费用 − (2×费+1 − 目标价值) × 1.5 − 集结/法典的 2 分`）。</summary>
        public static float ScoreReturnToHand(UnitState target, int cost)
        {
            if (target == null || !target.IsAlive) return 0f;
            float v = (2f * cost + 1f - ValueInPlay(target)) * 1.5f;
            float bonus = (target.Has(KeywordTable.Rally) ? 2f : 0f) + (target.Has(KeywordTable.Codex) ? 2f : 0f);
            return cost - v - bonus;
        }

        /// <summary>抽一张牌值多少分（原版 `ScoreFromDrawingCard`：`手牌上限 − 手牌数 − 1`；牌库空则负分）。</summary>
        public static float ScoreDraw(BattleContext ctx)
        {
            int p = ctx.Active;
            int hand = ctx.Players[p].Hand.Count;
            int deck = ctx.Players[p].Deck.Count;
            if (deck == 0) return -2f - RuleCore.HandMax;           // 原版：牌库空 → 抽牌是**坏事**
            return RuleCore.HandMax - hand - 1;
        }

        /// <summary>
        /// 关键词分值表（原版 `ScoreFromBuff` 的 trait → 分值表，44 行，见正本 §五·三）。
        /// ⚠️ 只列**我们卡池里真有的**那些（其余原版也是 0 分）；名字用我们 `KeywordTable` 的写法。
        /// </summary>
        public static float TraitScore(string keyword)
        {
            switch (keyword)
            {
                case KeywordTable.Shield: return 2.0f;
                case KeywordTable.Vanguard: return 2.0f;
                case KeywordTable.Stealth: return 1.5f;
                case "pack": return 1.5f;
                case "camouflage": return 1.5f;          // 无 const：照 `RuleCore` 的写法用字面量
                case KeywordTable.LongRange: return 1.5f;
                case KeywordTable.Backlash: return 1.0f;
                case KeywordTable.Flying: return 1.0f;
                case "shuriken": return 0.0f;
                case "vulnerable": return -1.0f;
                case "markerlight": return -1.0f;
                case "stun": return -1.5f;
                case "blind": return -1.5f;
                case KeywordTable.CantAttack: return -2.0f;
                default: return 0.0f;                                // 原版：**没列出的就是 0 分**
            }
        }

        /// <summary>一张卡上所有关键词的分值合计（原版 `ValueOfTraits` = `Σ CardTrait._aIScore`）。
        /// ⚠️ 原版那个 `_aIScore` 是**挂在数据资产上**的 ⇒ 这里用 <see cref="TraitScore"/> 那张表，
        /// 数值来自反编译（见正本 §五·三），**表本身是原版的**。</summary>
        public static float KeywordValue(CardDef card)
        {
            if (card == null || card.Keywords == null) return 0f;
            float sum = 0f;
            foreach (var kv in card.Keywords)
                if (kv.Value > 0) sum += TraitScore(kv.Key.ToLowerInvariant());
            return sum;
        }

        // ------------------------------------------------------------------
        //  卡面效果 → 分（原版 `ScoreFromPlayingNow` → `ScoreFromCriteria`）
        // ------------------------------------------------------------------

        /// <summary>
        /// 一串效果的分数合计。**原版是遍历卡资产上的 `ScoringCriteria` 表**（`RawCardScript 0x2E8/0x2F0`），
        /// 那些值在服务端 ⇒ 我们从**自己解析出来的 `EffectOp`** 推（见 <see cref="ScoreOp"/>）。
        /// ⚠️ 这一层的系数**是我们配的**，不是从原版读的 —— 结构与量级照原版（分都是「几个点」）。
        /// </summary>
        public static float ScoreOps(BattleContext ctx, IReadOnlyList<EffectOp> ops, int slot)
        {
            if (ctx == null || ops == null) return 0f;
            float sum = 0f;
            foreach (var op in ops) sum += ScoreOp(ctx, op, slot);
            return sum;
        }

        /// <summary>一条 op 值多少分（<see cref="ScoreOps"/> 的逐条版）。</summary>
        public static float ScoreOp(BattleContext ctx, EffectOp op, int slot)
        {
            if (op == null) return 0f;
            int me = ctx.Active, foe = 1 - me;
            var foeBoard = ctx.Players[foe].Board;
            var mineBoard = ctx.Players[me].Board;
            var myW = ctx.Players[me].Warlord;

            switch (op.Verb)
            {
                case "deal":
                {
                    int dmg = op.Amount > 0 ? op.Amount : 1;
                    bool hitsAll = op.Target != null && op.Target.Count == 0;
                    float best = 0f; int n = 0;
                    // ⚠️ **督军也在 `Board[WarlordSlot]` 里**（`RuleCore.NewBattle` 把它放进棋盘、
                    //    和 `PlayerState.Warlord` 是同一个对象）⇒ 这里**不要再单独算一次督军**，
                    //    否则 `all` 那种群伤会把目标数算重。
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = foeBoard[t];
                        if (tu == null || !tu.IsAlive) continue;
                        n++;
                        float v = ScoreDamaging(tu, dmg);
                        if (v > best) best = v;
                    }
                    return hitsAll ? best * System.Math.Max(1, n) : best;
                }

                case "heal":
                {
                    int amount = op.Amount > 0 ? op.Amount : 1;
                    float best = myW != null ? ScoreHealing(myW, amount) : 0f;
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = mineBoard[t];
                        if (tu == null || tu.IsAlive == false) continue;
                        float v = ScoreHealing(tu, amount);
                        if (v > best) best = v;
                    }
                    return best;
                }

                case "draw": case "drawtype": case "drawref":
                    return System.Math.Max(1, op.Amount) * ScoreDraw(ctx);

                case "destroy":
                {
                    float best = 0f;
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = foeBoard[t];
                        if (tu == null || !tu.IsAlive) continue;
                        float v = ValueInPlay(tu);                      // 直接摧毁 = 按它的价值算
                        if (v > best) best = v;
                    }
                    return best;
                }

                case "stun":
                {
                    float best = 0f;
                    for (int t = 0; t < BoardSpec.Size; t++)
                    {
                        var tu = foeBoard[t];
                        if (tu == null || !tu.IsAlive) continue;
                        float v = ScoreStun(tu);
                        if (v > best) best = v;
                    }
                    return best;
                }

                case "return":
                    return 0f;        // 弹回手牌：原版按「目标当前费用」算，我们的 op 里没有那个数 ⇒ 不猜，记 0

                case "give": case "gain":
                    return PayloadScore(op.Payload) * System.Math.Max(1, op.Amount);

                case "blind":
                    return 1.0f;      // 失明＝远程攻击力算 0（原版给目标的负分是 −1.5）—— 我们配的小正值

                case "lowercost":
                    return op.Amount * 1.0f;    // 降费：原版没有对应 criteria，**我们配的**

                case "gainenergy": case "refill":
                    return 1.0f * System.Math.Max(1, op.Amount);

                default:
                    // 没写的（`deploy` / `create` / `repeat` / `chooseone` …）一律 0 ——
                    // **不猜**。要给它分值就得先有原版依据，见正本 §八。
                    return 0f;
            }
        }

        /// <summary>
        /// `give` / `gain` 的载荷值多少分。载荷原文形如 `+1 attack` / `armour 2` / `shield` / `flank`。
        /// 属性增减益的系数**是我们配的**（原版 `ScoreFromBuff` 的 `addStats` 出口是
        /// `(近战+远程+血)` 的原始和 —— 那个口径直接搬过来会跟卡价值量级不搭）；
        /// 关键词那半**照原版的表**（<see cref="TraitScore"/>）。
        /// </summary>
        public static float PayloadScore(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return 0f;
            string s = payload.Trim().ToLowerInvariant().Replace("+", " ").Replace("-", " ");

            string[] parts = s.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            int num = 0; string word = null;
            for (int i = 0; i < parts.Length; i++)
            {
                int v;
                if (int.TryParse(parts[i], out v)) { num += v; continue; }
                if (word == null) word = parts[i];
            }
            if (word == null) return 0f;
            if (num <= 0) num = 1;

            switch (word)
            {
                case "attack": case "melee": return num * 1.0f;
                case "ranged": return num * 1.0f;
                case "health": return num * 0.7f;
                case "armour": case "armor": return num * 0.6f;
                case "cost": return num * 0.5f;
                default: return TraitScore(word);      // 关键词：整条载荷算一份（原版那张表）
            }
        }

        // ==================================================================
        //  ③ 斩杀线（原版 `CombinedAttackCanKillWarlord`）
        // ==================================================================

        /// <summary>
        /// 这一回合**最多能对对面督军造成多少伤害**。
        ///
        /// 把「还能动的单位」逐个数一遍，每个单位取它**能打出的最大那一份**：
        ///   · **攻击**：近战 / 远程两路各问一次引擎（`IsValidTarget`），取伤害高的那一路
        ///     —— 一个单位一回合只出一刀，**不能两路相加**；
        ///   · **技能**：只算「打 `EnemyWarlord`」那类伤害技能。
        ///     ⚠️ 技能和攻击**二选一**（放了技能这个单位就疲劳），所以取两者里大的那个，**也不能相加**。
        ///
        /// ⚠️ **和原版的一处差别**：原版 `CombinedAttackCanKillWarlord` **只累加攻击动作**
        ///    （`AI__CombinedAttackCanKillWarlord.c:50-185`），我们这里把「打脸的伤害技能」也算进去了
        ///    —— 因为下一刀一定是「攻击**或**技能」二选一，算进去才是真上界。判据**只此一处**。
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

        /// <summary>原版 `CombinedAttackCanKillWarlord` 的名字，判据就是 <see cref="CanLethal"/>（**只此一处**）。</summary>
        public static bool CombinedAttackCanKillWarlord(BattleContext ctx) { return CanLethal(ctx); }

        // ==================================================================
        //  ④ 难度：随机挑掉最低分的动作（原版 `TweakAvailableActions`）
        // ==================================================================

        /// <summary>三个难度旋钮（原版在 `AIBotsConfig.BotConfig` 里：`skipChance / minSkips / maxSkips`）。</summary>
        public struct Knobs
        {
            public int MinSkips;      // 无条件删掉最低分的几条
            public int MaxSkips;      // 最多删几条
            public int SkipChance;    // 之后每条以这个百分比（0-100）被删
        }

        /// <summary>
        /// 难度 → 旋钮。
        /// 🔴 **这四个档的取值是我们配的**：原版那三旋钮的**值**在 `AIBotsConfig` 的资产里
        /// （按排位/连败挑配置），**本地没有那份数据**（和 `ScoringCriteria` 同一种情况）。
        /// 结构照原版：**越简单 = 砍得越狠**（砍掉的都是分最低的那些，所以 AI 会做出菜的选择）。
        ///
        /// ⚠️ **`MinSkips` 一律给 0**（原版那个值是「无条件先砍掉几条」）—— 给正数的话，
        ///    当 AI 只剩一两条动作时会把它们**全砍光**，表现成「对手发呆、什么都不做」。
        ///    改成「每条按概率砍 + 最多砍 N 条」就没有这个死角，而难度梯度照样在。
        /// </summary>
        public static Knobs KnobsOf(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return new Knobs { MinSkips = 0, MaxSkips = 3, SkipChance = 90 };
                case AiDifficulty.Easy:      return new Knobs { MinSkips = 0, MaxSkips = 2, SkipChance = 60 };
                case AiDifficulty.Normal:    return new Knobs { MinSkips = 0, MaxSkips = 1, SkipChance = 30 };
                default:                     return new Knobs { MinSkips = 0, MaxSkips = 0, SkipChance = 0 };
            }
        }

        /// <summary>
        /// 难度旋钮：把动作按分降序排，然后**从最低分那条开始**、跳过 `endTurn`，
        /// 前 `MinSkips` 条**无条件删**，之后每条掷一次骰子（`< SkipChance` 就删），
        /// 最多删 `MaxSkips` 条。`endTurn` **永远不删**（它是基准，删了 AI 就没法收手）。
        ///
        /// ⚠️ 骰子走 <see cref="BattleContext.AiRng"/>（种子派生 ⇒ 同一局可复现）。
        /// ⚠️ 与反编译的一处细节差别：原版是「删一条 → 再往前挪一条重掷」，这里是「从尾巴上逐条掷一次」。
        ///    语义一致（都是**砍最低分的**、最多 `MaxSkips` 条），消耗的随机数个数不同。
        /// </summary>
        public static void TweakAvailableActions(BattleContext ctx, List<AiAction> list, AiDifficulty d)
        {
            var k = KnobsOf(d);
            if (k.MaxSkips <= 0 || list == null || list.Count <= 1) return;

            list.Sort((x, y) => y.Score.CompareTo(x.Score));      // 降序（原版 OrderByDescending(actionScore)）

            int removed = 0;
            for (int i = list.Count - 1; i >= 0 && removed < k.MaxSkips; i--)
            {
                if (list[i].Kind == AiActionKind.EndTurn) continue;
                bool cut = removed < k.MinSkips || ctx.AiRng.Next(0, 100) < k.SkipChance;
                if (!cut) continue;
                list.RemoveAt(i);
                removed++;
            }
        }

        /// <summary>
        /// 挑最好的一条（原版 `SelectBestAction`）：先拿 `endTurn` 当基准，
        /// **严格大于才换**（平手保留先出现的 —— 所以**这个过程本身不掷骰**）；
        /// 唯一的例外：当前最佳还是 `endTurn` 而候选是攻击时，攻击胜出 ——
        /// **除非**出手单位是英雄 / 带 `stealth` / `vanguard` / 特殊攻击打分（原版那四类排除），
        /// 带 `berzerk` 的则**一定**胜出。
        /// </summary>
        public static AiAction SelectBestAction(BattleContext ctx, List<AiAction> list)
        {
            if (list == null || list.Count == 0) return null;

            AiAction best = null;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Kind == AiActionKind.EndTurn) { best = list[i]; break; }
            if (best == null) best = list[0];

            foreach (var cur in list)
            {
                if (cur.Kind == AiActionKind.EndTurn) continue;
                if (cur.Score > best.Score) { best = cur; continue; }
                if (best.Kind == AiActionKind.EndTurn
                    && (cur.Kind == AiActionKind.AttackMelee || cur.Kind == AiActionKind.AttackRanged)
                    && KillerOverride(ctx, cur)) best = cur;
            }
            return best;
        }

        /// <summary>原版 `SelectBestAction` 里那个例外：这一刀哪怕分不高也该打出去。</summary>
        static bool KillerOverride(BattleContext ctx, AiAction a)
        {
            var u = ctx.Players[ctx.Active].Board[a.Slot];
            if (u == null) return false;
            if (u.Has("berzerk")) return true;                        // 原版：狂暴一定出手
            if (u.IsWarlord) return false;                            // 督军不参与这个例外
            if (u.Has(KeywordTable.Stealth) || u.Has(KeywordTable.Vanguard)) return false;
            return true;
        }

        // ==================================================================
        //  ⑤ 一步（原版 `PlayTurn`）
        // ==================================================================

        /// <summary>
        /// **做一步**（原版 `PlayTurn` 的形状：一次调用只挑一条动作）。
        /// 返回 false = 这回合没动作可做（最佳是 `endTurn`）—— 调用方该收手了。
        /// 返回 true 时 <paramref name="best"/> 就是要执行的那条。
        /// ⚠️ **只挑不动手** —— 执行仍由调用方（表现层 / 自检）走 `RuleCore.*`，这样才播得出动画。
        /// </summary>
        public static bool NextAction(BattleContext ctx, AiDifficulty diff, out AiAction best)
        {
            best = null;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            if (list.Count == 0) return false;
            foreach (var a in list) a.Score = ScoreAction(ctx, a);
            TweakAvailableActions(ctx, list, diff);
            best = SelectBestAction(ctx, list);
            if (best == null || best.Kind == AiActionKind.EndTurn) { best = null; return false; }
            return true;
        }

        /// <summary>默认难度的一步（`Normal`）。自检里要指定难度就用三参那个重载。</summary>
        public static bool NextAction(BattleContext ctx, out AiAction best)
        {
            return NextAction(ctx, AiDifficulty.Normal, out best);
        }

        /// <summary>
        /// 现在**到底有没有事可做**（不掷骰、不吃难度旋钮）。
        /// 用途是表现层的「这回合卡住了吗 → 自动结束回合」那个判断：
        /// 拿 <see cref="NextAction"/> 去问会**误判** —— 难度那套会随机砍掉最低分的动作，
        /// 玩家明明还能出牌也可能被砍成只剩 `endTurn`。
        /// </summary>
        public static bool HasAnyAction(BattleContext ctx)
        {
            if (ctx == null || ctx.IsOver) return false;
            foreach (var a in EnumerateActions(ctx))
                if (a.Kind != AiActionKind.EndTurn) return true;
            return false;
        }

        /// <summary>执行一条动作（调用方拿着 <see cref="NextAction"/> 给的动作时用它）。
        /// 返回 true = 执行成功。</summary>
        public static bool ExecuteAction(BattleContext ctx, AiAction a)
        {
            if (ctx == null || a == null || ctx.IsOver) return false;
            int me = ctx.Active;
            switch (a.Kind)
            {
                case AiActionKind.PlayCard:
                    return RuleCore.PlayCard(ctx, me, a.HandIdx, a.Slot) == RuleCodes.OK;
                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                    return RuleCore.DeclareAttack(ctx, me, a.Slot, a.TargetP, a.TargetSlot, a.Ranged) == RuleCodes.OK;
                case AiActionKind.ActiveAbility:
                    if (a.AltKeyword == "oath")
                        return RuleCore.UseOathAbility(ctx, me, a.Slot) == RuleCodes.OK;
                    if (a.AltKeyword != null)
                        return RuleCore.UseAlternative(ctx, me, a.Slot, a.AltKeyword,
                                                       a.TargetSlot >= 0 ? a.TargetSlot : -1) == RuleCodes.OK;
                    return RuleCore.UseAbility(ctx, me, a.Slot, a.TargetSlot >= 0 ? a.TargetSlot : -1) == RuleCodes.OK;
                default:
                    return false;
            }
        }

        // ==================================================================
        //  ⑥ 兼容层 —— 老的调用点还在用这几个入口
        //     （`BattleDriver` / `BattleAutoDrive` / `BattleScene` / `RuleEngineTest` 共 30 处）
        //     语义 = 「从这一类动作里挑分最高的那条」，判据全部走上面的打分族，**不另写一份**。
        // ==================================================================

        /// <summary>
        /// 这个单位这一刀用近战还是远程：**远程攻击力更高就用远程**。
        /// ⚠️ 和 `BattleDriver.UseRanged` 是**同一条规则**，两处必须一致 ——
        ///    各写一份的话，AI 算出来的刀和画面上播出来的刀会对不上。
        /// ⚠️ **2026-09-17 之后 AI 自己不再用它挑刀**（照原版：近战/远程**各算一次分再挑高的**），
        ///    这条留着给表现层与旧调用点用。
        /// </summary>
        public static bool UseRanged(UnitState u)
        {
            return u != null && u.RangedAttack > u.Attack;
        }

        /// <summary>
        /// 这一手出什么牌、落哪一格。**单位卡和战术卡都算**，取打分最高的那条。
        /// 落点由**引擎**说了算（`RuleCore.CanPlayCard`），这里不重写一遍合法性判断。
        /// </summary>
        public static bool NextPlay(BattleContext ctx, out int handIdx, out int slot)
        {
            handIdx = -1; slot = -1;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            AiAction best = null;
            foreach (var a in list)
            {
                if (a.Kind != AiActionKind.PlayCard) continue;
                a.Score = ScoreAction(ctx, a);
                if (best == null || a.Score > best.Score) best = a;
            }
            if (best == null) return false;
            handIdx = best.HandIdx; slot = best.Slot;
            return true;
        }

        /// <summary>该出哪张手牌（返回手牌索引，-1 = 不出）。判据就是 <see cref="NextPlay"/>。</summary>
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

        /// <summary>
        /// 该不该放技能（含誓约 / 替代行动）。
        ///
        /// 🔴 **比较口径：和「**同一个单位**的平A」比**（老版本写的是「技能伤害 ≥ 攻击力就放」，
        ///    两者是同一件事的两种写法：技能**不吃反击**，打得一样多就没道理去平A）。
        /// ⚠️ **不能拿「全场最高的那一刀」来比** —— 那样督军的一刀会把场上单位的好技能全压掉
        ///    （2026-09-17 踩过：`Reef Guard` 那族断言当场红）。比较范围限定在**本单位**。
        /// </summary>
        public static bool NextAbility(BattleContext ctx, out int slot, out int targetSlot)
        {
            slot = -1; targetSlot = -1;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            foreach (var a in list) a.Score = ScoreAction(ctx, a);

            AiAction best = null;
            foreach (var a in list)
            {
                if (a.Kind != AiActionKind.ActiveAbility) continue;
                if (a.Score <= 0f) continue;                 // endTurn 基准是 0

                // 本单位「**打同一个目标**」的平A值多少（近战/远程取高的那路）
                // 🔴 **必须同目标比**：拿「本单位最高的那一刀」来比是错的 —— 打督军那一刀天生比
                //    打小兵分高，会把「打小兵的技能」全压掉（2026-09-17 踩过：`TestAiUsesAbility`
                //    的 2 攻打 2 伤技能当场红）。原版的技能和攻击本来就**各自对应同一个目标**。
                float ownAttack = 0f;
                if (a.TargetSlot >= 0)
                {
                    foreach (var b in list)
                        if (b.Slot == a.Slot && b.TargetSlot == a.TargetSlot
                            && (b.Kind == AiActionKind.AttackMelee || b.Kind == AiActionKind.AttackRanged)
                            && b.Score > ownAttack) ownAttack = b.Score;
                }

                if (ownAttack > a.Score) continue;           // 平A更划算就不放技能
                if (best == null || a.Score > best.Score) best = a;
            }
            if (best == null) return false;
            slot = best.Slot; targetSlot = best.TargetSlot;
            return true;
        }

        /// <summary>
        /// 该打哪一刀（分最高的那条攻击动作）。
        /// ⚠️ 近战/远程**各算一次分**（照原版），返回的那条带着它自己的 `ranged`。
        /// </summary>
        public static bool NextAttack(BattleContext ctx, out int atkSlot, out int targetP, out int targetSlot,
                                      out bool ranged)
        {
            atkSlot = -1; targetP = -1; targetSlot = -1; ranged = false;
            if (ctx == null || ctx.IsOver) return false;

            var list = EnumerateActions(ctx);
            AiAction best = null;
            foreach (var a in list)
            {
                if (a.Kind != AiActionKind.AttackMelee && a.Kind != AiActionKind.AttackRanged) continue;
                a.Score = ScoreAction(ctx, a);
                if (best == null || a.Score > best.Score) best = a;
            }
            if (best == null) return false;
            atkSlot = best.Slot; targetP = best.TargetP; targetSlot = best.TargetSlot; ranged = best.Ranged;
            return true;
        }

        /// <summary>
        /// **该把哪几张起手牌换掉**（原版 `AI.GetAiMulliganCards`，2026-09-17 反编译解开 ——
        /// 见正本 §二）：
        ///
        /// ```
        /// 遍历手牌：
        ///   · mulliganOption == whispersOfChaos(30) → 保留
        ///   · 否则 manaCost > 4                      → 换掉
        ///   · 其余保留
        /// ```
        ///
        /// ⚠️ **我们这边的两处对应/差别**（如实标着）：
        ///   ① `whispersOfChaos` 这一支**我们卡池里没有对应物**（那是原版的卡种类，
        ///      我们的 `type` 只有 unit / tactic / hero / defence）⇒ 这条分支不适用，不是被我们砍了；
        ///   ② **防御卡排除在外** —— 那是**引擎自己的规矩**（`RuleCore.Mulligan` 里写着「防御卡不许换掉」，
        ///      理由见那段注释）。原版 AI 那边没有这一条，因为原版的防御卡不是这么发的。
        /// </summary>
        public static List<int> AiMulliganIndices(BattleContext ctx, int p)
        {
            var idx = new List<int>();
            if (ctx == null || !ctx.MulliganOpen) return idx;
            var hand = ctx.Players[p].Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var c = hand[i];
                if (c == null) continue;
                if (c.Type == "defence") continue;          // 引擎规矩：防御卡不许换
                if (c.Cost > 4) idx.Add(i);
            }
            return idx;
        }

        /// <summary>
        /// 一口气把这回合打完（自检里跑完整对局用它；表现层要分部播的话别用）。
        /// 难度固定 <see cref="AiDifficulty.Hard"/> —— **不掷骰**，让批量自检只受种子影响。
        /// </summary>
        public static void PlayTurn(BattleContext ctx)
        {
            for (int guard = 0; guard < 64 && ctx != null && !ctx.IsOver; guard++)
            {
                AiAction a;
                if (!NextAction(ctx, AiDifficulty.Hard, out a)) break;
                if (!ExecuteAction(ctx, a)) break;          // 引擎说不行就停手（别死循环）
            }
        }
    }
}
