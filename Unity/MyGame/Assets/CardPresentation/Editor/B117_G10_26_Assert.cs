// B117_G10_26_Assert.cs — 第一百一十七会话 · 战斗域第 26 片「战斗内联机 · 断线重连表现」
//                           执行代理 `B117-G10-26`
//
// ============================ 覆盖 ============================
//  14 条断言，**全部是纯函数**（`Loc.T` 词条 + `NetBattle` 的 static 拼法/公式），
//  ⛔ 不起 socket、不建窗口、不等帧 —— 批处理里没有帧循环，这一片能验的就该只验这些。
//
//    A 段：**「开窗那一刻」与「每秒那一拍」的弹窗正文不是同一个拼法**（本轮修的那一条）
//      A1  前提：四条 `Battle/HUD/*` 词条取得到（`Loc.T` 取不到会回键名 ⇒ 后面全假绿）
//      A2 ★ 开窗（本机掉线）= `LostConnectionMsg` **换行** `PleaseWaitConnection`，**不带 `mm:ss`**
//      A3 ★ 开窗（对面掉线）= `WaitOpponentConnectionMsg` **一行**
//      A4 ★ 每秒那一拍（本机掉线）= `LostConnectionMsg` + `mm:ss`，**没有** `PleaseWaitConnection`
//      A5 ★ 每秒那一拍（对面掉线）= `WaitOpponentConnectionMsg` + `mm:ss`
//      A6   `mm:ss` 的格式（5 → `00:05`；65 → `01:05`）
//      A7   灭自证：A2 要求**有** `PleaseWaitConnection`、A4 要求**没有** ⇒ 两处合成一个函数必有一条红
//
//    B 段：**两端的重连秒数公式不是同一个**（本轮修的第二条）
//      B1   对手那条：`max(15, 30 − 3×enemyDisconnects)`（地板 15）
//      B2 ★ 本机那条：`max(10, 30 − 3×(playerDisconnects + 1))`（地板 10，第一次 = 27）
//      B3 ★ 灭自证：两条在 n = 0..4 上**逐值不同**，且地板不同（10 vs 15）⇒ 不可能是一个函数
//      B4   负输入按 0 处理（两处都不许抛 / 不许出负数）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  A2 把开窗那一支改回 `ReconnectPopup(...)`（带秒）⇒ A2 红。
//  A3 给对面那一档也拼第二行 ⇒ A3 红。
//  A4 把 `ReconnectPopup` 的 head 改回 `+ "\n" + PleaseWaitConnection` ⇒ A4 红。
//  A7 ↑ 这三条**不可能同时满足**：A2/A3 要开窗不带秒、A4/A5 要那一拍带秒且本机档没有第二行
//     ⇒ 「两处合成一个函数」怎么改都会有一条红。
//  B2 把 `StartReconnectCountdown` 退回一律用 `ReconnectSecondsFor` ⇒ B3 红（两条公式同值）。
//  B3 把两条公式改成同一个（任取一条）⇒ B3 红。
//
// ============================ 判据出处（一手）============================
//  · 开窗：`d:/2/tools/decomp_full/Everguild.BattleErrorUIManager__ShowDisconnectionPopUp.c:30-58`
//      `param_2 != 0` ⇒ `+0x30`（LostConnection）再 `AppendLine(+0x60)`（PleaseWaitConnection）；
//      `param_2 == 0` ⇒ `+0x48`（WaitingForOpponent）**一行**。**整个函数体没有 `TimeSpan`**。
//  · 每秒那一拍：`…__UpdateReconnectWindow.c:27-30`
//      `isPlayer ? +0x30 : +0x48` → `AppendLine(head)` → `AppendLine(TimeSpan.FromSeconds(sec))`
//      ⇒ **只有第一颗**，第二颗（PleaseWaitConnection）**不在**这一支里。
//  · 字段↔词条：`NetBattle.cs` 头部那一段（`BattleErrorUIManager` 的 9 个 `LocalizedString` 字段名）。
//  · 秒数：
//      对手 `BattleManager._ForfeitDisconnectedEnemy_d__322__MoveNext.c:61` `Math.Max(15, max + n×−3)`
//             （`enemyDisconnects` = `BattleManager+0x45C`，`dump.cs:30878`）
//      本机 `BattleManager._AttemptReconnect_d__320__MoveNext.c:29-31` `Math.Max(10, max + (n+1)×−3)`
//             （`playerDisconnects` = `BattleManager+0x448`，`dump.cs:30877`，**取的时候还没自增**）
//      `maxSecToReconnect` = `VarsGlobal+0x30` = **30**（`资料/VarsGlobal_原版数值.md`）。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（只要一个 `Loc` 表，不需要 GameObject）⇒ 任一宿主的聚合行、`//` 之前同样式追加即可：
//      B117G1026_Assert.Run(); pass += B117G1026_Assert.Pass; fail += B117G1026_Assert.Fail;
//  ⚠️ **先看那一行第一个 `//` 在哪**（`CLAUDE.md` §三：追加到 `//` 之后 = 断言永远不跑，
//     而且 `typecheck` / 账闸 / 退出码全都绿）。
//
// ============================ 边界（如实记）============================
//  · ⛔ 不验「弹窗真的弹出来了 / 窗口有没有那颗 OK 钮 / 每秒重建会不会闪」—— 那要真 Play（批处理无窗口系统）。
//    其中「原版那扇窗**没有按钮**（`ShowPopUp(text,0,0,0,0,0)`）、我们是带 OK 钮的 `PopUpGameWindow`」
//    **没有修**（落点在 `NetRuntime.cs`，本片红线不许动）⇒ 记在报告里。
//  · ⛔ 不验「本机掉线时**不停表**」那一条（原版 `ShowDisconnectionPopup.c:19-31` 只有 `param_2 == 0`
//    也就是**对手**掉线那一支调 `ClockManager.PauseClock()`）—— 我们目前两端都停表，**没有修**
//    （会联动 `BattleDriver.PlayerInputFrozen`，且本片无法用纯函数验到）⇒ 记在报告里。
using CardPresentation;
using CardPresentation.Net;
using UnityEngine;

