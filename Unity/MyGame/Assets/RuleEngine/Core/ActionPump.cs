// ActionPump.cs — 原版「四条动作队列」的**泵点 / 动作外壳**（引擎线 `T03·c` 批 B · 行为接线）
//
// 判据正本（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`）：
//   `资料/普查产出_1021/W61_引擎线_T03c批B判据.md` §2.3 / §4.A 第 3、5 步
//   `资料/普查产出_1020/W58_战斗结算_多词条顺序判据.md` §二
//
// 原版形状（`BattleManager__Update.c:1250-1299` + `NextActionInQueue.c:11-28`
//           + `ResolveActionFromQueue.c:32-44` + `FinishResolvingAction.c:34-46`）：
//   ```
//   Update():
//     if (!duringBattle && !duringScriptedActions) return;
//     if (resolvingActionFlag(+0x280)) return;                  // ← 正在结算 ⇒ 本帧不泵
//     lane = NextActionInQueue();                               // ← 2 → 1 → 3 → 0
//     if (lane 没有动作) return;
//     resolvingActionFlag = true;
//     action = lane[0]; lane.RemoveAt(0);                       // ← 同道 FIFO
//     ResolveAction(action);
//     FinishResolvingAction();                                  // ← 清 flag
//   ```
//   ⇒ **每帧最多一条**。我们**没有帧循环**（铁律：批处理下没有帧循环），
//     所以等价物 = **在「一次顶层动作结束」这个边界上把队列取空** ——
//     取出的**次序**与原版逐字相同（2→1→3→0 循环），差的只有「两条之间夹了几帧」，
//     而帧间隔在纯规则引擎里**不可观测**（表现层读的是 `ctx.Signals` 序列，序列本身不变）。
//
// 🔴 **三条设计口径（都有判据，别推翻）**：
//   ① **动作内部不递归泵队** —— `Resolving` 为真时 `PumpActionQueues` 直接返回；
//      动作里产生的触发由**当前泵的循环**接着取（原版同一个形状：`Update` 下一帧才取）。
//   ② **闸在泵点、不在队列** —— `ActionQueue.TryDequeue` 不看 `Resolving`（见那个类的注释）。
//   ③ **顶层动作外壳 = `ActionScope`** —— 进入时 `Resolving = true`（这一条动作正在结算，
//      里面产生的触发**只入队**），退出时清 flag 并**取空队列**。
//      原版那个「当前动作」是 `ResolveActionFromQueue` 结算的那一条；我们是玩家/引擎的
//      顶层入口（出牌 / 攻击 / 回合起止 / 主动技能 / 死亡处理 …）。
//
// ⚠️ **为什么「顶层入口也要算一条正在结算的动作」**：原版玩家点一下 → 建一条
//   `remotePlayerAction(36)`@Player 入队 → `Update` 取它结算；它结算期间产生的触发
//   **全部排队**，等它跑完才轮到。我们如果把出牌直接跑完再泵，次序**一样**
//   （Player 道本来就是最后消费的一条道）；差别只出现在「队列里已经攒着东西」时 ——
//   而原版每帧都泵，玩家点得动的时候队列**必然已经空**（`Busy` 时输入层不该放行）。
//   ⇒ 两种写法观测等价；本实现选「外壳」这一种，因为它不必把 `PlayCard` 的返回值/`out`
//     参数改成异步（那会成片改调用点，且 `RuleEngineTest` 的 6000+ 条断言都同步读终局）。
namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>
        /// **一次泵最多结算几条** —— ⛔ 不是队列容量，是**防跑飞的硬闸**。
        ///
        /// 为什么需要：原版的触发链由**游戏规则**保证收敛（没有深度上限，
        /// `CardAbility.priority` 只决定道，不决定次数）。而我们这边一旦有
        /// 「A 触发 B、B 再触发 A」这种环，队列会**永不空** —— 没有栈溢出，但会死循环。
        /// 既有那条 `ctx.EffectChain &lt; MaxEffectChain(8)` 是**同步递归**时代的上限，
        /// 现在触发改走队列、递归深度回到 1（泵是循环，不是递归）⇒ 那条闸**不再兜得住**。
        /// ⇒ 这里补一条**结构**上的兜底：超过就 **LogError（出声）+ 清空四道 + 停止泵**。
        /// ⚠️ 值取 4096 的依据：一回合内真实动作量在**个位到几十**（一次攻击最多 ~10 条，
        ///    一次出牌 ~20 条），4096 是「三个数量级以上的余量」⇒ 正常对局摸不到，
        ///    摸到就一定是环。⛔ 别拿它当「队列满了」的容量判据。
        /// </summary>
        public const int MaxPumpedActionsPerDrain = 4096;

        /// <summary>
        /// 「**一条顶层动作正在结算**」的作用域 —— 相当于原版
        /// `ResolveActionFromQueue` 里置 `resolvingActionFlag(+0x280)` 到
        /// `FinishResolvingAction` 清掉的那一段。
        ///
        /// 用法：<c>using (RuleCore.BeginAction(ctx)) { …原有函数体… }</c>
        ///
        /// 🔴 **必须成对**（`using` 交给编译器），否则 flag 会留在 `true` 上 ⇒
        ///   之后**所有**泵点都不干活（静默：触发全部堆在队列里不结算）。
        /// 🔴 **嵌套安全**：内层看到 `Resolving == true` ⇒ 什么都不做（**不泵队**、退出时也不清 flag）
        ///   —— 这正是「动作内部不递归泵队」。
        /// ⚠️ 进入时**先泵一次**：把「上一个顶层入口没取干净的残留」结掉。
        ///   今天不该有残留（每个顶层入口退出时都取空），这一步是**结构性兜底**，
        ///   零成本（队列空时 `TryDequeue` 立刻 false）。
        /// </summary>
        public struct ActionScope : System.IDisposable
        {
            readonly BattleContext _ctx;
            readonly bool _outermost;

            internal ActionScope(BattleContext ctx)
            {
                _ctx = ctx;
                _outermost = ctx != null && !ctx.ActionQueue.Resolving;
                if (_outermost)
                {
                    // 先把上一轮残留结掉（正常情况下队列是空的，见类型注释）
                    PumpActionQueues(ctx);
                    ctx.ActionQueue.Resolving = true;
                }
            }

            public void Dispose()
            {
                if (_ctx == null || !_outermost) return;
                _ctx.ActionQueue.Resolving = false;
                PumpActionQueues(_ctx);          // ← 出队到空（2→1→3→0 循环）
                // ---- 🆕 2026-10-24（引擎线 `G2` · `C4` D9）：`WhileInPlay`(20) 的表现口 ----
                //  落点在**这里**（= 一条**顶层动作**结束、队列已排空），对原版
                //  `BattleManager__FinishResolvingAction.c:91`（闸 = `ActionsLeftInQueue() < 1`）。
                //  ⛔ **别放进 `PumpActionQueues`**：`ActionScope` 的**构造函数**也会调它一次
                //  （`:87` 结掉上一轮残留）⇒ 那样一次动作会发**两条**（`G2` 探针实测 2 / 2）。
                //  ⛔ 也别挂 `Aura.Recompose`（每次棋盘写入都跑 = 一动作多播）。
                //  判据 / 只发有动画行那一张的理由 / 手牌那半为什么没做 →
                //  `RuleCore.EmitWhileInPlayPass` 的 doc（⚠️ `Core/EffectResolver.cs` 是
                //  `partial class RuleCore` ⇒ 这里写 `RuleCore.`，照文件名写 `EffectResolver.` 编译不过）。
                RuleCore.EmitWhileInPlayPass(_ctx);
                // 🔴 **结构性兜底（不该发生）**：顶层动作都跑完了队列却没空 ⇒ 两种病，**分开报**。
                //    ⛔ 不静默；⛔ **别合并成一句** —— 两种病的修法完全不同：
                //      ① **泵点漏了**（`Resolving` 已清、队列还有东西）⇒ 去找哪个顶层入口没包 `BeginAction`；
                //      ② **`Resolving` 闸泄漏**（退出后它还是 `true`）⇒ 泵点循环的条件就是 `!q.Resolving`
                //         （见 `PumpActionQueues`），泄漏时**一条都不取**、队列当然非空 ——
                //         这**不是**泵点漏，是有人把 `Resolving` 置真却没配对清掉（`ActionScope` 之外不许写它）。
                //    ⚠️ 2026-10-21 审查（`W61b` 第 6 条）：原来只有 ① 那一句、还带着 `!Resolving` 的前置 ——
                //      ⇒ 最该报的 ② 恰好在此时沉默（「从此所有触发都不结算」却一条日志都没有）。
                //    对局结束时队列本来就允许有残留（见泵点），所以 `IsOver` 那一档不报。
                if (_ctx.ActionQueue.Count > 0 && !_ctx.IsOver)
                {
                    if (_ctx.ActionQueue.Resolving)
                        UnityEngine.Debug.LogError(
                            $"[ActionQueue] 顶层动作退出后 `Resolving` **仍为 true**，队列里还有 "
                            + $"{_ctx.ActionQueue.Count} 条 —— 这是**闸泄漏**（不是泵点漏）：泵点循环的条件是 "
                            + "`!Resolving` ⇒ 从此一条都不会被取出（症状 = 之后所有触发都不结算）。"
                            + "查谁在 `ActionScope` 之外直接写 `ActionQueue.Resolving`"
                            + "（判据 → Core/ActionPump.cs 的 `ActionScope` doc）。");
                    else
                        UnityEngine.Debug.LogError(
                            $"[ActionQueue] 顶层动作结束后队列仍有 {_ctx.ActionQueue.Count} 条没结算 —— "
                            + "泵点漏了（哪个顶层入口没包 `BeginAction`？判据 → Core/ActionPump.cs 头注释）");
                }
                // 🆕 **2026-10-22（`W62` · R2）**：本条顶层动作结束 ⇒ 关掉「刚离场」窗口
                //   （`BattleContext.DyingThisAction` 的 doc：原版那面窗口 = `UnitDeath` 协程跑完之前，
                //   我们等价成**一条顶层动作**）。⚠️ 必须**泵完之后**才清 —— 队列里那些监听器
                //   正是靠这个窗口才还能指着刚死的那个人。
                _ctx.DyingThisAction.Clear();
            }
        }

        /// <summary>开一个顶层动作作用域（见 <see cref="ActionScope"/>）。`ctx == null` ⇒ 空作用域。</summary>
        public static ActionScope BeginAction(BattleContext ctx)
        {
            return new ActionScope(ctx);
        }

        /// <summary>
        /// 把**一条 ability 的结算**排进队列（原版 `BattleManager.AddAutoActionToQueue`）。
        ///
        /// 🔴 **它只入队、不结算** —— 哪怕队列是空的、哪怕当前没有动作在跑。
        ///   「什么时候真的跑」只由泵点决定（`ActionScope` 退出 / 显式 `PumpActionQueues`）。
        ///   原版也是这样：`AddAutoActionToQueue` 只 `List.Add`，取队的是 `Update`。
        /// ⚠️ **不要**在这里做「队列空就直接执行」那种短路 —— 那会把
        ///   「跨道 priority 压过入队序」这条语义整个抹掉（先入 DelayedInstant 的东西
        ///   会被后来的 Instant 抢到前面去）。
        /// </summary>
        /// <param name="lane">进哪一条道。非法值（不在 0..3）由 `ActionQueue.Enqueue` LogError 后丢弃。</param>
        /// <param name="label">人话标签（日志 / 排查用）</param>
        /// <param name="body">出队时要跑的那段结算（**必须自带上下文** —— 见 <see cref="QueuedAction.Execute"/>）</param>
        /// <param name="actor">
        /// 🆕 2026-10-21（审查 `W61b` 第 3 条）：**这条 ability 的「施放者」是哪个场上单位**
        /// （原版把 `actingCard` 一起打包进 `BattleAction`）。`null` = 不关任何单位
        /// （战术卡留下的常驻效果 / 手牌里的监听器 / 死亡结转那一族）。
        /// </param>
        /// <param name="requireActorInPlay">
        /// **出队时要不要复核「施放者还能不能跑这条 ability」** —— ⚠️ **2026-10-22（`W63` · C1）起
        /// 这是【回退口径】**（只在 <paramref name="abilityContext"/> 未知时生效）：
        /// `true` = `Play(0)` 的近似 —— 结算那一刻要求 `CardScript.IsInPlay(card)`，
        /// 所以出队时施放者已经离场（死亡 / 回手 / 移出棋盘 / 变成残骸）⇒ **这条作废**；
        /// `false` = `InPlayOrDying(40)`（**待死也算在场**）/ `50`（**在墓地也算**）/ 没有施放者 —— 照跑。
        /// ⚠️ 缺省 `false`：既有调用点**行为零变化**，只有显式传 `true` 的才参与复核；
        ///   「哪个关键词能判」由 `ActorMustBeInPlayForTrigger`（数据表）一处决定。
        /// </param>
        /// <param name="abilityContext">
        /// 🔴🆕 **2026-10-22（`W63` · C1）：这条 ability 的原版 `abilityContext`**（六档，见
        /// <see cref="IsRightContext"/>）。取值一律走 <see cref="AbilityContextOfAbility"/>：
        /// 它会套 `trigger 名优先 → 关键词兜底`，**但不套 skip 表** ——
        /// 原版 skip 的那五族（`ThisCardPlayed` / `ThisUnitDeath` / `Codex` / `Sacrifice` /
        /// `TriggerEnchantment`）请直接**传缺省**（或 `ActorMustBeInPlayForTrigger` 的结果为假时也别传）。
        /// `-1`（缺省 = 未知）⇒ 泵点**回退** <paramref name="requireActorInPlay"/> 那条旧口径
        /// ⇒ **今天不传 = 行为逐字不变**。
        /// ⚠️ 传了已知 context ⇒ <paramref name="requireActorInPlay"/> **不再生效**（context 更准）。
        /// </param>
        public static bool EnqueueAbility(BattleContext ctx, ActionPriority lane, string label,
                                          System.Action body,
                                          UnitState actor = null, bool requireActorInPlay = false,
                                          bool extraTrigger = false,
                                          int abilityContext = CardDef.AbilityContextUnknown,
                                          bool requireActorInPlayOrDying = false)
        {
            if (ctx == null || body == null) return false;
            // ============================================================
            //  🔴🆕 **2026-10-23（引擎线 `W63` 收口）：`abilityContext` 的闸搬回【入队那一刻】**
            //
            //  判据（第一权威 = 全量反编译）：原版 `CardAbility.CanTriggerAbility` 是
            //  **`RawCardScript.TriggerAbilities` 在广播那一刻同步调**的
            //  （`RawCardScript__TriggerAbilities.c:108,207`），`IsRightContext` 全库**只有它一个调用者**
            //  ⇒ **原版在入队前判**，入了队就照跑（死亡本身不在 `CancelEffectsFromDisappearingUnit`
            //  的四个调用点里：只 service jam / transformUnit / TransformIntoRemnant / removeEffect）。
            //  我们 2026-10-22（`W63` · C1）把它做在**出队**，比原版**更严**：
            //  「入队时还在、被后面动作打死」原版照跑、我们取消 —— `W63 #2`（Slay 正文整条丢失）
            //  就是这个偏差的产物。⇒ 判点搬到入队，出队只保留原版那四种「主动取消」里
            //  我们唯一建模得到的**变残骸 / transform**（见泵点那一支）。
            //
            //  ⚠️ **只在能定位载体时判**（`FindBoardCell` 找得到施放者）：定位不到 ⇒ **不判**
            //     （保持改前口径 —— 那些是「已离场 / 还没上场」的入队点，原版闸看的是**卡状态**，
            //     我们此刻认不出它处在哪一档，⛔ 不许猜）。
            //  ⚠️ `-1`（未知 / 旧卡表 / 第二解析路没接列）⇒ **一个字都不判**，回退
            //     `requireActorInPlay` 那条旧口径（在泵点），与改前逐字相同。
            // ============================================================
            if (abilityContext != CardDef.AbilityContextUnknown && actor != null)
            {
                int co, cs;
                FindBoardCell(ctx, actor, out co, out cs);
                if (co >= 0 && !IsRightContext(ctx, co, cs, abilityContext, actor.Instance))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] **不触发**「{label}」—— 载体「{actor.Name}」此刻不在原版 `abilityContext="
                        + AbilityContextName(abilityContext) + "` 要求的区域"
                        + "（原版 `CardAbility.CanTriggerAbility` → `IsRightContext` 在**入队前**判，"
                        + "判据 `RawCardScript__TriggerAbilities.c:108,207`；⛔ 不是静默丢，这条就是出声口）");
                    return false;
                }
            }
            var act = new QueuedAction
            {
                Label = label, Execute = body,
                Actor = actor, RequireActorInPlay = requireActorInPlay,
                AbilityContext = abilityContext,
                // 🔴🆕 **2026-10-23（`Slay` 就地结算复核）**：出队时按原版
                //   `CardScript.IsInPlayOrDying`（含 `waitingToDie`）复核载体 ——
                //   判据 `BattleManager__ResolveTriggerSlay.c:116-226`；今天只有 Slay 那一跳传 `true`。
                RequireActorInPlayOrDying = requireActorInPlayOrDying,
                // 🔴 **2026-10-22（`W63` #1 · Mob「再来一次」无限循环）**：**派生动作继承守卫**。
                //
                //   判据：额度（`UnitState.ExtraTriggers`）是**监听者**在它自己的队列动作里挂上的
                //   （`EffectResolver.DoExtraTrigger`），所以「额外触发会不会无限续杯」只取决于
                //   **那批监听者的队列动作跑的时候** `ctx.ExtraTriggerDepth` 是不是 > 0。
                //   改前只有 `FireExtraTriggers` 顶起来的那个窗口算数 ⇒ 监听者的动作是**另一条队列条目**，
                //   它跑的时候 depth 已经落回 0 ⇒ 再挂一次额度 ⇒ 一路撞到 `MaxPumpedActionsPerDrain(4096)`。
                //   ⇒ 守卫必须盖住**整条派生链**：只要这条动作是在「额外触发」的结算期间入队的，
                //     它就属于同一次额外触发，泵点也要为它顶住 depth。
                //   ⚠️ 额度仍然**按一次攻击一次性消费**（`TakeExtraTrigger` 取走即清零，只此一处）；
                //     本行只传播「正在跑的是额外触发」这个事实，⛔ 不是第二份判据。
                //   ⚠️ 不是「把结构闸调小」—— 4096 一个字都没动。
                ExtraTrigger = extraTrigger || ctx.ExtraTriggerDepth > 0,
            };
            return ctx.ActionQueue.Enqueue(act, lane);
        }

        /// <summary>
        /// **这条触发的那条 ability，出队时要不要按 `Play(0)` 复核「载体还活着在场」**。
        ///
        /// 🔴 **2026-10-22（引擎线 `W63` · `T03·c/d` 收口）就地改口径** —— 旧版是一张
        /// **按关键词族猜**的硬编码表（`rally` / `mob` / `slay` / `artifice` / `abouttoattack`
        /// 一律 `true`），因为当时引擎**没有** `abilityContext` 数据。现在数据已落进卡表
        /// （`cards_engine.json` v14，每卡 `abilityContext` 一列）⇒ 判据换成**数据**：
        ///   ① **skip 表**（原版 `CardAbility__CanTriggerAbility.c:69-95`）：trigger
        ///      `0 ThisCardPlayed` / `80 ThisUnitDeath` / `145 Codex` / `220 Sacrifice` /
        ///      `500 TriggerEnchantment` **整段跳过**这一格 ⇒ 一律 `false`（不判）；
        ///   ② **取这张卡这条 ability 的 context**（<see cref="AbilityContextOfAbility"/>）：
        ///      `Play(0)` ⇒ `true`（**必须活着在场**）；其余五档 ⇒ `false`
        ///      （不是「必须在场」那一条 —— 由泵点的 <see cref="IsRightContext"/> 按各自档位判，
        ///      见 `QueuedAction.AbilityContext`）；
        ///   ③ **`-1` 未知**（旧卡表 / 这张卡没有这条 trigger / 生成器还没落列）⇒ **回退旧关键词表**
        ///      （<see cref="LegacyActorMustBeInPlayForKeyword"/>）。这是**正常回退**、不出声；
        ///      数据真缺列时 `CardDatabase.Parse` 的 `version<14` 守卫已经在装载时出过声。
        ///
        /// 🔴 **判据只写一份**：⛔ 别在调用点各写一份（批 D/E 只调本方法或 <see cref="AbilityContextOfAbility"/>）。
        ///
        /// ⚠️ **旧表为什么只有「整族都是 0」的那五个**：那是**没有数据时**的保守近似
        /// （判多了会把 `InPlayOrDying(40)` / `InPlayOrCemetery(50)` 那一档静默掐掉 ——
        /// `Rage` / `Unstable` / `Regiment` 正是靠它工作）；现在数据到位，**混着的族也能逐卡判**
        /// （`strike` 那 5 条 40、`codex` 的 3 条 50 各自按自己的 context 走）。
        ///
        /// <para>⚠️ **时机偏差（已知、本笔不改判点）**：原版 `IsRightContext` 在**广播那一刻 /
        /// 入队之前**判（`RawCardScript__TriggerAbilities.c:108,207`），我们这道闸在**出队时**判。
        /// ⇒ 「入队时还在、被后面动作打死」原版照跑、我们取消（**更严**）。改入队判会让
        /// `RuleEngineTest` 的 `T27` ②③ 红（判据 → `普查产出_1022/W63_abilityContext_施工简报.md` §1·2 / §6.7）。</para>
        /// </summary>
        /// <param name="keyword">引擎侧触发关键词（`KeywordTable.*`）。**旧签名** —— 没有卡、没有
        /// trigger 名 ⇒ context 恒 `-1` ⇒ **行为与改前逐字相同**。⏭ 批 D（`RuleCore` C5）改调三参重载。</param>
        public static bool ActorMustBeInPlayForTrigger(string keyword)
        {
            return ActorMustBeInPlayForTrigger(null, null, keyword);
        }

        /// <inheritdoc cref="ActorMustBeInPlayForTrigger(string)"/>
        /// <param name="card">这条 ability 的**载体卡**（`actor.Card` / 附魔卡 `only.Card`）。
        /// `null` ⇒ 未知（走旧表）。</param>
        /// <param name="triggerName">原版 `AbilityTrigger` **枚举名**（`UnitAttack` / `Strike` …，
        /// 大小写不敏感）。`null` ⇒ 只按关键词兜底。</param>
        /// <param name="keyword">引擎侧触发关键词；`triggerName` 查不到时兜底用。</param>
        public static bool ActorMustBeInPlayForTrigger(CardDef card, string triggerName, string keyword)
        {
            if (TriggerSkipsAbilityContext(triggerName, keyword)) return false;
            // 🔴🆕 **2026-10-24（引擎线 `A1` 第 5 件 · 判据由 C1 闭合）**：这一格改走
            //   <see cref="AbilityContextOfAbilityV14"/> —— 原版 `CardAbility.abilityContext` 是
            //   **序列化枚举、默认 `0 = Play`**（`cards_raw.json` 1351 张 / 1496 条 **0 条缺键**），
            //   而我们卡表**只落非 0 行**（`gen_cards_engine.py` 的 `ability_contexts_of`）
            //   ⇒ 「查不到这一行」在有 v14 真值快照的卡上 = **`Play(0)` 被省略**，**不是未知**
            //   （判据 → `资料/普查产出_1024/C1_引擎线判据总表.md` §5·1）。
            //   ⚠️ **`-1` 只剩两种来路**：旧卡表 / 第二解析路没接列 / 载体卡拿不到
            //   （`DeathWatch` 那一支传 `null`）—— 那才回退旧关键词表。
            //   ⚠️ 本方法的返回值只喂 **`requireActorInPlay`（出队复核）**；凡**同时**传了
            //   `abilityContext` 的调用点，泵点会走「变残骸才取消」那一支、**跳过**这一支
            //   （`ActionPump.PumpActionQueues` 的 `else if` 结构）⇒ 原版「入队后死亡不取消」
            //   那条口径（`W63`）不受影响。
            int context = AbilityContextOfAbilityV14(card, triggerName, keyword);
            if (context == CardDef.AbilityContextUnknown)
                return LegacyActorMustBeInPlayForKeyword(keyword);   // `-1` 契约：回退旧表
            return context == CardDef.AbilityContextPlay;
        }

        /// <summary>
        /// **这张卡这条 ability 的 `abilityContext`** —— 消费端**唯一**取值口（卡 + trigger 名 + 关键词）。
        ///
        /// 查法（照 <see cref="CardDef.AbilityContextOfTrigger"/> 的 doc）：
        ///   ① **trigger 名优先** —— 它覆盖全部行（`cards_engine.json` v14 的 `abilityContext[].name`）；
        ///   ② **关键词兜底** —— 生成器只给 `ABILITY_TRIGGER_KEYWORD` 有映射的 53/159 行落了 `keyword`
        ///      （`UnitAttack` 这一族**没有**关键词，⛔ 别以为「关键词查不到 = 不用判」）。
        /// 两者都查不到 ⇒ `-1`（<see cref="CardDef.AbilityContextUnknown"/>，⛔ **不是** `Play(0)`）。
        ///
        /// ⚠️ 本方法**不套 skip 表**（skip = 「这条 trigger 原版根本不判」，见
        /// <see cref="TriggerSkipsAbilityContext"/>）；调用方拿到 `-1` 时**一律按「不判」处置**，
        /// 两者结果同向。
        /// </summary>
        public static int AbilityContextOfAbility(CardDef card, string triggerName, string keyword)
        {
            if (card == null) return CardDef.AbilityContextUnknown;
            int c = card.AbilityContextOfTrigger(triggerName);
            if (c == CardDef.AbilityContextUnknown) c = card.AbilityContextOfKeyword(keyword);
            return c;
        }

        /// <summary>
        /// 🔴🆕 **2026-10-23（引擎线 `W63` · 收口）：在 <see cref="AbilityContextOfAbility"/> 之上补
        /// 「**v14 缺行 = `Play(0)`**」这一跳** —— 消费端**唯一**取值口（两个 accessor 分工见下）。
        ///
        /// 为什么要有这一跳：`cards_engine.json` v14 起**只落 `context != 0` 的行**
        /// （生成器 `ability_contexts_of()` 的落值口径，判据 → `W63_abilityContext_施工简报.md` §5·A）。
        /// ⇒ 「这张卡这条 ability 在表里查不到」在**有真值快照的卡**上不是「未知」，而是
        /// **`Play(0)` 被省略了**。没有这一跳，`-1` 会一路回退到
        /// <see cref="LegacyActorMustBeInPlayForKeyword"/> 那张 5 词表 ⇒
        /// **`Slay` / `Mob` / `Regroup` 这一族全部判不出**（旧表按关键词猜，⛔ 那正是要退役的东西）。
        ///
        /// 判据（逐条）：
        ///   ① 先按 <see cref="AbilityContextOfAbility"/> 查真值（trigger 名优先 → 关键词兜底）；
        ///   ② 查不到 **且** 这张卡的列确实来自 v14（<see cref="CardDef.AbilityContextsVersioned"/>）
        ///      **且** 认得出 trigger 名（`triggerName` 非空，或 `keyword` 能反查
        ///      → <see cref="TriggerNameOfKeyword"/>）⇒ **`Play(0)`**；
        ///   ③ 其余 ⇒ `-1`（<see cref="CardDef.AbilityContextUnknown"/>，⛔ 不许读成 `Play(0)`）。
        ///
        /// ⚠️ **为什么 ② 要「认得出 trigger 名」**：生成器只在
        /// `ABILITY_TRIGGER_KEYWORD` 有映射的行上落 `keyword`（21 条）。像 `swarm` / `survivor`
        /// 这种既没有 trigger 名映射、又不在映射表里的关键词 —— 查不到**不能**推成 0
        /// （那会把一条本来 `-1` 的路径静默变成「必须活着在场」）。
        /// `AbilityContextsVersioned` 为假（旧卡表 / 第二解析路没接列）⇒ 照旧 `-1`。
        ///
        /// 🔴 **消费端一律用本方法**（`-1` 契约 = 「不知道，回退旧口径」）；
        /// 只有「我要看原始真值、不想被省略语义影响」的断言才直接调
        /// <see cref="AbilityContextOfAbility"/>。
        /// </summary>
        public static int AbilityContextOfAbilityV14(CardDef card, string triggerName, string keyword)
        {
            if (card == null) return CardDef.AbilityContextUnknown;
            int c = AbilityContextOfAbility(card, triggerName, keyword);
            if (c != CardDef.AbilityContextUnknown) return c;
            string tn = string.IsNullOrEmpty(triggerName)
                      ? TriggerNameOfKeyword(keyword)
                      : triggerName;
            if (!string.IsNullOrEmpty(tn) && card.AbilityContextsVersioned)
                return CardDef.AbilityContextPlay;      // v14 只落非 0 ⇒ 缺行 = Play(0)
            return CardDef.AbilityContextUnknown;
        }

        /// <summary>
        /// 🆕 **2026-10-23（引擎线 `W63`）**：引擎侧触发关键词（`KeywordTable.*`）→
        /// 原版 `AbilityTrigger` **枚举名**（`CardAbility.trigger` 的那个名字）。
        ///
        /// 🔴 **这张表的正本是生成器**：`工具/gen_cards_engine.py` 的 `ABILITY_TRIGGER_KEYWORD`
        /// （21 条，`trigger 数值 → keyword`）。本表是它的**反向转写**，两边的键必须逐字对齐 ——
        /// 生成器加/删一行，这里必须同步（`ruleprobe` 与 `CardDatabase` 两侧的
        /// `abilityContext` 落值口径都靠它）。
        /// 判据 → `资料/普查产出_1022/W63_abilityContext_施工简报.md` §3「JSON 形状」。
        ///
        /// ⚠️ **认不出 ⇒ `null`**（⛔ 别退回关键词本身当 trigger 名：原版 trigger 名是
        /// `UnitAttack` / `UseSpiritStone` 这种驼峰枚举名，与引擎关键词不是一套词）。
        /// ⚠️ `EffectResolver.AbilityTriggerNameOfKeyword`（原来只覆盖 4 个词、只服务
        /// `FollowUpSchedulerOf`）现在**转调本方法** ⇒ 判据只此一处。
        /// </summary>
        public static string TriggerNameOfKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            switch (keyword)
            {
                case KeywordTable.Slay:          return "Slay";              // 120
                case KeywordTable.Codex:         return "Codex";             // 145
                case KeywordTable.Sacrifice:     return "Sacrifice";         // 220
                case KeywordTable.AboutToAttack: return "AboutToAttack";     // 280
                case KeywordTable.Mob:           return "Mob";               // 300
                case KeywordTable.Regiment:      return "Regiment";          // 301
                case KeywordTable.Survivor:      return "Survivor";          // 420
                case KeywordTable.Landing:       return "Landing";           // 440
                case KeywordTable.Artifice:      return "Artifice";          // 560
                case KeywordTable.Strike:        return "Strike";            // 580
                case KeywordTable.SpiritStone:   return "UseSpiritStone";    // 600
                case KeywordTable.Pray:          return "Pray";              // 655
                case KeywordTable.Duty:          return "Duty";              // 662
                case KeywordTable.Ambush:        return "Ambush";            // 670
                case KeywordTable.Uprising:      return "Uprising";          // 680
                case KeywordTable.Teleport:      return "Teleport";          // 690
                case KeywordTable.Agenda:        return "Agenda";            // 705
                case KeywordTable.Stimulation:   return "Stimulation";       // 710
                case KeywordTable.Ecstasy:       return "Ecstasy";           // 715
                case KeywordTable.Cruelty:       return "Cruelty";           // 720
                case KeywordTable.Ferocity:      return "Ferocity";          // 725
                default:                         return null;
            }
        }

        /// <summary>
        /// 原版 **skip 表**：这些触发**整段不判** `abilityContext`
        /// （`CardAbility__CanTriggerAbility.c:69-95`）。
        ///
        /// trigger 数值 ↔ 枚举名 = `0 ThisCardPlayed` · `80 ThisUnitDeath` · `145 Codex` ·
        /// `220 Sacrifice` · `500 TriggerEnchantment`。
        /// 引擎侧入队时**不带 trigger 数值**（只有关键词 / trigger 名）⇒ 这里按**名字 + 关键词**认；
        /// 关键词那一半覆盖 `codex` / `backlash`（= `ThisUnitDeath`）/ `sacrifice` 三个
        /// （正是 `RuleCore` C5 现用的三个词，见 `KeywordTable`）。
        /// ⚠️ 两个入参都空 ⇒ **不 skip** ⇒ 走 context（多半 `-1` ⇒ 旧表）—— 宁可多判一次，别漏放行。
        /// </summary>
        static bool TriggerSkipsAbilityContext(string triggerName, string keyword)
        {
            if (!string.IsNullOrEmpty(triggerName))
            {
                if (string.Equals(triggerName, "ThisCardPlayed", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(triggerName, "ThisUnitDeath", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(triggerName, "Codex", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(triggerName, "Sacrifice", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(triggerName, "TriggerEnchantment", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return keyword == KeywordTable.Codex
                || keyword == KeywordTable.Backlash
                || keyword == KeywordTable.Sacrifice;
        }

        /// <summary>
        /// **旧关键词表**（2026-10-21 `W61b` 第 3 条那张）—— 只在 context `-1`（未知）时兜底。
        /// 判据 = 当时现读 `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `abilityContext` 分布：
        /// 只有「**整族都是 `Play(0)`**」的才判（`rally`/`ThisUnitSummoned(195)` · `mob`/`Mob(300)` ·
        /// `slay`/`Slay(120)` · `artifice`/`Artifice(560)` · `abouttoattack`/`(280)`）；
        /// 混着 40/50 的一律不判。⛔ **别按关键词名猜**（`strike` 听着像「在场才能打」，数据里 5 条是 40）。
        /// ⛔ 别删它：旧卡表 / 第二解析路（`ruleprobe` 没接列时）就靠这一条。
        /// </summary>
        public static bool LegacyActorMustBeInPlayForKeyword(string keyword)
        {
            return keyword == KeywordTable.Rally
                || keyword == KeywordTable.Mob
                || keyword == KeywordTable.Slay
                || keyword == KeywordTable.Artifice
                || keyword == KeywordTable.AboutToAttack;
        }

        /// <summary>
        /// 🆕 2026-10-21（审查 `W61b` 第 3 条）：**这个单位此刻还在不在「场上」** ——
        /// 泵点出队前复核施放者用（<see cref="QueuedAction.RequireActorInPlay"/>）。
        ///
        /// 判据（原版 `CardScript__IsInPlay` + `CardAbility__IsRightContext` 的 `Play(0)` 那一支）：
        ///   · 死了（`IsAlive` 假 = 生命归零）⇒ 不算；
        ///   · 不在**任何一方棋盘**上（回手 / 回牌库 / 被移出 / 翻面成残骸时原 `UnitState` 已被换掉）⇒ 不算。
        /// ⚠️ **督军就在 `Board[BoardSpec.WarlordSlot]` 上**（`PlayerState.Warlord` 是同一个对象的
        ///    另一个引用），所以这一趟扫棋盘就覆盖了督军，⛔ 不需要另判。
        /// ⚠️ 「待死」（`ctx.DeferDeaths` 窗口里已经归零、但还没结算死亡）在原版是**状态 5** ——
        ///    它只满足 `InPlayOrDying`，**不满足** `IsInPlay` ⇒ 这里返回 `false` 是对的
        ///    （`IsAlive` 已经是假的：我们的 `Health <= 0` 与「待死」是同一个状态）。
        /// </summary>
        public static bool UnitInPlay(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return true;          // 没有施放者 ⇒ 不关任何单位
            if (!u.IsAlive) return false;
            int ow, sl;
            FindBoardCell(ctx, u, out ow, out sl);
            return ow >= 0;
        }

        /// <summary>
        /// 这个单位此刻在**哪一格**（`owner` / `slot`；找不到 ⇒ 两个都 `-1`）——
        /// 出队复核「载体还在不在原版要求的区域」时定位用。
        /// ⛔ **别缓存入队时的格号**：入队 → 出队之间它可能被挪位 / 抢控制权换边 / 变残骸。
        /// </summary>
        static void FindBoardCell(BattleContext ctx, UnitState u, out int owner, out int slot)
        {
            owner = -1;
            slot = -1;
            if (ctx == null || u == null) return;
            for (int p = 0; p < ctx.Players.Length; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (ReferenceEquals(board[s], u)) { owner = p; slot = s; return; }
            }
        }

        /// <summary>
        /// 🔴🆕 **2026-10-23（引擎线 `W63` 收口）：这条动作的载体**是不是在队列里被人换掉了
        /// （= 原版 `CancelEffectsFromDisappearingUnit` 的 `TransformIntoRemnant` / `transformUnit(27)`
        /// 两路）。泵点出队时唯一保留的「按状态取消」。
        ///
        /// 判据：**同一份 `CardInstance`** 此刻是否在某一方棋盘上、但挂着的**不是入队时那个
        /// `UnitState` 对象** —— 变残骸走的就是这一条
        /// （`RuleCore.cs:8454` 的 `new UnitState(u.Instance, false) { IsRemnant = true }`：
        ///  `Instance` 沿用、对象换新），`transformUnit` 同形。
        /// ⚠️ **找不到那份 `Instance`（死了 / 回手 / 洗回牌库 / 移出棋盘）⇒ 返回 `false`（不取消）** ——
        ///   原版那四个调用点里**没有死亡**，`destroyUnit(45)` / `recallToHand(26)` 是**保留**的
        ///   （判据 → 泵点那段注释）。⛔ 别把它写成「不在场就取消」，那就是 `W63 #2` 的回归。
        /// ⚠️ `Instance` 为 `null`（理论上不会有）⇒ 不取消。
        /// </summary>
        static bool CarrierReplacedByRemnant(BattleContext ctx, UnitState carrier)
        {
            if (ctx == null || carrier == null) return false;
            var inst = carrier.Instance;
            if (inst == null) return false;
            for (int p = 0; p < ctx.Players.Length; p++)
            {
                var board = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = board[s];
                    if (u == null || !ReferenceEquals(u.Instance, inst)) continue;
                    return !ReferenceEquals(u, carrier);     // 同一份牌换了 UnitState
                }
            }
            return false;                                   // 找不到 ⇒ 死亡/离场 ⇒ 不取消
        }

        /// <summary>
        /// 🔴🆕 **2026-10-22（引擎线 `W63` · C1）：原版 `CardAbility.IsRightContext` 的等价物** ——
        /// 「这条 ability 的**载体**此刻是不是待在它要求的那个区域」。泵点出队前复核用
        /// （<see cref="QueuedAction.AbilityContext"/>）。**全仓唯一的一条判据**，⛔ 别在调用点另写。
        ///
        /// 判据（第一权威 = `d:/2/tools/decomp_full/CardAbility__IsRightContext.c` 逐句）：
        /// <code>
        ///   0  Play              :20-32  IsInPlay &amp;&amp; !EnoughPendingDamageToDie &amp;&amp; !isRemnant
        ///   10 Hand              :34-38  IsInHand
        ///   20 Deck              :41-47  cardState ∈ {inDeck(0), inDeckDrawing(16)}
        ///   30 Cemetery          :51-55  cardState == 6
        ///   40 InPlayOrDying     :52-55  状态 {2,3,15,17,**5 待死**} —— 🔴 **含待死**
        ///   50 InPlayOrCementery :66-72  IsInCemetery || IsInPlay —— 🔴 **不含待死**（与 40 不等价）
        /// </code>
        ///
        /// 🔴 **`-1`（未知）⇒ 返回 `true`**（= 不拦）：让调用方**回退旧关键词表**
        /// （`UnitInPlay` / `QueuedAction.RequireActorInPlay` 那一套）。⛔ 别把「未知」读成 `Play(0)`。
        ///
        /// ⚠️ **`slot` 只对 0/40/50 有意义**（棋盘格号）；10/20/30 是**按 `Instance` 查三个区域**。
        /// 所以多出 `carrier` 一参：载体离开棋盘之后（死亡 / 回手 / 洗回牌库）格子上就找不到它了，
        /// **只有 `UnitState.Instance` 还认得出那一份**（readonly；翻面成残骸也沿用同一个）。
        /// `carrier == null` ⇒ 现从 `Board[owner][slot]` 取；`owner` 认不出（`-1`）⇒ 两个玩家的区域都查
        /// （`Instance` 一局唯一，不会认错）。
        /// ⚠️ **`owner`/`slot` 对不上 `carrier`** ⇒ 那一格**已经换人** ⇒ 按「不在棋盘」处理
        /// （⛔ 别只看「格子非空」就放行）。
        ///
        /// 其它值（数据坏）⇒ **出声**（`LogError`）后按未知处理（返回 `true`）。
        /// </summary>
        public static bool IsRightContext(BattleContext ctx, int owner, int slot, int context,
                                          CardInstance carrier = null)
        {
            if (ctx == null) return true;                              // 没有 ctx ⇒ 不拦（与 UnitInPlay 同口径）
            if (context == CardDef.AbilityContextUnknown) return true; // `-1` 契约：回退旧表

            UnitState cell = null;
            if (owner >= 0 && owner < ctx.Players.Length && slot >= 0 && slot < BoardSpec.Size)
                cell = ctx.Players[owner].Board[slot];
            CardInstance inst = carrier != null ? carrier : (cell != null ? cell.Instance : null);
            if (cell != null && inst != null && !ReferenceEquals(cell.Instance, inst))
                cell = null;                                          // 那一格已经换人

            switch (context)
            {
                case CardDef.AbilityContextPlay:
                    // 活着 + 在棋盘 + 未待死 + **不是残骸**。
                    // `IsAlive` 补的是「血已归零、还没进待死队列」那一格。
                    // 🆕 2026-10-24（引擎线 `A1` 第 5 件）：**`+0x65 isRemnant == 0` 这一道补上了** ——
                    //   原版 `CardAbility__IsRightContext.c:20-30` 的 `Play(0)` 支逐句是：
                    //   `IsInPlay(card)` ∧ `!EnoughPendingDamageToDie(card)` ∧
                    //   `*(char *)(card + 0x65) == 0`；而 `+0x65` 就是 `EntityScript.isRemnant`
                    //   （`dump.cs:22001`），`CheckIfMeetsCriteria.c:255-261` 的 `includeRemnants`
                    //   读的也是同一格。⇒ 残骸**不算** `Play(0)` 的合法载体。
                    //   ⚠️ 今天零可达（残骸是 `new UnitState(...){IsRemnant=true}`，不带 ability），
                    //   本条堵的是形状（⛔ 不是「没判据所以不做」）。
                    return cell != null && cell.IsAlive && !ctx.IsPendingDeath(cell) && !cell.IsRemnant;
                case CardDef.AbilityContextInPlayOrDying:
                    return cell != null;                              // 🔴 含待死（状态 5 也算在场）
                case CardDef.AbilityContextInPlayOrCemetery:
                    return (cell != null && !ctx.IsPendingDeath(cell)) // 🔴 不含待死
                        || InZone(ctx, owner, inst, Zone.Discard);
                case CardDef.AbilityContextHand:     return InZone(ctx, owner, inst, Zone.Hand);
                case CardDef.AbilityContextDeck:     return InZone(ctx, owner, inst, Zone.Deck);
                case CardDef.AbilityContextCemetery: return InZone(ctx, owner, inst, Zone.Discard);
                default:
                    UnityEngine.Debug.LogError(
                        $"[ActionPump] 非法 `abilityContext={context}`（合法 0/10/20/30/40/50 或 -1）—— "
                        + "按「未知」处置（放行）；判据 → Core/ActionPump.cs 的 IsRightContext doc");
                    return true;
            }
        }

        /// <summary>三个**非棋盘**区域（`PlayerState.Hand` / `Deck` / `Discard`）。</summary>
        enum Zone { Hand, Deck, Discard }

        /// <summary>这一份牌是不是在某一方的某个区域里（`IsRightContext` 的 10/20/30/50 用）。</summary>
        static bool InZone(BattleContext ctx, int owner, CardInstance inst, Zone zone)
        {
            if (inst == null) return false;              // 认不出是哪一份 ⇒ 不能确认 ⇒ 从严
            if (ctx == null) return true;
            if (owner >= 0 && owner < ctx.Players.Length)
                return ListOf(ctx.Players[owner], zone).Contains(inst);
            for (int p = 0; p < ctx.Players.Length; p++) // 载体已离场 ⇒ 两边都查
                if (ListOf(ctx.Players[p], zone).Contains(inst)) return true;
            return false;
        }

        static System.Collections.Generic.List<CardInstance> ListOf(PlayerState ps, Zone zone)
        {
            return zone == Zone.Hand ? ps.Hand : (zone == Zone.Deck ? ps.Deck : ps.Discard);
        }

        /// <summary>`abilityContext` 的人话名（日志用；未知/非法 ⇒ 原数字）。</summary>
        static string AbilityContextName(int context)
        {
            switch (context)
            {
                case CardDef.AbilityContextPlay:             return "Play(0)";
                case CardDef.AbilityContextHand:             return "Hand(10)";
                case CardDef.AbilityContextDeck:             return "Deck(20)";
                case CardDef.AbilityContextCemetery:         return "Cemetery(30)";
                case CardDef.AbilityContextInPlayOrDying:    return "InPlayOrDying(40)";
                case CardDef.AbilityContextInPlayOrCemetery: return "InPlayOrCemetery(50)";
                default:                                     return "?(" + context + ")";
            }
        }

        /// <summary>按**卡 + 触发关键词**取这条 ability 的原版道（`CardAbility.priority`）。
        /// 缺数据 ⇒ `1`（Instant）。⚠️ 非法值（数据坏）⇒ 原样返回，由 `ActionQueue.Enqueue` 出声丢弃。</summary>
        public static ActionPriority TriggerLane(UnitState u, string keyword)
        {
            return TriggerLaneOfCard(u != null ? u.Card : null, keyword);
        }

        /// <inheritdoc cref="TriggerLane(UnitState,string)"/>
        public static ActionPriority TriggerLaneOfCard(CardDef card, string keyword)
        {
            int p = card != null ? card.TriggerPriority(keyword) : CardDef.DefaultTriggerPriority;
            return (ActionPriority)p;
        }

        /// <summary>
        /// **事件监听器那一条 ability 的道**（`When … , …` / `At the end of your turn` / 常驻 / 部署 / 信仰）。
        ///
        /// <para>🔴🆕 **2026-10-23（引擎线 `P1·①`）起走数据**：`cards_engine.json` v18 的
        /// `abilityTriggerPriority`（键 = **原版 `AbilityTrigger` 枚举名**）→
        /// <see cref="CardDef.AbilityTriggerPriority"/>。**缺键 / 认不出 / 旧表 ⇒ 原版默认 1 = Instant**
        /// （与 <see cref="TriggerLaneOfCard"/> 同一条缺省契约）。</para>
        ///
        /// <para>**为什么不能拿 `TriggerPriority(keyword)` 顶上**：那一列的键是**引擎触发关键词**
        /// （`artifice` / `codex` / `slay` …，21 个），而监听器这一族走的是 `WhenEventKind`
        /// （`play` / `deploy` / `die` / `damaged` …）—— **两套词**，查不到任何东西。v18 另落一列，
        /// 键 = 原版 trigger 名（`OtherUnitDeath(90)` / `UnitReceivesMarkOfChaos(510)` /
        /// `FollowUpResolution(410)` …），与 `abilityContext`（v14）同一套主键。</para>
        ///
        /// <param name="card">监听者那张卡。`null`（常驻效果没有载体 / 已离场）⇒ 默认道。</param>
        /// <param name="triggerName">这条监听能力对应的**原版 `AbilityTrigger` 枚举名**。
        /// ⚠️ **只有判据确定的调用点才传** —— 传 `null` = 「认不出是哪条 ability」⇒ 默认道
        /// （⛔ 不许按措辞猜一个名字：`RuleEngineTest` 与 `EffectResolver` 的回合族注释都记了
        /// 「同一措辞落在不同 trigger 号上」这条实测）。</param>
        /// </summary>
        public static ActionPriority ListenerLane(CardDef card, string triggerName = null)
        {
            if (card == null || string.IsNullOrEmpty(triggerName))
                return (ActionPriority)CardDef.DefaultTriggerPriority;
            return (ActionPriority)card.AbilityTriggerPriority(triggerName);
        }

        /// <summary>
        /// **泵点** —— 原版 `BattleManager.Update` 那一段的等价物：
        /// `Resolving` 为假时按 **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**
        /// 一条一条取、结算，直到四道全空。
        ///
        /// 🔴 **`Resolving` 为真就立刻返回**（原版 `Update` 同一条闸）⇒ 动作内部调到这里是**空转**。
        /// 🔴 每结算一条，把它自己的 `Resolving` 置真/置假 —— 于是**它内部产生的触发只入队**，
        ///   由**本循环**接着取（原版下一帧取，次序相同）。
        /// 🔴🆕 **取出来之后、结算之前只复核原版那几种「主动取消」** ——
        ///   有 `AbilityContext`（数据）时只看「载体是不是被换成了残骸 / 被 transform」
        ///   （<see cref="CarrierReplacedByRemnant"/>）；`-1` 未知才回退 `RequireActorInPlay` 那一支。
        ///   ⚠️ **2026-10-23（`W63` 收口）起 `abilityContext` 的主闸在【入队那一刻】**
        ///   （`EnqueueAbility`），不在这里 —— 见下面那段判据。
        /// ⚠️ `ctx.IsOver` ⇒ 停止泵队（原版 `duringBattleFlag` 为假时 `Update` 也不再取）。
        ///   剩下的条目**保留在队列里**并如实记一条日志（⛔ 不静默丢）。
        /// </summary>
        public static void PumpActionQueues(BattleContext ctx)
        {
            if (ctx == null) return;
            var q = ctx.ActionQueue;
            if (q.Resolving) return;
            // 原版 `Update` 只在 `duringBattleFlag || duringScriptedActionsFlag` 时才泵
            // （`Update.c:1250`）⇒ 对局结束就不再取队（剩下的条目保留，由下面那句如实记）。
            if (ctx.IsOver) return;

            int n = 0;
            ActionPriority lane;
            QueuedAction act;
            while (!q.Resolving && q.TryDequeue(out lane, out act))
            {
                if (++n > MaxPumpedActionsPerDrain)
                {
                    UnityEngine.Debug.LogError(
                        $"[ActionQueue] 一次泵队超过 {MaxPumpedActionsPerDrain} 条 —— 判定为触发环，"
                        + $"**已清空四道并停止泵队**（剩余 {q.Count} 条被丢弃；最后一条 = 「{act}」）");
                    q.Clear();
                    return;
                }
                // ============================================================
                //  🔴 出队时**只**复核原版那四种「主动取消」（`W63` 收口，2026-10-23）
                //
                //  🔴🆕 **就地改口径（铁律 5）**：2026-10-22（`W63` · C1）这里曾经按
                //  `abilityContext` 六档复核「载体还在不在原版要求的区域」。**那个判点错了** ——
                //  原版 `IsRightContext` 只有一个调用者 `CardAbility.CanTriggerAbility`，
                //  而它是 `RawCardScript.TriggerAbilities` 在**广播那一刻同步调**的
                //  （`RawCardScript__TriggerAbilities.c:108,207`）⇒ **原版闸在入队前**；
                //  入了队的动作**死亡不会取消**它（`BattleManager.CancelEffectsFromDisappearingUnit`
                //  全库只有四个调用点：`removeEffect(16)` / `jamUnit(22)` / `transformUnit(27)` /
                //  `TransformIntoRemnant`，而且明确**保留** `destroyUnit(45)` / `recallToHand(26)`）。
                //  我们做在出队 ⇒ 比原版更严：「入队时还在、被后面动作打死」原版照跑、我们取消
                //  —— `W63 #2`（Slay 正文整条丢失）就是这个偏差的产物。
                //  ⇒ **主闸已搬到 `EnqueueAbility`（入队那一刻）**，这里只留「主动取消」那一半。
                //
                //  今天建模得到的「主动取消」= **变残骸 / transform**：同一份 `CardInstance`
                //  在棋盘上被换成了**另一个 `UnitState`**（`RuleCore.CleanupDeaths` 里
                //  `new UnitState(u.Instance, …) { IsRemnant = true }`，`RuleCore.cs:8454`）
                //  ⇒ <see cref="CarrierReplacedByRemnant"/> 认得出。
                //    · `jamUnit` / `transformUnit` / `removeEffect` 三路：现池**零可达**
                //      （`jam` 只在 `ResolveAction.c:2574` 的动画四条道上，我们没有对应建模）
                //      ⇒ 如实记在报告里，⛔ 不是「不用做」，是「还没有判据把它接出来」。
                //    · **死亡 / 回手 / 洗回牌库 ⇒ 不取消**（原版保留 `destroyUnit` / `recallToHand`）。
                //
                //  ⚠️ **没有施放者（`act.Actor == null`）⇒ 不判**：与 `UnitInPlay(null)` 同口径。
                //     `-1`（旧卡表 / 第二解析路没接列）⇒ **原样回退**旧口径
                //     `RequireActorInPlay && !UnitInPlay(...)`（= `Play(0)` 的近似）。
                //     🔴 **例外（2026-10-24 `F1`）**：同时还传了 `RequireActorInPlayOrDying`
                //     （今天只有 Slay）⇒ **不用旧口径**，改由下面那道更宽的 `IsOnBoardOrDying` 判。
                // ============================================================
                if (act != null && act.Actor != null
                    && act.AbilityContext != CardDef.AbilityContextUnknown)
                {
                    if (CarrierReplacedByRemnant(ctx, act.Actor))
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[ActionQueue] **取消**一条待结算动作「{act.Label}」（{act.Priority} 道）—— "
                            + $"载体「{act.Actor.Name}」**已变成残骸 / 被 transform**（原版 "
                            + "`BattleManager.CancelEffectsFromDisappearingUnit` 的 "
                            + "`TransformIntoRemnant` / `transformUnit(27)` 两路；"
                            + "⛔ 不是静默丢，这条就是出声口）");
                        continue;
                    }
                }
                // 🔴🆕 **2026-10-24（`F1` · 「Slay 正文整条丢失」那一格的根因）**：
                //   `RequireActorInPlayOrDying` 那一道（下面 :753）**比这一支宽** ——
                //   `IsInPlayOrDying` 把 `waitingToDie(5)` 也算在场，而这一支的 `UnitInPlay`
                //   只认「还站在棋盘上」。`abilityContext=-1`（合成夹具卡 / 旧卡表）时这一支是
                //   `Play(0)` 的**旧口径回退**，**与 OrDying 闸同时在** ⇒ 谁先 `continue` 谁说了算，
                //   而这里是它先 ⇒ 「凶手在反噬里被打死、Slay 出队前已从棋盘摘下」那一格被**这一支**
                //   取消（实测 `W58ILBlaster·slay` 的取消警告走的就是 `Play(0)` 旧口径那句）。
                //   原版 `BattleManager__ResolveTriggerSlay.c:116-226` 在出队时判的是
                //   `CardScript__IsInPlayOrDying()`（`CardScript__IsInPlayOrDying.c:8-13`，
                //   `state ∈ {2,15,3,17,5}`）⇒ **待死照样触发**。⇒ 传了 OrDying 就不再走这一支，
                //   交给下面 :753 那一闸（`IsOnBoardOrDying` = 棋盘 ∨ `IsPendingDeath` ∨ `DyingThisAction`）。
                else if (act != null && !act.RequireActorInPlayOrDying
                         && act.RequireActorInPlay && !UnitInPlay(ctx, act.Actor))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] **取消**一条待结算动作「{act.Label}」（{act.Priority} 道）—— "
                        + $"施放者「{(act.Actor != null ? act.Actor.Name : "?")}」已经离场"
                        + "（原版 `CardAbility.IsRightContext` 的 `Play(0)` 同样不结算；"
                        + "⚠️ 这一支是 `abilityContext=-1` 的**旧口径回退**；"
                        + "⛔ 不是静默丢，这条就是出声口）");
                    continue;
                }
                // ============================================================
                //  🔴🆕 **2026-10-23（`Slay` 就地结算复核）：Slay 专属的出队复核**
                //
                //  判据 = `BattleManager__ResolveTriggerSlay.c:116-226`：出队时
                //  `CardScript__IsInPlayOrDying()` 为假 ⇒ **取消这一条**（`CardScript__TriggerSlay` 不跑）。
                //  `IsInPlayOrDying`（`CardScript__IsInPlayOrDying.c:8-13`）放行
                //  `state ∈ {2,15,3,17,5}` —— **`waitingToDie(5)` 也算在场** ⇒ 它比
                //  `abilityContext = Play(0)` 宽：凶手刚被打死（待死 / 同一条顶层动作里已判死）
                //  **照样要触发**，只有「真的不在这局里了（回手 / 回牌库 / 已处理完）」才取消。
                //  ⛔ 这不是第二份 `abilityContext`：那一闸在入队（`EnqueueAbility`），这一闸在出队，
                //     两个判点都是原版明写的（`TriggerAbilities` 广播时 / `ResolveTriggerSlay` 出队时）。
                // ============================================================
                if (act != null && act.RequireActorInPlayOrDying && act.Actor != null
                    && !IsOnBoardOrDying(ctx, act.Actor))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] **取消**一条待结算动作「{act.Label}」（{act.Priority} 道）—— "
                        + $"施放者「{act.Actor.Name}」已不在棋盘上、也不处于「待死」（原版 "
                        + "`BattleManager__ResolveTriggerSlay` 的出队复核 `CardScript.IsInPlayOrDying` "
                        + "同样不结算 · `CardScript__IsInPlayOrDying.c:8-13`；"
                        + "⛔ 不是静默丢，这条就是出声口）");
                    continue;
                }
                q.Resolving = true;
                // 🆕 2026-10-22（`W62` · R2）：**额外触发**那一条在它**结算期间**顶起重入守卫
                //   （判据 → `QueuedAction.ExtraTrigger` 的 doc：守卫必须盖住「它再广播一遍」的那一刻，
                //    而不是「它入队」的那一刻）。
                if (act != null && act.ExtraTrigger) ctx.ExtraTriggerDepth++;
                try
                {
                    // 🔴🆕 **2026-10-22（`W62` · R2 · Slay 交错那一格）：一条动作里死的人，
                    //   攒到这条动作**跑完**再结算**（原版：`CardScript__CheckIfDead` 判死那一刻
                    //   只 `AddAutoActionToQueue` 一条 death action，它排在本动作**之后**）。
                    //   不加这一层，队列体里打死的单位会**当场**跑完整条 `CleanupDeaths`
                    //   （`EvtKind.Death` 信号、被移除棋盘…）⇒ 「外层动作的 Slay 记账」与
                    //   「嵌套死亡的死讯」的先后会被抹掉（`TestW58NestedDeathInterleave` ①② 的红）。
                    //   ⇒ `DeathBatch.Dispose` → `FlushDeaths`：把本条动作攒下的死者逐个处理，
                    //   **它们的死亡触发 / 记账一律再入队到队尾**（FIFO = 原版 `[A,B,C]`）。
                    using (SimultaneousDamage(ctx))
                    {
                        if (act != null && act.Execute != null) act.Execute();
                    }
                }
                finally
                {
                    if (act != null && act.ExtraTrigger) ctx.ExtraTriggerDepth--;
                    q.Resolving = false;
                }
                if (ctx.IsOver)
                {
                    if (q.Count > 0)
                        ctx.Log($"[ActionQueue] 对局已结束 —— 停止泵队，队列里还剩 {q.Count} 条不结算");
                    return;
                }
            }
        }
    }
}
