// B104_D_Assert.cs — 执行代理 `B104_D`（第一百零四会话 · 2026-10-31）
//
// ============================ 它盯的是什么 ============================
//   施工单 → `资料/完全复刻/判据/B104_T21与多目标_时间线_施工单.md` 的 D1 / D2 两行。本代理落地的两件：
//
//   · **D1（引擎）**：`RuleCore.BeginTurnCore` 在**本地人类回合**拆成 pre-wait / post-wait 两半，
//     从 `turn_setup` 相位起（`ResolveAtTurn("turn_setup")` / `BroadcastTurnSetup` / 回合初抽牌 /
//     `turn_start` 触发段 / `BroadcastActivateMinions` / `BroadcastTurnStarted` / 天赋）推迟到
//     `RuleCore.ResolvePendingTurnDraw` 才跑。开关 = `BattleContext.DeferTurnDraw`（**默认关**）。
//   · **D2（表现层）**：`BattleDriver.HoldForTurnHeadsUp` 末尾排 `_clock + TurnHeadsUpHold`（1.5 s）
//     的补跑；门 = 本地人类回合 ∧ 非教程第 1 回合 ∧ 非回放/联机（`BattleDriver.PrepareTurnDeferral`）。
//
//   判据（第一权威）= `d:/2/tools/decomp_full/BattleManager._NextTurn_d__395__MoveNext.c`：
//     :358-362 对手支（不进等待）直接 `BroadcastTurnSetup` + `TurnStartCardDraw`
//     :444-491 本地玩家支：`ShowHeadsUpMessage(YourTurn)` → `WaitForSeconds(1.5)` → `state = 2`
//     :76-78   `case 2` → `goto LAB_1809aaee2` → `:465-473` → `LAB_1809aaf0c` = `:359-362`
//     :371     `BroadcastTurnStart` · `:384` `BroadcastActivateMinions` · `:385` `BroadcastTurnStarted`
//     :451-475 教程局第 1 回合**整段 goto 跳过**（连等待都不进）
//
// ============================ ⛔ 本件**故意**没有的断言 ============================
//   · **真机/时间线那一半**（t = 1.4 s 手牌不变、t ≥ 1.5 s +1）—— 要 `BattleDriver` + 场景 + 时间线泵，
//     本件是**纯引擎静态类**，一条 GameObject 都不建。那条由 `BattleScene` 侧的真机验收覆盖。
//   · **对手手牌 / 联机那一侧的行为** —— 本件只能断「开关关着时引擎走同步老路」与
//     「座位不是本地座位时不推迟」这两条等价判据。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   D1-2/D1-3 ← 把 `BeginTurnCore` 里那个 `ShouldDeferTurnDraw` 分支删掉（= 回到同步抽牌）⇒ 手牌当场 +1 ⇒ 红。
//   D1-5      ← 让 `ResolvePendingTurnDraw` 什么都不做 ⇒ 补抽不到 ⇒ 红；
//               让它再跑一遍 pre-wait（能量重算 / `Turn++`）⇒ D1-6 与 D1-9 一起红。
//   D1-7      ← 去掉 `PendingTurnDraw` 这道闸（让补抽口不做幂等）⇒ 连调两次会抽两张 ⇒ 红。
//   D1-8      ← 去掉「`Active == DeferSeat`」那道闸（两侧都推迟）⇒ 对手那一侧 `PendingTurnDraw` 为真 ⇒ 红。
//   D1-10     ← 把 `ResolveAtTurn("turn_start")` 那一整段搬回 **pre-wait**（抽牌之前）⇒ 触发器数到的手牌
//               比 `after` 少 1 ⇒ 红。**这一条是本件的核心**：它同时冤枉不了「抽牌没发生」那种情况
//               （那种情况 D1-5 先红）。
//   D1-11     ← 去掉「教程第 1 回合不推迟」那道闸 ⇒ 红。
//   D1-12     ← 把「教程第 1 回合不推迟」写成「教程局永不推迟」⇒ 红。
//   D1-13     ← 让 `BeginTurnForReplayExpansion` 也走推迟分支 ⇒ 红。
//   D1-14     ← 把 `EffectResolver.AtTurnHandJobDeferred` 改回 `foreach (op) ResolveOne(...)`
//               （绕开 `ResolveOps` 的 `for each` 重复层）⇒ 手牌陷阱只掉 1 点 ⇒ 红。
//   D1-15     ← 把 `EffectResolver.AtTurnPersistJobDeferred` 改回 `foreach (op) ResolveOne(...)`
//               ⇒ 常驻效果只掉 1 点 ⇒ 红。
//   D1-16     ← 把 `DeployJobDeferred` 的 `reseedPerOp: true` 去掉（或把那个形参实现成空转）
//               ⇒ 第二条 op 的 `it` 落到第一条的目标上 ⇒ 红。
//   D1-17     ← 让 `DoRepeat` 把 `Prepaid` 那层去掉（直接递原 op 给 `ResolveOps`）
//               ⇒ 嵌套 `ChargePaidGroups` 再收一遍 ⇒ 能量多扣 2 ⇒ 红。
//
//   对照/反例（灭自证的另一半）：D1-4（开关关着 ⇒ 当场 +1）与 D1-5 用的是**同一套夹具、同一个位置**，
//   只差一个开关 ⇒ 任何「抽牌本身坏了」或「怎么都不抽」的实现都不可能让两条同时绿。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主（`Editor/BattleScene.cs` 或 `Editor/RuleEngineTest.cs`）里加一行：
//     `B104_D_Assert.Run(); _pass += B104_D_Assert.Pass; _fail += B104_D_Assert.Fail;`
//   本件**无副作用**：只建裸 `BattleContext`（不建 GameObject、不碰存档、不读真实卡池）。
using System.Collections.Generic;
using CardPresentation;      // BattleDriver（只读它的常量）
using RuleEngine;            // RuleCore / CardDef / BattleContext / CardInstance / EffectText / MatchType
using UnityEngine;

