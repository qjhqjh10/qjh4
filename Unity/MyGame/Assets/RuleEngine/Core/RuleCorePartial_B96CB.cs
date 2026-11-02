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
        //  账 248 · `BattleManager.CheckForHeroDeckTransform(DeckAndWarlordData, bool)`　📌 **本功能的集中注释（变身 / 战将）→ 文件末「§变身（督军换将）」那一大段**（原版判据链 · 我们的等价物 · 为什么休眠 · 以后的 mod 入口清单）—— 本段只讲这一次订正。
        // ==================================================================
        //
        // 🔴🔴 **2026-11-03（`B120_W30`）就地订正（铁律 5）—— `B96_CB` 那一版把语义读错了**：
        //   旧注释写「满足判据 ⇒ **把该玩家的牌库换成**『变身督军』那一副」「原版读 `rawBattleData+0x18`(督军) /
        //   `+0x28`(牌表)，我们连那个对象都没有」——**两条都不成立**（下面逐条给反证）。
        //
        // **第一权威**（本笔逐句现读）：
        //   · `decomp_full/BattleManager__CheckForHeroDeckTransform.c`（101 行，全文）
        //   · `decomp_full/BoardAnalysis__DeckMeetsCriteria.c`（101 行，全文）
        //   · `decomp_full/BattleManager.__c__DisplayClass282_0___CheckForHeroDeckTransform_b__0.c`（谓词，15 行）
        //   · `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:1097-1145`（同一方法的可读版，逐句交叉）
        //   · 调用点 `decomp_full/BattleManager__SetupBoardPhase.c:56/66`：`CheckForHeroDeckTransform(
        //       matchData.playerRawBattleData(+0x58), isPlayer:1)` / `(enemyRawBattleData(+0x50), isPlayer:0)`
        //   · 字段表 `dump.cs:77945`（`DeckAndWarlordData`）· `:78170`（`MatchData.originalPlayerWarlord/originalEnemyWarlord`）
        //   · `RawCardScript` 字段表 `dump.cs:23244`：`deckCriteria // 0x1F8` · `transformedWarlordId // 0x200`
        //   · `PlayerItem.uniqueId // 0x38`（`dump.cs:18804`）· `DeckCriteria`（`dump.cs:21785`：`countCriteria+0x10` /
        //     `triggerIf+0x18` / `referenceValue+0x1C`）· `DeckAndWarlordData.deckWarlord // 0x18`（`:77949`）
        //
        // ── 一、这个机制**到底在做什么**（`.c` + il2csharp 双向对齐）────────────────────────────────
        //   ① 取**这一副牌自己的督军** `rawBattleData.deckWarlord(+0x18)` —— 🔴 **就是这个座位开局那位督军**，
        //      ⛔ **不是**关卡/战役脚本里的什么「替换卡」；
        //   ② `BoardAnalysis.DeckMeetsCriteria(rawBattleData.shuffledCardLibrary(+0x28), deckWarlord)`：
        //      拿**这副牌本身的牌表**按督军身上的 `deckCriteria(+0x1f8)` 逐张筛、比张数
        //      （`countCriteria.targetsAffected == 0` ⇒ 原版**直接 return 1**「无条件即满足」；否则按
        //       `triggerIf(+0x18)` 1/2/3 = 相等/≥/≤ 与 `referenceValue(+0x1c)` 比）；
        //   ③ 满足了就按 `deckWarlord.transformedWarlordId(+0x200)`（**另一个督军的 id**）到
        //      `PlayerDataManager.fullHeroCollection` 里 `Find(hero.uniqueId(+0x38) == 那个 id)`；
        //   ④ 找到了：`matchData.originalPlayerWarlord(+0xd8)` / `originalEnemyWarlord(+0xe0)` ← 原督军，
        //      再把 `rawBattleData.deckWarlord` **换成找到的那一位**；玩家侧再
        //      `VoiceLinesController.SetTransformedWarlordNewChats`。
        //   ⇒ **只换「督军」这一格，牌库一张都不动**。名字 `HeroDeckTransform` = 「这副牌的督军变身」，
        //     不是「换一副牌」。🔴 谓词比的是 `hero.uniqueId == deckWarlord.transformedWarlordId`
        //     ——**两个不同的 id**（「变身成另一位督军」），所以「名字叫变身、比的是 id」没有矛盾；
        //     旧注释把它读成「同一个 id / 换牌库」是**按名字猜语义**（派活简报点名的那一类错）。
        //
        // ── 二、「没有对应数据」这个前提**错了一半**（旧注释的原文：`ctx.CampaignStage` 没有两格 ⇒ 恒 null）──
        //   ✅ **结构输入我们全有**：`DeckAndWarlordData` = **该座位自己的那副牌** ——
        //      `deckWarlord(+0x18)` = `PlayerState.Warlord.Card`、
        //      `shuffledCardLibrary(+0x28)` = `PlayerState.Deck`（CardDef 视图）
        //      ⇒ 两个取值口 <see cref="CampaignHeroTransformCard"/> / <see cref="CampaignHeroTransformDeck"/>
        //        **不再恒 null**（`RuleCore.NewBattle` 里两个调用点仍在原位，现在拿得到真数据）。
        //   ❌ **真正缺的是督军卡身上的两列**：`transformedWarlordId(+0x200)` 与 `deckCriteria(+0x1f8)`。
        //      我们卡表（`cards_engine.json` / `CardDef`）**没有这两列**，生成器也不输出；
        //      原始 SO 现扫 `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `full_raw`：
        //      **1,351 张里 `transformedWarlordId` 非空 = 0**、`deckCriteria` **全是默认档**
        //      （`countCriteria.targetsAffected = 0`）⇒ **本 build 里这条机制是「休眠」，不是「不可达」**。
        //   ⇒ 数据落地那天只改 <see cref="CampaignHeroTransformTargetId"/> /
        //      <see cref="CampaignHeroTransformDeckMeets"/> 两个取值口（写口与调用点都不用动）。
        //   ⛔ 不许为了让机制「跑起来」去编一个变身表 —— 那是卡 SO 上的字段，不是服务器档口可自设计的那一类。
        //
        // ── 三、还差什么（如实记，⛔ 别自己造字段）──────────────────────────────────────────────────
        //   · `MatchData.originalPlayerWarlord/originalEnemyWarlord` 的等价物 —— `BattleContext` **没有**这两格。
        //     本办法让写口用 `out originalWarlord` **交回**给调用方（原版写 matchData 的那一格）；
        //     调用方今天只能记日志。真要「换人演出 / 战后还能取原督军」，需要调度台在 `BattleContext` 上开一格。
        //   · `fullHeroCollection`（玩家持有的英雄总表）—— 引擎侧等价物 = `ctx.CardPool` 里 `Type == "hero"` 的那些
        //     （本项目全解锁）。`CardPool` 没给 ⇒ 如实出声、不换（⛔ 不拿牌库/卡组当英雄总表）。
        //   · `VoiceLinesController.SetTransformedWarlordNewChats` —— 表现层（本写手白名单外），今天没有等价调用。
        //
        // ⚠️ **与 `BoardAnalysis.DeckMeetsCriteria` 的分工**：判据本体在 `BoardAnalysis_B94B8.cs:45`（单一正本），
        //   本文件**不重写**；这里只接「原版那两个实参从哪来」这一跳。

        /// <summary>
        /// 原版 `BattleManager.CheckForHeroDeckTransform` 的**完整等价物**（判据 + 记录原督军 + 换督军）。
        ///
        /// <para>**跳序**（严格照 `.c`，⛔ 别重排）：① `deckWarlord == null` ⇒ 出声 + 不换（原版 `:29-35`）；
        /// ② `DeckMeetsCriteria` 不满足 ⇒ 不换（`:38-42`）；③ 按 `transformedWarlordId` 在英雄总表里找不到
        /// ⇒ 不换（`:43-56`）；④ `originalWarlord` ← 原督军、把督军换成找到的那位（`:57-74`）。</para>
        ///
        /// <para>📊 **今天恒走 ②/③ 的不换那一支**：`deckCriteria` / `transformedWarlordId` 两列本机没有
        /// （见方法组头 §二）。**休眠 ≠ 不可达**（铁律 11）—— 写口、判据与换人动作都已就位。</para>
        /// </summary>
        /// <param name="deckWarlord">原版 `DeckAndWarlordData.deckWarlord(+0x18)`：**该座位开局那位督军**。</param>
        /// <param name="deck">原版 `shuffledCardLibrary(+0x28)`：**该座位自己的牌表**（只用来判 criteria，⛔ 不会被改）。</param>
        /// <param name="transformedWarlordId">原版 `deckWarlord.transformedWarlordId(+0x200)`：要变身成的那位督军的 id。</param>
        /// <param name="deckMeetsCriteria">原版 `BoardAnalysis.DeckMeetsCriteria(deck, deckWarlord)` 的结果。
        /// `null` = **这一列查不到**（本机现状），⛔ 不当作「满足」、也不当作「不满足」静默处理。</param>
        /// <param name="originalWarlord">原版写进 `matchData.originalPlayerWarlord/originalEnemyWarlord` 的那一格
        /// （**换成的那一位生效时**才非 null；换不成功恒 null）。</param>
        /// <returns>`true` = 真的换了督军。</returns>
        public static bool CheckForHeroDeckTransform(BattleContext ctx, int seat,
                                                     CardDef deckWarlord, IReadOnlyList<CardDef> deck,
                                                     string transformedWarlordId, bool? deckMeetsCriteria,
                                                     out CardDef originalWarlord)
        {
            originalWarlord = null;
            if (ctx == null || seat < 0 || seat > 1) return false;
            var ps = ctx.Players[seat];
            if (ps == null) return false;

            // ① 原版 `:29-35 / :88-99`：rawBattleData（或其 deckWarlord）为空 ⇒ LogError + return
            // 🔴 **本组一律走 `UnityEngine.Debug`、⛔ 不用 `ctx.Log`**：`ctx.Log` 写的是 `ctx.Events`，
            //   而 `ctx.Events.Count` 被 `NetProtocol.Fingerprint` 计入 ⇒ 在「今天每局都走」的休眠分支里
            //   打日志会**改掉全池 1,128 张的 `play check` 指纹**（判据 → `ActionQueue.cs:596` 的同一条提醒）。
            if (deckWarlord == null)
            {
                UnityEngine.Debug.LogWarning($"[HeroDeckTransform:{ps.Name}] 这副牌没有督军（原版 `rawBattleData.deckWarlord(+0x18)` 为空）⇒ **不换**");
                return false;
            }
            // 原版 `:38` 的实参 `shuffledCardLibrary` 为空时 `DeckMeetsCriteria` 无牌可筛 ⇒ 同样不可能满足
            if (deck == null)
            {
                UnityEngine.Debug.LogWarning($"[HeroDeckTransform:{ps.Name}] 牌表为空（原版 `shuffledCardLibrary(+0x28)`）⇒ **不换**");
                return false;
            }
            // ② 原版 `:38-42`：`BoardAnalysis.DeckMeetsCriteria(shuffledCardLibrary, deckWarlord)`
            if (!deckMeetsCriteria.HasValue)
            {
                if (!_heroDeckTransformDataLogged)
                {
                    _heroDeckTransformDataLogged = true;
                    UnityEngine.Debug.Log($"[HeroDeckTransform:{ps.Name}] 「{deckWarlord.Name}」的 `deckCriteria(+0x1f8)` 本机没有这一列"
                          + " ⇒ 判据**查不到**、**不换**（⛔ 不按「满足」猜；数据落地口见方法组头 §二 · 本条只喊一次）");
                }
                return false;
            }
            if (!deckMeetsCriteria.Value)
            {
                UnityEngine.Debug.Log($"[HeroDeckTransform:{ps.Name}] 这副牌不满足「{deckWarlord.Name}」的 deckCriteria ⇒ **不换**"
                      + $"（原版 `DeckMeetsCriteria` 为假；牌表 {deck.Count} 张）");
                return false;
            }
            // ③ 原版 `:43-56`：`fullHeroCollection.Find(hero.uniqueId(+0x38) == transformedWarlordId(+0x200))`
            if (string.IsNullOrEmpty(transformedWarlordId))
            {
                UnityEngine.Debug.Log($"[HeroDeckTransform:{ps.Name}] 「{deckWarlord.Name}」的 `transformedWarlordId(+0x200)` 本机没有这一列"
                      + " ⇒ 不知道该变身成谁、**不换**（数据落地口见方法组头 §二）");
                return false;
            }
            var hero = FindHeroInCollection(ctx, transformedWarlordId);
            if (hero == null)
            {
                UnityEngine.Debug.Log($"[HeroDeckTransform:{ps.Name}] 英雄总表里没有 id =「{transformedWarlordId}」的督军"
                      + "（原版 `fullHeroCollection.Find` 返回 null ⇒ 直接 return，**保留原督军**）");
                return false;
            }
            // ④ 原版 `:57-74`：记录原督军 → `rawBattleData.deckWarlord = 找到的那位`（只换督军这一格）
            originalWarlord = deckWarlord;
            if (!SwapWarlordCard(ctx, seat, hero)) { originalWarlord = null; return false; }
            UnityEngine.Debug.Log($"[HeroDeckTransform:{ps.Name}] 「{deckWarlord.Name}」⇒「{hero.Name}」：牌表满足 deckCriteria"
                  + "且英雄总表里有这一位 ⇒ **督军已换**（原版 `CheckForHeroDeckTransform`；牌库一张未动）"
                  + $"· 原督军 = 「{deckWarlord.Name}」");
            return true;
        }

        /// <summary>
        /// 生产入口（`RuleCore.NewBattle` 的两个调用点用它）：四个输入全部从 `ctx` + 座位现取。
        /// <para>今天两列数据没有 ⇒ <see cref="CampaignHeroTransformTargetId"/> 返回 null、
        /// <see cref="CampaignHeroTransformDeckMeets"/> 返回 null ⇒ **恒返回 false**（休眠，不是不可达）。</para>
        /// </summary>
        public static bool CheckForHeroDeckTransform(BattleContext ctx, int seat,
                                                     CardDef deckWarlord, IReadOnlyList<CardDef> deck)
        {
            return CheckForHeroDeckTransform(ctx, seat, deckWarlord, deck,
                                             CampaignHeroTransformTargetId(ctx, seat),
                                             CampaignHeroTransformDeckMeets(ctx, seat),
                                             out _);
        }

        /// <summary>原版 `DeckAndWarlordData.deckWarlord(+0x18)`：**该座位开局那张督军卡**。
        /// <para>⚠️ 原版这一格是**这副牌自己的**督军（`matchData.playerRawBattleData / enemyRawBattleData`），
        /// ⛔ 不是关卡数据 —— 旧注释把它当成「关卡给的替换卡」是**读错对象**（反证见方法组头 §二）。</para></summary>
        public static CardDef CampaignHeroTransformCard(BattleContext ctx, int seat)
        {
            if (ctx == null || seat < 0 || seat >= ctx.Players.Length) return null;
            var ps = ctx.Players[seat];
            return ps != null && ps.Warlord != null ? ps.Warlord.Card : null;
        }

        /// <summary>原版 `DeckAndWarlordData.shuffledCardLibrary(+0x28)`：**该座位开局的牌库**（CardDef 视图）。
        /// <para>⚠️ 快照口径：原版读的是**开局那份** `DeckAndWarlordData`；本口读的是**此刻**的 `PlayerState.Deck`
        /// ⇒ 只在开局链（任何抽牌之前）调用才逐字等价（生产调用点就是那个位置）。</para></summary>
        public static IReadOnlyList<CardDef> CampaignHeroTransformDeck(BattleContext ctx, int seat)
        {
            if (ctx == null || seat < 0 || seat >= ctx.Players.Length) return null;
            var ps = ctx.Players[seat];
            if (ps == null) return null;
            var list = new List<CardDef>(ps.Deck.Count);
            for (int i = 0; i < ps.Deck.Count; i++)
            {
                var inst = ps.Deck[i];
                list.Add(inst != null ? inst.Card : null);
            }
            return list;
        }

        /// <summary>
        /// 原版 `RawCardScript.transformedWarlordId(+0x200)` 的取值口 —— **要变身成的那位督军的 id**。
        ///
        /// <para>🆕 **2026-11-03（`B121_WC` · 账 `B120-E03b`）：数据管线已通** —— 这一格现在读
        /// <see cref="CardDef.TransformedWarlordId"/>（列由 `工具/gen_cards_engine.py` 的 `transform_of`
        /// 落进 `cards_engine.json`，`CardDatabase.Parse` 挂到 `CardDef` 上）。
        /// 🔴 **今天全池仍是 `null`**，原因在**数据**（原始 SO 1,351 张这一列**全空**）——
        /// 不是「查不到」、更不是「不做事」：数据一出现，本方法**一行不用改**就开始工作
        /// （集中说明见文件末「§变身」那一大段）。</para>
        /// </summary>
        public static string CampaignHeroTransformTargetId(BattleContext ctx, int seat)
        {
            return CampaignHeroTransformTargetId(CampaignHeroTransformCard(ctx, seat));
        }

        /// <summary>同上，**纯函数形态**（自检/探针用：不依赖 `ctx`，只吃那张督军卡）。
        /// <para>`null` / 空 = 这张卡不会让督军变身（= 今天全池那一档）。</para></summary>
        public static string CampaignHeroTransformTargetId(CardDef deckWarlord)
        {
            return deckWarlord != null ? deckWarlord.TransformedWarlordId : null;
        }

        /// <summary>
        /// 原版 `BoardAnalysis.DeckMeetsCriteria(shuffledCardLibrary, deckWarlord)` 的求值结果
        /// （读的是督军身上的 `deckCriteria(+0x1f8)`）。
        ///
        /// <para>🆕 **2026-11-03（`B121_WC` · 账 `B120-E03b`）：数据管线已通** —— 判据逻辑落在下面的纯函数重载里；
        /// 🔴 **今天恒返回 `null`（= 查不到）**，因为原始 SO 的 `deckCriteria` 这一列虽然**每张都有默认值**，
        /// 但按「两列成对落盘」的口径**没有被写进卡表**（没有变身目标的卡，这一格没有意义）。</para>
        /// </summary>
        public static bool? CampaignHeroTransformDeckMeets(BattleContext ctx, int seat)
        {
            return CampaignHeroTransformDeckMeets(CampaignHeroTransformDeck(ctx, seat),
                                                  CampaignHeroTransformCard(ctx, seat));
        }

        /// <summary>
        /// **纯函数形态**的 <see cref="BoardAnalysis.DeckMeetsCriteria"/> 适配器（自检/探针用）。
        ///
        /// <para>**跳序严格照原版**（`BoardAnalysis__DeckMeetsCriteria.c`，101 行逐句）：
        /// ① `deckWarlord == null` ⇒ **查不到**（`null`）；② `deckCriteria == null` ⇒ **查不到**（`null`，
        /// 本机「这一列没落」那一档）；③ `transformedWarlordId` 空 ⇒ **`false`**（`.c:29-33` `return 0`）；
        /// ④ `countCriteria == null` ⇒ **`false`**（`.c:37-39`，⛔ 不是「无条件满足」）；
        /// ⑤ `countCriteria.targetsAffected == 0 (any)` ⇒ **`true`**（`.c:44-46` 直接 `return 1`）；
        /// ⑥ 否则逐张 `FilterMethods.CheckIfRawCardMeetsCriteria` 筛、再按 `triggerIf` / `referenceValue`
        /// 比张数（算法本体 = <see cref="BoardAnalysis.DeckMeetsCriteria"/>，**单一正本**，⛔ 本文件不重写）。</para>
        ///
        /// <para>🔴 **三态**：`null` = **查不到**（没有这一列 / 某一格本版判不了）—— ⛔ 不当作「不满足」，
        /// 也 ⛔ 不当作「满足」（写口会按「不换」处理，并如实出声）。</para>
        /// </summary>
        public static bool? CampaignHeroTransformDeckMeets(IReadOnlyList<CardDef> deck, CardDef deckWarlord)
        {
            if (deckWarlord == null) return null;
            var spec = deckWarlord.DeckCriteria;
            if (spec == null) return null;                                  // ②
            if (string.IsNullOrEmpty(deckWarlord.TransformedWarlordId)) return false;   // ③
            var cc = spec.CountCriteria;
            if (cc == null) return false;                                   // ④
            if (cc.TargetsAffected == 0) return true;                        // ⑤ `TargetsAffected.any`
            if (cc.TargetsAffected == CardDef.CountSetSpec.Unknown) return null;        // 判不了 ⇒ 查不到
            if (spec.TriggerIf == CardDef.CountSetSpec.Unknown
                || spec.ReferenceValue == CardDef.CountSetSpec.Unknown) return null;    // 同上
            if (deck == null) return false;

            // ⑥ 逐张判据：先整体求一遍值 —— 只要有一张「本版判不了」，整条结论就**查不到**（⛔ 不猜）。
            //   ⚠️ 空项直接剔掉（原版 `deck.cardLibrary` 里不可能有 null，`BoardAnalysis.DeckMeetsCriteria`
            //   那边也是 `continue`）⇒ 下面喂给它的 `cards` **没有 null** ⇒ 谓词按序调用、`idx` 恒对齐。
            var cards = new List<CardDef>(deck.Count);
            var oks = new List<bool>(deck.Count);
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i] == null) continue;
                string gap;
                bool? ok = TargetCriteriaEval.MatchesRawCard(deck[i], cc, out gap);
                if (ok == null)
                {
                    UnityEngine.Debug.Log($"[HeroDeckTransform] 「{deckWarlord.Name}」的 `deckCriteria.countCriteria` "
                          + $"第 {i} 张判不了（{gap}）⇒ 本次结论 **查不到**（⛔ 不当成「不满足」/「满足」）");
                    return null;
                }
                cards.Add(deck[i]);
                oks.Add(ok.Value);
            }
            int idx = 0;
            return BoardAnalysis.DeckMeetsCriteria(cards, _ => oks[idx++], spec.TriggerIf, spec.ReferenceValue);
        }

        /// <summary>原版 `PlayerDataManager.fullHeroCollection.Find(hero.uniqueId == id)` 的等价物。
        /// <para>引擎侧没有「玩家持有表」（本项目全解锁）⇒ 用 `ctx.CardPool` 里 `Type == "hero"` 的那些。
        /// `CardPool` 没给 ⇒ 返回 null（调用方会**出声**、⛔ 不拿牌库/卡组顶替英雄总表）。</para></summary>
        static CardDef FindHeroInCollection(BattleContext ctx, string id)
        {
            var pool = ctx != null ? ctx.CardPool : null;
            if (pool == null) return null;
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                if (!string.Equals(c.Type, "hero", System.StringComparison.Ordinal)) continue;
                if (!string.Equals(c.Id, id, System.StringComparison.Ordinal)) continue;
                return c;
            }
            return null;
        }

        /// <summary>
        /// **把某个座位的督军换成另一位**（原版 `rawBattleData.deckWarlord = 找到的那位` 的落地）。
        /// <para>原版这一跳发生在 `SetupPlayerDeckAndInitialTroops → CreatePlayerDeck` **之前**（督军卡还没建）；
        /// 我们的调用点是「两位玩家都建好之后」⇒ 这里**重建一份督军 `UnitState`** 并补回建人期已经落在旧督军身上的三样：
        /// `Exhausted = false`（与 `BuildPlayer` 同句）、`PlayOrder`（次序在换人前已定）、生命修正
        /// （按「旧 `MaxHealthBase` − 旧卡面 `Health`」原样搬；绝对上限覆盖由 `ApplyWarlordLifeOverride` 幂等重落）。</para>
        /// </summary>
        internal static bool SwapWarlordCard(BattleContext ctx, int seat, CardDef hero)
        {
            if (ctx == null || hero == null || seat < 0 || seat >= ctx.Players.Length) return false;
            var ps = ctx.Players[seat];
            var old = ps != null ? ps.Warlord : null;
            if (old == null) return false;
            var oldDef = old.Card;

            var nu = new UnitState(ctx.NewInstance(hero), true);
            nu.Exhausted = false;                     // 与 `BuildPlayer` 同一句（督军不受部署当回合限制）
            nu.PlayOrder = old.PlayOrder;             // 原版督军的场上次序在换人之前已定（`SetUnitOrderInPlay`）
            if (oldDef != null)
            {
                int delta = old.MaxHealthBase - oldDef.Health;   // = 建人期 `Vars.warlordLifeChange`（其他来源见下）
                if (delta != 0)
                {
                    nu.MaxHealthBase = System.Math.Max(1, nu.MaxHealthBase + delta);
                    nu.Health = nu.MaxHealth;
                }
            }
            ps.Warlord = nu;
            ps.Board[BoardSpec.WarlordSlot] = nu;
            ActivateTraitsOnSummonOrEnchantment(nu);  // 与 `BuildPlayer` 同一跳（原版 `SetPlayerDataAndWarlord` 的尾巴）
            // 绝对上限覆盖（`EventAI`/`PracticeOffline`）：幂等（绝对赋值），没有注入时立刻返回。
            ApplyWarlordLifeOverride(ctx);
            // 🆕 2026-11-03（执行写手 `B121_E03F_VFX` · 账 `B121-E03F` · 用户 2026-11-03 裁定）：
            //   **表现层窄口** —— 换人**成功**（换失败的三条闸都在上面各 `return false`，走不到这里）才发一条
            //   `EvtKind.WarlordTransform`，供 `BattleDriver.PlayWarlordTransformFeel` → `TransformPresentation.Play`
            //   播那段 🔶 **自己设计（非原版）**的 0.9 s 三拍演出。
            //   · 为什么放在**引擎**（而不是某个出牌调用点）：变身有**两条**来路 ——
            //     ① 开局 `CheckForHeroDeckTransform`（原版那条）；② `B121-E03F` 的 13 张变身战术卡（自设计）。
            //     两路都过本方法，**在这里发＝不会漏、也不会各写一份**（同 `RuleCore.Draw` 的单口纪律）。
            //   · 纯表现信号：不改局面、不进动作流、**不动 `NetProtocol.Fingerprint`**
            //     （`Fingerprint` / `StateHash` 自 2026-10-31 起都不含 `ctx.Events.Count`），
            //     而且 P2P 两端跑的是同一条确定性引擎路径 ⇒ 两端**都**会发（不是「只在一端发生」的分叉面）。
            //   · 字段约定见 `EvtKind.WarlordTransform` 的 doc：`CardId` = 新督军 · `Effect` = 旧督军。
            ctx.Emit(EvtKind.WarlordTransform, seat, BoardSpec.WarlordSlot, hero.Name,
                     effect: oldDef != null ? oldDef.Name : null,
                     instanceId: (nu.Instance != null) ? nu.Instance.Id : 0);
            return true;
        }

        /// <summary>「两列数据本机没有」只喊一次（`RuleEngineTest` 一次跑几百局 `NewBattle`，⛔ 不要刷屏）。
        /// ⚠️ 它是**诊断节流**、不是状态：数据落列后这条日志自然不再走（金丝雀断言 `B120_W30_Assert` F1/F2 会先红）。</summary>
        static bool _heroDeckTransformDataLogged;

        // ================================================================================================
        //  §变身（督军换将）· **集中注释** —— 账 `战斗逻辑驱动:248`（`BattleManager.CheckForHeroDeckTransform`）
        //  执行代理 `B120_W30`（判据订正）+ `B121_WC`（数据管线 · 账 `B120-E03b`/`E03e`）· 2026-11-03
        //
        //  🔎 **改这条链之前先读这一段**（grep 关键词：变身 / 战将 / 督军 / Transform）——
        //     旧注释把它写成「换整副牌库」，那是**读错语义**，已在 2026-11-03 订正（反证见下）。
        //
        //  ── 一、这个功能是什么 ────────────────────────────────────────────────────────────────────────
        //   **「这副牌的督军变身成另一位督军」**（名字 `HeroDeckTransform` 里那个 Deck = 「这副牌的」，
        //   ⛔ 不是「换一副牌」）。开局装配棋盘时双方各查一次；满足条件就把**当前战将（督军）**换成
        //   另一位已持有的英雄。**牌库一张都不动**。玩家侧再换一套对白。
        //   ⚠️ 与两条同名但不同的事**别混**：
        //     · `ResolveTransformUnit`（场上/手牌**单位**变身，`chosenAbility 80/85/90`）—— 另一条链；
        //     · `Tooltip`/实体版的「战将变身」—— 那是玩法说明，不是这段代码。
        //
        //  ── 二、原版判据链（第一权威，逐句现读 `d:/2/tools/decomp_full/`）─────────────────────────────
        //   调用点：`BattleManager__SetupBoardPhase.c:56`（玩家，`matchData.playerRawBattleData(+0x58)`，
        //           `isPlayer=1`）与 `:66`（敌方，`enemyRawBattleData(+0x50)`，`isPlayer=0`）—— **只有这两处**。
        //   ① 取**这一副牌自己的督军**：`rawBattleData.deckWarlord(+0x18)`（= 这个座位开局那位督军）；
        //   ② `BoardAnalysis.DeckMeetsCriteria(rawBattleData.shuffledCardLibrary(+0x28), deckWarlord)`
        //      —— 拿**这副牌本身的牌表**按督军身上的 `deckCriteria(+0x1F8)` 逐张筛、比张数：
        //        · `transformedWarlordId(+0x200)` 空 ⇒ `return 0`；
        //        · `deckCriteria` 空 / `countCriteria` 空 ⇒ `return 0`；
        //        · `countCriteria.targetsAffected == 0 (any)` ⇒ **`return 1`（无条件即满足）**；
        //        · 否则逐张 `FilterMethods.CheckIfRawCardMeetsCriteria` 后按
        //          `triggerIf(+0x18)`（1 相等 / 2 ≥ / 3 ≤）与 `referenceValue(+0x1c)` 比；
        //   ③ 满足 ⇒ 按 `deckWarlord.transformedWarlordId`（**另一位督军的 id**）到
        //      `PlayerDataManager.fullHeroCollection` 里 `Find(hero.uniqueId(+0x38) == 那个 id)`；
        //   ④ 找到 ⇒ `matchData.originalPlayerWarlord(+0xD8)` / `originalEnemyWarlord(+0xE0)` ← 原督军，
        //      再把 `rawBattleData.deckWarlord` **换成找到的那一位**（🔴 **只换督军这一格，牌库一张不动**），
        //      玩家侧再 `VoiceLinesController.SetTransformedWarlordNewChats`。
        //
        //  ── 三、我们的等价物（都在本文件，⛔ 别在别处再写一份）─────────────────────────────────────
        //   | 原版那一跳 | 我们的落点 |
        //   | `rawBattleData.deckWarlord(+0x18)` | `CampaignHeroTransformCard(ctx, seat)` = `PlayerState.Warlord.Card` |
        //   | `shuffledCardLibrary(+0x28)` | `CampaignHeroTransformDeck(ctx, seat)` = `PlayerState.Deck` 的 CardDef 视图 |
        //   | `deckWarlord.deckCriteria(+0x1F8)` | `CampaignHeroTransformDeckMeets(...)`（三态：`null` = 查不到）→ `CardDef.DeckCriteria` |
        //   | `deckWarlord.transformedWarlordId(+0x200)` | `CampaignHeroTransformTargetId(...)` → `CardDef.TransformedWarlordId` |
        //   | `fullHeroCollection.Find(...)` | `FindHeroInCollection`（`ctx.CardPool` 里 `Type=="hero"` 的；本项目全解锁） |
        //   | `rawBattleData.deckWarlord = 找到的那位` | `SwapWarlordCard`（重建 `UnitState` + 补 `Exhausted/PlayOrder/生命修正`） |
        //   | `matchData.originalPlayerWarlord/originalEnemyWarlord` | **没有等价字段**；写口用 `out originalWarlord` 交回调用方 |
        //   | 整条 | `CheckForHeroDeckTransform(ctx, seat, deckWarlord, deck)`（生产入口，四个输入全从 `ctx` 现取） |
        //   调用点：`RuleCore.NewBattle` 的 `CheckForHeroDeckTransform(ctx, 0/1, …)` 两个（= 原版 `:56/:66`）。
        //   🔴 **W30 订正的要点**：只换**督军这一格**、**不换牌库**；「名称叫 Transform、谓词比的是 id」不矛盾 ——
        //     比的是 `hero.uniqueId == deckWarlord.transformedWarlordId`，**两个不同的 id**。
        //
        //  ── 四、为什么今天休眠（数据事实，⛔ 不是「不可达」/ 不是缺口）─────────────────────────────────
        //   原始 SO 1,351 张（`资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `full_raw` 现扫）：
        //     · `transformedWarlordId` 非空 = **0 / 1,351**；
        //     · `deckCriteria` 全是默认档（`countCriteria.targetsAffected = 0` ⇒ 原版「无条件即满足」那一支）。
        //   ⇒ 生成器按「两列成对落盘」的口径**一个键都不写**（`工具/gen_cards_engine.py` 的 `transform_of`），
        //     `CardDef.TransformedWarlordId` 恒 `null` ⇒ 写口永远停在「没有变身目标」那一支。
        //   🔴 **休眠 ≠ 不可达**（铁律 11）：写口、判据、换人动作、数据管线**都已就位**，只差数据。
        //   ⛔ **不许为了让机制跑起来去编一张变身表** —— 那是卡 SO 上的字段，不是服务器档口可自设计的那一类。
        //
        //  ── 五、以后加 mod / 新功能，从这里接（入口清单）──────────────────────────────────────────────
        //   ① **数据**：`工具/gen_cards_engine.py` 的 `transform_of`（两列唯一的落盘口，成对）→
        //      `Assets/RuleEngine/Resources/cards_engine.json`（`--write` 落盘）→
        //      `CardDatabase.MapDeckCriteria` + `CardDef.AttachTransformData`（载体，已接）。
        //   ② **判据**：`CampaignHeroTransformDeckMeets(IReadOnlyList<CardDef>, CardDef)`（纯函数，
        //      算法本体在 `BoardAnalysis.DeckMeetsCriteria`，⛔ 别在这里重写）；
        //      逐张谓词 = `TargetCriteriaEval.MatchesRawCard`（卡面级，判不了会退回 `null` = 查不到）。
        //   ③ **写口**：`CheckForHeroDeckTransform(...)`；换人动作 `SwapWarlordCard`（**别改成「换牌库」**）。
        //   ④ **生产接线**：`RuleCore.NewBattle` 的两个调用点（原版 `SetupBoardPhase.c:56/66`）。
        //   ⑤ **表现层（还没接）**：原版 `VoiceLinesController.SetTransformedWarlordNewChats` ——
        //      `CardPresentation` 那侧等价物尚未建（账 `B120-E03c`）；要接就在写口返回 `true` 之后那一步。
        //   ⑥ **原督军的去处（还没接）**：`MatchData.originalPlayerWarlord/originalEnemyWarlord` 无等价字段，
        //      今天只用 `out originalWarlord` 交回；要「换人演出 / 战后取原督军」得先给 `BattleContext` 开一格。
        //   ⑦ **自检**：`Editor/RuleEngineTest_B121WC.cs`（E03b 数据管线 · 纯函数两态）＋
        //      `CardPresentation/Editor/B120_W30_Assert.cs`（A~F 六组 · 写口/换人/牌库不动）。
        // ================================================================================================

        // ==================================================================
        //  🆕 2026-11-03（执行写手 `B121_E03F_VFX` · 账 `B121-E03F`）：**自检口**
        // ==================================================================
        /// <summary>自检专用：把 <see cref="SwapWarlordCard"/> 抬成公开口 —— 让「督军变身演出」那条链
        /// 能在**变身卡数据落地之前**就被验（`B121-E03F` 的 13 张变身战术卡属于 `E03F-IMPL`）。
        /// <para>⚠️ 它**只是同一段代码的别名**（⛔ 不在这里另写一份换人逻辑）：换人 + 补 `Exhausted/PlayOrder/生命修正`
        /// 全在 `SwapWarlordCard` 里；本口额外做的只有「事件照发」这一件（同生产路径）。</para>
        /// <para>生产代码**不许**调它 —— 生产入口 = <see cref="CheckForHeroDeckTransform"/>（原版那条）
        /// 与 <see cref="ResolveWarlordTransform"/>（🔶 自设计的 13 张变身战术卡，见文件末那一大段；
        /// 2026-11-03 `B121_E03F_DATA` 就地订正：原来这里写「唯一的生产入口」—— 现在有两个）。</para></summary>
        public static bool SwapWarlordCardForTest(BattleContext ctx, int seat, CardDef hero)
        {
            return SwapWarlordCard(ctx, seat, hero);
        }

        // ==================================================================
        //  🆕 2026-11-03（执行写手 `B121_E03F_DATA` · 账 `B121-E03F` · 用户 2026-11-03 裁定）：
        //    **13 张变身战术卡**（`TRANSFORM_<FACTION>`）的引擎侧 —— 三件东西，各自只有一份实现。
        // ==================================================================
        //  🔶 自己设计（非原版）· 用户 2026-11-03 裁定：
        //    原版**没有**「打出后把督军换成同阵营另一位」这张卡、也没有这条效果 —— 原版变身只发生在
        //    开局装配棋盘**之前**（`CheckForHeroDeckTransform`，见本文件「§变身」那一大段），
        //    换的是 `rawBattleData.deckWarlord` + 对白池，**没有卡牌 / 费用 / 演出**。
        //    ⇒ 本组三项全部自定：卡数据（`工具/gen_cards_engine.py` 的 `TRANSFORM_CARDS`）、
        //      效果码（`WarlordTransformAbilityCode`）、演出（执行写手 `B121_E03F_VFX` 的
        //      `TransformPresentation`，由 `SwapWarlordCard` 里那条 `EvtKind.WarlordTransform` 驱动）。
        //
        //  🧩 分工（⛔ 别在别处再写一份）：
        //    ① <see cref="HasWarlordTransformAbility"/> —— 「这张卡是不是变身卡」的**唯一判据**
        //       （读卡表 `chosenAbility` 列；沙盒 / 探针 / 断言读同一列）。
        //    ② <see cref="WarlordTransformCandidates"/> —— 「能换成谁」的**唯一算法**
        //       （同阵营 ∧ `Type=="hero"` ∧ `Id != 当前督军`；**按 `Id` 序** ⇒ 与池子顺序无关）。
        //    ③ <see cref="ResolveWarlordTransform"/> —— 选人（`ctx.Rng`）+ 换人（`SwapWarlordCard`）。
        //       换人本体**只有 `SwapWarlordCard` 那一份**（⛔ 别复制一段「重建 UnitState」的代码）。

        /// <summary>🔶 自己设计（非原版）· 用户 2026-11-03 裁定：变身战术卡在 `chosenAbility` 列上的**效果码**。
        /// <para>**为什么是 900**：原版 `AbilityEffect` 枚举的取值域是 `0..480`
        /// （末尾 `480 highlightBloodThirst`，判据 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`；
        /// 现池 `chosenAbility` 实际出现 **52 种**、最大 **480**）⇒ **900 落在枚举之外，不会与任何原版分支撞码**。
        /// 消费口只有一处：`EffectResolver.TacticOpsOf`（`CanPlayTactic` / `PlayTacticCore` 都经它取 op）。</para></summary>
        public const int WarlordTransformAbilityCode = 900;

        /// <summary>这张卡是不是 🔶 自设计的变身战术卡 —— **唯一判据**（`chosenAbility` 列含 <see cref="WarlordTransformAbilityCode"/>）。
        /// <para>`null`（列未知 / 旧表 / 手写夹具）⇒ `false`（⛔ 不猜）。</para></summary>
        public static bool HasWarlordTransformAbility(CardDef card)
        {
            if (card == null) return false;
            var cas = card.ChosenAbilities;
            if (cas == null) return false;
            for (int i = 0; i < cas.Length; i++)
                if (cas[i] == WarlordTransformAbilityCode) return true;
            return false;
        }

        /// <summary>战术 / 防御卡**这一把要结算的 op 流** —— 唯一取值口。
        /// <para>普通卡 = `EffectText.Parse(card.Desc)`（逐字等于改前那两处各自写的那一句）；
        /// 变身卡 = **一条** <c>warlordtransform</c> op（卡面正文是**说明文字**、不是可解析的效果句，
        /// 所以 ⛔ 不能让解析器去猜）。</para>
        /// <para>⚠️ `CanPlayTactic` 与 `PlayTacticCore` **必须都从这里取** —— 两处各解析一次的话，
        /// 「要不要选目标」与「实际结算什么」会分叉（拖拽高亮说能选、打出去却空过）。</para></summary>
        public static List<EffectOp> TacticOpsOf(CardDef card)
        {
            if (card == null) return null;
            if (HasWarlordTransformAbility(card))
                return new List<EffectOp> { new EffectOp { Verb = "warlordtransform", Source = card.Name } };
            return EffectText.Parse(card.Desc, out _, out _, card: card);
        }

        /// <summary>「这一方能换成谁」—— **唯一算法**：同阵营的督军卡、且不是当前这位。
        /// <para>口径（🔶 自设计，用户 2026-11-03 裁定「随机变同阵营、非当前的另一位督军」）：
        /// 扫 `ctx.CardPool` 的 `Type == "hero"`；**按 `Id` 序号排序** ⇒ 与池子遍历顺序无关（可复现的前提）；
        /// 同一 `Id` 只留一份（池里理论上无重复，重复时按第一份，⛔ 不去重后改变随机分布）。</para>
        /// <para>`ctx` / `CardPool` 为空 ⇒ **空表**（调用方会出声）。`null` 与空表在这里同义 ——
        /// 因为「能换成谁」的答案确实是「一个都没有」，不是「判不了」。</para></summary>
        public static List<CardDef> WarlordTransformCandidates(BattleContext ctx, int seat)
        {
            var list = new List<CardDef>();
            if (ctx == null || ctx.Players == null || seat < 0 || seat >= ctx.Players.Length) return list;
            var ps = ctx.Players[seat];
            var cur = ps != null && ps.Warlord != null ? ps.Warlord.Card : null;
            var pool = ctx.CardPool;
            if (cur == null || pool == null) return list;
            var seen = new HashSet<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                if (!string.Equals(c.Type, "hero", System.StringComparison.Ordinal)) continue;
                if (!string.Equals(c.Faction, cur.Faction, System.StringComparison.Ordinal)) continue;
                if (string.Equals(c.Id, cur.Id, System.StringComparison.Ordinal)) continue;
                if (!seen.Add(c.Id)) continue;
                list.Add(c);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        /// <summary>变身战术卡的**结算本体**：选人（≥2 个时按 `ctx.Rng` 随机）→ <see cref="SwapWarlordCard"/>。
        /// <para>三档（🔶 自设计 · 用户 2026-11-03 裁定「候选 ≥2 随机、0 个空放并出声、1 个直接换」）：</para>
        /// <list type="bullet">
        /// <item>0 个候选 ⇒ `false` + `failure` 文案 + `Debug.LogWarning`（**不崩**，卡照常进弃牌堆）；</item>
        /// <item>1 个候选 ⇒ **不掷骰**（掷了会无谓推进随机流、让后续判定漂移）；</item>
        /// <item>≥2 个候选 ⇒ `ctx.Rng.Next(n)`（`System.Random`；同一个 seed ⇒ 同一个结果）。</item>
        /// </list>
        /// <para>⚠️ 随机源用 `ctx.Rng`（种子化）而**不是** `UnityEngine.Random` —— 工程铁律「一局必须可复现」。
        /// ⛔ 也**不用** `ctx.GetRandomInt`：那是原版那 100 格预烧表的消费口，这张卡原版不存在，
        /// 借它会改变已有卡的随机序列。</para></summary>
        public static bool ResolveWarlordTransform(BattleContext ctx, int seat, out CardDef picked,
                                                   out int candidateCount, out string failure)
        {
            picked = null; candidateCount = 0; failure = null;
            if (ctx == null) { failure = "没有对局上下文"; return false; }
            if (ctx.Players == null || seat < 0 || seat >= ctx.Players.Length)
            { failure = $"座位越界（{seat}）"; return false; }
            var ps = ctx.Players[seat];
            var cur = ps != null && ps.Warlord != null ? ps.Warlord.Card : null;
            if (cur == null) { failure = "这一方没有督军"; return false; }

            var cands = WarlordTransformCandidates(ctx, seat);
            candidateCount = cands.Count;
            if (candidateCount == 0)
            {
                failure = $"「{cur.Name}」（{cur.Faction}）在卡池里没有另一位同阵营督军";
                UnityEngine.Debug.LogWarning($"[变身战术卡] 座位 {seat}：{failure} ⇒ **空放**（不崩、不变身）");
                return false;
            }
            picked = candidateCount == 1 ? cands[0] : cands[ctx.Rng.Next(candidateCount)];
            if (!SwapWarlordCard(ctx, seat, picked))
            {
                failure = $"换人动作自身失败（「{cur.Name}」⇒「{picked.Name}」）";
                picked = null;
                UnityEngine.Debug.LogWarning($"[变身战术卡] 座位 {seat}：{failure}");
                return false;
            }
            UnityEngine.Debug.Log($"[变身战术卡] 座位 {seat}：「{cur.Name}」⇒「{picked.Name}」"
                  + $"（候选 {candidateCount} 位{(candidateCount > 1 ? "，按 ctx.Rng 随机选中" : "，唯一候选")}）");
            return true;
        }
    }
}
