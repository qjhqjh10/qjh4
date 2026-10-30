// B104_W64_Assert.cs — 执行代理 `B104_W64`（第一百零四会话 · 2026-10-31）
//
// ============================ 它盯的是什么 ============================
//   施工单 → `资料/完全复刻/判据/B104_W64_施工单.md` §一 W1（B6-b）/ §二 W2（B6-a）。本代理落地的两件：
//
//   · **B6-b**：任务点**下降**那一拍发 `EvtKind.QuestSpent`（原版 `ManaTypeHolder.vfxOnValueDown`
//     = prefab **`Quest UI Trigger`**，guid `2223a4d2…`）。链路 =
//     `EffectResolver.QuestPointThreshold`（`ps.QuestPoints -= step` 之后）→ `ctx.Signals`
//     → `VfxMap.ByEvent[quest_spent]` → `BattleDriver.GenericVfxFor` / `IsResourceUiEvent`（HUD 锚点）。
//   · **B6-a**：`gain_spirit` 的那行映射**删掉**（原版 13 场 × 2 份 `SpiritStoneHolder` 共 26 份
//     `ManaTypeHolder` 实例全是 `useVfxOnValueChange = 0`、两个槽都是空串）并把它加进
//     `VfxMap.IsOriginalNoGenericEvent`（否则 `ResolveFallback` 会把「原版没有」喊成「漏配」）。
//   逐条判据（原版出处 / 26 份实例实读 / `Waystone UI Gain` 全库 0 引用）写在
//     `RuleEngine/Core/BattleEvent.cs` 的 `EvtKind.QuestSpent` doc ＋ `VfxMap` 里那两段注释。
//
// ============================ ⛔ 本件**故意**没有的断言 ============================
//   · **真机并排看 HUD 任务图标**（施工单 §一 验收第二句）—— 真机项，不在本件。
//   · **`PlaySignal` 真的把件播出来**（要 GameObject / `CardEffects` / 帧泵）—— 本件只到
//     「事件 → 特效名 → `IsHudAnchoredEvent`」这三跳；真播链由 `BattleScene` 既有那组断言覆盖。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   W2-1/W2-2 ← 删掉 `EffectResolver.QuestPointThreshold` 里那 7 行 `ctx.Signals.Add(...)` ⇒ 0 条 ⇒ 红。
//   W2-3      ← 把它改成「每次加任务点都发」（挪到 `DoFactionResource` 的 `ps.QuestPoints += n` 旁边）⇒ 红。
//   W3-1      ← 删掉 `VfxMap.ByEvent` 的 `{ QuestSpent, "Quest UI Trigger" }` ⇒ `Resolve` 回 null ⇒ 红。
//   W3-2      ← 把它改挂 `Quest UI Gain`（= 把「下降」冒充成「上升」那一件）⇒ 红。
//   W4-1      ← 删掉 `BattleDriver.GenericVfxFor` 的 `case EvtKind.QuestSpent` ⇒ 回 null ⇒ 红。
//   W4-2      ← 把 `QuestSpent` 从 `IsResourceUiEvent` 里去掉 ⇒ `IsHudAnchoredEvent` 回 false ⇒ 红
//               （那一档会让 `PlaySignal` 的 `if (slot < 0) return;` 把整条**静默**吃掉）。
//   W5-1/W5-2 ← 把它挪进 `TimedKinds` 却不补 `DurationOf` / `SourceOf` 的 `case`
//               ⇒ `DurationOf` 走 `ReportMissingCase` 出声；本件断「零时长 + 有出处」⇒ 红。
//   W6-1      ← 把 `{ GainSpirit, "Waystone UI Gain" }` 那行挂回去 ⇒ `Resolve(GainSpirit)` 非 null ⇒ 红。
//   W6-2      ← 把 `GainSpirit` 从 `IsOriginalNoGenericEvent` 移出 ⇒ 红（「原版没有」会被喊成「漏配」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主（`Editor/BattleScene.cs` 或 `Editor/RuleEngineTest.cs`）里加一行：
//     `B104_W64_Assert.Run(); _pass += B104_W64_Assert.Pass; _fail += B104_W64_Assert.Fail;`
//   本件**无副作用**：只建裸 `BattleContext`（不建 GameObject、不碰存档、不改 `VfxMap` 静态表）。
using System.Collections.Generic;
using CardPresentation;      // VfxMap / BattleDriver / EventTiming
using RuleEngine;            // RuleCore / CardDef / BattleContext / BattleEvent / EvtKind / RuleCodes
using UnityEngine;

