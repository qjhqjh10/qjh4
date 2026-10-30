// RuleCorePartial_B103W5.cs — 第一百零三会话 · 波 1 · 执行代理 `B103_W5` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 365+ 处
//   ⇒ 往它中间插成员会让下游落点整体下移、`check_ledger_points` 大批报漂移。
//   本文件与 `RuleCorePartial_B88E1.cs` / `RuleCorePartial_B99E8.cs` 同形：**新成员一律落在这里**
//   （本文件没有任何账的落点）。
//   ⚠️ `EffectResolver.cs` 与 `ActionPump.cs` **也都是 `partial class RuleCore`**（现读文件头），
//   所以本文件能直接调它们里的 `private static Do*` —— 这不是越权，是同一个类的另一半。
//
// 本批（B103_W5）落在本文件的东西 —— 账 `战斗逻辑驱动:1282` / `:1380`
// （`RawCardScript.ResolveActiveAbility` 的两个重载）里**休眠·机制真缺**那批中，
// 「原版这一支 = 逐目标调一个原子动作」且**我们引擎已有那个原子动作**的几支：
//   · `RunResetActionsRow`       ← `chosenAbility 220 resetActions`
//   · `RunMissActionRow`         ← `chosenAbility 290 missAction`
//   · `RunRevealOrderRow`        ← `chosenAbility 280 revealOrder`
//   · `RunTriggerSacrificeRow`   ← `chosenAbility 375 triggerSacrifice`
//   · `RunDrawUntilEqualRow`     ← `chosenAbility 330 drawUntilEqual`
// 施工单 / 分批表 → `资料/完全复刻/判据/B102_R4_C类大工程两条.md` §①（1·3 表 + 1·4 分批），⛔ 不在这里抄第二份。
//
// ⚠️ **这五支都还没接进 ca 分派**（分派口在 `EffectResolver.RunActiveRowOpsOrGrantedTrigger`、
//   计划表闸在 `RuleCore.ActiveRowEntersPlan` —— **两个文件都不在本代理白名单**）。
//   可粘贴补丁见 `资料/完全复刻/报告/B103_W5.md` §④；本文件的方法体是**接线前就已经在**的执行体，
//   由 `Editor/B103W5_Assert.cs` 的 `…ForTest` 门直接点测。
//
// ⚠️ 判据出处逐条写在各自方法头上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  共用：这一行（行下标 `row`）**解目标表** —— 与 `RunGrantedTriggerRow` /
        //  `RunSetReferenceCardRow` / `RunHighlightBloodThirstRow` 同一套，⛔ 不另写一份解析
        // ==================================================================
        //  判据链（三条，逐句现读）：
        //   ① 这一行已被 `CardDef.AttachByAbilityKind` 配到正文 op ⇒ 借**那条 op 的 `Target`**
        //      走 `ResolveTargets`；
        //   ② 否则只有 `ta ∈ {30 target, 220}` 能用玩家点定的那个目标
        //      （`AbilityLogic__GetTargets.c:192-207`：整张函数只有这两档读 `param_5`）；
        //   ③ 其它 `ta` **不猜**：出声 + 这一行不结算（⛔ 不许退回「己方全体」那样近似的目标集）。
        //  ⚠️ 原版这一层读的是 `param_1 + 0x78`（这条 `AbilityLogic` 自己的目标表）——
        //     它由 `AbilityLogic.GetTargets` 按本行 `targetCriteria` 算出来；我们把它落在 op 上
        //     （`EffectOp.Target` / `TargetCriteriaSet`）⇒ ① 是同一个东西。
        /// <returns>`null` = 解不出（调用方**不结算**并已出声）；空表 = 解得出但一个目标都没有。</returns>
        static List<UnitState> B103W5RowTargets(BattleContext ctx, int p, UnitState u, int row, int ta,
                                                UnitState chosen, string caName)
        {
            var ops = ActiveRowOpsOf(u, row);
            var spec = (ops.Count > 0) ? ops[0].Target : null;
            if (spec != null) return ResolveTargets(ctx, p, spec, u, chosen, null, ops[0]);
            if ((ta == 30 || ta == 220) && chosen != null) return new List<UnitState> { chosen };
            ctx.Log($"⚠️「{u.Name}」的主动能力 `{caName}` **解不出目标集**（`ta={ta}`，这一行没有配到正文 op）"
                  + " ⇒ 本版**不猜**，这一行不结算。（目标集判据 = `AbilityLogic__GetTargets.c:192-207`，"
                  + "整张函数只有 `ta = 30/220` 读调用方给的那个目标）");
            return null;
        }

        /// <summary>取「行下标 <paramref name="row"/> 的那条 `activeAbilityLogic` 规格」。
        /// <para>用途：读**这一行自己**的 `playersAffected` / `furtherMultiplier`（原版
        /// `AbilityLogic + 0x20` / `local_178`）。`UseActiveAbilityCore` 的计划表只往下传行下标，
        /// 不传规格对象。</para>
        /// <para>🔴 **多候选 ⇒ 返回 `null`（不猜）**：同一张卡的 active / alternative 两组可以各有
        /// 一个「第 `row` 行」，而下标相同并不代表是同一条 logic ⇒ 判不出一律不认。</para></summary>
        static CardDef.ActiveAbilityLogicSpec B103W5RowSpecOf(UnitState u, int row, int ca)
        {
            if (u == null || u.Card == null || row < 0) return null;
            CardDef.ActiveAbilityLogicSpec hit = null;
            for (int g = 0; g < 2; g++)
            {
                var grp = u.Card.ActiveAbilityLogicGroup(
                    g == 0 ? CardDef.ActiveAbilityObject : CardDef.AlternativeAbilityObject);
                if (grp == null) continue;
                for (int i = 0; i < grp.Count; i++)
                {
                    var s = grp[i];
                    if (s == null || s.ChosenAbility == null || row >= s.ChosenAbility.Length) continue;
                    if (ActiveRowIntOf(s.ChosenAbility, row) != ca) continue;
                    if (hit == null) hit = s;
                    else if (!ReferenceEquals(hit, s)) return null;
                }
            }
            return hit;
        }

        // ==================================================================
        //  ca 220 —— `resetActions`（原版 `AbilityLogic__PlayAbility.c:2623-2638`）
        // ==================================================================
        // **判据（第一权威，逐句现读 `decomp_full/AbilityLogic__PlayAbility.c:2623-2638`）**：
        //   `if (iVar8 == 0xdc)` ⇒ `while (iVar20 < targets.Count)`：
        //     `BattleManager__AddResetActions(bm, targets[i], abilityData.priority(+0x44),
        //       logic.isPlayerTurn(+0x74), abilityData.thisCard(+0x10), blockPlayerActions(+0x48), 0)`
        //   `0xdc = 220`（`AbilityEffect.resetActions`）。**逐目标各排一条**，没有别的语义。
        //
        // **入队之后的结算** = `CardScript__ResetActions.c`（这一条腿我们早已实现）：
        //   `:18` 必须还是单位 · `:22-27` `cardState` 必须在场 · `:29-31` 计数 −1 · `:34` `ActivateMinion` 重算两位。
        //   🔴 我们的等价物 = **现成的** `RefundForcedAttackAction`（**一条规则只此一处**，
        //   ⛔ 别在这儿再写一份「减一 + 重算」）—— 它就是 `CardScript.ResetActions` 的逐句落点，
        //   文件头写在 `RuleCore.cs` 的「强制攻击退还」那一段。
        //   ⚠️ 它多一道 `ctx.Players[p].Board[atkSlot] != u ⇒ return` 的闸，那正是原版 `:22-27`
        //      「不在场 ⇒ 一个字段都不写」的等价物 ⇒ 语义一致。
        //   ⚠️ 它的日志文案写的是「强制攻击不消耗它的行动」（那条调用点的场景）——
        //      `AddResetActions` 走的是**同一个** `CardScript.ResetActions`，文案略偏但结算一致；如实标着。
        // 📊 可达性：`cards_truth.json` 1,351 张现扫 `chosenAbility 220` = **0**（`cards_engine.json` 池内同）
        //   ⇒ 休眠件（铁律 11：照样建出来）。
        /// <summary>`chosenAbility 220 = resetActions` 的行执行器（逐目标把行动额度退回去）。</summary>
        /// <returns>真结算的目标数（0 = 解不出目标 / 一个都没在场上）。</returns>
        internal static int RunResetActionsRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var targets = B103W5RowTargets(ctx, p, u, row, ta, chosen, "220 resetActions");
            if (targets == null) return 0;
            int done = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.IsAlive) continue;          // 原版 `:22-27`（不在场 ⇒ 不写字段）
                int op2, os2;
                if (!FindSlot(ctx, t, out op2, out os2)) continue;
                RefundForcedAttackAction(ctx, op2, os2, t);     // = 原版 `CardScript.ResetActions` 的落点
                done++;
            }
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 220`（resetActions）：给 {done}/{targets.Count} 个目标重置了行动"
                  + "（原版 `AbilityLogic__PlayAbility.c:2623-2638` 逐目标 `AddResetActions`；"
                  + "结算 = `CardScript.ResetActions.c`）");
            return done;
        }

        // ==================================================================
        //  ca 290 —— `missAction`（原版 `AbilityLogic__PlayAbility.c:2514-2532`）
        // ==================================================================
        // **判据（逐句现读）**：`if (iVar8 == 0x122)` ⇒ `while (iVar20 < targets.Count)`：
        //   `BattleManager__AddMissAction(bm, targets[i], abilityData.priority(+0x44),
        //     logic.isPlayerTurn(+0x74), abilityData.thisCard(+0x10), blockPlayerActions(+0x48), 0)`
        //   `0x122 = 290`（`AbilityEffect.missAction`）。**逐目标各排一条**。
        //
        // **入队之后的结算** = `CardScript__MissActions.c:1-33` —— 这一条腿我们早已实现，
        //   落点 = `RuleCore.MissActions(ctx, u)`（**一条规则只此一处**，⛔ 别在这儿再写一份「空放记账」）。
        // 📊 可达性：池内 `chosenAbility 290` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 290 = missAction` 的行执行器（逐目标记一次「空放」）。</summary>
        /// <returns>真记账的目标数。</returns>
        internal static int RunMissActionRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var targets = B103W5RowTargets(ctx, p, u, row, ta, chosen, "290 missAction");
            if (targets == null) return 0;
            int done = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null) continue;
                MissActions(ctx, t);                            // = 原版 `CardScript.MissActions` 的落点
                done++;
            }
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 290`（missAction）：给 {done}/{targets.Count} 个目标记了一次空放"
                  + "（原版 `AbilityLogic__PlayAbility.c:2514-2532` 逐目标 `AddMissAction`；结算 = `CardScript.MissActions.c`）");
            return done;
        }

        // ==================================================================
        //  ca 280 —— `revealOrder`（原版 `AbilityLogic__PlayAbility.c:2483-2512`）
        // ==================================================================
        // **判据（逐句现读）**：`if (iVar8 == 0x118)` ⇒ 每个目标**两跳**、同一轮里连着做：
        //     ① `BattleManager__AddRevealOrders(bm, targets[i], ability.enchantmentCard(ability+0x88),
        //          abilityData.priority(+0x44), 1, 0)`
        //     ② `BattleManager__SpendEnchantment(bm, targets[i], ability.enchantmentCard(+0x88), 0)`
        //   `0x118 = 280`（`AbilityEffect.revealOrder`）。
        //
        // **入队之后的结算** = `BattleManager._ResolveRevealOrders_d__549.MoveNext` 那条协程
        //   （① `HasSecretOrder` → ② `RevealedSecret` → ③ `DisplayRevealedCard` + `DestroyCard`
        //   → ④ `buffType == 15 (singleUse)` 的摘附魔 → ⑤ `AddExecuteSecretOrderAction`）。
        //   这一条腿我们早已实现，落点 = `RuleCore.RevealSecretOrders(ctx, p, slot)`（**一条规则只此一处**）。
        //   🔴 它的文件头**自己就写着**「本方法是 `chosenAbility == revealOrder(280)` 那一跳的落点，
        //      数据层一通就能接」—— 本方法就是那条接线。
        //   ⚠️ **如实标着一处差异**：② `SpendEnchantment`（把附魔置「已花掉」）与 ④ 的
        //      `RemoveEnchantment(…, 2)`（入队摘除）在我们这边**合成一条** `Enchantments.EnqueueRemove`
        //      `+ Enchantments.EnqueueRemovePray`（见 `RevealSecretOrders` 的 doc ④）——
        //      原版的「花掉」是**留在身上但不可再用**，我们的「摘掉」是**离身**。
        //      池内 280 = 0 张 ⇒ 今天零可观测（如实标着，⛔ 不当成已核）。
        // 📊 可达性：池内 `chosenAbility 280` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 280 = revealOrder` 的行执行器（逐目标揭示并执行一条密令）。</summary>
        /// <returns>真揭示并执行了密令的目标数。</returns>
        internal static int RunRevealOrderRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var targets = B103W5RowTargets(ctx, p, u, row, ta, chosen, "280 revealOrder");
            if (targets == null) return 0;
            int done = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.IsAlive) continue;
                int tp, ts;
                if (!FindSlot(ctx, t, out tp, out ts)) continue;
                if (RevealSecretOrders(ctx, tp, ts)) done++;     // ① + ②（② 的差异见上）
            }
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 280`（revealOrder）：{done}/{targets.Count} 个目标揭示了密令"
                  + "（原版 `AbilityLogic__PlayAbility.c:2483-2512` 逐目标 `AddRevealOrders` + `SpendEnchantment`）");
            return done;
        }

        // ==================================================================
        //  ca 375 —— `triggerSacrifice`（原版 `AbilityLogic__PlayAbility.c:2111-2126`）
        // ==================================================================
        // **判据（逐句现读）**：`if (iVar8 == 0x177)` ⇒ `while (iVar20 < targets.Count)`：
        //   `BattleManager__AddTriggerSacrifice(bm, targets[i], abilityData.thisCard(+0x10),
        //     abilityData.priority(+0x44), 1, 0)`
        //   `0x177 = 375`（`AbilityEffect.triggerSacrifice`）。**逐目标各登记一条 `sacrifice` 触发**。
        //   ⇒ 形状与 `370 Relentless` / `400 Courtesy` **同一族**（`RunGrantedTriggerRow`），
        //     只是关键词不同 —— 所以入队复用 `RuleCore.EnqueueGrantedTrigger`（`ActionPump.cs` 类尾 §九，
        //     **一条规则只此一处**），⛔ 别在这儿另写一张「已登记触发」表。
        // ⚠️ `sacrifice` 已在 `RoutableTriggers` + `TriggerKeywordOf`（`CardDef.cs:7216`
        //   `Sacrifice = "sacrifice"`；原版 `AbilityTrigger.Sacrifice = 220`）。
        // 📊 可达性：池内 `chosenAbility 375` = **0**、`abilityTriggers` 含 220 = 0 ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 375 = triggerSacrifice` 的行执行器（逐目标登记一条 `sacrifice` 触发）。</summary>
        /// <returns>成功入队的目标数（0 = 解不出目标 / 全部被闸挡下）。</returns>
        internal static int RunTriggerSacrificeRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var targets = B103W5RowTargets(ctx, p, u, row, ta, chosen, "375 triggerSacrifice");
            if (targets == null) return 0;
            var lane = ListenerLane(u.Card, "Sacrifice");        // = 原版 `abilityData.ability.priority(+0x44)`
            int queued = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null) continue;
                if (EnqueueGrantedTrigger(ctx, KeywordTable.Sacrifice, t, actingCard: u, lane: lane,
                                          label: $"{u.Name}→{t.Name}·{KeywordTable.Sacrifice}"))
                    queued++;
            }
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 375`（triggerSacrifice）："
                  + $"给 {queued}/{targets.Count} 个目标各登记一条 `{KeywordTable.Sacrifice}` 触发"
                  + "（原版 `AbilityLogic__PlayAbility.c:2111-2126` 逐目标 `AddTriggerSacrifice`）");
            return queued;
        }

        // ==================================================================
        //  ca 330 —— `drawUntilEqual`（原版 `AbilityLogic__PlayAbility.c:2373-2418`）
        // ==================================================================
        // **判据（逐句现读 `:2373-2418`）**：
        //   · `:2378` `iVar8 = NumCardsInHand(GetPlayerHand(bm, thisCard.isPlayerSide(+0x40)))` —— **对手**手牌数
        //     （`AbilityLogic__AddDrawCard` 里那个 bool 是 `isPlayer`，这里先取 `*(thisCard+0x40) == '\0'` 的反面）
        //   · `:2395` `local_150[0] = 对手手牌数 − 自己手牌数`，`:2400` 打一条日志
        //   · `:2403` 若 `playersAffected(+0x20) ∈ {0 Both, 10 Self}` 且 `diff > 0`
        //        ⇒ 循环 `diff` 次，每次 `AbilityLogic__AddDrawCard(…, isPlayer = 自己, local_178, …)`
        //     `:2410` 若 `playersAffected ∈ {0 Both, 20 Enemy}` 且 `diff < 0`
        //        ⇒ 循环 `−diff` 次，每次 `AddDrawCard(…, isPlayer = 对手, local_178, …)`
        //   ⇒ 语义 = 「把**少**的那一方抽到与另一方**相等**」；那一方由 `playersAffected` 决定，
        //      而「谁少」由**当场的手牌数**决定（⚠️ **两条闸是「且」，不是「或」**）。
        //   `local_178` = `logic.furtherMultiplier`（每一轮抽几张）；缺省/非法（`<= 0`）⇒ **按 1 张**
        //   （原版这个循环变量在别的支里恒 1；`AddDrawCard` 的第 5 参就是它 —— 见 `:2492-2506` 的同名用法）。
        //
        // **原子动作复用**：每次抽牌 = `DoDraw`（`EffectResolver.cs:3654`，`draw` 动词的落点）——
        //   它含「手牌上限 / 疲劳 / 代词槽 / 本次抽到哪几张」四条记账，⛔ 别在这儿直接调 `Draw` 绕开它们。
        // 📊 可达性：池内 `chosenAbility 330` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 330 = drawUntilEqual` 的行执行器（把少手牌的那一方补到相等）。</summary>
        /// <returns>真抽了几张（0 = 两方本来就一样多 / `playersAffected` 那一侧不抽）。</returns>
        internal static int RunDrawUntilEqualRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var spec = B103W5RowSpecOf(u, row, 330);
            if (spec == null)
            {
                ctx.Log($"⚠️「{u.Name}」的主动能力 `chosenAbility 330`（drawUntilEqual）：**取不到这一行的规格**"
                      + "（行下标在多条 logic 里都能对上 ⇒ 判不出 `playersAffected`）⇒ 本版**不猜**，这一行不结算"
                      + "（判据 `AbilityLogic__PlayAbility.c:2403/2410` 的两条闸都读 `playersAffected(+0x20)`）");
                return 0;
            }
            int affected = ActiveRowIntOf(spec.PlayersAffected, row);    // 0 Both / 10 Self / 20 Enemy
            int perDraw = ActiveRowIntOf(spec.FurtherMultiplier, row);  // = 原版 `local_178`
            int drawCount = perDraw > 0 ? perDraw : 1;

            int myHand = ctx.Players[p].Hand != null ? ctx.Players[p].Hand.Count : 0;
            int foeHand = ctx.Players[1 - p].Hand != null ? ctx.Players[1 - p].Hand.Count : 0;
            int diff = foeHand - myHand;                                // 正 = 对手多 ⇒ 我抽；负 = 我多 ⇒ 对手抽
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 330`（drawUntilEqual）：手牌 我 {myHand} / 对手 {foeHand}"
                  + $"（差 {diff}）· playersAffected={affected} · 每轮 {drawCount} 张"
                  + "（原版 `AbilityLogic__PlayAbility.c:2373-2418`）");

            int drawn = 0;
            if ((affected == 0 || affected == 10) && diff > 0)
            {
                var op = new EffectOp { Verb = "draw", Amount = drawCount, Source = "drawUntilEqual" };
                for (int k = 0; k < diff && !ctx.IsOver; k++)
                {
                    DoDraw(ctx, p, $"{u.Name} 的主动能力 chosenAbility 330", op);
                    drawn += drawCount;
                }
            }
            else if ((affected == 0 || affected == 20) && diff < 0)
            {
                var op = new EffectOp { Verb = "draw", Amount = drawCount, Source = "drawUntilEqual" };
                for (int k = 0; k < -diff && !ctx.IsOver; k++)
                {
                    DoDraw(ctx, 1 - p, $"{u.Name} 的主动能力 chosenAbility 330（对手侧）", op);
                    drawn += drawCount;
                }
            }
            return drawn;
        }

        // ==================================================================
        //  ca 135 —— `stunMultiplier`（原版 `AbilityLogic__PlayAbility.c:659-681`）
        // ==================================================================
        // **判据（逐句现读 `:659-681`，`0x87 = 135`）**：
        //   `lVar11 = SupportMethods__ShuffleCardList(本行目标表, matchData, 0, 0)`   ← **先洗牌**
        //   `for (; iVar20 < lVar11.Count && 0 < local_178; local_178--) { AddStun(bm, lVar11[iVar20], priority, isPlayerTurn, thisCard, blockPlayerActions, 0); iVar20++; }`
        //   ⇒ **洗牌后取前 `local_178`（= `furtherMultiplier`）个，各眩晕一次**（不是「每个目标眩晕 N 次」）。
        // **我们的等价物**：`EffectTargetSpec.Random + Count` —— `ResolveTargets` 的 `:1909`
        //   `else if (spec.Random && spec.Count < pool.Count)` 就是「从池里**随机取 Count 个**」
        //   （走 `ctx.GetRandomInt`，原版预烧表），与 `ShuffleCardList + 取前 N` 同一件事。
        //   ⇒ 把本行 op 的 `Target` **克隆**一份、置 `Random = true, Count = fm`，再交给 `DoStun`
        //   （⛔ 不自己造目标集 / 不自己造数值：目标池来自本行解析出来的 op，计数来自 `fm`）。
        // ⚠️ **`local_178 < 1` ⇒ 一个都不晕**（`.c:670` 的循环条件）—— 这里 `fm <= 0` 同样直接返回 0。
        // 📊 可达性：池内 `chosenAbility 135` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 135 = stunMultiplier` 的行执行器（洗牌后取前 `fm` 个各眩晕一次）。</summary>
        /// <returns>真眩晕的目标数。</returns>
        internal static int RunStunMultiplierRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var spec0 = B103W5RowSpecOf(u, row, 135);
            int times = spec0 != null ? ActiveRowIntOf(spec0.FurtherMultiplier, row) : 0;   // = 原版 `local_178`
            if (times < 1)
            {
                ctx.Log($"「{u.Name}」的主动能力 `chosenAbility 135`（stunMultiplier）："
                      + $"`furtherMultiplier` = {times} ⇒ **一个都不晕**（原版 `AbilityLogic__PlayAbility.c:670` 的循环条件 `0 < local_178`，"
                      + "判不出这一行的规格时同样按 0 处理）");
                return 0;
            }
            var ops = ActiveRowOpsOf(u, row);
            var spec = (ops.Count > 0) ? ops[0].Target : null;
            if (spec == null)
            {
                if (!((ta == 30 || ta == 220) && chosen != null))
                {
                    ctx.Log($"⚠️「{u.Name}」的主动能力 `chosenAbility 135`（stunMultiplier）**解不出目标池**（`ta={ta}`，"
                          + "这一行没有配到正文 op）⇒ 本版**不猜**，这一行不结算"
                          + "（目标集判据 = `AbilityLogic__GetTargets.c:192-207`，只有 `ta = 30/220` 读调用方给的那个目标）");
                    return 0;
                }
                spec = new EffectTargetSpec { Raw = "(135：调用方点定的那一个)", Side = "enemy", Kind = "any", Count = 1 };
            }
            var narrow = spec.Clone();
            narrow.Random = true;          // 原版 `ShuffleCardList` + 取前 N
            narrow.Count = times;          // 原版 `local_178`
            var op2 = new EffectOp
            {
                Verb = "stun", Amount = 1, Target = narrow,
                Source = (ops.Count > 0 && ops[0].Source != null) ? ops[0].Source : "stunMultiplier",
            };
            var un = new List<string>();
            ctx.EffectChain++;
            try { DoStun(ctx, p, $"{u.Name} 的主动能力 chosenAbility 135", op2, chosen, un); }
            finally { ctx.EffectChain--; }
            ReportUnresolved(ctx, $"「{u.Name}」的主动能力（stunMultiplier）", un);
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 135`（stunMultiplier）：从目标池里**随机取 {times} 个**各眩晕一次"
                  + "（原版 `AbilityLogic__PlayAbility.c:659-681`：`ShuffleCardList` 后取前 `local_178` 个）");
            return times;
        }

        // ==================================================================
        //  ca 340 —— `removeThisEnchantment`（原版 `AbilityLogic__PlayAbility.c:2250-2295`）
        // ==================================================================
        // **判据（逐句现读 `:2250-2295`，`0x154 = 340`）**：
        //   · 遍历的是 **`abilityData.thisCard(+0x10)` 自己的 `+0x108` 效果表** —— ⛔ 不是 actingCard、也**不读目标表**；
        //   · 闸①：`CardEffect.buffType(+0x38) ∈ {0xf(15 singleUse), 0x10(16 choice)}`；
        //   · 闸②：`effect.enchantingCard(+0x88) != null` 且
        //     `AbilityLogic__MatchingTrigger(this, effect.enchantingCard + 0x290, iVar21, 0)` 为真
        //     （`MatchingTrigger.c`：走那张卡的触发表，**命中某条 `+0x10 == iVar21` 就返回 1**）。
        //     🔴 `iVar21 = local_res20`（`.c:285`）= `PlayAbility` 的第 4 个形参 = **`param_4`**；
        //        主动能力那条路恒传 **400**（`RawCardScript__ResolveActiveAbility.c:58` 的 `ThisUnitAbilityUsed`）
        //        ⇒ 闸② = **那张附魔卡自己带 trigger 400**。
        //   · 命中 ⇒ `AddRemoveEnchantment(bm, **thisCard**, effect, priority, 0, 0)` ⇒ **摘掉自己身上这一条**。
        // **我们的等价物**：`CardInstance.ActiveEffects`（= 原版 `+0x108` 的附魔那半边）＋
        //   `ActiveCardEffect.BuffType`（= 原版 `+0x38`）＋ `ActiveCardEffect.EnchantmentCard`（= `+0x88`）
        //   ＋ `CardDef.AbilityTriggerPresence(400)`（= `MatchingTrigger`）＋
        //   `Enchantments.EnqueueRemove`（摘除**唯一的写点**）。
        // 📊 可达性：`cards_engine.json` 池内 `activeAbilityLogic` 里 `ca 340` = 0 行；但**卡级** `chosenAbility=340`
        //   有 **2 卡 / 3 行**（附魔模板 `Pray Enchantment` + `DA42 Reconnaissance Mission`）⇒ 本支是本批里
        //   **唯一有真实数据**的一条。
        /// <summary>`chosenAbility 340 = removeThisEnchantment` 的行执行器（摘掉**施放者自己**身上那条附魔）。</summary>
        /// <returns>真入队摘除的条数。</returns>
        internal static int RunRemoveThisEnchantmentRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            var inst = u.Instance;
            if (inst == null || inst.ActiveEffects == null || inst.ActiveEffects.Count == 0)
            {
                ctx.Log($"「{u.Name}」的主动能力 `chosenAbility 340`（removeThisEnchantment）：它自己身上**没有附魔记录** ⇒ 无事可做"
                      + "（原版 `AbilityLogic__PlayAbility.c:2250-2295` 遍历的就是 `thisCard + 0x108`）");
                return 0;
            }
            // 先收集再入队（`EnqueueRemove` 只入队、不立即摘，但仍照「先快照后动手」的纪律写，避免边遍历边改集合）
            var hits = new List<ActiveCardEffect>();
            for (int i = 0; i < inst.ActiveEffects.Count; i++)
            {
                var eff = inst.ActiveEffects[i];
                if (eff == null) continue;
                // 闸①：`buffType ∈ {15 singleUse, 16 choice}`
                if (eff.BuffType != Enchantments.BuffTypeSingleUse && eff.BuffType != Enchantments.BuffTypeChoice) continue;
                // 闸②：那张附魔卡存在，且**它自己带 trigger 400**（= 原版 `MatchingTrigger(…, 400)`）
                if (eff.EnchantmentCard == null) continue;
                if (eff.EnchantmentCard.AbilityTriggerPresence(400) != 1) continue;
                hits.Add(eff);
            }
            int queued = 0;
            for (int i = 0; i < hits.Count; i++)
                if (Enchantments.EnqueueRemove(ctx, inst, hits[i], ActionPriority.Instant,
                                               $"{u.Name}·removeThisEnchantment"))
                    queued++;
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 340`（removeThisEnchantment）："
                  + $"{queued}/{hits.Count} 条自己身上的附魔被排进摘除"
                  + "（原版 `AbilityLogic__PlayAbility.c:2250-2295`：自己的 `+0x108` 里 `buffType ∈ {15,16}` 且那张卡带 trigger 400）");
            return queued;
        }

        // ==================================================================
        //  ca 200 —— `exchangeRandomFriendlyMinion`（原版 `AbilityLogic__PlayAbility.c:462-536`）
        // ==================================================================
        // **判据（第一权威，逐句现读 `decomp_full/AbilityLogic__PlayAbility.c:462-536`；首行函数名 = 自己）**：
        //   · `:469-478` **临时造一个 `TargetCriteria`**（⛔ 不是这一行自己的 `targetCriteria`）：
        //       `targetsAffected = 10 (allFiltered)` · `friendlyUnits = 1` · `enemyUnits = 0`
        //       · `includeHero = 0` · `includeMinions = 1` · `includeSpells/Whispers = 0`
        //       · `maxTargets = 1`（`+0x40`）  ⇒ **己方随机一个部队**
        //     （`TargetSelectMethod` 缺省 `0 = random`）。`:479-483` 拿它调 `AbilityLogic.GetTargets`。
        //   · 三段分支（全部 `goto LAB_1808206bb` 汇合）：
        //       ① `:485-495` criteria 目标表**空** ∧ 本行目标表（`param_1 + 0x78`）非空
        //            ⇒ `AddStealMinion(bm, 本行目标表[0], priority, block, 0)`
        //       ② `:497-507` criteria 非空 ∧ 本行目标表**空**
        //            ⇒ `AddStealMinion(bm, criteria[0], priority, block, 0)`
        //       ③ `:509-520` 两者都非空
        //            ⇒ `AddExchangeMinions(bm, abilityData.actingCard(+0x18), criteria[0], 本行目标表[0],
        //                                  priority, block, 0)`
        //          ⚠️ **`actingCard` 不是被换的那两个之一** —— 它只是 `AddExchangeMinions` 的第 1 形参
        //             （`BattleManager__AddExchangeMinions.c` 的签名），被换的是**后两个**形参
        //             （`_ResolveExchangeMinion_d__552` 只读 `+0x38` 那个 2 元素列表，`:40` 的闸是 `1 < Count`）。
        //   ⇒ **本支的缺口 = 「临时 criteria → GetTargets」这一跳**（上一手报告 §7·2 的结论）。
        //
        // **我们的等价物**：
        //   · 「临时 criteria」= 一条 `Side=own / Kind=troop / Count=1 / Random=true` 的 `EffectTargetSpec`
        //     走 `ResolveTargets`（`ResolveTargetsCore` 的 `spec.Random && spec.Count < pool.Count`
        //     那一支 = 「从池里随机取 N 个」，走 `ctx.GetRandomInt` 的原版预烧表）。
        //     `Kind="troop"` ⇒ `AddSide` 的 `troopOnly` 跳过督军 = 原版 `includeHero = 0`。
        //   · 抢 = `DoTakeControl`（`stealminion` 那一路：从原主摘掉 → `BoardSlots.NextFreeWithoutDisplacing`
        //     落到对面 → `ChangeOwner` 的等价；判据见那个方法的 doc）。
        //   · 换 = `DoExchange`（两个非施放者的那一支 = 它自己的 rule ⑵）。
        //   ⚠️ **如实标着两条残余**：① 原版 `includeSelf = 0`，我们**没有**「把施放者从 pool 里剔掉」这一位
        //      ⇒ 施放者若自己是部队会被随机选中（原版不会）；② `maxTargets = 1` 那一档的并列规则原版是
        //      `TargetSelectMethod.random`（缺省 0），我们 `Random=true` 同档。
        //   ⚠️ 原版 `:509-520` 只取**第 0 个**两份目标表（不是遍历）—— 这里照抄。
        // 📊 可达性：`cards_engine.json` 池内 1,129 张现扫 `chosenAbility 200` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 200 = exchangeRandomFriendlyMinion` 的行执行器。</summary>
        /// <returns>真结算的单位数（0 = 两侧都空 / 解不出）。</returns>
        internal static int RunExchangeRandomFriendlyMinionRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;

            // ① 临时 criteria（原版 `.c:469-483`）—— **己方随机一个部队**
            var critSpec = new EffectTargetSpec
            {
                Raw = "(200 临时 criteria：己方随机一个部队)",
                Side = "own", Kind = "troop", Count = 1, Random = true,
            };
            var critTargets = ResolveTargets(ctx, p, critSpec, null, null, null, null);
            UnitState cm = (critTargets != null && critTargets.Count > 0) ? critTargets[0] : null;

            // ② 本行自己的目标表（原版 `param_1 + 0x78`）
            var rowTargets = B103W5RowTargets(ctx, p, u, row, ta, chosen, "200 exchangeRandomFriendlyMinion");
            if (rowTargets == null) return 0;
            UnitState rt = (rowTargets.Count > 0) ? rowTargets[0] : null;

            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 200`（exchangeRandomFriendlyMinion）："
                  + $"临时 criteria（己方随机一个部队）→ {(cm != null ? cm.Name : "（空）")} · "
                  + $"本行目标表 → {(rt != null ? rt.Name : "（空）")}"
                  + "（原版 `AbilityLogic__PlayAbility.c:462-536`，三段分支）");

            if (cm == null && rt == null)
            {
                ctx.Log($"「{u.Name}」的 `chosenAbility 200`：两侧都空 ⇒ **这一支什么都不做**（原版 `.c:485/:497` 两条都不命中）");
                return 0;
            }
            // 【分支 ①】criteria 空 ∧ 本行目标非空 ⇒ 抢本行目标（`.c:485-495`）
            if (cm == null) return B103W5StealOne(ctx, p, rt, u, "200：临时 criteria 空 ⇒ 抢本行目标") ? 1 : 0;
            // 【分支 ②】criteria 非空 ∧ 本行目标空 ⇒ 抢 criteria 目标（`.c:497-507`）
            if (rt == null) return B103W5StealOne(ctx, p, cm, u, "200：本行目标空 ⇒ 抢 criteria 目标") ? 1 : 0;
            // 【分支 ③】两者都非空 ⇒ 换（`.c:509-520`；`actingCard` 只是签名上的第 1 参，不参与换位）
            return B103W5ExchangePair(ctx, p, cm, rt, u) ? 1 : 0;
        }

        /// <summary>抓一张**指定的**单位（= 原版 `AddStealMinion(bm, stolenCard, priority, block)` 的落点）。
        /// <para>复用 <see cref="DoTakeControl"/>：把「要抢的那一张」塞进 `ResolveTargets` 的
        /// `chosen` 覆盖槽（`op.SubjectTargetsAffected = 30` ⇒ 采纳调用方给的目标，判据
        /// `AbilityLogic__GetTargets.c:192-207`），语义与 `stealminion` 那一路**同一条搬运逻辑**（⛔ 不另写）。</para></summary>
        static bool B103W5StealOne(BattleContext ctx, int p, UnitState target, UnitState caster, string why)
        {
            if (target == null) return false;
            var op = new EffectOp
            {
                Verb = "stealminion",
                Source = why,
                SubjectTargetsAffected = 30,
                Target = new EffectTargetSpec { Raw = "(200 抢指定的那一张)", Side = "any", Kind = "any", Count = 1 },
            };
            var un = new List<string>();
            DoTakeControl(ctx, p, why, op, target, un);
            ctx.Log($"{why}：「{target.Name}」按原版 `AddStealMinion` 抢到对面"
                  + "（原版 `AbilityLogic__PlayAbility.c:485-495/:497-507`）");
            return true;
        }

        /// <summary>把**两个指定的**单位换位（= 原版 `AddExchangeMinions` 的落点）。
        /// <para>复用 <see cref="DoExchange"/> 的 rule ⑵（两个非施放者那一支）：把这两张塞进
        /// `ctx.LastTargets`、用 `Side="prev"` 的 spec 取回**同序**两份，并**临时**把 `ctx.ActingUnit`
        /// 置空（否则它的 rule ⑴ 会把施放者当成换位的一方）—— `try/finally` 原样还原。</para></summary>
        static bool B103W5ExchangePair(BattleContext ctx, int p, UnitState a, UnitState b, UnitState caster)
        {
            if (a == null || b == null) return false;
            var savedTargets = new List<UnitState>(ctx.LastTargets);
            var savedActing = ctx.ActingUnit;
            var savedLast = ctx.LastTarget;
            ctx.LastTargets.Clear(); ctx.LastTargets.Add(a); ctx.LastTargets.Add(b);
            var op = new EffectOp
            {
                Verb = "exchange",
                Source = "200 exchange",
                Target = new EffectTargetSpec { Raw = "(200 显式对)", Side = "prev", Count = 0 },
            };
            var un = new List<string>();
            try
            {
                ctx.ActingUnit = null;   // 让 `DoExchange` 走 rule ⑵（(others[0], others[1]) = (a, b)）
                return DoExchange(ctx, p, op.Source, op, null, un);
            }
            finally
            {
                ctx.ActingUnit = savedActing;
                ctx.LastTargets.Clear(); ctx.LastTargets.AddRange(savedTargets);
                ctx.LastTarget = savedLast;
            }
        }

        // ==================================================================
        //  ca 345 —— `removeEnchantment`（原版 `AbilityLogic__PlayAbility.c:2297-2371`）
        // ==================================================================
        // **判据（第一权威，逐句现读 `decomp_full/AbilityLogic__PlayAbility.c:2297-2371`）**：
        //   外层遍历**本行目标表**（`param_1 + 0x78`），内层遍历**该目标自己的** `activeEffects(+0x108)`：
        //     · `effect.enchantment(+0x88) != null`（⚠️ **+0x88 = `CardEffect.enchantment`，不是 `enchantingCard(+0x18)`** ——
        //       按 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardEffect.cs` 的声明序逐格数偏移；
        //       ⚠️ 上一手报告 §② 把 `+0x88` 标成 `enchantingCard`，那是**标签写错、偏移对**，就地订正 —— 铁律 5）
        //     · `ability.buffEffect(+0x50) != null` ∧
        //       `effect.enchantment(+0x88) == ability.buffEffect.enchantment(+0x50 → +0x88)`
        //     ⇒ `AddRemoveEnchantment(bm, 目标, effect, priority, 0, 0)`
        //   ⇒ 语义 = **把目标身上「与本条 ability 的 `buffEffect.enchantment` 同一张模板」的附魔摘掉**。
        //   ⚠️ **与 `340` 的区别**：340 摘的是**施放者自己**身上、`buffType ∈ {15,16}` 且那张模板带 trigger 400 的；
        //      345 摘的是**目标**身上、与本行模板**同一张**的（**不看 buffType**）。
        //
        // **判据缺的那一格 = 「本行 ability 的 `buffEffect.enchantment`」**：`ActiveAbilityLogicSpec`
        // **不携带** `buffEffect`。但**数据面有** —— 卡表 `enchantment` 列（`Tool/gen_cards_engine.py` 的
        // `enchantment_grants_of`）就是 `buffEffect.enchantment` 的 PPtr，逐行记 `{at, ab, lg, buffType, trigger, ta, token}`；
        // C# 侧现成入口 = `Enchantments.GrantedEnchantmentRowsOf(CardDef)`（`token` = 模板 `uniqueId`）。
        // ⇒ **不需要给 `ActiveAbilityLogicSpec` 加字段**（上一手报告 §6·4 的假设可以撤回）。
        // ⚠️ **两条如实标着的残余**：① 那张表只收 `buffType ∈ {12,15,16}` 的行（`ENCHANTMENT_ATTACH_BUFFTYPES`），
        //     而 345 不看 buffType ⇒ 若某行 buffType ∉ {12,15,16} 它不在表里（今天我们看不到这样的行）；
        //   ② 按 `LogicIndex == row` 匹配：卡上有多条 ability 且 `lg` 撞号时判不出 ⇒ **不猜**（返回 0 + 出声）。
        // 📊 可达性：池内 `chosenAbility 345` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 345 = removeEnchantment` 的行执行器（摘掉目标身上与本行模板同源的附魔）。</summary>
        /// <returns>真入队摘除的条数。</returns>
        internal static int RunRemoveEnchantmentRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || u.Card == null || (p != 0 && p != 1)) return 0;

            // ---- ① 本行 `buffEffect.enchantment`（= 卡表 `enchantment` 列里 `lg == row` 的那一行）----
            var grants = Enchantments.GrantedEnchantmentRowsOf(u.Card);
            string template = null;
            int hits = 0;
            if (grants != null)
            {
                for (int i = 0; i < grants.Count; i++)
                {
                    var r = grants[i];
                    if (r == null || string.IsNullOrEmpty(r.TemplateCardId)) continue;
                    if (r.LogicIndex != row) continue;
                    template = r.TemplateCardId; hits++;
                }
            }
            if (hits != 1 || string.IsNullOrEmpty(template))
            {
                ctx.Log($"⚠️「{u.Name}」的主动能力 `chosenAbility 345`（removeEnchantment）**判不出本行的 `buffEffect.enchantment`**"
                      + $"（卡表 `enchantment` 列里 `lg == {row}` 的行 = {hits} 条）⇒ 本版**不猜**，这一行不结算"
                      + "（判据 `AbilityLogic__PlayAbility.c:2320-2330`：`effect.enchantment(+0x88) == ability.buffEffect(+0x50).enchantment`）");
                return 0;
            }

            // ---- ② 本行目标表（原版 `param_1 + 0x78`）----
            var targets = B103W5RowTargets(ctx, p, u, row, ta, chosen, "345 removeEnchantment");
            if (targets == null) return 0;

            // ---- ③ 逐目标：摘「那张模板」同源的附魔（先快照后动手）----
            int queued = 0;
            for (int ti = 0; ti < targets.Count; ti++)
            {
                var t = targets[ti];
                if (t == null || !t.IsAlive || t.Instance == null || t.Instance.ActiveEffects == null) continue;
                var hitList = new List<ActiveCardEffect>();
                for (int i = 0; i < t.Instance.ActiveEffects.Count; i++)
                {
                    var eff = t.Instance.ActiveEffects[i];
                    if (eff == null || eff.EnchantmentCard == null) continue;
                    if (eff.EnchantmentCard.Id != template) continue;
                    hitList.Add(eff);
                }
                for (int i = 0; i < hitList.Count; i++)
                    if (Enchantments.EnqueueRemove(ctx, t.Instance, hitList[i], ActionPriority.Instant,
                                                   $"{u.Name}·removeEnchantment"))
                        queued++;
            }
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 345`（removeEnchantment）："
                  + $"{targets.Count} 个目标身上，{queued} 条「与本行模板同源（{template}）」的附魔被排进摘除"
                  + "（原版 `AbilityLogic__PlayAbility.c:2297-2371`：逐目标扫 `+0x108`、比 `effect.enchantment(+0x88)`）");
            return queued;
        }

        // ==================================================================
        //  ca 80 —— `transformSelf`（原版 `AbilityLogic__PlayAbility.c:884-904`，`case 0x50`）
        // ==================================================================
        // **判据（第一权威，逐句现读 `decomp_full/AbilityLogic__PlayAbility.c:884-904`）**：
        //   `GetCardToSummon(ability, ability.summonCriteria(+0x58), abilityData, bm, 本行目标表(+0x78), 0)`
        //   ⇒ 候选表；非空 ⇒ 取**第 0 张**；`ability.summonCriteria != null` ∧ `bm != null` ⇒
        //   `AddTransformCard(bm, abilityData.actingCard(+0x18) /*targetCard*/, 候选[0] /*transformedIntoCard*/,
        //      priority(+0x44), ability.checkIfCasterInPlay(+0x74), abilityData.thisCard(+0x10) /*casterCard*/,
        //      ability.summonCriteria.keepEffects(+0x2a), blockPlayerActions(+0x48), 0)`
        //   ⇒ **把这支 ability 的施放者（actingCard）自己变成候选里的那一张**，`keepEffects` 取 `summonCriteria`。
        //   ⚠️ **与 `90 DoBecome` 主体不同**：90 是「手牌里的战略卡变成 A 或 B」，80 是**施放者自己**；
        //      与 `85 transformUnitWithBuff` 同一族（但 80 **不带** `buffEffect`）。
        //
        // **我们的等价物**：`RuleCore.TransformUnit(ctx, owner, slot, newCard, keepEffects, by)`
        //   （`TransformUnit_B94B12.cs:178`，已把 `ResolveTransformUnit` 7 跳逐句落齐）。
        // ⚠️ **两处如实标着的判据缺**（与 `85` 同一个数据缺口，见 `RunTransformUnitRow` 的 doc ①②）：
        //   ① 「变成哪张卡」的唯一名源 = 这一行配到的 `become` 正文 op（`EffectOp.Payload` 的 `|` 列表）——
        //      `abilitySummon` 列**不含 `activeAbilityLogic`**，`summonCriteria` 本身不带卡名；
        //   ② `keepEffects` 取 `AbilitySummonAt(ab, lg).KeepEffects`，对 active 行今天**必然**返 `null`。
        //   两条任一判不出 ⇒ **出声 + 这一行不结算**（猜错会静默改对局强度 —— 与 `85` 同一条纪律）。
        // 📊 可达性：池内 `chosenAbility 80` = **0** ⇒ 休眠件（铁律 11 照建）。
        /// <summary>`chosenAbility 80 = transformSelf` 的行执行器（把施放者自己变成候选卡）。</summary>
        /// <returns>真变身 ⇒ 1；判据缺 / 解不出 ⇒ 0（并已出声）。</returns>
        internal static int RunTransformSelfRow(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || u.Card == null) return 0;
            int op2, slot;
            if (!FindSlot(ctx, u, out op2, out slot))
            {
                ctx.Log($"「{u.Name}」的 `chosenAbility 80`（transformSelf）：它**不在场上** ⇒ 这一行不结算"
                      + "（原版 `AddTransformCard` 的 targetCard = `abilityData.actingCard(+0x18)`）");
                return 0;
            }
            var ops = ActiveRowOpsOf(u, row);

            // ---- ① `keepEffects`（原版 `summonCriteria.keepEffects(+0x2A)`）----
            int ab = (ops.Count > 0) ? ops[0].AbilityIndex : -1;
            int lg = (ops.Count > 0) ? ops[0].LogicIndex : -1;
            var sm = (ab >= 0 && lg >= 0) ? u.Card.AbilitySummonAt(ab, lg) : null;
            int keepRaw = (sm != null) ? sm.KeepEffects : -1;
            if (keepRaw != 0 && keepRaw != 1)
            {
                ctx.Log($"⚠️「{u.Name}」的 `chosenAbility 80`（transformSelf）**判不出 `keepEffects`**"
                      + $"（`abilitySummon` 那格里是 {(keepRaw < 0 ? "缺键/未落列" : keepRaw.ToString())}）"
                      + " ⇒ 本版**不猜**，这一行不结算（判据 `PlayAbility.c:886` 的 `*(*(param_1+0x58)+0x2a)`；"
                      + "⚠️ `abilitySummon` 列不含 `activeAbilityLogic` —— 数据缺口，见 `RunTransformUnitRow` 的 doc ②）");
                return 0;
            }
            bool keepEffects = keepRaw == 1;

            // ---- ② 变成哪张卡（唯一名源 = 卡面 `become` op 的 `Payload`；见 doc 缺口①）----
            CardDef newCard = null;
            int found = 0;
            for (int i = 0; i < ops.Count; i++)
            {
                if (!string.Equals(ops[i].Verb, "become", System.StringComparison.Ordinal)) continue;
                foreach (string raw in (ops[i].Payload ?? "").Split('|'))
                {
                    string nm = raw.Trim();
                    if (nm.Length == 0) continue;
                    found++;
                    var cand = CreatePool.FindByName(ctx.CardPool, nm);
                    if (cand != null) newCard = cand;
                }
            }
            if (newCard == null || found != 1)
            {
                ctx.Log($"⚠️「{u.Name}」的 `chosenAbility 80`（transformSelf）**判不出变成哪张卡**"
                      + $"（本行 `become` op 的候选数 = {found}、查到的卡 = {(newCard != null ? newCard.Name : "（无）")}）"
                      + " ⇒ 本版**不猜**，这一行不结算（判据 `PlayAbility.c:884` 的 `GetCardToSummon`；"
                      + "⚠️ `abilitySummon` 列不含 `activeAbilityLogic` —— 数据缺口，见 `RunTransformUnitRow` 的 doc ①）");
                return 0;
            }

            // ---- ③ 变身（= 原版 `AddTransformCard` 的落点）----
            bool ok = TransformUnit(ctx, op2, slot, newCard, keepEffects, $"{u.Name} 的主动能力 chosenAbility 80");
            ctx.Log($"{u.Name} 的主动能力 `chosenAbility 80`（transformSelf）："
                  + (ok ? $"把自己变成了「{newCard.Name}」（keepEffects={keepEffects}）" : "变身没生效（见上一条）")
                  + "（原版 `AbilityLogic__PlayAbility.c:884-904` `AddTransformCard(actingCard, 候选[0], …, keepEffects)`）");
            return ok ? 1 : 0;
        }

        // ==================================================================
        //  自检门（只转调，**不改任何行为**；⛔ 产品路径别调）
        // ==================================================================
        /// <summary>自检门：只转调 <see cref="RunResetActionsRow"/>。</summary>
        public static int RunResetActionsRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunResetActionsRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunMissActionRow"/>。</summary>
        public static int RunMissActionRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunMissActionRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunRevealOrderRow"/>。</summary>
        public static int RunRevealOrderRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunRevealOrderRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunTriggerSacrificeRow"/>。</summary>
        public static int RunTriggerSacrificeRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunTriggerSacrificeRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunDrawUntilEqualRow"/>。</summary>
        public static int RunDrawUntilEqualRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunDrawUntilEqualRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="B103W5RowSpecOf"/>（给断言钉「多候选 ⇒ 不猜」那条）。</summary>
        public static bool B103W5RowSpecResolvedForTest(UnitState u, int row, int ca)
        { return B103W5RowSpecOf(u, row, ca) != null; }

        /// <summary>自检门：只转调 <see cref="RunStunMultiplierRow"/>。</summary>
        public static int RunStunMultiplierRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunStunMultiplierRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunRemoveThisEnchantmentRow"/>。</summary>
        public static int RunRemoveThisEnchantmentRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunRemoveThisEnchantmentRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：读某个单位身上附魔记录的条数（断言用它先钉前提，⛔ 产品路径别调）。</summary>
        public static int B103W5EffectCountForTest(UnitState u)
        { return (u != null && u.Instance != null && u.Instance.ActiveEffects != null) ? u.Instance.ActiveEffects.Count : 0; }

        // ==================================================================
        //  ca 71 —— `summonUnitAndSetRef` 的闭环（本代理 `B103_W5b`，**零产品代码改动**）
        // ==================================================================
        // **判据（第一权威，逐句现读 `decomp_full/AbilityLogic__PlayAbility.c`）**：
        //   · `71 summonUnitAndSetRef` 与 `70 summonUnit` **同一个 `case 0x47/0x46` 体**
        //     （`:722-878`）—— 唯一的差别是 `:868` 多写一句
        //     `*(local_res8 + 0x280) = uVar12`（把**刚召唤出来的那张**记到 `AbilityLogic` 实例侧）。
        //   · 🔴 **`+0x280` ≠ `+0x278`**（上一轮 `B103_W5c` §7·1 已就地订正）：`+0x278` 是
        //     `CardScript.referencedCards`（**列表**，`380 setReferenceCard` 写）= 我们的
        //     `UnitState.ReferencedCards`；`+0x280` 在 **`AbilityLogic` 实例侧**、「本 ability 刚召唤那张」。
        //   · ⇒ 我们的等价物 = `DoDeploy` 末尾那段**同一套 last-target 记账**
        //     （`EffectResolver.cs` 主循环：`ctx.LastTargets.Clear()` → 逐个 `ctx.LastTargets.Add(deployed)`
        //     → `ctx.LastTarget = ctx.LastTargets[0]`）—— 紧跟的下一行 `them/it` 读的就是它。
        //   📊 可达性：池内 `chosenAbility 71` = **0**（`cards_engine.json` 1,129 张现扫）⇒ 休眠件。
        //   ⇒ **不新建执行器、不改 `UnitState`**，只补一条「`DoDeploy` 之后 `LastTargets` 就是被召唤那张」的闭环断言。
        /// <summary>自检门：把一条 `deploy` op 直接喂给 `DoDeploy`（= 原版 `summonUnit(70/71)` 那一族的落点，⛔ 产品路径别调）。</summary>
        public static bool B103W5DeployForTest(BattleContext ctx, int owner, string by, EffectOp op)
        { return ctx != null && op != null && DoDeploy(ctx, owner, by, op, new List<string>()); }

        /// <summary>自检门：读「刚部署的那批」（= 原版 `AbilityLogic + 0x280`「本 ability 刚召唤那张」的等价物）。</summary>
        public static int B103W5LastTargetsCountForTest(BattleContext ctx)
        { return (ctx != null && ctx.LastTargets != null) ? ctx.LastTargets.Count : 0; }

        /// <summary>自检门：读「刚部署的那批」的第 0 张（= 原版 `+0x280`；`null` = 空表）。</summary>
        public static UnitState B103W5LastTargetFirstForTest(BattleContext ctx)
        { return (ctx != null && ctx.LastTargets != null && ctx.LastTargets.Count > 0) ? ctx.LastTargets[0] : null; }

        /// <summary>自检门：只转调 <see cref="RunExchangeRandomFriendlyMinionRow"/>。</summary>
        public static int RunExchangeRandomFriendlyMinionRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunExchangeRandomFriendlyMinionRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunRemoveEnchantmentRow"/>。</summary>
        public static int RunRemoveEnchantmentRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunRemoveEnchantmentRow(ctx, p, u, row, ta, chosen); }

        /// <summary>自检门：只转调 <see cref="RunTransformSelfRow"/>。</summary>
        public static int RunTransformSelfRowForTest(BattleContext ctx, int p, UnitState u, int row, int ta, UnitState chosen)
        { return RunTransformSelfRow(ctx, p, u, row, ta, chosen); }
    }
}
