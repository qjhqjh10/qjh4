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
                                     out List<string> unresolved,
                                     CardDef sourceCard = null)
        {
            unresolved = new List<string>();
            if (ops == null) return 0;
            string by = source != null ? source.Name : "战术卡";

            // **这次结算的是哪张卡** —— 只有「常驻效果登记」需要它：
            // 规则书英文版 `:39-41` 要求把写「For the rest of this battle」的卡**单独放一摞备查**，
            // 所以 `PersistentEffect` 得记着来源是哪张。别处一律不看这个字段。
            ctx.PlayingCard = sourceCard;

            // 一张卡的结算 = **一个计数窗口**：`For each troop drawn …` 数的是这张卡自己抽到的牌
            // （见 `BattleContext.DrawnThisResolve`）。嵌套结算（Rally 触发里再打一张牌）会各开各的窗口。
            ctx.DrawnThisResolve.Clear();

            // 「上一次选牌挑中的那张」也按**一张卡一个窗口**清 —— 同一张卡里的分句要能互相指代
            // （`Choose a troop from your deck.` + `Create a copy of it in your hand` 是**两条分句**），
            // 但**不能**漏到下一张卡去：不然一张卡里的 `create a copy of it` 会复制到
            // 上一张卡选中的那张牌（2026-09-13 加选牌 handler 时定的口径）。
            ctx.LastChosenCard = null;

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

                // ---- 数值 = 某个阵营资源：`Refill Energy equal to your Faith`（2026-09-13 第三十三轮）----
                // ⚠️ 必须**递副本**下去，不能改 `op.Amount` 本身 —— 理由同下面那条注释：
                //    `EffectOp` 列表是**解析产物、会被复用**（同一张卡第二次打出时是同一份对象）。
                if (!string.IsNullOrEmpty(op.AmountRef))
                {
                    var ps = ctx.Players[owner];
                    int n = op.AmountRef == "faith" ? ps.Faith
                          : op.AmountRef == "spirit" ? ps.SpiritStones : -1;
                    if (n < 0)
                    {
                        ctx.Log($"{by}：「{op.Source}」的数值要取「{op.AmountRef}」，本版不认识这个资源 "
                              + "—— **这条没生效**");
                        unresolved.Add(op.Source + "（数值来源认不出：" + op.AmountRef + "）");
                        continue;
                    }
                    var refOp = op.Clone();
                    refOp.Amount = n;
                    // `give` / `gain` / `lose` 的数值写在**载荷原文**里（同 for-each 那条注释）
                    if (n != 0 && (op.Verb == "give" || op.Verb == "gain" || op.Verb == "lose"))
                        refOp.Payload = ScalePayload(op.Payload, n);
                    ctx.Log($"{by}：「{op.Source}」的数值 = 你的"
                          + (op.AmountRef == "faith" ? "信仰" : "灵魂石") + $" {n}");
                    // ⚠️ `n == 0` 照常递下去 —— `Deal 0 damage` / `Refill 0` 是「生效了但数值是 0」，
                    //    不是「这条没生效」（报错了玩家会以为卡坏了）。
                    if (ResolveOne(ctx, owner, source, by, refOp, chosen, unresolved)) done++;
                    continue;
                }

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
            // 「相邻」的 `Self` 锚点要知道**是谁在施放**（见 `ResolveAdjacentAnchor`）。
            // 它收在**上下文**里而不是给结算函数加参数：结算函数是一张**统一签名的表**
            // （`EffectDispatch`，28 个 handler），为一个字段去改 28 个签名 + 整张表，改动面不成比例。
            // 保存/恢复是必须的：效果会**嵌套**（`Repeat this effect` / `Choose one` 里再调 `ResolveOps`），
            // 不恢复的话外层后半段会拿到内层的施放者。
            var prevActing = ctx.ActingUnit;
            ctx.ActingUnit = source;
            try { return ResolveOneCore(ctx, owner, source, by, op, chosen, unresolved); }
            finally { ctx.ActingUnit = prevActing; }
        }

        static bool ResolveOneCore(BattleContext ctx, int owner, UnitState source, string by,
                                   EffectOp op, UnitState chosen, List<string> unresolved)
        {
            // ---- 付费激活：付不起就**整段不结算**（原版 `rule_core.gd:2529`）----
            // ⚠️ **2026-09-13 第三十三轮修了一条真 bug**：这里原来**只扣能量、完全不看 `CostKind`**。
            //    于是 `8 [Faith]: Deploy an additional Battle Sister` 那族卡**一直在偷偷扣能量** ——
            //    卡是 4 费的，要求 8 能量等于「永远不激活」，所以**没人发现**。这是标准的静默错行为。
            //    货币名 → 规范取值只有一处判据（`EffectText.CostKindOf`），这里不再自己认字符串。
            if (op.Cost > 0)
            {
                var ps = ctx.Players[owner];
                string kind = EffectText.CostKindOf(op.CostKind);
                if (kind != "" && kind != "energy" && kind != "faith" && kind != "spirit" && kind != "oath")
                {
                    // 认不出的货币（`might` / `attack` / `icon` …）**明说不支持**，别默默扣能量
                    ctx.Log($"{by}：「{op.Source}」要付 {op.Cost} 点「{op.CostKind}」，"
                          + "这个货币本版不认识 —— **这一条没生效**");
                    unresolved.Add(op.Source + "（付费货币认不出：" + op.CostKind + "）");
                    return false;
                }
                int have = kind == "faith" ? ps.Faith
                         : kind == "spirit" ? ps.SpiritStones
                         : ps.Energy;
                string shown = kind == "faith" ? "信仰" : kind == "spirit" ? "灵魂石" : "能量";
                if (have < op.Cost)
                {
                    ctx.Log($"{by}：「{op.Source}」需要 {op.Cost} 点{shown}才激活，不够 —— **这一条没生效**");
                    unresolved.Add(op.Source + "（付费不够）");
                    return false;
                }
                if (kind == "faith") ps.Faith -= op.Cost;
                else if (kind == "spirit") ps.SpiritStones -= op.Cost;
                else ps.Energy -= op.Cost;
                ctx.Log($"{by} 付了 {op.Cost} 点{shown}激活「{op.Source}」");

                // 🆕 2026-09-13 A3：**花灵魂石激活一个「灵魂石能力」** ⇒ 广播事件。
                //    卡面：`When you trigger a Spirit Stone ability, gain Sniper and +1 Ranged Attack`
                //    （`Bright Lance Vyper`）。语义与出处（`AbilityTrigger.UseSpiritStone = 600` /
                //    `BattleActionType.useWaystone = 76` → `triggerSpiritStone = 77`）写在
                //    `WhenEvent.SpiritAbility` 的注释里。
                //    ⚠️ 卡面写的是 `**you** trigger …` ⇒ 这是**玩家级**事件（`who` = 付石那一方），
                //       `card`/`subject` 都传 `null`（和 `When you gain Faith` 同一类）。
                //    ⚠️ **实测卡池 0 张带这种前缀** ⇒ 这条广播现在**发不出来**（监听器点亮但不响），
                //       和 `Ravenwing Champion` 同类。留着是对的，但别当成「已经铺完」。
                if (kind == "spirit")
                    BroadcastWhen(ctx, WhenEventKind.SpiritAbility, owner, null, null);
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

            EffectHandler h;
            if (EffectDispatch.TryGetValue(op.Verb ?? "", out h))
                return h(ctx, owner, by, op, chosen, unresolved);

            ctx.Log($"{by}：「{op.Source}」的动作「{op.Verb}」本版还没实现 —— **这条没生效**");
            unresolved.Add(op.Source + "（动词 " + op.Verb + " 没实现）");
            return false;
        }

        /// <summary>一个动词的结算函数。签名统一，好放进表里。</summary>
        public delegate bool EffectHandler(BattleContext ctx, int owner, string by, EffectOp op,
                                           UnitState chosen, List<string> unresolved);

        /// <summary>
        /// **动词 → 结算函数**。**只此一处** —— `ImplementedEffectVerbs` 从这张表**派生**。
        ///
        /// 为什么要收成表（2026-09-12）：以前是 `switch` + 另一个手写的 `ImplementedEffectVerbs` 清单，
        /// **两张表要手工对齐**。忘了加后者 → 卡「能打但不报缺机制」；忘了加前者 →
        /// 「报了缺机制但其实能打」。两种都是**静默**的，而且加动词时最容易踩。
        /// 现在加一个动词 = 在下面加一行，覆盖率那层自动跟上。
        /// </summary>
        public static readonly Dictionary<string, EffectHandler> EffectDispatch =
            new Dictionary<string, EffectHandler>
        {
            { "deal",       (c, o, b, op, ch, un) => DoDeal(c, o, b, op, ch, un) },
            // `Each of your units deals damage equal to its Shuriken to a random enemy`（`Sudden Assault`）
            { "eachunitdeal",(c, o, b, op, ch, un) => DoEachUnitDeal(c, o, b, op) },
            // 强制攻击族（`Make … attack by itself` / `Target … attacks …` / 裸 `Attack …`）
            { "forceattack",(c, o, b, op, ch, un) => DoForceAttack(c, o, b, op, ch, un) },
            // `6 [Energy]: Extend effect until your next turn` / `8 [Energy]: Give it permanently`
            { "paidmod",    (c, o, b, op, ch, un) => DoPaidMod(c, o, b, op, ch, un) },
            { "heal",       (c, o, b, op, ch, un) => DoHeal(c, o, b, op, ch, un) },
            { "draw",       (c, o, b, op, ch, un) => DoDraw(c, o, b, op) },
            { "drawtype",   (c, o, b, op, ch, un) => DoDrawType(c, o, b, op) },
            { "drawref",    (c, o, b, op, ch, un) => DoDrawRef(c, o, b, op, un) },
            { "return",     (c, o, b, op, ch, un) => DoReturn(c, o, b, op, un) },
            { "destroy",    (c, o, b, op, ch, un) => DoDestroy(c, o, b, op, ch, un) },
            { "stun",       (c, o, b, op, ch, un) => DoStun(c, o, b, op, ch, un) },
            { "blind",      (c, o, b, op, ch, un) => DoBlind(c, o, b, op, ch, un) },
            // 🆕 2026-09-13 A4：两个小原子（`Does nothing` / `Reload the Duty …`）
            { "noeffect",   (c, o, b, op, ch, un) => DoNoEffect(c, o, b, op) },
            { "costwhen",   (c, o, b, op, ch, un) => DoCostWhenStub(c, o, b, op) },
            // `This costs N less if you control a unit with <关键词>` 的**标记 op**（见 `EffectText.TryCostIfControl`）
            { "costifcontrol", (c, o, b, op, ch, un) => DoCostIfControlStub(c, o, b, op) },
            { "sethealth",  (c, o, b, op, ch, un) => DoSetHealth(c, o, b, op, ch, un) },
            { "reloadduty", (c, o, b, op, ch, un) => DoReloadDuty(c, o, b, op, un) },
            { "give",       (c, o, b, op, ch, un) => DoGive(c, o, b, op, ch, un, +1) },
            { "gain",       (c, o, b, op, ch, un) => DoGive(c, o, b, op, ch, un, +1) },
            { "gainenergy", (c, o, b, op, ch, un) => DoEnergy(c, o, b, op) },
            // 阵营资源（2026-09-13 第三十三轮）：给**玩家自己**的计数器加，不是给单位加
            { "gainfaith",  (c, o, b, op, ch, un) => DoFactionResource(c, o, b, op, "faith",  un) },
            { "gainspirit", (c, o, b, op, ch, un) => DoFactionResource(c, o, b, op, "spirit", un) },
            { "gainquest",  (c, o, b, op, ch, un) => DoFactionResource(c, o, b, op, "quest",  un) },
            { "spendspirit",(c, o, b, op, ch, un) => DoSpendSpirit(c, o, b, op, un) },
            { "lose",       (c, o, b, op, ch, un) => DoGive(c, o, b, op, ch, un, -1) },
            { "refill",     (c, o, b, op, ch, un) => DoRefill(c, o, b, op) },
            { "chooseone",  (c, o, b, op, ch, un) => DoChooseOne(c, o, b, op, ch, un) },
            { "choosecard", (c, o, b, op, ch, un) => DoChooseCard(c, o, b, op, ch, un) },
            { "persist",    (c, o, b, op, ch, un) => DoPersist(c, o, b, op, un) },
            { "atturn",     (c, o, b, op, ch, un) => DoAtTurn(c, o, b, op, un) },
            { "costmore",   (c, o, b, op, ch, un) => DoCostMore(c, o, op, un) },
            { "create",     (c, o, b, op, ch, un) => DoCreate(c, o, b, op, un) },
            { "deploy",     (c, o, b, op, ch, un) => DoDeploy(c, o, b, op, un) },
            { "lowercost",  (c, o, b, op, ch, un) => DoLowerCost(c, o, b, op, un) },
            { "repeat",     (c, o, b, op, ch, un) => DoRepeat(c, o, b, op, ch, un) },
            { "reanimate",  (c, o, b, op, ch, un) => DoReanimate(c, o, b, op, un) },
        };

        /// <summary>
        /// 抽一张**指定兵种/类别**的牌（`Draw a troop` / `Draw a Vehicle from your deck`）——
        /// 「定向翻找」。
        ///
        /// **语义出处：规则书 L461-466**（`rule_core.gd:4650` 的 `_draw_specific_type` 逐条照抄它）：
        ///   > Draw cards from your deck until you draw a matching card, put that card into your hand
        ///   > or deploy it as indicated, **then shuffle your deck**.
        /// 照抄的三个细节：
        ///   ① 从**牌库顶往下翻**（= 从列表末尾往前，和 `Draw` 同一边），
        ///      **翻过去的牌留在牌库**（不是「抽出来再看」，是「找」）；
        ///   ② 找到 N 张就停；**一张都没找到就什么都不发生**（只写日志）；
        ///   ③ 找到了才**洗牌**（「then shuffle your deck」）—— 用 `ctx.Rng`，同一局可复现。
        /// </summary>
        static bool DoDrawType(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var ps = ctx.Players[owner];
            string kind = op.Payload ?? "";
            int want = op.Amount > 0 ? op.Amount : 1;
            int got = 0;
            var taken = new List<CardDef>();
            for (int i = ps.Deck.Count - 1; i >= 0 && got < want; i--)
            {
                var c = ps.Deck[i];
                if (c == null || !CreatePool.MatchesKind(c, kind)) continue;
                ps.Deck.RemoveAt(i);
                taken.Add(c);
                got++;
            }
            foreach (var c in taken) ps.Hand.Add(c);
            EnforceHandLimit(ctx, owner);

            // `For each troop drawn …` 要数这个（和 `DoDraw` 同一条路）
            foreach (var c in taken) ctx.DrawnThisResolve.Add(c);

            if (got == 0)
            {
                ctx.Log($"{by}：「{op.Source}」翻遍了牌库也没找到「{kind}」—— 什么都没抽到");
                return true;                       // 找不到是**正常空过**，不是失败
            }
            Shuffle(ps.Deck, ctx.Rng);             // 规则书：「then shuffle your deck」
            ctx.Log($"{by}：「{op.Source}」定向翻找「{kind}」拿到 {got} 张（{Names(taken)}），牌库已洗");
            return true;
        }

        static string Names(List<CardDef> list)
        {
            string s = "";
            foreach (var c in list) { if (s.Length > 0) s += "、"; s += c.Name; }
            return s;
        }

        static string Names(List<UnitState> list)
        {
            string s = "";
            foreach (var u in list) { if (s.Length > 0) s += "、"; s += u.Name; }
            return s;
        }

        /// <summary>这个单位在**哪一方、哪一格**（找不到返回 false）。棋盘只有 9 格，直接扫。</summary>
        static bool FindSlot(BattleContext ctx, UnitState u, out int player, out int slot)
        {
            player = -1; slot = -1;
            if (u == null) return false;
            for (int p = 0; p < 2; p++)
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (ctx.Players[p].Board[s] == u) { player = p; slot = s; return true; }
            return false;
        }

        /// <summary>
        /// 「相邻」的**锚点单位** —— 相对**谁**算相邻（见 <see cref="AdjacentAnchor"/>）。
        /// 认不出返回 `null`，调用方**空过并如实报**（绝不退回「整个池子」）。
        ///
        /// ⚠️ `Self` / `ActingCard` 靠 `source` —— 单位触发的正文（`Strike:` / `Rally:` / `Mob:` /
        ///    `Codex:`）有；**战术卡那条路 `source` 是 null**（`ResolveOps(ctx, p, null, …)`），
        ///    所以「战术卡 + 裸 adjacent」这一支**认不出**（实测 36 张里没有这种卡，见交接文档）。
        /// </summary>
        static UnitState ResolveAdjacentAnchor(BattleContext ctx, int owner, EffectTargetSpec spec,
                                               UnitState source, UnitState chosen)
        {
            switch (spec.Anchor)
            {
                case AdjacentAnchor.Self:
                case AdjacentAnchor.ActingCard:
                    return source;
                case AdjacentAnchor.PreviousTarget:
                    // 玩家点的那一个优先，其次才是上一条效果选中的（`ctx.LastTarget`）
                    return chosen ?? ctx.LastTarget;
                case AdjacentAnchor.TargetIfMeetsCriteria:
                    // 🆕 2026-09-13 A3：**事件宾语优先** —— 原版 `adjacentToTargetIfMeetsCriteria = 110`
                    // 配的就是 `TargetsAffected.target = 30`（事件的目标）。
                    // 实测一张：`Long Fang` 的 `When this unit attacks an enemy with Hunt Mark,
                    // deal 3 damage to adjacent enemies` ⇒ 锚点 = **被打的那个敌人**。
                    // ⚠️ 没有事件宾语时才退回老的「玩家点的 / 上一条效果选中的」——
                    //    这一支原来没人产出（110 一直是死值），所以退回不会改变既有行为。
                    return ctx.EventTarget ?? chosen ?? ctx.LastTarget;
                case AdjacentAnchor.FriendlyWarlord:
                    return ctx.Players[owner].Warlord;
                case AdjacentAnchor.EnemyWarlord:
                    return ctx.Players[1 - owner].Warlord;
                default:
                    return null;                       // Unset = 认不出
            }
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

            // 施放者：调用点一律传 `null`（历史原因），所以从这里回落到上下文的
            // `ActingUnit`（由 `ResolveOne` 在结算每一条效果**之前**设好、结算完恢复）。
            // 「相邻」的 `Self` 锚点要用它 —— 见 `ResolveAdjacentAnchor`。
            if (source == null) source = ctx.ActingUnit;

            // 指代上一条效果的目标（`it` = 一个 / `them` = 一批）
            if (spec.Side == "prev" || spec.Kind == "prev")
            {
                if (spec.Count == 0)
                {
                    // `them` —— 上一条效果**影响到的整批**（谁被打了就治谁，谁被部署了就给它加）
                    foreach (var u in ctx.LastTargets)
                        if (u != null && u.IsAlive) list.Add(u);
                    return list;
                }
                var last = ctx.LastTarget;
                if (last != null && last.IsAlive) list.Add(last);
                return list;
            }

            // ---- `the target of the attack` —— **事件的宾语**（2026-09-13 A3）----
            // 例：`Valtus` 的 `When a friendly unit attacks, deal 3 damage to the target of the attack`。
            // ⚠️ 和上面那条 `prev` **不是一个槽**：攻击事件里 `LastTarget` 是**攻击者**
            //    （监听正文的 `it` 指它），宾语在 `ctx.EventTarget` 里 —— 两个角色同时存在，见那个字段。
            // ⚠️ 拿不到（不在事件里 / 广播没传宾语）⇒ **空过并如实报**，绝不退回「全场挑一个」。
            if (spec.Side == "eventtarget" || spec.Kind == "eventtarget")
            {
                var ev0 = ctx.EventTarget;
                if (ev0 != null && ev0.IsAlive) list.Add(ev0);
                return list;
            }

            // ---- 卡面**没写主语**（`Gain +2 Attack`）：有施放者就是施放者自己 ----
            // 2026-09-13 A3。见 `EffectTargetSpec.Subjectless`（判据的来龙去脉写在那儿）。
            // ⚠️ `source` 上面已经从 `ctx.ActingUnit` 回落过了 —— 单位触发正文那条路有它，
            //    战术卡那条路是 `null`（`ResolveOps(ctx, p, null, …)`）⇒ 那种仍落到己方全体（行为不变）。
            if (spec.Subjectless)
            {
                if (source != null && source.IsAlive) list.Add(source);
                // 战术卡没有施放者，但**玩家点了一个目标**（`chosen`）—— 那就是这张卡说的「谁」。
                // ⚠️ 排在「己方全体」**之前**：`Beacon of Faith` 那类卡的第一次「whose」是
                //    `your units`（`PickTarget` 返回 null ⇒ `chosen` 也是 null）⇒ 仍然落到己方全体 ✓
                //    行为不变；而真点了目标的卡（`Luminous Strike` 那种）就不会再打全体。
                else if (chosen != null && chosen.IsAlive) list.Add(chosen);
                else
                {
                    // 没有施放者、也没点目标：**退回既有近似**（己方全体），并把这件事说清楚 ——
                    // 静默换语义比报一行日志糟得多。
                    AddSide(list, ctx.Players[owner], false, spec.Kind == "troop", -1);
                }
                return list;
            }

            // ---- `… you deploy` / `… you put in play` —— **刚部署上场的那个**（2026-09-13）----
            // 例：`For the rest of this battle, give Shield to all Drones you deploy`。
            //
            // ⚠️ **不能查场上**：那样会把**部署这张卡之前就躺在场上**的同类单位也加一遍 ——
            //    卡面写的是「你**部署**的」，不是「你场上**所有**的」。原版同理：它拿的是事件参数里
            //    那张被召唤的牌（`CardScript__ResolveUnitSummoned.c:33` 的第 5 个实参），不是一次查询。
            //
            // 目标从 `ctx.LastTargets` 取 —— 这是**已有的代词机制**（`RuleCore.ResolveDeploy`
            // 在结算前把那个单位放进去），不另开一份「刚部署」的存放位置。
            if (spec.Deployed)
            {
                var depCrit = CardCriteria.FromTarget(spec);
                foreach (var u in ctx.LastTargets)
                {
                    if (u == null || !u.IsAlive) continue;
                    // 兵种/关键词这件事**在这里也判一次**：触发那一刻已经判过（`ResolveDeploy`），
                    // 但 `Deployed` 也可能出现在别的句子里（不经过那条路），多一道不吃亏。
                    if (depCrit != null && !depCrit.Matches(u.Card)) continue;
                    list.Add(u);
                }
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

            // ---- 兵种 / 已受伤 / 正在祈祷 这三道筛 —— **判据只一份**（见 `ApplyTargetFilters`）----
            // 🔴 2026-09-13 A4 批 2 抽出来的：以前这三道筛只写在**结算**这一侧，
            //    而表现层问「这一格能不能选」走的是 `IsLegalPick`（它只看阵营 + 「排不排除督军」）——
            //    两边口径不一致 ⇒ **拖拽时高亮说能选、松手打出去却空过**
            //    （`CanPlayTactic` 的注释点名过这个毛病：「另写一份会出现…」）。
            //    ⇒ 两处现在都读这一份。
            ApplyTargetFilters(ctx, pool, spec);

            // ---- 「相邻」：目标集 = **锚点那一圈**（过同一套筛选）----
            // 🔴 2026-09-13 候选 F。在此之前 `EffectTargetSpec.Adjacent` **全仓没有消费者** ——
            //    卡面写着「相邻」的那 36 张打的**不是卡面写的目标集**，而且**卡面不打 `*`**
            //    （正文解析是成功的）= 本工程红线点名的**静默失效**。
            //
            // 两条语义都**有出处**（不是挑的）：
            //   ① **相邻 = 锚点所在那一方棋盘行内的左右紧邻格** —— 判据唯一收在 `BoardSpec.AdjacentSlots`
            //      （原来践踏 / 爆裂各内联一份）。原版 `BattleManager.GetAdjacentUnits`（反编译
            //      `decomp_out/BattleManager__GetAdjacentUnits.c`）按单位的**所属方**取那一方的
            //      `MinionManager.GetAdjacentUnits`。
            //   ② **相邻单位要过同一套筛选**（阵营 / 兵种 / 关键词）—— 原版 `CardScript.TargetedSpellPlayed`
            //      对相邻单位逐个调 `criteria.Matches(...)`（`decomp_out/CardScript__TargetedSpellPlayed.c:60-70`）。
            //      这里用 `pool.Contains(u)` 判：`pool` 正是「过了筛选的那一批」。
            if (spec.Adjacent)
            {
                var anchor = ResolveAdjacentAnchor(ctx, owner, spec, source, chosen);
                int ap = -1, aslot = -1;
                if (anchor != null && anchor.IsAlive && FindSlot(ctx, anchor, out ap, out aslot))
                {
                    if (spec.AnchorInSet) list.Add(anchor);       // 卡面点名了锚点本体 ⇒ 它也在目标集里
                    var slots = new List<int>();
                    BoardSpec.AdjacentSlots(aslot, slots);
                    foreach (int sl in slots)
                    {
                        var u = ctx.Players[ap].Board[sl];
                        if (u == null || !u.IsAlive || list.Contains(u)) continue;
                        if (!pool.Contains(u)) continue;          // 过不了同一套筛选 ⇒ 不算相邻目标
                        list.Add(u);
                    }
                    // 取几个：复数字面（`adjacent units`）= 那一圈**全部**；单数（`an adjacent troop`）
                    // 才按个数裁 —— **不掷骰**，按槽号小的优先（和引擎里其它「取第一个」的规矩一致）。
                    // ⚠️ 锚点被点名（`an enemy and its adjacent units`）时 `Count` 指的是**锚点**选几个，
                    //    不能拿去裁邻居（否则「打 3 个」会被裁成 1 个）。
                    int keep = (spec.AdjacentAll || spec.AnchorInSet) ? 0 : spec.Count;
                    if (keep > 0 && list.Count > keep)
                    {
                        if (spec.Random)
                        {
                            while (list.Count > keep) list.RemoveAt(ctx.Rng.Next(list.Count));
                        }
                        else list.RemoveRange(keep, list.Count - keep);
                    }
                    ctx.Log($"（「相邻」：锚点 {anchor.Name}（{ap} 方 {aslot} 号格）→ {list.Count} 个目标"
                          + $"：{Names(list)}）");
                }
                else
                {
                    // ⚠️ **锚点拿不到就空过**，绝不退回「整个池子」—— 那正是「打得比卡面宽」。
                    ctx.Log($"（「相邻」：锚点 {spec.Anchor} 认不出 / 不在场上 ⇒ **这次空过**，"
                          + "不按整个目标池打）");
                }
                ctx.LastTargets.Clear();
                ctx.LastTargets.AddRange(list);
                if (list.Count > 0) ctx.LastTarget = list[0];
                return list;
            }

            if (spec.Count == 0)
            {
                list.AddRange(pool);                       // `all`
            }
            else if (!string.IsNullOrEmpty(spec.PickMost))
            {
                // ---- `the enemy with **highest attack**` / `lowest Health`（2026-09-13 A4 批 2）----
                // 出处：`Target friendly unit attacks the enemy with highest attack`（`Peerless Bladesmen`）·
                //       `Attacks a random enemy with lowest Health`（`Damaged Hexmark`）。
                // 挑法**不掷骰**：并列时取**槽号在前**的那个（和引擎里其它「取第一个」的规矩一致，
                // 也保证同一局可复现 —— 铁律：定死的规则不要改成随机）。
                // ⚠️ `attack` 取的是**近战攻击力**（`UnitState.Attack`）：卡面写的是裸 `attack`，
                //    而远程是单独一栏（`ranged attack`）；本版就这一张卡，按近战解，**如实记在这儿**。
                bool low = spec.PickMost.StartsWith("-");
                string stat = spec.PickMost.Substring(1).Trim();
                UnitState best = null;
                int bestV = 0;
                foreach (var u in pool)
                {
                    if (u == null || !u.IsAlive) continue;
                    int v = StatOf(u, stat);
                    if (best == null || (low ? v < bestV : v > bestV)) { best = u; bestV = v; }
                }
                if (best != null) list.Add(best);
                ctx.Log($"（按「{spec.PickMost}」挑：{Names(pool)} → {best?.Name}）");
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
            // 供下一句 `it` / `them` 指代。**两个都要写**：`it` 取第一个、`them` 取整批
            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(list);
            if (list.Count > 0) ctx.LastTarget = list[0];
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

            // ✅ **防御卡走同一条路**（2026-09-13 第三十三轮）。
            //    以前这里直接 `return ErrUnimplemented`，理由是「引擎里除了组卡校验没有任何地方认识 `defence`」。
            //    实测：39 张防御卡的 desc **39/39 都能完整解析**，动词也都是已实现的那批
            //    （`Deal` / `Give` / `Heal` / `Deploy` / `Choose a … put it in your hand` /
            //     `Your next X costs N less` / `Refill` / `Create`）—— 所以那条理由不成立，撤掉。
            //    规则书 `:105`：防御卡就是「后手可打出的**特殊战术**」（战术大类，见 `rule_core.gd:4696`
            //    「防御卡=计策类」）。⇒ **它和战术卡共用这一份判据**，不另开一条。

            // 解析不了 → 明说不支持。**先于费用判断**：否则一张用不起的卡会报「能量不足」，
            // 把「本版不支持」误导成「再等等就能打」（单位卡那条注释里记着同一个坑）
            if (!EffectText.IsFullyParsed(card.Desc)) return RuleCodes.ErrUnimplemented;
            var ops = EffectText.Parse(card.Desc, out _, out _);

            if (CostOf(ctx, p, card) > ps.Energy) return RuleCodes.ErrCost;

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
            // ⚠️ **必须和结算那一侧读同一份筛**（2026-09-13 A4 批 2 修）——
            //    以前只筛阵营 + 「排不排除督军」，兵种 / 已受伤 / 正在祈祷 三道筛漏在这儿
            //    ⇒ **拖拽时高亮说能选、松手打出去却空过**（那正是 `CanPlayTactic` 注释点名的毛病）。
            //    现在两边都调 `ApplyTargetFilters`，**只此一处**。
            ApplyTargetFilters(ctx, pool, spec, quiet: true);
            return pool.Contains(u);
        }

        /// <summary>
        /// **目标池的三道筛**：兵种（`SubtypeFilter`）/ 已受伤（`DamagedOnly`）/ 正在祈祷（`PrayedOnly`）。
        ///
        /// **判据只此一处** —— 结算（<see cref="ResolveTargets"/>）和「这一格能不能选」
        /// （<see cref="IsLegalPick"/>，表现层拖拽时问的就是它）都调它。
        /// 两边口径一旦分叉，症状是**拖拽高亮说能选、打出去却空过**（`CanPlayTactic` 的注释里
        /// 点过这个毛病，2026-09-13 A4 批 2 真的撞到了：`IsLegalPick` 少筛了三道）。
        ///
        /// ⚠️ **宁可不过滤也不能筛错**：没写条件的一律不动；筛完一个不剩也**不回退**去按整个池子打
        ///    —— 回退正是「打得比卡面宽」那个毛病。查不到兵种的卡（`subtype` 是空串）**保留**在池子里。
        /// </summary>
        /// <param name="quiet">true = 不打日志（表现层拖拽时会疯狂调用，刷屏没意义）</param>
        static void ApplyTargetFilters(BattleContext ctx, List<UnitState> pool, EffectTargetSpec spec,
                                       bool quiet = false)
        {
            if (spec == null) return;

            // ---- 兵种筛（`a friendly Vehicle` / `each friendly Beast`）----
            // 原版数据里有 `subtype` 字段（Infantry / Vehicle / Beast …）。判据见上面那三条纪律。
            if (!string.IsNullOrEmpty(spec.SubtypeFilter))
            {
                int before = pool.Count;
                int unknown = 0;
                foreach (var u in pool)
                    if (u != null && u.Card != null && string.IsNullOrEmpty(u.Card.Subtype)) unknown++;
                pool.RemoveAll(u => u == null || u.Card == null
                                    || (u.Card.Subtype.Length > 0
                                        && !string.Equals(u.Card.Subtype, spec.SubtypeFilter,
                                                          System.StringComparison.OrdinalIgnoreCase)));
                if (!quiet && pool.Count != before)
                    ctx.Log($"（按兵种筛「{spec.SubtypeFilter}」：{before} → {pool.Count}"
                          + (unknown > 0 ? $"，另有 {unknown} 张原版没给兵种、保留在池子里" : "") + "）");
            }

            // ---- 关键词筛（`a troop with Destroyer`）----
            if (!string.IsNullOrEmpty(spec.KeywordFilter))
            {
                int before = pool.Count;
                pool.RemoveAll(u => u == null || u.Card == null || !u.Has(spec.KeywordFilter));
                if (!quiet && pool.Count != before)
                    ctx.Log($"（按关键词筛「{spec.KeywordFilter}」：{before} → {pool.Count}）");
            }

            // ---- `all **damaged** …` —— 只挑失去过生命的 ----
            // 判据照规格书 `rule_core.gd:1415`：**`health < max_health`**。
            if (spec.DamagedOnly)
            {
                int before = pool.Count;
                pool.RemoveAll(u => u == null || u.Health >= u.MaxHealth);
                if (!quiet) ctx.Log($"（按「已受伤」筛：{before} → {pool.Count}）");
            }

            // ---- `… that is Praying` —— 只挑正在祈祷的 ----
            // 判据 = `UnitState.Prayed` —— 和 `anypraying` 那条条件**读同一个字段**。
            if (spec.PrayedOnly)
            {
                int before = pool.Count;
                pool.RemoveAll(u => u == null || !u.Prayed);
                if (!quiet) ctx.Log($"（按「正在祈祷」筛：{before} → {pool.Count}）");
            }
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

            int paid = CostOf(ctx, p, card);
            ps.Energy -= paid;
            ps.Hand.RemoveAt(handIdx);
            ctx.Log($"{ps.Name} 打出战术卡「{card.Name}」（{paid} 能）");
            // 战术卡到这儿才算真打出去 —— 事件要在**校验与扣费都过了之后**发
            // （和单位卡那条 `Play` 对称，日志/表现层两边都能看见战术卡）
            ctx.Emit(EvtKind.Play, p, targetSlot, card.Name);
            // 🆕 `When you play a Stratagem, …` / `When your opponent plays a Stratagem, …`
            // （2026-09-13 第三十三轮）。**排在 `Emit` 之后、结算之前** ——
            // 「打出了」这个事实发生在效果结算之前；监听方看到的就是「对面刚打了一张计策」。
            BroadcastWhen(ctx, WhenEventKind.Play, p, card, null);

            var unresolved = new List<string>();
            // ---- 🆕 激励（`Stimulation`）：**被战术选中时、结算前**触发自己那条正文 ----
            // 规则书 `:212`「被战术选中时、**结算前**：触发能力」。
            // ⚠️ 卡面**没写「友方」**（和突触 `:217` 的「被**友方**战术选中时」正好不同）
            //    ⇒ 谁的战术选中它都触发，我们照字面来。
            // ⚠️ 位置：**结算之前**（规则书写死了「结算前」）—— 和突触正好一前一后。
            if (chosen != null && chosen.Has(KeywordTable.Stimulation))
            {
                var st = chosen;
                int sp, sslot;
                if (FindSlot(ctx, st, out sp, out sslot))
                    FireTriggerAt(ctx, st, KeywordTable.Stimulation, sp, sslot);
            }

            ResolveOps(ctx, p, null, ops, chosen, out unresolved, sourceCard: card);

            // ---- 🆕 突触（`Synapse`）：**被友方战术选中时，对相邻部队/单位重复效果** ----
            // 规则书 `:217`「被友方战术选中时：对**相邻**部队/单位重复效果（**依战术而定**）」。
            //
            // **原版出处**（反编译 `decomp_out/CardScript__TargetedSpellPlayed.c:55-75`）：
            //   ① 判据是「**施放者与目标同一方**」（`param_2.sideFlag == param_3.sideFlag`）
            //      ⇒ 只有**友方**战术选中自己人才触发；
            //   ② 目标带 `synapse`（trait `0x47e`）时，取 `BattleManager.GetAdjacentUnits(目标)`
            //      —— 就是我们 `BoardSpec.AdjacentSlots` 那一份（A1 抽出来的唯一判据）；
            //   ③ 对每个相邻单位**再过一次战术自己的 `TargetCriteria`**
            //      （`criteria.Matches(caster, tuple)`）⇒ 我们复用 `IsLegalPick`（同一份判据）；
            //   ④ 然后**把这张战术的效果再跑一遍**（`OnCardPlayedWithTargetJustThisCardPlayed`），
            //      挨个以那个邻居为目标；
            //   ⑤ 最后 `BroadcastUnitSynapse` ⇒ 我们发 `When … triggers Synapse`。
            // ⚠️ 位置：**排在效果之后、进弃牌堆之前** —— 照原版同一个函数里的先后
            //    （`GoToCemetery(self)` 在突触那一段**后面**）。
            if (chosen != null && targetSlot >= 0 && side != "enemy"
                && chosen.Has(KeywordTable.Synapse) && !ctx.SynapseBusy)
            {
                RepeatTacticOnAdjacent(ctx, p, card, ops, chosen);
            }

            // 进弃牌堆（原版战术卡结算完就进弃牌堆）
            ps.Discard.Add(card);
            if (unresolved.Count > 0)
                ctx.Log($"⚠️ 「{card.Name}」有 {unresolved.Count} 条效果本版没结算："
                      + string.Join("、", unresolved));

            // ---- 🆕 巧技（`Artifice`）：**每次你打出战术时**触发（规则书 `:168`）----
            // 「每次打出战术时触发额外效果」。收听者是**你那一排所有带 `Artifice` 的单位**
            // （所以用 `FireTriggerOnSide`，和 `Cruelty` 同一支）。
            //
            // ⚠️ 位置**排在效果结算与弃牌之后**：那张战术已经彻底打完了，巧技是它的**额外**。
            //    （原版反编译里看不到先后 —— `CardScript.TargetedSpellPlayed` 里
            //     `BroadcastTacticPlayed` 在效果**之前**、`BroadcastUnitSynapse` 在之后，
            //     两件事，我们没有能分辨巧技该挂哪一处依据 ⇒ **这是我们的选择**，如实标着。）
            // ⚠️ 防御卡走的是同一条路（规则书 `:105`：防御卡属于战术大类），所以也会触发巧技。
            FireTriggerOnSide(ctx, p, KeywordTable.Artifice);
            return RuleCodes.OK;
        }

        /// <summary>
        /// **突触（`Synapse`）的重复**：把刚打出的这张战术**在相邻单位身上各再跑一遍**。
        ///
        /// 语义五条都照原版反编译（`CardScript__TargetedSpellPlayed.c:55-75`），逐条写在
        /// `PlayTactic` 的调用点旁边。这里只管**怎么重复**：
        ///   · 锚点 = **被这张战术选中的那个单位**（`chosen`）；
        ///   · 候选 = 它的**左右紧邻格**（`BoardSpec.AdjacentSlots` —— A1 抽出来的唯一判据）；
        ///   · 每个候选**再过一遍这张战术的目标规格**（`IsLegalPick`，和「高亮能不能点」同一份判据）；
        ///   · 命中者以**它自己**为 `chosen` 再跑一遍这张卡的 ops。
        ///
        /// ⚠️ **递归保护**：`ctx.SynapseBusy` —— 邻居自己也可能带 `synapse`，
        ///    没有它就会「A 触发 B、B 又触发 A」转不停。用完立刻复位。
        /// ⚠️ **不去重**：同一个邻居只会在候选里出现一次（左右两格互不相同）。
        /// </summary>
        static void RepeatTacticOnAdjacent(BattleContext ctx, int p, CardDef card,
                                           List<EffectOp> ops, UnitState anchor)
        {
            int ap = -1, aslot = -1;
            if (!FindSlot(ctx, anchor, out ap, out aslot)) return;      // 不在场上 ⇒ 无从相邻
            var spec = EffectText.PickTarget(ops);
            if (spec == null) return;                                   // 这张战术没有「选的谁」⇒ 不重复

            var slots = new List<int>();
            BoardSpec.AdjacentSlots(aslot, slots);

            ctx.Log($"突触（Synapse）：{anchor.Name} 被友方战术选中 —— 对相邻单位重复效果");
            ctx.SynapseBusy = true;
            try
            {
                foreach (int s in slots)
                {
                    var nb = ctx.Players[ap].Board[s];
                    if (nb == null || !nb.IsAlive) continue;
                    if (!IsLegalPick(ctx, p, spec, nb)) continue;        // 过不了同一套筛选 ⇒ 跳过
                    var un2 = new List<string>();
                    ResolveOps(ctx, p, null, ops, nb, out un2, sourceCard: card);
                    ctx.Log($"（突触：效果也在相邻的 {nb.Name} 上跑了一遍）");
                }
            }
            finally { ctx.SynapseBusy = false; }

            // `When this unit triggers Synapse, …` / `When a friendly unit triggers Synapse, …`
            // （`Broodlord` / `Zoanthrope`）—— 触发者是**被选中的那个带突触的单位**。
            // ⚠️ 走 `BroadcastKeywordEvent`：突触**没有卡面正文**，`FireTriggerAt` 在「没写效果」
            //    时会提前返回、连广播都不发（和 `swarm` 同一个理由）。
            BroadcastKeywordEvent(ctx, WhenEventKind.Triggers(KeywordTable.Synapse), anchor);
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

        /// <summary>
        /// **付费修饰型激活** —— `6 [Energy]: Extend effect until your next turn`（`Miraculous Feat`）·
        /// `8 [Energy]: Give it permanently`（`Daemonbreaker`）。（2026-09-13 A4 批 1）
        ///
        /// 语义出处：`rule_core.gd:1588 _energy_act_prep` —— 两栏 `undo` / `replay`：
        ///   · `extend`    ：撤销基础效果 → 把 `this turn` 换成 `until your next turn` 重结算
        ///   · `permanent` ：撤销基础效果 → **去掉时长**重结算（永久版）
        /// 付费是**可选**的：不付就保持基础效果（`:1799`「false → 放弃（**基础已结算**）」）——
        /// 那一步不在这里判，`ResolveOneCore` 的付费门槛已经在**本 op 之前**拦下了。
        ///
        /// ⚠️ **`undo` 的范围是「本卡这次施加的限时增益」**，按 `TempBuff.SourceCard`（真卡名）匹配 ——
        ///    不是 `Src`（战术卡那条路上恒为「战术卡」），那会把别的战术卡一起撤掉。
        /// ⚠️ 重结算的是 `op.BaseOps`（`Parse` 回填的**本卡在它之前**那批 op）的**副本**，
        ///    改完时长再走一遍 `ResolveOps`；**副本要去掉付费**（不该再扣一次钱）。
        /// ⚠️ 没有基础效果时**如实报**，别当成功。
        /// </summary>
        static bool DoPaidMod(BattleContext ctx, int owner, string by, EffectOp op,
                              UnitState chosen, List<string> unresolved)
        {
            string kind = string.IsNullOrEmpty(op.Payload) ? "extend" : op.Payload;
            string cardName = ctx.PlayingCard != null ? ctx.PlayingCard.Name : by;

            // ① 撤销本卡这次施加的限时增益（两边棋盘都要扫 —— 增益可能加在对方单位上）
            int undone = 0;
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u != null) undone += u.RemoveBuffsFromCard(cardName);
                }

            // ② 拿基础那批 op 的副本，改时长之后重结算
            var baseOps = op.BaseOps;
            if (baseOps == null || baseOps.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」是付费修饰型激活，但**本卡前面没有效果可改** ——"
                      + $"只撤销了 {undone} 条限时增益");
                unresolved.Add(op.Source + "（修饰型激活：本卡前面没有基础效果）");
                return false;
            }

            var replay = new List<EffectOp>(baseOps.Count);
            foreach (var b in baseOps)
            {
                var c = b.Clone();
                if (kind == "permanent") c.Duration = "";              // 永久：去掉所有时长
                else if (c.Duration == "turn") c.Duration = "nextturn"; // 延长：本回合 → 到你下个回合
                c.Cost = 0;                                             // 副本不该再扣一次钱
                c.CostKind = null;
                replay.Add(c);
            }
            ctx.Log($"{by}：「{op.Source}」{(kind == "permanent" ? "改为永久" : "延长到你下个回合")}"
                  + $" —— 撤销 {undone} 条限时增益，按新时长重结算 {replay.Count} 条");
            ResolveOps(ctx, owner, ctx.ActingUnit, replay, by, chosen);
            return true;
        }

        /// <summary>目标规格里 `PickMost` 那个属性叫什么 → 单位身上的取值。
        /// ⚠️ `attack` = **近战攻击力**（理由写在 `ResolveTargets` 里 `PickMost` 那段）。</summary>
        static int StatOf(UnitState u, string stat)
        {
            switch ((stat ?? "").Trim())
            {
                case "attack": case "melee attack": return u.Attack;
                case "ranged attack": return u.RangedAttack;
                case "health": return u.Health;
                default: return 0;
            }
        }

        /// <summary>
        /// `forceattack` —— **强制某个单位真的去打一下**（2026-09-13 A4 批 2）。
        ///
        /// 四张卡走这一条：`Murderous Desires`（`Make a damaged friendly unit attack by itself`）·
        /// `Peerless Bladesmen`（`Target friendly unit attacks the enemy with highest attack`）·
        /// `Let Loose` 的尾句 · `Damaged Hexmark` 的 `Artifice` 正文（裸 `Attacks a random enemy …`）。
        ///
        /// 🔴 **本函数最重要的一条：攻击一律走 `RuleCore.DeclareAttack`** ——
        ///    疲劳（攻击配额）/ 隐身与伪装现身 / 先锋与飞行的目标限制 / **反击伤害** / 事件广播
        ///    全都由**那一条路径**统一处理。**绝不在效果层另写一遍攻击**
        ///    （那正是本工程反复强调的「两处写同一条规则 = 迟早不一致」）。
        ///
        /// 攻击类型：**取近战 / 远程里高的那个**（原版 `ChooseAttackTypeAutomatically` 的规则），
        /// 打不动再试另一种 —— 见下面那一大段 ⚠️（**这是我们挑的**，原版强制攻击走不走自动选型查不到）。
        /// </summary>
        static bool DoForceAttack(BattleContext ctx, int owner, string by, EffectOp op,
                                  UnitState chosen, List<string> unresolved)
        {
            // ---- ① 谁去打 ----
            // 卡面点名了攻击者（`Make …` / `Target …`）就按目标规格收：`Count == 0` = **全部各打一次**、
            // `Count == 1` = 玩家点的那一个。卡面**没写**（裸 `Attacks a random enemy …`）⇒ **本卡自己**
            // （`ctx.ActingUnit` 由 `ResolveOne` 在结算每条效果前设好）。
            var attackers = new List<UnitState>();
            if (op.Target == null)
            {
                var self = ctx.ActingUnit;
                if (self != null && self.IsAlive) attackers.Add(self);
            }
            else
            {
                attackers.AddRange(ResolveTargets(ctx, owner, op.Target, null, chosen));
                attackers.RemoveAll(u => u == null || !u.IsAlive);
            }
            if (attackers.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」一个能去攻击的单位都没有，空过");
                return true;
            }

            int done = 0;
            foreach (var atk in attackers)
            {
                if (!atk.IsAlive) continue;                 // 前面那个打反击死了就不再打
                // ⚠️ `FindSlot` 的两个 out 是 `(player, slot)` —— **别传反**（2026-09-13 A4 批 2 撞到过：
                //    传反之后攻击者恒是**自己 0 号格**那个单位，症状是「第二个单位说它本回合已行动」）。
                int atkOwner, atkSlot;
                if (!FindSlot(ctx, atk, out atkOwner, out atkSlot)) continue;

                // ---- ② 打谁 ----
                var cand = DefenderPool(ctx, owner, op);
                if (cand.Count == 0)
                {
                    ctx.Log($"{by}：「{op.Source}」{atk.Name} 一个可打的敌人都没有，跳过");
                    continue;
                }

                // ---- ③ 打（走唯一那条攻击路径）----
                // 攻击类型：**取近战 / 远程里高的那个**，打不动再试另一种。
                // 出处：原版 `CardScript.ChooseAttackTypeAutomatically`（`decomp_out/CardScript__ChooseAttackTypeAutomatically.c:8-11`）
                //   写的是 `(近战 < 远程) + 1` —— 1=近战 / 2=远程（`AttackTypes.cs`），**就是挑高的**。
                // ⚠️ 但「强制攻击**是否**调用那个自动选型」**查不到**（`ResolveForceAttack` 的真逻辑在
                //    它的 `MoveNext` 里，那个方法**不在这次反编译导出中**，本地也重新生成不了）——
                //    ⇒ **这一条是我们照原版的自动选型规则挑的，别当成「原版强制攻击就是这样」**。
                bool preferRanged = RuleCore.FieldAttack(ctx, owner, atk, true)
                                  > RuleCore.FieldAttack(ctx, owner, atk, false);
                UnitState tgt;
                int code = TryAttackOnce(ctx, owner, atkSlot, cand, preferRanged, out tgt);
                if (code != RuleCodes.OK) code = TryAttackOnce(ctx, owner, atkSlot, cand, !preferRanged, out tgt);
                if (code != RuleCodes.OK)
                {
                    ctx.Log($"{by}：「{op.Source}」{atk.Name} 打不了 —— {RuleCodes.Describe(code)}");
                    unresolved.Add($"{op.Source}（{atk.Name} 强制攻击失败：{RuleCodes.Describe(code)}）");
                    continue;
                }
                ctx.Log($"{by}：「{op.Source}」{atk.Name} 被强制攻击 {tgt.Name}");
                done++;
            }
            return done > 0;
        }

        /// <summary>
        /// `forceattack` 的**被打候选**。
        /// 卡面写了（`Target2`）就按它收 —— `PickMost`（挑攻击力最高 / 生命最低）与
        /// `Random` 都已经在 `ResolveTargets` 里生效。
        /// **没写**（`by itself` / `attack by itself`）⇒ 全场敌方单位，由**攻击合法性**去筛
        /// （`TryAttackOnce` 会逐个试）。
        /// ⚠️ `by itself` 的挑选顺序 = **按槽号依次试第一个打得动的** —— 「不掷骰、定死的规则」，
        ///    和引擎里其它「取第一个」的规矩一致（同一局必须可复现）。
        /// </summary>
        static List<UnitState> DefenderPool(BattleContext ctx, int owner, EffectOp op)
        {
            var spec = op.Target2;
            if (spec == null)
                spec = new EffectTargetSpec
                {
                    Raw = "(未写目标：全场敌方单位，由攻击合法性筛)",
                    Side = "enemy", Kind = "any", Count = 0,
                };
            var list = ResolveTargets(ctx, owner, spec, null, null);
            list.RemoveAll(u => u == null || !u.IsAlive);
            return list;
        }

        /// <summary>在候选里挑一个**打得动**的，按 <paramref name="ranged"/> 打一次。
        /// `OK` = 真打出去了；否则返回**最后一次**的失败码。
        /// ⚠️ 反复试是安全的：`RuleCore.DeclareAttack` 把校验全做在改动**之前**（先 `IsValidTarget`
        ///    再消耗攻击），所以失败的那几次**没有副作用**。</summary>
        static int TryAttackOnce(BattleContext ctx, int owner, int atkSlot,
                                 List<UnitState> cand, bool ranged, out UnitState tgt)
        {
            tgt = null;
            int last = RuleCodes.ErrTarget;
            foreach (var d in cand)
            {
                if (d == null || !d.IsAlive) continue;
                int dp, dslot;
                if (!FindSlot(ctx, d, out dp, out dslot)) continue;
                int code = RuleCore.DeclareAttack(ctx, owner, atkSlot, dp, dslot, ranged);
                if (code == RuleCodes.OK) { tgt = d; return RuleCodes.OK; }
                last = code;
            }
            return last;
        }

        /// <summary>
        /// `Each of your units deals damage equal to its &lt;关键词&gt; to a random enemy`
        /// （`Sudden Assault`，SaimHann；2026-09-13 A4 批 1）。
        ///
        /// **逐个己方单位、各自结算**：伤害 = **它自己**的关键词值（`UnitState.KwValue`，
        /// 取值处和攻击时的星镖同源 —— `RuleCore.DeclareAttack` 里那个 `shuriken`）。
        /// ⚠️ **不是**「求和打一次」，也**不是**「用施放者的值」—— 卡面写的是 `its Shuriken`。
        /// ⚠️ 每个单位**各自掷一个随机敌人**（卡面 `a random enemy`，不是「都打同一个」）；
        ///    掷法走 `ctx.Rng`，同一局可复现。
        /// ⚠️ 关键词值 ≤ 0 的单位**跳过并如实计数**（它本来就没有星镖，不是失败）。
        /// </summary>
        static bool DoEachUnitDeal(BattleContext ctx, int owner, string by, EffectOp op)
        {
            string kw = string.IsNullOrEmpty(op.Payload) ? "shuriken" : op.Payload;
            if (op.Target == null)
            {
                ctx.Log($"{by}：「{op.Source}」没写打谁 ⇒ **这次空过**（不按整个目标池打）");
                return true;
            }

            // 候选池：把「挑几个」放开（`Count = 0` = 全部），逐个单位自己掷 ——
            // 直接用 `op.Target` 的话 `ResolveTargets` 会**先替我们掷好一个**，那就成了「都打同一个」。
            var all = new EffectTargetSpec
            {
                Raw = op.Target.Raw, Side = op.Target.Side, Kind = op.Target.Kind, Count = 0,
                DamagedOnly = op.Target.DamagedOnly, PrayedOnly = op.Target.PrayedOnly,
                SubtypeFilter = op.Target.SubtypeFilter, KeywordFilter = op.Target.KeywordFilter,
            };
            var pool = ResolveTargets(ctx, owner, all, null, null);
            pool.RemoveAll(u => u == null || !u.IsAlive);
            if (pool.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」敌方一个合法目标都没有，空过");
                return true;
            }

            var mine = ctx.Players[owner];
            int hitters = 0, dealt = 0, skipped = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = mine.Board[s];
                if (u == null || !u.IsAlive) continue;
                int n = u.KwValue(kw);
                if (n <= 0) { skipped++; continue; }      // 没有这个关键词 / 值是 0 ⇒ 它本来就不打
                pool.RemoveAll(t => t == null || !t.IsAlive);
                if (pool.Count == 0) break;               // 打空了就停（后面的单位没目标）
                var t = pool[ctx.Rng.Next(pool.Count)];
                dealt += Hurt(ctx, t, n, by);
                hitters++;
            }
            ctx.Log($"{by}：「{op.Source}」{hitters} 个单位各自按自己的 {kw} 值开火（合计 {dealt} 点）"
                  + (skipped > 0 ? $"，另有 {skipped} 个单位没有 {kw}、跳过" : ""));
            return true;
        }

        static bool DoHeal(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved)        {
            // `1-5` 区间：和 `Deal` 一样走 `ctx.Rng`（原版 `rule_core.gd:2677` 的 `randi_range`）
            int heal = op.AmountMax > op.Amount ? ctx.Rng.Next(op.Amount, op.AmountMax + 1) : op.Amount;

            if (op.Target == null)
            {
                // `Heal N` 没写目标 = **治己方督军**（原版 `rule_core.gd:2842`）
                var w = ctx.Players[owner].Warlord;
                if (w != null) w.Health = System.Math.Min(w.MaxHealth, w.Health + heal);
                ctx.Log($"{by}：「{op.Source}」治疗己方督军 {heal}（现 {w?.Health}）");
                return true;
            }
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen);
            foreach (var t in targets)
                if (t != null && t.IsAlive) t.Health = System.Math.Min(t.MaxHealth, t.Health + heal);
            ctx.Log($"{by}：「{op.Source}」治疗 {targets.Count} 个目标各 {heal} 点");
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

        /// <summary>
        /// `deploy` —— **免费把一个单位放进场上**（原版 `rule_core.gd:2970` 那一支）。
        ///
        /// 和 `create` 的关系：**同一个候选池算法**（`CreatePool`），区别只在「去哪儿」——
        /// `create` 进手牌/牌库，`deploy` **直接下场**。卡面的兵种/阵营/费用/关键词限制因此一模一样地筛。
        ///
        /// 三个来源（`op.DeployFrom`）：
        ///   · `pool`      —— 全卡池（`Deploy a Battle Sister` / `Deploy 8 random Ork Infantry`）
        ///   · `deck`      —— 自己牌库（`Deploy 4 random troops from your deck`），**部署后从牌库移走**
        ///   · `graveyard` —— 自己弃牌堆（`Deploy up to 5 friendly Infantry troops that died this game`），同样移走
        ///
        /// ⚠️ **弃牌堆那条是近似**：我们**没有「本局阵亡」的账**，只能拿 `Discard` 里 `type == "unit"`
        ///    的充数 —— 手牌超上限打进弃牌堆的单位卡也会被算进去。原版有完整的阵亡流水，我们没有。
        ///    如实写在这儿，别当它是精确的。
        /// ⚠️ **只部署单位卡**（`unitsOnly`）—— 部署一张战术卡是没有意义的事。
        /// </summary>
        /// <summary>
        /// `deploy` 的入口 —— 只在 `Each player deploys …` 时分岔，其余原样走 <see cref="DoDeployOnce"/>。
        ///
        /// `Each player deploys 3 troops from their deck`（`Birth of a Saga`，2026-09-13 A4 批 1）
        /// 要**双方各部署一次**（各从**自己的**牌库）。
        ///
        /// 🔴 **顺序是「先对手、后自己」—— 这一条是「我们挑的」，不是原版数据**
        ///    （那张卡在原版走的是另一条路，本地没有对应的方法体可读）。
        ///    挑它的理由：`LastTargets` 最后要停在**自己这边刚部署的那批**上 ——
        ///    下一句 `Your troops deployed this way gain Flank and Armour 3 this turn` 读的就是它
        ///    （`EffectTargetSpec.Deployed`），反过来会被对手那批覆盖 ⇒ 给自己人的增益全跑到对面去。
        /// </summary>
        static bool DoDeploy(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            if (!op.EachPlayer) return DoDeployOnce(ctx, owner, by, op, unresolved);
            bool foe = DoDeployOnce(ctx, 1 - owner, by, op, unresolved);
            bool me = DoDeployOnce(ctx, owner, by, op, unresolved);
            return me || foe;
        }

        static bool DoDeployOnce(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            string faction = (ps.Warlord != null && ps.Warlord.Card != null) ? ps.Warlord.Card.Faction : null;

            List<CardDef> source = null;
            string srcName = "卡池";
            if (op.DeployFrom == "deck") { source = ps.Deck; srcName = "牌库"; }
            else if (op.DeployFrom == "graveyard") { source = ps.Discard; srcName = "弃牌堆"; }

            var pool = CreatePool.Resolve(source != null ? (IReadOnlyList<CardDef>)source : ctx.CardPool,
                                          op.Payload, faction, unitsOnly: true,
                                          costMin: op.CostMin, costMax: op.CostMax);
            if (!pool.Ok)
            {
                ctx.Log($"{by}：「{op.Source}」部署不了 —— {pool.Why}（**这条没生效**）");
                unresolved.Add(op.Source + "（部署：" + pool.Why + "）");
                return false;
            }

            int n = op.Amount > 0 ? op.Amount : 1;
            var picked = PickN(ctx, pool.Cards, n, op.UpTo);

            int ok = 0;
            string names = "";
            // 「刚部署的这批」记进 `LastTargets` —— 后面那句 `and give **them** Vanguard` 指着它
            ctx.LastTargets.Clear();
            foreach (var card in picked)
            {
                int slot;
                if (!DeployFree(ctx, owner, card, out slot)) break;   // 满场 → 后面的也放不下，停
                if (source != null) source.Remove(card);              // 牌库/弃牌堆里那份要移走
                var deployed = ctx.Players[owner].Board[slot];
                if (deployed != null) ctx.LastTargets.Add(deployed);
                ok++;
                if (names.Length > 0) names += "、";
                names += card.Name + "→" + slot;
            }
            if (ctx.LastTargets.Count > 0) ctx.LastTarget = ctx.LastTargets[0];

            ctx.Log($"{by}：「{op.Source}」从{srcName}部署 {ok}/{picked.Count} 个"
                  + (names.Length > 0 ? $"（{names}）" : "")
                  + (pool.Detail != null ? $"（{pool.Detail}）" : ""));
            if (ok < picked.Count)
                unresolved.Add(op.Source + $"（只部署了 {ok}/{picked.Count} 个：场上没空格或池子不够）");
            return ok > 0;
        }

        /// <summary>
        /// 从候选池里**抽 N 张**（`create` 和 `deploy` 共用这一份 —— 两处各写一份迟早不一致）。
        ///
        /// 抽法**不重复**（同一张不会在一次效果里出现两遍），**抽空了从头再来** ——
        /// 附录 B 的「几种、各 N 张、上限多少」是同一副有限牌堆的循环用法
        /// （例：`3 个极限战士载具（18 种，各 3 张）`）。
        /// `upTo = true` 时**不循环**：有几张算几张。
        ///
        /// ⚠️ 一律走 `ctx.Rng`（种子化）—— 同一局必须永远可复现，不用 `UnityEngine.Random`。
        /// </summary>
        static List<CardDef> PickN(BattleContext ctx, List<CardDef> pool, int n, bool upTo)
        {
            var picked = new List<CardDef>(n);
            if (pool == null || pool.Count == 0 || n <= 0) return picked;

            var remaining = new List<CardDef>(pool);
            for (int i = 0; i < n; i++)
            {
                if (remaining.Count == 0)
                {
                    if (upTo) break;                       // 「至多 N」——不够就不再凑
                    remaining.AddRange(pool);
                }
                int k = ctx.Rng.Next(remaining.Count);
                picked.Add(remaining[k]);
                remaining.RemoveAt(k);
            }
            return picked;
        }

        /// <summary>
        /// `create` —— **造牌**：凭空把卡放进手牌 / 牌库顶（不是抽，也不是从墓地捞）。
        ///
        /// 语义出处：
        ///   · **规则书附录 B「生成/复制卡的阵营指南」**（`资料/规则书/…_中文翻译.md:254-287`）
        ///   · **规则书附录 C「骰子查找表」**（同文件 `:289-320`）—— 实体版的候选名单
        ///   · 候选池的算法在 <see cref="CreatePool"/>
        ///
        /// 三件事：
        ///   ① 算候选池（具名卡 / 按阵营+兵种筛 / 名单里挑）；
        ///   ② 用 `ctx.Rng` 从这里抽 N 张（**同一局必须可复现**，不用 `UnityEngine.Random`）；
        ///   ③ 放进目的地（自己手牌 / 对手手牌 / 自己牌库顶），手牌超上限按抽牌那条同样的规则丢。
        ///
        /// ⚠️ **选法**：附录 B 的「几种、各 N 张、上限多少」是一副**有限的牌堆** ——
        ///    所以按「不重复地抽，抽空了从头再来」办（同一张卡因此最多出现 floor(N/池大小)+1 次）。
        ///    这不是原版的实现（原版是远程数据驱动的生成器），是**照附录 B 的文字**做的近似，
        ///    如实写在这儿。
        /// ⚠️ **池子空 / 认不出** 一律如实报，**不换成别的卡**（红线：不许静默失败）。
        /// </summary>
        static bool DoCreate(BattleContext ctx, int owner, string by, EffectOp op,
                             List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            ctx.LastCreated.Clear();       // 「上一批造出来的」窗口：每张卡各开各的

            // 施放者阵营 = 自己督军那张卡的 `faction`。卡面**没写**阵营时用它
            // （规则书附录 B：每个生成器生成**自己阵营**的卡）。
            string faction = null;
            if (ps.Warlord != null && ps.Warlord.Card != null) faction = ps.Warlord.Card.Faction;

            var pool = CreatePool.Resolve(ctx.CardPool, op.Payload, faction);

            // `Create a copy of **it**` —— 指代上一条效果的目标那张卡（原版 `it_target`）。
            // 目标可能是场上的单位，它的 `Card` 就是要复制的那张。
            if (pool.CopyOfPrev)
            {
                // 「复制**哪张**」有两个来源，**先看选牌那个**：
                //   · `Choose a troop from your deck. Draw it and create a copy of it in your hand`
                //     —— `it` = **刚选中的那张**。它在手牌/牌库里，**不在场上**，
                //        而 `LastTarget` 是 `UnitState`，够不着它（2026-09-13 加选牌 handler 时接通）
                //   · `Deal 3 damage to an enemy. Create a copy of it in your hand`
                //     —— `it` = **上一条效果打中的那个场上单位**（原有路径，不动）
                // ⚠️ 选牌那个**按一张卡一个窗口清空**（`ResolveOps` 入口），所以不会有陈旧值。
                var t = ctx.LastTarget;
                if (ctx.LastChosenCard != null)
                {
                    pool.Cards.Add(ctx.LastChosenCard);
                    pool.Detail = "复制刚选中的「" + ctx.LastChosenCard.Name + "」";
                }
                else if (t == null || t.Card == null)
                {
                    ctx.Log($"{by}：「{op.Source}」要复制上一条效果的目标，但前面没有目标 —— **这条没生效**");
                    unresolved.Add(op.Source + "（`it` 没有可复制的目标）");
                    return false;
                }
                else
                {
                    pool.Cards.Add(t.Card);
                    pool.Detail = "复制上一条效果的目标「" + t.Card.Name + "」";
                }
            }
            else if (!pool.Ok)
            {
                ctx.Log($"{by}：「{op.Source}」造不出牌 —— {pool.Why}（**这条没生效**）");
                unresolved.Add(op.Source + "（造牌：" + pool.Why + "）");
                return false;
            }

            int n = op.Amount > 0 ? op.Amount : 1;
            var picked = PickN(ctx, pool.Cards, n, false);

            string names = "";
            foreach (var c in picked)
            {
                if (names.Length > 0) names += "、";
                names += c.Name;
            }

            switch (op.Dest)
            {
                case "decktop":
                    // 牌库**顶** = 列表末尾（`Draw` 从末尾 pop，和 `rule_core` 的 `pop_back` 一致）
                    ctx.Players[owner].Deck.AddRange(picked);
                    ctx.Log($"{by}：「{op.Source}」造了 {n} 张放到自己牌库顶：{names}"
                          + (pool.Detail != null ? $"（{pool.Detail}）" : ""));
                    return true;

                case "hand":
                case "enemyhand":
                {
                    int who = op.Dest == "hand" ? owner : 1 - owner;
                    ctx.Players[who].Hand.AddRange(picked);
                    ctx.LastCreated.AddRange(picked);      // `They cost 1 less` 指着它们
                    EnforceHandLimit(ctx, who);

                    // 🆕 `When you create a Secret, …` / `When you create a Sabotage, …`
                    // （2026-09-13 第三十四轮）。**放在 `hand` / `enemyhand` 这一支里** ——
                    //   隐秘与破坏按定义就是**手上**的牌，`decktop` 那一支放的是别的东西。
                    // ⚠️ 事件归属传的是 **`owner`（造牌的施放者）**，不是 `who`（牌落到谁手里）：
                    //   卡面写 `When **you** create a Sabotage`，而破坏是造到**敌方手上**的，
                    //   拿收牌方当归属会让**对方**的监听器响，而且不报错。
                    // ⚠️ 判「是不是隐秘 / 破坏」用 <see cref="CreatePool.MatchesKind"/> ——
                    //   那是全仓**唯一**的「这张卡算不算某一类」判据（`KindWords` 里有
                    //   `{"secret","subtype","Secret"}` / `{"sabotage","subtype","Sabotage"}`），
                    //   不另写一份名字比对。
                    foreach (var c in picked)
                    {
                        if (CreatePool.MatchesKind(c, "secret"))
                            BroadcastWhen(ctx, WhenEventKind.CreatesSecret, owner, c, null);
                        else if (CreatePool.MatchesKind(c, "sabotage"))
                            BroadcastWhen(ctx, WhenEventKind.CreatesSabotage, owner, c, null);
                    }

                    ctx.Log($"{by}：「{op.Source}」造了 {n} 张给 {ctx.Players[who].Name}：{names}"
                          + (pool.Detail != null ? $"（{pool.Detail}）" : ""));
                    return true;
                }
            }

            ctx.Log($"{by}：「{op.Source}」的目的地「{op.Dest}」本版不认识 —— **这条没生效**");
            unresolved.Add(op.Source + "（造牌目的地 " + op.Dest + " 不认识）");
            return false;
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
                if (t != null && t.IsAlive && !t.IsStunned)
                {
                    t.IsStunned = true;
                    // 🆕 `When an enemy receives a Stun, …`（2026-09-13 第三十四轮）。
                    // ⚠️ `!t.IsStunned` 那道守卫是**行为保持**的 —— 原来重复眩晕也只是把 `true`
                    //    再赋一次（没副作用），但**广播不能重复**：卡面写的是「**收到**一次眩晕」。
                    BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, t);
                }
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

        // ==================================================================
        //  选牌 `Choose a <筛选> [from/in <来源>] [and <动词>]`
        //  权威源 `rule_core.gd:1157 _resolve_choose` + `:1041 _chosen_apply`
        // ==================================================================

        /// <summary>
        /// **选牌**：从某个候选域里挑一张卡，然后对它做一件事。
        ///
        /// 选法是原版既定的**单机口径**（`rule_core.gd:1159` 函数头：
        /// 「从句提取 → 候选域 → 3 候选 → **单机自动选 1**（battle.gd 弹窗后接）」）：
        /// 规则书 :233 说「随机选出 3 张候选、玩家选 1 张」，单机没有弹窗，
        /// 所以直接**从全集里等概率取一张** —— 这与「洗牌后取前三的第一张」**分布相同**，
        /// 但不是同一个随机数消耗序列，所以别拿它和 rule_core 逐帧对随机。
        /// 表现层的选牌 UI 是**另一件**（`资料/阵营推进_清单与交接.md` §四点名了），不阻塞这里。
        ///
        /// 🔴 **最要紧的一步是写引用位**（<see cref="BattleContext.LastCreated"/> /
        /// <see cref="BattleContext.LastChosenCard"/>）：这一族的动作**常常写在下一句**
        /// （`Choose a troop from your deck.` + `Draw it and create a copy of it in your hand`），
        /// 而后一句本来就已经能解析、能结算 —— 只要它引用得到选中的那张卡。
        /// </summary>
        static bool DoChooseCard(BattleContext ctx, int owner, string by, EffectOp op,
                                 UnitState chosen, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            var foe = ctx.Players[1 - owner];

            // ---- ① 候选域（`rule_core.gd:991 _choose_candidates` 的五个来源）----
            List<CardDef> source = null;          // null = 全卡池（`pool`）
            string srcName;
            switch (op.ChooseSrc)
            {
                case "deck":      source = ps.Deck;  srcName = "自己牌库";   break;
                case "hand":      source = ps.Hand;  srcName = "自己手牌";   break;
                case "enemyhand": source = foe.Hand; srcName = "对手手牌";   break;
                case "dead":
                    source = DeadCandidates(ctx, owner, op.ChooseDeadScope);
                    srcName = op.ChooseDeadScope == "since_last_turn"
                            ? "自你上个回合起阵亡的部队" : "本局阵亡的部队";
                    break;
                default:          srcName = "全卡池"; break;
            }

            IReadOnlyList<CardDef> candsIn = source != null ? (IReadOnlyList<CardDef>)source : ctx.CardPool;
            var cands = CreatePool.FilterChoose(candsIn, ctx.CardPool, op.ChooseWhat,
                                                out string detail, out string why);
            if (why != null)
            {
                // 「候选是空的」在这一族里是**正常结局**（规则书 :233 允许空候选），
                // 但**卡面写了要做的事没做**，所以照样如实报出来 —— 不许静默空过。
                ctx.Log($"{by}：「{op.Source}」在{srcName}里没得选 —— {why}（**这条没生效**）");
                unresolved.Add(op.Source + "（选牌：" + why + "）");
                return false;
            }

            var pick = cands[ctx.Rng.Next(cands.Count)];

            // ---- ② 引用位：原版 `:1151-1152` **两个都写** ----
            // `LastCreated` 接通已有的 `(指代上一张)`（`Lower its cost by N` / `It costs N less`）；
            // `LastChosenCard` 给「复制选中那张」用（那类指代对象在**手牌/牌库里**，
            // 而 `LastTarget` 是 `UnitState`，够不着）。
            ctx.LastCreated.Clear();
            ctx.LastCreated.Add(pick);
            ctx.LastChosenCard = pick;

            // ---- ②b 规则书英文版 `:477`：`choose` 的**没被选中的候选要归还，并把牌库洗掉** ----
            // 原文：「Whenever a card uses the word "choose", randomly select **3** cards from the set
            // of possibilities and the player chooses which one to keep/draw/resolve.
            // **Return unchosen cards drawn from your deck and shuffle.**」
            // 单机自动选 1 时，「从全集等概率取 1」与「洗牌取前 3 再选 1」**分布相同**，
            // 但「牌库被洗过」是**可观察结果**，不能省掉。
            // ⚠️ 只在**来源是牌库**时洗 —— 那半句的限定词正是 `drawn **from your deck**`。
            if (op.ChooseSrc == "deck") Shuffle(ps.Deck, ctx.Rng);

            string act = op.ChooseAct ?? "";
            string what = string.IsNullOrEmpty(op.ChooseWhat) ? "任意卡" : op.ChooseWhat;

            switch (act)
            {
                // 动作在**下一句**里（`Choose a troop from your deck. Draw it and …`）——
                // 那一段自己就能解析，本 op 的活到此为止。
                case "":
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」"
                          + $"（{what}；{detail}）—— 后续句接着结算");
                    return true;

                case "hand":
                case "draw":
                {
                    RemoveFromSource(ctx, owner, op.ChooseSrc, pick);
                    ps.Hand.Add(pick);
                    EnforceHandLimit(ctx, owner);
                    // `draw` 要记进「本次结算抽到的牌」—— `For each troop drawn …` 数这个
                    // （和 `DoDraw` / `DoDrawType` 同一条路）
                    if (act == "draw") ctx.DrawnThisResolve.Add(pick);
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」"
                          + $"（{what}）→ {(act == "draw" ? "抽上手" : "放入手牌")}");
                    return true;
                }

                case "deploy":
                {
                    RemoveFromSource(ctx, owner, op.ChooseSrc, pick);
                    int slot;
                    if (!DeployFree(ctx, owner, pick, out slot))
                    {
                        ctx.Log($"{by}：「{op.Source}」选了「{pick.Name}」但场上没空格 —— **这条没生效**");
                        unresolved.Add(op.Source + "（选牌部署：场上没空格）");
                        return false;
                    }
                    // 「刚部署的那个」= 后续 `and give **it** …` 的指代对象
                    var dep = ctx.Players[owner].Board[slot];
                    if (dep != null)
                    {
                        ctx.LastTargets.Clear();
                        ctx.LastTargets.Add(dep);
                        ctx.LastTarget = dep;
                    }
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」（{what}）→ 部署到槽 {slot}");
                    return true;
                }

                case "decktop":
                {
                    RemoveFromSource(ctx, owner, op.ChooseSrc, pick);
                    ps.Deck.Add(pick);          // 牌库**顶** = 列表末尾（`Draw` 从末尾 pop）
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」（{what}）→ 放到自己牌库顶");
                    return true;
                }

                case "todeck":
                case "return":
                {
                    // `return` 的来源是**手牌**（`Choose a card in your hand and return it to your deck`），
                    // 但 `ChooseSrc` 已经把它记成 `hand` 了，所以取走这一步两者同路。
                    RemoveFromSource(ctx, owner, op.ChooseSrc, pick);
                    ps.Deck.Add(pick);
                    Shuffle(ps.Deck, ctx.Rng);   // B11「洗回牌库」—— 规则书那族都要求洗
                    ctx.Log($"{by}：「{op.Source}」把「{pick.Name}」洗回自己牌库（{what}）");
                    return true;
                }

                case "shuffle":
                {
                    // `Choose a card in the enemy hand and shuffle it into their deck`
                    // —— 洗进的是**对手的**牌库（`their`）。
                    for (int i = foe.Hand.Count - 1; i >= 0; i--)
                        if (ReferenceEquals(foe.Hand[i], pick)) foe.Hand.RemoveAt(i);
                    foe.Deck.Add(pick);
                    Shuffle(foe.Deck, ctx.Rng);
                    ctx.Log($"{by}：「{op.Source}」把对手手里的「{pick.Name}」洗回对手牌库（{what}）");
                    return true;
                }

                case "enemyhand":
                {
                    RemoveFromSource(ctx, owner, op.ChooseSrc, pick);
                    foe.Hand.Add(pick);
                    EnforceHandLimit(ctx, 1 - owner);
                    ctx.Log($"{by}：「{op.Source}」选了「{pick.Name}」（{what}）→ 放进**对手**手牌");
                    return true;
                }

                case "copies":
                {
                    int n = op.ChooseCopies > 0 ? op.ChooseCopies : 2;
                    for (int i = 0; i < n; i++) ps.Hand.Add(pick);
                    EnforceHandLimit(ctx, owner);
                    ctx.Log($"{by}：「{op.Source}」选了「{pick.Name}」（{what}）→ 造 {n} 张复制进手牌");
                    return true;
                }
            }

            // 认不出的动作**绝不默认成 `to_hand`** —— 那是静默的错误语义（原版 `:1193` 就默认了）
            ctx.Log($"{by}：「{op.Source}」的动作「{act}」本版不认识 —— **这条没生效**");
            unresolved.Add(op.Source + "（选牌动作 " + act + " 没实现）");
            return false;
        }

        /// <summary>
        /// `Choose a friendly troop that died **this game / this battle / since your last turn**`
        /// 的候选 —— 出处 <see cref="BattleContext.DeadUnits"/>。
        ///
        /// `this battle` 与 `this game` 是**同一个意思**（`rule_core.gd:1199-1204` 合并成 `all`）。
        /// `since your last turn` 的窗口 = `DeathTurn >= 本方最近一次回合开始时的全局回合号`，
        /// **自己回合里死的和对手回合里死的都算**，上一轮之前的不算。
        /// </summary>
        static List<CardDef> DeadCandidates(BattleContext ctx, int owner, string scope)
        {
            var outp = new List<CardDef>();
            int mark = ctx.Players[owner].LastTurnStartMark;
            foreach (var d in ctx.DeadUnits)
            {
                if (d.Owner != owner || d.Card == null) continue;
                if (scope == "since_last_turn" && d.DeathTurn < mark) continue;
                bool dup = false;
                foreach (var c in outp) if (ReferenceEquals(c, d.Card)) { dup = true; break; }
                if (!dup) outp.Add(d.Card);
            }
            return outp;
        }

        /// <summary>
        /// 从**来源**里把选中的那张取走（部署/回手/洗回牌库时都要）。
        ///
        /// `pool` 是**凭空生成**，没有「取走」这一步 —— 所以它什么都不做。
        /// 墓地那条走 <see cref="BattleContext.TakeFromGraveyard"/>（`Discard` 与 `DeadUnits`
        /// **必须一起**移除，那条规则只写在那一个地方）。
        /// </summary>
        static void RemoveFromSource(BattleContext ctx, int owner, string srcKind, CardDef card)
        {
            if (card == null) return;
            var ps = ctx.Players[owner];
            switch (srcKind)
            {
                case "deck":      RemoveRef(ps.Deck, card); break;
                case "hand":      RemoveRef(ps.Hand, card); break;
                case "enemyhand": RemoveRef(ctx.Players[1 - owner].Hand, card); break;
                case "dead":      ctx.TakeFromGraveyard(owner, card); break;
            }
        }

        /// <summary>按**引用**摘掉一张（不是按名字 —— 手牌里两张同名卡是两张牌）。</summary>
        static void RemoveRef(List<CardDef> list, CardDef card)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (ReferenceEquals(list[i], card)) { list.RemoveAt(i); return; }
        }

        /// <summary>按**引用**找在不在（同 <see cref="RemoveRef"/>：名字相同是两张牌）。</summary>
        static bool ContainsRef(List<CardDef> list, CardDef card)
        {
            foreach (var c in list) if (ReferenceEquals(c, card)) return true;
            return false;
        }

        /// <summary>
        /// `Draw it` / `Draw them` —— 把**前面那条效果（通常是选牌）指到的那张卡**抽上手。
        ///
        /// 指代对象是 `ctx.LastChosenCard`（选牌 handler 写的），退而求其次看 `ctx.LastCreated`。
        /// ⚠️ 这个动词是**修一个静默错解析**才有的：以前 `Draw it` 掉进 `drawtype`，
        ///    被当成「抽一种叫 `it` 的兵种」，翻遍牌库找不到、只写一行日志。
        ///    详见 `EffectText.ReDrawRef` 的注释。
        /// </summary>
        static bool DoDrawRef(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            var refs = new List<CardDef>();
            if (ctx.LastChosenCard != null) refs.Add(ctx.LastChosenCard);
            else refs.AddRange(ctx.LastCreated);

            if (refs.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要抽「它」，但前面没有指代对象（既没选牌、也没造牌）"
                      + " —— **这条没生效**");
                unresolved.Add(op.Source + "（`it` 没有可抽的对象）");
                return false;
            }

            var got = new List<CardDef>();
            foreach (var c in refs)
            {
                if (ContainsRef(ps.Hand, c)) continue;   // 已经在手上，别再来一张
                RemoveRef(ps.Deck, c);                   // 从牌库取走（不在牌库里就什么都不做）
                ps.Hand.Add(c);
                ctx.DrawnThisResolve.Add(c);             // 和 `DoDraw` 同一条路：`for each … drawn` 数它
                got.Add(c);
            }
            EnforceHandLimit(ctx, owner);

            // 「刚抽到的这批」= 后续 `lower its cost by N` / `create a copy of it` 的指代对象
            ctx.LastCreated.Clear();
            foreach (var c in refs) ctx.LastCreated.Add(c);

            if (got.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要抽「它」，但那张已经在手上了 —— 没动作");
                return false;
            }
            ctx.Log($"{by}：「{op.Source}」把「{Names(got)}」抽上手");
            return true;
        }

        /// <summary>
        /// `Return X to your hand / to the top of their deck / to your deck` —— 把**场上**的卡
        /// 挪回手牌或牌库。
        ///
        /// **语义出处（权威）**：规则书**英文原版 `:455-457`**「Cards Sent into the Deck」——
        ///   &gt; Cards returned to the deck from the battlefield … should be **shuffled in
        ///   &gt; unless otherwise stated by a card effect.**
        /// ⇒ `to the top of their deck` 属「另有说明」→ **放牌库顶、不洗**；
        ///   `to your deck` 没写「顶」→ **洗入**。两条分支在下面分得很清楚。
        ///
        /// ⚠️ 回手/回牌库**不是阵亡**：卡进手牌或牌库，不是 `Discard`，
        ///    **也不进 `DeadUnits`**（复活的卡不该从弃牌堆里捞）。
        /// </summary>
        static bool DoReturn(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            // 目标可能有**两个**：`Return a friendly troop **and a random enemy troop** to the top of their deck`
            var phrases = new List<string>();
            foreach (string piece in (op.Payload ?? "").Split(new[] { " and " }, System.StringSplitOptions.None))
            {
                string p2 = piece.Trim();
                if (p2.Length > 0) phrases.Add(p2);
            }

            var moved = new List<CardDef>();
            int miss = 0;
            foreach (string phrase in phrases)
            {
                var spec = EffectText.ParseTarget(phrase);
                var targets = spec == null ? new List<UnitState>()
                                           : ResolveTargets(ctx, owner, spec, null, null);
                if (targets.Count == 0) { miss++; continue; }

                foreach (var u in targets)
                {
                    int p, slot;
                    if (!FindUnit(ctx, u, out p, out slot)) continue;
                    var ps2 = ctx.Players[p];
                    var card = u.Card;
                    ps2.Board[slot] = null;

                    switch (op.Dest)
                    {
                        case "hand":
                            ps2.Hand.Add(card);
                            EnforceHandLimit(ctx, p);
                            break;
                        case "decktop":
                            ps2.Deck.Add(card);      // 牌库**顶** = 列表末尾（`Draw` 从末尾 pop）；**不洗**
                            break;
                        default:                     // `deck` —— 规则书 :455「shuffled in」
                            ps2.Deck.Add(card);
                            Shuffle(ps2.Deck, ctx.Rng);
                            break;
                    }
                    ctx.Emit(new BattleEvent { Kind = EvtKind.Return, Player = p, Slot = slot, CardId = card.Name });
                    moved.Add(card);
                }
            }

            if (moved.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」没有可回手/回牌库的目标（{miss} 个目标短语都没匹配到）"
                      + " —— **这条没生效**");
                unresolved.Add(op.Source + "（return 没有目标）");
                return false;
            }

            // 引用位：后面那句 `It costs 4 less`（`Master of Manoeuvre`）指着**刚回手的那张**
            ctx.LastCreated.Clear();
            foreach (var c in moved) ctx.LastCreated.Add(c);

            string where = op.Dest == "hand" ? "手牌" : (op.Dest == "decktop" ? "牌库顶" : "牌库（已洗）");
            ctx.Log($"{by}：「{op.Source}」把「{Names(moved)}」放回{where}");
            return true;
        }

        /// <summary>卡名列表 → 可读字符串（日志用）。**复用上面那个已有的 `Names`**，
        /// 别在同一个类里写第二份 —— 两份迟早不一致。</summary>

        // ==================================================================
        //  常驻效果 / 手牌陷阱（回合起止触发）
        //  规则书英文版 `:39-41`「Persistent Effects」= 写「For the rest of this battle」的卡
        //  **被弃置后依然生效**，要单独放一摞备查 —— 卡本身就是效果来源。
        // ==================================================================

        /// <summary>
        /// **登记一条常驻效果**（`For the rest of this battle, at the start/end of your turn, …`）。
        /// 登记之后由 <see cref="ResolveAtTurn"/> 在每个自己回合的起/止消费。
        /// </summary>
        static bool DoPersist(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            if (op.AtTurnOps == null || op.AtTurnOps.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要登记常驻效果，但正文解析不出来 —— **这条没生效**");
                unresolved.Add(op.Source + "（常驻效果正文解析不出来）");
                return false;
            }
            ctx.PersistentEffects.Add(new PersistentEffect
            {
                Owner = owner,
                Source = ctx.PlayingCard,       // 规则书要求这类卡留档备查，来源要记下来
                Phase = op.AtTurnPhase,
                // `at the start|end of your turn` 走回合段；`deploy` 走**部署事件**
                // （原版 `OtherUnitSummoned=190`，见 `BattleContext.PersistentEffect.Trigger`）
                Trigger = op.AtTurnPhase == "deploy" ? "deploy" : "turn",
                Criteria = op.Filter,
                Ops = op.AtTurnOps,
                Body = op.Payload,
            });
            // 日志要打人话：`deploy` 不是回合的某一刻，别套「自己回合开始/结束」那两句
            if (op.AtTurnPhase == "deploy")
            {
                string what = op.Filter != null && !op.Filter.IsEmpty ? op.Filter.ToString() : "任意单位";
                ctx.Log($"{by}：「{op.Source}」登记了**常驻效果**（自己部署 {what} 时 → {op.Payload}）"
                      + " —— 这张卡之后被弃置也照样生效（规则书 :39-41）");
                return true;
            }
            string when = op.AtTurnPhase == "turn_start" ? "自己回合开始" : "自己回合结束";
            ctx.Log($"{by}：「{op.Source}」登记了**常驻效果**（{when} → {op.Payload}）"
                  + " —— 这张卡之后被弃置也照样生效（规则书 :39-41）");
            return true;
        }

        /// <summary>
        /// **持续改费**：`Sabotage cards in the enemy hand cost 1 more`（`Underground Network`）。
        ///
        /// 做一件事：往 `ctx.CostMods` 挂一条**带筛选条件**的永久修正。
        /// 由 <see cref="RuleCore.CostOf"/> **现算** —— 和原版一样**不改卡上的费用字段**
        /// （原版：`CardEffect.costChange` + `buffType=changeCost`，算费用时查 `EntityScript.CurrentCost`）。
        ///
        /// ⚠️ **`HandOf` 必须显式设**：这条的正主是**对手手里**的破坏卡，不是自己的人。
        ///    原版对应的字段是 `HandEffect.playersAffected`（`PlayerHand__UpdateCardEffects.c:267` 判自己、
        ///    `:303` 判敌方）—— 极性写反，整整一档效果就没了，而且是**静默**的。
        /// </summary>
        static bool DoCostMore(BattleContext ctx, int owner, EffectOp op, List<string> unresolved)
        {
            if (op.Target == null || string.IsNullOrEmpty(op.Target.Kind))
            {
                ctx.Log($"「{op.Source}」要加价，但看不出加在哪类牌上 —— **这条没生效**");
                unresolved.Add(op.Source + "（持续改费说不出加给哪类牌）");
                return false;
            }
            int amount = op.Amount > 0 ? op.Amount : 1;
            ctx.CostMods.Add(new CostMod
            {
                Player = owner,
                Key = "*",                                      // 不限卡名，靠 Criteria 筛
                Delta = +amount,
                // `the enemy hand` → 对手手里那批；`your hand` → 自己手里那批
                HandOf = op.Target.Side == "enemy" ? 1 - owner : owner,
                Criteria = new CardCriteria { KindWord = op.Target.Kind, Keyword = op.Target.KeywordFilter },
                ExpireTurn = -1,                                // 本场战斗有效
            });
            ctx.Log($"{ctx.Players[owner].Name}：「{op.Source}」本场战斗内，"
                  + $"{ctx.Players[op.Target.Side == "enemy" ? 1 - owner : owner].Name} 手里的"
                  + $"{op.Target.Kind} 卡费用 +{amount}"
                  + $"（算费用时现查，不改卡面 —— 照原版 `costChange` 的做法）");
            return true;
        }

        /// <summary>
        /// **手牌陷阱**（`At the end of your turn, …`，`keywords` 带 `Sabotage`）**被打出时**——
        /// **不结算那个效果**，只把「打出去 = 丢弃它」这件事说清楚。
        ///
        /// 为什么不算报错：这类卡躺在手牌里**每回合都害持有者**，打出去（进弃牌堆）本身就是
        /// 有意义的动作 —— 也就是「解掉这个陷阱」。`rule_core.gd` 走的是同一条：
        /// 打出时那句 `At the end of your turn, …` 没有任何 handler 认领，卡照常进弃牌堆、陷阱随之消失。
        /// ⚠️ 但**必须说出来**，不能让玩家以为「打出去就触发了那个效果」。
        /// </summary>
        static bool DoAtTurn(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            string when = op.AtTurnPhase == "turn_start" ? "开始" : "结束";
            ctx.Log($"{by}：「{op.Source}」是一张**手牌陷阱** —— 打出去只是**丢弃它**"
                  + $"（进弃牌堆、不再害你）；那个效果只在**持有者回合{when}**触发，不在这里结算");
            return true;
        }

        /// <summary>
        /// 把一串**已经解析好的 op** 结算掉 —— **触发式效果**（`Rally:` / `Strike:` / `Slay:` /
        /// `Backlash:` / `Penitence:`）走这条（2026-09-13 第三十一轮）。
        ///
        /// ⚠️ 和 <see cref="ResolveEffect"/>（那个**封闭文法**）**并存**，由
        /// <see cref="RuleCore.FireTriggerAt"/> 二选一：卡面正文解析得出来就走这里，否则走老路。
        /// 为什么两条都要留着 —— 我们自己设计的那 26 张卡写的是 `Damage 2 EnemyUnit`
        /// （只有封闭文法认得），而**原版卡面**写的是 `Rally: Stun an enemy`（只有 `EffectText` 认得）。
        /// </summary>
        public static void ResolveOps(BattleContext ctx, int owner, UnitState source,
                                      IReadOnlyList<EffectOp> ops, string by = "效果",
                                      UnitState seed = null)
        {
            if (ctx == null || ops == null || ops.Count == 0) return;

            // 🔑 **`this troop` / `this unit` / `it` 指的是「触发的那一个」** —— 但解析器把这一族代词
            //    统一归到 `prev`（`EffectTargetSpec.Side == "prev"`，那是给战术卡的
            //    `Choose a troop … Draw it` 用的）。触发式正文里**没有「上一句」**，
            //    所以由这里把 `source` 放进去当「上一条的目标」—— 借的是**同一套代词机制**，不另开口子。
            // ⚠️ **不种的话是静默错打**：`Return this troop to your hand`（`Warp Spider`）
            //    会去回手**上一张被指过的牌**，场上看不出哪里不对。
            //    种子**只在开头放一次** —— 同一段正文里后面的 `and give it …` 要能接着用
            //    前一条效果刚定下的目标（那正是 `LastTargets` 的用法）。
            //
            // ⚠️ **`seed` 覆盖 `source`**（2026-09-13 事件层加的）：事件触发时，
            //    卡面的 `it` 指的是**发生那件事的单位**（刚部署的那台载具 / 刚死的那个兵），
            //    **不是**监听者自己。`BroadcastWhen` 把那个单位从 `seed` 传进来。
            //    不传 = 老行为（用 `source`），所以既有调用点一处都不用改。
            var savedTargets = new List<UnitState>(ctx.LastTargets);
            var savedLast = ctx.LastTarget;
            var prime = seed != null ? seed : source;
            if (prime != null)
            {
                ctx.LastTargets.Clear();
                ctx.LastTargets.Add(prime);
                ctx.LastTarget = prime;
            }

            var unresolved = new List<string>();
            for (int i = 0; i < ops.Count; i++)
                ResolveOne(ctx, owner, source, by, ops[i], null, unresolved);

            // 用完还原：调用方（`FireTriggerAt` 上面那层）可能还指望原来的值
            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(savedTargets);
            ctx.LastTarget = savedLast;
        }

        /// <summary>
        /// **回合起止触发段** —— 规则书「回合结构」第 9 步（开始）/ 第 14 步（结束）。
        ///
        /// 段内顺序规则书**没规定**，照 `rule_core.gd:400-403` 定的确定性口径：
        ///   **① 当前行动方手牌里的陷阱卡 → ② 已登记的常驻效果（属于当前行动方的）**。
        /// ⚠️ **不许改成随机** —— 对局必须可复现（工程铁律）。
        ///
        /// 为什么先**快照**再结算：结算会改手牌与棋盘（`your troops take 1 damage` 会死人），
        /// 边遍历边改列表是未定义行为。
        /// </summary>
        public static void ResolveAtTurn(BattleContext ctx, string phase)
        {
            if (ctx.IsOver) return;
            int active = ctx.Active;
            var jobs = new List<EffectOp>();

            // ① 当前行动方**手牌里**的陷阱卡（被塞进来的破坏卡 —— **持有者**回合生效）
            foreach (var c in ctx.Players[active].Hand)
            {
                if (c == null) continue;
                var at = EffectText.SplitAtTurn(c.Desc);
                if (at == null || at[0] != phase) continue;
                var ops = EffectText.Parse(at[1], out _, out _);
                if (ops == null) continue;
                foreach (var o in ops)
                {
                    o.Source = c.Name + "：" + at[1];      // 日志要看出「是哪张陷阱干的」
                    jobs.Add(o);
                }
            }

            // ② 已登记的常驻效果 —— **只有它自己那一方的回合才触发**（`rule_core.gd:434`）
            // ⚠️ 只收 `Trigger == "turn"` 的：`Trigger == "deploy"` 那批（部署时给）走
            //    `RuleCore.ResolveDeploy`，**不在这条路上**。虽然它们的 `Phase` 是 `deploy`
            //    天然对不上 `phase`，但显式判一下，免得以后加了新 Phase 名就串味。
            foreach (var pe in ctx.PersistentEffects)
            {
                if (pe.Trigger != "turn") continue;
                if (pe.Owner != active || pe.Phase != phase || pe.Ops == null) continue;
                foreach (var o in pe.Ops) jobs.Add(o);
            }

            if (jobs.Count == 0) return;

            string label = "回合" + (phase == "turn_start" ? "开始" : "结束") + "触发";
            ctx.Log($"—— {label}段：{jobs.Count} 条 ——");
            var unresolved = new List<string>();
            foreach (var o in jobs)
                ResolveOne(ctx, active, null, label, o, null, unresolved);
        }

        /// <summary>
        /// **部署时触发段** —— 某个单位刚被放上场时跑一遍。
        ///
        /// 卡面形如 `For the rest of this battle, give Shield to all Drones you deploy`
        /// （TauEmpire `Experimental Drone`）/ `… give Armour 1 to Vehicles you put in play`
        /// （Ultramarines `Armoured Support`）。它们登记在 `ctx.PersistentEffects` 里
        /// （`Trigger == "deploy"`），**不被回合清理** —— 和「本场战斗」的字面意思一致。
        ///
        /// **触发时机**：<see cref="RuleCore.PlayCard"/>（从手牌打出）与
        /// <see cref="RuleCore.DeployFree"/>（效果免费部署）**两条路都调** ——
        /// 卡面写的是 `you **put in play**`，免费部署也是「放进场上」。
        ///
        /// **原版出处**：`CardScript.ResolveUnitSummoned`（`CardScript__ResolveUnitSummoned.c:33`）
        /// → `OnTrigger(AbilityTrigger.OtherUnitSummoned = 190, …)`；广播器
        /// `BattleManagerSupport__BroadcastUnitSummoned.c` 依次发给**自己 `:23` → 场上每张牌 `:41`
        /// → 当前回合方手牌 `:53` → 另一方手牌 `:65`**。我们只实现「打出的那一方登记的常驻效果」这一支
        /// —— 手牌里那些等着被 `SetupCardInHand` 挂效果的**还没做**（见 `资料/常驻效果_数据与设计.md` §六）。
        ///
        /// ⚠️ **与 Rally 的先后**：`RuleCore.PlayCard` 里这段排在 `Emit(Deploy)` 之后、
        ///    **`Rally` 之前** —— 这样 Rally 结算时看得见刚给出的关键词。
        ///    这一条是**我们挑的**：原版 `ResolveUnitSummoned` 里看不到 Rally 走哪条触发，先后无据可查。
        ///
        /// ⚠️ **串行保护**：部署效果自己可能又部署（`Deploy …`），递归下靠
        ///    `ctx.EffectChain` 截断，和 `RuleCore.FireTriggerAt` 用同一个上限。
        /// </summary>
        public static void ResolveDeploy(BattleContext ctx, int owner, UnitState unit)
        {
            if (ctx == null || unit == null || unit.Card == null || ctx.IsOver) return;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{unit.Name} 的部署触发不再连锁");
                return;
            }

            // 先快照再结算：结算会改棋盘（部署效果可能又部署），边遍历边改列表是未定义行为。
            var jobs = new List<PersistentEffect>();
            foreach (var pe in ctx.PersistentEffects)
            {
                if (pe.Trigger != "deploy" || pe.Owner != owner || pe.Ops == null) continue;
                // 筛选条件：`Drone` / `Vehicle` / 带 `Destroyer` 的 …；空 = 不筛
                // ⚠️ **传 `unit` 不传 `unit.Card`**（2026-09-13 第三十四轮）：关键词那一维要问
                //    单位**身上**有没有 —— 运行时加的那些（`Hunt Mark` / `Dark Pact`）卡面没印，
                //    拿 `CardDef` 判会**永远判不中且不报错**。见 `CardCriteria.Matches(UnitState)`。
                if (pe.Criteria != null && !pe.Criteria.IsEmpty && !pe.Criteria.Matches(unit)) continue;
                jobs.Add(pe);
            }
            if (jobs.Count == 0) return;

            // `LastTargets` 是**代词机制**的存放位置（`give it/them X` 靠它）。
            // 「刚部署的那个」借它表达 —— 不另开一份存放位置。**用完必须还原**，
            // 否则会踩到调用方（`DoDeploy` 的 `and give them Vanguard` 尾句）刚放进去的那批。
            var savedTargets = new List<UnitState>(ctx.LastTargets);
            var savedLast = ctx.LastTarget;

            var unresolved = new List<string>();
            foreach (var pe in jobs)
            {
                string name = pe.Source != null ? pe.Source.Name : "?";
                ctx.Log($"—— 部署触发段：「{name}」盯上 {unit.Name}"
                      + $"（{pe.Criteria}）→ {pe.Body} ——");
                // ⚠️ **每一条之前都要重设**：上一条的结算会把 `LastTargets` 覆盖掉，
                //    不重设的话第二条效果就打在了错误的目标上（静默错打）。
                ctx.LastTargets.Clear();
                ctx.LastTargets.Add(unit);
                ctx.LastTarget = unit;

                ctx.EffectChain++;
                foreach (var o in pe.Ops)
                    ResolveOne(ctx, owner, unit, "部署触发", o, null, unresolved);
                ctx.EffectChain--;
            }

            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(savedTargets);
            ctx.LastTarget = savedLast;
        }

        /// <summary>
        /// **事件广播段** —— 某件事（部署 / 死亡 / 攻击 / 受伤）发生了，回头问「谁在听」。
        ///
        /// 这是第三十二轮做的**事件层的下半截**：上半截（`Core/WhenEvent.cs`）只把
        /// 卡面 `When &lt;事件&gt;, &lt;正文&gt;` 认出来、收进 `CardDef.WhenTriggers`；
        /// **没人广播的话那些监听器一辈子不会响**（而卡面照样不打 `*` —— 静默失效）。
        ///
        /// **原版出处**：`BattleManagerSupport__BroadcastUnitSummoned.c` —— 部署事件依次发给
        /// **自己 `:23` → 场上每张牌 `:41` → 当前回合方手牌 `:53` → 另一方手牌 `:65`**。
        /// 我们**只做「场上每张牌」这一支**（`:41`）——
        /// ⚠️ 手牌那两支（`:53`/`:65`）就是「牌在手里也要监听」，属**降费那一族**，
        ///    由 <see cref="RuleCore"/> 在 `CostWhens` 上另接（见 `资料/事件层_数据与设计.md` §三）。
        ///
        /// <param name="kind">见 <see cref="WhenEventKind"/>：`deploy` / `die` / `attack` / `damaged`</param>
        /// <param name="who">**发生这件事的单位归谁**；`-1` = 无归属（例：疲劳伤害没有来源单位）</param>
        /// <param name="card">那个单位的卡（判兵种/关键词用）</param>
        /// <param name="subject">
        /// **发生这件事的那个单位**（卡面里的 `it` / `this troop` 指的就是它）。
        ///
        /// 🔴 **不传的话是静默错打**（2026-09-13 自检抓出来的）：正文里写 `give **it** Flank` 时，
        ///    代词由 <see cref="ResolveOps"/> 从 `ctx.LastTargets` 里取 ——
        ///    而 `ResolveOps` 的默认行为是把 **`source`（监听者自己）** 种进去。
        ///    于是 `When you deploy a Vehicle, give it Flank` 会把 Flank 给**监听者自己**，
        ///    而不是给刚部署的那台载具 —— **而卡面照旧不打 `*`**（正文是好的），
        ///    只有「换个东西量一下」才看得出来（实测：断言量的那台车 Attack 没变）。
        ///    ⇒ 有 `subject` 时**由这里显式种**，`ResolveOps` 就不会再覆盖（它只在 `source != null` 时种，
        ///      而我们把 `source` 传 `null` 的那条路……不行，`source` 还要当 Owner 用）。
        ///    做法：**先种、再调**，并在调用前后备份/还原 `LastTargets`（和 `ResolveDeploy` 同一套）。
        /// </param>
        /// <param name="actor">
        /// **干这件事的那个单位**（2026-09-13 A3 加）—— `kills` 的**凶手**、`attacks` 的**攻击者**。
        /// 卡面里 `this unit …` 那类**自指**就靠它判（<see cref="WhenEvent.ActorSelf"/>）。
        /// ⚠️ 它和 <paramref name="subject"/> **不是一回事**：`kills` 的 `subject` 是**被杀的那个**。
        /// 不传 = 那一类监听器**一次都不响**（宁可收不到，也不放宽成「谁干的都算」）。
        /// </param>
        /// <param name="target">
        /// **这件事的宾语那个单位**（2026-09-13 A3 加）—— `attacks` 的**被打者**。
        /// 两个用途：① 筛「打的是不是带猎杀标记的敌人」（`When … attacks an enemy with Hunt Mark`）；
        /// ② 正文里的 `the target of the attack` / `adjacent enemies` 指的**就是它**
        /// （存进 `ctx.EventTarget`，见那个字段的注释）。
        /// </param>
        ///
        /// ⚠️ **先快照再结算**：监听器的效果会改棋盘（能打死人、也能再部署），
        ///    边遍历边改数组是未定义行为。和 <see cref="ResolveDeploy"/> 同一个理由。
        public static void BroadcastWhen(BattleContext ctx, string kind, int who, CardDef card,
                                         UnitState subject = null,
                                         UnitState actor = null, UnitState target = null)
        {
            if (ctx == null || kind == null || ctx.IsOver) return;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，「{kind}」事件的监听不再连锁");
                return;
            }

            // 宾语的**归属**在**入口处定一次** —— 监听器可能把它打死（前一条监听器的效果），
            // 而「事件发生那一刻它归谁」才是该判的东西。
            // ⚠️ `UnitState` 上**故意没有**「我归谁」字段（格位是棋盘的事，见 `RuleCore.FindUnit`），
            //    所以只能在这儿算、当参数传进判据（判据本身仍然只在 `WhenEvents.Matches` 一处）。
            int targetWho = target != null ? OwnerOf(ctx, target) : -1;

            // ---- ⓪ 手牌里的监听器（事件触发式降费那一族）----
            // 🔴 **必须排在下面那句「场上没人听就 return」之前**：
            //    这一族的监听器挂在**玩家身上**、和自家场上有没有单位**毫无关系** ——
            //    `Lower cost by 1 when an enemy dies` 在自己一个兵都没有时照样该降。
            //    放到后面会被那条早退**静默跳过**（正是本工程红线禁止的失效方式）。
            BroadcastCostWhen(ctx, kind, who, card);

            // ---- ① 快照：双方棋盘上所有还活着的单位 ----
            var listeners = new List<UnitState>();
            for (int p = 0; p < 2; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || u.Card == null || !u.IsAlive) continue;
                    if (u.Card.WhenTriggers.Count == 0) continue;   // 没监听器的卡直接跳过
                    listeners.Add(u);
                }
            }
            if (listeners.Count == 0) return;

            // ---- ② 逐个问「这条事件你算不算」，算的就把 op 结算掉 ----
            // ⚠️ `LastTargets` 是**代词机制**的存放位置：`it` / `this troop` 全从它取。
            //    事件的「它」= **发生这件事的那个单位**（刚部署的那台载具 / 刚死的那个兵），
            //    **不是**监听者自己。用完必须还原，别踩到调用方刚放进去的那批。
            var savedTargets = new List<UnitState>(ctx.LastTargets);
            var savedLast = ctx.LastTarget;
            // ⚠️ `EventTarget`（宾语）也一起 —— 它是**另一件事**，不是 `LastTarget` 的别名：
            //    `Doomstalker` 的 `it` 指**攻击者**（由 `subject` 种进 `LastTargets`），
            //    `Valtus` 的 `the target of the attack` 指**被打的那个**（走这个字段）。
            //    同一句里两个指代同时存在，所以**必须是两个槽**。
            var savedEventTarget = ctx.EventTarget;
            ctx.EventTarget = target;

            foreach (var u in listeners)
            {
                // ⚠️ 快照之后世界可能已经变了（前一个监听器把人打死了）——再判一次
                if (!u.IsAlive) continue;

                int owner = OwnerOf(ctx, u);
                if (owner < 0) continue;                             // 已经不在场上了

                var ops = u.Card.FireWhen(kind, owner, who, card, u, subject, actor, target, targetWho);
                if (ops == null || ops.Count == 0) continue;

                var names = new List<string>();
                foreach (var t in u.Card.WhenTriggers)
                    if (t.Ev != null && t.Ev.Kind == kind) names.Add(t.ToString());
                ctx.Log($"—— 事件「{kind}」触发：「{u.Name}」的监听器 → "
                      + string.Join("；", names.ToArray()) + " ——");

                // ⚠️ **每次都重种**：上一条效果会把 `LastTargets` 覆盖掉
                //    （和 `ResolveDeploy` 里那句「每一条之前都要重设」同一条教训）。
                //    ⚠️ 这里**不再自己种** —— 交给 `ResolveOps` 的 `seed` 参数，
                //       否则它会紧接着把 `source`（监听者自己）盖上去，等于白种。
                ctx.EffectChain++;
                ResolveOps(ctx, owner, u, ops, "事件触发", subject);
                ctx.EffectChain--;

                if (ctx.IsOver) break;
            }

            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(savedTargets);
            ctx.LastTarget = savedLast;
        }

        /// <summary>
        /// **手牌监听段** —— `… Lower cost by 1 when &lt;事件&gt;` 那一族（`CardDef.CostWhens`）。
        ///
        /// 2026-09-13 第三十四轮接上。**在这之前 `FireCostWhen` 一直没人调** ——
        /// 卡把这族事件收下了（`CardDef.CostWhens` 有 3 张），但**费用从来不会真的降**，
        /// 而卡面也不会打 `*`（正文是好的）⇒ 典型的静默失效。
        ///
        /// **原版出处**：牌**在手上就要开始监听** —— `PlayerHand.SetupCardInHand`
        /// （`PlayerHand__SetupCardInHand.c:60-73`）在牌入手牌那一刻按 `TargetCriteria`
        /// 把效果挂到**那张牌自己身上**。我们把它落成 `ctx.CostMods` 上的一条修正。
        ///
        /// **⚠️ 三处「我们挑的 / 我们的近似」，都如实标着**：
        ///   ① **按卡 id 匹配**（`CostMod.Key = CardDef.Id`）。手里两张同名卡**仍是同一个
        ///      `CardDef` 对象**（`CardDef._costWhens` 是卡表级的、不是每张牌一份），
        ///      所以「只给这一张降」做不到 —— 会给该 id 的**所有副本**一起降。
        ///      这是规则书审计第③条剩下的那半（卡实例身份），
        ///      见 `资料/规则书_实现指南对账.md` §一·③。比按卡名好（同名跨阵营不会误伤），
        ///      但仍**不等于**原版的复制品语义。
        ///   ② **每满足一次事件就叠一次**（卡面字面 `Lower cost by 1 when …`）。
        ///      `rule_core.gd` 这四张卡**一张都没实现** ⇒ **没有权威依据**。
        ///   ③ **登记后不过期**（跟着对局走）—— 与 <see cref="DoCostMore"/> 的永久修正是同一条。
        ///
        /// ⚠️ **排在场上监听器之前**（调用点见 <see cref="BroadcastWhen"/>）：
        ///    场上监听器的效果可能抽牌/改手牌，那些**事件发生之后才到手**的牌
        ///    不该为这一次事件享受降费。先扫手牌 = 扫的是「事件发生的那一刻就在手里的牌」。
        /// </summary>
        static void BroadcastCostWhen(BattleContext ctx, string kind, int who, CardDef card)
        {
            for (int p = 0; p < 2; p++)
            {
                var hand = ctx.Players[p].Hand;
                for (int i = 0; i < hand.Count; i++)
                {
                    var c = hand[i];
                    if (c == null || c.CostWhens.Count == 0) continue;

                    // `listener` 传 p —— 手牌属于谁，极性（friendly / enemy）就相对谁说。
                    // 判据全在 `WhenEvents.Matches`（**只此一份**），这里不再自己判一次。
                    var hits = c.FireCostWhen(kind, p, who, card);
                    if (hits == null) continue;

                    foreach (var h in hits)
                    {
                        ctx.CostMods.Add(new CostMod
                        {
                            Player = p,
                            Key = c.Id,
                            Delta = -h.Delta,
                            ExpireTurn = -1,
                        });
                        ctx.Log($"—— 事件「{kind}」触发：「{c.Name}」在手里监听 → 费用 -{h.Delta}"
                              + $"（现价 {RuleCore.CostOf(ctx, p, c)}）——");
                    }
                }
            }
        }

        /// <summary>
        /// 发一条「**某个单位身上发生了关键词层面的事**」的事件 ——
        /// `When an enemy gets Hunt Mark, …` · `When you gain [Shield], …` ·
        /// `When an enemy receives a Stun, …` · `When a friendly unit loses Stealth, …`
        /// （2026-09-13 第三十四轮）。
        ///
        /// **为什么收在一处**：这四件事的发生点散在**两个文件三处**
        /// （`ApplyOneGain` 的 `AddKeyword` · `DoStun` 与 `Concussion` 的 `IsStunned` ·
        ///  `DeclareAttack` 里的 `RemoveKeyword(Stealth)`），但「这件事发生在**谁**身上」
        /// 的算法是**同一条**（<see cref="OwnerOf"/>）。各写各的迟早不一致。
        ///
        /// ⚠️ **`who` 一定要是「发生这件事的那个单位归谁」**，不是「谁干的」——
        ///    `When an **enemy** gets Hunt Mark` 的极性判据在 `WhenEvents.Matches` 里
        ///    拿它和监听者的阵营比，给错就是整档反着触发、而且不报错。
        /// </summary>
        static void BroadcastKeywordEvent(BattleContext ctx, string kind, UnitState u)
        {
            if (u == null || u.Card == null) return;
            BroadcastWhen(ctx, kind, OwnerOf(ctx, u), u.Card, u);
        }

        /// <summary>这个单位在**谁**的棋盘上。不在场上返回 -1（快照之后可能已经被打死了）。</summary>
        static int OwnerOf(BattleContext ctx, UnitState u)
        {
            for (int p = 0; p < 2; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (ReferenceEquals(board[s], u)) return p;
            }
            return -1;
        }
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

            // ---- ⓪b 上一次「花掉全部灵魂石」花了几颗（2026-09-13 第三十三轮）----
            // 正主只有一张：`Hosts of the Dead` = `Deploy a Wraithguard. Spend all your Spirit Stones.
            // **For each one, deploy a Wraithguard**` —— 这里的「one」指的就是**刚花掉的那几颗**。
            // 数量由 `DoSpendSpirit` 记进 `ctx.LastSpentSpirit`（**只算一次**），这里只读。
            if (op.CountScope == "spiritspent") return ctx.LastSpentSpirit;

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
                    if (c == null) continue;
                    if (reference == "any" || reference == "card") { n++; continue; }
                    if (!DrawnMatches(c, reference)) continue;
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
                    ApplyOneGain(ctx, owner, t, p, sign, op.Duration, by, null, unresolved);
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
                                 int sign, string duration, string by, string grantSource = null,
                                 List<string> unresolved = null)
        {
            int val = p.Value * sign;

            if (p.IsKeyword)
            {
                if (val >= 0)
                {
                    u.AddKeyword(p.Keyword, val);
                    // 🆕 事件层：`When an enemy gets Hunt Mark, …` / `When you gain [Shield], …`
                    // （2026-09-13 第三十四轮）—— **授予点就是这里**，原来只是没人广播。
                    // ⚠️ `val > 0` 才算「获得」：`val == 0` 是空给，不该触发「有人拿到了」。
                    // ⚠️ **只播这两个关键词**，别顺手推广成「任何关键词被给予」 ——
                    //    多播一种就多一类可能被误触发的监听器，而卡面并没有那种写法。
                    if (val > 0)
                    {
                        if (p.Keyword == "huntmark") BroadcastKeywordEvent(ctx, WhenEventKind.GetsHuntMark, u);
                        else if (p.Keyword == KeywordTable.Shield) BroadcastKeywordEvent(ctx, WhenEventKind.GetsShield, u);
                    }
                }
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

                    // ⚠️ **兜底护栏：不认识的属性词 —— 报出来，别静默什么都不做**（2026-09-13 候选 E 加）。
                    //
                    //    🔴 **先把事实说清楚（这很重要，别被这段注释误导）**：
                    //      ① `GivePayload.Parse` 的属性词是**白名单**（`ReAttr` 正则 + 裸 `+N` 兜底），
                    //         `PayloadOp.Attr` **只可能**是 attack / ranged / health / armour / energy 之一
                    //         ⇒ **这个 `default` 分支今天够不到**，它是护栏、不是修好的 bug。
                    //      ② 卡池里那 4 张「属性词不对」的卡（`Banner Nob` 的 `+1 [weapon]` ·
                    //         `Hidden Hunters` 的 `[power]` · `Avenging Zeal` 的 `[health icon]` / `[attack icon]`）
                    //         **在更早的地方就断了**：`weapon` 不在 `ReAttr` 的白名单里 ⇒
                    //         `GivePayload.Parse` 返回空 ⇒ `DoGive` 走「**载荷…本版不认识 —— 这条没生效**」
                    //         并把卡名记进 `unresolved`（实测事件流里两行都在）。
                    //         ⇒ **它们本来就被如实报出来了，不是静默失效。**
                    //      ③ 但卡面写「+1 ⟨紫枪⟩ = 远程」而实际不生效，**仍然是该修的** ——
                    //         那是**数据侧**的活（把词改对），走 `资料/普查产出_0913/卡表三堆裁定.md` 那套流程。
                    //
                    //    ⇒ 那为什么还留这个 `default`：**属性词表加人时忘了在这里加 case** 的话，
                    //      就会退化成「静默什么都不做」—— 和这个工程反复踩的那类坑**同形**。
                    //      护栏成本 6 行，值得。
                    default:
                        ctx.Log($"{by}：「{p.Source}」要给 {u.Name} 加「{p.Attr}」，"
                              + "但**属性词表里没有这个词** —— **这条没生效**");
                        if (unresolved != null)
                            unresolved.Add($"{p.Source}（属性「{p.Attr}」不认识）");
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
                    // 真卡名（`by` 在战术卡那条路上恒为「战术卡」，不足以定位「本卡施加的」）
                    SourceCard = ctx.PlayingCard != null ? ctx.PlayingCard.Name : by,
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

            // 🆕 `When a friendly troop receives a Dark Pact, …`（2026-09-13 第三十三轮）。
            // ⚠️ `who` = **拿到契约那个单位**的阵营（不是给它的那个人的）—— 和 `Die` 的极性判据同一条：
            //    监听方要判的是「**我这边**有没有人收到契约」。
            int pactOwner = OwnerOf(ctx, u);
            if (pactOwner >= 0) BroadcastWhen(ctx, WhenEventKind.GetsDarkPact, pactOwner, u.Card, u);

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

        /// <summary>
        /// **阵营资源 +N**（信仰 / 灵魂石 / 任务点）—— 2026-09-13 第三十三轮。
        ///
        /// 和能量**不一样的地方**：这三个**没有上限**（用户 2026-09-12 亲口定的口径：
        /// 信仰「没有上限、不会衰减」；灵魂石「没有初始值、没有上限、没有每回合增长」），
        /// 所以这里**不加 `Min(上限, …)`** —— 照抄能量那条会悄悄给它设一个上限。
        ///
        /// 数值来源只有两处（**都不是「缺」，是本来就没有**）：
        ///   · 卡面效果（`Gain 2 Spirit Stones` / `Gain 1 ☀` / `Gain 2 任务点`）
        ///   · 路标石单位死亡 → +1 灵魂石（规则书 `:210`/`:225`，在 `KillUnit` 里）
        /// </summary>
        static bool DoFactionResource(BattleContext ctx, int owner, string by, EffectOp op, string kind,
                                      List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            bool faith = kind == "faith", quest = kind == "quest";
            string what = quest ? "任务点" : (faith ? "信仰" : "灵魂石");
            int n = op.Amount;
            if (n <= 0)
            {
                ctx.Log($"{by}：「{op.Source}」要给{what}，但数量是 0 —— **这条没生效**");
                unresolved.Add(op.Source + "（阵营资源数量为 0）");
                return false;
            }
            if (quest)
            {
                ps.QuestPoints += n;
                ctx.Log($"{by}：「{op.Source}」任务点 +{n}（现 {ps.QuestPoints}）");
                QuestPointThreshold(ctx, owner, by, op);
            }
            else if (faith) { ps.Faith += n; ctx.Log($"{by}：「{op.Source}」信仰 +{n}（现 {ps.Faith}）"); }
            else { ps.SpiritStones += n; ctx.Log($"{by}：「{op.Source}」灵魂石 +{n}（现 {ps.SpiritStones}）"); }

            // 🆕 广播：`When you gain Faith, …`（`Paragon Warsuit`）/ `When you collect a Spirit Stone, …`
            //    （4 张灵族单位）—— 卡面真的这么写，所以这里必须发得出来（`WhenEvent` 认它）。
            ctx.Signals.Add(new BattleEvent
            {
                Kind = quest ? EvtKind.GainQuest : (faith ? EvtKind.GainFaith : EvtKind.GainSpirit),
                Player = owner,
                Slot = -1,                       // 阵营资源是**玩家的**，不属于任何格位
                Amount = n,
                Effect = $"{what} +{n}",
                Turn = ctx.Turn,
            });

            // 🔴 **事件层广播**（2026-09-13 候选 E 补）—— **这三种原来一个广播点都没有**。
            //
            //    `WhenEvent.Parse` 早就认得出 `When you gain Faith, …` / `When you collect a Spirit Stone, …`
            //    （`WhenEvent.cs` 的 `gain faith` / `collect spirit stone` 两条分支），
            //    卡面也**不打 `*`**（正文是好的）—— 但 `BroadcastWhen` **从来没被调用过** ⇒
            //    `Paragon Warsuit`（`When you gain Faith, deal 4 damage to the enemy warlord`）
            //    和 4 张灵族单位（`When you collect a Spirit Stone, gain Shield`）
            //    注册的是**一条永远不会响的监听器**。这正是本工程红线里的**静默失效**：
            //    「认得出」不等于「发得出来」。
            //
            //    ⚠️ **上面那句 `ctx.Signals.Add` 不能顶替它** —— 那只喂**表现层**（放特效/飘字），
            //       和事件层是两条完全不同的通道。**别把 Signals 当成广播**（这次就是这么漏的）。
            //
            //    ⚠️ `who` 传**获得资源的那一方**：卡面写 `you gain Faith` ⇒ `Parse` 会把 `OwnerIs`
            //       设成 `RelFriendly`，而 `Matches` 拿 `who` 和**监听者所属方**比 ——
            //       传别人就是整档反着触发。
            //    ⚠️ `card` / `subject` 传 `null`：这是**玩家级**事件，没有具体单位
            //       （和 `When you draw a card` 同一类）。
            BroadcastWhen(ctx, quest ? WhenEventKind.GainQuest
                                  : (faith ? WhenEventKind.GainFaith : WhenEventKind.GainSpirit),
                         owner, null, null);
            return true;
        }

        /// <summary>任务点的阈值（**只此一处**）：每满 3 点 → 向牌库加入 1 张隐秘并洗牌。</summary>
        private const int QuestPointStep = 3;

        /// <summary>
        /// **规则书 `:199`**：「任务（Quest）| **每获得 3 点任务**：向牌库加入 1 张**隐秘**并洗牌」。
        ///
        /// ⚠️ **「隐秘」（Secret）是造牌**（`CreatePool` 的 `secret` 那一类），找不到就如实报出来、
        ///    **不假装成功** —— 但**任务点照样扣/留**（阈值是按累计值算的，和造不造得出无关）。
        /// ⚠️ 阈值判据放在**这一处**：`DoFactionResource` 每次加完调它。
        /// </summary>
        static void QuestPointThreshold(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var ps = ctx.Players[owner];
            int gained = ps.QuestPoints / QuestPointStep;      // 累计满了几次
            if (gained <= ps.QuestMilestone) return;           // 这次没跨过新的阈值
            int times = gained - ps.QuestMilestone;
            ps.QuestMilestone = gained;

            // 施放者阵营 = 自己督军那张卡的 `faction`（和 `DoCreate` 同一条判据）
            string faction = null;
            if (ps.Warlord != null && ps.Warlord.Card != null) faction = ps.Warlord.Card.Faction;

            for (int i = 0; i < times; i++)
            {
                var pick = CreatePool.Resolve(ctx.CardPool, "a secret", faction);
                if (pick == null || pick.Cards == null || pick.Cards.Count == 0)
                {
                    // **不假装成功**：造不出来就明说（但任务点照样留着 —— 阈值按累计值算）
                    ctx.Log($"{by}：任务点到 {ps.QuestPoints}（第 {gained} 次）**本该**往牌库加 1 张隐秘，"
                          + "但卡池里找不出「隐秘」这类牌 —— **这条没生效**");
                    continue;
                }
                var card = pick.Cards[ctx.Rng.Next(pick.Cards.Count)];
                ps.Deck.Add(card);
                ctx.Log($"{by}：任务点满 {QuestPointStep}（第 {gained} 次）→ 往牌库加入「{card.Name}」");
            }
            Shuffle(ps.Deck, ctx.Rng);                         // 「并洗牌」（规则书 :199）
        }

        /// <summary>
        /// **`Spend all your Spirit Stones`**（`Hosts of the Dead`：`Deploy a Wraithguard.
        /// Spend all your Spirit Stones. For each one, deploy a Wraithguard`）—— 2026-09-13 第三十三轮。
        ///
        /// ⚠️ 它**必须和后面那句 `For each one, …` 一起才成立**（花掉的数量就是后面重复的次数）。
        /// 这里的做法：把花掉的数量记进 `ctx.LastSpentSpirit`，由紧跟的 `repeat` 读它 ——
        /// **次数只有一处判据**，不在两处各算一遍。
        /// </summary>
        static bool DoSpendSpirit(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            int n = ps.SpiritStones;
            if (n <= 0)
            {
                ctx.Log($"{by}：「{op.Source}」要花掉全部灵魂石，但一颗都没有 —— **这条没生效**");
                unresolved.Add(op.Source + "（没有灵魂石可花）");
                ctx.LastSpentSpirit = 0;
                return false;
            }
            ps.SpiritStones = 0;
            ctx.LastSpentSpirit = n;
            ctx.Log($"{by}：「{op.Source}」花掉全部 {n} 颗灵魂石");
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
                case "energyzero":
                {
                    // `Codex:` —— 「你的能量为 0 时触发效果」（规则书 :175）。
                    // ⚠️ 判的是**结算那一刻**的能量：这张卡的费已经扣过了，所以「刚好用光」才成立，
                    //    和原版的 `_check_codex` 在 `play_card` 之后判是同一个时点。
                    holds = ctx.Players[owner].Energy == 0;
                    return true;
                }
                case "damaged":
                {
                    var t = chosen ?? ctx.LastTarget;
                    if (t == null) return false;
                    holds = t.Health < t.MaxHealth;
                    return true;
                }
                case "anypraying":
                {
                    // `if any friendly unit is Praying`（`Sororitas Rhino`）。
                    // 判据 = `UnitState.Prayed`，**只此一份**（`Each friendly unit that is Praying`
                    // 那条走的是目标池筛，读的也是它 —— 见 `EffectTargetSpec.PrayedOnly`）。
                    bool any = false;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var pu = ctx.Players[owner].Board[s];
                        if (pu != null && pu.Prayed) { any = true; break; }
                    }
                    holds = any;
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

        /// <summary>
        /// 抽到的这张牌算不算 `For each <em>X</em> drawn` 里的 X。
        ///
        /// ⚠️ **2026-09-12 更正**：这里原来有一句「兵种词过滤不了（卡表里没有兵种字段）」，
        /// 于是**一律 n++**（`For each troop drawn` 变成了「每抽一张」）—— 那句话当时是对的，
        /// 原版 `subtype` 是这一天补回来的（`CardDef.Subtype`），补回来之后就成了错的。
        /// 现在按 `Type` / `Subtype` 真筛。
        ///
        /// `reference` 的取值由 `EffectText.ClassifyCount` 的正则给定，只有这几个词（不是开放词表），
        /// 所以这里穷举——**认不出的按「都算」并留了注释**，不假装认得。
        /// </summary>
        static bool DrawnMatches(CardDef c, string reference)
        {
            switch (reference)
            {
                // `troop` / `unit` = 单位卡（督军是 `type=hero`，不算）
                case "troop": case "unit": return c.Type == "unit";
                case "vehicle":  return Same(c.Subtype, "Vehicle");
                case "infantry": return Same(c.Subtype, "Infantry");
                // `counter` 是 `ClassifyCount` 正则里那个兜底词，卡池里没有对应兵种 —— 都算
                default: return true;
            }
        }

        static bool Same(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// **结算层真的实现了的动词** —— 解析得出来但 `EffectDispatch` 里没登记的动词
        /// （现在只剩 `repeat` / `lowercost` / `reanimate`），效果是**打出去什么都不发生**。
        ///
        /// 给 `EffectText.Coverage` 在**解析阶段**报「能解析但没机制」用 ——
        /// 不然它们会躺在「载荷有机制」那一栏里冒充能跑（第 ③ 栏要防的就是这种静默失效）。
        ///
        /// ⚠️ **从 `EffectDispatch` 派生**（2026-09-12 改）。以前这里是**手写的第二份清单**，
        ///    和 `ResolveOne` 的 `switch` 要手工对齐；忘了改一边就是「能打但不报」或者
        ///    「报了但能打」，两种都是静默的。现在加动词只需在 `EffectDispatch` 加一行。
        /// </summary>
        public static readonly System.Collections.Generic.HashSet<string> ImplementedEffectVerbs =
            new System.Collections.Generic.HashSet<string>(EffectDispatch.Keys);

        /// <summary>
        /// `Lower its Health to N` —— **把当前生命设成 N**（2026-09-13 A4）。
        /// 出处：`Eternal Servitude`（Sautekh）；原版那一支是 `ResolveChangeMaxHealth`，
        /// 但卡面写的是「its Health」（当前生命），照卡面做。
        /// ⚠️ **只降不升**（动词是 `lower`）：已经 ≤ N 的不动，并**如实打一行日志** ——
        ///    「没生效」和「本来就低于 N 所以不用改」是两件事，别让它们长得一样。
        /// ⚠️ 走 `EmitHit` 发伤害事件（表现层靠它掉血飘字/血条动画）——
        ///    直接改 `Health` 而不发事件，画面上会**看不到变化**（第三十四轮 Hunt Mark 那条踩过）。
        /// </summary>
        static bool DoSetHealth(BattleContext ctx, int owner, string by, EffectOp op,
                                UnitState chosen, List<string> unresolved)
        {
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any", Count = 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要降生命，但没有指到任何单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（sethealth 没有目标）");
                return false;
            }

            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                if (t.Has("invulnerable"))
                {
                    // 无敌 = 「无法被伤害或摧毁」（规则书 :190）—— 直接改生命等于绕过它，不行
                    ctx.Log($"{t.Name} 有 Invulnerable —— 「{op.Source}」改不了它的生命");
                    continue;
                }
                if (t.Health <= op.Amount)
                {
                    ctx.Log($"{t.Name} 的生命已经是 {t.Health}（≤ {op.Amount}）—— 「降」不动它");
                    continue;
                }
                int before = t.Health;
                t.Health = op.Amount;
                EmitHit(ctx, t, before - op.Amount);
                n++;
                ctx.Log($"{t.Name} 的生命 {before} → {op.Amount}（「{op.Source}」）");
            }
            if (n == 0) { unresolved.Add(op.Source + "（sethealth 一个都没改）"); return false; }
            return true;
        }

        /// <summary>
        /// `Lower cost by N when &lt;事件&gt;` 的**标记 op** —— 见 `EffectText.TryCostWhenStub`。
        /// 它存在的唯一目的是让那句话**解析得出来**（`IsFullyParsed` 的三处消费点全靠它）；
        /// 真正的降费在**牌还在手上时**就登记进 `ctx.CostMods` 了，这里什么都不用做。
        /// ⚠️ **仍然打一行日志**：不清不楚的「什么都没发生」和「实现好了」在画面上一样。
        /// </summary>
        static bool DoCostWhenStub(BattleContext ctx, int owner, string by, EffectOp op)
        {
            ctx.Log($"{by}：「{op.Source}」是**事件触发式降费**（降 {op.Amount} 费）—— "
                  + "它在牌还在手上时就已经登记好了，结算这一步不用再做（这行是说明，不是失败）");
            return true;
        }

        /// <summary>
        /// `This costs N less if you control a unit with &lt;关键词&gt;` 的**标记 op**
        /// —— 见 `EffectText.TryCostIfControl`。（2026-09-13 A4 收尾·批 1）
        ///
        /// 和 <see cref="DoCostWhenStub"/> 同一条纪律：那句话存在的唯一目的是**让整句解析得出来**
        /// （`IsFullyParsed` 的消费点全靠它），**真正的降费在 `RuleCore.CostOf` 里现算**
        /// （读 `CardDef.CostIfControls`），结算这一步什么都不用做。
        ///
        /// 🔴 **这个 handler 是补上的**：第一版只加了 `EffectText` 那半边、忘了在
        ///    `EffectDispatch` 里登记 —— 症状是**卡照样打得出去**（解析是干净的），
        ///    但每次结算都往日志里写一行「动作 costifcontrol 本版还没实现」并被算进 `unresolved`。
        ///    是**逐阵营覆盖率表**先报出来的（SaimHann 那栏多了一条「动词 costifcontrol」）——
        ///    **加动词 = 解析表 + 结算表两处都要加**，这正是那张表存在的意义。
        /// </summary>
        static bool DoCostIfControlStub(BattleContext ctx, int owner, string by, EffectOp op)
        {
            ctx.Log($"{by}：「{op.Source}」是**静态条件降费**（控制着带「{op.Payload}」的单位时降 "
                  + $"{op.Amount} 费）—— 判据在 `RuleCore.CostOf` 每次现算，结算这一步不用再做"
                  + "（这行是说明，不是失败）");
            return true;
        }

        /// <summary>
        /// `Does nothing` —— **认识、而且本来就没事可做**（2026-09-13 A4）。
        /// 出处：`Improvised Barricade`（规则书 `:204` 的「破坏」假卡，原版设计就是空效果）。
        /// ⚠️ **仍然打一行日志** —— 红线是「不许**静默**失败」，这张是**明说的空**，
        ///    和「机制没做所以什么都没发生」必须能分辨（那两种状态在画面上长得一样）。
        /// </summary>
        static bool DoNoEffect(BattleContext ctx, int owner, string by, EffectOp op)
        {
            ctx.Log($"{by}：「{op.Source}」的效果就是「什么都不做」（原版设计如此，不是本版没做）");
            return true;
        }

        /// <summary>
        /// `Reload the Duty abilities …` —— 把带 `Duty` 的单位的「本局用过」复位（2026-09-13 A4）。
        /// 规则书 `:181`「职责：一次性能力，**可被「装填」再次使用**」；
        /// 语义照 `rule_core.gd:2912-2931`（一字不差）：`all your …` → 己方所有带 duty 的单位；
        /// 否则指代**前一句那个目标**（`ctx.LastTarget`）。
        /// </summary>
        static bool DoReloadDuty(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var targets = new List<UnitState>();
            bool all = op.Payload == "all";
            if (all)
            {
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[owner].Board[s];
                    if (u != null && u.IsAlive && u.Has(KeywordTable.Duty)) targets.Add(u);
                }
            }
            else
            {
                var t = ctx.LastTarget;
                if (t != null && t.IsAlive && t.Has(KeywordTable.Duty)) targets.Add(t);
            }

            if (targets.Count == 0)
            {
                // ⚠️ **如实报**：找不到带 duty 的单位不是「成功但没效果」，是这条没生效
                ctx.Log($"{by}：「{op.Source}」要装填职责，但没有可供装填的单位"
                      + (all ? "（你场上没有带 Duty 的单位）" : "（前一句没指到一个带 Duty 的单位）")
                      + " —— **这条没生效**");
                unresolved.Add(op.Source + "（没有带 Duty 的可装填单位）");
                return false;
            }

            int n = 0;
            foreach (var u in targets)
            {
                if (!u.DutyUsed) continue;              // 本来就能用，不用报
                u.DutyUsed = false;
                n++;
                ctx.Log($"{u.Name} 的职责被装填（本局可再次使用）");
            }
            ctx.Log($"{by}：「{op.Source}」装填了 {n} 个单位的职责（候选 {targets.Count} 个）");
            return true;
        }

        /// <summary>
        /// `lowercost` —— **降费**（`Lower the cost of X by N` / `They cost N less` / `Your troops cost N less`）。
        ///
        /// 挂在 `ctx.CostMods` 上，由 <see cref="RuleCore.CostOf"/> 现算（**不改 `CardDef`** ——
        /// 那是共享不可变对象，改它会污染整个卡池）。
        ///
        /// 三种「谁降价」：
        ///   · `(指代上一张)` —— `They cost 1 less` 接在 `Create three … in your hand` 后面，
        ///     指的是**刚造出来那批**（`ctx.LastCreated`，对应原版 `ctx.last_created`）
        ///   · `<兵种词>` —— `all Vehicles` / `all Beasts` / `troops` / `cards`
        ///   · `<卡名>` —— `Tyrnak and Fenrir`
        ///
        /// ⚠️ **两个如实标着的近似**：
        ///   ① 按**卡名**匹配，所以手里两张同名卡会一起降价（我们没有卡实例这个身份）；
        ///   ② **快照**：只把「现在就在手牌/牌库里」的那些登记下来（`Lower the cost of X by N`
        ///      在卡牌游戏里就是结算那一刻的集合）。之后抽到的同**名**卡因为①也跟着便宜。
        /// </summary>
        static bool DoLowerCost(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            int delta = -(op.Amount > 0 ? op.Amount : 1);
            int expire = op.Duration == "turn" ? ctx.Turn : -1;

            var keys = new List<string>();
            var shown = new List<string>();
            // 🆕 同下标存 **卡对象** —— 「设为 N 费」那一支要按**那张卡当时真费用**算差值（2026-09-13 A4）
            var cardRefs = new List<CardDef>();
            string detail;

            if (op.Payload == "(指代上一张)")
            {
                // 指代对象有两个来源，**先看刚造出来的、再看刚抽到的**：
                //   · `Create three … in your hand. They cost 1 less` → `ctx.LastCreated`
                //   · `Draw 3 cards and lower their cost by 3`（DarkAngels `Convoke the Circle`）、
                //     `Draw it and lower its cost by 3`（TauEmpire `Emergency Dispensation`）
                //     → `ctx.DrawnThisResolve`（「本次结算抽到的牌」）
                // ⚠️ 两个来源都按**一张卡一个窗口**清（`DrawnThisResolve` 在 `ResolveOps` 入口清、
                //    `LastCreated` 在 `DoCreate` 入口清），所以读不到上一张卡的陈旧值。
                //    只在 `LastCreated` 为空时才退到抽牌那一栏 —— 严格比原来更宽，不会改既有行为。
                var refs = ctx.LastCreated.Count > 0 ? ctx.LastCreated : ctx.DrawnThisResolve;
                if (refs.Count == 0)
                {
                    ctx.Log($"{by}：「{op.Source}」要降费，但前面没有可指代的卡"
                          + "（既没「刚造出来的」也没「刚抽到的」）—— **这条没生效**");
                    unresolved.Add(op.Source + "（it/they 没有指代对象）");
                    return false;
                }
                foreach (var c in refs) { keys.Add(c.Id); shown.Add(c.Name); cardRefs.Add(c); }
                detail = (ctx.LastCreated.Count > 0 ? "刚才造出来的那批（" : "刚才抽到的那批（")
                       + string.Join("、", shown.ToArray()) + "）";
            }
            else
            {
                string t = (op.Payload ?? "").ToLowerInvariant();
                bool inHand = true, inDeck = true;
                if (t.Contains(" in your hand and deck")) t = t.Replace(" in your hand and deck", "");
                else if (t.Contains(" in your deck")) { t = t.Replace(" in your deck", ""); inHand = false; }
                else if (t.Contains(" in your hand")) { t = t.Replace(" in your hand", ""); inDeck = false; }
                if (t.StartsWith("all ")) t = t.Substring(4).Trim();

                var pool = new List<CardDef>();
                if (inHand) pool.AddRange(ps.Hand);
                if (inDeck) pool.AddRange(ps.Deck);

                string where = inHand && inDeck ? "手牌与牌库" : (inHand ? "手牌" : "牌库");
                if (t == "cards" || t == "card" || t.Length == 0)
                {
                    foreach (var c in pool) { keys.Add(c.Id); shown.Add(c.Name); cardRefs.Add(c); }
                    detail = where + "里的所有牌";
                }
                else
                {
                    detail = "「" + t + "」";
                    foreach (string piece in t.Split(new[] { " and " }, System.StringSplitOptions.None))
                    {
                        string p2 = piece.Trim();
                        if (p2.Length == 0) continue;
                        if (CreatePool.IsKindWord(p2))
                        {
                            foreach (var c in pool)
                                if (CreatePool.MatchesKind(c, p2)) { keys.Add(c.Id); shown.Add(c.Name); cardRefs.Add(c); }
                        }
                        else
                        {
                            // ⚠️ **认不出的对象一律不登记**。第一版是「查不到就拿原文当 key」——
                            //    于是正则切错的 `f all beasts` 会被当成一张卡登记进去、**还报成功**。
                            //    静默的错行为比「这条没生效」糟糕得多。
                            var card = CreatePool.FindByName(ctx.CardPool, p2);
                            if (card == null)
                            {
                                ctx.Log($"{by}：「{op.Source}」里的「{p2}」既不是兵种词也不是卡名"
                                      + "（卡池里没有）—— **这条没生效**");
                                unresolved.Add(op.Source + "（降费对象「" + p2 + "」认不出）");
                                return false;
                            }
                            keys.Add(card.Id);
                            shown.Add(card.Name);
                        }
                    }
                    detail += " 里的 " + where;
                }
            }

            if (keys.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」降费找不到对象 —— **这条没生效**");
                unresolved.Add(op.Source + "（降费对象找不到）");
                return false;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                int d = delta;
                // ---- 「**设为** N 费」（`reduce its cost to 1`，2026-09-13 A4）----
                // 🔴 差值只能**在这儿**算：同一张卡在不同局面下真费用不同（别的降费也叠在上面），
                //    解析期算不了。`Amount` 那条记的是「降多少」，这条记的是「变成多少」。
                if (op.CostSetTo > 0 && i < cardRefs.Count)
                    d = op.CostSetTo - RuleCore.CostOf(ctx, owner, cardRefs[i]);
                ctx.CostMods.Add(new CostMod { Player = owner, Key = keys[i], Delta = d, ExpireTurn = expire });
            }

            string what = op.CostSetTo > 0 ? $"设为 {op.CostSetTo} 费" : $"每张 {delta} 费";
            ctx.Log($"{by}：「{op.Source}」{detail} {what}" + (expire >= 0 ? "（本回合）" : "（永久）")
                  + $"，登记 {keys.Count} 张：{string.Join("、", shown.ToArray())}");
            return true;
        }

        /// <summary>
        /// `repeat` —— **把本句之前的效果原样再来一遍**。
        ///
        /// 语义出处：`rule_core.gd:2542` → `_resolve_repeat`；变体有四种（全在实测数据里）：
        ///   · `Repeat this effect`（无条件）
        ///   · `Repeat this effect for each friendly Vehicle`（计数 —— 由 `for each` 层先剥，
        ///     所以走到这里时 `op.CountScope` 已经填好，**外面那层会替我们重复 N 遍**）
        ///   · `If any troop dies, repeat this effect`（条件 —— 由 `if` 层挂到 op.Condition 上）
        ///   · `Oath 3: Repeat this effect`（付费 —— 由 `Oath N:` 前缀挂到 op.Cost 上）
        /// ⇒ **这个 handler 只管「重放」这一件事**，条件/计数/付费都在别的层，各管各的。
        ///
        /// ⚠️ **重放的是解析产物本身**（`op.RepeatOps`），不是副本 —— `EffectOp` 本来就是只读的
        ///    （数值放大走 `Clone`），重放不会改到它。
        /// ⚠️ 深度由 `ctx.EffectChain` 兜底（和 Rally/Backlash 那条链共用），
        ///    免得 `repeat` 套 `repeat` 打转。
        /// </summary>
        /// <summary>
        /// **`Reanimate a friendly Remnant`**（Sautekh 的阵营机制，2026-09-13 第三十三轮）——
        /// 这是**最后一个没实现的动词**，补完它之后「完全解析的卡里每条动词都实现了」。
        ///
        /// **规则书 `:203`**：「残骸（Remnant）| 本部队死亡时**翻面表示残骸**；残骸受伤害或
        /// 控制者回合结束时被摧毁」。所以「Reanimate」= 把**自己那个翻面的残骸**翻回来。
        ///
        /// ⚠️ **我们简化了什么**（不许把选择写成原版做法）：我们的引擎**没有「翻面」这个棋盘状态**
        ///    （残骸在那边是一张翻面的实体牌，还占着格位）⇒ 这里**拿「本方墓地里死掉的单位」当残骸**。
        ///    差别有两处：① 原版的残骸**还在场上**（能被对手打掉），我们这里它已经进了墓地；
        ///    ② 原版一次能翻回来的位置有限，我们直接找最左空格。
        ///    要精确复刻得给棋盘加「翻面」状态 —— 和路标石是同一件事（见 `KeywordTable.Waystone`）。
        ///
        /// ⚠️ **走 `DeployFree`**（不另写一份放牌逻辑）—— 那一份已经管了找空格、发 `Deploy` 事件、
        ///    广播 `Deploy` 监听器、以及 `ResolveDeploy`（常驻效果盯某类牌）。
        ///    额外**再广播一条 `Reanimated`**：卡面 `When Reanimated, …`（4 张 Sautekh 单位）等的是它。
        /// </summary>
        static bool DoReanimate(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            bool all = op.Target == null || op.Target.Count == 0;   // `Reanimate all friendly Remnants`

            // ---- 候选 ① = **自己场上的残骸**（2026-09-13 A2 起走这条）----
            // 规则书 `:203` 的「翻面」：残骸**留在场上**（原版场上是一个 3D 体，
            // `BattleCardUI.CreateRemnantBody`），而卡面**全写** `Reanimate a friendly Remnant`
            // （那 16 张里 15 张是这个写法）⇒ 从场上翻回来才对。「随机」走 `ctx.Rng`（可复现）。
            var remSlots = new List<int>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                if (u != null && u.IsRemnant) remSlots.Add(s);
            }
            if (remSlots.Count > 0)
            {
                var order = new List<int>(remSlots);
                if (!all)
                {
                    // `a random friendly Remnant` —— 用**种子定死**的随机源（工程铁律）
                    int k = ctx.Rng.Next(order.Count);
                    int pick = order[k];
                    order.Clear();
                    order.Add(pick);
                }
                int got = 0;
                foreach (int s in order)
                {
                    var rem = ps.Board[s];
                    if (rem == null || !rem.IsRemnant) continue;
                    // 翻回来 = 那个格位上换成一个**活着的、全须全尾的**单位
                    int slot = s;
                    ps.Board[slot] = new UnitState(rem.Card, false);
                    ctx.Log($"{by}：「{op.Source}」把 {rem.Name} 从**残骸**翻回来（槽 {slot}）");
                    // `When Reanimated, …` —— 和 `DeployFree` 那条路发同一种广播
                    // （⚠️ 那个重载是 `BroadcastWhen(ctx, kind, owner, card, unit)`：卡 + 场上单位）
                    BroadcastWhen(ctx, WhenEventKind.Reanimated, owner, rem.Card, ps.Board[slot]);
                    got++;
                }
                if (got > 0) return true;
            }

            // ---- 候选 ② = 墓地（**旧口径的退路**）----
            // ⚠️ 为什么还留着：不是每个「复活」效果都写 `Remnant`，而且旧行为有几张卡在用。
            //    场上没有残骸时**退回墓地**，但**日志会说清**（不静默换了来源）。
            var picks = new List<CardDef>();
            for (int i = ctx.DeadUnits.Count - 1; i >= 0; i--)
            {
                var d = ctx.DeadUnits[i];
                if (d.Owner != owner || d.Card == null || !d.Card.IsUnit) continue;
                if (picks.Contains(d.Card)) continue;              // 同一张卡只翻一次
                picks.Add(d.Card);
                if (!all) break;
            }
            if (picks.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要翻残骸，但场上没有残骸、墓地里也没有单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（没有可翻的残骸）");
                return false;
            }
            if (remSlots.Count == 0)
                ctx.Log($"{by}：「{op.Source}」场上没有残骸，**退回墓地**翻（旧口径）");

            int done = 0;
            foreach (var card in picks)
            {
                int slot;
                if (!DeployFree(ctx, owner, card, out slot)) break;   // 满场 → 后面的也放不下，停
                ctx.TakeFromGraveyard(owner, card);                   // 翻回来了，就从墓地/弃牌堆里拿走
                ctx.Log($"{by}：「{op.Source}」把 {card.Name} 从墓地翻回来（槽 {slot}）");
                BroadcastWhen(ctx, WhenEventKind.Reanimated, owner, card, ps.Board[slot]);
                done++;
            }
            return done > 0;
        }

        static bool DoRepeat(BattleContext ctx, int owner, string by, EffectOp op,
                             UnitState chosen, List<string> unresolved)
        {
            if (op.RepeatOps == null || op.RepeatOps.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要重复，但前面没有可重复的效果 —— **这条没生效**");
                unresolved.Add(op.Source + "（repeat 前面没有效果）");
                return false;
            }
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"{by}：「{op.Source}」连锁太深，不再重复");
                unresolved.Add(op.Source + "（连锁深度到顶）");
                return false;
            }

            ctx.Log($"{by}：「{op.Source}」把前面 {op.RepeatOps.Count} 条效果重复一遍");
            ctx.EffectChain++;
            int done = 0;
            try
            {
                foreach (var o in op.RepeatOps)
                    if (ResolveOne(ctx, owner, null, by, o, chosen, unresolved)) done++;
            }
            finally { ctx.EffectChain--; }
            return done > 0;
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
