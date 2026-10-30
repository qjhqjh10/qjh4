// B104_T21_Assert.cs — 执行代理 `B104_T21`（第一百零四会话 · 特效/动画 T21-a / T21-b）的断言
//
// ============================ 它盯的是什么 ============================
// 施工单 → `资料/完全复刻/判据/B104_T21与多目标_时间线_施工单.md` §一 的 T21-a / T21-b。
//
//  **T21-a（表现侧）**：`EvtKind.Trigger ∧ Slot < 0`（手牌陷阱 / 手牌里的卡发的那一拍）不再被
//  `BattleDriver.PlaySignal` 的 `if (slot < 0) return;` 整条吞掉，改走**手牌档锚通道**：
//    锚 = `CardView.EffectAnchor(false)`（= `2DCard` / `FaceRoot`）；
//    对手手牌没有 `CardView` ⇒ 跳过（并出声）。
//  判据：`CardScript__GetEffectAnchor.c:9-19` · `AbilityLogic__PlayAnim.c:185-222`。
//
//  **T21-b（引擎侧）**：`EvtKind.Trigger` 的「发射那一拍 → 结算那一拍」之间那个**目标回填**通道：
//    ① `EmitTurnTriggerRaw` 的 `InstanceId` 来自 `CardInstance.Id`（手牌那一趟不再恒 0）；
//    ② `ResolveWithTriggerTargetSink` 是**唯一**的 arm / save / restore 口（棋盘与手牌两个队列体共用）。
//  判据：`AbilityLogic__PlayAbility.c:257-264` · `AbilityLogic__PlayAnim.c:185-222`。
//
// ============================ 怎么用（宿主）============================
//  表现侧：`B104_T21_Assert.RunPresentation(drv);`   // `drv` = 本宿主手上那台 `BattleDriver`
//     · 推荐位置 = `Editor/BattleScene.cs` 里「已经开好一局、手牌里有卡」的那一节末尾。
//     · ⚠️ 它会调一次 `drv.PlaySignalForTest(...)`（= 生产那条 `PlaySignal`）⇒ 可能往时间线里排拍；
//       **不会改棋盘 / 手牌 / `Ctx` 的一格**（合成事件不进引擎的 `Signals`，只走表现层那一侧）。
//     · ⚠️ 它**会换掉 `CardEffects.Play` 钩子再换回去**（`try/finally`）—— ⛔ 别与别的用例并行调。
//  引擎侧：`B104_T21_Assert.RunEngine(ctx);`        // `ctx` = 本宿主手上的 `BattleContext`
//     · 推荐位置 = `RuleEngineTest.Run` 最后一节（那之后不再有人依赖 `Signals` / `ActionLog` 的行数）。
//     · ⚠️ 它往 `ctx.Signals` / `ctx.ActionLog` 各塞两条事件，**结束时逐条摘掉**（原状态还原）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A4：把 `CardView.EffectAnchor(false)` 改成返回 `transform`（卡根）⇒ **A4 红**。
//   · A5：把 `EffectAnchor(bool)` 改成两支返回同一颗 ⇒ **A5 红**。
//   · B4：`HandCardViewOf` 遇 `instanceId > 0` 时改成「名字也对上就算」⇒ **B4 红**。
//   · C3：把 `PlaySignal` 里 T21-a 那一段删掉（回到 `if (slot < 0) return;`）⇒ **C3 红**（计数不涨）。
//   · C4：把 `HandCardViewOf` 的 `owner != _me` 那一句删掉、改成按名字在手牌里找 ⇒ **C4 红**
//          （对手座位会「借」到本机那张同名牌）。
//   · D2：把 `PlayAbilityBeat` 里 `handAnchored` 那句回退删掉 ⇒ **D2 红**（锚点掉到 `createCardPos(140)`）。
//   · D3：把 `handAnchored: true` 传成 `false` ⇒ **D3 红**（同上）。
//   · E2：把 `InstanceId = unit != null ? unit.InstanceId : instanceId` 改回 `: 0` ⇒ **E2 红**。
//   · F2：把 `ResolveWithTriggerTargetSink` 里的 `finally` 还原删掉 ⇒ **F2 红**。
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B104_T21`：T21-a（手牌档锚）/ T21-b（Trigger 目标回填通道）的落地断言。</summary>
public static class B104_T21_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B104_T21] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ================================================================ 表现侧（T21-a）
    /// <summary>表现侧入口（宿主 = `Editor/BattleScene.cs`；见文件头「怎么用」）。</summary>
    public static void RunPresentation(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "B104_T21 · T21-a：手牌档锚（`Trigger ∧ Slot<0` 不再被吞）");

        // ---- A 段：前提 + 判据（**先断前提，再断结论** —— `if (x != null) Check(...)` 是静默跳过，本仓红线）
        Tr(drv != null, "A0 **前提**：拿到 `BattleDriver` 实例（拿不到 ⇒ 下面全部空转）｜本文件不静默跳过");
        if (drv == null) { LogTotal("表现侧"); return; }

        Tr(drv.Ctx != null, "A1 **前提**：这一局已经开好（`drv.Ctx != null`）");
        if (drv.Ctx == null) { LogTotal("表现侧"); return; }

        CardView v = FirstHandView(drv);
        Tr(v != null, "A2 **前提**：本机手牌里至少有一张 `CardView`（空手牌 ⇒ 下面的锚点断言无对象可量）");
        if (v == null) { LogTotal("表现侧"); return; }

        Tr(v.FaceLayerRoot != null && !ReferenceEquals(v.FaceLayerRoot, v.transform),
           "A3 **前提**：这张卡建过 `2DCard` 那一级（`FaceLayerRoot` 存在且 ≠ 卡根）"
           + " —— 没有它，「手牌档 / 场上档」两支会是同一个 Transform，A4/A5 就没有判别力");

        // 🔴 判据（`CardScript__GetEffectAnchor.c:9-19`：手牌卡状态 ⇒ `card2DController` = `2DCard`）
        Tr(ReferenceEquals(v.EffectAnchor(false), v.FaceLayerRoot),
           "A4 **判据**：`EffectAnchor(false)`（手牌档）= `FaceRoot` = 原版 `card2DController`（`2DCard` 那一级）"
           + "｜灭自证：改成返回卡根 ⇒ 红");
        Tr(!ReferenceEquals(v.EffectAnchor(false), v.EffectAnchor(true)),
           "A5 **判据**：手牌档锚 ≠ 场上档锚（原版 `+0x30` vs `+0x1A0` 两处）"
           + "｜灭自证：两支返回同一颗 ⇒ 红");

        // ---- B 段：`HandCardViewOf` 的配对口径
        Tr(v.Inst != null, "B0 **前提**：这张手牌视图带实例号（`CardView.Inst`）—— 没有它，实例号那一路无对象可量");
        int instId = v.Inst != null ? v.Inst.Id : 0;
        int foeSeat = 1 - drv.MySeat;

        if (instId > 0)
            Tr(ReferenceEquals(drv.HandCardViewOfForTest(drv.MySeat, v.Data.id, instId), v),
               "B1 **结论**：按实例号认 —— 命中**这一张**（同名两张手牌靠它区分）");
        Tr(!ReferenceEquals(v, null) && v.Data.id != null, "B2 **前提**：这张卡的 `CardData.id` 非空（按名字认那一路要用）");
        var byName = drv.HandCardViewOfForTest(drv.MySeat, v.Data.id, 0);
        Tr(byName != null && byName.Data.id == v.Data.id,
           "B3 **结论**：没有实例号时按卡名认（`CardData.id` = 卡名）");
        Tr(drv.HandCardViewOfForTest(foeSeat, v.Data.id, instId) == null,
           "B4 **结论**：**对手座位**拿不到视图（对手手牌只有卡背 `ImageQuad`）"
           + "｜灭自证：删掉 `owner != _me` ⇒ 会借到本机那张同名牌 ⇒ 红");
        if (instId > 0)
            Tr(drv.HandCardViewOfForTest(drv.MySeat, v.Data.id, int.MaxValue) == null,
               "B5 **结论**：带实例号时**只认实例号**（名字对上、实例号对不上 ⇒ null，⛔ 不按名字猜）");

        // ---- C 段：`PlaySignal` 那条分支真的进去了（这一条断的是**接线**，不是锚点）
        int before = drv.HandTriggerPlayAttemptsForTest;
        var eMine = new BattleEvent
        {
            Kind = EvtKind.Trigger, Player = drv.MySeat, Slot = -1,
            CardId = v.Data.id, InstanceId = instId,
            Keyword = "TurnEnd", Effect = "turn:turn_end",
        };
        bool threw = false;
        try { drv.PlaySignalForTest(eMine); }
        catch (System.Exception ex) { threw = true; Debug.LogError("[B104_T21] `PlaySignalForTest` 抛了：" + ex); }
        Tr(!threw, "C0 **结论**：`Trigger ∧ Slot<0` 那条路**不抛**（表现层不许因为一条合成事件把一局打断）");
        C(drv.HandTriggerPlayAttemptsForTest, before + 1,
          "C1 **结论**：本机手牌的 `Trigger ∧ Slot<0` 走进了 T21-a 那一支（计数 +1）"
          + "｜灭自证：删掉 T21-a 那一段、回到 `if (slot < 0) return;` ⇒ 计数不涨 ⇒ 红");

        int beforeFoe = drv.HandTriggerPlayAttemptsForTest;
        var eFoe = new BattleEvent
        {
            Kind = EvtKind.Trigger, Player = foeSeat, Slot = -1,
            CardId = v.Data.id, InstanceId = instId,
            Keyword = "TurnEnd", Effect = "turn:turn_end",
        };
        try { drv.PlaySignalForTest(eFoe); }
        catch (System.Exception ex) { threw = true; Debug.LogError("[B104_T21] `PlaySignalForTest`（对手）抛了：" + ex); }
        C(drv.HandTriggerPlayAttemptsForTest, beforeFoe,
          "C2 **结论**：对手手牌 ⇒ **跳过**（不进那一支；与原版 `PlayAnim.c:185-222` 的 `LogWarning` 后跳过同结论）"
          + "｜灭自证：把 `HandCardViewOf` 的座位闸删掉 ⇒ 计数 +1 ⇒ 红");

        // ---- D 段：锚点真的落在手牌档（`0 self` 这一档：`handAnchored` 直接压过 `OnBoardOf` 那条推断）
        //
        //  🔴 **为什么要临时把 `AnimAnchors.OnBoardOf` 钉成 `true`**：真实的 `OnBoardOf` 对这张手牌视图本来就返回
        //  false（它不在 `_myUnits`/`_foeUnits` 里）⇒ 不钉的话 `handAnchored: false` 也会落到同一个手牌锚，
        //  这一对断言就**没有判别力**（两条都对 = 断不出「handAnchored 有没有接线」）。
        //  钉成 true 后：`handAnchored:false` ⇒ 场上档锚；`true` ⇒ 手牌档锚 —— 两条必须**不同**。
        Vector3 lastPos = Vector3.zero;
        Transform lastParent = null;
        string lastName = null;
        int lastCount = 0;
        var prevPlay = CardEffects.Play;
        var prevOnBoard = AnimAnchors.OnBoardOf;
        Tr(prevOnBoard != null, "D-1 **前提**：`AnimAnchors.OnBoardOf` 已被驱动挂上（`HookAnimAnchors`）");
        CardEffects.Play = (name, pos, parent) => { lastName = name; lastPos = pos; lastParent = parent; lastCount++; };
        AnimAnchors.OnBoardOf = _ => true;
        try
        {
            var probe = new BattleEvent { Kind = EvtKind.Trigger, Player = drv.MySeat, Slot = -1, CardId = v.Data.id };
            int spSelf = (int)PositionOption.self;

            lastCount = 0; lastParent = null;
            drv.PlayAbilityBeatHandForTest(probe, "T21_ProbeVfx", spSelf, spSelf, 0, v, null, true);
            Tr(lastCount > 0 && lastName == "T21_ProbeVfx",
               "D0 **前提**：探针那一拍真的调到了 `CardEffects`（否则 D1/D2 无对象可量）");
            Tr(ReferenceEquals(lastParent, v.EffectAnchor(false)),
               "D1 **结论**：`handAnchored: true` ⇒ 起锚点 = 这张手牌卡的 `EffectAnchor(false)`"
               + "（= 原版 `GetEffectAnchor` 手牌档那一支；⚠️ 此时 `OnBoardOf` 被钉成 true）"
               + "｜灭自证：删掉 `handAnchored` 那一句 ⇒ 落到场上档锚 ⇒ 红");

            lastCount = 0; lastParent = null;
            drv.PlayAbilityBeatHandForTest(probe, "T21_ProbeVfx", spSelf, spSelf, 0, v, null, false);
            Tr(lastCount > 0 && ReferenceEquals(lastParent, v.EffectAnchor(true))
               && !ReferenceEquals(lastParent, v.EffectAnchor(false)),
               "D2 **结论（灭自证的另一半）**：同一个调用、`handAnchored: false` ⇒ 锚点 = **场上档**"
               + "（`OnBoardOf` 钉成 true 时 `0 self` 走 `EffectAnchor(true)`）"
               + "｜证明 D1 那个「对上了」是 `handAnchored` 给的，不是碰巧");
        }
        finally { AnimAnchors.OnBoardOf = prevOnBoard; CardEffects.Play = prevPlay; }

        LogTotal("表现侧");
    }

    static CardView FirstHandView(BattleDriver drv)
    {
        var hs = drv.HandViewsForTest;
        if (hs == null) return null;
        for (int i = 0; i < hs.Count; i++) if (hs[i] != null) return hs[i];
        return null;
    }

    // ================================================================ 引擎侧（T21-b）
    /// <summary>引擎侧入口（宿主 = `RuleEngineTest.Run`；见文件头「怎么用」）。</summary>
    public static void RunEngine(BattleContext ctx)
    {
        MenuCheck.Section(_sink, "B104_T21 · T21-b：Trigger 的「目标侧 + 格号」通道（实例号 / arm / save / restore）");

        Tr(ctx != null, "E0 **前提**：拿到 `BattleContext`（拿不到 ⇒ 下面全部空转）｜本文件不静默跳过");
        if (ctx == null) { LogTotal("引擎侧"); return; }

        CardDef card = AnyCard(ctx);
        Tr(card != null, "E1 **前提**：能从这一局里取到一张 `CardDef`（棋盘 / 手牌 / 卡池任意一处）");
        if (card == null) { LogTotal("引擎侧"); return; }

        var prevSink = ctx.TriggerTargetSink;
        var prevDone = ctx.TriggerTargetSinkDone;
        int sigBefore = ctx.Signals.Count;
        int logBefore = ctx.ActionLog.Count;
        var emitted = new System.Collections.Generic.List<BattleEvent>();
        try
        {
            // ---- E 段：实例号（手牌那一趟不再恒 0）
            var withId = RuleCore.EmitTurnTriggerRawForTest(ctx, card, 0, "TurnEnd", "turn:turn_end", 4242);
            if (withId != null) emitted.Add(withId);
            Tr(withId != null, "E2 **前提**：F 族发射体真发得出来（裸发射口不过闸）");
            if (withId != null)
            {
                C(withId.Kind, EvtKind.Trigger, "E3 **结论**：`Kind = Trigger`");
                C(withId.Slot, -1, "E4 **结论**：手牌那一趟 `Slot = -1`（不在棋盘上）");
                C(withId.InstanceId, 4242, "E5 **结论**：`InstanceId` 来自调用方给的 `CardInstance.Id`（⛔ 不再恒 0）"
                                         + "｜灭自证：改回 `: 0` ⇒ 红");
                C(withId.Keyword, "TurnEnd", "E6 **结论**：`Keyword` = 原版 trigger 名（未带 `#i` 后缀）");
                C(withId.Effect, "turn:turn_end", "E7 **结论**：`Effect` = 相位标签（诊断位）");
                Tr(ctx.Signals.Count > 0 && ReferenceEquals(ctx.Signals[ctx.Signals.Count - 1], withId),
                   "E8 **结论**：`Signals` 里存的就是**同一个对象**（表现层事后读到的就是它 —— 这是「回填」能成立的前提）");
            }
            var zero = RuleCore.EmitTurnTriggerRawForTest(ctx, card, 0, "TurnEnd", "turn:turn_end", 0);
            if (zero != null) emitted.Add(zero);
            Tr(zero != null && zero.InstanceId == 0,
               "E9 **结论（灭自证的另一半）**：传 0 就真的是 0 ⇒ 证明 E5 那个 4242 不是常量");

            // ---- F 段：arm / save / restore（唯一口）
            var sentinel = new BattleEvent { Kind = EvtKind.Trigger, CardId = "T21_Sentinel" };
            ctx.TriggerTargetSink = sentinel;
            ctx.TriggerTargetSinkDone = true;

            bool ran = false, insideOk = false;
            RuleCore.ResolveWithTriggerTargetSinkForTest(ctx, withId, () =>
            {
                ran = true;
                insideOk = ReferenceEquals(ctx.TriggerTargetSink, withId) && !ctx.TriggerTargetSinkDone;
            });
            Tr(ran, "F0 **前提**：结算体被执行了（arm 只是顺手记目标，⛔ 不能因此吞掉效果本身）");
            Tr(insideOk, "F1 **结论**：结算期间 `TriggerTargetSink` = **这条事件** 且 `Done = false`（arm 生效）");
            Tr(ReferenceEquals(ctx.TriggerTargetSink, sentinel) && ctx.TriggerTargetSinkDone,
               "F2 **结论**：结算完 **restore 回外层**（嵌套触发不被内层覆盖）"
               + "｜灭自证：删掉 `finally` 里那两句 ⇒ 这里读到的是内层事件 ⇒ 红");

            bool ranNull = false;
            RuleCore.ResolveWithTriggerTargetSinkForTest(ctx, null, () => { ranNull = true; });
            Tr(ranNull && ReferenceEquals(ctx.TriggerTargetSink, sentinel),
               "F3 **结论**：`evt == null`（这一趟没发事件）⇒ **照常结算**、且不动外层那一格");
        }
        finally
        {
            // 原状态还原（⛔ 不给后续断言留下污染）
            ctx.TriggerTargetSink = prevSink;
            ctx.TriggerTargetSinkDone = prevDone;
            for (int i = 0; i < emitted.Count; i++) { ctx.Signals.Remove(emitted[i]); ctx.ActionLog.Remove(emitted[i]); }
            // 万一上面抛在中途、`Signals` 被改成别的形状 ⇒ 按条数兜底（只截掉**本段新增**的那几条）
            while (ctx.Signals.Count > sigBefore) ctx.Signals.RemoveAt(ctx.Signals.Count - 1);
            while (ctx.ActionLog.Count > logBefore) ctx.ActionLog.RemoveAt(ctx.ActionLog.Count - 1);
        }

        LogTotal("引擎侧");
    }

    static CardDef AnyCard(BattleContext ctx)
    {
        for (int p = 0; p < ctx.Players.Length; p++)
        {
            var ps = ctx.Players[p];
            if (ps == null) continue;
            for (int s = 0; s < ps.Board.Length; s++) if (ps.Board[s] != null && ps.Board[s].Card != null) return ps.Board[s].Card;
            for (int i = 0; i < ps.Hand.Count; i++) if (ps.Hand[i] != null && ps.Hand[i].Card != null) return ps.Hand[i].Card;
        }
        var pool = ctx.CardPool;
        if (pool != null && pool.Count > 0) return pool[0];
        return null;
    }

    static void LogTotal(string side)
    {
        Debug.Log(_sink.P + $"B104_T21 合计（{side}）：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