/// <summary>执行代理 `B104_D` 的断言：回合初抽牌推迟到抬头 1.5 s 之后（D1 引擎拆分 + D2 的引擎等价判据）。</summary>
public static class B104_D_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B104D] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "D1/D2 回合初抽牌推迟到抬头 1.5 s 之后（原版 `_NextTurn` 的 WaitForSeconds 支）");
        LocalTurnDeferred();     // D1-2..D1-7
        OtherSeatImmediate();    // D1-8
        TurnStartSeesDrawnCard();// D1-9/D1-10（核心）
        HandTrapRepeatsPerCard();// D1-14（同一条语义的另一条通道：手牌陷阱）
        PersistentRepeatsPerCard();// D1-15（第三条通道：常驻效果）
        DeployReseedsPerOp();    // D1-16（部署触发：逐 op 重种 `LastTargets`）
        RepeatChargesOnce();     // D1-17（`Repeat this effect`：重复不再收费）
        TutorialFirstTurn();     // D1-11/D1-12
        ReplayExpansion();       // D1-13
        AnchorConstants();       // D2 的常量锚
    }

    // ------------------------------------------------------------------
    //  ① 本地人类回合：`BeginTurn` 返回时手牌不变，补抽口才 +1
    // ------------------------------------------------------------------
    static void LocalTurnDeferred()
    {
        // ---- 正例：开关开 + 行动方就是被推迟的那一侧 ----
        var ctx = NewCtx();
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;
        int hand0 = ctx.Players[0].Hand.Count;
        int turn0 = ctx.Turn;
        C(ctx.Active, 0, "（前提）建局后行动方 = 座位 0（`FirstSeat`）");

        RuleCore.BeginTurn(ctx);

        C(ctx.Turn, turn0 + 1, "D1-1 回合号照常 +1（推迟的只有 post-wait 那半，pre-wait 一个字没动）");
        C(ctx.Players[0].Energy, 2, "D1-1b pre-wait 的能量照常发（P1 第 1 回合 = 2）");
        C(ctx.Players[0].Hand.Count, hand0, "★ D1-2 `BeginTurn` 返回时手牌**没变** —— 抽牌被推迟了"
            + "（原版 `_NextTurn:483-489 WaitForSeconds(1.5)` 之后才 `:360-362 TurnStartCardDraw`）"
            + "｜🧨 删掉 `BeginTurnCore` 里那个 `ShouldDeferTurnDraw` 分支（回到同步抽牌）⇒ 本条红");
        Tr(ctx.PendingTurnDraw, "★ D1-3 `PendingTurnDraw` = 真 —— 它是「唯一补抽口」的闸，也挡住重复补"
            + "｜🧨 置假 / 不置 ⇒ D1-5 与 D1-7 一起红");

        RuleCore.ResolvePendingTurnDraw(ctx);
        C(ctx.Players[0].Hand.Count, hand0 + 1, "★ D1-5 补抽口跑到 ⇒ 手牌 +1（post-wait 真的补上了）"
            + "｜🧨 让 `ResolvePendingTurnDraw` 什么都不做 ⇒ 红");
        Tr(!ctx.PendingTurnDraw, "D1-6 补完就清闸");
        C(ctx.Turn, turn0 + 1, "D1-6b 补抽口**不重跑 pre-wait**（回合号不再 +1）"
            + "｜🧨 让它整段重跑 `BeginTurnCore` ⇒ 红");

        RuleCore.ResolvePendingTurnDraw(ctx);
        C(ctx.Players[0].Hand.Count, hand0 + 1, "★ D1-7 **重复调用不会抽两次**（`PendingTurnDraw` 那道闸）"
            + "｜🧨 去掉幂等闸 ⇒ 变 +2 ⇒ 红");

        // ---- 对照（灭自证）：同一夹具、同一个位置，只把开关关掉 ⇒ 当场 +1 ----
        var cctx = NewCtx();
        cctx.DeferTurnDraw = false;
        cctx.DeferSeat = 0;
        int ch0 = cctx.Players[0].Hand.Count;
        RuleCore.BeginTurn(cctx);
        C(cctx.Players[0].Hand.Count, ch0 + 1, "对照 D1-4 开关**关着** ⇒ 当场抽 1 张（回放 / 联机 / 自检走的同步老路，"
            + "与 D1-2 同夹具同位置 ⇒ 「抽牌坏了」不可能让两条同时绿）");
        Tr(!cctx.PendingTurnDraw, "对照 D1-4b 开关关着 ⇒ 不挂 `PendingTurnDraw`");
    }

    // ------------------------------------------------------------------
    //  ② 对手（不是本地座位）那一侧：**立即**抽 —— 原版 `_NextTurn:358` 的另一支
    // ------------------------------------------------------------------
    static void OtherSeatImmediate()
    {
        var ctx = NewCtx();
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;                 // 本地 = 座位 0
        RuleCore.BeginTurn(ctx);           // P1 的回合（推迟）
        RuleCore.ResolvePendingTurnDraw(ctx);

        RuleCore.EndTurn(ctx);             // → 换边
        C(ctx.Active, 1, "（前提）换边之后行动方 = 座位 1");
        int h1 = ctx.Players[1].Hand.Count;
        RuleCore.BeginTurn(ctx);

        C(ctx.Players[1].Hand.Count, h1 + 1, "★ D1-8 **对手那一侧立即抽 1 张**（原版 `_NextTurn:358` 的 else 支"
            + "不经过那 1.5 s，直接 `:360-362`）｜🧨 去掉「`Active == DeferSeat`」那道闸（两侧都推迟）⇒ 红");
        Tr(!ctx.PendingTurnDraw, "D1-8b 对手回合不挂 `PendingTurnDraw`");
    }

    // ------------------------------------------------------------------
    //  ③ 核心：`turn_start` 触发器看到的必须是**抽完之后**的手牌
    //
    //  夹具 = **场上的一个单位**，卡面 `At the start of your turn, …` +
    //  正文 `deal 1 damage to the enemy warlord for each card in your hand`
    //  ⇒ 敌方督军掉的血 == **触发器结算那一刻的手牌张数**。
    //  推迟那一半没跑时 ⇒ 0 点（连触发都没发）；补抽之后 ⇒ 手牌 = 抽前 + 1。
    //
    //  🔴 **为什么不是「手牌陷阱」那条路**（本会话实测，2026-10-31）：手牌陷阱的结算口
    //  `AtTurnHandJobDeferred`（`Core/EffectResolver.cs:8988`）是 `foreach (op) ResolveOne(…)`
    //  —— 它**绕过** `ResolveOps` 里的 `for each` 重复（`TryComputeRepeats`）⇒ 正文的计数整条丢掉、
    //  永远只打 **1** 点（实测：手牌 5 张也只掉 1）。那条文件在本代理黑名单 ⇒ **只记不修**
    //  （已写进报告 §八，报给调度台）；本断言改走**棋盘单位**（`AtTurnUnitJobDeferred` → `ResolveOps`），
    //  重复生效、读得到手牌数。
    //
    //  ⚠️ 两条**前提**断言是硬的（从句形状 / 计数分类变了就红，不会变成假绿）。
    // ------------------------------------------------------------------
    static void TurnStartSeesDrawnCard()
    {
        var probe = UnitWithBody("B104D_ProbeUnit",
            "At the start of your turn, deal 1 damage to the enemy warlord for each card in your hand");

        // ---- 前提（硬）：这个形状被认成 `turn_start` + `you` 的 at-turn 从句 ----
        var clauses = EffectText.AtTurnClauses(probe.Desc);
        string body = null;
        for (int i = 0; i < clauses.Count; i++)
            if (clauses[i].Phase == "turn_start" && clauses[i].View == "you") { body = clauses[i].Body; break; }
        Tr(body != null, "（前提）`AtTurnClauses` 认出 `turn_start` + `you` 的从句（共 " + clauses.Count + " 条）"
            + " —— 认不出的话下面断的就不是相位");
        var ops = body == null ? null : EffectText.Parse(body, out _, out _);
        Tr(ops != null && ops.Count > 0 && ops[0].CountScope == "hand",
           "（前提）正文里的 `for each card in your hand` 解析成 **`CountScope = hand`**"
         + "（实得 " + (ops == null || ops.Count == 0 ? "(空表)" : (ops[0].CountScope ?? "null")) + "）");
        if (body == null || ops == null || ops.Count == 0 || ops[0].CountScope != "hand") return;

        var ctx = NewCtx();
        PlaceUnit(ctx, 0, 3, probe);
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;

        var foe = ctx.Players[1].Warlord;
        Tr(foe != null, "（前提）对手督军存在（`Players[1].Warlord`）");
        if (foe == null) return;
        int hp0 = foe.Health;
        int handBefore = ctx.Players[0].Hand.Count;

        RuleCore.BeginTurn(ctx);
        C(foe.Health, hp0, "★ D1-9 推迟那一半没跑 ⇒ `turn_start` 触发**也还没结算**（敌方督军一点没掉）"
            + "｜🧨 把 `ResolveAtTurn(\"turn_start\")` 留在 pre-wait ⇒ 它已经打了 ⇒ 红");
        C(ctx.Players[0].Hand.Count, handBefore, "（前提）此刻手牌还没变");

        RuleCore.ResolvePendingTurnDraw(ctx);
        int handAfter = ctx.Players[0].Hand.Count;
        C(handAfter, handBefore + 1, "（前提）补抽让手牌 +1");
        C(foe.Health, hp0 - handAfter,
          "★★ D1-10 `turn_start` 触发器读到的手牌 = **抽完之后**的张数（敌方督军掉 "
          + (hp0 - foe.Health) + " 点 = 手牌 " + handAfter + " 张）"
          + "｜🧨 把 `turn_start` 那一段搬回 pre-wait（抽牌之前结算）⇒ 只掉 " + handBefore + " 点 ⇒ 红");
    }

    // ------------------------------------------------------------------
    //  ③b D1-14：**手牌陷阱**那条通道也要按 `for each … in your hand` 重复
    //  （2026-10-31 修的真缺陷，与上面 D1-9/10 的【棋盘】通道互不替代）
    //
    //  **改前**：`AtTurnHandJobDeferred` 是 `foreach (var o in j.Ops) ResolveOne(...)`，
    //  它**绕过**了 `ResolveOps` 的 `for each` 重复层（`TryComputeRepeats`）⇒ 正文里的计数整条丢掉、
    //  永远只结算 **1** 遍（实测：手牌 5 张的 `deal 1 damage … for each card in your hand` 只掉 1 点）。
    //  **改后**：与棋盘那趟同形 —— `ResolveOps(ctx, j.Owner, null, j.Ops, label, sourceCard: j.Card)`。
    //  判据 = 原版 `AbilityLogic.PlayAbility.c:181-236`（`local_178` 是这条 logic 的次数/倍数，
    //  `:729` 是循环上界）对所有 ability 一视同仁 —— 手牌陷阱与棋盘触发在那一层没有分叉。
    // ------------------------------------------------------------------
    static void HandTrapRepeatsPerCard()
    {
        var trap = Tactic("B104D_Trap", "At the start of your turn, deal 1 damage to the enemy warlord for each card in your hand");

        var at = EffectText.SplitAtTurn(trap.Desc);
        Tr(at != null && at[0] == "turn_start",
           "（前提）`SplitAtTurn` 把这张陷阱认成**单句前缀式 turn_start**"
           + "（实得 " + (at == null ? "(不认)" : at[0]) + "）");
        var ops = at == null ? null : EffectText.Parse(at[1], out _, out _);
        Tr(ops != null && ops.Count > 0 && ops[0].CountScope == "hand",
           "（前提）正文解析出 `CountScope = hand`（实得 "
           + (ops == null || ops.Count == 0 ? "(空表)" : (ops[0].CountScope ?? "null")) + "）");
        if (at == null || ops == null || ops.Count == 0 || ops[0].CountScope != "hand") return;

        var ctx = NewCtx();
        ctx.Players[0].Hand.Add(new CardInstance(trap, 940001));
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;

        var foe = ctx.Players[1].Warlord;
        Tr(foe != null, "（前提）对手督军存在（`Players[1].Warlord`）");
        if (foe == null) return;
        int hp0 = foe.Health;
        int handBefore = ctx.Players[0].Hand.Count;   // 起手 3 + 这张陷阱 = 4

        RuleCore.BeginTurn(ctx);
        C(foe.Health, hp0, "（前提）推迟那一半没跑 ⇒ 手牌陷阱的 at-turn 也还没结算");
        RuleCore.ResolvePendingTurnDraw(ctx);
        int handAfter = ctx.Players[0].Hand.Count;
        C(handAfter, handBefore + 1, "（前提）补抽让手牌 +1");

        C(foe.Health, hp0 - handAfter,
          "★★ D1-14 **手牌陷阱**的 `for each card in your hand` 按**张数**重复（敌方督军掉 "
          + (hp0 - foe.Health) + " 点 = 手牌 " + handAfter + " 张）"
          + "｜🧨 把 `AtTurnHandJobDeferred` 改回 `foreach (op) ResolveOne(...)`（绕过 `TryComputeRepeats`）"
          + " ⇒ 只掉 1 点 ⇒ 红");
    }

    // ------------------------------------------------------------------
    //  ③c D1-15：**常驻效果**（`PersistentEffects`）那条通道也要按 `for each` 重复
    //  （与 D1-10【棋盘】/ D1-14【手牌陷阱】是**同一个洞的第三面**，2026-10-31 一并修）
    //
    //  改前：`AtTurnPersistJobDeferred` 同样是 `foreach (var o in j.Ops) ResolveOne(...)`
    //  ⇒ 常驻效果正文里的 `for each …` 整条丢掉、永远只结算 1 遍。
    //  改后：`ResolveOps(ctx, j.Owner, null, j.Ops, label, sourceCard: j.Card)`（三条通道同一形状）。
    //  判据同上（`AbilityLogic.PlayAbility.c:181-236` 对所有 ability 一视同仁）。
    // ------------------------------------------------------------------
    static void PersistentRepeatsPerCard()
    {
        const string body = "deal 1 damage to the enemy warlord for each card in your hand";
        var ops = EffectText.Parse(body, out _, out _);
        Tr(ops != null && ops.Count > 0 && ops[0].CountScope == "hand",
           "（前提）常驻效果的正文解析出 `CountScope = hand`（实得 "
           + (ops == null || ops.Count == 0 ? "(空表)" : (ops[0].CountScope ?? "null")) + "）");
        if (ops == null || ops.Count == 0 || ops[0].CountScope != "hand") return;

        var ctx = NewCtx();
        // 常驻效果这一摞是**我们自设计的替代结构**（原版没有这个类型，见 `PersistentEffect` 的 doc）
        // ⇒ 夹具直接往表里塞一条：`Owner` = 本回合方、`Trigger = "turn"`、`Phase = "turn_start"`。
        ctx.PersistentEffects.Add(new PersistentEffect
        {
            Owner = 0,
            Source = Tactic("B104D_PersistSrc", ""),   // 只作「能力属主 = 写出这条效果的那张卡」
            Phase = "turn_start",
            Trigger = "turn",
            Ops = ops,
            Body = body,
        });
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;

        var foe = ctx.Players[1].Warlord;
        Tr(foe != null, "（前提）对手督军存在（`Players[1].Warlord`）");
        if (foe == null) return;
        int hp0 = foe.Health;
        int handBefore = ctx.Players[0].Hand.Count;   // 起手 3

        RuleCore.BeginTurn(ctx);
        C(foe.Health, hp0, "（前提）推迟那一半没跑 ⇒ 常驻效果的 at-turn 也还没结算");
        RuleCore.ResolvePendingTurnDraw(ctx);
        int handAfter = ctx.Players[0].Hand.Count;
        C(handAfter, handBefore + 1, "（前提）补抽让手牌 +1");

        C(foe.Health, hp0 - handAfter,
          "★★ D1-15 **常驻效果**的 `for each card in your hand` 按**张数**重复（敌方督军掉 "
          + (hp0 - foe.Health) + " 点 = 手牌 " + handAfter + " 张）"
          + "｜🧨 把 `AtTurnPersistJobDeferred` 改回 `foreach (op) ResolveOne(...)` ⇒ 只掉 1 点 ⇒ 红");
    }

    // ------------------------------------------------------------------
    //  ③d D1-16：**部署触发常驻**那条通道 —— 每条 op 之前要**重种 `LastTargets`**
    //  （2026-10-31 同族全扫的第四面；`TAU42 Experimental Drones` 等真卡走这条）
    //
    //  夹具：一条 `Trigger = "deploy"` 的常驻效果，正文两句：
    //    ① `Give +1 Attack to an enemy troop`（目标 = 敌方那个单位）
    //    ② `Give +1 Health to it`（`it` = **刚部署的那个** —— 靠每条 op 之前重种）
    //  ⇒ 期望：敌方 +1 攻、**刚部署的那个 +1 血**、敌方血**不变**。
    //  ⚠️ 改前这一段是 `foreach (op) ResolveOne(...)` + 手工重种；改成 `ResolveOps` 之后靠
    //     新加的可选形参 `reseedPerOp: true` 保住老契约（缺省 false ⇒ 其它调用点零变化）。
    // ------------------------------------------------------------------
    static void DeployReseedsPerOp()
    {
        const string body = "Give +1 Attack to an enemy troop. Give +1 Health to it";
        var ops = EffectText.Parse(body, out _, out _);
        Tr(ops != null && ops.Count >= 2,
           "（前提）部署触发的正文解析出 **2 条 op**（实得 " + (ops == null ? 0 : ops.Count) + "）");
        if (ops == null || ops.Count < 2) return;

        var ctx = NewCtx();
        var foe = PlaceUnit(ctx, 1, 3, Unit("B104D_DeployFoe"));    // op① 的目标
        var dep = PlaceUnit(ctx, 0, 3, Unit("B104D_DeploySelf"));   // 「刚部署的那个」= seed
        ctx.PersistentEffects.Add(new PersistentEffect
        {
            Owner = 0,
            Source = Tactic("B104D_DeploySrc", ""),
            Phase = "deploy",
            Trigger = "deploy",
            Ops = ops,
            Body = body,
        });
        int eAtk0 = foe.Attack, eHp0 = foe.Health, dHp0 = dep.Health;

        using (RuleCore.BeginAction(ctx)) RuleCore.ResolveDeploy(ctx, 0, dep);

        C(foe.Attack, eAtk0 + 1, "（前提）op① 生效：敌方那个单位 +1 攻");
        C(dep.Health, dHp0 + 1,
          "★★ D1-16 第二条 op 的 `it` = **刚部署的那个**（每条 op 之前重种 `LastTargets`）"
          + "｜🧨 去掉 `reseedPerOp: true` ⇒ `it` 落到 op① 的目标上（敌方 +1 血）⇒ 本条红");
        C(foe.Health, eHp0, "D1-16b 敌方血量**不变**（`it` 没有串到第一条的目标上）"
          + "｜🧨 同上 ⇒ 变 +1 ⇒ 红");
    }

    // ------------------------------------------------------------------
    //  ③e D1-17：**`Repeat this effect` 重复一遍不再次收费**（`A1415` 的契约）
    //  （2026-10-31 同族全扫：`DoRepeat` 改走 `ResolveOps` 时必须先整批包 `Prepaid`）
    //
    //  夹具：正文 `Deal 1 damage to an enemy. Repeat this effect` 解析出来两条 op，
    //  再把付费前缀贴到**重复那一条**（`RepeatOps[0]`）上（实测它与前一条是**同一个对象**，
    //  所以外层也会按组收一次）。
    //  ⇒ 期望：能量 10 → **8**（只收一次 2）、敌方 5 → **3**（`deal 1` 打两遍）。
    //  ⚠️ 不包 `Prepaid` 就会被嵌套的 `ChargePaidGroups` **再收一遍**（能量 → 6）。
    // ------------------------------------------------------------------
    static void RepeatChargesOnce()
    {
        var ops = EffectText.Parse("Deal 1 damage to an enemy. Repeat this effect", out _, out _);
        bool shaped = ops != null && ops.Count >= 2 && ops[1].Verb == "repeat"
                      && ops[1].RepeatOps != null && ops[1].RepeatOps.Count == 1;
        Tr(shaped, "（前提）解析出 `deal` + `repeat`（`RepeatOps` 恰 1 条）"
                 + "（实得 " + (ops == null ? 0 : ops.Count) + " 条）");
        if (!shaped) return;

        var inner = ops[1].RepeatOps[0];
        inner.Cost = 2;            // 手动把「付费前缀」贴到**重复的那条** op 上（= `2 Energy:` 那一份）
        inner.CostKind = "";
        Tr(inner.Cost > 0 && !inner.CostShared,
           "（前提）重复那条 op 带着 `Cost = 2` 且**未标 already-paid** ⇒ 下面两条才有判别力");

        var ctx = NewCtx();
        RuleCore.BeginTurn(ctx);
        var foe = PlaceUnit(ctx, 1, 3, UnitWithHealth("B104D_RepFoe", 5));   // ⚠️ `Unit()` 是 1 血；这条要「打两遍还活着」⇒ 显式 5 血
        ctx.Players[0].Energy = 10;
        int hp0 = foe.Health;

        using (RuleCore.BeginAction(ctx)) RuleCore.ResolveOps(ctx, 0, null, ops, "D1-17 探针");

        C(ctx.Players[0].Energy, 8,
          "★★ D1-17 重复那条 op 的付费前缀**只收一次**（能量 10 → 8）"
          + "｜🧨 `DoRepeat` 不包 `Prepaid` ⇒ 嵌套 `ResolveOps` 再收一遍（→ 6）⇒ 红");
        C(foe.Health, hp0 - 2, "D1-17b `Repeat this effect` 的**次数**正确（`deal 1` 打两遍，5 → 3）");
    }

    // ------------------------------------------------------------------
    //  ④ 教程局第 1 回合不推迟（原版 `_NextTurn:451-475` 整段 goto 跳过），第 2 个自己的回合照样推迟
    // ------------------------------------------------------------------
    static void TutorialFirstTurn()
    {
        var ctx = NewCtx();
        ctx.PlayMode = GameMode.Tutorial;
        C(ctx.MatchType, MatchType.Tutorial, "（前提）`PlayMode = Tutorial` ⇒ `MatchType = 100`");
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;
        int h0 = ctx.Players[0].Hand.Count;

        RuleCore.BeginTurn(ctx);
        C(ctx.Turn, 1, "（前提）这是全局第 1 回合");
        C(ctx.Players[0].Hand.Count, h0 + 1, "★ D1-11 **教程局第 1 回合不推迟** ⇒ 当场抽 1 张"
            + "（原版 `_NextTurn:451-475` 那条 `IsTutorialMatch ∧ turnCounter == 1` 整段 goto 跳过那段等待）"
            + "｜🧨 去掉「教程第 1 回合」那道闸 ⇒ 本回合手牌不变 ⇒ 红");
        Tr(!ctx.PendingTurnDraw, "D1-11b 教程第 1 回合不挂 `PendingTurnDraw`");

        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);   // → P2 的回合（不是本地座位 ⇒ 立即）
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);   // → P1 第 2 个自己的回合（全局第 3 回合）
        C(ctx.Active, 0, "（前提）又轮到座位 0");
        C(ctx.Turn, 3, "（前提）全局第 3 回合");
        Tr(ctx.PendingTurnDraw, "★ D1-12 教程局**第 2 个**自己的回合照样推迟（原版只跳第 1 回合）"
            + "｜🧨 把那条闸写成「教程局永不推迟」⇒ 红");
    }

    // ------------------------------------------------------------------
    //  ⑤ 回放展开入口：即使开关开着也**不**推迟（`evenIfOver` 那条路）
    // ------------------------------------------------------------------
    static void ReplayExpansion()
    {
        var ctx = NewCtx();
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;
        int h0 = ctx.Players[0].Hand.Count;
        RuleCore.BeginTurnForReplayExpansion(ctx);
        C(ctx.Players[0].Hand.Count, h0 + 1, "★ D1-13 `BeginTurnForReplayExpansion`（回放展开）**永不推迟** —— 回放要的是"
            + "「录制时那一拍逐字重现」｜🧨 让它也走推迟分支 ⇒ 手牌不变 ⇒ 红");
        Tr(!ctx.PendingTurnDraw, "D1-13b 回放展开不挂 `PendingTurnDraw`");
    }

    // ------------------------------------------------------------------
    //  ⑥ D2 的常量锚：那 1.5 s **只有一处**（`BattleDriver.TurnHeadsUpHold`）
    // ------------------------------------------------------------------
    static void AnchorConstants()
    {
        C(BattleDriver.TurnHeadsUpHold, 1.5f,
          "D2-1 抬头之后那一拍 = **1.5 s**（`DAT_1834b3090` 实读；`HoldForTurnHeadsUp` 的 `ExtendTimelineTail` /"
        + " `BeginBlockingWindow` / 补抽排期**三处共用这一个常量**）｜🧨 三处各写一个字面量 ⇒ 迟早分叉");
    }

    // ================================================================
    //  夹具（与 `RuleEngineTest` 那套同形：真实 `NewBattle` + `shuffle: false` + 倒序放起手）
    // ================================================================
    static CardDef Unit(string id)
        => new CardDef(id, id, "unit", "", null, "Test", 1, 0, 1, 0, null);

    /// <summary>带**指定血量**的单位（`Unit()` 固定 1 血；`D1-17` 要「被 1 点伤害打两遍还活着」⇒ 显式给血）。</summary>
    static CardDef UnitWithHealth(string id, int hp)
        => new CardDef(id, id, "unit", "", null, "Test", 1, 0, hp, 0, null);

    /// <summary>带卡面正文的**战术卡**（手牌陷阱那一档：`At the start of your turn, …` 整句）。</summary>
    static CardDef Tactic(string id, string desc)
        => new CardDef(id, id, "tactic", desc, "common", "Test", 0, 0, 0, 0, null);

    /// <summary>带**卡面正文**的单位（`turn_start` 那一族夹具用）—— 正文由 `AtTurnClauses` 解析。</summary>
    static CardDef UnitWithBody(string id, string desc)
        => new CardDef(id, id, "unit", desc, "common", "Test", 1, 0, 5, 0, null);

    /// <summary>把一张卡摆上场（形状照 `RuleEngineTest.Place` 的夹具口径：走对局计数器发实例号、
    /// 记部署回合、取进场序号 ⇒ 「按进场序排序」不会退化成原顺序）。</summary>
    static UnitState PlaceUnit(BattleContext ctx, int seat, int slot, CardDef def)
    {
        var u = new UnitState(ctx.NewInstance(def), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[seat].Board[slot] = u;
        return u;
    }

    static CardDef Hero()
        => new CardDef("B104D_Warlord", "B104D_Warlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                       subtype: "Warlord");

    /// <summary>两边各一副纯 filler（起手 3 + 20 张后备）；`cardPool` 传空表 —— 本件一条 `create` 都不走。
    /// ⚠️ 建完之后 `Turn = 0` / `Active = FirstSeat` / 双方手牌 = 起手那几张（还没抽回合初那张）。</summary>
    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d0.Add(Unit("filler" + i));
        var d1 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d1.Add(Unit("foefiller" + i));
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }
}
