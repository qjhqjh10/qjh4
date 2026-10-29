// RuleCorePartial_B96CB.cs — 第九十五会话 · 战斗线 C21 · 执行代理 `B96_CB` 的 `RuleCore` partial 扩展
//
// 🔴 为什么单开文件（同 `RuleCorePartial_B88E14.cs` / `_B93F7.cs` / `_B94B8.cs` 的纪律）：
//   `RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 365+ 处，往它中间插成员会让下游落点整体漂移
//   ⇒ 新成员一律落本文件（铁律 18·b）。
//
// 本批（`B96_CB`）四件（账行号 = `资料/完全复刻/账/战斗逻辑驱动.tsv` 的物理行号）：
//   · 921  `CardScript.ShowCardBeingDrawn(float)` —— 两档（弃牌揭示 0.2 / 偷牌揭示 0.4）
//          的**唯一发射口** <see cref="EmitCardBeingDrawn"/>（`EvtKind` 那两个成员在 `BattleEvent.cs`）。
//   · 1037 `EntityScript.GetThisCardPlayedDamage(CardScript)` —— 「被打出的那张牌是**手牌里的卡**」那一档的
//          取数口（棋盘单位那一档早已落在 `RuleCorePartial_B94B8.cs:147/158`）。
//   · 372/422 `BattleManager.GetPremulliganScriptedData` / `IsPremulliganScriptAvailable` —— **执行那一半**
//          <see cref="RunPremulliganScriptedTurn"/>（谓词/取数在 `TutorialPremulligan.cs`，单一正本，本文件只转调）。
//   · 248  `BattleManager.CheckForHeroDeckTransform(DeckAndWarlordData, bool)` —— **换牌库写口**
//          <see cref="CheckForHeroDeckTransform"/>（算法 `BoardAnalysis.DeckMeetsCriteria` 已落 `BoardAnalysis_B94B8.cs:45`）。
//
// ⚠️ 判据出处写在每个方法头上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  账 921 · `CardScript.ShowCardBeingDrawn(float stepTime)`
        // ==================================================================
        //
        // **第一权威**（`d:/2/tools/decomp_full/CardScript__ShowCardBeingDrawn.c` 全文 14 行，亲读）：
        //   ① `cardState(+0x228) = 9`（`inHandMoving`）；② `BattleCardUI.SetSortingLayer(HandMoving)`；
        //   ③ `if (isPlayer == 0) TurnCardAround(this, stepTime, stepTime)` —— **只有「不是自己那张」才翻**。
        //   两个调用点各用自己的 `stepTime`（⛔ 别统一）：
        //     · `BattleManager._ResolveDiscardFromHand_d__544__MoveNext.c:95` = **0.2**
        //       （`.rdata 0x1834b2bb0`，`工具/read_literal.py` 实读；
        //        我们的常量 = `CardFeel.DiscardRevealStepTime`）；
        //     · `BattleManager._ResolveStealFromDeck_d__547__MoveNext.c:243` = **0.4**
        //       （`.rdata 0x1834b3158`；`CardFeel.StealRevealStepTime`）。
        //
        // 🔴 **原版这两拍各在结算的哪一刻**（判据 = 上面那两条协程）：
        //   · 弃牌：`_ResolveDiscardFromHand` 的**演出段**（净语义 ④「RemoveCardFromHand」**之前**）；
        //   · 偷牌：`_ResolveStealFromDeck` 的 `:239 ResetToValuesInHand` **之后**、`:81 RemoveCardFromDeck`
        //     与 `:90 AddCardNotDrawnToHand` **之前**。
        //   ⇒ 两条都在「这张牌**还在原区域里**、已经能被看见」那一刻 ⇒ 本事件也必须在**摘除之前**发。
        //
        // ⚠️ **字段**（消费端按此读）：
        //   `Kind` = <see cref="EvtKind.DiscardReveal"/> / <see cref="EvtKind.StealReveal"/> ·
        //   `Player` = **原持有方**（= 原版 `EntityScript.isPlayer(+0x40)`，即「这张牌属于谁的手/牌库」）·
        //   `Slot` **恒 -1**（不在场上）· `CardId` = 卡名 · `InstanceId` = 这一份的实例号（`CardInstance.Id`）。
        // ⚠️ **纯表现信号**：不改任何局面、不进动作流（`BattleEvent` 不进 `.json`）。
        /// <summary>
        /// 账 921 的**唯一发射口** —— 原版 `CardScript.ShowCardBeingDrawn(float stepTime)` 两档
        /// （判据与时刻见文件头那段）。
        /// <para>⛔ **不要在各调用点各拼一份 `BattleEvent`**（两处写同一条规则 = 迟早不一致）。</para>
        /// </summary>
        /// <param name="seat">**原持有方**的座位（= 原版 `isPlayer`；弃牌 = 那一手的主人，偷牌 = **被偷**那一方）。</param>
        /// <param name="inst">被揭示的那一份牌（`null` ⇒ 什么都不发并出声）。</param>
        /// <param name="isSteal">`false` = 弃牌揭示（0.2）· `true` = 偷牌揭示（0.4）。</param>
        public static void EmitCardBeingDrawn(BattleContext ctx, int seat, CardInstance inst, bool isSteal)
        {
            if (ctx == null) return;
            if (inst == null || inst.Card == null)
            {
                ctx.Log("[ShowCardBeingDrawn] 要揭示的那一份牌是空的 —— **这条没发**（原版 `CardScript.ShowCardBeingDrawn` "
                      + "永远有一张 CardScript 实参；我们这里取不到 ⇒ 如实出声，⛔ 不静默）");
                return;
            }
            ctx.Emit(isSteal ? EvtKind.StealReveal : EvtKind.DiscardReveal,
                     seat, -1, inst.Card.Name, instanceId: inst.Id);
        }

        // ==================================================================
        //  账 1037 · `EntityScript.GetThisCardPlayedDamage(CardScript targetCard)` 的【手牌】那一档
        // ==================================================================
        //
        // 棋盘单位那一档（`U.FxOps(keyword)`）已落 `RuleCorePartial_B94B8.cs:147/158`；这里补的是
        // **「被打出的那张牌是手牌里的卡」**——原版 `CardBoardHighlight__ToggleCombatPreviewHighlight.c:48`
        // 的 `GetThisCardPlayedDamage(other, this)` 里 `other` **就是**那张要打出去的牌（本次现读，判据同 `.c`）：
        //   取 `other.rawCardData.GetAbilitiesForTriggerRawcard(ThisCardPlayed(0))`，逐条 ability 判 criteria
        //   后只收 `{10 doDamage, 12 doDamageMultiRandom, 15 doDamageAndDestroySelf}` 三档的
        //   `damageCriteria.minDamage(+0x14)` —— 🔴 **原版漏 `13 doDamageFast`，照抄、⛔ 别补齐**。
        //
        // ⚠️ **本工程的结构差异（如实标）**：我们的战术卡正文**不是**「ability + chosenAbility + damageCriteria」，
        //   而是 `EffectText.Parse(card.Desc)` 出来的 `EffectOp` 串 —— 打出去时 `EffectResolver.PlayTacticCore`
        //   读的**就是这一份**（`EffectResolver.cs:2564` 那句 `EffectText.Parse(card.Desc, …)`）。
        //   ⇒ 结构等价物 = 「把手牌那张卡的正文解析出来、只收**打在玩家点选目标上的 `deal`**」
        //   （与 `AddDealEntries` / `DealsToPickedTarget` 同一套口径，判据见 `RuleCore.cs:8345`）。
        /// <summary>
        /// 「**这张（手牌里的）牌被打出时会打出的伤害**」逐条取数 —— 原版
        /// `EntityScript.GetThisCardPlayedDamage` 在「被打出的是手牌卡」那一档的结构等价物
        /// （判据与差异见方法组头注释）。
        /// </summary>
        /// <returns>逐条伤害（不含护甲/减伤 —— 与 `AttackDamageEntries` 同口径）。空表 = 这一张没有这类伤害。</returns>
        public static List<int> ThisCardPlayedDamageEntries(BattleContext ctx, CardDef def)
        {
            var list = new List<int>();
            if (def == null) return list;
            // 正文缺失（卡面没有效果文字）⇒ 原版那 183 张之外的那些，一条都不收
            if (string.IsNullOrEmpty(def.Desc)) return list;
            // ⚠️ 解析的实参口径照 `EffectResolver.PlayTacticCore`（`card:` 传进去让「无 choose 的卡」也注入
            //    `choicesDefinition`；那一条对我们「只挑 deal」没有影响，但它决定了 op 的形状 ⇒ 必须同口径）。
            List<string> unparsed, partial;
            var ops = EffectText.Parse(def.Desc, out unparsed, out partial, card: def);
            AddDealEntries(ops, list);
            return list;
        }

        /// <summary>`GetThisCardPlayedDamage` 原样的返回形态（一个 `int`）。见 <see cref="ThisCardPlayedDamageEntries(BattleContext, CardDef)"/>。</summary>
        public static int ThisCardPlayedDamage(BattleContext ctx, CardDef def)
        {
            var l = ThisCardPlayedDamageEntries(ctx, def);
            int sum = 0;
            for (int i = 0; i < l.Count; i++) sum += l[i];
            return sum;
        }

        // ==================================================================
        //  账 372 / 422 · preMulligan 脚本的【执行那一半】
        // ==================================================================
        //
        // **第一权威**：`BattleManager._StartBattleSequence_d__331__MoveNext.c:33` —— 判门
        //   `IsPremulliganScriptAvailable()` 为真 ⇒ `ExecuteScriptedTurn(GetPremulliganScriptedData(), newScriptedTurn: 1, …)`
        //   并**按住协程**，`:81-88` 才 `_SetupMulliganPhase` / `TutorialStartSequence`。
        // 谓词/取数 = `TutorialPremulligan.IsPremulliganScriptAvailable` / `.GetPremulliganScriptedData`
        //   （**单一正本**，`Core/TutorialPremulligan.cs:36/51`；⛔ 本文件只转调，不写第二份闸）。
        //
        // ⚠️ **顺序怎么保证**：原版靠协程 `yield`；我们的脚本执行器 `TutorialScript.ExecuteAction` 是**同步**的
        //   （每条动作当场落到引擎 + 发 `TutorialScript.Executed` 给表现层）⇒ 「**先跑完、调用方才继续**」
        //   在调用方那一侧是**天然的**（本方法返回之后 `BattleDriver.Begin` 才进换牌/首回合）。
        //   需要等演出时才异步 —— 那一条由 `ICampaignScriptHost` / `TutorialScript.Executed` 的订阅方自己排，
        //   ⛔ 不在这里造第二套调度。**如实记**：原版是「等脚本协程跑完」，我们是「同步跑完 + 演出并行」。
        //
        // 📌 **数据现状**：`tutorial_stages.json` 6/6 关 `preMulligan.scriptedTurn = false`
        //   ⇒ 谓词**恒假**、本方法今天恒返回 0、那 19 条动作原版也不跑（休眠 ≠ 不可达，⛔ 不判 D）。
        /// <summary>
        /// 跑「换牌前脚本」（原版 `_StartBattleSequence:33` 那一跳）。
        /// <para>返回**真的交给执行器的条数**（`0` = 本局没有这条脚本 ⇒ 调用方照原样继续）。</para>
        /// </summary>
        public static int RunPremulliganScriptedTurn(BattleContext ctx)
        {
            if (ctx == null || ctx.Tutorial == null) return 0;
            var stage = ctx.Tutorial.Stage;
            // 门（判据 = `BattleManager__IsPremulliganScriptAvailable.c`，单一正本在 `TutorialPremulligan`）
            if (!TutorialPremulligan.IsPremulliganScriptAvailable(stage)) return 0;
            var turn = TutorialPremulligan.GetPremulliganScriptedData(stage);
            if (turn == null || turn.actions == null || turn.actions.Length == 0) return 0;
            // 原版第二个实参 `resetActionCounter: 1`（`d__597` 协程开头那句 `ResetActionCounter()`）
            ctx.Tutorial.ResetActionCounter();
            int n = 0;
            for (int i = 0; i < turn.actions.Length; i++)
            {
                var a = turn.actions[i];
                if (a == null) continue;
                // 与战后脚本**同一台执行器**（`BattleDriver.TickPostBattleScript` 也是这一句）——
                // ⛔ 别直接调 `ApplyTutorialActionView`：那样认不出的动作类型会被静默吞掉。
                ctx.Tutorial.ExecuteAction(ctx, a);
                n++;
            }
            ctx.Log($"[Tutorial] 换牌前脚本跑完了（原版 `_StartBattleSequence:33` → `ExecuteScriptedTurn`）："
                  + $"第 {stage.stage} 关 · **{n}/{turn.actions.Length} 条**交给执行器");
            return n;
        }

        // ==================================================================
        //  账 248 · `BattleManager.CheckForHeroDeckTransform(DeckAndWarlordData, bool)`
        // ==================================================================
        //
        // **第一权威**（`decomp_full/BattleManager__CheckForHeroDeckTransform.c` + `BoardAnalysis__DeckMeetsCriteria.c` 101 行；
        // 调用点 = `SetupBoardPhase.c:56/66` 的 `SetPlayerDataAndWarlord` 族）：
        //   `rawBattleData != null` ∧ 关卡给了「督军替换卡」∧ `BoardAnalysis.DeckMeetsCriteria(牌表, 替换卡)`
        //   ⇒ **把该玩家的牌库换成「变身督军」那一副**。
        // 我们已有的那一半 = `BoardAnalysis.DeckMeetsCriteria`（`BoardAnalysis_B94B8.cs:45`，111 行算法等价物）。
        //
        // ⚠️ **数据现状（🔴 本机不存在，⛔ 不许自己编）**：原版读的是 `rawBattleData + 0x18`（变身督军）与
        //   `+ 0x28`（牌表）两格；我们的 `ctx.CampaignStage`（`BattleContext.cs:922`）是 `TutorialStageData`，
        //   **没有这两格**、且 `tutorial_stages.json` 也没有 ⇒ 接上**零可观测**。
        //   ⇒ 本方法是**写口 + 判据**（铁律 11：休眠机制照样建），**今天没有任何生产调用能读到真数据**。
        // ⚠️ **牌库写口**：`PlayerState.Deck` 是 `List<CardInstance>`（`PlayerState.cs:19`）——
        //   原版是「换掉那一副牌」，所以**整体换内容**（`Clear` + 逐张新建实例），⛔ 不是「往现有牌库里插」。
        /// <summary>
        /// 原版 `BattleManager.CheckForHeroDeckTransform` 的等价物：满足判据就把 <paramref name="seat"/> 的牌库
        /// 换成 <paramref name="substituteDeck"/> 那一副。
        /// <para>返回 `true` = 真的换了。**任一项输入为空 ⇒ 返回 `false` 并（只在第一次）出声** ——
        /// 原版那两道闸也不过（`rawBattleData == null` / 关卡没给替换卡）。</para>
        /// </summary>
        /// <param name="transformCard">「变身督军」那张卡（原版 `rawBattleData + 0x18`）。</param>
        /// <param name="substituteDeck">替换后的牌表（原版 `rawBattleData + 0x28`）。</param>
        public static bool CheckForHeroDeckTransform(BattleContext ctx, int seat,
                                                     CardDef transformCard, IReadOnlyList<CardDef> substituteDeck)
        {
            if (ctx == null || seat < 0 || seat > 1) return false;
            var ps = ctx.Players[seat];
            if (ps == null) return false;
            // 🔴 数据两格本机不存在（见方法组头注释）⇒ 恒走这一支。⛔ 别为了让「有调用点」而造假数据。
            if (transformCard == null || substituteDeck == null || substituteDeck.Count == 0) return false;
            // 判据 = `BoardAnalysis.DeckMeetsCriteria(牌表, 替换卡)` —— 原版的「这副牌是不是属于他要变身的那一位」
            //   （算法单一正本在 `BoardAnalysis_B94B8.cs:45`，⛔ 这里不重写）。
            //   原版那两个形参 `mode` / `targetCount` 在 `CheckForHeroDeckTransform` 的调用点传的是
            //   `DeckCriteria.count` 与其 `targetCount`；本机取不到（`tutorial_stages.json` 无此格）⇒ 传 `0/0`
            //   （= `DeckMeetsCriteria` 的「没有数量条件」档），**如实记**。
            //   ⚠️ 匹配键 = `CardDef.Id`（= 卡表里的数据库名那一列；与 `cards_engine.json` 的 `id` 同源）。
            string wantId = transformCard.Id;
            bool meets = BoardAnalysis.DeckMeetsCriteria(substituteDeck,
                                                         c => c != null && c.Id == wantId,
                                                         0, 0);
            if (!meets)
            {
                ctx.Log($"「变身督军」判据不满足（原版 `BoardAnalysis.DeckMeetsCriteria` 为假）⇒ {ps.Name} 的牌库**不换**");
                return false;
            }
            // ---- 换牌库（原版「换成那一副」；整体换内容，⛔ 不是追加）----
            ps.Deck.Clear();
            for (int i = 0; i < substituteDeck.Count; i++)
            {
                var d = substituteDeck[i];
                if (d == null) continue;
                ps.Deck.Add(ctx.NewInstance(d));     // 每一张都是**新的一份**（原版换的是整副牌的对象）
            }
            ctx.Log($"{ps.Name} 的牌库被换成「变身督军」那一副（{ps.Deck.Count} 张）"
                  + " —— 原版 `BattleManager.CheckForHeroDeckTransform`（判据见方法组头注释）");
            return true;
        }

        /// <summary>「变身督军」那张卡的**取值口**（原版 `rawBattleData + 0x18`）。
        /// <para>🔴 **本机查不到 ⇒ 恒 `null`**（判据：`ctx.CampaignStage` 的类型是 `TutorialStageData`，
        /// 它没有「变身督军」这一格；`Resources/tutorial_stages.json` 逐关也没有）。
        /// **这是「查不到」不是「不做事」** —— 数据落地那天**只改这一个方法**，
        /// `RuleCore.NewBattle` 里那两个调用点（账 248）不用动。</para></summary>
        public static CardDef CampaignHeroTransformCard(BattleContext ctx)
        {
            // 原版读 `rawBattleData(+0x30 那个对象) + 0x18`；我们连那个对象都没有。
            return null;
        }

        /// <summary>「变身督军」替换用的**牌表**（原版 `rawBattleData + 0x28`）。见 <see cref="CampaignHeroTransformCard"/>：本机恒 `null`。</summary>
        public static IReadOnlyList<CardDef> CampaignHeroTransformDeck(BattleContext ctx)
        {
            return null;
        }
    }
}
