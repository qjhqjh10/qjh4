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
    public sealed partial class QueuedAction
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
        /// 🔴🆕 **2026-10-23（`Slay` 就地结算复核）：出队时要不要按原版
        /// `CardScript.IsInPlayOrDying` 复核 <see cref="Actor"/>。**
        ///
        /// 判据（第一权威 = 全量反编译）：`BattleManager__ResolveTriggerSlay.c:116-226` ——
        /// Slay 这条动作出队时只有 `CardScript__IsInPlayOrDying()`（`:8-13`，
        /// `state ∈ {2 inPlay, 15 inPlayTransforming, 3 inPlayAttacking, 17, **5 waitingToDie**}`）
        /// 为真才 `CardScript__TriggerSlay()`，否则**取消这一条**。
        ///
        /// 与 <see cref="RequireActorInPlay"/> 的区别 = **`waitingToDie` 算不算在场**：
        /// 那一档要求活着在场（`Play(0)`，待死 ⇒ 不算），这一档**待死也算**
        /// （`Core/RuleCore.IsOnBoardOrDying`）。⇒ 本条**不比** `RequireActorInPlay` 严，
        /// 它是 Slay 自己的那一道出队复核。
        /// 🔴 **2026-10-24（`F1`）**：两闸**同时为真**时**以本条为准** —— 泵点里那一支旧口径
        /// （`RequireActorInPlay && !UnitInPlay(...)`）现在会先让位（见 `ActionPump.PumpActionQueues`
        /// 的出队复核段）。否则「凶手在反噬里被打死」那一格会被旧的 `Play(0)` 支**先取消**，
        /// 等于把这一条更宽的闸架空（实测 `W58ILBlaster·slay`）。
        ///
        /// ⛔ 缺省 `false`（不判）—— 今天只有 `TryCreditKillCore` 的 Slay 那一跳传 `true`。
        /// </summary>
        public bool RequireActorInPlayOrDying;

        /// <summary>
        /// 🔴🆕 **2026-10-22（引擎线 `W63` · C1）：这条 ability 的原版 `abilityContext`**
        /// （=`CardAbility.abilityContext // +0x14`；六档判据见 `RuleCore.IsRightContext` 的 doc）。
        ///
        /// <para>泵点在出队之后、结算之前按它复核「**载体**还在不在原版要求的区域」：
        /// `0` 活着在场 · `10` 手牌 · `20` 牌库 · `30` 墓地 · `40` 在场（**含待死**）·
        /// `50` 在场或墓地（**不含待死**）。</para>
        ///
        /// <para>🔴 <see cref="CardDef.AbilityContextUnknown"/>（`-1`，**缺省**）= 未知 ⇒
        /// 泵点**回退** <see cref="RequireActorInPlay"/> 那条旧口径（`-1` 是旧卡表 / 这一路还没接线的
        /// 正常状态，**今天所有调用点都是这一档** ⇒ 行为零变化）。
        /// ⛔ **别把「未知」读成 `Play(0)`** —— 0 是合法取值（「必须活着在场」），
        /// 读错会把死亡族 / 墓地族的触发静默掐死。</para>
        /// </summary>
        public int AbilityContext = CardDef.AbilityContextUnknown;

        /// <summary>
        /// 🆕 **2026-10-22（`W62` · R2 · Mob「再来一次」）：这一条属于【额外触发链】**
        /// （链首 = 卡面 `it triggers an additional time` / `it applies the effect twice` 那一条）。
        ///
        /// 🔴 **口径（2026-10-22 `W63` 订正 · 铁律 5）**：旧文案写的是「**这一条是**额外触发
        /// **那一条**」—— 那只描述了**链首**，与实现不符。实现里它是
        /// <c>extraTrigger || ctx.ExtraTriggerDepth &gt; 0</c>（`RuleCore.EnqueueAbility`）：
        /// **链首**由入队点显式传 `extraTrigger: true`，而**派生动作**（额外触发结算期间入队的
        /// 那些监听者动作）靠 `ctx.ExtraTriggerDepth &gt; 0` **自动继承** ⇒ 本字段 = 「额外触发
        /// **或**派生自额外触发链」，不是「只有链首」。
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

        /// <summary>
        /// 🆕 2026-10-27（执行代理 `B9_W68ENG3` · 引擎真缺口第一批 G19 = 账行 625 `WaitingForBlockingAction` 的查询口）：
        /// **原版 `BattleAction.actionType` 的原始号**（`int`，⛔ 别用我们自己的枚举 —— 原版号本身就是数据：
        /// `27 = transformUnit`、`25 = addHandEffect` …）。
        ///
        /// <para>**唯一用途**：让 <see cref="ActionQueue.Has"/> 能按原版号问「四条道里还有没有某一类动作」
        /// —— 原版 `BattleManager.WaitingForBlockingAction(bool)` 就是扫四条队列找
        /// `actionType == 27 (transformUnit)` 的一条（`d:/2/tools/decomp_full/BattleManager__WaitingForBlockingAction.c`：
        /// `local_108[0] == 0x1b`(27) 命中即 `return 1`）。</para>
        ///
        /// <para>⚠️ **缺省 0 = 「未标类型」**（不是某个原版动作号）—— 今天**所有入队点都没写这一格** ⇒ 行为零变化；
        /// 要用它时（消费端等 W57 / G02 `transformUnit`）在入队处回填原版号。</para>
        /// </summary>
        public int ActionType;

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
    public sealed partial class ActionQueue
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

        /// <summary>
        /// 🆕 2026-10-27（执行代理 `B9_W68ENG3` · 引擎真缺口第一批 G19 = 账行 625）：
        /// 原版 `BattleManager.WaitingForBlockingAction(bool showTips)` 的「队列查询口」——
        /// 问「四条动作道里**有没有**任一动作满足这个条件」（典型用途：还有没有 `transformUnit(27)`）。
        ///
        /// <para>**判据（第一权威 · 现读）** = `d:/2/tools/decomp_full/BattleManager__WaitingForBlockingAction.c`：
        /// `matchType == 0xa0(160 Replay)` ⇒ 直接返回 false；否则按 **`actionQueue(+0x350)` →
        /// `instantQueue(+0x358)` → `interruptQueue(+0x368)` → `delayedInstantQueue(+0x360)`** 四条道逐条扫，
        /// 任一条 `actionType(+0x00) == 27` 命中即返回 true。</para>
        ///
        /// <para>🔴 **扫描顺序说明（如实标注，铁律 2/5）**：施工判据 §二 G19 把原版描述成「**倒序**扫四条队列」，
        /// 但 `.c` 里每条道内是 `iVar4 = 0; … iVar4 = iVar4 + 1`（**升序**，下标 0 → `Count-1`），
        /// 四条道的先后则如上（`0x350→0x358→0x368→0x360`）。**本方法是布尔存在性查询**，前向/后向扫描对
        /// 结果**没有可观测差别** ⇒ 按 `.c` 的实际顺序（道 0→1→2→3、道内升序）实现。
        /// ⚠️ 若将来要返回「命中哪一条」而非 bool，必须先按 `.c` 定死顺序再改。</para>
        ///
        /// <para>⚠️ 道下标 `Player(0)/Instant(1)/Interrupt(2)/DelayedInstant(3)` 恰好等于原版那四条队列的
        /// 扫描顺序（`_lanes[]` 的声明序同 <see cref="ActionPriority"/>），所以按 `lane = 0..3` 直扫即可，
        /// ⛔ **不要**借 <see cref="TryDequeue"/> 的 `DequeueOrder`（那是**消费序**，与原版本函数的扫描序不同）。</para>
        ///
        /// <para>⚠️ 今天**0 消费者**（`transformUnit` 机制 = G02 还没做 ⇒ 拿 `ActionType==27` 问恒 false），
        /// 且 `ActionType` 缺省 0 ⇒ 纯增量、行为零变化。</para>
        /// </summary>
        public bool Has(System.Predicate<QueuedAction> match)
        {
            if (match == null) return false;
            // 道 0→1→2→3（Player/Instant/Interrupt/DelayedInstant）= 原版 `+0x350→+0x358→+0x368→+0x360`；道内升序。
            for (int lane = 0; lane < LaneCount; lane++)
            {
                var list = _lanes[lane];
                for (int i = 0; i < list.Count; i++)
                    if (match(list[i])) return true;
            }
            return false;
        }

        // ==========================================================================================
        //  🆕 2026-10-28（执行代理 `B89_E1` · 完全复刻批次 R9「动作队列族」）
        //  **按条撤动作**（账行 L240 / L241 / L242 的队列侧）—— 原版三层：
        //    ① `BattleManager.CancelBattleActionFromQueue(List<BattleAction>, int actionIndex)`
        //        撤**指定下标**那一条，**撤之前按 `actionType` 补副作用**，末尾 `RemoveAt(index)`；
        //    ② `BattleManager.CancelEffectsFromDisappearingUnitFromQueue(…, queue, …)`
        //        倒序遍历**一条**队列，决定撤哪几条 ⇒ 逐条调 ①；
        //    ③ `BattleManager.CancelEffectsFromDisappearingUnit(…)`
        //        对四条队列各调一次 ②。
        //  本类只提供 ① 与「按谓词撤一条」的原语；② / ③ 在 `Core/ActionPump.cs`（同一个 `partial class RuleCore`）。
        //  判据（第一权威）= `d:/2/tools/decomp_full/BattleManager__CancelBattleActionFromQueue.c` 逐句。
        // ⛔ **别把 `Clear()` 当它用** —— `Clear()` 是整表清（只在泵环硬闸那一处），这条是「语义性撤销」。
        // ==========================================================================================

        /// <summary>
        /// 只读取某条道里的第 <paramref name="index"/> 条（越界 ⇒ `null`）。
        /// 原版等价物 = `List&lt;BattleAction&gt;.get_Item(index)`（`CancelEffectsFromDisappearingUnitFromQueue.c`
        /// 在一个 `for` 里反复 `get_Item(queue, i)` 取出来逐格判）。
        /// ⚠️ 返回的是**引用**，⛔ 别在持有它的同时调 <see cref="RemoveQueuedActionAt"/> 改变下标。
        /// </summary>
        public QueuedAction At(ActionPriority lane, int index)
        {
            int i = (int)lane;
            if (i < 0 || i >= LaneCount) return null;
            var list = _lanes[i];
            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }

        /// <summary>
        /// 原版 `BattleManager.CancelBattleActionFromQueue(List&lt;BattleAction&gt; queue, int actionIndex)`
        /// 的等价物 —— **撤掉某条道里指定下标的那一条**。
        ///
        /// <para>**逐句判据**（`decomp_full/BattleManager__CancelBattleActionFromQueue.c`）：
        /// ① 下标越界（`actionIndex >= queue.Count`）⇒ `LogError("No action in queue " + index)` 后**什么都不做**；
        /// ② `actionType == 1 (attack)` ⇒ 对 `+0x08`（= `actingCard`）调 `CardScript.CancelAttack()`；
        /// ③ `actionType == 0x17 (23 createHandCard)` ⇒ 对 `+0x10`（= `targetCard`）取 `gameObject` 后
        ///    `SetActive(false)`；
        /// ④ 最后 `queue.RemoveAt(actionIndex)`。</para>
        ///
        /// <para>🔴 **两个副作用是【语义】不是清理**（施工判据原话）：撤一条「正在打出去的攻击」和
        /// 撤一条「已经建好手牌卡体的 createHandCard」，原版都要**把那一步的后果收回去**。
        /// ⛔ 别只做 `List.RemoveAt`。</para>
        ///
        /// <para>⚠️ **本批的两个副作用仍走「待接点」**（`RuleCore` / 表现层都不属本批的白名单文件）
        /// —— 见 <see cref="ApplyCancelSideEffects"/> 的 doc。</para>
        /// </summary>
        /// <returns>`true` = 真的撤了一条；`false` = 下标越界/道非法（已按原版出声）。</returns>
        public bool RemoveQueuedActionAt(ActionPriority lane, int index)
        {
            int i = (int)lane;
            if (i < 0 || i >= LaneCount)
            {
                UnityEngine.Debug.LogError(
                    $"[ActionQueue] CancelBattleActionFromQueue：非法道 {(int)lane}（合法 0..3）"
                    + " —— 原版同一格是 `LogError` 后什么都不做");
                return false;
            }
            var list = _lanes[i];
            if (index < 0 || index >= list.Count)
            {
                UnityEngine.Debug.LogError(
                    $"[ActionQueue] No action in queue {index}（道 {(int)lane} 共 {list.Count} 条）"
                    + " —— 原版 `BattleManager.CancelBattleActionFromQueue.c` 同一格也是 LogError 后 return");
                return false;
            }
            var act = list[index];
            ApplyCancelSideEffects(_cancelCtx, act);
            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 按谓词**撤一条**（账行 L240 施工判据要求的那个口）。
        /// 扫序 = 道 **0→1→2→3**（= `_lanes[]` 声明序 = 原版 `actionQueue(+0x350)` →
        /// `instantQueue(+0x358)` → `interruptQueue(+0x368)` → `delayedInstantQueue(+0x360)`），
        /// **道内倒序**（= `CancelEffectsFromDisappearingUnitFromQueue.c` 的 `iVar1 = count-1 … downto 0`）。
        /// 命中第一条即撤（走 <see cref="RemoveQueuedActionAt"/>，因此**副作用照原版补**）。
        ///
        /// <para>⚠️ 本方法**只撤一条**；原版 `CancelEffectsFromDisappearingUnitFromQueue` 是「整条泳道全扫」，
        /// 那个循环在 `Core/ActionPump.cs` 的 `CancelEffectsFromDisappearingUnitFromQueue` 里，
        /// 由它逐条回调 <see cref="RemoveQueuedActionAt"/>（正是原版「扫 + 逐条调 ①」的形状）。</para>
        /// </summary>
        public bool RemoveQueuedAction(System.Predicate<QueuedAction> match)
        {
            if (match == null) return false;
            for (int lane = 0; lane < LaneCount; lane++)
            {
                var list = _lanes[lane];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!match(list[i])) continue;
                    return RemoveQueuedActionAt((ActionPriority)lane, i);
                }
            }
            return false;
        }

        /// <summary>
        /// 撤一条动作之前的**副作用**（原版 `BattleManager.CancelBattleActionFromQueue.c` 的那个 `switch`）。
        ///
        /// <para>🔴 **类型号是原版 `BattleActionType` 的字面量**，⛔ 别换成我们自己的枚举：
        /// `1 = attack` · `0x17 = 23 = createHandCard`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleActionType.cs`）。
        /// 本类只认这两个（原版 `switch` 只有这两支，其它类型**只 `RemoveAt`、不补副作用**）。</para>
        ///
        /// <para>🔴🔴 **反证（铁律 5 · 就地订正施工判据）**：施工判据（`判据/B88_R9_主线68条分批.md`
        /// §批次 1）写的是「类型 1 = `CancelAttack(targetCard)`」，**与 `.c` 不符**：
        /// `.c` 类型 1 那一支读的是 `local_100 = 缓冲区 + 0x08`，而 `BattleAction` 的
        /// `+0x08 = actingCard`、`+0x10 = targetCard`（`Core/ActionPump.cs` 类尾「五、②」那张表
        /// ＋ `BattleAction.cs` 字段声明序 ⇒ 逐字节吻合）。旁证：`CardScript__CancelAttack.c:35`
        /// 的闸是「调用者自己必须处于 `inPlayAttacking(3)/0x11`」，那只能是**攻方自己**。
        /// ⇒ 按 `.c` 走 `actingCard`（= 我们的 <see cref="QueuedAction.Actor"/>）。</para>
        ///
        /// <para>🔴 **两支的现状（2026-10-29 `B95_WPUMP` 现读）**：
        /// · 类型 1 ⇒ ✅ **已接等价物** = <see cref="CancelAttackSideEffect"/>（= 原版 `CardScript__CancelAttack`
        ///   的状态闸；清缓冲 / `PrepareAttack` 两格在本仓**没有对应物**，理由见类尾 §十）；
        ///   只有调用方拿不到 `BattleContext`（旧的**两参**重载）时才退回「待接点」出声。
        /// · 类型 0x17 ⇒ ✅ **引擎半已接** = <see cref="OnCancelCreateHandCard"/>（表现层订阅口；`RuleEngine` 里没有
        ///   `GameObject`）⇒ 有订阅者就调它、**不计数**；没有订阅者才退回「待接点」出声 + `CancelSideEffectsPendingForTest++`。</para>
        /// </summary>
        void ApplyCancelSideEffects(BattleContext ctx, QueuedAction act)
        {
            if (act == null) return;
            switch (act.ActionType)
            {
                case 1:                 // attack（`BattleActionType.attack`）⇒ 原版 `CardScript__CancelAttack(actingCard, 0)`
                    // 原版四句：① 清待结算伤害缓冲（`.c:19-34`，本仓伤害逐段现结 ⇒ 无缓冲可清）
                    // ② `cardState ∉ {3 inPlayAttacking, 0x11}` ⇒ LogWarning + return（`.c:35-40`）
                    // ③ `param_2 == 0` ⇒ 跳过 `+0x350` 那道闸（`CancelBattleActionFromQueue.c:37` 传 0）
                    // ④ `PrepareAttack(0,1,0)` 重新举刀（`.c:45`，我们无帧状态）⇒ 等价物 = 状态闸，见类尾 §十
                    // ⇒ 撤这条动作 = **这一记从来没打出去**（攻击在出队时才结算）⇒ 没有可回收的局面量
                    if (!CancelAttackSideEffect(ctx, act)) _cancelSideEffectsPending++;
                    break;
                case 0x17:              // 23 = createHandCard ⇒ 原版 `targetCard.gameObject.SetActive(false)`（`.c:44-52`）；引擎里没有 GameObject
                    if (OnCancelCreateHandCard != null) { OnCancelCreateHandCard(act.Target); break; }   // 表现层订阅口（`BattleDriver` 类尾订阅）；有订阅者 ⇒ 已按原版收回去，不计数
                    _cancelSideEffectsPending++;   // 没有订阅者 ⇒ **仍是待接点**（出声 + 计数，⛔ 不静默）
                    UnityEngine.Debug.LogWarning(
                        "[ActionQueue] 撤掉一条 `createHandCard(0x17)` 动作 —— **没有**执行 `SetActive(false)`："
                        + "表现层没订阅 `ActionQueue.OnCancelCreateHandCard`（账 L240 的待接点）。"
                        + $"动作「{act}」，目标「{(act.Target != null ? act.Target.Name : "(null)")}」");
                    break;
            }
        }

        int _cancelSideEffectsPending;

        /// <summary>走到「待接点」而**没能**补副作用的次数（排查 / 断言用）。⛔ 不是容量、不是错误计数。</summary>
        public int CancelSideEffectsPendingForTest { get { return _cancelSideEffectsPending; } }
    }

    /// <summary>
    /// <see cref="QueuedAction"/> 的**第二批字段**（账行 L241 / L242 用的两格）。
    ///
    /// 🔴 **为什么单开一个 `partial` 段、而不插进上面那个类体**：本文件被 `资料/完全复刻/账/*.tsv`
    /// 按 `文件:行号` 引用（`Count` / `CountOf` / `Busy` / `Enqueue` / `TryDequeue` / `Clear` / `Has`
    /// 共 7 处落点在 `QueuedAction` 之后）—— 在类体中间插行会让它们**整体漂移**
    /// （CLAUDE.md 铁律「新成员一律追加到类尾」）。把新字段放在文件末尾 = **零漂移**。
    /// </summary>
    public sealed partial class QueuedAction
    {
        /// <summary>
        /// 🆕 2026-10-28（执行引擎批次 `B89_E1` · 账行 L241 / L242）：
        /// **原版 `BattleAction.targetCard`（`+0x10`）的对应物** —— 这一条动作指向的那一张。
        ///
        /// <para>**唯一用途** = 离场清扫时按「`targetCard` == 指定那张」再筛一道
        /// （`BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c`：`param_3` 非空时
        /// 要求 `queue[i].targetCard == param_3`，否则跳过）。</para>
        ///
        /// <para>⚠️ **今天几乎全 `null`**：只有 `EnqueueAbility` 的 `abilityTarget` 形参会回填它
        /// （原版那一格就是 `Add*` 动作的 `targetCard`），而施工判据口径下今天只有
        /// `FireBoardWhenListener` 一路传 `abilityTarget`。⇒ **`targetCard` 过滤这一半今天覆盖不到**，
        /// 如实标着（⛔ 不是「做完了」）。好在原版四个调用点里有三个传 `targetCard = null`
        /// （`ResolveJamUnit` / `ResolveAction` 的 jam 支；只有 `RemoveSelfBuffsFromQueues` /
        /// `ResolveTransformUnit` 传自己那张）⇒ 主路径上这一半**无可观测差异**。</para>
        /// </summary>
        public UnitState Target;

        /// <summary>
        /// 🆕 2026-10-28（`B89_E1` · 账行 L242）：**原版 `BattleAction.deathType`（`+0xF0`）的对应物**
        /// （`UnitDeathType`：`undefined(0)` / `combatAttacker(10)` / `combatDefender(15)` / `ability(20)` /
        /// `tactic(30)` / `poison(40)` / `collectWaystone(50)`）。
        ///
        /// <para>**唯一用途** = 离场清扫的**豁免**：`actionType == 0x2d (45 destroyUnit)` **且**
        /// `deathType == 0x28 (40 poison)` ⇒ 原版**直接结束整条泳道的清扫**、不撤它
        /// （`CancelEffectsFromDisappearingUnitFromQueue.c` 的 `local_130[0] == 0x2d &amp;&amp; local_40 == 0x28`
        /// 那一支；`local_40` 的偏移 = 缓冲区 + 0xF0 = `deathType`，判据 = 字段声明序 + 对齐）。</para>
        ///
        /// <para>⚠️ 缺省 `0` = 未标（今天**所有入队点都没写这一格**，且 `ActionType` 也恒 0
        /// ⇒ 这条豁免结构上走不到，纯增量、行为零变化）。要用它时与 <see cref="ActionType"/> 一起回填。</para>
        /// </summary>
        public int DeathType;
    }

    // ==========================================================================================
    //  🆕 2026-10-29（执行代理 `B95_WPUMP` · 账 L240 的「类型 1 副作用」）——
    //  **为什么单开一个 `partial` 段、且写在文件最末**：本文件被 `账/*.tsv` 按 `文件:行号` 引用
    //  （`TryDequeue` / `At` / `RemoveQueuedActionAt` / `RemoveQueuedAction` / `QueuedAction.Target`
    //   / `QueuedAction.DeathType` 共 6 处落点在类体内）⇒ 在类体中间插行会让它们**整体漂移**。
    //  新成员放在文件末尾 = **零漂移**。
    //
    //  ── §十：`CancelAttack` 的等价物（原版 `CardScript__CancelAttack(longlong this, char param_2)`）──
    //  判据（第一权威 · 全文现读 `d:/2/tools/decomp_full/CardScript__CancelAttack.c:1-48`）：
    //    :19-34  ① `+0xf8`（待结算伤害缓冲：`count +0x18` / 数组 `+0x10`）非空 ⇒
    //            `+0x1c++`、`+0x18 = 0`、`Array.Clear(缓冲, 0, count)` —— **清掉待结算伤害**
    //    :35-40  ② `+0x228`（cardState）∉ `{3 inPlayAttacking, 0x11 inPlayAminingAttack}` ⇒
    //            `CustomDebug.LogWarning` 后 **return**（连 `PrepareAttack` 都不跑）
    //    :41-44  ③ `param_2 != 0 && +0x350 == 0` ⇒ LogWarning + return
    //            （本引擎唯一的调用方 `BattleManager__CancelBattleActionFromQueue.c:37` 传的是 **0** ⇒ 这一支不进）
    //    :45-46  ④ `PrepareAttack(this, 0, 1, 0)` + `StartCoroutine`
    //  ⇒ **四格里三格在本仓没有对应物、一格有**：
    //    · ① 我们**伤害逐段现结**、没有待结算缓冲（同一结论见 `RuleCore.cs:16679`）⇒ 无物可清；
    //    · ② **有** = `RuleCore.IsAttackingOnBoard`（原版 cardState 那道闸在我们模型里的落点，账 `A1983`）；
    //    · ③ 调用方恒传 0 ⇒ 结构上进不来；
    //    · ④ `PrepareAttack` 是「举刀」那个帧状态机 ⇒ 我们的攻击**在出队时才结算**，
    //       撤掉这条动作 = 这一记从来没打出去 ⇒ 没有「已经摆好的局面」要退回。
    // ==========================================================================================
    public sealed partial class ActionQueue
    {
        /// <summary>「这一次撤动作时能不能拿到 <see cref="BattleContext"/>」的**单线程传递格**。
        /// 只有 <see cref="RemoveQueuedActionAt(BattleContext, ActionPriority, int)"/> 在调两参版本时临时置它，
        /// 出来就还原（`try/finally`）⇒ ⛔ 别在别处读写它，也别指望它跨调用存活。</summary>
        BattleContext _cancelCtx;

        /// <summary>
        /// <see cref="RemoveQueuedActionAt(ActionPriority, int)"/> 的**带上下文**重载 ——
        /// 只为让 `attack(1)` 的撤销副作用能走 <see cref="CancelAttackSideEffect"/>（那道状态闸要 `ctx.Players`）。
        /// 两参版本逐字复用（下标/道合法性那套判据 ⛔ 别抄第二份）。
        /// </summary>
        public bool RemoveQueuedActionAt(BattleContext ctx, ActionPriority lane, int index)
        {
            var prev = _cancelCtx;
            _cancelCtx = ctx;
            try { return RemoveQueuedActionAt(lane, index); }
            finally { _cancelCtx = prev; }
        }

        /// <summary>
        /// 原版 **`CardScript__CancelAttack(actingCard, 0)`** 的等价物（撤一条 `attack(1)` 动作之前补）。
        ///
        /// <para>返回 `true` = **已按原版处置完**（不计 `CancelSideEffectsPendingForTest`）；
        /// `false` = 仍是「待接点」（调用方计数并出声）。逐句判据与「三格无对应物」的理由 → 本段头 §十。</para>
        ///
        /// <para>⚠️ **这里一行 `ctx.Log` 都不许打**：`ctx.Events` 没有 drain 却被算进 `Fingerprint`
        /// ⇒ 只在一端发生的日志会让 P2P 两端指纹分叉（判定失败也不再写第二条）。</para>
        /// </summary>
        static bool CancelAttackSideEffect(BattleContext ctx, QueuedAction act)
        {
            if (act == null) return true;              // 没有条目 ⇒ 无事可做
            if (ctx == null) return false;             // 旧两参入口：拿不到场上状态 ⇒ 照旧走「待接点」出声
            if (act.Actor == null)
            {
                // 原版 `CancelBattleActionFromQueue.c:30` 在 `actingCard == 0` 时走 `FUN_1803f47a0`（NRE，不返回）。
                // ⛔ 我们不复制崩溃语义：出声一次，然后**照样把条目撤掉**（调用方继续 RemoveAt）。
                UnityEngine.Debug.LogWarning(
                    "[ActionQueue] 撤掉一条 `attack(1)` 动作，但它的施放者是 null —— 原版这一格是 NRE"
                  + "（`CancelBattleActionFromQueue.c:30` 的 `FUN_1803f47a0`）；我们出声后照常撤。"
                  + $"动作「{act}」");
                return true;
            }
            if (!RuleCore.IsAttackingOnBoard(ctx, act.Actor))
            {
                // 原版 `CancelAttack.c:35-40`：cardState 不在 {3, 0x11} ⇒ LogWarning 后 return（不 PrepareAttack）。
                UnityEngine.Debug.LogWarning(
                    "[ActionQueue] 撤掉一条 `attack(1)` 动作 —— 「" + (act.Actor.Name ?? "?")
                  + "」已不在棋盘上/已死；原版 `CardScript__CancelAttack.c:35-40` 在这一格是 "
                  + "`CustomDebug.LogWarning` 后直接 return（不重新举刀）");
                return true;
            }
            // 状态闸通过：原版还剩 ①清缓冲（我们没有那个缓冲）、④PrepareAttack（我们攻击出队才结算）。
            // ⇒ 等价物到此为止 —— **不是**待接点，所以不计数、也不再报警。
            return true;
        }
    }

    // ==========================================================================================
    //  🆕 2026-10-29（执行代理 `B97_W38` · 账 L240 的「类型 0x17 副作用」）——
    //  **为什么单开一个 `partial` 段、且写在文件最末**：本文件被 `账/*.tsv` 按【行号】引用
    //  （`ActionQueue.cs:14/7/185/224/233/245/261/277/292/402/487/582/599` 共 13 处）⇒ 在类体中间
    //  插成员会让它们**整体漂移**（铁律 18·b）。新成员放在文件末尾 = **零漂移**。
    //
    //  ── §十一：`createHandCard(0x17)` 的 `SetActive(false)`（原版 `CancelBattleActionFromQueue.c:44-52`）──
    //  判据（第一权威 · 全文现读 `d:/2/tools/decomp_full/BattleManager__CancelBattleActionFromQueue.c:1-57`）：
    //    `case 0x17` ⇒ 取 `+0x10`（= `targetCard`）的 `gameObject` 后 `SetActive(false)`；随后统一 `RemoveAt(index)`。
    //  ⇒ **那一句是纯 `CardPresentation` 的事**（`RuleEngine` 里没有 `GameObject`、也没有卡体）。
    //    🔴 **结构差异、语义同一**（⛔ 不是「自己设计」：原版对目标卡体做的事一字不差，只是跨了程序集边界）：
    //    引擎在撤这条动作时把 `act.Target`（= 原版 `targetCard`）交出去，由表现层把那张的卡体关掉。
    //  ⚠️ **缺省 `null` = 行为与加这个钩子之前逐字相同**（出声 + `CancelSideEffectsPendingForTest++`）；
    //    订阅方在 `CardPresentation`（`BattleDriver` 类尾）⇒ 账 L240 剩下的那一跳见 `报告/B97_W38.md` §B。
    //  ⚠️ **今天是休眠件**：全仓 `actionType: 0x17` 的入队点 = 0（`grep` 只命中判据注释）⇒ 接上也零可观测。
    //    照铁律 11 照做（只有先后、没有「不做」）。
    // ==========================================================================================
    public sealed partial class ActionQueue
    {
        /// <summary>
        /// 原版 `CancelBattleActionFromQueue.c:44-52` 那句 **`targetCard.gameObject.SetActive(false)`** 的表现层订阅口。
        /// 实参 = 原版 `targetCard`（`+0x10`）= 我们的 <see cref="QueuedAction.Target"/>（可能是 `null` —— 原版同一格会 NRE）。
        /// <para>⛔ 只在「撤一条 `actionType == 0x17 (createHandCard)` 的动作」时被调，每次至多一次；
        /// 没有订阅者 ⇒ 不出声也不报错地走回「待接点」那条（见 `ApplyCancelSideEffects`）。</para>
        /// </summary>
        public static System.Action<UnitState> OnCancelCreateHandCard;
    }
}
