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
                // 🔴🆕 **2026-10-25（`E1` · 第三段 · 「240 `WhileInPlayVariable` 的重挂点」）**：
                //   **一条顶层动作结束 ⇒ 重算一次全场「在场」派生状态**。
                //   判据 = `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §1·4 第 1 条：
                //   原版 `BattleManager__FinishResolvingAction.c` 尾部 `BroadcastWhileInPlay(this, false, null)`
                //   → `CardScript.UpdateWhileInPlay` → **连发 `WhileInPlay(20)` 与 `WhileInPlayVariable(240)` 两档**
                //   （`RawCardScript.cs:1239-1253`），每次都是「**先摘掉此前加的全部、再重加一份**」。
                //   ⇒ 这正是 `*ValueOptions` 那种**快照会过期**的效果的刷新机制（§1·1/§1·2：折叠发生在
                //     `PlayAbility`，存进 `activeEffects` 的是数字；靠重挂重新折叠）。
                //
                //   ⚠️ **为什么挂在这里、不挂在 `PumpActionQueues` 里**：那个函数一次动作会被调很多次
                //     （构造函数一次 + 队列非空就循环）⇒ 那是「一动作多播」。这个 `Dispose` 对
                //     **最外层**动作只跑一次 —— 与 `FinishResolvingAction` 一一对应。
                //   ⚠️ **幂等**：`Auras.Recompose` 的形状本来就是「整份摘掉再重加」（`Aura.cs` 的 doc），
                //     多跑一次不改结果，只是把「来源在场的加成」重新快照一遍。
                //   ⚠️ 顺序：**先 Recompose 再 `EmitWhileInPlayPass`** —— 表现口要看刷新后的状态。
                //   ⚠️ **2026-10-25（`E1` · 第三段·第 2 版）**：这里**只跑「近战=生命」那一小趟刷新**，
                //     不跑整个 `Auras.Recompose` —— 实测整跑会把 `BL18` / `UM18` 两张**光环卡**
                //     的结算读数改掉（`net` 各 +4），根因是 `Recompose` 是「整份摘掉再重加」，
                //     而它同时会重算费用/关键词/誓约授予那一串，**超出了「重挂 240 快照」这件事的范围**。
                //     判据只需要「240 的快照在动作边界刷新」，`RefreshMeleeEqualsHealth` 就是那一趟
                //     （只遍历 `Card.MeleeEqualsHealth` 的单位；池内 1 张 = `SAU27`）。
                Auras.RefreshMeleeEqualsHealth(_ctx);
                // 🔴🆕 **2026-10-26（批次 4 · `R5-F1`，判据由 `F3` 第 4 轮报告 §2·9 / §六·② 给全）：**
                //   `FinishResolvingAction` 尾部原版 `BroadcastWhileInPlay(this, false, null)` 连发
                //   `WhileInPlay(20)` 与 **`WhileInPlayVariable(240)`** 两档。240 那一档的形状是
                //   `RemoveExtrinsicEffectsFrom` + 重新 `AddEffect` ⇒ 快照型 `*ValueOptions` 靠它重折叠。
                //   `Auras.RefreshInPlayNumericAndKeywords(_ctx)` 是**零副作用**版：与 `Recompose`
                //   共用 `RecomposeCore` 与同一趟迭代，只多 `numericOnly` 开关（**跳过费用登记 + 誓约授予**，
                //   `Health` 一个字节都不动，整段包在 `BeginAuraRecompute`/`EndAuraRecompute` 里）。
                //   ⛔ **不要换成 `Auras.Recompose`** —— 它是「先全清再加回」，会重放夹血 / 费用重登记 /
                //   誓约授予（实测把 `BL18` / `UM18` 两张光环卡的 `net` 各改了 **+4**）。
                //   ⚠️ 位置 = `EmitWhileInPlayPass` **之前**（表现口要看刷新后的状态）。
                // 🔴🆕 **2026-10-26（批次 6 · `G14` 任务①）：上面那条「不能接」的结论【已作废】，本行现在是【已接】。**
                //   历史（⛔ 别删，这是踩过的坑）：批次 4 `F1` 隔离实验发现接上它会打掉「减价光环」——
                //     · 接上 ⇒ `play check` 指纹 **b362b4da… ≠ 基线 39c4f77e…**（rc=1）：`①-a` 主档 2 张
                //       （BL27 `Your Stratagems cost 1 less` · SW18 `Friendly Beasts cost 1 less`）、`①-b` 电池 7 张
                //       （AM15 / GSC30 / BL27 / BL18 …），全部坐在 `💰 手牌总价` 一个量上（`7 → 0` 变 `7 → 1`）。
                //   根因（👈 批次 6 已坐实，不再只是推断）：`RecomposeCore` 的 ①「整份摘掉」里那两行
                //     **无条件**清 `ctx.CostMods` 里 `Tag == GrantTag` 的记录，而**重新登记**的 ② 段被
                //     `numericOnly` 跳过 ⇒ **清了不重加** ⇒ 减价光环整层消失。
                //   修法（`G14` 已落）：`Aura.cs` 那两行加 `!numericOnly` ⇒ **重算路径整层不碰费用档**
                //     （口径 = `CLAUDE.md` §三「重算必须零副作用：不改 `Health`、**不重登记费用**、不重授誓约」；费用档注册是 `f(棋盘, ctx.Active, AuraSpecs)` 的纯函数，棋盘变动那趟 `Recompose` 已刷新）。
                //   验收：接上本行后 `play check` 指纹**逐字**回基线 `39c4f77e5b2a76f404c241beef94228c`、rc=0
                //     （隔离口径：本批其余改动全开，只差这一行）。
                //   ⚠️ `ActionPump.cs` 本批原本无人写 ⇒ 这一行由 `G14` 打开（简报明确授权，见报告 §①）。
                //   ⛔ 仍**不要**换成 `Auras.Recompose` —— 它会重放夹血 / 重授誓约（`BL18`/`UM18` 的 `net` +4）。
                Auras.RefreshInPlayNumericAndKeywords(_ctx);   // ✅ 2026-10-26（批次 9 · 执行代理 `E7P06` · 账 669 · 本行【已打开】）：判据 = `FinishResolvingAction` 尾部 `BroadcastWhileInPlay` 连发 20 / **240** 两档（原版 `RawCardScript.cs:1239-1253`），240 = 「先摘再重加」的快照刷新 ⇒ 就挂在这里（`EmitWhileInPlayPass` **之前**）。⚠️ 上一格阻塞（批次 6 `G14`：接上后 `BL18` / `UM18` 的电池读数各 **+4** ⇒ 指纹 `4865c48b… ≠ 39c4f77e…`）由**调度台**裁定带入基线（⛔ 子代理不重钉基线）；隔离读数与逐条解释 → `报告/B9_E7P06_ActionPump与CountEval.md` §三。· 🔴 2026-10-27（B9 · `W18AR`）：本行**不动** `Exhausted` / `CannotAttackThisTurn` —— 窄口刷新是「摘了又原样挂回」，`UnitState.EndAuraRecompute` 只在 `fast`/`flank` 豁免**真的丢了**（光环来源离场）时才重算（判据与理由 → `UnitState` 类尾那段「光环重算周期内 act 两位的重算时机」）；⛔ 别为了「让刷新别碰这两位」把本行注释回去（那是账 669 的接线）。

                // ---- 🆕 2026-10-24（引擎线 `G2` · `C4` D9）：`WhileInPlay`(20) 的表现口 ----
                //  落点在**这里**（= 一条**顶层动作**结束、队列已排空），对原版
                //  `BattleManager__FinishResolvingAction.c:91`（闸 = `ActionsLeftInQueue() < 1`）。
                //  ⛔ **别放进 `PumpActionQueues`**：`ActionScope` 的**构造函数**也会调它一次
                //  （`:87` 结掉上一轮残留）⇒ 那样一次动作会发**两条**（`G2` 探针实测 2 / 2）。
                //  🔴 **2026-10-25 订正（`E1` · 第三段）**：本行原来还写「⛔ 也别挂 `Aura.Recompose`」——
                //     那是针对「挂进 `PumpActionQueues`」说的（一动作多次）；**挂在 `Dispose` 里是正确的**
                //     （判据 §1·4 第 1 条 `FinishResolvingAction` 尾部 `BroadcastWhileInPlay`），
                //     已在本行**上面**加上。⛔ 别把它挪进 `PumpActionQueues`。
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
        /// <param name="abilityTrigger">
        /// 🆕 **2026-10-25（执行代理 `E2` · `A3-2`）**：这条动作**是哪一条 ability** —— 原版
        /// `AbilityTrigger` **枚举名**（`UnitAttack` / `OtherUnitDeath` …，与
        /// <see cref="TriggerNameOfWhenKind"/> / <see cref="CardDef.AbilityTriggerNumber"/> 同一套词）。
        /// 与 <paramref name="abilityTarget"/> 一起给，才会启用「ability 级 `targetCriteria`」那道闸
        /// （判据 → <see cref="AbilityLevelCriteriaAllows"/> 的 doc）。
        /// ⚠️ **缺省 `null` = 不判**（今天绝大多数调用点）—— ⛔ 别用「关键词」顶它（不是一套词）。
        /// </param>
        /// <param name="abilityTarget">
        /// 🆕 同上：原版 `CanTriggerAbility` 的 **`targetCard`**（🔴 **不是 `actingCard`** ——
        /// 那一格是第一道闸 `actingCardCriteria` 的候选）。
        /// `null` = 本路径没有点目标 ⇒ 那道闸**判不了 ⇒ 放行 + 出声**（原版此跳会 NRE）。
        /// </param>
        /// <param name="abilityActingCard">
        /// 🆕 **2026-10-25（执行代理 `E3` · `E2` 报告 §六）**：原版 `CanTriggerAbility` 的
        /// **`actingCard`** —— **第一道闸**（`actingCardCriteria` `+0x28`）的被筛对象。
        /// ⚠️ 只有 <paramref name="abilityActingGate"/> 为 `true` 时才看这一格
        /// （= 这条入队路径**已经查清** `actingCard` 是哪一个 `UnitState`）。
        /// `null` = 这条路径**拿不到** `actingCard`（例如没有来源的死亡：毒 / 反噬 / 反击 …）
        /// ⇒ 那一道闸**判不了 ⇒ 放行 + 出声**（⛔ 不许拿 `subject` / `actor` 顶上去猜）。
        /// </param>
        /// <param name="abilityHostCard">
        /// 🆕 **2026-10-25（执行代理 `E9` · `J5` §二 F5 / §三·2）：这条 ability 的【宿主卡】**
        /// —— 原版 `CanTriggerAbility(thisCard, …)` 的 `thisCard` 所对应的 `CardDef`。
        /// <para>为什么需要它：没有 <paramref name="actor"/> 的路径（**战术 / 防御卡**）原来整道
        /// ability 级闸被静默跳过；而那一跳的 `thisCard` 在原版**就是那张战术牌自己**
        /// （判据逐句：`RawCardScript__OnCardPlayedWithTarget.c:40-50` →
        /// `TriggerAbilities(0, manager, thisCard=param_3, cardPlayed=param_4, targetCard=param_5)`；
        /// `RawCardScript__TriggerAbilities.c:207` → `CanTriggerAbility(ability, trigger, manager,
        /// <b>thisCard=param_4</b>, <b>actingCard=param_5</b>, <b>targetCard=param_6</b>, …)`；
        /// 形参表 `d:/2/tools/il2cpp_out/dump.cs:20360/23463/23475`）⇒ 战术牌**自己发、自己是宿主**。</para>
        /// 缺省 `null` ⇒ 回退 `actor.Card`（单位那条路**行为逐字不变**）。
        /// </param>
        /// <param name="abilityOwnerSeat">
        /// 🆕 同上：**宿主卡属于哪个座位**（原版 `thisCard.isPlayer`）。
        /// 只在 <paramref name="actor"/> 为 `null`（战术 / 防御卡）时用 —— 那一路求不出 `FindBoardCell`。
        /// 缺省 `-1` = 不知道 ⇒ 那道闸**判不了 ⇒ 放行 + 出声**（⛔ 不静默跳过）。
        /// </param>
        /// <param name="abilityActingGate">
        /// 🆕 同上：**这条入队路径要不要跑第一道闸**。缺省 `false` = 不判（今天绝大多数调用点）。
        /// 🔴 **判据 = 原版 `CardAbility__CanTriggerAbility.c:127-130` 的 `actingCard` 到底是哪一格**——
        /// 只有逐族查清过的那两族才置 `true`（`Attack` = 攻击者；`Die` = `CleanupDeaths` 的
        /// `actingCard` 形参，= 原版 `BattleAction.actingCard` / `DamageInfo.actingCard` = **凶手**，
        /// ⛔ **不是死者**：`RawCardScript.OnMinionDeath(manager, thisCard, actingCard, deadCard, …)`
        /// 里 `actingCard` 与 `deadCard` 是**两个不同的形参**）。
        /// </param>
        public static bool EnqueueAbility(BattleContext ctx, ActionPriority lane, string label,
                                          System.Action body,
                                          UnitState actor = null, bool requireActorInPlay = false,
                                          bool extraTrigger = false,
                                          int abilityContext = CardDef.AbilityContextUnknown,
                                          bool requireActorInPlayOrDying = false,
                                          string abilityTrigger = null,
                                          UnitState abilityTarget = null,
                                          UnitState abilityActingCard = null,
                                          bool abilityActingGate = false,
                                          CardDef abilityHostCard = null,
                                          int abilityOwnerSeat = -1, int actionType = 0)
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
            //  🔴 **2026-10-24（执行代理 `W2` · `C5` Q3）就地订正**：原来这里写「只在能定位载体时判」
            //     —— 那一句让 `Hand(10)/Deck(20)/Cemetery(30)` 三档（17 行：`Hand` 16 · `Cemetery` 1）
            //     **永远不过闸**（载体按定义不在棋盘上 ⇒ `FindBoardCell` 恒 -1）。
            //     现在：**三档区域判据传 `-1/-1`**（`InZone` 自己两边扫），棋盘三档用现查格号
            //     （`co < 0` ⇒ 那三档自然不通过 —— 与原版「按卡状态判 ⇒ 多半不触发」同向）。
            //     ⚠️ `-1`（未知 / 旧卡表 / 第二解析路没接列）⇒ **一个字都不判**，回退
            //     `requireActorInPlay` 那条旧口径（在泵点），与改前逐字相同。
            // ============================================================
            if (abilityContext != CardDef.AbilityContextUnknown && actor != null)
            {
                int co, cs;
                FindBoardCell(ctx, actor, out co, out cs);
                // 🔴 2026-10-24（执行代理 `W2` · `C5` Q3）：**`Hand/Deck/Cemetery` 三档也要判**。
                //   改前只有「能在棋盘上定位到载体」时才调 `IsRightContext` ⇒ 那三档的载体**按定义
                //   不在棋盘上**（`FindBoardCell` 恒 `-1`）⇒ 那 17 行（`Hand` 16 · `Cemetery` 1）
                //   **永远不过闸**（静默放行）。而 `IsRightContext` 本来就支持这三档
                //   （它用 `Instance` 在 `ps.Hand/Deck/Discard` 里找，`InZone`），缺的只是调用点
                //   **没传 `-1/-1`**。
                //   判据（第一权威）：原版 `CardAbility__IsRightContext.c` 的六档逐句
                //   （0 Play / 10 Hand / 20 Deck / 30 Cemetery / 40 InPlayOrDying / 50 InPlayOrCemetery）；
                //   调用点唯一 = `CardAbility.CanTriggerAbility`，由 `RawCardScript.TriggerAbilities`
                //   在广播那一刻同步调（`:108` / `:207`）。
                //   ⇒ 口径：**`context != Unknown && actor != null` 一律调 `IsRightContext`**；
                //     棋盘三档（0/40/50）用**现查到的格号**（`co < 0` ⇒ 那三档自然不通过），
                //     区域三档（10/20/30）传 `-1/-1`（`InZone` 会自己两边扫）。
                bool boardContext = abilityContext == CardDef.AbilityContextPlay
                                 || abilityContext == CardDef.AbilityContextInPlayOrDying
                                 || abilityContext == CardDef.AbilityContextInPlayOrCemetery;
                bool ok = boardContext
                    ? IsRightContext(ctx, co, cs, abilityContext, actor.Instance)
                    : IsRightContext(ctx, -1, -1, abilityContext, actor.Instance);
                if (!ok)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] **不触发**「{label}」—— 载体「{actor.Name}」此刻不在原版 `abilityContext="
                        + AbilityContextName(abilityContext) + "` 要求的区域"
                        + (boardContext
                            ? ""
                            : "（这一档是 `Hand/Deck/Cemetery`，判据 = `CardAbility__IsRightContext.c` 的 "
                              + "`IsInHand`/`IsInCemetery`，按 `Instance` 在三个区域里找）")
                        + "（原版 `CardAbility.CanTriggerAbility` → `IsRightContext` 在**入队前**判，"
                        + "判据 `RawCardScript__TriggerAbilities.c:108,207`；⛔ 不是静默丢，这条就是出声口）");
                    return false;
                }
            }
            // ============================================================
            //  🔴🆕 **2026-10-25（执行代理 `E3` · `E2` 报告 §六）：ability 级 `actingCardCriteria`
            //  的触发前【第一道闸】**
            //
            //  判据（第一权威 = 全量反编译）：`CardAbility__CanTriggerAbility.c:127-130`
            //    `CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`
            //  ⇒ 原版**紧接在 `IsRightContext` 之后、`targetCriteria` 那道闸之前**判一次
            //    （同一函数 `:131-135` 才是第二道闸）⇒ **短路次序 = acting → target → board**。
            //  被筛对象 = **`actingCard`**（形参表 `dump.cs:20360`）。
            //
            //  ⚠️ **只在 `abilityActingGate = true` 的路径上跑** —— 那一位是调用点对
            //    「这条事件的 `actingCard` 是哪一格」**已经查清**的正面信号；
            //    今天只有 `FireBoardWhenListener` 的 `Attack` / `Die` 两族置 `true`。
            //  三档处置（与本函数下面那道 target 闸同口径，⛔ 别合并）：
            //    · **确定不满足** ⇒ **不触发**；· **定位不唯一** ⇒ **不触发**（⛔ 不猜第几条）；
            //    · **拿不到 `actingCard` / 列没落 / 认不出 / 该 `ta` 档没建池器** ⇒
            //      **判不了 ⇒ 放行 + 出声**（⛔ 不把「不知道」当「不满足」）。
            //  覆盖面（现读 v27 池）与残余 → `资料/普查产出_1025/E3_actingCardCriteria.md`。
            // ============================================================
            if (abilityActingGate && !string.IsNullOrEmpty(abilityTrigger)
                && (actor == null || actor.Card == null))
            {
                // 🔴🆕 **2026-10-25（执行代理 `E9`）：⛔ 不再静默跳过。**
                //  本闸的被筛对象是原版的 `actingCard`（一个 `CardScript`）。`actor == null`
                //  的路径（战术 / 防御卡）里原版 `actingCard` = **那张牌自己**（判据见
                //  `EnqueueAbility` 的 `abilityHostCard` doc）—— 我们**没有**「打出去的那张
                //  战术牌」的 `UnitState` ⇒ 这一闸**判不了**。
                //  按本函数的契约（见上面那段注释的三档）⇒ **放行 + 出声**，
                //  ⛔ 既不许静默跳过（改前就是），也不许拿 `abilityTarget` 顶上去猜。
                AbilityActingCriteriaWarningsForTest++;
                UnityEngine.Debug.LogWarning(
                    $"[ActionQueue] 「{label}」的 ability 级 `actingCardCriteria` **判不了** —— "
                  + "这条入队路径没有 `actor`（战术 / 防御卡那一族）。原版这一闸的被筛对象是 "
                  + "`actingCard` = **那张牌自己**（`RawCardScript__OnCardPlayedWithTarget.c:40` → "
                  + "`TriggerAbilities(…, thisCard, cardPlayed, targetCard)`；"
                  + "`RawCardScript__TriggerAbilities.c:207` → `CanTriggerAbility(…, thisCard, actingCard, targetCard, …)`），"
                  + "而我们没有「已打出的那张战术牌」的 `UnitState` ⇒ 本次**不筛**（放行；"
                  + "⛔ 不把「不知道」当「不满足」，也⛔ 不拿 `abilityTarget` 顶替）。");
            }
            else if (abilityActingGate && !string.IsNullOrEmpty(abilityTrigger))
            {
                int aw, aslot;
                FindBoardCell(ctx, actor, out aw, out aslot);
                if (aw < 0)
                {
                    AbilityActingCriteriaWarningsForTest++;
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] 「{label}」的 ability 级 `actingCardCriteria` **判不了** —— 载体"
                      + $"「{actor.Name}」此刻不在棋盘上（求不出 `owner`）⇒ 本次**不筛**（放行）。"
                      + "判据 = 原版 `CardAbility__CanTriggerAbility.c:127-130`（`Matches` 要用 `owner` 判友/敌）。");
                }
                else
                {
                    string awhy;
                    bool aamb;
                    bool? verdict = ActingLevelCriteriaAllows(ctx, aw, actor, actor.Card,
                                                              abilityTrigger, abilityActingCard, abilityTarget,
                                                              out awhy, out aamb);
                    if (verdict == false)
                    {
                        AbilityActingCriteriaWarningsForTest++;
                        UnityEngine.Debug.LogWarning(
                            $"[ActionQueue] **不触发**「{label}」—— ability 级 `actingCardCriteria` **不满足**"
                          + $"（卡「{actor.Card.Name}」· trigger={abilityTrigger}）。"
                          + (aamb
                              ? " 这一档是「**ability 定位不唯一**」：" + awhy
                              : " 候选 = `actingCard`「"
                                + (abilityActingCard != null ? abilityActingCard.Name : "?") + "」。")
                          + "判据 = 原版 `CardAbility__CanTriggerAbility.c:127-130`"
                          + "（`CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`；"
                          + "⛔ 不是静默丢，这条就是出声口）。");
                        return false;
                    }
                    if (verdict == null)
                    {
                        AbilityActingCriteriaWarningsForTest++;
                        UnityEngine.Debug.LogWarning(
                            $"[ActionQueue] 「{label}」的 ability 级 `actingCardCriteria` **判不了**：{awhy}"
                          + " ⇒ 本次**不筛**（放行；⛔ 不把「不知道」当「不满足」）。"
                          + "判据同上；数据侧 = `CardDef.ActingLevelCriteriaForTrigger`。");
                    }
                }
            }
            // ============================================================
            //
            //  判据（第一权威 = 全量反编译）：`CardAbility__CanTriggerAbility.c:131-135`
            //    `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`
            //  ⇒ 原版在**触发前**判一次；被筛对象 = **`targetCard`**
            //    （形参表 `dump.cs:20360`；⛔ 不是 `actingCard` —— 那是第一道闸 `+0x28`）。
            //
            //  ⚠️ **只对「能唯一定位到某条 ability」的路径生效**：调用点必须同时给
            //    `abilityTrigger`（原版 `AbilityTrigger` 枚举名）与 `abilityTarget`（`targetCard`）。
            //    三档处置（⛔ 别合并）：
            //      · **确定不满足** ⇒ **不触发**；· **定位不唯一** ⇒ **不触发**（简报口径：⛔ 不猜第几条）；
            //      · **列没落 / 认不出 / 本路径没有 `targetCard` / 该 `ta` 档没建池器** ⇒
            //        **判不了 ⇒ 放行 + 出声**（⛔ 不把「不知道」当「不满足」）。
            //  ⚠️ 今天只有 `FireBoardWhenListener` 那一跳传（其余十几个调用点**一个都没接**）——
            //    覆盖面与残余 → `资料/普查产出_1025/E2_ability_criteria管线.md`。
            // ============================================================
            if (!string.IsNullOrEmpty(abilityTrigger))
            {
                // 🔴🆕 **2026-10-25（执行代理 `E9` · `J5` §三·2）：宿主卡 / 座位的两条来路。**
                //   ① 有 `actor`（棋盘上的单位）：座位 = 现查格号所在玩家，宿主卡 = `actor.Card`；
                //   ② **没有 `actor`（战术 / 防御卡）**：座位 = `abilityOwnerSeat`，宿主卡 =
                //      `abilityHostCard`。原版这一跳的 `thisCard` **就是那张战术牌自己**
                //      （`RawCardScript__OnCardPlayedWithTarget.c:40` →
                //       `TriggerAbilities(0, mgr, thisCard=param_3, cardPlayed=param_4, targetCard=param_5)`；
                //       `RawCardScript__TriggerAbilities.c:207` → `CanTriggerAbility(…, thisCard, actingCard, targetCard, …)`；
                //       形参表 `dump.cs:20360/23463/23475`）⇒ 它的 `targetCriteria`（`+0x30`）
                //      被筛对象 = **玩家点选的目标**（`abilityTarget`，一个真 `UnitState`）
                //      ⇒ 这一闸**在战术卡上跑得起来**，缺的只是座位与宿主卡。
                //      ⚠️ `thisCard`（`UnitState`）传 `null`：我们没有「已打出的那张牌」的单位态；
                //      `Matches` 对 `thisCard == null` 的那几格走它自己的「判不了/跳过」契约（见类头）。
                CardDef hostCard = abilityHostCard != null
                                 ? abilityHostCard
                                 : (actor != null ? actor.Card : null);
                UnitState gateThisCard = actor;
                int gateOwner = -1;
                string seatWhy = null;
                if (actor != null && actor.Card != null)
                {
                    int aw, aslot;
                    FindBoardCell(ctx, actor, out aw, out aslot);
                    if (aw < 0)
                        seatWhy = $"载体「{actor.Name}」此刻不在棋盘上（求不出 `owner`）";
                    else
                        gateOwner = aw;
                }
                else if (actor == null && abilityOwnerSeat >= 0)
                {
                    gateOwner = abilityOwnerSeat;
                    gateThisCard = null;   // 战术牌自己没有 `UnitState`（原版 `thisCard` = 那张牌）
                }
                else
                {
                    seatWhy = "本路径既没有 `actor`、也没给 `abilityOwnerSeat`（求不出 `owner`）";
                }

                if (seatWhy != null)
                {
                    AbilityLevelCriteriaWarningsForTest++;
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] 「{label}」的 ability 级 `targetCriteria` **判不了** —— {seatWhy}"
                      + " ⇒ 本次**不筛**（放行）。"
                      + "判据 = 原版 `CardAbility__CanTriggerAbility.c:131-135`（`Matches` 要用 `owner` 判友/敌）。");
                }
                else if (hostCard == null)
                {
                    AbilityLevelCriteriaWarningsForTest++;
                    UnityEngine.Debug.LogWarning(
                        $"[ActionQueue] 「{label}」的 ability 级 `targetCriteria` **判不了** —— "
                      + "拿不到【宿主卡】（`abilityHostCard` / `actor.Card` 都是 null）⇒ "
                      + "查不到这条 ability 的 criteria 行 ⇒ 本次**不筛**（放行；⛔ 不静默跳过）。");
                }
                else
                {
                    string cwhy;
                    bool camb;
                    bool? verdict = AbilityLevelCriteriaAllows(ctx, gateOwner, gateThisCard, hostCard,
                                                               abilityTrigger, abilityTarget, out cwhy, out camb);
                    if (verdict == false)
                    {
                        AbilityLevelCriteriaWarningsForTest++;
                        UnityEngine.Debug.LogWarning(
                            $"[ActionQueue] **不触发**「{label}」—— ability 级触发条件 **不满足**"
                          + $"（卡「{hostCard.Name}」· trigger={abilityTrigger}）。"
                          + (camb
                              ? " 这一档是「**ability 定位不唯一**」：" + cwhy
                              : (string.IsNullOrEmpty(cwhy)
                                  ? " 候选 = `targetCard`「" + (abilityTarget != null ? abilityTarget.Name : "?") + "」。"
                                  // 🆕 2026-10-26（`G17`）：`cwhy` 现在也可能是**第三道闸（board）**的缺口。
                                  : " " + cwhy))
                          + "判据 = 原版 `CardAbility__CanTriggerAbility.c:127-140`"
                          + "（`CheckIfMeetsCriteria(thisCard, actingCard/targetCard, targetCard, *(ability+0x28 / +0x30), 0)`"
                          + " ＋ `BoardAnalysis.MeetsCriteria(…, *(ability+0x38), …, ignoreRandomTrigger, 0)`；"
                          + "⛔ 不是静默丢，这条就是出声口）。");
                        return false;
                    }
                    if (verdict == null)
                    {
                        AbilityLevelCriteriaWarningsForTest++;
                        UnityEngine.Debug.LogWarning(
                            $"[ActionQueue] 「{label}」的 ability 级触发条件 **判不了**：{cwhy}"
                          + " ⇒ 本次**不筛**（放行；⛔ 不把「不知道」当「不满足」）。"
                          + "判据同上；数据侧 = `CardDef.AbilityLevelCriteriaForTrigger`"
                          + " ＋ `ActionPump.BoardLevelCriteriaAllows`（`role=\"count\"` 那一行）。");
                    }
                }
            }
            var act = new QueuedAction
            {
                Label = label, Execute = body,
                Actor = actor, Target = abilityTarget, RequireActorInPlay = requireActorInPlay,
                AbilityContext = abilityContext, ActionType = actionType,   // 账 L490：原版 `BattleActionType` 号回填（缺省 0 = 未标；消费者见 `ActionQueue.RemoveQueuedActionAt` 的副作用 switch 与 `CancelEffectsFromDisappearingUnitFromQueue` 的三档豁免）
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
        ///      🔴🆕 2026-10-24（执行代理 `E8`）：**「这张卡没有这条 trigger」这一档从本日起真的会出现**
        ///      —— <see cref="AbilityContextOfAbilityV14"/> 补上了判据前提②
        ///      （`AbilityTriggerPresence == 1`）；改前它被「versioned ⇒ 一律 `Play(0)`」吞掉了。
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
            //   ⚠️ **`-1` 的来路**（都回退旧关键词表）：① 旧卡表 / 第二解析路没接列；
            //   ② 载体卡拿不到（`DeathWatch` 那一支传 `null`）；
            //   ③ **认不出 trigger 名**（关键词反查不到枚举名）；
            //   ④ 🔴🆕 **2026-10-24（`E8`）这张卡本来就没有这条 trigger 的 ability**
            //      （`AbilityTriggerPresence == 0`）/ v17 列也缺（`-1`）——
            //      判据 = `A2` Q3 前提②：没有那一行就没有「0 被省略」这回事，
            //      ⛔ 不许拿 `Play(0)` 顶（详见 `AbilityContextOfAbilityV14` 的 doc ②）。
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
        ///      → <see cref="TriggerNameOfKeyword"/>）
        ///      **且** <see cref="CardDef.AbilityTriggerPresence(string)"/> == `1`（= 这张卡**确实有**
        ///      这条 trigger 的 ability —— v17 真值列）⇒ **`Play(0)`**；
        ///   ③ 其余 ⇒ `-1`（<see cref="CardDef.AbilityContextUnknown"/>，⛔ 不许读成 `Play(0)`）。
        ///
        /// ⚠️ **为什么 ② 要「认得出 trigger 名」**：生成器只在
        /// `ABILITY_TRIGGER_KEYWORD` 有映射的行上落 `keyword`（21 条）。像 `swarm` / `survivor`
        /// 这种既没有 trigger 名映射、又不在映射表里的关键词 —— 查不到**不能**推成 0
        /// （那会把一条本来 `-1` 的路径静默变成「必须活着在场」）。
        /// `AbilityContextsVersioned` 为假（旧卡表 / 第二解析路没接列）⇒ 照旧 `-1`。
        ///
        /// 🔴🆕 **2026-10-24（执行代理 `E8` · 判据 `资料/普查产出_1024c/A2_引擎战斗判据总表.md` Q3
        /// 的**前提②**）：上面 ② 还多一道 **`AbilityTriggerPresence(tn) == 1`** —— 为什么必须有它：
        ///   · 「缺行 = `Play(0)`」这条推理的**立足点**是「**这条 ability 本来会有一行**，
        ///     只是生成器把 `0` 省略了」（`gen_cards_engine.py` 的 `ABILITY_CONTEXT_DEFAULT = 0`）；
        ///   · 若这张卡**根本没有**这条 trigger 的 ability（v17 `abilityTriggers` 明确没有 = `0`），
        ///     那么「查不到行」的成因就是**没有这条 ability**，⛔ 不是「省略了 0」
        ///     —— 这时推成 `Play(0)` 就是**给一条不存在的 ability 编口径**
        ///     （同一条纪律写在 `EffectResolver.AbilityContextOfAbilityV14` 的「调用前提」那段）；
        ///   · 判据链：`CardAbility__CanTriggerAbility.c:126` 那一格是**逐 ability 行**判的
        ///     `IsRightContext`（`thisCard` = 那一行的载体）⇒ 行不存在 ⇒ 这一格从不执行
        ///     ⇒ 原版对「没有这条 ability」**没有**任何 context 结论。
        ///   · `-1`（v17 列也没有：旧卡表 / 第二解析路没接列）同样**不推** —— 两条前提
        ///     「同时满足」才成立（Q3 §4）。
        ///   ⛔ **别把这一道写成 `!= 0`**：`-1`（未知）会被放过去 ⇒ 又回到「无凭据也当 Play」。
        ///   ⚠️ 实测口径（2026-10-24 现读 `cards_engine.json` v24 / 1127 张）：
        ///   `Slay(120)` 只有 **16** 张卡有、`Strike(580)` **30** 张、`Mob(300)` **16** 张
        ///   —— 收口前这 1127 张**全部**被读成 `Play(0)`（`TestW61T27ActorLeftPlayCancels` ⑩
        ///   那条「全池每一张卡的 `Slay` 都读成 `Play(0)`」就是旧口径的画像）。
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
            if (string.IsNullOrEmpty(tn) || !card.AbilityContextsVersioned)
                return CardDef.AbilityContextUnknown;
            // 🔴🆕 2026-10-24（执行代理 `E8` · 判据 `A2` Q3 前提②）：**这张卡必须真的有这条
            //   trigger 的 ability**（v17 `abilityTriggers` 真值列）—— `1` = 有、`0` = 明确没有、
            //   `-1` = 未知（旧表）。⛔ 只有 `1` 才允许把「缺行」读成 `Play(0)`；
            //   `0`/`-1` 都回 `-1`（未知），由调用点回退旧口径。理由 / 反例详见本方法 doc ②。
            int presence = card.AbilityTriggerPresence(tn);
            if (presence != 1) return CardDef.AbilityContextUnknown;
            return CardDef.AbilityContextPlay;      // v14 只落非 0 ⇒ 缺行 = Play(0)
        }

        /// <summary>🆕 v26（2026-10-25 · 执行代理 `E2`）：ability 级 `targetCriteria` 这道闸
        /// **发过几次声**（三档：不满足 / 判不了 / 载体不在场）。
        ///
        /// <para>**只给自检用**：那些 `Debug.LogWarning` 的输出流在 `RuleEngineTest` / 离线探针里
        /// **抓不到** ⇒ 用这个计数当「出声了没有」的判别式（计数没涨 = 那声没发）。
        /// ⛔ 引擎里没有任何生产消费者。</para></summary>
        public static int AbilityLevelCriteriaWarningsForTest { get; private set; }

        /// <summary>🆕 v27（2026-10-25 · 执行代理 `E3`）：ability 级 **`actingCardCriteria`（第一道闸）**
        /// **发过几次声**（三档：不满足 / 判不了 / 载体不在场）。
        ///
        /// <para>**只给自检用**（理由同 <see cref="AbilityLevelCriteriaWarningsForTest"/> —— `Debug.LogWarning`
        /// 的输出流在自检里抓不到）。⛔ 引擎里没有任何生产消费者。</para></summary>
        public static int AbilityActingCriteriaWarningsForTest { get; private set; }

        /// <summary>
        /// 🆕 **2026-10-25（执行代理 `E3` · `E2` 报告 §六）：ability 级 `actingCardCriteria` 的
        /// 触发前第一道闸**（原版 `CardAbility.actingCardCriteria` `+0x28`，
        /// `CardAbility__CanTriggerAbility.c:127-130`）。
        ///
        /// <para>🔴 **被筛的是 `actingCard`（不是 `targetCard`）** —— 判据逐句：
        /// `CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`；
        /// 形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` ⇒ `param_5 = actingCard`（**本闸的候选**）、
        /// `param_6 = targetCard`（第二道闸 `+0x30` 的候选，见 `AbilityLevelCriteriaAllows`）。</para>
        ///
        /// <para>**返回值三态**（调用点必须分清，⛔ 别把 `null` 当 `false`）：</para>
        /// <list type="bullet">
        /// <item>`true` = 满足 / 明确没有这一格条件（原版 `targetsAffected = any` 直接 `return 1`）；</item>
        /// <item>`false` = **明确不满足**（含 **ability 定位不唯一**）⇒ 调用点**不触发**并发声；</item>
        /// <item>`null` = **判不了**（列没落 / 认不出 / 本路径拿不到 `actingCard` /
        ///       36 格里有的档本版没建池器）⇒ `why` 说明是哪一档 ⇒ 调用点**出声并放行**。</item>
        /// </list>
        ///
        /// <para>⚠️ **候选为空 ⇒ 判不了、不是不满足**：原版这一跳会 NRE（`CheckIfMeetsCriteria.c:216`
        /// 拿 `param_2` 解引用），我们这边不复制崩溃语义，但也不假装它满足。</para>
        /// </summary>
        /// <param name="owner">`thisCard`（= 载体）所在玩家下标（`Matches` 判友/敌要用）。</param>
        /// <param name="thisCard">原版的 `thisCard` = 这条 ability 的载体（我们 = `actor`）。</param>
        /// <param name="candidate">原版的 `actingCard`。</param>
        /// <param name="targetCard">原版的 `targetCard`（本闸不直接筛它，但 `ta = 30/90` 那几档要它比身份）。</param>
        public static bool? ActingLevelCriteriaAllows(BattleContext ctx, int owner, UnitState thisCard,
                                                      CardDef card, string triggerName, UnitState candidate,
                                                      UnitState targetCard, out string why)
        {
            bool amb;
            return ActingLevelCriteriaAllows(ctx, owner, thisCard, card, triggerName, candidate,
                                             targetCard, out why, out amb);
        }

        /// <summary>🆕 v27：同 <see cref="ActingLevelCriteriaAllows(BattleContext,int,UnitState,CardDef,string,UnitState,UnitState,out string)"/>，
        /// 另把「ability 定位**不唯一**」标出来（那一档 ⇒ **不触发** + 出声；与「判不了」方向相反）。</summary>
        public static bool? ActingLevelCriteriaAllows(BattleContext ctx, int owner, UnitState thisCard,
                                                      CardDef card, string triggerName, UnitState candidate,
                                                      UnitState targetCard, out string why, out bool ambiguous)
        {
            ambiguous = false;
            why = null;
            if (card == null) { why = "没有卡（判不了）"; return null; }
            var set = card.ActingLevelCriteriaForTrigger(triggerName, out why, out ambiguous);
            if (set == null)
            {
                // `why == null` ⇒ 明确「这条 ability 没有这一格条件」（`ta = any` / 没落行）⇒ 放行；
                // 歧义 ⇒ **不触发**（简报口径：⛔ 不猜是第几条）；
                // 其余（列没落 / 认不出 / 没有这条 ability）⇒ 判不了（调用点出声 + 放行）。
                if (why == null) return true;
                return ambiguous ? (bool?)false : null;
            }
            if (candidate == null)
            {
                why = "本路径拿不到 `actingCard`（原版这一跳是 `actingCard` 解引用 ⇒ 会 NRE）⇒ 判不了";
                return null;
            }
            string gap;
            // 🔴🆕 **2026-10-25（执行代理 `E9` · `J5` §二 F5）：第 ⑧ 格（`conditionsFilter` /
            //   `excludeCondition`）—— 改前这里**恒传 `null`**（= 明确「没有条件」）⇒ 原版
            //   `CheckIfMeetsCriteria.c:298-303` 的那一格**从来不跑**。
            //   数据早就在：生成器把这两格落到 `unitConditions` 的 `role="acting"` 行（`lg = -1`）；
            //   拼回一个 `TargetCriteria` 的既有先例 = `RuleCore` 的 `ActiveAbilityPickGate`（`E5-1`）。
            var cond = AbilityLevelConditionRow(card, triggerName, "acting");
            bool? ok = TargetCriteriaEval.Matches(ctx, owner, set, candidate, cond, thisCard, targetCard, out gap);
            if (ok == null && !string.IsNullOrEmpty(gap)) why = gap;
            return ok;
        }

        /// <summary>
        /// 🔴🆕 **2026-10-25（执行代理 `E9` · `J5` §二 F5）：ability 级 criteria 的第 ⑧ 格
        /// （`conditionsFilter` / `excludeCondition`）的唯一拼接口。**
        ///
        /// <para>原版那两个字段（`TargetCriteria +0x38` / `+0x3d`）与我们这 36 格**同属一个对象**
        /// （`FilterMethods__CheckIfMeetsCriteria.c:298-303` 是那条 `&&` 链的第 ⑧ 步），只是生成器
        /// 按 `CONDITION_ROLES` 把它拆到 `unitConditions` 列 ⇒ 消费端按 `(ab, lg = -1, role)` 拼回去。</para>
        ///
        /// <para>返回值三态（照 `RuleCore` 那个先例的口径，⛔ 别改）：</para>
        /// <list type="bullet">
        /// <item>有这一行 ⇒ 原样返回（`Condition` + `Exclude` 都带着）；</item>
        /// <item>没有这一行 + <see cref="CardDef.ConditionsVersioned"/> ⇒ `null`
        ///   = **明确没有这一格条件**（`none(0)` ⇒ 放行，⛔ 不是「判不了」）；</item>
        /// <item>没有这一行 + 旧表（&lt; v15）⇒ 送一个 `Condition = ConditionUnknown` 的哨兵
        ///   ⇒ `ConditionEval` 回 `null` ⇒ `Matches` 在第 ⑧ 步给「判不了」⇒ 调用点放行 + 出声
        ///   （⛔ 别拿「未知」冒充「没有条件」）。</item>
        /// </list>
        /// </summary>
        /// <param name="role">`"acting"`（第一道闸 `+0x28` 的被筛对象）/ `"target"`（第二道闸 `+0x30`）。</param>
        static CardDef.ConditionSpec AbilityLevelConditionRow(CardDef card, string triggerName, string role)
        {
            if (card == null) return null;
            // 定位口径只此一处：`UniqueAbilityIndexForTrigger`（⛔ 别在调用点另写一份）。
            // 走到本方法时调用方已确认 `set != null` ⇒ 它必然定位唯一、`ab >= 0`。
            int ab = card.UniqueAbilityIndexForTrigger(triggerName, out _, out _);
            if (ab < 0) return null;                       // 定位不到 ⇒ 不传 cond（那一档由 set 自己出声）
            CardDef.ConditionSpec spec;
            if (card.TryGetCondition(ab, -1, role, out spec) && spec != null) return spec;
            if (card.ConditionsVersioned) return null;      // 明确「这一格没有条件」
            return new CardDef.ConditionSpec { Condition = CardDef.ConditionUnknown, Role = role };
        }

        /// <summary>
        /// 🆕 **2026-10-25（执行代理 `E2` · `A3-2`）：ability 级 `targetCriteria` 的触发前闸**
        /// （原版 `CardAbility.targetCriteria` `+0x30`，`CardAbility__CanTriggerAbility.c:131-135`）。
        ///
        /// <para>🔴 **被筛的是 `targetCard`（不是 `actingCard`）** —— 判据逐句：
        /// `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`；
        /// 形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` ⇒ `param_5 = actingCard`（第一道闸 `+0x28` 的候选）、
        /// `param_6 = targetCard`（**本闸的候选**）。</para>
        ///
        /// <para>**返回值三态**（调用点必须分清，⛔ 别把 `null` 当 `false`）：</para>
        /// <list type="bullet">
        /// <item>`true` = 满足 / 明确没有这一格条件（原版 `targetsAffected = any` 直接 `return 1`）；</item>
        /// <item>`false` = **明确不满足** ⇒ 调用点**不触发**并发声；</item>
        /// <item>`null` = **判不了**（列没落 / ability 定位不唯一 / 本路径没有 `targetCard` /
        ///       36 格里有的档本版没建池器）⇒ `why` 说明是哪一档 ⇒ 调用点**出声并放行**
        ///       （⛔ 不许当「不满足」把触发掐死）。</item>
        /// </list>
        ///
        /// <para>⚠️ **候选为空 ⇒ 判不了、不是不满足**：原版这一跳会 NRE（`CheckIfMeetsCriteria.c:216`
        /// 拿 `param_2` 解引用），我们这边不复制崩溃语义，但也不假装它满足。</para>
        ///
        /// <para>🔴🆕 **2026-10-26（执行代理 `G17` · 批次 6）：本方法现在判**两道**闸** ——
        /// 第二道 `targetCriteria`（`+0x30`）＋ **第三道 `boardCriteria`（`+0x38`）**
        /// （原版 `CardAbility__CanTriggerAbility.c:127-140` 是一条 `&&` 链，短路次序
        /// `acting → target → board`）。第三道闸的语义 / 三态 / 数据来源 →
        /// <see cref="BoardLevelCriteriaAllows"/> 的 doc。返回值仍是三态：
        /// `false` 现在也包含「**board 明确不满足**」（`why` 会写清是哪一道闸、哪一格）。</para>
        /// </summary>
        public static bool? AbilityLevelCriteriaAllows(BattleContext ctx, int owner, UnitState thisCard,
                                                      CardDef card, string triggerName, UnitState candidate,
                                                      out string why)
        {
            bool amb;
            return AbilityLevelCriteriaAllows(ctx, owner, thisCard, card, triggerName, candidate,
                                             out why, out amb);
        }

        /// <summary>🆕 v26：同 <see cref="AbilityLevelCriteriaAllows(BattleContext,int,UnitState,CardDef,string,UnitState,out string)"/>，
        /// 另把「ability 定位**不唯一**」标出来 —— 那一档按简报的口径是**不触发**（⛔ 不猜第几条），
        /// 与「列没落 / 认不出 / 没有 targetCard」那几档（判不了 ⇒ 放行 + 出声）**方向相反**。</summary>
        public static bool? AbilityLevelCriteriaAllows(BattleContext ctx, int owner, UnitState thisCard,
                                                      CardDef card, string triggerName, UnitState candidate,
                                                      out string why, out bool ambiguous)
        {
            ambiguous = false;
            why = null;
            if (card == null) { why = "没有卡（判不了）"; return null; }
            var set = card.AbilityLevelCriteriaForTrigger(triggerName, out why, out ambiguous);
            if (set == null)
            {
                // `why == null` ⇒ 明确「这条 ability 没有触发条件」（`ta = any` / 没落行）⇒ 放行；
                // 歧义 ⇒ **不触发**（简报口径：⛔ 不猜是第几条）；
                // 其余（列没落 / 认不出 / 没有这条 ability）⇒ 判不了（调用点出声 + 放行）。
                //
                // 🔴🆕 **2026-10-26（执行代理 `G17` · 批次 6 · 第三道闸）：这里**不能**直接 `return true`**。
                //   原版 `CardAbility__CanTriggerAbility.c:127-140` 是**一条 `&&` 链**：
                //   第二道闸「没有条件」= `CheckIfMeetsCriteria` 直接回真 ⇒ **第三道闸 `board(+0x38)` 照样要判**。
                //   改前这一支直接放行 ⇒「有 board 条件、没有 target 条件」的那些 ability 的第三道闸
                //   **从来没被求值过**（现读 99 条 `role="count"` 行里 25 条 `targetsAffected = 0`）。
                if (why == null) return BoardLevelCriteriaAllows(ctx, owner, thisCard, card, triggerName, out why);
                return ambiguous ? (bool?)false : null;
            }
            if (candidate == null)
            {
                why = "本路径没有 `targetCard`（原版这一跳是 `targetCard` 解引用 ⇒ 会 NRE）⇒ 判不了";
                return null;
            }
            string gap;
            // 🔴🆕 **2026-10-25（执行代理 `E9` · `J5` §二 F5）：第 ⑧ 格 —— 改前恒传 `null`。**
            //   理由 / 三态契约见 <see cref="AbilityLevelConditionRow"/> 的 doc。
            //   ⚠️ `thisCard` 可能是 `null`（战术卡那条路）：原版 `thisCard` = 那张牌自己，
            //   我们只有它的 `CardDef` ⇒ `Matches` 里凡是要 `UnitState` 的格走它自己的
            //   「判不了 / 跳过」契约（类头那段），⛔ 不在这里另判一次。
            var cond = AbilityLevelConditionRow(card, triggerName, "target");
            bool? ok = TargetCriteriaEval.Matches(ctx, owner, set, candidate, cond, thisCard, candidate, out gap);
            if (ok == null && !string.IsNullOrEmpty(gap)) why = gap;
            // 🔴🆕 **2026-10-26（`G17`）：原版是一条 `&&` 链 ⇒ target **明确不满足**时 board 不求值。**
            if (ok == false) return false;

            // ---- 第三道闸：`boardCriteria`（`+0x38`）—— 原版 `BoardAnalysis.MeetsCriteria` ----
            // 判据 / 三态 / 数据来源 → <see cref="BoardLevelCriteriaAllows"/> 的 doc。
            string bwhy;
            bool? board = BoardLevelCriteriaAllows(ctx, owner, thisCard, card, triggerName, out bwhy);
            if (board == false) { why = bwhy; return false; }
            if (board == null)
            {
                // 「判不了 ⇒ 出声 + 放行」与 target 闸同口径；`ok` 也可能是 `null`（target 判不了）
                // ⇒ **两个缺口都留住**（调用点只打印 `why`）。
                why = string.IsNullOrEmpty(why) ? bwhy
                    : (string.IsNullOrEmpty(bwhy) ? why : why + "；且 board：" + bwhy);
                return null;
            }
            return ok;
        }

        // ==========================================================================================
        //  🆕 v29（2026-10-25 · 执行代理 `X6` · **口径乙**）：按 `chosenAbility` 家族认行的
        //  ability 级闸 —— 专治「同一 trigger 有两条 ability」那 23 张卡（判据 →
        //  `资料/待办判据_引擎_1025b.md` §一；数据 → 卡表 v29 的 `abilityLevelCriteria[].ca`）。
        // ==========================================================================================
        //  判据（原版）：`RawCardScript__TriggerAbilities.c:207/231` —— 原版**不选**，逐条 ability
        //    各判各结算（`CanTriggerAbility` 每条各一次）。我们这条正文是一整块 thunk ⇒ 只能近似：
        //    · 甲（多 ability 取 OR）：对 19/23 张**恒真空转**（criteria 行全在 `ab>=1`、`ab0` 无行）
        //      ⇒ 主对话**否掉**；
        //    · 乙（本函数）：按 op 的动词 → 能力种类族名 → 认那一行 —— 更接近，且真会筛。
        //  ⚠️ 仍是近似：原版是「一条 ability 判一次、整条结算」，我们是「逐 op 判」——
        //    差别只在「一条 ability 的 logic 混了两种家族」那种卡（实测 4 张 ⇒ 判不了 ⇒ 放行）。
        //    这是**已知并接受**的近似，⛔ 别当成「等价于原版」。

        /// <summary>🆕 v29：`AbilityFamilyTargetAllows` 判「判不了」时**出声**的次数（只增不减）。
        /// <para>**只给自检用**：`Debug.LogWarning` 的输出流离线探针与 `RuleEngineTest` 都抓不到
        /// ⇒ 用计数当判别式（同 <see cref="AbilityLevelCriteriaWarningsForTest"/> 的纪律）。
        /// ⚠️ 同一个 `(卡, 族)` **只喊一次**（`_abilityFamilyGateNotices` 去重）⇒ 「出声一次」是可断言的。</para></summary>
        public static int AbilityFamilyGateWarningsForTest { get; private set; }

        /// <summary>🆕 v29：族闸真正**剔掉一批 op** 的次数（**每次触发 / 每次结算最多 +1**，
        /// 不是「剔了几条 op」）。
        /// <para>**只给自检用** —— 它是「criteria 真的筛了」在**生产路径**上的唯一仪器
        /// （`ctx.Log` 的输出流离线探针与 `RuleEngineTest` 都抓不到）。</para></summary>
        public static int AbilityFamilyGateSkipForTest { get; private set; }

        /// <summary>🆕 v29：族闸**剔掉一批 op** 时调一次（两个调用点：`EffectResolver.ApplyAbilityFamilyGate`
        /// 的出牌路 · `RuleCore.AbilityFamilyGateOpsForTrigger` 的触发路）。</summary>
        internal static void NoteAbilityFamilyGateSkipForTest() { AbilityFamilyGateSkipForTest++; }

        /// <summary>🆕 v29：把 <see cref="AbilityFamilyGateWarningsForTest"/> /
        /// <see cref="AbilityFamilyGateSkipForTest"/> 与去重表清零 —— **只给自检用**
        /// （否则前一条用例喊过一声、后一条就数不到）。⛔ 生产侧别调。</summary>
        public static void ResetAbilityFamilyGateForTest()
        {
            AbilityFamilyGateWarningsForTest = 0;
            AbilityFamilyGateSkipForTest = 0;
            _abilityFamilyGateNotices.Clear();
        }

        static readonly System.Collections.Generic.HashSet<string> _abilityFamilyGateNotices =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>🆕 v29：同 `(ab, role)` 的 8 格条件行（`unitConditions` 的 `lg = -1` 那一批）。
        /// 与 <see cref="AbilityLevelConditionRow(CardDef,string,string)"/> 同一套三态，只是**按 `ab` 定位**
        /// （家族认行已经拿到 `ab` 了，⛔ 别再用 trigger 名去猜一遍）。</summary>
        static CardDef.ConditionSpec AbilityLevelConditionRowAt(CardDef card, int ability, string role)
        {
            if (card == null || ability < 0) return null;
            CardDef.ConditionSpec spec;
            if (card.TryGetCondition(ability, -1, role, out spec) && spec != null) return spec;
            if (card.ConditionsVersioned) return null;      // 明确「这一格没有条件」
            return new CardDef.ConditionSpec { Condition = CardDef.ConditionUnknown, Role = role };
        }

        /// <summary>🆕 v29（**口径乙**）：按能力种类族名判 ability 级 `targetCriteria`（`+0x30`）。
        ///
        /// <para>三态（与 <see cref="AbilityLevelCriteriaAllows(BattleContext,int,UnitState,CardDef,string,UnitState,out string,bool)"/>
        /// **同一套**，⛔ 调用点别把 `null` 当 `false`）：</para>
        /// <list type="bullet">
        /// <item>`true`  = 满足 / **明确没有这一族的条件**（原版 `targetsAffected = any` 直接 `return 1`）；</item>
        /// <item>`false` = **明确不满足** ⇒ 那一族的那条 op **不结算**（调用点出声）；</item>
        /// <item>`null`  = **判不了**（旧表 / 这一族 ≥2 行 / 有行家族为空 / 没给族名 / 没有 `targetCard` /
        ///       本版没建池器的那几档）⇒ 调用点**放行**；**本函数自己出声一次**（同卡同族去重）
        ///       并 `AbilityFamilyGateWarningsForTest++`。</item>
        /// </list>
        /// <para>⚠️ 「判不了 ⇒ 放行」的方向与 <see cref="AbilityLevelCriteriaAllows"/> 的「不唯一 ⇒ 不触发」
        /// **不同**，是**故意**的：那一档（按 trigger 定位）在 v26 就定了「不猜就不触发」；
        /// 这一档没有「整条 ability」可丢，丢掉整个战术正文才是更坏的结果（`E9` §四 实测过）。</para></summary>
        public static bool? AbilityFamilyTargetAllows(BattleContext ctx, int owner, UnitState thisCard,
                                                     CardDef card, string triggerName, string family,
                                                     UnitState candidate, out string why, out bool ambiguous)
        {
            ambiguous = false;
            why = null;
            if (card == null) { why = "没有卡（判不了）"; return null; }
            int ab;
            var set = card.AbilityLevelCriteriaForFamily(triggerName, family, out why, out ambiguous, out ab);
            if (set == null)
            {
                if (why == null) return true;                 // 明确「这一族没有触发条件」
                EmitAbilityFamilyGateNotice(card, triggerName, family, why);
                return null;
            }
            if (candidate == null)
            {
                why = "这一族有触发条件，但本路径没有 `targetCard`（原版这一跳是 `targetCard` 解引用）⇒ 判不了";
                EmitAbilityFamilyGateNotice(card, triggerName, family, why);
                return null;
            }
            string gap;
            // 8 格条件行（`conditionsFilter`/`excludeCondition`）按认出来的 `ab` 取 —— 与 36 格无关，
            // 两者拼回原版同一个 `TargetCriteria` 对象（口径 → `TargetCriteriaEval` 文件头）。
            var cond = AbilityLevelConditionRowAt(card, ab, "target");
            bool? ok = TargetCriteriaEval.Matches(ctx, owner, set, candidate, cond, thisCard, candidate, out gap);
            if (ok == null)
            {
                // 逐维判不了（`TargetCriteriaEval` 的 `null` 契约）⇒ **放行 + 出声**（同卡同族去重）。
                why = string.IsNullOrEmpty(gap) ? "`TargetCriteriaEval` 判不了（没给原因）" : gap;
                EmitAbilityFamilyGateNotice(card, triggerName, family, why);
                return null;
            }
            return ok;
        }

        /// <summary>🆕 v29：`AbilityFamilyTargetAllows` 的**唯一出声口**（同 `(卡, trigger, 族)` 只喊一次、
        /// 计数只加一次）。⛔ 别在调用点各喊一遍 —— 那是「两处写同一条规则」的同族坑。</summary>
        static void EmitAbilityFamilyGateNotice(CardDef card, string triggerName, string family, string why)
        {
            // 🔴 **去重键分两档**（「出声一次」的可断言口径）：
            //   · 这张卡**有行的 `ca` 为空**（`""` = 家族判不了，实测 5 行 / 4 张卡）⇒ 它是**整卡**
            //     认不出来 ⇒ 键**不含族名**：同一张卡同一条 trigger **只喊一次**（`DA33` 有 damage
            //     + resource 两族，但它整卡就一个洞 ⇒ 不该喊两遍）。
            //   · 其余（例如「同一族 ≥2 行」的 `SOR31`）⇒ 键含族名（那确实是**按族**判不了的）。
            bool cardLevel = card != null
                          && card.AbilityLevelCriteriaRowCount > card.AbilityLevelCriteriaFamilyKnownRowCount;
            string key = (card != null && card.Id != null ? card.Id : "?") + "|" + triggerName
                       + (cardLevel ? "|*" : "|" + family);
            if (!_abilityFamilyGateNotices.Add(key)) return;
            AbilityFamilyGateWarningsForTest++;
            UnityEngine.Debug.LogWarning(
                $"[RuleEngine] 卡「{(card != null ? card.Name : "?")}」（{(card != null ? card.Id : "?")}）的 "
              + $"ability 级 `targetCriteria` **按能力种类族名认行判不了**"
              + $"（trigger={triggerName}{(cardLevel ? "" : ", 族=" + family)}）：{why}"
              + " ⇒ 本次**不筛**（放行；⛔ 不把「不知道」当「不满足」）。"
              + "判据 = `资料/待办判据_引擎_1025b.md` §一（口径乙）；数据 = 卡表 v29 的 "
              + "`abilityLevelCriteria[].ca`。");
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
                // 🆕 **2026-10-25（执行代理 `X3` · 判据文档 `待办判据_引擎_1025b` §二·④）**：
                //   `rally` → `ThisCardPlayed`(0)。
                //   判据 = 数据现读（`cards_engine.json` v28）：引擎解析后带 `rally` 关键词的卡
                //   **105 张**（`keywords` 列里精确写 `Rally` 的 98 张 ＋ 写成 `Rally: <效果>` 的 7 张 ——
                //   `KeywordTable.Parse` 按 `':'` 切前半段，两类都归成 `rally`），
                //   这 105 张的 `abilityTriggers` 里 **`trigger 0` 全部存在**（105/105）
                //   ⇒ 引擎「部署时触发 rally」与原版「打出这张卡 ⇒ `TriggerAbilities(0)`」是同一件事
                //   （`RawCardScript__OnCardPlayedWithTarget.c:40` → `TriggerAbilities(0, …)`）。
                //   ⚠️ **今天接上它是零行为变化**：`FireTriggerAt` 拿到这个名字后只会
                //   `TriggerSkipsAbilityContext("ThisCardPlayed")`（= 原版 skip 表）⇒
                //   `abilityContext` 传 `Unknown`，与改前「认不出 ⇒ Unknown」逐字相同。
                //   它**是**给「`FireTriggerAt` 补 `abilityTrigger/abilityTarget`」那一步铺的路 ——
                //   那一步落在 `RuleCore.cs`（本轮不在白名单），所以本行今天只是把映射补齐。
                case KeywordTable.Rally:         return "ThisCardPlayed";    // 0
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
                default:                         return ExtraTriggerName(keyword);   // ⛔ 别直接加 case：会插行 ⇒ 账漂移，见类尾 §八
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
        /// 🔴🆕 2026-10-24（`W25` · `D1` 的 R3）：**40 档的「待死」我们靠 `ctx.DyingThisAction` 认**
        /// （`CleanupDeaths` 当场把死者从棋盘摘下 ⇒ `cell` 恒 null；原版那一刻卡状态是 `waitingToDie(5)`）。
        /// ⛔ **50 档不加** —— 原版 50 支读的是 `IsInCemetery || IsInPlay`，**不含** state 5。
        ///
        /// 🔶 自己设计（非原版）：`-1`（未知）⇒ 返回 `true`（= 不拦）· 原版没有「未知」这一档（一切非法值都 fail-closed）
        /// （`IsRightContext.c:84-90` = `LogWarning` + `return 0`），而我们的 `-1` 是**数据缺口**（旧卡表 / 该行没落 / 认不出 trigger 名）⇒ 只能回退旧关键词表（`UnitInPlay` / `QueuedAction.RequireActorInPlay` 那一套）；调度台 2026-10-27 裁定「保留 + 打本标记」（账 679）。⛔ 别把「未知」读成 `Play(0)`。
        ///
        /// ⚠️ **`slot` 只对 0/40/50 有意义**（棋盘格号）；10/20/30 是**按 `Instance` 查三个区域**。
        /// 所以多出 `carrier` 一参：载体离开棋盘之后（死亡 / 回手 / 洗回牌库）格子上就找不到它了，
        /// **只有 `UnitState.Instance` 还认得出那一份**（readonly；翻面成残骸也沿用同一个）。
        /// `carrier == null` ⇒ 现从 `Board[owner][slot]` 取；`owner` 认不出（`-1`）⇒ 两个玩家的区域都查
        /// （`Instance` 一局唯一，不会认错）。
        /// ⚠️ **`owner`/`slot` 对不上 `carrier`** ⇒ 那一格**已经换人** ⇒ 按「不在棋盘」处理
        /// （⛔ 别只看「格子非空」就放行）。
        ///
        /// 其它值（数据坏）⇒ **出声**（`LogWarning`）后 **`return false`**（原版 `IsRightContext.c:84-90` 的 fail-closed；账 679）。
        /// </summary>
        public static bool IsRightContext(BattleContext ctx, int owner, int slot, int context,
                                          CardInstance carrier = null)
        {
            if (ctx == null) return true;                              // 没有 ctx ⇒ 不拦（与 UnitInPlay 同口径）
            if (context == CardDef.AbilityContextUnknown) return true; // 🔶 自己设计（非原版）：`-1` 契约 = 回退旧表（见本方法 doc）

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
                    // 🔴🆕 2026-10-24（执行代理 `W25` · `D1` 的 R3）：**「or dying」那半边补上了** ——
                    //   原版 `CardScript__IsInPlayOrDying.c:5-11` 的卡状态集是 `{2,3,5,15,17}`，
                    //   其中 **`5 = waitingToDie`** 也算在场；而我们的 `cleanupDeaths` 是**当场**
                    //   `BoardSlots.RemoveAt` ⇒ 那个单位**格子上已经找不到**（`slot = -1`）。
                    //   等价物 = `BattleContext.DyingThisAction`（「本条顶层动作里刚离场」，
                    //   doc → `BattleContext.cs`；写入点 `RuleCore.cs:8938`，**在 Unstable/Backlash
                    //   入队之前** ⇒ 入队这一刻查得到）。
                    //   🧨 不加这半边：`Unstable` 的 `abilityContext=40`（现读 18 条全是 40）
                    //   **根本过不了入队闸** ⇒ 不稳定再也不爆（`D1` 的第 5 条）。
                    //   ⚠️ **50 档故意不加**：原版 `IsRightContext.c` 的 50 支是
                    //   `IsInCemetery || IsInPlay`（**不含** state 5）⇒ 与 40 **不等价**（那正是
                    //   `W63` 那条注释写的），别为了「对称」把它一起放宽。
                    return cell != null || IsDyingThisAction(ctx, inst);   // 🔴 含待死（状态 5 也算在场）
                case CardDef.AbilityContextInPlayOrCemetery:
                    return (cell != null && !ctx.IsPendingDeath(cell)) // 🔴 不含待死
                        || InZone(ctx, owner, inst, Zone.Discard);
                case CardDef.AbilityContextHand:     return InZone(ctx, owner, inst, Zone.Hand);
                case CardDef.AbilityContextDeck:     return InZone(ctx, owner, inst, Zone.Deck);
                case CardDef.AbilityContextCemetery: return InZone(ctx, owner, inst, Zone.Discard);
                default:
                    UnityEngine.Debug.LogWarning(
                        $"[ActionPump] 非法 `abilityContext={context}`（合法 0/10/20/30/40/50 或 -1）—— 原版 fail-closed："
                        + "`CardAbility__IsRightContext.c:84-90` 其余值 ⇒ LogWarning + `return 0`（这条就是出声口）");
                    return false;
            }
        }

        /// <summary>🆕 2026-10-24（执行代理 `W25`）：**`InPlayOrDying(40)` 的「or dying」半边** ——
        /// 这一份牌是不是**本条顶层动作里刚被判死**的那一个（原版卡状态 `waitingToDie(5)`）。
        ///
        /// <para>判据：`CardAbility__IsRightContext.c` 的 `0x28(40)` 支 ⇒ `CardScript__IsInPlayOrDying.c:5-11`
        /// 的状态集 `{2, 15, 3, 17, 5}` —— 含 `5`。我们的 `CleanupDeaths` 当场把人从棋盘摘下，
        /// 所以只认 `DyingThisAction`（写点 `RuleCore.cs:8938`，**在 Unstable/Backlash 入队之前**）。
        /// ⚠️ 比的是 `UnitState.Instance`（readonly；翻面成残骸也沿用同一份）—— ⛔ 不比 `UnitState` 引用
        /// （死亡时登记的那一份与我们手上这份可能不是同一个对象）。</para></summary>
        static bool IsDyingThisAction(BattleContext ctx, CardInstance inst)
        {
            if (ctx == null || inst == null) return false;
            foreach (var u in ctx.DyingThisAction)
                if (u != null && ReferenceEquals(u.Instance, inst)) return true;
            return false;
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
                //    · `jamUnit` / `transformUnit` / `removeEffect` 三路：现池**零可达**（三源判据 = 本类尾「账 166」那一段，⛔ 别再重查一遍）
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
                    if (CarrierReplacedByRemnant(ctx, act.Actor) && act.ActionType != 0x2d && act.ActionType != 0x1a)   // 判据 = decomp_full/BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c:77-91（原版对 destroyUnit / recallToHand **保留**：`:84-86` 0x1a=26 全豁免 · `:77-81` 0x2d=45 且 deathType 0x28 poison · `:88-91` shouldIgnoreDestroy 且 0x2d）。按调度台 2026-10-28 裁定简化为「0x2d / 0x1a 一律不撤」（常量现读确认 0x2d=45 destroyUnit、0x1a=26 recallToHand，同 ActionQueue.cs:2156-2157）
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

        // ==========================================================================================
        //  🆕 **2026-10-26（执行代理 `G17` · 批次 6）：ability 级触发前的【第三道闸】`boardCriteria`
        //  （原版 `CardAbility.boardCriteria` `+0x38`）**
        // ==========================================================================================
        //  判据（逐句现读）：
        //    · `d:/2/tools/decomp_full/CardAbility__CanTriggerAbility.c:136-140`：
        //        `BoardAnalysis.MeetsCriteria(manager, *(ability+0x38), thisCard, actingCard, targetCard,
        //                                     ignoreRandomTrigger, 0)`
        //      ⇒ 三道闸是**一条 `&&` 链**（`acting(+0x28) → target(+0x30) → board(+0x38)`）；
        //    · `d:/2/tools/decomp_full/BoardAnalysis__MeetsCriteria.c`（155 行全文）——
        //      语义逐句 = `CountEval.MeetsBoardCriteria`（那一份文件头有逐行对照）。
        //  数据（**已由批次 5 的 `G3` 备好，⛔ 不是本件新落**）：`unitConditions` 里**同一 `ab`** 的
        //    `role="count"` 行 = 这条 ability 的 board gate（`opt` / `op` / `refValue` + `countSet`）。
        //    生成器 `count_row_carries_board_info()` 保证「`targetsAffected != 0` 或 `playerTurn != 0`」
        //    才落行；`CardDatabase.IsUnconditionalCriteriaRole` 放行它的 `cond = 0`。
        //  三态（与 target 闸**同一套**，⛔ 调用点别把 `null` 当 `false`）：
        //    · `true`  = 满足 / **明确没有 board 条件**（`ConditionsVersioned` 且这条 ability 没有这一行）；
        //    · `false` = **明确不满足**（`playerTurn` 不符 / 数出来比较不过）⇒ 调用点不触发 + 出声；
        //    · `null`  = **判不了**（旧表没落 / 定位不到 ability / `countSet` 没落 /
        //                 `CountEval` 那一格本版没实现）⇒ 调用点**出声 + 放行**，`why` 写清是哪一格。
        //  ⚠️ `ignoreRandom`（原版第 6 实参）：由调用方逐路径给；今天生产上的七族
        //    （`EffectResolver.AbilityLevelGateTrigger`）全部传 0（`RawCardScript__TriggerAbilities.c`
        //    的 12 个实参里第 10 个恒 0）⇒ 本入口按 `false` 调。真命中 `RandomChance(8)` 时
        //    `CountEval` 的**掷法**本版未实现 ⇒ 判不了 + 出声（⛔ 不拿 `ctx.Rng.Next` 顶）。
        // ==========================================================================================
        public static bool? BoardLevelCriteriaAllows(BattleContext ctx, int owner, UnitState thisCard,
                                                     CardDef card, string triggerName, out string why)
        {
            why = null;
            if (card == null) { why = "没有卡（判不了）"; return null; }
            // 定位口径只此一处（同 `AbilityLevelConditionRow`）：`UniqueAbilityIndexForTrigger`。
            int ab = card.UniqueAbilityIndexForTrigger(triggerName, out string locateWhy, out bool amb);
            if (ab < 0)
            {
                // 走到这里的前提是 target 闸已经定位成功过 ⇒ 正常路径上不会发生（防御性分支）。
                // ⛔ 不拿 `why` 文案去 `Contains` 判档（`CardDef.UniqueAbilityIndexForTrigger` 的 doc）。
                if (locateWhy == null) return true;
                why = amb ? "ability 定位不唯一 ⇒ board 条件判不了：" + locateWhy : locateWhy;
                return amb ? (bool?)false : null;
            }
            // `null` = 明确「这条 ability 没有 board 条件」（只有 `ConditionsVersioned` 才会走到）；
            // 旧表 ⇒ 返回 `ConditionUnknown` 的哨兵（见 `AbilityLevelConditionRowAt` 的三态 doc）。
            var row = AbilityLevelConditionRowAt(card, ab, "count");
            if (row == null) return true;
            if (row.Condition == CardDef.ConditionUnknown)
            {
                why = "`unitConditions` 列没落（旧表 < v15）⇒ `role=\"count\"` 那一行**未知**"
                    + "（⛔ 不是「没有条件」）";
                return null;
            }
            if (row.CountSet == null)
            {
                why = "`role=\"count\"` 行没有 `countSet`（旧表 < v16 / 生成器没落）⇒ 被数集合未知";
                return null;
            }
            var gate = new CardDef.BoardGateSpec
            {
                CountOption = row.CountOption,
                StatOption = row.StatOption,
                ReferenceValue = row.ReferenceValue,
                CountSet = row.CountSet,
            };
            string cgap;
            // 🔴 `cond` = 这一行的 `cond` 格（原版 `countCriteria.conditionsFilter`）—— **必须传**：
            //   现读 99 条 `role="count"` 行里 6 条非 0（`221`×2 / `331`×2 / `222` / `243`），
            //   不传就等于「把只数带条件的候选」静默变成「全数」（⛔ 那是多数）。
            bool? ok = CountEval.MeetsBoardCriteria(ctx, owner, gate, thisCard, out cgap, false, row.Condition);
            if (ok == null) why = cgap ?? "board 条件判不了";
            return ok;
        }

        // ==========================================================================================
        //  🔴 **2026-10-26（第八十三会话 · 执行代理 `W1` · 账 B9 的 20 行）：
        //  原版 14 个 `AddTrigger*` / 6 个 `ResolveTrigger*` 的【动作类型 + AbilityTrigger +
        //  三卡槽 + 队列道】对照表（判据 = 逐文件现读 `d:/2/tools/decomp_full/`）**
        // ==========================================================================================
        //
        //  ⚠️ **为什么写在类尾、不贴到 `TriggerNameOfKeyword`（`:1081-1123`）每一行旁边**：
        //    本文件被 `资料/完全复刻/账/*.tsv` 按 `文件:行号` 引用（现读 12 处，最大到 `:1606`）；
        //    在那张 switch 里插行会让下游**全部漂移**（CLAUDE.md 铁律 ·「新成员一律追加到类尾」）。
        //    ⇒ 只追加在类尾，**零漂移**。⛔ 别再往 `:1084-1120` 中间插行。
        //
        //  ── 一、原版 `AddTrigger*` 的**统一形状**（除账 187 / 191 两个例外）──────────────────
        //    签名 = `AddTriggerX(manager param_1, targetCard param_2, actingCard param_3,
        //                        priority param_4, blockPlayerActions param_5)`
        //    体内三句（每个文件同一位置）：
        //      · `BattleAction___ctor(action, <actionType>, 0, param_3, param_5, 0)`
        //        ⇒ `casterCard(+0xD8) = param_3` · `blockPlayerActions(+0xF4) = param_5` ·
        //          `checkIfCasterInPlay(+0xD0) = 0`（**14 个全是 0**）
        //      · `local_1f8[0] = param_2` ⇒ `targetCard(+0x10) = param_2`
        //      · `uStack_200 = param_3` ⇒ `actingCard(+0x08) = param_3` —— 🔴 **16 个 `AddTrigger*` 文件里
        //        只有 4 个写**（Mob / Pray / Synapse ＋ 账 187 的 Stimulation，它写的是 `param_4`），
        //        其余 12 个**不写**（`+0x08` 保持 null）；**本表 14 行 = 4 写 10 不写**
        //      · `BattleManager__AddAutoActionToQueue(manager, action, param_4, 0)`
        //        ⇒ **道 = 调用方给的 priority**（0 Player / 1 Instant / 2 Interrupt / 3 DelayedInstant，
        //        见 `Core/ActionQueue.cs` 文件头；本方法**自己不改道**）
        //
        //    🔑 **「+0x08 到底写没写」的判法**（本批复用；16 个 `AddTrigger*` 文件 **100% 一致**）：
        //      Ghidra 的**局部变量声明宽度** ——
        //        `undefined8 local_208 [2]`（16 字节，**盖住** `+0x08`）⇔ **没写**；
        //        `undefined8 local_208`（8 字节）**+** `undefined8 uStack_200` ⇔ **写了**。
        //      与各自 Resolve 侧的读法自洽 ⇒ 可以当判据直接复用，不必每张都读汇编。
        //
        //  ── 二、逐条对照（`+0x08` = actingCard 槽；`+0xD8` = casterCard）────────────────────
        //    账行 | 原版方法                    | actionType        | AbilityTrigger        | +0x08           | Resolve 侧 OnTrigger 三槽
        //    -----|------------------------------|-------------------|-----------------------|-----------------|----------------------------
        //    186  | AddTriggerAmbush             | 0x53 = 83         | 670 Ambush            | **不写**        | `CardScript__TriggerAmbush.c:12-15`：this=acting=target=**自己**
        //    187  | AddTriggerCardPlayedByStimulation | 0x5b = 91    | **字面量 0**（ThisCardPlayed） | **写 param_4** | this=+0x08=**被打出那张战术**·acting=+0xD8=**同一张战术**·target=+0x10=Stimulation 单位（调用点 `param_3==param_4` ⇒ 只有两张不同的卡）
        //    189  | AddTriggerCourtesy           | 0x41 = 65         | 43 Courtesy           | **不写**        | `ResolveTriggerCourtesy.c:217`：this=target(+0x10)·acting=+0xD8·target=null
        //    190  | AddTriggerMob                | 0x50 = 80（`isAdditionalMobTrigger` 真 ⇒ **0x51 = 81**） | 300 Mob | **写 param_3** | `ResolveTriggerMob.c:218`：this=target·acting=+0xD8·target=null
        //    191  | AddTriggerPlayerCodex        | 0x46 = 70         | 145 Codex             | **不写**        | `CardScript__TriggerCodex.c:29`：this=acting=自己·target=`codexSource ?? 自己`
        //    192  | AddTriggerPray               | 0x52 = 82         | 655 Pray              | **写 param_3**  | `ResolveTriggerPray.c:217`：this=target·acting=+0xD8·target=null
        //    193  | AddTriggerRegiment           | 0x58 = 88         | 301 Regiment          | **不写**        | `CardScript__TriggerRegiment.c:12-15`：三槽全自己
        //    194  | AddTriggerRelentless         | 0x3e = 62         | 230 Relentless        | **不写**        | `ResolveTriggerRelentless.c:217`：this=target·acting=+0xD8·target=null
        //    196  | AddTriggerSacrifice          | 0x3f = 63         | 220 Sacrifice         | **不写**        | `CardScript__TriggerSacrifice.c:41-43`：三槽全自己
        //    197  | AddTriggerSlay               | 0x59 = 89         | 120 Slay              | **不写**        | `CardScript__TriggerSlay.c:12-15`：三槽全自己
        //    198  | AddTriggerSpiritStone        | 0x4d = 77         | 600 UseSpiritStone    | **不写**        | `CardScript__TriggerSpiritStone.c:15-16`：this=自己·**acting=param_2（≠自己）**·target=自己
        //    199  | AddTriggerSynapse            | 0x4e = 78         | **630 TriggeredSynapse** | **写 param_3** | `CardScript__TriggeredSynapse.c:6-9`：this=自己·acting=param_2·target=null
        //    200  | AddTriggerTeleport           | 0x5a = 90         | 690 Teleport          | **不写**        | `CardScript__TriggerTeleport.c:12-15`：三槽全自己
        //    201  | AddTriggerUprising           | 0x54 = 84         | 680 Uprising          | **不写**        | `CardScript__TriggerUprising.c:12-15`：三槽全自己
        //
        //    ⚠️ **两个例外（⛔ 别按上表推）**：
        //      · **账 187**：签名只有 4 参、**没有 priority**（`AddTriggerCardPlayedByStimulation.c:2`），
        //        且 `AddAutoActionToQueue(…, **1 硬编码**, 0)`（`:79`）；
        //        `casterCard = param_3 = 那张战术`、`targetCard(+0x10) = param_2 = 被刺激的单位`、
        //        `actingCard(+0x08) = param_4 = 被打出那张`（`:27-30`）。
        //        调用点 = `RawCardScript__OnCardPlayedWithTarget.c:56`。
        //      · **账 191**：签名顺序**与其它 13 个相反** —— `AddTriggerPlayerCodex(manager param_1,
        //        actingCard param_2, targetCard param_3, priority param_4, blockPlayerActions param_5)`；
        //        `casterCard = param_2`（`:27`）、`targetCard(+0x10) = param_3`（`:28`）。
        //
        //  ── 三、`ResolveTrigger*` 侧（出队闸 + 真正的 `OnTrigger`）──────────────────────────
        //    · **602** `ResolveTriggerAmbush.c:128-130`：出队闸 = `targetCard` 非空 ∧
        //      `CardScript.IsInPlayOrDying()`；随后 `:226 CardScript__TriggerAmbush()`（三槽全自己）。
        //      🔴 **语义上只读 `+0x10`（`param_2[2]`）**（`if (param_2[2] != 0) { IsInPlayOrDying(param_2[2]) … }`）；
        //      `+0x08` / `+0xD8` 除了 Ghidra 那两段「整块 0x100 结构体搬运副本」以外
        //      **没有任何语义读点**（全文件核过，`0xd8` 零命中）⇒ 「`casterCard` ≠ `actingCard`」
        //      这个区分**观测不到**（原判据说「两槽表达不了」，实测是**观测等价**）。
        //      ⚠️ 后面 `:127 if (local_f8 != 0) { IsInPlayOrDying(); … }` 那个 `local_f8` 是
        //      **幻影局部量**（Ghidra 全文件无赋值点）⇒ 它应就是 `targetCard`，⛔ 别当成第三张卡。
        //      闸我们由 `abilityContext = 40 (InPlayOrDying)` 承担。
        //    · **603** `ResolveTriggerCardPlayedByStimulation.c:205-208`：现场从池里取一个
        //      `<>f__AnonymousType31` 闭包（`thunk_FUN_18042f4a0` + `..ctor(x, 0)`），
        //      调 `RawCardScript.OnTrigger(this, trigger, mgr, thisCard, actingCard, targetCard,
        //      resolveOnlyEnchantments, ignoreRandomTrigger)` —— **8 个实参、trigger 位是字面量 `0`**。
        //      🔴 **`0` 的语义已 VA 级定案 = `AbilityTrigger.ThisCardPlayed(0)`，不是「跑整条 ability」的哨兵**：
        //        ① `RawCardScript.OnTrigger`（7 参重载，VA **`0x18094A9F0`**，RVA `0x94A9F0`）
        //           **整份体只有 19 行、是一个纯转发器**（Ghidra `.c` 全读 + 机器码核对：
        //           `0x18094AA17 mov ebp,edx` 保存第 2 实参、`0x18094AA69 mov edx,ebp` 原样放回、
        //           `0x18094AA9F call 0x18094B420`），**对 trigger **不做任何比较/分支**；
        //        ② `0x18094B420` = `RawCardScript.TriggerAbilities`，它把 trigger 原样传给
        //           `CardAbility__CanTriggerAbility(ability, trigger, …)`；
        //        ③ `CardAbility__CanTriggerAbility.c:71` = `if (*(int *)(ability + 0x10 /* triggerValue */)
        //           != trigger) return 0;` ⇒ **`trigger == 0` 就是「只挑 `triggerValue == 0` 那条能力」**。
        //      ⇒ 原版这句 = **把「被打出那张战术」自己的 `ThisCardPlayed` 能力跑一遍**。
        //      三槽 = `+0x08`(被打出那张) / `+0xD8`(**同一张**，因为调用点 `param_3 == param_4`) /
        //      `+0x10`(被刺激的单位) ⇒ **只有两张不同的卡 ⇒ 我们的两槽模型表达得了**。
        //    · **605/610** `ResolveTriggerCourtesy.c:217` / `ResolveTriggerRelentless.c:217` ——
        //      **逐字节同形**，只差 trigger id（43 vs 230）与 type 常量（65 vs 62）。
        //    · **607** `ResolveTriggerPlayerCodex.c`：`:185 TriggerCodex(自己)` →
        //      `:233-283` 若 `raw.HasAbilityType(0x91)` 则 `BroadcastUnitCodex(自己)` →
        //      `:285-511` **循环**扫「同阵营 ∧ 在场 ∧ 非自己」逐张 `TriggerCodex(那张)`，
        //      且该张 `HasCurrentTrait(1000 = codex trait)` 时再 `BroadcastUnitCodex`。
        //      ⇒ **Codex 是群体触发**（原判据说「我们只做了第一段」—— 实测**已实现**：
        //        `RuleCore.CheckCodex`（`RuleCore.cs:12917` 起）扫整排 `p.Board`，
        //        2026-10-14 `A1155①` 已删掉那句多写的 `break`；同阵营/在场/非自己由
        //        「按 `p` 扫本排」+ `Has(Codex)` + 每轮重读 `Board[s]` 承担）。
        //
        //  ── 四、我们的**两槽模型** ↔ 原版三槽（`RuleCore.FireTriggerAt`）──────────────────
        //    `FireTriggerAt(ctx, u, keyword, owner, slot, chosen, subject, …)`：
        //      · `u`       = 原版 `OnTrigger.thisCard`（还决定读谁的 `FxOps(keyword)`）；
        //      · `subject` = 原版 `OnTrigger.actingCard`（`RuleCore.cs:12180-12190` 的 doc 写明了；
        //                    **缺省 `null` = `u` 自己**）；
        //      · `chosen`  = 这一条 op 的**目标**（≈ `targetCard`，但它是 **op 级**、不是 OnTrigger 级）。
        //    ⇒ **两槽够用的判据**：原版三槽满足 `thisCard == actingCard`（`targetCard` 可同可异）
        //      时，`u` + 缺省 `subject` 就是**逐字同形** —— 本表 14 个 trigger **全部**属于这一档
        //      （🔴 2026-10-26 `W1b` 就地订正：原判据说「187/603 三张卡全不同」，**错** ——
        //        调用点 `param_3 == param_4` ⇒ `thisCard == actingCard`，见 §三·603 的 VA 定案）。
        //    ⇒ **两槽真的不够的，只剩一条、且不在本文件**：
        //      · 账 572 `UnitHealed`（`RuleCore.HealUnit`）：thisCard=被治那张、actingCard=治疗源、
        //        targetCard=原 targetCard —— 三张里**两张不同且都要**，补它要扩签名 + 单开一批。
        //      · 账 198 / 199 的 `actingCard = param_2`：那一槽**能**用 `subject` 表达，
        //        但 `ResolveTriggerSpiritStone.c:266` 的 `local_30` / `TriggeredSynapse` 的
        //        `param_2` 在 Ghidra 体里**无赋值点**（幻影局部量）⇒ **具体传的是哪张卡未定**，
        //        如实标着（⛔ 不猜）。
        //    ⛔ **别为了让这几条「表达得出」去改 `FireTriggerAt` 的签名**：它被全引擎的触发链
        //      （`RuleCore` C5 / `EffectResolver` / 附魔递归）引用，扩签名要单开一批 + 全量自检。
        //
        //  ── 五、本批顺手把两个裸偏移表具名（⛔ 别再各处重推）────────────────────────────────
        //    ① `VarsGlobal` 的 **5 条优先级道**（原版 `AddAutoActionToQueue` 的 `priority` 来源）：
        //       `+0xb4 playerActionPriority` · `+0xb8 instantPriority` · `+0xbc deathPriority` ·
        //       `+0xc0 backlashPriority` · `+0xc4 turnEndPriority`
        //       判据 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/VarsGlobal.cs` 字段声明序
        //       （`deckSize` 起 `+0x18`，逐个 4 字节、`Vector2` 占 8）；**两处独立验证**：
        //       `+0x6c addCardToDeckShowScale`（`AddCardInPlayToDeck.c`）·
        //       `+0x70 cardDrawnScale`（`AddActiveTrapToHand.c`）都与字段表吻合。
        //    ② `BattleAction` 字段偏移（这批 `Add*` 行全靠它）：
        //       `+0x00 actionType(int)` · `+0x04 isPlayer(bool)` · `+0x08 actingCard` ·
        //       `+0x10 targetCard` · `+0x18 multiTargetCard` · `+0x38 attackType` ·
        //       `+0x70 cardEffect` · `+0x78 manaBuff` · `+0x88 newCardData` ·
        //       `+0xD0 checkIfCasterStillInPlay` · `+0xD8 casterCard` · `+0xE0 casterCardRaw` ·
        //       `+0xEC keepEffects` · `+0xF4 blockPlayerActions` · `+0xF6 toRemnant`
        //       （判据 = `BattleAction.cs` 字段声明序 + `BattleAction__.ctor.c:1-30` 的四处写点）。
        //
        //  ── 六、逐条【我方落点】（写手/审查按这个找，⛔ 别再按方法名 grep 猜）──────────────
        //    186 Ambush      → `TriggerNameOfKeyword` 的 `case KeywordTable.Ambush`（`:1113`）；
        //                      触发口 `RuleCore.DeclareAttack`（`RuleCore.cs:6267`，翻开后补打）
        //                      与 `RuleCore.ApplyDamage`（`RuleCore.cs:8592`，挨伤害翻开）
        //    187/603 Stimulation → `TriggerNameOfKeyword` 的 `case KeywordTable.Stimulation`（`:1117`）；
        //                      🔴🆕 **2026-10-26 `W1b` 就地改判 C → A（VA 定案后）**：原版这一段是
        //                      **两条动作，但它们拼起来就跑「这张战术自己的效果」一次** ——
        //                        (a) `OnTrigger(710 Stimulation, thisCard=被选中的单位, actingCard=那张战术,
        //                            targetCard=被选中的单位)`（`RawCardScript__OnCardPlayedWithTarget.c:53-55`，
        //                            闸 = `playedCardType == 0x14(Tactic)` ∧ `HasCurrentTrait(chosen, 0x4dd=1245
        //                            = DefinedTrait.stimulation)`）；
        //                        (b) `AddTriggerCardPlayedByStimulation` → 出队后 `OnTrigger(0 ThisCardPlayed, …)`，
        //                            **它替代**了同一函数里那条 `TriggerAbilities(0)`（`:40`）——即
        //                            **「把这张战术自己的效果跑一遍」只是改成排队、并稍带上 (thisCard=actingCard=战术,
        //                            targetCard=被刺激单位)**。⛔ **不是**「再跑一次额外的东西」。
        //                      🔴 为什么判 A（四条都核过）：
        //                        ① `0` = `ThisCardPlayed`（VA 定案，见 §三·603）；
        //                        ② 三槽里 `thisCard == actingCard`（调用点 `param_3 == param_4`）⇒ 两张卡，
        //                           两槽模型够用；
        //                        ③ **次序**：原版 (a) 入队、(b) 入队 ⇒ FIFO「先激励、后战术正文」。
        //                           我们 `EffectResolver.PlayTactic` 就是 `FireTriggerAt(Stimulation)`（`:2557`）
        //                           先入 Instant 道、再 `EnqueueAbility(Instant, "tactic-body:")`（`:2588`）
        //                           —— 这正是 2026-10-22 `W62·R2` 为这一格做的修正，**逐字同序**；
        //                        ④ 我们「战术自己的效果」= `PlayTacticBodyDeferred` 跑卡面正文
        //                           （= 原版 `TriggerAbilities(0)` 那条 ability 的等价物），`chosen` 传的就是
        //                           被刺激的单位 ⇒ `targetCard` 那一槽也对上。
        //                      ⚠️ **残留差异（观测不到，如实标着）**：原版 `actingCard` 槽填的是**这张战术**，
        //                      我们这一路没有「actingCard」概念（跑自己的正文不需要它）；若将来有卡的正文
        //                      依赖「我是被谁触发的」，这条要重开。
        //                      ⚠️ 另注：`PlayTacticBodyDeferred` 跑的是**解析出来的 op**，原版走的是
        //                      `TriggerAbilities(0)`（含 `CanTriggerAbility` 的 ability 级三道闸）——
        //                      那是**「卡面正文到达率」那条更大的账**，不在本行。
        //    189/605 Courtesy   → ✅ **2026-10-26（批次 9 · 执行代理 `E7P06`）：映射 + 两段入口已落地**
        //                      （改前是「整条没有」）。落点 = `ExtraTriggerName`（`courtesy` → `Courtesy`/43）
        //                      ＋ `EnqueueGrantedTrigger`（= 原版 `AddTriggerCourtesy` 入队 ＋
        //                      `ResolveTriggerCourtesy` 的 `IsInPlayOrDying` 闸 ＋ `OnTrigger(43)` 三槽）；
        //                      判据与残余（`KeywordTable` 常量 / `EffectResolver` 的 `chosenAbility 400` 分派）
        //                      → 类尾 §九。时间限定：接上前两句后**立刻可达**，`play check` 必须重比。
        //    190 Mob         → `:1104` `case KeywordTable.Mob`；触发口 `RuleCore.cs:6678/10201`；
        //                      `isAdditionalMobTrigger` 那第二档 type（81）我们不需要建模 ——
        //                      两条 type 在 Resolve 侧落到**同一个** `OnTrigger(300)`
        //                      （`ResolveTriggerMob.c:218`），差别只在 `param_3` 的一个分支。
        //    191/607 Codex   → `:1101` `case KeywordTable.Codex`；触发口 `RuleCore.CheckCodex`
        //                      （`RuleCore.cs:12917` 起，见 §三·607）。
        //    192 Pray        → `:1111` `case KeywordTable.Pray`。
        //    193 Regiment    → `:1105` `case KeywordTable.Regiment`。
        //    194/610 Relentless → ✅ 同 189（`relentless` → `Relentless`/230）：映射 + `EnqueueGrantedTrigger`
        //                      已落地；两份 `.c` 与 Courtesy **逐字节同形**（`diff` 只剩函数名 / 一个标签 /
        //                      trigger id `0x2b(43)` vs `0xe6(230)`，actionType `0x41(65)` vs `0x3e(62)`）
        //                      ⇒ 一个入口同时覆盖两族，⛔ 别为它再写第二份。
        //                      残余同 189（`KeywordTable` 常量 / `chosenAbility 370` 的 op 分派；判据 → 类尾 §九）。
        //    196 Sacrifice   → `:1102` `case KeywordTable.Sacrifice`。
        //    197 Slay        → `:1100` `case KeywordTable.Slay`；出队复核 = `PumpActionQueues` 的
        //                      `RequireActorInPlayOrDying` 那一支（本文件 `:1536-1546`，判据
        //                      `ResolveTriggerSlay.c:116-226` 的 `CardScript.IsInPlayOrDying`）。
        //    198 SpiritStone → `:1110` `case KeywordTable.SpiritStone`（名字 = `UseSpiritStone`）；
        //                      ⚠️ 起触发的是`ResolveSpiritAbility` 一族，不走 `FireTriggerAt(SpiritStone)`。
        //    199 Synapse     → 🔴 **本表【不该】加 `synapse`**：`TriggerNameOfKeyword` 只服务
        //                      「`FireTriggerAt` 拿到的关键词 → AbilityTrigger 名」这一条路，
        //                      而突触在我们这里是 `EffectResolver.RepeatTacticOnAdjacent` 尾部那条
        //                      `BroadcastKeywordEvent(Triggers(synapse))`（不经过 `FireTriggerAt`）；
        //                      630 `TriggeredSynapse` 的名字映射在 `EffectResolver.TriggerNameOfWhenKind`
        //                      （`TN_TriggeredSynapse`，判据 `CardScript__TriggeredSynapse.c:6-9`）。
        //                      ⛔ 别往 `:1084-1120` 里加 `case KeywordTable.Synapse` ——
        //                      没有任何调用点，加了就是死代码（而且会插行导致账漂移）。
        //    200 Teleport    → `:1115` `case KeywordTable.Teleport`；触发口 `RuleCore.cs:4186`。
        //    201 Uprising    → `:1114` `case KeywordTable.Uprising`。
        //
        //  ── 七、账 166 `AddRemoveEnchantment`（`removeEffect(16)`）的三源判据 ──────────────
        //    **原版可达**（所以 ⛔ 不能判「原版没有」）：唯一调用方 =
        //      `AbilityLogic__PlayAbility.c:2280`（另一处 `:2360` 同一段），双闸 =
        //      `abilityLogic == 0x154 (340)` ∧ `cardEffect.spellType == 0x0f (15)`。
        //    **本 build 不可达**：
        //      ① `cards_engine.json` 1,128 张里 `spellType == 15` 的 **0 张**
        //         （`cards_truth.json` 1,351 张同 —— `spellType` 分布只有 0/90/150/180/190/200/
        //          210/220/230/240/260/265/270，**没有 15**）；
        //      ② 我们引擎**没有** `removeEffect(16)` 的发射口 —— 全仓只有
        //         `Core/TutorialScript.cs:114` 的枚举名与 `ActionPump.cs` 的注释；
        //         `EffectResolver` 里也没有对应的 op 分派档；
        //      ③ `activeAbilityLogic` 132 张卡里 0 张带 340 的整条逻辑。
        //    ⇒ **状态 = D（本 build 不适用）+ 时间限定**：出现 `spellType == 15` 的卡 /
        //      `removeEffect` 真接上分派档时**必须重跑**本条（它不是「原版没有」，是「这版池里到不了」）。
        //    ⚠️ 原版这条动作**只带 targetCard + cardEffect 两颗**（`AddRemoveEnchantment.c:28-30`：
        //      `casterCard` 也填 targetCard），`blockPlayerActions = param_5`。
        //
        // ==========================================================================================
        //
        //  ── 八、`TriggerNameOfKeyword` 的【不可插行】补充行（⛔ 别把它们搬回 `:1084-1120` 那张 switch）
        // ==========================================================================================
        //  🔴 **为什么要有这个方法**：那张 switch 被 `账/*.tsv` 按 `文件:行号` 引用（现读 12 处）；
        //    往里插一行 `case` 会让**本文件下游所有落点 + 本段（`:1653` 起）整体 +1**。
        //    ⇒ 新补的映射一律走 `default:` 这一跳（`TriggerNameOfKeyword` 的 `default` 现在转调本方法），
        //      本方法**只追加、不改既有行**。
        //
        //  · **账 187/603（`W1b` 顺手发现 · 第八十三会话）**：`RuleCore.FireSpentAllMana`
        //    （`RuleCore.cs:12802` 起）会 `FireTriggerAt(w, KeywordTable.SpentAllMana, …)`（`:12812`），
        //    但原表**没有 `spentallmana` 这一行** ⇒ `_kwTriggerName == null` ⇒
        //    `AbilityContextOfAbilityV14(card, null, "spentallmana")` 的 `TriggerNameOfKeyword` 那一跳
        //    也拿不到名字 ⇒ `abilityContext` 恒走 `-1` 回退口径（= ability 级四道闸**静默不判**）。
        //    ✅ 原版对应物 **存在**：`CardScript__TriggerSpentAllMana` → `OnTrigger(0x281 = 641
        //    = `AbilityTrigger.SpentAllMana`)`（`SpentAllMana` 在 `CardDef.AbilityTriggerNumberOfName`
        //    `CardDef.cs:1940` 名字表里本来就有）。
        //    📊 **今天零可观测**（现读 `cards_engine.json` 1,128 张）：`abilityTriggers` 含 **641 的 0 张**、
        //    `desc`/`descZh` 里出现 `SpentAllMana` 的也是 **0 张** ⇒ 补它**不改任何现有行为**；
        //    补的是「原版有、我们缺」这一层（铁律 11：休眠机制也要做）。
        //    ⚠️ 一个都没补的还有 **`backlash`** —— 但它不受影响：`TriggerSkipsAbilityContext`
        //    的**关键词**那一半已经兜住了它（见那一支），所以不需要名字。
        //    📌 **同一套形状的既有先例** = `WhenEvents.ExtraTriggerNameForKind`（`Core/WhenEvent.cs:2087`）
        //    —— `TriggerNameOfWhenKind` 的 switch 之后也是这么兜底的（`EffectResolver.cs:7421`）。
        static string ExtraTriggerName(string keyword)
        {
            if (keyword == KeywordTable.SpentAllMana) return "SpentAllMana";   // 641
            // 🆕 **2026-10-26（批次 9 · 执行代理 `E7P06` · 账 189/605 · 194/610）：`courtesy` / `relentless`。**
            //   原版两个 trigger 号 = `CardDef.AbilityTriggerNumberOfName` 里早就有的那两条
            //   （`CardDef.cs:1919 {"Courtesy", 43}` / `:1927 {"Relentless", 230}`），缺的只是
            //   「引擎关键词 → 这个枚举名」这一跳。没有它 ⇒ `TriggerNameOfKeyword` 返回 null ⇒
            //   `AbilityContextOfAbilityV14(card, null, "courtesy")` 拿不到名字 ⇒ 四道 ability 级闸
            //   **静默不判**（同 §八 里 `spentallmana` 那一格的形状）。
            //   ⚠️ **写字符串字面量、不写 `KeywordTable.Courtesy`**：那两个常量还没建
            //   （`KeywordTable` 在 `Core/CardDef.cs`，不在本件白名单）⇒ 等那边加了常量后
            //   **这里改成引用**（⛔ 别两处各留一份字面量）。判据与残余 → 类尾 §九。
            // ⚠️ 今天池内 **0 张**卡的 `keywords` 里真的有这两个词（`cards_engine.json` v32 现扫），
            //   所以本行**零行为变化**，补的是「卡面/数据一出现就自动生效」这一层（铁律 11）。
            if (keyword == KeywordTable.Courtesy) return "Courtesy";        // 43
            if (keyword == KeywordTable.Relentless) return "Relentless"; if (keyword == KeywordTable.TriggerOnceIfBoardCriteria) return "TriggerOnceIfBoardCriteria";   // 230 / 200（200 这一格 = B90·B1 补：`RawCardScript__UpdateTriggerOnce.c:13` 那一趟的名字）
            return null;
        }

        // ==========================================================================================
        //  §九 🔴 **2026-10-26（批次 9 · 执行代理 `E7P06` · 账 189/605 · 194/610）：
        //  `Courtesy(43)` / `Relentless(230)` 两族 —— 原版「Add（入队）+ Resolve（结算）」两段机制**
        // ==========================================================================================
        //  ⚠️ 为什么写在类尾：本文件被 `账/*.tsv` 按 `文件:行号` 引用（见 §七 开头那段的同一条理由）
        //     ⇒ 只追加，零漂移；那张关键词 switch（`:1084-1123`）一个字都不动（新映射走 `ExtraTriggerName`）。
        //
        //  ── 一、原版四个方法体（第一权威 = `d:/2/tools/decomp_full/`，逐个现读）──────────────
        //   · **`BattleManager__AddTriggerCourtesy.c:20-79`** —— 与
        //     **`BattleManager__AddTriggerRelentless.c:20-79` 逐字节同形**，只差 actionType
        //     （`0x41 = 65 triggerCourtesy` / `0x3e = 62 triggerRelentless`）。体内三句：
        //      ① `BattleAction___ctor(action, <type>, 0, param_3, param_5, 0)`（`:27`）
        //         ⇒ `casterCard(+0xD8) = actingCard` · `blockPlayerActions(+0xF4) = param_5` ·
        //            `checkIfCasterInPlay(+0xD0) = 0`；
        //      ② `local_1f8[0] = param_2`（`:28`）⇒ `targetCard(+0x10) = targetCard`；
        //         `+0x08`（`actingCard` 槽）**不写**（判法 = 局部量声明宽度，见 §一 那条通用判法）；
        //      ③ `AddAutoActionToQueue(manager, action, param_4, 0)`（`:77`）⇒
        //         **纯入队、不判任何前置**、不读卡状态；道 = 调用方给的 `priority`。
        //   · **`BattleManager__ResolveTriggerCourtesy.c:28-264`** —— 与 `…Relentless.c` **去掉常数后完全一致**
        //     （实测 `diff` 只剩函数名 / 一个标签 / trigger id `0x2b(43)` vs `0xe6(230)`）：
        //      ① `targetCard(+0x10) != null` 且 `CardScript.IsInPlayOrDying()` **为假** ⇒
        //         `FinishResolvingAction(<type>)` 后**直接返回**（②不跑）；
        //      ② `raw = targetCard.rawCardData(+0x28)`；`raw != null` ⇒
        //         `RawCardScript.OnTrigger(<43/230>, manager, thisCard = targetCard,
        //          actingCard = casterCard(+0xD8), targetCard = null)`（`:217`）
        //         ⇒ **三槽只涉及两张卡**：`thisCard == targetCard`、`actingCard` = 发起那次登记的单位；
        //      ③ `FinishResolvingAction(<type>)`。
        //     ⚠️ `targetCard == null` 或 `rawCardData == null` ⇒ 原版 NRE（`FUN_1803f47a0` 不返回）
        //        —— **我们不复制崩溃语义**（出声 + 不触发，见下面的入口）。
        //   · **唯一调用点** = `AbilityLogic__PlayAbility.c:2006`（`chosenAbility == 0x172 = 370` ⇒ Relentless）
        //     与 `:2102`（`chosenAbility == 0x190 = 400` ⇒ Courtesy）：都是「**对目标列表逐个**各入队一条」，
        //     实参形状 = `(manager, targetCard = 那个目标, actingCard = abilityData.thisCard(+0x10),
        //                 priority = abilityData.ability.priority(+0x44), blockPlayerActions = 1)`。
        //
        //  ── 二、我们这边的两段映射（结构不同、序列等价；⛔ 别在别处再写一份）──────────────────
        //    ① **Add** = `EnqueueAbility(..., actor: target, requireActorInPlayOrDying: true, abilityContext: 未知)`
        //       —— 纯入队、入队时不判前置（与原版 `Add*` 同形；`abilityContext` 留未知 ⇒ 入队那一刻
        //       不跑 `IsRightContext`，正合原版「Add 什么都不判」）。
        //    ② **Resolve** = **泵点出队时**那道 `QueuedAction.RequireActorInPlayOrDying` 复核
        //       （`PumpActionQueues` 的 `IsOnBoardOrDying` 那一支）= 原版 `CardScript.IsInPlayOrDying`；
        //       闸的判据**只此一份**，本段不另写一遍。
        //    ③ 出队后的正文 = `FireTriggerAt(target, keyword, …, subject: actingCard)`
        //       —— `u` 是 `OnTrigger.thisCard`、`subject` 是 `OnTrigger.actingCard`（见 §四 的
        //       两槽模型；原版 `thisCard == targetCard` 且 `targetCard` 槽为 `null` ⇒ `chosen` 传 `null`）。
        //    ④ 关键词 → trigger 名 = `ExtraTriggerName`（`courtesy` → `Courtesy(43)` /
        //       `relentless` → `Relentless(230)`）。**两段必须成对**：只做④不做②③ ⇒ 卡面认得出、
        //       但没人登记 ⇒ 永远不触发（那正是改前的形状）。
        //
        //  ── 三、⏭ 还差什么（都在别的写手的文件里，⛔ 本件不动）────────────────────────────────
        //    · **数据侧「认得出」**：`CardDef.KeywordTable` 仍没有 `Courtesy` / `Relentless` 常量；`Prefixes`
        //      **`relentless` 2026-10-26 `B8_D` 已登记**（原文 = `new[]{"relentless","relentless"}`），**`courtesy` 仍缺**
        //      ⇒ 卡数据真写 `courtesy` 会被 `Normalize` **静默丢弃** —— 落点 = `Core/CardDef.cs`（P-07，⛔ 本件不动）。
        //    · **调用点**：`chosenAbility 370 / 400` 的 op 分派在 `EffectResolver`（P-02）。
        //      今天池内这两档 **0 张卡**（`cards_truth.json` 1,351 张逐张扫 `chosenAbility`
        //      = 370/375/390/400 **各 0**；`cards_engine.json` v32 的 1,128 张同）
        //      ⇒ **本件落地后 `play check` 指纹必须逐字节不变**（已验，见报告 §二）。
        //
        //  ── 四、池内可达性（时间限定）──────────────────────────────────────────────────────────
        //    今天 **0 张**卡带 `courtesy` / `relentless` 关键词，也没有任何 ability 的
        //    `chosenAbility` 是 370 / 400 ⇒ 本族是**休眠档**（与 §六 里 189/194 当年的判法同口径）。
        //    ⛔ **不是「不用做」**（铁律 11）：映射 + 两段入口已落地；一旦卡池出现带这两个词的卡、
        //    或出现 chosenAbility 370/400 的 ability ⇒ 本族**立刻可达**，届时只需接 P-02 的 op 分派。
        // ==========================================================================================

        /// <summary>
        /// **原版 `AddTriggerCourtesy` / `AddTriggerRelentless` 那一族的等价入口**（两段合一）：
        /// 「给 <paramref name="target"/> 登记一条 <paramref name="keyword"/> 触发」——
        /// **立即入队**一条动作（= 原版 `Add*`：纯入队、无前置），出队时按
        /// `CardScript.IsInPlayOrDying` 复核（= 原版 `Resolve*` 的第一道闸，由泵点那道
        /// `RequireActorInPlayOrDying` 承担），再跑**目标自己**的那条能力
        /// （= 原版 `OnTrigger(<trigger>, …, thisCard = target, actingCard = casterCard, targetCard = null)`）。
        ///
        /// <para>**判据**（逐句现读，全文 → 类尾 §九）：
        /// `decomp_full/BattleManager__AddTriggerCourtesy.c:20-79` ·
        /// `decomp_full/BattleManager__ResolveTriggerCourtesy.c:28-264`（与 `…Relentless.c` 去掉常数后
        /// **完全一致**）· 唯一调用点 `AbilityLogic__PlayAbility.c:2006`（`chosenAbility 370`）/
        /// `:2102`（`chosenAbility 400`）。</para>
        ///
        /// <para>⚠️ **`blockPlayerActions = 1`**（原版 Add 的第 5 实参恒真）：那是「这条动作结算期间
        /// 玩家不能操作」的输入锁 —— 我们**没有帧循环也没有输入锁**（`ActionScope` 的形状见文件头），
        /// 这条语义**无对应物**，如实标着（它只影响 UI 可操作性，不进结算指纹）。</para>
        ///
        /// <para>⚠️ **只判据-backed 到这两个关键词**：同族 14 个 `AddTrigger*` 里，
        /// `+0x08` 写不写、Resolve 侧三槽怎么填**各不相同**（对照表 → 类尾 §二）。
        /// 本入口的三槽口径 = **本族（Courtesy / Relentless）实测的那一种**；别拿它顶
        /// `SpiritStone` / `Synapse` 那种 `actingCard ≠ 发起者` 的档（见 §四 的「两槽真的不够」）。</para>
        /// </summary>
        /// <param name="keyword">引擎侧关键词（`courtesy` / `relentless`；⛔ 不是 trigger 枚举名 ——
        /// 名字那一跳由 <see cref="TriggerNameOfKeyword"/> / <see cref="ExtraTriggerName"/> 承担）。</param>
        /// <param name="target">被登记的那张卡（原版 `targetCard`）。`null` ⇒ 出声 + 不入队
        /// （原版此处 NRE，⛔ 我们不复制崩溃语义）。</param>
        /// <param name="actingCard">发起这次登记的单位（原版 `Add*` 的 `actingCard` ⇒ 落到
        /// `casterCard(+0xD8)`，Resolve 时作为 `OnTrigger.actingCard`）。`null` ⇒ `FireTriggerAt` 的
        /// `subject` 取缺省（= `target` 自己）—— 与「没有发起者」的那些路径同口径。</param>
        /// <param name="lane">进哪一条道。**判据 = 原版调用方给的 `priority`**
        /// （`AbilityLogic__PlayAbility.c:2006/2102` 传的是 `abilityData.ability.priority(+0x44)`），
        /// ⛔ **不是** `TriggerLane(target, keyword)`（那是「目标自己那条能力的道」，用在**下一跳**：
        /// `FireTriggerAt` 内部会自己按它入队）。</param>
        /// <param name="label">人话标签（日志 / 排查用）；`null` ⇒ `<目标名>·<keyword>·granted`。</param>
        /// <returns>入队成功与否（`ActionQueue.Enqueue` 的返回值；非法道会被它出声丢弃）。</returns>
        public static bool EnqueueGrantedTrigger(BattleContext ctx, string keyword, UnitState target,
                                                 UnitState actingCard, ActionPriority lane,
                                                 string label = null)
        {
            if (ctx == null) return false;
            if (string.IsNullOrEmpty(keyword)) return false;
            if (target == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[ActionQueue] **不登记**一条触发 —— 目标卡是 `null`。原版 "
                  + "`BattleManager__ResolveTriggerCourtesy.c:28` 在这一格会 NRE（`FUN_1803f47a0` 不返回），"
                  + "**我们不复制崩溃语义**（⛔ 不是静默丢，这条就是出声口）。");
                return false;
            }
            // 出队时才算格位：原版 Resolve 侧也只读 `targetCard` 本身（不读存档时的格位）⇒
            // 这里与 `FireTriggerAt` 的其它调用点同形（格位显式传，供表现层反查视图）。
            System.Action body = () =>
            {
                int o, s;
                FindBoardCell(ctx, target, out o, out s);
                // 🆕 2026-10-26（`E7P06`）：`chosen`（≈ 原版 `targetCard` 那槽）**传 null** ——
                //   原版这一跳三槽是 `thisCard = targetCard` / `actingCard = casterCard` / **`targetCard = null`**
                //   （`ResolveTriggerCourtesy.c:217`），⛔ 别顺手把 `target` 也填进去（那会让卡面代词
                //   「it」多指一次，且 `TargetIfMeetsCriteria` 那一档候选会变）。
                FireTriggerAt(ctx, target, keyword, o, s, chosen: null, subject: actingCard);
            };
            return EnqueueAbility(ctx, lane,
                                  label ?? ((target.Name ?? "?") + "·" + keyword + "·granted"),
                                  body,
                                  actor: target,
                                  // ② = 原版 `Resolve*` 的出队闸 `CardScript.IsInPlayOrDying`
                                  // （`ResolveTriggerCourtesy.c:28-31`）—— 判据只此一份（泵点那一支）。
                                  requireActorInPlayOrDying: true, actionType: keyword == KeywordTable.Courtesy ? 0x41 : keyword == KeywordTable.Relentless ? 0x3e : 0);   // 账 L490 · ActionType 回填：courtesy=triggerCourtesy 0x41=65 · relentless=triggerRelentless 0x3e=62（判据 W2 §附录 A）
        }

        // ==========================================================================================
        //  🆕 2026-10-28（执行代理 `B89_E1` · 完全复刻批次 R9「动作队列族」· 账行 L241 / L242 / L832）
        //  **离场单位的「入队后清扫」** —— 原版三层里的 ② / ③（① 在 `Core/ActionQueue.cs`：
        //  `ActionQueue.RemoveQueuedActionAt` / `RemoveQueuedAction`）。
        //
        //  判据（第一权威 · 逐句现读）：
        //    · `decomp_full/BattleManager__CancelEffectsFromDisappearingUnit.c`：
        //      对**四条队列**各调一次 `…FromQueue`，序 = `actionQueue(+0x350)` → `instantQueue(+0x358)`
        //      → `interruptQueue(+0x368)` → `delayedInstantQueue(+0x360)`
        //      （= 我们的 `Player(0)` → `Instant(1)` → `Interrupt(2)` → `DelayedInstant(3)`，
        //       正好等于 `ActionQueue._lanes[]` 的声明序）；
        //      ⚠️ ILSpy 那份把第三个实参写成 `shouldIgnoreDestroy2 = default`（**反编译 bug**，
        //      会把 `shouldIgnoreDestroy` 恒读成 false）—— `.c` 四个调用都传的 `param_4` ⇒ **按 `.c`**。
        //    · `decomp_full/BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c`：
        //      倒序遍历**一条**泳道，命中条件与豁免见方法 doc。
        //
        //  🔴 **与 `CarrierReplacedByRemnant` 的区别**（⛔ 别把那个改改就用）：
        //    · 那个 = **出队时**复核「载体被换成了残骸 / 被 transform」，只在泵点跑，判的是状态；
        //    · 这两个 = **入队之后（动作发生的当下）立刻**按「载体是哪张卡」把已有条目**删掉**，
        //      判的是**身份**（`casterCard == disappearingUnit`）。语义不同、时机不同。
        // ==========================================================================================

        /// <summary>
        /// 原版 `BattleManager.CancelEffectsFromDisappearingUnit(CardScript disappearingUnit,
        /// CardScript targetCard = null, bool shouldIgnoreDestroy = false)` 的等价物 ——
        /// **对四条泳道各跑一次** <see cref="CancelEffectsFromDisappearingUnitFromQueue"/>。
        ///
        /// <para>**调用点（原版四个，本批一个都没接 —— 待接点见下）**：
        /// `RemoveSelfBuffsFromQueues`（`CancelEffectsFromDisappearingUnit(bm, card, card, true)`）·
        /// `ResolveAction` 的 jam 支（`(bm, card, null, false)`）·
        /// `ResolveJamUnit`（`(bm, card, null, false)` —— **就是账 L832 那一格**）·
        /// `ResolveTransformUnit`（`(bm, card, card, true)`）。</para>
        ///
        /// <para>🔴 **待接点（本批白名单只有 `ActionQueue.cs` / `ActionPump.cs`）**：
        /// 账 L832 的落点是 `RuleEngine/Core/EffectResolver.cs` 的 `ResolveJamUnit`（属 E3），
        /// 正确插入位置 = 它的 **⑤ 广播之后、⑥ 光环窄口重算之前**
        /// （原版 `BattleManager__ResolveJamUnit.c:263` 就在 `BroadcastUnitJammed` 与
        /// `UpdateCardEffects` 之间；`EffectResolver.cs` 里那一段的 doc 已经把顺序写死）。
        /// 接的时候传 `ctx, jammed, targetCard: null, shouldIgnoreDestroy: false`（= 原版实参）。</para>
        /// </summary>
        /// <returns>四条道一共撤掉几条。</returns>
        public static int CancelEffectsFromDisappearingUnit(BattleContext ctx, UnitState disappearingUnit,
                                                            UnitState targetCard = null,
                                                            bool shouldIgnoreDestroy = false)
        {
            if (ctx == null || disappearingUnit == null) return 0;
            int n = 0;
            // 原版调用序 = `+0x350` → `+0x358` → `+0x368` → `+0x360`
            //          = Player(0) → Instant(1) → Interrupt(2) → DelayedInstant(3)
            //          = `ActionQueue._lanes[]` 的声明序（⛔ 不是 `DequeueOrder` 的消费序）。
            n += CancelEffectsFromDisappearingUnitFromQueue(ctx, disappearingUnit, targetCard,
                                                            ActionPriority.Player, shouldIgnoreDestroy);
            n += CancelEffectsFromDisappearingUnitFromQueue(ctx, disappearingUnit, targetCard,
                                                            ActionPriority.Instant, shouldIgnoreDestroy);
            n += CancelEffectsFromDisappearingUnitFromQueue(ctx, disappearingUnit, targetCard,
                                                            ActionPriority.Interrupt, shouldIgnoreDestroy);
            n += CancelEffectsFromDisappearingUnitFromQueue(ctx, disappearingUnit, targetCard,
                                                            ActionPriority.DelayedInstant, shouldIgnoreDestroy);
            return n;
        }

        /// <summary>
        /// 原版 `BattleManager.CancelEffectsFromDisappearingUnitFromQueue(CardScript jammedUnit,
        /// CardScript targetCard, List&lt;BattleAction&gt; queue, bool shouldIgnoreDestroy = false)`
        /// 的等价物 —— **倒序**扫一条泳道，把命中的条目逐条交给
        /// <see cref="ActionQueue.RemoveQueuedActionAt"/>（= 原版「扫 + 逐条调
        /// `CancelBattleActionFromQueue`」的形状）。
        ///
        /// <para>**命中条件**（`.c` 逐句）：
        /// ① `queue[i].casterCard` 非空 **且** `== jammedUnit`；—— 🔴 我们这一格 = <see cref="QueuedAction.Actor"/>
        ///    （原版 `casterCard(+0xD8)`；⛔ **不是** `actingCard(+0x08)`，那两格在原版是两个字段）。
        ///    判法见 `Core/ActionPump.cs` 类尾「五、②」那张偏移表 + `BattleAction.cs` 字段声明序。
        /// ② `actionType != 1 (attack)`（attack 不撤 —— 原版那个 `while` 条件的第二支）；
        /// ③ `targetCard == null` **或** `queue[i].targetCard == targetCard`
        ///    （原版 `+0x10`；我们 = <see cref="QueuedAction.Target"/>，今天多半是 `null` —— 见它的 doc）。</para>
        ///
        /// <para>**命中之后的三个「直接结束整条泳道」的豁免**（`.c` 里三处 `return`）：
        /// ⓐ `actionType == 0x2d (45 destroyUnit)` **且** `deathType == 0x28 (40 poison)`；
        /// ⓑ `actionType == 0x1a (26 recallToHand)`；
        /// ⓒ `shouldIgnoreDestroy` 为真 **且** `actionType == 0x2d (45)`。
        /// ⚠️ ⓐⓑⓒ 原版是**结束整条泳道的清扫**（不是跳过这一条）—— 照抄；工具侧若发现语义不合，
        /// 回 `.c` 而不是就地改（铁律 11 例外①之外一律先记账）。</para>
        ///
        /// <para>**撤之前会补副作用**（`attack` 的 `CancelAttack` / `createHandCard` 的 `SetActive(false)`）——
        /// 由 `ActionQueue.RemoveQueuedActionAt` 统一做，⛔ 别在这里另写一份。</para>
        /// </summary>
        /// <returns>这条泳道撤掉几条。</returns>
        public static int CancelEffectsFromDisappearingUnitFromQueue(BattleContext ctx, UnitState jammedUnit,
                                                                     UnitState targetCard, ActionPriority lane,
                                                                     bool shouldIgnoreDestroy = false)
        {
            if (ctx == null || jammedUnit == null) return 0;
            var q = ctx.ActionQueue;
            if (q == null) return 0;
            int n = 0;
            // 倒序（原版 `iVar1 = count - 1 … 0`）：倒着删时**下标小于 i 的条目不会被挪动**，
            // 所以不必在删除后调整 i（顺带与原版逐字同一遍历方向）。
            for (int i = q.CountOf(lane) - 1; i >= 0; i--)
            {
                var a = q.At(lane, i);
                if (a == null) continue;                    // 并发改动兜底（理论上不会）
                if (a.Actor == null) continue;               // ① 前半：`casterCard` 非空
                if (!ReferenceEquals(a.Actor, jammedUnit)) continue;   // ① 后半：是消失的那张
                if (a.ActionType == 1) continue;             // ② attack 不撤
                if (targetCard != null
                    && (a.Target == null || !ReferenceEquals(a.Target, targetCard))) continue;   // ③

                // ── 命中：三种豁免都**结束整条泳道**（原版三处 `return`）──────────────
                if (a.ActionType == 0x2d && a.DeathType == 0x28) return n;             // 45 destroyUnit + 40 poison
                if (a.ActionType == 0x1a) return n;                                    // 26 recallToHand
                if (shouldIgnoreDestroy && a.ActionType == 0x2d) return n;             // 45 destroyUnit（豁免档）

                UnityEngine.Debug.LogWarning(
                    "[ActionQueue] Removing action from queue from card " + (jammedUnit.Name ?? "?")
                    + ", " + ((int)a.ActionType) + "（原版 `BattleManager.CancelEffectsFromDisappearingUnitFromQueue.c` "
                    + "同一句 `CustomDebug.LogWarning(\"Removing action from queue from card \" + cardName + \", \" + …)`；"
                    + "原版打印的是 `BattleActionType` 名，我们打的是原版号 —— 我们还没建那个枚举）");
                if (q.RemoveQueuedActionAt(lane, i)) n++;
            }
            return n;
        }
    }
}
