// RuleEngineTest_EngineB94B15.cs — 第九十四会话 · 波 3 · 执行代理 `B15`（`Core/ActionPump.cs` 的 8 条 B/C）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// `Editor/RuleEngineTest.cs` 同一时刻只有一个写手（F1）⇒ 本批断言全写在这里、**只交一个入口方法名**，
// 由调度台在 `Run()` 里加一行 `Step(B94B15EngineGaps)`（⛔ 本代理不改 F1 的文件）。
//
// 本件补的是 **F4（`RuleEngineTest_EngineB94F4.cs` 的 T1/T2）没覆盖到的两半**，以及本批新改的那一处：
//   ① 账 242 `CancelEffectsFromDisappearingUnitFromQueue` 的**命中条件 ③（`targetCard` 过滤）**
//      —— 判据 `BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c:90-110`：
//      `param_3`（targetCard）非空 ⇒ 要求 `queue[i].targetCard == param_3`；
//      `queue[i].targetCard` 为 null ⇒ **跳过**（⛔ 不是「null 也算命中」）。F4 的 T2 八格全用 `targetCard=null`，
//      这一半**一次都没跑过**。
//   ② 账 241 `CancelEffectsFromDisappearingUnit` 的**「豁免只结束那一条泳道」**：F4 的 T2·⑦ 只验了
//      「两条道各撤 1 ⇒ 合计 2」；「Instant 道先撞豁免 ⇒ **不终止** Player 道」这一格没跑过
//      （`.c` 里三处 `return` 是**函数级返回**，等价物是「只 return 本泳道的扫描」）。
//   ③ 账 490 `ResolveAction` 的 `ActionType` 回填通道（`EnqueueAbility(..., actionType: N)` ⇒
//      `QueuedAction.ActionType = N`；缺省 0 = 未标）。**「0 = 未标」是这一族的固有隐患**：
//      `CancelEffectsFromDisappearingUnitFromQueue` 把 `!= 1` 当「可撤」⇒ 未标的 attack 会被撤
//      （原版不会）。今天 `jam` 生产者 0 张 ⇒ 零可观测，本件把这一现状**锁住**（改了就红 ⇒ 逼着重新裁定）。
//   ④ 本批 `ActionPump.cs` 改的那一处日志：撤销警告现在打 `BattleActionType` **枚举名**
//      （原版 `System.Enum::ToString`；`Core/TutorialScript.cs:96` 的 91 档）。枚举若被重新编号，
//      三档豁免（1 / 0x1a / 0x2d）会**静默失效** ⇒ 这里把七个被代码当常数用的成员逐个钉死。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行 `Step(B94B15EngineGaps)`）。</summary>
    public static void B94B15EngineGaps()
    {
        Section("🆕 B15（B94 波 3）：撤泳道 targetCard 过滤 · 豁免只结束本道 · ActionType 回填/未标 0 · BattleActionType 常数");
        B94B15T1TargetCardFilter();
        B94B15T2ExemptionEndsLaneOnly();
        B94B15T3ActionTypePlumbing();
        B94B15T4BattleActionTypeConstants();
    }

    // ==============================================================================================
    //  ① 命中条件 ③：`targetCard` 过滤（账 242 · F4 未覆盖的那一半）
    // ==============================================================================================

    static void B94B15T1TargetCardFilter()
    {
        // ── ① 传了 targetCard ⇒ 只撤「那一条的 Target 就是它」的；Target=null / 别人的都不撤 ─────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B15Jammed", 1, 1, 3)), false);
            var wanted = new UnitState(ctx.NewInstance(Unit("B15Wanted", 1, 1, 3)), false);
            var other = new UnitState(ctx.NewInstance(Unit("B15Other", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Target = null, Label = "t-null" },
                                    ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Target = other, Label = "t-other" },
                                    ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Target = wanted, Label = "t-wanted" },
                                    ActionPriority.Instant);

            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, wanted, ActionPriority.Instant);
            Check(n, 1, "★★ ① 给了 `targetCard` ⇒ **只撤 Target 就是它** 的那 1 条（`Target=null` / 别人的都不撤）"
                      + "｜判据 `BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c:90-110`"
                      + "｜🧨 把 ③ 写成「`targetCard != null ⇒ 一律撤」/ 把 `Target == null` 当命中 ⇒ 实得 3 ⇒ 红");
            Check(ctx.ActionQueue.CountOf(ActionPriority.Instant), 2,
                  "① 另外两条仍在（`Target=null` 那条**不**因为「没指向」就被当成命中）"
                + "｜🧨 删掉 `a.Target == null ⇒ continue` 这一支 ⇒ 实得 1 ⇒ 红");
        }
        // ── ② targetCard = null ⇒ ③ 整条不筛 ⇒ 三条全中（原版四个调用点里三个走这一档）───────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B15Jammed2", 1, 1, 3)), false);
            var a = new UnitState(ctx.NewInstance(Unit("B15A2", 1, 1, 3)), false);
            var b = new UnitState(ctx.NewInstance(Unit("B15B2", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Target = null, Label = "n" },
                                    ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Target = a, Label = "a" },
                                    ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Target = b, Label = "b" },
                                    ActionPriority.Instant);

            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 3, "★★ ① `targetCard = null` ⇒ **③ 不筛**，三条全撤（原版 `op_Equality(param_3,0)` 为真直接进豁免段）"
                      + "｜🧨 把「`targetCard == null`」也当成一个要比较的目标（实得 1 或 0）⇒ 红");
        }
    }

    // ==============================================================================================
    //  ② 豁免只结束**本泳道**（账 241 —— F4·T2⑦ 没跑过的那一格）
    // ==============================================================================================

    static void B94B15T2ExemptionEndsLaneOnly()
    {
        // ── ① Instant 道先撞豁免 ⓑ（26 recallToHand）⇒ 只停 Instant 道；Player 道那条照撤 ──────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B15J2", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 26, Label = "exempt26" },
                                    ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "cancel" },
                                    ActionPriority.Player);

            int n = RuleCore.CancelEffectsFromDisappearingUnit(ctx, jammed, null, false);
            Check(n, 1, "★★ ② Instant 道先撞豁免 ⓑ ⇒ **只结束那一条泳道**，Player 道那条照撤（合计 1）"
                      + "｜判据 `BattleManager__CancelEffectsFromDisappearingUnit.c` 对四条队列**各调一次**"
                      + "`…FromQueue`，三处豁免的 `return` 只返回到**那一次调用**"
                      + "｜🧨 把豁免写成「跳过整轮四道」（例如在 `CancelEffectsFromDisappearingUnit` 里提前 return）⇒ 实得 0 ⇒ 红");
            Check(ctx.ActionQueue.CountOf(ActionPriority.Instant), 1, "② 撞豁免的那条（26）**仍在**（豁免 = 不撤）");
            Check(ctx.ActionQueue.CountOf(ActionPriority.Player), 0, "② Player 道那条已撤");
        }
        // ── ② 四道各一条可撤 ⇒ 合计 4（原版四个队列一个不漏）────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B15J3", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "p" }, ActionPriority.Player);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "i" }, ActionPriority.Instant);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "r" }, ActionPriority.Interrupt);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 2, Label = "d" }, ActionPriority.DelayedInstant);

            int n = RuleCore.CancelEffectsFromDisappearingUnit(ctx, jammed, null, false);
            Check(n, 4, "★★ ② **四条泳道各撤 1 ⇒ 合计 4**（原版调用序 `+0x350→+0x358→+0x368→+0x360`"
                      + " = Player→Instant→Interrupt→DelayedInstant，一个都不能漏）"
                      + "｜🧨 漏扫任一道（例如只扫 Player/Instant）⇒ 实得 <4 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ③ `ActionType` 回填通道 + 「缺省 0 = 未标」这一现状（账 490 / 242）
    // ==============================================================================================

    static void B94B15T3ActionTypePlumbing()
    {
        // ── ① 缺省 = 0（未标）──────────────────────────────────────────────────────────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            bool ok = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B15 unmarked", () => { });
            CheckTrue(ok, "③ （前提）`EnqueueAbility` 缺省 `actionType` 入队成功");
            var qa = ctx.ActionQueue.At(ActionPriority.Instant, 0);
            CheckTrue(qa != null, "③ （前提）拿得到那一条");
            Check(qa.ActionType, 0,
                  "★★ ③ `EnqueueAbility` 的 `actionType` **缺省 = 0（未标）** —— 原版每条 `BattleAction` 都有真类型，"
                + "我们只有 12 处调用点回填（1/1/4/16/16/51/61/77/77/79/87 ＋ 本文件的 0x41/0x3e）"
                + "｜🧨 把缺省改成非 0（例如抢一个真类型的号）⇒ 红 ——「未标」冒充真类型会让 `…FromQueue` 的豁免误判");
        }
        // ── ② 显式给 ⇒ 原值落进队列条目（27 = transformUnit，账 172 要用的那个号）───────────────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "B15 marked", () => { }, actionType: 27);
            var qa = ctx.ActionQueue.At(ActionPriority.Instant, 0);
            CheckTrue(qa != null, "③ （前提）带了 `actionType: 27` 也入队成功");
            Check(qa.ActionType, 27,
                  "★★ ③ 显式 `actionType: 27`（`BattleActionType.transformUnit`）**原值落进 `QueuedAction.ActionType`**"
                + "｜判据 `BattleManager__AddTransformCardWithBuff.c` 的 `BattleAction___ctor(…, 0x1b(27), …)`（账 172）"
                + "｜🧨 中间被 `?? 0` / 被 `abilityContext` 之类的形参挤掉 ⇒ 实得 0 ⇒ 红");
        }
        // ── ③ 「未标 0」在撤泳道扫描里**等同于「不是 attack」⇒ 会被撤**（锁现状，⛔ 不是「这是对的」）────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var jammed = new UnitState(ctx.NewInstance(Unit("B15J4", 1, 1, 3)), false);
            ctx.ActionQueue.Enqueue(new QueuedAction { Actor = jammed, ActionType = 0, Label = "unmarked" },
                                    ActionPriority.Instant);
            int n = RuleCore.CancelEffectsFromDisappearingUnitFromQueue(ctx, jammed, null, ActionPriority.Instant);
            Check(n, 1, "★★ ③ **「未标 0」在撤泳道扫描里会被撤**（原版 `type != 1` ⇒ 可撤；我们未标的 attack 也是 0）"
                      + "⇒ 今天 `jam` 生产者 0 张所以零可观测，但**回填不全就是隐患**（账 490 的 7 处仍 0）"
                      + "｜🧨 有人把「未标 0」当成「不可撤」或反过来给 0 一个真类型号 ⇒ 红 ⇒ 逼着重新裁定");
        }
    }

    // ==============================================================================================
    //  ④ `BattleActionType` 常数（`Core/TutorialScript.cs:96` 的 91 档）—— 代码把它们当**字面量**在用
    // ==============================================================================================

    static void B94B15T4BattleActionTypeConstants()
    {
        // `CancelEffectsFromDisappearingUnitFromQueue` 的命中/豁免 + `ApplyCancelSideEffects` 的 switch
        // 全部拿这几个号硬比（`1` / `0x1a` / `0x2d`）；枚举一改号 ⇒ 那些分支**静默错档**。
        Check((int)BattleActionType.attack, 1,
              "★★ ④ `attack = 1`（命中条件 ② 的「attack 不撤」用的就是这个号 · 原版 `BattleActionType.cs:4`）"
            + "｜🧨 重新编号 / 抄错 ⇒ 红");
        Check((int)BattleActionType.createHandCard, 23,
              "★★ ④ `createHandCard = 0x17 = 23`（`ActionQueue.ApplyCancelSideEffects` 的副作用 switch）｜🧨 见上");
        Check((int)BattleActionType.recallToHand, 26,
              "★★ ④ `recallToHand = 0x1a = 26`（豁免 ⓑ）｜🧨 见上");
        Check((int)BattleActionType.transformUnit, 27,
              "★★ ④ `transformUnit = 0x1b = 27`（账 172 的出队体 —— 状态层已由 `B94_B12` 落地，但**无 27 号入队点**；"
            + "`CancelEffectsFromDisappearingUnit` 的原版调用点之一）｜🧨 见上");
        Check((int)BattleActionType.destroyUnit, 45,
              "★★ ④ `destroyUnit = 0x2d = 45`（豁免 ⓐ / ⓒ）｜🧨 见上");
        Check((int)BattleActionType.triggerRelentless, 62,
              "★★ ④ `triggerRelentless = 0x3e = 62`（`EnqueueGrantedTrigger` 回填的第二个常数）｜🧨 见上");
        Check((int)BattleActionType.triggerCourtesy, 65,
              "★★ ④ `triggerCourtesy = 0x41 = 65`（`EnqueueGrantedTrigger` 回填的第一个常数）｜🧨 见上");

        // 本批改的日志：原版 `System.Enum::ToString` 打**枚举名**，不在表里的号打**数字**（同 `Enum.ToString`）。
        Check(((BattleActionType)45).ToString(), "destroyUnit",
              "★★ ④ 撤销警告打的是**枚举名**（本批 `ActionPump.cs` 改的 `((BattleActionType)a.ActionType)`）"
            + "｜判据 `BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c` 的 `System_Enum__ToString`"
            + "｜🧨 改回打原版号 / 名字拼错 ⇒ 红");
        Check(((BattleActionType)9999).ToString(), "9999",
              "★★ ④ 不在枚举里的号 ⇒ 打**数字**（`Enum.ToString` 的缺省行为；原版同形）"
            + "｜🧨 换成手写查表并给未知档抛异常 / 打空串 ⇒ 红");
    }
}