/// <summary>执行代理 `B104_W64` 的断言：任务点下降 → `Quest UI Trigger`（B6-b）＋ `gain_spirit` 撤销映射（B6-a）。</summary>
public static class B104_W64_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B104W64] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B6-b 任务点下降 → `Quest UI Trigger` · B6-a `gain_spirit` 撤销映射");
        EventFromEngine();   // W1 前提 / W2
        MappingAndWire();    // W3 / W4 / W5
        GainSpiritRemoved(); // W6
    }

    // ------------------------------------------------------------------
    //  W2 · 引擎侧：真的扣了任务点 ⇒ **恰 1 条** `QuestSpent`（且字段对）
    // ------------------------------------------------------------------
    static void EventFromEngine()
    {
        // ---- 正例：`Gain 3 任务点`（经典档阈值 = 3）⇒ 加 3 → 满阈值 → 扣 3 ----
        var ctx = NewCtx(Tactic("T_B104Quest3", "Gain 3 任务点"));
        C(ctx.Active, 0, "（前提）现在是 P1 的回合（`Turn = " + ctx.Turn + "`）");
        int hand = HandIdx(ctx, 0, "T_B104Quest3");
        Tr(hand >= 0, "（前提）手牌里有 `T_B104Quest3`");
        C(Count(ctx, EvtKind.QuestSpent), 0, "（前提）开打前 0 条 `QuestSpent`");
        if (hand < 0) return;                       // 夹具没起 —— 下面的断言会全部变成假绿，直接返回

        C(RuleCore.PlayTactic(ctx, 0, hand, -1), RuleCodes.OK, "打出 `Gain 3 任务点`");
        C(ctx.Players[0].QuestPoints, 0,
          "（前提）满阈值后余数 = 0（+3 − 3；原版 `PlayerManager__AddQuestPoints.c:101-104`）");

        C(Count(ctx, EvtKind.QuestSpent), 1,
          "★ W2-1 **恰 1 条** `QuestSpent`（扣一个阈值 = 发一条；"
        + "🧨 删掉 `QuestPointThreshold` 里那 5 行 `ctx.Signals.Add` ⇒ 0 条 ⇒ 红）");
        var spent = Find(ctx, EvtKind.QuestSpent);
        Tr(spent != null, "★ W2-2 找得到那条 `QuestSpent`");
        if (spent != null)
        {
            C(spent.Player, 0, "W2-2a `Player` = 扣点那一方（0）");
            C(spent.Slot, -1, "W2-2b `Slot` 恒 -1（阵营资源是**玩家的**，同 `GainQuest` 的约定）");
            C(spent.Amount, 3, "W2-2c `Amount` = 被减掉的点数（= 经典档阈值 3，不是「当前余数」0）");
        }

        // ---- 反例：**没到阈值** ⇒ 一条都不发（不是「每次加任务点都发」）----
        var ctx2 = NewCtx(Tactic("T_B104Quest1a", "Gain 1 任务点"),
                          Tactic("T_B104Quest1b", "Gain 1 任务点"));
        C(ctx2.Active, 0, "（前提 ②）现在是 P1 的回合");
        int h2a = HandIdx(ctx2, 0, "T_B104Quest1a");
        int h2b = HandIdx(ctx2, 0, "T_B104Quest1b");
        Tr(h2a >= 0 && h2b >= 0, "（前提 ②）手牌里有两张 `Gain 1 任务点`");
        if (h2a >= 0 && h2b >= 0)
        {
            RuleCore.PlayTactic(ctx2, 0, h2a, -1);
            // ⚠️ 第二张的下标会变（第一张从手牌里移走了）⇒ 重新找
            h2b = HandIdx(ctx2, 0, "T_B104Quest1b");
            if (h2b >= 0) RuleCore.PlayTactic(ctx2, 0, h2b, -1);
            C(ctx2.Players[0].QuestPoints, 2,
              "（前提 ②）只加 2 点（阈值 3 没到 ⇒ 原版这时**不扣**）");
            C(Count(ctx2, EvtKind.QuestSpent), 0,
              "★ W2-3 没到阈值 ⇒ **0 条** `QuestSpent`"
            + "（🧨 把发事件挪到 `DoFactionResource` 的 `ps.QuestPoints += n` 旁边（每次加都发）⇒ 这里会变 2 ⇒ 红）");
        }
    }

    // ------------------------------------------------------------------
    //  W3 / W4 / W5 · 映射 + 播放接线 + 事件登记（三个静态表，纯函数层）
    // ------------------------------------------------------------------
    static void MappingAndWire()
    {
        C(VfxMap.Resolve(VfxMap.QuestSpent), "Quest UI Trigger",
          "★ W3-1 任务点下降 ⇒ **`Quest UI Trigger`**（原版 `vfxOnValueDown`，26 份实例实读）"
        + "｜🧨 删掉 `ByEvent` 那行 ⇒ 回 null ⇒ 红");
        Tr(VfxMap.Resolve(VfxMap.QuestSpent) != VfxMap.Resolve(VfxMap.GainQuest),
          "★ W3-2 它和 `GainQuest`（`Quest UI Gain`）**不是同一件** —— 原版是两个方向的**两个槽**"
        + "｜🧨 把它改挂 `Quest UI Gain` ⇒ 本条与 W3-1 同时红");
        Tr(System.Array.IndexOf(VfxMap.Events, VfxMap.QuestSpent) >= 0,
          "W3-3 `VfxMap.Events` 里有 `quest_spent`（否则 `Unmapped()` / `Describe()` 覆盖不到它）");
        bool inUnmapped = false;
        foreach (var ev in VfxMap.Unmapped()) if (ev == VfxMap.QuestSpent) { inUnmapped = true; break; }
        Tr(!inUnmapped, "W3-4 它**不在** `Unmapped()` 里（= `ByEvent` 那行在，且没落进两张「空表」）");

        C(BattleDriver.GenericVfxFor(new BattleEvent { Kind = EvtKind.QuestSpent }), VfxMap.QuestSpent,
          "★ W4-1 `BattleDriver.GenericVfxFor(QuestSpent)` = `quest_spent`"
        + "｜🧨 删掉那个 `case` ⇒ 回 null ⇒ 红（删了 case 就没有任何生产消费点）");
        Tr(BattleDriver.IsHudAnchoredEvent(EvtKind.QuestSpent),
          "★ W4-2 `IsHudAnchoredEvent(QuestSpent)` = 真 —— 否则 `PlaySignal` 的 `slot < 0 → return` 会把整条"
        + "**静默**吃掉（2026-09-15 那四个资源事件同一个形状）"
        + "｜🧨 把它从 `IsResourceUiEvent` 里去掉 ⇒ 红");

        Tr(EventTiming.IsZeroDurationKind(EvtKind.QuestSpent) && !EventTiming.IsTimedKind(EvtKind.QuestSpent),
          "★ W5-1 登记成**零时长**（原版 `ManaTypeHolder.TryDOVfx` 是 `Instantiate` 之后立刻返回、0 处 `yield`）"
        + "｜🧨 挪进 `TimedKinds` 却不补 `DurationOf`/`SourceOf` 的 `case` ⇒ W5-2 红 + 跑一次出声");
        C(EventTiming.DurationOf(EvtKind.QuestSpent), 0f,
          "★ W5-2 `DurationOf(QuestSpent)` = 0（不占时间线 —— ⛔ 别按 `Quest UI Trigger` 自己演多久去排，"
        + "那会把下一条事件推后一段原版不存在的等待）");
        string why;
        Tr(EventTiming.IsSourced(EvtKind.QuestSpent, out why),
          "★ W5-3 `IsSourced(QuestSpent)` = 真（`BattleScene` 的 `unsourced == 2` 不会因它红）"
        + (string.IsNullOrEmpty(why) ? "" : "（原因：" + why + "）"));
        Tr(EventTiming.IsRegisteredEventKind(EvtKind.QuestSpent),
          "W5-4 它在登记表里（`ZeroDurationKinds` 或 `TimedKinds`）—— `A2·J2③` 那条「每一档都登记过」的闸");
    }

    // ------------------------------------------------------------------
    //  W6 · B6-a：`gain_spirit` 的映射**必须**是空的（原版没有这个 UI 槽）
    // ------------------------------------------------------------------
    static void GainSpiritRemoved()
    {
        Tr(VfxMap.Resolve(VfxMap.GainSpirit) == null,
          "★ W6-1 `Resolve(gain_spirit)` = null —— 原版 13 场 26 份 `SpiritStoneHolder` 全是 "
        + "`useVfxOnValueChange = 0`、`vfxOnValueUp/Down` 空串；`Waystone UI Gain` 那件 prefab 全库 **0 引用**"
        + "｜🧨 把 `{ GainSpirit, \"Waystone UI Gain\" }` 挂回去 ⇒ 红");
        Tr(VfxMap.IsOriginalNoGenericEvent(VfxMap.GainSpirit),
          "★ W6-2 `IsOriginalNoGenericEvent(gain_spirit)` = 真 —— 否则 `ResolveFallback` 会走 "
        + "`WarnNoFactionVariant`，把「原版本来就没有」喊成「漏配」（假信号）"
        + "｜🧨 把它从那张表里移出 ⇒ 红");
        Tr(!VfxMap.IsKnownGapEvent(VfxMap.GainSpirit),
          "W6-3 它**不是**「已知缺口」—— 已知缺口要**出声**，而这一档原版就是空的（不该出声）");
        Tr(!VfxMap.ResolvesForSomeFaction(VfxMap.GainSpirit),
          "W6-4 任何阵营都解析不出（`ByFaction` 里也没有它的行）");
        Tr(VfxMap.QuestSpent != VfxMap.GainSpirit,
          "W6-5 「下降」与「灵魂石」是两个不同的键（⛔ 别把 `QuestSpent` 当成 `GainSpirit` 的第二个出口）");
    }

    // ================================================================
    //  夹具
    //  ⚠️ 与 `RuleEngineTest.QuestFixture` 同一条形状（真实 `NewBattle` + `shuffle: false` + 倒序放起手），
    //     但本文件是**独立静态类** ⇒ 夹具自己写一份（拿不到 `RuleEngineTest` 里 `private` 的那些）。
    // ================================================================
    static CardDef Tactic(string id, string desc)
        => new CardDef(id, id, "tactic", desc, "common", "Test", 0, 0, 0, 0, null);

    static CardDef Unit(string id)
        => new CardDef(id, id, "unit", "", null, "Test", 1, 0, 1, 0, null);

    static CardDef Hero()
        => new CardDef("B104W64_Warlord", "B104W64_Warlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                       subtype: "Warlord");

    /// <summary>P0 起手 = `hand`（`NewBattle` 抽牌是 `pop_back` ⇒ 倒序放）；P1 一副纯 filler。
    /// ⚠️ `cardPool` 传空表：满阈值后造隐秘那一步会找不到卡并**如实出声**（那不影响本件要验的事件）。</summary>
    static BattleContext NewCtx(params CardDef[] hand)
    {
        var d0 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d0.Add(Unit("filler" + i));
        for (int i = hand.Length - 1; i >= 0; i--) d0.Add(hand[i]);
        var d1 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d1.Add(Unit("foefiller" + i));
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 12;
        return ctx;
    }

    /// <summary>推进到「P1 的第 n 个回合」（与 `RuleEngineTest.ToP1Turn` 同构）。</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Tr(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i] != null && hand[i].Card != null && hand[i].Card.Name == name) return i;
        return -1;
    }

    static int Count(BattleContext ctx, EvtKind k)
    {
        int n = 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i] != null && ctx.Signals[i].Kind == k) n++;
        return n;
    }

    static BattleEvent Find(BattleContext ctx, EvtKind k)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i] != null && ctx.Signals[i].Kind == k) return ctx.Signals[i];
        return null;
    }
}
