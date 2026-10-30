// RuleCorePartial_B103W5c.cs — 第一百零三会话 · 波 1 · 执行代理 `B103_W5c` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 365+ 处
//   ⇒ 往它中间插成员会让下游落点整体下移、`check_ledger_points` 大批报漂移。
//   本文件与 `RuleCorePartial_B103W5.cs` / `RuleCorePartial_B88E1.cs` 同形：**新成员一律落在这里**
//   （本文件没有任何账的落点）。`EffectResolver.cs` / `ActionPump.cs` 也是 `partial class RuleCore`
//   ⇒ 本文件能直接调它们里的 `static` 成员（同一个类的另一半，⛔ 不是越权）。
//
// 本批（`B103_W5c`）落在本文件的东西 —— 账 `战斗逻辑驱动:1282` / `:1380`
// （`RawCardScript.ResolveActiveAbility` 的两个重载）里**休眠·机制真缺**那批中，
// 「原版这一支 = 逐目标调一个原子动作」的最后两支：
//   · `RunDisappearFromHandRow` ← `chosenAbility 162 disappearFromHand`
//   · `RunDisplayFromHandRow`   ← `chosenAbility 310 displayFromHand`
// 两支的**动作类型**（入队口 + 出队处理口）在 `ActionPump.cs` 类尾（见那里 2026-10-30 那一段）：
//   `ActionTypeDisappearFromHand = 56` / `ActionTypeDisplayFromHand = 57`。
//
// 施工单 / 分批表 → `资料/完全复刻/判据/B102_R4_C类大工程两条.md` §①，⛔ 不在这里抄第二份。
// 判据出处逐条写在各自方法头上（铁律 2/3：没有出处就写「查不到」）。
//
// ⚠️ **判据（第一权威，逐句现读 `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c`）**：
//   · `:605-622`（`case 0xa2 = 162`）⇒ `while` 遍历 `possibleTargets(param_1 + 0x78)`，逐个
//     `BattleManager__AddDisappearFromHand(bm, 那个目标, abilityData.priority(+0x44),
//       abilityData.blockPlayerActions(+0x48), 0)`；
//   · `:2227-2242`（`case 0x136 = 310`）⇒ 同形，只有被调的方法换成 `AddDisplayFromHand`。
//   ⇒ **两支的差别只在「入队哪一个动作」，逐目标的形状完全一致** ⇒ 本文件把「挑一侧手牌里的哪一张」
//     收成一处（`B103W5cPickHandCard`），两个 Row 只负责各自入队。
//
// 🔴 **如实标着的口径差（与 `RunDiscardFromHandRow` 同一条边界，⛔ 不是新缺口）**：
//   原版的 `possibleTargets` 是 `AbilityLogic.GetTargets` 按 `targetCriteria` 在**手牌**里当场筛出来的
//   那几张**具体手牌**；我们这一层 `ResolveTargets` 只到 `UnitState`（手牌是 `CardInstance`），
//   没有实例级 criteria 收口口 ⇒ 只做到「受影响那一侧的**手牌全集**里挑一份」，
//   由 `PickStealTarget`（全仓唯一的「区域里挑一份实例」口）承担，并**出声**。
//   ⛔ 不许为了「看起来一致」在这里扫一整侧手牌全动 —— 那是别的分支的语义，会静默多动牌。
//
// 📊 可达性：`cards_truth.json` 1,351 张现扫 `chosenAbility` ∈ {162, 310} = **0 / 0**
//   （`cards_engine.json` 池内 `activeAbilityLogic` 里同样 0 行）⇒ **休眠件**（铁律 11 照建）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  共用 ①：读这一行（行下标 `row`、`chosenAbility == ca` 那一行）的 `playersAffected`
        // ==================================================================
        //  原版 `AbilityLogic + 0x20`（`playersAffected`：`0` 场上的牌/单位 · `10` 自己 · `20` 对方）。
        //  载体 = 卡表 v24 的逐行那一列（`CardDef.ActiveAbilityLogicSpec.PlayersAffected`）。
        //  🔴 **读不到 ⇒ 返回 `-1`（⛔ 不猜 `0 Both`）**：多候选 / 该行没有这一列 / 下标越界
        //     一律当「判不出」⇒ 调用方出声 + 这一行不结算。
        //  ⚠️ **为什么不复用 `PlayersAffectedOf`**（`EffectResolverPartial_B88E2.cs:819`）：它读的是
        //     `ctx.ActingUnit / ctx.PlayingCard`（那条依赖 `ctx.ActingUnit` 在结算期被设好），
        //     而这一族的入口 `RunActiveRowOpsOrGrantedTrigger` 手上**已经有 `u`**（施放者本人）
        //     ⇒ 直接从 `u.Card` 取，少一跳依赖。**两条读的是同一列**（同一份判据），⛔ 不是第二份数据。
        static int B103W5cPlayersAffected(UnitState u, int row, int ca)
        {
            var spec = B103W5RowSpecOf(u, row, ca);
            return spec != null ? ActiveRowIntOf(spec.PlayersAffected, row) : -1;
        }

        // ==================================================================
        //  共用 ②：从某一侧手牌里挑一份**实例**
        // ==================================================================
        //  复用 `PickStealTarget`（`EffectResolver.cs:17410`）—— 全仓**唯一**的「从某个区域表里挑一份实例」口
        //  （面板给了 `ChooseCardIds` / `ChoosePicks` 就按面板；否则等概率；`ctx.LastChosenCard` 优先）。
        //  ⛔ 不另写一份挑牌逻辑（两处写同一条规则 = 迟早不一致）。
        static CardInstance B103W5cPickHandCard(BattleContext ctx, int seat, int ownerSeat, string caName,
                                                out List<string> unresolved)
        {
            unresolved = new List<string>();
            var ps = ctx.Players[seat];
            if (ps == null || ps.Hand == null || ps.Hand.Count == 0)
            {
                ctx.Log($"⚠️「{caName}」：第 {seat} 号位的手牌是**空的** ⇒ 这一行不结算（原版 `possibleTargets` 也是空表）");
                return null;
            }
            return PickStealTarget(ctx, ps.Hand, ctx.LastChosenCard,
                                   caName, caName,
                                   (seat == ownerSeat ? "本侧" : "对面") + "的手牌",
                                   unresolved);
        }

        // ==================================================================
        //  ca 162 —— `disappearFromHand`（原版 `AbilityLogic__PlayAbility.c:605-622`）
        // ==================================================================
        // **判据（逐句现读）**：`if (iVar8 == 0xa2)` ⇒ `while (iVar20 < targets.Count)`：
        //   `BattleManager__AddDisappearFromHand(bm, targets[i], *(abilityData + 0x44) /*priority*/,
        //     *(abilityData + 0x48) /*blockPlayerActions*/, 0)`
        //   ⇒ 每个目标入队**一条** `disappearFromHand(56)` 动作（动作号判据 = `BattleManager__AddDisappearFromHand.c:23`
        //   的 `0x38`）。结算体 = `_ResolveDisappearFromHand_d__545`（逐跳见
        //   `RuleCore.ResolveDisappearFromHand` 的 doc：`IsInHand` 闸 → `RemoveCostEffects` → `DestroyCard`
        //   → `GoToCemetery`（ephemeral ⇒ 移出游戏 / 否则 ⇒ 进弃牌堆）→ `RemoveCardFromHand`）。
        // 📊 可达性：池内 `chosenAbility 162` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 162 = disappearFromHand` 的行执行器（逐目标把一张手牌送走）。</summary>
        /// <returns>成功入队的目标数（0 = 判不出哪一侧手牌 / 那一侧手牌为空 / 入队被拒）。</returns>
        internal static int RunDisappearFromHandRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || u.Card == null || (p != 0 && p != 1)) return 0;
            int pa = B103W5cPlayersAffected(u, row, 162);
            if (pa != 10 && pa != 20)
            {
                ctx.Log($"⚠️「{u.Name}」的主动能力 `chosenAbility 162`（disappearFromHand）：`playersAffected` 读不到或不是 10/20"
                      + "（原版读的是 `AbilityLogic.playersAffected`）⇒ 判不出**动哪一侧的手牌**，本版**不猜**、这一行不结算"
                      + "（`0 Both` 原版会两侧各来一遍，我们这条只动一侧 ⇒ 同样不猜）");
                return 0;
            }
            int seat = pa == 10 ? p : 1 - p;
            List<string> unresolved;
            var card = B103W5cPickHandCard(ctx, seat, p, "162 disappearFromHand", out unresolved);
            if (card == null)
            {
                ReportUnresolved(ctx, "「" + u.Name + "」的主动能力（disappearFromHand）", unresolved);
                return 0;
            }
            // 原版逐目标各入队一条（`.c:617` 在 `while` 里）；我们走同一个入队口。
            // ⚠️ 道按 `Instant`（判据 `CardAbility__ctor.c:5` 的缺省 `+0x18 = 1`；与 `RunRemoveFromCemeteryRow` 同一条口径）。
            bool ok = EnqueueDisappearFromHand(ctx, seat, card, ActionPriority.Instant);
            ReportUnresolved(ctx, "「" + u.Name + "」的主动能力（disappearFromHand）", unresolved);
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 162`（disappearFromHand）："
                  + (ok ? $"给 {(seat == p ? "自己" : "对面")}手牌的「{card.Card.Name}」入队一条「从手牌消失」"
                        : "**没有入队**（见上一条日志）")
                  + "（原版 `AbilityLogic__PlayAbility.c:605-622` 逐目标 `AddDisappearFromHand`；"
                  + "结算体 = `_ResolveDisappearFromHand_d__545`，含 `RemoveCostEffects`）");
            return ok ? 1 : 0;
        }

        // ==================================================================
        //  ca 310 —— `displayFromHand`（原版 `AbilityLogic__PlayAbility.c:2227-2242`）
        // ==================================================================
        // **判据（逐句现读）**：`if (iVar8 == 0x136)` ⇒ `while (… targets …)`：
        //   `BattleManager__AddDisplayFromHand(bm, targets[i], *(abilityData + 0x44) /*priority*/,
        //     *(abilityData + 0x48) /*blockPlayerActions*/, 0)`
        //   ⇒ 每个目标入队**一条** `displayFromHand(57)` 动作（动作号判据 = `BattleManager__AddDisplayFromHand.c:23`
        //   的 `0x39`）。结算体 = `_ResolveDisplayFromHand_d__548`（逐跳见
        //   `RuleCore.ResolveDisplayFromHand` 的 doc —— 🔴 它不是「零写入」：会往墓地动作日志追加一条
        //   `displayedCard(30)`；牌本身**留在手牌里**，其余全是表现层）。
        // 📊 可达性：池内 `chosenAbility 310` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 310 = displayFromHand` 的行执行器（逐目标展示一张手牌）。</summary>
        /// <returns>成功入队的目标数。</returns>
        internal static int RunDisplayFromHandRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || u.Card == null || (p != 0 && p != 1)) return 0;
            int pa = B103W5cPlayersAffected(u, row, 310);
            if (pa != 10 && pa != 20)
            {
                ctx.Log($"⚠️「{u.Name}」的主动能力 `chosenAbility 310`（displayFromHand）：`playersAffected` 读不到或不是 10/20"
                      + " ⇒ 判不出**展示哪一侧的手牌**，本版**不猜**、这一行不结算"
                      + "（`0 Both` 原版两侧各来一遍 ⇒ 同样不猜）");
                return 0;
            }
            int seat = pa == 10 ? p : 1 - p;
            List<string> unresolved;
            var card = B103W5cPickHandCard(ctx, seat, p, "310 displayFromHand", out unresolved);
            if (card == null)
            {
                ReportUnresolved(ctx, "「" + u.Name + "」的主动能力（displayFromHand）", unresolved);
                return 0;
            }
            bool ok = EnqueueDisplayFromHand(ctx, seat, card, ActionPriority.Instant);
            ReportUnresolved(ctx, "「" + u.Name + "」的主动能力（displayFromHand）", unresolved);
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 310`（displayFromHand）："
                  + (ok ? $"给 {(seat == p ? "自己" : "对面")}手牌的「{card.Card.Name}」入队一条「展示手牌」"
                        : "**没有入队**（见上一条日志）")
                  + "（原版 `AbilityLogic__PlayAbility.c:2227-2242` 逐目标 `AddDisplayFromHand`）");
            return ok ? 1 : 0;
        }

        // ==================================================================
        //  自检门（⚠️ 产品路径 ⛔ 别调；`Editor/B103W5_Assert.cs` 与将来的探针共用）
        // ==================================================================
        /// <summary>自检门：只转调 <see cref="RunDisappearFromHandRow"/>。</summary>
        public static int RunDisappearFromHandRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunDisappearFromHandRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunDisplayFromHandRow"/>。</summary>
        public static int RunDisplayFromHandRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunDisplayFromHandRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：直接点测**出队处理口** `disappearFromHand(56)`（绕过队列 —— 队列由 `CaseGate` 那一段单独钉）。</summary>
        public static int ResolveDisappearFromHandForTest(BattleContext ctx, int seat, CardInstance card)
        { return ResolveDisappearFromHand(ctx, seat, card); }

        /// <summary>自检门：直接点测**出队处理口** `displayFromHand(57)`。</summary>
        public static int ResolveDisplayFromHandForTest(BattleContext ctx, int seat, CardInstance card)
        { return ResolveDisplayFromHand(ctx, seat, card); }
    }
}
