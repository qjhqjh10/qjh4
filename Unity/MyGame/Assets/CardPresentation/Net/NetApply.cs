// NetApply.cs — 把**线上那条动作**落到引擎上的**唯一一份实现**
//
// 为什么单独一个文件：这段要被两处用 ——
//   ① `BattleDriver.ApplyLoggedAction`（真对局：落地之后要 `RefreshAll` 等表现层的事）；
//   ② 联机自检（两个裸 `BattleContext` 对打，**不该拖一整个对局驱动进来**）。
// 两处各写一份 = 迟早不一致（`CLAUDE.md` §三），而且**自检验的就不是真对局走的那条路**了。
//
// 判据 → `资料/联机P2P_设计与交接.md` §五·3（座位翻译）/ §六 N4。
using System;
using System.Collections.Generic;
using RuleEngine;

namespace CardPresentation.Net
{
    public static class NetApply
    {
        /// <summary>
        /// 落一条线上动作。**座位是绝对的**（0 = 主机 / 1 = 客机，两端同一套编号）
        /// ⇒ 这里**不做任何翻译**（`资料/联机P2P_设计与交接.md` §五·3 的那套镜像翻译**已作废**，
        /// 原因见 `NetProtocol.Fingerprint` 的注释）。`mySeat` 只用于日志。
        /// 返回引擎码。**不碰表现层** —— 那是调用方的事。
        /// </summary>
        public static int Apply(BattleContext ctx, MsgAction m, int mySeat, Action<string> log = null)
        {
            if (ctx == null || m == null) return RuleCodes.ErrBadHand;
            int actorSeat = m.actor;                       // 绝对座位：谁的牌就是谁的座位

            // 换牌那两条是**伪动作**（不在 `AiActionKind` 里）—— 它们也要能被重放，所以走同一条流
            if (m.kind == NetActionKind.Mulligan)
            {
                int n = RuleCore.Mulligan(ctx, actorSeat, m.marks ?? new int[0]);
                if (log != null) log($"换牌落地：座位 {actorSeat} 换了 {n} 张");
                return RuleCodes.OK;
            }
            if (m.kind == NetActionKind.MulliganDone)
            {
                RuleCore.EndMulligan(ctx);
                RuleCore.BeginTurn(ctx);
                if (log != null) log("换牌阶段结束，开打");
                return RuleCodes.OK;
            }

            // 🆕 2026-09-30：**进攻卡 / 防御卡**那两条（原版没有同步包，这是我们自己的设计 —— 见
            //   `NetActionKind.OffensivePick` 的注释）。它们也是**伪动作**：不在 `AiActionKind` 里，
            //   但走同一条流 ⇒ 重连时能重放、录像里也在。
            //   ⚠️ 顺序上它们跑在**换牌之后、`BeginTurn` 之前**（原版 `FinishMulliganFirstPhase` →
            //      `SetupEnviromentalEffectPhase`）。
            if (m.kind == NetActionKind.OffensivePick)
            {
                RuleCore.ChooseOffensiveCard(ctx, actorSeat, m.envSlot, m.envSO ?? "");
                if (log != null) log($"进攻卡落地：座位 {actorSeat} 选了槽 {m.envSlot}（环境 `{m.envSO}`）");
                return RuleCodes.OK;
            }
            if (m.kind == NetActionKind.DefensivePick)
            {
                RuleCore.ChooseDefensiveCard(ctx, actorSeat, m.envSlot);
                // 还要**换进手牌**（原版 `ClickChosenCardDone` 那条链）。只同步下标的话，
                // 重连重放时会缺这一步 ⇒ 两端手牌不同。
                var card = FindCard(ctx, m.defId);
                if (card != null) RuleCore.SetDefensiveCard(ctx, actorSeat, card);
                else if (log != null)
                    log($"⚠️ 防御卡 `{m.defId}` 在本机卡池里**查不到** ⇒ 只记了槽号，没换进手牌（不许静默）");
                return RuleCodes.OK;
            }

            // 面板答案先排进队列（与本地那条同一个约定：**顺序必须与结算顺序一致**）
            ctx.ChoosePicks.Clear(); ctx.ChooseCardIds.Clear();
            if (m.picks != null) for (int i = 0; i < m.picks.Length; i++) ctx.ChoosePicks.Enqueue(m.picks[i]);
            if (m.pickIds != null) for (int i = 0; i < m.pickIds.Length; i++) ctx.ChooseCardIds.Enqueue(m.pickIds[i]);

            // 座位：**绝对编号 ⇒ 什么都不翻**（`slot` / `handIdx` / `targetP` 全是全局统一的）
            int targetP = m.targetP;
            var kind = (AiActionKind)m.kind;
            int code;
            switch (kind)
            {
                case AiActionKind.PlayCard:
                    // ⚠️ **用手牌下标、不用实例 id**：实例号按座位顺序发（`NextInstanceId++`）
                    //    ⇒ 两端「自己 = 0 号位」时**同一条牌的实例号不一样**（`handId` 在这里没用）。
                    code = RuleCore.PlayCard(ctx, actorSeat, m.handIdx, m.slot);
                    break;
                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                    code = RuleCore.DeclareAttack(ctx, actorSeat, m.slot, targetP, m.targetSlot, m.ranged);
                    break;
                case AiActionKind.ActiveAbility:
                    if (m.altKeyword == "oath") code = RuleCore.UseOathAbility(ctx, actorSeat, m.slot);
                    else if (!string.IsNullOrEmpty(m.altKeyword))
                        code = RuleCore.UseAlternative(ctx, actorSeat, m.slot, m.altKeyword, m.targetSlot);
                    else code = RuleCore.UseAbility(ctx, actorSeat, m.slot, m.targetSlot);
                    break;
                case AiActionKind.CollectWaystone:
                    code = RuleCore.CollectWaystone(ctx, actorSeat, m.slot);
                    break;
                case AiActionKind.EndTurn:
                    code = RuleCore.EndTurn(ctx);
                    if (code == RuleCodes.OK) RuleCore.BeginTurn(ctx);   // 对面回合开打（与本地那条同一约定）
                    break;
                default:
                    return RuleCodes.ErrUnimplemented;
            }
            if (code != RuleCodes.OK && log != null)
                log($"这条动作被引擎拒了：{RuleCodes.Describe(code)}（kind={m.kind}）");
            return code;
        }