/// <summary>执行代理 `B117-G10-26`：战斗内联机的**掉线重连表现**（弹窗正文两套拼法 + 两端秒数公式）。</summary>
public static class B117G1026_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G10-26] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    const string KLost = "Battle/HUD/LostConnectionMsg";
    const string KWait = "Battle/HUD/WaitOpponentConnectionMsg";
    const string KPlease = "Battle/HUD/PleaseWaitConnection";

    public static void Run()
    {
        MenuCheck.Section(_sink, "A · 掉线弹窗：开窗那一刻 vs 每秒那一拍");

        // ---- A1 前提：词条取得到（取不到时 `Loc.T` 回键名 ⇒ 下面几条会拿键名假绿）----
        string lost = Loc.T(KLost), wait = Loc.T(KWait), please = Loc.T(KPlease);
        MenuCheck.True(_sink,
            !string.IsNullOrEmpty(lost) && lost != KLost
         && !string.IsNullOrEmpty(wait) && wait != KWait
         && !string.IsNullOrEmpty(please) && please != KPlease,
            $"（前提）三条 `Battle/HUD/*` 词条都在表里（实得「{lost}」/「{wait}」/「{please}」）"
          + " —— ⛔ 取不到时 `Loc.T` 回键名，下面几条会**假绿**");

        // ---- A2 开窗（本机掉线）：两行、且不带 mm:ss ----
        string initSelf = NetBattle.ReconnectPopupInitial(true);
        MenuCheck.True(_sink,
            initSelf.Contains(lost) && initSelf.Contains(please) && !HasClock(initSelf),
            $"★ A2 开窗（本机掉线）= `LostConnectionMsg` 换行 `PleaseWaitConnection`、**不带秒**"
          + $"（实得「{Escape(initSelf)}」）"
          + "｜🧨 改回 `ReconnectPopup(...)`（带 `mm:ss`）⇒ 本行红"
          + "（判据 `…__ShowDisconnectionPopUp.c:30-58`：`+0x30` 再 `AppendLine(+0x60)`，全函数没有 `TimeSpan`）");

        // ---- A3 开窗（对面掉线）：一行 ----
        string initPeer = NetBattle.ReconnectPopupInitial(false);
        MenuCheck.Check(_sink, initPeer, wait,
            $"★ A3 开窗（对面掉线）= `WaitOpponentConnectionMsg` **一行**（实得「{Escape(initPeer)}」）"
          + "｜🧨 给这一档也拼第二行 / 拼上 `mm:ss` ⇒ 本行红（判定 = 逐字相等）");

        // ---- A4 每秒那一拍（本机掉线）：head + mm:ss，**没有**第二行 ----
        string tickSelf = NetBattle.ReconnectPopup(true, 25);
        MenuCheck.True(_sink,
            tickSelf.Contains(lost) && tickSelf.Contains("00:25") && !tickSelf.Contains(please),
            $"★ A4 每秒那一拍（本机掉线）= `LostConnectionMsg` + `00:25`，**没有 `PleaseWaitConnection`**"
          + $"（实得「{Escape(tickSelf)}」）"
          + "｜🧨 把 head 改回 `+ \"\\n\" + PleaseWaitConnection` ⇒ 本行红"
          + "（判据 `…__UpdateReconnectWindow.c:27-30`：只取第一颗 `LocalizedString` + `AppendLine(TimeSpan)`）");

        // ---- A5 每秒那一拍（对面掉线）----
        MenuCheck.Check(_sink, NetBattle.ReconnectPopup(false, 25), wait + "\n00:25",
            "★ A5 每秒那一拍（对面掉线）= `WaitOpponentConnectionMsg` + 换行 + `00:25`"
          + "｜🧨 拼上第二行 / 秒数格式变了 ⇒ 本行红");

        // ---- A6 mm:ss 的格式 ----
        MenuCheck.Check(_sink, NetBattle.ReconnectPopup(false, 5), wait + "\n00:05",
            "A6a `mm:ss`：5 秒 ⇒ `00:05`（原版 `TimeSpan.FromSeconds(sec).ToString()` 同形）");
        MenuCheck.Check(_sink, NetBattle.ReconnectPopup(false, 65), wait + "\n01:05",
            "A6b `mm:ss`：65 秒 ⇒ `01:05`（换算不是「秒数原样打印」）");

        // ---- A7 灭自证：两条要求**互斥** ----
        MenuCheck.True(_sink,
            initSelf.Contains(please) && !tickSelf.Contains(please),
            "★ A7 灭自证：**开窗**要 `PleaseWaitConnection`、**每秒那一拍**不许有它"
          + " ⇒ 把两处合成一个函数**怎么改都会红一条**"
          + "（这正是本轮修掉的那条缺陷：原来两处共用 `ReconnectPopup`）");

        MenuCheck.Section(_sink, "B · 重连秒数：对手一条、本机一条");

        // ---- B1 对手那条 ----
        MenuCheck.Check(_sink, NetBattle.ReconnectSecondsFor(0), 30,
            "B1a 对手第 1 次掉线 = **30** 秒（`max(15, 30 − 3×0)`）");
        MenuCheck.Check(_sink, NetBattle.ReconnectSecondsFor(1), 27,
            "B1b 对手第 2 次 = 27（`−3×1`）");
        MenuCheck.Check(_sink, NetBattle.ReconnectSecondsFor(9), NetBattle.ReconnectSecondsFloor,
            $"B1c 对手那条的地板 = **{NetBattle.ReconnectSecondsFloor}**（不是 0、也不是 10）");

        // ---- B2 本机那条（本轮修的那一条）----
        MenuCheck.Check(_sink, NetBattle.SelfReconnectSecondsFor(0), 27,
            "★ B2a 本机第 1 次掉线 = **27**（`max(10, 30 − 3×(0+1))`，`_AttemptReconnect_d__320:29-31`；"
          + "⚠️ 原来这里返回的是对手那条的 30）"
          + "｜🧨 退回一律 `ReconnectSecondsFor` ⇒ 本行红");
        MenuCheck.Check(_sink, NetBattle.SelfReconnectSecondsFor(1), 24, "B2b 本机第 2 次 = 24（`−3×2`）");
        MenuCheck.Check(_sink, NetBattle.SelfReconnectSecondsFor(5), 12, "B2c 本机第 6 次 = 12（`−3×6`）");
        MenuCheck.Check(_sink, NetBattle.SelfReconnectSecondsFor(7), NetBattle.SelfReconnectSecondsFloor,
            $"B2d 本机那条的地板 = **{NetBattle.SelfReconnectSecondsFloor}**（原版 `Math.Max(10, …)`）");

        // ---- B3 灭自证：两条公式**逐值不同** ----
        bool allDiffer = NetBattle.SelfReconnectSecondsFor(0) != NetBattle.ReconnectSecondsFor(0)
                      && NetBattle.SelfReconnectSecondsFor(1) != NetBattle.ReconnectSecondsFor(1)
                      && NetBattle.SelfReconnectSecondsFor(2) != NetBattle.ReconnectSecondsFor(2)
                      && NetBattle.SelfReconnectSecondsFor(3) != NetBattle.ReconnectSecondsFor(3)
                      && NetBattle.SelfReconnectSecondsFor(4) != NetBattle.ReconnectSecondsFor(4)
                      && NetBattle.SelfReconnectSecondsFloor != NetBattle.ReconnectSecondsFloor;
        MenuCheck.True(_sink, allDiffer,
            $"★ B3 灭自证：两端公式在 n=0..4 上**逐值不同**，地板也不同"
          + $"（本机 {NetBattle.SelfReconnectSecondsFor(0)}/{NetBattle.SelfReconnectSecondsFor(4)}/{NetBattle.SelfReconnectSecondsFloor}"
          + $" vs 对手 {NetBattle.ReconnectSecondsFor(0)}/{NetBattle.ReconnectSecondsFor(4)}/{NetBattle.ReconnectSecondsFloor}）"
          + " ⇒ 把两条合并成一个函数，**B1 与 B2 必有一组红**");

        // ---- B4 负输入 ----
        MenuCheck.True(_sink,
            NetBattle.SelfReconnectSecondsFor(-5) == 27 && NetBattle.ReconnectSecondsFor(-5) == 30,
            "B4 负输入按 0 处理（两条都不许抛、也不许出负数）");
    }

    // ==========================================================================================
    //  小工具
    // ==========================================================================================

    /// <summary>正文里有没有 `mm:ss` 那种时间片（`\d\d:\d\d`）。用来区分「开窗（原版不带秒）」与「每秒那一拍」。</summary>
    static bool HasClock(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i + 4 < s.Length; i++)
            if (char.IsDigit(s[i]) && char.IsDigit(s[i + 1]) && s[i + 2] == ':'
             && char.IsDigit(s[i + 3]) && char.IsDigit(s[i + 4])) return true;
        return false;
    }

    /// <summary>把换行显式化（日志是一行一条，直接印会把 `✓` 行拆断）。</summary>
    static string Escape(string s) { return s == null ? "<null>" : s.Replace("\n", "\\n"); }
}
