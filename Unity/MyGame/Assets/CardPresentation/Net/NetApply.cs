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
using System.Runtime.CompilerServices;
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

            // 🔴 **2026-10-24（`F8` · `A1099` 第 3 条分叉）：被推迟的 `BeginTurn` 在这里兑现 ——
            //   除了「环境卡」那两条伪动作，**下一条动作（不管是什么）落地之前都必须先把回合开起来**。**
            //
            //   **判据 = 录制端的程序点**（不是我们挑的）：活局 / 重连重放那一趟在 `MulliganDone`
            //   落地之后**当场**就开回合（`BattleDriver.ApplyLoggedAction` 末尾那句 /
            //   `NetReplay` 收尾那句）⇒ 录像里 **`MulliganDone` 之后的每一条**（含伪动作）
            //   的 `DeepHash` 都含「回合已开」（`Turn` / 能量 / 起手那张 + 天赋卡都在里面）。
            //   回放侧原来只让**真动作**兑现它（本方法下面那句），⇒ 「`MulliganDone` 之后再来一条
            //   **伪动作**」时两边差一整个回合 —— 实测（`A1099` 第 3 条 = kind 100 的换牌残条）：
            //   录的是 `T1 能2 手5 库25`、放的是 `T0 能0 手3 库26`，终局指纹却相等
            //   （回放收尾那句补上了回合 ⇒ **只有逐条轨迹抓得到**）。
            //
            //   ⚠️ **环境卡那两条必须留着不兑现**：原版 `StartBattlePhase` 排在
            //   `SetupEnviromentalEffectPhase` / `ApplyOffensiveAndDefensiveEffects` **之后**
            //   （判据 C2/R-03，见 `MulliganDone` 那一支的逐句注释）⇒ `IsEnvPick` 排除。
            //   ⚠️ 第一次 `MulliganDone`：此刻表里**还没有**待办（待办是它自己下面记的）⇒ 不会误开回合。
            //   ⚠️ 不认识的 `kind` 也会先兑现 —— 与活局一致（活局那时回合早就开过了）；
            //      它随后照旧被拒（下面 `IsRealActionKind` 那道闸）。
            if (!IsEnvPick(m) && SetupTurnPending(ctx)) BeginTurnAfterSetup(ctx);

            // 换牌那两条是**伪动作**（不在 `AiActionKind` 里）—— 它们也要能被重放，所以走同一条流
            if (m.kind == NetActionKind.Mulligan)
            {
                int n = RuleCore.Mulligan(ctx, actorSeat, m.marks ?? new int[0]);
                if (log != null) log($"换牌落地：座位 {actorSeat} 换了 {n} 张");
                return RuleCodes.OK;
            }
            if (m.kind == NetActionKind.MulliganDone)
            {
                bool wasOpen = ctx.MulliganOpen;
                // 🔴 **2026-10-24（`A29` · 判据 = `C2_引擎线判据_回放与环境.md` R-01 / R-03）**：
                //   这里**只关换牌阶段**，⛔ **不再顺手 `BeginTurn`**。
                //
                //   **原版次序**（逐句读过 `d:/2/tools/decomp_full/`）：
                //     `_FinishMulliganFirstPhase_d__349__MoveNext.c:107`（`SetupEnviromentalEffectPhase`，选环境卡）
                //   → `_FinishMulliganFinalPhase_d__351__MoveNext.c:142`（`ApplyOffensiveAndDefensiveEffects`，环境生效）
                //   → `:159-160`（`ShuffleDeck`）→ `:165`（`CompleteMulliganPhase`）
                //   → **`:233 StartBattlePhase`（= 回合开始，是【最后一步】）**。
                //   ⇒ 「换牌完成」与「回合开始」之间**还夹着环境卡那两条伪动作**。
                //
                //   **录制端本来就是这个次序**（`BattleDriver.OnMulliganDone`：`RecRaw(MulliganDone)`
                //   → `BeginOffensivePhaseIfAny()` → `BeginBattleAfterSetup()` → `RuleCore.BeginTurn`）
                //   ⇒ **要改的只有这一侧（回放 / 重连重放）**。原来这里顺手开回合，等于把原版的
                //   **最后一步**提到了**第二步** ⇒ 录像里 `MulliganDone` 那一格的 `DeepHash`（录制端取在
                //   `BeginTurn` **之前**，`BattleDriver.cs` 的 `RecRaw`）与回放端取的（`BeginTurn` **之后**）
                //   必然不等 ⇒ 出厂 `mulliganEnabled=1` 的**每一局**都假报分叉。
                //
                //   回合开始改由 <see cref="BeginTurnAfterSetup"/> 在**`MulliganDone` 之后的每一条动作之前**
                //   补上（环境卡那两条伪动作**除外** ⇒ 次序自然正确）。🔴 **2026-10-24（`F8`）就地订正**：
                //   这里原来写「第一条真动作之前」—— 太窄，见 `Apply` 开头那一大段。
                //   ⚠️ 判别「待补」用的是 <see cref="PendingSetupBegin"/> 这张弱表，⛔ **不是**
                //      `ctx.Turn == 0 && !ctx.MulliganOpen` —— 后者对「压根没有换牌阶段、`BeginTurn`
                //      还没跑」的裸 `BattleContext`（自检夹具直接调 `NetApply.Apply`）**恒真**，
                //      会凭空给它开一个回合（静默改状态）。
                RuleCore.EndMulligan(ctx);
                // 只有**真的关掉了换牌阶段**才记待办（`EndMulligan` 在 `MulliganOpen == false` 时是空操作）——
                // 否则一条重复的 `MulliganDone` 会凭空给这一局再补一个 `BeginTurn`（静默多开一个回合）。
                if (wasOpen)
                {
                    PendingSetupBegin.Remove(ctx);
                    PendingSetupBegin.Add(ctx, PendingSetupMark);
                }
                if (log != null) log("换牌阶段结束（回合开始推迟到环境卡落地之后）");
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

            // 🔴 **2026-10-24（`A29`）：走到这里 = 开局那一段已经结束**（换牌 → 环境卡都落完了），
            //   因为上面四条伪 `kind` 已经各自 `return`。原版 `StartBattlePhase`（= `BeginTurn`）
            //   就排在环境卡之后、第一条真动作之前。
            //   🔴 **2026-10-24（`F8`）：那句「补回合」**已经挪到本方法**开头**（`!IsEnvPick(m) &&
            //   SetupTurnPending(ctx)` 那一条）—— 判据是「`MulliganDone` 之后**任何**一条动作之前都得补上，
            //   不只是真动作」（录像里伪动作后面那几格的 `DeepHash` 也含回合已开）。
            //   ⛔ **别在这里再加一句**：规则只留一处（同一条写在两处 = 迟早不一致）。
            //   · 下面那道 `IsRealActionKind` 闸**照旧**拒掉不认识的 `kind` —— 但**回合已经由开头那句
            //     兑现过了**，这与活局同形（活局收到坏动作时回合早就开过了；坏的只是这一条，不是回合）。
            var kind = (AiActionKind)m.kind;
            if (!IsRealActionKind(kind)) return RuleCodes.ErrUnimplemented;

            // 面板答案先排进队列（与本地那条同一个约定：**顺序必须与结算顺序一致**）
            ctx.ChoosePicks.Clear(); ctx.ChooseCardIds.Clear();
            if (m.picks != null) for (int i = 0; i < m.picks.Length; i++) ctx.ChoosePicks.Enqueue(m.picks[i]);
            if (m.pickIds != null) for (int i = 0; i < m.pickIds.Length; i++) ctx.ChooseCardIds.Enqueue(m.pickIds[i]);

            // 座位：**绝对编号 ⇒ 什么都不翻**（`slot` / `handIdx` / `targetP` 全是全局统一的）
            int targetP = m.targetP;
            int code;
            switch (kind)
            {
                case AiActionKind.PlayCard:
                    // ⚠️ **用手牌下标、不用实例 id**：实例号是**进程内自增**的（`NextInstanceId++` ——
                    //    前面打过几局就走到哪儿了），**两台之间根本不可比** ⇒ `handId` 在这里没用。
                    //    ⚠️ 2026-10-19（`A1114`(a) 同族）就地订正：原来这里写「两端『自己 = 0 号位』时实例号不一样」
                    //    —— 那是**镜像时代**的理由，**已作废**（座位从 2026-09-26 起是绝对的）；结论不变。
                    code = RuleCore.PlayCard(ctx, actorSeat, m.handIdx, m.slot);
                    break;
                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                    code = RuleCore.DeclareAttack(ctx, actorSeat, m.slot, targetP, m.targetSlot, m.ranged);
                    break;
                case AiActionKind.ActiveAbility:
                    if (m.altKeyword == "oath") code = RuleCore.UseOathAbility(ctx, actorSeat, m.slot);
                    else if (!string.IsNullOrEmpty(m.altKeyword) && m.altKeyword != SimpleAI.AltActiveAbility)
                        code = RuleCore.UseAlternative(ctx, actorSeat, m.slot, m.altKeyword, m.targetSlot);
                    else code = UseAbilityOrActive(ctx, actorSeat, m);
                    break;
                case AiActionKind.CollectWaystone:
                    code = RuleCore.CollectWaystone(ctx, actorSeat, m.slot);
                    break;
                case AiActionKind.EndTurn:
                    code = RuleCore.EndTurn(ctx);
                    if (code == RuleCodes.OK) RuleCore.BeginTurn(ctx);   // 对面回合开打（与本地那条同一约定）
                    break;
                default:
                    // `IsRealActionKind` 已经在进来之前挡掉了 ⇒ 这一支走不到（留着只为编译期兜底；
                    // ⛔ 别把上面那个判据搬到这里 —— 那条「哪些 kind 算真动作」只该有一处）。
                    return RuleCodes.ErrUnimplemented;
            }
            if (code != RuleCodes.OK && log != null)
                log($"这条动作被引擎拒了：{RuleCodes.Describe(code)}（kind={m.kind}）");
            return code;
        }

        // ==================================================================
        //  🔴 2026-10-24（`A29`）：**「换牌做完、回合还没开」这个中间态**
        //
        //  原版 `StartBattlePhase`（= 我们的 `RuleCore.BeginTurn`）排在
        //  `换牌完成 → 选环境卡 → 环境生效 → 洗牌/补牌` **之后**（判据逐句见 `MulliganDone` 那一支）。
        //  我们的动作流里**没有「setup 结束」这个显式标记** ⇒ 只能这样落：
        //    · `MulliganDone` 落地时**记一笔待办**（<see cref="PendingSetupBegin"/>）；
        //    · **`MulliganDone` 之后的每一条动作**（环境卡那两条除外）落地之前把它兑现
        //      （<see cref="BeginTurnAfterSetup"/>）。🔴 **2026-10-24（`F8`）就地订正**：这里原来写
        //      「**第一条真动作**落地之前」—— 那条太窄：录像里 `MulliganDone` 之后可能还跟一条
        //      **伪动作**（换牌残条 / 重复的 `MulliganDone` / 被拒的坏 kind），而录制端那时**回合已经开了**
        //      ⇒ 只让真动作兑现会让那一格的 `DeepHash` 必然不等（`A1099` 第 3 条分叉就是这么来的）。
        //  这样回放 / 重连重放 / 活局的**动作次序**都与原版同形，而且录像里 `MulliganDone` 那一格的
        //  `DeepHash` 两端取的**是同一个程序点**（`BeginTurn` 之前）。
        //
        //  ⚠️ 为什么用弱表而不是 `ctx` 上的字段：`BattleContext` 归 `RuleEngine/**`（本轮不在白名单），
        //     而且**加字段会进制指纹**（`NetProtocol.Fingerprint` 会数 `Events.Count` 那一族）。
        //  ⚠️ 为什么不用 `ctx.Turn == 0 && !ctx.MulliganOpen` 当判据：对「没有换牌阶段、`BeginTurn`
        //     还没跑」的裸 `BattleContext` 它**恒真**（自检夹具直接调 `NetApply.Apply`）——
        //     那会凭空开一个回合，**静默改状态**。
        // ==================================================================

        /// <summary>「换牌已做完、`BeginTurn` 还没跑」的 context 表。键弱引用 ⇒ 不会拖住已结束的对局。</summary>
        static readonly ConditionalWeakTable<BattleContext, object> PendingSetupBegin
            = new ConditionalWeakTable<BattleContext, object>();
        static readonly object PendingSetupMark = new object();

        /// <summary>这些 `kind` 才叫「真动作」（= 会落到引擎结算表的那些）。
        /// ⛔ 四条伪 `kind`（`Mulligan` / `MulliganDone` / `OffensivePick` / `DefensivePick`）、
        /// 录像侧的 `RecKind*`（200+）**都不算** —— 它们跑在开局那一段里面。
        /// 🔴 「哪些算真动作」**只此一处**。
        /// ⚠️ **2026-10-24（`F8`）**：`Apply` 里那条「补开 `BeginTurn`」的判据**不再用它**了 ——
        ///   现在按「是不是环境卡」划（见 `Apply` 开头那句），因为伪动作与坏 `kind` 也得先开回合。
        ///   本方法仍是「真动作」的唯一判据（`Apply` 末尾那道 `ErrUnimplemented` 闸靠它）。</summary>
        public static bool IsRealActionKind(AiActionKind kind)
        {
            switch (kind)
            {
                case AiActionKind.PlayCard:
                case AiActionKind.AttackMelee:
                case AiActionKind.AttackRanged:
                case AiActionKind.ActiveAbility:
                case AiActionKind.CollectWaystone:
                case AiActionKind.EndTurn:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>这个 context 现在是不是「换牌做完了、回合还没开」（= 开局那一段还差最后一步）。
        /// 自检/诊断用；产品路径只需要 <see cref="BeginTurnAfterSetup"/>。</summary>
        public static bool SetupTurnPending(BattleContext ctx)
        {
            return ctx != null && PendingSetupBegin.TryGetValue(ctx, out _);
        }

        /// <summary>把被推迟的 `BeginTurn` 补上（对应原版 `StartBattlePhase`）。**幂等**。
        /// 返回 true = 这一次真的开了回合；false = 没有待办（无换牌局 / 已经开过 / `ctx` 为 null）。
        /// <para>🔴 调用点（⛔ 别在别处再叫）：① <see cref="Apply"/> **开头** —— 「`MulliganDone` 之后的
        /// 每一条动作（环境卡那两条除外）落地之前」（`F8` 就地订正：原来是「第一条真动作之前」，
        /// 那条太窄，见上面 `PendingSetupBegin` 那节）；
        /// ② `BattleDriver.PlayReplay` / `NetReplay` 收尾（流里最后一条就是 `MulliganDone` / 环境卡的那种 Edge）；
        /// ③ `BattleDriver.ApplyLoggedAction` **末尾**（**活局**那一路 —— 换牌一结束就要开回合，
        /// 否则玩家第一手没有能量、这一局会卡在回合 0；重放那两趟由 `_logReplaying` 挡住）；
        /// ④ `BattleDriver.PlayReplay` 的**投降**那一支（录像侧的 `RecKindForfeit` 不走 `Apply`）；
        /// ⚠️ ③ 必须在 `RecMsg` **之后**：录像里 `MulliganDone` 那一格要与录制端取同一个程序点。</para></summary>
        public static bool BeginTurnAfterSetup(BattleContext ctx)
        {
            if (ctx == null) return false;
            if (!PendingSetupBegin.TryGetValue(ctx, out _)) return false;
            PendingSetupBegin.Remove(ctx);
            RuleCore.BeginTurn(ctx);
            return true;
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

        // ── ⑪ 主动技能那一格的**兜底两支**（🆕 2026-10-27 · 执行代理 `B87_A11`）──────────────
        //  `oath` / 替代行动在调用点先分流；剩下两支：哨兵 `SimpleAI.AltActiveAbility` = 原版通用那一格
        //  主动能力（`activeAbilityLogic`）· `null` = 普通主动技。
        //  🔴 **为什么必须认这个哨兵**：不认 ⇒ 对面 / 回放方把 ④ 档那一手路由成 `UseAbility`
        //  （`ErrNoAbility`）⇒ **P2P 两端指纹静默分叉**（单机看不出来）。判据同 `SimpleAI.UseAbilityOrActive`。

        /// <summary>线上 `ActiveAbility` 动作的兜底落地：哨兵 ⇒ `RuleCore.UseActiveAbility`，否则 ⇒ `RuleCore.UseAbility`。
        /// <para>⚠️ 「跑不了就不改状态」是**引擎侧**保证的（`UseActiveAbility` 在一条行都跑不了时
        /// 直接 `ErrUnimplemented`、不置 `Exhausted` / 不发 `EvtKind.Ability`）⇒ 两端走**同一个函数**、
        /// 得到**同一个码**，⛔ 别在这里补任何「先改再回滚」的分支。</para></summary>
        static int UseAbilityOrActive(BattleContext ctx, int actorSeat, MsgAction m)
        {
            return m.altKeyword == SimpleAI.AltActiveAbility
                 ? RuleCore.UseActiveAbility(ctx, actorSeat, m.slot, m.targetSlot)
                 : RuleCore.UseAbility(ctx, actorSeat, m.slot, m.targetSlot);
        }
    }
}
