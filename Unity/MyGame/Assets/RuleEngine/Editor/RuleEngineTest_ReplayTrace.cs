// RuleEngineTest_ReplayTrace.cs — 回放「逐动作全量状态指纹」记账器的自检（引擎线 `W2` 第 1 件）
//
// 判据（真机实据）：`资料/普查产出_1024d/真机验收记录_1024d.md` F7 ——
//   `[Replay] 演完了：8 回合 · 终局指纹 -1912646073 · 轨迹摘要 1187623750（录制时 1187623750）
//    ≠ 录制时的终局 -1762953645 ⇒ 不是同一局`。
//   ⇒ **动作轨迹逐条相同、终局不同** = 重演本身分叉（或开局条件不同源）。
//   现有 `_replayDivergence` 比的是 `DeepHash`（`BattleDriver.cs`），**与终局 `StateHash` 口径不同**
//   ⇒ 差在「StateHash 覆盖、DeepHash 不覆盖」的那几维时，它就抓不到。本类补的就是这一格（逐动作
//   全量指纹 + 开局指纹），语义 / 接线说明 → `RuleEngine/Core/ReplayFingerprintTrace.cs` 文件头。
//
// ⚠️ 本文件只测**记账器本身**（纯逻辑）。**接线**（`ReplayStore` 落两列 + `BattleDriver` 两端各调一次）
//    不在引擎白名单里 —— 由主对话另行派活，见本批执行报告。
//
// 🔴 断言都带 `🧨`；期望值全部写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批入口（`Run()` 里一行 `Step(TestReplayTrace)`）。</summary>
    public static void TestReplayTrace()
    {
        Section("🆕 引擎线 W2：回放逐动作全量指纹（`ReplayFingerprintTrace`）—— 开局不同源 / 第 N 条分叉 / 条数不一致");
        ReplayTraceHappy();
        ReplayTraceDivergence();
        ReplayTraceStartMismatch();
        ReplayTraceCountMismatch();
        ReplayTraceLegacyNoColumn();
    }

    static void ReplayTraceHappy()
    {
        var rec = ReplayFingerprintTrace.ForRecording();
        rec.RecordStart(111);
        rec.RecordAfterAction(201);
        rec.RecordAfterAction(202);
        rec.RecordAfterAction(203);
        Check(rec.SampleCount, 3, "`Recording` 收满 3 条动作后指纹");
        Check(rec.ToArray().Length, 3, "`ToArray()` 落盘形状 = 3 个 int（`JsonUtility` 直吃）");
        Check(rec.ToArray()[2], 203, "落盘的第 3 格就是第 3 条动作之后的指纹");

        var play = ReplayFingerprintTrace.ForReplay(rec.StartHash, rec.ToArray());
        CheckTrue(play.CheckStart(111), "★★ 开局指纹相同 ⇒ `CheckStart` = true（这一格是「开局条件同源」的判据）");
        CheckTrue(play.Observe(0, 201), "第 0 条之后指纹相同 ⇒ true");
        CheckTrue(play.Observe(1, 202), "第 1 条之后指纹相同 ⇒ true");
        CheckTrue(play.Observe(2, 203), "第 2 条之后指纹相同 ⇒ true");
        play.Finish(3);
        Check(play.FirstDivergentIndex, -1,
              "★★ 全程一致 ⇒ `FirstDivergentIndex = -1`（没有分叉）"
            + "｜🧨 任何一次 `Observe` 误判 ⇒ 这里变成动作序号 ⇒ 红");
        Check(play.ActionCountMismatch, 0, "条数一致 ⇒ `ActionCountMismatch = 0`");
        Check(play.Summary(), "回放全量指纹：逐条一致", "总结文案 = 「逐条一致」（自检/日志都读这一句）");
    }

    static void ReplayTraceDivergence()
    {
        var play = ReplayFingerprintTrace.ForReplay(111, new[] { 201, 202, 203 });
        CheckTrue(play.CheckStart(111), "（前提）开局一致");
        CheckTrue(play.Observe(0, 201), "（前提）第 0 条一致");
        CheckTrue(!play.Observe(1, 999),
                  "★★ 第 1 条之后指纹 202 → 999 ⇒ **`Observe` 返回 false**（这一条就是出声口）"
                + "｜🧨 只比终局、不逐条比 ⇒ 实得 true ⇒ 红（那就退回了改前形状）");
        Check(play.FirstDivergentIndex, 1,
              "★★ `FirstDivergentIndex = 1`（**第一条**不等的位置；后面的都覆盖不了它）"
            + "｜🧨 记成「最后一条」或「第一次调用」⇒ 红");
        Check(play.DivergentExpected, 202, "分叉点那两格：录制时 = 202");
        Check(play.DivergentActual, 999, "分叉点那两格：现在 = 999");
        // 后续条不再改第一分叉点（根因只有一个）
        play.Observe(2, 12345);
        Check(play.FirstDivergentIndex, 1, "后续条再不等也**不改**第一分叉点（根因只有一个）");
        Check(play.Summary(), "回放全量指纹：**第 1 条动作之后分叉**（录 202 / 现 999）",
              "总结文案带上动作序号与两格值");
    }

    static void ReplayTraceStartMismatch()
    {
        var play = ReplayFingerprintTrace.ForReplay(111, new[] { 201 });
        CheckTrue(!play.CheckStart(777),
                  "★★ 开局指纹 111 → 777 ⇒ `CheckStart` = false（**起始条件不同源**）"
                + "｜🧨 不判开局 ⇒ 实得 true ⇒ 红（那种分叉会被误读成「中途分叉」，查错方向）");
        Check(play.FirstDivergentIndex, ReplayFingerprintTrace.StartDiverged,
              "★★ 开局不同源用**独立哨兵**（`-2`），⛔ 不占用动作序号（0 是合法序号）"
            + "｜🧨 写成 0 ⇒ 与「第 0 条分叉」分不开 ⇒ 红");
        Check(play.Summary(), "回放全量指纹：**开局不同源**（录 111 / 现 777）", "总结文案分得开这两种病");
    }

    static void ReplayTraceCountMismatch()
    {
        // 录了 3 条，只演了 2 条 ⇒ 少演
        var fewer = ReplayFingerprintTrace.ForReplay(111, new[] { 201, 202, 203 });
        fewer.CheckStart(111);
        fewer.Observe(0, 201);
        fewer.Observe(1, 202);
        fewer.Finish(2);
        Check(fewer.ActionCountMismatch, 2,
              "★★ 录制 3 条、这次只演 2 条 ⇒ `ActionCountMismatch = 2`（**少演**）"
            + "｜🧨 不比条数 ⇒ 实得 0 ⇒ 红（「少演但已有那几条哈希都对」是最难看见的一种错）");
        Check(fewer.ExpectedActionCount, 3, "`ExpectedActionCount = 3`（录制时那一条数）");
        Check(fewer.FirstDivergentIndex, -1,
              "★ **条数与分叉是两件事**：少演不等于某一条分叉 ⇒ `FirstDivergentIndex` 仍为 -1"
            + "｜🧨 把条数问题塞进 `FirstDivergentIndex` ⇒ 红（两种病要分开看）");

        // 演得比录的多 ⇒ 多演
        var more = ReplayFingerprintTrace.ForReplay(111, new[] { 201 });
        more.CheckStart(111);
        more.Observe(0, 201);
        more.Observe(1, 202);        // 超界 ⇒ 不出声（条数在 Finish 统一报）
        more.Finish(2);
        Check(more.ActionCountMismatch, 2, "★★ 录制 1 条、演了 2 条 ⇒ `ActionCountMismatch = 2`（**多演**）");
    }

    static void ReplayTraceLegacyNoColumn()
    {
        var old = ReplayFingerprintTrace.ForReplay(111, null);
        CheckTrue(old.NoExpectedTrace,
                  "★ 老录像没有逐动作指纹 ⇒ `NoExpectedTrace = true`（**明确标成「这一层没判」**）");
        CheckTrue(old.Observe(0, 12345),
                  "★ 没有录制值时 `Observe` **不报分叉**（这条对账做不了 —— 但下面那条断言钉住它不许冒充「通过」）");
        Check(old.FirstDivergentIndex, -1, "没有录制值 ⇒ `FirstDivergentIndex` 保持 -1（不是「分叉」）");
        old.Finish(1);
        CheckTrue(old.NoExpectedTrace, "★★ `Finish` 之后 `NoExpectedTrace` **仍然为真**"
                                      + " —— 调用方（`BattleDriver`）据此在日志里写「本层对账没做」（⛔ 不是通过）"
                                      + "｜🧨 让 `Finish` 把它清掉 ⇒ 老档会被静默当成「已验」⇒ 红");
    }
}
