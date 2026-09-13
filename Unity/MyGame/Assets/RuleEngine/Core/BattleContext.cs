// BattleContext.cs — 一局对战的完整状态
//
// 随机数用 `System.Random(seed)` 而**不是** `UnityEngine.Random` —— 后者有全局状态、
// 会被别处悄悄消耗，对局就没法复现了。同一个种子必须永远得到同一局。
// （特效线吃过这个亏：粒子随机种子没钉死，两遍扫描 60% 的行会变，噪声和要测的改动一样大。）
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>一条费用修正。`Key` = 卡名归一化（`CreatePool.Norm`），`"*"` = 不限卡名。</summary>
    public class CostMod
    {
        public int Player;
        public string Key;
        /// <summary>负数 = 降价</summary>
        public int Delta;
        /// <summary>到哪一回合为止；`-1` = 永久</summary>
        public int ExpireTurn = -1;

        public override string ToString()
        {
            return (Delta >= 0 ? "+" : "") + Delta + (Key == "*" ? " 所有牌" : " " + Key)
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
