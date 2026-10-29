// B97W5_RowsAndDedupe.cs — 第九十七会话 · 波 1 · 执行代理 `B97_W5`
//
// 🔴 **为什么另起一个文件、且文件名不用 `EffectResolverPartial_*` / `RuleCorePartial_*`**：
//   ① 全仓**没有** `EffectResolver` 这个类 —— `EffectResolver.cs` 与 `RuleCore.cs` 都是
//      `public static partial class RuleCore`（见 `EffectResolver.cs:27`；别按文件名写 `EffectResolver.`）；
//   ② 那两种前缀的文件名**已分别归本波 `B97_W1`（`RuleCore*`）/ `B97_W2`（`EffectResolver*`）所有**
//      （文件所有权表）—— 换一个不撞名的文件，两个写手就不会互相覆盖。
//
// 本文件装两件（两件都是**休眠件**，铁律 11：休眠也做，⛔ 不判 D）：
//   ① 账 `:850`（＋独立账 `BL37`）：`chosenAbility 160 discardFromHand` / `165 discardWholeHand`
//      的**生产端分派行**。原版在 `AbilityLogic__PlayAbility.c:577-603` / `:293-345` 逐目标组
//      `AddDiscardFromHand` 动作；那两个动作的结算体（`_ResolveDiscardFromHand_d__544` →
//      `RemoveCostEffects` → `RemoveCardFromHand`）我们**早就落齐了**
//      （`EffectResolverPartial_B88E2.cs:681 DoDiscardFromHand` / `:705 DoDiscardWholeHand` →
//       `DiscardHandCardCore:749` → `RuleCore.RemoveCostEffects:14760`）。
//      ⇒ 真缺口只是「**没有生产端**」：`EffectText` 对这两个动词 0 生产者（卡面里也没有
//      `discard` 的句式），而 `RunActiveRowOpsOrGrantedTrigger` 的 `ca` 分派表里没有 160/165 两支。
//      本文件补的就是那两支（**接在既有的结算体上**，⛔ 不另写第二份搬运逻辑）。
//   ② 账 `:1254`：`RawCardScript.IsCumulativeMutation` 的**下游唯一读点**
//      `CardScript.HasSingleUseMutation(caster, targetCard)` —— 逐句等价位。
//      （谓词本身 `CardDef.IsCumulativeMutation` 已于 `CardDefPartial_B94B16.cs:274` 落地、数据列 v41 已落。）
//
// 📊 可达性（现读，如实标）：
//   · `chosenAbility 160`：池内 **0 张**（`cards_truth.json` 1,351 张里唯一那一行落在 BL37 的
//     **重复 SO** `f3df0cfc…`，池内那张记录是 30）· `165` = **0 张**；
//   · `trait 780 (mutation)` 池内 **0 张** · `mutationType == 20` **0 行** ⇒ ② 也休眠。
//   ⇒ 本文件两条都**零行为变化**。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==========================================================================================
        //  ① 账 `:850` / `BL37` —— `chosenAbility 160 / 165` 的行分派
        // ==========================================================================================
        //  判据（🥇 `decomp_full/AbilityLogic__PlayAbility.c` 逐句现读）：
        //   · `:577-603` `iVar8 == 0xa0 (160)`：`while` 遍历 `possibleTargets(param_1 + 0x78)`，
        //     逐个 `BattleManager__AddDiscardFromHand(bm, 那个目标, priority = ability+0x44,
        //     blockPlayerActions = abilityData+0x48, 0)`；
        //   · `:293-345` `iVar8 == 0xa5 (165)`：先 `GetCardsInHandList(inclMinions=1, inclSpells=1,
        //     inclWhispers=0, inclWeapons=1, affected(己), affected(敌))`，再逐张同一条 `AddDiscardFromHand`。
        //   ⇒ **两支的差别只在「候选池从哪来」**，结算体完全是同一条 `_ResolveDiscardFromHand`。
        //     我们这边候选池的来源已经在那两个 handler 里各写了一份（`DoDiscardFromHand` 从一侧手牌挑、
        //     `DoDiscardWholeHand` 按 `playersAffected` 两侧整手）⇒ 本文件**只做分派**，⛔ 不碰候选池。
        //  🔴 **`op` 是合成的**（原版这两个能力没有「正文 op」，`chosenAbility` 就是全部语义）——
        //     合成一条最小 op 只是为了复用既有 handler 的 `op.Source` / `op.Target.Side` 两个读取点；
        //     `Target.Side` 取 `null` ⇒ handler 走它自己的默认侧（与 `stealhand` 同一条默认）。

        /// <summary>整条能力的 `op.Source` 文案（日志里那张卡名）；`u` 为空时回落到能力种类名。
        /// <para>`AbilityIndex` / `LogicIndex` 按调用方给的 <paramref name="row"/> 填 —— `DoDiscardWholeHand`
        /// 要靠 `op.LogicIndex` 去读 `playersAffected`（`PlayersAffectedOf`：`< 0` ⇒ 读不到 ⇒ 按原版 SO 缺省
        /// `0 = Both` 并**出声**）。⛔ 别留 `-1`：那会让「只弃本侧」静默变成「两侧全弃」。</para></summary>
        static EffectOp SyntheticRowOp(UnitState u, string verb, int row)
        {
            var op = new EffectOp();
            op.Verb = verb;
            op.Source = (u != null && u.Card != null) ? u.Card.Name : verb;
            op.Target = null;
            op.AbilityIndex = 0;            // 主动能力对象那一组（`CardDef.ActiveAbilityObject`）
            op.LogicIndex = row;
            return op;
        }

        /// <summary>
        /// **`chosenAbility 160 (discardFromHand)` 的生产端分派**
        /// （判据 `AbilityLogic__PlayAbility.c:577-603`，见本文件头的逐句摘要）。
        /// <para>结算体 = `DoDiscardFromHand`（`EffectResolverPartial_B88E2.cs:681`）——
        /// 它在「以销毁方式离手」那一刻调 `RuleCore.RemoveCostEffects`（账 `:850` 的那一半）。</para>
        /// <para>⚠️ **候选池的口径差（如实标）**：原版的 `possibleTargets` 是 `GetTargets(ta=…手牌族)` 筛出来的
        /// **具体那几张手牌**；我们这一层手牌不是 `UnitState`、`ResolveTargets` 到不了 ⇒ 由 handler 自己
        /// 「在受影响那一侧的手牌全集里挑一份」（面板给了就按面板、否则等概率），并**出声**。
        /// ⛔ 别为了「看起来一致」在这里扫一整侧手牌全弃 —— 那是 `165` 的语义，会静默多弃。</para>
        /// <para>🔴 **哪一侧**这一格**不猜**：取 `playersAffected`（`AbilityLogic.playersAffected`，
        /// 载体 = 卡表 v24 那一列，`PlayersAffectedOf` 读）。`0 Both` 原版会**两侧各来一遍**，而我们这条
        /// handler 一次只动一侧 ⇒ `Both` 也**出声 + 不结算**（⛔ 别默认成「对面」：那正是改前
        /// `DiscardHandSeatFor` 的缺省行为，会静默弃错人）。</para>
        /// <para>📊 池内 `chosenAbility == 160` = **0 张**（1,351 张里唯一那一行在 BL37 的重复 SO）⇒ 休眠件。</para>
        /// </summary>
        public static int RunDiscardFromHandRow(BattleContext ctx, int p, UnitState u,
                                                  int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || u.Card == null) return 0;
            var op = SyntheticRowOp(u, "discardfromhand", row);
            int pa = PlayersAffectedOf(ctx, op, u.Name);
            if (pa != 10 && pa != 20)
            {
                ctx.Log($"⚠️「{u.Name}」的 `chosenAbility 160`（discardFromHand）：`playersAffected` = "
                      + (pa < 0 ? "**读不到**" : pa.ToString()) + "（原版读的是 `AbilityLogic.playersAffected`）"
                      + " ⇒ 判不出**弃哪一侧的手牌**，本版**不猜**、这一行不结算"
                      + "（`0 Both` 原版两侧各来一遍，而本 handler 一次只动一侧 ⇒ 同样不猜）");
                return 0;
            }
            op.Target = new EffectTargetSpec { Side = pa == 10 ? "own" : "enemy", Kind = "any" };
            var unresolved = new List<string>();
            bool ok = DoDiscardFromHand(ctx, p, u.Name, op, chosen, unresolved);
            if (unresolved.Count > 0)
                ctx.Log($"「{u.Name}」的 `chosenAbility 160`（discardFromHand）：{string.Join("；", unresolved)}");
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 160`（discardFromHand）："
                  + (ok ? $"弃掉了 {(pa == 10 ? "自己" : "对面")} 1 张手牌" : "**没有弃成**（见上一条日志）")
                  + "（原版 `AbilityLogic__PlayAbility.c:577-603` 逐目标 `AddDiscardFromHand`；"
                  + "结算体同 `_ResolveDiscardFromHand`，含 `RemoveCostEffects`）");
            return ok ? 1 : 0;
        }

        /// <summary>
        /// **`chosenAbility 165 (discardWholeHand)` 的生产端分派**
        /// （判据 `AbilityLogic__PlayAbility.c:293-345`；六格实参的口径 → `DoDiscardWholeHand` 的 doc）。
        /// <para>结算体 = `DoDiscardWholeHand`（`EffectResolverPartial_B88E2.cs:705`）：按 `playersAffected`
        /// 决定弃哪一侧/两侧的整手牌，**每张都走同一条** `DiscardHandCardCore`（含 `RemoveCostEffects`）。</para>
        /// <para>📊 池内 `chosenAbility == 165` = **0 张** ⇒ 休眠件。</para>
        /// </summary>
        public static int RunDiscardWholeHandRow(BattleContext ctx, int p, UnitState u,
                                                   int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || u.Card == null) return 0;
            var op = SyntheticRowOp(u, "discardwholehand", row);
            int pa = PlayersAffectedOf(ctx, op, u.Name);
            if (pa != 0 && pa != 10 && pa != 20)
            {
                ctx.Log($"⚠️「{u.Name}」的 `chosenAbility 165`（discardWholeHand）：`playersAffected` = "
                      + (pa < 0 ? "**读不到**" : pa.ToString())
                      + " ⇒ 判不出弃哪一侧，本版**不猜**、这一行不结算"
                      + "（原版 SO 缺省 `0 Both`；我们宁可不动，也不静默把两侧全弃）");
                return 0;
            }
            var unresolved = new List<string>();
            bool ok = DoDiscardWholeHand(ctx, p, u.Name, op, chosen, unresolved);
            if (unresolved.Count > 0)
                ctx.Log($"「{u.Name}」的 `chosenAbility 165`（discardWholeHand）：{string.Join("；", unresolved)}");
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 165`（discardWholeHand）："
                  + (ok ? "整手牌已按 `playersAffected` 处理" : "**没有弃成**（见上一条日志）")
                  + "（原版 `AbilityLogic__PlayAbility.c:293-345`）");
            return ok ? 1 : 0;
        }

        // ==========================================================================================
        //  ② 账 `:1254` —— 原版 `CardScript.HasSingleUseMutation(CardScript card)`
        // ==========================================================================================
        //  判据（🥇 `decomp_full/CardScript__HasSingleUseMutation.c:1-60` 逐句现读）：
        //    ① `if (card == null) return 0;`
        //    ② `if (RawCardScript.IsCumulativeMutation(card) != 0) return 0;`
        //       ⇒ **累计型变异不参与去重**（多条可以叠）。
        //    ③ 扫 `this.activeEffects(+0x108)`，逐个比 `CardEffect.enchantment(+0x88)` 是不是 `card`
        //       —— 命中 ⇒ `return 1`。扫完没有 ⇒ `0`。
        //  我们的等价物：
        //    · `+0x108` = `CardInstance.ActiveEffects`（`List<ActiveCardEffect>`，`CardInstance.cs:139`）；
        //    · `+0x88` = `ActiveCardEffect.EnchantmentCard`（`Enchantment.cs:78`，类型就是 `CardDef`）；
        //    · `IsCumulativeMutation` = `CardDef.IsCumulativeMutation()`（`CardDefPartial_B94B16.cs:274`）。
        //  🔴 **`this`（施放者）是 `CardInstance`，`card`（候选）我们手上是 `CardDef`** ——
        //    原版两边都是 `CardScript`；我们这一层「候选」在池子里只有模板 ⇒ 按**模板引用相等**比
        //    （`ActiveCardEffect.EnchantmentCard` 本身也是模板 ⇒ 两边同一层，不会错配）。

        /// <summary>原版 **`CardScript.HasSingleUseMutation(CardScript card)`** —— 「这条**单次**变异
        /// 已经挂在这个施放者身上了吗」。
        /// <para>`true` ⇒ `AbilityLogic.GetTargets` 会把这个候选**从池子里剔掉**
        /// （判据 `AbilityLogic__GetTargets.c:1370-1385`：`traitsFilter == 0x30c (780 mutation)` 时逐候选调它）。</para>
        /// <para>⚠️ `card.IsCumulativeMutation()` 为真 ⇒ **恒 `false`**（累计型可以叠 —— 原版 `.c:22-27`）。
        /// ⛔ 别把这条当成「已经挂过 ⇒ 不能再挂」的通用去重：它只对**单次**变异成立。</para>
        /// </summary>
        public static bool HasSingleUseMutation(CardInstance caster, CardDef targetCard)
        {
            if (targetCard == null) return false;                       // `.c:19-21`
            if (targetCard.IsCumulativeMutation()) return false;        // `.c:22-27`
            if (caster == null || caster.ActiveEffects == null) return false;
            for (int i = 0; i < caster.ActiveEffects.Count; i++)
            {
                var e = caster.ActiveEffects[i];
                if (e == null || e.EnchantmentCard == null) continue;
                if (ReferenceEquals(e.EnchantmentCard, targetCard)) return true;   // `.c:33-56`
            }
            return false;
        }

        /// <summary>↔ 原版 `AbilityLogic.GetTargets` 末尾那段「按 `traitsFilter == 780` 剔除已挂过的单次变异」。
        ///
        /// <para>🔑 **下面两个「取那一行 criteria 的某一格」的小助手**是给 `ResolveTargets` 那两处接线用的
        /// （`ResolveTargets` 在 `EffectResolver.cs`、不在本代理白名单 ⇒ 接线见报告里的可粘贴补丁）。
        /// 它们**只按 `op` 自带的 `AbilityIndex` / `LogicIndex` 去查卡表那一列**，⛔ 不猜、不在别处再存一份。</para>
        ///
        /// <para>判据（🥇 `decomp_full/AbilityLogic__GetTargets.c:155-172` ＋ `:1370-1385` 合读）：
        /// 候选池建好之后 ——
        /// <c>if (criteria.traitsFilter(+0x34) != 0x30c) skip;</c>
        /// <c>actingCard = actingCard ?? thisCard;</c>
        /// <c>for (i 倒序) if (HasSingleUseMutation(actingCard, pool[i])) RemoveAt(i);</c></para>
        ///
        /// <para>⚠️ 调用方**必须**把 `criteria` 的 `traitsFilter` 传进来（本方法自己再判一次 `780`）——
        /// ⛔ 别在别处无条件调它：那会把「不筛变异」的目标池也剔掉一半。</para>
        /// <para>`actingCard == null` ⇒ 原版回落 `thisCard`；我们这里 `caster` 为 `null` 时
        /// 同样**什么都不剔**并出声（宁可少剔也不多剔 —— 多剔是静默改强度）。</para>
        /// <para>📊 池内 `trait 780` = **0 张** ⇒ 休眠件（铁律 11）。</para>
        /// </summary>
        public static List<UnitState> FilterSingleUseMutationTargets(BattleContext ctx, List<UnitState> pool,
                                                                      CardInstance caster, int traitsFilter,
                                                                      string by = null)
        {
            const int MutationTrait = 780;      // `DefinedTrait.mutation`（`CardDefPartial_B94B16.cs:61`）
            if (pool == null || pool.Count == 0 || traitsFilter != MutationTrait) return pool;
            if (caster == null)
            {
                if (ctx != null)
                    ctx.Log($"⚠️ {(string.IsNullOrEmpty(by) ? "目标池" : by)}：criteria 的 `traitsFilter = 780 (mutation)`，"
                          + "但这一行拿不到施放者的**实例**（原版回落 `thisCard` 的 `activeEffects`）"
                          + " ⇒ 本版**不做单次变异去重**并出声（⛔ 不猜成「全剔」或「全留」）");
                return pool;
            }
            int before = pool.Count, removed = 0;
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                var t = pool[i];
                if (t == null || t.Card == null) continue;
                if (!HasSingleUseMutation(caster, t.Card)) continue;
                pool.RemoveAt(i); removed++;
            }

            if (removed > 0 && ctx != null)
                ctx.Log($"{(string.IsNullOrEmpty(by) ? "目标池" : by)}：`traitsFilter = 780 (mutation)` ⇒ 剔掉 "
                      + $"{removed} 个**已经挂过同一条单次变异**的候选（{before} → {pool.Count}）"
                      + "（原版 `AbilityLogic__GetTargets.c:1370-1385` + `CardScript__HasSingleUseMutation.c`）");
            return pool;
        }

        /// <summary>这条 op 所在那一行 criteria 的 `selectFromChooseResolution`（取不到 ⇒ `0`）。
        /// <para>`op.AbilityIndex >= 0` ⇒ 查 `cardAbilities` 那一列（`CardDef.AbilityTargetCriteriaAt`）；
        /// `< 0`（`-1` 主动 / `-2` 替代）⇒ 查主动能力对象那一列（`ActiveAbilityTargetCriteriaAt`）。</para></summary>
        public static int SelectFromChooseResolutionOf(UnitState source, EffectOp op)
        {
            var set = CriteriaRowOf(source, op);
            return set != null ? set.SelectFromChooseResolution : 0;
        }

        /// <summary>同上，取 `traitsFilter`（`null` 的那一行 ⇒ `0` = 不筛）。
        /// <para>⚠️ `0` 与「读不到」在**原版**是两个不同状态；本助手在「读不到」时也回 `0`，
        /// 所以调用方**只能**把它当「要不要进 780 那支」的开关 —— 真正的 `null` 语义由 `CountEval` 那边管。</para></summary>
        public static int TraitsFilterOf(UnitState source, EffectOp op)
        {
            var set = CriteriaRowOf(source, op);
            return set != null ? set.TraitsFilter : 0;
        }

        /// <summary>`op` 所指那一行的 `CountSetSpec`（`null` = 这张卡没有那一列 / 下标越界）。</summary>
        static CardDef.CountSetSpec CriteriaRowOf(UnitState source, EffectOp op)
        {
            if (source == null || source.Card == null || op == null) return null;
            if (op.AbilityIndex >= 0) return source.Card.AbilityTargetCriteriaAt(op.AbilityIndex, op.LogicIndex);
            return source.Card.ActiveAbilityTargetCriteriaAt(op.AbilityIndex, op.LogicIndex);
        }
    }
}
