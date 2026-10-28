// RuleCorePartial_B94B8.cs — 第九十四会话 · 波 2 · 执行代理 `B94_B8` 的 `RuleCore` partial 扩展
//
// 🔴 为什么单开文件：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 365+ 处，
//   往它中间插成员会让下游落点整体下移 ⇒ 新成员一律落本文件（与 `RuleCorePartial_B88E14.cs`
//   / `RuleCorePartial_B93F7.cs` 同形）。
//
// 本批（B94_B8）落在本文件的东西（战斗域「无落点」两条）：
//   · `BroadcastQuestResolved(ctx, owner, questCard)` —— 账 `战斗逻辑驱动:212`
//     （原版 `BattleManager__BroadcastQuestResolved.c` 全文逐句照抄）。**休眠**：生产端
//     `spellType == 120` 池内 0 张 ⇒ 今天零可达（铁律 11：照样建，⛔ 不判 D）。
//   · `ThisCardPlayedDamageEntries` / `ThisCardPlayedDamage` —— 账 `战斗逻辑驱动:1037`
//     （原版 `EntityScript__GetThisCardPlayedDamage.c:1-106`）。
//     本工程**没有**「按 ability 的 chosenAbility + damageCriteria.minDamage 取数」的非主动能力模型
//     （`CardDef.ActiveAbilityLogicSpec` 只覆盖 activeAbility）⇒ 这里给的是**结构等价物**：
//     按 `u.FxOps(keyword)` 里「打在玩家点的那个敌人身上」的 `deal` op 逐条求和
//     （与 `RuleCore.ActiveAbilityDamageEntries` 同一套 `AddDealEntries` / `DealsToPickedTarget`）。
//
// ⚠️ 判据出处写在方法头上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  账 212 `BattleManager.BroadcastQuestResolved(CardScript questCard)`
        // ==================================================================
        //
        // **第一权威**（本笔逐行现读 2026-10-29）：`d:/2/tools/decomp_full/BattleManager__BroadcastQuestResolved.c`
        // （全文 60 行，逻辑只有两跳）：
        //   ① 扫 `unitsInPlay`（`manager + 0x470`）逐张：`CardScript__IsInPlay` 为真 ⇒
        //      `CardScript__QuestResolved(card, questCard)`；
        //   ② `BattleManager__GetCardsInHandList(bm, 1,1,1,1,1,1, 0)`（**六个 true**：双方手牌 · 全卡种）
        //      逐张：`!= questCard` ⇒ 同上。
        // `QuestResolved`（`CardScript__QuestResolved.c`）= `RawCardScript__OnQuestResolved(raw, mgr, this, questCard, isEnchantment=0)`
        // = `RawCardScript__OnTrigger(0x1EA = 490 QuestTriggered, mgr, thisCard, questCard)`（`RawCardScript__OnQuestResolved.c:3`）。
        // ⇒ 语义 = **让场上每一张在-play 的卡、双方手牌每一张（除 questCard 自己）各跑一次它自己的 490 能力**。
        //
        // **唯一生产端**：`CardScript__RemoveEffect.c:240-247` —— `buffType ∈ {0xC, 0xF, 0x10}`
        //   ∧ `cardType(+0x94) == 0x28 (40 Whispers)` ∧ `spellType(+0x90) == 0x78 (120)` ∧ `!fromJam`
        //   ⇒ `BroadcastQuestResolved` + `questCounter(+0x270) = 0`。
        // **池内可达性**：`spellType == 120` 池内 **0 张**（`cards_truth.json` 全表 spellType 值域无 120）
        //   ⇒ **休眠**（⛔ 不是「不做」/「判 D」）。
        //
        // ⚠️ **为什么先判「有没有这条 490 正文」再触发**：原版 `OnTrigger(490)` 对没有 490 能力的卡
        //   **什么都不做**；而我们的 `FireTriggerAlways` 在「没正文」时会发一条 `EvtKind.Trigger` 事件
        //   + 广播监听器（那是给「没有正文但有系统触发的词」用的）。若对场上每张卡都无条件调它，
        //   会给一堆本来无 490 能力的卡**凭空发事件**（污染 `play all dump` 指纹）。
        //   ⇒ 只对「确有 490 正文（卡上原生 / 挂上去的 / 附魔挂的）」的卡触发，与原版等价。
        //
        // ⚠️ **手牌那一半的等价物**：手牌 `CardInstance` 没有 `UnitState`，走
        //   `EffectResolver.RunAbilityOfAnotherCard(ctx, inst.Card, "QuestTriggered", …)`（它读卡自己的
        //   `TriggerOps("questtriggered")` 并结算，正是「让这张卡跑它自己的 490 能力」）。
        //   ⚠️ 原版把 `questCard` 当 `targetCard` 传进 `OnTrigger`；我们的 `RunAbilityOfAnotherCard` 的
        //   `targetCard` 形参是 `UnitState`，而 questCard 是手牌/附魔 `CardInstance`（不是棋盘单位）
        //   ⇒ 这里**传 null**（如实标注；休眠，今天不可观测）。
        /// <summary>
        /// **「任务已解决」的全场广播** —— 原版 `BattleManager__BroadcastQuestResolved.c` 全文（见上）。
        /// 让场上每一张 in-play 的卡、双方手牌每一张（除 <paramref name="questCard"/> 自己）各跑一次它的 490 能力。
        /// </summary>
        /// <param name="ctx">对局上下文。</param>
        /// <param name="owner">施放者（任务卡所属玩家号 0/1；只为日志/归属，扫的是**全场**）。</param>
        /// <param name="questCard">被解决的那张任务卡（手牌侧要排除它自己）。</param>
        public static void BroadcastQuestResolved(BattleContext ctx, int owner, CardInstance questCard)
        {
            if (ctx == null) return;
            // ---- ① 场上（原版 unitsInPlay +0x470，先过 CardScript.IsInPlay）—— 两边都扫 ----
            for (int side = 0; side < 2; side++)
            {
                var ps = ctx.Players[side];
                if (ps == null) continue;
                for (int slot = 0; slot < BoardSpec.Size; slot++)
                {
                    var u = ps.Board[slot];
                    // 原版闸 `CardScript__IsInPlay()`（cardState ∈ {2,15,3,17}）的等价物 =
                    //   `!u.IsAlive` 挡掉「待死/已死」那一档（与 `RuleCore.cs:1450` 同一个代理口径）。
                    if (u == null || !u.IsAlive) continue;
                    if (!HasQuestResolvedBody(u)) continue;   // 没有 490 正文 ⇒ 原版什么都不做
                    FireTriggerAlways(ctx, u, "questtriggered", side, slot);
                }
            }
            // ---- ② 双方手牌（原版 GetCardsInHandList(1,1,1,1,1,1,0)）—— 排除 questCard 自己 ----
            for (int side = 0; side < 2; side++)
            {
                var ps = ctx.Players[side];
                if (ps == null) continue;
                var hand = ps.Hand;
                for (int i = 0; i < hand.Count; i++)
                {
                    var inst = hand[i];
                    if (inst == null || inst.Card == null) continue;
                    if (ReferenceEquals(inst, questCard)) continue;   // 原版 `op_Inequality(card, questCard)`
                    var ops = inst.Card.TriggerOps("questtriggered");
                    if (ops == null || ops.Count == 0) continue;      // 没 490 正文 ⇒ 原版什么都不做
                    // ⚠️ `RunAbilityOfAnotherCard` 住在 `EffectResolver.cs`，但那个文件里是**同一个
                    //    `public static partial class RuleCore`**（⇒ 直接调，⛔ 别写 `EffectResolver.`）。
                    RunAbilityOfAnotherCard(ctx, inst.Card, "QuestTriggered", side,
                                            null, null, null, inst.Card.Name);
                }
            }
        }

        /// <summary>这张在场单位有没有「任务已解决(490)」的可结算正文 —— 卡上原生 / 挂在单位上的 / 附魔挂的。
        /// （用于让 `BroadcastQuestResolved` 与原版 `OnTrigger(490)` 的「没能力就什么都不做」等价。）</summary>
        static bool HasQuestResolvedBody(UnitState u)
        {
            if (u == null) return false;
            if (u.HasFx("questtriggered")) return true;
            if (Enchantments.HasAny(u))
            {
                var m = Enchantments.Matching(u, "questtriggered");
                if (m != null && m.Count > 0) return true;
            }
            return false;
        }

        // ==================================================================
        //  账 1037 `EntityScript.GetThisCardPlayedDamage(CardScript targetCard)`
        // ==================================================================
        //
        // **第一权威**（本笔逐行现读 2026-10-29）：`d:/2/tools/decomp_full/EntityScript__GetThisCardPlayedDamage.c:1-106`：
        //   取 `GetAbilitiesForTriggerRawcard(ThisCardPlayed(0))`；每个 ability 先
        //   `CheckIfMeetsCriteria(this, target, target, ability.targetCriteria)`，再对该 ability 的
        //   `abilityLogic(+0x50)` 逐条：`chosenAbility(+0x10) ∈ {10 doDamage, 12 doDamageMultiRandom,
        //   15 doDamageAndDestroySelf}` ⇒ 累加 `damageCriteria(+0x40).minDamage(+0x14)`。
        //   🔴 **原版只算这三档、漏 `13 doDamageFast`**（`.c:80-93`）—— 照抄、⛔ 别补齐。
        //
        // ⚠️ **本工程的结构差异（如实标）**：我们没有「非主动能力的 `chosenAbility` + `damageCriteria`」
        //   模型 —— `CardDef.ActiveAbilityLogicSpec`（带 `ChosenAbility[]` / `DamageCriteria`）**只覆盖
        //   `activeAbility`**；触发能力（`cardAbilities`）只有**文本解析出来的 `EffectOp`**。
        //   ⇒ 这里给的是**结构等价物**（与同文件 `ActiveAbilityDamageEntries` 同一套辅助函数）：
        //     `u.FxOps(keyword)` 里每条 `deal`、且判据 = `DealsToPickedTarget`（打在「玩家点的那个敌人」上）
        //     ⇒ 逐条收 Amount。与 `ActiveAbilityDamageEntries` 的取舍**一致**（见其 doc 的「结构等价物」节）。
        //   ⚠️ `keyword` 由调用方给（`rally` / `strike` …，= 卡面 ThisCardPlayed(0) 那一族的正文键）。
        //
        // **可达性**：原版可达 183 张（账 B92_B7）；**我们侧结构上不可达** —— `CardBoardHighlight.PreviewProbe`
        //   只挂在棋盘卡（`BattleDriver.BoardHighlightOf`），`BattleBoardPreview` 的 `other` 恒为棋盘单位
        //   ⇒ `OtherIsTactic` 恒假（账 B93_F3）。⇒ 本口是**给将来接线用的取数口**，今天零行为变化。

        /// <summary>
        /// **「这张牌被打出时打出的伤害」的逐条取数** —— 原版 `EntityScript__GetThisCardPlayedDamage.c`
        /// 的结构等价物（判据与差异见方法组头注释）。
        /// </summary>
        /// <param name="keyword">ThisCardPlayed(0) 那一族的正文键（`rally` / `strike` / …）。</param>
        /// <returns>逐条伤害（不含护甲/减伤 —— 与 `AttackDamageEntries` 同口径）。空表 = 没有这一族伤害。</returns>
        public static List<int> ThisCardPlayedDamageEntries(BattleContext ctx, int p, int slot, string keyword)
        {
            var list = new List<int>();
            if (ctx == null || string.IsNullOrEmpty(keyword)) return list;
            var u = ctx.Players[p].Board[slot];
            if (u == null) return list;
            AddDealEntries(u.FxOps(keyword), list);   // 复用 ActiveAbilityDamageEntries 那套「只收打在点选目标上的 deal」
            return list;
        }

        /// <summary>`GetThisCardPlayedDamage` 原样的返回形态：把所有条目求和成一个数（原版返回 `int`）。</summary>
        public static int ThisCardPlayedDamage(BattleContext ctx, int p, int slot, string keyword)
        {
            var list = ThisCardPlayedDamageEntries(ctx, p, slot, keyword);
            int sum = 0;
            for (int i = 0; i < list.Count; i++) sum += list[i];
            return sum;
        }
    }
}