        /// <summary>这条动作落地之后是不是**回合已经换人**（调用方要重置 AI 计时、对指纹等）。</summary>
        public static bool EndsTurn(MsgAction m)
        {
            return m != null && ((AiActionKind)m.kind) == AiActionKind.EndTurn;
        }

        /// <summary>换牌阶段结束那一条（面板要收掉、交互要打开）。</summary>
        public static bool IsMulliganDone(MsgAction m)
        {
            return m != null && m.kind == NetActionKind.MulliganDone;
        }

        /// <summary>🆕 2026-09-30：这条动作是不是「进攻卡 / 防御卡」那两条 —— 它们发生在
        /// **换牌之后、开打之前**，所以主机那边的「不是你的回合」那道守卫要**放行**它们
        /// （那时候还没轮到谁，`Ctx.Active` 根本不适用）。</summary>
        public static bool IsEnvPick(MsgAction m)
        {
            return m != null && (m.kind == NetActionKind.OffensivePick || m.kind == NetActionKind.DefensivePick);
        }

        /// <summary>🆕 2026-09-30：这条动作是不是**先手方选进攻卡** —— 它是「该应用哪条环境」的判据，
        /// 落地之后调用方要**重新走一次 reveal / 应用**（见 `BattleDriver.ApplyLoggedAction`）。</summary>
        public static bool IsOffensivePick(MsgAction m)
        {
            return m != null && m.kind == NetActionKind.OffensivePick;
        }

        /// <summary>按 `CardDef.Id` 从**本局卡池**里查一张卡（防御卡那条路要用它把卡换回来）。
        /// ⚠️ 查不到就返回 null、由调用方**出声** —— 不许静默（本项目的红线）。</summary>
        static CardDef FindCard(BattleContext ctx, string id)
        {
            if (ctx == null || string.IsNullOrEmpty(id) || ctx.CardPool == null) return null;
            for (int i = 0; i < ctx.CardPool.Count; i++)
                if (ctx.CardPool[i] != null && ctx.CardPool[i].Id == id) return ctx.CardPool[i];
            return null;
        }
    }
}
