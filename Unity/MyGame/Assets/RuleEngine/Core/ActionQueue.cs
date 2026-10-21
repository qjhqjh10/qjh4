// ActionQueue.cs — 原版「四条动作队列」的原语（引擎线 `T03·c` 批 B · 第 1 步）
//
// 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`；汇总见
//       `资料/普查产出_1021/W61_引擎线_T03c批B判据.md` §2.2 / §2.3）：
//   · 原版 `BattleManager` 里有**四条独立的 FIFO 队列**：
//       `actionQueue(+0x350)` · `instantQueue(+0x358)` · `delayedInstantQueue(+0x360)` ·
//       `interruptQueue(+0x368)`（`BattleManager.cs:4535/4537/4539/4541` ·
//       `BattleManager__.ctor.c:97/101/105/109` 按同序初始化）。
//   · 分道：`BattleManager__AddAutoActionToQueue.c` —— 0→`+0x350`(Player) · 1→`+0x358`(Instant) ·
//     2→`+0x368`(Interrupt) · 3→`+0x360`(DelayedInstant)；**其他值 LogError 并丢弃**（⛔ 不 clamp）。
//   · 消费序（`BattleManager__Update.c:1284-1299` / `NextActionInQueue.c:11-28`）：
//       **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**
//     —— 🔴 **不是按枚举数值从小到大排**（3 反而排在 0 前面），所以 `DequeueOrder` 必须**显式写死**。
//   · 同一条道 = 严格 FIFO：入队 `List.Add`（追加队尾，全库没有 `Insert(0, …)`），
//     出队 `get_Item(0)` + `RemoveAt(0)`（`ResolveActionFromQueue.c:42-44`）。
//   · **不抢占**：`resolvingActionFlag(+0x280)` 为真时 `Update` **不泵队**；Interrupt 只是
//     「下一个动作边界上的最高道」，**不会打断正在结算的那一条**（`Update.c:1271-1299`）。
//   · **每拍最多一条**（`Update` 每帧只调一次 `ResolveActionFromQueue`）。
//
// 🔴 **本文件只有【原语】**：`Enqueue` / `TryDequeue` / `Count` / `Resolving`。
//    ⛔ 它**不是**调度器 —— `Core/ActionScheduler.cs` 是另一件东西（DateTime 定时调度），别混。
//    ✅ **2026-10-21 接线批之后**：`RuleCore` / `EffectResolver` 的所有触发**只入队**，
//       泵点是 `Core/ActionPump.cs` 的 `ActionScope` / `PumpActionQueues`
//       （⚠️ 本文件头原来写着「本批不接线，仍走同步递归」—— 那句自接线批起**已不成立**，就地订正）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 原版 `ActionPriority`（`ActionPriority.cs:1-7`）。
    /// 🔴 **数值必须逐字是 0/1/2/3** —— 它是**数据里的字面量**
    /// （`cards_engine.json` 的 `triggerPriority[].priority` 与 `CardAbility.priority` 用的是同一套），
    /// ⛔ 别改成别的顺序（`RuleEngineTest` 的 `T1` 用字面量盯着）。
    /// ⚠️ **枚举数值 ≠ 消费顺序** —— 消费序见 <see cref="ActionQueue.TryDequeue"/>。
    /// </summary>
    public enum ActionPriority
    {
        /// <summary>玩家动作（`remotePlayerAction(36)` / `turnEnd` 那一路）。</summary>
        Player = 0,
        /// <summary>默认道 —— **不写 priority 的 ability 都在这里**（`CardAbility__ctor.c:5` 写 `+0x18 = 1`）。</summary>
        Instant = 1,
        /// <summary>打断道 —— 消费序**第一**（不是按数值第二）。</summary>
        Interrupt = 2,
        /// <summary>延迟瞬发 —— 消费序**最后**（比 Player 还晚）。</summary>
        DelayedInstant = 3,
    }

    /// <summary>
    /// 队列里的**一条动作**（最小可承载形状）。
    ///
    /// 🔴 **为什么先留一个空壳载荷**（`T03·c` 批 B 第 1 步）：原版的「动作」是
    /// `BattleAction` 那一族对象（`ResolveAction` 按 `actionType` 分派）。我们还没有那个类型 ——
    /// 本批只做**队列原语**，所以载荷先放 <see cref="Payload"/>（`object`），后续批把
    /// `ResolveOps` 的入参塞进来、再把 `ResolveAction` 那一跳补齐。
    /// ⚠️ 名字**故意不叫 `Action`** —— 那会与 `System.Action` 撞名（`using System;` 的文件里歧义）。
    /// </summary>
    public sealed class QueuedAction
    {
        /// <summary>它进的是哪条道（`Enqueue` 时按入参回填，出队时与 `TryDequeue` 的 `lane` 一致）。</summary>
        public ActionPriority Priority;

        /// <summary>全局入队序号（1 起、单调递增）—— **排查/断言用**，不参与任何结算判据。</summary>
        public int Seq;

        /// <summary>人话标签（卡名 / 正文 / 触发关键词）—— 只给日志与排查用。</summary>
        public string Label;

        /// <summary>动作载荷。自由形态（排查 / 将来的结构化动作用）。</summary>
        public object Payload;

        /// <summary>
        /// 🆕 2026-10-21（审查 `W61b` 第 3 条）：**这条 ability 的「施放者」** ——
        /// 原版 `BattleAction` 里那一格 `actingCard` 的对应物（我们这边是场上的 `UnitState`）。
        ///
        /// 泵点在出队之后、结算之前拿它复核「施放者还在不在场上」
        /// （判据 → `RuleCore.PumpActionQueues` 里那一段注释）。`null` = 这条动作不关任何场上单位
        /// （战术卡留下的常驻效果 / 手牌里的监听器 / 死亡结转 …）。
        /// </summary>
        public UnitState Actor;

        /// <summary>
        /// 🆕 同上：**出队时要不要复核 <see cref="Actor"/> 还在场上**。
        /// `true` = 原版 `abilityContext = Play(0)`（`CardAbility__IsRightContext.c:20-30` 要求 `IsInPlay`）；
        /// `false`（缺省）= 原版 `InPlayOrDying(40)` 或「没有施放者」—— **一律照跑**。
        /// ⛔ 缺省 `false` 是**故意的**：死亡触发那一族（`Backlash` / `Unstable` / `minionDeath` 记账）
        /// 的施放者**正是刚死的那个人**，判「还在场」会把整条链掐死。
        /// </summary>
        public bool RequireActorInPlay;

        /// <summary>
        /// 🆕 **2026-10-22（`W62` · R2 · Mob「再来一次」）：这一条是【额外触发】那一条**
        /// （= 卡面 `it triggers an additional time` / `it applies the effect twice`）。
        ///
        /// 🔴 **为什么需要**：`ctx.ExtraTriggerDepth` 那道**重入守卫**原来夹在
        /// `RuleCore.FireExtraTriggers` 的`for` 循环外面 —— 那是因为旧的「额外触发」是
        /// **当场结算**的（广播也在当场，守卫拦得住 `DoExtraTrigger` 重新挂额度）。
        /// 触发改入队之后，`ExtraTriggerDepth` 在**入队那一刻**顶起来、入队完就落下，
        /// 而真正会再广播一遍的是**出队时**的那一刻 ⇒ 守卫**完全失效**（额度会被无限重新挂上，
        /// 一路撞到 `MaxPumpedActionsPerDrain`）。
        /// ⇒ 改成随条目走：泵点在 `Execute()` 外面顶/落这个深度（见 `RuleCore.PumpActionQueues`）。
        /// ⛔ 别删这个字段回到「只在 `FireExtraTriggers` 里顶」—— 那条路今天挡不住任何东西。
        /// </summary>
        public bool ExtraTrigger;

        /// <summary>
        /// 🔴 **出队后真正要跑的那段结算**（2026-10-21 接线批加）。
        /// 泵点（<see cref="RuleCore.PumpActionQueues"/>）在 `Resolving = true` 的那一小段里调它一次。
        ///
        /// ⚠️ 它**必须自带全部上下文**（闭包捕获：op 列表、施放者、代词槽…）——
        /// 入队之后到出队之前，`ctx` 上的那些槽可能已经被别的动作改过（原版也是这个形状：
        /// `BattleAction` 把 `actingCard` / `targetCard` / `DamageInfo` 一起打包带走）。
        /// ⚠️ `null` ⇒ 只占位不结算（单测里造纯顺序用例时用），**泵点不报错**。
        /// </summary>
        public System.Action Execute;

        public override string ToString()
            => (Label ?? "?") + "@" + Priority + "#" + Seq;
    }

    /// <summary>
    /// 原版 `BattleManager` 那**四条 FIFO 队列**的等价物（`T03·c` 批 B 第 1 步）。
    ///
    /// 用法（后续批；本批只被单测调用）：
    /// <code>
    /// queue.Enqueue(new QueuedAction { Label = "…" }, ActionPriority.Instant);
    /// if (!queue.Resolving &amp;&amp; queue.TryDequeue(out var lane, out var act)) { …结算… }
    /// </code>
    ///
    /// 🔴 **`TryDequeue` 不看 <see cref="Resolving"/>** —— 原版那道闸在**泵队的那一处**
    /// （`BattleManager__Update.c:1271` 先判 `resolvingActionFlag(+0x280)` **再** `NextActionInQueue`），
    /// 不在 `NextActionInQueue` 里面 ⇒ 本类照原版把闸留给调用方（泵点）。
    /// ⛔ 别在这里加「Resolving 时返回 false」—— 那会让「谁在泵队」这件事多一处口径。
    /// </summary>
    public sealed class ActionQueue
    {
        /// <summary>道数 = 4（`ActionPriority` 的 0..3）。</summary>
        public const int LaneCount = 4;

        /// <summary>
        /// 🔴 **消费序：Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)** ——
        /// 原版 `BattleManager__Update.c:1284-1299` 的**逐字顺序**（`NextActionInQueue.c:11-28` 同）。
        /// ⛔ **不是**按枚举数值排；⛔ 别从 `ActionPriority` 算出来（那样改枚举就会静默改秩序）。
        /// </summary>
        static readonly int[] DequeueOrder = { 2, 1, 3, 0 };

        readonly List<QueuedAction>[] _lanes =
        {
            new List<QueuedAction>(),   // 0 Player
            new List<QueuedAction>(),   // 1 Instant
            new List<QueuedAction>(),   // 2 Interrupt
            new List<QueuedAction>(),   // 3 DelayedInstant
        };

        int _seq;
        int _dropped;

        /// <summary>
        /// 原版 `resolvingActionFlag(+0x280)`：**正在结算一条动作**。为真时泵队那一处**不许**出队
        /// （原版 `Update` 就是不泵）。本类**不自己置位** —— 由结算方置真/置假（后续批）。
        /// </summary>
        public bool Resolving { get; set; }

        /// <summary>四条道里的**动作总数**（原版 `ActionsInQueue` = 四道之和 > 0 或 resolving）。</summary>
        public int Count
        {
            get
            {
                int n = 0;
                for (int i = 0; i < LaneCount; i++) n += _lanes[i].Count;
                return n;
            }
        }

        /// <summary>**被丢弃**的入队次数（非法 priority / 空动作）—— 排查用，⛔ 不是容量。
        /// 原版对非法 priority 是 `LogError` 后丢弃（`AddAutoActionToQueue.c`），这里同口径。</summary>
        public int Dropped { get { return _dropped; } }

        /// <summary>原版 `ActionsInQueue` = 「四道总数 &gt; 0 **或** resolving」。
        /// ⚠️ 与它同名的那条在 `BattleManagerActionsInQueue.c` 里就是这么写的 ——
        /// **正在结算的那一条也算「忙」**（调用方拿它判「这回合还有没有东西要跑」）。
        /// 🔴 2026-10-21（审查 `W61b` 第 11 条）：**今天没有任何调用点** —— 它是按原版名字
        /// 保留的一等 API（「输入层该不该放行」本来该读它，我们走的是「每个动作边界必取空」
        /// 那条路 ⇒ 无可观测差异）。⛔ 别删（删了就没有「忙」这个名字了），
        /// 也⛔ 别拿它去拦输入 —— 那是另一笔要做的事。</summary>
        public bool Busy { get { return Resolving || Count > 0; } }

        /// <summary>某一条道里现在有几条（排查/断言用）。</summary>
        public int CountOf(ActionPriority lane)
        {
            int i = (int)lane;
            return (i >= 0 && i < LaneCount) ? _lanes[i].Count : 0;
        }

        /// <summary>
        /// 入队到 <paramref name="priority"/> 那条道的**队尾**（原版 `List.Add`，全库没有 `Insert(0, …)`）。
        ///
        /// 🔴 **非法 priority（不在 0..3）⇒ `LogError` 并【丢弃】**，返回 `false` —— 原版
        /// `BattleManager__AddAutoActionToQueue` 对非法值也是 `LogError` 后丢弃（判据报告 §2.2）。
        /// ⛔ **不 clamp 到最近的道、不默认 Instant** —— 那会把「数据错」变成「悄悄走错道」。
        /// </summary>
        public bool Enqueue(QueuedAction action, ActionPriority priority)
        {
            int lane = (int)priority;
            if (lane < 0 || lane >= LaneCount)
            {
                _dropped++;
                UnityEngine.Debug.LogError(
                    $"[ActionQueue] 非法 priority={(int)priority}（合法 0..3：Player/Instant/Interrupt/DelayedInstant）"
                    + $"—— 已丢弃动作「{(action != null ? action.Label : "(null)")}」"
                    + "（原版 AddAutoActionToQueue 对非法值也是 LogError 后丢弃）");
                return false;
            }
            if (action == null)
            {
                _dropped++;
                UnityEngine.Debug.LogError("[ActionQueue] Enqueue 收到 null 动作 —— 已丢弃（⛔ 不静默）");
                return false;
            }
            action.Priority = priority;
            action.Seq = ++_seq;
            _lanes[lane].Add(action);        // 队尾
            return true;
        }

        /// <summary>
        /// 按**固定道序**取出一条：`Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)`；
        /// 同一条道内 **FIFO**（取队首、`RemoveAt(0)`）。四道全空 ⇒ `false`。
        ///
        /// ⚠️ 一次只出一条（原版 `Update` **每帧最多一条**）；连续取 = 调用方循环。
        /// ⚠️ **不看 <see cref="Resolving"/>**（判据见类注释）。
        /// </summary>
        public bool TryDequeue(out ActionPriority lane, out QueuedAction action)
        {
            for (int k = 0; k < DequeueOrder.Length; k++)
            {
                int i = DequeueOrder[k];
                var list = _lanes[i];
                if (list.Count == 0) continue;
                action = list[0];
                list.RemoveAt(0);
                lane = (ActionPriority)i;
                return true;
            }
            lane = default(ActionPriority);
            action = null;
            return false;
        }

        /// <summary>清空四条道（**不重置** <see cref="Resolving"/> / <see cref="Dropped"/>）。
        /// 🔴 唯一的调用点 = `RuleCore.PumpActionQueues` 的「触发环硬闸」（`MaxPumpedActionsPerDrain`）——
        ///    正常对局永远走不到。⚠️ 2026-10-21 审查（`W61b` 第 11 条）原文写「`Clear()` 无调用点」，
        ///    已不成立（泵点那一支就是）。⛔ 别在别处顺手调它当「重开」用（一局重开是新建 `BattleContext`）。</summary>
        public void Clear()
        {
            for (int i = 0; i < LaneCount; i++) _lanes[i].Clear();
        }
    }
}
