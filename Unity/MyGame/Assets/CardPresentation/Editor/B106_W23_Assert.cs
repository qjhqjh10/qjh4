// B106_W23_Assert.cs — 执行代理 `B106_W23`（第一百零六会话 · 临时卡「回合末次序」）
//
// ============================ 它盯的是什么（判据只写在这里一次）============================
//  **临时卡（`ephemeral`）的「回合末触发」与「移出手牌」到底谁先**。
//
//  原版判据（第一权威，现读 `d:/2/tools/decomp_full/CardScript__OnTurnEnd.c`）——**同一个逐卡方法体**：
//    · `:64` `RawCardScript__OnTrigger(…, 0x2b = 43 Courtesy)` →
//      `:72` `RawCardScript__OnTrigger(…, 0x28 = 40 TurnEnd)` →
//      `:80` `RawCardScript__OnTrigger(…, 0x2d = 45 Resolution)`（后两条只给「本回合行动方」那一侧）；
//    · 之后：`:84-108` 再生 → `:110-113` 毒 → `:117-123` 残骸 → `:124-134` 眩晕/失明闸门 →
//      `:135-139` 标记光；
//    · **最后** `:141 cVar11 = EntityScript__HasCurrentTrait(param_1, 5 /* ephemeral */)`
//      且 `param_2 == card.isPlayer` 且 `card + 0x228 ∈ {1,4,8,9,10,12}`（全是手牌态）
//      ⇒ `:147 BattleManager__AddDisappearFromHand(...)`。
//  ⇒ **先触发、后移出**。
//
//  改前我们：`RuleCore.EndTurnCore` 里 `SweepEphemeral(ctx, ctx.Active)` 排在
//    `EmitTurnEndCourtesy(ctx); ResolveAtTurn(ctx, "turn_end"); EmitTurnEndResolution(ctx);` **之前**
//    ⇒ 手牌里的临时卡**看不到自己那一下回合末触发**（它已经被移出手牌了）⇒ 次序相反。
//  本笔把那**一句**挪到「闸门摘除」之后（= 原版 `:141` 的位置）、`ctx.IsOver` 早退之前。
//
// ============================ 三段（A 前提 / B 核心 / C 控制组）============================
//  A 前提：卡面形状五样都成立（硬检查，⛔ 不是 `if (前提) Check(结论)` —— 那样前提不成立会**静默**）。
//  B 核心：**同一张卡**（`Ephemeral` ＋ `At the end of your turn, …`）在行动方手牌里走一次 `EndTurn`：
//    ① `EvtKind.Trigger`（`Keyword = "TurnEnd"` · `Effect = "turn_end"`）**出现**
//       —— 它只在 `EffectResolver.ResolveAtTurn` 的**手牌快照**里建出 job 才会发
//       ⇒ **出现 = 触发那一刻卡还在手牌**；
//    ② `EvtKind.Return`（`Effect = "ephemeral"`）**出现**（`RuleCore.RemoveCardFromHandToRemoved` 那一跳）；
//    ③ **次序** `triggerIdx < removeIdx`；
//    ④ 去向 = `ctx.Removed`（移出游戏）、手牌里不再有它、日志里那一段**真的开跑**。
//  C 控制组：**同一段 desc、去掉 `Ephemeral` 关键词** ⇒ ① 照样出现、② 不出现、卡留在手里。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · **B1（核心判别式）**：把 `SweepEphemeral` 挪回 `ResolveAtTurn("turn_end")` **之前**（= 改前的次序）
//    ⇒ 手牌快照里已经没有这张卡 ⇒ **一条 `Trigger` 事件都不会发** ⇒ `triggerIdx = -1` ⇒ **B1 红**；
//    而 **C1 仍然绿**（控制组那张不带 ephemeral，不受清扫影响）⇒ 一眼看出「不是事件的管线坏了，是次序反了」。
//    这两条**结构上不可能同时满足**（一个要求「ephemeral 那张有 Trigger」、一个要求「有 Trigger 与 ephemeral 无关」）。
//  · **B3（次序判别式）**：改成「先快照手牌 → 再移出 → 再用快照发触发」⇒ B1 绿（事件发了）而 **B3 红**（序反了）。
//    ⛔ 只断「最后卡没了」是不够的 —— 那样改回旧次序照样绿。
//  · **B2**：把去向从「移出游戏」改成弃牌堆（`AddToCemetery`）⇒ 没有 `Effect == "ephemeral"` 的 `Return` ⇒ B2 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/BattleScene.cs` 的 **`Run()` 方法尾聚合行**
//  （那一行以 `Debug.Log(P + $"=== 合计：` 结尾、后面才是 `//` 注释）——
//  在 `Debug.Log(P + $"=== 合计` **之前**、**同一物理行**追加（行数零变化）：
//      B106_W23_Assert.Run(); pass += B106_W23_Assert.Pass; fail += B106_W23_Assert.Fail;
//  ⚠️ 追加前先看那一行**第一个 `//` 在哪**：代码必须落在它**之前**
//     （2026-10-30 一天踩 6 次「代码落进行末注释、`typecheck 0/0` 全绿、断言一条没跑」）。
//  ⚠️ 本件**不建 GameObject / 不碰 `BattleDriver` / 不倚赖宿主立好的画布**：只造内存里的 `BattleContext`。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B106_W23`：钉**原版 `CardScript__OnTurnEnd` 的次序** —— 临时卡先吃 `turn_end`
/// 触发、**之后**才被移出手牌（原版 `.c:64/72/80` → `:141/:147`）。</summary>
public static class B106_W23_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B106W23] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // ================================================================== 夹具
    const string EphName = "B106W23_EphemeralTrap";
    const string CtrlName = "B106W23_ControlTrap";

    // 逐字照真卡 `Poisoned Supplies`（`a7bb23355a30d344e9d2decf976d91e1`，现读 `cards_engine.json`
    // 的 `desc`）—— 这样 `EffectText.IsHandTrap` / `SplitAtTurn` / `Parse` 走的是
    // `RuleEngineTest_EngineA3.A3Case1AtTurnBodyCriteriaAttached` 已经钉过的那条路（1 条 `deal`）。
    const string TrapDesc = "At the end of your turn, your troops take 1 damage";

    /// <summary>督军（`subtype` 必须给 `"Warlord"` —— 与 `B99C1_Assert.Hero` / `B100F26_Assert.Hero` 同口径）。</summary>
    static CardDef Warlord(string id)
    {
        return new CardDef(id, id, "hero", "", "common", "Test", 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>牌库填充（只为不空牌库；⛔ 不参与任何断言）。</summary>
    static CardDef Filler(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 0, 1, 0, (string[])null);
    }

    /// <summary>合成的手牌陷阱（非单位卡 + `At the end of your turn, …`）。
    /// `abilityTriggers: {40}` = 原版 `TurnEnd`（`CardDef.AbilityTriggerNumberOfName` 的 `{"TurnEnd", 40}`）
    /// —— 手牌那一趟的 `EvtKind.Trigger` 走 `HasTriggerSource` 的「ability presence」那道闸
    /// （`CardAbility__CanTriggerAbility.c:46-48`），没有这一列就**不会发事件**。
    /// ⚠️ `abilityTriggersVersioned: true` 必须给（`AbilityTriggerPresence(int)` 没有它恒回 `-1` = 未知）。
    /// 🔶 自己设计（非原版）：**合成卡名/阵营**（`B106W23_*` / `Test`）—— 本件只测次序，不需要真卡 ·
    /// 原版没有这两张卡，判据不受影响 · 调度台 2026-10-31 裁定（本件夹具，不进产品数据）。</summary>
    static CardDef HandTrap(string id, bool ephemeral)
    {
        return new CardDef(
            id, id, "tactic", TrapDesc, "common", "Test", 1, 0, 0, 0,
            ephemeral ? new[] { "Ephemeral" } : new string[0],
            abilityTriggers: new[] { 40 },
            abilityTriggersVersioned: true);
    }

    /// <summary>造一局「第 1 回合 · 行动方 = 座位 0」的对局（牌序固定 ⇒ 结果可复现）。
    /// ⚠️ 与 `B100F26_Assert.Battle` / `B99C1_Assert` 的 C 段同一套推进法。</summary>
    static BattleContext NewBattle()
    {
        var d0 = new List<CardDef> { Warlord("B106W23_W0") };
        var d1 = new List<CardDef> { Warlord("B106W23_W1") };
        for (int i = 0; i < 20; i++)
        {
            d0.Add(Filler("B106W23_a" + i));
            d1.Add(Filler("B106W23_b" + i));
        }
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        // 本件靠 `ctx.ActionLog` 的**下标次序**判先后 ⇒ 别让它按 400 条截断（默认 `LogCapacity = 400`）。
        ctx.LogCapacity = 4000;
        int guard = 0;
        while (ctx.Turn != 1 || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) return null;
        }
        return ctx;
    }

    /// <summary>`ctx.ActionLog` 里从 <paramref name="from"/> 起的第一条命中事件的下标（`-1` = 没有）。</summary>
    static int FindEvent(BattleContext ctx, int from, EvtKind kind, string cardId,
                         string keyword, string effect)
    {
        var log = ctx.ActionLog;
        for (int i = Math.Max(0, from); i < log.Count; i++)
        {
            var e = log[i];
            if (e == null || e.Kind != kind || e.CardId != cardId) continue;
            if (keyword != null && e.Keyword != keyword) continue;
            if (effect != null && e.Effect != effect) continue;
            return i;
        }
        return -1;
    }

    static bool HasRemoved(BattleContext ctx, CardInstance inst)
    {
        foreach (var r in ctx.Removed)
            if (r != null && ReferenceEquals(r.Instance, inst) && r.Reason == "ephemeral") return true;
        return false;
    }

    static bool HasLog(BattleContext ctx, string needle)
    {
        var ev = ctx.Events;
        for (int i = 0; i < ev.Count; i++)
            if (ev[i] != null && ev[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    static void SegFail(string section, Exception e)
    {
        Tr(false, section + " 段异常（算红，⛔ 不是跳过）: " + e.GetType().Name + "：" + e.Message);
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B106_W23：临时卡 —— **先 `turn_end` 触发、后移出手牌**"
                              + "（原版 `CardScript__OnTurnEnd.c:64/72/80` → `:141/:147`）");
        // 逐段 catch：任一段抛异常不许把后面几段与合计行一起吞掉（`B106_W10` 的教训）。
        try { SectionA_Premise(); } catch (Exception e) { SegFail("A 前提", e); }
        try { SectionB_Order(); } catch (Exception e) { SegFail("B 核心次序", e); }
        try { SectionC_Control(); } catch (Exception e) { SegFail("C 控制组", e); }
        Debug.Log(_sink.P + "B106W23 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================
    //  A：判据前提（卡面形状 —— 任何一条不成立，B 段的结论都不成立）
    // ============================================================
    static void SectionA_Premise()
    {
        MenuCheck.Section(_sink, "A 前提：合成卡 = 非单位卡 + 手牌陷阱形态 + `TurnEnd(40)` ability");

        var eph = HandTrap(EphName, true);
        var ctrl = HandTrap(CtrlName, false);

        Tr(!eph.IsUnit, "A1 `" + EphName + "` 是**非单位卡**（`IsHandTrap` 的第一道卫是 `!IsUnit`）");
        Tr(eph.Has("ephemeral"), "A2 `" + EphName + "` 带 `ephemeral` 关键词（`IsEphemeral` / `TryTakeOneEphemeral` 认的那一格）");
        Tr(!ctrl.Has("ephemeral"), "A3 控制组 `" + CtrlName + "` **不带** `ephemeral`（= 与 B 段唯一的一处差异）");

        Tr(EffectText.IsHandTrap(eph), "A4 `EffectText.IsHandTrap` 认它是**手牌陷阱**");
        var at = EffectText.SplitAtTurn(eph.Desc);
        Tr(at != null && at[0] == "turn_end",
           "A5 `SplitAtTurn` 认出相位 = `turn_end`（`ResolveAtTurn(ctx, \"turn_end\")` 的手牌那一趟才收它）");
        var ops = (at != null && at.Length > 1) ? EffectText.Parse(at[1], out _, out _) : null;
        Tr(ops != null && ops.Count == 1 && ops[0].Verb == "deal",
           "A6 正文解析成 **1 条 `deal`**（与真卡 `Poisoned Supplies` 逐字同形；"
         + "`ResolveAtTurn` 要求 `ops.Count > 0` 才会建手牌 job）");
        C(eph.AbilityTriggerPresence("TurnEnd"), 1,
          "A7 `AbilityTriggerPresence(\"TurnEnd\") == 1` —— 手牌那一趟 `EmitTurnTriggerNamed` 的闸"
        + "（`HasTriggerSource` → `RawCardScript__TriggerAbility` 的 ability presence）；"
        + "⚠️ 没有 `abilityTriggersVersioned: true` 时它恒 `-1`（未知）⇒ 不发事件 ⇒ B 段会假红");
    }

    // ============================================================
    //  B：核心 —— 触发先、移出后
    // ============================================================
    static void SectionB_Order()
    {
        MenuCheck.Section(_sink, "B 核心：`Ephemeral` + `At the end of your turn, …` ⇒ **先触发、后移出**");

        var ctx = NewBattle();
        Tr(ctx != null, "B0 （前提）造出「第 1 回合 · 行动方 = 座位 0」的对局");
        if (ctx == null) return;

        var trap = ctx.NewInstance(HandTrap(EphName, true));
        ctx.Players[0].Hand.Add(trap);
        Tr(ctx.Players[0].Hand.Contains(trap), "B0 （前提）这张临时卡已经在座位 0 的手牌里");
        Tr(ctx.IsEphemeral(trap), "B0 （前提）这一份被判成临时卡（`Card.Has(\"ephemeral\")` 那条路）");

        int from = ctx.ActionLog.Count;
        RuleCore.EndTurn(ctx);

        int trig = FindEvent(ctx, from, EvtKind.Trigger, EphName, "TurnEnd", "turn_end");
        int rem = FindEvent(ctx, from, EvtKind.Return, EphName, null, "ephemeral");

        Tr(trig >= 0,
           "B1 ★★ **`turn_end` 触发那一刻卡还在手牌** —— 发了 `EvtKind.Trigger`"
         + "（`Keyword = \"TurnEnd\"` · `Effect = \"turn_end\"`）"
         + "｜判据 原版 `CardScript__OnTurnEnd.c:72`（TurnEnd）在 `:141/:147`（移出）**之前**"
         + "｜🧨 把 `SweepEphemeral` 挪回 `ResolveAtTurn(\"turn_end\")` **之前**（= 改前的次序）"
         + "⇒ 手牌快照里已经没有这张卡 ⇒ **一条 Trigger 都不发** ⇒ 红（而 C1 仍绿）");

        Tr(rem >= 0,
           "B2 移出事件在（`EvtKind.Return` · `effect = \"ephemeral\"`）"
         + "｜🧨 把去向改成弃牌堆（`AddToCemetery`）⇒ 没有这一条 ⇒ 红");

        Tr(trig >= 0 && rem >= 0 && trig < rem,
           "B3 ★★ **次序 = 先触发、后移出**（trigger #" + trig + " < remove #" + rem + "）"
         + "｜🧨 改成「先快照手牌 → 再移出 → 再拿快照发触发」⇒ B1 绿而**本条红**"
         + "（⛔ 只断「最后卡没了」挡不住这种改法）");

        Tr(!ctx.Players[0].Hand.Contains(trap), "B4 回合末确实把这张临时卡**移出了手牌**");
        Tr(HasRemoved(ctx, trap),
           "B5 去向 = **移出游戏**（`ctx.Removed` 里 `Reason == \"ephemeral\"`、按**实例**记；⛔ 不是弃牌堆）");
        Tr(HasLog(ctx, "回合结束触发段"),
           "B6 `turn_end` 触发段**真的开跑了**（日志有「回合结束触发段：N 条」）"
         + "｜🧨 改前 `total == 0` 会走 `ResolveAtTurn` 的早退 ⇒ 这一行日志根本没有 ⇒ 红");
    }

    // ============================================================
    //  C：控制组 —— 同一段 desc、**不带** `Ephemeral`
    //     （证明「B1 的那条 Trigger」不是恒真：管线坏了这里也会红）
    // ============================================================
    static void SectionC_Control()
    {
        MenuCheck.Section(_sink, "C 控制组：同一段 desc、**去掉 `Ephemeral`** ⇒ 触发照发、不出现移出事件、卡留在手里");

        var ctx = NewBattle();
        Tr(ctx != null, "C0 （前提）造出「第 1 回合 · 行动方 = 座位 0」的对局");
        if (ctx == null) return;

        var ctrl = ctx.NewInstance(HandTrap(CtrlName, false));
        ctx.Players[0].Hand.Add(ctrl);
        Tr(!ctx.IsEphemeral(ctrl), "C0 （前提）控制组这张**不是**临时卡");

        int from = ctx.ActionLog.Count;
        RuleCore.EndTurn(ctx);

        int trig = FindEvent(ctx, from, EvtKind.Trigger, CtrlName, "TurnEnd", "turn_end");
        int rem = FindEvent(ctx, from, EvtKind.Return, CtrlName, null, "ephemeral");

        Tr(trig >= 0,
           "C1 **控制组的 `TurnEnd` 触发也发了** —— 说明 B1 的那条事件不是「时序之外恒真」"
         + "｜🧨 把 `HasTriggerSource` 的 presence 闸或 `EmitTurnTriggerNamed` 弄坏 ⇒ 本条与 B1 一起红"
         + "（那时**不是**次序问题）");
        Tr(rem < 0, "C2 控制组**没有** `effect == \"ephemeral\"` 的移出事件（它不是临时卡）");
        Tr(ctx.Players[0].Hand.Contains(ctrl),
           "C3 控制组那张**仍留在手里**（战术卡回合末不会自己离开手牌）"
         + "｜🧨 把清扫写成「扫整张手牌」⇒ 本条红");
    }
}
