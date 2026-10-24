// RuleEngineTest_Priority.cs — 引擎线 `T03·c` 批 B（第 1 步）：**四道动作队列（`ActionQueue`）** 的断言
// （2026-10-21 第六十一会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_Order` / `_BoardModel` / `_S14` / `_S23_*` / `_Diag` 都在），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(...)`**。
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1021/W61_引擎线_T03c批B判据.md`：
//   · §2.1 枚举 `ActionPriority { Player = 0, Instant = 1, Interrupt = 2, DelayedInstant = 3 }`
//     （`ActionPriority.cs:1-7`；默认 1 = Instant，`CardAbility__ctor.c:5`）。
//   · §2.2 分道（`AddAutoActionToQueue.c`）：0/1/2/3 → 四条独立队列；非法值 `LogError` **丢弃**。
//   · §2.3 消费序（`Update.c:1284-1299` / `NextActionInQueue.c:11-28`）：
//     **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**；同道 FIFO（`List.Add` +
//     `get_Item(0)` + `RemoveAt(0)`）。
//   · §5 本文件覆盖的断言编号：**T1 / T2 / T3 / T8 / T9 / T13**
//     （T4 不抢占、T5–T7 与 PlayOrder/默认值、T10–T12 死亡链与回合末 —— 要等队列接线，另批）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 **期望值一律写死字面量**（`0/1/2/3`、`"2,1,3,0"`）—— ⛔ 不从 `ActionQueue` 的实现常量反算
//    （那会自证：实现改错、期望跟着错，永远绿）。
//
// ⚠️ **2026-10-21 就地订正**：本文件头原来写着「本批**不接线**（`RuleCore` / `EffectResolver`
//    仍同步递归）⇒ 这里只测**原语**」—— 那句**自接线批起已不成立**。现在：T1~T13 测原语，
//    T4/T20~T22 测泵点，**T23~T29 从公开入口 / 真卡走一遍**（接线层的回归保护，见那一组的分节注释）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>
    /// 本批的**唯一入口**（`Run()` 里一行 `Step(TestW61ActionQueue)`）。
    /// 六个子用例是 `W61…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。
    /// </summary>
    public static void TestW61ActionQueue()
    {
        Section("🆕 `W61` · `T03·c` 批 B：四道动作队列（`ActionQueue` 原语）");
        W61T1EnumValues();
        W61T2LaneOrder();
        W61T3SameLaneFifo();
        W61T8InterruptJumpsQueue();
        W61T9DelayedInstantLast();
        W61T13InvalidPriority();
        // 🆕 2026-10-21（**接线批**）：下面五条测**泵点 / 动作外壳 / 道与入队序的关系** ——
        //   T1~T13 只测 `ActionQueue` 原语（那时还没有泵点，T4 测不了）。
        W61T4NoPreempt();
        W61T20EnqueueDoesNotShortCircuit();
        W61T21PumpOrderAndRequeue();
        W61T22ScopeDrains();
        W61T5PriorityBeatsEnqueueOrder();
        // 🆕 2026-10-21（接线第一步）：数据层那一跳（`cards_engine.json` v13 `triggerPriority`
        //   → `CardDatabase.Parse` → `CardDef._triggerPriority`）—— 上面六条测的是原语，这条测**接线**。
        W61T14DataTriggerPriority();
        // ==================================================================
        //  🆕 2026-10-21（审查 `W61b` 第 3 / 7 / 8 条 · **接线层**的回归保护）
        //
        //  🔴 为什么单开这一组：上面 T1~T22 全部只作用于 `EnqueueAbility` / `PumpActionQueues` /
        //     `ActionQueue` **本身** —— 把 `FireTriggerAt` / 三跳广播 / `ResolveAtTurn` / `ResolveDeploy`
        //     全部改回**同步递归**（即 `T03·c` 之前的形态），那 19 条**依然全绿**。
        //     ⇒ 下面五条**从公开入口或真卡走一遍**，观测点 = 「动作体里 / 边界之前」那一刻状态。
        // ==================================================================
        W61T23BroadcastQueuesUntilBoundary();
        W61T24AtTurnEntryNeedsPump();
        W61T25DeployEntryNeedsPump();
        W61T29FireTriggerQueuesUntilBoundary();
        W61T26PumpPointCount();
        W61T27ActorLeftPlayCancels();
        W61T28MissingPriorityDropped();
        // 🆕 2026-10-23（`T03·c` 残余 · 审查 §三·4）：**手牌跳 / 常驻跳**各自的接线断言
        W61T30HandListenerQueuesUntilBoundary();
        W61T31PersistentQueuesUntilBoundary();
        // 🆕 2026-10-24（引擎线 `A12` 第 4 件）：`BroadcastHandTrapWhen`（**非单位卡**那一跳）
        W61T32HandTrapQueuesUntilBoundary();
    }

    // ==================================================================
    //  🧰 本组夹具（`W61` 打头 = 辅助方法，不会被 `RunCoverageCompiledCases` 当成用例）
    // ==================================================================

    /// <summary>按**卡名**从真卡池取一张（取不到返回 null，调用方自己 `CheckTrue` 出声）。</summary>
    static CardDef W61PoolCard(string name)
    {
        foreach (var c in CardDatabase.Load())
            if (c != null && c.Name == name) return c;
        return null;
    }

    /// <summary>手牌里那张卡的下标（-1 = 不在手里）。</summary>
    static int W61HandIdx(BattleContext c, int p, CardDef card)
    {
        for (int i = 0; i < c.Players[p].Hand.Count; i++)
            if (c.Players[p].Hand[i].Card == card) return i;
        return -1;
    }

    // ==================================================================
    //  T23 · **三跳广播 → 棋盘一跳**：监听器的结算必须等动作边界
    // ==================================================================
    /// <summary>
    /// 真卡：`Ork Nob`（`When you play a troop, give it Flank`）。
    /// 场景 = 外面**自己开一层 `BeginAction`**（于是 `PlayCard` 自己的外壳变成内层、**不泵队**）：
    ///   ① 在动作体里打出 `Assault Intercessor` ⇒ 广播入队、监听器**还没结算**；
    ///   ② 动作体里断言「新兵**没有** Flank」＋「队列里有一条」；
    ///   ③ 外壳退出（= 动作边界）⇒ 泵点结算 ⇒ 新兵**有了** Flank。
    /// 🧨 把 `BroadcastWhen` 棋盘那一跳改回**同步 `ResolveOps`** ⇒ ① 之后 Flank 已经加上 ⇒ 第 ② 条红。
    ///    （这正是 `T03·c` 之前的形态 —— 本批之前没有任何一条断言能抓住它。）
    /// 🧪 夹具自检：`Assault Intercessor` 自己**没有** Flank（否则「有 Flank」证明不了监听器跑过）。
    /// </summary>
    static void W61T23BroadcastQueuesUntilBoundary()
    {
        var nob = W61PoolCard("Ork Nob");
        var grunt = W61PoolCard("Assault Intercessor");
        CheckTrue(nob != null && grunt != null, "T23 夹具：卡池里拿得到 `Ork Nob` / `Assault Intercessor`");
        if (nob == null || grunt == null) return;

        var ctx = Battle(new[] { nob, grunt }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        // 注意：`Flank` 没有 `KeywordTable` 常量（不在那张表里，见 `CardDef` 的 54 词清单）⇒ 用字面量。
        CheckTrue(!grunt.Has("flank"), "🧪 T23 夹具：`Assault Intercessor` 自己**没有** flank 关键词");

        RuleCore.PlayCard(ctx, 0, W61HandIdx(ctx, 0, nob), 2, out int slotNob);
        CheckTrue(slotNob >= 0 && ctx.Players[0].Board[slotNob] != null, "T23 前提：`Ork Nob` 已上场（监听者就位）");
        Check(ctx.ActionQueue.Count, 0, "T23 前提：打出 Nob 之后队列已被泵空（他的监听器不为自己响）");

        UnitState gruntU = null;
        using (RuleCore.BeginAction(ctx))                       // ← 外层外壳：PlayCard 变成内层（不泵队）
        {
            RuleCore.PlayCard(ctx, 0, W61HandIdx(ctx, 0, grunt), 3, out int slotG);
            gruntU = slotG >= 0 ? ctx.Players[0].Board[slotG] : null;
            CheckTrue(gruntU != null, "T23 中间态：`Assault Intercessor` 已上场");
            if (gruntU == null) return;
            CheckTrue(ctx.ActionQueue.Count > 0, "★ T23 动作体内：监听器那条**在队列里等着**（⛔ 不是当场结算）");
            Check(gruntU.Has("flank"), false,
                  "★ T23 动作体内：监听器**还没结算** ⇒ 新兵此刻**没有** flank"
                + " —— 🧨 把 `BroadcastWhen` 棋盘那一跳改回同步 `ResolveOps` ⇒ 本条红");
            Check(ctx.ActionQueue.Resolving, true, "🧪 T23 动作体内：`Resolving` 为真（所以泵队空转）");
        }
        CheckTrue(gruntU.Has("flank"), "★ T23 边界之后：监听器结算完毕 ⇒ 新兵**拿到了** flank"
                                     + " —— 🧨 `ActionScope.Dispose` 不泵队 ⇒ 本条红");
        Check(ctx.ActionQueue.Count, 0, "T23 边界之后：队列取空");
    }

    // ==================================================================
    //  T24 · **`ResolveAtTurn` 公开入口**：只入队，没人泵就不结算（审查第 1 条）
    // ==================================================================
    /// <summary>
    /// 真卡：`TAU6 Gun Drone`（`At the start of your turn, deal 2 damage to a random enemy`）。
    /// 敌方棋盘空 ⇒ 「随机敌人」只能是敌方督军。
    ///   ① 裸调 `RuleEngine.ResolveAtTurn(ctx, "turn_start")`（**不包 `BeginAction`**）
    ///      ⇒ 敌方督军血量**一点没掉**、队列里有一条；
    ///   ② `PumpActionQueues` ⇒ 掉 2。
    /// 🧨 `ResolveAtTurn` 改回同步 `ResolveOps` ⇒ ① 就掉血 ⇒ 红。
    /// 🧨 有人给 `EnqueueAbility` 加「队列空就直接跑」的短路 ⇒ ① 就掉血 ⇒ 红。
    /// ⚠️ 这一条钉的正是 `CardPresentation/Editor/BattleScene.cs` 那两处**裸调**（审查第 1 条：
    ///    `Gun Drone` 的 2 伤实际上从来没落地）；那边已改成包 `BeginAction`（并有自己的落地断言）。
    /// </summary>
    static void W61T24AtTurnEntryNeedsPump()
    {
        var drone = W61PoolCard("Gun Drone");
        CheckTrue(drone != null, "T24 夹具：卡池里拿得到 `Gun Drone`");
        if (drone == null) return;

        var ctx = Battle(new[] { drone }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        RuleCore.PlayCard(ctx, 0, W61HandIdx(ctx, 0, drone), 2, out int slot);
        CheckTrue(slot >= 0 && ctx.Players[0].Board[slot] != null, "T24 前提：`Gun Drone` 已上场");
        Check(ctx.ActionQueue.Count, 0, "T24 前提：打出之后队列已空");
        var foe = ctx.Players[1].Warlord;
        CheckTrue(foe != null, "T24 前提：敌方督军在");
        if (foe == null) return;
        int hp0 = foe.Health;

        RuleCore.ResolveAtTurn(ctx, "turn_start");              // ← 裸调：没有外壳、没有泵点
        Check(foe.Health, hp0, "★ T24 裸调 `ResolveAtTurn` ⇒ 敌方督军**一点血都没掉**（触发只入队）"
                            + " —— 🧨 改回同步结算 ⇒ 本条红");
        CheckTrue(ctx.ActionQueue.Count > 0, "★ T24 那条 at-turn 作业**在队列里**（⛔ 不是丢了）");

        RuleCore.PumpActionQueues(ctx);
        Check(foe.Health, hp0 - 2, "★ T24 泵点之后：`Gun Drone` 的 2 伤**落地**"
                                 + " —— 🧨 入队之后没有任何泵点 ⇒ 本条红");
        Check(ctx.ActionQueue.Count, 0, "T24 泵完 ⇒ 队列空");
    }

    // ==================================================================
    //  T25 · **`ResolveDeploy` 公开入口**：只入队，没人泵就不结算
    // ==================================================================
    /// <summary>
    /// 夹具 = **手搓一条 `Trigger == "deploy"` 的常驻效果**（`PersistentEffect` 是本工程自设计的容器，
    /// 见 `BattleContext.PersistentEffects` 的注释）⇒ 它只经过 `ResolveDeploy` 那一条真实路径。
    /// 正文 = `Deal 2 damage to the enemy Warlord`。
    ///   ① 裸调 `RuleCore.ResolveDeploy` ⇒ 督军血量不变、队列里有一条；
    ///   ② 泵点 ⇒ 掉 2。
    /// 🧨 `ResolveDeploy` 里那句 `EnqueueAbility` 改回同步 `ResolveOne` ⇒ ① 就掉血 ⇒ 红。
    /// </summary>
    static void W61T25DeployEntryNeedsPump()
    {
        var peon = Unit("DeployPeon", 1, 1, 2);
        var ctx = Battle(new[] { peon }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        RuleCore.PlayCard(ctx, 0, W61HandIdx(ctx, 0, peon), 2, out int slot);
        var dep = slot >= 0 ? ctx.Players[0].Board[slot] : null;
        CheckTrue(dep != null, "T25 前提：`DeployPeon` 已上场");
        if (dep == null) return;

        var ops = EffectText.Parse("Deal 2 damage to the enemy Warlord", out _, out _);
        CheckTrue(ops != null && ops.Count > 0, "T25 夹具：`Deal 2 damage to the enemy Warlord` 解析得出来");
        if (ops == null || ops.Count == 0) return;

        ctx.PersistentEffects.Add(new PersistentEffect
        {
            Owner = 0,
            Source = Tactic("DeployWatcher", 1, ""),
            Trigger = "deploy",
            Ops = ops,
            Body = "Deal 2 damage to the enemy Warlord",
        });

        var foe = ctx.Players[1].Warlord;
        CheckTrue(foe != null, "T25 前提：敌方督军在");
        if (foe == null) return;
        int hp0 = foe.Health;

        RuleCore.ResolveDeploy(ctx, 0, dep);                    // ← 裸调：没有外壳、没有泵点
        Check(foe.Health, hp0, "★ T25 裸调 `ResolveDeploy` ⇒ 部署触发**没结算**（只入队）"
                            + " —— 🧨 改回同步 `ResolveOne` ⇒ 本条红");
        CheckTrue(ctx.ActionQueue.Count > 0, "★ T25 那条部署触发**在队列里**");

        RuleCore.PumpActionQueues(ctx);
        Check(foe.Health, hp0 - 2, "★ T25 泵点之后：部署触发的 2 伤**落地**");
        Check(ctx.ActionQueue.Count, 0, "T25 泵完 ⇒ 队列空");
    }

    // ==================================================================
    //  T29 · **`FireTriggerAt` 那一跳**（Rally 走的就是它）：只入队
    // ==================================================================
    /// <summary>
    /// 夹具 = 一张**合成卡**：`Rally: Deal 2 damage to the enemy Warlord`（从手牌打出 ⇒
    /// `RuleCore.PlayCardCore` 调 `FireTriggerOnBoard(unit, Rally)` → `FireTriggerAt`）。
    /// 外面包一层 `BeginAction`（于是 `PlayCard` 自己的外壳不泵队）：
    ///   ① 动作体里：敌方督军血量**没掉**、队列里有一条（`·rally`）；
    ///   ② 边界之后：掉 2。
    /// 🧨 `FireTriggerAt` 改回同步 `ResolveOps` / `ResolveEffect` ⇒ ① 就掉血 ⇒ 红。
    /// </summary>
    static void W61T29FireTriggerQueuesUntilBoundary()
    {
        var rallier = new CardDef("ZZW61Rally", "ZZW61Rally", "unit",
                                  "Rally: Deal 2 damage to the enemy Warlord", "common", "Test",
                                  1, 1, 1, 0, null);
        CheckTrue(rallier.TriggerOps(KeywordTable.Rally) != null,          // 语法不成立这条就早红
                  "T29 夹具：`Rally: …` 解析出了触发 op");
        var ctx = Battle(new[] { rallier }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        var foe = ctx.Players[1].Warlord;
        CheckTrue(foe != null, "T29 前提：敌方督军在");
        if (foe == null) return;
        int hp0 = foe.Health;

        using (RuleCore.BeginAction(ctx))
        {
            RuleCore.PlayCard(ctx, 0, W61HandIdx(ctx, 0, rallier), 2, out int slot);
            CheckTrue(slot >= 0 && ctx.Players[0].Board[slot] != null, "T29 中间态：本单位已上场");
            CheckTrue(ctx.ActionQueue.Count > 0, "★ T29 动作体内：`Rally` 那条**在队列里**（⛔ 不是当场结算）");
            Check(foe.Health, hp0, "★ T29 动作体内：`Rally` 的 2 伤**还没落地**"
                                + " —— 🧨 把 `FireTriggerAt` 改回同步结算 ⇒ 本条红");
        }
        Check(foe.Health, hp0 - 2, "★ T29 边界之后：`Rally` 的 2 伤**落地**");
        Check(ctx.ActionQueue.Count, 0, "T29 边界之后：队列取空");
    }

    // ==================================================================
    //  T26 · **泵点集合 = 19 个 `BeginAction` 调用点**（审查第 8 条）
    // ==================================================================
    /// <summary>
    /// 「漏一个泵点 = 静默丢一条动作」（审查第 1 / 第 5 条就是这么来的）⇒ 这个数必须钉住。
    /// 判据 = **源码里 `using (BeginAction(ctx))` / `using (RuleCore.BeginAction(ctx))` 出现的条数**
    /// （**整行正则**：行内**任意位置**都算；注释行以 `//` / `*` 开头 ⇒ 排除）。
    ///
    /// 🔴 **2026-10-22（`W63`）就地订正（铁律 5）：旧版是「只看行首 + 只认 `RuleCore.` 前缀」，两处都漏** ——
    ///   ① `RuleCore.cs:3307` 的 `PlayCard` 写成 `try { using (BeginAction(ctx)) return … }`
    ///      ⇒ `using` **不在行首**，旧扫描器漏计；
    ///   ② `EffectResolver.cs:1676` 也是同一个 `try { … }` 同行写法；另外两处（`:7164` / `:8775`）
    ///      **连 `RuleCore.` 前缀都没有**（同一个类里直接调 `BeginAction`）
    ///      ⇒ 旧前缀在整份 `EffectResolver.cs` 里命中 **0**（`nEff = 0` 就是这么来的）。
    ///   ⇒ 旧期望「13 + 1 = 14」是**扫描器给出的假数**；现读真值 = **`RuleCore.cs` 16 + `EffectResolver.cs` 3 = 19**
    ///      （2026-10-24 现读：`A1` 第 4 件新增 `BeginTurnForReplayExpansion` 的泵点 `RuleCore.cs:1542`）。
    ///   ⚠️ **没有丢泵点**：W62 列的 13 个全在，+ R1 新增的
    ///      `Hurt`（泵点 `RuleCore.cs:8040`）/ `TryCancelAttack`（泵点 `RuleCore.cs:7139`）= 15；
    ///      再加 2026-10-24（引擎线 `A1` 第 4 件）新增的 `BeginTurnForReplayExpansion`（`RuleCore.cs:1542`）= 16。
    /// 🧨 任何一处 `using (BeginAction…)` 被删 / 被注释掉 ⇒ 数字变小 ⇒ 红（= 那个顶层入口从此不泵队）。
    /// 🧨 **新增**顶层入口时本条也会红 —— 那是**故意的**：去确认新入口真的包了 `BeginAction`，
    ///    再把这里的期望值 +1（⛔ 别改成「≥19」，那会让「漏一个」和「加一个」分不开）。
    /// </summary>
    static void W61T26PumpPointCount()
    {
        string assets = UnityEngine.Application.dataPath;      // …/MyGame/Assets
        int nRule = W61CountPumpPoints(System.IO.Path.Combine(assets, "RuleEngine/Core/RuleCore.cs"));
        int nEff = W61CountPumpPoints(System.IO.Path.Combine(assets, "RuleEngine/Core/EffectResolver.cs"));
        // 夹具只判「文件读到了」。⛔ **别在这里要求 `> 0`** —— 那会让「真丢了一个泵点」同时红两条，
        //    分不清是**夹具前提塌了**还是**实现缺陷**（`W63` 之前就是这种双重红：#10 只是 #12 的级联）。
        CheckTrue(nRule >= 0 && nEff >= 0,
                  "T26 夹具：两份源码都读到了（**-1 = 文件不存在**；路径见上面两行）");
        Check(nRule, 16, "★ T26 `RuleCore.cs` 的 `using (BeginAction(ctx))` = 16（2026-10-24 现读：`A1` 新增 `BeginTurnForReplayExpansion`）");
        Check(nEff, 3, "★ T26 `EffectResolver.cs` 的 `using (BeginAction(ctx))` = 3"
                     + "（`PlayTacticCore` :2042 · `BroadcastWhen` 壳 :8341 · `GrantDarkPact` 壳 :10185）");
        Check(nRule + nEff, 19, "★ T26 **泵点总数 = 19**（漏一个 ⇒ 那个顶层入口的触发永远不结算）");
    }

    /// <summary>`T26` 的行扫描：**整行正则** —— 行内**任意位置**出现
    /// `using (BeginAction(ctx))` 或 `using (RuleCore.BeginAction(ctx))` 都算一条。
    /// ⚠️ 两条都必须认：`EffectResolver.cs` 有两处（`:7164` / `:8775`）**没有 `RuleCore.`**；
    ///    而且必须看**行内**（`RuleCore.cs:3307` 与 `EffectResolver.cs:1676` 都被 `try { … }` 包在同一行）。
    /// 排除 = **注释行**（trim 后以 `//` 或 `*` 开头；行尾注释不影响 —— 只要代码那半句还在，
    /// 那本来就是活的泵点）。
    /// ⚠️ **如实标着的残余**：`/* … */` 块注释里提到它时本扫描器仍会数进来（这两个文件里今天没有）。
    /// 🧨 **改坏法**：只认行首（`StartsWith`）⇒ `EffectResolver.cs` 只数到 **2**（`:1676` 的 `try {` 挡掉）
    ///    ⇒ 第二条红；去掉正则里的 `(?:RuleCore\.)?` ⇒ 还是 **2**（`:7164`/`:8775` 没有前缀）⇒ 同样红
    ///    —— **两半都是必需的**。
    /// </summary>
    static int W61CountPumpPoints(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Check(path, "<存在>", "T26 夹具：找不到源码文件 —— 下面的计数断言会一起红");
            return -1;
        }
        var re = new System.Text.RegularExpressions.Regex(
            @"using\s*\(\s*(?:RuleCore\.)?BeginAction\s*\(\s*ctx\s*\)\s*\)",
            System.Text.RegularExpressions.RegexOptions.Compiled);
        int n = 0;
        foreach (string line in System.IO.File.ReadAllLines(path))
        {
            string t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;   // 行注释 / doc 注释
            if (t.StartsWith("*", StringComparison.Ordinal)) continue;    // 块注释续行
            n += re.Matches(line).Count;
        }
        return n;
    }

    // ==================================================================
    //  T27 · **出队复核「施放者已离场」**（审查第 3 条）
    // ==================================================================
    /// <summary>
    /// 原版判据 = `CardAbility__IsRightContext.c:20-30`（`Play(0)` 要求 `IsInPlay`）⇒
    /// 结算那一刻施放者已经离场（死亡 / 回手 / 移出棋盘）⇒ 这条 ability **不结算**。
    /// 三步（**后两步是灭自证**）：
    ///   ① 施放者**在场上** ⇒ 照常结算（防「泵点恒取消」的假绿）；
    ///   ② 把它从棋盘上摘掉（= 离场）⇒ 同一条动作**被取消**（`ran` 不涨）、并从队列里出掉（⛔ 不是卡住）；
    ///   ③ 同一条动作但 `requireActorInPlay: false`（= 死亡触发那一族 `InPlayOrDying`）⇒ **照跑**
    ///      （防「只要 Actor 非 null 就一律取消」—— 那会把 `Backlash`/`Unstable`/`minionDeath` 整条链掐死）。
    /// 🧨 删掉泵点里那个 `RequireActorInPlay` 分支 ⇒ ② 的 `ran` 变 1 ⇒ 红。
    /// 🧨 把那个分支写成「有 Actor 就取消」⇒ ③ 的 `ran` 不变 ⇒ 红。
    /// </summary>
    static void W61T27ActorLeftPlayCancels()
    {
        var ghost = Unit("Ghost", 1, 1, 1);
        var ctx = Battle(new[] { ghost }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        RuleCore.PlayCard(ctx, 0, W61HandIdx(ctx, 0, ghost), 2, out int slot);
        var u = slot >= 0 ? ctx.Players[0].Board[slot] : null;
        CheckTrue(u != null, "T27 前提：`Ghost` 已上场");
        if (u == null) return;

        int ran = 0;
        // ① 对照：还在场上 ⇒ 照常结算
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27-在場", () => ran++,
                                actor: u, requireActorInPlay: true);
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 1, "🧪 T27 对照：施放者**还在场上** ⇒ 这条照常结算（防「泵点恒取消」假绿）");

        // ② 摘掉它（= 离场：死亡 / 回手 / 移出棋盘，三种都是「不在任何一方棋盘上」）
        ctx.Players[0].Board[slot] = null;
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27-已離場", () => ran++,
                                actor: u, requireActorInPlay: true);
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 1, "★ T27 施放者已离场 ⇒ 这条**被取消**（⛔ 没结算）"
                    + " —— 🧨 删掉泵点里 `RequireActorInPlay` 那个分支 ⇒ 本条红");
        Check(ctx.ActionQueue.Count, 0, "★ T27 被取消的那条**已经出队**（⛔ 不是卡在队列里）");

        // ③ 死亡触发那一族（原版 `InPlayOrDying(40)`）：施放者已离场也**照跑**
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27-死亡族", () => ran++,
                                actor: u, requireActorInPlay: false);
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 2, "★ T27 `requireActorInPlay: false`（= 原版 `InPlayOrDying`）⇒ 施放者离场**也照跑**"
                    + " —— 🧨 写成「有 Actor 就取消」⇒ `Backlash`/`Unstable`/`minionDeath` 整条链断 ⇒ 本条红");
        ctx.Players[0].Board[slot] = u;                        // 还原（不影响别的用例，纯礼貌）

        // ==================================================================
        //  🔴🆕 **2026-10-23（`W63` 收口）：`abilityContext` 的闸在【入队那一刻】**
        // ==================================================================
        //  判据（第一权威 = 全量反编译）：原版 `CardAbility.CanTriggerAbility` 由
        //  `RawCardScript.TriggerAbilities` 在**广播那一刻**同步调
        //  （`RawCardScript__TriggerAbilities.c:108,207`），`IsRightContext` 全库只有这一个调用者
        //  ⇒ **原版闸在入队前**；入了队的动作**死亡不会取消**它
        //  （`BattleManager.CancelEffectsFromDisappearingUnit` 只有 removeEffect / jam / transform /
        //  变残骸四个调用点，且**保留** `destroyUnit` / `recallToHand`）。
        //  ⇒ 我们的闸从泵点（出队）搬到 `EnqueueAbility`（入队），泵点只留「变残骸 / transform」。
        //  下面 ④～⑧ 逐档钉住这条口径。
        // ==================================================================
        //  ④ 载体活着在场 + `Play(0)` ⇒ 照常触发
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27④-在场Play", () => ran++,
                                actor: u, abilityContext: CardDef.AbilityContextPlay);
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 3, "★ T27④ 载体**活着在场** + `abilityContext=Play(0)` ⇒ 照常入队并结算"
                    + "（🧨 入队闸写成「恒 false」⇒ 本条红）");

        //  🔴 **如实标着（口径不完全等价，本笔不改实现 —— 2026-10-23 · 8 红修复批顺手发现）**：
        //    `Play(0)` 的闸我们写的是 `cell.IsAlive`（= `Health > 0`）+ `!IsPendingDeath`
        //    （`Core/ActionPump.cs:551-553`）；原版 `CardAbility__IsRightContext.c:18-32` 是
        //    `IsInPlay(card) && !EnoughPendingDamageToDie`，而 `CardScript__IsInPlay.c:7-13`
        //    只看**卡状态四档**、**根本不看血量**。
        //    ⇒ 对「`Health ≤ 0` 且 `Survivor > 0`」这一格：原版**放行**、我们**拦下**。
        //    现读**不可达**（幸存者那一支在 `RuleCore.CleanupDeaths` 里就把血救回来了，
        //    `Core/RuleCore.cs:8320-8432`），但**口径不严格等价** —— 记在这里，
        //    ⛔ 别把它当成「已经和原版一样」。（诊断报告 §6·2，选项 ② 加固未做。）

        //  ⑤ 载体已归零（= 原版「待死 / `EnoughPendingDamageToDie`」）+ `Play(0)` ⇒ **根本不触发**
        int hp0 = u.Health;
        u.Health = 0;
        bool queued5 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27⑤-待死Play", () => ran++,
                                               actor: u, abilityContext: CardDef.AbilityContextPlay);
        RuleCore.PumpActionQueues(ctx);
        CheckTrue(!queued5, "★ T27⑤ 载体**已待死** + `Play(0)` ⇒ `EnqueueAbility` 直接返回 false（⛔ 不入队）"
                          + " —— 判据 = `IsRightContext` 的 `!EnoughPendingDamageToDie`"
                          + "（`CardAbility__IsRightContext.c:20-32`）；🧨 删掉入队闸 ⇒ 返回 true ⇒ 红");
        Check(ran, 3, "★ T27⑤ 这一条**没有结算**（`ran` 不涨）—— 🧨 闸只写日志、不 `return false` ⇒ 红");

        //  ⑥ 同一条待死载体换成 `InPlayOrDying(40)` ⇒ 照触发（状态 5 也算在场）
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27⑥-待死40", () => ran++,
                                actor: u, abilityContext: CardDef.AbilityContextInPlayOrDying);
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 4, "★ T27⑥ `InPlayOrDying(40)` 的载体**待死也照触发**"
                    + "（`CardAbility__IsRightContext.c:52-55` 状态含 5）—— 🧨 把 40 也按 `Play(0)` 判 ⇒ 红");

        //  ⑦ `InPlayOrCemetery(50)` ⇒ 待死**不算**（与 40 不等价）
        //  🔴 **2026-10-23（8 红修复批）夹具订正（δ：原来那条夹具根本没建立「待死」）**：
        //    以前这里只是 `u.Health = 0` —— 而本引擎判「待死 / 原版 state 5」用的是
        //    **`_pendingDeaths` 队列**（`BattleContext.IsPendingDeath:1565-1571`，doc 自述
        //    「口径 = 原版 `CardStateOptions.waitingToDie = 5`」），`IsRightContext` 的 50 档读的
        //    就是它（`Core/ActionPump.cs:556-558`）。**只改血量不进队列 ⇒ 载体仍算在场** ⇒ 旧夹具
        //    红的是夹具、不是实现（真对局里「站在棋盘上 + 血 ≤ 0 + 不在待死队列」这个状态产不出来：
        //    写血 ≤ 0 的引擎路径当场跟一句 `CleanupDeaths`）。
        //    ⚠️ **另起一个载体**（`u` 留给 ⑧⑨ —— 它们要的是「活着在场」的同一个单位）。
        //    ⚠️ 待死队列的清空口 `TakePendingDeaths()` 是 `internal`（`BattleContext.cs:1590`），
        //       Editor 程序集够不着 ⇒ **撤不回来**；本用例在方法末尾，`ctx` 随即丢弃、漏不到别的用例。
        //    判据 → `d:/2/tools/decomp_full/CardAbility__IsRightContext.c:66-72`（50 = `IsInCemetery || IsInPlay`）
        //         ＋ `CardScript__IsInPlay.c:7-13`（四档，**不含 5**）。
        int dslot = BoardSpec.Size - 1;
        while (dslot >= 0 && ctx.Players[0].Board[dslot] != null) dslot--;   // 本夹具只有 `Ghost`，末格必空；被占就退到最近空格
        CheckTrue(dslot >= 0, "T27⑦ 前提：0 号方还有空格摆「待死」载体（否则本条与 ⑧⑨ 的基线会一起偏）");
        if (dslot >= 0)
        {
            var dying = Place(ctx, 0, dslot, Unit("Dying", 1, 1, 1));
            dying.Health = 0;                       // 语义上「血归零」
            ctx.AddPendingDeath(0, dslot, -1);      // ← 这才是原版 state 5（唯一公开入口，`BattleContext.cs:1544`）
            bool queued7 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27⑦-待死50", () => ran++,
                                                   actor: dying, abilityContext: CardDef.AbilityContextInPlayOrCemetery);
            RuleCore.PumpActionQueues(ctx);
            CheckTrue(!queued7, "★ T27⑦ `InPlayOrCemetery(50)` 的载体**待死不算在场**（`:66-72` 不含状态 5）"
                              + " —— 🧨 把 40/50 写成同一档 ⇒ 本条红");
            Check(ran, 4, "★ T27⑦ 这条同样没结算");
            ctx.Players[0].Board[dslot] = null;     // 清场（待死队列撤不回来，见上注）
        }
        u.Health = hp0;                             // ⑧⑨ 要的 `u` 是**活着在场**的（`Play(0)` 的闸看 `IsAlive`）

        //  ⑧ 入队之后、出队之前载体**死亡（离场）** ⇒ **照跑**（原版死亡不取消）
        //     ⚠️ 这一条钉的就是「把闸搬回出队」这个回归：泵点若仍按 context 复核 ⇒ `ran` 不涨 ⇒ 红。
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27⑧-入队后离场", () => ran++,
                                actor: u, abilityContext: CardDef.AbilityContextPlay);
        ctx.Players[0].Board[slot] = null;                     // 入队之后才离场（= 死了 / 回手 / 移出）
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 5, "★ T27⑧ 入队时在场、**出队前离场** ⇒ 这条**照跑**（原版 `IsRightContext` 只管入队那一刻；"
                    + "`CancelEffectsFromDisappearingUnit` 不含死亡）"
                    + " —— 🧨 把 context 复核搬回泵点 ⇒ 本条红（那正是 `W63 #2` Slay 正文丢失的成因）");

        //  ⑨ 载体**变成了残骸**（同一份 `CardInstance`、换了一个 `UnitState`）⇒ **取消**
        //     判据 = `RuleCore.CleanupDeaths` 的 `new UnitState(u.Instance, …) { IsRemnant = true }`；
        //     这是原版四个「主动取消」里我们今天唯一建模得到的一路。
        ctx.Players[0].Board[slot] = u;
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27⑨-变残骸", () => ran++,
                                actor: u, abilityContext: CardDef.AbilityContextPlay);
        ctx.Players[0].Board[slot] = new UnitState(u.Instance, false) { IsRemnant = true };
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 5, "★ T27⑨ 载体**变成残骸**（同一 `Instance`、换了 `UnitState`）⇒ 这条**被取消**"
                    + "（原版 `TransformIntoRemnant` → `CancelEffectsFromDisappearingUnit`）"
                    + " —— 🧨 连残骸那一支也删掉 ⇒ 本条红");
        ctx.Players[0].Board[slot] = null;                     // 清场（本用例结束）

        //  ⑩ 数据侧：「v14 缺行 = `Play(0)`」这一跳真的在起作用（池内结构性 + 负对照）
        var pool10 = CardDatabase.Load();
        int nHopPlay = 0, nRawUnknown = 0, nPresence = 0;
        foreach (var c in pool10)
        {
            if (c.AbilityTriggerPresence("Slay") == 1) nPresence++;
            if (RuleCore.AbilityContextOfAbilityV14(c, "Slay", "slay") == CardDef.AbilityContextPlay) nHopPlay++;
            if (RuleCore.AbilityContextOfAbility(c, "Slay", "slay") == CardDef.AbilityContextUnknown) nRawUnknown++;
        }
        // 🔴 **2026-10-24（执行代理 E8）**：「缺行 = `Play(0)`」有了**前提②**（这张卡确实有这条 trigger）
        //   ⇒ 全池**不再**人人都是 Play；正确形状 = **iff**：`presence==1` 的卡 ⇔ 读成 `Play(0)`。
        Check(nHopPlay, nPresence,
              $"★ T27⑩ 全池 `Slay(120)` 读成 **`Play(0)`** 的卡数 == 真有这条 trigger 的卡数（现读 {nPresence} 张）"
            + " —— 🧨 删掉前提② ⇒ 全池 1127 张都变 Play ⇒ 红；删掉「versioned ⇒ Play(0)」那一跳 ⇒ 0 张 ⇒ 红");
        Check(nRawUnknown, pool10.Count,
              "🧪 T27⑩ 对照：**原始**访问器对 `Slay` 一律 `-1`（证明确实是那一跳在供值，不是数据里有行）"
            + " —— 🧨 把 `AbilityContextOfAbilityV14` 改成直接转调原始访问器 ⇒ 上一条红、本条仍绿（两条一起钉死）");
        //  ⚠️ `CardDatabase.Load()` 会按真卡池重建全局卡名索引 —— 那是**更强**的还原
        //    （`T28` 用 `CreatePool.BuildNameIndex` 做的同一件事），不影响后面的用例。
    }

    // ==================================================================
    //  T28 · **`priority` 缺键 ⇒ 出声 + 丢弃**（审查第 7 条）
    // ==================================================================
    /// <summary>
    /// 引擎侧（`CardDatabase.MapTriggerPriority`）与探针侧（`工具/ruleprobe/Program.cs` 的 `nBadTp`）
    /// 必须**同口径**：`priority` 缺键 / 越界 **都出声并丢弃**。
    /// 修之前：`TriggerPriorityDto.priority` 是 `int` 默认 **0** ⇒ 缺键那条**静默落进 Player(0) 道**
    /// （最后才结算的一条道），而探针报「1 条形不全」—— 同一份数据两个读数。
    /// 本条把一段**手写 JSON**（v13，一张卡，`triggerPriority` 那条**没有 `priority` 键**）喂给
    /// `CardDatabase.Parse`，断言读回来是**默认 1**（= 那条被丢了），⛔ 不是 0。
    /// 🧨 把哨兵 `TriggerPriorityDto.MissingPriority` 改回 0 / 去掉 `MapTriggerPriority` 那道闸
    ///    ⇒ 读回 0 ⇒ 红（并会多打一条 `Debug.LogError`，那条**不是**失败，是本用例在测的「出声」）。
    /// </summary>
    static void W61T28MissingPriorityDropped()
    {
        const string json = "{\"version\":13,\"cards\":[{"
            + "\"id\":\"ZZW61\",\"name\":\"ZZW61NoPrio\",\"type\":\"unit\",\"desc\":\"\","
            + "\"rarity\":\"common\",\"faction\":\"Test\",\"cost\":1,\"attack\":1,\"health\":1,"
            + "\"ranged\":0,\"keywords\":[],"
            + "\"triggerPriority\":[{\"keyword\":\"artifice\"}]}]}";
        var pool = CardDatabase.Parse(json);
        Check(pool.Count, 1, "T28 夹具：手写 JSON 解析出 1 张卡");
        if (pool.Count != 1) return;
        var c = pool[0];
        CheckTrue(c != null && c.Id == "ZZW61", "T28 夹具：拿到那张卡（id = ZZW61）");
        if (c == null) return;
        Check(c.TriggerPriority("artifice"), CardDef.DefaultTriggerPriority,
              "★ T28 `priority` 缺键那一行**被丢弃** ⇒ 读回默认 1（⛔ 不是 0 = Player 道）"
            + " —— 🧨 去掉哨兵 / 去掉 `MapTriggerPriority` 那道闸 ⇒ 读回 0 ⇒ 红");
        // 🧪 对照：合法值必须照收（防「整列都被丢」的假绿）
        Check(c.TriggerPriority("no_such_keyword"), 1, "🧪 T28 对照：没登记的关键词 ⇒ 默认 1");

        // ⚠️ **副作用隔离**：`CardDatabase.Parse` 末尾会重建**全局卡名索引**
        //   （`CreatePool.BuildNameIndex`）⇒ 不还原的话，本次自检**后面**那些靠卡名指目标的用例
        //   会拿到「只有 1 张卡」的索引（静默变红，且根因看不出来）。用真卡池重建一次。
        CreatePool.BuildNameIndex(CardDatabase.Load());
        CheckTrue(CreatePool.MatchCardName("Ork Nob") != null,
                  "T28 收尾：全局卡名索引已用真卡池重建（`Ork Nob` 又能按卡名指到）");
    }

    // ==================================================================
    //  T1 · 枚举数值（字面量 0/1/2/3）
    // ==================================================================
    /// <summary>
    /// **枚举数值必须逐字是 0/1/2/3** —— 它是**数据里的字面量**
    /// （`cards_engine.json` 的 `triggerPriority[].priority` 与 `CardAbility.priority` 同一套）。
    /// 🧨 把 `Interrupt` 与 `DelayedInstant` 换个位置、或整体 +1，本条必红。
    /// ⛔ 期望值写死（`(int)Enum.X` 去比 `Enum.X` 那种写法**恒真**，等于没测）。
    /// </summary>
    static void W61T1EnumValues()
    {
        Check((int)ActionPriority.Player, 0, "T1 `ActionPriority.Player` 的字面量 = 0");
        Check((int)ActionPriority.Instant, 1, "T1 `ActionPriority.Instant` 的字面量 = 1（= 原版默认值）");
        Check((int)ActionPriority.Interrupt, 2, "T1 `ActionPriority.Interrupt` 的字面量 = 2");
        Check((int)ActionPriority.DelayedInstant, 3, "T1 `ActionPriority.DelayedInstant` 的字面量 = 3");
    }

    // ==================================================================
    //  T2 · 道序 = 字面量 [2,1,3,0]
    // ==================================================================
    /// <summary>
    /// 四条道**各放一条**（入队顺序故意按道号 0→1→2→3），出队必须是
    /// **Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)**。
    /// 🧨 若实现「按枚举数值从小到大排」，出队序会变成 0,1,2,3 ⇒ 本条必红
    ///    （`[2,1,3,0]` 与 `[0,1,2,3]` 只有首尾两个数巧合相同，中间两位直接不同）。
    /// </summary>
    static void W61T2LaneOrder()
    {
        var q = new ActionQueue();
        CheckTrue(q.Enqueue(new QueuedAction { Label = "p" }, ActionPriority.Player), "T2 夹具：Player 道入队成功");
        CheckTrue(q.Enqueue(new QueuedAction { Label = "i" }, ActionPriority.Instant), "T2 夹具：Instant 道入队成功");
        CheckTrue(q.Enqueue(new QueuedAction { Label = "x" }, ActionPriority.Interrupt), "T2 夹具：Interrupt 道入队成功");
        CheckTrue(q.Enqueue(new QueuedAction { Label = "d" }, ActionPriority.DelayedInstant),
                  "T2 夹具：DelayedInstant 道入队成功");
        Check(q.Count, 4, "T2 夹具：四条道各一条 ⇒ Count = 4");

        var got = new List<string>();
        for (int k = 0; k < 4; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T2 第 {k + 1} 次 `TryDequeue` 应成功（道序断言的前置）");
            if (ok) got.Add(((int)lane).ToString());
        }
        Check(string.Join(",", got.ToArray()), "2,1,3,0",
              "T2 道序 = 字面量 [2,1,3,0]（Interrupt→Instant→DelayedInstant→Player）");
        Check(q.Count, 0, "T2 取完四条 ⇒ Count = 0");
    }

    // ==================================================================
    //  T3 · 同道 FIFO ＋ 追加入**队尾**
    // ==================================================================
    /// <summary>
    /// Player 道依次放 A、B、C（中间往别的道插一条，验「插队不影响本道」），
    /// 取出来必须还是 **A → B → C**（原版 `List.Add` 入队尾 + `get_Item(0)`/`RemoveAt(0)` 出队首）。
    /// 🧨 若实现是 LIFO（`RemoveAt(Count-1)`）、或往队首插（`Insert(0, …)`）⇒ 取到 C,B,A ⇒ 必红。
    /// 🧪 夹具自检：三条各自的 `Seq` 必须是**递增**的（1,2,4 —— 中间那条 Instant 拿了 3），
    ///    否则「队尾追加」这半句没被测到（例如实现把 Seq 也重排了）。
    /// </summary>
    static void W61T3SameLaneFifo()
    {
        var q = new ActionQueue();
        var a = new QueuedAction { Label = "A" };
        var b = new QueuedAction { Label = "B" };
        var x = new QueuedAction { Label = "X" };
        var c = new QueuedAction { Label = "C" };
        q.Enqueue(a, ActionPriority.Player);
        q.Enqueue(b, ActionPriority.Player);
        q.Enqueue(x, ActionPriority.Instant);    // 插到**另一条道**，不该打乱 Player 道的相对次序
        q.Enqueue(c, ActionPriority.Player);

        Check(q.CountOf(ActionPriority.Player), 3, "T3 夹具：Player 道里 3 条（A/B/C）");
        Check(q.CountOf(ActionPriority.Instant), 1, "T3 夹具：Instant 道里 1 条（X）");

        // 先取走 Instant 的 X（道序在前），再取 Player 道的三条
        ActionPriority lane0;
        QueuedAction first;
        CheckTrue(q.TryDequeue(out lane0, out first), "T3 第一次出队成功");
        Check((int)lane0, 1, "T3 第一条来自 Instant(1) 道（X）");
        Check(first != null ? first.Label : "(null)", "X", "T3 第一条是 X");

        var got = new List<string>();
        for (int k = 0; k < 3; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T3 Player 道第 {k + 1} 次出队应成功");
            if (ok) got.Add(act.Label);
        }
        Check(string.Join(",", got.ToArray()), "A,B,C", "T3 同一条道 = FIFO（A→B→C），⛔ 不是 LIFO");

        // 🧪 队尾追加：Seq 单调递增（A=1 B=2 X=3 C=4）
        Check(a.Seq, 1, "🧪 T3 A 的入队序号 = 1");
        Check(b.Seq, 2, "🧪 T3 B 的入队序号 = 2");
        Check(x.Seq, 3, "🧪 T3 X 的入队序号 = 3（另一条道也参与全局递增）");
        Check(c.Seq, 4, "🧪 T3 C 的入队序号 = 4 ⇒ C 是**队尾**追加（⛔ 不是插队首）");
    }

    // ==================================================================
    //  T8 · Interrupt 跳队（跨道优先，不需要抢占）
    // ==================================================================
    /// <summary>
    /// 先入 Instant 的 A、再入 Interrupt 的 B ⇒ 出队必须 **B → A**。
    /// 🧨 若实现「按入队顺序出队」（单队列）⇒ 取到 A,B ⇒ 必红。
    /// ⚠️ 这里只测「跨道优先」；「**不抢占**正在结算的那一条」是 `Resolving` 旗标的语义
    ///    （T4，等泵点接线后再测 —— 本批没有泵点，测不了）。
    /// </summary>
    static void W61T8InterruptJumpsQueue()
    {
        var q = new ActionQueue();
        q.Enqueue(new QueuedAction { Label = "A" }, ActionPriority.Instant);
        q.Enqueue(new QueuedAction { Label = "B" }, ActionPriority.Interrupt);

        var got = new List<string>();
        for (int k = 0; k < 2; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T8 第 {k + 1} 次出队成功");
            if (ok) got.Add(act.Label);
        }
        Check(string.Join(",", got.ToArray()), "B,A",
              "T8 Interrupt 道**跳队**：后入队的 B(Interrupt) 先出，A(Instant) 后出");
    }

    // ==================================================================
    //  T9 · DelayedInstant 最后（连 Player 都排在它前面）
    // ==================================================================
    /// <summary>
    /// 入队顺序**故意反着来**（3 → 0 → 1 → 2，即 DelayedInstant, Player, Instant, Interrupt），
    /// 出队必须仍是 **Interrupt → Instant → DelayedInstant → Player**（= `2,1,3,0`）。
    /// 🧨 若实现按**入队顺序**、或按**枚举数值**（0,1,2,3）排 ⇒ 出队序都不是 `2,1,3,0` ⇒ 必红。
    /// 🔴 这一条是本批唯一能证明「**DelayedInstant 比 Player 还晚**」的断言（T2 的入队顺序里
    ///    DelayedInstant 与 Player 的相对位置没被单独拉开）。
    /// </summary>
    static void W61T9DelayedInstantLast()
    {
        var q = new ActionQueue();
        q.Enqueue(new QueuedAction { Label = "d" }, ActionPriority.DelayedInstant);   // 先入、最后出
        q.Enqueue(new QueuedAction { Label = "p" }, ActionPriority.Player);
        q.Enqueue(new QueuedAction { Label = "i" }, ActionPriority.Instant);
        q.Enqueue(new QueuedAction { Label = "x" }, ActionPriority.Interrupt);

        var got = new List<string>();
        for (int k = 0; k < 4; k++)
        {
            ActionPriority lane;
            QueuedAction act;
            bool ok = q.TryDequeue(out lane, out act);
            CheckTrue(ok, $"T9 第 {k + 1} 次出队成功");
            if (ok) got.Add(act.Label);
        }
        Check(string.Join(",", got.ToArray()), "x,i,d,p",
              "T9 DelayedInstant 最后（出队 = Interrupt→Instant→DelayedInstant→Player，入队顺序故意反）");
    }

    // ==================================================================
    //  T13 · 非法 priority ⇒ LogError ＋ 丢弃（⛔ 不 clamp、不默认）
    // ==================================================================
    /// <summary>
    /// `Enqueue(action, (ActionPriority)5)` 与 `(ActionPriority)(-1)` 都必须：
    /// **返回 false** ＋ **不入队** ＋ `Dropped` 计数 +1。
    /// 🧨 若实现「clamp 到最近的道」或「默认 Instant」⇒ `Count` 会变成 1/2 ⇒ 必红
    ///    （只断「不抛异常」是自证 —— 那样 clamp/默认 也照样绿）。
    /// ⚠️ 本用例会**故意打两条 `Debug.LogError`**（这就是「出声」本身）——
    ///    批处理日志里看到这两条**不是**失败，它们由下面三条断言兜着。
    /// </summary>
    static void W61T13InvalidPriority()
    {
        var q = new ActionQueue();

        bool ok5 = q.Enqueue(new QueuedAction { Label = "bad5" }, (ActionPriority)5);
        Check(ok5, false, "T13 非法 priority=5 ⇒ Enqueue 必须返回 false");
        Check(q.Count, 0, "T13 非法 priority=5 的动作**不入队**（Count=0 —— 若 clamp/默认，这里 ≥1 ⇒ 红）");
        Check(q.Dropped, 1, "T13 非法 priority=5 ⇒ 丢弃计数 = 1（出声口在 `Enqueue` 的 LogError）");

        bool okNeg = q.Enqueue(new QueuedAction { Label = "badNeg" }, (ActionPriority)(-1));
        Check(okNeg, false, "T13 非法 priority=-1 ⇒ Enqueue 必须返回 false");
        Check(q.Count, 0, "T13 两条非法动作都不入队 ⇒ Count 仍为 0");
        Check(q.Dropped, 2, "T13 丢弃计数累计 = 2");

        // 🧪 夹具对照：合法的 0 必须能进（否则「全部丢弃」也能让上面几条绿 ⇒ 假绿）
        bool ok0 = q.Enqueue(new QueuedAction { Label = "ok0" }, ActionPriority.Player);
        CheckTrue(ok0, "🧪 T13 对照：合法 priority=0 必须入队成功（防「Enqueue 恒 false」的假绿）");
        Check(q.Count, 1, "🧪 T13 对照：合法那条真的进了队列（Count=1）");
    }

    // ==================================================================
    //  T14 · 数据级：`cards_engine.json` v13 的 `triggerPriority` → `CardDef`
    // ==================================================================
    /// <summary>
    /// **这一条测的是「接线那一跳」** —— `CardDatabase.Parse` 把 v13 的 `triggerPriority` 列
    /// 逐条传给 `CardDef` 的构造参数（落进 `CardDef._triggerPriority` / `TriggerPriority()`），
    /// ⛔ 不是测 `TriggerPriority()` 自己的「缺键 ⇒ 1」那半句。
    ///
    /// 🔴 **期望值全部写死**，来源 = `cards_engine.json` v13 现读（2026-10-21）：
    ///   非默认的**只有 4 张** —— `ASH53` spiritstone 3 · `SAU13`/`SAU70` artifice 3 · `UM37` codex 3。
    ///
    /// 🧨 什么会让它红：
    ///   · `CardDatabase` **不传**这一列（= 本批之前的状态）⇒ 四张卡的 3 全变 1 ⇒ 前四条红；
    ///   · 生成器把 `GOF97` 那条**空 logic 的 `priority=0`** 落进来 ⇒ 全池扫描多一条 ⇒ 红；
    ///   · 生成器 / 卡表把某张卡的**默认值 1 也写进列**、或给别的卡落了非默认值 ⇒ 全池扫描的集合变大 ⇒ 红。
    ///   · `CardDef.TriggerPriority` 的「缺键 ⇒ 1」被改成返回 0 / 抛异常 ⇒ `SAU1` 那两条红。
    ///
    /// ⚠️ 全池扫描用的关键词表 = `CardDef.RoutableTriggers`（25 词）—— 生成器收的 21 个键
    ///   **逐个都在里面**（`gen_cards_engine.py` 的 `ABILITY_TRIGGER_KEYWORD` 收词时就核过这一条）
    ///   ⇒ 不会漏扫；多扫到的几个词（`rally`/`backlash`/…）在数据里没有对应列，返回默认 1，无副作用。
    /// </summary>
    static void W61T14DataTriggerPriority()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool.Count >= 1000, $"T14 夹具：卡表加载 {pool.Count} 张（应 ≥ 1000）");

        var sau13 = CardDatabase.FindById(pool, "SAU13");
        var sau70 = CardDatabase.FindById(pool, "SAU70");
        var um37 = CardDatabase.FindById(pool, "UM37");
        var ash53 = CardDatabase.FindById(pool, "ASH53");
        var sau1 = CardDatabase.FindById(pool, "SAU1");
        var gof97 = CardDatabase.FindById(pool, "GOF97");
        CheckTrue(sau13 != null && sau70 != null && um37 != null && ash53 != null && sau1 != null && gof97 != null,
                  "T14 夹具：六张样本卡都在池里（SAU13 / SAU70 / UM37 / ASH53 / SAU1 / GOF97）");
        // 夹具不成立就**不再往下比**（否则空引用会把整条用例抛掉、后面的断言一条都记不上）
        if (sau13 == null || sau70 == null || um37 == null || ash53 == null || sau1 == null || gof97 == null)
            return;

        // ---- ① 四张真卡：非默认 3（DelayedInstant）----
        Check(sau13.TriggerPriority("artifice"), 3, "T14 `SAU13`（Canoptek Spyder）`artifice` = 3（DelayedInstant · 原版真值）");
        Check(sau70.TriggerPriority("artifice"), 3, "T14 `SAU70`（Damaged Hexmark）`artifice` = 3（DelayedInstant · 原版真值）");
        Check(um37.TriggerPriority("codex"), 3, "T14 `UM37`（Death from Above）`codex` = 3（DelayedInstant · 原版真值）");
        Check(ash53.TriggerPriority("spiritstone"), 3, "T14 `ASH53`（Reclaim the Stars）`spiritstone` = 3（DelayedInstant · 原版真值）");

        // ---- ② 普通卡：默认 1 ----
        // `SAU1` 的 `keywords` 里**就写着** `Artifice`（卡面 `Artifice: Deploy a Canoptek Scarab`），
        //   但它的原版 priority = 1 ⇒ 列里没有它 ⇒ 读出来必须是 1。
        //   🧨 若接线把「有 Artifice 关键词」当成「有 priority」、或 `Parse` 给全池都塞了 1（改为落默认值），
        //      这条与下面的全池扫描会一起露。
        Check(sau1.TriggerPriority("artifice"), 1, "T14 `SAU1`（普通卡 · `keywords` 有 `Artifice`）`artifice` = 1（默认 Instant）");
        // ---- ③ 没登记的关键词 ⇒ 默认 1（⛔ 不是 0）----
        Check(sau1.TriggerPriority("no_such_trigger_keyword"), 1, "T14 未登记的关键词 ⇒ 默认 1（⛔ 不是 0）");

        // ---- ④ GOF97 那条数据陷阱：空 logic 的 priority=0 必须**不落** ----
        //   真值 `GOF97 Gargantuan Squiggoth`：第 0 条 `trigger=0 / priority=0 / logic=0`（空 logic）、
        //   第 1 条 `trigger=0 / priority=1 / logic=2`、第 2 条 `trigger=300 Mob / priority=1`。
        //   生成器「只认有 logic 的那条」⇒ 本卡**一条非默认都不该有**（尤其不能凭空出现 = 0 / = 2）。
        //   🧨 生成器改回「第一条为准」那版 ⇒ 若 trigger=0 哪天被映射成关键词，这里会先红；
        //      今天 trigger=0 没有关键词 ⇒ 本条的**主要兜底**是下面那条全池扫描（GOF97 不在清单里）。
        Check(gof97.TriggerPriority("mob"), 1, "T14 `GOF97`（空 logic 那条 priority=0 必须不落）`mob` = 1（默认 Instant）");

        // ---- ⑤ 全池扫描：非默认项必须**恰好**是上面那 4 条 ----
        //   这一条是**结构性**的：它同时覆盖「漏接线」（0 条）、「多接线 / 生成器回归」（>4 条），
        //   而且期望清单里 **没有 GOF97** ⇒ 那条陷阱的两种可能错法都会被它抓住。
        var got = new List<string>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            foreach (string kw in CardDef.RoutableTriggers)
            {
                int pr = c.TriggerPriority(kw);
                if (pr != CardDef.DefaultTriggerPriority) got.Add(c.Id + "/" + kw + "=" + pr);
            }
        }
        got.Sort(System.StringComparer.Ordinal);
        Check(got.Count, 4, "★ T14 全池非默认 priority 的**条目数** = 4（2026-10-21 现读；漏接线 ⇒ 0、多落一条 ⇒ 5+）");
        Check(string.Join(",", got.ToArray()), "ASH53/spiritstone=3,SAU13/artifice=3,SAU70/artifice=3,UM37/codex=3",
              "★ T14 全池非默认 priority 的**逐条清单**（`id/关键词=值`，按 Ordinal 排序）");
    }

    // ==================================================================
    //  T4 · 不抢占：`Resolving` 为真时泵点**一条都不取**
    // ==================================================================
    /// <summary>
    /// 原版 `BattleManager__Update.c:1271` —— `resolvingActionFlag(+0x280)` 为真时**不泵队**；
    /// Interrupt 只是「下一个动作边界上的最高道」，**不会打断正在结算的那一条**。
    /// 🧨 若 `PumpActionQueues` 不看 `Resolving` ⇒ 第一次泵就会跑掉 1 条 ⇒ 第一条断言红。
    /// 🧪 对照：把 `Resolving` 清成 false 再泵，**必须**跑掉那条 ⇒ 防「泵点恒不干活」的假绿。
    /// </summary>
    static void W61T4NoPreempt()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int ran = 0;
        RuleCore.EnqueueAbility(ctx, ActionPriority.Interrupt, "T4", () => ran++);

        ctx.ActionQueue.Resolving = true;                       // = 正在结算一条动作
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 0, "T4 `Resolving = true` ⇒ 泵点**一条都不取**（Interrupt 不抢占当前动作）");
        Check(ctx.ActionQueue.Count, 1, "T4 那条动作仍在队列里（⛔ 没被丢弃）");

        ctx.ActionQueue.Resolving = false;
        RuleCore.PumpActionQueues(ctx);
        Check(ran, 1, "🧪 T4 对照：`Resolving = false` 之后同一条**必须**被结算（防「泵点恒不干活」假绿）");
        Check(ctx.ActionQueue.Count, 0, "T4 泵完 ⇒ 队列空");
    }

    // ==================================================================
    //  T20 · 入队**不短路**（「全队列」与「队列空就直接跑」的分水岭）
    // ==================================================================
    /// <summary>
    /// 🔴 这一条钉的是**用户 2026-10-21 的口径：按原版做全队列，⛔ 不许降级成「队列空就直接结算」**。
    /// 原版 `BattleManager.AddAutoActionToQueue` 只做 `List.Add`，取队的是 `Update`。
    /// 🧨 若有人给 `EnqueueAbility` 加一句「`Count == 0` 就直接跑」的短路「优化」⇒ 这条立刻红。
    ///    而那种改动在**单条动作**的对局里**看不出任何差别**（T2/T5 那种跨道用例才露，且看起来只是「顺序怪」）
    ///    ⇒ 所以单开一条把它钉死。
    /// </summary>
    static void W61T20EnqueueDoesNotShortCircuit()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int ran = 0;
        bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T20", () => ran++);
        CheckTrue(ok, "T20 夹具：入队成功");
        Check(ran, 0, "★ T20 入队之后**一次都没跑**（入队 ≠ 结算 —— 全队列，⛔ 不是「空了就直接跑」）");
        Check(ctx.ActionQueue.Count, 1, "T20 它在队列里等着泵点");
    }

    // ==================================================================
    //  T21 · 泵序 + 动作内部产生的触发**只入队、排到道序位上**
    // ==================================================================
    /// <summary>
    /// 场景（一条动作 = A，它在结算时再入队 B(Instant) 与 C(Interrupt)）：
    ///   ① 泵取 A（Instant 道）→ A 执行 → 它把 B、C 追加进各自的队尾；
    ///   ② 泵**接着**取 —— 此时队里有 B(1)、C(2) ⇒ **C 先**（跨道 priority 压过入队序）；
    ///   ③ 最后 B。
    /// ⇒ 期望执行序 = `A,C,B`。
    /// 🧨 若动作内部**递归**结算（A 里直接跑 B/C）⇒ 观测到 `A,B,C` ⇒ 红；
    ///    若泵点按入队顺序取（单队列）⇒ 也是 `A,B,C` ⇒ 红。
    /// 🧪 夹具自检：A 里那两次入队都必须返回 true（否则「压根没入队」会让期望巧合成立）。
    /// </summary>
    static void W61T21PumpOrderAndRequeue()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var seq = new List<string>();
        ctx.ActionQueue.Enqueue(new QueuedAction
        {
            Label = "A",
            Execute = () =>
            {
                seq.Add("A");
                CheckTrue(RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B", () => seq.Add("B")),
                          "🧪 T21 A 内部：B 入队成功");
                CheckTrue(RuleCore.EnqueueAbility(ctx, ActionPriority.Interrupt, "C", () => seq.Add("C")),
                          "🧪 T21 A 内部：C 入队成功");
            },
        }, ActionPriority.Instant);

        RuleCore.PumpActionQueues(ctx);
        Check(string.Join(",", seq.ToArray()), "A,C,B",
              "T21 泵序：A(先) → C(Interrupt 跨道压过入队序) → B（动作内部的触发 **只入队**，⛔ 不递归）");
        Check(ctx.ActionQueue.Count, 0, "T21 泵完 ⇒ 队列空");
    }

    // ==================================================================
    //  T22 · 顶层动作外壳：`using (BeginAction(ctx))` 退出时把队列取空
    // ==================================================================
    /// <summary>
    /// 这是「泵点集合」那条施工决策的**唯一结构断言**：只要动作是在 `BeginAction` 里跑的，
    /// 它内部入队的触发**必然**在退出那一刻被结算（我们无帧循环 ⇒ 等价于原版下一帧）。
    /// 🧨 若 `ActionScope.Dispose` 忘了泵（或 `Resolving` 忘了清）⇒ `ran` 停在 0 ⇒ 红。
    /// 🧪 第二条外壳是**灭自证**：只测一次的话，「Dispose 把 `Resolving` 留成 true」这种错
    ///    在单次运行里看不出来（第二次会静默一条都不跑）。
    /// </summary>
    static void W61T22ScopeDrains()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int ran = 0;
        using (RuleCore.BeginAction(ctx))
        {
            RuleCore.EnqueueAbility(ctx, ActionPriority.DelayedInstant, "T22", () => ran++);
            Check(ran, 0, "T22 外壳内部：触发**只入队**（`Resolving` 为真 ⇒ 不结算）");
        }
        Check(ran, 1, "★ T22 外壳退出 ⇒ 队列被取空（DelayedInstant 也照样跑）");
        Check(ctx.ActionQueue.Resolving, false, "T22 外壳退出后 `Resolving` 必须清掉（否则以后所有泵点都空转）");

        using (RuleCore.BeginAction(ctx))
            RuleCore.EnqueueAbility(ctx, ActionPriority.Player, "T22b", () => ran++);
        Check(ran, 2, "🧪 T22 第二个外壳仍然能把动作泵出来（防「Resolving 残留 ⇒ 之后全静默」）");
    }

    // ==================================================================
    //  T5 · **真卡数据**上的「跨道 priority 压过入队序」
    // ==================================================================
    /// <summary>
    /// 判据（判据报告 §2.6）：层次 = **① 监听者遍历序（进场序 / `PlayOrder`）→ ② 卡内序 →
    ///   ③ 入队（各 ability 的 priority 道尾）→ ④ 执行（固定道序，同道 FIFO）**
    ///   ⇒ `PlayOrder` **只**在同一条道内等价于执行序；**跨道时 priority 压过它**。
    ///
    /// 本条用**真卡真数据**造那一步：`SAU13`（`artifice` = 3 = DelayedInstant）**先**入队、
    /// `SAU1`（`artifice` = 1 = Instant）**后**入队 ⇒ 出队必须是 `SAU1 → SAU13`。
    /// 🧨 若 `TriggerLane` 不读数据（恒 1）⇒ 两条都进 Instant ⇒ 出队变成 `SAU13,SAU1`（先入先出）⇒ 红。
    /// 🧨 若泵点按入队序执行 ⇒ 同样是 `SAU13,SAU1` ⇒ 红。
    /// 🧪 夹具自检：两条的 `TriggerLane` 必须**分别是 3 与 1**（否则这条只是在测「两条 Instant 的 FIFO」）。
    /// ⚠️ 它**不能**替代真对局里的 T5（那要跑 `Artifice` 的实际触发）；它钉的是
    ///    「数据 → 道 → 消费序」这条链**在真实卡数据上**是通的。
    /// </summary>
    static void W61T5PriorityBeatsEnqueueOrder()
    {
        var pool = CardDatabase.Load();
        var sau13 = CardDatabase.FindById(pool, "SAU13");
        var sau1 = CardDatabase.FindById(pool, "SAU1");
        CheckTrue(sau13 != null && sau1 != null, "T5 夹具：SAU13 / SAU1 都在池里");
        if (sau13 == null || sau1 == null) return;

        Check((int)RuleCore.TriggerLaneOfCard(sau1, "artifice"), 1, "🧪 T5 夹具：`SAU1` 的 artifice 道 = 1（Instant）");
        Check((int)RuleCore.TriggerLaneOfCard(sau13, "artifice"), 3, "🧪 T5 夹具：`SAU13` 的 artifice 道 = 3（DelayedInstant）");

        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var seq = new List<string>();
        RuleCore.EnqueueAbility(ctx, RuleCore.TriggerLaneOfCard(sau13, "artifice"), "SAU13", () => seq.Add("SAU13"));
        RuleCore.EnqueueAbility(ctx, RuleCore.TriggerLaneOfCard(sau1, "artifice"), "SAU1", () => seq.Add("SAU1"));
        RuleCore.PumpActionQueues(ctx);

        Check(string.Join(",", seq.ToArray()), "SAU1,SAU13",
              "★ T5 **跨道 priority 压过入队序**：后入队的 SAU1(Instant) 先于先入队的 SAU13(DelayedInstant)");
    }

    // ==================================================================
    //  T30 / T31 · 🔴 2026-10-23（`T03·c` 残余）：**手牌跳 / 常驻跳各自的接线断言**
    //
    //  判据 → `普查产出_1021/W61b_引擎_T03c审查修复.md` §三·4 / §七·2：
    //    T23 钉的是**棋盘那一跳**（`BroadcastWhen` 的棋盘监听器），而 `BroadcastWhen` 一共**四跳**
    //    （棋盘 / 常驻 `BroadcastPersistentWhen` / 手牌陷阱 `BroadcastHandTrapWhen` /
    //     手牌单位卡 `BroadcastHandWhen`）；后两跳从 2026-10-21 起也**只入队**
    //    （`EnqueueAbility`），但**没有一条断言钉它们** ⇒ 「全改回同步递归」时 T23 仍绿。
    //  本条补上缺的这两跳。
    // ==================================================================

    /// <summary>
    /// **T30 · 手牌跳（单位卡那一支 = `BroadcastHandWhen`）只入队、由泵点结算。**
    ///
    /// 夹具：手里放一张**单位卡**，正文 `When you deploy a troop, gain 2 Quest Points`；
    /// 外面自己开一层 `BeginAction`（⇒ `PlayCard` 变成内层、不泵队），在里面部署一个部队。
    ///   · 动作体内：队列里躺着那条手牌监听、任务点**还没涨**；
    ///   · 边界之后：任务点 +2。
    /// 🧨 把 `BroadcastHandWhen` 那条 `EnqueueAbility` 改回同步 `ResolveOps` ⇒ 动作体内就 +2（红）。
    /// 🧨 `ActionScope.Dispose` 不泵队 ⇒ 边界之后不涨（红）。
    /// </summary>
    static void W61T30HandListenerQueuesUntilBoundary()
    {
        var watcher = new CardDef("W61T30W", "W61T30W", "unit",
                                  "When you deploy a troop, gain 2 Quest Points",
                                  "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
        CheckTrue(watcher.WhenTriggers.Count > 0,
                  "🧪 T30 夹具：监听器注册上了（没注册 ⇒ 下面两条假绿）");
        CheckTrue(watcher.CanListenForEvents, "🧪 T30 夹具：它是单位卡 ⇒ 走 `BroadcastHandWhen` 那一跳");

        var troop = Unit("W61T30T", 1, 1, 5);
        var ctx = Battle(new[] { troop }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        ctx.Players[0].Hand.Add(ctx.NewInstance(watcher));      // 直接塞进手牌（不走抽牌）
        int qp0 = ctx.Players[0].QuestPoints;

        using (RuleCore.BeginAction(ctx))                        // ← 外层外壳：PlayCard 不泵队
        {
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "W61T30T"), 2, out int slot),
                      RuleCodes.OK, "T30 部署一个部队（外层动作里）");
            CheckTrue(slot >= 0 && ctx.Players[0].Board[slot] != null, "T30 前提：部队已上场");
            CheckTrue(ctx.ActionQueue.Count > 0,
                      "★ T30 动作体内：**手牌监听那条在队列里等着**（⛔ 不是当场结算）"
                    + "｜🧨 `BroadcastHandWhen` 改回同步 `ResolveOps` ⇒ 本行红");
            Check(ctx.Players[0].QuestPoints, qp0,
                  "★ T30 动作体内：手牌监听**还没结算** ⇒ 任务点没涨"
                + "｜🧨 同上（同步）⇒ 本行实得 +2");
        }
        Check(ctx.Players[0].QuestPoints, qp0 + 2,
              "★ T30 边界之后：手牌监听结算完毕 ⇒ **任务点 +2**"
            + "｜🧨 `ActionScope.Dispose` 不泵队 ⇒ 本行实得 +0");
        Check(ctx.ActionQueue.Count, 0, "T30 边界之后：队列取空");
    }

    /// <summary>
    /// **T31 · 常驻跳（`BroadcastPersistentWhen`）只入队、由泵点结算。**
    ///
    /// 夹具：先打一张 `For the rest of this battle, give Shield to all Drones you deploy`
    /// 登记一条**部署时**的常驻效果（判据 → `TestDeployBuffAndCostMore` ①，那条已经端到端验过）；
    /// 然后在外层 `BeginAction` 里部署一张 Drone。
    ///   · 动作体内：队列里有常驻监听、Drone **还没有**护盾；
    ///   · 边界之后：护盾到手。
    /// 🧨 把 `BroadcastPersistentWhen` 那条 `EnqueueAbility` 改回同步 ⇒ 动作体内就有护盾（红）。
    /// </summary>
    static void W61T31PersistentQueuesUntilBoundary()
    {
        var pool = CardDatabase.Load();
        var tac = Tactic("W61T31Tac", 1,
                         "For the rest of this battle, give Shield to all Drones you deploy");
        var drone = new CardDef("W61T31D", "W61T31D", "unit", "", "common", "Test",
                                1, 1, 3, 0, null, subtype: "Drone");
        var ctx = BattlePool(new[] { tac, drone }, new[] { Unit("E", 1, 1, 9) }, pool,
                             warlordFaction: "TauEmpire");
        ToP1Turn(ctx, 3);
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W61T31Tac"), -1), RuleCodes.OK,
                  "T31 打出「本场：你部署的 Drone 获得护盾」");
        Check(ctx.PersistentEffects.Count, 1, "T31 前提：登记了 1 条常驻效果");

        UnitState droneU = null;
        using (RuleCore.BeginAction(ctx))
        {
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "W61T31D"), 2, out int slot),
                      RuleCodes.OK, "T31 部署一张 Drone（外层动作里）");
            var d = slot >= 0 ? ctx.Players[0].Board[slot] : null;
            droneU = d;
            CheckTrue(d != null, "T31 前提：Drone 已上场");
            CheckTrue(ctx.ActionQueue.Count > 0,
                      "★ T31 动作体内：**常驻监听那条在队列里等着**（⛔ 不是当场结算）"
                    + "｜🧨 `BroadcastPersistentWhen` 改回同步 ⇒ 本行红");
            CheckTrue(d != null && !d.HasShield,
                      "★ T31 动作体内：常驻效果**还没结算** ⇒ Drone 没有护盾"
                    + "｜🧨 同上（同步）⇒ 本行实得「有护盾」");
        }
        CheckTrue(droneU != null && droneU.HasShield,
                  "★ T31 边界之后：常驻效果结算完毕 ⇒ **护盾到手**"
                + "｜🧨 `ActionScope.Dispose` 不泵队 ⇒ 本行实得「没护盾」");
    }

    /// <summary>
    /// **T32 · 手牌陷阱那一跳（`BroadcastHandTrapWhen`，非单位卡）只入队、由泵点结算。**
    ///
    /// 为什么单列一条：`BroadcastWhen` 四条非棋盘跳里，棋盘跳有 `T23`、手牌跳（单位卡）有 `T30`、
    /// 常驻跳有 `T31`，**只剩这一条没有独立断言**（`A1` 报告 §四末）——
    /// 把它改回同步 `ResolveOps`，上面三条**全都还是绿的**。
    ///
    /// 夹具：一张**计策卡**（`HandTrapWhens` 只收「**非单位卡** + 整条 desc 就是一句
    /// `When &lt;事件&gt;, &lt;正文&gt;`」，判据 → `CardDef.CollectHandTrapWhens` /
    /// `EffectText.SplitHandTrapWhen`）躺在手牌里，监听「部署一个部队」⇒ 任务点 +2。
    /// 在外层 `BeginAction` 里部署一个部队：
    ///   · 动作体内：队列里躺着那条陷阱、任务点**还没涨**；
    ///   · 边界之后：任务点 +2、队列取空。
    /// 🧨 `BroadcastHandTrapWhen` 改回同步 `ResolveOps` ⇒ 动作体内就 +2（红）。
    /// 🧨 `ActionScope.Dispose` 不泵队 ⇒ 边界之后不涨（红）。
    /// </summary>
    static void W61T32HandTrapQueuesUntilBoundary()
    {
        var trap = Tactic("W61T32Trap", 1, "When you deploy a troop, gain 2 Quest Points");
        // 夹具自证：**没登记成手牌陷阱 ⇒ 下面三条都是假绿**
        CheckTrue(trap.HandTrapWhens.Count > 0,
                  "🧪 T32 夹具：非单位卡的那句 `When …` 登记成**手牌陷阱**了（`HandTrapWhens` 非空）");
        CheckTrue(trap.WhenTriggers.Count == 0,
                  "🧪 T32 夹具：它**不是**单位卡 ⇒ 不许同时进 `WhenTriggers`（那会结算两次）");
        CheckTrue(!trap.CanListenForEvents,
                  "🧪 T32 夹具：`CanListenForEvents` 只对单位卡为真 ⇒ 它走的是手牌陷阱那一跳");

        var troop = Unit("W61T32T", 1, 1, 5);
        var ctx = Battle(new[] { troop }, new CardDef[0]);
        ctx.Players[0].Energy = 99;
        ctx.Players[0].Hand.Add(ctx.NewInstance(trap));      // 直接塞进手牌（不走抽牌）
        int qp0 = ctx.Players[0].QuestPoints;

        using (RuleCore.BeginAction(ctx))                    // ← 外层外壳：PlayCard 不泵队
        {
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "W61T32T"), 2, out int slot),
                      RuleCodes.OK, "T32 部署一个部队（外层动作里）");
            CheckTrue(slot >= 0 && ctx.Players[0].Board[slot] != null, "T32 前提：部队已上场");
            CheckTrue(ctx.ActionQueue.Count > 0,
                      "★ T32 动作体内：**手牌陷阱那条在队列里等着**（⛔ 不是当场结算）"
                    + "｜🧨 `BroadcastHandTrapWhen` 改回同步 `ResolveOps` ⇒ 本行红");
            Check(ctx.Players[0].QuestPoints, qp0,
                  "★ T32 动作体内：手牌陷阱**还没结算** ⇒ 任务点没涨"
                + "｜🧨 同上（同步）⇒ 本行实得 +2");
        }
        Check(ctx.Players[0].QuestPoints, qp0 + 2,
              "★ T32 边界之后：手牌陷阱结算完毕 ⇒ **任务点 +2**"
            + "｜🧨 `ActionScope.Dispose` 不泵队 ⇒ 本行实得 +0");
        Check(ctx.ActionQueue.Count, 0, "T32 边界之后：队列取空");
    }
}
