// EffectResolver.cs — **把解析出来的效果真正打出去**（`EffectText` 的结算侧）
//
// 分工：
//   `EffectText`   把卡面文本 → `EffectOp` 列表（只解析，不动状态）
//   `GivePayload`  把 `give X to Y` 的 X → 属性增减益 / 关键词授予
//   **本文件**      把 `EffectOp` 落到 `BattleContext` 上（改状态、发事件、写日志）
//
// ⚠️ **2026-10-17 就地订正（铁律 5）**：这一行原来写的是
//   「**权威语义来源**：`d:/warpforge/scripts/rule_core.gd`」—— **那是错的**：
//   那份 `.gd` 是**我们自己的上一版 Godot 复刻**（70 个 `.gd`），**不是原版**，
//   **不能当原版语义判据**（出处 = `CLAUDE.md` 铁律 2 的 2026-09-18 更正）。
//   ✅ **权威 = 原版全量反编译** `d:/2/tools/decomp_full/`（26,282 个方法体）。
//   ⚠️ 本文件下面凡引 `rule_core.gd:<行>` 的，一律读作「**我们上一版当时怎么写的**
//   （旁证、非权威）」，别当原版结论。
// 它当时照着抄的目标（这几条仍然是**我们这套实现的来源**，不是判据）：
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
        /// **`instead` 的配对与裁决** —— 返回「哪几条 op **不结算**」（`skip[]`）。
        ///
        /// 🔴 **两个 `ResolveOps` 重载共用这一份**（`A1685`，2026-10-22）：
        ///   · `pairConditional: true`  = **出牌 / 战术卡那条路**（那个重载有 `chosen`，
        ///     它就是「这一下要打的那个目标」⇒ 条件判得准）；
        ///   · `pairConditional: false` = **能力层那条路**（`IReadOnlyList` 重载：触发 /
        ///     灵魂石 / 誓约 / 信仰 / 常驻 / 手牌陷阱 / 突触邻居 / 死亡结转…）。
        ///   ⛔ **别把规则写第二份** —— 本工程红线：两处写同一条规则 = 迟早不一致。
        ///
        /// ## 配对（结构）
        /// 🔴 **2026-10-23（`A12` · 铁律 5 就地订正）**：判据**只此一处** =
        ///   <see cref="EffectOp.FindInsteadBaseIndex"/>。本行原来写「四条逐字相同：…③ 动词要相同」——
        ///   **③ 已经不对了**：`SW53` / `TAU58` 的真卡写法是 `give … destroy it **instead**`（跨动词），
        ///   旧判据因此配不上 ⇒ 条件成立时两条都结算。现口径 = ① 碰到另一条 `Instead` 就停
        ///   ② 跳过带 `Condition` 的 ③ **不再要求同动词** ④ 不跨过另一条替换；
        ///   **同一句 `instead` 解出的多条 op 算一个段**（`segEnd`），整段一起跳/一起留。
        /// （`EffectText.cs` 里那趟「被替换那条的目标复制给 `instead` 那条」用的是**同一份判据**。）
        ///
        /// ## 裁决（谁跳谁）
        ///   · 条件成立   → 跳过**被替换的**那条（`skip[baseIdx]`），只结算这一条；
        ///   · 条件不成立 → 跳过**整个替换段**（`[i..segEnd]`），被替换的那条照常结算。
        /// ⚠️ 条件判不了时不在这儿做决定，交给 `ResolveOne` 去走它那条
        ///    「条件判不了 → 整条不生效 + 如实报出来」的路（红线：不许静默失败）。
        ///
        /// ## 🔴 `A12`（2026-10-23）：**条件只判一次** —— `Judged[]` 是干嘛的
        /// `A1` 那一轮只把「跳谁」收进本函数、没回头查主循环那道闸，留下一个**判两次**的隐患：
        /// `ResolveOneCore` 的 `if (!string.IsNullOrEmpty(op.Condition)) ConditionHolds(...)`
        /// 会**再判一遍**，而那时**前面的 op 已经结算过了**、`ctx.LastTarget` 也换了。
        /// 这对 `instead` 是错的：它的条件（`it already had Hunt Mark` / `it has Flying`）
        /// 是**动手之前**的前置状态（原版在 `AbilityLogic` 取目标那一刻判，`PlayAbility` 之前）。
        /// ⇒ 本函数判过的那一段（`judgeable && !usesForEach`）记进 `Judged[]`，
        /// 主循环据此**用 `WithoutCondition()` 递一份去了条件的副本**，不再判第二次。
        /// ⚠️ 让位的那两档（判不了 / 要数 `for each`）**不进 `Judged`** —— 主循环照旧判。
        ///
        /// ⚠️ **本条要数 `for each` 时，整个替换机制让位**。
        ///    数 `… for each Dark Pact on it` 要读 `it` 的目标，而那时 `ctx.LastTarget`
        ///    可能还是上一张卡留下的（这类卡的「it」就是本次的目标，不是上一条效果的产物）。
        ///    与其按一个过时的值判错，不如放弃替换：两条各自走正规流程。
        ///    **代价是数值会叠加，如实写在这儿，不装作它是原版语义。**
        ///
        /// ## 🔴 `A1388`（2026-10-10）：无条件的 `instead`【一定】替换
        /// **改前怎么错的**：对**每一条** `instead` 都调 `ConditionHolds`，而它读的是
        ///   `op.ConditionKind`、`default:` 返 **`false` = 判不了**。
        ///   **没有条件**的 `instead` ⇒ `ConditionKind` 是空串 ⇒ 落 `default` ⇒ `!judgeable`
        ///   ⇒ **两条都照常走**。实测（`SOR6 Righteous Repugnance` 卡面原文）：`Give +1 [Attack]
        ///   and +1 [Ranged] to your units this turn. 4 Energy: Give +2 [Attack] and +2 [Ranged]
        ///   instead and Heal them 1` ⇒ 我方单位近战 **1 → 4**（**两段都加了**），
        ///   而卡面那个 `instead` 本该**替换掉第一段**（正解 **1 → 3**）。
        /// **判据（为什么「没有条件」= 恒成立）**：`instead` 的配对语义是「这句话替换掉前一句」，
        ///   而**只有条件句**（`If …, … instead`）才谈得上「条件成立不成立」—— 那份条件由
        ///   `TryIf` / `TryIfNoComma` 剥下来写在 `op.Condition` / `op.ConditionKind` 上。
        ///   **条件字段为空 = 这句本来就没有条件可判**，不是「判不了」。同一条口径
        ///   `ResolveOneCore` 自己也用着（它那道条件闸是 `if (!string.IsNullOrEmpty(op.Condition))`
        ///   —— 空的时候**根本不进**）。⛔ 别改回去。
        ///
        /// ## 🔴 `A1685`（2026-10-22）：能力层那条路原来**一条都不消费** `Instead`
        /// **病灶**：配对+裁决**内联在出牌那个重载里**，能力层那个重载一个字没有 ⇒
        ///   同样的卡面在两条路上**行为不一致**（能力层「两条都跑」）。**离线实测**
        ///   （`D:/tmp/wf_w2` 探针，合成卡 `Rally: Deal 3 damage to an enemy. Deal 6 damage
        ///   instead`）：出牌路 = **6** 伤（对）· 能力路 = **9** 伤（`3 + 6` 都结算了，错）。
        /// **本笔**：规则收进本函数、两条路共用 ⇒ 能力层也消费 `instead`。
        ///
        /// ⚠️ **能力层**（`pairConditional: false`）**也配有条件的那一族**（2026-10-18 `T02` 起）——
        ///    判据不是偷懒，而是「先解被替换那条的目标、再判条件」：
        ///   · 能力层**没有 `chosen`** ⇒ 直接 `ConditionHolds` 会退回 `ctx.LastTargets` / `ctx.LastTarget`，
        ///     而 `ResolveOps` 入口刚把那个槽种成 **`prime` = 这条能力的施放者** ⇒
        ///     结算前判「it has Flying / it has Armour」**判的是施放者自己**（`ASH31 Crimson Hunter` 实测：
        ///     施放者带 `Flying`、目标不带 ⇒ 直接判会恒成立 ⇒ 打非飞行目标也从 3 变 6）。
        ///   · 正解 = **先用 `ResolveTargets` 解出被替换那条的目标**（只算目标、不结算），把它临时当
        ///     referent 填进 `ctx.LastTargets`，`ConditionHolds` 用**它**判；判完把槽原样还原。
        ///     ⇒ 目标带 `Flying` 时只结算替代那一条（6），不带时只结算基数（3），**不再 3+6**。
        ///   · 条件判不了 / 要数 `for each` 时**仍让位**（两条各走正规流程，如实记）。
        /// </summary>
        /// <summary>`InsteadSkips` 的产物：**哪几条不结算**（`Skip`）＋ **哪几条的条件已经判过**
        /// （`Judged`，主循环据此不再判第二次 —— 理由见 `InsteadSkips` 的 doc「条件只判一次」）。
        /// 两个数组都以 `ops` 的下标为下标。</summary>
        sealed class InsteadPlan
        {
            public bool[] Skip;
            public bool[] Judged;
        }

        static InsteadPlan InsteadSkips(BattleContext ctx, int owner, IReadOnlyList<EffectOp> ops,
                                   UnitState chosen, bool pairConditional, UnitState source = null,
                                   // 🆕 2026-10-22（`W64` · `B-16`）：这条 ability 所属的卡（喂结构化条件）。
                                   //   `null` = 不知道 ⇒ 条件退回旧正文口径（行为不变）。
                                   CardDef card = null)
        {
            var plan = new InsteadPlan { Skip = new bool[ops.Count], Judged = new bool[ops.Count] };
            var skip = plan.Skip;

            // ⚠️ 这里**只配对、不决定跳谁** —— 决定放到下面那趟做。
            //    因为「跳谁」取决于条件，而条件可能要数 `for each`、要用**这次选好的目标**
            //    （`chosen` 在这一步才拿得到 / 能力层根本没有），在这里判会读到上一张卡留下的
            //    `ctx.LastTarget`。
            // ---- `instead`：成对决定谁跳谁（**必须在主循环之前**）----
            // 因为跳的可能是**前面**那条（`baseIdx < i`），主循环走到 `i` 时它早就结算完了。
            for (int i = 0; i < ops.Count; i++)
            {
                if (!ops[i].Instead) continue;
                // 🔴 **2026-10-23（`A1688` 残余）：配对判据**只此一处** —— `EffectOp.FindInsteadBaseIndex`
                //   （解析层「目标承接」那一趟也调它）。本批就地改了那一条判据的两处：
                //   ① 同一句 `instead` 里的多条 op 算**一个段**（只有段首配对、整段一起跳/一起留）；
                //   ② **不再要求同动词**（`SW53` / `TAU58` 是 `give … destroy it instead`）。
                //   判据与真卡出处 → `EffectOp.FindInsteadBaseIndex` 的 doc。
                int segEnd;
                int baseIdx = EffectOp.FindInsteadBaseIndex(ops, i, out segEnd);
                if (baseIdx < 0) continue;
                bool holds, judgeable;
                if (string.IsNullOrEmpty(ops[i].Condition))
                {
                    // 无条件的 `instead`（`A1388` 修的那一支）—— 没有条件可判 ⇒ 恒成立
                    judgeable = true; holds = true;
                }
                else if (pairConditional)
                {
                    // 出牌 / 战术卡那条路：`chosen` 是玩家选定的目标，条件在结算前就判得准。
                    judgeable = ConditionHolds(ctx, owner, ops[i], chosen, out holds, card);
                }
                else
                {
                    // 🔴 **2026-10-18（T02）· 能力层**：没有 `chosen`，且 `ctx.LastTargets` 此刻还是
                    //   入口种下的**施放者**（`prime`）⇒ 直接判会「判施放者自己」（`ASH31` 那个陷阱）。
                    //   正解 = **先把被替换那条的目标解出来**（`ResolveTargets`，只算目标、不结算），
                    //   用它当 referent 判条件；判完把槽原样还原。
                    var bt = ResolveTargets(ctx, owner, ops[baseIdx].Target, source, null,
                                            ops[baseIdx].TargetCondition);
                    var savedTargets = new List<UnitState>(ctx.LastTargets);
                    var savedLast = ctx.LastTarget;
                    ctx.LastTargets.Clear();
                    ctx.LastTargets.AddRange(bt);
                    ctx.LastTarget = bt.Count > 0 ? bt[0] : null;
                    judgeable = ConditionHolds(ctx, owner, ops[i],
                                               bt.Count == 1 ? bt[0] : null, out holds, card);
                    ctx.LastTargets.Clear();
                    ctx.LastTargets.AddRange(savedTargets);
                    ctx.LastTarget = savedLast;
                }
                // `for each` 那一档：**段内任何一条**要数，都不能折叠（两条各走正规流程）。
                bool usesForEach = false;
                for (int k = i; k <= segEnd; k++)
                    if ((ops[k].PerCount > 0 && !string.IsNullOrEmpty(ops[k].CountRef))
                        || !string.IsNullOrEmpty(ops[k].CountScope)) usesForEach = true;
                if (!judgeable || usesForEach) continue;      // 两条都照常走（条件留给主循环判）
                if (holds) skip[baseIdx] = true;              // 替换成立：原来那条不结算
                else
                {
                    // 🔴 **2026-10-23（`A1688` 残余）**：条件**不成立**时，**整段替换**都不结算
                    //   （不是只跳段首那一条）—— 否则 `DA33` 的 `gain 1 Quest Point` 会白拿。
                    for (int k = i; k <= segEnd; k++) skip[k] = true;
                }
                // 🆕 `A12`：这一段的条件**已经判过**（在上面，用的是「动手之前」的状态与
                //   被替换那条的目标）⇒ 主循环不许再判第二次（见 doc「条件只判一次」）。
                for (int k = i; k <= segEnd; k++) plan.Judged[k] = true;
            }
            return plan;
        }

        /// <summary>`instead` 的条件已经由 <see cref="InsteadSkips"/> 判过 ⇒ 递一份**去掉条件**的副本
        /// 给 `ResolveOne`，让它那道 `ConditionHolds` 闸不再判第二次。
        /// <para>为什么要去条件而不是「传一个开关形参」：`ResolveOne` / `ResolveOneCore` 有十几个调用点，
        /// 加形参要一处处传；而去条件的副本与 `Prepaid` / `AmountRef` 那两处「递副本」是同一个套路，
        /// 且 `EffectOp.Clone()` 是 `MemberwiseClone`、⛔ 不会写回解析产物（同一张卡第二次打出时是同一份对象）。</para></summary>
        static EffectOp WithoutCondition(EffectOp op)
        {
            if (op == null) return null;
            if (string.IsNullOrEmpty(op.Condition) && string.IsNullOrEmpty(op.ConditionKind)) return op;
            var c = op.Clone();
            c.Condition = "";
            c.ConditionKind = "";
            return c;
        }

        /// <summary>
        /// 🆕 v29（2026-10-25 · 执行代理 `X6` · **口径乙**）：**ability 级 `targetCriteria` 的逐 op 家族闸**
        /// —— 把「23 张卡的 32 行 criteria」真正接上（判据 → `资料/待办判据_引擎_1025b.md` §一）。
        ///
        /// <para>**做什么**：对每条 op，`op.Verb` → 能力种类族名（`CardDef.FamiliesOfVerb`，
        /// 唯一词表 = `CardDef.ActiveAbilityFamilyHasVerb`）→ 按族名认那一行
        /// `abilityLevelCriteria`（`CardDef.AbilityLevelCriteriaForFamily`）→ 拿**玩家点选的目标**
        /// 过 `TargetCriteriaEval.Matches` ⇒ **明确不满足**就 `skip[i] = true`
        /// （= 这条 op 不结算；钱照收 —— 原版「一条 ability 激活了」与「它哪条 logic 真做了事」是两件事，
        /// 判据 → `ChargePaidGroups` 那段注释）。</para>
        ///
        /// <para>🔴 **窄闸（任一不过 ⇒ 整段不启用，行为逐字不变）**：</para>
        /// <list type="number">
        /// <item>调用点给了 <paramref name="card"/> 与 <paramref name="triggerName"/>；
        ///   今天只有战术 / 防御卡正文体传（`PlayTacticBodyDeferred`）。</item>
        /// <item>卡表的 `abilityLevelCriteria` 来自 v26+（否则那行根本不存在）。</item>
        /// <item>**按 trigger 定位不唯一**（`card.UniqueAbilityIndexForTrigger(triggerName)` 回 `-1` 且
        ///   `ambiguous`）。唯一定位得到的卡**不启用** —— 它们走既有的
        ///   `ActionPump.EnqueueAbility` 那条路（`E2`/`E9` 接的），两处都判会是**双重闸**。</item>
        /// </list>
        ///
        /// <para>**三档处置**（同 `RuleCore.AbilityFamilyTargetAllows`，⛔ 别合并）：</para>
        /// <list type="bullet">
        /// <item>`true` / 「这一族没有条件」⇒ 不动；</item>
        /// <item>`false` ⇒ `skip[i] = true` + `ctx.Log` 报出「哪张卡 / 哪条 op / 哪一族 / 为什么」；</item>
        /// <item>`null`（判不了）⇒ **放行**（那个函数自己已经出声一次并计数）。</item>
        /// </list>
        /// <para>⚠️ 一条 op 的动词被 **≥2 族**同时认（实测只有 `choosecard`）⇒ 按**判不了**处置
        /// （传空族名给那个函数，它会出声）—— ⛔ 不许「挑第一族」。</para>
        /// </summary>
        static void ApplyAbilityFamilyGate(BattleContext ctx, int owner, UnitState thisCard,
                                           CardDef card, string triggerName,
                                           List<EffectOp> ops, UnitState chosen, bool[] skip, string by)
        {
            if (ctx == null || card == null || string.IsNullOrEmpty(triggerName)
                || ops == null || skip == null) return;
            // 闸门③：只在「按 trigger 定位不唯一」的卡上启用（其余卡走既有那条路）。
            string uwhy;
            bool uamb;
            int uab = card.UniqueAbilityIndexForTrigger(triggerName, out uwhy, out uamb);
            if (uab >= 0 || !uamb) return;

            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                if (op == null || string.IsNullOrEmpty(op.Verb)) continue;
                string[] fams = CardDef.FamiliesOfVerb(op.Verb);
                if (fams.Length == 0) continue;                       // 这个动词不属于任何能力种类 ⇒ 不判
                string fam = fams.Length == 1 ? fams[0] : "";          // ≥2 族 ⇒ 判不了（⛔ 不挑第一族）
                string why;
                bool amb;
                bool? ok = RuleCore.AbilityFamilyTargetAllows(ctx, owner, thisCard, card, triggerName,
                                                              fam, chosen, out why, out amb);
                if (ok == false)
                {
                    skip[i] = true;
                    RuleCore.NoteAbilityFamilyGateSkipForTest();
                    ctx.Log($"{by}：「{op.Source}」这一条按能力种类族（`{fam}`）的 ability 级 "
                          + "`targetCriteria` **不满足** ⇒ **不结算这一条**"
                          + "（原版逐条 ability 各判各结算 ⇒ 那一族的 logic 不跑；钱照收）");
                }
            }
        }

        /// <summary>
        /// `EffectOp.AmountRef` → 具体数值。**判据只此一处**（铁律 6）：
        /// 出牌路（<see cref="ResolveOps(BattleContext,int,UnitState,List{EffectOp},UnitState,out List{string},CardDef,bool[])"/>）
        /// 与**触发路**（<see cref="ResolveOps(BattleContext,int,UnitState,IReadOnlyList{EffectOp},string,UnitState)"/>）
        /// 都调它。
        ///
        /// 现认三种来源（解析侧 `EffectText` 的两条正则）：
        ///   · `faith` —— `… equal to your Faith`（`Emperor's Judgement` / `Amalia Novena`）
        ///   · `spirit` —— `… equal to your Spirit Stones`
        ///   · `enemyhand` —— `… equal to the number of cards in the enemy hand`（`GSC35 Benefictus`）
        ///
        /// 🔴 **2026-10-19（`A2001` / E9）把这条从出牌路抽出来**：改前这半条**只写在出牌那条路上**，
        ///    而触发路（所有 Rally/Strike/… 的正文都走它）**没有** ⇒ 触发正文里的 `equal to …`
        ///    一律按 `Amount = 0` 结算。实测 `ruleprobe play GSC35` 的读数 = 「对 1 个目标造成 **0** 点」
        ///    （敌方手里明明有 3 张）。同族的 `Amalia Novena`（`equal to your Faith`）也一样是 0。
        ///
        /// 返回 `false` = 这个来源本版不认识（已写日志与 `unresolved`，调用方跳过这一条）。
        /// 没有 `AmountRef` 时 `resolved` 原样返回**同一个对象**（调用方用 `ReferenceEquals` 判「换没换」）。
        /// ⚠️ **必须递副本**下去，不能改 `op.Amount` 本身 —— `EffectOp` 列表是解析产物、会被复用。
        /// ⚠️ `n == 0` 是合法结果（`Deal 0 damage` 是「生效了但数值是 0」，不是「没生效」）。
        /// </summary>
        static bool TryApplyAmountRef(BattleContext ctx, int owner, string by, EffectOp op,
                                      List<string> unresolved, out EffectOp resolved)
        {
            resolved = op;
            if (string.IsNullOrEmpty(op.AmountRef)) return true;

            var ps = ctx.Players[owner];
            int n = op.AmountRef == "faith" ? ps.Faith
                  : op.AmountRef == "spirit" ? ps.SpiritStones
                  // 「enemy」= **施放者的对手**（`owner` 的对家）——
                  // 判据在 `EffectText.ReEqualEnemyHandCount` 的 doc（别放宽成任一手牌）。
                  : op.AmountRef == "enemyhand" ? ctx.Players[1 - owner].Hand.Count
                  : -1;
            if (n < 0)
            {
                ctx.Log($"{by}：「{op.Source}」的数值要取「{op.AmountRef}」，本版不认识这个来源 "
                      + "—— **这条没生效**");
                unresolved.Add(op.Source + "（数值来源认不出：" + op.AmountRef + "）");
                return false;
            }
            var refOp = op.Clone();
            refOp.Amount = n;
            // `give` / `gain` / `lose` 的数值写在**载荷原文**里（同 for-each 那条注释）
            if (n != 0 && (op.Verb == "give" || op.Verb == "gain" || op.Verb == "lose"))
                refOp.Payload = ScalePayload(op.Payload, n);
            ctx.Log($"{by}：「{op.Source}」的数值 = "
                  + (op.AmountRef == "faith" ? "你的信仰"
                     : op.AmountRef == "spirit" ? "你的灵魂石"
                     : "对手手牌张数") + $" {n}");
            resolved = refOp;
            return true;
        }

        /// <summary>
        /// 结算一组效果（一张战术卡 / 一次触发 / 一个主动技能的效果部分）。
        /// </summary>
        /// <param name="chosen">表现层已选定的目标（需要选目标的战术卡才有）</param>
        /// <param name="paidOut">🆕 2026-10-19（`W57FIX2` · P1）：可选。给了就按**本条 <paramref name="ops"/>
        /// 的下标**回填「这一条所属的付费组收到钱没有」（`ChargePaidGroups` 的 `paidOk`）。
        /// 唯一读点 = `PlayTactic` 判「战术卡自己的 `UseFaith` / `UseSpiritStone` 那一拍要不要发」；
        /// ⛔ 别拿它代替返回值（那是「整条 ability 激活了没有」的另一个口径）。</param>
        /// <returns>有多少条 op **完整结算**了（没完整结算的会写进 <paramref name="unresolved"/>）</returns>
        public static int ResolveOps(BattleContext ctx, int owner, UnitState source,
                                     List<EffectOp> ops, UnitState chosen,
                                     out List<string> unresolved,
                                     CardDef sourceCard = null,
                                     bool[] paidOut = null,
                                     // 🆕 v29（2026-10-25 · 执行代理 `X6` · 口径乙）：**出牌正文的逐 op 家族闸**。
                                     //   两个都非空才启用 —— 只由战术 / 防御卡的正文体传（见 `PlayTacticBodyDeferred`）。
                                     //   判据 / 三态 / 为什么只在这一条路 → `RuleCore.AbilityFamilyTargetAllows` 的 doc。
                                     CardDef abilityFamilyCard = null,
                                     string abilityFamilyTrigger = null)
        {
            unresolved = new List<string>();
            if (ops == null) return 0;
            string by = source != null ? source.Name : "战术卡";

            // **这次结算的是哪张卡** —— 只有「常驻效果登记」需要它。
            // 🔴 **2026-10-19（`A1955`）：判据换成原版**（原文留痕 —— 原来这里写的是「规则书英文版
            // `:39-41` 要求把写『For the rest of this battle』的卡**单独放一摞备查**」；
            // 那份是**粉丝实体版**、自己第 5 行写着 `Not official. Fan project.` ⇒ ⛔ 不当判据）。
            // **原版怎么表达「本场战斗有效」**（判据 = `d:/2/tools/decomp_full/`）：
            //   · 没有 `PersistentEffect` 这个类型（全量 `dump.cs` 0 命中）；
            //   · 「本场」靠**不带任何时长旗标**表达（`CardEffect` 只有回合级 5 个旗标 + `permanentBuff`）；
            //   · 真正的载体 = **一张活着的牌**（附魔 `CardScript__AddEffect.c:826-828` →
            //     `CardScript__AddEnchantment.c:25,37,43` `activeEnchantments(+0x110)`；
            //     或 `replaceAbility(40)` → `CardScript__AddEffect.c:242-251`），派发触发时
            //     `RawCardScript__TriggerAbilities.c:50-88` 把附魔里的能力一起算进来。
            //   ⇒ 所以**来源卡**仍要记（我们这摞是**自设计**的替代结构，日志/排查靠它），
            //     但「单独放一摞」是**实体台面**的备忘办法，数字版没有对应物。
            //   完整判据块 → 本文件 `DoPersist` 上方那段注释（`A1950` 已逐条换过）。
            ctx.PlayingCard = sourceCard;

            // 一张卡的结算 = **一个计数窗口**：`For each troop drawn …` 数的是这张卡自己抽到的牌
            // （见 `BattleContext.DrawnThisResolve`）。嵌套结算（Rally 触发里再打一张牌）会各开各的窗口。
            ctx.DrawnThisResolve.Clear();

            // 🆕 **2026-10-18（`A888`）：手牌那一侧的代词槽也跟着清** ——
            //    口径与上一句**同一条**（「一条能力一个窗口」，原版 `AbilityLogic.SetPreviousAbilityTargets`
            //    先 `Clear()` 再 `AddRange()`）。写点只有 `DoDraw` 与 `AttachEffectToHandInstances` 两处，
            //    都在 `ResolveOps` 之内 ⇒ 不清就会**漏进下一张卡**。
            //    🔴 不清的后果是**静默错打**：`GOF_Da_Red_Waaagh` / `UM23` 那两张单数 `it`
            //    （`Draw a troop and give it +1`）自己**不写槽**、全靠兜底 ——
            //    槽里留着上一张卡的残值就会把它们顶掉，而且卡面照旧不打 `*`。
            ctx.LastHandTargets.Clear();
            ctx.LastHandTarget = null;

            // 🔴 **2026-10-17（B7「卡实例身份 —— `SetupCardInHand` 那一套」）：代词槽也要按「一条能力」清零。**
            //
            //     **原版判据**：`AbilityLogic.SetPreviousAbilityTargets`（`d:/2/tools/decomp_full/`
            //     `AbilityLogic__SetPreviousAbilityTargets.c:19-28`）对每一条能力**先 `Clear()` 再
            //     `AddRange()`** —— 也就是 `it` / `them` 指的永远是**上一条能力**的目标，
            //     **上一条能力没有目标就是空**。（它在 `AbilityLogic__PlayAbility.c:2745` 调，
            //     即「这条能力结算完就把槽覆盖掉」。）原版**没有**「槽里还留着上上一次的」这回事。
            //
            //     **我们这边为什么漏**：`LastTargets` 只由 `ResolveTargets` 写，而
            //     「抽牌 / 定向翻找 / 造牌 / 抽上手」这几条 op **一条都不写它** ⇒
            //     上一张卡（或上一次结算）留下的值会**漏进这一张卡**。
            //     实测形状：上一张战术打过某个单位之后，`TAU54 Dynamic Offensive` 的
            //     `If they are Battlesuits, give them Flank`（判据在 `ConditionHolds` 的
            //     `targethaskw` 支、投递在 `DoGive` 的 `prev` 支）会拿**上一张卡打过的那个单位**去判 ——
            //     而且是**静默**错判（卡面不打 `*`）。
            //     ⚠️ 只清 `LastTargets`（**一批**），**不动 `LastTarget`**（**一个**）——
            //        后者由 `chosen`（上面那段）/ `ResolveOps(…, seed)` 显式种，是刻意的。
            ctx.LastTargets.Clear();
            // 🆕 2026-10-24（执行代理 `E2` · `E8` 顺手发现 2）：**本条 ability 的目标并集也从空起手**
            //   —— 原版 `abilityTargetsToSave(+0x80)` 每次 play 都是新的（`AbilityLogic__DeepCopy.c`
            //   不拷贝那一格）。⛔ 别跨 ability 累积：本槽语义是「**上一条**能力 + 本条已解出的」。
            ctx.AbilityTargetsSaved.Clear();

            // 「上一次选牌挑中的那张」也按**一张卡一个窗口**清 —— 同一张卡里的分句要能互相指代
            // （`Choose a troop from your deck.` + `Create a copy of it in your hand` 是**两条分句**），
            // 但**不能**漏到下一张卡去：不然一张卡里的 `create a copy of it` 会复制到
            // 上一张卡选中的那张牌（2026-09-13 加选牌 handler 时定的口径）。
            ctx.LastChosenCard = null;

            // 🔴 **2026-10-18（`A976`）：`LastCreated` 也要按【一张卡一个窗口】清。**
            //
            //    **原来为什么漏**：`LastCreated` 那 5 个 `Clear()` 点**全在别的 writer 的入口**
            //    （`DoCreate` · `DoChooseCard` · `DoDrawRef` · `DoReturn` 的两条支），
            //    **唯独 `ResolveOps` 入口没有** ⇒ 上一张卡「造/挑/回手」出来的那几份**跨卡存活**。
            //
            //    **真差异（离线实测，真卡真卡池）**：先打一张「**在手里印一张
            //    `Reanimation Protocol`**」，再打一张「抽一张牌、它少 1 费」——
            //    被降费的是**上一张卡印出来的那张**，不是刚抽到的那张。
            //    根因就在 `DoLowerCost` 的「指代上一张」支：
            //    `ctx.LastCreated.Count > 0 ? LastCreated : DrawnThisResolve`
            //    —— `LastCreated` **优先**、空了才退到抽牌那一栏，而它带着上一张卡的陈旧值。
            //    ⚠️ 而且这是**静默错打**：卡面不打 `*`、日志里两张卡名都在，只有逐张比才看得出。
            //
            //    **口径**：与 `DrawnThisResolve` / `LastHandTarget(s)` / `LastTargets` /
            //    `LastChosenCard` **同一个窗口** = 「一条能力一个窗口」
            //    （原版判据 `AbilityLogic.SetPreviousAbilityTargets`：每一条能力先 `Clear()` 再
            //      `AddRange()`，见上面那段注释 ⇒ **没有「槽里还留着上一张卡的」这回事**）。
            //    ✅ **同一条 resolve 内「op1 写 / op2 读」不受影响**：
            //    `Create three … in your hand. They cost 1 less` 两条 op 在同一次 `ResolveOps` 里
            //    ⇒ 清在入口、写在 `DoCreate`、读在 `DoLowerCost`，一路照旧
            //    （自检两处一号验收靶：`Master of Manoeuvre` 的 `It costs 4 less` 与 `T_Return`）。
            //    ⛔ 别把它挪到「用完之后清」—— 那样同一条卡里的第二个分句就读不到了。
            ctx.LastCreated.Clear();

            // 表现层**已经替这张卡选好了目标**时，先把它记成「当前目标」——
            // 卡面上那些 `for each Dark Pact **on it**` 的 `it` 指的正是这张卡自己的目标，
            // 而这条效果前面**没有任何「上一条效果」**去设置 `ctx.LastTarget`。
            // 不种下去就会读到**上一张卡**留下的值（2026-09-12 撞到：同一张卡第二次打出反而少算）。
            if (chosen != null) ctx.LastTarget = chosen;

            // ⚠️ 这里**只配对、不决定跳谁** —— 决定放到主循环里做。
            //    因为「跳谁」取决于条件，而条件可能要数 `for each`、要用**这次选好的目标**
            //    （`chosen` 在这一步才拿得到），在这里判会读到上一张卡留下的 `ctx.LastTarget`。
            var plan = InsteadSkips(ctx, owner, ops, chosen, pairConditional: true, card: sourceCard);
            var skip = plan.Skip;

            // 🆕 v29（2026-10-25 · 执行代理 `X6` · **口径乙**）：**ability 级 `targetCriteria` 的逐 op 家族闸**。
            //   ⚠️ 必须排在 `InsteadSkips` **之后** —— 配对（`EffectOp.FindInsteadBaseIndex`）要在
            //   **完整 op 表**上算；我们只是在它的 `Skip` 数组上再 OR 一批（⛔ 别反过来先删 op，
            //   那会让 `instead` 配不上对，症状是「两条都跑」或「两条都不跑」）。
            //   ⚠️ 只在「按 trigger 定位不唯一」的卡上启用（那 23 张）⇒ 其余卡行为逐字不变。
            ApplyAbilityFamilyGate(ctx, owner, source, card: abilityFamilyCard,
                                   triggerName: abilityFamilyTrigger, ops, chosen, skip, by);

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
            //
            // 🔴 **2026-10-10（第十四会话 · `A1388`）：无条件的 `instead`【一定】替换。**
            //
            // **改前怎么错的**：这里对**每一条** `instead` 都调 `ConditionHolds`，而它读的是
            //   `op.ConditionKind`、`default:` 返 **`false` = 判不了**（`EffectResolver.cs` 的
            //   `ConditionHolds` 末尾那一条）。**没有条件**的 `instead` ⇒ `ConditionKind` 是空串
            //   ⇒ 落 `default` ⇒ `!judgeable` ⇒ 上面那句 `continue` ⇒ **两条都照常走**。
            //   实测（`SOR6 Righteous Repugnance` 卡面原文）：`Give +1 [Attack] and +1 [Ranged]
            //   to your units this turn. 4 Energy: Give +2 [Attack] and +2 [Ranged] instead
            //   and Heal them 1` ⇒ 我方单位近战 **1 → 4**（**第一段 +1 与付费段 +2 都加了**），
            //   而卡面那个 `instead` 本该**替换掉第一段**（正解 **1 → 3**）。
            //
            // **判据（为什么「没有条件」= 恒成立）**：`instead` 的配对语义是「这句话替换掉前一句」，
            //   而**只有条件句**（`If …, … instead`）才谈得上「条件成立不成立」——
            //   那份条件由 `TryIf` / `TryIfNoComma` 剥下来写在 `op.Condition` / `op.ConditionKind` 上
            //   （`EffectText.cs`）。**条件字段为空 = 这句本来就没有条件可判**，
            //   不是「判不了」。同一条口径 `ResolveOneCore` 自己也用着：
            //   它那道条件闸是 `if (!string.IsNullOrEmpty(op.Condition))` —— 空的时候**根本不进**。
            //   ⇒ 这里补上同一句：**空条件 ⇒ `judgeable = true, holds = true`**。
            //   ⚠️ 有条件的 `instead` 一字不变（仍走 `ConditionHolds`、判不了仍「两条都跑 + 报出来」）。
            //
            // ✅ **2026-10-12（`A1412`）：洞 3 的接口**就是「钱在下面那一段**动手之前**就按组收掉了」
            //    —— 被跳掉的是「哪一条 logic 结算」，跳不掉「这一条 ability 被激活了」这件事。
            //    ⚠️ 改前这一行靠的是 `pays[]` **顺延**（「被跳掉的那条付费 op 转给同组下一条去收」），
            //    那个补丁只在**本组还有下一条**时才成立 ⇒ 已随本笔删除（见 `ChargePaidGroups`）。
            //    真实形状（`A1414` 补完 `deal` / `heal` / `stun` 的贴点之后可达）：
            //    `2 ☀: Deal 1 damage. Deal 2 damage instead` ⇒ 只结算后者、**只扣 2 点**。
            // ⚠️ 「谁跳谁」的裁决**不在这里** —— 已经和配对一起搬进 `InsteadSkips`
            //    （两个重载**共用那一份**，出处与全部理由都在它的 doc 里，⛔ 别再内联回来）。
            //    留下的这一条只是**位置约束**：配对+裁决必须排在下面 `ChargePaidGroups` 之前 ——
            //    那一段会读 `ctx.Players[].Energy` 判条件（`SOR6` 的
            //    `If you have less than 6 Energy, … instead`），先收钱会让它读到**扣过费之后**
            //    的余额、把条件判反。

            // ---- 🔴 2026-10-12（`A1412`）：**收费抬到 `ability` 层** —— 按组收一次，与「哪条 op 会执行」彻底解耦 ----
            //
            // **原版判据**（与上面几段同一条链，逐条核过）：一条 ability = **一个** `manaCost`
            //   ＋ 一串 `AbilityLogic`（`ActiveAbility.cs` = `{ int manaCost; … activeAbilityLogic }` ·
            //   `CardAbility.cs` = `{ trigger; … abilityLogic }`）；一次激活只调**一次**
            //   `PlayerManager__UseMana`
            //   （`d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22`），
            //   余额也是**整条 ability 判一次**（`BattleManager__CanUseActiveAbility.c`）。
            //   ⇒ 「这一条 ability 激活了没有」与「它下面哪几条 logic 真做了事」是**两件事**
            //     ⇒ 钱必须在**动手之前**收完。
            //
            // **本段一次收掉改前那四个补丁各自治的一个洞**（同一个根因 = 收费落在 `ResolveOneCore`
            // 的逐 op 口上、而「谁付钱」只能从**执行次序**里推）：
            //   · `for each` / `N times` 跑 N 遍 ⇒ 只收一次（`A1369` 洞 1；改前靠「第 2 遍起递
            //     `CostShared = true` 的副本」）；
            //   · `, plus` / 尾句把一份前缀盖到一串 op ⇒ 只收一次（`A1369` 洞 2 · `A1358`；
            //     改前靠解析层 `StampPaidCost` 只给第一条打「付钱」标记）；
            //   · **付款那条被 `instead` 跳掉 ⇒ 照收**（`A1369` 洞 3；改前靠下面「付款顺延到本组
            //     下一条」那个补丁 —— 它只在**本组还有下一条**时才成立，且要读执行次序）；
            //   · **付款那条付不起 ⇒ 整组一条都不结算**（`A1413` ②）—— 原版「付不起 = 整条 ability
            //     不激活」，那一串 logic 一条都不跑。
            //   · `for each` 数出 **N = 0** 也**照收**（`A1413` ①；改前靠下面那一格单独送一趟
            //     `payOnly`，现在由本段统一保证）。
            //   ⚠️ 改前这四件事分散在**解析层标记 ＋ 执行时逐 op 判 ＋ 两处补丁**里，
            //      而补丁能成立的前提（「付钱那条一定第一个执行到」「本组还有下一条」）**全是执行次序**
            //      ⇒ 换个形状就静默漏一个洞。抬到这一层之后，钱只看**批次结构**（`CostShared` 这一份判据）。
            //
            // ⚠️ **位置**：必须排在**上面那段 `instead` 配对【之后】** —— 那一段会读
            //    `ctx.Players[].Energy` 判条件（`SOR6` 的 `If you have less than 6 Energy, … instead`），
            //    先收钱会让它读到**扣过费之后**的余额、把条件判反。
            // ⚠️ **钱的算法仍然只有一处** —— 本段只按组调 `ResolveOne(..., payOnly: true)`，
            //    查余额 / 扣费 / 日志 / 灵魂石世界事件全在 `ResolveOneCore` 的付费支里
            //    （⛔ 不写第二份余额比较，理由与清单 → `ChargePaidGroups` 的 doc）。
            var paidOk = ChargePaidGroups(ctx, owner, source, by, ops, chosen, unresolved);
            // 🆕 **2026-10-19（第五十七会话 `W57FIX2` · P1）：把「每条 op 所属的付费组收到钱没有」
            //    原样递给调用方**（只在调用方给了数组时）。`PlayTactic` 用它判
            //    「这张战术卡自己的 `UseFaith(605)` / `UseSpiritStone(600)` 能力到底激活了没有」——
            //    原版 `RawCardScript__OnCardPlayedWithTarget.c:59-84` 在**打出的那张牌自己**身上
            //    发这两拍，而 `CanTriggerAbility` 对 600/605 的闸是
            //    `GetCurrentSpiritStone / GetCurrentFaith >= ability.triggerValue`
            //    （`CardAbility__CanTriggerAbility.c:96-112`）⇒ 与我们「付费组收到钱」同一判据。
            if (paidOut != null)
                for (int i = 0; i < paidOut.Length && i < paidOk.Length; i++) paidOut[i] = paidOk[i];

            int done = 0;
            for (int i = 0; i < ops.Count; i++)
            {
                if (skip[i] || !paidOk[i]) continue;
                var op = ops[i];
                // 🆕 2026-10-23（`A12`）：`instead` 的条件已由 `InsteadSkips` 在**动手之前**判过
                //    ⇒ 这里不再判第二次（详见 `InsteadSkips` 的 doc「条件只判一次」）。
                if (plan.Judged[i]) op = WithoutCondition(op);

                // ---- 数值 = 某个资源 / 计数：`… equal to your Faith` / `… equal to the number of cards in the enemy hand` ----
                // ⚠️ 必须**递副本**下去，不能改 `op.Amount` 本身 —— 理由同下面那条注释：
                //    `EffectOp` 列表是**解析产物、会被复用**（同一张卡第二次打出时是同一份对象）。
                // 🔴 2026-10-19（`A2001` / E9）：判据收成 `TryApplyAmountRef` **一处** ——
                //    触发路（`ResolveOps` 的重载）原来没有这半条 ⇒ 触发正文里的 `equal to …` 按 0 结算。
                EffectOp amtOp;
                if (!TryApplyAmountRef(ctx, owner, by, op, unresolved, out amtOp)) continue;
                if (!ReferenceEquals(amtOp, op))
                {
                    // ⚠️ `n == 0` 照常递下去 —— `Deal 0 damage` / `Refill 0` 是「生效了但数值是 0」，
                    //    不是「这条没生效」（报错了玩家会以为卡坏了）。
                    if (ResolveOne(ctx, owner, source, by, Prepaid(amtOp), chosen, unresolved, card: sourceCard)) done++;
                    // 🆕 2026-10-24（执行代理 `E2`）：**每条 op 结束就整表覆盖一次**（原版每个效果 case 出口都调；
                    //   ⛔ 别只留出口那一次 ⇒ 同一次结算里后面的 op 会读到上一张卡的残值）。
                    NotePreviousAbilityTargets(ctx);
                    continue;
                }

                // ---- `for each` 增量型：数值 = 基础 + 计数 × 增量 ----
                // `Give +2 Melee Attack to a friendly troop, and an additional +2 Melee Attack
                //  for each Dark Pact on it` —— 一份契约时是 **+4**，不是 +2 再来两遍。
                // 🔴 **2026-10-24（执行代理 `E1` · `C3` §1·1）：算法只此一份 =
                //    `IsPerCountIncrement` ＋ `TryFoldPerCount`**（触发路同一个入口）。
                //    改前这一整套只写在**这里** ⇒ 触发路把 `PerCount` 整条丢掉（静默少数）。
                if (IsPerCountIncrement(op))
                {
                    EffectOp foldOp;
                    if (!TryFoldPerCount(ctx, owner, source, by, op, chosen, sourceCard, unresolved, out foldOp))
                        continue;
                    if (ResolveOne(ctx, owner, source, by, Prepaid(foldOp), chosen, unresolved, card: sourceCard)) done++;
                    NotePreviousAbilityTargets(ctx);   // 🆕 `E2`：逐 op 同步（同 P4）
                    continue;
                }

                // ---- `for each` 重复型：这一条**结算 N 遍** ----
                // 出处：规则书 :233「随机选出 N 张候选 … 结算」；原版 `_resolve_for_each:2524`。
                // ⚠️ **N = 0 就是一次都不结算**（不是「退化成 1 次」）—— 盘上一个友方部队都没有时，
                //    `Deal 1 damage to an enemy for each friendly unit` 就该什么都不做。
                // 🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 C）：算法**只此一份** =
                //    `TryComputeRepeats`（出牌路与触发路共用）。改前这一整套（正文计数 → 结构化 `mult`
                //    → `RepeatTimes` → `furtherMultiplier`）**只写在出牌路**里，触发层完全没有 ——
                //    `AM18`（回合末「按友方载具数」）因此只结算 1 遍。
                int repeats;
                if (!TryComputeRepeats(ctx, owner, source, op, sourceCard, chosen, by, unresolved, out repeats))
                    continue;
                // 🔴 2026-10-26（批次 8 · `B8_P`）：**阵营资源（buffMana 族）的乘数要乘进数值、不是重跑 N 遍** ——
                //   判据 `AbilityLogic__AdjustManaEffect.c:5-7`（`amount *= 乘数`，随后只建**一条** `BattleAction`）。
                //   ⚠️ 只影响「几次事件/广播」，数值不变 ⇒ 指纹会变、血量不变。见 `FoldManaMultiplier` 的 doc。
                op = FoldManaMultiplier(ctx, by, op, ref repeats);


                // ---- 🔴 2026-10-10（第十四会话 · `A1369` 洞 1）：**这 N 遍【只收一次钱】** ----
                //
                // **形状**（全池今天 0 张）：`4 Energy: Deal 1 damage to an enemy for each friendly troop`
                //   —— 付费前缀落在一条 `for each` 的 op 上，而那条 op **跑 N 遍**。
                //   `ResolveOneCore` 的付费支是**逐次**判的 ⇒ 原来**收 N 次**
                //   （离线实测：场上 3 个部队 ⇒ 能量 20 → 8，日志里「付了 4 点能量」打了 3 遍）。
                //
                // **判据（为什么只收一次）**：原版「重复」不是重跑 ability，而是 `AbilityLogic` 上的
                //   **乘数** —— `AbilityLogic.cs` 的 `TargetCriteria multiplier`
                //   （Tooltip：*"If different from 'Any', multiply by number of targets meeting criteria"*）
                //   ＋ `int furtherMultiplier` ⇒ `for each` 是**在一个 logic 内把数值乘 N**，
                //   **根本不回到收费那一层**；一次激活只调一次 `PlayerManager.UseMana`
                //   （`d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22`）。
                //   （`N times` 那一支 = `EffectOp.RepeatTimes`，同一条循环、同理。）
                //
                // ⚠️ 改前（`A1369` 洞 1 那一笔）第 2 遍起递的是**手工改过 `CostShared` 的副本** ——
                //    那个坑本身仍然成立（`CostShared` 写回解析产物会被**永久固化**：同一张卡第二次
                //    打出时是同一份对象，见上面 `op.Amount` 那条注释），只是现在不需手工做，
                //    `Prepaid` 统一负责。
                // ✅ **2026-10-12（`A1412`）：本段不再自己收钱** —— 钱已经由 `ResolveOps` 顶端的
                //    `ChargePaidGroups` 按组收过一次，这里**每一遍都递「已经付过」的那一份**
                //    （`Prepaid`）。于是改前那两条补丁**全部删掉**：
                //      · 第 2 遍起不用再手工 `Clone()` ＋ `CostShared = true`（`A1369` 洞 1）；
                //      · `repeats == 0` 不用再单独送一趟 `payOnly`（`A1413` ①）——
                //        那两件事现在都由「**先收钱、再执行**」这一条不变量保证。
                //    ⚠️ `N = 0` 时 `ok` **不因「收了钱」而置真**：这一刻效果**没落地** ⇒
                //       与改前一样不计进 `done`（只收钱、不改「这条结算了没有」的口径）。
                var firstOp = Prepaid(op);
                bool ok = false;
                for (int k = 0; k < repeats; k++)
                {
                    if (ResolveOne(ctx, owner, source, by, firstOp, chosen, unresolved, card: sourceCard)) ok = true;
                    NotePreviousAbilityTargets(ctx);   // 🆕 `E2`：每一遍都是一条 op 的出口（原版同样逐效果覆盖）
                }
                if (ok) done++;
            }
            // 摧毁/伤害可能把督军打死 —— 结算完统一判一次胜负
            // （`Hurt` 自己不判，和攻击那条路一致：由调用方在结算完之后判）
            if (!ctx.IsOver) CheckWinner(ctx);
            // 🆕 2026-10-24（引擎线 `A1` 第 1 件）：出牌 / 战术卡这条路的「上一条能力的目标」。
            NotePreviousAbilityTargets(ctx);
            return done;
        }

        /// <summary>
        /// 🔴 **2026-10-12（`A1412`）：把「这一条的付费前缀**已经结算过了**」递给 `ResolveOneCore`。**
        ///
        /// **什么时候用**：`ResolveOps` 顶端已经调过 <see cref="ChargePaidGroups"/>（= 一条 ability 的
        /// 付费前缀**按组收了一次**），于是**执行那一段**里每条 op 的钱都**不许再动一次** ——
        /// 一律经本方法递下去（`Cost > 0` 的**才需要** `Clone()` 一份改了那一格）。
        ///
        /// 判据仍然**只有**解析产物上那一份 <see cref="EffectOp.CostShared"/>（`EffectText.StampPaidCost` 贴、
        /// 这里是**读**）—— 本方法**不引入第二份「谁付钱」的口径**，只是把「已经付过」这个事实
        /// 转成 `ResolveOneCore` 认的形式（它那一支**不查余额、不再扣、不重复广播**）。
        /// 同理 `DoRepeat`（`A1415`：`Repeat this effect` 重复的那批 op 一个钱都不再收）也走它。
        ///
        /// ⛔ **绝不写回 `op.CostShared`**：`EffectOp` 列表是解析产物、会被复用
        /// （见 `ResolveOps` 里 `op.Amount` 那条注释）—— 所以一律 `Clone()` 出**副本**。
        /// （`op.Cost <= 0` 或**本来就已标 shared** 的 ⇒ 递**原件**，其余分支零拷贝、行为一字不变。）
        ///
        /// ⚠️ **历史**：本方法在 `A1412` 之前叫 `RoutePay(op, pay)`，第二个参数是「**这一趟谁付钱**」，
        /// 由 `ResolveOps` 本地算的 `pays[]` 决定（`instead` 跳掉付钱那条时**顺延**给同组下一条，
        /// `for each` 第 2..N 遍递 `false`）。收费抬到能力层之后那个数组**整个不需要了** ——
        /// 现在只有「已经付过」这一种情形。
        /// </summary>
        static EffectOp Prepaid(EffectOp op)
        {
            if (op == null || op.Cost <= 0) return op;      // 没价 ⇒ `CostShared` 这一格无意义
            if (op.CostShared) return op;                   // 已经标着「别人付过」 ⇒ 递原件
            var c = op.Clone();                             // `Clone()` = `MemberwiseClone`，改这一格够用
            c.CostShared = true;
            return c;
        }

        /// <summary>
        /// 🔴 **2026-10-12（`A1412`）：一条 ability 的付费前缀【按组收一次】—— 在动手之前收完。**
        ///
        /// **判据（原版）**：一条 ability = **一个** `manaCost` ＋ 一串 `AbilityLogic`
        /// （`ActiveAbility.cs` / `CardAbility.cs`，签名桩第一手），一次激活只调**一次**
        /// `PlayerManager__UseMana`（`d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22`），
        /// 余额也是**整条 ability 判一次**（`BattleManager__CanUseActiveAbility.c`）。
        /// ⇒ 「这一条 ability 激活了没有」与「它下面哪几条 logic 真做了事」是**两件事**。
        ///
        /// **组怎么划**（判据只认 `CostShared` 这一份 —— 与解析层 `StampPaidCost` 同一份）：
        /// 一份付费前缀 = 一条 `Cost &gt; 0 &amp;&amp; !CostShared` 的 op（**付钱那条**）
        /// ＋ 紧跟的一串 `Cost &gt; 0 &amp;&amp; CostShared`。
        /// 实测口径（`ruleprobe` 全池 dump）：付费 op **89** 条、其中 `shared=1` **3** 条
        /// （`UM81` / `UM110` / `SOR6`）；**带 ≥2 个付费组的卡只有 3 张、全是 `faith`**
        /// （`SOR27 Canoness` 2/4 · `SOR68 Sacred Rose Sister` 2/4 · `SOR44 Beacon of Faith` 4/7）
        /// ⇒ **按组**收是必须的：信仰那一格是**逐条门槛**，把整批合成「一个数」会静默答错另一半
        /// （`CardDef.FaithOps` 的 doc 里写了同一件事）。
        ///
        /// **返回值**：与 <paramref name="ops"/> **同长**的 `ok[]`，`false` = 这一条所属的组
        /// **没收到钱** ⇒ 调用方的执行循环**整组跳过**它（原版「付不起 = 整条 ability 不激活，
        /// 那一串 logic 一条都不跑」）。
        ///
        /// ⛔ **钱的算法不在这里 —— 一处都没有搬**：本方法只按组调
        /// `ResolveOne(..., payOnly: true)`，于是「查余额 / 判货币 / 扣费 / 打日志 / 灵魂石世界事件」
        /// 仍然**全部**留在 `ResolveOneCore` 的付费支里（`CostPrefixPayable` 是那一份余额判据）。
        /// 本方法只做两件它做不了的事：**何时收**（动手之前）与**收不到时整组怎么办**。
        ///
        /// ⚠️ **`payOnly` 的返回值 = 「这笔钱结算了没有」**（`A1412` 起）：认不出货币 / 付不起 ⇒ `false`；
        ///    收到钱、被 `waived` 免付（`ctx.SuppressCostDepth`，触发式激活 / `oathDouble` 重放）、
        ///    或这条本来就标着 `CostShared` ⇒ `true`。改前它返回的是「这条 op 有没有价」，
        ///    而唯一的调用点（`A1413` ① 的 `repeats == 0`）**没读**那个返回值 ⇒ 改动无回归。
        /// </summary>
        static bool[] ChargePaidGroups(BattleContext ctx, int owner, UnitState source, string by,
                                       IReadOnlyList<EffectOp> ops, UnitState chosen,
                                       List<string> unresolved)
        {
            var ok = new bool[ops.Count];
            for (int i = 0; i < ops.Count; i++) ok[i] = true;
            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                if (op == null || op.Cost <= 0 || op.CostShared) continue;   // 一组只认「付钱那条」
                if (ResolveOne(ctx, owner, source, by, op, chosen, unresolved, payOnly: true))
                    continue;                                                // 收到钱（或免付）⇒ 整组照常
                ok[i] = false;                                               // 没收到 ⇒ 这一条自己也不结算
                int j = i + 1;
                for (; j < ops.Count && ops[j] != null && ops[j].Cost > 0 && ops[j].CostShared; j++)
                    ok[j] = false;
                if (j > i + 1)
                    ctx.Log($"{by}：「{op.Source}」这份付费前缀**付不了**（余额不够 / 货币认不出）"
                          + " ⇒ 同属它的其余效果**一并跳过**（原版一条 ability 付不起就整条不激活，"
                          + "⛔ 不是白给后半句）");
            }
            return ok;
        }

        /// <summary>
        /// 🔴 **2026-10-11（第十四会话 · `A1413` ②）：`CostKindOf` 归一之后的货币名，本版认不认得。**
        ///
        /// 判据**原来内联在 `ResolveOneCore` 的付费支里**（那一长串 `kind != "energy" &amp;&amp; …`）——
        /// 提出来是为了让「整组要不要一起跳过」那一处（`ChargePaidGroups`，`A1412` 之前叫
        /// `ResolveOps` 里的 `groupUnpaid[]`）读**同一份**，⛔ 别在别处再写一遍这个货币名清单。
        /// </summary>
        static bool CostKindKnown(string kind)
            => kind == "" || kind == "energy" || kind == "faith" || kind == "spirit" || kind == "oath";

        /// <summary>
        /// 🔴 **2026-10-11（第十四会话 · `A1413` ②）：这一条 op 的付费前缀【现在付得起吗】。**
        ///
        /// `ResolveOneCore` 的付费闸（原 `!waived &amp;&amp; have &lt; op.Cost`）与
        /// `ChargePaidGroups`（`A1412` 之前叫 `ResolveOps` 里的 `groupUnpaid[]`）
        /// 的「付钱那条付不起 ⇒ 整组一并跳过」**共用这一份** —— **判据只此一处**，
        /// ⛔ 别在别处再写一遍余额比较 / 货币池映射。
        ///
        /// **三种「付不了」**：① 货币名认不出（`CostKindKnown` 为假）；
        /// ② 余额不够；③ ——（`waived` = `ctx.SuppressCostDepth &gt; 0`，触发式激活**免付**，算付得起）。
        /// ⚠️ **本方法只回答「能不能」，不改任何状态、不写日志** —— 扣钱与那两行日志留在
        ///    `ResolveOneCore` 的付费支里（一处落点）。
        ///
        /// 🔴 **2026-10-11（`A1453`）：`faith` 这一格的语义是【阈值】、不是余额。**
        ///    判据见 `ResolveOneCore` 付费支里那段长注释 —— 原版是 `faith &gt;= triggerValue`
        ///    的**门槛**，**没有任何扣除点**。本方法的**表达式一字不改**（`have &gt;= op.Cost`
        ///    正是 `faith &gt;= N`），改的只是「通过的后果」：信仰通过之后**不扣**。
        ///    ⇒ 谁要加第四种货币，**先想清楚它是余额还是阈值**，⛔ 别默认按余额做。
        /// </summary>
        static bool CostPrefixPayable(BattleContext ctx, int owner, EffectOp op, string kind)
        {
            if (!CostKindKnown(kind)) return false;             // ① 货币认不出 ⇒ 整条不结算
            if (ctx.SuppressCostDepth > 0) return true;         // ③ 触发式激活：免付
            var ps = ctx.Players[owner];
            // ⚠️ `faith` 这一格读的是**阈值对照值**（不是「余额」）—— 语义差别见上面那两行。
            int have = kind == "faith" ? ps.Faith
                     : kind == "spirit" ? ps.SpiritStones
                     : ps.Energy;
            return have >= op.Cost;
        }

        /// <summary>
        /// 转调 <see cref="ResolveOneCore"/>（见那里的 `payOnly` 参数）。
        /// </summary>
        /// <param name="payOnly">🆕 2026-10-11（`A1413` ①）：**只走付费支、不结算效果**
        /// （`A1412` 起的主调用点 = `ChargePaidGroups` —— 一条 ability 的付费前缀要在效果
        /// **动手之前**按组收一次；原来那一格是 `for each` 的 `repeats == 0`）。
        /// 缺省 `false` ⇒ 其余调用点一字不变。**返回值 = 「这笔钱结算了没有」**
        /// （`A1412` 起；判据与早退那两支见 `ResolveOneCore` 里 `if (payOnly)` 那一段的注释）。</param>
        static bool ResolveOne(BattleContext ctx, int owner, UnitState source, string by,
                               EffectOp op, UnitState chosen, List<string> unresolved,
                               bool payOnly = false,
                               // 🆕 2026-10-22（`W64` · `B-16`）：这条 op 所属的卡 → 结构化条件。
                               //   `null` = 不知道 ⇒ 条件退回旧正文口径（既有调用点行为不变）。
                               CardDef card = null)
        {
            // 「相邻」的 `Self` 锚点要知道**是谁在施放**（见 `ResolveAdjacentAnchor`）。
            // 它收在**上下文**里而不是给结算函数加参数：结算函数是一张**统一签名的表**
            // （`EffectDispatch`，28 个 handler），为一个字段去改 28 个签名 + 整张表，改动面不成比例。
            // 保存/恢复是必须的：效果会**嵌套**（`Repeat this effect` / `Choose one` 里再调 `ResolveOps`），
            // 不恢复的话外层后半段会拿到内层的施放者。
            // 🆕 2026-10-22（`W64` · `B-16`）：**有结构化目标条件行、却归不到这条 op** ⇒ 出声一次
            //   （同一 (卡, 原因) 只喊一次，见 `NoteTargetConditionGap`）。⛔ 不静默、⛔ 也不按卡硬套。
            if (card != null && op != null && op.Target != null && op.TargetCondition == null
                && !string.IsNullOrEmpty(op.TargetConditionGap))
                NoteTargetConditionGap(ctx, card, op);
            var prevActing = ctx.ActingUnit;
            ctx.ActingUnit = source;
            try { return ResolveOneCore(ctx, owner, source, by, op, chosen, unresolved, payOnly, card); }
            finally { ctx.ActingUnit = prevActing; }
        }

        static bool ResolveOneCore(BattleContext ctx, int owner, UnitState source, string by,
                                   EffectOp op, UnitState chosen, List<string> unresolved,
                                   bool payOnly = false, CardDef card = null)
        {
            // ---- 付费激活：付不起就**整段不结算**（我们自己的 `rule_core.gd:2529`（旁证、非权威））----
            // ⚠️ **2026-09-13 第三十三轮修了一条真 bug**：这里原来**只扣能量、完全不看 `CostKind`**。
            //    于是 `8 [Faith]: Deploy an additional Battle Sister` 那族卡**一直在偷偷扣能量** ——
            //    卡是 4 费的，要求 8 能量等于「永远不激活」，所以**没人发现**。这是标准的静默错行为。
            //    货币名 → 规范取值只有一处判据（`EffectText.CostKindOf`），这里不再自己认字符串。
            if (op.Cost > 0)
            {
                var ps = ctx.Players[owner];
                string kind = EffectText.CostKindOf(op.CostKind);
                if (!CostKindKnown(kind))
                {
                    // 认不出的货币（`might` / `attack` / `icon` …）**明说不支持**，别默默扣能量
                    ctx.Log($"{by}：「{op.Source}」要付 {op.Cost} 点「{op.CostKind}」，"
                          + "这个货币本版不认识 —— **这一条没生效**");
                    unresolved.Add(op.Source + "（付费货币认不出：" + op.CostKind + "）");
                    return false;
                }
                // 🆕 2026-09-16：**免付费**（`ctx.SuppressCostDepth > 0`）—— 只有「触发灵魂石能力」那条路
                // （`ResolveSpiritAbilityForced`，`Cosmic Serpent`）。判据与出处写在
                // `BattleContext.SuppressCostDepth` 的注释里（原版触发路径不查余额、不扣石）。
                // ⚠️ **仍然照旧判货币名**（上面那段）—— 免的是**付费**，不是「认不认识这个货币」。
                bool waived = ctx.SuppressCostDepth > 0;
                // ⚠️ **余额那一格不再在这里现算** —— 转调 `CostPrefixPayable`（判据只此一处，
                //    `ChargePaidGroups` 判「付钱那条付不起 ⇒ 整组一并跳过」读的是同一份，见 `A1413` ②）。
                string shown = kind == "faith" ? "信仰" : kind == "spirit" ? "灵魂石" : "能量";

                // 🔴 **2026-10-11（第十三会话 · `A1346` ②乙 ＋ `A1358`）：同一条付费前缀【只收一次】。**
                //
                //    **原版判据**（一条 ability = 一个 cost ＋ 一串 logic）：
                //      · `CardAbility.cs` = `{ AbilityTrigger trigger; …; List<AbilityLogic> abilityLogic }`；
                //        主动付费那一档 `ActiveAbility.cs` = `{ int manaCost; …; List<AbilityLogic> activeAbilityLogic }`；
                //      · `d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22` 取誓约值之后
                //        **一次激活只调一次 `PlayerManager__UseMana`**；`BattleManager__CanUseActiveAbility.c`
                //        也是**整条 ability 判一次**余额。
                //    **我们原来错在哪**：解析层是「一个付费前缀 → 贴到本段每条 op」
                //      （`EffectText.StampPaidCost` 上一版是裸 `foreach`），而一条前缀会盖住 2 条 op
                //      （跨句承接的第二句、或 `Finish` 把 `op.Tail` 递归解出来的那一条）
                //      ⇒ 这里**逐 op 收** ⇒ **同一次激活扣两倍**。
                //      实据（离线探针 `工具/ruleprobe`，全池跑过）：`UM110 Fall Back`
                //      `Oath 2: Lower its cost by 2 and give it Flank`（2 条各 `Cost=2` ⇒ 扣 4）、
                //      `SOR6 Righteous Repugnance` 的 `4 Energy:`（2 条各 4 ⇒ 扣 8）。
                //
                //    **口径**：`CostShared == true` = 这一条的钱**已经付掉了** ⇒
                //      **① 不查余额**（付过之后再查会把第二半句误判成「付不起」）、
                //      **② 不再扣**、**③ 不重复广播**（下面那段 `kind == "spirit"` 的世界事件）。
                //    🔴 **2026-10-12（`A1412`）这条口径的【来源】变了，含义不变**：原来这个标记
                //       **只**由解析层的 `EffectText.StampPaidCost` 打（组里第一条付钱、其余标 shared），
                //       执行那一段靠它 + `pays[]` 顺延推「谁付钱」；现在**收钱已经抬到 `ResolveOps`
                //       顶端按组收一遍**（`ChargePaidGroups`）⇒ 执行那一段的 op 一律经 `Prepaid(op)`
                //       递下来、**全都带着这个标记**（含**真正付钱的那一条**）。
                //       ⇒ 「谁付钱」这个判断在整个执行循环里**已经不存在了**，本支只剩「**别再收**」。
                //    ⚠️ **认不出的货币那道闸仍然逐条过**（上面那段没动）—— 一组里两条货币名相同，
                //       但「不认识的货币」该报几条就报几条，别让它靠在组首那一条上。
                //       🔴 **`A1412` 起实测影响面 = 0**：货币认不出的**那一组整组不执行**
                //       （`ChargePaidGroups` 判它没结算 ⇒ 组内 op 在调用方就被跳过），
                //       ⇒ 同一个「不认识的货币」**由 N 条日志变 1 条**（组首那条）。
                //       全池实测：89 条付费 op 的货币**全在** `energy/faith/spirit/oath` 里（0 处认不出）
                //       ⇒ 今天无卡可观测；将来真有这种卡，日志仍**出声**、只少几条重复。
                if (op.CostShared)
                {
                    // ⚠️ **`faith` 那一档措辞要换**：它是**阈值**不是钱（`A1453`）——
                    //    「不再收」这句话对信仰是**假的**（本来就没收过）。
                    // 🔴 **2026-10-12（`A1412`）措辞再改一次**：收费抬到 `ability` 层之后，
                    //    **付钱那条自己也是走到这一支的**（`ResolveOps` 执行那一段一律把 op 递成
                    //    `Prepaid(op)` ⇒ `CostShared == true`）⇒ 不能再写「与**上一条**同属一份前缀」
                    //    （对付钱那条而言这是假话）。改成「**这一份前缀在同一次激活里已经收过**」——
                    //    对两条都是真话（钱是在 `ChargePaidGroups` 那一趟收的）。
                    ctx.Log(kind == "faith"
                          ? $"{by}：「{op.Source}」的信仰门槛 {op.Cost} **不再判一次**"
                            + "（这份信仰前缀在同一次激活里已经判过 —— 收费/门槛抬在 ability 层）"
                          : $"{by}：「{op.Source}」的 {op.Cost} 点{shown}**不再收**"
                            + "（这份付费前缀在同一次激活里已经收过 —— 收费抬在 ability 层）");
                }
                else if (!CostPrefixPayable(ctx, owner, op, kind))
                {
                    // 🔴 `faith` 的措辞**必须是「阈值不够」**（`A1453`）—— 说成「付不起」会把机制讲错，
                    //    玩家/下个会话会以为它是可以攒钱去付的开销。
                    if (kind == "faith")
                    {
                        ctx.Log($"{by}：「{op.Source}」要信仰达到 {op.Cost}（阈值），"
                              + $"现在只有 {ps.Faith} —— **这一条没生效**（信仰**不扣除**）");
                        unresolved.Add(op.Source + "（信仰不足 " + op.Cost + "）");
                    }
                    else
                    {
                        ctx.Log($"{by}：「{op.Source}」需要 {op.Cost} 点{shown}才激活，不够 —— **这一条没生效**");
                        unresolved.Add(op.Source + "（付费不够）");
                    }
                    return false;
                }
                else if (!waived)
                {
                    // 🔴 **2026-10-11（第十四会话 · `A1453`）：信仰（`faith`）是【阈值】，一分都不扣。**
                    //
                    //   **原版判据（反编译，逐条核过）**：
                    //     · 全量反编译里 **没有**任何扣除信仰的点 —— `PlayerManager.AddFaithMana`
                    //       （`faithMana@0xB0 += n`）**两个调用点全是「加」**；
                    //       `HasEnoughMana` 的四个池是 `0x4C/0x74/0x88/0x9C`，`faith` 在 `0xB0`
                    //       **不在池里**；`ManaType` 只有 `Normal` / `SpiritStone`；
                    //       `CardScript` 上**没有**与 `CanUseSpiritStone` 对应的 `CanUseFaith`。
                    //     · 「付费」那一读法（`N ☀:` = 花 N 点信仰）**唯一的收费点**在这个 build 里
                    //       读不出来：`get_currentAbilityCost` 的 RVA `0x4BD440` 从 DLL 读出的原始字节
                    //       = `33 C0 C3`（`xor eax,eax; ret`，ICF 死值化）⇒ `HasEnoughMana` 恒真。
                    //     · 真的那一支在 `CardAbility__CanTriggerAbility.c` 的 605 支：
                    //       `faith >= 1` **且** `faith >= ability.triggerValue`（`+0x20`，即卡面那个 N）
                    //       ⇒ **阈值判定**。（604/605 由 `TriggerAbilities` 在
                    //       `RawCardScript__OnCardPlayedWithTarget.c:73`（Tactic/Whispers 打出时）与
                    //       `CardScript__ResolveUnitSummoned.c:51`（Minion 入场时）发起。）
                    //   ⇒ **闸门（够不够）留在上面那句 `CostPrefixPayable` 里不动**（它判的
                    //      `have >= op.Cost` 正是 `faith >= N`），**只把这一个「扣」删掉**。
                    //   ⚠️ 扣不得的实证代价：`A1436` 的夹具直调里 6 张卡的信仰 20→14/17/15/19/17/12 ——
                    //      那是我们自洽、但与原版不是同一种机制（`A1453` 的「反向对照证据」）。
                    if (kind == "faith")
                    {
                        ctx.Log($"{by}：信仰 {ps.Faith} ≥ {op.Cost}（阈值判定通过），激活「{op.Source}」"
                              + " —— **不扣除信仰**（信仰是阈值、不是开销）");
                    }
                    else
                    {
                        if (kind == "spirit") ps.SpiritStones -= op.Cost;
                        // 🔴 **2026-10-21（引擎线 `T03·b`）**：能量的扣除**走唯一的那个口**
                        //   （`RuleCore.SpendNormalMana`）—— 原版 `UseMana(…, ManaType.Normal, …)`
                        //   → `PlayerManager__UseNormalEnergy`，扣到 0 会触发 `SpentAllMana`。
                        //   ⛔ 别在这里直接 `ps.Energy -= …`（那样「花光法力」永远不触发，
                        //      而且下一次加支出点还会再漏一处）。
                        else RuleCore.SpendNormalMana(ctx, owner, op.Cost, op.Source);
                        ctx.Log($"{by} 付了 {op.Cost} 点{shown}激活「{op.Source}」");
                    }
                }
                else
                {
                    // 日志照打：不清不楚的「什么都没发生」和「实现好了」在画面上一样（本工程的规矩）
                    ctx.Log($"{by}：「{op.Source}」的 {op.Cost} 点{shown}**免付**"
                          + "（触发式激活，不是玩家主动花钱 ——「真触发」）");
                }

                // 🆕 2026-09-13 A3：**花灵魂石激活一个「灵魂石能力」** ⇒ 广播事件。
                //    卡面：`When you trigger a Spirit Stone ability, gain Sniper and +1 Ranged Attack`
                //    （`Bright Lance Vyper`）。语义与出处（`AbilityTrigger.UseSpiritStone = 600` /
                //    `BattleActionType.useWaystone = 76` → `triggerSpiritStone = 77`）写在
                //    `WhenEvent.SpiritAbility` 的注释里。
                //    ⚠️ 卡面写的是 `**you** trigger …` ⇒ 这是**玩家级**事件（`who` = 付石那一方），
                //       `card`/`subject` 都传 `null`（和 `When you gain Faith` 同一类）。
                //    ⚠️ ~~**实测卡池 0 张带这种前缀** ⇒ 这条广播现在**发不出来**（监听器点亮但不响），
                //       和 `Ravenwing Champion` 同类。留着是对的，但别当成「已经铺完」。~~
                //    ✅ **2026-10-04 更正：「卡池 0 张带这种前缀 / 发不出来」不成立** —— 实测
                //       `RuleEngine/Resources/cards_engine.json`（1126 张）里 `desc` 带 `N [Spirit Stone]:`
                //       的有 **28 张**（16 部队 + 12 计策；全文 `[Spirit Stone]` 共 28 处），其中 13 张在
                //       `desc` 开头 ⇒ **有触发源**。（同一处错句在 `WhenEvent.SpiritAbility` 的注释里也有一份，一并改了。）
                //    🆕 2026-09-16：**免付费那条路也发**（上面 `waived`）—— `Cosmic Serpent` 干的就是
                //       「触发这些能力」，卡面那句话（`When **you trigger** a Spirit Stone ability`）
                //       说的正是这件事，按付费与否分叉反而是错的。
                //    🔴 **2026-10-11（`A1346` ②乙）：一组里只发一次**（`!op.CostShared`）——
                //       卡面写的是「**当**你触发一个灵魂石能力」，一次激活 = 一次触发；
                //       一份付费前缀盖住 2 条 op 时发两遍就成了「触发两次」的假事件。
                if (kind == "spirit" && !op.CostShared)
                    BroadcastUnitSpiritStone(ctx, owner, source);
            }

            // 🔴 **2026-10-11（第十四会话 · `A1413` ①）：`payOnly` —— 只走付费支、不结算效果。**
            //
            // **为什么需要它**：付费前缀要在**效果动手之前**结算（`A1412` 把收费抬到 `ability` 层，
            //   调用点是 `ChargePaidGroups`），而「结算一条效果」与「付一条前缀的钱」是两件事
            //   ⇒ 本参数让调用方**只过付费这一支**、后面的条件闸与 handler 一概不走。
            //   改前它唯一的调用点是 `repeats == 0` 那一格（`A1413` ①）：
            //   `for each` 数出 0 遍 ⇒ 一遍都不跑 ⇒ 原来**一分钱也不收**，而原版
            //   **激活了就收**（`d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22`）。
            // ⚠️ **放在付费支【之后】** —— 这样收钱那一整套（查余额 / 扣 / 日志 / 灵魂石广播）
            //    **一份判据、一处落点**，⛔ 没有第二份「谁付多少」的算法。
            // ⚠️ **条件闸与 handler 都不走**：`Condition` 判的是「这条 logic 做不做」，
            //    与「这条 ability 激活了没有」是两件事（原版条件在 logic 内部判、钱已经付了）。
            // 🔴 **2026-10-12（`A1412`）：返回值 = 「这笔钱结算了没有」**（`ChargePaidGroups` 靠它
            //    决定整组跳不跳）。走到这一行 = 付费支**一路通过**（扣了 / 免付 / 本来就
            //    `CostShared` 由同组付钱那条付过）⇒ `true`；
            //    认不出货币、余额不够那两支**在上面早退 `return false`**，与「没结算」同义。
            //    ⚠️ **改前**这里返回的是 `op.Cost > 0`（= 「这条 op 有没有价」），而当时唯一的
            //    调用点**没读**返回值 ⇒ 语义换成上面这个**没有任何既有行为跟着变**。
            //    ⚠️ `Cost <= 0` 的 op 不会被 `ChargePaidGroups` 送进来（它只送「付钱那条」），
            //    真送进来也只可能是「没价 ⇒ 没有可收的钱」⇒ 一并算「结算完了」是安全的。
            if (payOnly) return true;

            // ---- 条件：判不了就**不结算**，并如实报出来 ----
            if (!string.IsNullOrEmpty(op.Condition))
            {
                bool ok;
                if (!ConditionHolds(ctx, owner, op, chosen, out ok, card))
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
            // `Stratagems in your hand become a Hunting Wolf or Fenrisian Wolf`（`Hrolf the Ironhowl`）
            { "become",     (c, o, b, op, ch, un) => DoBecome(c, o, b, op, un) },
            // `6 [Energy]: Extend effect until your next turn` / `8 [Energy]: Give it permanently`
            { "paidmod",    (c, o, b, op, ch, un) => DoPaidMod(c, o, b, op, ch, un) },
            { "heal",       (c, o, b, op, ch, un) => DoHeal(c, o, b, op, ch, un) },
            { "draw",       (c, o, b, op, ch, un) => DoDraw(c, o, b, op) },
            { "drawtype",   (c, o, b, op, ch, un) => DoDrawType(c, o, b, op) },
            { "drawref",    (c, o, b, op, ch, un) => DoDrawRef(c, o, b, op, un) },
            // 🔴🆕 **2026-10-26（完全复刻 · `P3` · `TAU68`）**：`return` 这一行原来**把 `ch` 丢了**
            //   （`DoReturn(c, o, b, op, un)`），而同一块上下的 `heal` / `deal` / `give` 全都转发。
            //   后果 = 玩家点定的目标在「回手 / 回牌库」这条路上**被静默忽略**，handler 自己按卡面
            //   文本重扫棋盘 ⇒ `TAU68 Trail Shaper`（`Rally: Return a friendly Infantry to your hand`）
            //   把**刚打出去的它自己**收回手牌。
            //   判据（第一权威）= 原版 `AbilityLogic__PlayAbility.c:549-568`（`iVar8 == 0x8c`）：
            //   `recallToOwnerHand` 是**遍历 `GetTargets` 的结果表**逐张 `AddRecallToHand`，
            //   而 `ta=30 (target)` 的结果表 = `{targetCard}`（`AbilityLogic__GetTargets.c:192-207`）
            //   ⇒ 原版**只回玩家点的那一张**，没有「扫第一个」这种兜底。
            { "return",     (c, o, b, op, ch, un) => DoReturn(c, o, b, op, ch, un) },
            { "destroy",    (c, o, b, op, ch, un) => DoDestroy(c, o, b, op, ch, un) },
            { "stun",       (c, o, b, op, ch, un) => DoStun(c, o, b, op, ch, un) },
            // 🆕 **2026-10-26（完全复刻批次 4 · `F2` · 判据来自 `F3` 的 R3-F2）**：
            //   `Jam a unit` / `Jam all …` —— 动词 `jam`（`CardDef.ActiveAbilityFamilyHasVerb` 早就认它）。
            //   语义全在 `UnitState.ApplyJam()`（`F3` 已建）；本文件只负责
            //   「选目标 → 五步驱动顺序 → 广播 → 致死走死亡结算」，见 `ResolveJamUnit` 的 doc。
            { "jam",        (c, o, b, op, ch, un) => DoJam(c, o, b, op, ch, un) },
            { "blind",      (c, o, b, op, ch, un) => DoBlind(c, o, b, op, ch, un) },
            // 🆕 2026-09-13 A4：两个小原子（`Does nothing` / `Reload the Duty …`）
            { "noeffect",   (c, o, b, op, ch, un) => DoNoEffect(c, o, b, op) },
            { "costwhen",   (c, o, b, op, ch, un) => DoCostWhenStub(c, o, b, op) },
            // `<效果> when <事件>`（**后缀形**）的**标记 op**（`A1461` / `A1575`，2026-10-12）——
            // 见 `EffectText.TryWhenSuffix`：机制在**事件层**（`CardDef.WhenTriggers` +
            // `BroadcastWhen`），这一条只是让整句解析得出并挡住 `TryGain` 的错解。
            { "whenclause", (c, o, b, op, ch, un) => DoWhenClauseStub(c, o, b, op) },
            // `This costs N less if you control a unit with <关键词>` 的**标记 op**（见 `EffectText.TryCostIfControl`）
            { "costifcontrol", (c, o, b, op, ch, un) => DoCostIfControlStub(c, o, b, op) },
            { "sethealth",  (c, o, b, op, ch, un) => DoSetHealth(c, o, b, op, ch, un) },
            // 🔴 **`setCurrentHealth(100)`**（原版 `CardScript__AddEffect.c:229-247`；卡面 `Lower its Health to N`）
            //   —— **无条件写、不夹上限、不查 Invulnerable、不发伤害事件**。⛔ 别和 `sethealth`（buffType 9）混。
            { "setcurrenthealth", (c, o, b, op, ch, un) => DoSetCurrentHealth(c, o, b, op, ch, un) },
            // `Double the Melee Attack and Health of a friendly troop`（`Possession`，Black Legion）
            { "double",     (c, o, b, op, ch, un) => DoDouble(c, o, b, op, ch, un) },
            // `The next time it uses Ferocity this turn, it stays in play`（`Bjorn's Shrine`）
            { "ferocitystay",(c, o, b, op, ch, un) => DoFerocityStay(c, o, b, op, ch, un) },
            // `it triggers an additional time` / `it applies the effect twice` —— 🆕 2026-09-16
            // （`GOF_Big_Choppa_Nob` 的 Mob · `TL30 Broodlord` 的 Synapse）
            { "extratrigger",(c, o, b, op, ch, un) => DoExtraTrigger(c, o, b, op, ch, un) },
            // `Take control of an enemy troop this turn and give it Fast` —— 🆕 2026-09-16
            // （`GSC_Telephatic_Domination`，全池唯一一张）
            { "takecontrol",(c, o, b, op, ch, un) => DoTakeControl(c, o, b, op, ch, un) },
            // `Trigger the <关键词> ability/abilities of <目标>` / `… and trigger their <X> abilities`
            // —— 🆕 2026-09-14 A4 批 4（`Author of the Codex` / `Duty's End` / `Atalan Leader` / 5 条尾句）
            { "triggerability",(c, o, b, op, ch, un) => DoTriggerAbility(c, o, b, op, ch, un) },
            { "reloadduty", (c, o, b, op, ch, un) => DoReloadDuty(c, o, b, op, un) },
            { "give",       (c, o, b, op, ch, un) => DoGive(c, o, b, op, ch, un, +1) },
            { "gain",       (c, o, b, op, ch, un) => DoGive(c, o, b, op, ch, un, +1) },
            { "gainenergy", (c, o, b, op, ch, un) => DoEnergy(c, o, b, op, un) },
            // 阵营资源（2026-09-13 第三十三轮）：给**玩家自己**的计数器加，不是给单位加
            { "gainfaith",  (c, o, b, op, ch, un) => DoFactionResource(c, o, b, op, "faith",  un) },
            { "gainspirit", (c, o, b, op, ch, un) => DoFactionResource(c, o, b, op, "spirit", un) },
            { "gainquest",  (c, o, b, op, ch, un) => DoFactionResource(c, o, b, op, "quest",  un) },
            { "spendspirit",(c, o, b, op, ch, un) => DoSpendSpirit(c, o, b, op, un) },
            { "lose",       (c, o, b, op, ch, un) => DoGive(c, o, b, op, ch, un, -1) },
            { "refill",     (c, o, b, op, ch, un) => DoRefill(c, o, b, op) },
            { "chooseone",  (c, o, b, op, ch, un) => DoChooseOne(c, o, b, op, ch, un) },
            { "choosecard", (c, o, b, op, ch, un) => DoChooseCard(c, o, b, op, ch, un) },
            // 🆕 2026-09-14 T3：**选一个效果**（`Exemplary Warrior` / Leviathan 两张）——
            // ⚠️ **不是** `choosecard`：那个从卡池/牌库/手牌里筛**卡**，这个从**登记好的固定几项**里挑。
            { "chooseeffect",(c, o, b, op, ch, un) => DoChooseEffect(c, o, b, op, ch, un) },
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
        /// **判据入口**（🔴 `D26`，2026-10-19 改指真权威；⚠️ 下面这几份**只当索引用、未逐句核**）：
        /// `decomp_full/BattleManager__AddDrawSpecificCard.c`（定向翻找的入口）·
        /// `decomp_full/SupportMethods__ShuffleCardList.c`（`BattleManager__ShuffleDeck.c` 转调它
        /// ⇒ ③「找到才洗牌」那半条的真权威）· `decomp_full/CardScript__SetupInDeck.c`。
        /// ⚠️ ② **规则书 L461-466**（**粉丝实体版、第二来源**；`rule_core.gd:4650` 的
        /// `_draw_specific_type` 逐条照抄它 —— 而那份 `.gd` 是**我们自己的上一版 Godot 复刻**，
        /// **旁证、非判据**，见文件头）：
        ///   > Draw cards from your deck until you draw a matching card, put that card into your hand
        ///   > or deploy it as indicated, **then shuffle your deck**.
        /// 照抄的三个细节：
        ///   ① 从**牌库顶往下翻**（= 从列表末尾往前，和 `Draw` 同一边），
        ///      **翻过去的牌留在牌库**（不是「抽出来再看」，是「找」）；
        ///   ② 找到 N 张就停；**一张都没找到就什么都不发生**（只写日志）；
        ///   ③ 找到了才**洗牌**（「then shuffle your deck」）—— 走 `ctx.GetRandomInt`（原版预烧表；2026-10-27 `E1RNG` 前是 `ctx.Rng`）。
        /// </summary>
        static bool DoDrawType(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var ps = ctx.Players[owner];
            string kind = op.Payload ?? "";
            int want = op.Amount > 0 ? op.Amount : 1;
            int got = 0;
            var taken = new List<CardInstance>();      // 第 7 行第 2 步：翻找出来的**是那几份**
            for (int i = ps.Deck.Count - 1; i >= 0 && got < want; i--)
            {
                var c = ps.Deck[i];
                if (c == null || !CreatePool.MatchesKind(c.Card, kind)) continue;
                ps.Deck.RemoveAt(i);
                taken.Add(c);
                got++;
            }
            // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）：进手牌走「先判上限、再 Add」那一个 helper。**
            //   `RuleCore.AddCardToHandWithinLimit` 的形状 = 原版 `PlayerHand._AddDrawnCardToHand_d__37:113-140`
            //   / `_AddCardNotDrawnToHand_d__39:26-30`（满了就把**刚翻出来的这张**送墓地、手牌一张不动）。
            //   ⛔ 别再「先全 Add、最后 `EnforceHandLimit`」—— 手牌本来就超上限时那会丢到旧牌身上。
            var landed = new List<CardInstance>();
            foreach (var c in taken)
                if (RuleCore.AddCardToHandWithinLimit(ctx, owner, c)) landed.Add(c);

            // `For each troop drawn …` 要数这个（和 `DoDraw` 同一条路）
            // 🔴 第 7 行第 3 步：记的是**哪几份**（指代要落到具体那一份上）
            // ⚠️ 只记**真进了手牌**的那几份 —— 与 `DoDraw` 同形（它按 `Hand` 里新增的取，
            //   满手时被送墓地的那张本来就不算「抽到」）。
            foreach (var c in landed) ctx.DrawnThisResolve.Add(c);

            // 🆕 **2026-10-18（第三会话 · `A992` 遗留乙）：手牌那一侧的代词槽也要填 —— 与 `DoDraw` 同形。**
            //    判据只此一处（`DoDraw` 那一段），这里是**照抄**它那三句（`Clear()` + 逐个 `Add` +
            //    单数槽「恰好一张才填」）—— ⛔ 别自创形状，两边迟早会不一致。
            //    ⚠️ 与上一句**不是一回事**：`DrawnThisResolve` 是「本次结算抽到几张」的**计数账**
            //      （`For each troop drawn …` 数它、**只累加不清**），这一对是**代词槽**
            //      （`BattleContext.LastHandTargets` / `LastHandTarget`，`Clear()` 之后装
            //      **最近一批**落手的 —— `DoDraw` 本来就是这个语义）。
            //    ⚠️ **今天零可观测差异**（`HandReferents` 先读槽、槽空才退回计数账，而本步两条都写）；
            //      补它是为了堵住那个**会咬人的形状**：同一 resolve 里
            //      「`draw` 之后走这一条、再 `give it/them`」——那时槽里会留着**上一批**
            //      （或干脆是空的），与「刚进手牌的那几份」不是同一批。
            ctx.LastHandTargets.Clear();
            foreach (var c in landed) ctx.LastHandTargets.Add(c);
            ctx.LastHandTarget = ctx.LastHandTargets.Count == 1 ? ctx.LastHandTargets[0] : null;

            if (got == 0)
            {
                ctx.Log($"{by}：「{op.Source}」翻遍了牌库也没找到「{kind}」—— 什么都没抽到");
                return true;                       // 找不到是**正常空过**，不是失败
            }
            Shuffle(ps.Deck, ctx);             // 规则书：「then shuffle your deck」
            ctx.Log($"{by}：「{op.Source}」定向翻找「{kind}」拿到 {got} 张（{Names(taken)}），牌库已洗");
            return true;
        }

        static string Names(List<CardDef> list)
        {
            string s = "";
            foreach (var c in list) { if (s.Length > 0) s += "、"; s += c.Name; }
            return s;
        }

        static string Names(List<CardInstance> list)      // 第 7 行第 2 步加的：日志里打「哪几份」的名字
        {
            string s = "";
            foreach (var c in list) { if (s.Length > 0) s += "、"; s += c.Card != null ? c.Card.Name : "?"; }
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
        ///   ① **不许掷骰**：`random` 的选择用 `ctx.GetRandomInt`（原版预烧表；2026-10-27 `E1RNG` 前是 `ctx.Rng`），
        ///      绝不用 `UnityEngine.Random`；
        ///   ② **目标找不到就空过**，不自动改打别的（那是替玩家做决定）。
        /// </summary>
        /// <summary>🆕 2026-10-23（`B-17`）：`TargetsAffected` 数值 → 枚举名（**只给日志/自检看**）。
        /// 表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`；只收常被问到的几档，
        /// 其余返回 `""`（⛔ 不编名字）。</summary>
        static string TargetsAffectedName(int ta)
        {
            switch (ta)
            {
                case 0: return "any";
                case 10: return "allFiltered";
                case 20: return "self";
                case 30: return "target";
                case 40: return "attacker";
                case 80: return "actingCard";
                case 120: return "friendlyHero";
                case 130: return "enemyHero";
                // 🆕 2026-10-24（引擎线 `G5`）：`ta=200/201` 那一族的运行期引用表，见 `SetReferenceCards`
                // / `UnitState.ReferencedCards`（判据 → `资料/普查产出_1024/C5_剩余判据普查.md` §3）。
                case 200: return "referencedCards";
                case 201: return "referencedCardsInPlay";
                default: return "";
            }
        }

        /// <summary>
        /// 🔴 **2026-10-24（引擎线 `G5`）：原版能力 `380 setReferenceCard` 的消费者。**
        ///
        /// <para>**判据（第一权威，`d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:2128-2160`，
        /// `iVar8 == 0x17c`）**：先给 `actingCard.referencedCards` **换一张新 List**（`:2139-2143`
        /// 直写字段 `+0x278`），再把**本条 abilityLogic 自己的目标表**逐个 `Add` 进去（`:2144-2160`）。
        /// ⇒ 语义 = **覆盖**（不是追加到上一次的结果上）。我们照抄：`Clear()` 后再 `AddRange()`
        /// （`UnitState.ReferencedCards` 是 `readonly` 字段，换对象这一层用清空等价表达）。</para>
        ///
        /// <para>**目标表从哪来**：就是这条 logic 自己的 `targetCriteria` —— 这里直接复用
        /// <see cref="ResolveTargets"/>（与 ta=200/201 的**读**同一份解析，⛔ 不另写一遍）。
        /// 池内唯一的 380 用法（`AM54` / `GOF93`）其 `ta = 150 previousAbilityFiltered`，
        /// 而 `ta` 已在 `EffectText.<see cref="EffectText.AssignAbilityTargets"/>` 那一趟挂到 op 上。</para>
        ///
        /// <para>⚠️ **今天没有生产调用点**：我们的引擎**不跑 SO `abilityLogic`**，
        /// `cards_engine.json` 也没有 `chosenAbility` 这一列（见 `EffectResolver.FollowUpSchedulerOf`
        /// 的说明）⇒ 380 这条能力没有自然触发口。本方法是**结构件**：先把原版那一跳的语义落成
        /// 可被调用的一个口（自检直接调它），等能力数据那条线接上再挂调用点。
        /// ⛔ 别为了让某张卡「看起来动了」而在文字管线里硬塞一个猜测的调用点。</para>
        ///
        /// <para>⚠️ **副作用**：`ResolveTargets` 会覆写 `ctx.LastTargets` / `ctx.LastTarget`
        ///（供下一句 `it` / `them` 指代）。原版 380 那一跳读的是**另一套**静态池
        ///（`SetPreviousAbilityTargets`），不碰这两个代词槽 ⇒ 这里**先存后还原**，
        /// 免得一条「设立引用表」的 logic 把后续正文的代词目标悄悄改掉（那会静默错打）。</para>
        /// </summary>
        /// <returns>写进引用表的条数（`acting == null` ⇒ 0）。</returns>
        public static int SetReferenceCards(BattleContext ctx, int owner, UnitState acting,
                                            EffectTargetSpec spec, UnitState source = null,
                                            UnitState chosen = null)
        {
            if (ctx == null) return 0;
            if (acting == null)
            {
                ctx.Log("（能力 380 `setReferenceCard`：**没有施放的那张牌**（`actingCard` 为空）"
                      + "⇒ 引用表无处可写，本次空过 —— 原版 `PlayAbility.c:2139` 写的是 `actingCard + 0x278`）");
                return 0;
            }

            var saved = new List<UnitState>(ctx.LastTargets);
            var savedLast = ctx.LastTarget;
            var got = ResolveTargets(ctx, owner, spec, source, chosen);
            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(saved);
            ctx.LastTarget = savedLast;

            acting.ReferencedCards.Clear();
            for (int i = 0; i < got.Count; i++)
                if (got[i] != null) acting.ReferencedCards.Add(got[i]);
            ctx.Log($"（能力 380 `setReferenceCard`：「{acting.Name}」的引用表 = "
                  + $"{acting.ReferencedCards.Count} 张（{Names(acting.ReferencedCards)}）"
                  + " —— 判据 `AbilityLogic__PlayAbility.c:2128-2160`）");
            return acting.ReferencedCards.Count;
        }

        /// <summary>🆕 **2026-10-25（执行代理 `E4` · `E4-1`）：把一个「这条 ability 落到的目标」
        /// 写进正等着填的那条 `EvtKind.Trigger`。调用点 = `ResolveTargets` 那一个收口
        /// （⛔ 别在别处再写一次 —— 两处写同一条规则，迟早不一致）。
        ///
        /// <para><b>三条判据</b>：
        /// ① **只填第一个**（`TriggerTargetSinkDone`）—— 原版按 `possibleTargets` 逐目标各播一份，
        ///    我们的事件只能带 1 个（`BattleEvent.Target*` 是标量）；
        /// ② **跳过「就是施放者自己」的目标** —— 原版落点 `boardPos = GetBoardPosOfCard(targetCard)`，
        ///    自指时 `boardPos == start` ⇒ `normalize(0)`、**不位移**（与「没有目标」同一结果）。
        ///    实据：`TL37 Spore Mine` 的两条 logic 是「对随机敌人 3 点」(lg=0) 与「摧毁自己」(lg=1)，
        ///    **先解析出来的是后者的自指目标**；而那条 `SporeMine_Explosion` 位移挂在**敌人那条**上
        ///    ⇒ 不跳过就永远填成自己（等于没修）。
        /// ③ **必须在捕获这一刻就落格号**（`FindSlot`）—— 目标常常被这条效果**打死**，
        ///    等结算完再查就查不到了（那会让「打死的那个」反而没有落点）。</para>
        /// </summary>
        static void NoteTriggerTargetSink(BattleContext ctx, List<UnitState> list)
        {
            var evt = ctx?.TriggerTargetSink;
            if (evt == null || ctx.TriggerTargetSinkDone || list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var u = list[i];
                if (u == null) continue;
                if (ctx.ActingUnit != null && ReferenceEquals(u, ctx.ActingUnit)) continue;   // 判据 ②
                int tp, ts;
                if (!FindSlot(ctx, u, out tp, out ts)) continue;                              // 判据 ③
                evt.TargetPlayer = tp;
                evt.TargetSlot = ts;
                evt.TargetCardId = u.Name;
                ctx.TriggerTargetSinkDone = true;
                return;
            }
        }

        /// <summary>🔴🆕 **2026-10-25（执行代理 `E4` · `E4-1`）：`ResolveTargets` 的**唯一收口** ——
        /// 它只做一件事：把「这条 ability **第一个**真的落到的目标」记进
        /// <see cref="BattleContext.TriggerTargetSink"/>（如果那条 `EvtKind.Trigger` 正等着填）。
        ///
        /// <para>**为什么要包一层**：`ResolveTargets` 有 **13 个 `return list`**（每档目标规格各一个），
        /// 在每个 `return` 前各补一句 = 13 份同一条规则。包一层 ⇒ **判据只此一处**。</para>
        ///
        /// <para>**为什么要填**：见 <see cref="BattleContext.TriggerTargetSink"/> 的 doc
        /// （原版 `AbilityLogic__PlayAbility.c:257-264` 把 `GetTargets` 的结果交给
        /// `PlayAnim`，而 `PlayAnim.c:171-248` 按目标逐张 `AddCardAnim` ⇒ 动画要落点就必须要目标）。
        /// 我们的事件在**广播那一拍**发出、目标要等**结算那一拍**才选出来 ⇒ 事后填同一个对象。</para>
        /// </summary>
        public static List<UnitState> ResolveTargets(BattleContext ctx, int owner,
                                                     EffectTargetSpec spec, UnitState source,
                                                     UnitState chosen,
                                                     CardDef.ConditionSpec structuredCondition = null,
                                                     EffectOp op = null)
        {
            var list = ResolveTargetsCore(ctx, owner, spec, source, chosen, structuredCondition, op);
            NoteTriggerTargetSink(ctx, list);
            return list;
        }

        /// <summary>`ResolveTargets` 的方法体（判据全在里面；**别直接调它** —— 走上面那个收口，
        /// 否则「trigger 事件的目标」会漏记）。</summary>
        static List<UnitState> ResolveTargetsCore(BattleContext ctx, int owner,
                                                     EffectTargetSpec spec, UnitState source,
                                                     UnitState chosen,
                                                     // 🆕 2026-10-22（`W64` · `B-16`）：这一格的结构化条件行
                                                     //   （原版 `TargetCriteria.conditionsFilter`）。
                                                     //   `null` = 不按结构化条件筛。
                                                     //   生产调用点传的是 **`op.TargetCondition`**（逐 op 归位，
                                                     //   见 `EffectText.AssignAbilityIndices`）；`IsLegalPick`
                                                     //   传的是**同一条**行 ⇒ 高亮与结算不会分叉。
                                                     CardDef.ConditionSpec structuredCondition = null,
                                                     // 🆕 2026-10-23（引擎线 · 战斗审计 `B-17`）：**这条 op 本身**。
                                                     //   只有 `spec.Subjectless` ∧ 没有施放者 ∧ 没有点目标那一格用得上
                                                     //   （读 `op.SubjectTargetsAffected`，原版 `abilityLogic.targetCriteria`）。
                                                     //   传 `null` = 不知道是哪一条 ⇒ 照旧走「己方全体」近似并出声。
                                                     EffectOp op = null)
        {
            var list = new List<UnitState>();
            if (spec == null) return list;

            // 🆕 2026-09-16：**目标只在手牌**（`all friendly troops in hand` / `a random Beast in
            //   your hand`）—— 场上**不加**（见 `EffectTargetSpec.HandOnly`）。
            //   手牌那半由 `DoGive` 里 `GrantHandBuffForTargets` 登记，**不走这里**。
            if (spec.HandOnly) return list;

            // 施放者：调用点一律传 `null`（历史原因），所以从这里回落到上下文的
            // `ActingUnit`（由 `ResolveOne` 在结算每一条效果**之前**设好、结算完恢复）。
            // 「相邻」的 `Self` 锚点要用它 —— 见 `ResolveAdjacentAnchor`。
            if (source == null) source = ctx.ActingUnit;

            // ---- 🆕 2026-10-24（引擎线 `G5`）：原版 `ta = 200 / 201` 的 `referencedCards` ----
            // 判据（第一权威 = `d:/2/tools/decomp_full/`，现读）：
            //   · 分派：`AbilityLogic__GetTargets.c:770-781`（`iVar6 == 0xc8` = 200）⇒ 把
            //     `actingCard.referencedCards`（`+0x278`）**整表 AddRange**，然后 `goto` 出口
            //     ⇒ **不过筛、不 Trim、不看 targetSelectMethod**；
            //   · `:784-806`（`iVar6 == 0xc9` = 201）同上，但**只把 `IsInPlay` 的加进去**
            //     （判据 `CardScript__IsInPlay.c:7`：`cardState ∈ {2,15,3,17}`）。
            //   · 写入方 = 能力 `380 setReferenceCard`（见 `SetReferenceCards`）；
            //     **不是**卡 SO 字段 ⇒ 数据面为空是正常的（`资料/普查产出_1024/C5_剩余判据普查.md` §3）。
            // 🔴 **2026-10-24 就地订正（铁律 5 · 只读代理 `C3` 现核）**：上面把写侧口径写成了
            //   `actingCard.referencedCards` —— **基对象不对**。原版 `AbilityLogic__PlayAbility.c:2128-2160`
            //   写的是 **`abilityData.thisCard`（`AbilityData + 0x10`）**，再由它取 `.referencedCards`
            //   （`CardScript + 0x278`）；`actingCard` 只是**大多数情况下恰好就是同一张**。
            //   ⇒ 读侧那两句（`GetTargets.c:770-781` / `:784-806`）不变；**引用表可以跨手牌/场上/墓地**，
            //   而我们 `UnitState.ReferencedCards` 只装 `UnitState`（见 `UnitState.cs:642` 的如实标注）。
            // 🔴 **位置必须在最前**：原版这两档**完全绕开** `targetCriteria` 的其它字段
            //   （与 `ta=240` 同一形态），所以文本解析出来的 `Side/Kind/Count` 一个字都不该参与。
            // ⚠️ **按 `op.SubjectTargetsAffected` 分派**（这是本仓 op ↔ `targetsAffected` 的唯一镜像，
            //   见 `EffectText.AssignAbilityTargets`）；`-1` = 读不到 ⇒ 走下面对文本 spec 的那几条老路。
            if (op != null && (op.SubjectTargetsAffected == 200 || op.SubjectTargetsAffected == 201))
            {
                bool inPlayOnly = op.SubjectTargetsAffected == 201;
                if (source == null)
                {
                    // 原版写的是 `actingCard.referencedCards` —— 没有施放者就无处可读。
                    // 手牌打出的战术卡在**本结构里**没有载体（见 `UnitState.ReferencedCards` 的 ⚠️），如实出声。
                    ctx.Log($"（`ta={op.SubjectTargetsAffected} referencedCards`：**没有施放的那张牌**"
                          + "（`actingCard` 为空）⇒ 目标表为空。⚠️ 手牌卡这条载体本批**没做**，"
                          + "见 `资料/普查产出_1024/G5_裸Deal与ta200.md` §件2）");
                    return list;
                }
                for (int i = 0; i < source.ReferencedCards.Count; i++)
                {
                    var u = source.ReferencedCards[i];
                    if (u == null) continue;
                    // 201 = 只留 `IsInPlay`（原版 `CardScript.IsInPlay`；我们的等价物见 `IsUnitInPlay`）。
                    // ⚠️ 200 **一律不筛** —— 包括**已经离场**的那些（原版是整表 AddRange）。
                    if (inPlayOnly && !IsUnitInPlay(ctx, u)) continue;
                    list.Add(u);
                }
                ctx.LastTargets.Clear();
                ctx.LastTargets.AddRange(list);
                if (list.Count > 0) ctx.LastTarget = list[0];
                ctx.Log($"（`ta={op.SubjectTargetsAffected} referencedCards`："
                      + $"「{source.Name}」的引用表 {source.ReferencedCards.Count} 张"
                      + (inPlayOnly ? "，只留还在场上的" : "（整表）")
                      + $" ⇒ 目标 {list.Count} 张（{Names(list)}）—— 判据 `AbilityLogic__GetTargets.c:"
                      + (inPlayOnly ? "784-806" : "770-781") + "`）");
                return list;
            }

            // 指代上一条效果的目标（`it` = 一个 / `them` = 一批）
            if (spec.Side == "prev" || spec.Kind == "prev")
            {
                // 🔴 **2026-10-19（`A1426`）：判据 = 「活着的对象」∨「还在棋盘上」。**
                //   原版目标解析层（`AbilityLogic.GetTargets.c`）**不按生死筛**（R1 §`A1426`），
                //   挨不住它的是**加效果那一步**的 `cardState` 闸。`Sacrifice:` 那一刻
                //   单位 `Health <= 0` 但 **`cardState` 仍是 `2 inPlay`**（`CheckIfDead.c:108-110`
                //   的触发排在状态变更之前）⇒ 代词 `it` 指的是它，原版拿得到。
                //   ⚠️ **`IsAlive` 那一半【必须留着】**：代词目标也可能是**已经回手 / 在牌库里**的
                //   卡（`Return a friendly troop to your hand. … give it Flank`，
                //   `UM110 Fall Back` 实测）—— 它们不在棋盘上、但对象还活着，
                //   原版把效果挂到**那张手牌**上（`SetupCardInHand` 那一族）；只查棋盘会把
                //   这条已发布的效果静默丢掉（2026-10-19 全量自检红过：`S14` 的
                //   「`give it Flank` 真的给了目标」）。
                //   ⇒ 口径 = **`IsAlive || IsUnitInPlay`**（严格是旧行为的超集：只多出
                //   「血 ≤ 0、但还在场」这一档）。
                if (spec.Count == 0)
                {
                    // `them` —— 上一条效果**影响到的整批**（谁被打了就治谁，谁被部署了就给它加）
                    foreach (var u in ctx.LastTargets)
                        if (u != null && IsUnitInPlayOrDying(ctx, u)) list.Add(u);
                    return list;
                }
                var last = ctx.LastTarget;
                if (last != null && IsUnitInPlayOrDying(ctx, last)) list.Add(last);
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
            //
            // 🔴 **2026-10-10（第十四会话 · `A1407`）：`source` 那一档【不看 `IsAlive`】。**
            //
            // **改前怎么错的**：这里写的是 `source != null && source.IsAlive`。而 `IsAlive` = `Health > 0`
            //   （`UnitState.cs`）。原版把**献祭**那一跳排在**救回**（`UseSurvivor`）之前
            //   （`CardScript__CheckIfDead.c:152-158` 在 `:160-165` 之前）⇒ 触发 `Sacrifice:` 正文那一刻，
            //   单位身上还是 `Health = -3` ⇒ `IsAlive == false` ⇒ **一路落到下面的「己方全体」兜底**：
            //   `Sacrifice: Gain +1 Attack` 的 +1 加到了**督军**身上（离线实测：督军 `atk 2 → 3`、
            //   施放者自己仍是 2）。**而且那一支一行日志都没有**（注释写着「并把这件事说清楚」——
            //   话在那儿，代码不在）。影响面**不止 `sacrifice`**：**任何「死后才触发的触发」+ 无主语正文**
            //   都走这一支（`Backlash` 族同形）。
            //
            // **判据（`self` / `actingCard` 那一档不判生死）—— 原版反编译实证**：
            //   `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`（`TargetsAffected` 的
            //   **唯一** resolver）里，这两档是**直接放进结果列表、没有任何存活/在场判断**：
            //     · `:184-191` `if (iVar6 == 0x14)`（`self = 20`）⇒ `FUN_180002430(list, param_3)` = 加 **thisCard**
            //     · `:761-769` `if ((iVar6 == 0x50) || (iVar6 == 0x28))`（`actingCard = 80` / `attacker = 40`）
            //       ⇒ `FUN_180002430(list, param_4)` = 加 **actingCard**
            //     两处都 `goto LAB_180818485`（`:1378` 的共同返回口）—— **闸门在它们后面**，
            //     不会把这张卡再筛掉。（枚举值 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/
            //     TargetsAffected.cs`；调用点 `:184` 的 `iVar6` 就是 `criteria.targetsAffected`。）
            //   ⚠️ **判据的另外一半仍缺**：「一条**无主语正文**在原版数据里配的 `targetsAffected`
            //     到底是 `self` 还是别的值」是**服务端数据**（本地无卡、无卡面可核）
            //      ⇒ **不写成「原版一定打自己」**，只写成「**这一档不该按生死换语义**」。
            //      留痕见 `资料/普查产出_第十四会话/W_SurvivalSacrifice族.md` §三 + 本笔报告。
            //
            // ⚠️ **为什么可以去掉 `IsAlive`**：`Subjectless` 这个旗标的口径本来就是
            //   「**有施放者就是它自己**」（`EffectTargetSpec.Subjectless` 的 doc 写的正是这句话），
            //   那个 `IsAlive` 是**实现时多出来的一道闸**，不是这一档的判据。
            //   ⚠️ 用 `Subjectless` 的动词只有「给/加/减/治/翻倍/回手/受这份伤/触发能力」这几族
            //   （`EffectText` 的 9 个产出点），**没有一个会把单位移出棋盘**；`return` 的
            //   「施放者已离场」那一支在 `DoReturn` 里**更早**就接管了（见那边的注释）⇒ 不受影响。
            //   🔴 **但要如实说清另一半（离线实测）**：结算层还有一条**一致约定** ——
            //   10 个动词 handler 的逐目标循环都写着 `if (t == null || !t.IsAlive) continue;`
            //   ⇒ 本支选出的那个（已死的）施放者**会被它们丢掉**，数值上看不见（`Backlash` 族尤甚）。
            //   **这是两件事**：本笔修的是「**不要静默换成己方全体**」（目标选对了），
            //   那 10 道闸**没动**（放开的风险见下面那段日志的注释）—— 照样**如实出声**。
            if (spec.Subjectless)
            {
                if (source != null)
                {
                    // 🔴 **2026-10-10（`A1407`）：「施放者已死」这一格必须【出声】**。
                    //
                    //    本仓的结算层有一条**一致约定**：**一律不给非活体（`Health ≤ 0`）加效果** ——
                    //    10 个动词 handler 的逐目标循环里都写着 `if (t == null || !t.IsAlive) continue;`
                    //    （`DoGive` / `DoDeal` / `DoHeal` / `DoStun` / …）。
                    //    ⇒ 施放者已死时，下面那句 `list.Add(source)` 交给它们之后**什么都不会发生**。
                    //    **只出声、不换语义**：⛔ 这里**不改成**「退回己方全体」（那正是改前的病），
                    //    ⛔ 也不去放开那 10 道闸（`Give +N Health` 会把 `Health = -3` 的尸体推成
                    //    `> 0` ⇒ **复活一个不该复活的**）。该不该放开 → 本笔报告里列给调度台裁。
                    //    ⚠️ **今天的可观测面**：`cards_engine.json` 1126 张里带
                    //    `Sacrifice:` / `Survivor:` 的卡 **0 张** ⇒ 这条日志只在
                    //    「死后才触发的触发」+ 无主语正文那种形状上出现，不会刷屏。
                    if (!source.IsAlive)
                        ctx.Log($"（「{spec.Raw}」：目标是**施放者自己**「{source.Name}」——"
                              + $"而它此刻生命 {source.Health} ≤ 0 ⇒ 按本仓「不给非活体加效果」的"
                              + "既有约定，**这一条对它不会生效**。如实报出来，⛔ 不改成「加给全队」）");
                    list.Add(source);
                }
                // 战术卡没有施放者，但**玩家点了一个目标**（`chosen`）—— 那就是这张卡说的「谁」。
                // ⚠️ 排在「己方全体」**之前**：`Beacon of Faith` 那类卡的第一次「whose」是
                //    `your units`（`PickTarget` 返回 null ⇒ `chosen` 也是 null）⇒ 仍然落到己方全体 ✓
                //    行为不变；而真点了目标的卡（`Luminous Strike` 那种）就不会再打全体。
                // 🔴 **2026-10-24（引擎线 `G5`）：这一支要用 `chosen`，必须【这一条 op 自己的
                //    `targetsAffected` 就是「用调用方给的那个目标」】** —— 判据（现读
                //    `AbilityLogic__GetTargets.c:192-207`）：整张函数里**只有** `iVar6 == 0x1e (30 target)`
                //    与 `0xdc (220)` 两支会去读 `param_5`（= `targetCard`）并把它塞进结果表，**其余每一档
                //    都把 `targetCard` 忽略掉**。⇒ 一条**无主语** op 若不是这两档，玩家点的那个目标
                //    **不是它的目标**（`ta=-1` 读不到时仍按旧行为放行 —— 宁可不改也不猜）。
                //    🔬 **这条闸是实测逼出来的**：改前 `GOF103 Da Hunt is On`（`Deal 2 damage.` +
                //    `The next Beast you play gains Flank.`，第二条 `subjTa = 0 any`）把 Flank 加到了
                //    **玩家点中的那个敌人**身上（`play` 基线整表只有这 1 张变，见
                //    `资料/普查产出_1024/G5_裸Deal与ta200.md` §件1）。加闸后回到「己方全体」的既有近似。
                else if (chosen != null && chosen.IsAlive
                         && (op == null || op.SubjectTargetsAffected < 0
                             || op.SubjectTargetsAffected == 30 || op.SubjectTargetsAffected == 220))
                    list.Add(chosen);
                else
                {
                    // 🔴 **2026-10-23（引擎线 · 战斗审计 `B-17`）：先按原版 `targetsAffected` 取。**
                    //   判据 = `AbilityLogic__GetTargets.c`（`+0x10` 分发）；归位两道闸门 →
                    //   `EffectText.AssignAbilityTargets`；值挂在 `op.SubjectTargetsAffected`。
                    //   ⚠️ **只认 `120 friendlyHero`**（`:721-737` → `GetHero(actingCard.isPlayer)` =
                    //   **施放方督军**）—— 语义与卡面「your Warlord」逐字对得上，且它在我们模型里
                    //   就是一个**具体的单位**。其余值**不猜**：
                    //     · `20 self` = **写着这条 ability 的那张卡**（战术卡/附魔卡自己，不是场上单位）；
                    //     · `80 actingCard` / `40 attacker` = 触发方，战术卡这条路上根本没有；
                    //     · `10 allFiltered` / `0 any` = 要按 `friendlyUnits/enemyUnits/includeHero/
                    //       includeMinions` 现筛（我们这一列**没落那些位** ⇒ 拿不准，宁可不改）。
                    //   读不到（`-1`）⇒ 退回既有近似 + 出声（下面那一段）。
                    int ta = op != null ? op.SubjectTargetsAffected : -1;
                    if (ta == 120)
                    {
                        var hero = ctx.Players[owner].Board[BoardSpec.WarlordSlot];
                        if (hero != null && hero.IsAlive) list.Add(hero);
                        ctx.Log($"（「{spec.Raw}」：**没有施放者、也没点目标** ⇒ 按原版"
                              + " `targetsAffected = 120 friendlyHero` 落到**己方督军**"
                              + $"（{Names(list)}）—— 判据 `AbilityLogic__GetTargets.c:721-737`）");
                        return list;
                    }
                    // 没有施放者、也没点目标：**退回既有近似**（己方全体），并把这件事说清楚 ——
                    // 静默换语义比报一行日志糟得多。
                    // 🔴 **2026-10-10（`A1407`）：这句话以前只写在注释里、代码里一行都没有**
                    //    （那一支是**唯一**没有任何日志的降级路）—— 现在真出声，并报出**具体收进了谁**。
                    AddSide(list, ctx.Players[owner], false, spec.Kind == "troop", -1);
                    ctx.Log($"（「{spec.Raw}」：**没有施放者、也没点目标** ⇒ 按既有近似落到**己方全体**"
                          + $"（{list.Count} 个：{Names(list)}）—— 这条近似不是原版语义，如实报出来；"
                          + $"原版这条 logic 的 `targetsAffected` = {ta}（{TargetsAffectedName(ta)}）"
                          + (ta < 0 ? "，**读不到**" : "，本版没接这一档") + "）");
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

            // ---- 🆕 `… attacked [by this unit]` —— **被这一下打到的那个**（2026-09-14 A5 批 3）----
            // 目标从 `ctx.LastTargets` 取 —— 触发方（`RuleCore.DeclareAttack` 攻击结算之后那块）
            // 把这一下的被打者从 `seed:` 种了进去，和 `spec.Deployed` 借的是**同一套代词机制**。
            // ⚠️ **筛选条件在这里补判**：卡面写的是 `any enemy troop **with Armour** attacked by this unit`、
            //    `… **with Hunt Mark** attacked`（`Arjac Rockfist`）—— 不带条件一律摧毁就是**打得比卡面宽**，
            //    而且卡面不打 `*`（句子解析得好好的）。
            if (spec.AttackedBySelf)
            {
                var atkCrit = CardCriteria.FromTarget(spec);
                foreach (var u in ctx.LastTargets)
                {
                    if (u == null || !u.IsAlive) continue;              // 已经死了的不再处理
                    if (atkCrit != null && !atkCrit.Matches(u.Card)) continue;
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
            ApplyTargetFilters(ctx, pool, spec, structuredCondition: structuredCondition);
            // 🆕 v25（2026-10-24 · 执行代理 `W2` · `C5` Q4）：再按原版 `(ab,lg)` 那条 logic 的
            //   **其余 36 格** criteria 筛一遍（`op.TargetCriteriaSet` + `op.TargetCondition` 拼回一个对象）。
            //   ⚠️ 排在 `Adjacent` **之前** —— 原版相邻目标的候选也要过同一份 criteria
            //   （`pool.Contains(u)` 那一段读的正是筛完的池）。
            ApplyTargetCriteriaFilter(ctx, pool, op, owner, source, chosen, quiet: false);

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
                var anchor = ResolveAdjacentAnchorFromPool(ctx, owner, spec, source, chosen, pool);
                int ap = -1, aslot = -1;
                // 🔴 **2026-10-26（批次 6 · `G14` ④）：锚点【已死】也要算；残骸换过对象就按 `Instance` 映射。** 判据（第一权威，现读）= `MinionManager__GetAdjacentUnits.c:52-70`：它在 `+0x60`（己方部队列表）里**按引用找锚点**、取 `i-1`/`i+1` —— 死亡单位要等到 `MoveToCemetery` **之后**才离开那条列表 ⇒ 锚点刚死（`IsAlive == false`、同一拍还在棋盘上）时**原版照打邻居**。我们原来那句 `anchor.IsAlive` 把整条挡掉 = **静默空过**（4 张可复现：`ASH73 Whirling Death` · `Particle Whip` · `SAU56 Methodical Destruction` · `None Must Know`；反证 `SOR43 Cleansing Flames` 锚点活着时正常）。
                //   ⚠️ 第二道 = `RuleCore.BoardOccupantByInstance`（**只认 `Instance`**：锚点翻面成残骸后棋盘上是**另一个对象**；先例 = 眩晕那条 `A1224`）。⛔ 别按格号找 —— 同一批里别人真死会让连续列表内移。
                if (anchor != null && !FindSlot(ctx, anchor, out ap, out aslot)) { var occ = RuleCore.BoardOccupantByInstance(ctx, anchor); if (occ != null) FindSlot(ctx, occ, out ap, out aslot); }
                if (anchor != null && ap >= 0)
                {
                    // 🔴 2026-10-26（批次 8 · `B8_P`）：**锚点本体也过阵营筛** —— 原版对相邻候选逐个
                    //   `CheckIfMeetsCriteria`（`AbilityLogic__GetTargets.c:1100-1110`），锚点自己不是例外。
                    //   改前这一句无条件 `list.Add(anchor)` ⇒ 锚点一旦落错边（`GOF97` 晕自己 / `SOR43` 打自己人）
                    //   就**静默打到自己人**，卡面还打着解析成功的绿灯。
                    if (spec.AnchorInSet && AnchorOnSpecSide(spec, ap, owner)) list.Add(anchor);       // 卡面点名了锚点本体 ⇒ 它也在目标集里
                    var slots = new List<int>();
                    BoardSpec.AdjacentSlots(aslot, slots);
                    foreach (int sl in slots)
                    {
                        var u = ctx.Players[ap].Board[sl];
                        if (u == null || list.Contains(u)) continue;
                        // 🔴 2026-10-26（批次 8 · `B8_P`）：**邻居不看死活**（判据 = `MinionManager__GetAdjacentUnits.c:52-70`：
                        //   按引用在那一侧的**部队列表**里找锚点、取 `i±1`；死亡单位要等 `MoveToCemetery` 之后才离开列表
                        //   ⇒ 锚点刚死、邻居刚死，同一拍都还在列表里）。改前这一句多要了一道 `!u.IsAlive`
                        //   —— `G14` 只修了锚点那一半，邻居这一半漏着 ⇒ 同一拍刚死的邻居静默少打。
                        //   筛选（阵营/兵种/关键词/受伤/祈祷/结构化 criteria）照旧要过；`pool` 那一份是**活的**
                        //   （`AddSide` 跳过死单位）⇒ 死邻居在 `AdjacentNeighborCounts` 里按**同一套筛**单独判一次。
                        if (!AdjacentNeighborCounts(ctx, owner, spec, op, source, chosen, structuredCondition, u, ap, pool)) continue;
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
                            while (list.Count > keep) list.RemoveAt(ctx.GetRandomInt(0, list.Count));
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
                    // 🔴🆕 **2026-10-26（批次 6 · `G14` ④）：这一支不再静默** —— 原来只有一行 `ctx.Log`（玩家侧日志），而它记的是**我们的缺口**、不是局面事件 ⇒ 改走 `Debug.LogWarning`（引擎级告警；⛔ 不用 `ctx.Log`：那会改 `ctx.Events.Count`，动到 `NetProtocol.Fingerprint`，见 `DoStun` 那段同名提醒）。
                    //   ⚠️ 每种 (锚点档, 施放者) 只报一次（静态去重）—— 否则每个动作都刷一行，真问题反而看不见。
                    WarnAdjacentAnchorMissing(spec.Anchor.ToString(), source != null ? source.Name : null);
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
                // 挑法**不掷骰**（同一局必须可复现 —— 铁律：定死的规则不要改成随机）。
                // ⚠️ **并列规则分两档**（2026-10-24 `B7` 就地订正：这一句以前一律写「取槽号在前」）：
                //    · `-health` = 原版 `GetLowestHealthUnit` 那一支 ⇒ **督军优先，其次按进场序（`PlayOrder`）**
                //      —— 见下面那段判据；
                //    · 其余（`+attack` 等）= **没有原版判据**，仍按池序（= 格号序）取先出现者。
                // ⚠️ `attack` 取的是**近战攻击力**（`UnitState.Attack`）：卡面写的是裸 `attack`，
                //    而远程是单独一栏（`ranged attack`）；本版就这一张卡，按近战解，**如实记在这儿**。
                bool low = spec.PickMost.StartsWith("-");
                string stat = spec.PickMost.Substring(1).Trim();
                UnitState best = null;
                int bestV = 0;

                // 🔴🆕 **2026-10-24（引擎线 `B7` · B6 报告的「顺手发现 2」）：`lowest Health` 补两处口径差。**
                //
                // 判据（第一权威，本轮现读）= `d:/2/tools/decomp_full/BattleManager__GetLowestHealthUnit.c`
                // （分派 → `AbilityLogic__GetTargets.c:835-844` 的 `iVar6 == 0xf0 (240 lowestHealth)`；
                //  🔴 **2026-10-24（引擎线 `G5`）就地订正（铁律 5）**：这条注释原来写
                //    「裸 `Deal N damage` 走的正是 240，见 `EffectText.TryDeal` 的 `PickMost = "-health"`」——
                //    **作废**：裸 Deal 的原版 `ta = 30 (target)`（33/33 张现读），改后不再走这一支；
                //    这一支现在只服务**卡面明写** `with lowest Health` 的那几张（`ta=240`）。
                //    判据 → `资料/普查产出_1024/C4_ta240是否含督军.md` §三 + §六·2）：
                //   · **① 候选闸**（`:73-88` 收集 + `:122-128` 复核）= `IsInPlay` ∧
                //     **`!EnoughPendingDamageToDie`** ∧ `!isRemnant(+0x65)` ∧ cardType ∉ {20 Tactic, 40 Whispers}。
                //     我们改前只判 `IsAlive` ⇒ **残骸（`Health = 1`、`IsAlive` 真）会被挑中**；
                //     待死（`waitingToDie`，= 我们的 `ctx.IsPendingDeath`）也能被挑中。
                //   · **② 初始 best = 该侧督军**（`:100-109`，与 `BattleManager__GetHero.c` 逐句同形）⇒
                //     **并列时督军优先**、空池回落督军；改前 `foreach` + 严格小于 ⇒ 并列取**池里先出现**的那个
                //     （池序 = 格号序，督军在 `Board[4]`，同血的 1~3 号格部队会抢先）。
                //   · **③ 遍历序 = `allCardsInPlay` 追加序**（`:66` 的 `*(manager + 0x470)`）——
                //     同一张表就是我们的 `UnitState.PlayOrder`（见那个字段的 doc）⇒ ⛔ 不是格号序。
                // ⚠️ `cardType ∈ {20, 40}` 那一道**在我们这一侧是空闸**：`pool` 只由 `AddSide` 扫
                //    `PlayerState.Board` 得来，盘上只有单位/督军（`CardDef.Type` = `unit`/`hero`），
                //    没有 Tactic/Whispers 的位置 ⇒ **照判据记在这里、不另造一个卡类判据**。
                // ⚠️ 只动 `-health` 这一档：`+attack` / `-attack` 等的遍历序与并列规则**保持原样**
                //    （它们不是 `GetLowestHealthUnit` 那条判据，别顺手「统一」）。
                bool lowestHealth = low && string.Equals(stat, "health", System.StringComparison.OrdinalIgnoreCase);
                var pickPool = pool;
                if (lowestHealth)
                {
                    var cand = new List<UnitState>(pool.Count);
                    foreach (var u in pool)
                    {
                        if (u == null || !u.IsAlive) continue;
                        if (ctx.IsPendingDeath(u)) continue;                    // `IsInPlay` 假（状态 5）
                        if (u.IsRemnant) continue;                              // `+0x65 == 0`
                        if (RuleCore.EnoughPendingDamageToDie(u)) continue;     // 原版那道判死闸
                        cand.Add(u);
                    }
                    // 原版遍历序 = 进场追加序；`StableSortByPlayOrder` 把没取号的（`-1`）排到末尾。
                    RuleCore.StableSortByPlayOrder(cand, u => u.PlayOrder);
                    pickPool = cand;
                    // 初始 best = 该侧督军（**只在它自己也是合法候选时**预置 —— 否则会把被卡面筛掉的
                    // 督军当成目标，例如 `the enemy **troop** with lowest Health` 时督军根本不在池里）。
                    best = WarlordInPool(ctx, cand);
                    if (best != null) bestV = StatOf(best, stat);
                }

                foreach (var u in pickPool)
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
                    int k = ctx.GetRandomInt(0, copy.Count);
                    list.Add(copy[k]);
                    copy.RemoveAt(k);
                }
            }
            else
            {
                // ⚠️ **这一支 = 原版 `TargetSelectMethod.firstAdded(7)` 的行为**（从表头往后取 N 个），
                //    **不是**「随手写的一个退化支」—— 但它**很可能不是原版给这些卡配的那一档**：
                //    原版是在八档里 switch（`AbilityLogic__GetTargets.c:1171`），`default` 走 `random`；
                //    而**哪张卡配哪一档在服务端**，本地一条证据都没有。
                //    🔴 **2026-09-25 已裁决：保持现状、别改**（改 random 只是换一个猜法，而且
                //    `maxTargets < 1` 时原版**根本不裁**、会命中全体，同样是未知）。
                //    完整判据与「为什么不改」→ `资料/AI_原版反编译_0917.md` §十·7·b。
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
        /// <param name="chosen">🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）**：玩家**点定的那个单位**。
        ///   原版认的是这个对象（`action+0x20` 是 CardScript 引用）⇒ **侧由它自己带来**；
        ///   传 `null` 时退回「按格号 + 卡面 `PickSide` 解一侧」的老口径（逐字不变）。
        ///   ⚠️ 与 <see cref="RuleCore.PlayCard"/> 的 `chosen` 是**同一个对象**、同一条判据
        ///   （`RuleCore.PlayCardCore` 那一句 `PlayTactic(…, ChosenTargetSlot(…), chosen)`）。</param>
        public static int CanPlayTactic(BattleContext ctx, int p, int handIdx, int targetSlot,
                                       UnitState chosen = null)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            var ps = ctx.Players[p];
            if (handIdx < 0 || handIdx >= ps.Hand.Count) return RuleCodes.ErrBadHand;

            var inst = ps.Hand[handIdx];          // 第 7 行第 3 步：算费用要连**哪一份**一起给
            var card = inst.Card;     // 第 7 行第 2 步：手牌存实例，这里要的是模板
            if (card.IsUnit) return RuleCodes.ErrBadHand;         // 单位卡走 PlayCard，不是这条

            // 🔴 **2026-10-19：`cost < 0` = 原版 `manaCost = -1` 的哨兵 ⇒ 不可直接打出。**
            //    `Dark Pact of Fate/Blood/Excess/Resilience` 是**附魔模板**（用户 2026-10-19 裁定：
            //    它们没有费用），正常永远不该出现在手牌里；但**万一**被造进手牌，下面那句
            //    `CostOf(...) > ps.Energy` 挡不住 —— `CostOf` 末尾是 `Math.Max(0, v)`，
            //    会把 `-1` **clamp 成 0** ⇒ 被误判成「**免费可打**」。所以必须显式挡在费用判断之前。
            //    判据与码的语义见 `RuleCodes.ErrNotPlayable`。
            if (card.Cost < 0) return RuleCodes.ErrNotPlayable;

            // 🆕 2026-10-25（`X20`）：**隐藏附魔模板（`type = enchantment`）不可打出** ——
            //   同 `RuleCore.CanPlayCard` 那条（这里是 `PlayCard` 的分流入口，两条路都要挡）。
            //   为什么必须显式挡：本卡 `desc` 为空 ⇒ `EffectText.IsFullyParsed` 会说「没有不认识的句子」
            //   ⇒ 被当成「可打、但什么都不做」（花 2 费、静默）。判据 = `CardDef.IsEnchantment` 一处。
            if (card.IsEnchantment) return RuleCodes.ErrNotPlayable;

            // ✅ **防御卡走同一条路**（2026-09-13 第三十三轮）。
            //    以前这里直接 `return ErrUnimplemented`，理由是「引擎里除了组卡校验没有任何地方认识 `defence`」。
            //    实测：39 张防御卡的 desc **39/39 都能完整解析**，动词也都是已实现的那批
            //    （`Deal` / `Give` / `Heal` / `Deploy` / `Choose a … put it in your hand` /
            //     `Your next X costs N less` / `Refill` / `Create`）—— 所以那条理由不成立，撤掉。
            //    规则书 `:105`：防御卡就是「后手可打出的**特殊战术**」（战术大类，见 `rule_core.gd:4696`
            //    「防御卡=计策类」）。⇒ **它和战术卡共用这一份判据**，不另开一条。

            // 解析不了 → 明说不支持。**先于费用判断**：否则一张用不起的卡会报「能量不足」，
            // 把「本版不支持」误导成「再等等就能打」（单位卡那条注释里记着同一个坑）
            //
            // 🆕 2026-09-14 A4 批 4：**事件型手牌陷阱**（`When you play a Stratagem, …`，
            //    `Jammed Communications`）单列 —— 它的 desc 是**一句监听句**，**本来就不该**
            //    被打出时结算 ⇒ `IsFullyParsed` 对它为假是**对的**，但「打不出去」不是。
            //    和回合起止型（`At the end of your turn, …`）保持同一个口径：
            //    **能打出去 = 丢弃它**（`DoAtTurn` 早就是这么做的）。
            if (!EffectText.IsFullyParsed(card.Desc) && !EffectText.IsHandTrap(card))
                return RuleCodes.ErrUnimplemented;
            // 🆕 2026-10-19（`A1959`/`A1960`）：把 **card** 交给解析器 —— 原版 `choicesDefinition`
            //   要让「正文没写 choose 的选牌卡」也产出一条 `choosecard`（判据/闸门见
            //   `EffectText.InjectChoicesDefinitionOp`）。⚠️ 这里只影响 `PickTarget`/`PickSide`
            //   的候选 op 列表（注入的 choosecard 没有 `Target`，不会改变「要不要选目标」）。
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);

            if (CostOf(ctx, p, inst) > ps.Energy) return RuleCodes.ErrCost;

            // **要选目标**的才要求给格位；`Refill 2 Energy` / `Draw 2 cards` 这类随便放哪都行。
            // 「这一格能不能选」用**结算时那一份判据**（`IsLegalPick` → `AddSide`）——
            // 另写一份会出现「拖拽高亮说能选、打出去却空过」。
            var spec = EffectText.PickTarget(ops);
            // 🆕 2026-10-22（`W64` · `B-16`）：**选目标那条 op** 自己带的结构化条件行 ——
            //   和 `DoDeal` / `DoHeal` 等 handler 传给 `ResolveTargets` 的是**同一格**
            //   （`op.TargetCondition`）⇒ 高亮与结算同口径。
            CardDef.ConditionSpec pickCond = null;
            if (spec != null)
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ReferenceEquals(ops[i].Target, spec)) { pickCond = ops[i].TargetCondition; break; }
            if (spec != null)
            {
                // 🔴 **2026-10-18（第三会话 · `A985⑧` 第二步「拆码」）：这两句原来返回 `ErrSlot`。**
                //    `ErrSlot` **同时**盖着「那一侧满了」（原版 `NotEnoughRoom`，
                //    `RuleCore.CanPlayCard` 那一格）⇒ 调用方**从码上分不出因**。
                //    原版这一支是 `BattleManager__CanPlayCard.c:142-156`：`IsValidSpellTarget`
                //    扫完全场**一个合法目标都没有** ⇒ `Battle/Tips/NoTargetAvailable`。
                //    ⚠️ 下面两句是同一件事的两半（**没给格位 / 给了个越界格位** 与 **那一格没有合法候选**）：
                //      本卡**要求**选目标，而这一支判完就是「**没有可点的目标**」 ⇒ 两半同归这一档。
                //    ⚠️ 别和其它「目标类」码混：`B-31`（2026-10-23）之后，攻击那一侧的
                //      Vanguard/Stealth/Flying **各有各的码**（`ErrAttackBlock` / `…Stealth` / `…Flying`），
                //      而 `ErrTarget` 只剩「参数非法 / 技能目标非法」这一层意思。
                // 🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）：先认「玩家点定的那个单位」，再退回按格号解。**
                //   原版这一支认的是**目标对象本身**（`_ResolvePlayCardFromHand…:905-940` 把 CardScript 写进
                //   `action+0x20`）⇒ **侧由目标自己带来**；改前只拿格号反查一侧
                //   （`spec.Side == "enemy" ? 1-p : p`），而 `Side == "any"` 的卡 `PickTarget` 返回 null
                //   （它只认 own/enemy）⇒ 「点敌方的单位」被解成**我方同号格**（静默错打，19 张卡）。
                //   ⛔ 别把 `chosen` 只当提示：它是**唯一**带侧信息的入口（`PickedBySlot` 的 doc 写了兜底）。
                UnitState u;
                if (chosen != null) u = chosen;
                else if (targetSlot < 0 || !BoardSpec.IsValid(targetSlot)) return RuleCodes.ErrNoTargetAvailable;
                else u = PickedBySlot(ctx, p, spec, targetSlot);
                if (u == null || !IsLegalPick(ctx, p, spec, u, pickCond, card)) return RuleCodes.ErrNoTargetAvailable;
            }
            return RuleCodes.OK;
        }

        /// <summary>这一格在不在**候选目标列表**里（`troop` 类不含督军、敌方隐身单位不可选）</summary>
        static bool IsLegalPick(BattleContext ctx, int owner, EffectTargetSpec spec, UnitState u,
                                // 🆕 2026-10-22（`W64` · `B-16`）：**和结算那一侧同一行**结构化条件 ——
                                //   不传就是「高亮说能选、打出去空过」（`CanPlayTactic` 注释点名的老毛病）。
                                CardDef.ConditionSpec structuredCondition = null,
                                // 🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）**：这次选目标的那张牌 ——
                                //   只服务 `camouflage` 那一格（原版 `RawCardScript__IsValidTarget.c:16-40`
                                //   要求施放者 `cardType == 0x14`）。`null` ⇒ 按「非战术卡」处理（老口径里
                                //   伪装对所有来源都挡 ⇒ 传 null 的调用点与改前一致，零回归）。
                                CardDef casterCard = null)
        {
            var pool = new List<UnitState>();
            bool troopOnly = spec.Kind == "troop";
            bool tacticCaster = casterCard != null && !casterCard.IsUnit;   // `!IsUnit` = cardType 20（tactic/defence/enchantment）
            if (spec.Side == "own" || spec.Side == "any")
                AddSide(pool, ctx.Players[owner], false, troopOnly, -1, stealthGate: true);
            if (spec.Side == "enemy" || spec.Side == "any")
                AddSide(pool, ctx.Players[1 - owner], true, troopOnly, owner, stealthGate: true,
                        tacticCaster: tacticCaster);
            // ⚠️ **必须和结算那一侧读同一份筛**（2026-09-13 A4 批 2 修）——
            //    以前只筛阵营 + 「排不排除督军」，兵种 / 已受伤 / 正在祈祷 三道筛漏在这儿
            //    ⇒ **拖拽时高亮说能选、松手打出去却空过**（那正是 `CanPlayTactic` 注释点名的毛病）。
            //    现在两边都调 `ApplyTargetFilters`，**只此一处**。
            ApplyTargetFilters(ctx, pool, spec, quiet: true, structuredCondition: structuredCondition);
            return pool.Contains(u);
        }

        /// <summary>
        /// **一张「打出去还要点一个目标」的卡，现在能点谁** —— 给表现层的**两段式手势**用
        /// （🆕 2026-10-25 · 完全复刻 · 执行代理 A · `R1` P0）。
        ///
        /// <para>**判据**：原版 `_ChooseUnitTarget_d__780__MoveNext.c:24-75` 在玩家出牌后
        /// `ShowPotentialTargets(card, mode=1)` —— 点亮的就是「这张卡这一下的合法目标」；
        /// 点中之后 `_ResolvePlayCardFromHand…:905-940` 把它写进 `action+0x20`，结算按它走。
        /// ⇒ 表现层要点亮/校验的必须是**结算时那一份判据**（本仓红线：高亮与结算不许分叉）。</para>
        ///
        /// <para>⚠️ 与 <see cref="CanPlayTactic"/> 用的是**同一条链**
        /// （`EffectText.PickTarget` → `AddSide` → `ApplyTargetFilters`），
        /// 只是把「问一张卡」包装出来给表现层用（`IsLegalPick` 是私有的，别在表现层另写一份）。</para>
        /// </summary>
        /// <returns>空表 = 这张卡**不需要**选目标（或没有合法候选）⇒ 调用方按「直接打出去」处理。</returns>
        /// <param name="asIfOnBoard">
        /// 🆕 **2026-10-26（完全复刻批次 4 · 执行代理 `F2`）：把「这张牌落地之后那个单位」也算进池子。**
        ///
        /// <para>🔴 **判据**：原版这一档的**次序是「先落位、再算目标池」** ——
        /// `BattleManager__NeedToChooseMinionTarget.c:7-19` 只在**单位已经落地**之后被问；
        /// 真正挑人那一步 `_ResolvePlayCardFromHand_d__447__MoveNext.c:849-860` 走的
        /// `IsThereValidUnitTarget` 扫的是**落位之后**的棋盘。
        /// 我们这一版的形状是「点完目标才落位」（见 `BattleDriver` 那段 ①），
        /// 所以算池子时**待落地那张自己不在棋盘上** ⇒
        /// ① 只指友军的卡漏掉「可以指自己」这一档；
        /// ② 最坏一格 = 它是唯一合法目标 ⇒ 池空 ⇒ **我们根本不问**（原版会问）。
        /// 现读卡池：`targeted == 1` 的可指友军单位卡 **11+ 张**（`AM70` / `DA5` / `GSC4` /
        /// `SOR22` / `SOR31` / `GOF80` …）。</para>
        ///
        /// <para>⚠️ **只影响「要不要问」这一层**：传进来的 `UnitState` 是**预览对象**
        /// （不是棋盘上的那一格），所以表现层**点不到它** —— 收口要在 `CardPresentation` 那侧
        /// （见 `报告/F2_第4轮.md` §三 的改动请求）。⛔ 这不是「多给了个候选」，
        /// 而是把判据从「落位前」搬回原版的「落位后」。</para>
        /// </param>
        public static List<UnitState> PlayTargetPool(BattleContext ctx, int owner, CardDef card,
                                                     UnitState asIfOnBoard = null)
        {
            var empty = new List<UnitState>();
            if (ctx == null || card == null) return empty;
            IReadOnlyList<EffectOp> ops = card.TriggerOps(KeywordTable.Rally);
            if (ops == null || ops.Count == 0)
                ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            var spec = ops != null ? EffectText.PickTarget(ops) : null;
            if (spec == null) return empty;

            var pool = new List<UnitState>();
            bool troopOnly = spec.Kind == "troop";
            // 🔴 2026-10-26（批次 9 · `E9ER`）：`camouflage` 只挡**战术卡来源**（原版 `RawCardScript__IsValidTarget.c:16-40`
            //   的 `施放者.cardType == 0x14`）—— 本函数的 `card` 就是那张要打出的牌 ⇒ 判据直接读它。
            bool tacticCaster = card != null && !card.IsUnit;
            if (spec.Side == "own" || spec.Side == "any")
                AddSide(pool, ctx.Players[owner], false, troopOnly, -1, stealthGate: true);
            if (spec.Side == "enemy" || spec.Side == "any")
                AddSide(pool, ctx.Players[1 - owner], true, troopOnly, owner, stealthGate: true,
                        tacticCaster: tacticCaster);
            CardDef.ConditionSpec pickCond = null;
            for (int i = 0; i < ops.Count; i++)
                if (ops[i] != null && ReferenceEquals(ops[i].Target, spec)) { pickCond = ops[i].TargetCondition; break; }
            // ⚠️ **先筛「棋盘上那批」**（老口径逐字不变），预览单位**单独跑一次同样的筛**再并进来 ——
            //    ⛔ 别把预览塞进 `pool` 再统一筛：`ApplyStructuredConditionFilter` 遇到「判不了」
            //    是**整条不筛**（它 `return` 掉整轮），预览若被判不了会把**棋盘那批的筛也一起废掉**。
            ApplyTargetFilters(ctx, pool, spec, quiet: true, structuredCondition: pickCond);
            if (asIfOnBoard != null && asIfOnBoard.IsAlive && !(troopOnly && asIfOnBoard.IsWarlord)
                && !pool.Contains(asIfOnBoard))
            {
                var probe = new List<UnitState> { asIfOnBoard };
                try { ApplyTargetFilters(ctx, probe, spec, quiet: true, structuredCondition: pickCond); }
                catch (System.Exception e)
                {
                    // ⛔ 不许把一条「高亮候选算不出来」升级成「这张牌打不出去」的崩溃：
                    //    预览对象不在棋盘上，个别依赖**格位**的条件可能判不出来 ⇒ 那就不并它，并**出声**。
                    ctx.Log($"（`PlayTargetPool` 的「落位后」预览筛目标时抛了 {e.GetType().Name}："
                          + $"「{e.Message}」⇒ 本次不把待落地那张算进候选）");
                    probe.Clear();
                }
                if (probe.Count > 0) pool.Add(asIfOnBoard);
            }
            return pool;
        }

        /// <summary>🆕 **2026-10-26（批次 4 · `F2`）**：<see cref="PlayTargetPool(BattleContext,int,CardDef,UnitState)"/>
        /// 的「手牌那一份实例」重载 —— 它自己造那个**预览** <see cref="UnitState"/>。
        ///
        /// <para>**为什么要这个重载**：调用方手上只有 `CardInstance`（手牌里那一份），
        /// 而池子的元素类型是 `UnitState`。`new UnitState(inst, false)` 是**纯构造**
        /// （没有 `ctx` 写入、不消耗实例号、不改 `inst`）—— 判据 = `UnitState.cs` 的那个构造函数体
        /// 只读 `card.Attack/RangedAttack/Health/Keywords` 并算 `Exhausted`/`CurrentAttackType`。</para>
        /// <para>⛔ 别用 `ctx.NewInstance(card)` 造预览：那条会**消耗一次实例号**并写
        /// `ctx.PlayingCard` 一类记账（`BattleContext.NewInstance` 的 doc），
        /// 而本函数在高亮时**每格调一次**（`IsLegalPlayTarget`）⇒ 会把实例号吹飞、还会污染「谁在出牌」。</para>
        /// <para>⚠️ `instance == null` ⇒ 退化成不补（老行为）。</para>
        /// </summary>
        public static List<UnitState> PlayTargetPoolAsIfDeployed(BattleContext ctx, int owner,
                                                                CardInstance instance)
        {
            if (ctx == null || instance == null || instance.Card == null)
                return PlayTargetPool(ctx, owner, instance != null ? instance.Card : null);
            UnitState preview;
            try { preview = new UnitState(instance, false); }
            catch (System.Exception e)
            {
                // ⛔ 不许静默失败：造不出预览就退回老行为，并且**说出来**。
                ctx.Log($"（`PlayTargetPoolAsIfDeployed` 造预览单位失败：{e.GetType().Name} "
                      + $"「{e.Message}」⇒ 本次按「落位前」的老口径算目标池）");
                return PlayTargetPool(ctx, owner, instance.Card);
            }
            return PlayTargetPool(ctx, owner, instance.Card, preview);
        }

        /// <summary>这一格是不是这张卡**现在**的合法目标 —— 判据 = <see cref="PlayTargetPool"/> 的成员判定。</summary>
        public static bool IsLegalPlayTarget(BattleContext ctx, int owner, CardDef card, UnitState u,
                                             UnitState asIfOnBoard = null)
        {
            if (u == null) return false;
            return PlayTargetPool(ctx, owner, card, asIfOnBoard).Contains(u);
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
                                       bool quiet = false,
                                       // 🆕 2026-10-22（`W64` · `B-16`）：结构化条件行（见 `ApplyStructuredConditionFilter`）
                                       CardDef.ConditionSpec structuredCondition = null)
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
                // 🆕 2026-09-14：`SubtypeFilter` 可以是 **`|` 分隔的枚举**
                //   （`a friendly Infantry or Beast` / `a friendly Battlesuit or Vehicle`）——
                //   命中**任何一个**即可。判据由 `EffectText` 那边写（`kw + " or " + 兵种词`）。
                var want = spec.SubtypeFilter.Split('|');
                pool.RemoveAll(u => u == null || u.Card == null
                                    || (u.Card.Subtype.Length > 0
                                        && !System.Array.Exists(want, w =>
                                               string.Equals(u.Card.Subtype, w.Trim(),
                                                             System.StringComparison.OrdinalIgnoreCase))));
                if (!quiet && pool.Count != before)
                    ctx.Log($"（按兵种筛「{spec.SubtypeFilter.Replace("|", " 或 ")}」：{before} → {pool.Count}"
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

            // ---- 取反的关键词筛（`all **other** enemies` —— 见 `EffectTargetSpec.NotKeyword`）----
            // 🆕 2026-09-16：`Blacksword Missiles` 的后半句。**判据不在这一层**：
            //   解析层 `Finish` 已经把「前半句的关键词筛」取反挂进来了，这里只负责照着筛。
            if (!string.IsNullOrEmpty(spec.NotKeyword))
            {
                int before = pool.Count;
                pool.RemoveAll(u => u == null || u.Card == null || u.Has(spec.NotKeyword));
                if (!quiet && pool.Count != before)
                    ctx.Log($"（按「排除带 {spec.NotKeyword} 的」：{before} → {pool.Count}）");
            }

            // ---- 卡名筛（`your Primaris Intercessor` / `all friendly Canoptek Scarabs`）----
            // 🆕 2026-09-16「按卡名指目标」的结算侧（解析侧见 `EffectText.TailCardName`）。
            // 🔴 判据 = **全等**（`CreatePool.Norm` 归一后相等），**不是「包含」** ——
            //    `Eliminator Sergeant` 的名字里也有 `Eliminator`，按包含匹配会让它自己命中自己
            //    （纪律写在 `CardCriteria.Name` 的注释里，两处共用同一个归一化函数）。
            // ⚠️ **不过滤「subtype 是空串」的卡**（兵种那条有一条「查不到就保留」的兜底）——
            //    卡名是卡的固有属性、不存在查不到的情况，而这里放行一张就是**打错人**。
            if (!string.IsNullOrEmpty(spec.NameFilter))
            {
                int before = pool.Count;
                string want = CreatePool.Norm(spec.NameFilter);
                pool.RemoveAll(u => u == null || u.Card == null
                                    || CreatePool.Norm(u.Card.Name) != want);
                if (!quiet)
                    ctx.Log($"（按卡名筛「{spec.NameFilter}」：{before} → {pool.Count}）");
            }

            // ---- `all **damaged** …` —— 只挑失去过生命的 ----
            // 判据**查不到**（🔴 `D26`，2026-10-19）：这是**原版 `targetCriteria` 的一个筛选位**、
            // 不是一段可指的方法 —— 两张表都搜过（`grep -rln "IsDamaged\|get_isDamaged\|lostHealth\|LostHealth" d:/2/tools/decomp_full/` ⇒ **0 命中**）。
            // ⚠️ 原来这里写「判据照**规格书** `rule_core.gd:1415`」—— 那份 `.gd` 是**我们自己的上一版复刻**、
            // **不是规格书**（`CLAUDE.md` 铁律 2 的 2026-09-18 更正）⇒ **降级成旁证**。
            // 可核的那一半 = **卡面原文**（`all damaged …` 这一族卡面上就是这个词）；
            // ⛔ 别把下面那一行当「照抄原版」读。
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

            // ---- 🆕 2026-10-22（`W64` · `B-16`）：原版 `TargetCriteria.conditionsFilter` ----
            ApplyStructuredConditionFilter(ctx, pool, structuredCondition, quiet);
        }

        /// <summary>
        /// 🆕 v25（2026-10-24 · 执行代理 `W2` · `C5` Q4 / `A2` Q7）：按**原版 `TargetCriteria`
        /// 的其他 36 格**筛目标池（判据 / join 规则 / 过滤次序 → `TargetCriteriaEval` 的文件头）。
        ///
        /// <para>**输入** = `op.TargetCriteriaSet`（`(ab,lg)` 那条 logic 的 36 格）＋
        /// `op.TargetCondition`（同 `(ab,lg)` 的 `cond`/`ex` —— 原版那两格就在同一个对象里）。
        /// **这就是 `C5` Q4 说的「拼回一个对象一次判」**，⛔ 不是两阶段。</para>
        ///
        /// <para>🔴 **判不了 ⇒ 整条不筛**（与 <see cref="ApplyStructuredConditionFilter"/> 同一纪律）：
        /// 筛掉一半再降级会让「哪些进了池」变成一半原版一半近似，说不清；整条不筛至少是
        /// 「这条 criteria 本版没生效」，而且**出声**（`StructuredConditionNotice`，同一 key 只喊一次）。</para>
        /// </summary>
        static void ApplyTargetCriteriaFilter(BattleContext ctx, List<UnitState> pool, EffectOp op,
                                              int owner, UnitState source, UnitState chosen, bool quiet)
        {
            if (op == null || pool == null || pool.Count == 0) return;
            var set = op.TargetCriteriaSet;
            // 🆕 2026-10-24（`W25` · `D1` 的顺手发现③）：**日志里的坐标要用「实际挂 criteria 的那一对」**。
            //   改前打的是 `op.AbilityIndex/LogicIndex` —— 那对字段由 `AssignAbilityIndices` 按
            //   `role="target"` 行归位，而 criteria 是在**另一个方法**里按 `(ab,lg)` 贴的
            //   ⇒ 在 `Exemplary Warrior` / `GOF74` 这类卡上它**恒 -1**，日志会指向不存在的行。
            string tcWhere = (op.TargetCriteriaAbility >= 0 || op.TargetCriteriaLogic >= 0)
                ? $"`(ab={op.TargetCriteriaAbility}, lg={op.TargetCriteriaLogic})`"
                : "`(ab=?, lg=?)`（这条 op 上没有 criteria 坐标）";
            if (set == null)
            {
                // `no-row` = v25 表里这一格本该有行却没有（数据坏了）。**出声一次**，不筛。
                if (op.TargetCriteriaGap == "no-row")
                    StructuredConditionNotice("targetcriteria|no-row|" + (op.Source ?? "?"),
                        $"「{op.Source}」这一条 op 的 36 格 criteria **在 v25 表里没有对应行**"
                      + $"（{tcWhere}）—— 目标池**不按结构化 criteria 筛**。"
                      + "判据 `CardDatabase.MapTargetCriteriaRows`；重跑工具/gen_cards_engine.py。");
                // 🆕 2026-10-24（`W25` · `D1` 的 R2）：**多 op 块 ⇒ 按位置贴没有判据 ⇒ 不贴**。
                //   这是「回到 v25 之前的行为」，不是静默：**出声一次**（同卡只喊一次）。
                else if (op.TargetCriteriaGap == "logic-gate")
                    StructuredConditionNotice("targetcriteria|logic-gate|" + (op.Source ?? "?"),
                        $"「{op.Source}」这一块正文里有**不止一条 op** —— 「第 j 条 op = 第 j 条 logic」没有判据"
                      + "（实测反例 `Exemplary Warrior`：卡面顺序与 `abilityLogic` 顺序相反）"
                      + " ⇒ 这一条**不挂** `targetCriteria`、目标池**不按它筛**。"
                      + "判据 `AbilityLogic__GetTargets.c` + `AssignAbilityIndices` 的 G3；"
                      + "⚠️ 这是「宁可不筛也不猜」的降级，不是「原版没有这一格」。");
                return;
            }
            int before = pool.Count;
            var kept = new List<UnitState>(pool.Count);
            foreach (var u in pool)
            {
                string why;
                bool? ok = TargetCriteriaEval.Matches(ctx, owner, set, u, op.TargetCondition,
                                                      source, chosen, out why);
                if (ok == null)
                {
                    StructuredConditionNotice(
                        "targetcriteria|" + (op.Source ?? "?") + "|" + op.TargetCriteriaAbility + "|" + op.TargetCriteriaLogic + "|" + why,
                        $"「{op.Source}」的 36 格 `targetCriteria`（{tcWhere}）"
                      + $"**本版判不了**：{why} ⇒ 这一条**不筛目标池**（⛔ 没有当成「不成立」、也没有筛空）。");
                    return;
                }
                if (ok.Value) kept.Add(u);
            }
            pool.Clear();
            pool.AddRange(kept);
            if (!quiet && pool.Count != before)
                ctx.Log($"（按原版 `targetCriteria` 36 格筛（{tcWhere}）："
                      + $"{before} → {pool.Count}）");
        }

        /// <summary>
        /// 🆕 2026-10-22（`W64` · `B-16` 目标过滤那一档）：按**原版结构化条件**筛目标池
        /// —— `MeetsConditionCriteria(candidate, cond, excludeCondition)`，取反在
        /// <see cref="MeetsUnitCondition"/> 里做（判据 → 那里）。
        ///
        /// <para><b>谁传进来</b>：**逐 op 的那一格** `EffectOp.TargetCondition`（由
        /// `EffectText.AssignAbilityIndices` 归位、四道闸门；生产调用点 = `ResolveTargets` 的
        /// `structuredCondition` 形参，`DoDeal` / `DoHeal` / … 传 `op.TargetCondition`，
        /// `IsLegalPick` 传**同一格**）。`null` = 这条 op 没有/读不到 ⇒ **不筛**。</para>
        ///
        /// <para>🔴 **为什么绝不许「按卡」套**（那会把整张卡的行贴到每一条 op 上）—— 三条反例，
        /// 2026-10-22 现读卡池，也正是 `TestW64OpAbilityTargetConditions` 钉住的东西：</para>
        /// <list type="bullet">
        /// <item>`TL46 Rapacious Hunger`（`Deal 3 damage to all enemies. For each one that dies,
        ///   heal 2 to a random friendly unit`）：`40 damaged` 那一行属于**后半句的治疗目标**；
        ///   按卡应用会把「全体敌人」的伤害池也筛成「已受伤」⇒ 少打一批敌人。</item>
        /// <item>`ASH73 Whirling Death`（`Deal 5 damage to a troop. If the target dies, deal 2 …`）：
        ///   `220/ex=0`（将死）那一行属于**后半句**；按卡应用会把 5 点伤害的目标筛成「血 ≤ 0」
        ///   ⇒ 我们的同步模型里**一个目标都没有**（卡直接不打）。</item>
        /// <item>`SW14 Wolf Priest`（两条治疗 ability，只有第一条带 `40`）：按卡应用会把第二条
        ///   （`Rally: Heal …`，原文没要求「已受伤」）也筛掉。</item>
        /// </list>
        ///
        /// <para>⚠️ 行里带的 `count`/`multi` 那几档**不是单个单位**的形状 ⇒ 由调用方别传进来；
        /// `MeetsUnitCondition` 判不了的值返回 `null` ⇒ 这里**不筛**（⛔ 不猜）并**出声**。</para>
        ///
        /// <para>⚠️ 行里带的 `count`/`multi` 那几档**不是单个单位**的形状 ⇒ 由调用方别传进来；
        /// `MeetsUnitCondition` 判不了的值返回 `null` ⇒ 这里**不筛**（⛔ 不猜）。</para>
        /// </summary>
        static void ApplyStructuredConditionFilter(BattleContext ctx, List<UnitState> pool,
                                                   CardDef.ConditionSpec row, bool quiet)
        {
            if (row == null || pool == null || pool.Count == 0) return;
            if (!RoleTakesTargetSubject(row.Role)) return;      // 不是「一个目标」的形状 ⇒ 不接
            int before = pool.Count;
            var kept = new List<UnitState>(pool.Count);
            foreach (var u in pool)
            {
                bool? ok = MeetsUnitCondition(ctx, u, row.Condition, row.Exclude);
                if (ok == null)
                {
                    // 判不了 ⇒ **整条不筛**（⛔ 不猜、⛔ 不空池）——但**必须出声**：
                    //   静默不筛 = 玩家看不出「这条条件本版没生效」（本工程红线）。
                    StructuredConditionNotice("unjudgeable-target|" + row.Condition,
                        $"结构化**目标**条件 `cond={row.Condition}`（ex={(row.Exclude ? 1 : 0)}）"
                      + "本版判不了 ⇒ 这一条**不筛目标池**（⛔ 没有当成「不成立」、也没有筛空）。"
                      + "能判的值 → `ConditionEval.KindOf` / 判不了的原因 → `ConditionEval.GapOf`。");
                    return;
                }
                if (ok.Value) kept.Add(u);
            }
            pool.Clear();
            pool.AddRange(kept);
            if (!quiet && pool.Count != before)
                ctx.Log($"（按原版结构化条件筛 `cond={row.Condition}`（ex={row.Exclude}）："
                      + $"{before} → {pool.Count}）");
        }

        /// <summary>
        /// 🆕 2026-10-22（`W64` · `B-16`）：**「这张卡有 `role=target` 的结构化条件行，却归不到
        /// 这条 op」⇒ 出声一次**。同一 `(卡, 原因)` 只喊一次（`StructuredConditionNotice` 去重）。
        ///
        /// <para>⛔ **不筛**的理由：条件是**逐 ability** 的（`cardAbilities[i].targetCriteria` /
        /// `abilityLogic[j].targetCriteria`）。归不到位时按**卡**套 = **打错人**，三条反例：
        /// `TL46`（`40` 属于后半句的治疗 ⇒ 会把「全体敌人」的伤害池也筛成已受伤）·
        /// `ASH73`（`220 = 将死` 属于「如果它死了」那一半 ⇒ 会让 5 点伤害**一个目标都没有**）·
        /// `SW14`（两条治疗 ability 只有第一条带 `40` ⇒ 会把第二条也筛掉）。
        /// 归位判据与四道闸门 → `EffectText.AssignAbilityIndices`。</para>
        ///
        /// <para>原因码（`EffectOp.TargetConditionGap`）：`blocks&lt;ab` = `desc` 的能力块比行引用的
        /// ability 少 · `fragment-multi-ability` / `fragment-multi-block` = 这是**触发正文片段**
        /// （`CardDef.AddTriggerOp` 逐条触发解析）而整卡不止一条 ability · `logic-gate` = `lg &gt;= 0`
        /// 但那个能力块里不止一条 op（「第几条 logic」与「第几条 op」对不上）· `ambiguous` = 同一
        /// `(ab,lg)` 匹配到多条行（⛔ 不挑一条）· `postcheck` = `220 + ex=0`（「将死」，**结算后**判定，
        /// 不是选目标的筛）· `row-unused` = 行挂的那条 ability 在 `desc` 里一句都没印。</para>
        /// </summary>
        static void NoteTargetConditionGap(BattleContext ctx, CardDef card, EffectOp op)
        {
            string why;
            switch (op.TargetConditionGap)
            {
                case "blocks<ab":
                    why = "`desc` 里的能力块比条件行引用的 ability 少（文字顺序不能当 ability 下标）";
                    break;
                case "fragment-multi-ability":
                    why = "这是**触发正文片段**，而卡上有多条 ability（片段属于哪一条读不到）";
                    break;
                case "fragment-multi-block":
                    why = "这是**触发正文片段**，而整张卡的 `desc` 不止一个能力块";
                    break;
                case "logic-gate":
                    why = "条件挂在某条 `abilityLogic` 上，而那个能力块里不止一条 op（对不上第几条 logic）";
                    break;
                case "ambiguous":
                    why = "同一个 `(ab, lg)` 匹配到多条条件行（不挑一条）";
                    break;
                case "postcheck":
                    why = "那一行是 `220 + ex=0`（「将死」）—— 它是**结算后**的判定（卡面「If the target dies」），"
                        + "不是选目标的筛（本仓由 `ConditionHolds` 的 `targetdies` 判）";
                    break;
                case "row-unused":
                    why = "条件行挂的那条 ability 在 `desc` 里一句都没印（或印出来的那几句解不出 op）";
                    break;
                default:
                    why = "原因码不认识（" + op.TargetConditionGap + "）";
                    break;
            }
            StructuredConditionNotice("targetgap|" + card.Id + "|" + op.TargetConditionGap,
                $"卡「{card.Name}」（{card.Id}）的 `role=target` 结构化条件行**归不到具体 op** —— "
              + why + $" ⇒ 「{op.Source}」这一条的目标池**不按结构化条件筛**（⛔ 不按卡套：那会打错人）。"
              + "要接这一档，需要生成器落「每条 ability 的正文区间 / effect 类型」（v16 之外的新列）。");
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
        /// <param name="chosen">🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）**：玩家点定的那个单位 ——
        ///   **唯一带侧信息的目标入口**（原版 `action+0x20` 是 CardScript 引用）。`null` ⇒ 退回按格号解，
        ///   见 <see cref="PickedBySlot"/> 与 `Side == "any"` 那 19 张卡的缺口说明。</param>
        public static int PlayTactic(BattleContext ctx, int p, int handIdx, int targetSlot = -1,
                                     UnitState chosen = null)
        {
            // 🔴 **2026-10-22（`W62/D5②`）：整条战术结算都在「来源卡 = 这张战术」的作用域里** ——
            //   `ResolveOps` 那条路传的是 `source = null`（`ActingUnit` 也是 `null`）⇒ 效果造出来的牌
            //   （`Create … in your hand` 那一族）只能靠这一格拿来源。改前靠的是 `PlayingCard`，
            //   而它**从不清空** ⇒ 非结算期造牌会盖成「本局最后打出的那张」。
            //   原版判据：`AbilityLogic__PlayAbility.c` 把 `AbilityData.thisCard`（= 这张战术）当 `creator`。
            //   ⚠️ save/restore 在 `using` **之外** —— 战术正文已入队，泵点在 `ActionScope.Dispose` 里跑，
            //      那一刻本字段必须还在（`ResolveOps(…, sourceCard: card)` 那条注释里说的就是这件事）。
            CardDef tacSource = ctx != null ? ctx.HandCardOrNull(p, handIdx) : null;
            CardDef prevSource = ctx != null ? ctx.PushCreationSource(tacSource) : null;
            try { using (RuleCore.BeginAction(ctx)) return PlayTacticCore(ctx, p, handIdx, targetSlot, chosen); }
            finally { if (ctx != null) ctx.CreationSource = prevSource; }
        }

        /// <summary><see cref="RuleCore.BeginAction"/> 包住的原文（2026-10-21 `T03·c` 批 B 接线）。</summary>
        static int PlayTacticCore(BattleContext ctx, int p, int handIdx, int targetSlot,
                                  UnitState pickedByPlayer = null)
        {
            int code = CanPlayTactic(ctx, p, handIdx, targetSlot, pickedByPlayer);
            if (code != RuleCodes.OK) return code;

            var ps = ctx.Players[p];
            var inst = ps.Hand[handIdx];          // 第 7 行第 2 步：打出的是手牌里的**那一份**
            var card = inst.Card;
            // 🆕 2026-10-19（`A1960`）：`card` 传进去 —— 正文没写 choose 的 `choicesDefinition` 卡
            //   也要发一条 `choosecard`（候选域 `specific`；闸门见 `EffectText.InjectChoicesDefinitionOp`）。
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            string side = EffectText.PickSide(ops);

            // 🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）：「点定的那个单位」优先，格号只当兜底。**
            //   判据：原版 `_ResolvePlayCardFromHand…:905-940` 把**目标 CardScript 引用**写进 `action+0x20`，
            //   结算时 `IsValidSpellTarget(target)` / `GetTargets(targetCard)` 都读那个对象 ⇒ **侧由目标自带**。
            //   改前这里只有 `ctx.Players[side == "enemy" ? 1-p : p].Board[targetSlot]` 一条路 ⇒
            //     · `Side == "any"` 的卡（现池 19 张，`PickTarget` 对它们返回 null、`PickSide` 因此给 `""`）
            //       无论玩家点哪一侧都读**我方**同号格 ⇒ **打不到对面**（玩家可见的错打）；
            //     · 我方那一格恰好空着时 `chosen` 直接是 null（效果按「无目标」空过）。
            //   ⇒ 有 `pickedByPlayer` 就用它；没有才走「按 `side` 读那一侧的同号格」（= 改前逐字不变）。
            //   ⛔ **不加**「我方空着就解对面」那种兜底：格号本身不带侧信息，两侧同号格都有人时
            //      两种解都「看着对」—— 猜一个就是静默错打；交给调用方把对象传进来（`PickedBySlot` 的 doc）。
            UnitState chosen = pickedByPlayer;
            if (chosen == null && targetSlot >= 0 && BoardSpec.IsValid(targetSlot))
                chosen = side == "enemy" ? ctx.Players[1 - p].Board[targetSlot]
                                         : ctx.Players[p].Board[targetSlot];

            int paid = CostOf(ctx, p, inst);
            // 🔴 2026-10-21（引擎线 `T03·b`）：走唯一的扣能量口（原版 `PayCardCost` → `UseMana`
            //   → `UseNormalEnergy`）—— 扣到 0 会触发 `SpentAllMana`。⛔ 别改回 `ps.Energy -= paid;`
            RuleCore.SpendNormalMana(ctx, p, paid, "打出「" + card.Name + "」");
            // `next …` 那族费用修正**用完即销** —— 和 `RuleCore.PlayCard`（单位那条）对称，
            // 两处都必须在**付费之后**调（见 `RuleCore.ConsumeOnceCostMods`）
            RuleCore.ConsumeOnceCostMods(ctx, p, inst);
            ps.Hand.RemoveAt(handIdx);
            // 🆕 2026-10-17（B19）：**记一笔「本局打出过的牌」**（候选域 `played` 的唯一来源之一；
            //    单位卡那一笔在 `RuleCore.PlayCard`）。
            //    ⚠️ 与 `RuleCore.PlayCard` **各写各的一笔**：那条只在单位分支里写，两处不会重记。
            //    🔴 **2026-10-18（`A974`）：落点下移到 `BroadcastWhen(Play)` 之后**（见下面那一处）。
            // 🆕 2026-10-17（B24）：**「本局打出过的隐秘」独立计数器** —— 与上面那张流水表
            //   **不是同一件事**（`BattleContext.PlayedCards` 记的是「打出过的牌」，
            //   尺寸/清点/用途都不同；原版也是两条各写一笔：`AddPlayCardAction` 与
            //   `AddSecretPlayed` **在同一段协程里**）。
            //   判据 = 反编译 `BattleManager._ResolvePlayCardFromHand__MoveNext.c:1267-1274`：
            //   `get_spellType(card) == 0x96`（**150 = Secret**）时 `PlayerManager.AddSecretPlayed`。
            //   ⇒ **打出才算、「生成」不算**（`CreateSecret` 那条路不写这一笔，与原版一致）。
            //   ⚠️ 位置与上一条表**同一个点**（校验过 / 付费过 / 手牌已拿走之后）——
            //      打不出去的牌一张都不记。字段与读点见 `PlayerState.SecretsPlayed`。
            //   ⚠️ **战术卡这一条写点覆盖 100%**：我们那 5 张隐秘卡全是 `type: "tactic"`
            //      （实测 `cards_engine.json`：`DA_Convoke_the_Circle` / `DA_None_Must_Know` /
            //      `DA_Obscure_Ritual` / `DA_Rites_of_Penance` / `DA_Smothering_Decree`），
            //      而 `RuleCore.PlayCard` 对**非单位卡开头就分流到这儿**（那句 `&& !ctx.Players[p].Hand[handIdx].Card.IsUnit) return PlayTactic(…)`）
            //      ⇒ 不会漏、也不会重记。
            if (card.Subtype == "Secret") ps.SecretsPlayed++;
            ctx.Log($"{ps.Name} 打出战术卡「{card.Name}」（{paid} 能）");
            // 🆕 2026-10-17（`A1547`）：**本回合打出第几张**在这一刻 +1。
            //    判据 = 原版 `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1390-1406`
            //    的 `ObscuredInt.op_Increment`（写 `BattleManager + 0x3D0`）。
            //    ⚠️ 与 `RuleCore.PlayCard`（单位支）**各写各的一笔**：两条路互斥、不会重记。
            ctx.CardsPlayedInTurn++;
            // 🆕 **2026-10-25（执行代理 `X2`）**：**成就出牌计数**（`Ach_12/54/87/89/97`）——
            //   与 `RuleCore.PlayCardCore` 那一笔**同一个口径、同一个点**（判据 §B-3 #3：
            //   「单位走 `PlayCard`、战术走 `PlayTactic` —— **两条都要**」）。
            //   ⚠️ 分类逻辑**不在这里重写** —— 唯一分类口 = `RuleCore.CardPlayedTagMask`。
            ctx.Tally.AddCardPlayed(p, RuleCore.CardPlayedTagMask(card, inst));
            // 战术卡到这儿才算真打出去 —— 事件要在**校验与扣费都过了之后**发
            // （和单位卡那条 `Play` 对称，日志/表现层两边都能看见战术卡）
            // 🆕 2026-10-18（`A985⑥①`）：**把「这张战术卡有没有指目标、指的是哪一格」记进事件**。
            //
            //    **判据（逐行读的方法体）**：`d:/2/tools/decomp_full/CemeteryManager__GetActionText.c`
            //    的 **case `10`**（`:85`）—— 「靶向」那一维的判别式与 `Ability` **同一条**：
            //      · `cVar2 = UnityEngine_Object__op_Equality(uStack_50, 0, 0)`，
            //        而 `uStack_50` = `*(undefined8 *)(param_2 + 10)` ⇒ **字节 0x28 = 目标引用**
            //        （⚠️ `param_2` 是 `int*`，别被变量名带偏）。
            //      · **引用非 null**（靶向）⇒ 按 `cVar3 = (char)*(param_2 + 6)`（= `isPlayer`）二选一。
            //      · **引用为 null** ⇒ 再按 `local_18._4_1_`（**字节 0x1C = 伏击位**）分「普通 / 伏击」两档，
            //        每档再按 `cVar3` 二选一；不分靶向的那两支落在 `LAB_18061b41d`。
            //      ⇒ **2(靶向) + 2(普通) + 2(伏击) = 6 条**，与
            //        `资料/普查产出_1018/G5_战斗本地化剩余.md` §3.2 那张表逐条对上。
            //    ⚠️ **没指目标就一个都不填**（保持 `-1` / `null`）。
            //    🔴 **判别式必须两个条件都满足**（`PickTarget(ops) != null` **且** `chosen != null`）：
            //       `CanPlayTactic` 只在**卡面要求选目标**时才校验 `targetSlot`
            //       （`spec != null` 那一支），而**不需要目标的战术卡也能被拖到某一格上打出**
            //       ⇒ 只看 `chosen != null` 会把「随便放的那一格」误记成「靶向」。
            //    🆕 **同一支还有第二维：「伏击」**（`BattleEvent.PlayAmbush`）—— 原版那条
            //       case `10` 的 6 档本来就是 `2(靶向) + 2(普通) + 2(伏击)`，伏击位 = 记录
            //       **字节 `0x1C`**（`local_18._4_1_`）。判别式与 `RuleCore.PlayCard` 那边
            //       **共用同一个函数** `RuleCore.PlaysFaceDown`（⛔ 别在这儿再写一份）。
            //       ⚠️ 战术卡实际上不会是伏击（伏击是单位的关键词）⇒ 这一维在本条上**恒 false**，
            //          但照样按同一句算 —— 「是不是伏击」是**卡的纯函数**，不按卡种分家。
            //    ⚠️ 这里用 `Emit(BattleEvent)` 那个重载而不是按字段那个 —— 后者**没有**这个形参
            //       （它的签名在 `BattleContext.cs`，本笔没在改动范围里）。
            //    🔴 **消费面**：`BattleDriver.BuildCardContext` 直读这两个字段算
            //       `targetIsPlayer = e.TargetPlayer == _me`（→ `WFModuleCollisions` /
            //       `WFModuleTransformModifier`），且 `_animfxLastEvent.TargetSlot` 还喂
            //       `targetIsWarlord` ⇒ 填了会改**靶向战术卡**那条特效的输入。
            bool targeted = EffectText.PickTarget(ops) != null && chosen != null;
            ctx.Emit(new BattleEvent
            {
                Kind = EvtKind.Play,
                Player = p,
                Slot = targetSlot,
                CardId = card.Name,
                TargetPlayer = targeted ? (side == "enemy" ? 1 - p : p) : -1,
                TargetSlot   = targeted ? targetSlot : -1,
                TargetCardId = targeted ? chosen.Name : null,
                PlayAmbush   = PlaysFaceDown(card),
            });
            // 🆕 `When you play a Stratagem, …` / `When your opponent plays a Stratagem, …`
            // （2026-09-13 第三十三轮）。**排在 `Emit` 之后、结算之前** ——
            // 「打出了」这个事实发生在效果结算之前；监听方看到的就是「对面刚打了一张计策」。
            BroadcastWhen(ctx, WhenEventKind.Play, p, card, null);

            // 🔴 **2026-10-18（`A974`）：这一笔**下移到广播之后**。**
            //    判据 = 原版同链：`BroadcastCardPlayed`（`BattleManager._ResolvePlayCardFromHand_d__447
            //    __MoveNext.c:1426`）**在** `CemeteryManager.AddPlayCardAction`（同文件 `:1503`）**之前**
            //    —— 顺序上原版是「先记账『本局打出过的隐秘』(`:1274` `AddSecretPlayed`)
            //    → 再广播『有人打出了一张牌』(`:1426`) → 最后记『本局打出过的牌』(`:1503`)」，
            //    与这里逐跳一致（`ps.SecretsPlayed++` 仍在上面原位）。
            //    位置 = 「校验过了、费用付掉了、手牌已经拿走」之后 ⇒
            //    **打不出去的战术卡一张都不会记上**（前面任一关没过就 return 了）。
            //    ⚠️ 与 `RuleCore.PlayCard` **各写各的一笔**：那条只在单位分支里写，两处不会重记。
            ctx.NotePlayed(p, inst, true);

            // 🆕 2026-09-14 A4 批 4：**事件型手牌陷阱**（`When you play a Stratagem, …`）——
            //    它的 desc 是**监听句**，不是打出时的效果 ⇒ 打出去只是**丢弃它**。
            //    ⚠️ **必须说出来**（红线：不许静默）—— 不能让玩家以为「打出去就触发了那个效果」。
            //    口径与回合起止型那条 `DoAtTurn` 完全一致。
            if ((ops == null || ops.Count == 0) && EffectText.IsHandTrap(card))
                ctx.Log($"{ps.Name}：「{card.Name}」是一张**手牌陷阱** —— 打出去只是**丢弃它**"
                      + "（进弃牌堆、不再害持有者）；它的效果只在**持有者手里**监听事件时触发");

            // ⚠️ `unresolved` 现在在 `PlayTacticBodyDeferred` 里建（正文整段入队，2026-10-22 `W62` · R2）。
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

            // ==========================================================================
            //  🔴 **2026-10-22（`W62` · R2 · 激励那一格）：战术卡【自己的正文】也入队。**
            //
            //  原版形状：`CardScript.TargetedSpellPlayed` 先 `OnTrigger` 激发励那条 ability
            //  （→ `PlayAbility` → `AddAutoActionToQueue`），再跑这张战术自己的正文
            //  ⇒ 两条 ability 各自入队，FIFO **先激励、后正文**。
            //  我们这边正文原来**内联**，而 `FireTriggerAt` 从 2026-10-21 起只入队 ⇒ 伤害抢在
            //  激励的队列动作前面 —— 规则书 `:212` 的「结算**前**」变成「结算**后**」
            //  （`TestStimulation` ①：5 血先挨 6 伤就死了，那 +5 血永远轮不到它）。
            //  ⚠️ 正文 + 它后面那一串收尾（600/605 两拍 · 进弃牌堆 · 巧技 · 第 3 张牌 · 典籍 ·
            //    `followAfterResolution`）**整体**搬进队列体 —— 它们本来就是「这次出牌结算」的后续，
            //    拆开会留下「弃牌堆先进去了、正文还没结算」那种中间态。
            //  ⚠️ 代价如实标着：效果落地推迟到泵点 —— 仍在**同一次 `PlayTactic` 调用内**
            //    （`PlayTactic` 自带 `ActionScope`，退出前一定跑完）⇒ 返回值 / 事件序列不变。
            //  ⚠️ 道取 `Instant`：现读 `cards_engine.json`（1127 张）全池只有 **4** 张卡有非默认
            //    `triggerPriority`（`spiritstone` / `artifice`×2 / `codex`，都是 3），
            //    **没有一个是 `stimulation`** ⇒ 激励那条触发也是 `Instant`，同道 FIFO 自然给出
            //    「先激励、后正文」。
            // ==========================================================================
            var _tacP = p;
            var _tacCard = card;
            var _tacOps = ops;
            var _tacChosen = chosen;
            var _tacTargetSlot = targetSlot;
            var _tacSide = side;
            var _tacInst = inst;
            var _tacPaid = new bool[ops != null ? ops.Count : 0];
            int _tacSpiritBefore = ps.SpiritStones;
            EnqueueAbility(ctx, ActionPriority.Instant, "tactic-body:" + card.Name,
                           () => PlayTacticBodyDeferred(ctx, _tacP, _tacCard, _tacOps, _tacChosen,
                                                        _tacTargetSlot, _tacSide, _tacInst,
                                                        _tacPaid, _tacSpiritBefore));
            return RuleCodes.OK;
        }

        /// <summary>
        /// <see cref="PlayTactic"/> 的**正文队列体**（2026-10-22 `W62` · R2 · 激励那一格抽出）——
        /// 原文逐字搬下来（`ResolveOps` → 突触 → 600/605 → 进弃牌堆 → 巧技 → 第 3 张 → 典籍 →
        /// `followAfterResolution`），⛔ 别在这里改次序。
        /// ⚠️ `paidFlags` / `spiritBefore` 由入队那一刻算好带进来（原版也是把上下文打包进 action）。
        /// </summary>
        static void PlayTacticBodyDeferred(BattleContext ctx, int p, CardDef card,
                                           List<EffectOp> ops, UnitState chosen, int targetSlot,
                                           string side, CardInstance inst, bool[] paidFlags,
                                           int spiritBefore)
        {
            if (ctx == null || card == null) return;
            var ps = ctx.Players[p];
            var unresolved = new List<string>();
            // 🔴 **2026-10-19（第五十七会话 `W57FIX2` · P3）：这一整段（含突触重复）都在
            //    「这次出牌结算」的作用域里** —— `ctx.CardPlaySource` 是 `DoFactionResource`
            //    取「任务点来源卡」的唯一通道（⛔ 不再读陈旧的 `ctx.PlayingCard`）。
            //    用 `try/finally` 还原：突触可能嵌套（邻居身上再触发别人的结算），
            //    而且 `PlayTactic` 之后马上还会跑巧技/典籍这些**不属于这次出牌**的段。
            var prevPlaySource = ctx.CardPlaySource;
            ctx.CardPlaySource = card;
            try
            {
                ResolveOps(ctx, p, null, ops, chosen, out unresolved, sourceCard: card, paidOut: paidFlags,
                           // 🆕 v29（`X6` · 口径乙）：战术 / 防御卡的正文 = 原版 `TriggerAbilities(0, …)`
                           // 那一跳（`RawCardScript__OnCardPlayedWithTarget.c:40`）⇒ `trigger 0` = `ThisCardPlayed`。
                           abilityFamilyCard: card, abilityFamilyTrigger: "ThisCardPlayed");

                // 🆕 2026-10-24（引擎线 `J2` · 战斗线 `W64` `B-30`）：**效果 470（`showCardsWindow`）**。
                //   原版那一条 `abilityLogic` 排在「正文（这批卡回到牌库 / 降费）」**之后**
                //   ⇒ 落点 = `ResolveOps` 跑完这一拍（此刻 `LastCreated` 里正是刚回去的那批）。
                //   ⚠️ 它是**纯展示**：只 `Emit` 一条事件，一个字都不改局面
                //   （判据 = `AbilityLogic__PlayAbility.c` case `0x1d6` 那条链上没有任何状态写入）。
                EmitShowCardsWindow(ctx, p, card);
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
            }
            finally { ctx.CardPlaySource = prevPlaySource; }

            // ---- 🆕 2026-10-19（`W57FIX2` · P1）：战术卡自己的 `600` / `605` 两拍 ----
            // 原版 `RawCardScript__OnCardPlayedWithTarget.c:59-84` 在**打出的那张牌自己**身上发；
            // 闸 = 那张卡真有对应动画行（`EmitCardTrigger` → `HasAnimTrigger`），
            // 「能力激活了没有」= 付费组收到钱（`paidOut`）或 `spendspirit` 真花了石头（ASH52）。
            EmitTacticAbilityUseTriggers(ctx, p, card, ops, paidFlags, spiritBefore);

            // 进弃牌堆（原版战术卡结算完就进弃牌堆）
            ps.Discard.Add(inst);      // 第 7 行第 2 步：进弃牌堆的是**那一份实例**（不是模板）
            // 判据收在 `ReportUnresolved` 一处（2026-09-16：它原来只有这一处真的读 `unresolved`，
            // 另外四处建了不读 —— 见那边的注释）。
            ReportUnresolved(ctx, "「" + card.Name + "」", unresolved);

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

            // ---- 🆕 `AbilityTrigger.PlayedThirdCardInTurn = 15`（账 `A1547`）----
            // 原版 `RawCardScript__OnCardPlayedWithTarget.c:105-112`：本回合第 3 张牌打出时，
            // 在**进攻卡自己身上**跑它 `trigger == 15` 的能力。战术卡也是「打出的一张牌」，
            // 所以这一支同样要接（⛔ 别只接单位卡那一路）。
            FirePlayedThirdCardInTurn(ctx, p, card);

            // ---- 🆕 典籍（`Codex`）的自动触发点：**打出之后的能量为 0**（2026-09-14 A5）----
            // 照我们自己的参考实现 `rule_core.gd:2183`（旁证、非权威）（战术打完那张牌的收尾）。
            // ⚠️ 排在巧技**之后** —— 那边也是把 `_check_codex` 放在战术那一支的**最后一行**。
            // 🔴 **2026-10-26（完全复刻批次 9 · 执行代理 `E9ER`）：补闸②** —— 原版 `CheckIfCodexTriggered.c:30-36`
            //   在 `manaLeft == 0` 之后还要判 `shouldCheckCost ∧ CurrentCost < 1`（**0 费卡不触发 Codex**）；
            //   本调用点原来只传 `(ctx, p)` ⇒ `playedCard == null` ⇒ 闸②**放行**（= 0 费的战术卡也会触发）。
            //   战术卡走的是 `PayCardCost` 那条路（`PayCardCost.c:30` 传 **1**）⇒ `shouldCheckCost: true`，
            //   与 `PlayCardCore` 末尾那个单位卡调用点**同一口径**。`inst` = 刚打出的那一份（`CostOf` 只读卡与 mods）。
            CheckCodex(ctx, p, inst, shouldCheckCost: true);

            // ---- 🆕 2026-10-19（战斗线 `T21` · 缺口 A）：`followAfterResolution`（320/325）的**结算尾部事件** ----
            // 原版：卡自己的 trigger-0 能力里带 `AbilityEffect.followAfterResolution = 320` ⇒
            //   `AbilityLogic__PlayAbility.c:2474` 把 `followAfterResolution` 动作（类型 58）压进队列；
            //   队列轮到它时 `BattleManager__ResolveFollowAfterResolution.c:380` →
            //   `CardScript__FollowUpResolutionPlayed.c:7` → `OnTrigger(0x19a = 410)`。
            // ⇒ **本拍的动画 = 那张卡的 trigger 410 行**（不是 0 行），所以关键词写 `FollowUpResolution`。
            // ⚠️ 位置 = **这张战术彻底结算完之后**（原版是队列里的下一条动作）。
            // ⚠️ 落点：有棋盘目标 ⇒ 用**目标那一侧**的格号（原版动画演在目标卡上；
            //   `EvtKind.Trigger` 的 `Player` 决定表现层取哪一侧的棋盘布局，所以目标在敌方时
            //   `Player` 要写 `1-p`）；没有（抽牌 / 手牌降费那一族）⇒ `-1`
            //   —— 原版那几条的 `AnimInfo.startPosOption` 是手牌/自身锚，我们的表现层今天没有
            //   手牌锚这条落点（报告 §四 如实记着「仍不可达」）。
            if (FollowUpSchedulerOf(card) == "ThisCardPlayed")
            {
                // 🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）**：这一拍演在**哪一侧**按「点定的那个单位归谁」算 ——
                //   原版 `followAfterResolution` 的那条动画挂在**目标 CardScript** 上（`isPlayer` 取目标的归属）。
                //   改前只有 `side == "enemy" ? 1-p : p` 一条：`Side == "any"` 的卡 `side` 恒 `""` ⇒
                //   一律落到**我方**（现在这类卡能打到对面了 ⇒ 不改就会「打在敌方、动画演在我方同号格」）。
                int fuOwner = chosen != null ? OwnerOf(ctx, chosen) : -1;
                if (fuOwner < 0) fuOwner = chosen != null ? (side == "enemy" ? 1 - p : p) : p;
                EmitFollowUpResolution(ctx, card, fuOwner,
                                       chosen != null ? targetSlot : -1,
                                       chosen != null ? chosen.InstanceId : 0);
            }
            return;   // 队列体没有返回值（原 `return RuleCodes.OK;` 留在 `PlayTacticCore`）
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
            // 邻居那一趟抽成闭包：可能跑**两遍**（`it applies the effect twice`，见下面那一段）
            System.Action runNeighbors = () =>
            {
                foreach (int s in slots)
                {
                    var nb = ctx.Players[ap].Board[s];
                    if (nb == null || !nb.IsAlive) continue;
                    if (!IsLegalPick(ctx, p, spec, nb)) continue;        // 过不了同一套筛选 ⇒ 跳过
                    var un2 = new List<string>();
                    ResolveOps(ctx, p, null, ops, nb, out un2, sourceCard: card,
                               // 🆕 v29（`X6`）：突触这一趟同样过家族闸 —— 原版每跑一遍都重新取目标
                               // （`CardScript__TargetedSpellPlayed.c:55-75` 的 `GetAdjacentUnits` + 逐条
                               //  `TargetCriteria` 复核）⇒ 候选 = 那个邻居（`nb`），不是原目标。
                               abilityFamilyCard: card, abilityFamilyTrigger: "ThisCardPlayed");
                    // 🔴 2026-09-16：原来 `un2` 建了不读（见 `ReportUnresolved` 的注释）
                    ReportUnresolved(ctx, "突触（在 " + nb.Name + " 上重跑的那一遍）", un2);
                    ctx.Log($"（突触：效果也在相邻的 {nb.Name} 上跑了一遍）");
                }
            };

            ctx.SynapseBusy = true;
            try { runNeighbors(); }
            finally { ctx.SynapseBusy = false; }

            // `When this unit triggers Synapse, …` / `When a friendly unit triggers Synapse, …`
            // （`Broodlord` / `Zoanthrope`）—— 触发者是**被选中的那个带突触的单位**。
            // ⚠️ 走 `BroadcastKeywordEvent`：突触**没有卡面正文**，`FireTriggerAt` 在「没写效果」
            //    时会提前返回、连广播都不发（和 `swarm` 同一个理由）。
            BroadcastKeywordEvent(ctx, WhenEventKind.Triggers(KeywordTable.Synapse), anchor);

            // 🆕 2026-09-16 `it applies the effect twice`（`TL30 Broodlord`）——
            //   ⚠️ **必须排在广播之后**：那份额度正是**广播时**由监听者挂到 `anchor` 上的
            //      （见 `DoExtraTrigger`），排在前面就永远读到 0。
            //   ⚠️ 这一步只是在**这一次**里多跑一趟邻居，不是「永久翻倍」。
            //
            // 🔴 **2026-10-22（`W62` · R2）就地订正（铁律 5）：光「排在广播之后」已经不够了。**
            //   触发改入队之后，广播**只入队**，挂额度那条监听器要到**出队时**才跑
            //   ⇒ 在这里就地 `TakeExtraTrigger` 永远读到 0（`Test` 里「相邻那个吃了两次」红）。
            //   ⇒ 消费本身也变成**一条队列动作**，排在广播那批监听器之后（同为 `ListenerLane`，FIFO 保证次序）。
            //   ⛔ 别挪回就地（那一格永远读不到额度）；⛔ 也别把额度改到 `DoExtraTrigger` 里直接重跑
            //      —— 那会绕开「这一次 Synapse 的锚点 / 目标过滤」这套上下文。
            var _synAnchor = anchor;
            System.Action _synRun = runNeighbors;
            EnqueueAbility(ctx, ListenerLane(card), "synapse-extra:" + (anchor.Name ?? "?"),
                           () => SynapseExtraDeferred(ctx, _synAnchor, _synRun),
                           actor: _synAnchor, requireActorInPlay: false);
        }

        /// <summary>
        /// <see cref="RepeatTacticOnAdjacent"/> 的**「再来一遍」队列体**（2026-10-22 `W62` · R2）。
        /// 原文就是原来那四行：取额度 → 有就再跑 `runNeighbors`（全程顶 `ExtraTriggerDepth`）——
        /// 只是**挪到出队时刻**跑，好让「挂额度」那条监听器先结算。
        /// </summary>
        static void SynapseExtraDeferred(BattleContext ctx, UnitState anchor, System.Action runNeighbors)
        {
            if (ctx == null || anchor == null || runNeighbors == null || ctx.IsOver) return;
            int extra = RuleCore.TakeExtraTrigger(ctx, anchor, KeywordTable.Synapse);
            if (extra <= 0) return;
            ctx.Log($"（突触：`{anchor.Name}` 的效果**再来一遍** —— `it applies the effect twice`）");
            ctx.SynapseBusy = true;
            try { RuleCore.FireExtraTriggers(ctx, extra, i => runNeighbors()); }
            finally { ctx.SynapseBusy = false; }
        }

        /// <summary>
        /// 收集**一方**的候选目标（含督军）。
        ///
        /// 两条过滤，出处都在我们自己的 `_collect_tactic_targets`（旁证、非权威）的 pick 分支（`rule_core.gd:4030` 附近）：
        ///   ① **`troop` 类目标不含督军** —— 规则书：效果目标为 troop 不能影响督军
        ///      （原版 `if is_troop_only and tu.is_warlord: return out`）；
        ///      **`an enemy` / `all enemies` 这类则包含督军**（全体分支只判 `_effect_target_blocked`）。
        ///   ② **敌方那一侧的三种「不能被选中」关键词** —— `stealth` / `camouflage` / **`ward`(140)∨`wardWb`(145)**。
        ///      · `stealth`：规则书 Stealth「Cannot be targeted in any way」；
        ///      · `camouflage`：Camouflage「不能被敌方战术与效果选中」；
        ///      · 🔴🆕 **2026-10-25（执行代理 `X3` · 判据文档 `待办判据_引擎_1025b` §〇·2）**：`ward` 那
        ///        **另一半**接上了。改前这里只有前两个词，`ward` 只在**主动能力候选闸**
        ///        （`RuleCore.CanUseAbilityTarget`）挡，**战术卡 / 通用效果目标完全不挡** ——
        ///        代码里原来那句自述「那一半还没接」就指这里。
        ///        判据（第一权威，逐句）：
        ///        ① **战术卡目标**：`BattleManager__IsValidSpellTarget.c:41-53` —— 三支同形，
        ///           都是 `候选.isPlayer != 施放者.isPlayer` ⇒ 再看
        ///           `HasCurrentTrait(0x46 stealth)` / `get_isCurrentlyWarded` /
        ///           `get_isCurrentlyCamouflaged`，任一成立即 `return 0`；
        ///        ② **通用目标**：`RawCardScript__IsValidTarget.c:16-40` —— `ward` 那支
        ///           **不分施放者卡种**、只要 `param_2.isPlayer != 候选.isPlayer` 就拒；
        ///           同族 `stealth` 也只在异侧拒。
        ///        ⇒ 两条判据在「**只在异侧**」这一点上一致，正是本函数 `isFoe &amp;&amp; caster &gt;= 0` 那一格。
        ///        ⚠️ **池内 0 张**（`cards_engine.json` v28 的 `keywords`/`desc`/`descZh` 与
        ///        `cards_raw.json` 1,351 张的 `cardTraits` 现扫各 0 命中）⇒ **今天零行为变化**，
        ///        只有合成夹具走得到。⛔ 别因为「不可达」就把它删掉（铁律 11：原版有的都要有）。
        ///        ⚠️ **已知偏离（🔴 2026-10-26 批次 9 · `E9ER` 已修，见 `tacticCaster` 形参）**：
        ///        改前我们对**所有**来源都挡 camouflage（= 战术那支的口径）⇒「单位能力打伪装单位」比原版通用那支**严**。
        ///        现在按原版：`camouflage` 只在**施放者是战术卡**（`cardType 20`）时挡 —— 判据
        ///        `RawCardScript__IsValidTarget.c:16-40` 那多出来的一条 `施放者.cardType == 0x14`。
        /// </summary>
        /// <param name="caster">施放者；传 -1 表示这是己方侧、不做隐身过滤</param>
        /// <param name="tacticCaster">🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）**：这次效果的施放者是不是**战术卡**。
        ///   `camouflage` 那一格**只对战术卡的来源生效** —— 原版 `RawCardScript__IsValidTarget.c:16-40`
        ///   的 camouflage 支多一个条件 `施放者.cardType == 0x14 (§Tactic)`（`IsValidSpellTarget.c:41-53`
        ///   那条路本身就是战术卡专用的，两支由此**同口径**）。改前我们对**所有**来源都挡 camouflage
        ///   ⇒ 「单位能力打伪装单位」比原版**严**（账 `B9` · 缺口 ④）。调用方按「点选/结算的那张牌」
        ///   传进来（`IsLegalPick` / `PlayTargetPool` 手上都有 `CardDef`）。
        ///   ⚠️ **池内 0 张可达**（`camouflage` 只能由 `addTrait` 效果授予）⇒ 今天零行为变化。
        static void AddSide(List<UnitState> into, PlayerState ps, bool isFoe, bool troopOnly, int caster,
                              bool stealthGate = false, bool tacticCaster = false)
        {
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ps.Board[s];
                if (u == null || !u.IsAlive) continue;
                if (troopOnly && u.IsWarlord) continue;
                if (stealthGate && isFoe && caster >= 0
                    && (u.Has(KeywordTable.Stealth)
                        || (tacticCaster && u.Has(ConditionEval.Camouflage))
                        || IsCurrentlyWarded(u))) continue;
                into.Add(u);
            }
        }

        // ==================================================================
        //  各个动词的结算
        // ==================================================================

        static bool DoDeal(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved)
        {
            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」没有合法目标，空过");
                return true;                                   // 没目标 = 正常空过，不是失败
            }
            // `1-3` 区间：原版是 `randi_range`（`:2677`）—— 用 ctx.Rng 保证可复现
            int dmg = op.AmountMax > op.Amount ? ctx.GetRandomInt(op.Amount, op.AmountMax + 1) : op.Amount;

            int dealt = 0;
            bool altUsed = false;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                // 🆕 「条件换数值」`Deal N …, or M if <条件>`（`Vindicator` / `Wulfen Pack Leader`）——
                //    **逐目标判**：`it has Hunt Mark` 说的就是**这一个**目标（理由见 `AltHolds`）。
                int one = dmg;
                if (op.AltAmount != 0 && AltHolds(ctx, owner, op, t, by))
                {
                    one = op.AltAmount; altUsed = true;
                    ctx.Log($"{by}：「{op.Source}」条件「{op.AltCondition}」成立 ⇒ 「{t.Name}」这一下"
                          + $"**{dmg} → {one}** 点");
                }
                // 🔴 **2026-10-10（`A1250`）**：**效果击杀也算一次击杀** —— 判据 = 原版
                // `CardScript__ResolveDeadCard.c:252`（`local_28 == 0x14 /* ability(20) */` 那一档）。
                // 凶手 = `ctx.ActingUnit`（= 原版 `AbilityData.actingCard`：**是它那条能力干的**）；
                // 战术卡那条路上它是 `null` ⇒ 不记（原版也记不了：原版 `param_1` 遍历的是
                // **场上那批牌**（`bm + 0x470`），战术卡不在里面）。
                // ⚠️ **阵营要在 `Hurt` 之前取** —— 打完它可能已经不在棋盘上了（`OwnerOf` 会返回 -1，
                //    而阵营是 `Kills` 事件极性的判据，给 -1 = 整档监听器静默不响）。
                // 判别式 / 发射口**只有一处** ⇒ `RuleCore.TryCreditKill`。
                int vOwner = OwnerOf(ctx, t);
                // 🆕 **2026-10-20（`W58-INTERLEAVE`）**：把「致死伤害的来源卡」一并交给 `Hurt`
                //   —— 死亡若被延后进「同时伤害」批，它会随 `PendingDeath` 走到 `FlushDeaths`，
                //   在那个死者**自己的那一拍**上记账（原版 death action 的 FIFO：死者1 反噬
                //   → 死者1 的 Slay → 死者2 …）。批外（没延后）的死亡仍由下面这一句当场记。
                dealt += Hurt(ctx, t, one, by, actingCard: ctx.ActingUnit,
                              // 🔴 2026-10-24（`C1` §3）：效果伤害的死因按**来源卡种**定 ——
                              //   单位能力 ⇒ `ability(20)`（原版 `deathType = ability`）；
                              //   **战术卡 / 防御卡 / 密令卡** ⇒ `tactic(30)`
                              //   （判据 `BattleManager__GetDeathType.c:140-146` +
                              //    `SupportMethods__IsSpellType.c:3-6`，见 `RuleCore.DeathTypeOfSource`）。
                              deathType: RuleCore.DeathTypeOfSource(ctx),
                              // 🆕 2026-10-25（`X2`）：效果伤害 = **计谋**那一类（原版 `AttackTypes.Spell = 3`，
                              //   成就 `Ach_4` = `Stratagem Damage`）。判据 §六 问题 2 第一个打标点。
                              kind: DamageKind.Spell);
                if (!ctx.DeathsDeferred)
                    RuleCore.TryCreditKill(ctx, ctx.ActingUnit, vOwner, t);
            }
            ctx.Log($"{by}：「{op.Source}」对 {targets.Count} 个目标造成 {dmg} 点伤害（共 {dealt}）"
                  + (altUsed ? "（其中有的目标按**条件替换值**结算，见上一行）" : ""));
            return true;
        }

        /// <summary>
        /// **付费修饰型激活** —— `6 [Energy]: Extend effect until your next turn`（`Miraculous Feat`）·
        /// `8 [Energy]: Give it permanently`（`Daemonbreaker`）。（2026-09-13 A4 批 1）
        ///
        /// 🔴 **判据查不到（`D26`，2026-10-19）**：原版把这类「付费改前一句时长」做成
        /// **两条独立的 ability 效果**（`CardScript.AddEffect` 那一路），**没有** `undo`/`replay`
        /// 这种文本层回填的做法 ⇒ 本版反编译里**没有对应的方法可指**。
        /// ⚠️ 下面这三条 `undo` / `replay` / `:1799` 的语义是**我们上一版 Godot 复刻**
        /// `rule_core.gd:1588 _energy_act_prep` 的写法（**旁证、非判据**）。
        /// 可核的那一半 = **卡面原文**：`Sorotitas/4计策/Warpforge_52_Miraculous-Feat.png` ·
        /// `Sorotitas/2天赋/Warpforge_02_Daemonbreaker.png`。
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

        /// <summary>🆕 2026-10-24（引擎线 `B7`）：**`pool` 里有没有那一方的督军**（有就返回它，没有返回 `null`）。
        ///
        /// 用途 = 原版 `BattleManager__GetLowestHealthUnit.c:100-109` 的「**初始 best = 该侧督军**」
        /// （那段与 `BattleManager__GetHero.c` 的主体逐句同形：`*(manager + (isPlayer0 ? 200 : 0xC0))`
        /// 再取 `+0x28`）。原版**不判**督军是否在候选闸里就先塞进去，是因为 `ta = 240`
        /// （`AbilityLogic__GetTargets.c:835-844`）**不过筛**、督军一定在 `allCardsInPlay` 里；
        /// 我们这一侧的 `pool` 已经被卡面词筛过（`a friendly **troop**` 之类）⇒ 只有它**本身也在候选表里**
        /// 才预置，否则会把一个被卡面排除的对象当成目标。
        /// ⚠️ 两侧督军都在池里时（`Side == "any"` 那种）取 **player 0 那个** —— 原版同样只会解析出**一侧**
        /// （`bVar16` 是个布尔），今天池内**没有任何卡**走到这一格（见 B7 报告「可达性」）。</summary>
        static UnitState WarlordInPool(BattleContext ctx, List<UnitState> pool)
        {
            if (ctx == null || ctx.Players == null || pool == null || pool.Count == 0) return null;
            for (int p = 0; p < ctx.Players.Length; p++)
            {
                if (ctx.Players[p] == null) continue;
                var w = ctx.Players[p].Warlord;
                if (w == null) continue;
                for (int i = 0; i < pool.Count; i++)
                    if (ReferenceEquals(pool[i], w)) return w;
            }
            return null;
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
        /// 攻击类型：**取近战 / 远程里高的那个**（用户 2026-09-14 给的口径），**不兜底** ——
        /// 那一种打不动就如实报「打不了」，**不要偷偷换另一种**（见下面 ③ 那段注释）。
        /// </summary>
        static bool DoForceAttack(BattleContext ctx, int owner, string by, EffectOp op,
                                  UnitState chosen, List<string> unresolved)
        {
            // ---- ① 谁去打 ----
            // 卡面点名了攻击者（`Make …` / `Target …`）就按目标规格收：`Count == 0` = **全部各打一次**、
            // `Count == 1` = 玩家点的那一个。卡面**没写**（裸 `Attacks a random enemy …`）⇒ **本卡自己**
            // （`ctx.ActingUnit` 由 `ResolveOne` 在结算每条效果前设好）。
            //
            // 🔴🆕 **2026-10-25（引擎线 `A1`）：这条 op 的 `targetCriteria` 该筛哪一侧 —— 两族不一样。**
            //   原版只有**一个** `logic.targetCriteria`，`PlayAbility` 先把它建成的 `possibleTargets`
            //   放进 `abilityLogic +0x78`（`AbilityLogic__PlayAbility.c:278-282` 调
            //   `AbilityLogic__GetTargets(manager, logic.targetCriteria, …)`），**然后按 `chosenAbility` 分派**：
            //     · `0xe6 / 0xe7`（230 `forceAttack` / 231 `forceAttackDontCountAttack`）·
            //       `0xe9`（233 `forceAttackFriendly`）—— 把 `possibleTargets[i]` 逐个传给
            //       `AddForceAttack(targetCard, …)` / `AddForceAttackFriendlyUnits`，而
            //       `BattleManager._ResolveForceAttack_d__504__MoveNext.c:33-66` 拿那张卡去
            //       `AI__SelectBestAttackAction(…, card, …)` 再 `AddScriptedAttack(attacker = 那张卡, …)`
            //       ⇒ **这张卡是被逼着去攻击的那一个** ⇒ `possibleTargets` = **攻击者**，
            //       criteria 筛的是**攻击者池**（我们下面这一支今天的行为，**是对的**）。
            //     · `0xeb / 0xec / 0xed / 0xee`（235 `scriptedSelfAttack` / 236 `scriptedActingCardAttack` /
            //       237 `scriptedTargetCardAttackDontCount` / 238 `scriptedSelfCardAttackDontCount`）——
            //       `AddScriptedAttack / AddScriptedForceAttack / AddRedirectedAttack` 的**攻击者由能力本身定死**
            //       （`abilityData+0x10/+0x18/+0x20`），`possibleTargets[i]` 一律落**被打的那一个**
            //       ⇒ `possibleTargets` = **受害者**，criteria 筛的是**受害者池**。
            //   ⇒ **判据是 `chosenAbility` 那一格**（键在卡 SO 的 `abilityLogic[]` 上）；
            //     我们的卡池 `cards_engine.json` **没有这一列**（见报告「没查清 / 还差什么」），
            //     所以这里用**文本形状**做代理，并已**逐卡核过全池**（`desc`+触发正文里的 `attack` 句式）：
            //       · `Target <攻击者> attacks <被打的>`（**两个目标都点名**）—— **全池只有 `EC46`**，
            //         它的 `chosenAbility = 237` ⇒ criteria 归**受害者**；
            //       · `Make <攻击者> attack by itself`（`BL64`，231）· 裸 `attack by itself`（`DA25`，231）
            //         ⇒ criteria 归**攻击者**（保持原样）。
            //   ⛔ 别把「criteria 一律挪到受害者池」当成通则 —— 那会把 `BL64`（`ta=30` +
            //      `conditionsFilter=damaged`：**描述的是被点名的那个友方单位**）筛到敌人身上。
            bool critOnDefender = op.Target != null && op.Target2 != null && op.TargetCriteriaSet != null
                                  && !string.Equals(op.Payload, "byitself", System.StringComparison.Ordinal);

            // 🆕 **2026-10-25（引擎线 `A2` · `A2-1`）：攻击者**没点名**（= 本卡自己打）那一族，criteria 同样筛受害者池。**
            //   判据（`AbilityLogic__PlayAbility.c` 按 `chosenAbility` 分派；表见 `A1` 报告 §A1）：
            //     · `230/231/233`（`forceAttack*`）—— `possibleTargets` 是被逼着攻击的**攻击者**；
            //     · `235/236/237/238`（`scripted*`）—— 攻击者由能力本身定死，`possibleTargets` 是**被打的那个**。
            //   `op.Target == null` 只可能是后者（卡面没点名攻击者 ⇒ 本卡自己出手），
            //   ⇒ 这条 logic 的 `targetCriteria` 描述的是**受害者**。
            //   本批这五张卡的裁定（逐张核过卡面 + `cards_truth.json` 的 `chosenAbility`）：
            //     · `EC12`（238 `Attack a random enemy`）· `EC22`（238 `Attack the enemy with highest attack`）·
            //       `SAU70`（238 `Attacks a random enemy with lowest Health`）·
            //       `GOF86`（235 `attack a random non Flying enemy`）⇒ **criteria 归受害者**，本支生效；
            //     · `SW9`（238，但 logic `ta=30 target`）⇒ **不套**：它的 huntMark 行是
            //       **ability 级** criteria（`CardAbility.targetCriteria +0x30`，`CanTriggerAbility` 触发前判；
            //       原版 `cards_truth.json` 的两条 ability 一行 `traitsFilter=1260 猎杀标记`、一行 `+excludeTrait`
            //       ⇒ 那是「目标带不带猎杀标记 ⇒ 走哪条 ability」的**分支闸**，不是受害者筛子），
            //       而 logic 级 `ta=30` = 事件目标原样入表 —— 与卡面 `Attack a non Flying enemy troop`
            //       **不矛盾**（「非飞行」由文本 `NotKeyword` 承担）。⇒ 见报告 §A2-1。
            //   ⚠️ **身份/承接档不套**：`ta ∈ {20 self, 30 target, 40 attacker, 70 abilityOwner, 80 actingCard,
            //      90 targetIfMeetsCriteria, 140/150 previousAbilityTargets}` 描述的不是「受害者池」，
            //      硬套会被 `TargetCriteriaEval.Matches` 的身份比较**整个筛空**（`ta=20`：候选必须等于施放者）。
            if (!critOnDefender && op.Target == null && op.Target2 != null && op.TargetCriteriaSet != null)
            {
                int tta = op.TargetCriteriaSet.TargetsAffected;
                if (tta != 20 && tta != 30 && tta != 40 && tta != 70 && tta != 80 && tta != 90
                    && tta != 140 && tta != 150)
                    critOnDefender = true;
            }

            var attackers = new List<UnitState>();
            if (op.Target == null)
            {
                var self = ctx.ActingUnit;
                if (self != null && self.IsAlive) attackers.Add(self);
            }
            else
            {
                // `critOnDefender` ⇒ 这一条 criteria **不**筛攻击者池（它描述的是被打的那个）。
                attackers.AddRange(ResolveTargets(ctx, owner, op.Target, null, chosen,
                                                  critOnDefender ? null : op.TargetCondition,
                                                  critOnDefender ? null : op));
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

                // ---- ② 攻击类型：**取近战 / 远程里高的那个**（要先定，因为挑谁要按它算「打不打得死」）----
                // 出处：**用户 2026-09-14 给的口径** ——「就当做这个单位进行的普通攻击；
                //   攻击类型**根据它近战/远程里最高的那个攻击力值**决定；打谁**由效果决定**」。
                // ✓ 和原版 `CardScript.ChooseAttackTypeAutomatically` 的规则一致
                //   （`decomp_out/CardScript__ChooseAttackTypeAutomatically.c:8-11` 写的是 `(近战<远程)+1`）。
                // ⚠️ **不兜底**：那一种打不动（飞行 / 压制 / 该类型攻击力为 0）就**如实报「打不了」**，
                //    **不要偷偷换另一种**（用户口径里没有这一条，换了就是「比卡面做得宽」）。
                //    —— 2026-09-14 改：原来写过一版「高的打不动就试另一种」，按铁律 3 去掉了。
                bool ranged = RuleCore.FieldAttack(ctx, owner, atk, true)
                            > RuleCore.FieldAttack(ctx, owner, atk, false);

                // ---- ③ 打谁 ----
                // 🔴 `critOnDefender`（见 ① 上面那段判据）⇒ **这条 criteria 在这一侧生效**：
                //    原版 `possibleTargets` 就是受害者集，`GetTargets` 建池时就过 `targetCriteria`，
                //    **先筛再裁**（`maxTargets` / `targetSelectMethod` 在筛完之后才用）
                //    ⇒ 必须把 criteria 交给 `ResolveTargets` 在**裁剪之前**过，⛔ 不能等 `DefenderPool`
                //    取完一个再来补判（那就变成「挑错了再丢弃」，与 `GetTargets` 的次序不符）。
                var cand = DefenderPool(ctx, owner, op, critOnDefender);
                if (cand.Count == 0)
                {
                    ctx.Log($"{by}：「{op.Source}」{atk.Name} 一个可打的敌人都没有，跳过");
                    continue;
                }
                // ⚠️ **只在卡面没写打谁时重排**（`by itself` / `attack by itself`）。
                //    卡面写了要求（`a random enemy` / `with lowest Health` / `with highest attack`）
                //    就**照卡面挑** —— 用户口径是「**根据要求**选择攻击对象，**没有这个说明**才优先挑
                //    可以摧毁的单位」，两半都要照办。
                if (op.Target2 == null)
                    cand = PreferKillable(ctx, owner, atk, atkSlot, cand, ranged);

                // ---- ④ 打（走唯一那条攻击路径）----
                UnitState tgt;
                int code = TryAttackOnce(ctx, owner, atkSlot, cand, ranged, out tgt);
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
        /// （`TryAttackOnce` 会逐个试），顺序再由 `PreferKillable` 把「打得死的」提到前面。
        /// ⚠️ 顺序**不掷骰**（同一局必须可复现）：先「打得死的」组、组内按**槽号**小的在前。
        ///
        /// <param name="withCriteria">🆕 2026-10-25（引擎线 `A1`）：**这一条 op 的 `targetCriteria`
        /// 筛的是受害者池**（判据 / 为什么不是一律 —— 见 `DoForceAttack` ① 上面那一大段）。
        /// `true` ⇒ 把 `op` + `op.TargetCondition` 交给 <see cref="ResolveTargets"/>，
        /// 让它**在裁剪之前**过 36 格 criteria（原版 `GetTargets` 的次序：先筛、再 `maxTargets`）。
        /// `false` ⇒ 老行为（不筛）。</param>
        static List<UnitState> DefenderPool(BattleContext ctx, int owner, EffectOp op, bool withCriteria)
        {
            var spec = op.Target2;
            if (spec == null)
                spec = new EffectTargetSpec
                {
                    Raw = "(未写目标：全场敌方单位，由攻击合法性筛)",
                    Side = "enemy", Kind = "any", Count = 0,
                };
            var list = ResolveTargets(ctx, owner, spec, null, null,
                                      withCriteria ? op.TargetCondition : null,
                                      withCriteria ? op : null);
            list.RemoveAll(u => u == null || !u.IsAlive);
            return list;
        }

        /// <summary>
        /// 强制攻击的**候选重排**：卡面**没写打谁**时，把「**这一下打得死的**」挪到前面
        /// （各组内部仍按**格位号**，不掷骰 —— 同一局必须可复现）。
        ///
        /// 🔴 **出处：用户 2026-09-14 给的口径** ——「根据要求选择攻击对象，
        ///    **不过没有这个说明**，那么就是**优先选择可以摧毁的单位**」。
        ///    所以本函数**只在 `Target2 == null` 时被调用**；卡面写了要求
        ///    （`a random enemy` / `with lowest Health` / `with highest attack`）就走卡面那条路。
        ///
        /// 判据全部**共用**，不另写第二份：
        ///   · 攻击力 = `RuleCore.FieldAttack`（与真打时同源）
        ///   · 能不能打死 = `RuleCore.WouldKill`（与 `ApplyDamage` 共用同一份伤害公式）
        ///   · 打不打得到 = `RuleCore.IsValidTarget`（与 `DeclareAttack` 同一份合法性判据）
        ///     ⚠️ 排「打得动」的只是为了让**日志说实话**；排错了也不影响结果 ——
        ///     `TryAttackOnce` 本来就会逐个重试。
        /// ⚠️ **远程**才加 Markerlight（它只对远程伤害生效）—— 与 `DeclareAttack` 里那段同口径。
        /// ⚠️ 一个都打不死 ⇒ **原样返回**（退回「按槽号试第一个打得动的」，行为与加这条之前一致）。
        /// </summary>
        static List<UnitState> PreferKillable(BattleContext ctx, int owner, UnitState atk, int atkSlot,
                                              List<UnitState> cand, bool ranged)
        {
            int baseAtk = RuleCore.FieldAttack(ctx, owner, atk, ranged);
            var killable = new List<UnitState>();
            var rest = new List<UnitState>();
            foreach (var d in cand)
            {
                if (d == null || !d.IsAlive) continue;
                int dp, dslot;
                bool canHit = FindSlot(ctx, d, out dp, out dslot)
                           && RuleCore.IsValidTarget(ctx, owner, atkSlot, dp, dslot, ranged) == RuleCodes.OK;
                int dmg = baseAtk;
                if (ranged && d.Has("markerlight")) dmg += d.KwValue("markerlight");
                if (canHit && RuleCore.WouldKill(d, dmg)) killable.Add(d);
                else rest.Add(d);
            }
            if (killable.Count == 0) return rest;      // 一个都打不死 ⇒ 原顺序
            ctx.Log($"{atk.Name} 的强制攻击：卡面没写打谁 ⇒ **优先挑打得死的** —— "
                  + $"{Names(killable)}" + (rest.Count > 0 ? $"（打不死的 {rest.Count} 个排在后面）" : ""));
            killable.AddRange(rest);
            return killable;
        }

        /// <summary>在候选里挑一个**打得动**的，按 <paramref name="ranged"/> 打一次。
        /// `OK` = 真打出去了；否则返回**最后一次**的失败码。
        /// ⚠️ 反复试是安全的：`RuleCore.DeclareAttack` 把校验全做在改动**之前**（先 `IsValidTarget`
        ///    再消耗攻击），所以失败的那几次**没有副作用**。
        /// 🔴 **2026-10-10（`A1256`）：这里传 `forced: true`** —— 本函数只被 `forceattack` 那条 op 用
        ///    （`DoForceAttack` ⇒ `TryAttackOnce`），而原版「强制攻击」是**另一条 action**
        ///    （`BattleActionType 0x2a` → `_ResolveForceAttack_d__504`），**全库只有 `_ResolveAttack`
        ///    调 `CheckUnitsCancellingAttack`** ⇒ **原版那条路不被替身截**。
        ///    我们共用同一条 `DeclareAttack` ⇒ 必须显式告诉它「这一记是强制的」，
        ///    否则「强制攻击打督军」会被 `Vargard Obyron` 的替身改到它自己身上（真偏离）。
        ///    判据全写在 `RuleCore.DeclareAttack` 的 `forced` 形参上。</summary>
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
                int code = RuleCore.DeclareAttack(ctx, owner, atkSlot, dp, dslot, ranged, forced: true);
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
        /// <summary>
        /// `Stratagems in your hand become a Hunting Wolf or Fenrisian Wolf`
        /// （`Hrolf the Ironhowl`，SpaceWolves，2026-09-14 A5 批 4，全池只 1 处）。
        ///
        /// 手牌里每一张**战略卡**换成候选卡之一。
        ///
        /// 🔴 **更正痕迹（铁律 5）**：这里原来写着「`or` 到底怎么解，三层权威全都没有 ⇒ 两层都是我们挑的」——
        ///    **2026-10-24（`C1` §7）推翻**：原版语义**查得到**（见下面 ② 的逐句判据），
        ///    只是它落在**结构化资产**（`summonCriteria.unitsToSummon` + `chosenAbility 90`）里，
        ///    不在**卡面文本**上 —— 当年按 `become` 这个词在反编译里搜，搜的不是这条路。
        ///    ✅ **仍然是我们挑的只有一层**：**「把英文正文 `a X or Y` 解析成「这两个候选」**这一跳
        ///    （原版没有文本解析层）。候选名单本身对得上（`Hunting Wolf` / `Fenrisian Wolf`）。
        ///    成品卡图（`Space Wolves/3部队/Warpforge_23_Hrolf-the-Ironhowl.png`，照铁律 7 核过）只印着 `or`。
        ///      ① **挑法** = **随机**（`ctx.Rng`）—— 🔴 **2026-09-14 用户裁决**：
        ///         「`a Hunting Wolf or Fenrisian Wolf` 这个就是**随机变成两个中的一个**」。
        ///         卡面**没有** `choose` 字样 ⇒ 按规则书 `:475` 那条「`choose` 关键词才轮到玩家选」
        ///         的反面，这里**不该问玩家**。
        ///         ⚠️ 沿革：第二十六轮是「每张手牌各自 `ctx.Rng` 随机」（近似），
        ///         2026-09-14 晚改成 `TakePick` 问玩家（**那个改错了，本次改回随机**）。
        ///      ② 🔴 **2026-10-24 就地订正（引擎线 `T02` · `C1` §7 · `SW18`）：原版是【逐张目标各自随机】。**
        ///         判据（第一权威，逐句读过反编译）：
        ///           · `AbilityLogic__PlayAbility.c:950` 的 `chosenAbility == 0x5a /*90*/` 那一支，
        ///             `:953-978` 是 **`while` 循环遍历目标表**（`logic.possibleTargets(+0x78)`，
        ///             由 `targetCriteria` 的 `GetTargets` 填 = `SW18` 的 `ta 50 inHandFiltered` 手牌），
        ///             **循环体内 `:958` 每次都调 `AbilityLogic__GetCardToSummon(...)`**；
        ///           · `AbilityLogic__GetCardToSummon.c:191-206`：候选非空 ∧ `summonAllTargets == 0`
        ///             ⇒ **`:193` `GetRandomInt(0, count)` 取一个** ⇒ **每次调用独立掷一次**。
        ///         ⇒ 两张以上手牌时结果是**混合**的（有的变 Hunting Wolf、有的变 Fenrisian Wolf）。
        ///         ⚠️ 改前这里是「**只掷一次、全手牌统一用**」（旧口径是照卡面单数冠词猜的，
        ///         三层权威都查不到 `or` 怎么解）—— 那是错的，现在按上面那条判据改成逐张掷。
        ///         ⚠️ 掷的**次数**也照原版：**每一张被选中的战略卡各掷一次**（即使掷出来跟它现在
        ///         同名，也照样掷——原版是先 `GetCardToSummon` 再 `AddTransformCard`）。
        ///    ⇒ 每次变身照旧**逐张打日志**，不静默。
        ///
        /// ⚠️ 「手牌里哪些算战略卡」的判据**转调 `CreatePool.MatchesKind(c, "stratagem")`**
        ///    （只此一份；它认 `type = tactic` **或** `defence`）。
        /// </summary>
        static bool DoBecome(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var names = new List<CardDef>();
            foreach (string n in (op.Payload ?? "").Split('|'))
            {
                string t = n.Trim();
                if (t.Length == 0) continue;
                var cand = CreatePool.FindByName(ctx.CardPool, t);
                if (cand == null)
                {
                    ctx.Log($"{by}：「{op.Source}」要变成「{t}」，卡池里却查不到同名的卡 —— **这一项没生效**");
                    unresolved.Add(op.Source + "（变身目标查不到：" + t + "）");
                    continue;
                }
                names.Add(cand);
            }
            if (names.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」变身的候选**一个都查不到** —— **这条没生效**");
                return false;
            }

            var ps = ctx.Players[owner];
            int changed = 0, seen = 0;

            for (int i = 0; i < ps.Hand.Count; i++)
            {
                var cur = ps.Hand[i];
                if (cur == null || !CreatePool.MatchesKind(cur.Card, "stratagem")) continue;
                seen++;
                // 🔴 **2026-10-24（`C1` §7 · `SW18`）：逐张**各掷一次**（判据见函数头 ②）。
                //   ⚠️ 掷的位置必须在这里：**先掷、后判「是不是已经同名」** —— 原版
                //   `PlayAbility.c:958` 先 `GetCardToSummon`（掷），`:974` 才 `AddTransformCard`。
                var pick = names.Count <= 1 ? names[0] : names[ctx.GetRandomInt(0, names.Count)];
                if (ReferenceEquals(pick, cur.Card))
                {
                    ctx.Log($"{ps.Name} 的手牌「{cur.Card.Name}」掷到**同名** ⇒ 不变（原版也照掷一次）");
                    continue;
                }
                // 🔴 第 7 行第 2 步 + **用户 2026-09-18 拍的口径**：**变身 = 另发新实例、状态不跟**
                //   （卡都换了；改前那套按 `CardDef` 键，换 def 之后键就变了 ⇒ 旧行为本来也是「不跟」）。
                ps.Hand[i] = ctx.NewInstance(pick);
                changed++;
                ctx.Log($"{ps.Name} 的手牌「{cur.Card.Name}」变成了「{pick.Name}」");
            }
            // 🔴 **2026-10-24（`E2` · `DG2` 红 #6）**：汇总日志**必须带「变身」二字** ——
            //   `BattleScene.cs` 那条「日志里留下了变身那一笔（**不静默**）」是**全文 grep「变身」**。
            //   本批 `C1` 重写文案时把它写成了「变了身」（拆开的），断言当场失配（行为没退化，只是换了词）。
            //   ⇒ 词头写回「变身：」；⛔ 别改成只让断言去找「变了身」，那还是盯文案（下次再改词还会红）。
            ctx.Log($"{by}：「{op.Source}」变身：手牌里 {seen} 张战略卡，{changed} 张变了身"
                  + $"（候选：{string.Join(" / ", names.ConvertAll(c => c.Name).ToArray())}）"
                  + "—— 逐张各自随机（判据 `PlayAbility.c:950-978` + `GetCardToSummon.c:193`）");
            return true;
        }

        static bool DoEachUnitDeal(BattleContext ctx, int owner, string by, EffectOp op)
        {
            // 两种数值写法（2026-09-14 A5 批 3 第 4/5 条把后者补上）：
            //   · 关键词型 —— `damage equal to its Shuriken`：`Payload` 是关键词名，`Amount == 0`
            //   · 定值型   —— `deals 1-2 damage`：`Amount`（+ `AmountMax`）写死，`Payload` 空
            bool fixedDmg = op.Amount > 0;
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
            int hitters = 0, dealt = 0, skipped = 0, filtered = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = mine.Board[s];
                if (u == null || !u.IsAlive) continue;

                // `Other friendly <X>` —— 主语筛选 + 排掉施放者自己（见 `EffectOp.Subject` /
                // `EffectOp.OtherThanSelf`）。`Each of your units …` 那两支两者都是空的，行为不变。
                if (op.Subject != null && !op.Subject.IsEmpty && !op.Subject.Matches(u.Card)) { filtered++; continue; }
                if (op.OtherThanSelf && u == ctx.ActingUnit) { filtered++; continue; }

                int n = fixedDmg
                      ? (op.AmountMax > op.Amount ? ctx.GetRandomInt(op.Amount, op.AmountMax + 1) : op.Amount)
                      : u.KwValue(kw);
                if (n <= 0) { skipped++; continue; }      // 没有这个关键词 / 值是 0 ⇒ 它本来就不打
                pool.RemoveAll(t => t == null || !t.IsAlive);
                if (pool.Count == 0) break;               // 打空了就停（后面的单位没目标）
                var t = pool[ctx.GetRandomInt(0, pool.Count)];
                // 🔴 **2026-10-10（`A1250`）**：效果击杀也要记一次 —— 凶手是**开火的这个单位 `u`**，
                // 不是 `ctx.ActingUnit`：卡面写的是「**its** Shuriken / deals 1-2 damage」，
                // 这一下是**它自己**打的（`for each friendly unit` 逐个数出来）。
                // ⚠️ 如实标着：原版那一跳的 `actingCard` **读不到**（那批卡的 ability 资产在远端 CCD）
                //    ⇒ 这一格是**按卡面语义**落的，不是逐跳核过的。
                // 阵营要在 `Hurt` 之前取（打完它可能已经不在棋盘上了，见 `DoDeal` 同一条）。
                int vOwner = OwnerOf(ctx, t);
                // `W58-INTERLEAVE`：同 `DoDeal` —— 延后进批的死亡由 `FlushDeaths` 逐死者记账。
                dealt += Hurt(ctx, t, n, by, actingCard: u,
                              // 🔴 2026-10-24（`G2` · `C5` 顺手发现 11）：死因同 `DoDeal` —— 按**来源卡种**
                              //   取 `ability(20)` / `tactic(30)`。单位能力驱动时 `ActingUnit` = 那个单位
                              //   ⇒ 仍是 `ability(20)`（与改前逐字相同）；**战术卡**驱动时它才是 `null`、
                              //   回落 `CreationSource`（= 那张战术卡）⇒ `tactic(30)`。
                              deathType: RuleCore.DeathTypeOfSource(ctx),
                              // 🆕 2026-10-25（`X2`）：按关键词打出去的伤害也是**效果伤害** ⇒ 计谋那一类
                              //   （成就 `Ach_4`）。判据 §六 问题 2 第二个打标点。
                              kind: DamageKind.Spell);
                if (!ctx.DeathsDeferred)
                    RuleCore.TryCreditKill(ctx, u, vOwner, t);
                hitters++;
            }
            string howMuch = fixedDmg
                          ? (op.AmountMax > op.Amount ? $"各 {op.Amount}-{op.AmountMax} 点" : $"各 {op.Amount} 点")
                          : $"各自按自己的 {kw} 值";
            ctx.Log($"{by}：「{op.Source}」{hitters} 个单位{howMuch}开火（合计 {dealt} 点）"
                  + (skipped > 0 ? $"，另有 {skipped} 个单位没有 {kw}、跳过" : "")
                  + (filtered > 0 ? $"，{filtered} 个不满足主语筛选（`Other friendly …`）" : ""));
            return true;
        }

        static bool DoHeal(BattleContext ctx, int owner, string by, EffectOp op,
                           UnitState chosen, List<string> unresolved)        {
            // `1-5` 区间：和 `Deal` 一样走 `ctx.Rng`（我们自己的 `rule_core.gd:2677`（旁证、非权威）的 `randi_range`）
            int heal = op.AmountMax > op.Amount ? ctx.GetRandomInt(op.Amount, op.AmountMax + 1) : op.Amount;

            // 🆕 2026-10-20（战斗线 `W58`）：**逐目标走 `HealUnit`** —— 它负责「实际量 = min(请求, 缺失)」
            // 与「`healed > 0` 才发 `EvtKind.Heal`」这两个原版闸（判据 → `HealUnit` 的 doc）。
            // 多目标这条循环**逐个**调用 ⇒ 引擎按目标顺序发 N 条 `Heal` 事件；表现层的时间线
            // 再把它们**串行**排开（每目标 1.5 s，照原版 `ResolveHeal` 的 `yield return`）。
            if (op.Target == null)
            {
                // `Heal N` 没写目标 = **治己方督军**（我们自己的 `rule_core.gd:2842`（旁证、非权威））
                var w = ctx.Players[owner].Warlord;
                int healed = w != null ? HealUnit(ctx, w, heal, false) : 0;
                ctx.Log($"{by}：「{op.Source}」治疗己方督军 {heal}（实际 {healed}，现 {w?.Health}）");
                return true;
            }
            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            int total = 0;
            foreach (var t in targets)
                total += HealUnit(ctx, t, heal, false);
            ctx.Log($"{by}：「{op.Source}」治疗 {targets.Count} 个目标各 {heal} 点（实际合计 {total}）");
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
            for (int i = before; i < hand.Count; i++) ctx.DrawnThisResolve.Add(hand[i]);   // 第 7 行第 3 步：记**哪几份**
            // 🆕 **2026-10-18（`A888`）：同时填「手牌那一侧的代词槽」** ——
            //    `Draw two troops and give them +1`（`GOF50`）/ `Draw a troop and give it +1`
            //    （`GOF_Da_Red_Waaagh`）里的 `them` / `it` 指的就是**刚抽上手的这几份**。
            //    ⚠️ 和上一句**不是一回事**：上句是「本次结算抽到几张」的**计数账**
            //    （`For each troop drawn …` 数它），这一对才是**代词槽**
            //    （`BattleContext.LastHandTargets` / `LastHandTarget`）。
            //    ⚠️ **单数槽只在恰好一张时填**：`Draw two troops … give it …` 这种写法卡池里没有，
            //    从一堆里挑一个 = 静默猜（红线）⇒ 留 `null`，由 `HandReferents` 退回兜底。
            ctx.LastHandTargets.Clear();
            for (int i = before; i < hand.Count; i++) ctx.LastHandTargets.Add(hand[i]);
            ctx.LastHandTarget = ctx.LastHandTargets.Count == 1 ? ctx.LastHandTargets[0] : null;
            ctx.Log($"{by}：「{op.Source}」抽了 {op.Amount} 张");
            return true;
        }

        /// <summary>
        /// `deploy` —— **免费把一个单位放进场上**（我们自己的 `rule_core.gd:2970`（旁证、非权威）那一支）。
        ///
        /// 和 `create` 的关系：**同一个候选池算法**（`CreatePool`），区别只在「去哪儿」——
        /// `create` 进手牌/牌库，`deploy` **直接下场**。卡面的兵种/阵营/费用/关键词限制因此一模一样地筛。
        ///
        /// 三个来源（`op.DeployFrom`）：
        ///   · `pool`      —— 全卡池（`Deploy a Battle Sister` / `Deploy 8 random Ork Infantry`）
        ///   · `deck`      —— 自己牌库（`Deploy 4 random troops from your deck`），**部署后从牌库移走**
        ///   · `graveyard` —— 自己弃牌堆（`Deploy up to 5 friendly Infantry troops that died this game`），同样移走
        ///
        /// ✅ **弃牌堆那条（2026-10-23 起精确）**：候选取 `BattleContext.DeadUnits`（本局阵亡流水，
        ///    带 `Owner` / `DeathTurn` / `Instance`），⛔ 不再拿 `Discard` 里 `type == "unit"` 充数
        ///    —— 手牌超上限打进弃牌堆的单位卡不会被算进来；同名两份也分得清。
        ///    ⚠️ **改前这里写的是**「我们**没有「本局阵亡」的账**，只能拿 `Discard` 充数」——
        ///    **已过期**（铁律 5 就地订正；`BattleContext.DeadUnit` 早就在，`TakeFromGraveyard` 一直在用）。
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

            List<CardInstance> source = null;
            string srcName = "卡池";
            if (op.DeployFrom == "deck") { source = ps.Deck; srcName = "牌库"; }
            else if (op.DeployFrom == "graveyard")
            {
                // 🔴 **2026-10-23（引擎线 `新账-97` ③/⑧）：候选取 `DeadUnits`（本局阵亡流水），
                //    不再拿 `Discard` 充数。** 判据：
                //      · 卡面这类写法 = `… that died this game`（`GSC55 Eve of the Uprising`）；
                //      · `DeadUnits` 记着 `Owner` / `DeathTurn` / 现在还有 **`Instance`**，
                //        同名两份、以及「手牌超上限打进弃牌堆的单位卡」都能分开；
                //      · 顺序 = 死亡先后（表是追加的）。
                //    ⚠️ 取走时走 `TakeFromGraveyard(owner, instance)`（`Discard` 与 `DeadUnits`
                //    两条一起摘 —— 那条规则只写在 `BattleContext` 一处）。
                source = new List<CardInstance>();
                foreach (var d in ctx.DeadUnits)
                {
                    if (d == null || d.Owner != owner || d.Instance == null || d.Card == null) continue;
                    if (!source.Contains(d.Instance)) source.Add(d.Instance);
                }
                srcName = "本局阵亡的部队";
            }

            // ==========================================================================================
            //  🆕 2026-10-19（第五十五会话 · 引擎线 `E12`）：**`deploy them / it` = 上一条效果刚落进手牌的那几份**
            // ==========================================================================================
            // 起因（`AM58 Siege Warfare`）：卡面 `Draw two Vehicles from your deck and deploy them`
            //   解析成两条 op：`drawtype(2, vehicles)` + `deploy(payload="them", deployFrom=<空>)`。
            //   改前第二条落进「候选池 = 全卡池 + payload 当名字查」⇒ `them` 不是卡名 ⇒
            //   **一张都部署不了**（探针实测：只抽了 2 张、棋盘不动）。
            // 判据：`them` 指的就是**上一条效果刚放进手牌的那几份** —— 引擎里已经有这个槽：
            //   `BattleContext.LastHandTargets`（`DoDraw` / `DoDrawType` 末尾按**实例**填；
            //    `BattleDraw` 的注释写着它就是 `them/it` 的代词槽）。
            // 落法照原版 `_ResolveCardAutoSummoned` 那一族：**先拿离手牌 → `DeployFree(fromHand:true)`**
            //   （触发 Rally、兑现手牌加成、`DeploySource = AutoPlayFromHand`）；打不出去就**留回手里**。
            //   ⚠️ `it` 只认单数槽 `LastHandTarget`（`Draw two … give **it**` 那种写法卡池里没有，
            //      一堆里挑一个 = 猜 —— 判空就如实报）。
            //    ⚠️ 只收 `it` / `them` —— `the target` / `this unit` / `this troop` 指的是**场上的**
            //      那个单位（不是手牌里的一批）；那种 `deploy …` 卡池里现读 0 条，真出现时是**另一条语义**（复制/移动），
            //      ⛔ 别顺手塞进这一支。
            string pronoun = (op.Payload ?? "").Trim();
            if (source == null && (pronoun == "it" || pronoun == "them"))
            {
                var refs = new List<CardInstance>();
                if (pronoun == "it")
                {
                    if (ctx.LastHandTarget != null) refs.Add(ctx.LastHandTarget);
                }
                else
                {
                    refs.AddRange(ctx.LastHandTargets);
                    // `HandReferents` 的同一套兜底：槽空才退回「本次结算抽到的」那本计数账。
                    if (refs.Count == 0 && ctx.DrawnThisResolve.Count > 0)
                        refs.AddRange(ctx.DrawnThisResolve);
                }
                if (refs.Count == 0)
                {
                    ctx.Log($"{by}：「{op.Source}」要部署「{op.Payload}」，但**上一条效果没有留下任何一张**"
                          + " —— **这条没生效**");
                    unresolved.Add(op.Source + "（deploy " + op.Payload + "：上一条效果没留牌）");
                    return false;
                }
                int okRef = 0;
                ctx.LastTargets.Clear();
                foreach (var r in refs)
                {
                    if (r == null) continue;
                    int handIdx0 = ps.Hand.IndexOf(r); ps.Hand.Remove(r);   // 原版 `PlayerHand.RemoveCardFromHand`。🔴 **2026-10-26（批次 6 · `G14` ②）拿走【之前】先记原下标** —— 下面打不出去时要**原位插回**（原版 `PlayerHand.AddCardNotDrawnToHandAtIndex`，判据 `decomp_full/PlayerHand._AddCardNotDrawnToHandAtIndex_d__40__MoveNext.c:30-58`：`List.Insert(indexInHand, card)`）
                    int rslot;
                    if (!DeployFree(ctx, owner, r, out rslot, fromHand: true))
                    {
                        RuleCore.AddCardToHandAtWithinLimit(ctx, owner, r, handIdx0);   // 🔴 **原位插回**（⛔ 不是 `Hand.Add` —— 追加队尾会让玩家看到「打不出去的那张自己跳到手牌最右边」；原版是 `Insert(原下标)`）
                        ctx.Log($"{by}：「{op.Source}」的「{r.Card.Name}」部署不了（场上没空格）—— 留在手里");
                        break;
                    }
                    var dep0 = ps.Board[rslot];
                    if (dep0 != null) ctx.LastTargets.Add(dep0);
                    okRef++;
                }
                if (ctx.LastTargets.Count > 0) ctx.LastTarget = ctx.LastTargets[0];
                ctx.Log($"{by}：「{op.Source}」把上一条效果留下的 {okRef}/{refs.Count} 张**直接部署**"
                      + $"（{Names(new List<CardInstance>(refs))}）");
                if (okRef < refs.Count)
                    unresolved.Add(op.Source + $"（deploy {op.Payload}：只部署了 {okRef}/{refs.Count}）");
                return okRef > 0;
            }

            // 🔴🆕 **2026-10-24（引擎线 `A24` · `新账-104`）：`Deploy an X and a Y`** ——
            //   一次部署**两种不同的具名卡**（全池唯一一句：`EC58 Vengeful Surge`
            //   `Deploy an Infractor and a Tormentor`）。判据与「为什么不拆成两条 op」→
            //   `EffectOp.Payload2` 的 doc（原版 `unitsToSummon` 两条 + `summonAllTargets = 1`）。
            //   `LastTargets` **必须是两次部署的并集**：紧跟的 `give them Flank` 指的就是这两张
            //   （拆成两条 op ⇒ 第二条会把槽清成只剩它自己 ⇒ `them` 少给一张，**静默半错**）。
            //   ⚠️ `source == null` 是**防御性**条件：`Payload2` 只在「从全卡池造」那一档才会被填
            //      （见 `EffectText.TryDeploy`），真出现「牌库/墓地 + 第二个载荷」的怪组合时
            //      **退回单载荷那条老路**（只部署 `Payload`），⛔ 绝不悄悄去全池抽第二张。
            if (source == null && !string.IsNullOrEmpty(op.Payload2))
            {
                int pairOk = 0;
                string pairNames = "";
                ctx.LastTargets.Clear();
                string[] halves = { op.Payload, op.Payload2 };
                for (int h = 0; h < halves.Length; h++)
                {
                    var sub = CreatePool.Resolve(ctx.CardPool, halves[h], faction, unitsOnly: true);
                    if (!sub.Ok)
                    {
                        // 一侧解析不出 ⇒ **另一侧照部署**（⛔ 别把两半一起吞）；
                        // 这一半如实出声（红线：不许静默失败）。
                        ctx.Log($"{by}：「{op.Source}」部署不了「{halves[h]}」—— {sub.Why}（**这一半没生效**）");
                        unresolved.Add(op.Source + "（部署「" + halves[h] + "」：" + sub.Why + "）");
                        continue;
                    }
                    // 具名卡 ⇒ 池里通常 1 张。⚠️ 万一这个名字落进了兵种/阵营筛（不是今天的数据形状），
                    // 仍照「各 1 张」抽 —— 选法复用 `PickN`，⛔ 别在这里另编一份。
                    foreach (var card in PickN(ctx, sub.Cards, 1, false))
                    {
                        int slot;
                        if (!DeployFree(ctx, owner, ctx.NewInstance(card), out slot)) break;
                        var dep = ps.Board[slot];
                        if (dep != null) ctx.LastTargets.Add(dep);
                        pairOk++;
                        if (pairNames.Length > 0) pairNames += "、";
                        pairNames += card.Name + "→" + slot;
                    }
                }
                if (ctx.LastTargets.Count > 0) ctx.LastTarget = ctx.LastTargets[0];
                ctx.Log($"{by}：「{op.Source}」部署 **{pairOk}/{halves.Length}** 个"
                      + (pairNames.Length > 0 ? $"（{pairNames}）" : ""));
                return pairOk > 0;
            }

            // 第 7 行第 2 步：区域里现在存的是**实例**，而 `CreatePool` 只认卡面属性
            // ⇒ 把区域**投影成卡模板表**（**逐份投影、保留重复** —— 和改之前那份 `List<CardDef>` 完全同长同序，
            //   所以候选池的算法与随机序列一个字都没变）。
            // 🔴 **2026-10-19（`A1571`）：区域来源【不按施放者阵营筛】。**
            //    `casterFaction` 是「**从全卡池造牌**」那一档的限定（卡片没写阵营时按你的阵营算，
            //    规则书附录 B）。而 `deck` / `graveyard` 是**你自己的区域** —— 里面的牌已经
            //    属于这副牌组，再按施放者阵营筛一次是**另一条规则**：它会静默丢掉
            //    阵营字段对不上的牌（离线探针的夹具牌库整库都是 `Fixture` 阵营 ⇒
            //    `BL55` 实得「按兵种 troops 一张都没筛到」= **A1571 的「真打零效果」就是这个**）。
            //    原版 `autoPlayFromHand` 那条路直接拿牌库里的牌（`GetCardScriptToSummon`），
            //    **没有**这一层阵营过滤。⇒ 区域来源传 `null`；全卡池那条照旧传 `faction`。
            var pool = CreatePool.Resolve(source != null ? source.ConvertAll(h => h.Card)
                                                         : ctx.CardPool,
                                          op.Payload, source != null ? null : faction,
                                          unitsOnly: true,
                                          costMin: op.CostMin, costMax: op.CostMax);
            // ---- 🆕 2026-10-22（`A1682`）：**复制「指代的那一个」**（`deploy a copy of this troop`）----
            //  卡面（`SAU72 Immortals Phalanx`）：`When reanimated, deploy a copy of this troop`
            //  （中文：「被复生时，部署此部队的一个复制。」）。
            //  🔴 **改前**：`CreatePool.Resolve` **认得**这个载荷（它把 `copy of <指代>` 归成
            //     `CopyOfPrev`），但**本函数不看那个旗标** ⇒ 一律走「从池里挑」，而复制品的
            //     候选池**是空的** ⇒ 打「部署不了 —— （**这条没生效**）」+「有 1 条效果本版没结算」，
            //     棋盘**一个单位都不多**（出声那一半是对的，⛔ 别改坏；见下面 `!pool.Ok` 那一支）。
            //
            //  **判据**（原版那条路 —— 逐条核过）：
            //   · 卡面原文 = `cards_engine.json` 的 `SAU72`（成品卡图同）。
            //   · 能力形态 = `AbilityEffect.summonUnit(70)` + `SummonCriteria`
            //     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/SummonCriteria.cs`：
            //      `unitsToSummon` / `chooseCardInPlay` / `keepEffects` / `createAtPos` /
            //      `shouldRemoveFromCemetery`）；
            //   · `AbilityLogic__PlayAbility.c:733-1010`（`case 0x46/0x47/0x48/0x4b/0x4d`
            //     = `summonUnit(70)` / `summonUnitAndSetRef(71)` / `summonUnitAndTriggerEffect(72)` /
            //      `summonBuffedUnit(75)` / `summonUnitDelayed(77)`）→ `BattleManager__AddSummonUnit`
            //     → `BattleManager._ResolveSummonUnit_d__510__MoveNext.c`
            //     → **`BattleCardManager__CreateCard(cardManager, RawCardScript, …)`**：
            //     造的是**卡模板的一份新牌**，不是把场上那个单位连状态搬过去。
            //   ⇒ **复制品 = 原始定义那一档**（卡面数值/关键词、满血），**不是**当前受伤/buff 状态。
            //   ⚠️ **如实标着**：`keepEffects` 那一格原版填几**读不到**（卡数据在远端 CCD，铁律 2），
            //      我们按「原始定义」实现 —— 它同时是**唯一在本地有判据**的那一档；
            //      而 SAU72 触发的那一刻原件本身**就是**刚满血翻回来的
            //      （`DoReanimate` 走 `rem.TransformBackFromRemnant()`、`Health` 必回满）⇒
            //      **两档在今天的可达形状上同值**。
            //   · 落点 = `DeployFree` → `BoardSlots.NextWithoutDisplacing`（**人少那侧最外一格**）——
            //     与原版那条 summon 路用的 `MinionManager__GetNextSlotWithoutDisplacing()`
            //     （`…_d__510__MoveNext.c:135`）**同一个函数**。
            //   · 召唤病 / `Deploy` 事件 / 部署触发 / 信仰能力：全由 `DeployFree` 那**一个**入口给
            //     （⛔ 别在这儿另写一份，见它 doc 里那几条「第二个入口」的纪律）。
            //   · 指代 = `ctx.LastTarget`（`CreatePool` 已判过这个载荷是「复制**指代对象**」，
            //     可指 `this troop` / `it` / `them` / `this unit`）。口径与 `DoCreate` 的
            //     `CopyOfPrev` 那一支**取同一个槽**；能力层由 `ResolveOps` 入口种成 `prime`
            //     （= `seed` = 被再造的那个单位），出牌那条路由 `chosen` 种。
            //     **取不到就如实报**（⛔ 不猜一个目标）。
            if (pool.CopyOfPrev)
            {
                var copysrc = ctx.LastTarget;
                if (copysrc == null || copysrc.Card == null)
                {
                    ctx.Log($"{by}：「{op.Source}」要复制指代的那个单位，但**取不到它** —— **这条没生效**");
                    unresolved.Add(op.Source + "（要复制的指代对象取不到）");
                    return false;
                }
                int cn = op.Amount > 0 ? op.Amount : 1;
                int cok = 0;
                string cnames = "";
                ctx.LastTargets.Clear();                    // 「刚部署的这批」= 复制品（口径同下面主循环）
                for (int k = 0; k < cn; k++)
                {
                    int cslot;
                    // ⚠️ 走 **`CardDef` 那个重载**：它内部 `ctx.NewInstance(card)` 发**一份新的牌**
                    //    —— 复制品**是另一张牌**，⛔ 不能沿用原件那个 `CardInstance`
                    //    （沿用会让场上两张牌共用一个实例号：回手 / 洗回牌库 / 阵亡记账会互相带跑）。
                    if (!DeployFree(ctx, owner, copysrc.Card, out cslot)) break;  // 满场 ⇒ 停
                    var cpy = ps.Board[cslot];
                    if (cpy != null) ctx.LastTargets.Add(cpy);
                    cok++;
                    if (cnames.Length > 0) cnames += "、";
                    cnames += copysrc.Card.Name + "→" + cslot;
                }
                if (ctx.LastTargets.Count > 0) ctx.LastTarget = ctx.LastTargets[0];
                ctx.Log($"{by}：「{op.Source}」复制 {copysrc.Name} **{cok}/{cn}** 份并部署"
                      + (cnames.Length > 0 ? $"（{cnames}）" : ""));
                if (cok < cn)
                    unresolved.Add(op.Source + $"（只复制部署了 {cok}/{cn} 个：场上没空格）");
                return cok > 0;
            }

            if (!pool.Ok)
            {
                ctx.Log($"{by}：「{op.Source}」部署不了 —— {pool.Why}（**这条没生效**）");
                unresolved.Add(op.Source + "（部署：" + pool.Why + "）");
                return false;
            }

            int n = op.Amount > 0 ? op.Amount : 1;
            // 🆕 「条件换数值」形态②：`Deploy a Beast Snagga Boy, or 3 if …`（`Monster Hunters`）——
            //    这里换的是**数量**（条件不看目标，传 null）。
            if (op.AltAmount != 0 && AltHolds(ctx, owner, op, null, by))
            {
                ctx.Log($"{by}：「{op.Source}」条件「{op.AltCondition}」成立 ⇒ 数量 **{n} → {op.AltAmount}**");
                n = op.AltAmount;
            }
            var picked = PickN(ctx, pool.Cards, n, op.UpTo);

            int ok = 0;
            string names = "";
            // 「刚部署的这批」记进 `LastTargets` —— 后面那句 `and give **them** Vanguard` 指着它
            ctx.LastTargets.Clear();
            foreach (var card in picked)
            {
                // 第 7 行第 2 步：来自**区域**的那一份要**沿用**（把牌库/弃牌堆里的那份搬上场），
                // 只有「卡片凭空造」的卡池那条才发新实例。找法：同模板里**从末尾**取一份（和 `Draw` 同向）。
                CardInstance src = null;
                if (source != null)
                    for (int i = source.Count - 1; i >= 0; i--)
                        if (ReferenceEquals(source[i].Card, card)) { src = source[i]; break; }
                var inst = src != null ? src : ctx.NewInstance(card);

                int slot;
                // 🔴 **2026-10-19（`A1571`）：`autoPlayFromHand` 那一档 —— 先「进手牌」再免费打出。**
                //   判据 = `_ResolveCardAutoSummoned_d__511__MoveNext.c`：
                //     · 协程开头要求 `CardScript.IsInHand(card)`（或 `cardState == 0x10 inDeckDrawing`）；
                //     · `:180` 那一支 `MinionPlayedIntoField(..., 1 /*shouldTriggerSummon*/)` ⇒ 会触发
                //       `ThisUnitSummoned(195)`（= 我们 `DeployFree` 的 `Rally`）；普通召唤
                //       （`_ResolveSummonUnit_d__510` 传 `0`）不触发。这一条是两条路**唯一**的可观测差别。
                //     · 前面 `PlayerHand.RemoveCardFromHand` + 末尾 `PlayerHand.CardPlayedWithEffects`
                //       ⇒ 手牌加成（`SetupCardInHand` 挂上去的那一份）也要兑现。
                //   ⇒ 我们照它：**从区域挪进手牌 → `SetupCardInHand` → 立刻从手牌拿掉 → `DeployFree(fromHand:true)`**。
                //   ⚠️ **不扣能量**：协程里没有 `UseMana`（与 `PlayCard` 不同）⇒ 免费打出。
                //   ⚠️ **不发 `EvtKind.Play` / 不记 `NotePlayed`**：协程里没有 `BroadcastCardPlayed` /
                //      `AddPlayCardAction` 那一跳（⛔ 别照 `PlayCard` 的样子补）。
                //   ⚠️ **`TroopsPlayed` 要数**：协程 stage 4 调了 `ChallengeLogMgr.LogPlayTroop`
                //      （「打出的部队卡张数」= 日常任务那一层）；不数的话 task 会少算。
                if (op.AutoPlay && src != null)
                {
                    source.Remove(src);
                    ps.Hand.Add(src);
                    SetupCardInHand(ctx, owner, src);   // 原版 `PlayerHand.SetupCardInHand`：手牌效果挂到这一份上
                    ps.Hand.Remove(src);
                    if (!DeployFree(ctx, owner, src, out slot, fromHand: true))
                    {
                        // ⚠️ **打不出去就留在手里**（原版 `autoPlayFromHand` 是「先抽进手、再打」；打不出去时那张牌仍在手牌里，⛔ 不能吞掉）。
                        // 🔴 2026-10-26（批次 6 · `G14` ②）：走「原位插回」那一口 —— 这一份是**上一行刚 append 进手牌**的（来源是牌库/弃牌堆，**没有更早的手牌下标**）⇒ 它的「原位」= **队尾** `ps.Hand.Count`（与 `Hand.Add` 完全等价）；改走同一口是为了「进手牌」只剩 `RuleCore` 那**一处**（`SetupCardInHand` 不再另写）。
                        RuleCore.AddCardToHandAtWithinLimit(ctx, owner, src, ps.Hand.Count);
                        break;
                    }
                    ctx.TroopsPlayed[owner]++;          // 原版 `ChallengeLogMgr.LogPlayTroop`（协程 stage 4）
                }
                else
                {
                    if (!DeployFree(ctx, owner, inst, out slot)) break;   // 满场 → 后面的也放不下，停
                    if (src != null)
                    {
                        // 牌库：从那份列表里按实例摘；墓地：`Discard` 与 `DeadUnits` 两条一起摘
                        // （唯一口在 `BattleContext.TakeFromGraveyard`，⛔ 别在这儿手写第二份）
                        if (op.DeployFrom == "graveyard") ctx.TakeFromGraveyard(owner, src);
                        else source.Remove(src);
                    }
                }
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
        /// ⚠️ 一律走 `ctx.GetRandomInt`（原版预烧表）—— ⛔ 不用 `ctx.Rng.Next` / `UnityEngine.Random`（2026-10-27 `E1RNG`）。
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
                int k = ctx.GetRandomInt(0, remaining.Count);
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
        ///   ② 用 `ctx.GetRandomInt` 从这里抽 N 张（原版预烧表；2026-10-27 `E1RNG` 前是 `ctx.Rng`）；
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

            // ⚠️ **费用区间要传下去**（🆕 2026-09-15）—— `Runtherd` 的
            //    `Mob: Create a random Ork Beast in your hand that costs 5 or less`
            //    （中文：群体：在你的手牌中生成一个**费用 5 或以下**的随机兽人野兽）。
            //    不传就等于把「5 费及以下」这个限定**静默丢掉**、造出任意费用的兽。
            //    判据由 `EffectText.TryCreate` 抽（与 `deploy` 共用 `ReCostLimit`）。
            var pool = CreatePool.Resolve(ctx.CardPool, op.Payload, faction,
                                          costMin: op.CostMin, costMax: op.CostMax);

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
                // 🆕 **2026-09-16：事件的「那张卡」优先** —— `Whenever you play a … Stratagem,
                //    create an Ephemeral copy of **it**`（`Neurotyrant`，`TL69`）。
                //    那个 `it` 是**刚打出的那张战术卡**（`CardDef`），不是场上单位
                //    ⇒ `LastTarget`（`UnitState`）够不着它。见 `BattleContext.EventCard`。
                if (ctx.EventCard != null)
                {
                    pool.Cards.Add(ctx.EventCard);
                    pool.Detail = "复制事件里那张「" + ctx.EventCard.Name + "」";
                }
                else if (ctx.LastChosenCard != null)
                {
                    pool.Cards.Add(ctx.LastChosenCard.Card);      // 卡池里放的是**卡模板**（第 7 行第 3 步：指代槽是实例）
                    pool.Detail = "复制刚选中的「" + ctx.LastChosenCard.Card.Name + "」";
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

            // 🔴 第 7 行第 2 步：**造出来的每一张都发一份新实例**（复制品与原件从此分得开）
            var made = picked.ConvertAll(c => ctx.NewInstance(c));

            // 🆕 **2026-09-16：`Ephemeral` 复制品**（`create an **Ephemeral** copy of it`，`Neurotyrant`）——
            //   逐张打上临时标记。见 `CreatePoolResult.MarkEphemeral`。
            // 🔴 第 7 行第 3 步：标记打在**造出来的那一份**上（原来按卡模板记份数 ⇒ 同名会互相串）
            if (pool.MarkEphemeral)
                foreach (var m in made) m.EphemeralMarked = true;

            string names = "";
            foreach (var c in picked)
            {
                if (names.Length > 0) names += "、";
                names += c.Name;
            }

            switch (op.Dest)
            {
                case "decktop":
                    // 牌库**顶** = 列表末尾（`Draw` 从末尾 pop）。
                    // 🔴 判据 = 原版**下标 0 = 牌库顶**（`decomp_full/BattleManager__DrawCardFromDeck.c`
                    //    取 `List[0]` · `BattleManager__AddInitialCardToDeck.c` 的 `Insert(0, card)` ·
                    //    `BattleManager__AddCardToDeck.c` 的 `SetActive(go, index == 0)`），
                    //    我们把列表**末尾**当顶（镜像约定）⇒ 放牌库顶 = `AddRange` 追加。
                    //    （`D29①` 改掉原来那句「和 `rule_core` 的 `pop_back` 一致」= 自证。）
                    ctx.Players[owner].Deck.AddRange(made);
                    ctx.Log($"{by}：「{op.Source}」造了 {n} 张放到自己牌库顶：{names}"
                          + (pool.Detail != null ? $"（{pool.Detail}）" : ""));
                    return true;

                case "hand":
                case "enemyhand":
                {
                    int who = op.Dest == "hand" ? owner : 1 - owner;
                    // 🆕 **2026-10-18（`W4` 整改 · 漏点）**：这一支**也是「进手牌」** ——
                    //   原版这条路 = `_ResolveCreateHandCard_d__512:135` → `PlayerHand.AddCardNotDrawnToHand`
                    //   → `:34` 内部调 `SetupCardInHand`。上一轮漏插这里（`RuleCore.SpawnTideCopies` 潮涌
                    //   与 `RuleCore.PlayCompanions` 伴生那两处都插了）⇒ 补上。⚠️ 落点用 **`who`**（牌落到谁手里就是谁的手牌）。
                    // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）：改走「先判上限、再 Add」。**
                    //   原版 `_AddCardNotDrawnToHand_d__39:26-30/63-65` 是**满了就把刚造的那张送墓地**，
                    //   不是「先全 Add、再丢表尾」⇒ 手牌本来就超上限时会误伤旧牌（同一条不变量）。
                    var landed = new List<CardInstance>();
                    foreach (var m in made)
                        if (RuleCore.AddCardToHandWithinLimit(ctx, who, m)) landed.Add(m);
                    // ⚠️ 指代槽 / 费用折扣只认**真落了手**的那几份（`They cost 1 less` 指着它们，
                    //   进了墓地的没必要再挂）；`picked`（隐秘/破坏广播）仍是「造出来的那几张」，不改。
                    ctx.LastCreated.AddRange(landed);      // `They cost 1 less` 指着**那几份**（第 7 行第 3 步）

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
                    // 🔴 **2026-10-26（批次 8 · `B8_AE`）：下面两条广播只跑棋盘跳。**
                    //   原版 `BattleManagerSupport__BroadcastSecretCreated.c` /
                    //   `…__BroadcastSabotageCreated.c` 全文 `GetCardsInHandRef` = **0**
                    //   （🥇 `decomp_full/`：只有「预调 `:28`/`:22` + 棋盘循环 :29-46/:23-40」两段）
                    //   ⇒ 「`When you create a Secret, …`」那两张（`DA15`/`DA66`）**只在棋盘上**才该响。
                    //   ⚠️ 原版那个预调的实参 = **刚造出来的那张卡**（`param_2`，在手牌/牌库里，
                    //   不是棋盘单位）⇒ 我们这边 `subject` 只能是 `null`（`preCalled` 是 `UnitState`）。
                    foreach (var c in picked)
                    {
                        if (CreatePool.MatchesKind(c, "secret"))
                        {
                            BroadcastWhen(ctx, WhenEventKind.CreatesSecret, owner, c, null,
                                          boardAndPersistentOnly: true);
                            // 🆕 **2026-10-25（执行代理 `X18`）**：成就 `Ach_78`（`CreateSecret`）计数
                            //   —— 判据 §B-3 #14「在同一个 if 旁边累加」。归属 = **造牌的施放者 `owner`**
                            //   （与上一句广播同一条口径；⛔ 不是 `who` = 收牌方）。
                            ctx.Tally.AddSecret(owner);
                        }
                        else if (CreatePool.MatchesKind(c, "sabotage"))
                        {
                            BroadcastWhen(ctx, WhenEventKind.CreatesSabotage, owner, c, null,
                                          boardAndPersistentOnly: true);
                            // 🆕 同上：成就 `Ach_65`（`AddSabotageCard`）计数。
                            ctx.Tally.AddSabotage(owner);
                        }
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
            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
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
                if (FindUnit(ctx, t, out o2, out s2))
                {
                    // `W58-INTERLEAVE`：把来源卡一并交给 `CleanupDeaths` —— 死亡若被延后进批，
                    // `FlushDeaths` 会在那个死者自己的那一拍上记账；批外仍由下面这一句当场记。
                    CleanupDeaths(ctx, o2, s2, actingCard: ctx.ActingUnit,
                                  // 🔴 2026-10-24（`C1` §3）：`Destroy` 效果的死因同 `DoDeal` ——
                                  //   按**来源卡种**取 `ability(20)` / `tactic(30)`
                                  //   （判据 → `RuleCore.DeathTypeOfSource`）。
                                  deathType: RuleCore.DeathTypeOfSource(ctx));
                    // 🔴 **2026-10-10（`A1250`）**：**「摧毁」也是击杀的一条** —— 判据 = 原版那条闸
                    // 允许 `local_28 == 0x14 /* ability(20) */`（`CardScript__ResolveDeadCard.c:252`）。
                    // ⚠️ 阵营 (`o2`) 现成（就在上一行），不用像 `DoDeal` 那样先存 —— 但那一份是
                    //    `FindUnit` 在**摧毁之前**找到的格位，同样是对的。
                    // 凶手 = `ctx.ActingUnit`（= 原版 `AbilityData.actingCard`）。
                    if (!ctx.DeathsDeferred)
                        RuleCore.TryCreditKill(ctx, ctx.ActingUnit, o2, t);
                }
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
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, null, op);

            // ---- 🔴 2026-10-11（`A1224`）：**代词目标的「旧对象」要映射回棋盘上现在占位的那一个** ----
            // 起因（判据正本 = `资料/普查产出_第十三会话/R2_A1224现核.md`）：
            // 卡面 `Deal N damage to an enemy **and Stun it**` 里的 `it` 解析成 `Side/Kind = "prev"`,
            // 而**上一条 op 刚刚把这个目标打死了**：
            //   `DoDeal` → `Hurt`（本文件 `:1346`）→ `RemoveIfDead` → `CleanupDeaths` **当场**跑完，
            //   棋盘那一格上**已经换成翻面后的残骸**（`RuleCore.CleanupDeaths` 残骸那一段新建的
            //   **另一个 `UnitState`、同一个 `CardInstance`**），而 `ctx.LastTarget` 仍是
            //   `ResolveTargets` 在**伤害之前**写下的**那个旧对象**（本文件 `:853`）。
            // ⇒ `ResolveTargets` 的 `prev` 支（`:628` / `:632`）拿 `u.IsAlive` 一挡 ⇒ `targets` **空**
            //   ⇒ 只打一行「眩晕了 0 个单位」= **静默不晕**（全池 8 张卡都走这一条，见 §六·①）。
            // 🔴 **原版会晕**：那道闸是 `CardScript__Stun.c:44-45` 的 `cardState ∈ {2,0xf,3,0x11}`
            //   （= `IsInPlay()`），而 `CardScript__CheckIfDead.c:141-144` 的翻面支
            //   **不置 `waitingToDie(5)`** ⇒ 那一刻 `cardState` **仍是 2** ⇒ 闸真。
            //   （`A1176` 已经把这套判据用在 `DeclareAttack` 的震荡那一格；本笔是**同一个缺口**
            //    在**效果 op 那条路**上的另一半。）
            //
            // 修法：**代词支**筛完为空时，按 `BoardOccupantByInstance`（**只认 `Instance`**、
            // **不按格号** —— 理由见那个方法的注释：同一批里别人真死会让连续列表内移）把
            // `LastTarget(s)` 映射成**棋盘上现在的占位者**：
            //   · 映射得到 ⇒ 它就是这一个 op 的目标（残骸 `IsAlive` 真，下面那道 `!t.IsAlive` 过得去）；
            //   · 映射不到（真的死了、已经不在棋盘上）⇒ **照旧空过**，与原版一致。
            // ⚠️ **`spec.Count == 0` 才是 `them`**（上一条效果影响到的**整批**；`ResolveTargets`
            //    用的是同一条判据，`:624`），否则是 `it`（**一个**）。
            // ⚠️ **不加 `ctx.Log`**：末尾那句「眩晕了 N 个单位」已经把结果如实报出来了
            //    （N 从 0 变 1 就是这件事的观测点），而**新增 Log 会改 `ctx.Events.Count` ⇒ 动到
            //    `NetProtocol.Fingerprint`**（本工程踩过，见 `RuleCore` 里那两条同名提醒）。
            // ⛔ **不改 `ResolveTargets` 的 `prev` 支本身**：那是**所有动词共用的口**
            //    （`Deal` / `Destroy` / `Give` / `Blind` … 每个动词在原版各有自己的闸）
            //    —— 一把梭 = 拿 `Stun` 的判据去改别的动词。
            // ⛔ **也不放开下面那句 `!t.IsAlive`**：占位者（残骸）本来就活着、过得了；
            //    放开它反而会把**真死**的目标混进来。
            if (targets.Count == 0 && (spec.Side == "prev" || spec.Kind == "prev"))
            {
                if (spec.Count == 0)
                {
                    foreach (var old in ctx.LastTargets)
                    {
                        var occ = BoardOccupantByInstance(ctx, old);
                        if (occ != null) targets.Add(occ);
                    }
                }
                else
                {
                    var occ = BoardOccupantByInstance(ctx, ctx.LastTarget);
                    if (occ != null) targets.Add(occ);
                }
            }

            // 🔴 `AddEffect` 闸②（jam）/ 闸④（施放者不在场）—— 判据 `.c:85-98 / 171-195`。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, chosen, false, by)) return true;

            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                // 🔴 `AddEffect` 闸①（卡状态）+ 闸③（残骸门）—— `Stun` 在原版是 `addTrait(3)`，
                //    ⇒ 残骸**一律不收**（只有 `addStats` 的生命项与 `addTraitRemnant(90)` 放行）。
                if (!AddEffectCardStateAllows(ctx, t, by)) continue;
                if (!AddEffectRemnantAllowsOp(ctx, t, op, by)) continue;
                // 🆕 **2026-10-09（`A1166`）**：上面这一句 `!IsAlive` 对应原版 `CardScript__Stun.c:44-45`
                //    那道 `cardState ∈ {2, 0xf, 3, 0x11}`（= `CardScript.IsInPlay()`）闸。
                //    🔴 **2026-10-11（`A1224`）就地订正：这一段原来写着「已判等价、⛔ 不另加判据」——
                //    那句话【错了一档】，别再照它判「不用改」。** 错在哪：
                //      · 原来那句的前提是「本方法的目标一律来自 `ResolveTargets` → `AddSide`
                //        （**只遍历 `ps.Board[s]`**，本文件 `:1296-1304`）⇒ 按构造就在棋盘上」；
                //      · 但**代词支根本不走 `AddSide`**（`ResolveTargets` 的 `prev` 支在 `:622-634`
                //        就 `return` 了）⇒ 那句前提对代词目标**不成立**；
                //      · 而且 `!IsAlive`（= `Health > 0`）**比 `IsInPlay()` 收得更紧**：
                //        「血 ≤ 0、但要**翻面成残骸**」那一档原版**在场上、会晕**，我们**不晕**。
                //    ⇒ `A1176` 已于 2026-10-10 在 `RuleCore.cs:2766` 把「等价」那个结论推翻
                //      （判定口 = `RuleCore.StunStillOnBoard` / `BoardOccupantByInstance`）；
                //      上面那段**代词映射**就是这同一个结论在效果 op 这条路的上半。
                //    ⚠️ 这一句 `!IsAlive` **留着**（理由见上面那段 `⛔ 也不放开…`）。
                //    逐档对照（18 档 `CardStateOptions` 与我们的对应物）与判据出处
                //    见 `RuleCore.StunBlockedByTraits` 下方那段 `A1166` 注释。
                // 🔴 **2026-10-09（`A1134`）：`unstunnable`（原版 `HasCurrentTrait(0xfa)`）⇒ 整个施加段跳过。**
                //    判据 = 原版 `CardScript__Stun.c:46-47`：
                //      `cVar13 = EntityScript__HasCurrentTrait(param_1, 0xfa, 0); if (cVar13 == '\0') { …施加… }`
                //    —— 非假时那一支只打一条 `LogWarning` + `ActivateUnstunnable` 音效，**不加 trait**。
                //    ⚠️ **是「当前关键词」（`UnitState.Has` = 可被运行时授予），⛔ 不是印刷关键词。**
                //    判定口共用 `RuleCore.StunBlockedByTraits`（震荡那一格、`SimpleAI.ScoreStun` 同一条）。
                if (RuleCore.StunBlockedByTraits(t))
                {
                    ctx.Log($"{by}：「{op.Source}」想眩晕 {t.Name}，但它有 `unstunnable`"
                          + "（原版 `CardScript__Stun.c:46` 直接跳过施加）");
                    continue;
                }
                if (t.IsStunned) continue;      // 已经是了：见下面那条「广播不能重复」
                // 🔴 **2026-10-09（`A1116`）**：原来是 `t.IsStunned = true;`（**只写字段、不挂关键词**）
                //     ⇒ 改成**挂 `stun` trait**。判据 = 原版 `CardScript__Stun.c:48`
                //     `AddTraitSilently(param_1, 100, …)` —— 全反编译里施加眩晕**只此一条路**
                //     （`grep -n "AddTraitSilently(param_1,100" *.c` 只命中它）；
                //     读点全是 `HasCurrentTrait(100)`（`CheckStun` / `CanAttackCard` /
                //     `IsValidAttackTarget` / `AllowResolveAttack` / `get_mightAct` / `ActivateMinion` …）
                //     ⇒ **原版只有一份表示**，`IsStunned` 现在的定义就是它。
                t.AddKeyword(KeywordTable.Stun, 1);
                // 🆕 **2026-10-24（引擎线 `G1b` · `C2` §6）**：原版 `CardScript__Stun.c:125` **每次被晕**
                //   都调 `DisplayStatusAnim`。本行前面已有 `if (t.IsStunned) continue;` ⇒ 走到这里
                //   一定**首次**（`Amount = 0` ⇒ 由 `SyncKeywords` 的「新出现」路演，不在这里重复演）。
                EmitStatusApplied(ctx, t, KeywordTable.Stun, reapply: false);
                // 🆕 2026-10-09（`A1121`）：**施加时清「回合开始时就在这个状态」那个闸门** ——
                //    判据 = `CardScript__Stun.c:98`（`*(undefined1 *)(card + 0x55) = 0;`）。
                //    ⇒ 这一下眩晕**不会**在**本回合末**被 `RuleCore.EndTurn` 摘掉
                //    （它是「回合中途挂上的」，不是「回合开始时就在的」）——
                //    要撑到它自己的下个回合末才摘 ⇒ 「只废一个回合」。
                //    ⚠️ 原版那一句还带一个 `+0x108 != 0`（「activeEffects 列表非空」）的守卫，
                //       那是表现层的容器、对我们没有对应物 ⇒ 无条件执行，如实标着。
                t.StunnedAtStartOfTurn = false;
                // 🆕 `When an enemy receives a Stun, …`（2026-09-13 第三十四轮）。
                // ⚠️ `!t.IsStunned` 那道守卫是**行为保持**的 —— 重复眩晕也只是把关键词叠一层
                //    （`Has` 一样为真），但**广播不能重复**：卡面写的是「**收到**一次眩晕」。
                BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, t);
            }
            ctx.Log($"{by}：「{op.Source}」眩晕了 {targets.Count} 个单位");
            return true;
        }

        /// <summary>
        /// 失明：**远程攻击力视为 0**（规则书 :166；我们自己的 `rule_core.gd:2814`（旁证、非权威）置 `blind = true`）。
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
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, null, op);

            // 🔴 `AddEffect` 闸②（jam）/ 闸④（施放者不在场）—— 判据 `.c:85-98 / 171-195`。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, chosen, false, by)) return true;

            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                // 🔴 `AddEffect` 闸①（卡状态）+ 闸③（残骸门）—— `Blind` 在原版是 `addTrait(3)` ⇒ 残骸一律不收。
                if (!AddEffectCardStateAllows(ctx, t, by)) continue;
                if (!AddEffectRemnantAllowsOp(ctx, t, op, by)) continue;
                // 🔴 **2026-10-09（`A1116`）**：这里原来是一句 `t.IsBlind = true;`
                //     ⇒ **删掉** —— `IsBlind` 现在是关键词的**派生只读属性**（原版只有 trait
                //     `0x3cf = 975` 一份表示，`CardScript__AddEffect.c:475-487` 施加、
                //     `EntityScript__get_CurrentRangeAttack.c:25-27` 读），下面那句 `AddKeyword` 就是它。
                // 卡面写 `until your next turn` → 撑过**对手的一整个回合**，
                // 到施放者自己的下个回合开始才清（`RuleCore.BeginTurn` 按 `BlindOwner` + `BlindTurnEnd` 判）。
                // 到期点 = 当前回合 + 2（+1 是「对手的回合」，再 +1 才是「我的下个回合」）。
                // ⚠️ **原版另有一条到期路**（`CardScript__OnTurnEnd.c:130-134` 的闸门，
                //    见 `UnitState.BlindedAtStartOfTurn`）—— 那一跳只废一个回合，与本机制等价，两条并存。
                t.BlindTurnEnd = ctx.Turn + 2;
                t.BlindOwner = owner;
                // 🆕 2026-10-09（`A1121`）：**施加时清闸门** —— 判据 = `CardScript__AddEffect.c:487`
                //    （挂上 `0x3cf` 那一段里的 `*(undefined1 *)(card + 0x56) = 0;`）。
                t.BlindedAtStartOfTurn = false;
                bool hadBlind = t.KwValue(KeywordTable.Blind) > 0;
                t.AddKeyword(KeywordTable.Blind, 1);   // ← **这就是唯一表示**（不再是「只为 Has 可见」）
                // 🆕 2026-10-24（引擎线 `G1b` · `C2` §6）：状态施加事件（判据同 `DoStun` 那一处）。
                EmitStatusApplied(ctx, t, KeywordTable.Blind, hadBlind);
                n++;
                ctx.Log($"{t.Name} 失明了（远程攻击力视为 0，到回合 {t.BlindTurnEnd + 1} 结束）");
            }
            ctx.Log($"{by}：「{op.Source}」失明了 {n} 个单位");
            return true;
        }

        /// <summary>
        /// 三选一：**挑一个选项结算**（`EffectText.TryChooseOne` 把三个选项都解析好了，存在 `Payload` 里）。
        ///
        /// 我们自己的那一版是「单机自动选 1」（`rule_core.gd:1159`（旁证、非权威）），我们照办 —— 但用 `ctx.Rng`
        /// （种子化）而不是 `randi()`，同一局必须永远可复现。
        ///
        /// ⚠️ **掷骰发生在结算层、不在解析层**：解析层只负责「三个选项的语法认不认得」，
        ///    这样覆盖率这个进度条才和掷骰无关、每次跑都得到同一个数。
        /// </summary>
        /// <summary>
        /// **「本该问玩家」的这一步**：有面板答案就用面板的，没有就退回 `ctx.Rng`（老行为）。
        ///
        /// ⚠️ **两条路都要能分辨**：用了面板的记 `ChooseAnswered`，没用的只记 `ChooseSites`
        /// ⇒ 结算完 `ChooseSites &gt; ChooseAnswered` 就说明**有几次是引擎替玩家挑的**，
        /// 而那种事**不报错**（本工程的静默失败红线）。
        /// ⚠️ **下标越界当没选**（并如实报）—— 静默取第 0 项会让「选错一张」查不出来。
        /// </summary>
        static int TakePick(BattleContext ctx, int count, string by, string what, List<string> unresolved)
        {
            if (count <= 1) return 0;
            ctx.ChooseSites++;
            if (ctx.ChoosePicks.Count > 0)
            {
                int i = ctx.ChoosePicks.Dequeue();
                if (i >= 0 && i < count)
                {
                    ctx.ChooseAnswered++;
                    ctx.Log($"{by}：「{what}」按**面板上的选择**取第 {i + 1} 项");
                    return i;
                }
                ctx.Log($"{by}：「{what}」面板给的下标 {i} **越界**（候选只有 {count} 个）"
                      + " ⇒ 改由引擎等概率挑");
                if (unresolved != null) unresolved.Add(what + "（面板下标越界）");
            }
            return ctx.GetRandomInt(0, count);
        }

        /// <summary>三选一的**选项文本**（`op.Payload` 里 `|` 分隔；表现层开面板要用）。</summary>
        public static string[] ChooseOneOptions(EffectOp op)
        {
            return (op.Payload ?? "").Split('|');
        }

        /// <summary>
        /// 🆕 2026-09-14：**选牌那一族的「挑一张」** —— 与 <see cref="TakePick"/> 同一套队列纪律，
        /// 外加一条「**面板可以按 `CardDef.Id` 指定**」。
        ///
        /// **为什么单开一个**：规则书英文版 `:475`
        /// 「Whenever a card uses the word "choose", **randomly select 3 cards** from the set of
        /// possibilities and the player chooses which one to keep/draw/resolve」
        /// ⇒ 玩家看见的是**随机抽出的 3 张**，而引擎手里的候选表是**完整的那一份**
        /// ⇒ 下标记不上，必须按**身份**（`Id`）对。
        ///
        /// ⚠️ **引擎侧不必真的先抽 3 张**：「从 N 张里随机抽 3 再挑 1」与「从 N 张里等概率挑 1」
        ///    **是同一个分布** —— 真正要「抽 3」的是**面板**（决定玩家看见哪几张）。
        /// ⇒ 没人指定 `Id` 时行为与老代码**逐字相同**。
        /// ⚠️ 两条队列**必须同进同出**（见 `BattleContext.ChooseCardIds` 的 ①）——
        ///    少出一个下标会让**后面每一处选择全部错位**，而且**不报错**。
        /// </summary>
        static CardDef TakePickCard(BattleContext ctx, List<CardDef> cands, string by, string what,
                                    List<string> unresolved)
        {
            ctx.ChooseSites++;
            if (ctx.ChooseCardIds.Count > 0)
            {
                // **有 `Id` 就以 `Id` 为准**（它才认得准）：`picks[0]` 那个下标是**面板摆出来的
                // 那一行**里的位置，而那一行可能只有 3 张（规则书 `:475`）—— 拿它去索引
                // 引擎手里的**完整候选表**会**挑错一张**，而且不报错。
                string wantId = ctx.ChooseCardIds.Dequeue();
                if (ctx.ChoosePicks.Count > 0) ctx.ChoosePicks.Dequeue();     // 对齐（见上面那条 ⚠️）
                foreach (var c in cands)
                    if (c != null && string.Equals(c.Id, wantId, System.StringComparison.Ordinal))
                    {
                        ctx.ChooseAnswered++;
                        ctx.Log($"{by}：「{what}」按**面板抽出的候选**挑了「{c.Name}」");
                        return c;
                    }
                ctx.Log($"{by}：「{what}」面板报回来的那张（id={wantId}）**不在候选里**"
                      + " ⇒ 改由引擎等概率挑");
                if (unresolved != null) unresolved.Add(what + "（面板报的卡不在候选里）");
                return cands[ctx.GetRandomInt(0, cands.Count)];
            }
            if (ctx.ChoosePicks.Count > 0)
            {
                int i = ctx.ChoosePicks.Dequeue();
                if (i >= 0 && i < cands.Count)
                {
                    ctx.ChooseAnswered++;
                    ctx.Log($"{by}：「{what}」按**面板上的选择**取第 {i + 1} 项");
                    return cands[i];
                }
                ctx.Log($"{by}：「{what}」面板给的下标 {i} **越界**（候选只有 {cands.Count} 个）"
                      + " ⇒ 改由引擎等概率挑");
                if (unresolved != null) unresolved.Add(what + "（面板下标越界）");
            }
            return cands[ctx.GetRandomInt(0, cands.Count)];
        }

        static bool DoChooseOne(BattleContext ctx, int owner, string by, EffectOp op,
                                UnitState chosen, List<string> unresolved)
        {
            var opts = ChooseOneOptions(op);
            if (opts.Length == 0) return false;

            // 🔴 **两种挑法**（`EffectOp.RandomPick` 的注释写了判据来源）：
            //   · `RandomPick == true` —— 卡面写的是 `A or B`、**没有** `choose` 字样
            //     ⇒ **引擎随机挑**（用户 2026-09-14 裁决），**不问玩家、不计 `ChooseSites`**
            //   · 否则 —— 卡面明写 `choose one` ⇒ 走面板那条队列（`TakePick`）
            int pick;
            if (op.RandomPick)
            {
                pick = opts.Length <= 1 ? 0 : ctx.GetRandomInt(0, opts.Length);
                ctx.Log($"{by}：「{op.Source}」随机择一（{opts.Length} 项）→ 第 {pick + 1} 项（{opts[pick]}）"
                      + " —— 卡面**没有** `choose` 字样，按用户口径该随机、不该问玩家");
            }
            else
            {
                pick = TakePick(ctx, opts.Length, by, op.Source + "（三选一）", unresolved);
                ctx.Log($"{by}：「{op.Source}」三选一 → 选第 {pick + 1} 项（{opts[pick]}）");
            }

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
        //  🔴 **判据入口 = 原版反编译**（`D26`，2026-10-19 改指；⚠️ **只当索引、未逐句核**）：
        //    `decomp_full/BattleManager__ChoiceOfCardPlayer.c` · `BattleManager__ChoiceOfCardEnemy.c`
        //    （选牌的两侧入口）· `BattleManager__ChooseCardMethod.c`（含协程 `_ChooseCardMethod_d__449`）
        //    · `BattleManager__GetChoiceFullPool.c`（候选全集 —— 「候选域」那半条的真权威）。
        //  ⚠️ 原来写「**权威源** `rule_core.gd:1157 _resolve_choose` + `:1041 _chosen_apply`」——
        //    那份 `.gd` 是**我们自己的上一版 Godot 复刻**、**旁证、非权威**（文件头那条）。
        // ==================================================================

        /// <summary>
        /// **选牌**：从某个候选域里挑一张卡，然后对它做一件事。
        ///
        /// 选法是我们自己那一版既定的**单机口径**（`rule_core.gd:1159`（旁证、非权威）函数头：
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
        /// <summary>
        /// **纯读**：这一次选牌的**候选**有哪些。**不掷骰、不改任何状态** —— 表现层开面板用它。
        /// 与 <see cref="DoChooseCard"/> **共用同一份判据**（那边也只调这一个函数），
        /// 免得「面板列出来的候选」和「引擎真会选的候选」两处各写一份（迟早不一致）。
        /// `why != null` = 候选是空的 —— 在这一族里是**正常结局**（规则书 `:233` 允许空候选）。
        /// </summary>
        /// <summary>
        /// 原版 `RawCardScript.IsHidden()` 的**代理**判据 —— 「这张卡该不该出现在随机/选择池里」。
        ///
        /// <para>🆕 **2026-10-25（完全复刻 · 执行代理 A · 账 2b）** 新加。⚠️ **判据不全，如实标**：
        /// `RawCardScript__IsHidden.c` **不在**反编译集里（`d:/2/tools/decomp_full/` 与 ilspy 全树都没有），
        /// 只能拿一条**可见的**代理：卡名带 `(tutorial)`。
        /// 现读卡池里这一类就是**教程装订的督军副本**（`Marneus Calgar (tutorial)` /
        /// `Uriel Ventris (tutorial)`），正是原版最该藏起来的那批。</para>
        /// <para>⛔ 别把它当「原版就是按卡名判」—— 真判据拿到之后**就地替换**这一处（判据只此一处）。</para>
        /// </summary>
        static bool IsHiddenCard(CardDef c)
        {
            if (c == null) return true;
            string n = c.Name;
            return !string.IsNullOrEmpty(n)
                && n.IndexOf("(tutorial)", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static List<CardDef> ChooseCardCandidates(BattleContext ctx, int owner, EffectOp op,
                                                         out string srcName, out string detail, out string why)
        {
            var ps = ctx.Players[owner];
            var foe = ctx.Players[1 - owner];

            // ---- 候选域（`rule_core.gd:991 _choose_candidates` 的五个来源 ＋ 我们补的两个）----
            //   🆕 第 6 个 `played`（2026-10-17 B14）· 第 7 个 `specific`（2026-10-19 `A1960`，
            //   原版 `choicesDefinition.filterSpecificCards`）—— 前者在下面 `switch` 里，
            //   后者在它**之前**就返回了（按 id 点名、不能再过文本筛选）。
            // 🔴 第 7 行第 2 步：区域里存的是**实例**，而这一层要的是「有哪些**卡**可选」
            //    ⇒ 区域**投影成卡模板表**（逐份投影、**保留重复**：和改之前那份 `List<CardDef>`
            //    同长同序，所以候选筛选与随机序列都没变）。选完「要哪一张」之后，
            //    再由 `RemoveFromSource` 去区域里**摘出对应的那一份实例**。
            List<CardDef> source = null;          // null = 全卡池（`pool`）
            // 🆕 2026-10-19（`A1960`）：**第 7 个候选域 `specific`** —— 原版
            //   `choicesDefinition.filterSpecificCards`（`BattleManager__GetChoiceFullPool.c:36-49`：
            //   `targetsAffected == 0xb4(180)` ⇒ 候选池就是这张表）。**先于通用 `FilterChoose` 返回** ——
            //   它是**按 id 点名**的，不是文本筛选（再筛一遍会把池外变体卡与同名项筛错）。
            if (op.ChooseSrc == "specific")
            {
                srcName = "原版 choicesDefinition 指定的卡";
                var ids = op.ChooseSpecificIds;
                if (ids == null || ids.Count == 0)
                {
                    // 🔴🆕 **2026-10-25（完全复刻 · 执行代理 A · 账 2b）：按原版补上「回落全卡池」那一段。**
                    //   判据（第一权威，现读）= `AbilityLogic.GetTargets`（`AbilityLogic.cs:723-…`）的
                    //   `targetsAffected == choiceResolution` 分支：`filterSpecificCards` 为空时
                    //   **遍历全卡池**，逐张三道闸：
                    //     ① `FilterMethods.CheckIfRawCardMeetsCriteria(rawCard, criteria, manager, owner)`
                    //     ② `!rawCard.cantBeRandomlyCreated`       ← 卡表 1128 张里 **167 张 = 1**
                    //     ③ `!rawCard.IsHidden()`
                    //   ⇒ 这是 `cantBeRandomlyCreated` 在**原版里唯一的运行期读取点**。
                    //   ⚠️ **① 我们只做得到「卡模板级」的那一子集**：`CardCriteria.FromTarget(op.Target)`
                    //      （兵种 / 关键词 / 卡名 / spellType / 督军），原版 `CheckIfRawCardMeetsCriteria`
                    //      还有 rarity / cost / expansion 等格 —— 判据不全 ⇒ **这里如实出声**
                    //      （`ctx.Log` 一行），⛔ 不假装筛齐了。
                    //   ⚠️ **② 的 `IsHidden()`**：原版那个谓词的方法体我们**没读到**
                    //      （`RawCardScript__IsHidden.c` 不在反编译集里）⇒ 按「卡名 `(tutorial)` 结尾」
                    //      这一条**可见的**代理做（现池该类卡都是教程装订的督军副本），并在日志里标明是代理。
                    //   ⚠️ **这一段今天【还不可达】**：注入 `specific` 的闸在
                    //      `CardDef.HasSpecificCardChoice`（要求 `filterSpecificCards` 非空）——
                    //      放开它要先逐张核「这张卡的效果真的会消费选中那张」，否则就是
                    //      「开一个选了不生效的面板」（本仓红线）。⇒ 本笔只把**池**建对、把闸留着。
                    var crit = CardCriteria.FromTarget(op.Target);
                    var pool = new List<CardDef>();
                    int skippedRandom = 0, skippedHidden = 0, skippedCrit = 0;
                    foreach (var c in ctx.CardPool)
                    {
                        if (c == null) continue;
                        if (c.CantBeRandomlyCreated) { skippedRandom++; continue; }        // ②
                        if (IsHiddenCard(c)) { skippedHidden++; continue; }               // ③（代理判据）
                        if (crit != null && !crit.Matches(c)) { skippedCrit++; continue; } // ①（子集）
                        pool.Add(c);
                    }
                    srcName = "原版 `choiceResolution` 的**全卡池回落**";
                    detail = "全池 " + ctx.CardPool.Count + " 张 → " + pool.Count + " 张"
                           + "（剔：`cantBeRandomlyCreated` " + skippedRandom
                           + " · 隐藏 " + skippedHidden
                           + (crit != null ? " · 准则 " + skippedCrit : " · 准则（本卡没写）")
                           + "）";
                    why = "① 准则只做到卡模板级子集（兵种/关键词/卡名/spellType/督军），"
                        + "原版还有 rarity/cost/expansion 等格未覆盖；③ `IsHidden` 用的是"
                        + "「卡名以 `(tutorial)` 结尾」这条**代理**判据（原版方法体未读到）";
                    ctx.Log($"[choose] 「{op.Source}」按原版 `AbilityLogic.GetTargets` 的 choiceResolution 分支"
                          + $"回落全卡池：{detail}；{why}");
                    return pool;
                }
                var byId = new Dictionary<string, CardDef>(System.StringComparer.Ordinal);
                foreach (var c in ctx.CardPool)
                    if (c != null && !string.IsNullOrEmpty(c.Id)) byId[c.Id] = c;
                var list = new List<CardDef>();
                var outside = new List<string>();
                foreach (var id in ids)
                {
                    CardDef hit;
                    if (byId.TryGetValue(id, out hit)) list.Add(hit);   // 重复项照原版保留（同 id 出现两次）
                    else outside.Add(id);
                }
                detail = "原版指定 " + ids.Count + " 张 · 池内命中 " + list.Count
                       + (outside.Count > 0
                          ? " · **池外 " + outside.Count + " 张**（变体卡 SO 等，如实跳过："
                            + string.Join("、", outside.ToArray()) + "）"
                          : "");
                if (list.Count == 0)
                {
                    why = "`choicesDefinition` 指定的卡**一张都不在卡池里** —— 这条选牌没法问";
                    return new List<CardDef>();
                }
                why = null;
                return list;
            }
            switch (op.ChooseSrc)
            {
                case "deck":      source = ps.Deck.ConvertAll(h => h.Card);  srcName = "自己牌库";   break;
                case "hand":      source = ps.Hand.ConvertAll(h => h.Card);  srcName = "自己手牌";   break;
                case "enemyhand": source = foe.Hand.ConvertAll(h => h.Card); srcName = "对手手牌";   break;
                case "dead":
                    source = DeadCandidates(ctx, owner, op.ChooseDeadScope);
                    srcName = op.ChooseDeadScope == "since_last_turn"
                            ? "自你上个回合起阵亡的部队" : "本局阵亡的部队";
                    break;
                // 🆕 2026-10-17（B19）：`… you played this game`（`Suppressor` 的 `Oath 1:`）——
                //   候选域 = **本局你打出过的牌**（记账表 = `BattleContext.PlayedCards`）。
                //   🔴 B14 那一版这里**返 0 + 如实报**（⛔ 不拿全卡池顶替 —— 加它之前的行为是
                //      候选 = 全卡池 349 张、而且**一句错都不报**）；本批把**真域**接上了。
                //   ⚠️ **顺序 = 最近打出的在前**（表是追加的 ⇒ 倒着走）。这个顺序**有意义**：
                //      出了这个 switch 之后，`played` 域**只取第一张**（见函数末尾那条判别）。
                case "played":
                {
                    var mine = new List<CardDef>();
                    for (int i = ctx.PlayedCards.Count - 1; i >= 0; i--)
                    {
                        var pc = ctx.PlayedCards[i];
                        if (pc.Owner != owner || pc.Card == null) continue;
                        mine.Add(pc.Card);   // 逐份投影（同名打过两次 ⇒ 两个格，与原版日志同口径）
                    }
                    srcName = "本局你打出过的牌";
                    if (mine.Count == 0)
                    {
                        // ⚠️ 与「有牌、但一张都不合筛选」**分开说** —— 后者由 `FilterChoose` 自己报
                        //    （`why = 候选里没有符合「…」的卡`）；两种情况要查的地方完全不同。
                        detail = null;
                        why = "本局你**一张牌都还没打出过**（卡面 `you played this game` 的候选域是空的）";
                        return new List<CardDef>();
                    }
                    source = mine;
                    break;
                }
                default:          srcName = "全卡池"; break;
            }

            IReadOnlyList<CardDef> candsIn = source != null ? (IReadOnlyList<CardDef>)source : ctx.CardPool;
            var outp = CreatePool.FilterChoose(candsIn, ctx.CardPool, op.ChooseWhat, out detail, out why);

            // ---- `played` 域：**至多一张**（原版只给「最近打出的那一张」）----
            // 🔴 **判据 = 反编译，两条互相独立的证据吻合**（细节见
            //    `资料/普查产出_1017/W_B19_本局打出过的牌.md` §一）：
            //   ① **指令流**：`CemeteryManager.GetCardsPlayed`（VA `0x18061BC70`）从日志**末尾往前**扫，
            //      遇到第一张合条件的就 `List.Add` —— 而 `0x18061BE1B`（`call List.Add` 那条）之后
            //      **没有任何回跳**，直接落进 `0x18061BE20` 的公共尾声 ⇒ **列表里至多一项**；
            //   ② **枚举名**：`TargetsAffected.lastPlayedIfMeetsCriteria`（= 190；
            //      `AbilityLogic__GetTargets.c:173` 那支）字面就是「**最近**打出过的那张（若合条件）」。
            //   ⇒ 同一张打过多次、或打过好几张不同的 ⇒ 候选**仍然只有一张**（最近打出的那一张）。
            //   ⚠️ **卡面写的是 `Choose a …`**（看着像多选）—— 与原版行为**冲突**时取**原版实现**
            //      （铁律 10 第 3 条：卡面是印刷品，行为看代码）；这条冲突如实记在报告里。
            //   ⚠️ 要改成「全量」就删掉下面这一句 —— 表里本来就留着**全部**打出过的牌。
            if (op.ChooseSrc == "played" && outp.Count > 1)
                outp.RemoveRange(1, outp.Count - 1);

            return outp;
        }

        static bool DoChooseCard(BattleContext ctx, int owner, string by, EffectOp op,
                                 UnitState chosen, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            var foe = ctx.Players[1 - owner];      // `shuffle` / `enemyhand` 两个分支要它

            // ---- ① 候选域（判据只有 `ChooseCardCandidates` 一处）----
            string srcName, detail, why;
            var cands = ChooseCardCandidates(ctx, owner, op, out srcName, out detail, out why);
            if (why != null)
            {
                // 「候选是空的」在这一族里是**正常结局**（规则书 :233 允许空候选），
                // 但**卡面写了要做的事没做**，所以照样如实报出来 —— 不许静默空过。
                ctx.Log($"{by}：「{op.Source}」在{srcName}里没得选 —— {why}（**这条没生效**）");
                unresolved.Add(op.Source + "（选牌：" + why + "）");
                return false;
            }

            var pick = TakePickCard(ctx, cands, by, op.Source + "（选牌）", unresolved);

            // ---- ①b 🔴🆕 2026-10-23（`W63·审查-日志3键`）：**从敌方手牌里选牌 ⇒ 亮出那一张** ----
            //  原版判据（全量反编译）：`CemeteryManager__AddDisplayedCardActionToCemetery.c`
            //    （动作类型 `local_1e8 = 0x1e` = **30 = `displayedCard`**）由
            //    `BattleManager._ResolveDisplayFromHand_d__548__MoveNext` 落一行，发起者 = 能力
            //    opcode **`0x136`(310)**（`AbilityLogic__PlayAbility.c:2225-2240` →
            //    `BattleManager__AddDisplayFromHand.c:23` 入队）。
            //  我们这边「从敌方手牌里挑一张」只有 `choosecard` 的 `enemyhand` 支这一个入口
            //    （`ChooseCardCandidates` 的 `enemyhand` 档），它在本模型里的**唯一可观察结果**
            //    就是「对手手里那张牌被亮出来了」⇒ 在**选定那一刻**发一条。
            //  ⚠️ **载体是借的（如实标着）**：`EvtKind` 里没有 `displayedCard` 这一档，而枚举只许
            //    追加（属引擎侧另一笔账）⇒ 借 `EvtKind.Draw` + `Effect == "display"`；渲染侧
            //    `BattleDriver` 的 `case EvtKind.Draw` 已按这个子标签分两支
            //    （`ActionDisplay{Your,Enemy}CardInHand`，2026-10-22 写完并断言）。⛔ 别在别处再造信号名。
            //  `Player` = **看牌那一方**（= 这条能力的控制者 `owner`，不是被看的那一方）；
            //  `CardId` = 被亮出的那一张；`slot = -1`（它在手牌里、不在棋盘上）。
            if (op.ChooseSrc == "enemyhand" && pick != null)
                ctx.Emit(EvtKind.Draw, owner, -1, pick.Id, effect: "display");

            // ---- ② 引用位：原版 `:1151-1152` **两个都写** ----
            // `LastCreated` 接通已有的 `(指代上一张)`（`Lower its cost by N` / `It costs N less`）；
            // `LastChosenCard` 给「复制选中那张」用（那类指代对象在**手牌/牌库里**，
            // 而 `LastTarget` 是 `UnitState`，够不着）。
            // 🔴 第 7 行第 3 步：两个槽装的都是**实例** ——
            //    来源是区域就指向**区域里的那一份**（不取走）；来源是卡池（凭空生成）才新发一份
            //    （后面 `Draw it` / `lower its cost` 才有东西可指）。
            // ⚠️ **下面那些动作分支必须复用同一个 `pickedInst`**（`?? pickedInst`）——
            //    各写一次 `ctx.NewInstance(pick)` 的话，进手牌的是**另一份**幽灵，
            //    而减费/加费钉在这一份上 ⇒ **静默不生效**（实测踩过：`It costs 2 less` 挂空）。
            var pickedInst = FindZoneInstance(ctx, owner, op.ChooseSrc, pick) ?? ctx.NewInstance(pick);
            ctx.LastCreated.Clear();
            ctx.LastCreated.Add(pickedInst);
            ctx.LastChosenCard = pickedInst;

            // ---- ②b 规则书英文版 `:477`：`choose` 的**没被选中的候选要归还，并把牌库洗掉** ----
            // 原文：「Whenever a card uses the word "choose", randomly select **3** cards from the set
            // of possibilities and the player chooses which one to keep/draw/resolve.
            // **Return unchosen cards drawn from your deck and shuffle.**」
            // 单机自动选 1 时，「从全集等概率取 1」与「洗牌取前 3 再选 1」**分布相同**，
            // 但「牌库被洗过」是**可观察结果**，不能省掉。
            // ⚠️ 只在**来源是牌库**时洗 —— 那半句的限定词正是 `drawn **from your deck**`。
            if (op.ChooseSrc == "deck") Shuffle(ps.Deck, ctx);

            string act = op.ChooseAct ?? "";
            string what = string.IsNullOrEmpty(op.ChooseWhat) ? "任意卡" : op.ChooseWhat;

            switch (act)
            {
                // 动作在**下一句**里（`Choose a troop from your deck. Draw it and …`）——
                // 那一段自己就能解析，本 op 的活到此为止。
                case "":
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」"
                          + $"（{what}；{detail}）—— 后续句接着结算");
                    // 🆕 2026-10-17（B14）：**从对手手牌挑一张、卡面又没有后续动作**的
                    //   （`Reconnaissance Mission`：`Choose a card in your opponent's hand.`
                    //    + `When played, gain 3 Quest Points`）—— 本版**只挑出来**，什么都不改。
                    //   ⚠️ **原版语义仍【未查到】**（成品卡图上门就只有那一句 + `gain 3`；
                    //      原版卡效果数据在远端 CCD，本地没有）—— 所以这里**只出声、不发明语义**：
                    //      上一句日志说的「后续句接着结算」在这张卡上是**假的**（后一句是 `gain 3`，
                    //      跟挑出来那张牌无关），不说清楚下一个会话会以为挑完有事发生。
                    if (op.ChooseSrc == "enemyhand")
                        ctx.Log($"{by}：「{op.Source}」—— **本版只挑出来了，没做任何动作**"
                              + "（原版语义未查到：卡面只有这一句，没有后续动作；见"
                              + "`资料/选牌_数据与规格.md` §甲·七）");
                    return true;

                // 🆕 2026-10-17（B14）：`Choose a card in your opponent's hand and increase its
                //   cost by 1`（`Jackal Scout` 的 `Rally:`）—— **给挑中那一张加价**。
                //   判据 = **成品卡图上的卡面原文**（`d:/2/Warpforge部队卡片/Genestealer Cult/3部队/
                //   Warpforge_03_Jackal-Scout.png`，2026-10-17 亲读：「… and increase its cost by 1」）。
                //   ⚠️ 原版效果数据在远端 CCD ⇒ **没有反编译侧的佐证**；卡面即最高判据（铁律 7）。
                //   落法：`CostMod` **钉到那一份实例**上（`Key="*"` + `HandInstanceId`，见
                //   `CostMod.HandInstanceId` 的配方），`HandOf` = **那张牌躺在谁手里** ——
                //   加的是**对手手里那一张**的价（加给对手的牌，`Sabotage cards in the enemy hand
                //   cost 1 more` 那条同一条路）。
                //   ⚠️ **没写时长**（卡面没有 `this turn`）⇒ 永久（跟着那张牌走）。
                case "inccost":
                {
                    if (op.ChooseSrc != "enemyhand" && op.ChooseSrc != "hand")
                    {
                        ctx.Log($"{by}：「{op.Source}」要加价，但候选域「{op.ChooseSrc}」不是手牌 —— "
                              + "**这条没生效**");
                        unresolved.Add(op.Source + "（inccost：候选域不是手牌）");
                        return false;
                    }
                    // 那一份**就在那个人的手里**（`FindZoneInstance` 是「来源 → 区域」的唯一判据）
                    var pin = FindZoneInstance(ctx, owner, op.ChooseSrc, pick);
                    if (pin == null)
                    {
                        ctx.Log($"{by}：「{op.Source}」要加价，但在{srcName}里找不到「{pick.Name}」"
                              + "**那一份** —— **这条没生效**");
                        unresolved.Add(op.Source + "（inccost：找不到那一份实例）");
                        return false;
                    }
                    int handOwner = op.ChooseSrc == "enemyhand" ? 1 - owner : owner;
                    int inc = op.ChooseCostDelta > 0 ? op.ChooseCostDelta : 1;
                    ctx.CostMods.Add(new CostMod
                    {
                        // `HandOf` 才是判据（`CostModApplies` 里 `HandOf >= 0` 时只看它）；
                        // `Player` 一并写成那一方，免得将来有人只读 `Player` 时看岔。
                        Player = handOwner,
                        HandOf = handOwner,
                        Key = "*",                 // ⛔ 别写卡 id —— 那样同名副本会被一起加价
                        HandInstanceId = pin.Id,
                        Delta = inc,
                        ExpireTurn = -1,
                    });
                    ctx.Log($"{by}：「{op.Source}」把{srcName}里的「{pick.Name}」**加了 {inc} 费**"
                          + $"（钉在那一份上，id={pin.Id}；永久）");
                    return true;
                }

                case "hand":
                case "draw":
                {
                    // 第 7 行第 2 步：来源是区域 ⇒ **沿用摘下来那一份**；来源是卡池 ⇒ 新造一份
                    var moved = RemoveFromSource(ctx, owner, op.ChooseSrc, pick) ?? pickedInst;
                    // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）：进手牌走「先判上限、再 Add」。**
                    //   满了 ⇒ 这份**刚摘下来的**进弃牌堆、手牌一张不动
                    //   （原版 `_AddCardNotDrawnToHand_d__39:26-30` 同形；⛔ 别再 `Add` 完再 `EnforceHandLimit`）。
                    bool landed = RuleCore.AddCardToHandWithinLimit(ctx, owner, moved);
                    // `draw` 要记进「本次结算抽到的牌」—— `For each troop drawn …` 数这个
                    // （和 `DoDraw` / `DoDrawType` 同一条路；⚠️ 它还按卡模板记，第 3 步改实例）
                    // ⚠️ 只记**真进了手牌**的（与 `DoDraw` 同形）。
                    if (act == "draw" && landed) ctx.DrawnThisResolve.Add(moved);   // 第 7 行第 3 步：记那一份
                    // 🆕 **2026-10-18（第三会话 · `A992` 遗留乙）：手牌那一侧的代词槽也要填**
                    //    —— 与 `DoDraw` 同形（`Clear()` + 逐个 `Add` + 单数槽「恰好一张才填」，
                    //    判据只此一处 = `DoDraw` 那一段，⛔ 别自创形状）。理由与「今天零可观测差异」
                    //    见 `DoDrawType` 那一段逐条注释。
                    //    ⚠️ **只在 `act == "draw"` 这一支填** —— 与上面那句 `DrawnThisResolve`
                    //      同一个分支（本步的口径就是「这两条对称」）。
                    //      `act == "hand"`（`put/add/create/return it in your hand`）**不填**：
                    //      它进手牌的理由与「抽」不同，而且单数 `it` 那一族走的是
                    //      `ctx.LastChosenCard`（`DoChooseCard` 自己写的），**判据未给出** ⇒ 如实留空、
                    //      由 `HandReferents` 的兜底兜着，⛔ 不替卡面做决定。
                    if (act == "draw")
                    {
                        ctx.LastHandTargets.Clear();
                        if (landed) { ctx.LastHandTargets.Add(moved); ctx.LastHandTarget = moved; }
                        else ctx.LastHandTarget = null;   // 没落手 ⇒ 槽里不留「刚进手牌的」
                    }
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」"
                          + $"（{what}）→ {(act == "draw" ? "抽上手" : "放入手牌")}"
                          + (landed ? "" : "**（手牌已满 ⇒ 进弃牌堆）**"));
                    return true;
                }

                case "deploy":
                {
                    var moved = RemoveFromSource(ctx, owner, op.ChooseSrc, pick) ?? pickedInst;
                    // 🔴 **2026-10-19（第五十五会话 `E12` · `A2143` 的「敌方手牌」那一格）：落点 = 【那张牌的主人】。**
                    //   判据 = 原版 `BattleManager._ResolveCardAutoSummoned_d__511__MoveNext.c`：
                    //     · `:74/92/101` 三处都读 **`*(char*)(card + 0x40)`**（`EntityScript.isPlayer`，
                    //       `dump.cs:21973`）⇒ `isPlayer == 0` 取 `+0xe8`（**enemyMinionManager**）、
                    //       非 0 取 `+0xe0`（`playerMinionManager`，字段名见 `dump.cs:30742-30743`）；
                    //     · `:110` / `:128` 两个演出支：敌方牌 = `PlayEnemyCard`（`DisplayEnemyCardInCenter`
                    //       ＋ `TurnCardAround`），本机牌 = `PlayPlayerCard`。
                    //   ⇒ **原版没有「从敌方手牌打到自己场上」这条路**：牌从谁的手里拿出来，
                    //     就落在**谁**的棋盘上。（`GOF93 Hunta Rig` 的「部署它」= 把对面手里的部队
                    //     强行摆到**对面**场上，下一句 `This troop attacks it` 才是自己打它。）
                    //   ⇒ `DeployFree` 的 `owner` 用 **`1 - owner`**；`fromHand: true`（原来漏了）——
                    //     这条正是原版 `autoPlayFromHand(250)` 那一档（GOF93 的 SO = `chosenAbility 250`，
                    //     `targetCriteria{targetsAffected:50, enemyUnits, selectFromChooseResolution:1}`）：
                    //     触发 Rally、兑现手牌加成、`DeploySource` = `AutoPlayFromHand`。
                    //   ⚠️ **不加「敌方手牌」这个 `DeploySource` 档** —— 原版的「谁的牌」就是 `Player`
                    //     （= 牌的 `isPlayer`），事件里已经带着；再加一档 = 同一条规则的第二个表示。
                    //     表现层挑 `PlayEnemyCard` / `PlayPlayerCard` 的判别式 = **`e.Player == 本机座位`**。
                    int deployOwner = op.ChooseSrc == "enemyhand" ? 1 - owner : owner;
                    bool fromHand = op.ChooseSrc == "enemyhand" || op.ChooseSrc == "hand";
                    int slot;
                    if (!DeployFree(ctx, deployOwner, moved, out slot, fromHand: fromHand))
                    {
                        ctx.Log($"{by}：「{op.Source}」选了「{pick.Name}」但场上没空格 —— **这条没生效**");
                        unresolved.Add(op.Source + "（选牌部署：场上没空格）");
                        return false;
                    }
                    // 「刚部署的那个」= 后续 `and give **it** …` 的指代对象
                    var dep = ctx.Players[deployOwner].Board[slot];
                    if (dep != null)
                    {
                        ctx.LastTargets.Clear();
                        ctx.LastTargets.Add(dep);
                        ctx.LastTarget = dep;
                    }
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」（{what}）→ "
                          + $"部署到{ctx.Players[deployOwner].Name}的槽 {slot}"
                          + (deployOwner != owner ? "（原版 `isPlayer` 那一支：牌主 = 落点）" : ""));
                    return true;
                }

                case "decktop":
                {
                    var moved = RemoveFromSource(ctx, owner, op.ChooseSrc, pick) ?? pickedInst;
                    ps.Deck.Add(moved);         // 牌库**顶** = 列表末尾（`Draw` 从末尾 pop）
                    ctx.Log($"{by}：「{op.Source}」从{srcName}选了「{pick.Name}」（{what}）→ 放到自己牌库顶");
                    return true;
                }

                case "todeck":
                {
                    // 🔴 **2026-10-19（第五十七会话 `W57` · DA-3）：`DA44 Secret Agenda`
                    //   `Choose a Dark Angels Secret and add it to your deck` —— 改前是
                    //   `Add + Shuffle(整副)` 且**不发** `CreatesSecret`；原版不是这么走的。**
                    //
                    //   **判据（SO 真值 + 反编译）**：`DA44` 的 `cardAbilities[0].abilityLogic[0]
                    //   .chosenAbility = 270`（= `createCardInDeck`，`cards_raw.json` 实读）；
                    //   `AbilityLogic__PlayAbility.c:2607` 的 270 支 → `BattleManager__AddNewCardToDeck`
                    //   → 与「任务点满阈值造隐秘」**同一条 `AddCardInPlayToDeck` 路径**：
                    //     · 随机下标 `Insert`（`BattleManager__AddCardInPlayToDeck.c:36-37,63-64`，
                    //       整副牌序**保留**）；
                    //     · `spellType == 0x96(150)` ⇒ `BroadcastSecretCreated`
                    //       （`_ResolveCreateDeckCard…:113-119`）。
                    //   ⚠️ 原版两处差别都是**可观测**的：① 牌序（洗整副 vs 随机插入）；
                    //      ② `DA15` / `DA66`（`When you create a Secret, …`）**根本不响**。
                    //   ⚠️ 本版 `todeck` 只有 DA44 一张（全池按 `to your deck` 且不含 `return` 扫过）。
                    //   ⛔ `return`（`return it to your deck`）**仍走下面的 B11 洗回牌库** —— 两类分开。
                    var moved = RemoveFromSource(ctx, owner, op.ChooseSrc, pick) ?? pickedInst;
                    InsertIntoDeckRandom(ctx, owner, moved);
                    ctx.Log($"{by}：「{op.Source}」把「{pick.Name}」随机插入自己牌库（{what}）");
                    // 原版 `_ResolveCreateDeckCard…:113-119` 读的是**造出来那张的 `spellType`**。
                    // 与 `QuestPointThreshold` 同一份判据（`CreatePool.MatchesKind`），只是那边造的一定是隐秘。
                    if (CreatePool.MatchesKind(pick, "secret"))
                    {
                        // 🔴 2026-10-26（批次 8 · `B8_AE`）：原版 `BroadcastSecretCreated.c` 全文
                        //   `GetCardsInHandRef` = 0 ⇒ 只跑棋盘跳（判据与 `DoCreate` 的那两条同一份）。
                        BroadcastWhen(ctx, WhenEventKind.CreatesSecret, owner, pick, null,
                                      boardAndPersistentOnly: true);
                        // 🆕 **2026-10-25（执行代理 `X18`）**：成就 `Ach_78` 的第二处写点
                        //   —— 本支（`DA44 Secret Agenda` 的 `to your deck`）与 `DoCreate` 的
                        //   `hand` 支**各自对应原版同一个 `BroadcastSecretCreated` 函数**
                        //   （判据见上面那段：`_ResolveCreateDeckCard…:113-119`）⇒ 计数必须两边都写，
                        //   只在 `hand` 支写会**漏掉 DA44 造的那一张**。归属 = 造牌的施放者。
                        ctx.Tally.AddSecret(owner);
                        ctx.Log($"{by}：「{pick.Name}」是隐秘（`spellType = 150`）⇒ 广播 `CreatesSecret`"
                              + "（原版 `BroadcastSecretCreated`；`DA15` / `DA66` 靠它响）");
                    }
                    return true;
                }

                case "return":
                {
                    // `return` 的来源是**手牌**（`Choose a card in your hand and return it to your deck`），
                    // 但 `ChooseSrc` 已经把它记成 `hand` 了，所以取走这一步两者同路。
                    var moved = RemoveFromSource(ctx, owner, op.ChooseSrc, pick) ?? pickedInst;
                    ps.Deck.Add(moved);
                    Shuffle(ps.Deck, ctx);   // B11「洗回牌库」—— 规则书那族都要求洗
                    ctx.Log($"{by}：「{op.Source}」把「{pick.Name}」洗回自己牌库（{what}）");
                    return true;
                }

                case "shuffle":
                {
                    // `Choose a card in the enemy hand and shuffle it into their deck`
                    // —— 洗进的是**对手的**牌库（`their`）。第 7 行第 2 步：按实例摘一份。
                    var moved = TakeFromZone(foe.Hand, pick) ?? pickedInst;
                    foe.Deck.Add(moved);
                    Shuffle(foe.Deck, ctx);
                    ctx.Log($"{by}：「{op.Source}」把对手手里的「{pick.Name}」洗回对手牌库（{what}）");
                    return true;
                }

                case "enemyhand":
                {
                    var moved = RemoveFromSource(ctx, owner, op.ChooseSrc, pick) ?? pickedInst;
                    // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）**：同样先判上限再 Add
                    //   （满了 ⇒ 进**收牌方**的弃牌堆、它的手牌一张不动）。
                    RuleCore.AddCardToHandWithinLimit(ctx, 1 - owner, moved);
                    ctx.Log($"{by}：「{op.Source}」选了「{pick.Name}」（{what}）→ 放进**对手**手牌");
                    return true;
                }

                case "copies":
                {
                    // 复制品 = **各发一份新实例**（不是选中那一份本身）
                    int n = op.ChooseCopies > 0 ? op.ChooseCopies : 2;
                    var landed = new List<CardInstance>(n);
                    for (int i = 0; i < n; i++)
                    {
                        var copy = ctx.NewInstance(pick);
                        // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）**：先判上限再 Add
                        //   （满了 ⇒ 那份复制品进弃牌堆、手牌不动）。
                        if (RuleCore.AddCardToHandWithinLimit(ctx, owner, copy)) landed.Add(copy);
                    }
                    // 🔴 **2026-10-14（账 `A1989`）就地补：指代槽改指向【刚造出来的那批复制品】。**
                    //    与 `DoCreate` 的 `hand` 支**同形**（那支的 `ctx.LastCreated.AddRange(made)`
                    //    注释写的就是「`They cost 1 less` 指着**那几份**」）——
                    //    而这一支**以前不写**，槽里留的是上面 `:3130` 那份**卡池里凭空发的幽灵**
                    //    （不在任何区域）⇒ 后面那句 `Lower their cost by 1` 会**钉在幽灵上**，
                    //    `CostModApplies` 拿 `HandInstanceId` 一比就落空（`:900`）⇒
                    //    **卡面印的「复制品便宜 1 费」静默不发生**。
                    //    📏 实测（`ruleprobe.sh play GSC29`，2026-10-14）：改前两张复制品仍按
                    //    2 费算（手牌总价 7 → 6 = 只少了出牌那 5 费 + 两张各 2 费），
                    //    日志还写着「刚才造出来的那批（…）登记 **1** 张」= 登的是那个幽灵。
                    //    ⚠️ **判据来自解析层**：`Lower their cost by N` 由 `EffectText.FeedChooseResidue`
                    //    （`A1989`）作为**独立 op** 排在 `choosecard` 之后产出 —— 它的
                    //    `Payload = (指代上一张)` 读的就是本槽。⛔ 别把这两行删了又指望它生效。
                    //    ⚠️ 影响面：`ChooseAct == "copies"` 的卡全池**只有 2 张**
                    //    （`GSC29` / `UM114`），后者**没有后续句** ⇒ 本笔只对 `GSC29` 可观测。
                    ctx.LastCreated.Clear();
                    ctx.LastCreated.AddRange(landed);
                    ctx.Log($"{by}：「{op.Source}」选了「{pick.Name}」（{what}）→ 造 {n} 张复制进手牌"
                          + (landed.Count == n ? "" : $"（其中 {landed.Count} 张落了手，其余手牌已满 ⇒ 进弃牌堆）"));
                    return true;
                }
            }

            // 认不出的动作**绝不默认成 `to_hand`** —— 那是静默的错误语义（原版 `:1193` 就默认了）
            ctx.Log($"{by}：「{op.Source}」的动作「{act}」本版不认识 —— **这条没生效**");
            unresolved.Add(op.Source + "（选牌动作 " + act + " 没实现）");
            return false;
        }

        // ==================================================================
        //  🆕 2026-09-14 T3：**「选一个效果」**（`chooseeffect`）
        // ==================================================================

        /// <summary>
        /// 选效果池里的**一项**。两种形态，二选一（不会同时有）：
        ///   · <see cref="Card"/> 非空 —— 条目是**一张卡**，效果文字只有**一份来源**（读那张卡的 `desc`）
        ///   · <see cref="Payload"/> 非空 —— 条目是**一段载荷原文**，交给 `GivePayload` 解
        /// </summary>
        public sealed class ChooseEffectEntry
        {
            /// <summary>给人看的名字（**UI 做出来之后**要用它当选项标题）。</summary>
            public string Label;
            /// <summary>条目是**一张卡**（卡名）。效果文字从那卡的 `desc` 读 —— 不在这儿抄第二份。</summary>
            public string Card;
            /// <summary>条目是**一段载荷原文**（`+1 armour` / `+2 melee attack` …）。</summary>
            public string Payload;
        }

        /// <summary>
        /// **Leviathan 那两张共用的六项**。
        /// ⚠️ **定义一份、两张卡引用同一份** —— 交接文档明写「两张卡共用同一套选项，
        ///    差别只在作用域」；抄成两份迟早不一致。
        ///
        /// 🔴 **2026-10-19（第五十五会话 E10 · `E9` 对账 β 的 `triggerEffectInCard(260)`）就地订正
        ///    （铁律 5 / 铁律 11）**：原来这里是**用户 2026-09-13 给的三项**
        ///    （`+1 armour` / `+2 attack` / `+2 ranged attack`）。真值（当前 build 的 SO）不是三项 ——
        ///    `Hyper-adaptation`(`TL49`) 与 `Infinite Biomorphologies`(`TL50`) 各自的 260 能力
        ///    指向 **6 张同族 `_enchantment` 卡**（`cards_raw.json`，`m_Name` 直接写着
        ///    `T6 Leviathan-Hyper-adaptation_enchantment1..6` /
        ///    `T12 Leviathan-Infinite Biomorphologies_enchantment1..6`），逐张是：
        ///      · enchantment1 = `armourChange 1`                      （TL50 还带 `doNotStack 1`）
        ///      · enchantment2 = `buffType 3` + `trait 70 (stealth)` + `armourChange 1`
        ///      · enchantment3 = `meleeAttackChange 2`
        ///      · enchantment4 = `rangeAttackChange 2`
        ///      · enchantment5 = `healthChange 2`
        ///      · enchantment6 = `buffType 3` + `trait 980 (vanguard)`
        ///    ⇒ 三项里**两项对得上**（armour 1 / +2 melee / +2 ranged），
        ///      **少** `+2 Health` · `Stealth and +1 Armour` · `Vanguard` 三项。
        ///    判据链：`AbilityLogic@cardAbilities.abilityLogic.chosenAbility 260 = triggerEffectInCard`
        ///    （`资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv:47`）+
        ///    `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:2534` 的 `0x104` 支（把目标逐个收进
        ///    一张表、随后由 `param_1 + 0x80` 消费 —— 即 260 是「触发卡内那几条效果」的包装）；
        ///    `trait 70/980` 的释义见同一份词典 `:224/:236`（`DefinedTrait.cs:8/95`）。
        ///    ⚠️ 池子的**数量**由 `ChooseEffectOptions` 决定，`DoChooseEffect` 三等分/六等分都在
        ///    `TakePick` 一处（面板侧读的是同一份，`BattleDriver` 不需要改）。
        /// </summary>
        static readonly ChooseEffectEntry[] LeviathanEffectPool =
        {
            new ChooseEffectEntry { Label = "+1 Armour",            Payload = "+1 armour" },
            new ChooseEffectEntry { Label = "+2 Melee Attack",      Payload = "+2 attack" },
            new ChooseEffectEntry { Label = "+2 Ranged Attack",     Payload = "+2 ranged attack" },
            new ChooseEffectEntry { Label = "+2 Health",            Payload = "+2 health" },
            new ChooseEffectEntry { Label = "Stealth and +1 Armour", Payload = "stealth and +1 armour" },
            new ChooseEffectEntry { Label = "Vanguard",             Payload = "vanguard" },
        };

        /// <summary>
        /// **「选一个效果」的池子表** —— 键 = **施放的那张卡的名字**（查 `ctx.PlayingCard`）。
        ///
        /// 🔴 **两条池子的出处（都是用户给的，别再清点）**：
        ///   · `Exemplary Warrior` —— 用户 **2026-09-14** 给了卡图位置：
        ///     `d:/2/Warpforge部队卡片/Ultramarines/2天赋/` 的 `Warpforge_00b/00c/00d`
        ///     （三张 **0 费** UM 卡），**卡面已亲读**（铁律 7）。局部互证：这四张在引擎表里
        ///     id 全是自造的、且 **0 副原版预组收录**（它们不是牌组牌，只是天赋 + 它的三个选项）。
        ///     ⚠️ `Catechism of Death` **不在池子里**（用户点名的就是那三张）。
        ///   · `Hyper-adaptation` / `Infinite Biomorphologies` —— 用户 **2026-09-13** 给的三项固定效果。
        ///
        /// ⚠️ **不是 `choosecard`**：那个从卡池/牌库/手牌里筛**卡**，这个从**这张表**里挑。
        /// ⚠️ 表里**没有的卡**用 `chooseeffect` ⇒ 结算层**如实报「没登记池子」**，不静默空过。
        /// </summary>
        static readonly Dictionary<string, ChooseEffectEntry[]> ChooseEffectPools =
            new Dictionary<string, ChooseEffectEntry[]>
        {
            {
                "Exemplary Warrior", new[]
                {
                    new ChooseEffectEntry { Label = "Righteous Fury",      Card = "Righteous Fury" },
                    new ChooseEffectEntry { Label = "Master of Arms",      Card = "Master of Arms" },
                    new ChooseEffectEntry { Label = "Paragon of Ultramar", Card = "Paragon of Ultramar" },
                }
            },
            { "Hyper-adaptation",        LeviathanEffectPool },
            { "Infinite Biomorphologies", LeviathanEffectPool },
            // 🆕 **2026-09-16：`Mekaniak`** —— 卡面 `Give a **Kustom Job** of your choice to a
            //    friendly Vehicle`。`Kustom Job` **不是卡名**，是**同组那三张 0 费 `Ephemeral` 卡**
            //    （三张的目标都写 `a friendly Vehicle`）。
            //    ✅ **2026-10-19（第五十五会话 E11）就地升级（铁律 5）：不再是「结构推断」——
            //       SO 里有**直证**：`Mekaniak`（`cards_raw.json`，`uniqueId 9dcbe3d0…`）的
            //       `relatedCard1/2/3` 三个 PPtr 解出来正好是
            //       `5615906809088092713 = More Dakka` · `1138737746304900322 = Ramshackle` ·
            //       `-8202013711145922002 = Wreckin Ball`（按 `path_id` 在 1,351 张全卡 SO 里命中）。
            //       `relatedCard*` 这一族在反编译方法体里 grep 不到读点（只有字段访问）⇒ 判据是
            //       **数据侧的对象引用**，不是运行期行为；语义判据仍以卡面 + `chooseeffect` 为准。
            //    📦 **原来的判据（2026-09-16，用户拍板，仍然成立、只是不再是唯一一条）**：
            //       卡面编号结构 `Mekboy Gazmek`(25) + `Mekaniak`(25B) + `25C/D/E` 与
            //       `Lieutenant Titus`(00) + `Exemplary Warrior`(00a) + `00b/c/d` **完全同形**，
            //       而后者那三张早已被定为它的池子（2026-09-14 用户裁定）。
            //       ⇒ 两条独立判据指向同一组三张卡。全过程 → `资料/普查产出_1019/E11_报告.md` §二·2。
            //    ⚠️ 解析入口见 `EffectText.TryChooseEffect` 的 ③ 那一支（`ReGiveKustomJob`）。
            {
                "Mekaniak", new[]
                {
                    new ChooseEffectEntry { Label = "More Dakka",    Card = "More Dakka" },
                    new ChooseEffectEntry { Label = "Ramshackle",    Card = "Ramshackle" },
                    new ChooseEffectEntry { Label = "Wreckin' Ball", Card = "Wreckin' Ball" },
                }
            },
        };

        /// <summary>
        /// 「选一个效果」的候选表 —— 按**正在结算的那张卡的名字**查（判据只有这一处，
        /// `DoChooseEffect` 与表现层面板**共用**）。查不到返回 null。
        /// </summary>
        public static ChooseEffectEntry[] ChooseEffectOptions(string cardName)
        {
            ChooseEffectEntry[] pool = null;
            if (!string.IsNullOrEmpty(cardName)) ChooseEffectPools.TryGetValue(cardName, out pool);
            return pool;
        }

        /// <summary>这一条 `chooseeffect` 是不是「给手牌」那种（`Infinite Biomorphologies`）。
        ///
        /// ✅ **2026-10-17（B14）就地订正（铁律 5）**：这句原来写的是
        /// 「**那一版没做**（见 `DoChooseEffect` ②），表现层**别为它开面板**（开了也没用）」
        /// —— **已过期**：引擎侧 **2026-09-16 就做完了**（`DoChooseEffect` ② → `GrantHandBuff`
        /// → `CardInstance.HandEffects`（🔴 **2026-10-18（`G9`）就地订正，铁律 5**：这里原来写的是
        /// `CardInstance.HandBuffOps` —— **那是【兼容视图】、不是存放处**（`CardInstance.HandBuffOps` 的 doc 里明写
        /// 「它**不是**存放处，⛔ 别再往它上面 `Add`」，读它还会 `WarnCompatView` 出声）；
        /// 存放处从 `W4`/`A885` 起 = **`CardInstance.HandEffects`**，逐条判要用里面每一条自己的格子）
        /// → `RuleCore.ApplyHandBuffs`；断言 `RuleEngineTest` ⑧ 两处）。
        /// 判据（本文件同一处）：`DoChooseEffect` 里 `handScope` 那一支**不再 `return false`**。
        /// ✅ **2026-10-17（B24）就地订正（铁律 5）**：这四行原来写着
        /// 「**仍然没做的是【面板那一侧】**：`BattleDriver.ShowAsk` 见到这个谓词就『这一版没做，
        /// 不问了』⇒ 玩家永远选不了那三项，引擎按 `ctx.Rng` 等概率挑」—— **已不成立**：
        /// **A905（2026-10-17）把 `ShowAsk` 里那条短路删掉了**（订正痕迹 → `BattleDriver` 里那条 `A905` 订正痕，grep `A905` 定位），
        /// `hand` 与 `self` / `give` 现在**走同一条面板路**（唯一差别在**结算落点**：给手牌 vs 给目标）。
        /// 判据全文 = `资料/普查产出_1017/W_B22_选牌ask时机.md` §A905（+ §③ 的 A905 判别式）。
        /// ⚠️ `DoChooseEffect` 里 `TakePick` 那条「没人给答案就用 `ctx.Rng` 等概率挑」**照旧保留** ——
        ///    那**不是**「面板没做」，是「玩家/AI 没回答」时的退路（`ctx.ChooseSites > ChooseAnswered` 会报）。
        /// 这个谓词本身**判据不变**（`op.Payload == "hand"`），别删。</summary>
        public static bool ChooseEffectIsHand(EffectOp op)
        {
            return op != null && op.Payload == "hand";
        }

        /// <summary>
        /// **这张卡的正文里，有多少处「本该问玩家」**（按**结算顺序**排列）—— 表现层开面板靠它。
        ///
        /// 🔴 **它和结算顺序必须一致**：`ctx.ChoosePicks` 是「表现层先按顺序填好、引擎结算时依次出队」
        ///    的 ⇒ 这里漏一处或顺序错一处，就会**选错一张**，而且**不报错**。
        ///    两者读的是**同一个** `EffectText.Parse(card.Desc)`，所以顺序天然一致 ——
        ///    真正的风险是「有些 ask 点**不在这条 desc 里**」（事件层的监听正文、
        ///    `When …` 之类）。那些**这一版覆盖不到**，靠 `ctx.ChooseSites &gt; ctx.ChooseAnswered`
        ///    暴露出来（**不许静默**）。
        /// </summary>
        public static List<EffectOp> PlayerChooseOps(CardDef card)
        {
            var r = new List<EffectOp>();
            if (card == null || string.IsNullOrWhiteSpace(card.Desc)) return r;
            // 🆕 2026-10-19（`A1960`）：`card` 必须传 —— 这条 ask 列表与**结算**读的是同一份
            //   `EffectText.Parse(card.Desc, card: card)`；不传的话注入的 `choosecard`
            //   只出现在结算里、面板不问 ⇒ `ChooseSites > ChooseAnswered` 会红（而且后果是随机挑）。
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            if (ops == null) return r;
            foreach (var op in ops)
                // 🔴 **`op.RandomPick` 的一律排除** —— 那是「卡面没写 `choose` ⇒ 引擎随机挑」
                //    （`EffectText` 的 `TryEitherOr` 产的），**本来就不该问玩家**。
                //    2026-09-14 用户口径：只有卡面**明确写了让玩家选**才轮到玩家。
                if (op.RandomPick) continue;
                else if (op.Verb == "choosecard" || op.Verb == "chooseone" || op.Verb == "chooseeffect")
                    r.Add(op);
            // ⚠️ **`become` 已从这里移除**（2026-09-14 用户裁决）：`Hrolf the Ironhowl` 的
            //    `Stratagems in your hand become a Hunting Wolf or Fenrisian Wolf` 卡面没有 `choose`
            //    ⇒ 改成**随机**（`DoBecome` 里掷 `ctx.Rng`），不再开面板。
            return r;
        }

        /// <summary>
        /// `chooseeffect` —— **选一个效果**（2026-09-14 T3）。
        ///
        /// 三种作用域（`op.Payload`，由 `EffectText.TryChooseEffect` 定）：
        ///   · `self` —— 池子里的条目是**整张卡的效果**，各自带自己的目标（`Exemplary Warrior`）
        ///   · `give` —— 条目是**载荷**，给 `op.Target` 挑中的单位（`Hyper-adaptation`）
        ///   · `hand` —— `Infinite Biomorphologies` 的「给手牌里的全部部队」：
        ///     ✅ **2026-09-16 做完**（这一格 2026-10-17（B14）就地订正，原来写「这一版没做」）——
        ///     落点 `GrantHandBuff` + `CardInstance.HandEffects` + `RuleCore.ApplyHandBuffs`
        ///     （🔴 **2026-10-18（`G9`）就地订正，铁律 5**：原来写的是 `CardInstance.HandBuffOps`，
        ///     那是**兼容视图**、不是存放处 —— 存放处 = `CardInstance.HandEffects`）。
        ///     ✅ **2026-10-17（B24）就地订正（铁律 5）**：这一行原来写着
        ///     「🔴 没做的只剩**面板那一侧**（`BattleDriver.ShowAsk` 里 `ChooseEffectIsHand` 那一支）」
        ///     —— **已不成立**：A905（2026-10-17）把那条短路删掉了（痕迹 → `BattleDriver` 里那条 `A905` 订正痕，grep `A905` 定位）
        ///     ⇒ 这一支现在**面板照常弹**（玩家自己从那三项里挑，不再由引擎替他挑）。
        ///     判据全文 = `资料/普查产出_1017/W_B22_选牌ask时机.md` §A905。
        ///
        /// ⚠️ **挑法（原版是玩家从 3 项里选 1）**：UI 做出来之前用 `ctx.Rng` **等概率取 1**，
        ///    与 `DoChooseCard`（选牌）**同一口径** —— 而且同一局可复现（种子固定）。
        ///    ⚠️ **2026-10-17（B24）订正**：下一句「「选效果」面板是独立一件」**已经兑现** ——
        ///    面板 2026-10-17 起**会弹**（A905），所以 `ctx.Rng` 现在只在
        ///    **玩家/AI 没回答**（`ctx.ChoosePicks` 空）时兜底（`TakePick`）。
        ///    ⚠️ **这是我们的挑法，不是原版**（原版是玩家选）—— 「选效果」面板是独立一件，
        ///    见 `资料/选牌_数据与规格.md` §甲·四之二（⚠️ 更正：原来指 `资料/选牌Choose_数据与设计.md`，2026-10-10 已并入）。
        ///
        /// ⚠️ 条目怎么结算**全走已有那条路**（`give` 合成一条 op 交给 `ResolveOne`；
        ///    是卡就把它的 `desc` 解析出来逐条 `ResolveOne`）—— 不另写一份结算。
        /// </summary>
        static bool DoChooseEffect(BattleContext ctx, int owner, string by, EffectOp op,
                                   UnitState chosen, List<string> unresolved)
        {
            // ---- ① 池子按**正在结算的那张卡**的名字查 ----
            string cardName = ctx.PlayingCard != null ? ctx.PlayingCard.Name : null;
            ChooseEffectEntry[] pool = ChooseEffectOptions(cardName);
            if (pool == null || pool.Length == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要**选一个效果**，但"
                      + (cardName != null ? $"「{cardName}」**没登记效果池**" : "**不在结算某张卡的过程里**")
                      + " —— **这条没生效**（不许静默空过）");
                unresolved.Add(op.Source + "（没登记效果池）");
                return false;
            }

            // ---- ② 作用域 = **手牌**（`Infinite Biomorphologies`）----------------------------------
            //   🆕 2026-09-16 **做掉了**。原来这里直接 `return false`，理由写的是
            //   「手牌卡没有实例身份，加成无处可存」—— **那个前提只对了一半**：
            //   这张卡给的是「手牌里**所有**部队」，**每一份都要给** ⇒ 按「卡 + 份数」记账
            //   与「实例身份」**语义等价**（见 `CardInstance.HandEffects` 的注释 ——
            //   🔴 **2026-10-18（`G9`）就地订正，铁律 5**：这里原来指 `CardInstance.HandBuffOps`，
            //   那是**兼容视图**、不是存放处；存放处 = `CardInstance.HandEffects`；
            //   同名两张一起吃到本来就是对的）。
            //   ⇒ 现在**照常挑一项**，挑完落到 `GrantHandBuff`（④-b 那个分支里按 `handScope` 分流）。
            bool handScope = ChooseEffectIsHand(op);

            // ---- ③ 挑一项（有面板答案就用面板的，见 `TakePick`）----
            var pick = pool[TakePick(ctx, pool.Length, by, op.Source + "（选效果）", unresolved)];
            ctx.Log($"{by}：「{op.Source}」选效果 → **{pick.Label}**");

            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"{by}：「{op.Source}」连锁太深，选出来的效果不再结算");
                unresolved.Add(op.Source + "（连锁深度到顶）");
                return false;
            }

            ctx.EffectChain++;
            try
            {
                // ---- ④-a 条目是**一张卡**：把它的正文解析出来、逐条交给**同一个结算器** ----
                if (!string.IsNullOrEmpty(pick.Card))
                {
                    var cd = CreatePool.FindByName(ctx.CardPool, pick.Card);
                    if (cd == null)
                    {
                        ctx.Log($"{by}：「{op.Source}」选中的「{pick.Card}」**在卡池里找不到** —— "
                              + "**这条没生效**");
                        unresolved.Add(op.Source + "（池子里的「" + pick.Card + "」不在卡池里）");
                        return false;
                    }
                    // 🆕 2026-10-19（`A1960`）：`cd` 也传 —— 选中那张卡自己的 `choicesDefinition`
                    //   若要求选牌、而它的正文没写 choose，同样要发一条 `choosecard`。
                    var ops = EffectText.Parse(cd.Desc, out _, out _, card: cd);
                    if (ops == null || ops.Count == 0)
                    {
                        ctx.Log($"{by}：「{op.Source}」选中的「{pick.Card}」正文**解析不出效果** —— "
                              + "**这条没生效**（正文：{cd.Desc}）");
                        unresolved.Add(op.Source + "（「" + pick.Card + "」的正文解析不出来）");
                        return false;
                    }
                    int done = 0;
                    foreach (var o in ops)
                        // 🆕 2026-10-22（`W64` · `B-16`）：这些 op 来自**被选中的那张卡**（`cd`）⇒ 结构化条件按它判。
                        if (ResolveOne(ctx, owner, null, by, o, chosen, unresolved, card: cd)) done++;
                    return done > 0;
                }

                // ---- ④-b 条目是**一段载荷**：合成一条 `give`，复用「批量给」那条路 ----
                //   ⚠️ 这样目标解析 / 载荷解析 / 时长 / 日志**全部复用**，不另写一份。
                //   时长由**卡面**决定：`Hyper-adaptation` 卡面没有 `this turn` ⇒ **永久**（已亲读卡图）。
                //
                // 🆕 2026-09-16：**作用域 = 手牌**（`TL53 Infinite Biomorphologies`）走另一条路 ——
                //   本卡解析出来**没有目标**（`chooseeffect 载荷「hand」 目标[（没写）]`），
                //   走下面那条 `DoGive` 会落到兜底的「己方全体**场上**单位」（`(未写目标：己方全体)`）
                //   ⇒ **加错地方而且不报错**。判据用现成的 `ChooseEffectIsHand`（表现层也是它）。
                if (handScope)
                    return GrantHandBuff(ctx, owner, by, op, pick.Payload, unresolved);
                var gv = new EffectOp
                {
                    Verb = "give",
                    Source = op.Source,
                    Payload = pick.Payload,
                    Target = op.Target,
                };
                return ResolveOne(ctx, owner, null, by, gv, chosen, unresolved);
            }
            finally { ctx.EffectChain--; }
        }

        // ==================================================================
        //  ✅ 2026-10-18（`A885`）· 手牌效果的**唯一挂载点**
        // ==================================================================
        //
        //  **原版判据**（逐句读的方法体，都在 `d:/2/tools/decomp_full/`）：
        //   · **挂**：`CardScript.AddEffect(那张牌, handEffect.cardEffect)`
        //     （`PlayerHand__SetupCardInHand.c:71` · `PlayerHand__AddHandEffect.c:126-135`），
        //     存在**牌自己**的 `+0x108`（`List<CardEffect>`，`dump.cs` 核过）。
        //   · **摘**：`PlayerHand__RemoveHandEffectAt.c:30-40` 遍历整只手牌逐张 `CardScript.RemoveEffect`。
        //   · **核销次数**：`PlayerHand__CardPlayedWithEffects.c:39`（`numberOfUses--`）· `:43-45`（`< 1 ⇒ 摘`）。
        //   · **到期**：`PlayerHand__UpdateCardEffects.c:100`（`untilEndOfTurn`，**不比较谁的回合**）·
        //     `:119-137`（`IsPlayerTurn() != originalIsPlayer` 且 `+0x33`/`+0x35`）·
        //     `:156-190`（`+0x34`，**我们的解析层产不出这一档**）；
        //     **调用点** = `BattleManager__ResolveEndTurn.c:678` 与 `:680`（**两方手牌各一次**、
        //     `endOfTurn = 1`）；`BattleManager__ResolveAction.c:2576/2578` 那次传 `0`
        //     （只处理 `extrinsic` 那一族，我们没复刻）⇒ 落点 = `RuleCore.ExpireHandBuffs`。
        //
        //  **我们这边**：三个登记点（`GrantHandBuff` / `GrantHandBuffForTargets` /
        //  `AttachEffectToHandInstances`）**全部落到下面这一个方法**，各自只负责「谁该挂」。

        /// <summary>🆕 2026-10-18（`A885` ②）**「手牌里的部队」那一档目标规格** ——
        /// 给 `GrantHandBuff` 挂载时当 `targetCriteria` 用（`TL53 Infinite Biomorphologies` 卡面
        /// `… all **troops** in your hand`），后来再由 `RuleCore.SetupCardInHand` 拿它筛**后进手牌**的牌
        /// （原版 `PlayerHand__SetupCardInHand.c:58` 的 `FilterMethods.CheckIfMeetsCriteria`）。</summary>
        static readonly EffectTargetSpec HandTroopCriteria = new EffectTargetSpec
        {
            Raw = "(手牌里的部队)", Side = "own", Kind = "unit", Count = 0, Auto = true,
        };

        /// <summary>
        /// 🔴 **2026-10-18（`A963` ③ · 止血件）：`HandTroopCriteria` 的【按卡名收窄版】——
        /// 「**只匹配它自己那一张**」。**
        ///
        /// **为什么不能直接改 `HandTroopCriteria`**：它是**共用件**（`grep` 实测 3 个调用点 +
        /// `RuleCore.SetupCardInHand` 那一侧消费它登记的记录）——
        /// ① `GrantHandBuff` 的「先登记记录」（`:3169` 那一带）·
        /// ② `GrantHandBuff` 的「贴到【现在】手里那几张」·
        /// ③ `BroadcastHandWhen` 的「手牌监听器 + 正文没写主语」那一跳（本类里，就是本条要收窄的那处）。
        /// ①② 服务的正是卡面写着 **`all Beasts in your hand`** 那一族（`GOF81 Beast Snagga Nob`
        /// 等）⇒ **必须保持「按兵种筛」的宽度**，动了就是把那几张卡改成不给。
        /// ⇒ 按铁律：「不许直接改共用件，要在**调用点**传一个收窄后的新 criteria」。
        ///
        /// **为什么收窄**：本批（`fe239e9`）把「手牌监听器里没写主语的正文」从「洒给全队」
        /// 改成「落到**它自己**身上」（判据 = 原版那张手牌牌自己就是 `acting card`）。
        /// 但那条路登记的**记录**当时还带着 `HandTroopCriteria`（「手牌里的**所有部队**」）
        /// ⇒ 效果会顺着记录**扩散**给**之后进手牌的任何部队**，而卡面只说它自己
        /// （真卡：`BL15 Rubric Marine` 的 `When you draw a card, gain a Dark Pact of Fate`）。
        ///
        /// **收窄成什么**：`NameFilter = 那一份牌自己的卡名` ⇒ 记录只对**同名**的牌成立。
        /// ⚠️ **如实标着这是止血、不是原版口径**：原版那一格是数据里的
        /// `HandEffect.targetCriteria`，而 `BL15` 的判据（原版 `buffHand`(50) 还是 `buffSelf`(40)）
        /// **在本地缺失的 `allcards` bundle 里**（与 `A897` / `A163` 同一类：判据可查、输入本地没有）
        /// ⇒ 我们**不能**照原版改。⇒ 这里只做**最保守的那一半**：
        /// **保住机制**（效果照旧挂在这一份身上、打出时照旧兑现），**只砍掉没有判据支持的扩散**。
        /// ⚠️ **可能比原版少给**（若原版是 `buffHand`，同名之外的同手牌部队也该吃上）。
        /// 📌 **将来怎么补**：`allcards` 到位后读那张卡的 `targetCriteria`；
        ///   若为 `buffHand` ⇒ 把这个调用点换回 `HandTroopCriteria`（一处改动，机制不用动）。
        /// ⚠️ **仍是近似**：`RuleCore.HandEffectFits` 只拿得到 `CardDef`（不是实例）
        ///   ⇒ 「同名多份」区分不了 —— 这是本工程「卡实例身份」那条线的既有边界，不是本件引入的。
        /// </summary>
        static EffectTargetSpec HandSelfCriteria(CardInstance inst)
        {
            // 🔴 **2026-10-23（`W64 B-09`）就地订正（铁律 5）**：本方法原先只收 `cardName`、
            //   用 `NameFilter = 卡名` 收窄，上一段 doc 里那句「`BL15` 的判据（原版 `buffHand`(50)
            //   还是 `buffSelf`(40)）**在本地缺失的 `allcards` bundle 里**」**已过期** ——
            //   `allcards` 2026-10-18 已本地导出，现读 `cards_raw.json`：
            //   `BL6`（旧 id `BL15`）`cardAbilities[0].abilityLogic[0].chosenAbility = 40`。
            //   `AbilityEffect.cs:12`：`buffSelf` = 40、`buffHand` = 50 ⇒ **原版只加它自己那一份**。
            //   ⇒ 从「同名全部」收窄成**按实例身份**（`OnlyInstanceId`）。
            //   ⚠️ 手里两张同名牌时，原版只有被触发的那一份吃得上 —— 旧写法会两张一起吃（偏多）。
            return new EffectTargetSpec
            {
                Raw = "(手牌里的自己)", Side = "own", Kind = "unit", Count = 0, Auto = true,
                NameFilter = inst != null && inst.Card != null ? inst.Card.Name : null,
            };
        }

        /// <summary>
        /// 🆕 2026-10-18（`W4` 整改 · 账 5）：**这一份牌现在属于哪一方的手牌**（**按引用**找）。
        /// 挂在手牌效果登记那一步 —— 要往 `PlayerState.HandEffectRecords`（= 原版
        /// `PlayerHand.activeEffects`）里记一笔，而 `AttachHandEffect` 手上只有实例、没有玩家号。
        /// 🔴 **2026-10-18（`G3`）就地订正（铁律 5）**：这一句原来还缀着
        /// 「（它被 5 个入口共用，签名已经很长了，⛔ **别再往里塞一个 owner 形参**）」——
        /// **`G3` 加了一个**（`ownerForRecord`，**可选**、默认 `-1` = 老行为）。**理由压过那条偏好**：
        /// 「记录记到谁的手牌上」**不能**依赖「这一份牌此刻在不在手里」（见下面那笔真差异），
        /// 而调用方（`GrantHandBuff` / `GrantHandBuffForTargets`）**本来就拿着 owner**。
        /// 找不到（分离实例 / 夹具自己拼的）返回 `-1` ⇒ **不登记**（那时也没有「手牌」这回事）。
        ///
        /// 🔴 **2026-10-18（`W5` · 审查 §5 账 6）核查结论（写在这里，别只活在报告里）**：
        ///   · **「不在任何手牌里的实例」这个情形，原版没有对应物** —— 原版那条记录是**长在
        ///     `PlayerHand` 上**的（`PlayerHand.activeEffects // +0x48`），`AddHandEffect` 由
        ///     **哪一方打出的能力**决定挂到哪一方手上，与「现在手里有没有这张牌」**无关**。
        ///     我们这边 `AttachHandEffect` 的**五个真实入口**全都从 `ctx.Players[owner].Hand` 里取实例
        ///     （`GrantHandBuff` / `GrantHandBuffForTargets` / `AttachEffectToHandInstances` /
        ///      `BroadcastHandWhen` 的自指那一条 / 自检直接调的公开入口）⇒ **真对局里 `HandOwnerOf`
        ///     永远找得到**；找不到的只有**分离实例**（自检夹具自己拼的 `new UnitState(card,…)`）
        ///     —— 那种情形原版压根没有 ⇒ 如实标着、**不为它造一套**。
        ///   ✅ **同一次核查顺带查出的那条【真差异】—— 2026-10-18（`G3`）已修**
        ///     （这一段原来写「另记账（⛔ 别把它读成已修）…**本批没做，如实挂着**」，**已过期**）：
        ///     原版 `PlayerHand.AddHandEffect`（`PlayerHand__AddHandEffect.c:88-139`）是
        ///     **先把记录收进 `activeEffects`（`:90-110`）**、**再**遍历 `currentHand` 逐张贴上去
        ///     （`:112-139`）⇒ **手牌里一张符合条件的都没有时，那条记录照样登记**，
        ///     于是**后来才进手牌的牌**（抽到的 / 造出来的）依旧吃得上。
        ///     我们当时那一侧，`GrantHandBuff` 在「手上一个部队都没有」时**什么都不挂**
        ///     （`n == 0` 直接 `return false` + `unresolved`）⇒ **那条记录不存在**
        ///     ⇒ 之后的来客吃不上（`Beast Snagga Nob` 的 `At the end of your turn,
        ///     give +1 to all Beasts in your hand` **手里当时没有野兽**时就落进这一支）。
        ///     **怎么修的**：<see cref="RegisterHandEffectRecord"/> 把原版那**第一步**单独做出来
        ///     （**无条件**登记、**不需要任何载体**），`GrantHandBuff` / `GrantHandBuffForTargets`
        ///     改成「**先登记记录 → 再遍历当前手牌逐张贴**」；`AttachHandEffect` 于是收得下
        ///     `inst == null`（= 只登记、不贴牌）。判据与断言见那两个方法。
        ///     ⚠️ 这一笔之后，`HandEffectRegistry` 读到的就是**可能有「无载体」记录**的表
        ///     （它读的本来就是记录表 —— `PlayerState.HandEffectRecords`）。
        /// </summary>
        static int HandOwnerOf(BattleContext ctx, CardInstance inst)
        {
            if (ctx == null || inst == null) return -1;
            for (int p = 0; p < 2; p++)
            {
                if (ctx.Players == null || ctx.Players[p] == null) continue;
                var h = ctx.Players[p].Hand;
                if (h == null) continue;
                for (int i = 0; i < h.Count; i++)
                    if (ReferenceEquals(h[i], inst)) return p;
            }
            return -1;
        }

        /// <summary>
        /// **把一条手牌效果挂到【那一份】牌上** —— 原版 `CardScript.AddEffect(card, cardEffect)`。
        ///
        /// 🔴 **2026-10-18（`A885`）：挂在 `CardInstance` 自己身上**（`HandBuffOps` 那一族字段），
        ///    ⛔ **不再挂到对局级的表上**（原来那张 `BattleContext.HandBuffs` 已删）。理由与判据
        ///    写在 `CardInstance.HandBuffOps` 的注释里（一句话：原版那效果**就长在牌上**，
        ///    打出时跟着上场 —— `RemoveCardFromHand.c:20` 只是把牌从手上摘掉、不销毁不重建）。
        ///
        /// ⚠️ **载荷在这一刻不解析**（三个登记点同一条纪律）：只留原文，兑现时交给 `give` 那条路 ——
        ///    载荷词表**只有 `GivePayload` 一份判据**，别在这儿再写一遍。
        /// ⚠️ **`dedupSamePayload`**：原版 `CardScript.AlreadyContainsEffect` 挡的是「**同一条
        ///    `cardEffect` 已经在这张牌上**」（`PlayerHand__SetupCardInHand.c:65` ·
        ///    `CheckEffectsOnNewCard.c:37`）—— **只有 `AttachEffectToHandInstances` 那条路要它**；
        ///    另外两条（`chooseeffect` 给手牌 · `in play and in hand`）是「每次结算各算一次」的常驻效果、
        ///    **必须叠加**（`Beast Snagga Nob` 撑三个回合结束 = 三份：
        ///    `RuleEngineTest.TestBeastbossAndPayloadSegments` ④）。
        /// </summary>
        /// <param name="uses">同一次挂载发给多张牌时**共用同一个盒子**（原版额度记在 `HandEffect`
        /// 记录上、不在牌上）；`null` = 不限次。见 <see cref="CardInstance.HandBuffUses"/>。</param>
        /// <param name="extrinsic">🆕 2026-10-18（`A885` ②）：原版 `CardEffect.extrinsic // +0x30`
        /// —— 「这条效果**系在正在场上那个来源身上**」（tooltip `Effect is tied to acting card,
        /// not target card`；写点 `AbilityLogic__PlayAbility.c:1197/:1354`：
        /// `cardEffect.extrinsic = abilityData.whileInPlay || abilityData.whileInPlayVariable`）。
        /// 🔴 **2026-10-18（`W4` 整改）· 就地订正（铁律 5）**：这一格原来写着「生产者判据 =
        /// **挂载这一刻有没有「正在行动的单位」**（`ctx.ActingUnit != null`）」—— **那是错的**，
        /// 而且是**方向性地偏向误摘**（一次性能力 `Deploy:` / `Battlecry:` 也会被标上 ⇒
        /// 施放者一离场就摘）。真旗标来自那个 **ability 描述符**（`AbilityData.whileInPlay // +0x40`
        /// / `whileInPlayVariable // +0x41`，`dump.cs:19557-19566`），**我们的解析层不产出它**
        /// ⇒ **判据未齐时不用近似顶替**：今天**没有任何调用点传 `true`**，
        /// 这条通道**没有生产者**（机制留着，等输入）。⛔ 别再把「此刻有单位」当成它。</param>
        /// <param name="criteria">🆕 2026-10-18（`A885` ②）：原版 `HandEffect.targetCriteria` ——
        /// **后进手牌的牌**要过这一关才知道吃不吃得上（`PlayerHand__SetupCardInHand.c:58`）。
        /// `null` = 没有可判的规格 ⇒ **这条记录【不补给】后进手牌的牌**
        /// （`RuleCore.HandEffectFits` 判不出来那一支是 `return false`，于是 `SetupCardInHand` 只出声、不贴牌）。
        /// ⚠️ **2026-10-18 订正（铁律 5，`W5` 收窄后没跟着改）**：原来这里写着
        /// 「那时 `RuleCore.SetupCardInHand` **退回「单位卡」这一档**，见那儿」—— **那个退路已经不存在了**。
        /// 判据 = 原版 `FilterMethods__CheckIfMeetsCriteria.c` 头两句：**criteria 缺失 ⇒ `LogError` + `return 0`（拒）**；
        /// 只有「criteria 存在但为空」才 `return 1`（全放行）—— 我们 `Target == null` 对应的是前者。
        /// 📌 真对局里四个生产者**全部**传 spec（`GrantHandBuff` / `GrantHandBuffForTargets` /
        /// `AttachEffectToHandInstances` / `BroadcastHandWhen` 自指那一条）⇒ 本值只由自检默认值产生。</param>
        /// <param name="recordId">🆕 2026-10-18（`W4` 整改 · 审查问题 6）：**同一次挂载的记录号** ——
        /// 一次调用发给 N 张牌时，调用方**先取一次** <see cref="BattleContext.NextHandEffectRecord"/>
        /// 再逐张传进来（N 张共用同一个号 = 原版那**一个** `HandEffect` 记录）。
        /// `0` = 没指定 ⇒ 这里现取一个（单张挂载的场合）。用途见
        /// <see cref="CardInstance.HandEffect.RecordId"/>。</param>
        /// <param name="ownerForRecord">🆕 2026-10-18（`G3` 真差异 ①）：**这条记录记到谁的手牌上** ——
        /// 给了就不用 `HandOwnerOf` 回查（那两条路**手上可能一份载体都没有** ⇒ 回查会返回 -1
        /// ⇒ 记录静默丢失）。`-1`（默认）= 按引用回查（老行为）。</param>
        /// <returns>**真的挂上去了**（或 `inst == null` 时**记录真的登记了**）返回 `true`；
        /// 被 `dedupSamePayload` 挡掉返回 `false`。</returns>
        static bool AttachHandEffect(BattleContext ctx, CardInstance inst, string source, string payload,
                                     string duration, CardInstance.HandBuffUses uses, bool dedupSamePayload,
                                     bool extrinsic = false, EffectTargetSpec criteria = null,
                                     int recordId = 0, int ownerForRecord = -1,
                                     int onlyInstanceId = 0)
        {
            // ⚠️ 顺序：**先判空、再取号**（原来先取号 —— `ctx == null` 时会在
            //    `ctx.NextHandEffectRecord()` 上 NRE，那句 `ctx == null` 白写了）。
            if (ctx == null || string.IsNullOrEmpty(payload)) return false;
            // 🔴 **`AddEffect` 闸②（jam ∧ extrinsic）/ 闸④（施放者不在场）** —— 判据
            //   `CardScript__AddEffect.c:85-98 / 171-195`。**手牌效果这一层是本仓唯一带 `extrinsic`
            //   载体的通路**（`CardInstance.HandEffect.Extrinsic`；见「AddEffect 的四道前置闸」那节头注）。
            //   施放者 = `ctx.ActingUnit`（= 原版 `CardEffect.enchantingCard`，本函数下面就是拿它填
            //   `entry.ActingUnit`）。⚠️ 闸拒绝 = 合法的「不挂」⇒ 返回 `false`（同「去重挡掉」那一档）。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, null, extrinsic, $"手牌效果（{source}）")) return false;
            if (recordId == 0) recordId = ctx.NextHandEffectRecord();
            // 🔴 **`inst == null` 是【合法输入】**（2026-10-18 `G3` 真差异 ①）—— 它的意思是
            //    「**只登记记录、不贴任何牌**」= 原版 `PlayerHand.AddHandEffect` 的**第一步**
            //    （`PlayerHand__AddHandEffect.c:90-110`，无条件把记录收进 `activeEffects`）。
            //    手上一个符合条件的都没有时，**记录照样要进表**，后来进手牌的牌才吃得上
            //    （真卡：`GOF81 Beast Snagga Nob`）。见 `RegisterHandEffectRecord`。

            // ---- ① 去重（账 690：原版 `CardScript.AlreadyContainsEffect` → `CardEffect.IsSameEffect` **逐字段比**，见 `IsSameHandEffect`；⛔ 别退回「来源 + 载荷」字符串 —— 那会把不同施放者 / 不同到期档的两条压成一条）----
            if (inst != null && dedupSamePayload)
            {
                var cand = new CardInstance.HandEffect { Op = new EffectOp { Verb = "give", Source = source, Payload = payload }, Extrinsic = extrinsic, ActingUnit = ctx.ActingUnit, Target = criteria, OnlyInstanceId = onlyInstanceId, Expire = HandExpireOf(duration), ExpireTurn = ctx.Turn };
                foreach (var e0 in inst.HandEffects) if (IsSameHandEffect(e0, cand)) return false;
            }

            // ---- ② 载荷合成一条 `give`，目标走 `Subjectless`（兑现时以**刚上场的那个单位**为准）----
            var self = new EffectTargetSpec
            {
                Raw = "(手牌效果：打出时给这张牌自己)", Side = "own", Kind = "unit",
                Count = 1, Auto = true, Subjectless = true,
            };

            // ---- ③ 来源 + 到期（原版 `CardEffect` 那四个 `until*`，我们解析层只能产出两档）----
            //   🔴 **2026-10-18（`A885` ④）· 就地订正（铁律 5）**：这一段原来写「到期记在【这一份】上…
            //      这里记的是**最后挂上的那一条**的到期」、「等出现带时长的写法，要把到期挪到 op 上」
            //      —— **两句话都过期了**：到期位**已经挪到每条效果自己身上**
            //      （`CardInstance.HandEffect.Expire / ExpireTurn`，= 原版一条 `CardEffect` 一个到期位、
            //       `CardScript +0x108` 那张 `List<CardEffect>` 上逐条各带各的）。
            //      ⇒ 每一次挂载**追加一条**（原版 `CardScript.AddEffect` 就是 `List.Add`），
            //        绝不覆盖别人的到期位。判据全文 = `CardInstance.HandEffect` 的注释。
            var entry = new CardInstance.HandEffect
            {
                Op = new EffectOp
                {
                    Verb = "give", Source = source, Payload = payload, Target = self, Duration = duration,
                },
                Source = source,
                Uses = uses,
                RecordId = recordId,   // 🔴 「同一次挂载」的记录号（见 `CardInstance.HandEffect.RecordId`）
                // 🔴 **2026-10-18（`W4` 整改 · 审查问题 5）**：`Extrinsic` **只认显式形参**。
                //    ⛔ **原来写的是 `extrinsic || ctx.ActingUnit != null`** —— 那是「结算这一刻
                //    有个在场单位」，与真旗标（`AbilityData.whileInPlay // +0x40`）**不是一回事**：
                //    一次性能力（`Deploy:` / `Battlecry:` 那一族）也会被标成 extrinsic，
                //    于是施放者一离场这条手牌效果就被**误摘**。
                //    ⇒ 判据未齐时**不拿近似顶替**（审查口径：拿不到就「只对**持续型**标注」）。
                //    🔴 **2026-10-18 主对话查实（第三轮）**：`whileInPlay` 在本仓**只出现在签名桩**
                //      （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AbilityData,RawCardScript,
                //        DefinedTrait}.cs`），**任何数据文件里都没有**（`Unity/数据/` 与
                //      `RuleEngine/Resources/` 零命中，`d:/warpforge` 也零命中）
                //      ⇒ 与 `A897`（本地没有那个 bundle）、`A163`（判据在远端）**同一类**：
                //      **判据可查、输入本地没有** ⇒ 按本工程惯例**如实挂着、不硬造**。
                //    ⚠️ **今天没有任何调用点传 `true`** ⇒ **这条通道没有生产者**（**误摘风险已消除**）；
                //      **一旦输入到位，挂载点就在这儿、已经就绪**（只需调用方传 `true`）。
                Extrinsic = extrinsic,
                ActingUnit = ctx.ActingUnit,
                Target = criteria,
                // 🔴 **2026-10-23（`W64 B-09`）**：只补给**这一份**（0 = 不限）。
                //   判据 / 为什么不是 `EffectTargetSpec` 上的字段 → `HandSelfCriteria` 的 doc。
                OnlyInstanceId = onlyInstanceId,
            };
            switch (duration)
            {
                case "turn":      // `this turn` ⇒ `untilEndOfTurn // +0x32`
                    entry.Expire = CardInstance.HandBuffExpiry.EndOfTurn;
                    entry.ExpireTurn = ctx.Turn;
                    break;
                case "nextturn":  // `until your next turn` ⇒ `+0x33` / `+0x35` 那一支
                    entry.Expire = CardInstance.HandBuffExpiry.OwnerTurnStart;
                    entry.ExpireTurn = ctx.Turn;
                    break;
                default:          // 原版四个 `until*` 全为假 ⇒ **不过期**
                    entry.Expire = CardInstance.HandBuffExpiry.Never;
                    entry.ExpireTurn = 0;
                    break;
            }
            // ⚠️ **`inst == null` = 只登记、不贴牌**（见方法头那段）—— 贴牌 = 原版 `:112-139`
            //    那一步，由调用方遍历**当前手牌**各调一次（`GrantHandBuff` / `GrantHandBuffForTargets`）。
            if (inst != null) inst.HandEffects.Add(entry);

            // ---- ③-b 🆕 2026-10-18（`W4` 整改 · 账 5）：**登记进那一方手牌的【记录表】**
            //      （= 原版 `PlayerHand.activeEffects // +0x48`）----
            //    ⚠️ 表里放的是**上面那个 `entry` 对象本身**（同一批引用，不是副本）——
            //    `PlayerState.HandEffectRecords` 的注释里有「为什么这不是第二份状态」。
            //    ⚠️ 按 `RecordId` 去重：同一次挂载发给 N 张牌时**只登记一条**（原版一条记录）。
            {
                // ⚠️ 归属：调用方给了就**信调用方** —— `GrantHandBuff` / `GrantHandBuffForTargets`
                //    那两条路**手上可能一份载体都没有**（`HandOwnerOf` 那时返回 -1 ⇒ 记录会丢，
                //    正是 `G3` 真差异 ① 的根因）；没给才按引用回查
                //    （`AttachEffectToHandInstances` / `BroadcastHandWhen` 那条路给不出 owner）。
                int own = ownerForRecord >= 0 ? ownerForRecord
                                              : (inst != null ? HandOwnerOf(ctx, inst) : -1);
                if (own >= 0)
                {
                    var recs = ctx.Players[own].HandEffectRecords;
                    bool has = false;
                    for (int k = 0; k < recs.Count; k++)
                        if (recs[k] != null && recs[k].RecordId == recordId) { has = true; break; }
                    if (!has) recs.Add(entry);
                }
            }

            // ---- ④ 「在手里就生效」那一半：🆕 2026-10-18（`A885` ②）**消费者补上了【一半】** ----
            //   原版这一刻效果已经能被读到（`EntityScript.HasCurrentTrait` 同时读 `+0x128` 与 `+0x108`）；
            //   ✅ 补上的一半 = **后进手牌的牌也吃得上**（`RuleCore.SetupCardInHand`，
            //      原版 `PlayerHand__SetupCardInHand.c:71`）；⛔ 不补的那一半 = **卡面 / 查询**
            //      把这条效果**画出来**（我们没有那个消费者，**不做假装有**）。
            //   我们**打出时**才兑现（`RuleCore.ApplyHandBuffs`）。⛔ 别在日志里写成「在手里就加上了」。
            return true;
        }

        /// <summary>
        /// 🔴 **2026-10-18（`G3` 真差异 ①）：原版 `PlayerHand.AddHandEffect` 的【第一步】**
        /// —— **无条件**把一条记录收进 `PlayerState.HandEffectRecords`
        /// （= 原版 `PlayerHand.activeEffects // +0x48`），**与「此刻手里有没有符合条件的牌」无关**。
        ///
        /// **判据（逐段读方法体）**：`PlayerHand__AddHandEffect.c`
        ///   · `:89` 给 `cardEffect` 打 `doNotStack`；
        ///   · `:90-110` 把这条 `HandEffect` **收进 `activeEffects`**；
        ///   · `:112-139` **之后才**遍历 `currentHand` 逐张 `CardScript.AddEffect`。
        /// ⇒ 两段的**先后**就是判据：**记录先于贴牌**，而且那段循环体里**没有**「贴到 0 张就把记录撤掉」。
        /// 现实后果（真卡）：`GOF81 Beast Snagga Nob` 的
        /// `At the end of your turn, give +1 to all Beasts in your hand` —— **手里当时没有野兽**，
        /// 原版照样登记 ⇒ **后来抽到 / 造出来的野兽照样吃得上**。
        /// 我们原来这一支落在 `GrantHandBuff` 的「`n == 0` ⇒ `return false` + `unresolved`」上
        /// ⇒ **那条记录根本不存在** ⇒ 之后的来客永远吃不上（静默少给，卡面不打 `*`）。
        ///
        /// ⚠️ **只登记记录、不贴牌** —— 贴牌是第二步，由调用方遍历**当前手牌**各调一次
        /// （`GrantHandBuff` / `GrantHandBuffForTargets` 的循环）。两条分开之后，
        /// 「贴不贴得上」不再决定「记录在不在」。
        /// ⛔ **别退回「先贴、贴上了才登记」那个形状**（那是本笔要修的缺陷本身）。
        /// </summary>
        /// <param name="owner">这条记录**记到哪一方的手牌**上（调用方知道，别让它回查 —— 见
        /// `AttachHandEffect` 的 `ownerForRecord` 参数）。</param>
        /// <returns>登记好的那一条（★ 不再返回「成没成」的布尔 —— 这个动作没有失败分支）；
        /// 守卫不过（建场期 / 座位号非法）返回 `null`。</returns>
        static CardInstance.HandEffect RegisterHandEffectRecord(BattleContext ctx, int owner,
                string source, string payload, string duration, CardInstance.HandBuffUses uses,
                EffectTargetSpec criteria, int recordId)
        {
            if (ctx == null || ctx.Players == null || owner < 0 || owner > 1
                || ctx.Players[owner] == null) return null;
            if (recordId == 0) recordId = ctx.NextHandEffectRecord();
            // ⚠️ 同一次挂载只登记一条（`recordId` 是「同一次挂载」的身份，见 `HandEffect.RecordId`）
            var recs = ctx.Players[owner].HandEffectRecords;
            for (int k = 0; k < recs.Count; k++)
                if (recs[k] != null && recs[k].RecordId == recordId) return recs[k];
            // `inst` 传 `null` = **只登记、不贴牌**（口径写在那边的注释里）
            AttachHandEffect(ctx, null, source, payload, duration, uses, false, false,
                             criteria, recordId, owner);
            for (int k = 0; k < recs.Count; k++)
                if (recs[k] != null && recs[k].RecordId == recordId) return recs[k];
            return null;
        }

        /// <summary>
        /// **原版 `CardEffect.IsSameEffect`（`CardEffect.cs:200-240`）的等价判据** ——
        /// 「这两条手牌效果是不是**同一条**」（🆕 2026-10-25 · 完全复刻 · 执行代理 A · `E` 的判据）。
        ///
        /// <para>**逐字段对照表**（原版字段 → 我们的那一格）：</para>
        /// <list type="table">
        ///   <item><term>buffType</term><description>`Op.Verb`（我们这一层 verb 就是 buff 的种类）</description></item>
        ///   <item><term>enchantingCard（+heroCheck）</term><description>`ActingUnit`（同一施放者**实例**）</description></item>
        ///   <item><term>enchantmentCardId</term><description>**我们没有这一格 ⇒ 两侧恒等**。🔴 2026-10-27（账 690）**这条现在有据、不再只是「如实标着」**：原版那一格是 `AbilityData.enchantmentCardId (+0x4c)` 抄进 `CardEffect (+0xa4)`（`AbilityLogic__PlayAbility.c:1222` `*(undefined4 *)(uVar23 + 0xa4) = *(undefined4 *)(param_3 + 0x4c)`），而**我们的附魔族根本不写 `HandEffects`** —— `Enchantment.cs:46-49` 白纸黑字「附魔的 `CardEffect` **绝不放进** `HandEffects`」（手牌那族「打出即摘」、附魔「跟着牌在场不摘」，语义相反）⇒ 两条手牌效果**不可能**带不同的 `enchantmentCardId` ⇒ 这一维在本仓恒等，比不比都一样</description></item>
        ///   <item><term>extrinsic / extrinsicVariable</term><description>`Extrinsic`（`extrinsicVariable` 这一维我们没建 ⇒ 恒等）</description></item>
        ///   <item><term>until* 五档</term><description>`Expire`（枚举就是这五档）+ `ExpireTurn`（`Never` 时那一格无意义 ⇒ 不比）</description></item>
        ///   <item><term>*Change / *ValueOptions（近战/远程/血/费/随机）</term><description>`Op.Amount` + `Op.AmountMax` + `Op.CostSetTo` + `Op.Payload` / `Payload2`</description></item>
        ///   <item><term>targetCriteria（谁吃得上）</term><description>`Target` 的**值**（下面那六个决定「谁吃」的格）</description></item>
        /// </list>
        /// <para>⛔ **不比**：`Source`（来源字符串 —— 那正是 `W4` 判死的错口径）、`RecordId`（记录号只回答
        /// 「是不是同一次挂载」，不影响「是不是同一条效果」）。</para>
        /// </summary>
        static bool IsSameHandEffect(CardInstance.HandEffect a, CardInstance.HandEffect b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Op == null || b.Op == null) return false;

            // ① buffType
            if (!string.Equals(a.Op.Verb, b.Op.Verb, System.StringComparison.Ordinal)) return false;
            // ② enchantingCard（施放者实例；+`heroCheck` 那一维原版只在「督军」上多说一句，我们按实例比已经更严）
            if (!ReferenceEquals(a.ActingUnit, b.ActingUnit)) return false;
            // ③ extrinsic（`extrinsicVariable` 我们没建 ⇒ 恒等，见 doc）
            if (a.Extrinsic != b.Extrinsic) return false;
            // ④ until* 五档
            if (a.Expire != b.Expire) return false;
            if (a.Expire != CardInstance.HandBuffExpiry.Never && a.ExpireTurn != b.ExpireTurn) return false;
            // ⑤ 数值那五组
            if (a.Op.Amount != b.Op.Amount || a.Op.AmountMax != b.Op.AmountMax) return false;
            if (a.Op.CostSetTo != b.Op.CostSetTo) return false;
            if (!string.Equals(a.Op.Payload, b.Op.Payload, System.StringComparison.Ordinal)) return false;
            if (!string.Equals(a.Op.Payload2, b.Op.Payload2, System.StringComparison.Ordinal)) return false;
            // ⑥ targetCriteria 的**值**（原版那条 `targetCriteria` 是 HandEffect 上的字段）
            var ta = a.Target; var tb = b.Target;
            if ((ta == null) != (tb == null)) return false;
            if (ta != null)
            {
                if (!string.Equals(ta.Side, tb.Side, System.StringComparison.Ordinal)) return false;
                if (!string.Equals(ta.Kind, tb.Kind, System.StringComparison.Ordinal)) return false;
                if (ta.Count != tb.Count || ta.Random != tb.Random) return false;
                if (!string.Equals(ta.SubtypeFilter, tb.SubtypeFilter, System.StringComparison.Ordinal)) return false;
                if (!string.Equals(ta.KeywordFilter, tb.KeywordFilter, System.StringComparison.Ordinal)) return false;
                if (!string.Equals(ta.NotKeyword, tb.NotKeyword, System.StringComparison.Ordinal)) return false;
            }
            // ⑦ 我们自己的「只补这一份」那一格（实现位；`0` = 不限）
            if (a.OnlyInstanceId != b.OnlyInstanceId) return false;
            return true;
        }

        /// <summary>
        /// 🆕 2026-10-18（`A885` ②）**把「登记表上已有的那一条」原样补挂到另一份牌上** ——
        /// 原版 `PlayerHand__SetupCardInHand.c:71` 的 `CardScript.AddEffect(card, effect)`。
        ///
        /// ⚠️ **补挂的是【同一条】**（`Op` 引用共享、`Uses` 盒子共享、来源/到期/`Extrinsic`/施放者
        /// 照抄）—— 原版传的就是**同一个 `CardEffect` 对象**（`List.get_Item(hand.activeEffects, i)`），
        /// 所以那一批牌**共用同一个 `numberOfUses`**（见 `CardInstance.HandBuffUses`）。
        /// ⛔ 别在这儿重新造一条「等价」的（那就把共享计数断成两份了）。
        ///
        /// ⚠️ **去重 = 原版 `CardScript.AlreadyContainsEffect`（`:65`）**：同来源 + 同载荷已经在
        /// 这一份身上就不补。⛔ 别放宽 —— 放宽会在一张牌上叠出两条一模一样的（打出时翻倍）。
        /// </summary>
        public static bool AttachHandEffectCopy(BattleContext ctx, CardInstance inst,
                                                CardInstance.HandEffect src)
        {
            if (ctx == null || inst == null || src == null || src.Op == null) return false;
            // 🔴 **2026-10-18（`W4` 整改 · 审查问题 6）**：这里原来按「来源 + 载荷」字符串去重 ——
            //    **那条路是错的、别再回去**（`W4` 的同一条账）。
            // 🔴🆕 **2026-10-25（完全复刻 · 执行代理 A · 卡牌面代理 `E` 的判据）：改成【逐字段比】。**
            //   判据（第一权威，现读）= `CardEffect__IsSameEffect.c` / `CardEffect.cs:200-240`：
            //   原版 `CardScript.AlreadyContainsEffect` 拿目标身上**每一条** `activeEffects`
            //   跟这条比**一整串字段**：`buffType` · `enchantingCard`（+`heroCheck`）· `enchantmentCardId` ·
            //   `extrinsic` · `extrinsicVariable` · `untilEndOfTurn` / `untilPlayerTurnStart` /
            //   `untilEnemyTurnStart` / `untilFollowingPlayerTurnStart` / `untilNextAttack` ·
            //   `meleeAttackChange`+`meleeAttackValueOptions` · `rangeAttackChange`+`…ValueOptions` ·
            //   `healthChange`+`…ValueOptions` · `costChange`+`costValueOptions` · `randomValueChange`。
            //   ⇒ **两条字段完全相同的记录只算一条**（不看记录号）。
            //   ⚠️ 而 `RecordId` 只回答「是不是**同一次挂载**」—— 那是**另一件事**（登记表
            //      `RuleCore.HandEffectRegistry` 按记录去重，那里改不得，见 `W4`）。
            //   ⛔ **别退回「来源 + 载荷」字符串**：那会把不同施放者/不同到期档的两条压成一条。
            // 🔴 **`AddEffect` 闸②/闸④** —— 补挂等同于一次 `CardScript.AddEffect`（判据
            //   `CardScript__AddEffect.c:85-98 / 171-195`）。施放者取**这条记录自己的** `ActingUnit`
            //   （= 原版 `cardEffect.enchantingCard`），⛔ 不是 `ctx.ActingUnit`（补挂可能发生在别的动作里）。
            if (!AddEffectCasterGates(ctx, src.ActingUnit, null, src.Extrinsic, $"手牌效果（补挂：{src.Source}）"))
                return false;

            foreach (var e0 in inst.HandEffects)
                if (IsSameHandEffect(e0, src)) return false;

            inst.HandEffects.Add(new CardInstance.HandEffect
            {
                Op = src.Op,
                Source = src.Source,
                RecordId = src.RecordId,   // ⚠️ **照抄**（补挂的是同一条记录，不是新的一条）
                Expire = src.Expire,
                ExpireTurn = src.ExpireTurn,
                Uses = src.Uses,
                Extrinsic = src.Extrinsic,
                ActingUnit = src.ActingUnit,
                Target = src.Target,
                OnlyInstanceId = src.OnlyInstanceId,   // `B-09`：按份钉的那一格**照抄**
            });
            ctx.Log($"（进手牌：「{inst.Card.Name}」吃上了**还在生效**的手牌效果「{src.Source}」"
                  + $"（{src.Op.Payload}）—— 原版 `PlayerHand.SetupCardInHand`）");
            return true;
        }

        /// <summary>
        /// 造一个「还能打出几次」的**共享计数盒** —— `limitedUses &lt;= 0` 就是**不限次**（返回 `null`）。
        /// 判据：原版 `HandEffect.limitedUses // +0x28`（开关）· `numberOfUses // +0x2c`（计数）。
        /// ⚠️ **一个盒子发给一批牌**（`AttachHandEffect` 的 `uses` 参数）—— 那正是原版的形状。
        /// </summary>
        static CardInstance.HandBuffUses MakeHandBuffUses(int limitedUses)
        {
            if (limitedUses <= 0) return null;
            return new CardInstance.HandBuffUses { Limited = true, Left = limitedUses, Max = limitedUses };
        }

        /// <summary>
        /// **给手牌挂一条效果的【公开】入口** —— 对应原版 `PlayerHand.AddHandEffect(HandEffect)`
        /// （`PlayerHand__AddHandEffect.c:88-139`：收一条记录，然后**遍历 `currentHand` 逐张**
        /// `CardScript.AddEffect`）。所以这里也是**一次调用发一批牌**，
        /// 而不是「一次一张」—— 那正是 `numberOfUses` 能**共享**的原因。
        ///
        /// 🔴 **2026-10-18（`A885` + `A886`）新开**：以前这张表只有引擎内部的三个登记点在写，
        ///    外面既写不进去也读不出来。现在存放处是 `CardInstance`（每个实例一份），
        ///    写点什么数据能产出就挂在这里。
        ///
        /// ⚠️ **今天没有数据生产者**：`limitedUses` 在原版长在 `HandEffect` 这个 ScriptableObject 上
        ///    （`+0x28`），我们没解出那张表；卡面正文里也**没有**对应写法
        ///    （全文 grep「`give … in hand/in your hand`」的六张卡一句都不带时长/次数）
        ///    ⇒ 三个内部登记点恒传 `0`（不限次）。**这条公开入口就是那个缺口**：
        ///    自检直接调它验机制（`RuleEngineTest.TestHandEffectExpiryAndUses`），
        ///    将来数据到位时也挂在这里 —— ⛔ 别另造一套。
        /// </summary>
        /// <param name="limitedUses">还能打出几次；`&gt; 0` = 限次（**这一批牌共享这一个计数**）。</param>
        /// <param name="criteria">🆕 2026-10-18（`A885` ②）：原版 `HandEffect.targetCriteria`
        /// —— **后进手牌的牌**要过它（`PlayerHand__SetupCardInHand.c:58`）；`null` = 没有可判的规格
        /// ⇒ **这份记录【不补给】后进手牌的牌**（`RuleCore.HandEffectFits` 判不出来就 `return false`，
        /// `SetupCardInHand` 只出声、不贴牌；原版判据 = `FilterMethods__CheckIfMeetsCriteria.c`
        /// 「criteria 缺失 ⇒ `LogError` + `return 0`」）。⚠️ **2026-10-18 订正**：这句原来只写「没有可判的规格」、
        /// **没写后果**（旧口径还含「退回单位卡放行」那一档，`W5` 当天已收窄掉）。
        /// 📌 真对局里四个生产者都传 spec ⇒ 本值只由自检默认值产生。</param>
        /// <returns>真挂上去的份数（被去重挡掉 / `null` 的不算）。</returns>
        public static int AttachHandEffectToInstances(BattleContext ctx, string source, string payload,
                                                      string duration, int limitedUses,
                                                      List<CardInstance> insts,
                                                      EffectTargetSpec criteria = null)
        {
            if (ctx == null || insts == null || insts.Count == 0) return 0;
            var uses = MakeHandBuffUses(limitedUses);   // **一个盒子发一批牌**（原版 `HandEffect.numberOfUses`）
            // 🔴 **2026-10-18（`W4` 整改）**：这一批 N 张牌共用**一个记录号**
            //    （= 原版「一条 `HandEffect` 记录发给 N 张牌」的形状）—— 见 `HandEffect.RecordId`。
            int rid = ctx.NextHandEffectRecord();
            int n = 0;
            foreach (var inst in insts)
                if (AttachHandEffect(ctx, inst, source, payload, duration, uses, false, false, criteria, rid))
                    n++;
            if (n > 0)
                ctx.Log($"（手牌效果：「{source}」挂到了 **{n} 张牌**上 ——「{payload}」"
                      + (string.IsNullOrEmpty(duration) ? "（不过期）"
                         : duration == "turn" ? "（本回合结束到期）" : "（到你下回合到期）")
                      + (limitedUses > 0 ? $"，限 {limitedUses} 次（**这一批共享**）" : "")
                      + "；打出时兑现，原版 `CardScript.AddEffect`）");
            return n;
        }

        /// <summary>🆕 2026-09-16 **「选一个效果，给你手牌里的所有部队」**（`TL53 Infinite Biomorphologies`）——
        /// 挂到**手牌里每一张单位卡自己**身上，**打出时才兑现**（`RuleCore.ApplyHandBuffs`）。
        ///
        /// 登记规则：**手牌里每一张单位卡各挂一份**（同名两张 = 两份 —— 卡面要的就是「所有部队」）。
        /// 战术 / 防御卡跳过：卡面写的是 `all **troops** in your hand`。
        /// ⚠️ `payload` 在这一刻**不解析**（只留原文）—— 兑现时交给 `give` 那条路，
        ///    载荷词表**只有那一份判据**，别在这儿再写一遍。
        ///
        /// 🔴 **2026-10-18（`G3` 真差异 ①）：本方法是【两步】** ——
        /// ① `RegisterHandEffectRecord` **无条件**登记记录（原版 `PlayerHand__AddHandEffect.c:90-110`）；
        /// ② 再遍历**当前手牌**逐张贴（`:112-139`）。
        /// ⇒ **手上一个部队都没有**时返回 `true`（记录已经生效，后来的来客吃得上）——
        ///    ⛔ 别改回「`n == 0` ⇒ `return false` + `unresolved`」（那会让记录不存在 ⇒ 静默少给）。
        /// </summary>
        static bool GrantHandBuff(BattleContext ctx, int owner, string by, EffectOp op,
                                  string payload, List<string> unresolved)
        {
            if (string.IsNullOrEmpty(payload))
            {
                ctx.Log($"{by}：「{op.Source}」选了「给手牌」，但**没选到任何效果** —— 这条没生效");
                unresolved.Add(op.Source + "（chooseeffect 给手牌：没选到效果）");
                return false;
            }

            // 原版这一条 `HandEffect` 的 `numberOfUses` 是**共享**的（发给几张牌就几张共用一份
            // 计数器，见 `CardInstance.HandBuffUses`）⇒ 盒子在**循环外**造**一个**、每份都引用它。
            // ⚠️ 今天没有任何卡能产出 `limitedUses`（原版它在 `HandEffect` 这个 ScriptableObject 上，
            //    我们没解出那张表）⇒ 恒 `null`（不限次）；机制在这里、写点等数据
            //    （公开写点 = <see cref="AttachHandEffectToInstances"/>，自检直接调它）。
            var uses = MakeHandBuffUses(0);

            int rid = ctx.NextHandEffectRecord();   // 一条记录发给这一批牌（见 `HandEffect.RecordId`）

            // ---- ① **先登记记录**（🔴 **无条件**）----
            //   🔴 **2026-10-18（`G3` 真差异 ①）**：原版 `PlayerHand.AddHandEffect` 是**两步** ——
            //   `:90-110` 把记录收进 `activeEffects`，**之后**（`:112-139`）才遍历 `currentHand` 逐张贴。
            //   ⇒ **手上一个符合条件的都没有时，记录照样在**，后来进手牌的牌照样吃得上。
            //   我们原来是「贴上了才登记」（记录由 `AttachHandEffect` 顺手写、而它得先有一份载体）
            //   ⇒ 一条都不贴时记录**根本不存在** ⇒ 之后的来客永远吃不上（静默少给）。
            //   真卡：`GOF81 Beast Snagga Nob` 的 `… give +1 to all Beasts in your hand`（手里当时没野兽）。
            var rec = RegisterHandEffectRecord(ctx, owner, op.Source, payload, null, uses,
                                               HandTroopCriteria, rid);

            // ---- ② **贴到【现在】手里那几张**（原版 `:112-139`）----
            int n = 0;
            foreach (var inst in ctx.Players[owner].Hand)     // 第 7 行第 3 步：**一份一条**
            {
                var c = inst.Card;
                if (c == null || c.Type != "unit") continue;      // 「手牌里的**部队**」
                AttachHandEffect(ctx, inst, op.Source, payload, null, uses, false, false,
                                 HandTroopCriteria, rid, owner);
                n++;
            }
            if (n == 0)
            {
                // ⚠️ **这条【不是】「没生效」** —— 记录已经登记（上面那一步），后来的来客吃得上
                //    ⇒ **不进 `unresolved`**（那不是「本版没结算」），但**必须出声**（不许静默）。
                //    🔴 **而且必须【照着 `rec` 说】** —— 登记没成时也说「已经登记」= 静默失败
                //       （红线）。这句文案就是那条出口，别把它写成无条件的。
                ctx.Log($"{by}：「{op.Source}」要给手牌里的部队加「{payload}」，但**你手上一个部队都没有** —— "
                      + (rec != null
                         ? "效果**已经登记**（原版 `PlayerHand__AddHandEffect.c:90-110` 先记后贴），"
                           + "**之后进手牌的部队照样吃得上**"
                         : "⚠️ **记录也没登记上**（座位号非法 / 建场期）—— **这条真的没生效**"));
                return true;
            }
            ctx.Log($"{by}：「{op.Source}」选了「{payload}」—— **手牌里 {n} 张部队卡**打出时会带上它");
            return true;
        }

        /// <summary>
        /// 🆕 2026-09-16 **`… in play and in hand` 的「手牌」那半**（`Avenging Zeal`：
        /// `Give +2 … to all your units **in play and in hand**`）。
        ///
        /// 和 <see cref="GrantHandBuff"/> **同一个挂载点、同一个兑现点**（`RuleCore.ApplyHandBuffs`
        /// 在这次之后打出那个单位时生效，一个字没改），差别只在**谁决定"给什么"**：
        /// 那一条是「选效果」选的（`chooseeffect` 的 `hand` 作用域），这条是**卡面直接写的载荷**。
        ///
        /// 判据只有 <see cref="EffectTargetSpec.AlsoHand"/> 一个 —— 解析层看到目标短语里有
        /// `in hand` / `in your hand` 才置位（见那里的注释：这条原来**三层同时堵死**、静默只加场上）。
        ///
        /// ⚠️ **载荷在这一刻不解析**（和 `GrantHandBuff` 同一条纪律）：只留原文，
        ///    兑现时交给 `give` 那条路，**载荷词表只有那一份判据**。
        /// </summary>
        static void GrantHandBuffForTargets(BattleContext ctx, int owner, string by,
                                            EffectOp op, EffectTargetSpec spec, List<string> unresolved)
        {
            if (spec == null || !spec.AlsoHand) return;
            if (string.IsNullOrEmpty(op.Payload))
            {
                // 载荷不认识的 `give` 在 `DoGive` 那边已经 `unresolved` 报过了，这里不重复报
                return;
            }
            // 目标 = **这张牌自己**（兑现时以刚上场的那个单位为准）—— 同 `GrantHandBuff`
            //   （`self` 那个 `EffectTargetSpec` 现在由 `AttachHandEffect` 统一造，别在这儿再写一份）
            // 原版这一条 `HandEffect` 的 `numberOfUses` 是**共享**的 ⇒ 一个盒子、每份都引用它。
            // ⚠️ 恒 `null`（不限次）：没有数据能产出 `limitedUses`，见 `AttachHandEffectToInstances`。
            var uses = MakeHandBuffUses(0);

            // 🔴 **2026-10-18（`W4` 整改）**：这一批牌共用**一个记录号**
            //    （= 原版「一条 `HandEffect` 记录发给 N 张牌」）—— 见 `HandEffect.RecordId`。
            int rid = ctx.NextHandEffectRecord();

            // ---- ① **先登记记录**（🔴 **无条件**，和 `GrantHandBuff` 同一步）----
            //   🔴 **2026-10-18（`G3` 真差异 ①）**：原版 `PlayerHand__AddHandEffect.c` 是
            //   `:90-110` 先收记录、`:112-139` 再逐张贴 ⇒ **一个候选都没有时记录照样在**，
            //   后来进手牌的牌照样吃得上。真卡 = `GOF81 Beast Snagga Nob`
            //   （`At the end of your turn, give +1 to all Beasts in your hand`，**手里当时没野兽**）
            //   —— 它走的**正是本方法**（`atturn` → `DoGive` → 这里）。
            //   ⚠️ 记录上的 `Target` = `spec`（= 原版 `HandEffect.targetCriteria`）：
            //      后来进手牌的牌靠它过 `RuleCore.SetupCardInHand` 的 `HandEffectFits`。
            var recHb = RegisterHandEffectRecord(ctx, owner, op.Source, op.Payload, op.Duration, uses, spec, rid);

            int n = 0;
            // 先按**兵种筛**收候选（和场上**同一份**判据）
            // 🔴 第 7 行第 3 步：候选收的是**实例**（`cands` 的长度与顺序和改之前那份 `List<CardDef>`
            //    完全一致 —— 一张手牌一条 —— 所以下面那个洗牌取前 N 的**随机序列一个字没变**）。
            var cands = new List<CardInstance>();
            foreach (var inst in ctx.Players[owner].Hand)
            {
                var c = inst.Card;
                if (c == null || c.Type != "unit") continue;          // 「手牌里的部队」
                if (!string.IsNullOrEmpty(spec.SubtypeFilter) && !string.IsNullOrEmpty(c.Subtype)
                    && !System.Array.Exists(spec.SubtypeFilter.Split('|'), w =>
                           string.Equals(c.Subtype, w.Trim(), System.StringComparison.OrdinalIgnoreCase)))
                    continue;
                cands.Add(inst);
            }
            // ⚠️ **卡面写了「随机一个」就不能给全部** —— `Give +1 … to **a random Beast in your hand**`
            //   （`spec.Count == 1` + `Random`）；`Count == 0` 才是「全部」
            //   （`Avenging Zeal` 的 `all your units in play and in hand`）。
            if (spec.Count > 0 && cands.Count > spec.Count)
            {
                if (spec.Random)
                {
                    // 洗牌取前 N 张 —— 原版 `SupportMethods.GetRandomOrder` 那条（按名次挑选）；
                    // 实现在 `RuleCore.ShuffleByRandomOrder`（⛔ 别在这儿再写一份）。
                    ShuffleByRandomOrder(cands, ctx);
                    // ⚠️ 2026-10-27（`E1RNG`）：原来这里是就地 Fisher–Yates（`ctx.Rng.Next(i+1)`）——
                    // 两者都是均匀排列，但原版是「按名次挑选」⇒ 换成公共实现、随机源也变成预烧表。
                    // （`cands` 长度不变；下面 `RemoveRange` 取的仍是前 N。）
                }
                cands.RemoveRange(spec.Count, cands.Count - spec.Count);
            }
            foreach (var inst in cands)
            {
                // 🔴 **2026-09-16 那条修正保留**：载荷相同就「叠一份」，不再只记份数。
                //   出处：`Beast Snagga Nob`（`GOF81`）卡面逐字
                //   （`d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_07_Beast-Snagga-Nob.png`）：
                //   `[爪]Stomp. **At the end of your turn**, give +1[拳] to all Beasts in your hand`
                //   —— 那是**每个回合结束都来一次**的常驻效果：撑到第三个回合，
                //   打出手里那张 Beast 时就该 **+3**。
                //   （原来只 `Count++` ⇒ 打出一张时只跑一遍 `Ops` ⇒ **永远只 +1**，静默少算。）
                //
                // 🔴 **2026-09-18 第 7 行第 3 步**：条目按**实例**记了，所以
                //   「**来源不同**」不再需要「只累加份数、不叠加载荷」那个近似 ——
                //   两个来源各记**各的条目**，兑现时两条都跑（那正是卡面说的两件事）。
                //
                // 🔴 **2026-10-18（`A885`）**：条目从对局表搬到**那一份牌自己**身上
                //   （`inst.HandBuffOps`，= 原版 `CardScript +0x108`）——
                //   「同一个 `Instance` 的条目」现在就是**那一个列表**，
                //   `ReferenceEquals(h.Instance, inst)` 那趟回查没了。
                //   ⇒ 「同来源就并进同一条」= **同一张牌的那个列表里追加一条**（等价，且不会再查错表）。
                AttachHandEffect(ctx, inst, op.Source, op.Payload, op.Duration, uses, false, false, spec,
                                 rid, owner);
                n++;
            }
            ctx.Log($"{by}：「{op.Source}」的 `in hand` 那半：**手牌里 {n} 张部队卡**"
                  + (op.Duration == "turn" ? "本回合" : op.Duration == "nextturn" ? "到你下回合" : "")
                  + $"打出时会带上「{op.Payload}」"
                  + (n == 0
                     // ⚠️ 照 `recHb` 说（登记没成时不许说「已经登记」—— 那是静默失败）
                     ? (recHb != null
                        ? "（⚠️ 现在一张都没有 —— 但效果**已经登记**（原版 `AddHandEffect` 先记后贴），"
                          + "**之后进手牌的部队照样吃得上**）"
                        : "（⚠️ 现在一张都没有，而且**记录也没登记上** —— **这条真的没生效**）")
                     : ""));
        }

        // ==================================================================
        //  🔴 2026-10-17 · B7「卡实例身份 —— `SetupCardInHand` 那一套」
        // ==================================================================
        //
        //  **一、原版判据（逐句读的方法体；文件都在 `d:/2/tools/decomp_full/`）**
        //
        //   · `PlayerHand.SetupCardInHand(CardScript card)`（`PlayerHand__SetupCardInHand.c`）
        //     —— **牌进入手牌那一刻**调，四个调用点：`PlayerHand._AddDrawnCardToHand_d__37__MoveNext` ·
        //     `…_AddCardNotDrawnToHand_d__39` · `…_AddCardNotDrawnToHandAtIndex_d__40` ·
        //     `…_AddActiveTrapToHand_d__38`。除了「挂到手上那套界面动作」（`:24-47` 设父节点 / 排序层 /
        //     `ShowCardBack`），正事在 `:38-77`：遍历 `activeEffects`（`PlayerHand +0x48`，
        //     `List<HandEffect>`，声明见 `Warpforge_code/Scripts/Assembly-CSharp/PlayerHand.cs:519`），
        //     对每一条 `FilterMethods.CheckIfMeetsCriteria(词, card, card, handEffect.targetCriteria, 0)`
        //     （`:58-59`）→ 命中且 `CardScript.AlreadyContainsEffect` 为假（`:65`）→
        //     **`CardScript.AddEffect(card, handEffect.cardEffect)`（`:71`）**。
        //     ⇒ **效果挂在「那一张牌」自己身上**（`CardScript +0x108` = `List<CardEffect>`）。
        //
        //   · `PlayerHand.AddHandEffect(HandEffect)`（`PlayerHand__AddHandEffect.c:88-139`）：
        //     ① 给 `cardEffect` 打 **`doNotStack = 1`**（`:89` 写的是 `+0x75`；
        //        `CardEffect.doNotStack` 见 `dump.cs:119095` —— 旁边一行是 `permanentBuff // 0x74`，
        //        而 `isHandBuff` 在 **`0xA8`**）。
        //        🔴 **2026-10-18 订正**：这里原来写的是「打 `isHandBuff = 1`」—— **偏移读错了**，
        //           `:89` 那个 `+0x75` 是 `doNotStack`（「Don't stack bonuses」），不是 `isHandBuff`。
        //           措辞上的后果：把「这条手牌效果**不许叠加**」记成了「把它标成手牌 buff」——
        //           后者是**另一条路**（`isHandBuff` 的写点在别处，本段不依赖它），
        //           所以当时没炸，但判据是错的。实读证据：`PlayerHand__AddHandEffect.c:89`；
        //           字段偏移证据：`d:/2/tools/il2cpp_out/dump.cs:119092-119113`
        //           （`CardEffect`，`permanentBuff // 0x74` · `doNotStack // 0x75` · `isHandBuff // 0xA8`）。
        //     ② 把这条 `HandEffect` 收进 `activeEffects`（`:90-110`）；
        //     ③ **立刻**遍历 `currentHand`（`PlayerHand +0x50` = `List<CardScript>`），
        //        对**过筛的每一张牌**调 `CardScript.AddEffect(那张牌, cardEffect)`（`:126-135`）。
        //     ⇒ 「牌在手上时把效果挂到那张牌自己身上」= **逐张牌挂**，
        //       不是「按 `CardDef.Id` 记一条全局修正」。
        //
        //   · 摘掉：`RemoveHandEffectAt(index)`（`PlayerHand__RemoveHandEffectAt.c:30-40`）遍历
        //     `currentHand`、对每张牌 `CardScript.RemoveEffect(那张牌, handEffect.cardEffect)`；
        //     `RemoveEffectsFromCard(card)`（`PlayerHand__RemoveEffectsFromCard.c:88-105`）反向由一张牌
        //     找回它头上那条 `HandEffect`。两条都**只动那一张牌**，不是动「所有同名卡」。
        //
        //   · 🎯 **打出时怎么处理**：`PlayerHand.RemoveCardFromHand`（`PlayerHand__RemoveCardFromHand.c:20`）
        //     **只是把这个 `CardScript` 从 `currentHand` 里 `Remove` 掉**（不销毁、不重建，
        //     后面只跟一个重排动画）⇒ **手上那张牌打到场上还是同一个对象**，
        //     挂在它身上的 `CardEffect` **跟着一起上场**。（旁证：棋盘两侧的 minion 也是
        //     `List<CardScript>` —— 我们自己的 `Core/BoardSlots.cs` 文件头就是从 `dump.cs:39326` 抄的。）
        //     次数上限的账由 `PlayerHand.CardPlayedWithEffects`（`…:28-45`）核销：
        //     `handEffect.limitedUses` 为真、且打出的那张牌身上有这条效果 ⇒ `numberOfUses--`，
        //     减到 0 就 `RemoveHandEffectAt`。
        //
        //   · 过期：`PlayerHand.UpdateCardEffects(bool endOfTurn)`（`:119-166` 按
        //     `BattleManager.IsPlayerTurn` 分档，`:332` 逐条 `RemoveHandEffectAt`）。
        //     ✅ **2026-10-18（`A886`）补：调用点查实了**（原来记的是「没查到」）——
        //     `BattleManager__ResolveEndTurn.c:678` 与 `:680` 对**两方手牌**各调一次、传 `endOfTurn = 1`；
        //     `BattleManager__ResolveAction.c:2576/2578` 传的是 `0`（那只处理 `extrinsic` 那一族
        //     —— 来源已经不在场 / 已变身的那种，**我们没复刻**）⇒ 我们只落**回合结束那一趟**。
        //
        //  **二、我们这边怎么落 —— 只补「缺的那几样」，不另起一套机制**
        //
        //   手牌那一侧的「哪一份」= `CardInstance`（2026-09-18 第 7 行已经做完）；
        //   「挂在那一份上、打出时兑现」的存放处 = **那一份牌自己**（`CardInstance.HandBuffOps` 那一族，
        //   = 原版 `CardScript +0x108`），兑现点是 `RuleCore.ApplyHandBuffs`（打出**那一份**时跑它的
        //   `Ops`、跑完即摘）。⇒ 本段三样：
        //     ① **指代** <see cref="HandReferents"/> —— **手牌代词槽**里那几份（`LastHandTargets` /
        //        `LastHandTarget`），槽空才退回「本次结算里进到手牌」的兜底
        //        （= 原版遍历 `currentHand` 时「哪些牌现在在手里」那一半）；
        //     ② **投递** <see cref="AttachEffectToHandInstances"/> —— 把载荷挂到**那几份**上
        //        （统一走 <see cref="AttachHandEffect"/>）；
        //     ③ **到期 / 次数** `RuleCore.ExpireHandBuffs` + `ApplyHandBuffs` 里那次核销
        //        （= 原版 `UpdateCardEffects` / `CardPlayedWithEffects`）。
        //
        //   ✅ **2026-10-18（`A885` + `A886`）状态订正 —— 这一段原来挂着「只做到一半」。**
        //     · ✅ **「效果放在哪儿」已经按原版落定**：**挂在那一张牌自己身上**
        //       （`CardInstance.HandBuffOps` / `HandBuffSource` / `HandBuffExpire*` / `HandBuffUsesRef`），
        //       ⛔ 对局级那张 `BattleContext.HandBuffs` **已删**（两处各存一份 = 迟早不一致）。
        //     · ✅ **到期与次数上限补上了**（原版 `CardEffect` 的 `until*` / `HandEffect` 的
        //       `limitedUses`+`numberOfUses`），清扫点 = `RuleCore.ExpireHandBuffs`（`EndTurn` 里调）。
        //     · ✅ **代词槽已做**（`A888`）：`BattleContext.LastHandTargets` / `LastHandTarget`，
        //       写点 `DoDraw` + `AttachEffectToHandInstances`、清点 `ResolveOps` 入口。
        //     · ⚠️ **仍然没有消费者的**：「在手里就看得见」（卡面 / 查询）—— 我们**不做假装有**；
        //       「还在手里时就改费用」走的是**另一条路**（`CostMods` + `CostMod.HandInstanceId`，
        //       见 `BroadcastCostWhen`），不在本段。
        //     🆕 **2026-10-18（`A885` ②④）再补三条（这一段的「状态」以这三条为准）**：
        //       · ✅ **每份一条 → 每条一条**：到期位长在 `CardInstance.HandEffect` 上
        //         （原版一条 `CardEffect` 一个到期位、一份牌可挂多条各带各的）。
        //       · ✅ **「后进手牌的牌也吃上既有手牌效果」那一层做了**：`RuleCore.SetupCardInHand`
        //         （原版 `PlayerHand__SetupCardInHand.c:38-74`，四个调用点 = 进手牌的每一条路），
        //         登记表 = **现场扫手牌实例**（`HandEffectRegistry`），⛔ 仍然**没有对局级的表**。
        //         ⚠️ 近似一处（如实标着）：`targetCriteria` 我们只有条目上的 `Target` 规格，
        //         判不出来时退回「单位卡」这一档 —— 见 `SetupCardInHand` 的注释。
        //       · ✅ **`extrinsic` 那一支做了**（原版 `CardEffect.extrinsic // +0x30`，
        //         `PlayerHand__UpdateCardEffects.c` 那两条**无条件**支线）：落点在
        //         `RuleCore.HandEffectExpired` —— 施放者（`ActingUnit`）不在场上了就摘。

        /// <summary>
        /// **`they` / `them` / `it` 在手牌这一侧指的是哪几份**（**在手里**的那些）。
        ///
        /// 出处：原版那套是**逐张牌**落效果，作用对象就是 `currentHand` 里现在躺着的那些牌
        /// （`PlayerHand__SetupCardInHand.c:38-77` · `PlayerHand__AddHandEffect.c:112-139`）。
        ///
        /// 🔴 **2026-10-18（`A888`）取数源改了：先读「手牌代词槽」，槽空才退回兜底。**
        ///   · **槽** = `BattleContext.LastHandTargets`（复数）+ `LastHandTarget`（单数），
        ///     由 `DoDraw`（刚抽上手的那几份）与 <see cref="AttachEffectToHandInstances"/>
        ///     （刚挂上效果的那几份）写、由 `ResolveOps` 入口清（与 `DrawnThisResolve` 同一个窗口）。
        ///   · **兜底** = `BattleContext.DrawnThisResolve`（本次结算抽到的那几份）——
        ///     ⚠️ 那是**计数账**（`For each troop drawn …` 数它）被借来当代词用，属**推断**，
        ///     只有「先抽、后指代」这一种卡面形状兜得住 ⇒ 现在降级成兜底。
        ///   ⚠️ **订正**：原来这里写着「所以**不另开一个槽**（`BattleContext.cs` 不在本件的可改范围里）」——
        ///     那是当时**可改范围**的限制，不是口径。槽已经开了，本段的取数源也随之改了。
        ///
        /// ⚠️ **为什么不能用 `LastTargets`**：它是 `List&lt;UnitState&gt;`（场上的单位），
        ///    手里的牌**没有 `UnitState`** —— 这正是「给手里的卡挂关键词」原来走不通的根因。
        ///
        /// ⚠️ **它只是 `prev` 的兜底，不是抢班**：两处调用点都**先看场上那一批**
        ///    （`ctx.LastTargets` / `ctx.LastTarget`），场上那一批能拿到就以**场上**为准。
        ///    卡池实测（`cards_engine.json` 全文正则）：「既 `draw` 又用 `them`」的一共 **3 张** ——
        ///    `TAU54 Dynamic Offensive`（`Draw 2 troops from your deck. If they are Battlesuits,
        ///    give them Flank`）· `GOF50 Tide of Muscle`（`Draw two troops and give them +1`）·
        ///    `AM58 Siege Warfare`（`Draw two Vehicles from your deck and **deploy** them` ——
        ///    那一句是 `deploy`，不走本段）。前两张的 `them` 指的都是**刚抽上手的那几张**，
        ///    而**没有任何一张**是「先抽牌、再拿 `them` 指场上的单位」⇒ 兜底不会误伤。
        ///    🆕 **2026-10-18 补另外 2 张**：单数 `it` 的 `GOF_Da_Red_Waaagh`
        ///    （`Draw a troop and give it +1`）与 `UM23 Spear of Macragge`
        ///    （`Draw a Vehicle and give it Armour 2`）**走的是同一条路**
        ///    （`DoGive` 那条判据只看 `spec.Side == "prev" &amp;&amp; spec.Kind == "prev"`，
        ///    **不看 `Count`**）—— 它们那一句是 `Draw a …`（定向翻找，只出一张）
        ///    ⇒ 两张卡的槽由 `DoDraw` 填得上；⚠️ 但它们是**兜底仍然必须存在**的原因之一。
        /// </summary>
        static List<CardInstance> HandReferents(BattleContext ctx, int owner)
        {
            var res = new List<CardInstance>();
            if (ctx == null) return res;
            var hand = ctx.Players[owner].Hand;

            // ---- ① 先读槽（单数槽 + 复数槽合成一批）----
            //   ⚠️ 两个调用点都只想要「一批实例」，**没有一个需要区分单复数**
            //      （`DoGive` 那条判据不看 `Count`）⇒ 在这里合成，别在调用点各拼一次。
            //   ⚠️ **槽里的那几份仍然要过「还在不在手牌」** —— 指代发生之后它们可能已被打掉/弃掉。
            if (ctx.LastHandTarget != null && ContainsInst(hand, ctx.LastHandTarget))
                res.Add(ctx.LastHandTarget);
            foreach (var inst in ctx.LastHandTargets)
                if (inst != null && ContainsInst(hand, inst) && !ContainsInst(res, inst)) res.Add(inst);

            // ---- ② 槽空才退回兜底：本次结算里进到手牌、**且还在手里**的那几份 ----
            //   ⚠️ **兜底必须留着**：上面那段点名的四张卡都靠它（并且单数那两张
            //      「`Draw a …` 只出一张」⇒ 槽其实也填得上；留着它是为了**槽真的空**时
            //      ——例：写点以后变了 / 抽牌走的是别的入口——**不静默失效**）。
            if (res.Count == 0)
                foreach (var inst in ctx.DrawnThisResolve)
                    if (inst != null && ContainsInst(hand, inst)) res.Add(inst);   // 已被打掉/弃掉的不算
            return res;
        }

        /// <summary>
        /// **把手牌里那几份当成 `give` 的目标**（原版 `PlayerHand.SetupCardInHand` 的语义）。
        ///
        /// 落法：给**每一份**各挂一条（走到 <see cref="AttachHandEffect"/>，落在**那一份牌自己**身上 =
        /// 原版 `CardScript +0x108`），载荷原样留着（`Target` 走 `Subjectless` —— 兑现时以
        /// **刚上场的那个单位**为准，和 `GrantHandBuff` / `GrantHandBuffForTargets` **同一个挂载点、
        /// 同一个兑现点**）。
        ///
        /// ⚠️ **载荷在这一刻不解析**（三条路同一条纪律）：只留原文，兑现时交给 `give` 那条路 ——
        ///    载荷词表**只有 `GivePayload` 一份判据**，别在这儿再写一遍。
        /// ⚠️ **同一份不再重复挂「同一条效果」**（原版 `CardScript.AlreadyContainsEffect` → `CardEffect.IsSameEffect`
        ///    **逐字段比**：`PlayerHand__SetupCardInHand.c:65` · `CheckEffectsOnNewCard.c:37`；我们的等价判据 = `IsSameHandEffect`，账 690）。
        ///    载荷**不同**就各记各的（那是两件事，`GrantHandBuffForTargets` 同一条口径）。
        ///    `dedupSamePayload = true` 就是这一条；⛔ **别在登记点改这条纪律**（改了 `Beast Snagga Nob`
        ///    那族的「三回合叠三份」会静默少算）。
        /// </summary>
        static int AttachEffectToHandInstances(BattleContext ctx, int owner, string by, EffectOp op,
                                               List<CardInstance> insts, List<string> unresolved)
        {
            if (ctx == null || insts == null || insts.Count == 0) return 0;
            if (string.IsNullOrEmpty(op.Payload)) return 0;

            // 原版这一条 `HandEffect` 的 `numberOfUses` 是**共享**的 ⇒ 一个盒子、每份都引用它。
            // ⚠️ 恒 `null`（不限次）：没有数据能产出 `limitedUses`，见 `AttachHandEffectToInstances`。
            var uses = MakeHandBuffUses(0);

            // 🔴 **2026-10-18（`W4` 整改 · 审查问题 6）**：这一批牌共用**一个记录号**
            //    （= 原版「一条 `HandEffect` 记录发给 N 张牌」）—— 不给同一个号的话，
            //    登记表会把「一次挂载」看成 N 个记录 ⇒ 后进手牌的牌会拿到 **N 份**（翻倍）。
            int rid = ctx.NextHandEffectRecord();

            int n = 0;
            foreach (var inst in insts)
            {
                if (inst == null) continue;
                // ⚠️ `n` 只数**真挂上去的**（被「同来源 + 同载荷」去重挡掉的不算）——
                //    与改之前那个 `if (e == null) { Add(); n++; }` 同一个语义。
                if (AttachHandEffect(ctx, inst, op.Source, op.Payload, op.Duration, uses, true, false,
                                     op.Target, rid))
                    n++;
            }
            ctx.Log($"{by}：「{op.Source}」把「{op.Payload}」挂到了**手牌里 {n} 张卡**上"
                  + $"（{Names(insts)}）—— **打出时**带上这条效果"
                  + "（原版 `PlayerHand.SetupCardInHand`：效果挂在**那一张牌自己**身上）");
            // 🆕 **2026-10-18（`A888`）：挂完就把「手牌那一侧的代词槽」指向这几份** ——
            //    这一步的语义本来就是「**效果落到了手里哪几张**」，与 `DoDraw` 是同一个槽的两个写点。
            //    ⚠️ 放在**日志之后、`return` 之前**；`insts` 是调用方筛过的（还在手里），原样收。
            //    ⚠️ 单数槽同 `DoDraw`：只在恰好一张时填（多条时留 `null`，不替卡面挑）。
            ctx.LastHandTargets.Clear();
            foreach (var inst in insts) if (inst != null) ctx.LastHandTargets.Add(inst);
            ctx.LastHandTarget = ctx.LastHandTargets.Count == 1 ? ctx.LastHandTargets[0] : null;
            return n;
        }

        // （原来这里有个 `HandBuffHasPayload(h, payload)` —— **2026-10-18（`A885`）删掉了**：
        //   它要在**对局表**里逐条回查「这一份是不是已经挂过同来源同载荷的那条」；
        //   搬到实例上之后，那一趟回查就是**那一份自己的** `HandBuffOps`，
        //   逻辑并进了 `AttachHandEffect` 的 `dedupSamePayload`。⛔ 别再往对局表上重建一份。）

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
        /// 在**本局打出过的牌**（`BattleContext.PlayedCards`）里找「**那一份**」——
        /// 从表**末尾往前**（最近的优先）。找不到返回 null。
        ///
        /// 🔴 **认的是表里记的那一份实例**（<see cref="PlayedCard.Instance"/>），**不是**按卡模板
        ///    另找一份 —— 「打出过的牌」记的就是**打出的那一份**（原版按 `actingCardUniqueGID`
        ///    回查 `CardScript`，同一件事）。同名打过两张时「哪一张更近」由它定死（可复现、不掷骰）。
        /// </summary>
        static PlayedCard FindPlayed(BattleContext ctx, int owner, CardDef card)
        {
            if (card == null) return null;
            for (int i = ctx.PlayedCards.Count - 1; i >= 0; i--)
            {
                var pc = ctx.PlayedCards[i];
                if (pc.Owner == owner && pc.Card != null && ReferenceEquals(pc.Card, card)) return pc;
            }
            return null;
        }

        /// <summary>
        /// 从**来源**里把选中的那张取走（部署/回手/洗回牌库时都要），**并返回摘走的那一份**。
        ///
        /// `pool` 是**凭空生成**，没有「取走」这一步 —— 所以它返回 **null**
        /// （调用方据此 `ctx.NewInstance(pick)` 发新的一份）。
        /// 墓地那条走 <see cref="BattleContext.TakeFromGraveyard"/>（`Discard` 与 `DeadUnits`
        /// **必须一起**移除，那条规则只写在那一个地方）。
        ///
        /// 🔴 **2026-09-18 第 7 行第 2 步**：返回值从「无」变成「摘走的那一份」——
        ///    调用方要拿它把**同一个实例**放进目标区域（回手、部署、洗回牌库都要求「还是那一份」）。
        /// </summary>
        static CardInstance RemoveFromSource(BattleContext ctx, int owner, string srcKind, CardDef card)
        {
            if (card == null) return null;
            var ps = ctx.Players[owner];
            switch (srcKind)
            {
                case "deck":      return TakeFromZone(ps.Deck, card);
                case "hand":      return TakeFromZone(ps.Hand, card);
                case "enemyhand": return TakeFromZone(ctx.Players[1 - owner].Hand, card);
                case "dead":      return ctx.TakeFromGraveyard(owner, card);
                // 🆕 2026-10-17（B19）：`played` 域 —— 摘的是**表里记的那一份实例**
                //   （`FindPlayed` 是唯一判据），不是按卡模板另找一份。
                //   它现在在哪个区由**区域归属**决定：打出的战术卡在**弃牌堆**（原版的
                //   `cemetery` 就是它）、单位在**棋盘**上。所以按**实例身份**依次试
                //   弃牌堆 → 手牌 → 牌库（`TakeInstFromZone` 按引用摘，`TakeFromZone` 是按模板摘的，别混）。
                //   ⛔ **棋盘不摘**（那要连棋盘索引、光环重算、阵亡一起动，是另一件事）——
                //      这种情况**出声**并返回 null（调用方的 `?? pickedInst` 兜底），不许静默。
                case "played":
                {
                    var row = FindPlayed(ctx, owner, card);
                    var inst = row != null ? row.Instance : null;
                    if (inst == null) return null;          // 表里没有 ⇒ 调用方按既有兜底走
                    var taken = TakeInstFromZone(ps.Discard, inst)
                             ?? TakeInstFromZone(ps.Hand, inst)
                             ?? TakeInstFromZone(ps.Deck, inst);
                    if (taken == null)
                        ctx.Log($"「{card.Name}」是本局打出过的牌，但它现在**不在弃牌堆/手牌/牌库里**"
                              + "（打出的单位就在场上）—— **本版不从棋盘上摘**，这一条没生效");
                    return taken;
                }
            }
            return null;                 // `pool`：没有可摘的一份 ⇒ 调用方新发
        }

        /// <summary>
        /// 在某个区域里**找**那一份（**不取走**）—— 找不到返回 null。第 7 行第 3 步。
        /// 用于「先把指代槽指向它，动作在**下一句**」（`Choose a troop from your deck. **Draw it** …`）。
        /// </summary>
        static CardInstance FindZoneInstance(BattleContext ctx, int owner, string srcKind, CardDef card)
        {
            if (card == null) return null;
            var ps = ctx.Players[owner];
            List<CardInstance> zone;
            switch (srcKind)
            {
                case "deck":      zone = ps.Deck; break;
                case "hand":      zone = ps.Hand; break;
                case "enemyhand": zone = ctx.Players[1 - owner].Hand; break;
                case "dead":
                {
                    // 🆕 2026-10-23（引擎线 `新账-97` ③/⑧）：先按**阵亡流水**找最近那一份实例
                    //    （同名两份不再拿弃牌堆里最后一张凑）；找不到才退回按模板。
                    for (int i = ctx.DeadUnits.Count - 1; i >= 0; i--)
                    {
                        var d = ctx.DeadUnits[i];
                        if (d == null || d.Owner != owner || d.Instance == null) continue;
                        if (!ReferenceEquals(d.Card, card)) continue;
                        if (ContainsInst(ps.Discard, d.Instance)) return d.Instance;
                    }
                    zone = ps.Discard;
                    break;
                }
                // 🆕 2026-10-17（B19）：`played` 域 —— **认的是表里那一份实例**（`FindPlayed` 唯一判据）。
                // ⚠️ **只认「确实躺在能摘的那三个区里」的那一份**，这一条与 `RemoveFromSource`
                //    的 `case "played"` **必须同口径**：那边摘不走、这边却把它交出去，调用方的
                //    `?? pickedInst` 就会把**还留在原处（多半在棋盘上）的那一份也塞进手牌**
                //    ⇒ 同一份牌同时出现在两个区域（静默的不变量破坏）。
                case "played":
                {
                    var row = FindPlayed(ctx, owner, card);
                    var inst = row != null ? row.Instance : null;
                    if (inst == null) return null;
                    if (ContainsInst(ps.Discard, inst) || ContainsInst(ps.Hand, inst)
                        || ContainsInst(ps.Deck, inst)) return inst;
                    return null;                  // 在场上 / 已经不在任何区 ⇒ 没有「可指的那一份」
                }
                default:          return null;            // `pool`：凭空生成，本来就没有「哪一份」
            }
            for (int i = zone.Count - 1; i >= 0; i--)
                if (ReferenceEquals(zone[i].Card, card)) return zone[i];
            return null;
        }

        /// <summary>区域里有没有**这一份**（按对象身份比）。</summary>
        static bool ContainsInst(List<CardInstance> zone, CardInstance inst)
        {
            if (zone == null || inst == null) return false;
            foreach (var h in zone) if (ReferenceEquals(h, inst)) return true;
            return false;
        }

        /// <summary>
        /// 从某个区域里**按卡模板**摘走**一份**，返回摘掉的那一份（没有就 null）。
        /// ⚠️ **从末尾往前找**（和 `Draw` 取牌库末尾、原版 `pop_back` 同一个方向）——
        ///    同名多份时「摘哪一份」由它定死，可复现、不掷骰。
        /// </summary>
        static CardInstance TakeFromZone(List<CardInstance> zone, CardDef card)
        {
            if (zone == null || card == null) return null;
            for (int i = zone.Count - 1; i >= 0; i--)
                if (ReferenceEquals(zone[i].Card, card)) { var t = zone[i]; zone.RemoveAt(i); return t; }
            return null;
        }

        /// <summary>
        /// **按实例身份**从区域里摘走那一份（没有就 null）。与 <see cref="TakeFromZone"/> 是两件事
        /// —— 那条按**卡模板**摘，同名副本会被摘错。
        ///
        /// 🆕 2026-10-17（B19）：`played` 域要的正是「**就是打出过的那一份**」
        /// （<see cref="PlayedCard.Instance"/>），所以不能用按模板那条。
        /// </summary>
        static CardInstance TakeInstFromZone(List<CardInstance> zone, CardInstance inst)
        {
            if (zone == null || inst == null) return null;
            for (int i = zone.Count - 1; i >= 0; i--)
                if (ReferenceEquals(zone[i], inst)) { zone.RemoveAt(i); return inst; }
            return null;
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

        /// <summary>某个区域里**有没有这个模板的一份**（第 7 行第 2 步加：区域里存的是实例）。</summary>
        static bool ContainsCard(List<CardInstance> zone, CardDef card)
        {
            if (zone == null || card == null) return false;
            foreach (var h in zone) if (ReferenceEquals(h.Card, card)) return true;
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
            var refs = new List<CardInstance>();      // 第 7 行第 3 步：指代槽装的是**哪一份**
            if (ctx.LastChosenCard != null) refs.Add(ctx.LastChosenCard);
            else refs.AddRange(ctx.LastCreated);

            if (refs.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要抽「它」，但前面没有指代对象（既没选牌、也没造牌）"
                      + " —— **这条没生效**");
                unresolved.Add(op.Source + "（`it` 没有可抽的对象）");
                return false;
            }

            var got = new List<CardInstance>();
            foreach (var r in refs)
            {
                if (r == null || r.Card == null) continue;
                if (ContainsCard(ps.Hand, r.Card)) continue;  // 已经在手上，别再来一张（**按卡模板**判 —— 与改之前一致）
                // 从牌库**摘那一份**（第 7 行第 2 步：摘出来的是实例，跟着进口袋）
                // ⚠️ 不在牌库里时：**把指代的那一份本身放进来**（`?? r`）。
                //    老代码是「照样 `Hand.Add(卡模板)`」—— 结果一样是「手里多一张」，
                //    但那会造出**第二份**幽灵，而后续的 `lower its cost by N` 钉在 `r` 上 ⇒ **静默挂空**。
                var inst = TakeFromZone(ps.Deck, r.Card) ?? r;
                // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）**：先判上限再 Add
                //   （满了 ⇒ 这份进弃牌堆、手牌不动，且**不算**「抽到」）。
                if (!RuleCore.AddCardToHandWithinLimit(ctx, owner, inst)) continue;
                ctx.DrawnThisResolve.Add(inst);          // 第 7 行第 3 步：记**那一份**
                got.Add(inst);
            }

            // 🆕 **2026-10-18（第三会话 · `A992` 遗留乙）：手牌那一侧的代词槽也要填 —— 与 `DoDraw` 同形**
            //    （`Clear()` + 逐个 `Add` + 单数槽「恰好一张才填」；判据只此一处 = `DoDraw` 那一段）。
            //    收的是 `got`（**真的被搬进手牌的那几份**），不是 `refs` —— 已经在手上、这一跳没动作的
            //    那几份**不该进槽**（它们不是「刚进手牌的」）。
            //    ⚠️ `got` 为空时这一对是**空的**（`Clear()` 之后什么都没加）—— 那是对的：
            //      本跳什么都没搬进来 ⇒ 槽里没有「刚进手牌的」，由 `HandReferents` 退回兜底。
            //    理由与「今天零可观测差异」见 `DoDrawType` 那一段逐条注释。
            ctx.LastHandTargets.Clear();
            foreach (var inst0 in got) ctx.LastHandTargets.Add(inst0);
            ctx.LastHandTarget = ctx.LastHandTargets.Count == 1 ? ctx.LastHandTargets[0] : null;

            // 「刚抽到的这批」= 后续 `lower its cost by N` / `create a copy of it` 的指代对象
            ctx.LastCreated.Clear();
            foreach (var r in refs) ctx.LastCreated.Add(r);

            if (got.Count == 0)
            {
                // ⚠️ 两种「没动作」：已经在手上（改前的唯一一种）/ **手牌已满**（`P2-5` 之后的新一档，
                //   helper 已经各打了一条「进弃牌堆」的日志）—— 措辞要认得这一档，别一律说成「已经在手上」。
                ctx.Log($"{by}：「{op.Source}」要抽「它」，但没有一张落进手牌"
                      + "（已经在手上，或手牌已满）—— 没动作");
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
        static bool DoReturn(BattleContext ctx, int owner, string by, EffectOp op,
                             UnitState chosen, List<string> unresolved)
        {
            // 目标可能有**两个**：`Return a friendly troop **and a random enemy troop** to the top of their deck`
            var phrases = new List<string>();
            foreach (string piece in (op.Payload ?? "").Split(new[] { " and " }, System.StringSplitOptions.None))
            {
                string p2 = piece.Trim();
                if (p2.Length > 0) phrases.Add(p2);
            }

            var moved = new List<CardInstance>();      // 第 7 行第 2 步：回手/洗回去的是**那几份实例**
            int miss = 0;

            // 🔴 **施放者已经不在场上了**（`Backlash:` 那条路 —— 2026-09-14 A5 批 3 第 1 条实测发现）——
            //    卡面写 `Returns to your hand` 时，单位**已经死过一轮了**：`RuleCore.CleanupDeaths`
            //    把「进弃牌堆 + 发 Death 事件 + 广播死亡监听」全做完**才**放 Backlash
            //    （那边写着「单位**已经不在棋盘上了**」，不用查）。
            //    而此时 `ResolveTargets` 的 `Subjectless` 分支判的是 `source != null && source.IsAlive`
            //    ⇒ 死人**不满足** ⇒ 一路落到最后的「己方全体」兜底
            //    ⇒ **把全场单位一起收进手牌**。日志上还只看到「把 A、B、C 放回手牌」，
            //      看不出哪儿错了（这就是本工程最怕的**静默错打**）。
            //    ⇒ 这里显式接管：**谁死的就是谁的卡，从弃牌堆捞回手牌**。
            //       `TakeFromGraveyard` 会连同 `DeadUnits` 那条记录一起撤掉 ——
            //       不撤的话同一张卡能被「复活」两次（那个方法的注释点名的就是这个）。
            if (op.Target != null && op.Target.Subjectless
                && ctx.ActingUnit != null && !ctx.ActingUnit.IsAlive)
            {
                var dead = ctx.ActingUnit.Card;
                if (dead == null)
                {
                    ctx.Log($"{by}：「{op.Source}」要回手，但施放者认不出是哪张卡 —— **这条没生效**");
                    unresolved.Add(op.Source + "（施放者已离场且认不出卡）");
                    return false;
                }
                if (op.Dest != "hand")
                {
                    // 「离场之后回**牌库**」在全卡池没有实测用例 ⇒ **不猜**，如实报。
                    // 静默按「回手」处理就是换了一套语义还不说。
                    ctx.Log($"{by}：「{op.Source}」施放者已离场，回「{op.Dest}」这一支**本版没做**"
                          + " —— **这条没生效**");
                    unresolved.Add(op.Source + "（离场后回非手牌）");
                    return false;
                }
                // 🔴 第 7 行第 2 步 + **用户 2026-09-18 拍的口径**：回手**沿用同一个实例**
                //    （默认保留实例态）—— 所以这里取的是「施放者那一份」，不是模板。
                // 🔴 **2026-10-17 追加（配套 `RuleCore.CleanupDeaths` 那次下移）**：反噬已改成
                //    **在「进弃牌堆」之前**结算（照原版 `CardScript.CheckIfDead` →
                //    `TriggerUnitBacklashActions` 那条链；原版把牌放进墓地是 `CardScript.GoToCemetery`
                //    的最后一跳 —— 逐跳判据抄在 `RuleCore.CleanupDeaths` 末尾）。
                //    ⇒ 走到这里时它**还没进弃牌堆**，`TakeFromGraveyard` 必然落空；落空时
                //    **直接沿用施放者自己那一份实例**。
                //    ⚠️ 不这么改就退成 `NewInstance`：手牌里多一份**新的**实例，而旧那一份稍后
                //    仍会被登记进弃牌堆与阵亡表 ⇒ **同一张卡既在手又在墓地**（静默的重复）。
                //    ⚠️ 那句 `TakeFromGraveyard` **留着**：从别的入口进来时（它已经登记过）
                //    仍要把 `Discard` 与 `DeadUnits` 两条记录一起撤掉。
                var back = ctx.TakeFromGraveyard(owner, ctx.ActingUnit.Instance)
                           ?? ctx.ActingUnit.Instance
                           ?? ctx.NewInstance(dead);
                // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）**：先判上限再 Add
                //   （满了 ⇒ 这份原实例进弃牌堆、手牌一张不动）。
                bool backLanded = RuleCore.AddCardToHandWithinLimit(ctx, owner, back);
                ctx.LastCreated.Clear();      // 后面那句 `It costs 4 less` 指着刚回手的那张
                if (backLanded) ctx.LastCreated.Add(back);   // 🔴 一号验收靶：`LastCreated` 装的是**实例**（第 3 步）
                ctx.Log($"{by}：「{op.Source}」→「{dead.Name}」"
                      + (backLanded ? "**回到手牌**" : "**手牌已满 ⇒ 直接进弃牌堆**（原版 `AddCardNotDrawnToHand` 同形）")
                      + "（反噬排在「进弃牌堆」之前结算 —— 原版也是这个次序 ——"
                      + "所以此刻它还在等待入墓，直接沿用那一份实例）");
                // 🆕 2026-10-26（完全复刻批次 3 · `E2`）：照 `BattleManagerSupport__BroadcastCardReturnedHand.c`
                //   广播「这张牌回到手牌」。⚠️ 走到这里时牌**还没进弃牌堆、也不在棋盘上**
                //   （反噬排在入墓之前）⇒ 只带 `card`、`subject` 传 `null`
                //   （`WhenEvents.Matches` 在 `subject == null` 时用 `card` 兜底筛条件）。
                //   `who` = **收下这张牌的那一方** = `owner`（原版极性判据看的就是它）。
                if (backLanded) RecordCardReturnedEvent(ctx, owner, back.Card, true, by);
                return true;
            }

            // 🆕 **没写主语的 `Returns to <目的地>`**（2026-09-14 A5 批 3）——
            //    `Grot Orderly` 的 `At the start of your turn, return to your hand` ·
            //    `Backlash: Returns to your hand and costs 2 more this turn`。
            //    卡面**没写「谁」回去** ⇒ `Payload` 是空的，目标落在 `op.Target` 上
            //    （`EffectTargetSpec.Subjectless`：**有施放者就是它自己**，和 `Heal N` 同一条口径）。
            //    ⇒ 把 `op.Target` 当成**唯一那个**目标规格，下面那段一个字都不用改。
            var specs = new List<EffectTargetSpec>();
            foreach (string phrase in phrases) specs.Add(EffectText.ParseTarget(phrase));
            if (specs.Count == 0)
            {
                if (op.Target == null)
                {
                    ctx.Log($"{by}：「{op.Source}」要回手，但看不出**谁**回去 —— **这条没生效**");
                    unresolved.Add(op.Source + "（return 没写目标）");
                    return false;
                }
                specs.Add(op.Target);
            }

            foreach (var spec in specs)
            {
                // 🔴 **2026-10-24（引擎线 `N1`）：目标在【墓地】的那一族走另一支** ——
                //   卡面写 `… troops that **died this game**`（`GSC55 Eve of the Uprising`，
                //   全池唯一持有者）时，原版 `targetsAffected = 60 = inCemeteryFiltered`
                //   （`AbilityLogic__GetTargets.c:949-960` → `BattleManager.GetCardsInCemetery`），
                //   **不是棋盘扫描**。判据归一口 = `EffectText.CemeteryTargetScope`（⛔ 别在这里再判一次）。
                //   ⚠️ 这段必须排在 `ResolveTargets` **之前**：改之前它落到棋盘扫描 ⇒
                //      **收走的是还活着的友方部队**（`play "Eve of the Uprising"` 实测）。
                string deadScope = spec == null ? null : EffectText.CemeteryTargetScope(spec.Raw);
                if (deadScope != null)
                {
                    if (ReturnFromCemetery(ctx, owner, by, op, spec, deadScope, moved) == 0) miss++;
                    continue;
                }

                // ⚠️ `source` 传 `null`：`ResolveTargets` 内部会**从 `ctx.ActingUnit` 回落**
                //    （`ResolveOne` 在结算每条效果之前把它设好，见那边的注释）——
                //    `Subjectless` 那一支靠它判「就是施放者自己」。本函数**没有** `source` 形参。
                // 🔴🆕 **2026-10-26（完全复刻 · `P3` · `TAU68`）**：第 4 个形参是 **`chosen`**，
                //    原来硬写 `null` ⇒ 玩家点定的目标在这一条路上**被丢掉**（判据见 `EffectDispatch`
                //    里 `"return"` 那一行上面的整段注释）。改成转发之后，口径与 `heal` / `deal` /
                //    `give` 等**其余 18 处** `ResolveTargets` 调用一致（全仓只有这里和 `Shuriken`
                //    那条「每个单位自己掷」会丢，后者按语义**应该**丢）。
                //    ⚠️ `chosen == null`（= 这张牌不需要选目标 / 调用方没点）⇒ 行为与改前**逐字相同**。
                var targets = spec == null ? new List<UnitState>()
                                           : ResolveTargets(ctx, owner, spec, null, chosen);
                if (targets.Count == 0) { miss++; continue; }

                foreach (var u in targets)
                {
                    int p, slot;
                    if (!FindUnit(ctx, u, out p, out slot)) continue;
                    var ps2 = ctx.Players[p];
                    // 🔴 **用户 2026-09-18 拍的口径**：回手 / 洗回牌库 = **同一个 `CardInstance`**
                    //    （不新建），实例态**默认保留**、效果的后续分句可以显式改它
                    //    （`Master of Manoeuvre` 的 `It costs 4 less` 正是那样一条分句）。
                    var inst = u.Instance;
                    // 🔴 2026-10-01：回手/回牌库 = 原版 `_ResolveRecallToHand/_ResolveRecallToDeck`
                    //    两支都走 `MinionManager.RemoveMinion`（`List.Remove`）⇒ **外侧的单位内移一格补位**。
                    BoardSlots.RemoveAt(ps2, slot);
                    Auras.Recompose(ctx);      // 🆕 A7：棋盘变动 ⇒ 光环重算

                    switch (op.Dest)
                    {
                        case "hand":
                            // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-5`）**：先判上限再 Add
                            //   （满了 ⇒ 这份实例进弃牌堆，收牌方手牌一张不动）。
                            RuleCore.AddCardToHandWithinLimit(ctx, p, inst);
                            break;
                        case "decktop":
                            ps2.Deck.Add(inst);      // 牌库**顶** = 列表末尾（`Draw` 从末尾 pop）；**不洗**
                            break;
                        default:                     // `deck` —— 规则书 :455「shuffled in」
                            ps2.Deck.Add(inst);
                            Shuffle(ps2.Deck, ctx);
                            break;
                    }
                    ctx.Emit(new BattleEvent { Kind = EvtKind.Return, Player = p, Slot = slot, CardId = inst.Card.Name,
                                               InstanceId = u.InstanceId });
                    // 🆕 2026-10-26（完全复刻批次 3 · `E2`）：逐张广播回手 / 回牌库
                    //   （原版 `BroadcastCardReturned{Hand,Deck}` 对**每一张**被挪走的牌各发一次，
                    //    见 `BattleManagerSupport__BroadcastCardReturnedHand.c:20`）。
                    //   `who` = `p`（收下这张牌的那一方）；`dest == hand` 决定种类，
                    //   `deck` / `decktop` 都算「回牌库」（原版两个广播器的差别只看去手还是去牌库）。
                    RecordCardReturnedEvent(ctx, p, inst.Card, op.Dest == "hand", by);
                    moved.Add(inst);
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
            // 🔴 **一号验收靶**（待办第 7 行）：`LastCreated` 装的是**哪一份实例** ——
            //    所以「手里两张同名载具时两条一起降价」这个老毛病在这里被收掉。
            ctx.LastCreated.Clear();
            foreach (var c in moved) ctx.LastCreated.Add(c);

            string where = op.Dest == "hand" ? "手牌" : (op.Dest == "decktop" ? "牌库顶" : "牌库（已洗）");
            ctx.Log($"{by}：「{op.Source}」把「{Names(moved)}」放回{where}");
            return true;
        }

        /// <summary>
        /// 🔴 **2026-10-24（引擎线 `N1`）：`Return … troops that died this game …` 的【墓地】那一支。**
        ///
        /// <para>**改前是错的**（铁律 5 留痕）：`DoReturn` 对所有 `return` 目标都走
        /// `ResolveTargets`（**棋盘扫描**），于是 `GSC55 Eve of the Uprising`
        /// （卡面 `Return up to 5 friendly troops that **died this game** to your deck`）
        /// **把棋盘上还活着的友方部队收走** —— 实测 P0 三张满血部队被放回牌库，与卡面正相反。</para>
        ///
        /// <para>**判据（第一权威 = 反编译方法体 + 卡数据）**：
        ///   · `AbilityLogic__GetTargets.c:949-960`：`targetsAffected == 0x3c(60)` 那一支
        ///     调 `BattleManager.GetCardsInCemetery(manager, includeMinions, includeSpells,
        ///     includeWhispers, includeWeapons, …)` —— **候选取自墓地列表**，不是场上；
        ///   · `BattleManager__GetCardsInCemetery.c`：按 `playerObject + 0x110` 那个列表**正序**
        ///     （`iVar5 = 0..n`，= **最早进墓的在前**）、排除 `cardType == 10(hero)`、
        ///     `isRemnant(+0x65) != 0` 的条目；
        ///   · 卡数据 `cards_engine.json` 的 `GSC55.abilityTargets[0].ta = [60, 150, 150]`、
        ///     `abilityMultipliers[0].fm = [5, 0, 0]`（= 最多 5 张）。</para>
        ///
        /// <para>**候选用 `BattleContext.DeadUnits`**（本局阵亡流水，`Owner`/`DeathTurn`/`Instance`
        /// 都在）：它**正是**「本局死过的部队」那一份表（`Discard` 混装战术卡、且没有死亡时间）。
        /// 取走时走 `BattleContext.TakeFromGraveyard`（`Discard` 与 `DeadUnits` **一起**移除 ——
        /// 那条规则只写在那一个地方）。</para>
        ///
        /// <para>⚠️ **与棋盘完全解耦**：本支**不碰棋盘**（不 `RemoveAt`、不 `Auras.Recompose`）
        /// —— 这也是本笔的判别式：**棋盘上有活人时它们一张都不该被收走**。</para>
        ///
        /// <para>⚠️ **已知口径差（如实标）**：原版 `isRemnant(+0x65)` 那道闸我们没有对应物
        /// （`DeadUnits` 只存卡模板、分不出「它死时是不是残骸」）。今天**全池只有 `GSC55` 一张**
        /// 会走到这一支（`cards_engine.json` 1127 张现扫：`return` ＋ 目标短语含 `died` 的只有它），
        /// 而残骸被摧毁那一档（`RuleCore.CleanupDeaths` 的 `u.IsRemnant` 支）**确实**会把卡登进
        /// `DeadUnits` ⇒ 将来若有卡面写「return … **remnants** that died …」，要回来补这一维。
        /// </para>
        ///
        /// <returns>真的搬走的张数（0 = 墓地里没有符合的，调用方记一条 `miss`）。</returns>
        static int ReturnFromCemetery(BattleContext ctx, int owner, string by, EffectOp op,
                                      EffectTargetSpec spec, string deadScope, List<CardInstance> moved)
        {
            // 谁的墓地：`friendly`（= own，默认）用施放方；`enemy` 用对面；其余按自己。
            int sideOwner = spec.Side == "enemy" ? 1 - owner : owner;
            // 兵种/关键词/具名这三道筛 —— 走**同一份翻译**（`CardCriteria.FromTarget`，
            // ⛔ 别在这里另写一套；`troop` 在它那里 = `Type == "unit"`，天然排除督军）。
            var crit = CardCriteria.FromTarget(spec);

            int limit = op.Amount > 0 ? op.Amount : int.MaxValue;   // 卡面 `up to 5` / `all`
            int mark = ctx.Players[sideOwner].LastTurnStartMark;

            // ---- ① 先挑（**不改表**：`TakeFromGraveyard` 会从 `DeadUnits` 里删条目）----
            //   顺序 = `DeadUnits` 的**正序**（它是按死亡时间 append 的）= 原版 cemetery 列表序
            var picks = new List<DeadUnit>();
            for (int i = 0; i < ctx.DeadUnits.Count && picks.Count < limit; i++)
            {
                var d = ctx.DeadUnits[i];
                if (d == null || d.Card == null) continue;
                if (d.Owner != sideOwner) continue;
                if (!d.Card.IsUnit) continue;                     // 原版只收「在场过的单位」
                if (deadScope == "since_last_turn" && d.DeathTurn < mark) continue;
                if (deadScope == "this_turn" && d.DeathTurn != ctx.Turn) continue;
                if (crit != null && !crit.Matches(d.Card)) continue;
                picks.Add(d);
            }

            if (picks.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要在**墓地**里取"
                      + $"「{spec.Raw}」，但墓地里没有符合条件的部队 —— **这条没生效**"
                      + "（原版这一支的 `targetsAffected` = 60 = inCemeteryFiltered，**不扫棋盘**）");
                return 0;
            }

            // ---- ② 逐张取走并搬进目标区 ----
            int n = 0;
            foreach (var d in picks)
            {
                // 精确到**那一份实例**（同名死了两次不会拿错）；老记录（`Instance == null`）退回按模板。
                var inst = d.Instance != null
                         ? ctx.TakeFromGraveyard(sideOwner, d.Instance)
                         : ctx.TakeFromGraveyard(sideOwner, d.Card);
                if (inst == null)
                {
                    ctx.Log($"{by}：「{op.Source}」要在墓地里取「{d.Card.Name}」，"
                          + "但弃牌堆里已经找不到那一份 —— 跳过（不出声就是静默少一张）");
                    continue;
                }
                var ps2 = ctx.Players[sideOwner];
                switch (op.Dest)
                {
                    case "hand":
                        // 满手 ⇒ 这份进弃牌堆（`AddCardToHandWithinLimit` 自带出声），与原版 `AddCardNotDrawnToHand` 同形
                        RuleCore.AddCardToHandWithinLimit(ctx, sideOwner, inst);
                        break;
                    case "decktop":
                        ps2.Deck.Add(inst);      // 牌库顶 = 列表末尾；**不洗**
                        break;
                    default:                     // `deck` —— 规则书 :455「shuffled in」
                        ps2.Deck.Add(inst);
                        Shuffle(ps2.Deck, ctx);
                        break;
                }
                // 事件：这一格**不在场**（`Slot = -1`）⇒ 表现层不该去棋盘上找它
                ctx.Emit(new BattleEvent { Kind = EvtKind.Return, Player = sideOwner, Slot = -1,
                                           CardId = inst.Card.Name, InstanceId = inst.Id });
                moved.Add(inst);
                n++;
            }
            return n;
        }

        /// <summary>卡名列表 → 可读字符串（日志用）。**复用上面那个已有的 `Names`**，
        /// 别在同一个类里写第二份 —— 两份迟早不一致。</summary>

        // ==================================================================
        //  常驻效果 / 手牌陷阱（回合起止触发）—— 卡面 `For the rest of this battle, …` 那一族
        //
        //  🔴 **2026-10-14（账 `A1950`）：判据换成原版。** 本段原来引的是
        //     《Warpforge Offline Rulebook》英文版 `:39-41`「Persistent Effects = 写
        //     `For the rest of this battle` 的卡**被弃置后依然生效**，要单独放一摞备查」
        //     —— 那份**自己第 5 行写着 `Not official. Fan project.`**（见 `CLAUDE.md` §一·2）
        //     ⇒ **粉丝实体版，⛔ 不当判据**（原话留在这里当痕迹）。
        //
        //  **原版实际上是怎么表达「本场战斗有效」的**（判据 = `d:/2/tools/decomp_full/`）：
        //   · **没有 `PersistentEffect` 这个类型** —— 全量 `dump.cs` 里 `PersistentEffect`
        //     **0 命中**；13 个带 `persistent` 的类型（`I2BasePersistentStorage` /
        //     `DebugUIHandlerPersistentCanvas` / `PersistentCall` …）**全是 I2 / Addressables /
        //     dots / Unity 的**，与卡牌无关。⇒ 本文件这个 `PersistentEffect` 是**我们自设计的**，
        //     原版没有对应物，⛔ 别写成「原版有这一层」。
        //   · **没有「本场」这个时长旗标**：`CardEffect` 只有回合级的 `untilEndOfTurn(0x32)` /
        //     `untilPlayerTurnStart(0x33)` / `untilEnemyTurnStart(0x34)` /
        //     `untilFollowingPlayerTurnStart(0x35)` / `untilNextAttack(0x36)` + `permanentBuff(0x74)`
        //     ⇒ **一个旗标都不带的 `CardEffect` 就是「本场」的**（靠「没人来清它」表达）；
        //     回合边界只清 `untilEndOfTurn` 那一档（`CardScript__ClearEndOfTurnEffects.c:23,27`）。
        //   · **真正的机制 = 把效果【搬离打出的那张牌】，挂到一张还活着的载体牌身上**：
        //     ① `CardEffect{buffType = enchantment(12) / singleUseEnchantment(15) /
        //        choiceEnchantment(16), enchantment = <附魔卡>}` → `CardScript__AddEffect.c:826-828`
        //        → `CardScript__AddEnchantment.c:25,37,43`（挂进 `card.activeEnchantments`，`+0x110`）；
        //     ② `BuffType.replaceAbility(40)` → `CardScript__AddEffect.c:242-251`：
        //        `card.activeAbilityRef(+0x118) = effect.enchantment`、`hasActiveAbility = 1`。
        //     之后派发触发时 **`RawCardScript__TriggerAbilities.c:50-88` 会把这张牌
        //     `activeEnchantments(+0x110)` 里每个附魔的 `cardAbilities(+0x290)` 一起算进来**
        //     （`RawCardScript.GetAbilitiesForTrigger(thisCard, trigger, includeEnchantments)`）
        //     ⇒ **载体牌在场一天，这份能力就有效一天**；载体不在场了，能力随之没了。
        //     ⚠️ 打出的那张战术卡本身**进弃牌堆**：`CardScript__GoToCemetery.c:8` 置
        //     `cardState = inCemetery(6)` → `:17 BattleManager__AddToCemetery` →
        //     `CemeteryManager__AddCardToCemetery.c:17,52`（入 `cemeteryCardList` + `SetActive(false)`）
        //     —— **弃牌堆里那张牌只是记录，不是效果来源**。
        //   · **触发只送到「场上 + 双方手牌」，弃牌堆不在任何遍历范围内**：
        //     `BattleManagerSupport__BroadcastTurnStart.c:18,25,33,42` ·
        //     `…__BroadcastTurnEnd.c:18,26,45,54` · `…__BroadcastUnitSummoned.c:20,28,44,54`
        //     —— 三处都只走 `BattleManager.unitsInPlay(+0x470)`（且要 `CardScript.IsInPlay`）
        //     + 当前回合方手牌 + 另一方手牌。⇒ **原版不可能让一张已经弃置的牌自己继续触发**；
        //     粉丝规则书那句「单独放一摞备查」是**实体台面**的备忘办法，数字版没有对应物。
        //   · **来源离场时的两种处置，判据 = `CardEffect.extrinsic(0x30)`**（工具提示
        //     "Effect is tied to acting card, not target card"）+ `CardEffect.enchantingCard(0x18)`：
        //     ① **有绑定**（`extrinsic` 为真且 `enchantingCard == 离场那张`）⇒ **主动撤回**：
        //        `PlayerHand__RemoveFromGameCard.c:28,33,39`（手牌那一份）·
        //        `BattleManager__RemoveExtrinsicEffectsFrom.c:36,45,80`（场上/手牌那一份，
        //        调用点 `CardScript__ResolveDeadCard.c:244` · `CardScript__AddEffect.c:823`）；
        //     ② **没绑定** ⇒ 它本来就不在离场那张牌身上（在玩家手上 / 在某张活着的载体上）⇒ 继续有效。
        //   ⇒ **一句话**：原版**有**「本场战斗有效」这个概念，实现方式是
        //     「**把效果交给一张活着的载体牌**」；**没有**「弃置后由那张牌自己继续生效」这条路。
        //     我们这摞 `PersistentEffect`（谁 + 来源卡 + 时机 + 正文，登记后**永不清理**）是
        //     **我们自己的选型**，落点与上面第 ② 支（没绑定 ⇒ 继续有效）的结果一致。
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
                Source = ctx.PlayingCard,       // 来源卡：日志/排查用（我们这摞是自设计结构，见 `A1955`）
                Phase = op.AtTurnPhase,
                // `at the start|end of your turn` 走回合段；`deploy` 走**部署事件**
                // （原版 `OtherUnitSummoned=190`，见 `BattleContext.PersistentEffect.Trigger`）；
                // 🆕 2026-09-14 A4 批 3：**第三类 `when`** —— `For the rest of this battle,
                // when <事件>, <正文>`（`Raid Tactics`）。判据在 `op.When`，
                // 由 `BroadcastPersistentWhen` 在每次事件广播时扫。
                Trigger = op.AtTurnPhase == "deploy" ? "deploy"
                        : (op.AtTurnPhase == "turn_start" || op.AtTurnPhase == "turn_end") ? "turn"
                        : "when",
                Ev = op.When,
                Criteria = op.Filter,
                Ops = op.AtTurnOps,
                Body = op.Payload,
            });
            // 日志要打人话：`deploy` 不是回合的某一刻，别套「自己回合开始/结束」那两句
            if (op.AtTurnPhase == "deploy")
            {
                string what = op.Filter != null && !op.Filter.IsEmpty ? op.Filter.ToString() : "任意单位";
                // 🔴 2026-10-14（账 `A1666`）：下面**两句**原来结尾都带「（**规则书 :39-41**）」——
                //    那份《Warpforge Offline Rulebook》**自己第 5 行写着 `Not official. Fan project.`**
                //    ⇒ **粉丝实体版、⛔ 不当出处**，已从**日志字符串**里删掉（见 `CLAUDE.md` §一·2）。
                //    ⚠️ 「弃置后照样生效」这件事的判据**不是**它：卡面原文写着 `For the rest of this battle`
                //       （成品卡图 = 可逐张核），原版侧另有 `AbilityTrigger.OtherUnitSummoned = 190`
                //       （现读 `d:/2/tools/il2cpp_out/dump.cs:20268`）。
                ctx.Log($"{by}：「{op.Source}」登记了**常驻效果**（自己部署 {what} 时 → {op.Payload}）"
                      + " —— 这张卡之后被弃置也照样生效");
                return true;
            }
            string when = op.AtTurnPhase == "turn_start" ? "自己回合开始" : "自己回合结束";
            ctx.Log($"{by}：「{op.Source}」登记了**常驻效果**（{when} → {op.Payload}）"
                  + " —— 这张卡之后被弃置也照样生效");
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
            int amount = op.Amount > 0 ? op.Amount : 1;

            // ---- 🆕 2026-09-14 A5 批 3 第 1 条：**这张卡自己**加价 ----
            //   `Backlash: Returns to your hand and costs 2 more this turn`（`Makari the Grot`，全池唯一）。
            //   走 `costmore` 这个动词、靠 `Payload` 的哨兵值分流（带 `Target` 的那一支才是「加给某一类牌」）。
            //   照旧**不改卡面费用**：登记一条 `CostMod`，由 `RuleCore.CostOf` 现算 ——
            //   原版的 `costChange` 也是挂在卡上现算（见 `CostMod` 的类注释），`CardDef` 是共享对象、
            //   动它会污染整个卡池。
            if (op.Payload == EffectText.SelfCostMoreMarker)
            {
                // 结算时「这张卡」是谁：战术卡走 `PlayingCard`；`Backlash` 这类**单位触发**
                // 由 `ResolveOne` 设的 `ActingUnit` 更可靠（触发正文不一定写 `PlayingCard`）。
                var card = (ctx.ActingUnit != null ? ctx.ActingUnit.Card : null) ?? ctx.PlayingCard;
                if (card == null)
                {
                    ctx.Log($"「{op.Source}」要给「这张卡」加价，但结算时认不出是哪一张 —— **这条没生效**");
                    unresolved.Add(op.Source + "（自己加价说不出是哪张卡）");
                    return false;
                }
                // 🔴 第 7 行第 3 步：**钉到「那一份」上** —— 这张卡刚刚被前面的 `Return` 放回手里，
                //    指代槽（`LastCreated`）里就是它；拿不到才退回按卡 id 匹配（老行为）。
                CardInstance pin = null;
                if (ctx.LastCreated.Count > 0
                    && ReferenceEquals(ctx.LastCreated[ctx.LastCreated.Count - 1].Card, card))
                    pin = ctx.LastCreated[ctx.LastCreated.Count - 1];
                else if (ctx.ActingUnit != null && ReferenceEquals(ctx.ActingUnit.Card, card))
                    pin = ctx.ActingUnit.Instance;
                ctx.CostMods.Add(new CostMod
                {
                    Player = owner,
                    // 钉得住份就钉份（`*` + `HandInstanceId`），钉不住才按 **id** 挂
                    //（同名跨阵营的卡是两张，别一起加价）
                    Key = pin != null ? "*" : card.Id,
                    HandInstanceId = pin != null ? pin.Id : 0,
                    Delta = +amount,
                    ExpireTurn = op.Duration == "turn" ? ctx.Turn : -1,
                });
                ctx.Log($"{ctx.Players[owner].Name}：「{op.Source}」→「{card.Name}」费用 +{amount}"
                      + (op.Duration == "turn" ? "（本回合）" : "")
                      + $"（现价 {(pin != null ? RuleCore.CostOf(ctx, owner, pin) : RuleCore.CostOf(ctx, owner, card))}，算费用时现查）");
                return true;
            }

            if (op.Target == null || string.IsNullOrEmpty(op.Target.Kind))
            {
                ctx.Log($"「{op.Source}」要加价，但看不出加在哪类牌上 —— **这条没生效**");
                unresolved.Add(op.Source + "（持续改费说不出加给哪类牌）");
                return false;
            }
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
        ///
        /// <para>🆕 **2026-10-19（第五十七会话 `W57` · T21 C/D）：返回值 = 「这条 ability 激活了没有」**。
        /// 判据 = **至少有一条 op 没被 `instead` 跳掉、也没被付费组拦下**
        /// （`ChargePaidGroups` 的 `paidOk`）—— 也就是原版「这条 ability 真的被触发了」。
        /// 改前是 `void`；改成 `bool` 对既有调用点**零影响**（语句调用可以忽略返回值）。
        /// 唯一读点 = `ResolveSpiritAbility` / `ResolveFaithAbility` 拿它决定要不要发
        /// `UseSpiritStone(600)` / `UseFaith(605)` 那一拍 —— **付不起时原版整条不激活、也就不该有那拍动画**。</para>
        /// <para>🆕 **2026-10-20（第五十七会话 `FACTION-DOUBLE605`）**：可选 `activatedGroups` ——
        /// 给了就回填**这次激活了哪几个付费组**（每个付费组首那条 `EffectOp`，按 ops 顺序）。
        /// 判据 = `ChargePaidGroups` 的 `paidOk[组首]`（门槛/余额过了 = 原版那条 ability 激活）；
        /// 唯一读点 = `EmitFaithAbilityUseTriggers`（同一张卡两条 605 能力各发一次）。
        /// ⛔ 别拿它代替返回值（返回值是「整条 ability 激活了没有」的另一口径）。</para>
        /// </summary>
        public static bool ResolveOps(BattleContext ctx, int owner, UnitState source,
                                      IReadOnlyList<EffectOp> ops, string by = "效果",
                                      UnitState seed = null, List<EffectOp> activatedGroups = null,
                                      // 🆕 2026-10-25（完全复刻 · 执行代理 A · B 域 `R1` P0）：**玩家点定的那个目标**。
                                      //   原版 `_ResolvePlayCardFromHand…MoveNext.c:905-940` 把玩家点的目标写进
                                      //   `action+0x20`，ability 结算时 `AbilityLogic.GetTargets` 优先用它
                                      //   （`GetTargets` 里 `has_pick_target` 那一支 ⇒ 我们的 `chosen`）。
                                      //   ⚠️ 只有「出牌两段式」那条路会传；其余调用点保持 `null`（行为不变）。
                                      UnitState chosen = null)
        {
            if (ctx == null || ops == null || ops.Count == 0) return false;

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
            //
            // 🔴 **2026-10-18（第三会话 · `A976` 复核）：`LastCreated` 也要在这个入口清。**
            //    本批先前只在**另一个重载**（`ResolveOps(…, out unresolved)`，出牌 / 战术卡那条）的清槽段里
            //    加了一句 `ctx.LastCreated.Clear();` —— 而**触发层走的是这个重载**
            //    （8 个调用点：`DoPaidMod` / `ResolveAtTurn` / `ResolveSpiritAbility` /
            //      `ResolveOathAbility` / `BroadcastWhen` / `BroadcastPersistentWhen` /
            //      `BroadcastHandTrapWhen` / `BroadcastHandWhen`）⇒ **抽牌那一跳根本没被清到**。
            //    **判据**（只读诊断 + 原版反编译）：原版**只有一个**指代槽
            //    （`AbilityLogic__GetRefCard.c` 取 `previousAbilityTargets[0]`），
            //    写点唯一且是**整表覆盖**（`AbilityLogic__SetPreviousAbilityTargets.c` = `Array.Clear` + `AddRange`，
            //    由 `PlayAbility.c:2743-2745` 在**该能力末尾**填）⇒ **「上一条能力留下的值参与下一条能力判定」原版不存在**。
            //    而我们这条槽会**跨过整个对手回合活到我方抽牌**（对方不出牌时），
            //    于是 `When you draw a card, it costs 1 less` 会把降费给**上一张造出来的牌**。
            //    ⚠️ **只清 `LastCreated`、⛔ 别顺手清 `DrawnThisResolve`** ——
            //    那会把 `TestS10CompanyMasterLowerCost` ①（靠 `DrawReferentScope` 临时种值）改红。
            //    ⏭ **结构性根治仍开着**（把这条槽做成随结算帧的局部表，照原版 `AbilityLogic+0x80` 的形态）⇒ 见 A 表。
            ctx.LastCreated.Clear();

            // 🔴 **2026-10-18（第三会话 · `A993`）：其余三个指代槽也在本入口清 —— 口径与上面那句逐字相同**
            //    （「**一条能力一个窗口**」；原版判据 `AbilityLogic.SetPreviousAbilityTargets` =
            //     每一条能力先 `Array.Clear` 再 `AddRange`，**写点唯一**、整表覆盖 ⇒
            //     「上一条能力留下的值参与下一条能力判定」原版不存在）。
            //
            //    **为什么这一批要补**：`ResolveOps` 有**两个重载**，本批之前只在**重载 #1**
            //    （出牌 / 战术卡那条，`EffectResolver.cs` 开头那段清槽）上清了这四个槽；
            //    **触发层走的是本重载**（8 个调用点见上面那段注释）⇒ 触发层里
            //    「上一张卡留下的代词」照样跨卡存活。`A976`（`LastCreated`）是同一族的第一笔，
            //    本笔把剩下三个槽一并补齐。
            //
            //    ⛔ **`DrawnThisResolve` 故意不清**（不是漏）—— 它的窗口**不是**「一条能力」：
            //      `RuleCore.Draw` 在 `BroadcastWhen(Draw, …)` **外面**套了 `DrawReferentScope`
            //      **显式把槽种成「刚抽到的那一份」**（`EffectResolver.DrawReferentScope` 头注释；
            //      `S10` / `A962`），监听器的正文正是要在广播**进行中**读它
            //      ⇒ 在这儿清会**吃掉调用方刚种下的值**，`Company Master`（`DA31`）的降费**重新失效**
            //      （`TestS10CompanyMasterLowerCost` ① 会红）。
            //      判据差别一句话：**那两个手槽是「谁写谁清」，这一个槽是「调用方种、op 读」。**
            ctx.LastHandTargets.Clear();
            ctx.LastHandTarget = null;
            ctx.LastChosenCard = null;

            var savedTargets = new List<UnitState>(ctx.LastTargets);
            var savedLast = ctx.LastTarget;
            var prime = seed != null ? seed : source;
            // 🔴 **2026-10-18（`A993`）：「上一条效果打中的那一批 / 那一个」也从【空】起手。**
            //    `prime` 非空（= 这条能力有主语）⇒ 它就是这一条能力的第一号目标（老行为，一字不动）；
            //    `prime` 为空 ⇒ 这一条能力**没有主语**，槽就该是空的 ——
            //    改之前它留着**调用方**的那一批（`BroadcastHandTrapWhen` 那条路**既不给 `source`
            //    也不给 `seed`**，`BroadcastPersistentWhen` / `BroadcastHandWhen` 在 `subject == null`
            //    时也一样，`DoPaidMod` 在 `ActingUnit`/`chosen` 都空时也一样）
            //    ⇒ 监听器正文里的 `them` / `it` 会**静默**落到外面那张卡的目标上。
            //    ⚠️ **必须清在 `savedTargets` 之后**：那一次保存是为了**退出时还原调用方的值**
            //    （`BroadcastWhen` 的棋盘那一跳、`ResolveDeploy` 都指望它），清在保存之前会把
            //    调用方的种子一起抹掉 ⇒ 那是**行为变化**，不是收口。
            //    ⚠️ 实测口径：今天全池 **1126 张里一张都不依赖**这一支的残值
            //    （`prime == null` 能走到的三类监听器 —— 手牌陷阱（`At the end of your turn`）·
            //      常驻效果（`For the rest of this battle`）· 手牌监听（非单位卡的 `When …`）——
            //      这三族的 `desc` **逐张核过：没有一张**在正文里用 `them` / `it` 指代「上一条效果的目标」）
            //    ⇒ 本句今日**零可观测差异**，钉住的是「将来新增监听器不许漏出去」。
            ctx.LastTargets.Clear();            // 🆕 2026-10-24（执行代理 `E2`）：与另一个重载同一句 —— 目标并集从空起手。
            ctx.AbilityTargetsSaved.Clear();
            ctx.LastTarget = null;
            if (prime != null)
            {
                ctx.LastTargets.Add(prime);
                ctx.LastTarget = prime;
            }

            var unresolved = new List<string>();
            // ---- 🔴 2026-10-12（`A1412`）：**收费抬到 `ability` 层** ----
            // 本重载才是「**一条 ability 被激活**」的入口（灵魂石 / 誓约 / 信仰 / 事件触发 / 常驻 /
            // 手牌陷阱 / 突触邻居 / 死亡结转 …，见上面那 8 个调用点）⇒ 付费前缀必须在
            // **这一条 ability 动手之前**按组收一次（判据与算法 → `ChargePaidGroups` 的 doc）。
            // ⚠️ **改前这里没有这一层**：付费是在逐条 op 的 `ResolveOneCore` 里判的，靠解析层的
            //    `CostShared` 标记保证「同一份前缀只收一次」。那套在执行**次序**上有个漏：
            //    付钱那条**付不起**时，同组标着 `CostShared` 的其余 op **不查余额照常结算**
            //    ⇒ 白给（`A1413` ② 那一笔只堵住了另一个重载的那条路）—— 本段一并堵上。
            // ---- 🔴 2026-10-22（`A1685`）：**`instead` 在能力层也要消费** ----
            //  改前这一条路**一个字都没有** ⇒ 卡面写 `…instead` 的两条 op **都**结算，与出牌那条路
            //  行为不一致（实测 `Rally: Deal 3 damage to an enemy. Deal 6 damage instead`
            //  ⇒ 出牌路 6 伤（对）· 能力路 **9 伤**（错））。
            //  ⚠️ **规则只有一份** = `InsteadSkips`（⛔ 别在这儿内联一段配对的副本）。
            //  ⚠️ **有条件的那一族**：2026-10-18（`T02`）起也配对了 —— 能力层没有 `chosen`，
            //     但 `InsteadSkips` 会**先解出被替换那条的目标**当 referent 判条件（判完还原），
            //     ⛔ 别再用「判的是施放者」那个理由把条件档排除（那会回到 `3 + 6` 双结算）。
            //  ⚠️ **位置**：必须排在下面 `ChargePaidGroups` **之前**（同出牌那条路：先配对裁决、
            //     再收钱 —— 收费会改 `Energy`，而已有卡的 `instead` 条件会读余额，如 `SOR6`）。
            var plan = InsteadSkips(ctx, owner, ops, null, pairConditional: false, source: source,
                                    card: source != null ? source.Card : null);
            var skip = plan.Skip;

            // ================================================================
            //  🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 C）：整条 ability 触发 N 遍**
            //
            //  判据 = `RawCardScript__TriggerAbilities.c:226-259`：`multiCountCriteria != null`
            //  且 `targetsAffected != 0` ⇒ 先 `GetTargets(...)` 求候选表，再
            //  `for (i < 表长) TriggerAbility(...)`；**表空（N = 0）⇒ 一次都不触发**
            //  （连付费都不发生，所以下面的 `ChargePaidGroups` 也一并跳过）。
            //  `targetsAffected == 0 (any)` ⇒ 那条 criteria 不携带任何信息 ⇒ 不覆盖正文（见
            //  `TryStructuredMultiTrigger` 的 doc）。
            // ================================================================
            int abilityRepeats = 1;
            {
                int mn; string mgap;
                if (TryStructuredMultiTrigger(ctx, owner, ops, source != null ? source.Card : null, source,
                                              out mn, out mgap))
                {
                    abilityRepeats = mn;
                    if (mn != 1)
                        ctx.Log($"{by}：原版 `multiCountCriteria` = **{mn}** ⇒ **整条能力触发 {mn} 遍**"
                              + "（`RawCardScript__TriggerAbilities.c:226-259`）");
                }
                else if (mgap != null)
                {
                    StructuredConditionNotice("multi-gap|" + (source != null && source.Card != null ? source.Card.Id : "?") + "|" + mgap,
                        $"「{by}」的 `multiCountCriteria`（角色 `multi`）**本版判不了**：{mgap}"
                      + " ⇒ 退回正文口径（`for each …`）。");
                }
                if (abilityRepeats <= 0)
                    ctx.Log($"{by}：原版 `multiCountCriteria` 数出 **0** 个候选 ⇒ **整条能力不触发**"
                          + "（`RawCardScript__TriggerAbilities.c:226-259`：列表为空 = 一次都不触发）");
            }

            bool anyRunnable = false;              // 🆕 W57：这条 ability 激活了没有（见返回值的 doc）
            if (abilityRepeats > 0)
            {
            var paidOk = ChargePaidGroups(ctx, owner, source, by, ops, null, unresolved);
            // 🆕 2026-10-20（`FACTION-DOUBLE605`）：把「哪几个付费组收到钱」回填给调用方 ——
            //   组首 = `Cost > 0 && !CostShared`（判据同 `ChargePaidGroups` / `EffectOp.CostShared`）。
            //   原版那一条 ability 激活 = `CanTriggerAbility` 的门槛过了；在我们这里就是这一位。
            if (activatedGroups != null)
                for (int i = 0; i < ops.Count; i++)
                {
                    var gop = ops[i];
                    if (gop == null || gop.Cost <= 0 || gop.CostShared) continue;
                    if (paidOk[i]) activatedGroups.Add(gop);
                }
            for (int r = 0; r < abilityRepeats; r++)
            for (int i = 0; i < ops.Count; i++)
            {
                if (skip[i]) continue;             // 🆕 `A1685`：这条被 `instead` 替换掉了
                if (!paidOk[i]) continue;          // 这一条所属的付费前缀**没收到钱** ⇒ 整组不结算
                anyRunnable = true;
                // 🆕 2026-10-23（`A12`）：`instead` 的条件已由 `InsteadSkips` 在**动手之前**判过
                //    ⇒ 这里不再判第二次（同出牌路；理由见 `InsteadSkips` 的 doc「条件只判一次」）。
                var op = plan.Judged[i] ? WithoutCondition(ops[i]) : ops[i];
                // 🔴 2026-10-19（`A2001` / E9）：触发正文里的 `equal to …` 也要换成具体数值 ——
                //   判据与出牌路**同一个函数**（`TryApplyAmountRef`），⛔ 别在这儿另写一份。
                //   改前这条路没有这一跳 ⇒ 所有触发里的 `equal to your Faith / 敌方手牌数` 都按 0 结算。
                EffectOp amtOp;
                if (!TryApplyAmountRef(ctx, owner, by, op, unresolved, out amtOp)) continue;

                // 🔴 **2026-10-24（`G6` · `C6` §1·3 C）：这条 op 自己的 `repeats`**
                //   —— 判据与出牌路**同一份**（`TryComputeRepeats`），不要再抄一遍。
                //   ⚠️ `CountScope == "died"` 一档**不套用**（触发本身**就是**那次死亡事件
                //   （`OtherUnitDeath(90)` / `UnitDeathByAbility(180)`），再乘「本回合累计阵亡数」= **平方**；
                //   原版那两张卡的 `multiCountCriteria` 现读 `ta = 0 (any)`（`AM34` / `TL46`）= 数据也说不重复）。
                //
                // 🔴 **2026-10-24（执行代理 `E1` · `C3` §1·1）：`PerCount` 增量型** —— 改前这一档
                //   在本重载里被**整条丢掉**（只打一条 `trigger-repeat-percount` 提示、`opRepeats` 保持 1），
                //   而 `ResolveOne` **不读 `PerCount`** ⇒ 触发正文里的 `give/gain/lose` 只剩**基础值**
                //   （静默少数）。现在与出牌路**同一个入口**：`IsPerCountIncrement` ＋ `TryFoldPerCount`
                //   （原版 = 一次结算、数值乘进去，⛔ 不是跑 N 遍；判据见那个方法的 doc）。
                int opRepeats = 1;
                EffectOp runOp = amtOp;
                if (IsPerCountIncrement(amtOp))
                {
                    if (!TryFoldPerCount(ctx, owner, source, by, amtOp, source,
                                         source != null ? source.Card : null, unresolved, out runOp))
                        continue;                  // 计数数不出来 ⇒ 整条跳过（日志/unresolved 已写）
                }
                else if (amtOp.CountScope == "died")
                {
                    StructuredConditionNotice(
                        "trigger-repeat-died|" + (source != null && source.Card != null ? source.Card.Id : "?") + "|" + amtOp.Verb,
                        $"「{by}」的「{amtOp.Source}」是 `died` 档的 `for each` —— 触发层**不按它重复**"
                      + "（这条触发本身就是那次死亡事件，再乘累计阵亡数就是平方；"
                      + " 原版这两张卡的 `multiCountCriteria` 现读 `ta = 0 (any)` ⇒ 不重复）");
                }
                else if (!TryComputeRepeats(ctx, owner, source, amtOp,
                                            source != null ? source.Card : null, source, by, unresolved,
                                            out opRepeats))
                    continue;                      // 计数数不出来 ⇒ 整条跳过（日志/unresolved 已写）

                if (opRepeats <= 0)
                {
                    ctx.Log($"{by}：「{amtOp.Source}」按 `{amtOp.CountRef}` 计 **0** 遍 ⇒ 这条什么都不做");
                    continue;
                }
                // 🔴 2026-10-26（批次 8 · `B8_P`）：触发路同一个口径 —— 阵营资源的乘数**乘进数值**、只结算 1 遍
                //   （判据 `AbilityLogic__AdjustManaEffect.c:5-7`；与出牌路**共用** `FoldManaMultiplier`）。
                runOp = FoldManaMultiplier(ctx, by, runOp, ref opRepeats);
                // 🆕 2026-10-22（`W64` · `B-16`）：触发路的卡 = `source.Card`（`source` = 触发的那张）。
                for (int k = 0; k < opRepeats; k++)
                {
                    ResolveOne(ctx, owner, source, by, Prepaid(runOp), chosen, unresolved,
                               card: source != null ? source.Card : null);
                    NotePreviousAbilityTargets(ctx);   // 🆕 `E2`：触发路同一个口径（逐 op 同步）
                }
            }
            }

            // 🔴 **2026-09-16：这里原来「建了、传了、从来没读」。**
            //    这个重载是**所有触发层**的入口（事件触发 / 常驻效果 / 灵魂石 / 誓约 /
            //    手牌陷阱 / 突触邻居 / 死亡结转 …），不读就等于**这些路上的丢段一条日志都不打**
            //    （实测：`DA21 Inner Circle Companion` 走 agenda 触发就是这种）。
            ReportUnresolved(ctx, "「" + by + "」", unresolved);

            // 用完还原：调用方（`FireTriggerAt` 上面那层）可能还指望原来的值
            // 🆕 2026-10-24（引擎线 `A1` 第 1 件）：**先记「上一条能力的目标」、再还原调用方的槽**
            //   —— 顺序反了就会把调用方那一批当成「这次能力的目标」。
            NotePreviousAbilityTargets(ctx);
            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(savedTargets);
            ctx.LastTarget = savedLast;
            return anyRunnable;
        }

        /// <summary>
        /// **把没结算的那些如实报出来**（`unresolved` 是 `ResolveOne` 一路攒下来的）。
        ///
        /// 🔴 **2026-09-16 收成一处**：这个 `List&lt;string&gt;` 原来有**四处**「建了、传了、
        ///   从来没读」，只有 `PlayTactic` 一处真的读它 ⇒
        ///   **触发层 / 回合起止段 / 突触邻居 / 死亡结转**这四条路上
        ///   「这条效果本版没结算」**一条日志都不会打** —— 正是红线禁止的静默。
        /// ⚠️ 只在**非空**时打（别刷屏）；`label` 写清是哪条路上发生的。
        /// </summary>
        static void ReportUnresolved(BattleContext ctx, string label, List<string> unresolved)
        {
            if (unresolved == null || unresolved.Count == 0) return;
            ctx.Log($"⚠️ {label} 有 {unresolved.Count} 条效果本版没结算："
                  + string.Join("、", unresolved));
        }

        // ==================================================================
        //  🆕 2026-10-19（战斗线 `T21` · 引擎事件缺口 A / E / F）
        //
        //  这一节只有一件事：**在正确的时机发一条 `EvtKind.Trigger`**（表现层按
        //  `BattleEvent.Keyword` = 原版 `AbilityTrigger` 枚举名归口到那张卡自己的
        //  `cardAnimations[]` 行 —— `Core/VfxMap.TryResolveTriggerPicks`）。
        //
        //  ⛔ **不在这里结算任何效果** —— 三族的「游戏效果」各自在原来的地方跑：
        //    · A `followAfterResolution`（320/325）：24 张「有 410 动画行」的卡里，**19 张**的
        //      **第二半效果**本来就由卡面文本解析覆盖（`Reanimate … and give it +1 Health` 这类）；
        //    · E 监听者：`FireWhen → ResolveOps` 原样；
        //    · F 回合起止：`ResolveAtTurn` 原样。
        //  这一节补的是**原版有、我们从来没发过**的那一拍（报告 §三 A/E/F）。
        //
        //  🔴 **三族共用的闸** = `CardDef.HasAnimTrigger(名字)`：卡片自己的逐候选动画里
        //  没有一个标到那个 trigger 名的行 ⇒ **不发**（发了也没东西播，还会在战斗日志里
        //  多印一行、并可能掉进 `trigger` 粗桶乱播 —— 报告 §五·6 那条「多播」）。
        // ==================================================================

        /// <summary>原版 `AbilityTrigger` 枚举名（`dump.cs:20235-20335`）—— 表现层
        /// `VfxMap.IsRoutedTriggerName` 认的就是这一套名字（大小写不敏感）。</summary>
        const string TN_FollowUpResolution  = "FollowUpResolution";   // 410
        const string TN_OtherCardPlayed     = "OtherCardPlayed";      // 10
        const string TN_OtherUnitSummoned   = "OtherUnitSummoned";    // 190
        // 🆕 **2026-10-24（引擎线 `G2` · `C4` D8）：部署那一拍的**自己**那一支。**
        //   判据 = 原版 `CardScript__ResolveUnitSummoned.c` 两支互斥：
        //     `op_Inequality(param_2, param_1)` ⇒ `OtherUnitSummoned(190)`（**别人**被召唤）；
        //     `op_Equality(param_2, param_1)`  ⇒ `ThisUnitSummoned(195)`（**自己**）。
        //   ⚠️ 名字表：`CardDef.cs` 的 `AbilityTrigger` 那张 `{名字, 数值}` 表里 `ThisUnitSummoned = 195`。
        //   候选侧现读**只有 1 张**（`cards_engine.json`：`animTimes[].tn == "ThisUnitSummoned"`
        //   与 `abilityTriggers` 含 195 的都是 **`SW10 Grey Hunter` 1 张 / 1 行**）⇒ 放行它
        //   不会给别的卡凭空发事件（2026-10-24 现读全池）。
        const string TN_ThisUnitSummoned    = "ThisUnitSummoned";     // 195
        const string TN_OtherUnitDeath      = "OtherUnitDeath";       // 90
        const string TN_UnitAttack          = "UnitAttack";           // 50
        const string TN_UnitDamaged         = "UnitDamaged";          // 60
        const string TN_OtherCardDrawn      = "OtherCardDrawn";       // 260
        const string TN_Reanimated          = "Reanimated";           // 570
        const string TN_UnitReceivesMark    = "UnitReceivesMarkOfChaos"; // 510
        // 🆕 **2026-10-24（`A30` · `C4` D7 的第二批）：监听事件那两个原版 `AbilityTrigger` 名。**
        //   判据（第一权威 = 全量反编译，`d:/2/tools/decomp_full/`）：
        //     · `UnitReceivesHuntMark`(520)：`CardScript__ReactToUnitHuntMarked.c:7` 的
        //       `RawCardScript__OnTrigger(..., 0x208, ...)`（`0x208` = **520**）；广播器
        //       `BattleManagerSupport__BroadcastUnitObtainedHuntMark.c`（我们 = `BroadcastKeywordEvent`
        //       的 `p.Keyword == "huntmark"` 那一跳，现读 `EffectResolver.cs:9327`）。
        //     · `OnCollectFaith`(665)：`CardScript__ReactToCollectFaith.c:7` 的
        //       `RawCardScript__OnTrigger(..., 0x299, ...)`（`0x299` = **665**）；广播器
        //       `BattleManagerSupport__BroadcastCollectFaith.c` / `BattleManager__BroadcastCollectedFaith.c`
        //       （我们 = `BroadcastWhen(ctx, WhenEventKind.GainFaith, …)`，现读 `:10001` / `:10043`）。
        //   ⚠️ **它们不是引擎关键词** ⇒ `KeywordTable` 里**没有**、也**不该新造**常量：
        //     `kind` 那一侧是 `WhenEventKind.GetsHuntMark = "gethuntmark"` / `GainFaith = "gainfaith"`
        //     （`WhenEvent.cs:190` / `:107`），返回的这一侧才是原版枚举名。
        const string TN_UnitReceivesHuntMark = "UnitReceivesHuntMark"; // 520
        const string TN_OnCollectFaith       = "OnCollectFaith";       // 665
        // 🆕 **2026-10-24（引擎线 `G1b` · `C2` §2 · D8）：灵族「收集路标石」那一拍（610）。**
        //   判据 = `CardScript__ReactToCollectSpiritStone.c:5-9` 的
        //   `RawCardScript__OnTrigger(thisCard, thisCard, thisCard, 0x262, …)`（`0x262` = **610**）；
        //   广播器 = `BattleManagerSupport__BroadcastCollectSpiritStone.c:22-38`（先对收集者那张卡
        //   `ReactToCollectSpiritStone`，再遍历**全场**在场卡、跳过收集者）。
        //   我们的事件源 = `RuleCore.CollectWaystoneCore` 的 `BroadcastWhen(WhenEventKind.GainSpirit, …)`
        //   （现读全仓**唯一**一处发 `GainSpirit` 的地方 ⇒ 不会因为「任何加灵魂石」误发）。
        //   候选现读（`cards_engine.json`，判据 = `animTimes[].tn == "OnCollectSpiritStone"`）：
        //   **5 行 / 4 张** —— `ASH80 Farseer`（`PathOfTheSeer_Self`）· `ASH28 Spiritseer Qelenaris` ·
        //   `ASH78 Warp Spider Exarch`（2 行）· `ASH25 Warlock Skyrunner`；四张的 `abilityTriggers` 都含 610。
        //   ⚠️ **它不是引擎关键词** ⇒ `KeywordTable` 里**没有**、也**不该新造**常量（同 `TN_OnCollectFaith`）。
        const string TN_OnCollectSpiritStone = "OnCollectSpiritStone"; // 610
        const string TN_TriggeredFerocity   = "TriggeredFerocity";    // 726
        const string TN_TriggeredPray       = "TriggeredPray";        // 660
        const string TN_TriggeredDuty       = "TriggeredDuty";        // 661
        const string TN_TriggeredSwarm      = "TriggeredSwarm";       // 620
        // 🆕 **2026-10-24（执行代理 `J1` · `C5` D12）：突触那一拍的原版 `AbilityTrigger` 名。**
        //   判据（第一权威 = 全量反编译）：`CardScript__ResolveUnitSummoned.c` 同族之外，
        //   突触的广播器 = `BattleManagerSupport__BroadcastUnitSynapse`（我们 = `RepeatTacticOnAdjacent`
        //   尾部那条 `BroadcastKeywordEvent(ctx, WhenEventKind.Triggers(KeywordTable.Synapse), anchor)`，
        //   现读 `EffectResolver.cs:2270`）；`AbilityTrigger.TriggeredSynapse = 630`
        //   （`CardDef.cs` 的枚举表；`dump.cs:20235-20335`）。
        //   候选现读（`cards_engine.json`，判据 = `animTimes[].tn == "TriggeredSynapse"`）
        //   **1 行 / 1 张**：`TL18 Zoanthrope`（`Psychic_Lightning_Tyranid`）。
        //   🔴 改前 `TriggersPrefix` 分支只有 Ferocity / Pray / Duty / Swarm 四条 ⇒ 本方法回 `null`
        //   ⇒ `EmitListenerTrigger` 直接 return ⇒ `EvtKind.Trigger` **从不发** ⇒ `TL18` 的技能动画一条都不播。
        const string TN_TriggeredSynapse    = "TriggeredSynapse";     // 630
        // 🆕 **2026-10-19（第五十七会话 `W57` · T21 C/D）：阵营能力自己那一拍。**
        // 判据 = `AbilityTrigger` 枚举（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs:71-72`）：
        //   `UseSpiritStone = 600` · `UseFaith = 605`。
        // 原版发起源：600 = `RawCardScript__TriggerAbility.c:42-53`（打出时扣石那一段）＋
        //   `CardScript__TriggerSpiritStone.c:16-18`（触发式激活）；605 = 同一条
        //   `RawCardScript__TriggerAbility` 的 605 支 ＋ `CardScript__ResolveUnitSummoned.c:51`。
        const string TN_UseFaith            = "UseFaith";             // 605
        const string TN_UseSpiritStone      = "UseSpiritStone";       // 600

        /// <summary>🆕 **E 族：一条「当……时」事件 → 原版 `AbilityTrigger` 名**（监听者自己的动画行）。
        ///
        /// <para>**判据（逐条，全部来自反编译的 `OnTrigger` 调用点）**：
        /// `RawCardScript__OnCardPlayedWithTarget.c:96`（`TriggerAbilities(10)` = `OtherCardPlayed`）·
        /// `CardScript__TriggeredFerocity.c:7`（`0x2d6` = 726）·
        /// `CardScript__ReactToUnitMarked.c:7`（`0x1fe` = 510 = `UnitReceivesMarkOfChaos`）·
        /// 🆕 `CardScript__ReactToUnitHuntMarked.c:7`（`0x208` = 520 = `UnitReceivesHuntMark`）·
        /// 🆕 `CardScript__ReactToCollectFaith.c:7`（`0x299` = 665 = `OnCollectFaith`）；
        /// 其余几条 = `AbilityTrigger` 枚举名与本工程 `WhenEventKind` 的**字面对应**
        /// （`OtherUnitSummoned` / `OtherUnitDeath` / `OtherCardDrawn` / `TriggeredPray` /
        ///  `TriggeredDuty` / `TriggeredSwarm`）。</para>
        ///
        /// <para>⚠️ **认不出就回 `null`**（不发事件）—— ⛔ 别猜一个名字：认错 = 拿别的 trigger
        /// 的动画行来播（`VfxMap` 的归口是按名字过滤的）。</para>
        ///
        /// <para>🔴🆕 **2026-10-24（引擎线 `G2` · `C4` D8）：`Deploy` 这一拍要按 `SelfOnly` 分流。**
        /// 原版 `CardScript__ResolveUnitSummoned.c` 内部两支**互斥**：
        /// `op_Inequality(param_2, param_1)` ⇒ `OtherUnitSummoned(190)`（**别人**被召唤）·
        /// `op_Equality(param_2, param_1)` ⇒ `ThisUnitSummoned(195)`（**自己**）。
        /// 我们的 `When deployed, …`（省主语）记成 `Deploy + SelfOnly`（`WhenEvent.cs:847-850`），
        /// 正对应 195；`When you deploy a troop, …` 是 190。
        /// ⇒ 本方法的 <paramref name="selfOnly"/> 就是那一条判别式：
        ///   `true`（= 这是**主体自己**那一跳，`FireBoardWhenListener` 的 `selfOnlyOnly`）⇒ 195；
        ///   `false`（棋盘循环 / 手牌 / 常驻效果那一跳）⇒ 190，**行为逐字不变**。
        /// ⚠️ **改前**：190/195 不分 ⇒ `SW10` 的部署监听器**跑了但一条事件都不发**
        /// （`HasTriggerSource` 双闸都过不了：它既没有 190 的动画行、`abilityTriggers` 里也只有 195）。
        /// ⛔ 别把整个 `Deploy` 支改成 195 —— 那会让 `When you deploy a troop, …` 那一整族
        /// （`OtherUnitSummoned` 190）全部静默失声（判据：`FireBoardWhenListener` 的 `selfOnlyOnly` doc）。</para></summary>
        /// <param name="selfOnly">`true` = **被召唤的就是监听者自己**（原版 195 那一支）。
        /// 只有 `FireBoardWhenListener` 的 `selfOnlyOnly == true`（= 两条部署入口的预调）传。
        /// 缺省 `false` = 改前行为（190）。</param>
        static string TriggerNameOfWhenKind(string kind, bool selfOnly = false)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            switch (kind)
            {
                case WhenEventKind.Play:          return TN_OtherCardPlayed;
                case WhenEventKind.Deploy:        return selfOnly ? TN_ThisUnitSummoned : TN_OtherUnitSummoned;
                case WhenEventKind.Die:           return TN_OtherUnitDeath;
                case WhenEventKind.Attack:        return TN_UnitAttack;
                case WhenEventKind.Damaged:       return TN_UnitDamaged;
                case WhenEventKind.Draw:          return TN_OtherCardDrawn;
                case WhenEventKind.Reanimated:    return TN_Reanimated;
                case WhenEventKind.GetsDarkPact:  return TN_UnitReceivesMark;
                // 🆕 **2026-10-24（`A30` · `C4` D7）：两条「授予类」监听事件补上映射。**
                //   改前这两条**没有分支** ⇒ `EmitListenerTrigger` 在 `tn == null` 时直接 return
                //   ⇒ `EvtKind.Trigger` **从不发**（表现层那一拍永远等不到；判据见常量区那一段）。
                //   安全边界（2026-10-24 现读全池）：`abilityTriggers` 含 **520** 的只有 `SW20` / `SW35`，
                //   含 **665** 的只有 `SOR38` ⇒ 补这两条**不会**给别的卡凭空发事件。
                case WhenEventKind.GetsHuntMark:  return TN_UnitReceivesHuntMark;
                case WhenEventKind.GainFaith:     return TN_OnCollectFaith;
                // 🆕 **2026-10-24（引擎线 `G1b` · `C2` §2 · D8）**：`When you collect a Spirit Stone, …`
                //   —— 改前**没有这条分支** ⇒ `EmitListenerTrigger` 在 `tn == null` 时直接 return
                //   ⇒ `EvtKind.Trigger` **从不发** ⇒ `ASH80/28/78/25` 的 610 动画一条都不播。
                //   判据与安全边界 → `TN_OnCollectSpiritStone` 的常量注释。
                case WhenEventKind.GainSpirit:    return TN_OnCollectSpiritStone;
                case WhenEventKind.Prays:         return TN_TriggeredPray;
                // `When you trigger a Spirit Stone ability, …`（`ASH81 Bright Lance Vyper`）——
                //   原版那条监听能力的 trigger 名 = `OtherCardSpiritStone`（455，`dump.cs` 枚举表）。
                case WhenEventKind.SpiritAbility: return "OtherCardSpiritStone";
                // 🔴🆕 **2026-10-25（执行代理 `E6` · `E5-5` 那 13 行）：补两条缺失的映射。**
                //   改前这两条**没有分支** ⇒ `EmitListenerTrigger` 在 `tn == null` 时直接 return
                //   ⇒ `EvtKind.Trigger` **从不发**（表现层那一拍永远等不到），
                //   而且 ability 级两道闸也没有 trigger 名可用（`AbilityLevelGateTrigger` 回 `null`）。
                //   判据（第一权威，逐句）：
                //     · `CardScript__ReactToUnitMarkerlight.c:7` → `OnTrigger(0x203 = 515)`；
                //       广播器 `BattleManagerSupport__BroadcastUnitObtainedMarkerlight.c:20`；
                //     · `CardScript__ReactToUnitStunned.c:7` → `OnTrigger(0x123 = 291)`；
                //       广播器 `BattleManagerSupport__BroadcastUnitStunned.c`（`(被晕者, 施害者)`）。
                //   安全边界（现读 v28 池）：`abilityTriggers` 含 **515** 的只有 `TAU15`、
                //   含 **291** 的只有 `ASH75` / `ASH74`（`ASH74` 另带 46）⇒ 补这两条**不会**给
                //   别的卡凭空发事件；`HasTriggerSource` 的 presence 闸照旧先判（`presence == 0` ⇒ 不发）。
                case WhenEventKind.GetsMarkerlight: return "UnitReceivesMarkerlight"; // 515
                case WhenEventKind.GetsStun:        return "UnitStunned";             // 291
                // 🆕 **2026-10-26（完全复刻批次 3 · `E2`）：回手 / 回牌库 两族。**
                //   判据 = `RawCardScript__OnCardReturnedHand.c:7`（`0x1d6` = **470**）与
                //   `…Deck.c:7`（`0x1e0` = **480**）；`CardDef.cs` 的 `AbilityTriggerNumberOfName`
                //   里两条名字本来就有（`{"CardReturnedToHand", 470}` / `{"CardReturnedToDeck", 480}`）。
                //   ⚠️ **今天零可观测**：卡池 0 张卡面写这类监听、`abilityTriggers` 含 470/480 的
                //   也是 0 张（现读 1,128 张）⇒ `HasTriggerSource` 的 presence 闸照旧先判。
                //   两条照铁律 11 补全（休眠机制也要做）。
                case WhenEventKind.CardReturnedHand: return "CardReturnedToHand";     // 470
                case WhenEventKind.CardReturnedDeck: return "CardReturnedToDeck";     // 480
                // 🆕 **2026-10-26（完全复刻批次 4 · `F2`）：治疗 / 骷髅 / 安魂 三族。**
                //   判据（第一权威，逐行现读 `d:/2/tools/decomp_full/`）：
                //     · `CardScript__ReactToUnitHealed.c:7`        → `OnTrigger(0x46  = 70)`；
                //     · `CardScript__TriggeredEarnSkull.c:7`       → `OnTrigger(0x280 = 640)`；
                //     · `CardScript__TriggerOtherCardRequiem.c:14` → `OnTrigger(0x1b3 = 435)`。
                //   三个名字在 `CardDef.cs:1918-1940` 的 `AbilityTriggerNumericNames` 里本来就有
                //   （`{"UnitHealed", 70}` / `{"EarnedSkull", 640}` / `{"OtherCardRequiem", 435}`）。
                //   📊 **今天零可观测**（现读 1,128 张）：卡面写上述三种监听的 **0 张**、
                //   `abilityTriggers` 含 70 / 640 / 435 的也是 **0 张**
                //   ⇒ `EmitListenerTrigger` 那边的 `HasTriggerSource` presence 闸照旧先判，
                //   补这三条**不会**给任何现有卡凭空发事件（铁律 11：休眠机制也要做）。
                case WhenEventKind.UnitHealed:       return "UnitHealed";       // 70
                case WhenEventKind.EarnedSkull:      return "EarnedSkull";      // 640
                case WhenEventKind.OtherCardRequiem: return "OtherCardRequiem"; // 435
                // 🆕 2026-10-26（批次 6 · `G14` ③）：任务点 / 隐秘 / 破坏 三族补映射（判据 = `.c` 现读：`ReactToCollectQuestPoints.c:7`→`0x2b7=695` · `ReactToSecretCreated.c:7`→`700` · `ReactToSabotageCreated.c:7`→`0x2bd=701`；名字表 `CardDef.cs:1944`）。**改前没有这三支 ⇒ 效果照跑、`EvtKind.Trigger` 一条不发 ⇒ 动画静默不播**（可达 3+2+1 张）。
                case WhenEventKind.GainQuest:        return "OnCollectQuestPoints"; // 695
                case WhenEventKind.CreatesSecret:    return "OnSecretCreated";      // 700
                case WhenEventKind.CreatesSabotage:  return "OnSabotageCreated";    // 701
            }
            const string prefix = WhenEventKind.TriggersPrefix;
            if (kind.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                string kw = kind.Substring(prefix.Length);
                switch (kw)
                {
                    case KeywordTable.Ferocity: return TN_TriggeredFerocity;
                    case KeywordTable.Pray:     return TN_TriggeredPray;
                    case KeywordTable.Duty:     return TN_TriggeredDuty;
                    case KeywordTable.Swarm:    return TN_TriggeredSwarm;
                    // 🆕 2026-10-24（执行代理 `J1` · `C5` D12）：突触（`synapse`）那一拍。
                    //   判据 / 候选规模 → `TN_TriggeredSynapse` 的常量注释。
                    //   ⚠️ 安全边界（现读全池）：`abilityTriggers` 含 630 的只有 `TL18`/`TL19`/`TL70`，
                    //   其中**只有 `TL18` 有这条动画行**；另外两张会走 `HasTriggerSource` 的
                    //   「有 ability、没动画行」那支（照发 + 出声，表现层无件）—— 与既有 A/E/F 族同口径。
                    case KeywordTable.Synapse:  return TN_TriggeredSynapse;
                    // 🆕 **2026-10-25（完全复刻 · 执行代理 A · 第 2 轮 · 账 2c）：补 Mob / Codex / Regiment / Requiem 四条。**
                    //   判据（第一权威，逐条现读 —— 注意原版这一族**命名不统一**，「别的卡」用的 id 有两族）：
                    //     · `CardScript__TriggerOtherCardMob.c:14` → `OnTrigger(0x285 = **645** = `AbilityTrigger.OtherCardMob`)`；
                    //     · `CardScript__TriggerOtherCardCodex.c` → `OnTrigger(0x28a = **650** = `TriggeredCodex`)`
                    //       （Codex 这一族「别的卡」用的仍是 `Triggered<X>` 那个 id）；
                    //     · `CardScript__TriggerOtherCardRegiment.c:14` → `OnTrigger(**646** = `OtherCardRegiment`)`；
                    //     · `CardScript__TriggerOtherCardRequiem.c:14` → `OnTrigger(**435** = `OtherCardRequiem`)`
                    //       （`Requiem` 自己那一档是 **430**）。
                    //   触发 id 全表 → `CardDef.cs:1889-1902`（`{"OtherCardMob", 645}` / `{"OtherCardRegiment", 646}` /
                    //   `{"TriggeredCodex", 650}` / `{"Requiem", 430}` / `{"OtherCardRequiem", 435}`，现读）。
                    //   ⚠️ **机制那一半早就在**（`FireTriggerAlways` → `BroadcastWhen(Triggers(kw))` 覆盖双方棋盘 + 双方手牌，
                    //   见 `RuleCore.cs:9110-9180` 那一整段）；这里缺的只是**表现/ability 闸要的那个 trigger 名字** ——
                    //   缺它 ⇒ `EmitListenerTrigger` 在 `tn == null` 时直接 return ⇒ `EvtKind.Trigger` 从不发。
                    //   📊 **可达性（现读卡池）**：「When a friendly unit triggers **Mob**, it triggers an additional time」
                    //   = `GOF68`（**1 张**，真可达）；Codex / Regiment / Requiem 的「triggers …」句 **0 张**
                    //   ⇒ 这三条是「照原版补全、今天不响」（铁律 11：休眠机制也做）。
                    case KeywordTable.Mob:      return "OtherCardMob";        // 645
                    case KeywordTable.Codex:    return "TriggeredCodex";      // 650
                    case KeywordTable.Regiment: return "OtherCardRegiment";   // 646
                    case "requiem":             return "OtherCardRequiem";    // 435（我们**没有**该关键词常量：池内 0 张）
                    // 🆕 **2026-10-26（完全复刻批次 3 · `E2`）：`sacrifice` 那一拍补上 trigger 名。**
                    //   判据 = `CardScript__ReactToUnitUsedSacrifice.c:7`：
                    //   `RawCardScript__OnTrigger(raw, 0xe1, mgr, thisCard, param_2, param_2, 0)`
                    //   —— `0xe1 = **225** = `AbilityTrigger.OtherCardSacrifice`（`CardDef.cs` 名字表现读）。
                    //   ⚠️ **不是 220**：220(`Sacrifice`) 是**用掉献祭的那张自己**的能力
                    //   （`FireTriggerAt(Sacrifice)`），225 才是「**别的**卡用了献祭」这一拍
                    //   —— 两者在原版分两个 `OnTrigger`，别合并。
                    //   自排除：`sacrifice` **不在** `KeywordTriggerIncludesSelf` 名单里
                    //   ⇒ `WhenEvents.SelfExcludesOnKeywordTrigger` 默认自排除，与原版
                    //   `BroadcastUnitUsedSacrifice` 那 3 条 `op_Inequality` 一致（`WhenEvent.cs` 的判据表）。
                    //   📊 可达性（现读 1,128 张）：卡面写「uses Sacrifice」的监听 **0 张**；
                    //   `abilityTriggers` 含 225 的也是 **0 张** ⇒ 今天这条映射只补口径、零行为变化。
                    case KeywordTable.Sacrifice: return "OtherCardSacrifice";  // 225
                    // 🆕 **2026-10-26（批次 6 · `G14` ③）：`triggers:landing`（空投舱落地）补映射**（判据 = `CardScript__TriggerOtherCardLanding.c:19` → `OnTrigger(0x1c2=450)`，`op_Inequality(param_2, param_1)` = 自排除；名字表 `CardDef.cs:1933`）。**改前没有这一支 ⇒ 动画静默不播**；池内「卡面写 triggers landing」0 张 ⇒ 今天休眠。
                    case KeywordTable.Landing:  return "OtherCardLanding";     // 450

                }
            }
            return WhenEvents.ExtraTriggerNameForKind(kind);   // 🆕 2026-10-26（批次 8 · `B8_C`）：`kills`→`Slay`(120) · `losestealth`→`UnitLoseStealth`(647)；其余（`getsshield`/`becomesremnant`/`unitjammed`）**原版也没有** AbilityTrigger 号 ⇒ 仍返 null。判据全在 `WhenEvents.ExtraTriggerNameForKind` 的注释里（另见 `WhenEvents.KindsAbsentInOriginal`）。
        }

        // ==========================================================================================
        //  🔴🆕 **2026-10-25（执行代理 `E2` · `A3-2`）：ability 级 `targetCriteria` 闸的
        //  「trigger 名 / `targetCard`」两个实参 —— 只对**已经查清映射**的事件返回非 null。**
        // ==========================================================================================
        //  原版这一跳（`CardAbility__CanTriggerAbility.c:131-135`）的候选是 **`targetCard`**，
        //  而我们的 `BroadcastWhen` 把事件宾语拆成 `subject` / `target` 两格 —— 两者**不是一回事**，
        //  逐族查过（第一权威 = 全量反编译）：
        //    · **`Attack`**：`CardScript__ResolveUnitAttacked.c:28` `OnTrigger(0x32, bm, thisCard, param_2,
        //      param_3)`；广播器 `BattleManagerSupport__BroadcastUnitAttacked.c:22/:29` 两跳的
        //      `param_3` 都是**被打者** ⇒ 我们的 `target`（`RuleCore.cs:6300` 传的就是它）。
        //    · **`Die`**：`CardScript__TriggerOnMinionDeath.c:97-99` → `OnMinionDeath(…, thisCard,
        //      actingCard=deadCard, targetCard=deadCard)`；我们的 `BroadcastWhen(Die, …, subject: u)`
        //      **没有 `target`** ⇒ 候选取 `subject`（= 刚死的那个）。
        //    · 🔴🆕 **2026-10-25（执行代理 `E6` · `E5-5` 那 13 行）再补五族**（逐句判据见下）：
        //      `GetsDarkPact(510)` / `Reanimated(570)` / `GetsMarkerlight(515)` / `Damaged(60)` /
        //      `GetsStun(291)`。
        //      ⚠️ **`Damaged` 的旧注释被证伪、已就地改掉**（铁律 5）：原来这里写
        //      「`CardScript__ResolveDamageDealt.c:98` 的 `targetCard` 是 `param_2`（**伤害来源**）」——
        //      **不成立**。`OnTrigger` 的形参表是
        //      `(trigger, manager, thisCard, **actingCard**, **targetCard**, …)`（`dump.cs:23487`），
        //      而 `ResolveDamageDealt` 那一跳（`:158-161` / `:167-170` / `:182-184`）传的是
        //      `OnTrigger(0x3c, …, thisCard=param_1, actingCard=*(DamageInfo+0x10), targetCard=param_2)`
        //      ⇒ **`targetCard` = `param_2` = 被打者**（广播器 `BattleManagerSupport__BroadcastUnitDamaged.c:19`
        //      的两跳实参 = `(被打者, 被打者, DamageInfo)`）⇒ 就是我们的 `subject`。
        //      缺的是 **`actingCard`（伤害来源）**—— 那一道闸本批**不启用**（见 `AbilityActingGate*`）。
        //      （同一条错句 `E5` 已在 `E2` 报告里订正，但**代码里这份漏了** ⇒ 本批一起改。）
        //    · **`GetsDarkPact(510)`**：`CardScript__ReactToUnitMarked.c:7`
        //      `OnTrigger(0x1fe, …, param_1 /*thisCard*/, **param_2** /*actingCard*/, **param_2** /*targetCard*/)`
        //      且广播器 `BattleManagerSupport__BroadcastUnitObtainedMark.c:20` 是 `(param_2, param_2)`
        //      ⇒ **acting = target = 被标记者 = 我们的 `subject`**。
        //    · **`Reanimated(570)`**：`CardScript__ReactToUnitReanimated.c:7` 同一形状
        //      （`OnTrigger(0x23a, …, param_2, param_2)`）⇒ 两者都 = `subject`（被复活者）。
        //    · **`GetsMarkerlight(515)`**：`CardScript__ReactToUnitMarkerlight.c:7`
        //      （`OnTrigger(0x203, …, param_2, param_2)`）+ 广播器 `…ObtainedMarkerlight.c:20`
        //      `(param_2, param_2)` ⇒ 两者都 = `subject`（被标记者）。
        //    · **`GetsStun(291)`**：`CardScript__ReactToUnitStunned.c:7`
        //      `OnTrigger(0x123, …, thisCard=param_1, **actingCard=param_3**, **targetCard=param_2**)`
        //      ⇒ 🔴 **两格不是同一个形参**：`targetCard` = 被晕者（= 我们的 `subject`）、
        //      `actingCard` = 施害者（`BroadcastUnitStunned(manager, 被晕者, 施害者)`，我们那两跳
        //      **只带了被晕者**）⇒ **target 闸能接、acting 闸接不了**（见 `AbilityActingGate*`）。
        //    · ⛔ **其余事件仍一律不启用**（返回 `null`）——它们的 `targetCard` 是哪一格没查清。
        //  ⚠️ 覆盖面（现读 v28 池）：`abilityLevelCriteria` 85 行 =
        //    **已接 35 行**（`Attack` 6 + `Die` 16 + `510` 4 + `570` 5 + `515` 1 + `60` 1 + `291` 2）
        //    ＋ **未接 50 行**（`ThisCardPlayed` 40 · `FollowUpResolution` 5 · `Strike` 2 ·
        //    `AboutToAttack` 1 · `TriggeredSwarm` 1 · `OnSabotageCreated` 1 —— 都在**别的入队点 / 专路**）。
        //    评估表 → `资料/普查产出_1025/E5_引擎剩余小缺口.md` §五；本批报告 →
        //    `资料/普查产出_1025/E6_十三行接闸.md`。

        /// <summary>🆕 2026-10-25（`E6`）：本族**要不要跑** ability 级 `targetCriteria`（`+0x30`）那道闸 ——
        /// 只对**已经查清 `targetCard` 是哪一格**的事件返回 `true`（⛔ 不是「有事件就判」）。
        /// <para>逐族判据见上面那段长注释；一句话：`Attack` → `target`，其余六族 → `subject`。</para></summary>
        static bool AbilityLevelGateApplies(string kind)
        {
            switch (kind)
            {
                case WhenEventKind.Attack:
                case WhenEventKind.Die:
                case WhenEventKind.GetsDarkPact:      // 510（`ReactToUnitMarked.c:7`）
                case WhenEventKind.Reanimated:        // 570（`ReactToUnitReanimated.c:7`）
                case WhenEventKind.GetsMarkerlight:   // 515（`ReactToUnitMarkerlight.c:7`）
                case WhenEventKind.Damaged:           // 60（`ResolveDamageDealt` 0x3c：targetCard = 被打者）
                case WhenEventKind.GetsStun:          // 291（`ReactToUnitStunned.c:7`：targetCard = 被晕者）
                    return true;
            }
            return false;
        }

        /// <summary>见上面那段：不适用 ⇒ `null`（不启用闸）；适用 ⇒ 该族 trigger 名。</summary>
        static string AbilityLevelGateTrigger(string kind, bool selfOnly)
        {
            if (!AbilityLevelGateApplies(kind)) return null;
            return TriggerNameOfWhenKind(kind, selfOnly);
        }

        /// <summary>见上面那段：`Attack` 取 `target`（被打者）、其余六族取 `subject`；不适用 ⇒ `null`
        /// （`null` = 本路径没有 `targetCard` ⇒ 那道闸判不了 ⇒ 出声并放行）。</summary>
        static UnitState AbilityLevelGateTarget(string kind, UnitState subject, UnitState target)
        {
            if (kind == WhenEventKind.Attack) return target;
            return AbilityLevelGateApplies(kind) ? subject : null;
        }

        // ==========================================================================================
        //  🔴🆕 **2026-10-25（执行代理 `E3` · `E2` 报告 §六）：ability 级 `actingCardCriteria`
        //  （`+0x28`，**第一道闸**）的「这一族要不要跑 / `actingCard` 是哪一格」。**
        // ==========================================================================================
        //  判据（第一权威 = 全量反编译）：
        //    · **`Attack`**：`CardScript__ResolveUnitAttacked.c:28` 的
        //      `OnTrigger(0x32, bm, thisCard, param_2, param_3)` = (thisCard, **actingCard**, targetCard)；
        //      广播器 `BattleManagerSupport__BroadcastUnitAttacked.c:22/:29` 两跳传的
        //      `param_2` 都是**攻击者**（`_ResolveAttack_d__438:988` 实参 = `attacker, target`）
        //      ⇒ 我们的 `subject`（`RuleCore.cs:6300` 的 `BroadcastWhen(Attack, …, subject: attacker)`）。
        //    · **`Die`**：`RawCardScript.OnMinionDeath(manager, thisCard, **actingCard**, **deadCard**, …)`
        //      （`d:/2/tools/il2cpp_out/dump.cs:23526`）—— 🔴 **`actingCard` 与 `deadCard` 是两个形参**；
        //      `CardScript__TriggerOnMinionDeath` 把 `thisCard=观察者`、`actingCard`（来自
        //      `ResolveDeadCard` 的 `BattleAction.actingCard`）、`deadCard` 分别传下去。
        //      而我们的 `OtherUnitDeath(90)` 那一跳（`RuleCore.CleanupDeaths` 的 `BroadcastWhen(Die, …)`）
        //      **只有死者那一格**（`subject`），凶手在 **`CleanupDeaths` 的 `actingCard` 形参**里
        //      （= `DamageInfo.actingCard` 一路带下来的来源卡）⇒ 本函数的 `actingCard` 形参就是它。
        //      ⚠️ **拿不到（毒 / 反噬 / 反击 / 不稳定 / 回合开始清场那些没有来源的死）
        //      ⇒ 返回 `null` ⇒ 那道闸判不了 ⇒ 出声并跳过**（⛔ 别拿 `subject`（死者）顶 —— 那是筛错人）。
        //    · ⛔ 其余事件一律**不启用**（返回 `null`）：它们的 `actingCard` 是哪一格**没查清**。
        //  ⚠️ 覆盖面（现读 v27 池 · `abilityActingCriteria` 落 83 行 / 80 张）：
        //    `trigger 50 UnitAttack` **14 行**（全是单位，判据已闭合）·
        //    `trigger 90 OtherUnitDeath` **12 行**（其中 11 行是**战术卡** ⇒ 不是棋盘监听者，
        //    这一跳根本到不了；1 行是 `SOR10`，它的卡面是 `When this unit kills an enemy` =
        //    我们的 `Kills` 事件，也不走这一跳）· `trigger 80 ThisUnitDeath` 1 行（我们的 `Die`
        //    广播恒映射成 90，到不了）⇒ **今天真正会被这道闸筛到的 = `Attack` 那 14 行**。
        //    全文 → `资料/普查产出_1025/E3_actingCardCriteria.md` §三。
        //  🔴🆕 **2026-10-25（执行代理 `E6`）：再启用三族** —— `GetsDarkPact(510)` / `Reanimated(570)` /
        //    `GetsMarkerlight(515)`。判据（第一权威，逐句）= 那三个接收函数的 `OnTrigger` 实参：
        //      `ReactToUnitMarked.c:7` / `ReactToUnitReanimated.c:7` / `ReactToUnitMarkerlight.c:7`
        //      都是 `(…, thisCard=观察者, **actingCard=param_2**, **targetCard=param_2**)`，而广播器
        //      （`…ObtainedMark.c:20` / `…Reanimated.c` / `…ObtainedMarkerlight.c:20`）传的是
        //      `(主体, 主体)` ⇒ **actingCard = 被标记 / 被复活 / 被标记的那个单位 = 我们的 `subject`**。
        //    ⚠️ 真正会**被筛到**的只有 `510` 那 **2 行**（`BL63 Havoc` / `BL72 Accursed Helbrute`，
        //      `abilityActingCriteria` 现读 v28）；`570` / `515` 在 acting 表里 **0 行** ⇒
        //      启用后走「明确没有这一格条件」那条（`why == null` ⇒ 放行、不出声）。
        //    ⛔ **`Damaged(60)` / `GetsStun(291)` 不启用**：那两族的 `actingCard` 是**伤害来源 / 施害者**，
        //      我们这两跳**广播里根本没带**（`EmitHit` 没有来源形参、`BroadcastKeywordEvent` 只有被晕者）
        //      ⇒ 硬接就是「拿 subject 顶」= 筛错人。判据已查清、**缺的是发射口那一格**（记账，⛔ 不是不做）。

        /// <summary>见上面那段：`Attack` / `Die` / `GetsDarkPact` / `Reanimated` / `GetsMarkerlight`
        /// 五族**启用**第一道闸（其余 = 不判；`Damaged` / `GetsStun` 缺 `actingCard` 那一格）。</summary>
        static bool AbilityActingGateApplies(string kind)
        {
            return kind == WhenEventKind.Attack || kind == WhenEventKind.Die
                || kind == WhenEventKind.GetsDarkPact     // 510
                || kind == WhenEventKind.Reanimated       // 570
                || kind == WhenEventKind.GetsMarkerlight; // 515
        }

        /// <summary>见上面那段：`Attack` / `GetsDarkPact` / `Reanimated` / `GetsMarkerlight` 取 `subject`、
        /// `Die` 取 `actingCard`（凶手）；其余 / 拿不到 ⇒ `null`（那道闸判不了 ⇒ 出声并跳过）。</summary>
        static UnitState AbilityActingGateCard(string kind, UnitState subject, UnitState actingCard)
        {
            // 510 / 570 / 515：`OnTrigger(…, actingCard=param_2, targetCard=param_2)` ⇒ 两者都是被标记 /
            // 被复活 / 被标记的那个单位（= 我们的 `subject`）。判据见上面那段长注释。
            if (kind == WhenEventKind.Attack) return subject;
            if (kind == WhenEventKind.Die) return actingCard;
            if (kind == WhenEventKind.GetsDarkPact
                || kind == WhenEventKind.Reanimated
                || kind == WhenEventKind.GetsMarkerlight) return subject;
            return null;
        }

        /// <summary>🆕 **A 族：这张卡的 `followAfterResolution`（`AbilityEffect` 320 / 325）挂在哪一条能力里**
        /// —— 回原版 `AbilityTrigger` 枚举名；这张卡没有那个效果（或有但调度者判不出来）⇒ `null`。
        ///
        /// <para>**判据 = 当前 build 的 SO 真值**：`cardAbilities[].abilityLogic[].chosenAbility == 320/325`
        /// （`资料/普查产出_1018/allcards_1350/cards_raw.json`，逐卡核过；文件名与池 id 的对应
        /// 走 `cards_engine.json` 的 `id`）。🔴 **2026-10-25 就已订正（此前这里是「19 张」）**：
        /// 口径 = 池内（1127 张）「`chosenAbility ∈ {320,325}`」= **25 张**，表里 **25 条**全覆盖
        /// （17 张战术/防御 = `ThisCardPlayed`，8 张单位 = 各自的关键词/监听器）。
        /// 另有 **5 张在表外，且原版本身不触发**（`DA29` / `DA14` / `SW9` / `UM118` / `UM116`
        /// —— 它们的 410 能力在当前 build 的 `abilityLogic` 里**找不到任何 320/325**）⇒ 不收。
        /// ⚠️ `SW9` 那 **2 行**（`abilityTargets` 里 410 行）同样按「原版本身不触发」不收
        /// （判据 = 10-25 判据文档 §〇·3 / `E9` §四）。</para>
        ///
        /// <para>⚠️ **为什么是硬编码表**：引擎加载的 `cards_engine.json` **只有 `animTimes`**
        /// （`tg`/`tn`），没有 `abilityLogic` 的 `chosenAbility`；`cards_truth.json` 有，但它是
        /// `Resources` 资产、由 `RuleEngine/Data` 装载（不在本批白名单）。
        /// ⇒ 本表是**当前 build 的转写**，卡池再迁 id / 改能力时**必须重生成**（报告 §四 已记）。</para></summary>
        // 🔴🆕 2026-10-25（执行代理 `X3`）：可见性 `internal` → `public` —— 唯一目的 = **让自检能断言这张表**
        //   （编辑器程序集与运行时不共用 `internal`，本仓没有 `InternalsVisibleTo`；`E9` §四 也记着这一点）。
        //   它是一个**纯读**的硬编码查询，不改任何行为。
        public static string FollowUpSchedulerOf(CardDef card)
        {
            if (card == null) return null;
            switch (card.Id)
            {
                // ---- 战术 / 防御：320 挂在 trigger 0（= `ThisCardPlayed`）的 ability 里 ----
                case "ASH73": case "AM54": case "AM58": case "EC47": case "EC49":
                case "SAU61": case "GOF104": case "GOF102":
                case "EC40": case "EC42": case "GOF31": case "GOF93":      // 🆕 10-25：漏的 4 张（trigger 0）
                case "3ebb159036778d14ebd1c48451aa6ca0":   // Greatest Warboss
                case "SW56": case "TAU40": case "UM39":
                    return "ThisCardPlayed";

                // ---- 单位：320 挂在各自那条能力里（trigger 号见每条注释）----
                case "ASH81": return "OtherCardSpiritStone";   // 455 · When you trigger a Spirit Stone ability
                case "AM8":   case "AM21": return "Regiment";  // 301
                case "GSC14": return TN_OtherCardPlayed;       // 10 · When your opponent plays a Stratagem
                case "GSC17": return "Strike";                 // 580
                case "SAU11": return "Artifice";               // 560
                case "GOF59": return "Mob";                    // 300
                case "TL3":   return "Strike";                 // 580  🆕 10-25：漏
                case "UM64":  return "Codex";                  // 145  🆕 10-25：漏
                default: return null;
            }
        }

        /// <summary>
        /// 🆕 **效果 `showCardsWindow`(470) —— 「这张卡打出后要不要摊开一批卡给玩家看」，以及那扇窗的标题键**
        /// （`null` = 这张卡**没有** 470 那一条 `abilityLogic`）。
        ///
        /// <para>**判据 = 当前 build 的 SO 真值**（第一权威 = 全量反编译 + `cards_raw.json`）：
        /// `cardAbilities[].abilityLogic[].chosenAbility == 470`（`AbilityEffect.cs:83` `showCardsWindow = 470`）。
        /// 现读 `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张 SO）—— **全池只有 1 张**有它：
        /// `GSC55 Eve of the Uprising`，而且那一条的 `buffEffect.effectText = "EffectDisplayCardsAddToDeck"`
        /// ⇒ 标题键 = `"Card_Abilities/" + effectText`（官方本地化现读
        /// `Card_Abilities/EffectDisplayCardsAddToDeck` = `These cards have returned to your deck` /
        /// `这些张牌已返回到你的卡组`，命中）。</para>
        ///
        /// <para>**原版那一整条链**（逐段读过，图见 `EvtKind.ShowCardsWindow` 的 doc）：
        /// `AbilityLogic__PlayAbility.c` case `0x1d6` → `AddShowCardsWindowAction` → `AddAutoActionToQueue`
        /// → `ResolveAction` case `0x56`（`IsPlayerTurn` 才开窗）→ `BattleHud.DisplayMultiCardView`。</para>
        ///
        /// <para>⚠️ **为什么是硬编码表**（与 `FollowUpSchedulerOf` **同一个理由**）：
        /// 引擎加载的 `cards_engine.json` **没有 `abilityLogic` / `chosenAbility` 这一列**
        /// （只有 `animTimes` / `abilityTriggers` / `abilityTargets` / `abilityMultipliers`），
        /// 而生成器 `工具/gen_cards_engine.py` 不在本批白名单 ⇒ 这一格**只能先按当前 build 转写**。
        /// 🔴 卡池改 id / 改能力时**必须重生成这张表**；`TestW62EntryPump` 的
        /// `TestW64ShowCardsWindow` 的 ① 组钉着「全池恰好 1 张 = `GSC55`」+ 那一条标题键
        /// ⇒ 重生成丢列 / 换 id ⇒ 那边直接红（⛔ 不会静默失效）。</para>
        /// </summary>
        public static string ShowCardsWindowOf(CardDef card)
        {
            if (card == null) return null;
            switch (card.Id)
            {
                // `cards_raw.json` 现读：`cardAbilities[0].abilityLogic[2]` 的
                // `chosenAbility = 470` + 同一格 `buffEffect.effectText = "EffectDisplayCardsAddToDeck"`。
                case "GSC55": return "EffectDisplayCardsAddToDeck";
                default: return null;
            }
        }

        /// <summary>
        /// 效果 470 的**发射口** —— 判据 / 「为什么是硬编码表」全在 `ShowCardsWindowOf` 与
        /// `EvtKind.ShowCardsWindow` 的 doc 里，这里只解决**摊开哪几张**。
        ///
        /// <para>**「摊开哪几张」的判据**：原版那一条 `abilityLogic` 的
        /// `targetCriteria.targetsAffected = previousAbilityFiltered(150)`
        /// （`TargetsAffected.cs:26`）= **「被上一条能力筛出来的那批」**。
        /// 我们对应的槽 = `ctx.LastCreated`（`DoReturn` 末尾按**实例**填的那批 `moved`；
        /// `DoLowerCost` 只读不写 ⇒ 战术正文那一串 op 跑完它还在）。</para>
        ///
        /// <para>⚠️ **已知偏离（如实标，⛔ 别当 bug 改回去）**：原版即使目标为空也会把那条动作
        /// **入队**、开一扇**空窗**；我们**不发** 0 张的窗（`MultiCardDisplay` 要 ≥1 张才有东西可摊，
        /// 空窗对玩家没有任何可观察内容），**但要出声**（`ctx.Log` —— 红线：不许静默）。
        /// 现池唯一持有者 `GSC55` 在「本局没有部队阵亡」时正是这一档。</para>
        /// </summary>
        public static void EmitShowCardsWindow(BattleContext ctx, int owner, CardDef card)
        {
            if (ctx == null || card == null) return;
            string title = ShowCardsWindowOf(card);
            if (title == null) return;      // 这张卡没有 470 ⇒ 什么都不做（全池 1,126/1,127 走这一支）

            var names = new List<string>();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var c in ctx.LastCreated)
                if (c != null && c.Card != null && seen.Add(c.Card.Name)) names.Add(c.Card.Name);

            if (names.Count == 0)
            {
                ctx.Log($"「{card.Name}」的多卡展示窗（效果 470）**没有可摊开的卡** ⇒ 不开窗"
                      + "（原版会开一扇空窗；本偏离见 EmitShowCardsWindow 的 doc）");
                return;
            }

            string key = "Card_Abilities/" + title;
            ctx.Emit(new BattleEvent
            {
                Kind = EvtKind.ShowCardsWindow,
                Player = owner,
                Slot = -1,
                CardId = card.Name,
                Effect = key,
                CardNames = names,
            });
            ctx.Log($"「{card.Name}」摊开 {names.Count} 张卡：「{string.Join("、", names)}」"
                  + $"（效果 470 · 标题键 `{key}`）");
        }

        /// <summary>结算层的一个关键词 → 原版 `AbilityTrigger` 名（只列 `FollowUpSchedulerOf` 用到的四个）。</summary>
        internal static string AbilityTriggerNameOfKeyword(string keyword)
        {
            // 🔴 **2026-10-23（`W63` 收口）：本方法原来只覆盖 4 个词（只服务
            //   `FollowUpSchedulerOf`）。`abilityContext` 要按原版 trigger 名查真值
            //   ⇒ 扩成全量 21 条，正本据已收进 `RuleCore.TriggerNameOfKeyword`
            //   （`Core/ActionPump.cs`，与生成器 `ABILITY_TRIGGER_KEYWORD` 逐字对齐）。
            //   本方法**只转发**，⛔ 别在这里另写一张表。
            return RuleCore.TriggerNameOfKeyword(keyword);
        }

        /// <summary>🆕 **A 族发射口**：给一张卡发 `FollowUpResolution`（410）事件。
        /// 闸 = <see cref="HasTriggerSource"/>（动画行 **或** 真值里有这条 ability）。
        /// `slot` = 动画落点（棋盘格号；`-1` = 不在场上）。</summary>
        internal static void EmitFollowUpResolution(BattleContext ctx, CardDef card, int player, int slot, int instanceId)
        {
            if (ctx == null || card == null) return;
            if (!HasTriggerSource(card, TN_FollowUpResolution, ctx, "followAfterResolution(320)")) return;
            ctx.Emit(EvtKind.Trigger, player, slot, card.Name,
                     keyword: TN_FollowUpResolution,
                     effect: "followAfterResolution(320)",
                     instanceId: instanceId);
            ctx.Log($"「{card.Name}」结算完毕 —— 后续结算触发（原版 `FollowUpResolution` = 410）");
        }

        /// <summary>🆕 **E 族发射口**：监听者**真的结算了** ⇒ 给它发一条按它自己卡名的
        /// `EvtKind.Trigger`（关键词 = 那条监听能力的原版 trigger 名）。
        /// `unit` 为 null（手牌 / 弃牌堆里的监听者）时 `slot = -1`（表现层今天没有手牌锚，见报告 §四）。
        /// <para>🆕 **2026-10-24（引擎线 `G2` · `C4` D8）**：多一个 <paramref name="selfOnly"/> ——
        /// 透传给 <see cref="TriggerNameOfWhenKind"/>（`Deploy` 的 190/195 分流）。
        /// 只有「主体自己那一跳」（`FireBoardWhenListener` 的 `selfOnlyOnly`）传 `true`；
        /// 其余调用点（常驻 / 手牌陷阱 / 手牌监听 / 棋盘循环）**一律缺省 `false`**，行为逐字不变。</para></summary>
        internal static void EmitListenerTrigger(BattleContext ctx, CardDef card, int owner, UnitState unit,
                                                 string kind, bool selfOnly = false)
        {
            if (ctx == null || card == null) return;
            string tn = TriggerNameOfWhenKind(kind, selfOnly);
            if (tn == null) { WarnUnmappedListenerKind(kind); return; }   // 🔴 2026-10-26（批次 6 · `G14` ③）：不再静默 —— 未映射 ⇒ 效果照跑、动画不播，必须出声（见 `WarnUnmappedListenerKind`）
            if (!EmitCardTrigger(ctx, card, owner, unit, tn, "listener:" + kind)) return;
            ctx.Log($"「{card.Name}」的监听器结算完毕 → 触发事件 `{tn}`（原版 AbilityTrigger）");
        }

        /// <summary>🆕 **2026-10-24（引擎线 `G1b` · `C2` §6）：关键词施加的统一发射口。**
        ///
        /// <para>**判据 = 原版 `BattleCardUI.DisplayStatusAnim` 的调用点那一族**：
        /// 它在「某条 effect / trait 被加到这张卡上」那一刻被调（`CardScript__Stun.c:125` ·
        /// `CardScript__AddEffect.c:678/:890`），`Start(0)` 那一档**每次都重演**、
        /// `OnPlay(2)` 那一档只首次演（判据全文 → `TraitFrames.DisplayStatus` 的 doc）。</para>
        ///
        /// <para>🔴 <paramref name="reapply"/> = **施加【之前】这个关键词已经在身上**（`KwValue &gt; 0`）——
        /// 它写进 `BattleEvent.Amount`：`0` = 首次（由 `SyncKeywords` 的「新出现」路演）、
        /// `&gt; 0` = 重施加（由本事件演）。两条路**结构上互斥** ⇒ 一次施加恰好演一次
        /// （见 `EvtKind.StatusApplied` 的 doc）。⛔ 别把两档都交给人演（同一次施加会演两遍）。</para>
        ///
        /// <para>⚠️ 不在场上（`FindSlot` 失败）⇒ **不发**：表现层没有落点；
        /// 它上场时 `SyncKeywords` 那条路仍会把「新出现」补上。</para></summary>
        static void EmitStatusApplied(BattleContext ctx, UnitState u, string keyword, bool reapply)
        {
            if (ctx == null || u == null || string.IsNullOrEmpty(keyword)) return;
            int p, slot;
            if (!FindSlot(ctx, u, out p, out slot)) return;
            ctx.Emit(EvtKind.StatusApplied, p, slot, keyword,
                     amount: reapply ? 1 : 0, instanceId: u.InstanceId);
        }

        /// <summary>
        /// 🆕 **2026-10-25（`E1` · 第四段 · W9）：一个 trait 被摘掉的**结构化发射口**。**
        /// 与 <see cref="EmitStatusApplied"/> 成对（那条管「挂上」）。判据与「为什么要单开一种」→
        /// <see cref="EvtKind.TraitRemoved"/> 的 doc（`Expiring State` 那一支需要它）。
        /// ⚠️ **必须在摘除【之后】调**（`Amount` 记的是摘之前的层级）。
        /// ⚠️ 只对**在场**单位发（`FindSlot` 失败 = 已经不在棋盘上 ⇒ 表现层也没东西可演）。
        /// </summary>
        public static void EmitTraitRemoved(BattleContext ctx, UnitState u, string keyword, int amountBefore)
        {
            if (ctx == null || u == null || string.IsNullOrEmpty(keyword)) return;
            int p, slot;
            if (!FindSlot(ctx, u, out p, out slot)) return;
            ctx.Emit(EvtKind.TraitRemoved, p, slot, keyword,
                     amount: amountBefore, instanceId: u.InstanceId);
        }

        /// <summary>
        /// 🔴🆕 **2026-10-24（引擎线 `T03·c` · `C1` §3）：`UnitDeathByAbility(180)` 的结构化发射口。**
        ///
        /// <para>**判据（第一权威，逐层方法体）**：
        /// `BattleManagerSupport__BroadcastDeadUnit.c:82-150`（遍历 `manager+0x470` = `allCardsInPlay`，
        /// 对**每一张在场卡**各调一次 `CardScript__ResolveDeadCard`，`:147`）
        /// → `CardScript__ResolveDeadCard.c:228-237` → `CardScript__TriggerOnMinionDeath.c:97`
        /// → **`RawCardScript__OnMinionDeath.c:25-37`**：按 `deathType` 分派
        /// `10 → 175 UnitDeathAttacking` · **`20 ability → 180 UnitDeathByAbility`** · `40 → 170 UnitDeathByPoison`；
        /// 其余（`15 combatDefender` / `30 tactic` / `50 collectWaystone`）**一条都不发**。</para>
        ///
        /// <para>**谁收到**：`param_1 = thisCard` 是**每一张在场卡**（含刚死的那个自己 ——
        /// `BroadcastDeadUnit` 那一刻它还在 `allCardsInPlay` 里、状态 `waitingToDie(5)`）；
        /// 死的是谁由 `param_5 = dead` 传进来。⇒ 本函数扫**双方棋盘全部单位**（**不**按
        /// `WhenTriggers` 过滤 —— 那是**正文**监听器；这一族是**结构化** `abilityTriggers`），
        /// 顺序照原版活列表 = **进场序**（`PlayOrder`）。</para>
        ///
        /// <para>🔴 **与正文后置条件 `targetdies` 的分工（两路并存，别双发）**：
        /// 4 张带 180 的卡（`AM9` / `AM34` / `SOR29` / `UM99`）**效果本身**今天仍由正文解析的
        /// 后置条件 `targetdies`（`EffectText.EffectCondition`）承担 —— 那条链**不动**。
        /// 本函数补的是**结构化发射口**：① 表现层按原版 trigger 名归口动画；
        /// ② 将来做结构化 ability 时的挂点。⇒ ⛔ **这里只发 `EvtKind.Trigger`、不结算任何效果**，
        /// 否则会与 `targetdies` 双发（同一件事做两遍）。</para>
        ///
        /// <para>**`abilityContext` 闸**（原版 `CardAbility__CanTriggerAbility` → `IsRightContext`，
        /// 在**广播那一刻**对**能力载体 thisCard** 判）：现读 4 张里 `AM9` 是 **`50 InPlayOrCemetery`**、
        /// 另三张是 **`0 Play`** ⇒ ⛔ 别一律按 Play 判。`-1`（旧表/未知）⇒ 不判（保持改前口径）。</para>
        /// </summary>
        internal static void EmitDeathByAbilityTriggers(BattleContext ctx, int deadOwner, UnitState dead)
        {
            EmitDeathTriggerToListeners(ctx, deadOwner, dead,
                "UnitDeathByAbility", 180, "deathByAbility", "被能力打死");
        }

        /// <summary>
        /// 🔴🆕 **2026-10-24（引擎线 `G2` · `C5` §1）：`UnitDeathByPoison(170)` 的结构化发射口。**
        ///
        /// <para>**判据**：与 180 **同一个调用**（`RawCardScript__OnMinionDeath.c:25-37`），
        /// 死因白名单里 `deathType == 0x28 (40 poison)` ⇒ `uVar2 = 0xaa = 170`。
        /// 15 / 30 / 50 以及 `0 undefined` 都在 `:34` 的 `return` 上 ⇒ **一条都不发**。
        /// 触发名见 `Core/CardDef.cs:1592-1593`（`UnitDeathByPoison`）。</para>
        ///
        /// <para>⚠️ 与 <see cref="EmitDeathByAbilityTriggers"/> **逐字同形**，区别只有触发名/号 ——
        /// 别再复制第三份：三族共用 <see cref="EmitDeathTriggerToListeners"/>。
        /// ⚠️ 池内 1,351 张卡**没有任何一张**带 170（`C5` §1 证据⑧）⇒ 现读**不可观测**，
        /// 断言用合成夹具（`RuleEngineTest_G2.cs`）。</para>
        /// </summary>
        internal static void EmitDeathByPoisonTriggers(BattleContext ctx, int deadOwner, UnitState dead)
        {
            EmitDeathTriggerToListeners(ctx, deadOwner, dead,
                "UnitDeathByPoison", 170, "deathByPoison", "中毒而死");
        }

        /// <summary>
        /// 🔴🆕 **2026-10-24（引擎线 `G2` · `C5` §1）：`UnitDeathAttacking(175)` 的结构化发射口。**
        ///
        /// <para>**判据**：同一条白名单，`deathType == 10 (combatAttacker)` ⇒ `uVar2 = 0xaf = 175`
        /// （`RawCardScript__OnMinionDeath.c:25-26`）。语义 = 「**在攻击时死**（被反击打死）」——
        /// 我们的唯一产地 = 反击那一路（`RuleCore.cs` 的 `DeclareAttack` 尾段 `Hurt(…, 反击)`，
        /// 原版 `_ResolveAttack_d__438__MoveNext.c:1142` 的 `UnitDeathType.combatAttacker(10)`）。
        /// ⚠️ 攻击**防御方**死亡是 `combatDefender(15)` ⇒ **不发**（⛔ 别把「攻击中有人死」都当 175）。</para>
        ///
        /// <para>⚠️ 池内 0 张卡监听 175 ⇒ 现读不可观测（同上）。</para>
        /// </summary>
        internal static void EmitDeathAttackingTriggers(BattleContext ctx, int deadOwner, UnitState dead)
        {
            EmitDeathTriggerToListeners(ctx, deadOwner, dead,
                "UnitDeathAttacking", 175, "deathAttacking", "在攻击中死亡");
        }

        /// <summary>
        /// 🔴🆕 **2026-10-24（引擎线 `G2`）：死因三族（**170 / 175 / 180**）共用的发射口核心。**
        ///
        /// <para>三族的原版判据是同一条 `RawCardScript__OnMinionDeath.c:25-37` 的 switch：
        /// `10 → 175` · `20 → 180` · `40 → 170` · 其余 `return` ⇒ 三族**除触发名/号之外逐字相同**
        /// （同一份监听者集合、同一道 `abilityContext` 闸、同一份 `EvtKind.Trigger`），
        /// ⇒ 收在本函数一处，⛔ 别复制三份（复制的那两份迟早与 180 分叉）。</para>
        ///
        /// <para>⚠️ **本函数只发信号、不结算效果**（理由同 180：效果由正文 `targetdies` 承担）。</para>
        /// </summary>
        static void EmitDeathTriggerToListeners(BattleContext ctx, int deadOwner, UnitState dead,
                                               string TN, int triggerId, string effectPrefix, string how)
        {
            if (ctx == null || ctx.IsOver) return;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，「{TN}」的发射不再连锁");
                return;
            }
            // 快照（照原版活列表的**进场序**）—— 见 doc：这一族**不**按 `WhenTriggers` 过滤。
            var listeners = new List<BoardListener>();
            for (int p = 0; p < 2; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || u.Card == null) continue;
                    listeners.Add(new BoardListener { Unit = u, Owner = p });
                }
            }
            RuleCore.StableSortByPlayOrder(listeners, l => l.Unit.PlayOrder);
            using (EventSlotsScope.TargetAndCard(ctx, dead, dead != null ? dead.Card : null))
            {
                foreach (var l in listeners)
                {
                    var u = l.Unit;
                    if (u == null || u.Card == null) continue;
                    // context 闸：只在**能定位载体**时判（与 `ActionPump.EnqueueAbility` 同一条纪律）
                    int context = AbilityContextOfAbilityV14(u.Card, TN, null);
                    if (context != CardDef.AbilityContextUnknown)
                    {
                        int o, s;
                        FindBoardCell(ctx, u, out o, out s);
                        if (o >= 0 && !IsRightContext(ctx, o, s, context, u.Instance))
                        {
                            // ⛔ 不许静默：原版这一格是「不触发」，我们也说不出为什么别的卡没响
                            ctx.Log($"[{TN}]「{u.Card.Name}」此刻不在原版 `abilityContext="
                                  + AbilityContextName(context) + "` 要求的区域 ⇒ 不发（判据 `IsRightContext`）");
                            continue;
                        }
                    }
                    if (EmitCardTrigger(ctx, u.Card, l.Owner, u, TN,
                                        effectPrefix + ":" + (dead != null ? dead.Name : "?")))
                        ctx.Log($"「{u.Card.Name}」收到「{(dead != null ? dead.Name : "?")} {how}」"
                              + $" → 触发事件 `{TN}`（原版 AbilityTrigger = {triggerId}）");
                }
            }
        }


        /// <summary>🔴🆕 **2026-10-24（引擎线 `G2` · `C4` D9）：`WhileInPlay`(20) 的**表现口** ——
        /// 每次「动作队列排空」给场上带这一拍动画的单位各发一条 `EvtKind.Trigger`。
        ///
        /// <para>**为什么需要**：原版 `AbilityTrigger.WhileInPlay = 20` 的动画行由
        /// `CardScript__UpdateWhileInPlay.c:19-20` → `RawCardScript__UpdateWhileInPlay.c:14`
        /// 的 `RawCardScript__TriggerAbilities(0x14 = 20)` 派发。**这一跳在缓存闸之前** ——
        /// 也就是说「牌型缓存没变」**不影响**它（缓存 `+0x358` 只护 `RemovePackBuffs`/`AddEffect`
        /// 那半段，见 `Aura.cs` 的那段注释）。⇒ 原版**每次** `BroadcastWhileInPlay` 都会重发
        /// trigger 20，不是「只在重算有变化时发」。</para>
        ///
        /// <para>**频率（判据 = 全量反编译，逐调用点核过）**：`BattleManagerSupport__BroadcastWhileInPlay`
        /// 只有 6 个调用点 ——
        /// · `BattleManager__FinishResolvingAction.c:91`（**动作队列排空**那一格，`:34-37`
        ///   `ActionsLeftInQueue() < 1 && param_2 != 7`）← **本条落点**；
        /// · `BattleManager._NextTurn_d__395__MoveNext.c:404`（换回合）·
        /// · `_ResolveExchangeMinion_d__552:344` / `_ResolveStealMinion_d__551:122` /
        ///   `ResolveTransformUnit.c:1726` / `…TransformFromRemnant_b__0:23`（四条棋盘变动）。
        /// 我们这四条棋盘变动都发生在动作内部，队列在那条动作结束后排空 ⇒ **这个落点全都覆盖**。
        /// ⛔ **不许挂到 `Aura.Recompose`**（它每次棋盘写入都跑，一个动作里可能好几次 ⇒ 多播）。</para>
        ///
        /// <para>**为什么不发满 13 张**：`cards_engine.json` 现读 `abilityTriggers` 含 **20** 的有
        /// **13 张**，但其中**只有 `SW18 Hrolf the Ironhowl` 有 `animTimes[].tn == "WhileInPlay"`
        /// 的动画行**（2 行）。其余 12 张的**效果**由光环层（`Aura.Recompose`）负责，这一条信号
        /// 只为**表现**；没有动画行 ⇒ 发了也没东西播、还会让 `HasTriggerSource` 的 ② 支
        /// 每次排空打一条「有 ability 没动画行」日志 ⇒ 这里**先按 `HasAnimTrigger` 收窄**
        /// （⛔ 不是静默吞掉能力：那 12 张的能力判据在光环层，不在这条信号上）。</para>
        ///
        /// <para>⚠️ **手牌那一半没做**：原版 `BroadcastWhileInPlay` 也扫**当前玩家手牌**与**对手手牌**
        /// （`:54-117`）。现读全池**没有一张手牌卡**带 `WhileInPlay` 动画行
        /// （唯一那两张 `SW18` 行都是棋盘单位）⇒ 今天无可观测差异；⚠️ 将来出现时**要一起补**。</para>
        /// </summary>
        internal static void EmitWhileInPlayPass(BattleContext ctx)
        {
            if (ctx == null || ctx.IsOver) return;
            const string TN_WhileInPlay = "WhileInPlay";   // 20（`CardDef.cs` 的 `AbilityTrigger` 表）
            for (int p = 0; p < 2; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < board.Length; s++)
                {
                    var u = board[s];
                    if (u == null || u.Card == null) continue;
                    if (!HasAnimRowOnly(u.Card, TN_WhileInPlay)) continue;   // 见 doc：只为表现，无行不发
                    EmitCardTrigger(ctx, u.Card, p, u, TN_WhileInPlay, "whileInPlay", animRowOnly: true);
                }
            }
        }

        /// <summary>🔴🆕 **2026-10-23（引擎线 `P1·③`）：`EvtKind.Trigger` 三族共用的【双闸】**
        /// —— A（`FollowUpResolution` 410）/ E（监听者）/ F（回合起止）都只走这一处判。
        ///
        /// <para>**改前**：闸 = <see cref="CardDef.HasAnimTrigger"/>（问「**有没有动画行**」）。
        /// 错在把「有东西可播」当成了「这条 ability 在不在」—— 实测两者**不等价**：
        /// 410 有 ability 的 30 张里 **6 张没有动画行** · 31 缺 4 张 · 30 缺 2 张
        /// （`GOF86` / `UM88` / `TAU3` / `BL35` 的 `animTimes` 是空表却带回合起止正文）
        /// ⇒ 那一拍 `EvtKind.Trigger` **从不发**（监听者/表现层都收不到）。
        /// 判据 → `资料/普查产出_1023c/引擎深C_数据吻合.md` §四·1。</para>
        ///
        /// <para>**闸（双闸，⛔ 不是删闸；🔴 2026-10-24 `W2` · `C5` Q2 订正了次序）**：
        /// ① **ability presence 才是第一道**（`AbilityTriggerPresence`，原版 `CanTriggerAbility.c:46-48`）：
        ///    `0` ⇒ **不发**（原版连 `EvtKind.Trigger` 那一层都没有）；`1` ⇒ 照发，
        ///    并看有没有动画行 —— **没有就出声**（同一 `(卡,trigger)` 只喊一次）；
        /// ② `-1`（旧卡表 / 合成夹具 / 认不出的名字）⇒ 回退「有动画行 ⇒ 发」的旧口径。
        /// 🔴 **改前**是「有动画行 ⇒ 发；否则看 presence」—— 次序反了（今天全池**零可观测差异**，
        /// 但有动画行、没 ability 的卡在原版是**绝对静默**的）。</para>
        ///
        /// <para>⚠️ **为什么不直接删闸**：删了会把「连 ability 都没有」的卡也发一遍
        /// ⇒ 掉进 `trigger` 粗桶**乱播**（判据见本节开头那段「多播」注释）。</para>
        /// </summary>
        static bool HasTriggerSource(CardDef card, string triggerName, BattleContext ctx, string effect)
        {
            if (card == null || string.IsNullOrEmpty(triggerName)) return false;

            // 🔴 2026-10-24（执行代理 `W2` · `C5` Q2 收口）：**ability presence 是第一道闸**。
            //   判据（第一权威，逐句读过）：
            //     · `RawCardScript.TriggerAbilities` 是**唯一入口**，它遍历每条 ability 先过
            //       `CardAbility.CanTriggerAbility`（`RawCardScript__TriggerAbilities.c:108` / `:207`）；
            //     · `CardAbility__CanTriggerAbility.c:46-48` 的**第一句**就是
            //       `if (ability.trigger != trigger) return 0;` ⇒ ability 不在 ⇒ **什么都不发生**
            //       （不结算、不发事件、**也不播动画**）；
            //     · 动画在**里面**（`TriggerAbility` → `PlayTriggerTraitAnim`，`:60`），
            //       只会在 `CanTriggerAbility` 返回 true 之后才走。
            //   ⇒ 正确次序 = `presence == 1` 优先；`presence == 0` 直接不发；`-1`（旧表 / 认不出）
            //     才回退「有没有动画行」这条旧口径。
            //   ⚠️ 改前是「先看动画行、再看 ability」⇒ **语义反了**，但今天**全池 0 张卡**落在
            //     「有动画行、没有 ability」这个差集里（`C5` Q2 逐卡逐行现读，41 个 trigger 名全 0）
            //     ⇒ 本条**零可观测行为变化**，修的是结构。
            int presence = card.AbilityTriggerPresence(triggerName);
            if (presence == 0) return false;                 // 明确没有这条 ability ⇒ 原版全静默
            bool anim = card.HasAnimTrigger(triggerName);
            if (presence == 1)
            {
                // 有 ability、没动画行 ⇒ 引擎语义上这一拍照发，但**出声**（表现层没东西可播）。
                // ⚠️ 同一 `(卡, trigger)` 只喊一次 —— `Pray` 那 23 张「655 ability、一行动画都没有」
                //   会让这条日志**每局刷屏**（`C5` Q2 的表）；⛔ 但不许降级成静默（本工程红线）。
                if (!anim)
                    NoteAbilityWithoutAnimRow(card, triggerName, effect, ctx);
                return true;
            }
            return anim;                                     // `-1`：旧卡表 / 合成夹具 / 认不出的名字 ⇒ 旧口径
        }

        /// <summary>🆕 2026-10-24（执行代理 `W2` · `C5` Q2）：**只看「有没有动画行」的表现闸**
        /// （与 <see cref="HasTriggerSource"/> 的语义闸分开）。
        ///
        /// <para>**为什么要分开**：`WhileInPlay(20)` / `WhileInPlayVariable(240)` 这两族在原版
        /// **永远不走** `PlayTriggerTraitAnim`（`RawCardScript__TriggerAbility.c:60` 那两条 `!=`）；
        /// `EmitWhileInPlayPass` 自称「**只为表现，无行不发**」（判据在那里的 doc），
        /// 若跟着语义闸一起放宽，`WhileInPlay` 会从「1 张有动画行的卡」变成「13 张有 ability 的卡」
        /// 都发事件（多 12 条空转）⇒ 这一族继续用**本方法**（= 改前的口径）。</para>
        /// </summary>
        static bool HasAnimRowOnly(CardDef card, string triggerName)
        {
            return card != null && !string.IsNullOrEmpty(triggerName) && card.HasAnimTrigger(triggerName);
        }

        /// <summary>「有 ability、没动画行」的出声口 —— 同一个 `(卡, trigger)` **只喊一次**。
        /// ⛔ 不是静默（`C5` Q2 明确要求保留这条日志）；⛔ 也不是每局刷屏（`Pray` 那 23 张）。
        /// 🔴 **2026-10-25 订正（铁律 5 · 联机自检 7 红查实）**：原来这里还有一句
        /// `if (ctx != null) ctx.Log(msg);` —— **已删**，因为：
        ///   · `_abilityNoAnimNoted` 是**进程级静态**去重表 ⇒ 同一个进程里**第二个** `BattleContext`
        ///     （联机自检/重放重建都这样跑）**拿不到**这条 note；
        ///   · `NetProtocol.Fingerprint` 把 `ctx.Events.Count` 计入哈希 ⇒ 两端指纹差 **31² = 961**
        ///     （实测 `-381087003` vs `-381087964`，局面其实逐位相同）；
        ///   · 项目自家纪律：诊断类出声**只走 `Debug.LogWarning`**、⛔ 不进 `ctx.Log`
        ///     （先例 `RuleCore.NoteAbilityPickOwnerGap` / `NoteActivePickGap` / `StructuredConditionNotice`）。
        /// 「不许静默」由下面那句 `Debug.LogWarning` 满足。</summary>
        static void NoteAbilityWithoutAnimRow(CardDef card, string triggerName, string effect, BattleContext ctx)
        {
            string key = (card.Id ?? card.Name ?? "?") + "|" + triggerName;
            if (!_abilityNoAnimNoted.Add(key)) return;
            string msg = BuildAbilityNoAnimRowMessage(card, triggerName, effect);
            UnityEngine.Debug.LogWarning(msg);
            // ⛔ 别再加回 `ctx.Log(msg)` —— 会动 `ctx.Events.Count` / 联机 Fingerprint（见上）。
        }

        /// <summary>「有 ability、没动画行」那条日志的**文案**（唯一产地 —— `NoteAbilityWithoutAnimRow`
        /// 与自检读口 <see cref="AbilityNoAnimRowMessageForTest"/> 共用，⛔ 别在两处各写一份）。</summary>
        static string BuildAbilityNoAnimRowMessage(CardDef card, string triggerName, string effect)
        {
            if (string.Equals(triggerName, "Talent", System.StringComparison.Ordinal))
            {
                // 🆕 **2026-10-25（引擎线 `A2` · `A2-5`）：天赋那一拍换准确文案。**
                //   原来它走下面那条通用句「有 ability 但**没有对应动画行**」—— 技术上真
                //   （`animTimes` 里确实 0 行），但**误导**：天赋的表现**不走 `animTimes`**，
                //   走的是 **trait 框**（`trait_effect_bindings.json` 的 `triggerType 3/6`，
                //   由 `Core/TraitFrames.DisplayTrigger` 接；判据 → `EmitTalentTrigger` 的 doc
                //   与 `资料/普查产出_1024c/C5_引擎战斗剩余判据.md` §Q1）。
                //   ⛔ 仍然 `LogWarning` + `ctx.Log`（本工程红线：不许静默），只是**把话说准**。
                return $"[EffectResolver] 「{card.Name}」的 `Talent`（AbilityTrigger 46）触发**已发出**"
                    + $"（{effect}）—— 原版判据 `CardAbility__CanTriggerAbility.c:46-48`："
                    + "ability presence 是第一道闸。**`animTimes` 里没有对应行不是缺陷**："
                    + "天赋那一拍的表现是 **trait 框**（`Resources/trait_effect_bindings.json` 的 "
                    + "`triggerType 3/6`，由 `Core/TraitFrames.DisplayTrigger` 接），不走 `animTimes`。"
                    + "本消息同一 (卡,trigger) 只出现一次。";
            }
            return $"[EffectResolver] 「{card.Name}」有 `{triggerName}` 这条 ability 但**没有对应动画行**"
                 + $" ⇒ 照发事件（{effect}）—— 判据 = 原版 `CardAbility__CanTriggerAbility.c:46-48`"
                 + "（ability presence 是第一道闸，动画在里头播）；表现层这一拍可能没有东西可播"
                 + "（数据缺口，如实记着）。本消息同一 (卡,trigger) 只出现一次。";
        }

        /// <summary>🔴 **只为自检开门**（同 `HurtForTest` / `CountForTest` 的纪律：**不改任何行为**）——
        /// 返回 <see cref="NoteAbilityWithoutAnimRow"/> 会打的**文案**（不写日志、不进
        /// `_abilityNoAnimNoted` 账）。用途 = `A2-5` 断言「天赋那一拍的说法准确」。
        /// ⚠️ 自检只断**关键短语**，⛔ 别把整句抄进测试（改一个字就红，那不是判据）。</summary>
        public static string AbilityNoAnimRowMessageForTest(CardDef card, string triggerName)
        {
            if (card == null || string.IsNullOrEmpty(triggerName)) return "";
            return BuildAbilityNoAnimRowMessage(card, triggerName, "test");
        }


        static readonly HashSet<string> _abilityNoAnimNoted = new HashSet<string>();

        /// <summary>🔴 **只为自检开门**（同 `HurtForTest` / `CountForTest` 的纪律：**不改任何行为**）——
        /// Editor 程序集看不见 `internal`，所以这里必须 `public`。
        /// 读口 = <see cref="HasTriggerSource"/>（`C5` Q2 的语义闸：**presence 优先**）。</summary>
        public static bool HasTriggerSourceForTest(CardDef card, string triggerName,
                                                   BattleContext ctx = null, string effect = "test")
        {
            return HasTriggerSource(card, triggerName, ctx, effect);
        }

        /// <summary>🔴 **只为自检开门**：读口 = <see cref="HasAnimRowOnly"/>（`WhileInPlay` 那一路的表现闸）。
        /// 与上面那条**互为灭自证**：同一张「有 ability、没动画行」的卡，两条必须一真一假。</summary>
        public static bool HasAnimRowOnlyForTest(CardDef card, string triggerName)
        {
            return HasAnimRowOnly(card, triggerName);
        }

        /// <summary>🆕 **所有「按原版 trigger 名发 `EvtKind.Trigger`」的唯一发射体**
        /// （`T21` A/E/F 三族 ＋ `W57` 的 600/605 共用）。
        ///
        /// <para>**闸 = <see cref="HasTriggerSource"/>**（判据只此一处）：
        /// 卡片自己的逐候选动画里有一个标到这个名字的行 **或者** 真值 `abilityTriggers` 说它有这条
        /// ability ⇒ 才发。两者都没有 ⇒ **不发**（发了也没东西播，还会在战斗日志里多印一行、
        /// 并可能掉进 `trigger` 粗桶乱播）。</para>
        /// <para>返回 `true` = 真发出去了（调用方可以据此打日志）。</para>
        /// <para>🆕 **2026-10-20（`FACTION-DOUBLE605`）**：可选 `abilityIndex`（≥0 = 原版
        /// `cardAbilities[i]` 下标）—— 写进 <see cref="BattleEvent.AbilityIndex"/>，并把 `#i` 后缀
        /// 拼进 `Keyword` 当**传输副本**（`BattleDriver` 只转 `Keyword`，见 `BattleEvent.AbilityIndex` 的 doc）。
        /// `-1`（默认）⇒ `Keyword` 一个字节都不变。</para></summary>
        static bool EmitCardTrigger(BattleContext ctx, CardDef card, int owner, UnitState unit,
                                    string triggerName, string effect, int abilityIndex = -1,
                                    // 🆕 2026-10-24（`W2` · `C5` Q2）：走**表现闸**（只看动画行）而不是语义闸。
                                    //   只有 `EmitWhileInPlayPass` 传 true（判据 → `HasAnimRowOnly` 的 doc）。
                                    bool animRowOnly = false)
        {
            if (ctx == null || card == null || string.IsNullOrEmpty(triggerName)) return false;
            if (animRowOnly)
            {
                if (!HasAnimRowOnly(card, triggerName)) return false;
            }
            else if (!HasTriggerSource(card, triggerName, ctx, effect)) return false;
            int slot = -1, p = owner;
            if (unit != null && FindSlot(ctx, unit, out p, out slot)) { /* 用找到的座位/格号 */ }
            else if (unit != null) { slot = -1; p = owner; }
            ctx.Emit(EvtKind.Trigger, p, slot, card.Name,
                     keyword: BattleEvent.KeywordWithAbility(triggerName, abilityIndex),
                     effect: effect,
                     instanceId: unit != null ? unit.InstanceId : 0,
                     abilityIndex: abilityIndex);
            return true;
        }

        /// <summary>🔴🆕 **2026-10-25（引擎线 `A3` · `C5` Q1）：「天赋」那一拍（`talent`）的发射口。**
        ///
        /// <para>**为什么单开一个方法**：这一拍（`Talent` / 46）在**两个** `SpawnTalents` 生成分支
        /// 之前都要发，且判据散文很长 ⇒ 收在这里当**唯一**入口（⛔ 别在调用点再抄第二份）。
        /// ⚠️ 本文件与 `RuleCore.cs` 是**同一个 `partial class`**（本文件的 `namespace` 里就是
        /// `public static partial class RuleCore` —— 全仓**没有**叫 `EffectResolver` 的类；
        /// 文档里那些 `EffectResolver.Xxx` 是口头叫法），所以调用点直接写方法名。</para>
        ///
        /// <para>**判据**（五跳，逐句 → `资料/普查产出_1024c/C5_引擎战斗剩余判据.md` §Q1）：
        /// `CardScript.OnTurnStart.c:160`（`HasCurrentTrait(0x34d trait talent)`）→
        /// `:167 OnTrigger(0x2e = AbilityTrigger.Talent = 46)` → `RawCardScript.TriggerAbility.c:60`
        /// `PlayTriggerTraitAnim` → `BattleAction 0x44` → `CardScript.ActivateTriggerTraitAnim` →
        /// `GetTraitFromAbilityTrigger(46)` = `DefinedTrait.talent(845)` →
        /// `BattleCardUI.DisplayTriggerAnim(845)` ⇒ 播 `Talent.asset` 的 trait 框
        /// （`triggerType 3 / TalentTraitTrigger / TalentEffect`，现读 `trait_collection.json`）。</para>
        ///
        /// <para>⚠️ **闸在这里【不再】判「这张卡有没有天赋」** —— 那一条由调用方按引擎已有的天赋判据
        /// 判过（`TalentCardData.Resolve(...).Count &gt; 0 || TalentName != null`；三个口径现读都等于
        /// **96 张**）。本方法只走 <see cref="HasTriggerSource"/> 那条**统一**闸
        /// （`abilityTriggers` 含 46 ⇒ presence = 1 ⇒ 发）。</para>
        ///
        /// <para>⚠️ **这一拍在 `animTimes` 里没有对应行**（trait 框走另一张表
        /// `trait_effect_bindings.json`）⇒ <see cref="HasTriggerSource"/> 会按纪律打一条
        /// 「有 ability、没动画行」的**一次性**日志（同一 `(卡,trigger)` 只喊一次）。
        /// 这是**如实**的：`animTimes` 确实没有它；trait 框由 `Core/TraitFrames.cs` 接。</para>
        /// </summary>
        internal static void EmitTalentTrigger(BattleContext ctx, CardDef card, int owner, UnitState unit)
        {
            const string TN = "Talent";                       // `AbilityTrigger.Talent = 46`
            if (!EmitCardTrigger(ctx, card, owner, unit, TN, "talent:turnStart")) return;
            ctx.Log($"「{card.Name}」回合开始 → 触发事件 `{TN}`（原版 `AbilityTrigger.Talent = 46`）");
        }

        /// <summary>🆕 **2026-10-19（第五十七会话 `W57` · T21 C/D）：一张卡【自己的阵营能力】激活了** ——
        /// 按它自己的动画行发 `EvtKind.Trigger`（`UseFaith` 605 / `UseSpiritStone` 600）。
        ///
        /// <para>**为什么必须发**：`card_vfx_by_card.json` 里这两族的动画行逐条标着
        /// 「**未归口（保持 `ability`；原版触发 `UseFaith`/`UseSpiritStone` 我们引擎没有对应事件）**」
        /// —— 也就是引擎从来没告诉表现层「这是哪一拍」，只能掉进 `ability` 粗桶。
        /// 现读规模（`cards_engine.json` 的 `animTimes`，2026-10-19 现数）：
        /// `UseFaith` **47 行 / 26 张** · `UseSpiritStone` **74 行 / 29 张**。
        /// 有了这条，`VfxMap.TryResolveTriggerPicks` 就能按名字归口。</para>
        ///
        /// <para>⚠️ **只在能力真的激活了之后调**（调用方用 `ResolveOps` 的返回值判）——
        /// 付不起 / 门槛没过的卡原版**整条 ability 都不激活**，那一拍动画也就不该播。</para>
        /// <para>🆕 **2026-10-20（`FACTION-DOUBLE605`）**：`abilityIndex`（≥0 = 原版 `cardAbilities[i]`
        /// 的下标）透传给 <see cref="EmitCardTrigger"/> —— 同一张卡多条 605 能力各发一次时用它区分
        /// （见 `EmitFaithAbilityUseTriggers` 的 doc）。单组卡保持 `-1` ⇒ 与改前逐字节相同。</para>
        /// </summary>
        internal static void EmitAbilityUseTrigger(BattleContext ctx, CardDef card, int owner, UnitState unit,
                                                   string triggerName, int triggerId, int abilityIndex = -1)
        {
            if (ctx == null || card == null) return;
            if (!EmitCardTrigger(ctx, card, owner, unit, triggerName, "ability:" + triggerName, abilityIndex)) return;
            ctx.Log($"「{card.Name}」的阵营能力激活完毕 → 触发事件 `{triggerName}`"
                  + $"（原版 AbilityTrigger = {triggerId}"
                  + (abilityIndex >= 0 ? $" · cardAbilities[{abilityIndex}]" : "") + "）");
        }

        /// <summary>
        /// 🆕 **2026-10-19（第五十七会话 `W57FIX2` · P1）：一张【战术卡】打出去时，它自己的
        /// `UseSpiritStone(600)` / `UseFaith(605)` 两拍。**
        ///
        /// <para>**原版判据**：`RawCardScript__OnCardPlayedWithTarget.c:59-84` —— 这一整段在
        /// `param_3 == param_4`（**收回调的那张牌就是被打出的那张牌**）里跑：
        ///   ① `param_3 != 0` ⇒ `TriggerAbilities(600)`；
        ///   ② 被打出的牌 `cardType == 0x14(Tactic)` 或 `0x28(Whispers)` ⇒ `TriggerAbilities(0x25d = 605)`。
        /// 闸在 `CardAbility__CanTriggerAbility.c:96-112`：`thisCard == cardPlayed` 时要求
        /// `GetCurrentSpiritStone / GetCurrentFaith >= ability.triggerValue`。
        /// 我们这边「阈值够不够」**复用** `ChargePaidGroups` 的 `paidOk`（同一份余额判据
        /// `CostPrefixPayable`，⛔ 别另写一份）—— 付费组收到钱 = 原版那条闸通过。</para>
        ///
        /// <para>⚠️ **只给战术卡用**（`PlayTactic` 一处调用）。单位卡那两拍走
        /// `ResolveSpiritAbility` / `ResolveFaithAbility`（`W57` 已接上）——
        /// ⛔ 别把本函数搬进 `ResolveOps` 统一发，那会把单位侧重复发一遍。</para>
        ///
        /// <para>⚠️ **`Hosts of the Dead`（ASH52）没有付费前缀**：正文 `Spend all your Spirit Stones`
        /// 解析成 `spendspirit`（`Cost = 0`）⇒ 只能靠「**结算前**手里有石头」判它激活
        /// （原版闸正是 `GetCurrentSpiritStone >= triggerValue(1)`）。`spiritBefore` 由调用方
        /// 在 `ResolveOps` **之前**取（结算会把它花光）。</para>
        ///
        /// <para>⚠️ 原版 605 只对 Tactic/Whispers 发；我们走 `PlayTactic` 的牌全部是这两档
        /// （`tactic` / `defence` 在原版都是 `cardType 20`）⇒ 不再额外判一次类型。</para>
        /// </summary>
        static void EmitTacticAbilityUseTriggers(BattleContext ctx, int owner, CardDef card,
                                                 IReadOnlyList<EffectOp> ops, bool[] paid, int spiritBefore)
        {
            if (ctx == null || card == null || ops == null || paid == null) return;
            // 🆕 2026-10-20（`FACTION-DOUBLE605`）：信仰那一边改成**收组**（原版每条 605 能力各发一次）。
            //   组首 = `Cost > 0 && !CostShared` 且 `paidOut` 说它收到钱；门槛 = `op.Cost`。
            var faithGroups = new List<EffectOp>();
            bool spirit = false;
            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                if (op == null || op.Cost <= 0 || op.CostShared) continue;   // 只看「付钱那条」
                if (i >= paid.Length || !paid[i]) continue;                  // 付费组没收到钱 ⇒ 这条没激活
                string kind = EffectText.CostKindOf(op.CostKind);
                if (kind == "faith") faithGroups.Add(op);
                else if (kind == "spirit") spirit = true;
            }
            if (!spirit && spiritBefore > 0)
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ops[i].Verb == "spendspirit") { spirit = true; break; }

            // 原版次序：600 在前、605 在后（`OnCardPlayedWithTarget.c:66` → `:73`）。
            if (spirit) EmitAbilityUseTrigger(ctx, card, owner, null, TN_UseSpiritStone, 600);
            if (faithGroups.Count > 0) EmitFaithAbilityUseTriggers(ctx, card, owner, null, faithGroups);
        }

        /// <summary>🆕 **2026-10-20（第五十七会话 `FACTION-DOUBLE605`）：`UseFaith(605)` 这一拍的唯一发射口**
        /// （单位侧 `ResolveFaithAbility` 与战术侧 `EmitTacticAbilityUseTriggers` 共用）。
        ///
        /// <para>**原版语义**：`RawCardScript__TriggerAbilities.c:59-88,207-265` 逐条遍历
        /// `cardAbilities`，每条过 `CanTriggerAbility`（605 的门槛 = `faith &gt;= ability.triggerValue`，
        /// `CardAbility__CanTriggerAbility.c:114-126`）就**那一条各演一次**。⇒ 同一张卡有两条 605
        /// 能力、门槛都过时，动画是**两条**；只过一条时**只有那一条**的动画（改前会把两条一起播）。</para>
        ///
        /// <para>**三种结果**：
        /// ① 这张卡不足两组信仰能力 ⇒ **老行为**：一条事件、`Keyword` 不带后缀、`AbilityIndex = -1`
        ///    （逐字节不变 —— 全池 26 张 `UseFaith` 卡里 23 张走这一档）；
        /// ② 多组、且 `CardDef.TryGetFaithAbilityIndex` 能把每组的门槛配到 `cardAbilities[i]` ⇒
        ///    **每组一条**事件（`AbilityIndex` = 能力下标；`Keyword` 带 `#i` 后缀当传输）；
        /// ③ 多组但配不上（条数不等 / 门槛乱序）⇒ 退回 ① 并**出声**（⛔ 不猜哪条动画属于哪条能力）。</para></summary>
        /// <param name="activatedGroups">本次**过了门槛的**付费组首（`EffectOp`，门槛 = `op.Cost`），
        /// 按 ops 顺序（= 正文顺序 = 原版 `cardAbilities` 顺序）。</param>
        static void EmitFaithAbilityUseTriggers(BattleContext ctx, CardDef card, int owner, UnitState unit,
                                                IReadOnlyList<EffectOp> activatedGroups)
        {
            if (ctx == null || card == null || activatedGroups == null || activatedGroups.Count == 0) return;

            // ① 单组 ⇒ 老行为（现读全池 23/26 张这一档，必须逐字节不变）
            if (card.FaithAbilityGroupCount < 2)
            {
                EmitAbilityUseTrigger(ctx, card, owner, unit, TN_UseFaith, 605);
                return;
            }

            // ②/③ 多组 ⇒ 逐组配 `cardAbilities[i]`
            var idx = new int[activatedGroups.Count];
            for (int i = 0; i < activatedGroups.Count; i++)
            {
                var g = activatedGroups[i];
                if (g == null || !card.TryGetFaithAbilityIndex(g.Cost, out idx[i]))
                {
                    ctx.Log($"「{card.Name}」有多组信仰能力，但这几组的门槛与动画行**对不上**"
                          + "（`CardDef.TryGetFaithAbilityIndex`）⇒ 这一拍按**一条**发"
                          + "（原版是每条 605 各发一次；⛔ 不猜哪条动画属于哪条能力）");
                    EmitAbilityUseTrigger(ctx, card, owner, unit, TN_UseFaith, 605);
                    return;
                }
            }
            for (int i = 0; i < activatedGroups.Count; i++)
                EmitAbilityUseTrigger(ctx, card, owner, unit, TN_UseFaith, 605, idx[i]);
        }

        /// <summary>🆕 **F 族：回合起止那一拍该发哪个 trigger 名**。
        /// 判据 = 卡片自己的 `AnimTimes` 里**唯一那一条** `Turn*` 名的行
        /// （现读全池：18 张卡**每张只有一种** —— `TurnStart` 3 · `TurnStartPlayer` 3 ·
        ///  `TurnEnd` 11 · `TurnBeforeEnd` 1，报告 §三 F）。
        ///
        /// <para>🔴🆕 **2026-10-24（引擎线 `T21④` · `A24`）：现在只服务 `turn_start` 那一趟**
        /// （`EmitAtTurnTriggerPass` 的 `onlyName == null` 分支）。`turn_end` 已改成
        /// **两遍指名发射**（`TurnBeforeEnd` 整趟 → `TurnEnd` 整趟，见 `EmitAtTurnTriggers`）——
        /// 那里**不再经过本方法**，所以「两张名字都有时只取第一个」这条对 `turn_end` 已不成立
        /// （原版两次广播都会响 ⇒ 我们两遍各发一次）。</para>
        ///
        /// <para>🔴🆕 **2026-10-23（引擎线 `P1·③`）**：改判 <see cref="HasTriggerSource"/>（双闸）——
        /// 只有动画行、没有 `abilityTriggers` 那一批（`TurnStart` 2 张 · `TurnStartPlayer` 4 张）
        /// 以前**连名字都取不到**（`null` ⇒ 整拍不发）。没有动画行的卡靠真值那一闸放行并出声。</para>
        ///
        /// `phase` 对不上 / 两闸都不成立 ⇒ `null`（不发）。</summary>
        static string TurnTriggerNameOf(CardDef card, string phase, BattleContext ctx = null)
        {
            if (card == null) return null;
            bool start = phase == "turn_start";
            var names = start
                ? new[] { "TurnStart", "TurnStartPlayer" }
                : new[] { "TurnEnd", "TurnBeforeEnd" };
            for (int i = 0; i < names.Length; i++)
                if (HasTriggerSource(card, names[i], ctx, "turn:" + phase))
                {
                    // ⚠️ 两张名字都有的卡今天不存在；真出现时**按顺序取第一个**并出声（别静默）。
                    //   ⚠️ 内层那次判**不传 ctx**（否则「有 ability 没动画」那条提示会打两遍）。
                    if (i > 0 && HasTriggerSource(card, names[0], null, "turn:" + phase) && ctx != null)
                        ctx.Log($"[EffectResolver] 「{card.Name}」同时标了 `{names[0]}` 与 `{names[i]}` 两种"
                              + $" {phase} trigger（动画行或 ability 真值）⇒ 按顺序取第一个");
                    return names[i];
                }
            return null;
        }

        /// <summary>🔥 **F 族发射口**：回合起止段里一个单位/一张手牌/一条常驻效果发事件。
        /// 名字按 <see cref="TurnTriggerNameOf"/> 在当相位的那几个候选里二选一
        /// （`turn_start` 走这条；`turn_end` 走 <see cref="EmitAtTurnTriggers"/> 的两遍）。
        /// <para>🆕 **2026-10-25（`E4` · `E4-1`）**：返回值改成**真发出去的那条 `BattleEvent`**
        /// （`null` = 没发）—— 棋盘单位那一趟要靠它把「结算时才选出来的目标」事后填进去，
        /// 见 <see cref="BattleContext.TriggerTargetSink"/>。</para></summary>
        static BattleEvent EmitTurnTrigger(BattleContext ctx, CardDef card, int owner, UnitState unit, string phase)
        {
            if (ctx == null || card == null) return null;
            string tn = TurnTriggerNameOf(card, phase, ctx);
            if (tn == null) return null;
            // ⚠️ 上面已经过闸（`TurnTriggerNameOf` 内部就是 `HasTriggerSource`）⇒ 这里**不再过第二次**，
            //    否则「有 ability、没动画行」那条出声日志会打两遍。
            return EmitTurnTriggerRaw(ctx, card, owner, unit, tn, phase);
        }

        /// <summary>🔥 **F 族发射口（指名版）**：只发 <paramref name="triggerName"/> 那一个名字。
        /// 闸 = <see cref="HasTriggerSource"/>（动画行 **或** 真值 ability 双闸）。
        /// 返回**真发出去的那条事件**（`null` = 没发，判据同 <see cref="EmitTurnTrigger"/>）。</summary>
        static BattleEvent EmitTurnTriggerNamed(BattleContext ctx, CardDef card, int owner, UnitState unit,
                                         string triggerName, string phase)
        {
            if (ctx == null || card == null || string.IsNullOrEmpty(triggerName)) return null;
            if (!HasTriggerSource(card, triggerName, ctx, "turn:" + phase)) return null;
            return EmitTurnTriggerRaw(ctx, card, owner, unit, triggerName, phase);
        }

        /// <summary>🔥 **F 族发射体**：不再过闸（调用方已经判过 —— 判据只留一处）。
        /// 返回**真发出去的那条事件**（给 <see cref="AtTurnUnitJobDeferred"/> 事后填目标用）。</summary>
        static BattleEvent EmitTurnTriggerRaw(BattleContext ctx, CardDef card, int owner, UnitState unit,
                                       string triggerName, string phase)
        {
            int slot = -1, p = owner;
            if (unit != null && FindSlot(ctx, unit, out p, out slot)) { /* 用找到的座位/格号 */ }
            else if (unit != null) { slot = -1; p = owner; }
            var evt = new BattleEvent
            {
                Kind = EvtKind.Trigger,
                Player = p,
                Slot = slot,
                CardId = card.Name,
                Keyword = triggerName,
                Effect = phase,
                InstanceId = unit != null ? unit.InstanceId : 0,
            };
            ctx.Emit(evt);
            return evt;
        }

        /// <summary>🆕 **2026-10-24（引擎线 `T21④` · 第四批 `A24`）：回合起止那一拍的
        /// `EvtKind.Trigger` **发射顺序** —— 照原版两个 Broadcaster，与**结算顺序解耦**。
        ///
        /// <para>**判据**（第一权威 = 全量反编译，本轮亲读）：
        /// · `BattleManager__ResolveEndTurn.c:385` → `BattleManagerSupport__BroadcastTurnBeforeEnd(...)`、
        ///   `:433` → `BattleManagerSupport__BroadcastTurnEnd(...)` —— **同一段直线代码、同帧**；
        ///   而卡侧 `CardScript__OnTurnBeforeEnd.c` = `RawCardScript.OnTrigger(0x29 = 41)`、
        ///   `CardScript__OnTurnEnd.c` = `OnTrigger(0x28 = 40)` ⇒ **41 严格早于 40**；
        /// · `BattleManagerSupport__BroadcastTurnEnd.c` / `__BroadcastTurnBeforeEnd.c` 的遍历序
        ///   **逐行一致**：① `manager+0x470`（`allCardsInPlay`，只对 `IsInPlay` 的调
        ///   `OnTurnEnd/OnTurnBeforeEnd`）→ ② `GetCardsInHandRef(IsPlayerTurn())`（**当前玩家手牌**）
        ///   → ③ `GetCardsInHandRef(!IsPlayerTurn())`（**对手手牌**）；
        /// · `BattleManagerSupport__BroadcastTurnStart.c` 同一序（⇒ `turn_start` 也按它发）。</para>
        ///
        /// <para>⚠️ **只改事件顺序**。三份作业的**入队/结算**次序（手牌 → 棋盘 → 常驻）是 `T03·c` 的
        /// 既有口径，⛔ **别顺手一起改**（那是另一条账，见 `ResolveAtTurn` 顶部那句）。</para>
        /// </summary>
        static void EmitAtTurnTriggers(BattleContext ctx, int active, string phase,
                                       List<AtTurnUnitJob> unitJobs, List<AtTurnHandJob> handJobs,
                                       List<AtTurnPersistJob> persistJobs)
        {
            if (ctx == null) return;
            if (phase == "turn_end")
            {
                // 🔴 **两遍**：`BroadcastTurnBeforeEnd`（`:385`）整趟跑完，才轮到
                //   `BroadcastTurnEnd`（`:433`）。⛔ 退回「一趟里按卡二选一」⇒ 手牌陷阱的
                //   `TurnEnd(40)` 会排到棋盘单位的 `TurnBeforeEnd(41)` 前面（本轮修的正是这条）。
                EmitAtTurnTriggerPass(ctx, active, phase, "TurnBeforeEnd", unitJobs, handJobs, persistJobs);
                EmitAtTurnTriggerPass(ctx, active, phase, "TurnEnd", unitJobs, handJobs, persistJobs);
            }
            else
            {
                // `turn_start`：原版 `BroadcastTurnStart`（`_NextTurn_d__395__MoveNext.c:371`）一趟；
                // 卡侧 `CardScript__OnTurnStart.c` 是「**无条件** `OnTrigger(0x1e = TurnStart, 30)`，
                // 随后**卡主 == 当前回合方** 时再 `OnTrigger(0x1f = TurnStartPlayer, 31)`」
                // ⇒ 两个名字在原版是**同一个回调里的两次 OnTrigger**（不是两条广播）。
                // ⚠️ `BroadcastTurnStarted`（`:385`，排在 `BroadcastActivateMinions` 之后）发的是
                //   `CardScript__OnTurnStarted.c` 里的 **`0xa0 = 160`**，**不是 31**（本轮现核）。
                // ⇒ 我们目前仍是**二选一**发（`TurnTriggerNameOf`）：池内**没有一张卡两个名字都有**
                //   ⇒ 今天无可观测差异；真出现时原版是「先 30 再 31（31 仅 owner==行动方）」，
                //   要按那条补（另有一条账，见报告「顺手发现」①）。
                EmitAtTurnTriggerPass(ctx, active, phase, null, unitJobs, handJobs, persistJobs);
            }
        }

        /// <summary>一趟广播（= 原版 `BroadcastTurnStart` / `BroadcastTurnBeforeEnd` / `BroadcastTurnEnd`
        /// 里的那三个循环）。<paramref name="onlyName"/> = `null` ⇒ 按 `TurnTriggerNameOf` 二选一。</summary>
        static void EmitAtTurnTriggerPass(BattleContext ctx, int active, string phase, string onlyName,
                                          List<AtTurnUnitJob> unitJobs, List<AtTurnHandJob> handJobs,
                                          List<AtTurnPersistJob> persistJobs)
        {
            BattleEvent EmitOne(CardDef c, int owner, UnitState unit)
            {
                if (c == null) return null;
                if (onlyName == null) return EmitTurnTrigger(ctx, c, owner, unit, phase);
                // ⚠️ 两张名字都有的卡今天**不存在**（现读全池 18 张 `Turn*` 卡各占一个名字：
                //    TurnEnd 11 · TurnBeforeEnd 1 · TurnStart 3 · TurnStartPlayer 3）。
                //    真出现时原版的**两条广播都会响**（两次 `OnTrigger`）⇒ 我们两遍各发一次，并出声。
                if (onlyName == "TurnBeforeEnd"
                    && HasTriggerSource(c, "TurnEnd", null, "turn:" + phase)
                    && HasTriggerSource(c, "TurnBeforeEnd", null, "turn:" + phase))
                    ctx.Log($"[EffectResolver] 「{c.Name}」同时标了 `TurnEnd` 与 `TurnBeforeEnd` 两种"
                          + $" {phase} trigger ⇒ 两条广播各发一次"
                          + "（原版 `BroadcastTurnBeforeEnd` 与 `BroadcastTurnEnd` 是两趟，都会响）");
                return EmitTurnTriggerNamed(ctx, c, owner, unit, onlyName, phase);
            }

            // ① 棋盘（`allCardsInPlay`）：顺序 = `unitJobs` 的**进场序**
            //    （`ResolveAtTurn` 里那一次 `StableSortByPlayOrder` 是**唯一**排序点，⛔ 别在这里再排）。
            //    递归保护与结算那一趟同一套（回合起止效果自己可能又触发回合效果）——
            //    `ctx.EffectChain` 在这一趟里不会变（`EnqueueAbility` 只入队），但仍照原样判一次。
            //    🆕 **2026-10-25（`E4` · `E4-1`）**：把这条事件**存进作业**（`j.TriggerEvent`）——
            //    结算那一拍要靠它把「那一刻才选出来的目标」事后填进去（`AtTurnUnitJobDeferred`）。
            //    ⚠️ `unitJobs` 存的是**结构体** ⇒ 必须按下标写回（`foreach` 拿到的是副本）。
            if (ctx.EffectChain < BattleContext.MaxEffectChain)
                for (int i = 0; i < unitJobs.Count; i++)
                {
                    var j = unitJobs[i];
                    if (j.Unit == null || j.Unit.Card == null) continue;
                    var evt = EmitOne(j.Unit.Card, j.Owner, j.Unit);
                    if (evt != null) { j.TriggerEvent = evt; unitJobs[i] = j; }
                }

            // ② **当前玩家手牌**（`GetCardsInHandRef(IsPlayerTurn())`）
            //    ⚠️ 手牌那一趟**不存事件**：没有棋盘格位可填（`Target*` 也只能是 `-1`）。
            foreach (var j in handJobs) EmitOne(j.Card, j.Owner, null);

            // ③ **对手手牌**（原版第三个循环）—— **今天这一趟恒空，所以没有循环体**：
            //    `EffectText.SplitAtTurn` 只认 `you` 视角的手牌陷阱（见它的 doc），
            //    而「`your turn`」只可能在**持有者自己**的回合成立 ⇒ 对手手牌在行动方的回合里
            //    一条都不该发；结算层（`handJobs`）也只扫行动方手牌，两层口径一致。
            //    ⚠️ 将来若出现 `each turn` 的手牌陷阱，**这里和结算层要一起改**
            //    （只改一层 = 「播了效果没人结算」，或反之）—— 另有一条账，⛔ 别只在这一层加发。

            // ④ 常驻（我们自己的模型；原版广播没有这一趟，排在最后 —— 见 `ResolveAtTurn` ③ 那段）
            foreach (var j in persistJobs) EmitOne(j.Card, j.Owner, null);
        }

        /// <summary>
        /// **回合起止触发段** —— 规则书「回合结构」第 9 步（开始）/ 第 14 步（结束）。
        ///
        /// 段内顺序 = **原版广播序**（2026-10-24 `D11` 订正）：**棋盘（进场序）→ 当前方手牌 → 常驻**
        /// （判据 → `BattleManagerSupport__BroadcastTurnStart.c:18-31/32-43/44-61`；见方法体里那段更正痕迹）。
        /// **三个触发源**：
        ///   ① **双方棋盘上的 at-turn 单位**（`rule_core.gd` 里叫 `for pi in 2: for slot …`）→
        ///   ② 当前行动方**手牌里的陷阱卡** → ③ 已登记的常驻效果。
        /// ⚠️ **② 原来整层没做**（2026-09-14 A5 批 2 补）：`Chronomancer`（回合结束再起）·
        ///    `Grot Orderly`（回合开始回手）· `Beast Snagga Nob` · `Ghallaron's Champion` ·
        ///    `Konstrictus Tormentor` · `Aquilon Servo-Sentry`（`each turn` 每回合）·
        ///    `Unleashed TramplaSquig`（`each turn`）· `Concealed Explosives`（后缀式）……
        ///    这些单位**自己的**回合起止正文**一条都不会发生**，而且报表上看不出来
        ///    （它们的 desc 解析得出来、载荷也有机制）。
        /// ⚠️ **不许改成随机** —— 对局必须可复现（工程铁律）。
        ///
        /// 为什么先**快照**再结算：结算会改手牌与棋盘（`your troops take 1 damage` 会死人），
        /// 边遍历边改列表是未定义行为。
        /// </summary>
        public static void ResolveAtTurn(BattleContext ctx, string phase)
        {
            if (ctx.IsOver) return;
            int active = ctx.Active;
            var handJobs = new List<AtTurnHandJob>();
            var unitJobs = new List<AtTurnUnitJob>();
            var persistJobs = new List<AtTurnPersistJob>();

            // ① 当前行动方**手牌里**的陷阱卡（被塞进来的破坏卡 —— **持有者**回合生效）
            foreach (var h0 in ctx.Players[active].Hand)   // 第 7 行第 2 步：手牌存实例
            {
                var c = h0.Card;
                if (c == null) continue;
                // 🔴 **2026-10-19（第五十五会话 E10 · `E9` §三·8 的 `A2001` 余账）：单位卡不进这一趟。**
                //    判据 = **卡面语义 + `UnitState` 的在场模型**：这一段是「**躺在持有者手里**的
                //    陷阱卡」通道（`Poisoned Supplies` / `Cult Propaganda`），原版那两个
                //    `PlayerHand` 扫描只收**手牌**；而**单位卡自己的回合起止正文**由下面 ② 那一趟
                //    （棋盘单位）结算 —— 原版 `CardScript.OnTurnStart/OnTurnEnd` 也只在**在场**时跑
                //    （`CardScript__OnTurnStart.c` / `OnTurnEnd.c` 的入口闸 = `IsInPlay`）。
                //    ⛔ **不能只加 `c.IsUnit` 一句就完**：判据只留一处 —— 用 `EffectText.IsHandTrap(c)`
                //    （它内部就是 `c.IsUnit ⇒ false` + `SplitAtTurn`/`SplitHandTrapWhen`，见它的 doc）。
                //    ⚠️ **2026-09-18 那次试过又撤回的**正是「裸加 `if (c.IsUnit) continue;`」——
                //    撤回的原因是**测试**（`TestBeastbossAndPayloadSegments`）靠这条路凑数；
                //    2026-10-19 同批把那条断言改成「棋盘那一趟自己给满 3 份」⇒ 现在可以按原版改。
                //    实据：`SOR70` / `TAU10` 未出场的单位在回合末就结算了自己的 at-turn 句
                //    （`E9_报告.md` §二·3），本笔之后不再发生。
                if (!EffectText.IsHandTrap(c)) continue;
                var at = EffectText.SplitAtTurn(c.Desc);
                if (at == null || at[0] != phase) continue;
                var ops = EffectText.Parse(at[1], out _, out _);
                if (ops == null || ops.Count == 0) continue;
                // 🔴🆕 **2026-10-25（引擎线 `A3` · `A3-1`）：手牌陷阱这条路的正文同样是
                //   `Parse(正文)`（不带 card）⇒ 一样没过 `targetCriteria` 的贴片。
                //   命中的是 `Poisoned Supplies`（`At the end of your turn, your troops take 1 damage`，
                //   唯一一条 trigger 40 的 ability、`ta=[10]`、criteria `friendlyUnits=1 +
                //   includeMinions=1 + includeRemnants=0`）—— 最后那格是**文本层没有的信息**：
                //   原版这一下**不打残骸**。窄闸（单 op + 单 ability + `ta==10`）见那个方法的 doc。
                EffectText.AttachAtTurnBodyCriteria(c, ops, phase);
                foreach (var o in ops)
                    o.Source = c.Name + "：" + at[1];      // 日志要看出「是哪张陷阱干的」
                // 🆕 2026-10-19（`T21` · 缺口 F）：**记下是哪张手牌**（回合起止那一拍要按它的卡名
                //   发事件 —— `Cult Propaganda` / `Poisoned Supplies` 的逐卡动画就挂在这一拍）。
                handJobs.Add(new AtTurnHandJob { Owner = active, Card = c, Ops = ops });
            }

            // ② 🆕 **双方棋盘上的 at-turn 单位**（2026-09-14 A5 批 2）——
            //    判据**查不到**（🔴 `D26`，2026-10-19）：`atturn` 这一族是**从卡面正文解析出来的**，
            //    原版没有文本解析层（理由与搜过的路径/词 → `EffectText.cs` 文件头那段）；
            //    「`each turn` 触发双方 / `your turn` 只触发自己」这半条在原版是**结构化 ability 数据**
            //    里的触发条件，今天没逐卡解出 ⇒ 下面两条目前**只有 gd 旁证**
            //    （原写「判据两条，都照 `rule_core.gd:420-441`」——那份 `.gd` 是**我们自己的复刻**）：
            //      · `view == "each"`（`each|every turn`）⇒ **双方回合都触发**；
            //        `view == "you"` ⇒ **只在控制者自己的回合**触发（`pi != active` 就跳）。
            //      · 扫描顺序 = **玩家索引 0 → 1**、每个玩家**槽位升序**
            //        （那边就是 `for pi in 2: for slot in BOARD_SIZE`）。⚠️ 它上面那句注释写的是
            //        「我方棋盘 → 敌棋盘」，**与代码不一致** —— 我们照**代码**（差异只在
            //        两个 at-turn 效果互相影响时才看得出来，两个都实现为「先 0 后 1」）。
            //    ⚠️ 用**卡面原文**判（`CardDef` 是共享不可变的），不要拿 `TriggerOps` ——
            //       `atturn` 不是触发关键词，它没有那个登记表。
            for (int pi = 0; pi < 2; pi++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pi].Board[s];
                    if (u == null || u.Card == null) continue;
                    // 🔴🆕 **2026-10-24（引擎线 `T03·c` · `C1` §6）：原版这道循环有【三支】，不是两支。**
                    //   判据 = `BattleManagerSupport__BroadcastTurnEnd.c:26-41`（`BroadcastTurnStart` 同形）：
                    //     · `IsInPlay(card)` ⇒ 调 `OnTurnEnd`（正常）；
                    //     · **否则**：`HasCurrentTrait(card, 0x41a /* = 1050 DefinedTrait.remnant */)`
                    //       且 `cardState(+0x228) == 5 (waitingToDie)` ⇒ **也调 `OnTurnEnd`**。
                    //       （trait 号的机器码级证据：`GameAssembly.dll` RVA `0x9B6BE0` → 文件偏移
                    //        `0x9B55E0`，`:0x9B6C95 mov edx, 0x41a` → `:0x9B6C9A call EntityScript$$HasCurrentTrait`。）
                    //   ⇒ 我们这一侧的映射：
                    //     · **在场** = 活着 ∧ 没进将死队列（原版 `IsInPlay` 四档 ≈ 这个）；
                    //     · **第三支** = **不在场 ∧ `IsRemnant` ∧ 已进将死队列**（= `waitingToDie`）。
                    //   ⚠️ 与上面 `IsPendingDeath` **不是同一个概念**（C1 已点名）：`IsPendingDeath` 只是
                    //      「在棋盘上但待死」；第三支还要求 **remnant 且不在场** ⇒ 两个条件**都要**判。
                    bool inPlay = u.IsAlive && !ctx.IsPendingDeath(u);
                    bool waitingRemnant = !inPlay && u.IsRemnant && ctx.IsPendingDeath(u);
                    if (!inPlay && !waitingRemnant) continue;
                    bool notInPlayBranch = !inPlay;
                    foreach (var cl in EffectText.AtTurnClauses(u.Card.Desc))
                    {
                        if (cl.Phase != phase) continue;
                        if (cl.View != "each" && pi != active) continue;
                        var ops = EffectText.Parse(cl.Body, out _, out _);
                        if (ops == null || ops.Count == 0) continue;
                        // 🆕 **2026-10-25（引擎线 `A2` · `A2-1`）：at-turn 片段正文不走
                        //   `Parse(desc, card:)`，`targetCriteria` 那条贴片路没跑过 ⇒ 这里补。
                        //   窄闸（单 ability + 单 op + trigger 与相位一致）见那个方法的 doc；
                        //   `A2` 那笔只做 `forceattack`（全池唯一 `GOF86`）。
                        // 🔴 **2026-10-25（引擎线 `A3` · `A3-1`）**：改成总入口
                        //   `AttachAtTurnBodyCriteria` —— 它再加一支 `deal` / `give`
                        //   （`AM18` / `AM67` / `TAU6` / `TAU7` / `TAU13`；逐卡裁定见
                        //   `AttachAtTurnDealGiveCriteria` 的 doc）。
                        EffectText.AttachAtTurnBodyCriteria(u.Card, ops, phase);
                        unitJobs.Add(new AtTurnUnitJob
                        {
                            Owner = pi, Unit = u, Ops = ops,
                            NotInPlayRemnant = notInPlayBranch,
                        });
                    }
                }

            // ③ 已登记的常驻效果 —— **只有它自己那一方的回合才触发**（`rule_core.gd:432-437`）
            // ⚠️ 只收 `Trigger == "turn"` 的：`Trigger == "deploy"` 那批（部署时给）走
            //    `RuleCore.ResolveDeploy`，**不在这条路上**。虽然它们的 `Phase` 是 `deploy`
            //    天然对不上 `phase`，但显式判一下，免得以后加了新 Phase 名就串味。
            foreach (var pe in ctx.PersistentEffects)
            {
                if (pe.Trigger != "turn") continue;
                if (pe.Owner != active || pe.Phase != phase || pe.Ops == null) continue;
                persistJobs.Add(new AtTurnPersistJob { Owner = pe.Owner, Card = pe.Source, Ops = pe.Ops });
            }

            // 🔴 2026-10-21（引擎线 `T03·c` 批 A · `W59`）：**棋盘那一趟按【进场序】**（双方混排）——
            //   原版 `BroadcastTurnStart` / `BroadcastTurnEnd` 的棋盘趟遍历的是 `allCardsInPlay`
            //   （判据 → `资料/普查产出_1020/W58_战斗结算_多词条顺序判据.md` §二「回合开始/结束」两行 +
            //     §一·A1）⇒ 原来是「玩家索引 0 → 1、每方格号升序」，多单位局面会**与原版不同**。
            //   ⚠️ **结算序**里手牌陷阱仍排在棋盘前面（①②③ 的相对次序是既有口径，
            //   原版「棋盘 vs 手牌」的**结算**先后另有一条账，见 §四·D11，⛔ 别顺手一起改）。
            //   🔴 **但「触发事件」的发射顺序已经不跟它走了**：2026-10-24（`T21④` · `A24`）起
            //   `EmitAtTurnTriggers` 按原版广播序发（棋盘 → 当前玩家手牌 → 常驻），见那个方法的 doc。
            RuleCore.StableSortByPlayOrder(unitJobs, j => j.Unit.PlayOrder);

            int total = handJobs.Count + unitJobs.Count + persistJobs.Count;
            if (total == 0) return;

            string label = "回合" + (phase == "turn_start" ? "开始" : "结束") + "触发";
            ctx.Log($"—— {label}段：{total} 条 ——");
            var unresolved = new List<string>();
            // 🆕 2026-10-19（`T21` · 缺口 F）：这一拍要发 `EvtKind.Trigger`（关键词 = 该卡
            //   `AnimTimes` 里那一个 `Turn*` 名）。判据 = 原版 `CardScript.OnTurnStart/OnTurnEnd`
            //   逐卡跑 body 时走的是 `RawCardScript.OnTrigger(0x1e = 30 / 0x28 = 40 / 0x29 = 41 …)`
            //   （报告 §三 F）。游戏效果本身由下面的 `ResolveOps` 原样结算。
            // 🔴 **2026-10-24（`T21④` · `A24`）：发射从「边遍历作业边发」改成【先整趟发完】再入队。**
            //   原版 = `BroadcastTurnBeforeEnd`（`ResolveEndTurn.c:385`）整趟 → `BroadcastTurnEnd`（`:433`）
            //   整趟，两趟各自按「棋盘（`allCardsInPlay`，进场序）→ 当前玩家手牌 → 对手手牌」遍历。
            //   改前：手牌那一趟排最前、且 40/41 混在同一批 ⇒ 序与原版不同（`T21④` 的病灶）。
            //   发射顺序的判据全文 → `EmitAtTurnTriggers` 的 doc。
            //   ⚠️ 2026-10-24（`D11`）起，下面三趟 foreach 的次序也按原版广播序排（棋盘 → 手牌 → 常驻）。
            EmitAtTurnTriggers(ctx, active, phase, unitJobs, handJobs, persistJobs);

            // 🔴 **2026-10-24（引擎线 `T03·c` · `D11`）：结算顺序改成【棋盘 → 当前方手牌 → 常驻】**
            //   —— 与上面 `EmitAtTurnTriggers` 的**发射序**一致（原版广播三趟就是结算三趟）。
            //   判据 = `BattleManagerSupport__BroadcastTurnStart.c:18-31 / 32-43 / 44-61`：三趟同帧直线代码
            //   ① `allCardsInPlay`（棋盘，只对 `IsInPlay` 的调 `OnTurnStart`）→ ② `GetCardsInHandRef(IsPlayerTurn())`
            //   当前方手牌 → ③ `GetCardsInHandRef(!IsPlayerTurn())` 另一方手牌；
            //   `__BroadcastTurnEnd.c` / `__BroadcastTurnBeforeEnd.c` 逐行同形。
            //   ⚠️ **更正痕迹（铁律 5）**：这一行原来写「结算顺序 = 手牌 → 棋盘 → 常驻
            //   （`rule_core.gd:402` 那句确定性口径；⛔ 别改）」—— `rule_core.gd` 是**我们自己的 Godot 复刻**
            //   （旁证，不是权威），而原版广播序里**棋盘在前**。判别用例 = `Poisoned Supplies`（手牌陷阱）
            //   × `GOF90 Wurrboy`（棋盘、按友军数打伤害）：原版 Wurrboy 先结算、数到的友军包含
            //   「随后被 Poisoned Supplies 打死的那个」；旧的「手牌先」会少算。
            //   ③ 对手手牌那一趟今天恒空（见 `EmitAtTurnTriggers` ③ 的 doc）⇒ 这里没有循环体。
            //   ④ 常驻（我们自己的模型；原版广播没有这一趟）仍排最后。
            foreach (var j in unitJobs)
            {
                // 递归保护与 `RuleCore.FireTriggerAt` 同一套（回合起止效果自己可能又触发回合效果）
                if (ctx.EffectChain >= BattleContext.MaxEffectChain)
                {
                    ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{j.Unit.Name} 的{label}不再连锁");
                    continue;
                }
                // 🔴 2026-10-21（`T03·c` 批 B 接线）：**只入队**（判据同上）。
                //   ⚠️ 施放者 `j.Unit` 在入队这一刻定死 —— 它可能被本轮别的动作打死，
                //   「return to your hand」「heal 1」那类自我指代要的是**触发那一刻的那个单位**。
                var _uj = j;
                EnqueueAbility(ctx, ListenerLane(_uj.Unit.Card), "回合" + phase + ":" + _uj.Unit.Name,
                               () => AtTurnUnitJobDeferred(ctx, _uj, label),
                               // 🔴🆕 2026-10-21（审查 `W61b` 第 3 条）：施放者 = 那个 at-turn 单位。
                               //   原版 `CardScript.OnTurnStart/OnTurnEnd` 的入口闸就是 `IsInPlay`
                               //   （见上面 ① 那段注释）⇒ 出队时它若已离场，这条作废。
                               // 🔴 **2026-10-22（`W63` · C6）：保持 `requireActorInPlay: true`，本轮【不】改数据驱动。**
                               //   原因 = 「回合起止从句 → 原版 `AbilityTrigger` 数值」这条映射**数据确认不了**：
                               //   `EffectText.AtTurnClauses` 只给出 `Phase`（`turn_start` / `turn_end`）与
                               //   `View`（`you` / `each`），而池内**同一措辞落在不同 trigger 号**上 ——
                               //   `start + your` 命中了 `TurnStartPlayer(31)`（`GSC5`/`TL8`/`TL37`/`GOF80`）
                               //   也命中了 `TurnStart(30)`（`TAU6`/`TAU7`/`TAU13`/`SOR66`），
                               //   `end + your` 命中了 `TurnEnd(40)`（`AM18`/`GOF82`/`SOR70`/`TL6`）也命中了
                               //   `TurnBeforeEnd(41)`（`SAU20`）⇒ 按措辞挑一个 = **替原版编口径**。
                               //   ⚠️ 影响面很小但真实：池内 at-turn 牌的 context 只有 `GOF90 Wurrboy` 是 40
                               //   （其余全是 0 = 与今天的 `require:true` 同档），而它的 `TurnEnd(40)` 与
                               //   `Unstable(81)` 两条能力**同卡同值**，分不清哪条对应 desc 那句。
                               //   判据未闭合 ⇒ 按简报 §4·C6「数据确认不了就保持现状」处置，账记进报告。
                               // 🔴🆕 2026-10-24（`C1` §6）：**第三支例外** —— 原版这一支处理的
                               //   就是「**不在场**的待死残骸」，`requireActorInPlay: true` 会在出队时
                               //   把它整条掐掉（它本来就不在场）⇒ 那一条传 `false`。
                               actor: _uj.Unit, requireActorInPlay: !_uj.NotInPlayRemnant);
            }
            foreach (var j in handJobs)
            {
                // 🔴 2026-10-21（`T03·c` 批 B 接线）：**一条 at-turn ability = 一条队列动作**
                //   （原版 `CardScript.OnTurnStart/OnTurnEnd` → `OnTrigger` → `PlayAbility`
                //    → `AddAutoActionToQueue`，判据报告 §4.A 第 2 步 6-11）。
                var _hj = j;
                EnqueueAbility(ctx, ListenerLane(_hj.Card), "回合" + phase + ":" + (_hj.Card != null ? _hj.Card.Name : "?"),
                               () => AtTurnHandJobDeferred(ctx, active, _hj, label));
            }
            foreach (var j in persistJobs)
            {
                // 🔴 2026-10-21（`T03·c` 批 B 接线）：**只入队**（判据同上）。
                var _pj = j;
                EnqueueAbility(ctx, ListenerLane(_pj.Card), "回合" + phase + "常驻:" + (_pj.Card != null ? _pj.Card.Name : "?"),
                               () => AtTurnPersistJobDeferred(ctx, active, _pj, label));
            }
            // 🔴 **2026-09-16：这一段的 `unresolved` 原来也从来没读** —— 见 `ReportUnresolved` 的注释。
            ReportUnresolved(ctx, label + "段", unresolved);
        }

        // ---- 回合起止段的三个**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）----
        //  原同步段的代码逐句搬进来；差别只有 `EffectChain` 的 ++/-- 留在体内（它量嵌套深度）。
        //  ⚠️ `unresolved` 是**本次 `ResolveAtTurn` 共用的一个列表** —— 出队时再往里写，
        //     而 `ReportUnresolved` 在**入队之后**就跑了 ⇒ 队列体里攒下的丢段**不会被报出来**。
        //     ⛔ 不许静默：每个队列体自己报（下面三处都调 `ReportUnresolved`）。
        static void AtTurnHandJobDeferred(BattleContext ctx, int active, AtTurnHandJob j, string label)
        {
            // 🆕 2026-10-20（`W57FIX2` 续 · 审计 P3 旁路）：手牌陷阱的能力属主 = **那张手牌**，
            //   不是「没有来源卡」（判据链 → `BattleContext.FactionSourceCard`）。
            var mine = new List<string>();
            var prevFactionSrc = ctx.FactionSourceCard;
            ctx.FactionSourceCard = j.Card;
            try
            {
                foreach (var o in j.Ops) ResolveOne(ctx, active, null, label, o, null, mine, card: j.Card);
            }
            finally { ctx.FactionSourceCard = prevFactionSrc; }
            ReportUnresolved(ctx, label + "段（" + (j.Card != null ? j.Card.Name : "?") + "）", mine);
        }

        static void AtTurnUnitJobDeferred(BattleContext ctx, AtTurnUnitJob j, string label)
        {
            // 递归保护与 `RuleCore.FireTriggerAt` 同一套（回合起止效果自己可能又触发回合效果）
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{j.Unit.Name} 的{label}不再连锁");
                return;
            }
            ctx.EffectChain++;
            try
            {
                // 🔴🆕 **2026-10-25（`E4` · `E4-1`）：这一条回合触发发出去的那条 `EvtKind.Trigger`**
                //   要先「arm」起来 —— 结算这一拍第一次落到目标时由 `ResolveTargets` 的收口**当场写进事件**
                //   （`BattleContext.Signals` 存的是同一个对象引用，表现层 `DrainSignals` 之后才读
                //   ⇒ 一定看得到；落格号也必须**当场**取 —— 目标常常被这条效果打死）。
                //   ⚠️ 保存/还原是必须的：效果会嵌套（`Repeat this effect` / 连锁触发），
                //   和外层 `LastTargets` 那套同一个理由。
                var prevSink = ctx.TriggerTargetSink;
                var prevDone = ctx.TriggerTargetSinkDone;
                ctx.TriggerTargetSink = j.TriggerEvent;
                ctx.TriggerTargetSinkDone = false;
                try
                {
                    // 施放者传**这个单位** —— 「return to your hand」「heal 1」那种自我指代才认得对
                    ResolveOps(ctx, j.Owner, j.Unit, j.Ops, label);
                }
                finally
                {
                    ctx.TriggerTargetSink = prevSink;
                    ctx.TriggerTargetSinkDone = prevDone;
                }
            }
            finally { ctx.EffectChain--; }
        }

        static void AtTurnPersistJobDeferred(BattleContext ctx, int active, AtTurnPersistJob j, string label)
        {
            // 🆕 2026-10-20（`W57FIX2` 续 · 审计 P3 旁路）：回合型常驻效果的**能力属主** =
            //   写出这条效果的那张卡（`j.Card` = `PersistentEffect.Source`），
            //   ⛔ 不是「没有来源卡」（判据链 → `BattleContext.FactionSourceCard`）。
            var mine = new List<string>();
            var prevFactionSrc = ctx.FactionSourceCard;
            ctx.FactionSourceCard = j.Card;
            try
            {
                foreach (var o in j.Ops) ResolveOne(ctx, active, null, label, o, null, mine, card: j.Card);
            }
            finally { ctx.FactionSourceCard = prevFactionSrc; }
            ReportUnresolved(ctx, label + "段（" + (j.Card != null ? j.Card.Name : "?") + "）", mine);
        }

        /// <summary>回合起止段里「棋盘单位」那一类作业（要带上**施放者**，代词/自我指代才认得对）。</summary>
        struct AtTurnUnitJob
        {
            public int Owner;
            public UnitState Unit;
            public List<EffectOp> Ops;
            /// <summary>🔴🆕 **2026-10-25（`E4` · `E4-1`）：广播那一趟为这张卡发出的那条 `EvtKind.Trigger`**
            /// （`null` = 这一趟没发）。结算体靠它把「第一个落到的目标」事后填进去 ——
            /// 判据与机制见 <see cref="BattleContext.TriggerTargetSink"/>。
            /// ⚠️ `turn_end` 那一趟会**跑两遍**（`TurnBeforeEnd` → `TurnEnd`）：同一张卡两遍都发得出来时，
            /// 这里留**后一遍**那条；现池**没有一张卡两个名字都有**（18 张 `Turn*` 卡各占一个名字）。</summary>
            public BattleEvent TriggerEvent;
            /// <summary>🔴🆕 **2026-10-24（引擎线 `T03·c` · `C1` §6）：这一条走的是原版
            /// `BroadcastTurnEnd` 棋盘趟的**第三支**（`!IsInPlay ∧ remnant(1050) ∧ cardState==5 waitingToDie`），
            /// 不是正常的「在场单位」那一支。
            /// ⇒ 出队时**不许**按 `requireActorInPlay` 把它掐掉（它本来就「不在场」）。
            /// ⚠️ 今天池内不可达（没有残骸带 `TurnEnd(40)`/`TurnBeforeEnd(41)`），由合成夹具断言。</summary>
            public bool NotInPlayRemnant;
        }

        /// <summary>🆕 2026-10-19（`T21` · 缺口 F）：回合起止段里「**手牌陷阱**」那一类作业 ——
        /// 带 `Card` 是因为那一拍要按**这张手牌的卡名**发 `EvtKind.Trigger`
        /// （`Cult Propaganda` / `Poisoned Supplies` 的 `TurnEnd` 动画行）。</summary>
        struct AtTurnHandJob
        {
            public int Owner;
            public CardDef Card;
            public List<EffectOp> Ops;
        }

        /// <summary>🆕 2026-10-19（`T21` · 缺口 F）：回合起止段里「**已登记的常驻效果**」那一类作业 ——
        /// `Card` = 打出它的那张卡（`PersistentEffect.Source`），事件按它卡名发。</summary>
        struct AtTurnPersistJob
        {
            public int Owner;
            public CardDef Card;
            public List<EffectOp> Ops;
        }

        /// <summary>
        /// **激活这张卡的「灵魂石能力」** —— 卡面 `N [Spirit Stone]: …` 那一句。
        ///
        /// 🔴 **这就是那一族缺的触发点。** 不做这一步，那 28 张卡的付费句「解析得出来、载荷也有机制」
        ///    但**永远不会发生**，而且**报表看不见它**（判据是「解析得出 + 有机制 + **没有触发点**」
        ///    —— 见 `资料/单位卡desc与光环_批次划分.md` §一⑦）。和 A7 的光环是同一个坑。
        ///
        /// **时机**：单位卡在**部署时**（<see cref="RuleCore.PlayCard"/>，排在 `Rally` **之前** ——
        /// 这样 Rally 结算时看得见刚给出的关键词，和 `ResolveDeploy` 是同一个理由）。
        /// 战术卡 / 天赋卡**不走这里** —— `PlayTactic` 解析整条 `desc` 时就会结算那一条。
        /// 原版出处：`CardScript.CanUseSpiritStone` 在 dump 里唯一的调用点是打出牌协程
        /// （`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:719-731`）。
        /// ✅ **2026-10-07 加固（A120 / A121）**：反编译里 **600 的发起源只有两处** ——
        /// 「打出一张带目标的牌」（`RawCardScript__OnCardPlayedWithTarget.c:66`）与 77 号动作
        /// （`CardScript__TriggerSpiritStone.c:16-18`），而**原版扣石就在那个 600 分支里**
        /// （`RawCardScript__TriggerAbility.c:42-53`：`thisCard == cardPlayed` 时
        /// `UseMana(pm, ability+0x20, 5 /*ManaType.SpiritStone*/, 0, 0)`，**全库唯一一处**）
        /// ⇒ **正面支持「打出时」**。⚠️ **口径来源 = 用户口径（2026-09-19）＋ 反编译有据；
        /// 【实况未核】**（原版已关服 + 手牌注入失败，本地核不了）。
        /// 判据全文 → `资料/普查产出_1007/波7判据核查.md` §A120 / §A121。
        /// ⚠️ **别和 `useWaystone`（76）混了** —— 那是「**收集**」，见 `CardDef.SpiritOps` 的注释。
        ///
        /// **付费与「不够」怎么报**：钱由 `ResolveOne` 的付费段按 `op.Cost` / `op.CostKind`
        /// 从 `ps.SpiritStones` 扣（货币名判据是 `EffectText.CostKindOf`，全仓唯一）；
        /// **不够时它自己会记日志 + 记 `unresolved`**（「需要 N 点灵魂石才激活，不够 —— 这条没生效」），
        /// **不静默**。🔴 **2026-10-12（`A1412`）：收钱的时机抬到了 `ResolveOps` 顶端** ——
        /// 这一条 ability 的付费前缀在**它动手之前**按组收一次（`ChargePaidGroups`），
        /// 收不到就**整组不结算**（原版「付不起 = 整条 ability 不激活」）。
        /// ✅ **2026-09-25 更正：这一段原来写着「我们比原版少一步『选择』⇒ 这是近似」—— 那句已作废。**
        ///    **「够就自动付」与原版一致**：用户 2026-09-19 给的权威口径是「**达到要求就强制扣除；
        ///    不够则不扣、增强效果也不发动**」；反编译也支持 —— `BattleManager__CanUseWaystone.c` 里
        ///    **既不查灵魂石余额、也不问你付不付**（那两条判定在**另一条链** `CanUseSpiritStone` 上，
        ///    两条互不调用）。判据全文 → `资料/查证_useWaystone_语义.md` §一 结论 5/§三。
        /// </summary>
        public static void ResolveSpiritAbility(BattleContext ctx, int owner, UnitState unit)
        {
            if (ctx == null || unit == null || unit.Card == null || ctx.IsOver) return;
            var ops = unit.Card.SpiritOps;
            if (ops == null || ops.Count == 0) return;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{unit.Name} 的灵魂石能力不再结算");
                return;
            }
            // 🔴 2026-10-21（`T03·c` 批 B 接线）：**灵魂石能力 = 一条队列动作**
            //   （原版 `RawScript.TriggerAbility` 的 600 支 → `PlayAbility` → `Add*Action`
            //    → `AddAutoActionToQueue`；道 = 数据列 `triggerPriority["spiritstone"]`，默认 1）。
            //   ⚠️ `activated` 的消费者（`EmitAbilityUseTrigger`）一起搬进队列体。
            // 🆕 **2026-10-22（`W63` · C3）**：`UseSpiritStone(600)` 的 `abilityContext` **逐卡判**
            //   （池内现读 `{0:16, 50:13}`，`cards_raw.json`）——
            //   `0 Play` = 必须活着在场 · `50` = 在场或墓地（**不含待死**）· `-1` 未知 ⇒ 回退旧口径。
            //   判据 → `CardAbility__IsRightContext.c`；取值口 → `AbilityContextOfAbilityV14` 的 doc。
            int actx = AbilityContextOfAbilityV14(unit.Card, "UseSpiritStone");
            EnqueueAbility(ctx, TriggerLane(unit, "spiritstone"), "灵魂石:" + unit.Name,
                           () => SpiritAbilityDeferred(ctx, owner, unit, ops),
                           // 🔴🆕 2026-10-21（审查 `W61b` 第 3 条）就地订正（2026-10-22 `W63` · C3）：
                           //   `requireActorInPlay: false` 现在**只当 `-1` 的回退档**（= 改前的「不判」）——
                           //   已知 context 时由泵点按 `QueuedAction.AbilityContext` 判（context 更准，
                           //   见 `EnqueueAbility` 的 `abilityContext` 参数 doc）。
                           actor: unit, requireActorInPlay: false, abilityContext: actx);
            return;
        }

        /// <summary><see cref="ResolveSpiritAbility"/> 的**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）。</summary>
        static void SpiritAbilityDeferred(BattleContext ctx, int owner, UnitState unit,
                                          IReadOnlyList<EffectOp> ops)
        {
            ctx.EffectChain++;
            bool activated = ResolveOps(ctx, owner, unit, ops, "灵魂石能力");
            ctx.EffectChain--;
            // 🆕 2026-10-19（第五十七会话 `W57` · T21 D）：**能力激活了** ⇒ 按这张卡自己的动画行
            //    发 `UseSpiritStone(600)`（闸 = `CardDef.HasAnimTrigger`，见 `EmitAbilityUseTrigger`）。
            //    ⚠️ 只在 `activated` 时发 —— 余额不够时原版整条不激活（`CanUseSpiritStone` 先判余额）。
            if (activated) EmitAbilityUseTrigger(ctx, unit.Card, owner, unit, TN_UseSpiritStone, 600);
            if (ctx.IsOver) return;
            // 这一族里 `Deploy a Wraithguard` / `Create a copy …` 会**改棋盘** ⇒ 光环要重算
            // （和别处 9 个棋盘写入点同一条纪律，见 `Core/Aura.cs` 的 `Recompose` 注释）
            Auras.Recompose(ctx);
        }

        /// <summary>
        /// **触发一个灵魂石能力、但不付费**（`ASH52 Cosmic Serpent`）—— 🆕 2026-09-16。
        ///
        /// 与 <see cref="ResolveSpiritAbility"/> 是**同一份正文**（`CardDef.SpiritOps`）、
        /// 同一条结算路径，**唯一的差别是「不查余额、不扣石」** —— 判据与出处逐条写在
        /// `BattleContext.SuppressCostDepth` 的注释里（原版触发路径没有付费调用）。
        ///
        /// ⚠️ 为什么单开一个入口而不是加个 `bool` 参数：`ResolveSpiritAbility` 是**部署时**那条路
        ///    （`RuleCore.PlayCard` → 玩家主动激活、该付费），两处的**判据不同源**；
        ///    用一个参数会让调用点看不出自己在走哪条语义。
        /// ⚠️ 正文一条都没有（`SpiritOps` 空）时**什么都不做** —— 调用点
        ///    （`DoTriggerAbility` 的 `SpiritStone` 分支）会照旧记 `unresolved`，不静默。
        /// </summary>
        public static void ResolveSpiritAbilityForced(BattleContext ctx, int owner, UnitState unit)
        {
            if (ctx == null || unit == null || unit.Card == null || ctx.IsOver) return;
            if (unit.Card.SpiritOps == null || unit.Card.SpiritOps.Count == 0) return;
            // 🔴 2026-10-21（`T03·c` 批 B 接线）：**免付那一档必须跟队列动作一起走** ——
            //   `SuppressCostDepth` 若留在调用方，入队后立刻会 `--` 回来，队列体跑的时候
            //   就变成「照常收费」了（静默多扣钱）。⇒ 整段包进队列体。
            var _u = unit;
            EnqueueAbility(ctx, TriggerLane(unit, "spiritstone"), "灵魂石(免付):" + unit.Name,
                           () =>
                           {
                               ctx.SuppressCostDepth++;
                               try { SpiritAbilityDeferred(ctx, owner, _u, _u.Card.SpiritOps); }
                               finally { ctx.SuppressCostDepth--; }
                           },
                           // 🔴🆕 2026-10-21（审查 `W61b` 第 3 条）：同上（免付那一档走同一个能力）——
                           //   `UseSpiritStone(600)` 的 context 混着 `50`（在墓地也算）⇒ **不判在场**。
                           actor: _u, requireActorInPlay: false);
        }

        /// <summary>
        /// **激活誓约能力**（卡面 `Oath N: 正文`）—— 🆕 2026-09-16。
        ///
        /// 与 <see cref="ResolveSpiritAbility"/> 同形（同一套 `ResolveOps`），三处不同：
        ///   · 正文来自 `CardDef.OathOps`（不是 `SpiritOps`）；
        ///   · **付费由 `ResolveOneCore` 开头那段自己做**（`CostKind = "oath"` ⇒ 扣**能量**；
        ///     灵魂石那条扣的是石头）—— 这里**不另写付费**，避免两处各写一份。
        ///     🔴 **2026-10-12（`A1412`）：收钱的**时机**抬到了 `ResolveOps` 顶端**
        ///     （`ChargePaidGroups` → 仍走 `ResolveOneCore` 那一个付费支）⇒ **本函数下方那三遍
        ///     里只有第 1 遍真收钱**（后两遍在 `SuppressCostDepth` 下 = 免付），
        ///     而「这一条 ability 收几次」由**这一次 `ResolveOps` 调用**的组数决定；
        ///   · **多结算几次**：`RuleCore.OathExtraReplays` = 同方场上带 `oathDouble` 的牌数
        ///     （原版 `BattleManager__IsThereDoubleOathEffect.c:33` 数牌数、
        ///      `CardScript__ResolveActiveAbilityPlayed.c:66-72` 基础 1 次 + 再来 N 次）。
        ///
        /// **返回「这次激活有没有生效」**（`RuleCore.UseOathAbility` 靠它决定要不要记次数）：
        /// 判据 = **能量被扣掉了**（付费失败时 `ResolveOneCore` 会打日志并整条不结算，
        /// 那种情况**不该消耗这次激活**）。`OathCost == 0` 的卡（理论上没有）恒算成功。
        /// </summary>
        public static bool ResolveOathAbility(BattleContext ctx, int owner, UnitState unit)
        {
            if (ctx == null || unit == null || unit.Card == null || ctx.IsOver) return false;
            var ops = unit.Card.OathOps;
            if (ops == null || ops.Count == 0) return false;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{unit.Name} 的誓约能力不再结算");
                return false;
            }

            int replays = RuleCore.OathExtraReplays(ctx, owner);
            int energyBefore = ctx.Players[owner].Energy;

            ctx.EffectChain++;
            for (int i = 0; i <= replays; i++)
            {
                // 🔴 **只有第一遍付费**（2026-09-16 实测踩到）：`Oath N:` 的 N 是**挂在 op 上的
                //    付费前缀**（`op.Cost`），而每一次重放都会再走一遍 `ResolveOneCore` 的付费分支
                //    ⇒ 不抑制的话「多结算一次」会**再扣一次费**（自检当场抓到：`oathDouble` 那条
                //    期望 5→4、实得 5→3）。原版是**激活时付一次**（`BattleManager__PayActiveAbilityCostOath`
                //    在激活那一步调，重放走 `RawCardScript__ResolveActiveAbility`、不再付费）。
                if (i > 0)
                {
                    ctx.Log($"（誓约：`oathDouble` 让这次激活**多结算一次** —— 第 {i + 1} 遍，**不再收费**）");
                    ctx.SuppressCostDepth++;
                }
                try
                {
                    // ⚠️ 用**收了 `IReadOnlyList` 的那个重载**（和 `ResolveSpiritAbility` 同一份）——
                    //    `CardDef.OathOps` 是 `IReadOnlyList`，另一个重载只吃 `List`。
                    //    `seed = unit`：正文里的 `this troop` / `it` 指的就是这张牌自己。
                    ResolveOps(ctx, owner, unit, ops, "誓约能力", unit);
                }
                finally { if (i > 0) ctx.SuppressCostDepth--; }
                if (ctx.IsOver) break;
            }
            ctx.EffectChain--;

            // 棋盘可能被改过（`Oath 1: Deal 1 damage` 会死人、`Gain Camouflage` 只是加关键词）——
            // 光环重算和别处 9 个棋盘写入点同一条纪律（`Core/Aura.cs` 的 `Recompose` 注释）。
            if (!ctx.IsOver) Auras.Recompose(ctx);

            bool paid = unit.Card.OathCost <= 0 || ctx.Players[owner].Energy < energyBefore;
            if (!paid)
                ctx.Log($"{unit.Name} 的誓约能力**没有生效**（见上面那行原因）——次数不消耗");
            return paid;
        }

        /// <summary>
        /// **结算信仰能力**（卡面 `N ☀: 正文` / `N [faith]: 正文`）—— 🆕 2026-10-11（`A1452` ＋ `A1453`）。
        ///
        /// 与 <see cref="ResolveSpiritAbility"/> **同形**（同一套 `ResolveOps`、同一处 `EffectChain`
        /// 记账、同一条 `Auras.Recompose` 收尾），正文来自 <see cref="CardDef.FaithOps"/>。
        ///
        /// 🔴 **与灵魂石那一支的两处差别**：
        ///   · **N 不在这个函数里判** —— 门槛（`faith &gt;= N`）由 `ResolveOneCore` 的付费支
        ///     借 `CostPrefixPayable` 判（判据**只此一处**，⛔ 别在这儿再写一遍余额比较）；
        ///     🔴 **2026-10-12（`A1412`）：判的时机抬到了 `ResolveOps` 顶端、按【组】判一次**
        ///     （`ChargePaidGroups`）⇒ 从前是「够的那几条照常结算、不够的那几条**各自**打一行
        ///     日志并不结算」，现在**同一份前缀整组一个结论**：
        ///     够 ⇒ 组内 op 都结算（组首那行「阈值判定通过」＋ 组内其余各一行「门槛不再判一次」）；
        ///     不够 ⇒ **整组一条都不结算**（组首那行「要信仰达到 N…不够 —— 这一条没生效」
        ///     ＋ 一行「同属它的其余效果一并跳过」）。**仍然不是静默**。
        ///     ⚠️ **逐条门槛这件事本身没变**：`SOR27`/`SOR68`（`2 ☀:` ＋ `4 ☀:`）是**两组**，
        ///     各判各的 —— 一批合一就会静默答错另一半（见 `ChargePaidGroups` 的 doc）。
        ///   · **一分信仰都不扣**（`A1453`）—— 理由与全部反编译出处写在 `ResolveOneCore`
        ///     付费支那段注释里。
        ///
        /// **谁调它**（两处，都是「一张牌进场上」那一刻）：
        ///   · `RuleCore.PlayCard` 的部队入场那一段（**从手牌打出**，和 `ResolveSpiritAbility` 相邻）；
        ///   · `RuleCore.DeployFree`（**效果免费部署 / 召唤 / 再造**）。
        ///   原版的发起源是 `CardScript__ResolveUnitSummoned.c:51` —— 它盯的是**入场**这件事、
        ///   **不管是哪种方式进来的** ⇒ 两条入口都要点火。逐条见 <see cref="CardDef.FaithOps"/> 的 doc。
        ///   ⚠️ **对照 `Rally` / 灵魂石**：那两条**只在「从手牌打出」那条路上**发
        ///   （`ResolveUnitSummoned` 里**没有** 600）⇒ `DeployFree` **只加信仰、不加它们**（故意的）。
        ///
        /// ⚠️ **没有「本回合部署」「每回合 1 次」这类次数限制** —— 那是 `CanUseOathAbility`
        ///    （`CardScript__CanUseOathAbility.c:8` / `:16-20`）**誓约专属**的闸，604/605 那条路没有。
        /// ⚠️ **一次部署只走一次**（本单位带着它上场的那一刻）—— 不是每回合重复触发。
        /// </summary>
        public static void ResolveFaithAbility(BattleContext ctx, int owner, UnitState unit)
        {
            if (ctx == null || unit == null || unit.Card == null || ctx.IsOver) return;
            var ops = unit.Card.FaithOps;
            if (ops == null || ops.Count == 0) return;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{unit.Name} 的信仰能力不再结算");
                return;
            }
            // 🔴 2026-10-21（`T03·c` 批 B 接线）：**信仰能力 = 一条队列动作**（判据同灵魂石）。
            //   ⚠️ `activated` / `faithGroups` 的消费者（`EmitFaithAbilityUseTriggers`）一起搬进队列体。
            var _ops = ops;
            // 🆕 **2026-10-22（`W63` · C3）**：`UseFaith(605)` 的 `abilityContext` **逐卡判**
            //   （池内现读 `{0:14, 50:15}`）—— `0 Play` = 必须活着在场 · `50` = 在场或墓地
            //   （**不含待死**）· `-1` 未知 ⇒ 回退旧口径。取值口 → `AbilityContextOfAbilityV14` 的 doc。
            int fctx = AbilityContextOfAbilityV14(unit.Card, "UseFaith");
            EnqueueAbility(ctx, ListenerLane(unit.Card, TN_UseFaith), "信仰:" + unit.Name,
                           () => FaithAbilityDeferred(ctx, owner, unit, _ops),
                           // 🔴🆕 2026-10-21（审查 `W61b` 第 3 条）就地订正（2026-10-22 `W63` · C3）：
                           //   `requireActorInPlay: false` 现在**只当 `-1` 的回退档**（= 改前的「不判」）——
                           //   已知 context 时由泵点按 `QueuedAction.AbilityContext` 判。
                           actor: unit, requireActorInPlay: false, abilityContext: fctx);
            return;
        }

        /// <summary><see cref="ResolveFaithAbility"/> 的**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）。</summary>
        static void FaithAbilityDeferred(BattleContext ctx, int owner, UnitState unit,
                                         IReadOnlyList<EffectOp> ops)
        {
            ctx.EffectChain++;
            // `seed = unit`：正文里的 `this troop` / `it` 指的就是这张牌自己（同誓约那一支）
            // 🆕 2026-10-20（`FACTION-DOUBLE605`）：`activatedGroups` 接住「哪几个信仰付费组过了门槛」
            //   —— 原版同一张卡的两条 605 能力是**各自判定、各演一次**（SOR56/SOR23/SOR67）。
            var faithGroups = new List<EffectOp>();
            bool activated = ResolveOps(ctx, owner, unit, ops, "信仰能力", unit, faithGroups);
            ctx.EffectChain--;
            // 🆕 2026-10-19（第五十七会话 `W57` · T21 C）：**能力激活了**（门槛 `faith >= N` 过了）⇒
            //    按这张卡自己的动画行发 `UseFaith(605)`（闸 = `CardDef.HasAnimTrigger`）。
            //    ⚠️ 门槛没过时 `ResolveOps` 的付费组整组拦下 ⇒ `activated == false` ⇒ 这一拍不发，
            //    与原版「`CanTriggerAbility` 不成立 = ability 不激活」一致。
            // 🆕 2026-10-20（`FACTION-DOUBLE605`）：多组信仰能力的卡**每组发一条**（带 `AbilityIndex`），
            //    单组卡走的还是原来那一条（见 `EmitFaithAbilityUseTriggers` 的 doc）。
            if (activated) EmitFaithAbilityUseTriggers(ctx, unit.Card, owner, unit, faithGroups);
            if (ctx.IsOver) return;
            // 这一族里 `Deploy a …` / `Create a copy …` 会**改棋盘** ⇒ 光环要重算
            // （和别处 9 个棋盘写入点同一条纪律，见 `Core/Aura.cs` 的 `Recompose` 注释）
            Auras.Recompose(ctx);
        }

        /// <summary>
        /// `extratrigger` —— **「刚才那个机制的触发再来一次」的额度**（🆕 2026-09-16）。
        ///
        /// 卡面两句（都在**事件层**的正文位置）：`When a friendly unit triggers Mob, it triggers an
        /// additional time`（`GOF_Big_Choppa_Nob`）· `When this unit triggers Synapse, it applies the
        /// effect twice`（`TL30 Broodlord`）。`Payload` = **哪个机制**，由 `CardDef.AddWhenTrigger`
        /// 从事件种类里填（正文那半句自己看不出来）。
        ///
        /// 这个 op **只挂额度**，真正的「再来一次」由各机制**自己消费**：
        ///   · Mob —— `RuleCore.DeclareAttack` 的近战那一段
        ///   · Synapse —— `RepeatTacticOnAdjacent` 的广播之后
        /// 目标 = **事件主语**（`it`）= 那个正在触发机制的单位。
        ///
        /// ⚠️ **重入守卫**：那一次额外触发**自己会再广播一遍同一件事** ⇒ 不加守卫就会重新挂上额度，
        ///    下一次攻击白捡一次（静默）。`ctx.ExtraTriggerDepth > 0` 时只记日志、**不挂**。
        /// ⚠️ `Payload` 为空 = 解析时没能确定机制（两条事件共用一份 ops 那种情况）⇒ **如实报**，不猜。
        /// </summary>
        static bool DoExtraTrigger(BattleContext ctx, int owner, string by, EffectOp op,
                                   UnitState chosen, List<string> unresolved)
        {
            if (string.IsNullOrEmpty(op.Payload))
            {
                ctx.Log($"{by}：「{op.Source}」说要「再触发一次」，但**看不出是哪个机制** —— 这条没生效");
                unresolved.Add(op.Source + "（extratrigger 没有机制名）");
                return false;
            }
            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                if (ctx.ExtraTriggerDepth > 0)
                {
                    // 我们正跑着的那一次**就是**额外触发，它自己又广播了一遍 ⇒ 不能再挂一次
                    ctx.Log($"（{t.Name} 的 `{op.Payload}` 额外触发正在跑 —— 这一条不重复记账）");
                    continue;
                }
                int cur;
                t.ExtraTriggers.TryGetValue(op.Payload, out cur);
                t.ExtraTriggers[op.Payload] = cur + 1;
                n++;
                ctx.Log($"{t.Name}：**{op.Payload} 的触发会再来一次**（「{op.Source}」）");
            }
            if (n == 0 && ctx.ExtraTriggerDepth == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要标记「再触发一次」，但没有指到任何单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（extratrigger 没有目标）");
                return false;
            }
            return true;
        }

        /// <summary>
        /// `takecontrol` —— **把敌方一个部队抢过来**（🆕 2026-09-16）。全池只有
        /// `GSC_Telephatic_Domination` 的 `Take control of an enemy troop this turn and give it Fast`。
        ///
        /// 「抢」= **把这个 `UnitState` 从对手的 `Board[]` 挪到我的 `Board[]`**
        /// （归属在我们这儿就是「待在谁的数组里」；原版是一个可翻转的 bool `+0x40`，
        /// 见 `BattleContext.TempControl`）。归还由 `RuleCore.EndTurn` 那一段做。
        ///
        /// 四处要说明的地方：
        ///   · **落点**照原版 `GetNextSlotWithoutDisplacing`（本仓 = `BoardSlots.NextWithoutDisplacing`）——
        ///     **人少的那一侧的最外一格（平手走右）**；**满场返回 −1 ⇒ 抢不过来**，
        ///     如实打日志 + 记 `unresolved`。
        ///     ⚠️ **2026-10-05 订正**：这里原来写「**落点**是我们挑的 …… 落到**己方第一个空格**」——
        ///     正文 **2026-10-01 就已改成原版那一条**（见下面 `int to = …` 上面那三行注释），
        ///     只有本 summary 忘了跟。
        ///   · **入场那一刻【无条件】置召唤病**（= `RuleCore.ResetSummonSickness(t)`；
        ///     2026-10-18 `A1093` 之前写的是 `t.Exhausted = true`）。判据 = 原版
        ///     `BattleManager._ResolveStealMinion_d__551__MoveNext.c:119`
        ///     `CardScript__ResetSummonSickness(card)` —— 它**紧跟** `:81-82 MoveMinionIntoSlot`
        ///     （= 移动**成功**那一刻）、**不看任何关键词**（函数体 `:8` 把 `card+0x58` 写成 **1**，
        ///     而 `+0x58` 就是 `summonSickness`，见 `CardScript__ResetSummonSickness.c`；
        ///     同一方法 `:21` 再调 `ActivateMinion` 按新值重算能不能动）
        ///     ⇒ 卡面 `and give it Fast` 之所以要写出来，正是「**抢来必有召唤病**」。
        ///     ⚠️ 这就是「转移归属 ⇒ 重新入场 ⇒ 召唤病」那条语义的一半（另一半 = 回合末
        ///     **归还**时也写**同一位**，见 `RuleCore.EndTurn` 的归还段 —— 两处**共用同一个调用**）。
        ///     ⚠️ 「无条件」只管**成功入场**这一支：`IsAvailableSlot` 为假那支
        ///     （`:35` → `:41 LogWarning` → `:107 FinishResolvingAction`）**连移动都不走**、
        ///     自然也不置病。
        ///   · **`Fast` 要显式清 `Exhausted`**：`AddKeyword("fast")` 只写关键词，
        ///     而 `Exhausted` **只在构造时**按关键词算过一次（见 `UnitState.AddKeyword`）
        ///     ⇒ 不显式清的话「给予它迅捷」会**给个关键词却动不了**（静默）。
        ///   · **棋盘动过就要 `Auras.Recompose`** —— 两侧的棋盘都变了（别处 9 个写入点同一条纪律）。
        /// </summary>
        static bool DoTakeControl(BattleContext ctx, int owner, string by, EffectOp op,
                                  UnitState chosen, List<string> unresolved)
        {
            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」没有合法目标，空过");
                return true;                                   // 没目标 = 正常空过（和 `DoDeal` 同一条口径）
            }
            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                int fromP, fromSlot;
                if (!FindSlot(ctx, t, out fromP, out fromSlot)) continue;   // 不在场上的抢不了
                if (fromP == owner)
                {
                    ctx.Log($"（{t.Name} 本来就是你的人 —— 跳过）");
                    continue;
                }
                // 🔴 2026-10-01：落点改成原版那一条 —— 抢来的单位走 `GetNextSlotWithoutDisplacing`
                //    （人少的一侧的最外一格、平手走右），出处 = `BattleManager._ResolveStealMinion_d__551__MoveNext.c:79`
                //    （`SummonMinion/MoveMinionIntoSlot` 的实参就是它）。原来写的是「从 0 号格起第一个空格」。
                // 🔴 2026-10-21（引擎线 `T02` 同族 · `A1692`）：落点与「免费部署」**同一条判据**
                //    （`BoardSlots.NextFreeWithoutDisplacing`）—— 有洞时那一格可能已经有人，
                //    ⛔ **不许静默覆盖**（原版没有这一档：两侧是连续 `List`、不做占用检查）。
                int to = BoardSlots.NextFreeWithoutDisplacing(ctx.Players[owner], out int blocked);
                if (to < 0)
                {
                    if (blocked >= 0)
                        ctx.Log($"{by}：落点 {blocked} 被 {ctx.Players[owner].Board[blocked].Name} 占着"
                              + $"（棋盘不连续）、该侧也没有空格 —— 「{op.Source}」抢不过来（{t.Name} 留在对面）");
                    else
                        ctx.Log($"{by}：你的部署位满了 —— 「{op.Source}」抢不过来（{t.Name} 留在对面）");
                    unresolved.Add(op.Source + "（己方没有空格放抢来的单位）");
                    continue;
                }
                if (blocked >= 0)
                    ctx.Log($"{by}：落点 {blocked} 已被 {ctx.Players[owner].Board[blocked].Name} 占着"
                          + $"（棋盘不连续）—— **不覆盖**，抢来的 {t.Name} 改放该侧最外的空格 {to}");

                // 从原主那边摘掉 = 原版 `RemoveMinion` ⇒ **要补位**（外侧的人整体内移一格）
                BoardSlots.RemoveAt(ctx.Players[fromP], fromSlot);
                ctx.Players[owner].Board[to] = t;
                ctx.TempControls.Add(new BattleContext.TempControl
                {
                    Unit = t, Owner = fromP, Turn = ctx.Turn, Slot = fromSlot,
                });
                Auras.Recompose(ctx);      // 两侧棋盘都变了 ⇒ 光环重算

                // ============ 入场两步：① 无条件置召唤病 → ② 尾句 `give it Fast` 再清掉 ============
                //
                // ① 🔴 **2026-10-08（`D28` 施工单 B）：入场那一刻【无条件】置召唤病。**
                //    判据 = 原版 `BattleManager._ResolveStealMinion_d__551__MoveNext.c:119`
                //    `CardScript__ResetSummonSickness(card)` —— 它**紧跟** `:81-82 MoveMinionIntoSlot`
                //    （= 移动成功那一刻），**没有任何条件**：那个函数体把 `card+0x58` 写成 **1**
                //    （`CardScript__ResetSummonSickness.c`），而 `+0x58` 就是 `summonSickness`
                //    （`EntityScript.get/set_summonSickness`），`CardScript__OnTurnStart.c:85`
                //    在回合开始把同一格写 0（= 解疲劳）⇒ **1 = 有召唤病 = 不能动**。
                //    ⚠️ **位置不能提前**：`IsAvailableSlot` 为假那支（`:35` → `:41 LogWarning`
                //    → `:107 FinishResolvingAction`）**连 `ChangeOwner`/移动都不走** ⇒ 也就没有
                //    召唤病。本行必须落在上面 `to < 0` 那个 `continue` **之后**。
                //    ⚠️ **改前**是「只在尾句含 `fast` 时清、否则**保持原状**」⇒ 偷一个**本来没疲劳**
                //    的单位（对面回合结束时刷过的那一批）就能**当场再动一次**（静默错）。
                //    ⚠️ 今天全池**只有** `GSC_Telephatic_Domination` 一张 `takecontrol`、而它自带
                //    `and give it Fast` ⇒ 这条差异在真卡上**观测不到**（先置病、下一段又被清掉）。
                //    但判据是「原版无条件置」⇒ **照做**（铁律 11：不因不可观测就不做）。
                //
                // ①′ 🔴 **2026-10-18（`A1093` + `A1071`）：这一行从 `t.Exhausted = true;` 改成
                //    调 `RuleCore.ResetSummonSickness(t)`。** 两笔是**同一条语义的两半**，别各写一套：
                //      · `A1093`：我们原来**少一位** —— `UnitState` 只有 `canAct`(`+0x230`) /
                //        `canAttack`(`+0x23C`)，而原版 `+0x58`（`summonSickness`）**本身是一个字段**
                //        （有独立读点：`BattleManager__IsValidAttackTarget.c:200` ·
                //        `AI__ScoreFromGivingCharge.c:9-12` · `AI__ScoreFromCriteria.c:296-299`）。
                //        现在补上了（`UnitState.SummonSickness`），这一行**写它**；
                //      · `A1071`：写的是「**只写 `Exhausted`**」那一版的口径 —— 但 `Exhausted` 的语义
                //        是 `canAct`，而原版 `ResetSummonSickness` 是「写 `+0x58 = 1` **再**按新值
                //        重算 `canAct`」（同方法 `:21` 转调 `ActivateMinion`）⇒ 这里正是那个「再算」。
                //    ⇒ **净效果**：不带 `fast`/`flank`/`ferocity`/`oath` 的抢来仍是 `Exhausted = true`
                //      （和改前一样）；**带的抢来就是 `false`**（改前一元化置真 —— 那一半才是差异）。
                RuleCore.ResetSummonSickness(t);

                // ② 卡面 `and give it Fast` ⇒ **现在就能动**（见上面 `Fast` 那一条说明）
                //
                // 🔴 **2026-10-17（F7）实测：这一句【不能】并进 `RuleCore.HasDeployExemption`。**
                //    那条判据读的是**此刻**的关键词（`u.Has("fast"/"flank"/"ferocity")`），而这里的
                //    尾句是**本 op 之后的第二条 op** —— 真卡 `Telephatic Domination` 实测解析成
                //    `[takecontrol(tail="give it fast"), give(payload="fast")]`，`ResolveOps` 又**按序**跑
                //    ⇒ 走到这一行时 `t.Has("fast")` 还是 **false**（离屏实测：
                //    `Exhausted=True · Has(fast)=False · HasDeployExemption=False`）。
                //    ⛔ 照字面改成 `if (RuleCore.HasDeployExemption(t)) t.Exhausted = false;`
                //    这句就**永远不成立** ⇒ 抢过来的单位仍疲劳、`give it Fast` 成了空话
                //    （离屏三态对照里那一版实测红：`Exhausted=True`；`RuleEngineTest` ⑨ 的
                //    「★ `and give it Fast` ⇒ 现在就能动」会跟着红）。
                //    ⇒ 它判的是**「卡面马上要给的豁免」**（文本预判），和那条**读现状**的判据是
                //      **两回事**；真要收成一处，得先让尾句那条 op 结算掉（要动 `RuleCore`）。
                //    依据与实测输出：`资料/普查产出_1017/F7_判据收口.md`
                if (!string.IsNullOrEmpty(op.Tail)
                    && op.Tail.IndexOf("fast", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    t.Exhausted = false;

                ctx.Log($"{by}：**把 {t.Name} 抢过来了**（{ctx.Players[fromP].Name} 的 {fromSlot} 号格"
                      + $" → 你的 {to} 号格；本回合结束归还）");
                n++;
            }
            if (n == 0) return false;
            ctx.Log($"{by}：「{op.Source}」抢到 {n} 个单位");
            return true;
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
        /// **原版出处**：`CardScript.ResolveUnitSummoned`（`CardScript__ResolveUnitSummoned.c:31-33`）
        /// → `OnTrigger(AbilityTrigger.OtherUnitSummoned = 190, …)`；广播器
        /// `BattleManagerSupport__BroadcastUnitSummoned.c` 依次发给**自己 `:21` → 场上每张牌
        /// `:28-39` → 当前回合方手牌 `:43-52` → 另一方手牌 `:53-70`**。
        /// 我们**全部四跳都接上了**（自己那一跳 = `WhenEvent.SelfOnly`，
        /// 手牌那两跳 = <see cref="BroadcastHandWhen"/>，2026-10-18 · `A887`）。
        /// 🔴 **2026-10-17 更正**（原文写「手牌里那些等着被 `SetupCardInHand` 挂效果的**还没做**」）：
        ///    `SetupCardInHand` 那套的判据已逐句读完，**「把效果挂到手里那一张牌上」的机制当天落地** ——
        ///    本文件的 `HandReferents` + `AttachEffectToHandInstances`（原版判据逐条写在那两处上方）。
        /// 🔴 **2026-10-18 就地订正（`A887`）**：上一版这段话的**后半截**（「**仍然没做的**是
        ///    『广播到**手牌**』本身那两跳（`:53` / `:65`）—— 即『我手里那张牌在听
        ///    `OtherUnitSummoned`』还没接线」）**已不成立** —— 那两跳**当天接上了**
        ///    （`BroadcastWhen` 的 ⓪-d → `BroadcastHandWhen`，两跳顺序照原版）。
        ///    留痕：`资料/事件层_数据与设计.md` §五 第 1 行同一批订正过（原来把
        ///    「监听范围不判回合方」记成**我们挑的**）；`资料/常驻效果_数据与设计.md` §六
        ///    那张表里对应的条目也**该划掉**（⚠️ 该文件不在本件的可改范围里，未动）。
        ///    ⚠️ 别再写成「一点都没做」，也别再写成「只差手牌那一跳」—— 现在是**四跳齐**。
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

            // ⚠️ 2026-10-21（审查 `W61b` 第 11 条）：这里原来还有一个 `var unresolved = new List<string>();`
            //   —— **没有写者**（队列体各自攒自己那份 `mine` 并自己 `ReportUnresolved`）⇒ 已删（死代码）。
            //   同一族：`ResolveAtTurn` 末尾那个 `unresolved` 仍传给 `ReportUnresolved`，保留（它是那条路的
            //   「本次一段共用」口径，今天恒空但**不是**死变量 —— 见那边的注释）。
            foreach (var pe in jobs)
            {
                string name = pe.Source != null ? pe.Source.Name : "?";
                ctx.Log($"—— 部署触发段：「{name}」盯上 {unit.Name}"
                      + $"（{pe.Criteria}）→ {pe.Body} ——");
                // ⚠️ 这三句**现在是空转**（2026-10-21 接线后）：真正要用的那次种法在队列体
                //    `DeployJobDeferred` 里（这里种完，紧接着的 `savedTargets` 还原会把它抹掉）。
                //    ⛔ 别删队列体里那一份 —— 删了 `give it Vanguard` 会指到别人的目标上。
                ctx.LastTargets.Clear();
                ctx.LastTargets.Add(unit);
                ctx.LastTarget = unit;

                // 🔴 2026-10-21（`T03·c` 批 B 接线）：**一条部署触发 ability = 一条队列动作**
                //   —— 原版 `CardScript.ResolveUnitSummoned` → `OnTrigger(190)` → `PlayAbility`
                //   → `AddAutoActionToQueue`（判据报告 §4.A 第 2 步 6-11 / §2.4）。
                //   ⚠️ `LastTargets` 的种法一起搬进队列体（播报那边退出时会还原，见那边注释）。
                var _pe = pe;                  // for 循环变量按帧捕获
                EnqueueAbility(ctx, ListenerLane(_pe.Source, TN_OtherUnitSummoned), "部署触发:" + name,
                               () => DeployJobDeferred(ctx, owner, unit, _pe));
            }

            ctx.LastTargets.Clear();
            ctx.LastTargets.AddRange(savedTargets);
            ctx.LastTarget = savedLast;
        }

        /// <summary>
        /// <see cref="ResolveDeploy"/> 的**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）。
        ///
        /// ⚠️ 代码体逐句搬自原同步段，只把「每一条之前重种 `LastTargets`」也搬了进来 ——
        ///    广播那边 `ResolveDeploy` 的 save/restore 在**入队之后立刻**把槽还原，
        ///    队列体是另一个时刻，不重种的话 `give it Vanguard` 会指到别人的目标上。
        /// ⚠️ `unresolved` 在队列体里自己攒自己报（`ReportUnresolved`），理由同
        ///    `AtTurnHandJobDeferred`。
        /// </summary>
        static void DeployJobDeferred(BattleContext ctx, int owner, UnitState unit, PersistentEffect pe)
        {
            // 🔴🆕 2026-10-21（审查 `W61b` 第 4 条 · PLAUSIBLE）：**种了要还原**
            //   （`try/finally`，写法照 `ResolveDeploy` 自己那三行）。
            //   为什么必须还原：队列体跑在**另一个时刻** —— `ResolveDeploy` 的 save/restore
            //   在**入队之后立刻**就把槽还原了，队列体若不还原，泵完之后
            //   `ctx.LastTarget`（单数）会**停在刚部署的那个单位**上，跨动作残留 ⇒
            //   下一条没有显式 `chosen`/`seed` 的能力走 `ConditionHolds` 兜底时拿到别人的目标（静默错打）。
            //   （`LastTargets` 会被下一次 `ResolveOps` 清掉，所以只有单数槽有实质风险 —— 但两格一起还原。）
            //   判据 = 本文件 `:6724-6727` 的「用完**必须**还原，否则会踩到调用方」+ `ResolveOps` 的清槽口径。
            var savedTargets = new List<UnitState>(ctx.LastTargets);
            var savedLast = ctx.LastTarget;
            // ⚠️ **每一条之前都要重设**：上一条的结算会把 `LastTargets` 覆盖掉，
            //    不重设的话第二条效果就打在了错误的目标上（静默错打）。
            ctx.LastTargets.Clear();
            ctx.LastTargets.Add(unit);
            ctx.LastTarget = unit;

            var mine = new List<string>();
            ctx.EffectChain++;
            // 🆕 2026-10-20（`W57FIX2` 续 · 审计 P3 旁路）：这条**常驻作业**的原版能力属主
            //   是 `pe.Source`，**不是**被部署的那个单位（判据链 → `BattleContext.FactionSourceCard`）。
            //   只影响 `DoFactionResource` 的「任务点来源卡」；代词/相邻锚点仍走 `ActingUnit`。
            var prevFactionSrc = ctx.FactionSourceCard;
            ctx.FactionSourceCard = pe.Source;
            try
            {
                foreach (var o in pe.Ops)
                    ResolveOne(ctx, owner, unit, "部署触发", o, null, mine, card: pe.Source);
            }
            finally
            {
                ctx.FactionSourceCard = prevFactionSrc;
                ctx.EffectChain--;
                // 🔴 还原调用方（`ResolveDeploy` 之后紧接着跑的那段）刚种好的代词槽
                ctx.LastTargets.Clear();
                ctx.LastTargets.AddRange(savedTargets);
                ctx.LastTarget = savedLast;
            }
            ReportUnresolved(ctx, "「部署触发」（" + (pe.Source != null ? pe.Source.Name : "?") + "）", mine);
        }

        /// <summary>
        /// 🔴 **事件广播期间那几个「代词槽 / 事件槽」的保存—还原** —— **一处形状，三条跳共用**
        /// （`W5` 顺手报回、`G3` 2026-10-18 落地）。
        ///
        /// **它挡的是哪一件事**：`EventTarget`（事件宾语）在 `BroadcastWhen` 的**棋盘那一跳**里
        /// 原来是「存了但从没还原」（另两跳 —— `BroadcastPersistentWhen` / `BroadcastHandWhen` ——
        /// **都有**还原）⇒ 广播结束后 `ctx.EventTarget` **留着上一次的宾语**
        /// ⇒ 同一次结算里后续读 `the target of the attack` / `AdjacentAnchor.TargetIfMeetsCriteria`
        /// 的地方（`ResolveTargets` :590-600 · `ResolveAdjacentAnchor` :529-536）会读到**陈旧值**。
        /// 判据：`BattleContext.EventTarget` 的注释白纸黑字写着
        /// 「由 `BroadcastWhen` 在广播事件时设、**广播完恢复**（和 `LastTargets` 同一套保存/还原）」。
        ///
        /// ⛔ **别再往这三跳里各写一份 save/restore** —— 三份迟早不一致（**这次就是三份里漏了一份**）。
        /// ⚠️ **还原的是「进入前的值」，不是 `null`**：监听器的正文里还会再广播事件（嵌套），
        ///    外层那份**必须**回来，否则一进一出就把调用方刚种好的宾语抹掉。
        /// ⚠️ **`EventCard` 只在 <see cref="TargetAndCard"/> 那一支被种** ——
        ///    `BroadcastPersistentWhen` 原来不碰它，别顺手把它清成 `null`（那是**行为变化**）。
        /// </summary>
        struct EventSlotsScope : System.IDisposable
        {
            readonly BattleContext _ctx;
            readonly List<UnitState> _savedTargets;
            readonly UnitState _savedLast;
            readonly UnitState _savedEventTarget;
            readonly CardDef _savedEventCard;

            EventSlotsScope(BattleContext ctx)
            {
                _ctx = ctx;
                _savedTargets = new List<UnitState>(ctx.LastTargets);
                _savedLast = ctx.LastTarget;
                _savedEventTarget = ctx.EventTarget;
                _savedEventCard = ctx.EventCard;
            }

            /// <summary>种**事件宾语**（`EventCard` 不动）—— `BroadcastPersistentWhen` 那一跳用。</summary>
            public static EventSlotsScope TargetOnly(BattleContext ctx, UnitState target)
            {
                var s = new EventSlotsScope(ctx);
                ctx.EventTarget = target;
                return s;
            }

            /// <summary>种**事件宾语 + 事件卡**（`create a copy of it` 要 `EventCard`）——
            /// `BroadcastWhen` 的棋盘那一跳与 `BroadcastHandWhen` 用。</summary>
            public static EventSlotsScope TargetAndCard(BattleContext ctx, UnitState target, CardDef card)
            {
                var s = new EventSlotsScope(ctx);
                ctx.EventTarget = target;
                ctx.EventCard = card;
                return s;
            }

            /// <summary>还原（`using` 块退出时跑，**异常也会跑**）。</summary>
            public void Dispose()
            {
                _ctx.LastTargets.Clear();
                _ctx.LastTargets.AddRange(_savedTargets);
                _ctx.LastTarget = _savedLast;
                _ctx.EventTarget = _savedEventTarget;
                _ctx.EventCard = _savedEventCard;
            }
        }

        /// <summary>
        /// 🆕 **2026-10-18（`S10` · 待办 `A962` ② 收口）：`draw` 广播期间的「指代槽」** ——
        /// 把 <see cref="BattleContext.DrawnThisResolve"/> **临时种成「刚抽到的那一份」**，
        /// 广播跑完（**含抛异常**）后还原成**调用方原来的值**。（形状与 <see cref="EventSlotsScope"/> 同族。）
        ///
        /// 🔴 **为什么必须有它（今天是一次都不生效的真缺陷）**：
        ///   `Company Master`（`DA31`）卡面 `When you draw a card, it costs 1 less this turn`
        ///   解析成 `lowercost amount=1 payload=(指代上一张)`，走 <see cref="DoLowerCost"/> 的
        ///   `op.Payload == "(指代上一张)"` 那一支 —— 那一支的指代**只有两个来源**：
        ///   `ctx.LastCreated` → 兜底 `ctx.DrawnThisResolve`（`EffectResolver.cs` 的 `:6938`）。
        ///   而 `DrawnThisResolve` 原来的**写点只有三条**（`DoDraw` / `DoChooseCard` / `DoDrawRef`），
        ///   **`RuleCore.Draw` 一条都没有** ⇒ **普通抽牌**（回合开始那一抽）的监听器读到的永远是空
        ///   ⇒ 日志白纸黑字「要降费，但前面没有可指代的卡……**这条没生效**」。
        ///   离线实测（`D:/tmp/wf_s10_probe` · 真卡真卡池）：加这一层之前 `CostOf = 5`（印价）、
        ///   之后 `CostOf = 4`（5−1）。✅
        ///
        /// 🔴 **为什么必须套在【广播外面】、而不是在广播里补写**：
        ///   监听器的正文要**在广播进行中**读那个槽（`DoLowerCost` 就在监听器那一跳里跑）
        ///   ⇒ 种的动作必须发生在 `BroadcastWhen` **之前**、还原在其**之后**。
        ///
        /// ⚠️ **还原的是「进入前的值」，不是 `Clear()`** —— 与 `EventSlotsScope` 同一条纪律：
        ///   `DoDraw` / `DoDrawRef` 在**广播之外**往这个槽里累加「本次结算抽到的全部」
        ///   （`For each troop drawn …` 数它）⇒ 不还原就会把调用方那一批**吃掉**。
        ///   `DrawReferentScope` 只动 `DrawnThisResolve` **一个**槽
        ///   （`LastCreated` / `LastHandTarget(s)` 一律不碰）⇒ 与 `EventSlotsScope` 嵌套**不打架**
        ///   （`BroadcastWhen` 内部自己会开一层 `EventSlotsScope`）。
        ///
        /// ⚠️ **窗口只放宽这一处**：广播期间槽里 = **恰好那一份**（不是「调用方原有的 + 这一份」）——
        ///   卡面 `it` 是**单数**（= 抽出来的那张牌，用户 2026-10-18 澄清的口径）⇒ 一次抽多张时
        ///   每次广播只指「这一次抽出来的那一份」。
        /// ⚠️ **连带（今日零命中，如实标着）**：广播期间 `HandReferents` 的**兜底**与
        ///   `CountRef("draw")` 读的也是这个槽 ⇒ 窗口内它们的读数从「空/陈旧」变成「1 份」。
        ///   今天**卡池里只有 2 张**监听 `draw`（`DA31 Company Master` · `BL15 Rubric Marine`），
        ///   两者的效果都**不读** `HandReferents` / `CountRef("draw")` ⇒ 实测无变化；
        ///   **将来新增 `draw` 监听器时要重新判这一段**。
        /// </summary>
        struct DrawReferentScope : System.IDisposable
        {
            readonly BattleContext _ctx;
            readonly List<CardInstance> _saved;

            DrawReferentScope(BattleContext ctx, CardInstance inst)
            {
                _ctx = ctx;
                _saved = new List<CardInstance>(ctx.DrawnThisResolve);
                ctx.DrawnThisResolve.Clear();
                if (inst != null) ctx.DrawnThisResolve.Add(inst);
            }

            /// <summary>种**刚抽到的那一份**（`inst` 为 `null` ⇒ 只清空，不放东西）。</summary>
            public static DrawReferentScope Seed(BattleContext ctx, CardInstance inst)
            {
                return new DrawReferentScope(ctx, inst);
            }

            /// <summary>还原（`using` 块退出时跑，**异常也会跑**）。</summary>
            public void Dispose()
            {
                _ctx.DrawnThisResolve.Clear();
                _ctx.DrawnThisResolve.AddRange(_saved);
            }
        }

        /// <summary>
        /// **事件广播段** —— 某件事（部署 / 死亡 / 攻击 / 受伤）发生了，回头问「谁在听」。
        ///
        /// 这是第三十二轮做的**事件层的下半截**：上半截（`Core/WhenEvent.cs`）只把
        /// 卡面 `When &lt;事件&gt;, &lt;正文&gt;` 认出来、收进 `CardDef.WhenTriggers`；
        /// **没人广播的话那些监听器一辈子不会响**（而卡面照样不打 `*` —— 静默失效）。
        ///
        /// **原版出处**：`BattleManagerSupport__BroadcastUnitSummoned.c` —— 部署事件依次发给
        /// **自己 `:21` → 场上每张牌 `:28-39`（`IsInPlay` 过滤）→ 当前回合方手牌 `:43-52`
        /// → 另一方手牌 `:53-70`**。
        /// 🔴 **2026-10-18 订正（`A887`）：手牌那两跳【已经接上了】，不再是空的。**
        ///    原来这里写着「我们**只做「场上每张牌」这一支**（`:41`）—— 手牌那两支属**降费那一族**，
        ///    由 `RuleCore` 在 `CostWhens` 上另接」—— 那句话**把两件事混成了一件**：
        ///    降费那一族（`CardDef.CostWhens`）是**另一条收线**，与手牌**单位卡**的 `WhenTriggers`
        ///    各走各的。现在手牌那一跳由 <see cref="BroadcastHandWhen"/> 接上。
        ///
        /// **判据（手牌那一跳真的会响）**：接收端 `CardScript__ResolveUnitSummoned.c` 的闸是
        /// `cardState != 5`（`:21` / `:36` / `:55`），而 `CardStateOptions.waitingToDie = 5`
        /// ⇒ **`inHand` 不被排除**；`:25-34` 对「被召唤的那张 ≠ 自己」**无条件**
        /// `OnTrigger(0xbe = 190 = OtherUnitSummoned)` ⇒ 躺在手里的单位卡照样收到。
        ///
        /// ⚠️ **手牌那一跳是「全场每一张广播器」的通用形状，不是部署专有** —— 实测
        /// `d:/2/tools/decomp_full/BattleManagerSupport__Broadcast*.c` **43 个里有 37 个**
        /// 调 `BattleManager.GetCardsInHandRef`。我们这边只有一个统一广播器
        /// （<see cref="BroadcastWhen"/>，部署 / 死亡 / 攻击 / 受伤 / 关键词事件都走它）
        /// ⇒ 手牌那一跳也**对所有 `kind` 生效**，这是照原版的形状来的，不是放宽。
        ///
        /// </summary>
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
        /// <param name="preCalled">
        /// 🔴 **2026-10-20（`W58` · D8）：已经被调用方【预调过一次】的主体** —— 快照时**跳过它**。
        /// 传它的几族（全部逐条核过原版 `.c`）：`Damaged`（`RuleCore.EmitHit`）·
        /// `Attack`（`RuleCore.DeclareAttack`，另见 <paramref name="preCalled2"/>）·
        /// `Deploy`（`RuleCore` 两条部署入口）· `GetsHuntMark`（`BroadcastKeywordEvent`）。
        ///
        /// 判据 = 原版 `BattleManagerSupport.BroadcastUnitDamaged` @ `0x1809B77C0`（机器码直读）：
        ///   · `0x1809B7829-0x1809B7835` 先给**被打者自己**调一次
        ///     `CardScript.ResolveDamageDealt(damagedUnit, damagedUnit, damageInfo)`；
        ///   · 棋盘循环 `0x1809B78BC-0x1809B78C9` 用 `UnityEngine.Object.op_Inequality(card, damagedUnit)`
        ///     **跳过被打者**（反编译 `.c` 里那句 `op_Inequality` 丢了第二个实参，机器码是 `mov rdx, r14`）。
        ///
        /// ⚠️ **「预调」与「排除」必须成对**：只预调不排除 = 主体**响两次**；
        /// 只排除不预调 = 主体**被整个漏掉**（它不在按格号序排的循环里；`Deploy` 的预调**只跑
        /// `SelfOnly` 那一半**，见 `FireBoardWhenListener` 的 `selfOnlyOnly`）。
        /// ⚠️ 别把这个形参推广成「所有事件都排除主体」—— 其它时机的原版形状**没有逐条核过**
        /// （见 `W58_战斗结算_受伤广播D8.md` §「其它 BroadcastWhen 调用点核对」；
        /// `W58_战斗结算_其余广播顺序.md` 逐条核了 `Deploy` / `Attack` / `HuntMark` /
        /// `Mob|Regiment|Swarm`，后者原版**没有预调**、自排除在接收方体内，我们由
        /// `WhenEvents.SelfExcludesOnKeywordTrigger` 等价实现）。
        /// </param>
        /// <param name="preCalled2">
        /// 🆕 **2026-10-20（`W58` · BROADCAST）：第二个已被预调的主体** —— 目前只有 `Attack` 传。
        ///
        /// 判据 = 原版 `BattleManagerSupport__BroadcastUnitAttacked.c`：
        /// `:22` 先预调 **被打者**（`ResolveUnitAttacked(target, attacker, target, …)`）、
        /// `:29` 再预调 **攻击者**（`ResolveUnitAttacked(attacker, attacker, target, …)`）；
        /// 棋盘循环 `:38` / `:44` 两条 `op_Inequality` 把**这两个都排除**
        /// （`:44` 那条在机器码里是 `op_Inequality(item, target)`；`.c` 把第二实参丢了）。
        /// ⇒ 次序是**被打者 → 攻击者 → 其余棋盘 → 手牌**（「被攻击方优先结算」）。
        /// </param>
        ///
        /// ⚠️ **先快照再结算**：监听器的效果会改棋盘（能打死人、也能再部署），
        ///    边遍历边改数组是未定义行为。和 <see cref="ResolveDeploy"/> 同一个理由。
        /// <param name="boardListeners">
        /// 🆕 **2026-10-22（`W62` · R2 · 狂暴那一格）：棋盘那一跳的【监听者快照】可以由调用方给。**
        ///
        /// 🔴 **为什么需要**：触发改入队之后，「自己那条正文 + 尾部广播」整体搬进了队列体
        /// （`FireTriggerDeferred`），于是**快照在出队那一刻才拍**。而原版的广播是
        /// **同步**发生的（`CardScript__ResolveUnitAttacked.c:142 BroadcastUnitMob` /
        /// `BroadcastUnitFerocity` 都在 `OnTrigger` 之后同一个函数里、`PlayAbility` 只是入队）
        /// ⇒ 快照应当拍在**入队那一刻**。反例（`A1562-③`）：带 `Ferocity` 的单位自己用狂暴，
        /// 广播还没跑，它已经被「洗回牌库」（`RuleCore.cs` 的 `Ferocity` 那一支）⇒
        /// 出队时 `IsPendingDeath` / 棋盘扫描都收不到它 ⇒ **它自己的
        /// `When … triggers Ferocity` 监听器一次都不响**（静默）。
        /// · `null`（缺省）= 老行为：在**这一跳**现扫棋盘（`BroadcastWhen` 的绝大多数调用点）。
        /// · 非 null = 用调用方在**事件发生那一刻**拍好的快照（本函数会拷一份再排序，不改实参）。
        /// </param>
        public static void BroadcastWhen(BattleContext ctx, string kind, int who, CardDef card,
                                         UnitState subject = null,
                                         UnitState actor = null, UnitState target = null,
                                         UnitState preCalled = null,
                                         UnitState preCalled2 = null,
                                         List<BoardListener> boardListeners = null,
                                         UnitState actingCard = null,
                                         // 🆕 2026-10-26（批次 4 · `F2`）：只跑「棋盘跳 + 常驻效果跳」。
                                         //   用途 = `BroadcastEarnedSkull`：原版那个广播器**只有棋盘一跳**
                                         //   （`BattleManagerSupport__BroadcastEarnedSkull.c:15-35`，
                                         //   没有 `GetCardsInHandRef` 那两跳、没有预调）⇒ 那三跳
                                         //   （事件型降费 / 手牌陷阱 / 手牌单位）必须关掉，否则我们会叫醒
                                         //   原版根本不会叫醒的监听者（「打得比卡面宽」）。
                                         //   ⚠️ **常驻效果跳照旧保留**：原版那类「For the rest of this battle,
                                         //   when …」是**挂在场上卡/督军上的附魔**（判据见 `报告/F2_第4轮.md`
                                         //   §四 的 `SAU53` 附魔链）⇒ 我们的 `ctx.PersistentEffects` 正是它的
                                         //   等价物，不算「多一跳」。
                                         //   ⛔ 别把它设成 `true` 当成「省点算力」的开关 —— 默认 `false`
                                         //   才是所有既有调用点的原口径。
                                         bool boardAndPersistentOnly = false)
        {
            // 🔴 **2026-10-22（`W62` · R1）：公开入口必须自带 `ActionScope`。**
            //   触发改「只入队」之后，广播**本身**会把监听器的 ability 排进队列；而泵点只在
            //   `ActionScope` 退出时发生 ⇒ 本函数如果没有作用域、调用方也没开（**自检/夹具直调**、
            //   以及 `GrantDarkPact` 这一类次级入口），那批监听器就**永远停在队列里不结算、且无告警**
            //   （`ActionScope.Dispose` 的「泵点漏了」只在**外层**作用域退出时发）。
            //   ⚠️ 嵌套安全：已经在一条动作里（`Resolving == true`）⇒ 内层什么都不做（`ActionPump.cs` 的设计口径 ②）。
            //   ⛔ 不是「新造泵点」—— 就是同族入口那个 `using (BeginAction(ctx))` 形状（照 `ApplyDamage`）。
            using (BeginAction(ctx))
                BroadcastWhenCore(ctx, kind, who, card, subject, actor, target,
                                  preCalled, preCalled2, boardListeners, actingCard,
                                  boardAndPersistentOnly);
        }

        /// <summary><see cref="BroadcastWhen"/> 的原文（2026-10-22 `W62` · R1 抽出，逐字搬下来）。</summary>
        static void BroadcastWhenCore(BattleContext ctx, string kind, int who, CardDef card,
                                      UnitState subject, UnitState actor, UnitState target,
                                      UnitState preCalled, UnitState preCalled2,
                                      List<BoardListener> boardListeners, UnitState actingCard = null,
                                      // 🆕 2026-10-26（批次 4 · `F2`）：见 `BroadcastWhen` 的同名参数
                                      bool boardAndPersistentOnly = false)
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

            // ---- ① 快照：双方棋盘上所有还活着的单位 ----
            // 🔴 **2026-10-18（`W5` · D1 §6·1 + 必查 7）：快照挪到【四条非棋盘跳之前】，而四条非棋盘跳
            //   挪到【棋盘跳之后】** —— 原版次序是 **自己 → 棋盘 → 手牌**
            //   （`BattleManagerSupport__BroadcastUnitSummoned.c:21 / :25-39 / :44-64`；
            //    `BattleManagerSupport__BroadcastUnitDamaged.c:19 / :20-61 / :63-82` —— 逐行核过）。
            //   改之前那四条**全排在棋盘跳之前**，理由是「躲下面那句『场上没人听就 return』」。
            //   ⇒ 现在两头都满足：**快照先算**（这样下面「没人听」也不影响非棋盘那四条），
            //     **结算次序改成棋盘在前、手牌在后**。
            //   ⚠️ **快照只覆盖棋盘那一跳**（`listeners`）；那四条各自内部仍按各自的取数源实时取
            //     —— 那也正是原版的形状（`GetCardsInHandRef(...)` 返回的是**活的**手牌列表，
            //     不是快照）。⛔ 别把「快照」推广成「给手牌也照相」：原版没这一步，
            //     而且手牌跳的监听集合本来就该是「轮到时那一眼」的。
            // 🆕 2026-10-22（`W62` · R2）：快照要么由调用方在【事件发生那一刻】给（见 `boardListeners`），
            //   要么就在这里现扫（老行为，绝大多数调用点）。拷一份是因为下面 `StableSortByPlayOrder` 会原地排序。
            // 🆕 2026-10-26（第八十三会话 · 执行代理 `B1`）：**这一族的棋盘跳是否也收待死单位**
            //   —— 原版每条广播器的闸**不统一**，只有 `BroadcastUnitUsedSacrifice`
            //   （= `triggers:sacrifice`）那一族三趟**都没有 `IsInPlay`** ⇒ 由
            //   `BoardJumpIncludesPendingDeath` 一处按族判（⛔ 别在这里写第二份名单）。
            bool boardIncludesPending = BoardJumpIncludesPendingDeath(kind);
            var listeners = boardListeners != null
                          ? new List<BoardListener>(boardListeners)
                          : SnapshotBoardListeners(ctx, preCalled, preCalled2, boardIncludesPending);

            // ---- ② 棋盘那一跳（原版第 2 跳）----
            // 🔴 2026-10-21（引擎线 `T03·c` 批 A · `W59`）：**先按【进场序】排，再逐条结算** ——
            //   原版这一趟遍历的是 `allCardsInPlay`（进场顺序的追加列表，`BattleManager + 0x470`），
            //   ⛔ 不是棋盘格号、也不是「玩家 0 整排 → 玩家 1 整排」
            //   （判据 → `资料/普查产出_1020/W58_战斗结算_多词条顺序判据.md` §〇 / §一·A1 / §四·D1；
            //    样例 = `BattleManagerSupport__BroadcastUnitSummoned.c:25-39`）。
            //   ⇒ 多单位同时听同一个事件时，「先部署的那个先响」。
            //   ⚠️ 排序发生在**快照之后、预调之外**（`preCalled` / `preCalled2` 已经被排除出快照，
            //   它们的先后仍由调用方决定 —— 原版那条预调本来就在循环之前，不在这一趟里）。
            RuleCore.StableSortByPlayOrder(listeners, l => l.Unit.PlayOrder);
            if (listeners.Count > 0)
            {
                // ⚠️ `LastTargets` 是**代词机制**的存放位置：`it` / `this troop` 全从它取。
                //    事件的「它」= **发生这件事的那个单位**（刚部署的那台载具 / 刚死的那个兵），
                //    **不是**监听者自己。用完必须还原，别踩到调用方刚放进去的那批。
                // ⚠️ `EventTarget`（宾语）也一起 —— 它是**另一件事**，不是 `LastTarget` 的别名：
                //    `Doomstalker` 的 `it` 指**攻击者**（由 `subject` 种进 `LastTargets`），
                //    `Valtus` 的 `the target of the attack` 指**被打的那个**（走这个字段）。
                //    同一句里两个指代同时存在，所以**必须是两个槽**。
                //    🆕 **2026-10-18（`G3` 真差异 ③）：种法 + 还原**统一走 `EventSlotsScope`**
                //    （原来是这里手写一份 save、**漏 restore**，另两跳各写一份 —— 三份里漏了一份）。
                //    ⇒ 这一段到此**既种也还原**，而且 `try/finally` 兜住异常。
                // ⚠️ **「那张卡」也一起种**（`EventCard`，2026-09-16 加的）—— `create a copy of it`
                //    （`Neurotyrant`）要它，见 `BattleContext.EventCard` 的注释。
                using (EventSlotsScope.TargetAndCard(ctx, target, card))
                {
                    foreach (var l in listeners)
                    {
                        // 🔴 **2026-10-20（`W58` · D8）：这一跳抽成了 `FireBoardWhenListener`** ——
                        //   `EmitHit` 的「被打者预调」必须与这里**逐句同形**（判据共用一份；
                        //   ⛔ 别在 `EmitHit` 里另写一条简化版）。原来这段逐句原样，只是换了函数边界。
                        // 🆕 2026-10-22（`W63` #9）：`pinnedOwner` = 快照那一刻的归属 —— 出队时
                        //   那个人可能已经被洗回牌库 / 移出棋盘，现查会把它的监听器静默丢掉。
                        FireBoardWhenListener(ctx, l.Unit, kind, who, card, subject, actor, target, targetWho,
                                              // 🆕 2026-10-26（`B1`）：原版**没有 `IsInPlay` 闸**的那一族
                                              //   （`triggers:sacrifice`）这里也要放行待死单位 ——
                                              //   判据仍是 `BoardJumpIncludesPendingDeath` 一处。
                                              allowPendingDeath: boardIncludesPending,
                                              pinnedOwner: l.Owner, actingCard: actingCard);
                        if (ctx.IsOver) break;
                    }
                }
                // ⚠️ **这里【不】因为 `ctx.IsOver` 提前 return** —— 那四条非棋盘跳各自内部
                //    有 `ctx.IsOver` 守卫（`BroadcastHandTrapWhen` / `BroadcastHandWhen` 是 `return`、
                //    `BroadcastPersistentWhen` 是 `break`），而且**改动之前它们也是照跑不误的**
                //    （它们排在棋盘跳前面）⇒ 加一个 `return` 会**悄悄改掉**那条既有行为。
            }

            // ---- ③ 非棋盘那四条跳（原版第 3 跳：手牌）------------------------------------
            // 🔴 **它们必须【仍然】与「自家场上有没有单位」无关**（这一条一字未改，只是次序挪到棋盘之后）：
            //   这一族的监听者**不在棋盘上**（挂在玩家身上 / 在弃牌堆里 / 躺在手牌里），
            //   所以**没有**「棋盘那一跳没人听就整条跳过」这回事 —— 上面那句 `if (listeners.Count > 0)`
            //   只是**不再提前 return**（原来那句 `if (listeners.Count == 0) return;` 会把这四条静默跳过）。
            //   原版也是分开的三跳（棋盘 → `GetCardsInHandRef(IsPlayerTurn())` → `GetCardsInHandRef(!…)`）。
            //
            //   ⓪ **事件触发式降费**（挂**玩家**身上：`Lower cost by 1 when an enemy dies`）。
            //   🆕 2026-10-26（批次 4 · `F2`）：`boardAndPersistentOnly` 时**这三条手牌侧的跳全关**
            //   （原版 `BroadcastEarnedSkull` 没有手牌两跳 —— 见 `BroadcastWhen` 同名参数注释）。
            // `BroadcastPersistentWhen` 不在关的那一组里（它是「附魔挂在场上」的等价物）。
            if (!boardAndPersistentOnly) BroadcastCostWhen(ctx, kind, who, card);

            //   ⓪-b **常驻效果里的「当……时」监听器**（2026-09-14 A4 批 3）：
            //   `For the rest of this battle, when a friendly troop uses Ferocity,
            //    deal 2 damage to the enemy Warlord`（`Raid Tactics`）。
            //   这类**没有实体监听者** —— 来源是一张**已经进弃牌堆的战术卡**，棋盘上没人可挂，
            //   所以单独扫 `ctx.PersistentEffects`。
            //   ⚠️ 它**不是**原版三跳里的某一跳（原版没有「弃牌堆里的战术卡在听」这条）——
            //      它在哪一跳结算**没有判据**，这里按「非棋盘那一族」一起排在手牌那两跳之前
            //      （改动前它也在最前，相对次序未变）。
            BroadcastPersistentWhen(ctx, kind, who, subject, actor, target, targetWho);

            //   ⓪-c **手牌里的陷阱监听器**（`When you play a Stratagem, …`，2026-09-14 A4 批 4）。
            //   `Jammed Communications`（Genestealers 的破坏卡）—— 它是一张躺在持有者手牌里的**非单位卡**，
            //   所以棋盘那一跳**永远收不到它**（`CardDef.CanListenForEvents => IsUnit` 挡的正是这一族）。
            if (!boardAndPersistentOnly)
                BroadcastHandTrapWhen(ctx, kind, who, card, subject, actor, target, targetWho, kind == WhenEventKind.Draw, DrawnNow(ctx, kind));

            //   ⓪-d **手牌里的单位卡监听器**（`A887`，2026-10-18）。
            //   卡面 `When <事件>, …`（`CardDef.WhenTriggers`），监听者是**一张躺在手牌里的单位卡**
            //   （`CardDef.CanListenForEvents => IsUnit`）。⛔ **不是** ⓪-c 那一族：
            //   那一族是**非单位卡**的 `HandTrapWhens`（两边互斥，见 `BroadcastHandWhen` 的注释）。
            //   原版次序 = `:43-52`（当前回合方手牌）→ `:53-70`（另一方手牌），本函数按这个顺序遍历。
            if (!boardAndPersistentOnly)
                BroadcastHandWhen(ctx, kind, who, card, subject, actor, target, targetWho, kind == WhenEventKind.Draw, DrawnNow(ctx, kind));
        }

        /// <summary>
        /// **棋盘监听者快照的一条** —— 单位 + **它在快照那一刻的归属方**（🆕 2026-10-22 `W63` #9）。
        ///
        /// 🔴 **为什么要把 owner 一起照下来**：触发改入队之后，「拍快照」与「逐条唤醒监听者」
        /// 之间隔着一整段队列（`FireTriggerAt` 在**入队那一刻**拍，`FireTriggerDeferred` 在
        /// **出队那一刻**才广播）。这段时间里世界会变 —— 典型反例 `A1562-③`：
        /// 带 `Ferocity` 的单位自己用狂暴，`FireTriggerAt` 之后**紧接着**被洗回牌库
        /// （`RuleCore.ResolveAlternativeCore`）⇒ 出队时 `OwnerOf` 反查得 `-1`，
        /// 它自己的 `When … triggers Ferocity` 监听器**一次都不响**（静默）。
        /// 原版是**同步广播**（`BroadcastUnitFerocity`，RVA `0x9B7C50`，三跳 0 条 `op_Inequality`）
        /// ⇒ 归属应当取**事件发生那一刻**的值，⛔ 不是出队那一刻现查的。
        /// </summary>
        public struct BoardListener
        {
            public UnitState Unit;
            /// <summary>快照那一刻它在谁的棋盘上（= 快照循环的 `p`）。⛔ 别用 `OwnerOf` 现查替代。</summary>
            public int Owner;
        }

        /// <summary>
        /// **棋盘那一跳的「监听者快照」** —— 从 `BroadcastWhen` ②里那段逐字抽出来
        /// （2026-10-22 `W62` · R2：`FireTriggerAt` 要能在**入队那一刻**拍同一份快照）。
        /// 过滤口径与抽出来之前**逐字相同**（⛔ 别在这里改）：
        /// 跳过无卡 / 待死 / 没有监听器的单位，并排除 `preCalled` / `preCalled2` 两个预调主体。
        ///
        /// <para>🆕 **2026-10-26（第八十三会话 · 执行代理 `B1`）：`includePendingDeath`** ——
        /// 原版**没有 `IsInPlay` 闸**的那一族（`triggers:sacrifice`）**不跳待死单位**。
        /// 判据 = `BattleManagerSupport.BroadcastUnitUsedSacrifice`
        /// （ILSpy `BattleManagerSupport.cs:1764-1805`；`BattleManager.BroadcastSacrificeResolved`
        /// 只是转调它）三趟循环里**唯一**的闸是 `card != unitSacrifice`（自排除）——
        /// **没有** `IsInPlay`；接收方 `CardScript.ReactToUnitUsedSacrifice`
        /// （`CardScript.cs:7783-7788`）体内也没有。⇒ 与 `BroadcastUnitSpiritStone` /
        /// `BroadcastUnitSummoned` 那一族**不同**：那些棋盘那一趟**逐张判 `IsInPlay`**
        /// （`BattleManagerSupport.cs:1057` / `…__BroadcastUnitSummoned.c:25-39`）。
        /// 本参数**只由 <see cref="BoardJumpIncludesPendingDeath"/> 按族打开**，
        /// ⛔ 别把默认值改成 `true`。</para>
        /// </summary>
        static List<BoardListener> SnapshotBoardListeners(BattleContext ctx, UnitState preCalled,
                                                      UnitState preCalled2,
                                                      bool includePendingDeath = false)
        {
            var listeners = new List<BoardListener>();
            if (ctx == null) return listeners;
            for (int p = 0; p < 2; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || u.Card == null) continue;
                    // 2026-10-12（账 A1600）：原版这一跳的谓词是 `CardScript.IsInPlay()`（只看状态字、不看生命；
                    // `CardScript__IsInPlay.c:7-14` = {inPlay 2, inPlayAttacking 3, inPlayTransforming 0xf,
                    // inPlayAminingAttack 0x11}）。我们对应的「不在场上」= 已入将死队列（≈ waitingToDie 5）。
                    // 🆕 2026-10-26（`B1`）：原版**没有**这道闸的族（`triggers:sacrifice`）按参数放行 —— 见上面的 param doc。
                    if (!includePendingDeath && ctx.IsPendingDeath(u)) continue;
                    if (u.Card.WhenTriggers.Count == 0) continue;   // 没监听器的卡直接跳过
                    // 🔴 **2026-10-20（`W58` · D8）**：调用方已经对 `preCalled` **预调过一次**
                    //   ⇒ 这一跳**跳过它**（原版 `BroadcastUnitDamaged` 的
                    //   `op_Inequality(card, damagedUnit)` @ `0x1809B78C2`）。
                    //   ⛔ 只排除不预调 = 被打者整个漏掉；只预调不排除 = 双响。
                    // 🆕 同批（BROADCAST）：`Attack` 有**两个**预调主体（被打者 + 攻击者），
                    //   原版 `BroadcastUnitAttacked.c:38/:44` 两条 `op_Inequality` 各排除一个。
                    if (preCalled != null && ReferenceEquals(u, preCalled)) continue;
                    if (preCalled2 != null && ReferenceEquals(u, preCalled2)) continue;
                    // 🆕 2026-10-22（`W63` #9）：**归属一起照下来**（见 `BoardListener` 的 doc）。
                    listeners.Add(new BoardListener { Unit = u, Owner = p });
                }
            }
            return listeners;
        }

        /// <summary>
        /// **一个棋盘监听者的一跳** —— `BroadcastWhen` 的棋盘那一跳与
        /// <see cref="RuleCore.EmitHit"/> 的「被打者预调」**共用同一套**（判据共用一份，
        /// ⛔ 别在两处各写一遍：原版这两处是**同一个口** `CardScript.ResolveDamageDealt`）。
        ///
        /// 🔴 **2026-10-20（`W58` · D8）**：判据 = 原版 `BattleManagerSupport.BroadcastUnitDamaged`
        /// @ `0x1809B77C0`（机器码直读）：`0x1809B7829-0x1809B7835` 先给被打者自己
        /// `ResolveDamageDealt(damagedUnit, damagedUnit, damageInfo)`，
        /// 棋盘循环 `0x1809B78BC-0x1809B78C9` 再用 `op_Inequality(card, damagedUnit)` 跳过它。
        /// ⇒ 「预调」不是另一条简化路径，就是**这一个函数**。
        ///
        /// ⚠️ 函数体是**从 `BroadcastWhen` 的循环体逐句搬出来的**（换的只是 `continue` → `return`）——
        ///    `ctx.IsOver` 的循环控制留在调用方（`break` / 不调）。
        ///
        /// <param name="selfOnlyOnly">
        /// 🆕 **2026-10-20（`W58` · BROADCAST）：只跑这张卡上 `SelfOnly` 的那几条监听器。**
        /// 目前只有 `Deploy` 的预调传（`RuleCore` 的两条部署入口）。
        ///
        /// 判据 = 原版 `BattleManagerSupport__BroadcastUnitSummoned.c`：
        /// `:21` 的预调是 `CardScript.ResolveUnitSummoned(self, self, …)`，而该函数体内
        /// （`CardScript__ResolveUnitSummoned.c`）先判 `op_Inequality(param_2, param_1)`
        /// （**不是自己**才发 `OtherUnitSummoned=190`）、再判 `op_Equality(param_2, param_1)`
        /// （**是自己**才发 `ThisUnitSummoned=195` / `UseFaith=605`）⇒ **预调只唤醒 `ThisUnitSummoned`
        /// 那一支**，`OtherUnitSummoned` 仍只发给别人（循环 `:34` 那句 `op_Inequality` 把主体排除）。
        /// 我们的 `When deployed, …`（省主语）记成 `Deploy + SelfOnly`（`WhenEvent.cs:847-850`），
        /// 正对应 `ThisUnitSummoned`；`When you deploy a troop, …` 是 `OtherUnitSummoned`。
        /// ⛔ **不能**用整份 `FireWhen` 做 Deploy 预调 —— 那会把主体自己的「当你部署一个部队时」
        /// 也唤醒（原版 190 是**排除**主体的），静默多触发一次。
        /// </param>
        ///
        /// <param name="allowPendingDeath">
        /// 🆕 **2026-10-21（引擎线 `T03·d` · `P2-2` 副产物）：这一跳**允许**监听者已在「待死」队列里。**
        ///
        /// 判据 = 原版广播器的**预调**那一跳在棋盘循环**之外**、**不带 `IsInPlay`**：
        ///   · `BattleManagerSupport__BroadcastUnitAttacked.c:22`（被打者）/ `:29`（攻击者）只判非 null，
        ///     循环 `:48` 才 `CardScript__IsInPlay(...)`；
        ///   · `CardScript__ResolveUnitAttacked.c:26` 也只有「接收者身上有 `RawCardScript`」一道闸。
        /// 能力层自己的闸是 `CardAbility.IsRightContext`：`InPlayOrDying(40)` 用 `IsInPlayOrDying`
        /// （`CardScript__IsInPlayOrDying.c:10-13`：**状态 5 = 待死算在场**）、`Play(0)` 用
        /// `IsInPlay`（不算）。现读当前 build（`资料/普查产出_1018/allcards_1350/cards_raw.json`）：
        /// `SW16 Long Fang`（全池唯一 `When this unit attacks …`）的 `abilityContext` = **40** ⇒
        /// 哨戒把攻击者打死之后，它自己的 `When this unit attacks` **仍然要响**。
        /// ⚠️ **只给「预调」那一跳用，而且本轮只有【攻击者】那一下传 `true`**：
        ///   · 棋盘循环那一跳必须继续挡待死单位（原版那里有 `IsInPlay`）；
        ///   · 被打者那一下（`:22`）原版同样无条件，但被打者侧的 ctx 是**混合**的
        ///     （`SAU31 Doomstalker` / `UM70 Invictor Warsuit` = 40、`TL24 Harpy` = 0）
        ///     ⇒ 2026-10-22 起**改由 `abilityContext` 逐卡判**（见下一条）。
        ///
        /// 🔴🆕 **2026-10-22（`W63` · C2）就地订正（铁律 5）**：本节原来写着
        /// 「我们这一层**没有** `abilityContext` 字段 ⇒ `Play(0)` 的同族卡（`UM83 Valtus` /
        /// `GSC12 Neophyte Iconbearer` / `DA22 Techmarine`）自己正待死时也会跟着响 …… 要精确收口
        /// 得把 `abilityContext` 带进卡数据」—— **这条已做完**：卡表 v14 已有 `abilityContext` 列
        /// （B1/B2 落地，池内 170 条真值），本函数现在把它交给 `PendingDeathAllowsListener`：
        ///   · **预调 + `Attack`** ⇒ 按 context：`40` 放开（待死算在场）· `0/10/20/30/50` 挡 ·
        ///     `-1`（旧表 / 认不出）回退**本参数**（= 改前口径）；
        ///   · **棋盘循环**（以及非 `Attack` 的预调）⇒ **不按 context**，仍只看本参数（简报 §6.6）。
        /// ⇒ `UM83/GSC12/DA22`（context 0）不再多响 · `SAU31/UM70`（context 40）不再少响。
        /// 判据逐句 → `PendingDeathAllowsListener` 的 doc。
        /// </param>
        /// <param name="actingCard">
        /// 🆕 **2026-10-25（执行代理 `E3` · `E2` 报告 §六）：原版 `CanTriggerAbility` 的 `actingCard`**
        /// —— **第一道闸**（`actingCardCriteria` `+0x28`）的被筛对象，与 `targetCard`（第二道闸）
        /// **不是同一格**。只有调用点**已经查清**这一格是谁时才传（今天 = `Die` 广播传
        /// `CleanupDeaths` 的 `actingCard` 形参 = **凶手**）。
        /// `null` = 这条路径拿不到 ⇒ 那道闸**判不了 ⇒ 放行 + 出声**（⛔ 拿 `subject` / `actor` 顶上就是筛错人）。
        /// ⚠️ `Attack` 那一族**不用这个形参**：它的 `actingCard` = 攻击者 = `subject`
        /// （判据 → <see cref="AbilityActingGateCard"/>）。
        /// </param>
        static void FireBoardWhenListener(BattleContext ctx, UnitState u, string kind, int who,
                                          CardDef card, UnitState subject, UnitState actor,
                                          UnitState target, int targetWho,
                                          bool selfOnlyOnly = false, bool allowPendingDeath = false,
                                          int pinnedOwner = -1, bool preCall = false,
                                          UnitState actingCard = null)
        {
            // ⚠️ 快照之后世界可能已经变了（前一个监听器把人打死了）——再判一次。
            // 🔴🆕 2026-10-22（`W63` · C2）：预调那一跳改成**按 `abilityContext` 判**（判据 →
            //   `PendingDeathAllowsListener` 的 doc）；棋盘循环仍走改前的状态闸 ——
            //   `!allowPendingDeath` ⇒ 待死就挡，⛔ 别整体放开（简报 §6.6）。
            if (!PendingDeathAllowsListener(ctx, u, kind, preCall, allowPendingDeath)) return;

            // 🆕 **2026-10-22（`W63` #9 · `A1562-③`）**：**`pinnedOwner >= 0` ⇒ 用快照那一刻的归属，
            //   ⛔ 不再用 `OwnerOf` 现查。**
            //   为什么：触发改入队之后，「拍快照」在**入队那一刻**、「逐条唤醒」在**出队那一刻**，
            //   中间隔着一整段队列。典型反例 = 带 `Ferocity` 的单位自己用狂暴：`FireTriggerAt`
            //   之后紧接着被洗回牌库（`RuleCore.ResolveAlternativeCore`）⇒ 出队时 `OwnerOf` 得 `-1`，
            //   它自己的 `When … triggers Ferocity` 监听器被这一行静默丢掉。
            //   原版是**同步广播**（`BroadcastUnitFerocity`，RVA `0x9B7C50`，三跳 0 条 `op_Inequality`）
            //   ⇒ 归属取**事件发生那一刻**的值才对。口径写在 `BoardListener` 的 doc 里（只此一处）。
            //   ⚠️ 缺省 `-1` = 老行为（`EmitHit` 的「被打者预调」那一跳直接调本函数，没有快照）。
            int owner;
            if (pinnedOwner >= 0) owner = pinnedOwner;
            else
            {
                owner = OwnerOf(ctx, u);
                if (owner < 0) return;                         // 已经不在场上了
            }

            // 🔴 `selfOnlyOnly`：只按 `SelfOnly` 那几条取 op（判据见上面 param 块）——
            //    ⛔ 别改 `CardDef.FireWhen` 的语义（那是棋盘那一跳的整份取法，判据只此一处）。
            List<EffectOp> ops;
            if (selfOnlyOnly)
            {
                ops = null;
                foreach (var t in u.Card.WhenTriggers)
                {
                    if (t.Ev == null || t.Ev.Kind != kind || !t.Ev.SelfOnly) continue;
                    if (!WhenEvents.Matches(t.Ev, owner, who, card, u, subject, actor, target, targetWho))
                        continue;
                    if (ops == null) ops = new List<EffectOp>();
                    ops.AddRange(t.Ops);
                }
            }
            else
            {
                ops = u.Card.FireWhen(kind, owner, who, card, u, subject, actor, target, targetWho);
            }
            if (ops == null || ops.Count == 0) return;

            var names = new List<string>();
            foreach (var t in u.Card.WhenTriggers)
                if (t.Ev != null && t.Ev.Kind == kind && (!selfOnlyOnly || t.Ev.SelfOnly))
                    names.Add(t.ToString());
            ctx.Log($"—— 事件「{kind}」触发：「{u.Name}」的监听器 → "
                  + string.Join("；", names.ToArray()) + " ——");

            // ⚠️ **每次都重种**：上一条效果会把 `LastTargets` 覆盖掉
            //    （和 `ResolveDeploy` 里那句「每一条之前都要重设」同一条教训）。
            //    ⚠️ 这里**不再自己种** —— 交给 `ResolveOps` 的 `seed` 参数，
            //       否则它会紧接着把 `source`（监听者自己）盖上去，等于白种。
            // 🆕 2026-10-19（战斗线 `T21` · 缺口 E）：**监听者真的结算了** ⇒ 发一条按
            //   **反应者卡名**的 `EvtKind.Trigger`（关键词 = 这条监听能力的原版 trigger 名）。
            //   判据：`RawCardScript__OnCardPlayedWithTarget.c:96`（`TriggerAbilities(10)`）·
            //   `CardScript__TriggeredFerocity.c:7`（726）· `CardScript__ReactToUnitMarked.c:7`（510）
            //   —— 原版在**监听者自己**的 `OnTrigger` 上转给 `TriggerAbilities`，
            //   我们的等价物就是这一条（报告 §三 E）。闸 = 那张卡真有该 trigger 的动画行。
            // 🆕 2026-10-24（`G2` · D8）：`selfOnlyOnly` 透传 —— `Deploy` 的「自己被召唤」
            //   那一跳发 `ThisUnitSummoned(195)`，其余仍 `OtherUnitSummoned(190)`（判据 → 映射方法的 doc）。
            // 🔴🆕 **2026-10-25（执行代理 `E8` · `E8-2`）：这里【不再】发 —— 挪到 `EnqueueAbility` 之后。**
            //   判据（第一权威）`RawCardScript__TriggerAbilities.c:108 / :207`：原版**先**
            //   `CardAbility.CanTriggerAbility`（两道 criteria 闸）⇒ 过了才 `TriggerAbility`
            //   → `:60 PlayTriggerTraitAnim`。我们原来把 `EmitListenerTrigger`（= 那一拍动画的事件）
            //   排在**入队闸之前** ⇒ 能力被 criteria 拒掉时动画仍播（`E6` 报告 §七·2）。
            //   ⇒ 现在用它**下面** `EnqueueAbility` 的返回值当闸（那两道闸的裁决在
            //   `ActionPump.EnqueueAbility` 里，**判据仍然只有一份**；本笔只是把「发事件」这一步
            //   挪到判决之后）。⛔ 别在这里另写一份判定 —— 那就是第二份判据。

            // ============================================================
            //  🔴 **2026-10-21（引擎线 `T03·c` 批 B 接线）：监听器这条 ability**只入队，不结算**
            //
            //  原版：`RawCardScript.OnTrigger` → `TriggerAbilities` → `AbilityLogic.PlayAbility`
            //  → `AddAutoActionToQueue`（`_ResolveBacklash_d__451` / `BroadcastUnitDamaged` 那几条
            //  广播器里，监听者的 ability 都是**入队**的）⇒ 不抢占当前动作。
            //  （判据汇总 → `资料/普查产出_1021/W61_引擎线_T03c批B判据.md` §2.3 / §2.6）
            //  ⇒ 「谁先听」仍由这一层（广播遍历序 = 进场序 `PlayOrder`）决定，
            //    「谁先结算」由队列道序决定 —— **跨道时 priority 压过 PlayOrder**，同道才轮得到它。
            //  🔴🆕 **2026-10-25（执行代理 `E8` · `E8-2`）就地订正下面这一句**：原来写
            //    「`EmitListenerTrigger` **仍在广播这一刻发**」—— 位置**改了**：现在排在
            //    `EnqueueAbility` **之后**、且**只在它返回 `true` 时**发（原版次序 =
            //    `CanTriggerAbility` → `TriggerAbility` → `PlayTriggerTraitAnim`，判据见上面那一笔）。
            //    仍然是「广播这一刻」（不等出队）—— 只是「发不发」由那两道闸说了算。
            // ============================================================
            // ⚠️ `owner` / `u` / `subject` 全部**在入队时定死**（原版把 `actingCard` /
            //    `targetCard` 一起打包进 action；我们靠闭包）。⛔ 别改成「出队时现查」——
            //    监听者可能已经被别的动作打死/离场，现查会得到 -1。
            bool queued = EnqueueAbility(ctx, ListenerLane(u.Card, TriggerNameOfWhenKind(kind, selfOnlyOnly)), (u.Name ?? "?") + "·listen:" + kind,
                           () => FireBoardListenerDeferred(ctx, u, owner, subject, kind, ops,
                                                          card, target, selfOnlyOnly),
                           // 🔴🆕 2026-10-21（审查 `W61b` 第 3 条）：施放者 = **监听者自己** `u`。
                           //   ⚠️ **但这一族【不】判「必须在场」** —— 数据现读
                           //   `OtherUnitDeath(90)` 的 `abilityContext` = `{50:11, 0:18, 40:1, 10:2}`，
                           //   而 `50` = `IsInCemetery || IsInPlay` ⇒ **死在墓地里的监听者也要响**；
                           //   `OtherCardPlayed(10)` 那一族则是 `{0:14, 10:1}`。本层判不出是哪一种
                           //   ⇒ 一律不取消（⛔ 取消会把「墓地也照听」那 11 条静默掐死）。
                           //   精确做法要读**这张卡这条 ability** 的 `abilityContext`（卡表没有该列 →
                           //   报告里的欠账）。`allowPendingDeath` 那一档（原版 40）当然更不取消。
                           actor: u, requireActorInPlay: false,
                           // 🔴🆕 **2026-10-25（执行代理 `E2` · `A3-2`）：ability 级 `targetCriteria` 闸**
                           //   原版 `CardAbility__CanTriggerAbility.c:131-135` 在这一刻（入队前）判一次，
                           //   被筛对象 = **`targetCard`**（`dump.cs:20360` 的形参表）。
                           //   ⚠️ **只有 `AbilityLevelGateApplies` 里列出的七族** 已经查清 `targetCard`
                           //     （`Attack` → `target`；`Die` / `510` / `570` / `515` / `60` / `291` → `subject`）；
                           //     其余事件一律**不启用这道闸**（⛔ 不拿 subject 猜「就是 targetCard」）。
                           //   覆盖面 / 残余 → `资料/普查产出_1025/E2_ability_criteria管线.md` ·
                           //   `资料/普查产出_1025/E6_十三行接闸.md`（2026-10-25 `E6` 补齐后 35/85 行已接）。
                           abilityTrigger: AbilityLevelGateTrigger(kind, selfOnlyOnly),
                           abilityTarget: AbilityLevelGateTarget(kind, subject, target),
                           // 🔴🆕 **2026-10-25（执行代理 `E3` · `E2` 报告 §六）：ability 级
                           //   `actingCardCriteria`（`+0x28`）—— 原版**第一道闸**，在 target 那道【之前】。
                           //   判据 `CardAbility__CanTriggerAbility.c:127-130`；⚠️ `Attack` / `Die` /
                           //   `510` / `570` / `515` 五族的 `actingCard` 查清了（`Attack` = 攻击者 = `subject`；
                           //   `Die` = 凶手 = `CleanupDeaths` 的 `actingCard` 形参，⛔ 不是死者；
                           //   后三族 = 被标记 / 被复活者 = `subject`，见 `AbilityActingGateApplies` 的注释）。
                           //   `Damaged` / `GetsStun` 缺那一格 ⇒ 不启用（⛔ 不拿 subject 顶）。
                           //   拿不到 ⇒ `null` ⇒ 那道闸**判不了 ⇒ 出声并跳过**（⛔ 不猜）。
                           abilityActingCard: AbilityActingGateCard(kind, subject, actingCard),
                           abilityActingGate: AbilityActingGateApplies(kind));

            // 🔴🆕 **2026-10-25（执行代理 `E8` · `E8-2`）：按原版次序发「这一拍真的触发了」。**
            //   `RawCardScript__TriggerAbilities.c:108/:207`：**先** `CanTriggerAbility`
            //   （= `EnqueueAbility` 里那两道 ability 级 criteria 闸）⇒ 过了才 `TriggerAbility`
            //   → `PlayTriggerTraitAnim`（`:60`）。⇒ `queued == false`（被闸拒 / 参数非法）
            //   ⇒ **连 `EvtKind.Trigger` 一起跳过**（只由 `EnqueueAbility` 那边出声）。
            //   🧨 灭自证：把这一句改回无条件调用 ⇒ 「被拒 ⇒ `EvtKind.Trigger` 0 条」那条断言红。
            if (queued) EmitListenerTrigger(ctx, u.Card, owner, u, kind, selfOnlyOnly);
        }

        /// <summary>
        /// 🆕 **2026-10-22（引擎线 `W63` · `T03·c/d` · C2/C3）：这张卡【这条 ability】的原版
        /// `abilityContext`** —— `EffectResolver` 侧的唯一取值口（带 v14 卡表「缺行 = `Play(0)`」的落值口径）。
        ///
        /// <para>**为什么需要「缺行 = 0」这一跳**：生成器 `ability_contexts_of` **只落 `context != 0` 的行**
        /// （`工具/gen_cards_engine.py:1329-1330`：`0` = `Play` 是默认值、省略不落）⇒
        /// 「表是 v14」＋「这张卡确实有这条 ability」⇒ **缺行 = `Play(0)`**。
        /// `CardDef.AbilityContextsVersioned` 正是那个「表是 v14」的正面信号
        /// （`CardDatabase.cs:613` 按 `dto.version >= 14` 置）—— 旧表 / 手搓卡（自检夹具）一律 `false`
        /// ⇒ 返回 `-1`（未知），由调用方回退今天的口径。</para>
        ///
        /// <para>⚠️ **调用前提**：调用点必须已经确认「这张卡有这条 ability」
        /// （C2 = 它正因为这个事件被唤醒；C3 = 它有 `SpiritOps` / `FaithOps` —— 2026-10-22 拿
        /// 「卡面付费前缀」近似核对：池内 28 张 `N [Spirit Stone]:` / 25 张 `N ☀:` 的卡
        /// **全部**都能在 `cards_raw.json` 里找到对应的 `trigger 600 / 605` ability；
        /// 反向的两张 `ASH52` / `SOR71` 是「有 ability 而正文形态不同」⇒ 走到这一支也仍是真 ability）。
        /// 否则缺行会被当成 `Play(0)`，等于给一条**不存在**的 ability 编口径。</para>
        ///
        /// <para>🔴 `-1` = 未知（旧表 / 认不出 trigger 名）⇒ 调用方**回退今天的口径**，
        /// ⛔ 不是 `Play(0)`（`0` 是合法取值「必须活着在场」）。</para>
        /// </summary>
        static int AbilityContextOfAbilityV14(CardDef card, string triggerName)
        {
            if (card == null) return CardDef.AbilityContextUnknown;
            // ⛔ 认不出 trigger 名 ⇒ **未知**，不许套「缺行 = 0」那一条 ——
            //   「查不到这一列」与「这条 ability 的 context 是 0」是两件事（`TriggerNameOfWhenKind`
            //   对认不出的 kind 返回 null，见它的 doc：认不出就别猜）。
            if (string.IsNullOrEmpty(triggerName)) return CardDef.AbilityContextUnknown;
            // 🔴 **2026-10-23（`W63` 收口）：本方法只剩一个转发** ——
            //   「v14 缺行 = `Play(0)`」那一跳已经收进
            //   `RuleCore.AbilityContextOfAbilityV14`（`Core/ActionPump.cs`），全仓**只此一处**；
            //   ⛔ 别在本文件里另写一份（两处写同一条规则 = 迟早不一致）。
            return RuleCore.AbilityContextOfAbilityV14(card, triggerName, null);
        }

        /// <summary>
        /// 🆕 **2026-10-22（引擎线 `W63` · C2）：棋盘监听者的【待死闸】** —— `u` 这一跳能不能被唤醒。
        ///
        /// <para>**改前**（2026-10-21 `P2-2` 副产物）：`!allowPendingDeath &amp;&amp; ctx.IsPendingDeath(u)`
        /// ⇒ **预调无条件放开、棋盘循环无条件挡**，逐卡精度拿不到。
        /// **改后**：只有**预调**（`preCall`）那一跳、且事件是 `WhenEventKind.Attack` 时，
        /// 按 **`abilityContext`** 判；其余一律**维持改前口径**。</para>
        ///
        /// <para>**判据**（`Attack` 广播器的预调两跳）：
        /// `BattleManagerSupport__BroadcastUnitAttacked.c:22`（被打者）/ `:29`（攻击者）**只判非 null**；
        /// 棋盘循环 `:48` 才 `CardScript__IsInPlay(...)` ⇒ **预调没有状态闸**，唯一的能力闸是
        /// `CardAbility.IsRightContext`（`d:/2/tools/decomp_full/CardAbility__IsRightContext.c`）：
        /// `Play(0)` = `IsInPlay &amp;&amp; !EnoughPendingDamageToDie`（**待死不算在场**）·
        /// `InPlayOrDying(40)` = 状态含 **5 待死** · `InPlayOrCemetery(50)` = **不含待死**。</para>
        ///
        /// <para>🔴 **棋盘循环（`preCall == false`）不许按 context 放开**：那里对应的是
        /// `CardScript__IsInPlay` 这道**卡状态闸**（不是 ability 层的），待死单位在原版那一趟里
        /// 同样被跳过（简报 §6.6）⇒ 这一档永远返回 `allowPendingDeath`（= 改前口径）。</para>
        ///
        /// <para>🔴 `-1`（未知 / 旧卡表）⇒ 返回 `allowPendingDeath` = **改前口径**
        /// （攻击者预调照放、被打者与其余预调照挡）—— 「今天不传 = 行为逐字不变」。</para>
        ///
        /// ⚠️ 本轮**只对 `Attack` 放开 context**：`Damaged` 的预调（`RuleCore.EmitHit` 那一跳）
        /// 原版同样没有状态闸（`BattleManagerSupport__BroadcastUnitDamaged.c:19`），但简报把范围
        /// 限定在「预调两跳」⇒ 它**保持改前口径**，记进报告当残余。
        /// </summary>
        static bool PendingDeathAllowsListener(BattleContext ctx, UnitState u, string kind,
                                               bool preCall, bool allowPendingDeath)
        {
            if (ctx == null || u == null) return true;
            if (!ctx.IsPendingDeath(u)) return true;            // 不在待死队列 ⇒ 这道闸不关它
            if (!preCall || kind != WhenEventKind.Attack) return allowPendingDeath;   // 见 doc：不改
            int actx = AbilityContextOfAbilityV14(u.Card, TriggerNameOfWhenKind(kind));
            if (actx == CardDef.AbilityContextInPlayOrDying) return true;    // 40 = 含待死 ⇒ 放
            if (actx != CardDef.AbilityContextUnknown) return false;         // 0/10/20/30/50 都不含待死
            return allowPendingDeath;                                        // `-1` 契约：回退改前口径
        }

        /// <summary>
        /// <see cref="FireBoardWhenListener"/> 的**队列体** —— 泵点取到这一条时才跑
        /// （2026-10-21 引擎线 `T03·c` 批 B 接线抽出）。
        ///
        /// ⚠️ 代码体逐句搬自原来的同步段（`EffectChain` 夹住 `ResolveOps` + 尾部
        /// `followAfterResolution` 那一跳），⛔ 别在这里改次序。
        /// ⚠️ `ops` 是**广播那一刻**选出来的（`FireWhen` 现算的新列表，不是缓存），
        ///    随队列动作一起带走 —— 原版 `GetAbilitiesForTrigger` 也是在 `OnTrigger` 里先选完再排队。
        /// </summary>
        static void FireBoardListenerDeferred(BattleContext ctx, UnitState u, int owner,
                                              UnitState subject, string kind, List<EffectOp> ops,
                                              CardDef card, UnitState target, bool selfOnly = false)
        {
            // 🔴 **事件槽必须在这里重种一次**（`EventSlotsScope`）：广播那边那个 `using` 在
            //    **入队之后立刻退出**、把 `EventTarget` / `EventCard` 还原了；队列体在另一个时刻跑，
            //    不重种的话 `create a copy of it` / `the target of the attack` 会读到**调用方的旧值**。
            //    判据 = 原版把 `actingCard` / `targetCard` **打包进 action**（`RawScript.OnTrigger` 的形参表）。
            using (EventSlotsScope.TargetAndCard(ctx, target, card))
            {
                ctx.EffectChain++;
                try { ResolveOps(ctx, owner, u, ops, "事件触发", subject); }
                finally { ctx.EffectChain--; }
            }

            // 🆕 2026-10-19（`T21` · 缺口 A 的另一半）：这张监听卡自己的能力里带
            //   `followAfterResolution(320)` ⇒ **结算完这一条监听之后**再发 410 那一拍。
            //   现读只有两张走这条路：`GSC14 Acolyte Leader`（10）· `ASH81 Bright Lance Vyper`（455）。
            if (!ctx.IsOver)
            {
                string sched = FollowUpSchedulerOf(u.Card);
                // 🆕 2026-10-24（`G2` · D8）：`selfOnly` 透传 —— 与上面那条事件用**同一个**
                //   trigger 名判（`Deploy` 的 190/195 分流），否则「监听者那一条 410」会按错名字比。
                if (sched != null && sched == TriggerNameOfWhenKind(kind, selfOnly))
                    EmitFollowUpResolution(ctx, u.Card, owner,
                                           FindSlot(ctx, u, out _, out int fuSlot) ? fuSlot : -1,
                                           u.InstanceId);
            }
        }

        /// <summary>
        /// **「先预调主体、再广播其余」里的那一半『预调』** —— 🆕 2026-10-20（`W58` · BROADCAST）。
        ///
        /// 原版每一族「有主体预调」的广播器都同一个形状：**同一个接收函数**先对主体调一次，
        /// 再按棋盘序把其余接收者调一遍（循环里用 `op_Inequality` 跳过主体）。⇒ 我们这边
        /// 「预调」与「棋盘那一跳」**必须是同一个函数**（<see cref="FireBoardWhenListener"/>），
        /// 这里只补齐它周围那三件事（判空 / `EffectChain` 闸 / 事件槽），**判据不再写第二份**。
        ///
        /// 🔴 **`EffectChain` 上限必须在这里再判一次**：预调**绕过了** `BroadcastWhen` 入口
        /// 那道闸（它只在函数开头判 `ctx.EffectChain >= MaxEffectChain`）。不判的话，
        /// 「监听器再造成伤害/再触发」这条环在到达上限后仍会经预调继续往下走 ⇒ **无界递归**
        /// （D8 的离线探针实测爆栈；`RuleEngineTest` 有递归守卫用例）。
        /// 判法与 `BroadcastWhen` 入口一字不差。
        ///
        /// ⚠️ **事件槽与紧接着那次广播同形**（`target` / `card` 两个实参原样传）——
        /// 原版预调那一跳带的就是同一个事件宾语与同一张来源卡。
        ///
        /// 🆕 **`allowPendingDeath`**：照原版，**预调那一跳不判 `IsInPlay`** ⇒ 主体已经在待死队列里也照跑
        /// （本轮**只有 `Attack` 的【攻击者】那一下**传 `true`；判据全文 → `FireBoardWhenListener` 的 param 块）。
        ///     ⚠️ 棋盘循环那一跳**不许**传它 —— 原版那里逐张判 `IsInPlay`。
        ///
        /// 🔴🆕 **2026-10-22（`W63` · C2）**：本函数内层那一跳现在传 **`preCall: true`** ——
        ///     这是「预调」的**唯一标记**（`FireBoardWhenListener` 的待死闸靠它区分「预调」与
        ///     「棋盘循环」，见 `PendingDeathAllowsListener` 的 doc）。
        ///     ⚠️ 判别本身仍**只认 `kind == Attack`**（本轮只放开原版那两跳）；其余预调
        ///     （`Deploy` / `Damaged` / `GetsHuntMark` …）行为与改前逐字相同。
        /// </summary>
        static void PreCallBoardListener(BattleContext ctx, UnitState u, string kind, int who,
                                         CardDef card, UnitState subject, UnitState actor,
                                         UnitState target, int targetWho, bool selfOnlyOnly = false,
                                         bool allowPendingDeath = false)
        {
            if (u == null || ctx == null || kind == null) return;
            if (ctx.IsOver || ctx.EffectChain >= BattleContext.MaxEffectChain) return;
            using (EventSlotsScope.TargetAndCard(ctx, target, card))
                FireBoardWhenListener(ctx, u, kind, who, card, subject, actor, target, targetWho,
                                      selfOnlyOnly, allowPendingDeath, preCall: true);
        }

        /// <summary>
        /// **常驻效果里的「当……时」监听段**（🆕 2026-09-14 A4 批 3）。
        ///
        /// 卡面：`For the rest of this battle, when a friendly troop uses Ferocity, deal 2 damage
        /// to the enemy Warlord`（`Raid Tactics`，SpaceWolves，4 费）。
        ///
        /// 🔴 **和棋盘上的监听器（`UnitState.Card.WhenTriggers`）是两条路**：
        ///    那些是**实体卡**在听（`CardDef.CanListenForEvents =&gt; IsUnit`），
        ///    这些是**一张已经进弃牌堆的战术卡**留下的常驻效果在听 —— 棋盘上没人可挂。
        ///
        /// 🔴 **出处**：
        ///   · 事件名 —— `rule_core.gd:3493` `["ally_ferocity","you", r"a friendly (?:unit|troop)
        ///     (?:triggers?|uses?) (?:a )?ferocity"]`，广播在 `:2382`。
        ///   · 原版的广播器 `BattleManagerSupport__BroadcastUnitFerocity.c`（**完整体**）：
        ///     发 `TriggerAbilitySignal(Ferocity=725)` → **场上在演的每张牌** → **当前回合方手牌** →
        ///     **另一方手牌**，逐个调 `CardScript.TriggeredFerocity`(=726)。
        ///     ⚠️ 原版也是「**牌**在听」，而这张卡的监听者是**已进弃牌堆的战术卡** ——
        ///     那一端在原版**同样找不到对应**（`BroadcastTacticPlayed` 也带 `IsInPlay` 检查）。
        ///     ⇒ 我们这条是**照卡面字面做的**，容器形状是本工程自定的，**如实标着**。
        ///
        /// ⚠️ **判据共用 `WhenEvents.Matches`**（不另写一套）；`pe.Owner` 当监听者、
        ///    `who` 是事件发生的那一方 —— 卡面的 `friendly` 是**相对打出这张卡的人**说的。
        /// ⚠️ 遍历用下标 + 取快照长度：结算里可能**新增**常驻效果，不该在同一趟里被扫到。
        /// ⚠️ 代词（`LastTargets`）与棋盘那段**一样**要种要还原（`subject` = 发生这件事的那个单位）。
        /// </summary>
        static void BroadcastPersistentWhen(BattleContext ctx, string kind, int who,
                                            UnitState subject, UnitState actor, UnitState target, int targetWho)
        {
            if (ctx.PersistentEffects.Count == 0) return;
            int n = ctx.PersistentEffects.Count;          // 快照：结算里新增的不进这一趟
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                var pe = ctx.PersistentEffects[i];
                if (pe.Trigger != "when" || pe.Ev == null || pe.Ev.Kind != kind) continue;
                if (pe.Ops == null || pe.Ops.Count == 0) continue;
                if (!WhenEvents.Matches(pe.Ev, pe.Owner, who, pe.Source, null,
                                        subject, actor, target, targetWho)) continue;
                any = true;
                break;
            }
            if (!any) return;                             // 没人听 ⇒ 一个字节都不动

            // 🔴 **2026-10-18（`G3` 真差异 ③）**：种/还原统一走 `EventSlotsScope`（原来这里手写一份）
            //   —— `TargetOnly`：这一跳**不碰** `EventCard`（它原来也不碰），别顺手清成 null。
            using (EventSlotsScope.TargetOnly(ctx, target))
            {
                for (int i = 0; i < n; i++)
                {
                    var pe = ctx.PersistentEffects[i];
                    if (pe.Trigger != "when" || pe.Ev == null || pe.Ev.Kind != kind) continue;
                    if (pe.Ops == null || pe.Ops.Count == 0) continue;
                    if (!WhenEvents.Matches(pe.Ev, pe.Owner, who, pe.Source, null,
                                            subject, actor, target, targetWho)) continue;

                    ctx.Log($"—— 事件「{kind}」触发**常驻效果**：「{pe.Body}」"
                          + $"（{ctx.Players[pe.Owner].Name}）——");
                    // 🆕 2026-10-19（`T21` · 缺口 E，第四跳）：常驻效果的**来源卡**自己也有
                    //   `cardAnimations[]`（典型 = 已进弃牌堆的战术卡 `Raid Tactics`）⇒ 给它发一条
                    //   按它卡名的事件。⚠️ 它**不在棋盘上** ⇒ `Slot = -1`（表现层今天没有
                    //   弃牌堆锚，报告 §四 如实记「仍不可达」）。
                    // 🔴🆕 **2026-10-25（执行代理 `X3`）：这一跳也接 ability 级闸、且按闸的裁决发事件。**
                    //   判据 = 同一份原版次序 `RawCardScript__TriggerAbilities.c:108/:207`
                    //   （**先** `CanTriggerAbility` 两道 criteria ⇒ 过了才 `TriggerAbility` →
                    //    `:60 PlayTriggerTraitAnim`）—— 与 `E8-2` 改的棋盘那一跳**逐字同一条**，
                    //   `E8` 报告 §九·5 点名「将来给它们接线时必须一起改成同样的先后」。
                    //   宿主卡 / 座位：这条能力的 `thisCard` = **写出这条常驻效果的那张卡**
                    //   （`pe.Source`），座位 = `pe.Owner`（`E9` 给战术卡开的那两个实参通道）。
                    //   ⚠️ `abilityActingGate` 传 `false`：本路径**拿不到** `actingCard`
                    //      （`BroadcastPersistentWhen` 只有 `actor` 形参，`Die` 那一族的真凶手在
                    //      `BroadcastWhen` 的 `actingCard` 里、没往下传）⇒ 按 `E3` 的契约
                    //      「不确定是哪一格 ⇒ 不启用」，⛔ 不拿 `actor`/`subject` 顶。
                    //   🔴 2026-10-21（`T03·c` 批 B 接线）：**常驻效果这条 ability 也只入队**
                    //      （原版走同一个 `PlayAbility → AddAutoActionToQueue`）。
                    //      `EventSlotsScope` 与 `CardPlaySource` 的种/还原**一起搬进队列体**（见那边）。
                    var _pe = pe;                      // 循环变量按帧捕获（C# 5 起 foreach 才是逐帧；这里是 for）
                    bool pqueued = EnqueueAbility(ctx, ListenerLane(_pe.Source, TriggerNameOfWhenKind(kind)), "常驻:" + kind,
                                   () => PersistentListenerDeferred(ctx, _pe, kind, subject, target),
                                   abilityTrigger: AbilityLevelGateTrigger(kind, false),
                                   abilityTarget: AbilityLevelGateTarget(kind, subject, target),
                                   abilityActingGate: false,
                                   abilityHostCard: _pe.Source, abilityOwnerSeat: _pe.Owner);
                    // 🧨 灭自证：把 `if (pqueued)` 改成无条件调用 ⇒「被闸拒 ⇒ 0 条事件」那条断言红。
                    if (pqueued) EmitListenerTrigger(ctx, _pe.Source, pe.Owner, null, kind);
                    if (ctx.IsOver) break;
                }
            }
        }

        /// <summary>
        /// <see cref="BroadcastPersistentWhen"/> 的**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）。
        ///
        /// 原同步段的代码逐句搬进来，只多做两件「跨时刻」的事：
        ///   · **重种事件槽** —— 广播那边 `EventSlotsScope.TargetOnly` 的 `using` 在**入队后立刻退出**，
        ///     队列体在另一个时刻跑，不重种 `the target of the attack` 会读到旧值；
        ///   · **重种 `ctx.CardPlaySource`** —— 原注释：这次结算的能力属主 = 写出这条常驻效果的卡
        ///     （`pe.Source`），⛔ 不是「本局最后打出的那张」。
        /// ⚠️ `EffectChain` 的 `++/--` 也一起搬到这里（它量的是**嵌套深度**，不是排队长度）。
        /// </summary>
        static void PersistentListenerDeferred(BattleContext ctx, PersistentEffect pe, string kind,
                                               UnitState subject, UnitState target)
        {
            using (EventSlotsScope.TargetOnly(ctx, target))
            {
                ctx.EffectChain++;
                var prevPlaySource = ctx.CardPlaySource;
                ctx.CardPlaySource = pe.Source;
                // 🔴 **2026-10-22（`W62/D5②`）**：`CreationSource` 与它**同一条判据、同一段作用域** ——
                //   这条常驻效果 / 手牌监听造牌时，原版的 `creator` 就是写出这条效果的那张卡
                //   （`AbilityLogic__PlayAbility.c` 传 `AbilityData.thisCard`；这里 = `pe.Source`）。
                var prevCreationSource = ctx.PushCreationSource(pe.Source);
                try { ResolveOps(ctx, pe.Owner, null, pe.Ops, "常驻效果", subject); }
                finally
                {
                    ctx.EffectChain--;
                    ctx.CardPlaySource = prevPlaySource;
                    ctx.CreationSource = prevCreationSource;
                }
            }
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
        ///   ① 🔴 **2026-10-17 更正：这一条已不成立 —— 现在按卡实例匹配，不是按卡 id。**
        ///      原文写的是「**按卡 id 匹配**（`CostMod.Key = CardDef.Id`）……手里两张同名卡会
        ///      **一起**降价」。那描述的是 **2026-09-18 第 7 行第 3 步之前**的行为；
        ///      现在函数体里登记的是一条 `Key = "*"` + `HandInstanceId = handInst.Id` 的修正
        ///      （见下面那段「🔴 第 7 行第 3 步：**钉在这一份上**」），**只有手里那一份**看得见
        ///      （`CostOf(ctx, p, 实例)` 才读得到、`CostOf(ctx, p, 卡模板)` 会跳过它 ——
        ///      口径见 `资料/卡实例身份_爆炸半径.md` §八·3）。
        ///      **错因**：改了代码没回头改成员注释 ⇒ 同一件事两份说法打架。
        ///      ⚠️ `CardDef._costWhens` 仍是**卡表级**的（这一半没变）—— 变的是**登记出来的修正
        ///         钉在哪一份上**。
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
                    var handInst = hand[i];            // 第 7 行第 3 步：降的是**手里这一份**
                    var c = handInst.Card;
                    if (c == null || c.CostWhens.Count == 0) continue;

                    // `listener` 传 p —— 手牌属于谁，极性（friendly / enemy）就相对谁说。
                    // 判据全在 `WhenEvents.Matches`（**只此一份**），这里不再自己判一次。
                    var hits = c.FireCostWhen(kind, p, who, card);
                    if (hits == null) continue;

                    // 🆕 2026-10-19（`T21` · 缺口 E，第五跳）：**事件触发式降费**也是「监听者真的结算」
                    //   （现读 `AM56 Vengeance for Cadia` 有 `OtherUnitDeath` 的动画行）。
                    //   ⚠️ `Slot = -1`（它在手里 / 弃牌堆，不在棋盘上）。
                    // 🔴🆕 **2026-10-25（执行代理 `X3`）如实标的缺口**：这一路**没有 `EnqueueAbility`**
                    //   —— 降费是「登记一条 `CostMod`」（= 原版 `SetupCardInHand` 挂到牌上的那条修正），
                    //   不是一条进入动作队列的 ability ⇒ **没有「入队闸的返回值」可以当裁决**。
                    //   原版这条修正到底是不是一条 `CardAbility`（会不会过 `CanTriggerAbility` 那两道
                    //   criteria）**判据没查到**（`PlayerHand__SetupCardInHand.c:60-73` 只证「按
                    //   `TargetCriteria` 把效果挂到牌上」，没证它在触发那一刻还过一道闸）。
                    //   ⇒ 本路径**维持改前**：无条件发事件。⛔ 别为了对齐形状在这里硬造一个入队点
                    //      （那会让降费变成「排队结算」，是**行为变化**，且没有判据支撑）。
                    EmitListenerTrigger(ctx, c, p, null, kind);

                    foreach (var h in hits)
                    {
                        ctx.CostMods.Add(new CostMod
                        {
                            Player = p,
                            // 🔴 第 7 行第 3 步：**钉在这一份上** —— 原来 `Key = c.Id` 会让
                            //    手里所有同名副本一起降价（`D-10` 那一族）。
                            Key = "*",
                            HandInstanceId = handInst.Id,
                            Delta = -h.Delta,
                            ExpireTurn = -1,
                        });
                        ctx.Log($"—— 事件「{kind}」触发：「{c.Name}」在手里监听 → 费用 -{h.Delta}"
                              + $"（现价 {RuleCore.CostOf(ctx, p, handInst)}）——");
                    }
                }
            }
        }

        /// <summary>
        /// **手牌陷阱监听段** —— `When &lt;事件&gt;, …` 那一族（`CardDef.HandTrapWhens`）。
        ///
        /// 🆕 2026-09-14 A4 批 4。出处：`Jammed Communications`（Genestealers 的**破坏卡**，
        /// 卡面橙字 `Sabotage`）：`When you play a Stratagem, your Warlord takes 1 damage`。
        /// 破坏卡是**塞进对手手牌**的（规则书 `:204`）⇒ 「你」= **持有者**，
        /// 与同族 `Poisoned Supplies`（`At the end of your turn, your troops take 1 damage`）
        /// 已经实现的口径一致。
        ///
        /// 🔴 **「手牌里的牌监听事件」这件事原版有对应机制**（2026-09-14 查实，纠正了
        /// `资料/战术卡剩余7条_语义查证.md` §七 原来那句「原版也找不到对应」）：
        ///   · `BattleManagerSupport__BroadcastCardPlayed`（`decomp_out/`）**会遍历双方手牌** ——
        ///     在场上 → 当前回合方手牌 → 另一方手牌，逐个 `CardScript.ResolveCardPlayed`；
        ///   · `RawCardScript` 上有一对**陷阱专用钩子** `OnTrapDrawn` / `OnTrapResolved`。
        ///   ⚠️ **但这是旁证，不是直证**：原版卡数据在远程 CCD（`BattleManager__BroadcastTacticPlayed`
        ///   那条**只**通知在场上、而且门槛是 `artifice` 熟悉），我们**没法证明这张卡走的就是那条路**。
        ///   ⇒ 如实标着「机制形状有据、这张卡走哪条路无据」。
        ///
        /// ⚠️ **排在场上监听器之前**（和 <see cref="BroadcastCostWhen"/> 同一条理由）：
        ///    先扫的是「事件发生的那一刻就在手里的牌」。
        /// ⚠️ `listener` 传 `p`（手牌属于谁）—— 极性（friendly / enemy）相对持有者判，
        ///    判据全在 `WhenEvents.Matches`（**只此一份**）。
        /// </summary>
        static void BroadcastHandTrapWhen(BattleContext ctx, string kind, int who, CardDef card,
                                          UnitState subject, UnitState actor, UnitState target,
                                          int targetWho, bool drawMode = false, CardInstance drawnCard = null)
        {
            for (int hop = 0; hop < 2; hop++)
            {
                int p = drawMode ? (ctx.Active == 1 ? 1 : 0) : hop; var hand = ctx.Players[p].Hand;
                for (int i = 0; i < hand.Count; i++)
                {
                    var c = hand[i].Card;          // 第 7 行第 2 步：手牌存实例，这里只要模板
                    if (c == null || c.HandTrapWhens.Count == 0 || (drawMode && ReferenceEquals(hand[i], drawnCard))) continue;

                    // ⚠️ **先快照 ops 再结算** —— 触发会改手牌（能抽牌、能把牌拿走），
                    //    边遍历边读 `hand` 是未定义行为（和 `ResolveDeploy` 同一条教训）。
                    var ops = c.FireHandTrapWhen(kind, p, who, card, subject, actor, target, targetWho);
                    if (ops == null || ops.Count == 0) continue;

                    var names = new List<string>();
                    foreach (var t in c.HandTrapWhens)
                        if (t.Ev != null && t.Ev.Kind == kind) names.Add(t.ToString());
                    ctx.Log($"—— 事件「{kind}」触发：「{c.Name}」**在 {ctx.Players[p].Name} 手里**监听 → "
                          + string.Join("；", names.ToArray()) + " ——");

                    if (ctx.EffectChain >= BattleContext.MaxEffectChain)
                    {
                        ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，"
                              + $"「{c.Name}」的手牌陷阱不再连锁");
                        continue;
                    }

                    // `source` 传 null（手牌里的牌**没有 `UnitState`** —— 它不在棋盘上），
                    // `owner` 传持有者 ⇒ 正文里 `your Warlord` 那类 `own/…` 目标是**持有者**的。
                    // 🆕 2026-10-19（`T21` · 缺口 E，手牌陷阱那一跳）：同上，按**那张手牌**发事件。
                    //   ⚠️ `Slot = -1`（不在场上；表现层今天没有手牌锚 —— 报告 §四）。
                    // 🔴🆕 **2026-10-25（执行代理 `X3`）：发事件改到 `EnqueueAbility` 之后、按它的裁决发**
                    //   （判据 = `RawCardScript__TriggerAbilities.c:108/:207` 的「先闸后动画」，
                    //    与 `E8-2` 改的棋盘那一跳同一条；`E8` 报告 §九·5 点名要一起改）。
                    //   宿主卡 / 座位 = 这张手牌自己 + 它的持有者（原版这一跳的 `thisCard` = 那张牌）。
                    // 🔴 2026-10-21（`T03·c` 批 B 接线）：**手牌陷阱这条 ability 也只入队**
                    //   —— 原版走同一个 `PlayAbility → AddAutoActionToQueue`（判据同上）。
                    //   `ops` / `p` / 持有者在入队这一刻定死（原版把上下文打包进 action）。
                    // 🔴 **2026-10-22（`W62` · R4）**：上面那句「`p` 在入队这一刻定死」是**假的** ——
                    //   闭包捕获的是 **`for` 的循环变量本身**（C# 5 起只有 `foreach` 才是逐帧捕获），
                    //   循环跑完 `p == 2` ⇒ 出队时 `ctx.Players[2]` ⇒ `IndexOutOfRangeException`
                    //   （`HandTrapListenerDeferred → ResolveOps` 的 `warlord` 支）。
                    //   照同族 `:7458` 的 `var _pe = pe;` 同形修：**入队这一刻拷一份**。
                    var _p = p;                        // 循环变量按帧捕获（for 不是逐帧）
                    var _opsT = ops;
                    bool htqueued = EnqueueAbility(ctx, ListenerLane(c, TriggerNameOfWhenKind(kind)), "手牌陷阱:" + kind,
                                   () => HandTrapListenerDeferred(ctx, _p, _opsT),
                                   abilityTrigger: AbilityLevelGateTrigger(kind, false),
                                   abilityTarget: AbilityLevelGateTarget(kind, subject, target),
                                   abilityActingGate: false,      // 本路径拿不到 `actingCard`（同常驻那一跳）
                                   abilityHostCard: c, abilityOwnerSeat: _p);
                    // 🧨 灭自证：把 `if (htqueued)` 改成无条件调用 ⇒「被闸拒 ⇒ 0 条事件」那条断言红。
                    if (htqueued) EmitListenerTrigger(ctx, c, _p, null, kind);
                }
            }
        }

        /// <summary>
        /// **手牌监听段（单位卡那一支）** —— `When &lt;事件&gt;, …`（`CardDef.WhenTriggers`），
        /// 监听者是**一张躺在手牌里的单位卡**。
        ///
        /// 🆕 **2026-10-18（`A887`）补的「广播的最后一跳」。** 在此之前 `BroadcastWhen`
        /// **只扫棋盘**（`ctx.Players[p].Board[s]`）⇒ 手里的单位卡收不到任何事件，
        /// 而卡面**照旧不打 `*`**（正文是好的）—— 正是本工程点名的静默失效。
        ///
        /// **原版出处**：`BattleManagerSupport__BroadcastUnitSummoned.c` 的**第 3、4 跳** ——
        ///   · `:43-52` `GetCardsInHandRef(IsPlayerTurn())` 逐张 `:51 ResolveUnitSummoned`；
        ///   · `:53-70` `GetCardsInHandRef(!IsPlayerTurn())` 逐张 `:63`。
        ///   ⇒ **两跳**：**先当前回合方的手牌、再另一方**（本函数按这个顺序遍历）。
        /// ⚠️ **这条「两跳」是被订正过的口径**：`资料/事件层_数据与设计.md` §五 原来把
        ///   「监听范围不判回合方」记成**我们挑的**（当时我们只有棋盘那一支）；现在按原版分成两跳，
        ///   那份文档同一行**已就地订正**（2026-10-18）。
        ///
        /// 🔴 **接收端真的会响（已证）**：`CardScript__ResolveUnitSummoned.c` 的闸是
        ///   `cardState != 5`（`:21/:36/:55`），而 `CardStateOptions.waitingToDie = 5`
        ///   ⇒ **`inHand` 不被排除**；`:25-34` 对「被召唤的那张 ≠ 自己」**无条件**
        ///   `OnTrigger(0xbe = 190 = OtherUnitSummoned)`。
        ///   （「= 自己」那一支是 `:40-53`，对应我们的 `WhenEvent.SelfOnly` —— 手牌里的牌
        ///    **没有 `UnitState`** ⇒ 那一支天然不触发，与 `FireHandTrapWhen` 同一条口径。）
        ///
        /// 🔴 **只扫单位卡**：跳过条件用 `CardDef.CanListenForEvents`（`= IsUnit`，
        ///   登记侧 `<see cref="CardDef"/>` 的 `AddWhenTrigger` 用的就是同一道闸）。
        ///   ⛔ **不能不判这一条**：非单位卡那半句 `When …` 是**手牌陷阱**
        ///   （`CardDef.HandTrapWhens`），由 <see cref="BroadcastHandTrapWhen"/> 消费 ——
        ///   两边都扫的话同一张卡会被结算两次。
        ///
        /// 🔴 **2026-10-18（`S7` · `A962` ①）就地订正（铁律 5）**：这一段原来写
        ///   「**排序：必须排在 `BroadcastWhen` 里那句『场上没人听就 `return`』之前**」——
        ///   **那句早退 2026-10-18（`W5`）已经不存在了**（棋盘那一跳改成 `if (listeners.Count > 0) { … }`，
        ///   四条非棋盘跳**无条件**跑）⇒ 本条约束**已经没有对应物**。
        ///   现在的次序判据 = **原版三跳「自己 → 棋盘 → 手牌」**（⓪ / ⓪-b / ⓪-c / ⓪-d 一律排在
        ///   **棋盘那一跳之后**，见 `BroadcastWhen` 里「② / ③」两段注释；出处
        ///   `BattleManagerSupport__BroadcastUnitSummoned.c:21/:25-39/:44-64`）。
        ///   这一段里**一字未改仍然成立**的只有：**它与「自家场上有没有单位」毫无关系**。
        ///
        /// ⚠️ **照原版扫「双方手牌」**，不是只扫打牌那一方：`GetCardsInHandRef` 两跳合起来
        ///   就是**两边都扫**，极性（`friendly` / `enemy`）由 `listener` 传持有者 `p` 交给
        ///   `WhenEvents.Matches` 判（**判据只此一份**，这里不再自己判一次）。
        /// </summary>
        static void BroadcastHandWhen(BattleContext ctx, string kind, int who, CardDef card,
                                      UnitState subject, UnitState actor, UnitState target,
                                      int targetWho, bool drawMode = false, CardInstance drawnCard = null)
        {
            // ---- ⓪ 廉价的预扫：两边手牌里一张「会听的单位卡」都没有就一个字节都不动 ----
            //   （和 `BroadcastPersistentWhen` 那句「没人听 ⇒ 一个字节都不动」同一条纪律：
            //     这个函数在每次伤害/攻击/部署上都会被调，别让它白白建快照。）
            bool any = false;
            for (int p = 0; p < 2 && !any; p++)
            {
                var h = ctx.Players[p].Hand;
                for (int i = 0; i < h.Count; i++)
                {
                    var c0 = h[i] != null ? h[i].Card : null;
                    if ((c0 != null && c0.CanListenForEvents && c0.WhenTriggers.Count > 0) || (drawMode && h[i] != null && h[i].ArmedTrap != 0)) { any = true; break; }   // 后半截 = 账 250（第三批）：被抽的**武装陷阱**自己那一跳与「有没有单位卡会听」无关，别被这里的早退吞掉
                }
            }
            if (!any) return;

            // 代词槽（`LastTargets`）与事件槽（`EventTarget` / `EventCard`）**和棋盘那段一样**
            // 要种要还原 —— 起因见 `BroadcastWhen` 的 `subject` / `target` 两段注释。
            // 🔴 **2026-10-18（`G3` 真差异 ③）**：种/还原统一走 `EventSlotsScope`（原来这里手写一份）。
            using (EventSlotsScope.TargetAndCard(ctx, target, card))
            {
                // ---- ① 两跳：先当前回合方的手牌，再另一方（原版 `:43-52` / `:53-70`）----
                // `drawMode`（= 账 627 `BroadcastCardDrawn`）走**三趟**，见 `DrawHandHopTakes` 的 doc
                int active = ctx.Active == 1 ? 1 : 0; int hops = drawMode ? 3 : 2;
                for (int hop = 0; hop < hops; hop++)
                {
                    int p = drawMode ? active : (hop == 0 ? active : 1 - active);
                    var hand = ctx.Players[p].Hand;
                    // ⚠️ **遍历取下标的实时 `hand.Count`**：触发会改手牌（能抽牌、能把牌拿走）。
                    //    **每张卡先快照 ops 再结算**（下面 `ResolveOps` 用的 `ops` 已经是快照），
                    //    与 `BroadcastHandTrapWhen` / `ResolveDeploy` 同一条教训。
                    for (int i = 0; i < hand.Count; i++)
                    {
                        var inst = hand[i];
                        var c = inst != null ? inst.Card : null;
                        if (drawMode && hop == 0 && drawnCard != null && ReferenceEquals(inst, drawnCard) && EmitTrapDrawnInsteadOfCardDrawn(ctx, inst, p)) continue;   // 账 250（第三批）：被抽的**武装陷阱**发 250 并**取代它的 260**（判据 `CardScript__CardDrawn.c:16-31`；⛔ 只有这一跳能走到 —— 棋盘跳与两趟手牌跳都只传 `!= drawnCard`）
                        if (c == null || !c.CanListenForEvents || c.WhenTriggers.Count == 0 || (drawMode && !DrawHandHopTakes(hop, inst, drawnCard))) continue;

                        // ⚠️ `listener` 传 `p`（**手牌属于谁**）· `listenerUnit` 传 `null`
                        //    （手里的牌没有 `UnitState` ⇒ `SelfOnly` / `ActorSelf` 那两族
                        //     天然不触发，与 `FireHandTrapWhen` 同一条口径：宁可收不到）。
                        var ops = c.FireWhen(kind, p, who, card, null, subject, actor, target, targetWho);
                        if (ops == null || ops.Count == 0) continue;

                        var names = new List<string>();
                        foreach (var t in c.WhenTriggers)
                            if (t.Ev != null && t.Ev.Kind == kind) names.Add(t.ToString());
                        ctx.Log($"—— 事件「{kind}」触发：「{c.Name}」**在 {ctx.Players[p].Name} 手里**监听 → "
                              + string.Join("；", names.ToArray()) + " ——");

                        if (ctx.EffectChain >= BattleContext.MaxEffectChain)
                        {
                            ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，"
                                  + $"「{c.Name}」的手牌监听器不再连锁");
                            continue;
                        }

                        // 🆕 2026-10-19（`T21` · 缺口 E，手牌单位卡那一跳）：按**那张手牌**发事件。
                        //   放到 selfOps/restOps 分叉**之前** —— 「正文没写主语」那一档同样是一次
                        //   真的结算（它把加成挂到了手里那张牌上）。⚠️ `Slot = -1`（不在场上）。
                        // 🔴🆕 **2026-10-25（执行代理 `X3`）：这一句挪到【分叉之后】**（见下面 `if`）——
                        //   入队那条支要按 `EnqueueAbility` 的裁决发；只有 `selfOps` 那一支没有入队口，
                        //   照旧无条件发（它确实结算了）。判据同 `E8-2`（先闸后动画）。

                        // `source` 传 null（手牌里的牌**没有 `UnitState`** —— 它不在棋盘上），
                        // `owner` 传持有者 `p` ⇒ 正文里 `your Warlord` 那类 `own/…` 目标是**持有者**的；
                        // `seed` 传 `subject` ⇒ 卡面的 `it` 指的是**发生那件事的那个单位**
                        // （刚部署的那台载具 / 刚死的那个兵），不是监听者自己。
                        //
                        // 🔴 **2026-10-18（`A887` 的续）：正文【没写主语】的那几条不再洒给全队**
                        //    （这一段原来如实标着「真偏离、没修」—— 现在修了，铁律 11）。
                        //    原版里手牌那张牌**自己就是 `CardScript`（acting card）** ⇒
                        //    「没写主语」的效果落到**它自己**身上。到我们这一侧，
                        //    「它自己」= **那一份手牌实例**（`CardInstance.HandEffects`，
                        //    = 原版 `CardScript +0x108`）——`A885` ② 已经把「进手牌挂效果、
                        //    打出时兑现」那条路落好了（`AttachHandEffect` / `RuleCore.ApplyHandBuffs`）
                        //    ⇒ 那张牌**打出时**带上这条加成。
                        //    ⛔ **不是**「塞一个临时的『手牌实例目标』」—— 这里**一个假 `UnitState`
                        //    都不造**（手牌里的牌没有 `UnitState`，硬造一个就是自欺）。
                        //    判据 = `EffectTargetSpec.Subjectless`（卡面**没写主语**那个旗标，
                        //    正是「卡面没写主语：有施放者就是施放者自己」用的同一个）。
                        //    ⚠️ 只收 `give` / `gain` / `lose` 三条（判据与理由见 `HandListenerSelfOp`），
                        //    其余**照旧**走 `ResolveOps`。
                        var selfOps = new List<EffectOp>();
                        var restOps = new List<EffectOp>();
                        foreach (var o in ops)
                            (HandListenerSelfOp(o) ? selfOps : restOps).Add(o);
                        foreach (var o in selfOps)
                            AttachHandEffect(ctx, inst, o.Source, o.Payload, o.Duration,
                                             null, false, false, HandSelfCriteria(inst),
                                             onlyInstanceId: inst != null ? inst.Id : 0);
                        if (selfOps.Count > 0)
                            ctx.Log($"—— 手牌监听：「{c.Name}」正文**没写主语**的那 {selfOps.Count} 条"
                                  + "落**在它自己**身上（打出时兑现 —— 原版那张牌自己是 acting card；"
                                  + "⛔ 不再洒给己方全体）——");
                        // 🔴 2026-10-21（`T03·c` 批 B 接线）：**手牌监听那条 ability 也只入队**。
                        //   ⚠️ 上面 `AttachHandEffect` 那半（把加成挂到**手里那一份**上）**留在原地**：
                        //   它不是「一条 ability 的结算」，而是原版 `SetupCardInHand` 那一跳的等价物
                        //   （牌进手牌时就挂，见那个方法自己的判据），与队列无关。
                        // 🔴🆕 **2026-10-25（执行代理 `X3`）**：入队这一跳补 ability 级闸
                        //   （宿主卡 = 这张手牌、座位 = 持有者；`actingCard` 拿不到 ⇒ 那道不启用——
                        //    与常驻 / 手牌陷阱两跳同一条契约），并按它的返回值发 `EvtKind.Trigger`
                        //   （判据 `RawCardScript__TriggerAbilities.c:108/:207`，同 `E8-2`）。
                        bool hlqueued = false;
                        if (restOps.Count > 0)
                        {
                            var _restOps = restOps;
                            var _lcard = c;
                            hlqueued = EnqueueAbility(ctx, ListenerLane(_lcard, TriggerNameOfWhenKind(kind)), "手牌监听:" + kind,
                                           () => HandListenerDeferred(ctx, p, _restOps, subject,
                                                                      target, card),
                                           abilityTrigger: AbilityLevelGateTrigger(kind, false),
                                           abilityTarget: AbilityLevelGateTarget(kind, subject, target),
                                           abilityActingGate: false,   // 本路径拿不到 `actingCard`（同上）
                                           abilityHostCard: _lcard, abilityOwnerSeat: p);
                        }
                        // 🧨 灭自证：把 `&& !hlqueued` 那条支改成「无条件发」⇒「被闸拒 ⇒ 0 条事件」断言红。
                        //   ⚠️ 只有 `selfOps` 那一支**没有入队口**（`AttachHandEffect` 不是 ability 结算）
                        //      ⇒ 它照旧算「结算过、要发」。
                        if (selfOps.Count > 0 || hlqueued)
                            EmitListenerTrigger(ctx, c, p, null, kind);
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="BroadcastHandTrapWhen"/> 的**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）。
        /// ⚠️ 原同步段**没有** `EventSlotsScope`（这一跳不种事件宾语，只有 `seed = null` 的
        /// `ResolveOps` —— 见那边的注释），所以这里也不开，**逐句等价**。
        /// </summary>
        static void HandTrapListenerDeferred(BattleContext ctx, int p, List<EffectOp> ops)
        {
            ctx.EffectChain++;
            try { ResolveOps(ctx, p, null, ops, "手牌陷阱"); }
            finally { ctx.EffectChain--; }
        }

        /// <summary>
        /// <see cref="BroadcastHandWhen"/> 的**队列体**（2026-10-21 `T03·c` 批 B 接线抽出）。
        /// 🔴 必须**重种事件槽**：广播那边 `EventSlotsScope.TargetAndCard` 的 `using` 在入队后
        /// 立刻退出，队列体在另一个时刻跑（理由同 `FireBoardListenerDeferred`）。
        /// </summary>
        static void HandListenerDeferred(BattleContext ctx, int p, List<EffectOp> ops,
                                         UnitState subject, UnitState target, CardDef card)
        {
            using (EventSlotsScope.TargetAndCard(ctx, target, card))
            {
                ctx.EffectChain++;
                try { ResolveOps(ctx, p, null, ops, "手牌监听", subject); }
                finally { ctx.EffectChain--; }
            }
        }

        /// <summary>
        /// 🆕 2026-10-18（`A887` 的续）**这一条 op 是不是「正文没写主语、该落在监听者自己身上」的**
        /// —— 判据与理由写在 <see cref="BroadcastHandWhen"/> 那段注释里。
        ///
        /// 三条闸，缺一不可：
        ///   · 动词只能是 `give` / `gain` / **`lose`**（**手牌加成的载荷词表只有 `GivePayload`
        ///     一份判据**，而它认的是「给的量」；`lose` 走同一条路 —— 见下）；
        ///   · 有**载荷原文**（空载荷的 `give` 本来就等于什么都没给）；
        ///   · 目标规格带着 `Subjectless`（= 卡面**没写主语**的旗标，
        ///     `EffectTargetSpec.Subjectless` 的注释里有这条判据的来龙去脉）。
        /// ⛔ **别放宽**：`give them X` / `deal 3 damage to an enemy troop` 那些**有宾语**
        ///    的必须照旧走 `ResolveOps`（它们要打的是场上的谁）。
        ///
        /// 🔴 **2026-10-18（`W5` · 审查 §5 账 4）· 就地改口（铁律 5）**：这一格原来写着
        ///   「**手牌加成表达不了减法** ⇒ `lose` 那种一律照旧走 `ResolveOps`，别在这儿硬塞」——
        ///   **收下了，理由如下（逐条核过）**：
        ///   · **原版收**：手牌效果挂的是 `CardScript.AddEffect(card, handEffect.cardEffect)`
        ///     （`PlayerHand__SetupCardInHand.c:71`），那个 `cardEffect` 可以是**任何**效果 ——
        ///     `PlayerHand__AddHandEffect.c` / `SetupCardInHand.c` 里**没有任何按效果类型过滤**的闸
        ///     ⇒ 「手牌监听器 + `lose`」在原版**照样会挂到那张牌自己身上**。
        ///   · **我们这侧也真的跑得通**（不是「硬塞」）：
        ///     `lose` 的调度是 `DoGive(..., sign: -1)`（`EffectResolver` 的 `lose` 那一行），
        ///     而载荷词表 `GivePayload.ReAttr` 收 `[+-]?\d+ &lt;属性&gt;` ⇒ `lose 1 attack` 的
        ///     `payload = "1 attack"` **解得出来**；打出时 `ApplyHandBuffs` 把 `source` 传成
        ///     刚上场的那一个、`Target` 是挂载时写死的 `Subjectless` ⇒ **减的正是它自己**。
        ///   · **前置**：`EffectText.TryLose` 在**没写主语**时给出的 `Target` 原来是 `null`
        ///     （走不到这里），同批已改成与 `TryGive` 同一条 `Subjectless` 规格 ——
        ///     两条缺一条这一支都不会触发。
        ///   ⚠️ **实测无卡走这一支**（全池 6 张含 `lose` 的卡主语都写全了）⇒ 机制对齐、非缺陷修复。
        /// </summary>
        static bool HandListenerSelfOp(EffectOp op)
        {
            if (op == null) return false;
            if (op.Verb != "give" && op.Verb != "gain" && op.Verb != "lose") return false;
            if (string.IsNullOrEmpty(op.Payload)) return false;
            var t = op.Target;
            return t != null && t.Subjectless;
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
            int owner = OwnerOf(ctx, u);

            // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P2-7`）：`GetsMarkerlight` 与 `GetsHuntMark` 同形，
            //   同样「先预调主体、再广播且排除它」。**
            //   判据 = `BattleManagerSupport__BroadcastUnitObtainedMarkerlight.c`（与
            //   `…ObtainedHuntMark.c` / `…ObtainedMark.c` **逐行同形**，只有接收函数名不同）：
            //     · `:20 CardScript.ReactToUnitMarkerlight(param_2, param_2, 0)` —— 接收者 = 被标记者；
            //     · 棋盘循环 `:29` 与最后一跳手牌 `:64 op_Inequality(uVar3, param_2, 0)`（**显式**）都跳过它；
            //     · 接收端 `CardScript__ReactToUnitMarkerlight.c:7` 发 `OnTrigger(0x203 = 515)`
            //       = `AbilityTrigger.UnitReceivesMarkerlight`。
            //   ⚠️ 两步必须成对（只预调 = 双响；只排除 = 主体漏掉）。
            // ⛔ **`GetsShield` 不动（映射不成立，不是「漏改」）**：它**没有**对应的原版广播器。
            //   原版 `BroadcastUnitObtainedMark` 的 `ObtainedMark` = **Mark of Chaos**
            //   （`CardScript__AddEffect.c:782-792` 的 `case 8` / `:926` 的 `BuffType.markOfChaos(8)`
            //    → `Enchantments.GetMarkOfChaos` → `BroadcastMarkResolved`），接收端
            //   `CardScript__ReactToUnitMarked.c:7` 发 `OnTrigger(0x1fe = 510)`
            //   = `AbilityTrigger.UnitReceivesMarkOfChaos(510)`；而 `MarkOfChaosType` 只有
            //   Khorne/Nurgle/Slaanesh/Tzeentch/Random —— **不是** `[Shield]`（`DefinedTrait.shield = 80`）。
            //   原版 `AbilityTrigger` 表里**没有任何「获得护盾」的触发**（只有 510 / 515 / 520 这三个
            //   「获得某标记」）⇒ `GetsShield` 是我们这一侧的**推断词**，没有原版形状可照；
            //   要不要给它预调、预调谁，得先有判据（已记进报告，⛔ 别照 HuntMark 硬套）。
            bool preCallSelf = kind == WhenEventKind.GetsHuntMark
                            || kind == WhenEventKind.GetsMarkerlight;
            if (preCallSelf)
                PreCallBoardListener(ctx, u, kind, owner, u.Card, u, null, null, -1);

            BroadcastWhen(ctx, kind, owner, u.Card, u, preCalled: preCallSelf ? u : null);
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
        /// <summary>
        /// 这种卡面在实测数据里有两条（`Graceful Avoidance` 的 Backlash、
        /// `Duty's End` 的「触发所有友方的典籍能力」），玩法是
        /// **把一段能力文字当礼物送给一个单位**。
        ///
        /// 做三件事：
        ///   ① 把 `关键词: 效果文字` 拆开，效果文字过 `EffectSpec.Parse`（和我们自设计的 26 张同一套文法）；
        ///   ② 挂到单位身上（`UnitState.GrantOps`）—— 卡上原生那条优先，这条是后备；
        ///   ③ 补上那个触发关键词，这样 `RuleCore.FireTriggerAt` 到时机就会找它。
        ///
        /// ⚠️ 效果文字解析不了就**整条不挂**并如实报 —— 挂一个空的触发上去
        ///    会让卡看起来生效而实际什么都没发生（本工程的红线）。
        /// </summary>
        static void GrantEmbeddedAbility(BattleContext ctx, UnitState u, string embedded,
                                         string by, string src, List<string> unresolved,
                                         string duration = null)
        {
            GrantEmbeddedCore(ctx, u, embedded, by, src, unresolved, false, duration);
        }

        /// <summary>
        /// 上面那条的**光环版**（2026-09-14 A7）—— 判据**转调**同一条核心，
        /// 唯一差别是**记在哪本账上**：光环版的这两个关键词与触发效果要进 `UnitState` 的光环账，
        /// 好让 `Auras.Recompose` 能**精确收回**（只收光环那一份，不碰卡面印的 / 别人给的）。
        ///
        /// 🔴 **为什么必须共用一份判据**：这一段是「拆 `关键词: 正文` / 词表归一 / 解析正文 /
        ///    解析不了就如实报」—— 抄第二份的话，哪天那边改了（比如加一种嵌入写法），
        ///    光环这一份就**悄悄落后，而且不报错**（本工程点名的静默失效形态）。
        ///
        /// ⚠️ **不传 `unresolved`**：光环的失败在上层已经有「载荷解释不了就整条不收」那道闸
        ///    （`Auras.TryParse`），走到这里解析不了属于**卡面数据与词表不一致** ——
        ///    照旧 `ctx.Log` 如实报（核心函数对 `null` 是安全的）。
        /// </summary>
        internal static void GrantEmbeddedAbilityFromAura(BattleContext ctx, UnitState u, string embedded,
                                                          UnitState src)
        {
            GrantEmbeddedCore(ctx, u, embedded, src.Name, src.Name + "：" + embedded, null, true);
        }

        /// <summary>
        /// 嵌入效果的**唯一实现**（两个入口共用，见上面两条的说明）。
        /// <paramref name="fromAura"/> = 记进光环那本账（可被 `Auras.Recompose` 精确收回）。
        /// </summary>
        static void GrantEmbeddedCore(BattleContext ctx, UnitState u, string embedded,
                                      string by, string src, List<string> unresolved, bool fromAura,
                                      string duration = null)
        {
            string kw = GivePayload.EmbeddedKeyword(embedded);
            string body = GivePayload.EmbeddedBody(embedded);
            if (string.IsNullOrEmpty(kw) || string.IsNullOrEmpty(body))
            {
                if (unresolved != null) unresolved.Add(src + "（嵌入效果格式不对）");
                return;
            }
            int len;
            string norm = KeywordTable.Normalize(kw, out len);
            if (norm == null || len != kw.Length)
            {
                ctx.Log($"{by}：「{src}」里的关键词「{kw}」词表里没有 —— **这条没生效**");
                if (unresolved != null) unresolved.Add(src + "（嵌入的关键词 " + kw + " 不认识）");
                return;
            }

            // 🔴 **2026-09-14 改**：正文交给**真解析器**（`EffectText.Parse`）—— 和卡上原生
            //    `Rally:` / `Slay:` 那些正文**同一台**。原来走的是 `EffectSpec`，那是给我们
            //    **自己设计的 26 张卡**写的封闭文法（只认 `Damage/Heal/Draw`），
            //    而挂上来的全是**原版卡面原文**（`Return to your hand` / `Deploy a Black Legionary` /
            //    `Lower the cost of a random troop in hand by 1` …）⇒ 全池 **10 张**卡
            //    「解析得出、有机制、却永远不会发生」。见 `UnitState._grantedOps` 的注释。
            var ops = EffectText.Parse(body, out _, out _);
            if (ops == null || ops.Count == 0)
            {
                ctx.Log($"{by}：「{src}」的效果文字「{body}」本版解析不了 —— **这条没生效**");
                if (unresolved != null) unresolved.Add(src + "（嵌入效果 " + body + " 解析不了）");
                return;
            }

            // 记进哪本账 —— **只差这一处**：光环那份要能被 `Auras.Recompose` 精确收回
            // ⚠️ 光环那份**连关键词也要记在光环账上**（`AddAuraKeyword`）—— 那是收回时的依据，
            //    原来就这么写的，改 op 形态时**别把这一行弄丢**。
            if (fromAura) { u.GrantAuraOps(norm, ops, body); u.AddAuraKeyword(norm, 1); }
            else { u.GrantOps(norm, ops, body); u.AddKeyword(norm, 1); }

            // 🆕 2026-09-16：**限时 → 记一条，到期撤**（和普通 `give` 那条路同一个形状，
            //   见 `DoGive` 末尾那段）。
            //   为什么必须补：`Give "Slay: …"` 这类**嵌入效果**要把关键词也挂上去
            //   （`AddKeyword`），而 `give` 那条路原来**只对普通属性/关键词记 `TempBuff`** ——
            //   于是卡面写「**this turn**」的那些，过了回合**还在**。
            //   实测 4 张带时长：`Helspear Assault` · `Master Outrider` · `Power of the Waaagh!` ·
            //   `Uge Choppa`（另外 9 张不带时长，`duration` 为空 ⇒ 直接跳过，行为不变）。
            //   ⚠️ 到期撤**不能只摘关键词** —— `UnitState.RemoveAll` 那侧必须**同时**清
            //      `_grantedOps` / `_grantedText`，否则 `RuleCore.FireTriggerAt` 照样找得到正文
            //      （门在 `FxOps` 里、不在触发点），表现是「关键词没了、效果照放」。
            if (!string.IsNullOrEmpty(duration))
            {
                u.AddTempBuff(new UnitState.TempBuff
                {
                    IsKeyword = true,
                    Name = norm,
                    Value = 1,
                    Owner = fromAura ? -1 : 0,
                    UntilMyNextTurn = duration == "nextturn",
                    Src = by,
                    SourceCard = ctx.PlayingCard != null ? ctx.PlayingCard.Name : by,
                });
            }
            ctx.Log($"{by}：给 {u.Name} 挂上「{norm}：{body}」"
                  + $"（到 {norm} 的时机结算）"
                  + (string.IsNullOrEmpty(duration) ? ""
                     : duration == "nextturn" ? "，到你下个回合结束" : "，本回合有效"));
        }

        /// <summary>
        /// `for each …` 到底数出几个。**语义出处：我们自己的 `rule_core.gd:1347`（旁证、非权威）的 `_fe_count`**
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

        /// <summary>
        /// 自检用：把「会造成伤害的**唯一入口**」暴露出来（那个函数是 private）。
        /// 和 `CountForTest` 同一条纪律 —— **只为自检开门，不改变任何行为**。
        ///
        /// 🔴 光环自检要用它，是因为走 `ApplyDamage` **不够**：那个只扣血，
        /// 「离场 → 收回加成」在下一段（伤害入口 → `CleanupDeaths`）里。
        /// 2026-09-14 实测踩到：只调 `ApplyDamage(..., 99)` 的单位**还在棋盘上**，
        /// 于是「来源离场 ⇒ 加成收回」那条断言测的是**根本没发生的事**。
        /// ⚠️ 可见性是 `public` 不是 `internal` —— 自检在 **Editor 程序集**里，
        ///    和 `Core` 不是一个程序集，`internal` 它看不见（`CountForTest` 同理）。
        /// </summary>
        public static int HurtForTest(BattleContext ctx, UnitState u, int amount, string source)
        {
            return Hurt(ctx, u, amount, source);
        }

        /// <summary>
        /// 🆕 2026-10-20（`W58`）自检用：把**治疗的唯一落地口**暴露出来（`HealUnit` 是 `private`）。
        /// 和 `CountForTest` / `HurtForTest` 同一条纪律 —— **只为自检开门，不改变任何行为**。
        ///
        /// <para>断言要直接验的几档（原版 `CardScript.Heal` 的四道闸，见 `HealUnit` 的 doc）：
        /// 普疗发一条 `EvtKind.Heal`（`Amount` = 实际量）· 满血 / 溢疗归零**不发** ·
        /// 非正数**不治不发** · Syphon 置位（`HealSyphon = true`）。</para>
        /// </summary>
        /// <returns>实际治疗量（`0` = 没治）。</returns>
        public static int HealForTest(BattleContext ctx, UnitState u, int amount, bool syphon = false)
        {
            return HealUnit(ctx, u, amount, syphon);
        }

        /// <summary>
        /// 🆕 **2026-10-24（引擎线 `A1` 第 1 件 · 执行代理 `E2` 改成【逐 op 同步】）：把
        /// 「本条 ability 至今解出的目标**并集**」记进 <see cref="BattleContext.PreviousAbilityTargets"/>** ——
        /// 原版 `AbilityLogic.SetPreviousAbilityTargets`（`Array.Clear` + `AddRange`，**整表覆盖**）。
        ///
        /// <para>🔴 **改前（`A1` 第一版）只在 `ResolveOps` 的两条出口同步一次**，取的是
        /// `ctx.LastTargets`（= **最后一个**解出目标的效果）⇒ 同一次结算里**后面**那条 op 读
        /// `targetsAffected = 150` 时，拿到的是**上一张卡**的残值（`E8` 顺手发现 2 的形状：
        /// 「先打一张指定目标的卡、再打一张同一次结算里带 `150` 的卡」⇒ 静默多数）。
        ///</para>
        /// <para>**原版判据（`d:/2/tools/decomp_full/` 逐行现读，2026-10-24）**：
        /// `AbilityLogic__PlayAbility.c` 的**每一个效果 `case`** 出口都是同一段 ——
        /// `uVar18 = *(ulonglong *)(param_1 + 0x80); … AbilityLogic__SetPreviousAbilityTargets(param_1,uVar18,0);`
        /// 之后 `switchD_…_caseD_1cf: return;`（41 个 `case 0x..` / 48 处引用 `LAB_18081cc87`；
        /// 全函数只有 `:2745` 一处直接调用，其余全部走那个标签）⇒ **每一条效果结束就整表覆盖一次**。
        /// 累加表 `abilityTargetsToSave(+0x80)` 在**一条 ability 内**只增不减
        /// （`AbilityLogic__AddDrawCard` 等助手往 `param_1 + 0x80` 里 `AddRange`；
        /// `AbilityLogic__DeepCopy.c` **不拷贝**这一格 ⇒ 每次 play 从空开始）。
        ///</para>
        ///
        /// <para>🔑 **所以三处缺一不可**：
        ///  · **起手清空** —— 两条 `ResolveOps` 入口各一句 `ctx.AbilityTargetsSaved.Clear()`（= 新的
        ///    `DeepCopy` 那一格）；
        ///  · **逐 op 同步** —— 两个主循环里 4 个 `ResolveOne` 调用点之后各调一次本方法；
        ///  · **出口再同步一次** —— 幂等（并集去重），兜住「最后一条 op 没走主循环」的路径。
        /// ⛔ 别退回「只在出口调一次」—— 那正是 `E8` 那条残值缺陷的形状。</para>
        ///
        /// <para>⚠️ **仍然如实标着的近似**：
        /// ① 原版表里可以有**手牌 / 牌库的卡**（`AddDrawCard` 塞进去的那批）—— 我们的槽是
        ///    `List&lt;UnitState&gt;`、只装场上单位，抽牌那一跳由 `CountEval` 的 `DrawnThisResolve`
        ///    分支单独建模（`E8`）；
        /// ② **触发路**（`ResolveOps` 重载 #2）入口会把 `prime`（= 触发的那一个 / 事件主体）种进
        ///    `ctx.LastTargets` ⇒ 它也会被并进本表。原版那条表里装的是「`GetTargets` 解出来的候选」，
        ///    触发主体通常正是其中一员 ⇒ 方向一致；今天 `ta = 150` 的消费行 0 条 ⇒ **零可观测差异**
        ///    （真要判它得等有卡用到，见 `N2_八个role.md` §五·⑤）；
        /// ③ 原版「这条 ability 一条效果都没解出目标」时静态表**保留上一张卡的值**（它只在效果出口
        ///    才被覆盖），我们起手清空 ⇒ 那种形状读到空表 —— 与**改前**（出口同步取被入口清过的
        ///    `LastTargets`）行为**一致**，不是本件引入的差异。</para>
        /// </summary>
        static void NotePreviousAbilityTargets(BattleContext ctx)
        {
            var acc = ctx.AbilityTargetsSaved;
            var lt = ctx.LastTargets;
            if (lt != null)
                for (int i = 0; i < lt.Count; i++)
                {
                    var u = lt[i];
                    if (u == null) continue;
                    // 并集去重：同一条 op 跑 N 遍（`repeats`）时目标会重复进来；
                    // `UnitState` 没有重写 `Equals` ⇒ `Contains` 就是**引用相等**（正是我们要的）。
                    if (!acc.Contains(u)) acc.Add(u);
                }
            ctx.PreviousAbilityTargets.Clear();
            ctx.PreviousAbilityTargets.AddRange(acc);
        }

        /// <summary>🆕 2026-10-24（引擎线 `A1` 第 3 件）：原版 `TargetCriteria.spellType` 的
        /// **Sabotage = 230** —— `GSC56` 的 `multiCountCriteria.spellType` 现读 230；
        /// 卡表里 `subtype = Sabotage` 的 4 张也全是 230（两把尺子同值）。
        /// ⚠️ 与 `Enchantment.cs` 的 `SpellTypeSecretOrder(60)` / `SpellTypeQuest(120)` 同族，
        /// 但用途不同（那两格是**附魔**的判据，本格是**手牌卡**的判据）⇒ ⛔ 别并到那边去。</summary>
        const int SpellTypeSabotage = 230;

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
                    if (c == null || c.Card == null) continue;
                    if (reference == "any" || reference == "card") { n++; continue; }
                    if (!DrawnMatches(c.Card, reference)) continue;   // ⚠️ 这一层按**卡模板**筛（第 7 行第 3 步：槽里是实例）
                    n++;
                }
                return n;
            }

            // ---- ①b 🆕 2026-10-24（引擎线 `A1` 第 3 件）：**手牌里的某类牌** ----
            //  `for each Sabotage in the opponent's hand`（`GSC56`）/
            //  `for each Vehicle in your hand`（`GSC75`）。
            //  判据 = 原版 `GetTargets` 的 `inHandFiltered(50)` 分支：
            //  `AbilityLogic__GetTargets.c:884-892` → `BattleManager.GetCardsInHandList(…,
            //  friendlyUnits, enemyUnits, …)` ⇒ 候选是**手牌**（侧 = 谁的），再按卡种 / `spellType` 筛。
            //  `GSC56` 的结构化数据现读：`ta 50 / enemyUnits 1 / includeSpells 1 / spellType 230`。
            //  ⚠️ 认不出的筛选词 ⇒ 返回 `-1`（调用方如实报「数不出来」），⛔ 不悄悄当 0。
            if (op.CountScope == "hand" || op.CountScope == "enemyhand")
            {
                int who = op.CountScope == "enemyhand" ? 1 - owner : owner;
                string what = reference;
                int n = 0;
                foreach (var ci in ctx.Players[who].Hand)
                {
                    if (ci == null || ci.Card == null) continue;
                    // 🔴 **2026-10-24（`G6` · `C6` §四·2）：判据收口到 `CountKindMatches`**
                    //   （与盘面分支 / `DrawnMatches` **同一份**；改前这里是第二份 if/else 链）。
                    //   `null` = 这个词本版不认 ⇒ 返回 -1（调用方如实报「数不出来」）。
                    bool? hit = CountKindMatches(ci.Card, what);
                    if (hit == null) return -1;
                    if (!hit.Value) continue;
                    n++;
                }
                return n;
            }

            // ---- ② 本回合阵亡的单位（`For each one that dies`）----
            if (op.CountScope == "died") return ctx.DiedThisTurn;

            // ---- ②b 🆕 2026-10-24（引擎线 `A1` 第 2 件）：**自上一回合以来的阵亡** ----
            //  `Repeat for each friendly troop that died since your last turn`（`UM77 Chairon`）。
            //  判据 = `CardScript__HasDiedSinceLastTurn.c:26-47`（`turnDied ∈ {turnCounter,
            //  turnCounter - 1}`）—— 与 `ConditionEval` 的 `cond 222` **同一口径**，
            //  `reference` 形如 `own|troop` / `any|unit`（`EffectText.ClassifyCount` 落的）。
            //  🔴 **⛔ 别把它并进 `died`**：那一条的窗口是「本回合」，反噬 / `for each one that dies`
            //     要的就是它（`TestDiedThisTurnDuringBacklash` 钉着）。
            //  ⚠️ `ctx.DeadUnits` 只收**真离场**（翻面成残骸那一支不进来）—— 与原版
            //     `inCemeteryFiltered`（墓地）同口径；`turnDied` 自己会连残骸一起计，
            //     但**这里的消费者数的是「墓地里有多少个」**（`UM77` 的 `multiplierSet` = `inCemeteryFiltered(60)`）。
            //  ⛔🔴 **2026-10-24（执行代理 `E6`）：这一支【不许】改用 `ctx.DiedLastTurn`。**
            //    那一格是**不分阵营、不分兵种**的全局计数（写点唯一 = `RuleCore.BeginTurnCore`），
            //    拿它顶替会把卡面的 `own|troop` / `enemy|…` 两个侧筛**静默丢掉**
            //    （数到对面死的兵）。判据：原版这一档是**逐卡**的
            //    `CardScript.turnDied` + `CardScript__HasDiedSinceLastTurn.c:26-47`，
            //    乘数集 = `inCemeteryFiltered(60)`（过一个墓地筛选），原版**没有**全局计数器。
            //    那两格今天**生产侧无读者**（只为 `EngineA1` 的自检保留），见 `RuleCore.cs`
            //    写点上方那段订正；判别式 = `RuleEngineTest_EngineE6.cs` 的 `E6T1`
            //    （把那格故意写成 999，这里的读数必须不变）。
            if (op.CountScope == "diedsincelastturn")
            {
                bool ownSide = reference.StartsWith("own");
                // 🆕 2026-10-24（`B7` · R1 `P2-10`）：第三档 `enemy|…` —— 原版这一位是
                // `multiCountCriteria.enemyUnits`，改前分类层把它折成 `own`（见 `ClassifyCount` 那段）。
                bool enemySide = reference.StartsWith("enemy");
                bool troopOnly = reference.EndsWith("troop");
                int nd = 0;
                for (int i = 0; i < ctx.DeadUnits.Count; i++)
                {
                    var d = ctx.DeadUnits[i];
                    if (d == null || d.Card == null) continue;
                    if (d.DeathTurn != ctx.Turn && d.DeathTurn != ctx.Turn - 1) continue;
                    if (ownSide && d.Owner != owner) continue;
                    if (enemySide && d.Owner == owner) continue;
                    if (troopOnly && d.Card.Type == "hero") continue;   // 督军阵亡 = 输局，正常不进表
                    nd++;
                }
                return nd;
            }

            // ---- ③ 本局打出过的某类牌（`For each Secret you played this game`）----
            // 🔴 **2026-10-17（B24）：`secret` 这一支已经有真计数器了** —— 判据是反编译里那条
            //    **独立计数器** `PlayerManager.secretsPlayed`（`TargetsAffected.secretsPlayed = 245`；
            //    写点 = `_ResolvePlayCardFromHand` 里 `spellType == 150` 时 `AddSecretPlayed`），
            //    **不是**这张「打出过的牌」流水表 —— 全部出处见 `PlayerState.SecretsPlayed` 的注释。
            //    ⚠️ 改之前这里对**所有** `played` 一律返 -1（如实报「数不出来」）⇒
            //       `Relic Munitions` 的 `Repeat for each Secret you played this game`
            //       整条**不结算**（卡面写了的事没做）。
            // ⚠️ 别的引用词（`sabotage` / `any`）**原版没有对应计数器**（`TargetsAffected` 枚举里
            //    只有 `secretsPlayed` 这一个）⇒ 照旧返 -1 让调用方如实报，
            //    **不许当成 0 悄悄空过、也不许拿上一条流水表顶替**。
            if (op.CountScope == "played")
            {
                if (reference == "secret") return ctx.Players[owner].SecretsPlayed;
                return -1;
            }

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
                // 🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 B / §四·2）：兵种筛收口到 `CountKindMatches`。**
                //  改前这里**只判 `troop`**，`vehicle / infantry / daemon / beast / drone / unit`
                //  一个都不筛 ⇒ `AM18` 的 `for each friendly Vehicle in play` 被数成「友方全体单位」
                //  （`AM18` 自己 + 督军都会被数进去）。同一份判据原来在 `DrawnMatches` 与手牌分支
                //  **各有一份** —— 这次漏的就是盘面这一处（两处写同一条规则 = 迟早不一致）。
                bool? hit = CountKindMatches(u.Card, kind);
                if (hit == null) return -1;       // 兵种词不认识 ⇒ 如实报「数不出来」（⛔ 不当「都算」）
                if (!hit.Value) continue;
                // `for each damaged enemy` —— 受损 = 当前血 < 上限
                if (damagedOnly && u.Health >= u.MaxHealth) continue;
                count++;
            }
            return count;
        }

        /// <summary>
        /// **「兵种词 → 这张卡算不算」的唯一判据** —— `CountFor` 的盘面 / 手牌分支与
        /// <see cref="DrawnMatches"/> 三条计数路**共用这一份**（2026-10-24 · 执行代理 `G6` · `C6` §四·2）。
        ///
        /// <para>**为什么必须收成一处**：改前盘面分支只筛 `troop`（载具/步兵/…全不筛 ⇒ `AM18` 少数成
        /// 「友方全体」），而同一份判据在 `DrawnMatches` 与手牌分支各写了一份 —— 三处里漏一处就是**静默**多数。</para>
        ///
        /// <para>`kind` 的取值域 = `EffectText.ClassifyCount` 落的**封闭词表**
        /// （`any` / `card` / `troop` / `unit` / `vehicle` / `infantry` / `daemon` / `beast` / `drone`；
        /// 手牌那一路另有 `spell` / `sabotage`）。返回 `null` = **这个词本版不认**
        /// （调用方如实报「数不出来」，⛔ 不许当「都算」—— 那是静默多数）。</para>
        ///
        /// <para>口径：兵种词一律转 `CreatePool` 的 `KindWords` **那一张表**（⛔ 不在这里另写一张）；
        /// 只有三条**这里才有的**语义补丁：① 卡模板这一层分不开 `troop` / `unit`（差别只在督军 ⇒
        /// 盘面分支另有一句 `kind == "troop" &amp;&amp; u.IsWarlord ⇒ 不算`）；② `unit` **含督军**
        /// （`CardDef.Type == "hero"`）；③ 手牌/抽牌两路上的 `spell` 指**战术卡**（`Type == "tactic"`），
        /// 不是 `KindWords` 里那个 `spell` 行（那是 `subtype`）。</para>
        /// </summary>
        static bool? CountKindMatches(CardDef c, string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            switch (kind)
            {
                case "any": case "card": return true;
                case "troop": return c != null && c.Type == "unit";                       // 部队（不含督军）
                case "unit": return c != null && (c.Type == "unit" || c.Type == "hero");   // 单位（含督军）
                case "spell": return c != null && c.Type == "tactic";                      // 手牌那一路的「法术」
                case "sabotage": return c != null && c.SpellType == SpellTypeSabotage;
            }
            return CreatePool.IsKnownKind(kind) ? (bool?)CreatePool.MatchesKind(c, kind) : null;
        }

        // ==================================================================
        //  🔴 `AddEffect` 的四道【前置闸】（完全复刻 · 第一批；第五道 extrinsic 去重**不在本批**）
        // ==================================================================
        //
        //  **判据（第一权威，现读）= `d:/2/tools/decomp_full/CardScript__AddEffect.c`**：
        //    · 闸① 卡状态      `.c:67-78`   `cardState - 5U < 2 || == 0xb` ⇒ LogWarning + `return 0`
        //    · 闸② jam         `.c:85-98`   施放者带 `jam(0x82)` ∧ `extrinsic` ⇒ `return 0`（**静默**）
        //    · 闸③ 残骸门      `.c:159-170` `isRemnant(+0x65)` ⇒ 只放行
        //                                    `addStats(1)` 且 `healthChange(+0x60) != 0`、或 `addTraitRemnant(90)`
        //    · 闸④ 施放者不在场 `.c:171-195` 施放者是 `Minion(cardType==0)` ∧
        //                                    `cardState ∉ {2 inPlay, 3 inPlayAttacking, 15 inPlayTransforming,
        //                                    17 inPlayAminingAttack}` ⇒ LogWarning + `return 0`
        //  **旁证（i2c，字段名更好读）**：`out_cpp2il_dev_ilspy/CardScript.cs:17407+`
        //    （⚠️ 它把 `+0x228` 那几次读折叠成了 `(nint)0`/`-17` ⇒ **状态值一律只认 `.c`**）。
        //
        //  ## 闸装在哪、⛔ 别往哪儿装
        //  原版四道闸都在 `CardScript.AddEffect(card, effect)` 里 = **每 (目标, 效果) 一次**。
        //  我们的等价物 = `DoGive`（`give/gain/lose` = `addStats`/`addTrait` 那一族，落点 `ApplyOneGain`）
        //  与几个「一个动词一条 buff」的 handler（`stun` / `blind` / `sethealth` / `double`）。
        //  ⛔ **不装在 `ResolveOneCore` 入口** —— 原版 `deal` / `destroy` / `heal` 那些**不走 AddEffect**，
        //  在入口统一加闸会顺手改掉伤害/摧毁/治疗的语义（那是真回归，不是复刻）。
        //
        //  ## 两处**模型映射**（如实标注：这是落法，不是原版定论）
        //  1. 闸① 按 `cardState ∈ {5 waitingToDie, 6 inCemetery, 11 removedFromGame}` 拒。我们没有
        //     cardState 状态机 ⇒ 落成 **`!IsUnitInPlayOrDying`**（既不在棋盘、也不在本条动作的死亡窗口里）。
        //     理由：**我们的待死标记落得比原版的状态迁移早** —— `Sacrifice:` 触发时原版 `cardState` 仍是
        //     `2 inPlay`（`A1426`），`W62` 也证明同批内 dying 的单位原版仍按在场算
        //     ⇒ 拿 `IsPendingDeath` 当 state 5 会**反向误杀**那两族（它们本来就该收到 buff）。
        //  2. 闸④ 的「在场」= `cardState ∈ {2,3,15,17}` ⇒ 落成 **在棋盘 ∨ `ctx.DyingThisAction`**（同一理由）。
        //
        //  ## 闸② 只有**手牌效果**那一层可判（如实标注）
        //  `extrinsic`（`CardEffect +0x30`）在本仓**只有** `CardInstance.HandEffect.Extrinsic` 一个载体
        //  （写点 `AbilityLogic.PlayAbility.c:1197/:1354` = `whileInPlay || whileInPlayVariable`，
        //  我们的解析层不产出它 —— 见 `AttachHandEffect` 的 `extrinsic` 参数 doc）。
        //  ⇒ 板上 buff 的 extrinsic 这一位**判不了**，一律按 `false` 放行；⛔ 别拿「此刻有单位」近似
        //  （`W4` 已经因为那个近似误摘过一次）。

        /// <summary>
        /// **闸① 卡状态**（`CardScript__AddEffect.c:67-78`）—— 返回 `false` = 原版 `return 0`。
        /// 映射与理由见上面那段「两处模型映射」第 1 条（⛔ 别改成 `IsPendingDeath`）。
        /// </summary>
        static bool AddEffectCardStateAllows(BattleContext ctx, UnitState target, string by)
        {
            if (ctx == null || target == null) return false;
            if (IsUnitInPlayOrDying(ctx, target)) return true;
            ctx.Log($"{by}：`AddEffect` **闸① 卡状态** —— 「{target.Name}」已经离场"
                  + "（原版 `cardState ∈ {5 waitingToDie, 6 inCemetery, 11 removedFromGame}` 不受 buff，"
                  + "`CardScript__AddEffect.c:67-78`）⇒ **这条效果不收**");
            return false;
        }

        /// <summary>
        /// **闸③ 残骸门的核心判据 —— 只此一处**（`CardScript__AddEffect.c:159-170`）。
        ///
        /// 原版那句原文：`if (isRemnant) { if (buffType == addStats) { if (healthChange != 0) 放行 }
        /// else if (buffType == addTraitRemnant) 放行; Log(…remnant and the effect is not health buff);
        /// return 0 }`。三个入参就是那句 if 的三个条件 —— ⛔ **别在调用点各写一份**：
        ///   · <paramref name="addStats"/> = 这是 `addStats(1)` 那一族（我们的 `give/gain/lose` 的**数值**载荷）
        ///   · <paramref name="healthChange"/> = 这一项的**生命**增量（原版 `+0x60`；非生命项传 0）
        ///   · <paramref name="addTraitRemnant"/> = 这是 `addTraitRemnant(90)`
        ///     （🔴 **判据订正 2026-10-26 批次 4 · `F2`**：原来这里写「我们这边 = 授予 `remnant` 关键词」
        ///      —— **那是误读**。原版判的是**投递类型 `buffType == 0x5a`**，`0x5a` 授的 trait 是
        ///      `CardEffect.trait`（对 `SAU53` 是 `80 shield`），不是「`remnant` 这个关键词」；
        ///      它的来源 = `EffectOp.AddTraitRemnant`（解析期按「接受者按定义是残骸」置位））
        ///
        /// 🔴 **为什么必须逐【载荷项】判而不是逐 op**：`give +1 melee and +1 health` 这种一条 op 带两项的写法，
        /// 原版判的是**每一项的 `healthChange`** ⇒ 近战那半不收、生命那半照收。按 op 一刀切会**静默少给**。
        /// </summary>
        static bool AddEffectRemnantAllows(BattleContext ctx, UnitState target, bool addStats,
                                           int healthChange, bool addTraitRemnant, string by, string what)
        {
            if (ctx == null || target == null || !target.IsRemnant) return true;
            if (RemnantDoorAllowsCore(addStats, healthChange, addTraitRemnant)) return true;
            ctx.Log($"{by}：`AddEffect` **闸③ 残骸门** —— 「{target.Name}」是一具**残骸**，"
                  + "只收「生命增减」与 `addTraitRemnant`；这一项「" + (what ?? "?") + "」**不收**"
                  + "（原版 `Trying to AddEffect to … which is remnant and the effect is not health buff`，"
                  + "`CardScript__AddEffect.c:159-170`）");
            return false;
        }

        /// <summary>🆕 **2026-10-26（批次 4 · `F2`）**：闸③ 的**纯判据**（无 ctx / 无日志 / 无副作用）——
        /// 判据只此一份，`AddEffectRemnantAllows` 与自检口径都调它。
        /// 原文 = `if ((buffType == addStats &amp;&amp; healthChange != 0) || buffType == addTraitRemnant) 放行`。</summary>
        static bool RemnantDoorAllowsCore(bool addStats, int healthChange, bool addTraitRemnant)
            => (addStats && healthChange != 0) || addTraitRemnant;


        /// <summary>把**一项载荷**（`PayloadOp` + 已带符号的值）翻译成闸③的三个条件。</summary>
        /// <param name="viaAddTraitRemnant">
        /// 🆕 **2026-10-26（完全复刻批次 4 · `F2`）**：这一条增益的**投递类型**是不是原版
        /// `buffType 90 addTraitRemnant`（来自 `EffectOp.AddTraitRemnant`，由解析期按
        /// 「接受者按定义是残骸」置位 —— 唯一一处 = `When … becomes a Remnant it gains …`）。
        ///
        /// 🔴 **判据订正（铁律 5）**：改前这里是
        /// `bool addTraitRemnant = p.IsKeyword &amp;&amp; p.Keyword == KeywordTable.Remnant;`
        /// —— **那是误读**。原版 `CardScript__AddEffect.c:159-170` 的 `else if (buffType != 0x5a) reject`
        /// 判的是**投递类型**；`0x5a` 授的 trait 是 `CardEffect.trait(+0x3c)`（对 `SAU53` 是 `80 shield`），
        /// **不是**「`remnant` 这个关键词」。旧写法在池内是**死代码**（没有任何卡授予 `remnant`），
        /// 而它同时让 `SAU53` 那条永远过不了闸 ⇒ 改它零回归。
        /// </param>
        static bool AddEffectRemnantAllowsPayload(BattleContext ctx, UnitState u, PayloadOp p,
                                                  int val, string by, bool viaAddTraitRemnant = false)
        {
            if (u == null || !u.IsRemnant) return true;
            bool addStats = !p.IsKeyword && !string.IsNullOrEmpty(p.Attr);
            int healthChange = 0;
            if (addStats && NormalizeAttr(p.Attr) == "health") healthChange = val;
            // ⚠️ **`p.IsKeyword` 再挡一道**：原版 `buffType 90` 只授 trait、不授属性 ——
            //   属性项即使被标了投递类型，也照旧走 `addStats` 那条规则（只有生命增减放行）。
            bool isAddTraitRemnant = p.IsKeyword && viaAddTraitRemnant;
            return AddEffectRemnantAllows(ctx, u, addStats, healthChange, isAddTraitRemnant, by, p.Source);
        }

        /// <summary>
        /// 🆕 **2026-10-26（批次 4 · `F2`）自检口径**：闸③ 残骸门的**纯判据**入口
        /// （不碰 ctx / 不改状态 / 不写日志）—— 给「非 `addTraitRemnant` 的关键词授予在残骸上仍被拒」
        /// 那条**反例断言**用（断言落在 `Editor/RuleEngineTest.cs` 的 SAU53 段之后）。
        ///
        /// <para>⛔ **这是给断言读的一个纯函数，不是绕过闸③ 的新入口**：产品路径只有
        /// <see cref="AddEffectRemnantAllowsPayload"/> 一处，两者调的是**同一个**
        /// <see cref="AddEffectRemnantAllows"/>（判据仍只此一份）。</para>
        /// </summary>
        /// <param name="isKeyword">这一项是不是关键词授予（`PayloadOp.IsKeyword`）</param>
        /// <param name="attr">属性名（关键词授予时传 `null`）</param>
        /// <param name="val">已带符号的值</param>
        /// <param name="viaAddTraitRemnant">投递类型是不是 `buffType 90`</param>
        public static bool RemnantDoorAllowsForTest(bool isKeyword, string attr, int val,
                                                    bool viaAddTraitRemnant)
        {
            bool addStats = !isKeyword && !string.IsNullOrEmpty(attr);
            int healthChange = (addStats && NormalizeAttr(attr) == "health") ? val : 0;
            bool isAddTraitRemnant = isKeyword && viaAddTraitRemnant;
            return RemnantDoorAllowsCore(addStats, healthChange, isAddTraitRemnant);
        }


        /// <summary>非载荷型 buff（`stun` / `blind` / `sethealth` / `double` / 手牌效果）——
        /// 原版它们是 `buffType` ≠ 1 且 ≠ 90 ⇒ 对残骸**一律不收**。</summary>
        static bool AddEffectRemnantAllowsOp(BattleContext ctx, UnitState t, EffectOp op, string by)
            => AddEffectRemnantAllows(ctx, t, false, 0, false, by, op != null ? op.Source : null);

        /// <summary>
        /// **闸② jam**（`CardScript__AddEffect.c:85-98`）：施放者带 `jam(DefinedTrait.jam = 130)` ∧
        /// 这条效果是 **extrinsic** ⇒ 不生效（原版这一支**不写日志**，我们出声 —— 本仓红线「不许静默」）。
        /// `extrinsic` 的载体与「为什么只有手牌那一层可判」→ 本段头注。
        /// </summary>
        static bool AddEffectCasterNotJammed(BattleContext ctx, UnitState caster, bool extrinsic, string by)
        {
            if (ctx == null || caster == null || !extrinsic) return true;
            if (!caster.Has(KeywordTable.Jam)) return true;
            ctx.Log($"{by}：`AddEffect` **闸② jam** —— 施放者「{caster.Name}」身上带 **jam**"
                  + "（`DefinedTrait.jam = 130`），这条 **extrinsic** 效果不收"
                  + "（原版 `CardScript__AddEffect.c:85-98`）");
            return false;
        }

        /// <summary>那个 <see cref="UnitState"/> 的那份牌**在不在手牌里**（给闸④的那条例外用）。
        /// 按**引用**比，不按卡名（同名两张是两份）。</summary>
        static bool AddEffectCasterInHand(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null || u.Instance == null) return false;
            for (int p = 0; p < 2; p++)
                foreach (var inst in ctx.Players[p].Hand)
                    if (ReferenceEquals(inst, u.Instance)) return true;
            return false;
        }

        /// <summary>
        /// **闸④ 施放者不在场**（`CardScript__AddEffect.c:171-195`）。
        /// 原版只对 **Minion**（`cardType == 0`）判；**督军 / 战术卡 / 附魔**那一档整段跳过。
        /// 例外（原版唯一一条）= `enchantingCard == 目标` ∧ 目标 `cardState ∈ {inHand(1), inHandShowing(8),
        /// inHandMoving(9), inHandPlaying(10), waitingToBePlayed(4), inHandJustCreated(12)}`
        /// ⇒ 落成「施放者就是目标 ∧ 那份牌在手上」（= 手牌里自己给自己挂效果）。
        /// </summary>
        static bool AddEffectCasterInPlayGate(BattleContext ctx, UnitState caster, UnitState target, string by)
        {
            if (ctx == null || caster == null) return true;              // `enchantingCard == null` ⇒ 原版整段跳过
            var card = caster.Card;
            if (card == null || !card.IsUnit || caster.IsWarlord) return true;   // 只判 Minion
            if (IsUnitInPlay(ctx, caster) || ctx.DyingThisAction.Contains(caster)) return true;
            if (ReferenceEquals(caster, target) && AddEffectCasterInHand(ctx, caster)) return true;
            ctx.Log($"{by}：`AddEffect` **闸④ 施放者不在场** —— 施放者「{caster.Name}」是一张**部队**"
                  + "（`cardType == Minion`）且已经不在场，这条效果不收"
                  + "（原版 `Trying to AddEffect to … from card no longer in play`，"
                  + "`CardScript__AddEffect.c:171-195`）");
            return false;
        }

        /// <summary>闸② + 闸④ —— 两道都**只看施放者**（与目标无关），每条 op 判一次就够。</summary>
        static bool AddEffectCasterGates(BattleContext ctx, UnitState caster, UnitState target,
                                         bool extrinsic, string by)
            => AddEffectCasterNotJammed(ctx, caster, extrinsic, by)
               && AddEffectCasterInPlayGate(ctx, caster, target, by);

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
            // 🆕 「条件换数值」形态③：替换值**在载荷里**（`Give +1 [Attack] …, or +3 [Attack] if they are
            //    Destroyer`，`Disruption Blades`）。**逐目标判** —— 卡面「若**其**为毁灭者」说的是
            //    被加的那批单位自己（混合编队时各自拿各自的那份）。
            var payloadAlt = string.IsNullOrEmpty(op.AltPayload) ? null : GivePayload.Parse(op.AltPayload);

            // 🔴 **`AddEffect` 闸②（jam）/ 闸④（施放者不在场）** —— 判据 `.c:85-98 / 171-195`，
            //   落点与映射见本文件「AddEffect 的四道前置闸」那一节。两道都**只看施放者** ⇒ 每条 op 判一次。
            //   ⚠️ 施放者 = `ResolveOne` 在结算前设好的 `ctx.ActingUnit`（战术卡那条路是 `null` ⇒ 闸④ 跳过，
            //      与原版 `enchantingCard == null` 整段跳过同形）。
            //   ⚠️ 闸拒绝 = **合法的「什么都不做」**（同「条件不成立」那一支）⇒ 返回 `true`、不进 `unresolved`；
            //      日志已经出声，⛔ 别让它悄悄过去。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, chosen, false, by)) return true;

            // 无目标 = 我们自己的 `rule_core.gd:3137`（旁证、非权威）那一版落到己方全体（它自己标为近似）
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：己方全体)", Side = "own", Kind = "unit", Count = 0, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, null, op);
            // 🆕 2026-09-16：**`… in play and in hand` 的「手牌」那半**（`Avenging Zeal` 那一族）——
            //   ⚠️ 放在「场上没有目标就空过」那个 `return` **之前**：手里有部队、场上空着时，
            //     这半句**照样该登记**（卡面写的就是「场上和手牌」）。
            GrantHandBuffForTargets(ctx, owner, by, op, spec, unresolved);

            // 🔴 **2026-10-17（B7：卡实例身份 —— `SetupCardInHand` 那一套）**：
            //    `give them X` 里的 `them` **也可能指手里的那几张** —— 原版把效果
            //    **挂到那一张牌自己身上**（`PlayerHand.SetupCardInHand` / `AddHandEffect`，
            //    判据逐条写在 `HandReferents` 上面那段注释里）。
            //    取目标的规则 = **场上那一批能拿到就以场上为准**，场上空着才轮到手里的
            //    （理由与全卡池实测见 `HandReferents`）。
            //    ⚠️ 这里**不 return** —— 下面 `targets.Count == 0` 那条「空过」的日志要认得这件事，
            //       否则刚挂完就报一句「没有合法目标」，自相矛盾。
            var handTargets = (targets.Count == 0 && spec.Side == "prev" && spec.Kind == "prev")
                              ? HandReferents(ctx, owner)
                              : null;
            if (handTargets != null && handTargets.Count > 0)
                AttachEffectToHandInstances(ctx, owner, by, op, handTargets, unresolved);

            // 🆕 **2026-09-16：载荷里「给玩家自己」的阵营资源那一段**（见 `PayloadOp.Resource`）——
            //   出处：`Inner Circle Companion`（`DA21`）的 `Gain Vanguard and **2 Quest Points**`：
            //   一条 `gain` 里**混了两种接受者**（给单位 Vanguard + 给玩家 2 任务点）。
            // 🔴 **在目标循环之前单独结算一次**：
            //   ⚠️ **别放进 `ApplyOneGain`** —— 那是**逐目标**调的 ⇒ 每个目标各加一次，
            //      而且绕掉 `QuestPointThreshold` 与 `When you gain [Quest Point]` 广播。
            //   ⚠️ 放在 `targets.Count == 0` 那个 `return` **之前** —— 资源是给玩家的，
            //      场上一个合法目标都没有时**照样该给**。
            //   ⚠️ 读 `payload`（不是 `payloadNow`）—— 后者是**逐目标**在循环里按 `AltHolds` 挑的，
            //      而资源**不随目标变**（`Inner Circle Companion` 也没有 alt 载荷）。
            // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P3-1`）**：记一笔「**给玩家的载荷真的生效了**」。
            //   下面 `targets.Count == 0` 那条分支原来一律打「没有合法目标，空过」——
            //   可纯能量 / 纯资源的载荷**已经结算过了**（能量在 7904 那段、资源在这段），
            //   日志会自相矛盾（`P2-5` 同一族的「话说反了」）。措辞按这一位分叉。
            bool playerPayloadApplied = false;
            foreach (var p in payload)
            {
                if (string.IsNullOrEmpty(p.Resource)) continue;
                playerPayloadApplied |= DoFactionResource(ctx, owner, by, new EffectOp
                {
                    Verb = "gain" + p.Resource, Source = op.Source, Amount = p.Value,
                }, p.Resource, unresolved);
            }

            // 🔴 **2026-10-20（`W58-TURN-A`）：载荷里的【能量】也必须在目标循环之前单独结算一次。**
            //   原因：能量是**玩家资源**，不是「给某个单位 +1」——
            //   原版 `BattleManager__ProcessManaBuff.c:38-62` 按 `manaBuff.manaAmount` 循环、只对
            //   `PlayerManager` 调一次 `AddNewMana`，**与目标个数无关**。
            //   改之前 DA61 `Plasma Generator`（`Gain 1 Energy`）被当成「给己方全体 +1 能量」——
            //   `ruleprobe play DA61` 实测**4 个目标各加一次当前能量**（净 +4），
            //   而原版是 `totalMana +1`、当前能量不动。
            //   ⚠️ 语义/判据与 <see cref="DoEnergy"/> 同一份，⛔ 别在这里另写一套：
            //   本支只负责「**只加一次** + 不逐目标」，字段怎么动由下面这几行照 `DoEnergy` 做。
            foreach (var p in payload)
            {
                if (p.Unresolved || p.Attr != "energy") continue;
                // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P3-2` 的同族）**：`0` 量能量
                //   不许写成「结算成功」—— 与 `DoEnergy` 那条同一份口径（出声 + `unresolved`）。
                if (p.Value * sign == 0)
                {
                    ctx.Log($"{by}：「{op.Source}」要给能量，但数量是 0 —— **这条没生效**");
                    unresolved?.Add(op.Source + "（能量数量为 0）");
                    continue;
                }
                var eps = ctx.Players[owner];
                eps.MaxEnergy += p.Value * sign;
                ctx.Vars.AddManaBonus(owner, p.Value * sign);
                ctx.Log($"{by}：「{op.Source}」最大能量 {(sign > 0 ? "+" : "")}{p.Value * sign}"
                      + $"（现 {eps.Energy}/{eps.MaxEnergy}；`Gain N Energy` = `ManaBuff.createMana`："
                      + "**永久抬上限、当前能量不动**，且**只结算一次**、不随目标个数翻倍）");
                if (p.Value * sign != 0) playerPayloadApplied = true;
                break;      // 载荷里多条能量段也只算一次（原版按 manaAmount 循环，不按目标数）
            }

            if (targets.Count == 0)
            {
                // 🔴 **2026-10-17（B7）**：目标已经**挂到手牌那几份上了**（`AttachEffectToHandInstances`）
                //    ⇒ 这不是「空过」，别再报一句自相矛盾的日志。原版同一件事走的是
                //    `PlayerHand.SetupCardInHand`（效果落在**那张牌**上），本来就没有「场上目标」。
                if (handTargets != null && handTargets.Count > 0) return true;
                // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P3-1`）**：已按 `P3-1` 分叉 ——
                //   **纯玩家载荷（能量 / 阵营资源）已经生效**时不能再打「没有合法目标，空过」，
                //   那句话会让一条**真的结算了**的效果看起来像空过。
                // ⚠️ **2026-10-20（`W58-REFIX`）补充（铁律 5）**：这一支在**真实对局里基本不可达** ——
                //   `RuleCore.NewBattle` 一定把督军摆在 `Board[BoardSpec.WarlordSlot]`（`RuleCore.cs:549`），
                //   而「纯能量」载荷没写主语 ⇒ `target = 己方全体 unit` 至少命中督军 ⇒ `targets.Count >= 1`，
                //   走的是 `DoGive` 尾部的「只含能量 ⇒ 已按**玩家自己**结算一次」（`:8064-8068`）。
                //   ⇒ `P3-1` 所说「两句日志会同时出现」**不成立**（`targets.Count == 0` 时这里先 `return`，
                //   到不了 8067）。本支保留，只为真出现 0 目标时不再误报「空过」。
                if (playerPayloadApplied)
                {
                    ctx.Log($"{by}：「{op.Source}」的载荷是**给玩家的**（能量 / 阵营资源）—— "
                          + "**上面那一句已经结算了**；场上没有需要挂效果的目标，这一条不算空过");
                    return true;
                }
                // ⚠️ 「只在手牌」那几张（`HandOnly`）走到这里是**正常的** —— 别报成失败
                ctx.Log(spec.HandOnly
                        ? $"{by}：「{op.Source}」的目标只在手牌（场上不加）—— 已按 `in hand` 登记"
                        : $"{by}：「{op.Source}」没有合法目标，空过");
                return true;
            }
            // 🔴 **2026-10-26（批次 8 · `B8_AE`）：「相邻」那一家【不裁】。** 下面这条裁剪的口径是
            //   「卡面只写了一个目标（`a friendly troop`），却解出多个 ⇒ 取第一个」，而
            //   `ResolveTargetsCore` 的相邻那一段**已经**按同一份判据裁过：
            //   `keep = (spec.AdjacentAll || spec.AnchorInSet) ? 0 : spec.Count`（「取几个」那一段）。
            //   ⇒ 这里再裁一次会把**相邻那一圈整个抹掉**，只剩锚点。实证：
            //   `f8260229254a09746af6d9ff7a9b0b54 Ethereal Supreme`
            //   （`Give +1 Health to your Warlord and adjacent units`）相邻那一步算出
            //   「3 个目标：FIXWarlord_TauEmpire、FIX_A、FIX_B」，裁完只剩督军 +1 血
            //   （`FIX_A` / `FIX_B` 一点没动）—— 卡面写的是「战将和相邻单位」，**静默少给两个**。
            //   ⚠️ 只对 `Adjacent` 豁免（`AnchorInSet` / `AdjacentAll` 只在相邻那一支里置位）。
            if (targets.Count > 1 && op.Target != null && op.Target.Count == 1 && !op.Target.Auto
                && !op.Target.Adjacent)
            {
                // 要多目标却只写了一个（如 `a friendly troop`）→ 取第一个，别默默全加
                targets.RemoveRange(1, targets.Count - 1);
            }

            foreach (var t in targets)
            {
                // 🔴 **2026-10-19（`A1426`）：判据从 `!IsAlive` 换成「在不在场上」。**
                //   原版**没有**一道「逐目标再判活体」的闸；唯一那道在**加效果**那一步、
                //   判的是 `cardState`（**位置**）：`CardScript__AddEffect.c:67-75`
                //   `cardState ∈ {5 waitingToDie, 6 inCemetery, 11 removedFromGame} ⇒ return 0`，
                //   正判 = `CardScript.IsInPlay()` = `cardState ∈ {2,3,15,17}`。
                //   判据全文与 11 处宿主清单 → `资料/普查产出_第二十五会话/R1_引擎判据收集.md` §`A1426`。
                //
                //   ⇒ **`Health <= 0` 但还在场上（等着翻面成残骸）的目标原版会加、我们以前不加** ——
                //    `Sacrifice:` / `Backlash:` 触发时 `cardState` 正是 `2 inPlay`
                //    （`CardScript__CheckIfDead.c:108-110` 用 `{2,3,15,17}` 判在场、
                //     触发排在状态变更之前）⇒ 那半条效果被我们静默丢掉。
                //
                //   🔴 **判别式（⛔ 别越过）：这不是「删闸」** —— `Give +N Health` 能把
                //   `Health = -3` 推成 `> 0` ⇒ **会复活不该复活的**。只把判据换成
                //   「**活着 ∨ 还在棋盘上**」：
                //     · 活着 ⇒ 留着（旧行为 —— **含已经回手 / 在牌库里的那张**：
                //       `Return a friendly troop to your hand. … give it Flank` 的效果是挂到
                //       **手牌那一份**上的，只查棋盘会把它静默丢掉）；
                //     · 血 ≤ 0 但还在棋盘上（`cardState = 2 inPlay`，等着翻面）⇒ **也要给**（本笔修的）；
                //     · 血 ≤ 0 且已经离场（`{5,6,11}`）⇒ 拿不到（不复活尸体）。
                //   ⚠️ **只有 `DoGive` 这一处已核**（其余 10 处 `!IsAlive` 的原版入口闸**未查**）
                //   ⇒ ⛔ 别顺手把同族的 `DoDeal` / `DoDestroy` / `DoStun` … 一起改。
                // 🔴 **`AddEffect` 闸① 卡状态**（原版 `CardScript__AddEffect.c:67-78`）——
                //   判据收在 `AddEffectCardStateAllows` **一处**（映射与「为什么不拿 `IsPendingDeath` 判」
                //   写在那一节的头注里）。⛔ 别在这里再写一份 `!IsUnitInPlayOrDying`。
                if (t == null || !AddEffectCardStateAllows(ctx, t, by)) continue;
                // 🔴 **2026-10-22（`W62` · R2）就地订正（铁律 5）：判据扩成原版 `IsInPlayOrDying`。**
                //   上一版是 `t.IsAlive || IsUnitInPlay(ctx, t)`（`A1426`）—— 少了**「本条顶层动作里刚被判死、
                //   已从棋盘摘下来」**那一档，而原版 `waitingToDie(5)`（`CardScript__IsInPlayOrDying.c:10-13`）
                //   **算在场**。实测症状（`TestT03dAttackerKilledBySentryStillFiresOwnAttack` ①）：
                //   被哨戒打死的攻击者，自己那条 `When this unit attacks, gain +1 Attack` 到出队时
                //   已经「不在棋盘、也不活着」⇒ **+1 加不上、还不报错**。
                //   判据收在 `IsUnitInPlayOrDying` 一处（⛔ 别在这里另写一份）。
                // 🆕 2026-09-16 「**它本回合内死了，就把这条效果转给另一个**」
                // （`BL74 Spreading Corruption`：`If it dies this turn, apply this effect to
                // another random enemy troop`）—— 解析层把「转给谁」挂在**这条 op** 上
                // （`EffectOp.DeathWatchTarget`），这里按**实际打中的那个单位**登记监听。
                // ⚠️ 记的是 `t` 这个对象（不是卡名）—— 同名两张分得开。
                // 结算在 `RuleCore.FlushDeathWatches`（死亡那一刻跑**一次**）。
                if (op.DeathWatchTarget != null)
                    ctx.DeathWatches.Add(new BattleContext.DeathWatch
                    {
                        Target = t, Owner = owner, ExpireTurn = ctx.Turn, Source = op.Source,
                        Ops = new List<EffectOp>
                        {
                            new EffectOp
                            {
                                Verb = "give", Source = op.Source, Payload = op.Payload,
                                Target = op.DeathWatchTarget,
                            },
                        },
                    });
                var payloadNow = payload;
                if (payloadAlt != null && AltHolds(ctx, owner, op, t, by))
                {
                    payloadNow = payloadAlt;
                    ctx.Log($"{by}：「{op.Source}」条件「{op.AltCondition}」成立 ⇒ 「{t.Name}」拿的是"
                          + $"**{op.AltPayload}**（不是 {op.Payload}）");
                }
                foreach (var p in payloadNow)
                {
                    // 🆕 **载荷里有认不出的段**（`PayloadOp.Unresolved`，2026-09-16）——
                    //   那半句**没生效**，必须**说出来**（原来它是**静默丢掉**的：
                    //   `ParseInto` 的 `any` 只看「有没有一段解出来」，解不出的段不留任何痕迹，
                    //   报表与卡面双双放行）。实测受影响 4 张：`UM58` 丢 `1 health` ·
                    //   `GOF_Worst_Temper` 丢 `wings flying` · `TAU74` 丢 `+1 power` ·
                    //   `DA21` 丢 2 点任务。**保留了「能用那半」，只是让丢段有声。**
                    if (p.Unresolved)
                    {
                        ctx.Log($"{by}：「{op.Source}」里这一段本版不认识 —— **这半句没生效**："
                              + $"「{p.Source}」");
                        unresolved.Add(op.Source + "（载荷里认不出的段：" + p.Source + "）");
                        continue;
                    }
                    // 🔴 **2026-10-20（`W58-TURN-A`）**：能量已在**目标循环之前**单独结算过一次
                    //   （玩家资源，不随目标个数翻倍）⇒ 这里跳过，⛔ 别再逐目标 `Energy += val`。
                    if (p.Attr == "energy") continue;
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
                        GrantEmbeddedAbility(ctx, t, p.Embedded, by, op.Source, unresolved, op.Duration);
                        continue;
                    }
                    // 🆕 **2026-09-16：代词载荷 `it as well`**（`Accursed Helbrute`）——
                    //   卡面 `When a friendly troop receives a ✦Dark Pact, this troop gains it as well`。
                    //   那个 `it` = **事件带过来的那份契约**，不是「随机一份」，所以**不能**走下面
                    //   `GrantDarkPact(…, p.Variant, …)`（`Variant` 是 null ⇒ 会随机抽一种，
                    //   队友拿「暴戾」它拿「命运」，卡面写的是「**同样**获得」）。
                    //   取法：事件主语在 `GrantDarkPact` 里**先写种类（`_keywordsPact[u] = k`）
                    //   再广播**（`BroadcastWhen(GetsDarkPact, …, u)`）⇒ 监听器结算时读得到。
                    var evPact = ctx.LastTarget;
                    // 🔴 **自递归护栏（必须有）**：Helbrute **自己**收到契约时，监听器会「再给自己一份」
                    //   ⇒ 再走一遍 case 17 ⟳，一路撞到 `BattleContext.MaxEffectChain = 8` 才被截断。
                    //   事件主语**已经拿到了** ⇒ 跳过它，环就断在这里。
                    //
                    // 🔴 **2026-10-25（K7 · 铁律 5 就地订正）：这一支走的是原版 `case 17`
                    //   （`lastMarkOfChaos`），⛔ 不是「再走一次 case 8」。** 上一版写的是
                    //   `GrantDarkPact(…, PactOf(evPact), …)` —— 那条会**再广播一次 510 + 重写槽 + 重新 Roll**
                    //   （`case 8` 的全套副作用），而原版 case 17 只有一句
                    //   `TriggerEnchantment(bm.lastMarkOfChaos, …)`（`.c:933-940`）。
                    //   ⇒ 现在读**全局槽**（原版 `bm.lastMarkOfChaos`），只重放效果那一半。
                    if (p.CopyEventPact && evPact != null && !ReferenceEquals(t, evPact))
                        ApplyLastMarkOfChaos(ctx, owner, t, by);
                    if (p.CopyEventPact) continue;
                    if (p.IsKeyword && p.Keyword == KeywordTable.DarkPact)
                    {
                        GrantDarkPact(ctx, owner, t, p.Variant, by);
                        continue;
                    }
                    ApplyOneGain(ctx, owner, t, p, sign, op.Duration, by, null, unresolved,
                                 op.AddTraitRemnant);
                }
            }
            // 🔴 **2026-10-20（`W58-TURN-A`）**：只有能量时**别报「给了 N 个目标」** ——
            //   它已按玩家自己结算过一次（见上面那段），而目标循环里一个都没给。
            bool hasEnergy = payload.Exists(x => !x.Unresolved && x.Attr == "energy");
            bool hasOther = payload.Exists(x => !x.Unresolved && x.Attr != "energy");
            if (hasEnergy && !hasOther)
                ctx.Log($"{by}：「{op.Source}」只含能量 ⇒ 已按**玩家自己**结算一次（⛔ 不按目标个数翻倍）");
            else
                ctx.Log($"{by}：「{op.Source}」给了 {targets.Count} 个目标");
            return true;
        }

        /// <summary>
        /// 属性名规范化。`GivePayload` 会给出 `melee` —— 它和 `attack` 是**同一个字段**
        /// （我们自己的 `rule_core.gd:3325`（旁证、非权威）把 `melee/might/fist/strength` 一并归到 `attack`，
        ///  见 `GivePayload.ReAttr` 那条注释）。
        /// ⚠️ 2026-09-12 撞到：这里原来是 `switch (p.Attr)`，`melee` 一个 case 都没命中 →
        ///    **黑暗契约的「+2 近战」静默丢失**（解析出来了、日志说给了、数没变）。
        /// </summary>
        /// <summary>
        /// 载荷里的属性词 → **引擎自己的字段名**。目前只有一条：`melee` → `attack`。
        ///
        /// 🔴 **为什么必须有它**：`GivePayload.ReAttr` 那条正则**认得 `melee`**，
        /// 但它的 `switch` **不映射** ⇒ `PayloadOp.Attr` 会留着 `"melee"`，
        /// 而 `UnitState.ApplyGrant` 的 switch 里**只有** `attack`/`ranged`/`health`/`armour`
        /// ⇒ **`+1 Melee` 会被静静丢掉**（解析成功、结算时无操作，日志一声不响）。
        ///
        /// ⚠️ **2026-09-14 A7 实做时踩到**：光环那条路（`Auras.ApplyAura`）一开始**绕过了这个函数**
        /// 直接 `RecordGrant(op.Attr, …)` ⇒ `Company Ancient` 的
        /// `Your other units have +1 Melee and +1 Ranged Attack` **只加了远程那一半**。
        /// ⇒ **属性词进 `RecordGrant` / 字段之前必须过这里**（判据只此一处）。
        /// ⚠️ 可见性是 `public` **不是** `internal` —— 自检在 **Editor 程序集**里、和 `Core` 不是一个
        ///    程序集，`internal` 它看不见（和 `CountForTest` / `HurtForTest` 同一个理由）。
        /// </summary>
        public static string NormalizeAttr(string attr)
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
                                 List<string> unresolved = null,
                                 // 🆕 2026-10-26（批次 4 · `F2`）：这一项增益的**投递类型**
                                 //   是不是原版 `buffType 90 addTraitRemnant`（来自 `EffectOp.AddTraitRemnant`）。
                                 //   ⛔ 别在别处另写一份判据 —— 闸③ 只在 `EffectResolver.cs` 那一处判。
                                 bool viaAddTraitRemnant = false)
        {
            int val = p.Value * sign;

            // 🔴 **`AddEffect` 闸③ 残骸门**（原版 `CardScript__AddEffect.c:159-170`）——
            //   **逐项**判（原版判的是每一项的 `healthChange`）：残骸只收「生命增减」与 `addTraitRemnant`。
            //   判据只此一处 = `AddEffectRemnantAllows`；这里是「`addStats` 这一族」的那个调用点。
            if (!AddEffectRemnantAllowsPayload(ctx, u, p, val, by, viaAddTraitRemnant)) return;

            // 🆕 2026-10-25（`E1` · 属性模型 Stage 2b）：这一条增益在**修饰表**里的那一格。
            //   非关键词条目建好后交给下面「限时 → 记一条」那一段（那边把它挂到 `TempBuff.Mod` 上，
            //   到期**摘格**而不是「减回去」）。`armour` 走关键词、不进修饰表 ⇒ 恒 `null`。
            UnitState.StatMod appliedMod = null;
            if (p.IsKeyword)
            {
                if (val >= 0)
                {
                    // 🆕 2026-10-24（引擎线 `G1b` · `C2` §6）：**施加前**是否已经在身上 ——
                    //   写进 `StatusApplied.Amount`（重施加 = 1 / 首次 = 0，见 `EvtKind.StatusApplied` 的 doc）。
                    bool hadKw = u.KwValue(p.Keyword) > 0;
                    // 🔴 **2026-10-25（`E1` · 第三段 D3/D4）**：`AddKeyword` 现在会**拒收**两种授予
                    //   （毒 × 抗性），所以它返回 `false` ⇒ **不能**再无条件 `EmitStatusApplied` / 广播，
                    //   否则会对外宣称「拿到了一个它其实没有的 trait」（静默不一致）。
                    // 🔴🆕 **2026-10-26（批次 9 · `W14EC` · 账 64 的另一半）：`addTrait` 正支改走【记录原语】。**
                    //   判据（🥇 `decomp_full/CardScript__AddEffect.c:469-568`，`W13EN` §二·7 逐句亲读）：
                    //   原版每一条 `addTrait(3)` / `addTraitRemnant(90)` 都在 `activeEffects(+0x108)` 里
                    //   留**一条 CardEffect 记录**，合并键 = 同 trait ∧ 同 `markOfChaosType(+0x78)` ∧
                    //   同 `daemonHostType(+0x7c)` ∧ `SameTimingAsEffect` ∧ **两边 extrinsic 都为 0**；
                    //   值 = **Σ 记录 `traitValue(+0x40)`**（`EntityScript__GetCurrentTraitValueWithModifiers.c`）。
                    //   `UnitState.ApplyTraitEffect` 就是那条记录的等价物：合并语义（add 累加 / replace 取 Max /
                    //   count 每次 +1 且新建强置 1）**加上**把增量落到 `_keywords` ⇒ 净读数与「N 次 `AddKeyword`」
                    //   逐字相同（本笔**不改任何数值**，只补「实例」：能回答「这是哪条、谁给的」、可被按记录摘）。
                    //   ⚠️ **只切「无限期」那一档**：
                    //     · `duration` 非空（`this turn` / `until your next turn`）= 限时路（下面 `AddTempBuff`），
                    //       到期由 `RevertBuffs` → `RemoveKeyword` 摘词条、**不会**摘记录 ⇒ 切过去会留下一本
                    //       永不清理的记录账 ⇒ **保持 `AddKeyword`**（如实记：原版连限时那条也建记录）；
                    //     · `val == 0`（空给）原版连记录都不建 ⇒ 保持 `AddKeyword`（**零行为变化**）。
                    //   ⚠️ `extrinsic` 恒 `false`：本仓 `extrinsic` 只有两个载体（`CardInstance.HandEffect.Extrinsic`
                    //     与光环 `Auras.GrantTag` → `UnitState.IsExtrinsicSource`），**都不经过本函数**
                    //     （光环那条走 `Auras` 的 `RecordGrant`）⇒ 这里判不到「来自 trigger 20/240」，见报告 §五·4。
                    //   ⚠️ `markOfChaosType` / `daemonHostType`：解析层没有这两列（`PayloadOp` 不带）⇒ 恒 0。
                    bool added;
                    if (val > 0 && string.IsNullOrEmpty(duration))
                        added = u.ApplyTraitEffect(p.Keyword, val,
                                   // `buffType 90 addTraitRemnant` 与 `3 addTrait` **共用同一个合并循环**
                                   //（`.c:558-566`）⇒ 只差这一个入参（投递类型由解析期置位，见上面那个形参）。
                                   buffType: viaAddTraitRemnant ? UnitState.BuffTypeAddTraitRemnant
                                                                : UnitState.BuffTypeAddTrait,
                                   stackType: -1,          // -1 = 按 `CardTrait._stackable` 真值现查（只有 markOfChaos = count）
                                   source: by, extrinsic: false, timingBits: 0) != null;
                    else
                        added = u.AddKeyword(p.Keyword, val);
                    if (!added)
                    {
                        ctx.Log($"{by}：「{u.Name}」身上有 **Resistant** —— 这次「{p.Keyword}」**没有挂上**"
                              + "（原版 `CardScript.AddEffect` 的毒 × 抗性闸）");
                        if (unresolved == null) { /* 出声即可，不算「没解析」 */ }
                    }
                    // `val > 0` 才算「加上了」（同下面广播那一族的口径；`+0` 是空给）。
                    if (added && val > 0) EmitStatusApplied(ctx, u, p.Keyword, hadKw);
                    // 🆕 事件层：`When an enemy gets Hunt Mark, …` / `When you gain [Shield], …`
                    // （2026-09-13 第三十四轮）—— **授予点就是这里**，原来只是没人广播。
                    // ⚠️ `val > 0` 才算「获得」：`val == 0` 是空给，不该触发「有人拿到了」。
                    // ⚠️ **只播这两个关键词**，别顺手推广成「任何关键词被给予」 ——
                    //    多播一种就多一类可能被误触发的监听器，而卡面并没有那种写法。
                    if (added && val > 0)
                    {
                        if (p.Keyword == "huntmark")
                        {
                            BroadcastKeywordEvent(ctx, WhenEventKind.GetsHuntMark, u);
                            // 🆕 **2026-10-25（执行代理 `X18` · 第二批）**：成就 `Ach_96`（`GiveHuntsMark`）计数
                            //   —— 与上一句广播**同一跳**（都是「这份猎杀标记加上了」）。
                            //   归属 = **施放者 `owner`**（`ApplyOneGain` 的第一个实参，来自 `DoGive` 的
                            //   `owner`）⇒ 判据 §八 N3 原来写「拿不到施放者」**不成立**，已就地更正。
                            //   按**目标个数**计（原版 `GiveHuntsMark__OnAddCardEffect.c` 每加一次 +1）。
                            ctx.Tally.AddHuntMark(owner);
                        }
                        else if (p.Keyword == KeywordTable.Shield) BroadcastKeywordEvent(ctx, WhenEventKind.GetsShield, u);
                        // 🆕 2026-10-12（`A1461` / `A1575`）：**标记光**也归这一族。
                        //   卡面：`Gains Long Range when an **enemy gains Markerlight**`（`TAU20 Sniper Drone`）。
                        //   ⚠：**全仓给 `markerlight` 的路只有本函数**（`GivePayload` → `ApplyOneGain`）——
                        //   `RuleCore.DeclareAttack` 那几处是**读**它、算完 `RemoveAll`，不是授予点；
                        //   `Aura` 那边也不给它。⇒ 只此一处要广播（多一处会双响）。🔴 **原版同形**（`d:/2/tools/decomp_full/`）：`CardScript__AddEffect.c:635/:637`（效果 id `0x492`）→ `BattleManager.BroadcastMarkerlightResolved`，而紧邻的 `:639/:643`（id `0x4ec`）就是猎杀标记那条 ⇒ 原版也是**同一口紧邻两条**。四跳形状见 `WhenEvent.GetsMarkerlight` 的注释。
                        else if (p.Keyword == "markerlight") BroadcastKeywordEvent(ctx, WhenEventKind.GetsMarkerlight, u);
                    }
                }
                else
                {
                    // 🆕 **2026-10-25（引擎线 `A2` · `A2-2`）：`lose <trait>` = 原版「加一条
                    //   `CardEffect{buffType = removeTrait(4), trait = X}`」** —— `CardScript__AddEffect.c:680-694`
                    //   的 `case 4`（先 `RemoveActiveEffects(4, trait)` 去重 → `List.Add` → `RemoveBuffedTrait`
                    //   + `RemoveTrait`）。`EntityScript.HasRemovedTrait` 读的正是那些条目
                    //   ⇒ 这一支必须写进 `UnitState` 的「已移除」簿记（写入点只此一处）。
                    // ⚠️ **只在「这次真的把它摘干净了」时写账**：原版 `RemoveTrait` 找不到 trait 就
                    //   `LogWarning` 直接返回、**不写账**；而「减一层还剩」不是「移除 trait」（原版那是
                    //   另一条 addStats 减值的路）。
                    int kwBefore = u.KwValue(p.Keyword);
                    bool hadKw = u.Has(p.Keyword);
                    // 🔴 **2026-10-25（`E1` · 第三段 D5-①）：`stealth` 被摘掉要广播 `LosesStealth`。**
                    //   判据 = 原版 `CardScript__AddEffect.c` 的 `case 4`（`removeTrait`）**第一步**：
                    //   `if (trait == 0x46 stealth && HasCurrentTrait(stealth)) BroadcastUnitLoseStealth(this);`
                    //   —— ⚠️ 它排在 `RemoveActiveEffects(4,T)` / `RemoveBuffedTrait` / `RemoveTrait` **之前**
                    //     （`CardScript.cs` 对应段的顺序），所以我们也在摘之前广播。
                    //   ⚠️ 池内这条**可达**：`lose Stealth` 出现在 6 张卡的正文里（`ASH63` / `AM43` / `DA27` /
                    //     `SAU43` / `GOF99` / `UM7`），`removeTrait` 计 14 条（stealth 9 / camouflage 4 / invulnerable 1）。
                    if (p.Keyword == KeywordTable.Stealth && hadKw)
                        BroadcastKeywordEvent(ctx, WhenEventKind.LosesStealth, u);
                    // 🔴 **2026-10-25（`E1` · 第三段 D5-②）：`lose <trait>` 是「**整条摘掉**」，不是「减一层」。**
                    //   判据 = `CardScript__AddEffect.c:680-698` 的 `case 4`：`RemoveActiveEffects(4,T)`
                    //   → `List.Add`（留下 removeTrait 记录，`HasRemovedTrait` 靠它）→ **`RemoveBuffedTrait(T)`**
                    //   （摘掉该 trait 的**所有非 extrinsic 授予**）→ **`RemoveTrait(T)`**（把词条本身摘掉）。
                    //   ⇒ 第 4+5 步合起来的净效果 = **该 trait 从身上消失**（不管原来叠了几层）。
                    //   ⚠️ 我们以前的 `RemoveKeyword(kw, -val)` 只减一层 —— 对**布尔 trait** 恰好等价，
                    //     对可叠的数值 trait 会少摘。**池内 14 条 `removeTrait` 全是布尔**
                    //     （`stealth` / `camouflage` / `invulnerable`）⇒ 本次改动**零行为变化**，
                    //     只是把口径改对（⛔ 别改回去）。
                    //   ⚠️ `blindedAtStartOfTurn`（D5-③）已经在 `RemoveAll` 里（`UnitState.cs` 的
                    //     `if (keyword == KeywordTable.Blind) BlindedAtStartOfTurn = false;`）⇒ 不用再来一遍。
                    // 🔴🆕 **2026-10-26（批次 4 · `F2` · `F3` 的 R4b-F2 判据落点）：这一处【必须】走
                    //    `RemoveAll` 的**非 silent** 形态** —— 它是原版 `CardScript__RemoveTrait`
                    //    （`AddEffect` 的 `case 4`，会 `UpdateSummonSicknessAnims` 重算），
                    //    ⛔ **不是** `RemoveTraitSilently`。三条路的对应关系（`F3` §2·4 已读全，别混）：
                    //      · `RemoveTrait`（会重算）        ← 本行（`lose <trait>` / `removeTrait`）；
                    //      · `RemoveTraitSilently`（不重算）← 原版只有两处调用点：
                    //        ① `CardScript__RemoveEffect.c:69`（撤一条效果时顺手摘它带的 trait）
                    //        ② `CardScript__TransformIntoRemnant.c:74`（翻残骸时逐个摘）
                    //        ⇒ **我们这侧两处都不需要改**（现读实测）：
                    //          ① 的等价位是 `UnitState.RevertBuffs` / `RemoveKeyword` —— `RemoveKeyword`
                    //             **本来就不重算** `fast`/`flank`，天然就是 silent 语义；
                    //          ② 的等价位是 `RuleCore.CleanupDeaths` 的残骸支（`RuleCore.cs:9501`）——
                    //             它 **new 一个全新 `UnitState`**，天然不带任何 trait ⇒ 「逐个摘」这一步
                    //             在我们这里**没有对应物**，净效果等价（`Exhausted` 也显式写 true）。
                    //     ⛔ 别把本行改成 `RemoveAll(p.Keyword, silent: true)` —— 那会让
                    //        `lose Fast` / `lose Flank` 之后召唤病**不回来**（与原版相反）。
                    u.RemoveAll(p.Keyword);
                    if (hadKw) u.MarkTraitRemoved(p.Keyword);
                    // 🆕 **2026-10-25（`E1` · 第四段 · W9）**：**摘掉了**也要有一个结构化信号（`Expiring` 那一支靠它）。
                    //   ⚠️ 只在「真的有过」时发（`kwBefore > 0`）—— 摘一个本来没有的 trait 不该演「消失」。
                    if (kwBefore > 0) EmitTraitRemoved(ctx, u, p.Keyword, kwBefore);
                }
            }
            else if (grantSource != null && p.Attr != "energy")
            {
                appliedMod = u.RecordGrant(NormalizeAttr(p.Attr), val, grantSource);
            }            else
            {
                switch (NormalizeAttr(p.Attr))
                {
                    // 🔴 **2026-10-25（`E1` · 属性模型 Stage 2b）**：`attack` / `ranged` **不再写字段** ——
                    //   落一条**修饰**（= 原版 `buffType == 1 addStats` 的 `meleeAttackChange` /
                    //   `rangeAttackChange` 那一格）。「当前值」由 `UnitState.CurrentMeleeAttack`
                    //   现算（正序扫 + `setMeleeAttack` 命中即 return，判据 `get_CurrentMeleeAttack.c:29-67`）。
                    //   ⚠️ **限时**（`duration` 非空）的那一种**不在这里建** —— 交给下面
                    //   「限时 → 记一条」那一段（`AddTempBuff` 会在那里建同一条修饰），
                    //   否则一次施加会记两条修饰（数值翻倍、且到期只摘得掉一条）。
                    case "attack":
                        if (string.IsNullOrEmpty(duration)) appliedMod = u.AddStatMod("attack", val);
                        break;
                    case "ranged":
                        if (string.IsNullOrEmpty(duration)) appliedMod = u.AddStatMod("ranged", val);
                        break;
                    // 🔴 **2026-10-25（Stage 2b + K1）**：生命**正加**当场加到当前生命（原版
                    //   `CardScript__AddEffect.c:331-346` `healthChange >= 1 ⇒ currentHealth += healthChange`，
                    //   **没有向上夹上限** —— 可以超过上限）；
                    //   **负值不在这里减**：原版那一支是 `currentHealth = min(currentHealth,
                    //   get_currentMaxHealthProcessed())`（**只夹、不减**，而 `processed` 上限此时
                    //   已经把这笔负值算进去了）⇒ 夹的动作放到本方法**末尾**（那时修饰一定已进表）。
                    //   ⚠️ 以前这里是两条都 `Health += val` + 物化 `MaxHealth` ⇒ 负值会**多减一次**。
                    case "health":
                        if (val >= 1) u.Health += val;
                        if (string.IsNullOrEmpty(duration)) appliedMod = u.AddStatMod("health", val);
                        break;
                    // 负甲 clamp 0（原版 `:3320`：防「负甲反而加伤」）
                    //
                    // 🔴 **2026-10-16（账 `A2015`）：护甲要【两种表示一起动】。**
                    //   改前这一行**只写 `u.Armor`、不同步 `_keywords["armour"]`** ——
                    //   `+2 Armour` 之后 `KwValue`/`Has("armour")` 仍是 0（同一件事两种表示脱节）。
                    //   判据 = `CardDef` 构造函数头部那段（`Core/CardDef.cs:269-271`，**本笔现读**）：
                    //     「**全仓的护甲读点 = `_keywords[KeywordTable.Armour]` 这一格**（`KwValue` /
                    //      `Has` / `UnitState.Armor`（`UnitState.cs:594` 构造里那句
                    //      `Armor = KwValue(KeywordTable.Armour)`），全部走它）……
                    //      **结算一律读 `KwValue`/`Has`**」
                    //   ⇒ 只写字段那一半 = 让「读数收成一处」那条口径落空。
                    //   ⚠️ **原版是「同一件事两种表示」**：trait 那一条（= 我们 `_keywords`）
                    //     与 `+0xb8` 那个 ObscuredInt（= 我们 `u.Armor`）授甲时**一起动**
                    //     （`CardScript._ReceiveDamage_d__381__MoveNext.c:64-98` 结算读的是 `+0xb8`，
                    //      而 `CardScript__AddEffect` 的授甲是按 trait 走）。
                    //   ⚠️ **同步只此一份**：`UnitState.SyncKeywordState` 里那句
                    //     `if (keyword == Armour) Armor += value;`（`Core/UnitState.cs:827`），
                    //     由 `AddKeyword` 调；`RemoveKeyword` 那一半只动关键词、**不动 `Armor`**
                    //     （`Core/UnitState.cs:831-837`）⇒ 下面减那一支要把字段那一半自己补上。
                    //   🔴 **2026-10-19 就地订正（`T01 §六·6`②）：上一句「`RemoveKeyword` 那一半
                    //     只动关键词、不动 `Armor`」**已不成立** —— `RemoveKeyword` 现在自己会
                    //     同步减 `Armor`（判据与理由见 `Core/UnitState.cs` 那个方法里的注释）。
                    //     ⇒ 下面减那一支**只走 `RemoveKeyword`、⛔ 别再自己写 `u.Armor = …`**
                    //     （会减两次）。
                    //   ⚠️ **负甲 clamp 0 照旧**（原版 `:3320`）：`delta` 取的是**夹过之后**的增量
                    //     ⇒ `Armor` 已经是 0 时再来一个负值 ⇒ `delta == 0` ⇒ 两处都不动。
                    //   ⚠️ **今天不是活缺陷**：全仓只有 `t.Armor > 0` 在读它（本文件
                    //     `targethasarmour` 那一格，**现读 `EffectResolver.cs:7514`** ——
                    //     账上写的是 `:7469`，那是本笔改动**之前**的读数；改动都在它上面 ⇒ 往下漂了）⇒ 「给甲」这条路
                    //     改前改后**行为差异 = 0**；改的是「两处表示会不会脱节」。
                    //   🔴 **同族的另一个写点**：`UnitState.ApplyGrant` 的 `case "armour"`
                    //     （`Core/UnitState.cs`）原来**一模一样地只写 `Armor`** ——
                    //     **2026-10-19（`T01 §六·6`①）已按本支同一个形状修好**（走
                    //     `AddKeyword` / `RemoveKeyword` 两个口）。⛔ 别把任何一处改回裸写字段。
                    case "armour":
                    {
                        int before = u.Armor;
                        int after = System.Math.Max(0, before + val);   // 原版 `:3320` 负甲夹 0
                        int delta = after - before;                     // 夹过之后的实际增量
                        if (delta > 0)
                        {
                            // 加：走 `AddKeyword` ⇒ `SyncKeywordState` 把 `Armor` **一起**加上 `delta`
                            // （⛔ 这里**别**再自己写一遍 `u.Armor`，会加两次）。
                            // 🆕 2026-10-24（引擎线 `G1b` · `C2` §6）：状态施加事件（`armour` 是
                            //   13 个**有 Start(0) 行**的 trait 之一 ⇒ 重施加要重播 `ArmourEffect`）。
                            u.AddKeyword(KeywordTable.Armour, delta);
                            EmitStatusApplied(ctx, u, KeywordTable.Armour, before > 0);
                        }
                        else if (delta < 0)
                        {
                            // 减：`RemoveKeyword` 现在**自己会同步 `Armor`**（`T01 §六·6`②，
                            // 见 `Core/UnitState.cs` 那个方法里的注释）⇒ 这里只走它，⛔ 别再补字段。
                            u.RemoveKeyword(KeywordTable.Armour, -delta);
                        }
                        break;
                    }
                    case "energy":
                        // 🔴 **2026-10-20（`W58-TURN-A`）**：照原版 `ManaBuff.createMana` → `AddNewMana`：
                        //   **永久抬最大能量、当前能量不动**（原来这里是 `Energy += val`，把 `Gain` 当成了 `Refill`）。
                        //   ⚠️ `DoGive` 已经在目标循环**之前**单独结算过一次（不逐目标）⇒ 正常路径到不了这里；
                        //   这一支只给其它调用点兜底（今天只有 `GrantDarkPact`，它不载荷能量）。
                        ctx.Players[owner].MaxEnergy += val;
                        ctx.Vars.AddManaBonus(owner, val);
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
                    // 🔴 **2026-10-25（Stage 2b）**：非关键词要过 `NormalizeAttr`（`melee → attack`）。
                    //   以前这里直接写 `p.Attr` ⇒ 名字是 `"melee"` 时 `RevertBuffs` 的
                    //   `switch (b.Name)` 一个 case 都命中不了（**到期什么都不撤**，静默）。
                    //   现在数值走修饰表，名字不对就是「这条增益根本不存在」——必须归一。
                    Name = p.IsKeyword ? p.Keyword : NormalizeAttr(p.Attr),
                    Value = val,
                    Owner = owner,
                    UntilMyNextTurn = duration == "nextturn",
                    Src = by,
                    // 真卡名（`by` 在战术卡那条路上恒为「战术卡」，不足以定位「本卡施加的」）
                    SourceCard = ctx.PlayingCard != null ? ctx.PlayingCard.Name : by,
                    // 带来源那条路（`RecordGrant`）已经建好了修饰 ⇒ 复用它，⛔ 别再建一条。
                    Mod = appliedMod,
                });
            }

            // 🔴 **2026-10-25（`E1` · K1）**：`addStats.health` 的**负值那一支** ——
            //   原版 `CardScript__AddEffect.c:331-346`：
            //     `if (healthChange >= 1) currentHealth += healthChange;`
            //     `else currentHealth = min(currentHealth, get_currentMaxHealthProcessed());`
            //   ⚠️ **只夹、不减**，而且夹的是**已经含这笔负值**的 processed 上限。
            //   ⇒ 必须放在「限时 → 记一条」**之后**（那时修饰一定已经进表）。
            if (!p.IsKeyword && p.Attr != "energy" && NormalizeAttr(p.Attr) == "health" && val < 1)
                u.ClampHealthToMax();

            // 🆕 **2026-10-18（`W5` · `K3` 账 · `+0x120` 第 10 个写点）**：
            //   攻值一变 ⇒ **按当前数值重挑攻击型**（原版 `CardScript.UpdateAttackText`，
            //   `CardScript__UpdateAttackText.c:21-29`）—— 它被**所有改属性的地方**调用
            //   （`AddEffect` / `ChangeBaseAttack` / `UpdateFigures` / `ReactToUnitJammed`），
            //   这里是我们「逐条增减益落到单位上」的那一处（`ApplyOneGain`）。
            //   ⚠️ 只对**攻/远攻**重挑（`health`/`armour` 改了不影响「近战还是远程」的取舍）——
            //     原版那个函数虽然会被各条路调到，但重挑的算式只吃这两个值。
            //   ⚠️ 走**按引用找格号**的那一支（`RecomputeCurrentAttackTypeOf`）：本方法手上只有
            //     `UnitState`、没有格号；不在场上（分离实例）⇒ 什么都不做。
            //   ⚠️ 原版 `+0x40`（`isPlayer`）那个守卫**我们不复刻** —— 逐条理由见
            //     `UnitState.SyncAttackTypeAfterStatChange` 的注释（`Core/` 没有本机概念）。
            if (!p.IsKeyword && (NormalizeAttr(p.Attr) == "attack" || NormalizeAttr(p.Attr) == "ranged"))
                RuleCore.RecomputeCurrentAttackTypeOf(ctx, u);
        }

        // ==================================================================
        //  黑暗契约（Dark Pacts）
        // ==================================================================

        /// <summary>
        /// 黑暗契约的四种**效果文本**（`give` 载荷的写法）。
        ///
        /// 🔴 **2026-10-25（K7 · 铁律 5 就地订正）：这一段原来写「回反编译查过，判据查不到」——
        /// 那是【没查对地方】，判据现已找到（两重）：**
        ///   ① 原版 `CardScript__AddEffect.c:776-792`（case 8）把 `newEffect.markOfChaosType(+0x78)`
        ///      交给 `Enchantments.GetMarkOfChaos`，取到的是一张**附魔卡**，然后 `TriggerEnchantment`
        ///      它（= 触发它的 `TriggerAbilities(卡, 500)`）；
        ///   ② 那 4 张附魔卡就在 `cards_truth.json` 里（`cardType 20 / spellType 190`），**它们的 500 能力
        ///      就是这四份效果**（逐条见 `ApplyMarkBody` 的头注）。`Enchantments.json`
        ///      （`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/`）把
        ///      `MarkOfChaosType 10/20/30/40` 各映射一张、`50 Random` **映射 4 张**（⇒ 均匀四选一）。
        ///   ⇒ 本表与那四张卡的**卡面正文逐字一致**，不是「旁证凑出来的」。
        ///   （上一版把来源写成 `rule_core.gd:1655` 的 `DARK_PACT_FX`（我们自己的 Godot 复刻）——
        ///    结论碰巧相同，但**判据换成上面这两重**。）
        /// ⚠️ 顺序照抄原版字典的字面顺序 —— `random` 掷骰按这个序，改顺序会改对局结果。
        /// ⚠️ 四种在我们的卡池里叫 `Dark Pact of Fate/Blood/Excess/Resilience`；SO 真值里**同一个 id**
        ///    叫 `Mark of Tzeentch/Khorne/Slaanesh/Nurgle (enchantment)`。
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
        ///   · 随机种类走 `ctx.GetRandomInt`（原版预烧表）而不是 `randi()` —— 2026-10-27 `E1RNG` 前是 `ctx.Rng`
        ///     （见 `RuleCore` 文件头）。
        ///   · 原版提取效果文字时拿的是**小写副本**，于是 `EffectSpec.Parse` 永远失配（等于这一步没生效）。
        ///     我们保留原始大小写。**这是补原版的漏，不是抄错。**
        /// </summary>
        public static void GrantDarkPact(BattleContext ctx, int owner, UnitState u, string kind, string by)
        {
            // 🔴 **2026-10-22（`W62` · R1）：这个入口也会入队**（②③ 从 2026-10-21 起只入队，
            //   ④ 的 `ApplyOneGain` 还可能再入队）⇒ 必须自带 `ActionScope`，否则自检/夹具直调时
            //   那批监听器永远不结算（**静默**：泵点漏了只在**外层**作用域退出时才报）。
            //   形状与同族入口一致（`using (BeginAction(ctx)) …Core(…)`，照 `RuleCore.ApplyDamage`）。
            using (BeginAction(ctx)) GrantDarkPactCore(ctx, owner, u, kind, by);
        }

        /// <summary><see cref="GrantDarkPact"/> 的原文（2026-10-22 `W62` · R1 抽出，逐字搬下来）。</summary>
        static void GrantDarkPactCore(BattleContext ctx, int owner, UnitState u, string kind, string by)
        {
            if (u == null) return;

            string k = kind;
            if (string.IsNullOrEmpty(k) || k == "random" || !HasPact(k))
            {
                // 🆕 2026-10-19（`A1960`）：**先看刚选中的那张卡** —— 原版用 `choicesDefinition`
                //   的 `filterSpecificCards`（四张 `Mark of …`）让玩家挑一份契约，选中那张就是种类。
                //   我们的选择面板把选中那张写进 `ctx.LastChosenCard`（`DoChooseCard` ②）⇒ 这里消费它。
                //   🔴 **只认 `Dark Pact of <种类>` 那四张**（`PactKindOfChosenCard`）；不是契约卡 ⇒ 走随机。
                //   ⚠️ `LastChosenCard` 按「一条能力一个窗口」清（`ResolveOps` 入口）⇒ 不会读到上一张卡的。
                string fromChoice = PactKindOfChosenCard(ctx.LastChosenCard != null ? ctx.LastChosenCard.Card : null);
                if (fromChoice != null)
                {
                    k = fromChoice;
                    ctx.Log($"{by}：黑暗契约的种类取自**刚选中的那张**「{ctx.LastChosenCard.Card.Name}」");
                }
                else
                {
                    k = DarkPactFx[ctx.GetRandomInt(0, DarkPactFx.Length)][0];
                }
            }

            // 🔴 **2026-10-25（K7 · 铁律 5 就地订正）：上一版这里写「同一单位只能有一份契约、换一份就把
            //   旧的增益收回」—— 判据不成立，已改。**
            //
            //   原版 `CardScript__AddEffect.c:776-792`（case 8）**没有**「摘掉旧印记」这一步：
            //   它只做 `TriggerEnchantment(新的那张附魔卡)`（⇒ 那条能力的 `addTrait(330) + addStats + 关键词`
            //   照常累加）→ 写 `bm.lastMarkOfChaos` → 广播。`addStats` 本来就是**累加**语义
            //   （`.c:385-402`：`meleeAttackChange/rangeAttackChange/healthChange` 逐项 +=），
            //   `bm.lastMarkOfChaos` 也只是「**最后一次**是哪一份」，**不撤前一份**。
            //   ⇒ 一个单位身上可以有**多份**印记（`BL46 Gifts of Chaos` 就是连给 2 份，见 §K7 判据）。
            //   ⚠️ 「层数」在我们这边的代理 = `KeywordTable.DarkPact` 的**计数**（`CountFor` 的
            //      `darkpact` 分支读 `KwValue`，`EffectText` 也写明「契约可叠加、不是布尔」）
            //      ⇒ 每落一份 **+1**（⛔ 别再 `RemoveAll` 归 1 —— 那会让 `BL48` 每份契约只抽到 1 张）。
            _keywordsPact[u] = k;                        // 旁表只记**最后一份**的种类（原版 `lastMarkOfChaos` 的逐单位镜像）
            SetLastMarkOfChaos(ctx, k);                  // 原版 `bm.lastMarkOfChaos(+0x2f0) = raw`（**全局**一格，case 17 读它）
            ctx.Log($"{by}：{u.Name} 获得「{PactNameZh(k)}」黑暗契约"
                  + $"（层数 {(u.KwValue(KeywordTable.DarkPact) + 1)}）");

            // 🆕 2026-10-19（战斗线 `T21` 缺口 B）：**附魔卡自己那一拍** —— 原版
            //   `CardScript__AddEffect.c:776-792`（case 8）→ `Enchantments.GetMarkOfChaos(...)`
            //   → `RawCardScript__TriggerEnchantment.c:7-13`：`TriggerAbilities(附魔卡, 0)` +
            //   `TriggerAbilities(附魔卡, 500)`。
            //   ⚠️ 触发的是**附魔卡自己的 `cardAnimations[]`**（四张 `Dark Pact of *` 的
            //   `Buff_MarkOf*`），**不是载体单位那条** ⇒ 事件的 `CardId` 要写**附魔卡名**，
            //   落点仍是载体那一格。⛔ 别改成载体卡名 —— 那会去查单位自己的 `trigger` 行，
            //   四张契约的动画永远播不出来（判据与数据现状 → `资料/普查产出_1019/W_战斗线_T21现核_1019b.md` §三 B）。
            EmitEnchantmentTrigger(ctx, u, k);

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
                // 🆕 2026-10-19（`T21` · 缺口 E 的「自发段」）：原版这条走的也是
                //   `CardScript__ReactToUnitMarked.c:7`（`OnTrigger(0x1fe = 510)`）——
                //   给它发一条按**自己卡名**的事件（`BL3` / `BL12` / `BL69` 的动画行就挂在这）。
                //   ⚠️ 今天这三张原版卡走的是**事件层**（`WhenTriggers` + `SelfOnly`），
                //   本支只覆盖 `EffectSpec` 那条旧路（`FromOriginalPool == false`）⇒
                //   两条**互斥**、不会双发（见 `BodyResolves` 的口径）。
                EmitListenerTrigger(ctx, card, owner, u, WhenEventKind.GetsDarkPact);
                ctx.Log($"{u.Name} 的「After receiving a Dark Pact」触发");
                // 🔴🆕 2026-10-21（审查 `W61b` 第 2 条 · CONFIRMED）：**只入队，不就地结算**。
                //   判据 = 本支自己的注释上面那一条：原版走 `CardScript__ReactToUnitMarked.c:7`
                //   （`OnTrigger(0x1fe = 510)`）→ `TriggerAbilities` → `PlayAbility`
                //   → `AddAutoActionToQueue` ⇒ 和同一个 `GetsDarkPact` 事件的其余监听器
                //   （上一行 `BroadcastWhen` 已经只入队）**同一条道、按入队序结算**。
                //   ⚠️ 就地 `ResolveEffect` 会把同一事件的听众拆成两段（②③ 抢在泵点之前），
                //      而且 ②③ 里再产生的触发也脱离了本动作边界 —— 这正是本批要消灭的形态。
                //   ⚠️ 触达面如实记：只有 `FromOriginalPool == false` 的旧 `EffectSpec` 卡走这条
                //      （原版三张 BL 卡走事件层），**今天真池 0 张可达**（铁律 11：照样要接）。
                EnqueueAbility(ctx, ListenerLane(card), (u.Name ?? "?") + "·darkpact-self",
                               () => DarkPactReactDeferred(ctx, owner, u, selfFx, "收到黑暗契约后"),
                               // 施放者 = 拿到契约的那个单位（原版 context `Play(0)`）
                               // 🔴 **2026-10-22（`W63` · C6）：保持 `requireActorInPlay: true`，不传 `abilityContext`。**
                               //   这一支只覆盖 `FromOriginalPool == false` 的旧 `EffectSpec` 卡（见上面那段注释）
                               //   ⇒ 它们**不在原版卡池里、没有 `abilityContext` 行**（`AbilityContextsVersioned`
                               //   也不会是 true 的那一路）⇒ 「原版 `Play(0)`」只是注释里的推断，
                               //   数据确认不了 ⇒ 按简报 §4·C6 保持现状。
                               actor: u, requireActorInPlay: true);
            }

            // ③ 同伴广播：「当友方部队收到黑暗契约时…」（原版只找**第一个**这样的单位就 break）
            var ps = ctx.Players[owner];
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var hv = ps.Board[s];
                if (hv == null || hv == u) continue;
                var fx = hv.Card != null ? hv.Card.Effect("when a friendly troop receives a dark pact") : null;
                if (fx == null) continue;
                // 🆕 2026-10-19（`T21` · 缺口 E）：同上 —— 按**反应者**（同伴）的卡名发事件。
                EmitListenerTrigger(ctx, hv.Card, owner, hv, WhenEventKind.GetsDarkPact);
                ctx.Log($"{hv.Name} 的「When a friendly troop receives a Dark Pact」触发");
                // 🔴🆕 2026-10-21（审查 `W61b` 第 2 条）：同 ② —— 只入队（判据同上）。
                var _hv = hv;
                EnqueueAbility(ctx, ListenerLane(_hv.Card), (_hv.Name ?? "?") + "·darkpact-ally",
                               () => DarkPactReactDeferred(ctx, owner, _hv, fx, "友方收到黑暗契约时"),
                               // 🔴 2026-10-22（`W63` · C6）：同上（`EffectSpec` 旧路、无 `abilityContext` 数据）
                               //   ⇒ 保持 `requireActorInPlay: true`，不传 `abilityContext`。
                               actor: _hv, requireActorInPlay: true);
                break;
            }

            // ④ 契约自身效果 —— 永久（不带时长）。**与 case 17 共用同一份**（`ApplyMarkBody`）。
            ApplyMarkBody(ctx, owner, u, k, by);
        }

        // ==================================================================
        //  🔴 K7：`markOfChaos(8)` / `lastMarkOfChaos(17)` 的共用落点
        // ==================================================================
        //
        //  **判据（第一权威，现读）**：
        //   · `CardScript__AddEffect.c:776-792`（case 8）：Log → `Enchantments.GetMarkOfChaos(bm.enchantments,
        //     newEffect.markOfChaosType(+0x78))` → 取不到就**整条跳过** → `RawCardScript.TriggerEnchantment(
        //     附魔卡, bm, this, this, false, 0)` → `bm.lastMarkOfChaos(+0x2f0) = 附魔卡` →
        //     `BattleManager.BroadcastMarkResolved(this)`。
        //   · `CardScript__AddEffect.c:933-940`（case 0x11 = **17**）：`bm.lastMarkOfChaos` 为空就跳过；
        //     否则 `TriggerEnchantment(bm.lastMarkOfChaos, …)` —— **不写槽、不广播、不取新的**。
        //   · `RawCardScript__TriggerEnchantment.c:7-13` = `TriggerAbilities(附魔卡, 0)` + `(附魔卡, 500)`；
        //     附魔卡 = 4 张 `Mark of X (enchantment)`（`cards_truth.json` 的 id 见下），它们的 **500 能力**
        //     就是这份印记的真实效果（`decomp_full` + SO 真值，逐条：
        //       10 Khorne   `9a2cadc64c80a6b42912441105bb1cee` = addTrait(330) + melee +2 + addTrait(980 vanguard)
        //       20 Nurgle   `1dfc9bc738bfc4f4d93d48f165aee510` = addTrait(330) + addTrait(1030 regen,1) + health +1
        //       30 Slaanesh `b8e81b2fe85cda747a68c4d6326b669c` = addTrait(330) + melee +2 + range +2
        //       40 Tzeentch `4a48457c24877f345be2e96f867c1e94` = addTrait(330) + addTrait(985 camouflage) + health +2
        //         ⚠️ Tzeentch 那条 `addTrait(330)` 上还挂着 `costChange: 1` —— **死值**：`case 3` 的
        //         addTrait 分支**不读** `+0x68`（只有 `case 1/2/10` 的 addStats 读，`.c:385-402`）⇒ 不入效果。
        //       50 Random = `Enchantments.json` 的 `cardEffect: 50` 那一格**挂了 4 张**卡 ⇒ 四选一（均匀）。
        //     ⚠️ 这 4 张卡在**我们卡池里**叫 `Dark Pact of Blood/Excess/Fate/Resilience`（id 完全相同），
        //        所以 `DarkPactFx` 的四句和它们的**卡面正文**逐字一致 —— 那张表不是「旁的来源」，
        //        它正是这四条数据的人读版（`EntityScript` 的 tsa/keyword 值也在这里对上：
        //        980 = vanguard · 985 = camouflage · 1030 = regeneration，`DefinedTrait.cs:94-101`）。
        //
        //  **本仓两处模型差异（如实标注）**：
        //   ① 原版调 `GetMarkOfChaos` 取不到附魔卡 ⇒ **整条 case 8 不发生**（不落任何效果）。
        //      我们的效果**不来自那张卡的数据**（是 `DarkPactFx` 硬表）⇒ 不把「卡池里有没有那张卡」
        //      当成前置闸（否则夹具的小卡池会让印记凭空消失）。缺卡时只有 `EmitEnchantmentTrigger` 出声。
        //   ② 原版「印记层数」= trait `330 markOfChaos` 的数值；我们另外还维护 `KeywordTable.DarkPact`
        //      的计数（`for each Dark Pact on it` 读它）。**两种表示一起加**（本方法一处写）。

        // 🔴 trait **330 = `markOfChaos`** 的常量已并进 `KeywordTable.MarkOfChaos`（`CardDef.cs`，
        //   2026-10-25 调度台授权后搬走）—— ⛔ 别在这里再定义一份（铁律 6）。
        //   ⚠️ 它**不在** `KeywordTable.Implemented` 里，那是**故意的**：330 没有「每关键词一条机制」，
        //   它只是计数/标记；放进 `Implemented` 会让战斗界面渲染一个原版没有的徽章（理由写在那个常量上）。

        /// <summary>`BattleManager.lastMarkOfChaos`（`+0x2f0`）—— **本局最后一次落地的印记**。
        /// 存成「ctx + 种类」而不是 `Dictionary&lt;BattleContext,…&gt;`：不随对局数增长，换局由
        /// <see cref="ResetTransientForNewBattle"/> 清。</summary>
        static BattleContext _lastMarkCtx;
        static string _lastMarkKind;

        static void SetLastMarkOfChaos(BattleContext ctx, string kind)
        {
            _lastMarkCtx = ctx;
            _lastMarkKind = kind;
        }

        /// <summary>本局最后一份印记的种类；没有（或换了局）⇒ `null`（原版「槽是空的」。</summary>
        public static string LastMarkOfChaos(BattleContext ctx)
            => (ctx != null && ReferenceEquals(_lastMarkCtx, ctx)) ? _lastMarkKind : null;

        /// <summary>
        /// **把一份印记落到单位身上** —— 原版 case 8 与 case 17 **共用的那一半**
        /// （= `TriggerEnchantment` 触发的附魔卡 500 能力：`addTrait(330)` + `addStats` + 关键词）。
        /// ⛔ 别在 case 8 / case 17 各写一套（两处写同一条规则 = 迟早不一致）。
        ///
        /// 落三样：① trait `markOfChaos`(330) +1（原版每个印记第一条 logic 就是它）
        /// ② `darkpact` 计数 +1（我们的层数代理，`CountFor` / `EffectText` 都读它）
        /// ③ `DarkPactFx` 那一句的载荷（= 那张附魔卡 500 能力的 `addStats` + 关键词）。
        /// </summary>
        static void ApplyMarkBody(BattleContext ctx, int owner, UnitState u, string k, string by)
        {
            if (ctx == null || u == null) return;

            // ① + ②（同一次写入，两个表示都要）
            u.AddKeyword(KeywordTable.MarkOfChaos, 1);
            u.AddKeyword(KeywordTable.DarkPact, 1);
            // 🆕 **2026-10-25（执行代理 `X18` · 第二批）**：成就 `Ach_6`（`GiveMarkOfChaos`）计数 ——
            //   判据 = 原版 `GiveMarkOfChaos__OnAddCardEffect.c`：`isKeyword && buffType == 0x14a`
            //   （**0x14a = 330 = `markOfChaos`**）⇒ **每加一份印记 +1**。
            //   ⇒ 落点跟着**这一跳**走（case 8 与 case 17 都算 —— 原版两条都经 `TriggerEnchantment`
            //   的 `addTrait(330)`，所以都该计）。归属 = **施放者 `owner`**。
            ctx.Tally.AddMarkOfChaos(owner);

            // ③ 契约自身的属性/关键词
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
                // ⚠️ `grantSource` 照旧传种类名：属性那一半会进 `UnitState` 的 grant 账
                //    （`RecordGrant`），日后若要按来源追溯还能查到是谁给的。
                ApplyOneGain(ctx, owner, u, p, +1, "", by + "（黑暗契约）", k);
            }
        }

        /// <summary>
        /// **`lastMarkOfChaos(17)` 的落点** —— 原版 case 17：把**本局最后那一份**印记重放到 `u` 身上。
        /// 与 case 8 的三点不同（⛔ 别照着 case 8 抄）：**不取新的种类、不写槽、不广播 510**。
        /// 效果那一半与 case 8 共用 <see cref="ApplyMarkBody"/>。
        /// </summary>
        static void ApplyLastMarkOfChaos(BattleContext ctx, int owner, UnitState u, string by)
        {
            string last = LastMarkOfChaos(ctx);
            if (string.IsNullOrEmpty(last))
            {
                // 原版 `if (bm.lastMarkOfChaos == null) goto tail`（**静默跳过**）—— 我们出声。
                ctx.Log($"{by}：「{u.Name}」要重放**最后一份**黑暗契约，但本局还没有任何印记落地 ⇒ 什么都不发生"
                      + "（原版 `CardScript__AddEffect.c:933-940` 那一支是空的）");
                return;
            }
            ctx.Log($"{by}：「{u.Name}」重放**最后一份**印记「{PactNameZh(last)}」"
                  + "（原版 `lastMarkOfChaos(17)` —— 不取新的、不广播）");
            ApplyMarkBody(ctx, owner, u, last, by);
            // 🔴 **2026-10-26（`E11` · 诊断 `D1` #2 · 上一批 `K7` 的漏项）**：旁表补一笔 ——
            //   原版 `BattleManager` 的 case 17 把 `lastMarkOfChaos` **直接塞进 `newEffect.enchantment`**
            //   （**同一份**，不重掷）⇒ 这个单位身上「最后一份契约的种类」也应该变成它。
            //   ⚠️ `ApplyMarkBody` 是 case 8 / case 17 **共用**的那一半，里面**没有**这一笔（那是 case 8 的槽写入）。
            //   ⛔ **不要**在这里 `SetLastMarkOfChaos`（全局槽不该被重放覆盖 —— 原版 case 17 `:943-960` 不写槽）；
            //   ⛔ **不要**广播 510 / 重新 Roll（原版 case 17 不取新种类）。
            _keywordsPact[u] = last;
        }

        /// <summary>
        /// 契约种类的旁表：`UnitState` 的关键词字典是 `&lt;string,int&gt;`，装不下 `blood`/`fate` 这种名字。
        /// 不塞进 `UnitState` 是为了不把「一个阵营的机制名词」渗进通用单位状态。
        /// </summary>
        static readonly Dictionary<UnitState, string> _keywordsPact = new Dictionary<UnitState, string>();

        /// <summary>🆕 2026-10-23（引擎线 `新账-89` ③）：清掉**跨局静态旁表**。
        /// 唯一调用点 = `RuleCore.NewBattle`（每局开头）；本表只记「这个单位拿到的是哪一种黑暗契约」，
        /// 键是 `UnitState`（每局新对象）⇒ 跨局没有意义，留着只会让长会话里的旧单位引用泄漏。</summary>
        public static void ResetTransientForNewBattle()
        {
            _keywordsPact.Clear();
            // 🆕 K7：`lastMarkOfChaos`（原版 `BattleManager +0x2f0`）也是**逐局**的 ——
            //   不清的话上一局的最后一枚印记会被下一局的 case 17 读到（静默错）。
            _lastMarkCtx = null;
            _lastMarkKind = null;
        }

        /// <summary>
        /// 🆕 2026-10-21（审查 `W61b` 第 2 条）：**黑暗契约的「自触发 / 同伴触发」两条队列体**
        /// —— 原同步段那两句 `ResolveEffect(...)` 逐字搬进来（⛔ 别在这里改次序/语义）。
        ///
        /// 判据（原版 `CardScript__ReactToUnitMarked.c:7` → `TriggerAbilities` → `PlayAbility`
        /// → `AddAutoActionToQueue`）⇒ 它和同一个 `GetsDarkPact` 事件的其余监听器**同队同道**，
        /// 由泵点按道序 + FIFO 结算 —— **不抢占当前动作**。
        /// ⚠️ `EffectChain` 的 `++/--` 留在体内（它量的是**嵌套深度**，不是排队长度；
        ///    和 `FireTriggerDeferred` 同一个形状）。
        /// ⚠️ 上下文全部从形参进来（施放者 / `EffectSpec` 在入队那一刻定死）——
        ///    出队时 `ctx` 上的代词槽可能已被别的动作改过（同 `QueuedAction.Execute` 的口径）。
        /// </summary>
        static void DarkPactReactDeferred(BattleContext ctx, int owner, UnitState u,
                                          EffectSpec fx, string label)
        {
            if (ctx == null || u == null || fx == null || ctx.IsOver) return;
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{u.Name} 的「{label}」不再结算");
                return;
            }
            ctx.EffectChain++;
            try { ResolveEffect(ctx, owner, u, fx, null); }
            finally { ctx.EffectChain--; }
        }

        /// <summary>种类 → **卡池里那张契约卡**（4 张 `Dark Pact of X`；同一批 id 在 SO 真值里叫
        /// `Mark of X (enchantment)`）。找不到 ⇒ `null`（调用方各自决定「出声+跳过」还是「退回随机」）。
        /// ⚠️ 判据只此一处（`PactKindOfChosenCard` 那一套），⛔ 别在调用点另写一份。</summary>
        static CardDef FindPactCard(BattleContext ctx, string kind)
        {
            if (ctx == null || ctx.CardPool == null || string.IsNullOrEmpty(kind)) return null;
            for (int i = 0; i < ctx.CardPool.Count; i++)
            {
                var c = ctx.CardPool[i];
                if (c != null && PactKindOfChosenCard(c) == kind) return c;
            }
            return null;
        }

        /// <summary>🆕 2026-10-19（战斗线 `T21` 缺口 B）：**附魔卡被触发**那一拍的引擎事件
        /// （原版 `TriggerEnchantment`）。
        ///
        /// <para>**判据（逐句）**：`CardScript__AddEffect.c:776-792`（case 8）——
        /// `Enchantments.GetMarkOfChaos(bm, effectId)` 取到那张附魔卡之后调
        /// `RawCardScript__TriggerEnchantment(enchantmentCard, bm, actingCard, actingCard, …)`；
        /// 后者（`RawCardScript__TriggerEnchantment.c:7-13`）连发两条
        /// `TriggerAbilities(enchantmentCard, 0)` + `TriggerAbilities(enchantmentCard, **500**)`。
        /// ⇒ **触发的是附魔卡自己的 `cardAnimations[]`**（四张 `Dark Pact of *` 的 `Buff_MarkOf*`，
        /// `AnimTimes` 里 `tg = 500 / tn = TriggerEnchantment`），不是载体单位那条。</para>
        ///
        /// <para>**事件怎么发**：`CardId` = **附魔卡名**（`VfxMap` 的逐卡表按 id 或卡名查，
        /// 见 `VfxMap.ResolveCardIdOf`）；`Player/Slot` = **载体单位**那一格（原版动画就演在
        /// `actingCard` 上）；`Keyword` = <see cref="KeywordTable.TriggerEnchantment"/>（表现层
        /// 按它 + `CardAnimTime.TriggerName` 归口 —— 见 `VfxMap.TryResolveTriggerPicks`）。</para>
        ///
        /// <para>⚠️ **找不到那张附魔卡就出声、不发射**（⛔ 不拿载体名顶替 —— 那会去查单位自己的
        /// `trigger` 行，播出来是原版没有的第二下；⛔ 也不静默）。</para>
        /// </summary>
        static void EmitEnchantmentTrigger(BattleContext ctx, UnitState u, string kind)
        {
            if (ctx == null || u == null) return;
            CardDef pactCard = null;
            // ① 原版那条路：附魔卡 = **刚挑中的那一张**（`A1960`：选择面板写进 `ctx.LastChosenCard`）
            var chosen = ctx.LastChosenCard != null ? ctx.LastChosenCard.Card : null;
            if (chosen != null && PactKindOfChosenCard(chosen) == kind) pactCard = chosen;
            // ② 其余各档：种类 → 卡池里同名后缀的那张（判据 = `FindPactCard` 一处，⛔ 别在这儿另写一份）
            if (pactCard == null) pactCard = FindPactCard(ctx, kind);
            if (pactCard == null)
            {
                ctx.Log($"黑暗契约（{kind}）：**卡池里找不到对应的附魔卡** ⇒ `TriggerEnchantment` 那一拍不播"
                      + "（⛔ 不拿载体单位那条动画顶替）");
                return;
            }
            int p, slot;
            if (!FindSlot(ctx, u, out p, out slot)) return;   // 已经不在场上 ⇒ 那一拍没有落点
            ctx.Emit(EvtKind.Trigger, p, slot, pactCard.Name,
                     keyword: KeywordTable.TriggerEnchantment,
                     effect: "TriggerEnchantment(500)",
                     instanceId: u.InstanceId);
            ctx.Log($"{u.Name} 身上的附魔「{pactCard.Name}」触发"
                  + "（原版 `TriggerEnchantment` = 500；动画演在它自己那一格）");
        }


        /// <summary>
        /// 每种契约**会授予哪些关键词** —— **四个印记各给哪些关键词的唯一清单**。
        /// ⚠️ 必须和 <see cref="DarkPactFx"/> 的文本**对得上**（同一件事的两种写法）；属性增减益
        /// （+2 生命…）走 `UnitState.RecordGrant` 那条路，不在这里列。
        /// 🔴 **2026-10-25（K7）：本表今天【没有调用点】，这是故意的** —— 上一版用它做
        /// 「换一份契约就把旧的关键词收回去」，而原版 `AddEffect.c:776-792`（case 8）**从不撤旧印记**
        /// （`addStats` 累加、`lastMarkOfChaos` 只是「最后一份是哪份」）⇒ 那条已按判据删掉。
        /// 保留本表 = 保留「哪份印记给哪个关键词」这一格知识（`DarkPactFx` 只写数字/名字，不太直观）。
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

        /// <summary>这个单位身上**最后一份**契约的种类；没有契约返回 null。
        /// ⚠️ 一个单位可以有**多份**印记（原版不撤旧的）⇒ 本方法只回答「最后一份是哪种」，
        /// 「有几份」读 `KwValue(KeywordTable.DarkPact)`（我们）／trait `330` 的数值（原版）。</summary>
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

        /// <summary>
        /// 🆕 **2026-10-19（`A1960`）**：**刚选中的那张卡** → 契约种类名（`fate` / `blood` / `excess` /
        /// `resilience`）；不是那四张之一 ⇒ `null`。
        ///
        /// <para>**为什么需要它**：原版的「授予黑暗契约」是两步 —— ①玩家从
        /// `choicesDefinition.filterSpecificCards`（四张 `Mark of Khorne/Nurgle/Slaanesh/Tzeentch`）
        /// 里挑一份 ②把挑中的那份授予目标（`AbilityLogic.chosenAbility = buffSelf/buffOthers` +
        /// `selectFromChooseResolution`）。我们的引擎把这两步压成一条 `give … dark pact`，所以
        /// **种类只能从「选中的那张卡」反推**。</para>
        ///
        /// <para>🔴 **判据 = 卡名**：池内那四张的 `name` 逐字是
        /// `Dark Pact of Fate/Blood/Excess/Resilience`（`cards_engine.json` 现读 4 张，
        /// `subtype = "Dark Pact"`）。⚠️ **不按 id 硬编码**（id 是数据侧的事；K06 刚整批换过）。</para>
        /// </summary>
        static string PactKindOfChosenCard(CardDef c)
        {
            if (c == null || string.IsNullOrEmpty(c.Name)) return null;
            bool isPact = string.Equals(c.Subtype, CreatePool.DarkPactSubtype, System.StringComparison.OrdinalIgnoreCase)
                          || c.Name.IndexOf("Dark Pact", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isPact) return null;
            string low = c.Name.ToLowerInvariant();
            if (low.EndsWith("of fate")) return "fate";
            if (low.EndsWith("of blood")) return "blood";
            if (low.EndsWith("of excess")) return "excess";
            if (low.EndsWith("of resilience")) return "resilience";
            return null;
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

        /// <summary>
        /// **`Gain N Energy`**（解析层的 `gainenergy`）—— 🔴 **原版语义 = 永久抬高【最大能量】**，
        /// 不是「把已经花掉的补回来」。
        ///
        /// <para>**判据（第一权威 · 逐句）**：
        /// · `BattleManager__ProcessManaBuff.c:38-62`：`ManaBuff.createMana(+0x16)` 为真 ⇒
        ///   `PlayerManager__AddNewMana`；`refillMana(+0x17)` 为真 ⇒ `PlayerManager__RefillOneMana`。
        /// · `PlayerManager__AddNewMana.c`：`totalMana(+0x38)++` **无条件**；
        ///   `unusedMana(+0x4C)` 只在实参 `filledMana` 为真时 +1。
        /// · 该实参在反汇编里 = `ManaBuff.refillMana`（`0x971e11: movzx edx, byte ptr [rbx+0x17]`
        ///   → `call AddNewMana`）⇒ **只带 `createMana` 的卡：上限 +N、当前能量一点不动**。
        ///   卡表里 DA61 `Plasma Generator` 就是这一档（`manaBuff.createMana = 1`、没有 `refillMana`）；
        ///   全池带 `createMana` 的 49 处、`refillMana` 的 40 处、两者都有的只有 3 处（2026-10-20 现读
        ///   `cards_truth.json`）。</para>
        ///
        /// <para>⛔ **别写成 `Min(MaxEnergy, Energy + n)`** —— 那是「补当前能量」，对应的是
        /// <see cref="DoRefill"/> 那一支（卡面写 `Refill N Energy`，如 `GSC63 Alien Idol`）。</para>
        /// </summary>
        static bool DoEnergy(BattleContext ctx, int owner, string by, EffectOp op,
                             List<string> unresolved)
        {
            var pay = GivePayload.Parse(op.Payload);
            int amount = 0;
            if (pay != null) foreach (var p in pay) amount += p.Value;
            if (amount == 0) amount = op.Amount;
            // 🔴 **2026-10-20（`W58-REVIEWFIX-B2` · 审查 `P3-2`）：`amount == 0` 不是「结算成功」。**
            //   改之前这里照样 `MaxEnergy += 0`、日志打「最大能量 +0」、`return true` ——
            //   「认出了但为 0」看起来和「真的加了」一模一样（本工程红线：不许静默/假成功）。
            //   口径与 <see cref="DoFactionResource"/> 同一条：出声 + 进 `unresolved` + 返回 false。
            if (amount == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要给能量，但数量是 0 —— **这条没生效**");
                unresolved?.Add(op.Source + "（能量数量为 0）");
                return false;
            }
            var ps = ctx.Players[owner];
            // 原版 `totalMana++`（永久）；`unusedMana` 不动。我们这边 `MaxEnergy` 每回合会被公式重算
            // ⇒ 把这份永久增量记进 `ctx.Vars`，`BeginTurn` 再加回来（判据 → `GameplayVariables.manaBonusSeat0`）。
            ps.MaxEnergy += amount;
            ctx.Vars.AddManaBonus(owner, amount);
            ctx.Log($"{by}：「{op.Source}」最大能量 +{amount}（现 {ps.Energy}/{ps.MaxEnergy}；"
                  + "`Gain N Energy` = `ManaBuff.createMana`：**永久抬上限、当前能量不动**）");
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
            // 🔴 **2026-10-19（第五十七会话 `W57` · DA-4）：`your opponent gains …` 那一支的落点换算。**
            //   解析层（`EffectText.TryGain` 的资源分支）把敌方主语记成 `op.Target.Side == "enemy"`
            //   （判据 → `EffectText.IsEnemyGainSubject`）⇒ 这里把 owner 翻到另一边。
            //   **反编译判据**：`ProcessManaBuff(param_2 /*isPlayer*/, …)` 的 `lVar3` 就是**收资源那一方**
            //   的 `PlayerManager`（`param_2 == 0` 取 `+200`、否则 `+0xc0`）—— 同一份代码对两边都跑，
            //   「your opponent gains」只是把 `isPlayer` 换个值 ⇒ **落点由调用方给的那一侧决定**。
            //   ⚠️ `by` / 日志仍按**施放方**写；换边时额外说一句（别让日志看起来像对方自己拿的）。
            int gainOwner = owner;
            if (op.Target != null && op.Target.Side == "enemy") gainOwner = 1 - owner;
            var ps = ctx.Players[gainOwner];
            bool faith = kind == "faith", quest = kind == "quest";
            string what = quest ? "任务点" : (faith ? "信仰" : "灵魂石");
            int n = op.Amount;
            if (n <= 0)
            {
                ctx.Log($"{by}：「{op.Source}」要给{what}，但数量是 0 —— **这条没生效**");
                unresolved.Add(op.Source + "（阵营资源数量为 0）");
                return false;
            }
            if (gainOwner != owner)
                ctx.Log($"{by}：「{op.Source}」写的是**对手**获得{what} ⇒ 加给 {ps.Name}（不是施放方自己）");
            if (quest)
            {
                ps.QuestPoints += n;
                // ⚠️ 这里打的是**加完、还没减阈值**的值（原版 `AddQuestPoints` 内部随后就会减掉一个阈值，
                //    HUD 读的是减完的**余数** —— 见下面 `QuestPointThreshold` 的注释）。
                ctx.Log($"{by}：「{op.Source}」任务点 +{n}（累计 {ps.QuestPoints}）");
                QuestPointThreshold(ctx, gainOwner, by, op);
            }
            else if (faith) { ps.Faith += n; ctx.Log($"{by}：「{op.Source}」信仰 +{n}（现 {ps.Faith}）"); }
            else { ps.SpiritStones += n; ctx.Log($"{by}：「{op.Source}」灵魂石 +{n}（现 {ps.SpiritStones}）"); }

            // `ctx.Signals` = **表现层**那一条（放特效/飘字）—— 三种资源都发。
            ctx.Signals.Add(new BattleEvent
            {
                Kind = quest ? EvtKind.GainQuest : (faith ? EvtKind.GainFaith : EvtKind.GainSpirit),
                Player = gainOwner,
                Slot = -1,                       // 阵营资源是**玩家的**，不属于任何格位
                Amount = n,
                Effect = $"{what} +{n}",
                Turn = ctx.Turn,
            });

            // ---- 事件层广播（**三种资源各不相同** —— 照反编译逐支对）----
            //
            // ⚠️ **上面那句 `ctx.Signals.Add` 不能顶替它** —— 那只喂**表现层**（放特效/飘字），
            //    和事件层是两条完全不同的通道。**别把 Signals 当成广播**（这次就是这么漏的）。
            //
            // 🔴 **2026-10-19（第五十七会话 `W57`）逐条改三处，判据全在 `BattleManager__ProcessManaBuff.c`**：
            //   ① **任务点**（`:51-72`）：`AddQuestPoints` 之后有个 `for i in 0..value` 的循环，
            //      **每个任务点广播一次**；subject = **获得点数的那张源卡**，源卡不属于该方时
            //      **退该方英雄卡**。→ 我们原来一次加值只广播一次、且 subject 传 `null`
            //      （`DA5` 的 `it heals 2` 因此治错人；`DA34`/`DA12` 一次 `Gain 3` 少打/少治两次）。
            //   ② **信仰**（`:90-110`）：加完之后**只广播一次**，subject = **该方英雄卡**
            //      （`BattleManagerSupport__BroadcastCollectFaith(param_1, *(lVar3 + 0x28))`）。
            //      → 原来传 `null`（今天唯一监听者 `SOR38` 不用代词 ⇒ 无可观测差异，但结构上是错的）。
            //   ③ **灵魂石**（`:83-105`）：**这里【不】发任何卡级广播** —— 只有 `CollectSpiritStoneSignal`
            //      （消费端是 `MissionChallenge`，不是卡）。卡级广播的**唯一**来源是
            //      `_ResolveUseWaystone` 里的 `BroadcastCollectedSpiritStone`（全库唯一调用点，
            //      我们对应 `RuleCore.CollectWaystone` 那一行）。
            //      🔴 改前这条广播是**无限递归**的来源：`ASH28 Spiritseer Qelenaris`
            //      「When you collect a Spirit Stone, gain 2 additional Spirit Stones」的 `gain 2`
            //      又走回本函数 ⇒ 再广播 ⇒ 再触发自己（`MaxEffectChain = 8` 截断 ⇒ `1 + 2×8 = 17`，
            //      实测收 1 颗 **5 → 22**）。同一根因让 `ASH80`/`ASH78`/`ASH25` 在**任何**加石时误响。
            if (quest)
            {
                // 🔴 **2026-10-19（第五十七会话 `W57FIX2` · P3/P4）：来源卡的取法逐支照
                //   `BattleManager__ProcessManaBuff.c:51-76`**：
                //     · `param_4`（来源卡）**== 0 ⇒ `break`**（**一条广播都不发**）；
                //     · 来源卡属于**收点数那一方** ⇒ subject = **来源卡**；
                //     · 来源卡**属于另一方** ⇒ subject = **收点数那一方的英雄卡**。
                //   ⛔ 改前这里在「没有单位」时拿 `ctx.PlayingCard` 当来源 —— 那个字段
                //      = **本局最后打出的一张牌**、**从不清空** ⇒ 常驻效果（`DA55`）触发时
                //      会拿旧牌当来源（`DA5` 因此误判；审计 P3）。
                //   现在「这次出牌结算」的来源卡只从 `ctx.CardPlaySource` 取
                //   （`PlayTactic` / `BroadcastPersistentWhen` 显式种、用完还原）——
                //   **没有就是没有，不再退到任何陈旧值**。
                var srcUnit = ctx.ActingUnit;
                // 优先级：**常驻作业覆写 > 正在结算的单位 > 这次出牌结算的卡**（见两条字段的 doc）。
                // 常驻作业那条要压过 `ActingUnit`：那三条路（`ResolveDeploy` / `ResolveAtTurn` 的
                // 常驻与手牌陷阱）的 `ActingUnit` 是「事件宾语」，不是原版的 `AbilityData.thisCard`
                // （判据链见 `BattleContext.FactionSourceCard` 的 doc）。
                bool haveSource = ctx.FactionSourceCard != null
                                  || srcUnit != null || ctx.CardPlaySource != null;
                UnitState subj = null;
                CardDef srcCard = null;
                bool sourceOwnedByGainer =
                    ctx.FactionSourceCard != null ? owner == gainOwner      // 常驻来源卡恒属施放方
                  : srcUnit != null ? OwnerOf(ctx, srcUnit) == gainOwner
                  : owner == gainOwner;                                     // CardPlaySource 恒属施放方

                if (!haveSource)
                {
                    // 原版 `param_4 == 0` ⇒ 那个 while 直接 `break`（**一条都不播**）。
                    // 我们不静默：说一句，好让「监听器不响」有据可查。
                    ctx.Log($"{by}：「{op.Source}」加了 {n} 点任务点，但**这次结算没有来源卡**"
                          + "（原版 `ProcessManaBuff` 的 `param_4 == 0` ⇒ `break`）"
                          + " ⇒ **不广播** `When … obtains Quest Point`");
                }
                else if (sourceOwnedByGainer)
                {
                    if (ctx.FactionSourceCard != null) srcCard = ctx.FactionSourceCard;  // 常驻作业：能力属主
                    else if (srcUnit != null) { subj = srcUnit; srcCard = srcUnit.Card; } // 源卡 = 正在结算的那个单位
                    else srcCard = ctx.CardPlaySource;                                    // 战术卡（没有单位可当代词）
                }
                else
                {
                    // 原版第二条支：源卡不属于该方 ⇒ **该方英雄卡**
                    var wl = ctx.Players[gainOwner].Warlord;
                    if (wl != null) { subj = wl; srcCard = wl.Card; }
                }
                if (srcCard == null)
                {
                    ctx.Log($"{by}：「{op.Source}」加了 {n} 点任务点，但**找不到可用的来源卡 / 督军卡**"
                          + " ⇒ 按原版 `ProcessManaBuff` 那一支**不广播** `When … obtains Quest Point`");
                }
                else
                {
                    // 🔴 **2026-10-26（批次 8 · `B8_AE` · 判据由 `B8_R` 查清）：只跑棋盘跳。**
                    //   原版 `BattleManagerSupport__BroadcastCollectQuestPoints.c` 全文
                    //   `GetCardsInHandRef` = **0**、`GetCardsInHandList` = **0**
                    //   （🥇 `decomp_full/`，整个函数只有「预调 `:22` + 棋盘循环 `:23-42`」两段）
                    //   ⇒ 手牌里的监听者**原版一次都不叫醒**。我们原来走通用四跳
                    //   （事件型降费 / 手牌陷阱 / 手牌单位）⇒ `DA5` / `DA12` / `DA6` 躺在手里
                    //   也会被叫醒并**真的 `ResolveOps`**。⛔ 别改回默认四跳。
                    for (int k = 0; k < n; k++)
                    {   PreCallBoardListener(ctx, subj, WhenEventKind.GainQuest, gainOwner, srcCard, subj, null, null, -1, allowPendingDeath: true);
                        BroadcastWhen(ctx, WhenEventKind.GainQuest, gainOwner, srcCard, subj, preCalled: subj, boardAndPersistentOnly: true); }   // 账 631 同族（2026-10-27 `E6ENG`）：`BroadcastCollectQuestPoints.c:22` 也是预调、`:29-38` 排除主体 ⇒ 与信仰支同形，逐点配对（原版 `ProcessManaBuff.c:53-73` 每 1 点走一遍）
                }
            }
            else if (faith)
            {
                var wl = ctx.Players[gainOwner].Warlord;
                // 🔴 **2026-10-26（批次 8 · `B8_AE`）：只跑棋盘跳** —— 原版
                //   `BattleManagerSupport__BroadcastCollectFaith.c` 全文 `GetCardsInHandRef` = 0
                //   （只有「预调 `:22` + 棋盘循环 `:23-42`」两段；🥇 `decomp_full/`）。
                //   通用四跳的「手牌单位」那一跳会对手牌里的单位卡**真的 `ResolveOps`**
                //   ⇒ `SOR38 Paragon Warsuit`（`When you gain Faith, deal 4 damage to the enemy Warlord`）
                //   躺在手里、你获得 1 点信仰，我们就会对敌督军造成 4 点伤害 —— 原版不会。
                // 🔴 账 631：原版**先预调收集者自己**（`BattleManagerSupport__BroadcastCollectFaith.c:22` `ReactToCollectFaith(unitCollected, unitCollected)`）、
                //   棋盘循环 `:29-38` 再 `op_Inequality(u, unitCollected)` 把它**排除** ⇒ 「预调 + 排除」必须成对（只排不预 = 它整个人漏掉；只预不排 = 双响）。
                PreCallBoardListener(ctx, wl, WhenEventKind.GainFaith, gainOwner, wl != null ? wl.Card : null, wl, null, null, -1, allowPendingDeath: true);
                BroadcastWhen(ctx, WhenEventKind.GainFaith, gainOwner,
                              wl != null ? wl.Card : null, wl,
                              preCalled: wl, boardAndPersistentOnly: true);
            }
            return true;
        }

        /// <summary>
        /// 🔴 **2026-10-19（第五十七会话 `W57` · P2）：`alternativeTraitLogic` 的【第二个消费点】**
        /// —— 原版 `CardScript__FinishActiveAbility.c`。
        ///
        /// <para>**反编译逐行**（`d:/2/tools/decomp_full/CardScript__FinishActiveAbility.c`）：
        /// 主动能力（`ActiveAbility`，调用方 = `BattleManager._ResolvePlayActiveAbility_d__479`）**结束时**：
        /// ① `matchData.gameplayVariables.alternativeTraitLogic (+0x4B) != 0`；
        /// ② 这张卡 `RawCardScript__HasDefaultTrait(card, 0x4a1 = 1185 = DefinedTrait.pray)`；
        /// ⇒ `PlayerManager__AddFaithMana(该卡控制者, +1)` → `BroadcastCollectedFaith(该卡)`。</para>
        ///
        /// <para>**我们接在哪**：`RuleCore.UseAlternative`（Duty / Pray / Ferocity / Agenda ——
        /// 原版这四族与 `oath` 走同一个 `_ResolvePlayActiveAbility` 入口，见那一处 `EvtKind.Ability`
        /// 的长注释）。本版全池带 pray 特性的 23 张卡**全部**只有 `Pray:` 这一种主动能力
        /// （`cards_raw.json` 逐张核过）⇒ 进得来这里的只有 `keyword == Pray`。</para>
        ///
        /// <para>⚠️ **「带 pray 特性」的判据**：本版 `CardDef` 没有 `CardTraits` 列（`cardTraits` 是
        /// 纯落值、不喂引擎），能用的等价物是「**这张卡有 `Pray:` 正文**」（= 它走到了这里）。
        /// 两边差 **1 张**：`SOR13 Simulacrum Bearer` 有 pray 特性但正文里没有 `Pray:` 段
        /// ⇒ 它本来也进不了替代行动这条路 ⇒ **在本入口上两者等价**（逐张对过 23 / 22）。
        /// ⛔ 别为此去改 `CardDef`（不在本批范围）。</para>
        ///
        /// <para>🔴 **2026-10-25 就地订正（铁律 5）：本段原来写「官方 base `CardTraitDescription/pray` 那句
        /// 『下回合开始 +1 信仰』**在原版代码里不存在** / ⛔ 别照文案去补」—— 那是【错的】，整段重写。**
        /// 经典那条**确实存在**，只是**不在 C# 里、在卡数据里**（隐藏附魔 `Pray Enchantment`）：
        ///   ① 全池 **23 张带 `pray` 特性**的卡，在 `cardAbilities[trigger = 655 (AbilityTrigger.Pray)]
        ///      .abilityLogic[0].buffEffect` 里挂 `{buffType:15 singleUseEnchantment,
        ///      enchantment: PPtr → Pray Enchantment}`（`chosenAbility 40 buffSelf`）⇒ **祈祷那一刻挂到自己身上**；
        ///   ② 该附魔卡（`m_Name = Z-NC Sororitas-Pray_enchantment`，`cardType 20`，
        ///      `path_id 1528516720421242597`）两条能力：
        ///      · ability[0] `trigger 60 (UnitDamaged)` → `chosenAbility 340 removeThisEnchantment` = **受伤即撤**
        ///        （⚠️ `CardScript__ResolveDamageDealt.c:25-28` 对 `damage == 0` 直接 `return` ⇒ **盾挡掉的 0 伤不算**）；
        ///      · ability[1] `trigger 31 (TurnStartPlayer)` → `chosenAbility 60 buffMana`
        ///        `{manaAmount:1, friendlyPlayer:1, createMana:1, faithMana:1}` → 再 `340` = **自己回合开始 +1、一次性**；
        ///   ③ 结算：`BattleManager__ResolveManaBuff` → `BattleManager__ProcessManaBuff.c:28,79 AddFaithMana(+1)`。
        /// ⇒ `CardTraitDescription/pray` 与 `prayAlt`（「立刻 +1，再下回合 +1」）**两句都是真的**；
        /// **本方法这一支只是冲突档多出来的「立刻 +1」**，⛔ 不是「下回合 +1」的来源。
        /// 判据全文 → `资料/待办判据_祈祷信仰与任务点_1025b.md` §二·④。</para>
        ///
        /// <para>⚠️ **模式开关 = `alternativeTraitLogic`，⛔ 不是 `PlayModes` / `MatchType`**
        /// （全量反编译 `+0x4B` **4 读 0 写**：本处 ＋ `PlayerManager__AddQuestPoints.c:28` /
        /// `PlayerManager__ResetMana.c:118` / `PlayerManager._SetQuestPointsWithDelay_d__43__MoveNext.c:52`）。
        /// `Classic = false` / `Skirmish = true`（判据 → `GameplayVariables.alternativeTraitLogic` 的 doc；
        /// 两者合起来正是用户 2026-10-25 的「经典 3 / 冲突 4、冲突祈祷立刻 +1」）。
        /// ⛔ **别拿 `isPraying(+0x67)` 当信仰来源** —— 全库只有一个读点
        /// `FilterMethods__MeetsConditionCriteria.c:294-296`，只服务「目标是正在祈祷的单位」这类筛选。</para>
        /// </summary>
        public static void FinishActiveAbility(BattleContext ctx, int owner, UnitState unit, string keyword)
        {
            if (ctx == null || unit == null || unit.Card == null || ctx.IsOver) return;
            var v = ctx.Vars;
            if (v == null || !v.alternativeTraitLogic) return;        // 原版 `+0x4B == false` ⇒ 整段跳过
            if (keyword != KeywordTable.Pray) return;                 // 只有 pray 特性那一族（见上面的判据）
            var ps = ctx.Players[owner];
            ps.Faith += 1;
            ctx.Log($"{unit.Name} 的主动能力结束 —— `alternativeTraitLogic` = true（Alt 档）且它带 pray 特性 ⇒ "
                  + $"信仰 +1（现 {ps.Faith}；原版 `CardScript__FinishActiveAbility.c`）");
            // 原版 `BroadcastCollectedFaith(该卡)` —— subject = **这张祈祷卡本身**。
            // 🔴 **2026-10-26（批次 8 · `B8_AE`）：只跑棋盘跳** —— `BattleManager__BroadcastCollectedFaith.c`
            //   是 `BattleManagerSupport__BroadcastCollectFaith` 的**纯转调**（全文 2 行，🥇 `decomp_full/`），
            //   而后者 `GetCardsInHandRef` = 0 ⇒ 与 `DoFactionResource` 的信仰支同一条广播、同样不该有手牌跳。
            // 账 631：同上 —— 先预调这张祈祷卡自己（`BroadcastCollectFaith.c:22`），棋盘跳再排除它（`:29-38` 的 `op_Inequality`）。
            PreCallBoardListener(ctx, unit, WhenEventKind.GainFaith, owner, unit.Card, unit, null, null, -1, allowPendingDeath: true);
            BroadcastWhen(ctx, WhenEventKind.GainFaith, owner, unit.Card, unit,
                          preCalled: unit, boardAndPersistentOnly: true);
            // 表现层那一条（与 `DoFactionResource` 的信仰支同形 —— 同一个 `EvtKind`、`Slot = -1`）
            ctx.Signals.Add(new BattleEvent
            {
                Kind = EvtKind.GainFaith, Player = owner, Slot = -1, Amount = 1,
                Effect = "祈祷特性 +1 信仰", Turn = ctx.Turn,
            });
        }

        // ==================================================================
        //  任务点 → 隐秘（原版 DA1 / DA2 / DA3 / DA75 的**隐藏能力**）
        //
        //  原版调用链（全量反编译逐跳，判定见 `资料/普查产出_1019/W_引擎线_DA1深挖_1019b.md` §2.3）：
        //    `BattleManager.ProcessManaBuff`（`ManaBuff.questPoints` 为真）
        //      → `PlayerManager.AddQuestPoints`（`PlayerManager__AddQuestPoints.c`）
        //      → 达到阈值 ⇒ **先减掉一个阈值**（`:101-104`）、再 `RawCardScript.OnQuestResolved`（`:118-119`）
        //      → `AbilityTrigger.QuestTriggered = 490` 命中督军卡上的 `createCardInDeck = 270`
        //      → 从 5 张黑暗天使隐秘里随机 1 张 → `BattleManager.AddCardInPlayToDeck`（**随机下标 Insert**）
        //      → `spellType == 0x96(150)` ⇒ `BroadcastSecretCreated`（`_ResolveCreateDeckCard…:113-119`）。
        //
        //  ⚠️ 我们**没有按卡能力建模**（`QuestTriggered` / `createCardInDeck` 两个符号全 `Assets/` 0 命中），
        //     所以这里按**结果层**实现：拿任务点 → 满阈值 → 造 1 张隐秘。当前卡池里**只有 4 张黑暗天使
        //     督军**（DA1/DA2/DA3/DA75，`cards_engine.json` 现读 = 该阵营全部 hero）带这条能力，
        //     其他阵营 **0 张** ⇒ 用「督军阵营 == DarkAngels」当「有没有这条能力」的判据。
        // ==================================================================

        /// <summary>任务点阈值 —— 原版 `ScenarioVariables.questPointsForTrigger`
        /// （`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/DefaultScenario.json` 实读 = **3**；
        /// `EditorScenario` / `TutorialScenario` 同为 3/4，见 `资料/加时与冲突模式_原版规格.md` §2.6）。</summary>
        public const int QuestPointStepDefault = 3;

        /// <summary>任务点阈值 Alt 档 —— 原版 `ScenarioVariables.questPointsForTriggerAlt`（实读 = **4**）。
        ///
        /// 二选一写在 `PlayerManager__AddQuestPoints.c:28-41`：`GameplayVariablesData.alternativeTraitLogic`
        /// （`+0x4B`，**原版是 `bool`**，见 `dump.cs:130190`）为 `false` ⇒ 取 `+0x2C`（= `questPointsForTrigger`），
        /// 否则取 `+0x30`（= `...Alt`）。
        /// 🔴 **Alt 那一档的实例值本地 0 命中**（`alternativeTraitLogic` 由 LiveOps 下发 ——
        /// `资料/加时与冲突模式_原版规格.md` §2.5b 记为「未解」）⇒ 照字段走、**不猜**：
        /// `false` ⇒ 3（经典档）；`true` ⇒ 4。
        /// </summary>
        public const int QuestPointStepAlt = 4;

        /// <summary>本局的任务点阈值（**唯一判据**；HUD 的 `任务点/分母` 也应当读它，
        /// ⛔ 别再在表现层写死 `/3`）。</summary>
        public static int QuestPointStepFor(BattleContext ctx)
        {
            var v = ctx != null ? ctx.Vars : null;
            return v != null && v.alternativeTraitLogic ? QuestPointStepAlt : QuestPointStepDefault;
        }

        /// <summary>
        /// 任务点这条链的**每局附属状态**：一次性提示的记账（随机插入位**已并回引擎随机表**）。
        ///
        /// ✅ **2026-10-27（`E1RNG`）已改**：原来这里另开一条派生 `System.Random` 当插入位随机源，
        /// 理由是「原版走 `MatchData` 自己的随机表、不从洗牌流里拿数」。现在引擎随机源本身就是
        /// **原版那张表**（`ctx.GetRandomInt` = 开局预烧 100 个整数 + 按下标取）⇒ 直接用它，
        /// 与原版 `BattleManager__AddCardInPlayToDeck.c:37` **同一个来源**（「一条表 = 一个来源」）。
        /// ⚠️ 下面 `InsertRng` / `QuestInsertSeed` / `QuestHashMix` 已**停用**（保留只为不动行号，
        /// 下一次可以安全动行号时删掉 —— ⛔ 别再把它们接回随机路径）。
        ///
        /// ⚠️ 仍然成立的一条：`ctx.Rng` **不是**结算随机源（它现在只当烧表用的种子流），
        /// 结算路径⛔ 别再直接 `Rng.Next`。
        /// 判据：`BattleContext.GetRandomInt` 的 doc（`MatchData__GetRandomInt.c` 逐句）。
        /// </summary>
        sealed class QuestAux
        {
            // （已停用）public System.Random InsertRng;  —— 随机源 = `ctx.GetRandomInt`（见类 doc）
            public bool AltNoticeDone;
            public bool NonFactionNoticeDone;
        }

        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BattleContext, QuestAux>
            QuestAuxByCtx = new System.Runtime.CompilerServices.ConditionalWeakTable<BattleContext, QuestAux>();

        static QuestAux QuestAuxOf(BattleContext ctx)
        {
            QuestAux aux;
            if (QuestAuxByCtx.TryGetValue(ctx, out aux)) return aux;
            aux = new QuestAux();               // ⚠️ 2026-10-27（`E1RNG`）：原为 `new QuestAux { InsertRng = new System.Random(QuestInsertSeed(ctx)) }` —— 随机源已并回 `ctx.GetRandomInt`，不再建独立流
            QuestAuxByCtx.Remove(ctx);          // `Add` 对同键会抛 ⇒ 先删（正常路径上本来就没有）
            QuestAuxByCtx.Add(ctx, aux);
            return aux;
        }

        /// <summary>FNV-1a —— **不能用 `string.GetHashCode`**（.NET 里按进程随机化，同一局面两次跑会不同）。⚠️ 2026-10-27（`E1RNG`）**已停用**（原只服务 `QuestAux.InsertRng`，随机源已并回 `ctx.GetRandomInt`）—— 保留只为不动行号。</summary>
        static void QuestHashMix(ref uint h, int v)
        {
            unchecked { h ^= (uint)v; h *= 16777619u; }
        }

        static int QuestInsertSeed(BattleContext ctx)   // ⚠️ 2026-10-27（`E1RNG`）：**已停用**（随机源并回 `ctx.GetRandomInt`）—— 保留只为不动行号，⛔ 别再接线
        {
            unchecked
            {
                uint h = 2166136261u;
                QuestHashMix(ref h, ctx.FirstSeat);
                for (int p = 0; p < ctx.Players.Length; p++)
                {
                    var ps = ctx.Players[p];
                    QuestHashMix(ref h, p);
                    if (ps == null) continue;
                    var wl = ps.Warlord != null ? ps.Warlord.Card : null;
                    string id = wl != null ? wl.Id : null;
                    if (id != null)
                        for (int i = 0; i < id.Length; i++) { h ^= id[i]; h *= 16777619u; }
                    for (int i = 0; i < ps.Deck.Count; i++) QuestHashMix(ref h, ps.Deck[i].Id);
                    for (int i = 0; i < ps.Hand.Count; i++) QuestHashMix(ref h, ps.Hand[i].Id);
                }
                return (int)h;
            }
        }

        /// <summary>
        /// 🔴 **2026-10-19（第五十七会话 `W57`）：「加一张牌进牌库」的唯一判据 —— 随机下标 `Insert`、
        /// ⛔ 不洗整副。**
        ///
        /// <para>**原版**：`BattleManager__AddCardInPlayToDeck.c:36-37`
        /// （`GetRandomInt(0, deck.Count + 1)`，上界开区间 ⇒ 下标 ∈ `[0, Count]`）→ `:63-64` `List.Insert`。
        /// ⚠️ 规则书那句「并洗牌」是**粉丝实体版**的改写，与反编译不一致 ⇒ 按反编译（铁律 2/11）。</para>
        ///
        /// <para>**镜像**：原版下标 0 = **牌库顶**；我们的约定相反（列表末尾 = 顶，见 `DoCreate`
        /// 的 `decktop` 那一小段注释）⇒ `ourIndex = Count - originalIndex`。</para>
        ///
        /// <para>**两个调用点，同一条原版随机表**（`BattleManager.GetRandomInt`）：
        /// ① 任务点满阈值造隐秘（`QuestPointThreshold`）；
        /// ② `DA44 Secret Agenda` 的 `add it to your deck`（原版 SO `chosenAbility = 270
        /// = createCardInDeck`，与 ① **同一个 `AddNewCardToDeck`** —— `PlayAbility.c:2607`）。
        /// ⇒ 用同一条 `ctx.GetRandomInt`（= 原版那张预烧表；⛔ 别一处用预烧表、一处另开流）—— 2026-10-27（`E1RNG`）已把独立的 `QuestAux.InsertRng` 并回这张表。</para>
        /// </summary>
        static void InsertIntoDeckRandom(BattleContext ctx, int owner, CardInstance inst)
        {
            if (ctx == null || inst == null) return;
            var ps = ctx.Players[owner];
            var aux = QuestAuxOf(ctx);   // ⚠️ 2026-10-27（`E1RNG`）：随机源改走共用的原版表（下一行 `ctx.GetRandomInt`，= 原版 `BattleManager.GetRandomInt`）—— 本行已不掷骰，留着只为不动行号
            int origIndex = ps.Deck.Count > 0 ? ctx.GetRandomInt(0, ps.Deck.Count + 1) : 0;
            ps.Deck.Insert(ps.Deck.Count - origIndex, inst);
        }

        /// <summary>
        /// **任务点阈值**（判据只此一处）—— 照原版 `PlayerManager.AddQuestPoints` 的语义：
        ///
        ///   · 加完点后**只看一次**：`QuestPoints >= 阈值` ⇒ **减掉一个阈值**、`QuestMilestone++`、
        ///     造 1 张隐秘。**一次调用最多触发一次**（原版 `:101-104` 只减一次，不循环）。
        ///   · **减阈值是语义的一部分**（不是记账技巧）：原版 HUD 在延迟更新后读的就是**减完的值**
        ///     （`PlayerManager._SetQuestPointsWithDelay…:65,76`）⇒ 玩家看到的是**余数** `0..2 / 3`，
        ///     不是累计值 `3/3`、`4/3`（这正是我们原来与原版不一致的可见差异）。
        ///   · **阈值**读 `ScenarioVariables`（3 / Alt 4，见 <see cref="QuestPointStepFor"/>）；
        ///     Alt 档的实例值本地拿不到 ⇒ 按 `alternativeTraitLogic` 走、并在首次触发时**出声**。
        ///   · **减阈值对任何阵营都发生**（原版在查督军能力**之前**就减了）；
        ///     但「造隐秘」只对**带 `QuestTriggered` 能力**的督军发生 —— 我们按阵营代理（见上面那段）。
        ///   · 造不出来（卡池没有「隐秘」）⇒ **不假装成功**：如实报、不静默。
        /// </summary>
        static void QuestPointThreshold(BattleContext ctx, int owner, string by, EffectOp op)
        {
            var ps = ctx.Players[owner];
            int step = QuestPointStepFor(ctx);
            if (ps.QuestPoints < step) return;                 // 没到阈值：原版只刷新 HUD（我们不播动画）

            var aux = QuestAuxOf(ctx);
            if (!aux.AltNoticeDone)
            {
                aux.AltNoticeDone = true;
                // 🔴 **2026-10-19（第五十七会话 `W57`）：「取不到 Alt 真值」要【明确出声】。**
                //   改前只有 `ctx.Log`（= **战斗日志面板**里的一行）⇒ 开发侧在 Console 里**看不到**，
                //   于是「我们其实拿不到 Alt 档的真值」这条只有翻日志的人知道。
                //   现在补一条**一次性的 `LogWarning`**（同一个对局只打一次，见 `AltNoticeDone`），
                //   然后**继续按经典档 3** —— ⛔ 不猜 4。
                //
                //   **判据现状（如实写进警告里）**：`alternativeTraitLogic` 是
                //   `GameplayVariablesData +0x4B`（原版 **`bool`**，LiveOps 事件载荷），**实例真值在服务器**、
                //   本地 0 命中；我们的 `Classic` / `Skirmish` 两个预设**都是 `false`**
                //   （`GameplayVariables.cs` 的 `alternativeTraitLogic`）⇒ **Alt 档只在事件下发 `true` 时生效**，
                //   冲突模式也走 3（反编译里这个 flag 与 PlayMode **没有任何绑定**）。
                //   `ScenarioVariables.questPointsForTriggerAlt` 的 4 是**字段真值**（DefaultScenario
                //   三份资产实读 3/4），只是没人把它选中。
                bool alt = ctx.Vars != null && ctx.Vars.alternativeTraitLogic;
                string altName = alt ? "Alt" : "";
                string altWhy = alt
                    ? "`alternativeTraitLogic` = true（Alt 档）"
                    : "`alternativeTraitLogic` 的实例值本地 0 命中 ⇒ 按经典档 3";
                ctx.Log($"任务点阈值 = {step}（原版 `ScenarioVariables.questPointsForTrigger{altName}` = {step}；{altWhy}）");
                if (!alt)
                    UnityEngine.Debug.LogWarning(
                        "[Rule] 任务点阈值：`GameplayVariablesData.alternativeTraitLogic` 的真值**不在本地**"
                        + "（它是 LiveOps 事件载荷 `+0x4B`；`Classic`/`Skirmish` 预设都是 false，而原版这个 flag"
                        + "与 PlayMode 没有绑定）⇒ **按经典档 3 继续**（Alt 档 = 4 只在事件下发 true 时生效）。"
                        + "拿到服务器真值之前 ⛔ 别把 4 猜成「冲突模式」的默认值。");
            }

            // 原版先 `ManaManager.SetQuestPoints(满值, 阈值)`（`:97`）再减（`:101-104`）——
            // 那一步是给 HUD 播「涨满」的延迟动画。我们没有那段动画 ⇒ 直接落到余数这一档（结果相同）。
            int full = ps.QuestPoints;
            ps.QuestPoints -= step;
            ps.QuestMilestone++;

            // 施放者阵营 = 自己督军那张卡的 `faction`（和 `DoCreate` 同一条判据）
            string faction = null;
            if (ps.Warlord != null && ps.Warlord.Card != null) faction = ps.Warlord.Card.Faction;

            // 原版 `QuestTriggered` 发在**督军卡的 RawCardScript** 上（`:118-119`），由卡上的
            // `CardAbility(trigger=490)` 命中 `createCardInDeck`。
            // 🔴 **2026-10-26（批次 9 · 执行代理 `E9ER`）**：判据从「督军阵营 == DarkAngels」**升级成原版那一位** ——
            //   `CardDef.AbilityTriggerPresence(490)` = 原版 `RawCardScript.HasAbilityType(490)`
            //   （`HasAbilityType.c:16-38` 遍历 `cardAbilities[].trigger`，只看 trigger、不看 logic 空不空）
            //   的等价物（`abilityTriggers` 列，v17 起每张卡都落）。三档（⛔ 未知不许读成「没有」）：
            //     `1` = 有 ⇒ 造；`0` = 明确没有 ⇒ 不造并出声一次；`-1` = 未知（旧表）⇒ 退回**阵营代理**并出声。
            //   ⇒ 账 `B9` 缺口 ⑤「490 分支缺失」里「别的阵营以后加 490 能力我们表示不出来」这一档**补上了**。
            int has490 = (ps.Warlord != null && ps.Warlord.Card != null)
                       ? ps.Warlord.Card.AbilityTriggerPresence(490) : 0;
            if (has490 != 1)
            {
                bool proxyByFaction = has490 < 0
                    && string.Equals(faction, "DarkAngels", System.StringComparison.Ordinal);
                if (!proxyByFaction)
                {
                    if (!aux.NonFactionNoticeDone)
                    {
                        aux.NonFactionNoticeDone = true;
                        ctx.Log($"{by}：任务点到 {full}、已按原版减阈值（余 {ps.QuestPoints}），"
                              + $"但督军「{ps.Warlord?.Card?.Name ?? "(无)"}」（阵营「{faction ?? "(无)"}」）"
                              + $"**没有** `trigger = 490 (QuestTriggered)` 的能力 ⇒ **不造隐秘**"
                              + "（判据 `CardDef.AbilityTriggerPresence(490)` = 原版 `RawCardScript.HasAbilityType(490)`）");
                    }
                    return;
                }
                ctx.Log($"{by}：`abilityTriggers` 列读不到（旧表）⇒ 任务点奖励**退回阵营代理**判"
                      + "（`DarkAngels`）—— 原版判据是督军卡上的 `trigger = 490` 能力");
            }

            var pick = CreatePool.Resolve(ctx.CardPool, "a secret", faction);
            if (pick == null || pick.Cards == null || pick.Cards.Count == 0)
            {
                // **不假装成功**：造不出来就明说（原版这时同样已经把阈值减掉了）
                ctx.Log($"{by}：任务点满 {step}（第 {ps.QuestMilestone} 次）**本该**往牌库加 1 张隐秘，"
                      + "但卡池里找不出「隐秘」这类牌 —— **这条没生效**"
                      + (pick != null && !string.IsNullOrEmpty(pick.Why) ? $"（{pick.Why}）" : ""));
                return;
            }

            var card = pick.Cards[ctx.GetRandomInt(0, pick.Cards.Count)];
            var inst = ctx.NewInstance(card);                 // 第 7 行第 2 步：加进去的是**新造的一份**

            // 入牌库 = **随机下标 Insert**，**不洗整副** —— 判据与算法只在 `InsertIntoDeckRandom` 一处
            // （原版 `BattleManager__AddCardInPlayToDeck.c:36-37` + `:63-64`）。
            InsertIntoDeckRandom(ctx, owner, inst);

            // 原版 `_ResolveCreateDeckCard…:113-119`：读 `spellType == 0x96(150)` ⇒ `BroadcastSecretCreated`。
            // ⛔ 漏了这一跳 = `DA15`（`When you create a Secret, …`）/ `DA66` 注册的是**永远不响的监听器**（静默）。
            // 归属传 `owner`（造牌的施放者），与 `DoCreate` 的 `hand` 分支同一条口径。
            if (CreatePool.MatchesKind(card, "secret"))
            {
                // 🔴 2026-10-26（批次 8 · `B8_AE`）：原版 `BroadcastSecretCreated.c` 全文
                //   `GetCardsInHandRef` = 0 ⇒ 只跑棋盘跳（与 `DoCreate` / `DA44` 那条同一份判据）。
                BroadcastWhen(ctx, WhenEventKind.CreatesSecret, owner, card, null,
                              boardAndPersistentOnly: true);
                // 🆕 **2026-10-25（执行代理 `X18`）**：成就 `Ach_78` 的第三处写点 ——
                //   任务点满阈值造隐秘入牌库，原版走**同一个** `BroadcastSecretCreated`
                //   （`_ResolveCreateDeckCard…:113-119`）⇒ 与上面两处同一条判据、同一个归属。
                ctx.Tally.AddSecret(owner);
            }

            ctx.Log($"{by}：任务点满 {step}（第 {ps.QuestMilestone} 次）→ 往牌库加入「{card.Name}」"
                  + $"（{full} − {step} = 余 {ps.QuestPoints}/{step}）");
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
        /// 🔴 **「条件换数值」那个条件成不成立**（`EffectOp.AltAmount` 那一族，2026-09-15）。
        ///
        /// **为什么不在 `ResolveOps` 的 `instead` 那一套里做**：那套在**结算前**就把条件判完、
        /// 决定跳哪一条；而这一族的条件说的是**这一下要打的那个目标**
        /// （`it has Hunt Mark` / `they are Destroyer`）—— 那个目标**结算前还不存在**。
        /// 用那套会走到「条件判不了 ⇒ 两条都跑」，伤害**叠加**（3+6=9），比原来更糟。
        /// ⇒ 改在**数值被消费的地方**逐目标判（`DoDeal` / `DoGive` / `DoDeployOnce`）。
        ///
        /// ⚠️ **判不了 → 用基数 + 如实打日志**（不静默、也不猜）。
        /// </summary>
        static bool AltHolds(BattleContext ctx, int owner, EffectOp op, UnitState target, string by)
        {
            if (string.IsNullOrEmpty(op.AltCondition)) return false;
            // 条件原文/规范名挂在**另一对字段**上（`AltCondition*`），这里临时组一条只带条件的 op 去问
            var probe = new EffectOp
            {
                Condition = op.AltCondition,
                ConditionKind = op.AltConditionKind ?? "",
                Source = op.Source,
            };
            bool holds;
            if (!ConditionHolds(ctx, owner, probe, target, out holds))
            {
                ctx.Log($"{by}：「{op.Source}」的条件「{op.AltCondition}」本版**判不了**"
                      + " ⇒ 按基数结算（这一行就是证据，不是静默）");
                return false;
            }
            return holds;
        }

        /// <summary>
        /// **结算层判不了的条件种类** —— `ConditionHolds` 里 `return false` 的那几种
        /// （契约：返回 false = **「判不了」**，不是「不成立」）。
        ///
        /// 🔴 **为什么要单列这张表**（2026-09-16 静默桩普查）：**`ConditionKind` 认得出来 ≠
        ///    结算层判得了**。覆盖率那一层（`EffectText.OpHasMechanism`）原来只查「kind 是不是空」
        ///    ⇒ 这几种**报表放行、卡面不打 `*`**，「这张卡的这半句永远不会发生」这件事
        ///    只有运行时一行日志。首例 `istype`（`Excessive Vigour` 的 `If it's a Daemon`），
        ///    照这张表普查出**家族共 6 个成员**（见 `_tmp_view/stub_audit_0916.md`）。
        ///
        /// ⚠️ **加条件种类时两处一起改**（下面 `ConditionHolds` 的 switch ＋ 这张表）——
        ///    本工程为「两张表要手工对齐」吃过一次亏（见 `EffectDispatch` 的注释）。
        ///    这里由自检兜底：**全池出现表里任何一个种类 ⇒ 自检红**（`RuleEngineTest.TestConditionKindsAreLive`）。
        /// </summary>
        public static readonly System.Collections.Generic.HashSet<string> UnjudgeableConditions =
            new System.Collections.Generic.HashSet<string>
        {
            // ⚠️ **只放结算层真判不了的** —— 自检断言「全池出现的条件种类 ∩ 这张表 = ∅」，
            //    所以实现掉一个就要从这儿**删掉一个**（`energycheck` 2026-09-16 实现掉之后就删了）。
            "istype",        // `If it's a <兵种>` 的兜底：认得出词的已改道 `targethaskw`，
                             //   能落到这儿的只剩「那个兵种词我们不认识」（如 `If it's a Grot`）
            "foreach",       // `for each` 的计数层还没做（当前**全池 0 张**在用）
        };

        /// <summary>这个条件种类结算层**判得了**吗 —— 见 <see cref="UnjudgeableConditions"/>。</summary>
        public static bool CanJudgeCondition(string kind)
        {
            return !string.IsNullOrEmpty(kind) && !UnjudgeableConditions.Contains(kind);
        }

        // ==================================================================
        //  🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16` 消费端）：**结构化条件**
        //  `TargetCriteria.conditionsFilter`（卡池 v15 的 `unitConditions` 列，见 `CardDef.ConditionOf`）
        // ==================================================================
        //
        // **为什么要有这一段**：原版的条件是**数据**（`UnitConditions` 枚举），我们此前整层靠
        //   正文正则猜（`EffectText` 的 `ConditionKind`）。本段把「正文猜出来的条件种类」与
        //   「卡上的结构化条件行」对上，**能对上就用原版语义判**；对不上/判不了**一律退回正文**
        //   （⛔ 不静默改变既有行为）。
        //
        // 🔴 **能对上的只有四类（这是数据决定的，不是我收窄的）** —— 2026-10-22 全池实测：
        //   池内 167 张卡有 `unitConditions` 行，其中**只有 9 张**同时有正文解析出来的条件从句，
        //   而这 9 张里 5 张是 `Codex:`（`energyzero`）配一行**目标筛选**（"打带护甲/不会死的目标"）——
        //   两者**不是同一句话**（那不是 `conditionsFilter` 的语义）⇒ ⛔ 绝对不能拿那一行去判 `energyzero`。
        //   其余 158 张的结构化条件**根本没有对应的正文从句**（它们是**目标筛选**：`random enemy with
        //   Armour` / `non Flying` / `not about to die`），要接的是 `ResolveTargets` 那一层，
        //   **不是** `ConditionHolds`。
        //   ✅ **2026-10-22（同一波的后半）那一层已经接上了**：`EffectOp` 现在**有**
        //   `AbilityIndex` / `LogicIndex`（`EffectText.AssignAbilityIndices` 归位，四道闸门），
        //   生产调用点传 `op.TargetCondition`（`ResolveTargets` 与 `IsLegalPick` 同一格）。
        //   ⚠️ 归不了位的卡**不筛 + 出声**（`NoteTargetConditionGap`）—— 原因码见那里。
        //   ⇒ 本段的映射表**只收「正文那一句 = 原版同一格 `conditionsFilter`」的**四类：
        //     · `targetsurvives` → `220` + `excludeCondition=1`（"没死"）
        //     · `targetdies`     → `220` + `excludeCondition=0`
        //     · `targethasarmour`→ `244` + `0`
        //     · `damaged`        → `40`  + `0`
        //   判据（原版求值器）：`d:/2/tools/decomp_full/FilterMethods__MeetsConditionCriteria.c`
        //     · 220 = `:253-257` → `CardScript__EnoughPendingDamageToDie(card)`（语义 = 「将死」；
        //       ⚠️ 枚举名 `dealtKillDmg` **名字 ≠ 语义**，同一条已在 `RuleCore.cs:11103` 订正过）；
        //     · 244 = `:298-311`（读护甲那组值 `0 <` ⇒ 真）；
        //     · 40  = `:87-98`（`GetCurrentDamage >= 1 && !EnoughPendingHealingToFull`；
        //       我们**没有「待处理治疗」**这一档 ⇒ 等价于 `Health < MaxHealth`，与 `AiTargets.MeetsCondition` 同口径）；
        //     · `exclude` = 末段 `:384-389`：`exclude ? !match : match`。
        //
        // 🔴 **`ConditionUnknown`（旧表）⇒ 保留旧正则 + 出声一次**；`ConditionNone`（列是新的、
        //   这一格确实没有条件）⇒ **没有条件、不再解析正文**。两种都走 `TryStructuredCondition`。
        //
        // ⚠️ **已知缺口（本波不猜、留账）**：
        //   ① `deaths`（`EC58`）/ `anypraying`（`SOR70`）对应的是 `role="count"` 的行 —— 但 v15 的列里
        //      **只落了比较三参数（`opt/op/refValue`）**，**没落被数的那个集合**
        //      （`countCriteria.friendlyUnits/includeHero/includeMinions/targetsAffected`）⇒
        //      「数谁」读不到 ⇒ **本波不接**（接了就是猜）。
        //   ② ~~`EffectOp` 上没有 ability/logic 下标~~ ⇒ 🆕 **2026-10-22 已补**（`EffectOp.AbilityIndex` /
        //      `LogicIndex`）。**条件这一层仍然不用它**：本段要的是「正文那一句 = 原版同一格」，而
        //      ability 下标解决的是**目标筛选**那一层（`ResolveTargets`）。EC58 的两条 `count` 行仍不接
        //      （缺口①：被数集合 v15 没落，v16 落的是 `countSet`，消费端下一波才切）。
        //   ③ `SW22`（`the target is a troop and survives`）被判成 `targethaskw`（混合从句），
        //      结构化行只有 `220/ex=1` 那一半 ⇒ 用结构化会**丢掉 "is a troop"** ⇒ 本波不动。

        /// <summary>正文条件种类 → (原版 `UnitConditions` 数值, `excludeCondition`)。**只收四类**，
        /// 理由见上面那段（⛔ 别往里加「看起来像」的：`energyzero` 那 5 张配的是**目标筛选**，不是同一句话）。
        ///
        /// <para>🔴 **2026-10-23（引擎线 `E1` · 回归 `R1`）：前两类是【结算后判定】，与后两类（目标筛选）
        /// 的「没有行 ⇒ 没有条件」那条推理不通用** —— 见 <see cref="IsPostCheckConditionKind"/>：
        /// 它们只在**卡上真有这一行**时才用结构化数据，没有行一律退回正文口径。</para></summary>
        static readonly Dictionary<string, (int Cond, int Exclude)> ConditionKindToUnitCondition =
            new Dictionary<string, (int Cond, int Exclude)>(System.StringComparer.Ordinal)
        {
            { "targetsurvives",  (220, 1) },
            { "targetdies",      (220, 0) },
            { "targethasarmour", (244, 0) },
            { "damaged",         (40,  0) },
        };

        /// <summary>
        /// 🔴 **2026-10-23（引擎线 `E1` · 回归 `R1`）：这一类条件判的是「一条效果**结算完之后**，
        /// 刚才那个目标死没死」**（卡面 `If the target dies, …` / `If the target survives, …`），
        /// ⛔ **不是**「选目标时的筛」（`conditionsFilter` 那一层由 `ResolveTargets` 吃）。
        ///
        /// <para>**为什么单列**：`TryStructuredCondition` 的 `n == 0` 分支把「卡上没有这一行」
        /// 读成「原版数据里没有这一格条件 ⇒ 没有条件 ⇒ `holds = true`」—— 那个推理只对**筛选**成立
        /// （筛子缺失 = 不筛）。对**结算后判定**它会把「数据没落这一格」误读成「条件恒成立」：
        /// `AM39` / `UM34` / `GSC40` / `3f38d2e2` / `30007162` 五张 `If the target dies` 在
        /// `cards_engine.json` 里**一条 `unitConditions` 行都没有** ⇒ 目标活着也兑现支线
        /// （`plays` 基线 10-21 的回归，判据 → `资料/普查产出_1023c/play基线归因签字.md` §二 R1）。</para>
        ///
        /// <para>⚠️ 同文件 `ConditionHolds` 的 `targetdies` 分支（`:10293-10299`）本来就是对的正文口径
        /// （`LastTarget == null || !LastTarget.IsAlive`）—— 本方法只负责让它**轮得到**。
        /// ⚠️ 卡上**真有**这一行时仍然走结构化（原版数据优先）—— 见 `n > 0` 那两支。</para>
        /// </summary>
        static bool IsPostCheckConditionKind(string kind)
        {
            return kind == "targetdies" || kind == "targetsurvives";
        }

        /// <summary>结构化条件行里，哪些 `role` 的**主语**是「一个单位」、本段能判
        /// （`target` = 选定的/上一条的目标，`acting` = 施放者自己）。
        /// `count` / `multi` / `mult` / 卡级计分那几档**不是这个形状** ⇒ 不接（见本节「已知缺口」）。</summary>
        static bool RoleTakesUnitSubject(string role)
        {
            return role == "target" || role == "acting";
        }

        /// <summary>🔴🆕 2026-10-23（引擎线 `新账-95` #2）：**能当「目标池的筛」用的 role** ——
        /// 在 <see cref="RoleTakesUnitSubject"/> 之上加 **`cardtarget`**
        /// （原版卡级 `cardTargetCriteria`，判据 `RawCardScript__IsValidTarget.c:44`）。
        ///
        /// <para>⚠️ **只给目标池那一层用**（<see cref="ApplyStructuredConditionFilter"/>）——
        /// `TryStructuredCondition`（正文条件那一层）**仍然只认** `target` / `acting`：
        /// 卡级行说的是「这张卡能点谁」，不是「某条效果的结算条件」
        /// （两处混用会把 `BL50` 那类卡的目标筛条件错当成后半句「如果它死了」的判定）。</para>
        /// </summary>
        static bool RoleTakesTargetSubject(string role)
        {
            return RoleTakesUnitSubject(role) || role == "cardtarget";
        }

        static readonly HashSet<string> _structuredConditionNotices =
            new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>结构化条件这条线上**每一次**「本波不接 / 退回正文」的原因 —— 同一 (卡, 种类, 原因)
        /// **只喊一次**（否则每张卡每回合刷屏；本工程红线是「不许静默」，不是「必须刷屏」）。</summary>
        static void StructuredConditionNotice(string key, string msg)
        {
            if (!_structuredConditionNotices.Add(key)) return;
            UnityEngine.Debug.LogWarning("[RuleEngine] 结构化条件（W64·B-16）：" + msg);
        }

        /// <summary>
        /// **原版 `FilterMethods.MeetsConditionCriteria` 的等价求值**（单个单位）。
        /// 返回 `null` = **本版判不了这个枚举值**（⛔ 不是「不成立」）。
        ///
        /// <para>🔴 **2026-10-23（`B-16` 整件）：本方法现在只是转发到 `ConditionEval.MeetsUnit`**
        /// —— 逐值判据（`UnitConditions` 全部成员、每格都标了 `MeetsConditionCriteria.c:行号`）
        /// **全在 `Core/ConditionEval.cs`**，⛔ 别在这里再写第二份（两处迟早不一致）。
        /// 「判得了 / 模型缺数据 / 原版自己没实现」三档 → `ConditionEval.KindOf` / `GapOf`。</para>
        ///
        /// <para>`exclude` 的取反照旧在出口做一次（原版 `MeetsConditionCriteria.c:384-389`，
        /// 实现见 `ConditionEval.Meets`）—— ⛔ 调用方**不要**再取反一次。</para>
        /// </summary>
        public static bool? MeetsUnitCondition(BattleContext ctx, UnitState u, int cond, bool exclude)
        {
            return ConditionEval.MeetsUnit(ctx, u, cond, exclude);
        }

        /// <summary>结构化条件行 → 被判的那个单位（`null` = 这一行没有可判的主语 ⇒ 退回正文）。</summary>
        static UnitState StructuredConditionSubject(BattleContext ctx, CardDef.ConditionSpec row, UnitState chosen)
        {
            if (row == null) return null;
            switch (row.Role)
            {
                case "acting": return ctx != null ? ctx.ActingUnit : null;
                case "target": return chosen ?? (ctx != null ? ctx.LastTarget : null);
                default: return null;
            }
        }

        /// <summary>
        /// 试着用**结构化条件**判这条 op 的条件。返回 `true` = 已判（`holds` 有效）；
        /// `false` = 本波不接 / 缺数据 / 判不了 ⇒ **调用方退回正文正则**（旧行为）。
        ///
        /// <para>判定次序：</para>
        /// <list type="number">
        /// <item>种类不在 <see cref="ConditionKindToUnitCondition"/> ⇒ 不归这一层（正文）。</item>
        /// <item>`card.ConditionsVersioned == false`（旧表）⇒ 出声一次 + 正文。</item>
        /// <item>候选 = 同 `cond` + 同 `exclude` 且 `role` 能吃单位主语的行：
        ///   · **恰好一条** ⇒ 按原版语义判；
        ///   · **多于一条** ⇒ 归不了位（没有 ability 下标）⇒ 出声 + 正文；
        ///   · **零条**：若同 `cond` 的**反侧**存在 ⇒ 数据与正文互相矛盾 ⇒ 出声 + 正文；
        ///     否则 ⇒ **原版数据里没有这一格条件 = 没有条件** ⇒ `holds = true`（⛔ 不再解析正文）。</item>
        /// </list>
        /// </summary>
        static bool TryStructuredCondition(BattleContext ctx, int owner, EffectOp op, UnitState chosen,
                                           CardDef card, out bool holds)
        {
            holds = false;
            if (card == null || op == null) return false;
            string kind = op.ConditionKind ?? "";
            (int Cond, int Exclude) want;
            if (!ConditionKindToUnitCondition.TryGetValue(kind, out want)) return false;   // 不归这一层

            if (!card.ConditionsVersioned)
            {
                // 旧表（< v15）：整列没有 ⇒ 未知。保留旧正则（这是**过渡守卫**，生产是 v15）。
                StructuredConditionNotice("oldtable|" + card.Id + "|" + kind,
                    $"卡「{card.Name}」（{card.Id}）的卡表**没有 `unitConditions` 列**（version < 15）⇒ "
                  + $"条件「{op.Condition}」退回**旧正文口径**（等价于「未知」，⛔ 不是「没有条件」）。"
                  + "重跑 工具/gen_cards_engine.py 生成 v15 卡表。");
                return false;
            }

            CardDef.ConditionSpec hit = null;
            int n = 0;
            bool oppositeSide = false;
            bool otherRole = false;
            foreach (var row in card.Conditions)
            {
                if (row == null || row.Condition != want.Cond) continue;
                if (row.Exclude != (want.Exclude != 0)) { oppositeSide = true; continue; }
                if (!RoleTakesUnitSubject(row.Role)) { otherRole = true; continue; }
                hit = row; n++;
            }

            if (n > 1)
            {
                StructuredConditionNotice("ambig|" + card.Id + "|" + kind,
                    $"卡「{card.Name}」（{card.Id}）有 **{n} 条** `cond={want.Cond}`/ex={want.Exclude} 的条件行"
                  + $"（正文「{op.Condition}」）—— 没有 ability 下标、归不了位 ⇒ 退回**旧正文口径**。"
                  + "（要接这一档，得先把 `unitConditions` 行与 ability 对上号。）");
                return false;
            }
            if (n == 0)
            {
                if (oppositeSide)
                {
                    StructuredConditionNotice("opposite|" + card.Id + "|" + kind,
                        $"卡「{card.Name}」（{card.Id}）的数据里只有 `cond={want.Cond}` 的**反侧**条件"
                      + $"（正文「{op.Condition}」判的是 ex={want.Exclude}）—— 正文与数据矛盾 ⇒ 退回**旧正文口径**。");
                    return false;
                }
                if (otherRole)
                {
                    // 同 `cond` 的行挂在 `count` / `multi` / `mult` / 卡级计分上 —— 那些形状**不是**
                    // 「一个单位满不满足」，本波不接（`count` 的被数集合、`multi` 的配套参数都还没落列）。
                    StructuredConditionNotice("otherrole|" + card.Id + "|" + kind,
                        $"卡「{card.Name}」（{card.Id}）的 `cond={want.Cond}` 条件挂在 "
                      + "`count` / `multi` / `mult` / 卡级计分 上（不是「一个单位」的形状）—— 本波**不接**"
                      + $"（正文「{op.Condition}」照旧走正文；⛔ 不拿别的位置硬凑）。");
                    return false;
                }
                // 🔴 **2026-10-23（引擎线 `E1` · 回归 `R1`）：结算后判定（`targetdies` / `targetsurvives`）
                //   没有这一行时**不能**读成「没有条件」** —— 那会让「目标活着」也成立（5 张卡的回归）。
                //   原版那半句挂在 abilityLogic 上、v15 列没落 ⇒ 本仓判据 = 正文（`ConditionHolds` 那一支）。
                //   ⛔ 别改成 `holds = false`（= 恒不成立，同样错）；要的是**退回正文**（返回 false）。
                if (IsPostCheckConditionKind(kind))
                {
                    StructuredConditionNotice("postcheck-norow|" + card.Id + "|" + kind,
                        $"卡「{card.Name}」（{card.Id}）**没有** `cond={want.Cond}`/ex={want.Exclude} 的条件行"
                      + $"（正文「{op.Condition}」）—— 这是**结算后判定**，与目标筛选不同："
                      + "「没有行」不能读成「没有条件」 ⇒ 退回**正文口径**（判上一条效果目标的死活）。");
                    return false;
                }
                // 原版数据里没有这一格条件 ⇒ **没有条件**。按铁律「原版是数据」，不再拿正文猜。
                // ⚠️ 这一步成立的前提是「这张卡**在真值快照里**」—— 2026-10-22 现读：
                //   池内 **1127/1127** 张都在 `cards_raw.json`（1,351 张 SO）里 ⇒「没有行」只能读成
                //   「原版这张卡真的没有条件行」，而不是「快照没这张卡」。将来若出现池外新卡，
                //   **先拿到 per-card 的「在不在快照」信号**再谈这条，否则这里会把它误判成无条件。
                StructuredConditionNotice("none|" + card.Id + "|" + kind,
                    $"卡「{card.Name}」（{card.Id}）**没有** `cond={want.Cond}`/ex={want.Exclude} 的条件行 ⇒ "
                  + $"按结构化数据判为「**没有条件**」（正文「{op.Condition}」不再解析；v15 卡表）。");
                holds = true;
                return true;
            }

            var t = StructuredConditionSubject(ctx, hit, chosen);
            if (t == null)
            {
                StructuredConditionNotice("nosubject|" + card.Id + "|" + kind,
                    $"卡「{card.Name}」（{card.Id}）的条件行（role={hit.Role}）**没有可判的主语**"
                  + $"（正文「{op.Condition}」）⇒ 退回旧正文口径。");
                return false;
            }
            bool? v = MeetsUnitCondition(ctx, t, hit.Condition, hit.Exclude);
            if (v == null)
            {
                StructuredConditionNotice("unjudgeable|" + card.Id + "|" + hit.Condition,
                    $"卡「{card.Name}」（{card.Id}）的结构化条件 `cond={hit.Condition}` **本波判不了** ⇒ 退回旧正文口径。");
                return false;
            }
            holds = v.Value;
            return true;
        }

        /// <summary>
        /// 🆕 2026-10-22（`W64` · `B-16`）：**判一条 op 的条件**的统一入口（结算层走的就是它）。
        /// 返回 `false` = **本版判不了**（`holds` 无效）；`true` ⇒ `holds` 有效。
        /// <paramref name="structured"/> = 这一次用的是**原版结构化数据**（`true`）
        /// 还是**旧正文正则**（`false`）—— 自检靠它分辨「到底走没走到新路」，生产不读。
        ///
        /// <para>⚠️ 结构化那一档**只吃「正文那一句 = 原版同一格 `conditionsFilter`」的四类**
        /// （见 <see cref="ConditionKindToUnitCondition"/> 上面那段）；其余一律正文。</para>
        ///
        /// <para>🔴 **调用方注意**：`role = acting` 的条件行读 <see cref="BattleContext.ActingUnit"/> ——
        /// 结算层是由 `ResolveOne` 临时设好再调的（它包了 save/restore）；
        /// **绕过 `ResolveOne` 直接调本 API 的地方**（自检 / 探针）要自己把它摆上，否则那一档会
        /// 因为「没有主语」而退回正文。</para>
        /// </summary>
        public static bool TryEvaluateCondition(BattleContext ctx, int owner, EffectOp op, UnitState chosen,
                                                CardDef card, out bool holds, out bool structured)
        {
            holds = false;
            structured = TryStructuredCondition(ctx, owner, op, chosen, card, out holds);
            if (structured) return true;
            // 🆕 2026-10-23（引擎线 `新账-97` ④）：`role == "count"` 那一层（`deaths` / `anypraying` /
            //   `noeffect` 等正文种类）—— 有 `countSet` 就按原版数据数，判不了再退回正文。
            //   判据 → `Core/CountEval.cs` 文件头；⛔ 别把「数几个」写进下面那段正文正则。
            if (TryCountCondition(ctx, owner, op, card, out holds)) { structured = true; return true; }
            // 正文那一档：**故意不再传 card**（结构化已经试过/不适用，别再来一遍）。
            return ConditionHolds(ctx, owner, op, chosen, out holds, null);
        }

        /// <summary>
        /// 条件成不成立。**返回 false = 「本版判不了」**（不是「不成立」）—— 调用方必须区别对待。
        /// 出处：**判据查不到**（🔴 `D26`，2026-10-19）—— `ConditionKind` 这一族是**从卡面正文
        /// 解析出来的**（原版没有文本解析层，理由与搜过的路径/词 → `EffectText.cs` 文件头那段）；
        /// 原版的条件是**结构化 ability 数据**里的字段，逐卡没解出 ⇒
        /// 下面那些判据目前只有 **gd 旁证**（原写「出处：`rule_core.gd:2635` / `:2652` / `:2737`」——
        /// 那份 `.gd` 是**我们自己的上一版复刻**，**旁证、非权威**）。
        ///
        /// <para>🆕 2026-10-22（`W64` · `B-16`）：入口先问**结构化条件**（<see cref="TryStructuredCondition"/>）——
        /// 它是原版数据；只有「不归这一层 / 旧表 / 归不了位 / 判不了」才落到下面这段正文正则。</para>
        /// </summary>
        /// <param name="card">这条 op **所属的卡**（`null` = 不知道 ⇒ 只能走正文）。
        /// ⚠️ 别拿 `ctx.PlayingCard` 顶替 —— 那个字段在事件触发路径里是**陈旧值**（见 `BattleContext.PlayingCard` 的 doc）。</param>
        /// <summary>
        /// 🆕 2026-10-23（引擎线 `新账-97` ④）：**`role == "count"` 的求值入口**。
        /// 先用这条 op 自己的 `AbilityIndex/LogicIndex` 找那一行；找不到才按正文种类
        /// （`deaths` / `anypraying` / `noeffect`）在卡上唯一地认一行。
        /// 返回 `false` = 本版判不了（没有 `count` 行 / 没有 `countSet` / 集合或比较档未实现）
        /// ⇒ 调用方退回正文口径（**不许**拿 false 当「不成立」）。
        /// </summary>
        static bool TryCountCondition(BattleContext ctx, int owner, EffectOp op, CardDef card, out bool holds)
        {
            holds = false;
            if (ctx == null || op == null || card == null) return false;
            CardDef.ConditionSpec spec = null;
            bool byIndex = op.AbilityIndex >= 0
                        && card.TryGetCondition(op.AbilityIndex, op.LogicIndex, "count", out spec)
                        && spec != null && spec.CountSet != null;
            if (!byIndex) spec = FindCountRow(card, op.ConditionKind);
            if (spec == null || spec.CountSet == null)
            {
                // 「这张卡有 count 行、但归不到这条 op」也要出声一次（否则看起来像「本版没接」）。
                if (FindCountRowCount(card, op.ConditionKind) > 1)
                    StructuredConditionNotice("count-ambig|" + card.Id + "|" + op.ConditionKind,
                        $"卡「{card.Name}」（{card.Id}）的 `count` 行**多于一条**且没有可用的 ability 下标"
                      + $"（正文「{op.Condition}」）⇒ 退回**正文口径**（⛔ 不猜是哪一条）。");
                return false;
            }

            string gap;
            var r = CountEval.Meets(ctx, owner, spec, ctx.ActingUnit, out gap);
            if (r == null)
            {
                StructuredConditionNotice("count-gap|" + card.Id + "|" + (gap ?? "?"),
                    $"卡「{card.Name}」（{card.Id}）的 `count` 条件（cond={spec.Condition}，正文「{op.Condition}」）"
                  + $"**本版判不了**：{gap} ⇒ 退回**正文口径**。");
                return false;
            }
            holds = r.Value;
            return true;
        }

        /// <summary>
        /// 🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 C）：整条 ability 触发 N 遍** ——
        /// 原版 `CardAbility.multiCountCriteria`（`CardAbility.cs` 字段序第 9 格，实例偏移 `+0x40`）。
        ///
        /// <para>**判据** = `RawCardScript__TriggerAbilities.c:226-259`：
        /// `multiCountCriteria == null` ⇒ 触发一次；`targetsAffected == 0 (any)` ⇒ 触发一次；
        /// 否则 `GetTargets(manager, multiCountCriteria, thisCard, thisCard, targetCard,
        /// checkMaxTargets:true)` 求候选表，**表长 = N**，`for (i &lt; N) TriggerAbility(...)`；
        /// **表空 ⇒ 一次都不触发**。</para>
        ///
        /// <para>**返回值**：`true` = 结构化数据可判（`n` 有效，含 `n = 0`）；
        /// `false` + `gap == null` = 没有这一行 / 不适用（最常见，⛔ 不是缺陷）；
        /// `false` + `gap != null` = 有行但判不了（调用方退回正文口径并出声）。</para>
        ///
        /// <para>🔴 **`ta == 0 (any)` 按「不适用」返回**（⛔ 不返回 1）：原版 `any` 让其余 35 格全部失效
        /// （`Any disregards all other filters`）⇒ 这条 criteria 不携带任何「数谁」信息；而正文的
        /// `for each X` **可能来自另一个机制** —— 一号实例 `GOF90 Wurrboy`：外层 ability 的
        /// `multiCountCriteria` 与 `multiplier` 都是 `any(0)`，真正的「按友方部队数」写在**它挂的那张
        /// 附魔**（`648c49… Wurrboy`）的 `multiplier.ta = 10` 上。按数据返回 1 会把正文 `for each`
        /// **压成 1 遍**（比缺数据更糟）。</para>
        ///
        /// <para>⚠️ **归位门**：只按 `ops` 上真的归位过的 `AbilityIndex` 取行（`multi` 行永远挂在
        /// `lg = -1` 的 ability 级）。`AbilityIndex &lt; 0`（句柄没归位）⇒ 返回 `false` + `gap == null`
        /// —— ⛔ 不按卡猜是哪一条 ability（`AM18` 的回合末正文在 `ResolveAtTurn` 里是**单独解析**的，
        /// 下标恒 `-1`，它的 N 由正文 `for each` 那条路给，见 `TryComputeRepeats`）。</para>
        /// </summary>
        static bool TryStructuredMultiTrigger(BattleContext ctx, int owner, IReadOnlyList<EffectOp> ops,
                                              CardDef card, UnitState actor, out int n, out string gap)
        {
            n = 0; gap = null;
            if (ctx == null || card == null || ops == null || ops.Count == 0) return false;
            int ab = -1;
            for (int i = 0; i < ops.Count; i++)
                if (ops[i] != null && ops[i].AbilityIndex >= 0) { ab = ops[i].AbilityIndex; break; }
            if (ab < 0) return false;
            CardDef.ConditionSpec spec;
            if (!card.TryGetCondition(ab, -1, "multi", out spec) || spec == null) return false;
            if (spec.MultiSet == null)
            {
                gap = "`multi` 行**没有** `multiSet`（旧卡表 / 生成器没落）";
                return false;
            }
            int ta = spec.MultiSet.TargetsAffected;
            if (ta == CardDef.CountSetSpec.Unknown)
            {
                gap = "`multiSet.targetsAffected` **未知（没落）**";
                return false;
            }
            if (ta == 0) return false;              // `any` ⇒ 不携带信息（见 doc）
            var r = CountEval.CountTargets(ctx, owner, spec.MultiSet, 0, actor, out gap);
            if (r == null) return false;
            n = r.Value;
            return true;
        }

        /// <summary>
        /// 🔴 **2026-10-24（执行代理 `E1` · `C3` §1·1）：这条 op 是不是「`for each` **增量**型」** ——
        /// 判据**只此一份**（出牌路与触发路**都调它**；⛔ 别在两处各写一遍条件式）。
        ///
        /// <para>形状 = `Give +2 Melee Attack …, and an additional +2 … for each Dark Pact on it`
        /// —— 一份契约时是 **+4**（数值放大），**不是** +2 再来两遍。</para>
        /// <para>`deal` / `heal` **不算**这一档：它们的 `for each` 是**重复**（打 N 遍、每遍重挑目标），
        /// 走 <see cref="TryComputeRepeats"/>。判据 = `AbilityLogic__PlayAbility.c:1461-1508`（伤害是
        /// **一个** `DamageInfo` 一次 `DealMultiDamage`，倍率进数值）；只有
        /// `case 12 doDamageMultiRandom`（`:1511-1530`）才是循环 N 次。</para>
        /// </summary>
        static bool IsPerCountIncrement(EffectOp op)
        {
            return op != null && op.PerCount > 0 && !string.IsNullOrEmpty(op.CountRef)
                && op.Verb != "deal" && op.Verb != "heal";
        }

        /// <summary>
        /// 🔴 **2026-10-24（执行代理 `E1` · `C3` §1·1）：`for each` **增量**型的**唯一实现** ——
        /// 数值 = 基础 `Amount` ＋ 计数 × `PerCount`（载荷动词要把增量写回载荷原文）。
        ///
        /// <para>**为什么必须收成一处**：原版这一格是 <b>一次结算、数值乘进去</b>，不是「跑 N 遍」
        /// （判据：`AbilityLogic.multiplier` / `furtherMultiplier` → `PlayAbility.c:181-236` 的 `local_178`；
        /// 伤害族 `:1461-1508` 一个 `DamageInfo` 一次 `DealMultiDamage`；增益族
        /// `NewBuffStat.c:61-67/207-232` ⇒ `计数 × (change × multiplier)`，数据实证 `BL43`）。
        /// 改前这一整套**只写在出牌路**里，触发路把 `PerCount` **整条丢掉**（`ResolveOne` 不读它）
        /// ⇒ 触发正文里的 `give/gain/lose` 只剩基础值。</para>
        ///
        /// <para>返回 `false` = 这条 op **不该结算**（计数数不出来；日志与 <paramref name="unresolved"/>
        /// 已在本方法里写过 —— 与改前出牌路一字不差）。返回 `true` 时
        /// <paramref name="folded"/> = 要递下去的那条：**没有增量**时是**同一个对象**（调用方可以
        /// `ReferenceEquals` 判「换没换」），有增量时是**副本**（⛔ 绝不改解析产物本身）。</para>
        ///
        /// <para>`actor` = 结构化乘数里 `includeSelf` / `excludeNotSelf` 的「这条 ability 的所有者」
        /// （与 <see cref="TryComputeRepeats"/> 同一条口径）：出牌路传 `chosen`（改前原样）、
        /// 触发路传**触发的那张单位**。</para>
        /// </summary>
        static bool TryFoldPerCount(BattleContext ctx, int owner, UnitState source, string by,
                                    EffectOp op, UnitState actor, CardDef sourceCard,
                                    List<string> unresolved, out EffectOp folded)
        {
            folded = op;
            if (!IsPerCountIncrement(op)) return true;

            int n = CountFor(ctx, owner, source, op);
            if (n < 0)
            {
                ctx.Log($"{by}：「{op.Source}」的计数「{op.CountRef}」本版数不出来 —— **这条没生效**");
                unresolved.Add(op.Source + "（计数 " + op.CountRef + " 数不出来）");
                return false;
            }
            // 🆕 v22（2026-10-24 · `新账-95` #2）：**结构化乘数（`role == "mult"`）优先**。
            //   原版这一格 = `AbilityLogic.multiplier`（`PlayAbility.c:181-236`）——
            //   数据里就是「按几个目标放大」，比正文正则权威（铁律 10 第 1 条）。
            //   两者不一致时**按数据**并出声（⛔ 不静默取正文）。
            {
                int sm; string smGap;
                if (TryStructuredMultiplier(ctx, owner, op, sourceCard, actor, out sm, out smGap))
                {
                    if (sm != n)
                        ctx.Log($"{by}：「{op.Source}」正文计数「{op.CountRef}」= {n}，"
                              + $"而原版 `multiplier` 结构化数据 = **{sm}** ⇒ 按数据 {sm} 结算"
                              + "（`AbilityLogic__PlayAbility.c:181-236`）");
                    n = sm;
                }
                else if (smGap != null)
                {
                    StructuredConditionNotice("mult-gap|" + (sourceCard != null ? sourceCard.Id : "?") + "|" + smGap,
                        $"卡「{sourceCard.Name}」（{sourceCard.Id}）的 `mult` 条件（正文「{op.Source}」）"
                      + $"**本版判不了**：{smGap} ⇒ 退回**正文计数**「{op.CountRef}」。");
                }
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
            folded = effOp;
            return true;
        }

        /// <summary>
        /// 🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 C）：一条 op 要结算几遍** ——
        /// **出牌路与触发路共用这一份**（⛔ 别在两处各写一遍：一处漏改就是静默少数/多数）。
        ///
        /// <para>**优先序（照改前出牌路原样搬过来，⛔ 别调换）**：
        /// ① 正文 `CountRef`/`CountScope` → <see cref="CountFor"/>；
        /// ② 结构化 `role == "mult"`（`AbilityLogic.multiplier`）**优先于**正文 —— 数据可判时以数据为准并出声；
        /// ③ `EffectOp.RepeatTimes`（`…, N times`）**乘上去**（每遍重新挑目标、重掷 Rng）；
        /// ④ `AbilityFurtherMultiplier` —— **只在正文没给次数时**才乘（卡面那个数就是它，再乘是平方）。</para>
        ///
        /// <para>返回 `false` = 这条 op 的**计数数不出来**（`CountFor` 返回 `-1`）⇒ 调用方整条跳过
        /// （本方法已经把日志与 `unresolved` 写了，与出牌路改前一字不差）。</para>
        ///
        /// <para>`actor` = 结构化乘数里 `includeSelf` / `excludeNotSelf` 的「**这条 ability 的所有者**」
        /// （`CountEval.CountTargets` 的形参）：出牌路传 `chosen`（改前原样）、触发路传**触发的那张单位**
        /// （`source`）—— `GOF4` 的 `includeSelf = 0` 靠它把自己排除掉。</para>
        /// </summary>
        static bool TryComputeRepeats(BattleContext ctx, int owner, UnitState source, EffectOp op,
                                      CardDef sourceCard, UnitState actor, string by,
                                      List<string> unresolved, out int repeats)
        {
            repeats = 1;
            if (op == null) return true;
            bool textCounted = false;
            if (!string.IsNullOrEmpty(op.CountRef) && op.CountScope != null)
            {
                int n = CountFor(ctx, owner, source, op);
                if (n < 0)
                {
                    ctx.Log($"{by}：「{op.Source}」的计数「{op.CountRef}」本版数不出来 —— **这条没生效**");
                    unresolved.Add(op.Source + "（计数 " + op.CountRef + " 数不出来）");
                    return false;
                }
                repeats = n;
                textCounted = true;
                ctx.Log($"{by}：「{op.Source}」按 {op.CountRef} 计 {n} 遍");
            }
            // 🆕 v22（2026-10-24 · `新账-95` #2）：**结构化乘数（`role == "mult"`）优先** ——
            //   原版 `AbilityLogic.multiplier`（`PlayAbility.c:181-236`）的个数 = `local_178`。
            //   正文里那个 `for each X` 只是它的转写 ⇒ 数据可判时**以数据为准**并出声。
            //   ⚠️ 只在本条 op 的 `(ab, lg)` 真有一条带 `multiplierSet` 的 `mult` 行时生效。
            {
                int sm; string smGap;
                if (TryStructuredMultiplier(ctx, owner, op, sourceCard, actor, out sm, out smGap))
                {
                    if (textCounted && sm != repeats)
                        ctx.Log($"{by}：「{op.Source}」正文计数「{op.CountRef}」= {repeats}，"
                              + $"而原版 `multiplier` 结构化数据 = **{sm}** ⇒ 按数据 {sm} 遍"
                              + "（`AbilityLogic__PlayAbility.c:181-236`）");
                    else if (!textCounted)
                        ctx.Log($"{by}：「{op.Source}」正文没给次数 ⇒ 按原版 `multiplier` "
                              + $"结构化数据结算 **{sm}** 遍");
                    repeats = sm;
                }
                else if (smGap != null)
                {
                    StructuredConditionNotice("mult-gap|" + (sourceCard != null ? sourceCard.Id : "?") + "|" + smGap,
                        $"卡「{sourceCard.Name}」（{sourceCard.Id}）的 `mult` 条件（正文「{op.Source}」）"
                      + $"**本版判不了**：{smGap} ⇒ 退回**正文口径**。");
                }
            }
            // 🆕 2026-09-16：**`…, N times` 的重复次数**（`EffectOp.RepeatTimes`）——
            //   走**同一条** `for` 循环：每遍 `ResolveOne` 会**重新挑目标、重掷 Rng**
            //   ⇒ 正是卡面「对 1 个**随机**敌人造成 1 点伤害，共 8 次」的意思。
            if (op.RepeatTimes > 1) repeats *= op.RepeatTimes;

            // ---- 🆕 2026-10-24（引擎线 `A12` 第 3 件）：`AbilityLogic.furtherMultiplier` ----
            // 原版 `AbilityLogic__PlayAbility.c:235-236`：
            //   `if (0 < *(int *)(param_1 + 0x38)) local_178 = local_178 * *(int *)(param_1 + 0x38);`
            // `local_178` 就是这条 logic 的**次数 / 倍数**（召唤循环上界 `:729` 起 ·
            // `local_178 * damageCriteria.damage` `:708` · `AdjustCardEffect(…, local_178, …)`）。
            //
            // 🔴 **为什么只在「正文没给次数」时才乘**：卡面印的那个数**就是** `local_178`
            //  （`Deploy 8 …` / `Draw 5 …` / `Deal 1 damage …, six times` —— 实测 27 张 fM>1
            //  的卡逐张核过，正文都自带同一个数），我们解析层已经从正文读到了它
            //  ⇒ 再乘一次就是**平方**（`Rok Invasion` 会变成部署 64 张）。
            //  ⇒ 本格的规则：**正文给过数 ⇒ 只对账、不乘**（给数的方式三种：
            //   `RepeatTimes` / `Amount` / `CountRef` 的 `for each` 动态计数）。
            //   `CountRef` 那一档是**动态**的（数场上几个），与常数 fM 不可比 ⇒ 一句话不说。
            int fm = op.AbilityFurtherMultiplier;
            if (fm > 1)
            {
                if (string.IsNullOrEmpty(op.CountRef))
                {
                    int textCount = op.RepeatTimes > 1 ? op.RepeatTimes
                                  : (op.Amount > 1 ? op.Amount : 1);
                    if (textCount <= 1 && repeats == 1)
                    {
                        repeats = fm;
                        ctx.Log($"{by}：「{op.Source}」正文没给次数 ⇒ 按原版 "
                              + $"`furtherMultiplier = {fm}` 结算 **{fm}** 遍");
                    }
                    else if (textCount != fm)
                    {
                        ctx.Log($"{by}：「{op.Source}」正文给的次数 {textCount} 与原版 "
                              + $"`furtherMultiplier = {fm}` **不一致** ⇒ 按正文结算"
                              + "（⛔ 不叠加：卡面那个数就是原版这一个数）");
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：**`role == "mult"` 的结构化乘数** ——
        /// 原版 `AbilityLogic.multiplier`（`AbilityLogic__PlayAbility.c:181-236`）的个数。
        ///
        /// <para>**判据（逐句读反编译）**：`GetTargets(manager, logic.multiplier, card, …)` 得到目标表，
        /// `targetsAffected == any(0)` ⇒ `local_178 = 1`；`210/220` ⇒ **累加 `traitsFilter` 的值**；
        /// 其余 ⇒ **表长**。随后 `if (0 < furtherMultiplier) local_178 *= furtherMultiplier`
        /// —— 那一格**不在本方法**（走 `EffectOp.AbilityFurtherMultiplier`，那儿已有消费者）。</para>
        ///
        /// <para>**返回值**：`true` = `n` 有效（结构化数据可判）；`false` + `gap == null` = 这条 op
        /// **没有** `mult` 行 / 不适用（最常见，⛔ 不是缺陷）；`false` + `gap != null` = 有行但**判不了**
        /// （调用方退回正文口径并出声，⛔ 不拿 0 顶）。</para>
        ///
        /// <para>🔴 **`exclude` 取反不在这里**：`excludeCondition` 的取反是在**逐候选**判条件时做的
        /// （`CountEval.CountSet` 现在固定 `exclude=false`）⇒ 这一格非 0 时**如实判不了**（出声），
        /// ⛔ 不许当「没写取反」。今天池内 4 条 `mult` 行 `ex=0` ⇒ 零影响。</para>
        ///
        /// <para>🔴 **2026-10-24（执行代理 `G6` · `C6` §1·3 A＋§1·4）：两处订正**（改前这两条都不成立）：
        /// ① **`targetsAffected == 0 (any)` ⇒ 本方法返回 `false`（= 不覆盖正文）**，⛔ 不返回 1。
        ///    原版 `any` 确实是 `local_178 = 1`，但它同时让**其余 35 格全部失效**
        ///    （`Any disregards all other filters`）⇒ 这个「乘数」没有任何信息；而正文里的
        ///    `for each X` **可能来自另一个机制** —— 一号实例 `GOF90 Wurrboy`：外层 ability 的
        ///    `multiplier` / `multiCountCriteria` 都是 `any(0)`，真正「按友方部队数」写在**它挂的那张附魔**
        ///    （`648c49… Wurrboy`）的 `multiplier.ta = 10` 上。按数据返回 1 会把正文那条 `for each`
        ///    **压成 1 遍**（比缺数据更糟）。
        /// ② **归位门没过（`op.AbilityIndex &lt; 0`）时按「卡上唯一一条**有值**的 `mult` 行」兜底**
        ///    （见 <see cref="FindSoleMultiplierRow"/>）—— 改前这里写着「⛔ 不做兜底猜测」。改的原因：
        ///    `A` 笔让 `cond == 0` 的行落下来了，而 `EffectText.AssignBlockIndicesOnly` 的片段门
        ///    （整卡**只有一条 ability**）把 `GOF4 Slugga Boy`（`Tide 1. Rally: …` = 两个能力块）
        ///    这类卡的触发正文**一律留在 `-1`** ⇒ 不兜底的话 `GOF4` 的 `multiplier`（`includeSelf = 0`
        ///    ＋ `filterSpecificCards = [自己]`）**永远取不到**，卡面「for each **other** friendly Slugga Boy」
        ///    会照正文数成「含自己」= +1。兜底只收**唯一一条有信息**的行（`ta != 0` 且 != 未知），
        ///    多于一条一律不猜。⛔ 等归位门补上后请删掉这一段。
        ///    ⚠️ **兜底只对 `AbilityIndex &lt; 0` 的 op 生效**：归位了但这一格没有 `mult` 行的 op
        ///    （例：`GOF74` 的 `Give Armour 1` = `(0,0)`）**必须保持「没有乘数」** —— 否则会被兜到
        ///    同卡 `(0,1)` 那条 `cond=40 damaged` 的乘数上，按 0 遍把「给督军 Armour 1」整条抹掉
        ///    （2026-10-24 实测：`play GOF74` 的那条 `Armour 1` 会消失）。</para>
        /// </summary>
        static bool TryStructuredMultiplier(BattleContext ctx, int owner, EffectOp op, CardDef card,
                                            UnitState actor, out int n, out string gap)
        {
            n = 0; gap = null;
            if (ctx == null || op == null || card == null) return false;
            CardDef.ConditionSpec spec = null;
            // 先按**下标**归位（`mult` 行永远带具体 `lg`，见 `CONDITION_ROLES` 的落值口径）；
            // 🔴 只有「**整条 op 都没归位**」（`AbilityIndex < 0`）才走兜底 —— ⛔ 不能对
            //    「归位了、但这一格没有 mult 行」的 op 兜底：实测 `GOF74` 的 op0
            //    （`Give Armour 1`，`(0,0)`）会被兜到 `(0,1)` 那条乘数上、按 0 遍抹掉。
            if (op.AbilityIndex >= 0 && card.TryGetCondition(op.AbilityIndex, op.LogicIndex, "mult", out spec) && spec != null)
            { /* 归位命中 */ }
            else if (op.AbilityIndex < 0)
                spec = FindSoleMultiplierRow(card);
            else
                spec = null;
            if (spec == null)
                return false;
            if (spec.MultiplierSet == null)
            {
                gap = "`mult` 行**没有** `multiplierSet`（旧卡表 / 生成器没落）";
                return false;
            }
            if (spec.Exclude)
            {
                gap = "`mult` 行的 `excludeCondition` = 1（逐候选取反）本版未实现";
                return false;
            }
            // ① `any(0)` ⇒ 没有乘数（理由见 doc ①）—— ⛔ 不是 `return n = 1`。
            int ta = spec.MultiplierSet.TargetsAffected;
            if (ta == CardDef.CountSetSpec.Unknown)
            {
                gap = "`multiplierSet.targetsAffected` **未知（没落）**";
                return false;
            }
            if (ta == 0) return false;
            var r = CountMultiplierTargets(ctx, owner, spec.MultiplierSet, spec.Condition, actor, out gap);
            if (r == null) return false;
            n = r.Value;
            return true;
        }

        /// <summary>
        /// 🆕 **2026-10-24（执行代理 `G6` · 见 <see cref="TryStructuredMultiplier"/> doc ②）**：
        /// `op` 归不到 `(ab, lg)` 时，按「**卡上唯一一条有值**的 `mult` 行」兜底。
        ///
        /// <para>「有值」= `MultiplierSet != null` 且 `targetsAffected != 0 / != 未知` ——
        /// `any(0)` 的行不携带乘数（`Any disregards all other filters`），拿它兜底只会把正文压成 1 遍。</para>
        /// <para>🔴 **结构闸（2026-10-24 实测加）**：还要求**整卡只有一条 ability、那条只有一条 logic**
        /// （`AbilityTargets.Count == 1 &amp;&amp; [0].TargetsAffected.Length == 1`）。
        /// 理由：兜底没有「这一格属于哪条 op」的信息，卡上有多条 logic 时它会把**别的 op** 乘掉 ——
        /// `UM77 Chairon`（`Deal 1-2 damage` 被 `(1,0)` 那条 `cond=222` 按 0 遍抹掉 · 实测
        /// `play UM77` 伤害 4 → 0）与 `TL68`（同类）就是这么撞出来的。</para>
        /// <para>收窄后**必须恰好剩一条**才用；0 条 / 多于一条 ⇒ `null`（⛔ 不猜是哪一条，与
        /// <see cref="FindCountRow"/> 同一口径）。用兜底时出声一次（`mult-fallback`）。</para>
        /// </summary>
        static CardDef.ConditionSpec FindSoleMultiplierRow(CardDef card)
        {
            if (card == null || card.Conditions == null || card.Conditions.Count == 0) return null;
            // ① **先卡一道结构闸**：整卡**只有一条 ability**、且那条**只有一条 logic**。
            //    只有这样，「那唯一一条乘数」才可能属于当前这条 op；否则同一张卡上别的 op 会被
            //    错误地乘 / 乘 0（实测：`UM77 Chairon` 的 `Deal 1-2 damage` 会被 `(1,0)` 那条
            //    `cond=222` 的乘数按 0 遍抹掉、`TL68` 的 Rally 同类）。判据 = `AbilityTargets`
            //    （v17 列，`[i]` = `cardAbilities[i]`、内层 = `abilityLogic[j]`）。
            var ats = card.AbilityTargets;
            if (ats == null || ats.Count != 1) return null;
            var a0 = ats[0];
            if (a0 == null || a0.TargetsAffected == null || a0.TargetsAffected.Length != 1) return null;
            CardDef.ConditionSpec hit = null;
            int n = 0;
            for (int i = 0; i < card.Conditions.Count; i++)
            {
                var row = card.Conditions[i];
                if (row == null || row.Role != "mult" || row.MultiplierSet == null) continue;
                if (row.Ability != 0 || row.Logic != 0) continue;      // 与上面那道闸对齐
                int ta = row.MultiplierSet.TargetsAffected;
                if (ta == 0 || ta == CardDef.CountSetSpec.Unknown) continue;
                hit = row; n++;
            }
            if (n != 1) return null;
            StructuredConditionNotice("mult-fallback|" + card.Id,
                $"卡「{card.Name}」（{card.Id}）的正文 op **没有归位到 ability/logic**（`AbilityIndex < 0`）"
              + "⇒ 按**卡上唯一一条有值的 `mult` 行**取结构化乘数（⛔ 不是猜：多于一条时不用）。");
            return hit;
        }

        /// <summary>
        /// 🆕 **2026-10-24（执行代理 `G6` · `C6` §1·4）**：`mult` 的候选计数 ——
        /// 一般情况直接转 <see cref="CountEval.CountTargets"/>；**只有**带 `filterSpecificCards`
        /// （原版 `TargetCriteria.filterSpecificCards`，`+0x70`）时才走本文件补的那一档。
        ///
        /// 为什么补：`CountEval.SetGap` 把「`filterSpecificCards` 非空」判成**未实现**（返回 `null`），
        /// 而 `GOF4 Slugga Boy` 的整个 `multiplier` 就是「**只有这一种卡**（它自己）＋ `includeSelf = 0`」——
        /// 不补这一档，卡面 `for each **other** friendly Slugga Boy` 永远退回正文（含自己 ⇒ 恒 +1）。
        /// ⚠️ **等 `CountEval` 支持这一维后请删掉 `CountSpecificCardsOnBoard`**（同一判据别留两份）。
        /// </summary>
        static int? CountMultiplierTargets(BattleContext ctx, int owner, CardDef.CountSetSpec set,
                                           int cond, UnitState actor, out string gap)
        {
            gap = null;
            if (set == null) { gap = "没有 criteria"; return null; }
            if (set.FilterSpecificCardIds == null || set.FilterSpecificCardIds.Length == 0)
                return CountEval.CountTargets(ctx, owner, set, cond, actor, out gap);
            return CountSpecificCardsOnBoard(ctx, owner, set, cond, actor, out gap);
        }

        /// <summary>
        /// 见 <see cref="CountMultiplierTargets"/>：数「**指定卡**在场上、且过其余筛」的个数。
        ///
        /// 判据 = `FilterMethods__CheckIfMeetsCriteria.c`：`:210-232` 阵营闸 · `:234-244`
        /// `includeSelf` / `excludeNotSelf` · `:255-261` 残骸闸 · `:286-300` 卡种白名单 ·
        /// `:300-420` 条件（`conditionsFilter`）。**只实现 `targetsAffected ∈ {10, 55}`（在场上）**
        /// —— 其余集合族本版未实现 ⇒ `null` + 出声（⛔ 不拿「在场上」顶替）。
        /// 其余「未实现的筛」（`armyFilter` / `keywordFilter` / `traitsFilter` / …）与
        /// `CountEval.SetGap` **同一张清单**：只要非 0 就 `null`（⛔ 不静默当「不过滤」）。
        /// </summary>
        static int? CountSpecificCardsOnBoard(BattleContext ctx, int owner, CardDef.CountSetSpec set,
                                              int cond, UnitState actor, out string gap)
        {
            gap = null;
            if (ctx == null) { gap = "没有对局上下文"; return null; }
            if (set.TargetsAffected != 10 && set.TargetsAffected != 55)
            {
                gap = $"`targetsAffected={set.TargetsAffected}` 的 `filterSpecificCards` 本版未实现";
                return null;
            }
            // 其余维度：与 `CountEval.SetGap` 同一张清单（非 0 = 未实现 ⇒ 判不了）。
            if (set.ArmyFilter != 0 || set.KeywordFilter != 0 || set.TraitsFilter != 0
                || set.SpellType != 0 || set.AttackFilterOption != 0
                || set.CurrentHealthFilterOption != 0 || set.ManaCostFilterOption != 0
                || set.RarityFilterOption != 0
                || (set.ExcludeEnchantmentIds != null && set.ExcludeEnchantmentIds.Length > 0)
                || set.ExcludeSimilarCards != 0 || set.ExcludeTargetted != 0
                || set.ExcludeFriendlyHero != 0 || set.ExcludeEnemyHero != 0
                || set.ExcludeRace != 0 || set.ExcludeTrait != 0
                || set.SelectFromChooseResolution != 0)
            {
                gap = "`filterSpecificCards` 与其它**未实现的筛**同时出现（本版判不了）";
                return null;
            }
            bool anySide = set.FriendlyUnits == 1 || set.EnemyUnits == 1;
            if (!anySide) { gap = "`friendlyUnits` / `enemyUnits` 都是 0（原版一个候选都不留）"; return 0; }

            int count = 0;
            for (int p = 0; p < 2; p++)
            {
                bool friendly = p == owner;
                if (friendly ? set.FriendlyUnits != 1 : set.EnemyUnits != 1) continue;
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || !u.IsAlive) continue;
                    // 卡种白名单 / 残骸闸 / 自他闸 —— 与 `CountEval.IncludeUnit` 同口径
                    // （那一份是 `private`；这里只补「指定卡」这一维，**其余逐条照抄**）。
                    if (u.IsRemnant && set.IncludeRemnants == 0) continue;
                    bool hero = u.IsWarlord;
                    if (hero && set.IncludeHero == 0) continue;
                    if (!hero && set.IncludeMinions == 0) continue;
                    if (actor != null)
                    {
                        bool self = ReferenceEquals(u, actor);
                        if (self && set.IncludeSelf == 0) continue;
                        if (!self && set.ExcludeNotSelf == 1) continue;
                    }
                    if (!CardIdIn(u.Card, set.FilterSpecificCardIds)) continue;
                    var v = ConditionEval.MeetsUnit(ctx, u, cond, false);
                    if (v == null) { gap = $"cond={cond} 在候选身上判不了"; return null; }
                    if (v.Value) count++;
                }
            }
            return count;
        }

        /// <summary>这张卡的 id 在不在那一串里（`filterSpecificCards` 解出来的 `uniqueId`）。</summary>
        static bool CardIdIn(CardDef c, string[] ids)
        {
            if (c == null || ids == null) return false;
            for (int i = 0; i < ids.Length; i++)
                if (string.Equals(ids[i], c.Id, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>`TryCountCondition` 的第二档：正文种类 → 卡上**唯一**一条 `count` 行。
        /// 多于一条时按**比较档**收窄一次（`noeffect` = 「数出 0 个」/ `deaths` = 「至少 1 个」）——
        /// 收窄后必须**恰好剩一条**才用，否则 `null`（归不了位，⛔ 不猜是哪一条）。</summary>
        static CardDef.ConditionSpec FindCountRow(CardDef card, string kind)
        {
            var rows = FindCountRows(card, kind);
            if (rows.Count == 1) return rows[0];
            if (rows.Count == 0) return null;
            // 🔴 **多条**：卡面那一句的意思能唯一对上一条比较档时才收窄 ——
            //   · `noeffect`（"if **no** enemy is affected"）= 数出 **0** 个 ⇒ `EqualTo / 0`；
            //   · `deaths`（"if a friendly troop **died**"）= 至少 **1** 个 ⇒ `EqualOrHigher / ≥1`；
            //   · `anypraying` 同上（"if **any** friendly unit is Praying"）。
            //   这是**拿数据自己的比较档去对正文的意思**，不是按卡猜；对不上/收窄后仍多条 ⇒ null。
            int wantOp = 0, wantRef = -1;
            switch (kind)
            {
                case "noeffect": wantOp = 1; wantRef = 0; break;
                case "deaths": case "anypraying": wantOp = 2; wantRef = 1; break;
                default: return null;
            }
            CardDef.ConditionSpec hit = null;
            int n = 0;
            foreach (var row in rows)
            {
                if (row.StatOption != wantOp || row.ReferenceValue < wantRef) continue;
                hit = row; n++;
            }
            return n == 1 ? hit : null;
        }

        /// <summary>卡上所有能对上这个正文种类的 `count` 行（`deaths` 收 221/222 两档）。</summary>
        static List<CardDef.ConditionSpec> FindCountRows(CardDef card, string kind)
        {
            var list = new List<CardDef.ConditionSpec>();
            if (card == null || string.IsNullOrEmpty(kind)) return list;
            int a, b;
            switch (kind)
            {
                case "deaths":     a = 221; b = 222; break;   // diedThisTurn / diedSinceLastTurn
                case "anypraying": a = 243; b = -1;  break;   // isPraying
                case "noeffect":   a = 331; b = -1;  break;   // `stealthOrCamouflage`（AM43 的「没有敌人被影响」）
                default: return list;
            }
            foreach (var row in card.Conditions)
            {
                if (row == null || row.Role != "count" || row.CountSet == null) continue;
                if (row.Condition != a && row.Condition != b) continue;
                list.Add(row);
            }
            return list;
        }

        static int FindCountRowCount(CardDef card, string kind)
        {
            return FindCountRows(card, kind).Count;
        }

        static bool ConditionHolds(BattleContext ctx, int owner, EffectOp op,
                                   UnitState chosen, out bool holds, CardDef card = null)
        {
            holds = false;
            if (TryStructuredCondition(ctx, owner, op, chosen, card, out holds)) return true;
            // 🆕 2026-10-23（`新账-97` ④）：直接调用本方法（带 card）的路径也要吃到 `count` 那一层。
            if (TryCountCondition(ctx, owner, op, card, out holds)) return true;
            switch (op.ConditionKind)
            {
                case "targetdiesorfriendly":
                {
                    // 🔴 **2026-10-21（引擎线 `T02` · `or` 族）**：`If the target dies **or** is a
                    //   friendly unit, gain 1 ☀`（`SOR31 Dogmata`；中文「如果目标死亡**或**为友方单位」）。
                    //   **两个析取项都要判** —— 改前 `EffectCondition.Normalize` 只认前半句
                    //   （`Contains("target dies")`）⇒ 归成 `targetdies`，后半句**静默消失**。
                    //
                    //   「死了」那一半沿用 `targetdies` 的口径（`LastTarget == null` 也算 ——
                    //   前面那句一个目标都没选中）。「友方」那一半判的是**目标此刻在谁的棋盘上**
                    //   （原版把「友方」表达成 `targetCriteria.friendlyUnits`：
                    //    `cards_1195_2022时代.json` 的 `SOR31.cardAbilities[1].targetCriteria` =
                    //    `targetsAffected 10 / friendlyUnits 1 / includeHero 1 / includeMinions 1 /
                    //     includeRemnants 1`）⇒ ⛔ 不是「这张卡的阵营/卡种」那种静态属性。
                    //   ⚠️ 目标优先取「上一条效果打中的那个」（`LastTarget`），其次才是本次选定的
                    //      —— 卡面说的是「**那个**目标」，而这句话紧跟在 `Deal 1 damage` 之后。
                    var t = ctx.LastTarget ?? chosen;
                    if (t == null) { holds = true; return true; }      // 同 `targetdies`：没有目标 ⇒ 当「死了」
                    if (!t.IsAlive) { holds = true; return true; }
                    int tp, tslot;
                    holds = FindSlot(ctx, t, out tp, out tslot) && tp == owner;
                    return true;
                }
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
                    // `If a friendly troop died this turn, give them Flank`（`Vengeful Surge`）。
                    // 🔴 **2026-09-14 改**：原来这里**一律返回「判不了」**（注释写着「本版没有死亡流水」）——
                    //    那个理由**早就不成立了**：`BattleContext.DeadUnits` 记着 `Owner` 与 `DeathTurn`，
                    //    是 A4 批 1 为「选一张本局阵亡的卡」建的，**同一条判据能直接读出「本回合死没死」**。
                    //    「判不了」的代价是整条效果不结算（而且是**静默**的，卡面不打 `*`）。
                    // 窗口 = `DeathTurn == ctx.Turn`（`PlayerState.LastTurnStartMark` 那种窗口是
                    // 「自你上个回合起」，**不是**这一条要的「本回合」）。
                    bool any = false;
                    foreach (var d in ctx.DeadUnits)
                    {
                        if (d == null || d.Owner != owner || d.DeathTurn != ctx.Turn) continue;
                        if (d.Card == null || !CreatePool.MatchesKind(d.Card, "troop")) continue;
                        any = true; break;
                    }
                    holds = any;
                    return true;
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
                    // `If you control <短语>` —— 卡面实测（2026-09-16 全池普查，7 处）：
                    //   · `a Vehicle`（`AM76` / `DA49` / `GOF10`）
                    //   · `a Beast`（`GOF78` / `GOF_Dok_s_Toolz_Painboss_talent`）
                    //   · `a Spanner or Mekboy Gazmek`（`GOF17` —— 兵种词**或卡名**）
                    //   · `3 or more troops`（`SW47`）
                    //   ⚠️ `no other troops` 那一族**不在这里** —— `Normalize` 归的是
                    //      `ownnoothertroops`（那条排在本条前面）。
                    // 🔴 **2026-09-16 修**：原来只实现了「控制 **0 个部队**吗」，而且**带数字的一律
                    //    返回「判不了」** ⇒ `If you control a Vehicle` 实际问的是「你场上有部队吗」：
                    //    有部队 ⇒ 判**不成立**（该触发时不触发）· 空场 ⇒ 判成立（不该触发时触发）。
                    //    **五张判错、一张判不了**，而且走的是**正常分支** —— 连「判不了」都不报，
                    //    是全池最静的一档（见 `_tmp_view/stub_audit_0916.md`）。
                    int need; string[] alts;
                    if (!ParseControlAsk(op.Condition, out need, out alts)) return false;   // 判不了
                    int n = 0;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var cu = ctx.Players[owner].Board[s];
                        if (cu == null || !cu.IsAlive) continue;
                        foreach (var a in alts) if (UnitMatchesControlAlt(cu, a)) { n++; break; }
                    }
                    holds = n >= need;
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
                    //
                    // 🆕 2026-09-14 A4 批 4：**强行触发**期间（`ctx.ForcedTriggerDepth > 0`）
                    //    这条条件**一律判成立** —— `Trigger the Codex ability of a friendly unit`
                    //    （`Author of the Codex`）的全部意义就是「不等那个时机、现在就结算一遍」，
                    //    不绕过去这个动词等于没做。理由与出处见 `BattleContext.ForcedTriggerDepth`。
                    holds = ctx.ForcedTriggerDepth > 0 || ctx.Players[owner].Energy == 0;
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
                // `If it's a <兵种>` —— **2026-09-16 起这条是兜底，不是主路**。
                // 🔴 更正：原来这里的注释写着「本版没做兵种字段」—— **那句从 2026-09-12 起就过时了**
                //    （`CreatePool.KindWords` 里 `daemon`/`vehicle`/`beast`/`infantry`/`monster`/
                //    `battlesuit`/`drone`/`structure` 全都有）。真正的病根在**解析层**：
                //    `EffectText.ClauseFullWord/IsClauseWord` 的正则只认分开写的系动词、
                //    **认不出缩写 `it's` / `they're`** ⇒ `If it's a Daemon` 掉进 `istype`。
                //    08-16 修完那个正则后，**已知词一律走 `targethaskw`（下面那条，真会判）**；
                //    能落到这里只剩「**那个词我们不认识**」（如 `If it's a Grot`）。
                // 这时返回 false = **「判不了」**（`ConditionHolds` 的契约，与「不成立」不同）⇒
                // 调用点（:295）会报 `本版判不了 —— 整条效果没有生效` 并记进 `unresolved`。
                // **这正是我们要的：不许静默失败。别把它改成静默的「不成立」。**
                case "istype": return false;
                // ---- 🆕 2026-09-14 A6 族 B：四条实测「判不了」的条件 ----
                case "rangedzero":
                {
                    // `Then, if it has 0 Ranged Attack, destroy it`（`Terrifying Crescendo`）——
                    // 前一句刚给了 `-3 Ranged Attack`，所以判的是**那一刻的当前值**（不是卡面印刷值）。
                    var t = chosen ?? ctx.LastTarget;
                    if (t == null) return false;
                    holds = t.RangedAttack <= 0;
                    return true;
                }
                case "warlordlowhp":
                {
                    // `If your Warlord has 10 or less Health, lower their cost by 4`（`At All Costs`）。
                    // ⚠️ 阈值**从条件原文里读**（别写死 10）—— 换个数字的卡迟早会有。
                    int n = ParseFirstNumber(op.Condition);
                    if (n < 0) return false;                       // 读不出阈值 ⇒ 判不了
                    var w = ctx.Players[owner].Warlord;
                    if (w == null) return false;
                    holds = w.Health <= n;
                    return true;
                }
                case "ownnoothertroops":
                {
                    // `If you control no other troops`（`Wulfen Pack Leader`，2026-09-15）。
                    // 🔴 **是「其他」** —— 卡自己就在场上、而且它自己就是一张部队
                    //    （`Wulfen Pack Leader` 是 unit）⇒ 不排掉自己这条**永远判不成立**。
                    // 「自己」= `ctx.ActingUnit`（战术卡那条路上是 null，那时本来就没什么可排的）。
                    holds = CountTroops(ctx, owner, ctx.ActingUnit) == 0;
                    return true;
                }
                case "enemyhightoughness":
                {
                    // `If your opponent controls a troop with 5 or more Health`（`Monster Hunters`，
                    // 2026-09-15）。阈值**从条件原文里读**，别写死 5。
                    // ⚠️ **判的是「当前生命」不是「生命上限」** —— 与本工程已有的镜像写法同口径：
                    //    `If your Warlord has 10 or less Health`（`warlordlowhp`）读的就是 `w.Health`。
                    //    （这条是我们挑的，卡面两种读法都通 —— 已记进 `卡牌效果or句_审计.md` 待核。）
                    int n = ParseFirstNumber(op.Condition);
                    if (n < 0) return false;                       // 读不出阈值 ⇒ 判不了
                    bool any = false;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var eu = ctx.Players[1 - owner].Board[s];
                        if (eu == null || !eu.IsAlive) continue;
                        if (eu.Card != null && !CreatePool.MatchesKind(eu.Card, "troop")) continue;
                        if (eu.Health >= n) { any = true; break; }
                    }
                    holds = any;
                    return true;
                }
                case "targethaskw":
                {
                    // `<代词> has/have/is/are <关键词或兵种>[ or <关键词或兵种>]`：
                    //   `If it has Stealth, give it Flank`（`Flickerjump`）·
                    //   `If it is [Destroyer], give it Armour 1`（`Hardwired Destruction`）·
                    //   `If they are Battlesuits, give them Flank`（`Dynamic Offensive`）·
                    //   `If it has Hunt Mark`（`Vindicator`，2026-09-15 接通 —— **两词关键词**）·
                    //   `If it's a Vehicle **or** Battlesuit`（`TAU48 Technological Supremacy`）。
                    // 判据（**哪些**词）读 `EffectCondition.ClauseKeywords`，**和解析层同一份**。
                    // 🔴 **2026-09-16 修**：原来读的是 `ClauseKeyword`（**单数**，只取第一个词）
                    //    ⇒ `… a Vehicle or Battlesuit` **只判了载具**，Battlesuit 那半**静默丢掉**，
                    //    而探针上这张卡显示「完全解析 · 不认识 0 · 半懂 0」。
                    var words = EffectCondition.ClauseKeywords(op.Condition);
                    if (words == null || words.Length == 0) return false;
                    // 目标 = **上一条效果选中的那一批**（`them` = `ctx.LastTargets`；单个用 `LastTarget`）
                    var ts = new System.Collections.Generic.List<UnitState>();
                    if (chosen != null) ts.Add(chosen);
                    else if (ctx.LastTargets != null) ts.AddRange(ctx.LastTargets);
                    if (ts.Count == 0 && ctx.LastTarget != null) ts.Add(ctx.LastTarget);
                    // ⚠️ **「没有目标 ⇒ 判不了」这个决定要等一等**：`them` 还可能指**手里的那几张**
                    //    （见下面那段，2026-10-17 B7 加的），而手牌那一支要用到下面解出来的
                    //    `asKw` / `asKind` / `kindWord` / `sideOf` ⇒ 判空推迟到那之后。
                    // 每个词先解成「关键词」或「兵种」；**有一个两种都不是 ⇒ 判不了**
                    // （不认识的词当成立 = 静默打错，这是本工程的红线）。
                    // 🆕 **2026-09-16：`friendly troop` / `enemy troop` 这种带阵营前缀的**
                    //    （`Armoury of Excess` 的 `If target is a friendly troop`）——
                    //    前缀剥掉、记成一边，剩下的当兵种词判；**两边都查，判据仍只此一处**。
                    var asKw = new string[words.Length];
                    var asKind = new bool[words.Length];
                    var kindWord = new string[words.Length];
                    var sideOf = new int[words.Length];
                    for (int i = 0; i < words.Length; i++)
                    {
                        string wd = words[i];
                        // **裸的 `enemy` / `friendly`**（`If target is an enemy`）—— 只判阵营、**不筛兵种**
                        if (wd == "enemy") { sideOf[i] = -1; kindWord[i] = ""; asKw[i] = null; asKind[i] = false; continue; }
                        if (wd == "friendly") { sideOf[i] = 1; kindWord[i] = ""; asKw[i] = null; asKind[i] = false; continue; }
                        if (wd.StartsWith("friendly ")) { sideOf[i] = 1; wd = wd.Substring(9).Trim(); }
                        else if (wd.StartsWith("enemy ")) { sideOf[i] = -1; wd = wd.Substring(6).Trim(); }
                        kindWord[i] = wd;
                        int klen;
                        string kw = KeywordTable.Normalize(wd, out klen);
                        asKw[i] = (kw != null && klen == wd.Length) ? kw : null;
                        asKind[i] = CreatePool.IsKnownKind(wd);
                        if (asKw[i] == null && !asKind[i]) return false;
                    }
                    // 🔴 **2026-10-17（B7：卡实例身份 —— `SetupCardInHand` 那一套）**：
                    //    `them` 也可能指 **手里那几张** —— 原版把效果**挂在那一张牌自己身上**
                    //    （`PlayerHand.SetupCardInHand`，判据逐条写在 `HandReferents` 上方那段）。
                    //    判据**复用同一份** `words` / `sideOf` / `kindWord`（**不另写一张词表**），
                    //    只是被判的对象从 `UnitState` 换成 `CardInstance`：
                    //    · 关键词 → `CardDef.Has`（手牌那一侧还没有「运行时加的关键词」这个查询口，
                    //               所以读模板本身 —— 见 `AttachEffectToHandInstances` 的如实说明）；
                    //    · 兵种   → 同一个 `CreatePool.MatchesKind`（**判据只此一份**）；
                    //    · 阵营   → `HandReferents(ctx, owner)` 收的**本来就是 `owner` 自己的手牌**
                    //               ⇒ `enemy …` 前缀这一条对它是**判不了**，不算命中。
                    //    一号靶：`TAU54 Dynamic Offensive`
                    //    `Draw 2 troops from your deck. If they are Battlesuits, give them Flank.`
                    if (ts.Count == 0)
                    {
                        var handTs = HandReferents(ctx, owner);
                        if (handTs.Count == 0) return false;       // 场上、手上都没有目标 ⇒ 判不了
                        bool allHand = true;
                        foreach (var hi in handTs)
                        {
                            var hc = hi == null ? null : hi.Card;
                            if (hc == null) continue;
                            bool hit = false;
                            for (int i = 0; i < words.Length; i++)
                            {
                                if (sideOf[i] == -1) continue;                    // `enemy …` 前缀
                                if (asKw[i] == null && !asKind[i]) { hit = true; break; }
                                if ((asKw[i] != null && hc.Has(asKw[i]))
                                    || (asKind[i] && CreatePool.MatchesKind(hc, kindWord[i])))
                                { hit = true; break; }
                            }
                            if (!hit) { allHand = false; break; }
                        }
                        ctx.Log($"（「{op.Condition}」判的是**手牌里那 {handTs.Count} 份**"
                              + $"（{Names(handTs)}）⇒ {(allHand ? "成立" : "不成立")}）");
                        holds = allHand;
                        return true;
                    }
                    // ⚠️ **全中才算成立**（复数目标是「它们都是 X」）—— **这是我们挑的**：
                    //    卡面写的是 `If they are Battlesuits`（复数 + 系动词），读作「都符合」。
                    //    单个目标时「任一备选命中」即成立（`A or B` 的或）。
                    bool all = true;
                    foreach (var t in ts)
                    {
                        if (t == null) continue;
                        bool hit = false;
                        for (int i = 0; i < words.Length; i++)
                        {
                            // 带了 `friendly` / `enemy` 前缀的，**先判阵营**
                            if (sideOf[i] != 0)
                            {
                                int want = sideOf[i] == 1 ? owner : 1 - owner;
                                if (OwnerOf(ctx, t) != want) continue;
                            }
                            // **只判阵营那种**（裸 `enemy` / `friendly`，`kindWord` 是空串）⇒ 阵营对上就成立
                            if (asKw[i] == null && !asKind[i]) { hit = true; break; }
                            if ((asKw[i] != null && t.Has(asKw[i]))
                                || (asKind[i] && t.Card != null
                                    && CreatePool.MatchesKind(t.Card, kindWord[i])))
                            { hit = true; break; }
                        }
                        if (!hit) { all = false; break; }
                    }
                    holds = all;
                    return true;
                }
                case "foreach": return false;     // for-each 计数层还没做
                case "energycheck":
                {
                    // `If you have less than N Energy, …`。
                    // 🔴 **2026-09-16 修**：原来这一支是 `return false`（= 判不了）⇒ 那半句从来
                    //    不发生；而它是张**单位卡** ⇒ 卡面连 `*` 都不打（`BattleDriver` 只给战术卡打星）。
                    // ⚠️ 阈值**从条件原文里读**（别写死 6）—— 换个数字的卡迟早会有。
                    // 🔴 **2026-10-11（`A1357`）更正**：这一支原来的注释写着「（`SOR72 Adelaide the Serene`）
                    //    —— **全池就这一张**」—— **那句已不成立**：`SOR72` 的英文 `desc` 把**信仰**错抄成了
                    //    `Energy`（数据侧已改回 `☀`），它现在归 `faithcheck`。今天**全池 0 张**走这一支，
                    //    但**留着**（`less than N Energy` 是合法写法，换张卡就有；删了还会让
                    //    `RuleEngineTest` 的 `CanJudgeCondition("energycheck")` 那条断言红）。
                    int n = ParseFirstNumber(op.Condition);
                    if (n < 0) return false;                        // 读不出阈值 ⇒ 判不了
                    holds = ctx.Players[owner].Energy < n;
                    return true;
                }
                case "faithcheck":
                {
                    // 🆕 **2026-10-11（第十三会话 · `A1357`）**：同一条判据、**另一种资源** ——
                    //   `Rally: If you have less than 6 ☀, gain 1 ☀ for each enemy unit`
                    //   （`SOR72 Adelaide the Serene`，全池唯一一张；卡图与 `descZh` 都是**金太阳**）。
                    //
                    //   **为什么必须与 `energycheck` 分开两条 case**：判「`☀` 是哪种资源」的
                    //   判据只此一处（`EffectText.MentionsFaith`，与 `CostKindOf` 同源）——
                    //   让一个 case 去现认字符串就等于**第二份判据**，两处迟早不一致。
                    //   ⚠️ 阈值同样**从条件原文里读**（别写死 6）。
                    //   ⚠️ 读的是 `PlayerState.Faith`（**不是** `Energy`）—— 原来错判能量时，
                    //      `SOR72` 的这半句等于「按当前能量决定给不给信仰」，静默错语义。
                    int n = ParseFirstNumber(op.Condition);
                    if (n < 0) return false;                        // 读不出阈值 ⇒ 判不了
                    holds = ctx.Players[owner].Faith < n;
                    return true;
                }
                default: return false;
            }
        }

        /// <summary>条件原文里的**第一个整数**（读不出来返回 -1）。给 `your warlord has N or less Health` 用。</summary>
        static int ParseFirstNumber(string s)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            var m = System.Text.RegularExpressions.Regex.Match(s, @"\d+");
            int n;
            return (m.Success && int.TryParse(m.Value, out n)) ? n : -1;
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
            // 🔴 **2026-10-24（执行代理 `G6` · `C6` §四·2）：判据收口到 `CountKindMatches`**
            //   （与 `CountFor` 的盘面 / 手牌分支**同一份**；改前这里是第三份 switch）。
            // ⚠️ 保留改前的**兜底语义**：`ClassifyCount` 的词表是封闭的，认不出的（历史注释点名的
            //    `counter`）按「都算」处理 —— 这里**不改成一 -1**，那会让那张卡整条不结算。
            return CountKindMatches(c, reference) ?? true;
        }

        static bool Same(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// **结算层真的实现了的动词** —— 解析得出来但 `EffectDispatch` 里没登记的动词，
        /// 效果是**打出去什么都不发生**。
        ///
        /// 🔴 **2026-10-11 就地订正（铁律 5 · `A1464`）**：这一行原来写着
        ///    「（现在只剩 `repeat` / `lowercost` / `reanimate`）」—— **那三个早就在表里**
        ///    （`repeat` `:600` · `lowercost` `:599` · `reanimate` `:601`），
        ///    而下面那行说明本集合是**从表派生**的 ⇒ 那句括注**自相矛盾**。
        ///    **错因**：那是手写清单的残留（同一段自己也写着「以前这里是手写的第二份清单」），
        ///    改成派生之后**忘了删括注**。
        ///    ✅ **现算的答案是空集**：解析侧 `EffectText.cs` 里 `Verb = "…"` 的字面量共 **41** 个，
        ///    减掉 `EffectDispatch.Keys` ⇒ **0 个没登记**（2026-10-11 静态算过；`EffectText` /
        ///    `CardDef` / 本文件里**没有动态拼动词名**的写法）。
        ///    ⛔ **别再往这里写「只剩哪几个」这种清单**（它会烂）—— 要问「谁没登记」，
        ///    现算「`EffectText` 的动词集合 − `EffectDispatch.Keys`」这个差集。
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
        /// **`setCurrentHealth(100)`** —— 原版 `CardScript__AddEffect.c:229-247`（卡面
        /// `Lower its Health to 1`，SO 里是 `buffType 100 / healthChange N`）。
        ///
        /// 原版逐句：① **先** `ShouldTriggerEcastasy(this, this, healthChange)`（`:232-243`）——
        /// 目标自己带**印刷的** `Ecstasy X` 且 `X < 当前生命 ≤ …` ⇒ `OnTrigger(0x2cb = 715)`；
        /// ② 然后 `currentHealth(+0x68) = healthChange(+0x60)` —— **无条件写、不夹上限、不查 Invulnerable**。
        /// ⚠️ 原版这一支**不发伤害**（不经过 `ReceiveDamage`）⇒ 我们**别调 `EmitHit`**（那会多出一条伤害事件、
        /// 还会把 `Ecstasy`/`Cruelty`/`Penitence` 那一族按「受伤」的极性多触发一次）。
        /// ⛔ **不是 `DoSetHealth`**：那个对应 `buffType 9 setHealth`（= `currentHealth = currentMaxHealthProcessed`，
        /// `.c:794-820`，**池内 0 张**），语义不同（`DoSetHealth` 还是「只降 + 查 Invulnerable」的老近似）。
        /// </summary>
        static bool DoSetCurrentHealth(BattleContext ctx, int owner, string by, EffectOp op,
                                       UnitState chosen, List<string> unresolved)
        {
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any", Count = 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, null, op);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要把生命设成 {op.Amount}，但没有指到任何单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（setcurrenthealth 没有目标）");
                return false;
            }

            int n = 0;
            foreach (var t in targets)
            {
                if (t == null) continue;
                // 🔴 `AddEffect` 闸①/③ 照过（原版这两道闸在所有 buff 之前，见「AddEffect 的四道前置闸」）
                if (!AddEffectCardStateAllows(ctx, t, by)) continue;
                if (!AddEffectRemnantAllowsOp(ctx, t, op, by)) continue;

                int before = t.Health;
                t.Health = op.Amount;                  // 无条件（原版 `:245`）
                CheckEcstasyCrossing(ctx, t, before);  // 判据只此一处（RuleCore.CheckEcstasyCrossing）
                n++;
                ctx.Log($"{t.Name} 的生命被**设为** {op.Amount}（{before} → {t.Health}）—— 「{op.Source}」"
                      + "（原版 `setCurrentHealth`：无条件写、不夹上限、不查 Invulnerable）");
            }
            if (n == 0) { unresolved.Add(op.Source + "（setcurrenthealth 一个都没设）"); return false; }
            return true;
        }

        /// <summary>
        /// **`sethealth` —— 只对应原版 `buffType == 9 setHealth`**（`CardScript__AddEffect.c:794-820`：
        /// `currentHealth = currentMaxHealthProcessed`）。
        ///
        /// 🔴 **2026-10-25（`K8` · 铁律 5 就地订正）：这个动词现在【没有数据生产者】，而且不再是
        /// 「`Lower its Health to N` 那一条」。**
        ///   · 卡面 `Lower its Health to N`（`EC39 Eternal Servitude`）在 SO 里是 **`buffType == 100
        ///     setCurrentHealth`**（`cards_truth.json` 现读：`{buffType: 100, healthChange: 1}`）——
        ///     解析层已改判给**新动词 `setcurrenthealth`**（<see cref="DoSetCurrentHealth"/>）。
        ///   · `buffType == 9` 现读**池内 0 张** ⇒ 本方法今天**跑不到**。
        ///   🔴 **2026-10-25（`E1` · 铁律 5 就地订正）：上面那句「它的实现仍是老近似……」已不成立** ——
        ///     属性模型 Stage 2c 落地后（`CurrentMaxHealthProcessed` 现算 + `IsSet` 修饰），
        ///     本方法已按真语义重写：**记一条覆盖上限的修饰 → `Health = MaxHealth`（无 clamp、不查 `Invulnerable`）**。
        ///     留痕原因：口径要变的时候先把「当时是什么」写下来，下个会话才知道这一条被核过。
        /// ⚠️ 保留的一条**表现口径**（我们挑的，不是原版语义）：值变了就 `EmitHit` 发一条伤害事件
        /// （表现层靠它掉血飘字/血条动画；原版 9 不经过 `ReceiveDamage`）—— **100 那一支不发**，两条别混。
        /// </summary>
        static bool DoSetHealth(BattleContext ctx, int owner, string by, EffectOp op,
                                UnitState chosen, List<string> unresolved)
        {
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any", Count = 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, null, op);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要降生命，但没有指到任何单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（sethealth 没有目标）");
                return false;
            }

            // 🔴 `AddEffect` 闸②（jam）/ 闸④（施放者不在场）—— 判据 `.c:85-98 / 171-195`。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, chosen, false, by)) return true;

            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                // 🔴 `AddEffect` 闸①（卡状态）+ 闸③（残骸门）——
                //   `setHealth(9)` / `setCurrentHealth(100)` 都不是 `addStats` 的生命项 ⇒ 残骸一律不收。
                if (!AddEffectCardStateAllows(ctx, t, by)) continue;
                if (!AddEffectRemnantAllowsOp(ctx, t, op, by)) continue;
                // 🔴 **2026-10-25（`E1` · 属性模型 Stage 2c + ② 合批）：「`setHealth(9)` 与 `MaxHealth`
                //   两格并一格」在这一段落地。** 判据 = `CardScript__AddEffect.c:794-820` +
                //   `get_currentMaxHealthProcessed.c:94/:104` + 判据 §7·6 ④.4：
                //     ① 把这条 `setHealth` 记进**修饰表**（`IsSet`）—— 它**覆盖**上限算式里的
                //        `base` 那一项（`base = MaxHealthBase + Σ非extrinsic`，**整项换掉、不是加**），
                //        但**保留 extrinsic 的加血**（`iVar5`）；
                //     ② 应用那一刻 `currentHealth = currentMaxHealthProcessed`；
                //     ③ **不夹上限、不查 `Invulnerable`**（原版这一支两者都没有）。
                //   ⚠️ 改前这里是老近似：`只降不升 + 比当前小才改 + 查 Invulnerable` —— 三条都不是原版。
                //   ⚠️ 池内现在 **0 张**（`buffType 9` 现读 0）⇒ 这一笔**今天没有可观察量**，
                //     但语义按判据补齐（铁律 11：只排先后、不排做不做）。
                var setMod = t.AddStatMod("health", op.Amount, null, false, isSet: true);
                if (!string.IsNullOrEmpty(op.Duration))
                {
                    // 限时的那一份：到期**摘格**（`RevertBuffs` 会 `RemoveStatMod` + `ClampHealthToMax`）
                    t.AddTempBuff(new UnitState.TempBuff
                    {
                        IsKeyword = false, Name = "health", Value = op.Amount, Owner = owner,
                        UntilMyNextTurn = op.Duration == "nextturn", Src = by,
                        SourceCard = ctx.PlayingCard != null ? ctx.PlayingCard.Name : by,
                        Mod = setMod,
                    });
                }
                int before = t.Health;
                t.Health = t.MaxHealth;                 // ② 应用那一刻 = 覆盖后的 `currentMaxHealthProcessed`
                // 表现层靠伤害事件掉血飘字/血条动画（这一支**不是**伤害，原版不发 `ReceiveDamage`；
                //   ⚠️ 这是**我们挑的**表现口，别把 `EmitHit` 当成原版语义）
                if (before != t.Health) EmitHit(ctx, t, before - t.Health);
                n++;
                ctx.Log($"{t.Name} 的生命 {before} → {t.Health}（上限 {t.MaxHealth}）—— 「{op.Source}」"
                      + "（原版 `setHealth`：写完 `currentHealth = currentMaxHealthProcessed`）");
            }
            if (n == 0) { unresolved.Add(op.Source + "（sethealth 一个都没改）"); return false; }
            return true;
        }

        /// <summary>
        /// `double` —— **把近战攻击与生命翻倍**（`Possession`，Black Legion，8 费；2026-09-14 A4 批 3）。
        ///
        /// **语义出处**：卡面自己写着 `Double the Melee Attack and Health of a friendly troop`。
        /// 原版那两个 handler（`BattleManager__ResolveChangeBaseAttack.c` /
        /// `__ResolveChangeMaxHealth.c`，**都反编译出来了**）收的是**新值、不是增量**——
        /// 「×2」是**效果层**算的，而那一层的数据**我们拿不到**（同 `Dark Pact` 那类）。
        ///
        /// ⚠️ **两处是我们的取舍，别当成原版定论**（写下来免得下次当成查到了）：
        ///  ① **按「当前显示值」×2**（含身上已加的增益）。原版那两个 handler 还会**先清掉该类增益**
        ///     再写（`CardScript__ChangeBaseAttack.c` 里那段 `RemoveAt`）—— 我们**没清**。
        ///     差别只出现在「挂着增益的单位」上，而且我们给的方向是**更强**。
        ///  ② **生命：当前值连上限一起 ×2**。依据是本工程**既有的生命口径**
        ///     （`rule_core.gd:3317`「生命增减**连上限一起动**」，见 `ApplyOneGain` 里那条）——
        ///     **不是**从 `Possession` 这张卡查到的。
        ///     ⚠️ 张力在这儿：原版 `ChangeMaxHealth` 这个名字看着像「只改上限」，可它**同时**服务
        ///     `Eternal Servitude` 的 `Lower its Health to N`（我们对那条的读法是「改**当前**生命」，
        ///     见 `DoSetHealth`）。两张卡互为线索但**没能定论** ⇒ 这两条**如实标着是取舍**。
        /// 产出：近战攻击 · 生命上限 · 当前生命**三个一起翻倍**。
        /// </summary>
        static bool DoDouble(BattleContext ctx, int owner, string by, EffectOp op,
                             UnitState chosen, List<string> unresolved)
        {
            // 🆕 2026-09-15：**翻哪几项由 `Payload` 定**（判据在 `EffectText.TryDouble`）——
            //   · `melee,health` —— `Possession`「使一个友方部队的**近战攻击和生命**翻倍」
            //   · `melee,ranged` —— `Maulerfiend` 的 `Ecstasy 5`
            //     「**狂喜 5：使本部队的近战和远程翻倍**」—— ⚠️ **没有生命**！
            //   原来这里压根不读 `Payload`、一律按「近战+生命」翻 —— 所以第二种写法
            //   此前是被 `ReDouble` **整句挡在门外**的（挡的理由对、做法不对）。
            string pw = (op.Payload ?? "").Trim().ToLowerInvariant();
            bool dMelee = pw.Contains("melee"), dHealth = pw.Contains("health"), dRanged = pw.Contains("ranged");
            if (!dMelee && !dHealth && !dRanged)
            {
                ctx.Log($"{by}：「{op.Source}」要翻倍，但**没说翻哪几项**（载荷「{op.Payload}」）"
                      + " —— **这条没生效**（不猜）");
                unresolved.Add(op.Source + "（double：没说翻哪几项）");
                return false;
            }

            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            // 🔴 `AddEffect` 闸②（jam）/ 闸④（施放者不在场）—— 判据 `.c:85-98 / 171-195`。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, chosen, false, by)) return true;

            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                // 🔴 `AddEffect` 闸①（卡状态）+ 闸③（残骸门）——
                //   原版 `doubleAttack(20)` 在分派**之前**被改写成 `addStats` 且只带
                //   `meleeAttackChange`（`healthChange == 0`，`.c:198-207`）⇒ 残骸一律不收。
                if (!AddEffectCardStateAllows(ctx, t, by)) continue;
                if (!AddEffectRemnantAllowsOp(ctx, t, op, by)) continue;
                int a0 = t.Attack, h0 = t.Health, m0 = t.MaxHealth, r0 = t.RangedAttack;
                // 🔴 **2026-10-25（`E1` · K1 · 第一权威 `CardScript__AddEffect.c:198-207`）**：
                //     `if (buffType == 0x14 doubleAttack) { buffType = 1;
                //          if (CurrentMeleeAttack < 0x3fffffff) meleeAttackChange = CurrentMeleeAttack; }`
                //   ⇒ 原版「翻倍」**不是写 base**，而是**落一条 `addStats` 修饰、值 = 施加那一刻的当前值**
                //     （溢出保护：当前值 ≥ `0x3fffffff` 就**不写**、即不翻）。
                //   🔴 **这一条订正了 `判据/战斗逻辑驱动_前置门与现算口径.md` §6·2·A 那句
                //     「按 base 算新 base ⇒ 落 `ChangeBaseAttack` 等价」** —— 按方案 §三·9
                //     （实现完回头核判据），以 `.c` 原文为准。第一版按那句写成 `AttackBase *= 2`，
                //     实测把 `EC30`（9 base + 1 修饰）从 20 变成 19 ⇒ 探针基线报警，正好暴露了判据的错。
                //   ⚠️ **只有近战那一支是原文**；`ranged` / `health` 两格原版没有独立方法体
                //     （`doubleAttack` 只改写 `meleeAttackChange`）—— 我们的卡面写的是「近战和生命」
                //     （`Possession`）/「近战和远程」（`Ecstasy 5`），这里按近战那一支**同形外推**，如实标着。
                if (dMelee && a0 < 0x3fffffff) t.AddStatMod("attack", a0);
                if (dRanged && r0 < 0x3fffffff) t.AddStatMod("ranged", r0);
                if (dHealth) { t.AddStatMod("health", m0); t.Health = h0 * 2; }
                if (dMelee || dRanged) RuleCore.RecomputeCurrentAttackTypeOf(ctx, t);
                n++;
                var parts = new System.Text.StringBuilder();
                if (dMelee) parts.Append($"近战攻击 {a0} → {t.Attack}");
                if (dRanged) parts.Append((parts.Length > 0 ? "、" : "") + $"远程攻击 {r0} → {t.RangedAttack}");
                if (dHealth) parts.Append((parts.Length > 0 ? "、" : "") + $"生命 {h0}/{m0} → {t.Health}/{t.MaxHealth}");
                ctx.Log($"{t.Name} 的{parts}（「{op.Source}」）");
            }
            if (n == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要翻倍，但没有指到任何单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（double 没有目标）");
                return false;
            }
            return true;
        }

        /// <summary>
        /// `ferocitystay` —— **下一次用狂暴时留在场上**（`Bjorn's Shrine`，SpaceWolves，1 费；2026-09-14 A4 批 3）。
        ///
        /// 这个 op **只打标记**，真正的判据在 <see cref="RuleCore.UseAlternative"/> 的狂暴那一段
        /// （回牌库之前查 `UnitState.FerocityStay`，**用掉当场清** —— 「下一次」就靠这一清）。
        ///
        /// 🔴 **出处**：`CardScript__UsedActiveAbility.c:52-64`（`has(ferocity)` → 广播 →
        ///    `has(dontReturnFerocity)` 或 `EnoughPendingDamageToDie` 才跳过回牌库）+ `:75-88`
        ///    （用完 `SendRemoveEffect(..., 1)` 摘掉 ⇒ 一次性；`DefinedTrait:131 dontReturnFerocity=1270`）。
        /// ⚠️ 目标必须是**场上的单位** —— 标到一张已经不在场上的卡上没有意义，如实报。
        /// </summary>
        static bool DoFerocityStay(BattleContext ctx, int owner, string by, EffectOp op,
                                   UnitState chosen, List<string> unresolved)
        {
            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            int n = 0;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                t.FerocityStay = true;
                n++;
                // 🆕 2026-09-16：常驻版（`SW36 Bjorn the Fell-Handed` 的 `it stays in play`）走同一个标记，
                // 但措辞不能再说「本回合下一次」—— 它每次用狂暴都会重新标一次（事件层），
                // 日志照实说，免得下一个会话看日志以为是一次性。
                ctx.Log(op.Payload == "always"
                        ? $"{t.Name}：**用狂暴时留在场上**（常驻 ——「{op.Source}」）"
                        : $"{t.Name}：**本回合下一次用狂暴时留在场上**（「{op.Source}」）");
            }
            if (n == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要标记「留在场上」，但没有指到任何单位 —— **这条没生效**");
                unresolved.Add(op.Source + "（ferocitystay 没有目标）");
                return false;
            }
            return true;
        }

        /// <summary>
        /// `Trigger the &lt;关键词&gt; ability/abilities of &lt;目标&gt;` —— **强行把那个关键词的正文
        /// 现在结算一遍**。以及尾句形态 `… and trigger their &lt;关键词&gt; abilities`
        /// （目标由 `EffectText.Finish` 的反向共用目标继承前半句）。
        ///
        /// 🆕 2026-09-14 A4 批 4。卡面两族写法、实测 8 张卡，清单与出处见
        /// `RuleCore.TriggerKeywordOf` 的注释；`op.Payload` 是关键词的**规范名**
        /// （`EffectText.TryTriggerAbility` 已经过了 `CardDef.RoutableTriggers` 那道闸）。
        ///
        /// ⚠️ **`forced = true`**：这类句子的语义就是「不等那个时机」。它对
        ///    `ConditionKind == EnergyZero` 那一类条件有效（`Codex:` 靠它），别的条件照判。
        ///    出处与「哪半条查不到」写在 `BattleContext.ForcedTriggerDepth`。
        /// ⚠️ **触发不了的（那张卡上压根没有这个关键词的正文）要如实报** ——
        ///    `TriggerKeywordOf` 返回 false 时记一条 unresolved，别让「触发了」和
        ///    「触发了但什么都没有」长得一样（本工程红线）。
        /// ⚠️ **先快照再触发**：触发会改棋盘（能打死人、能再部署），边遍历边读数组是未定义行为
        ///    —— 和 `ResolveDeploy` / `BroadcastWhen` 同一条教训。
        /// </summary>
        static bool DoTriggerAbility(BattleContext ctx, int owner, string by, EffectOp op,
                                     UnitState chosen, List<string> unresolved)
        {
            if (string.IsNullOrEmpty(op.Payload))
            {
                unresolved.Add(op.Source + "（triggerability 没写是哪个关键词）");
                return false;
            }

            // 🆕 2026-10-22（`W64`·`B-16`）：按**这条 op** 的结构化目标条件行筛（`null` = 没有/读不到 ⇒ 不筛）。
            var targets = ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要强行触发 `{op.Payload}`，"
                      + "但**没有指到任何单位** —— 这条没生效");
                unresolved.Add(op.Source + "（triggerability 没有目标）");
                return false;
            }

            var snapshot = new List<UnitState>(targets);
            // ⚠️ `Payload` 可以是**逗号分隔的多个关键词**（`Trigger the Teleport **and Slay** effects
            //    of a friendly unit`，`Master Lazarus`）—— 拆开逐个触发。
            var kws = op.Payload.Split(',');
            int n = 0;
            foreach (var t in snapshot)
            {
                // ⚠️ 快照之后世界可能已经变了（前一个触发把人打死了 / 把牌挪走了）—— 再判一次
                if (t == null || !t.IsAlive) continue;
                bool any = false;
                foreach (string raw in kws)
                {
                    string kw = raw.Trim();
                    if (kw.Length == 0) continue;
                    // 🆕 2026-09-16：**灵魂石能力**（卡面 `N [Spirit Stone]: 正文`）不在
                    //    `CardDef.TriggerOps` 里 —— 它收在 `CardDef.SpiritOps`（`CollectSpiritOps`），
                    //    所以走不了下面那条通用路（`TriggerKeywordOf` 对 `spiritstone` **恒为 null**，
                    //    直接落到「身上没有正文」那一行 ⇒ 变成「解析得出、结算空转」）。
                    //    这条卡面只有一张：`ASH52 Cosmic Serpent`
                    //    （`Trigger the abilities requiring Spirit Stones of all your troops`）。
                    //    ⚠️ **不付费**：原版触发路径不查余额、不扣石，证据见
                    //       `BattleContext.SuppressCostDepth`。
                    //    ⚠️ 单位**没有**灵魂石能力时照旧如实报（不静默）—— 它就是「身上没有」。
                    if (kw == KeywordTable.SpiritStone)
                    {
                        if (t.Card != null && t.Card.SpiritOps != null && t.Card.SpiritOps.Count > 0)
                        {
                            ResolveSpiritAbilityForced(ctx, owner, t);
                            any = true;
                            continue;
                        }
                        ctx.Log($"{t.Name} 身上没有灵魂石能力（`N [Spirit Stone]:`）—— 「{op.Source}」在它身上空过");
                        unresolved.Add(op.Source + "（" + t.Name + " 没有 spiritstone）");
                        continue;
                    }
                    // 🆕 2026-10-14（账 `A1874`）：**祈祷（`triggerPray`）**。
                    // 🔴 **原版这一档【不是】「跑目标身上那条 `Pray:` 正文」** —— 别让它掉进下面那条通用路。
                    //   判据（全量反编译，逐句读过）：`chosenAbility = 462` 的真身 =
                    //   `AbilityLogic__PlayAbility.c:1838` `case 0x1ce:` →
                    //   `BattleManager__AddTriggerPray` → `BattleManager__ResolveTriggerPray.c:266`
                    //   `EntityScript__set_isPraying(target, 1)` ＋ `:305` `BroadcastUnitPray`
                    //   ⇒ **让目标进入「正在祈祷」那一刻**，不结算任何 `Pray:` 正文。
                    //   （读这个状态的是 `SOR70 Sororitas Rhino`「At the end of your turn,
                    //     if any friendly unit is Praying, deploy a Sacred Rose Sister」那一类。）
                    //   ⚠️ 掉进通用路的后果是**静默打错东西**：会把目标卡自己的 `Pray: …` 正文
                    //     （`Deal 1 damage` / `Deploy a Celestian` …）当成「祈祷的响应」再跑一遍。
                    //   ⚠️ 与 `RuleCore.UseAlternative(…, "pray")` **同一条语义**（那边也置 `Prayed`
                    //     并发 `WhenEventKind.Prays`）—— 两条路各写一份？不是：这里发的是
                    //     **「被外部触发」**那一支，`UseAlternative` 是**玩家/AI 主动激活**那一支，
                    //     原版也是两个入口（`ResolveTriggerPray` vs 主动能力那条路）。
                    if (kw == KeywordTable.Pray)
                    {
                        if (t.Card == null) continue;
                        int towner = RuleCore.OwnerOf(ctx, t);
                        if (!t.Prayed)
                        {
                            t.Prayed = true;      // = 原版 `EntityScript.set_isPraying(t, 1)`
                            RuleCore.BroadcastWhen(ctx, WhenEventKind.Prays, towner, t.Card, t);
                            ctx.Log($"{t.Name} 进入「正在祈祷」（`{op.Source}`）");
                        }
                        else
                        {
                            ctx.Log($"{t.Name} **本来就在祈祷**（`{op.Source}` 这一下是重复的，不重复广播）");
                        }
                        any = true;
                        continue;
                    }
                    if (RuleCore.TriggerKeywordOf(ctx, t, kw, forced: true)) { any = true; continue; }
                    ctx.Log($"{t.Name} 身上没有 `{kw}` 的正文/效果 —— 「{op.Source}」在它身上空过");
                    unresolved.Add(op.Source + "（" + t.Name + " 没有 " + kw + "）");
                }
                if (any) n++;
            }
            if (n == 0) return false;
            ctx.Log($"{by}：「{op.Source}」强行触发了 {n} 个单位的 `{op.Payload}`");
            return true;
        }

        /// <summary>
        /// `&lt;效果&gt; when &lt;事件&gt;`（**后缀形**）的**标记 op** —— 见 `EffectText.TryWhenSuffix`
        /// （`A1461` / `A1575`，2026-10-12）。
        ///
        /// 和 <see cref="DoCostWhenStub"/> 同一条纪律：这句话的机制**在别处** ——
        /// 效果那半在**卡表加载时**就注册成事件监听器了（`CardDef.AddWhenTrigger` 的 ①-c 支 →
        /// `WhenTriggers`），事件真的发生时由 `BroadcastWhen` → `FireWhen` → `ResolveOps` 结算。
        /// 所以打出去这一步**没有可做的事**；它存在的唯一目的是让整句**解析得出来**
        /// （`IsFullyParsed` 的三处消费点）并**挡住 `TryGain` 的错解**（那会把标记光发给敌人）。
        /// ⚠️ **仍然打一行日志**（红线：不许静默失败）—— 不然「本来就不该做什么」和
        ///    「忘了实现」在画面上长得一样。
        /// </summary>
        static bool DoWhenClauseStub(BattleContext ctx, int owner, string by, EffectOp op)
        {
            ctx.Log($"{by}：「{op.Source}」是**事件触发式**正文（效果那半 = 「{op.Payload}」）—— "
                  + "它在卡表加载时就注册成监听器了，事件真的发生时由事件层结算（这行是说明，不是失败）");
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
        /// 语义照 `rule_core.gd:2912-2931`（一字不差，⚠️ **我们上一版 Godot 复刻、旁证、非权威**；
        /// 🔴 `D26` 2026-10-19：**判据查不到** —— `Reload the Duty abilities` 这一句是**卡面文本**，
        /// 原版把它编码成 ability 数据；`DutyUsed` 在原版对应的那个「本局用过」位（`+0x50` 一带）
        /// **没有**能一次指全的方法可引 ⇒ 只剩卡面原文可核）：`all your …` → 己方所有带 duty 的单位；
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
        ///   · `&lt;兵种词>` —— `all Vehicles` / `all Beasts` / `troops` / `cards`
        ///   · `&lt;卡名>` —— `Tyrnak and Fenrir`
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
            // 🔴 **2026-09-18 第 7 行第 3 步：同下标存「哪一份」**（`null` = 这一项钉不住实例）。
            //    钉得住时登记成 `Key="*" + HandInstanceId` ⇒ **只降那一份**；
            //    钉不住（卡池里凭空指名的卡 / `this card`）才退回老的「按卡 id 匹配全部副本」。
            var pins = new List<CardInstance>();
            string detail;

            if (op.Payload == "(指代上一张)")
            {
                // 指代对象有两个来源，**先看刚造出来的、再看刚抽到的**：
                //   · `Create three … in your hand. They cost 1 less` → `ctx.LastCreated`
                //   · `Draw 3 cards and lower their cost by 3`（DarkAngels `Convoke the Circle`）、
                //     `Draw it and lower its cost by 3`（TauEmpire `Emergency Dispensation`）
                //     → `ctx.DrawnThisResolve`（「本次结算抽到的牌」）
                // ⚠️ 两个来源都按**一张卡一个窗口**清（`DrawnThisResolve` 与 `LastCreated`
                //    **都在** `ResolveOps` 入口清）⇒ 读不到上一张卡的陈旧值。
                //    🔴 **2026-10-18（`A976`）就地订正（铁律 5）**：这一句原来写的是
                //      「`LastCreated` 在 `DoCreate` 入口清」，**那是错的** ——
                //      `DoCreate` 只在**这一张卡真的有 `create` 那一条 op** 时才跑；
                //      上一张卡造过牌、这一张只 `draw` ⇒ 那个槽**一直带着上一张卡的值**
                //      （症状：`draw` 那张该降费，实际降的是上一张造出来的那张）。
                //      修法 = 清点补在 `ResolveOps` 入口（与 `DrawnThisResolve` 并排），
                //      见 `ResolveOps` 开头那一段。
                //    只在 `LastCreated` 为空时才退到抽牌那一栏 —— 严格比原来更宽，不会改既有行为。
                var refs = ctx.LastCreated.Count > 0 ? ctx.LastCreated : ctx.DrawnThisResolve;
                if (refs.Count == 0)
                {
                    ctx.Log($"{by}：「{op.Source}」要降费，但前面没有可指代的卡"
                          + "（既没「刚造出来的」也没「刚抽到的」）—— **这条没生效**");
                    unresolved.Add(op.Source + "（it/they 没有指代对象）");
                    return false;
                }
                // 🔴 第 7 行第 3 步：**指代槽现在装的是实例**（`LastCreated` / `DrawnThisResolve`）
                //    ⇒ `They cost 1 less` 直接钉在**那几份**上。这就是一号验收靶
                //    （`Master of Manoeuvre` 的 `It costs 4 less`）走的那条路。
                foreach (var c in refs) { keys.Add(c.Card.Id); shown.Add(c.Card.Name); cardRefs.Add(c.Card); pins.Add(c); }
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
                // `… a random troop **in hand** by 1`（`Living Icon` 那句嵌在 `Your Warlord gains "…"`
                // 里，写的是短一截的 `in hand`）—— 不认这一条的话 `t` 会剩成
                // `random troop in hand`，照样查不到。⚠️ 放在 `in your hand` **之后**判，
                // 两条都能命中时以长的为准。
                else if (t.Contains(" in hand")) { t = t.Replace(" in hand", ""); inDeck = false; }
                if (t.StartsWith("all ")) t = t.Substring(4).Trim();

                // 🔴 第 7 行第 3 步：池子里装**实例**（「降哪一份」要落到具体那一份上）——
                //    长度与顺序和改之前那份 `List<CardDef>` **完全一致**（手牌在前、牌库在后，逐份展开）。
                var pool = new List<CardInstance>();
                if (inHand) pool.AddRange(ps.Hand);
                if (inDeck) pool.AddRange(ps.Deck);

                string where = inHand && inDeck ? "手牌与牌库" : (inHand ? "手牌" : "牌库");
                if (t == "this card" || t == "this")
                {
                    // 🆕 `This card costs 1 less for each <X>`（2026-09-14 A5 收尾扫到）——
                    //   `Unholy Smite`（`for each Dark Pact on friendly units`）·
                    //   `Fenrisian Wolfpack`（`for each Hunt Mark on enemy troops`）。
                    //   「谁降价」= **本卡自己**。原样送下去的话 `IsKindWord("this card")` 认不出、
                    //   `FindByName("this card")` 也找不到 ⇒ 报「没生效」——
                    //   **和 `a random Infantry` 是同一类静默失效**（载荷里的虚词没被认出来）。
                    //   ⚠️ `ctx.PlayingCard` 由 `ResolveOps` 在入口写；单位触发那条路写的是 `ActingUnit`。
                    var self = ctx.ActingUnit != null ? ctx.ActingUnit.Card : ctx.PlayingCard;
                    if (self == null && ctx.PlayingCard != null) self = ctx.PlayingCard;
                    if (self == null)
                    {
                        ctx.Log($"{by}：「{op.Source}」要给「本卡」降费，但结算时认不出是哪一张 —— **这条没生效**");
                        unresolved.Add(op.Source + "（`this card` 认不出是哪张）");
                        return false;
                    }
                    // 🔴 **2026-10-23（`W64 B-10`）**：能钉住**这一份**就钉（不再一律 `null` 退回
                    //   「按卡 id 匹配全部副本」）。判据 = 原版费用修正是挂在**那张牌**上的
                    //   `CardEffect.costChange`（`CardScript__AddEffect.c:403`、`EntityScript.CurrentCost`）
                    //   ⇒ 按**实例**，不是按定义。
                    //   ① 单位能力那条路：`ctx.ActingUnit.Instance` 就是它；
                    //   ② 战术卡那条路**仍钉不住**：`ctx.PlayingCard` 只存 `CardDef`
                    //      （`BattleContext.PlayingCard` 的 doc 明写「卡牌定义」），而打出的那一份
                    //      已经从手牌挪走 ⇒ 退到按 id 匹配（如实留着，补齐要给 `ctx` 加
                    //      「正在结算的那一份」，属另一笔）。
                    CardInstance selfPin = ctx.ActingUnit != null ? ctx.ActingUnit.Instance : null;
                    keys.Add(self.Id); shown.Add(self.Name); cardRefs.Add(self); pins.Add(selfPin);
                    detail = selfPin != null ? "**本卡自己**（已钉到这一份）" : "**本卡自己**";
                }
                else if (t == "cards" || t == "card" || t.Length == 0)
                {
                    foreach (var c in pool) { keys.Add(c.Card.Id); shown.Add(c.Card.Name); cardRefs.Add(c.Card); pins.Add(c); }
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
                                if (CreatePool.MatchesKind(c.Card, p2))
                                { keys.Add(c.Card.Id); shown.Add(c.Card.Name); cardRefs.Add(c.Card); pins.Add(c); }
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
                            // 钉一份：优先**池子里**（手牌/牌库）那一份；池子里没有就不钉（退回按卡 id 匹配）
                            CardInstance hit = null;
                            foreach (var h in pool) if (ReferenceEquals(h.Card, card)) { hit = h; break; }
                            pins.Add(hit);
                        }
                    }
                    // 🔴 `a random Infantry` = **随机挑一张**，不是「手牌里所有 Infantry 一起降」。
                    //    卡面那个单数 `a` 与 `random` 是语义（解析层已剥掉并把标记记在 `op.PickOne`，
                    //    见 `EffectOp.PickOne`）。不这一刀的话，四张卡会静默地降**全部**同类牌。
                    //    ⚠️ 四个列表（`keys`/`shown`/`cardRefs`/`pins`）是**同下标平行**追加的，
                    //       整个换掉时四份要一起换，不然会和 `keys` 错位。
                    if (op.PickOne && keys.Count > 1)
                    {
                        int k = ctx.GetRandomInt(0, keys.Count);
                        string keepId = keys[k], keepName = shown[k];
                        var keepCard = cardRefs[k];
                        var keepPin = pins[k];
                        keys.Clear(); shown.Clear(); cardRefs.Clear(); pins.Clear();
                        keys.Add(keepId); shown.Add(keepName); cardRefs.Add(keepCard); pins.Add(keepPin);
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
                var pin = i < pins.Count ? pins[i] : null;
                // ---- 「**设为** N 费」（`reduce its cost to 1`）----
                // 🔴 **2026-10-25（`K8` · 铁律 5 就地订正）：不再折成【快照 delta】**。
                //    旧口径在这儿算 `d = N − 当时现价` 再加一条 delta —— 那会把「**排在 `setCost` 之前**的
                //    `changeCost`」吃掉（原版 `get_CurrentCost.c:52-79` 是 `clamp0(N + Σ 全部 changeCost)`，
                //    见 `CostMod.SetTo` 的 doc）。⇒ 现在落一条 **`SetTo = N`** 的记录，`CostOf` 每次现算。
                //    ⚠️ `>= 0`（不是 `> 0`）—— `Your next Stratagem this turn costs **0**` 那一支就是
                //       「变成 0 费」；哨兵是 `-1`，见 `EffectOp.CostSetTo` 的注释。
                bool isSet = op.CostSetTo >= 0 && i < cardRefs.Count;
                ctx.CostMods.Add(new CostMod
                {
                    Player = owner,
                    // 🔴 第 7 行第 3 步：**钉得住份就钉份**（`Key="*"` + `HandInstanceId`）——
                    //    这样「只降这一张复制品」才成立；钉不住才退回按卡 id 匹配全部副本。
                    Key = pin != null ? "*" : keys[i],
                    HandInstanceId = pin != null ? pin.Id : 0,
                    Delta = isSet ? 0 : d,
                    SetTo = isSet ? op.CostSetTo : -1,
                    ExpireTurn = expire,
                    Once = op.NextOnly,
                });
            }

            string what = op.CostSetTo >= 0 ? $"设为 {op.CostSetTo} 费" : $"每张 {delta} 费";
            ctx.Log($"{by}：「{op.Source}」{detail} {what}" + (expire >= 0 ? "（本回合）" : "（永久）")
                  + $"，登记 {keys.Count} 张：{string.Join("、", shown.ToArray())}");
            return true;
        }

        /// <summary>
        /// `repeat` —— **把本句之前的效果原样再来一遍**。
        ///
        /// 语义出处（🔴 `D26` 2026-10-19：**判据查不到** —— `Repeat this effect` 是卡面文本，
        /// 原版把它编码成 ability 数据（同一条 ability 再来一遍），**没有**「把本句之前的效果
        /// 原样重放」这种文本层做法 ⇒ 本版反编译里没有对应的方法可指；
        /// 原来此处写「语义出处：`rule_core.gd:2542` → `_resolve_repeat`」—— 那是**我们上一版复刻**
        /// （**旁证、非权威**））；变体有四种（全在实测数据里）：
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
        /// 控制者回合结束时被摧毁」。所以「Reanimate」= 把自己那个残骸**变回部队**。
        ///
        /// 🔴 **2026-09-25 更正 —— 下面这段「我们简化了什么」已经过期，我方的做法就是原版的做法。**
        /// 【读】原版：残骸**是留在原格位的那同一张卡**（`EntityScript.isRemnant // 0x65 = true`、
        ///    `cardState == inPlay(2)`、1 血、攻 0），外观是盖上去的 `RemnantBody3D &lt;阵营>` prefab，
        ///    **不是「翻面」**（全量签名桩 grep 不到 flip/faceDown；`ShowCardBack` 只给手牌用）。
        ///    ⇒ 「翻面」只是那份**粉丝实体规则书**的说法（它要把数字版改成纸牌，用了 creative interpretation）。
        /// 【我们】`UnitState.IsRemnant` + 留在 `Board[slot]` + `RuleCore` 的回合末 `DestroyRemnants` ——
        ///    **与原版同构**，**没有简化**。别照那段旧注释去"修"。
        /// ⚠️ ~~真正的缺口**不在这里**，在两处：① 场上**看不见**哪张是残骸（表现层没接线，`CardPresentation/` 里
        ///    `IsRemnant` 0 命中）② 灵族的 **`clickWaystone` 主动收集**没做。~~ 判据 → `资料/查证_useWaystone_语义.md` §六。
        ///    ✅ **2026-10-04 更正：这两条都早已做完，上面那句「① `IsRemnant` 0 命中 / ② 主动收集没做」不成立。**
        ///    ① 表现层接线了 —— `CardView.SetRemnantBody`（`CardPresentation/Core/CardView.cs` 的 `SetRemnantBody`）
        ///       + `BattleDriver.RemnantPrefabOf`；
        ///       ⇒ 现在 `CardPresentation/` 里 `IsRemnant` **11 处命中**。
        ///    ② 主动收集 2026-09-25 做了 —— `RuleCore.CanCollectWaystone` / `CollectWaystone`，
        ///       点它那一处在 `BattleDriver.cs` 里那处 `RuleCore.CanCollectWaystone`（AI 那一支同文件）。判据 → 同上 §六。
        ///
        /// ⚠️ **墓地退路走 `DeployFree`**（不另写一份放牌逻辑）—— 那一份管了找空格、发 `Deploy` 事件、
        ///    以及回合态（`ApplyDeployTurnState` / 攻击类型重算 / 光环重算）。
        ///    🔴 **2026-10-24 就地订正（引擎线 `W12` · 铁律 5）**：本段原来还写着它「广播 `Deploy` 监听器、
        ///    以及 `ResolveDeploy`」—— **已不成立**：`DeployFreeCore` 现在按 `DeploySource.Reanimate`
        ///    **整组跳过**部署广播（`When deployed` 195 / `When you deploy …` 190 / 常驻部署效果 / `UseFaith` 605），
        ///    判据 = 原版 `…DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31` 的清单里
        ///    **没有 `ResolveUnitSummoned` / `BroadcastUnitSummoned`**（逐条写在 `DeployFreeCore` 那段长注释）。
        ///    ⚠️ 残骸那条退路（候选 ①）**本来就不发**这些；现在两条退路的输出面对齐。
        ///    两条退路都额外**再广播一条 `Reanimated`**：卡面 `When Reanimated, …`（4 张 Sautekh 单位）等的是它。
        ///    断言 = `Editor/RuleEngineTest_W12.cs` 的 `W12ReanimateSkipsDeployBroadcast`。
        /// </summary>
        static bool DoReanimate(BattleContext ctx, int owner, string by, EffectOp op, List<string> unresolved)
        {
            var ps = ctx.Players[owner];
            // 🔴 **2026-10-11（`A1465`）：单复数**不再由「目标解不解得出来」推 ——
            //    判据改成**卡面原文**，见 `ReanimatesAll` 的 doc。
            bool all = ReanimatesAll(ctx, by, op);   // `Reanimate all friendly Remnants`

            // ---- 候选 ① = **自己场上的残骸**（2026-09-13 A2 起走这条）----
            // 规则书 `:203` 的「翻面」：残骸**留在场上**（原版场上是一个 3D 体，
            // `BattleCardUI.CreateRemnantBody`），而卡面**全写** `Reanimate a friendly Remnant`
            // （那 16 张里 15 张是这个写法）⇒ 从场上翻回来才对。「随机」走 `ctx.GetRandomInt`（2026-10-27 `E1RNG`；原 `ctx.Rng`）。
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
                    int k = ctx.GetRandomInt(0, order.Count);
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
                    // 🔴 第 7 行第 1 步：翻回来**也是那一张牌**（残骸就是它翻的面）
                    //    ⇒ **原地**翻（原版同一个 `CardScript` 对象）—— 见 `ReanimateRemnantInPlace`。
                    var back = ReanimateRemnantInPlace(rem);
                    back.DeployedTurn = ctx.Turn;   // 🆕 誓约能力的「本回合部署」判据（同上）
                    // 🔴 2026-10-21（引擎线 `T03·c` 批 A · `W59`）：**残骸翻回来 = 重新进场** ⇒ 取**新号**。
                    //   判据 = 原版「死掉 `List.Remove`、之后复活/再造 `List.Add` 到**表尾**」
                    //   （`_ResolveMinionDeath_d__454__MoveNext.c:56-59` 与
                    //     `_ResolveSummonUnit_d__510__MoveNext.c:179-183`；总述见 `UnitState.PlayOrder` 的 doc）
                    //   ⇒ 翻回来的那个单位在「谁先响」上要排到**所有现存在场者之后**。
                    back.PlayOrder = ctx.TakePlayOrder();
                    ps.Board[slot] = back;
                    Auras.Recompose(ctx);      // 🆕 A7：棋盘变动 ⇒ 光环重算
                    // 🔴 **2026-10-17（F9）：部署豁免在【第三个入口】上也要重算一次** ——
                    //    与 `RuleCore.PlayCard:1247` / `RuleCore.DeployFree:1588` 那两次**同一个理由、
                    //    同一份判据**（⛔ 别在这边另写一份）：上面 `ReanimateRemnantInPlace` 的
                    //    `Exhausted` 是**构造时**按**卡模板**关键词算的快照（= `UnitState` 构造里那句 `Exhausted = !RuleCore.HasDeployExemption(this)`），
                    //    而光环（上面那次 `Auras.Recompose` → `Aura` 的 `AddAuraKeyword`）
                    //    是在**那之后**才可能把 `fast`/`flank`/`ferocity` 挂上来的
                    //    ⇒ 不重算就是「**光环给了侧翼、翻回来的单位却动不了**」（静默错；
                    //    改前离屏实测 `Has(flank)=True · Exhausted=True`，见 `资料/普查产出_1017/F9_第三入口.md`）。
                    //    ⚠️ **只重算刚翻回来的这一个 `back`** —— ⛔ 别遍历全场：本回合**已经行动过**的单位
                    //      也是 `Exhausted = true`，一律解掉会把它们放活（一回合动两次，同样是静默错）。
                    //    ⚠️ 位置与另外两个入口**逐字对齐**：光环重算 → **这次重算** → 之后才
                    //       `ctx.Log` / 广播 `When Reanimated, …` 那些后续。
                    //    🔴 **2026-10-18（第三会话 · `A992` 遗留乙）：这里原来只写一位**
                    //       （`back.Exhausted = !RuleCore.HasDeployExemption(back);`）——
                    //       漏了原版第二个位 `canAttack`（`+0x23C`）⇒ 翻回来的 `ferocity` / `oath` 单位
                    //       **当回合仍然能攻击**，而它们按判据只该「能动、不能攻击」
                    //       （`CardScript__ActivateTraitsOnSummonOrEnchantment.c:86-99` 把 `canAttack` 压假，
                    //       见 `RuleCore.HasAttackDeployExemption` / `ApplyDeployTurnState` 上方那段）。
                    //       ⇒ 改调那两个入口**现成的那一份**（`RuleCore.PlayCard:1568` /
                    //       `RuleCore.DeployFree` 走的是同一个函数）—— ⛔ 别在这儿再手写一位。
                    RuleCore.ApplyDeployTurnState(back);
                    ctx.Log($"{by}：「{op.Source}」把 {rem.Name} 从**残骸**翻回来（槽 {slot}）");
                    // 🆕 **2026-10-19（战斗线 B3 的接口）：发一条 `Deploy`，来源 = `Reanimate`。**
                    //   🔴 **这是本批次【新加】的事件**（原来这一支**一条 `Deploy` 都不发** ⇒ 表现层
                    //      完全不知道有个单位落到了这一格上；`DoReanimate` 的 doc 说它「走 `DeployFree`」
                    //      只对**下面那条墓地退路**成立）。加它的理由：`EvtKind.Deploy` 的语义本来就是
                    //      「**单位落到格位上**」（见 `BattleEvent.Deploy` 的注），再造正是这件事；
                    //      而 B3 的消费约定要「再造 ⇒ 只响落地两条」，没有事件就演不了。
                    //   ⚠️ **只发事件、不发 `When deployed` 广播**（本类的 `BroadcastWhen(Deploy)` /
                    //      `ResolveDeploy` 那两跳照旧**不跑**）—— 加广播会多触发一批卡面效果，那是
                    //      **另一条账**，不在本接口里。
                    //   ⚠️ 位置：`ApplyDeployTurnState` 之后（与另外两个部署入口的次序一致：
                    //      落位 → 光环/回合位 → 记日志 → 发事件）。
                    ctx.Emit(EvtKind.Deploy, owner, slot, rem.Name, deployFrom: DeploySource.Reanimate,
                             instanceId: rem.InstanceId);
                    // `When Reanimated, …` —— 和 `DeployFree` 那条路发同一种广播
                    // （⚠️ 那个重载是 `BroadcastWhen(ctx, kind, owner, card, unit)`：卡 + 场上单位）
                    BroadcastWhen(ctx, WhenEventKind.Reanimated, owner, rem.Card, ps.Board[slot]);
                    // 🆕 **2026-10-25（执行代理 `X18` · 第二批）**：成就 `Ach_14`（`UnitReanimated`）计数 ——
                    //   **每翻回一个 +1**（不是「一次效果 +1」）。归属 = 施放者 `owner`。
                    //   ⚠️ 原版 `UnitReanimated__OnUnitReanimated.c` 那格 `unit+0x40 != 0` 的过滤
                    //   语义**没解出来**（反编译产物撞名）⇒ 我们按「翻了就算」，如实标为近似（判据 §B-3 #15）。
                    ctx.Tally.AddReanimated(owner);
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
                // 🆕 2026-10-19（战斗线 B3 的接口）：墓地退路也标成 `Reanimate` ——
                //   `DeployFree` 默认按 `fromHand:false` 会写成 `Summon`，而这一支语义上是**再造**。
                if (!DeployFree(ctx, owner, card, out slot, deploySource: DeploySource.Reanimate))
                    break;   // 满场 → 后面的也放不下，停
                ctx.TakeFromGraveyard(owner, card);                   // 翻回来了，就从墓地/弃牌堆里拿走
                ctx.Log($"{by}：「{op.Source}」把 {card.Name} 从墓地翻回来（槽 {slot}）");
                BroadcastWhen(ctx, WhenEventKind.Reanimated, owner, card, ps.Board[slot]);
                // 🆕 同上（`Ach_14`）：**墓地退路**也逐单位 +1 —— 两条支各写一次，
                //   因为它们是同一语义的两个入口（⛔ 别只在场上那一支写，那会漏掉旧口径那 16 张卡）。
                ctx.Tally.AddReanimated(owner);
                done++;
            }
            return done > 0;
        }

        /// <summary>
        /// 🔴 **2026-10-11（第十四会话 · `A1465`）：这条 `reanimate` 该翻【一个】还是【全部】残骸。**
        ///
        /// **原来怎么错的**：`DoReanimate` 开头原来是
        ///   `bool all = op.Target == null || op.Target.Count == 0;`
        ///   —— 而 `ParseTarget("a friendly remnant")` 与 `ParseTarget("all friendly remnants")`
        ///   **都返回 `null`**（`EffectText.cs` 那条「不认识的词 → **不猜**」的守卫：
        ///   `remnant` 既不是 kind 词、也不是池里某张卡的名字）⇒ **`Target == null` 分不出单复数**，
        ///   于是**单数卡面被当成「全体」**：`Reanimate a friendly Remnant` 会把场上**全部**残骸翻回来。
        ///   ⇒ 这是**静默放宽**（解不出 ⇒ 放大），不是「没查到」。
        ///
        /// **判据 = 卡面原文**（`op.Source`；铁律 7「拿不准就看卡面」）。全池 `desc`/`descZh`
        ///   提到 `reanimate` 的共 **16 张**（`cards_engine.json` 逐张扫过），其中**只有 1 张**写复数：
        ///     · `SAU52 Resurrection Orb` = `Reanimate all friendly Remnants`（**唯一**「全体」）
        ///     · 其余 15 张一律 `Reanimate a friendly Remnant` / `a random friendly Remnant`
        ///       （`SAU26` · `SAU46` · `SAU67` · `SAU49` · `SAU25` · `SAU27` · `SAU16` …）
        ///   ⚠️ **只在 `reanimat… → remnant` 这一截里找 `all`** —— ⛔ 别拿整句 `Contains("all")`：
        ///      尾句（如 `… and deal 1 damage to all enemies`）里的 `all` 会被误认成**目标**。
        ///   ⚠️ **认不出时按【单数】**（`Target` 解不出、或 `Source` 里根本没有这一段）：
        ///      卡面 16 张里 15 张是单数，而「解不出就放宽成全体」**正是本笔要治的病**；
        ///      这两格**都打日志出声**（本工程红线：不许静默失败）。
        ///
        /// ⚠️ **`Target != null` 时照旧按 `Count == 0` 判**（那条「全部单位」的写法走的是它），
        ///   本方法**只在 `Target == null` 这一支上改了行为**。
        /// </summary>
        static bool ReanimatesAll(BattleContext ctx, string by, EffectOp op)
        {
            if (op.Target != null) return op.Target.Count == 0;     // 解析得出来 ⇒ 照旧，一字不变
            if (string.IsNullOrEmpty(op.Source))
            {
                ctx.Log($"{by}：这条「复活残骸」**没有目标、正文也不在手上** —— 按卡面"
                      + "（单数，一个残骸）处理，**不放大成「全体」**");
                return false;
            }
            var m = System.Text.RegularExpressions.Regex.Match(
                op.Source, @"reanimates?\s+(.*?remnants?)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                ctx.Log($"{by}：「{op.Source}」里认不出「reanimate … Remnant」这个目标段 —— "
                      + "按卡面（单数，一个残骸）处理，**不放大成「全体」**");
                return false;
            }
            return System.Text.RegularExpressions.Regex.IsMatch(
                m.Groups[1].Value, @"\ball\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
                // 🔴 **2026-10-10（第十四会话 · `A1415`）：重复的那批 op **一个钱都不再收**。**
                //
                // **改前怎么错的**：这里对每条 `o` **直接调 `ResolveOne`**，而付费是在
                //   `ResolveOneCore` 里**逐 op** 判的 ⇒ 若 `RepeatOps` 里有一条
                //   `Cost > 0 && !CostShared`，钱会**再扣一遍**。
                //   **形状**：`2 Energy: Deal 2 damage to an enemy. Repeat this effect` ⇒
                //   前半句那 2 点能量付过一次，`repeat` 又付一次 ⇒ **一次激活收 4 点**。
                //
                // **判据（为什么不再收）**：原版收费在 **ability 层** —— 一条 ability = 一个
                //   `manaCost` ＋ 一串 `AbilityLogic`（`ActiveAbility.cs`），一次激活只调一次
                //   `PlayerManager.UseMana`（`d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22`）；
                //   `Repeat this effect` 重复的是**那串 logic**，**不回到收费那一层**
                //   ⇒ 与 `for each` / `N times` 的重复（`:296-321` 那一段，钱只在第 1 遍收）
                //   **同一条口径、同一个工具**。
                //
                // ⚠️ **递副本**：`Prepaid(o)`（`A1412` 之前的写法是 `RoutePay(o, false)`，
                //   同一个意思）只在「这条要付钱」时才 `Clone()`（`o.Cost <= 0` 或
                //   `o.CostShared` 已经为真 ⇒ **递原件**，主路零拷贝）
                //   —— ⛔ **绝不写回 `op.CostShared`**（`EffectOp` 列表是解析产物、会被复用，
                //   见 `ResolveOps` 里 `op.Amount` 那条注释）。
                // ⚠️ **可观测性**：全池**今天 0 处可达** —— 本笔用**活的工作区解析器**逐条跑过
                //   `cards_engine.json` 的 1126 张（desc ＋ keywords 共 2147 条文本）：
                //   `repeat` op 共 **16** 个，其 `RepeatOps` 里带 `Cost > 0 && !CostShared` 的
                //   **0 个**（带价的 7 处 —— 6 张 `N [货币]:` ＋ `UM115` 的 `Oath 3:` ——
                //   价都贴在 `repeat` op **自己**身上，它重复的那条 `cost = 0`）
                //   ⇒ 本行今天不改任何真卡读数，是**堵形状**（`A1369` 报告 §五·2）。
                //   真读数见本笔报告的离线驱动。
                foreach (var o in op.RepeatOps)
                    if (ResolveOne(ctx, owner, null, by, Prepaid(o), chosen, unresolved)) done++;
            }
            finally { ctx.EffectChain--; }
            return done > 0;
        }

        /// <summary>某方场上**部队**（不含督军）的数量。
        /// <paramref name="exclude"/> 不为 null 时把它**自己**排掉（`If you control no other troops`）。</summary>
        static int CountTroops(BattleContext ctx, int owner, UnitState exclude = null)
        {
            int n = 0;
            var b = ctx.Players[owner].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (b[s] != null && !b[s].IsWarlord && b[s].IsAlive && !ReferenceEquals(b[s], exclude)) n++;
            return n;
        }

        /// <summary>从 `If it already had Hunt Mark` 里抠出关键词名；认不出返回 null</summary>
        static string ExtractKeywordFromCondition(string cond)
        {
            if (string.IsNullOrEmpty(cond)) return null;
            string c = cond.ToLowerInvariant();
            foreach (var kw in KeywordTable.Implemented)
                if (c.Contains(kw)) return kw;
            // 🔴 **2026-09-16 补：多词关键词的卡面写法** —— `Hunt Mark` 在 `KeywordTable` 里的
            //    规范名是 **`huntmark`（没有空格）**，而条件原文写的是 `hunt mark`
            //    ⇒ 上面那一轮 `c.Contains("huntmark")` **永远是假** ⇒
            //    `If it already had Hunt Mark, destroy it instead`（`SW53 Embers of Prospero`）
            //    **整条判不了**、「改为摧毁」从来不发生（而且卡面不打 `*`）。
            //    做法：把文里的空格去掉再比一次 —— 判据仍是**同一张 `Implemented` 表**，
            //    不另写一份词表（那正是两处迟早不一致的来源）。
            string squashed = c.Replace(" ", "");
            foreach (var kw in KeywordTable.Implemented)
                if (squashed.Contains(kw)) return kw;
            return null;
        }

        /// <summary>
        /// `If you control &lt;短语&gt;` —— 拆成「**要几个**」＋「**要什么**」。
        ///
        /// 卡面实测（2026-09-16 全池普查，`controlcount` 共 7 处）：
        ///   · `a Vehicle` / `a Beast` / `a Spanner or Mekboy Gazmek` —— 阈值 1；
        ///   · `3 or more troops`（`SW47`）—— 阈值 3。
        /// ⚠️ `no other troops` 那一族**不该到这儿**（`Normalize` 归的是 `ownnoothertroops`），
        ///    这里再排一次是**保险**：万一有人改了 `Normalize` 的顺序，别把它静默当成「≥1」。
        /// 拆不出来返回 **false = 判不了**（如实报，别猜）。
        /// </summary>
        static bool ParseControlAsk(string condition, out int need, out string[] alts)
        {
            need = 1; alts = null;
            if (string.IsNullOrEmpty(condition)) return false;
            string c = condition.ToLowerInvariant();
            int at = c.IndexOf("control");
            if (at < 0) return false;
            string rest = c.Substring(at + 7).Trim().TrimEnd('.', ' ');
            if (rest.Length == 0 || rest.StartsWith("no ") || rest.Contains("no other")) return false;

            var mNum = System.Text.RegularExpressions.Regex.Match(
                rest, @"^(\d+)\s+or\s+more\s+(?<tail>.+)$");
            if (mNum.Success)
            {
                need = int.Parse(mNum.Groups[1].Value);
                rest = mNum.Groups["tail"].Value.Trim();
            }

            var list = new System.Collections.Generic.List<string>();
            foreach (var seg in System.Text.RegularExpressions.Regex.Split(rest, @"\s+or\s+|,\s*"))
            {
                string w = System.Text.RegularExpressions.Regex
                    .Replace(seg.Trim(), @"^(?:a|an|the|any)\s+", "").Trim();
                if (w.Length > 0) list.Add(w);
            }
            if (list.Count == 0) return false;
            alts = list.ToArray();
            return true;
        }

        /// <summary>
        /// 这个单位算不算 `If you control &lt;短语&gt;` 里说的那种 —— **兵种词**（`Vehicle` / `Beast` /
        /// `troops`）或**卡名**（`Mekboy Gazmek`）。两个命名空间都要查，和
        /// `EffectText.IsKnownWord` 同一条口径（那边也是「关键词表 **或** 兵种表」）。
        /// </summary>
        static bool UnitMatchesControlAlt(UnitState u, string alt)
        {
            if (u == null || u.Card == null) return false;
            if (CreatePool.IsKnownKind(alt) && CreatePool.MatchesKind(u.Card, alt)) return true;
            return string.Equals(u.Card.Name, alt, System.StringComparison.OrdinalIgnoreCase);
        }

        // ==================================================================
        //  🆕 2026-10-26（完全复刻批次 3 · 执行代理 `E2`）：回手 / 回牌库 事件的**唯一发射口**
        // ==================================================================
        //  ⚠️ **追加在类尾**（本批纪律：新成员一律追加，别插进已有成员之间 —— 否则本文件
        //     下游所有账行号整体漂）。
        /// <summary>
        /// 一张牌被送回手牌 / 洗回牌库**之后**，按原版广播一次
        /// （`CardScript.CardReturnedHand` / `CardReturnedDeck` 那一族）。
        ///
        /// <para>判据（第一权威，逐条现读 `d:/2/tools/decomp_full/`）：
        ///   · `BattleManagerSupport__BroadcastCardReturnedHand.c:20-81` —— 四跳：
        ///     被送回那张自己 → **场上每张牌**（`+0x470`）→ **当前回合方**手牌 → **另一方**手牌
        ///     （最后一跳 `op_Inequality(item, 被送回那张)`）；
        ///   · `BattleManagerSupport__BroadcastCardReturnedDeck.c` 与它**逐句同形**；
        ///   · 接收端 `CardScript__CardReturnedHand.c:1-13` → `OnTrigger(0x1d6 = 470)`；
        ///     `CardScript__CardReturnedDeck.c:1-13` → `OnTrigger(0x1e0 = 480)`。
        /// ⇒ 我们的 `BroadcastWhen` 就是那四跳（自己 / 棋盘 / 手牌两跳 ⇒ 见 `WhenEvents.Matches`
        ///   与 `BroadcastWhenCore` 的 ⓪/⓪-c/⓪-d 三条手牌跳），所以这里只做两件事：
        ///   ① 选事件种类（手牌 / 牌库）；② 把「谁收下了这张牌」当 `who`。</para>
        ///
        /// <para>⚠️ **`subject` 故意传 `null`**：两个调用点都在「牌已经离开棋盘」之后
        /// （回手 / 洗回 / 反噬），它此时**不在** `SnapshotBoardListeners` 的候选里；传一个
        /// 已离场的 `UnitState` 只会让 `SelfOnly` 类判据拿到**假事实**。条件筛选走
        /// `Matches` 的 `card` 兜底分支（`ev.Criteria.Matches(card)`）。</para>
        ///
        /// <para>⚠️ **它不是 `EvtKind.Return` 的替代**：那条是给表现层播位移动画的
        /// （`DoReturn` 早就发），本条只叫醒「When &lt;被送回&gt;」的监听器。</para>
        /// <para>⚠️ **今天零可观测**：卡池 1,128 张里卡面写这类监听的 0 张（现读）
        /// ⇒ 这条是照铁律 11 补全的休眠机制；真出现卡面时**先核词形**（见
        /// `WhenEventKind.CardReturnedHand` 的 doc）。</para>
        /// </summary>
        /// <param name="who">**收下这张牌的那一方**（0/1）—— 相对极性判据看的就是它。</param>
        /// <param name="card">被挪动的那张牌（`subject` 传 `null` ⇒ 条件筛选靠它兜底）。</param>
        /// <param name="toHand">`true` = 回手牌；`false` = 回牌库（`deck` / `decktop` 都算）。</param>
        static void RecordCardReturnedEvent(BattleContext ctx, int who, CardDef card, bool toHand, string by)
        {
            if (ctx == null || card == null) return;
            BroadcastWhen(ctx, toHand ? WhenEventKind.CardReturnedHand : WhenEventKind.CardReturnedDeck,
                          who, card, null);
        }

        // ==========================================================================================
        //  🆕 2026-10-26（完全复刻批次 4 · 执行代理 `F2`）：
        //  三族「原版 `BattleManagerSupport.Broadcast*`」的等价广播器
        // ==========================================================================================
        //  🔴 **三条的跳数【各不相同】，⛔ 别照「三跳模板」套**（逐条现读 `d:/2/tools/decomp_full/`）：
        //   · `BroadcastUnitHealed.c:17-58`   棋盘（`:25` `IsInPlay` 过滤）→ 当前回合方手牌（`:33-42`）
        //                                     → 另一方手牌（`:43-56`）；`:30/:41/:53` 三处**都没有
        //                                     `op_Inequality`** ⇒ **无预调、无自排除**（治疗者自己也收得到）。
        //   · `BroadcastEarnedSkull.c:15-35`  **只有棋盘一跳**（`:26` `IsInPlay`）；**没有手牌两跳、没有预调**；
        //                                     入参是「刚跨过骷髅阈值的那张督军卡」
        //                                     （`BattleManager__BroadcastEarnSkull.c:1-20` 取该方英雄卡）。
        //   · `BroadcastUnitRequiem.c:19-70`  棋盘（`:27` `IsInPlay`）→ 手牌两跳（`:41-51` / `:50-63`）；
        //                                     自排除在**接收方体内**
        //                                     （`CardScript__TriggerOtherCardRequiem.c:14` 的 `op_Inequality`）
        //                                     ⇒ 我们由 `WhenEvents.SelfExcludesOnKeywordTrigger` 等价实现。
        //  ⚠️ **三条今天都零可观测**（现读 `cards_engine.json` 1,128 张：卡面写这三类监听的 0 张、
        //     `abilityTriggers` 含 70 / 640 / 435 的 0 张）⇒ 铁律 11 的休眠机制。
        //  ⚠️ `subject` 一律传「发生这件事的那个单位」的 `UnitState`（不是 `CardDef`）——
        //     `Matches` 的 `Criteria` 那一维要读**运行时**关键词
        //     （`CardCriteria.Matches(UnitState)`）；传 `null` 会让带关键词筛的监听器
        //     **恒判不中且不报错**（本仓红线里的静默失效，`WhenEvents.Matches` 那段注释点名过）。

        /// <summary>
        /// 一个单位**被治疗后**按原版广播（trigger **70** = `AbilityTrigger.UnitHealed`）。
        ///
        /// <para>**判据（逐行现读）**：接收端 `CardScript__ReactToUnitHealed.c:7`
        /// → `OnTrigger(0x46 = 70)`；广播器 `BattleManagerSupport__BroadcastUnitHealed.c:17-58`。
        /// 🔴 **原版唯一的生产调用点不是 `CardScript.Heal`** —— 是
        /// `CardScript__UseSurvivor.c:87`（幸存者把血写满之后那一跳；
        /// `CardScript__Heal.c` 与 `_Heal_d__468__MoveNext.c` 里 `BroadcastUnitHealed` **0 命中**）。
        /// ⇒ 我们的调用点 = `RuleCore.CleanupDeaths` 的幸存者支
        /// （`RuleCore.cs` 里 `FireTriggerAlways(…, KeywordTable.Survivor, …)` 之后那一行，
        /// 就是「原版 `:87`」的等价位置 —— 那边目前只发了一条负数 `Hit` 当标记）。
        /// ⚠️ **该调用点不在本代理白名单**（`RuleCore.cs` 属 `F1`）⇒ 已写改动请求，**这一半还没接**。</para>
        /// </summary>
        /// <param name="who">**被治疗那个单位的归属方**（0/1）—— 极性判据看的就是它。</param>
        /// <param name="healed">被治疗的那个单位（`subject` 传它 ⇒ 关键词/兵种筛读**运行时**状态）。</param>
        public static void BroadcastUnitHealed(BattleContext ctx, int who, UnitState healed)
        {
            if (ctx == null || healed == null) return;
            BroadcastWhen(ctx, WhenEventKind.UnitHealed, who, healed.Card,
                          subject: healed, actor: null, target: null);
        }

        /// <summary>
        /// 某一方**获得骷髅**之后按原版广播（trigger **640** = `AbilityTrigger.EarnedSkull`）。
        ///
        /// <para>**判据（逐行现读）**：接收端 `CardScript__TriggeredEarnSkull.c:7`
        /// → `OnTrigger(0x280 = 640)`；广播器 `BattleManagerSupport__BroadcastEarnedSkull.c:15-35`
        /// —— **只有棋盘一跳**（逐个 `IsInPlay`）。原版**没有**手牌两跳、**没有**预调,
        /// 也不再额外排除谁（`:15-35` 只有一条 `op_Inequality`，那是 `.c` 丢实参的噪声，机器码里是对 `null` 判）。
        /// ⇒ 本函数用 `boardAndPersistentOnly: true` 走 `BroadcastWhen`：
        /// 只保留**棋盘跳**与**常驻效果跳**（后者是「原版把这类效果做成挂在场上卡/督军上的附魔」
        /// 的等价物 —— `SAU53` 那条判据见 `报告/F2_第4轮.md` §四），
        /// ⛔ 不跑事件型降费 / 手牌陷阱 / 手牌单位那三跳（原版确实没有）。</para>
        ///
        /// <para>✅ **调用点（🔴 2026-10-26 批次 9 · 执行代理 `E9ER` 已接）**：原版在
        /// `CardScript.CheckForSkullEarned`（`CardScript__CheckForSkullEarned.c:1-69`：督军当前血跨过阈值
        /// ⇒ 把 `+0x354` 抬到新值、**只在变大时**广播一次）。我们的等价发射点 =
        /// <see cref="NoteWarlordHealthForSkulls"/>（每局附加状态里存「这张督军卡跨到第几档」），
        /// 由 `RuleCore` 的**两个督军血量写点**调：`Hurt` 的 `u.Health -= actual` 与牌库抽空的疲劳支。
        /// ⚠️ 计数按被打那个督军记、得主 = **对面**（`who = 1 - 被打督军的 owner`）—— 判据见那个方法的头注。</para>
        /// </summary>
        /// <param name="who">**获得骷髅那一方**（0/1）。</param>
        /// <param name="warlord">**刚跨过阈值的那张督军单位**（原版 `earnedWarlord` 实参）。</param>
        public static void BroadcastEarnedSkull(BattleContext ctx, int who, UnitState warlord)
        {
            if (ctx == null || warlord == null) return;
            BroadcastWhen(ctx, WhenEventKind.EarnedSkull, who, warlord.Card,
                          subject: warlord, actor: null, target: null,
                          boardAndPersistentOnly: true);
        }

        /// <summary>
        /// 某个单位**用了「安魂」（Requiem）**之后按原版广播（trigger **435** =
        /// `AbilityTrigger.OtherCardRequiem`）。
        ///
        /// <para>**判据（逐行现读）**：接收端 `CardScript__TriggerOtherCardRequiem.c:14/:20`
        /// → `OnTrigger(0x1b3 = 435)`，且 `:14` 的 `op_Inequality(param_2, param_1)`
        /// （触发源 ≠ 接收者自己）**在接收方体内** ⇒ 自排除由
        /// `WhenEvents.SelfExcludesOnKeywordTrigger` 那一处等价实现（⛔ 别在广播侧再排一次）。
        /// 广播器 `BattleManagerSupport__BroadcastUnitRequiem.c:19-70`：棋盘（`IsInPlay`）→ 双方手牌。</para>
        ///
        /// <para>⚠️ **430 ≠ 435**：430（`Requiem`）是「用安魂那张**自己**的正文」那一跳
        /// （`BattleManager__ResolveTriggerRequiem.c` 里先 `OnTrigger(0x1ae = 430)`），
        /// 435 才是「**别的**卡用了”。两条是两个 `OnTrigger`，⛔ 别合并。</para>
        ///
        /// <para>⚠️ **调用点还没接**：原版由 `ResolveTriggerRequiem`（死亡收尾那条）发；
        /// 我们没有 `Requiem` 关键词的正文 + 自触发那一半（`RuleCore` / `CardDef`，非本代理白名单）
        /// ⇒ 已写改动请求。⚠️ 本函数的 `actor` 传 `unitRequiem`：卡面写
        /// `When this unit uses Requiem, …` 时走 `WhenEvent.ActorSelf`（判「谁干的」）。</para>
        /// </summary>
        /// <param name="who">**用安魂那个单位的归属方**（0/1）。</param>
        /// <param name="unitRequiem">用了安魂的那个单位（= 原版 `unitRequiem` 实参，
        /// `TriggerOtherCardRequiem` 体内拿它做自排除）。</param>
        public static void BroadcastUnitRequiem(BattleContext ctx, int who, UnitState unitRequiem)
        {
            if (ctx == null || unitRequiem == null) return;
            BroadcastWhen(ctx, WhenEventKind.OtherCardRequiem, who, unitRequiem.Card,
                          subject: unitRequiem, actor: unitRequiem, target: null);
        }

        // ==========================================================================================
        //  🆕 2026-10-26（完全复刻批次 4 · 执行代理 `F2`）：**`Jam` 族的驱动侧**（`F3` 的 R3-F2 / R3-F2b）
        // ==========================================================================================
        //  ⚠️ **状态侧不在本文件**：清效果 / 摘 trait / 挂 jam / 压主动技能 = `UnitState.ApplyJam()`
        //     （`F3` 已建，判据 = `CardScript.ReactToUnitJammed` 的 ①②③④⑤）。
        //     本文件只负责原版 `BattleManager__ResolveJamUnit.c:1-321` 那五步的**顺序**：
        //       闸 `targetCard != null && IsInPlayOrDying(targetCard)`
        //       → `ReactToUnitJammed`（= `ApplyJam()` + 「别人」那一支 + 致死检查）
        //       → `BroadcastUnitJammed` → `CancelEffectsFromDisappearingUnit`
        //       → 两侧 `UpdateCardEffects` → `FinishResolvingAction`
        //  🔴 **顺序错就错**（`调度台` 2026-10-26 转述 `F3` 的判据）：摘 trait 必须在广播之前
        //     （监听者看到的是**已经 jam 过**的那张），致死检查紧跟在 `ApplyJam()` 之后。

        /// <summary>`Jam &lt;目标&gt;` 这个动词的结算入口（`EffectDispatch` 的 `"jam"` 那一行）。
        ///
        /// <para>**判据**：`F3` 已读全 `BattleManager__ResolveJamUnit.c:1-321`；正文见
        /// <see cref="ResolveJamUnit"/> 的 doc。这里只做两件事：
        /// ① 解目标（`TryJam` 那句卡面 → `EffectTargetSpec`）；② 逐个交给 `ResolveJamUnit`。</para>
        /// <para>⚠️ 目标解不出来 ⇒ 照本仓红线**出声**（`unresolved` + 日志），⛔ 不静默空过。</para>
        /// </summary>
        static bool DoJam(BattleContext ctx, int owner, string by, EffectOp op,
                          UnitState chosen, List<string> unresolved)
        {
            // `Jam` 没写目标 = 一个敌方单位（照 `DoStun` 的兜底：槽号最小的那个）。
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any",
                Count = op.Amount > 0 ? op.Amount : 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, op.TargetCondition, op);
            if (targets.Count == 0)
            {
                ctx.Log($"{by}：「{op.Source}」的 **Jam** 没有合法目标，空过");
                unresolved?.Add($"{op.Source}: Jam 没有合法目标");
                return false;
            }
            int n = 0;
            foreach (var t in targets)
                if (ResolveJamUnit(ctx, t, by)) n++;
            ctx.Log($"{by}：「{op.Source}」**Jam** 了 {n} 个单位");
            return true;
        }

        /// <summary>🆕 **2026-10-26（批次 4 · `F2`）**：`CardScript.IsInPlayOrDying` 的等价位。
        ///
        /// <para>**判据**：原版 `IsInPlayOrDying` = `cardState ∈ {2 inPlay, 3 inPlayAttacking, 15?, 17?}`
        /// 再加上 `waitingToDie(5)`。我们**没有 `cardState` 这一层**（本仓如实标着：视图不存卡状态），
        /// 能表达它的只有三件事：**还在棋盘上**（调用方给的就是棋盘上的引用）·
        /// `IsAlive`（`Health &gt; 0`）· **待死窗口**（`ctx.IsPendingDeath` 或
        /// `!EnoughPendingDamageToDie`，后者覆盖 `Health ≤ 0 但 Survivor ≥ 1` 的「还活着」那一档）。</para>
        /// <para>⚠️ 这三条**不是** `cardState` 的逐值等价物（`inPlayAttacking` 那种中间态我们没有）
        /// —— 如实标着；今天 `jam` 在池内 0 张，零可观测。</para>
        /// </summary>
        static bool IsInPlayOrDying(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return false;
            if (u.IsAlive) return true;
            if (ctx.IsPendingDeath(u)) return true;
            return !EnoughPendingDamageToDie(u);      // `Health ≤ 0` 但有 Survivor ⇒ 仍在场
        }

        /// <summary>🆕 **2026-10-26（批次 4 · `F2`）**：`BattleManager.ResolveJamUnit` 的五步驱动。
        ///
        /// <para>**判据（`F3` 读全 `decomp_full/BattleManager__ResolveJamUnit.c:1-321`，
        /// 状态侧真体 = `out_cpp2il_dev_ilspy/CardScript.cs:19980-20140`）**：
        /// ① 闸：`targetCard != null &amp;&amp; IsInPlayOrDying(targetCard)`，不满足 ⇒ **什么都不做**；
        /// ② `ReactToUnitJammed(被 jam 的那张, 被 jam 的那张)` —— **自己那一支**（`ApplyJam()`）；
        ///    同一次广播还会让**别人**走 else 支：`RemoveExtrinsicEffectsFrom(被 jam 那张)` + 重算常驻；
        /// ③ `BroadcastUnitJammed(被 jam 的那张)`；
        /// ④ `CancelEffectsFromDisappearingUnit`（原版取消「施放者已离场」的排队效果）；
        /// ⑤ 双方 `UpdateCardEffects`（我们 = 手牌效果重算 + 光环重算）；
        /// ⑥ `FinishResolvingAction`（我们由调用栈自然结束 — 本仓没有那个显式收尾口）。</para>
        ///
        /// <para>🔴 **谁被 jam**：`ApplyJam()` 的 ② 步会**摘掉全部 trait**（含卡面印的）——
        /// 这就是原版 `ReactToUnitJammed` 的 ② 支。⚠️ 它走的是 `RemoveAll` 的**非 `silent`** 形态
        /// （原版那一处是 `RemoveTrait`，会重算 `fast`/`flank` 的召唤病两位），
        /// ⛔ 别给它传 `silent: true`（那是另一族的两处调用点，见 `F3` 的 §2·4）。</para>
        ///
        /// <para>⚠️ **没做的两格（如实标）**：
        /// · `CancelEffectsFromDisappearingUnit` —— 我们的对应物是 `ActionPump` 的排队取消，
        ///   但它判的是「施放者不在场」，与 jam 无关；原版这一步在 jam 语境下要撤的是
        ///   「被 jam 那张已排队的被动/主动」⇒ 我们**没有这个口**（`ActionPump` 属 `F1`）
        ///   ⇒ 已写改动请求。
        /// · `UpdateCardEffects` 的**手牌那一半**：我们只做了「按来源摘 extrinsic 手牌效果」
        ///   （<see cref="RemoveHandEffectsFromSource"/>），没有原版那种整体重算 —— 增量等价。</para>
        ///
        /// <para>📊 **可达性**：池内 **0 张**卡能产出 `jam`（`desc` / `descZh` / `keywords` 三扫 0 命中）
        /// ⇒ 零可观测（铁律 11：休眠机制也做）。</para>
        /// </summary>
        /// <returns>`true` = 真的 jam 了；`false` = 被闸 ① 挡下（已经离场/不在棋盘）</returns>
        static bool ResolveJamUnit(BattleContext ctx, UnitState jammed, string by)
        {
            if (ctx == null || jammed == null) return false;

            // ---- ① 闸 ----
            if (!IsInPlayOrDying(ctx, jammed))
            {
                ctx.Log($"{by}：`Jam` 的目标「{jammed.Name}」已不在场（闸 `IsInPlayOrDying` 不成立）⇒ 什么都不做"
                      + "（原版 `BattleManager__ResolveJamUnit.c` 第一句）");
                return false;
            }

            int who = OwnerOf(ctx, jammed);

            // ---- ② 被 jam 那张自己（`ApplyJam()` = ReactToUnitJammed 的 ①②③④⑤）----
            //   返回值**不是**「成功没有」，而是 `Health <= 0`（原版第 ⑥ 步的前置）。
            bool dead = jammed.ApplyJam();
            ctx.Log($"{by}：**Jam** 「{jammed.Name}」（清掉全部效果 + 摘掉全部 trait + 挂 jam"
                  + (jammed.IsWarlord ? "；督军 ⇒ 主动技能照旧" : "；非督军 ⇒ 主动技能被压掉") + "）");

            // ---- ③ 「别人被 jam」：按来源撤 extrinsic（原版同一次广播的 else 支）----
            //   原版：`BroadcastUnitJammed` 对**场上每张牌 + 双方手牌**各调一次
            //   `ReactToUnitJammed(那张, 被 jam 的)`；`那张 != 被 jam 的` ⇒ else 支
            //   `RemoveExtrinsicEffectsFrom(被 jam 的) + UpdateWhileInPlay(false)`。
            //   ⚠️ **我们只能在效果记录层按「来源卡名」撤**（`F3` 已在 `UnitState` 里标清：
            //      `RecordGrant` 那条路没记施放者卡名）⇒ 与 `RemoveExtrinsicEffectsFrom` 同一条限制。
            int stripped = 0;
            for (int side = 0; side < 2; side++)
            {
                var board = ctx.Players[side].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || ReferenceEquals(u, jammed)) continue;
                    stripped += u.RemoveExtrinsicEffectsFrom(jammed.Name);
                }
                // 手牌那一跳（原版最后两跳遍历的是**手牌**）
                stripped += RemoveHandEffectsFromSource(ctx, side, jammed.Name);
            }
            if (stripped > 0)
                ctx.Log($"（「{jammed.Name}」被 jam ⇒ 从场上/手牌撤掉它给的 {stripped} 条外来效果）");

            // ---- ④ 致死检查（原版第 ⑥ 步 `CheckIfDead(undefined)`，紧跟在 `ApplyJam()` 之后）----
            //   ⚠️ 顺序是**原版写死的**：致死在这里判，广播在它之后
            //      （`ReactToUnitJammed` 的 ⑥ 在函数体内，`BroadcastUnitJammed` 在 `ResolveJamUnit` 里）。
            if (dead)
            {
                int slot = -1;
                if (who >= 0)
                    for (int s = 0; s < BoardSpec.Size; s++)
                        if (ReferenceEquals(ctx.Players[who].Board[s], jammed)) { slot = s; break; }
                if (slot >= 0)
                    CleanupDeaths(ctx, who, slot, killer: -1, maySurvive: true,
                                  actingCard: ctx.ActingUnit);
                else
                    // ⛔ 不许静默：找不到格位 = 我们记的 `who` 和棋盘不一致，必须说出来。
                    ctx.Log($"（`Jam` 后「{jammed.Name}」血 ≤ 0，但**在 P{who} 的棋盘上找不到它的格位**"
                          + " ⇒ 死亡结算没跑，需要查 `OwnerOf` / 棋盘写入）");
            }

            // ---- ⑤ 广播（`BroadcastUnitJammed`）----
            //   ⚠️ 原版这个广播器**一发 `OnTrigger` 都没有**（接收方 `ReactToUnitJammed` 体内 0 处），
            //      它的「广播」等于上面 ③ 那一轮清理 ⇒ 我们这里发的 `UnitJammed` 事件
            //      **是我们给这条广播起的名字**（🔶 见 `WhenEventKind.UnitJammed` 的 doc）。
            BroadcastWhen(ctx, WhenEventKind.UnitJammed, who, jammed.Card,
                          subject: jammed, actor: jammed, target: null);

            // ---- ⑥ 两侧 `UpdateCardEffects`（原版最后一步）----
            //   我们这一侧的等价口 = 光环的**零副作用窄口**（`Auras.RefreshInPlayNumericAndKeywords`，
            //   `F3` 的 R5 建的；⛔ 别换成 `Auras.Recompose` —— 它会重放夹血/费用登记/誓约授予）。
            Auras.RefreshInPlayNumericAndKeywords(ctx);
            return true;
        }

        /// <summary>🆕 **2026-10-26（批次 4 · `F2`）**：把「某张卡给的 **extrinsic** 手牌效果」从一方手牌里整份摘掉。
        ///
        /// <para>**判据**：原版 `ReactToUnitJammed` 的 else 支 `RemoveExtrinsicEffectsFrom(disenchantedCard)`
        /// —— 它撤的是「**绑在施放者身上**（`extrinsic`）才成立」的那一批
        /// （`CardEffect.extrinsic // +0x30` 的语义见 `CardInstance.HandEffect.Extrinsic` 的 doc）。</para>
        /// <para>⚠️ **只摘 `Extrinsic` 的**：非 extrinsic 的手牌效果是**挂在目标身上的**，
        /// 施放者离场/jam 都不该撤（原版同一个判据）。</para>
        /// <para>⚠️ **先摘记录再摘载荷**（记录表是「后进手牌的牌要不要补」的唯一判据）——
        /// 反过来会让后面进手的牌**又被补上**同一条已撤掉的效果。</para>
        /// </summary>
        static int RemoveHandEffectsFromSource(BattleContext ctx, int owner, string sourceName)
        {
            if (ctx == null || string.IsNullOrEmpty(sourceName)) return 0;
            var recs = ctx.Players[owner].HandEffectRecords;
            var ids = new List<int>();
            for (int k = recs.Count - 1; k >= 0; k--)
            {
                var r = recs[k];
                if (r == null || !r.Extrinsic) continue;
                if (!string.Equals(r.Source, sourceName, System.StringComparison.Ordinal)) continue;
                if (r.RecordId != 0) ids.Add(r.RecordId);
                recs.RemoveAt(k);
            }
            if (ids.Count == 0) return 0;
            var hand = ctx.Players[owner].Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var inst = hand[i];
                if (inst == null) continue;
                inst.HandEffects.RemoveAll(e => e != null && ids.Contains(e.RecordId));
            }
            return ids.Count;
        }

        // ==================================================================
        //  🆕 2026-10-26（完全复刻 · 批次 6 · 执行代理 `G14` · 任务③）
        //     **`TriggerNameOfWhenKind` 没映射的事件要【出声】** —— 追加在类尾（⛔ 别插进中间：那会让下游行号整体漂）
        // ==================================================================
        //
        //  🔴 **为什么必须出声**（工程红线「不许静默失败」）：`TriggerNameOfWhenKind` 返回 `null` 时，
        //  `EmitListenerTrigger` 原来是**直接 `return`** ⇒ 结果是「**卡的效果照跑、动画一条不播**」，
        //  而且玩家和我们**都看不出哪里不对**。批次 6（`G14`）已补 4 条映射（`GainQuest` 695 / `CreatesSecret` 700 /
        //  `CreatesSabotage` 701 / `Triggers:landing` 450）。
        //
        //  🆕 **2026-10-26（批次 8 · `B8_C` · 任务②）**：剩下几种**逐种回原版查过**，结论分两类 ——
        //   · **真缺 ⇒ 已补**（`WhenEvents.ExtraTriggerNameForKind`；`TriggerNameOfWhenKind` 在 switch 之后兜底调）：
        //      `kills` → `Slay`(120，`CardScript__TriggerSlay.c:9-11`) ·
        //      `losestealth` → `UnitLoseStealth`(647，`CardScript__TriggerLoseStealth.c:5-9`)。
        //   · **原版本身就没有 AbilityTrigger 号 ⇒ 这里【降噪但不静音】**（`WhenEvents.KindsAbsentInOriginal`）：
        //      `getsshield`（`AbilityTrigger` 97 条无 shield 名 · `CardScript` 15 个 `ReactTo*` 无 shield）·
        //      `becomesremnant`（枚举无 Remnant 名 · `CardScript.TransformIntoRemnant` 体内 0 处 `OnTrigger`；
        //      `SAU53` 那条走附魔 SO `buffType 90`）·
        //      `unitjammed`（`ReactToUnitJammed` 体内 0 处 `OnTrigger`，`F2` 第 4 轮 §2·1）。
        //      ⇒ 这三种**不再报 `LogWarning`**（否则「真缺口」会被预期噪声淹掉），改成每种一次的
        //      `Debug.Log`（信息级），文案直接写「**原版也没有** ⇒ 预期，不是我们的缺口」。
        //      ⛔ 仍然**出声**：静默失败是红线，只是把级别降下来。
        //
        //  ⚠️ **只报一次 / 每种 kind 一次**（静态去重）：否则每个动作都刷一行，日志里就看不见真问题了。
        //  ⚠️ 用引擎级日志（**不是** `ctx.Log`）：`ctx.Log` 是玩家侧日志，这是**引擎自己的诊断**、
        //     日志筛查看得见（`F1` 第 4 轮踩过「拿 `ctx.Log` 当错误日志」那个坑）。
        static readonly HashSet<string> _unmappedWhenKindWarned = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>见上面那段：`EmitListenerTrigger` 在 `TriggerNameOfWhenKind` 返回 `null` 时调一次（每种 kind 一次）。
        /// <para>两类分档：**原版也没有号**的（`WhenEvents.KindsAbsentInOriginal`）⇒ `Log`（信息级，预期）；
        /// 其余（真缺口）⇒ `LogWarning`。</para>
        /// // 🔶 自己设计（非原版）：未映射事件的告警口 · 原版没有这种诊断（它自己不会漏映射）· `G14` 2026-10-26 定；分档 `B8_C` 2026-10-26 加</summary>
        static void WarnUnmappedListenerKind(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return;
            if (!_unmappedWhenKindWarned.Add(kind)) return;
            const string tail = "（`EmitListenerTrigger` 的 `tn == null` 支：监听器的**效果照跑、动画一条都不播**）";
            if (WhenEvents.KindsAbsentInOriginal.Contains(kind))
            {
                // 🔉 **降噪档**：逐种查过，原版**本来就没有**这条 AbilityTrigger ⇒ 不出动画是**预期**。
                UnityEngine.Debug.Log($"[TriggerNameOfWhenKind] 事件 `{kind}` 没有 AbilityTrigger 名 —— "
                    + "**原版也没有**（判据见 `WhenEvents.ExtraTriggerNameForKind` / `KindsAbsentInOriginal` 的注释）"
                    + $" ⇒ 不出 Trigger 动画是**预期行为**，不是缺口{tail}。");
                return;
            }
            UnityEngine.Debug.LogWarning($"[TriggerNameOfWhenKind] 事件 `{kind}` 没有映射到 AbilityTrigger 名 ⇒ "
                + $"**这是我们的映射缺口**{tail}。"
                + "判据口径见 `EffectResolver.TriggerNameOfWhenKind` 的 switch 与 `WhenEvents.ExtraTriggerNameForKind`；"
                + "已知原版也没有号的三种见 `WhenEvents.KindsAbsentInOriginal`。");
        }

        /// <summary>
        /// 🆕 **2026-10-26（批次 6 · `G14` ④）**：**「相邻」锚点找不到**时出声（每种 `(锚点档, 施放者)` 一次）。
        /// // 🔶 自己设计（非原版）：未映射/找不到时的告警口 · 原版没有这种诊断（它自己不会漏映射）· `G14` 2026-10-26 定
        /// 该支的语义本身是「空过」（⛔ 不退回整个目标池），但**不能静默** —— 那是我们的定位缺口（工程红线）。
        /// </summary>
        static readonly HashSet<string> _adjacentAnchorWarned = new HashSet<string>(System.StringComparer.Ordinal);

        static void WarnAdjacentAnchorMissing(string anchorKind, string sourceName)
        {
            string key = (anchorKind ?? "?") + "|" + (sourceName ?? "?");
            if (!_adjacentAnchorWarned.Add(key)) return;
            UnityEngine.Debug.LogWarning($"[相邻] 锚点 `{anchorKind}`（施放者：{sourceName ?? "无"}）认不出 / 已不在棋盘上 "
                + "⇒ 这次**空过**（不按整个目标池打）。⚠️ 若卡面确实写着「相邻」，这就是**我们的缺口**："
                + "判据 `MinionManager__GetAdjacentUnits.c:52-70`（在己方部队列表里按引用找锚点）。");
        }

        // ==========================================================================================
        //  🆕 2026-10-26（批次 8 · 执行代理 `B8_P`）：**「相邻」目标集的两处真缺陷** ＋ **阵营资源乘值**
        //  🔴 **一律追加在类尾**（本文件被账大量按 `EffectResolver.cs:<行号>` 引用；
        //     在类中间插成员会让下面每一行整体下移 ⇒ 账里的落点全部漂）。
        // ==========================================================================================

        /// <summary>
        /// 「相邻」锚点的**收口版**：先走 <see cref="ResolveAdjacentAnchor"/>，再补一条「`PreviousTarget`
        /// 不许落回这条能力的主语」的兜底。
        ///
        /// <para>**判据（第一权威，现读）= `decomp_full/AbilityLogic__GetTargets.c:1046-1057`**（🥇）：
        /// `ta == 0x6e (110 adjacentToTargetIfMeetsCriteria)` 那一支是
        /// `cVar3 = UnityEngine_Object.op_Inequality(param_5, 0); if (cVar3 != '\0') { GetAdjacentUnits(param_1, param_5, 0); … }`
        /// —— **锚点 = `param_5` = 这条 ability 的【目标】**；`param_5 == null` 时**连候选都不填**
        /// （⛔ 不是退回「施放者自己」）。对比 `ta == 0x69 (105)` 那一支才是「上一批目标的首个，**否则** `param_3`（拥有者）」。
        /// 全池 `ta` 分布（技能行现读）= **110: 14 行 / 100: 17 / 113: 3 / 106: 1 / 105: 0**
        /// ⇒ 卡池里**一条 105 都没有**，所以我们解析层那个 `PreviousTarget` 实际对应的全是原版的 **110**，
        /// 它的兜底**不该**是「拥有者自己」。</para>
        ///
        /// <para>🔴 **改前的可见缺陷（两张，卡面都不打 `*`）**：
        /// · `GOF97 Gargantuan Squiggoth`：`Rally: Stun an enemy and adjacent units` 是**单 op**
        ///   ⇒ `ctx.LastTarget` 还是能力入口种下的主语（`ResolveOps` 的 `prime = source`）
        ///   ⇒ 锚点 = 它自己 ⇒ 配合 `AnchorInSet` **把自己眩晕**（探针读数见账 `B8_P`）。
        /// · `SOR43 Cleansing Flames` / `AM23` / `TL27` / `UM105` / `UM115` / `9238…` 同形（`enemy … and its adjacent`）。</para>
        ///
        /// <para>🔶 自己设计（非原版）：目标池取首个兜底 · 原版 param_5==null ⇒ 候选为空 · 调度台 2026-10-26 裁定 ｜ 兜底口径：原版「目标」由 `param_5` 从外面给（玩家 / AI 选）；
        /// 我们这条路上没有 `chosen` 时，按**目标池的第一个**当锚点 —— 与本工程其它「取第一个」的定死规则同一条
        /// （⛔ 不掷骰、同一局必须可复现）。**有** `chosen`、或同一 ability 前面那条 op 已经选中过目标
        /// （`ctx.LastTarget != source`）时，**原口径一字不动**。</para>
        /// </summary>
        static UnitState ResolveAdjacentAnchorFromPool(BattleContext ctx, int owner, EffectTargetSpec spec,
                                                        UnitState source, UnitState chosen, List<UnitState> pool)
        {
            var anchor = ResolveAdjacentAnchor(ctx, owner, spec, source, chosen);
            if (spec == null || spec.Anchor != AdjacentAnchor.PreviousTarget) return anchor;
            if (chosen != null) return anchor;                                     // 调用方点定的目标优先（原版 `param_5`）
            if (anchor != null && !ReferenceEquals(anchor, source)) return anchor;  // 同一 ability 前一条 op 的目标
            // 到这里 = 这条 ability **还没有真正的「上一个目标」**（`ctx.LastTarget` 只是主语）⇒ 按目标池挑一个。
            if (pool == null || pool.Count == 0) return null;
            ctx.Log($"（「相邻」锚点：这条能力还没有选过目标（`ctx.LastTarget` 只是施放者自己）"
                  + $" ⇒ 按目标池取第一个「{pool[0].Name}」当锚点）");
            return pool[0];
        }

        /// <summary>
        /// 锚点/邻居的**阵营**是否落在这条 op 写的那一侧（`spec.Side` ∈ {`own`,`enemy`,`any`}）。
        /// 判据 = `AbilityLogic__GetTargets.c:1100-1110`：相邻候选**逐个**过 `CheckIfMeetsCriteria`
        /// （`friendlyUnits/enemyUnits/includeHero…` 那张 36 格）⇒ 锚点自己不是例外。
        /// </summary>
        static bool AnchorOnSpecSide(EffectTargetSpec spec, int anchorPlayer, int owner)
        {
            if (spec == null) return true;
            bool ownSide = anchorPlayer == owner;
            if (spec.Side == "own") return ownSide;
            if (spec.Side == "enemy") return !ownSide;
            return true;                                   // `any` / 其它值 = 不过滤
        }

        /// <summary>
        /// 「相邻」那一圈的**成员判定**（改前的两句 `!u.IsAlive` ＋ `pool.Contains(u)` 合成这一处）。
        ///
        /// <para>① **活着**且 `pool.Contains` ⇒ 收（**原口径一字不动**）。</para>
        /// <para>② **活着但不在池里** ⇒ 收不了（过不了同一套筛选，原口径）。</para>
        /// <para>③ **刚死**（`Health &lt;= 0`、还没出队）⇒ 判据 `MinionManager__GetAdjacentUnits.c:52-70`（🥇）：
        ///   原版在那一侧的**部队列表**里按引用找锚点、取 `i±1`，死亡单位要等 `MoveToCemetery` 才离开列表
        ///   ⇒ **同一拍刚死的邻居照样是相邻目标**。改前多要了一道 `IsAlive`（`G14` 只修了锚点那一半）
        ///   ⇒ 静默少打。这里用**同一套筛**重判一次（`pool` 是活的，含不了它）：阵营 ＋ `troopOnly` ＋
        ///   <see cref="ApplyTargetFilters"/> ＋ <see cref="ApplyTargetCriteriaFilter"/>。</para>
        /// </summary>
        static bool AdjacentNeighborCounts(BattleContext ctx, int owner, EffectTargetSpec spec, EffectOp op,
                                           UnitState source, UnitState chosen,
                                           CardDef.ConditionSpec structuredCondition,
                                           UnitState u, int anchorPlayer, List<UnitState> pool)
        {
            if (u == null) return false;
            if (pool != null && pool.Contains(u)) return true;          // ① 活着的那一批
            if (u.IsAlive) return false;                               // ② 活着却过不了筛选
            if (!AnchorOnSpecSide(spec, anchorPlayer, owner)) return false;   // ③ 死邻居：同一套筛
            if (spec.Kind == "troop" && u.IsWarlord) return false;      // `AddSide` 的 `troopOnly` 那一道
            var one = new List<UnitState> { u };
            ApplyTargetFilters(ctx, one, spec, quiet: true, structuredCondition: structuredCondition);
            if (one.Count == 0) return false;
            ApplyTargetCriteriaFilter(ctx, one, op, owner, source, chosen, quiet: true);
            return one.Count > 0;
        }

        /// <summary>
        /// 这条 op 的「数值」是不是**玩家身上的阵营资源**（`buffMana` 那一族）——
        /// 原版 `AbilityLogic.AdjustManaEffect` 对它做的是**乘进数值**，不是重跑 N 遍。
        /// 判据 = `decomp_full/AbilityLogic__AdjustManaEffect.c:1-11`（🥇 全文 11 行）：
        /// `*(int *)(lVar1 + 0x10) = param_3 * *(int *)(lVar1 + 0x10);` —— `param_3` = 倍率，
        /// 直接把 `ManaBuff` 的数量格**乘一次**；随后 `BattleManager.AddManaBuff` 只建**一条** `BattleAction(0x22)`
        /// （`BattleManager__AddManaBuff.c:22`）⇒ 一次加值、一次广播。
        /// </summary>
        static bool IsManaAmountOp(EffectOp op)
        {
            return op != null && (op.Verb == "gainfaith" || op.Verb == "gainspirit"
                                || op.Verb == "gainquest" || op.Verb == "gainenergy");
        }

        /// <summary>
        /// 阵营资源（信仰/灵魂石/任务点/最大能量）的「`for each` / `furtherMultiplier`」要**乘进 `Amount`**、
        /// 然后**只结算 1 遍**（判据见 <see cref="IsManaAmountOp"/>）。
        ///
        /// <para>🔴 **改前的可见差异**：`SOR49 Shrineworld`（`fm = 3`）我们打**三行**「信仰 +1」
        /// （三次 `CollectFaith` 广播）—— 数值恰好也是 +3，但**事件次数 3≠1**：`SOR38 Paragon Warsuit`
        /// 的「When you gain Faith」在原版响 **1** 次、我们响 **3** 次。见账 `B8_P` 的观测列。</para>
        ///
        /// <para>⚠️ **只管乘数那一半**：`…, N times`（<see cref="EffectOp.RepeatTimes"/>）是**另一条判据**
        /// （原版真是「重复结算」，见 `TryComputeRepeats` ③ 的 doc）⇒ `RepeatTimes &gt; 1` 时**不折**，
        /// 保持原来那条循环。</para>
        /// <para>返回**同一个对象** = 不折（调用方 `ReferenceEquals` 判得出来）；否则是**副本**
        /// （⛔ 绝不改解析产物本身 —— 那张卡第二次打出时是同一份对象）。</para>
        /// </summary>
        static EffectOp FoldManaMultiplier(BattleContext ctx, string by, EffectOp op, ref int repeats)
        {
            if (op == null || repeats <= 1 || !IsManaAmountOp(op)) return op;
            if (op.RepeatTimes > 1) return op;             // `…, N times` 那一档走原循环（见上面 doc）
            var folded = op.Clone();
            folded.Amount = op.Amount * repeats;
            ctx.Log($"{by}：「{op.Source}」原版是**乘进数值**、不是重跑 {repeats} 遍"
                  + $"（`AbilityLogic__AdjustManaEffect.c:5-7`）⇒ {op.Amount} × {repeats} = {folded.Amount}，结算 1 遍");
            repeats = 1;
            return folded;
        }

        // ==========================================================================================
        //  🆕 2026-10-26（第八十三会话 · 执行代理 `B1`）：两处广播接收方缺口
        //  （新成员一律追加到类尾 —— 避免给 `账/*.tsv` 的 `文件:行号` 落点造成漂移）
        // ==========================================================================================

        /// <summary>
        /// **这一族广播的「棋盘那一跳」是否也收【待死】单位** —— 原版每条广播器的闸**不统一**，
        /// 本方法就是那份名单的**唯一落点**（⛔ 别在 `BroadcastWhenCore` / `SnapshotBoardListeners`
        /// 里再写第二份）。
        ///
        /// <para>**为什么会有这个问题**：我们的 `BroadcastWhen` 是**一个统一广播器**，
        /// 原版是**每条广播器各自一份循环** ⇒ 「棋盘那一趟要不要判 `IsInPlay`」在原版里
        /// **逐族不同**，统一器只能把它变成一个按 `kind` 查的表。</para>
        ///
        /// <para>**今天只有一族是「不判」的**：
        /// `triggers:sacrifice`（= 原版 `BattleManager.BroadcastSacrificeResolved`
        /// → `BattleManagerSupport.BroadcastUnitUsedSacrifice`）。判据（逐句现读，ILSpy 优先）：
        /// <list type="number">
        /// <item>`BattleManager.BroadcastSacrificeResolved`
        /// （`out_cpp2il_dev_ilspy/BattleManager.cs:27937-27939`）= 一句转调
        /// `BattleManagerSupport.BroadcastUnitUsedSacrifice(this, unitSacrifice)`。</item>
        /// <item>`BattleManagerSupport.BroadcastUnitUsedSacrifice`
        /// （`BattleManagerSupport.cs:1764-1805`）三趟：棋盘（`:1766-1777`）→ 当前回合方手牌
        /// （`:1778-1790`）→ 另一方手牌（`:1791-1804`）。**三趟的闸都只有**
        /// `if (cardScript != unitSacrifice)`（自排除）—— **没有 `IsInPlay()`、没有 null 判**。
        /// （`.c` 版 `decomp_full/BattleManagerSupport__BroadcastUnitUsedSacrifice.c` 第 1/2 趟里
        /// 那句 `op_Inequality(uVar3)` **看着只有一个实参**，那是 Ghidra 把 `rdx` 掉了 —— 机器码里
        /// 有 `mov rdx, rsi`，与第 3 趟的 `op_Inequality(uVar3, param_2)` 同义。）</item>
        /// <item>接收方 `CardScript.ReactToUnitUsedSacrifice`
        /// （`CardScript.cs:7783-7788`）体内**只有** `OnTrigger(225, …, this, default, default)`
        /// —— 自排除写在调用方，接收方不再排一次。</item>
        /// </list>
        /// ⇒ 待死（≈ 原版 `waitingToDie(5)`）的卡**照样收**这一族。</para>
        ///
        /// <para>⛔ **别把这条推广到别的族**：`BroadcastUnitSpiritStone`
        /// （`BattleManagerSupport.cs:1050-1083`）棋盘那一趟有 `if (cardScript.IsInPlay())`
        /// （`:1057`）；`BroadcastUnitSummoned` / `Damaged` / `Attacked` / `Landing` …
        /// 同族也各有自己的 `IsInPlay`。要加新族**回原版广播器逐行读一遍**再往这里加。</para>
        /// </summary>
        static bool BoardJumpIncludesPendingDeath(string kind)
        {
            if (kind == null) return false;
            // `WhenEventKind.Triggers("sacrifice")` —— 棋盘那一跳没有 `IsInPlay` 的那一族之一（现读 `cards_engine.json` 里
            // 提到 `sacrifice` 的卡 **0 张**，所以今天**不可观测**；本节是口径对齐）。
            // 🆕 2026-10-27（B9 · `E6ENG` · 账 627 / 631）：`Draw` / `GainFaith` / `GainQuest` 三族**同样没有** `IsInPlay` 闸
            //   —— `BattleManagerSupport__BroadcastCardDrawn.c`（棋盘循环全文只判非 null，`Ghidra` 第 30-33 行）·
            //   `…__BroadcastCollectFaith.c:29-38` · `…__BroadcastCollectQuestPoints.c:29-38`（后两者逐句同形：预调 + 棋盘循环，循环体只有 `op_Inequality(u, subject)`）。
            return kind == WhenEventKind.Triggers(KeywordTable.Sacrifice) || kind == WhenEventKind.Draw || kind == WhenEventKind.GainFaith || kind == WhenEventKind.GainQuest;
        }

        /// <summary>
        /// 某个单位**用了灵魂石能力**之后按原版广播（trigger **455** = `AbilityTrigger.OtherCardSpiritStone`）。
        ///
        /// <para>**🔴 本方法存在的唯一理由是【自排除】**（2026-10-26 第八十三会话 · 执行代理 `B1`）。
        /// 改前调用点直接 `BroadcastWhen(SpiritAbility, owner, null, null)` —— `subject` 传 `null`
        /// ⇒ `WhenEvents` 那条「关键词触发族·自排除」守卫「拿不到事实就不收窄」⇒
        /// **自己用灵魂石时自己的监听器会被叫醒**（比卡面宽）。</para>
        ///
        /// <para>**判据（逐句，第一权威 = ILSpy `out_cpp2il_dev_ilspy`，第二 = `decomp_full/*.c`）**：
        /// <list type="bullet">
        /// <item>`CardScript.TriggerSpiritStone(this)`：先 `RawCardScript.OnTrigger(600, …, this, this)`，
        /// 再判这张卡**真有** 600 那条 ability ⇒ `BattleManager.BroadcastUnitSpiritStone(this)`
        /// （`decomp_full/CardScript__TriggerSpiritStone.c` 全文；`BattleManager.cs:27987-27989`
        /// 只是一句转调）⇒ 广播的实参**就是发起那张卡**。</item>
        /// <item>接收方 `CardScript.TriggerOtherCardSpiritStone(CardScript spUnit)`
        /// （`CardScript.cs:7619-7627`）**体内第一句**就是
        /// `if (spUnit != this) { … OnTrigger(455, …, actingCard = default, targetCard = default); }`
        /// —— `.c` 里同一句渲染成 `UnityEngine_Object__op_Inequality(param_2, param_1)`。
        /// ⇒ **发起者自己不被叫醒**；且 `actingCard` / `targetCard` **都是 `null`**。</item>
        /// <item>广播器 `BattleManagerSupport.BroadcastUnitSpiritStone`
        /// （`BattleManagerSupport.cs:1050-1083`）三趟：棋盘（**带 `IsInPlay` 闸**，`:1057`）→
        /// 当前回合方手牌（`:1064-1072`）→ 另一方手牌（`:1073-1082`）；三趟**都没有**额外的
        /// `op_Inequality` —— 自排除**只在接收方体内**。</item>
        /// </list></para>
        ///
        /// <para>**为什么自排除写在这一侧、而不是 `WhenEvent.cs` 的
        /// `WhenEvents.SelfExcludesOnKeywordTrigger`**：那个守卫**只对 `triggers:*` 那一族生效**
        /// （入口按 `WhenEventKind.TriggersPrefix` 判），而 `SpiritAbility` 的 Kind 是
        /// `spiritability`、**不在那一族里**；反过来 `triggers:*` 那一族**原版也不自排除**
        /// （自己本身就是触发器 —— 见那个方法的 doc 里 `Ferocity` / `Synapse` 那几条）
        /// ⇒ **⛔ 绝不能一刀切去改 `triggers:*` 那一支**。`OtherCardRequiem` 是同一形状，
        /// 它落进了 `WhenEvent.cs`；本笔的文件白名单只有本文件 ⇒ 走
        /// `BroadcastWhen` 原生的 `boardListeners` 口（调用方在**事件发生那一刻**拍快照）实现，
        /// 判据仍然**只有 `SnapshotBoardListeners` 一份**（这里只多做一件「剔掉发起者」）。</para>
        ///
        /// <para>⚠️ **手牌那两跳不另做排除**：原版排的是「**同一个对象**」，而手牌里的卡
        /// 不可能就是场上刚用石的那个 `UnitState` ⇒ 等价于不排除（我们照样发）。</para>
        ///
        /// <para>⚠️ **`card` / `subject` / `actor` 都【故意】传 `null`**：原版 `OnTrigger` 的
        /// `actingCard` / `targetCard` 都是 `default`（见上）⇒ 传 `subject` 会**多出**一个
        /// 「事件主体」（`ResolveOps` 的 `it` 种子 + `SelfOnly` / `criteria` 会拿它判），
        /// 那是**比原版宽**。原版那个 `spUnit` **只用于自排除**，所以这里也只用于自排除。</para>
        /// </summary>
        /// <param name="who">**付石那一方**（原版 `IsPlayerTurn()` 那一侧的手牌先扫）。</param>
        /// <param name="spUnit">**发起那张卡**（= 原版 `unitsp`）。`null` ⇒ 不做自排除（出声那一档）。</param>
        public static void BroadcastUnitSpiritStone(BattleContext ctx, int who, UnitState spUnit)
        {
            if (ctx == null) return;
            if (spUnit == null)
            {
                // 施放者**不是棋盘上的单位**（现读全是「手牌里的战术卡付石」那一档：`ASH49` / `ASH54` … 12 张）。
                // 原版这时 `unitsp` 是**手牌那张 CardScript**，而 `TriggerOtherCardSpiritStone` 排的是
                // 「同一个对象」—— 手牌卡与棋盘/手牌里的**监听者**永不相等 ⇒ **排除退化为无操作**，
                // 与我们这条 `null` 支路**等价**。⚠️ 我们这层没有手牌卡的 CardScript 身份 ⇒ 不猜、出声。
                ctx.Log("灵魂石广播：发起方不是棋盘单位（手牌战术卡付石）⇒ 自排除退化为无操作（与原版等价）");
                BroadcastWhen(ctx, WhenEventKind.SpiritAbility, who, null, null);
                return;
            }
            // 快照由调用方在**事件发生那一刻**拍（`BroadcastWhen` 的 `boardListeners` 口），
            // 判据仍是 `SnapshotBoardListeners` 那一份 —— 这里只多做一件事：剔掉发起者自己。
            var snap = SnapshotBoardListeners(ctx, null, null);
            snap.RemoveAll(l => ReferenceEquals(l.Unit, spUnit));
            BroadcastWhen(ctx, WhenEventKind.SpiritAbility, who, null, null, boardListeners: snap);
        }

        // ==================================================================
        //  🔴 2026-10-26（完全复刻批次 9 · 执行代理 `E9ER`）：**「玩家点定的目标」这一层的唯一判据**
        //
        //  治的是账 `B9` 的缺口 ②「`Side == "any"` 的 19 张卡打不到对面」（玩家可见的错打）。
        //  **原版**：出牌时目标以 **CardScript 引用**的形式进动作（`_ResolvePlayCardFromHand…:905-940`
        //  写 `action+0x20`），结算时 `IsValidSpellTarget(target)` / `GetTargets(targetCard)` 读那个**对象**
        //  ⇒ **侧是跟着目标走的**，格号在原版根本不是目标身份。
        //  **我们**：表现层/录像/探针之间传的是**格号**（`AiAction.Slot`），引擎按「卡面 `PickSide` 那一侧」
        //  去读同号格 ⇒ 卡面是「任意一侧」时（`EffectText.PickTarget` 对 `Side=="any"` 返回 `null`、
        //  于是 `PickSide` 给 `""`）**恒读我方** ⇒ 玩家点对面的单位，效果落在我方同号格上。
        //  ⇒ 收口：能拿到**对象**（`chosen` / `pickedByPlayer`）就一律以它为准；格号只在没有对象时兜底。
        //  ⚠️ 这条**不是**新语义：`IsLegalPick` 早就对 `Side=="any"` 给**两侧并集**（`AddSide` 两支都加），
        //    表现层 `PlayTargetPool` 同理 ⇒ 池子一直允许点对面，只有「解成谁」这一格缺侧信息。
        // ==================================================================

        /// <summary>
        /// **按格号解「玩家点的是谁」** —— 只在调用方**没**给目标对象时用
        /// （<see cref="CanPlayTactic"/> 的校验支）。
        ///
        /// <para>侧按卡面规格解：`enemy` ⇒ 对面；`own` ⇒ 我方；**`any` / `spec == null` ⇒ 我方**
        /// （= 改前的老口径，⛔ 不改：格号不带侧信息，两侧同号格都有人时「猜对面」就是另一种静默错打）。</para>
        ///
        /// <para>⚠️ 想让「任意一侧」的卡真能打到对面，调用方必须把**对象**递进来
        /// （`RuleCore.PlayCard(ctx, p, handIdx, slot, chosen)` → `PlayTactic(…, chosen)`）；
        /// 表现层那条路已经这么做了（`BattleDriver._chosenTargetForNextPlay`）。</para>
        /// </summary>
        static UnitState PickedBySlot(BattleContext ctx, int owner, EffectTargetSpec spec, int slot)
        {
            if (ctx == null || slot < 0 || !BoardSpec.IsValid(slot)) return null;
            string s = spec != null ? spec.Side : null;
            if (s == "enemy") return ctx.Players[1 - owner].Board[slot];
            return ctx.Players[owner].Board[slot];
        }

        // ==================================================================
        //  🔴 2026-10-26（完全复刻批次 9 · 执行代理 `E9ER`）：**「督军血跨过骷髅阈值 ⇒ 广播 640」的发射点**
        //
        //  治的是账 `B9` 的缺口 ⑥「`BroadcastEarnSkull` 发射点缺失」：`BroadcastEarnedSkull`
        //  （本文件同名的 `public static` 方法，判据已逐句读过）一直**没有调用点** ⇒ 17 张带 `trigger 640`
        //  能力的卡（`Earthquake` / `Star Orbit` …，池内 0 张可达）永远是聋子。
        //
        //  **判据（`decomp_full/CardScript__CheckForSkullEarned.c:1-69`，逐句）**：
        //   ```
        //   if (cardType != 10) return;                       // ① 只对**督军卡**（cardType 10 = Hero）
        //   if (EnoughPendingDamageToDie()) return;           // ② 这一下会死 ⇒ 不广播（局都结束了）
        //   n = GetEnemySkullCount(myHealth);                 // ③ =「我的血 ≤ 阈值」的档数（20/10/0）
        //   if (saved /*+0x354*/ < n) { saved = n; BroadcastEarnSkull(...); }   // ④ **只在变大时**、**一次**
        //   ```
        //   `GetEnemySkullCount`（`BattleScoreManager__GetEnemySkullCount.c`）的计数式 =
        //   `#{t ∈ {20,10,0} : health <= t}`，与 `DeckRules.SkullsFor(health)` **逐字同义**（现读核过）。
        //   ⚠️ ④ 是「跳到几就抬到几、只广播**一次**」——**不是**逐档各发一次；`play` 里一次掉 15 血
        //   （25→10）只该响 1 声。⛔ 别改成 for 循环逐档发。
        //   ⚠️ **得骷髅的是被打那一方的【对手】**（“EnemySkullCount” 的语义；`BroadcastEarnSkull` 取
        //   `manager + 0xc0/+200` 两套集合之一）⇒ `who = 1 - 被打督军的 owner`。
        //   ⚠️ 计数按**被打那个督军**记（每方只有一张督军卡 ⇒ 与「按得主记」等价），字段含义 = 「这张
        //   督军卡的血**曾经**到过第几档」；血被治回去**不回退**（与 `+0x354` 一致）。
        //  ⚠️ 存档位置用**每局附加状态**（`ConditionalWeakTable`，与 `QuestAux` 同一套模式）——
        //   `PlayerState` / `BattleContext` 不在本代理白名单，而这是**派生状态**（不参与指纹/存档）。
        // ==================================================================

        sealed class SkullAux
        {
            /// <summary>下标 = **督军的主人**；值 = 这张督军卡的血已经跨到第几档（0..3）。</summary>
            public readonly int[] Earned = new int[2];
        }

        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BattleContext, SkullAux>
            SkullAuxByCtx = new System.Runtime.CompilerServices.ConditionalWeakTable<BattleContext, SkullAux>();

        static SkullAux SkullAuxOf(BattleContext ctx)
        {
            SkullAux aux;
            if (SkullAuxByCtx.TryGetValue(ctx, out aux)) return aux;
            aux = new SkullAux();
            SkullAuxByCtx.Remove(ctx);          // `Add` 对同键会抛 ⇒ 先删（同 `QuestAuxOf`）
            SkullAuxByCtx.Add(ctx, aux);
            return aux;
        }

        /// <summary>
        /// **督军血量写完之后调一次**（`RuleCore` 里两个真实写点：`Hurt` 的 `u.Health -= actual`
        /// 与牌库抽空的疲劳那一支）。非督军 / 已死 / 没跨档 ⇒ 什么都不做（零开销）。
        ///
        /// <para>⚠️ 判据与「为什么只广播一次」「得主为什么是对面」→ 上面那段注释块。
        /// ⛔ 别在「治疗 / 加血上限」那些写点上调它（原版 `CheckForSkullEarned` 也只在掉血之后被调）。</para>
        /// </summary>
        public static void NoteWarlordHealthForSkulls(BattleContext ctx, UnitState warlord)
        {
            if (ctx == null || warlord == null || !warlord.IsWarlord) return;
            if (!warlord.IsAlive) return;                     // ≈ 原版 ②「这一下会死 ⇒ 不广播」
            int owner, slot;
            if (!FindUnit(ctx, warlord, out owner, out slot)) return;   // 已经不在棋盘上
            int n = DeckRules.SkullsFor(warlord.Health);
            var aux = SkullAuxOf(ctx);
            if (n <= aux.Earned[owner]) return;               // ④ 只在**变大**时
            aux.Earned[owner] = n;
            BroadcastEarnedSkull(ctx, 1 - owner, warlord);    // 得骷髅的是**对面**
        }

        // ==========================================================================================
        //  🆕 2026-10-26（批次 9 · 执行代理 `W10WR` · 调度台追加）：**残骸翻回 = 原地翻**
        // ==========================================================================================
        //  判据（🥇 第一权威 · 全量反编译，逐句读过）：
        //   · `decomp_full/CardScript__TransformFromRemnant.c:1-19`（全文三句）：`*(undefined2*)(+0x65) = 0`
        //     —— **两个字节一次写**（`isRemnant` + `isLeavingRemnant`）· 表现层换模型 · `UpdateHealthText`；
        //   · 完成回调 `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`：
        //     `ResetToValuesInHand(true)` → `BroadcastUnitReanimated` → `SetAsUnitInPlay` →
        //     `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` →
        //     `CheckStun`（**值恢复在第一句**，后面全是广播/重算）；
        //   · `decomp_full/CardScript__ResetToValuesInHand.c:45-70`：`activeEffects.Clear()` ＋
        //     `activeEnchantments.Clear()` —— **两次 `List.Clear()`**
        //     ⇒ 🔴 翻回**会清掉 buff / 附魔 / 标记**，**只保留** `companionCounter(+0x60)` /
        //     `tideCounter(+0x5C)` / `orderInPlay(+0x44)` / `isPraying(+0x67)`（`.c` 全文没碰这四个）。
        //   · 数：`ResetToValuesInHand` 的逐句（①–⑨）见 `UnitState.ResetToValuesInHand` 的 doc。
        //
        //  ⇒ 改前是 `new UnitState(rem.Instance, false)`（**换一个新单位对象**）——
        //    ⚠️ 那与「同一个 `CardScript` 对象翻回来」不符：上面那四个「跨翻回保留」的计数器会被丢掉，
        //    而附魔那两本账（挂在 `CardInstance` 上、`ResetToValuesInHand` 够不到）整本留着。
        //  ⚠️ 三句**引擎侧口径**（`DeployedTurn` / `PlayOrder` / `ApplyDeployTurnState`）**不搬进来** ——
        //    它们是我们的落位/回合位实现，不是原版 `ResetToValuesInHand` 的一部分 ⇒ 仍由 `DoReanimate` 写。
        //    🔎 顺带：`ResetToValuesInHand` ⑤ 会写 `AttacksThisTurn = 0`，所以
        //    `RuleCore.ApplyDeployTurnState` → `RecomputeActAndAttack` 里那道 `+0x48` 闸**照旧恒假**。
        //  ⚠️ 表现层那四句（`StopAnimsInPlay` / `DestroyRemnantBody` / `SetSpriteAlpha` /
        //    `CardHighlight.ChangeState`）我们没有那一层 ⇒ 不在这里（与 `ResetToValuesInHand` doc ⑩ 同口径）。
        // ==========================================================================================
        /// <summary>**把一具残骸原地翻回成部队**（原版 `CardScript.TransformFromRemnant` + 完成回调的
        /// `ResetToValuesInHand(true)` 两步合起来）。返回**同一个** <see cref="UnitState"/> 对象
        /// （原版就是同一个 `CardScript`，只是 `+0x65` 那两位字节翻回去）。
        /// <para>⚠️ 调用方仍要做三件引擎侧的事：`DeployedTurn` / `PlayOrder` / `ApplyDeployTurnState`。</para></summary>
        static UnitState ReanimateRemnantInPlace(UnitState rem)
        {
            if (rem == null) return null;
            var inst = rem.Instance;
            if (inst != null)
            {
                // ① `activeEffects.Clear()` ＋ ② `activeEnchantments.Clear()`（原版 `:45-70` 那两次 `List.Clear()`）
                //    ⚠️ 我们这两本账挂在 `CardInstance` 上，`ResetToValuesInHand` 够不到 ⇒ 由这里清
                //    （`UnitState.ResetToValuesInHand` 的 doc ② 就是把这一句显式留给调用方）。
                inst.ActiveEffects.Clear();
                inst.ActiveEnchantments.Clear();
            }
            // ③–⑨ ＋ `isRemnant`/`isLeavingRemnant` 两字节
            //    （`questCounter = 0` 在 `ResetToValuesInHand` 的 ③ 里 —— `Instance.QuestCounter = 0`）
            rem.TransformBackFromRemnant();
            return rem;
        }

        // ==================================================================
        //  🆕 2026-10-27（B9 · 执行代理 `E6ENG` · 账 627 / 690）—— 三个小口，**追加在类尾**
        //  （⛔ 别插到中间：`账/*.tsv` 大量按 `文件:行号` 落点，插中间会成片漂移）
        // ==================================================================

        /// <summary>
        /// **账 627（`BroadcastCardDrawn`）的手牌趟数判据** —— 第 `hop` 趟该不该处理这一份手牌实例。
        ///
        /// <para>**判据（逐句现读 ILSpy `out_cpp2il_dev_ilspy/BattleManagerSupport.cs:704-744`；
        /// Ghidra `BattleManagerSupport__BroadcastCardDrawn.c` 交叉核过同一形状）**：
        /// <list type="number">
        /// <item>`:709` **预调**：`drawnCard.CardDrawn(drawnCard)` —— 被抽那张**先给自己响一次**；</item>
        /// <item>`:710-717` 棋盘 `unitsInPlay` 逐个 `CardDrawn(drawnCard)`（**无 `IsInPlay` 闸**，
        ///   见 `BoardJumpIncludesPendingDeath`）；</item>
        /// <item>`:718-730` `GetCardsInHandRef(IsPlayerTurn())` 逐个、**`!= drawnCard` 才响**；</item>
        /// <item>`:731-743` **又** `GetCardsInHandRef(IsPlayerTurn())`（🔴 本意应是 `!IsPlayerTurn()`）
        ///   ⇒ 当前回合方手牌**再响一遍**、对手手牌 **0 遍**。
        ///   **原版这里漏一个 `!`，我们照抄，⛔ 别修**（修了 = 与原版玩家看到的不一致）。</item>
        /// </list></para>
        ///
        /// <para>⇒ 我们这边三趟：**hop 0 = 只处理被抽那张**（= 原版 ① 的预调）、
        /// **hop 1 / hop 2 = 当前回合方整手、两趟都排除被抽那张**（= 原版 ③ ④）。
        /// 合计：被抽那张 **1 次** · 当前手牌其余各 **2 次** · 对手手牌 **0 次**。</para>
        ///
        /// <para>⚠️ **已知次序偏差（如实标）**：原版那条预调发生在**棋盘跳之前**，我们这一跳排在棋盘之后
        ///   （`BroadcastHandWhen` / `BroadcastHandTrapWhen` 都在 `BroadcastWhen` 的 ③ 段）
        ///   ⇒ 只影响「被抽那张自己的监听器」与棋盘监听器之间的先后。现读池内**0 张场上卡监听 `draw`**
        ///   （监听者只有 `BL15` / `DA31` 两张**手牌单位卡**）⇒ 今天无可观察差异；
        ///   **池内出现场上卡监听 `draw` 时必须重判**。
        /// ⚠️ 被抽的**陷阱卡**走原版另一条钩子（`CardScript__CardDrawn.c:23` → `OnTrapDrawn`
        ///   → `OnTrigger(0xfa = 250)`），本实现只有 260 那条 ⇒ **250 那条未实现**（已记进报告）。</para>
        /// </summary>
        static bool DrawHandHopTakes(int hop, CardInstance inst, CardInstance drawnCard)
        {
            bool isDrawn = drawnCard != null && ReferenceEquals(inst, drawnCard);
            return hop == 0 ? isDrawn : !isDrawn;
        }

        /// <summary>**账 627**：本次「抽到了」的那一份实例（`hop 0` 的预调用它）。取自
        /// <see cref="BattleContext.DrawnThisResolve"/> —— `RuleCore.Draw` 用 `DrawReferentScope`
        /// 把窗口种成**恰好 [刚抽到的那一份]**（见那个 scope 的 doc）。
        /// 不是 `draw` 事件、或槽里不是恰好一份 ⇒ `null`（= 不预调、也不排除 ⇒ 退化成「整手各两遍」）。</summary>
        static CardInstance DrawnNow(BattleContext ctx, string kind)
        {
            if (ctx == null || kind != WhenEventKind.Draw) return null;
            var d = ctx.DrawnThisResolve;
            return d != null && d.Count == 1 ? d[0] : null;
        }

        /// <summary>**账 690** 用：`duration` → 原版那几档 `until*` 的等价枚举。
        /// 逐档与 `AttachHandEffect` 里的 `switch (duration)` 相同（⛔ 两处别写岔；
        /// 这里只给**去重比较**用，不写回 entry）。</summary>
        static CardInstance.HandBuffExpiry HandExpireOf(string duration)
        {
            if (duration == "turn") return CardInstance.HandBuffExpiry.EndOfTurn;
            if (duration == "nextturn") return CardInstance.HandBuffExpiry.OwnerTurnStart;
            return CardInstance.HandBuffExpiry.Never;
        }

        /// <summary>
        /// 🆕 **2026-10-27（B9 · 第三批 · `E6ENG` · 账 703 / 1272）：`CardScript.CardDrawn` 的
        /// 「被抽到的那张**自己**」那一跳里的 `trap + armed ⇒ 250` 分支。**
        ///
        /// <para>**判据（🥇 Ghidra `CardScript__CardDrawn.c:16-31`；ILSpy `CardScript.cs` 交叉核过同一形状）**：
        /// `CardDrawn(this, drawnCard)` 两条路**互斥** ——
        /// · `drawnCard != this` ⇒ `OnCardDrawn` ⇒ trigger **260** `OtherCardDrawn`（`actingCard = drawnCard`）；
        /// · `drawnCard == this` 且 `isTrapCard(this)` 且 `armedTrap(+0x54)` ⇒ `OnTrapDrawn`
        ///   ⇒ trigger **250** `ThisCardDrawnAsTrap`（`actingCard` / `targetCard` **都是 `default`**；
        ///   `RawCardScript__OnTrapDrawn.c` 全文 = `OnTrigger(0xfa = 250, …, this, default, default)`），
        ///   然后**直接 `return`** ⇒ **这一发取代它的 260**。</para>
        ///
        /// <para>⛔ **只有预调那一跳能走到 250**：棋盘跳与两趟手牌跳都只把事件传给 `!= drawnCard` 的接收者
        /// （`BattleManagerSupport__BroadcastCardDrawn.c:27-76` / ILSpy `:710-743`）⇒ 调用方只允许在
        /// `hop 0` 且 `inst == drawnCard` 时调本方法。</para>
        ///
        /// <para>**三态（`CardInstance.ArmedTrap`：`1` 已布设 / `0` 判得出不是 / `-1` 判不了）**：
        /// `-1` ⇒ **出声**（⛔ 不当 0）并落回 260 那条保守路；`ArmedTrap == 1` 而 `CardDef.IsTrapCard` 不成
        /// `true` ⇒ 也出声（理论上不该发生 —— `ArmedTrap` 就是由 `IsTrapCard` 算出来的，两处打架就是数据坏了）。</para>
        ///
        /// <para>🔴 **效果层今天没有判据**：池内 / 全 SO（`cards_truth.json` 1,351 张）里
        /// `abilities[].trigger == 250` 的 **0 行**（对照值：260 = 2 行 · 665 = 1 行 · 695 = 3 行 ⇒ 扫描有效），
        /// `spellType == 70`（`SpellType.Trap`）**0 张** ⇒ 本方法**只发事件 + 出声**，
        /// ⛔ **不发明效果**（`chosenAbility` 挂的是什么、打谁 —— 一律无据）。
        /// 事件那一层走 <see cref="EmitCardTrigger"/>：它的 presence 闸读
        /// `CardDef.AbilityTriggerNumericNames` 里**已有**的 `{"ThisCardDrawnAsTrap", 250}`，开箱可用；
        /// ⛔ **不为它新建 `WhenEventKind` 词形** —— 250 是**结构化 trigger**，不是卡面写法
        /// （判据 + 理由 → `报告/B9_E6ENG_广播四件与光环残骸.md` §⑪）。</para>
        /// </summary>
        /// <returns>`true` = **已按 250 处理** ⇒ 调用方必须 `continue`（这一张**不再走** 260 那条路）。</returns>
        static bool EmitTrapDrawnInsteadOfCardDrawn(BattleContext ctx, CardInstance inst, int owner)
        {
            if (ctx == null || inst == null || inst.Card == null) return false;
            int armed = inst.ArmedTrap;
            if (armed == CardInstance.ArmedTrapUnknown)
            {
                NoteTrapDrawnOnce(inst.Card, "armedTrap 判不了", ($"[RuleEngine] 「{inst.Card.Name}」被抽到时的 `armedTrap` **判不了**"
                    + "（`CardInstance.ArmedTrap == -1`：卡表缺 `cardType` 列 / 这张卡不在真值快照里）"
                    + " ⇒ 按 **260** 走、**没有**按 250 处理（⛔ 未知不当 0；这里选保守那一支并出声）。"));
                return false;
            }
            if (armed != 1) return false;
            string why;
            if (inst.Card.IsTrapCard(out why) != true)
            {
                NoteTrapDrawnOnce(inst.Card, "armedTrap 与 isTrapCard 打架", ($"[RuleEngine] 「{inst.Card.Name}」的 `ArmedTrap == 1` 但 `IsTrapCard` 不成 true"
                    + $"（{why ?? "判得出是 false"}）—— 两处判据打架（`ArmedTrap` 本来就由 `IsTrapCard` 算出）⇒ 按 260 走。"));
                return false;
            }
            bool emitted = EmitCardTrigger(ctx, inst.Card, owner, null, "ThisCardDrawnAsTrap", "trapDrawn");
            NoteTrapDrawnOnce(inst.Card, "武装陷阱被抽到", ($"[RuleEngine] 「{inst.Card.Name}」是**被抽到的武装陷阱** ⇒ 原版这一跳发 trigger"
                + " **250 `ThisCardDrawnAsTrap`**（`CardScript__CardDrawn.c:16-31`），并**取代**它的 260。"
                + $"⚠️ 事件{(emitted ? "已发" : "**没发**（这张卡的卡表里没有 250 那条 ability）")}；"
                + "**效果层本仓无判据**（池内 / 全 SO 里 trigger 250 的 ability = 0 条）⇒ **不执行效果**，⛔ 不发明。"));
            return true;
        }

        /// <summary>
        /// 🆕 **2026-10-27（B9 · 第五批 · `E6ENG` · F2）：`EmitTrapDrawnInsteadOfCardDrawn` 三条告警的「每张卡一次」闩。**
        ///
        /// <para>**为什么需要**：那三条告警都**按抽牌次数**触发 —— `ArmedTrap == -1` 那一态在卡表旧一档时会对
        /// **每一张**卡的**每一次**抽牌各喊一遍（`E7ENG` 报的噪音）。而这三条话术的**信息量是「每张卡」级的**
        /// （「这张卡判不了」/「这张卡两处判据打架」/「这张卡是武装陷阱、效果层没判据」），不是「每一次」级的。</para>
        ///
        /// <para>⛔ **只闩日志，不闩事件** —— `EmitCardTrigger` 那一发每次都照发（账 703 的验收读数
        /// 就是数「250 事件几条」，闩了它等于把机制关掉）。形状与 <see cref="NoteAbilityWithoutAnimRow"/> 同族
        /// （同一 `(卡, 原因)` 只喊一次；⛔ 别加回 `ctx.Log`，那会动 `ctx.Events.Count` / 联机 Fingerprint）。</para>
        /// </summary>
        static readonly HashSet<string> _trapDrawnNoted = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>见 <see cref="_trapDrawnNoted"/>：同一张卡的同一类告警只喊一次。</summary>
        static void NoteTrapDrawnOnce(CardDef card, string reason, string message)
        {
            string id = card != null ? (card.Id ?? card.Name) : null;
            if (!_trapDrawnNoted.Add((id ?? "?") + "|" + reason)) return;
            UnityEngine.Debug.LogWarning(message);
        }
    }
}
