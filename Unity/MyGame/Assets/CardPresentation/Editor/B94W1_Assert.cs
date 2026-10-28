// B94W1_Assert.cs — 执行代理 **B94_W1**（第九十四会话 · 波 1.5）的断言
//
// ============================ 它盯的是什么 ============================
// `VictoryCountersStore.RecordVictory` 的**生产接线**（对局结算时记胜场）—— 判据是原版
// `PlayerDataManager.ProcessMatchResult`（唯一写点，服务端回包整份替换）+ 它的四个云脚本成功回调调用方。
// 逐条判据/做法/没落地部分 → `资料/完全复刻/报告/B94_W1.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94W1_Assert.Run();`
//   · **不跑 Unity 批处理**，**不写玩家存档**（A 组用 `BattleDriver.VictoryRecordSinkForTest` 拦下写盘、收尾还原）。
//   · 建议宿主（调度台统一接线，本文件不接）：
//       - **`Editor/MainMenuScene.cs`** 或 **`Editor/ProfileDataAssertE13` 那一族**（战将精通/个人资料那一片）
//     接线两行：`B94W1_Assert.Run();  _sink.Pass += B94W1_Assert.Pass; _sink.Fail += B94W1_Assert.Fail;`
//   · 若宿主**真打过一局**（如 `BattleScene.Run` 第 7 节），可在打完后再调
//     `B94W1_Assert.AssertAfterMatch(callsBefore, mode, replay, winner, me)` —— 它把「当场那次结算有没有按胜/负/平
//     去记」与同一份纯判定对上（不依赖结局是胜还是负，所以永不 flaky）。
//
// ============================ 🔴 灭自证（每条都写了「改哪一处会让它红」）============================
//   · A1/A2/A3 = `ShouldRecordVictory` 的胜/负/平三态。把判据从 `winner == me+1` 改成 `winner != me` ⇒ A2 红；
//                整条改成恒 true ⇒ A2/A3 红。
//   · B 组 = 教程/回放不记、练习记。把教程从排除表删掉 ⇒ B1 红；把练习也排除 ⇒ B3 红。
//   · A0 = 「调用计数 > 0」**前提**：胜局那次必须让 `VictoryRecordCallsForTest` 增量 = 1
//          （= 生产落点真的走到了 `RecordVictoryCore`）。若把调用点从结算支删掉 / 判定恒 false ⇒ A0 红。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B94_W1：`VictoryCountersStore.RecordVictory` 生产接线的断言。
/// <para>⛔ **期望值全是原版实读结论 / 字面量**（胜局记、负/平不记、教程不记、练习记），**不从被测实现里读**。</para></summary>
public static class B94W1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94W1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94-W1：胜场计数 RecordVictory 生产接线（胜/负/平 + 教程/回放/练习门 + 调用计数前提）");

        SectionA_胜场记录三态();
        SectionB_模式门();

        Debug.Log(_sink.P + "B94-W1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 胜场记录三态（胜=记 / 负=不记 / 平=不记）
    static void SectionA_胜场记录三态()
    {
        MenuCheck.Section(_sink, "A `ShouldRecordVictory` 三态 + 生产落点调用计数（前提：胜局计数 > 0）");

        // ⛔ 不写玩家真存档：把落盘换成自检口，收尾还原
        var savedSink = BattleDriver.VictoryRecordSinkForTest;
        int captured = 0; string capOriginal = null; string[] capDefeated = null; int capWins = 0;
        try
        {
            BattleDriver.VictoryRecordSinkForTest = (id, defs, wins) =>
            {
                captured++; capOriginal = id; capDefeated = defs; capWins = wins;
            };

            const int me = 0;   // 座 0

            // ---- 胜局：winner = me+1 = 1 ----
            BattleDriver.ResetVictoryRecordCounterForTest();
            captured = 0;
            bool recWin = BattleDriver.RecordVictoryForTest(GameMode.Classic, false, /*winner*/ 1, me, "UM47");
            C(recWin, true, "A1 ★ 胜局（`winner == me+1`）⇒ **要记**（原版胜场计数只在胜局累加）");
            // ★ 前提（硬检查）：调用计数必须真的 +1 —— 防「判定对了但生产落点没走」的静默跳过
            C(BattleDriver.VictoryRecordCallsForTest, 1,
              "A0 ★ **前提**：胜局这一次让「真去记」计数 = 1（> 0）—— 生产落点 `RecordVictoryCore` 确实走到；" +
              "把它从结算支删掉 / 判定恒 false ⇒ 这条红");
            C(captured, 1, "A0b 前提：下游只被喂了一次（不重复记）");
            C(capWins, 1, "A0c 记的是「+1 胜」");
            Tr(capDefeated != null && capDefeated.Length == 1 && capDefeated[0] == "UM47",
               "A0d 内层喂的是**我方督军原始 id**（raw=`UM47`）—— 这是 `PlayerInfo.GetVictoryCounters` 折叠的输入形状");

            // ---- 败局：winner = 2 ----
            int beforeLoss = BattleDriver.VictoryRecordCallsForTest;
            bool recLoss = BattleDriver.RecordVictoryForTest(GameMode.Classic, false, /*winner*/ 2, me, "UM47");
            C(recLoss, false, "A2 ★ 败局（`winner == 对面`）⇒ **不记**（把判据改成 `winner != me` / 恒 true ⇒ 这条红）");
            C(BattleDriver.VictoryRecordCallsForTest, beforeLoss, "A2b 败局**不动**调用计数（负场不进 `totalVictories`）");

            // ---- 平局：winner = 3 ----
            int beforeDraw = BattleDriver.VictoryRecordCallsForTest;
            bool recDraw = BattleDriver.RecordVictoryForTest(GameMode.Classic, false, /*winner*/ 3, me, "UM47");
            C(recDraw, false, "A3 ★ 平局（`winner == 3`）⇒ **不记**");
            C(BattleDriver.VictoryRecordCallsForTest, beforeDraw, "A3b 平局不动调用计数");
        }
        finally
        {
            BattleDriver.VictoryRecordSinkForTest = savedSink;
            BattleDriver.ResetVictoryRecordCounterForTest();
        }

        // 纯判定的三态（不碰盘、不碰计数）—— 与上面「生产落点」是同一份实现，这里再独立断一次
        Tr(BattleDriver.ShouldRecordVictory(GameMode.Classic, false, 1, 0), "A4 纯判定：胜局 ⇒ true");
        Tr(!BattleDriver.ShouldRecordVictory(GameMode.Classic, false, 2, 0), "A5 纯判定：败局 ⇒ false");
        Tr(!BattleDriver.ShouldRecordVictory(GameMode.Classic, false, 3, 0), "A6 纯判定：平局 ⇒ false");
    }

    // ------------------------------------------------------------ B 模式门（教程/回放不记；练习记）
    static void SectionB_模式门()
    {
        MenuCheck.Section(_sink, "B 模式门：教程/回放不记 · 练习记（判据 = 四个云脚本成功回调是否调 ProcessMatchResult）");

        // 教程：`RecordTutorialFinished` 的成功回调全文只有一句 Log、**不调** ProcessMatchResult ⇒ 不记
        Tr(!BattleDriver.ShouldRecordVictory(GameMode.Tutorial, false, 1, 0),
           "B1 ★ 教程局 ⇒ **不记**（原版 `PlayerDataManager___RecordTutorialFinished_g__HandleSuccess_464_0.c` 不调 `ProcessMatchResult`）");

        // 回放：不是「真打完」
        Tr(!BattleDriver.ShouldRecordVictory(GameMode.Classic, /*replay*/ true, 1, 0),
           "B2 ★ 回放局 ⇒ **不记**（回放不是「真打完」）");

        // 练习：`RecordPracticeMatchFinished` 的成功回调里直接调 `ProcessMatchResult` ⇒ **记**
        Tr(BattleDriver.ShouldRecordVictory(GameMode.OfflinePractice, false, 1, 0),
           "B3 ★ 练习局（`OfflinePractice`）⇒ **记**（原版 `…RecordPracticeMatchFinished_g__HandleSuccess_0.c` 调 `ProcessMatchResult`；" +
           "把练习也排除 ⇒ 这条红）");
        Tr(BattleDriver.ShouldRecordVictory(GameMode.Skirmish, false, 1, 0),
           "B4 遭遇局（`Skirmish`）⇒ 记（原版 `FastModeBaseEvent.RecordMatchFinished` 成功回调调 `ProcessMatchResult`）");
    }

    // ------------------------------------------------------------ C 宿主「真打过一局」后的接线核对（可选）
    /// <summary>宿主打完一局后调：把「当场那次结算有没有按胜/负/平去记」与同一份纯判定对上。
    /// <para>不依赖结局是胜还是负 ⇒ **永不 flaky**（胜局要求计数 +1、负/平要求计数 +0）。</para></summary>
    public static void AssertAfterMatch(int callsBefore, GameMode mode, bool replay, int winner, int me)
    {
        MenuCheck.Section(_sink, "C 宿主实打一局之后：生产结算按胜负决定「记 / 不记」");

        bool should = BattleDriver.ShouldRecordVictory(mode, replay, winner, me);
        int delta = BattleDriver.VictoryRecordCallsForTest - callsBefore;
        C(delta, should ? 1 : 0,
          "C1 ★ 结算那一跳的「记一场胜场」次数与纯判定一致（胜局 +1 / 负平 +0）——" +
          "🧨 把 `RecordVictoryCounterIfAny()` 从结算支删掉 ⇒ 胜局这里 delta=0 ⇒ 红");
        if (should)
            Tr(delta > 0, "C2 ★ **前提**：胜局当场「真去记」计数 > 0（防断言静默跳过）");
    }
}
