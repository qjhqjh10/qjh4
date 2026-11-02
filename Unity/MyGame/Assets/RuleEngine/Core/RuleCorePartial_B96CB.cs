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
        /// <para>🔴 **本机没有这一列 ⇒ 恒 `null`**（判据：`cards_engine.json` / `CardDef` 都没有它，生成器不输出；
        /// 原始 SO 的 `cards_raw.json` `full_raw` 现扫 1,351 张**非空 = 0**）。
        /// **这是「查不到」不是「不做事」** —— 数据落地那天**只改这一个方法**：
        /// 例如从 `CardDatabase` 把 SO 的 `transformedWarlordId` 挂到 `CardDef` 上，这里 `return ctx.Players[seat].Warlord.Card.TransformedWarlordId;`。</para>
        /// </summary>
        public static string CampaignHeroTransformTargetId(BattleContext ctx, int seat)
        {
            return null;
        }

        /// <summary>
        /// 原版 `BoardAnalysis.DeckMeetsCriteria(shuffledCardLibrary, deckWarlord)` 的求值结果
        /// （读的是督军身上的 `deckCriteria(+0x1f8)`）。
        ///
        /// <para>🔴 **本机没有 `deckCriteria` 这一列 ⇒ 恒 `null`（三态：查不到，⛔ 不是「不满足」）**。
        /// 数据落地后的写法：把 `countCriteria` 翻成我们的逐张谓词（`TargetCriteriaEval` / `CardCriteria` 那一路），
        /// 再调 <see cref="BoardAnalysis.DeckMeetsCriteria"/>（单一正本在 `BoardAnalysis_B94B8.cs:45`，⛔ 别在这重写）
        /// —— `targetsAffected == 0` 时按原版早退传 `meets: null`（= 无条件即满足）。</para>
        /// </summary>
        public static bool? CampaignHeroTransformDeckMeets(BattleContext ctx, int seat)
        {
            return null;
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
        static bool SwapWarlordCard(BattleContext ctx, int seat, CardDef hero)
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
            return true;
        }

        /// <summary>「两列数据本机没有」只喊一次（`RuleEngineTest` 一次跑几百局 `NewBattle`，⛔ 不要刷屏）。
        /// ⚠️ 它是**诊断节流**、不是状态：数据落列后这条日志自然不再走（金丝雀断言 `B120_W30_Assert` F1/F2 会先红）。</summary>
        static bool _heroDeckTransformDataLogged;
    }
}
