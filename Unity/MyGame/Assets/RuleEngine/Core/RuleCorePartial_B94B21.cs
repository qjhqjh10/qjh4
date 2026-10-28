// RuleCorePartial_B94B21.cs — 第九十四会话 · 波 6 · 执行代理 `B94_B21`
//
// 🔴 为什么单开文件：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 365+ 处，
//   往它中间插成员会让下游落点整体下移 ⇒ 新成员一律落本文件（与 `RuleCorePartial_B88E14.cs`
//   / `RuleCorePartial_B93F7.cs` / `RuleCorePartial_B94B8.cs` 同形）。
//
// 本批（B94_B21）只做 **战斗域「休眠」那一档**（生产端池内 0 张 ⇒ 铁律 11：机制照样建，
// ⛔ **绝不判 D**；时间限定 = 本 build / 2025-07-30 资源快照 / 安卓 1.35.0）。两条机制本体：
//
//   · `BroadcastTurnSetup(ctx)`                    —— 账 `战斗逻辑驱动:642`
//     （原版 `BattleManagerSupport__BroadcastTurnSetup.c` 全文逐句照抄，三跳）。
//     休眠证据：`TurnSetup(155)` 池内 **0/1351**（`cards_engine.json` 的 `abilityTriggers` 全卡 walk = 0/1129）。
//   · `BroadcastSupportOnSummon(ctx, side, unit)`  —— 账 `战斗逻辑驱动:876` 的 **550 `Support`** 那一跳
//     （原版 `CardScript__ResolveUnitSummoned.c:46-60` 逐句）。
//     休眠证据：`Support(550)` 池内 **0 张**。
//
// ⚠️ 判据出处写在每个方法头上（铁律 2/3：没有出处就写「查不到」；⛔ 不凭印象）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>原版 `AbilityTrigger.TurnSetup` 的枚举名（`CardDef.AbilityTriggerNumberOfName` 里 `{"TurnSetup", 155}`）。</summary>
        /// <remarks>⛔ 别改写成小写关键词：`_triggerOps` 的键虽然是小写规范名，但 `Turn Setup:` / `At the start of
        /// the turn` 这一族**我们根本没有解析出口**（`EffectText.SplitAtTurn` 只产出 `turn_start` / `turn_end`），
        /// 本触发今天只能靠 `abilityTriggers` 数据 + 事件层表达。传**枚举名**与同族的
        /// `FireHandAbilityTrigger(… "TurnStart" …)` / `"TurnStarted"` 一个口径（那两条走的是同一个约定）。</remarks>
        public const string TurnSetupTriggerName = "TurnSetup";

        /// <summary>原版 `AbilityTrigger.Support` 的枚举名（`CardDef.cs` 名字表 `{"Support", 550}`）。</summary>
        public const string SupportTriggerName = "Support";

        // ==========================================================================================
        //  账 642 `BattleManagerSupport.BroadcastTurnSetup(BattleManager manager, bool isPlayerTurn)`
        // ==========================================================================================
        //
        // **第一权威**（本笔逐行现读 2026-10-29）：`d:/2/tools/decomp_full/BattleManagerSupport__BroadcastTurnSetup.c`
        // （全文 90 行，逻辑只有三跳，**没有**任何 `op_Inequality` 预调）：
        //   ① `:34-51` 棋盘：`manager + 0x470`（`unitsInPlay`，**一个跨双方的单一列表**）逐张，
        //      先过 `CardScript__IsInPlay()` ⇒ `CardScript__OnTurnSetup(card, playerTurn, isInHand: 0, …)`；
        //   ② `:52-64` `BattleManager__GetCardsInHandRef(bm, IsPlayerTurn())`（**当前行动方**手牌）逐张
        //      ⇒ `CardScript__OnTurnSetup(card, playerTurn, isInHand: **1**, …)`；
        //   ③ `:65-78` `GetCardsInHandRef(bm, !IsPlayerTurn())`（**对手**手牌）逐张 ⇒ 同上（`isInHand: 1`）。
        //
        // **接收端**：`CardScript__OnTurnSetup.c:1-172` —— 遍历 `activeEffects(+0x108)` 按 `+0x33/+0x34/+0x35`
        //   三档到期旗标逐条 `SendRemoveEffect`、重置 `+0x32`，末尾 `OnTrigger(0x9b = 155 TurnSetup)`。
        //   ⇒ 「到期撤 buff」那一半**我们已经有了**（`UnitState.TempBuff` 的三档 `due` + `RuleCore`
        //   的相位扫场，判据见账 `816`），本文件负责的是**缺的那一半 = 155 那一跳本身**。
        //
        // ⚠️ **与原版的已知差别（如实标，⛔ 不粉饰）**：
        //   · 原版 `OnTurnSetup` 对**每一张**在场卡都调（155 有没有正文由 `OnTrigger` 内部自己判空转）；
        //     我们只在「这张卡**真有** 155 能力」（`AbilityTriggerPresence == 1`，或附魔挂的那份有）
        //     时才调 —— 因为我们的 `FireTriggerAlways` 对「没正文」的卡会发一条 `EvtKind.Trigger`
        //     （那是给「没有正文但有系统触发的词」用的），无条件扫全场会给一堆本来无 155 能力的卡
        //     **凭空发事件**、污染 `play all dump` 指纹。⇒ 加这道筛子 = 与原版「空转」等价。
        //     （同一套取舍的既有先例 = `RuleCorePartial_B94B8.cs` 的 `HasQuestResolvedBody`。）
        //   · 原版那个 `playerTurn` 实参在我们的触发通路上**没有对应槽位**（`FireTriggerAt` 的形参表里没有它）⇒
        //     本口不收这个参数（同 `BroadcastTurnStartedHandHops(ctx)` 的先例）。⚠️ 今天 155 池内 0 张，
        //     这条差别**零可观测**；数据一变就要回来补一个形参。
        //
        // **池内可达性**：`TurnSetup(155)` = `cards_engine.json` 的 `abilityTriggers` 全卡 walk **0/1129**、
        //   SO 全量 **0/1351**（`B91_L4` 复核）⇒ **休眠**。⛔ **不判 D**（数据一变就可达）。

        /// <summary>
        /// **回合准备（setup）相位的三跳广播** —— 原版 `BattleManagerSupport__BroadcastTurnSetup.c` 全文（见上）。
        /// 让场上每一张 in-play 的卡、双方手牌每一张各跑一次它的 155 能力。
        /// </summary>
        /// <returns>真正发出/结算的次数（棋盘那一跳 + 双手两跳之和）。</returns>
        public static int BroadcastTurnSetup(BattleContext ctx)
        {
            if (ctx == null) return 0;
            int n = 0;

            // ---- ① 棋盘（原版 unitsInPlay +0x470，先过 CardScript.IsInPlay）—— 两边都扫 ----
            for (int side = 0; side < ctx.Players.Length; side++)
            {
                var ps = ctx.Players[side];
                if (ps == null || ps.Board == null) continue;
                for (int slot = 0; slot < BoardSpec.Size; slot++)
                {
                    var u = ps.Board[slot];
                    // 原版 `CardScript__IsInPlay()`（cardState ∈ {2,15,3,17}）的等价物 =
                    //   `u.IsAlive` 挡掉「待死/已死」那一档（与 `RuleCore.cs:1450` 同一个代理口径）。
                    if (u == null || !u.IsAlive) continue;
                    if (!HasTurnSetupBody(u)) continue;      // 没有 155 能力 ⇒ 原版那一跳空转（见上「已知差别」）
                    FireTriggerAlways(ctx, u, TurnSetupTriggerName, side, slot);
                    n++;
                }
            }

            // ---- ② ③ 双方手牌两跳（原版 GetCardsInHandRef(IsPlayerTurn) → GetCardsInHandRef(!IsPlayerTurn)）----
            // ⚠️ `HandHops` 住在 `EffectResolver.cs`，但那个文件里是**同一个 `public static partial class RuleCore`**
            //    （⇒ 直接调，⛔ 别写 `EffectResolver.`）。它逐张走 `FireHandAbilityTrigger`：里面已经有
            //    「`AbilityTriggerPresence == 1`」+「`abilityContext == 10 Hand`」+「有正文」三道闸，
            //    与原版 `OnTrigger` 内部那套 `CanTriggerAbility` 同形 ⇒ 不需要在这儿再筛一遍。
            n += HandHops(ctx, TurnSetupTriggerName, "turn_setup:155");
            return n;
        }

        /// <summary>这张在场单位有没有「回合准备(155)」这条能力 —— 卡上原生 / 附魔卡挂的。
        /// （用途：让 <see cref="BroadcastTurnSetup"/> 与原版 `OnTrigger(155)` 的「没能力就空转」等价。）</summary>
        static bool HasTurnSetupBody(UnitState u)
        {
            if (u == null) return false;
            if (u.Card != null && u.Card.AbilityTriggerPresence(TurnSetupTriggerName) == 1) return true;
            if (u.HasFx(TurnSetupTriggerName)) return true;
            if (Enchantments.HasAny(u))
            {
                var m = Enchantments.Matching(u, TurnSetupTriggerName);
                if (m != null && m.Count > 0) return true;
            }
            return false;
        }

        // ==========================================================================================
        //  账 876 `CardScript.ResolveUnitSummoned` 的第 ③ 跳 = **550 `Support`**
        // ==========================================================================================
        //
        // **第一权威**（本笔逐行现读 2026-10-29）：`d:/2/tools/decomp_full/CardScript__ResolveUnitSummoned.c:46-60`：
        //   ```
        //   if (op_Inequality(cardPlayed, this)) {                       // 「不是我」
        //     if (cardPlayed != null && cardPlayed.rawCardData != null) {
        //       if (RawCardScript__IsUnit(cardPlayed.rawCardData)) {      // 刚落地那张是**部队**
        //         if (cardPlayed.isPlayer(+0x40) == this.isPlayer(+0x40))  // **同方**
        //           RawCardScript__OnTrigger(raw, 0x226 = 550 Support, this, actingCard=cardPlayed, targetCard=param_3);
        //       }
        //     }
        //   }
        //   ```
        //   注：`:20-28` = 190、`:30-45` = 195/605、`:62-105` = 680 都已经在本仓**别处**接好（账 876 的差异列），
        //   本口只补 **550** 这一跳（账 876 残余 ①）。
        //
        // **接收者是谁**：原版 `ResolveUnitSummoned` 是**逐卡**调的（`BroadcastUnitSummoned` 遍历 `unitsInPlay`
        //   每张各调一次）⇒ 550 的接收者 = **同方每一张在场卡**（含督军），排除「刚落地那张自己」。
        //
        // **池内可达性**：`Support(550)` 池内 **0 张**（`cards_engine.json` `abilityTriggers` 全卡 walk）⇒ **休眠**。
        //   ⚠️ 别拿「卡面有 `Support:` 这个词」当可达 —— 那一族走的是**关键词**（`KeywordTable` 的 `support`），
        //   与 ability trigger 550 是**两个不同的轴**（同 `regiment` 301 vs `OtherCardRegiment` 646 那一组）。

        /// <summary>
        /// **「有部队被召唤」时给同方其它卡发 550 `Support`** —— 原版
        /// `CardScript__ResolveUnitSummoned.c:46-60` 的逐句等价物（判据见上）。
        /// </summary>
        /// <param name="ctx">对局上下文。</param>
        /// <param name="side">刚落地那张牌的主人（原版 `cardPlayed.isPlayer`）。</param>
        /// <param name="summoned">刚落地的那张（原版 `cardPlayed`；它的 `UnitState` 本身就意味着「是部队」）。</param>
        /// <returns>真正触发到的接收者张数。</returns>
        public static int BroadcastSupportOnSummon(BattleContext ctx, int side, UnitState summoned)
        {
            if (ctx == null || summoned == null) return 0;
            if (ctx.Players == null || side < 0 || side >= ctx.Players.Length) return 0;
            var ps = ctx.Players[side];
            if (ps == null || ps.Board == null) return 0;

            // 快照再触发：结算会改棋盘（能再部署、能打死人）—— 与 `FireTriggerOnSide` 同一条教训。
            var found = new List<UnitState>();
            for (int slot = 0; slot < BoardSpec.Size; slot++)
            {
                var t = ps.Board[slot];
                if (t == null || !t.IsAlive || t.Card == null) continue;
                if (ReferenceEquals(t, summoned)) continue;                 // 原版 `op_Inequality(cardPlayed, this)`
                if (!HasSupportBody(t)) continue;                            // 没有 550 ⇒ 原版 `OnTrigger` 空转
                found.Add(t);
            }
            // 原版遍历 `unitsInPlay`（**进场序**）⇒ 与 `FireTriggerOnSide` 同一个排序口径（⛔ 别按格号）
            StableSortByPlayOrder(found, u => u.PlayOrder);

            int n = 0;
            foreach (var t in found)
            {
                if (t == null || !t.IsAlive) continue;                      // 快照之后可能已被前一个打死
                int o, s;
                if (!FindUnit(ctx, t, out o, out s) || o != side) continue; // 当前格号（触发可能挪过人）
                // 🔴 三槽：`thisCard = 接收者` / `actingCard = 刚落地那张` / `targetCard = param_3`。
                //    我们的 `FireTriggerAt(ctx, u, kw, owner, slot, chosen, subject)` 里
                //    `subject` = 「这件事发生在谁身上」= 原版 `actingCard` 那一槽（判据见该方法的 doc 与 `A1574`）。
                FireTriggerAlways(ctx, t, SupportTriggerName, side, s, summoned);
                n++;
            }
            return n;
        }

        /// <summary>这张在场单位有没有「他卡被召唤(550 Support)」这条能力 —— 卡上原生 / 附魔卡挂的。
        /// （用途：让 <see cref="BroadcastSupportOnSummon"/> 与原版 `OnTrigger(550)` 的「没能力就空转」等价。）</summary>
        static bool HasSupportBody(UnitState u)
        {
            if (u == null || u.Card == null) return false;
            if (u.Card.AbilityTriggerPresence(SupportTriggerName) == 1) return true;
            if (u.HasFx(SupportTriggerName)) return true;
            if (Enchantments.HasAny(u))
            {
                var m = Enchantments.Matching(u, SupportTriggerName);
                if (m != null && m.Count > 0) return true;
            }
            return false;
        }
    }
}
