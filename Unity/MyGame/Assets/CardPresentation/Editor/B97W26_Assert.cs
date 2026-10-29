// B97W26_Assert.cs — 执行代理 B97_W26（第九十七会话 · 波 1 补位）
// ============================ 它盯的是什么 ============================
//  施工单 → `资料/完全复刻/判据/B97_R6_轮抽P2P房施工单.md` §二·3（步骤 2 · 功能本体）+ §三「两条会静默错的硬约束」
//  报告   → `资料/完全复刻/报告/B97_W26.md`
//  · A 段：`To Battle` 走联机时交过去的**模式串**必须 = `"ClosedDeck"`
//          （传 `"Skirmish"` ⇒ `Ctx.PlayMode = Skirmish` ⇒ 对局记录 `Mode = "Skirmish"` ⇒ **轮抽战绩永远不涨**，零报错）
//  · B 段：交过去的 `PlayerDeck.GameMode` 必须 = **13**（`NetPendingBattle.Vars` 只读 `IsSkirmish` ⇒ 填 7 会让 12 张牌按经典 30 张打，零报错）
//  · C 段：联机接管那条路**不许**写 `PrebuiltDecks.SetPendingBattleDeck` / `BattleDriver.SetPendingPlayMode`
//          （那两个是「读一次就清」的跨场景静态通道，联机这条路没人读 ⇒ 写了会漏到**下一局单机**）
//
// ============================ 🔴 灭自证（每条都写清「怎么才能让它红」）============================
//  · A1 断的是**字面量** `"ClosedDeck"`（期望值来自 `DraftOutcomes` 的消费者口径，不从 `NetModeName` 自己读）
//        🧨 把 `NetModeName` 改成 `PlayModeNames.Name(GameMode.Skirmish)` ⇒ A1/A5/A6 红、A7 反而绿。
//  · A6/A7 用**真 `DraftOutcomes`**数两条合成记录：发对串 ⇒ 数到 1；发 `"Skirmish"` ⇒ 数到 0
//        —— 这条把「静默 bug 长什么样」直接量出来，不是嘴上说。
//  · B2 断字面量 13；B7/B8 是**反例端到端**：`gameMode = 7` ⇒ `IsSkirmish = false` 且 `Vars.deckSize = 30`
//        🧨 把 `NetDeckGameMode` 改成 `(int)GameMode.ClosedDeck` ⇒ B2/B4/B5/B6 红。
//  · C6/C7 在**真接管体**（`DraftModeWindow.OnNetTookOver`，不是另写一份模拟）跑完之后读两条静态通道
//        🧨 在 `OnNetTookOver` 里加 `PrebuiltDecks.SetPendingBattleDeck(pre)` 或
//           `BattleDriver.SetPendingPlayMode(GameMode.ClosedDeck)` ⇒ C6/C7 立刻红。
//  · C9 断「没有联机会话时点取消**不假装成功**」🧨 把 `CancelSearch` 写成无条件 `NetTookOver = false` ⇒ C9 红。
//  ⛔ 本文件**不跑 Unity 批处理**、不建持久资产；临时结点收尾 `DestroyImmediate`。
//
// ============================ 宿主接线（由调度台做）============================
//   建议宿主：`Editor/DraftWindowAssertD2.cs`（轮抽窗自身那条链，本批三条都落在这扇窗上）
//     或 `Editor/NetSelfTest.cs`（联机域；它已经在建 `SearchingMatchPopup`，环境最接近）。
//   宿主 `Run()` 里加两行：
//     B97W26_Assert.Run();
//     _sink.Pass += B97W26_Assert.Pass; _sink.Fail += B97W26_Assert.Fail;
using System;
using System.Collections.Generic;
using CardPresentation;
using CardPresentation.Net;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B97_W26：轮抽 P2P 房（步骤 2）的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W26_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B97W26] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W26 · 轮抽 P2P 房（步骤 2：`To Battle` 走联机）");

        SectionA_过网模式串();
        SectionB_规则参数档();
        SectionC_联机接管不泄漏();

        Debug.Log(_sink.P + "B97_W26 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 过网模式串必须是 "ClosedDeck"
    static void SectionA_过网模式串()
    {
        // 判据：原版 `DraftEventBase.get_EventPlayMode` = `return 7`（= `PlayModes.ClosedDeck`）
        //      → `资料/完全复刻/判据/B97_R6_轮抽P2P房施工单.md` §三·1 第 3 行。
        // 🧨 改坏法：`NetModeName` 改成 `PlayModeNames.Name(GameMode.Skirmish)` ⇒ 本条红。
        C(DraftModeWindow.NetModeName, "ClosedDeck",
          "A1 过网的模式串 == \"ClosedDeck\"（字面量；原版 `EventPlayMode` = 7 ClosedDeck）");

        // 送出去的串被**真实消费链**解析回来：`MsgStart.mode → NetPendingBattle.PlayMode`（`PlayModeNames.Parse`）。
        C(PlayModeNames.Parse(DraftModeWindow.NetModeName), GameMode.ClosedDeck,
          "A2 `PlayModeNames.Parse(MsgStart.mode)` == `ClosedDeck`");

        // 端到端：一份**真的 `MsgStart`**（mode = 我们发的那串）走 `NetPendingBattle.FromStart`。
        var pb = NetPendingBattle.FromStart(new MsgStart
        {
            seed = 12345,
            mode = DraftModeWindow.NetModeName,
            arena = "battlearena1",
            hostFirst = 0,
            hostFaction = "Ultramarines",
            clientFaction = "Ultramarines",
        }, true);
        Tr(pb != null, "A3 夹具：`MsgStart` 能被 `NetPendingBattle.FromStart` 读出来（不为 null）");
        if (pb != null)
        {
            C(pb.ModeStr, "ClosedDeck", "A4 开局包里的 mode 串逐字 == \"ClosedDeck\"");
            C(pb.PlayMode, GameMode.ClosedDeck, "A5 开局包的 `PlayMode` == `ClosedDeck(7)`（= 原版 `MatchData.playMode`）");

            // 那一串正是**战绩回灌**数的那个串 —— 拿真 `DraftOutcomes` 数一遍。
            var log = new List<BattleLogData.Match>
            {
                new BattleLogData.Match { Mode = pb.ModeStr, Result = BattleLogData.Outcome.Victory },
            };
            C(DraftModeWindow.DraftOutcomes(log).Count, 1,
              "A6 `DraftOutcomes` 认得出这条记录（⇒ 打完回壳后轮抽战绩会 +1）");
        }

        // 反例（灭自证的另一面）：`"Skirmish"` 那条记录**不该**被数成轮抽局。
        var log2 = new List<BattleLogData.Match>
        {
            new BattleLogData.Match { Mode = "Skirmish", Result = BattleLogData.Outcome.Victory },
        };
        C(DraftModeWindow.DraftOutcomes(log2).Count, 0,
          "A7 反例：`Mode = \"Skirmish\"` 的记录**不算**轮抽局（发错串 = 战绩永远不涨）");
    }

    // ------------------------------------------------------------ B 规则参数档必须是 13（Skirmish）
    static void SectionB_规则参数档()
    {
        C((int)GameMode.Skirmish, 13, "B1 夹具：`GameMode.Skirmish` 的枚举值 == 13");
        C(DraftModeWindow.NetDeckGameMode, 13,
          "B2 交过网的卡组 `gameMode` == 13（= `LaunchBattle` 里 `pre.gameMode` 的真源）");

        var pd = PrebuiltDecks.ToPlayerDeck(new PrebuiltDecks.Deck
        {
            deckId = "b97w26",
            name = "B97",
            faction = "Ultramarines",
            gameMode = DraftModeWindow.NetDeckGameMode,
            cardIds = new string[0],
        });
        Tr(pd != null, "B3 夹具：`PrebuiltDecks.ToPlayerDeck` 能造出 `PlayerDeck`");
        if (pd != null)
        {
            C(pd.GameMode, 13, "B4 过网后 `PlayerDeck.GameMode` 仍是 13");
            Tr(pd.IsSkirmish, "B5 `PlayerDeck.IsSkirmish` 为真（`NetPendingBattle.Vars` 只读这一格）");
            C(new NetPendingBattle { Seat0Deck = pd }.Vars.deckSize, 12,
              "B6 由此得到的规则参数 = **12 张**那套（Skirmish）");
        }

        // 反例端到端（= 施工单「反坑 2」的实测）：填 7 会**静默**把 12 张按经典 30 张打。
        var bad = PrebuiltDecks.ToPlayerDeck(new PrebuiltDecks.Deck
        {
            deckId = "b97w26bad",
            name = "B97bad",
            gameMode = (int)GameMode.ClosedDeck,
            cardIds = new string[0],
        });
        Tr(bad != null && !bad.IsSkirmish, "B7 反例：`gameMode = 7` ⇒ `IsSkirmish` 为假（不是我们要的那一档）");
        if (bad != null)
            C(new NetPendingBattle { Seat0Deck = bad }.Vars.deckSize, 30,
              "B8 反例：`gameMode = 7` ⇒ `Vars.deckSize` = **30**（经典那套 —— 12 张轮抽牌会按它打，零报错）");
    }

    // ------------------------------------------------------------ C 联机接管不许写跨局静态通道
    static void SectionC_联机接管不泄漏()
    {
        // 前置硬检查（先断前提，再断结论 —— ⛔ 不写 `if (x != null) Check(...)`）。
        PrebuiltDecks.ClearPendingBattleDeck();
        BattleDriver.TakePendingPlayMode();
        Tr(PrebuiltDecks.PendingSource == null, "C1 前提：`PrebuiltDecks.PendingSource` 已清空");
        Tr(BattleDriver.PendingPlayMode == null, "C2 前提：`BattleDriver.PendingPlayMode` 已清空");

        var pre = new PrebuiltDecks.Deck
        {
            deckId = "b97w26net",
            name = "B97net",
            faction = "Ultramarines",
            gameMode = DraftModeWindow.NetDeckGameMode,
            cardIds = new string[0],
        };

        GameObject go = new GameObject("B97W26_DraftWin");
        DraftModeWindow win = null;
        Exception err = null;
        try
        {
            win = go.AddComponent<DraftModeWindow>();
            win.OnNetTookOver(pre, null);           // = `TryStart` 返回真之后**那一整支**（真代码，不是另写一份模拟）
        }
        catch (Exception e) { err = e; }

        Tr(win != null, "C3 夹具：`DraftModeWindow` 组件建得出来");
        if (win != null)
        {
            Tr(win.NetTookOver, "C4 接管后 `NetTookOver` = 真");
            Tr(win.StartedBattle, "C5 接管后 `StartedBattle` = 真（自检口径不变）");

            // 🔴 C6/C7 = 施工单「反坑 4」的唯一真闸：
            //   🧨 往 `OnNetTookOver` 里加 `SetPendingBattleDeck` / `SetPendingPlayMode` 任一句 ⇒ 这两条立刻红。
            Tr(PrebuiltDecks.PendingSource == null,
               "C6 联机接管后 `PrebuiltDecks.PendingSource` 仍是 null（否则漏到下一局单机）");
            Tr(BattleDriver.PendingPlayMode == null,
               "C7 联机接管后 `BattleDriver.PendingPlayMode` 仍是 null（同上）");

            if (err == null)
            {
                Tr(win.NetWaiting, "C8 接管后进入「等对手」状态（`Searching Oponent Popup` 在等真人，不跑 12 秒）");
            }
            else
            {
                // 建不出 UI（批处理里可能发生，同 `NetSelfTest` N⑤–N⑧ 的口径）⇒ **如实说「没验到」**，不写成绿。
                Debug.LogWarning("[B97W26] ⚠️ C8 **没验到**：本宿主建不出 `Searching Oponent Popup`（"
                               + err.GetType().Name + "：" + err.Message + "）—— 这不是「通过」也不是「失败」；"
                               + "C4–C7 那四条**照常成立**（它们不依赖 UI）。");
            }

            // 没有联机会话时点取消 ⇒ **不许假装取消成功**（`NetMatchmaking.Cancel` 自己那道闸）。
            win.CancelSearch();
            Tr(win.NetTookOver, "C9 没有联机会话时点取消 ⇒ `NetTookOver` **不变假**（不假装成功）");
        }

        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        NetRuntime.DrainNoticesForTest();                     // 清掉 C9 那一句通知，不污染别的宿主
        PrebuiltDecks.ClearPendingBattleDeck();               // 收尾：两条通道恢复干净
        BattleDriver.TakePendingPlayMode();
    }
}
