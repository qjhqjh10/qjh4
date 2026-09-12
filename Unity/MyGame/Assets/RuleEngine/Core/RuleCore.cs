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
        public static BattleContext NewBattle(IList<CardDef> deckA, IList<CardDef> deckB,
                                              int seed = 0, bool shuffle = true)
        {
            var ctx = new BattleContext(seed);
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
            CheckWinner(ctx);
            return ctx;
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
            var p = ctx.ActivePlayer;
            p.TurnCount++;
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
            if (card.Cost > ps.Energy) return RuleCodes.ErrCost;

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

            ps.Energy -= card.Cost;
            ps.Hand.RemoveAt(handIdx);

            // 部署当回合不可行动 —— UnitState 构造出来就是 Exhausted = true
            var unit = new UnitState(card, false);
            ps.Board[slot] = unit;

            ctx.Log($"{ps.Name} 部署 {unit.Name}（{card.Cost} 费，{unit.Attack}/{unit.Health}）"
                  + $"到槽 {slot}，能量剩 {ps.Energy}");
            ctx.Emit(EvtKind.Deploy, p, slot, unit.Name);

            // Rally（集结）：「从手牌部署后触发效果」—— 规则书 :200。
            // ⚠️ 触发在**部署之后**，所以效果里 `Self` 指向的已经是场上这个单位
            FireTriggerOnBoard(ctx, unit, KeywordTable.Rally);
            CheckWinner(ctx);
            return RuleCodes.OK;
        }

        // ==================================================================
        //  攻击
        // ==================================================================

        /// <summary>场上攻击力。近战和远程是两套数值。所有攻击力修正都要走这里，别在调用处直接读字段</summary>
        public static int FieldAttack(BattleContext ctx, int p, UnitState u, bool ranged)
        {
            return ranged ? u.RangedAttack : u.Attack;
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

            int atk = FieldAttack(ctx, p, attacker, ranged);
            if (atk <= 0) return RuleCodes.ErrNoAttack;

            int code = IsValidTarget(ctx, p, atkSlot, tgtP, tgtSlot, ranged);
            if (code != RuleCodes.OK) return code;

            // 攻击者消耗：v1 每回合一次（Blood Thirst 的双击未实现）
            attacker.AttacksThisTurn++;
            attacker.Exhausted = true;
            if (attacker.Has(KeywordTable.Stealth))
            {
                attacker.RemoveKeyword(KeywordTable.Stealth);
                ctx.Log($"{attacker.Name} 攻击后现身（失去 Stealth）");
            }

            var target = ctx.Players[tgtP].Board[tgtSlot];

            // 攻击宣言：**在伤害之前**发 —— 表现层才有「抬手 → 命中」的余地
            ctx.Emit(EvtKind.Attack, p, atkSlot, attacker.Name, ranged: ranged,
                     targetPlayer: tgtP, targetSlot: tgtSlot);

            int dealt = Hurt(ctx, target, atk, attacker.Name);
            ctx.Log($"{attacker.Name} {(ranged ? "远程" : "近战")}攻击 {target.Name}："
                  + $"{atk} 攻 → 实际 {dealt} 伤（{target.Name} 剩 {target.Health}）");

            // 反击：目标用**近战攻击力**反击（不是远程）。只有 Long Range 攻击者免疫
            bool noCounter = ranged && attacker.Has(KeywordTable.LongRange);
            if (!noCounter && target.Attack > 0)
            {
                int back = Hurt(ctx, attacker, target.Attack, target.Name);
                ctx.Log($"{target.Name} 反击 {attacker.Name}："
                      + $"{target.Attack} 攻 → 实际 {back} 伤（{attacker.Name} 剩 {attacker.Health}）");
            }

            // ⚠️ 伤害走的是 `Hurt` —— 它自己会做「受伤触发 → 离场结算」。
            //    这儿**别再调 CleanupDeaths**：重复调用本身安全，但 Backlash 会发两遍。
            //    （旧版这里是显式 CleanupDeaths(ctx, p, atkSlot) + CleanupDeaths(ctx, tgtP, tgtSlot)）

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
        /// Shield 先挡 → Armour X 减免（**最低 1**，不是 0）→ 扣血。返回实际扣血量。
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

            int actual = dmg;
            if (u.Armor > 0) actual = Math.Max(1, actual - u.Armor);

            u.Health -= actual;
            EmitUnit(ctx, EvtKind.Hit, u, actual);
            return actual;
        }

        /// <summary>
        /// **会造成伤害的地方的唯一入口**：扣血 → 受伤触发（Penitence）→ 死了就离场（Death / Backlash）。
        /// </summary>
        static int Hurt(BattleContext ctx, UnitState u, int amount, string source)
        {
            // 已经死了的不再挨第二遍。
            // 什么时候会走到这：攻击者先被对方的**忏悔**打死了，回来还要结算反击 ——
            // 不给这一条的话，会往一个已经不在场上的单位发一条 Player/Slot 全是 -1 的 Hit 事件
            if (u == null || !u.IsAlive) return 0;

            int dealt = ApplyDamage(ctx, u, amount, source);

            // Penitence（忏悔）：「受到伤害但未死亡时触发效果」—— 规则书 :196。
            // ⚠️ 只有**真掉血**才算受伤（Shield 全挡 = 没受伤，Armour 也只可能减到最低 1，不会变 0）
            if (dealt > 0 && u.IsAlive) FireTriggerOnBoard(ctx, u, KeywordTable.Penitence);

            RemoveIfDead(ctx, u);
            return dealt;
        }

        /// <summary>死了就从棋盘上拿掉（督军除外 —— 它留在槽 4，胜负交给 <see cref="CheckWinner"/>）</summary>
        static void RemoveIfDead(BattleContext ctx, UnitState u)
        {
            int owner, slot;
            if (!FindUnit(ctx, u, out owner, out slot)) return;
            CleanupDeaths(ctx, owner, slot);
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

        /// <summary>生命归零的单位离场。督军不离场（留在槽 4），胜负交给 CheckWinner</summary>
        static void CleanupDeaths(BattleContext ctx, int p, int slot)
        {
            if (!BoardSpec.IsValid(slot)) return;

            var ps = ctx.Players[p];
            var u = ps.Board[slot];
            if (u == null || u.IsAlive) return;

            if (u.IsWarlord)
            {
                ctx.Log($"{ps.Name} 的督军 {u.Name} 倒下");
                ctx.Emit(EvtKind.Death, p, slot, u.Name);
                return;
            }

            ps.Board[slot] = null;
            ps.Discard.Add(u.Card);
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
