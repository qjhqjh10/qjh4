// BattleContext.cs — 一局对战的完整状态
//
// 随机数用 `System.Random(seed)` 而**不是** `UnityEngine.Random` —— 后者有全局状态、
// 会被别处悄悄消耗，对局就没法复现了。同一个种子必须永远得到同一局。
// （特效线吃过这个亏：粒子随机种子没钉死，两遍扫描 60% 的行会变，噪声和要测的改动一样大。）
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 一条**常驻效果**（`BattleContext.PersistentEffects` 的一项）。
    ///
    /// 规则书英文版 `:39-41` 说这类卡要**单独放一摞**在弃牌堆旁 ——
    /// 所以 <see cref="Source"/> 记着是哪张卡、<see cref="Owner"/> 记着是谁打出的。
    /// </summary>
    public class PersistentEffect
    {
        /// <summary>谁打出的 —— 只有**他自己的回合**才触发（`rule_core.gd:434`）</summary>
        public int Owner;
        /// <summary>来源卡（日志与排查用；规则书要求这类卡留档备查）</summary>
        public CardDef Source;
        /// <summary>`turn_start` / `turn_end` / `deploy`（见 <see cref="Trigger"/>）</summary>
        public string Phase;
        /// <summary>触发时要结算的 op（**解析一次存下来**，见 `EffectOp.AtTurnOps`）</summary>
        public List<EffectOp> Ops;
        /// <summary>正文原文（日志要打人话）</summary>
        public string Body;

        /// <summary>
        /// **什么事件让它触发**。`Phase` 与它配对：`Trigger` 说「哪一类事件」，
        /// `Phase` 说「这一类里的哪一刻」。
        ///
        /// · `"turn"`（默认，`turn_start` / `turn_end`）—— 回合起止段，见 <see cref="RuleCore.ResolveAtTurn"/>
        /// · `"deploy"` —— **某个单位被部署上场时**，见 <see cref="RuleCore.ResolveDeploy"/>
        ///
        /// **原版出处**：部署那条路走 `CardScript.ResolveUnitSummoned`（`CardScript__ResolveUnitSummoned.c:33`）
        /// → `OnTrigger(AbilityTrigger.OtherUnitSummoned = 190, …)`；广播在
        /// `BattleManagerSupport__BroadcastUnitSummoned.c` —— **先自己 `:23`，再场上每张牌 `:41`，
        /// 再当前回合方手牌 `:53`，再另一方手牌 `:65`**（手牌也会收到，所以「手里的牌盯着部署事件」是原版就有的）。
        ///
        /// ⚠️ **不是 `HandEffect`**：`HandEffect` 是 `AbilityLogic.handBuff` 的字段、配
        ///    `AbilityEffect.buffHand = 50`，那是**技能**用的容器（2026-09-13 查证，见 `资料/常驻效果_数据与设计.md`）。
        /// </summary>
        public string Trigger = "turn";

        /// <summary>
        /// 这条常驻效果**作用在哪种卡上**（`Trigger == "deploy"` 时用）。
        /// `null` / 空 = 不筛。见 <see cref="CardCriteria"/>。
        /// </summary>
        public CardCriteria Criteria;

        public override string ToString()
        {
            return (Source != null ? Source.Name : "?") + "@" + Phase;
        }
    }

    /// <summary>
    /// 一个**阵亡的部队**（`BattleContext.DeadUnits` 的一项）。
    ///
    /// 为什么要单独一张表而不是查 `Discard`：见 `BattleContext.DeadUnits` 的注释 ——
    /// `Discard` 混装了打出的战术卡，而且它没有**死亡时间**。
    /// 「自你上个回合之后死的」这个窗口全靠 <see cref="DeathTurn"/> 划。
    /// </summary>
    public class DeadUnit
    {
        /// <summary>死掉的那个单位是哪张卡（`UnitState.Card`，和牌库/弃牌堆里是**同一个对象**）</summary>
        public CardDef Card;
        /// <summary>它属于哪一方（`Choose a **friendly** troop that died …` 只在自己这边挑）</summary>
        public int Owner;
        /// <summary>死在第几回合（全局 `ctx.Turn`）。与 <see cref="PlayerState.LastTurnStartMark"/> 比出窗口</summary>
        public int DeathTurn;

        public override string ToString() { return (Card != null ? Card.Name : "?") + "@T" + DeathTurn; }
    }

    /// <summary>一条费用修正。`Key` = 卡名归一化（`CreatePool.Norm`），`"*"` = 不限卡名。</summary>
    public class CostMod
    {
        public int Player;
        public string Key;
        /// <summary>负数 = 降价</summary>
        public int Delta;
        /// <summary>到哪一回合为止；`-1` = 永久</summary>
        public int ExpireTurn = -1;

        /// <summary>
        /// **作用在哪些卡上**（`null` / 空 = 只看 <see cref="Key"/> 的卡名）。
        ///
        /// 原版没有「费用修正列表」这种东西 —— 费用修正就是挂在卡上的一个 `CardEffect`：
        /// `CoreEffect.costChange`（`CardEffect.cs:64`，内存 `+0x68`）+ `buffType = changeCost(2)`
        /// （`BuffType.cs`）。挂载时**累加**（`CardScript__AddEffect.c:403`），算费用时由
        /// `EntityScript.CurrentCost` **现算**（`EntityScript.cs:74`）—— **不改卡上的费用字段**。
        /// 我们这边对应物就是 `RuleCore.CostOf` 现算，判据挂在这个字段上。
        ///
        /// 例：`Sabotage cards in the enemy hand cost 1 more`（`Underground Network`）——
        /// 原版那个 `HandEffect` 的 `targetCriteria` 带 `spellType = Sabotage(230)`
        /// （`SpellType.cs`），对应这里 `Criteria.KindWord = "sabotage"`。
        /// </summary>
        public CardCriteria Criteria;

        /// <summary>
        /// 这条修正作用在**哪一方的手牌**上；`-1` = 不限（只看 <see cref="Player"/>）。
        ///
        /// 为什么要单开一维：`Sabotage cards in **the enemy hand** cost 1 more` ——
        /// 打出者是 P1，被加价的却是 **P2 手里**的破坏卡。原版靠 `HandEffect.playersAffected`
        /// 表达（`PlayerHand__UpdateCardEffects.c:267` 判 `==10` 是自己、`:303` 判 `!=0x14` 是敌方，
        /// `HandEffect.cs` 的字段之一），**极性写反了整整一档效果就没了**。
        /// 我们这里 `HandOf = 1 - 打出者`。
        ///
        /// ⚠️ 旧的降费（`DoLowerCost`）不设这个字段，走 `-1` —— 行为与改动前完全一致。
        /// </summary>
        public int HandOf = -1;

        public override string ToString()
        {
            return (Delta >= 0 ? "+" : "") + Delta + (Key == "*" ? " 所有牌" : " " + Key)
                 + (Criteria != null && !Criteria.IsEmpty ? "（" + Criteria + "）" : "")
                 + (HandOf >= 0 ? $"（只算 P{HandOf + 1} 手里的）" : "")
                 + (ExpireTurn >= 0 ? $"（到回合 {ExpireTurn}）" : "");
        }
    }

    public class BattleContext
    {
        public readonly PlayerState[] Players = new PlayerState[2];

        /// <summary>全局回合序号（从 1 开始，每换一次边 +1）</summary>
        public int Turn;

        /// <summary>当前行动方：0 / 1</summary>
        public int Active;

        /// <summary>0 = 进行中，1/2 = 该方胜，3 = 平局</summary>
        public int Winner;

        /// <summary>是谁投降的（0/1）。`-1` = 没人投降（正常分出胜负）。
        /// 结算面板要按它换一句话 —— 原版 `BattleResult.Forfeit` 和「督军倒下」是两种结局。</summary>
        public int ForfeitedBy = -1;

        /// <summary>种子化的随机源。洗牌用它 —— 引擎里**只有这一处**随机</summary>
        public readonly Random Rng;

        /// <summary>事件日志。自检断言、调试、将来接 UI 都读它</summary>
        public readonly List<string> Events = new List<string>();

        /// <summary>
        /// **结构化事件流**（谁在哪个格位做了什么）。和 <see cref="Events"/> 的分工：
        ///   · `Events`  —— 给人看的字符串日志，只增不减，用来读和排查
        ///   · `Signals` —— 给**表现层**看的，带格位/卡名/数值，用来决定「在哪播什么特效」
        /// 表现层用 <see cref="DrainSignals"/> 搬走（搬完即空，不用记游标）。
        /// </summary>
        public readonly List<BattleEvent> Signals = new List<BattleEvent>();

        /// <summary>
        /// 效果链深度。Rally 打死人 → Backlash → 又打到带忏悔的单位 → 再触发……
        /// 这类环**天生存在**（规则书 :237「同时触发」那节就是讲它的），
        /// 所以靠深度上限截断，而不是靠「保证不会发生」。超过 <see cref="MaxEffectChain"/> 就不再连锁。
        /// </summary>
        public int EffectChain;

        /// <summary>效果链最多连锁几层</summary>
        public const int MaxEffectChain = 8;

        /// <summary>只留最近 N 条，防长对局把内存吃满</summary>
        public int EventCapacity = 2000;

        /// <summary>
        /// **本回合阵亡了几个单位**。规则书 :189 / :201 那一族「每有 1 个阵亡就…」按它计数
        /// （`For each one that dies, heal 2 …`）。回合开始时清零。
        /// 原版 `rule_core.gd` 里没有这个字段，是照着卡面语义补的。
        /// </summary>
        public int DiedThisTurn;

        /// <summary>
        /// **本次结算**（一次 `ResolveOps`）里抽到的牌。`For each troop drawn …`
        /// 这种「前一句抽了牌、后一句按张数翻倍」的写法靠它。
        ///
        /// 为什么按「本次结算」而不是「本次抽牌」：卡面写的是
        /// `Draw 2 troops.` + `For each troop drawn …` 两条**分句**，
        /// 中间还隔着别的处理 —— 计数窗口得把整张卡罩住。
        /// 由 `ResolveOps` 在入口清零（一张卡的结算就是一次窗口）。
        /// </summary>
        public readonly List<CardDef> DrawnThisResolve = new List<CardDef>();

        /// <summary>
        /// **全卡池** —— `create` 造牌的候选来源（见 <see cref="CreatePool"/>）。
        ///
        /// 为什么要单独传一份：一局对战只看得到双方**牌库**，而造牌是按**阵营 + 兵种**从
        /// **整个卡池**里筛（`Create three Ultramarines Vehicles` —— 那 18 张载具不可能都在牌库里）。
        ///
        /// ⚠️ **null = 这一局没给卡池**。此时造牌**如实报「没有卡池」并什么都不做**，
        ///    绝不退化成「从双方牌库里抽」—— 那会悄悄造出卡面上没有的牌（红线：不许静默失败）。
        /// </summary>
        public IReadOnlyList<CardDef> CardPool;

        public BattleContext(int seed)
        {
            Rng = new Random(seed);
        }

        public PlayerState ActivePlayer { get { return Players[Active]; } }
        public PlayerState Opponent { get { return Players[1 - Active]; } }

        public bool IsOver { get { return Winner != 0; } }

        /// <summary>
        /// **开局换牌阶段**（原版 `MulliganManager` + `PlayerHand.AddCardsToMulligan`；
        /// 规则书 :46「换牌（Mulligan）| 可弃回任意起手牌后重洗补抽」）。
        ///
        /// `NewBattle(openMulligan: true)` 时开、`RuleCore.EndMulligan` 关。
        /// ⚠️ **换牌只能在这个阶段做**（`RuleCore.Mulligan` 会检查它）—— 对局开打之后再换就是改牌堆。
        /// </summary>
        public bool MulliganOpen;

        /// <summary>
        /// **上一句效果打中的那个单位** —— 供文本里的 `it` / `them` / `the target` 指代。
        ///
        /// 为什么放在 ctx 上：这是**跨分句**的上下文（`Deal 3 damage to an enemy. If it dies, draw a card.`
        /// 两个分句要串起来），不是某一个单位的属性。原版用 `rule_core` 的 `it_target` / `_last_pick`
        /// 两个变量记同一件事（`:2730`），我们合成一个。
        /// ⚠️ 它**可能已经死了/已经不在场上**（`If the target dies` 就是要判这个），所以取用方必须判活。
        /// </summary>
        public UnitState LastTarget;

        /// <summary>
        /// **上一条效果影响到的**那一批单位**（`Deal 2 damage to all units and give **them** Blind`）。
        ///
        /// 和 <see cref="LastTarget"/> 的分工：`it` / `the target` 指**一个**，`them` 指**一批**。
        /// 只记 `LastTarget` 会让 `give them X` 落到「己方全体」那个近似上（原版 `rule_core.gd:3137`
        /// 自己标着「近似」）—— `Deploy 3 Grot and give **them** Vanguard` 就会给错人。
        /// 由 <see cref="RuleCore.ResolveOps"/> 那条路（`ResolveTargets` / `DoDeploy`）写。
        /// </summary>
        public readonly List<UnitState> LastTargets = new List<UnitState>();

        /// <summary>
        /// **费用修正**（`Lower the cost of … by N` / `They cost N less`）。
        ///
        /// 为什么要有它：`CardDef` 是**不可变的共享对象**（整个卡池共用一份），
        /// 把费用改在它身上会污染所有同名卡、连卡组编辑器都跟着变。
        /// 所以费用修正挂在对局上，由 <see cref="RuleCore.CostOf"/> 现算。
        ///
        /// ⚠️ **按卡名匹配，不是按卡实例**：手里两张同名卡会**一起**降价。
        ///    原版能区分实例（每张卡有 GID），我们手里只有 `CardDef` 引用 —— 如实标着这个近似。
        /// </summary>
        public readonly List<CostMod> CostMods = new List<CostMod>();

        /// <summary>
        /// **上一条效果造出来的那些卡** —— 供 `They cost 1 less` / `It costs 2 less` 指代
        /// （原版用 `ctx.last_created` 记同一件事，见 `rule_core.gd:3002`）。
        /// </summary>
        public readonly List<CardDef> LastCreated = new List<CardDef>();

        /// <summary>
        /// **上一次选牌挑中的那张卡**（`Choose a …`）—— 原版 `rule_core.gd:1152` 的 `ctx["_chosen_card"]`。
        ///
        /// 和 <see cref="LastCreated"/> 的分工：选牌时**两个都写**（原版 `:1151-1152` 就是同时写
        /// `last_created` 和 `_chosen_card`），所以 `Lower its cost by N` / `It costs N less`
        /// 那条走 `LastCreated` 的 `(指代上一张)` 路径**不用改**就通了。
        /// 这个字段单独留一份，是给**「复制选中那张」**（`create a copy of it`）用的 ——
        /// 那种句子的指代对象在**手牌/牌库里**，不在场上，`LastTarget`（`UnitState`）够不着。
        /// </summary>
        public CardDef LastChosenCard;

        /// <summary>
        /// **本局阵亡的部队**（`Choose a friendly troop that died this game / this battle /
        /// since your last turn` 的候选来源）—— 原版 `rule_core.gd:1004-1024` 的 `dead` 域。
        ///
        /// ⚠️ **为什么不复用 `PlayerState.Discard`**：`Discard` 是**单位与战术混装**的
        ///    （打出的战术卡也进那儿，见 `RuleCore.PlayTactic`），拿它当墓地会挑出「已经打掉的战术卡」。
        ///    而且它还缺**死亡发生在第几回合**这个信息 ——
        ///    `since your last turn` 的窗口正是靠它划的。
        ///
        /// 督军**不进这张表**（`RuleCore.KillUnit` 对督军提前 return）—— 督军不能复活。
        /// </summary>
        public readonly List<DeadUnit> DeadUnits = new List<DeadUnit>();

        /// <summary>
        /// **常驻效果**（规则书英文版 `:39-41`「Persistent Effects」，中文版 `:32`）：
        /// 写「For the rest of this battle」的卡**被弃置后依然生效**，规则书要求把这类卡
        /// **单独放一摞**在弃牌堆旁边备查 —— 也就是说**卡本身就是效果来源**，
        /// 所以这里存的是「谁 + 来源卡 + 触发时机 + 正文」，不是把效果烘成一个数。
        ///
        /// 由 `EffectResolver.DoPersist` 在**打出时**登记，`RuleCore.ResolveAtTurn` 在每个
        /// 回合的起/止按 `Owner == 当前行动方` 消费（`rule_core.gd:431-435` 同一口径）。
        /// </summary>
        public readonly List<PersistentEffect> PersistentEffects = new List<PersistentEffect>();

        /// <summary>
        /// **正在结算的是哪张卡**（由 `ResolveOps` 在入口写）。
        /// 只有「常驻效果登记」用它 —— 规则书要求这类卡单独留档，得记来源。别处不看。
        /// </summary>
        public CardDef PlayingCard;

        /// <summary>
        /// 从墓地取走一张（`Choose a … that died … and deploy/hand/deck it`）。
        ///
        /// **同一条规则只写这一处**：`Discard`（供 `DeployFrom == "graveyard"` 那条老路）与
        /// `DeadUnits`（本表）**必须一起移除** —— 只移一边的话，同一张卡能被复活两次。
        /// </summary>
        public void TakeFromGraveyard(int owner, CardDef card)
        {
            if (card == null) return;
            for (int i = DeadUnits.Count - 1; i >= 0; i--)
                if (DeadUnits[i].Owner == owner && ReferenceEquals(DeadUnits[i].Card, card))
                    DeadUnits.RemoveAt(i);
            var disc = Players[owner].Discard;
            for (int i = disc.Count - 1; i >= 0; i--)
                if (ReferenceEquals(disc[i], card)) disc.RemoveAt(i);
        }

        public void Log(string message)
        {
            Events.Add(message);
            if (Events.Count > EventCapacity) Events.RemoveRange(0, Events.Count - EventCapacity);
        }

        // ==================================================================
        //  事件流
        // ==================================================================

        /// <summary>发一条事件。引擎里**所有** <see cref="Signals"/> 都从这儿进。</summary>
        public void Emit(BattleEvent e)
        {
            if (e == null) return;
            e.Turn = Turn;
            Signals.Add(e);
            AppendLog(e);
        }

        /// <summary>
        /// **留档**的战斗日志：一行 = 一个动作。和 <see cref="Signals"/> 的区别是
        /// **它不会被搬走** —— `Signals` 是给表现层「这一刻播什么特效」的，`DrainSignals` 一搬就空；
        /// 日志面板要的是「这一局都发生过什么」，所以在这儿再存一份。
        /// ⚠️ 来源就是 <see cref="Emit"/> —— **同一份事件流**，不另写一套（两处写同一条规则迟早不一致）。
        /// </summary>
        public readonly List<BattleEvent> ActionLog = new List<BattleEvent>();

        /// <summary>日志最多留多少行（长对局别把内存吃满）</summary>
        public int LogCapacity = 400;

        /// <summary>把事件收进留档日志。两条合并规则：
        /// ① **出单位卡会连着发 `Play` + `Deploy`** —— 只留 `Play`（不然每张单位卡都重复一行）；
        ///    单独发的 `Deploy`（效果召唤来的）照留。
        /// ② 连续同一条 `Hit` 不合并（打了几次就是几次，原版日志也是逐条列）。</summary>
        void AppendLog(BattleEvent e)
        {
            if (e.Kind == EvtKind.Deploy && ActionLog.Count > 0)
            {
                var last = ActionLog[ActionLog.Count - 1];
                if (last.Kind == EvtKind.Play && last.Player == e.Player && last.CardId == e.CardId) return;
            }
            ActionLog.Add(e);
            if (ActionLog.Count > LogCapacity) ActionLog.RemoveRange(0, ActionLog.Count - LogCapacity);
        }

        /// <summary>按字段发一条（不传的字段保持默认值）</summary>
        public void Emit(EvtKind kind, int player, int slot, string cardId,
                         string keyword = null, string effect = null,
                         int amount = 0, bool ranged = false,
                         int targetPlayer = -1, int targetSlot = -1,
                         string targetCardId = null)
        {
            Emit(new BattleEvent
            {
                Kind = kind,
                Player = player,
                Slot = slot,
                CardId = cardId,
                Keyword = keyword,
                Effect = effect,
                Amount = amount,
                Ranged = ranged,
                TargetPlayer = targetPlayer,
                TargetSlot = targetSlot,
                TargetCardId = targetCardId,
            });
        }

        /// <summary>
        /// 把待播的事件**搬走并清空**（不是拷贝 —— 搬完引擎这边就没了，不会重复播）。
        /// 返回搬走了几条。
        /// </summary>
        public int DrainSignals(List<BattleEvent> into)
        {
            if (into == null) { int n = Signals.Count; Signals.Clear(); return n; }
            int count = Signals.Count;
            into.AddRange(Signals);
            Signals.Clear();
            return count;
        }

        /// <summary>丢弃待播事件（不给表现层的路径用，比如「一口气跑完 200 回合」的脚本推进）</summary>
        public void ClearSignals() { Signals.Clear(); }

        /// <summary>诊断用：两边的简况</summary>
        public string Describe()
        {
            return $"回合 {Turn}（P{Active + 1} 行动）  "
                 + $"P1 {Players[0]}  |  P2 {Players[1]}";
        }
    }
}
