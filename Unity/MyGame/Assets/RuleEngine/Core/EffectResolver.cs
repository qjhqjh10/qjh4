// EffectResolver.cs — **把解析出来的效果真正打出去**（`EffectText` 的结算侧）
//
// 分工：
//   `EffectText`   把卡面文本 → `EffectOp` 列表（只解析，不动状态）
//   `GivePayload`  把 `give X to Y` 的 X → 属性增减益 / 关键词授予
//   **本文件**      把 `EffectOp` 落到 `BattleContext` 上（改状态、发事件、写日志）
//
// **权威语义来源**：`d:/warpforge/scripts/rule_core.gd`
//   · `_collect_tactic_targets`（`:2439` 起，187 行的目标词表）→ 我们的 `ResolveTargets`
//   · `_apply_gain`（`:3266`）→ 我们的 `ApplyGain`
//   · `_resolve_text` 各分支的**结算**部分（伤害 / 治疗 / 摧毁 / 眩晕 …）→ `ResolveOps`
//
// 🔴 **本文件的红线：不许静默失败。**
//    碰到还没实现的动词/关键词，**必须在 ctx.Log 里说出来**（玩家会看到），
//    并让调用方知道「这张卡没有完整生效」。装成跑通了比不实现更糟。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  效果结算：`EffectOp` 列表
        // ==================================================================

        /// <summary>
        /// 结算一组效果（一张战术卡 / 一次触发 / 一个主动技能的效果部分）。
        /// </summary>
        /// <param name="chosen">表现层已选定的目标（需要选目标的战术卡才有）</param>
        /// <returns>有多少条 op **完整结算**了（没完整结算的会写进 <paramref name="unresolved"/>）</returns>
        public static int ResolveOps(BattleContext ctx, int owner, UnitState source,
                                     List<EffectOp> ops, UnitState chosen,
                                     out List<string> unresolved)
        {
            unresolved = new List<string>();
            if (ops == null) return 0;
            string by = source != null ? source.Name : "战术卡";

            // 一张卡的结算 = **一个计数窗口**：`For each troop drawn …` 数的是这张卡自己抽到的牌
            // （见 `BattleContext.DrawnThisResolve`）。嵌套结算（Rally 触发里再打一张牌）会各开各的窗口。
            ctx.DrawnThisResolve.Clear();

            // 表现层**已经替这张卡选好了目标**时，先把它记成「当前目标」——
            // 卡面上那些 `for each Dark Pact **on it**` 的 `it` 指的正是这张卡自己的目标，
            // 而这条效果前面**没有任何「上一条效果」**去设置 `ctx.LastTarget`。
            // 不种下去就会读到**上一张卡**留下的值（2026-09-12 撞到：同一张卡第二次打出反而少算）。
            if (chosen != null) ctx.LastTarget = chosen;

            // ⚠️ 这里**只配对、不决定跳谁** —— 决定放到主循环里做。
            //    因为「跳谁」取决于条件，而条件可能要数 `for each`、要用**这次选好的目标**
            //    （`chosen` 在这一步才拿得到），在这里判会读到上一张卡留下的 `ctx.LastTarget`。
            var skip = new bool[ops.Count];
            var insteadBase = new int[ops.Count];
            for (int i = 0; i < ops.Count; i++) insteadBase[i] = -1;
            for (int i = 0; i < ops.Count; i++)
            {
                if (!ops[i].Instead) continue;

                for (int j = i - 1; j >= 0; j--)
                {
                    if (ops[j].Instead) break;                                 // 不跨过另一条替换
                    if (!string.IsNullOrEmpty(ops[j].Condition)) continue;     // 只替换无条件的
                    if (ops[j].Verb != ops[i].Verb) continue;
                    insteadBase[i] = j;
                    break;
                }
            }

            // ---- `instead`：成对决定谁跳谁（**必须在主循环之前**）----
            // 因为跳的可能是**前面**那条（`baseIdx < i`），主循环走到 `i` 时它早就结算完了。
            //   · 条件成立   → 跳过被替换的那条，只结算这一条；
            //   · 条件不成立 → 跳过这一条，**被替换的那条照常结算**。
            // ⚠️ 条件判不了时不在这儿做决定，交给 `ResolveOne` 去走它那条
            //    「条件判不了 → 整条不生效 + 如实报出来」的路（红线：不许静默失败）。
            //
            // ⚠️ **本条要数 `for each` 时，整个替换机制让位**。
            //    数 `… for each Dark Pact on it` 要读 `it` 的目标，而那时 `ctx.LastTarget`
            //    可能还是上一张卡留下的（这类卡的「it」就是本次的目标，不是上一条效果的产物）。
            //    与其按一个过时的值判错，不如放弃替换：两条各自走正规流程。
            //    **代价是数值会叠加，如实写在这儿，不装作它是原版语义。**
            for (int i = 0; i < ops.Count; i++)
            {
                if (!ops[i].Instead || insteadBase[i] < 0) continue;
                int baseIdx = insteadBase[i];
                bool holds;
                bool judgeable = ConditionHolds(ctx, owner, ops[i], chosen, out holds);
                bool usesForEach = (ops[i].PerCount > 0 && !string.IsNullOrEmpty(ops[i].CountRef))
                                   || !string.IsNullOrEmpty(ops[i].CountScope);
                if (!judgeable || usesForEach) continue;      // 两条都照常走
                if (holds) skip[baseIdx] = true;              // 替换成立：原来那条不结算
                else skip[i] = true;                          // 不成立：这条不结算，原效果照常
            }

            int done = 0;
            for (int i = 0; i < ops.Count; i++)
            {
                if (skip[i]) continue;
                var op = ops[i];

                // ---- `for each` 增量型：数值 = 基础 + 计数 × 增量 ----
                // `Give +2 Melee Attack to a friendly troop, and an additional +2 Melee Attack
                //  for each Dark Pact on it` —— 一份契约时是 **+4**，不是 +2 再来两遍。
                if (op.PerCount > 0 && !string.IsNullOrEmpty(op.CountRef) && op.Verb != "deal" && op.Verb != "heal")
                {
                    int n = CountFor(ctx, owner, source, op);
                    if (n < 0)
                    {
                        ctx.Log($"{by}：「{op.Source}」的计数「{op.CountRef}」本版数不出来 —— **这条没生效**");
                        unresolved.Add(op.Source + "（计数 " + op.CountRef + " 数不出来）");
                        continue;
                    }
                    // ⚠️ **不能改 `op.Amount` 本身**：`EffectOp` 列表是**解析产物、会被复用**
                    //    （同一张卡第二次打出时是同一份对象），改了就一次比一次高。
                    //    2026-09-12 撞到：第二次打 `Daemonic Frenzy` 变成 +4 而不是 +2。
                    int before = op.Amount;
                    int eff = op.Amount + n * op.PerCount;
                    ctx.Log($"{by}：「{op.Source}」基础 {before} + 每个 {op.CountRef} 加 {op.PerCount}"
                          + $" × {n} = **{eff}**");
                    // 递一份**副本**下去（不动解析产物本身 —— 见上面那条注释）
                    var effOp = op.Clone();
                    effOp.Amount = eff;
                    // ⚠️ `give` / `gain` / `lose` 的数值**不在 `Amount` 里，在 `Payload` 的原文里**
                    //    （`+2 melee attack` 这种，由 `GivePayload` 现解）。只改 `Amount` 的话
                    //    载荷还是 +2 —— 2026-09-12 撞到：`Daemonic Frenzy` 有契约时只加了 2 不是 4。
                    //    所以这类动词要**把增量写回载荷文本**。
                    // ⚠️ **`eff == 0` 时不能去改载荷**：载荷里的数字是**基础值**（`+2 melee attack` 的 2），
                    //    而 `Amount` 对这类载荷是 0。把载荷改成 0 就把基础值抹掉了 —— 2026-09-12 撞到：
                    //    没契约时（eff = 0 + 0）`Daemonic Frenzy` 变成 +0。
                    if (eff != 0 && (op.Verb == "give" || op.Verb == "gain" || op.Verb == "lose"))
                        effOp.Payload = ScalePayload(op.Payload, eff);
                    if (ResolveOne(ctx, owner, source, by, effOp, chosen, unresolved)) done++;
                    continue;
                }

                // ---- `for each` 重复型：这一条**结算 N 遍** ----
                // 出处：规则书 :233「随机选出 N 张候选 … 结算」；原版 `_resolve_for_each:2524`。
                // ⚠️ **N = 0 就是一次都不结算**（不是「退化成 1 次」）—— 盘上一个友方部队都没有时，
                //    `Deal 1 damage to an enemy for each friendly unit` 就该什么都不做。
                int repeats = 1;
                if (!string.IsNullOrEmpty(op.CountRef) && op.CountScope != null)
                {
                    int n = CountFor(ctx, owner, source, op);
                    if (n < 0)
                    {
                        ctx.Log($"{by}：「{op.Source}」的计数「{op.CountRef}」本版数不出来 —— **这条没生效**");
                        unresolved.Add(op.Source + "（计数 " + op.CountRef + " 数不出来）");
                        continue;
                    }
                    repeats = n;
                    ctx.Log($"{by}：「{op.Source}」按 {op.CountRef} 计 {n} 遍");
                }

                bool ok = false;
                for (int k = 0; k < repeats; k++)
                    if (ResolveOne(ctx, owner, source, by, op, chosen, unresolved)) ok = true;
                if (ok) done++;
            }
            // 摧毁/伤害可能把督军打死 —— 结算完统一判一次胜负
            // （`Hurt` 自己不判，和攻击那条路一致：由调用方在结算完之后判）
            if (!ctx.IsOver) CheckWinner(ctx);
            return done;
        }

        static bool ResolveOne(BattleContext ctx, int owner, UnitState source, string by,
                               EffectOp op, UnitState chosen, List<string> unresolved)
        {
            // ---- 付费激活：付不起就**整段不结算**（原版 `rule_core.gd:2529`）----
            if (op.Cost > 0)
            {
                var ps = ctx.Players[owner];
                if (ps.Energy < op.Cost)
                {
                    ctx.Log($"{by}：「{op.Source}」需要 {op.Cost} 能量才激活，能量不够 —— **这一条没生效**");
                    unresolved.Add(op.Source + "（付费不够）");
                    return false;
                }
                ps.Energy -= op.Cost;
                ctx.Log($"{by} 付了 {op.Cost} 点能量激活「{op.Source}」");
            }

            // ---- 条件：判不了就**不结算**，并如实报出来 ----
            if (!string.IsNullOrEmpty(op.Condition))
            {
                bool ok;
                if (!ConditionHolds(ctx, owner, op, chosen, out ok))
                {
                    ctx.Log($"{by}：「{op.Source}」的条件「{op.Condition}」本版判不了 —— "
                          + "**整条效果没有生效**（不是「条件不成立」）");
                    unresolved.Add(op.Source + "（条件判不了：" + op.Condition + "）");
                    return false;
                }
                if (!ok)
                {
                    ctx.Log($"{by}：「{op.Source}」的条件「{op.Condition}」不成立，跳过");
                    return true;                     // 条件不成立是**正常结算**，不是失败
                }
            }

            switch (op.Verb)
            {
                case "deal":       return DoDeal(ctx, owner, by, op, chosen, unresolved);
                case "heal":       return DoHeal(ctx, owner, by, op, chosen, unresolved);
                case "draw":       return DoDraw(ctx, owner, by, op);
                case "destroy":    return DoDestroy(ctx, owner, by, op, chosen, unresolved);
                case "stun":       return DoStun(ctx, owner, by, op, chosen, unresolved);
                case "blind":      return DoBlind(ctx, owner, by, op, chosen, unresolved);
                case "give":       return DoGive(ctx, owner, by, op, chosen, unresolved, +1);
                case "gain":       return DoGive(ctx, owner, by, op, chosen, unresolved, +1);
                case "gainenergy": return DoEnergy(ctx, owner, by, op);
                case "lose":       return DoGive(ctx, owner, by, op, chosen, unresolved, -1);
                case "refill":     return DoRefill(ctx, owner, by, op);
                case "chooseone":  return DoChooseOne(ctx, owner, by, op, chosen, unresolved);
            }

            ctx.Log($"{by}：「{op.Source}」的动作「{op.Verb}」本版还没实现 —— **这条没生效**");
            unresolved.Add(op.Source + "（动词 " + op.Verb + " 没实现）");
            return false;
        }

        // ==================================================================
        //  目标：`EffectTargetSpec` → 场上的单位
        // ==================================================================

        /// <summary>
        /// 目标短语 → 单位列表。**规则来自 `rule_core.gd:_collect_tactic_targets`（`:2439`）**。
        ///
        /// ⚠️ 两条铁律（本工程反复强调）：
        ///   ① **不许掷骰**：`random` 的选择用 `ctx.Rng`（种子定死、同一局可复现），
        ///      绝不用 `UnityEngine.Random`；
        ///   ② **目标找不到就空过**，不自动改打别的（那是替玩家做决定）。
        /// </summary>
        public static List<UnitState> ResolveTargets(BattleContext ctx, int owner,
                                                     EffectTargetSpec spec, UnitState source,
                                                     UnitState chosen)
        {
            var list = new List<UnitState>();
            if (spec == null) return list;

            // 指代上一条效果的目标
            if (spec.Side == "prev" || spec.Kind == "prev")
            {
                var last = ctx.LastTarget;
                if (last != null && last.IsAlive) list.Add(last);
                return list;
            }

            bool own = spec.Side == "own" || spec.Side == "any";
            bool enemy = spec.Side == "enemy" || spec.Side == "any";

            var pool = new List<UnitState>();
            if (spec.Kind == "warlord")
            {
                if (own && ctx.Players[owner].Warlord != null) pool.Add(ctx.Players[owner].Warlord);
                if (enemy && ctx.Players[1 - owner].Warlord != null) pool.Add(ctx.Players[1 - owner].Warlord);
            }
            else
            {
                bool troopOnly = spec.Kind == "troop";
                if (own) AddSide(pool, ctx.Players[owner], false, troopOnly, -1);
                if (enemy) AddSide(pool, ctx.Players[1 - owner], true, troopOnly, owner);
            }

            // ⚠️ **兵种词过滤不了**：`spec.Kind` 可能是 `vehicle` / `beast` / `battlesuit` 这类，
            //    但我们卡表里**没有兵种字段**（`CardDef.Type` 只有 unit/tactic/hero/defence）——
            //    只能按整个目标池打。原版也一样（`_collect_tactic_targets` 注释写着「类型过滤精度 P2」）。
            //    解析层会把这种目标标成 `KindUnfilterable`，覆盖率里单独报，不让它冒充精确打击。
            //    （原来这儿有两行 `if (spec.Kind == "infantry") pool.RemoveAll(u => !u.Has("infantry"))`
            //      —— 那是**死代码**：单位身上不会有 "infantry" 这种关键词，永远过滤不掉任何东西。）

            if (spec.Count == 0)
            {
                list.AddRange(pool);                       // `all`
            }
            else if (spec.Random && spec.Count < pool.Count)
            {
                // **种子定死**：用 ctx 的 Rng，别用 UnityEngine.Random
                var copy = new List<UnitState>(pool);
                for (int i = 0; i < spec.Count && copy.Count > 0; i++)
                {
                    int k = ctx.Rng.Next(copy.Count);
                    list.Add(copy[k]);
                    copy.RemoveAt(k);
                }
            }
            else
            {
                int n = 0;
                foreach (var u in pool)
                {
                    list.Add(u);
                    if (++n >= spec.Count) break;
                }
            }

            // 玩家/调用方已选定目标时，优先用他选的（原版 `has_pick_target` 那一路）
            if (chosen != null && spec.Count == 1 && list.Count > 0 && !list.Contains(chosen))
            {
                list.Clear();
                list.Add(chosen);
            }
            if (list.Count > 0) ctx.LastTarget = list[0];     // 供下一句 `it` / `them` 指代
            return list;
        }

        // ==================================================================
        //  打出战术卡
        // ==================================================================

        /// <summary>
        /// **战术卡能不能打**（只判不执行）—— 和 <see cref="PlayTactic"/> 共用这一份判据。
        ///
        /// 表现层拖拽过程中要**实时**知道这张牌能不能落，不能等松手才发现
        /// （单位卡那条是 `RuleCore.CanPlayCard`，同一个道理）。
        /// </summary>
        /// <param name="targetSlot">表现层选定的目标格位；不需要选目标时传 -1</param>
        public static int CanPlayTactic(BattleContext ctx, int p, int handIdx, int targetSlot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            var ps = ctx.Players[p];
            if (handIdx < 0 || handIdx >= ps.Hand.Count) return RuleCodes.ErrBadHand;

            var card = ps.Hand[handIdx];
            if (card.IsUnit) return RuleCodes.ErrBadHand;         // 单位卡走 PlayCard，不是这条

            // 防御卡引擎里除了组卡合法性校验，没有任何地方认识 `defence` —— 明说不支持
            if (card.Type == "defence") return RuleCodes.ErrUnimplemented;

            // 解析不了 → 明说不支持。**先于费用判断**：否则一张用不起的卡会报「能量不足」，
            // 把「本版不支持」误导成「再等等就能打」（单位卡那条注释里记着同一个坑）
            if (!EffectText.IsFullyParsed(card.Desc)) return RuleCodes.ErrUnimplemented;
            var ops = EffectText.Parse(card.Desc, out _, out _);

            if (card.Cost > ps.Energy) return RuleCodes.ErrCost;

            // **要选目标**的才要求给格位；`Refill 2 Energy` / `Draw 2 cards` 这类随便放哪都行。
            // 「这一格能不能选」用**结算时那一份判据**（`IsLegalPick` → `AddSide`）——
            // 另写一份会出现「拖拽高亮说能选、打出去却空过」。
            var spec = EffectText.PickTarget(ops);
            if (spec != null)
            {
                if (targetSlot < 0 || !BoardSpec.IsValid(targetSlot)) return RuleCodes.ErrSlot;
                var u = ctx.Players[spec.Side == "enemy" ? 1 - p : p].Board[targetSlot];
                if (u == null || !IsLegalPick(ctx, p, spec, u)) return RuleCodes.ErrSlot;
            }
            return RuleCodes.OK;
        }

        /// <summary>这一格在不在**候选目标列表**里（`troop` 类不含督军、敌方隐身单位不可选）</summary>
        static bool IsLegalPick(BattleContext ctx, int owner, EffectTargetSpec spec, UnitState u)
        {
            var pool = new List<UnitState>();
            bool troopOnly = spec.Kind == "troop";
            if (spec.Side == "own" || spec.Side == "any")
                AddSide(pool, ctx.Players[owner], false, troopOnly, -1);
            if (spec.Side == "enemy" || spec.Side == "any")
                AddSide(pool, ctx.Players[1 - owner], true, troopOnly, owner);
            return pool.Contains(u);
        }

        /// <summary>
        /// **打出战术卡**：扣费 → 结算效果 → 进弃牌堆。（`rule_core.gd:play_card` 的战术分支）
        ///
        /// 和单位卡的差别：战术卡**不落格位**，所以 <paramref name="targetSlot"/> 是「效果打谁」，
        /// 不是「放哪」。要选哪个目标由卡面文本决定（<see cref="EffectText.PickSide"/>）。
        ///
        /// ⚠️ **解析不了的卡直接拒绝**（返回 <see cref="RuleCodes.ErrUnimplemented"/>），
        ///    不许「扣了费然后什么都不发生」—— 那是最难查的那种 bug。
        /// </summary>
        /// <param name="targetSlot">表现层选定的目标格位；不需要选目标时传 -1</param>
        public static int PlayTactic(BattleContext ctx, int p, int handIdx, int targetSlot = -1)
        {
            int code = CanPlayTactic(ctx, p, handIdx, targetSlot);
            if (code != RuleCodes.OK) return code;

            var ps = ctx.Players[p];
            var card = ps.Hand[handIdx];
            var ops = EffectText.Parse(card.Desc, out _, out _);
            string side = EffectText.PickSide(ops);

            UnitState chosen = null;
            if (targetSlot >= 0 && BoardSpec.IsValid(targetSlot))
                chosen = ctx.Players[side == "enemy" ? 1 - p : p].Board[targetSlot];

            ps.Energy -= card.Cost;
            ps.Hand.RemoveAt(handIdx);
            ctx.Log($"{ps.Name} 打出战术卡「{card.Name}」（{card.Cost} 能）");

            var unresolved = new List<string>();
            ResolveOps(ctx, p, null, ops, chosen, out unresolved);

            // 进弃牌堆（原版战术卡结算完就进弃牌堆）
            ps.Discard.Add(card);
            if (unresolved.Count > 0)
                ctx.Log($"⚠️ 「{card.Name}」有 {unresolved.Count} 条效果本版没结算："
                      + string.Join("、", unresolved));
            return RuleCodes.OK;
        }

        /// <summary>
        /// 收集**一方**的候选目标（含督军）。
        ///
        /// 两条过滤，出处都在原版 `_collect_tactic_targets` 的 pick 分支（`rule_core.gd:4030` 附近）：
        ///   ① **`troop` 类目标不含督军** —— 规则书：效果目标为 troop 不能影响督军
        ///      （原版 `if is_troop_only and tu.is_warlord: return out`）；
        ///      **`an enemy` / `all enemies` 这类则包含督军**（全体分支只判 `_effect_target_blocked`）。
        ///   ② **隐身的单位不能被敌方效果选中** —— 规则书 Stealth「Cannot be targeted in any way」/
        ///      Camouflage「不能被敌方战术与效果选中」（原版 `_effect_target_blocked`）。
        /// </summary>
        /// <param name="caster">施放者；传 -1 表示这是己方侧、不做隐身过滤</param>
        static void AddSide(List<UnitState> into, PlayerState ps, bool isFoe, bool troopOnly, int caster)
        {
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                if (u == null || !u.IsAlive) continue;
                if (troopOnly && u.IsWarlord) continue;
                if (isFoe && caster >= 0
                    && (u.Has(KeywordTable.Stealth) || u.Has("camouflage"))) continue;
                into.Add(u);
            }
        }

        // ==================================================================
        //  各个动词的结算
        // ==================================================================

        static bool DoDeal(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved)
        {
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」没有合法目标，空过");
                return true;                                   // 没目标 = 正常空过，不是失败
            }
            // `1-3` 区间：原版是 `randi_range`（`:2677`）—— 用 ctx.Rng 保证可复现
            int dmg = op.AmountMax > op.Amount ? ctx.Rng.Next(op.Amount, op.AmountMax + 1) : op.Amount;

            int dealt = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                dealt += Hurt(ctx, t, dmg, by);
            }
            ctx.Log($"{by}：「{op.Source}」对 {targets.Count} 个目标造成 {dmg} 点伤害（共 {dealt}）");
            return true;
        }

        static bool DoHeal(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved)
        {
            if (op.Target == null)
            {
                // `Heal N` 没写目标 = **治己方督军**（原版 `rule_core.gd:2842`）
                var w = ctx.Players[owner].Warlord;
                if (w != null) w.Health = System.Math.Min(w.MaxHealth, w.Health + op.Amount);
                ctx.Log($"{by}：「{op.Source}」治疗己方督军 {op.Amount}（现 {w?.Health}）");
                return true;
            }
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen);
            foreach (var t in targets)
                if (t != null && t.IsAlive) t.Health = System.Math.Min(t.MaxHealth, t.Health + op.Amount);
            ctx.Log($"{by}：「{op.Source}」治疗 {targets.Count} 个目标各 {op.Amount} 点");
            return true;
        }

        static bool DoDraw(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var hand = ctx.Players[owner].Hand;
            int before = hand.Count;
            for (int i = 0; i < op.Amount && !ctx.IsOver; i++) Draw(ctx, owner);
            // 记下这次抽到的是哪几张 —— `For each troop drawn …` 就数这个
            // （见 `BattleContext.DrawnThisResolve`）。⚠️ 牌库抽空时 `Draw` 只扣疲劳，
            // 手牌不变，所以这段可能一张都不记 —— 那是对的，「抽到的」确实没有。
            for (int i = before; i < hand.Count; i++) ctx.DrawnThisResolve.Add(hand[i]);
            ctx.Log($"{by}：「{op.Source}」抽了 {op.Amount} 张");
            return true;
        }

        static bool DoDestroy(BattleContext ctx, int owner, string by, EffectOp op,
                              UnitState chosen, List<string> unresolved)
        {
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen);
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                // `Invulnerable` 挡得住摧毁（规则书 L190 / 原版 `:2820`）
                if (t.Has("invulnerable"))
                {
                    ctx.Log($"{by}：「{op.Source}」想摧毁 {t.Name}，但它有 Invulnerable —— 挡下了");
                    continue;
                }
                // ⚠️ **不能走 `Hurt`**：`ApplyDamage` 要减护甲，5 血 2 甲吃 5 点只掉 3 ——
                //    「摧毁」就变成「打一下」。直接置死，再走正常的离场流程（弃牌堆 + Death 事件 + Backlash）。
                t.Health = 0;
                int o2, s2;
                if (FindUnit(ctx, t, out o2, out s2)) CleanupDeaths(ctx, o2, s2);
                else ctx.Log($"{by}：摧毁 {t.Name}，但它已经不在场上");
            }
            ctx.Log($"{by}：「{op.Source}」摧毁了 {targets.Count} 个目标");
            return true;
        }

        static bool DoStun(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved)
        {
            // `Stun` 没写目标 = 打一个敌方单位（原版 `:2755` 起）。
            // ⚠️ 不掷骰：目标按**槽号最小**那个走（`ResolveTargets` 的顺序就是棋盘顺序）。
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any",
                Count = op.Amount > 0 ? op.Amount : 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen);
            foreach (var t in targets)
                if (t != null && t.IsAlive) t.IsStunned = true;
            ctx.Log($"{by}：「{op.Source}」眩晕了 {targets.Count} 个单位");
            return true;
        }

        /// <summary>
        /// 失明：**远程攻击力视为 0**（规则书 :166；原版 `rule_core.gd:2814` 置 `blind = true`）。
        ///
        /// 没写目标时和 `Stun` 一样按**槽号最小**的敌方单位走（原版 `:2804` 那条兜底：
        /// `btargets` 空就退回 `an enemy`）—— **不掷骰**，同一局必须可复现。
        ///
        /// 时长：卡面写 `until your next turn`，登记在 <see cref="UnitState.BlindTurnEnd"/> 上，
        /// 到下一回合结束时清（见 `RuleCore.EndTurn`）。
        /// </summary>
        static bool DoBlind(BattleContext ctx, int owner, string by, EffectOp op,
                            UnitState chosen, List<string> unresolved)
        {
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any",
                Count = op.Amount > 0 ? op.Amount : 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen);

            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                t.IsBlind = true;
                // 卡面写 `until your next turn` → 撑过**对手的一整个回合**，
                // 到施放者自己的下个回合开始才清（`RuleCore.BeginTurn` 按 `BlindOwner` + `BlindTurnEnd` 判）。
                // 到期点 = 当前回合 + 2（+1 是「对手的回合」，再 +1 才是「我的下个回合」）。
                t.BlindTurnEnd = ctx.Turn + 2;
                t.BlindOwner = owner;
                t.AddKeyword("blind", 1);        // 只为 `Has`/日志可见 —— 真正生效的是 `IsBlind`
                n++;
                ctx.Log($"{t.Name} 失明了（远程攻击力视为 0，到回合 {t.BlindTurnEnd + 1} 结束）");
            }
            ctx.Log($"{by}：「{op.Source}」失明了 {n} 个单位");
            return true;
        }

        /// <summary>
        /// 三选一：**挑一个选项结算**（`EffectText.TryChooseOne` 把三个选项都解析好了，存在 `Payload` 里）。
        ///
        /// 原版是「单机自动选 1」（`rule_core.gd:1159`），我们照办 —— 但用 `ctx.Rng`
        /// （种子化）而不是 `randi()`，同一局必须永远可复现。
        ///
        /// ⚠️ **掷骰发生在结算层、不在解析层**：解析层只负责「三个选项的语法认不认得」，
        ///    这样覆盖率这个进度条才和掷骰无关、每次跑都得到同一个数。
        /// </summary>
        static bool DoChooseOne(BattleContext ctx, int owner, string by, EffectOp op,
                                UnitState chosen, List<string> unresolved)
        {
            var opts = (op.Payload ?? "").Split('|');
            if (opts.Length == 0) return false;

            int pick = opts.Length == 1 ? 0 : ctx.Rng.Next(opts.Length);
            ctx.Log($"{by}：「{op.Source}」三选一 → 选第 {pick + 1} 项（{opts[pick]}）");

            // 选项文本回进解析器再过一遍 —— 拿到的 ops 和正文里写的完全一样
            var sub = EffectText.ParseSegment(opts[pick]);
            if (sub.Ops == null) { unresolved.Add(op.Source + "（选项解析失败）"); return false; }

            int done = 0;
            foreach (var o in sub.Ops)
                if (ResolveOne(ctx, owner, null, by, o, chosen, unresolved)) done++;
            return done > 0;
        }

        /// <summary>
        /// 给单位挂上一段**嵌入的触发效果** —— `Give "💀 Backlash: Return to your hand" to a friendly troop`。
        ///
        /// 这种卡面在实测数据里有两条（`Graceful Avoidance` 的 Backlash、
        /// `Duty's End` 的「触发所有友方的典籍能力」），玩法是
        /// **把一段能力文字当礼物送给一个单位**。
        ///
        /// 做三件事：
        ///   ① 把 `关键词: 效果文字` 拆开，效果文字过 `EffectSpec.Parse`（和我们自设计的 26 张同一套文法）；
        ///   ② 挂到单位身上（`UnitState.GrantEffect`）—— 卡上原生那条优先，这条是后备；
        ///   ③ 补上那个触发关键词，这样 `RuleCore.FireTriggerAt` 到时机就会找它。
        ///
        /// ⚠️ 效果文字解析不了就**整条不挂**并如实报 —— 挂一个空的触发上去
        ///    会让卡看起来生效而实际什么都没发生（本工程的红线）。
        /// </summary>
        static void GrantEmbeddedAbility(BattleContext ctx, UnitState u, string embedded,
                                         string by, string src, List<string> unresolved)
        {
            string kw = GivePayload.EmbeddedKeyword(embedded);
            string body = GivePayload.EmbeddedBody(embedded);
            if (string.IsNullOrEmpty(kw) || string.IsNullOrEmpty(body))
            {
                unresolved.Add(src + "（嵌入效果格式不对）");
                return;
            }
            int len;
            string norm = KeywordTable.Normalize(kw, out len);
            if (norm == null || len != kw.Length)
            {
                ctx.Log($"{by}：「{src}」里的关键词「{kw}」词表里没有 —— **这条没生效**");
                unresolved.Add(src + "（嵌入的关键词 " + kw + " 不认识）");
                return;
            }

            var spec = EffectSpec.Parse(body);
            if (spec == null)
            {
                ctx.Log($"{by}：「{src}」的效果文字「{body}」本版解析不了 —— **这条没生效**");
                unresolved.Add(src + "（嵌入效果 " + body + " 解析不了）");
                return;
            }

            u.GrantEffect(norm, spec);
            u.AddKeyword(norm, 1);
            ctx.Log($"{by}：给 {u.Name} 挂上「{norm}：{spec.Source}」"
                  + $"（到 {norm} 的时机结算）");
        }

        /// <summary>
        /// `for each …` 到底数出几个。**语义出处：原版 `rule_core.gd:1347` 的 `_fe_count`**
        /// （分支顺序照抄），`CountScope` 由 `EffectText.ClassifyCount` 分好类。
        ///
        /// 为什么计数要单独一个函数：`for each` 出现在 **38 个分句**里，写法五花八门，
        /// 但「数什么」只有四类。分类在解析层、计数在结算层，两边各判一次就会不一致。
        /// </summary>
        /// <summary>自检用：把结算层的计数暴露出来（解析层和结算层各判一次就会不一致）</summary>
        public static int CountForTest(BattleContext ctx, int owner, UnitState target, EffectOp op)
        {
            return CountFor(ctx, owner, target, op);
        }

        static int CountFor(BattleContext ctx, int owner, UnitState source, EffectOp op)
        {
            string reference = op.CountRef ?? "";

            // ---- ⓪ 黑暗契约的层数（不是单位数）----
            // 出处：原版 `_fc_count:1349`（`dark pact` 是它第一个判的分支）。
            // `on it` = 上一条效果的目标；否则 = **本方**全场求和（原版只数 `_player(ctx,p)` 那一侧）。
            if (op.CountScope == "darkpact")
            {
                if (reference == "it")
                {
                    var t = ctx.LastTarget;
                    return t != null ? t.KwValue(KeywordTable.DarkPact) : 0;
                }
                int n = 0;
                var myBoard = ctx.Players[owner].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (myBoard[s] != null) n += myBoard[s].KwValue(KeywordTable.DarkPact);
                return n;
            }

            // ---- ① 本次结算抽到的牌（`For each troop drawn`）----
            if (op.CountScope == "draw")
            {
                int n = 0;
                foreach (var c in ctx.DrawnThisResolve)
                {
                    if (reference == "any" || reference == "card") { n++; continue; }
                    // 兵种词过滤不了（卡表里没有兵种字段，见 `EffectTargetSpec.KindUnfilterable`）——
                    // 和原版同精度：`_fe_count` 那边也只能按类型词近似
                    n++;
                }
                return n;
            }

            // ---- ② 本回合阵亡的单位（`For each one that dies`）----
            if (op.CountScope == "died") return ctx.DiedThisTurn;

            // ---- ③ 本局打出过的某类牌 ----
            // ⚠️ 本版**没有**「本局打出过的隐秘/破坏卡」这种流水（原版靠 `_secret_played` 计数）。
            //    认得出这个写法但数不出来 —— 返回 -1 让调用方如实报，**不许当成 0 悄悄空过**。
            if (op.CountScope == "played") return -1;

            // ---- ④ 盘面：`own|enemy` + 兵种 + 是否受损 ----
            // `reference` 形如 `enemy|troop|all` / `own|any|damaged`
            var parts = reference.Split('|');
            if (parts.Length != 3) return -1;
            bool foe = parts[0] == "enemy";
            string kind = parts[1];
            bool damagedOnly = parts[2] == "damaged";

            int count = 0;
            var board = ctx.Players[foe ? 1 - owner : owner].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                if (u == null || !u.IsAlive) continue;
                // `troop(s)` 排除督军（原版 `_fe_count` 的 `troop_only` 过滤）
                if (kind == "troop" && u.IsWarlord) continue;
                // `for each damaged enemy` —— 受损 = 当前血 < 上限
                if (damagedOnly && u.Health >= u.MaxHealth) continue;
                count++;
            }
            return count;
        }

        /// <summary>
        /// `give` / `gain`（<paramref name="sign"/> = +1）与 `lose`（-1）。
        /// 载荷解释交给 <see cref="GivePayload"/>，施加照 `rule_core._apply_gain:3266`。
        /// </summary>
        static bool DoGive(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved, int sign)
        {
            var payload = GivePayload.Parse(op.Payload);
            if (payload == null)
            {
                ctx.Log($"{by}：「{op.Source}」的载荷「{op.Payload}」本版不认识 —— **这条没生效**");
                unresolved.Add(op.Source + "（载荷 " + op.Payload + " 不认识）");
                return false;
            }

            // 无目标 = 原版落到己方全体（`rule_core.gd:3137`，原版自己标为近似）
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：己方全体)", Side = "own", Kind = "unit", Count = 0, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」没有合法目标，空过");
                return true;
            }
            if (targets.Count > 1 && op.Target != null && op.Target.Count == 1 && !op.Target.Auto)
            {
                // 要多目标却只写了一个（如 `a friendly troop`）→ 取第一个，别默默全加
                targets.RemoveRange(1, targets.Count - 1);
            }

            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                foreach (var p in payload)
                {
                    // 关键词没机制 → **说出来**（原版靠 `KW_IMPLEMENTED` 静默忽略，我们不吃这个亏）
                    if (p.IsKeyword && !KeywordTable.Implemented.Contains(p.Keyword))
                    {
                        ctx.Log($"{by}：「{op.Source}」要给 {t.Name} 加「{p.Keyword}」，"
                              + "但本版**还没有这个关键词的机制** —— 加上了也不会生效");
                        unresolved.Add(op.Source + "（关键词 " + p.Keyword + " 没机制）");
                        continue;
                    }
                    if (p.IsEmbedded)
                    {
                        GrantEmbeddedAbility(ctx, t, p.Embedded, by, op.Source, unresolved);
                        continue;
                    }
                    if (p.IsKeyword && p.Keyword == KeywordTable.DarkPact)
                    {
                        GrantDarkPact(ctx, owner, t, p.Variant, by);
                        continue;
                    }
                    ApplyOneGain(ctx, owner, t, p, sign, op.Duration, by);
                }
            }
            ctx.Log($"{by}：「{op.Source}」给了 {targets.Count} 个目标");
            return true;
        }

        /// <summary>
        /// 属性名规范化。`GivePayload` 会给出 `melee` —— 它和 `attack` 是**同一个字段**
        /// （原版 `rule_core.gd:3325` 把 `melee/might/fist/strength` 一并归到 `attack`，
        ///  见 `GivePayload.ReAttr` 那条注释）。
        /// ⚠️ 2026-09-12 撞到：这里原来是 `switch (p.Attr)`，`melee` 一个 case 都没命中 →
        ///    **黑暗契约的「+2 近战」静默丢失**（解析出来了、日志说给了、数没变）。
        /// </summary>
        static string NormalizeAttr(string attr)
        {
            if (attr == "melee") return "attack";
            return attr;
        }

        /// <summary>
        /// 把**一项**载荷落到一个单位上（`rule_core._apply_gain:3266`）。
        /// </summary>
        /// <param name="grantSource">非空时，属性增减益改走 <see cref="UnitState.RecordGrant"/>
        /// —— 记「这是谁给的」，好让来源消失时整份收回（黑暗契约被替换就是这种情况）。
        /// 为空 = 老行为（直接改字段，没有收回的账）。</param>
        static void ApplyOneGain(BattleContext ctx, int owner, UnitState u, PayloadOp p,
                                 int sign, string duration, string by, string grantSource = null)
        {
            int val = p.Value * sign;

            if (p.IsKeyword)
            {
                if (val >= 0) u.AddKeyword(p.Keyword, val);
                else u.RemoveKeyword(p.Keyword, -val);
            }
            else if (grantSource != null && p.Attr != "energy")
            {
                u.RecordGrant(NormalizeAttr(p.Attr), val, grantSource);
            }            else
            {
                switch (NormalizeAttr(p.Attr))
                {
                    case "attack": u.Attack += val; break;
                    case "ranged": u.RangedAttack += val; break;
                    // 生命增减**连上限一起动**（原版 `:3317`）；负值别把上限压到 0 以下
                    case "health":
                        u.Health += val;
                        u.MaxHealth = System.Math.Max(1, u.MaxHealth + val);
                        break;
                    // 负甲 clamp 0（原版 `:3320`：防「负甲反而加伤」）
                    case "armour": u.Armor = System.Math.Max(0, u.Armor + val); break;
                    case "energy":
                        ctx.Players[owner].Energy += val;
                        break;
                }
            }

            // 限时 → 记一条，到期撤
            if (!string.IsNullOrEmpty(duration) && p.Attr != "energy")
            {
                u.AddTempBuff(new UnitState.TempBuff
                {
                    IsKeyword = p.IsKeyword,
                    Name = p.IsKeyword ? p.Keyword : p.Attr,
                    Value = val,
                    Owner = owner,
                    UntilMyNextTurn = duration == "nextturn",
                    Src = by,
                });
            }
        }

        // ==================================================================
        //  黑暗契约（Dark Pacts）
        // ==================================================================

        /// <summary>
        /// 黑暗契约的四种。**逐条照抄 `rule_core.gd:1655` 的 `DARK_PACT_FX`**；
        /// 规则书 :179 的中文译名也对得上（鲜血 / 纵欲 / 命运 / 韧性）。
        /// ⚠️ 顺序照抄原版字典的字面顺序 —— `random` 掷骰按这个序，改顺序会改对局结果。
        /// </summary>
        static readonly string[][] DarkPactFx =
        {
            new[] { "fate",       "Give +2 Health and Camouflage" },
            new[] { "blood",      "Give +2 Melee Attack and Vanguard" },
            new[] { "excess",     "Give +2 Melee Attack and +2 Ranged Attack" },
            new[] { "resilience", "Give +1 Health and Regeneration 1" },
        };

        /// <summary>
        /// 给单位授予一份黑暗契约。**三件事照原版 `_grant_dark_pact`（`:1663`）的顺序**：
        ///   ① 记下契约种类（`kws.darkpact = 种类`）
        ///   ② 单位自己的 `After receiving a Dark Pact, …` 段落 —— 立刻结算
        ///   ③ 同伴单位（原版是 Havoc）的 `When a friendly troop receives a Dark Pact, …` —— 广播一次
        ///   ④ 契约自身的效果（<see cref="DarkPactFx"/>）**永久**给它加上
        ///
        /// ⚠️ 和原版的两处**有意的不同**，都写在注释里免得下一个会话当成 bug 改回去：
        ///   · 随机种类走 `ctx.Rng`（种子化）而不是 `randi()` —— 本工程要求**同一局永远可复现**
        ///     （见 `RuleCore` 文件头）。
        ///   · 原版提取效果文字时拿的是**小写副本**，于是 `EffectSpec.Parse` 永远失配（等于这一步没生效）。
        ///     我们保留原始大小写。**这是补原版的漏，不是抄错。**
        /// </summary>
        public static void GrantDarkPact(BattleContext ctx, int owner, UnitState u, string kind, string by)
        {
            if (u == null) return;

            string k = kind;
            if (string.IsNullOrEmpty(k) || k == "random" || !HasPact(k))
                k = DarkPactFx[ctx.Rng.Next(DarkPactFx.Length)][0];

            // 同一单位同时只能有一份契约（原版 `u["kws"]["darkpact"] = k` 是**赋值**不是累加）。
            // ⚠️ 但原版那一下**只改了自己那个键**，前一份契约给的增益（伪装 / 再生 / 先锋…）
            //    会**永远留在身上** —— 换成「韧性」之后还带着「命运」的伪装。
            //    我们按卡面语义撤干净：把上一份契约带的增益一起收走（见 `PactGrantedKeywords`）。
            string old = null;
            _keywordsPact.TryGetValue(u, out old);
            if (!string.IsNullOrEmpty(old))
            {
                foreach (string kw in PactGrantedKeywords(old)) u.RemoveAll(kw);
                u.RevertGrantsFrom(old);                 // 属性加成（+2 生命 / +2 近战…）也一并收回
                ctx.Log($"{u.Name} 的旧契约「{PactNameZh(old)}」被替换，它给的增益一并收回");
            }
            u.RemoveAll(KeywordTable.DarkPact);
            u.AddKeyword(KeywordTable.DarkPact, 1);
            _keywordsPact[u] = k;                        // 种类记在旁表（UnitState 只有 <string,int>）
            ctx.Log($"{by}：{u.Name} 获得「{PactNameZh(k)}」黑暗契约");

            var card = u.Card;

            // ② 单位自己的「收到契约后…」
            var selfFx = card != null ? card.Effect("after receiving a dark pact") : null;
            if (selfFx != null)
            {
                ctx.Log($"{u.Name} 的「After receiving a Dark Pact」触发");
                ResolveEffect(ctx, owner, u, selfFx, null);
            }

            // ③ 同伴广播：「当友方部队收到黑暗契约时…」（原版只找**第一个**这样的单位就 break）
            var ps = ctx.Players[owner];
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var hv = ps.Board[s];
                if (hv == null || hv == u) continue;
                var fx = hv.Card != null ? hv.Card.Effect("when a friendly troop receives a dark pact") : null;
                if (fx == null) continue;
                ctx.Log($"{hv.Name} 的「When a friendly troop receives a Dark Pact」触发");
                ResolveEffect(ctx, owner, hv, fx, null);
                break;
            }

            // ④ 契约自身效果 —— 永久（不带时长）
            string payloadText = null;
            foreach (var pair in DarkPactFx) if (pair[0] == k) payloadText = pair[1];
            var ops = GivePayload.Parse(payloadText);
            if (ops == null)
            {
                ctx.Log($"{by}：契约「{k}」的效果文字「{payloadText}」本版解析不了 —— **这份契约没有实际效果**");
                return;
            }
            foreach (var p in ops)
            {
                if (p.IsKeyword && !KeywordTable.Implemented.Contains(p.Keyword))
                {
                    ctx.Log($"{by}：契约「{k}」要给 {u.Name} 加「{p.Keyword}」，但本版没这个机制 —— 跳过");
                    continue;
                }
                ApplyOneGain(ctx, owner, u, p, +1, "", by + "（黑暗契约）", k);
            }
        }

        /// <summary>
        /// 契约种类的旁表：`UnitState` 的关键词字典是 `&lt;string,int&gt;`，装不下 `blood`/`fate` 这种名字。
        /// 不塞进 `UnitState` 是为了不把「一个阵营的机制名词」渗进通用单位状态。
        /// </summary>
        static readonly Dictionary<UnitState, string> _keywordsPact = new Dictionary<UnitState, string>();

        /// <summary>
        /// 每种契约**会授予哪些关键词**（用来在契约被替换时收回去）。
        /// ⚠️ 必须和 <see cref="DarkPactFx"/> 的文本**对得上** —— 那是同一件事的两种写法。
        /// 属性增减益（+2 生命…）走 `UnitState.RecordGrant` 那条路，不用在这里列。
        /// </summary>
        static string[] PactGrantedKeywords(string kind)
        {
            switch (kind)
            {
                case "fate": return new[] { "camouflage" };
                case "blood": return new[] { "vanguard" };
                case "excess": return new string[0];
                case "resilience": return new[] { "regeneration" };
                default: return new string[0];
            }
        }

        /// <summary>这个单位身上的契约种类；没有契约返回 null</summary>
        public static string PactOf(UnitState u)
        {
            string k;
            return (u != null && _keywordsPact.TryGetValue(u, out k)) ? k : null;
        }

        static bool HasPact(string k)
        {
            foreach (var pair in DarkPactFx) if (pair[0] == k) return true;
            return false;
        }

        static string PactNameZh(string k)
        {
            switch (k)
            {
                case "blood": return "鲜血";
                case "excess": return "纵欲";
                case "fate": return "命运";
                case "resilience": return "韧性";
                default: return k;
            }
        }

        static bool DoEnergy(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var pay = GivePayload.Parse(op.Payload);
            int amount = 0;
            if (pay != null) foreach (var p in pay) amount += p.Value;
            if (amount == 0) amount = op.Amount;
            var ps = ctx.Players[owner];
            ps.Energy = System.Math.Min(ps.MaxEnergy, ps.Energy + amount);
            ctx.Log($"{by}：「{op.Source}」回了 {amount} 点能量（现 {ps.Energy}/{ps.MaxEnergy}）");
            return true;
        }

        static bool DoRefill(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var ps = ctx.Players[owner];
            ps.Energy = op.Amount > 0 ? System.Math.Min(ps.MaxEnergy, ps.Energy + op.Amount) : ps.MaxEnergy;
            ctx.Log($"{by}：「{op.Source}」能量 → {ps.Energy}/{ps.MaxEnergy}");
            return true;
        }

        // ==================================================================
        //  条件
        // ==================================================================

        /// <summary>
        /// 条件成不成立。**返回 false = 「本版判不了」**（不是「不成立」）—— 调用方必须区别对待。
        /// 出处：`rule_core.gd:2635` / `:2652` / `:2737`。
        /// </summary>
        static bool ConditionHolds(BattleContext ctx, int owner, EffectOp op,
                                   UnitState chosen, out bool holds)
        {
            holds = false;
            switch (op.ConditionKind)
            {
                case "targetdies":
                {
                    // 「目标死了没有」—— 判的是**上一条效果的目标**
                    var t = ctx.LastTarget;
                    holds = t == null || !t.IsAlive;
                    return true;
                }
                case "alreadyhas":
                {
                    // 「目标身上已经有 X 了吗」——判的是选定的/上一句的目标（原版 `:2652`）
                    var t = chosen != null ? chosen : ctx.LastTarget;
                    if (t == null) return false;
                    string kw = ExtractKeywordFromCondition(op.Condition);
                    if (kw == null) return false;          // 条件里的关键词我们不认识 → 判不了
                    holds = t.Has(kw);
                    return true;
                }
                case "deaths":
                {
                    // 「有单位死过吗」—— 本版**没有**死亡流水，判不了
                    return false;
                }
                // ---- 2026-09-12 补的四类（原先一律判不了 → 整条效果不生效）----
                case "targetsurvives":
                {
                    // `If the target survives, heal 1-5 to it` —— 和 `targetdies` 正好是一对。
                    // 判的是**上一条效果打中的那个单位**（`ctx.LastTarget`），没打中过就算不成立。
                    var t = ctx.LastTarget;
                    holds = t != null && t.IsAlive;
                    return true;
                }
                case "targethasarmour":
                {
                    // `If it has Armour, deal 8 damage instead`（原版 `:2635` 条件伤害那一支）。
                    // 「有没有护甲」= `Armor > 0`（护甲的唯一来源是 `Armour X` 关键词，见 `UnitState`）。
                    var t = chosen ?? ctx.LastTarget;
                    if (t == null) return false;
                    holds = t.Armor > 0;
                    return true;
                }
                case "controlcount":
                {
                    // `If you don't control any, create an Intercessor in your hand`
                    // —— 目前实测只有「一个都没有」这一种写法，按它判。
                    // ⚠️ `If you control 3 or more…` 那种**带数字**的还没遇到，遇到要单独解数字，
                    //    现在一律当作「没控制任何部队」会判错，所以这里显式排除带数字的条件。
                    if (System.Text.RegularExpressions.Regex.IsMatch(op.Condition ?? "", @"\d"))
                        return false;
                    holds = CountTroops(ctx, owner) == 0;
                    return true;
                }
                case "noeffect":
                {
                    // `If no enemy is affected, …` —— 取决于上一条效果**打空了没有**。
                    // 判据：`ctx.LastTarget` 还停在上一句打中的目标上（`ResolveTargets` 会写它）。
                    // ⚠️ 这一条只在「紧跟在一条伤害/效果之后」时才有意义；`LastTarget` 为空
                    //    表示前面那句一个目标都没选到 → 条件成立。
                    holds = ctx.LastTarget == null;
                    return true;
                }
                case "damaged":
                {
                    var t = chosen ?? ctx.LastTarget;
                    if (t == null) return false;
                    holds = t.Health < t.MaxHealth;
                    return true;
                }
                case "istype": return false;      // 「如果是载具」—— 本版没做兵种字段
                case "foreach": return false;     // for-each 计数层还没做
                case "energycheck": return false;
                default: return false;
            }
        }

        /// <summary>
        /// 把载荷里的**数值**换成结算层算好的那个（`+2 melee attack` → `+4 melee attack`）。
        ///
        /// 为什么要这一步：`give` / `gain` / `lose` 的数值**不在 `EffectOp.Amount` 里**，
        /// 而是**载荷原文里的数字**，由 `GivePayload` 现解（见 `ApplyOneGain`）。
        /// `for each` 的增量只改 `Amount` 的话，载荷还是原值 —— 增量静默丢掉。
        ///
        /// ⚠️ 只替换**第一个**数字：载荷里通常只有一个数（`armour 2` / `+2 health` / `+2 melee attack`）。
        /// </summary>
        static string ScalePayload(string payload, int newAmount)
        {
            if (string.IsNullOrEmpty(payload)) return payload;
            // 取载荷里**第一个数字**当基础值 —— `+2 melee attack` / `armour 2` / `+2 health` 都是这个形状。
            // ⚠️ 不能拿 `op.Amount` 当基准：那个字段对 `give` 载荷经常是 0
            //（`Give +2 Melee Attack …` 的 `+2` 只在载荷里，解析层没往 `Amount` 写）——
            // 2026-09-12 撞到：按 `Amount` 找数字永远找不到，增量静默丢掉。
            var m = System.Text.RegularExpressions.Regex.Match(payload, @"\d+");
            if (!m.Success) return payload;
            string oldStr = m.Value;
            int oldVal = int.Parse(oldStr);
            if (newAmount == oldVal) return payload;
            // 保留原来的正负号：`+2` → `+4`（换完数字，前面的 `+` 不动）
            return payload.Substring(0, m.Index) + newAmount + payload.Substring(m.Index + oldStr.Length);
        }

        /// <summary>某方场上**部队**（不含督军）的数量</summary>
        static int CountTroops(BattleContext ctx, int owner)
        {
            int n = 0;
            var b = ctx.Players[owner].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (b[s] != null && !b[s].IsWarlord && b[s].IsAlive) n++;
            return n;
        }

        /// <summary>从 `If it already had Hunt Mark` 里抠出关键词名；认不出返回 null</summary>
        static string ExtractKeywordFromCondition(string cond)
        {
            if (string.IsNullOrEmpty(cond)) return null;
            string c = cond.ToLowerInvariant();
            foreach (var kw in KeywordTable.Implemented)
                if (c.Contains(kw)) return kw;
            return null;
        }
    }
}
