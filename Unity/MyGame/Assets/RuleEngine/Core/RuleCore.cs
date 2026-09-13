// RuleCore.cs — 简单版规则引擎的核心
//
// 语义来源：`d:/warpforge/scripts/rule_core.gd`（4719 行，照着规则书 + 原版反编译核过）。
// **改任何一条规则之前，先回去看那份** —— 下面每一处的注释都标了出处。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。
//    随机数走 System.Random(seed)，同一个种子必须永远得到同一局。
//
// v1 只实现 5 个关键词：Vanguard / Stealth / Flying / Armour / Shield。
// 其余关键词会被解析出来但不参与结算 —— 用 `UnimplementedKeywords()` 查有哪些被忽略了。
//
// 2026-09-12 增补：**技能与触发**
//   · 触发类关键词（Rally / Strike / Slay / Backlash / Penitence）—— 时机照抄规则书 :161 的 61 关键词表
//   · 主动技能 `Ability:` —— 花掉本单位一次行动放效果（原版「替代行动」的简化版）
//   · 效果文字走 `EffectSpec` 那个封闭文法，**不解析原版 desc**
//   · 每次发生什么都往 `ctx.Signals` 里发一条 **结构化事件**（见 BattleEvent.cs）
//     表现层据此播「部队卡发动技能」「触发效果」这两类特效 —— 这两类以前接不上，
//     就是因为引擎里没有这两种事件。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ---- 规则常量（对齐 rule_core.gd:44-49）----
        public const int BoardSize = BoardSpec.Size;
        public const int WarlordSlot = BoardSpec.WarlordSlot;
        public const int StartHand = 3;
        public const int HandMax = 10;
        public const int DefaultWarlordHealth = 30;
        public const int DefaultWarlordAttack = 2;

        static readonly CardDef FallbackWarlord = new CardDef(
            "warlord_default", "Warlord", "hero", "", null, null,
            0, DefaultWarlordAttack, DefaultWarlordHealth, 0, null);

        // ==================================================================
        //  开局
        // ==================================================================

        /// <summary>
        /// 创建对局：提取督军 → 洗牌 → 双方各起手 <see cref="StartHand"/> 张。
        /// 牌组里第一张 <c>type == "hero"</c> 的卡被提为督军，其余进牌库；没有就用默认督军。
        /// </summary>
        /// <param name="shuffle">
        /// 关掉洗牌 → 牌库保持传入顺序，测试就能摆出确定的起手。
        /// （`rule_core.new_battle` 也有这个开关，语义一致。）
        /// </param>
        /// <param name="cardPool">
        /// **全卡池** —— `create` 造牌的候选来源（`CreatePool`）。不传 = 这一局不能造牌
        /// （造牌那条会**如实报**「没有卡池」，不会退化成从牌库里抽）。
        /// 调用方通常就是 `CardDatabase.Load()` 那一份。
        /// </param>
        public static BattleContext NewBattle(IList<CardDef> deckA, IList<CardDef> deckB,
                                              int seed = 0, bool shuffle = true,
                                              IList<CardDef> cardPool = null,
                                              bool openMulligan = false)
        {
            var ctx = new BattleContext(seed);
            ctx.CardPool = cardPool == null ? null : new List<CardDef>(cardPool);

            ctx.Players[0] = BuildPlayer(deckA, ctx.Rng, "P1", shuffle);
            ctx.Players[1] = BuildPlayer(deckB, ctx.Rng, "P2", shuffle);
            ctx.Active = 0;
            ctx.Turn = 0;

            // 轮流发牌（和 rule_core 一致：i 循环里两边各抽一张）
            for (int i = 0; i < StartHand; i++)
            {
                Draw(ctx, 0);
                Draw(ctx, 1);
            }
            ctx.Log($"开局：双方各起手 {StartHand} 张，{ctx.Players[0].Name} 先手");

            // 换牌阶段（原版：抽完起手牌进 `_SetupMulliganPhase`，双方换完才 `StartBattlePhase`）。
            // ⚠️ 默认**关**：这是「要不要进这个阶段」的选择，由调用方说 —— 表现层单机默认开，
            //    规则自检默认关（不然每个用例都要先换一副牌才能验回合 1 的账）。
            ctx.MulliganOpen = openMulligan;

            CheckWinner(ctx);
            return ctx;
        }

        // ==================================================================
        //  开局换牌（原版 `MulliganManager` / `PlayerHand.FinishMulligan`）
        //
        //  规则书 :46「**换牌（Mulligan）| 可弃回任意起手牌后重洗补抽**」——
        //  三个动作都要有：**弃回**（进牌库）、**重洗**（洗牌库）、**补抽**（抽同样张数）。
        //  原版这条链在反编译里是明的：`_SetupMulliganPhase` → `MulliganManager.ActivateMulligan`
        //  →（玩家点完）`_FinishMulliganFirstPhase` → `_FinishMulliganFinalPhase`
        //  → **`BattleManager.ShuffleDeck`** → `PlayerHand.CompleteMulliganPhase` → `StartBattlePhase`。
        // ==================================================================

        /// <summary>
        /// 换掉第 `player` 方手里的 `handIndices` 那几张牌：**弃回牌库 → 洗牌 → 补抽同样张数**。
        /// 返回真正换掉的张数（-1 = 不在换牌阶段，调用方该把它报出来，别当成功）。
        ///
        /// ⚠️ 用 `ctx.Rng`（种子化）—— 对局必须可复现（本工程的铁律）。
        /// </summary>
        public static int Mulligan(BattleContext ctx, int player, IList<int> handIndices)
        {
            if (ctx == null || player < 0 || player > 1) return -1;
            if (!ctx.MulliganOpen) return -1;
            if (handIndices == null || handIndices.Count == 0) return 0;

            var ps = ctx.Players[player];

            // 去重 + **从大到小**删 —— 从小到大删的话，删掉一个后面的下标就全错位了
            var idx = new List<int>();
            for (int i = 0; i < handIndices.Count; i++)
            {
                int k = handIndices[i];
                if (k < 0 || k >= ps.Hand.Count || idx.Contains(k)) continue;
                idx.Add(k);
            }
            if (idx.Count == 0) return 0;
            idx.Sort();

            for (int i = idx.Count - 1; i >= 0; i--)
            {
                ps.Deck.Add(ps.Hand[idx[i]]);
                ps.Hand.RemoveAt(idx[i]);
            }

            // 重洗：换回去的牌要**洗匀**，不然对手能从牌库顺序推出你换掉了什么
            //（原版是 `FinishMulliganFinalPhase` 里统一 `ShuffleDeck`，我们在这里洗同一件事）
            Shuffle(ps.Deck, ctx.Rng);

            // 补抽同样张数
            for (int i = 0; i < idx.Count; i++) Draw(ctx, player);

            ctx.Log($"{ps.Name} 换牌 {idx.Count} 张（弃回牌库 → 重洗 → 补抽）");
            return idx.Count;
        }

        /// <summary>换牌阶段结束（双方都决定了）。关掉标志，之后 `Mulligan` 不再有效。</summary>
        public static void EndMulligan(BattleContext ctx)
        {
            if (ctx == null || !ctx.MulliganOpen) return;
            ctx.MulliganOpen = false;
            ctx.Log("换牌阶段结束");
        }

        static PlayerState BuildPlayer(IList<CardDef> deck, Random rng, string name, bool shuffle)
        {
            var p = new PlayerState { Name = name };
            CardDef warlordCard = null;

            if (deck != null)
            {
                foreach (var c in deck)
                {
                    if (c == null) continue;
                    if (warlordCard == null && c.Type == "hero") warlordCard = c;
                    else p.Deck.Add(c);
                }
            }

            if (shuffle) Shuffle(p.Deck, rng);

            p.Warlord = new UnitState(warlordCard ?? FallbackWarlord, true);
            p.Warlord.Exhausted = false;        // 督军不受「部署当回合不可行动」约束
            p.Board[BoardSpec.WarlordSlot] = p.Warlord;
            return p;
        }

        /// <summary>
        /// **这张牌现在要几费** —— 卡面印的费用 + 本方的费用修正。
        ///
        /// **判据只此一处**：能不能打（`CanPlayCard` / `CanPlayTactic`）、扣费（`PlayCard` /
        /// `PlayTactic`）、AI 挑牌、卡面显示，全都问它。各写各的话会出现
        /// 「画面显示 2 费、点下去说能量不够」这种对不上的毛病。
        ///
        /// ⚠️ **按卡名匹配**（`CostMod.Key`）：同名卡一起降价 —— 我们没有卡实例这个身份。
        ///    卡组构筑（`DeckBuilder`）和候选池筛选（`CreatePool`）**用印的费用**，不走这里 ——
        ///    那是「这张牌的数值」，不是「这一局打它要花多少」。
        /// </summary>
        public static int CostOf(BattleContext ctx, int owner, CardDef c)
        {
            if (c == null) return 0;
            if (ctx == null || ctx.CostMods.Count == 0) return c.Cost;

            int v = c.Cost;
            string key = CreatePool.Norm(c.Name);
            for (int i = 0; i < ctx.CostMods.Count; i++)
            {
                var m = ctx.CostMods[i];
                if (m.Player != owner) continue;
                if (m.ExpireTurn >= 0 && ctx.Turn > m.ExpireTurn) continue;
                if (m.Key != "*" && m.Key != key) continue;
                v += m.Delta;
            }
            return System.Math.Max(0, v);
        }

        /// <summary>清掉已过期的费用修正（回合结束时调）</summary>
        static void ExpireCostMods(BattleContext ctx)
        {
            for (int i = ctx.CostMods.Count - 1; i >= 0; i--)
                if (ctx.CostMods[i].ExpireTurn >= 0 && ctx.Turn > ctx.CostMods[i].ExpireTurn)
                    ctx.CostMods.RemoveAt(i);
        }

        /// <summary>Fisher–Yates。用 ctx 的种子化随机源，对局才可复现</summary>
        static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        // ==================================================================
        //  回合
        // ==================================================================

        /// <summary>
        /// 回合开始：能量 → 解疲劳 → 抽牌。（rule_core.begin_turn）
        ///
        /// ⚠️ **能量按「每方自己的回合数」算，不是全局回合数。**
        ///    rule_core.gd 记着这条修正：「此前按全局回合数 → 后手首回合 2 能 /
        ///    先手第 2 回合 3 能，全对局能量曲线偏高」。
        ///    结果就是：先手第 1 回合 2 能、后手第 1 回合也是 2 能。
        /// </summary>
        public static void BeginTurn(BattleContext ctx)
        {
            if (ctx.IsOver) return;

            ctx.Turn++;
            // 「本回合」的费用修正到期 —— **必须在 Turn++ 之后**：
            // 它是在**上一回合**登记的（`ExpireTurn = 登记时的 Turn`），
            // 放到 `EndTurn` 里撤的话那会儿 Turn 还没变，会晚撤一个回合（撞到过）。
            ExpireCostMods(ctx);
            ctx.DiedThisTurn = 0;      // 「本回合阵亡数」按回合清零（`For each one that dies …` 用）
            var p = ctx.ActivePlayer;
            p.TurnCount++;
            // `Choose a friendly troop that died **since your last turn**` 的窗口起点。
            // 记在 `Turn++` 之后 = 「本回合开始的那一刻」，见 `PlayerState.LastTurnStartMark`。
            p.LastTurnStartMark = ctx.Turn;
            p.MaxEnergy = p.TurnCount + 1;
            p.Energy = p.MaxEnergy;

            // 己方单位解疲劳（对方的不动）
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = p.Board[s];
                if (u != null) u.RefreshForNewTurn();
            }

            // 「直到你的下个回合」的限时增益，在**施放者自己的回合开始时**撤
            // （`rule_core.gd:3019`）。两边场上都要扫 —— buff 可能在对方单位身上（`give -1 attack to an enemy`）。
            int reverted = 0;
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u2 = ctx.Players[pl].Board[s];
                    if (u2 != null) reverted += u2.RevertBuffs(false, ctx.Active);
                }
            if (reverted > 0) ctx.Log($"（{reverted} 条「直到你下个回合」的增益到期）");

            // ---- 失明到期（卡面写 `until your next turn`）----
            //      **在施放者自己的下一个回合开始时清**，和「直到你的下个回合」的限时增益同一个口径
            //      （那一条就在上面几行 `RevertBuffs(false, ctx.Active)`）。
            //      结果：卡在**对手的整个回合**里都还有效 —— 那正是这张牌的用处。
            //      ⚠️ 原版这条清除读的是 `blind_turn`、写的是 `blind_turn_end`（字段名对不上），
            //         所以原版实际表现为**永不恢复**。我们按卡面语义实现，不照抄那个笔误。
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u == null || !u.IsBlind) continue;
                    if (u.BlindOwner != ctx.Active) continue;        // 只按**施放者**的回合算
                    if (u.BlindTurnEnd >= 0 && ctx.Turn >= u.BlindTurnEnd)
                    {
                        u.IsBlind = false;
                        u.BlindTurnEnd = -1;
                        u.BlindOwner = -1;
                        u.RemoveAll("blind");
                        ctx.Log($"{u.Name} 的失明恢复（远程攻击力回到 {u.RangedAttack}）");
                    }
                }

            // ---- 回合**开始**触发段（规则书回合结构**第 9 步**"Beginning of turn effects"）----
            // 放在「限时增益到期」**之后**、**抽牌之前** —— 和 `rule_core.gd:1867-1871`
            // （`_expire_temp_buffs` → `_at_turn_effects("start")`）的相对次序一致。
            // 内容是：当前行动方**手牌里的陷阱卡** + 他登记的**常驻效果**。
            ResolveAtTurn(ctx, "turn_start");
            if (ctx.IsOver) return;      // 触发段能打死督军（`your troops take 1 damage` 那类）

            ctx.Log($"回合 {ctx.Turn} 开始：{p.Name} 能量 {p.Energy}，抽 1 张");
            Draw(ctx, ctx.Active);
        }

        /// <summary>回合结束：能量作废 → 移交。返回 <see cref="CheckWinner"/> 的结果。</summary>
        public static int EndTurn(BattleContext ctx)
        {
            if (ctx.IsOver) return ctx.Winner;

            var p = ctx.ActivePlayer;

            // 「本回合」的限时增益在**这一回合结束时**撤（`rule_core.gd:3018`）——
            // 两边场上都要扫（`give -1 attack to an enemy troop this turn` 也可能打在对方身上）
            int reverted = 0;
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u != null) reverted += u.RevertBuffs(true, ctx.Active);
                }
            if (reverted > 0) ctx.Log($"（{reverted} 条「本回合」增益到期）");

            // ---- 回合**结束**触发段（规则书回合结构**第 14 步**"End of turn abilities"）----
            // 放在「本回合限时增益到期」**之后**、**能量清零与再生之前** ——
            // 和 `rule_core.gd:2010-2024`（`_expire_temp_buffs(true)` → `_at_turn_effects("end")`
            // → `energy = 0` → Regeneration）的相对次序一致。
            ResolveAtTurn(ctx, "turn_end");
            if (ctx.IsOver) return ctx.Winner;

            // ---- 再生 X：**每回合结束时**治疗 X（规则书 :201「每回合结束时治疗 X」）----
            //      原版 `rule_core.gd:2025` 明写 `at the end of EACH turn → 双方单位`，
            //      而且是在 `energy = 0` **之前**结算的 —— 顺序照抄。
            for (int pl = 0; pl < 2; pl++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[pl].Board[s];
                    if (u == null || !u.IsAlive || !u.Has("regeneration")) continue;
                    int heal = u.KwValue("regeneration");
                    int before = u.Health;
                    u.Health = System.Math.Min(u.Health + heal, u.MaxHealth);
                    if (u.Health != before)
                    {
                        ctx.Log($"{u.Name} 的 Regeneration {heal}：{before} → {u.Health}"
                              + $"（上限 {u.MaxHealth}）");
                        EmitUnit(ctx, EvtKind.Hit, u, -(u.Health - before));   // 负数 = 治疗，表现层据此走绿字
                    }
                }

            p.Energy = 0;                       // 经典模式：未用能量作废（遭遇模式才保存 1 点）
            ctx.Active = 1 - ctx.Active;
            ctx.Log($"回合 {ctx.Turn} 结束，轮到 {ctx.ActivePlayer.Name}");
            return CheckWinner(ctx);
        }

        /// <summary>
        /// 抽 1 张。牌库空 → 疲劳 +1 并让督军挨这么多伤害。（rule_core._draw）
        /// ⚠️ 疲劳能打死督军，所以这里必须复查胜负。
        /// </summary>
        public static void Draw(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];

            if (ps.Deck.Count == 0)
            {
                ps.Fatigue++;
                ps.Warlord.Health -= ps.Fatigue;
                ctx.Log($"{ps.Name} 牌库抽空 —— 疲劳 {ps.Fatigue} 点伤害（督军剩 {ps.Warlord.Health}）");
                // 疲劳也算「挨了一下」—— 不带走 ApplyDamage（它不吃护盾/护甲），但要发事件，
                // 否则画面上督军莫名其妙掉血、一点反馈都没有
                ctx.Emit(EvtKind.Hit, p, BoardSpec.WarlordSlot, ps.Warlord.Name, amount: ps.Fatigue);
                CheckWinner(ctx);
                return;
            }

            // 从牌库**末尾**抽（和 rule_core 的 pop_back 一致）——
            // 这样 `_deck([a,b,c])` 这种「构造好顺序的牌库」测试才和原实现对得上
            int last = ps.Deck.Count - 1;
            var card = ps.Deck[last];
            ps.Deck.RemoveAt(last);
            ps.Hand.Add(card);
            EnforceHandLimit(ctx, p);
        }

        /// <summary>
        /// 手牌超上限 → 多出来的进弃牌堆（原版同样规则）。
        ///
        /// **只此一处**：抽牌和造牌（`create`）都调它 —— 两处各写一份，迟早出现
        /// 「抽牌会爆牌、造牌不会」这种对不上的行为。
        /// 从**末尾**丢（= 最后到手的那张），和 `Draw` 从牌库末尾抽是对称的。
        /// </summary>
        public static void EnforceHandLimit(BattleContext ctx, int p)
        {
            var ps = ctx.Players[p];
            while (ps.Hand.Count > HandMax)
            {
                int over = ps.Hand.Count - 1;
                var dropped = ps.Hand[over];
                ps.Hand.RemoveAt(over);
                ps.Discard.Add(dropped);
                ctx.Log($"{ps.Name} 手牌超上限，{dropped.Name} 进弃牌堆");
            }
        }

        // ==================================================================
        //  出牌
        // ==================================================================

        /// <summary>
        /// 只判不执行 —— 表现层拖拽过程中要**实时**知道这张牌能不能打，不能等松手才发现。
        /// 和 <see cref="PlayCard"/> 共用这一份判据，不是另写一份。
        /// </summary>
        public static int CanPlayCard(BattleContext ctx, int p, int handIdx, int slot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;

            var ps = ctx.Players[p];
            if (handIdx < 0 || handIdx >= ps.Hand.Count) return RuleCodes.ErrBadHand;

            var card = ps.Hand[handIdx];

            // 战术卡走**另一条判据**（不落格位、可能要选目标 —— 见 `EffectResolver.CanPlayTactic`），
            // 但**入口仍然是这一个**：表现层只问 `CanPlayCard`，免得两处各判一份、迟早不一致。
            //
            // ⚠️ 顺序照旧：**先于费用判断**。否则一张用不起的战术卡会报「能量不足」，
            //    把「本版不支持」误导成「再等等就能打」（原注释记的就是这个坑）。
            if (!card.IsUnit) return CanPlayTactic(ctx, p, handIdx, slot);

            // ⚠️ 校验顺序和 rule_core.play_card 一致：**先费用、后格位**
            //    （测试断言过「非法格不扣费」—— 顺序反了会出现「判了格位却已经扣过费」的中间态）
            if (CostOf(ctx, p, card) > ps.Energy) return RuleCodes.ErrCost;

            if (!BoardSpec.IsDeployable(slot)) return RuleCodes.ErrSlot;   // 含督军格
            if (ps.Board[slot] != null) return RuleCodes.ErrSlot;

            return RuleCodes.OK;
        }

        /// <summary>
        /// 打出第 handIdx 张手牌到 slot 格。（rule_core.play_card）
        ///
        /// ⚠️ **战术卡走同一个入口、不同的分支**：它不落格位，`slot` 的含义变成「效果打谁」，
        ///    交给 `EffectResolver.PlayTactic`（扣费 → 结算 → 弃牌堆）。表现层不用分两条路调。
        /// </summary>
        public static int PlayCard(BattleContext ctx, int p, int handIdx, int slot)
        {
            // 单位卡的判据在下面；战术卡先分流（判据共用 `CanPlayTactic`，不在这儿重写一份）
            if (p >= 0 && p < 2 && handIdx >= 0 && handIdx < ctx.Players[p].Hand.Count
                && !ctx.Players[p].Hand[handIdx].IsUnit)
                return PlayTactic(ctx, p, handIdx, slot);

            int code = CanPlayCard(ctx, p, handIdx, slot);
            if (code != RuleCodes.OK) return code;

            var ps = ctx.Players[p];
            var card = ps.Hand[handIdx];

            int costPaid = CostOf(ctx, p, card);
            ps.Energy -= costPaid;
            ps.Hand.RemoveAt(handIdx);

            // 「打出了这张牌」——单位卡紧接着还会发一条 `Deploy`，**日志那边会把连着的那条合并掉**
            // （`BattleContext.AppendLog`）。这条也是表现层「出牌那一下」的锚点：
            // 以前只有 `Deploy`，**战术卡压根没有事件**。
            ctx.Emit(EvtKind.Play, p, slot, card.Name);

            // 部署当回合不可行动 —— UnitState 构造出来就是 Exhausted = true
            var unit = new UnitState(card, false);
            ps.Board[slot] = unit;

            ctx.Log($"{ps.Name} 部署 {unit.Name}（{costPaid} 费，{unit.Attack}/{unit.Health}）"
                  + $"到槽 {slot}，能量剩 {ps.Energy}");
            ctx.Emit(EvtKind.Deploy, p, slot, unit.Name);

            // Rally（集结）：「从手牌部署后触发效果」—— 规则书 :200。
            // ⚠️ 触发在**部署之后**，所以效果里 `Self` 指向的已经是场上这个单位
            FireTriggerOnBoard(ctx, unit, KeywordTable.Rally);
            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        // ==================================================================
        //  免费部署（`Deploy …` 效果的落点）
        // ==================================================================

        /// <summary>
        /// **免费把一个单位放进本方第一个空格** —— 不花能量、不占手牌。
        /// 逐条照抄原版 `rule_core.gd:3871` 的 `_deploy_unit`：
        ///   ① 从槽 0 起找第一个空格（跳过督军槽）；**满场就什么都不做**（返回 false，不挤掉别人）；
        ///   ② `fast` / `flank` 的部署当回合不疲劳 —— 这件事在 `UnitState` 构造里就做掉了；
        ///   ③ 发一条部署事件（表现层靠它播登场特效）。
        ///
        /// ⚠️ **不触发 Rally**。这是**故意的**，不是漏了：规则书 `:200` 写的是
        ///   「集结：**从手牌**部署后触发效果」，原版也只在 `play_card` 那条路上触发
        ///   （`rule_core.gd:2314`），`_deploy_unit` 里没有（它的注释明说 play_card 路径自己广播）。
        ///   `RuleCore.PlayCard` 那边照旧触发 —— 两条路不一样是对的。
        /// </summary>
        /// <param name="slot">放到哪一格；失败时是 -1</param>
        public static bool DeployFree(BattleContext ctx, int owner, CardDef card, out int slot)
        {
            slot = -1;
            if (ctx == null || card == null) return false;
            if (owner < 0 || owner >= ctx.Players.Length) return false;

            var ps = ctx.Players[owner];
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                if (s == BoardSpec.WarlordSlot) continue;
                if (ps.Board[s] != null) continue;

                var unit = new UnitState(card, false);
                ps.Board[s] = unit;
                slot = s;
                ctx.Log($"{ps.Name} 免费部署 {unit.Name}（{unit.Attack}/{unit.Health}）到槽 {s}");
                ctx.Emit(EvtKind.Deploy, owner, s, unit.Name);
                return true;
            }
            ctx.Log($"{ps.Name} 场上没空格了 —— {card.Name} 部署不了");
            return false;
        }

        // ==================================================================
        //  攻击
        // ==================================================================

        /// <summary>
        /// 场上攻击力。近战和远程是两套数值。**所有攻击力修正都要走这里**，别在调用处直接读字段。
        ///
        ///   · 兽群（Pack）：场上每有 1 个**友方部队** +1 近战 +1 远程
        ///     （规则书 :195；原版 `rule_core.gd:4172` `field_attack` —— 原版只有这一处修正，
        ///      ⚠️ 它**不数督军**：过滤条件是 `not tu.is_warlord`，但**包含自己**）
        ///   · 失明（Blind）：**远程攻击设为 0**（规则书 :166；原版 `:4212` 是直接 `return ERR_NO_ATTACK`，
        ///     效果等价 —— 攻击力 0 就发不出攻击。走这里而不是在 `DeclareAttack` 里提前 return，
        ///     是为了让「为什么打不了」在界面上仍然显示成「没有攻击力」这一个原因）
        /// </summary>
        public static int FieldAttack(BattleContext ctx, int p, UnitState u, bool ranged)
        {
            if (ranged && u.IsBlind) return 0;

            int baseAtk = ranged ? u.RangedAttack : u.Attack;

            if (u.Has("pack"))
            {
                int n = 0;
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var tu = board[s];
                    if (tu != null && !tu.IsWarlord) n++;
                }
                baseAtk += n;
            }
            return baseAtk;
        }

        /// <summary>
        /// 目标合法性。（rule_core.is_valid_target）
        /// </summary>
        public static int IsValidTarget(BattleContext ctx, int p, int atkSlot, int tgtP, int tgtSlot, bool ranged)
        {
            if (tgtP != 0 && tgtP != 1) return RuleCodes.ErrTarget;
            if (!BoardSpec.IsValid(atkSlot) || !BoardSpec.IsValid(tgtSlot)) return RuleCodes.ErrNotUnit;

            var attacker = ctx.Players[p].Board[atkSlot];
            if (attacker == null) return RuleCodes.ErrNotUnit;
            if (attacker.Has(KeywordTable.CantAttack)) return RuleCodes.ErrNoAttack;

            var target = ctx.Players[tgtP].Board[tgtSlot];
            if (target == null) return RuleCodes.ErrNotUnit;
            if (tgtP == p) return RuleCodes.ErrSelf;

            // Stealth：隐身单位不可被攻击（攻击后揭示）
            if (target.Has(KeywordTable.Stealth)) return RuleCodes.ErrTarget;

            // Vanguard：敌方场上有 Vanguard → 只能打 Vanguard。**卡牌游戏的核心目标规则**
            bool enemyHasVanguard = false;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[tgtP].Board[s];
                if (u != null && u != target && u.Has(KeywordTable.Vanguard))
                {
                    enemyHasVanguard = true;
                    break;
                }
            }
            if (enemyHasVanguard && !target.Has(KeywordTable.Vanguard)) return RuleCodes.ErrTarget;

            // Flying：**检查的是目标**（飞行单位不能被近战打到），远程正常，同为飞行可以。
            // ⚠️ rule_core.gd 修正过方向：「此前禁止飞行单位近战打地面、却允许地面近战打飞行」—— 正好反了
            if (!ranged && target.Has(KeywordTable.Flying) && !attacker.Has(KeywordTable.Flying))
                return RuleCodes.ErrTarget;

            return RuleCodes.OK;
        }

        /// <summary>
        /// 攻击结算。（rule_core.declare_attack）
        ///
        /// ⚠️⚠️ **远程攻击默认也吃反击。** 只有 Long Range 免。
        ///    直觉上会写成「远程不受反击」，那是错的 —— rule_core.gd 有明确修正记录：
        ///    「2026-08-21 修正：此前远程完全不吃反击，Long Range/Sniper 成死代码」。
        /// </summary>
        public static int DeclareAttack(BattleContext ctx, int p, int atkSlot,
                                        int tgtP, int tgtSlot, bool ranged = false)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!BoardSpec.IsValid(atkSlot)) return RuleCodes.ErrNotUnit;

            var attacker = ctx.Players[p].Board[atkSlot];
            if (attacker == null) return RuleCodes.ErrNotUnit;
            if (attacker.Exhausted) return RuleCodes.ErrExhausted;
            if (attacker.IsStunned) return RuleCodes.ErrStunned;

            // **攻击配额**：嗜血（Blood Thirst）每回合最多 2 次，其余 1 次。
            // 规则书 :172「你的回合可进行至多 2 次攻击」；原版 `rule_core.gd:4205`
            int atkLimit = attacker.Has("bloodthirst") ? 2 : 1;
            if (attacker.AttacksThisTurn >= atkLimit) return RuleCodes.ErrExhausted;

            // 压制：**无法执行近战攻击**（规则书 :194；原版 `:4209`）。⚠️ 只禁近战，远程照常 ——
            // 顺序也在原版那个位置：挨在攻击配额之后、攻击力判定之前。
            // （失明不在这里提前 return，它在 `FieldAttack` 里把远程攻击力算成 0，见那边的注释）
            if (!ranged && attacker.Has("pindown")) return RuleCodes.ErrPindown;

            int atk = FieldAttack(ctx, p, attacker, ranged);
            if (atk <= 0) return RuleCodes.ErrNoAttack;

            int code = IsValidTarget(ctx, p, atkSlot, tgtP, tgtSlot, ranged);
            if (code != RuleCodes.OK) return code;

            // 攻击者消耗：**达到配额上限才疲劳** —— 原版 `rule_core.gd:4255`：
            // `attacks_turn += 1; if attacks_turn >= atk_limit: exhausted = true`
            // （嗜血单位打完第一次**不**疲劳，所以还能再打一次）
            attacker.AttacksThisTurn++;
            if (attacker.AttacksThisTurn >= atkLimit) attacker.Exhausted = true;
            if (attacker.Has(KeywordTable.Stealth))
            {
                attacker.RemoveKeyword(KeywordTable.Stealth);
                ctx.Log($"{attacker.Name} 攻击后现身（失去 Stealth）");
            }
            // 伪装：**攻击前**不能被敌方战术/效果选中，攻击之后就没了（规则书 :173；原版 `:4259` 一带）
            if (attacker.Has("camouflage"))
            {
                attacker.RemoveKeyword("camouflage");
                ctx.Log($"{attacker.Name} 攻击后失去伪装（Camouflage）");
            }

            var target = ctx.Players[tgtP].Board[tgtSlot];

            // 攻击宣言：**在伤害之前**发 —— 表现层才有「抬手 → 命中」的余地
            // `targetCardId` 现在就记下来：留存日志以后回看时，那个格位早就换人了
            ctx.Emit(EvtKind.Attack, p, atkSlot, attacker.Name, ranged: ranged,
                     targetPlayer: tgtP, targetSlot: tgtSlot,
                     targetCardId: target != null ? target.Name : null);

            // ---- 哨戒 X：**攻击哨戒单位时攻击者先受 X 伤害**，「然后照常结算攻击」----
            //      规则书 :205；原版 `rule_core.gd:4280`（在星镖**之前**，是攻击结算的第 0 步）。
            //      ⚠️ 「照常结算」= 挨了哨戒**不打断攻击**，攻击者就算被打死也照样把这一下打完
            //      —— 原版就是顺序执行、没有中断。别自作主张加「死了就取消攻击」。
            if (target.Has("sentry"))
            {
                int se = target.KwValue("sentry");
                int sd = Hurt(ctx, attacker, se, target.Name + " 的 Sentry");
                ctx.Log($"Sentry {se}：{target.Name} 反击了正在攻击它的 {attacker.Name} {sd} 伤"
                      + $"（剩 {attacker.Health}）");
            }

            // ---- 星镖 X：**攻击伤害之前**先对目标追加 X 点（规则书 :207；原版 `:4285`）----
            //      目标被这 X 点打死就**跳过攻击伤害**（原版那支 `target_died`）
            bool targetDied = false;
            if (attacker.Has("shuriken"))
            {
                int sh = attacker.KwValue("shuriken");
                int extra = Hurt(ctx, target, sh, attacker.Name + " 的 Shuriken");
                ctx.Log($"Shuriken {sh}：{attacker.Name} 先对 {target.Name} 追加 {extra} 伤"
                      + $"（剩 {target.Health}）");
                if (!target.IsAlive) targetDied = true;
            }

            int dealt = 0;
            int hpBefore = target.Health;      // 践踏要算「溢出多少」，所以得记打之前那一下
            if (!targetDied)
            {
                int dmg = atk;
                // 标记光 X：目标带标记光时，**远程**攻击伤害 +X；受远程伤害后**移除全部**标记光
                // （规则书 :192；原版 `:4296` 一带）
                if (ranged && target.Has("markerlight"))
                {
                    int ml = target.KwValue("markerlight");
                    dmg += ml;
                    // ⚠️ 规则书 :192 是「移除**全部**标记光」—— 用 `RemoveKeyword` 只会减一层
                    target.RemoveAll("markerlight");
                    ctx.Log($"{target.Name} 身上的 Markerlight {ml} 让这次远程伤害 +{ml}，标记光随后移除");
                }
                dealt = Hurt(ctx, target, dmg, attacker.Name, p);
            }
            ctx.Log($"{attacker.Name} {(ranged ? "远程" : "近战")}攻击 {target.Name}："
                  + $"{atk} 攻 → 实际 {dealt} 伤（{target.Name} 剩 {target.Health}）");

            // ⚠️ `targetDied` 只在星镖分支里被赋过值 —— 普通攻击打死的那一枪没有标记，
            //    而 Sniper 的判断依据正是它。这里补上：`dealt > 0` 是「真的打中了」
            //    （护盾全挡 = 0、无敌 = 0、打空 = 0），打中了且没血了就是摧毁。
            if (dealt > 0 && !target.IsAlive) targetDied = true;

            // 反击：目标用**近战攻击力**反击（不是远程）。只有两个来源能免：
            //   · Long Range：远程攻击不承受伤害（规则书 :191）
            //   · Sniper：「若**远程**攻击**会摧毁**目标：不承受反击伤害」（规则书 :209；原版 `:4312`）
            // ⚠️ 原版 `rule_core.gd:4310` 有一条修正记录：「此前『目标死则不反击』= 近战击杀免反（规则偏差）
            //    + Sniper 成死代码」—— 所以反击**不**因目标死亡而跳过，只能靠这两个关键词免。
            bool noCounter = ranged && attacker.Has(KeywordTable.LongRange);
            bool sniperKill = ranged && attacker.Has("sniper") && targetDied;
            if (!noCounter && !sniperKill && target.Attack > 0)
            {
                int back = Hurt(ctx, attacker, target.Attack, target.Name);
                ctx.Log($"{target.Name} 反击 {attacker.Name}："
                      + $"{target.Attack} 攻 → 实际 {back} 伤（{attacker.Name} 剩 {attacker.Health}）");
            }
            else if (sniperKill)
            {
                ctx.Log($"{attacker.Name} 的 Sniper：远程击杀 {target.Name} —— **不承受反击**");
            }

            // ⚠️ 伤害走的是 `Hurt` —— 它自己会做「受伤触发 → 离场结算」。
            //    这儿**别再调 CleanupDeaths**：重复调用本身安全，但 Backlash 会发两遍。
            //    （旧版这里是显式 CleanupDeaths(ctx, p, atkSlot) + CleanupDeaths(ctx, tgtP, tgtSlot)）

            // ---- 践踏 Stomp：**溢出伤害**对目标**相邻随机一个**敌方单位造成 ----
            //      规则书 :213「攻击时，溢出伤害对目标相邻随机敌方单位造成」；
            //      原版 `rule_core.gd:4372` 的判据 + `_stomp_splash:4489` 的实现，逐条照抄：
            //        ① 目标**被打死**了（没死就谈不上「溢出」）
            //        ② `dealt > 打之前的血量`（护盾全挡 / 无敌时 dealt 是 0，不成立）
            //        ③ 候选 = 目标格**左右紧邻**那两格里的单位（**不排除督军** —— 原版没排除）
            //        ④ 候选里**随机**挑一个（走 `ctx.Rng`，同一局可复现；原版是 `randi()`）
            if (targetDied && attacker.Has("stomp") && dealt > hpBefore)
            {
                var cands = new List<int>();
                for (int off = -1; off <= 1; off += 2)
                {
                    int adj = tgtSlot + off;
                    if (!BoardSpec.IsValid(adj)) continue;
                    var au0 = ctx.Players[tgtP].Board[adj];
                    if (au0 != null && au0.IsAlive) cands.Add(adj);
                }
                if (cands.Count > 0)
                {
                    int pickSlot = cands[ctx.Rng.Next(cands.Count)];
                    var au = ctx.Players[tgtP].Board[pickSlot];
                    int excess = dealt - hpBefore;
                    int sd = Hurt(ctx, au, excess, attacker.Name + " 的 Stomp");
                    ctx.Log($"Stomp：{attacker.Name} 的 {excess} 点溢出伤害溅到相邻的 {au.Name}"
                          + $"（实际 {sd}，剩 {au.Health}）");
                }
            }

            // ---- 爆裂 X：攻击时对目标**相邻的敌方单位**造成 X 伤害（规则书 :170；原版 `:4348`）----
            //      ⚠️ 只溅射**部队**，不溅射督军（原版那儿写着 `au.is_warlord: continue`）
            if (attacker.Has("blast"))
            {
                int blast = attacker.KwValue("blast");
                for (int off = -1; off <= 1; off += 2)
                {
                    int adj = tgtSlot + off;
                    if (!BoardSpec.IsValid(adj)) continue;
                    var au = ctx.Players[tgtP].Board[adj];
                    if (au == null || au.IsWarlord || !au.IsAlive) continue;
                    int bd = Hurt(ctx, au, blast, attacker.Name + " 的 Blast");
                    ctx.Log($"Blast {blast}：溅射 {au.Name} {bd} 伤（剩 {au.Health}）");
                }
            }

            // ---- 震荡：**被本单位攻击的单位获得眩晕**（规则书 :177；原版 `:4363`）----
            //      原版只在**目标没死**时施加
            if (!targetDied && target.IsAlive && attacker.Has("concussion"))
            {
                target.IsStunned = true;
                ctx.Log($"{target.Name} 被 {attacker.Name} 打晕了（Concussion）");
            }

            // ---- 攻击之后的触发：规则书 :208 / :214，都写着「（本单位存活时）」----
            // 存活判据要连**还在不在场上**一起看 —— 督军血 ≤ 0 时仍占着槽 4，但它已经不算活着了
            if (attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
            {
                // Slay（斩杀）：「攻击并**摧毁单位**后触发能力」。督军不是「被摧毁」，所以不算
                if (!target.IsWarlord && !target.IsAlive)
                    FireTriggerAt(ctx, attacker, KeywordTable.Slay, p, atkSlot);

                // Strike（猛击）：「攻击后触发能力」—— 打没打死都算。放在斩杀之后：
                // 先结算「干掉了」这件更具体的事，再结算「攻击过了」这件泛化的事
                if (attacker.IsAlive && ctx.Players[p].Board[atkSlot] == attacker)
                    FireTriggerAt(ctx, attacker, KeywordTable.Strike, p, atkSlot);
            }

            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        /// <summary>
        /// 通用伤害结算。（rule_core._damage_unit）
        ///
        /// **顺序照原版（`rule_core.gd:4406` 的函数头写着）**：
        ///   `Shield → Invulnerable → Vulnerable/护甲修正 → 扣血`
        ///   ① `Shield` 全挡（不受伤害）
        ///   ② `Invulnerable` **免疫伤害**（规则书 :190「无法被伤害或摧毁」）
        ///   ③ `Vulnerable X` **多加 X 点**（⚠️ 名字容易看反 —— 它是**加伤**，原版 `:4418`）
        ///   ④ `Armour X` 减免，**最低 1**（不是 0）—— 且原版注明这是**任何来源**的伤害都减（`:4420`）
        /// 返回实际扣血量。
        ///
        /// ⚠️ 会**造成伤害**的地方请用 <see cref="Hurt"/>，别直接调这个 ——
        ///    它少了「受伤触发」和「离场结算」两步，漏掉就会出现「血是负的但人还在场上」。
        /// </summary>
        public static int ApplyDamage(BattleContext ctx, UnitState u, int dmg, string source)
        {
            if (u.HasShield)
            {
                u.HasShield = false;
                ctx.Log($"{u.Name} 的 Shield 挡下了 {source} 的伤害");
                // 挡下也发事件（Amount = 0）：画面上「盾碎了」也要有反馈，
                // 而「掉血了」是另一回事 —— 旧表现层靠对比血量，这两件事根本分不开
                EmitUnit(ctx, EvtKind.Hit, u, 0);
                return 0;
            }

            // **无敌**：免疫伤害（规则书 :190；原版 `_damage_unit:4414` 直接 return 0）。
            // 挡下也发事件（Amount = 0）—— 和 Shield 同理，表现层要能看到「打不动」
            if (u.Has("invulnerable"))
            {
                ctx.Log($"{u.Name} 有 Invulnerable —— {source} 的 {dmg} 点伤害被完全挡下");
                EmitUnit(ctx, EvtKind.Hit, u, 0);
                return 0;
            }

            int actual = dmg;
            // **易伤 X**：受到伤害 **+X**（原版 `:4418`）。⚠️ 是加伤，别看成减伤
            if (u.Has("vulnerable")) actual += u.KwValue("vulnerable");
            if (u.Armor > 0) actual = Math.Max(1, actual - u.Armor);

            u.Health -= actual;
            EmitUnit(ctx, EvtKind.Hit, u, actual);
            return actual;
        }

        /// <summary>
        /// **会造成伤害的地方的唯一入口**：扣血 → 受伤触发（Penitence）→ 死了就离场（Death / Backlash）。
        /// </summary>
        /// <param name="killer">谁干的这一下（用于**猎杀标记** —— 它要把击杀者的督军治回来）。
        /// `-1` = 无来源（星辰镖 / 爆炸 / 反击…都不是「击杀者」）。</param>
        static int Hurt(BattleContext ctx, UnitState u, int amount, string source, int killer = -1)
        {
            // 已经死了的不再挨第二遍。
            // 什么时候会走到这：攻击者先被对方的**忏悔**打死了，回来还要结算反击 ——
            // 不给这一条的话，会往一个已经不在场上的单位发一条 Player/Slot 全是 -1 的 Hit 事件
            if (u == null || !u.IsAlive) return 0;

            int dealt = ApplyDamage(ctx, u, amount, source);

            // Penitence（忏悔）：「受到伤害但未死亡时触发效果」—— 规则书 :196。
            // ⚠️ 只有**真掉血**才算受伤（Shield 全挡 = 没受伤，Armour 也只可能减到最低 1，不会变 0）
            if (dealt > 0 && u.IsAlive) FireTriggerOnBoard(ctx, u, KeywordTable.Penitence);

            RemoveIfDead(ctx, u, killer);
            return dealt;
        }

        /// <summary>死了就从棋盘上拿掉（督军除外 —— 它留在槽 4，胜负交给 <see cref="CheckWinner"/>）</summary>
        static void RemoveIfDead(BattleContext ctx, UnitState u, int killer = -1)
        {
            int owner, slot;
            if (!FindUnit(ctx, u, out owner, out slot)) return;
            CleanupDeaths(ctx, owner, slot, killer);
        }

        /// <summary>
        /// 找这个单位在**谁的第几格**。不在场上返回 false（owner/slot 置 -1）。
        ///
        /// 为什么不给 `UnitState` 加个「我在第几格」的字段：格位是**棋盘的事**，单位状态是**单位的事**，
        /// 同一件事记两份早晚不同步 —— 这工程踩过一模一样的坑（见 `资料/规则引擎_进度与交接.md` 第五节）。
        /// 一局最多 18 个格子，扫一遍不值当省。
        /// </summary>
        static bool FindUnit(BattleContext ctx, UnitState u, out int owner, out int slot)
        {
            if (u != null)
            {
                for (int p = 0; p < 2; p++)
                {
                    var b = ctx.Players[p].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        if (b[s] != u) continue;
                        owner = p; slot = s; return true;
                    }
                }
            }
            owner = -1; slot = -1; return false;
        }

        /// <summary>按「单位 → 它在谁的第几格」发一条事件（不在场上就带 -1 的格位，表现层会跳过）</summary>
        static void EmitUnit(BattleContext ctx, EvtKind kind, UnitState u, int amount)
        {
            int owner, slot;
            FindUnit(ctx, u, out owner, out slot);
            ctx.Emit(kind, owner, slot, u != null ? u.Name : null, amount: amount);
        }

        /// <summary>
        /// 生命归零的单位离场。督军不离场（留在槽 4），胜负交给 CheckWinner
        /// </summary>
        /// <param name="killer">击杀方（0/1）。`-1` = 无来源。**猎杀标记要用它**。</param>
        static void CleanupDeaths(BattleContext ctx, int p, int slot, int killer = -1)
        {
            if (!BoardSpec.IsValid(slot)) return;

            var ps = ctx.Players[p];
            var u = ps.Board[slot];
            if (u == null || u.IsAlive) return;
            // ---- 猎杀标记：**带标记的敌方部队被摧毁时** ----
            //   「对敌方督军造成伤害并治疗我方督军，数值 = 其标记数」（规则书 :189；原版 `rule_core.gd:4562`）
            //   ⚠️ 三个细节照原版：
            //     ① 督军**也算** —— 原版只排除了「被摧毁的这一个是督军」，清理标记的那段没有排除督军；
            //     ② 治疗的是**击杀者的督军**，不是击杀者本人；
            //     ③ 只有**敌方**单位会被打上标记（卡面都写 `give Hunt Mark to an enemy troop`），
            //        所以这里再判一次 killer != p，免得自己人误伤时触发。
            if (u.Has("huntmark") && killer >= 0 && killer != p)
            {
                int marks = u.KwValue("huntmark");
                var enemyW = ctx.Players[p].Warlord;          // 标记单位的**主人**的督军 → 挨打
                var killerW = ctx.Players[killer].Warlord;    // 击杀者的督军 → 回血
                if (enemyW != null)
                {
                    int dmg = ApplyDamage(ctx, enemyW, marks, "Hunt Mark");
                    EmitUnit(ctx, EvtKind.Hit, enemyW, dmg);
                    ctx.Log($"Hunt Mark {marks}：{ps.Name} 的督军 {enemyW.Name} 挨 {dmg} 伤"
                          + $"（剩 {enemyW.Health}）");
                }
                if (killerW != null)
                {
                    int before = killerW.Health;
                    killerW.Health = System.Math.Min(killerW.Health + marks, killerW.MaxHealth);
                    int healed = killerW.Health - before;
                    if (healed > 0) EmitUnit(ctx, EvtKind.Hit, killerW, -healed);
                    ctx.Log($"Hunt Mark {marks}：{ctx.Players[killer].Name} 的督军 {killerW.Name}"
                          + $" 回 {healed} 血（{before} → {killerW.Health}）");
                }
                CheckWinner(ctx);
            }


            if (u.IsWarlord)
            {
                ctx.Log($"{ps.Name} 的督军 {u.Name} 倒下");
                ctx.Emit(EvtKind.Death, p, slot, u.Name);
                return;
            }

            ps.Board[slot] = null;
            ps.Discard.Add(u.Card);
            // 阵亡登记（`Choose a … that died this game / this battle / since your last turn`
            // 的候选来源）—— 与 `Discard` **同时**写，取走时也**同时**移除
            // （`BattleContext.TakeFromGraveyard`，一条规则只写一处）。
            // 督军在上面那条 `if (u.IsWarlord) … return` 里已经返回了，**不会**进这张表。
            ctx.DeadUnits.Add(new DeadUnit { Card = u.Card, Owner = p, DeathTurn = ctx.Turn });
            ctx.DiedThisTurn++;      // `For each one that dies …` 按它计数（回合开始清零）
            ctx.Log($"{ps.Name} 的 {u.Name} 阵亡，进弃牌堆");
            // 先发 Death 再结算反噬：表现层要**趁格位还有意义的时候**播阵亡特效
            ctx.Emit(EvtKind.Death, p, slot, u.Name);

            // Backlash（反噬）：「单位死亡时触发效果」—— 规则书 :169（「被摧毁时的触发效果立即结算」）。
            // ⚠️ 单位**已经不在棋盘上了**，所以得把格位显式传进去 ——
            //    它既决定特效播在哪，也是「这张卡死在哪」的唯一记录
            FireTriggerAt(ctx, u, KeywordTable.Backlash, p, slot);
        }

        // ==================================================================
        //  技能与触发（2026-09-12 增补）
        //
        //  两类东西，共用同一套「效果」结算：
        //    · **主动技能**（`Ability:`）—— 玩家/AI 主动放，花掉本单位一次行动
        //    · **触发效果**（Rally / Strike / Slay / Backlash / Penitence）—— 时机到了自动放
        //  两者都会往 `ctx.Signals` 发一条事件，表现层据此播那两类特效。
        // ==================================================================

        /// <summary>
        /// 这个单位现在能不能**开始**发动技能 —— 只看它自己：在不在、有没有技能、行动过没有。
        /// **不判目标。**
        ///
        /// 为什么要和 <see cref="CanUseAbility"/> 分开：要选目标的技能，表现层得先
        /// 「进入选目标状态、点亮合法目标」，那一步还没选呢。拿要求目标的判据去问，
        /// 只会得到 `ErrTarget`，于是永远进不了选目标状态（踩过，见 BattleScene 的技能用例）。
        /// </summary>
        public static int CanStartAbility(BattleContext ctx, int p, int slot)
        {
            if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;
            if (!BoardSpec.IsValid(slot)) return RuleCodes.ErrNotUnit;

            var u = ctx.Players[p].Board[slot];
            if (u == null) return RuleCodes.ErrNotUnit;
            if (!u.HasAbility) return RuleCodes.ErrNoAbility;
            if (u.Exhausted) return RuleCodes.ErrExhausted;
            if (u.IsStunned) return RuleCodes.ErrStunned;
            return RuleCodes.OK;
        }

        /// <summary>
        /// 能不能发动技能，**并且**打中 <paramref name="targetSlot"/>。
        /// 不需要选目标的技能（治疗/抽牌/打督军）传不传 targetSlot 都一样。
        ///
        /// ⚠️ **技能不是攻击**，所以攻击目标那三条限制里只有一条适用：
        ///
        /// | 关键词 | 管不管技能 | 出处 |
        /// |---|---|---|
        /// | 先锋 Vanguard | **不管** —— 原文是「其他单位不能被选为**攻击目标**」 | 规则书 :223 |
        /// | 飞行 Flying | **不管** —— 原文是「只能被…以**近战攻击**选中」 | 规则书 :188 |
        /// | 潜行 Stealth | **管** —— 原文是「不能被**任何方式**选中」 | 规则书 :211 |
        /// | 无法攻击 Can't Attack | **不管** —— 只禁攻击。0 攻单位能放技能，正是它的价值 | 规则书 :174 |
        /// </summary>
        /// <param name="targetSlot">技能目标需要选单位时才用（`EffectTargets.NeedsPick`）</param>
        public static int CanUseAbility(BattleContext ctx, int p, int slot, int targetSlot = -1)
        {
            int code = CanStartAbility(ctx, p, slot);
            if (code != RuleCodes.OK) return code;

            if (!EffectTargets.NeedsPick(ctx.Players[p].Board[slot].Ability.Target)) return RuleCodes.OK;

            if (!BoardSpec.IsValid(targetSlot)) return RuleCodes.ErrTarget;
            var t = ctx.Players[1 - p].Board[targetSlot];
            // 要选**单位**的目标就只能是部队 —— 想打督军的卡，目标那一栏会直接写 `EnemyWarlord`
            if (t == null || t.IsWarlord) return RuleCodes.ErrTarget;
            if (t.Has(KeywordTable.Stealth)) return RuleCodes.ErrTarget;

            return RuleCodes.OK;
        }

        /// <summary>
        /// 发动主动技能：**花掉这个单位本回合的行动**，结算它的效果。
        ///
        /// ⚠️ 技能**不吃反击**（反击是「被攻击」的结果，技能不是攻击）。
        ///    这是主动技能相对普通攻击的立身之本 —— `Ironclad` 2 攻打 2 伤技能，选技能就少挨还手。
        /// </summary>
        public static int UseAbility(BattleContext ctx, int p, int slot, int targetSlot = -1)
        {
            int code = CanUseAbility(ctx, p, slot, targetSlot);
            if (code != RuleCodes.OK) return code;

            var u = ctx.Players[p].Board[slot];
            var spec = u.Ability;
            var chosen = EffectTargets.NeedsPick(spec.Target) ? ctx.Players[1 - p].Board[targetSlot] : null;

            u.Exhausted = true;
            u.AttacksThisTurn++;      // 算「本回合已行动」
            // ⚠️ 技能**不解除 Stealth**：规则书 :211 说的是「**攻击**前不能被选中」，
            //    放技能不是攻击。v1 里没有既带 Stealth 又带技能的单位，这条先按原文来。

            ctx.Emit(EvtKind.Ability, p, slot, u.Name,
                     keyword: KeywordTable.Ability, effect: spec.Source, amount: spec.Amount);
            ctx.Log($"{ctx.Players[p].Name} 的 {u.Name} 发动技能「{spec.Source}」");

            ctx.EffectChain++;
            ResolveEffect(ctx, p, u, spec, chosen);
            ctx.EffectChain--;

            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        /// <summary>
        /// 触发一个效果 —— **所有触发都走这一个口子**（发事件 + 递归保护 + 结算）。
        /// 格位显式传入：反噬这类「单位已经离场」的触发，靠它才知道特效该播在哪。
        /// </summary>
        static bool FireTriggerAt(BattleContext ctx, UnitState u, string keyword,
                                  int owner, int slot, UnitState chosen = null)
        {
            var spec = u != null ? u.Effect(keyword) : null;
            // 没写效果 = 不触发（不是「触发了但没效果」）—— 卡上没这条就不该有反馈
            if (spec == null) return false;

            // 递归保护：Rally 打死人 → 反噬 → 又打到带忏悔的单位 → …… 这类环**天生存在**
            // （规则书 :237「同时触发」那一节讲的就是它），靠深度上限截断
            if (ctx.EffectChain >= BattleContext.MaxEffectChain)
            {
                ctx.Log($"效果链已达 {BattleContext.MaxEffectChain} 层，{u.Name} 的 {keyword} 不再连锁");
                return false;
            }

            // 事件先发：表现层要的是「这一刻、这一格，有个触发发生了」，
            // 效果成不成立（比如对面场上没人可打）是另一回事
            ctx.Emit(EvtKind.Trigger, owner, slot, u.Name,
                     keyword: keyword, effect: spec.Source, amount: spec.Amount);
            ctx.Log($"{u.Name} 触发 {keyword.ToUpperInvariant()}：「{spec.Source}」");

            ctx.EffectChain++;
            ResolveEffect(ctx, owner, u, spec, chosen);
            ctx.EffectChain--;
            return true;
        }

        /// <summary>单位还在场上时触发（自己找格位）</summary>
        static bool FireTriggerOnBoard(BattleContext ctx, UnitState u, string keyword)
        {
            int owner, slot;
            if (!FindUnit(ctx, u, out owner, out slot)) return false;
            return FireTriggerAt(ctx, u, keyword, owner, slot);
        }

        /// <summary>
        /// 结算一个效果。**这是效果的唯一出口** —— 主动技能和触发都调它。
        /// </summary>
        /// <param name="chosen">调用方已选定的目标单位（目标栏写 `EnemyUnit` 时才有）</param>
        static void ResolveEffect(BattleContext ctx, int owner, UnitState source, EffectSpec spec, UnitState chosen)
        {
            string by = source != null ? source.Name : "效果";

            switch (spec.Verb)
            {
                case "damage":
                {
                    var t = ResolveTarget(ctx, owner, source, spec.Target, chosen);
                    if (t == null)
                    {
                        ctx.Log($"{by} 的效果「{spec.Source}」没有合法目标，空过");
                        return;
                    }
                    int dealt = Hurt(ctx, t, spec.Amount, by + " 的效果");
                    ctx.Log($"{by} 的效果「{spec.Source}」对 {t.Name} 造成 {dealt} 伤（剩 {t.Health}）");
                    break;
                }

                case "heal":
                {
                    var t = ResolveTarget(ctx, owner, source, spec.Target, chosen);
                    if (t == null) return;
                    int before = t.Health;
                    // 不超过上限 —— 否则「治疗」会变成变相溢出伤害的储备
                    t.Health = Math.Min(t.MaxHealth, t.Health + spec.Amount);
                    ctx.Log($"{by} 的效果「{spec.Source}」治疗 {t.Name}：{before} → {t.Health}");
                    break;
                }

                case "draw":
                    // ⚠️ `Draw(ctx, p)` 一次只抽 1 张（和 rule_core 一致），数量靠自己循环
                    for (int i = 0; i < spec.Amount && !ctx.IsOver; i++) Draw(ctx, owner);
                    ctx.Log($"{by} 的效果「{spec.Source}」抽了 {spec.Amount} 张");
                    break;
            }
        }

        /// <summary>
        /// 效果的目标是谁。
        ///
        /// ⚠️ `EnemyUnit` 在没有「选目标」这一步的**触发类**效果里，取敌方**槽号最小**的活部队 ——
        ///    **定死规则，不掷骰**：种子只该影响洗牌，同一局必须永远可复现。
        ///    对面场上一个部队都没有时效果**空过**，不自动改打督军（那是替玩家做决定）。
        /// </summary>
        static UnitState ResolveTarget(BattleContext ctx, int owner, UnitState source, string target, UnitState chosen)
        {
            switch (target)
            {
                case EffectTargets.Self:
                    return source;
                case EffectTargets.OwnWarlord:
                    return ctx.Players[owner].Warlord;
                case EffectTargets.EnemyWarlord:
                    return ctx.Players[1 - owner].Warlord;
                case EffectTargets.EnemyUnit:
                {
                    if (chosen != null && chosen.IsAlive && !chosen.IsWarlord) return chosen;
                    var b = ctx.Players[1 - owner].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                        if (b[s] != null && !b[s].IsWarlord && b[s].IsAlive) return b[s];
                    return null;
                }
            }
            return null;
        }

        // ==================================================================
        //  胜负
        // ==================================================================

        /// <summary>
        /// 投降（原版 `BattleResult.Forfeit`）：**立刻**判对方胜，不看督军血量。
        ///
        /// ⚠️ 和 `CheckWinner` 的关系：那边是「督军倒下才算」，这边是玩家自己认输 ——
        ///    所以**不复用** `CheckWinner`（它见到两边都没倒会返回 0，等于投降无效）。
        ///    判过了就不再改（和 `CheckWinner` 一样：**胜负只判一次**）。
        /// 单机没有对手可以投降（AI 不投降），所以这条路只有玩家走得到。
        /// </summary>
        public static int Forfeit(BattleContext ctx, int player)
        {
            if (ctx.Winner != 0) return ctx.Winner;          // 已经结束了，投降不作数
            if (player != 0 && player != 1) return 0;        // 越界就什么都不做（调用方的问题）
            ctx.ForfeitedBy = player;
            ctx.Winner = player == 0 ? 2 : 1;
            ctx.Log($"{ctx.Players[player].Name} 投降 —— {ctx.Players[1 - player].Name} 获胜");
            return ctx.Winner;
        }

        /// <summary>0 = 进行中，1/2 = 该方胜，3 = 平局。（rule_core.check_winner）</summary>
        public static int CheckWinner(BattleContext ctx)
        {
            if (ctx.Winner != 0) return ctx.Winner;   // 已经判过了 —— 别再刷日志

            bool d0 = ctx.Players[0].IsDefeated;
            bool d1 = ctx.Players[1].IsDefeated;

            if (d0 && d1)
            {
                ctx.Winner = 3;
                ctx.Log("双方督军同时倒下 —— 平局");
            }
            else if (d0)
            {
                ctx.Winner = 2;
                ctx.Log($"{ctx.Players[1].Name} 获胜（{ctx.Players[0].Name} 的督军倒下）");
            }
            else if (d1)
            {
                ctx.Winner = 1;
                ctx.Log($"{ctx.Players[0].Name} 获胜（{ctx.Players[1].Name} 的督军倒下）");
            }
            return ctx.Winner;
        }

        // ==================================================================
        //  诊断
        // ==================================================================

        /// <summary>
        /// 这套牌里**有、但 v1 不结算**的关键词。用来量化「还差多少才像原版」，
        /// 而不是让它们静默失效。
        /// </summary>
        public static List<string> UnimplementedKeywords(IEnumerable<CardDef> cards)
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            if (cards == null) return new List<string>();

            foreach (var c in cards)
            {
                if (c == null) continue;
                foreach (var kv in c.Keywords)
                    if (!KeywordTable.Implemented.Contains(kv.Key)) found.Add(kv.Key);
            }
            return new List<string>(found);
        }

        /// <summary>
        /// 带了效果文字、但**文法解析不出来**的卡，逐条列成「卡名 · 关键词 · 原文」。
        ///
        /// 和 <see cref="UnimplementedKeywords"/> 是两件事：
        ///   前者问「**关键词**实现了没有」，这条问「**这张卡的效果**能不能真的跑」。
        ///   `rally` 已经实现了，但一张原版卡的 `Rally: &lt;一大段中文&gt;` 照样跑不了 ——
        ///   那种卡必须被看见（卡面标 `*`），不能装作它会结算。
        /// </summary>
        public static List<string> UnparsedEffects(IEnumerable<CardDef> cards)
        {
            var found = new List<string>();
            if (cards == null) return found;

            var seen = new HashSet<string>();
            foreach (var c in cards)
            {
                if (c == null) continue;
                foreach (var s in c.UnparsedEffects)
                {
                    string line = c.Name + " · " + s;
                    if (seen.Add(line)) found.Add(line);
                }
            }
            return found;
        }
    }
}
