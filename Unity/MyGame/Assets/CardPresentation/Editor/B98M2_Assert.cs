// B98M2_Assert.cs — 执行代理 B98_M2（第九十八会话 · 波 11 · 轮抽线）
// ============================ 它盯的是什么 ============================
//  上游残差 → `资料/完全复刻/报告/B98_K5.md` §七·1：
//             「`DraftEventBase.Expire` 的 `DraftState == OngoingRun(15)` 那一支**不可达** —— 本地状态反推只产出 0/5/20」
//  判据     → 原版 `DraftEventBase.Expire` 真体
//             `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.Expire__180f230.c`
//               · `:33`  `if ((int)param_1[5] < 5)`   ← `*(int*)(this + 0x28)` = `DraftState` 字段
//               · `:68`  `if ((int)param_1[5] == 0xf)` ← `OngoingRun(15)`：打云脚本 `ExpireDraftEvent`（跳过）
//               · `:78-85` else 支：`CustomDebug.Log("Trying to finish an unstarted run")` + `onComplete?.Invoke()`
//             状态机三条 → `CardPresentation/Shell/DraftModeWindow.cs:2259-2305 UpdateState()`（①②③）
//  · A–E 段：`DraftEventBase.CurrentDraftState` 的反推四档（0 / 5 / 10 / 15）与收尾三条
//  · F 段：`Expire` 在 `OngoingRun(15)` 下**走收尾那一支**（判支靠**日志字面量** + 状态副作用，不靠「感觉对」）
//  · G 段：**对照**——没开跑的一局（`SelectPack(10)`）下 `Expire` 走 else 支（证明 F 段不是「永远成立」的假绿）
//
// ============================ 🔴 灭自证（逐条写清「怎么才能让它红」）============================
//  · A1/B1/C1/D1 断的是**数字**（0/5/10/15），期望值来自原版枚举与 `UpdateState` 的分支表，不从被测代码里读回来。
//        🧨 把 `DraftStatePacksPicked` 删掉 / 把 ③ 改回「一律 `SelectWarlord`」 ⇒ **D1 红**；
//        🧨 把 ③ 的 `<` 写成 `<=` ⇒ **D4 红**（少一包那条会变成 15）。
//  · F1 断「没打 else 支那句 `Trying to finish an unstarted run`」—— 那句是**原版 `.c:79` 的字面量**，
//        期望值不从我们的代码里取。🧨 把 `CurrentDraftState` 的反推改回 K5 那一版（只给 0/5/20）
//        ⇒ 状态算成 5 ⇒ `Expire` 掉进 else 支 ⇒ 那句出现 ⇒ **F1 红**（同时 G1 反而仍是绿 —— 证明两支行得通）。
//  · F2 断「收尾支的正文日志出现」⇒ 防「两支都静默」这种假绿。
//  · G1/G2 是 F1/F3 的对照支：证明 else 支在本夹具下**确实可达**、`onComplete` **确实会被调**
//        （否则「不调 onComplete」这条无论怎么改都绿 = 自证）。
//  ⛔ 本文件**不跑 Unity 批处理**、不建持久资产、不写盘（`DraftModeWindow.SaveRunIfAuto` 只在 `StoreOn` 为真时写，
//     自检没设 `OverrideDir` ⇒ 不落盘）；唯一动到的全局状态 = `DraftModeWindow` 的静态 `_run`，**收尾必还原**。
//
// ============================ 宿主接线（由调度台做 —— 本代理不碰断言宿主）============================
//   建议宿主：`Editor/DraftWindowAssertD2.cs`（轮抽窗自身那条链）或任一已在跑 `RuleEngine`/`Shell` 域的宿主。
//   宿主 `Run()` 里加两行：
//     B98M2_Assert.Run();
//     _sink.Pass += B98M2_Assert.Pass; _sink.Fail += B98M2_Assert.Fail;
//   ⚠️ 本段**不要求**有打开的轮抽窗：没有窗时 F4 走「不假装收尾」那一支（同样是一条断言，不是静默跳过）。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B98_M2：轮抽 `DraftState` 反推补齐 `OngoingRun(15)`（= `Expire` 收尾支可达）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B98M2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B98M2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_M2 · 轮抽 `DraftState` 反推补齐 `OngoingRun(15)`（`Expire` 收尾支可达）");

        // 全局状态纪律：本段会**整体替换** `DraftModeWindow` 的静态 `_run`（这正是状态反推的唯一输入）。
        // 进来时那一份原样存下、收尾再装回去（⛔ 别把自检那一局留给真 Play）。
        var saved = DraftModeWindow.RunForTest;
        try
        {
            SectionState();
            SectionExpire();
        }
        catch (Exception e)
        {
            // 段内抛异常 ⇒ 记一条**失败**（⛔ 不静默吞掉，也不把宿主一起带走）
            Tr(false, "B98_M2 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (saved == null) DraftModeWindow.ClearRunForTest();
            else DraftModeWindow.SetRunForTest(saved);
        }

        Debug.Log(_sink.P + "B98_M2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    /// <summary>跑 <paramref name="body"/> 并收下它打的所有日志（判支用：两支的日志串是**原版 `.c` 的字面量**）。
    /// ⚠️ `Debug.Log` 在同线程上**同步**派发给 `logMessageReceived` ⇒ 返回时已经收全。</summary>
    static List<string> CaptureLogs(Action body)
    {
        var msgs = new List<string>();
        Application.LogCallback h = delegate (string cond, string stack, LogType type) { msgs.Add(cond ?? string.Empty); };
        Application.logMessageReceived += h;
        try { body(); }
        finally { Application.logMessageReceived -= h; }
        return msgs;
    }

    static bool HasLog(List<string> logs, string needle)
    {
        for (int i = 0; i < logs.Count; i++) if (logs[i] != null && logs[i].Contains(needle)) return true;
        return false;
    }

    /// <summary>造一份轮抽一局存档（只填本段要读的那几格 —— 其余留字段默认值）。</summary>
    static DraftRunSave Fixture(int joinState, int payState, string warlordId, int packs, int won, int lost)
    {
        var keys = new string[packs];
        for (int i = 0; i < packs; i++) keys[i] = "b98m2_pack_" + i;
        return new DraftRunSave
        {
            joinState = joinState,
            payState = payState,
            warlordId = warlordId,
            pickedKeys = keys,
            selectedCards = new string[0],
            matchesWon = won,
            matchesLost = lost,
        };
    }

    // ============================================================ A–D 段：状态反推四档
    static void SectionState()
    {
        // 判据：原版 `UpdateState` ①②③（本地等价物 = `DraftModeWindow.UpdateState()`，:2259-2305）
        //       + `DraftState` 字段真身 = `this+0x28`（`…Expire__180f230.c:33/:68` 两处比较）。
        var probe = new B98M2_ProbeDraft();

        // ---- A：没有一局 ⇒ Intro(0)（原版那格还是初值 0）
        DraftModeWindow.ClearRunForTest();
        Tr(DraftModeWindow.RunForTest == null, "A0 前提：`ClearRunForTest()` 之后 `RunForTest` 是 null");
        C(probe.CurrentDraftState, (int)DraftModeState.Intro, "A1 没有一局 ⇒ `Intro(0)`");

        // ---- B：已加入、还没付/免费报名（payState=NotPaid）、没选战将 ⇒ Intro(0)（原版 ② 的前半）
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.NotPaid, null, 0, 0, 0));
        Tr(DraftModeWindow.RunForTest != null && string.IsNullOrEmpty(DraftModeWindow.RunForTest.warlordId),
           "B0 前提：夹具是「已加入 + 未付款 + 没选战将」");
        C(probe.CurrentDraftState, (int)DraftModeState.Intro, "B1 已加入未付款、没选战将 ⇒ `Intro(0)`（原版 ②）");

        // ---- C：报名付过（Free）、还没选战将 ⇒ SelectWarlord(5)（原版 ② 的后半）
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free, null, 0, 0, 0));
        Tr(DraftModeWindow.RunForTest != null && DraftModeWindow.RunForTest.payState == (int)DraftPayState.Free,
           "C0 前提：夹具是「已加入 + 免费报名 + 没选战将」");
        C(probe.CurrentDraftState, (int)DraftModeState.SelectWarlord, "C1 已报名、没选战将 ⇒ `SelectWarlord(5)`（原版 ②）");

        // ---- D：选满包 ⇒ OngoingRun(15)  ★ 本轮补的那一档（K5 之前反推只到 5）
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free,
                                              "b98m2_warlord", DraftPacks.RoundsPerDraft, 0, 0));
        C(DraftModeWindow.RunForTest.pickedKeys.Length, DraftPacks.RoundsPerDraft,
          "D0 前提：夹具选满了 `RoundsPerDraft` 包");
        C(probe.CurrentDraftState, (int)DraftModeState.OngoingRun,
          "D1 选满包、还没收尾 ⇒ `OngoingRun(15)`（原版 ③ 的 15 那一档；🧨 反推改回 K5 那版 ⇒ 这里会是 5）");
        Tr(probe.CurrentDraftState != (int)DraftModeState.SelectWarlord,
           "D2 灭自证：它**不是**改前那个恒定值 `SelectWarlord(5)`");

        // ---- D3：边界 —— 少一包仍是 SelectPack(10)（🧨 把 ③ 的 `<` 写成 `<=` ⇒ 红）
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free,
                                              "b98m2_warlord", DraftPacks.RoundsPerDraft - 1, 0, 0));
        C(DraftModeWindow.RunForTest.pickedKeys.Length, DraftPacks.RoundsPerDraft - 1,
          "D3 前提：夹具是「少一包」");
        C(probe.CurrentDraftState, (int)DraftModeState.SelectPack,
          "D4 少一包 ⇒ `SelectPack(10)`（边界是 `<` 不是 `<=`）");

        // ---- E：收尾三条判据（原版 ①）—— 任一成立都落 Finished(20)，且**盖过** ③ 的 15
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Finished, (int)DraftPayState.Free,
                                              "b98m2_warlord", DraftPacks.RoundsPerDraft, 0, 0));
        C(probe.CurrentDraftState, (int)DraftModeState.Finished,
          "E1 ①之一：`joinState == Finished(2)` ⇒ `Finished(20)`（即使包也选满了）");

        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free,
                                              "b98m2_warlord", DraftPacks.RoundsPerDraft, 0, DraftModeWindow.MaxTries));
        C(probe.CurrentDraftState, (int)DraftModeState.Finished,
          "E2 ①之二：`matchesLost >= MaxTries` ⇒ `Finished(20)`");

        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free,
                                              "b98m2_warlord", DraftPacks.RoundsPerDraft,
                                              DraftModeWindow.MatchesToFinish, 0));
        C(probe.CurrentDraftState, (int)DraftModeState.Finished,
          "E3 ①之三：`matchesWon + matchesLost >= MatchesToFinish` ⇒ `Finished(20)`");
    }

    // ============================================================ F 段：`Expire` 的两支
    static void SectionExpire()
    {
        // 判据：`…Expire__180f230.c:68`（`== 0xf` ⇒ 收尾支，onComplete **不**在这一支被调 —— 它被打成云的完成回调）
        //       与 `:78-85`（else 支 ⇒ `CustomDebug.Log(...)` + `onComplete?.Invoke()`）。
        var probe = new B98M2_ProbeDraft();

        // ---- F：正在跑一局 ⇒ 收尾支（`== 0xf`），**不是** else 支
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free,
                                              "b98m2_warlord", DraftPacks.RoundsPerDraft, 0, 0));
        C(probe.CurrentDraftState, (int)DraftModeState.OngoingRun, "F0 前提：夹具此刻的状态是 `OngoingRun(15)`");
        bool hadWindow = DraftModeWindow.LastOpened != null;      // 有没有一扇活的轮抽窗（决定 `FinishRun` 那一跳走不走得到）
        int joinBefore = DraftModeWindow.RunForTest.joinState;
        int calls = 0;
        List<string> logs = CaptureLogs(delegate { probe.Expire(delegate { calls++; }); });

        // 🔴 判支的**主判据**：两支的日志串是原版 `.c` 里两条不同的字面量 ——
        //    else 支 = `CustomDebug.Log("Trying to finish an unstarted run (this will not do anything)")`（`.c:79`）
        //    🧨 把 `CurrentDraftState` 的反推改回 K5 那版（只给 0/5/20）⇒ 状态算成 5 ⇒ 打进 else 支 ⇒ 这行日志出现 ⇒ **红**。
        Tr(!HasLog(logs, "Trying to finish an unstarted run"),
           "F1 `Expire` 在 `OngoingRun(15)` 下**没有**打 else 支那句 `Trying to finish an unstarted run`"
         + "（= 真的走了收尾支；🧨 反推改回旧版 ⇒ 红）");
        Tr(HasLog(logs, "正在跑但拿不到窗") || HasLog(logs, "ExpireDraftEvent"),
           "F2 收尾支的**正文**确实跑了（有窗时打 `收尾后原版打云脚本 CloudHandle.ExpireDraftEvent`，无窗时打 `正在跑但拿不到窗`）");
        C(calls, 0,
          "F3 收尾支**不调** `onComplete`（原版那一支把完成动作交给云回调 `…_Expire_b__1`；本地没有服务器 ⇒ 不假装完成 —— "
        + "⚠️ 这是本实现的契约，见报告 §还差什么 第 1 条）");
        if (hadWindow)
        {
            C(DraftModeWindow.RunForTest.joinState, (int)DraftJoinState.Finished,
              "F4 有活的轮抽窗 ⇒ 那一支真的调了 `FinishRun()`（`joinState` → `Finished(2)`）");
        }
        else
        {
            C(DraftModeWindow.RunForTest.joinState, joinBefore,
              "F4 没有活的轮抽窗 ⇒ **不假装收尾**（`joinState` 保持 " + joinBefore + "；原版那一跳要窗，"
            + "本段不建窗 ⇒ 只验「不静默成功」这一半）");
        }

        // ---- G：对照支 —— 没开跑的一局 ⇒ else 支（**证明 F1/F3 不是「永远不出现 / 永远不调」的假绿**）
        DraftModeWindow.SetRunForTest(Fixture((int)DraftJoinState.Joined, (int)DraftPayState.Free,
                                              "b98m2_warlord", 1, 0, 0));
        C(probe.CurrentDraftState, (int)DraftModeState.SelectPack, "G0 前提：只选了 1 包 ⇒ `SelectPack(10)`");
        calls = 0;
        logs = CaptureLogs(delegate { probe.Expire(delegate { calls++; }); });
        Tr(HasLog(logs, "Trying to finish an unstarted run"),
           "G1 没开跑的一局 ⇒ `Expire` 走 else 支（原版 `.c:79` 那句日志出现）");
        C(calls, 1, "G2 同一支里 `onComplete` **被调一次**（原版 `.c:78-85` 的 `onComplete?.Invoke()`）");
    }

    // ============================================================ 夹具：一个能被实例化的轮抽活动
    /// <summary>`DraftEventBase&lt;TS,TD&gt;` 是抽象的，而且本仓**故意没有**具体子类
    /// （原版两个具体子类各有自己的账行，见 `Shell/DraftEventBase.cs` 文件尾那条注释）
    /// ⇒ 这里只为实现 `PlayEvent.EventPlayMode` 这一个抽象成员（= 原版 `EventPlayMode =&gt; PlayModes.ClosedDeck`）。
    /// 它**不建 UI、不碰资产**，只用来调 `Expire` / `CurrentDraftState` 这两个纯逻辑口。</summary>
    sealed class B98M2_ProbeDraft : DraftEventBase<DraftEventSave, DraftEventData>
    {
        /// <summary>原版 `DraftEventBase.get_EventPlayMode` = `7`（`PlayModes.ClosedDeck`；判据 → `判据/B97_R6_轮抽P2P房施工单.md` §三·1）。</summary>
        public override PlayModes EventPlayMode { get { return PlayModes.ClosedDeck; } }
    }
}
