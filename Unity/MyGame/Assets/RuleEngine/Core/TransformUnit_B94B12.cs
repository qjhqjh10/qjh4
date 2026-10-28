// TransformUnit_B94B12.cs — 第九十四会话 · 波 3 · 执行代理 `B94_B12` 的 `RuleCore` partial 扩展
//
// 🔴 为什么单开文件：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 365+ 处，
//   往它中间插成员会让下游落点整体下移 ⇒ 新成员一律落本文件（与 `RuleCorePartial_B88E14.cs`
//   / `RuleCorePartial_B93F7.cs` / `RuleCorePartial_B94B8.cs` 同形）。
//
// 本批落的东西 = **账 `战斗逻辑驱动:172` + `:548` 整条 `transformUnit`（在场单位变身）机制的引擎侧**
//   （原版 `BattleManager.AddTransformCardWithBuff` → 入队 `transformUnit(27)` → 出队 `ResolveTransformUnit`）。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  一、这条链【逐跳】怎么走（判据全部本笔 2026-10-29 现读）
// ════════════════════════════════════════════════════════════════════════════════════════════
//
// **① 生产端（谁调它）—— 全库只有一个**：
//   `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:904-941` 的 `iVar8 == 0x55 (85)` 支：
//     · `GetCardToSummon(...)` 取到「变成哪几张」（`*plVar2`，List）；
//     · `CardEffect.GetNewInstanceOfThisEffect(abilityLogic + 0x50)` + `AdjustCardEffect(...)`
//       ⇒ 造出**这一条 buff** 的一份新实例（`uVar23`），并把 `effect.owner(+0x18) = ability.+0x10`、
//       `effect.isPlayer(+0xA0) = *(ability.+0x10 + 0x40)`；
//     · 候选表非空 ⇒ 调
//       `AddTransformCardWithBuff(bm, target = ability.+0x18, newCardData = 候选[0], priority = ability.+0x44,
//                                 checkIfCasterInPlay = abilityLogic.+0x74, casterCard = ability.+0x10,
//                                 buffEffect = uVar23, keepEffects = *(abilityLogic.+0x58 + 0x2A),
//                                 blockPlayerActions = ability.+0x48)`
//     · **同族的无 buff 兄弟** `AddTransformCard`（`PlayAbility.c:890-903`）参数完全一样、只是没有 `buffEffect`。
//   `keepEffects` 那一格 = **`SummonCriteria.keepEffects`（`+0x2A`）**，即卡 SO 上的一个字段
//   （⇒ 我们的 `CardDef.AbilitySummonSpec.KeepEffects`，见 `CardDef.cs:9158`）。
//   **池内可达性 = 0 张**（`cards_truth.json` 里 chosenAbility 85 一张都没有）⇒ **休眠**；
//   按铁律 11 照样建（⛔ 不判 D）。
//
// **② 入队** —— `d:/2/tools/decomp_full/BattleManager__AddTransformCardWithBuff.c` 全文 108 行：
//     · `:38` `BattleAction___ctor(action, 0x1b /*27 = transformUnit*/, checkIfCasterInPlay, casterCard,
//                blockPlayerActions, 0)`
//     · `:39` `+0x10 = targetCard`（被变身的那张）· `:41` `+0x88 = transformedIntoCard`（变成哪张）
//       · `:56` `+0x70 = buffEffect` · `:58` `+0xEC = keepEffects`
//       · `+0xE0 = casterCard.cardName`（`:46-55`，caster 非空才填）
//     · `:106` `AddAutoActionToQueue(bm, action, priority, 0)`
//
// **③ 出队** —— `BattleManager__ResolveAction.c:2931` 的 91 路巨型 switch 里 `case 0x1b` 真调
//     `ResolveTransformUnit`（不是内联假象）⇒ 有真出队口。
//
// **④ `ResolveTransformUnit` 的体**（`d:/2/tools/decomp_full/BattleManager__ResolveTransformUnit.c`，2252 行；
//    其中 90% 是 Ghidra 展开的「结构体整份拷贝」，真语句只有下面这些）：
//     `:106-155` targetCard == null ⇒ `LogError` + `FinishResolvingAction` + return        （原版的**早退**）
//     `:205`     `isInPlay = targetCard.IsInPlay()`
//     `:254-261` `Log("ResolveTransformUnit {cardName}, in play {isInPlay}")`
//     `:313-317` `bm.cardSelected(+0x410) == targetCard` ⇒ `Log` + `StopDisplayingActiveAbility()`   ← 表现层
//     `:318-368` **isInPlay** ⇒ `targetCard.SetAsUnitTransforming()`（cardState 置 15）              ← 状态标记
//     `:416`     `BroadcastRemoveFromGame(bm, targetCard, null)`   —— 让全场「按来源撤 extrinsic」
//     `:417-516` `if (bm.playerHand)  PlayerHand.RemoveEffectsFromCard(playerHand, targetCard)`
//               `if (bm.enemyHand)   PlayerHand.RemoveEffectsFromCard(enemyHand,  targetCard)`
//                （**两手各一次**，顺序 = 我方手牌 → 敌方手牌；撤的是「这张卡打在别人身上的手牌效果」）
//     `:559-655` 🔴 **`keepEffects == 0`（假）支**：
//                  `:605` `targetCard.ResetToValuesInHand(true)`          ← 整份重置成手牌数值
//                  `:654` `CancelEffectsFromDisappearingUnit(bm, targetCard, targetCard, true)`
//     `:656-703` 🔴 **`keepEffects != 0`（真）支**：
//                  `:702` `targetCard.RemoveOwnExtrinsicEffects()`        ← 只摘「来源 = 它自己」的那些效果
//     `:791`     `targetCard.SetRawCardData(bm, action.newCardData)`     ← **换卡定义**
//     `:977-978` `targetCard.CardSetup(...)`；`:1028` `SetAsUnitInHand(true)` / `:1076` `SetAsUnitInPlay(true)`
//     `:1207`    `MinionPlayedIntoField(targetCard, 0 /*delay*/, unitInPlayCounter, 0 /*doTransformAnim*/,
//                                       **keepStatus = uVar23**, 1 /*shouldTriggerSummon*/, 0)`
//                ← 注意第 5 个实参就是 `local_2c`，而本方法的 `keepStatus` 那一格由 `CardScript` 上
//                  同一个 `keepEffects` 派生（见 `RuleCore.ApplyDeployTurnState` 的 doc：
//                  「`BattleManager__ResolveTransformUnit.c:1207` 那一处传的是运行时值 =
//                    `BattleAction.keepEffects`（`dump.cs` 的 `// 0xEC`）」）⇒ **同一位控制两件事**。
//     `:1580`    `UpdateWhileInPlay(targetCard, true)`                   ← WhileInPlay(20) 重算
//     `:1629`    `CheckStun()`
//     `:1678`    `UpdateFigures()`                                       ← 表现层
//     `:1726`    `BroadcastWhileInPlay(bm, 0, targetCard, 0)`            ← 全场 20 档广播
//     `:1775-1825` `HasCurrentTrait(0x4B0 = 1200 /*ambush*/)` ⇒ `SetAmbush(true)`
//     `:1828-2030` 两手 `PlayerHand.UpdateCardEffects` + `CheckEffectsOnNewCard` ⇒ `FinishResolvingAction`
//     `:2079-2083` `cardType != 10` ⇒ **return**（10 = 督军 ⇒ 才走下面的语音/聊天，`:2177-2180`）
//
// **⑤ 原版的 bug（照抄，⛔ 别修）**：
//   · 上面 `:2079-2083` 的 `return` 在 **`FinishResolvingAction` 之后**，所以「牌序没走到语音那一段」
//     只是少一条聊天，**不影响结算** —— 不是 bug，别当缺陷改。
//   · **真正的偷工**：`keepEffects != 0` 那一支**不`CancelEffectsFromDisappearingUnit`**
//     （`:656-703` 全文只有 `RemoveOwnExtrinsicEffects` 一句）⇒ **变身后「旧的待结算动作照跑」**。
//     本笔**照抄**（见下 `TransformUnit` 的两支），⛔ 别顺手补。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  二、🔴 「`local_12c` 两支在 Ghidra / ILSpy 里丢了」—— **本笔复核：不成立**（铁律 5 就地订正）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   派活简报要求「施工前用 VA 反汇编补」。本笔 2026-10-29 现跑（`工具/disasm_va.py` +
//   `dump.cs:32113` 的 `ResolveTransformUnit // RVA 0x980950 / VA 0x180980950`）：
//
//     180980EBD   48 8d 4d 40            lea rcx, [rbp + 0x40]
//     180980EBD   44 38 bd 2c 01 00 00   cmp byte ptr [rbp + 0x12c], r15b   ; ← BattleAction+0xEC = keepEffects
//     180980EC4   74 7a                  je  0x180980f40                    ; keepEffects == 0 ⇒ 去 F40
//     180980F23   48 8b 4d 50            mov rcx, [rbp + 0x50]              ; targetCard
//     180980F30   e8 9b 67 c7 ff         call 0x1805f76d0                   ; = CardScript.RemoveOwnExtrinsicEffects
//                                                                          ;   (dump.cs:36591 RVA 0x5F76D0)
//     180980F35   e9 f0 00 00 00         jmp  0x18098102a
//     180980FA0   45 33 c0               xor r8d, r8d
//     180980FA3   b2 01                  mov dl, 1
//     180980FA5   e8 36 71 c7 ff         call 0x1805f80e0                   ; = CardScript.ResetToValuesInHand(true)
//                                                                          ;   (dump.cs:36174 RVA 0x5F80E0)
//   ⇒ **两支都在机器码里，且判据就是 `[rbp+0x12C]`（= `local_12c` = `keepEffects`）**。
//   Ghidra 的 `.c` 也一样有（`:559 if (local_12c == '\0')` / `:656 else`）。
//   ⇒ 旧说法「两支丢了」**作废**；真丢的是**别的格**（见下「三」）。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  三、🔴 仍然**读不出**的格（如实标，⛔ 不猜）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   `ResolveTransformUnit.c` 里 `local_90` / `local_108` / `local_308` / `local_a8` / `local_2c` / `local_22c`
//   这六个**局部量没有任何赋值点**（Ghidra 丢了那几句；ILSpy 版整段是 `Cpp2ILHelpers.NoteDecompilerIssue`
//   噪声，更不可读）。从用法能反推的只有**形状**：
//     · `local_90` = `RawCardScript` ⇒ `SetRawCardData` 的实参（= `action.transformedIntoCard`）；
//     · `local_108` / `local_308` = 两个 `CardScript`（读 `+0x40 isPlayer` / `+0x44 unitInPlayCounter`）；
//     · `local_a8` / `local_22c` = 两个 bool（前者为真 ⇒ `LogWarning` + `AddEffect`，后者 ⇒ 重画数值）。
//   ⇒ 这三格（`CardSetup` 的实参、`AddEffect` 的靶、`CheckReplacedAbility` 的闸）**恢复不了**，
//     本笔不做；它们是**表现层 / 绑 buff 那一步**，与本笔的「状态层」正交。差什么写进报告。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  四、本工程的结构差异（如实标，⛔ 不假装等价）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   ① 🔴 **`UnitState.Instance` 与 `CardInstance.Card` 都是 `readonly`** ⇒ 原版「同一个 `CardScript`
//      对象上 `SetRawCardData` 换定义」我们做不到 ⇒ **必须发一份新 `CardInstance` + 新 `UnitState`
//      顶替原格**（`RuleCorePartial_B94B8` 那份 §6.5 的结论，本笔复核成立）。
//      🔑 但实例**号**沿用（`new CardInstance(newCard, oldInstance.Id)`）—— 原版 `+0x22c` 那个
//      本局唯一号在变身前后**不变**，我们照这个口径；副作用见 ②。
//   ② 由此带来的一条**行为差异（如实记）**：`ActionPump.CarrierReplacedByRemnant` 用
//      `ReferenceEquals(u.Instance, 载体.Instance)` 认「载体被换掉」⇒ 我们换了 `CardInstance` 对象
//      ⇒ 那个窄口**不会**因此取消待结算动作。**恰好**与原版的两支一致：
//      `keepEffects == false` 靠**显式** `CancelEffectsFromDisappearingUnit` 撤、
//      `keepEffects == true` **不撤**。⛔ 别为了「让它认得出」去改那个窄口。
//   ③ `RemoveOwnExtrinsicEffects` 我们只有一半（`UnitState.RemoveExtrinsicEffectsFrom(卡名)`
//      只覆盖 `TempBuff.SourceCard` 与 `_statMods.Source` 两本账）—— 与 `BroadcastRemoveFromGame`
//      那一段的如实标注同源（见那个方法的 doc）。**照它的口径用**，⛔ 别另造第三本账。
//   ④ `UpdateWhileInPlay(true)` + `BroadcastWhileInPlay` 我们**不在这里手动发**：
//      本仓的对应物是 `Auras.Recompose`（棋盘变动 ⇒ 常驻重算）＋ `RuleCore.EmitWhileInPlayPass`
//      （= `BroadcastWhileInPlay` 的等价物，挂在**动作队列排空**那一刻，`ActionPump.cs:157`）
//      ⇒ 本方法跑在动作里，排空时自然会发。⛔ 别在这里补一发（会重复播）。
//   ⑤ `CardSetup` / `UpdateFigures` / `SetAmbush` / 督军聊天 / `AddEffect` = 表现层或读不出的格
//      ⇒ 不搬（见「三」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  账 172 + 548：在场单位变身（`transformUnit` = 动作 0x1B = 27）
        // ==================================================================

        /// <summary>
        /// **把棋盘上某格的那个单位换成另一张卡** —— 原版 `BattleManager.ResolveTransformUnit`
        /// 的**状态层**等价物（判据逐跳见本文件头 §一；⛔ 别在别处另写第二份）。
        ///
        /// <para>**跳序**（严格照 `.c` 的先后，⛔ 别重排）：
        /// ① 校验 `targetCard != null`（原版 `:106-155` 为 null ⇒ `LogError` + 结束，**什么都不换**）；
        /// ② `BroadcastRemoveFromGame(target, null)`（`:416`）；
        /// ③ 两手 `RemoveEffectsFromCard`（`:417-516`）⇒ 我们的 `RemoveHandEffectsCastBy`；
        /// ④ `keepEffects` 两支（`:559-655` / `:656-703`，机器码级判据见文件头 §二）；
        /// ⑤ 换卡定义（`:791`）—— **我们只能换成新的 `CardInstance` + `UnitState`**（文件头 §四·①）；
        /// ⑥ 落地那一步（`:1028/:1076/:1207`）—— `ApplyDeployTurnState(keepStatus: keepEffects)`
        ///    （**同一个 `keepEffects` 位同时控制召唤病**，判据见它的 doc）；
        /// ⑦ 常驻重算 + `CheckStun`（`:1580/:1629`）—— 前者由 `Auras.Recompose` 承担（文件头 §四·④）。
        /// </para>
        ///
        /// <para>🔴 **原版 bug 照抄**：`keepEffects == true` 那一支**不**撤待结算动作
        /// （`.c:656-703` 全文只有 `RemoveOwnExtrinsicEffects` 一句）。⛔ 别顺手补。</para>
        ///
        /// <para>📊 **可达性**：生产端 `chosenAbility 85` 池内 **0 张** ⇒ 今天零调用方（休眠）。
        /// 本方法是**给那个生产端准备的唯一出口**（⛔ 不是「先查着、以后总用得上」——
        /// 派活简报点名「无落点的战斗机制直接做」，账 172/548 等它）。</para>
        /// </summary>
        /// <param name="ctx">对局上下文。</param>
        /// <param name="owner">被变身那张牌的主人（0/1）。</param>
        /// <param name="slot">它在棋盘上的格号（`BoardSpec.Size` 之内）。</param>
        /// <param name="newCard">变成**哪一张卡**（原版 `battleAction.transformedIntoCard`）。</param>
        /// <param name="keepEffects">原版 `SummonCriteria.keepEffects`（`+0x2A`）。
        /// `false` ⇒ **整份重置成手牌数值** + 撤待结算动作 + 吃召唤病；
        /// `true` ⇒ **只摘「来源 = 它自己」的效果**、保留当前血量/关键词覆盖层、**不**吃召唤病。</param>
        /// <param name="by">日志前缀（缺省 = 该方玩家名）。</param>
        /// <returns>真的换了 ⇒ `true`；前置不成立（棋空格 / 卡为 null / ctx 结束）⇒ `false`（**并出声**）。</returns>
        public static bool TransformUnit(BattleContext ctx, int owner, int slot, CardDef newCard,
                                         bool keepEffects, string by = null)
        {
            if (ctx == null || ctx.IsOver) return false;
            if (owner < 0 || owner > 1)
            {
                UnityEngine.Debug.LogWarning($"[RuleEngine] TransformUnit：座位 {owner} 越界（只有 0/1）⇒ **不换**。");
                return false;
            }
            var ps = ctx.Players[owner];
            if (ps == null) return false;
            string who = string.IsNullOrEmpty(by) ? ps.Name : by;

            // ---- ① 原版 `:106-155`：targetCard == null ⇒ LogError + FinishResolvingAction + return ----
            if (slot < 0 || slot >= BoardSpec.Size)
            {
                ctx.Log($"{who}：变身的目标格 {slot} 越界 ⇒ **这次变身没生效**"
                      + "（原版 `ResolveTransformUnit.c:106` 对空目标只记一条错误就结束）");
                return false;
            }
            var old = ps.Board[slot];
            if (old == null)
            {
                ctx.Log($"{who}：格 {slot} 上没有单位 ⇒ **这次变身没生效**"
                      + "（原版 `ResolveTransformUnit with null targetCard`）");
                return false;
            }
            if (newCard == null)
            {
                ctx.Log($"{who}：「{old.Name}」要变身，但**变成哪张卡是空的** ⇒ **这次变身没生效**");
                return false;
            }

            bool wasWarlord = old.IsWarlord;
            bool wasInPlay = old.IsAlive;

            // ---- ② 原版 `:416`：BroadcastRemoveFromGame(targetCard, null) ----
            //   判据 ↔ 落点：`RuleCorePartial_B88E1.BroadcastRemoveFromGame` 的 doc 里点名的
            //   4 个原版调用点之一就是 `ResolveTransformUnit`（本行 = 那条第 4 个的接线）。
            BroadcastRemoveFromGame(ctx, old, null);

            // ---- ③ 原版 `:417-516`：两手各一次 `PlayerHand.RemoveEffectsFromCard(hand, targetCard)` ----
            //   我们的等价物 `RemoveHandEffectsCastBy` **自己就扫两方**（判据 = 它的 doc：
            //   「原版调用点传的是两手各一次 ⇒ 本方法扫两方」）⇒ **只调一次**，
            //   ⛔ 别照原版写两遍（第二次是空转，只会让人以为还有第二个入口）。
            //   语义 = 撤「**这张卡当施放者**挂出去的手牌效果」（不是「挂在它身上的」）。
            if (old.Instance != null) RemoveHandEffectsCastBy(ctx, old.Instance);

            // ---- ④ 原版 `:559-703`：keepEffects 两支（机器码级判据 → 文件头 §二）----
            if (!keepEffects)
            {
                // 假支：`:605` ResetToValuesInHand(true) ⇒ `:654` CancelEffectsFromDisappearingUnit(card, card, true)
                old.ResetToValuesInHand(true);
                int killed = CancelEffectsFromDisappearingUnit(ctx, old, old, true);
                if (killed > 0)
                    ctx.Log($"{who}：「{old.Name}」变身（不保留效果）⇒ 撤掉 {killed} 条待结算动作"
                          + "（原版 `ResolveTransformUnit.c:654`）");
            }
            else
            {
                // 真支：`:702` RemoveOwnExtrinsicEffects —— 只摘「来源 = 它自己」的那些。
                // ⚠️ 我们只覆盖 `TempBuff.SourceCard` + `_statMods.Source` 两本账（文件头 §四·③）。
                // 🔴 原版这一支**不**撤待结算动作（`:656-703` 全文没有 CancelEffectsFromDisappearingUnit）
                //    —— **照抄，⛔ 别补**。
                old.RemoveExtrinsicEffectsFrom(old.Card != null ? old.Card.Name : null);
            }

            // ---- ⑤ 原版 `:791` SetRawCardData：换卡定义 ----
            //   原版是「同一个 CardScript 换 rawCardData」；我们 `Instance`/`Card` 都 readonly
            //   ⇒ 发新 CardInstance（**沿用实例号**，理由见文件头 §四·①/②）+ 新 UnitState。
            var oldInst = old.Instance;
            var ni = oldInst != null ? new CardInstance(newCard, oldInst.Id) : ctx.NewInstance(newCard);
            CarryInstanceState(oldInst, ni, keepEffects);
            var nu = new UnitState(ni, wasWarlord);

            if (keepEffects)
            {
                // 真支：原版**不**调 ResetToValuesInHand ⇒ 当前血量 / 关键词覆盖层 / 回合内状态**都还在**。
                // ⚠️ 结构等价物：我们搬「关键词覆盖层 = 当前关键词 − 新卡面」与「当前血量」，
                //    另外搬几个回合内计数器（见 CarryTurnState）。⛔ 这不是把 `activeEffects` 整本搬过去
                //    （我们那三本账是 private，没有公开的搬迁口）⇒ 未覆盖的见报告「差什么」。
                CarryKeywordOverlay(old, nu);
                int cap = nu.MaxHealth;
                int hp = old.Health;
                if (hp > cap) hp = cap;
                if (hp > 0) nu.Health = hp;
                CarryTurnState(old, nu);
            }

            // ---- ⑥ 写回格位（原版同一个对象，位置不动）----
            ps.Board[slot] = nu;
            // 督军格：`ps.Warlord` 是**另一份引用**，换对象必须跟着换（原版同一个对象 ⇒ 引用不变）。
            if (ReferenceEquals(ps.Warlord, old)) ps.Warlord = nu;

            // ---- ⑦ 落地 + 常驻重算 + CheckStun（原版 `:1076/:1207` · `:1580/:1726` · `:1629`）----
            //   顺序照 `DeployFree` 的写点（棋盘写入点唯一口径：`Recompose` **先**、`ApplyDeployTurnState` **后**）。
            Auras.Recompose(ctx);                                  // 棋盘变动 ⇒ 光环重算（9 个写入点同一纪律）
            ApplyDeployTurnState(nu, keepStatus: keepEffects);      // 原版 `:1207` 的第 5 实参 = 同一个 keepEffects
            CheckStun(ctx, nu);                                     // 原版 `:1629`

            ctx.Log($"{who}：「{old.Name}」{old.Health}血 变身成「{newCard.Name}」（格 {slot}"
                  + $"{(wasInPlay ? "" : "，原本不在场")}；keepEffects = {(keepEffects ? "真" : "假")}"
                  + $" ⇒ 血量 {nu.Health}/{nu.MaxHealth}、关键词 {nu.Keywords.Count} 条"
                  + (wasWarlord ? "，**这是督军格**" : "") + "）");
            return true;
        }

        // ==================================================================
        //  读数口：`SummonCriteria.keepEffects`（生产端要传的那一格）
        // ==================================================================

        /// <summary>
        /// **这张卡（作为变身时被施放的那张）的 `SummonCriteria.keepEffects` 读得到读不到** ——
        /// 原版 `AbilityLogic__PlayAbility.c:903/939/977` 的实参 = `summonCriteria(+0x58).keepEffects(+0x2A)`。
        ///
        /// <para>返回 `null` = **卡表里这一格是未知**（`-1` 哨兵 / 这条卡没有 `abilitySummon` 列）——
        /// 消费端**必须出声、⛔ 不许当成 `false`**（否则「表是旧的」会被静默读成「重置效果」，
        /// 而那正好是**另一档行为**）。这与 `CardInstance.ArmedTrap` 的三态口径同源。</para>
        ///
        /// <para>📊 现读可达性：`cards_truth.json` 里 `chosenAbility == 85` **0 张**；
        /// `SummonCriteria.keepEffects` 在全池也**读不到真值**（原始 ability SO 在远端 CCD）
        /// ⇒ 今天**必然返回 `null`**（休眠件，照样建）。</para>
        /// </summary>
        public static bool? KeepEffectsOf(CardDef card)
        {
            if (card == null) return null;
            var rows = card.AbilitySummons;
            if (rows == null) return null;                     // 整列未知（旧表 / 手写夹具）
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null) continue;
                if (r.KeepEffects < 0) return null;            // `-1` 哨兵 = 读不到，⛔ 不当 0
                return r.KeepEffects != 0;
            }
            return null;
        }

        // ==================================================================
        //  本文件私有助手
        // ==================================================================

        /// <summary>
        /// 把**实例态**从旧的 `CardInstance` 搬到新的那份。
        ///
        /// <para>`keepEffects == false` 时**只搬「原版 `ResetToValuesInHand` 不碰」的那几格**
        /// （`CardInstance.cs` 文件头 + `UnitState.ResetToValuesInHand` 的 doc：原版 `.c` 全文没碰
        ///  `companionCounter` / `tideCounter` / `orderInPlay` / `isPraying`）；
        /// 几本效果账（`ActiveEffects` / `ActiveEnchantments` / `QuestCounter`）**在新的那份里天然是空的**
        /// = 原版那句 `List.Clear()` ⇒ **不搬**。
        /// `keepEffects == true` 时原版**一个 `Clear` 都不跑** ⇒ **全搬**（含几本账的内容）。</para>
        /// </summary>
        static void CarryInstanceState(CardInstance from, CardInstance to, bool keepEffects)
        {
            if (from == null || to == null) return;
            // 原版 ResetToValuesInHand 不碰的几格：两档都搬
            to.IsCompanion = from.IsCompanion;
            to.CompanionCounter = from.CompanionCounter;
            to.CostDelta = from.CostDelta;
            to.DrawnThisTurn = from.DrawnThisTurn;
            to.EphemeralMarked = from.EphemeralMarked;
            to.MulliganOption = from.MulliganOption;
            if (!keepEffects) return;
            // 真支：原版不跑任何 Clear ⇒ 几本账连同计数一起搬
            to.QuestCounter = from.QuestCounter;
            for (int i = 0; i < from.HandEffects.Count; i++) to.HandEffects.Add(from.HandEffects[i]);
            for (int i = 0; i < from.ActiveEffects.Count; i++) to.ActiveEffects.Add(from.ActiveEffects[i]);
            for (int i = 0; i < from.ActiveEnchantments.Count; i++)
                to.ActiveEnchantments.Add(from.ActiveEnchantments[i]);
        }

        /// <summary>
        /// `keepEffects == true` 支：把旧单位的**关键词覆盖层**（= 当前关键词 − **新卡面**印的那一份）
        /// 搬到新单位上。
        ///
        /// <para>判据形状：原版这一支不跑 `ResetToValuesInHand` ⇒ `activeEffects` 里那些
        /// `buffType 3 (addTrait)` 的记录**还在**，而新卡面在 `SetRawCardData` 之后**已经换了**
        /// ⇒ 净值 = 旧当前值 − 新卡面值。🔴 **只补差额、不回搬卡面那一份**（回搬会把新卡自己的
        /// 关键词加成两次）。</para>
        ///
        /// <para>⚠️ **结构差异（如实标）**：我们的 `_keywords` 是**合并字典**，分不出「卡面印的」
        /// 与「效果给的」；这里用「对新卡面取差」来还原覆盖层 —— 与
        /// `UnitState.ClearAllKeywords` 的 doc 里那条已知差异同源。</para>
        /// </summary>
        /// <returns>真的补上去几条。</returns>
        static int CarryKeywordOverlay(UnitState from, UnitState to)
        {
            if (from == null || to == null) return 0;
            var face = to.Card != null ? to.Card.Keywords : null;
            int n = 0;
            foreach (var kv in from.Keywords)
            {
                int baseVal = 0;
                if (face != null && face.ContainsKey(kv.Key)) baseVal = face[kv.Key];
                int delta = kv.Value - baseVal;
                if (delta <= 0) continue;
                if (to.AddKeyword(kv.Key, delta)) n++;
            }
            return n;
        }

        /// <summary>
        /// `keepEffects == true` 支：搬回合内那几个计数器。
        ///
        /// <para>判据（`CardScript__ResetToValuesInHand.c` 的逐句清单，见
        /// `UnitState.ResetToValuesInHand` 的 doc ④⑤⑧）：**只有** `attacksThisTurn` /
        /// `usedActiveAbility` / `usedActiveAbilityTimesPerTurn` / 两个「回合开始时就在某状态」位 /
        /// `removeMarkerlightAtEndOfTurn` 会被清 —— 其余（本方法搬的这些）**原版留着**。
        /// 假支不搬：新的 `UnitState` 本身就是重置态（= 原版跑完 `ResetToValuesInHand` 的结果）。</para>
        /// </summary>
        static void CarryTurnState(UnitState from, UnitState to)
        {
            if (from == null || to == null) return;
            to.DeployedTurn = from.DeployedTurn;
            to.PlayOrder = from.PlayOrder;
            to.OathUsesThisTurn = from.OathUsesThisTurn;
            to.DutyUsed = from.DutyUsed;
            to.Prayed = from.Prayed;
            to.FerocityStay = from.FerocityStay;
            // ⚠️ `IsRemnant` / `IsLeavingRemnant` / `FaceDown` / `SwarmUnder` / `ReferencedCards`
            //    **不搬** —— 前者两个是「残骸身份」，变身的靶是在场单位；后两个是引用表，
            //    我们的实现里没有「别人指着我」的反向更新口（如实留缺口，见报告）。
        }
    }
}
